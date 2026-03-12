namespace DieMaking.Helpers;

/// <summary>
/// 类型转换辅助类
/// </summary>
public static class ConvertHelper
{
    /// <summary>
    /// 转换为整数
    /// </summary>
    public static int ToInt(object? value, int defaultValue = 0)
    {
        if (value == null || value == DBNull.Value)
            return defaultValue;

        if (value is int intValue)
            return intValue;

        if (int.TryParse(value.ToString(), out var result))
            return result;

        return defaultValue;
    }

    /// <summary>
    /// 转换为可空整数
    /// </summary>
    public static int? ToNullableInt(object? value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        if (value is int intValue)
            return intValue;

        if (int.TryParse(value.ToString(), out var result))
            return result;

        return null;
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    public static string ToString(object? value, string defaultValue = "")
    {
        if (value == null || value == DBNull.Value)
            return defaultValue;

        return value.ToString() ?? defaultValue;
    }

    /// <summary>
    /// 转换为小数
    /// </summary>
    public static decimal ToDecimal(object? value, decimal defaultValue = 0)
    {
        if (value == null || value == DBNull.Value)
            return defaultValue;

        if (value is decimal decimalValue)
            return decimalValue;

        if (decimal.TryParse(value.ToString(), out var result))
            return result;

        return defaultValue;
    }

    /// <summary>
    /// 转换为可空小数
    /// </summary>
    public static decimal? ToNullableDecimal(object? value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        if (value is decimal decimalValue)
            return decimalValue;

        if (decimal.TryParse(value.ToString(), out var result))
            return result;

        return null;
    }

    /// <summary>
    /// 转换为布尔值
    /// </summary>
    public static bool ToBool(object? value, bool defaultValue = false)
    {
        if (value == null || value == DBNull.Value)
            return defaultValue;

        if (value is bool boolValue)
            return boolValue;

        if (bool.TryParse(value.ToString(), out var result))
            return result;

        // 处理数字转换
        if (int.TryParse(value.ToString(), out var intResult))
            return intResult != 0;

        return defaultValue;
    }

    /// <summary>
    /// 转换为日期时间
    /// </summary>
    public static DateTime ToDateTime(object? value, DateTime defaultValue)
    {
        if (value == null || value == DBNull.Value)
            return defaultValue;

        if (value is DateTime dateTimeValue)
            return dateTimeValue;

        if (DateTime.TryParse(value.ToString(), out var result))
            return result;

        return defaultValue;
    }

    /// <summary>
    /// 转换为可空日期时间
    /// </summary>
    public static DateTime? ToNullableDateTime(object? value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        if (value is DateTime dateTimeValue)
            return dateTimeValue;

        if (DateTime.TryParse(value.ToString(), out var result))
            return result;

        return null;
    }

    /// <summary>
    /// 转换为枚举
    /// </summary>
    public static T ToEnum<T>(object? value, T defaultValue) where T : struct, Enum
    {
        if (value == null || value == DBNull.Value)
            return defaultValue;

        if (value is T enumValue)
            return enumValue;

        if (value is int intValue)
        {
            if (Enum.IsDefined(typeof(T), intValue))
                return (T)Enum.ToObject(typeof(T), intValue);
        }

        if (Enum.TryParse<T>(value.ToString(), true, out var result))
            return result;

        return defaultValue;
    }
}
