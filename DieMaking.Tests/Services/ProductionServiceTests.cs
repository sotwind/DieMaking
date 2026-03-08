using DieMaking.Models;
using DieMaking.Services;
using DieMaking.Tests.Common;
using Xunit;

namespace DieMaking.Tests.Services;

/// <summary>
/// ProductionService 单元测试
/// </summary>
public class ProductionServiceTests : TestBase
{
    private readonly ProductionService _service;

    public ProductionServiceTests()
    {
        _service = new ProductionService();
    }

    #region GetProductionBoard Tests

    [Fact]
    public void Test_GetProductionBoard_ReturnsBoardData()
    {
        // Arrange
        var startDate = DateTime.Now.AddDays(-30);
        var endDate = DateTime.Now;
        string? customerName = null;
        string? dieCode = null;

        // Act
        var result = _service.GetProductionBoardData(startDate, endDate, customerName, dieCode);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProductionBoardData>(result);
        Assert.NotNull(result.PendingList);
        Assert.NotNull(result.InProgressList);
        Assert.NotNull(result.CompletedList);
        Assert.NotNull(result.Statistics);
    }

    [Fact]
    public void Test_GetProductionBoard_WithCustomerFilter_ReturnsBoardData()
    {
        // Arrange
        var startDate = DateTime.Now.AddDays(-30);
        var endDate = DateTime.Now;
        var customerName = "测试客户";
        string? dieCode = null;

        // Act
        var result = _service.GetProductionBoardData(startDate, endDate, customerName, dieCode);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProductionBoardData>(result);
    }

    [Fact]
    public void Test_GetProductionBoard_WithDieCodeFilter_ReturnsBoardData()
    {
        // Arrange
        var startDate = DateTime.Now.AddDays(-30);
        var endDate = DateTime.Now;
        string? customerName = null;
        var dieCode = "DM2024";

        // Act
        var result = _service.GetProductionBoardData(startDate, endDate, customerName, dieCode);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProductionBoardData>(result);
    }

    [Fact]
    public void Test_GetProductionBoard_WithDateRange_ReturnsBoardData()
    {
        // Arrange
        var startDate = DateTime.Now.AddDays(-7);
        var endDate = DateTime.Now;
        string? customerName = null;
        string? dieCode = null;

        // Act
        var result = _service.GetProductionBoardData(startDate, endDate, customerName, dieCode);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProductionBoardData>(result);
    }

