namespace DieMaking.Enums;

/// <summary>
/// 权限键常量定义
/// </summary>
public static class PermissionKeys
{
    // 刀模管理权限
    public const string DieManage = "刀模管理";
    public const string DieAdd = "添加刀模";
    public const string DieEdit = "修改刀模";
    public const string DieDelete = "删除刀模";
    public const string DieAudit = "审核刀模";

    // 生产管理权限
    public const string Production = "生产管理";
    public const string ProcessStart = "开始工序";
    public const string ProcessComplete = "完成工序";
    public const string ProductionBoard = "生产看板";

    // 仓库管理权限
    public const string WarehouseManage = "仓库管理";
    public const string LocationManage = "库位管理";
    public const string DieInStock = "刀模入库";
    public const string DieBorrow = "刀模领用";
    public const string DieReturn = "刀模归还";
    public const string BorrowRecord = "借用记录";

    // 报废管理权限
    public const string ScrapManage = "报废管理";
    public const string ScrapApply = "报废申请";
    public const string ScrapAudit = "报废审核";

    // 报表统计权限
    public const string Report = "报表统计";
    public const string ReportCompletion = "完工报表";
    public const string ReportInventory = "库存报表";

    // 系统管理权限
    public const string SystemAdmin = "系统管理员";
    public const string UserManage = "用户管理";
    public const string SystemConfig = "系统配置";
}
