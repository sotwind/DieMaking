namespace DieMaking.Models;

/// <summary>
/// 用户模型类
/// </summary>
public class User
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserID { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string RealName { get; set; } = string.Empty;

    /// <summary>
    /// 权限列表（逗号分隔）
    /// </summary>
    public string Permissions { get; set; } = string.Empty;

    /// <summary>
    /// 工作站
    /// </summary>
    public string Workstation { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginTime { get; set; }

    /// <summary>
    /// 获取权限列表
    /// </summary>
    public List<string> GetPermissionList()
    {
        if (string.IsNullOrEmpty(Permissions))
            return new List<string>();
        return Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(p => p.Trim())
                          .ToList();
    }

    /// <summary>
    /// 检查是否有指定权限
    /// </summary>
    public bool HasPermission(string permission)
    {
        var permissions = GetPermissionList();
        return permissions.Contains(permission) || permissions.Contains(PermissionKeys.SystemAdmin);
    }
}

/// <summary>
/// 当前登录用户信息（静态类，用于全局访问）
/// </summary>
public static class CurrentUser
{
    /// <summary>
    /// 当前登录用户
    /// </summary>
    public static User? User { get; set; }

    /// <summary>
    /// 是否已登录
    /// </summary>
    public static bool IsLoggedIn => User != null;

    /// <summary>
    /// 检查当前用户是否有指定权限
    /// </summary>
    public static bool HasPermission(string permission)
    {
        if (User == null) return false;
        return User.HasPermission(permission);
    }
}
