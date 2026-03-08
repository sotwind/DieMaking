namespace DieMaking.Helpers;

/// <summary>
/// 数据库初始化器 - 负责数据库、表结构创建和初始数据导入
/// 重构后：协调各个专门的创建器完成初始化工作
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// 初始化数据库（检查并创建数据库、表、初始数据）
    /// </summary>
    public static InitializationResult Initialize()
    {
        var result = new InitializationResult();

        try
        {
            // 1. 检查并创建数据库
            var dbResult = DatabaseCreator.EnsureDatabaseExists();
            result.DatabaseCreated = dbResult.Created;
            result.Messages.Add(dbResult.Message);

            // 2. 检查并创建表结构
            var tableResult = TableCreator.EnsureTablesExist();
            result.TablesCreated = tableResult.TablesCreated;
            result.Messages.AddRange(tableResult.Messages);

            // 3. 创建索引
            using (var connection = DbHelper.CreateConnection())
            {
                connection.Open();
                IndexCreator.CreateIndexes(connection);
            }
            result.Messages.Add("创建数据库索引");

            // 4. 检查并插入初始数据
            var dataResult = DataSeeder.EnsureInitialData();
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
            var dbResult = await DatabaseCreator.EnsureDatabaseExistsAsync(cancellationToken);
            result.DatabaseCreated = dbResult.Created;
            result.Messages.Add(dbResult.Message);

            // 2. 检查并创建表结构
            var tableResult = await TableCreator.EnsureTablesExistAsync(cancellationToken);
            result.TablesCreated = tableResult.TablesCreated;
            result.Messages.AddRange(tableResult.Messages);

            // 3. 创建索引
            using (var connection = DbHelper.CreateConnection())
            {
                await connection.OpenAsync(cancellationToken);
                await IndexCreator.CreateIndexesAsync(connection, cancellationToken);
            }
            result.Messages.Add("创建数据库索引");

            // 4. 检查并插入初始数据
            var dataResult = await DataSeeder.EnsureInitialDataAsync(cancellationToken);
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
    /// 密码哈希（委托给 DataSeeder）
    /// </summary>
    public static string HashPassword(string password) => DataSeeder.HashPassword(password);
}

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
