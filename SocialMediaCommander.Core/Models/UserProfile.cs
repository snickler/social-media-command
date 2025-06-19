namespace SocialMediaCommander.Core.Models;

/// <summary>
/// Represents a user profile from a social media platform
/// </summary>
public class UserProfile
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Avatar { get; set; }
    public string? Banner { get; set; }
    public string? ProfileUrl { get; set; }
    public string? Website { get; set; }
    public string? Location { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public int PostCount { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public SocialPlatform Platform { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
} 