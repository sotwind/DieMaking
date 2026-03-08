using Microsoft.Data.SqlClient;

namespace DieMaking.Helpers;

/// <summary>
/// 索引创建器 - 负责数据库索引创建
/// </summary>
public static class IndexCreator
{
    /// <summary>
    /// 创建索引
    /// </summary>
    public static void CreateIndexes(SqlConnection connection)
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
            ("DM_DieBorrowRecord", "IX_DM_DieBorrowRecord_DieID_Status", "DieID, Status"),
            ("DM_DieBorrowRecord", "IX_DM_DieBorrowRecord_Borrower", "BorrowerNo, BorrowTime DESC"),

            // 完工记录复合索引
            ("DM_DieCompletion", "IX_DM_DieCompletion_DieID_Time", "DieID, CompleteTime DESC"),
            ("DM_DieCompletion", "IX_DM_DieCompletion_Operator", "OperatorNo, CompleteTime DESC"),

            // 报废记录复合索引
            ("DM_DieScrapRecord", "IX_DM_DieScrapRecord_Status_Time", "AuditStatus, ApplyTime DESC"),

            // 操作日志复合索引
            ("DM_OperationLog", "IX_DM_OperationLog_User_Time", "UserID, CreateTime DESC"),
            ("DM_OperationLog", "IX_DM_OperationLog_Type_Time", "OperationType, CreateTime DESC")
        };

        foreach (var (table, indexName, columns) in indexes)
        {
            CreateIndexIfNotExists(connection, table, indexName, columns);
        }
    }

    /// <summary>
    /// 异步创建索引
    /// </summary>
    public static async Task CreateIndexesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var indexes = new[]
        {
            ("DM_DieInfo", "IX_DM_DieInfo_Status_CreateTime", "Status, CreateTime DESC"),
            ("DM_DieInfo", "IX_DM_DieInfo_Customer_CreateTime", "CustomerName, CreateTime DESC"),
            ("DM_DieProcess", "IX_DM_DieProcess_DieID_Status", "DieID, Status"),
            ("DM_DieInventory", "IX_DM_DieInventory_Status_Location", "StorageStatus, LocationID"),
            ("DM_DieBorrowRecord", "IX_DM_DieBorrowRecord_DieID_Status", "DieID, Status"),
            ("DM_DieBorrowRecord", "IX_DM_DieBorrowRecord_Borrower", "BorrowerNo, BorrowTime DESC"),
            ("DM_DieCompletion", "IX_DM_DieCompletion_DieID_Time", "DieID, CompleteTime DESC"),
            ("DM_DieCompletion", "IX_DM_DieCompletion_Operator", "OperatorNo, CompleteTime DESC"),
            ("DM_DieScrapRecord", "IX_DM_DieScrapRecord_Status_Time", "AuditStatus, ApplyTime DESC"),
            ("DM_OperationLog", "IX_DM_OperationLog_User_Time", "UserID, CreateTime DESC"),
            ("DM_OperationLog", "IX_DM_OperationLog_Type_Time", "OperationType, CreateTime DESC")
        };

        foreach (var (table, indexName, columns) in indexes)
        {
            await CreateIndexIfNotExistsAsync(connection, table, indexName, columns, cancellationToken);
        }
    }

    /// <summary>
    /// 如果索引不存在则创建
    /// </summary>
    private static void CreateIndexIfNotExists(SqlConnection connection, string tableName, string indexName, string columns)
    {
        var checkSql = @"
            SELECT COUNT(*) FROM sys.indexes 
            WHERE name = @IndexName AND object_id = OBJECT_ID(@TableName)";

        using var checkCommand = new SqlCommand(checkSql, connection);
        checkCommand.Parameters.AddWithValue("@IndexName", indexName);
        checkCommand.Parameters.AddWithValue("@TableName", tableName);
        var exists = (int)checkCommand.ExecuteScalar()! > 0;

        if (exists) return;

        var createSql = $"CREATE INDEX {indexName} ON {tableName}({columns})";
        using var command = new SqlCommand(createSql, connection);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 异步如果索引不存在则创建
    /// </summary>
    private static async Task CreateIndexIfNotExistsAsync(SqlConnection connection, string tableName, string indexName, string columns, CancellationToken cancellationToken)
    {
        var checkSql = @"
            SELECT COUNT(*) FROM sys.indexes 
            WHERE name = @IndexName AND object_id = OBJECT_ID(@TableName)";

        using var checkCommand = new SqlCommand(checkSql, connection);
        checkCommand.Parameters.AddWithValue("@IndexName", indexName);
        checkCommand.Parameters.AddWithValue("@TableName", tableName);
        var exists = (int)await checkCommand.ExecuteScalarAsync(cancellationToken)! > 0;

        if (exists) return;

        var createSql = $"CREATE INDEX {indexName} ON {tableName}({columns})";
        using var command = new SqlCommand(createSql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
