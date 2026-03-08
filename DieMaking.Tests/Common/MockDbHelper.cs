using Microsoft.Data.SqlClient;
using Moq;
using System.Data;

namespace DieMaking.Tests.Common;

/// <summary>
/// 数据库Mock辅助类 - 提供标准化的数据库Mock设置
/// </summary>
public class MockDbHelper
{
    private readonly Mock<IDbConnection> _mockConnection;
    private readonly Mock<IDbCommand> _mockCommand;
    private readonly Mock<IDataReader> _mockReader;
    private readonly Mock<IDataParameterCollection> _mockParameters;

    public MockDbHelper()
    {
        _mockConnection = new Mock<IDbConnection>();
        _mockCommand = new Mock<IDbCommand>();
        _mockReader = new Mock<IDataReader>();
        _mockParameters = new Mock<IDataParameterCollection>();

        // 基础设置
        _mockConnection.Setup(c => c.CreateCommand()).Returns(_mockCommand.Object);
        _mockCommand.Setup(c => c.Parameters).Returns(_mockParameters.Object);
    }

    /// <summary>
    /// 获取模拟连接
    /// </summary>
    public Mock<IDbConnection> Connection => _mockConnection;

    /// <summary>
    /// 获取模拟命令
    /// </summary>
    public Mock<IDbCommand> Command => _mockCommand;

    /// <summary>
    /// 获取模拟读取器
    /// </summary>
    public Mock<IDataReader> Reader => _mockReader;

    /// <summary>
    /// 设置ExecuteReader返回单行数据
    /// </summary>
    public void SetupExecuteReaderSingleRow(Dictionary<string, object> rowData)
    {
        var readSequence = new Queue<bool>(new[] { true, false });
        _mockReader.Setup(r => r.Read()).Returns(() => readSequence.Count > 0 ? readSequence.Dequeue() : false);

        foreach (var kvp in rowData)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            _mockReader.Setup(r => r[key]).Returns(value);
        }

