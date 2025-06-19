namespace SocialMediaCommander.Core.Models;

/// <summary>
/// OAuth configuration for a social media platform
/// </summary>
public class OAuthConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AuthorizationEndpoint { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string? RevokeEndpoint { get; set; }
    public string? UserInfoEndpoint { get; set; }
    public string RedirectUri { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> AdditionalParameters { get; set; } = new();
} 