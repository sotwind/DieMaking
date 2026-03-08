using System.Configuration;
using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace DieMaking.Helpers;

/// <summary>
/// 数据库连接池配置
/// </summary>
public static class DbConnectionPoolConfig
{
    /// <summary>连接池最小连接数</summary>
    public const int MinPoolSize = 5;

    /// <summary>连接池最大连接数</summary>
    public const int MaxPoolSize = 100;

    /// <summary>连接超时时间（秒）</summary>
    public const int ConnectTimeout = 30;

    /// <summary>命令超时时间（秒）</summary>
    public const int CommandTimeout = 60;

    /// <summary>连接池连接生命周期（分钟）</summary>
    public const int LoadBalanceTimeout = 0;

    /// <summary>是否启用连接池</summary>
    public const bool Pooling = true;

    /// <summary>连接池连接超时时间（秒）</summary>
    public const int ConnectionLifetime = 0;
}

/// <summary>
/// SQL性能监控事件参数
/// </summary>
public class SqlPerformanceEventArgs : EventArgs
{
    /// <summary>SQL语句</summary>
    public string Sql { get; set; } = string.Empty;

    /// <summary>执行时间（毫秒）</summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>参数列表</summary>
    public SqlParameter[]? Parameters { get; set; }

    /// <summary>是否为慢查询</summary>
    public bool IsSlowQuery => ExecutionTimeMs > SlowQueryThreshold;

    /// <summary>慢查询阈值（毫秒）</summary>
    public const int SlowQueryThreshold = 1000;
}

/// <summary>
/// 数据库帮助类 - 提供优化的数据库访问功能
/// </summary>
public static class DbHelper
{
    private static string? _connectionString;
    private static readonly object _lockObj = new();

    /// <summary>
    /// SQL执行性能监控事件（调试用）
    /// </summary>
    public static event EventHandler<SqlPerformanceEventArgs>? SqlExecuted;

    /// <summary>
    /// 慢查询日志事件
    /// </summary>
    public static event EventHandler<SqlPerformanceEventArgs>? SlowQueryDetected;

    /// <summary>
    /// 是否启用SQL执行时间记录（调试模式）
    /// </summary>
    public static bool EnablePerformanceMonitoring { get; set; } =
#if DEBUG
        true;
#else
        false;
#endif

    /// <summary>
    /// 数据库连接字符串（自动添加连接池配置）
    /// </summary>
    public static string ConnectionString
    {
        get
        {
            if (_connectionString == null)
            {
                lock (_lockObj)
                {
                    if (_connectionString == null)
                    {
                        var baseConnectionString = ConfigurationManager.ConnectionStrings["DieMakingDB"]?.ConnectionString
                            ?? throw new InvalidOperationException("数据库连接字符串未配置");
                        _connectionString = BuildOptimizedConnectionString(baseConnectionString);
                    }
                }
            }
            return _connectionString;
        }
    }

    /// <summary>
    /// 构建优化的连接字符串
    /// </summary>
    private static string BuildOptimizedConnectionString(string baseConnectionString)
    {
        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            // 连接池配置
            MinPoolSize = DbConnectionPoolConfig.MinPoolSize,
            MaxPoolSize = DbConnectionPoolConfig.MaxPoolSize,
            ConnectTimeout = DbConnectionPoolConfig.ConnectTimeout,
            Pooling = DbConnectionPoolConfig.Pooling,
            LoadBalanceTimeout = DbConnectionPoolConfig.LoadBalanceTimeout,
            ConnectionLifetime = DbConnectionPoolConfig.ConnectionLifetime,

            // 启用异步操作
            AsynchronousProcessing = true,

            // 连接重试配置
            ConnectRetryCount = 3,
            ConnectRetryInterval = 10
        };

