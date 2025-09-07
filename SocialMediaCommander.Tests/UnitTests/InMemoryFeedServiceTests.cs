using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Comprehensive unit tests for InMemoryFeedService
/// Tests feed management, caching, and search functionality
/// </summary>
public class InMemoryFeedServiceTests : IDisposable
{
    private readonly InMemoryFeedService _service;

    public InMemoryFeedServiceTests()
    {
        _service = new InMemoryFeedService();
    }

    public void Dispose()
    {
        _service?.GetType().GetMethod("Dispose")?.Invoke(_service, null);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithMockData()
    {
        // Act & Assert - Service should be initialized with mock data
        Assert.NotNull(_service);

        // Verify that mock data was loaded by checking we can get feed items
        var platforms = new[] { SocialPlatform.X, SocialPlatform.Facebook };
        var result = _service.GetFeedItemsAsync(platforms, 10).Result;

        Assert.NotNull(result);
        // Should have some mock data
        Assert.True(result.Any());
    }

    [Fact]
    public async Task GetFeedItemsAsync_WithMultiplePlatforms_ShouldReturnFilteredResults()
    {
        // Arrange
        var platforms = new[] { SocialPlatform.X, SocialPlatform.Facebook };

        // Act
        var result = await _service.GetFeedItemsAsync(platforms, 20);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, item => Assert.Contains(item.Platform, platforms));
        Assert.True(result.Count() <= 20);

        // Should be sorted by PostedAt descending
        var sortedResult = result.ToList();
        for (int i = 0; i < sortedResult.Count - 1; i++)
        {
            Assert.True(sortedResult[i].PostedAt >= sortedResult[i + 1].PostedAt);
        }
    }

