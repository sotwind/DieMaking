# DatabaseInitializer.cs 拆分方案

## 原文件结构（1371行）

拆分为以下4个文件：

### 1. DatabaseCreator.cs（约200行）
负责数据库创建

```csharp
using Microsoft.Data.SqlClient;

namespace DieMaking.Helpers;

public static class DatabaseCreator
{
    private static readonly string _masterConnectionString;

    static DatabaseCreator()
    {
        var builder = new SqlConnectionStringBuilder(DbHelper.ConnectionString)
        {
            InitialCatalog = "master"
        };
        _masterConnectionString = builder.ConnectionString;
    }

    public static DatabaseEnsureResult EnsureDatabaseExists()
    {
        // 原 EnsureDatabaseExists 方法代码
    }

    private static string GetDatabaseName()
    {
        var builder = new SqlConnectionStringBuilder(DbHelper.ConnectionString);
        return builder.InitialCatalog;
    }
}

public class DatabaseEnsureResult
{
    public bool Created { get; set; }
    public string Message { get; set; } = "";
}
```

### 2. TableCreator.cs（约500行）
负责表结构创建

```csharp
using Microsoft.Data.SqlClient;

namespace DieMaking.Helpers;

public static class TableCreator
{
    public static TableEnsureResult EnsureTablesExist()
    {
        // 原 EnsureTablesExist 方法代码
    }

    // 所有 GetXXXTableSql 方法移到这里
    private static string GetUserTableSql() => @"...";
    private static string GetDieInfoTableSql() => @"...";
    private static string GetDieProcessTableSql() => @"...";
    // ... 其他表SQL
}

public class TableEnsureResult
{
    public int TablesCreated { get; set; }
    public List<string> Messages { get; set; } = new();
}
```

### 3. IndexCreator.cs（约150行）
负责索引创建

```csharp
using Microsoft.Data.SqlClient;

namespace DieMaking.Helpers;

public static class IndexCreator
{
    public static void CreateIndexes(SqlConnection connection)
    {
        // 原 CreateIndexes 方法代码
    }
}
```

### 4. DataSeeder.cs（约300行）
负责初始数据和密码哈希

```csharp
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace DieMaking.Helpers;

public static class DataSeeder
{
    public static InitialDataResult EnsureInitialData()
    {
        // 原 EnsureInitialData 方法代码
    }

    /// <summary>
    /// 密码哈希（SHA256）- 建议后续升级为 PBKDF2
    /// </summary>
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

public class InitialDataResult
{
    public bool DataInserted { get; set; }
    public List<string> Messages { get; set; } = new();
}
```

### 5. 修改后的 DatabaseInitializer.cs（约150行）

```csharp
namespace DieMaking.Helpers;

public static class DatabaseInitializer
{
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

            // 3. 检查并插入初始数据
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
}

public class InitializationResult
{
    public bool Success { get; set; }
    public bool DatabaseCreated { get; set; }
    public int TablesCreated { get; set; }
    public bool DataInitialized { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Messages { get; set; } = new();
}
```

## 文件结构

```
Helpers/
├── DatabaseInitializer.cs      # 主入口（150行）
├── DatabaseCreator.cs          # 数据库创建（200行）
├── TableCreator.cs             # 表结构创建（500行）
├── IndexCreator.cs             # 索引创建（150行）
└── DataSeeder.cs               # 初始数据（300行）
```

## 修改步骤

1. 创建4个新文件，复制对应代码
2. 修改 DatabaseInitializer.cs，改为调用新类
3. 确保所有 using 语句正确
4. 编译测试
