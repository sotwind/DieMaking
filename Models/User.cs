namespace DieMaking.Models;

/// <summary>
/// 用户实体类
/// </summary>
public class User
{
    /// <summary>用户ID</summary>
    public int UserID { get; set; }
    
    /// <summary>用户名</summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>密码</summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>真实姓名</summary>
    public string RealName { get; set; } = string.Empty;
    
    /// <summary>权限列表（逗号分隔）</summary>
    public string Permissions { get; set; } = string.Empty;
    
    /// <summary>工位</summary>
    public string Workstation { get; set; } = string.Empty;
    
    /// <summary>是否启用</summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>最后登录时间</summary>
    public DateTime? LastLoginTime { get; set; }

    /// <summary>
    /// 获取权限列表
    /// </summary>
    /// <returns>权限字符串列表</returns>
    public List<string> GetPermissionList()
    {
        if (string.IsNullOrEmpty(Permissions))
            return new List<string>();

        return Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(p => p.Trim())
                         .ToList();
    }

    /// <summary>
    /// 检查是否具有指定权限
    /// </summary>
    /// <param name="permission">权限名称</param>
    /// <returns>是否具有该权限</returns>
    public bool HasPermission(string permission)
    {
        return GetPermissionList().Contains(permission);
    }
}

/// <summary>
/// 权限键名常量
/// </summary>
public static class PermissionKeys
{
    /// <summary>刀模管理</summary>
    public const string DieManage = "刀模管理";
    /// <summary>添加刀模</summary>
    public const string DieAdd = "添加刀模";
    /// <summary>修改刀模</summary>
    public const string DieEdit = "修改刀模";
    /// <summary>审核刀模</summary>
    public const string DieAudit = "审核刀模";
    /// <summary>生产管理</summary>
    public const string Production = "生产管理";
    /// <summary>仓库管理</summary>
    public const string WarehouseManage = "仓库管理";
    /// <summary>库位管理</summary>
    public const string LocationManage = "库位管理";
    /// <summary>刀模入库</summary>
    public const string DieInStock = "刀模入库";
    /// <summary>刀模领用</summary>
    public const string DieBorrow = "刀模领用";
    /// <summary>刀模归还</summary>
    public const string DieReturn = "刀模归还";
    /// <summary>借用记录</summary>
    public const string BorrowRecord = "借用记录";
    /// <summary>报废申请</summary>
    public const string ScrapApply = "报废申请";
    /// <summary>报废审核</summary>
    public const string ScrapAudit = "报废审核";
    /// <summary>报表统计</summary>
    public const string Report = "报表统计";
    /// <summary>用户管理</summary>
    public const string UserManage = "用户管理";
    /// <summary>系统设置</summary>
    public const string SystemSettings = "系统设置";
    /// <summary>系统管理员</summary>
    public const string SystemAdmin = "系统管理员";
}

/// <summary>
/// 当前用户上下文
/// </summary>
public static class CurrentUser
{
    /// <summary>当前登录用户</summary>
    public static User? User { get; set; }

    /// <summary>是否已登录</summary>
    public static bool IsLoggedIn => User != null;

    /// <summary>
    /// 检查当前用户是否具有指定权限
    /// </summary>
    /// <param name="permission">权限名称</param>
    /// <returns>是否具有该权限</returns>
    public static bool HasPermission(string permission)
    {
        return User?.HasPermission(permission) ?? false;
    }
}
