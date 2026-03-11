using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace DieMaking.Helpers;

/// <summary>
/// 分页查询帮助类 - 提供优化的分页查询功能
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// 总记录数缓存字典
    /// </summary>
    private static readonly Dictionary<string, CountCacheItem> _countCache = new();

    /// <summary>
    /// 缓存过期时间（分钟）
    /// </summary>
    public static int CountCacheExpirationMinutes { get; set; } = 2;

    /// <summary>
    /// 是否启用总记录数缓存
    /// </summary>
    public static bool EnableCountCache { get; set; } = true;

    /// <summary>
    /// 执行分页查询（使用 OFFSET FETCH 语法）
    /// </summary>
    public static PagedResult<T> ExecutePagedQuery<T>(
        string baseSql,
        string orderBy,
        int pageIndex,
        int pageSize,
        Func<SqlDataReader, T> mapper,
        params SqlParameter[] parameters)
    {
        return ExecutePagedQuery<T>(baseSql, orderBy, pageIndex, pageSize, 60, mapper, parameters);
    }

    /// <summary>
    /// 执行分页查询（带超时设置）
    /// </summary>
    public static PagedResult<T> ExecutePagedQuery<T>(
        string baseSql,
        string orderBy,
        int pageIndex,
        int pageSize,
        int commandTimeout,
        Func<SqlDataReader, T> mapper,
        params SqlParameter[] parameters)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new PagedResult<T>();

        try
        {
            // 参数校验
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 1000) pageSize = 1000; // 限制最大页大小

            var offset = (pageIndex - 1) * pageSize;

            // 构建分页SQL - 将ORDER BY放在CTE内部，避免外部无法识别表别名的问题
            var pagedSql = $@"
                WITH PagedData AS (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY {orderBy}) AS RowNum
                    FROM (
                        {baseSql}
                    ) AS InnerQuery
                )
                SELECT * FROM PagedData
                WHERE RowNum > @Offset AND RowNum <= @Offset + @PageSize;

                SELECT COUNT(*) FROM (
                    {baseSql}
                ) AS CountQuery;";

            var pagedParameters = parameters.ToList();
            pagedParameters.Add(new SqlParameter("@Offset", offset));
            pagedParameters.Add(new SqlParameter("@PageSize", pageSize));

            using var connection = DbHelper.CreateConnection();
            connection.Open();

            using var command = new SqlCommand(pagedSql, connection);
            command.CommandTimeout = commandTimeout;
            command.Parameters.AddRange(pagedParameters.ToArray());

            // 读取分页数据
            var items = new List<T>();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(mapper(reader));
                }

                // 读取总数
                if (reader.NextResult() && reader.Read())
                {
                    result.TotalCount = Convert.ToInt32(reader[0]);
                }
            }

            result.Items = items;
            result.PageIndex = pageIndex;
            result.PageSize = pageSize;
            result.TotalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize);
            result.HasPreviousPage = pageIndex > 1;
            result.HasNextPage = pageIndex < result.TotalPages;

            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.ErrorMessage = ex.Message;
            throw;
        }
    }

    /// <summary>
    /// 异步执行分页查询
    /// </summary>
    public static async Task<PagedResult<T>> ExecutePagedQueryAsync<T>(
        string baseSql,
        string orderBy,
        int pageIndex,
        int pageSize,
        Func<SqlDataReader, T> mapper,
        params SqlParameter[] parameters)
    {
        return await ExecutePagedQueryAsync<T>(baseSql, orderBy, pageIndex, pageSize, 60, mapper, parameters);
    }

    /// <summary>
    /// 异步执行分页查询（带超时设置）
    /// </summary>
    public static async Task<PagedResult<T>> ExecutePagedQueryAsync<T>(
        string baseSql,
        string orderBy,
        int pageIndex,
        int pageSize,
        int commandTimeout,
        Func<SqlDataReader, T> mapper,
        params SqlParameter[] parameters)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new PagedResult<T>();

        try
        {
            // 参数校验
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 1000) pageSize = 1000;

            var offset = (pageIndex - 1) * pageSize;

            // 构建分页SQL - 将ORDER BY放在CTE内部，避免外部无法识别表别名的问题
            var pagedSql = $@"
                WITH PagedData AS (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY {orderBy}) AS RowNum
                    FROM (
                        {baseSql}
                    ) AS InnerQuery
                )
                SELECT * FROM PagedData
                WHERE RowNum > @Offset AND RowNum <= @Offset + @PageSize;

                SELECT COUNT(*) FROM (
                    {baseSql}
                ) AS CountQuery;";

            var pagedParameters = parameters.ToList();
            pagedParameters.Add(new SqlParameter("@Offset", offset));
            pagedParameters.Add(new SqlParameter("@PageSize", pageSize));

            await using var connection = await DbHelper.CreateAndOpenConnectionAsync();
            await using var command = new SqlCommand(pagedSql, connection);
            command.CommandTimeout = commandTimeout;
            command.Parameters.AddRange(pagedParameters.ToArray());

            // 读取分页数据
            var items = new List<T>();
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    items.Add(mapper(reader));
                }

                // 读取总数
                if (await reader.NextResultAsync() && await reader.ReadAsync())
                {
                    result.TotalCount = Convert.ToInt32(reader[0]);
                }
            }

            result.Items = items;
            result.PageIndex = pageIndex;
            result.PageSize = pageSize;
            result.TotalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize);
            result.HasPreviousPage = pageIndex > 1;
            result.HasNextPage = pageIndex < result.TotalPages;

            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.ErrorMessage = ex.Message;
            throw;
        }
    }

    /// <summary>
    /// 执行带缓存的分页查询
    /// </summary>
    public static PagedResult<T> ExecutePagedQueryWithCountCache<T>(
        string baseSql,
        string orderBy,
        int pageIndex,
        int pageSize,
        string countCacheKey,
        Func<SqlDataReader, T> mapper,
        params SqlParameter[] parameters)
    {
        return ExecutePagedQueryWithCountCache<T>(baseSql, orderBy, pageIndex, pageSize, countCacheKey, 60, mapper, parameters);
    }

    /// <summary>
    /// 执行带缓存的分页查询（带超时设置）
    /// </summary>
    public static PagedResult<T> ExecutePagedQueryWithCountCache<T>(
        string baseSql,
        string orderBy,
        int pageIndex,
        int pageSize,
        string countCacheKey,
        int commandTimeout,
        Func<SqlDataReader, T> mapper,
        params SqlParameter[] parameters)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new PagedResult<T>();

        try
        {
            // 参数校验
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 1000) pageSize = 1000;

            var offset = (pageIndex - 1) * pageSize;

            // 尝试从缓存获取总记录数
            int totalCount = GetCachedCount(countCacheKey);
            bool countFromCache = totalCount >= 0;

            if (!countFromCache)
            {
                // 缓存未命中，查询总数
                using var countConnection = DbHelper.CreateConnection();
                countConnection.Open();
                var countSql = $"SELECT COUNT(*) FROM ({baseSql}) AS CountQuery";
                using var countCommand = new SqlCommand(countSql, countConnection);
                countCommand.Parameters.AddRange(parameters);
                totalCount = Convert.ToInt32(countCommand.ExecuteScalar());
                
                // 缓存总记录数
                SetCachedCount(countCacheKey, totalCount);
            }

            result.TotalCount = totalCount;
            result.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            result.HasPreviousPage = pageIndex > 1;
            result.HasNextPage = pageIndex < result.TotalPages;

            // 查询分页数据 - 将ORDER BY放在CTE内部，避免外部无法识别表别名的问题
            var pagedSql = $@"
                WITH PagedData AS (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY {orderBy}) AS RowNum
                    FROM (
                        {baseSql}
                    ) AS InnerQuery
                )
                SELECT * FROM PagedData
                WHERE RowNum > @Offset AND RowNum <= @Offset + @PageSize";

            var pagedParameters = parameters.ToList();
            pagedParameters.Add(new SqlParameter("@Offset", offset));
            pagedParameters.Add(new SqlParameter("@PageSize", pageSize));

            using var connection = DbHelper.CreateConnection();
            connection.Open();

            using var command = new SqlCommand(pagedSql, connection);
            command.CommandTimeout = commandTimeout;
            command.Parameters.AddRange(pagedParameters.ToArray());

            var items = new List<T>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                items.Add(mapper(reader));
            }

            result.Items = items;
            result.PageIndex = pageIndex;
            result.PageSize = pageSize;
            result.CountFromCache = countFromCache;

            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.ErrorMessage = ex.Message;
            throw;
        }
    }

    /// <summary>
    /// 获取缓存的总记录数
    /// </summary>
    private static int GetCachedCount(string cacheKey)
    {
        if (!EnableCountCache) return -1;

        lock (_countCache)
        {
            if (_countCache.TryGetValue(cacheKey, out var item))
            {
                if (DateTime.Now - item.CachedAt < TimeSpan.FromMinutes(CountCacheExpirationMinutes))
                {
                    return item.Count;
                }
                // 过期移除
                _countCache.Remove(cacheKey);
            }
        }

        return -1;
    }

    /// <summary>
    /// 设置缓存的总记录数
    /// </summary>
    private static void SetCachedCount(string cacheKey, int count)
    {
        if (!EnableCountCache) return;

        lock (_countCache)
        {
            _countCache[cacheKey] = new CountCacheItem
            {
                Count = count,
                CachedAt = DateTime.Now
            };

            // 清理过期缓存
            CleanupExpiredCountCache();
        }
    }

    /// <summary>
    /// 清理过期的总记录数缓存
    /// </summary>
    private static void CleanupExpiredCountCache()
    {
        var expiredKeys = _countCache
            .Where(x => DateTime.Now - x.Value.CachedAt > TimeSpan.FromMinutes(CountCacheExpirationMinutes))
            .Select(x => x.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _countCache.Remove(key);
        }
    }

    /// <summary>
    /// 使总记录数缓存失效
    /// </summary>
    public static void InvalidateCountCache(string cacheKey)
    {
        lock (_countCache)
        {
            _countCache.Remove(cacheKey);
        }
    }

    /// <summary>
    /// 清空所有总记录数缓存
    /// </summary>
    public static void ClearCountCache()
    {
        lock (_countCache)
        {
            _countCache.Clear();
        }
    }

    /// <summary>
    /// 构建分页参数
    /// </summary>
    public static PaginationParams BuildPaginationParams(int pageIndex, int pageSize, int totalCount)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 1000) pageSize = 1000;

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        if (totalPages == 0) totalPages = 1;
        if (pageIndex > totalPages) pageIndex = totalPages;

        return new PaginationParams
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Offset = (pageIndex - 1) * pageSize,
            HasPreviousPage = pageIndex > 1,
            HasNextPage = pageIndex < totalPages
        };
    }
}

