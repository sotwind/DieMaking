using System.Text.RegularExpressions;

namespace DieMaking.Helpers;

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    /// <summary>是否验证通过</summary>
    public bool IsValid { get; set; }
    
    /// <summary>错误信息</summary>
    public string ErrorMessage { get; set; } = string.Empty;

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Fail(string message) => new() { IsValid = false, ErrorMessage = message };
}

/// <summary>
/// 验证规则
/// </summary>
public class ValidationRule
{
    /// <summary>字段名称</summary>
    public string FieldName { get; set; } = string.Empty;
    
    /// <summary>是否必填</summary>
    public bool IsRequired { get; set; }
    
    /// <summary>最大长度</summary>
    public int? MaxLength { get; set; }
    
    /// <summary>最小长度</summary>
    public int? MinLength { get; set; }
    
    /// <summary>正则表达式模式</summary>
    public string? RegexPattern { get; set; }
    
    /// <summary>自定义验证函数</summary>
    public Func<string?, bool>? CustomValidator { get; set; }
    
    /// <summary>自定义错误消息</summary>
    public string? CustomErrorMessage { get; set; }

    /// <summary>
    /// 执行验证
    /// </summary>
    public ValidationResult Validate(string? value)
    {
        // 必填验证
        if (IsRequired && string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult.Fail($"{FieldName}为必填项");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult.Success();
        }

        // 最小长度验证
        if (MinLength.HasValue && value.Length < MinLength.Value)
        {
            return ValidationResult.Fail($"{FieldName}长度不能少于{MinLength.Value}个字符");
        }

        // 最大长度验证
        if (MaxLength.HasValue && value.Length > MaxLength.Value)
        {
            return ValidationResult.Fail($"{FieldName}长度不能超过{MaxLength.Value}个字符");
        }

        // 正则表达式验证
        if (!string.IsNullOrEmpty(RegexPattern))
        {
            if (!Regex.IsMatch(value, RegexPattern))
            {
                return ValidationResult.Fail(CustomErrorMessage ?? $"{FieldName}格式不正确");
            }
        }

        // 自定义验证
        if (CustomValidator != null && !CustomValidator(value))
        {
            return ValidationResult.Fail(CustomErrorMessage ?? $"{FieldName}验证失败");
        }

        return ValidationResult.Success();
    }
}

/// <summary>
/// 验证帮助类 - 提供通用的验证方法
/// </summary>
public static class ValidationHelper
{
    #region 字符串验证

