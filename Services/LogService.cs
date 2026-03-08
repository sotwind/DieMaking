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
    /// 记录操作日志
    /// </summary>
    /// <param name="operationType">操作类型（如：新增、修改、删除、审核等）</param>
    /// <param name="content">操作内容描述</param>
    /// <param name="dieNo">关联的刀模编号（可选）</param>
    public static void LogOperation(string operationType, string content, string? dieNo = null)
    {
        try
        {
            // 异步记录日志，避免阻塞主流程
            Task.Run(() => DoLogOperation(operationType, content, dieNo));
        }
        catch
        {
            // 日志记录失败不影响主业务流程
        }
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
            DoLogOperation(operationType, content, dieNo);
        }
        catch
        {
            // 日志记录失败不影响主业务流程
        }
    }

    /// <summary>
    /// 执行实际的日志记录操作
    /// </summary>
    private static void DoLogOperation(string operationType, string content, string? dieNo)
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
            var sql = @"INSERT INTO DM_OperationLog (UserID, Username, OperationType, OperationDesc, DieID, IPAddress, CreateTime) 
                        VALUES (@UserID, @Username, @OperationType, @OperationDesc, @DieID, @IPAddress, GETDATE())";

            DbHelper.ExecuteNonQuery(sql,
                new SqlParameter("@UserID", userId ?? (object)DBNull.Value),
                new SqlParameter("@Username", username),
                new SqlParameter("@OperationType", operationType),
                new SqlParameter("@OperationDesc", content),
                new SqlParameter("@DieID", dieId ?? (object)DBNull.Value),
                new SqlParameter("@IPAddress", ipAddress));
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
}