        _mockCommand.Setup(c => c.ExecuteReader()).Returns(_mockReader.Object);
    }

    /// <summary>
    /// 设置ExecuteReader返回多行数据
    /// </summary>
    public void SetupExecuteReaderMultipleRows(List<Dictionary<string, object>> rows)
    {
        var readIndex = 0;
        _mockReader.Setup(r => r.Read()).Returns(() =>
        {
            if (readIndex < rows.Count)
            {
                readIndex++;
                return true;
            }
            return false;
        });

        if (rows.Count > 0)
        {
            foreach (var columnName in rows[0].Keys)
            {
                var colName = columnName; // 捕获变量
                _mockReader.Setup(r => r[colName]).Returns(() =>
                {
                    if (readIndex > 0 && readIndex <= rows.Count)
                    {
                        return rows[readIndex - 1][colName];
                    }
                    return DBNull.Value;
                });
            }
        }

        _mockCommand.Setup(c => c.ExecuteReader()).Returns(_mockReader.Object);
    }

    /// <summary>
    /// 设置ExecuteReader返回空结果
    /// </summary>
    public void SetupExecuteReaderEmpty()
    {
        _mockReader.Setup(r => r.Read()).Returns(false);
        _mockCommand.Setup(c => c.ExecuteReader()).Returns(_mockReader.Object);
    }

    /// <summary>
    /// 设置ExecuteScalar返回值
    /// </summary>
    public void SetupExecuteScalar(object? result)
    {
        _mockCommand.Setup(c => c.ExecuteScalar()).Returns(result);
    }

    /// <summary>
    /// 设置ExecuteNonQuery返回值
    /// </summary>
    public void SetupExecuteNonQuery(int rowsAffected)
    {
        _mockCommand.Setup(c => c.ExecuteNonQuery()).Returns(rowsAffected);
    }

    /// <summary>
    /// 设置参数添加
    /// </summary>
    public void SetupParameterAdding()
    {
        _mockParameters.Setup(p => p.Add(It.IsAny<IDbDataParameter>())).Returns(0);
    }

    /// <summary>
    /// 验证SQL命令文本
    /// </summary>
    public void VerifyCommandText(string expectedContains)
    {
        _mockCommand.VerifySet(c => c.CommandText = It.Is<string>(s => s.Contains(expectedContains)));
    }

    /// <summary>
    /// 创建模拟数据读取器行数据
    /// </summary>
    public static Dictionary<string, object> CreateRow(params (string Column, object Value)[] columns)
    {
        var row = new Dictionary<string, object>();
        foreach (var (column, value) in columns)
        {
            row[column] = value ?? DBNull.Value;
        }
        return row;
    }

    /// <summary>
    /// 创建用户行数据
    /// </summary>
    public static Dictionary<string, object> CreateUserRow(
        int userId = 1,
        string username = "testuser",
        string password = "password123",
        string realName = "测试用户",
        bool isActive = true)
    {
        return CreateRow(
            ("UserID", userId),
            ("Username", username),
            ("Password", password),
            ("RealName", realName),
            ("Permissions", "刀模管理,生产管理"),
            ("Workstation", "A01"),
            ("IsActive", isActive),
            ("CreateTime", DateTime.Now.AddDays(-30)),
            ("LastLoginTime", DateTime.Now.AddDays(-1))
        );
    }

    /// <summary>
    /// 创建刀模行数据
    /// </summary>
    public static Dictionary<string, object> CreateDieRow(
        int dieId = 1,
        string dieCode = "DM20240001",
        string customerName = "测试客户",
        int status = 0,
        int auditStatus = 0)
    {
        return CreateRow(
            ("DieID", dieId),
            ("DieCode", dieCode),
            ("CustomerName", customerName),
            ("ProductName", "测试产品"),
            ("Structure", "结构A"),
            ("ModelType", "模型B"),
            ("LayoutType", "排版C"),
            ("FluteType", "瓦楞D"),
            ("Material", "钢材"),
            ("ManufactureLength", 100.5m),
            ("ManufactureWidth", 80.0m),
            ("ManufactureHeight", 20.0m),
            ("BlankLength", 120.0m),
            ("BlankWidth", 100.0m),
            ("ProcessDesc", "测试工艺"),
            ("RequiredProcesses", "工序1,工序2"),
            ("Status", status),
            ("AuditStatus", auditStatus),
            ("SourceFactory", "本厂"),
            ("ExternalOrderID", DBNull.Value),
            ("DeliveryDate", DateTime.Now.AddDays(7)),
            ("CreateTime", DateTime.Now.AddDays(-5)),
            ("CreateUser", "admin"),
            ("UpdateTime", DBNull.Value),
            ("Remark", "测试备注")
        );
    }

    /// <summary>
    /// 创建工序行数据
    /// </summary>
    public static Dictionary<string, object> CreateProcessRow(
        int processId = 1,
        int dieId = 1,
        string processName = "测试工序",
        int status = 0)
    {
        return CreateRow(
            ("ProcessID", processId),
            ("DieID", dieId),
            ("ProcessName", processName),
            ("Status", status),
            ("StartTime", status > 0 ? DateTime.Now.AddHours(-2) : DBNull.Value),
            ("CompleteTime", status == 2 ? DateTime.Now.AddHours(-1) : DBNull.Value),
            ("OperatorNo", "OP001"),
            ("OperatorName", "操作员1"),
            ("BoardLength", 100),
            ("BoardWidth", 80),
            ("KnifeLength", 50),
            ("KnifeTraceLength", 200),
            ("Formula", "L*W*0.1"),
            ("Amount", 500.0m),
            ("PrevProcessID", DBNull.Value),
            ("IsPrevCompleted", true),
            ("CreateTime", DateTime.Now.AddDays(-3))
        );
    }

    /// <summary>
    /// 创建库位行数据
    /// </summary>
    public static Dictionary<string, object> CreateLocationRow(
        int locationId = 1,
        string locationCode = "A-01-01-01",
        int status = 0)
    {
        return CreateRow(
            ("LocationID", locationId),
            ("LocationCode", locationCode),
            ("Area", "A区"),
            ("ShelfNo", "01"),
            ("LayerNo", "01"),
            ("PositionNo", "01"),
            ("Description", "测试库位"),
            ("Status", status),
            ("CreateTime", DateTime.Now.AddDays(-30))
        );
    }

    /// <summary>
    /// 创建库存行数据
    /// </summary>
    public static Dictionary<string, object> CreateInventoryRow(
        int inventoryId = 1,
        int dieId = 1,
        int? locationId = 1,
        int status = 0)
    {
        return CreateRow(
            ("InventoryID", inventoryId),
            ("DieID", dieId),
            ("LocationID", locationId ?? (object)DBNull.Value),
            ("StorageStatus", status),
            ("InStockTime", DateTime.Now.AddDays(-10)),
            ("LastBorrowTime", status == 1 ? DateTime.Now.AddDays(-2) : DBNull.Value),
            ("LastReturnTime", status == 0 ? DateTime.Now.AddDays(-1) : DBNull.Value),
            ("TotalBorrowCount", 3),
            ("Remark", ""),
            ("UpdateTime", DateTime.Now),
            ("LocationCode", locationId.HasValue ? "A-01-01-01" : DBNull.Value),
            ("DieCode", "DM20240001"),
            ("CustomerName", "测试客户"),
            ("ProductName", "测试产品")
        );
    }

    /// <summary>
    /// 创建借用记录行数据
    /// </summary>
    public static Dictionary<string, object> CreateBorrowRecordRow(
        int borrowId = 1,
        int dieId = 1,
        int inventoryId = 1,
        int status = 0)
    {
        return CreateRow(
            ("BorrowID", borrowId),
            ("DieID", dieId),
            ("InventoryID", inventoryId),
            ("BorrowType", 0),
            ("BorrowerNo", "EMP001"),
            ("BorrowerName", "借用人"),
            ("BorrowDept", "生产部"),
            ("BorrowTime", DateTime.Now.AddDays(-2)),
            ("ExpectedReturnTime", DateTime.Now.AddDays(5)),
            ("ActualReturnTime", status == 1 ? DateTime.Now.AddDays(-1) : DBNull.Value),
            ("Purpose", "生产使用"),
            ("Status", status),
            ("ReturnOperatorNo", status == 1 ? "EMP002" : ""),
            ("ReturnOperatorName", status == 1 ? "归还操作员" : ""),
            ("Remark", "测试借用"),
            ("CreateTime", DateTime.Now.AddDays(-2)),
            ("DieCode", "DM20240001"),
            ("CustomerName", "测试客户"),
            ("ProductName", "测试产品")
        );
    }
}

/// <summary>
/// 模拟数据库连接工厂
/// </summary>
public static class MockDbConnectionFactory
{
    /// <summary>
    /// 创建模拟连接
    /// </summary>
    public static (Mock<IDbConnection> Connection, Mock<IDbCommand> Command, Mock<IDataReader> Reader) Create()
    {
        var mockConnection = new Mock<IDbConnection>();
        var mockCommand = new Mock<IDbCommand>();
        var mockReader = new Mock<IDataReader>();
        var mockParameters = new Mock<IDataParameterCollection>();

        mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);
        mockCommand.Setup(c => c.Parameters).Returns(mockParameters.Object);
        mockParameters.Setup(p => p.Add(It.IsAny<IDbDataParameter>())).Returns(0);

        return (mockConnection, mockCommand, mockReader);
    }
}
