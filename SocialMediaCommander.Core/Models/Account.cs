using System.ComponentModel.DataAnnotations;

namespace SocialMediaCommander.Core.Models;

/// <summary>
/// Represents a user account for a specific social media platform
/// </summary>
public class Account
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public SocialPlatform PlatformId { get; set; }
    
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string DisplayName { get; set; } = string.Empty;
    
    public string? Avatar { get; set; }
    
    public bool IsDefault { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Authentication status of the account
    /// </summary>
    public AuthenticationStatus AuthStatus { get; set; } = AuthenticationStatus.NotAuthenticated;
    
    /// <summary>
    /// OAuth tokens for API authentication
    /// </summary>
    public OAuthTokens? Tokens { get; set; }
    
    /// <summary>
    /// OAuth configuration for this account (client credentials, endpoints, etc.)
    /// This will be encrypted when stored
    /// </summary>
    public OAuthConfig? OAuthConfiguration { get; set; }
    
    /// <summary>
    /// Additional metadata for the account
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Generates a default avatar URL for the account
    /// </summary>
    public string GetAvatarUrl()
    {
        if (!string.IsNullOrEmpty(Avatar))
            return Avatar;
            
        var platformName = PlatformId.ToString().ToLower();
        var seed = $"{platformName}-{Id}";
        return $"https://api.dicebear.com/7.x/personas/svg?seed={seed}";
    }

    /// <summary>
    /// Checks if the account has valid authentication
    /// </summary>
    public bool IsAuthenticated => AuthStatus == AuthenticationStatus.Authenticated && 
                                   Tokens != null && 
                                   !Tokens.IsExpired;
}

/// <summary>
/// OAuth token information for authenticated accounts
/// </summary>
public class OAuthTokens
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public string[]? Scopes { get; set; }
    
    /// <summary>
    /// Checks if the access token is expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5); // 5 minute buffer
    
    /// <summary>
    /// Checks if the token can be refreshed
    /// </summary>
    public bool CanRefresh => !string.IsNullOrEmpty(RefreshToken);
}

/// <summary>
/// Authentication status for accounts
/// </summary>
public enum AuthenticationStatus
{
    NotAuthenticated,
    Authenticating,
    Authenticated,
    AuthenticationFailed,
    TokenExpired,
    Revoked
}

/// <summary>
/// Configuration for default accounts created for each platform
/// </summary>
public static class DefaultAccounts
{
    public static readonly Dictionary<SocialPlatform, Account> Accounts = new()
    {
        {
            SocialPlatform.BlueSky,
            new Account
            {
                Id = "bluesky-default",
                PlatformId = SocialPlatform.BlueSky,
                Username = "default_user",
                DisplayName = "Default BlueSky",
                IsDefault = true
            }
        },
        {
            SocialPlatform.X,
            new Account
            {
                Id = "x-default",
                PlatformId = SocialPlatform.X,
                Username = "default_user",
                DisplayName = "Default X",
                IsDefault = true
            }
        },
        {
            SocialPlatform.LinkedIn,
            new Account
            {
                Id = "linkedin-default",
                PlatformId = SocialPlatform.LinkedIn,
                Username = "default_user",
                DisplayName = "Default LinkedIn",
                IsDefault = true
            }
        },
        {
            SocialPlatform.Threads,
            new Account
            {
                Id = "threads-default",
                PlatformId = SocialPlatform.Threads,
                Username = "default_user",
                DisplayName = "Default Threads",
                IsDefault = true
            }
        },
        {
            SocialPlatform.Facebook,
            new Account
            {
                Id = "facebook-default",
                PlatformId = SocialPlatform.Facebook,
                Username = "default_user",
                DisplayName = "Default Facebook",
                IsDefault = true
            }
        }
    };

    public static IEnumerable<Account> GetAllDefaultAccounts()
    {
        return Accounts.Values.Select(account => new Account
        {
            Id = account.Id,
            PlatformId = account.PlatformId,
            Username = account.Username,
            DisplayName = account.DisplayName,
            Avatar = account.GetAvatarUrl(),
            IsDefault = account.IsDefault,
            CreatedAt = DateTime.UtcNow,
            LastUsed = DateTime.UtcNow
        });
    }

    public static Account? GetDefaultAccountForPlatform(SocialPlatform platform)
    {
        if (Accounts.TryGetValue(platform, out var account))
        {
            return new Account
            {
                Id = account.Id,
                PlatformId = account.PlatformId,
                Username = account.Username,
                DisplayName = account.DisplayName,
                Avatar = account.GetAvatarUrl(),
                IsDefault = account.IsDefault,
                CreatedAt = DateTime.UtcNow,
                LastUsed = DateTime.UtcNow
            };
        }
        return null;
    }
} 