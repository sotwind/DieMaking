using System.Data;
using DieMaking.Data;
using DieMaking.Models;
using Oracle.ManagedDataAccess.Client;

namespace DieMaking.Services;

/// <summary>
/// 易捷数据导入服务
/// </summary>
public class YijieImportService
{
    private readonly DieService _dieService;

    public YijieImportService()
    {
        _dieService = new DieService();
    }

    public YijieImportService(DieService dieService)
    {
        _dieService = dieService;
    }

    /// <summary>
    /// 从易捷系统查询工单
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="factoryName">工厂名称(可选)</param>
    /// <returns>工单列表</returns>
    public List<YijieWorkOrder> QueryWorkOrders(DateTime startDate, DateTime endDate, string? factoryName = null)
    {
        var orders = new List<YijieWorkOrder>();
        var dbInfos = YijieDatabaseConfig.GetDatabaseInfos();

        // 如果指定了工厂，只查询该工厂
        if (!string.IsNullOrEmpty(factoryName))
        {
            dbInfos = dbInfos.Where(d => d.FactoryName == factoryName).ToList();
        }

        foreach (var dbInfo in dbInfos)
        {
            try
            {
                var factoryOrders = QueryWorkOrdersFromDatabase(dbInfo, startDate, endDate);
                orders.AddRange(factoryOrders);
            }
            catch (Exception ex)
            {
                // 记录错误但继续查询其他工厂
                System.Diagnostics.Debug.WriteLine($"查询工厂 {dbInfo.FactoryName} 失败: {ex.Message}");
            }
        }

        return orders;
    }

