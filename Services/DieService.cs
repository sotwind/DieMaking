using System.Data;
using DieMaking.Helpers;
using DieMaking.Models;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

/// <summary>
/// 刀模服务类
/// </summary>
public class DieService
{
    #region 基础CRUD操作

    /// <summary>
    /// 根据ID获取刀模
    /// </summary>
    public DieInfo? GetDieById(int dieId)
    {
        if (dieId <= 0) return null;

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        const string sql = @"
            SELECT * FROM DieInfo 
            WHERE DieID = @DieID";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@DieID", dieId));

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapDieInfo(reader);
        }
        return null;
    }

    /// <summary>
    /// 搜索刀模列表
    /// </summary>
    public List<DieInfo> SearchDies(
        string? dieCode = null,
        string? customerName = null,
        DieStatus? status = null,
        AuditStatus? auditStatus = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var list = new List<DieInfo>();

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        var sql = @"SELECT * FROM DieInfo WHERE 1=1";
        var parameters = new List<SqlParameter>();

        if (!string.IsNullOrEmpty(dieCode))
        {
            sql += " AND DieCode LIKE @DieCode";
            parameters.Add(new SqlParameter("@DieCode", $"%{dieCode}%"));
        }

        if (!string.IsNullOrEmpty(customerName))
        {
            sql += " AND CustomerName LIKE @CustomerName";
            parameters.Add(new SqlParameter("@CustomerName", $"%{customerName}%"));
        }

        if (status.HasValue)
        {
            sql += " AND Status = @Status";
            parameters.Add(new SqlParameter("@Status", (int)status.Value));
        }

        if (auditStatus.HasValue)
        {
            sql += " AND AuditStatus = @AuditStatus";
            parameters.Add(new SqlParameter("@AuditStatus", (int)auditStatus.Value));
        }

        if (startDate.HasValue)
        {
            sql += " AND CreateTime >= @StartDate";
            parameters.Add(new SqlParameter("@StartDate", startDate.Value));
        }

        if (endDate.HasValue)
        {
            sql += " AND CreateTime <= @EndDate";
            parameters.Add(new SqlParameter("@EndDate", endDate.Value));
        }

        sql += " ORDER BY CreateTime DESC";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var param in parameters)
        {
            cmd.Parameters.Add(param);
        }

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapDieInfo(reader));
        }

        return list;
    }

    /// <summary>
    /// 获取所有刀模
    /// </summary>
    public List<DieInfo> GetAllDies()
    {
        return SearchDies();
    }

    /// <summary>
    /// 创建刀模
    /// </summary>
    public int CreateDie(DieInfo die, List<DieProcess>? processes = null)
    {
        if (die == null || string.IsNullOrEmpty(die.DieCode))
            return 0;

        // 如果没有传入工序，自动生成默认工序
        if (processes == null || processes.Count == 0)
        {
            processes = GenerateDefaultProcesses();
        }

        // 计算费用
        die.CalculateFees();

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        using var transaction = conn.BeginTransaction();
        try
        {
            // 插入刀模主表
            const string insertDieSql = @"
                INSERT INTO DieInfo (DieCode, WorkOrderNo, CustomerName, ProductName, Structure, ModelType, LayoutType, 
                    FluteType, Material, ManufactureLength, ManufactureWidth, ManufactureHeight, 
                    BlankLength, BlankWidth, KnifeLengthM, KnifeMarkLengthM, 
                    BoardFeeUnitPrice, BoardFee, ProductionUnitPrice, ProductionFee, DesignUnitPrice, DesignFee,
                    ProcessDesc, RequiredProcesses, Status, AuditStatus, 
                    SourceFactory, ExternalOrderID, DeliveryDate, CreateTime, CreateUser, Remark)
                VALUES (@DieCode, @WorkOrderNo, @CustomerName, @ProductName, @Structure, @ModelType, @LayoutType, 
                    @FluteType, @Material, @ManufactureLength, @ManufactureWidth, @ManufactureHeight, 
                    @BlankLength, @BlankWidth, @KnifeLengthM, @KnifeMarkLengthM,
                    @BoardFeeUnitPrice, @BoardFee, @ProductionUnitPrice, @ProductionFee, @DesignUnitPrice, @DesignFee,
                    @ProcessDesc, @RequiredProcesses, @Status, @AuditStatus, 
                    @SourceFactory, @ExternalOrderID, @DeliveryDate, @CreateTime, @CreateUser, @Remark);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = insertDieSql;
            AddDieParameters(cmd, die);

            var dieId = Convert.ToInt32(cmd.ExecuteScalar());

            // 插入工序
            foreach (var process in processes)
            {
                process.DieID = dieId;
                InsertProcess(conn, transaction, process);
            }

            transaction.Commit();
            return dieId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 生成默认工序（绘图、割板、弯刀、装刀、贴泡沫）
    /// </summary>
    private List<DieProcess> GenerateDefaultProcesses()
    {
        var processNames = new[] { "绘图", "割板", "弯刀", "装刀", "贴泡沫" };
        var processes = new List<DieProcess>();
        int? prevProcessId = null;

        foreach (var name in processNames)
        {
            var process = new DieProcess
            {
                ProcessName = name,
                Status = ProcessStatus.Pending,
                CreateTime = DateTime.Now,
                PrevProcessID = prevProcessId,
                IsPrevCompleted = prevProcessId == null // 第一个工序默认前道工序已完成
            };
            processes.Add(process);
            // 这里不设置 ProcessID，因为是新创建的记录
        }

        return processes;
    }

    /// <summary>
    /// 更新刀模
    /// </summary>
    public bool UpdateDie(DieInfo die, List<DieProcess>? processes)
    {
        if (die == null || die.DieID <= 0)
            return false;

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        using var transaction = conn.BeginTransaction();
        try
        {
            // 更新刀模主表
            const string updateSql = @"
                UPDATE DieInfo SET
                    DieCode = @DieCode,
                    CustomerName = @CustomerName,
                    ProductName = @ProductName,
                    Structure = @Structure,
                    ModelType = @ModelType,
                    LayoutType = @LayoutType,
                    FluteType = @FluteType,
                    Material = @Material,
                    ManufactureLength = @ManufactureLength,
                    ManufactureWidth = @ManufactureWidth,
                    ManufactureHeight = @ManufactureHeight,
                    BlankLength = @BlankLength,
                    BlankWidth = @BlankWidth,
                    ProcessDesc = @ProcessDesc,
                    RequiredProcesses = @RequiredProcesses,
                    Status = @Status,
                    AuditStatus = @AuditStatus,
                    SourceFactory = @SourceFactory,
                    ExternalOrderID = @ExternalOrderID,
                    DeliveryDate = @DeliveryDate,
                    UpdateTime = @UpdateTime,
                    Remark = @Remark
                WHERE DieID = @DieID";

            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = updateSql;
            AddDieParameters(cmd, die);
            cmd.Parameters.Add(new SqlParameter("@DieID", die.DieID));
            cmd.Parameters.Add(new SqlParameter("@UpdateTime", DateTime.Now));

            var rowsAffected = cmd.ExecuteNonQuery();

            // 更新工序
            if (processes != null)
            {
                // 先删除原有工序
                using var delCmd = conn.CreateCommand();
                delCmd.Transaction = transaction;
                delCmd.CommandText = "DELETE FROM DieProcess WHERE DieID = @DieID";
                delCmd.Parameters.Add(new SqlParameter("@DieID", die.DieID));
                delCmd.ExecuteNonQuery();

                // 插入新工序
                foreach (var process in processes)
                {
                    process.DieID = die.DieID;
                    InsertProcess(conn, transaction, process);
                }
            }

            transaction.Commit();
            return rowsAffected > 0;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 删除刀模
    /// </summary>
    public bool DeleteDie(int dieId)
    {
        if (dieId <= 0) return false;

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        using var transaction = conn.BeginTransaction();
        try
        {
            // 先删除工序
            using var delProcessCmd = conn.CreateCommand();
            delProcessCmd.Transaction = transaction;
            delProcessCmd.CommandText = "DELETE FROM DieProcess WHERE DieID = @DieID";
            delProcessCmd.Parameters.Add(new SqlParameter("@DieID", dieId));
            delProcessCmd.ExecuteNonQuery();

            // 再删除刀模
            using var delDieCmd = conn.CreateCommand();
            delDieCmd.Transaction = transaction;
            delDieCmd.CommandText = "DELETE FROM DieInfo WHERE DieID = @DieID";
            delDieCmd.Parameters.Add(new SqlParameter("@DieID", dieId));

            var rowsAffected = delDieCmd.ExecuteNonQuery();
            transaction.Commit();
            return rowsAffected > 0;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 审核刀模
    /// </summary>
    public bool AuditDie(int dieId, bool isApproved)
    {
        if (dieId <= 0) return false;

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        const string sql = @"
            UPDATE DieInfo SET
                AuditStatus = @AuditStatus,
                UpdateTime = @UpdateTime
            WHERE DieID = @DieID";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@DieID", dieId));
        cmd.Parameters.Add(new SqlParameter("@AuditStatus", isApproved ? (int)AuditStatus.Audited : (int)AuditStatus.Unaudited));
        cmd.Parameters.Add(new SqlParameter("@UpdateTime", DateTime.Now));

        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 获取刀模工序
    /// </summary>
    public List<DieProcess> GetDieProcesses(int dieId)
    {
        var list = new List<DieProcess>();
        if (dieId <= 0) return list;

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        const string sql = @"
            SELECT * FROM DieProcess 
            WHERE DieID = @DieID 
            ORDER BY ProcessID";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@DieID", dieId));

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapDieProcess(reader));
        }

        return list;
    }

    /// <summary>
    /// 检查刀模编号是否存在
    /// </summary>
    public bool IsDieCodeExists(string dieCode, int? excludeDieId = null)
    {
        if (string.IsNullOrEmpty(dieCode)) return false;

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        string sql = "SELECT COUNT(1) FROM DieInfo WHERE DieCode = @DieCode";
        if (excludeDieId.HasValue)
        {
            sql += " AND DieID != @ExcludeDieId";
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@DieCode", dieCode));
        if (excludeDieId.HasValue)
        {
            cmd.Parameters.Add(new SqlParameter("@ExcludeDieId", excludeDieId.Value));
        }

        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    /// <summary>
    /// 获取已完工但未入库的刀模列表
    /// </summary>
    public List<DieInfo> GetCompletedDiesNotInStock()
    {
        var list = new List<DieInfo>();
        var sql = @"SELECT d.*, u.RealName as CreateUserName 
                     FROM DM_DieInfo d
                     LEFT JOIN DM_User u ON d.CreateUser = u.Username
                     WHERE d.Status = @Status
                     AND NOT EXISTS (
                         SELECT 1 FROM DM_DieInventory i 
                         WHERE i.DieID = d.DieID AND i.StorageStatus != @DeletedStatus
                     )
                     ORDER BY d.UpdateTime DESC";

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@Status", (int)DieStatus.Completed));
        cmd.Parameters.Add(new SqlParameter("@DeletedStatus", (int)StorageStatus.Scrapped));

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapDieInfo(reader));
        }

        return list;
    }

    /// <summary>
    /// 更新工序状态
    /// </summary>
    public bool UpdateProcessStatus(int processId, ProcessStatus status, string? operatorNo = null, string? operatorName = null)
    {
        if (processId <= 0) return false;

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        var sql = @"
            UPDATE DieProcess SET
                Status = @Status";

        if (status == ProcessStatus.InProgress)
        {
            sql += ", StartTime = @StartTime";
        }
        else if (status == ProcessStatus.Completed)
        {
            sql += ", CompleteTime = @CompleteTime";
        }

        if (!string.IsNullOrEmpty(operatorNo))
        {
            sql += ", OperatorNo = @OperatorNo";
        }

        if (!string.IsNullOrEmpty(operatorName))
        {
            sql += ", OperatorName = @OperatorName";
        }

        sql += " WHERE ProcessID = @ProcessID";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@ProcessID", processId));
        cmd.Parameters.Add(new SqlParameter("@Status", (int)status));

        if (status == ProcessStatus.InProgress)
        {
            cmd.Parameters.Add(new SqlParameter("@StartTime", DateTime.Now));
        }
        else if (status == ProcessStatus.Completed)
        {
            cmd.Parameters.Add(new SqlParameter("@CompleteTime", DateTime.Now));
        }

        if (!string.IsNullOrEmpty(operatorNo))
        {
            cmd.Parameters.Add(new SqlParameter("@OperatorNo", operatorNo));
        }

        if (!string.IsNullOrEmpty(operatorName))
        {
            cmd.Parameters.Add(new SqlParameter("@OperatorName", operatorName));
        }

        return cmd.ExecuteNonQuery() > 0;
    }

    #endregion

    #region 改刀功能

    /// <summary>
    /// 添加改刀记录
    /// </summary>
    public bool AddModificationRecord(int dieId, decimal amount, string modifiedBy, string? reason = null, string? remark = null)
    {
        if (dieId <= 0 || amount <= 0 || string.IsNullOrEmpty(modifiedBy))
            return false;

        // 获取刀模信息
        var die = GetDieById(dieId);
        if (die == null) return false;

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        const string sql = @"
            INSERT INTO DieModificationRecord 
                (DieID, DieCode, CustomerName, ProductName, ModificationAmount, 
                 ModificationTime, ModifiedBy, Reason, Remark, CreateTime)
            VALUES 
                (@DieID, @DieCode, @CustomerName, @ProductName, @ModificationAmount, 
                 @ModificationTime, @ModifiedBy, @Reason, @Remark, @CreateTime)";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@DieID", dieId));
        cmd.Parameters.Add(new SqlParameter("@DieCode", die.DieCode));
        cmd.Parameters.Add(new SqlParameter("@CustomerName", die.CustomerName));
        cmd.Parameters.Add(new SqlParameter("@ProductName", die.ProductName));
        cmd.Parameters.Add(new SqlParameter("@ModificationAmount", amount));
        cmd.Parameters.Add(new SqlParameter("@ModificationTime", DateTime.Now));
        cmd.Parameters.Add(new SqlParameter("@ModifiedBy", modifiedBy));
        cmd.Parameters.Add(new SqlParameter("@Reason", reason ?? (object)DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@Remark", remark ?? (object)DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@CreateTime", DateTime.Now));

        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 获取改刀记录列表
    /// </summary>
    public List<DieModificationRecord> GetModificationRecords(
        int? dieId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? dieCode = null)
    {
        var list = new List<DieModificationRecord>();

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        var sql = @"SELECT * FROM DieModificationRecord WHERE 1=1";
        var parameters = new List<SqlParameter>();

        if (dieId.HasValue)
        {
            sql += " AND DieID = @DieID";
            parameters.Add(new SqlParameter("@DieID", dieId.Value));
        }

        if (!string.IsNullOrEmpty(dieCode))
        {
            sql += " AND DieCode LIKE @DieCode";
            parameters.Add(new SqlParameter("@DieCode", $"%{dieCode}%"));
        }

        if (startDate.HasValue)
        {
            sql += " AND ModificationTime >= @StartDate";
            parameters.Add(new SqlParameter("@StartDate", startDate.Value));
        }

        if (endDate.HasValue)
        {
            sql += " AND ModificationTime <= @EndDate";
            parameters.Add(new SqlParameter("@EndDate", endDate.Value));
        }

        sql += " ORDER BY ModificationTime DESC";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var param in parameters)
        {
            cmd.Parameters.Add(param);
        }

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapModificationRecord(reader));
        }

        return list;
    }

    /// <summary>
    /// 获取刀模的改刀总金额
    /// </summary>
    public decimal GetTotalModificationAmount(int dieId)
    {
        if (dieId <= 0) return 0;

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        const string sql = @"
            SELECT ISNULL(SUM(ModificationAmount), 0) 
            FROM DieModificationRecord 
            WHERE DieID = @DieID";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@DieID", dieId));

        return Convert.ToDecimal(cmd.ExecuteScalar());
    }

    #endregion

    #region 自动入库功能

    /// <summary>
    /// 自动入库 - 当刀模所有工序完成时自动入库
    /// </summary>
    /// <param name="dieId">刀模ID</param>
    /// <param name="operatorNo">操作员工号</param>
    /// <param name="operatorName">操作员姓名</param>
    /// <returns>(是否成功, 消息)</returns>
    public (bool Success, string Message) AutoStockIn(int dieId, string operatorNo, string operatorName)
    {
        if (dieId <= 0)
            return (false, "刀模ID无效");

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        using var transaction = conn.BeginTransaction();
        try
        {
            // 1. 检查刀模是否存在
            var die = GetDieById(dieId);
            if (die == null)
                return (false, "刀模不存在");

            // 2. 检查刀模是否已入库
            const string checkInventorySql = @"
                SELECT COUNT(1) FROM DM_DieInventory 
                WHERE DieID = @DieID AND StorageStatus = @StorageStatus";
            
            using var checkCmd = conn.CreateCommand();
            checkCmd.Transaction = transaction;
            checkCmd.CommandText = checkInventorySql;
            checkCmd.Parameters.Add(new SqlParameter("@DieID", dieId));
            checkCmd.Parameters.Add(new SqlParameter("@StorageStatus", (int)StorageStatus.InStock));
            
            var existsCount = Convert.ToInt32(checkCmd.ExecuteScalar());
            if (existsCount > 0)
            {
                transaction.Commit();
                return (true, "刀模已入库，无需重复入库");
            }

            // 3. 查找空闲库位
            var locationResult = FindFreeLocation(conn, transaction);
            
            // 无空闲库位时，检查配置是否自动创建
            if (locationResult == null)
            {
                var configService = new SystemConfigService();
                var autoCreate = configService.GetBoolConfig("AutoCreateLocation", true);
                
                if (autoCreate)
                {
                    // 自动创建新库位
                    locationResult = CreateNewLocation(conn, transaction);
                }
                else
                {
                    transaction.Rollback();
                    return (false, "无空闲库位，请手动创建库位");
                }
            }
            
            int? locationId = locationResult?.LocationID;
            string? locationCode = locationResult?.LocationCode;

            // 4. 创建入库记录
            const string insertInventorySql = @"
                INSERT INTO DM_DieInventory 
                    (DieID, LocationID, StorageStatus, InStockTime, TotalBorrowCount, Remark, UpdateTime)
                VALUES 
                    (@DieID, @LocationID, @StorageStatus, @InStockTime, 0, @Remark, @UpdateTime);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            
            var inStockTime = DateTime.Now;
            var remark = $"自动入库 - 所有工序完成 - 操作人：{operatorName}({operatorNo})";
            
            using var insertCmd = conn.CreateCommand();
            insertCmd.Transaction = transaction;
            insertCmd.CommandText = insertInventorySql;
            insertCmd.Parameters.Add(new SqlParameter("@DieID", dieId));
            insertCmd.Parameters.Add(new SqlParameter("@LocationID", locationId ?? (object)DBNull.Value));
            insertCmd.Parameters.Add(new SqlParameter("@StorageStatus", (int)StorageStatus.InStock));
            insertCmd.Parameters.Add(new SqlParameter("@InStockTime", inStockTime));
            insertCmd.Parameters.Add(new SqlParameter("@Remark", remark));
            insertCmd.Parameters.Add(new SqlParameter("@UpdateTime", inStockTime));
            
            var inventoryId = Convert.ToInt32(insertCmd.ExecuteScalar());

            // 5. 如果有指定库位，更新库位状态为占用
            if (locationId.HasValue)
            {
                const string updateLocationSql = @"
                    UPDATE DM_StorageLocation 
                    SET Status = @Status 
                    WHERE LocationID = @LocationID";
                
                using var updateCmd = conn.CreateCommand();
                updateCmd.Transaction = transaction;
                updateCmd.CommandText = updateLocationSql;
                updateCmd.Parameters.Add(new SqlParameter("@Status", (int)LocationStatus.Occupied));
                updateCmd.Parameters.Add(new SqlParameter("@LocationID", locationId.Value));
                updateCmd.ExecuteNonQuery();
            }

            // 6. 更新刀模状态为已完成
            const string updateDieSql = @"
                UPDATE DieInfo SET
                    Status = @Status,
                    UpdateTime = @UpdateTime
                WHERE DieID = @DieID";
            
            using var updateDieCmd = conn.CreateCommand();
            updateDieCmd.Transaction = transaction;
            updateDieCmd.CommandText = updateDieSql;
            updateDieCmd.Parameters.Add(new SqlParameter("@DieID", dieId));
            updateDieCmd.Parameters.Add(new SqlParameter("@Status", (int)DieStatus.Completed));
            updateDieCmd.Parameters.Add(new SqlParameter("@UpdateTime", inStockTime));
            updateDieCmd.ExecuteNonQuery();

            transaction.Commit();

            var locationMsg = locationCode != null ? $"，库位：{locationCode}" : "（未分配库位）";
            return (true, $"刀模自动入库成功{locationMsg}");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new Exception($"自动入库失败：{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 检查刀模是否所有工序都已完成
    /// </summary>
    public bool AreAllProcessesCompleted(int dieId)
    {
        if (dieId <= 0) return false;

        var processes = GetDieProcesses(dieId);
        if (processes.Count == 0) return false;

        return processes.All(p => p.Status == ProcessStatus.Completed);
    }

    /// <summary>
    /// 获取刀模入库状态
    /// </summary>
    public (bool IsInStock, DateTime? InStockTime, string? LocationCode) GetDieStockStatus(int dieId)
    {
        if (dieId <= 0) return (false, null, null);

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        const string sql = @"
            SELECT i.InStockTime, l.LocationCode 
            FROM DM_DieInventory i
            LEFT JOIN DM_StorageLocation l ON i.LocationID = l.LocationID
            WHERE i.DieID = @DieID AND i.StorageStatus = @StorageStatus";
        
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@DieID", dieId));
        cmd.Parameters.Add(new SqlParameter("@StorageStatus", (int)StorageStatus.InStock));

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var inStockTime = reader["InStockTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["InStockTime"]);
            var locationCode = reader["LocationCode"] == DBNull.Value ? null : reader["LocationCode"].ToString();
            return (true, inStockTime, locationCode);
        }

        return (false, null, null);
    }

    #endregion

    #region 库位管理

    /// <summary>
    /// 查找空闲库位
    /// </summary>
    /// <param name="conn">数据库连接</param>
    /// <param name="transaction">事务</param>
    /// <returns>空闲库位信息，如果没有则返回null</returns>
    private (int LocationID, string LocationCode)? FindFreeLocation(IDbConnection conn, IDbTransaction transaction)
    {
        const string sql = @"
            SELECT TOP 1 LocationID, LocationCode 
            FROM DM_StorageLocation 
            WHERE Status = @Status
            ORDER BY Area, ShelfNo, LayerNo, PositionNo";
        
        using var cmd = conn.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@Status", (int)LocationStatus.Free));
        
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var locationId = Convert.ToInt32(reader["LocationID"]);
            var locationCode = reader["LocationCode"].ToString() ?? string.Empty;
            reader.Close();
            return (locationId, locationCode);
        }
        reader.Close();
        return null;
    }

    /// <summary>
    /// 自动创建新库位
    /// </summary>
    /// <param name="conn">数据库连接</param>
    /// <param name="transaction">事务</param>
    /// <returns>新创建的库位信息</returns>
    private (int LocationID, string LocationCode) CreateNewLocation(IDbConnection conn, IDbTransaction transaction)
    {
        // 获取默认区域、货架、层配置
        var configService = new SystemConfigService();
        var defaultArea = configService.GetConfig("DefaultLocationArea") ?? "A";
        var defaultShelf = configService.GetConfig("DefaultLocationShelf") ?? "01";
        var defaultLayer = configService.GetConfig("DefaultLocationLayer") ?? "01";

        // 查询该区域货架层下的最大位置号
        const string maxPositionSql = @"
            SELECT ISNULL(MAX(PositionNo), 0) + 1
            FROM DM_StorageLocation 
            WHERE Area = @Area AND ShelfNo = @ShelfNo AND LayerNo = @LayerNo";
        
        int newPositionNo;
        using var maxCmd = conn.CreateCommand();
        maxCmd.Transaction = transaction;
        maxCmd.CommandText = maxPositionSql;
        maxCmd.Parameters.Add(new SqlParameter("@Area", defaultArea));
        maxCmd.Parameters.Add(new SqlParameter("@ShelfNo", defaultShelf));
        maxCmd.Parameters.Add(new SqlParameter("@LayerNo", defaultLayer));
        
        newPositionNo = Convert.ToInt32(maxCmd.ExecuteScalar());

        // 生成新库位编码：A-01-01-06
        var newLocationCode = $"{defaultArea}-{defaultShelf}-{defaultLayer}-{newPositionNo:D2}";

        // 插入新库位
        const string insertSql = @"
            INSERT INTO DM_StorageLocation 
                (LocationCode, Area, ShelfNo, LayerNo, PositionNo, Status, CreateTime)
            VALUES 
                (@LocationCode, @Area, @ShelfNo, @LayerNo, @PositionNo, @Status, @CreateTime);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        
        using var insertCmd = conn.CreateCommand();
        insertCmd.Transaction = transaction;
        insertCmd.CommandText = insertSql;
        insertCmd.Parameters.Add(new SqlParameter("@LocationCode", newLocationCode));
        insertCmd.Parameters.Add(new SqlParameter("@Area", defaultArea));
        insertCmd.Parameters.Add(new SqlParameter("@ShelfNo", defaultShelf));
        insertCmd.Parameters.Add(new SqlParameter("@LayerNo", defaultLayer));
        insertCmd.Parameters.Add(new SqlParameter("@PositionNo", newPositionNo));
        insertCmd.Parameters.Add(new SqlParameter("@Status", (int)LocationStatus.Free));
        insertCmd.Parameters.Add(new SqlParameter("@CreateTime", DateTime.Now));
        
        var newLocationId = Convert.ToInt32(insertCmd.ExecuteScalar());
        
        return (newLocationId, newLocationCode);
    }

    #endregion

    #region 导入功能

    /// <summary>
    /// 检查外部订单ID是否已存在
    /// </summary>
    public bool IsExternalOrderExists(string externalOrderId)
    {
        if (string.IsNullOrEmpty(externalOrderId)) return false;

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        const string sql = "SELECT COUNT(1) FROM DieInfo WHERE ExternalOrderID = @ExternalOrderID";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@ExternalOrderID", externalOrderId));

        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    /// <summary>
    /// 批量导入刀模
    /// </summary>
    public int BatchImportDies(List<DieInfo> dies, string createUser)
    {
        if (dies == null || dies.Count == 0 || string.IsNullOrEmpty(createUser))
            return 0;

        int importedCount = 0;

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        foreach (var die in dies)
        {
            // 检查是否已存在
            if (!string.IsNullOrEmpty(die.ExternalOrderNo) && IsExternalOrderExists(die.ExternalOrderNo))
                continue;

            // 生成刀模编号
            if (string.IsNullOrEmpty(die.DieCode))
            {
                die.DieCode = GenerateDieCode(conn);
            }

            die.CreateUser = createUser;
            die.CreateTime = DateTime.Now;
            die.Status = DieStatus.Pending;
            die.AuditStatus = AuditStatus.Unaudited;

            var dieId = CreateDie(die, die.Processes);
            if (dieId > 0)
                importedCount++;
        }

        return importedCount;
    }

    /// <summary>
    /// 生成刀模编号
    /// </summary>
    private string GenerateDieCode(IDbConnection conn)
    {
        const string sql = @"
            SELECT ISNULL(MAX(CAST(SUBSTRING(DieCode, 3, LEN(DieCode) - 2) AS INT)), 0) + 1
            FROM DieInfo 
            WHERE DieCode LIKE 'DM%' AND LEN(DieCode) >= 3";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var nextId = Convert.ToInt32(cmd.ExecuteScalar());

        return $"DM{DateTime.Now.Year}{nextId:D4}";
    }

    #endregion

    #region 辅助方法

    private void AddDieParameters(IDbCommand cmd, DieInfo die)
    {
        cmd.Parameters.Add(new SqlParameter("@DieCode", die.DieCode));
        cmd.Parameters.Add(new SqlParameter("@WorkOrderNo", die.WorkOrderNo ?? (object)DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@CustomerName", die.CustomerName));
        cmd.Parameters.Add(new SqlParameter("@ProductName", die.ProductName));
        cmd.Parameters.Add(new SqlParameter("@Structure", die.Structure));
        cmd.Parameters.Add(new SqlParameter("@ModelType", die.ModelType));
        cmd.Parameters.Add(new SqlParameter("@LayoutType", die.LayoutType));
        cmd.Parameters.Add(new SqlParameter("@FluteType", die.FluteType));
        cmd.Parameters.Add(new SqlParameter("@Material", die.Material));
        cmd.Parameters.Add(new SqlParameter("@ManufactureLength", die.ManufactureLength));
        cmd.Parameters.Add(new SqlParameter("@ManufactureWidth", die.ManufactureWidth));
        cmd.Parameters.Add(new SqlParameter("@ManufactureHeight", die.ManufactureHeight));
        cmd.Parameters.Add(new SqlParameter("@BlankLength", die.BlankLength));
        cmd.Parameters.Add(new SqlParameter("@BlankWidth", die.BlankWidth));
        cmd.Parameters.Add(new SqlParameter("@KnifeLengthM", die.KnifeLengthM ?? (object)DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@KnifeMarkLengthM", die.KnifeMarkLengthM ?? (object)DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@BoardFeeUnitPrice", die.BoardFeeUnitPrice));
        cmd.Parameters.Add(new SqlParameter("@BoardFee", die.BoardFee ?? (object)DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@ProductionUnitPrice", die.ProductionUnitPrice));
        cmd.Parameters.Add(new SqlParameter("@ProductionFee", die.ProductionFee ?? (object)DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@DesignUnitPrice", die.DesignUnitPrice));
        cmd.Parameters.Add(new SqlParameter("@DesignFee", die.DesignFee ?? (object)DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@ProcessDesc", die.ProcessDesc));
        cmd.Parameters.Add(new SqlParameter("@RequiredProcesses", die.RequiredProcesses));
        cmd.Parameters.Add(new SqlParameter("@Status", (int)die.Status));
        cmd.Parameters.Add(new SqlParameter("@AuditStatus", (int)die.AuditStatus));
        cmd.Parameters.Add(new SqlParameter("@SourceFactory", die.SourceFactory));
        cmd.Parameters.Add(new SqlParameter("@ExternalOrderID", die.ExternalOrderID ?? (object)DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@DeliveryDate", die.DeliveryDate ?? (object)DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@CreateTime", die.CreateTime));
        cmd.Parameters.Add(new SqlParameter("@CreateUser", die.CreateUser));
        cmd.Parameters.Add(new SqlParameter("@Remark", die.Remark));
    }

    private void InsertProcess(IDbConnection conn, IDbTransaction transaction, DieProcess process)
    {
        const string sql = @"
            INSERT INTO DieProcess (DieID, ProcessName, Status, OperatorNo, OperatorName, 
                BoardLength, BoardWidth, KnifeLength, KnifeTraceLength, Formula, Amount, 
                PrevProcessID, IsPrevCompleted, CreateTime)
            VALUES (@DieID, @ProcessName, @Status, @OperatorNo, @OperatorName, 
                @BoardLength, @BoardWidth, @KnifeLength, @KnifeTraceLength, @Formula, @Amount, 
                @PrevProcessID, @IsPrevCompleted, @CreateTime)";

        using var cmd = conn.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@DieID", process.DieID));
        cmd.Parameters.Add(new SqlParameter("@ProcessName", process.ProcessName));
        cmd.Parameters.Add(new SqlParameter("@Status", (int)process.Status));
        cmd.Parameters.Add(new SqlParameter("@OperatorNo", process.OperatorNo));
        cmd.Parameters.Add(new SqlParameter("@OperatorName", process.OperatorName));
        cmd.Parameters.Add(new SqlParameter("@BoardLength", process.BoardLength));
        cmd.Parameters.Add(new SqlParameter("@BoardWidth", process.BoardWidth));
        cmd.Parameters.Add(new SqlParameter("@KnifeLength", process.KnifeLength));
        cmd.Parameters.Add(new SqlParameter("@KnifeTraceLength", process.KnifeTraceLength));
        cmd.Parameters.Add(new SqlParameter("@Formula", process.Formula));
        cmd.Parameters.Add(new SqlParameter("@Amount", process.Amount));
        cmd.Parameters.Add(new SqlParameter("@PrevProcessID", process.PrevProcessID ?? (object)DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@IsPrevCompleted", process.IsPrevCompleted));
        cmd.Parameters.Add(new SqlParameter("@CreateTime", DateTime.Now));

        cmd.ExecuteNonQuery();
    }

    private DieInfo MapDieInfo(IDataReader reader)
    {
        return new DieInfo
        {
            DieID = Convert.ToInt32(reader["DieID"]),
            DieCode = reader["DieCode"].ToString() ?? string.Empty,
            WorkOrderNo = reader["WorkOrderNo"] == DBNull.Value ? null : reader["WorkOrderNo"].ToString(),
            CustomerName = reader["CustomerName"].ToString() ?? string.Empty,
            ProductName = reader["ProductName"].ToString() ?? string.Empty,
            Structure = reader["Structure"].ToString() ?? string.Empty,
            ModelType = reader["ModelType"].ToString() ?? string.Empty,
            LayoutType = reader["LayoutType"].ToString() ?? string.Empty,
            FluteType = reader["FluteType"].ToString() ?? string.Empty,
            Material = reader["Material"].ToString() ?? string.Empty,
            ManufactureLength = reader["ManufactureLength"] == DBNull.Value ? null : Convert.ToDecimal(reader["ManufactureLength"]),
            ManufactureWidth = reader["ManufactureWidth"] == DBNull.Value ? null : Convert.ToDecimal(reader["ManufactureWidth"]),
            ManufactureHeight = reader["ManufactureHeight"] == DBNull.Value ? null : Convert.ToDecimal(reader["ManufactureHeight"]),
            BlankLength = reader["BlankLength"] == DBNull.Value ? null : Convert.ToDecimal(reader["BlankLength"]),
            BlankWidth = reader["BlankWidth"] == DBNull.Value ? null : Convert.ToDecimal(reader["BlankWidth"]),
            KnifeLengthM = reader["KnifeLengthM"] == DBNull.Value ? null : Convert.ToDecimal(reader["KnifeLengthM"]),
            KnifeMarkLengthM = reader["KnifeMarkLengthM"] == DBNull.Value ? null : Convert.ToDecimal(reader["KnifeMarkLengthM"]),
            BoardFeeUnitPrice = reader["BoardFeeUnitPrice"] == DBNull.Value ? 90m : Convert.ToDecimal(reader["BoardFeeUnitPrice"]),
            BoardFee = reader["BoardFee"] == DBNull.Value ? null : Convert.ToDecimal(reader["BoardFee"]),
            ProductionUnitPrice = reader["ProductionUnitPrice"] == DBNull.Value ? 8m : Convert.ToDecimal(reader["ProductionUnitPrice"]),
            ProductionFee = reader["ProductionFee"] == DBNull.Value ? null : Convert.ToDecimal(reader["ProductionFee"]),
            DesignUnitPrice = reader["DesignUnitPrice"] == DBNull.Value ? 70m : Convert.ToDecimal(reader["DesignUnitPrice"]),
            DesignFee = reader["DesignFee"] == DBNull.Value ? null : Convert.ToDecimal(reader["DesignFee"]),
            ProcessDesc = reader["ProcessDesc"].ToString() ?? string.Empty,
            RequiredProcesses = reader["RequiredProcesses"].ToString() ?? string.Empty,
            Status = (DieStatus)Convert.ToInt32(reader["Status"]),
            AuditStatus = (AuditStatus)Convert.ToInt32(reader["AuditStatus"]),
            SourceFactory = reader["SourceFactory"].ToString() ?? string.Empty,
            ExternalOrderID = reader["ExternalOrderID"] == DBNull.Value ? null : Convert.ToInt32(reader["ExternalOrderID"]),
            ExternalOrderNo = reader["ExternalOrderNo"] == DBNull.Value ? null : reader["ExternalOrderNo"].ToString(),
            DeliveryDate = reader["DeliveryDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["DeliveryDate"]),
            CreateTime = Convert.ToDateTime(reader["CreateTime"]),
            CreateUser = reader["CreateUser"].ToString() ?? string.Empty,
            UpdateTime = reader["UpdateTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdateTime"]),
            Remark = reader["Remark"].ToString() ?? string.Empty
        };
    }

    private DieProcess MapDieProcess(IDataReader reader)
    {
        return new DieProcess
        {
            ProcessID = Convert.ToInt32(reader["ProcessID"]),
            DieID = Convert.ToInt32(reader["DieID"]),
            ProcessName = reader["ProcessName"].ToString() ?? string.Empty,
            Status = (ProcessStatus)Convert.ToInt32(reader["Status"]),
            StartTime = reader["StartTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["StartTime"]),
            CompleteTime = reader["CompleteTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["CompleteTime"]),
            OperatorNo = reader["OperatorNo"].ToString() ?? string.Empty,
            OperatorName = reader["OperatorName"].ToString() ?? string.Empty,
            BoardLength = Convert.ToDecimal(reader["BoardLength"]),
            BoardWidth = Convert.ToDecimal(reader["BoardWidth"]),
            KnifeLength = Convert.ToDecimal(reader["KnifeLength"]),
            KnifeTraceLength = Convert.ToDecimal(reader["KnifeTraceLength"]),
            Formula = reader["Formula"].ToString() ?? string.Empty,
            Amount = Convert.ToDecimal(reader["Amount"]),
            PrevProcessID = reader["PrevProcessID"] == DBNull.Value ? null : Convert.ToInt32(reader["PrevProcessID"]),
            IsPrevCompleted = Convert.ToBoolean(reader["IsPrevCompleted"]),
            CreateTime = Convert.ToDateTime(reader["CreateTime"])
        };
    }

    private DieModificationRecord MapModificationRecord(IDataReader reader)
    {
        return new DieModificationRecord
        {
            ModificationID = Convert.ToInt32(reader["ModificationID"]),
            DieID = Convert.ToInt32(reader["DieID"]),
            DieCode = reader["DieCode"].ToString() ?? string.Empty,
            CustomerName = reader["CustomerName"].ToString() ?? string.Empty,
            ProductName = reader["ProductName"].ToString() ?? string.Empty,
            ModificationAmount = Convert.ToDecimal(reader["ModificationAmount"]),
            ModificationTime = Convert.ToDateTime(reader["ModificationTime"]),
            ModifiedBy = reader["ModifiedBy"].ToString() ?? string.Empty,
            Reason = reader["Reason"] == DBNull.Value ? string.Empty : reader["Reason"].ToString()!,
            Remark = reader["Remark"] == DBNull.Value ? string.Empty : reader["Remark"].ToString()!,
            CreateTime = Convert.ToDateTime(reader["CreateTime"])
        };
    }

    #endregion
}
