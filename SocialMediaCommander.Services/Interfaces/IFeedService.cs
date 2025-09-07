using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Service interface for managing social media feeds
/// </summary>
public interface IFeedService
{
    /// <summary>
    /// Gets feed items from specified platforms
    /// </summary>
    Task<IEnumerable<SocialFeedItem>> GetFeedItemsAsync(
        IEnumerable<SocialPlatform> platforms,
        int limit = 50);

    /// <summary>
    /// Gets feed items with configuration options
    /// </summary>
    Task<IEnumerable<SocialFeedItem>> GetFeedItemsAsync(FeedConfiguration configuration);

    /// <summary>
    /// Refreshes feed data from external sources
    /// </summary>
    Task RefreshFeedAsync(IEnumerable<SocialPlatform> platforms);

    /// <summary>
    /// Gets the last refresh time for each platform
    /// </summary>
    Task<Dictionary<SocialPlatform, DateTime?>> GetLastRefreshTimesAsync();

    /// <summary>
    /// Searches feed items by content
    /// </summary>
    Task<IEnumerable<SocialFeedItem>> SearchFeedItemsAsync(
        string searchTerm,
        IEnumerable<SocialPlatform>? platforms = null);

    /// <summary>
    /// Gets feed items filtered by hashtags
    /// </summary>
    Task<IEnumerable<SocialFeedItem>> GetFeedItemsByHashtagsAsync(
        IEnumerable<string> hashtags,
        IEnumerable<SocialPlatform>? platforms = null);

    /// <summary>
    /// Gets trending hashtags across platforms
    /// </summary>
    Task<Dictionary<SocialPlatform, IEnumerable<string>>> GetTrendingHashtagsAsync();

    /// <summary>
    /// Gets feed statistics
    /// </summary>
    Task<FeedStatistics> GetFeedStatisticsAsync(
        IEnumerable<SocialPlatform> platforms,
        TimeSpan timeRange);

    /// <summary>
    /// Marks feed items as read
    /// </summary>
    Task MarkFeedItemsAsReadAsync(IEnumerable<string> itemIds);

    /// <summary>
    /// Gets unread feed items count
    /// </summary>
    Task<Dictionary<SocialPlatform, int>> GetUnreadCountsAsync();
}

/// <summary>
/// Statistics for social media feeds
/// </summary>
public class FeedStatistics
{
    public int TotalPosts { get; set; }
    public int TotalEngagement { get; set; }
    public Dictionary<SocialPlatform, int> PostsByPlatform { get; set; } = new();
    public Dictionary<SocialPlatform, int> EngagementByPlatform { get; set; } = new();
    public List<string> TopHashtags { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}