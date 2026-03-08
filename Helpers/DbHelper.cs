using System.Configuration;
using Microsoft.Data.SqlClient;

namespace DieMaking.Helpers;

public static class DbHelper
{
    private static string? _connectionString;

    public static string ConnectionString =>
        _connectionString ??= ConfigurationManager.ConnectionStrings["DieMakingDB"]?.ConnectionString
            ?? throw new InvalidOperationException("数据库连接字符串未配置");

    public static SqlConnection CreateConnection() => new(ConnectionString);

    public static object? ExecuteScalar(string sql, params SqlParameter[] parameters)
    {
        using var connection = CreateConnection();
        using var command = new SqlCommand(sql, connection);
        if (parameters.Length > 0) command.Parameters.AddRange(parameters);
        connection.Open();
        return command.ExecuteScalar();
    }

    public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
    {
        using var connection = CreateConnection();
        using var command = new SqlCommand(sql, connection);
        if (parameters.Length > 0) command.Parameters.AddRange(parameters);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public static List<T> ExecuteQuery<T>(string sql, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
    {
        var list = new List<T>();
        using var connection = CreateConnection();
        using var command = new SqlCommand(sql, connection);
        if (parameters.Length > 0) command.Parameters.AddRange(parameters);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read()) list.Add(mapper(reader));
        return list;
    }

    public static SqlDataReader ExecuteReader(string sql, SqlConnection connection, params SqlParameter[] parameters)
    {
        using var command = new SqlCommand(sql, connection);
        if (parameters.Length > 0) command.Parameters.AddRange(parameters);
        return command.ExecuteReader();
    }
}