/// <summary>
/// 分页参数
/// </summary>
public class PaginationParams
{
    /// <summary>当前页码</summary>
    public int PageIndex { get; set; }
    
    /// <summary>每页大小</summary>
    public int PageSize { get; set; }
    
    /// <summary>总记录数</summary>
    public int TotalCount { get; set; }
    
    /// <summary>总页数</summary>
    public int TotalPages { get; set; }
    
    /// <summary>偏移量</summary>
    public int Offset { get; set; }
    
    /// <summary>是否有上一页</summary>
    public bool HasPreviousPage { get; set; }
    
    /// <summary>是否有下一页</summary>
    public bool HasNextPage { get; set; }
}

/// <summary>
/// 分页查询结果
/// </summary>
public class PagedResult<T>
{
    /// <summary>数据列表</summary>
    public List<T> Items { get; set; } = new();
    
    /// <summary>总记录数</summary>
    public int TotalCount { get; set; }
    
    /// <summary>当前页码</summary>
    public int PageIndex { get; set; }
    
    /// <summary>每页大小</summary>
    public int PageSize { get; set; }
    
    /// <summary>总页数</summary>
    public int TotalPages { get; set; }
    
    /// <summary>是否有上一页</summary>
    public bool HasPreviousPage => PageIndex > 1;
    
