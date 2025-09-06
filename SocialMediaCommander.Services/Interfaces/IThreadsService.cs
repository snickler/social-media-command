using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Threads-specific service interface
/// </summary>
public interface IThreadsService : IPlatformService
{
    /// <summary>
    /// Gets user's Threads profile
    /// </summary>
    Task<UserProfile?> GetProfileAsync(Account account);

    /// <summary>
    /// Gets Threads rate limit information
    /// </summary>
    Task<RateLimitInfo> GetRateLimitInfoAsync(Account account);

    /// <summary>
    /// Likes a post on Threads
    /// </summary>
    Task<bool> LikeAsync(string postId, Account account);

    /// <summary>
    /// Reposts a post on Threads
    /// </summary>
    Task<bool> RepostAsync(string postId, Account account);
}