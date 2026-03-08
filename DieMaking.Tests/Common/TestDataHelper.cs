using DieMaking.Models;

namespace DieMaking.Tests.Common;

/// <summary>
/// 测试数据生成器 - 提供标准化的测试数据
/// </summary>
public static class TestDataHelper
{
    #region 用户测试数据

    /// <summary>
    /// 创建测试用户
    /// </summary>
    public static User CreateUser(
        int userId = 1,
        string username = "testuser",
        string password = "password123",
        string realName = "测试用户",
        string permissions = "刀模管理,生产管理",
        string workstation = "A01",
        bool isActive = true)
    {
        return new User
        {
            UserID = userId,
            Username = username,
            Password = password,
            RealName = realName,
            Permissions = permissions,
            Workstation = workstation,
            IsActive = isActive,
            CreateTime = DateTime.Now.AddDays(-30),
            LastLoginTime = DateTime.Now.AddDays(-1)
        };
    }

    /// <summary>
    /// 创建管理员用户
    /// </summary>
    public static User CreateAdminUser(int userId = 1)
    {
        return CreateUser(
            userId: userId,
            username: "admin",
            password: "admin123",
            realName: "系统管理员",
            permissions: "系统管理员,用户管理,刀模管理,生产管理,仓库管理,报表统计",
            workstation: "ADMIN");
    }

    /// <summary>
    /// 创建普通用户
    /// </summary>
    public static User CreateNormalUser(int userId = 2)
    {
        return CreateUser(
            userId: userId,
            username: "operator",
            password: "operator123",
            realName: "操作员",
            permissions: "刀模管理,生产管理",
            workstation: "OP01");
    }

    /// <summary>
    /// 创建禁用用户
    /// </summary>
    public static User CreateInactiveUser(int userId = 3)
    {
        return CreateUser(
            userId: userId,
            username: "inactive",
            password: "inactive123",
            realName: "禁用用户",
            isActive: false);
    }

    #endregion

    #region 刀模测试数据

    /// <summary>
    /// 创建测试刀模信息
    /// </summary>
    public static DieInfo CreateDieInfo(
        int dieId = 1,
        string dieCode = "DM20240001",
        string customerName = "测试客户",
        string productName = "测试产品",
        DieStatus status = DieStatus.Pending,
        AuditStatus auditStatus = AuditStatus.Unaudited)
    {
        return new DieInfo
        {
            DieID = dieId,
            DieCode = dieCode,
            CustomerName = customerName,
            ProductName = productName,
            Structure = "结构A",
            ModelType = "模型B",
            LayoutType = "排版C",
            FluteType = "瓦楞D",
            Material = "钢材",
            ManufactureLength = 100.5m,
            ManufactureWidth = 80.0m,
            ManufactureHeight = 20.0m,
            BlankLength = 120.0m,
            BlankWidth = 100.0m,
            ProcessDesc = "测试工艺描述",
            RequiredProcesses = "工序1,工序2,工序3",
            Status = status,
            AuditStatus = auditStatus,
            SourceFactory = "本厂",
            ExternalOrderID = null,
            DeliveryDate = DateTime.Now.AddDays(7),
            CreateTime = DateTime.Now.AddDays(-5),
            CreateUser = "admin",
            UpdateTime = null,
            Remark = "测试备注"
        };
    }

    /// <summary>
    /// 创建待生产刀模
    /// </summary>
    public static DieInfo CreatePendingDie(int dieId = 1)
    {
        return CreateDieInfo(dieId: dieId, status: DieStatus.Pending);
    }

    /// <summary>
    /// 创建生产中刀模
    /// </summary>
    public static DieInfo CreateInProgressDie(int dieId = 2)
    {
        return CreateDieInfo(dieId: dieId, status: DieStatus.InProgress);
    }

    /// <summary>
    /// 创建已完成刀模
    /// </summary>
    public static DieInfo CreateCompletedDie(int dieId = 3)
    {
        return CreateDieInfo(dieId: dieId, status: DieStatus.Completed);
    }

    /// <summary>
    /// 创建已审核刀模
    /// </summary>
    public static DieInfo CreateAuditedDie(int dieId = 4)
    {
        return CreateDieInfo(dieId: dieId, auditStatus: AuditStatus.Audited);
    }

