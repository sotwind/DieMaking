namespace DieMaking.Models;

/// <summary>
/// 刀模状态
/// </summary>
public enum DieStatus
{
    /// <summary>
    /// 待生产
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 生产中
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 2,

    /// <summary>
    /// 暂不生产
    /// </summary>
    OnHold = 3,

    /// <summary>
    /// 无需生产
    /// </summary>
    NotRequired = 4
}

/// <summary>
/// 审核状态
/// </summary>
public enum AuditStatus
{
    /// <summary>
    /// 未审核
    /// </summary>
    Unaudited = 0,

    /// <summary>
    /// 已审核
    /// </summary>
    Audited = 1
}

/// <summary>
/// 工序状态
/// </summary>
public enum ProcessStatus
{
    /// <summary>
    /// 待生产
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 生产中
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 2
}

/// <summary>
/// 工序类型枚举
/// </summary>
public enum ProcessType
{
    /// <summary>
    /// 绘图
    /// </summary>
    Drawing = 0,

    /// <summary>
    /// 割板
    /// </summary>
    BoardCutting = 1,

    /// <summary>
    /// 弯刀
    /// </summary>
    KnifeBending = 2,

    /// <summary>
    /// 装刀
    /// </summary>
    KnifeInstalling = 3,

    /// <summary>
    /// 贴泡沫
    /// </summary>
    FoamSticking = 4
}

/// <summary>
/// 枚举扩展方法
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// 获取刀模状态显示名称
    /// </summary>
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
    /// 获取审核状态显示名称
    /// </summary>
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
    /// 获取工序状态显示名称
    /// </summary>
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

    /// <summary>
    /// 获取工序类型显示名称
    /// </summary>
    public static string GetDisplayName(this ProcessType processType)
    {
        return processType switch
        {
            ProcessType.Drawing => "绘图",
            ProcessType.BoardCutting => "割板",
            ProcessType.KnifeBending => "弯刀",
            ProcessType.KnifeInstalling => "装刀",
            ProcessType.FoamSticking => "贴泡沫",
            _ => "未知"
        };
    }

    /// <summary>
    /// 获取所有工序类型列表
    /// </summary>
    public static List<(ProcessType Type, string Name)> GetAllProcessTypes()
    {
        return new List<(ProcessType, string)>
        {
            (ProcessType.Drawing, ProcessType.Drawing.GetDisplayName()),
            (ProcessType.BoardCutting, ProcessType.BoardCutting.GetDisplayName()),
            (ProcessType.KnifeBending, ProcessType.KnifeBending.GetDisplayName()),
            (ProcessType.KnifeInstalling, ProcessType.KnifeInstalling.GetDisplayName()),
            (ProcessType.FoamSticking, ProcessType.FoamSticking.GetDisplayName())
        };
    }
}
