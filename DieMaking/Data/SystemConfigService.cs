using System.Data;
using Microsoft.Data.SqlClient;

namespace DieMaking.Data;

/// <summary>
/// 系统配置服务
/// </summary>
public class SystemConfigService
{
    /// <summary>
    /// 获取配置值
    /// </summary>
    public string? GetConfig(string key)
    {
        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        const string sql = "SELECT ConfigValue FROM SystemConfig WHERE ConfigKey = @Key";
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@Key", key));

        var result = cmd.ExecuteScalar();
        return result?.ToString();
    }

    /// <summary>
    /// 获取配置值（带默认值）
    /// </summary>
    public decimal GetDecimalConfig(string key, decimal defaultValue)
    {
        var value = GetConfig(key);
        if (!string.IsNullOrEmpty(value) && decimal.TryParse(value, out var result))
        {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// 更新配置
    /// </summary>
    public bool UpdateConfig(string key, string value, string? updateUser = null)
    {
        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        const string sql = @"
            UPDATE SystemConfig 
            SET ConfigValue = @Value, UpdateTime = GETDATE(), UpdateUser = @UpdateUser
            WHERE ConfigKey = @Key";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@Key", key));
        cmd.Parameters.Add(new SqlParameter("@Value", value));
        cmd.Parameters.Add(new SqlParameter("@UpdateUser", updateUser ?? (object)DBNull.Value));

        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 获取所有配置
    /// </summary>
    public Dictionary<string, string> GetAllConfigs()
    {
        var configs = new Dictionary<string, string>();

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        const string sql = "SELECT ConfigKey, ConfigValue FROM SystemConfig";
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            configs[reader["ConfigKey"].ToString()!] = reader["ConfigValue"].ToString()!;
        }

        return configs;
    }

    /// <summary>
    /// 获取单价配置
    /// </summary>
    public (decimal BoardFee, decimal ProductionFee, decimal DesignFee) GetPriceConfigs()
    {
        var boardFee = GetDecimalConfig("BoardFeeUnitPrice", 90m);
        var productionFee = GetDecimalConfig("ProductionUnitPrice", 8m);
        var designFee = GetDecimalConfig("DesignUnitPrice", 70m);

        return (boardFee, productionFee, designFee);
    }
}
