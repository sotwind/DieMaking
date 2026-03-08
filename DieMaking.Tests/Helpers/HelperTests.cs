using DieMaking.Helpers;
using Xunit;

namespace DieMaking.Tests.Helpers;

/// <summary>
/// 辅助类单元测试
/// </summary>
public class HelperTests
{
    #region ConvertHelper Tests

    [Fact]
    public void Test_ConvertHelper_ToInt_ValidInteger_ReturnsInt()
    {
        // Arrange
        var value = 42;

        // Act
        var result = ConvertHelper.ToInt(value);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToInt_ValidString_ReturnsInt()
    {
        // Arrange
        var value = "42";

        // Act
        var result = ConvertHelper.ToInt(value);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToInt_InvalidValue_ReturnsZero()
    {
        // Arrange
        var value = "invalid";

        // Act
        var result = ConvertHelper.ToInt(value);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToInt_NullValue_ReturnsZero()
    {
        // Arrange
        object? value = null;

        // Act
        var result = ConvertHelper.ToInt(value);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToInt_DBNullValue_ReturnsZero()
    {
        // Arrange
        var value = DBNull.Value;

        // Act
        var result = ConvertHelper.ToInt(value);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToString_ValidString_ReturnsString()
    {
        // Arrange
        var value = "test";

        // Act
        var result = ConvertHelper.ToString(value);

        // Assert
        Assert.Equal("test", result);
    }

    [Fact]
    public void Test_ConvertHelper_ToString_Integer_ReturnsString()
    {
        // Arrange
        var value = 42;

        // Act
        var result = ConvertHelper.ToString(value);

        // Assert
        Assert.Equal("42", result);
    }

    [Fact]
    public void Test_ConvertHelper_ToString_NullValue_ReturnsEmptyString()
    {
        // Arrange
        object? value = null;

        // Act
        var result = ConvertHelper.ToString(value);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToString_DBNullValue_ReturnsEmptyString()
    {
        // Arrange
        var value = DBNull.Value;

        // Act
        var result = ConvertHelper.ToString(value);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToDecimal_ValidDecimal_ReturnsDecimal()
    {
        // Arrange
        var value = 123.45m;

        // Act
        var result = ConvertHelper.ToDecimal(value);

        // Assert
        Assert.Equal(123.45m, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToDecimal_ValidString_ReturnsDecimal()
    {
        // Arrange
        var value = "123.45";

        // Act
        var result = ConvertHelper.ToDecimal(value);

        // Assert
        Assert.Equal(123.45m, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToDecimal_InvalidValue_ReturnsZero()
    {
        // Arrange
        var value = "invalid";

        // Act
        var result = ConvertHelper.ToDecimal(value);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToBool_TrueValue_ReturnsTrue()
    {
        // Arrange
        var value = true;

        // Act
        var result = ConvertHelper.ToBool(value);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Test_ConvertHelper_ToBool_FalseValue_ReturnsFalse()
    {
        // Arrange
        var value = false;

        // Act
        var result = ConvertHelper.ToBool(value);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_ConvertHelper_ToBool_StringTrue_ReturnsTrue()
    {
        // Arrange
        var value = "true";

        // Act
        var result = ConvertHelper.ToBool(value);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Test_ConvertHelper_ToBool_StringFalse_ReturnsFalse()
    {
        // Arrange
        var value = "false";

        // Act
        var result = ConvertHelper.ToBool(value);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_ConvertHelper_ToBool_Integer1_ReturnsTrue()
    {
        // Arrange
        var value = 1;

        // Act
        var result = ConvertHelper.ToBool(value);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Test_ConvertHelper_ToBool_Integer0_ReturnsFalse()
    {
        // Arrange
        var value = 0;

        // Act
        var result = ConvertHelper.ToBool(value);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_ConvertHelper_ToDateTime_ValidDateTime_ReturnsDateTime()
    {
        // Arrange
        var expectedDate = new DateTime(2024, 1, 15);
        var value = expectedDate;

        // Act
        var result = ConvertHelper.ToDateTime(value, DateTime.MinValue);

        // Assert
        Assert.Equal(expectedDate, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToDateTime_ValidString_ReturnsDateTime()
    {
        // Arrange
        var expectedDate = new DateTime(2024, 1, 15);
        var value = "2024-01-15";

        // Act
        var result = ConvertHelper.ToDateTime(value, DateTime.MinValue);

        // Assert
        Assert.Equal(expectedDate, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToDateTime_InvalidValue_ReturnsDefault()
    {
        // Arrange
        var defaultValue = new DateTime(2024, 1, 1);
        var value = "invalid";

        // Act
        var result = ConvertHelper.ToDateTime(value, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToNullableInt_ValidValue_ReturnsInt()
    {
        // Arrange
        var value = 42;

        // Act
        var result = ConvertHelper.ToNullableInt(value);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToNullableInt_NullValue_ReturnsNull()
    {
        // Arrange
        object? value = null;

        // Act
        var result = ConvertHelper.ToNullableInt(value);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Test_ConvertHelper_ToNullableInt_DBNullValue_ReturnsNull()
    {
        // Arrange
        var value = DBNull.Value;

        // Act
        var result = ConvertHelper.ToNullableInt(value);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Test_ConvertHelper_ToNullableDateTime_ValidValue_ReturnsDateTime()
    {
        // Arrange
        var expectedDate = new DateTime(2024, 1, 15);
        var value = expectedDate;

        // Act
        var result = ConvertHelper.ToNullableDateTime(value);

        // Assert
        Assert.Equal(expectedDate, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToNullableDateTime_NullValue_ReturnsNull()
    {
        // Arrange
        object? value = null;

        // Act
        var result = ConvertHelper.ToNullableDateTime(value);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Test_ConvertHelper_ToNullableDateTime_DBNullValue_ReturnsNull()
    {
        // Arrange
        var value = DBNull.Value;

        // Act
        var result = ConvertHelper.ToNullableDateTime(value);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Test_ConvertHelper_ToNullableDecimal_ValidValue_ReturnsDecimal()
    {
        // Arrange
        var value = 123.45m;

        // Act
        var result = ConvertHelper.ToNullableDecimal(value);

        // Assert
        Assert.Equal(123.45m, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToNullableDecimal_NullValue_ReturnsNull()
    {
        // Arrange
        object? value = null;

        // Act
        var result = ConvertHelper.ToNullableDecimal(value);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Test_ConvertHelper_ToEnum_ValidEnumValue_ReturnsEnum()
    {
        // Arrange
        var value = 1;

        // Act
        var result = ConvertHelper.ToEnum(value, DieStatus.Pending);

        // Assert
        Assert.Equal(DieStatus.InProgress, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToEnum_InvalidValue_ReturnsDefault()
    {
        // Arrange
        var value = "invalid";
        var defaultValue = DieStatus.Pending;

        // Act
        var result = ConvertHelper.ToEnum(value, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void Test_ConvertHelper_ToEnum_NullValue_ReturnsDefault()
    {
        // Arrange
        object? value = null;
        var defaultValue = DieStatus.Pending;

        // Act
        var result = ConvertHelper.ToEnum(value, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    #endregion

    #region ValidationHelper Tests

    [Fact]
    public void Test_ValidationHelper_ValidateRequired_WithValue_ReturnsSuccess()
    {
        // Arrange
        var value = "test";
        var fieldName = "测试字段";

        // Act
        var result = ValidationHelper.ValidateRequired(value, fieldName);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateRequired_EmptyValue_ReturnsFail()
    {
        // Arrange
        var value = "";
        var fieldName = "测试字段";

        // Act
        var result = ValidationHelper.ValidateRequired(value, fieldName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("不能为空", result.ErrorMessage);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateRequired_NullValue_ReturnsFail()
    {
        // Arrange
        string? value = null;
        var fieldName = "测试字段";

        // Act
        var result = ValidationHelper.ValidateRequired(value, fieldName);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateLength_ValidLength_ReturnsSuccess()
    {
        // Arrange
        var value = "test";
        var fieldName = "测试字段";
        var minLength = 2;
        var maxLength = 10;

        // Act
        var result = ValidationHelper.ValidateLength(value, fieldName, minLength, maxLength);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateLength_TooLong_ReturnsFail()
    {
        // Arrange
        var value = "this is a very long text";
        var fieldName = "测试字段";
        var minLength = 2;
        var maxLength = 10;

        // Act
        var result = ValidationHelper.ValidateLength(value, fieldName, minLength, maxLength);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("不能超过", result.ErrorMessage);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateLength_TooShort_ReturnsFail()
    {
        // Arrange
        var value = "a";
        var fieldName = "测试字段";
        var minLength = 5;
        var maxLength = 10;

        // Act
        var result = ValidationHelper.ValidateLength(value, fieldName, minLength, maxLength);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("不能少于", result.ErrorMessage);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateRange_IntInRange_ReturnsSuccess()
    {
        // Arrange
        var value = 50;
        var fieldName = "测试字段";
        var min = 0;
        var max = 100;

        // Act
        var result = ValidationHelper.ValidateRange(value, fieldName, min, max);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateRange_IntOutOfRange_ReturnsFail()
    {
        // Arrange
        var value = 150;
        var fieldName = "测试字段";
        var min = 0;
        var max = 100;

        // Act
        var result = ValidationHelper.ValidateRange(value, fieldName, min, max);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("必须在", result.ErrorMessage);
    }

    [Fact]
    public void Test_ValidationHelper_ValidatePositive_PositiveValue_ReturnsSuccess()
    {
        // Arrange
        var value = 10m;
        var fieldName = "测试字段";

        // Act
        var result = ValidationHelper.ValidatePositive(value, fieldName);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Test_ValidationHelper_ValidatePositive_ZeroValue_ReturnsFail()
    {
        // Arrange
        var value = 0m;
        var fieldName = "测试字段";

        // Act
        var result = ValidationHelper.ValidatePositive(value, fieldName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("必须大于0", result.ErrorMessage);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateNonNegative_NonNegativeValue_ReturnsSuccess()
    {
        // Arrange
        var value = 0m;
        var fieldName = "测试字段";

        // Act
        var result = ValidationHelper.ValidateNonNegative(value, fieldName);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateNonNegative_NegativeValue_ReturnsFail()
    {
        // Arrange
        var value = -5m;
        var fieldName = "测试字段";

        // Act
        var result = ValidationHelper.ValidateNonNegative(value, fieldName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("不能为负数", result.ErrorMessage);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateEmail_ValidEmail_ReturnsSuccess()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var result = ValidationHelper.ValidateEmail(email);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateEmail_InvalidEmail_ReturnsFail()
    {
        // Arrange
        var email = "invalid-email";

        // Act
        var result = ValidationHelper.ValidateEmail(email);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateEmail_EmptyEmail_ReturnsSuccess()
    {
        // Arrange
        var email = "";

        // Act
        var result = ValidationHelper.ValidateEmail(email);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Test_ValidationHelper_ValidatePhone_ValidPhone_ReturnsSuccess()
    {
        // Arrange
        var phone = "13800138000";

        // Act
        var result = ValidationHelper.ValidatePhone(phone);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Test_ValidationHelper_ValidatePhone_InvalidPhone_ReturnsFail()
    {
        // Arrange
        var phone = "123";

        // Act
        var result = ValidationHelper.ValidatePhone(phone);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Test_ValidationHelper_ValidatePassword_ValidPassword_ReturnsSuccess()
    {
        // Arrange
        var password = "password123";

        // Act
        var result = ValidationHelper.ValidatePassword(password);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Test_ValidationHelper_ValidatePassword_EmptyPassword_ReturnsFail()
    {
        // Arrange
        var password = "";

        // Act
        var result = ValidationHelper.ValidatePassword(password);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("不能为空", result.ErrorMessage);
    }

    [Fact]
    public void Test_ValidationHelper_ValidatePassword_TooShort_ReturnsFail()
    {
        // Arrange
        var password = "123";
        var minLength = 6;

        // Act
        var result = ValidationHelper.ValidatePassword(password, minLength);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("长度至少为", result.ErrorMessage);
    }

    [Fact]
    public void Test_ValidationHelper_ValidatePassword_RequireDigit_NoDigit_ReturnsFail()
    {
        // Arrange
        var password = "password";
        var minLength = 6;
        var requireDigit = true;

        // Act
        var result = ValidationHelper.ValidatePassword(password, minLength, requireDigit);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("必须包含数字", result.ErrorMessage);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateDateOrder_ValidOrder_ReturnsSuccess()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        // Act
        var result = ValidationHelper.ValidateDateOrder(startDate, endDate);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateDateOrder_InvalidOrder_ReturnsFail()
    {
        // Arrange
        var startDate = new DateTime(2024, 12, 31);
        var endDate = new DateTime(2024, 1, 1);

        // Act
        var result = ValidationHelper.ValidateDateOrder(startDate, endDate);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("不能晚于", result.ErrorMessage);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateCode_ValidCode_ReturnsSuccess()
    {
        // Arrange
        var code = "ABC-123_test";

        // Act
        var result = ValidationHelper.ValidateCode(code);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateCode_InvalidCode_ReturnsFail()
    {
        // Arrange
        var code = "ABC@123";

        // Act
        var result = ValidationHelper.ValidateCode(code);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateName_ValidName_ReturnsSuccess()
    {
        // Arrange
        var name = "测试名称";

        // Act
        var result = ValidationHelper.ValidateName(name);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateName_EmptyName_ReturnsFail()
    {
        // Arrange
        var name = "";

        // Act
        var result = ValidationHelper.ValidateName(name);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Test_ValidationHelper_ValidateName_InvalidChar_ReturnsFail()
    {
        // Arrange
        var name = "测试<script>";

        // Act
        var result = ValidationHelper.ValidateName(name);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("非法字符", result.ErrorMessage);
    }

    [Fact]
    public void Test_ValidationResult_Success_ReturnsValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(string.Empty, result.ErrorMessage);
    }

    [Fact]
    public void Test_ValidationResult_Fail_ReturnsInvalidResult()
    {
        // Arrange
        var message = "验证失败";

        // Act
        var result = ValidationResult.Fail(message);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(message, result.ErrorMessage);
    }

    #endregion

    #region ConfigHelper Tests

    [Fact]
    public void Test_ConfigHelper_GetValue_ReturnsStringOrNull()
    {
        // Arrange
        var key = "TestKey";

        // Act
        var result = ConfigHelper.GetValue(key);

        // Assert - 可能返回null如果没有配置
        Assert.True(result == null || result is string);
    }

    [Fact]
    public void Test_ConfigHelper_GetValueInt_ReturnsInt()
    {
        // Arrange
        var key = "TestIntKey";

        // Act
        var result = ConfigHelper.GetValueInt(key, 0);

        // Assert
        Assert.IsType<int>(result);
    }

    [Fact]
    public void Test_ConfigHelper_GetValueBool_ReturnsBool()
    {
        // Arrange
        var key = "TestBoolKey";

        // Act
        var result = ConfigHelper.GetValueBool(key, false);

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void Test_ConfigHelper_FormatDate_ReturnsFormattedString()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15);

        // Act
        var result = ConfigHelper.FormatDate(date);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<string>(result);
    }

    [Fact]
    public void Test_ConfigHelper_FormatTime_ReturnsFormattedString()
    {
        // Arrange
        var time = new DateTime(2024, 1, 15, 14, 30, 0);

        // Act
        var result = ConfigHelper.FormatTime(time);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<string>(result);
    }

    [Fact]
    public void Test_ConfigHelper_FormatDateTime_ReturnsFormattedString()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 15, 14, 30, 0);

        // Act
        var result = ConfigHelper.FormatDateTime(dateTime);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<string>(result);
    }

    [Fact]
    public void Test_ConfigHelper_SystemName_IsNotNull()
    {
        // Assert
        Assert.NotNull(ConfigHelper.SystemName);
    }

    [Fact]
    public void Test_ConfigHelper_SystemVersion_IsNotNull()
    {
        // Assert
        Assert.NotNull(ConfigHelper.SystemVersion);
    }

    [Fact]
    public void Test_ConfigHelper_DefaultPageSize_IsPositive()
    {
        // Assert
        Assert.True(ConfigHelper.DefaultPageSize > 0);
    }

    [Fact]
    public void Test_ConfigHelper_DateFormat_IsNotNull()
    {
        // Assert
        Assert.NotNull(ConfigHelper.DateFormat);
    }

    [Fact]
    public void Test_ConfigHelper_TimeFormat_IsNotNull()
    {
        // Assert
        Assert.NotNull(ConfigHelper.TimeFormat);
    }

    [Fact]
    public void Test_ConfigHelper_DateTimeFormat_IsNotNull()
    {
        // Assert
        Assert.NotNull(ConfigHelper.DateTimeFormat);
    }

    #endregion
}