    /// <summary>
    /// 验证字符串是否为空
    /// </summary>
    public static ValidationResult ValidateRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult.Fail($"{fieldName}不能为空");
        }
        return ValidationResult.Success();
    }

    /// <summary>
    /// 验证字符串长度
    /// </summary>
    public static ValidationResult ValidateLength(string? value, string fieldName, int minLength, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return ValidationResult.Success();
        }

        if (value.Length < minLength)
        {
            return ValidationResult.Fail($"{fieldName}长度不能少于{minLength}个字符");
        }

        if (value.Length > maxLength)
        {
            return ValidationResult.Fail($"{fieldName}长度不能超过{maxLength}个字符");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// 验证正则表达式
    /// </summary>
    public static ValidationResult ValidateRegex(string? value, string fieldName, string pattern, string errorMessage)
    {
        if (string.IsNullOrEmpty(value))
        {
            return ValidationResult.Success();
        }

        if (!Regex.IsMatch(value, pattern))
        {
            return ValidationResult.Fail(errorMessage);
        }

        return ValidationResult.Success();
    }

    #endregion

    #region 数字验证

    /// <summary>
    /// 验证整数范围
    /// </summary>
    public static ValidationResult ValidateRange(int value, string fieldName, int min, int max)
    {
        if (value < min || value > max)
        {
            return ValidationResult.Fail($"{fieldName}必须在{min}到{max}之间");
        }
        return ValidationResult.Success();
    }

    /// <summary>
    /// 验证小数范围
    /// </summary>
    public static ValidationResult ValidateRange(decimal value, string fieldName, decimal min, decimal max)
    {
        if (value < min || value > max)
        {
            return ValidationResult.Fail($"{fieldName}必须在{min}到{max}之间");
        }
        return ValidationResult.Success();
    }

    /// <summary>
    /// 验证数值必须大于0
    /// </summary>
    public static ValidationResult ValidatePositive(decimal value, string fieldName)
    {
        if (value <= 0)
        {
            return ValidationResult.Fail($"{fieldName}必须大于0");
        }
        return ValidationResult.Success();
    }

    /// <summary>
    /// 验证数值必须大于等于0
    /// </summary>
    public static ValidationResult ValidateNonNegative(decimal value, string fieldName)
    {
        if (value < 0)
        {
            return ValidationResult.Fail($"{fieldName}不能为负数");
        }
        return ValidationResult.Success();
    }

    #endregion

    #region 日期验证

    /// <summary>
    /// 验证日期范围
    /// </summary>
    public static ValidationResult ValidateDateRange(DateTime value, string fieldName, DateTime minDate, DateTime maxDate)
    {
        if (value < minDate || value > maxDate)
        {
            return ValidationResult.Fail($"{fieldName}必须在{minDate:yyyy-MM-dd}到{maxDate:yyyy-MM-dd}之间");
        }
        return ValidationResult.Success();
    }

    /// <summary>
    /// 验证开始日期必须早于结束日期
    /// </summary>
    public static ValidationResult ValidateDateOrder(DateTime startDate, DateTime endDate, string startFieldName = "开始日期", string endFieldName = "结束日期")
    {
        if (startDate > endDate)
        {
            return ValidationResult.Fail($"{startFieldName}不能晚于{endFieldName}");
        }
        return ValidationResult.Success();
    }

    #endregion

    #region 常用格式验证

    /// <summary>
    /// 验证手机号
    /// </summary>
    public static ValidationResult ValidatePhone(string? phone, string fieldName = "手机号")
    {
        if (string.IsNullOrEmpty(phone))
        {
            return ValidationResult.Success();
        }

        // 中国大陆手机号格式
        const string pattern = @"^1[3-9]\d{9}$";
        if (!Regex.IsMatch(phone, pattern))
        {
            return ValidationResult.Fail($"{fieldName}格式不正确");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// 验证邮箱
    /// </summary>
    public static ValidationResult ValidateEmail(string? email, string fieldName = "邮箱")
    {
        if (string.IsNullOrEmpty(email))
        {
            return ValidationResult.Success();
        }

        const string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (!Regex.IsMatch(email, pattern))
        {
            return ValidationResult.Fail($"{fieldName}格式不正确");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// 验证身份证号
    /// </summary>
    public static ValidationResult ValidateIdCard(string? idCard, string fieldName = "身份证号")
    {
        if (string.IsNullOrEmpty(idCard))
        {
            return ValidationResult.Success();
        }

        // 15位或18位身份证号
        const string pattern = @"^(\d{15}|\d{17}[\dXx])$";
        if (!Regex.IsMatch(idCard, pattern))
        {
            return ValidationResult.Fail($"{fieldName}格式不正确");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// 验证密码强度
    /// </summary>
    public static ValidationResult ValidatePassword(string password, int minLength = 6, bool requireDigit = false, bool requireLetter = false)
    {
        if (string.IsNullOrEmpty(password))
        {
            return ValidationResult.Fail("密码不能为空");
        }

        if (password.Length < minLength)
        {
            return ValidationResult.Fail($"密码长度至少为{minLength}位");
        }

        if (requireDigit && !password.Any(char.IsDigit))
        {
            return ValidationResult.Fail("密码必须包含数字");
        }

        if (requireLetter && !password.Any(char.IsLetter))
        {
            return ValidationResult.Fail("密码必须包含字母");
        }

        return ValidationResult.Success();
    }

    #endregion

    #region 批量验证

    /// <summary>
    /// 批量验证
    /// </summary>
    public static (bool isValid, List<string> errors) ValidateMultiple(params (string? value, string fieldName, bool isRequired, int? maxLength)[] validations)
    {
        var errors = new List<string>();

        foreach (var (value, fieldName, isRequired, maxLength) in validations)
        {
            var result = ValidateRequired(value, fieldName);
            if (!result.IsValid)
            {
                errors.Add(result.ErrorMessage);
                continue;
            }

            if (maxLength.HasValue && !string.IsNullOrEmpty(value))
            {
                result = ValidateLength(value, fieldName, 0, maxLength.Value);
                if (!result.IsValid)
                {
                    errors.Add(result.ErrorMessage);
                }
            }
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// 使用验证规则批量验证
    /// </summary>
    public static (bool isValid, List<string> errors) ValidateWithRules(List<ValidationRule> rules)
    {
        var errors = new List<string>();

        foreach (var rule in rules)
        {
            // 这里需要从某个数据源获取值，实际使用时需要调整
            // 这是一个示例实现
        }

        return (errors.Count == 0, errors);
    }

    #endregion

    #region 业务验证

    /// <summary>
    /// 验证编码格式（字母、数字、下划线、横线）
    /// </summary>
    public static ValidationResult ValidateCode(string? code, string fieldName = "编码")
    {
        if (string.IsNullOrEmpty(code))
        {
            return ValidationResult.Success();
        }

        const string pattern = @"^[a-zA-Z0-9_\-]+$";
        if (!Regex.IsMatch(code, pattern))
        {
            return ValidationResult.Fail($"{fieldName}只能包含字母、数字、下划线和横线");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// 验证名称（不允许特殊字符）
    /// </summary>
    public static ValidationResult ValidateName(string? name, string fieldName = "名称", int maxLength = 50)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ValidationResult.Fail($"{fieldName}不能为空");
        }

        if (name.Length > maxLength)
        {
            return ValidationResult.Fail($"{fieldName}长度不能超过{maxLength}个字符");
        }

        // 不允许包含特殊字符
        if (name.Contains('<') || name.Contains('>') || name.Contains('\'') || name.Contains('"'))
        {
            return ValidationResult.Fail($"{fieldName}包含非法字符");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// 验证备注长度
    /// </summary>
    public static ValidationResult ValidateRemark(string? remark, string fieldName = "备注", int maxLength = 500)
    {
        if (!string.IsNullOrEmpty(remark) && remark.Length > maxLength)
        {
            return ValidationResult.Fail($"{fieldName}长度不能超过{maxLength}个字符");
        }

        return ValidationResult.Success();
    }

    #endregion
}
