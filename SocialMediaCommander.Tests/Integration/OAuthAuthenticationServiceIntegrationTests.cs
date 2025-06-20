using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using SocialMediaCommander.Services.Interfaces;
using FluentAssertions;

namespace SocialMediaCommander.Tests.Integration;

/// <summary>
/// Integration tests for OAuth authentication service
/// </summary>
public class OAuthAuthenticationServiceIntegrationTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationService _authService;
    private readonly ServiceProvider _serviceProvider;

    public OAuthAuthenticationServiceIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IOAuthConfigurationService, OAuthConfigurationService>();
        services.AddSingleton<IAuthenticationService>(provider =>
            new OAuthAuthenticationService(
                provider.GetRequiredService<HttpClient>(),
                provider.GetRequiredService<IOAuthConfigurationService>()
            ));
        
        _serviceProvider = services.BuildServiceProvider();
        _httpClient = _serviceProvider.GetRequiredService<HttpClient>();
        _authService = _serviceProvider.GetRequiredService<IAuthenticationService>();
    }

    [Theory]
    [InlineData(SocialPlatform.BlueSky)]
    [InlineData(SocialPlatform.X)]
    [InlineData(SocialPlatform.LinkedIn)]
    [InlineData(SocialPlatform.Threads)]
    [InlineData(SocialPlatform.Facebook)]
    public async Task StartAuthenticationAsync_ShouldReturnValidAuthorizationUrl_ForAllPlatforms(SocialPlatform platform)
    {
        // Act
        var result = await _authService.StartAuthenticationAsync(platform);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.AuthorizationUrl.Should().NotBeNullOrEmpty();
        result.State.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNullOrEmpty();

        // Verify URL structure
        var uri = new Uri(result.AuthorizationUrl!);
        uri.Should().NotBeNull();
        uri.Scheme.Should().Be("https");
        
        // Verify query parameters
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        query["client_id"].Should().NotBeNullOrEmpty();
        query["redirect_uri"].Should().NotBeNullOrEmpty();
        query["response_type"].Should().Be("code");
        query["state"].Should().Be(result.State);
        query["scope"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task StartAuthenticationAsync_ShouldReturnError_ForInvalidPlatform()
    {
        // Arrange
        var invalidPlatform = (SocialPlatform)999;

        // Act
        var result = await _authService.StartAuthenticationAsync(invalidPlatform);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("OAuth configuration not found");
        result.AuthorizationUrl.Should().BeNullOrEmpty();
    }

    [Theory]
    [InlineData(SocialPlatform.BlueSky)]
    [InlineData(SocialPlatform.X)]
    [InlineData(SocialPlatform.LinkedIn)]
    [InlineData(SocialPlatform.Threads)]
    [InlineData(SocialPlatform.Facebook)]
    public void GetOAuthConfig_ShouldReturnValidConfiguration_ForAllPlatforms(SocialPlatform platform)
    {
        // Act
        var config = _authService.GetOAuthConfig(platform);

        // Assert
        config.Should().NotBeNull();
        config.ClientId.Should().NotBeNullOrEmpty();
        config.ClientSecret.Should().NotBeNullOrEmpty();
        config.AuthorizationEndpoint.Should().NotBeNullOrEmpty();
        config.TokenEndpoint.Should().NotBeNullOrEmpty();
        config.UserInfoEndpoint.Should().NotBeNullOrEmpty();
        config.RedirectUri.Should().NotBeNullOrEmpty();
        config.Scopes.Should().NotBeEmpty();

        // Verify endpoints are valid URLs
        Uri.IsWellFormedUriString(config.AuthorizationEndpoint, UriKind.Absolute).Should().BeTrue();
        Uri.IsWellFormedUriString(config.TokenEndpoint, UriKind.Absolute).Should().BeTrue();
        Uri.IsWellFormedUriString(config.UserInfoEndpoint, UriKind.Absolute).Should().BeTrue();
        Uri.IsWellFormedUriString(config.RedirectUri, UriKind.Absolute).Should().BeTrue();
    }

    [Fact]
    public async Task CompleteAuthenticationAsync_ShouldReturnError_WithInvalidAuthorizationCode()
    {
        // Arrange
        var platform = SocialPlatform.BlueSky;
        var invalidCode = "invalid_code_12345";
        var state = "test_state";

        // Act
        var result = await _authService.CompleteAuthenticationAsync(platform, invalidCode, state);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed to exchange authorization code");
        result.Tokens.Should().BeNull();
        result.UserProfile.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_ShouldReturnFalse_WithInvalidToken()
    {
        // Arrange
        var platform = SocialPlatform.BlueSky;
        var invalidToken = "invalid_token_12345";

        // Act
        var result = await _authService.ValidateTokenAsync(platform, invalidToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeTokenAsync_ShouldReturnFalse_WithInvalidToken()
    {
        // Arrange
        var platform = SocialPlatform.BlueSky;
        var invalidToken = "invalid_token_12345";

        // Act
        var result = await _authService.RevokeTokenAsync(platform, invalidToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnError_WithInvalidRefreshToken()
    {
        // Arrange
        var platform = SocialPlatform.BlueSky;
        var invalidRefreshToken = "invalid_refresh_token_12345";

        // Act
        var result = await _authService.RefreshTokenAsync(platform, invalidRefreshToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Not implemented");
    }

    [Fact]
    public async Task GetUserProfileAsync_ShouldReturnNull_WithInvalidToken()
    {
        // Arrange
        var platform = SocialPlatform.BlueSky;
        var invalidToken = "invalid_token_12345";

        // Act
        var result = await _authService.GetUserProfileAsync(platform, invalidToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void OAuthConfig_ShouldHaveCorrectPlatformSpecificEndpoints()
    {
        // Test BlueSky configuration
        var blueSkyConfig = _authService.GetOAuthConfig(SocialPlatform.BlueSky);
        blueSkyConfig.AuthorizationEndpoint.Should().Contain("bsky.social");
        blueSkyConfig.Scopes.Should().Contain("read");
        blueSkyConfig.Scopes.Should().Contain("write");

        // Test X/Twitter configuration
        var twitterConfig = _authService.GetOAuthConfig(SocialPlatform.X);
        twitterConfig.AuthorizationEndpoint.Should().Contain("twitter.com");
        twitterConfig.Scopes.Should().Contain("tweet.read");
        twitterConfig.Scopes.Should().Contain("tweet.write");

        // Test LinkedIn configuration
        var linkedInConfig = _authService.GetOAuthConfig(SocialPlatform.LinkedIn);
        linkedInConfig.AuthorizationEndpoint.Should().Contain("linkedin.com");
        linkedInConfig.Scopes.Should().Contain("r_liteprofile");
        linkedInConfig.Scopes.Should().Contain("w_member_social");

        // Test Threads configuration
        var threadsConfig = _authService.GetOAuthConfig(SocialPlatform.Threads);
        threadsConfig.AuthorizationEndpoint.Should().Contain("threads.net");
        threadsConfig.Scopes.Should().Contain("threads_basic");

        // Test Facebook configuration
        var facebookConfig = _authService.GetOAuthConfig(SocialPlatform.Facebook);
        facebookConfig.AuthorizationEndpoint.Should().Contain("facebook.com");
        facebookConfig.Scopes.Should().Contain("pages_manage_posts");
    }

    [Fact]
    public void AuthenticationResult_ShouldHaveCorrectDefaultValues()
    {
        // Act
        var result = new AuthenticationResult();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
        result.Tokens.Should().BeNull();
        result.UserProfile.Should().BeNull();
        result.AuthorizationUrl.Should().BeNull();
        result.State.Should().BeNull();
    }

    [Fact]
    public void OAuthTokens_ShouldHandleExpirationCorrectly()
    {
        // Arrange
        var tokens = new OAuthTokens
        {
            AccessToken = "test_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // Expired
            RefreshToken = "refresh_token"
        };

        // Act & Assert
        tokens.IsExpired.Should().BeTrue();
        tokens.CanRefresh.Should().BeTrue();

        // Test non-expired token
        tokens.ExpiresAt = DateTime.UtcNow.AddHours(1);
        tokens.IsExpired.Should().BeFalse();

        // Test token without refresh capability
        tokens.RefreshToken = null;
        tokens.CanRefresh.Should().BeFalse();
    }

    [Fact]
    public void UserProfile_ShouldBeCreatedWithCorrectPlatform()
    {
        // Arrange & Act
        var profile = new UserProfile
        {
            Id = "123",
            Username = "testuser",
            DisplayName = "Test User",
            Platform = SocialPlatform.BlueSky,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        profile.Id.Should().Be("123");
        profile.Username.Should().Be("testuser");
        profile.DisplayName.Should().Be("Test User");
        profile.Platform.Should().Be(SocialPlatform.BlueSky);
        profile.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    public void Dispose()
    {
        (_authService as IDisposable)?.Dispose();
        _serviceProvider?.Dispose();
    }
} 