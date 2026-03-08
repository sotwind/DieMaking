using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace DieMaking.Helpers;

/// <summary>
/// SQL性能监控器 - 提供SQL执行时间记录、慢查询日志和性能分析功能
/// </summary>
public static class SqlPerformanceMonitor
{
    private static readonly ConcurrentDictionary<string, QueryStatistics> _queryStats = new();
    private static readonly ConcurrentQueue<SlowQueryLog> _slowQueryLogs = new();
    private static readonly object _logLock = new();

    /// <summary>
    /// 慢查询阈值（毫秒）
    /// </summary>
    public static int SlowQueryThreshold { get; set; } = 1000;

    /// <summary>
    /// 是否启用性能监控
    /// </summary>
    public static bool IsEnabled { get; set; } =
#if DEBUG
        true;
#else
        false;
#endif

    /// <summary>
    /// 是否启用慢查询日志记录到数据库
    /// </summary>
    public static bool EnableDatabaseLogging { get; set; } = true;

    /// <summary>
    /// 慢查询日志最大保留数量
    /// </summary>
    public static int MaxSlowQueryLogCount { get; set; } = 1000;

    /// <summary>
    /// 性能监控事件
    /// </summary>
    public static event EventHandler<QueryPerformanceEventArgs>? QueryExecuted;

    /// <summary>
    /// 慢查询检测事件
    /// </summary>
    public static event EventHandler<SlowQueryDetectedEventArgs>? SlowQueryDetected;

    #region 性能监控

    /// <summary>
    /// 记录查询执行
    /// </summary>
    public static void RecordQueryExecution(string sql, long executionTimeMs, SqlParameter[]? parameters = null)
    {
        if (!IsEnabled) return;

        var normalizedSql = NormalizeSql(sql);
        var stats = _queryStats.AddOrUpdate(
            normalizedSql,
            _ => new QueryStatistics(normalizedSql, executionTimeMs),
            (_, existing) => existing.AddExecution(executionTimeMs));

        // 检查是否为慢查询
        if (executionTimeMs > SlowQueryThreshold)
        {
            var slowQuery = new SlowQueryLog
            {
                SqlText = sql,
                NormalizedSql = normalizedSql,
                ExecutionTimeMs = executionTimeMs,
                Parameters = parameters != null ? string.Join(", ", parameters.Select(p => $"{p.ParameterName}={p.Value}")) : null,
                Timestamp = DateTime.Now
            };

            // 添加到内存队列
            AddSlowQueryToQueue(slowQuery);

            // 记录到数据库
            if (EnableDatabaseLogging)
            {
                _ = Task.Run(() => LogSlowQueryToDatabaseAsync(slowQuery));
            }

            // 触发事件
            SlowQueryDetected?.Invoke(null, new SlowQueryDetectedEventArgs
            {
                Sql = sql,
                ExecutionTimeMs = executionTimeMs,
                Threshold = SlowQueryThreshold
            });
        }

        // 触发查询执行事件
        QueryExecuted?.Invoke(null, new QueryPerformanceEventArgs
        {
            Sql = sql,
            ExecutionTimeMs = executionTimeMs,
            IsSlowQuery = executionTimeMs > SlowQueryThreshold
        });
    }

    /// <summary>
    /// 使用监控包装SQL执行
    /// </summary>
    public static T ExecuteWithMonitoring<T>(string sql, Func<T> executeFunc, SqlParameter[]? parameters = null)
    {
        if (!IsEnabled)
        {
            return executeFunc();
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            return executeFunc();
        }
        finally
        {
            stopwatch.Stop();
            RecordQueryExecution(sql, stopwatch.ElapsedMilliseconds, parameters);
        }
    }

    /// <summary>
    /// 异步使用监控包装SQL执行
    /// </summary>
    public static async Task<T> ExecuteWithMonitoringAsync<T>(string sql, Func<Task<T>> executeFunc, SqlParameter[]? parameters = null)
    {
        if (!IsEnabled)
        {
            return await executeFunc();
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            return await executeFunc();
        }
        finally
        {
            stopwatch.Stop();
            RecordQueryExecution(sql, stopwatch.ElapsedMilliseconds, parameters);
        }
    }

    #endregion

    #region 统计信息