    #endregion

    #region 刀模工序测试数据

    /// <summary>
    /// 创建测试刀模工序
    /// </summary>
    public static DieProcess CreateDieProcess(
        int processId = 1,
        int dieId = 1,
        string processName = "测试工序",
        ProcessStatus status = ProcessStatus.Pending)
    {
        return new DieProcess
        {
            ProcessID = processId,
            DieID = dieId,
            ProcessName = processName,
            Status = status,
            StartTime = status == ProcessStatus.InProgress || status == ProcessStatus.Completed ? DateTime.Now.AddHours(-2) : null,
            CompleteTime = status == ProcessStatus.Completed ? DateTime.Now.AddHours(-1) : null,
            OperatorNo = "OP001",
            OperatorName = "操作员1",
            BoardLength = 100,
            BoardWidth = 80,
            KnifeLength = 50,
            KnifeTraceLength = 200,
            Formula = "L*W*0.1",
            Amount = 500.0m,
            PrevProcessID = null,
            IsPrevCompleted = true,
            CreateTime = DateTime.Now.AddDays(-3)
        };
    }

    /// <summary>
    /// 创建待生产工序列表
    /// </summary>
    public static List<DieProcess> CreatePendingProcesses(int dieId = 1)
    {
        return new List<DieProcess>
        {
            CreateDieProcess(processId: 1, dieId: dieId, processName: "绘图", status: ProcessStatus.Pending),
            CreateDieProcess(processId: 2, dieId: dieId, processName: "切割", status: ProcessStatus.Pending),
            CreateDieProcess(processId: 3, dieId: dieId, processName: "打磨", status: ProcessStatus.Pending)
        };
    }

    /// <summary>
    /// 创建生产中工序列表
    /// </summary>
    public static List<DieProcess> CreateInProgressProcesses(int dieId = 1)
    {
        return new List<DieProcess>
        {
            CreateDieProcess(processId: 1, dieId: dieId, processName: "绘图", status: ProcessStatus.Completed),
            CreateDieProcess(processId: 2, dieId: dieId, processName: "切割", status: ProcessStatus.InProgress),
            CreateDieProcess(processId: 3, dieId: dieId, processName: "打磨", status: ProcessStatus.Pending)
        };
    }

    #endregion

    #region 库存测试数据

    /// <summary>
    /// 创建测试库位
    /// </summary>
    public static StorageLocation CreateStorageLocation(
        int locationId = 1,
        string locationCode = "A-01-01-01",
        LocationStatus status = LocationStatus.Free)
    {
        return new StorageLocation
        {
            LocationID = locationId,
            LocationCode = locationCode,
            Area = "A区",
            ShelfNo = "01",
            LayerNo = "01",
            PositionNo = "01",
            Description = "测试库位",
            Status = status,
            CreateTime = DateTime.Now.AddDays(-30)
        };
    }

    /// <summary>
    /// 创建测试库存
    /// </summary>
    public static DieInventory CreateDieInventory(
        int inventoryId = 1,
        int dieId = 1,
        int? locationId = 1,
        StorageStatus status = StorageStatus.InStock)
    {
        return new DieInventory
        {
            InventoryID = inventoryId,
            DieID = dieId,
            LocationID = locationId,
            StorageStatus = status,
            InStockTime = DateTime.Now.AddDays(-10),
            LastBorrowTime = status == StorageStatus.Borrowed ? DateTime.Now.AddDays(-2) : null,
            LastReturnTime = status == StorageStatus.InStock ? DateTime.Now.AddDays(-1) : null,
            TotalBorrowCount = 3,
            Remark = "",
            UpdateTime = DateTime.Now,
            LocationCode = locationId.HasValue ? "A-01-01-01" : null,
            DieCode = "DM20240001",
            CustomerName = "测试客户",
            ProductName = "测试产品"
        };
    }

