using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Service interface for handling OAuth authentication across social media platforms
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Initiates OAuth authentication flow for a platform
    /// </summary>
    Task<AuthenticationResult> StartAuthenticationAsync(SocialPlatform platform, string? redirectUri = null);

    /// <summary>
    /// Completes OAuth authentication flow with authorization code
    /// </summary>
    Task<AuthenticationResult> CompleteAuthenticationAsync(SocialPlatform platform, string authorizationCode, string? state = null);

    /// <summary>
    /// Refreshes an expired access token
    /// </summary>
    Task<AuthenticationResult> RefreshTokenAsync(SocialPlatform platform, string refreshToken);

    /// <summary>
    /// Revokes authentication tokens
    /// </summary>
    Task<bool> RevokeTokenAsync(SocialPlatform platform, string accessToken);

    /// <summary>
    /// Validates if a token is still valid
    /// </summary>
    Task<bool> ValidateTokenAsync(SocialPlatform platform, string accessToken);

    /// <summary>
    /// Gets user profile information using access token
    /// </summary>
    Task<UserProfile?> GetUserProfileAsync(SocialPlatform platform, string accessToken);

    /// <summary>
    /// Gets OAuth configuration for a platform
    /// </summary>
    OAuthConfig GetOAuthConfig(SocialPlatform platform);
}

/// <summary>
/// Result of an authentication operation
/// </summary>
public class AuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public OAuthTokens? Tokens { get; set; }
    public UserProfile? UserProfile { get; set; }
    public string? AuthorizationUrl { get; set; }
    public string? State { get; set; }
}

