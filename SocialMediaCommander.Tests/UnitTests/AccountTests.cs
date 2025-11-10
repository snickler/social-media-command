using FluentAssertions;
using SocialMediaCommander.Core.Models;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Tests for Account model and related classes
/// </summary>
public class AccountTests
{
    #region Account Constructor Tests

    [Fact]
    public void Account_Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var account = new Account();

        // Assert
        account.Id.Should().NotBeNullOrEmpty();
        account.Username.Should().BeEmpty();
        account.DisplayName.Should().BeEmpty();
        account.Avatar.Should().BeNull();
        account.IsDefault.Should().BeFalse();
        account.AuthStatus.Should().Be(AuthenticationStatus.NotAuthenticated);
        account.AuthMethod.Should().Be(AuthenticationMethod.OAuth);
        account.Tokens.Should().BeNull();
        account.AppPassword.Should().BeNull();
        account.OAuthConfiguration.Should().BeNull();
        account.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Account_Constructor_ShouldSetCreatedAt()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var account = new Account();

        // Assert
        account.CreatedAt.Should().BeOnOrAfter(before);
        account.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    #endregion

    #region GetAvatarUrl Tests

    [Fact]
    public void GetAvatarUrl_ShouldReturnCustomAvatar_WhenSet()
    {
        // Arrange
        var account = new Account
        {
            Avatar = "https://example.com/avatar.jpg"
        };

        // Act
        var url = account.GetAvatarUrl();

        // Assert
        url.Should().Be("https://example.com/avatar.jpg");
    }

    [Fact]
    public void GetAvatarUrl_ShouldGenerateDefault_WhenNotSet()
    {
        // Arrange
        var account = new Account
        {
            Id = "test123",
            PlatformId = SocialPlatform.BlueSky
        };

        // Act
        var url = account.GetAvatarUrl();

        // Assert
        url.Should().Contain("dicebear.com");
        url.Should().Contain("bluesky-test123");
    }

    #endregion

    #region IsAuthenticated Tests

