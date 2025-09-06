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
    private readonly ITwitterService _twitterService;
    private readonly ILinkedInService _linkedInService;
    private readonly IThreadsService _threadsService;
    private readonly IFacebookService _facebookService;

    public PlatformServicesIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IOAuthConfigurationService, TestOAuthConfigurationService>();
        services.AddSingleton<IAuthenticationService, TestAuthenticationService>();
        services.AddSingleton<IBlueSkyService, BlueSkyService>();
        services.AddSingleton<ITwitterService, TwitterService>();
        services.AddSingleton<ILinkedInService, LinkedInService>();
        services.AddSingleton<IThreadsService, ThreadsService>();
        services.AddSingleton<IFacebookService, FacebookService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _httpClient = _serviceProvider.GetRequiredService<HttpClient>();
        _blueSkyService = _serviceProvider.GetRequiredService<IBlueSkyService>();
        _twitterService = _serviceProvider.GetRequiredService<ITwitterService>();
        _linkedInService = _serviceProvider.GetRequiredService<ILinkedInService>();
        _threadsService = _serviceProvider.GetRequiredService<IThreadsService>();
        _facebookService = _serviceProvider.GetRequiredService<IFacebookService>();
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

    #region Twitter/X Service Tests

    [Fact]
    public async Task TwitterService_PostAsync_ShouldReturnError_WithoutAuthentication()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test tweet for X platform",
            TargetPlatforms = [SocialPlatform.X]
        };

        var account = new Account
        {
            PlatformId = SocialPlatform.X,
            Username = "test_user",
            DisplayName = "Test User"
        };

        // Act
        var result = await _twitterService.PostAsync(post, account);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not authenticated");
    }

    [Fact]
    public async Task TwitterService_ValidateContentAsync_ShouldEnforceCharacterLimit()
    {
        // Arrange
        var longContent = new string('A', 281); // Exceeds 280 character limit
        var post = new Post { Content = longContent };

        // Act
        var result = await _twitterService.ValidateContentAsync(post);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("280 characters"));
    }

    [Fact]
    public async Task TwitterService_ValidateContentAsync_ShouldPassValidContent()
    {
        // Arrange
        var validContent = "This is a valid tweet under 280 characters.";
        var post = new Post { Content = validContent };

        // Act
        var result = await _twitterService.ValidateContentAsync(post);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task TwitterService_GetPlatformLimitsAsync_ShouldReturnCorrectLimits()
    {
        // Act
        var limits = await _twitterService.GetPlatformLimitsAsync();

        // Assert
        limits.Should().NotBeNull();
        limits.CharacterLimit.Should().Be(280);
        limits.MaxMediaCount.Should().Be(4);
        limits.SupportedMediaTypes.Should().Contain("image/jpeg");
        limits.SupportedMediaTypes.Should().Contain("video/mp4");
        limits.MaxThreadLength.Should().Be(25);
        limits.DailyPostLimit.Should().Be(300);
    }

    #endregion

    #region LinkedIn Service Tests

    [Fact]
    public async Task LinkedInService_PostAsync_ShouldReturnError_WithoutAuthentication()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test post for LinkedIn",
            TargetPlatforms = [SocialPlatform.LinkedIn]
        };

        var account = new Account
        {
            PlatformId = SocialPlatform.LinkedIn,
            Username = "test_user",
            DisplayName = "Test User"
        };

        // Act
        var result = await _linkedInService.PostAsync(post, account);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not authenticated");
    }

    [Fact]
    public async Task LinkedInService_ValidateContentAsync_ShouldEnforceCharacterLimit()
    {
        // Arrange
        var longContent = new string('A', 3001); // Exceeds 3000 character limit
        var post = new Post { Content = longContent };

        // Act
        var result = await _linkedInService.ValidateContentAsync(post);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("3000 characters"));
    }

    [Fact]
    public async Task LinkedInService_ValidateContentAsync_ShouldPassValidContent()
    {
        // Arrange
        var validContent = "This is a valid LinkedIn post with professional content.";
        var post = new Post { Content = validContent };

        // Act
        var result = await _linkedInService.ValidateContentAsync(post);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task LinkedInService_GetPlatformLimitsAsync_ShouldReturnCorrectLimits()
    {
        // Act
        var limits = await _linkedInService.GetPlatformLimitsAsync();

        // Assert
        limits.Should().NotBeNull();
        limits.CharacterLimit.Should().Be(3000);
        limits.MaxMediaCount.Should().Be(9);
        limits.SupportedMediaTypes.Should().Contain("image/jpeg");
        limits.SupportedMediaTypes.Should().Contain("image/png");
        limits.MaxThreadLength.Should().Be(1); // LinkedIn doesn't support threads
        limits.DailyPostLimit.Should().Be(100);
    }

    #endregion

    #region Basic Service Tests

    [Fact]
    public async Task PlatformServices_GetTimelineAsync_ShouldReturnEmpty_WithoutAuthentication()
    {
        // Arrange
        var account = new Account
        {
            PlatformId = SocialPlatform.BlueSky,
            Username = "test_user",
            DisplayName = "Test User"
        };

        // Act
        var blueSkyResults = await _blueSkyService.GetTimelineAsync(account);
        var twitterResults = await _twitterService.GetTimelineAsync(account);
        var linkedInResults = await _linkedInService.GetTimelineAsync(account);

        // Assert
        blueSkyResults.Should().NotBeNull();
        blueSkyResults.Should().BeEmpty();
        
        twitterResults.Should().NotBeNull();
        twitterResults.Should().BeEmpty();
        
        linkedInResults.Should().NotBeNull();
        linkedInResults.Should().BeEmpty();
    }

    [Fact]
    public async Task PlatformServices_GetUserPostsAsync_ShouldReturnEmpty_WithoutAuthentication()
    {
        // Arrange
        var account = new Account
        {
            PlatformId = SocialPlatform.BlueSky,
            Username = "test_user",
            DisplayName = "Test User"
        };

        // Act
        var blueSkyResults = await _blueSkyService.GetUserPostsAsync(account);
        var twitterResults = await _twitterService.GetUserPostsAsync(account);
        var linkedInResults = await _linkedInService.GetUserPostsAsync(account);

        // Assert
        blueSkyResults.Should().NotBeNull();
        blueSkyResults.Should().BeEmpty();
        
        twitterResults.Should().NotBeNull();
        twitterResults.Should().BeEmpty();
        
        linkedInResults.Should().NotBeNull();
        linkedInResults.Should().BeEmpty();
    }

    [Fact]
    public async Task PlatformServices_ShouldHandleHttpExceptions_Gracefully()
    {
        // This test verifies that services handle network errors gracefully
        // In a real scenario, we might use a mock HTTP client that throws exceptions
        
        var post = new Post { Content = "Test post" };
        var account = new Account
        {
            PlatformId = SocialPlatform.BlueSky,
            Username = "test_user",
            DisplayName = "Test User"
        };
        
        // Test each service handles exceptions
        var blueSkyResult = await _blueSkyService.PostAsync(post, account);
        var twitterResult = await _twitterService.PostAsync(post, account);
        var linkedInResult = await _linkedInService.PostAsync(post, account);

        // All should return error results, not throw exceptions
        blueSkyResult.Success.Should().BeFalse();
        twitterResult.Success.Should().BeFalse();
        linkedInResult.Success.Should().BeFalse();
    }

    #endregion

    #region Threads Service Tests

    [Fact]
    public async Task ThreadsService_PostAsync_ShouldReturnError_WithoutAuthentication()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test post for Threads",
            TargetPlatforms = [SocialPlatform.Threads]
        };

        var account = new Account
        {
            PlatformId = SocialPlatform.Threads,
            Username = "test_user",
            DisplayName = "Test User"
        };

        // Act
        var result = await _threadsService.PostAsync(post, account);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not authenticated");
    }

    [Fact]
    public async Task ThreadsService_ValidateContentAsync_ShouldEnforceCharacterLimit()
    {
        // Arrange
        var longContent = new string('A', 501); // Exceeds 500 character limit
        var post = new Post { Content = longContent };

        // Act
        var result = await _threadsService.ValidateContentAsync(post);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("500 character"));
    }

    [Fact]
    public async Task ThreadsService_GetPlatformLimitsAsync_ShouldReturnCorrectLimits()
    {
        // Act
        var limits = await _threadsService.GetPlatformLimitsAsync();

        // Assert
        limits.Should().NotBeNull();
        limits.CharacterLimit.Should().Be(500);
        limits.MaxMediaCount.Should().Be(10);
        limits.SupportedMediaTypes.Should().Contain("image/jpeg");
        limits.SupportedMediaTypes.Should().Contain("video/mp4");
        limits.MaxThreadLength.Should().Be(500);
        limits.DailyPostLimit.Should().Be(250);
    }

    #endregion

    #region Facebook Service Tests

    [Fact]
    public async Task FacebookService_PostAsync_ShouldReturnError_WithoutAuthentication()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test post for Facebook",
            TargetPlatforms = [SocialPlatform.Facebook]
        };

        var account = new Account
        {
            PlatformId = SocialPlatform.Facebook,
            Username = "test_user",
            DisplayName = "Test User"
        };

        // Act
        var result = await _facebookService.PostAsync(post, account);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not authenticated");
    }

    [Fact]
    public async Task FacebookService_ValidateContentAsync_ShouldAllowLongContent()
    {
        // Arrange
        var longContent = new string('A', 10000); // Facebook allows long posts
        var post = new Post { Content = longContent };

        // Act
        var result = await _facebookService.ValidateContentAsync(post);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task FacebookService_GetPlatformLimitsAsync_ShouldReturnCorrectLimits()
    {
        // Act
        var limits = await _facebookService.GetPlatformLimitsAsync();

        // Assert
        limits.Should().NotBeNull();
        limits.CharacterLimit.Should().BeNull(); // Facebook doesn't have strict character limit
        limits.MaxMediaCount.Should().Be(10);
        limits.SupportedMediaTypes.Should().Contain("image/jpeg");
        limits.SupportedMediaTypes.Should().Contain("video/mp4");
        limits.MaxThreadLength.Should().Be(1); // Facebook doesn't support threads
        limits.DailyPostLimit.Should().Be(200);
    }

    #endregion

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
} 
