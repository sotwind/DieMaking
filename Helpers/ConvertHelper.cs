using System.Globalization;

namespace DieMaking.Helpers;

/// <summary>
/// 类型转换帮助类 - 提供安全的类型转换方法
/// </summary>
public static class ConvertHelper
{
    #region 基础类型转换

    /// <summary>
    /// 安全转换为整数
    /// </summary>
    public static int ToInt(object? value, int defaultValue = 0)
    {
        if (value == null || value == DBNull.Value)
            return defaultValue;

        try
        {
            return Convert.ToInt32(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 安全转换为可空整数
    /// </summary>
    public static int? ToNullableInt(object? value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        try
        {
            return Convert.ToInt32(value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 安全转换为长整数
    /// </summary>
    public static long ToLong(object? value, long defaultValue = 0)
    {
        if (value == null || value == DBNull.Value)
            return defaultValue;

        try
        {
            return Convert.ToInt64(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 安全转换为小数
    /// </summary>
    public static decimal ToDecimal(object? value, decimal defaultValue = 0)
    {
        if (value == null || value == DBNull.Value)
            return defaultValue;

        try
        {
            return Convert.ToDecimal(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 安全转换为可空小数
    /// </summary>
    public static decimal? ToNullableDecimal(object? value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        try
        {
            return Convert.ToDecimal(value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 安全转换为双精度浮点数
    /// </summary>
    public static double ToDouble(object? value, double defaultValue = 0)
    {
        if (value == null || value == DBNull.Value)
            return defaultValue;

        try
        {
            return Convert.ToDouble(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 安全转换为布尔值
    /// </summary>
    public static bool ToBool(object? value, bool defaultValue = false)
    {
        if (value == null || value == DBNull.Value)
            return defaultValue;

        try
        {
            return Convert.ToBoolean(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 安全转换为字符串
    /// </summary>
    public static string ToString(object? value, string defaultValue = "")
    {
        if (value == null || value == DBNull.Value)
            return defaultValue;

        return value.ToString() ?? defaultValue;
    }

    /// <summary>
    /// 安全转换为日期时间
    /// </summary>
    public static DateTime ToDateTime(object? value, DateTime defaultValue)
    {
        if (value == null || value == DBNull.Value)
            return defaultValue;

        try
        {
            return Convert.ToDateTime(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 安全转换为可空日期时间
    /// </summary>
    public static DateTime? ToNullableDateTime(object? value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        try
        {
            return Convert.ToDateTime(value);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region 枚举转换

    /// <summary>
    /// 安全转换为枚举
    /// </summary>
    public static T ToEnum<T>(object? value, T defaultValue) where T : struct, Enum
    {
        if (value == null || value == DBNull.Value)
            return defaultValue;

        try
        {
            var intValue = Convert.ToInt32(value);
            if (Enum.IsDefined(typeof(T), intValue))
            {
                return (T)(object)intValue;
            }
        }
        catch
        {
            // 尝试字符串解析
            if (Enum.TryParse<T>(value.ToString(), true, out var result))
            {
                return result;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// 安全转换为可空枚举
    /// </summary>
    public static T? ToNullableEnum<T>(object? value) where T : struct, Enum
    {
        if (value == null || value == DBNull.Value)
            return null;

        try
        {
            var intValue = Convert.ToInt32(value);
            if (Enum.IsDefined(typeof(T), intValue))
            {
                return (T)(object)intValue;
            }
        }
        catch
        {
            if (Enum.TryParse<T>(value.ToString(), true, out var result))
            {
                return result;
            }
        }

        return null;
    }

    #endregion

    #region 泛型转换

    /// <summary>
    /// 安全转换为目标类型
    /// </summary>
    public static T? ConvertValue<T>(object? value, T? defaultValue = default)
    {
        if (value == null || value == DBNull.Value)
            return defaultValue;

        try
        {
            var targetType = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlyingType == typeof(int))
            {
                return (T)(object)Convert.ToInt32(value);
            }
            else if (underlyingType == typeof(long))
            {
                return (T)(object)Convert.ToInt64(value);
            }
            else if (underlyingType == typeof(decimal))
            {
                return (T)(object)Convert.ToDecimal(value);
            }
            else if (underlyingType == typeof(double))
            {
                return (T)(object)Convert.ToDouble(value);
            }
            else if (underlyingType == typeof(float))
            {
                return (T)(object)Convert.ToSingle(value);
            }
            else if (underlyingType == typeof(bool))
            {
                return (T)(object)Convert.ToBoolean(value);
            }
            else if (underlyingType == typeof(DateTime))
            {
                return (T)(object)Convert.ToDateTime(value);
            }
            else if (underlyingType == typeof(string))
            {
                return (T)(object)(value.ToString() ?? "");
            }
            else if (underlyingType.IsEnum)
            {
                var intValue = Convert.ToInt32(value);
                return (T)Enum.ToObject(underlyingType, intValue);
            }
            else
            {
                return (T)Convert.ChangeType(value, underlyingType);
            }
        }
        catch
        {
            return defaultValue;
        }
    }

    #endregion

    #region 字符串转换

    /// <summary>
    /// 字符串转换为整数
    /// </summary>
    public static int ParseInt(string? value, int defaultValue = 0)
    {
        if (int.TryParse(value, out var result))
            return result;
        return defaultValue;
    }

    /// <summary>
    /// 字符串转换为小数
    /// </summary>
    public static decimal ParseDecimal(string? value, decimal defaultValue = 0)
    {
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;
        return defaultValue;
    }

    /// <summary>
    /// 字符串转换为双精度浮点数
    /// </summary>
    public static double ParseDouble(string? value, double defaultValue = 0)
    {
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;
        return defaultValue;
    }

    /// <summary>
    /// 字符串转换为日期时间
    /// </summary>
    public static DateTime? ParseDateTime(string? value, string[]? formats = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        formats ??= new[] { "yyyy-MM-dd", "yyyy/MM/dd", "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd HH:mm:ss" };

        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            return result;

        if (DateTime.TryParse(value, out result))
            return result;

        return null;
    }

    /// <summary>
    /// 字符串转换为布尔值
    /// </summary>
    public static bool ParseBool(string? value, bool defaultValue = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        // 常见真值
        var trueValues = new[] { "true", "1", "yes", "y", "是", "真" };
        if (trueValues.Contains(value.Trim().ToLowerInvariant()))
            return true;

        // 常见假值
        var falseValues = new[] { "false", "0", "no", "n", "否", "假" };
        if (falseValues.Contains(value.Trim().ToLowerInvariant()))
            return false;

        if (bool.TryParse(value, out var result))
            return result;

        return defaultValue;
    }

    #endregion

    #region 格式化输出

    /// <summary>
    /// 格式化金额为字符串
    /// </summary>
    public static string FormatMoney(decimal value, string format = "N2")
    {
        return value.ToString(format);
    }

    /// <summary>
    /// 格式化为百分比
    /// </summary>
    public static string FormatPercent(decimal value, int decimals = 2)
    {
        return value.ToString($"P{decimals}");
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;
        const long TB = GB * 1024;

        if (bytes >= TB)
            return $"{bytes / (double)TB:F2} TB";
        if (bytes >= GB)
            return $"{bytes / (double)GB:F2} GB";
        if (bytes >= MB)
            return $"{bytes / (double)MB:F2} MB";
        if (bytes >= KB)
            return $"{bytes / (double)KB:F2} KB";
        return $"{bytes} B";
    }

    /// <summary>
    /// 格式化时间间隔
    /// </summary>
    public static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
            return $"{timeSpan.TotalDays:F1}天";
        if (timeSpan.TotalHours >= 1)
            return $"{timeSpan.TotalHours:F1}小时";
        if (timeSpan.TotalMinutes >= 1)
            return $"{timeSpan.TotalMinutes:F0}分钟";
        return $"{timeSpan.TotalSeconds:F0}秒";
    }

    /// <summary>
    /// 格式化日期时间
    /// </summary>
    public static string FormatDateTime(DateTime? dateTime, string format = "yyyy-MM-dd HH:mm:ss", string nullText = "-")
    {
        return dateTime.HasValue ? dateTime.Value.ToString(format) : nullText;
    }

    /// <summary>
    /// 格式化日期
    /// </summary>
    public static string FormatDate(DateTime? dateTime, string format = "yyyy-MM-dd", string nullText = "-")
    {
        return dateTime.HasValue ? dateTime.Value.ToString(format) : nullText;
    }

    #endregion

    #region 集合转换

    /// <summary>
    /// 将对象列表转换为指定类型列表
    /// </summary>
    public static List<T> ToList<T>(IEnumerable<object?>? values, T? defaultValue = default) where T : notnull
    {
        var result = new List<T>();
        if (values == null)
            return result;

        foreach (var value in values)
        {
            var converted = ConvertValue(value, defaultValue);
            if (converted != null)
            {
                result.Add(converted);
            }
        }

        return result;
    }

    /// <summary>
    /// 将逗号分隔的字符串转换为整数列表
    /// </summary>
    public static List<int> ToIntList(string? value, char separator = ',')
    {
        var result = new List<int>();
        if (string.IsNullOrWhiteSpace(value))
            return result;

        var parts = value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (int.TryParse(part.Trim(), out var num))
            {
                result.Add(num);
            }
        }

        return result;
    }

    /// <summary>
    /// 将整数列表转换为逗号分隔的字符串
    /// </summary>
    public static string ToStringList(IEnumerable<int> values, string separator = ",")
    {
        return string.Join(separator, values);
    }

    #endregion

    #region 数据库读取器转换

    /// <summary>
    /// 从数据库读取器安全获取值
    /// </summary>
    public static T GetValue<T>(Microsoft.Data.SqlClient.SqlDataReader reader, string columnName, T defaultValue)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return defaultValue;

            var value = reader.GetValue(ordinal);
            return ConvertValue(value, defaultValue) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 从数据库读取器安全获取可空值
    /// </summary>
    public static T? GetNullableValue<T>(Microsoft.Data.SqlClient.SqlDataReader reader, string columnName) where T : struct
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return null;

            var value = reader.GetValue(ordinal);
            return ConvertValue<T>(value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 从数据库读取器安全获取字符串
    /// </summary>
    public static string GetString(Microsoft.Data.SqlClient.SqlDataReader reader, string columnName, string defaultValue = "")
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return defaultValue;

            return reader.GetString(ordinal) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    #endregion
}
