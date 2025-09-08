using System;
using System.Collections.Generic;
using System.Linq;

namespace SocialMediaCommander.Services.Helpers;

/// <summary>
/// Extracts topics from text content using keyword-based analysis
/// Extracted from FoundryLocalAIService to reduce cyclic complexity
/// </summary>
public class TopicExtractor
{
    private readonly Dictionary<string, TopicDefinition> _topicDefinitions;

    public TopicExtractor()
    {
        _topicDefinitions = InitializeTopicDefinitions();
    }

    /// <summary>
    /// Extracts topics from content based on keyword matching
    /// </summary>
    public List<string> ExtractTopics(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new List<string> { "General" };

        var contentLower = content.ToLower();
        var topics = new List<(string Topic, double Score)>();

        foreach (var topic in _topicDefinitions)
        {
            var score = CalculateTopicScore(contentLower, topic.Value);
            if (score > 0)
            {
                topics.Add((topic.Key, score));
            }
        }

        // Return topics sorted by relevance score
        var selectedTopics = topics
            .OrderByDescending(t => t.Score)
            .Take(5) // Limit to top 5 topics
            .Select(t => t.Topic)
            .ToList();

        return selectedTopics.Any() ? selectedTopics : new List<string> { "General" };
    }

    /// <summary>
    /// Extracts topics with confidence scores
    /// </summary>
    public List<TopicResult> ExtractTopicsWithScores(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new List<TopicResult> { new() { Topic = "General", Score = 1.0 } };

        var contentLower = content.ToLower();
        var topics = new List<TopicResult>();

        foreach (var topic in _topicDefinitions)
        {
            var score = CalculateTopicScore(contentLower, topic.Value);
            if (score > 0)
            {
                topics.Add(new TopicResult
                {
                    Topic = topic.Key,
                    Score = score,
                    MatchedKeywords = GetMatchedKeywords(contentLower, topic.Value),
                    Description = topic.Value.Description
                });
            }
        }

        // Return topics sorted by relevance score
        var sortedTopics = topics
            .OrderByDescending(t => t.Score)
            .Take(5) // Limit to top 5 topics
            .ToList();

        return sortedTopics.Any() ? sortedTopics : new List<TopicResult> { new() { Topic = "General", Score = 1.0 } };
    }

    /// <summary>
    /// Gets the primary topic (highest scoring topic)
    /// </summary>
    public string GetPrimaryTopic(string content)
    {
        var topics = ExtractTopicsWithScores(content);
        return topics.FirstOrDefault()?.Topic ?? "General";
    }

    /// <summary>
    /// Analyzes topic diversity in content
    /// </summary>
    public TopicAnalysis AnalyzeTopicDiversity(string content)
    {
        var topics = ExtractTopicsWithScores(content);

        return new TopicAnalysis
        {
            TopicCount = topics.Count,
            PrimaryTopic = topics.FirstOrDefault()?.Topic ?? "General",
            AllTopics = topics,
            DiversityScore = CalculateDiversityScore(topics),
            Analysis = GenerateTopicAnalysis(topics)
        };
    }

