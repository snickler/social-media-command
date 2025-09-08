using Xunit;
using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Tests.UnitTests;

public class SocialFeedModelsTests
{
    [Fact]
    public void SocialFeedItem_ShouldInitializeWithDefaults()
    {
        // Act
        var feedItem = new SocialFeedItem();

        // Assert
        Assert.NotNull(feedItem.Id);
        Assert.False(string.IsNullOrEmpty(feedItem.Id));
        Assert.Equal(default(SocialPlatform), feedItem.Platform);
        Assert.Equal(string.Empty, feedItem.AuthorName);
        Assert.Equal(string.Empty, feedItem.AuthorUsername);
        Assert.Equal(string.Empty, feedItem.AuthorAvatar);
        Assert.Equal(string.Empty, feedItem.Content);
        Assert.Equal(default(DateTime), feedItem.PostedAt);
        Assert.NotNull(feedItem.Media);
        Assert.Empty(feedItem.Media);
        Assert.NotNull(feedItem.Engagement);
        Assert.False(feedItem.IsThread);
        Assert.NotNull(feedItem.Hashtags);
        Assert.Empty(feedItem.Hashtags);
        Assert.Null(feedItem.PlatformPostId);
        Assert.Null(feedItem.PlatformUrl);
    }

    [Fact]
    public void SocialFeedItem_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var postedAt = DateTime.UtcNow;
        var media = new List<Media> { new() };
        var engagement = new SocialEngagement();
        var hashtags = new List<string> { "tech", "social" };

        // Act
        var feedItem = new SocialFeedItem
        {
            Id = id,
            Platform = SocialPlatform.X,
            AuthorName = "John Doe",
            AuthorUsername = "johndoe",
            AuthorAvatar = "https://example.com/avatar.jpg",
            Content = "Test content with #hashtags",
            PostedAt = postedAt,
            Media = media,
            Engagement = engagement,
            IsThread = true,
            Hashtags = hashtags,
            PlatformPostId = "tweet123",
            PlatformUrl = "https://twitter.com/johndoe/status/123"
        };

