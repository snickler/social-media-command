using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Facebook-specific service interface
/// </summary>
public interface IFacebookService : IPlatformService
{
    /// <summary>
    /// Gets user's Facebook profile
    /// </summary>
    Task<UserProfile?> GetProfileAsync(Account account);

    /// <summary>
    /// Gets Facebook rate limit information
    /// </summary>
    Task<RateLimitInfo> GetRateLimitInfoAsync(Account account);

    /// <summary>
    /// Likes a post on Facebook
    /// </summary>
    Task<bool> LikeAsync(string postId, Account account);

    /// <summary>
    /// Shares a post on Facebook
    /// </summary>
    Task<bool> ShareAsync(string postId, Account account);

    /// <summary>
    /// Posts to a Facebook page
    /// </summary>
    Task<PublishResult> PostToPageAsync(Post post, Account account, string pageId);

    /// <summary>
    /// Gets Facebook page posts
    /// </summary>
    Task<IEnumerable<SocialFeedItem>> GetPagePostsAsync(string pageId, Account account, int limit = 20);
}