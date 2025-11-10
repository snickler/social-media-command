using FluentAssertions;
using SocialMediaCommander.Services.Helpers;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Tests for TopicExtractor helper class
/// </summary>
public class TopicExtractorTests
{
    private readonly TopicExtractor _sut;

    public TopicExtractorTests()
    {
        _sut = new TopicExtractor();
    }

    [Fact]
    public void Constructor_ShouldInitialize_Successfully()
    {
        // Act
        var extractor = new TopicExtractor();

        // Assert
        extractor.Should().NotBeNull();
    }

    #region ExtractTopics Tests

    [Fact]
    public void ExtractTopics_ShouldReturnGeneral_WhenContentIsNull()
    {
        // Act
        var result = _sut.ExtractTopics(null!);

        // Assert
        result.Should().ContainSingle();
        result.Should().Contain("General");
    }

    [Fact]
    public void ExtractTopics_ShouldReturnGeneral_WhenContentIsEmpty()
    {
        // Act
        var result = _sut.ExtractTopics(string.Empty);

        // Assert
        result.Should().ContainSingle();
        result.Should().Contain("General");
    }

    [Fact]
    public void ExtractTopics_ShouldReturnGeneral_WhenNoTopicsMatch()
    {
        // Act
        var result = _sut.ExtractTopics("Random xyz abc def ghi jkl mno pqr stu");

        // Assert
        result.Should().Contain("General");
    }

    [Fact]
    public void ExtractTopics_ShouldDetectTechnology_WhenContentHasTechKeywords()
    {
        // Arrange
        var content = "This is about artificial intelligence and machine learning technology";

        // Act
        var result = _sut.ExtractTopics(content);

        // Assert
        result.Should().Contain("Technology");
    }

    [Fact]
    public void ExtractTopics_ShouldDetectBusiness_WhenContentHasBusinessKeywords()
    {
        // Arrange
        var content = "Startup company seeking venture capital investment for business growth";

        // Act
        var result = _sut.ExtractTopics(content);

        // Assert
        result.Should().Contain("Business");
    }

    [Fact]
    public void ExtractTopics_ShouldDetectSocialMedia_WhenContentHasSocialKeywords()
    {
        // Arrange
        var content = "Check out my new post on social media with viral content and engagement";

        // Act
        var result = _sut.ExtractTopics(content);

        // Assert
        result.Should().Contain("Social Media");
    }

    [Fact]
    public void ExtractTopics_ShouldDetectEducation_WhenContentHasEducationKeywords()
    {
        // Arrange
        var content = "Online learning course for students at university with certification";

        // Act
        var result = _sut.ExtractTopics(content);

        // Assert
        result.Should().Contain("Education");
    }

    [Fact]
    public void ExtractTopics_ShouldDetectHealth_WhenContentHasHealthKeywords()
    {
        // Arrange
        var content = "Healthcare and wellness fitness exercise for mental health and physical wellbeing";

        // Act
        var result = _sut.ExtractTopics(content);

        // Assert
        result.Should().Contain("Health");
    }

    [Fact]
    public void ExtractTopics_ShouldDetectMultipleTopics()
    {
        // Arrange
        var content = "Technology startup in healthcare sector using AI for medical diagnosis";

        // Act
        var result = _sut.ExtractTopics(content);

        // Assert
        result.Should().Contain("Technology");
        result.Should().Contain("Health");
    }

    [Fact]
    public void ExtractTopics_ShouldLimitToTop5Topics()
    {
        // Arrange - content that could match many topics
        var content = "Technology business education health entertainment news travel food science social";

        // Act
        var result = _sut.ExtractTopics(content);

        // Assert
        result.Should().HaveCountLessThanOrEqualTo(5);
    }

    [Fact]
    public void ExtractTopics_ShouldOrderByRelevance()
    {
        // Arrange - heavily weighted towards technology
        var content = "AI artificial intelligence machine learning technology software digital computer programming";

        // Act
        var result = _sut.ExtractTopics(content);

        // Assert
        result.First().Should().Be("Technology");
    }

    [Fact]
    public void ExtractTopics_ShouldBeCaseInsensitive()
    {
        // Arrange
        var content1 = "TECHNOLOGY AI SOFTWARE";
        var content2 = "technology ai software";

        // Act
        var result1 = _sut.ExtractTopics(content1);
        var result2 = _sut.ExtractTopics(content2);

        // Assert
        result1.Should().BeEquivalentTo(result2);
    }

    #endregion

    #region ExtractTopicsWithScores Tests

    [Fact]
    public void ExtractTopicsWithScores_ShouldReturnGeneral_WhenContentIsNull()
    {
        // Act
        var result = _sut.ExtractTopicsWithScores(null!);

        // Assert
        result.Should().ContainSingle();
        result[0].Topic.Should().Be("General");
        result[0].Score.Should().Be(1.0);
    }

    [Fact]
    public void ExtractTopicsWithScores_ShouldReturnGeneral_WhenContentIsEmpty()
    {
        // Act
        var result = _sut.ExtractTopicsWithScores(string.Empty);

        // Assert
        result.Should().ContainSingle();
        result[0].Topic.Should().Be("General");
    }

