using Xunit;
using SocialMediaCommander.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace SocialMediaCommander.Tests.UnitTests;

public class AIModelsTests
{
    [Fact]
    public void AIContentRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new AIContentRequest();

        // Assert
        Assert.NotNull(request.Id);
        Assert.False(string.IsNullOrEmpty(request.Id));
        Assert.Equal(string.Empty, request.Prompt);
        Assert.Equal(AIContentType.Post, request.ContentType);
        Assert.NotNull(request.TargetPlatforms);
        Assert.Empty(request.TargetPlatforms);
        Assert.NotNull(request.Keywords);
        Assert.Empty(request.Keywords);
        Assert.Equal(280, request.MaxLength);
        Assert.Equal(AITone.Professional, request.Tone);
        Assert.True(request.IncludeHashtags);
        Assert.False(request.IncludeEmojis);
        Assert.True(request.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void AIContentRequest_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var prompt = "Test prompt";
        var keywords = new List<string> { "test", "ai" };
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };
        var createdAt = DateTime.UtcNow;

        // Act
        var request = new AIContentRequest
        {
            Id = id,
            Prompt = prompt,
            ContentType = AIContentType.Thread,
            TargetPlatforms = platforms,
            BrandVoice = "Casual",
            Keywords = keywords,
            MaxLength = 500,
            Tone = AITone.Casual,
            IncludeHashtags = false,
            IncludeEmojis = true,
            Context = "Test context",
            CreatedAt = createdAt
        };