    /// <summary>是否有下一页</summary>
    public bool HasNextPage => PageIndex < TotalPages;
    
    /// <summary>执行时间（毫秒）</summary>
    public long ExecutionTimeMs { get; set; }
    
    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>总记录数是否来自缓存</summary>
    public bool CountFromCache { get; set; }
}

/// <summary>
/// 总记录数缓存项
/// </summary>
internal class CountCacheItem
{
    public int Count { get; set; }
    public DateTime CachedAt { get; set; }
}

/// <summary>
/// 分页控件
/// </summary>
public class PaginationControl : UserControl
{
    private Button _btnFirst = null!;
    private Button _btnPrev = null!;
    private Button _btnNext = null!;
    private Button _btnLast = null!;
    private Label _lblPageInfo = null!;
    private ComboBox _cmbPageSize = null!;

    /// <summary>
    /// 当前页码
    /// </summary>
    public int PageIndex { get; private set; } = 1;

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; private set; } = 20;

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; private set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// 页码改变事件
    /// </summary>
    public event EventHandler<PageChangedEventArgs>? PageChanged;

    /// <summary>
    /// 每页大小改变事件
    /// </summary>
    public event EventHandler<PageSizeChangedEventArgs>? PageSizeChanged;

    public PaginationControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Height = 40;
        this.Dock = DockStyle.Bottom;

        // 首页按钮
        _btnFirst = new Button
        {
            Text = "首页",
            Size = new Size(60, 28),
            Location = new Point(10, 6)
        };
        _btnFirst.Click += (s, e) => GoToPage(1);

        // 上一页按钮
        _btnPrev = new Button
        {
            Text = "上一页",
            Size = new Size(60, 28),
            Location = new Point(75, 6)
        };
        _btnPrev.Click += (s, e) => GoToPage(PageIndex - 1);

        // 页码信息
        _lblPageInfo = new Label
        {
            Text = "第 1 页 / 共 1 页 (共 0 条)",
            Location = new Point(145, 10),
            Size = new Size(200, 23),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // 下一页按钮
        _btnNext = new Button
        {
            Text = "下一页",
            Size = new Size(60, 28),
            Location = new Point(355, 6)
        };
        _btnNext.Click += (s, e) => GoToPage(PageIndex + 1);

        // 末页按钮
        _btnLast = new Button
        {
            Text = "末页",
            Size = new Size(60, 28),
            Location = new Point(420, 6)
        };
        _btnLast.Click += (s, e) => GoToPage(TotalPages);

        // 每页大小选择
        var lblPageSize = new Label
        {
            Text = "每页：",
            Location = new Point(490, 10),
            Size = new Size(45, 23),
            TextAlign = ContentAlignment.MiddleRight
        };

        _cmbPageSize = new ComboBox
        {
            Location = new Point(540, 6),
            Size = new Size(60, 28),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbPageSize.Items.AddRange(new object[] { 10, 20, 50, 100, 200 });
        _cmbPageSize.SelectedIndex = 1; // 默认20
        _cmbPageSize.SelectedIndexChanged += CmbPageSize_SelectedIndexChanged;

        this.Controls.Add(_btnFirst);
        this.Controls.Add(_btnPrev);
        this.Controls.Add(_lblPageInfo);
        this.Controls.Add(_btnNext);
        this.Controls.Add(_btnLast);
        this.Controls.Add(lblPageSize);
        this.Controls.Add(_cmbPageSize);

        UpdateButtonStates();
    }

    /// <summary>
    /// 设置分页数据
    /// </summary>
    public void SetPagination(int pageIndex, int pageSize, int totalCount)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalCount = totalCount;

        UpdateDisplay();
    }

    /// <summary>
    /// 更新显示
    /// </summary>
    private void UpdateDisplay()
    {
        var totalPages = TotalPages;
        if (totalPages == 0) totalPages = 1;

        _lblPageInfo.Text = $"第 {PageIndex} 页 / 共 {totalPages} 页 (共 {TotalCount} 条)";
        UpdateButtonStates();
    }

    /// <summary>
    /// 更新按钮状态
    /// </summary>
    private void UpdateButtonStates()
    {
        _btnFirst.Enabled = PageIndex > 1;
        _btnPrev.Enabled = PageIndex > 1;
        _btnNext.Enabled = PageIndex < TotalPages;
        _btnLast.Enabled = PageIndex < TotalPages;
    }

    /// <summary>
    /// 跳转到指定页
    /// </summary>
    private void GoToPage(int page)
    {
        var totalPages = TotalPages;
        if (totalPages == 0) totalPages = 1;

        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        if (page != PageIndex)
        {
            PageIndex = page;
            UpdateDisplay();
            PageChanged?.Invoke(this, new PageChangedEventArgs { PageIndex = PageIndex, PageSize = PageSize });
        }
    }

    /// <summary>
    /// 每页大小改变
    /// </summary>
    private void CmbPageSize_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_cmbPageSize.SelectedItem != null)
        {
            var newPageSize = (int)_cmbPageSize.SelectedItem;
            if (newPageSize != PageSize)
            {
                PageSize = newPageSize;
                PageIndex = 1; // 重置到第一页
                UpdateDisplay();
                PageSizeChanged?.Invoke(this, new PageSizeChangedEventArgs { PageSize = PageSize });
            }
        }
    }
}

/// <summary>
/// 页码改变事件参数
/// </summary>
public class PageChangedEventArgs : EventArgs
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// 每页大小改变事件参数
/// </summary>
public class PageSizeChangedEventArgs : EventArgs
{
    public int PageSize { get; set; }
}
