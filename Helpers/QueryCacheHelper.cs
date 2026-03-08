using System.Collections.Concurrent;
using System.Diagnostics;

namespace DieMaking.Helpers;

/// <summary>
/// 查询缓存帮助类 - 提供统计报表查询结果的缓存功能
/// </summary>
public static class QueryCacheHelper
{
    /// <summary>
    /// 缓存项
    /// </summary>
    private class CacheItem
    {
        public object? Data { get; set; }
        public DateTime CachedAt { get; set; }
        public TimeSpan Expiration { get; set; }
        public string CacheKey { get; set; } = string.Empty;
        public long HitCount { get; set; }
        public long MissCount { get; set; }
        public bool IsExpired => DateTime.Now - CachedAt > Expiration;
    }

    // 缓存存储
    private static readonly ConcurrentDictionary<string, CacheItem> _cache = new();
    private static readonly ConcurrentDictionary<string, DateTime> _countCache = new();
    
    // 缓存统计
    private static long _totalHits = 0;
    private static long _totalMisses = 0;
    private static long _totalEvictions = 0;

    /// <summary>
    /// 默认缓存过期时间
    /// </summary>
    public static TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 统计报表缓存过期时间
    /// </summary>
    public static TimeSpan StatsExpiration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 总记录数缓存过期时间
    /// </summary>
    public static TimeSpan CountExpiration { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// 最大缓存项数
    /// </summary>
    public static int MaxCacheItems { get; set; } = 100;

    /// <summary>
    /// 是否启用缓存
    /// </summary>
    public static bool IsEnabled { get; set; } = true;

    #region 缓存操作

    /// <summary>
    /// 获取或创建缓存
    /// </summary>
    public static T? GetOrCreate<T>(string cacheKey, Func<T> factory, TimeSpan? expiration = null) where T : class
    {
        if (!IsEnabled)
        {
            return factory();
        }

        var exp = expiration ?? DefaultExpiration;
        
        // 尝试从缓存获取
        if (_cache.TryGetValue(cacheKey, out var item))
        {
            if (!item.IsExpired)
            {
                Interlocked.Increment(ref _totalHits);
                item.HitCount++;
                return item.Data as T;
            }
            // 过期移除
            _cache.TryRemove(cacheKey, out _);
            Interlocked.Increment(ref _totalEvictions);
        }

        // 执行查询
        Interlocked.Increment(ref _totalMisses);
        var stopwatch = Stopwatch.StartNew();
        var data = factory();
        stopwatch.Stop();

        // 缓存结果
        if (data != null)
        {
            EnsureCacheSize();
            _cache[cacheKey] = new CacheItem
            {
                Data = data,
                CachedAt = DateTime.Now,
                Expiration = exp,
                CacheKey = cacheKey,
                HitCount = 0,
                MissCount = 1
            };
        }

        return data;
    }

    /// <summary>
    /// 异步获取或创建缓存
    /// </summary>
    public static async Task<T?> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
    {
        if (!IsEnabled)
        {
            return await factory();
        }

        var exp = expiration ?? DefaultExpiration;
        
        // 尝试从缓存获取
        if (_cache.TryGetValue(cacheKey, out var item))
        {
            if (!item.IsExpired)
            {
                Interlocked.Increment(ref _totalHits);
                item.HitCount++;
                return item.Data as T;
            }
            // 过期移除
            _cache.TryRemove(cacheKey, out _);
            Interlocked.Increment(ref _totalEvictions);
        }

        // 执行查询
        Interlocked.Increment(ref _totalMisses);
        var stopwatch = Stopwatch.StartNew();
        var data = await factory();
        stopwatch.Stop();

        // 缓存结果
        if (data != null)
        {
            EnsureCacheSize();
            _cache[cacheKey] = new CacheItem
            {
                Data = data,
                CachedAt = DateTime.Now,
                Expiration = exp,
                CacheKey = cacheKey,
                HitCount = 0,
                MissCount = 1
            };
        }

        return data;
    }

    /// <summary>
    /// 获取缓存
    /// </summary>
    public static T? Get<T>(string cacheKey) where T : class
    {
        if (!IsEnabled) return null;

        if (_cache.TryGetValue(cacheKey, out var item) && !item.IsExpired)
        {
            Interlocked.Increment(ref _totalHits);
            item.HitCount++;
            return item.Data as T;
        }

        return null;
    }

    /// <summary>
    /// 设置缓存
    /// </summary>
    public static void Set<T>(string cacheKey, T data, TimeSpan? expiration = null) where T : class
    {
        if (!IsEnabled || data == null) return;

        EnsureCacheSize();
        
        _cache[cacheKey] = new CacheItem
        {
            Data = data,
            CachedAt = DateTime.Now,
            Expiration = expiration ?? DefaultExpiration,
            CacheKey = cacheKey,
            HitCount = 0,
            MissCount = 0
        };
    }

    /// <summary>
    /// 移除缓存
    /// </summary>
    public static void Remove(string cacheKey)
    {
        _cache.TryRemove(cacheKey, out _);
    }

    /// <summary>
    /// 根据前缀移除缓存
    /// </summary>
    public static void RemoveByPrefix(string prefix)
    {
        var keysToRemove = _cache.Keys.Where(k => k.StartsWith(prefix)).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    public static void Clear()
    {
        _cache.Clear();
        _countCache.Clear();
        Interlocked.Exchange(ref _totalHits, 0);
        Interlocked.Exchange(ref _totalMisses, 0);
        Interlocked.Exchange(ref _totalEvictions, 0);
    }

    #endregion

    #region 总记录数缓存

    /// <summary>
    /// 获取或缓存总记录数
    /// </summary>
    public static int GetOrCacheCount(string tableName, Func<int> countFactory)
    {
        var cacheKey = $"Count:{tableName}";
        
        if (_countCache.TryGetValue(cacheKey, out var cachedTime))
        {
            if (DateTime.Now - cachedTime < CountExpiration)
            {
                // 从统计缓存获取（这里简化处理，实际应该缓存具体数值）
            }
        }

        var count = countFactory();
        _countCache[cacheKey] = DateTime.Now;
        return count;
    }

    /// <summary>
    /// 使表的总记录数缓存失效
    /// </summary>
    public static void InvalidateCountCache(string tableName)
    {
        _countCache.TryRemove($"Count:{tableName}", out _);
    }

    /// <summary>
    /// 使多个表的总记录数缓存失效
    /// </summary>
    public static void InvalidateCountCache(params string[] tableNames)
    {
        foreach (var tableName in tableNames)
        {
            InvalidateCountCache(tableName);
        }
    }

    #endregion

    #region 报表数据缓存专用方法

    /// <summary>
    /// 获取或缓存完工统计数据
    /// </summary>
    public static List<T> GetOrCacheCompletionStats<T>(DateTime startDate, DateTime endDate, Func<List<T>> factory) where T : class
    {
        var cacheKey = $"CompletionStats:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
        return GetOrCreate(cacheKey, factory, StatsExpiration) ?? new List<T>();
    }

    /// <summary>
    /// 获取或缓存工序统计数据
    /// </summary>
    public static List<T> GetOrCacheProcessStats<T>(DateTime? startDate, DateTime? endDate, string? processName, Func<List<T>> factory) where T : class
    {
        var cacheKey = $"ProcessStats:{startDate?.ToString("yyyyMMdd") ?? "null"}:{endDate?.ToString("yyyyMMdd") ?? "null"}:{processName ?? "all"}";
        return GetOrCreate(cacheKey, factory, StatsExpiration) ?? new List<T>();
    }

    /// <summary>
    /// 获取或缓存库存统计数据
    /// </summary>
    public static T? GetOrCacheInventoryStats<T>(Func<T> factory) where T : class
    {
        var cacheKey = "InventoryStats:Summary";
        return GetOrCreate(cacheKey, factory, StatsExpiration);
    }

    /// <summary>
    /// 获取或缓存库位分布统计
    /// </summary>
    public static List<T> GetOrCacheLocationDistribution<T>(Func<List<T>> factory) where T : class
    {
        var cacheKey = "LocationDistribution:All";
        return GetOrCreate(cacheKey, factory, StatsExpiration) ?? new List<T>();
    }

    /// <summary>
    /// 获取或缓存借用记录统计
    /// </summary>
    public static List<T> GetOrCacheBorrowStats<T>(DateTime? startDate, DateTime? endDate, Func<List<T>> factory) where T : class
    {
        var cacheKey = $"BorrowStats:{startDate?.ToString("yyyyMMdd") ?? "null"}:{endDate?.ToString("yyyyMMdd") ?? "null"}";
        return GetOrCreate(cacheKey, factory, StatsExpiration) ?? new List<T>();
    }

    /// <summary>
    /// 获取或缓存生产看板数据
    /// </summary>
    public static T? GetOrCacheProductionBoard<T>(Func<T> factory) where T : class
    {
        var cacheKey = "ProductionBoard:Current";
        return GetOrCreate(cacheKey, factory, TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// 使报表缓存失效
    /// </summary>
    public static void InvalidateStatsCache()
    {
        var keysToRemove = _cache.Keys.Where(k => 
            k.StartsWith("CompletionStats:") ||
            k.StartsWith("ProcessStats:") ||
            k.StartsWith("InventoryStats:") ||
            k.StartsWith("LocationDistribution:") ||
            k.StartsWith("BorrowStats:") ||
            k.StartsWith("ProductionBoard:")
        ).ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    #endregion

    #region 缓存管理

    /// <summary>
    /// 确保缓存大小不超过限制
    /// </summary>
    private static void EnsureCacheSize()
    {
        if (_cache.Count < MaxCacheItems) return;

        // 移除最旧的缓存项
        var itemsToRemove = _cache
            .OrderBy(x => x.Value.CachedAt)
            .Take(_cache.Count - MaxCacheItems + 1)
            .Select(x => x.Key)
            .ToList();

        foreach (var key in itemsToRemove)
        {
            _cache.TryRemove(key, out _);
            Interlocked.Increment(ref _totalEvictions);
        }
    }

    /// <summary>
    /// 清理过期缓存
    /// </summary>
    public static void CleanupExpired()
    {
        var expiredKeys = _cache
            .Where(x => x.Value.IsExpired)
            .Select(x => x.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
            Interlocked.Increment(ref _totalEvictions);
        }

        // 清理过期的计数缓存
        var expiredCountKeys = _countCache
            .Where(x => DateTime.Now - x.Value > CountExpiration)
            .Select(x => x.Key)
            .ToList();

        foreach (var key in expiredCountKeys)
        {
            _countCache.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public static CacheStatistics GetStatistics()
    {
        CleanupExpired();

        var items = _cache.Values.ToList();
        return new CacheStatistics
        {
            TotalItems = items.Count,
            TotalHits = Interlocked.Read(ref _totalHits),
            TotalMisses = Interlocked.Read(ref _totalMisses),
            TotalEvictions = Interlocked.Read(ref _totalEvictions),
            HitRate = CalculateHitRate(),
            ExpiredItems = items.Count(x => x.IsExpired),
            CacheKeys = items.Select(x => x.CacheKey).ToList()
        };
    }

    /// <summary>
    /// 计算命中率
    /// </summary>
    private static double CalculateHitRate()
    {
        var hits = Interlocked.Read(ref _totalHits);
        var misses = Interlocked.Read(ref _totalMisses);
        var total = hits + misses;
        return total > 0 ? (double)hits / total * 100 : 0;
    }

    #endregion
}

/// <summary>
/// 缓存统计信息
/// </summary>
public class CacheStatistics
{
    /// <summary>缓存项总数</summary>
    public int TotalItems { get; set; }
    
    /// <summary>缓存命中次数</summary>
    public long TotalHits { get; set; }
    
    /// <summary>缓存未命中次数</summary>
    public long TotalMisses { get; set; }
    
    /// <summary>缓存淘汰次数</summary>
    public long TotalEvictions { get; set; }
    
    /// <summary>命中率(%)</summary>
    public double HitRate { get; set; }
    
    /// <summary>过期项数</summary>
    public int ExpiredItems { get; set; }
    
    /// <summary>缓存键列表</summary>
    public List<string> CacheKeys { get; set; } = new();
    
    /// <summary>总请求数</summary>
    public long TotalRequests => TotalHits + TotalMisses;
}

/// <summary>
/// 缓存键生成器
/// </summary>
public static class CacheKeyBuilder
{
    /// <summary>
    /// 生成分页查询缓存键
    /// </summary>
    public static string BuildPagedQueryKey(string tableName, int pageIndex, int pageSize, string? filter = null)
    {
        return $"Paged:{tableName}:{pageIndex}:{pageSize}:{filter ?? "all"}";
    }

    /// <summary>
    /// 生成报表缓存键
    /// </summary>
    public static string BuildStatsKey(string reportType, DateTime startDate, DateTime endDate, string? additionalKey = null)
    {
        var key = $"Stats:{reportType}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
        if (!string.IsNullOrEmpty(additionalKey))
        {
            key += $":{additionalKey}";
        }
        return key;
    }

    /// <summary>
    /// 生成列表缓存键
    /// </summary>
    public static string BuildListKey(string listType, string? filter = null, string? sort = null)
    {
        return $"List:{listType}:{filter ?? "all"}:{sort ?? "default"}";
    }
}