    private Dictionary<string, TopicDefinition> InitializeTopicDefinitions()
    {
        return new Dictionary<string, TopicDefinition>
        {
            ["Technology"] = new()
            {
                Description = "Technology and digital innovation",
                PrimaryKeywords = new[] { "tech", "technology", "ai", "artificial intelligence", "software", "digital", "computer", "programming", "code", "data", "algorithm", "machine learning", "blockchain", "crypto", "internet", "web", "app", "mobile", "cloud", "automation" },
                SecondaryKeywords = new[] { "innovation", "digital transformation", "cybersecurity", "iot", "virtual reality", "augmented reality", "5g", "quantum", "developer", "engineer", "startup", "silicon valley" },
                Weight = 1.0
            },
            ["Business"] = new()
            {
                Description = "Business, entrepreneurship, and finance",
                PrimaryKeywords = new[] { "business", "entrepreneur", "startup", "company", "corporation", "finance", "investment", "revenue", "profit", "marketing", "sales", "strategy", "leadership", "management", "economy", "market", "industry", "commerce", "trade" },
                SecondaryKeywords = new[] { "growth", "scaling", "funding", "venture capital", "ipo", "merger", "acquisition", "competition", "customer", "client", "brand", "networking", "partnership", "negotiation" },
                Weight = 1.0
            },
            ["Social Media"] = new()
            {
                Description = "Social media and digital communication",
                PrimaryKeywords = new[] { "social media", "social", "media", "content", "post", "tweet", "share", "like", "follow", "followers", "engagement", "viral", "hashtag", "influencer", "instagram", "facebook", "twitter", "linkedin", "tiktok", "youtube" },
                SecondaryKeywords = new[] { "creator", "audience", "community", "platform", "algorithm", "reach", "impression", "analytics", "story", "reel", "live stream", "podcast", "blog", "vlog" },
                Weight = 1.0
            },
            ["Education"] = new()
            {
                Description = "Education, learning, and knowledge sharing",
                PrimaryKeywords = new[] { "education", "learning", "knowledge", "study", "student", "teacher", "school", "university", "college", "course", "training", "skill", "development", "academic", "research", "tutorial", "lesson", "workshop", "certification" },
                SecondaryKeywords = new[] { "online learning", "e-learning", "mooc", "curriculum", "pedagogy", "scholarship", "degree", "diploma", "graduation", "classroom", "lecture", "seminar", "mentoring" },
                Weight = 1.0
            },
            ["Health"] = new()
            {
                Description = "Health, wellness, and medical topics",
                PrimaryKeywords = new[] { "health", "wellness", "medical", "healthcare", "fitness", "exercise", "diet", "nutrition", "mental health", "physical health", "doctor", "medicine", "treatment", "therapy", "prevention", "symptoms", "diagnosis", "recovery" },
                SecondaryKeywords = new[] { "hospital", "clinic", "patient", "nurse", "pharmacy", "vaccine", "disease", "illness", "injury", "surgery", "rehabilitation", "meditation", "yoga", "gym", "workout" },
                Weight = 1.0
            },
            ["Entertainment"] = new()
            {
                Description = "Entertainment, media, and leisure",
                PrimaryKeywords = new[] { "entertainment", "movie", "film", "music", "game", "gaming", "sport", "sports", "fun", "comedy", "drama", "action", "concert", "festival", "show", "television", "tv", "streaming", "netflix", "disney" },
                SecondaryKeywords = new[] { "celebrity", "actor", "musician", "artist", "director", "producer", "album", "song", "video game", "esports", "team", "player", "championship", "tournament", "theater", "performance" },
                Weight = 1.0
            },
            ["News"] = new()
            {
                Description = "News, current events, and journalism",
                PrimaryKeywords = new[] { "news", "current events", "breaking news", "update", "report", "journalism", "media", "press", "announcement", "alert", "happening", "event", "story", "coverage", "investigation", "headline", "article" },
                SecondaryKeywords = new[] { "reporter", "journalist", "newspaper", "magazine", "broadcast", "live", "exclusive", "interview", "statement", "conference", "release", "bulletin", "trending", "viral news" },
                Weight = 1.0
            },
            ["Travel"] = new()
            {
                Description = "Travel, tourism, and exploration",
                PrimaryKeywords = new[] { "travel", "tourism", "vacation", "holiday", "trip", "journey", "destination", "explore", "adventure", "visit", "flight", "hotel", "booking", "sightseeing", "culture", "country", "city", "beach", "mountain" },
                SecondaryKeywords = new[] { "backpacking", "cruise", "resort", "guide", "itinerary", "passport", "visa", "airline", "airport", "luggage", "photography", "local", "cuisine", "landmark", "museum" },
                Weight = 1.0
            },
            ["Food"] = new()
            {
                Description = "Food, cooking, and culinary topics",
                PrimaryKeywords = new[] { "food", "cooking", "recipe", "cuisine", "restaurant", "chef", "meal", "dish", "ingredient", "flavor", "taste", "kitchen", "culinary", "dining", "eat", "drink", "beverage", "wine", "coffee" },
                SecondaryKeywords = new[] { "gourmet", "foodie", "gastronomy", "baking", "grilling", "vegetarian", "vegan", "organic", "farm to table", "street food", "fast food", "fine dining", "food truck", "delivery" },
                Weight = 1.0
            },
            ["Science"] = new()
            {
                Description = "Science, research, and discovery",
                PrimaryKeywords = new[] { "science", "research", "discovery", "experiment", "laboratory", "scientist", "study", "analysis", "theory", "hypothesis", "evidence", "data", "observation", "biology", "chemistry", "physics", "astronomy", "geology" },
                SecondaryKeywords = new[] { "innovation", "breakthrough", "publication", "peer review", "methodology", "statistics", "climate", "environment", "space", "universe", "dna", "gene", "evolution", "ecology" },
                Weight = 1.0
            }
        };
    }

