using System;
using System.Collections.Generic;
using System.Linq;
using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Helpers;

/// <summary>
/// Predicts engagement levels for social media content
/// Extracted from FoundryLocalAIService to reduce cyclic complexity
/// </summary>
public class EngagementPredictor
{
    private readonly Dictionary<SocialPlatform, PlatformEngagementFactors> _platformFactors;

    public EngagementPredictor()
    {
        _platformFactors = InitializePlatformFactors();
    }

    /// <summary>
    /// Predicts engagement score for content on specified platforms
    /// </summary>
    public double PredictEngagement(string content, List<SocialPlatform> platforms)
    {
        if (string.IsNullOrWhiteSpace(content) || !platforms.Any())
            return 0.5; // Default neutral score

        var scores = platforms.Select(platform => PredictEngagementForPlatform(content, platform)).ToList();
        return scores.Average();
    }

    /// <summary>
    /// Predicts engagement for a specific platform
    /// </summary>
    public double PredictEngagementForPlatform(string content, SocialPlatform platform)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0.5;

        var factors = _platformFactors.GetValueOrDefault(platform, _platformFactors[SocialPlatform.X]);
        var baseScore = 0.5; // Start with neutral

        // Content length optimization
        baseScore += CalculateLengthScore(content, factors);

        // Hashtag optimization
        baseScore += CalculateHashtagScore(content, factors);

        // Engagement triggers
        baseScore += CalculateEngagementTriggerScore(content, factors);

        // Platform-specific features
        baseScore += CalculatePlatformSpecificScore(content, platform, factors);

        // Sentiment bonus
        baseScore += CalculateSentimentScore(content);

