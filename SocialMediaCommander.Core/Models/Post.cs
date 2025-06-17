using System.ComponentModel.DataAnnotations;

namespace SocialMediaCommander.Core.Models;

/// <summary>
/// Represents a media file attached to a post
/// </summary>
public class Media
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    public string FilePath { get; set; } = string.Empty;
    
    public string? PreviewUrl { get; set; }
    
    [Required]
    public MediaType Type { get; set; }
    
    public long FileSize { get; set; }
    
    public string MimeType { get; set; } = string.Empty;
    
    public int? Width { get; set; }
    
    public int? Height { get; set; }
    
    public TimeSpan? Duration { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Types of media that can be attached to posts
/// </summary>
public enum MediaType
{
    Image,
    Video,
    Gif
}

/// <summary>
/// Represents a single post in a thread
/// </summary>
public class ThreadPost
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
    
    public List<Media> Media { get; set; } = new();
    
    public int Order { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a social media post that can be published to multiple platforms
/// </summary>
public class Post
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
    
    public List<SocialPlatform> TargetPlatforms { get; set; } = new();
    
    public List<string> Hashtags { get; set; } = new();
    
    public bool PromoMode { get; set; }
    
    public List<Media> Media { get; set; } = new();
    
    public bool IsThread { get; set; }
    
    public List<ThreadPost> ThreadPosts { get; set; } = new();
    
    public bool ThreadsOnlyMode { get; set; }
    
    /// <summary>
    /// Maps platform to selected account IDs
    /// </summary>
    public Dictionary<SocialPlatform, List<string>> SelectedAccounts { get; set; } = new();
    
    public PostStatus Status { get; set; } = PostStatus.Draft;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? PublishedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Results of publishing to each platform
    /// </summary>
    public Dictionary<SocialPlatform, PublishResult> PublishResults { get; set; } = new();

    /// <summary>
    /// Gets the total character count for the post content
    /// </summary>
    public int GetCharacterCount()
    {
        var baseCount = Content.Length;
        
        if (PromoMode && Hashtags.Any())
        {
            var hashtagText = string.Join(" ", Hashtags.Select(h => $"#{h}"));
            baseCount += hashtagText.Length + 2; // +2 for line breaks
        }
        
        return baseCount;
    }

    /// <summary>
    /// Validates the post for a specific platform
    /// </summary>
    public ValidationResult ValidateForPlatform(SocialPlatform platform)
    {
        var config = PlatformConfigurations.GetPlatformConfig(platform);
        var errors = new List<string>();

        // Check character limit
        if (config.CharacterLimit.HasValue)
        {
            var charCount = GetCharacterCount();
            if (charCount > config.CharacterLimit.Value)
            {
                errors.Add($"Content exceeds {config.Name} character limit of {config.CharacterLimit.Value}");
            }
        }

        // Check thread support
        if ((IsThread || ThreadsOnlyMode) && !config.ThreadSupport)
        {
            errors.Add($"{config.Name} does not support thread posts");
        }

        // Check media support
        if (Media.Any() && !config.MediaSupport)
        {
            errors.Add($"{config.Name} does not support media attachments");
        }

        // Check if account is selected
        if (!SelectedAccounts.ContainsKey(platform) || !SelectedAccounts[platform].Any())
        {
            errors.Add($"No account selected for {config.Name}");
        }

        return new ValidationResult(errors);
    }

    /// <summary>
    /// Formats the post content for a specific platform
    /// </summary>
    public string FormatForPlatform(SocialPlatform platform)
    {
        var config = PlatformConfigurations.GetPlatformConfig(platform);
        var formattedContent = Content;

        // Add hashtags if promo mode is enabled and platform supports hashtags
        if (PromoMode && Hashtags.Any() && config.HashtagSupport)
        {
            var hashtagString = string.Join(" ", Hashtags.Select(h => $"#{h}"));
            formattedContent = $"{formattedContent}\n\n{hashtagString}";
        }

        // Add media indicators if media is present and platform supports media
        if (Media.Any() && config.MediaSupport)
        {
            var mediaCount = Media.Count;
            formattedContent = $"{formattedContent}\n\n[{mediaCount} media attachment{(mediaCount > 1 ? "s" : "")}]";
        }

        // Add thread indicator
        if ((IsThread || ThreadsOnlyMode) && config.ThreadSupport && ThreadPosts.Any())
        {
            formattedContent = $"{formattedContent}\n\n[Thread with {ThreadPosts.Count + 1} posts]";
        }

        // Truncate if over character limit
        if (config.CharacterLimit.HasValue && formattedContent.Length > config.CharacterLimit.Value)
        {
            formattedContent = formattedContent.Substring(0, config.CharacterLimit.Value - 3) + "...";
        }

        return formattedContent;
    }
}

/// <summary>
/// Status of a post in the publishing workflow
/// </summary>
public enum PostStatus
{
    Draft,
    Publishing,
    Published,
    Failed,
    PartiallyPublished
}

/// <summary>
/// Result of publishing to a platform
/// </summary>
public class PublishResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PlatformPostId { get; set; }
    public DateTime PublishedAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Validation result for post content
/// </summary>
public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<string> Errors { get; set; } = new();

    public ValidationResult()
    {
    }

    public ValidationResult(List<string> errors)
    {
        Errors = errors;
    }
}

/// <summary>
/// Default hashtags commonly used in social media marketing
/// </summary>
public static class DefaultHashtags
{
    public static readonly List<string> Marketing = new()
    {
        "socialmedia",
        "digitalmarketing",
        "contentcreation",
        "marketing",
        "socialmediamarketing",
        "branding",
        "business",
        "entrepreneur",
        "success",
        "growth"
    };

    public static readonly List<string> Technology = new()
    {
        "technology",
        "innovation",
        "tech",
        "startup",
        "development",
        "programming",
        "software",
        "ai",
        "machinelearning",
        "blockchain"
    };

    public static readonly List<string> Lifestyle = new()
    {
        "lifestyle",
        "wellness",
        "motivation",
        "inspiration",
        "productivity",
        "mindfulness",
        "health",
        "fitness",
        "travel",
        "photography"
    };

    public static List<string> GetDefaultHashtags()
    {
        return Marketing.Take(5).ToList();
    }
} 