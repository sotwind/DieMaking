namespace DieMaking.Models;

/// <summary>
/// 库位状态
/// </summary>
public enum LocationStatus
{
    /// <summary>
    /// 空闲
    /// </summary>
    Free = 0,

    /// <summary>
    /// 占用
    /// </summary>
    Occupied = 1,

    /// <summary>
    /// 禁用
    /// </summary>
    Disabled = 2
}

/// <summary>
/// 存储状态
/// </summary>
public enum StorageStatus
{
    /// <summary>
    /// 在库
    /// </summary>
    InStock = 0,

    /// <summary>
    /// 借出
    /// </summary>
    Borrowed = 1,

    /// <summary>
    /// 报废
    /// </summary>
    Scrapped = 2,

    /// <summary>
    /// 维修中
    /// </summary>
    Repairing = 3
}

/// <summary>
/// 借用类型
/// </summary>
public enum BorrowType
{
    /// <summary>
    /// 内部领用
    /// </summary>
    Internal = 0,

    /// <summary>
    /// 生产领用
    /// </summary>
    Production = 1,

    /// <summary>
    /// 外借
    /// </summary>
    External = 2,

    /// <summary>
    /// 调拨
    /// </summary>
    Transfer = 3
}

/// <summary>
/// 借用状态
/// </summary>
public enum BorrowStatus
{
    /// <summary>
    /// 借用中
    /// </summary>
    Borrowing = 0,

    /// <summary>
    /// 已归还
    /// </summary>
    Returned = 1,

    /// <summary>
    /// 逾期
    /// </summary>
    Overdue = 2
}

/// <summary>
/// 库位信息模型
/// </summary>
public class StorageLocation
{
    /// <summary>
    /// 库位ID
    /// </summary>
    public int LocationID { get; set; }

    /// <summary>
    /// 库位编号
    /// </summary>
    public string LocationCode { get; set; } = string.Empty;

    /// <summary>
    /// 区域
    /// </summary>
    public string Area { get; set; } = string.Empty;

    /// <summary>
    /// 货架号
    /// </summary>
    public string ShelfNo { get; set; } = string.Empty;

    /// <summary>
    /// 层号
    /// </summary>
    public string LayerNo { get; set; } = string.Empty;

    /// <summary>
    /// 位置号
    /// </summary>
    public string PositionNo { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    public LocationStatus Status { get; set; } = LocationStatus.Free;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>
    /// 状态文本
    /// </summary>
    public string StatusText => Status switch
    {
        LocationStatus.Free => "空闲",
        LocationStatus.Occupied => "占用",
        LocationStatus.Disabled => "禁用",
        _ => "未知"
    };
}

/// <summary>
/// 刀模库存模型
/// </summary>
public class DieInventory
{
    /// <summary>
    /// 库存ID
    /// </summary>
    public int InventoryID { get; set; }

    /// <summary>
    /// 刀模ID
    /// </summary>
    public int DieID { get; set; }

    /// <summary>
    /// 库位ID
    /// </summary>
    public int? LocationID { get; set; }

    /// <summary>
    /// 存储状态
    /// </summary>
    public StorageStatus StorageStatus { get; set; } = StorageStatus.InStock;

    /// <summary>
    /// 入库时间
    /// </summary>
    public DateTime? InStockTime { get; set; }

    /// <summary>
    /// 最后借出时间
    /// </summary>
    public DateTime? LastBorrowTime { get; set; }

    /// <summary>
    /// 最后归还时间
    /// </summary>
    public DateTime? LastReturnTime { get; set; }

    /// <summary>
    /// 借用次数
    /// </summary>
    public int TotalBorrowCount { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get; set; } = string.Empty;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }

    // 关联信息
    public string? LocationCode { get; set; }
    public string? DieCode { get; set; }
    public string? CustomerName { get; set; }
    public string? ProductName { get; set; }
}

/// <summary>
/// 刀模借用记录模型
/// </summary>
public class DieBorrowRecord
{
    /// <summary>
    /// 借用记录ID
    /// </summary>
    public int BorrowID { get; set; }

    /// <summary>
    /// 刀模ID
    /// </summary>
    public int DieID { get; set; }

    /// <summary>
    /// 库存ID
    /// </summary>
    public int InventoryID { get; set; }

    /// <summary>
    /// 借用类型
    /// </summary>
    public BorrowType BorrowType { get; set; } = BorrowType.Internal;

    /// <summary>
    /// 借用人编号
    /// </summary>
    public string BorrowerNo { get; set; } = string.Empty;

    /// <summary>
    /// 借用人姓名
    /// </summary>
    public string BorrowerName { get; set; } = string.Empty;

    /// <summary>
    /// 借用部门
    /// </summary>
    public string BorrowDept { get; set; } = string.Empty;

    /// <summary>
    /// 借用时间
    /// </summary>
    public DateTime? BorrowTime { get; set; }

    /// <summary>
    /// 预计归还时间
    /// </summary>
    public DateTime? ExpectedReturnTime { get; set; }

    /// <summary>
    /// 实际归还时间
    /// </summary>
    public DateTime? ActualReturnTime { get; set; }

    /// <summary>
    /// 用途
    /// </summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    public BorrowStatus Status { get; set; } = BorrowStatus.Borrowing;

    /// <summary>
    /// 归还操作员工号
    /// </summary>
    public string ReturnOperatorNo { get; set; } = string.Empty;

    /// <summary>
    /// 归还操作员姓名
    /// </summary>
    public string ReturnOperatorName { get; set; } = string.Empty;

    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }

    // 关联信息
    public string? DieCode { get; set; }
    public string? CustomerName { get; set; }
    public string? ProductName { get; set; }

    /// <summary>
    /// 借用类型文本
    /// </summary>
    public string BorrowTypeText => BorrowType switch
    {
        BorrowType.Internal => "内部领用",
        BorrowType.Production => "生产领用",
        BorrowType.External => "外借",
        BorrowType.Transfer => "调拨",
        _ => "未知"
    };

    /// <summary>
    /// 状态文本
    /// </summary>
    public string StatusText => Status switch
    {
        BorrowStatus.Borrowing => "借用中",
        BorrowStatus.Returned => "已归还",
        BorrowStatus.Overdue => "逾期",
        _ => "未知"
    };
}
