using Microsoft.Extensions.DependencyInjection;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Tests.Integration;

/// <summary>
/// Integration tests for all platform services
/// </summary>
public class PlatformServicesIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly HttpClient _httpClient;
    private readonly IBlueSkyService _blueSkyService;
    // TODO: Add Twitter, LinkedIn, Threads, Facebook when implementations are ready

    public PlatformServicesIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IOAuthConfigurationService, TestOAuthConfigurationService>();
        services.AddSingleton<IAuthenticationService, TestAuthenticationService>();
        services.AddSingleton<IBlueSkyService, BlueSkyService>();
        // TODO: Add Twitter, LinkedIn, Threads, Facebook services when implementations are ready

        _serviceProvider = services.BuildServiceProvider();
        _httpClient = _serviceProvider.GetRequiredService<HttpClient>();
        _blueSkyService = _serviceProvider.GetRequiredService<IBlueSkyService>();
    }

    #region BlueSky Service Tests

    [Fact]
    public async Task BlueSkyService_PostAsync_ShouldReturnError_WithoutAuthentication()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test post for BlueSky",
            TargetPlatforms = [SocialPlatform.BlueSky]
        };

        var account = new Account
        {
            PlatformId = SocialPlatform.BlueSky,
            Username = "test_user",
            DisplayName = "Test User"
        };

        // Act
        var result = await _blueSkyService.PostAsync(post, account);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not authenticated");
    }

    [Fact]
    public async Task BlueSkyService_ValidateContentAsync_ShouldEnforceCharacterLimit()
    {
        // Arrange
        var longContent = new string('A', 301); // Exceeds 300 character limit
        var post = new Post { Content = longContent };

        // Act
        var result = await _blueSkyService.ValidateContentAsync(post);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("300 characters"));
    }

    [Fact]
    public async Task BlueSkyService_ValidateContentAsync_ShouldPassValidContent()
    {
        // Arrange
        var validContent = "This is a valid BlueSky post under 300 characters.";
        var post = new Post { Content = validContent };

        // Act
        var result = await _blueSkyService.ValidateContentAsync(post);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task BlueSkyService_GetPlatformLimitsAsync_ShouldReturnCorrectLimits()
    {
        // Act
        var limits = await _blueSkyService.GetPlatformLimitsAsync();

        // Assert
        limits.Should().NotBeNull();
        limits.CharacterLimit.Should().Be(300);
        limits.MaxMediaCount.Should().Be(4);
        limits.SupportedMediaTypes.Should().Contain("image/jpeg");
        limits.SupportedMediaTypes.Should().Contain("image/png");
        limits.MaxThreadLength.Should().Be(25);
    }

    [Fact]
    public async Task BlueSkyService_CreateSessionAsync_ShouldReturnError_WithInvalidCredentials()
    {
        // Arrange
        var account = new Account
        {
            PlatformId = SocialPlatform.BlueSky,
            Username = "invalid@example.com",
            DisplayName = "Invalid User"
        };

        // Act
        var result = await _blueSkyService.CreateSessionAsync(account);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    // TODO: Add Twitter, LinkedIn, Threads, Facebook service tests when implementations are ready

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
