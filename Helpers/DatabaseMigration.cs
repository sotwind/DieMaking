using Microsoft.Data.SqlClient;
using System.Reflection;

namespace DieMaking.Helpers;

/// <summary>
/// 数据库迁移管理器 - 支持数据库版本管理和升级脚本执行
/// </summary>
public static class DatabaseMigration
{
    /// <summary>
    /// 当前数据库版本
    /// </summary>
    public static readonly string CurrentVersion = "1.0.0";

    /// <summary>
    /// 数据库版本表名
    /// </summary>
    private const string VersionTableName = "DM_DatabaseVersion";

    /// <summary>
    /// 迁移历史记录
    /// </summary>
    private static readonly List<MigrationRecord> _migrationHistory = new();

    /// <summary>
    /// 迁移脚本字典
    /// </summary>
    private static readonly Dictionary<string, MigrationScript> _migrationScripts = new();

    static DatabaseMigration()
    {
        InitializeMigrationScripts();
    }

    #region 版本管理

    /// <summary>
    /// 获取当前数据库版本
    /// </summary>
    public static string GetDatabaseVersion()
    {
        try
        {
            using var connection = DbHelper.CreateConnection();
            connection.Open();

            // 检查版本表是否存在
            if (!TableExists(connection, VersionTableName))
            {
                return "0.0.0";
            }

            // 获取最新版本
            var sql = $@"
                SELECT TOP 1 VersionNumber 
                FROM {VersionTableName} 
                ORDER BY AppliedDate DESC, VersionID DESC";

            using var command = new SqlCommand(sql, connection);
            var result = command.ExecuteScalar();

            return result?.ToString() ?? "0.0.0";
        }
        catch
        {
            return "0.0.0";
        }
    }

    /// <summary>
    /// 异步获取当前数据库版本
    /// </summary>
    public static async Task<string> GetDatabaseVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = DbHelper.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            // 检查版本表是否存在
            if (!await TableExistsAsync(connection, VersionTableName, cancellationToken))
            {
                return "0.0.0";
            }

            // 获取最新版本
            var sql = $@"
                SELECT TOP 1 VersionNumber 
                FROM {VersionTableName} 
                ORDER BY AppliedDate DESC, VersionID DESC";

            using var command = new SqlCommand(sql, connection);
            var result = await command.ExecuteScalarAsync(cancellationToken);

