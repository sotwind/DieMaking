using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Helpers;

/// <summary>
/// 配置读取辅助类 - 支持缓存和配置变更通知
/// </summary>
public static class ConfigHelper
{
    #region 缓存

    private static readonly Dictionary<string, string> _configCache = new();
    private static readonly object _cacheLock = new();
    private static readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
    private static readonly Dictionary<string, DateTime> _cacheTimestamps = new();

    #endregion

    #region 当前配置值（快速访问）

    // 系统基本信息
    public static string SystemName { get; private set; } = "刀模管理系统";
    public static string SystemVersion { get; private set; } = "1.0.0";
    public static string CompanyName { get; private set; } = "";

    // 分页和格式
    public static int DefaultPageSize { get; private set; } = 20;
    public static string DateFormat { get; private set; } = "yyyy-MM-dd";
    public static string TimeFormat { get; private set; } = "HH:mm:ss";
    public static string DateTimeFormat { get; private set; } = "yyyy-MM-dd HH:mm:ss";

    // 路径
    public static string FileUploadPath { get; private set; } = @"C:\DieMaking\Uploads";

    // 日志
    public static LogLevel LogLevel { get; private set; } = LogLevel.Info;
    public static int LogRetentionDays { get; private set; } = 30;

    // 功能开关
    public static bool EnableAudit { get; private set; } = true;
    public static bool EnableOperationLog { get; private set; } = true;

    #endregion

    #region 事件

    /// <summary>
    /// 配置变更事件
    /// </summary>
    public static event EventHandler<ConfigChangedEventArgs>? ConfigChanged;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化配置（在应用程序启动时调用）
    /// </summary>
    public static void Initialize()
    {
        try
        {
            var configService = new ConfigService();

            // 加载所有配置到缓存
            RefreshCache(configService);

            // 订阅配置变更事件
            ConfigService.ConfigChanged += OnConfigChanged;

            // 启动定时刷新
            StartAutoRefresh();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleExceptionSilent(ex, "初始化配置");
        }
    }

    #endregion

    #region 缓存管理

    /// <summary>
    /// 尝试从缓存获取配置值
    /// </summary>
    public static bool TryGetFromCache(string key, out string? value)
    {
        lock (_cacheLock)
        {
            if (_configCache.TryGetValue(key, out var cachedValue))
            {
                // 检查缓存是否过期
                if (_cacheTimestamps.TryGetValue(key, out var timestamp))
                {
                    if (DateTime.Now - timestamp < _cacheExpiration)
                    {
                        value = cachedValue;
                        return true;
                    }
                    // 缓存过期，移除
                    _configCache.Remove(key);
                    _cacheTimestamps.Remove(key);
                }
            }

            value = null;
            return false;
        }
    }

    /// <summary>
    /// 添加配置到缓存
    /// </summary>
    public static void AddToCache(string key, string value)
    {
        lock (_cacheLock)
        {
            _configCache[key] = value;
            _cacheTimestamps[key] = DateTime.Now;
        }

        // 更新快速访问属性
        UpdateQuickAccessProperty(key, value);
    }

    /// <summary>
    /// 刷新缓存
    /// </summary>
    public static void RefreshCache(ConfigService? configService = null)
    {
        try
        {
            configService ??= new ConfigService();

            var configs = configService.GetAllConfigs();

            lock (_cacheLock)
            {
                _configCache.Clear();
                _cacheTimestamps.Clear();

                foreach (var config in configs)
                {
                    _configCache[config.ConfigKey] = config.ConfigValue;
                    _cacheTimestamps[config.ConfigKey] = DateTime.Now;

                    // 更新快速访问属性
                    UpdateQuickAccessProperty(config.ConfigKey, config.ConfigValue);
                }
            }
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleExceptionSilent(ex, "刷新配置缓存");
        }
    }

