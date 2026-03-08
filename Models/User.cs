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

public static class PermissionKeys
{
    public const string DieManage = "刀模管理";
    public const string DieAdd = "添加刀模";
    public const string DieEdit = "修改刀模";
    public const string DieAudit = "审核刀模";
    public const string Production = "生产管理";
    public const string WarehouseManage = "仓库管理";
    public const string LocationManage = "库位管理";
    public const string DieInStock = "刀模入库";
    public const string DieBorrow = "刀模领用";
    public const string DieReturn = "刀模归还";
    public const string BorrowRecord = "借用记录";
    public const string ScrapApply = "报废申请";
    public const string ScrapAudit = "报废审核";
    public const string Report = "报表统计";
    public const string UserManage = "用户管理";
    public const string SystemAdmin = "系统管理员";
}

public static class CurrentUser
{
    public static User? User { get; set; }

    public static bool IsLoggedIn => User != null;

    public static bool HasPermission(string permission)
    {
        return User?.HasPermission(permission) ?? false;
    }
}
