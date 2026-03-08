namespace DieMaking.Models;

/// <summary>
/// 刀模信息实体类
/// </summary>
public class DieInfo
{
    /// <summary>刀模ID</summary>
    public int DieID { get; set; }
    
    /// <summary>刀模编号</summary>
    public string DieCode { get; set; } = string.Empty;
    
    /// <summary>客户名称</summary>
    public string CustomerName { get; set; } = string.Empty;
    
    /// <summary>产品名称</summary>
    public string ProductName { get; set; } = string.Empty;
    
    /// <summary>结构类型</summary>
    public string Structure { get; set; } = string.Empty;
    
    /// <summary>模型类型</summary>
    public string ModelType { get; set; } = string.Empty;
    
    /// <summary>排版方式</summary>
    public string LayoutType { get; set; } = string.Empty;
    
    /// <summary>瓦楞类型</summary>
    public string FluteType { get; set; } = string.Empty;
    
    /// <summary>材料</summary>
    public string Material { get; set; } = string.Empty;

    // 尺寸信息
    /// <summary>制造长度</summary>
    public decimal ManufactureLength { get; set; }
    
    /// <summary>制造宽度</summary>
    public decimal ManufactureWidth { get; set; }
    
    /// <summary>制造高度</summary>
    public decimal ManufactureHeight { get; set; }
    
    /// <summary>下料长度</summary>
    public decimal BlankLength { get; set; }
    
    /// <summary>下料宽度</summary>
    public decimal BlankWidth { get; set; }

    // 工艺信息
    /// <summary>工艺描述</summary>
    public string ProcessDesc { get; set; } = string.Empty;
    
    /// <summary>所需工序</summary>
    public string RequiredProcesses { get; set; } = string.Empty;

    // 状态
    /// <summary>刀模状态</summary>
    public DieStatus Status { get; set; } = DieStatus.Pending;
    
    /// <summary>审核状态</summary>
    public AuditStatus AuditStatus { get; set; } = AuditStatus.Unaudited;

    // 关联信息
    /// <summary>来源工厂</summary>
    public string SourceFactory { get; set; } = string.Empty;
    
    /// <summary>外部订单ID</summary>
    public int? ExternalOrderID { get; set; }

    // 时间
    /// <summary>交货日期</summary>
    public DateTime? DeliveryDate { get; set; }
    
    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>创建用户</summary>
    public string CreateUser { get; set; } = string.Empty;
    
    /// <summary>更新时间</summary>
    public DateTime? UpdateTime { get; set; }
    
    /// <summary>备注</summary>
    public string Remark { get; set; } = string.Empty;

    // 辅助属性
    /// <summary>制造尺寸文本（长*宽*高）</summary>
    public string ManufactureSize => $"{ManufactureLength}*{ManufactureWidth}*{ManufactureHeight}";
    
    /// <summary>下料尺寸文本（长*宽）</summary>
    public string BlankSize => $"{BlankLength}*{BlankWidth}";
    
    /// <summary>状态显示文本</summary>
    public string StatusText => Status.GetDisplayName();
    
    /// <summary>审核状态显示文本</summary>
    public string AuditStatusText => AuditStatus.GetDisplayName();
}

/// <summary>
/// 刀模工序实体类
/// </summary>
public class DieProcess
{
    /// <summary>工序ID</summary>
    public int ProcessID { get; set; }
    
    /// <summary>刀模ID</summary>
    public int DieID { get; set; }
    
    /// <summary>工序名称</summary>
    public string ProcessName { get; set; } = string.Empty;
    
    /// <summary>工序状态</summary>
    public ProcessStatus Status { get; set; } = ProcessStatus.Pending;
    
    /// <summary>开始时间</summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>完成时间</summary>
    public DateTime? CompleteTime { get; set; }
    
    /// <summary>操作员工号</summary>
    public string OperatorNo { get; set; } = string.Empty;
    
    /// <summary>操作员姓名</summary>
    public string OperatorName { get; set; } = string.Empty;

    // 绘图工序特有字段
    /// <summary>板长</summary>
    public int? BoardLength { get; set; }
    
    /// <summary>板宽</summary>
    public int? BoardWidth { get; set; }
    
    /// <summary>刀长</summary>
    public int? KnifeLength { get; set; }
    
    /// <summary>刀痕长度</summary>
    public int? KnifeTraceLength { get; set; }

    // 金额计算
    /// <summary>计算公式</summary>
    public string? Formula { get; set; }
    
    /// <summary>金额</summary>
    public decimal? Amount { get; set; }

    // 前道工序依赖
    /// <summary>前道工序ID</summary>
    public int? PrevProcessID { get; set; }
    
    /// <summary>前道工序是否完成</summary>
    public bool IsPrevCompleted { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    // 辅助属性
    /// <summary>状态显示文本</summary>
    public string StatusText => Status.GetDisplayName();
}

/// <summary>
/// 刀模完工记录实体类
/// </summary>
public class DieCompletion
{
    /// <summary>完工ID</summary>
    public int CompletionID { get; set; }
    
    /// <summary>刀模ID</summary>
    public int DieID { get; set; }
    
    /// <summary>完工时间</summary>
    public DateTime CompleteTime { get; set; }
    
    /// <summary>总金额</summary>
    public decimal TotalAmount { get; set; }
    
    /// <summary>操作员工号</summary>
    public string OperatorNo { get; set; } = string.Empty;
    
    /// <summary>操作员姓名</summary>
    public string OperatorName { get; set; } = string.Empty;
    
    /// <summary>备注</summary>
    public string Remark { get; set; } = string.Empty;
}

// 枚举定义

/// <summary>
/// 刀模状态枚举
/// </summary>
public enum DieStatus
{
    /// <summary>待生产</summary>
    Pending = 0,
    /// <summary>生产中</summary>
    InProgress = 1,
    /// <summary>已完成</summary>
    Completed = 2,
    /// <summary>暂不生产</summary>
    OnHold = 3,
    /// <summary>无需生产</summary>
    NotRequired = 4
}

/// <summary>
/// 审核状态枚举
/// </summary>
public enum AuditStatus
{
    /// <summary>未审核</summary>
    Unaudited = 0,
    /// <summary>已审核</summary>
    Audited = 1
}

/// <summary>
/// 工序状态枚举
/// </summary>
public enum ProcessStatus
{
    /// <summary>待生产</summary>
    Pending = 0,
    /// <summary>生产中</summary>
    InProgress = 1,
    /// <summary>已完成</summary>
    Completed = 2
}

// 扩展方法

/// <summary>
/// 刀模相关枚举扩展方法
/// </summary>
public static class DieEnumExtensions
{
    /// <summary>
    /// 获取刀模状态的显示名称
    /// </summary>
    /// <param name="status">刀模状态</param>
    /// <returns>显示名称</returns>
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

    /// <summary>
    /// 获取审核状态的显示名称
    /// </summary>
    /// <param name="status">审核状态</param>
    /// <returns>显示名称</returns>
    public static string GetDisplayName(this AuditStatus status)
    {
        return status switch
        {
            AuditStatus.Unaudited => "未审核",
            AuditStatus.Audited => "已审核",
            _ => "未知"
        };
    }

    /// <summary>
    /// 获取工序状态的显示名称
    /// </summary>
    /// <param name="status">工序状态</param>
    /// <returns>显示名称</returns>
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
