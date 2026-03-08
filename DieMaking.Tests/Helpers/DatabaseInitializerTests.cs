using DieMaking.Helpers;
using Xunit;

namespace DieMaking.Tests.Helpers;

/// <summary>
/// DatabaseInitializer 测试类
/// </summary>
public class DatabaseInitializerTests
{
    #region HashPassword Tests

    [Fact]
    public void HashPassword_SamePassword_ReturnsSameHash()
    {
        // Arrange
        var password = "testPassword123";

        // Act
        var hash1 = InvokeHashPassword(password);
        var hash2 = InvokeHashPassword(password);

        // Assert
        Assert.NotNull(hash1);
        Assert.NotNull(hash2);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashPassword_DifferentPasswords_ReturnsDifferentHashes()
    {
        // Arrange
        var password1 = "testPassword123";
        var password2 = "testPassword456";

        // Act
        var hash1 = InvokeHashPassword(password1);
        var hash2 = InvokeHashPassword(password2);

        // Assert
        Assert.NotNull(hash1);
        Assert.NotNull(hash2);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashPassword_EmptyPassword_ReturnsHash()
    {
        // Arrange
        var password = "";

        // Act
        var hash = InvokeHashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void HashPassword_LongPassword_ReturnsHash()
    {
        // Arrange
        var password = new string('a', 1000);

        // Act
        var hash = InvokeHashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void HashPassword_UnicodePassword_ReturnsHash()
    {
        // Arrange
        var password = "测试密码123!@#";

        // Act
        var hash = InvokeHashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void HashPassword_SpecialCharacters_ReturnsHash()
    {
        // Arrange
        var password = "!@#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act
        var hash = InvokeHashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 通过反射调用私有的 HashPassword 方法
    /// </summary>
    private string? InvokeHashPassword(string password)
    {
        var method = typeof(DatabaseInitializer).GetMethod("HashPassword",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return method?.Invoke(null, new object[] { password }) as string;
    }

    #endregion
}
