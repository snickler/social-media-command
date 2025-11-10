using FluentAssertions;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Helpers;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Tests for AIContentParser helper class
/// </summary>
public class AIContentParserTests
{
    private readonly AIContentParser _sut;

    public AIContentParserTests()
    {
        _sut = new AIContentParser();
    }

    [Fact]
    public void Constructor_ShouldInitialize_Successfully()
    {
        // Act
        var parser = new AIContentParser();

        // Assert
        parser.Should().NotBeNull();
    }

    #region ParseGeneratedContent Tests

    [Fact]
    public void ParseGeneratedContent_ShouldReturnEmptyList_WhenResponseIsNull()
    {
        // Arrange
        var request = new AIContentRequest
        {
            TargetPlatforms = new List<SocialPlatform> { SocialPlatform.BlueSky }
        };

        // Act
        var result = _sut.ParseGeneratedContent(null!, request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseGeneratedContent_ShouldReturnEmptyList_WhenResponseIsEmpty()
    {
        // Arrange
        var request = new AIContentRequest
        {
            TargetPlatforms = new List<SocialPlatform> { SocialPlatform.BlueSky }
        };

        // Act
        var result = _sut.ParseGeneratedContent(string.Empty, request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseGeneratedContent_ShouldReturnContent_WhenResponseIsValid()
    {
        // Arrange
        var response = "Check out this amazing product! #tech #innovation";
        var request = new AIContentRequest
        {
            TargetPlatforms = new List<SocialPlatform> { SocialPlatform.BlueSky }
        };

        // Act
        var result = _sut.ParseGeneratedContent(response, request);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Content.Should().Be(response);
        result[0].Platform.Should().Be(SocialPlatform.BlueSky);
    }

    [Fact]
    public void ParseGeneratedContent_ShouldTrimResponse()
    {
        // Arrange
        var response = "  Content with whitespace  ";
        var request = new AIContentRequest
        {
            TargetPlatforms = new List<SocialPlatform> { SocialPlatform.BlueSky }
        };

        // Act
        var result = _sut.ParseGeneratedContent(response, request);

        // Assert
        result[0].Content.Should().Be("Content with whitespace");
    }

    [Fact]
    public void ParseGeneratedContent_ShouldSetCharacterCount()
    {
        // Arrange
        var response = "Test content";
        var request = new AIContentRequest
        {
            TargetPlatforms = new List<SocialPlatform> { SocialPlatform.BlueSky }
        };

        // Act
        var result = _sut.ParseGeneratedContent(response, request);

        // Assert
        result[0].CharacterCount.Should().Be(response.Length);
    }

    [Fact]
    public void ParseGeneratedContent_ShouldExtractHashtags()
    {
        // Arrange
        var response = "Great content #tech #ai #innovation";
        var request = new AIContentRequest
        {
            TargetPlatforms = new List<SocialPlatform> { SocialPlatform.BlueSky }
        };

        // Act
        var result = _sut.ParseGeneratedContent(response, request);

        // Assert
        result[0].Hashtags.Should().Contain("tech");
        result[0].Hashtags.Should().Contain("ai");
        result[0].Hashtags.Should().Contain("innovation");
    }

    [Fact]
    public void ParseGeneratedContent_ShouldGenerateSuggestions()
    {
        // Arrange
        var response = "Content";
        var request = new AIContentRequest
        {
            TargetPlatforms = new List<SocialPlatform> { SocialPlatform.BlueSky }
        };

        // Act
        var result = _sut.ParseGeneratedContent(response, request);

        // Assert
        result[0].Suggestions.Should().NotBeEmpty();
    }

    #endregion

    #region ParseOptimizationResponse Tests

    [Fact]
    public void ParseOptimizationResponse_ShouldReturnEmptyResponse_WhenResponseIsNull()
    {
        // Arrange
        var request = new AIOptimizationRequest();

        // Act
        var result = _sut.ParseOptimizationResponse(null!, request);

        // Assert
        result.Should().NotBeNull();
        result.OptimizedContent.Should().BeEmpty();
        result.ImprovementScore.Should().Be(0.0);
        result.Analysis.Should().NotBeNull();
    }

    [Fact]
    public void ParseOptimizationResponse_ShouldReturnEmptyResponse_WhenResponseIsEmpty()
    {
        // Arrange
        var request = new AIOptimizationRequest();

        // Act
        var result = _sut.ParseOptimizationResponse(string.Empty, request);

        // Assert
        result.Should().NotBeNull();
        result.OptimizedContent.Should().BeEmpty();
        result.ImprovementScore.Should().Be(0.0);
    }

    [Fact]
    public void ParseOptimizationResponse_ShouldParseValidResponse()
    {
        // Arrange
        var response = "Optimized content with #hashtags and call to action: click here!";
        var request = new AIOptimizationRequest();

        // Act
        var result = _sut.ParseOptimizationResponse(response, request);

        // Assert
        result.Should().NotBeNull();
        result.OptimizedContent.Should().Be(response);
        result.ImprovementScore.Should().BeGreaterThan(0);
        result.Analysis.Should().NotBeNull();
    }

    [Fact]
    public void ParseOptimizationResponse_ShouldCalculateSentimentScore()
    {
        // Arrange
        var response = "This is an amazing and excellent optimization!";
        var request = new AIOptimizationRequest();

        // Act
        var result = _sut.ParseOptimizationResponse(response, request);

        // Assert
        result.Analysis.SentimentScore.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void ParseOptimizationResponse_ShouldExtractHashtags()
    {
        // Arrange
        var response = "Content with #tech #ai #innovation";
        var request = new AIOptimizationRequest();

        // Act
        var result = _sut.ParseOptimizationResponse(response, request);

        // Assert
        result.Analysis.RecommendedHashtags.Should().Contain("tech");
        result.Analysis.RecommendedHashtags.Should().Contain("ai");
    }

    #endregion

    #region ParseAnalysisResponse Tests

    [Fact]
    public void ParseAnalysisResponse_ShouldReturnDefaultAnalysis_WhenResponseIsNull()
    {
        // Act
        var result = _sut.ParseAnalysisResponse(null!, "original", SocialPlatform.BlueSky);

        // Assert
        result.Should().NotBeNull();
        result.SentimentScore.Should().Be(0.5);
        result.ReadabilityScore.Should().Be(0.5);
        result.EngagementPotential.Should().Be(0.5);
        result.RecommendedHashtags.Should().BeEmpty();
    }

    [Fact]
    public void ParseAnalysisResponse_ShouldReturnDefaultAnalysis_WhenResponseIsEmpty()
    {
        // Act
        var result = _sut.ParseAnalysisResponse(string.Empty, "original", SocialPlatform.BlueSky);

        // Assert
        result.Should().NotBeNull();
        result.SentimentScore.Should().Be(0.5);
        result.ReadabilityScore.Should().Be(0.5);
        result.EngagementPotential.Should().Be(0.5);
    }

    [Fact]
    public void ParseAnalysisResponse_ShouldParseValidResponse()
    {
        // Arrange
        var response = "Great analysis with excellent insights! #tech #ai";

        // Act
        var result = _sut.ParseAnalysisResponse(response, "original", SocialPlatform.BlueSky);

        // Assert
        result.Should().NotBeNull();
        result.SentimentScore.Should().BeGreaterThan(0);
        result.ReadabilityScore.Should().BeGreaterThan(0);
        result.EngagementPotential.Should().BeGreaterThan(0);
        result.RecommendedHashtags.Should().Contain("tech");
    }

    [Fact]
    public void ParseAnalysisResponse_ShouldSetOptimalPostTime()
    {
        // Arrange
        var response = "Analysis content";

        // Act
        var result = _sut.ParseAnalysisResponse(response, "original", SocialPlatform.BlueSky);

        // Assert
        result.OptimalPostTime.Should().Be("12:00 PM");
    }

    [Fact]
    public void ParseAnalysisResponse_ShouldExtractKeywordDensity()
    {
        // Arrange
        var response = "This is tech and ai content about social media";

        // Act
        var result = _sut.ParseAnalysisResponse(response, "original", SocialPlatform.BlueSky);

        // Assert
        result.KeywordDensity.Should().NotBeNull();
        result.KeywordDensity.Should().NotBeEmpty();
    }

    #endregion

    #region ExtractHashtags Tests

    [Fact]
    public void ExtractHashtags_ShouldReturnEmptyList_WhenContentIsNull()
    {
        // Act
        var result = _sut.ExtractHashtags(null!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractHashtags_ShouldReturnEmptyList_WhenContentIsEmpty()
    {
        // Act
        var result = _sut.ExtractHashtags(string.Empty);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractHashtags_ShouldReturnEmptyList_WhenNoHashtags()
    {
        // Act
        var result = _sut.ExtractHashtags("Content without hashtags");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractHashtags_ShouldExtractSingleHashtag()
    {
        // Act
        var result = _sut.ExtractHashtags("Check out this #tech content");

        // Assert
        result.Should().ContainSingle();
        result.Should().Contain("tech");
    }

    [Fact]
    public void ExtractHashtags_ShouldExtractMultipleHashtags()
    {
        // Act
        var result = _sut.ExtractHashtags("Great #tech #ai #innovation content");

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("tech");
        result.Should().Contain("ai");
        result.Should().Contain("innovation");
    }

    [Fact]
    public void ExtractHashtags_ShouldRemoveTrailingPunctuation()
    {
        // Act
        var result = _sut.ExtractHashtags("Check #tech! #ai? #innovation.");

        // Assert
        result.Should().Contain("tech");
        result.Should().Contain("ai");
        result.Should().Contain("innovation");
    }

    [Fact]
    public void ExtractHashtags_ShouldReturnDistinctHashtags()
    {
        // Act
        var result = _sut.ExtractHashtags("Content #tech #tech #ai #ai");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("tech");
        result.Should().Contain("ai");
    }

    [Fact]
    public void ExtractHashtags_ShouldIgnoreSingleHashSymbol()
    {
        // Act
        var result = _sut.ExtractHashtags("Content with # symbol but no hashtag");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractHashtags_ShouldHandleCamelCaseHashtags()
    {
        // Act
        var result = _sut.ExtractHashtags("Content with #TechNews #AIInnovation");

        // Assert
        result.Should().Contain("TechNews");
        result.Should().Contain("AIInnovation");
    }

    #endregion

    #region ParseHashtags Tests

    [Fact]
    public void ParseHashtags_ShouldReturnEmptyList_WhenResponseIsNull()
    {
        // Act
        var result = _sut.ParseHashtags(null!, 10);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseHashtags_ShouldReturnEmptyList_WhenResponseIsEmpty()
    {
        // Act
        var result = _sut.ParseHashtags(string.Empty, 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseHashtags_ShouldExtractHashtagsFromLines()
    {
        // Arrange
        var response = "#Tech\n#AI\n#Innovation";

        // Act
        var result = _sut.ParseHashtags(response, 10);

        // Assert
        result.Should().Contain("Tech");
        result.Should().Contain("AI");
        result.Should().Contain("Innovation");
    }

    [Fact]
    public void ParseHashtags_ShouldRespectMaxCount()
    {
        // Arrange
        var response = "#Tech\n#AI\n#Innovation\n#Business\n#Marketing";

        // Act
        var result = _sut.ParseHashtags(response, 3);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public void ParseHashtags_ShouldExtractHashtagsFromMixedContent()
    {
        // Arrange
        var response = "Here are some hashtags: #Tech #AI\nAlso: #Innovation";

        // Act
        var result = _sut.ParseHashtags(response, 10);

        // Assert
        result.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void ParseHashtags_ShouldReturnDistinctHashtags()
    {
        // Arrange
        var response = "#Tech\n#Tech\n#AI\n#AI";

        // Act
        var result = _sut.ParseHashtags(response, 10);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public void ExtractHashtags_ShouldHandleLongContent()
    {
        // Arrange
        var content = string.Join(" ", Enumerable.Repeat("#tech", 100));

        // Act
        var result = _sut.ExtractHashtags(content);

        // Assert
        result.Should().ContainSingle();
        result.Should().Contain("tech");
    }

    [Fact]
    public void ParseGeneratedContent_ShouldHandleContentWithSpecialCharacters()
    {
        // Arrange
        var response = "Content with émojis 🚀 and spëcial chârs!";
        var request = new AIContentRequest
        {
            TargetPlatforms = new List<SocialPlatform> { SocialPlatform.BlueSky }
        };

        // Act
        var result = _sut.ParseGeneratedContent(response, request);

        // Assert
        result.Should().HaveCount(1);
        result[0].Content.Should().Be(response);
    }

    [Fact]
    public void ParseOptimizationResponse_ShouldHandleContentWithMultipleCallsToAction()
    {
        // Arrange
        var response = "Click here to learn more! Visit our site and check it out. Share with friends!";
        var request = new AIOptimizationRequest();

        // Act
        var result = _sut.ParseOptimizationResponse(response, request);

        // Assert
        result.ImprovementScore.Should().BeGreaterThan(0.5);
    }

    [Theory]
    [InlineData("Short")]
    [InlineData("This is a medium length content with some details that should score well")]
    [InlineData("This is a very long content that goes on and on with lots of details and information that might be too much for a single social media post but we're testing it anyway to see how the parser handles extremely long content that might exceed typical limits")]
    public void ParseGeneratedContent_ShouldHandleVariousLengths(string content)
    {
        // Arrange
        var request = new AIContentRequest
        {
            TargetPlatforms = new List<SocialPlatform> { SocialPlatform.BlueSky }
        };

        // Act
        var result = _sut.ParseGeneratedContent(content, request);

        // Assert
        result.Should().HaveCount(1);
        result[0].Content.Should().Be(content);
        result[0].CharacterCount.Should().Be(content.Length);
    }

    [Fact]
    public void ParseAnalysisResponse_ShouldDetectTechnologyTopics()
    {
        // Arrange
        var response = "This content discusses ai, software, and digital technology";

        // Act
        var result = _sut.ParseAnalysisResponse(response, "original", SocialPlatform.BlueSky);

        // Assert
        result.KeywordDensity.Should().Contain("Technology");
    }

    [Fact]
    public void ParseAnalysisResponse_ShouldDetectSocialMediaTopics()
    {
        // Arrange
        var response = "Great social media content about engagement and followers";

        // Act
        var result = _sut.ParseAnalysisResponse(response, "original", SocialPlatform.BlueSky);

        // Assert
        result.KeywordDensity.Should().Contain("Social Media");
    }

    [Fact]
    public void ExtractHashtags_ShouldHandleHashtagsAtStartOfContent()
    {
        // Act
        var result = _sut.ExtractHashtags("#tech content here");

        // Assert
        result.Should().Contain("tech");
    }

    [Fact]
    public void ExtractHashtags_ShouldHandleHashtagsAtEndOfContent()
    {
        // Act
        var result = _sut.ExtractHashtags("content here #tech");

        // Assert
        result.Should().Contain("tech");
    }

    #endregion
}
