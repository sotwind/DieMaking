using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace DieMaking.Helpers;

/// <summary>
/// 数据库初始化器 - 负责数据库、表结构创建和初始数据导入
/// </summary>
public static class DatabaseInitializer
{
    private static readonly string _masterConnectionString;

    static DatabaseInitializer()
    {
        // 构建指向master数据库的连接字符串
        var builder = new SqlConnectionStringBuilder(DbHelper.ConnectionString)
        {
            InitialCatalog = "master"
        };
        _masterConnectionString = builder.ConnectionString;
    }

    #region 数据库初始化

    /// <summary>
    /// 初始化数据库（检查并创建数据库、表、初始数据）
    /// </summary>
    public static InitializationResult Initialize()
    {
        var result = new InitializationResult();

        try
        {
            // 1. 检查并创建数据库
            var dbResult = EnsureDatabaseExists();
            result.DatabaseCreated = dbResult.Created;
            result.Messages.Add(dbResult.Message);

            // 2. 检查并创建表结构
            var tableResult = EnsureTablesExist();
            result.TablesCreated = tableResult.TablesCreated;
            result.Messages.AddRange(tableResult.Messages);

            // 3. 检查并插入初始数据
            var dataResult = EnsureInitialData();
            result.DataInitialized = dataResult.DataInserted;
            result.Messages.AddRange(dataResult.Messages);

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Messages.Add($"初始化失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 异步初始化数据库
    /// </summary>
    public static async Task<InitializationResult> InitializeAsync(CancellationToken cancellationToken = default)
    {
        var result = new InitializationResult();

        try
        {
            // 1. 检查并创建数据库
            var dbResult = await EnsureDatabaseExistsAsync(cancellationToken);
            result.DatabaseCreated = dbResult.Created;
            result.Messages.Add(dbResult.Message);

            // 2. 检查并创建表结构
            var tableResult = await EnsureTablesExistAsync(cancellationToken);
            result.TablesCreated = tableResult.TablesCreated;
            result.Messages.AddRange(tableResult.Messages);

            // 3. 检查并插入初始数据
            var dataResult = await EnsureInitialDataAsync(cancellationToken);
            result.DataInitialized = dataResult.DataInserted;
            result.Messages.AddRange(dataResult.Messages);

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Messages.Add($"初始化失败: {ex.Message}");
        }

        return result;
    }

    #endregion

    #region 数据库创建

    /// <summary>
    /// 确保数据库存在
    /// </summary>
    private static DatabaseEnsureResult EnsureDatabaseExists()
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
    private static async Task<DatabaseEnsureResult> EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
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

    #endregion

    #region 表结构创建

    /// <summary>
    /// 确保表结构存在
    /// </summary>
    private static TableEnsureResult EnsureTablesExist()
    {
        var result = new TableEnsureResult();
        var messages = new List<string>();
        var tablesCreated = 0;

        try
        {
            using var connection = DbHelper.CreateConnection();
            connection.Open();

            // 创建用户表
            if (CreateTableIfNotExists(connection, "DM_User", GetUserTableSql()))
            {
                tablesCreated++;
                messages.Add("创建表: DM_User");
            }

            // 创建刀模信息表
            if (CreateTableIfNotExists(connection, "DM_DieInfo", GetDieInfoTableSql()))
            {
                tablesCreated++;
                messages.Add("创建表: DM_DieInfo");
            }

            // 创建刀模工序表
            if (CreateTableIfNotExists(connection, "DM_DieProcess", GetDieProcessTableSql()))
            {
                tablesCreated++;
                messages.Add("创建表: DM_DieProcess");
            }

            // 创建完工记录表
            if (CreateTableIfNotExists(connection, "DM_DieCompletion", GetDieCompletionTableSql()))
            {
                tablesCreated++;
                messages.Add("创建表: DM_DieCompletion");
            }

            // 创建库存表
            if (CreateTableIfNotExists(connection, "DM_DieInventory", GetDieInventoryTableSql()))
            {
                tablesCreated++;
                messages.Add("创建表: DM_DieInventory");
            }

            // 创建库位表
            if (CreateTableIfNotExists(connection, "DM_StorageLocation", GetStorageLocationTableSql()))
            {
                tablesCreated++;
                messages.Add("创建表: DM_StorageLocation");
            }

            // 创建借用记录表
            if (CreateTableIfNotExists(connection, "DM_DieBorrowRecord", GetDieBorrowRecordTableSql()))
            {
                tablesCreated++;
                messages.Add("创建表: DM_DieBorrowRecord");
            }

            // 创建报废记录表
            if (CreateTableIfNotExists(connection, "DM_DieScrapRecord", GetDieScrapRecordTableSql()))
            {
                tablesCreated++;
                messages.Add("创建表: DM_DieScrapRecord");
            }

            // 创建操作日志表
            if (CreateTableIfNotExists(connection, "DM_OperationLog", GetOperationLogTableSql()))
            {
                tablesCreated++;
                messages.Add("创建表: DM_OperationLog");
            }

            // 创建系统配置表
            if (CreateTableIfNotExists(connection, "DM_SystemConfig", GetSystemConfigTableSql()))
            {
                tablesCreated++;
                messages.Add("创建表: DM_SystemConfig");
            }

            // 创建数据库版本表
            if (CreateTableIfNotExists(connection, "DM_DatabaseVersion", GetDatabaseVersionTableSql()))
            {
                tablesCreated++;
                messages.Add("创建表: DM_DatabaseVersion");
            }

            // 创建索引
            CreateIndexes(connection);

            result.TablesCreated = tablesCreated;
            result.Messages = messages;

            if (tablesCreated == 0)
            {
                messages.Add("所有表已存在，无需创建");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"创建表结构失败: {ex.Message}", ex);
        }

        return result;
    }

    /// <summary>
    /// 异步确保表结构存在
    /// </summary>
    private static async Task<TableEnsureResult> EnsureTablesExistAsync(CancellationToken cancellationToken)
    {
        var result = new TableEnsureResult();
        var messages = new List<string>();
        var tablesCreated = 0;

        try
        {
            using var connection = DbHelper.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            // 创建用户表
            if (await CreateTableIfNotExistsAsync(connection, "DM_User", GetUserTableSql(), cancellationToken))
            {
                tablesCreated++;
                messages.Add("创建表: DM_User");
            }

            // 创建刀模信息表
            if (await CreateTableIfNotExistsAsync(connection, "DM_DieInfo", GetDieInfoTableSql(), cancellationToken))
            {
                tablesCreated++;
                messages.Add("创建表: DM_DieInfo");
            }

            // 创建刀模工序表
            if (await CreateTableIfNotExistsAsync(connection, "DM_DieProcess", GetDieProcessTableSql(), cancellationToken))
            {
                tablesCreated++;
                messages.Add("创建表: DM_DieProcess");
            }

            // 创建完工记录表
            if (await CreateTableIfNotExistsAsync(connection, "DM_DieCompletion", GetDieCompletionTableSql(), cancellationToken))
            {
                tablesCreated++;
                messages.Add("创建表: DM_DieCompletion");
            }

            // 创建库存表
            if (await CreateTableIfNotExistsAsync(connection, "DM_DieInventory", GetDieInventoryTableSql(), cancellationToken))
            {
                tablesCreated++;
                messages.Add("创建表: DM_DieInventory");
            }

            // 创建库位表
            if (await CreateTableIfNotExistsAsync(connection, "DM_StorageLocation", GetStorageLocationTableSql(), cancellationToken))
            {
                tablesCreated++;
                messages.Add("创建表: DM_StorageLocation");
            }

            // 创建借用记录表
            if (await CreateTableIfNotExistsAsync(connection, "DM_DieBorrowRecord", GetDieBorrowRecordTableSql(), cancellationToken))
            {
                tablesCreated++;
                messages.Add("创建表: DM_DieBorrowRecord");
            }

            // 创建报废记录表
            if (await CreateTableIfNotExistsAsync(connection, "DM_DieScrapRecord", GetDieScrapRecordTableSql(), cancellationToken))
            {
                tablesCreated++;
                messages.Add("创建表: DM_DieScrapRecord");
            }

            // 创建操作日志表
            if (await CreateTableIfNotExistsAsync(connection, "DM_OperationLog", GetOperationLogTableSql(), cancellationToken))
            {
                tablesCreated++;
                messages.Add("创建表: DM_OperationLog");
            }

            // 创建系统配置表
            if (await CreateTableIfNotExistsAsync(connection, "DM_SystemConfig", GetSystemConfigTableSql(), cancellationToken))
            {
                tablesCreated++;
                messages.Add("创建表: DM_SystemConfig");
            }

            // 创建数据库版本表
            if (await CreateTableIfNotExistsAsync(connection, "DM_DatabaseVersion", GetDatabaseVersionTableSql(), cancellationToken))
            {
                tablesCreated++;
                messages.Add("创建表: DM_DatabaseVersion");
            }

            // 创建索引
            await CreateIndexesAsync(connection, cancellationToken);

            result.TablesCreated = tablesCreated;
            result.Messages = messages;

            if (tablesCreated == 0)
            {
                messages.Add("所有表已存在，无需创建");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"创建表结构失败: {ex.Message}", ex);
        }

        return result;
    }

    /// <summary>
    /// 如果表不存在则创建
    /// </summary>
    private static bool CreateTableIfNotExists(SqlConnection connection, string tableName, string createSql)
    {
        var checkSql = "SELECT COUNT(*) FROM sys.tables WHERE name = @TableName";
        using var checkCommand = new SqlCommand(checkSql, connection);
        checkCommand.Parameters.AddWithValue("@TableName", tableName);
        var exists = (int)checkCommand.ExecuteScalar()! > 0;

        if (!exists)
        {
            using var command = new SqlCommand(createSql, connection);
            command.ExecuteNonQuery();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 异步如果表不存在则创建
    /// </summary>
    private static async Task<bool> CreateTableIfNotExistsAsync(SqlConnection connection, string tableName, string createSql, CancellationToken cancellationToken)
    {
        var checkSql = "SELECT COUNT(*) FROM sys.tables WHERE name = @TableName";
        using var checkCommand = new SqlCommand(checkSql, connection);
        checkCommand.Parameters.AddWithValue("@TableName", tableName);
        var exists = (int)(await checkCommand.ExecuteScalarAsync(cancellationToken))! > 0;

        if (!exists)
        {
            using var command = new SqlCommand(createSql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }

        return false;
    }

    #endregion

    #region 初始数据

    /// <summary>
    /// 确保初始数据存在
    /// </summary>
    private static DataEnsureResult EnsureInitialData()
    {
        var result = new DataEnsureResult();
        var messages = new List<string>();
        var dataInserted = false;

        try
        {
            using var connection = DbHelper.CreateConnection();
            connection.Open();

            // 1. 创建默认管理员用户
            if (EnsureAdminUser(connection))
            {
                dataInserted = true;
                messages.Add("初始化: 默认管理员用户 (admin/admin123)");
            }

            // 2. 初始化系统配置
            if (EnsureSystemConfig(connection))
            {
                dataInserted = true;
                messages.Add("初始化: 系统配置参数");
            }

            // 3. 初始化常用工序类型
            if (EnsureProcessTypes(connection))
            {
                dataInserted = true;
                messages.Add("初始化: 常用工序类型");
            }

            // 4. 初始化示例库位数据
            if (EnsureSampleLocations(connection))
            {
                dataInserted = true;
                messages.Add("初始化: 示例库位数据");
            }

            result.DataInserted = dataInserted;
            result.Messages = messages;

            if (!dataInserted)
            {
                messages.Add("初始数据已存在，无需初始化");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"初始化数据失败: {ex.Message}", ex);
        }

        return result;
    }

    /// <summary>
    /// 异步确保初始数据存在
    /// </summary>
    private static async Task<DataEnsureResult> EnsureInitialDataAsync(CancellationToken cancellationToken)
    {
        var result = new DataEnsureResult();
        var messages = new List<string>();
        var dataInserted = false;

        try
        {
            using var connection = DbHelper.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            // 1. 创建默认管理员用户
            if (await EnsureAdminUserAsync(connection, cancellationToken))
            {
                dataInserted = true;
                messages.Add("初始化: 默认管理员用户 (admin/admin123)");
            }

            // 2. 初始化系统配置
            if (await EnsureSystemConfigAsync(connection, cancellationToken))
            {
                dataInserted = true;
                messages.Add("初始化: 系统配置参数");
            }

            // 3. 初始化常用工序类型
            if (await EnsureProcessTypesAsync(connection, cancellationToken))
            {
                dataInserted = true;
                messages.Add("初始化: 常用工序类型");
            }

            // 4. 初始化示例库位数据
            if (await EnsureSampleLocationsAsync(connection, cancellationToken))
            {
                dataInserted = true;
                messages.Add("初始化: 示例库位数据");
            }

            result.DataInserted = dataInserted;
            result.Messages = messages;

            if (!dataInserted)
            {
                messages.Add("初始数据已存在，无需初始化");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"初始化数据失败: {ex.Message}", ex);
        }

        return result;
    }

    /// <summary>
    /// 确保管理员用户存在
    /// </summary>
    private static bool EnsureAdminUser(SqlConnection connection)
    {
        var checkSql = "SELECT COUNT(*) FROM DM_User WHERE Username = 'admin'";
        using var checkCommand = new SqlCommand(checkSql, connection);
        var exists = (int)checkCommand.ExecuteScalar()! > 0;

        if (!exists)
        {
            var password = HashPassword("admin123");
            var insertSql = @"
                INSERT INTO DM_User (Username, Password, RealName, Permissions, Workstation, IsActive, CreateTime)
                VALUES ('admin', @Password, '系统管理员', 'ALL', 'Admin', 1, GETDATE())";

            using var command = new SqlCommand(insertSql, connection);
            command.Parameters.AddWithValue("@Password", password);
            command.ExecuteNonQuery();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 异步确保管理员用户存在
    /// </summary>
    private static async Task<bool> EnsureAdminUserAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var checkSql = "SELECT COUNT(*) FROM DM_User WHERE Username = 'admin'";
        using var checkCommand = new SqlCommand(checkSql, connection);
        var exists = (int)(await checkCommand.ExecuteScalarAsync(cancellationToken))! > 0;

        if (!exists)
        {
            var password = HashPassword("admin123");
            var insertSql = @"
                INSERT INTO DM_User (Username, Password, RealName, Permissions, Workstation, IsActive, CreateTime)
                VALUES ('admin', @Password, '系统管理员', 'ALL', 'Admin', 1, GETDATE())";

            using var command = new SqlCommand(insertSql, connection);
            command.Parameters.AddWithValue("@Password", password);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 确保系统配置存在
    /// </summary>
    private static bool EnsureSystemConfig(SqlConnection connection)
    {
        var configs = new[]
        {
            ("SystemName", "刀模管理系统", "系统名称"),
            ("CompanyName", "", "公司名称"),
            ("BackupPath", @"C:\DieMaking\Backup", "备份路径"),
            ("AutoBackup", "false", "自动备份"),
            ("BackupRetentionDays", "30", "备份保留天数"),
            ("SessionTimeout", "30", "会话超时时间(分钟)"),
            ("EnableAudit", "true", "启用审核流程"),
            ("DefaultPageSize", "20", "默认分页大小")
        };

        var inserted = false;

        foreach (var (key, value, description) in configs)
        {
            var checkSql = "SELECT COUNT(*) FROM DM_SystemConfig WHERE ConfigKey = @ConfigKey";
            using var checkCommand = new SqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@ConfigKey", key);
            var exists = (int)checkCommand.ExecuteScalar()! > 0;

            if (!exists)
            {
                var insertSql = @"
                    INSERT INTO DM_SystemConfig (ConfigKey, ConfigValue, Description, CreateTime)
                    VALUES (@ConfigKey, @ConfigValue, @Description, GETDATE())";

                using var command = new SqlCommand(insertSql, connection);
                command.Parameters.AddWithValue("@ConfigKey", key);
                command.Parameters.AddWithValue("@ConfigValue", value);
                command.Parameters.AddWithValue("@Description", description);
                command.ExecuteNonQuery();
                inserted = true;
            }
        }

        return inserted;
    }

    /// <summary>
    /// 异步确保系统配置存在
    /// </summary>
    private static async Task<bool> EnsureSystemConfigAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var configs = new[]
        {
            ("SystemName", "刀模管理系统", "系统名称"),
            ("CompanyName", "", "公司名称"),
            ("BackupPath", @"C:\DieMaking\Backup", "备份路径"),
            ("AutoBackup", "false", "自动备份"),
            ("BackupRetentionDays", "30", "备份保留天数"),
            ("SessionTimeout", "30", "会话超时时间(分钟)"),
            ("EnableAudit", "true", "启用审核流程"),
            ("DefaultPageSize", "20", "默认分页大小")
        };

        var inserted = false;

        foreach (var (key, value, description) in configs)
        {
            var checkSql = "SELECT COUNT(*) FROM DM_SystemConfig WHERE ConfigKey = @ConfigKey";
            using var checkCommand = new SqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@ConfigKey", key);
            var exists = (int)(await checkCommand.ExecuteScalarAsync(cancellationToken))! > 0;

            if (!exists)
            {
                var insertSql = @"
                    INSERT INTO DM_SystemConfig (ConfigKey, ConfigValue, Description, CreateTime)
                    VALUES (@ConfigKey, @ConfigValue, @Description, GETDATE())";

                using var command = new SqlCommand(insertSql, connection);
                command.Parameters.AddWithValue("@ConfigKey", key);
                command.Parameters.AddWithValue("@ConfigValue", value);
                command.Parameters.AddWithValue("@Description", description);
                await command.ExecuteNonQueryAsync(cancellationToken);
                inserted = true;
            }
        }

        return inserted;
    }

    /// <summary>
    /// 确保工序类型存在（作为系统配置存储）
    /// </summary>
    private static bool EnsureProcessTypes(SqlConnection connection)
    {
        var processTypes = new[] { "打样", "切板", "弯刀", "装刀", "质检", "入库" };
        var inserted = false;

        foreach (var processType in processTypes)
        {
            var checkSql = "SELECT COUNT(*) FROM DM_SystemConfig WHERE ConfigKey = @ConfigKey";
            using var checkCommand = new SqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@ConfigKey", $"ProcessType_{processType}");
            var exists = (int)checkCommand.ExecuteScalar()! > 0;

            if (!exists)
            {
                var insertSql = @"
                    INSERT INTO DM_SystemConfig (ConfigKey, ConfigValue, Description, CreateTime)
                    VALUES (@ConfigKey, @ConfigValue, '工序类型', GETDATE())";

                using var command = new SqlCommand(insertSql, connection);
                command.Parameters.AddWithValue("@ConfigKey", $"ProcessType_{processType}");
                command.Parameters.AddWithValue("@ConfigValue", processType);
                command.ExecuteNonQuery();
                inserted = true;
            }
        }

        return inserted;
    }

    /// <summary>
    /// 异步确保工序类型存在
    /// </summary>
    private static async Task<bool> EnsureProcessTypesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var processTypes = new[] { "打样", "切板", "弯刀", "装刀", "质检", "入库" };
        var inserted = false;

        foreach (var processType in processTypes)
        {
            var checkSql = "SELECT COUNT(*) FROM DM_SystemConfig WHERE ConfigKey = @ConfigKey";
            using var checkCommand = new SqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@ConfigKey", $"ProcessType_{processType}");
            var exists = (int)(await checkCommand.ExecuteScalarAsync(cancellationToken))! > 0;

            if (!exists)
            {
                var insertSql = @"
                    INSERT INTO DM_SystemConfig (ConfigKey, ConfigValue, Description, CreateTime)
                    VALUES (@ConfigKey, @ConfigValue, '工序类型', GETDATE())";

                using var command = new SqlCommand(insertSql, connection);
                command.Parameters.AddWithValue("@ConfigKey", $"ProcessType_{processType}");
                command.Parameters.AddWithValue("@ConfigValue", processType);
                await command.ExecuteNonQueryAsync(cancellationToken);
                inserted = true;
            }
        }

        return inserted;
    }

    /// <summary>
    /// 确保示例库位数据存在
    /// </summary>
    private static bool EnsureSampleLocations(SqlConnection connection)
    {
        var checkSql = "SELECT COUNT(*) FROM DM_StorageLocation";
        using var checkCommand = new SqlCommand(checkSql, connection);
        var exists = (int)checkCommand.ExecuteScalar()! > 0;

        if (!exists)
        {
            var locations = new[]
            {
                ("A", "01", "01", "01", "A-01-01-01", "A区1架1层1位"),
                ("A", "01", "01", "02", "A-01-01-02", "A区1架1层2位"),
                ("A", "01", "01", "03", "A-01-01-03", "A区1架1层3位"),
                ("A", "01", "02", "01", "A-01-02-01", "A区1架2层1位"),
                ("A", "01", "02", "02", "A-01-02-02", "A区1架2层2位"),
                ("A", "02", "01", "01", "A-02-01-01", "A区2架1层1位"),
                ("A", "02", "01", "02", "A-02-01-02", "A区2架1层2位"),
                ("B", "01", "01", "01", "B-01-01-01", "B区1架1层1位"),
                ("B", "01", "01", "02", "B-01-01-02", "B区1架1层2位"),
                ("B", "02", "01", "01", "B-02-01-01", "B区2架1层1位")
            };

            foreach (var (area, shelfNo, layerNo, positionNo, locationCode, description) in locations)
            {
                var insertSql = @"
                    INSERT INTO DM_StorageLocation (LocationCode, Area, ShelfNo, LayerNo, PositionNo, Description, Status, CreateTime)
                    VALUES (@LocationCode, @Area, @ShelfNo, @LayerNo, @PositionNo, @Description, 0, GETDATE())";

                using var command = new SqlCommand(insertSql, connection);
                command.Parameters.AddWithValue("@LocationCode", locationCode);
                command.Parameters.AddWithValue("@Area", area);
                command.Parameters.AddWithValue("@ShelfNo", shelfNo);
                command.Parameters.AddWithValue("@LayerNo", layerNo);
                command.Parameters.AddWithValue("@PositionNo", positionNo);
                command.Parameters.AddWithValue("@Description", description);
                command.ExecuteNonQuery();
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// 异步确保示例库位数据存在
    /// </summary>
    private static async Task<bool> EnsureSampleLocationsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var checkSql = "SELECT COUNT(*) FROM DM_StorageLocation";
        using var checkCommand = new SqlCommand(checkSql, connection);
        var exists = (int)(await checkCommand.ExecuteScalarAsync(cancellationToken))! > 0;

        if (!exists)
        {
            var locations = new[]
            {
                ("A", "01", "01", "01", "A-01-01-01", "A区1架1层1位"),
                ("A", "01", "01", "02", "A-01-01-02", "A区1架1层2位"),
                ("A", "01", "01", "03", "A-01-01-03", "A区1架1层3位"),
                ("A", "01", "02", "01", "A-01-02-01", "A区1架2层1位"),
                ("A", "01", "02", "02", "A-01-02-02", "A区1架2层2位"),
                ("A", "02", "01", "01", "A-02-01-01", "A区2架1层1位"),
                ("A", "02", "01", "02", "A-02-01-02", "A区2架1层2位"),
                ("B", "01", "01", "01", "B-01-01-01", "B区1架1层1位"),
                ("B", "01", "01", "02", "B-01-01-02", "B区1架1层2位"),
                ("B", "02", "01", "01", "B-02-01-01", "B区2架1层1位")
            };

            foreach (var (area, shelfNo, layerNo, positionNo, locationCode, description) in locations)
            {
                var insertSql = @"
                    INSERT INTO DM_StorageLocation (LocationCode, Area, ShelfNo, LayerNo, PositionNo, Description, Status, CreateTime)
                    VALUES (@LocationCode, @Area, @ShelfNo, @LayerNo, @PositionNo, @Description, 0, GETDATE())";

                using var command = new SqlCommand(insertSql, connection);
                command.Parameters.AddWithValue("@LocationCode", locationCode);
                command.Parameters.AddWithValue("@Area", area);
                command.Parameters.AddWithValue("@ShelfNo", shelfNo);
                command.Parameters.AddWithValue("@LayerNo", layerNo);
                command.Parameters.AddWithValue("@PositionNo", positionNo);
                command.Parameters.AddWithValue("@Description", description);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            return true;
        }

        return false;
    }

    #endregion

    #region 表结构SQL

    private static string GetUserTableSql() => @"
        CREATE TABLE DM_User (
            UserID INT IDENTITY(1,1) PRIMARY KEY,
            Username NVARCHAR(50) NOT NULL UNIQUE,
            Password NVARCHAR(256) NOT NULL,
            RealName NVARCHAR(50) NOT NULL,
            Permissions NVARCHAR(500),
            Workstation NVARCHAR(50),
            IsActive BIT DEFAULT 1,
            CreateTime DATETIME2 DEFAULT GETDATE(),
            LastLoginTime DATETIME2 NULL
        );
        CREATE INDEX IX_DM_User_Username ON DM_User(Username);
        CREATE INDEX IX_DM_User_IsActive ON DM_User(IsActive);";

    private static string GetDieInfoTableSql() => @"
        CREATE TABLE DM_DieInfo (
            DieID INT IDENTITY(1,1) PRIMARY KEY,
            DieCode NVARCHAR(50) NOT NULL UNIQUE,
            CustomerName NVARCHAR(100) NOT NULL,
            ProductName NVARCHAR(100) NOT NULL,
            Structure NVARCHAR(50),
            ModelType NVARCHAR(50),
            LayoutType NVARCHAR(50),
            FluteType NVARCHAR(50),
            Material NVARCHAR(100),
            ManufactureLength DECIMAL(10,2),
            ManufactureWidth DECIMAL(10,2),
            ManufactureHeight DECIMAL(10,2),
            BlankLength DECIMAL(10,2),
            BlankWidth DECIMAL(10,2),
            ProcessDesc NVARCHAR(500),
            RequiredProcesses NVARCHAR(200),
            Status INT DEFAULT 0,
            AuditStatus INT DEFAULT 0,
            SourceFactory NVARCHAR(100),
            ExternalOrderID INT,
            DeliveryDate DATE,
            CreateTime DATETIME2 DEFAULT GETDATE(),
            UpdateTime DATETIME2 NULL,
            CreateUser NVARCHAR(50),
            Remark NVARCHAR(500)
        );
        CREATE INDEX IX_DM_DieInfo_DieCode ON DM_DieInfo(DieCode);
        CREATE INDEX IX_DM_DieInfo_CustomerName ON DM_DieInfo(CustomerName);
        CREATE INDEX IX_DM_DieInfo_Status ON DM_DieInfo(Status);
        CREATE INDEX IX_DM_DieInfo_CreateTime ON DM_DieInfo(CreateTime);";

    private static string GetDieProcessTableSql() => @"
        CREATE TABLE DM_DieProcess (
            ProcessID INT IDENTITY(1,1) PRIMARY KEY,
            DieID INT NOT NULL,
            ProcessName NVARCHAR(50) NOT NULL,
            Status INT DEFAULT 0,
            StartTime DATETIME2 NULL,
            CompleteTime DATETIME2 NULL,
            OperatorNo NVARCHAR(50),
            OperatorName NVARCHAR(50),
            BoardLength INT,
            BoardWidth INT,
            KnifeLength INT,
            KnifeTraceLength INT,
            Formula NVARCHAR(200),
            Amount DECIMAL(18,2),
            PrevProcessID INT NULL,
            IsPrevCompleted BIT DEFAULT 0,
            CreateTime DATETIME2 DEFAULT GETDATE(),
            FOREIGN KEY (DieID) REFERENCES DM_DieInfo(DieID) ON DELETE CASCADE
        );
        CREATE INDEX IX_DM_DieProcess_DieID ON DM_DieProcess(DieID);
        CREATE INDEX IX_DM_DieProcess_Status ON DM_DieProcess(Status);
        CREATE INDEX IX_DM_DieProcess_ProcessName ON DM_DieProcess(ProcessName);";

    private static string GetDieCompletionTableSql() => @"
        CREATE TABLE DM_DieCompletion (
            CompletionID INT IDENTITY(1,1) PRIMARY KEY,
            DieID INT NOT NULL,
            CompleteTime DATETIME2 DEFAULT GETDATE(),
            TotalAmount DECIMAL(18,2),
            OperatorNo NVARCHAR(50),
            OperatorName NVARCHAR(50),
            Remark NVARCHAR(500),
            FOREIGN KEY (DieID) REFERENCES DM_DieInfo(DieID) ON DELETE CASCADE
        );
        CREATE INDEX IX_DM_DieCompletion_DieID ON DM_DieCompletion(DieID);
        CREATE INDEX IX_DM_DieCompletion_CompleteTime ON DM_DieCompletion(CompleteTime);";

    private static string GetDieInventoryTableSql() => @"
        CREATE TABLE DM_DieInventory (
            InventoryID INT IDENTITY(1,1) PRIMARY KEY,
            DieID INT NOT NULL UNIQUE,
            LocationID INT,
            StorageStatus INT DEFAULT 0,
            InStockTime DATETIME2,
            LastBorrowTime DATETIME2,
            LastReturnTime DATETIME2,
            TotalBorrowCount INT DEFAULT 0,
            Remark NVARCHAR(500),
            UpdateTime DATETIME2 DEFAULT GETDATE(),
            FOREIGN KEY (DieID) REFERENCES DM_DieInfo(DieID) ON DELETE CASCADE,
            FOREIGN KEY (LocationID) REFERENCES DM_StorageLocation(LocationID)
        );
        CREATE INDEX IX_DM_DieInventory_DieID ON DM_DieInventory(DieID);
        CREATE INDEX IX_DM_DieInventory_LocationID ON DM_DieInventory(LocationID);
        CREATE INDEX IX_DM_DieInventory_StorageStatus ON DM_DieInventory(StorageStatus);";

    private static string GetStorageLocationTableSql() => @"
        CREATE TABLE DM_StorageLocation (
            LocationID INT IDENTITY(1,1) PRIMARY KEY,
            LocationCode NVARCHAR(50) NOT NULL UNIQUE,
            Area NVARCHAR(50) NOT NULL,
            ShelfNo NVARCHAR(20) NOT NULL,
            LayerNo NVARCHAR(20) NOT NULL,
            PositionNo NVARCHAR(20) NOT NULL,
            Description NVARCHAR(200),
            Status INT DEFAULT 0,
            CreateTime DATETIME2 DEFAULT GETDATE()
        );
        CREATE INDEX IX_DM_StorageLocation_LocationCode ON DM_StorageLocation(LocationCode);
        CREATE INDEX IX_DM_StorageLocation_Area ON DM_StorageLocation(Area);
        CREATE INDEX IX_DM_StorageLocation_Status ON DM_StorageLocation(Status);";

    private static string GetDieBorrowRecordTableSql() => @"
        CREATE TABLE DM_DieBorrowRecord (
            BorrowID INT IDENTITY(1,1) PRIMARY KEY,
            DieID INT NOT NULL,
            InventoryID INT NOT NULL,
            BorrowType INT NOT NULL,
            BorrowerNo NVARCHAR(50) NOT NULL,
            BorrowerName NVARCHAR(50) NOT NULL,
            BorrowDept NVARCHAR(100),
            BorrowTime DATETIME2 DEFAULT GETDATE(),
            ExpectedReturnTime DATETIME2,
            ActualReturnTime DATETIME2,
            Purpose NVARCHAR(500),
            Status INT DEFAULT 0,
            ReturnOperatorNo NVARCHAR(50),
            ReturnOperatorName NVARCHAR(50),
            Remark NVARCHAR(500),
            CreateTime DATETIME2 DEFAULT GETDATE(),
            FOREIGN KEY (DieID) REFERENCES DM_DieInfo(DieID) ON DELETE CASCADE
        );
        CREATE INDEX IX_DM_DieBorrowRecord_DieID ON DM_DieBorrowRecord(DieID);
        CREATE INDEX IX_DM_DieBorrowRecord_Status ON DM_DieBorrowRecord(Status);
        CREATE INDEX IX_DM_DieBorrowRecord_BorrowTime ON DM_DieBorrowRecord(BorrowTime);";

    private static string GetDieScrapRecordTableSql() => @"
        CREATE TABLE DM_DieScrapRecord (
            ScrapID INT IDENTITY(1,1) PRIMARY KEY,
            DieID INT NOT NULL,
            InventoryID INT NOT NULL,
            ScrapReason NVARCHAR(500) NOT NULL,
            ScrapType NVARCHAR(50),
            ApplicantNo NVARCHAR(50) NOT NULL,
            ApplicantName NVARCHAR(50) NOT NULL,
            ApplyTime DATETIME2 DEFAULT GETDATE(),
            AuditorNo NVARCHAR(50),
            AuditorName NVARCHAR(50),
            AuditTime DATETIME2,
            AuditStatus INT DEFAULT 0,
            AuditRemark NVARCHAR(500),
            ScrapTime DATETIME2,
            CreateTime DATETIME2 DEFAULT GETDATE(),
            FOREIGN KEY (DieID) REFERENCES DM_DieInfo(DieID) ON DELETE CASCADE
        );
        CREATE INDEX IX_DM_DieScrapRecord_DieID ON DM_DieScrapRecord(DieID);
        CREATE INDEX IX_DM_DieScrapRecord_AuditStatus ON DM_DieScrapRecord(AuditStatus);
        CREATE INDEX IX_DM_DieScrapRecord_ApplyTime ON DM_DieScrapRecord(ApplyTime);";

    private static string GetOperationLogTableSql() => @"
        CREATE TABLE DM_OperationLog (
            LogID INT IDENTITY(1,1) PRIMARY KEY,
            UserID INT,
            Username NVARCHAR(50),
            OperationType NVARCHAR(50) NOT NULL,
            OperationDesc NVARCHAR(500) NOT NULL,
            DieID INT,
            IPAddress NVARCHAR(50),
            CreateTime DATETIME2 DEFAULT GETDATE()
        );
        CREATE INDEX IX_DM_OperationLog_UserID ON DM_OperationLog(UserID);
        CREATE INDEX IX_DM_OperationLog_CreateTime ON DM_OperationLog(CreateTime);
        CREATE INDEX IX_DM_OperationLog_OperationType ON DM_OperationLog(OperationType);";

    private static string GetSystemConfigTableSql() => @"
        CREATE TABLE DM_SystemConfig (
            ConfigID INT IDENTITY(1,1) PRIMARY KEY,
            ConfigKey NVARCHAR(100) NOT NULL UNIQUE,
            ConfigValue NVARCHAR(500),
            Description NVARCHAR(200),
            CreateTime DATETIME2 DEFAULT GETDATE(),
            UpdateTime DATETIME2
        );
        CREATE INDEX IX_DM_SystemConfig_ConfigKey ON DM_SystemConfig(ConfigKey);";

    private static string GetDatabaseVersionTableSql() => @"
        CREATE TABLE DM_DatabaseVersion (
            VersionID INT IDENTITY(1,1) PRIMARY KEY,
            VersionNumber NVARCHAR(20) NOT NULL,
            ScriptName NVARCHAR(200),
            AppliedDate DATETIME2 DEFAULT GETDATE(),
            AppliedBy NVARCHAR(50),
            Description NVARCHAR(500)
        );
        CREATE INDEX IX_DM_DatabaseVersion_VersionNumber ON DM_DatabaseVersion(VersionNumber);";

    #endregion

    #region 索引创建

    private static void CreateIndexes(SqlConnection connection)
    {
        // 复合索引优化常用查询
        var indexes = new[]
        {
            // 刀模信息复合索引
            ("DM_DieInfo", "IX_DM_DieInfo_Status_CreateTime", "Status, CreateTime DESC"),
            ("DM_DieInfo", "IX_DM_DieInfo_Customer_CreateTime", "CustomerName, CreateTime DESC"),

            // 工序复合索引
            ("DM_DieProcess", "IX_DM_DieProcess_DieID_Status", "DieID, Status"),

            // 库存复合索引
            ("DM_DieInventory", "IX_DM_DieInventory_Status_Location", "StorageStatus, LocationID"),

            // 借用记录复合索引
            ("DM_DieBorrowRecord", "IX_DM_DieBorrowRecord_Status_Time", "Status, BorrowTime DESC"),

            // 库位复合索引
            ("DM_StorageLocation", "IX_DM_StorageLocation_Area_Shelf", "Area, ShelfNo, LayerNo, PositionNo")
        };

        foreach (var (table, indexName, columns) in indexes)
        {
            try
            {
                var checkSql = $@"
                    SELECT COUNT(*) FROM sys.indexes 
                    WHERE name = @IndexName AND object_id = OBJECT_ID(@TableName)";

                using var checkCommand = new SqlCommand(checkSql, connection);
                checkCommand.Parameters.AddWithValue("@IndexName", indexName);
                checkCommand.Parameters.AddWithValue("@TableName", table);
                var exists = (int)checkCommand.ExecuteScalar()! > 0;

                if (!exists)
                {
                    var createSql = $@"CREATE INDEX {indexName} ON {table}({columns})";
                    using var command = new SqlCommand(createSql, connection);
                    command.ExecuteNonQuery();
                }
            }
            catch
            {
                // 忽略索引创建错误
            }
        }
    }

    private static async Task CreateIndexesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        // 复合索引优化常用查询
        var indexes = new[]
        {
            // 刀模信息复合索引
            ("DM_DieInfo", "IX_DM_DieInfo_Status_CreateTime", "Status, CreateTime DESC"),
            ("DM_DieInfo", "IX_DM_DieInfo_Customer_CreateTime", "CustomerName, CreateTime DESC"),

            // 工序复合索引
            ("DM_DieProcess", "IX_DM_DieProcess_DieID_Status", "DieID, Status"),

            // 库存复合索引
            ("DM_DieInventory", "IX_DM_DieInventory_Status_Location", "StorageStatus, LocationID"),

            // 借用记录复合索引
            ("DM_DieBorrowRecord", "IX_DM_DieBorrowRecord_Status_Time", "Status, BorrowTime DESC"),

            // 库位复合索引
            ("DM_StorageLocation", "IX_DM_StorageLocation_Area_Shelf", "Area, ShelfNo, LayerNo, PositionNo")
        };

        foreach (var (table, indexName, columns) in indexes)
        {
            try
            {
                var checkSql = $@"
                    SELECT COUNT(*) FROM sys.indexes 
                    WHERE name = @IndexName AND object_id = OBJECT_ID(@TableName)";

                using var checkCommand = new SqlCommand(checkSql, connection);
                checkCommand.Parameters.AddWithValue("@IndexName", indexName);
                checkCommand.Parameters.AddWithValue("@TableName", table);
                var exists = (int)(await checkCommand.ExecuteScalarAsync(cancellationToken))! > 0;

                if (!exists)
                {
                    var createSql = $@"CREATE INDEX {indexName} ON {table}({columns})";
                    using var command = new SqlCommand(createSql, connection);
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            catch
            {
                // 忽略索引创建错误
            }
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 获取数据库名称
    /// </summary>
    private static string GetDatabaseName()
    {
        var builder = new SqlConnectionStringBuilder(DbHelper.ConnectionString);
        return builder.InitialCatalog;
    }

    /// <summary>
    /// 密码哈希（SHA256）
    /// </summary>
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    #endregion
}

#region 结果类

/// <summary>
/// 初始化结果
/// </summary>
public class InitializationResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>是否创建了数据库</summary>
    public bool DatabaseCreated { get; set; }

    /// <summary>创建的表数量</summary>
    public int TablesCreated { get; set; }

    /// <summary>是否初始化了数据</summary>
    public bool DataInitialized { get; set; }

    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>操作消息</summary>
    public List<string> Messages { get; set; } = new();
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

/// <summary>
/// 表检查结果
/// </summary>
public class TableEnsureResult
{
    /// <summary>创建的表数量</summary>
    public int TablesCreated { get; set; }

    /// <summary>消息列表</summary>
    public List<string> Messages { get; set; } = new();
}

/// <summary>
/// 数据检查结果
/// </summary>
public class DataEnsureResult
{
    /// <summary>是否插入了数据</summary>
    public bool DataInserted { get; set; }

    /// <summary>消息列表</summary>
    public List<string> Messages { get; set; } = new();
}

#endregion
