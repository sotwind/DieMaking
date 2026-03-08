namespace DieMaking.Models;

public class User
{
    public int UserID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string Permissions { get; set; } = string.Empty;
    public string Workstation { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreateTime { get; set; }
    public DateTime? LastLoginTime { get; set; }

    public List<string> GetPermissionList()
    {
        if (string.IsNullOrEmpty(Permissions))
            return new List<string>();

        return Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(p => p.Trim())
                         .ToList();
    }

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
