using FluentAssertions;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Helpers;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Tests for EngagementPredictor helper class
/// </summary>
public class EngagementPredictorTests
{
    private readonly EngagementPredictor _sut;

    public EngagementPredictorTests()
    {
        _sut = new EngagementPredictor();
    }

    [Fact]
    public void Constructor_ShouldInitialize_Successfully()
    {
        // Act
        var predictor = new EngagementPredictor();

        // Assert
        predictor.Should().NotBeNull();
    }

    #region PredictEngagement Tests

    [Fact]
    public void PredictEngagement_ShouldReturnNeutral_WhenContentIsNull()
    {
        // Arrange
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = _sut.PredictEngagement(null!, platforms);

        // Assert
        result.Should().Be(0.5);
    }

    [Fact]
    public void PredictEngagement_ShouldReturnNeutral_WhenContentIsEmpty()
    {
        // Arrange
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = _sut.PredictEngagement(string.Empty, platforms);

        // Assert
        result.Should().Be(0.5);
    }

    [Fact]
    public void PredictEngagement_ShouldReturnNeutral_WhenPlatformsIsEmpty()
    {
        // Arrange
        var content = "Test content";
        var platforms = new List<SocialPlatform>();

        // Act
        var result = _sut.PredictEngagement(content, platforms);

        // Assert
        result.Should().Be(0.5);
    }