    /// <summary>
    /// 创建测试借用记录
    /// </summary>
    public static DieBorrowRecord CreateBorrowRecord(
        int borrowId = 1,
        int dieId = 1,
        int inventoryId = 1,
        BorrowStatus status = BorrowStatus.Borrowing)
    {
        return new DieBorrowRecord
        {
            BorrowID = borrowId,
            DieID = dieId,
            InventoryID = inventoryId,
            BorrowType = BorrowType.Production,
            BorrowerNo = "EMP001",
            BorrowerName = "借用人",
            BorrowDept = "生产部",
            BorrowTime = DateTime.Now.AddDays(-2),
            ExpectedReturnTime = DateTime.Now.AddDays(5),
            ActualReturnTime = status == BorrowStatus.Returned ? DateTime.Now.AddDays(-1) : null,
            Purpose = "生产使用",
            Status = status,
            ReturnOperatorNo = status == BorrowStatus.Returned ? "EMP002" : "",
            ReturnOperatorName = status == BorrowStatus.Returned ? "归还操作员" : "",
            Remark = "测试借用",
            CreateTime = DateTime.Now.AddDays(-2),
            DieCode = "DM20240001",
            CustomerName = "测试客户",
            ProductName = "测试产品"
        };
    }

    #endregion

    #region 生产看板测试数据

    /// <summary>
    /// 创建生产看板数据项
    /// </summary>
    public static DieBoardItem CreateDieBoardItem(
        int dieId = 1,
        DieStatus status = DieStatus.Pending,
        int totalProcesses = 3,
        int completedProcesses = 0)
    {
        return new DieBoardItem
        {
            DieID = dieId,
            DieCode = $"DM2024{dieId:D4}",
            CustomerName = "测试客户",
            ProductName = "测试产品",
            DeliveryDate = DateTime.Now.AddDays(7),
            Status = status,
            CreateTime = DateTime.Now.AddDays(-5),
            TotalProcesses = totalProcesses,
            CompletedProcesses = completedProcesses
        };
    }

    /// <summary>
    /// 创建完工记录
    /// </summary>
    public static CompletionRecord CreateCompletionRecord(
        int completionId = 1,
        int dieId = 1)
    {
        return new CompletionRecord
        {
            CompletionID = completionId,
            DieID = dieId,
            DieCode = $"DM2024{dieId:D4}",
            CustomerName = "测试客户",
            ProductName = "测试产品",
            CompleteTime = DateTime.Now.AddDays(-1),
            TotalAmount = 1500.0m,
            OperatorNo = "OP001",
            OperatorName = "操作员1",
            Remark = "测试完工"
        };
    }

    #endregion

    #region 报表统计测试数据

    /// <summary>
    /// 创建完工统计数据（按刀模）
    /// </summary>
    public static CompletionStatsByDie CreateCompletionStatsByDie(int dieId = 1)
    {
        return new CompletionStatsByDie
        {
            DieID = dieId,
            DieCode = $"DM2024{dieId:D4}",
            CustomerName = "测试客户",
            ProductName = "测试产品",
            RequiredProcesses = "绘图,切割,打磨",
            CompleteTime = DateTime.Now.AddDays(-1),
            TotalAmount = 1500.0m,
            OperatorName = "操作员1",
            Remark = ""
        };
    }

    /// <summary>
    /// 创建工序统计数据
    /// </summary>
    public static ProcessStats CreateProcessStats(string processName = "绘图")
    {
        return new ProcessStats
        {
            ProcessName = processName,
            TotalCount = 100,
            CompletedCount = 85,
            InProgressCount = 10,
            PendingCount = 5,
            CompletionRate = 85.0,
            AvgDurationMinutes = 120.5,
            TotalAmount = 50000.0m
        };
    }

    /// <summary>
    /// 创建库存汇总统计
    /// </summary>
    public static InventorySummaryStats CreateInventorySummaryStats()
    {
        return new InventorySummaryStats
        {
            TotalCount = 100,
            InStockCount = 70,
            BorrowedCount = 25,
            ScrappedCount = 3,
            RepairingCount = 2
        };
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 创建日期范围
    /// </summary>
    public static (DateTime StartDate, DateTime EndDate) CreateDateRange(int daysBack = 30)
    {
        var endDate = DateTime.Now;
        var startDate = endDate.AddDays(-daysBack);
        return (startDate, endDate);
    }

    /// <summary>
    /// 创建分页参数
    /// </summary>
    public static (int PageIndex, int PageSize) CreatePagingParams(int pageIndex = 1, int pageSize = 10)
    {
        return (pageIndex, pageSize);
    }

    #endregion
}
