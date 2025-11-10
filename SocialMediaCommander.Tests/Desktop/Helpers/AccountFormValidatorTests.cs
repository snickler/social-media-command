using System;
using System.Collections.Generic;
using Xunit;
using SocialMediaCommander.Desktop.Helpers;

namespace SocialMediaCommander.Tests.Desktop.Helpers;

/// <summary>
/// Unit tests for AccountFormValidator
/// Tests cyclic complexity reduction and form validation functionality
/// </summary>
public class AccountFormValidatorTests
{
    private readonly AccountFormValidator _validator;

    public AccountFormValidatorTests()
    {
        _validator = new AccountFormValidator();
    }

    [Fact]
    public void ValidateAccount_WithValidData_ReturnsValidResult()
    {
        // Arrange
        var username = "testuser123";
        var displayName = "Test User";

        // Act
        var result = _validator.ValidateAccount(username, displayName);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.FormattedErrorMessage);
    }

    [Fact]
    public void ValidateAccount_WithEmptyUsername_ReturnsInvalidResult()
    {
        // Arrange
        var username = "";
        var displayName = "Test User";

        // Act
        var result = _validator.ValidateAccount(username, displayName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Username is required", result.Errors);
        Assert.Contains("Username is required", result.FormattedErrorMessage);
    }

    [Fact]
    public void ValidateAccount_WithEmptyDisplayName_ReturnsInvalidResult()
    {
        // Arrange
        var username = "testuser";
        var displayName = "";

        // Act
        var result = _validator.ValidateAccount(username, displayName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Display name is required", result.Errors);
        Assert.Contains("Display name is required", result.FormattedErrorMessage);
    }

    [Fact]
    public void ValidateAccount_WithWhitespaceOnlyFields_ReturnsInvalidResult()
    {
        // Arrange
        var username = "   ";
        var displayName = "   ";

        // Act
        var result = _validator.ValidateAccount(username, displayName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Username is required", result.Errors);
        Assert.Contains("Display name is required", result.Errors);
    }

    [Theory]
    [InlineData("ab")] // Too short
    [InlineData("a")] // Too short
    [InlineData("")] // Empty
    public void ValidateAccount_WithTooShortUsername_ReturnsInvalidResult(string username)
    {
        // Arrange
        var displayName = "Test User";

        // Act
        var result = _validator.ValidateAccount(username, displayName);

        // Assert
        Assert.False(result.IsValid);
        if (!string.IsNullOrEmpty(username))
        {
            Assert.Contains("Username must be at least 3 characters long", result.Errors);
        }
    }

    [Fact]
    public void ValidateAccount_WithTooLongUsername_ReturnsInvalidResult()
    {
        // Arrange
        var username = new string('a', 51); // 51 characters
        var displayName = "Test User";

        // Act
        var result = _validator.ValidateAccount(username, displayName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Username cannot exceed 50 characters", result.Errors);
    }

    [Fact]
    public void ValidateAccount_WithTooLongDisplayName_ReturnsInvalidResult()
    {
        // Arrange
        var username = "testuser";
        var displayName = new string('a', 101); // 101 characters

        // Act
        var result = _validator.ValidateAccount(username, displayName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Display name cannot exceed 100 characters", result.Errors);
    }

    [Theory]
    [InlineData("user123")] // Valid
    [InlineData("user_123")] // Valid with underscore
    [InlineData("user-123")] // Valid with hyphen
    [InlineData("User123")] // Valid with capital letters
    [InlineData("user")] // Valid simple
    public void ValidateAccount_WithValidUsernameFormats_ReturnsValidResult(string username)
    {
        // Arrange
        var displayName = "Test User";

        // Act
        var result = _validator.ValidateAccount(username, displayName);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("user@123")] // Invalid character @
    [InlineData("user.123")] // Invalid character .
    [InlineData("user#123")] // Invalid character #
    [InlineData("user 123")] // Invalid space
    [InlineData("user!123")] // Invalid character !
    public void ValidateAccount_WithInvalidUsernameCharacters_ReturnsInvalidResult(string username)
    {
        // Arrange
        var displayName = "Test User";

        // Act
        var result = _validator.ValidateAccount(username, displayName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Username contains invalid characters", result.Errors[0]);
    }

    [Fact]
    public void ValidateAccount_WithValidOAuthConfiguration_ReturnsValidResult()
    {
        // Arrange
        var username = "testuser";
        var displayName = "Test User";
        var clientId = "client123456789";
        var clientSecret = "secret123456789012345";
        var redirectUri = "https://example.com/callback";

        // Act
        var result = _validator.ValidateAccount(username, displayName, clientId, clientSecret, redirectUri);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateAccount_WithIncompleteOAuthConfiguration_ReturnsInvalidResult()
    {
        // Arrange
        var username = "testuser";
        var displayName = "Test User";
        var clientId = "client123";
        var clientSecret = ""; // Missing secret

        // Act
        var result = _validator.ValidateAccount(username, displayName, clientId, clientSecret);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("OAuth Client Secret is required when providing OAuth configuration", result.Errors);
    }

    [Fact]
    public void ValidateOAuthConfiguration_WithoutAnyFields_ReturnsValidResult()
    {
        // Act
        var result = _validator.ValidateOAuthConfiguration(null, null, null);

        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.HasOAuthConfiguration);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateOAuthConfiguration_WithCompleteValidConfiguration_ReturnsValidResult()
    {
        // Arrange
        var clientId = "client123456789";
        var clientSecret = "secret123456789012345";
        var redirectUri = "https://example.com/callback";

        // Act
        var result = _validator.ValidateOAuthConfiguration(clientId, clientSecret, redirectUri);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.HasOAuthConfiguration);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateOAuthConfiguration_WithMissingClientId_ReturnsInvalidResult()
    {
        // Arrange
        var clientId = "";
        var clientSecret = "secret123456789012345";

        // Act
        var result = _validator.ValidateOAuthConfiguration(clientId, clientSecret);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("OAuth Client ID is required when providing OAuth configuration", result.Errors);
    }

    [Fact]
    public void ValidateOAuthConfiguration_WithMissingClientSecret_ReturnsInvalidResult()
    {
        // Arrange
        var clientId = "client123456789";
        var clientSecret = "";

        // Act
        var result = _validator.ValidateOAuthConfiguration(clientId, clientSecret);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("OAuth Client Secret is required when providing OAuth configuration", result.Errors);
    }

    [Fact]
    public void ValidateOAuthConfiguration_WithTooShortClientId_ReturnsInvalidResult()
    {
        // Arrange
        var clientId = "short"; // Less than 10 characters
        var clientSecret = "secret123456789012345";

        // Act
        var result = _validator.ValidateOAuthConfiguration(clientId, clientSecret);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("OAuth Client ID appears to be too short", result.Errors);
    }

    [Fact]
    public void ValidateOAuthConfiguration_WithTooShortClientSecret_ReturnsInvalidResult()
    {
        // Arrange
        var clientId = "client123456789";
        var clientSecret = "short"; // Less than 20 characters

        // Act
        var result = _validator.ValidateOAuthConfiguration(clientId, clientSecret);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("OAuth Client Secret appears to be too short", result.Errors);
    }

    [Theory]
    [InlineData("not-a-url")] // Invalid format
    [InlineData("http://")]    // Incomplete URL
    [InlineData("ftp://example.com/callback")] // Wrong protocol
    [InlineData("file:///path/to/file")] // Wrong protocol
    public void ValidateOAuthConfiguration_WithInvalidRedirectUri_ReturnsInvalidResult(string redirectUri)
    {
        // Arrange
        var clientId = "client123456789";
        var clientSecret = "secret123456789012345";

        // Act
        var result = _validator.ValidateOAuthConfiguration(clientId, clientSecret, redirectUri);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count > 0);
        // Should have either invalid URL format or wrong protocol error
        Assert.True(result.Errors.Exists(e => e.Contains("valid URL") || e.Contains("HTTP or HTTPS")));
    }

    [Theory]
    [InlineData("https://example.com/callback")]
    [InlineData("http://localhost:8080/callback")]
    [InlineData("https://myapp.herokuapp.com/auth/callback")]
    public void ValidateOAuthConfiguration_WithValidRedirectUri_ReturnsValidResult(string redirectUri)
    {
        // Arrange
        var clientId = "client123456789";
        var clientSecret = "secret123456789012345";

        // Act
        var result = _validator.ValidateOAuthConfiguration(clientId, clientSecret, redirectUri);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.HasOAuthConfiguration);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateAccount_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var username = "ab"; // Too short
        var displayName = new string('x', 101); // Too long
        var clientId = "short"; // Too short
        var clientSecret = ""; // Missing

        // Act
        var result = _validator.ValidateAccount(username, displayName, clientId, clientSecret);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Username must be at least 3 characters long", result.Errors);
        Assert.Contains("Display name cannot exceed 100 characters", result.Errors);
        Assert.Contains("OAuth Client ID appears to be too short", result.Errors);
        Assert.Contains("OAuth Client Secret is required when providing OAuth configuration", result.Errors);
        Assert.True(result.Errors.Count >= 4);
        Assert.Contains("⚠️", result.FormattedErrorMessage);
    }

    [Fact]
    public void ValidateOAuthConfiguration_WithPartialConfiguration_RequiresAllFields()
    {
        // Arrange - only redirect URI provided
        var redirectUri = "https://example.com/callback";

        // Act
        var result = _validator.ValidateOAuthConfiguration(null, null, redirectUri);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("OAuth Client ID is required when providing OAuth configuration", result.Errors);
        Assert.Contains("OAuth Client Secret is required when providing OAuth configuration", result.Errors);
    }

    [Fact]
    public void FormattedErrorMessage_WithNoErrors_ReturnsEmptyString()
    {
        // Arrange
        var username = "validuser";
        var displayName = "Valid User";

        // Act
        var result = _validator.ValidateAccount(username, displayName);

        // Assert
        Assert.Empty(result.FormattedErrorMessage);
    }

    [Fact]
    public void FormattedErrorMessage_WithErrors_StartsWithWarningSymbol()
    {
        // Arrange
        var username = "";
        var displayName = "Valid User";

        // Act
        var result = _validator.ValidateAccount(username, displayName);

        // Assert
        Assert.StartsWith("⚠️", result.FormattedErrorMessage);
    }
}
