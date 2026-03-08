using System.Data.Common;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Data.SqlClient;
using DieMaking.Forms;

namespace DieMaking.Helpers;

/// <summary>
/// 异常类型枚举
/// </summary>
public enum ExceptionType
{
    /// <summary>数据库异常</summary>
    Database,
    /// <summary>网络异常</summary>
    Network,
    /// <summary>业务逻辑异常</summary>
    Business,
    /// <summary>验证异常</summary>
    Validation,
    /// <summary>权限异常</summary>
    Authorization,
    /// <summary>系统异常</summary>
    System,
    /// <summary>未知异常</summary>
    Unknown
}

/// <summary>
/// 异常处理结果
/// </summary>
public class ExceptionHandleResult
{
    /// <summary>是否成功处理</summary>
    public bool Success { get; set; }
    /// <summary>用户友好的错误信息</summary>
    public string UserMessage { get; set; } = string.Empty;
    /// <summary>详细错误信息（技术细节）</summary>
    public string TechnicalDetails { get; set; } = string.Empty;
    /// <summary>异常类型</summary>
    public ExceptionType ExceptionType { get; set; }
    /// <summary>是否需要重试</summary>
    public bool CanRetry { get; set; }
    /// <summary>日志ID</summary>
    public string LogId { get; set; } = string.Empty;
}

/// <summary>
/// 全局异常处理帮助类
/// </summary>
public static class ExceptionHelper
{
    private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
    private static readonly object LogLock = new();

    static ExceptionHelper()
    {
        // 确保日志目录存在
        if (!Directory.Exists(LogDirectory))
        {
            Directory.CreateDirectory(LogDirectory);
        }
    }

    /// <summary>
    /// 处理异常并返回处理结果
    /// </summary>
    public static ExceptionHandleResult HandleException(Exception ex, string? operation = null, bool showDialog = true)
    {
        var result = AnalyzeException(ex, operation);
        
        // 记录日志
        LogException(ex, result, operation);
        
        // 显示错误对话框
        if (showDialog)
        {
            ShowErrorDialog(result);
        }
        
        return result;
    }

    /// <summary>
    /// 静默处理异常（不显示对话框，只记录日志）
    /// </summary>
    public static ExceptionHandleResult HandleExceptionSilent(Exception ex, string? operation = null)
    {
        var result = AnalyzeException(ex, operation);
        LogException(ex, result, operation);
        return result;
    }

    /// <summary>
    /// 分析异常类型并生成处理结果
    /// </summary>
    private static ExceptionHandleResult AnalyzeException(Exception ex, string? operation)
    {
        var result = new ExceptionHandleResult
        {
            LogId = GenerateLogId(),
            TechnicalDetails = GetFullExceptionDetails(ex)
        };

        // 根据异常类型分类
        switch (ex)
        {
            case SqlException sqlEx:
                result.ExceptionType = ExceptionType.Database;
                result.UserMessage = GetDatabaseErrorMessage(sqlEx);
                result.CanRetry = IsRetryableSqlError(sqlEx);
                break;

            case DbException dbEx:
                result.ExceptionType = ExceptionType.Database;
                result.UserMessage = "数据库操作失败，请稍后重试或联系管理员。";
                result.CanRetry = true;
                break;

            case SocketException socketEx:
                result.ExceptionType = ExceptionType.Network;
                result.UserMessage = "网络连接异常，请检查网络设置后重试。";
                result.CanRetry = true;
                break;

            case WebException webEx:
                result.ExceptionType = ExceptionType.Network;
                result.UserMessage = "网络请求失败，请检查网络连接后重试。";
                result.CanRetry = true;
                break;

            case InvalidOperationException invEx when invEx.Message.Contains("权限"):
                result.ExceptionType = ExceptionType.Authorization;
                result.UserMessage = "您没有执行此操作的权限，请联系管理员。";
                result.CanRetry = false;
                break;

            case ArgumentNullException argNullEx:
            case ArgumentException argEx:
                result.ExceptionType = ExceptionType.Validation;
                result.UserMessage = "输入数据无效，请检查输入内容后重试。";
                result.CanRetry = false;
                break;

            case BusinessException businessEx:
                result.ExceptionType = ExceptionType.Business;
                result.UserMessage = businessEx.Message;
                result.CanRetry = businessEx.CanRetry;
                break;

            case ValidationException validationEx:
                result.ExceptionType = ExceptionType.Validation;
                result.UserMessage = validationEx.Message;
                result.CanRetry = false;
                break;

            case UnauthorizedAccessException:
                result.ExceptionType = ExceptionType.Authorization;
                result.UserMessage = "访问被拒绝，您没有执行此操作的权限。";
                result.CanRetry = false;
                break;

            default:
                result.ExceptionType = ExceptionType.System;
                result.UserMessage = "系统出现错误，请稍后重试或联系管理员。";
                result.CanRetry = false;
                break;
        }

        return result;
    }

