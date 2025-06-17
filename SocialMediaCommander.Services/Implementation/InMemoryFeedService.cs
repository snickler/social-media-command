using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;
using System.Collections.Concurrent;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// In-memory implementation of IFeedService using mock data
/// </summary>
public class InMemoryFeedService : IFeedService
{
    private readonly ConcurrentDictionary<string, SocialFeedItem> _feedItems = new();
    private readonly ConcurrentDictionary<SocialPlatform, DateTime> _lastRefreshTimes = new();
    private readonly HashSet<string> _readItems = new();
    private readonly object _refreshLock = new();

    public InMemoryFeedService()
    {
        // Initialize with mock feed data
        InitializeMockData();
    }

    public Task<IEnumerable<SocialFeedItem>> GetFeedItemsAsync(IEnumerable<SocialPlatform> platforms, int limit = 50)
    {
        var items = _feedItems.Values
            .Where(item => platforms.Contains(item.Platform))
            .OrderByDescending(item => item.PostedAt)
            .Take(limit);

        return Task.FromResult(items);
    }

    public Task<IEnumerable<SocialFeedItem>> GetFeedItemsAsync(FeedConfiguration configuration)
    {
        var query = _feedItems.Values.AsEnumerable();

        // Filter by platforms
        if (configuration.EnabledPlatforms.Any())
        {
            query = query.Where(item => configuration.EnabledPlatforms.Contains(item.Platform));
        }

        // Filter by age
        var cutoffTime = DateTime.UtcNow - configuration.MaxAge;
        query = query.Where(item => item.PostedAt >= cutoffTime);

        // Apply sorting
        query = configuration.SortOrder switch
        {
            FeedSortOrder.Chronological => query.OrderByDescending(item => item.PostedAt),
            FeedSortOrder.Engagement => query.OrderByDescending(item => item.Engagement.GetTotalEngagement()),
            FeedSortOrder.Platform => query.OrderBy(item => item.Platform).ThenByDescending(item => item.PostedAt),
            _ => query.OrderByDescending(item => item.PostedAt)
        };

        // Group by platform if requested
        if (configuration.GroupByPlatform)
        {
            var grouped = query.GroupBy(item => item.Platform)
                .SelectMany(group => group.Take(configuration.MaxItemsPerPlatform));
            query = grouped;
        }

        var result = query.Take(configuration.MaxItemsPerPlatform * configuration.EnabledPlatforms.Count);
        return Task.FromResult(result.AsEnumerable());
    }

    public Task RefreshFeedAsync(IEnumerable<SocialPlatform> platforms)
    {
        lock (_refreshLock)
        {
            // Simulate refresh by generating new mock data
            var newItems = MockFeedData.GenerateMockFeedItems(20);
            var platformList = platforms.ToList();

            foreach (var item in newItems.Where(item => platformList.Contains(item.Platform)))
            {
                _feedItems.TryAdd(item.Id, item);
            }

            // Update refresh times
            foreach (var platform in platformList)
            {
                _lastRefreshTimes.AddOrUpdate(platform, DateTime.UtcNow, (key, value) => DateTime.UtcNow);
            }

            // Keep only recent items to prevent memory bloat
            var cutoffTime = DateTime.UtcNow.AddDays(-30);
            var oldItems = _feedItems.Values.Where(item => item.PostedAt < cutoffTime).ToList();
            foreach (var oldItem in oldItems)
            {
                _feedItems.TryRemove(oldItem.Id, out _);
            }
        }

        return Task.CompletedTask;
    }

    public Task<Dictionary<SocialPlatform, DateTime?>> GetLastRefreshTimesAsync()
    {
        var result = Enum.GetValues<SocialPlatform>()
            .ToDictionary(
                platform => platform,
                platform => _lastRefreshTimes.TryGetValue(platform, out var time) ? (DateTime?)time : null
            );

        return Task.FromResult(result);
    }

