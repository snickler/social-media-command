using System;
using System.Collections.ObjectModel;
using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Desktop.Helpers;

/// <summary>
/// Helper class for media file operations shared across view models
/// </summary>
public static class MediaHelper
{
    /// <summary>
    /// Adds a media file to the specified media collection if it hasn't reached the maximum limit
    /// </summary>
    /// <param name="media">The media collection to add to</param>
    /// <param name="filePath">Path to the media file</param>
    /// <param name="maxCount">Maximum number of media files allowed (default: 4)</param>
    /// <returns>True if the media was added successfully, false if the limit was reached</returns>
    public static bool AddMediaFile(ObservableCollection<Media> media, string filePath, int maxCount = 4)
    {
        if (media.Count >= maxCount)
        {
            // Max media files per post reached
            return false;
        }

        var fileName = System.IO.Path.GetFileName(filePath);
        var fileInfo = new System.IO.FileInfo(filePath);

        var mediaItem = new Media
        {
            Id = Guid.NewGuid().ToString(),
            FileName = fileName,
            FilePath = filePath,
            Type = GetMediaType(filePath),
            FileSize = fileInfo.Exists ? fileInfo.Length : 0,
            MimeType = GetMimeType(filePath),
            CreatedAt = DateTime.UtcNow
        };

        media.Add(mediaItem);
        return true;
    }

    /// <summary>
    /// Determines the media type based on file extension
    /// </summary>
    /// <param name="filePath">Path to the media file</param>
    /// <returns>The determined media type</returns>
    public static MediaType GetMediaType(string filePath)
    {
        var extension = System.IO.Path.GetExtension(filePath)?.ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp" => MediaType.Image,
            ".gif" => MediaType.Gif,
            ".mp4" or ".mov" or ".avi" or ".webm" or ".mkv" => MediaType.Video,
            _ => MediaType.Image
        };
    }

    /// <summary>
    /// Determines the MIME type based on file extension
    /// </summary>
    /// <param name="filePath">Path to the media file</param>
    /// <returns>The MIME type string</returns>
    public static string GetMimeType(string filePath)
    {
        var extension = System.IO.Path.GetExtension(filePath)?.ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".webm" => "video/webm",
            ".mkv" => "video/x-matroska",
            _ => "application/octet-stream"
        };
    }
}
