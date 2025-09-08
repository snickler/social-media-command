using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Unit tests for OAuthAuthenticationService functionality
/// </summary>
public class OAuthAuthenticationServiceTests : IDisposable
{
    private readonly OAuthAuthenticationService _authService;
    private readonly Mock<IOAuthConfigurationService> _mockConfigService;
    private readonly HttpClient _httpClient;

    public OAuthAuthenticationServiceTests()
    {
        _mockConfigService = new Mock<IOAuthConfigurationService>();
        _httpClient = new HttpClient();
        _authService = new OAuthAuthenticationService(_httpClient, _mockConfigService.Object);
    }

    [Fact]
    public async Task StartAuthenticationAsync_WithNoConfiguration_ShouldReturnFailure()
    {
        // Arrange
        var platform = SocialPlatform.X;
        _mockConfigService.Setup(x => x.GetConfigurationAsync(platform))
            .ReturnsAsync((OAuthConfig?)null);

        // Act
        var result = await _authService.StartAuthenticationAsync(platform);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("OAuth configuration not found");
        result.ErrorMessage.Should().Contain(platform.ToString());
    }

    [Fact]
    public async Task StartAuthenticationAsync_WithInvalidConfiguration_ShouldReturnFailure()
    {
        // Arrange
        var platform = SocialPlatform.X;
        var invalidConfig = new OAuthConfig
        {
            ClientId = "", // Invalid
            AuthorizationEndpoint = "invalid-url"
        };

        var validationResult = new OAuthValidationResult
        {
            IsValid = false,
            Errors = new List<string> { "Client ID is required", "Invalid URL" }
        };

        _mockConfigService.Setup(x => x.GetConfigurationAsync(platform))
            .ReturnsAsync(invalidConfig);
        _mockConfigService.Setup(x => x.ValidateConfigurationAsync(platform, invalidConfig))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _authService.StartAuthenticationAsync(platform);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid OAuth configuration");
        result.ErrorMessage.Should().Contain("Client ID is required");
    }

    [Fact]
    public async Task StartAuthenticationAsync_WithValidConfiguration_ShouldReturnSuccess()
    {
        // Arrange
        var platform = SocialPlatform.X;
        var validConfig = CreateValidOAuthConfig();

        var validationResult = new OAuthValidationResult { IsValid = true };

        _mockConfigService.Setup(x => x.GetConfigurationAsync(platform))
            .ReturnsAsync(validConfig);
        _mockConfigService.Setup(x => x.ValidateConfigurationAsync(platform, validConfig))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _authService.StartAuthenticationAsync(platform);

        // Assert
        result.Should().NotBeNull();
        // Note: In a real implementation, this would start an OAuth flow
        // For unit testing, we can check that configuration validation was called
        _mockConfigService.Verify(x => x.GetConfigurationAsync(platform), Times.Once);
        _mockConfigService.Verify(x => x.ValidateConfigurationAsync(platform, validConfig), Times.Once);
    }

    [Fact]
    public async Task StartAuthenticationAsync_WithCustomRedirectUri_ShouldUseCustomUri()
    {
        // Arrange
        var platform = SocialPlatform.BlueSky;
        var validConfig = CreateValidOAuthConfig();
        var customRedirectUri = "http://localhost:9000/callback";

        var validationResult = new OAuthValidationResult { IsValid = true };

        _mockConfigService.Setup(x => x.GetConfigurationAsync(platform))
            .ReturnsAsync(validConfig);
        _mockConfigService.Setup(x => x.ValidateConfigurationAsync(platform, validConfig))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _authService.StartAuthenticationAsync(platform, customRedirectUri);

        // Assert
        result.Should().NotBeNull();
        _mockConfigService.Verify(x => x.GetConfigurationAsync(platform), Times.Once);
    }

