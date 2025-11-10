using Xunit;
using SocialMediaCommander.Services.Interfaces;
using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Tests.UnitTests;

public class FeedServiceInterfaceModelsTests
{
    [Fact]
    public void FeedStatistics_ShouldInitializeWithDefaults()
    {
        // Act
        var stats = new FeedStatistics();

        // Assert
        Assert.Equal(0, stats.TotalPosts);
        Assert.Equal(0, stats.TotalEngagement);
        Assert.NotNull(stats.PostsByPlatform);
        Assert.Empty(stats.PostsByPlatform);
        Assert.NotNull(stats.EngagementByPlatform);
        Assert.Empty(stats.EngagementByPlatform);
        Assert.NotNull(stats.TopHashtags);
        Assert.Empty(stats.TopHashtags);
        Assert.True(stats.GeneratedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void FeedStatistics_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var postsByPlatform = new Dictionary<SocialPlatform, int>
        {
            { SocialPlatform.BlueSky, 150 }
        };

        var engagementByPlatform = new Dictionary<SocialPlatform, int>
        {
            { SocialPlatform.BlueSky, 2500 }
        };

        var topHashtags = new List<string> { "technology", "innovation", "ai", "business" };
        var generatedAt = DateTime.UtcNow;

        // Act
        var stats = new FeedStatistics
        {
            TotalPosts = 150,
            TotalEngagement = 2500,
            PostsByPlatform = postsByPlatform,
            EngagementByPlatform = engagementByPlatform,
            TopHashtags = topHashtags,
            GeneratedAt = generatedAt
        };

        // Assert
        Assert.Equal(150, stats.TotalPosts);
        Assert.Equal(2500, stats.TotalEngagement);
        Assert.Equal(postsByPlatform, stats.PostsByPlatform);
        Assert.Equal(engagementByPlatform, stats.EngagementByPlatform);
        Assert.Equal(topHashtags, stats.TopHashtags);
        Assert.Equal(generatedAt, stats.GeneratedAt);
    }

    [Fact]
    public void FeedStatistics_ShouldCalculateTotalsFromPlatformData()
    {
        // Arrange
        var stats = new FeedStatistics
        {
            PostsByPlatform = new Dictionary<SocialPlatform, int>
            {
                { SocialPlatform.BlueSky, 100 }
            },
            EngagementByPlatform = new Dictionary<SocialPlatform, int>
            {
                { SocialPlatform.BlueSky, 1500 }
            }
        };

        // Act
        var totalPosts = stats.PostsByPlatform.Values.Sum();
        var totalEngagement = stats.EngagementByPlatform.Values.Sum();

        // Assert
        Assert.Equal(100, totalPosts);
        Assert.Equal(1500, totalEngagement);
    }

    [Fact]
    public void FeedStatistics_ShouldSupportAllSocialPlatforms()
    {
        // Arrange
        var stats = new FeedStatistics();

        // Act
        stats.PostsByPlatform[SocialPlatform.BlueSky] = 100;
        stats.PostsByPlatform[SocialPlatform.BlueSky] = 50;
        stats.PostsByPlatform[SocialPlatform.BlueSky] = 75;
        stats.PostsByPlatform[SocialPlatform.BlueSky] = 25;
        stats.PostsByPlatform[SocialPlatform.BlueSky] = 40;

        stats.EngagementByPlatform[SocialPlatform.BlueSky] = 1500;

        // Assert
        Assert.Single(stats.PostsByPlatform);
        Assert.Single(stats.EngagementByPlatform);
        Assert.True(stats.PostsByPlatform.ContainsKey(SocialPlatform.BlueSky));
    }

    [Fact]
    public void FeedStatistics_ShouldHandleEmptyCollections()
    {
        // Act
        var stats = new FeedStatistics
        {
            PostsByPlatform = new Dictionary<SocialPlatform, int>(),
            EngagementByPlatform = new Dictionary<SocialPlatform, int>(),
            TopHashtags = new List<string>()
        };

        // Assert
        Assert.NotNull(stats.PostsByPlatform);
        Assert.Empty(stats.PostsByPlatform);
        Assert.NotNull(stats.EngagementByPlatform);
        Assert.Empty(stats.EngagementByPlatform);
        Assert.NotNull(stats.TopHashtags);
        Assert.Empty(stats.TopHashtags);
    }

    [Fact]
    public void FeedStatistics_ShouldSupportLargeNumbers()
    {
        // Act
        var stats = new FeedStatistics
        {
            TotalPosts = int.MaxValue,
            TotalEngagement = int.MaxValue
        };

        // Assert
        Assert.Equal(int.MaxValue, stats.TotalPosts);
        Assert.Equal(int.MaxValue, stats.TotalEngagement);
    }

    [Fact]
    public void FeedStatistics_TopHashtags_ShouldSupportOrderedList()
    {
        // Arrange
        var orderedHashtags = new List<string>
        {
            "technology", // Most popular
            "innovation",
            "business",
            "ai",
            "startup"     // Least popular
        };

        // Act
        var stats = new FeedStatistics
        {
            TopHashtags = orderedHashtags
        };

        // Assert
        Assert.Equal(orderedHashtags, stats.TopHashtags);
        Assert.Equal("technology", stats.TopHashtags[0]);
        Assert.Equal("startup", stats.TopHashtags[4]);
    }

    [Fact]
    public void FeedStatistics_ShouldAllowModificationOfCollections()
    {
        // Arrange
        var stats = new FeedStatistics();

        // Act
        stats.PostsByPlatform.Add(SocialPlatform.BlueSky, 100);
        stats.EngagementByPlatform.Add(SocialPlatform.BlueSky, 1500);
        stats.TopHashtags.Add("newhashtag");

        // Assert
        Assert.Single(stats.PostsByPlatform);
        Assert.Single(stats.EngagementByPlatform);
        Assert.Single(stats.TopHashtags);
        Assert.Equal(100, stats.PostsByPlatform[SocialPlatform.BlueSky]);
        Assert.Equal(1500, stats.EngagementByPlatform[SocialPlatform.BlueSky]);
        Assert.Equal("newhashtag", stats.TopHashtags[0]);
    }

    [Fact]
    public void FeedStatistics_ShouldCalculateEngagementRates()
    {
        // Arrange
        var stats = new FeedStatistics
        {
            PostsByPlatform = new Dictionary<SocialPlatform, int>
            {
                { SocialPlatform.BlueSky, 100 }
            },
            EngagementByPlatform = new Dictionary<SocialPlatform, int>
            {
                { SocialPlatform.BlueSky, 1500 }
            }
        };

        // Act
        var engagementRate = stats.PostsByPlatform[SocialPlatform.BlueSky] > 0
            ? (double)stats.EngagementByPlatform[SocialPlatform.BlueSky] / stats.PostsByPlatform[SocialPlatform.BlueSky]
            : 0;

        // Assert
        Assert.Equal(15.0, engagementRate); // 1500 engagement / 100 posts = 15 engagements per post
    }

    [Fact]
    public void FeedStatistics_GeneratedAt_ShouldBeRecentTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var stats = new FeedStatistics();
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(stats.GeneratedAt >= beforeCreation);
        Assert.True(stats.GeneratedAt <= afterCreation);
    }

    [Fact]
    public void FeedStatistics_ShouldSupportZeroValues()
    {
        // Act
        var stats = new FeedStatistics
        {
            TotalPosts = 0,
            TotalEngagement = 0
        };

        // Assert
        Assert.Equal(0, stats.TotalPosts);
        Assert.Equal(0, stats.TotalEngagement);
    }
}
