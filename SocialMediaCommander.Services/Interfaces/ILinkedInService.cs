using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// LinkedIn-specific service interface
/// </summary>
public interface ILinkedInService : IPlatformService
{
    /// <summary>
    /// Shares a post to LinkedIn
    /// </summary>
    Task<bool> SharePostAsync(string postId, Account account, string? commentary = null);

    /// <summary>
    /// Gets user's LinkedIn profile
    /// </summary>
    Task<UserProfile?> GetProfileAsync(Account account);

    /// <summary>
    /// Gets user's LinkedIn connections
    /// </summary>
    Task<IEnumerable<UserProfile>> GetConnectionsAsync(Account account, int limit = 50);

    /// <summary>
    /// Gets user's LinkedIn connections (simplified overload)
    /// </summary>
    Task<IEnumerable<UserProfile>> GetConnectionsAsync(Account account);

    /// <summary>
    /// Gets company page posts
    /// </summary>
    Task<IEnumerable<SocialFeedItem>> GetCompanyPostsAsync(string companyId, Account account, int limit = 20);

    /// <summary>
    /// Posts to a LinkedIn company page
    /// </summary>
    Task<PublishResult> PostToCompanyPageAsync(Post post, Account account, string companyId);

    /// <summary>
    /// Gets LinkedIn rate limit information
    /// </summary>
    Task<RateLimitInfo> GetRateLimitInfoAsync(Account account);
}