    /// <summary>
    /// 获取查询统计信息
    /// </summary>
    public static List<QueryStatistics> GetQueryStatistics()
    {
        return _queryStats.Values.OrderByDescending(s => s.AverageExecutionTimeMs).ToList();
    }

    /// <summary>
    /// 获取慢查询统计
    /// </summary>
    public static List<QueryStatistics> GetSlowQueryStatistics()
    {
        return _queryStats.Values
            .Where(s => s.AverageExecutionTimeMs > SlowQueryThreshold || s.MaxExecutionTimeMs > SlowQueryThreshold)
            .OrderByDescending(s => s.MaxExecutionTimeMs)
            .ToList();
    }

    /// <summary>
    /// 获取最频繁的查询
    /// </summary>
    public static List<QueryStatistics> GetMostFrequentQueries(int count = 10)
    {
        return _queryStats.Values
            .OrderByDescending(s => s.ExecutionCount)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// 获取性能报告
    /// </summary>
    public static PerformanceReport GetPerformanceReport()
    {
        var stats = _queryStats.Values.ToList();

        return new PerformanceReport
        {
            TotalQueries = stats.Sum(s => s.ExecutionCount),
            UniqueQueries = stats.Count,
            AverageExecutionTime = stats.Any() ? stats.Average(s => s.AverageExecutionTimeMs) : 0,
            MaxExecutionTime = stats.Any() ? stats.Max(s => s.MaxExecutionTimeMs) : 0,
            MinExecutionTime = stats.Any() ? stats.Min(s => s.MinExecutionTimeMs) : 0,
            SlowQueryCount = stats.Sum(s => s.SlowExecutionCount),
            TopSlowQueries = GetSlowQueryStatistics().Take(10).ToList(),
            TopFrequentQueries = GetMostFrequentQueries(10),
            GeneratedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 重置统计信息
    /// </summary>
    public static void ResetStatistics()
    {
        _queryStats.Clear();
    }

    #endregion

    #region 慢查询日志

    /// <summary>
    /// 获取慢查询日志
    /// </summary>
    public static List<SlowQueryLog> GetSlowQueryLogs(int count = 100)
    {
        return _slowQueryLogs.TakeLast(count).Reverse().ToList();
    }

    /// <summary>
    /// 清空慢查询日志
    /// </summary>
    public static void ClearSlowQueryLogs()
    {
        _slowQueryLogs.Clear();
    }

    /// <summary>
    /// 从数据库获取慢查询日志
    /// </summary>
    public static async Task<List<SlowQueryLog>> GetSlowQueryLogsFromDatabaseAsync(int count = 100)
    {
        try
        {
            var sql = $@"
                SELECT TOP {count} SqlText, ExecutionTimeMs, Parameters, CreateTime
                FROM DM_SlowQueryLog
                ORDER BY CreateTime DESC";

            return await DbHelper.ExecuteQueryAsync(sql, reader => new SlowQueryLog
            {
                SqlText = reader["SqlText"].ToString()!,
                ExecutionTimeMs = Convert.ToInt64(reader["ExecutionTimeMs"]),
                Parameters = reader["Parameters"].ToString(),
                Timestamp = Convert.ToDateTime(reader["CreateTime"])
            });
        }
        catch
        {
            return new List<SlowQueryLog>();
        }
    }

    /// <summary>
    /// 添加慢查询到队列
    /// </summary>
    private static void AddSlowQueryToQueue(SlowQueryLog slowQuery)
    {
        lock (_logLock)
        {
            _slowQueryLogs.Enqueue(slowQuery);

            // 限制队列大小
            while (_slowQueryLogs.Count > MaxSlowQueryLogCount)
            {
                _slowQueryLogs.TryDequeue(out _);
            }
        }
    }

    /// <summary>
    /// 记录慢查询到数据库
    /// </summary>
    private static async Task LogSlowQueryToDatabaseAsync(SlowQueryLog slowQuery)
    {
        try
        {
            // 确保表存在
            await EnsureSlowQueryLogTableAsync();

            var sql = @"
                INSERT INTO DM_SlowQueryLog (SqlText, ExecutionTimeMs, Parameters, CreateTime)
                VALUES (@SqlText, @ExecutionTimeMs, @Parameters, @CreateTime)";

            await DbHelper.ExecuteNonQueryAsync(sql,
                new SqlParameter("@SqlText", slowQuery.SqlText.Length > 4000 ? slowQuery.SqlText[..4000] : slowQuery.SqlText),
                new SqlParameter("@ExecutionTimeMs", slowQuery.ExecutionTimeMs),
                new SqlParameter("@Parameters", (object?)slowQuery.Parameters ?? DBNull.Value),
                new SqlParameter("@CreateTime", slowQuery.Timestamp));
        }
        catch
        {
            // 忽略错误
        }
    }

    /// <summary>
    /// 确保慢查询日志表存在
    /// </summary>
    private static async Task EnsureSlowQueryLogTableAsync()
    {
        try
        {
            var sql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DM_SlowQueryLog')
                BEGIN
                    CREATE TABLE DM_SlowQueryLog (
                        LogID INT IDENTITY(1,1) PRIMARY KEY,
                        SqlText NVARCHAR(MAX),
                        ExecutionTimeMs BIGINT,
                        Parameters NVARCHAR(MAX),
                        CreateTime DATETIME2 DEFAULT GETDATE()
                    );
                    CREATE INDEX IX_DM_SlowQueryLog_CreateTime ON DM_SlowQueryLog(CreateTime);
                END";

            await DbHelper.ExecuteNonQueryAsync(sql);
        }
        catch
        {
            // 忽略错误
        }
    }

    #endregion

    #region 连接池监控

    /// <summary>
    /// 获取数据库连接池状态
    /// </summary>
    public static ConnectionPoolMetrics GetConnectionPoolMetrics()
    {
        try
        {
            using var connection = DbHelper.CreateConnection();
            connection.Open();

            var sql = @"
                SELECT 
                    (SELECT COUNT(*) FROM sys.dm_exec_connections) AS TotalConnections,
                    (SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE status = 'sleeping') AS IdleSessions,
                    (SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE status = 'running') AS ActiveSessions,
                    (SELECT COUNT(*) FROM sys.dm_exec_requests WHERE blocking_session_id > 0) AS BlockedRequests,
                    (SELECT AVG(DATEDIFF(MILLISECOND, last_request_start_time, GETDATE())) 
                     FROM sys.dm_exec_sessions WHERE status = 'running') AS AvgRequestTimeMs";

            using var command = new SqlCommand(sql, connection);
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return new ConnectionPoolMetrics
                {
                    TotalConnections = Convert.ToInt32(reader["TotalConnections"]),
                    IdleSessions = Convert.ToInt32(reader["IdleSessions"]),
                    ActiveSessions = Convert.ToInt32(reader["ActiveSessions"]),
                    BlockedRequests = Convert.ToInt32(reader["BlockedRequests"]),
                    AverageRequestTimeMs = reader["AvgRequestTimeMs"] != DBNull.Value ? Convert.ToInt64(reader["AvgRequestTimeMs"]) : 0,
                    Timestamp = DateTime.Now
                };
            }
        }
        catch
        {
            // 忽略错误
        }

        return new ConnectionPoolMetrics();
    }

    /// <summary>
    /// 获取连接池历史指标
    /// </summary>
    public static List<ConnectionPoolMetrics> GetConnectionPoolHistory(int minutes = 60)
    {
        // 这里可以从历史记录表读取
        // 简化实现：返回当前指标
        return new List<ConnectionPoolMetrics> { GetConnectionPoolMetrics() };
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 规范化SQL（用于统计）
    /// </summary>
    private static string NormalizeSql(string sql)
    {
        // 移除多余空白
        var normalized = System.Text.RegularExpressions.Regex.Replace(sql, @"\s+", " ");
        // 替换参数值为占位符
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"@\w+\s*=\s*[^,\)]+", "@param=?");
        return normalized.Trim().ToLowerInvariant();
    }

    #endregion
}

#region 数据模型

/// <summary>
/// 查询统计信息
/// </summary>
public class QueryStatistics
{
    /// <summary>规范化SQL</summary>
    public string NormalizedSql { get; }

    /// <summary>执行次数</summary>
    public int ExecutionCount { get; private set; }

    /// <summary>总执行时间（毫秒）</summary>
    public long TotalExecutionTimeMs { get; private set; }

    /// <summary>平均执行时间（毫秒）</summary>
    public double AverageExecutionTimeMs => ExecutionCount > 0 ? (double)TotalExecutionTimeMs / ExecutionCount : 0;

    /// <summary>最小执行时间（毫秒）</summary>
    public long MinExecutionTimeMs { get; private set; }

    /// <summary>最大执行时间（毫秒）</summary>
    public long MaxExecutionTimeMs { get; private set; }

    /// <summary>慢查询次数</summary>
    public int SlowExecutionCount { get; private set; }

    /// <summary>最后执行时间</summary>
    public DateTime LastExecutionTime { get; private set; }

    public QueryStatistics(string normalizedSql, long executionTimeMs)
    {
        NormalizedSql = normalizedSql;
        ExecutionCount = 1;
        TotalExecutionTimeMs = executionTimeMs;
        MinExecutionTimeMs = executionTimeMs;
        MaxExecutionTimeMs = executionTimeMs;
        SlowExecutionCount = executionTimeMs > SqlPerformanceMonitor.SlowQueryThreshold ? 1 : 0;
        LastExecutionTime = DateTime.Now;
    }

    public QueryStatistics AddExecution(long executionTimeMs)
    {
        ExecutionCount++;
        TotalExecutionTimeMs += executionTimeMs;
        if (executionTimeMs < MinExecutionTimeMs) MinExecutionTimeMs = executionTimeMs;
        if (executionTimeMs > MaxExecutionTimeMs) MaxExecutionTimeMs = executionTimeMs;
        if (executionTimeMs > SqlPerformanceMonitor.SlowQueryThreshold) SlowExecutionCount++;
        LastExecutionTime = DateTime.Now;
        return this;
    }
}

/// <summary>
/// 慢查询日志
/// </summary>
public class SlowQueryLog
{
    /// <summary>SQL文本</summary>
    public string SqlText { get; set; } = string.Empty;

    /// <summary>规范化SQL</summary>
    public string NormalizedSql { get; set; } = string.Empty;

    /// <summary>执行时间（毫秒）</summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>参数</summary>
    public string? Parameters { get; set; }

    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 性能报告
/// </summary>
public class PerformanceReport
{
    /// <summary>总查询数</summary>
    public int TotalQueries { get; set; }

    /// <summary>唯一查询数</summary>
    public int UniqueQueries { get; set; }

    /// <summary>平均执行时间</summary>
    public double AverageExecutionTime { get; set; }

    /// <summary>最大执行时间</summary>
    public long MaxExecutionTime { get; set; }

    /// <summary>最小执行时间</summary>
    public long MinExecutionTime { get; set; }

    /// <summary>慢查询数</summary>
    public int SlowQueryCount { get; set; }

    /// <summary>最慢的查询</summary>
    public List<QueryStatistics> TopSlowQueries { get; set; } = new();

    /// <summary>最频繁的查询</summary>
    public List<QueryStatistics> TopFrequentQueries { get; set; } = new();

    /// <summary>生成时间</summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>慢查询率</summary>
    public double SlowQueryRate => TotalQueries > 0 ? (double)SlowQueryCount / TotalQueries * 100 : 0;
}

/// <summary>
/// 连接池指标
/// </summary>
public class ConnectionPoolMetrics
{
    /// <summary>总连接数</summary>
    public int TotalConnections { get; set; }

    /// <summary>空闲会话数</summary>
    public int IdleSessions { get; set; }

    /// <summary>活动会话数</summary>
    public int ActiveSessions { get; set; }

    /// <summary>被阻塞的请求数</summary>
    public int BlockedRequests { get; set; }

    /// <summary>平均请求时间（毫秒）</summary>
    public long AverageRequestTimeMs { get; set; }

    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>连接利用率</summary>
    public double ConnectionUtilization => TotalConnections > 0 ? (double)ActiveSessions / TotalConnections * 100 : 0;
}

/// <summary>
/// 查询性能事件参数
/// </summary>
public class QueryPerformanceEventArgs : EventArgs
{
    /// <summary>SQL语句</summary>
    public string Sql { get; set; } = string.Empty;

    /// <summary>执行时间（毫秒）</summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>是否为慢查询</summary>
    public bool IsSlowQuery { get; set; }
}

/// <summary>
/// 慢查询检测事件参数
/// </summary>
public class SlowQueryDetectedEventArgs : EventArgs
{
    /// <summary>SQL语句</summary>
    public string Sql { get; set; } = string.Empty;

    /// <summary>执行时间（毫秒）</summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>阈值（毫秒）</summary>
    public int Threshold { get; set; }
}

#endregion
