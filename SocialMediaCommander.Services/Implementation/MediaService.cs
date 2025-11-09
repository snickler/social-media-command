using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// Production implementation of IMediaService for managing media files
/// Follows Microsoft performance best practices with ConfigureAwait(false)
/// </summary>
public class MediaService : IMediaService
{
    private readonly Dictionary<string, Media> _mediaCache = new();
    private readonly string _mediaStoragePath;

    public MediaService()
    {
        // Store media metadata in app data, but preserve original file paths
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _mediaStoragePath = Path.Combine(appData, "SocialMediaCommander", "Media");
        Directory.CreateDirectory(_mediaStoragePath);
    }

    public async Task<Media> UploadMediaAsync(Stream fileStream, string fileName, string mimeType)
    {
        var media = new Media
        {
            Id = Guid.NewGuid().ToString(),
            FileName = fileName,
            MimeType = mimeType,
            FileSize = fileStream.Length,
            Type = GetMediaTypeFromMimeType(mimeType),
            FilePath = $"stream://{fileName}", // Placeholder for stream-based uploads
            PreviewUrl = null,
            CreatedAt = DateTime.UtcNow
        };

        _mediaCache[media.Id] = media;
        return await Task.FromResult(media).ConfigureAwait(false);
    }

    public async Task<Media> UploadMediaAsync(string filePath)
    {
        // Validate file exists
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Media file not found: {filePath}", filePath);
        }

        var fileInfo = new FileInfo(filePath);
        var fileName = fileInfo.Name;
        var extension = fileInfo.Extension;
        var mimeType = GetMimeTypeFromExtension(extension);

        // Create media object with ACTUAL file path preserved
        var media = new Media
        {
            Id = Guid.NewGuid().ToString(),
            FileName = fileName,
            MimeType = mimeType,
            FileSize = fileInfo.Length,
            Type = GetMediaTypeFromExtension(extension),
            FilePath = filePath, // ? Preserve actual file path for platform services to read
            PreviewUrl = null,
            CreatedAt = DateTime.UtcNow
        };

        // Determine dimensions for images
        if (media.Type == MediaType.Image)
        {
            try
            {
                // Note: For full production, use a library like ImageSharp to get dimensions
                // For now, we'll set placeholder dimensions
                media.Width = null;
                media.Height = null;
            }
            catch
            {
                // Ignore dimension detection errors
            }
        }