    private double CalculateTopicScore(string contentLower, TopicDefinition topic)
    {
        var score = 0.0;
        var wordCount = contentLower.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        // Score for primary keywords
        foreach (var keyword in topic.PrimaryKeywords)
        {
            if (contentLower.Contains(keyword.ToLower()))
            {
                score += topic.Weight * 2.0; // Primary keywords have double weight
            }
        }

        // Score for secondary keywords
        foreach (var keyword in topic.SecondaryKeywords)
        {
            if (contentLower.Contains(keyword.ToLower()))
            {
                score += topic.Weight;
            }
        }

        // Normalize by content length to avoid bias toward longer content
        return Math.Min(1.0, score / Math.Max(1, wordCount / 10.0));
    }

    private List<string> GetMatchedKeywords(string contentLower, TopicDefinition topic)
    {
        var matched = new List<string>();

        foreach (var keyword in topic.PrimaryKeywords.Concat(topic.SecondaryKeywords))
        {
            if (contentLower.Contains(keyword.ToLower()))
            {
                matched.Add(keyword);
            }
        }

        return matched;
    }

    private double CalculateDiversityScore(List<TopicResult> topics)
    {
        if (topics.Count <= 1)
            return 0.0;

        // Calculate diversity based on number of topics and score distribution
        var totalScore = topics.Sum(t => t.Score);
        var normalizedScores = topics.Select(t => t.Score / totalScore).ToList();

        // Calculate entropy-like measure for diversity
        var diversity = -normalizedScores.Sum(p => p * Math.Log(p, 2));
        return Math.Min(1.0, diversity / Math.Log(topics.Count, 2));
    }

    private string GenerateTopicAnalysis(List<TopicResult> topics)
    {
        if (!topics.Any())
            return "Content appears to be general in nature.";

        var primary = topics.First();
        var analysis = $"Primary topic: {primary.Topic} (confidence: {primary.Score:P0})";

        if (topics.Count > 1)
        {
            var secondary = topics.Skip(1).Take(2).Select(t => t.Topic);
            analysis += $". Also covers: {string.Join(", ", secondary)}.";
        }

        return analysis;
    }
}

/// <summary>
/// Definition of a topic with keywords and weights
/// </summary>
public class TopicDefinition
{
    public string Description { get; set; } = string.Empty;
    public string[] PrimaryKeywords { get; set; } = Array.Empty<string>();
    public string[] SecondaryKeywords { get; set; } = Array.Empty<string>();
    public double Weight { get; set; } = 1.0;
}

/// <summary>
/// Result of topic extraction with metadata
/// </summary>
public class TopicResult
{
    public string Topic { get; set; } = string.Empty;
    public double Score { get; set; }
    public List<string> MatchedKeywords { get; set; } = new();
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Comprehensive topic analysis result
/// </summary>
public class TopicAnalysis
{
    public int TopicCount { get; set; }
    public string PrimaryTopic { get; set; } = string.Empty;
    public List<TopicResult> AllTopics { get; set; } = new();
    public double DiversityScore { get; set; }
    public string Analysis { get; set; } = string.Empty;
}