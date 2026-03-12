using System.Data;
using DieMaking.Data;
using DieMaking.Models;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

/// <summary>
/// 工序报产服务（支持手机扫码）
/// </summary>
public class ProcessReportService
{
    private readonly DieService _dieService;

    public ProcessReportService()
    {
        _dieService = new DieService();
    }

    public ProcessReportService(DieService dieService)
    {
        _dieService = dieService;
    }

    /// <summary>
    /// 扫码报工 - 根据工单号查找刀模并完成工序
    /// </summary>
    public ScanReportResult ScanAndReport(string workOrderNo, string processName, string operatorNo, string operatorName)
    {
        var result = new ScanReportResult
        {
            WorkOrderNo = workOrderNo,
            ProcessName = processName,
            ScanTime = DateTime.Now,
            OperatorNo = operatorNo,
            OperatorName = operatorName
        };

        try
        {
            // 1. 根据工单号查找刀模
            var die = FindDieByWorkOrderNo(workOrderNo);
            if (die == null)
            {
                result.Status = 1;
                result.ErrorMessage = $"未找到工单号为 {workOrderNo} 的刀模";
                SaveScanRecord(result);
                return result;
            }

            result.DieID = die.DieID;
            result.DieCode = die.DieCode;

            // 2. 查找对应工序
            var processes = _dieService.GetDieProcesses(die.DieID);
            var process = processes.FirstOrDefault(p => p.ProcessName == processName);
            
            if (process == null)
            {
                result.Status = 1;
                result.ErrorMessage = $"刀模 {die.DieCode} 不存在工序 {processName}";
                SaveScanRecord(result);
                return result;
            }

            result.ProcessID = process.ProcessID;

            // 3. 检查工序状态
            if (process.Status == ProcessStatus.Completed)
            {
                result.Status = 1;
                result.ErrorMessage = $"工序 {processName} 已完成，无需重复报工";
                SaveScanRecord(result);
                return result;
            }

            // 4. 完成工序（简化流程：直接完成，无需开始）
            var success = _dieService.UpdateProcessStatus(
                process.ProcessID, 
                ProcessStatus.Completed, 
                operatorNo, 
                operatorName);

            if (success)
            {
                result.Status = 0;
                result.ErrorMessage = "报工成功";
                
                // 检查是否所有工序都已完成
                CheckAndUpdateDieStatus(die.DieID);
            }
            else
            {
                result.Status = 1;
                result.ErrorMessage = "更新工序状态失败";
            }
        }
        catch (Exception ex)
        {
            result.Status = 1;
            result.ErrorMessage = $"报工异常: {ex.Message}";
        }

        SaveScanRecord(result);
        return result;
    }

