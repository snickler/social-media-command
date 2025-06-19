using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Desktop.Services;

/// <summary>
/// Mock implementation of IMediaService for development and testing
/// Optimized for performance following Microsoft Docs best practices
/// </summary>
public class MockMediaService : IMediaService
{
    private readonly Dictionary<string, Media> _mediaStore = new();
    private readonly Random _random = new();

    public Task<Media> UploadMediaAsync(Stream fileStream, string fileName, string mimeType)
    {
        var media = new Media
        {
            Id = Guid.NewGuid().ToString(),
            FileName = fileName,
            MimeType = mimeType,
            FileSize = fileStream.Length,
            Type = GetMediaTypeFromMimeType(mimeType),
            FilePath = $"mock://uploads/{fileName}",
            PreviewUrl = $"mock://previews/{Guid.NewGuid()}",
            CreatedAt = DateTime.UtcNow
        };

        _mediaStore[media.Id] = media;
        return Task.FromResult(media);
    }

    public async Task<Media> UploadMediaAsync(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var mimeType = GetMimeTypeFromExtension(Path.GetExtension(filePath));
        
        await using var fileStream = File.OpenRead(filePath);
        return await UploadMediaAsync(fileStream, fileName, mimeType).ConfigureAwait(false);
    }

    public Task<Media?> GetMediaByIdAsync(string id)
    {
        _mediaStore.TryGetValue(id, out var media);
        return Task.FromResult(media);
    }

    public Task<IEnumerable<Media>> GetMediaByIdsAsync(IEnumerable<string> ids)
    {
        var medias = ids.Select(id => _mediaStore.GetValueOrDefault(id))
                       .Where(m => m != null)
                       .Cast<Media>();
        return Task.FromResult(medias);
    }

    public Task<bool> DeleteMediaAsync(string id)
    {
        return Task.FromResult(_mediaStore.Remove(id));
    }

    public Task<string?> GeneratePreviewAsync(string mediaId)
    {
        return Task.FromResult(_mediaStore.ContainsKey(mediaId) 
            ? $"https://example.com/previews/{mediaId}" 
            : null);
    }

    public Task<Stream?> GetMediaStreamAsync(string id)
    {
        if (!_mediaStore.ContainsKey(id))
            return Task.FromResult<Stream?>(null);

        // Return empty stream for mock
        return Task.FromResult<Stream?>(new MemoryStream());
    }

    public Task<ValidationResult> ValidateMediaForPlatformAsync(string mediaId, SocialPlatform platform)
    {
        return Task.FromResult(new ValidationResult());
    }

    public Task<Media> OptimizeMediaForPlatformAsync(string mediaId, SocialPlatform platform)
    {
        if (_mediaStore.TryGetValue(mediaId, out var media))
        {
            return Task.FromResult(media);
        }
        throw new ArgumentException($"Media with ID {mediaId} not found", nameof(mediaId));
    }

    public Task<MediaFormats> GetSupportedFormatsAsync(SocialPlatform platform)
    {
        return Task.FromResult(new MediaFormats
        {
            SupportedImageFormats = new List<string> { "jpg", "png", "gif", "webp" },
            SupportedVideoFormats = new List<string> { "mp4", "mov", "avi" },
            MaxFileSize = 10 * 1024 * 1024, // 10MB
            MaxDuration = TimeSpan.FromMinutes(5),
            SupportsGifs = true
        });
    }

    public Task<int> CleanupUnusedMediaAsync(TimeSpan olderThan)
    {
        var cutoffDate = DateTime.UtcNow - olderThan;
        var toRemove = _mediaStore.Where(kvp => kvp.Value.CreatedAt < cutoffDate)
                                 .Select(kvp => kvp.Key)
                                 .ToList();
        
        foreach (var id in toRemove)
        {
            _mediaStore.Remove(id);
        }
        
        return Task.FromResult(toRemove.Count);
    }

    public Task<MediaStorageStats> GetStorageStatisticsAsync()
    {
        return Task.FromResult(new MediaStorageStats
        {
            TotalFiles = _mediaStore.Count,
            TotalSize = _mediaStore.Values.Sum(m => m.FileSize),
            FilesByType = _mediaStore.Values
                .GroupBy(m => m.Type)
                .ToDictionary(g => g.Key, g => g.Count()),
            SizeByType = _mediaStore.Values
                .GroupBy(m => m.Type)
                .ToDictionary(g => g.Key, g => g.Sum(m => m.FileSize))
        });
    }

    public Task<Media> ProcessMediaAsync(string mediaId, MediaProcessingOptions options)
    {
        if (_mediaStore.TryGetValue(mediaId, out var media))
        {
            // Mock processing - return the same media
            return Task.FromResult(media);
        }
        throw new ArgumentException($"Media with ID {mediaId} not found", nameof(mediaId));
    }

    public Task<ValidationResult> ValidateFileAsync(Stream fileStream, string fileName, string mimeType)
    {
        var errors = new List<string>();
        
        // Mock validation
        if (fileStream.Length > 50 * 1024 * 1024) // 50MB limit
        {
            errors.Add("File size exceeds 50MB limit");
        }
        
        return Task.FromResult(new ValidationResult(errors));
    }

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
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            _ => "application/octet-stream"
        };
    }
} 