        return builder.ConnectionString;
    }

    /// <summary>
    /// 创建数据库连接
    /// </summary>
    public static SqlConnection CreateConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        return connection;
    }

    /// <summary>
    /// 异步创建并打开数据库连接
    /// </summary>
    public static async Task<SqlConnection> CreateAndOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = CreateConnection();
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// 执行标量查询
    /// </summary>
    public static object? ExecuteScalar(string sql, params SqlParameter[] parameters)
    {
        return ExecuteScalar(sql, DbConnectionPoolConfig.CommandTimeout, parameters);
    }

    /// <summary>
    /// 执行标量查询（带超时设置）
    /// </summary>
    public static object? ExecuteScalar(string sql, int commandTimeout, params SqlParameter[] parameters)
    {
        var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;

        try
        {
            using var connection = CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = commandTimeout;
            if (parameters.Length > 0) command.Parameters.AddRange(parameters);
            connection.Open();
            return command.ExecuteScalar();
        }
        finally
        {
            if (stopwatch != null)
            {
                stopwatch.Stop();
                OnSqlExecuted(sql, stopwatch.ElapsedMilliseconds, parameters);
            }
        }
    }

    /// <summary>
    /// 异步执行标量查询
    /// </summary>
    public static async Task<object?> ExecuteScalarAsync(string sql, params SqlParameter[] parameters)
    {
        return await ExecuteScalarAsync(sql, DbConnectionPoolConfig.CommandTimeout, parameters);
    }

    /// <summary>
    /// 异步执行标量查询（带超时设置）
    /// </summary>
    public static async Task<object?> ExecuteScalarAsync(string sql, int commandTimeout, params SqlParameter[] parameters)
    {
        var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;

        try
        {
            await using var connection = await CreateAndOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = commandTimeout;
            if (parameters.Length > 0) command.Parameters.AddRange(parameters);
            return await command.ExecuteScalarAsync();
        }
        finally
        {
            if (stopwatch != null)
            {
                stopwatch.Stop();
                OnSqlExecuted(sql, stopwatch.ElapsedMilliseconds, parameters);
            }
        }
    }

    /// <summary>
    /// 执行非查询操作
    /// </summary>
    public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
    {
        return ExecuteNonQuery(sql, DbConnectionPoolConfig.CommandTimeout, parameters);
    }

    /// <summary>
    /// 执行非查询操作（带超时设置）
    /// </summary>
    public static int ExecuteNonQuery(string sql, int commandTimeout, params SqlParameter[] parameters)
    {
        var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;

        try
        {
            using var connection = CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = commandTimeout;
            if (parameters.Length > 0) command.Parameters.AddRange(parameters);
            connection.Open();
            return command.ExecuteNonQuery();
        }
        finally
        {
            if (stopwatch != null)
            {
                stopwatch.Stop();
                OnSqlExecuted(sql, stopwatch.ElapsedMilliseconds, parameters);
            }
        }
    }

    /// <summary>
    /// 异步执行非查询操作
    /// </summary>
    public static async Task<int> ExecuteNonQueryAsync(string sql, params SqlParameter[] parameters)
    {
        return await ExecuteNonQueryAsync(sql, DbConnectionPoolConfig.CommandTimeout, parameters);
    }

    /// <summary>
    /// 异步执行非查询操作（带超时设置）
    /// </summary>
    public static async Task<int> ExecuteNonQueryAsync(string sql, int commandTimeout, params SqlParameter[] parameters)
    {
        var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;

        try
        {
            await using var connection = await CreateAndOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = commandTimeout;
            if (parameters.Length > 0) command.Parameters.AddRange(parameters);
            return await command.ExecuteNonQueryAsync();
        }
        finally
        {
            if (stopwatch != null)
            {
                stopwatch.Stop();
                OnSqlExecuted(sql, stopwatch.ElapsedMilliseconds, parameters);
            }
        }
    }

    /// <summary>
    /// 执行查询并映射结果
    /// </summary>
    public static List<T> ExecuteQuery<T>(string sql, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
    {
        return ExecuteQuery(sql, DbConnectionPoolConfig.CommandTimeout, mapper, parameters);
    }

    /// <summary>
    /// 执行查询并映射结果（带超时设置）
    /// </summary>
    public static List<T> ExecuteQuery<T>(string sql, int commandTimeout, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
    {
        var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;

        try
        {
            var list = new List<T>();
            using var connection = CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = commandTimeout;
            if (parameters.Length > 0) command.Parameters.AddRange(parameters);
            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read()) list.Add(mapper(reader));
            return list;
        }
        finally
        {
            if (stopwatch != null)
            {
                stopwatch.Stop();
                OnSqlExecuted(sql, stopwatch.ElapsedMilliseconds, parameters);
            }
        }
    }

    /// <summary>
    /// 异步执行查询并映射结果
    /// </summary>
    public static async Task<List<T>> ExecuteQueryAsync<T>(string sql, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
    {
        return await ExecuteQueryAsync(sql, DbConnectionPoolConfig.CommandTimeout, mapper, parameters);
    }

    /// <summary>
    /// 异步执行查询并映射结果（带超时设置）
    /// </summary>
    public static async Task<List<T>> ExecuteQueryAsync<T>(string sql, int commandTimeout, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
    {
        var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;

        try
        {
            var list = new List<T>();
            await using var connection = await CreateAndOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = commandTimeout;
            if (parameters.Length > 0) command.Parameters.AddRange(parameters);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) list.Add(mapper(reader));
            return list;
        }
        finally
        {
            if (stopwatch != null)
            {
                stopwatch.Stop();
                OnSqlExecuted(sql, stopwatch.ElapsedMilliseconds, parameters);
            }
        }
    }

    /// <summary>
    /// 执行数据读取器（需要外部管理连接生命周期）
    /// </summary>
    public static SqlDataReader ExecuteReader(string sql, SqlConnection connection, params SqlParameter[] parameters)
    {
        return ExecuteReader(sql, connection, DbConnectionPoolConfig.CommandTimeout, parameters);
    }

    /// <summary>
    /// 执行数据读取器（带超时设置）
    /// </summary>
    public static SqlDataReader ExecuteReader(string sql, SqlConnection connection, int commandTimeout, params SqlParameter[] parameters)
    {
        var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;

        try
        {
            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = commandTimeout;
            if (parameters.Length > 0) command.Parameters.AddRange(parameters);
            return command.ExecuteReader();
        }
        finally
        {
            if (stopwatch != null)
            {
                stopwatch.Stop();
                OnSqlExecuted(sql, stopwatch.ElapsedMilliseconds, parameters);
            }
        }
    }

    /// <summary>
    /// 异步执行数据读取器
    /// </summary>
    public static async Task<SqlDataReader> ExecuteReaderAsync(string sql, SqlConnection connection, params SqlParameter[] parameters)
    {
        return await ExecuteReaderAsync(sql, connection, DbConnectionPoolConfig.CommandTimeout, parameters);
    }

    /// <summary>
    /// 异步执行数据读取器（带超时设置）
    /// </summary>
    public static async Task<SqlDataReader> ExecuteReaderAsync(string sql, SqlConnection connection, int commandTimeout, params SqlParameter[] parameters)
    {
        var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;

        try
        {
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = commandTimeout;
            if (parameters.Length > 0) command.Parameters.AddRange(parameters);
            return await command.ExecuteReaderAsync();
        }
        finally
        {
            if (stopwatch != null)
            {
                stopwatch.Stop();
                OnSqlExecuted(sql, stopwatch.ElapsedMilliseconds, parameters);
            }
        }
    }

    /// <summary>
    /// 执行分页查询（使用 OFFSET FETCH）
    /// </summary>
    public static PagedResult<T> ExecutePagedQuery<T>(string baseSql, string orderBy, int pageIndex, int pageSize,
        Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
    {
        return ExecutePagedQuery(baseSql, orderBy, pageIndex, pageSize, DbConnectionPoolConfig.CommandTimeout, mapper, parameters);
    }

    /// <summary>
    /// 执行分页查询（带超时设置）
    /// </summary>
    public static PagedResult<T> ExecutePagedQuery<T>(string baseSql, string orderBy, int pageIndex, int pageSize,
        int commandTimeout, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1) pageSize = 10;

        var offset = (pageIndex - 1) * pageSize;

        // 构建分页SQL
        var pagedSql = $@"
            WITH PagedData AS (
                {baseSql}
            )
            SELECT * FROM PagedData
            ORDER BY {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*) FROM (
                {baseSql}
            ) AS CountQuery;";

        var pagedParameters = parameters.ToList();
        pagedParameters.Add(new SqlParameter("@Offset", offset));
        pagedParameters.Add(new SqlParameter("@PageSize", pageSize));

        var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;

        try
        {
            using var connection = CreateConnection();
            using var command = new SqlCommand(pagedSql, connection);
            command.CommandTimeout = commandTimeout;
            command.Parameters.AddRange(pagedParameters.ToArray());
            connection.Open();

            // 读取分页数据
            var items = new List<T>();
            using var reader = command.ExecuteReader();
            while (reader.Read()) items.Add(mapper(reader));

            // 读取总数
            int totalCount = 0;
            if (reader.NextResult() && reader.Read())
            {
                totalCount = Convert.ToInt32(reader[0]);
            }

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }
        finally
        {
            if (stopwatch != null)
            {
                stopwatch.Stop();
                OnSqlExecuted(pagedSql, stopwatch.ElapsedMilliseconds, pagedParameters.ToArray());
            }
        }
    }

    /// <summary>
    /// 异步执行分页查询
    /// </summary>
    public static async Task<PagedResult<T>> ExecutePagedQueryAsync<T>(string baseSql, string orderBy, int pageIndex, int pageSize,
        Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
    {
        return await ExecutePagedQueryAsync(baseSql, orderBy, pageIndex, pageSize, DbConnectionPoolConfig.CommandTimeout, mapper, parameters);
    }

    /// <summary>
    /// 异步执行分页查询（带超时设置）
    /// </summary>
    public static async Task<PagedResult<T>> ExecutePagedQueryAsync<T>(string baseSql, string orderBy, int pageIndex, int pageSize,
        int commandTimeout, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1) pageSize = 10;

        var offset = (pageIndex - 1) * pageSize;

        // 构建分页SQL
        var pagedSql = $@"
            WITH PagedData AS (
                {baseSql}
            )
            SELECT * FROM PagedData
            ORDER BY {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*) FROM (
                {baseSql}
            ) AS CountQuery;";

        var pagedParameters = parameters.ToList();
        pagedParameters.Add(new SqlParameter("@Offset", offset));
        pagedParameters.Add(new SqlParameter("@PageSize", pageSize));

        var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;

        try
        {
            await using var connection = await CreateAndOpenConnectionAsync();
            await using var command = new SqlCommand(pagedSql, connection);
            command.CommandTimeout = commandTimeout;
            command.Parameters.AddRange(pagedParameters.ToArray());

            // 读取分页数据
            var items = new List<T>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) items.Add(mapper(reader));

            // 读取总数
            int totalCount = 0;
            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                totalCount = Convert.ToInt32(reader[0]);
            }

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }
        finally
        {
            if (stopwatch != null)
            {
                stopwatch.Stop();
                OnSqlExecuted(pagedSql, stopwatch.ElapsedMilliseconds, pagedParameters.ToArray());
            }
        }
    }

    /// <summary>
    /// 执行事务操作
    /// </summary>
    public static bool ExecuteTransaction(Func<SqlConnection, SqlTransaction, bool> action)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var result = action(connection, transaction);
            if (result)
            {
                transaction.Commit();
            }
            else
            {
                transaction.Rollback();
            }
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 异步执行事务操作
    /// </summary>
    public static async Task<bool> ExecuteTransactionAsync(Func<SqlConnection, SqlTransaction, Task<bool>> action)
    {
        await using var connection = await CreateAndOpenConnectionAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            var result = await action(connection, transaction);
            if (result)
            {
                await transaction.CommitAsync();
            }
            else
            {
                await transaction.RollbackAsync();
            }
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 检查数据库连接是否健康
    /// </summary>
    public static bool TestConnection()
    {
        try
        {
            using var connection = CreateConnection();
            connection.Open();
            using var command = new SqlCommand("SELECT 1", connection);
            return command.ExecuteScalar() != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 异步检查数据库连接是否健康
    /// </summary>
    public static async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await CreateAndOpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand("SELECT 1", connection);
            return await command.ExecuteScalarAsync(cancellationToken) != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取当前连接池状态信息
    /// </summary>
    public static ConnectionPoolStats GetConnectionPoolStats()
    {
        try
        {
            // 使用性能计数器或SQL查询获取连接池信息
            using var connection = CreateConnection();
            connection.Open();

            using var command = new SqlCommand(@"
                SELECT 
                    (SELECT COUNT(*) FROM sys.dm_exec_connections) AS TotalConnections,
                    (SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE status = 'sleeping' AND last_request_start_time < DATEADD(SECOND, -30, GETDATE())) AS IdleConnections,
                    (SELECT COUNT(*) FROM sys.dm_exec_requests WHERE blocking_session_id > 0) AS BlockedConnections", connection);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new ConnectionPoolStats
                {
                    TotalConnections = Convert.ToInt32(reader["TotalConnections"]),
                    IdleConnections = Convert.ToInt32(reader["IdleConnections"]),
                    BlockedConnections = Convert.ToInt32(reader["BlockedConnections"]),
                    CheckTime = DateTime.Now
                };
            }
        }
        catch
        {
            // 忽略错误
        }

        return new ConnectionPoolStats();
    }

    /// <summary>
    /// 触发SQL执行事件
    /// </summary>
    private static void OnSqlExecuted(string sql, long executionTimeMs, SqlParameter[]? parameters)
    {
        if (!EnablePerformanceMonitoring) return;

        var args = new SqlPerformanceEventArgs
        {
            Sql = sql,
            ExecutionTimeMs = executionTimeMs,
            Parameters = parameters
        };

        SqlExecuted?.Invoke(null, args);

        if (args.IsSlowQuery)
        {
            SlowQueryDetected?.Invoke(null, args);
        }
    }

    /// <summary>
    /// 重置连接字符串（用于配置变更后）
    /// </summary>
    public static void ResetConnectionString()
    {
        lock (_lockObj)
        {
            _connectionString = null;
        }
    }
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
}

/// <summary>
/// 连接池统计信息
/// </summary>
public class ConnectionPoolStats
{
    /// <summary>总连接数</summary>
    public int TotalConnections { get; set; }

    /// <summary>空闲连接数</summary>
    public int IdleConnections { get; set; }

    /// <summary>被阻塞的连接数</summary>
    public int BlockedConnections { get; set; }

    /// <summary>检查时间</summary>
    public DateTime CheckTime { get; set; }
}
