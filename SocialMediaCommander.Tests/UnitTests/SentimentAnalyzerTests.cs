using FluentAssertions;
using SocialMediaCommander.Services.Helpers;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Tests for SentimentAnalyzer helper class
/// </summary>
public class SentimentAnalyzerTests
{
    private readonly SentimentAnalyzer _sut;

    public SentimentAnalyzerTests()
    {
        _sut = new SentimentAnalyzer();
    }

    [Fact]
    public void Constructor_ShouldInitialize_Successfully()
    {
        // Act
        var analyzer = new SentimentAnalyzer();

        // Assert
        analyzer.Should().NotBeNull();
    }

    #region AnalyzeSentiment Tests

    [Fact]
    public void AnalyzeSentiment_ShouldReturnNeutral_WhenContentIsNull()
    {
        // Act
        var result = _sut.AnalyzeSentiment(null!);

        // Assert
        result.Should().Be(0.5);
    }

    [Fact]
    public void AnalyzeSentiment_ShouldReturnNeutral_WhenContentIsEmpty()
    {
        // Act
        var result = _sut.AnalyzeSentiment(string.Empty);

        // Assert
        result.Should().Be(0.5);
    }

    [Fact]
    public void AnalyzeSentiment_ShouldReturnNeutral_WhenContentIsWhitespace()
    {
        // Act
        var result = _sut.AnalyzeSentiment("   ");

        // Assert
        result.Should().Be(0.5);
    }

    [Fact]
    public void AnalyzeSentiment_ShouldReturnNeutral_WhenContentHasNoSentimentWords()
    {
        // Act
        var result = _sut.AnalyzeSentiment("The quick brown fox jumps over the lazy dog");

        // Assert
        result.Should().Be(0.5);
    }

    [Fact]
    public void AnalyzeSentiment_ShouldReturnPositive_WhenContentHasPositiveWords()
    {
        // Act
        var result = _sut.AnalyzeSentiment("This is amazing and wonderful!");

        // Assert
        result.Should().BeGreaterThan(0.6);
    }

    [Fact]
    public void AnalyzeSentiment_ShouldReturnNegative_WhenContentHasNegativeWords()
    {
        // Act
        var result = _sut.AnalyzeSentiment("This is terrible and awful!");

        // Assert
        result.Should().BeLessThan(0.4);
    }

