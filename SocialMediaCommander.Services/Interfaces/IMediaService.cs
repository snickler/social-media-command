using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Service interface for managing media files
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// Uploads a media file from stream
    /// </summary>
    Task<Media> UploadMediaAsync(Stream fileStream, string fileName, string mimeType);
    
    /// <summary>
    /// Uploads a media file from file path
    /// </summary>
    Task<Media> UploadMediaAsync(string filePath);
    
    /// <summary>
    /// Gets media by ID
    /// </summary>
    Task<Media?> GetMediaByIdAsync(string id);
    
    /// <summary>
    /// Gets multiple media files by IDs
    /// </summary>
    Task<IEnumerable<Media>> GetMediaByIdsAsync(IEnumerable<string> ids);
    
    /// <summary>
    /// Deletes a media file
    /// </summary>
    Task<bool> DeleteMediaAsync(string id);
    
    /// <summary>
    /// Generates a preview/thumbnail for media
    /// </summary>
    Task<string?> GeneratePreviewAsync(string mediaId);
    
    /// <summary>
    /// Gets media file stream
    /// </summary>
    Task<Stream?> GetMediaStreamAsync(string id);
    
    /// <summary>
    /// Validates media file for platform requirements
    /// </summary>
    Task<ValidationResult> ValidateMediaForPlatformAsync(string mediaId, SocialPlatform platform);
    
    /// <summary>
    /// Optimizes media file for platform requirements
    /// </summary>
    Task<Media> OptimizeMediaForPlatformAsync(string mediaId, SocialPlatform platform);
    
    /// <summary>
    /// Gets supported media formats for a platform
    /// </summary>
    Task<MediaFormats> GetSupportedFormatsAsync(SocialPlatform platform);
    
    /// <summary>
    /// Cleans up unused media files
    /// </summary>
    Task<int> CleanupUnusedMediaAsync(TimeSpan olderThan);
    
    /// <summary>
    /// Gets media storage statistics
    /// </summary>
    Task<MediaStorageStats> GetStorageStatisticsAsync();
    
    /// <summary>
    /// Processes media file (resize, convert, etc.)
    /// </summary>
    Task<Media> ProcessMediaAsync(string mediaId, MediaProcessingOptions options);
    
    /// <summary>
    /// Validates file before upload
    /// </summary>
    Task<ValidationResult> ValidateFileAsync(Stream fileStream, string fileName, string mimeType);
}

/// <summary>
/// Supported media formats for a platform
/// </summary>
public class MediaFormats
{
    public List<string> SupportedImageFormats { get; set; } = new();
    public List<string> SupportedVideoFormats { get; set; } = new();
    public long MaxFileSize { get; set; }
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public bool SupportsGifs { get; set; }
}

/// <summary>
/// Media storage statistics
/// </summary>
public class MediaStorageStats
{
    public long TotalSize { get; set; }
    public int TotalFiles { get; set; }
    public Dictionary<MediaType, int> FilesByType { get; set; } = new();
    public Dictionary<MediaType, long> SizeByType { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Options for media processing
/// </summary>
public class MediaProcessingOptions
{
    public int? MaxWidth { get; set; }
    public int? MaxHeight { get; set; }
    public int? Quality { get; set; }
    public string? OutputFormat { get; set; }
    public bool GenerateThumbnail { get; set; } = true;
    public TimeSpan? MaxDuration { get; set; }
    public bool OptimizeForWeb { get; set; } = true;
} 