using Microsoft.Data.SqlClient;

namespace DieMaking.Helpers.Database;

/// <summary>
/// 表创建器 - 负责创建数据库表结构
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

            // 创建用户偏好设置表
            if (CreateTableIfNotExists(connection, "DM_UserPreference", GetUserPreferenceTableSql()))
            {
                tablesCreated++;
                messages.Add("创建表: DM_UserPreference");
            }

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
    public static async Task<TableEnsureResult> EnsureTablesExistAsync(CancellationToken cancellationToken)
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

            // 创建用户偏好设置表
            if (await CreateTableIfNotExistsAsync(connection, "DM_UserPreference", GetUserPreferenceTableSql(), cancellationToken))
            {
                tablesCreated++;
                messages.Add("创建表: DM_UserPreference");
            }

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

    #region 表结构SQL

    public static string GetUserTableSql() => @"
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

    public static string GetDieInfoTableSql() => @"
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

    public static string GetDieProcessTableSql() => @"
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

    public static string GetDieCompletionTableSql() => @"
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

    public static string GetDieInventoryTableSql() => @"
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

    public static string GetStorageLocationTableSql() => @"
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

    public static string GetDieBorrowRecordTableSql() => @"
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

    public static string GetDieScrapRecordTableSql() => @"
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

    public static string GetOperationLogTableSql() => @"
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

    public static string GetSystemConfigTableSql() => @"
        CREATE TABLE DM_SystemConfig (
            ConfigID INT IDENTITY(1,1) PRIMARY KEY,
            ConfigKey NVARCHAR(100) NOT NULL UNIQUE,
            ConfigValue NVARCHAR(500),
            Description NVARCHAR(200),
            CreateTime DATETIME2 DEFAULT GETDATE(),
            UpdateTime DATETIME2
        );
        CREATE INDEX IX_DM_SystemConfig_ConfigKey ON DM_SystemConfig(ConfigKey);";

    public static string GetDatabaseVersionTableSql() => @"
        CREATE TABLE DM_DatabaseVersion (
            VersionID INT IDENTITY(1,1) PRIMARY KEY,
            VersionNumber NVARCHAR(20) NOT NULL,
            ScriptName NVARCHAR(200),
            AppliedDate DATETIME2 DEFAULT GETDATE(),
            AppliedBy NVARCHAR(50),
            Description NVARCHAR(500)
        );
        CREATE INDEX IX_DM_DatabaseVersion_VersionNumber ON DM_DatabaseVersion(VersionNumber);";

    public static string GetUserPreferenceTableSql() => @"
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

    #endregion
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