    public Task<IEnumerable<SocialFeedItem>> SearchFeedItemsAsync(string searchTerm, IEnumerable<SocialPlatform>? platforms = null)
    {
        var query = _feedItems.Values.AsEnumerable();

        if (platforms != null && platforms.Any())
        {
            query = query.Where(item => platforms.Contains(item.Platform));
        }

        var searchResults = query.Where(item =>
            item.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            item.AuthorName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            item.Hashtags.Any(tag => tag.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        ).OrderByDescending(item => item.PostedAt);

        return Task.FromResult(searchResults.AsEnumerable());
    }

    public Task<IEnumerable<SocialFeedItem>> GetFeedItemsByHashtagsAsync(IEnumerable<string> hashtags, IEnumerable<SocialPlatform>? platforms = null)
    {
        var hashtagsList = hashtags.Select(tag => tag.TrimStart('#').ToLowerInvariant()).ToList();
        var query = _feedItems.Values.AsEnumerable();

        if (platforms != null && platforms.Any())
        {
            query = query.Where(item => platforms.Contains(item.Platform));
        }

        var results = query.Where(item =>
            item.Hashtags.Any(tag => hashtagsList.Contains(tag.ToLowerInvariant()))
        ).OrderByDescending(item => item.PostedAt);

        return Task.FromResult(results.AsEnumerable());
    }

    public Task<Dictionary<SocialPlatform, IEnumerable<string>>> GetTrendingHashtagsAsync()
    {
        var result = new Dictionary<SocialPlatform, IEnumerable<string>>();
        var recentCutoff = DateTime.UtcNow.AddHours(-24);

        foreach (var platform in Enum.GetValues<SocialPlatform>())
        {
            var platformItems = _feedItems.Values
                .Where(item => item.Platform == platform && item.PostedAt >= recentCutoff);

            var trendingHashtags = platformItems
                .SelectMany(item => item.Hashtags)
                .GroupBy(tag => tag.ToLowerInvariant())
                .OrderByDescending(group => group.Count())
                .Take(10)
                .Select(group => group.Key);

            result[platform] = trendingHashtags;
        }

        return Task.FromResult(result);
    }

    public Task<FeedStatistics> GetFeedStatisticsAsync(IEnumerable<SocialPlatform> platforms, TimeSpan timeRange)
    {
        var cutoffTime = DateTime.UtcNow - timeRange;
        var platformList = platforms.ToList();

        var relevantItems = _feedItems.Values
            .Where(item => platformList.Contains(item.Platform) && item.PostedAt >= cutoffTime);

        var stats = new FeedStatistics
        {
            TotalPosts = relevantItems.Count(),
            TotalEngagement = relevantItems.Sum(item => item.Engagement.GetTotalEngagement()),
            PostsByPlatform = relevantItems.GroupBy(item => item.Platform)
                .ToDictionary(group => group.Key, group => group.Count()),
            EngagementByPlatform = relevantItems.GroupBy(item => item.Platform)
                .ToDictionary(group => group.Key, group => group.Sum(item => item.Engagement.GetTotalEngagement())),
            TopHashtags = relevantItems
                .SelectMany(item => item.Hashtags)
                .GroupBy(tag => tag.ToLowerInvariant())
                .OrderByDescending(group => group.Count())
                .Take(20)
                .Select(group => group.Key)
                .ToList()
        };

        return Task.FromResult(stats);
    }

    public Task MarkFeedItemsAsReadAsync(IEnumerable<string> itemIds)
    {
        foreach (var itemId in itemIds)
        {
            _readItems.Add(itemId);
        }
        return Task.CompletedTask;
    }

    public Task<Dictionary<SocialPlatform, int>> GetUnreadCountsAsync()
    {
        var result = new Dictionary<SocialPlatform, int>();

        foreach (var platform in Enum.GetValues<SocialPlatform>())
        {
            var unreadCount = _feedItems.Values
                .Where(item => item.Platform == platform && !_readItems.Contains(item.Id))
                .Count();

            result[platform] = unreadCount;
        }

        return Task.FromResult(result);
    }

    private void InitializeMockData()
    {
        // Generate initial mock feed data
        var mockItems = MockFeedData.GenerateMockFeedItems(100);
        foreach (var item in mockItems)
        {
            _feedItems.TryAdd(item.Id, item);
        }

        // Initialize refresh times
        foreach (var platform in Enum.GetValues<SocialPlatform>())
        {
            _lastRefreshTimes.TryAdd(platform, DateTime.UtcNow.AddMinutes(-30)); // Simulate last refresh 30 minutes ago
        }
    }
} 