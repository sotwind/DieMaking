using DieMaking.Helpers;
using DieMaking.Models;
using Microsoft.Data.SqlClient;
using System.Net;

namespace DieMaking.Services;

/// <summary>
/// 通用日志服务 - 提供操作日志记录功能
/// </summary>
public static class LogService
{
    /// <summary>
    /// 检查当前日志级别是否满足记录条件
    /// </summary>
    private static bool ShouldLog(LogLevel messageLevel)
    {
        // 如果操作日志被禁用，不记录
        if (!ConfigHelper.EnableOperationLog)
            return false;

        // 获取当前配置的日志级别
        var configLevel = ConfigHelper.LogLevel;

        // 消息级别 >= 配置级别时才记录
        // Debug(0) < Info(1) < Warning(2) < Error(3)
        return messageLevel >= configLevel;
    }

    /// <summary>
    /// 将操作类型映射到日志级别
    /// </summary>
    private static LogLevel GetLogLevelForOperation(string operationType)
    {
        return operationType?.ToLower() switch
        {
            "错误" or "error" or "异常" or "登录失败" => LogLevel.Error,
            "警告" or "warning" => LogLevel.Warning,
            "调试" or "debug" => LogLevel.Debug,
            _ => LogLevel.Info
        };
    }

    /// <summary>
    /// 记录操作日志（自动根据操作类型判断级别）
    /// </summary>
    /// <param name="operationType">操作类型（如：新增、修改、删除、审核等）</param>
    /// <param name="content">操作内容描述</param>
    /// <param name="dieNo">关联的刀模编号（可选）</param>
    public static void LogOperation(string operationType, string content, string? dieNo = null)
    {
        try
        {
            // 根据操作类型获取日志级别
            var logLevel = GetLogLevelForOperation(operationType);

            // 检查是否满足记录条件
            if (!ShouldLog(logLevel))
                return;

            // 异步记录日志，避免阻塞主流程
            Task.Run(() => DoLogOperation(operationType, content, dieNo, logLevel));
        }
        catch
        {
            // 日志记录失败不影响主业务流程
        }
    }

    /// <summary>
    /// 记录指定级别的日志
    /// </summary>
    /// <param name="logLevel">日志级别</param>
    /// <param name="operationType">操作类型</param>
    /// <param name="content">操作内容描述</param>
    /// <param name="dieNo">关联的刀模编号（可选）</param>
    public static void LogWithLevel(LogLevel logLevel, string operationType, string content, string? dieNo = null)
    {
        try
        {
            // 检查是否满足记录条件
            if (!ShouldLog(logLevel))
                return;

            // 异步记录日志
            Task.Run(() => DoLogOperation(operationType, content, dieNo, logLevel));
        }
        catch
        {
            // 日志记录失败不影响主业务流程
        }
    }

    /// <summary>
    /// 记录调试日志
    /// </summary>
    public static void LogDebug(string operationType, string content, string? dieNo = null)
    {
        LogWithLevel(LogLevel.Debug, operationType, content, dieNo);
    }

    /// <summary>
    /// 记录信息日志
    /// </summary>
    public static void LogInfo(string operationType, string content, string? dieNo = null)
    {
        LogWithLevel(LogLevel.Info, operationType, content, dieNo);
    }

    /// <summary>
    /// 记录警告日志
    /// </summary>
    public static void LogWarning(string operationType, string content, string? dieNo = null)
    {
        LogWithLevel(LogLevel.Warning, operationType, content, dieNo);
    }

    /// <summary>
    /// 记录错误日志
    /// </summary>
    public static void LogError(string operationType, string content, string? dieNo = null)
    {
        LogWithLevel(LogLevel.Error, operationType, content, dieNo);
    }

    /// <summary>
    /// 同步记录操作日志（在需要立即记录的场景使用）
    /// </summary>
    /// <param name="operationType">操作类型</param>
    /// <param name="content">操作内容描述</param>
    /// <param name="dieNo">关联的刀模编号（可选）</param>
    public static void LogOperationSync(string operationType, string content, string? dieNo = null)
    {
        try
        {
            var logLevel = GetLogLevelForOperation(operationType);

            if (!ShouldLog(logLevel))
                return;

            DoLogOperation(operationType, content, dieNo, logLevel);
        }
        catch
        {
            // 日志记录失败不影响主业务流程
        }
    }