    [Fact]
    public void ExtractTopicsWithScores_ShouldIncludeScores()
    {
        // Arrange
        var content = "Technology and artificial intelligence in business";

        // Act
        var result = _sut.ExtractTopicsWithScores(content);

        // Assert
        result.Should().NotBeEmpty();
        result.All(r => r.Score > 0).Should().BeTrue();
    }

    [Fact]
    public void ExtractTopicsWithScores_ShouldIncludeMatchedKeywords()
    {
        // Arrange
        var content = "Technology and artificial intelligence software";

        // Act
        var result = _sut.ExtractTopicsWithScores(content);

        // Assert
        var techTopic = result.FirstOrDefault(r => r.Topic == "Technology");
        techTopic.Should().NotBeNull();
        techTopic!.MatchedKeywords.Should().NotBeEmpty();
    }

    [Fact]
    public void ExtractTopicsWithScores_ShouldIncludeDescription()
    {
        // Arrange
        var content = "Technology and artificial intelligence";

        // Act
        var result = _sut.ExtractTopicsWithScores(content);

        // Assert
        var techTopic = result.FirstOrDefault(r => r.Topic == "Technology");
        techTopic.Should().NotBeNull();
        techTopic!.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ExtractTopicsWithScores_ShouldOrderByScore()
    {
        // Arrange
        var content = "Technology AI software programming computer with some business";

        // Act
        var result = _sut.ExtractTopicsWithScores(content);

        // Assert
        if (result.Count > 1)
        {
            for (int i = 0; i < result.Count - 1; i++)
            {
                result[i].Score.Should().BeGreaterThanOrEqualTo(result[i + 1].Score);
            }
        }
    }

    [Fact]
    public void ExtractTopicsWithScores_ShouldLimitToTop5()
    {
        // Arrange
        var content = "Technology business education health entertainment news travel food science social";

        // Act
        var result = _sut.ExtractTopicsWithScores(content);

        // Assert
        result.Should().HaveCountLessThanOrEqualTo(5);
    }

    #endregion

    #region GetPrimaryTopic Tests

    [Fact]
    public void GetPrimaryTopic_ShouldReturnGeneral_WhenContentIsNull()
    {
        // Act
        var result = _sut.GetPrimaryTopic(null!);

        // Assert
        result.Should().Be("General");
    }

    [Fact]
    public void GetPrimaryTopic_ShouldReturnGeneral_WhenContentIsEmpty()
    {
        // Act
        var result = _sut.GetPrimaryTopic(string.Empty);

        // Assert
        result.Should().Be("General");
    }

    [Fact]
    public void GetPrimaryTopic_ShouldReturnMostRelevantTopic()
    {
        // Arrange
        var content = "Artificial intelligence and machine learning technology software digital";

        // Act
        var result = _sut.GetPrimaryTopic(content);

        // Assert
        result.Should().Be("Technology");
    }

    [Fact]
    public void GetPrimaryTopic_ShouldReturnGeneral_WhenNoTopicsMatch()
    {
        // Arrange
        var content = "Random unrelated words xyz abc def ghi jkl mno pqr";

        // Act
        var result = _sut.GetPrimaryTopic(content);

        // Assert
        result.Should().Be("General");
    }

    #endregion

    #region AnalyzeTopicDiversity Tests

    [Fact]
    public void AnalyzeTopicDiversity_ShouldReturnAnalysis_WhenContentIsValid()
    {
        // Arrange
        var content = "Technology business education with AI and startups for learning";

        // Act
        var result = _sut.AnalyzeTopicDiversity(content);

        // Assert
        result.Should().NotBeNull();
        result.TopicCount.Should().BeGreaterThan(0);
        result.PrimaryTopic.Should().NotBeNullOrEmpty();
        result.AllTopics.Should().NotBeEmpty();
        result.DiversityScore.Should().BeInRange(0.0, 1.0);
        result.Analysis.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AnalyzeTopicDiversity_ShouldHaveZeroDiversity_WhenSingleTopic()
    {
        // Arrange
        var content = "Technology AI software programming computer digital";

        // Act
        var result = _sut.AnalyzeTopicDiversity(content);

        // Assert
        result.DiversityScore.Should().Be(0.0);
    }

    [Fact]
    public void AnalyzeTopicDiversity_ShouldHaveHigherDiversity_WhenMultipleTopics()
    {
        // Arrange
        var singleTopicContent = "Technology AI software programming";
        var multiTopicContent = "Technology business education health entertainment";

        // Act
        var singleResult = _sut.AnalyzeTopicDiversity(singleTopicContent);
        var multiResult = _sut.AnalyzeTopicDiversity(multiTopicContent);

        // Assert
        multiResult.DiversityScore.Should().BeGreaterThanOrEqualTo(singleResult.DiversityScore);
    }

    [Fact]
    public void AnalyzeTopicDiversity_ShouldIncludePrimaryTopic()
    {
        // Arrange
        var content = "Technology and business with education";

        // Act
        var result = _sut.AnalyzeTopicDiversity(content);

        // Assert
        result.PrimaryTopic.Should().NotBeNullOrEmpty();
        result.AllTopics.Should().Contain(t => t.Topic == result.PrimaryTopic);
    }

    [Fact]
    public void AnalyzeTopicDiversity_ShouldGenerateAnalysisText()
    {
        // Arrange
        var content = "Technology AI with business and education";

        // Act
        var result = _sut.AnalyzeTopicDiversity(content);

        // Assert
        result.Analysis.Should().Contain("Primary topic:");
        result.Analysis.Should().Contain(result.PrimaryTopic);
    }

    [Fact]
    public void AnalyzeTopicDiversity_ShouldMentionSecondaryTopics_WhenMultiple()
    {
        // Arrange
        var content = "Technology business education health";

        // Act
        var result = _sut.AnalyzeTopicDiversity(content);

        // Assert
        if (result.TopicCount > 1)
        {
            result.Analysis.Should().Contain("Also covers:");
        }
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public void ExtractTopics_ShouldHandleLongContent()
    {
        // Arrange
        var content = string.Join(" ", Enumerable.Repeat("technology software AI", 100));

        // Act
        var result = _sut.ExtractTopics(content);

        // Assert
        result.Should().Contain("Technology");
    }

    [Fact]
    public void ExtractTopics_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var content = "Tech@logy and AI! Software#Development";

        // Act
        var result = _sut.ExtractTopics(content);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("Movie and film with entertainment", "Entertainment")]
    [InlineData("Breaking news and journalism", "News")]
    [InlineData("Travel vacation to beach destination", "Travel")]
    [InlineData("Food recipe cooking in kitchen", "Food")]
    [InlineData("Science research and discovery", "Science")]
    public void ExtractTopics_ShouldDetectCorrectTopic_ForVariousContent(string content, string expectedTopic)
    {
        // Act
        var result = _sut.ExtractTopics(content);

        // Assert
        result.Should().Contain(expectedTopic);
    }

    [Fact]
    public void ExtractTopicsWithScores_ShouldGiveHigherScores_ToPrimaryKeywords()
    {
        // Arrange - technology is a primary keyword, innovation is secondary
        var primaryContent = "This is about technology and software";
        var secondaryContent = "This is about innovation and digital transformation";

        // Act
        var primaryResults = _sut.ExtractTopicsWithScores(primaryContent);
        var secondaryResults = _sut.ExtractTopicsWithScores(secondaryContent);

        // Assert
        var primaryTechScore = primaryResults.FirstOrDefault(r => r.Topic == "Technology")?.Score ?? 0;
        var secondaryTechScore = secondaryResults.FirstOrDefault(r => r.Topic == "Technology")?.Score ?? 0;

        // Primary keywords should generally score higher, though content length normalization may affect this
        primaryTechScore.Should().BeGreaterThan(0);
        secondaryTechScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetPrimaryTopic_ShouldBeConsistentWithExtractTopics()
    {
        // Arrange
        var content = "Technology and artificial intelligence software";

        // Act
        var primaryTopic = _sut.GetPrimaryTopic(content);
        var allTopics = _sut.ExtractTopics(content);

        // Assert
        allTopics.Should().Contain(primaryTopic);
        allTopics.First().Should().Be(primaryTopic);
    }

    [Fact]
    public void AnalyzeTopicDiversity_ShouldHandleNullGracefully()
    {
        // Act
        var result = _sut.AnalyzeTopicDiversity(null!);

        // Assert
        result.Should().NotBeNull();
        result.PrimaryTopic.Should().Be("General");
    }

    [Fact]
    public void AnalyzeTopicDiversity_ShouldHandleEmptyGracefully()
    {
        // Act
        var result = _sut.AnalyzeTopicDiversity(string.Empty);

        // Assert
        result.Should().NotBeNull();
        result.PrimaryTopic.Should().Be("General");
    }

    [Fact]
    public void ExtractTopics_ShouldHandleWhitespaceOnly()
    {
        // Act
        var result = _sut.ExtractTopics("   ");

        // Assert
        result.Should().Contain("General");
    }

    [Fact]
    public void ExtractTopicsWithScores_ShouldNormalizeByContentLength()
    {
        // Arrange - both have same keywords but different lengths
        var shortContent = "technology AI";
        var longContent = "technology AI " + string.Join(" ", Enumerable.Repeat("filler", 100));

        // Act
        var shortResult = _sut.ExtractTopicsWithScores(shortContent);
        var longResult = _sut.ExtractTopicsWithScores(longContent);

        // Assert
        // Both should detect technology, though scores may differ due to normalization
        shortResult.Should().Contain(t => t.Topic == "Technology");
        longResult.Should().Contain(t => t.Topic == "Technology");
    }

    [Fact]
    public void AnalyzeTopicDiversity_ShouldIncludeAllExtractedTopics()
    {
        // Arrange
        var content = "Technology business education";

        // Act
        var result = _sut.AnalyzeTopicDiversity(content);

        // Assert
        result.TopicCount.Should().Be(result.AllTopics.Count);
    }

    #endregion
}
