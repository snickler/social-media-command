using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Twitter/X-specific service interface
/// </summary>
public interface ITwitterService : IPlatformService
{
    /// <summary>
    /// Retweets a post
    /// </summary>
    Task<bool> RetweetAsync(string postId, Account account);

    /// <summary>
    /// Likes a tweet
    /// </summary>
    Task<bool> LikeAsync(string postId, Account account);

    /// <summary>
    /// Gets Twitter rate limit information
    /// </summary>
    Task<RateLimitInfo> GetRateLimitInfoAsync(Account account);
}