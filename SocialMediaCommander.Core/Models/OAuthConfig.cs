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

/// <summary>
/// Represents the status of an OAuth configuration
/// </summary>
public class ConfigurationStatus
{
    /// <summary>
    /// Whether the configuration is properly set up and ready to use
    /// </summary>
    public bool IsConfigured { get; set; }
    
    /// <summary>
    /// Whether the configuration contains placeholder values
    /// </summary>
    public bool HasPlaceholders { get; set; }
    
    /// <summary>
    /// Short status description (e.g., "Ready", "Needs setup", "Invalid")
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed status message for the user
    /// </summary>
    public string Message { get; set; } = string.Empty;
} 