using System.Data;
using DieMaking.Helpers;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

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

        const string sql = "SELECT ConfigValue FROM SystemConfig WHERE ConfigKey = @ConfigKey";
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@ConfigKey", key));

        var result = cmd.ExecuteScalar();
        return result?.ToString();
    }

    /// <summary>
    /// 获取decimal类型配置
    /// </summary>
    public decimal GetDecimalConfig(string key, decimal defaultValue = 0)
    {
        var value = GetConfig(key);
        if (decimal.TryParse(value, out var result))
            return result;
        return defaultValue;
    }

    /// <summary>
    /// 获取int类型配置
    /// </summary>
    public int GetIntConfig(string key, int defaultValue = 0)
    {
        var value = GetConfig(key);
        if (int.TryParse(value, out var result))
            return result;
        return defaultValue;
    }

    /// <summary>
    /// 获取bool类型配置
    /// </summary>
    public bool GetBoolConfig(string key, bool defaultValue = false)
    {
        var value = GetConfig(key);
        if (bool.TryParse(value, out var result))
            return result;
        // 处理 "1"/"0" 或 "yes"/"no" 等格式
        if (value == "1" || value?.ToLower() == "true" || value?.ToLower() == "yes")
            return true;
        if (value == "0" || value?.ToLower() == "false" || value?.ToLower() == "no")
            return false;
        return defaultValue;
    }

    /// <summary>
    /// 更新配置
    /// </summary>
    public bool SetConfig(string key, string value, string? updateUser = null)
    {
        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        const string sql = @"
            UPDATE SystemConfig 
            SET ConfigValue = @ConfigValue, 
                UpdateTime = @UpdateTime, 
                UpdateUser = @UpdateUser 
            WHERE ConfigKey = @ConfigKey";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@ConfigKey", key));
        cmd.Parameters.Add(new SqlParameter("@ConfigValue", value));
        cmd.Parameters.Add(new SqlParameter("@UpdateTime", DateTime.Now));
        cmd.Parameters.Add(new SqlParameter("@UpdateUser", updateUser ?? (object)DBNull.Value));

        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 获取所有配置
    /// </summary>
    public List<SystemConfigItem> GetAllConfigs()
    {
        var list = new List<SystemConfigItem>();

        using var conn = DatabaseConfig.CreateConnection();
        conn.Open();

        const string sql = "SELECT * FROM SystemConfig ORDER BY ConfigKey";
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new SystemConfigItem
            {
                ConfigID = Convert.ToInt32(reader["ConfigID"]),
                ConfigKey = reader["ConfigKey"].ToString() ?? string.Empty,
                ConfigValue = reader["ConfigValue"].ToString() ?? string.Empty,
                ConfigType = reader["ConfigType"] == DBNull.Value ? null : reader["ConfigType"].ToString(),
                Description = reader["Description"] == DBNull.Value ? null : reader["Description"].ToString(),
                UpdateTime = reader["UpdateTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdateTime"]),
                UpdateUser = reader["UpdateUser"] == DBNull.Value ? null : reader["UpdateUser"].ToString()
            });
        }

        return list;
    }

    /// <summary>
    /// 获取默认单价配置
    /// </summary>
    public PriceConfig GetDefaultPriceConfig()
    {
        return new PriceConfig
        {
            BoardFeeUnitPrice = GetDecimalConfig("BoardFeeUnitPrice", 90m),
            ProductionUnitPrice = GetDecimalConfig("ProductionUnitPrice", 8m),
            DesignUnitPrice = GetDecimalConfig("DesignUnitPrice", 70m)
        };
    }

    /// <summary>
    /// 获取默认工序列表
    /// </summary>
    public List<string> GetDefaultProcesses()
    {
        var value = GetConfig("DefaultProcesses");
        if (string.IsNullOrEmpty(value))
            return new List<string> { "绘图", "割板", "弯刀", "装刀", "贴泡沫" };
        
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}

/// <summary>
/// 系统配置项
/// </summary>
public class SystemConfigItem
{
    public int ConfigID { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string? ConfigType { get; set; }
    public string? Description { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string? UpdateUser { get; set; }
}

/// <summary>
/// 单价配置
/// </summary>
public class PriceConfig
{
    public decimal BoardFeeUnitPrice { get; set; } = 90m;
    public decimal ProductionUnitPrice { get; set; } = 8m;
    public decimal DesignUnitPrice { get; set; } = 70m;
}