    /// <summary>
    /// 执行实际的日志记录操作
    /// </summary>
    private static void DoLogOperation(string operationType, string content, string? dieNo, LogLevel logLevel)
    {
        try
        {
            // 获取当前用户信息
            var userId = CurrentUser.User?.UserID;
            var username = CurrentUser.User?.Username ?? "";

            // 获取IP地址
            var ipAddress = GetClientIPAddress();

            // 获取刀模ID（如果提供了刀模编号）
            int? dieId = null;
            if (!string.IsNullOrEmpty(dieNo))
            {
                dieId = GetDieIdByCode(dieNo);
            }

            // 插入日志记录
            var sql = @"INSERT INTO DM_OperationLog (UserID, Username, OperationType, OperationDesc, DieID, IPAddress, LogLevel, CreateTime) 
                        VALUES (@UserID, @Username, @OperationType, @OperationDesc, @DieID, @IPAddress, @LogLevel, GETDATE())";

            DbHelper.ExecuteNonQuery(sql,
                new SqlParameter("@UserID", userId ?? (object)DBNull.Value),
                new SqlParameter("@Username", username),
                new SqlParameter("@OperationType", operationType),
                new SqlParameter("@OperationDesc", content),
                new SqlParameter("@DieID", dieId ?? (object)DBNull.Value),
                new SqlParameter("@IPAddress", ipAddress),
                new SqlParameter("@LogLevel", logLevel.ToString()));
        }
        catch
        {
            // 日志记录失败不抛出异常
        }
    }

    /// <summary>
    /// 根据刀模编号获取刀模ID
    /// </summary>
    private static int? GetDieIdByCode(string dieNo)
    {
        try
        {
            var sql = "SELECT DieID FROM DM_DieInfo WHERE DieCode = @DieCode";
            var result = DbHelper.ExecuteScalar(sql, new SqlParameter("@DieCode", dieNo));
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    private static string GetClientIPAddress()
    {
        try
        {
            // 获取本机IP地址
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    #region 日志清理功能

    /// <summary>
    /// 清理过期日志（根据配置的保留天数）
    /// </summary>
    /// <returns>清理的日志条数</returns>
    public static int CleanupExpiredLogs()
    {
        try
        {
            var retentionDays = ConfigHelper.LogRetentionDays;
            return CleanupLogs(retentionDays);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 清理指定天数之前的日志
    /// </summary>
    /// <param name="retentionDays">保留天数</param>
    /// <returns>清理的日志条数</returns>
    public static int CleanupLogs(int retentionDays)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-retentionDays);

            var sql = @"DELETE FROM DM_OperationLog WHERE CreateTime < @CutoffDate;
                        SELECT @@ROWCOUNT;";

            var result = DbHelper.ExecuteScalar(sql, new SqlParameter("@CutoffDate", cutoffDate));
            return result != null ? Convert.ToInt32(result) : 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 异步清理过期日志
    /// </summary>
    public static void CleanupExpiredLogsAsync()
    {
        Task.Run(() =>
        {
            try
            {
                var count = CleanupExpiredLogs();
                if (count > 0)
                {
                    // 记录清理操作
                    LogInfo("日志清理", $"自动清理了 {count} 条过期日志记录");
                }
            }
            catch
            {
                // 清理失败不抛出异常
            }
        });
    }

    /// <summary>
    /// 获取日志统计信息
    /// </summary>
    public static LogStatistics GetLogStatistics()
    {
        try
        {
            var sql = @"SELECT 
                            COUNT(*) as TotalCount,
                            SUM(CASE WHEN CreateTime >= DATEADD(day, -1, GETDATE()) THEN 1 ELSE 0 END) as TodayCount,
                            SUM(CASE WHEN CreateTime >= DATEADD(day, -7, GETDATE()) THEN 1 ELSE 0 END) as WeekCount,
                            SUM(CASE WHEN LogLevel = 'Error' THEN 1 ELSE 0 END) as ErrorCount,
                            SUM(CASE WHEN LogLevel = 'Warning' THEN 1 ELSE 0 END) as WarningCount,
                            MIN(CreateTime) as OldestLogDate
                        FROM DM_OperationLog";

            var result = DbHelper.ExecuteQuery(sql, reader => new LogStatistics
            {
                TotalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0,
                TodayCount = reader["TodayCount"] != DBNull.Value ? Convert.ToInt32(reader["TodayCount"]) : 0,
                WeekCount = reader["WeekCount"] != DBNull.Value ? Convert.ToInt32(reader["WeekCount"]) : 0,
                ErrorCount = reader["ErrorCount"] != DBNull.Value ? Convert.ToInt32(reader["ErrorCount"]) : 0,
                WarningCount = reader["WarningCount"] != DBNull.Value ? Convert.ToInt32(reader["WarningCount"]) : 0,
                OldestLogDate = reader["OldestLogDate"] != DBNull.Value ? Convert.ToDateTime(reader["OldestLogDate"]) : (DateTime?)null
            });

            return result.FirstOrDefault() ?? new LogStatistics();
        }
        catch
        {
            return new LogStatistics();
        }
    }

    #endregion
}

/// <summary>
/// 日志统计信息
/// </summary>
public class LogStatistics
{
    /// <summary>总日志数</summary>
    public int TotalCount { get; set; }

    /// <summary>今日日志数</summary>
    public int TodayCount { get; set; }

    /// <summary>本周日志数</summary>
    public int WeekCount { get; set; }

    /// <summary>错误日志数</summary>
    public int ErrorCount { get; set; }

    /// <summary>警告日志数</summary>
    public int WarningCount { get; set; }

    /// <summary>最早日志日期</summary>
    public DateTime? OldestLogDate { get; set; }
}
