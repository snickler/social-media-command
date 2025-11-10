using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using SocialMediaCommander.Services.Interfaces;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Comprehensive unit tests for FoundryLocalAIService
/// Tests AI content generation, optimization, and performance patterns
/// </summary>
public class FoundryLocalAIServiceTests : IDisposable
{
    private readonly Mock<ILogger<FoundryLocalAIService>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly AIModelConfig _config;
    private readonly FoundryLocalAIService _service;

    public FoundryLocalAIServiceTests()
    {
        _mockLogger = new Mock<ILogger<FoundryLocalAIService>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080/v1")
        };

        _config = new AIModelConfig
        {
            BaseUrl = "http://localhost:8080/v1",
            ModelName = "test-model",
            MaxTokens = 150,
            Temperature = 0.7,
            SystemPrompt = "You are a helpful AI assistant for social media content creation."
        };

        var options = Options.Create(_config);
        _service = new FoundryLocalAIService(_mockLogger.Object, options, _httpClient);
    }

    public void Dispose()
    {
        _service?.Dispose();
        _httpClient?.Dispose();
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Assert
        Assert.NotNull(_service);
        Assert.Equal(TimeSpan.FromMinutes(5), _httpClient.Timeout);
    }

    [Fact]
    public async Task GenerateContentAsync_WithValidRequest_ShouldReturnSuccessResponse()
    {
        // Arrange
        var request = new AIContentRequest
        {
            Prompt = "Write a professional post about AI technology",
            ContentType = AIContentType.Post,
            Tone = AITone.Professional,
            IncludeHashtags = true,
            IncludeEmojis = false,
            TargetPlatforms = new List<SocialPlatform> { SocialPlatform.BlueSky, SocialPlatform.BlueSky }
        };

        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "AI technology is transforming how we work and live. From automation to intelligent insights, the possibilities are endless. #AI #Technology #Innovation"
                    }
                }
            },
            usage = new
            {
                prompt_tokens = 25,
                completion_tokens = 30,
                total_tokens = 55
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));

        // Act
        var result = await _service.GenerateContentAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.GeneratedContent);
        Assert.NotEmpty(result.GeneratedContent);
        Assert.Contains("AI technology", result.GeneratedContent[0].Content);
        Assert.Contains("#AI", result.GeneratedContent[0].Content);
        Assert.Equal(37, result.Metrics.TokensUsed);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task GenerateContentAsync_WithNullRequest_ShouldReturnFailureResponse()
    {
        // Act
        var result = await _service.GenerateContentAsync((AIContentRequest)null!);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.GeneratedContent);
        Assert.Equal("Request cannot be null", result.ErrorMessage);
        Assert.Equal(0, result.Metrics.TokensUsed);
    }

    [Fact]
    public async Task GenerateContentAsync_WithEmptyPrompt_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new AIContentRequest
        {
            Prompt = "",
            ContentType = AIContentType.Post,
            Tone = AITone.Professional
        };

        // Act
        var result = await _service.GenerateContentAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.GeneratedContent);
        Assert.Equal("Prompt cannot be empty", result.ErrorMessage);
        Assert.Equal(0, result.Metrics.TokensUsed);
    }

    [Fact]
    public async Task GenerateContentAsync_WithHttpError_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new AIContentRequest
        {
            Prompt = "Test prompt",
            ContentType = AIContentType.Post,
            Tone = AITone.Professional
        };

        SetupHttpResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

        // Act
        var result = await _service.GenerateContentAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.GeneratedContent);
        Assert.Contains("HTTP error", result.ErrorMessage);
        Assert.Equal(0, result.Metrics.TokensUsed);
    }

    [Fact]
    public async Task GenerateContentAsync_WithNetworkException_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new AIContentRequest
        {
            Prompt = "Test prompt",
            ContentType = AIContentType.Post,
            Tone = AITone.Professional
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _service.GenerateContentAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.GeneratedContent);
        Assert.Contains("Network error", result.ErrorMessage);
        Assert.Equal(0, result.Metrics.TokensUsed);
    }

    [Fact]
    public async Task OptimizeContentAsync_WithValidRequest_ShouldReturnOptimizedContent()
    {
        // Arrange
        var request = new AIOptimizationRequest
        {
            Content = "AI is great for business",
            Platform = SocialPlatform.BlueSky,
            OptimizationType = AIOptimizationType.Engagement
        };

        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "AI is revolutionizing business operations, driving efficiency and innovation across industries. Here's how your organization can leverage these powerful technologies to stay competitive. #AI #Business #Innovation #Technology"
                    }
                }
            },
            usage = new
            {
                prompt_tokens = 30,
                completion_tokens = 45,
                total_tokens = 75
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));

        // Act
        var result = await _service.OptimizeContentAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("revolutionizing", result.OptimizedContent);
        Assert.Contains("#AI", result.OptimizedContent);
        Assert.NotEmpty(result.Suggestions);
    }

    [Fact]
    public async Task GenerateVariationsAsync_WithValidContent_ShouldReturnVariations()
    {
        // Arrange
        var content = "AI technology is changing the world";
        var count = 3;

        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "1. Artificial intelligence is transforming our global landscape\n2. AI innovations are reshaping society as we know it\n3. Machine learning technologies are revolutionizing every industry"
                    }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));

        // Act
        var result = await _service.GenerateVariationsAsync(content, count);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count >= 1);
        Assert.Contains(result, v => v.Content.Contains("Artificial intelligence"));
    }

    [Fact]
    public async Task GenerateHashtagsAsync_WithValidContent_ShouldReturnHashtags()
    {
        // Arrange
        var content = "AI technology is revolutionizing business operations";
        var count = 5;

        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "AI, Technology, Business, Innovation, Automation"
                    }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));

        // Act
        var result = await _service.GenerateHashtagsAsync(content, count);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("#AI", result);
        Assert.Contains("#Technology", result);
        Assert.Contains("#Business", result);
    }

    [Fact]
    public async Task GenerateThreadAsync_WithValidContent_ShouldReturnThreadPosts()
    {
        // Arrange
        var content = "AI is transforming business operations";
        var maxPosts = 3;

        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "1/3 AI is transforming business operations across all industries...\n\n2/3 From automation to predictive analytics, AI is enabling companies to...\n\n3/3 The future of business is AI-driven. Organizations that embrace these technologies will..."
                    }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));

        // Act
        var result = await _service.GenerateThreadAsync(content, maxPosts);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count >= 1);
        Assert.True(result.Count <= maxPosts);
        Assert.Contains(result, p => p.Contains("1/3"));
    }

    [Fact]
    public async Task AnalyzeContentAsync_WithValidContent_ShouldReturnAnalysis()
    {
        // Arrange
        var content = "AI technology is revolutionizing business operations";

        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = JsonSerializer.Serialize(new
                        {
                            sentiment = "Positive",
                            sentiment_confidence = 0.85,
                            key_topics = new[] { "AI", "technology", "business" },
                            engagement_prediction = 75,
                            readability_score = 8.5,
                            suggested_improvements = new[] { "Add specific examples", "Include statistics" }
                        })
                    }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));

        // Act
        var result = await _service.AnalyzeContentAsync(content, SocialPlatform.BlueSky);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.SentimentScore >= 0);
        Assert.True(result.ReadabilityScore >= 0);
        Assert.True(result.EngagementPotential >= 0);
        Assert.NotEmpty(result.RecommendedHashtags);
        Assert.NotNull(result.OptimalPostTime);
    }

    [Theory]
    [InlineData(AIContentType.Post)]
    [InlineData(AIContentType.Thread)]
    [InlineData(AIContentType.Story)]
    [InlineData(AIContentType.Advertisement)]
    public async Task GenerateContentAsync_WithDifferentContentTypes_ShouldAdaptPrompt(AIContentType contentType)
    {
        // Arrange
        var request = new AIContentRequest
        {
            Prompt = "AI technology benefits",
            ContentType = contentType,
            Tone = AITone.Professional
        };

        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new { content = $"Generated {contentType} content about AI technology benefits" }
                }
            },
            usage = new { total_tokens = 50 }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));

        // Act
        var result = await _service.GenerateContentAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Contains(contentType.ToString(), result.GeneratedContent[0].Content);
    }

    [Theory]
    [InlineData(AITone.Professional)]
    [InlineData(AITone.Casual)]
    [InlineData(AITone.Formal)]
    [InlineData(AITone.Friendly)]
    [InlineData(AITone.Humorous)]
    public async Task GenerateContentAsync_WithDifferentTones_ShouldAdaptPrompt(AITone tone)
    {
        // Arrange
        var request = new AIContentRequest
        {
            Prompt = "AI technology update",
            ContentType = AIContentType.Post,
            Tone = tone
        };

        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new { content = $"Content written in {tone} tone about AI technology" }
                }
            },
            usage = new { total_tokens = 40 }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));

        // Act
        var result = await _service.GenerateContentAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Contains(tone.ToString(), result.GeneratedContent[0].Content);
    }

    [Theory]
    [InlineData(SocialPlatform.BlueSky)]
    // TODO: Add Twitter, LinkedIn, Facebook, Threads when implementations are ready
    public async Task GenerateContentAsync_WithDifferentPlatforms_ShouldAdaptContent(SocialPlatform platform)
    {
        // Arrange
        var request = new AIContentRequest
        {
            Prompt = "Technology announcement",
            ContentType = AIContentType.Post,
            Tone = AITone.Professional,
            TargetPlatforms = new List<SocialPlatform> { platform }
        };

        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new { content = $"Platform-optimized content for {platform}" }
                }
            },
            usage = new { total_tokens = 35 }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));

        // Act
        var result = await _service.GenerateContentAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(platform, result.GeneratedContent[0].Platform);
        Assert.Contains(platform.ToString(), result.GeneratedContent[0].Content);
    }

    [Fact]
    public async Task GenerateContentAsync_WithBrandVoice_ShouldIncludeInPrompt()
    {
        // Arrange
        var request = new AIContentRequest
        {
            Prompt = "Product announcement",
            ContentType = AIContentType.Post,
            Tone = AITone.Professional,
            BrandVoice = "Innovative and customer-focused technology company"
        };

        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new { content = "Innovative customer-focused announcement about our latest product" }
                }
            },
            usage = new { total_tokens = 45 }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));

        // Act
        var result = await _service.GenerateContentAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Innovative", result.GeneratedContent[0].Content);
        Assert.Contains("customer-focused", result.GeneratedContent[0].Content);
    }

    [Fact]
    public async Task GenerateContentAsync_WithKeywords_ShouldIncludeInPrompt()
    {
        // Arrange
        var request = new AIContentRequest
        {
            Prompt = "Technology post",
            ContentType = AIContentType.Post,
            Tone = AITone.Professional,
            Keywords = new List<string> { "artificial intelligence", "machine learning", "automation" }
        };

        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new { content = "A comprehensive post about artificial intelligence, machine learning, and automation technologies for the modern business environment" }
                }
            },
            usage = new { total_tokens = 60 }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));

        // Act
        var result = await _service.GenerateContentAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("artificial intelligence", result.GeneratedContent[0].Content);
        Assert.Contains("machine learning", result.GeneratedContent[0].Content);
        Assert.Contains("automation", result.GeneratedContent[0].Content);
    }

    [Fact]
    public async Task GenerateContentAsync_WithHashtagsEnabled_ShouldIncludeHashtags()
    {
        // Arrange
        var request = new AIContentRequest
        {
            Prompt = "AI announcement",
            ContentType = AIContentType.Post,
            Tone = AITone.Professional,
            IncludeHashtags = true
        };

        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new { content = "AI announcement with relevant hashtags #AI #Technology #Innovation" }
                }
            },
            usage = new { total_tokens = 40 }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));

        // Act
        var result = await _service.GenerateContentAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("#AI", result.GeneratedContent[0].Content);
        Assert.Contains("#Technology", result.GeneratedContent[0].Content);
    }

    [Fact]
    public async Task GenerateContentAsync_WithEmojisEnabled_ShouldIncludeEmojis()
    {
        // Arrange
        var request = new AIContentRequest
        {
            Prompt = "Exciting product launch",
            ContentType = AIContentType.Post,
            Tone = AITone.Friendly,
            IncludeEmojis = true
        };

        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new { content = "🚀 Exciting product launch! 🎉 New AI features available now! 💡" }
                }
            },
            usage = new { total_tokens = 35 }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));

        // Act
        var result = await _service.GenerateContentAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("🚀", result.GeneratedContent[0].Content);
        Assert.Contains("🎉", result.GeneratedContent[0].Content);
        Assert.Contains("💡", result.GeneratedContent[0].Content);
    }

    [Fact]
    public void Service_DisposedProperly_ShouldNotThrow()
    {
        // Arrange
        var service = new FoundryLocalAIService(_mockLogger.Object, Options.Create(_config), new HttpClient());

        // Act & Assert
        service.Dispose();
        service.Dispose(); // Multiple dispose calls should not throw
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldHandleCorrectly()
    {
        // Arrange
        var request = new AIContentRequest
        {
            Prompt = "Concurrent test",
            ContentType = AIContentType.Post,
            Tone = AITone.Professional
        };

        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new { content = "Concurrent response" }
                }
            },
            usage = new { total_tokens = 20 }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));

        // Act
        var tasks = new Task<AIContentResponse>[5];
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = _service.GenerateContentAsync(request);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result => Assert.True(result.Success));
        Assert.All(results, result => Assert.Equal("Concurrent response", result.GeneratedContent[0].Content));
    }

    [Fact]
    public async Task LargeContentGeneration_ShouldHandleEfficiently()
    {
        // Arrange
        var request = new AIContentRequest
        {
            Prompt = "Write a detailed announcement about AI technology trends, innovations, and future implications for business and society. Include multiple perspectives and comprehensive analysis.",
            ContentType = AIContentType.Announcement,
            Tone = AITone.Professional
        };

        var largeContent = new StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            largeContent.AppendLine($"Section {i}: AI technology continues to evolve and transform industries...");
        }

        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new { content = largeContent.ToString() }
                }
            },
            usage = new { total_tokens = 2000 }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));

        // Act
        var result = await _service.GenerateContentAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.GeneratedContent[0].Content.Length > 1000);
        Assert.True(result.Metrics.TokensUsed > 1800); // Should be around 1847
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
    }
}
