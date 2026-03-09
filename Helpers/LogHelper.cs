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
