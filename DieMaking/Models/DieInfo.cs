namespace DieMaking.Models;

/// <summary>
/// 刀模信息模型类
/// </summary>
public class DieInfo
{
    /// <summary>
    /// 刀模ID
    /// </summary>
    public int DieID { get; set; }

    /// <summary>
    /// 刀模编号
    /// </summary>
    public string DieCode { get; set; } = string.Empty;

    /// <summary>
    /// 工单号
    /// </summary>
    public string WorkOrderNo { get; set; } = string.Empty;

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
    /// 模型类型
    /// </summary>
    public string ModelType { get; set; } = string.Empty;

    /// <summary>
    /// 排版类型
    /// </summary>
    public string LayoutType { get; set; } = string.Empty;

    /// <summary>
    /// 瓦楞类型
    /// </summary>
    public string FluteType { get; set; } = string.Empty;

    /// <summary>
    /// 材料
    /// </summary>
    public string Material { get; set; } = string.Empty;

    /// <summary>
    /// 制作长度(mm)
    /// </summary>
    public decimal? ManufactureLength { get; set; }

    /// <summary>
    /// 制作宽度(mm)
    /// </summary>
    public decimal? ManufactureWidth { get; set; }

    /// <summary>
    /// 制作高度(mm)
    /// </summary>
    public decimal? ManufactureHeight { get; set; }

    /// <summary>
    /// 毛坯长度(mm) - 板长
    /// </summary>
    public decimal? BlankLength { get; set; }

    /// <summary>
    /// 毛坯宽度(mm) - 板宽
    /// </summary>
    public decimal? BlankWidth { get; set; }

    /// <summary>
    /// 刀长(m)
    /// </summary>
    public decimal? KnifeLengthM { get; set; }

    /// <summary>
    /// 刀痕长(m)
    /// </summary>
    public decimal? KnifeMarkLengthM { get; set; }

    /// <summary>
    /// 板费单价(默认90元/平方米)
    /// </summary>
    public decimal BoardFeeUnitPrice { get; set; } = 90m;

    /// <summary>
    /// 板费
    /// </summary>
    public decimal? BoardFee { get; set; }

    /// <summary>
    /// 制作单价(默认8元/平方米)
    /// </summary>
    public decimal ProductionUnitPrice { get; set; } = 8m;

    /// <summary>
    /// 制作费
    /// </summary>
    public decimal? ProductionFee { get; set; }

    /// <summary>
    /// 设计单价(默认70元/平方米)
    /// </summary>
    public decimal DesignUnitPrice { get; set; } = 70m;

    /// <summary>
    /// 设计费
    /// </summary>
    public decimal? DesignFee { get; set; }

    /// <summary>
    /// 工艺描述
    /// </summary>
    public string ProcessDesc { get; set; } = string.Empty;

    /// <summary>
    /// 所需工序
    /// </summary>
    public string RequiredProcesses { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    public DieStatus Status { get; set; } = DieStatus.Pending;

    /// <summary>
    /// 审核状态
    /// </summary>
    public AuditStatus AuditStatus { get; set; } = AuditStatus.Unaudited;

    /// <summary>
    /// 来源工厂
    /// </summary>
    public string SourceFactory { get; set; } = string.Empty;

    /// <summary>
    /// 外部订单ID
    /// </summary>
    public int? ExternalOrderID { get; set; }

    /// <summary>
    /// 外部订单号
    /// </summary>
    public string ExternalOrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 交货日期
    /// </summary>
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>
    /// 创建用户
    /// </summary>
    public string CreateUser { get; set; } = string.Empty;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get; set; } = string.Empty;

    /// <summary>
    /// 制作尺寸（格式化显示）
    /// </summary>
    public string ManufactureSize =>
        ManufactureLength.HasValue && ManufactureWidth.HasValue && ManufactureHeight.HasValue
            ? $"{ManufactureLength}*{ManufactureWidth}*{ManufactureHeight}"
            : string.Empty;

    /// <summary>
    /// 毛坯尺寸（格式化显示）
    /// </summary>
    public string BlankSize =>
        BlankLength.HasValue && BlankWidth.HasValue
            ? $"{BlankLength}*{BlankWidth}"
            : string.Empty;

    /// <summary>
    /// 状态文本
    /// </summary>
    public string StatusText => Status.GetDisplayName();

    /// <summary>
    /// 审核状态文本
    /// </summary>
    public string AuditStatusText => AuditStatus.GetDisplayName();

