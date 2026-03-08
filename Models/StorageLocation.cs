namespace DieMaking.Models;

public class StorageLocation
{
    public int LocationID { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string ShelfNo { get; set; } = string.Empty;
    public string LayerNo { get; set; } = string.Empty;
    public string PositionNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public LocationStatus Status { get; set; } = LocationStatus.Free;
    public DateTime CreateTime { get; set; }

    public string StatusText => Status.GetDisplayName();
}

public class DieInventory
{
    public int InventoryID { get; set; }
    public int DieID { get; set; }
    public int? LocationID { get; set; }
    public StorageStatus StorageStatus { get; set; } = StorageStatus.InStock;
    public DateTime? InStockTime { get; set; }
    public DateTime? LastBorrowTime { get; set; }
    public DateTime? LastReturnTime { get; set; }
    public int TotalBorrowCount { get; set; }
    public string Remark { get; set; } = string.Empty;
    public DateTime UpdateTime { get; set; }

    // 关联信息
    public string? LocationCode { get; set; }
    public string? DieCode { get; set; }
    public string? CustomerName { get; set; }
    public string? ProductName { get; set; }

    public string StorageStatusText => StorageStatus.GetDisplayName();
}

public class DieBorrowRecord
{
    public int BorrowID { get; set; }
    public int DieID { get; set; }
    public int InventoryID { get; set; }
    public BorrowType BorrowType { get; set; } = BorrowType.Production;
    public string BorrowerNo { get; set; } = string.Empty;
    public string BorrowerName { get; set; } = string.Empty;
    public string BorrowDept { get; set; } = string.Empty;
    public DateTime BorrowTime { get; set; }
    public DateTime? ExpectedReturnTime { get; set; }
    public DateTime? ActualReturnTime { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public BorrowStatus Status { get; set; } = BorrowStatus.Borrowing;
    public string ReturnOperatorNo { get; set; } = string.Empty;
    public string ReturnOperatorName { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }

    // 关联信息
    public string? DieCode { get; set; }
    public string? CustomerName { get; set; }
    public string? ProductName { get; set; }

    public string BorrowTypeText => BorrowType.GetDisplayName();
    public string StatusText => Status.GetDisplayName();
}

public class DieScrapRecord
{
    public int ScrapID { get; set; }
    public int DieID { get; set; }
    public int InventoryID { get; set; }
    public string ScrapReason { get; set; } = string.Empty;
    public string ScrapType { get; set; } = string.Empty;
    public string ApplicantNo { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public DateTime ApplyTime { get; set; }
    public string? AuditorNo { get; set; }
    public string? AuditorName { get; set; }
    public DateTime? AuditTime { get; set; }
    public ScrapAuditStatus AuditStatus { get; set; } = ScrapAuditStatus.Pending;
    public string? AuditRemark { get; set; }
    public DateTime? ScrapTime { get; set; }
    public DateTime CreateTime { get; set; }

    // 关联信息
    public string? DieCode { get; set; }
    public string? CustomerName { get; set; }
    public string? ProductName { get; set; }

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
