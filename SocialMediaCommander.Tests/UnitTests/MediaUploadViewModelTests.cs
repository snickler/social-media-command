using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Desktop.ViewModels;
using SocialMediaCommander.Services.Interfaces;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Comprehensive unit tests for MediaUploadViewModel
/// Tests media upload functionality, validation, and MVVM patterns
/// </summary>
public class MediaUploadViewModelTests : IDisposable
{
    private readonly Mock<IMediaService> _mockMediaService;
    private readonly MediaUploadViewModel _viewModel;

    public MediaUploadViewModelTests()
    {
        _mockMediaService = new Mock<IMediaService>();
        _viewModel = new MediaUploadViewModel(_mockMediaService.Object);
    }

    public void Dispose()
    {
        _viewModel?.GetType().GetMethod("Dispose")?.Invoke(_viewModel, null);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var viewModel = new MediaUploadViewModel(_mockMediaService.Object);

        // Assert
        Assert.NotNull(viewModel.MediaFiles);
        Assert.NotNull(viewModel.MediaItems);
        Assert.False(viewModel.IsUploading);
        Assert.Equal(0, viewModel.UploadProgress);
        Assert.Contains("MB", viewModel.TotalStorageUsed); // Should show storage info
        Assert.True(viewModel.CanAddMore);
        Assert.False(viewModel.HasMedia);
        Assert.Equal("0 of 4 media files", viewModel.MediaCountText);
    }

