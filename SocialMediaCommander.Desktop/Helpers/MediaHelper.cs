using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Desktop.Helpers;

/// <summary>
/// Helper class for media file operations shared across ViewModels
/// </summary>
public static class MediaHelper
{
    /// <summary>
    /// Determines the media type based on file extension
    /// </summary>
    /// <param name="filePath">Path to the media file</param>
    /// <returns>The MediaType enum value</returns>
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
    /// Gets the MIME type based on file extension
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

    /// <summary>
    /// Creates a Media object from a file path
    /// </summary>
    /// <param name="filePath">Path to the media file</param>
    /// <returns>A new Media object with file information</returns>
    public static Media CreateMediaFromFile(string filePath)
    {
        var fileName = System.IO.Path.GetFileName(filePath);
        var fileInfo = new System.IO.FileInfo(filePath);

        return new Media
        {
            Id = Guid.NewGuid().ToString(),
            FileName = fileName,
            FilePath = filePath,
            Type = GetMediaType(filePath),
            FileSize = fileInfo.Exists ? fileInfo.Length : 0,
            MimeType = GetMimeType(filePath),
            CreatedAt = DateTime.UtcNow
        };
    }
}
