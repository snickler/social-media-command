using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using SocialMediaCommander.Services.Interfaces;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Comprehensive unit tests for FacebookService
/// Tests Facebook platform integration, posting, authentication, and error handling
/// </summary>
public class FacebookServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly HttpClient _httpClient;
    private readonly FacebookService _service;

    public FacebookServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockAuthService = new Mock<IAuthenticationService>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _service = new FacebookService(_httpClient, _mockAuthService.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FacebookService(null!, _mockAuthService.Object));
    }

    [Fact]
    public void Constructor_WithNullAuthService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FacebookService(_httpClient, null!));
    }

    [Fact]
    public void Platform_ShouldReturnFacebook()
    {
        // Act & Assert
        Assert.Equal(SocialPlatform.Facebook, _service.Platform);
    }

    [Fact]
    public async Task PostAsync_WithUnauthenticatedAccount_ShouldReturnFailureResult()
    {
        // Arrange
        var post = new Post { Content = "Test post" };
        var account = new Account
        {
            PlatformId = SocialPlatform.Facebook,
            AuthStatus = AuthenticationStatus.NotAuthenticated
        };

        // Act
        var result = await _service.PostAsync(post, account);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Account is not authenticated", result.ErrorMessage);
        Assert.Null(result.PlatformPostId);
    }

    [Fact]
    public async Task PostAsync_WithNullUserProfile_ShouldReturnFailureResult()
    {
        // Arrange
        var post = new Post { Content = "Test post" };
        var account = new Account
        {
            PlatformId = SocialPlatform.Facebook,
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens
            {
                AccessToken = "test_access_token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }
        };

        _mockAuthService.Setup(x => x.GetUserProfileAsync(SocialPlatform.Facebook, "test_access_token"))
                       .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.PostAsync(post, account);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Could not retrieve user profile", result.ErrorMessage);
        Assert.Null(result.PlatformPostId);
    }

    [Fact]
    public async Task PostAsync_WithValidRequest_ShouldReturnSuccessResult()
    {
        // Arrange
        var post = new Post { Content = "Test Facebook post" };
        var account = new Account
        {
            PlatformId = SocialPlatform.Facebook,
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens { AccessToken = "test_access_token", ExpiresAt = DateTime.UtcNow.AddHours(1) }
        };

        var userProfile = new UserProfile
        {
            Id = "123456789",
            DisplayName = "Test User",
            Platform = SocialPlatform.Facebook
        };

        var facebookResponse = new { id = "123456789_987654321" };
        var responseJson = JsonSerializer.Serialize(facebookResponse);

        _mockAuthService.Setup(x => x.GetUserProfileAsync(SocialPlatform.Facebook, "test_access_token"))
                       .ReturnsAsync(userProfile);

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _service.PostAsync(post, account);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("123456789_987654321", result.PlatformPostId);
        Assert.True(result.PublishedAt <= DateTime.UtcNow);
        Assert.True(result.PublishedAt > DateTime.UtcNow.AddMinutes(-1));
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task PostAsync_WithHttpError_ShouldReturnFailureResult()
    {
        // Arrange
        var post = new Post { Content = "Test post" };
        var account = new Account
        {
            PlatformId = SocialPlatform.Facebook,
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens { AccessToken = "test_access_token", ExpiresAt = DateTime.UtcNow.AddHours(1) }
        };

        var userProfile = new UserProfile
        {
            Id = "123456789",
            DisplayName = "Test User",
            Platform = SocialPlatform.Facebook
        };

        _mockAuthService.Setup(x => x.GetUserProfileAsync(SocialPlatform.Facebook, "test_access_token"))
                       .ReturnsAsync(userProfile);

        SetupHttpResponse(HttpStatusCode.BadRequest, "Invalid request");

        // Act
        var result = await _service.PostAsync(post, account);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Facebook API error", result.ErrorMessage);
        Assert.Null(result.PlatformPostId);
    }

    [Fact]
    public async Task PostAsync_WithNetworkException_ShouldReturnFailureResult()
    {
        // Arrange
        var post = new Post { Content = "Test post" };
        var account = new Account
        {
            PlatformId = SocialPlatform.Facebook,
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens { AccessToken = "test_access_token", ExpiresAt = DateTime.UtcNow.AddHours(1) }
        };

        var userProfile = new UserProfile
        {
            Id = "123456789",
            DisplayName = "Test User",
            Platform = SocialPlatform.Facebook
        };

        _mockAuthService.Setup(x => x.GetUserProfileAsync(SocialPlatform.Facebook, "test_access_token"))
                       .ReturnsAsync(userProfile);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Failed to post to Facebook:"));

        // Act
        var result = await _service.PostAsync(post, account);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Failed to post to Facebook:", result.ErrorMessage);
        Assert.Null(result.PlatformPostId);
    }

    [Fact]
    public async Task PostAsync_ShouldFormatPostContentForFacebook()
    {
        // Arrange
        var post = new Post { Content = "Test post with #hashtag" };
        var account = new Account
        {
            PlatformId = SocialPlatform.Facebook,
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens { AccessToken = "test_access_token", ExpiresAt = DateTime.UtcNow.AddHours(1) }
        };

        var userProfile = new UserProfile
        {
            Id = "123456789",
            DisplayName = "Test User",
            Platform = SocialPlatform.Facebook
        };

        var facebookResponse = new { id = "123456789_987654321" };
        var responseJson = JsonSerializer.Serialize(facebookResponse);

        _mockAuthService.Setup(x => x.GetUserProfileAsync(SocialPlatform.Facebook, "test_access_token"))
                       .ReturnsAsync(userProfile);

        string? requestContent = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                requestContent = request.Content?.ReadAsStringAsync().Result;
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _service.PostAsync(post, account);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(requestContent);
        Assert.Contains("Test post with #hashtag", requestContent);
        Assert.Contains("test_access_token", requestContent);
    }

    [Fact]
    public async Task PostAsync_ShouldSendRequestToCorrectEndpoint()
    {
        // Arrange
        var post = new Post { Content = "Test post" };
        var account = new Account
        {
            PlatformId = SocialPlatform.Facebook,
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens { AccessToken = "test_access_token", ExpiresAt = DateTime.UtcNow.AddHours(1) }
        };

        var userProfile = new UserProfile
        {
            Id = "123456789",
            DisplayName = "Test User",
            Platform = SocialPlatform.Facebook
        };

        var facebookResponse = new { id = "123456789_987654321" };
        var responseJson = JsonSerializer.Serialize(facebookResponse);

        _mockAuthService.Setup(x => x.GetUserProfileAsync(SocialPlatform.Facebook, "test_access_token"))
                       .ReturnsAsync(userProfile);

        string? requestUri = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                requestUri = request.RequestUri?.ToString();
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _service.PostAsync(post, account);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(requestUri);
        Assert.Contains("graph.facebook.com/v18.0/123456789/feed", requestUri);
    }

    [Fact]
    public async Task PostAsync_ShouldUsePostHttpMethod()
    {
        // Arrange
        var post = new Post { Content = "Test post" };
        var account = new Account
        {
            PlatformId = SocialPlatform.Facebook,
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens { AccessToken = "test_access_token", ExpiresAt = DateTime.UtcNow.AddHours(1) }
        };

        var userProfile = new UserProfile
        {
            Id = "123456789",
            DisplayName = "Test User",
            Platform = SocialPlatform.Facebook
        };

        var facebookResponse = new { id = "123456789_987654321" };
        var responseJson = JsonSerializer.Serialize(facebookResponse);

        _mockAuthService.Setup(x => x.GetUserProfileAsync(SocialPlatform.Facebook, "test_access_token"))
                       .ReturnsAsync(userProfile);

        HttpMethod? requestMethod = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                requestMethod = request.Method;
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _service.PostAsync(post, account);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, requestMethod);
    }

    [Theory]
    [InlineData("Simple post")]
    [InlineData("Post with emoji 🚀")]
    [InlineData("Post with #hashtags and @mentions")]
    [InlineData("Long post content that exceeds typical limits but should still be handled properly by the Facebook service implementation")]
    public async Task PostAsync_WithDifferentContentTypes_ShouldHandleCorrectly(string content)
    {
        // Arrange
        var post = new Post { Content = content };
        var account = new Account
        {
            PlatformId = SocialPlatform.Facebook,
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens { AccessToken = "test_access_token", ExpiresAt = DateTime.UtcNow.AddHours(1) }
        };

        var userProfile = new UserProfile
        {
            Id = "123456789",
            DisplayName = "Test User",
            Platform = SocialPlatform.Facebook
        };

        var facebookResponse = new { id = "123456789_987654321" };
        var responseJson = JsonSerializer.Serialize(facebookResponse);

        _mockAuthService.Setup(x => x.GetUserProfileAsync(SocialPlatform.Facebook, "test_access_token"))
                       .ReturnsAsync(userProfile);

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _service.PostAsync(post, account);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.PlatformPostId);
    }

    [Fact]
    public async Task PostAsync_WithAuthServiceException_ShouldReturnFailureResult()
    {
        // Arrange
        var post = new Post { Content = "Test post" };
        var account = new Account
        {
            PlatformId = SocialPlatform.Facebook,
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens { AccessToken = "test_access_token", ExpiresAt = DateTime.UtcNow.AddHours(1) }
        };

        _mockAuthService.Setup(x => x.GetUserProfileAsync(SocialPlatform.Facebook, "test_access_token"))
                       .ThrowsAsync(new InvalidOperationException("Account is not authenticated"));

        // Act
        var result = await _service.PostAsync(post, account);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Account is not authenticated", result.ErrorMessage);
        Assert.Null(result.PlatformPostId);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}

