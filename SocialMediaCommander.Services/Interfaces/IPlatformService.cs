using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Base interface for platform-specific social media services
/// </summary>
public interface IPlatformService
{
    /// <summary>
    /// The platform this service handles
    /// </summary>
    SocialPlatform Platform { get; }
    
    /// <summary>
    /// Posts content to the platform
    /// </summary>
    Task<PublishResult> PostAsync(Post post, Account account);
    
    /// <summary>
    /// Posts a thread to the platform
    /// </summary>
    Task<PublishResult> PostThreadAsync(Post post, Account account);
    
    /// <summary>
    /// Deletes a post from the platform
    /// </summary>
    Task<bool> DeletePostAsync(string postId, Account account);
    
    /// <summary>
    /// Gets user's posts from the platform
    /// </summary>
    Task<IEnumerable<SocialFeedItem>> GetUserPostsAsync(Account account, int limit = 20);
    
    /// <summary>
    /// Gets user's timeline/feed from the platform
    /// </summary>
    Task<IEnumerable<SocialFeedItem>> GetTimelineAsync(Account account, int limit = 50);
    
    /// <summary>
    /// Searches for posts on the platform
    /// </summary>
    Task<IEnumerable<SocialFeedItem>> SearchPostsAsync(string query, Account account, int limit = 20);
    
    /// <summary>
    /// Gets trending topics/hashtags
    /// </summary>
    Task<IEnumerable<string>> GetTrendingHashtagsAsync(Account account);
    
    /// <summary>
    /// Validates content for platform-specific requirements
    /// </summary>
    Task<ValidationResult> ValidateContentAsync(Post post);
    
    /// <summary>
    /// Uploads media to the platform
    /// </summary>
    Task<string> UploadMediaAsync(Media media, Account account);
    
    /// <summary>
    /// Gets platform-specific posting limits and restrictions
    /// </summary>
    Task<PlatformLimits> GetPlatformLimitsAsync();
}

/// <summary>
/// Platform-specific limits and restrictions
/// </summary>
public class PlatformLimits
{
    public int? CharacterLimit { get; set; }
    public int MaxMediaCount { get; set; }
    public long MaxMediaSize { get; set; }
    public string[] SupportedMediaTypes { get; set; } = Array.Empty<string>();
    public int MaxThreadLength { get; set; }
    public TimeSpan PostingInterval { get; set; }
    public int DailyPostLimit { get; set; }
    public Dictionary<string, object> AdditionalLimits { get; set; } = new();
}

/// <summary>
/// Rate limit information for a platform
/// </summary>
public class RateLimitInfo
{
    public int Remaining { get; set; }
    public int Limit { get; set; }
    public DateTime ResetTime { get; set; }
}

 