    /// <summary>
    /// 从指定数据库查询工单
    /// </summary>
    private List<YijieWorkOrder> QueryWorkOrdersFromDatabase(YijieDatabaseInfo dbInfo, DateTime startDate, DateTime endDate)
    {
        var orders = new List<YijieWorkOrder>();
        var connString = dbInfo.GetOracleConnectionString();

        using var conn = new OracleConnection(connString);
        conn.Open();

        // 根据系统类型选择不同的查询SQL
        string sql;
        if (dbInfo.ServerType == "新系统")
        {
            sql = GetNewSystemQuerySql();
        }
        else
        {
            sql = GetOldSystemQuerySql();
        }

        using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("startDate", OracleDbType.Date) { Value = startDate });
        cmd.Parameters.Add(new OracleParameter("endDate", OracleDbType.Date) { Value = endDate });

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var order = MapWorkOrder(reader, dbInfo.FactoryName);
            if (order != null)
            {
                orders.Add(order);
            }
        }

        return orders;
    }

    /// <summary>
    /// 获取新系统查询SQL
    /// </summary>
    private string GetNewSystemQuerySql()
    {
        return @"
            SELECT 
                o.ord_cde as OrderNo,
                c.cust_nme as CustomerName,
                p.prd_nme as ProductName,
                p.structure as Structure,
                p.material as Material,
                p.mft_lng as ManufactureLength,
                p.mft_wid as ManufactureWidth,
                p.mft_hgt as ManufactureHeight,
                p.blk_lng as BlankLength,
                p.blk_wid as BlankWidth,
                p.pcs_dsc as ProcessDesc,
                o.dlv_dte as DeliveryDate,
                o.ord_typ as OrderType,
                o.crt_dte as CreateTime
            FROM oe_order o
            LEFT JOIN pb_cust c ON o.cust_cde = c.cust_cde
            LEFT JOIN oe_order_prd p ON o.ord_cde = p.ord_cde
            WHERE o.crt_dte >= :startDate 
              AND o.crt_dte <= :endDate
              AND o.ord_typ = 'NEW'  -- 新刀订单
              AND o.ord_sta = 'CONFIRMED'  -- 已确认订单
            ORDER BY o.crt_dte DESC";
    }

    /// <summary>
    /// 获取老系统查询SQL
    /// </summary>
    private string GetOldSystemQuerySql()
    {
        return @"
            SELECT 
                o.ord_cde as OrderNo,
                c.cust_nme as CustomerName,
                p.prd_nme as ProductName,
                p.structure as Structure,
                p.material as Material,
                p.mft_lng as ManufactureLength,
                p.mft_wid as ManufactureWidth,
                p.mft_hgt as ManufactureHeight,
                p.blk_lng as BlankLength,
                p.blk_wid as BlankWidth,
                p.pcs_dsc as ProcessDesc,
                o.dlv_dte as DeliveryDate,
                o.ord_typ as OrderType,
                o.crt_dte as CreateTime
            FROM so_ord_mst o
            LEFT JOIN pb_cust c ON o.cust_cde = c.cust_cde
            LEFT JOIN so_ord_dtl p ON o.ord_cde = p.ord_cde
            WHERE o.crt_dte >= :startDate 
              AND o.crt_dte <= :endDate
              AND o.ord_typ = 'NEW'
              AND o.ord_sta = 'CONFIRMED'
            ORDER BY o.crt_dte DESC";
    }

    /// <summary>
    /// 映射工单数据
    /// </summary>
    private YijieWorkOrder? MapWorkOrder(IDataReader reader, string factoryName)
    {
        try
        {
            var orderNo = reader["OrderNo"]?.ToString();
            if (string.IsNullOrEmpty(orderNo))
                return null;

            return new YijieWorkOrder
            {
                OrderNo = orderNo,
                CustomerName = reader["CustomerName"]?.ToString() ?? string.Empty,
                ProductName = reader["ProductName"]?.ToString() ?? string.Empty,
                Structure = reader["Structure"]?.ToString() ?? string.Empty,
                Material = reader["Material"]?.ToString() ?? string.Empty,
                ManufactureLength = reader["ManufactureLength"] != DBNull.Value ? Convert.ToDecimal(reader["ManufactureLength"]) : 0,
                ManufactureWidth = reader["ManufactureWidth"] != DBNull.Value ? Convert.ToDecimal(reader["ManufactureWidth"]) : 0,
                ManufactureHeight = reader["ManufactureHeight"] != DBNull.Value ? Convert.ToDecimal(reader["ManufactureHeight"]) : 0,
                BlankLength = reader["BlankLength"] != DBNull.Value ? Convert.ToDecimal(reader["BlankLength"]) : 0,
                BlankWidth = reader["BlankWidth"] != DBNull.Value ? Convert.ToDecimal(reader["BlankWidth"]) : 0,
                ProcessDesc = reader["ProcessDesc"]?.ToString() ?? string.Empty,
                DeliveryDate = reader["DeliveryDate"] != DBNull.Value ? Convert.ToDateTime(reader["DeliveryDate"]) : null,
                IsNewDie = reader["OrderType"]?.ToString() == "NEW",
                OrderCreateTime = reader["CreateTime"] != DBNull.Value ? Convert.ToDateTime(reader["CreateTime"]) : DateTime.Now,
                FactoryName = factoryName
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 筛选可导入的工单(排除已存在的)
    /// </summary>
    public List<YijieWorkOrder> FilterImportableOrders(List<YijieWorkOrder> orders)
    {
        var importableOrders = new List<YijieWorkOrder>();

        foreach (var order in orders)
        {
            // 只导入新刀订单
            if (!order.IsNewDie)
                continue;

            // 检查是否已存在
            if (_dieService.IsExternalOrderExists(order.OrderNo))
                continue;

            importableOrders.Add(order);
        }

        return importableOrders;
    }

    /// <summary>
    /// 将易捷工单转换为刀模信息
    /// </summary>
    private DieInfo ConvertToDieInfo(YijieWorkOrder order)
    {
        var die = new DieInfo
        {
            DieCode = GenerateDieCode(),
            CustomerName = order.CustomerName,
            ProductName = order.ProductName,
            Structure = order.Structure,
            Material = order.Material,
            ManufactureLength = order.ManufactureLength,
            ManufactureWidth = order.ManufactureWidth,
            ManufactureHeight = order.ManufactureHeight,
            BlankLength = order.BlankLength,
            BlankWidth = order.BlankWidth,
            ProcessDesc = order.ProcessDesc,
            DeliveryDate = order.DeliveryDate,
            ExternalOrderNo = order.OrderNo,
            SourceFactory = order.FactoryName,
            Status = DieStatus.Pending,
            AuditStatus = AuditStatus.Unaudited,
            CreateTime = DateTime.Now
        };

        // 根据工艺描述生成默认工序
        die.Processes = GenerateDefaultProcesses(order.ProcessDesc);
        die.RequiredProcesses = string.Join(",", die.Processes.Select(p => p.ProcessName));

        return die;
    }

    /// <summary>
    /// 生成默认工序
    /// </summary>
    private List<DieProcess> GenerateDefaultProcesses(string processDesc)
    {
        var processes = new List<DieProcess>();
        var defaultProcessNames = new[] { "绘图", "切割", "打磨", "质检" };

        foreach (var name in defaultProcessNames)
        {
            processes.Add(new DieProcess
            {
                ProcessName = name,
                Status = ProcessStatus.Pending,
                CreateTime = DateTime.Now
            });
        }

        return processes;
    }

    /// <summary>
    /// 生成刀模编号
    /// </summary>
    private string GenerateDieCode()
    {
        return $"DM{DateTime.Now:yyyyMMdd}{new Random().Next(1000, 9999)}";
    }

    /// <summary>
    /// 导入工单到刀模系统
    /// </summary>
    /// <param name="orders">要导入的工单列表</param>
    /// <param name="createUser">创建人</param>
    /// <returns>导入结果</returns>
    public ImportResult ImportOrders(List<YijieWorkOrder> orders, string createUser)
    {
        var result = new ImportResult
        {
            TotalCount = orders.Count,
            SuccessCount = 0,
            FailedCount = 0,
            FailedOrders = new List<string>()
        };

        foreach (var order in orders)
        {
            try
            {
                var die = ConvertToDieInfo(order);
                var dieId = _dieService.CreateDie(die, die.Processes);

                if (dieId > 0)
                {
                    result.SuccessCount++;
                }
                else
                {
                    result.FailedCount++;
                    result.FailedOrders.Add(order.OrderNo);
                }
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.FailedOrders.Add($"{order.OrderNo}({ex.Message})");
            }
        }

        return result;
    }

    /// <summary>
    /// 获取可导入的工单(带筛选)
    /// </summary>
    public List<YijieWorkOrder> GetImportableWorkOrders(DateTime startDate, DateTime endDate, string? factoryName = null)
    {
        var allOrders = QueryWorkOrders(startDate, endDate, factoryName);
        return FilterImportableOrders(allOrders);
    }
}

/// <summary>
/// 导入结果
/// </summary>
public class ImportResult
{
    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 成功数量
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败数量
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// 失败的订单号列表
    /// </summary>
    public List<string> FailedOrders { get; set; } = new();

    /// <summary>
    /// 导入消息
    /// </summary>
    public string Message => $"成功导入 {SuccessCount}/{TotalCount} 条记录";
}