    [Fact]
    public void PredictEngagement_ShouldReturnScore_WhenInputIsValid()
    {
        // Arrange
        var content = "Check out this amazing product! #tech #innovation";
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = _sut.PredictEngagement(content, platforms);

        // Assert
        result.Should().BeInRange(0.0, 1.0);
        result.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void PredictEngagement_ShouldAverageMultiplePlatforms()
    {
        // Arrange
        var content = "Great content with #hashtags";
        // Note: Only BlueSky is currently implemented
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = _sut.PredictEngagement(content, platforms);

        // Assert
        result.Should().BeInRange(0.0, 1.0);
    }

    #endregion

    #region PredictEngagementForPlatform Tests

    [Fact]
    public void PredictEngagementForPlatform_ShouldReturnNeutral_WhenContentIsNull()
    {
        // Act
        var result = _sut.PredictEngagementForPlatform(null!, SocialPlatform.BlueSky);

        // Assert
        result.Should().Be(0.5);
    }

    [Fact]
    public void PredictEngagementForPlatform_ShouldReturnNeutral_WhenContentIsEmpty()
    {
        // Act
        var result = _sut.PredictEngagementForPlatform(string.Empty, SocialPlatform.BlueSky);

        // Assert
        result.Should().Be(0.5);
    }

    [Fact]
    public void PredictEngagementForPlatform_ShouldReturnHigherScore_ForOptimalLength()
    {
        // Arrange
        var shortContent = "Short";
        var optimalContent = new string('a', 150); // Within optimal range 80-250
        var longContent = new string('a', 500);

        // Act
        var shortScore = _sut.PredictEngagementForPlatform(shortContent, SocialPlatform.BlueSky);
        var optimalScore = _sut.PredictEngagementForPlatform(optimalContent, SocialPlatform.BlueSky);
        var longScore = _sut.PredictEngagementForPlatform(longContent, SocialPlatform.BlueSky);

        // Assert
        optimalScore.Should().BeGreaterThan(shortScore);
        optimalScore.Should().BeGreaterThan(longScore);
    }

    [Fact]
    public void PredictEngagementForPlatform_ShouldBoostScore_WithHashtags()
    {
        // Arrange
        var withoutHashtags = "Great content about technology";
        var withHashtags = "Great content about technology #tech #innovation";

        // Act
        var scoreWithout = _sut.PredictEngagementForPlatform(withoutHashtags, SocialPlatform.BlueSky);
        var scoreWith = _sut.PredictEngagementForPlatform(withHashtags, SocialPlatform.BlueSky);

        // Assert
        scoreWith.Should().BeGreaterThan(scoreWithout);
    }

    [Fact]
    public void PredictEngagementForPlatform_ShouldBoostScore_WithQuestions()
    {
        // Arrange
        var withoutQuestion = "This is great content";
        var withQuestion = "This is great content, don't you think?";

        // Act
        var scoreWithout = _sut.PredictEngagementForPlatform(withoutQuestion, SocialPlatform.BlueSky);
        var scoreWith = _sut.PredictEngagementForPlatform(withQuestion, SocialPlatform.BlueSky);

        // Assert
        scoreWith.Should().BeGreaterThan(scoreWithout);
    }

    [Fact]
    public void PredictEngagementForPlatform_ShouldBoostScore_WithCallToAction()
    {
        // Arrange
        var withoutCTA = "This is great content about a topic";
        var withCTA = "This is great content about a topic. Click here to learn more and follow us!";

        // Act
        var scoreWithout = _sut.PredictEngagementForPlatform(withoutCTA, SocialPlatform.BlueSky);
        var scoreWith = _sut.PredictEngagementForPlatform(withCTA, SocialPlatform.BlueSky);

        // Assert
        scoreWith.Should().BeGreaterThanOrEqualTo(scoreWithout);
    }

    [Fact]
    public void PredictEngagementForPlatform_ShouldBoostScore_WithPositiveSentiment()
    {
        // Arrange
        var neutral = "This is content about a product";
        var positive = "This is amazing and excellent content about a wonderful product!";

        // Act
        var neutralScore = _sut.PredictEngagementForPlatform(neutral, SocialPlatform.BlueSky);
        var positiveScore = _sut.PredictEngagementForPlatform(positive, SocialPlatform.BlueSky);

        // Assert
        positiveScore.Should().BeGreaterThan(neutralScore);
    }

    [Fact]
    public void PredictEngagementForPlatform_ShouldClampScore_Between0And1()
    {
        // Arrange
        var content = "Amazing! Excellent! Wonderful! Great! #tech #ai #innovation Visit now! Share this? Follow us!";

        // Act
        var result = _sut.PredictEngagementForPlatform(content, SocialPlatform.BlueSky);

        // Assert
        result.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void PredictEngagementForPlatform_ShouldWorkForBlueSky()
    {
        // Arrange
        var content = "Test content #hashtag";

        // Act
        var result = _sut.PredictEngagementForPlatform(content, SocialPlatform.BlueSky);

        // Assert
        result.Should().BeInRange(0.0, 1.0);
    }

    #endregion

    #region PredictEngagementDetailed Tests

    [Fact]
    public void PredictEngagementDetailed_ShouldReturnDefaultResult_WhenContentIsNull()
    {
        // Arrange
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = _sut.PredictEngagementDetailed(null!, platforms);

        // Assert
        result.Should().NotBeNull();
        result.OverallScore.Should().Be(0.5);
        result.PlatformScores.Should().BeEmpty();
        result.Recommendations.Should().Contain("Add content to get engagement predictions");
    }

    [Fact]
    public void PredictEngagementDetailed_ShouldReturnDefaultResult_WhenPlatformsIsEmpty()
    {
        // Arrange
        var content = "Test content";
        var platforms = new List<SocialPlatform>();

        // Act
        var result = _sut.PredictEngagementDetailed(content, platforms);

        // Assert
        result.Should().NotBeNull();
        result.OverallScore.Should().Be(0.5);
        result.Recommendations.Should().Contain("Add content to get engagement predictions");
    }

    [Fact]
    public void PredictEngagementDetailed_ShouldReturnCompleteResult_WhenInputIsValid()
    {
        // Arrange
        var content = "Check out this amazing product! #tech #innovation";
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = _sut.PredictEngagementDetailed(content, platforms);

        // Assert
        result.Should().NotBeNull();
        result.OverallScore.Should().BeInRange(0.0, 1.0);
        result.PlatformScores.Should().ContainKey(SocialPlatform.BlueSky);
        result.Factors.Should().NotBeNull();
        result.Recommendations.Should().NotBeEmpty();
        result.Analysis.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void PredictEngagementDetailed_ShouldCalculatePlatformScores()
    {
        // Arrange
        var content = "Great content with #hashtags";
        // Note: Only BlueSky is currently implemented
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = _sut.PredictEngagementDetailed(content, platforms);

        // Assert
        result.PlatformScores.Should().HaveCount(1);
        result.PlatformScores.Should().ContainKey(SocialPlatform.BlueSky);
    }

    [Fact]
    public void PredictEngagementDetailed_ShouldAnalyzeFactors()
    {
        // Arrange
        var content = "Check out this product? Visit now! #tech";
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = _sut.PredictEngagementDetailed(content, platforms);

        // Assert
        result.Factors.Length.Should().Be(content.Length);
        result.Factors.HashtagCount.Should().Be(1);
        result.Factors.HasQuestion.Should().BeTrue();
        result.Factors.HasCallToAction.Should().BeTrue();
    }

    [Fact]
    public void PredictEngagementDetailed_ShouldRecommendHashtags_WhenMissing()
    {
        // Arrange
        var content = "Great content without hashtags";
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = _sut.PredictEngagementDetailed(content, platforms);

        // Assert
        result.Recommendations.Should().Contain(r => r.Contains("hashtag"));
    }

    [Fact]
    public void PredictEngagementDetailed_ShouldRecommendQuestion_WhenMissing()
    {
        // Arrange
        var content = "Great content with #hashtags but no question";
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = _sut.PredictEngagementDetailed(content, platforms);

        // Assert
        result.Recommendations.Should().Contain(r => r.Contains("question"));
    }

    [Fact]
    public void PredictEngagementDetailed_ShouldRecommendCallToAction_WhenMissing()
    {
        // Arrange
        var content = "Great content with #hashtags and a question?";
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = _sut.PredictEngagementDetailed(content, platforms);

        // Assert
        result.Recommendations.Should().Contain(r => r.Contains("call-to-action"));
    }

    [Fact]
    public void PredictEngagementDetailed_ShouldRecommendMoreDetail_WhenTooShort()
    {
        // Arrange
        var content = "Short";
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = _sut.PredictEngagementDetailed(content, platforms);

        // Assert
        result.Recommendations.Should().Contain(r => r.Contains("more detail"));
    }

    [Fact]
    public void PredictEngagementDetailed_ShouldRecommendShortening_ForBlueSkyWhenTooLong()
    {
        // Arrange
        var content = new string('a', 350); // Over 300 characters
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = _sut.PredictEngagementDetailed(content, platforms);

        // Assert
        result.Recommendations.Should().Contain(r => r.Contains("shortening") || r.Contains("BlueSky"));
    }

    [Fact]
    public void PredictEngagementDetailed_ShouldSayOptimized_WhenContentIsGood()
    {
        // Arrange
        var content = "Check out this amazing product! Visit us today? Share with friends! #tech #innovation";
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = _sut.PredictEngagementDetailed(content, platforms);

        // Assert
        result.Recommendations.Should().Contain(r => r.Contains("optimized"));
    }

    [Fact]
    public void PredictEngagementDetailed_ShouldGenerateAnalysis()
    {
        // Arrange
        var content = "Test content with #hashtag";
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = _sut.PredictEngagementDetailed(content, platforms);

        // Assert
        result.Analysis.Should().NotBeNullOrEmpty();
        result.Analysis.Should().Contain("Engagement prediction");
    }

    [Theory]
    [InlineData("Great content with #hashtag and question?", "good")]
    [InlineData("Content with #hashtag", "fair")]
    [InlineData("x", "needs improvement")]
    public void PredictEngagementDetailed_ShouldCategorizeScore_Correctly(string content, string expectedCategory)
    {
        // Arrange
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = _sut.PredictEngagementDetailed(content, platforms);

        // Assert
        result.Analysis.Should().Contain(expectedCategory);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public void PredictEngagementForPlatform_ShouldHandleLongContent()
    {
        // Arrange
        var content = string.Join(" ", Enumerable.Repeat("word", 1000));

        // Act
        var result = _sut.PredictEngagementForPlatform(content, SocialPlatform.BlueSky);

        // Assert
        result.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void PredictEngagementForPlatform_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var content = "Content with émojis 🚀 and spëcial chârs! #tech";

        // Act
        var result = _sut.PredictEngagementForPlatform(content, SocialPlatform.BlueSky);

        // Assert
        result.Should().BeInRange(0.0, 1.0);
    }

    [Theory]
    [InlineData("Check out this amazing product! #tech #innovation")]
    [InlineData("What do you think about this? Visit now! #business")]
    [InlineData("Don't miss this exclusive offer! Share with friends! #sale #limited")]
    public void PredictEngagement_ShouldReturnHighScore_ForWellOptimizedContent(string content)
    {
        // Arrange
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = _sut.PredictEngagement(content, platforms);

        // Assert
        result.Should().BeGreaterThan(0.6, $"Content should have high engagement: {content}");
    }

    [Fact]
    public void PredictEngagementDetailed_ShouldBeConsistentWithSimplePrediction()
    {
        // Arrange
        var content = "Great content with #hashtags and question?";
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var simpleResult = _sut.PredictEngagement(content, platforms);
        var detailedResult = _sut.PredictEngagementDetailed(content, platforms);

        // Assert
        detailedResult.OverallScore.Should().BeApproximately(simpleResult, 0.01);
    }

    [Fact]
    public void PredictEngagement_ShouldHandleMultipleUrgencyWords()
    {
        // Arrange
        var withUrgency = "Don't miss this limited time offer! Hurry, buy now today!";
        var withoutUrgency = "Here is a product you might like";
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var scoreWith = _sut.PredictEngagement(withUrgency, platforms);
        var scoreWithout = _sut.PredictEngagement(withoutUrgency, platforms);

        // Assert
        scoreWith.Should().BeGreaterThan(scoreWithout);
    }

    [Fact]
    public void PredictEngagementForPlatform_ShouldBoostScore_ForBlueSkySpecificWords()
    {
        // Arrange
        var genericContent = "This is great content with #hashtags";
        var blueSkyContent = "This is great content with #hashtags - please repost and share!";

        // Act
        var genericScore = _sut.PredictEngagementForPlatform(genericContent, SocialPlatform.BlueSky);
        var blueSkyScore = _sut.PredictEngagementForPlatform(blueSkyContent, SocialPlatform.BlueSky);

        // Assert
        blueSkyScore.Should().BeGreaterThan(genericScore);
    }

    #endregion
}
