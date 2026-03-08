namespace DieMaking.Models;

/// <summary>
/// 库位实体类
/// </summary>
public class StorageLocation
{
    /// <summary>库位ID</summary>
    public int LocationID { get; set; }
    
    /// <summary>库位编号</summary>
    public string LocationCode { get; set; } = string.Empty;
    
    /// <summary>区域</summary>
    public string Area { get; set; } = string.Empty;
    
    /// <summary>架号</summary>
    public string ShelfNo { get; set; } = string.Empty;
    
    /// <summary>层号</summary>
    public string LayerNo { get; set; } = string.Empty;
    
    /// <summary>位号</summary>
    public string PositionNo { get; set; } = string.Empty;
    
    /// <summary>描述</summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>库位状态</summary>
    public LocationStatus Status { get; set; } = LocationStatus.Free;
    
    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>状态显示文本</summary>
    public string StatusText => Status.GetDisplayName();
}

/// <summary>
/// 刀模库存实体类
/// </summary>
public class DieInventory
{
    /// <summary>库存ID</summary>
    public int InventoryID { get; set; }
    
    /// <summary>刀模ID</summary>
    public int DieID { get; set; }
    
    /// <summary>库位ID</summary>
    public int? LocationID { get; set; }
    
    /// <summary>存储状态</summary>
    public StorageStatus StorageStatus { get; set; } = StorageStatus.InStock;
    
    /// <summary>入库时间</summary>
    public DateTime? InStockTime { get; set; }
    
    /// <summary>最后借出时间</summary>
    public DateTime? LastBorrowTime { get; set; }
    
    /// <summary>最后归还时间</summary>
    public DateTime? LastReturnTime { get; set; }
    
    /// <summary>总借用次数</summary>
    public int TotalBorrowCount { get; set; }
    
    /// <summary>备注</summary>
    public string Remark { get; set; } = string.Empty;
    
    /// <summary>更新时间</summary>
    public DateTime UpdateTime { get; set; }

    // 关联信息
    /// <summary>库位编号</summary>
    public string? LocationCode { get; set; }
    
    /// <summary>刀模编号</summary>
    public string? DieCode { get; set; }
    
    /// <summary>客户名称</summary>
    public string? CustomerName { get; set; }
    
    /// <summary>产品名称</summary>
    public string? ProductName { get; set; }

    /// <summary>存储状态显示文本</summary>
    public string StorageStatusText => StorageStatus.GetDisplayName();
}

/// <summary>
/// 刀模借用记录实体类
/// </summary>
public class DieBorrowRecord
{
    /// <summary>借用ID</summary>
    public int BorrowID { get; set; }
    
    /// <summary>刀模ID</summary>
    public int DieID { get; set; }
    
    /// <summary>库存ID</summary>
    public int InventoryID { get; set; }
    
    /// <summary>借用类型</summary>
    public BorrowType BorrowType { get; set; } = BorrowType.Production;
    
    /// <summary>借用人编号</summary>
    public string BorrowerNo { get; set; } = string.Empty;
    
    /// <summary>借用人姓名</summary>
    public string BorrowerName { get; set; } = string.Empty;
    
    /// <summary>借用部门</summary>
    public string BorrowDept { get; set; } = string.Empty;
    
    /// <summary>借用时间</summary>
    public DateTime BorrowTime { get; set; }
    
    /// <summary>预计归还时间</summary>
    public DateTime? ExpectedReturnTime { get; set; }
    
    /// <summary>实际归还时间</summary>
    public DateTime? ActualReturnTime { get; set; }
    
    /// <summary>用途</summary>
    public string Purpose { get; set; } = string.Empty;
    
    /// <summary>借用状态</summary>
    public BorrowStatus Status { get; set; } = BorrowStatus.Borrowing;
    
    /// <summary>归还操作员工号</summary>
    public string ReturnOperatorNo { get; set; } = string.Empty;
    
    /// <summary>归还操作员姓名</summary>
    public string ReturnOperatorName { get; set; } = string.Empty;
    
    /// <summary>备注</summary>
    public string Remark { get; set; } = string.Empty;
    
    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    // 关联信息
    /// <summary>刀模编号</summary>
    public string? DieCode { get; set; }
    
    /// <summary>客户名称</summary>
    public string? CustomerName { get; set; }
    
    /// <summary>产品名称</summary>
    public string? ProductName { get; set; }