        return Math.Min(1.0, Math.Max(0.0, baseScore));
    }

    /// <summary>
    /// Provides detailed engagement prediction with breakdown
    /// </summary>
    public EngagementPredictionResult PredictEngagementDetailed(string content, List<SocialPlatform> platforms)
    {
        if (string.IsNullOrWhiteSpace(content) || !platforms.Any())
        {
            return new EngagementPredictionResult
            {
                OverallScore = 0.5,
                PlatformScores = new Dictionary<SocialPlatform, double>(),
                Factors = new EngagementFactors(),
                Recommendations = new List<string> { "Add content to get engagement predictions" }
            };
        }

        var platformScores = new Dictionary<SocialPlatform, double>();
        var allFactors = new List<EngagementFactors>();

        foreach (var platform in platforms)
        {
            var score = PredictEngagementForPlatform(content, platform);
            platformScores[platform] = score;
            allFactors.Add(AnalyzeFactors(content, platform));
        }

        var overallScore = platformScores.Values.Average();
        var averageFactors = CombineFactors(allFactors);
        var recommendations = GenerateRecommendations(content, averageFactors, platforms);

        return new EngagementPredictionResult
        {
            OverallScore = overallScore,
            PlatformScores = platformScores,
            Factors = averageFactors,
            Recommendations = recommendations,
            Analysis = GenerateAnalysis(overallScore, platforms)
        };
    }

    private Dictionary<SocialPlatform, PlatformEngagementFactors> InitializePlatformFactors()
    {
        return new Dictionary<SocialPlatform, PlatformEngagementFactors>
        {
            [SocialPlatform.X] = new()
            {
                OptimalLength = new Range(100, 280),
                OptimalHashtags = new Range(1, 3),
                QuestionMultiplier = 1.2,
                CallToActionMultiplier = 1.1,
                ImageMultiplier = 1.3,
                VideoMultiplier = 1.5,
                LinkPenalty = 0.9,
                EngagementWords = new[] { "retweet", "share", "comment", "like", "follow", "reply" }
            },
            [SocialPlatform.Facebook] = new()
            {
                OptimalLength = new Range(200, 500),
                OptimalHashtags = new Range(1, 2),
                QuestionMultiplier = 1.3,
                CallToActionMultiplier = 1.2,
                ImageMultiplier = 1.4,
                VideoMultiplier = 1.6,
                LinkPenalty = 0.8,
                EngagementWords = new[] { "share", "comment", "like", "follow", "tag", "mention" }
            },
            [SocialPlatform.LinkedIn] = new()
            {
                OptimalLength = new Range(300, 800),
                OptimalHashtags = new Range(3, 5),
                QuestionMultiplier = 1.1,
                CallToActionMultiplier = 1.0,
                ImageMultiplier = 1.2,
                VideoMultiplier = 1.3,
                LinkPenalty = 1.0,
                EngagementWords = new[] { "share", "comment", "connect", "follow", "endorse", "recommend" }
            },
            [SocialPlatform.BlueSky] = new()
            {
                OptimalLength = new Range(80, 250),
                OptimalHashtags = new Range(1, 3),
                QuestionMultiplier = 1.2,
                CallToActionMultiplier = 1.1,
                ImageMultiplier = 1.3,
                VideoMultiplier = 1.4,
                LinkPenalty = 0.95,
                EngagementWords = new[] { "repost", "share", "reply", "like", "follow" }
            },
            [SocialPlatform.Threads] = new()
            {
                OptimalLength = new Range(100, 400),
                OptimalHashtags = new Range(1, 2),
                QuestionMultiplier = 1.3,
                CallToActionMultiplier = 1.1,
                ImageMultiplier = 1.4,
                VideoMultiplier = 1.5,
                LinkPenalty = 0.9,
                EngagementWords = new[] { "share", "reply", "like", "follow", "repost" }
            }
        };
    }

    private double CalculateLengthScore(string content, PlatformEngagementFactors factors)
    {
        var length = content.Length;

        if (length >= factors.OptimalLength.Start && length <= factors.OptimalLength.End)
            return 0.2; // Optimal length bonus

        if (length < factors.OptimalLength.Start)
            return -0.1; // Too short penalty

        if (length > factors.OptimalLength.End * 1.5)
            return -0.2; // Too long penalty

        return 0.0; // Acceptable length
    }

    private double CalculateHashtagScore(string content, PlatformEngagementFactors factors)
    {
        var hashtagCount = content.Count(c => c == '#');

        if (hashtagCount >= factors.OptimalHashtags.Start && hashtagCount <= factors.OptimalHashtags.End)
            return 0.15; // Optimal hashtag count bonus

        if (hashtagCount == 0)
            return -0.1; // No hashtags penalty (for most platforms)

        if (hashtagCount > factors.OptimalHashtags.End)
            return -0.05; // Too many hashtags penalty

        return 0.0;
    }

    private double CalculateEngagementTriggerScore(string content, PlatformEngagementFactors factors)
    {
        var score = 0.0;
        var contentLower = content.ToLower();

        // Question bonus
        if (content.Contains("?"))
            score += (factors.QuestionMultiplier - 1.0) * 0.1;

        // Call to action bonus
        if (factors.EngagementWords.Any(word => contentLower.Contains(word)))
            score += (factors.CallToActionMultiplier - 1.0) * 0.1;

        // Urgency words
        var urgencyWords = new[] { "now", "today", "limited", "exclusive", "hurry", "don't miss" };
        if (urgencyWords.Any(word => contentLower.Contains(word)))
            score += 0.05;

        return score;
    }

    private double CalculatePlatformSpecificScore(string content, SocialPlatform platform, PlatformEngagementFactors factors)
    {
        var score = 0.0;
        var contentLower = content.ToLower();

        switch (platform)
        {
            case SocialPlatform.LinkedIn:
                // Professional content bonus
                var professionalWords = new[] { "professional", "career", "business", "industry", "experience", "skills" };
                if (professionalWords.Any(word => contentLower.Contains(word)))
                    score += 0.1;
                break;

            case SocialPlatform.X:
                // Trending topics bonus (simplified)
                if (contentLower.Contains("breaking") || contentLower.Contains("news"))
                    score += 0.05;
                break;

            case SocialPlatform.Facebook:
                // Personal connection bonus
                var personalWords = new[] { "family", "friends", "community", "local", "personal" };
                if (personalWords.Any(word => contentLower.Contains(word)))
                    score += 0.08;
                break;
        }

        return score;
    }

    private double CalculateSentimentScore(string content)
    {
        // Simple sentiment boost for positive content
        var positiveWords = new[] { "great", "amazing", "excellent", "wonderful", "love", "awesome", "fantastic" };
        var contentLower = content.ToLower();

        return positiveWords.Any(word => contentLower.Contains(word)) ? 0.05 : 0.0;
    }

    private EngagementFactors AnalyzeFactors(string content, SocialPlatform platform)
    {
        return new EngagementFactors
        {
            Length = content.Length,
            HashtagCount = content.Count(c => c == '#'),
            HasQuestion = content.Contains("?"),
            HasCallToAction = HasCallToAction(content),
            SentimentScore = CalculateSentimentScore(content),
            Platform = platform
        };
    }

    private bool HasCallToAction(string content)
    {
        var ctaWords = new[] { "click", "visit", "check", "read", "watch", "follow", "share", "like", "comment", "subscribe" };
        return ctaWords.Any(word => content.ToLower().Contains(word));
    }

    private EngagementFactors CombineFactors(List<EngagementFactors> factors)
    {
        if (!factors.Any())
            return new EngagementFactors();

        return new EngagementFactors
        {
            Length = factors.First().Length, // Same content, same length
            HashtagCount = factors.First().HashtagCount,
            HasQuestion = factors.First().HasQuestion,
            HasCallToAction = factors.First().HasCallToAction,
            SentimentScore = factors.Average(f => f.SentimentScore),
            Platform = factors.First().Platform // Use first platform as reference
        };
    }

    private List<string> GenerateRecommendations(string content, EngagementFactors factors, List<SocialPlatform> platforms)
    {
        var recommendations = new List<string>();

        if (factors.Length < 50)
            recommendations.Add("Add more detail to increase engagement");

        if (factors.HashtagCount == 0)
            recommendations.Add("Add relevant hashtags to improve discoverability");

        if (!factors.HasQuestion)
            recommendations.Add("Consider adding a question to encourage interaction");

        if (!factors.HasCallToAction)
            recommendations.Add("Include a call-to-action to drive engagement");

        if (platforms.Contains(SocialPlatform.LinkedIn) && factors.Length < 200)
            recommendations.Add("LinkedIn posts perform better with more detailed content");

        if (platforms.Contains(SocialPlatform.X) && factors.Length > 280)
            recommendations.Add("Consider shortening for X/Twitter's character limit");

        return recommendations.Any() ? recommendations : new List<string> { "Content looks optimized for engagement!" };
    }

    private string GenerateAnalysis(double score, List<SocialPlatform> platforms)
    {
        var scoreCategory = score switch
        {
            >= 0.8 => "excellent",
            >= 0.6 => "good",
            >= 0.4 => "fair",
            _ => "needs improvement"
        };

        var platformNames = string.Join(", ", platforms.Select(p => p.ToString()));
        return $"Engagement prediction: {scoreCategory} ({score:P0}) for {platformNames}";
    }
}

