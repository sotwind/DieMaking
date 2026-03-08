using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace DieMaking.Helpers;

/// <summary>
/// 数据填充器 - 负责初始数据插入
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// 确保初始数据存在
    /// </summary>
    public static DataEnsureResult EnsureInitialData()
    {
        var result = new DataEnsureResult();
        var messages = new List<string>();
        var dataInserted = false;

        try
        {
            using var connection = DbHelper.CreateConnection();
            connection.Open();

            // 检查是否已有用户数据
            var checkSql = "SELECT COUNT(*) FROM DM_User";
            using var checkCommand = new SqlCommand(checkSql, connection);
            var userCount = (int)checkCommand.ExecuteScalar()!;

            if (userCount == 0)
            {
                // 插入默认管理员用户
                InsertDefaultUsers(connection);
                messages.Add("插入默认管理员用户");
                dataInserted = true;

                // 插入默认系统配置
                InsertDefaultConfigs(connection);
                messages.Add("插入默认系统配置");
            }

            result.DataInserted = dataInserted;
            result.Messages = messages;
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"初始化数据失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 异步确保初始数据存在
    /// </summary>
    public static async Task<DataEnsureResult> EnsureInitialDataAsync(CancellationToken cancellationToken = default)
    {
        var result = new DataEnsureResult();
        var messages = new List<string>();
        var dataInserted = false;

        try
        {
            using var connection = DbHelper.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var checkSql = "SELECT COUNT(*) FROM DM_User";
            using var checkCommand = new SqlCommand(checkSql, connection);
            var userCount = (int)await checkCommand.ExecuteScalarAsync(cancellationToken)!;

            if (userCount == 0)
            {
                await InsertDefaultUsersAsync(connection, cancellationToken);
                messages.Add("插入默认管理员用户");
                dataInserted = true;

                await InsertDefaultConfigsAsync(connection, cancellationToken);
                messages.Add("插入默认系统配置");
            }

            result.DataInserted = dataInserted;
            result.Messages = messages;
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"初始化数据失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 插入默认用户
    /// </summary>
    private static void InsertDefaultUsers(SqlConnection connection)
    {
        var password = HashPassword("admin123");

        var sql = @"
            INSERT INTO DM_User (Username, Password, RealName, Phone, Email, Role, Status, CreateTime)
            VALUES (@Username, @Password, @RealName, @Phone, @Email, @Role, @Status, GETDATE())";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Username", "admin");
        command.Parameters.AddWithValue("@Password", password);
        command.Parameters.AddWithValue("@RealName", "系统管理员");
        command.Parameters.AddWithValue("@Phone", "");
        command.Parameters.AddWithValue("@Email", "");
        command.Parameters.AddWithValue("@Role", 0); // Admin
        command.Parameters.AddWithValue("@Status", 1);
        command.ExecuteNonQuery();

        // 插入操作员用户
        var operatorPassword = HashPassword("operator123");
        using var opCommand = new SqlCommand(sql, connection);
        opCommand.Parameters.AddWithValue("@Username", "operator");
        opCommand.Parameters.AddWithValue("@Password", operatorPassword);
        opCommand.Parameters.AddWithValue("@RealName", "操作员");
        opCommand.Parameters.AddWithValue("@Phone", "");
        opCommand.Parameters.AddWithValue("@Email", "");
        opCommand.Parameters.AddWithValue("@Role", 1); // Operator
        opCommand.Parameters.AddWithValue("@Status", 1);
        opCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// 异步插入默认用户
    /// </summary>
    private static async Task InsertDefaultUsersAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var password = HashPassword("admin123");

        var sql = @"
            INSERT INTO DM_User (Username, Password, RealName, Phone, Email, Role, Status, CreateTime)
            VALUES (@Username, @Password, @RealName, @Phone, @Email, @Role, @Status, GETDATE())";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Username", "admin");
        command.Parameters.AddWithValue("@Password", password);
        command.Parameters.AddWithValue("@RealName", "系统管理员");
        command.Parameters.AddWithValue("@Phone", "");
        command.Parameters.AddWithValue("@Email", "");
        command.Parameters.AddWithValue("@Role", 0);
        command.Parameters.AddWithValue("@Status", 1);
        await command.ExecuteNonQueryAsync(cancellationToken);

        var operatorPassword = HashPassword("operator123");
        using var opCommand = new SqlCommand(sql, connection);
        opCommand.Parameters.AddWithValue("@Username", "operator");
        opCommand.Parameters.AddWithValue("@Password", operatorPassword);
        opCommand.Parameters.AddWithValue("@RealName", "操作员");
        opCommand.Parameters.AddWithValue("@Phone", "");
        opCommand.Parameters.AddWithValue("@Email", "");
        opCommand.Parameters.AddWithValue("@Role", 1);
        opCommand.Parameters.AddWithValue("@Status", 1);
        await opCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// 插入默认配置
    /// </summary>
    private static void InsertDefaultConfigs(SqlConnection connection)
    {
        var configs = new (string key, string value, string desc)[]
        {
            ("SystemName", "刀模管理系统", "系统名称"),
            ("DefaultPageSize", "20", "默认分页大小"),
            ("EnableOperationLog", "true", "启用操作日志"),
            ("PasswordMinLength", "6", "密码最小长度"),
            ("SessionTimeout", "30", "会话超时时间(分钟)")
        };

        var sql = @"
            INSERT INTO DM_SystemConfig (ConfigKey, ConfigValue, Description, CreateTime)
            VALUES (@ConfigKey, @ConfigValue, @Description, GETDATE())";

        foreach (var (key, value, desc) in configs)
        {
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ConfigKey", key);
            command.Parameters.AddWithValue("@ConfigValue", value);
            command.Parameters.AddWithValue("@Description", desc);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 异步插入默认配置
    /// </summary>
    private static async Task InsertDefaultConfigsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var configs = new (string key, string value, string desc)[]
        {
            ("SystemName", "刀模管理系统", "系统名称"),
            ("DefaultPageSize", "20", "默认分页大小"),
            ("EnableOperationLog", "true", "启用操作日志"),
            ("PasswordMinLength", "6", "密码最小长度"),
            ("SessionTimeout", "30", "会话超时时间(分钟)")
        };

        var sql = @"
            INSERT INTO DM_SystemConfig (ConfigKey, ConfigValue, Description, CreateTime)
            VALUES (@ConfigKey, @ConfigValue, @Description, GETDATE())";

        foreach (var (key, value, desc) in configs)
        {
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ConfigKey", key);
            command.Parameters.AddWithValue("@ConfigValue", value);
            command.Parameters.AddWithValue("@Description", desc);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 密码哈希（SHA256）
    /// </summary>
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

/// <summary>
/// 数据初始化结果
/// </summary>
public class DataEnsureResult
{
    public bool DataInserted { get; set; }
    public List<string> Messages { get; set; } = new();
}
