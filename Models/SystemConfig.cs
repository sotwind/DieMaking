namespace DieMaking.Models;

/// <summary>
/// 系统配置实体
/// </summary>
public class SystemConfig
{
    /// <summary>配置ID</summary>
    public int ConfigID { get; set; }
    
    /// <summary>配置键名</summary>
    public string ConfigKey { get; set; } = string.Empty;
    
    /// <summary>配置值</summary>
    public string ConfigValue { get; set; } = string.Empty;
    
    /// <summary>配置描述</summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>更新时间</summary>
    public DateTime? UpdateTime { get; set; }
}

/// <summary>
/// 系统配置键名常量
/// </summary>
public static class ConfigKeys
{
    // 基本设置
    public const string SystemName = "SystemName";
    public const string CompanyName = "CompanyName";
    public const string SystemVersion = "SystemVersion";
    public const string DefaultPageSize = "DefaultPageSize";
    public const string DateFormat = "DateFormat";
    public const string TimeFormat = "TimeFormat";
    public const string DateTimeFormat = "DateTimeFormat";
    public const string FileUploadPath = "FileUploadPath";

    // 安全设置 - 密码策略
    public const string PasswordMinLength = "PasswordMinLength";
    public const string PasswordRequireUppercase = "PasswordRequireUppercase";
    public const string PasswordRequireLowercase = "PasswordRequireLowercase";
    public const string PasswordRequireDigit = "PasswordRequireDigit";
    public const string PasswordRequireSpecialChar = "PasswordRequireSpecialChar";

    // 安全设置 - 登录策略
    public const string MaxLoginFailures = "MaxLoginFailures";
    public const string LockoutDuration = "LockoutDuration";
    public const string SessionTimeout = "SessionTimeout";

    // 日志设置
    public const string LogLevel = "LogLevel";
    public const string LogRetentionDays = "LogRetentionDays";

    // 功能开关
    public const string EnableAudit = "EnableAudit";
    public const string EnableOperationLog = "EnableOperationLog";
}

/// <summary>
/// 用户个性化设置扩展
/// </summary>
public class UserPreference
{
    public int UserID { get; set; }
    public string Theme { get; set; } = "Light"; // Light / Dark
    public int DefaultPageSize { get; set; } = 20;
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public string TimeFormat { get; set; } = "HH:mm:ss";
    public string DefaultPage { get; set; } = "DieList"; // 登录后默认页面
    public DateTime UpdateTime { get; set; }
}

/// <summary>
/// 主题类型
/// </summary>
public enum ThemeType
{
    Light,
    Dark
}

/// <summary>
/// 日志级别
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

/// <summary>
/// 默认页面选项
/// </summary>
public static class DefaultPageOptions
{
    public const string DieList = "DieList";
    public const string ProductionBoard = "ProductionBoard";
    public const string Warehouse = "Warehouse";
    public const string Report = "Report";

    public static readonly Dictionary<string, string> DisplayNames = new()
    {
        { DieList, "刀模列表" },
        { ProductionBoard, "生产看板" },
        { Warehouse, "仓库管理" },
        { Report, "报表统计" }
    };
}

/// <summary>
/// 配置变更事件参数
/// </summary>
public class ConfigChangedEventArgs : EventArgs
{
    public string ConfigKey { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public DateTime ChangedTime { get; set; }
}
