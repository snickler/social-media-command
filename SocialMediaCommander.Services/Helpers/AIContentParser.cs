using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Helpers;

/// <summary>
/// Parses AI-generated content and extracts structured information
/// Extracted from FoundryLocalAIService to reduce cyclic complexity
/// </summary>
public class AIContentParser
{
    /// <summary>
    /// Parses generated content response into structured format
    /// </summary>
    public List<AIGeneratedContent> ParseGeneratedContent(string response, AIContentRequest request)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return new List<AIGeneratedContent>();
        }

        var content = new AIGeneratedContent
        {
            Content = response.Trim(),
            CharacterCount = response.Length,
            ConfidenceScore = CalculateConfidenceScore(response),
            Platform = request.TargetPlatforms.FirstOrDefault(),
            Hashtags = ExtractHashtags(response),
            Suggestions = GenerateSuggestions(response)
        };

        return new List<AIGeneratedContent> { content };
    }

    /// <summary>
    /// Parses optimization response into structured format
    /// </summary>
    public AIOptimizationResponse ParseOptimizationResponse(string response, AIOptimizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return new AIOptimizationResponse
            {
                OptimizedContent = string.Empty,
                ImprovementScore = 0.0,
                Analysis = new AIAnalysis()
            };
        }

        return new AIOptimizationResponse
        {
            OptimizedContent = response.Trim(),
            Suggestions = ExtractOptimizationSuggestions(response),
            ImprovementScore = CalculatePerformanceScore(response),
            Analysis = new AIAnalysis
            {
                SentimentScore = CalculateSimpleSentiment(response),
                EngagementPotential = CalculatePerformanceScore(response),
                RecommendedHashtags = ExtractHashtags(response)
            }
        };
    }

    /// <summary>
    /// Parses analysis response into structured format
    /// </summary>
    public AIAnalysis ParseAnalysisResponse(string response, string originalContent, SocialPlatform platform)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return new AIAnalysis
            {
                SentimentScore = 0.5,
                ReadabilityScore = 0.5,
                EngagementPotential = 0.5,
                RecommendedHashtags = new List<string>()
            };
        }

        return new AIAnalysis
        {
            SentimentScore = CalculateSimpleSentiment(response),
            ReadabilityScore = CalculateReadabilityScore(response),
            EngagementPotential = CalculatePerformanceScore(response),
            RecommendedHashtags = ExtractHashtags(response),
            KeywordDensity = ExtractTopicsFromAnalysis(response),
            OptimalPostTime = "12:00 PM" // Simplified
        };
    }

    /// <summary>
    /// Extracts hashtags from content
    /// </summary>
    public List<string> ExtractHashtags(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new List<string>();
        }

        var hashtags = new List<string>();
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            if (word.StartsWith("#") && word.Length > 1)
            {
                var hashtag = word.Substring(1);
                // Remove punctuation from the end
                hashtag = hashtag.TrimEnd('.', ',', '!', '?', ';', ':');
                if (!string.IsNullOrEmpty(hashtag))
                {
                    hashtags.Add(hashtag);
                }
            }
        }

        return hashtags.Distinct().ToList();
    }

    /// <summary>
    /// Parses hashtag list from AI response
    /// </summary>
    public List<string> ParseHashtags(string response, int maxCount)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return new List<string>();
        }

        var hashtags = new List<string>();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("#"))
            {
                hashtags.Add(trimmed.Substring(1));
            }
            else if (trimmed.Contains("#"))
            {
                var extractedHashtags = ExtractHashtags(trimmed);
                hashtags.AddRange(extractedHashtags);
            }
        }

        return hashtags.Distinct().Take(maxCount).ToList();
    }

    private double CalculateConfidenceScore(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0.0;

        // Base confidence
        double confidence = 0.5;

        // Length factor (optimal length gets higher confidence)
        if (content.Length >= 50 && content.Length <= 500)
            confidence += 0.2;

        // Coherence factor (presence of complete sentences)
        if (content.Contains(".") && !content.StartsWith("#"))
            confidence += 0.1;

        // Structure factor (presence of hashtags)
        if (ExtractHashtags(content).Any())
            confidence += 0.1;

        // Grammar factor (basic check for capitalization)
        if (char.IsUpper(content.FirstOrDefault()))
            confidence += 0.1;

        return Math.Min(1.0, confidence);
    }

    private List<string> GenerateSuggestions(string content)
    {
        var suggestions = new List<string>();

        if (content.Length < 50)
        {
            suggestions.Add("Consider adding more detail to increase engagement");
        }

        if (!ExtractHashtags(content).Any())
        {
            suggestions.Add("Add relevant hashtags to improve discoverability");
        }

        if (!content.Contains("?"))
        {
            suggestions.Add("Consider adding a question to encourage interaction");
        }

        if (suggestions.Count == 0)
        {
            suggestions.Add("Content looks good - ready to publish!");
        }

        return suggestions;
    }

    private List<string> ExtractImprovements(string response)
    {
        // Extract improvement suggestions from AI response
        var improvements = new List<string>();

        if (response.Contains("improved") || response.Contains("optimized"))
        {
            improvements.Add("Content has been optimized for better engagement");
        }

        if (response.Contains("#"))
        {
            improvements.Add("Added relevant hashtags for better discoverability");
        }

        if (response.Contains("?"))
        {
            improvements.Add("Added question to encourage user interaction");
        }

        return improvements.Any() ? improvements : new List<string> { "Content has been optimized" };
    }

    private List<AIOptimizationSuggestion> ExtractOptimizationSuggestions(string response)
    {
        var suggestions = new List<AIOptimizationSuggestion>();

        if (response.Contains("hashtag"))
        {
            suggestions.Add(new AIOptimizationSuggestion
            {
                Type = "Hashtags",
                Suggestion = "Add relevant hashtags for better discoverability",
                Reason = "Hashtags increase content visibility",
                Impact = 0.2
            });
        }

        if (response.Contains("?"))
        {
            suggestions.Add(new AIOptimizationSuggestion
            {
                Type = "Engagement",
                Suggestion = "Include questions to encourage interaction",
                Reason = "Questions drive engagement",
                Impact = 0.15
            });
        }

        return suggestions;
    }

    private double CalculateSimpleSentiment(string content)
    {
        var positiveWords = new[] { "great", "excellent", "amazing", "wonderful", "fantastic", "good", "best", "love", "awesome" };
        var negativeWords = new[] { "bad", "terrible", "awful", "hate", "worst", "horrible", "disappointing", "poor" };

        var words = content.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var positiveCount = words.Count(w => positiveWords.Contains(w));
        var negativeCount = words.Count(w => negativeWords.Contains(w));

        if (positiveCount == 0 && negativeCount == 0) return 0.5;

        return (double)positiveCount / (positiveCount + negativeCount);
    }

    private double CalculateReadabilityScore(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0.0;

        // Simple readability based on sentence and word length
        var sentences = content.Split('.', '!', '?').Length;
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        if (sentences == 0) return 0.5;

        var avgWordsPerSentence = (double)words / sentences;

        // Optimal range is 15-20 words per sentence
        if (avgWordsPerSentence >= 15 && avgWordsPerSentence <= 20)
            return 1.0;

        if (avgWordsPerSentence < 10 || avgWordsPerSentence > 30)
            return 0.3;

        return 0.7;
    }

    private double CalculatePerformanceScore(string content)
    {
        // Calculate predicted performance score based on content characteristics
        double score = 0.5; // Base score

        // Length optimization
        if (content.Length >= 100 && content.Length <= 300)
            score += 0.2;

        // Hashtag presence
        if (ExtractHashtags(content).Count > 0)
            score += 0.15;

        // Question presence
        if (content.Contains("?"))
            score += 0.1;

        // Call to action presence
        var ctaWords = new[] { "click", "visit", "check", "read", "watch", "follow", "share", "like" };
        if (ctaWords.Any(word => content.ToLower().Contains(word)))
            score += 0.15;

        return Math.Min(1.0, score);
    }

    private string ExtractSentimentFromAnalysis(string response)
    {
        var responseLower = response.ToLower();

        if (responseLower.Contains("positive") || responseLower.Contains("good") || responseLower.Contains("great"))
            return "Positive";

        if (responseLower.Contains("negative") || responseLower.Contains("bad") || responseLower.Contains("poor"))
            return "Negative";

        return "Neutral";
    }

    private List<string> ExtractTopicsFromAnalysis(string response)
    {
        var topics = new List<string>();
        var responseLower = response.ToLower();

        var topicKeywords = new Dictionary<string, string[]>
        {
            ["Technology"] = new[] { "tech", "ai", "software", "digital", "computer", "internet", "data" },
            ["Business"] = new[] { "business", "marketing", "sales", "entrepreneur", "startup", "finance" },
            ["Social Media"] = new[] { "social", "media", "content", "engagement", "followers", "viral" },
            ["Education"] = new[] { "learn", "education", "training", "course", "knowledge", "study" },
            ["Health"] = new[] { "health", "fitness", "wellness", "medical", "care", "exercise" },
            ["Entertainment"] = new[] { "fun", "entertainment", "music", "movie", "game", "sport" },
            ["News"] = new[] { "news", "current", "event", "update", "breaking", "report" }
        };

        foreach (var topic in topicKeywords)
        {
            if (topic.Value.Any(keyword => responseLower.Contains(keyword)))
            {
                topics.Add(topic.Key);
            }
        }

        return topics.Any() ? topics : new List<string> { "General" };
    }

    private List<string> ExtractRecommendations(string response)
    {
        var recommendations = new List<string>();

        // Extract recommendations from AI response
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("•") || trimmed.StartsWith("-") || trimmed.StartsWith("*"))
            {
                recommendations.Add(trimmed.Substring(1).Trim());
            }
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("Content analysis completed successfully");
        }

        return recommendations;
    }

    private double CalculateContentScore(string response)
    {
        // Calculate content quality score based on analysis response
        double score = 0.5; // Base score

        var responseLower = response.ToLower();

        if (responseLower.Contains("excellent") || responseLower.Contains("great"))
            score += 0.3;
        else if (responseLower.Contains("good"))
            score += 0.2;
        else if (responseLower.Contains("fair") || responseLower.Contains("average"))
            score += 0.1;

        return Math.Min(1.0, score);
    }
}