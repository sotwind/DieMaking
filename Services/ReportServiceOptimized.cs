using DieMaking.Helpers;
using DieMaking.Models;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

/// <summary>
/// 报表统计服务类 - 优化版本（使用缓存和分页优化）
/// </summary>
public class ReportServiceOptimized : ReportService
{
    #region 完工统计 - 优化版本

    /// <summary>
    /// 获取完工统计数据（按刀模）- 使用缓存和分页优化
    /// </summary>
    public new List<CompletionStatsByDie> GetCompletionStatsByDie(DateTime? startDate, DateTime? endDate, string? dieCode = null, string? customerName = null, int pageIndex = 1, int pageSize = 100)
    {
        try
        {
            // 使用缓存
            return QueryCacheHelper.GetOrCacheCompletionStats(
                startDate ?? DateTime.MinValue,
                endDate ?? DateTime.MaxValue,
                () => base.GetCompletionStatsByDie(startDate, endDate, dieCode, customerName, pageIndex, pageSize));
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取完工统计数据（缓存版本）");
            // 缓存失败时回退到基础版本
            return base.GetCompletionStatsByDie(startDate, endDate, dieCode, customerName, pageIndex, pageSize);
        }
    }

    /// <summary>
    /// 获取完工统计数据（按客户汇总）- 使用缓存优化
    /// </summary>
    public new List<CompletionStatsByCustomer> GetCompletionStatsByCustomer(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var cacheKey = $"CompletionStatsByCustomer:{startDate?.ToString("yyyyMMdd") ?? "null"}:{endDate?.ToString("yyyyMMdd") ?? "null"}";
            
            return QueryCacheHelper.GetOrCreate(cacheKey, () =>
            {
                var baseSql = @"
                    SELECT 
                        d.CustomerName,
                        COUNT(*) as CompletionCount,
                        SUM(dc.TotalAmount) as TotalAmount,
                        MIN(dc.CompleteTime) as FirstCompleteTime,
                        MAX(dc.CompleteTime) as LastCompleteTime
                    FROM DM_DieCompletion dc WITH (NOLOCK)
                    INNER JOIN DM_DieInfo d WITH (NOLOCK) ON dc.DieID = d.DieID
                    WHERE 1=1";

                var parameters = new List<SqlParameter>();

                if (startDate.HasValue)
                {
                    baseSql += " AND dc.CompleteTime >= @StartDate";
                    parameters.Add(new SqlParameter("@StartDate", startDate.Value));
                }

                if (endDate.HasValue)
                {
                    baseSql += " AND dc.CompleteTime <= @EndDate";
                    parameters.Add(new SqlParameter("@EndDate", endDate.Value.AddDays(1).AddSeconds(-1)));
                }

                baseSql += " GROUP BY d.CustomerName ORDER BY CompletionCount DESC";

                return DbHelper.ExecuteQuery(baseSql, reader => new CompletionStatsByCustomer
                {
                    CustomerName = reader["CustomerName"].ToString() ?? "",
                    CompletionCount = Convert.ToInt32(reader["CompletionCount"]),
                    TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0,
                    FirstCompleteTime = reader["FirstCompleteTime"] != DBNull.Value ? Convert.ToDateTime(reader["FirstCompleteTime"]) : null,
                    LastCompleteTime = reader["LastCompleteTime"] != DBNull.Value ? Convert.ToDateTime(reader["LastCompleteTime"]) : null
                }, parameters.ToArray());
            }, QueryCacheHelper.StatsExpiration) ?? new List<CompletionStatsByCustomer>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取完工统计数据（按客户）");
            return new List<CompletionStatsByCustomer>();
        }
    }

    /// <summary>
    /// 获取完工统计数据（按日期汇总）- 使用缓存优化
    /// </summary>
    public new List<CompletionStatsByDate> GetCompletionStatsByDate(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var cacheKey = $"CompletionStatsByDate:{startDate?.ToString("yyyyMMdd") ?? "null"}:{endDate?.ToString("yyyyMMdd") ?? "null"}";
            
            return QueryCacheHelper.GetOrCreate(cacheKey, () =>
            {
                var baseSql = @"
                    SELECT 
                        CAST(dc.CompleteTime AS DATE) as CompleteDate,
                        COUNT(*) as CompletionCount,
                        SUM(dc.TotalAmount) as TotalAmount
                    FROM DM_DieCompletion dc WITH (NOLOCK)
                    WHERE 1=1";

                var parameters = new List<SqlParameter>();

                if (startDate.HasValue)
                {
                    baseSql += " AND dc.CompleteTime >= @StartDate";
                    parameters.Add(new SqlParameter("@StartDate", startDate.Value));
                }

                if (endDate.HasValue)
                {
                    baseSql += " AND dc.CompleteTime <= @EndDate";
                    parameters.Add(new SqlParameter("@EndDate", endDate.Value.AddDays(1).AddSeconds(-1)));
                }

                baseSql += " GROUP BY CAST(dc.CompleteTime AS DATE) ORDER BY CompleteDate DESC";

                return DbHelper.ExecuteQuery(baseSql, reader => new CompletionStatsByDate
                {
                    CompleteDate = Convert.ToDateTime(reader["CompleteDate"]),
                    CompletionCount = Convert.ToInt32(reader["CompletionCount"]),
                    TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0
                }, parameters.ToArray());
            }, QueryCacheHelper.StatsExpiration) ?? new List<CompletionStatsByDate>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取完工统计数据（按日期）");
            return new List<CompletionStatsByDate>();
        }
    }

    #endregion

    #region 工序统计 - 优化版本

    /// <summary>
    /// 获取工序统计数据 - 使用缓存优化
    /// </summary>
    public new List<ProcessStats> GetProcessStats(DateTime? startDate, DateTime? endDate, string? processName = null)
    {
        try
        {
            return QueryCacheHelper.GetOrCacheProcessStats(
                startDate,
                endDate,
                processName,
                () => base.GetProcessStats(startDate, endDate, processName));
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取工序统计数据（缓存版本）");
            return base.GetProcessStats(startDate, endDate, processName);
        }
    }

    /// <summary>
    /// 获取工序明细数据 - 使用分页优化
    /// </summary>
    public List<ProcessDetailStats> GetProcessDetailStatsPaged(DateTime? startDate, DateTime? endDate, string? processName = null, int pageIndex = 1, int pageSize = 100)
    {
        try
        {
            var baseSql = @"
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
                FROM DM_DieProcess dp WITH (NOLOCK)
                INNER JOIN DM_DieInfo d WITH (NOLOCK) ON dp.DieID = d.DieID
                WHERE 1=1";

            var parameters = new List<SqlParameter>();

            if (startDate.HasValue)
            {
                baseSql += " AND dp.CreateTime >= @StartDate";
                parameters.Add(new SqlParameter("@StartDate", startDate.Value));
            }

            if (endDate.HasValue)
            {
                baseSql += " AND dp.CreateTime <= @EndDate";
                parameters.Add(new SqlParameter("@EndDate", endDate.Value.AddDays(1).AddSeconds(-1)));
            }

            if (!string.IsNullOrEmpty(processName))
            {
                baseSql += " AND dp.ProcessName LIKE @ProcessName";
                parameters.Add(new SqlParameter("@ProcessName", $"%{processName}%"));
            }

            var countCacheKey = $"ProcessDetailStats_Count:{startDate?.ToString("yyyyMMdd")}:{endDate?.ToString("yyyyMMdd")}:{processName ?? "all"}";
            
            var pagedResult = PaginationHelper.ExecutePagedQueryWithCountCache(
                baseSql,
                "dp.CompleteTime DESC",
                pageIndex,
                pageSize,
                countCacheKey,
                reader => new ProcessDetailStats
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
                },
                parameters.ToArray());

            return pagedResult.Items;
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "获取工序明细数据（分页）");
            return new List<ProcessDetailStats>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取工序明细数据（分页）");
            return new List<ProcessDetailStats>();
        }
    }

    #endregion

    #region 库存统计 - 优化版本

    /// <summary>
    /// 获取库存汇总统计 - 使用缓存优化
    /// </summary>
    public new InventorySummaryStats GetInventorySummaryStats()
    {
        try
        {
            return QueryCacheHelper.GetOrCacheInventoryStats(() => base.GetInventorySummaryStats()) ?? new InventorySummaryStats();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取库存汇总统计（缓存版本）");
            return base.GetInventorySummaryStats();
        }
    }

    /// <summary>
    /// 获取库位分布统计 - 使用缓存优化
    /// </summary>
    public new List<LocationDistributionStats> GetLocationDistributionStats()
    {
        try
        {
            return QueryCacheHelper.GetOrCacheLocationDistribution(() => base.GetLocationDistributionStats());
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取库位分布统计（缓存版本）");
            return base.GetLocationDistributionStats();
        }
    }

    /// <summary>
    /// 获取库存明细数据 - 使用分页优化
    /// </summary>
    public PagedResult<InventoryDetailStats> GetInventoryDetailStatsPaged(string? area = null, string? shelfNo = null, StorageStatus? status = null, int pageIndex = 1, int pageSize = 100)
    {
        try
        {
            var baseSql = @"
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
                FROM DM_DieInventory di WITH (NOLOCK)
                INNER JOIN DM_DieInfo d WITH (NOLOCK) ON di.DieID = d.DieID
                LEFT JOIN DM_StorageLocation sl WITH (NOLOCK) ON di.LocationID = sl.LocationID
                WHERE 1=1";

            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(area))
            {
                baseSql += " AND sl.Area = @Area";
                parameters.Add(new SqlParameter("@Area", area));
            }

            if (!string.IsNullOrEmpty(shelfNo))
            {
                baseSql += " AND sl.ShelfNo = @ShelfNo";
                parameters.Add(new SqlParameter("@ShelfNo", shelfNo));
            }

            if (status.HasValue)
            {
                baseSql += " AND di.StorageStatus = @Status";
                parameters.Add(new SqlParameter("@Status", (int)status.Value));
            }

            var countCacheKey = $"InventoryDetail_Count:{area ?? "all"}:{shelfNo ?? "all"}:{status?.ToString() ?? "all"}";

            return PaginationHelper.ExecutePagedQueryWithCountCache(
                baseSql,
                "sl.Area, sl.ShelfNo, sl.LayerNo, sl.PositionNo",
                pageIndex,
                pageSize,
                countCacheKey,
                reader => new InventoryDetailStats
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
                },
                parameters.ToArray());
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "获取库存明细数据（分页）");
            return new PagedResult<InventoryDetailStats>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取库存明细数据（分页）");
            return new PagedResult<InventoryDetailStats>();
        }
    }

    /// <summary>
    /// 获取借用记录统计 - 使用缓存优化
    /// </summary>
    public new List<BorrowStats> GetBorrowStats(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            return QueryCacheHelper.GetOrCacheBorrowStats(
                startDate,
                endDate,
                () => base.GetBorrowStats(startDate, endDate));
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取借用记录统计（缓存版本）");
            return base.GetBorrowStats(startDate, endDate);
        }
    }

    #endregion

    #region 缓存管理

    /// <summary>
    /// 清除报表缓存
    /// </summary>
    public static void ClearCache()
    {
        QueryCacheHelper.InvalidateStatsCache();
        PaginationHelper.ClearCountCache();
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public static CacheStatistics GetCacheStatistics()
    {
        return QueryCacheHelper.GetStatistics();
    }

    #endregion
}

#region 报表统计模型（继承自ReportService）

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