    /// <summary>
    /// 获取数据库错误用户友好信息
    /// </summary>
    private static string GetDatabaseErrorMessage(SqlException ex)
    {
        return ex.Number switch
        {
            -1 or 2 or 53 or 258 => "无法连接到数据库服务器，请检查网络连接或联系管理员。",
            4060 => "无法访问数据库，请稍后重试或联系管理员。",
            18456 or 18452 => "数据库登录失败，请检查配置或联系管理员。",
            2627 or 2601 => "数据已存在，请勿重复添加。",
            547 => "数据关联错误，无法执行此操作。",
            1205 => "数据库操作超时，请稍后重试。",
            50000 => ex.Message, // 自定义错误消息
            _ => "数据库操作失败，请稍后重试或联系管理员。"
        };
    }

    /// <summary>
    /// 判断SQL错误是否可重试
    /// </summary>
    private static bool IsRetryableSqlError(SqlException ex)
    {
        return ex.Number switch
        {
            -1 or 2 or 53 or 258 or 1205 => true, // 连接问题或死锁
            _ => false
        };
    }

    /// <summary>
    /// 记录异常到日志文件
    /// </summary>
    private static void LogException(Exception ex, ExceptionHandleResult result, string? operation)
    {
        try
        {
            var logFile = Path.Combine(LogDirectory, $"error_{DateTime.Now:yyyyMMdd}.log");
            var logEntry = new StringBuilder();
            
            logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [LogID: {result.LogId}]");
            logEntry.AppendLine($"[操作] {operation ?? "未指定"}");
            logEntry.AppendLine($"[异常类型] {result.ExceptionType}");
            logEntry.AppendLine($"[用户消息] {result.UserMessage}");
            logEntry.AppendLine($"[异常详情] {ex.GetType().FullName}: {ex.Message}");
            logEntry.AppendLine($"[堆栈跟踪] {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                logEntry.AppendLine($"[内部异常] {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
            }
            
            logEntry.AppendLine(new string('-', 80));
            logEntry.AppendLine();

            lock (LogLock)
            {
                File.AppendAllText(logFile, logEntry.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // 日志记录失败时不应影响主流程
        }
    }

    /// <summary>
    /// 获取完整的异常详情
    /// </summary>
    private static string GetFullExceptionDetails(Exception ex)
    {
        var sb = new StringBuilder();
        var currentEx = ex;
        var level = 0;

        while (currentEx != null)
        {
            if (level > 0) sb.AppendLine($"--- 内部异常 (Level {level}) ---");
            sb.AppendLine($"类型: {currentEx.GetType().FullName}");
            sb.AppendLine($"消息: {currentEx.Message}");
            sb.AppendLine($"堆栈: {currentEx.StackTrace}");
            sb.AppendLine();
            
            currentEx = currentEx.InnerException;
            level++;
        }

        return sb.ToString();
    }

    /// <summary>
    /// 生成日志ID
    /// </summary>
    private static string GenerateLogId()
    {
        return $"ERR{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }

    /// <summary>
    /// 显示错误对话框
    /// </summary>
    private static void ShowErrorDialog(ExceptionHandleResult result)
    {
        if (Application.OpenForms.Count > 0)
        {
            var owner = Application.OpenForms[Application.OpenForms.Count - 1];
            ErrorDialog.ShowError(owner, result);
        }
        else
        {
            ErrorDialog.ShowError(null, result);
        }
    }

    /// <summary>
    /// 执行带异常处理的操作
    /// </summary>
    public static bool ExecuteWithExceptionHandling(Action action, string operationName, bool showError = true)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            HandleException(ex, operationName, showError);
            return false;
        }
    }

    /// <summary>
    /// 执行带异常处理的操作（带返回值）
    /// </summary>
    public static T? ExecuteWithExceptionHandling<T>(Func<T> func, string operationName, bool showError = true) where T : class
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            HandleException(ex, operationName, showError);
            return null;
        }
    }

    /// <summary>
    /// 执行带异常处理的数据库操作（自动回滚事务）
    /// </summary>
    public static bool ExecuteDbOperationWithTransaction(Func<bool> operation, string operationName)
    {
        try
        {
            return operation();
        }
        catch (SqlException ex)
        {
            HandleException(ex, operationName);
            return false;
        }
        catch (DbException ex)
        {
            HandleException(ex, operationName);
            return false;
        }
        catch (Exception ex)
        {
            HandleException(ex, operationName);
            return false;
        }
    }
}

/// <summary>
/// 业务异常
/// </summary>
public class BusinessException : Exception
{
    public bool CanRetry { get; }

    public BusinessException(string message, bool canRetry = false) : base(message)
    {
        CanRetry = canRetry;
    }

    public BusinessException(string message, Exception innerException, bool canRetry = false) : base(message, innerException)
    {
        CanRetry = canRetry;
    }
}

/// <summary>
/// 验证异常
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
    public ValidationException(string message, Exception innerException) : base(message, innerException) { }
}
