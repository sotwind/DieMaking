using DieMaking.Helpers;
using DieMaking.Models;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

/// <summary>
/// 系统配置管理服务
/// </summary>
public class ConfigService
{
    #region 事件

    /// <summary>
    /// 配置变更事件
    /// </summary>
    public static event EventHandler<ConfigChangedEventArgs>? ConfigChanged;

    #endregion

    #region 配置查询

    /// <summary>
    /// 获取所有配置项
    /// </summary>
    public List<SystemConfig> GetAllConfigs()
    {
        try
        {
            var sql = "SELECT * FROM DM_SystemConfig ORDER BY ConfigKey";
            return DbHelper.ExecuteQuery(sql, MapToConfig);
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "获取所有配置");
            return new List<SystemConfig>();
        }
    }

    /// <summary>
    /// 根据键名获取配置值
    /// </summary>
    public string? GetConfigValue(string key, string? defaultValue = null)
    {
        try
        {
            // 先尝试从缓存获取
            if (ConfigHelper.TryGetFromCache(key, out var cachedValue))
            {
                return cachedValue;
            }

            var sql = "SELECT ConfigValue FROM DM_SystemConfig WHERE ConfigKey = @ConfigKey";
            var result = DbHelper.ExecuteScalar(sql, new SqlParameter("@ConfigKey", key));

            var value = result?.ToString() ?? defaultValue;

            // 添加到缓存
            if (value != null)
            {
                ConfigHelper.AddToCache(key, value);
            }

            return value;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleExceptionSilent(ex, $"获取配置({key})");
            return defaultValue;
        }
    }

    /// <summary>
    /// 获取配置值（整数）
    /// </summary>
    public int GetConfigValueInt(string key, int defaultValue = 0)
    {
        var value = GetConfigValue(key);
        if (int.TryParse(value, out var result))
        {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// 获取配置值（布尔值）
    /// </summary>
    public bool GetConfigValueBool(string key, bool defaultValue = false)
    {
        var value = GetConfigValue(key);
        if (bool.TryParse(value, out var result))
        {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// 获取系统名称
    /// </summary>
    public string GetSystemName()
    {
        return GetConfigValue(ConfigKeys.SystemName, "刀模管理系统") ?? "刀模管理系统";
    }

    /// <summary>
    /// 获取系统版本
    /// </summary>
    public string GetSystemVersion()
    {
        return GetConfigValue(ConfigKeys.SystemVersion, "1.0.0") ?? "1.0.0";
    }

    /// <summary>
    /// 获取默认分页大小
    /// </summary>
    public int GetDefaultPageSize()
    {
        return GetConfigValueInt(ConfigKeys.DefaultPageSize, 20);
    }

    /// <summary>
    /// 获取日期格式
    /// </summary>
    public string GetDateFormat()
    {
        return GetConfigValue(ConfigKeys.DateFormat, "yyyy-MM-dd") ?? "yyyy-MM-dd";
    }

    /// <summary>
    /// 获取时间格式
    /// </summary>
    public string GetTimeFormat()
    {
        return GetConfigValue(ConfigKeys.TimeFormat, "HH:mm:ss") ?? "HH:mm:ss";
    }

    /// <summary>
    /// 获取完整日期时间格式
    /// </summary>
    public string GetDateTimeFormat()
    {
        return GetConfigValue(ConfigKeys.DateTimeFormat, "yyyy-MM-dd HH:mm:ss") ?? "yyyy-MM-dd HH:mm:ss";
    }

    /// <summary>
    /// 获取文件上传路径
    /// </summary>
    public string GetFileUploadPath()
    {
        return GetConfigValue(ConfigKeys.FileUploadPath, @"C:\DieMaking\Uploads") ?? @"C:\DieMaking\Uploads";
    }

    /// <summary>
    /// 获取日志保留天数
    /// </summary>
    public int GetLogRetentionDays()
    {
        return GetConfigValueInt(ConfigKeys.LogRetentionDays, 30);
    }

    /// <summary>
    /// 获取密码策略
    /// </summary>
    public PasswordPolicy GetPasswordPolicy()
    {
        return new PasswordPolicy
        {
            MinLength = GetConfigValueInt(ConfigKeys.PasswordMinLength, 6),
            RequireUppercase = GetConfigValueBool(ConfigKeys.PasswordRequireUppercase, false),
            RequireLowercase = GetConfigValueBool(ConfigKeys.PasswordRequireLowercase, false),
            RequireDigit = GetConfigValueBool(ConfigKeys.PasswordRequireDigit, false),
            RequireSpecialChar = GetConfigValueBool(ConfigKeys.PasswordRequireSpecialChar, false)
        };
    }

    /// <summary>
    /// 获取登录锁定策略
    /// </summary>
    public LockoutPolicy GetLockoutPolicy()
    {
        return new LockoutPolicy
        {
            MaxFailedAttempts = GetConfigValueInt(ConfigKeys.MaxLoginFailures, 5),
            LockoutDuration = GetConfigValueInt(ConfigKeys.LockoutDuration, 30),
            SessionTimeout = GetConfigValueInt(ConfigKeys.SessionTimeout, 30)
        };
    }

    #endregion

    #region 配置更新

    /// <summary>
    /// 更新配置值
    /// </summary>
    public bool UpdateConfig(string key, string value)
    {
        try
        {
            // 获取旧值
            var oldValue = GetConfigValue(key, "");

            var sql = @"
                UPDATE DM_SystemConfig 
                SET ConfigValue = @ConfigValue, UpdateTime = GETDATE() 
                WHERE ConfigKey = @ConfigKey;
                
                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO DM_SystemConfig (ConfigKey, ConfigValue, Description, CreateTime)
                    VALUES (@ConfigKey, @ConfigValue, '', GETDATE());
                END";

            var result = DbHelper.ExecuteNonQuery(sql,
                new SqlParameter("@ConfigKey", key),
                new SqlParameter("@ConfigValue", value));

            if (result > 0)
            {
                // 更新缓存
                ConfigHelper.AddToCache(key, value);

                // 触发变更事件
                OnConfigChanged(key, oldValue ?? "", value);

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, $"更新配置({key})");
            return false;
        }
    }

    /// <summary>
    /// 批量更新配置
    /// </summary>
    public bool UpdateConfigs(Dictionary<string, string> configs)
    {
        try
        {
            return DbHelper.ExecuteTransaction((connection, transaction) =>
            {
                foreach (var (key, value) in configs)
                {
                    var oldValue = GetConfigValue(key, "");

                    var sql = @"
                        UPDATE DM_SystemConfig 
                        SET ConfigValue = @ConfigValue, UpdateTime = GETDATE() 
                        WHERE ConfigKey = @ConfigKey;
                        
                        IF @@ROWCOUNT = 0
                        BEGIN
                            INSERT INTO DM_SystemConfig (ConfigKey, ConfigValue, Description, CreateTime)
                            VALUES (@ConfigKey, @ConfigValue, '', GETDATE());
                        END";

                    using var command = new SqlCommand(sql, connection, transaction);
                    command.Parameters.AddWithValue("@ConfigKey", key);
                    command.Parameters.AddWithValue("@ConfigValue", value);
                    command.ExecuteNonQuery();

                    // 更新缓存
                    ConfigHelper.AddToCache(key, value);

                    // 触发变更事件
                    OnConfigChanged(key, oldValue ?? "", value);
                }

                return true;
            });
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "批量更新配置");
            return false;
        }
    }

    /// <summary>
    /// 初始化默认配置
    /// </summary>
    public bool InitializeDefaultConfigs()
    {
        var defaultConfigs = new Dictionary<string, (string value, string description)>
        {
            // 基本设置
            { ConfigKeys.SystemName, ("刀模管理系统", "系统名称") },
            { ConfigKeys.CompanyName, ("", "公司名称") },
            { ConfigKeys.SystemVersion, ("1.0.0", "系统版本") },
            { ConfigKeys.DefaultPageSize, ("20", "默认分页大小") },
            { ConfigKeys.DateFormat, ("yyyy-MM-dd", "日期格式") },
            { ConfigKeys.TimeFormat, ("HH:mm:ss", "时间格式") },
            { ConfigKeys.DateTimeFormat, ("yyyy-MM-dd HH:mm:ss", "日期时间格式") },
            { ConfigKeys.FileUploadPath, (@"C:\DieMaking\Uploads", "文件上传路径") },

            // 安全设置 - 密码策略
            { ConfigKeys.PasswordMinLength, ("6", "密码最小长度") },
            { ConfigKeys.PasswordRequireUppercase, ("false", "密码要求大写字母") },
            { ConfigKeys.PasswordRequireLowercase, ("false", "密码要求小写字母") },
            { ConfigKeys.PasswordRequireDigit, ("false", "密码要求数字") },
            { ConfigKeys.PasswordRequireSpecialChar, ("false", "密码要求特殊字符") },

            // 安全设置 - 登录策略
            { ConfigKeys.MaxLoginFailures, ("5", "最大登录失败次数") },
            { ConfigKeys.LockoutDuration, ("30", "账户锁定时间(分钟)") },
            { ConfigKeys.SessionTimeout, ("30", "会话超时时间(分钟)") },

            // 日志设置
            { ConfigKeys.LogLevel, ("Info", "日志级别") },
            { ConfigKeys.LogRetentionDays, ("30", "日志保留天数") },

            // 功能开关
            { ConfigKeys.EnableAudit, ("true", "启用审核流程") },
            { ConfigKeys.EnableOperationLog, ("true", "启用操作日志") }
        };

        try
        {
            return DbHelper.ExecuteTransaction((connection, transaction) =>
            {
                foreach (var (key, (value, description)) in defaultConfigs)
                {
                    var checkSql = "SELECT COUNT(*) FROM DM_SystemConfig WHERE ConfigKey = @ConfigKey";
                    using var checkCommand = new SqlCommand(checkSql, connection, transaction);
                    checkCommand.Parameters.AddWithValue("@ConfigKey", key);
                    var exists = (int)checkCommand.ExecuteScalar()! > 0;

                    if (!exists)
                    {
                        var insertSql = @"
                            INSERT INTO DM_SystemConfig (ConfigKey, ConfigValue, Description, CreateTime)
                            VALUES (@ConfigKey, @ConfigValue, @Description, GETDATE())";

                        using var command = new SqlCommand(insertSql, connection, transaction);
                        command.Parameters.AddWithValue("@ConfigKey", key);
                        command.Parameters.AddWithValue("@ConfigValue", value);
                        command.Parameters.AddWithValue("@Description", description);
                        command.ExecuteNonQuery();
                    }
                }

                return true;
            });
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "初始化默认配置");
            return false;
        }
    }

    #endregion

    #region 用户个性化设置

    /// <summary>
    /// 获取用户个性化设置
    /// </summary>
    public UserPreference GetUserPreference(int userId)
    {
        try
        {
            var sql = "SELECT * FROM DM_UserPreference WHERE UserID = @UserID";
            var preferences = DbHelper.ExecuteQuery(sql, MapToPreference, new SqlParameter("@UserID", userId));

            if (preferences.FirstOrDefault() is UserPreference preference)
            {
                return preference;
            }

            // 返回默认设置
            return GetDefaultUserPreference(userId);
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, $"获取用户偏好设置(UserID:{userId})");
            return GetDefaultUserPreference(userId);
        }
    }

    /// <summary>
    /// 保存用户个性化设置
    /// </summary>
    public bool SaveUserPreference(UserPreference preference)
    {
        try
        {
            var sql = @"
                IF EXISTS (SELECT 1 FROM DM_UserPreference WHERE UserID = @UserID)
                BEGIN
                    UPDATE DM_UserPreference SET
                        Theme = @Theme,
                        DefaultPageSize = @DefaultPageSize,
                        DateFormat = @DateFormat,
                        TimeFormat = @TimeFormat,
                        DefaultPage = @DefaultPage,
                        UpdateTime = GETDATE()
                    WHERE UserID = @UserID;
                END
                ELSE
                BEGIN
                    INSERT INTO DM_UserPreference (UserID, Theme, DefaultPageSize, DateFormat, TimeFormat, DefaultPage, UpdateTime)
                    VALUES (@UserID, @Theme, @DefaultPageSize, @DateFormat, @TimeFormat, @DefaultPage, GETDATE());
                END";

            return DbHelper.ExecuteNonQuery(sql,
                new SqlParameter("@UserID", preference.UserID),
                new SqlParameter("@Theme", preference.Theme),
                new SqlParameter("@DefaultPageSize", preference.DefaultPageSize),
                new SqlParameter("@DateFormat", preference.DateFormat),
                new SqlParameter("@TimeFormat", preference.TimeFormat),
                new SqlParameter("@DefaultPage", preference.DefaultPage)) > 0;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, $"保存用户偏好设置(UserID:{preference.UserID})");
            return false;
        }
    }

    /// <summary>
    /// 获取默认用户偏好设置
    /// </summary>
    private UserPreference GetDefaultUserPreference(int userId)
    {
        return new UserPreference
        {
            UserID = userId,
            Theme = "Light",
            DefaultPageSize = GetDefaultPageSize(),
            DateFormat = GetDateFormat(),
            TimeFormat = GetTimeFormat(),
            DefaultPage = DefaultPageOptions.DieList,
            UpdateTime = DateTime.Now
        };
    }

    #endregion

    #region 密码策略验证

    /// <summary>
    /// 验证密码是否符合策略
    /// </summary>
    public (bool isValid, string message) ValidatePassword(string password)
    {
        var policy = GetPasswordPolicy();
        var errors = new List<string>();

        if (password.Length < policy.MinLength)
        {
            errors.Add($"密码长度至少为 {policy.MinLength} 位");
        }

        if (policy.RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add("密码必须包含大写字母");
        }

        if (policy.RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add("密码必须包含小写字母");
        }

        if (policy.RequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add("密码必须包含数字");
        }

        if (policy.RequireSpecialChar && !password.Any(c => !char.IsLetterOrDigit(c)))
        {
            errors.Add("密码必须包含特殊字符");
        }

        if (errors.Count > 0)
        {
            return (false, string.Join("；", errors));
        }

        return (true, "密码符合要求");
    }

    #endregion

    #region 私有方法

    private SystemConfig MapToConfig(SqlDataReader reader)
    {
        return new SystemConfig
        {
            ConfigID = Convert.ToInt32(reader["ConfigID"]),
            ConfigKey = reader["ConfigKey"].ToString() ?? "",
            ConfigValue = reader["ConfigValue"].ToString() ?? "",
            Description = reader["Description"].ToString() ?? "",
            CreateTime = reader["CreateTime"] != DBNull.Value ? Convert.ToDateTime(reader["CreateTime"]) : DateTime.MinValue,
            UpdateTime = reader["UpdateTime"] != DBNull.Value ? Convert.ToDateTime(reader["UpdateTime"]) : null
        };
    }

    private UserPreference MapToPreference(SqlDataReader reader)
    {
        return new UserPreference
        {
            UserID = Convert.ToInt32(reader["UserID"]),
            Theme = reader["Theme"].ToString() ?? "Light",
            DefaultPageSize = reader["DefaultPageSize"] != DBNull.Value ? Convert.ToInt32(reader["DefaultPageSize"]) : 20,
            DateFormat = reader["DateFormat"].ToString() ?? "yyyy-MM-dd",
            TimeFormat = reader["TimeFormat"].ToString() ?? "HH:mm:ss",
            DefaultPage = reader["DefaultPage"].ToString() ?? DefaultPageOptions.DieList,
            UpdateTime = reader["UpdateTime"] != DBNull.Value ? Convert.ToDateTime(reader["UpdateTime"]) : DateTime.Now
        };
    }

    private void OnConfigChanged(string key, string oldValue, string newValue)
    {
        ConfigChanged?.Invoke(this, new ConfigChangedEventArgs
        {
            ConfigKey = key,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedTime = DateTime.Now
        });
    }

    #endregion
}

/// <summary>
/// 密码策略
/// </summary>
public class PasswordPolicy
{
    public int MinLength { get; set; } = 6;
    public bool RequireUppercase { get; set; } = false;
    public bool RequireLowercase { get; set; } = false;
    public bool RequireDigit { get; set; } = false;
    public bool RequireSpecialChar { get; set; } = false;

    public override string ToString()
    {
        var requirements = new List<string>();
        requirements.Add($"最少{MinLength}位");
        if (RequireUppercase) requirements.Add("大写字母");
        if (RequireLowercase) requirements.Add("小写字母");
        if (RequireDigit) requirements.Add("数字");
        if (RequireSpecialChar) requirements.Add("特殊字符");
        return string.Join("、", requirements);
    }
}

/// <summary>
/// 登录锁定策略
/// </summary>
public class LockoutPolicy
{
    public int MaxFailedAttempts { get; set; } = 5;
    public int LockoutDuration { get; set; } = 30;
    public int SessionTimeout { get; set; } = 30;
}
