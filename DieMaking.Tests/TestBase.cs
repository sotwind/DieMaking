using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;

namespace DieMaking.Tests;

/// <summary>
/// 测试基类 - 提供通用的测试辅助方法
/// </summary>
public abstract class TestBase : IDisposable
{
    protected readonly string TestConnectionString;

    protected TestBase()
    {
        // 使用本地测试数据库连接字符串
        TestConnectionString = "Server=localhost;Database=DieMaking_Test;User Id=sa;Password=Test123456;TrustServerCertificate=True;";
    }

    /// <summary>
    /// 创建测试用的数据库连接
    /// </summary>
    protected SqlConnection CreateTestConnection()
    {
        return new SqlConnection(TestConnectionString);
    }

    /// <summary>
    /// 执行非查询SQL
    /// </summary>
    protected int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
    {
        using var connection = CreateTestConnection();
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        if (parameters != null)
        {
            command.Parameters.AddRange(parameters);
        }
        return command.ExecuteNonQuery();
    }

    /// <summary>
    /// 执行标量查询
    /// </summary>
    protected object? ExecuteScalar(string sql, params SqlParameter[] parameters)
    {
        using var connection = CreateTestConnection();
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        if (parameters != null)
        {
            command.Parameters.AddRange(parameters);
        }
        return command.ExecuteScalar();
    }

    /// <summary>
    /// 清理测试数据
    /// </summary>
    protected void CleanTestData(string tableName, string condition)
    {
        try
        {
            ExecuteNonQuery($"DELETE FROM {tableName} WHERE {condition}");
        }
        catch
        {
            // 忽略清理错误
        }
    }

    /// <summary>
    /// 使用反射设置私有字段值
    /// </summary>
    protected void SetPrivateField<T>(object obj, string fieldName, T value)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(obj, value);
    }

    /// <summary>
    /// 使用反射设置静态私有字段值
    /// </summary>
    protected void SetStaticPrivateField<T>(Type type, string fieldName, T value)
    {
        var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
        field?.SetValue(null, value);
    }

    /// <summary>
    /// 使用反射调用私有方法
    /// </summary>
    protected T? InvokePrivateMethod<T>(object obj, string methodName, params object[] parameters)
    {
        var method = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        var result = method?.Invoke(obj, parameters);
        return (T?)result;
    }

    public virtual void Dispose()
    {
        // 子类可以重写此方法进行清理
    }
}