    /// <summary>
    /// 根据工单号查找刀模
    /// </summary>
    private DieInfo? FindDieByWorkOrderNo(string workOrderNo)
    {
        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        const string sql = "SELECT * FROM DieInfo WHERE WorkOrderNo = @WorkOrderNo OR ExternalOrderID = @WorkOrderNo";
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
    /// 检查并更新刀模状态
    /// </summary>
    private void CheckAndUpdateDieStatus(int dieId)
    {
        var processes = _dieService.GetDieProcesses(dieId);
        var allCompleted = processes.All(p => p.Status == ProcessStatus.Completed);
        
        if (allCompleted && processes.Count > 0)
        {
            using var conn = DatabaseConfig.CreateConnection();
            conn.Open();

            const string sql = @"
                UPDATE DieInfo 
                SET Status = @Status, UpdateTime = @UpdateTime 
                WHERE DieID = @DieID";

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new SqlParameter("@DieID", dieId));
            cmd.Parameters.Add(new SqlParameter("@Status", (int)DieStatus.Completed));
            cmd.Parameters.Add(new SqlParameter("@UpdateTime", DateTime.Now));
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 保存扫码记录
    /// </summary>
    private void SaveScanRecord(ScanReportResult result)
    {
        try
        {
            using var conn = DatabaseConfig.CreateConnection();
            conn.Open();

            const string sql = @"
                INSERT INTO ScanReportRecord 
                    (WorkOrderNo, DieID, ProcessID, ProcessName, ScanTime, OperatorNo, OperatorName, Status, ErrorMessage, CreateTime)
                VALUES 
                    (@WorkOrderNo, @DieID, @ProcessID, @ProcessName, @ScanTime, @OperatorNo, @OperatorName, @Status, @ErrorMessage, @CreateTime)";

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new SqlParameter("@WorkOrderNo", result.WorkOrderNo));
            cmd.Parameters.Add(new SqlParameter("@DieID", result.DieID ?? (object)DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@ProcessID", result.ProcessID ?? (object)DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@ProcessName", result.ProcessName));
            cmd.Parameters.Add(new SqlParameter("@ScanTime", result.ScanTime));
            cmd.Parameters.Add(new SqlParameter("@OperatorNo", result.OperatorNo));
            cmd.Parameters.Add(new SqlParameter("@OperatorName", result.OperatorName));
            cmd.Parameters.Add(new SqlParameter("@Status", result.Status));
            cmd.Parameters.Add(new SqlParameter("@ErrorMessage", result.ErrorMessage ?? (object)DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@CreateTime", DateTime.Now));

            cmd.ExecuteNonQuery();
        }
        catch
        {
            // 记录失败不影响主流程
        }
    }

    /// <summary>
    /// 获取扫码记录列表
    /// </summary>
    public List<ScanReportRecord> GetScanRecords(DateTime? startDate = null, DateTime? endDate = null, string? workOrderNo = null)
    {
        var list = new List<ScanReportRecord>();

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        var sql = "SELECT * FROM ScanReportRecord WHERE 1=1";
        var parameters = new List<SqlParameter>();

        if (!string.IsNullOrEmpty(workOrderNo))
        {
            sql += " AND WorkOrderNo = @WorkOrderNo";
            parameters.Add(new SqlParameter("@WorkOrderNo", workOrderNo));
        }

        if (startDate.HasValue)
        {
            sql += " AND ScanTime >= @StartDate";
            parameters.Add(new SqlParameter("@StartDate", startDate.Value));
        }

        if (endDate.HasValue)
        {
            sql += " AND ScanTime <= @EndDate";
            parameters.Add(new SqlParameter("@EndDate", endDate.Value));
        }

        sql += " ORDER BY ScanTime DESC";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var param in parameters)
        {
            cmd.Parameters.Add(param);
        }

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ScanReportRecord
            {
                RecordID = Convert.ToInt32(reader["RecordID"]),
                WorkOrderNo = reader["WorkOrderNo"].ToString() ?? string.Empty,
                DieID = reader["DieID"] == DBNull.Value ? null : Convert.ToInt32(reader["DieID"]),
                ProcessID = reader["ProcessID"] == DBNull.Value ? null : Convert.ToInt32(reader["ProcessID"]),
                ProcessName = reader["ProcessName"].ToString() ?? string.Empty,
                ScanTime = Convert.ToDateTime(reader["ScanTime"]),
                OperatorNo = reader["OperatorNo"].ToString() ?? string.Empty,
                OperatorName = reader["OperatorName"].ToString() ?? string.Empty,
                DeviceInfo = reader["DeviceInfo"] == DBNull.Value ? null : reader["DeviceInfo"].ToString(),
                Status = Convert.ToInt32(reader["Status"]),
                ErrorMessage = reader["ErrorMessage"] == DBNull.Value ? null : reader["ErrorMessage"].ToString(),
                CreateTime = Convert.ToDateTime(reader["CreateTime"])
            });
        }

        return list;
    }

    private DieInfo MapDieInfo(IDataReader reader)
    {
        return new DieInfo
        {
            DieID = Convert.ToInt32(reader["DieID"]),
            DieCode = reader["DieCode"].ToString() ?? string.Empty,
            CustomerName = reader["CustomerName"].ToString() ?? string.Empty,
            ProductName = reader["ProductName"].ToString() ?? string.Empty,
            // ... 其他字段
        };
    }
}

/// <summary>
/// 扫码报工结果
/// </summary>
public class ScanReportResult
{
    public string WorkOrderNo { get; set; } = string.Empty;
    public int? DieID { get; set; }
    public string? DieCode { get; set; }
    public int? ProcessID { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public DateTime ScanTime { get; set; }
    public string OperatorNo { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public int Status { get; set; } // 0:成功, 1:失败
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 扫码报工记录
/// </summary>
public class ScanReportRecord
{
    public int RecordID { get; set; }
    public string WorkOrderNo { get; set; } = string.Empty;
    public int? DieID { get; set; }
    public int? ProcessID { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public DateTime ScanTime { get; set; }
    public string OperatorNo { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
    public int Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreateTime { get; set; }
}