/// <summary>
/// Comprehensive unit tests for LinkedInService
/// Tests LinkedIn platform integration, posting, authentication, and error handling
/// </summary>
public class LinkedInServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly HttpClient _httpClient;
    private readonly LinkedInService _service;

    public LinkedInServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockAuthService = new Mock<IAuthenticationService>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _service = new LinkedInService(_httpClient, _mockAuthService.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    [Fact]
    public void Platform_ShouldReturnLinkedIn()
    {
        // Act & Assert
        Assert.Equal(SocialPlatform.LinkedIn, _service.Platform);
    }

    [Fact]
    public async Task PostAsync_WithValidProfessionalContent_ShouldReturnSuccessResult()
    {
        // Arrange
        var post = new Post { Content = "Professional update about industry trends and insights" };
        var account = new Account
        {
            PlatformId = SocialPlatform.LinkedIn,
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens { AccessToken = "linkedin_access_token", ExpiresAt = DateTime.UtcNow.AddHours(1) }
        };

        var userProfile = new UserProfile
        {
            Id = "linkedin-user-123",
            DisplayName = "Professional User",
            Platform = SocialPlatform.LinkedIn
        };

        var linkedInResponse = new { id = "activity:123456789" };
        var responseJson = JsonSerializer.Serialize(linkedInResponse);

        _mockAuthService.Setup(x => x.GetUserProfileAsync(SocialPlatform.LinkedIn, "linkedin_access_token"))
                       .ReturnsAsync(userProfile);

        SetupHttpResponse(HttpStatusCode.Created, responseJson);

        // Act
        var result = await _service.PostAsync(post, account);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("activity:123456789", result.PlatformPostId);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}

/// <summary>
/// Comprehensive unit tests for ThreadsService
/// Tests Threads platform integration, posting, authentication, and error handling
/// </summary>
public class ThreadsServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly HttpClient _httpClient;
    private readonly ThreadsService _service;

    public ThreadsServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockAuthService = new Mock<IAuthenticationService>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _service = new ThreadsService(_httpClient, _mockAuthService.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    [Fact]
    public void Platform_ShouldReturnThreads()
    {
        // Act & Assert
        Assert.Equal(SocialPlatform.Threads, _service.Platform);
    }

    [Fact]
    public async Task PostAsync_WithValidThreadContent_ShouldReturnSuccessResult()
    {
        // Arrange
        var post = new Post { Content = "Casual conversation starter for the Threads community" };
        var account = new Account
        {
            PlatformId = SocialPlatform.Threads,
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens { AccessToken = "threads_access_token", ExpiresAt = DateTime.UtcNow.AddHours(1) }
        };

        var userProfile = new UserProfile
        {
            Id = "threads-user-456",
            DisplayName = "Social User",
            Platform = SocialPlatform.Threads
        };

        var threadsResponse = new { id = "thread_123456789" };
        var responseJson = JsonSerializer.Serialize(threadsResponse);

        _mockAuthService.Setup(x => x.GetUserProfileAsync(SocialPlatform.Threads, "threads_access_token"))
                       .ReturnsAsync(userProfile);

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _service.PostAsync(post, account);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("thread_123456789", result.PlatformPostId);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}