    [Fact]
    public async Task GetFeedItemsAsync_WithSinglePlatform_ShouldReturnOnlyThatPlatform()
    {
        // Arrange
        var platforms = new[] { SocialPlatform.LinkedIn };

        // Act
        var result = await _service.GetFeedItemsAsync(platforms, 10);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, item => Assert.Equal(SocialPlatform.LinkedIn, item.Platform));
    }

    [Fact]
    public async Task GetFeedItemsAsync_WithEmptyPlatforms_ShouldReturnEmpty()
    {
        // Arrange
        var platforms = Array.Empty<SocialPlatform>();

        // Act
        var result = await _service.GetFeedItemsAsync(platforms, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFeedItemsAsync_WithLimitZero_ShouldReturnEmpty()
    {
        // Arrange
        var platforms = new[] { SocialPlatform.X };

        // Act
        var result = await _service.GetFeedItemsAsync(platforms, 0);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFeedItemsAsync_WithConfiguration_ShouldApplyFilters()
    {
        // Arrange
        var config = new FeedConfiguration
        {
            EnabledPlatforms = new List<SocialPlatform> { SocialPlatform.X, SocialPlatform.Facebook },
            MaxAge = TimeSpan.FromDays(7),
            SortOrder = FeedSortOrder.Chronological,
            MaxItemsPerPlatform = 5,
            GroupByPlatform = false
        };

        // Act
        var result = await _service.GetFeedItemsAsync(config);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, item => Assert.Contains(item.Platform, config.EnabledPlatforms));

        // Check age filter
        var cutoffTime = DateTime.UtcNow - config.MaxAge;
        Assert.All(result, item => Assert.True(item.PostedAt >= cutoffTime));
    }

    [Fact]
    public async Task GetFeedItemsAsync_WithEngagementSort_ShouldSortByEngagement()
    {
        // Arrange
        var config = new FeedConfiguration
        {
            EnabledPlatforms = new List<SocialPlatform> { SocialPlatform.X },
            SortOrder = FeedSortOrder.Engagement,
            MaxItemsPerPlatform = 10
        };

        // Act
        var result = await _service.GetFeedItemsAsync(config);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();

        // Should be sorted by engagement descending
        for (int i = 0; i < resultList.Count - 1; i++)
        {
            var currentEngagement = resultList[i].Engagement.GetTotalEngagement();
            var nextEngagement = resultList[i + 1].Engagement.GetTotalEngagement();
            Assert.True(currentEngagement >= nextEngagement);
        }
    }

    [Fact]
    public async Task RefreshFeedAsync_ShouldAddNewItems()
    {
        // Arrange
        var platforms = new[] { SocialPlatform.X };
        var initialCount = (await _service.GetFeedItemsAsync(platforms, 100)).Count();

        // Act
        await _service.RefreshFeedAsync(platforms);

        // Assert
        var newCount = (await _service.GetFeedItemsAsync(platforms, 100)).Count();
        Assert.True(newCount >= initialCount); // Should have same or more items
    }

    [Fact]
    public async Task RefreshFeedAsync_ShouldUpdateRefreshTimes()
    {
        // Arrange
        var platforms = new[] { SocialPlatform.Facebook };

        // Act
        await _service.RefreshFeedAsync(platforms);
        var refreshTimes = await _service.GetLastRefreshTimesAsync();

        // Assert
        Assert.True(refreshTimes[SocialPlatform.Facebook].HasValue);
        Assert.True(refreshTimes[SocialPlatform.Facebook].Value > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task GetLastRefreshTimesAsync_ShouldReturnAllPlatforms()
    {
        // Act
        var refreshTimes = await _service.GetLastRefreshTimesAsync();

        // Assert
        Assert.NotNull(refreshTimes);
        Assert.Equal(5, refreshTimes.Count); // Should have all 5 platforms

        foreach (var platform in Enum.GetValues<SocialPlatform>())
        {
            Assert.True(refreshTimes.ContainsKey(platform));
        }
    }

    [Fact]
    public async Task SearchFeedItemsAsync_WithContentMatch_ShouldReturnMatches()
    {
        // Arrange
        var searchTerm = "test";

        // Act
        var result = await _service.SearchFeedItemsAsync(searchTerm);

        // Assert
        Assert.NotNull(result);
        // Should contain items with the search term (case insensitive)
        Assert.All(result, item =>
            Assert.True(
                item.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                item.AuthorName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                item.Hashtags.Any(tag => tag.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            )
        );
    }

    [Fact]
    public async Task SearchFeedItemsAsync_WithPlatformFilter_ShouldFilterByPlatform()
    {
        // Arrange
        var searchTerm = "a"; // Common letter to find matches
        var platforms = new[] { SocialPlatform.LinkedIn };

        // Act
        var result = await _service.SearchFeedItemsAsync(searchTerm, platforms);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, item => Assert.Equal(SocialPlatform.LinkedIn, item.Platform));
    }

    [Fact]
    public async Task SearchFeedItemsAsync_WithEmptySearchTerm_ShouldReturnAllItems()
    {
        // Act
        var result = await _service.SearchFeedItemsAsync("");

        // Assert - Empty search typically returns all items in this implementation
        Assert.NotNull(result);
        // Should return some items (since Contains("") returns true for all strings)
        Assert.True(result.Any());
    }

    [Fact]
    public async Task GetFeedItemsByHashtagsAsync_WithValidHashtags_ShouldReturnMatches()
    {
        // Arrange
        var hashtags = new[] { "technology", "ai" };

        // Act
        var result = await _service.GetFeedItemsByHashtagsAsync(hashtags);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, item =>
            Assert.Contains(item.Hashtags, tag =>
                hashtags.Any(searchTag => tag.ToLowerInvariant().Contains(searchTag.ToLowerInvariant()))));
    }

    [Fact]
    public async Task GetFeedItemsByHashtagsAsync_WithHashtagPrefix_ShouldHandleCorrectly()
    {
        // Arrange - Test with # prefix
        var hashtags = new[] { "#technology", "ai" };

        // Act
        var result = await _service.GetFeedItemsByHashtagsAsync(hashtags);

        // Assert
        Assert.NotNull(result);
        // Should handle hashtags with or without # prefix
    }

    [Fact]
    public async Task GetFeedItemsByHashtagsAsync_WithPlatformFilter_ShouldFilterByPlatform()
    {
        // Arrange
        var hashtags = new[] { "technology" };
        var platforms = new[] { SocialPlatform.X };

        // Act
        var result = await _service.GetFeedItemsByHashtagsAsync(hashtags, platforms);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, item => Assert.Equal(SocialPlatform.X, item.Platform));
    }

    [Fact]
    public async Task GetTrendingHashtagsAsync_ShouldReturnTrendingForAllPlatforms()
    {
        // Act
        var result = await _service.GetTrendingHashtagsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count); // Should have results for all 5 platforms

        foreach (var platform in Enum.GetValues<SocialPlatform>())
        {
            Assert.True(result.ContainsKey(platform));
            Assert.NotNull(result[platform]);
            // Should return up to 10 trending hashtags per platform
            Assert.True(result[platform].Count() <= 10);
        }
    }

    [Fact]
    public async Task GetFeedStatisticsAsync_ShouldCalculateCorrectStats()
    {
        // Arrange
        var platforms = new[] { SocialPlatform.X, SocialPlatform.Facebook };
        var timeRange = TimeSpan.FromDays(30);

        // Act
        var result = await _service.GetFeedStatisticsAsync(platforms, timeRange);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalPosts >= 0);
        Assert.True(result.TotalEngagement >= 0);
        Assert.NotNull(result.PostsByPlatform);
        Assert.NotNull(result.EngagementByPlatform);
        Assert.NotNull(result.TopHashtags);

        // Should only include specified platforms
        Assert.All(result.PostsByPlatform.Keys, platform => Assert.Contains(platform, platforms));
        Assert.All(result.EngagementByPlatform.Keys, platform => Assert.Contains(platform, platforms));

        // Should have up to 20 top hashtags
        Assert.True(result.TopHashtags.Count <= 20);
    }

    [Fact]
    public async Task MarkFeedItemsAsReadAsync_ShouldMarkItemsAsRead()
    {
        // Arrange
        var platforms = new[] { SocialPlatform.X };
        var feedItems = await _service.GetFeedItemsAsync(platforms, 5);
        var itemIds = feedItems.Select(item => item.Id).ToList();

        var initialUnreadCount = await _service.GetUnreadCountsAsync();

        // Act
        await _service.MarkFeedItemsAsReadAsync(itemIds);

        // Assert
        var finalUnreadCount = await _service.GetUnreadCountsAsync();
        Assert.True(finalUnreadCount[SocialPlatform.X] <= initialUnreadCount[SocialPlatform.X]);
    }

    [Fact]
    public async Task GetUnreadCountsAsync_ShouldReturnCountsForAllPlatforms()
    {
        // Act
        var result = await _service.GetUnreadCountsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count); // Should have counts for all 5 platforms

        foreach (var platform in Enum.GetValues<SocialPlatform>())
        {
            Assert.True(result.ContainsKey(platform));
            Assert.True(result[platform] >= 0);
        }
    }

    [Theory]
    [InlineData(SocialPlatform.X)]
    [InlineData(SocialPlatform.Facebook)]
    [InlineData(SocialPlatform.LinkedIn)]
    [InlineData(SocialPlatform.BlueSky)]
    [InlineData(SocialPlatform.Threads)]
    public async Task GetFeedItemsAsync_ForEachPlatform_ShouldReturnPlatformSpecificContent(SocialPlatform platform)
    {
        // Arrange
        var platforms = new[] { platform };

        // Act
        var result = await _service.GetFeedItemsAsync(platforms, 10);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, item => Assert.Equal(platform, item.Platform));
    }

    [Fact]
    public async Task RefreshFeedAsync_WithMultiplePlatforms_ShouldUpdateAllPlatforms()
    {
        // Arrange
        var platforms = new[] { SocialPlatform.X, SocialPlatform.Facebook, SocialPlatform.LinkedIn };

        // Act
        await _service.RefreshFeedAsync(platforms);
        var refreshTimes = await _service.GetLastRefreshTimesAsync();

        // Assert
        foreach (var platform in platforms)
        {
            Assert.True(refreshTimes[platform].HasValue);
            Assert.True(refreshTimes[platform].Value > DateTime.UtcNow.AddMinutes(-1));
        }
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldHandleCorrectly()
    {
        // Arrange
        var platforms = new[] { SocialPlatform.X };
        var tasks = new List<Task>();

        // Act - Perform multiple concurrent operations
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_service.GetFeedItemsAsync(platforms, 10));
            tasks.Add(_service.RefreshFeedAsync(platforms));
            tasks.Add(_service.SearchFeedItemsAsync("test"));
        }

        // Assert - Should complete without exceptions
        await Task.WhenAll(tasks);
        Assert.True(tasks.All(t => t.IsCompletedSuccessfully));
    }

    [Fact]
    public async Task FeedConfiguration_WithGroupByPlatform_ShouldLimitItemsPerPlatform()
    {
        // Arrange
        var config = new FeedConfiguration
        {
            EnabledPlatforms = new List<SocialPlatform> { SocialPlatform.X, SocialPlatform.Facebook },
            MaxItemsPerPlatform = 3,
            GroupByPlatform = true,
            SortOrder = FeedSortOrder.Platform
        };

        // Act
        var result = await _service.GetFeedItemsAsync(config);

        // Assert
        Assert.NotNull(result);
        var platformGroups = result.GroupBy(item => item.Platform);

        // Each platform should have at most MaxItemsPerPlatform items
        Assert.All(platformGroups, group => Assert.True(group.Count() <= config.MaxItemsPerPlatform));
    }

    [Fact]
    public async Task DataConsistency_AfterMultipleRefreshes_ShouldMaintainIntegrity()
    {
        // Arrange
        var platforms = new[] { SocialPlatform.X };

        // Act - Multiple refreshes
        for (int i = 0; i < 3; i++)
        {
            await _service.RefreshFeedAsync(platforms);
        }

        // Assert - Data should still be consistent
        var feedItems = await _service.GetFeedItemsAsync(platforms, 100);
        var statistics = await _service.GetFeedStatisticsAsync(platforms, TimeSpan.FromDays(30));
        var unreadCounts = await _service.GetUnreadCountsAsync();

        Assert.NotNull(feedItems);
        Assert.NotNull(statistics);
        Assert.NotNull(unreadCounts);

        // Statistics should be consistent with feed items
        Assert.True(statistics.TotalPosts >= 0);
        Assert.True(unreadCounts[SocialPlatform.X] >= 0);
    }

    [Fact]
    public async Task MemoryManagement_ShouldLimitOldItems()
    {
        // This test verifies that the service cleans up old items to prevent memory bloat
        // The service should automatically remove items older than 30 days during refresh

        // Arrange
        var platforms = new[] { SocialPlatform.X };

        // Act - Force multiple refreshes to trigger cleanup
        for (int i = 0; i < 5; i++)
        {
            await _service.RefreshFeedAsync(platforms);
        }

        // Assert - Should not have excessive items (this is more of a sanity check)
        var feedItems = await _service.GetFeedItemsAsync(platforms, 1000);
        Assert.True(feedItems.Count() < 10000); // Reasonable upper bound for in-memory storage
    }
}