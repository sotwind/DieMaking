using DieMaking.Helpers;
using DieMaking.Models;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

public class ProductionService
{
    #region 生产看板相关

    /// <summary>
    /// 获取生产看板数据 - 按状态分组
    /// </summary>
    public ProductionBoardData GetProductionBoardData(DateTime? startDate, DateTime? endDate, string? customerName, string? dieCode)
    {
        try
        {
            var data = new ProductionBoardData();

            // 获取待生产刀模
            data.PendingList = GetDieListByStatus(DieStatus.Pending, startDate, endDate, customerName, dieCode);

            // 获取生产中刀模
            data.InProgressList = GetDieListByStatus(DieStatus.InProgress, startDate, endDate, customerName, dieCode);

            // 获取已完成刀模
            data.CompletedList = GetDieListByStatus(DieStatus.Completed, startDate, endDate, customerName, dieCode);

            // 获取统计信息
            data.Statistics = GetProductionStatistics(startDate, endDate, customerName, dieCode);

            return data;
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "获取生产看板数据");
            return new ProductionBoardData();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取生产看板数据");
            return new ProductionBoardData();
        }
    }

    /// <summary>
    /// 根据状态获取刀模列表（使用优化查询）
    /// </summary>
    private List<DieBoardItem> GetDieListByStatus(DieStatus status, DateTime? startDate, DateTime? endDate, string? customerName, string? dieCode, int pageIndex = 1, int pageSize = 100)
    {
        try
        {
            // 使用优化的查询，减少子查询
            var baseSql = @"SELECT d.DieID, d.DieCode, d.CustomerName, d.ProductName, d.DeliveryDate,
                                d.Status, d.CreateTime,
                                p.TotalProcesses,
                                p.CompletedProcesses
                         FROM DM_DieInfo d WITH (NOLOCK)
                         LEFT JOIN (
                             SELECT DieID,
                                    COUNT(*) as TotalProcesses,
                                    SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) as CompletedProcesses
                             FROM DM_DieProcess WITH (NOLOCK)
                             GROUP BY DieID
                         ) p ON d.DieID = p.DieID
                         WHERE d.Status = @Status";

            var parameters = new List<SqlParameter> { new SqlParameter("@Status", (int)status) };

            if (startDate.HasValue)
            {
                baseSql += " AND d.CreateTime >= @StartDate";
                parameters.Add(new SqlParameter("@StartDate", startDate.Value));
            }

            if (endDate.HasValue)
            {
                baseSql += " AND d.CreateTime <= @EndDate";
                parameters.Add(new SqlParameter("@EndDate", endDate.Value.AddDays(1).AddSeconds(-1)));
            }

            if (!string.IsNullOrEmpty(customerName))
            {
                baseSql += " AND d.CustomerName LIKE @CustomerName";
                parameters.Add(new SqlParameter("@CustomerName", $"%{customerName}%"));
            }

            if (!string.IsNullOrEmpty(dieCode))
            {
                baseSql += " AND d.DieCode LIKE @DieCode";
                parameters.Add(new SqlParameter("@DieCode", $"%{dieCode}%"));
            }

            // 使用分页查询
            if (pageSize > 0)
            {
                var pagedResult = DbHelper.ExecutePagedQuery(baseSql, "CreateTime DESC", pageIndex, pageSize,
                    reader => new DieBoardItem
                    {
                        DieID = Convert.ToInt32(reader["DieID"]),
                        DieCode = reader["DieCode"].ToString() ?? "",
                        CustomerName = reader["CustomerName"].ToString() ?? "",
                        ProductName = reader["ProductName"].ToString() ?? "",
                        DeliveryDate = reader["DeliveryDate"] != DBNull.Value ? Convert.ToDateTime(reader["DeliveryDate"]) : null,
                        Status = (DieStatus)Convert.ToInt32(reader["Status"]),
                        CreateTime = Convert.ToDateTime(reader["CreateTime"]),
                        TotalProcesses = reader["TotalProcesses"] != DBNull.Value ? Convert.ToInt32(reader["TotalProcesses"]) : 0,
                        CompletedProcesses = reader["CompletedProcesses"] != DBNull.Value ? Convert.ToInt32(reader["CompletedProcesses"]) : 0
                    }, parameters.ToArray());

                return pagedResult.Items;
            }
            else
            {
                var sql = baseSql + " ORDER BY d.CreateTime DESC";
                return DbHelper.ExecuteQuery(sql, reader => new DieBoardItem
                {
                    DieID = Convert.ToInt32(reader["DieID"]),
                    DieCode = reader["DieCode"].ToString() ?? "",
                    CustomerName = reader["CustomerName"].ToString() ?? "",
                    ProductName = reader["ProductName"].ToString() ?? "",
                    DeliveryDate = reader["DeliveryDate"] != DBNull.Value ? Convert.ToDateTime(reader["DeliveryDate"]) : null,
                    Status = (DieStatus)Convert.ToInt32(reader["Status"]),
                    CreateTime = Convert.ToDateTime(reader["CreateTime"]),
                    TotalProcesses = reader["TotalProcesses"] != DBNull.Value ? Convert.ToInt32(reader["TotalProcesses"]) : 0,
                    CompletedProcesses = reader["CompletedProcesses"] != DBNull.Value ? Convert.ToInt32(reader["CompletedProcesses"]) : 0
                }, parameters.ToArray());
            }
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, $"获取刀模列表(Status:{status})");
            return new List<DieBoardItem>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, $"获取刀模列表(Status:{status})");
            return new List<DieBoardItem>();
        }
    }

    /// <summary>
    /// 获取生产统计信息
    /// </summary>
    private ProductionStatistics GetProductionStatistics(DateTime? startDate, DateTime? endDate, string? customerName, string? dieCode)
    {
        try
        {
            var sql = @"SELECT 
                            SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) as PendingCount,
                            SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) as InProgressCount,
                            SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) as CompletedCount,
                            COUNT(*) as TotalCount
                         FROM DM_DieInfo d
                         WHERE 1=1";

            var parameters = new List<SqlParameter>();

            if (startDate.HasValue)
            {
                sql += " AND d.CreateTime >= @StartDate";
                parameters.Add(new SqlParameter("@StartDate", startDate.Value));
            }

            if (endDate.HasValue)
            {
                sql += " AND d.CreateTime <= @EndDate";
                parameters.Add(new SqlParameter("@EndDate", endDate.Value.AddDays(1).AddSeconds(-1)));
            }

            if (!string.IsNullOrEmpty(customerName))
            {
                sql += " AND d.CustomerName LIKE @CustomerName";
                parameters.Add(new SqlParameter("@CustomerName", $"%{customerName}%"));
            }

            if (!string.IsNullOrEmpty(dieCode))
            {
                sql += " AND d.DieCode LIKE @DieCode";
                parameters.Add(new SqlParameter("@DieCode", $"%{dieCode}%"));
            }

            using var connection = DbHelper.CreateConnection();
            connection.Open();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());
            using var reader = command.ExecuteReader();
            
            if (reader.Read())
            {
                return new ProductionStatistics
                {
                    PendingCount = reader["PendingCount"] != DBNull.Value ? Convert.ToInt32(reader["PendingCount"]) : 0,
                    InProgressCount = reader["InProgressCount"] != DBNull.Value ? Convert.ToInt32(reader["InProgressCount"]) : 0,
                    CompletedCount = reader["CompletedCount"] != DBNull.Value ? Convert.ToInt32(reader["CompletedCount"]) : 0,
                    TotalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0
                };
            }

            return new ProductionStatistics();
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "获取生产统计信息");
            return new ProductionStatistics();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取生产统计信息");
            return new ProductionStatistics();
        }
    }

    #endregion

    #region 完工查询相关

    /// <summary>
    /// 查询完工记录
    /// </summary>
    public List<CompletionRecord> QueryCompletions(DateTime? startDate, DateTime? endDate, string? dieCode, string? processName)
    {
        try
        {
            var sql = @"SELECT c.CompletionID, c.DieID, d.DieCode, d.CustomerName, d.ProductName,
                                c.CompleteTime, c.TotalAmount, c.OperatorNo, c.OperatorName, c.Remark
                         FROM DM_DieCompletion c
                         INNER JOIN DM_DieInfo d ON c.DieID = d.DieID
                         WHERE 1=1";

            var parameters = new List<SqlParameter>();

            if (startDate.HasValue)
            {
                sql += " AND c.CompleteTime >= @StartDate";
                parameters.Add(new SqlParameter("@StartDate", startDate.Value));
            }

            if (endDate.HasValue)
            {
                sql += " AND c.CompleteTime <= @EndDate";
                parameters.Add(new SqlParameter("@EndDate", endDate.Value.AddDays(1).AddSeconds(-1)));
            }

            if (!string.IsNullOrEmpty(dieCode))
            {
                sql += " AND d.DieCode LIKE @DieCode";
                parameters.Add(new SqlParameter("@DieCode", $"%{dieCode}%"));
            }

            if (!string.IsNullOrEmpty(processName))
            {
                sql += " AND EXISTS (SELECT 1 FROM DM_DieProcess p WHERE p.DieID = c.DieID AND p.ProcessName LIKE @ProcessName)";
                parameters.Add(new SqlParameter("@ProcessName", $"%{processName}%"));
            }

            sql += " ORDER BY c.CompleteTime DESC";

            return DbHelper.ExecuteQuery(sql, reader => new CompletionRecord
            {
                CompletionID = Convert.ToInt32(reader["CompletionID"]),
                DieID = Convert.ToInt32(reader["DieID"]),
                DieCode = reader["DieCode"].ToString() ?? "",
                CustomerName = reader["CustomerName"].ToString() ?? "",
                ProductName = reader["ProductName"].ToString() ?? "",
                CompleteTime = Convert.ToDateTime(reader["CompleteTime"]),
                TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0,
                OperatorNo = reader["OperatorNo"].ToString() ?? "",
                OperatorName = reader["OperatorName"].ToString() ?? "",
                Remark = reader["Remark"]?.ToString() ?? ""
            }, parameters.ToArray());
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "查询完工记录");
            return new List<CompletionRecord>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "查询完工记录");
            return new List<CompletionRecord>();
        }
    }

    #endregion

    #region 工序报产相关

    /// <summary>
    /// 获取刀模的工序列表（用于报产选择）
    /// </summary>
    public List<DieProcessForReport> GetDieProcessesForReport(int dieId)
    {
        try
        {
            var sql = @"SELECT p.ProcessID, p.DieID, p.ProcessName, p.Status, p.StartTime, p.CompleteTime,
                                p.OperatorNo, p.OperatorName, p.Amount, p.PrevProcessID,
                                d.DieCode, d.CustomerName, d.ProductName
                         FROM DM_DieProcess p
                         INNER JOIN DM_DieInfo d ON p.DieID = d.DieID
                         WHERE p.DieID = @DieID
                         ORDER BY p.ProcessID";

            return DbHelper.ExecuteQuery(sql, reader => new DieProcessForReport
            {
                ProcessID = Convert.ToInt32(reader["ProcessID"]),
                DieID = Convert.ToInt32(reader["DieID"]),
                ProcessName = reader["ProcessName"].ToString() ?? "",
                Status = (ProcessStatus)Convert.ToInt32(reader["Status"]),
                StartTime = reader["StartTime"] != DBNull.Value ? Convert.ToDateTime(reader["StartTime"]) : null,
                CompleteTime = reader["CompleteTime"] != DBNull.Value ? Convert.ToDateTime(reader["CompleteTime"]) : null,
                OperatorNo = reader["OperatorNo"].ToString() ?? "",
                OperatorName = reader["OperatorName"].ToString() ?? "",
                Amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : null,
                PrevProcessID = reader["PrevProcessID"] != DBNull.Value ? Convert.ToInt32(reader["PrevProcessID"]) : null,
                DieCode = reader["DieCode"].ToString() ?? "",
                CustomerName = reader["CustomerName"].ToString() ?? "",
                ProductName = reader["ProductName"].ToString() ?? ""
            }, new SqlParameter("@DieID", dieId));
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, $"获取工序列表(DieID:{dieId})");
            return new List<DieProcessForReport>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, $"获取工序列表(DieID:{dieId})");
            return new List<DieProcessForReport>();
        }
    }

    /// <summary>
    /// 获取可报产的刀模列表（状态为待生产或生产中）
    /// </summary>
    public List<DieInfoForReport> GetAvailableDiesForReport()
    {
        try
        {
            var sql = @"SELECT DieID, DieCode, CustomerName, ProductName, Status, DeliveryDate
                         FROM DM_DieInfo
                         WHERE Status IN (0, 1)
                         ORDER BY CreateTime DESC";

            return DbHelper.ExecuteQuery(sql, reader => new DieInfoForReport
            {
                DieID = Convert.ToInt32(reader["DieID"]),
                DieCode = reader["DieCode"].ToString() ?? "",
                CustomerName = reader["CustomerName"].ToString() ?? "",
                ProductName = reader["ProductName"].ToString() ?? "",
                Status = (DieStatus)Convert.ToInt32(reader["Status"]),
                DeliveryDate = reader["DeliveryDate"] != DBNull.Value ? Convert.ToDateTime(reader["DeliveryDate"]) : null
            });
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "获取可报产刀模列表");
            return new List<DieInfoForReport>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取可报产刀模列表");
            return new List<DieInfoForReport>();
        }
    }

    /// <summary>
    /// 开始工序生产
    /// </summary>
    public bool StartProcess(int processId, string operatorNo, string operatorName)
    {
        try
        {
            // 检查前道工序是否完成
            if (!IsPrevProcessCompleted(processId))
            {
                ExceptionHelper.HandleException(new BusinessException("前道工序尚未完成，无法开始当前工序。"), "开始工序生产");
                return false;
            }

            var sql = @"UPDATE DM_DieProcess 
                         SET Status = 1, StartTime = GETDATE(), OperatorNo = @OperatorNo, OperatorName = @OperatorName
                         WHERE ProcessID = @ProcessID AND Status = 0";

            var result = DbHelper.ExecuteNonQuery(sql,
                new SqlParameter("@ProcessID", processId),
                new SqlParameter("@OperatorNo", operatorNo),
                new SqlParameter("@OperatorName", operatorName));

            // 同时更新刀模状态为生产中
            if (result > 0)
            {
                UpdateDieStatusToInProgress(processId);
            }

            return result > 0;
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, $"开始工序生产(ProcessID:{processId})");
            return false;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, $"开始工序生产(ProcessID:{processId})");
            return false;
        }
    }

    /// <summary>
    /// 完成工序生产
    /// </summary>
    public bool CompleteProcess(int processId, decimal? amount, string operatorNo, string operatorName, string? remark)
    {
        try
        {
            var sql = @"UPDATE DM_DieProcess 
                         SET Status = 2, CompleteTime = GETDATE(), 
                             OperatorNo = @OperatorNo, OperatorName = @OperatorName,
                             Amount = @Amount
                         WHERE ProcessID = @ProcessID AND Status = 1";

            var result = DbHelper.ExecuteNonQuery(sql,
                new SqlParameter("@ProcessID", processId),
                new SqlParameter("@OperatorNo", operatorNo),
                new SqlParameter("@OperatorName", operatorName),
                new SqlParameter("@Amount", (object?)amount ?? DBNull.Value));

            // 检查是否所有工序都已完成
            if (result > 0)
            {
                CheckAndCompleteDie(processId);
            }

            return result > 0;
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, $"完成工序生产(ProcessID:{processId})");
            return false;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, $"完成工序生产(ProcessID:{processId})");
            return false;
        }
    }

    /// <summary>
    /// 更新刀模状态为生产中
    /// </summary>
    private void UpdateDieStatusToInProgress(int processId)
    {
        try
        {
            var sql = @"UPDATE DM_DieInfo 
                         SET Status = 1 
                         WHERE DieID = (SELECT DieID FROM DM_DieProcess WHERE ProcessID = @ProcessID)
                         AND Status = 0";
            DbHelper.ExecuteNonQuery(sql, new SqlParameter("@ProcessID", processId));
        }
        catch (Exception ex)
        {
            // 状态更新失败不影响主流程，仅记录日志
            ExceptionHelper.HandleExceptionSilent(ex, "更新刀模状态为生产中");
        }
    }

    /// <summary>
    /// 检查并更新刀模完成状态
    /// </summary>
    private void CheckAndCompleteDie(int processId)
    {
        try
        {
            // 获取该工序所属的刀模ID
            var getDieSql = "SELECT DieID FROM DM_DieProcess WHERE ProcessID = @ProcessID";
            var dieId = DbHelper.ExecuteScalar(getDieSql, new SqlParameter("@ProcessID", processId));

            if (dieId != null && dieId != DBNull.Value)
            {
                // 检查是否所有工序都已完成
                var checkSql = @"SELECT COUNT(*) FROM DM_DieProcess 
                                  WHERE DieID = @DieID AND Status != 2";
                var incompleteCount = DbHelper.ExecuteScalar(checkSql, new SqlParameter("@DieID", dieId));

                if (Convert.ToInt32(incompleteCount) == 0)
                {
                    // 所有工序已完成，更新刀模状态并创建完工记录
                    var updateSql = "UPDATE DM_DieInfo SET Status = 2 WHERE DieID = @DieID";
                    DbHelper.ExecuteNonQuery(updateSql, new SqlParameter("@DieID", dieId));

                    // 创建完工记录
                    CreateCompletionRecord(Convert.ToInt32(dieId));
                }
            }
        }
        catch (Exception ex)
        {
            // 状态更新失败不影响主流程，仅记录日志
            ExceptionHelper.HandleExceptionSilent(ex, "检查并更新刀模完成状态");
        }
    }

    /// <summary>
    /// 创建完工记录
    /// </summary>
    private void CreateCompletionRecord(int dieId)
    {
        try
        {
            var sql = @"INSERT INTO DM_DieCompletion (DieID, CompleteTime, TotalAmount, OperatorNo, OperatorName, Remark)
                         SELECT @DieID, GETDATE(), SUM(Amount), MAX(OperatorNo), MAX(OperatorName), ''
                         FROM DM_DieProcess 
                         WHERE DieID = @DieID";

            DbHelper.ExecuteNonQuery(sql, new SqlParameter("@DieID", dieId));
        }
        catch (Exception ex)
        {
            // 完工记录创建失败不影响主流程，仅记录日志
            ExceptionHelper.HandleExceptionSilent(ex, "创建完工记录");
        }
    }

    /// <summary>
    /// 检查前道工序是否已完成
    /// </summary>
    public bool IsPrevProcessCompleted(int processId)
    {
        try
        {
            var sql = @"SELECT PrevProcessID FROM DM_DieProcess WHERE ProcessID = @ProcessID";
            var result = DbHelper.ExecuteScalar(sql, new SqlParameter("@ProcessID", processId));

            if (result == null || result == DBNull.Value)
                return true; // 没有前道工序

            var prevProcessId = Convert.ToInt32(result);

            var checkSql = "SELECT Status FROM DM_DieProcess WHERE ProcessID = @ProcessID";
            var statusResult = DbHelper.ExecuteScalar(checkSql, new SqlParameter("@ProcessID", prevProcessId));

            if (statusResult != null && statusResult != DBNull.Value)
            {
                return Convert.ToInt32(statusResult) == 2; // 2 = 已完成
            }

            return false;
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, $"检查前道工序状态(ProcessID:{processId})");
            return false;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, $"检查前道工序状态(ProcessID:{processId})");
            return false;
        }
    }

    #endregion
}

