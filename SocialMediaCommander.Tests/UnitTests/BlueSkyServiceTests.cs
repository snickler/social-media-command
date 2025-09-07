using System;
using System.Net.Http;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Unit tests for BlueSkyService functionality
/// </summary>
public class BlueSkyServiceTests : IDisposable
{
    private readonly BlueSkyService _blueSkyService;
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly HttpClient _httpClient;

    public BlueSkyServiceTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _httpClient = new HttpClient();
        _blueSkyService = new BlueSkyService(_httpClient, _mockAuthService.Object);
    }

    [Fact]
    public void Platform_ShouldReturnBlueSky()
    {
        // Act & Assert
        _blueSkyService.Platform.Should().Be(SocialPlatform.BlueSky);
    }

    [Fact]
    public async Task PostAsync_UnauthenticatedAccount_ShouldReturnFailure()
    {
        // Arrange
        var post = CreateTestPost();
        var account = CreateTestAccount();
        account.AuthStatus = AuthenticationStatus.NotAuthenticated;

        // Act
        var result = await _blueSkyService.PostAsync(post, account);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not authenticated");
    }

    [Fact]
    public async Task CreateSessionAsync_WithValidAccount_ShouldAttemptSession()
    {
        // Arrange
        var account = CreateAuthenticatedTestAccount();

        // Act
        var result = await _blueSkyService.CreateSessionAsync(account);

        // Assert
        // Since we don't have real BlueSky credentials, this will fail
        // but we can test that it doesn't throw an exception
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetProfileAsync_UnauthenticatedAccount_ShouldReturnNull()
    {
        // Arrange
        var account = CreateTestAccount();
        account.AuthStatus = AuthenticationStatus.NotAuthenticated;

        // Act
        var result = await _blueSkyService.GetProfileAsync(account);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Constructor_NullHttpClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new BlueSkyService(null!, _mockAuthService.Object));
    }

    [Fact]
    public void Constructor_NullAuthService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new BlueSkyService(_httpClient, null!));
    }

    [Fact]
    public async Task PostAsync_WithValidAccountButNoSession_ShouldReturnFailure()
    {
        // Arrange
        var post = CreateTestPost();
        var account = CreateAuthenticatedTestAccount();

        // Act
        var result = await _blueSkyService.PostAsync(post, account);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("session");
    }

    private static Post CreateTestPost()
    {
        return new Post
        {
            Content = "This is a test post for BlueSky",
            TargetPlatforms = [SocialPlatform.BlueSky]
        };
    }

    private static Account CreateTestAccount()
    {
        return new Account
        {
            Id = "test-bluesky-account",
            PlatformId = SocialPlatform.BlueSky,
            Username = "testuser.bsky.social",
            DisplayName = "Test User",
            AuthStatus = AuthenticationStatus.NotAuthenticated
        };
    }

    private static Account CreateAuthenticatedTestAccount()
    {
        return new Account
        {
            Id = "test-bluesky-account",
            PlatformId = SocialPlatform.BlueSky,
            Username = "testuser.bsky.social",
            DisplayName = "Test User",
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens
            {
                AccessToken = "test_access_token",
                TokenType = "Bearer",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}