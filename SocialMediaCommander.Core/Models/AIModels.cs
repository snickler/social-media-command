using System.ComponentModel.DataAnnotations;

namespace SocialMediaCommander.Core.Models;

/// <summary>
/// AI-powered content generation and optimization
/// </summary>
public class AIContentRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string Prompt { get; set; } = string.Empty;

    public AIContentType ContentType { get; set; } = AIContentType.Post;

    public List<SocialPlatform> TargetPlatforms { get; set; } = new();

    public string? BrandVoice { get; set; }

    public List<string> Keywords { get; set; } = new();

    public int MaxLength { get; set; } = 280;

    public AITone Tone { get; set; } = AITone.Professional;

    public bool IncludeHashtags { get; set; } = true;

    public bool IncludeEmojis { get; set; } = false;

    public string? Context { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AIContentResponse
{
    public string Id { get; set; } = string.Empty;

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public List<AIGeneratedContent> GeneratedContent { get; set; } = new();

    public AIMetrics Metrics { get; set; } = new();

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class AIGeneratedContent
{
    public string Content { get; set; } = string.Empty;

    public SocialPlatform Platform { get; set; }

    public int CharacterCount { get; set; }

    public List<string> Hashtags { get; set; } = new();

    public double ConfidenceScore { get; set; }

    public List<string> Suggestions { get; set; } = new();
}

public class AIMetrics
{
    public double ProcessingTimeMs { get; set; }

    public string ModelUsed { get; set; } = string.Empty;

    public int TokensUsed { get; set; }

    public double SentimentScore { get; set; }

    public List<string> DetectedTopics { get; set; } = new();

    public double EngagementPrediction { get; set; }
}

/// <summary>
/// AI-powered content optimization and analysis
/// </summary>
public class AIOptimizationRequest
{
    public string Content { get; set; } = string.Empty;

    public SocialPlatform Platform { get; set; }

    public AIOptimizationType OptimizationType { get; set; }

    public string? TargetAudience { get; set; }

    public DateTime? ScheduledTime { get; set; }
}

public class AIOptimizationResponse
{
    public string OptimizedContent { get; set; } = string.Empty;

    public List<AIOptimizationSuggestion> Suggestions { get; set; } = new();

    public double ImprovementScore { get; set; }

    public AIAnalysis Analysis { get; set; } = new();
}

public class AIOptimizationSuggestion
{
    public string Type { get; set; } = string.Empty;

    public string Suggestion { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public double Impact { get; set; }
}

public class AIAnalysis
{
    public double ReadabilityScore { get; set; }

    public double SentimentScore { get; set; }

    public List<string> KeywordDensity { get; set; } = new();

    public double EngagementPotential { get; set; }

    public List<string> RecommendedHashtags { get; set; } = new();

    public string OptimalPostTime { get; set; } = string.Empty;
}

/// <summary>
/// AI model configuration for Foundry Local
/// </summary>
public class AIModelConfig
{
    public string ModelName { get; set; } = "llama3.2";

    public string BaseUrl { get; set; } = "http://localhost:11434";

    public double Temperature { get; set; } = 0.7;

    public int MaxTokens { get; set; } = 2048;

    public string SystemPrompt { get; set; } = string.Empty;

    public Dictionary<string, object> Parameters { get; set; } = new();
}

public enum AIContentType
{
    Post,
    Thread,
    Story,
    Advertisement,
    Announcement,
    Question,
    Poll,
    Quote,
    Tutorial,
    News
}

public enum AITone
{
    Professional,
    Casual,
    Friendly,
    Authoritative,
    Humorous,
    Inspirational,
    Educational,
    Promotional,
    Conversational,
    Formal
}

public enum AIOptimizationType
{
    Engagement,
    Reach,
    Conversions,
    BrandAwareness,
    ClickThrough,
    Sentiment,
    Accessibility,
    SEO
}

/// <summary>
/// AI-powered analytics and insights
/// </summary>
public class AIInsights
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public List<AITrendAnalysis> TrendAnalysis { get; set; } = new();

    public List<AIAudienceInsight> AudienceInsights { get; set; } = new();

    public List<AIContentRecommendation> ContentRecommendations { get; set; } = new();

    public AIPerformancePrediction PerformancePrediction { get; set; } = new();
}

public class AITrendAnalysis
{
    public string Topic { get; set; } = string.Empty;

    public double TrendScore { get; set; }

    public List<string> RelatedKeywords { get; set; } = new();

    public string TrendDirection { get; set; } = string.Empty;

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

public class AIAudienceInsight
{
    public string Demographic { get; set; } = string.Empty;

    public double EngagementRate { get; set; }

    public List<string> PreferredContent { get; set; } = new();

    public string OptimalPostTime { get; set; } = string.Empty;

    public List<SocialPlatform> ActivePlatforms { get; set; } = new();
}

public class AIContentRecommendation
{
    public string ContentType { get; set; } = string.Empty;

    public string Topic { get; set; } = string.Empty;

    public double PotentialReach { get; set; }

    public string Reasoning { get; set; } = string.Empty;

    public List<string> SuggestedHashtags { get; set; } = new();
}

public class AIPerformancePrediction
{
    public double PredictedEngagement { get; set; }

    public double PredictedReach { get; set; }

    public double ConfidenceLevel { get; set; }

    public List<string> Factors { get; set; } = new();

    public Dictionary<SocialPlatform, double> PlatformPerformance { get; set; } = new();
}