    [Fact]
    public void AnalyzeSentiment_ShouldReturnPositive_WhenMorePositiveThanNegative()
    {
        // Act
        var result = _sut.AnalyzeSentiment("This is great great great but slightly bad");

        // Assert
        result.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void AnalyzeSentiment_ShouldHandlePunctuation_Correctly()
    {
        // Act
        var result = _sut.AnalyzeSentiment("Amazing! Wonderful! Great!!!");

        // Assert
        result.Should().BeGreaterThan(0.6);
    }

    [Fact]
    public void AnalyzeSentiment_ShouldBeCaseInsensitive()
    {
        // Arrange
        var content1 = "AMAZING WONDERFUL";
        var content2 = "amazing wonderful";

        // Act
        var result1 = _sut.AnalyzeSentiment(content1);
        var result2 = _sut.AnalyzeSentiment(content2);

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void AnalyzeSentiment_ShouldHandleMixedSentiment_Correctly()
    {
        // Act
        var result = _sut.AnalyzeSentiment("I love this amazing product but the price is terrible");

        // Assert
        result.Should().BeInRange(0.3, 0.7); // Mixed sentiment
    }

    #endregion

    #region GetSentimentCategory Tests

    [Theory]
    [InlineData(0.8, "Positive")]
    [InlineData(0.6, "Positive")]
    [InlineData(0.5, "Neutral")]
    [InlineData(0.4, "Negative")]
    [InlineData(0.2, "Negative")]
    [InlineData(0.0, "Negative")]
    [InlineData(1.0, "Positive")]
    public void GetSentimentCategory_ShouldReturnCorrectCategory_ForScore(double score, string expectedCategory)
    {
        // Act
        var result = _sut.GetSentimentCategory(score);

        // Assert
        result.Should().Be(expectedCategory);
    }

    [Fact]
    public void GetSentimentCategory_ShouldReturnPositive_WhenScoreIsAbove0_6()
    {
        // Act
        var result = _sut.GetSentimentCategory(0.7);

        // Assert
        result.Should().Be("Positive");
    }

    [Fact]
    public void GetSentimentCategory_ShouldReturnNegative_WhenScoreIsBelow0_4()
    {
        // Act
        var result = _sut.GetSentimentCategory(0.3);

        // Assert
        result.Should().Be("Negative");
    }

    [Fact]
    public void GetSentimentCategory_ShouldReturnNeutral_WhenScoreIsBetween0_4And0_6()
    {
        // Act
        var result = _sut.GetSentimentCategory(0.5);

        // Assert
        result.Should().Be("Neutral");
    }

    #endregion

    #region AnalyzeDetailed Tests

    [Fact]
    public void AnalyzeDetailed_ShouldReturnResult_WhenContentIsValid()
    {
        // Arrange
        var content = "This is an amazing and wonderful product!";

        // Act
        var result = _sut.AnalyzeDetailed(content);

        // Assert
        result.Should().NotBeNull();
        result.Score.Should().BeInRange(0, 1);
        result.Category.Should().NotBeNullOrEmpty();
        result.Confidence.Should().BeInRange(0, 1);
        result.PositiveKeywords.Should().NotBeNull();
        result.NegativeKeywords.Should().NotBeNull();
        result.Analysis.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AnalyzeDetailed_ShouldIdentifyPositiveKeywords_WhenContentHasPositiveWords()
    {
        // Arrange
        var content = "This is amazing and wonderful!";

        // Act
        var result = _sut.AnalyzeDetailed(content);

        // Assert
        result.PositiveKeywords.Should().Contain("amazing");
        result.PositiveKeywords.Should().Contain("wonderful");
    }

    [Fact]
    public void AnalyzeDetailed_ShouldIdentifyNegativeKeywords_WhenContentHasNegativeWords()
    {
        // Arrange
        var content = "This is terrible and awful!";

        // Act
        var result = _sut.AnalyzeDetailed(content);

        // Assert
        result.NegativeKeywords.Should().Contain("terrible");
        result.NegativeKeywords.Should().Contain("awful");
    }

    [Fact]
    public void AnalyzeDetailed_ShouldHaveHighConfidence_WhenContentHasManySentimentWords()
    {
        // Arrange
        var content = "amazing wonderful excellent fantastic great";

        // Act
        var result = _sut.AnalyzeDetailed(content);

        // Assert
        result.Confidence.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void AnalyzeDetailed_ShouldHaveLowConfidence_WhenContentHasFewSentimentWords()
    {
        // Arrange
        var content = "The quick brown fox jumps over the lazy dog with one good word";

        // Act
        var result = _sut.AnalyzeDetailed(content);

        // Assert
        result.Confidence.Should().BeLessThan(0.5);
    }

    [Fact]
    public void AnalyzeDetailed_ShouldGenerateAnalysisText_WithCorrectFormat()
    {
        // Arrange
        var content = "This is amazing!";

        // Act
        var result = _sut.AnalyzeDetailed(content);

        // Assert
        result.Analysis.Should().Contain("Sentiment is");
        result.Analysis.Should().Contain("score:");
        result.Analysis.Should().Contain("confidence");
    }

    [Fact]
    public void AnalyzeDetailed_ShouldReturnPositiveCategory_WhenContentIsPositive()
    {
        // Arrange
        var content = "This is amazing wonderful excellent fantastic outstanding perfect";

        // Act
        var result = _sut.AnalyzeDetailed(content);

        // Assert
        result.Category.Should().Be("Positive");
        result.Score.Should().BeGreaterThan(0.6);
    }

    [Fact]
    public void AnalyzeDetailed_ShouldReturnNegativeCategory_WhenContentIsNegative()
    {
        // Arrange
        var content = "This is terrible awful horrible disgusting worst";

        // Act
        var result = _sut.AnalyzeDetailed(content);

        // Assert
        result.Category.Should().Be("Negative");
        result.Score.Should().BeLessThan(0.4);
    }

    [Fact]
    public void AnalyzeDetailed_ShouldHandleNullContent_Gracefully()
    {
        // Act
        var result = _sut.AnalyzeDetailed(null!);

        // Assert
        result.Should().NotBeNull();
        result.Score.Should().Be(0.5);
        result.Category.Should().Be("Neutral");
    }

    [Fact]
    public void AnalyzeDetailed_ShouldHandleEmptyContent_Gracefully()
    {
        // Act
        var result = _sut.AnalyzeDetailed(string.Empty);

        // Assert
        result.Should().NotBeNull();
        result.Score.Should().Be(0.5);
        result.Category.Should().Be("Neutral");
    }

    [Fact]
    public void AnalyzeDetailed_ShouldNotContainDuplicateKeywords_WhenWordRepeats()
    {
        // Arrange
        var content = "amazing amazing amazing";

        // Act
        var result = _sut.AnalyzeDetailed(content);

        // Assert
        result.PositiveKeywords.Should().HaveCount(1);
        result.PositiveKeywords.Should().Contain("amazing");
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public void AnalyzeSentiment_ShouldHandleLongContent_Efficiently()
    {
        // Arrange
        var content = string.Join(" ", Enumerable.Repeat("This is amazing and wonderful", 1000));

        // Act
        var result = _sut.AnalyzeSentiment(content);

        // Assert
        result.Should().BeGreaterThan(0.6);
    }

    [Fact]
    public void AnalyzeSentiment_ShouldHandleSpecialCharacters_Correctly()
    {
        // Arrange
        var content = "Amazing!!! @#$%^&* Wonderful??? <<>> Great!!!";

        // Act
        var result = _sut.AnalyzeSentiment(content);

        // Assert
        result.Should().BeGreaterThan(0.6);
    }

    [Fact]
    public void AnalyzeSentiment_ShouldHandleNumbers_Correctly()
    {
        // Arrange
        var content = "This product is 100% amazing and 99% wonderful!";

        // Act
        var result = _sut.AnalyzeSentiment(content);

        // Assert
        result.Should().BeGreaterThan(0.6);
    }

    [Fact]
    public void AnalyzeDetailed_ShouldBeConsistentWithAnalyzeSentiment()
    {
        // Arrange
        var content = "This is an amazing product that I love!";

        // Act
        var detailedResult = _sut.AnalyzeDetailed(content);
        var simpleResult = _sut.AnalyzeSentiment(content);

        // Assert
        detailedResult.Score.Should().Be(simpleResult);
    }

    [Theory]
    [InlineData("I absolutely love this amazing product! It's fantastic and wonderful!")]
    [InlineData("Great service, excellent quality, very satisfied with my purchase!")]
    [InlineData("Outstanding performance, brilliant design, highly recommend!")]
    public void AnalyzeSentiment_ShouldReturnPositive_ForPositiveReviews(string content)
    {
        // Act
        var result = _sut.AnalyzeSentiment(content);

        // Assert
        result.Should().BeGreaterThan(0.6, $"Content should be positive: {content}");
    }

    [Theory]
    [InlineData("This is terrible! Worst product ever, completely disappointed.")]
    [InlineData("Awful experience, horrible quality, very unhappy with this purchase.")]
    [InlineData("Useless product, failed immediately, total waste of money.")]
    public void AnalyzeSentiment_ShouldReturnNegative_ForNegativeReviews(string content)
    {
        // Act
        var result = _sut.AnalyzeSentiment(content);

        // Assert
        result.Should().BeLessThan(0.4, $"Content should be negative: {content}");
    }

    #endregion
}
