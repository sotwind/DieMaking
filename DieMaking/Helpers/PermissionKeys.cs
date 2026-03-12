namespace DieMaking.Models;

/// <summary>
/// 权限键常量定义
/// </summary>
public static class PermissionKeys
{
    // 刀模管理权限
    public const string DieManage = "刀模管理";
    public const string DieAdd = "添加刀模";
    public const string DieEdit = "修改刀模";
    public const string DieAudit = "审核刀模";
    public const string DieDelete = "删除刀模";

    // 生产管理权限
    public const string Production = "生产管理";
    public const string ProductionBoard = "生产看板";
    public const string ProcessReport = "工序报产";
    public const string CompletionQuery = "完工查询";

    // 仓库管理权限
    public const string WarehouseManage = "仓库管理";
    public const string LocationManage = "库位管理";
    public const string DieInStock = "刀模入库";
    public const string DieBorrow = "刀模领用";
    public const string DieReturn = "刀模归还";
    public const string BorrowRecord = "借用记录";
    public const string ScrapApply = "报废申请";
    public const string ScrapAudit = "报废审核";

    // 报表统计权限
    public const string Report = "报表统计";
    public const string CompletionStats = "完工统计";
    public const string InventoryStats = "库存统计";
    public const string ProcessStats = "工序统计";

    // 系统管理权限
    public const string UserManage = "用户管理";
    public const string SystemAdmin = "系统管理员";
    public const string SystemSettings = "系统设置";
    public const string OperationLog = "操作日志";

    /// <summary>
    /// 获取所有权限键列表
    /// </summary>
    public static List<string> GetAllPermissions()
    {
        return new List<string>
        {
            // 刀模管理
            DieManage, DieAdd, DieEdit, DieAudit, DieDelete,
            // 生产管理
            Production, ProductionBoard, ProcessReport, CompletionQuery,
            // 仓库管理
            WarehouseManage, LocationManage, DieInStock, DieBorrow, DieReturn,
            BorrowRecord, ScrapApply, ScrapAudit,
            // 报表统计
            Report, CompletionStats, InventoryStats, ProcessStats,
            // 系统管理
            UserManage, SystemAdmin, SystemSettings, OperationLog
        };
    }

    /// <summary>
    /// 获取默认管理员权限
    /// </summary>
    public static string GetDefaultAdminPermissions()
    {
        return string.Join(",", GetAllPermissions());
    }
}
