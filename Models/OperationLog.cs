namespace DieMaking.Models;

/// <summary>
/// 操作日志实体类
/// </summary>
public class OperationLog
{
    /// <summary>日志ID</summary>
    public int LogID { get; set; }

    /// <summary>用户ID</summary>
    public int? UserID { get; set; }

    /// <summary>用户名</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>操作类型</summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>操作描述</summary>
    public string OperationDesc { get; set; } = string.Empty;

    /// <summary>关联刀模ID</summary>
    public int? DieID { get; set; }

    /// <summary>IP地址</summary>
    public string IPAddress { get; set; } = string.Empty;

    /// <summary>日志级别</summary>
    public string LogLevel { get; set; } = "Info";

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }
}
