using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Desktop.ViewModels;

/// <summary>
/// ViewModel for media upload and management functionality
/// </summary>
public partial class MediaUploadViewModel : ObservableObject
{
    private readonly IMediaService _mediaService;

    [ObservableProperty]
    private bool _isUploading = false;

    [ObservableProperty]
    private double _uploadProgress = 0;

    [ObservableProperty]
    private string _totalStorageUsed = "0 MB";

    public MediaUploadViewModel(IMediaService mediaService)
    {
        _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));

        MediaFiles = new ObservableCollection<MediaFileItem>();

        // Initialize with some mock data
        LoadMockData();
        UpdateStorageInfo();
    }

    #region Properties

    public ObservableCollection<MediaFileItem> MediaFiles { get; }
    public ObservableCollection<Media> MediaItems { get; } = new();

    public bool HasMedia => MediaItems.Any();
    public bool CanAddMore => MediaItems.Count < 4;
    public string MediaCountText => $"{MediaItems.Count} of 4 media files";

    #endregion

    #region Commands

    [RelayCommand]
    private async Task UploadAsync()
    {
        try
        {
            if (MediaItems.Count >= 4) return;

            // In a real implementation, this would open a file dialog
            // For now, simulate file selection
            await SimulateMediaUploadAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Upload failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RemoveMedia(Media media)
    {
        if (MediaItems.Contains(media))
        {
            MediaItems.Remove(media);
            OnPropertyChanged(nameof(HasMedia));
            OnPropertyChanged(nameof(CanAddMore));
            OnPropertyChanged(nameof(MediaCountText));
            System.Diagnostics.Debug.WriteLine($"Removed media: {media.FileName}");
        }
    }

    [RelayCommand]
    private async Task BrowseFilesAsync()
    {
        try
        {
            // In a real implementation, this would open a file dialog
            // For now, simulate file selection
            await SimulateFileUploadAsync("example-image.jpg", "📷", "JPG", "2.5 MB");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Browse files failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        MediaFiles.Clear();
        UpdateStorageInfo();
        OnPropertyChanged(nameof(HasMedia));
    }

    [RelayCommand]
    private async Task OptimizeAllAsync()
    {
        try
        {
            IsUploading = true;
            UploadProgress = 0;

            // Simulate optimization process
            for (int i = 0; i <= 100; i += 10)
            {
                UploadProgress = i;
                await Task.Delay(200);
            }

            // Update file sizes after optimization
            foreach (var file in MediaFiles)
            {
                file.FileSize = $"{Random.Shared.NextDouble() * 2 + 0.5:F1} MB";
            }

            UpdateStorageInfo();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Optimize all failed: {ex.Message}");
        }
        finally
        {
            IsUploading = false;
            UploadProgress = 0;
        }
    }

    [RelayCommand]
    private void RemoveFile(MediaFileItem file)
    {
        try
        {
            MediaFiles.Remove(file);
            UpdateStorageInfo();
            OnPropertyChanged(nameof(HasMedia));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Remove file failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void PreviewFile(MediaFileItem file)
    {
        try
        {
            // In a real implementation, this would open a preview window
            System.Diagnostics.Debug.WriteLine($"Previewing file: {file.FileName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Preview file failed: {ex.Message}");
        }
    }

    #endregion

    #region Helper Methods

    private async Task SimulateMediaUploadAsync()
    {
        try
        {
            if (MediaItems.Count >= 4) return;

            // Simulate async upload delay
            await Task.Delay(100);

            var mediaTypes = new[] { MediaType.Image, MediaType.Video };
            var mediaType = mediaTypes[Random.Shared.Next(mediaTypes.Length)];
            var fileName = mediaType == MediaType.Image ? $"image_{MediaItems.Count + 1}.jpg" : $"video_{MediaItems.Count + 1}.mp4";

            var media = new Media
            {
                Id = Guid.NewGuid().ToString(),
                FileName = fileName,
                FilePath = $"mock://path/{fileName}",
                Type = mediaType,
                MimeType = mediaType == MediaType.Image ? "image/jpeg" : "video/mp4",
                FileSize = Random.Shared.Next(1024 * 100, 1024 * 1024 * 10), // 100KB to 10MB
                PreviewUrl = $"mock://preview/{fileName}"
            };

            MediaItems.Add(media);
            OnPropertyChanged(nameof(HasMedia));
            OnPropertyChanged(nameof(CanAddMore));
            OnPropertyChanged(nameof(MediaCountText));

            System.Diagnostics.Debug.WriteLine($"Added media: {media.FileName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Simulate media upload failed: {ex.Message}");
        }
    }

    private async Task SimulateFileUploadAsync(string fileName, string icon, string extension, string size)
    {
        try
        {
            IsUploading = true;
            UploadProgress = 0;

            // Simulate upload progress
            for (int i = 0; i <= 100; i += 20)
            {
                UploadProgress = i;
                await Task.Delay(300);
            }

            // Add the new file
            var mediaFile = new MediaFileItem
            {
                Id = Guid.NewGuid().ToString(),
                FileName = fileName,
                FileTypeIcon = icon,
                FileExtension = extension,
                FileSize = size,
                UploadDate = DateTime.Now
            };

            MediaFiles.Add(mediaFile);
            UpdateStorageInfo();
            OnPropertyChanged(nameof(HasMedia));
        }
        finally
        {
            IsUploading = false;
            UploadProgress = 0;
        }
    }

    private void LoadMockData()
    {
        var mockFiles = new[]
        {
            new MediaFileItem
            {
                Id = "1",
                FileName = "social-media-banner.jpg",
                FileTypeIcon = "📷",
                FileExtension = "JPG",
                FileSize = "3.2 MB",
                UploadDate = DateTime.Now.AddDays(-2)
            },
            new MediaFileItem
            {
                Id = "2",
                FileName = "product-demo.mp4",
                FileTypeIcon = "🎥",
                FileExtension = "MP4",
                FileSize = "15.7 MB",
                UploadDate = DateTime.Now.AddDays(-5)
            },
            new MediaFileItem
            {
                Id = "3",
                FileName = "background-music.mp3",
                FileTypeIcon = "🎵",
                FileExtension = "MP3",
                FileSize = "4.1 MB",
                UploadDate = DateTime.Now.AddDays(-1)
            },
            new MediaFileItem
            {
                Id = "4",
                FileName = "company-logo.png",
                FileTypeIcon = "📷",
                FileExtension = "PNG",
                FileSize = "1.8 MB",
                UploadDate = DateTime.Now.AddHours(-3)
            }
        };

        foreach (var file in mockFiles)
        {
            MediaFiles.Add(file);
        }
    }

    private void UpdateStorageInfo()
    {
        // Calculate total storage used
        var totalMB = MediaFiles.Count * 5.2; // Approximate
        TotalStorageUsed = $"{totalMB:F1} MB used";
    }

    #endregion
}

/// <summary>
/// Represents a media file item in the upload view
/// </summary>
public partial class MediaFileItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _fileTypeIcon = "📄";

    [ObservableProperty]
    private string _fileExtension = string.Empty;

    [ObservableProperty]
    private string _fileSize = string.Empty;

    [ObservableProperty]
    private DateTime _uploadDate = DateTime.Now;
}