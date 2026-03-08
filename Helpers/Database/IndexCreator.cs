using Microsoft.Data.SqlClient;

namespace DieMaking.Helpers.Database;

/// <summary>
/// 索引创建器 - 负责创建数据库索引
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

    /// <summary>
    /// 异步创建索引
    /// </summary>
    public static async Task CreateIndexesAsync(SqlConnection connection, CancellationToken cancellationToken)
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
}