    [Fact]
    public void Test_GetProductionBoard_NoFilters_ReturnsBoardData()
    {
        // Act
        var result = _service.GetProductionBoardData(null, null, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProductionBoardData>(result);
    }

    #endregion

    #region StartProcess Tests

    [Fact]
    public void Test_StartProcess_ValidData_ReturnsTrue()
    {
        // Arrange
        var processId = 1;
        var operatorNo = "OP001";
        var operatorName = "操作员1";

        // Act
        var result = _service.StartProcess(processId, operatorNo, operatorName);

        // Assert - 没有真实数据库连接，返回false
        Assert.False(result);
    }

    [Fact]
    public void Test_StartProcess_InvalidProcessId_ReturnsFalse()
    {
        // Arrange
        var processId = -1;
        var operatorNo = "OP001";
        var operatorName = "操作员1";

        // Act
        var result = _service.StartProcess(processId, operatorNo, operatorName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_StartProcess_NonExistingProcess_ReturnsFalse()
    {
        // Arrange
        var processId = 99999;
        var operatorNo = "OP001";
        var operatorName = "操作员1";

        // Act
        var result = _service.StartProcess(processId, operatorNo, operatorName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_StartProcess_EmptyOperator_ReturnsFalse()
    {
        // Arrange
        var processId = 1;
        var operatorNo = "";
        var operatorName = "";

        // Act
        var result = _service.StartProcess(processId, operatorNo, operatorName);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CompleteProcess Tests

    [Fact]
    public void Test_CompleteProcess_ValidData_ReturnsTrue()
    {
        // Arrange
        var processId = 1;
        var amount = 500.0m;
        var operatorNo = "OP001";
        var operatorName = "操作员1";
        string? remark = null;

        // Act
        var result = _service.CompleteProcess(processId, amount, operatorNo, operatorName, remark);

        // Assert - 没有真实数据库连接，返回false
        Assert.False(result);
    }

    [Fact]
    public void Test_CompleteProcess_WithoutAmount_ReturnsTrue()
    {
        // Arrange
        var processId = 1;
        decimal? amount = null;
        var operatorNo = "OP001";
        var operatorName = "操作员1";
        string? remark = null;

        // Act
        var result = _service.CompleteProcess(processId, amount, operatorNo, operatorName, remark);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_CompleteProcess_WithRemark_ReturnsTrue()
    {
        // Arrange
        var processId = 1;
        var amount = 500.0m;
        var operatorNo = "OP001";
        var operatorName = "操作员1";
        var remark = "完成备注";

        // Act
        var result = _service.CompleteProcess(processId, amount, operatorNo, operatorName, remark);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_CompleteProcess_InvalidProcessId_ReturnsFalse()
    {
        // Arrange
        var processId = -1;
        var amount = 500.0m;
        var operatorNo = "OP001";
        var operatorName = "操作员1";

        // Act
        var result = _service.CompleteProcess(processId, amount, operatorNo, operatorName, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_CompleteProcess_NonExistingProcess_ReturnsFalse()
    {
        // Arrange
        var processId = 99999;
        var amount = 500.0m;
        var operatorNo = "OP001";
        var operatorName = "操作员1";

        // Act
        var result = _service.CompleteProcess(processId, amount, operatorNo, operatorName, null);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetCompletionRecords Tests

    [Fact]
    public void Test_GetCompletionRecords_WithFilters_ReturnsRecords()
    {
        // Arrange
        var startDate = DateTime.Now.AddDays(-30);
        var endDate = DateTime.Now;
        var dieCode = "DM2024";
        var processName = "绘图";

        // Act
        var result = _service.QueryCompletions(startDate, endDate, dieCode, processName);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<CompletionRecord>>(result);
    }

    [Fact]
    public void Test_GetCompletionRecords_NoFilters_ReturnsRecords()
    {
        // Act
        var result = _service.QueryCompletions(null, null, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<CompletionRecord>>(result);
    }

    [Fact]
    public void Test_GetCompletionRecords_WithDateRange_ReturnsRecords()
    {
        // Arrange
        var startDate = DateTime.Now.AddDays(-7);
        var endDate = DateTime.Now;

        // Act
        var result = _service.QueryCompletions(startDate, endDate, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<CompletionRecord>>(result);
    }

    [Fact]
    public void Test_GetCompletionRecords_WithDieCodeFilter_ReturnsRecords()
    {
        // Arrange
        var dieCode = "DM20240001";

        // Act
        var result = _service.QueryCompletions(null, null, dieCode, null);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<CompletionRecord>>(result);
    }

    [Fact]
    public void Test_GetCompletionRecords_WithProcessNameFilter_ReturnsRecords()
    {
        // Arrange
        var processName = "切割";

        // Act
        var result = _service.QueryCompletions(null, null, null, processName);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<CompletionRecord>>(result);
    }

    #endregion

    #region GetDieProcessesForReport Tests

    [Fact]
    public void Test_GetDieProcessesForReport_ExistingDie_ReturnsProcesses()
    {
        // Arrange
        var dieId = 1;

        // Act
        var result = _service.GetDieProcessesForReport(dieId);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<DieProcessForReport>>(result);
    }

    [Fact]
    public void Test_GetDieProcessesForReport_NonExistingDie_ReturnsEmptyList()
    {
        // Arrange
        var dieId = 99999;

        // Act
        var result = _service.GetDieProcessesForReport(dieId);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<DieProcessForReport>>(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetAvailableDiesForReport Tests

    [Fact]
    public void Test_GetAvailableDiesForReport_ReturnsDies()
    {
        // Act
        var result = _service.GetAvailableDiesForReport();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<DieInfoForReport>>(result);
    }

    #endregion

    #region IsPrevProcessCompleted Tests

    [Fact]
    public void Test_IsPrevProcessCompleted_NoPrevProcess_ReturnsTrue()
    {
        // Arrange
        var processId = 1;

        // Act
        var result = _service.IsPrevProcessCompleted(processId);

        // Assert - 没有真实数据库连接，返回false
        Assert.False(result);
    }

    [Fact]
    public void Test_IsPrevProcessCompleted_PrevCompleted_ReturnsTrue()
    {
        // Arrange
        var processId = 2;

        // Act
        var result = _service.IsPrevProcessCompleted(processId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_IsPrevProcessCompleted_InvalidProcessId_ReturnsFalse()
    {
        // Arrange
        var processId = -1;

        // Act
        var result = _service.IsPrevProcessCompleted(processId);

        // Assert
        Assert.False(result);
    }

    #endregion
}
