using DieMaking.Helpers;
using DieMaking.Models;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

/// <summary>
/// 刀模服务类 - 优化版本（使用分页和批量操作优化）
/// </summary>
public class DieServiceOptimized : DieService
{
    #region 分页查询优化

    /// <summary>
    /// 分页搜索刀模
    /// </summary>
    public PagedResult<DieInfo> SearchDiesPaged(
        string? dieCode = null, 
        string? customerName = null, 
        DieStatus? status = null,
        DateTime? startDate = null, 
        DateTime? endDate = null,
        int pageIndex = 1,
        int pageSize = 20)
    {
        try
        {
            var conditions = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(dieCode))
            {
                conditions.Add("d.DieCode LIKE @DieCode");
                parameters.Add(new SqlParameter("@DieCode", $"%{dieCode}%"));
            }

            if (!string.IsNullOrWhiteSpace(customerName))
            {
                conditions.Add("d.CustomerName LIKE @CustomerName");
                parameters.Add(new SqlParameter("@CustomerName", $"%{customerName}%"));
            }

            if (status.HasValue)
            {
                conditions.Add("d.Status = @Status");
                parameters.Add(new SqlParameter("@Status", (int)status.Value));
            }

            if (startDate.HasValue)
            {
                conditions.Add("d.CreateTime >= @StartDate");
                parameters.Add(new SqlParameter("@StartDate", startDate.Value));
            }

            if (endDate.HasValue)
            {
                conditions.Add("d.CreateTime <= @EndDate");
                parameters.Add(new SqlParameter("@EndDate", endDate.Value.Date.AddDays(1).AddSeconds(-1)));
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
            
            var baseSql = $@"
                SELECT d.*, u.RealName as CreateUserName 
                FROM DM_DieInfo d WITH (NOLOCK)
                LEFT JOIN DM_User u WITH (NOLOCK) ON d.CreateUser = u.Username
                {whereClause}";

            var countCacheKey = $"DieList_Count:{dieCode ?? "all"}:{customerName ?? "all"}:{status?.ToString() ?? "all"}";

            return PaginationHelper.ExecutePagedQueryWithCountCache(
                baseSql,
                "d.CreateTime DESC",
                pageIndex,
                pageSize,
                countCacheKey,
                MapToDieInfo,
                parameters.ToArray());
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "分页搜索刀模");
            return new PagedResult<DieInfo>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "分页搜索刀模");
            return new PagedResult<DieInfo>();
        }
    }

    /// <summary>
    /// 获取所有刀模列表（使用分页）
    /// </summary>
    public PagedResult<DieInfo> GetAllDiesPaged(int pageIndex = 1, int pageSize = 20)
    {
        try
        {
            var baseSql = @"
                SELECT d.*, u.RealName as CreateUserName 
                FROM DM_DieInfo d WITH (NOLOCK)
                LEFT JOIN DM_User u WITH (NOLOCK) ON d.CreateUser = u.Username";

            return PaginationHelper.ExecutePagedQuery(
                baseSql,
                "d.CreateTime DESC",
                pageIndex,
                pageSize,
                MapToDieInfo);
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "分页获取刀模列表");
            return new PagedResult<DieInfo>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "分页获取刀模列表");
            return new PagedResult<DieInfo>();
        }
    }

    #endregion

    #region 批量操作优化

    /// <summary>
    /// 批量导入刀模（使用SqlBulkCopy）
    /// </summary>
    public BulkOperationResult BulkImportDies(List<DieImportModel> dies, IProgress<BulkProgress>? progress = null)
    {
        if (dies == null || dies.Count == 0)
        {
            return new BulkOperationResult
            {
                Success = false,
                Message = "没有数据需要导入"
            };
        }

        try
        {
            // 构建列映射
            var columnMappings = new Dictionary<string, string>
            {
                { "DieCode", "DieCode" },
                { "CustomerName", "CustomerName" },
                { "ProductName", "ProductName" },
                { "Structure", "Structure" },
                { "ModelType", "ModelType" },
                { "LayoutType", "LayoutType" },
                { "FluteType", "FluteType" },
                { "Material", "Material" },
                { "ManufactureLength", "ManufactureLength" },
                { "ManufactureWidth", "ManufactureWidth" },
                { "ManufactureHeight", "ManufactureHeight" },
                { "BlankLength", "BlankLength" },
                { "BlankWidth", "BlankWidth" },
                { "ProcessDesc", "ProcessDesc" },
                { "RequiredProcesses", "RequiredProcesses" },
                { "Status", "Status" },
                { "SourceFactory", "SourceFactory" },
                { "ExternalOrderID", "ExternalOrderID" },
                { "DeliveryDate", "DeliveryDate" },
                { "CreateUser", "CreateUser" },
                { "Remark", "Remark" }
            };

            // 准备数据
            var importData = dies.Select(d => new
            {
                d.DieCode,
                d.CustomerName,
                d.ProductName,
                d.Structure,
                d.ModelType,
                d.LayoutType,
                d.FluteType,
                d.Material,
                d.ManufactureLength,
                d.ManufactureWidth,
                d.ManufactureHeight,
                d.BlankLength,
                d.BlankWidth,
                d.ProcessDesc,
                d.RequiredProcesses,
                Status = (int)DieStatus.Pending,
                d.SourceFactory,
                d.ExternalOrderID,
                d.DeliveryDate,
                CreateUser = CurrentUser.User?.Username ?? "system",
                d.Remark
            }).ToList();

            // 使用批量插入
            var result = BulkOperationHelper.BulkInsert(importData, "DM_DieInfo", columnMappings, 5000);

            // 使相关缓存失效
            if (result.Success)
            {
                PaginationHelper.ClearCountCache();
                QueryCacheHelper.InvalidateStatsCache();
            }

            return result;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "批量导入刀模");
            return new BulkOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Message = $"批量导入失败：{ex.Message}"
            };
        }
    }

    /// <summary>
    /// 异步批量导入刀模
    /// </summary>
    public async Task<BulkOperationResult> BulkImportDiesAsync(List<DieImportModel> dies, IProgress<BulkProgress>? progress = null)
    {
        if (dies == null || dies.Count == 0)
        {
            return new BulkOperationResult
            {
                Success = false,
                Message = "没有数据需要导入"
            };
        }

        try
        {
            var columnMappings = new Dictionary<string, string>
            {
                { "DieCode", "DieCode" },
                { "CustomerName", "CustomerName" },
                { "ProductName", "ProductName" },
                { "Structure", "Structure" },
                { "ModelType", "ModelType" },
                { "LayoutType", "LayoutType" },
                { "FluteType", "FluteType" },
                { "Material", "Material" },
                { "ManufactureLength", "ManufactureLength" },
                { "ManufactureWidth", "ManufactureWidth" },
                { "ManufactureHeight", "ManufactureHeight" },
                { "BlankLength", "BlankLength" },
                { "BlankWidth", "BlankWidth" },
                { "ProcessDesc", "ProcessDesc" },
                { "RequiredProcesses", "RequiredProcesses" },
                { "Status", "Status" },
                { "SourceFactory", "SourceFactory" },
                { "ExternalOrderID", "ExternalOrderID" },
                { "DeliveryDate", "DeliveryDate" },
                { "CreateUser", "CreateUser" },
                { "Remark", "Remark" }
            };

            var importData = dies.Select(d => new
            {
                d.DieCode,
                d.CustomerName,
                d.ProductName,
                d.Structure,
                d.ModelType,
                d.LayoutType,
                d.FluteType,
                d.Material,
                d.ManufactureLength,
                d.ManufactureWidth,
                d.ManufactureHeight,
                d.BlankLength,
                d.BlankWidth,
                d.ProcessDesc,
                d.RequiredProcesses,
                Status = (int)DieStatus.Pending,
                d.SourceFactory,
                d.ExternalOrderID,
                d.DeliveryDate,
                CreateUser = CurrentUser.User?.Username ?? "system",
                d.Remark
            }).ToList();

            var result = await BulkOperationHelper.BulkInsertAsync(importData, "DM_DieInfo", columnMappings, 5000, progress);

            if (result.Success)
            {
                PaginationHelper.ClearCountCache();
                QueryCacheHelper.InvalidateStatsCache();
            }

            return result;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "异步批量导入刀模");
            return new BulkOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Message = $"批量导入失败：{ex.Message}"
            };
        }
    }

    /// <summary>
    /// 批量更新刀模状态
    /// </summary>
    public BulkOperationResult BulkUpdateStatus(List<int> dieIds, DieStatus status)
    {
        if (dieIds == null || dieIds.Count == 0)
        {
            return new BulkOperationResult
            {
                Success = false,
                Message = "没有数据需要更新"
            };
        }

        try
        {
            // 构建批量更新数据
            var updateData = dieIds.Select(id => new { DieID = id, Status = (int)status, UpdateTime = DateTime.Now }).ToList();

            var result = BulkOperationHelper.BulkUpdate(
                updateData,
                "DM_DieInfo",
                "DieID",
                new List<string> { "Status", "UpdateTime" },
                1000);

            if (result.Success)
            {
                PaginationHelper.ClearCountCache();
                QueryCacheHelper.InvalidateStatsCache();
            }

            return result;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "批量更新刀模状态");
            return new BulkOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Message = $"批量更新失败：{ex.Message}"
            };
        }
    }

    /// <summary>
    /// 批量删除刀模
    /// </summary>
    public BulkOperationResult BulkDeleteDies(List<int> dieIds)
    {
        if (dieIds == null || dieIds.Count == 0)
        {
            return new BulkOperationResult
            {
                Success = false,
                Message = "没有数据需要删除"
            };
        }

        try
        {
            var result = BulkOperationHelper.BulkDelete(dieIds, "DM_DieInfo", "DieID", 1000);

            if (result.Success)
            {
                PaginationHelper.ClearCountCache();
                QueryCacheHelper.InvalidateStatsCache();
            }

            return result;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "批量删除刀模");
            return new BulkOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Message = $"批量删除失败：{ex.Message}"
            };
        }
    }

    #endregion

    #region 虚拟模式数据加载

    /// <summary>
    /// 异步加载刀模数据（用于虚拟模式DataGridView）
    /// </summary>
    public async Task<List<DieInfo>> LoadDiesAsync(
        string? dieCode = null,
        string? customerName = null,
        DieStatus? status = null,
        IProgress<LoadingProgress>? progress = null)
    {
        try
        {
            progress?.Report(new LoadingProgress { PercentComplete = 0, Message = "正在查询数据..." });

            var conditions = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(dieCode))
            {
                conditions.Add("d.DieCode LIKE @DieCode");
                parameters.Add(new SqlParameter("@DieCode", $"%{dieCode}%"));
            }

            if (!string.IsNullOrWhiteSpace(customerName))
            {
                conditions.Add("d.CustomerName LIKE @CustomerName");
                parameters.Add(new SqlParameter("@CustomerName", $"%{customerName}%"));
            }

            if (status.HasValue)
            {
                conditions.Add("d.Status = @Status");
                parameters.Add(new SqlParameter("@Status", (int)status.Value));
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
            
            var sql = $@"
                SELECT d.*, u.RealName as CreateUserName 
                FROM DM_DieInfo d WITH (NOLOCK)
                LEFT JOIN DM_User u WITH (NOLOCK) ON d.CreateUser = u.Username
                {whereClause}
                ORDER BY d.CreateTime DESC";

            progress?.Report(new LoadingProgress { PercentComplete = 30, Message = "正在读取数据..." });

            var result = await DbHelper.ExecuteQueryAsync(sql, MapToDieInfo, parameters.ToArray());

            progress?.Report(new LoadingProgress { PercentComplete = 100, Message = $"加载完成，共 {result.Count} 条记录" });

            return result;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "异步加载刀模数据");
            return new List<DieInfo>();
        }
    }

    #endregion

    #region 私有方法

    private DieInfo MapToDieInfo(SqlDataReader reader)
    {
        return new DieInfo
        {
            DieID = ConvertHelper.ToInt(reader["DieID"]),
            DieCode = ConvertHelper.ToString(reader["DieCode"]),
            CustomerName = ConvertHelper.ToString(reader["CustomerName"]),
            ProductName = ConvertHelper.ToString(reader["ProductName"]),
            Structure = ConvertHelper.ToString(reader["Structure"]),
            ModelType = ConvertHelper.ToString(reader["ModelType"]),
            LayoutType = ConvertHelper.ToString(reader["LayoutType"]),
            FluteType = ConvertHelper.ToString(reader["FluteType"]),
            Material = ConvertHelper.ToString(reader["Material"]),
            ManufactureLength = ConvertHelper.ToDecimal(reader["ManufactureLength"]),
            ManufactureWidth = ConvertHelper.ToDecimal(reader["ManufactureWidth"]),
            ManufactureHeight = ConvertHelper.ToDecimal(reader["ManufactureHeight"]),
            BlankLength = ConvertHelper.ToDecimal(reader["BlankLength"]),
            BlankWidth = ConvertHelper.ToDecimal(reader["BlankWidth"]),
            ProcessDesc = ConvertHelper.ToString(reader["ProcessDesc"]),
            RequiredProcesses = ConvertHelper.ToString(reader["RequiredProcesses"]),
            Status = ConvertHelper.ToEnum(reader["Status"], DieStatus.Pending),
            SourceFactory = ConvertHelper.ToString(reader["SourceFactory"]),
            ExternalOrderID = ConvertHelper.ToNullableInt(reader["ExternalOrderID"]),
            DeliveryDate = ConvertHelper.ToNullableDateTime(reader["DeliveryDate"]),
            CreateTime = ConvertHelper.ToDateTime(reader["CreateTime"], DateTime.Now),
            CreateUser = ConvertHelper.ToString(reader["CreateUser"]),
            UpdateTime = ConvertHelper.ToNullableDateTime(reader["UpdateTime"]),
            Remark = ConvertHelper.ToString(reader["Remark"])
        };
    }

    #endregion
}

/// <summary>
/// 刀模导入模型
/// </summary>
public class DieImportModel
{
    public string DieCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Structure { get; set; }
    public string? ModelType { get; set; }
    public string? LayoutType { get; set; }
    public string? FluteType { get; set; }
    public string? Material { get; set; }
    public decimal? ManufactureLength { get; set; }
    public decimal? ManufactureWidth { get; set; }
    public decimal? ManufactureHeight { get; set; }
    public decimal? BlankLength { get; set; }
    public decimal? BlankWidth { get; set; }
    public string? ProcessDesc { get; set; }
    public string? RequiredProcesses { get; set; }
    public string? SourceFactory { get; set; }
    public int? ExternalOrderID { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? Remark { get; set; }
}