    /// <summary>
    /// 计算面积（平方米）
    /// </summary>
    public decimal? CalculateArea()
    {
        if (BlankLength.HasValue && BlankWidth.HasValue)
        {
            // 将mm转换为m，然后计算面积
            var lengthInMeters = BlankLength.Value / 1000m;
            var widthInMeters = BlankWidth.Value / 1000m;
            return lengthInMeters * widthInMeters;
        }
        return null;
    }

    /// <summary>
    /// 计算费用
    /// </summary>
    public void CalculateFees()
    {
        var area = CalculateArea();
        if (area.HasValue)
        {
            BoardFee = area.Value * BoardFeeUnitPrice;
            ProductionFee = area.Value * ProductionUnitPrice;
            DesignFee = area.Value * DesignUnitPrice;
        }
    }

    /// <summary>
    /// 工序列表
    /// </summary>
    public List<DieProcess> Processes { get; set; } = new();
}

/// <summary>
/// 刀模工序模型类
/// </summary>
public class DieProcess
{
    /// <summary>
    /// 工序ID
    /// </summary>
    public int ProcessID { get; set; }

    /// <summary>
    /// 刀模ID
    /// </summary>
    public int DieID { get; set; }

    /// <summary>
    /// 工序名称
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    public ProcessStatus Status { get; set; } = ProcessStatus.Pending;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompleteTime { get; set; }

    /// <summary>
    /// 操作员工号
    /// </summary>
    public string OperatorNo { get; set; } = string.Empty;

    /// <summary>
    /// 操作员姓名
    /// </summary>
    public string OperatorName { get; set; } = string.Empty;

    /// <summary>
    /// 板长
    /// </summary>
    public decimal? BoardLength { get; set; }

    /// <summary>
    /// 板宽
    /// </summary>
    public decimal? BoardWidth { get; set; }

    /// <summary>
    /// 刀长
    /// </summary>
    public decimal? KnifeLength { get; set; }

    /// <summary>
    /// 刀痕长
    /// </summary>
    public decimal? KnifeTraceLength { get; set; }

    /// <summary>
    /// 计算公式
    /// </summary>
    public string Formula { get; set; } = string.Empty;

    /// <summary>
    /// 金额
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// 前道工序ID
    /// </summary>
    public int? PrevProcessID { get; set; }

    /// <summary>
    /// 前道工序是否完成
    /// </summary>
    public bool IsPrevCompleted { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>
    /// 状态文本
    /// </summary>
    public string StatusText => Status.GetDisplayName();
}

/// <summary>
/// 刀模信息（用于报表）
/// </summary>
public class DieInfoForReport
{
    public int DieID { get; set; }
    public string DieCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
}

/// <summary>
/// 生产看板数据项
/// </summary>
public class DieBoardItem
{
    public int DieID { get; set; }
    public string DieCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DateTime? DeliveryDate { get; set; }
    public DieStatus Status { get; set; }
    public DateTime? CreateTime { get; set; }
    public int TotalProcesses { get; set; }
    public int CompletedProcesses { get; set; }
}

/// <summary>
/// 生产看板数据
/// </summary>
public class ProductionBoardData
{
    public List<DieBoardItem> PendingList { get; set; } = new();
    public List<DieBoardItem> InProgressList { get; set; } = new();
    public List<DieBoardItem> CompletedList { get; set; } = new();
    public ProductionStatistics Statistics { get; set; } = new();
}

/// <summary>
/// 生产统计信息
/// </summary>
public class ProductionStatistics
{
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
}

/// <summary>
/// 完工记录
/// </summary>
public class CompletionRecord
{
    public int CompletionID { get; set; }
    public int DieID { get; set; }
    public string DieCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DateTime? CompleteTime { get; set; }
    public decimal? TotalAmount { get; set; }
    public string OperatorNo { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
}

/// <summary>
/// 按刀模统计的完工数据
/// </summary>
public class CompletionStatsByDie
{
    public int DieID { get; set; }
    public string DieCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string RequiredProcesses { get; set; } = string.Empty;
    public DateTime? CompleteTime { get; set; }
    public decimal? TotalAmount { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
}

/// <summary>
/// 工序统计
/// </summary>
public class ProcessStats
{
    public string ProcessName { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int CompletedCount { get; set; }
    public int InProgressCount { get; set; }
    public int PendingCount { get; set; }
    public double CompletionRate { get; set; }
    public double AvgDurationMinutes { get; set; }
    public decimal? TotalAmount { get; set; }
}

/// <summary>
/// 工序报表数据
/// </summary>
public class DieProcessForReport
{
    public int ProcessID { get; set; }
    public int DieID { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public ProcessStatus Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
}
