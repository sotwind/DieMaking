using DieMaking.Helpers;
using DieMaking.Models;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

/// <summary>
/// 报表统计服务类
/// </summary>
public class ReportService
{
    #region 完工统计

    /// <summary>
    /// 获取完工统计数据（按刀模）
    /// </summary>
    public List<CompletionStatsByDie> GetCompletionStatsByDie(DateTime? startDate, DateTime? endDate, string? dieCode = null, string? customerName = null)
    {
        try
        {
            var sql = @"
                SELECT 
                    d.DieID,
                    d.DieCode,
                    d.CustomerName,
                    d.ProductName,
                    d.RequiredProcesses,
                    dc.CompleteTime,
                    dc.TotalAmount,
                    dc.OperatorName,
                    dc.Remark
                FROM DM_DieCompletion dc
                INNER JOIN DM_DieInfo d ON dc.DieID = d.DieID
                WHERE 1=1";

            var parameters = new List<SqlParameter>();

            if (startDate.HasValue)
            {
                sql += " AND dc.CompleteTime >= @StartDate";
                parameters.Add(new SqlParameter("@StartDate", startDate.Value));
            }

            if (endDate.HasValue)
            {
                sql += " AND dc.CompleteTime <= @EndDate";
                parameters.Add(new SqlParameter("@EndDate", endDate.Value.AddDays(1).AddSeconds(-1)));
            }

            if (!string.IsNullOrEmpty(dieCode))
            {
                sql += " AND d.DieCode LIKE @DieCode";
                parameters.Add(new SqlParameter("@DieCode", $"%{dieCode}%"));
            }

            if (!string.IsNullOrEmpty(customerName))
            {
                sql += " AND d.CustomerName LIKE @CustomerName";
                parameters.Add(new SqlParameter("@CustomerName", $"%{customerName}%"));
            }

            sql += " ORDER BY dc.CompleteTime DESC";

            return DbHelper.ExecuteQuery(sql, reader => new CompletionStatsByDie
            {
                DieID = Convert.ToInt32(reader["DieID"]),
                DieCode = reader["DieCode"].ToString() ?? "",
                CustomerName = reader["CustomerName"].ToString() ?? "",
                ProductName = reader["ProductName"].ToString() ?? "",
                RequiredProcesses = reader["RequiredProcesses"].ToString() ?? "",
                CompleteTime = Convert.ToDateTime(reader["CompleteTime"]),
                TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0,
                OperatorName = reader["OperatorName"].ToString() ?? "",
                Remark = reader["Remark"].ToString() ?? ""
            }, parameters.ToArray());
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "获取完工统计数据（按刀模）");
            return new List<CompletionStatsByDie>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取完工统计数据（按刀模）");
            return new List<CompletionStatsByDie>();
        }
    }

    /// <summary>
    /// 获取完工统计数据（按客户汇总）
    /// </summary>
    public List<CompletionStatsByCustomer> GetCompletionStatsByCustomer(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var sql = @"
                SELECT 
                    d.CustomerName,
                    COUNT(*) as CompletionCount,
                    SUM(dc.TotalAmount) as TotalAmount,
                    MIN(dc.CompleteTime) as FirstCompleteTime,
                    MAX(dc.CompleteTime) as LastCompleteTime
                FROM DM_DieCompletion dc
                INNER JOIN DM_DieInfo d ON dc.DieID = d.DieID
                WHERE 1=1";

            var parameters = new List<SqlParameter>();

            if (startDate.HasValue)
            {
                sql += " AND dc.CompleteTime >= @StartDate";
                parameters.Add(new SqlParameter("@StartDate", startDate.Value));
            }

            if (endDate.HasValue)
            {
                sql += " AND dc.CompleteTime <= @EndDate";
                parameters.Add(new SqlParameter("@EndDate", endDate.Value.AddDays(1).AddSeconds(-1)));
            }

            sql += " GROUP BY d.CustomerName ORDER BY CompletionCount DESC";

            return DbHelper.ExecuteQuery(sql, reader => new CompletionStatsByCustomer
            {
                CustomerName = reader["CustomerName"].ToString() ?? "",
                CompletionCount = Convert.ToInt32(reader["CompletionCount"]),
                TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0,
                FirstCompleteTime = reader["FirstCompleteTime"] != DBNull.Value ? Convert.ToDateTime(reader["FirstCompleteTime"]) : null,
                LastCompleteTime = reader["LastCompleteTime"] != DBNull.Value ? Convert.ToDateTime(reader["LastCompleteTime"]) : null
            }, parameters.ToArray());
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "获取完工统计数据（按客户）");
            return new List<CompletionStatsByCustomer>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取完工统计数据（按客户）");
            return new List<CompletionStatsByCustomer>();
        }
    }

    /// <summary>
    /// 获取完工统计数据（按日期汇总）
    /// </summary>
    public List<CompletionStatsByDate> GetCompletionStatsByDate(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var sql = @"
                SELECT 
                    CAST(dc.CompleteTime AS DATE) as CompleteDate,
                    COUNT(*) as CompletionCount,
                    SUM(dc.TotalAmount) as TotalAmount
                FROM DM_DieCompletion dc
                WHERE 1=1";

            var parameters = new List<SqlParameter>();

            if (startDate.HasValue)
            {
                sql += " AND dc.CompleteTime >= @StartDate";
                parameters.Add(new SqlParameter("@StartDate", startDate.Value));
            }

            if (endDate.HasValue)
            {
                sql += " AND dc.CompleteTime <= @EndDate";
                parameters.Add(new SqlParameter("@EndDate", endDate.Value.AddDays(1).AddSeconds(-1)));
            }

            sql += " GROUP BY CAST(dc.CompleteTime AS DATE) ORDER BY CompleteDate DESC";

            return DbHelper.ExecuteQuery(sql, reader => new CompletionStatsByDate
            {
                CompleteDate = Convert.ToDateTime(reader["CompleteDate"]),
                CompletionCount = Convert.ToInt32(reader["CompletionCount"]),
                TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0
            }, parameters.ToArray());
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "获取完工统计数据（按日期）");
            return new List<CompletionStatsByDate>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取完工统计数据（按日期）");
            return new List<CompletionStatsByDate>();
        }
    }

    #endregion

    #region 工序统计

    /// <summary>
    /// 获取工序统计数据
    /// </summary>
    public List<ProcessStats> GetProcessStats(DateTime? startDate, DateTime? endDate, string? processName = null)
    {
        try
        {
            var sql = @"
                SELECT 
                    dp.ProcessName,
                    COUNT(*) as TotalCount,
                    SUM(CASE WHEN dp.Status = 2 THEN 1 ELSE 0 END) as CompletedCount,
                    SUM(CASE WHEN dp.Status = 1 THEN 1 ELSE 0 END) as InProgressCount,
                    SUM(CASE WHEN dp.Status = 0 THEN 1 ELSE 0 END) as PendingCount,
                    AVG(CASE WHEN dp.Status = 2 AND dp.CompleteTime IS NOT NULL AND dp.StartTime IS NOT NULL 
                        THEN DATEDIFF(MINUTE, dp.StartTime, dp.CompleteTime) ELSE NULL END) as AvgDurationMinutes,
                    SUM(dp.Amount) as TotalAmount
                FROM DM_DieProcess dp
                INNER JOIN DM_DieInfo d ON dp.DieID = d.DieID
                WHERE 1=1";

            var parameters = new List<SqlParameter>();

            if (startDate.HasValue)
            {
                sql += " AND dp.CreateTime >= @StartDate";
                parameters.Add(new SqlParameter("@StartDate", startDate.Value));
            }

            if (endDate.HasValue)
            {
                sql += " AND dp.CreateTime <= @EndDate";
                parameters.Add(new SqlParameter("@EndDate", endDate.Value.AddDays(1).AddSeconds(-1)));
            }

            if (!string.IsNullOrEmpty(processName))
            {
                sql += " AND dp.ProcessName LIKE @ProcessName";
                parameters.Add(new SqlParameter("@ProcessName", $"%{processName}%"));
            }

            sql += " GROUP BY dp.ProcessName ORDER BY TotalCount DESC";

            return DbHelper.ExecuteQuery(sql, reader => new ProcessStats
            {
                ProcessName = reader["ProcessName"].ToString() ?? "",
                TotalCount = Convert.ToInt32(reader["TotalCount"]),
                CompletedCount = Convert.ToInt32(reader["CompletedCount"]),
                InProgressCount = Convert.ToInt32(reader["InProgressCount"]),
                PendingCount = Convert.ToInt32(reader["PendingCount"]),
                AvgDurationMinutes = reader["AvgDurationMinutes"] != DBNull.Value ? Convert.ToDouble(reader["AvgDurationMinutes"]) : 0,
                TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0,
                CompletionRate = Convert.ToInt32(reader["TotalCount"]) > 0 
                    ? (double)Convert.ToInt32(reader["CompletedCount"]) / Convert.ToInt32(reader["TotalCount"]) * 100 
                    : 0
            }, parameters.ToArray());
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "获取工序统计数据");
            return new List<ProcessStats>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取工序统计数据");
            return new List<ProcessStats>();
        }
    }

    /// <summary>
    /// 获取工序明细数据
    /// </summary>
    public List<ProcessDetailStats> GetProcessDetailStats(DateTime? startDate, DateTime? endDate, string? processName = null)
    {
        try
        {
            var sql = @"
                SELECT 
                    dp.ProcessID,
                    d.DieCode,
                    d.CustomerName,
                    dp.ProcessName,
                    dp.Status,
                    dp.StartTime,
                    dp.CompleteTime,
                    dp.OperatorName,
                    dp.Amount,
                    CASE WHEN dp.CompleteTime IS NOT NULL AND dp.StartTime IS NOT NULL 
                        THEN DATEDIFF(MINUTE, dp.StartTime, dp.CompleteTime) ELSE NULL END as DurationMinutes
                FROM DM_DieProcess dp
                INNER JOIN DM_DieInfo d ON dp.DieID = d.DieID
                WHERE 1=1";

            var parameters = new List<SqlParameter>();

            if (startDate.HasValue)
            {
                sql += " AND dp.CreateTime >= @StartDate";
                parameters.Add(new SqlParameter("@StartDate", startDate.Value));
            }

            if (endDate.HasValue)
            {
                sql += " AND dp.CreateTime <= @EndDate";
                parameters.Add(new SqlParameter("@EndDate", endDate.Value.AddDays(1).AddSeconds(-1)));
            }

            if (!string.IsNullOrEmpty(processName))
            {
                sql += " AND dp.ProcessName LIKE @ProcessName";
                parameters.Add(new SqlParameter("@ProcessName", $"%{processName}%"));
            }

            sql += " ORDER BY dp.CompleteTime DESC";

            return DbHelper.ExecuteQuery(sql, reader => new ProcessDetailStats
            {
                ProcessID = Convert.ToInt32(reader["ProcessID"]),
                DieCode = reader["DieCode"].ToString() ?? "",
                CustomerName = reader["CustomerName"].ToString() ?? "",
                ProcessName = reader["ProcessName"].ToString() ?? "",
                Status = (ProcessStatus)Convert.ToInt32(reader["Status"]),
                StartTime = reader["StartTime"] != DBNull.Value ? Convert.ToDateTime(reader["StartTime"]) : null,
                CompleteTime = reader["CompleteTime"] != DBNull.Value ? Convert.ToDateTime(reader["CompleteTime"]) : null,
                OperatorName = reader["OperatorName"].ToString() ?? "",
                Amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : null,
                DurationMinutes = reader["DurationMinutes"] != DBNull.Value ? Convert.ToInt32(reader["DurationMinutes"]) : null
            }, parameters.ToArray());
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "获取工序明细数据");
            return new List<ProcessDetailStats>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取工序明细数据");
            return new List<ProcessDetailStats>();
        }
    }

    #endregion

    #region 库存统计

    /// <summary>
    /// 获取库存汇总统计
    /// </summary>
    public InventorySummaryStats GetInventorySummaryStats()
    {
        try
        {
            var sql = @"
                SELECT 
                    COUNT(*) as TotalCount,
                    SUM(CASE WHEN StorageStatus = 0 THEN 1 ELSE 0 END) as InStockCount,
                    SUM(CASE WHEN StorageStatus = 1 THEN 1 ELSE 0 END) as BorrowedCount,
                    SUM(CASE WHEN StorageStatus = 2 THEN 1 ELSE 0 END) as ScrappedCount,
                    SUM(CASE WHEN StorageStatus = 3 THEN 1 ELSE 0 END) as RepairingCount
                FROM DM_DieInventory";

            var result = DbHelper.ExecuteQuery(sql, reader => new InventorySummaryStats
            {
                TotalCount = Convert.ToInt32(reader["TotalCount"]),
                InStockCount = Convert.ToInt32(reader["InStockCount"]),
                BorrowedCount = Convert.ToInt32(reader["BorrowedCount"]),
                ScrappedCount = Convert.ToInt32(reader["ScrappedCount"]),
                RepairingCount = Convert.ToInt32(reader["RepairingCount"])
            }).FirstOrDefault();

            return result ?? new InventorySummaryStats();
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "获取库存汇总统计");
            return new InventorySummaryStats();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取库存汇总统计");
            return new InventorySummaryStats();
        }
    }

    /// <summary>
    /// 获取库位分布统计
    /// </summary>
    public List<LocationDistributionStats> GetLocationDistributionStats()
    {
        try
        {
            var sql = @"
                SELECT 
                    sl.Area,
                    sl.ShelfNo,
                    COUNT(di.InventoryID) as DieCount,
                    SUM(CASE WHEN di.StorageStatus = 0 THEN 1 ELSE 0 END) as InStockCount,
                    SUM(CASE WHEN di.StorageStatus = 1 THEN 1 ELSE 0 END) as BorrowedCount
                FROM DM_StorageLocation sl
                LEFT JOIN DM_DieInventory di ON sl.LocationID = di.LocationID
                GROUP BY sl.Area, sl.ShelfNo
                ORDER BY sl.Area, sl.ShelfNo";

            return DbHelper.ExecuteQuery(sql, reader => new LocationDistributionStats
            {
                Area = reader["Area"].ToString() ?? "",
                ShelfNo = reader["ShelfNo"].ToString() ?? "",
                DieCount = Convert.ToInt32(reader["DieCount"]),
                InStockCount = Convert.ToInt32(reader["InStockCount"]),
                BorrowedCount = Convert.ToInt32(reader["BorrowedCount"])
            });
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "获取库位分布统计");
            return new List<LocationDistributionStats>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取库位分布统计");
            return new List<LocationDistributionStats>();
        }
    }

    /// <summary>
    /// 获取库存明细数据
    /// </summary>
    public List<InventoryDetailStats> GetInventoryDetailStats(string? area = null, string? shelfNo = null, StorageStatus? status = null)
    {
        try
        {
            var sql = @"
                SELECT 
                    di.InventoryID,
                    d.DieCode,
                    d.CustomerName,
                    d.ProductName,
                    sl.Area,
                    sl.ShelfNo,
                    sl.LayerNo,
                    sl.PositionNo,
                    di.StorageStatus,
                    di.InStockTime,
                    di.LastBorrowTime,
                    di.LastReturnTime,
                    di.TotalBorrowCount
                FROM DM_DieInventory di
                INNER JOIN DM_DieInfo d ON di.DieID = d.DieID
                LEFT JOIN DM_StorageLocation sl ON di.LocationID = sl.LocationID
                WHERE 1=1";

            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(area))
            {
                sql += " AND sl.Area = @Area";
                parameters.Add(new SqlParameter("@Area", area));
            }

            if (!string.IsNullOrEmpty(shelfNo))
            {
                sql += " AND sl.ShelfNo = @ShelfNo";
                parameters.Add(new SqlParameter("@ShelfNo", shelfNo));
            }

            if (status.HasValue)
            {
                sql += " AND di.StorageStatus = @Status";
                parameters.Add(new SqlParameter("@Status", (int)status.Value));
            }

            sql += " ORDER BY sl.Area, sl.ShelfNo, sl.LayerNo, sl.PositionNo";

            return DbHelper.ExecuteQuery(sql, reader => new InventoryDetailStats
            {
                InventoryID = Convert.ToInt32(reader["InventoryID"]),
                DieCode = reader["DieCode"].ToString() ?? "",
                CustomerName = reader["CustomerName"].ToString() ?? "",
                ProductName = reader["ProductName"].ToString() ?? "",
                Area = reader["Area"]?.ToString() ?? "",
                ShelfNo = reader["ShelfNo"]?.ToString() ?? "",
                LayerNo = reader["LayerNo"]?.ToString() ?? "",
                PositionNo = reader["PositionNo"]?.ToString() ?? "",
                StorageStatus = reader["StorageStatus"] != DBNull.Value ? (StorageStatus)Convert.ToInt32(reader["StorageStatus"]) : StorageStatus.InStock,
                InStockTime = reader["InStockTime"] != DBNull.Value ? Convert.ToDateTime(reader["InStockTime"]) : null,
                LastBorrowTime = reader["LastBorrowTime"] != DBNull.Value ? Convert.ToDateTime(reader["LastBorrowTime"]) : null,
                LastReturnTime = reader["LastReturnTime"] != DBNull.Value ? Convert.ToDateTime(reader["LastReturnTime"]) : null,
                TotalBorrowCount = Convert.ToInt32(reader["TotalBorrowCount"])
            }, parameters.ToArray());
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "获取库存明细数据");
            return new List<InventoryDetailStats>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取库存明细数据");
            return new List<InventoryDetailStats>();
        }
    }

    /// <summary>
    /// 获取借用记录统计
    /// </summary>
    public List<BorrowStats> GetBorrowStats(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var sql = @"
                SELECT 
                    dbr.BorrowType,
                    dbr.Status,
                    COUNT(*) as RecordCount,
                    AVG(CASE WHEN dbr.ActualReturnTime IS NOT NULL THEN 
                        DATEDIFF(DAY, dbr.BorrowTime, dbr.ActualReturnTime) ELSE NULL END) as AvgBorrowDays
                FROM DM_DieBorrowRecord dbr
                WHERE 1=1";

            var parameters = new List<SqlParameter>();

            if (startDate.HasValue)
            {
                sql += " AND dbr.BorrowTime >= @StartDate";
                parameters.Add(new SqlParameter("@StartDate", startDate.Value));
            }

            if (endDate.HasValue)
            {
                sql += " AND dbr.BorrowTime <= @EndDate";
                parameters.Add(new SqlParameter("@EndDate", endDate.Value.AddDays(1).AddSeconds(-1)));
            }

            sql += " GROUP BY dbr.BorrowType, dbr.Status ORDER BY dbr.BorrowType, dbr.Status";

            return DbHelper.ExecuteQuery(sql, reader => new BorrowStats
            {
                BorrowType = (BorrowType)Convert.ToInt32(reader["BorrowType"]),
                Status = (BorrowStatus)Convert.ToInt32(reader["Status"]),
                RecordCount = Convert.ToInt32(reader["RecordCount"]),
                AvgBorrowDays = reader["AvgBorrowDays"] != DBNull.Value ? Convert.ToDouble(reader["AvgBorrowDays"]) : 0
            }, parameters.ToArray());
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "获取借用记录统计");
            return new List<BorrowStats>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取借用记录统计");
            return new List<BorrowStats>();
        }
    }

    #endregion
}