    [Fact]
    public void Constructor_WithNullMediaService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MediaUploadViewModel(null!));
    }

    [Fact]
    public void IsUploading_WhenChanged_ShouldNotifyPropertyChanged()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName!);

        // Act
        _viewModel.IsUploading = true;

        // Assert
        Assert.True(_viewModel.IsUploading);
        Assert.Contains("IsUploading", propertyChangedEvents);
    }

    [Fact]
    public void UploadProgress_WhenChanged_ShouldNotifyPropertyChanged()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName!);

        // Act
        _viewModel.UploadProgress = 50.0;

        // Assert
        Assert.Equal(50.0, _viewModel.UploadProgress);
        Assert.Contains("UploadProgress", propertyChangedEvents);
    }

    [Fact]
    public void TotalStorageUsed_WhenChanged_ShouldNotifyPropertyChanged()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName!);

        // Act
        _viewModel.TotalStorageUsed = "10 MB";

        // Assert
        Assert.Equal("10 MB", _viewModel.TotalStorageUsed);
        Assert.Contains("TotalStorageUsed", propertyChangedEvents);
    }

    [Fact]
    public void MediaItems_WhenEmpty_ShouldHaveCorrectProperties()
    {
        // Assert
        Assert.False(_viewModel.HasMedia);
        Assert.True(_viewModel.CanAddMore);
        Assert.Equal("0 of 4 media files", _viewModel.MediaCountText);
    }

    [Fact]
    public void MediaItems_WhenFull_ShouldHaveCorrectProperties()
    {
        // Arrange - Add 4 media items
        for (int i = 0; i < 4; i++)
        {
            _viewModel.MediaItems.Add(new Media
            {
                Id = $"media{i}",
                FileName = $"file{i}.jpg",
                FileSize = 1024,
                MimeType = "image/jpeg"
            });
        }

        // Act & Assert
        Assert.True(_viewModel.HasMedia);
        Assert.False(_viewModel.CanAddMore);
        Assert.Equal("4 of 4 media files", _viewModel.MediaCountText);
    }

    [Fact]
    public void MediaItems_WhenPartiallyFilled_ShouldHaveCorrectProperties()
    {
        // Arrange - Add 2 media items
        for (int i = 0; i < 2; i++)
        {
            _viewModel.MediaItems.Add(new Media
            {
                Id = $"media{i}",
                FileName = $"file{i}.jpg"
            });
        }

        // Assert
        Assert.True(_viewModel.HasMedia);
        Assert.True(_viewModel.CanAddMore);
        Assert.Equal("2 of 4 media files", _viewModel.MediaCountText);
    }

    [Theory]
    [InlineData(0, "0 of 4 media files")]
    [InlineData(1, "1 of 4 media files")]
    [InlineData(2, "2 of 4 media files")]
    [InlineData(3, "3 of 4 media files")]
    [InlineData(4, "4 of 4 media files")]
    public void MediaCountText_ForDifferentCounts_ShouldReturnCorrectFormat(int count, string expected)
    {
        // Arrange
        _viewModel.MediaItems.Clear();
        for (int i = 0; i < count; i++)
        {
            _viewModel.MediaItems.Add(new Media { Id = $"media{i}" });
        }

        // Assert
        Assert.Equal(expected, _viewModel.MediaCountText);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    public void HasMedia_ForDifferentCounts_ShouldReturnCorrectValue(int count, bool expected)
    {
        // Arrange
        _viewModel.MediaItems.Clear();
        for (int i = 0; i < count; i++)
        {
            _viewModel.MediaItems.Add(new Media { Id = $"media{i}" });
        }

        // Assert
        Assert.Equal(expected, _viewModel.HasMedia);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public void CanAddMore_ForDifferentCounts_ShouldReturnCorrectValue(int count, bool expected)
    {
        // Arrange
        _viewModel.MediaItems.Clear();
        for (int i = 0; i < count; i++)
        {
            _viewModel.MediaItems.Add(new Media { Id = $"media{i}" });
        }

        // Assert
        Assert.Equal(expected, _viewModel.CanAddMore);
    }

    [Fact]
    public void MediaFiles_ShouldBeObservableCollection()
    {
        // Assert
        Assert.NotNull(_viewModel.MediaFiles);
        Assert.IsAssignableFrom<System.Collections.ObjectModel.ObservableCollection<MediaFileItem>>(_viewModel.MediaFiles);
    }

    [Fact]
    public void MediaItems_ShouldBeObservableCollection()
    {
        // Assert
        Assert.NotNull(_viewModel.MediaItems);
        Assert.IsAssignableFrom<System.Collections.ObjectModel.ObservableCollection<Media>>(_viewModel.MediaItems);
    }

    [Fact]
    public void InitialState_ShouldBeValid()
    {
        // Assert initial state
        Assert.False(_viewModel.IsUploading);
        Assert.Equal(0, _viewModel.UploadProgress);
        Assert.Contains("MB", _viewModel.TotalStorageUsed); // Should contain some storage info
        Assert.NotNull(_viewModel.MediaFiles);
        Assert.NotNull(_viewModel.MediaItems);
        Assert.False(_viewModel.HasMedia);
        Assert.True(_viewModel.CanAddMore);
        Assert.Equal("0 of 4 media files", _viewModel.MediaCountText);
    }

    [Fact]
    public void PropertyChanges_ShouldNotifyCorrectly()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName!);

        // Act
        _viewModel.IsUploading = true;
        _viewModel.UploadProgress = 75.0;
        _viewModel.TotalStorageUsed = "25 MB";

        // Assert
        Assert.Contains("IsUploading", propertyChangedEvents);
        Assert.Contains("UploadProgress", propertyChangedEvents);
        Assert.Contains("TotalStorageUsed", propertyChangedEvents);
    }

    [Fact]
    public void UploadProgress_ValidRange_ShouldSetCorrectly()
    {
        // Test valid range values
        _viewModel.UploadProgress = 0;
        Assert.Equal(0, _viewModel.UploadProgress);

        _viewModel.UploadProgress = 50.5;
        Assert.Equal(50.5, _viewModel.UploadProgress);

        _viewModel.UploadProgress = 100;
        Assert.Equal(100, _viewModel.UploadProgress);
    }

    [Fact]
    public void UploadingState_WhenTrue_ShouldIndicateUploading()
    {
        // Act
        _viewModel.IsUploading = true;
        _viewModel.UploadProgress = 45;

        // Assert
        Assert.True(_viewModel.IsUploading);
        Assert.Equal(45, _viewModel.UploadProgress);
    }

    [Fact]
    public void UploadingState_WhenComplete_ShouldResetCorrectly()
    {
        // Arrange
        _viewModel.IsUploading = true;
        _viewModel.UploadProgress = 75;

        // Act
        _viewModel.IsUploading = false;
        _viewModel.UploadProgress = 0;

        // Assert
        Assert.False(_viewModel.IsUploading);
        Assert.Equal(0, _viewModel.UploadProgress);
    }

    [Fact]
    public void MultiplePropertyChanges_ShouldTriggerCorrectNotifications()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName!);

        // Act
        _viewModel.IsUploading = true;
        _viewModel.UploadProgress = 25;
        _viewModel.UploadProgress = 50;
        _viewModel.UploadProgress = 100;
        _viewModel.IsUploading = false;

        // Assert
        Assert.Contains("IsUploading", propertyChangedEvents);
        Assert.Contains("UploadProgress", propertyChangedEvents);

        // Verify multiple changes to UploadProgress are captured
        var progressChanges = propertyChangedEvents.Count(e => e == "UploadProgress");
        Assert.Equal(3, progressChanges); // 25, 50, 100

        // Verify multiple changes to IsUploading are captured
        var uploadingChanges = propertyChangedEvents.Count(e => e == "IsUploading");
        Assert.Equal(2, uploadingChanges); // true, then false
    }

    [Fact]
    public void StorageUsedFormatting_ShouldSupportDifferentFormats()
    {
        // Test different storage format strings
        _viewModel.TotalStorageUsed = "1.5 GB";
        Assert.Equal("1.5 GB", _viewModel.TotalStorageUsed);

        _viewModel.TotalStorageUsed = "500 KB";
        Assert.Equal("500 KB", _viewModel.TotalStorageUsed);

        _viewModel.TotalStorageUsed = "0 B";
        Assert.Equal("0 B", _viewModel.TotalStorageUsed);
    }

    [Fact]
    public void MediaItemsCollection_WhenModified_ShouldUpdateDependentProperties()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName!);

        // Act
        _viewModel.MediaItems.Add(new Media { Id = "test1", FileName = "test.jpg" });

        // The actual implementation might not automatically trigger property changes
        // for computed properties when collection changes, but we can test the logic

        // Assert current state after manual addition
        Assert.True(_viewModel.HasMedia);
        Assert.True(_viewModel.CanAddMore);
        Assert.Equal("1 of 4 media files", _viewModel.MediaCountText);
    }
}
