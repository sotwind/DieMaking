using Microsoft.Data.SqlClient;
using Moq;
using System.Data;

namespace DieMaking.Tests.Common;

/// <summary>
/// 测试基类 - 提供通用的测试基础设施
/// </summary>
public abstract class TestBase : IDisposable
{
    protected Mock<IDbConnection> MockConnection { get; }
    protected Mock<IDbCommand> MockCommand { get; }
    protected Mock<IDataReader> MockReader { get; }
    protected Mock<IDbTransaction> MockTransaction { get; }
    protected Mock<IDataParameterCollection> MockParameters { get; }

    protected TestBase()
    {
        MockConnection = new Mock<IDbConnection>();
        MockCommand = new Mock<IDbCommand>();
        MockReader = new Mock<IDataReader>();
        MockTransaction = new Mock<IDbTransaction>();
        MockParameters = new Mock<IDataParameterCollection>();

        // 设置基础连接行为
        MockConnection.Setup(c => c.CreateCommand()).Returns(MockCommand.Object);
        MockConnection.Setup(c => c.BeginTransaction()).Returns(MockTransaction.Object);
        MockCommand.Setup(c => c.Parameters).Returns(MockParameters.Object);
    }

    /// <summary>
    /// 设置模拟读取器返回单行数据
    /// </summary>
    protected void SetupReaderForSingleRow(params (string ColumnName, object Value)[] columns)
    {
        var readSequence = new Queue<bool>(new[] { true, false });
        MockReader.Setup(r => r.Read()).Returns(() => readSequence.Dequeue());

        foreach (var (columnName, value) in columns)
        {
            MockReader.Setup(r => r[columnName]).Returns(value);
        }

        MockCommand.Setup(c => c.ExecuteReader()).Returns(MockReader.Object);
    }

    /// <summary>
    /// 设置模拟读取器返回多行数据
    /// </summary>
    protected void SetupReaderForMultipleRows(List<Dictionary<string, object>> rows)
    {
        var readCount = 0;
        MockReader.Setup(r => r.Read()).Returns(() =>
        {
            if (readCount < rows.Count)
            {
                readCount++;
                return true;
            }
            return false;
        });

        if (rows.Count > 0)
        {
            foreach (var column in rows[0].Keys)
            {
                MockReader.Setup(r => r[column]).Returns(() =>
                {
                    if (readCount > 0 && readCount <= rows.Count)
                    {
                        return rows[readCount - 1][column];
                    }
                    return DBNull.Value;
                });
            }
        }

        MockCommand.Setup(c => c.ExecuteReader()).Returns(MockReader.Object);
    }

    /// <summary>
    /// 设置模拟标量查询返回值
    /// </summary>
    protected void SetupScalarResult(object? result)
    {
        MockCommand.Setup(c => c.ExecuteScalar()).Returns(result);
    }

    /// <summary>
    /// 设置模拟非查询操作返回值
    /// </summary>
    protected void SetupNonQueryResult(int rowsAffected)
    {
        MockCommand.Setup(c => c.ExecuteNonQuery()).Returns(rowsAffected);
    }

    /// <summary>
    /// 设置模拟参数添加
    /// </summary>
    protected void SetupParameterAdding()
    {
        MockParameters.Setup(p => p.Add(It.IsAny<IDbDataParameter>())).Returns(0);
    }

    /// <summary>
    /// 创建模拟的SqlParameter
    /// </summary>
    protected SqlParameter CreateParameter(string name, object? value)
    {
        return new SqlParameter(name, value ?? DBNull.Value);
    }

    public virtual void Dispose()
    {
        MockConnection.Reset();
        MockCommand.Reset();
        MockReader.Reset();
        MockTransaction.Reset();
        MockParameters.Reset();
    }
}

/// <summary>
/// 测试基类（泛型版本）- 用于特定服务测试
/// </summary>
public abstract class ServiceTestBase<TService> : TestBase where TService : class
{
    protected TService Service { get; set; } = null!;

    protected ServiceTestBase()
    {
    }

    /// <summary>
    /// 初始化服务实例（子类必须实现）
    /// </summary>
    protected abstract TService CreateService();

    /// <summary>
    /// 在每个测试方法前调用
    /// </summary>
    protected void InitializeService()
    {
        Service = CreateService();
    }
}