            return result?.ToString() ?? "0.0.0";
        }
        catch
        {
            return "0.0.0";
        }
    }

    /// <summary>
    /// 检查是否需要升级
    /// </summary>
    public static bool NeedsUpgrade()
    {
        var dbVersion = GetDatabaseVersion();
        return CompareVersions(dbVersion, CurrentVersion) < 0;
    }

    /// <summary>
    /// 执行数据库升级
    /// </summary>
    public static MigrationResult Upgrade()
    {
        var result = new MigrationResult();
        var currentVersion = GetDatabaseVersion();

        try
        {
            // 确保版本表存在
            EnsureVersionTable();

            // 获取需要执行的迁移脚本
            var pendingMigrations = GetPendingMigrations(currentVersion);

            if (pendingMigrations.Count == 0)
            {
                result.Success = true;
                result.Message = "数据库已是最新版本";
                result.CurrentVersion = currentVersion;
                return result;
            }

            using var connection = DbHelper.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var migration in pendingMigrations)
                {
                    // 执行迁移脚本
                    ExecuteMigrationScript(connection, transaction, migration);

                    // 记录版本
                    RecordVersion(connection, transaction, migration);

                    result.AppliedMigrations.Add(migration.Version);
                    result.Messages.Add($"已应用迁移: {migration.Version} - {migration.Description}");
                }

                transaction.Commit();
                result.Success = true;
                result.CurrentVersion = pendingMigrations.Last().Version;
                result.Message = $"数据库升级成功，从 {currentVersion} 升级到 {result.CurrentVersion}";
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                result.Success = false;
                result.ErrorMessage = $"迁移失败: {ex.Message}";
                result.Messages.Add($"回滚到版本: {currentVersion}");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"升级过程出错: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 异步执行数据库升级
    /// </summary>
    public static async Task<MigrationResult> UpgradeAsync(CancellationToken cancellationToken = default)
    {
        var result = new MigrationResult();
        var currentVersion = await GetDatabaseVersionAsync(cancellationToken);

        try
        {
            // 确保版本表存在
            await EnsureVersionTableAsync(cancellationToken);

            // 获取需要执行的迁移脚本
            var pendingMigrations = GetPendingMigrations(currentVersion);

            if (pendingMigrations.Count == 0)
            {
                result.Success = true;
                result.Message = "数据库已是最新版本";
                result.CurrentVersion = currentVersion;
                return result;
            }

            using var connection = DbHelper.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var migration in pendingMigrations)
                {
                    // 执行迁移脚本
                    await ExecuteMigrationScriptAsync(connection, transaction, migration, cancellationToken);

                    // 记录版本
                    await RecordVersionAsync(connection, transaction, migration, cancellationToken);

                    result.AppliedMigrations.Add(migration.Version);
                    result.Messages.Add($"已应用迁移: {migration.Version} - {migration.Description}");
                }

                await transaction.CommitAsync(cancellationToken);
                result.Success = true;
                result.CurrentVersion = pendingMigrations.Last().Version;
                result.Message = $"数据库升级成功，从 {currentVersion} 升级到 {result.CurrentVersion}"";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                result.Success = false;
                result.ErrorMessage = $"迁移失败: {ex.Message}";
                result.Messages.Add($"回滚到版本: {currentVersion}");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"升级过程出错: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 获取迁移历史
    /// </summary>
    public static List<MigrationRecord> GetMigrationHistory()
    {
        var history = new List<MigrationRecord>();

        try
        {
            using var connection = DbHelper.CreateConnection();
            connection.Open();

            if (!TableExists(connection, VersionTableName))
            {
                return history;
            }

            var sql = $@"
                SELECT VersionNumber, ScriptName, AppliedDate, AppliedBy, Description
                FROM {VersionTableName}
                ORDER BY AppliedDate ASC";

            using var command = new SqlCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                history.Add(new MigrationRecord
                {
                    Version = reader["VersionNumber"].ToString()!,
                    ScriptName = reader["ScriptName"].ToString(),
                    AppliedDate = Convert.ToDateTime(reader["AppliedDate"]),
                    AppliedBy = reader["AppliedBy"].ToString(),
                    Description = reader["Description"].ToString()
                });
            }
        }
        catch
        {
            // 忽略错误
        }

        return history;
    }

    /// <summary>
    /// 异步获取迁移历史
    /// </summary>
    public static async Task<List<MigrationRecord>> GetMigrationHistoryAsync(CancellationToken cancellationToken = default)
    {
        var history = new List<MigrationRecord>();

        try
        {
            using var connection = DbHelper.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            if (!await TableExistsAsync(connection, VersionTableName, cancellationToken))
            {
                return history;
            }

            var sql = $@"
                SELECT VersionNumber, ScriptName, AppliedDate, AppliedBy, Description
                FROM {VersionTableName}
                ORDER BY AppliedDate ASC";

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                history.Add(new MigrationRecord
                {
                    Version = reader["VersionNumber"].ToString()!,
                    ScriptName = reader["ScriptName"].ToString(),
                    AppliedDate = Convert.ToDateTime(reader["AppliedDate"]),
                    AppliedBy = reader["AppliedBy"].ToString(),
                    Description = reader["Description"].ToString()
                });
            }
        }
        catch
        {
            // 忽略错误
        }

        return history;
    }

    #endregion

    #region 迁移脚本管理

    /// <summary>
    /// 注册迁移脚本
    /// </summary>
    public static void RegisterMigration(string version, string description, string sqlScript)
    {
        _migrationScripts[version] = new MigrationScript
        {
            Version = version,
            Description = description,
            SqlScript = sqlScript
        };
    }

    /// <summary>
    /// 注册迁移脚本（带回调）
    /// </summary>
    public static void RegisterMigration(string version, string description, Func<SqlConnection, SqlTransaction, bool> action)
    {
        _migrationScripts[version] = new MigrationScript
        {
            Version = version,
            Description = description,
            Action = action
        };
    }

    /// <summary>
    /// 初始化内置迁移脚本
    /// </summary>
    private static void InitializeMigrationScripts()
    {
        // 版本 1.0.0 - 初始版本（创建基础表结构）
        RegisterMigration("1.0.0", "初始版本 - 创建基础表结构", @"
            -- 基础表结构已在 DatabaseInitializer 中创建
            -- 此迁移仅用于标记初始版本
            SELECT 1;
        ");

        // 版本 1.0.1 - 添加性能监控相关表
        RegisterMigration("1.0.1", "添加性能监控表", @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DM_SlowQueryLog')
            BEGIN
                CREATE TABLE DM_SlowQueryLog (
                    LogID INT IDENTITY(1,1) PRIMARY KEY,
                    SqlText NVARCHAR(MAX),
                    ExecutionTimeMs BIGINT,
                    Parameters NVARCHAR(MAX),
                    CreateTime DATETIME2 DEFAULT GETDATE()
                );
                CREATE INDEX IX_DM_SlowQueryLog_CreateTime ON DM_SlowQueryLog(CreateTime);
            END
        ");

        // 版本 1.0.2 - 添加数据备份记录表
        RegisterMigration("1.0.2", "添加数据备份记录表", @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DM_BackupRecord')
            BEGIN
                CREATE TABLE DM_BackupRecord (
                    BackupID INT IDENTITY(1,1) PRIMARY KEY,
                    BackupFileName NVARCHAR(500) NOT NULL,
                    BackupPath NVARCHAR(500) NOT NULL,
                    BackupSize BIGINT,
                    BackupType INT DEFAULT 0,
                    StartTime DATETIME2 DEFAULT GETDATE(),
                    EndTime DATETIME2,
                    Status INT DEFAULT 0,
                    ErrorMessage NVARCHAR(MAX),
                    CreatedBy NVARCHAR(50),
                    Remark NVARCHAR(500)
                );
                CREATE INDEX IX_DM_BackupRecord_CreateTime ON DM_BackupRecord(StartTime);
                CREATE INDEX IX_DM_BackupRecord_Status ON DM_BackupRecord(Status);
            END
        ");

        // 版本 1.0.3 - 优化索引
        RegisterMigration("1.0.3", "优化常用查询索引", @"
            -- 刀模信息表额外索引
            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DM_DieInfo_DeliveryDate')
                CREATE INDEX IX_DM_DieInfo_DeliveryDate ON DM_DieInfo(DeliveryDate);

            -- 工序表额外索引
            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DM_DieProcess_CompleteTime')
                CREATE INDEX IX_DM_DieProcess_CompleteTime ON DM_DieProcess(CompleteTime);

            -- 借用记录表额外索引
            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DM_DieBorrowRecord_BorrowerNo')
                CREATE INDEX IX_DM_DieBorrowRecord_BorrowerNo ON DM_DieBorrowRecord(BorrowerNo);
        ");
    }

    /// <summary>
    /// 获取待执行的迁移
    /// </summary>
    private static List<MigrationScript> GetPendingMigrations(string currentVersion)
    {
        return _migrationScripts
            .Where(m => CompareVersions(currentVersion, m.Key) < 0)
            .OrderBy(m => m.Key, new VersionComparer())
            .Select(m => m.Value)
            .ToList();
    }

    /// <summary>
    /// 执行迁移脚本
    /// </summary>
    private static void ExecuteMigrationScript(SqlConnection connection, SqlTransaction transaction, MigrationScript migration)
    {
        if (!string.IsNullOrEmpty(migration.SqlScript))
        {
            using var command = new SqlCommand(migration.SqlScript, connection, transaction);
            command.CommandTimeout = 300; // 5分钟超时
            command.ExecuteNonQuery();
        }

        if (migration.Action != null)
        {
            var success = migration.Action(connection, transaction);
            if (!success)
            {
                throw new Exception($"迁移脚本 {migration.Version} 执行失败");
            }
        }
    }

    /// <summary>
    /// 异步执行迁移脚本
    /// </summary>
    private static async Task ExecuteMigrationScriptAsync(SqlConnection connection, SqlTransaction transaction,
        MigrationScript migration, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(migration.SqlScript))
        {
            using var command = new SqlCommand(migration.SqlScript, connection, transaction);
            command.CommandTimeout = 300; // 5分钟超时
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        if (migration.Action != null)
        {
            var success = migration.Action(connection, transaction);
            if (!success)
            {
                throw new Exception($"迁移脚本 {migration.Version} 执行失败");
            }
        }
    }

    /// <summary>
    /// 记录版本
    /// </summary>
    private static void RecordVersion(SqlConnection connection, SqlTransaction transaction, MigrationScript migration)
    {
        var sql = $@"
            INSERT INTO {VersionTableName} (VersionNumber, ScriptName, AppliedDate, AppliedBy, Description)
            VALUES (@VersionNumber, @ScriptName, GETDATE(), @AppliedBy, @Description)";

        using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@VersionNumber", migration.Version);
        command.Parameters.AddWithValue("@ScriptName", $"Migration_{migration.Version}");
        command.Parameters.AddWithValue("@AppliedBy", Environment.UserName);
        command.Parameters.AddWithValue("@Description", migration.Description);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 异步记录版本
    /// </summary>
    private static async Task RecordVersionAsync(SqlConnection connection, SqlTransaction transaction,
        MigrationScript migration, CancellationToken cancellationToken)
    {
        var sql = $@"
            INSERT INTO {VersionTableName} (VersionNumber, ScriptName, AppliedDate, AppliedBy, Description)
            VALUES (@VersionNumber, @ScriptName, GETDATE(), @AppliedBy, @Description)";

        using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@VersionNumber", migration.Version);
        command.Parameters.AddWithValue("@ScriptName", $"Migration_{migration.Version}");
        command.Parameters.AddWithValue("@AppliedBy", Environment.UserName);
        command.Parameters.AddWithValue("@Description", migration.Description);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 确保版本表存在
    /// </summary>
    private static void EnsureVersionTable()
    {
        using var connection = DbHelper.CreateConnection();
        connection.Open();

        if (!TableExists(connection, VersionTableName))
        {
            var sql = $@"
                CREATE TABLE {VersionTableName} (
                    VersionID INT IDENTITY(1,1) PRIMARY KEY,
                    VersionNumber NVARCHAR(20) NOT NULL,
                    ScriptName NVARCHAR(200),
                    AppliedDate DATETIME2 DEFAULT GETDATE(),
                    AppliedBy NVARCHAR(50),
                    Description NVARCHAR(500)
                );
                CREATE INDEX IX_{VersionTableName}_VersionNumber ON {VersionTableName}(VersionNumber);";

            using var command = new SqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 异步确保版本表存在
    /// </summary>
    private static async Task EnsureVersionTableAsync(CancellationToken cancellationToken)
    {
        using var connection = DbHelper.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, VersionTableName, cancellationToken))
        {
            var sql = $@"
                CREATE TABLE {VersionTableName} (
                    VersionID INT IDENTITY(1,1) PRIMARY KEY,
                    VersionNumber NVARCHAR(20) NOT NULL,
                    ScriptName NVARCHAR(200),
                    AppliedDate DATETIME2 DEFAULT GETDATE(),
                    AppliedBy NVARCHAR(50),
                    Description NVARCHAR(500)
                );
                CREATE INDEX IX_{VersionTableName}_VersionNumber ON {VersionTableName}(VersionNumber);";

            using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 检查表是否存在
    /// </summary>
    private static bool TableExists(SqlConnection connection, string tableName)
    {
        var sql = "SELECT COUNT(*) FROM sys.tables WHERE name = @TableName";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TableName", tableName);
        return (int)command.ExecuteScalar()! > 0;
    }

    /// <summary>
    /// 异步检查表是否存在
    /// </summary>
    private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        var sql = "SELECT COUNT(*) FROM sys.tables WHERE name = @TableName";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TableName", tableName);
        return (int)(await command.ExecuteScalarAsync(cancellationToken))! > 0;
    }

    /// <summary>
    /// 比较版本号
    /// </summary>
    /// <returns>负数: v1 < v2, 0: v1 = v2, 正数: v1 > v2</returns>
    private static int CompareVersions(string v1, string v2)
    {
        var parts1 = v1.Split('.').Select(int.Parse).ToArray();
        var parts2 = v2.Split('.').Select(int.Parse).ToArray();

        var maxLength = Math.Max(parts1.Length, parts2.Length);

        for (int i = 0; i < maxLength; i++)
        {
            var p1 = i < parts1.Length ? parts1[i] : 0;
            var p2 = i < parts2.Length ? parts2[i] : 0;

            if (p1 != p2)
            {
                return p1 - p2;
            }
        }

        return 0;
    }

    #endregion
}

#region 数据模型

/// <summary>
/// 迁移脚本
/// </summary>
public class MigrationScript
{
    /// <summary>版本号</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>描述</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>SQL脚本</summary>
    public string? SqlScript { get; set; }

    /// <summary>回调操作</summary>
    public Func<SqlConnection, SqlTransaction, bool>? Action { get; set; }
}

/// <summary>
/// 迁移记录
/// </summary>
public class MigrationRecord
{
    /// <summary>版本号</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>脚本名称</summary>
    public string? ScriptName { get; set; }

    /// <summary>应用日期</summary>
    public DateTime AppliedDate { get; set; }

    /// <summary>执行人</summary>
    public string? AppliedBy { get; set; }

    /// <summary>描述</summary>
    public string? Description { get; set; }
}

/// <summary>
/// 迁移结果
/// </summary>
public class MigrationResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>消息</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>当前版本</summary>
    public string CurrentVersion { get; set; } = string.Empty;

    /// <summary>已应用的迁移</summary>
    public List<string> AppliedMigrations { get; set; } = new();

    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>详细消息</summary>
    public List<string> Messages { get; set; } = new();
}

/// <summary>
/// 版本比较器
/// </summary>
public class VersionComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        var parts1 = x.Split('.').Select(int.Parse).ToArray();
        var parts2 = y.Split('.').Select(int.Parse).ToArray();

        var maxLength = Math.Max(parts1.Length, parts2.Length);

        for (int i = 0; i < maxLength; i++)
        {
            var p1 = i < parts1.Length ? parts1[i] : 0;
            var p2 = i < parts2.Length ? parts2[i] : 0;

            if (p1 != p2)
            {
                return p1 - p2;
            }
        }

        return 0;
    }
}

#endregion
