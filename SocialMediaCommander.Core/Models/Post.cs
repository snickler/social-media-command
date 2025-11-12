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

    public DateTime CreatedAt { get; set; } = Post.GetSafeUtcNow();

    /// <summary>
    /// Alt text for accessibility (especially important for BlueSky)
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// Gets a formatted file size string
    /// </summary>
    public string FileSizeFormatted
    {
        get
        {
            if (FileSize < 1024)
                return $"{FileSize} B";
            if (FileSize < 1024 * 1024)
                return $"{FileSize / 1024.0:F1} KB";
            if (FileSize < 1024 * 1024 * 1024)
                return $"{FileSize / (1024.0 * 1024.0):F1} MB";
            return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
    }
}

/// <summary>
/// Types of media that can be attached to posts
/// </summary>
public enum MediaType
{
    Image,
    Video,
    Audio,
    Document,
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

    public DateTime CreatedAt { get; set; } = Post.GetSafeUtcNow();
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

    public DateTime CreatedAt { get; set; } = Post.GetSafeUtcNow();

    public DateTime? PublishedAt { get; set; }

    public DateTime UpdatedAt { get; set; } = Post.GetSafeUtcNow();

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

        // Truncate if over character limit
        if (config.CharacterLimit.HasValue && formattedContent.Length > config.CharacterLimit.Value)
        {
            formattedContent = formattedContent.Substring(0, config.CharacterLimit.Value - 3) + "...";
        }

        return formattedContent;
    }

    /// <summary>
    /// Safe DateTime.UtcNow that handles potential overflow issues
    /// </summary>
    internal static DateTime GetSafeUtcNow()
    {
        try
        {
            return DateTime.UtcNow;
        }
        catch (ArgumentOutOfRangeException)
        {
            // Fallback to a safe date if DateTime.UtcNow causes overflow
            return new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }
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