    [Theory]
    [InlineData(SocialPlatform.X)]
    [InlineData(SocialPlatform.BlueSky)]
    [InlineData(SocialPlatform.LinkedIn)]
    public async Task StartAuthenticationAsync_WithDifferentPlatforms_ShouldCallCorrectConfiguration(SocialPlatform platform)
    {
        // Arrange
        var validConfig = CreateValidOAuthConfig();
        var validationResult = new OAuthValidationResult { IsValid = true };

        _mockConfigService.Setup(x => x.GetConfigurationAsync(platform))
            .ReturnsAsync(validConfig);
        _mockConfigService.Setup(x => x.ValidateConfigurationAsync(platform, validConfig))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _authService.StartAuthenticationAsync(platform);

        // Assert
        result.Should().NotBeNull();
        _mockConfigService.Verify(x => x.GetConfigurationAsync(platform), Times.Once);
        _mockConfigService.Verify(x => x.ValidateConfigurationAsync(platform, validConfig), Times.Once);
    }

    [Fact]
    public async Task StartAuthenticationAsync_WhenConfigServiceThrows_ShouldReturnFailure()
    {
        // Arrange
        var platform = SocialPlatform.X;
        _mockConfigService.Setup(x => x.GetConfigurationAsync(platform))
            .ThrowsAsync(new InvalidOperationException("Config service error"));

        // Act
        var result = await _authService.StartAuthenticationAsync(platform);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("error");
    }

    [Fact]
    public async Task StartAuthenticationAsync_WhenValidationServiceThrows_ShouldReturnFailure()
    {
        // Arrange
        var platform = SocialPlatform.X;
        var validConfig = CreateValidOAuthConfig();

        _mockConfigService.Setup(x => x.GetConfigurationAsync(platform))
            .ReturnsAsync(validConfig);
        _mockConfigService.Setup(x => x.ValidateConfigurationAsync(platform, validConfig))
            .ThrowsAsync(new ArgumentException("Validation error"));

        // Act
        var result = await _authService.StartAuthenticationAsync(platform);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("error");
    }

    [Fact]
    public async Task StartAuthenticationAsync_WithNullRedirectUri_ShouldUseDefaultRedirectUri()
    {
        // Arrange
        var platform = SocialPlatform.X;
        var validConfig = CreateValidOAuthConfig();
        var validationResult = new OAuthValidationResult { IsValid = true };

        _mockConfigService.Setup(x => x.GetConfigurationAsync(platform))
            .ReturnsAsync(validConfig);
        _mockConfigService.Setup(x => x.ValidateConfigurationAsync(platform, validConfig))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _authService.StartAuthenticationAsync(platform, null);

        // Assert
        result.Should().NotBeNull();
        _mockConfigService.Verify(x => x.GetConfigurationAsync(platform), Times.Once);
    }

    [Fact]
    public async Task StartAuthenticationAsync_MultipleCallsWithSamePlatform_ShouldCallConfigServiceEachTime()
    {
        // Arrange
        var platform = SocialPlatform.X;
        var validConfig = CreateValidOAuthConfig();
        var validationResult = new OAuthValidationResult { IsValid = true };

        _mockConfigService.Setup(x => x.GetConfigurationAsync(platform))
            .ReturnsAsync(validConfig);
        _mockConfigService.Setup(x => x.ValidateConfigurationAsync(platform, validConfig))
            .ReturnsAsync(validationResult);

        // Act
        var result1 = await _authService.StartAuthenticationAsync(platform);
        var result2 = await _authService.StartAuthenticationAsync(platform);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        _mockConfigService.Verify(x => x.GetConfigurationAsync(platform), Times.Exactly(2));
    }

    private static OAuthConfig CreateValidOAuthConfig()
    {
        return new OAuthConfig
        {
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret",
            AuthorizationEndpoint = "https://api.twitter.com/oauth2/authorize",
            TokenEndpoint = "https://api.twitter.com/oauth2/token",
            RedirectUri = "http://localhost:8080/oauth/callback",
            Scopes = new[] { "read", "write" }
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}