namespace DieMaking.Helpers;

/// <summary>
/// 日志辅助类
/// </summary>
public static class LogHelper
{
    private static readonly string LogDirectory;
    private static readonly object LockObject = new();

    static LogHelper()
    {
        LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        if (!Directory.Exists(LogDirectory))
        {
            Directory.CreateDirectory(LogDirectory);
        }
    }

    /// <summary>
    /// 写入信息日志
    /// </summary>
    public static void Info(string message)
    {
        WriteLog("INFO", message);
    }

    /// <summary>
    /// 写入警告日志
    /// </summary>
    public static void Warning(string message)
    {
        WriteLog("WARN", message);
    }

    /// <summary>
    /// 写入错误日志
    /// </summary>
    public static void Error(string message)
    {
        WriteLog("ERROR", message);
    }

    /// <summary>
    /// 写入错误日志（带异常）
    /// </summary>
    public static void Error(string message, Exception ex)
    {
        WriteLog("ERROR", $"{message}\n异常：{ex.Message}\n堆栈：{ex.StackTrace}");
    }

    /// <summary>
    /// 写入调试日志
    /// </summary>
    public static void Debug(string message)
    {
        WriteLog("DEBUG", message);
    }

    /// <summary>
    /// 写入日志
    /// </summary>
    private static void WriteLog(string level, string message)
    {
        try
        {
            var logFile = Path.Combine(LogDirectory, $"{DateTime.Now:yyyyMMdd}.log");
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

            lock (LockObject)
            {
                File.AppendAllText(logFile, logEntry + Environment.NewLine);
            }
        }
        catch
        {
            // 日志写入失败时静默处理，避免递归异常
        }
    }

    /// <summary>
    /// 清理过期日志
    /// </summary>
    public static void CleanOldLogs(int retentionDays = 30)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-retentionDays);
            var logFiles = Directory.GetFiles(LogDirectory, "*.log");

            foreach (var file in logFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (DateTime.TryParseExact(fileName, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var fileDate))
                {
                    if (fileDate < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
        }
        catch
        {
            // 清理失败时静默处理
        }
    }

    /// <summary>
    /// 获取最近的日志文件路径
    /// </summary>
    public static string? GetLatestLogFile()
    {
        var logFiles = Directory.GetFiles(LogDirectory, "*.log")
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .FirstOrDefault();
        return logFiles;
    }

    /// <summary>
    /// 读取日志内容
    /// </summary>
    public static string ReadLog(string? logFilePath = null)
    {
        try
        {
            var filePath = logFilePath ?? GetLatestLogFile();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return string.Empty;
            }

            return File.ReadAllText(filePath);
        }
        catch
        {
            return string.Empty;
        }
    }
}