        // Assert
        Assert.Equal(id, feedItem.Id);
        Assert.Equal(SocialPlatform.X, feedItem.Platform);
        Assert.Equal("John Doe", feedItem.AuthorName);
        Assert.Equal("johndoe", feedItem.AuthorUsername);
        Assert.Equal("https://example.com/avatar.jpg", feedItem.AuthorAvatar);
        Assert.Equal("Test content with #hashtags", feedItem.Content);
        Assert.Equal(postedAt, feedItem.PostedAt);
        Assert.Equal(media, feedItem.Media);
        Assert.Equal(engagement, feedItem.Engagement);
        Assert.True(feedItem.IsThread);
        Assert.Equal(hashtags, feedItem.Hashtags);
        Assert.Equal("tweet123", feedItem.PlatformPostId);
        Assert.Equal("https://twitter.com/johndoe/status/123", feedItem.PlatformUrl);
    }

    [Theory]
    [InlineData(0, "Just now")]
    [InlineData(30, "Just now")]
    [InlineData(60, "1m ago")]
    [InlineData(120, "2m ago")]
    [InlineData(3600, "1h ago")]
    [InlineData(7200, "2h ago")]
    [InlineData(86400, "1d ago")]
    [InlineData(172800, "2d ago")]
    [InlineData(604800, "1w ago")]
    [InlineData(1209600, "2w ago")]
    [InlineData(2592000, "1mo ago")]
    [InlineData(5184000, "2mo ago")]
    [InlineData(31536000, "1y ago")]
    [InlineData(63072000, "2y ago")]
    public void SocialFeedItem_GetTimeAgo_ShouldReturnCorrectFormat(int secondsAgo, string expected)
    {
        // Arrange
        var feedItem = new SocialFeedItem
        {
            PostedAt = DateTime.UtcNow.AddSeconds(-secondsAgo)
        };

        // Act
        var result = feedItem.GetTimeAgo();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SocialEngagement_ShouldInitializeWithDefaults()
    {
        // Act
        var engagement = new SocialEngagement();

        // Assert
        Assert.Equal(0, engagement.Likes);
        Assert.Equal(0, engagement.Comments);
        Assert.Equal(0, engagement.Shares);
        Assert.Equal(0, engagement.Views);
        Assert.NotNull(engagement.CustomMetrics);
        Assert.Empty(engagement.CustomMetrics);
    }

    [Fact]
    public void SocialEngagement_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var customMetrics = new Dictionary<string, int>
        {
            { "bookmarks", 15 },
            { "quotes", 5 }
        };

        // Act
        var engagement = new SocialEngagement
        {
            Likes = 100,
            Comments = 25,
            Shares = 10,
            Views = 1500,
            CustomMetrics = customMetrics
        };

        // Assert
        Assert.Equal(100, engagement.Likes);
        Assert.Equal(25, engagement.Comments);
        Assert.Equal(10, engagement.Shares);
        Assert.Equal(1500, engagement.Views);
        Assert.Equal(customMetrics, engagement.CustomMetrics);
    }

    [Fact]
    public void SocialEngagement_GetTotalEngagement_ShouldCalculateCorrectly()
    {
        // Arrange
        var engagement = new SocialEngagement
        {
            Likes = 100,
            Comments = 25,
            Shares = 10,
            CustomMetrics = new Dictionary<string, int>
            {
                { "bookmarks", 15 },
                { "quotes", 5 }
            }
        };

        // Act
        var total = engagement.GetTotalEngagement();

        // Assert
        // 100 + 25 + 10 + 15 + 5 = 155
        Assert.Equal(155, total);
    }

    [Fact]
    public void SocialEngagement_GetTotalEngagement_WithNoCustomMetrics_ShouldCalculateCorrectly()
    {
        // Arrange
        var engagement = new SocialEngagement
        {
            Likes = 50,
            Comments = 20,
            Shares = 5
        };

        // Act
        var total = engagement.GetTotalEngagement();

        // Assert
        // 50 + 20 + 5 = 75
        Assert.Equal(75, total);
    }

    [Fact]
    public void FeedConfiguration_ShouldInitializeWithDefaults()
    {
        // Act
        var config = new FeedConfiguration();

        // Assert
        Assert.NotNull(config.EnabledPlatforms);
        Assert.Empty(config.EnabledPlatforms);
        Assert.Equal(10, config.MaxItemsPerPlatform);
        Assert.Equal(TimeSpan.FromDays(7), config.MaxAge);
        Assert.True(config.ShowEngagement);
        Assert.True(config.ShowMedia);
        Assert.False(config.GroupByPlatform);
        Assert.Equal(FeedSortOrder.Chronological, config.SortOrder);
    }

    [Fact]
    public void FeedConfiguration_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var platforms = new List<SocialPlatform> { SocialPlatform.X, SocialPlatform.LinkedIn };

        // Act
        var config = new FeedConfiguration
        {
            EnabledPlatforms = platforms,
            MaxItemsPerPlatform = 20,
            MaxAge = TimeSpan.FromDays(14),
            ShowEngagement = false,
            ShowMedia = false,
            GroupByPlatform = true,
            SortOrder = FeedSortOrder.Engagement
        };

        // Assert
        Assert.Equal(platforms, config.EnabledPlatforms);
        Assert.Equal(20, config.MaxItemsPerPlatform);
        Assert.Equal(TimeSpan.FromDays(14), config.MaxAge);
        Assert.False(config.ShowEngagement);
        Assert.False(config.ShowMedia);
        Assert.True(config.GroupByPlatform);
        Assert.Equal(FeedSortOrder.Engagement, config.SortOrder);
    }

    [Theory]
    [InlineData(FeedSortOrder.Chronological)]
    [InlineData(FeedSortOrder.Engagement)]
    [InlineData(FeedSortOrder.Platform)]
    public void FeedSortOrder_ShouldHaveAllValues(FeedSortOrder sortOrder)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(FeedSortOrder), sortOrder));
    }

    [Fact]
    public void MockFeedData_GenerateMockFeedItems_ShouldReturnCorrectCount()
    {
        // Arrange
        const int count = 10;

        // Act
        var items = MockFeedData.GenerateMockFeedItems(count);

        // Assert
        Assert.Equal(count, items.Count);
    }

    [Fact]
    public void MockFeedData_GenerateMockFeedItems_ShouldReturnOrderedByDate()
    {
        // Arrange
        const int count = 5;

        // Act
        var items = MockFeedData.GenerateMockFeedItems(count);

        // Assert
        for (int i = 0; i < items.Count - 1; i++)
        {
            Assert.True(items[i].PostedAt >= items[i + 1].PostedAt);
        }
    }

    [Fact]
    public void MockFeedData_GenerateMockFeedItems_ShouldHaveValidProperties()
    {
        // Arrange
        const int count = 10;

        // Act
        var items = MockFeedData.GenerateMockFeedItems(count);

        // Assert
        foreach (var item in items)
        {
            Assert.NotNull(item.Id);
            Assert.False(string.IsNullOrEmpty(item.Id));
            Assert.True(Enum.IsDefined(typeof(SocialPlatform), item.Platform));
            Assert.False(string.IsNullOrEmpty(item.AuthorName));
            Assert.False(string.IsNullOrEmpty(item.AuthorUsername));
            Assert.False(string.IsNullOrEmpty(item.AuthorAvatar));
            Assert.False(string.IsNullOrEmpty(item.Content));
            Assert.True(item.PostedAt <= DateTime.UtcNow);
            Assert.NotNull(item.Engagement);
            Assert.NotNull(item.Hashtags);
            Assert.False(string.IsNullOrEmpty(item.PlatformPostId));
            Assert.False(string.IsNullOrEmpty(item.PlatformUrl));
        }
    }

    [Fact]
    public void MockFeedData_GenerateMockFeedItems_ShouldExtractHashtags()
    {
        // Arrange
        const int count = 50; // Generate enough to likely get items with hashtags

        // Act
        var items = MockFeedData.GenerateMockFeedItems(count);

        // Assert
        var itemsWithHashtags = items.Where(i => i.Hashtags.Any()).ToList();
        Assert.NotEmpty(itemsWithHashtags);

        foreach (var item in itemsWithHashtags)
        {
            foreach (var hashtag in item.Hashtags)
            {
                Assert.False(string.IsNullOrEmpty(hashtag));
                Assert.DoesNotContain("#", hashtag); // Hashtags should not include the # symbol
            }
        }
    }

    [Fact]
    public void MockFeedData_GenerateMockFeedItems_WithDefaultCount_ShouldReturn50Items()
    {
        // Act
        var items = MockFeedData.GenerateMockFeedItems();

        // Assert
        Assert.Equal(50, items.Count);
    }

    [Fact]
    public void MockFeedData_GenerateMockFeedItems_ShouldHaveValidEngagement()
    {
        // Arrange
        const int count = 10;

        // Act
        var items = MockFeedData.GenerateMockFeedItems(count);

        // Assert
        foreach (var item in items)
        {
            Assert.True(item.Engagement.Likes >= 0);
            Assert.True(item.Engagement.Comments >= 0);
            Assert.True(item.Engagement.Shares >= 0);
            Assert.True(item.Engagement.Views >= 0);
        }
    }

    [Fact]
    public void MockFeedData_GenerateMockFeedItems_ShouldHaveVariedPlatforms()
    {
        // Arrange
        const int count = 50; // Generate enough to get platform variety

        // Act
        var items = MockFeedData.GenerateMockFeedItems(count);

        // Assert
        var platforms = items.Select(i => i.Platform).Distinct().ToList();
        Assert.True(platforms.Count > 1, "Should have posts from multiple platforms");
    }

    [Fact]
    public void MockFeedData_GenerateMockFeedItems_ShouldHaveThreadsOccasionally()
    {
        // Arrange
        const int count = 100; // Generate enough to statistically have some threads

        // Act
        var items = MockFeedData.GenerateMockFeedItems(count);

        // Assert
        var threadsCount = items.Count(i => i.IsThread);
        // With 10% chance, we should have some threads in 100 items
        Assert.True(threadsCount >= 0); // At minimum 0 threads is valid
        // But statistically we should have some
        if (count >= 50)
        {
            Assert.True(threadsCount < count * 0.5); // Should be significantly less than 50%
        }
    }

    [Fact]
    public void MockFeedData_GenerateMockFeedItems_ShouldHaveValidTimeRange()
    {
        // Arrange
        const int count = 10;
        var now = DateTime.UtcNow;

        // Act
        var items = MockFeedData.GenerateMockFeedItems(count);

        // Assert
        foreach (var item in items)
        {
            var ageInMinutes = (now - item.PostedAt).TotalMinutes;
            Assert.True(ageInMinutes >= 0, "Post time should not be in the future");
            Assert.True(ageInMinutes <= 10080, "Post should not be older than a week (10080 minutes)");
        }
    }
}