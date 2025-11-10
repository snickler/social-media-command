using System;
using System.ComponentModel.DataAnnotations;
using SocialMediaCommander.Core.Models;
using Xunit;
using FluentAssertions;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Unit tests for Account model and related classes
/// </summary>
public class AccountModelTests
{
    [Fact]
    public void Account_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var account = new Account();

        // Assert
        account.Id.Should().NotBeNullOrEmpty();
        account.Username.Should().BeEmpty();
        account.DisplayName.Should().BeEmpty();
        account.Avatar.Should().BeNull();
        account.IsDefault.Should().BeFalse();
        account.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        account.LastUsed.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        account.AuthStatus.Should().Be(AuthenticationStatus.NotAuthenticated);
        account.Tokens.Should().BeNull();
        account.OAuthConfiguration.Should().BeNull();
        account.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Account_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var account = new Account
        {
            PlatformId = SocialPlatform.BlueSky,
            Username = "testuser",
            DisplayName = "Test User"
        };

        // Act
        var validationContext = new ValidationContext(account);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(account, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Account_WithInvalidUsername_ShouldFailValidation(string? username)
    {
        // Arrange
        var account = new Account
        {
            PlatformId = SocialPlatform.BlueSky,
            Username = username!,
            DisplayName = "Test User"
        };

        // Act
        var validationContext = new ValidationContext(account);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(account, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Username"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Account_WithInvalidDisplayName_ShouldFailValidation(string? displayName)
    {
        // Arrange
        var account = new Account
        {
            PlatformId = SocialPlatform.BlueSky,
            Username = "testuser",
            DisplayName = displayName!
        };

        // Act
        var validationContext = new ValidationContext(account);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(account, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.MemberNames.Contains("DisplayName"));
    }

    [Fact]
    public void Account_WithTooLongUsername_ShouldFailValidation()
    {
        // Arrange
        var account = new Account
        {
            PlatformId = SocialPlatform.BlueSky,
            Username = new string('a', 51), // Exceeds 50 character limit
            DisplayName = "Test User"
        };

        // Act
        var validationContext = new ValidationContext(account);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(account, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Username"));
    }

    [Fact]
    public void Account_WithTooLongDisplayName_ShouldFailValidation()
    {
        // Arrange
        var account = new Account
        {
            PlatformId = SocialPlatform.BlueSky,
            Username = "testuser",
            DisplayName = new string('a', 101) // Exceeds 100 character limit
        };

        // Act
        var validationContext = new ValidationContext(account);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(account, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.MemberNames.Contains("DisplayName"));
    }

    [Fact]
    public void Account_GetAvatarUrl_WithAvatar_ShouldReturnAvatar()
    {
        // Arrange
        var avatarUrl = "https://example.com/avatar.jpg";
        var account = new Account
        {
            Avatar = avatarUrl,
            PlatformId = SocialPlatform.BlueSky
        };

        // Act
        var result = account.GetAvatarUrl();

        // Assert
        result.Should().Be(avatarUrl);
    }

    [Fact]
    public void Account_GetAvatarUrl_WithoutAvatar_ShouldReturnGeneratedUrl()
    {
        // Arrange
        var account = new Account
        {
            PlatformId = SocialPlatform.BlueSky
        };

        // Act
        var result = account.GetAvatarUrl();

        // Assert
        result.Should().StartWith("https://api.dicebear.com/7.x/personas/svg?seed=");
        result.Should().Contain("bluesky");
        result.Should().Contain(account.Id);
    }

    [Fact]
    public void Account_IsAuthenticated_WithValidTokens_ShouldReturnTrue()
    {
        // Arrange
        var account = new Account
        {
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens
            {
                AccessToken = "valid_token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }
        };

        // Act
        var isAuthenticated = account.IsAuthenticated;

        // Assert
        isAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void Account_IsAuthenticated_WithExpiredTokens_ShouldReturnFalse()
    {
        // Arrange
        var account = new Account
        {
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens
            {
                AccessToken = "expired_token",
                ExpiresAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        // Act
        var isAuthenticated = account.IsAuthenticated;

        // Assert
        isAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void Account_IsAuthenticated_WithoutTokens_ShouldReturnFalse()
    {
        // Arrange
        var account = new Account
        {
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = null
        };

        // Act
        var isAuthenticated = account.IsAuthenticated;

        // Assert
        isAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void Account_IsAuthenticated_WithWrongStatus_ShouldReturnFalse()
    {
        // Arrange
        var account = new Account
        {
            AuthStatus = AuthenticationStatus.NotAuthenticated,
            Tokens = new OAuthTokens
            {
                AccessToken = "valid_token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }
        };

        // Act
        var isAuthenticated = account.IsAuthenticated;

        // Assert
        isAuthenticated.Should().BeFalse();
    }
}

/// <summary>
/// Unit tests for OAuthTokens class
/// </summary>
public class OAuthTokensTests
{
    [Fact]
    public void OAuthTokens_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var tokens = new OAuthTokens();

        // Assert
        tokens.AccessToken.Should().BeEmpty();
        tokens.RefreshToken.Should().BeNull();
        tokens.TokenType.Should().Be("Bearer");
        tokens.Scopes.Should().BeNull();
    }

    [Fact]
    public void OAuthTokens_IsExpired_WithExpiredToken_ShouldReturnTrue()
    {
        // Arrange
        var tokens = new OAuthTokens
        {
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };

        // Act
        var isExpired = tokens.IsExpired;

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void OAuthTokens_IsExpired_WithValidToken_ShouldReturnFalse()
    {
        // Arrange
        var tokens = new OAuthTokens
        {
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Act
        var isExpired = tokens.IsExpired;

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void OAuthTokens_IsExpired_WithTokenExpiringInFourMinutes_ShouldReturnTrue()
    {
        // Arrange - Token expires in 4 minutes, but we have 5 minute buffer
        var tokens = new OAuthTokens
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(4)
        };

        // Act
        var isExpired = tokens.IsExpired;

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void OAuthTokens_CanRefresh_WithRefreshToken_ShouldReturnTrue()
    {
        // Arrange
        var tokens = new OAuthTokens
        {
            RefreshToken = "refresh_token_value"
        };

        // Act
        var canRefresh = tokens.CanRefresh;

        // Assert
        canRefresh.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void OAuthTokens_CanRefresh_WithoutRefreshToken_ShouldReturnFalse(string? refreshToken)
    {
        // Arrange
        var tokens = new OAuthTokens
        {
            RefreshToken = refreshToken
        };

        // Act
        var canRefresh = tokens.CanRefresh;

        // Assert
        canRefresh.Should().BeFalse();
    }
}

/// <summary>
/// Unit tests for DefaultAccounts static class
/// </summary>
public class DefaultAccountsTests
{
    [Fact]
    public void DefaultAccounts_Accounts_ShouldContainAllPlatforms()
    {
        // Act
        var accounts = DefaultAccounts.Accounts;

        // Assert
        accounts.Should().ContainKey(SocialPlatform.BlueSky);
        accounts.Should().ContainKey(SocialPlatform.BlueSky);
        accounts.Should().ContainKey(SocialPlatform.BlueSky);
        accounts.Should().ContainKey(SocialPlatform.BlueSky);
        accounts.Should().ContainKey(SocialPlatform.BlueSky);
    }

    [Fact]
    public void DefaultAccounts_GetAllDefaultAccounts_ShouldReturnAllPlatforms()
    {
        // Act
        var accounts = DefaultAccounts.GetAllDefaultAccounts().ToList();

        // Assert
        accounts.Should().HaveCount(1);
        accounts.Should().Contain(a => a.PlatformId == SocialPlatform.BlueSky);

        accounts.Should().OnlyContain(a => a.IsDefault);
        accounts.Should().OnlyContain(a => !string.IsNullOrEmpty(a.Avatar));
    }

    [Theory]
    [InlineData(SocialPlatform.BlueSky, "bluesky-default")]
    // TODO: Add other platforms when implementations are ready
    public void DefaultAccounts_GetDefaultAccountForPlatform_ShouldReturnCorrectAccount(SocialPlatform platform, string expectedId)
    {
        // Act
        var account = DefaultAccounts.GetDefaultAccountForPlatform(platform);

        // Assert
        account.Should().NotBeNull();
        account!.Id.Should().Be(expectedId);
        account.PlatformId.Should().Be(platform);
        account.IsDefault.Should().BeTrue();
        account.Avatar.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void DefaultAccounts_GetDefaultAccountForPlatform_WithInvalidPlatform_ShouldReturnNull()
    {
        // Act
        var account = DefaultAccounts.GetDefaultAccountForPlatform((SocialPlatform)999);

        // Assert
        account.Should().BeNull();
    }

    [Fact]
    public void DefaultAccounts_CreatedAccounts_ShouldHaveRecentTimestamps()
    {
        // Act
        var account = DefaultAccounts.GetDefaultAccountForPlatform(SocialPlatform.BlueSky);

        // Assert
        account.Should().NotBeNull();
        account!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        account.LastUsed.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
