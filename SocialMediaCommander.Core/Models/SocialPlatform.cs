namespace SocialMediaCommander.Core.Models;

/// <summary>
/// Represents the supported social media platforms
/// </summary>
public enum SocialPlatform
{
    BlueSky,
    X,
    LinkedIn,
    Threads,
    Facebook
}

/// <summary>
/// Configuration settings for each social media platform
/// </summary>
public class SocialPlatformConfig
{
    public SocialPlatform Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int? CharacterLimit { get; set; }
    public bool HashtagSupport { get; set; }
    public bool MediaSupport { get; set; }
    public bool ThreadSupport { get; set; }
    public string IconPath { get; set; } = string.Empty;
}

/// <summary>
/// Static configuration for all supported platforms
/// </summary>
public static class PlatformConfigurations
{
    public static readonly Dictionary<SocialPlatform, SocialPlatformConfig> Platforms = new()
    {
        {
            SocialPlatform.BlueSky,
            new SocialPlatformConfig
            {
                Id = SocialPlatform.BlueSky,
                Name = "BlueSky",
                Color = "#1285FE",
                CharacterLimit = 300,
                HashtagSupport = true,
                MediaSupport = true,
                ThreadSupport = true,
                IconPath = "avares://SocialMediaCommander.Desktop/Assets/Icons/bluesky.png"
            }
        },
        {
            SocialPlatform.X,
            new SocialPlatformConfig
            {
                Id = SocialPlatform.X,
                Name = "X",
                Color = "#000000",
                CharacterLimit = 280,
                HashtagSupport = true,
                MediaSupport = true,
                ThreadSupport = true,
                IconPath = "avares://SocialMediaCommander.Desktop/Assets/Icons/x.png"
            }
        },
        {
            SocialPlatform.LinkedIn,
            new SocialPlatformConfig
            {
                Id = SocialPlatform.LinkedIn,
                Name = "LinkedIn",
                Color = "#0077B5",
                CharacterLimit = null,
                HashtagSupport = true,
                MediaSupport = true,
                ThreadSupport = false,
                IconPath = "avares://SocialMediaCommander.Desktop/Assets/Icons/linkedin.png"
            }
        },
        {
            SocialPlatform.Threads,
            new SocialPlatformConfig
            {
                Id = SocialPlatform.Threads,
                Name = "Threads",
                Color = "#000000",
                CharacterLimit = 500,
                HashtagSupport = true,
                MediaSupport = true,
                ThreadSupport = true,
                IconPath = "avares://SocialMediaCommander.Desktop/Assets/Icons/threads.png"
            }
        },
        {
            SocialPlatform.Facebook,
            new SocialPlatformConfig
            {
                Id = SocialPlatform.Facebook,
                Name = "Facebook",
                Color = "#1877F2",
                CharacterLimit = null,
                HashtagSupport = true,
                MediaSupport = true,
                ThreadSupport = false,
                IconPath = "avares://SocialMediaCommander.Desktop/Assets/Icons/facebook.png"
            }
        }
    };

    public static SocialPlatformConfig GetPlatformConfig(SocialPlatform platform)
    {
        return Platforms.TryGetValue(platform, out var config) ? config : Platforms[SocialPlatform.BlueSky];
    }

    public static IEnumerable<SocialPlatformConfig> GetAllPlatforms()
    {
        return Platforms.Values;
    }

    public static IEnumerable<SocialPlatformConfig> GetThreadSupportedPlatforms()
    {
        return Platforms.Values.Where(p => p.ThreadSupport);
    }
} 