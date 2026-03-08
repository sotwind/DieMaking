namespace DieMaking.Models;

public class DieInfo
{
    public int DieID { get; set; }
    public string DieCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Structure { get; set; } = string.Empty;
    public string ModelType { get; set; } = string.Empty;
    public string LayoutType { get; set; } = string.Empty;
    public string FluteType { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;

    // 尺寸信息
    public decimal ManufactureLength { get; set; }
    public decimal ManufactureWidth { get; set; }
    public decimal ManufactureHeight { get; set; }
    public decimal BlankLength { get; set; }
    public decimal BlankWidth { get; set; }

    // 工艺信息
    public string ProcessDesc { get; set; } = string.Empty;
    public string RequiredProcesses { get; set; } = string.Empty;

    // 状态
    public DieStatus Status { get; set; } = DieStatus.Pending;
    public AuditStatus AuditStatus { get; set; } = AuditStatus.Unaudited;

    // 关联信息
    public string SourceFactory { get; set; } = string.Empty;
    public int? ExternalOrderID { get; set; }

    // 时间
    public DateTime? DeliveryDate { get; set; }
    public DateTime CreateTime { get; set; }
    public string CreateUser { get; set; } = string.Empty;
    public DateTime? UpdateTime { get; set; }
    public string Remark { get; set; } = string.Empty;

    // 辅助属性
    public string ManufactureSize => $"{ManufactureLength}*{ManufactureWidth}*{ManufactureHeight}";
    public string BlankSize => $"{BlankLength}*{BlankWidth}";
    public string StatusText => Status.GetDisplayName();
    public string AuditStatusText => AuditStatus.GetDisplayName();
}

public class DieProcess
{
    public int ProcessID { get; set; }
    public int DieID { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public ProcessStatus Status { get; set; } = ProcessStatus.Pending;
    public DateTime? StartTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public string OperatorNo { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;

    // 绘图工序特有字段
    public int? BoardLength { get; set; }
    public int? BoardWidth { get; set; }
    public int? KnifeLength { get; set; }
    public int? KnifeTraceLength { get; set; }

    // 金额计算
    public string? Formula { get; set; }
    public decimal? Amount { get; set; }

    // 前道工序依赖
    public int? PrevProcessID { get; set; }
    public bool IsPrevCompleted { get; set; }

    public DateTime CreateTime { get; set; }

    // 辅助属性
    public string StatusText => Status.GetDisplayName();
}

public class DieCompletion
{
    public int CompletionID { get; set; }
    public int DieID { get; set; }
    public DateTime CompleteTime { get; set; }
    public decimal TotalAmount { get; set; }
    public string OperatorNo { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
}

// 枚举定义
public enum DieStatus
{
    Pending = 0,      // 待生产
    InProgress = 1,   // 生产中
    Completed = 2,    // 已完成
    OnHold = 3,       // 暂不生产
    NotRequired = 4   // 无需生产
}

public enum AuditStatus
{
    Unaudited = 0,    // 未审核
    Audited = 1       // 已审核
}

public enum ProcessStatus
{
    Pending = 0,      // 待生产
    InProgress = 1,   // 生产中
    Completed = 2     // 已完成
}

// 扩展方法
public static class DieEnumExtensions
{
    public static string GetDisplayName(this DieStatus status)
    {
        return status switch
        {
            DieStatus.Pending => "待生产",
            DieStatus.InProgress => "生产中",
            DieStatus.Completed => "已完成",
            DieStatus.OnHold => "暂不生产",
            DieStatus.NotRequired => "无需生产",
            _ => "未知"
        };
    }

    public static string GetDisplayName(this AuditStatus status)
    {
        return status switch
        {
            AuditStatus.Unaudited => "未审核",
            AuditStatus.Audited => "已审核",
            _ => "未知"
        };
    }

    public static string GetDisplayName(this ProcessStatus status)
    {
        return status switch
        {
            ProcessStatus.Pending => "待生产",
            ProcessStatus.InProgress => "生产中",
            ProcessStatus.Completed => "已完成",
            _ => "未知"
        };
    }
}
