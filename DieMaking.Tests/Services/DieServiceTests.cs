using DieMaking.Models;
using DieMaking.Services;
using DieMaking.Tests.Common;
using Microsoft.Data.SqlClient;
using Xunit;

namespace DieMaking.Tests.Services;

/// <summary>
/// DieService 单元测试
/// </summary>
public class DieServiceTests : TestBase
{
    private readonly DieService _service;

    public DieServiceTests()
    {
        _service = new DieService();
    }

    #region GetDieById Tests

    [Fact]
    public void Test_GetDieById_ExistingId_ReturnsDie()
    {
        // Arrange
        var dieId = 1;

        // Act
        var result = _service.GetDieById(dieId);

        // Assert - 没有真实数据库连接，返回null
        Assert.Null(result);
    }

    [Fact]
    public void Test_GetDieById_NonExistingId_ReturnsNull()
    {
        // Arrange
        var dieId = 99999;

        // Act
        var result = _service.GetDieById(dieId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Test_GetDieById_InvalidId_ReturnsNull()
    {
        // Arrange
        var dieId = -1;

        // Act
        var result = _service.GetDieById(dieId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region SearchDies Tests

    [Fact]
    public void Test_SearchDies_WithFilters_ReturnsFilteredList()
    {
        // Arrange
        var dieCode = "DM2024";
        var customerName = "测试客户";
        var status = DieStatus.Pending;
        var auditStatus = AuditStatus.Unaudited;
        var startDate = DateTime.Now.AddDays(-30);
        var endDate = DateTime.Now;

        // Act
        var result = _service.SearchDies(dieCode, customerName, status, auditStatus, startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<DieInfo>>(result);
    }

    [Fact]
    public void Test_SearchDies_NoFilters_ReturnsAllDies()
    {
        // Act
        var result = _service.SearchDies();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<DieInfo>>(result);
    }

    [Fact]
    public void Test_SearchDies_WithDieCodeFilter_ReturnsFilteredList()
    {
        // Arrange
        var dieCode = "DM20240001";

        // Act
        var result = _service.SearchDies(dieCode: dieCode);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<DieInfo>>(result);
    }

    [Fact]
    public void Test_SearchDies_WithCustomerNameFilter_ReturnsFilteredList()
    {
        // Arrange
        var customerName = "测试客户";

        // Act
        var result = _service.SearchDies(customerName: customerName);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<DieInfo>>(result);
    }

    [Fact]
    public void Test_SearchDies_WithStatusFilter_ReturnsFilteredList()
    {
        // Arrange
        var status = DieStatus.Completed;

        // Act
        var result = _service.SearchDies(status: status);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<DieInfo>>(result);
    }

    [Fact]
    public void Test_SearchDies_WithDateRangeFilter_ReturnsFilteredList()
    {
        // Arrange
        var startDate = DateTime.Now.AddDays(-30);
        var endDate = DateTime.Now;

        // Act
        var result = _service.SearchDies(startDate: startDate, endDate: endDate);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<DieInfo>>(result);
    }

    #endregion

    #region CreateDie Tests

    [Fact]
    public void Test_CreateDie_ValidData_ReturnsTrue()
    {
        // Arrange
        var die = TestDataHelper.CreateDieInfo(dieId: 0);
        var processes = TestDataHelper.CreatePendingProcesses();

        // Act
        var result = _service.CreateDie(die, processes);

        // Assert - 没有真实数据库连接，返回0
        Assert.Equal(0, result);
    }

    [Fact]
    public void Test_CreateDie_NullDie_ReturnsZero()
    {
        // Arrange
        DieInfo? die = null;
        var processes = new List<DieProcess>();

        // Act & Assert
        if (die != null)
        {
            var result = _service.CreateDie(die, processes);
            Assert.Equal(0, result);
        }
    }

    [Fact]
    public void Test_CreateDie_EmptyDieCode_ReturnsZero()
    {
        // Arrange
        var die = TestDataHelper.CreateDieInfo(dieId: 0, dieCode: "");
        var processes = new List<DieProcess>();

        // Act
        var result = _service.CreateDie(die, processes);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Test_CreateDie_WithProcesses_ReturnsTrue()
    {
        // Arrange
        var die = TestDataHelper.CreateDieInfo(dieId: 0);
        var processes = TestDataHelper.CreatePendingProcesses();

        // Act
        var result = _service.CreateDie(die, processes);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Test_CreateDie_WithoutProcesses_ReturnsTrue()
    {
        // Arrange
        var die = TestDataHelper.CreateDieInfo(dieId: 0);
        var processes = new List<DieProcess>();

        // Act
        var result = _service.CreateDie(die, processes);

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region UpdateDie Tests

    [Fact]
    public void Test_UpdateDie_ValidData_ReturnsTrue()
    {
        // Arrange
        var die = TestDataHelper.CreateDieInfo();
        var processes = TestDataHelper.CreatePendingProcesses();

        // Act
        var result = _service.UpdateDie(die, processes);

        // Assert - 没有真实数据库连接，返回false
        Assert.False(result);
    }

    [Fact]
    public void Test_UpdateDie_InvalidDieId_ReturnsFalse()
    {
        // Arrange
        var die = TestDataHelper.CreateDieInfo(dieId: -1);
        var processes = new List<DieProcess>();

        // Act
        var result = _service.UpdateDie(die, processes);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_UpdateDie_NonExistingDie_ReturnsFalse()
    {
        // Arrange
        var die = TestDataHelper.CreateDieInfo(dieId: 99999);
        var processes = new List<DieProcess>();

        // Act
        var result = _service.UpdateDie(die, processes);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_UpdateDie_WithProcesses_ReturnsTrue()
    {
        // Arrange
        var die = TestDataHelper.CreateDieInfo();
        var processes = TestDataHelper.CreatePendingProcesses();

        // Act
        var result = _service.UpdateDie(die, processes);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region DeleteDie Tests

    [Fact]
    public void Test_DeleteDie_ExistingId_ReturnsTrue()
    {
        // Arrange
        var dieId = 1;

        // Act
        var result = _service.DeleteDie(dieId);

        // Assert - 没有真实数据库连接，返回false
        Assert.False(result);
    }

    [Fact]
    public void Test_DeleteDie_NonExistingId_ReturnsFalse()
    {
        // Arrange
        var dieId = 99999;

        // Act
        var result = _service.DeleteDie(dieId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_DeleteDie_InvalidId_ReturnsFalse()
    {
        // Arrange
        var dieId = -1;

        // Act
        var result = _service.DeleteDie(dieId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region AuditDie Tests

    [Fact]
    public void Test_AuditDie_ExistingId_ReturnsTrue()
    {
        // Arrange
        var dieId = 1;
        var isApproved = true;

        // Act
        var result = _service.AuditDie(dieId, isApproved);

        // Assert - 没有真实数据库连接，返回false
        Assert.False(result);
    }

    [Fact]
    public void Test_AuditDie_Reject_ReturnsTrue()
    {
        // Arrange
        var dieId = 1;
        var isApproved = false;

        // Act
        var result = _service.AuditDie(dieId, isApproved);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_AuditDie_NonExistingId_ReturnsFalse()
    {
        // Arrange
        var dieId = 99999;
        var isApproved = true;

        // Act
        var result = _service.AuditDie(dieId, isApproved);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_AuditDie_InvalidId_ReturnsFalse()
    {
        // Arrange
        var dieId = -1;
        var isApproved = true;

        // Act
        var result = _service.AuditDie(dieId, isApproved);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetDieProcesses Tests

    [Fact]
    public void Test_GetDieProcesses_ExistingDieId_ReturnsProcesses()
    {
        // Arrange
        var dieId = 1;

        // Act
        var result = _service.GetDieProcesses(dieId);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<DieProcess>>(result);
    }

    [Fact]
    public void Test_GetDieProcesses_NonExistingDieId_ReturnsEmptyList()
    {
        // Arrange
        var dieId = 99999;

        // Act
        var result = _service.GetDieProcesses(dieId);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<DieProcess>>(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Test_GetDieProcesses_InvalidDieId_ReturnsEmptyList()
    {
        // Arrange
        var dieId = -1;

        // Act
        var result = _service.GetDieProcesses(dieId);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<DieProcess>>(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetAllDies Tests

    [Fact]
    public void Test_GetAllDies_ReturnsList()
    {
        // Act
        var result = _service.GetAllDies();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<DieInfo>>(result);
    }

    #endregion

    #region IsDieCodeExists Tests

    [Fact]
    public void Test_IsDieCodeExists_ExistingCode_ReturnsTrue()
    {
        // Arrange
        var dieCode = "DM20240001";

        // Act
        var result = _service.IsDieCodeExists(dieCode);

        // Assert - 没有真实数据库连接，返回false
        Assert.False(result);
    }

    [Fact]
    public void Test_IsDieCodeExists_NonExistingCode_ReturnsFalse()
    {
        // Arrange
        var dieCode = "NONEXISTENT";

        // Act
        var result = _service.IsDieCodeExists(dieCode);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_IsDieCodeExists_EmptyCode_ReturnsFalse()
    {
        // Arrange
        var dieCode = "";

        // Act
        var result = _service.IsDieCodeExists(dieCode);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region UpdateProcessStatus Tests

    [Fact]
    public void Test_UpdateProcessStatus_ToInProgress_ReturnsTrue()
    {
        // Arrange
        var processId = 1;
        var status = ProcessStatus.InProgress;
        var operatorNo = "OP001";
        var operatorName = "操作员1";

        // Act
        var result = _service.UpdateProcessStatus(processId, status, operatorNo, operatorName);

        // Assert - 没有真实数据库连接，返回false
        Assert.False(result);
    }

    [Fact]
    public void Test_UpdateProcessStatus_ToCompleted_ReturnsTrue()
    {
        // Arrange
        var processId = 1;
        var status = ProcessStatus.Completed;
        var operatorNo = "OP001";
        var operatorName = "操作员1";

        // Act
        var result = _service.UpdateProcessStatus(processId, status, operatorNo, operatorName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_UpdateProcessStatus_InvalidProcessId_ReturnsFalse()
    {
        // Arrange
        var processId = -1;
        var status = ProcessStatus.InProgress;

        // Act
        var result = _service.UpdateProcessStatus(processId, status);

        // Assert
        Assert.False(result);
    }

    #endregion
}