        _mediaCache[media.Id] = media;
        return await Task.FromResult(media).ConfigureAwait(false);
    }

    public Task<Media?> GetMediaByIdAsync(string id)
    {
        _mediaCache.TryGetValue(id, out var media);
        return Task.FromResult(media);
    }

    public Task<IEnumerable<Media>> GetMediaByIdsAsync(IEnumerable<string> ids)
    {
        var medias = ids.Select(id => _mediaCache.GetValueOrDefault(id))
                       .Where(m => m != null)
                       .Cast<Media>();
        return Task.FromResult(medias);
    }

    public Task<bool> DeleteMediaAsync(string id)
    {
        if (_mediaCache.TryGetValue(id, out var media))
        {
            _mediaCache.Remove(id);

            // Optionally delete physical file if it was copied to our storage
            // For now, we preserve original files
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<string?> GeneratePreviewAsync(string mediaId)
    {
        if (_mediaCache.TryGetValue(mediaId, out var media))
        {
            // For images, the preview is the image itself
            if (media.Type == MediaType.Image && File.Exists(media.FilePath))
            {
                return Task.FromResult<string?>(media.FilePath);
            }
        }
        return Task.FromResult<string?>(null);
    }

    public async Task<Stream?> GetMediaStreamAsync(string id)
    {
        if (_mediaCache.TryGetValue(id, out var media) && File.Exists(media.FilePath))
        {
            return await Task.FromResult<Stream?>(File.OpenRead(media.FilePath)).ConfigureAwait(false);
        }
        return await Task.FromResult<Stream?>(null).ConfigureAwait(false);
    }

    public async Task<ValidationResult> ValidateMediaForPlatformAsync(string mediaId, SocialPlatform platform)
    {
        var result = new ValidationResult();

        if (!_mediaCache.TryGetValue(mediaId, out var media))
        {
            result.Errors.Add("Media not found");
            return await Task.FromResult(result).ConfigureAwait(false);
        }

        // Platform-specific validation
        var formats = await GetSupportedFormatsAsync(platform).ConfigureAwait(false);

        if (media.FileSize > formats.MaxFileSize)
        {
            result.Errors.Add($"File size exceeds platform limit of {formats.MaxFileSize / (1024 * 1024)}MB");
        }

        return result;
    }

    public Task<Media> OptimizeMediaForPlatformAsync(string mediaId, SocialPlatform platform)
    {
        if (_mediaCache.TryGetValue(mediaId, out var media))
        {
            // For now, return as-is. Full implementation would resize/compress
            return Task.FromResult(media);
        }
        throw new ArgumentException($"Media with ID {mediaId} not found", nameof(mediaId));
    }

    public Task<MediaFormats> GetSupportedFormatsAsync(SocialPlatform platform)
    {
        // Platform-specific limits
        return Task.FromResult(platform switch
        {
            SocialPlatform.X => new MediaFormats
            {
                SupportedImageFormats = new List<string> { "jpg", "jpeg", "png", "gif", "webp" },
                SupportedVideoFormats = new List<string> { "mp4", "mov" },
                MaxFileSize = 5 * 1024 * 1024, // 5MB for images
                MaxWidth = 4096,
                MaxHeight = 4096,
                MaxDuration = TimeSpan.FromMinutes(2.2),
                SupportsGifs = true
            },
            SocialPlatform.BlueSky => new MediaFormats
            {
                SupportedImageFormats = new List<string> { "jpg", "jpeg", "png", "gif", "webp" },
                SupportedVideoFormats = new List<string> { "mp4" },
                MaxFileSize = 1 * 1024 * 1024, // 1MB for images
                MaxWidth = 2000,
                MaxHeight = 2000,
                MaxDuration = TimeSpan.FromMinutes(1),
                SupportsGifs = true
            },
            _ => new MediaFormats
            {
                SupportedImageFormats = new List<string> { "jpg", "jpeg", "png" },
                SupportedVideoFormats = new List<string> { "mp4" },
                MaxFileSize = 10 * 1024 * 1024,
                MaxWidth = 4096,
                MaxHeight = 4096,
                MaxDuration = TimeSpan.FromMinutes(5),
                SupportsGifs = false
            }
        });
    }

    public Task<int> CleanupUnusedMediaAsync(TimeSpan olderThan)
    {
        var cutoffDate = DateTime.UtcNow - olderThan;
        var toRemove = _mediaCache.Where(kvp => kvp.Value.CreatedAt < cutoffDate)
                                 .Select(kvp => kvp.Key)
                                 .ToList();

        foreach (var id in toRemove)
        {
            _mediaCache.Remove(id);
        }

        return Task.FromResult(toRemove.Count);
    }

    public Task<MediaStorageStats> GetStorageStatisticsAsync()
    {
        return Task.FromResult(new MediaStorageStats
        {
            TotalFiles = _mediaCache.Count,
            TotalSize = _mediaCache.Values.Sum(m => m.FileSize),
            FilesByType = _mediaCache.Values
                .GroupBy(m => m.Type)
                .ToDictionary(g => g.Key, g => g.Count()),
            SizeByType = _mediaCache.Values
                .GroupBy(m => m.Type)
                .ToDictionary(g => g.Key, g => g.Sum(m => m.FileSize)),
            LastUpdated = DateTime.UtcNow
        });
    }

    public Task<Media> ProcessMediaAsync(string mediaId, MediaProcessingOptions options)
    {
        if (_mediaCache.TryGetValue(mediaId, out var media))
        {
            // Full implementation would resize/compress/convert
            // For now, return as-is
            return Task.FromResult(media);
        }
        throw new ArgumentException($"Media with ID {mediaId} not found", nameof(mediaId));
    }

    public async Task<ValidationResult> ValidateFileAsync(Stream fileStream, string fileName, string mimeType)
    {
        var errors = new List<string>();

        // Basic validation
        if (fileStream.Length > 50 * 1024 * 1024) // 50MB global limit
        {
            errors.Add("File size exceeds 50MB limit");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            errors.Add("File name is required");
        }

        return await Task.FromResult(new ValidationResult(errors)).ConfigureAwait(false);
    }

    #region Helper Methods

    private static MediaType GetMediaTypeFromMimeType(string mimeType)
    {
        return mimeType.ToLowerInvariant() switch
        {
            var mime when mime.StartsWith("image/") => MediaType.Image,
            var mime when mime.StartsWith("video/") => MediaType.Video,
            var mime when mime.StartsWith("audio/") => MediaType.Audio,
            _ => MediaType.Document
        };
    }

    private static MediaType GetMediaTypeFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" => MediaType.Image,
            ".mp4" or ".mov" or ".avi" or ".mkv" or ".webm" => MediaType.Video,
            ".mp3" or ".wav" or ".aac" or ".flac" or ".ogg" => MediaType.Audio,
            _ => MediaType.Document
        };
    }

    private static string GetMimeTypeFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            ".webm" => "video/webm",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".aac" => "audio/aac",
            ".flac" => "audio/flac",
            ".ogg" => "audio/ogg",
            _ => "application/octet-stream"
        };
    }

    #endregion
}