/// <summary>
/// Platform-specific engagement factors
/// </summary>
public class PlatformEngagementFactors
{
    public Range OptimalLength { get; set; } = new(0, 100);
    public Range OptimalHashtags { get; set; } = new(0, 5);
    public double QuestionMultiplier { get; set; } = 1.0;
    public double CallToActionMultiplier { get; set; } = 1.0;
    public double ImageMultiplier { get; set; } = 1.0;
    public double VideoMultiplier { get; set; } = 1.0;
    public double LinkPenalty { get; set; } = 1.0;
    public string[] EngagementWords { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Simple range structure
/// </summary>
public class Range
{
    public int Start { get; set; }
    public int End { get; set; }

    public Range(int start, int end)
    {
        Start = start;
        End = end;
    }
}

/// <summary>
/// Factors that influence engagement
/// </summary>
public class EngagementFactors
{
    public int Length { get; set; }
    public int HashtagCount { get; set; }
    public bool HasQuestion { get; set; }
    public bool HasCallToAction { get; set; }
    public double SentimentScore { get; set; }
    public SocialPlatform Platform { get; set; }
}

/// <summary>
/// Detailed engagement prediction result
/// </summary>
public class EngagementPredictionResult
{
    public double OverallScore { get; set; }
    public Dictionary<SocialPlatform, double> PlatformScores { get; set; } = new();
    public EngagementFactors Factors { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string Analysis { get; set; } = string.Empty;
}