#region 完工统计模型

public class CompletionStatsByDie
{
    public int DieID { get; set; }
    public string DieCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string RequiredProcesses { get; set; } = string.Empty;
    public DateTime CompleteTime { get; set; }
    public decimal TotalAmount { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
}

public class CompletionStatsByCustomer
{
    public string CustomerName { get; set; } = string.Empty;
    public int CompletionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? FirstCompleteTime { get; set; }
    public DateTime? LastCompleteTime { get; set; }
}

public class CompletionStatsByDate
{
    public DateTime CompleteDate { get; set; }
    public int CompletionCount { get; set; }
    public decimal TotalAmount { get; set; }
}

#endregion

#region 工序统计模型

public class ProcessStats
{
    public string ProcessName { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int CompletedCount { get; set; }
    public int InProgressCount { get; set; }
    public int PendingCount { get; set; }
    public double CompletionRate { get; set; }
    public double AvgDurationMinutes { get; set; }
    public decimal TotalAmount { get; set; }

    public string AvgDurationText => AvgDurationMinutes > 0 
        ? $"{AvgDurationMinutes / 60:F1}小时" 
        : "-";
}

public class ProcessDetailStats
{
    public int ProcessID { get; set; }
    public string DieCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public ProcessStatus Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public int? DurationMinutes { get; set; }