    /// <summary>
    /// 清除缓存
    /// </summary>
    public static void ClearCache()
    {
        lock (_cacheLock)
        {
            _configCache.Clear();
            _cacheTimestamps.Clear();
        }
    }

    #endregion

    #region 配置获取

    /// <summary>
    /// 获取配置值
    /// </summary>
    public static string? GetValue(string key, string? defaultValue = null)
    {
        // 先尝试从缓存获取
        if (TryGetFromCache(key, out var cachedValue))
        {
            return cachedValue;
        }

        // 从数据库获取
        try
        {
            var configService = new ConfigService();
            var value = configService.GetConfigValue(key, defaultValue);

            if (value != null)
            {
                AddToCache(key, value);
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
    public static int GetValueInt(string key, int defaultValue = 0)
    {
        var value = GetValue(key);
        if (int.TryParse(value, out var result))
        {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// 获取配置值（布尔值）
    /// </summary>
    public static bool GetValueBool(string key, bool defaultValue = false)
    {
        var value = GetValue(key);
        if (bool.TryParse(value, out var result))
        {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// 获取密码策略
    /// </summary>
    public static PasswordPolicy GetPasswordPolicy()
    {
        return new PasswordPolicy
        {
            MinLength = GetValueInt(ConfigKeys.PasswordMinLength, 6),
            RequireUppercase = GetValueBool(ConfigKeys.PasswordRequireUppercase, false),
            RequireLowercase = GetValueBool(ConfigKeys.PasswordRequireLowercase, false),
            RequireDigit = GetValueBool(ConfigKeys.PasswordRequireDigit, false),
            RequireSpecialChar = GetValueBool(ConfigKeys.PasswordRequireSpecialChar, false)
        };
    }

    /// <summary>
    /// 获取登录锁定策略
    /// </summary>
    public static LockoutPolicy GetLockoutPolicy()
    {
        return new LockoutPolicy
        {
            MaxFailedAttempts = GetValueInt(ConfigKeys.MaxLoginFailures, 5),
            LockoutDuration = GetValueInt(ConfigKeys.LockoutDuration, 30),
            SessionTimeout = GetValueInt(ConfigKeys.SessionTimeout, 30)
        };
    }

    #endregion

    #region 格式化方法

    /// <summary>
    /// 格式化日期
    /// </summary>
    public static string FormatDate(DateTime date)
    {
        return date.ToString(DateFormat);
    }

    /// <summary>
    /// 格式化时间
    /// </summary>
    public static string FormatTime(DateTime time)
    {
        return time.ToString(TimeFormat);
    }

    /// <summary>
    /// 格式化日期时间
    /// </summary>
    public static string FormatDateTime(DateTime dateTime)
    {
        return dateTime.ToString(DateTimeFormat);
    }

    /// <summary>
    /// 格式化日期时间（可空）
    /// </summary>
    public static string FormatDateTime(DateTime? dateTime, string nullText = "-")
    {
        return dateTime.HasValue ? dateTime.Value.ToString(DateTimeFormat) : nullText;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 更新快速访问属性
    /// </summary>
    private static void UpdateQuickAccessProperty(string key, string value)
    {
        switch (key)
        {
            case ConfigKeys.SystemName:
                SystemName = value;
                break;
            case ConfigKeys.SystemVersion:
                SystemVersion = value;
                break;
            case ConfigKeys.CompanyName:
                CompanyName = value;
                break;
            case ConfigKeys.DefaultPageSize:
                if (int.TryParse(value, out var pageSize))
                    DefaultPageSize = pageSize;
                break;
            case ConfigKeys.DateFormat:
                DateFormat = value;
                break;
            case ConfigKeys.TimeFormat:
                TimeFormat = value;
                break;
            case ConfigKeys.DateTimeFormat:
                DateTimeFormat = value;
                break;
            case ConfigKeys.FileUploadPath:
                FileUploadPath = value;
                break;
            case ConfigKeys.LogLevel:
                if (Enum.TryParse<LogLevel>(value, true, out var logLevel))
                    LogLevel = logLevel;
                break;
            case ConfigKeys.LogRetentionDays:
                if (int.TryParse(value, out var retentionDays))
                    LogRetentionDays = retentionDays;
                break;
            case ConfigKeys.EnableAudit:
                if (bool.TryParse(value, out var enableAudit))
                    EnableAudit = enableAudit;
                break;
            case ConfigKeys.EnableOperationLog:
                if (bool.TryParse(value, out var enableLog))
                    EnableOperationLog = enableLog;
                break;
        }
    }

    /// <summary>
    /// 配置变更事件处理
    /// </summary>
    private static void OnConfigChanged(object? sender, ConfigChangedEventArgs e)
    {
        // 更新缓存
        AddToCache(e.ConfigKey, e.NewValue);

        // 触发本地事件
        ConfigChanged?.Invoke(null, e);
    }

    /// <summary>
    /// 启动自动刷新
    /// </summary>
    private static void StartAutoRefresh()
    {
        // 使用定时器定期刷新缓存（每5分钟）
        var timer = new System.Threading.Timer(
            _ => RefreshCache(),
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));
    }

    #endregion
}

/// <summary>
/// 用户配置上下文（当前用户个性化设置）
/// </summary>
public static class UserConfigContext
{
    private static UserPreference? _currentUserPreference;

    /// <summary>
    /// 当前用户偏好设置
    /// </summary>
    public static UserPreference CurrentPreference
    {
        get
        {
            if (_currentUserPreference == null && CurrentUser.User != null)
            {
                LoadUserPreference();
            }
            return _currentUserPreference ?? GetDefaultPreference();
        }
        set => _currentUserPreference = value;
    }

    /// <summary>
    /// 加载当前用户偏好设置
    /// </summary>
    public static void LoadUserPreference()
    {
        if (CurrentUser.User == null) return;

        try
        {
            var configService = new ConfigService();
            _currentUserPreference = configService.GetUserPreference(CurrentUser.User.UserID);
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleExceptionSilent(ex, "加载用户偏好设置");
            _currentUserPreference = GetDefaultPreference();
        }
    }

    /// <summary>
    /// 保存当前用户偏好设置
    /// </summary>
    public static bool SaveUserPreference(UserPreference preference)
    {
        try
        {
            var configService = new ConfigService();
            if (configService.SaveUserPreference(preference))
            {
                _currentUserPreference = preference;
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "保存用户偏好设置");
            return false;
        }
    }

    /// <summary>
    /// 获取默认偏好设置
    /// </summary>
    private static UserPreference GetDefaultPreference()
    {
        return new UserPreference
        {
            UserID = CurrentUser.User?.UserID ?? 0,
            Theme = "Light",
            DefaultPageSize = ConfigHelper.DefaultPageSize,
            DateFormat = ConfigHelper.DateFormat,
            TimeFormat = ConfigHelper.TimeFormat,
            DefaultPage = DefaultPageOptions.DieList,
            UpdateTime = DateTime.Now
        };
    }

    /// <summary>
    /// 清除当前用户偏好设置缓存
    /// </summary>
    public static void ClearPreference()
    {
        _currentUserPreference = null;
    }

    /// <summary>
    /// 获取当前用户的分页大小
    /// </summary>
    public static int GetPageSize()
    {
        return CurrentPreference.DefaultPageSize > 0 ? CurrentPreference.DefaultPageSize : ConfigHelper.DefaultPageSize;
    }

    /// <summary>
    /// 获取当前用户的日期格式
    /// </summary>
    public static string GetDateFormat()
    {
        return !string.IsNullOrEmpty(CurrentPreference.DateFormat)
            ? CurrentPreference.DateFormat
            : ConfigHelper.DateFormat;
    }

    /// <summary>
    /// 获取当前用户的主题
    /// </summary>
    public static string GetTheme()
    {
        return CurrentPreference.Theme ?? "Light";
    }
}