    /// <summary>借用类型显示文本</summary>
    public string BorrowTypeText => BorrowType.GetDisplayName();
    
    /// <summary>状态显示文本</summary>
    public string StatusText => Status.GetDisplayName();
}

/// <summary>
/// 刀模报废记录实体类
/// </summary>
public class DieScrapRecord
{
    /// <summary>报废ID</summary>
    public int ScrapID { get; set; }
    
    /// <summary>刀模ID</summary>
    public int DieID { get; set; }
    
    /// <summary>库存ID</summary>
    public int InventoryID { get; set; }
    
    /// <summary>报废原因</summary>
    public string ScrapReason { get; set; } = string.Empty;
    
    /// <summary>报废类型</summary>
    public string ScrapType { get; set; } = string.Empty;
    
    /// <summary>申请人工号</summary>
    public string ApplicantNo { get; set; } = string.Empty;
    
    /// <summary>申请人姓名</summary>
    public string ApplicantName { get; set; } = string.Empty;
    
    /// <summary>申请时间</summary>
    public DateTime ApplyTime { get; set; }
    
    /// <summary>审核人工号</summary>
    public string? AuditorNo { get; set; }
    
    /// <summary>审核人姓名</summary>
    public string? AuditorName { get; set; }
    
    /// <summary>审核时间</summary>
    public DateTime? AuditTime { get; set; }
    
    /// <summary>审核状态</summary>
    public ScrapAuditStatus AuditStatus { get; set; } = ScrapAuditStatus.Pending;
    
    /// <summary>审核备注</summary>
    public string? AuditRemark { get; set; }
    
    /// <summary>报废时间</summary>
    public DateTime? ScrapTime { get; set; }
    
    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    // 关联信息
    /// <summary>刀模编号</summary>
    public string? DieCode { get; set; }
    
    /// <summary>客户名称</summary>
    public string? CustomerName { get; set; }
    
    /// <summary>产品名称</summary>
    public string? ProductName { get; set; }

    /// <summary>审核状态显示文本</summary>
    public string AuditStatusText => AuditStatus.GetDisplayName();
}

// 枚举定义
public enum LocationStatus
{
    Free = 0,      // 空闲
    Occupied = 1,  // 占用
    Disabled = 2   // 禁用
}

public enum StorageStatus
{
    InStock = 0,   // 在库
    Borrowed = 1,  // 借出
    Scrapped = 2,  // 报废
    Repairing = 3  // 维修中
}

public enum BorrowType
{
    Production = 0,   // 生产领用
    External = 1,     // 外借
    Transfer = 2      // 调拨
}

public enum BorrowStatus
{
    Borrowing = 0,    // 借用中
    Returned = 1,     // 已归还
    Overdue = 2       // 逾期
}

public enum ScrapAuditStatus
{
    Pending = 0,      // 待审核
    Approved = 1,     // 已通过
    Rejected = 2      // 已驳回
}

// 扩展方法
public static class WarehouseEnumExtensions
{
    public static string GetDisplayName(this LocationStatus status)
    {
        return status switch
        {
            LocationStatus.Free => "空闲",
            LocationStatus.Occupied => "占用",
            LocationStatus.Disabled => "禁用",
            _ => "未知"
        };
    }

    public static string GetDisplayName(this StorageStatus status)
    {
        return status switch
        {
            StorageStatus.InStock => "在库",
            StorageStatus.Borrowed => "借出",
            StorageStatus.Scrapped => "报废",
            StorageStatus.Repairing => "维修中",
            _ => "未知"
        };
    }

    public static string GetDisplayName(this BorrowType type)
    {
        return type switch
        {
            BorrowType.Production => "生产领用",
            BorrowType.External => "外借",
            BorrowType.Transfer => "调拨",
            _ => "未知"
        };
    }

    public static string GetDisplayName(this BorrowStatus status)
    {
        return status switch
        {
            BorrowStatus.Borrowing => "借用中",
            BorrowStatus.Returned => "已归还",
            BorrowStatus.Overdue => "逾期",
            _ => "未知"
        };
    }

    public static string GetDisplayName(this ScrapAuditStatus status)
    {
        return status switch
        {
            ScrapAuditStatus.Pending => "待审核",
            ScrapAuditStatus.Approved => "已通过",
            ScrapAuditStatus.Rejected => "已驳回",
            _ => "未知"
        };
    }
}