    public string StatusText => Status.GetDisplayName();
    public string DurationText => DurationMinutes.HasValue 
        ? $"{DurationMinutes.Value / 60}小时{DurationMinutes.Value % 60}分钟" 
        : "-";
}

#endregion

#region 库存统计模型

public class InventorySummaryStats
{
    public int TotalCount { get; set; }
    public int InStockCount { get; set; }
    public int BorrowedCount { get; set; }
    public int ScrappedCount { get; set; }
    public int RepairingCount { get; set; }

    public double InStockRate => TotalCount > 0 ? (double)InStockCount / TotalCount * 100 : 0;
    public double BorrowedRate => TotalCount > 0 ? (double)BorrowedCount / TotalCount * 100 : 0;
}

public class LocationDistributionStats
{
    public string Area { get; set; } = string.Empty;
    public string ShelfNo { get; set; } = string.Empty;
    public int DieCount { get; set; }
    public int InStockCount { get; set; }
    public int BorrowedCount { get; set; }
}

public class InventoryDetailStats
{
    public int InventoryID { get; set; }
    public string DieCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string ShelfNo { get; set; } = string.Empty;
    public string LayerNo { get; set; } = string.Empty;
    public string PositionNo { get; set; } = string.Empty;
    public StorageStatus StorageStatus { get; set; }
    public DateTime? InStockTime { get; set; }
    public DateTime? LastBorrowTime { get; set; }
    public DateTime? LastReturnTime { get; set; }
    public int TotalBorrowCount { get; set; }

    public string StorageStatusText => StorageStatus.GetDisplayName();
    public string LocationText => $"{Area}-{ShelfNo}-{LayerNo}-{PositionNo}";
}

public class BorrowStats
{
    public BorrowType BorrowType { get; set; }
    public BorrowStatus Status { get; set; }
    public int RecordCount { get; set; }
    public double AvgBorrowDays { get; set; }

    public string BorrowTypeText => BorrowType.GetDisplayName();
    public string StatusText => Status.GetDisplayName();
}

#endregion