        // Assert
        Assert.Equal(id, request.Id);
        Assert.Equal(prompt, request.Prompt);
        Assert.Equal(AIContentType.Thread, request.ContentType);
        Assert.Equal(platforms, request.TargetPlatforms);
        Assert.Equal("Casual", request.BrandVoice);
        Assert.Equal(keywords, request.Keywords);
        Assert.Equal(500, request.MaxLength);
        Assert.Equal(AITone.Casual, request.Tone);
        Assert.False(request.IncludeHashtags);
        Assert.True(request.IncludeEmojis);
        Assert.Equal("Test context", request.Context);
        Assert.Equal(createdAt, request.CreatedAt);
    }

    [Fact]
    public void AIContentRequest_ValidationShouldWork()
    {
        // Arrange
        var request = new AIContentRequest { Prompt = "" };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AIContentRequest.Prompt)));
    }

    [Fact]
    public void AIContentResponse_ShouldInitializeWithDefaults()
    {
        // Act
        var response = new AIContentResponse();

        // Assert
        Assert.Equal(string.Empty, response.Id);
        Assert.False(response.Success);
        Assert.Null(response.ErrorMessage);
        Assert.NotNull(response.GeneratedContent);
        Assert.Empty(response.GeneratedContent);
        Assert.NotNull(response.Metrics);
        Assert.True(response.GeneratedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void AIContentResponse_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = "test-id";
        var content = new List<AIGeneratedContent> { new() };
        var metrics = new AIMetrics();
        var generatedAt = DateTime.UtcNow;

        // Act
        var response = new AIContentResponse
        {
            Id = id,
            Success = true,
            ErrorMessage = "Test error",
            GeneratedContent = content,
            Metrics = metrics,
            GeneratedAt = generatedAt
        };

        // Assert
        Assert.Equal(id, response.Id);
        Assert.True(response.Success);
        Assert.Equal("Test error", response.ErrorMessage);
        Assert.Equal(content, response.GeneratedContent);
        Assert.Equal(metrics, response.Metrics);
        Assert.Equal(generatedAt, response.GeneratedAt);
    }

    [Fact]
    public void AIGeneratedContent_ShouldInitializeWithDefaults()
    {
        // Act
        var content = new AIGeneratedContent();

        // Assert
        Assert.Equal(string.Empty, content.Content);
        Assert.Equal(default(SocialPlatform), content.Platform);
        Assert.Equal(0, content.CharacterCount);
        Assert.NotNull(content.Hashtags);
        Assert.Empty(content.Hashtags);
        Assert.Equal(0.0, content.ConfidenceScore);
        Assert.NotNull(content.Suggestions);
        Assert.Empty(content.Suggestions);
    }

    [Fact]
    public void AIGeneratedContent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var content = "Test content";
        var platform = SocialPlatform.BlueSky;
        var hashtags = new List<string> { "#test", "#ai" };
        var suggestions = new List<string> { "Add emoji", "Shorten text" };

        // Act
        var generated = new AIGeneratedContent
        {
            Content = content,
            Platform = platform,
            CharacterCount = 150,
            Hashtags = hashtags,
            ConfidenceScore = 0.95,
            Suggestions = suggestions
        };

        // Assert
        Assert.Equal(content, generated.Content);
        Assert.Equal(platform, generated.Platform);
        Assert.Equal(150, generated.CharacterCount);
        Assert.Equal(hashtags, generated.Hashtags);
        Assert.Equal(0.95, generated.ConfidenceScore);
        Assert.Equal(suggestions, generated.Suggestions);
    }

    [Fact]
    public void AIMetrics_ShouldInitializeWithDefaults()
    {
        // Act
        var metrics = new AIMetrics();

        // Assert
        Assert.Equal(0.0, metrics.ProcessingTimeMs);
        Assert.Equal(string.Empty, metrics.ModelUsed);
        Assert.Equal(0, metrics.TokensUsed);
        Assert.Equal(0.0, metrics.SentimentScore);
        Assert.NotNull(metrics.DetectedTopics);
        Assert.Empty(metrics.DetectedTopics);
        Assert.Equal(0.0, metrics.EngagementPrediction);
    }

    [Fact]
    public void AIMetrics_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var topics = new List<string> { "technology", "ai" };

        // Act
        var metrics = new AIMetrics
        {
            ProcessingTimeMs = 1250.5,
            ModelUsed = "gpt-4",
            TokensUsed = 500,
            SentimentScore = 0.8,
            DetectedTopics = topics,
            EngagementPrediction = 0.75
        };

        // Assert
        Assert.Equal(1250.5, metrics.ProcessingTimeMs);
        Assert.Equal("gpt-4", metrics.ModelUsed);
        Assert.Equal(500, metrics.TokensUsed);
        Assert.Equal(0.8, metrics.SentimentScore);
        Assert.Equal(topics, metrics.DetectedTopics);
        Assert.Equal(0.75, metrics.EngagementPrediction);
    }

    [Fact]
    public void AIOptimizationRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new AIOptimizationRequest();

        // Assert
        Assert.Equal(string.Empty, request.Content);
        Assert.Equal(default(SocialPlatform), request.Platform);
        Assert.Equal(default(AIOptimizationType), request.OptimizationType);
        Assert.Null(request.TargetAudience);
        Assert.Null(request.ScheduledTime);
    }

    [Fact]
    public void AIOptimizationRequest_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var content = "Test content";
        var platform = SocialPlatform.BlueSky;
        var scheduledTime = DateTime.UtcNow.AddHours(2);

        // Act
        var request = new AIOptimizationRequest
        {
            Content = content,
            Platform = platform,
            OptimizationType = AIOptimizationType.Engagement,
            TargetAudience = "Tech professionals",
            ScheduledTime = scheduledTime
        };

        // Assert
        Assert.Equal(content, request.Content);
        Assert.Equal(platform, request.Platform);
        Assert.Equal(AIOptimizationType.Engagement, request.OptimizationType);
        Assert.Equal("Tech professionals", request.TargetAudience);
        Assert.Equal(scheduledTime, request.ScheduledTime);
    }

    [Fact]
    public void AIOptimizationResponse_ShouldInitializeWithDefaults()
    {
        // Act
        var response = new AIOptimizationResponse();

        // Assert
        Assert.Equal(string.Empty, response.OptimizedContent);
        Assert.NotNull(response.Suggestions);
        Assert.Empty(response.Suggestions);
        Assert.Equal(0.0, response.ImprovementScore);
        Assert.NotNull(response.Analysis);
    }

    [Fact]
    public void AIOptimizationResponse_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var optimizedContent = "Optimized content";
        var suggestions = new List<AIOptimizationSuggestion> { new() };
        var analysis = new AIAnalysis();

        // Act
        var response = new AIOptimizationResponse
        {
            OptimizedContent = optimizedContent,
            Suggestions = suggestions,
            ImprovementScore = 0.85,
            Analysis = analysis
        };

        // Assert
        Assert.Equal(optimizedContent, response.OptimizedContent);
        Assert.Equal(suggestions, response.Suggestions);
        Assert.Equal(0.85, response.ImprovementScore);
        Assert.Equal(analysis, response.Analysis);
    }

    [Fact]
    public void AIOptimizationSuggestion_ShouldInitializeWithDefaults()
    {
        // Act
        var suggestion = new AIOptimizationSuggestion();

        // Assert
        Assert.Equal(string.Empty, suggestion.Type);
        Assert.Equal(string.Empty, suggestion.Suggestion);
        Assert.Equal(string.Empty, suggestion.Reason);
        Assert.Equal(0.0, suggestion.Impact);
    }

    [Fact]
    public void AIOptimizationSuggestion_ShouldSetPropertiesCorrectly()
    {
        // Act
        var suggestion = new AIOptimizationSuggestion
        {
            Type = "Hashtag",
            Suggestion = "Add #TechTrends",
            Reason = "Increases discoverability",
            Impact = 0.3
        };

        // Assert
        Assert.Equal("Hashtag", suggestion.Type);
        Assert.Equal("Add #TechTrends", suggestion.Suggestion);
        Assert.Equal("Increases discoverability", suggestion.Reason);
        Assert.Equal(0.3, suggestion.Impact);
    }

    [Fact]
    public void AIAnalysis_ShouldInitializeWithDefaults()
    {
        // Act
        var analysis = new AIAnalysis();

        // Assert
        Assert.Equal(0.0, analysis.ReadabilityScore);
        Assert.Equal(0.0, analysis.SentimentScore);
        Assert.NotNull(analysis.KeywordDensity);
        Assert.Empty(analysis.KeywordDensity);
        Assert.Equal(0.0, analysis.EngagementPotential);
        Assert.NotNull(analysis.RecommendedHashtags);
        Assert.Empty(analysis.RecommendedHashtags);
        Assert.Equal(string.Empty, analysis.OptimalPostTime);
    }

    [Fact]
    public void AIAnalysis_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var keywordDensity = new List<string> { "tech: 5%", "ai: 3%" };
        var hashtags = new List<string> { "#tech", "#ai" };

        // Act
        var analysis = new AIAnalysis
        {
            ReadabilityScore = 0.8,
            SentimentScore = 0.7,
            KeywordDensity = keywordDensity,
            EngagementPotential = 0.85,
            RecommendedHashtags = hashtags,
            OptimalPostTime = "2:00 PM"
        };

        // Assert
        Assert.Equal(0.8, analysis.ReadabilityScore);
        Assert.Equal(0.7, analysis.SentimentScore);
        Assert.Equal(keywordDensity, analysis.KeywordDensity);
        Assert.Equal(0.85, analysis.EngagementPotential);
        Assert.Equal(hashtags, analysis.RecommendedHashtags);
        Assert.Equal("2:00 PM", analysis.OptimalPostTime);
    }

    [Fact]
    public void AIModelConfig_ShouldInitializeWithDefaults()
    {
        // Act
        var config = new AIModelConfig();

        // Assert - Updated for Foundry Local defaults
        Assert.Equal(string.Empty, config.ModelName); // Model selected by user in UI
        Assert.Equal("http://localhost:5273", config.BaseUrl); // Foundry Local default port
        Assert.Equal(0.7, config.Temperature);
        Assert.Equal(2048, config.MaxTokens);
        Assert.NotEmpty(config.SystemPrompt); // Has default system prompt now
        Assert.NotNull(config.Parameters);
        Assert.Empty(config.Parameters);
    }

    [Fact]
    public void AIModelConfig_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "top_p", 0.9 } };

        // Act
        var config = new AIModelConfig
        {
            ModelName = "gpt-4",
            BaseUrl = "https://api.openai.com",
            Temperature = 0.5,
            MaxTokens = 4096,
            SystemPrompt = "You are a helpful assistant",
            Parameters = parameters
        };

        // Assert
        Assert.Equal("gpt-4", config.ModelName);
        Assert.Equal("https://api.openai.com", config.BaseUrl);
        Assert.Equal(0.5, config.Temperature);
        Assert.Equal(4096, config.MaxTokens);
        Assert.Equal("You are a helpful assistant", config.SystemPrompt);
        Assert.Equal(parameters, config.Parameters);
    }

    [Theory]
    [InlineData(AIContentType.Post)]
    [InlineData(AIContentType.Thread)]
    [InlineData(AIContentType.Story)]
    [InlineData(AIContentType.Advertisement)]
    [InlineData(AIContentType.Announcement)]
    [InlineData(AIContentType.Question)]
    [InlineData(AIContentType.Poll)]
    [InlineData(AIContentType.Quote)]
    [InlineData(AIContentType.Tutorial)]
    [InlineData(AIContentType.News)]
    public void AIContentType_ShouldHaveAllValues(AIContentType contentType)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(AIContentType), contentType));
    }

    [Theory]
    [InlineData(AITone.Professional)]
    [InlineData(AITone.Casual)]
    [InlineData(AITone.Friendly)]
    [InlineData(AITone.Authoritative)]
    [InlineData(AITone.Humorous)]
    [InlineData(AITone.Inspirational)]
    [InlineData(AITone.Educational)]
    [InlineData(AITone.Promotional)]
    [InlineData(AITone.Conversational)]
    [InlineData(AITone.Formal)]
    public void AITone_ShouldHaveAllValues(AITone tone)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(AITone), tone));
    }

    [Theory]
    [InlineData(AIOptimizationType.Engagement)]
    [InlineData(AIOptimizationType.Reach)]
    [InlineData(AIOptimizationType.Conversions)]
    [InlineData(AIOptimizationType.BrandAwareness)]
    [InlineData(AIOptimizationType.ClickThrough)]
    [InlineData(AIOptimizationType.Sentiment)]
    [InlineData(AIOptimizationType.Accessibility)]
    [InlineData(AIOptimizationType.SEO)]
    public void AIOptimizationType_ShouldHaveAllValues(AIOptimizationType optimizationType)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(AIOptimizationType), optimizationType));
    }

    [Fact]
    public void AIInsights_ShouldInitializeWithDefaults()
    {
        // Act
        var insights = new AIInsights();

        // Assert
        Assert.NotNull(insights.Id);
        Assert.False(string.IsNullOrEmpty(insights.Id));
        Assert.True(insights.GeneratedAt <= DateTime.UtcNow);
        Assert.NotNull(insights.TrendAnalysis);
        Assert.Empty(insights.TrendAnalysis);
        Assert.NotNull(insights.AudienceInsights);
        Assert.Empty(insights.AudienceInsights);
        Assert.NotNull(insights.ContentRecommendations);
        Assert.Empty(insights.ContentRecommendations);
        Assert.NotNull(insights.PerformancePrediction);
    }

    [Fact]
    public void AIInsights_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var generatedAt = DateTime.UtcNow;
        var trends = new List<AITrendAnalysis> { new() };
        var audience = new List<AIAudienceInsight> { new() };
        var recommendations = new List<AIContentRecommendation> { new() };
        var prediction = new AIPerformancePrediction();

        // Act
        var insights = new AIInsights
        {
            Id = id,
            GeneratedAt = generatedAt,
            TrendAnalysis = trends,
            AudienceInsights = audience,
            ContentRecommendations = recommendations,
            PerformancePrediction = prediction
        };

        // Assert
        Assert.Equal(id, insights.Id);
        Assert.Equal(generatedAt, insights.GeneratedAt);
        Assert.Equal(trends, insights.TrendAnalysis);
        Assert.Equal(audience, insights.AudienceInsights);
        Assert.Equal(recommendations, insights.ContentRecommendations);
        Assert.Equal(prediction, insights.PerformancePrediction);
    }

    [Fact]
    public void AITrendAnalysis_ShouldInitializeWithDefaults()
    {
        // Act
        var trend = new AITrendAnalysis();

        // Assert
        Assert.Equal(string.Empty, trend.Topic);
        Assert.Equal(0.0, trend.TrendScore);
        Assert.NotNull(trend.RelatedKeywords);
        Assert.Empty(trend.RelatedKeywords);
        Assert.Equal(string.Empty, trend.TrendDirection);
        Assert.True(trend.DetectedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void AITrendAnalysis_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var keywords = new List<string> { "ai", "machine learning" };
        var detectedAt = DateTime.UtcNow;

        // Act
        var trend = new AITrendAnalysis
        {
            Topic = "Artificial Intelligence",
            TrendScore = 0.85,
            RelatedKeywords = keywords,
            TrendDirection = "Rising",
            DetectedAt = detectedAt
        };

        // Assert
        Assert.Equal("Artificial Intelligence", trend.Topic);
        Assert.Equal(0.85, trend.TrendScore);
        Assert.Equal(keywords, trend.RelatedKeywords);
        Assert.Equal("Rising", trend.TrendDirection);
        Assert.Equal(detectedAt, trend.DetectedAt);
    }

    [Fact]
    public void AIAudienceInsight_ShouldInitializeWithDefaults()
    {
        // Act
        var insight = new AIAudienceInsight();

        // Assert
        Assert.Equal(string.Empty, insight.Demographic);
        Assert.Equal(0.0, insight.EngagementRate);
        Assert.NotNull(insight.PreferredContent);
        Assert.Empty(insight.PreferredContent);
        Assert.Equal(string.Empty, insight.OptimalPostTime);
        Assert.NotNull(insight.ActivePlatforms);
        Assert.Empty(insight.ActivePlatforms);
    }

    [Fact]
    public void AIAudienceInsight_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var content = new List<string> { "Tech news", "Tutorials" };
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky, SocialPlatform.BlueSky };

        // Act
        var insight = new AIAudienceInsight
        {
            Demographic = "Tech professionals 25-35",
            EngagementRate = 0.12,
            PreferredContent = content,
            OptimalPostTime = "10:00 AM",
            ActivePlatforms = platforms
        };

        // Assert
        Assert.Equal("Tech professionals 25-35", insight.Demographic);
        Assert.Equal(0.12, insight.EngagementRate);
        Assert.Equal(content, insight.PreferredContent);
        Assert.Equal("10:00 AM", insight.OptimalPostTime);
        Assert.Equal(platforms, insight.ActivePlatforms);
    }

    [Fact]
    public void AIContentRecommendation_ShouldInitializeWithDefaults()
    {
        // Act
        var recommendation = new AIContentRecommendation();

        // Assert
        Assert.Equal(string.Empty, recommendation.ContentType);
        Assert.Equal(string.Empty, recommendation.Topic);
        Assert.Equal(0.0, recommendation.PotentialReach);
        Assert.Equal(string.Empty, recommendation.Reasoning);
        Assert.NotNull(recommendation.SuggestedHashtags);
        Assert.Empty(recommendation.SuggestedHashtags);
    }

    [Fact]
    public void AIContentRecommendation_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var hashtags = new List<string> { "#tech", "#innovation" };

        // Act
        var recommendation = new AIContentRecommendation
        {
            ContentType = "Tutorial",
            Topic = "AI Basics",
            PotentialReach = 5000.0,
            Reasoning = "High engagement on educational content",
            SuggestedHashtags = hashtags
        };

        // Assert
        Assert.Equal("Tutorial", recommendation.ContentType);
        Assert.Equal("AI Basics", recommendation.Topic);
        Assert.Equal(5000.0, recommendation.PotentialReach);
        Assert.Equal("High engagement on educational content", recommendation.Reasoning);
        Assert.Equal(hashtags, recommendation.SuggestedHashtags);
    }

    [Fact]
    public void AIPerformancePrediction_ShouldInitializeWithDefaults()
    {
        // Act
        var prediction = new AIPerformancePrediction();

        // Assert
        Assert.Equal(0.0, prediction.PredictedEngagement);
        Assert.Equal(0.0, prediction.PredictedReach);
        Assert.Equal(0.0, prediction.ConfidenceLevel);
        Assert.NotNull(prediction.Factors);
        Assert.Empty(prediction.Factors);
        Assert.NotNull(prediction.PlatformPerformance);
        Assert.Empty(prediction.PlatformPerformance);
    }

    [Fact]
    public void AIPerformancePrediction_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var factors = new List<string> { "Optimal timing", "Trending hashtags" };
        var platformPerformance = new Dictionary<SocialPlatform, double>
        {
            { SocialPlatform.BlueSky, 0.8 }
        };

        // Act
        var prediction = new AIPerformancePrediction
        {
            PredictedEngagement = 0.15,
            PredictedReach = 10000.0,
            ConfidenceLevel = 0.85,
            Factors = factors,
            PlatformPerformance = platformPerformance
        };

        // Assert
        Assert.Equal(0.15, prediction.PredictedEngagement);
        Assert.Equal(10000.0, prediction.PredictedReach);
        Assert.Equal(0.85, prediction.ConfidenceLevel);
        Assert.Equal(factors, prediction.Factors);
        Assert.Equal(platformPerformance, prediction.PlatformPerformance);
    }
}
