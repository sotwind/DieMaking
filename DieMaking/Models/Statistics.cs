namespace DieMaking.Models;

/// <summary>
/// 库存汇总统计
/// </summary>
public class InventorySummaryStats
{
    /// <summary>
    /// 总数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 在库数
    /// </summary>
    public int InStockCount { get; set; }

    /// <summary>
    /// 借出数
    /// </summary>
    public int BorrowedCount { get; set; }

    /// <summary>
    /// 已报废数
    /// </summary>
    public int ScrappedCount { get; set; }

    /// <summary>
    /// 维修中数
    /// </summary>
    public int RepairingCount { get; set; }
}

/// <summary>
/// 借用统计
/// </summary>
public class BorrowStats
{
    /// <summary>
    /// 总借用次数
    /// </summary>
    public int TotalBorrowCount { get; set; }

    /// <summary>
    /// 借用中次数
    /// </summary>
    public int BorrowingCount { get; set; }

    /// <summary>
    /// 已归还次数
    /// </summary>
    public int ReturnedCount { get; set; }

    /// <summary>
    /// 逾期次数
    /// </summary>
    public int OverdueCount { get; set; }

    /// <summary>
    /// 本月借用次数
    /// </summary>
    public int MonthBorrowCount { get; set; }

    /// <summary>
    /// 本月归还次数
    /// </summary>
    public int MonthReturnCount { get; set; }
}