#region 生产看板相关模型

public class ProductionBoardData
{
    public List<DieBoardItem> PendingList { get; set; } = new();
    public List<DieBoardItem> InProgressList { get; set; } = new();
    public List<DieBoardItem> CompletedList { get; set; } = new();
    public ProductionStatistics Statistics { get; set; } = new();
}

public class DieBoardItem
{
    public int DieID { get; set; }
    public string DieCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DateTime? DeliveryDate { get; set; }
    public DieStatus Status { get; set; }
    public DateTime CreateTime { get; set; }
    public int TotalProcesses { get; set; }
    public int CompletedProcesses { get; set; }

    public string ProgressText => $"{CompletedProcesses}/{TotalProcesses}";
    public double ProgressPercent => TotalProcesses > 0 ? (double)CompletedProcesses / TotalProcesses * 100 : 0;
}

public class ProductionStatistics
{
    public int PendingCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; }
}

#endregion

#region 完工查询相关模型

public class CompletionRecord
{
    public int CompletionID { get; set; }
    public int DieID { get; set; }
    public string DieCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DateTime CompleteTime { get; set; }
    public decimal TotalAmount { get; set; }
    public string OperatorNo { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
}

#endregion

#region 工序报产相关模型

public class DieProcessForReport
{
    public int ProcessID { get; set; }
    public int DieID { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public ProcessStatus Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public string OperatorNo { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public int? PrevProcessID { get; set; }
    public string DieCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    public string StatusText => Status.GetDisplayName();
    public bool CanStart => Status == ProcessStatus.Pending;
    public bool CanComplete => Status == ProcessStatus.InProgress;
}

public class DieInfoForReport
{
    public int DieID { get; set; }
    public string DieCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DieStatus Status { get; set; }
    public DateTime? DeliveryDate { get; set; }

    public string DisplayText => $"{DieCode} - {CustomerName} - {ProductName}";
}

#endregion
