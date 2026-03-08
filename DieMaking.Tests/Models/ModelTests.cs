using DieMaking.Models;
using Xunit;

namespace DieMaking.Tests.Models;

/// <summary>
/// 模型类单元测试
/// </summary>
public class ModelTests
{
    #region User Model Tests

    [Fact]
    public void Test_User_GetPermissionList_ReturnsList()
    {
        // Arrange
        var user = new User
        {
            Permissions = "刀模管理,生产管理,仓库管理"
        };

        // Act
        var permissions = user.GetPermissionList();

        // Assert
        Assert.NotNull(permissions);
        Assert.Equal(3, permissions.Count);
        Assert.Contains("刀模管理", permissions);
        Assert.Contains("生产管理", permissions);
        Assert.Contains("仓库管理", permissions);
    }

    [Fact]
    public void Test_User_GetPermissionList_EmptyPermissions_ReturnsEmptyList()
    {
        // Arrange
        var user = new User
        {
            Permissions = ""
        };

        // Act
        var permissions = user.GetPermissionList();

        // Assert
        Assert.NotNull(permissions);
        Assert.Empty(permissions);
    }

    [Fact]
    public void Test_User_HasPermission_ExistingPermission_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Permissions = "刀模管理,生产管理,仓库管理"
        };

        // Act
        var hasPermission = user.HasPermission("生产管理");

        // Assert
        Assert.True(hasPermission);
    }

    [Fact]
    public void Test_User_HasPermission_NonExistingPermission_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Permissions = "刀模管理,生产管理"
        };

        // Act
        var hasPermission = user.HasPermission("报表统计");

        // Assert
        Assert.False(hasPermission);
    }

    [Fact]
    public void Test_User_HasPermission_EmptyPermissions_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Permissions = ""
        };

        // Act
        var hasPermission = user.HasPermission("刀模管理");

        // Assert
        Assert.False(hasPermission);
    }

    [Fact]
    public void Test_User_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        Assert.Equal(0, user.UserID);
        Assert.Equal(string.Empty, user.Username);
        Assert.Equal(string.Empty, user.Password);
        Assert.Equal(string.Empty, user.RealName);
        Assert.Equal(string.Empty, user.Permissions);
        Assert.Equal(string.Empty, user.Workstation);
        Assert.True(user.IsActive);
    }

    #endregion

    #region DieInfo Model Tests

    [Fact]
    public void Test_DieInfo_ManufactureSize_ReturnsFormattedString()
    {
        // Arrange
        var die = new DieInfo
        {
            ManufactureLength = 100.5m,
            ManufactureWidth = 80.0m,
            ManufactureHeight = 20.0m
        };

        // Act
        var size = die.ManufactureSize;

        // Assert
        Assert.Equal("100.5*80.0*20.0", size);
    }

    [Fact]
    public void Test_DieInfo_BlankSize_ReturnsFormattedString()
    {
        // Arrange
        var die = new DieInfo
        {
            BlankLength = 120.0m,
            BlankWidth = 100.0m
        };

        // Act
        var size = die.BlankSize;

        // Assert
        Assert.Equal("120.0*100.0", size);
    }

    [Fact]
    public void Test_DieInfo_StatusText_ReturnsDisplayName()
    {
        // Arrange
        var die = new DieInfo
        {
            Status = DieStatus.Pending
        };

        // Act
        var statusText = die.StatusText;

        // Assert
        Assert.Equal("待生产", statusText);
    }

    [Fact]
    public void Test_DieInfo_AuditStatusText_ReturnsDisplayName()
    {
        // Arrange
        var die = new DieInfo
        {
            AuditStatus = AuditStatus.Audited
        };

        // Act
        var auditStatusText = die.AuditStatusText;

        // Assert
        Assert.Equal("已审核", auditStatusText);
    }

    [Fact]
    public void Test_DieInfo_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var die = new DieInfo();

        // Assert
        Assert.Equal(0, die.DieID);
        Assert.Equal(string.Empty, die.DieCode);
        Assert.Equal(string.Empty, die.CustomerName);
        Assert.Equal(string.Empty, die.ProductName);
        Assert.Equal(DieStatus.Pending, die.Status);
        Assert.Equal(AuditStatus.Unaudited, die.AuditStatus);
    }

    #endregion

    #region DieProcess Model Tests

    [Fact]
    public void Test_DieProcess_StatusText_ReturnsDisplayName()
    {
        // Arrange
        var process = new DieProcess
        {
            Status = ProcessStatus.Completed
        };

        // Act
        var statusText = process.StatusText;

        // Assert
        Assert.Equal("已完成", statusText);
    }

    [Fact]
    public void Test_DieProcess_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var process = new DieProcess();

        // Assert
        Assert.Equal(0, process.ProcessID);
        Assert.Equal(0, process.DieID);
        Assert.Equal(string.Empty, process.ProcessName);
        Assert.Equal(ProcessStatus.Pending, process.Status);
    }

    #endregion

    #region StorageLocation Model Tests

    [Fact]
    public void Test_StorageLocation_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var location = new StorageLocation();

        // Assert
        Assert.Equal(0, location.LocationID);
        Assert.Equal(string.Empty, location.LocationCode);
        Assert.Equal(string.Empty, location.Area);
        Assert.Equal(string.Empty, location.ShelfNo);
        Assert.Equal(string.Empty, location.LayerNo);
        Assert.Equal(string.Empty, location.PositionNo);
        Assert.Equal(LocationStatus.Free, location.Status);
    }

    #endregion

    #region DieInventory Model Tests

    [Fact]
    public void Test_DieInventory_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var inventory = new DieInventory();

        // Assert
        Assert.Equal(0, inventory.InventoryID);
        Assert.Equal(0, inventory.DieID);
        Assert.Null(inventory.LocationID);
        Assert.Equal(StorageStatus.InStock, inventory.StorageStatus);
        Assert.Equal(0, inventory.TotalBorrowCount);
    }

    #endregion

    #region DieBorrowRecord Model Tests

    [Fact]
    public void Test_DieBorrowRecord_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var record = new DieBorrowRecord();

        // Assert
        Assert.Equal(0, record.BorrowID);
        Assert.Equal(0, record.DieID);
        Assert.Equal(0, record.InventoryID);
        Assert.Equal(BorrowType.Internal, record.BorrowType);
        Assert.Equal(BorrowStatus.Borrowing, record.Status);
    }

    #endregion

    #region Enum Extension Tests

    [Theory]
    [InlineData(DieStatus.Pending, "待生产")]
    [InlineData(DieStatus.InProgress, "生产中")]
    [InlineData(DieStatus.Completed, "已完成")]
    [InlineData(DieStatus.OnHold, "暂不生产")]
    [InlineData(DieStatus.NotRequired, "无需生产")]
    public void Test_DieStatus_GetDisplayName_ReturnsCorrectName(DieStatus status, string expectedName)
    {
        // Act
        var displayName = status.GetDisplayName();

        // Assert
        Assert.Equal(expectedName, displayName);
    }

    [Theory]
    [InlineData(AuditStatus.Unaudited, "未审核")]
    [InlineData(AuditStatus.Audited, "已审核")]
    public void Test_AuditStatus_GetDisplayName_ReturnsCorrectName(AuditStatus status, string expectedName)
    {
        // Act
        var displayName = status.GetDisplayName();

        // Assert
        Assert.Equal(expectedName, displayName);
    }

    [Theory]
    [InlineData(ProcessStatus.Pending, "待生产")]
    [InlineData(ProcessStatus.InProgress, "生产中")]
    [InlineData(ProcessStatus.Completed, "已完成")]
    public void Test_ProcessStatus_GetDisplayName_ReturnsCorrectName(ProcessStatus status, string expectedName)
    {
        // Act
        var displayName = status.GetDisplayName();

        // Assert
        Assert.Equal(expectedName, displayName);
    }

    [Fact]
    public void Test_DieStatus_UnknownValue_ReturnsUnknown()
    {
        // Arrange
        var status = (DieStatus)999;

        // Act
        var displayName = status.GetDisplayName();

        // Assert
        Assert.Equal("未知", displayName);
    }

    [Fact]
    public void Test_AuditStatus_UnknownValue_ReturnsUnknown()
    {
        // Arrange
        var status = (AuditStatus)999;

        // Act
        var displayName = status.GetDisplayName();

        // Assert
        Assert.Equal("未知", displayName);
    }

    [Fact]
    public void Test_ProcessStatus_UnknownValue_ReturnsUnknown()
    {
        // Arrange
        var status = (ProcessStatus)999;

        // Act
        var displayName = status.GetDisplayName();

        // Assert
        Assert.Equal("未知", displayName);
    }

    #endregion

    #region PermissionKeys Tests

    [Fact]
    public void Test_PermissionKeys_Constants_AreDefined()
    {
        // Assert
        Assert.Equal("刀模管理", PermissionKeys.DieManage);
        Assert.Equal("添加刀模", PermissionKeys.DieAdd);
        Assert.Equal("修改刀模", PermissionKeys.DieEdit);
        Assert.Equal("审核刀模", PermissionKeys.DieAudit);
        Assert.Equal("生产管理", PermissionKeys.Production);
        Assert.Equal("仓库管理", PermissionKeys.WarehouseManage);
        Assert.Equal("库位管理", PermissionKeys.LocationManage);
        Assert.Equal("刀模入库", PermissionKeys.DieInStock);
        Assert.Equal("刀模领用", PermissionKeys.DieBorrow);
        Assert.Equal("刀模归还", PermissionKeys.DieReturn);
        Assert.Equal("借用记录", PermissionKeys.BorrowRecord);
        Assert.Equal("报废申请", PermissionKeys.ScrapApply);
        Assert.Equal("报废审核", PermissionKeys.ScrapAudit);
        Assert.Equal("报表统计", PermissionKeys.Report);
        Assert.Equal("用户管理", PermissionKeys.UserManage);
        Assert.Equal("系统管理员", PermissionKeys.SystemAdmin);
    }

    #endregion

    #region CurrentUser Tests

    [Fact]
    public void Test_CurrentUser_IsLoggedIn_NoUser_ReturnsFalse()
    {
        // Arrange
        CurrentUser.User = null;

        // Act
        var isLoggedIn = CurrentUser.IsLoggedIn;

        // Assert
        Assert.False(isLoggedIn);
    }

    [Fact]
    public void Test_CurrentUser_IsLoggedIn_WithUser_ReturnsTrue()
    {
        // Arrange
        CurrentUser.User = new User { UserID = 1, Username = "test" };

        // Act
        var isLoggedIn = CurrentUser.IsLoggedIn;

        // Assert
        Assert.True(isLoggedIn);

        // Cleanup
        CurrentUser.User = null;
    }

    [Fact]
    public void Test_CurrentUser_HasPermission_NoUser_ReturnsFalse()
    {
        // Arrange
        CurrentUser.User = null;

        // Act
        var hasPermission = CurrentUser.HasPermission("刀模管理");

        // Assert
        Assert.False(hasPermission);
    }

    [Fact]
    public void Test_CurrentUser_HasPermission_WithPermission_ReturnsTrue()
    {
        // Arrange
        CurrentUser.User = new User
        {
            UserID = 1,
            Username = "test",
            Permissions = "刀模管理,生产管理"
        };

        // Act
        var hasPermission = CurrentUser.HasPermission("刀模管理");

        // Assert
        Assert.True(hasPermission);

        // Cleanup
        CurrentUser.User = null;
    }

    [Fact]
    public void Test_CurrentUser_HasPermission_WithoutPermission_ReturnsFalse()
    {
        // Arrange
        CurrentUser.User = new User
        {
            UserID = 1,
            Username = "test",
            Permissions = "生产管理"
        };

        // Act
        var hasPermission = CurrentUser.HasPermission("刀模管理");

        // Assert
        Assert.False(hasPermission);

        // Cleanup
        CurrentUser.User = null;
    }

    #endregion
}
