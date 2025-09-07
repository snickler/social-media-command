namespace SocialMediaCommander.Core.Models;

/// <summary>
/// Represents a social media feed item displayed in the application
/// </summary>
public class SocialFeedItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public SocialPlatform Platform { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public string AuthorUsername { get; set; } = string.Empty;

    public string AuthorAvatar { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime PostedAt { get; set; }

    public List<Media> Media { get; set; } = new();

    public SocialEngagement Engagement { get; set; } = new();

    public bool IsThread { get; set; }

    public List<string> Hashtags { get; set; } = new();

    public string? PlatformPostId { get; set; }

    public string? PlatformUrl { get; set; }

    /// <summary>
    /// Gets the formatted time since the post was created
    /// </summary>
    public string GetTimeAgo()
    {
        var timeSpan = DateTime.UtcNow - PostedAt;

        return timeSpan.TotalDays switch
        {
            >= 365 => $"{(int)(timeSpan.TotalDays / 365)}y ago",
            >= 30 => $"{(int)(timeSpan.TotalDays / 30)}mo ago",
            >= 7 => $"{(int)(timeSpan.TotalDays / 7)}w ago",
            >= 1 => $"{(int)timeSpan.TotalDays}d ago",
            _ => timeSpan.TotalHours switch
            {
                >= 1 => $"{(int)timeSpan.TotalHours}h ago",
                _ => timeSpan.TotalMinutes switch
                {
                    >= 1 => $"{(int)timeSpan.TotalMinutes}m ago",
                    _ => "Just now"
                }
            }
        };
    }
}

/// <summary>
/// Represents engagement metrics for a social media post
/// </summary>
public class SocialEngagement
{
    public int Likes { get; set; }

    public int Comments { get; set; }

    public int Shares { get; set; }

    public int Views { get; set; }

    /// <summary>
    /// Platform-specific engagement metrics
    /// </summary>
    public Dictionary<string, int> CustomMetrics { get; set; } = new();

    /// <summary>
    /// Gets the total engagement count
    /// </summary>
    public int GetTotalEngagement()
    {
        return Likes + Comments + Shares + CustomMetrics.Values.Sum();
    }
}

/// <summary>
/// Configuration for displaying social feeds
/// </summary>
public class FeedConfiguration
{
    public List<SocialPlatform> EnabledPlatforms { get; set; } = new();

    public int MaxItemsPerPlatform { get; set; } = 10;

    public TimeSpan MaxAge { get; set; } = TimeSpan.FromDays(7);

    public bool ShowEngagement { get; set; } = true;

    public bool ShowMedia { get; set; } = true;

    public bool GroupByPlatform { get; set; } = false;

    public FeedSortOrder SortOrder { get; set; } = FeedSortOrder.Chronological;
}

/// <summary>
/// Sort order options for social feeds
/// </summary>
public enum FeedSortOrder
{
    Chronological,
    Engagement,
    Platform
}

/// <summary>
/// Mock data generator for social feed items (for demonstration purposes)
/// </summary>
public static class MockFeedData
{
    private static readonly Random _random = new();

    private static readonly List<string> _sampleAuthors = new()
    {
        "BlueSky User 1", "BlueSky User 2", "BlueSky User 3",
        "X User 1", "X User 2", "X User 3",
        "LinkedIn User 1", "LinkedIn User 2", "LinkedIn User 3",
        "Threads User 1", "Threads User 2", "Threads User 3",
        "Facebook User 1", "Facebook User 2", "Facebook User 3"
    };

    private static readonly List<string> _sampleContent = new()
    {
        "Latest updates from the world of tech! #technology #innovation",
        "Just shared my thoughts on sustainable business practices. Check out my latest article!",
        "Amazing day at the conference! Met so many inspiring people.",
        "New blog post about digital marketing trends for 2024. Link in bio!",
        "Beautiful sunset from my travels today. Nature never fails to amaze me.",
        "Working on an exciting new project. Can't wait to share more details!",
        "Great discussion at today's webinar about AI and the future of work.",
        "Celebrating small wins and progress. Every step counts!",
        "Quick tip: Always backup your work before making major changes.",
        "Inspired by the creativity and innovation I see in our community every day."
    };

    public static List<SocialFeedItem> GenerateMockFeedItems(int count = 50)
    {
        var items = new List<SocialFeedItem>();
        var platforms = Enum.GetValues<SocialPlatform>();

        for (int i = 0; i < count; i++)
        {
            var platform = platforms[_random.Next(platforms.Length)];
            var platformConfig = PlatformConfigurations.GetPlatformConfig(platform);
            var author = _sampleAuthors[_random.Next(_sampleAuthors.Count)];
            var content = _sampleContent[_random.Next(_sampleContent.Count)];

            var item = new SocialFeedItem
            {
                Platform = platform,
                AuthorName = author,
                AuthorUsername = author.Replace(" ", "").ToLower(),
                AuthorAvatar = $"https://api.dicebear.com/7.x/personas/svg?seed={author.Replace(" ", "")}",
                Content = content,
                PostedAt = DateTime.UtcNow.AddMinutes(-_random.Next(0, 10080)), // Random time within last week
                Engagement = new SocialEngagement
                {
                    Likes = _random.Next(0, 100),
                    Comments = _random.Next(0, 50),
                    Shares = _random.Next(0, 25),
                    Views = _random.Next(100, 1000)
                },
                IsThread = _random.Next(0, 10) == 0, // 10% chance of being a thread
                Hashtags = ExtractHashtags(content),
                PlatformPostId = $"{platform.ToString().ToLower()}-{Guid.NewGuid().ToString()[..8]}",
                PlatformUrl = $"https://{platform.ToString().ToLower()}.com/post/{Guid.NewGuid().ToString()[..8]}"
            };

            items.Add(item);
        }

        return items.OrderByDescending(item => item.PostedAt).ToList();
    }

    private static List<string> ExtractHashtags(string content)
    {
        var hashtags = new List<string>();
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            if (word.StartsWith('#') && word.Length > 1)
            {
                hashtags.Add(word[1..].TrimEnd('!', '.', ',', '?'));
            }
        }

        return hashtags;
    }
}