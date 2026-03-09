using Microsoft.Data.SqlClient;

namespace DieMaking.Helpers;

/// <summary>
/// 表创建器 - 负责数据库表结构创建
/// </summary>
public static class TableCreator
{
    /// <summary>
    /// 确保表结构存在
    /// </summary>
    public static TableEnsureResult EnsureTablesExist()
    {
        var result = new TableEnsureResult();
        var messages = new List<string>();
        var tablesCreated = 0;

        try
        {
            using var connection = DbHelper.CreateConnection();
            connection.Open();

            // 创建所有表
            var tables = new (string name, string sql)[]
            {
                ("DM_User", GetUserTableSql()),
                ("DM_DieInfo", GetDieInfoTableSql()),
                ("DM_DieProcess", GetDieProcessTableSql()),
                ("DM_DieCompletion", GetDieCompletionTableSql()),
                ("DM_DieInventory", GetDieInventoryTableSql()),
                ("DM_StorageLocation", GetStorageLocationTableSql()),
                ("DM_DieBorrowRecord", GetDieBorrowRecordTableSql()),
                ("DM_DieScrapRecord", GetDieScrapRecordTableSql()),
                ("DM_OperationLog", GetOperationLogTableSql()),
                ("DM_SystemConfig", GetSystemConfigTableSql()),
                ("DM_DatabaseVersion", GetDatabaseVersionTableSql()),
                ("DM_UserPreference", GetUserPreferenceTableSql())
            };

            foreach (var (name, sql) in tables)
            {
                if (CreateTableIfNotExists(connection, name, sql))
                {
                    tablesCreated++;
                    messages.Add($"创建表: {name}");
                }
            }

            result.TablesCreated = tablesCreated;
            result.Messages = messages;
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"创建表结构失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 异步确保表结构存在
    /// </summary>
    public static async Task<TableEnsureResult> EnsureTablesExistAsync(CancellationToken cancellationToken = default)
    {
        var result = new TableEnsureResult();
        var messages = new List<string>();
        var tablesCreated = 0;

        try
        {
            using var connection = DbHelper.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var tables = new (string name, string sql)[]
            {
                ("DM_User", GetUserTableSql()),
                ("DM_DieInfo", GetDieInfoTableSql()),
                ("DM_DieProcess", GetDieProcessTableSql()),
                ("DM_DieCompletion", GetDieCompletionTableSql()),
                ("DM_DieInventory", GetDieInventoryTableSql()),
                ("DM_StorageLocation", GetStorageLocationTableSql()),
                ("DM_DieBorrowRecord", GetDieBorrowRecordTableSql()),
                ("DM_DieScrapRecord", GetDieScrapRecordTableSql()),
                ("DM_OperationLog", GetOperationLogTableSql()),
                ("DM_SystemConfig", GetSystemConfigTableSql()),
                ("DM_DatabaseVersion", GetDatabaseVersionTableSql()),
                ("DM_UserPreference", GetUserPreferenceTableSql())
            };

            foreach (var (name, sql) in tables)
            {
                if (await CreateTableIfNotExistsAsync(connection, name, sql, cancellationToken))
                {
                    tablesCreated++;
                    messages.Add($"创建表: {name}");
                }
            }

            result.TablesCreated = tablesCreated;
            result.Messages = messages;
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"创建表结构失败: {ex.Message}", ex);
        }
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

        if (exists) return false;

        using var command = new SqlCommand(createSql, connection);
        command.ExecuteNonQuery();
        return true;
    }

    /// <summary>
    /// 异步如果表不存在则创建
    /// </summary>
    private static async Task<bool> CreateTableIfNotExistsAsync(SqlConnection connection, string tableName, string createSql, CancellationToken cancellationToken)
    {
        var checkSql = "SELECT COUNT(*) FROM sys.tables WHERE name = @TableName";
        using var checkCommand = new SqlCommand(checkSql, connection);
        checkCommand.Parameters.AddWithValue("@TableName", tableName);
        var exists = (int)await checkCommand.ExecuteScalarAsync(cancellationToken)! > 0;

        if (exists) return false;

        using var command = new SqlCommand(createSql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    // 表结构SQL（从原DatabaseInitializer.cs迁移）
    private static string GetUserTableSql() => @"
        CREATE TABLE DM_User (
            UserID INT IDENTITY(1,1) PRIMARY KEY,
            Username NVARCHAR(50) NOT NULL UNIQUE,
            Password NVARCHAR(255) NOT NULL,
            RealName NVARCHAR(50) NOT NULL,
            Phone NVARCHAR(20),
            Email NVARCHAR(100),
            Role INT DEFAULT 0,
            Status INT DEFAULT 1,
            LastLoginTime DATETIME2,
            CreateTime DATETIME2 DEFAULT GETDATE()
        );
        CREATE INDEX IX_DM_User_Username ON DM_User(Username);
        CREATE INDEX IX_DM_User_Status ON DM_User(Status);";

    private static string GetDieInfoTableSql() => @"
        CREATE TABLE DM_DieInfo (
            DieID INT IDENTITY(1,1) PRIMARY KEY,
            DieCode NVARCHAR(50) NOT NULL UNIQUE,
            CustomerName NVARCHAR(100) NOT NULL,
            ProductName NVARCHAR(100) NOT NULL,
            ProductSpec NVARCHAR(100),
            Material NVARCHAR(50),
            SizeLength DECIMAL(10,2),
            SizeWidth DECIMAL(10,2),
            SizeHeight DECIMAL(10,2),
            RequiredProcesses INT DEFAULT 0,
            DeliveryDate DATE,
            Status INT DEFAULT 0,
            AuditStatus INT DEFAULT 0,
            CreateUser NVARCHAR(50),
            CreateTime DATETIME2 DEFAULT GETDATE(),
            UpdateTime DATETIME2
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
            ProcessOrder INT DEFAULT 0,
            Status INT DEFAULT 0,
            StartTime DATETIME2,
            CompleteTime DATETIME2,
            OperatorNo NVARCHAR(50),
            OperatorName NVARCHAR(50),
            Remark NVARCHAR(500),
            CreateTime DATETIME2 DEFAULT GETDATE(),
            FOREIGN KEY (DieID) REFERENCES DM_DieInfo(DieID) ON DELETE CASCADE
        );
        CREATE INDEX IX_DM_DieProcess_DieID ON DM_DieProcess(DieID);
        CREATE INDEX IX_DM_DieProcess_Status ON DM_DieProcess(Status);";

    private static string GetDieCompletionTableSql() => @"
        CREATE TABLE DM_DieCompletion (
            CompletionID INT IDENTITY(1,1) PRIMARY KEY,
            DieID INT NOT NULL,
            ProcessID INT,
            CompleteTime DATETIME2 DEFAULT GETDATE(),
            Quantity INT DEFAULT 1,
            TotalAmount DECIMAL(18,2),
            OperatorNo NVARCHAR(50),
            OperatorName NVARCHAR(50),
            Remark NVARCHAR(500),
            CreateTime DATETIME2 DEFAULT GETDATE(),
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
            UpdateTime DATETIME2,
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
        CREATE INDEX IX_DM_StorageLocation_Status ON DM_StorageLocation(Status);";

    private static string GetDieBorrowRecordTableSql() => @"
        CREATE TABLE DM_DieBorrowRecord (
            RecordID INT IDENTITY(1,1) PRIMARY KEY,
            DieID INT NOT NULL,
            InventoryID INT NOT NULL,
            BorrowType INT NOT NULL,
            BorrowerNo NVARCHAR(50) NOT NULL,
            BorrowerName NVARCHAR(50) NOT NULL,
            BorrowTime DATETIME2 DEFAULT GETDATE(),
            ExpectedReturnTime DATETIME2,
            ActualReturnTime DATETIME2,
            ReturnerNo NVARCHAR(50),
            ReturnerName NVARCHAR(50),
            Status INT DEFAULT 0,
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
            LogLevel NVARCHAR(20) DEFAULT 'Info',
            CreateTime DATETIME2 DEFAULT GETDATE()
        );
        CREATE INDEX IX_DM_OperationLog_UserID ON DM_OperationLog(UserID);
        CREATE INDEX IX_DM_OperationLog_CreateTime ON DM_OperationLog(CreateTime);
        CREATE INDEX IX_DM_OperationLog_OperationType ON DM_OperationLog(OperationType);
        CREATE INDEX IX_DM_OperationLog_LogLevel ON DM_OperationLog(LogLevel);";

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

    private static string GetUserPreferenceTableSql() => @"
        CREATE TABLE DM_UserPreference (
            PreferenceID INT IDENTITY(1,1) PRIMARY KEY,
            UserID INT NOT NULL UNIQUE,
            Theme NVARCHAR(20) DEFAULT 'Light',
            DefaultPageSize INT DEFAULT 20,
            DateFormat NVARCHAR(20) DEFAULT 'yyyy-MM-dd',
            TimeFormat NVARCHAR(20) DEFAULT 'HH:mm:ss',
            DefaultPage NVARCHAR(50) DEFAULT 'DieList',
            UpdateTime DATETIME2 DEFAULT GETDATE(),
            FOREIGN KEY (UserID) REFERENCES DM_User(UserID) ON DELETE CASCADE
        );
        CREATE INDEX IX_DM_UserPreference_UserID ON DM_UserPreference(UserID);";
}

/// <summary>
/// 表创建结果
/// </summary>
public class TableEnsureResult
{
    public int TablesCreated { get; set; }
    public List<string> Messages { get; set; } = new();
}
