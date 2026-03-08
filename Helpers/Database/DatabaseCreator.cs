using Microsoft.Data.SqlClient;

namespace DieMaking.Helpers.Database;

/// <summary>
/// 数据库创建器 - 负责检查并创建数据库
/// </summary>
public static class DatabaseCreator
{
    private static readonly string _masterConnectionString;

    static DatabaseCreator()
    {
        // 构建指向master数据库的连接字符串
        var builder = new SqlConnectionStringBuilder(DbHelper.ConnectionString)
        {
            InitialCatalog = "master"
        };
        _masterConnectionString = builder.ConnectionString;
    }

    /// <summary>
    /// 确保数据库存在
    /// </summary>
    public static DatabaseEnsureResult EnsureDatabaseExists()
    {
        var databaseName = GetDatabaseName();

        try
        {
            using var connection = new SqlConnection(_masterConnectionString);
            connection.Open();

            // 检查数据库是否存在
            var checkSql = "SELECT COUNT(*) FROM sys.databases WHERE name = @DatabaseName";
            using var checkCommand = new SqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@DatabaseName", databaseName);
            var exists = (int)checkCommand.ExecuteScalar()! > 0;

            if (exists)
            {
                return new DatabaseEnsureResult { Created = false, Message = $"数据库 '{databaseName}' 已存在" };
            }

            // 创建数据库
            var createSql = $@"
                CREATE DATABASE [{databaseName}]
                COLLATE Chinese_PRC_CI_AS
                ON PRIMARY (
                    NAME = N'{databaseName}',
                    FILENAME = N'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.MSSQLSERVER\\MSSQL\\DATA\\{databaseName}.mdf',
                    SIZE = 10MB,
                    MAXSIZE = UNLIMITED,
                    FILEGROWTH = 10MB
                )
                LOG ON (
                    NAME = N'{databaseName}_log',
                    FILENAME = N'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.MSSQLSERVER\\MSSQL\\DATA\\{databaseName}_log.ldf',
                    SIZE = 5MB,
                    MAXSIZE = 2GB,
                    FILEGROWTH = 5MB
                )";

            using var createCommand = new SqlCommand(createSql, connection);
            createCommand.ExecuteNonQuery();

            return new DatabaseEnsureResult { Created = true, Message = $"数据库 '{databaseName}' 创建成功" };
        }
        catch (Exception ex)
        {
            throw new Exception($"创建数据库失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 异步确保数据库存在
    /// </summary>
    public static async Task<DatabaseEnsureResult> EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        var databaseName = GetDatabaseName();

        try
        {
            using var connection = new SqlConnection(_masterConnectionString);
            await connection.OpenAsync(cancellationToken);

            // 检查数据库是否存在
            var checkSql = "SELECT COUNT(*) FROM sys.databases WHERE name = @DatabaseName";
            using var checkCommand = new SqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@DatabaseName", databaseName);
            var exists = (int)(await checkCommand.ExecuteScalarAsync(cancellationToken))! > 0;

            if (exists)
            {
                return new DatabaseEnsureResult { Created = false, Message = $"数据库 '{databaseName}' 已存在" };
            }

            // 创建数据库
            var createSql = $@"
                CREATE DATABASE [{databaseName}]
                COLLATE Chinese_PRC_CI_AS
                ON PRIMARY (
                    NAME = N'{databaseName}',
                    FILENAME = N'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.MSSQLSERVER\\MSSQL\\DATA\\{databaseName}.mdf',
                    SIZE = 10MB,
                    MAXSIZE = UNLIMITED,
                    FILEGROWTH = 10MB
                )
                LOG ON (
                    NAME = N'{databaseName}_log',
                    FILENAME = N'C:\\Program Files\\Microsoft SQL Server\\MSSQL15.MSSQLSERVER\\MSSQL\\DATA\\{databaseName}_log.ldf',
                    SIZE = 5MB,
                    MAXSIZE = 2GB,
                    FILEGROWTH = 5MB
                )";

            using var createCommand = new SqlCommand(createSql, connection);
            await createCommand.ExecuteNonQueryAsync(cancellationToken);

            return new DatabaseEnsureResult { Created = true, Message = $"数据库 '{databaseName}' 创建成功" };
        }
        catch (Exception ex)
        {
            throw new Exception($"创建数据库失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取数据库名称
    /// </summary>
    private static string GetDatabaseName()
    {
        var builder = new SqlConnectionStringBuilder(DbHelper.ConnectionString);
        return builder.InitialCatalog;
    }
}

/// <summary>
/// 数据库检查结果
/// </summary>
public class DatabaseEnsureResult
{
    /// <summary>是否创建</summary>
    public bool Created { get; set; }

    /// <summary>消息</summary>
    public string Message { get; set; } = string.Empty;
}
