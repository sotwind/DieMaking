namespace DieMaking.Models;

/// <summary>
/// 改刀记录实体
/// </summary>
public class DieModificationRecord
{
    /// <summary>
    /// 改刀记录ID
    /// </summary>
    public int ModificationID { get; set; }

    /// <summary>
    /// 刀模ID
    /// </summary>
    public int DieID { get; set; }

    /// <summary>
    /// 刀模编号
    /// </summary>
    public string DieCode { get; set; } = string.Empty;

    /// <summary>
    /// 客户名称
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// 产品名称
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// 改刀金额
    /// </summary>
    public decimal ModificationAmount { get; set; }

    /// <summary>
    /// 改刀时间
    /// </summary>
    public DateTime ModificationTime { get; set; }

    /// <summary>
    /// 改刀人
    /// </summary>
    public string ModifiedBy { get; set; } = string.Empty;

    /// <summary>
    /// 改刀原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }
}
