using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Social Media Commander";
    
    public PostEditorViewModel PostEditor { get; }
    public SocialFeedViewModel SocialFeed { get; }
    
    public MainWindowViewModel()
    {
        // Initialize with mock services for now
        var postService = new MockPostService();
        var accountService = new InMemoryAccountService();
        var mediaService = new MockMediaService();
        var feedService = new InMemoryFeedService();
        
        PostEditor = new PostEditorViewModel(postService, accountService, mediaService);
        SocialFeed = new SocialFeedViewModel(feedService, accountService);
    }
}

// Mock media service for now
public class MockMediaService : IMediaService
{
    public Task<Media> UploadMediaAsync(Stream fileStream, string fileName, string mimeType)
    {
        var media = new Media
        {
            Id = Guid.NewGuid().ToString(),
            FileName = fileName,
            FilePath = $"mock://uploads/{fileName}",
            MimeType = mimeType,
            Type = GetMediaTypeFromMimeType(mimeType),
            FileSize = fileStream.Length,
            PreviewUrl = $"mock://previews/{fileName}"
        };
        return Task.FromResult(media);
    }
    
    public Task<Media> UploadMediaAsync(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var media = new Media
        {
            Id = Guid.NewGuid().ToString(),
            FileName = fileName,
            FilePath = $"mock://uploads/{fileName}",
            MimeType = GetMimeTypeFromExtension(Path.GetExtension(filePath)),
            Type = GetMediaTypeFromExtension(Path.GetExtension(filePath)),
            FileSize = 1024 * 1024, // Mock 1MB
            PreviewUrl = $"mock://previews/{fileName}"
        };
        return Task.FromResult(media);
    }
    
    public Task<Media?> GetMediaByIdAsync(string id)
    {
        return Task.FromResult<Media?>(null);
    }
    
    public Task<IEnumerable<Media>> GetMediaByIdsAsync(IEnumerable<string> ids)
    {
        return Task.FromResult(Enumerable.Empty<Media>());
    }
    
    public Task<bool> DeleteMediaAsync(string id)
    {
        return Task.FromResult(true);
    }
    
    public Task<string?> GeneratePreviewAsync(string mediaId)
    {
        return Task.FromResult<string?>($"mock://previews/{mediaId}");
    }
    
    public Task<Stream?> GetMediaStreamAsync(string id)
    {
        return Task.FromResult<Stream?>(new MemoryStream());
    }
    
    public Task<ValidationResult> ValidateMediaForPlatformAsync(string mediaId, SocialPlatform platform)
    {
        return Task.FromResult(new ValidationResult());
    }
    
    public Task<Media> OptimizeMediaForPlatformAsync(string mediaId, SocialPlatform platform)
    {
        var media = new Media { Id = mediaId };
        return Task.FromResult(media);
    }
    
    public Task<MediaFormats> GetSupportedFormatsAsync(SocialPlatform platform)
    {
        return Task.FromResult(new MediaFormats());
    }
    
    public Task<int> CleanupUnusedMediaAsync(TimeSpan olderThan)
    {
        return Task.FromResult(0);
    }
    
    public Task<MediaStorageStats> GetStorageStatisticsAsync()
    {
        return Task.FromResult(new MediaStorageStats());
    }
    
    public Task<Media> ProcessMediaAsync(string mediaId, MediaProcessingOptions options)
    {
        var media = new Media { Id = mediaId };
        return Task.FromResult(media);
    }
    
    public Task<ValidationResult> ValidateFileAsync(Stream fileStream, string fileName, string mimeType)
    {
        return Task.FromResult(new ValidationResult());
    }
    
    private static MediaType GetMediaTypeFromMimeType(string mimeType)
    {
        return mimeType.StartsWith("image/") ? MediaType.Image :
               mimeType.StartsWith("video/") ? MediaType.Video :
               MediaType.Image;
    }
    
    private static MediaType GetMediaTypeFromExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" => MediaType.Image,
            ".mp4" or ".avi" or ".mov" or ".webm" => MediaType.Video,
            _ => MediaType.Image
        };
    }
    
    private static string GetMimeTypeFromExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".avi" => "video/avi",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            _ => "application/octet-stream"
        };
    }
}
