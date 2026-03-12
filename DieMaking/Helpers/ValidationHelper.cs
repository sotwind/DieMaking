using System.Text.RegularExpressions;

namespace DieMaking.Helpers;

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static ValidationResult Fail(string message)
    {
        return new ValidationResult { IsValid = false, ErrorMessage = message };
    }
}

/// <summary>
/// 验证辅助类
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// 验证必填字段
    /// </summary>
    public static ValidationResult ValidateRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Fail($"{fieldName}不能为空");

        return ValidationResult.Success();
    }

    /// <summary>
    /// 验证长度
    /// </summary>
    public static ValidationResult ValidateLength(string? value, string fieldName, int minLength, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return ValidationResult.Success();

        if (value.Length < minLength)
            return ValidationResult.Fail($"{fieldName}长度不能少于{minLength}个字符");

        if (value.Length > maxLength)
            return ValidationResult.Fail($"{fieldName}长度不能超过{maxLength}个字符");

        return ValidationResult.Success();
    }

    /// <summary>
    /// 验证数值范围
    /// </summary>
    public static ValidationResult ValidateRange(int value, string fieldName, int min, int max)
    {
        if (value < min || value > max)
            return ValidationResult.Fail($"{fieldName}必须在{min}到{max}之间");

        return ValidationResult.Success();
    }

    /// <summary>
    /// 验证正数
    /// </summary>
    public static ValidationResult ValidatePositive(decimal value, string fieldName)
    {
        if (value <= 0)
            return ValidationResult.Fail($"{fieldName}必须大于0");

        return ValidationResult.Success();
    }

    /// <summary>
    /// 验证非负数
    /// </summary>
    public static ValidationResult ValidateNonNegative(decimal value, string fieldName)
    {
        if (value < 0)
            return ValidationResult.Fail($"{fieldName}不能为负数");

        return ValidationResult.Success();
    }

    /// <summary>
    /// 验证邮箱格式
    /// </summary>
    public static ValidationResult ValidateEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return ValidationResult.Success();

        var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (!Regex.IsMatch(email, pattern))
            return ValidationResult.Fail("邮箱格式不正确");

        return ValidationResult.Success();
    }
}