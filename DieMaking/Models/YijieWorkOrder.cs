namespace DieMaking.Models;

/// <summary>
/// 易捷工单信息
/// </summary>
public class YijieWorkOrder
{
    /// <summary>
    /// 工单号
    /// </summary>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 客户名称
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// 产品名称
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// 结构
    /// </summary>
    public string Structure { get; set; } = string.Empty;

    /// <summary>
    /// 材质
    /// </summary>
    public string Material { get; set; } = string.Empty;

    /// <summary>
    /// 制造长度(mm)
    /// </summary>
    public decimal ManufactureLength { get; set; }

    /// <summary>
    /// 制造宽度(mm)
    /// </summary>
    public decimal ManufactureWidth { get; set; }

    /// <summary>
    /// 制造高度(mm)
    /// </summary>
    public decimal ManufactureHeight { get; set; }

    /// <summary>
    /// 下料长度(mm)
    /// </summary>
    public decimal BlankLength { get; set; }

    /// <summary>
    /// 下料宽度(mm)
    /// </summary>
    public decimal BlankWidth { get; set; }

    /// <summary>
    /// 工艺描述
    /// </summary>
    public string ProcessDesc { get; set; } = string.Empty;

    /// <summary>
    /// 交货日期
    /// </summary>
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// 是否为新刀
    /// </summary>
    public bool IsNewDie { get; set; }

    /// <summary>
    /// 工单创建时间
    /// </summary>
    public DateTime OrderCreateTime { get; set; }

    /// <summary>
    /// 工厂名称
    /// </summary>
    public string FactoryName { get; set; } = string.Empty;
}
