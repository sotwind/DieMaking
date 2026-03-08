using Microsoft.Data.SqlClient;

namespace DieMaking.Helpers;

/// <summary>
/// 数据库健康状态
/// </summary>
public enum DatabaseHealthStatus
{
    /// <summary>健康</summary>
    Healthy,
    /// <summary>连接缓慢</summary>
    Slow,
    /// <summary>连接断开</summary>
    Disconnected,
    /// <summary>检查失败</summary>
    Error
}

/// <summary>
/// 数据库健康检查结果
/// </summary>
public class DatabaseHealthResult
{
    /// <summary>健康状态</summary>
    public DatabaseHealthStatus Status { get; set; }
    /// <summary>连接耗时（毫秒）</summary>
    public long ConnectionTimeMs { get; set; }
    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }
    /// <summary>最后检查时间</summary>
    public DateTime CheckTime { get; set; }
    /// <summary>数据库版本</summary>
    public string? DatabaseVersion { get; set; }
    /// <summary>数据库名称</summary>
    public string? DatabaseName { get; set; }

    public bool IsHealthy => Status == DatabaseHealthStatus.Healthy;
}

/// <summary>
/// 数据库健康检查帮助类
/// </summary>
public static class DatabaseHealthChecker
{
    /// <summary>
    /// 慢连接阈值（毫秒）
    /// </summary>
    private const int SlowConnectionThreshold = 2000;

    /// <summary>
    /// 检查数据库连接健康状态
    /// </summary>
    public static async Task<DatabaseHealthResult> CheckHealthAsync(int timeoutSeconds = 5)
    {
        var result = new DatabaseHealthResult
        {
            CheckTime = DateTime.Now
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            
            await Task.Run(() =>
            {
                using var connection = DbHelper.CreateConnection();
                connection.Open();
                
                // 获取数据库信息
                using var command = new SqlCommand(@"
                    SELECT @@VERSION AS Version, DB_NAME() AS DatabaseName", connection);
                
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    result.DatabaseVersion = reader["Version"]?.ToString()?.Split('\n').FirstOrDefault();
                    result.DatabaseName = reader["DatabaseName"]?.ToString();
                }
                
                // 测试简单查询
                using var testCommand = new SqlCommand("SELECT 1", connection);
                testCommand.ExecuteScalar();
                
            }, cts.Token);

            stopwatch.Stop();
            result.ConnectionTimeMs = stopwatch.ElapsedMilliseconds;

            // 判断连接速度
            result.Status = result.ConnectionTimeMs > SlowConnectionThreshold 
                ? DatabaseHealthStatus.Slow 
                : DatabaseHealthStatus.Healthy;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            result.ConnectionTimeMs = stopwatch.ElapsedMilliseconds;
            result.Status = DatabaseHealthStatus.Disconnected;
            result.ErrorMessage = "连接超时";
        }
        catch (SqlException ex)
        {
            stopwatch.Stop();
            result.ConnectionTimeMs = stopwatch.ElapsedMilliseconds;
            result.Status = DatabaseHealthStatus.Disconnected;
            result.ErrorMessage = GetSqlErrorMessage(ex);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ConnectionTimeMs = stopwatch.ElapsedMilliseconds;
            result.Status = DatabaseHealthStatus.Error;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// 同步检查数据库健康状态
    /// </summary>
    public static DatabaseHealthResult CheckHealth(int timeoutSeconds = 5)
    {
        return CheckHealthAsync(timeoutSeconds).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 测试数据库连接（简单版）
    /// </summary>
    public static bool TestConnection()
    {
        try
        {
            using var connection = DbHelper.CreateConnection();
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
    /// 获取数据库连接字符串信息（隐藏敏感信息）
    /// </summary>
    public static string GetSafeConnectionString()
    {
        try
        {
            var connectionString = DbHelper.ConnectionString;
            var builder = new SqlConnectionStringBuilder(connectionString);
            
            // 隐藏密码
            if (!string.IsNullOrEmpty(builder.Password))
            {
                builder.Password = "********";
            }
            
            // 隐藏用户ID的部分内容
            if (!string.IsNullOrEmpty(builder.UserID))
            {
                builder.UserID = MaskString(builder.UserID);
            }
            
            return builder.ConnectionString;
        }
        catch
        {
            return "无法获取连接字符串";
        }
    }

    /// <summary>
    /// 获取数据库服务器信息
    /// </summary>
    public static Dictionary<string, string> GetServerInfo()
    {
        var info = new Dictionary<string, string>();

        try
        {
            using var connection = DbHelper.CreateConnection();
            connection.Open();

            // 获取服务器信息
            using var command = new SqlCommand(@"
                SELECT 
                    @@SERVERNAME AS ServerName,
                    @@VERSION AS Version,
                    DB_NAME() AS DatabaseName,
                    (SELECT COUNT(*) FROM sys.databases WHERE state = 0) AS DatabaseCount,
                    (SELECT COUNT(*) FROM sys.dm_exec_connections) AS ConnectionCount", connection);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                info["服务器名称"] = reader["ServerName"]?.ToString() ?? "未知";
                info["数据库版本"] = reader["Version"]?.ToString()?.Split('\n').FirstOrDefault() ?? "未知";
                info["当前数据库"] = reader["DatabaseName"]?.ToString() ?? "未知";
                info["数据库数量"] = reader["DatabaseCount"]?.ToString() ?? "未知";
                info["当前连接数"] = reader["ConnectionCount"]?.ToString() ?? "未知";
            }
        }
        catch (Exception ex)
        {
            info["错误"] = ex.Message;
        }

        return info;
    }

    /// <summary>
    /// 获取SQL错误信息
    /// </summary>
    private static string GetSqlErrorMessage(SqlException ex)
    {
        return ex.Number switch
        {
            -1 or 2 or 53 or 258 => "无法连接到数据库服务器",
            4060 => "无法访问数据库",
            18456 or 18452 => "数据库登录失败",
            1205 => "数据库死锁",
            _ => $"数据库错误 ({ex.Number}): {ex.Message}"
        };
    }

    /// <summary>
    /// 隐藏字符串中间部分
    /// </summary>
    private static string MaskString(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= 4)
        {
            return "****";
        }

        return input[..2] + new string('*', input.Length - 4) + input[^2..];
    }
}
