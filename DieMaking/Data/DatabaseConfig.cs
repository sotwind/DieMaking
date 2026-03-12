using System.Data;
using Microsoft.Data.SqlClient;

namespace DieMaking.Data;

/// <summary>
/// 数据库配置类
/// </summary>
public static class DatabaseConfig
{
    private static string? _connectionString;

    /// <summary>
    /// SQL Server 连接字符串
    /// </summary>
    public static string ConnectionString
    {
        get => _connectionString ??= GetConnectionStringFromConfig();
        set => _connectionString = value;
    }

    /// <summary>
    /// 从配置文件获取连接字符串
    /// </summary>
    private static string GetConnectionStringFromConfig()
    {
        // 优先从环境变量读取
        var connStr = Environment.GetEnvironmentVariable("DIEMAKING_DB_CONNECTION");
        if (!string.IsNullOrEmpty(connStr))
            return connStr;

        // 默认连接字符串
        return "Server=localhost;Database=DieMaking;Trusted_Connection=True;TrustServerCertificate=True;";
    }

    /// <summary>
    /// 创建数据库连接
    /// </summary>
    public static IDbConnection CreateConnection()
    {
        return new SqlConnection(ConnectionString);
    }
}

/// <summary>
/// 易捷数据库配置
/// </summary>
public static class YijieDatabaseConfig
{
    /// <summary>
    /// 易捷数据库连接信息
    /// </summary>
    public static List<YijieDatabaseInfo> GetDatabaseInfos()
    {
        return new List<YijieDatabaseInfo>
        {
            new("新厂新系统", "新系统", "36.134.7.141", 1521, "dbms", "ferp", "b0003", "kuke.b0003"),
            new("老厂新系统", "新系统", "36.138.132.30", 1521, "dbms", "ferp", "read", "ejsh.read"),
            new("临海", "老系统", "36.137.213.189", 1521, "dbms", "ejsh", "read", "ejsh.read"),
            new("温森新系统", "新系统", "db.05.forestpacking.com", 1521, "dbms", "ferp", "read", "ejsh.read")
        };
    }
}

/// <summary>
/// 易捷数据库信息
/// </summary>
public class YijieDatabaseInfo
{
    public YijieDatabaseInfo(string factoryName, string serverType, string serverName, int port,
        string serviceName, string dbName, string userName, string password)
    {
        FactoryName = factoryName ?? throw new ArgumentNullException(nameof(factoryName));
        ServerType = serverType ?? throw new ArgumentNullException(nameof(serverType));
        ServerName = serverName ?? throw new ArgumentNullException(nameof(serverName));
        Port = port;
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        DbName = dbName ?? throw new ArgumentNullException(nameof(dbName));
        UserName = userName ?? throw new ArgumentNullException(nameof(userName));
        Password = password ?? throw new ArgumentNullException(nameof(password));
    }

    public string FactoryName { get; set; }
    public string ServerType { get; set; }
    public string ServerName { get; set; }
    public int Port { get; set; }
    public string ServiceName { get; set; }
    public string DbName { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }

    /// <summary>
    /// 获取Oracle连接字符串
    /// </summary>
    public string GetOracleConnectionString()
    {
        return $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={ServerName})(PORT={Port}))(CONNECT_DATA=(SERVICE_NAME={ServiceName})));User Id={UserName};Password={Password}";
    }
}
