using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// BlueSky-specific service interface
/// </summary>
public interface IBlueSkyService : IPlatformService
{
    /// <summary>
    /// Creates a BlueSky session
    /// </summary>
    Task<bool> CreateSessionAsync(Account account);

    /// <summary>
    /// Gets user's BlueSky profile
    /// </summary>
    Task<UserProfile?> GetProfileAsync(Account account);
}