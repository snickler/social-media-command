using System;
using System.Collections.Generic;
using System.Linq;

namespace SocialMediaCommander.Services.Helpers;

/// <summary>
/// Analyzes sentiment in text content
/// Extracted from FoundryLocalAIService to reduce cyclic complexity
/// </summary>
public class SentimentAnalyzer
{
    private readonly Dictionary<string, double> _positiveWords;
    private readonly Dictionary<string, double> _negativeWords;

    public SentimentAnalyzer()
    {
        _positiveWords = InitializePositiveWords();
        _negativeWords = InitializeNegativeWords();
    }

    /// <summary>
    /// Analyzes sentiment of content and returns a score between 0 (negative) and 1 (positive)
    /// </summary>
    public double AnalyzeSentiment(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0.5; // Neutral

        var words = content.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var positiveScore = 0.0;
        var negativeScore = 0.0;
        var totalWords = words.Length;

        foreach (var word in words)
        {
            var cleanWord = CleanWord(word);

            if (_positiveWords.ContainsKey(cleanWord))
            {
                positiveScore += _positiveWords[cleanWord];
            }

            if (_negativeWords.ContainsKey(cleanWord))
            {
                negativeScore += _negativeWords[cleanWord];
            }
        }

        // Normalize scores
        positiveScore = positiveScore / totalWords;
        negativeScore = negativeScore / totalWords;

        // Calculate final sentiment
        if (positiveScore == 0 && negativeScore == 0)
            return 0.5; // Neutral

        return positiveScore / (positiveScore + negativeScore);
    }

    /// <summary>
    /// Gets sentiment category as string
    /// </summary>
    public string GetSentimentCategory(double sentimentScore)
    {
        return sentimentScore switch
        {
            >= 0.6 => "Positive",
            <= 0.4 => "Negative",
            _ => "Neutral"
        };
    }

    /// <summary>
    /// Provides detailed sentiment analysis with breakdown
    /// </summary>
    public SentimentAnalysisResult AnalyzeDetailed(string content)
    {
        var score = AnalyzeSentiment(content);
        var category = GetSentimentCategory(score);
        var confidence = CalculateConfidence(content);
        var keywords = ExtractSentimentKeywords(content);

        return new SentimentAnalysisResult
        {
            Score = score,
            Category = category,
            Confidence = confidence,
            PositiveKeywords = keywords.Positive,
            NegativeKeywords = keywords.Negative,
            Analysis = GenerateAnalysisText(score, category, confidence)
        };
    }

    private Dictionary<string, double> InitializePositiveWords()
    {
        return new Dictionary<string, double>
        {
            ["amazing"] = 1.0,
            ["awesome"] = 1.0,
            ["excellent"] = 1.0,
            ["fantastic"] = 1.0,
            ["wonderful"] = 1.0,
            ["outstanding"] = 1.0,
            ["perfect"] = 1.0,
            ["brilliant"] = 1.0,
            ["great"] = 0.8,
            ["good"] = 0.7,
            ["nice"] = 0.6,
            ["best"] = 0.9,
            ["love"] = 0.8,
            ["like"] = 0.6,
            ["enjoy"] = 0.7,
            ["happy"] = 0.8,
            ["excited"] = 0.8,
            ["thrilled"] = 0.9,
            ["delighted"] = 0.8,
            ["pleased"] = 0.7,
            ["satisfied"] = 0.7,
            ["impressed"] = 0.7,
            ["beautiful"] = 0.7,
            ["helpful"] = 0.6,
            ["useful"] = 0.6,
            ["valuable"] = 0.7,
            ["effective"] = 0.6,
            ["successful"] = 0.7,
            ["positive"] = 0.6,
            ["better"] = 0.5,
            ["improved"] = 0.6,
            ["recommend"] = 0.6,
            ["support"] = 0.5,
            ["trust"] = 0.6,
            ["reliable"] = 0.6,
            ["quality"] = 0.5,
            ["professional"] = 0.5
        };
    }

    private Dictionary<string, double> InitializeNegativeWords()
    {
        return new Dictionary<string, double>
        {
            ["terrible"] = 1.0,
            ["awful"] = 1.0,
            ["horrible"] = 1.0,
            ["disgusting"] = 1.0,
            ["worst"] = 1.0,
            ["hate"] = 0.9,
            ["dislike"] = 0.7,
            ["bad"] = 0.7,
            ["poor"] = 0.6,
            ["disappointing"] = 0.8,
            ["frustrated"] = 0.7,
            ["annoying"] = 0.6,
            ["irritating"] = 0.6,
            ["useless"] = 0.8,
            ["worthless"] = 0.8,
            ["failed"] = 0.7,
            ["failure"] = 0.7,
            ["broken"] = 0.6,
            ["wrong"] = 0.5,
            ["difficult"] = 0.4,
            ["hard"] = 0.3,
            ["problem"] = 0.5,
            ["issue"] = 0.4,
            ["trouble"] = 0.5,
            ["concern"] = 0.4,
            ["worried"] = 0.6,
            ["confused"] = 0.5,
            ["upset"] = 0.7,
            ["angry"] = 0.8,
            ["sad"] = 0.6,
            ["unhappy"] = 0.7,
            ["disappointed"] = 0.7,
            ["unsatisfied"] = 0.6,
            ["unreliable"] = 0.6,
            ["untrust"] = 0.7,
            ["unsafe"] = 0.6,
            ["slow"] = 0.4,
            ["expensive"] = 0.4,
            ["cheap"] = 0.3
        };
    }

    private string CleanWord(string word)
    {
        // Remove punctuation and convert to lowercase
        return new string(word.Where(c => char.IsLetter(c)).ToArray()).ToLower();
    }

    private double CalculateConfidence(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0.0;

        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sentimentWords = 0;

        foreach (var word in words)
        {
            var cleanWord = CleanWord(word);
            if (_positiveWords.ContainsKey(cleanWord) || _negativeWords.ContainsKey(cleanWord))
            {
                sentimentWords++;
            }
        }

        // Confidence based on proportion of sentiment words
        return Math.Min(1.0, (double)sentimentWords / Math.Max(1, words.Length) * 5);
    }

    private (List<string> Positive, List<string> Negative) ExtractSentimentKeywords(string content)
    {
        var positive = new List<string>();
        var negative = new List<string>();

        if (string.IsNullOrWhiteSpace(content))
            return (positive, negative);

        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            var cleanWord = CleanWord(word);

            if (_positiveWords.ContainsKey(cleanWord))
            {
                positive.Add(cleanWord);
            }

            if (_negativeWords.ContainsKey(cleanWord))
            {
                negative.Add(cleanWord);
            }
        }

        return (positive.Distinct().ToList(), negative.Distinct().ToList());
    }

    private string GenerateAnalysisText(double score, string category, double confidence)
    {
        var confidenceLevel = confidence switch
        {
            >= 0.8 => "very high",
            >= 0.6 => "high",
            >= 0.4 => "moderate",
            >= 0.2 => "low",
            _ => "very low"
        };

        return $"Sentiment is {category.ToLower()} (score: {score:F2}) with {confidenceLevel} confidence.";
    }
}

/// <summary>
/// Result of detailed sentiment analysis
/// </summary>
public class SentimentAnalysisResult
{
    public double Score { get; set; }
    public string Category { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public List<string> PositiveKeywords { get; set; } = new();
    public List<string> NegativeKeywords { get; set; } = new();
    public string Analysis { get; set; } = string.Empty;
}