using System.Data;
using DieMaking.Helpers;
using DieMaking.Models;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

/// <summary>
/// 工序服务类
/// </summary>
public class ProcessService
{
    private readonly DieService _dieService;

    public ProcessService()
    {
        _dieService = new DieService();
    }

    public ProcessService(DieService dieService)
    {
        _dieService = dieService;
    }

    /// <summary>
    /// 获取默认工序列表
    /// </summary>
    public List<DieProcess> GetDefaultProcesses()
    {
        return new List<DieProcess>
        {
            new DieProcess { ProcessName = "绘图", Status = ProcessStatus.Pending },
            new DieProcess { ProcessName = "割板", Status = ProcessStatus.Pending },
            new DieProcess { ProcessName = "弯刀", Status = ProcessStatus.Pending },
            new DieProcess { ProcessName = "装刀", Status = ProcessStatus.Pending },
            new DieProcess { ProcessName = "贴泡沫", Status = ProcessStatus.Pending }
        };
    }

    /// <summary>
    /// 为刀模创建默认工序
    /// </summary>
    public bool CreateDefaultProcesses(int dieId)
    {
        var processes = GetDefaultProcesses();
        
        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        using var transaction = conn.BeginTransaction();
        try
        {
            foreach (var process in processes)
            {
                process.DieID = dieId;
                process.CreateTime = DateTime.Now;
                InsertProcess(conn, transaction, process);
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 通过扫码完成工序
    /// </summary>
    public (bool Success, string Message) CompleteProcessByScan(string workOrderNo, string processName, string operatorNo, string operatorName)
    {
        try
        {
            // 1. 查找刀模
            var die = FindDieByWorkOrderNo(workOrderNo);
            if (die == null)
            {
                return (false, $"未找到工单号 {workOrderNo} 对应的刀模");
            }

            // 2. 查找工序
            var processes = _dieService.GetDieProcesses(die.DieID);
            var process = processes.FirstOrDefault(p => p.ProcessName == processName);
            
            if (process == null)
            {
                return (false, $"刀模 {die.DieCode} 未找到工序 {processName}");
            }

            // 3. 更新工序状态为已完成（简化流程，直接完成）
            if (process.Status == ProcessStatus.Completed)
            {
                return (false, $"工序 {processName} 已完成，无需重复报产");
            }

            var success = UpdateProcessStatus(process.ProcessID, ProcessStatus.Completed, operatorNo, operatorName);
            
            if (success)
            {
                // 记录扫码报工
                RecordScanReport(workOrderNo, die.DieID, process.ProcessID, processName, operatorNo, operatorName);
                
                // 4. 检查是否所有工序都已完成，如果是则自动入库
                var autoStockInResult = CheckAndAutoStockIn(die.DieID, operatorNo, operatorName);
                if (autoStockInResult.Success)
                {
                    return (true, $"工序 {processName} 报产成功，{autoStockInResult.Message}");
                }
                
                return (true, $"工序 {processName} 报产成功");
            }

            return (false, "更新工序状态失败");
        }
        catch (Exception ex)
        {
            return (false, $"报产失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 检查刀模所有工序是否完成，如果完成则自动入库
    /// </summary>
    private (bool Success, string Message) CheckAndAutoStockIn(int dieId, string operatorNo, string operatorName)
    {
        try
        {
            // 获取刀模的所有工序
            var processes = _dieService.GetDieProcesses(dieId);
            
            // 检查是否所有工序都已完成
            var allCompleted = processes.All(p => p.Status == ProcessStatus.Completed);
            
            if (!allCompleted)
            {
                // 不是所有工序都完成，无需自动入库
                var completedCount = processes.Count(p => p.Status == ProcessStatus.Completed);
                var totalCount = processes.Count;
                return (false, $"已完成 {completedCount}/{totalCount} 道工序");
            }
            
            // 所有工序都已完成，执行自动入库
            var result = _dieService.AutoStockIn(dieId, operatorNo, operatorName);
            return result;
        }
        catch (Exception ex)
        {
            // 自动入库失败不影响工序报工，记录错误但不抛出
            return (false, $"自动入库检查失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 扫码获取刀模工序信息
    /// </summary>
    public (bool Success, DieInfo? Die, List<DieProcess>? Processes, string Message) GetDieInfoByScan(string workOrderNo)
    {
        try
        {
            var die = FindDieByWorkOrderNo(workOrderNo);
            if (die == null)
            {
                return (false, null, null, $"未找到工单号 {workOrderNo} 对应的刀模");
            }

            var processes = _dieService.GetDieProcesses(die.DieID);
            return (true, die, processes, "查询成功");
        }
        catch (Exception ex)
        {
            return (false, null, null, $"查询失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 根据工单号查找刀模
    /// </summary>
    private DieInfo? FindDieByWorkOrderNo(string workOrderNo)
    {
        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        const string sql = "SELECT TOP 1 * FROM DieInfo WHERE WorkOrderNo = @WorkOrderNo ORDER BY CreateTime DESC";
        
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@WorkOrderNo", workOrderNo));

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapDieInfo(reader);
        }

        return null;
    }

    /// <summary>
    /// 更新工序状态（简化版：只有待生产和已完成两种状态）
    /// </summary>
    public bool UpdateProcessStatus(int processId, ProcessStatus status, string? operatorNo = null, string? operatorName = null)
    {
        if (processId <= 0) return false;

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        var sql = @"
            UPDATE DieProcess SET
                Status = @Status";

        // 简化流程：直接完成时记录完成时间
        if (status == ProcessStatus.Completed)
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

        if (status == ProcessStatus.Completed)
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

    /// <summary>
    /// 记录扫码报工
    /// </summary>
    private void RecordScanReport(string workOrderNo, int dieId, int processId, string processName, 
        string operatorNo, string operatorName)
    {
        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        const string sql = @"
            INSERT INTO ScanReportRecord 
                (WorkOrderNo, DieID, ProcessID, ProcessName, ScanTime, 
                 ReportType, OperatorNo, OperatorName)
            VALUES 
                (@WorkOrderNo, @DieID, @ProcessID, @ProcessName, GETDATE(), 
                 0, @OperatorNo, @OperatorName)";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@WorkOrderNo", workOrderNo));
        cmd.Parameters.Add(new SqlParameter("@DieID", dieId));
        cmd.Parameters.Add(new SqlParameter("@ProcessID", processId));
        cmd.Parameters.Add(new SqlParameter("@ProcessName", processName));
        cmd.Parameters.Add(new SqlParameter("@OperatorNo", operatorNo));
        cmd.Parameters.Add(new SqlParameter("@OperatorName", operatorName));

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 插入工序
    /// </summary>
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
        cmd.Parameters.Add(new SqlParameter("@CreateTime", process.CreateTime));

        cmd.ExecuteNonQuery();
    }

    private DieInfo MapDieInfo(IDataReader reader)
    {
        return new DieInfo
        {
            DieID = Convert.ToInt32(reader["DieID"]),
            DieCode = reader["DieCode"].ToString() ?? string.Empty,
            CustomerName = reader["CustomerName"].ToString() ?? string.Empty,
            ProductName = reader["ProductName"].ToString() ?? string.Empty,
            Structure = reader["Structure"].ToString() ?? string.Empty,
            ModelType = reader["ModelType"].ToString() ?? string.Empty,
            LayoutType = reader["LayoutType"].ToString() ?? string.Empty,
            FluteType = reader["FluteType"].ToString() ?? string.Empty,
            Material = reader["Material"].ToString() ?? string.Empty,
            ManufactureLength = Convert.ToDecimal(reader["ManufactureLength"]),
            ManufactureWidth = Convert.ToDecimal(reader["ManufactureWidth"]),
            ManufactureHeight = Convert.ToDecimal(reader["ManufactureHeight"]),
            BlankLength = Convert.ToDecimal(reader["BlankLength"]),
            BlankWidth = Convert.ToDecimal(reader["BlankWidth"]),
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
}