    [Fact]
    public void IsAuthenticated_ShouldReturnFalse_WhenNotAuthenticated()
    {
        // Arrange
        var account = new Account
        {
            AuthStatus = AuthenticationStatus.NotAuthenticated
        };

        // Act
        var result = account.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnTrue_WhenOAuthWithValidTokens()
    {
        // Arrange
        var account = new Account
        {
            AuthStatus = AuthenticationStatus.Authenticated,
            AuthMethod = AuthenticationMethod.OAuth,
            Tokens = new OAuthTokens
            {
                AccessToken = "test-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }
        };

        // Act
        var result = account.IsAuthenticated;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnFalse_WhenOAuthTokenExpired()
    {
        // Arrange
        var account = new Account
        {
            AuthStatus = AuthenticationStatus.Authenticated,
            AuthMethod = AuthenticationMethod.OAuth,
            Tokens = new OAuthTokens
            {
                AccessToken = "test-token",
                ExpiresAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        // Act
        var result = account.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnTrue_WhenAppPasswordSet()
    {
        // Arrange
        var account = new Account
        {
            AuthStatus = AuthenticationStatus.Authenticated,
            AuthMethod = AuthenticationMethod.AppPassword,
            AppPassword = "my-app-password"
        };

        // Act
        var result = account.IsAuthenticated;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnFalse_WhenAppPasswordEmpty()
    {
        // Arrange
        var account = new Account
        {
            AuthStatus = AuthenticationStatus.Authenticated,
            AuthMethod = AuthenticationMethod.AppPassword,
            AppPassword = ""
        };

        // Act
        var result = account.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnTrue_WhenApiKeySet()
    {
        // Arrange
        var account = new Account
        {
            AuthStatus = AuthenticationStatus.Authenticated,
            AuthMethod = AuthenticationMethod.ApiKey,
            Metadata = new Dictionary<string, string>
            {
                { "ApiKey", "my-api-key" }
            }
        };

        // Act
        var result = account.IsAuthenticated;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnTrue_WhenUsernamePasswordSet()
    {
        // Arrange
        var account = new Account
        {
            AuthStatus = AuthenticationStatus.Authenticated,
            AuthMethod = AuthenticationMethod.UsernamePassword,
            Metadata = new Dictionary<string, string>
            {
                { "Password", "my-password" }
            }
        };

        // Act
        var result = account.IsAuthenticated;

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region OAuthTokens Tests

    [Fact]
    public void OAuthTokens_Constructor_ShouldInitializeWithDefaults()
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
    public void OAuthTokens_IsExpired_ShouldReturnFalse_WhenNotExpired()
    {
        // Arrange
        var tokens = new OAuthTokens
        {
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Act
        var result = tokens.IsExpired;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void OAuthTokens_IsExpired_ShouldReturnTrue_WhenExpired()
    {
        // Arrange
        var tokens = new OAuthTokens
        {
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };

        // Act
        var result = tokens.IsExpired;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void OAuthTokens_IsExpired_ShouldReturnTrue_WhenWithinBuffer()
    {
        // Arrange - Token expires in 3 minutes (within 5-minute buffer)
        var tokens = new OAuthTokens
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(3)
        };

        // Act
        var result = tokens.IsExpired;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void OAuthTokens_CanRefresh_ShouldReturnTrue_WhenRefreshTokenExists()
    {
        // Arrange
        var tokens = new OAuthTokens
        {
            RefreshToken = "refresh-token"
        };

        // Act
        var result = tokens.CanRefresh;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void OAuthTokens_CanRefresh_ShouldReturnFalse_WhenRefreshTokenNull()
    {
        // Arrange
        var tokens = new OAuthTokens
        {
            RefreshToken = null
        };

        // Act
        var result = tokens.CanRefresh;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void OAuthTokens_CanRefresh_ShouldReturnFalse_WhenRefreshTokenEmpty()
    {
        // Arrange
        var tokens = new OAuthTokens
        {
            RefreshToken = ""
        };

        // Act
        var result = tokens.CanRefresh;

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region DefaultAccounts Tests

    [Fact]
    public void DefaultAccounts_GetAllDefaultAccounts_ShouldReturnAccounts()
    {
        // Act
        var accounts = DefaultAccounts.GetAllDefaultAccounts();

        // Assert
        accounts.Should().NotBeNull();
        accounts.Should().NotBeEmpty();
        accounts.Should().AllSatisfy(a => a.IsDefault.Should().BeTrue());
    }

    [Fact]
    public void DefaultAccounts_GetDefaultAccountForPlatform_ShouldReturnAccount_WhenExists()
    {
        // Act
        var account = DefaultAccounts.GetDefaultAccountForPlatform(SocialPlatform.BlueSky);

        // Assert
        account.Should().NotBeNull();
        account!.PlatformId.Should().Be(SocialPlatform.BlueSky);
        account.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void DefaultAccounts_GetDefaultAccountForPlatform_ShouldReturnClone()
    {
        // Act
        var account1 = DefaultAccounts.GetDefaultAccountForPlatform(SocialPlatform.BlueSky);
        var account2 = DefaultAccounts.GetDefaultAccountForPlatform(SocialPlatform.BlueSky);

        // Assert
        account1.Should().NotBeNull();
        account2.Should().NotBeNull();
        // Should be different instances
        account1.Should().NotBeSameAs(account2);
        // But with same properties
        account1!.PlatformId.Should().Be(account2!.PlatformId);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public void Account_ShouldSupportMetadata()
    {
        // Arrange
        var account = new Account
        {
            Metadata = new Dictionary<string, string>
            {
                { "Key1", "Value1" },
                { "Key2", "Value2" }
            }
        };

        // Act & Assert
        account.Metadata.Should().HaveCount(2);
        account.Metadata["Key1"].Should().Be("Value1");
    }

    [Fact]
    public void Account_ComplexScenario_FullyAuthenticatedOAuth()
    {
        // Arrange
        var account = new Account
        {
            Id = "test-account",
            PlatformId = SocialPlatform.BlueSky,
            Username = "testuser",
            DisplayName = "Test User",
            Avatar = "https://example.com/avatar.jpg",
            AuthStatus = AuthenticationStatus.Authenticated,
            AuthMethod = AuthenticationMethod.OAuth,
            Tokens = new OAuthTokens
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                TokenType = "Bearer",
                Scopes = new[] { "read", "write" }
            }
        };

        // Act
        var isAuthenticated = account.IsAuthenticated;
        var avatarUrl = account.GetAvatarUrl();

        // Assert
        isAuthenticated.Should().BeTrue();
        avatarUrl.Should().Be("https://example.com/avatar.jpg");
        account.Tokens!.CanRefresh.Should().BeTrue();
        account.Tokens.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void Account_ComplexScenario_AppPasswordAuthentication()
    {
        // Arrange
        var account = new Account
        {
            PlatformId = SocialPlatform.BlueSky,
            Username = "bluesky-user",
            DisplayName = "BlueSky User",
            AuthStatus = AuthenticationStatus.Authenticated,
            AuthMethod = AuthenticationMethod.AppPassword,
            AppPassword = "my-app-password"
        };

        // Act
        var isAuthenticated = account.IsAuthenticated;

        // Assert
        isAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void OAuthTokens_ShouldHandleScopes()
    {
        // Arrange
        var tokens = new OAuthTokens
        {
            AccessToken = "token",
            Scopes = new[] { "read", "write", "delete" }
        };

        // Act & Assert
        tokens.Scopes.Should().HaveCount(3);
        tokens.Scopes.Should().Contain("read");
    }

    [Fact]
    public void Account_ShouldTrackLastUsed()
    {
        // Arrange
        var account = new Account();
        var initialLastUsed = account.LastUsed;

        // Act
        System.Threading.Thread.Sleep(10);
        account.LastUsed = DateTime.UtcNow;

        // Assert
        account.LastUsed.Should().BeAfter(initialLastUsed);
    }

    #endregion
}
