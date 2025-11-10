using Xunit;
using SocialMediaCommander.Services.Interfaces;
using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Tests.UnitTests;

public class MediaServiceInterfaceModelsTests
{
    [Fact]
    public void MediaFormats_ShouldInitializeWithDefaults()
    {
        // Act
        var mediaFormats = new MediaFormats();

        // Assert
        Assert.NotNull(mediaFormats.SupportedImageFormats);
        Assert.Empty(mediaFormats.SupportedImageFormats);
        Assert.NotNull(mediaFormats.SupportedVideoFormats);
        Assert.Empty(mediaFormats.SupportedVideoFormats);
        Assert.Equal(0, mediaFormats.MaxFileSize);
        Assert.Equal(0, mediaFormats.MaxWidth);
        Assert.Equal(0, mediaFormats.MaxHeight);
        Assert.Equal(default(TimeSpan), mediaFormats.MaxDuration);
        Assert.False(mediaFormats.SupportsGifs);
    }

    [Fact]
    public void MediaFormats_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var imageFormats = new List<string> { "jpg", "png", "webp" };
        var videoFormats = new List<string> { "mp4", "mov", "avi" };
        var maxDuration = TimeSpan.FromMinutes(5);

        // Act
        var mediaFormats = new MediaFormats
        {
            SupportedImageFormats = imageFormats,
            SupportedVideoFormats = videoFormats,
            MaxFileSize = 10485760, // 10MB
            MaxWidth = 1920,
            MaxHeight = 1080,
            MaxDuration = maxDuration,
            SupportsGifs = true
        };

        // Assert
        Assert.Equal(imageFormats, mediaFormats.SupportedImageFormats);
        Assert.Equal(videoFormats, mediaFormats.SupportedVideoFormats);
        Assert.Equal(10485760, mediaFormats.MaxFileSize);
        Assert.Equal(1920, mediaFormats.MaxWidth);
        Assert.Equal(1080, mediaFormats.MaxHeight);
        Assert.Equal(maxDuration, mediaFormats.MaxDuration);
        Assert.True(mediaFormats.SupportsGifs);
    }

    [Fact]
    public void MediaFormats_ShouldSupportEmptyFormatsLists()
    {
        // Act
        var mediaFormats = new MediaFormats
        {
            SupportedImageFormats = new List<string>(),
            SupportedVideoFormats = new List<string>()
        };

        // Assert
        Assert.NotNull(mediaFormats.SupportedImageFormats);
        Assert.Empty(mediaFormats.SupportedImageFormats);
        Assert.NotNull(mediaFormats.SupportedVideoFormats);
        Assert.Empty(mediaFormats.SupportedVideoFormats);
    }

    [Fact]
    public void MediaFormats_ShouldSupportLargeFileSizes()
    {
        // Act
        var mediaFormats = new MediaFormats
        {
            MaxFileSize = long.MaxValue
        };

        // Assert
        Assert.Equal(long.MaxValue, mediaFormats.MaxFileSize);
    }

    [Fact]
    public void MediaStorageStats_ShouldInitializeWithDefaults()
    {
        // Act
        var stats = new MediaStorageStats();

        // Assert
        Assert.Equal(0, stats.TotalSize);
        Assert.Equal(0, stats.TotalFiles);
        Assert.NotNull(stats.FilesByType);
        Assert.Empty(stats.FilesByType);
        Assert.NotNull(stats.SizeByType);
        Assert.Empty(stats.SizeByType);
        Assert.True(stats.LastUpdated <= DateTime.UtcNow);
    }

    [Fact]
    public void MediaStorageStats_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var filesByType = new Dictionary<MediaType, int>
        {
            { MediaType.Image, 100 },
            { MediaType.Video, 25 }
        };
        var sizeByType = new Dictionary<MediaType, long>
        {
            { MediaType.Image, 52428800 }, // 50MB
            { MediaType.Video, 157286400 } // 150MB
        };
        var lastUpdated = DateTime.UtcNow;

        // Act
        var stats = new MediaStorageStats
        {
            TotalSize = 209715200, // 200MB
            TotalFiles = 125,
            FilesByType = filesByType,
            SizeByType = sizeByType,
            LastUpdated = lastUpdated
        };

        // Assert
        Assert.Equal(209715200, stats.TotalSize);
        Assert.Equal(125, stats.TotalFiles);
        Assert.Equal(filesByType, stats.FilesByType);
        Assert.Equal(sizeByType, stats.SizeByType);
        Assert.Equal(lastUpdated, stats.LastUpdated);
    }

    [Fact]
    public void MediaStorageStats_ShouldCalculateStatisticsCorrectly()
    {
        // Arrange
        var stats = new MediaStorageStats
        {
            FilesByType = new Dictionary<MediaType, int>
            {
                { MediaType.Image, 50 },
                { MediaType.Video, 10 },
                { MediaType.Audio, 5 }
            },
            SizeByType = new Dictionary<MediaType, long>
            {
                { MediaType.Image, 100_000_000 }, // 100MB
                { MediaType.Video, 500_000_000 }, // 500MB
                { MediaType.Audio, 50_000_000 }   // 50MB
            }
        };

        // Act
        var totalFiles = stats.FilesByType.Values.Sum();
        var totalSize = stats.SizeByType.Values.Sum();

        // Assert
        Assert.Equal(65, totalFiles);
        Assert.Equal(650_000_000, totalSize);
    }

    [Fact]
    public void MediaStorageStats_ShouldHandleEmptyCollections()
    {
        // Act
        var stats = new MediaStorageStats
        {
            FilesByType = new Dictionary<MediaType, int>(),
            SizeByType = new Dictionary<MediaType, long>()
        };

        // Assert
        Assert.NotNull(stats.FilesByType);
        Assert.Empty(stats.FilesByType);
        Assert.NotNull(stats.SizeByType);
        Assert.Empty(stats.SizeByType);
    }

    [Fact]
    public void MediaProcessingOptions_ShouldInitializeWithDefaults()
    {
        // Act
        var options = new MediaProcessingOptions();

        // Assert
        Assert.Null(options.MaxWidth);
        Assert.Null(options.MaxHeight);
        Assert.Null(options.Quality);
        Assert.Null(options.OutputFormat);
        Assert.True(options.GenerateThumbnail);
        Assert.Null(options.MaxDuration);
        Assert.True(options.OptimizeForWeb);
    }

    [Fact]
    public void MediaProcessingOptions_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var maxDuration = TimeSpan.FromMinutes(2);

        // Act
        var options = new MediaProcessingOptions
        {
            MaxWidth = 1280,
            MaxHeight = 720,
            Quality = 85,
            OutputFormat = "webp",
            GenerateThumbnail = false,
            MaxDuration = maxDuration,
            OptimizeForWeb = false
        };

        // Assert
        Assert.Equal(1280, options.MaxWidth);
        Assert.Equal(720, options.MaxHeight);
        Assert.Equal(85, options.Quality);
        Assert.Equal("webp", options.OutputFormat);
        Assert.False(options.GenerateThumbnail);
        Assert.Equal(maxDuration, options.MaxDuration);
        Assert.False(options.OptimizeForWeb);
    }

    [Fact]
    public void MediaProcessingOptions_ShouldSupportNullableValues()
    {
        // Act
        var options = new MediaProcessingOptions
        {
            MaxWidth = null,
            MaxHeight = null,
            Quality = null,
            OutputFormat = null,
            MaxDuration = null
        };

        // Assert
        Assert.Null(options.MaxWidth);
        Assert.Null(options.MaxHeight);
        Assert.Null(options.Quality);
        Assert.Null(options.OutputFormat);
        Assert.Null(options.MaxDuration);
    }

    [Fact]
    public void MediaProcessingOptions_ShouldAcceptValidQualityRange()
    {
        // Arrange & Act
        var lowQuality = new MediaProcessingOptions { Quality = 1 };
        var midQuality = new MediaProcessingOptions { Quality = 50 };
        var highQuality = new MediaProcessingOptions { Quality = 100 };

        // Assert
        Assert.Equal(1, lowQuality.Quality);
        Assert.Equal(50, midQuality.Quality);
        Assert.Equal(100, highQuality.Quality);
    }

    [Fact]
    public void MediaProcessingOptions_ShouldAcceptValidDimensions()
    {
        // Act
        var options = new MediaProcessingOptions
        {
            MaxWidth = 4096,
            MaxHeight = 4096
        };

        // Assert
        Assert.Equal(4096, options.MaxWidth);
        Assert.Equal(4096, options.MaxHeight);
    }

    [Fact]
    public void MediaProcessingOptions_ShouldSupportCommonVideoFormats()
    {
        // Arrange
        var formats = new[] { "mp4", "avi", "mov", "wmv", "flv", "webm" };

        foreach (var format in formats)
        {
            // Act
            var options = new MediaProcessingOptions
            {
                OutputFormat = format
            };

            // Assert
            Assert.Equal(format, options.OutputFormat);
        }
    }

    [Fact]
    public void MediaProcessingOptions_ShouldSupportCommonImageFormats()
    {
        // Arrange
        var formats = new[] { "jpg", "jpeg", "png", "gif", "webp", "bmp", "tiff" };

        foreach (var format in formats)
        {
            // Act
            var options = new MediaProcessingOptions
            {
                OutputFormat = format
            };

            // Assert
            Assert.Equal(format, options.OutputFormat);
        }
    }

    [Fact]
    public void MediaProcessingOptions_ShouldSupportVariousDurations()
    {
        // Arrange
        var shortDuration = TimeSpan.FromSeconds(30);
        var mediumDuration = TimeSpan.FromMinutes(5);
        var longDuration = TimeSpan.FromHours(2);

        // Act
        var shortOptions = new MediaProcessingOptions { MaxDuration = shortDuration };
        var mediumOptions = new MediaProcessingOptions { MaxDuration = mediumDuration };
        var longOptions = new MediaProcessingOptions { MaxDuration = longDuration };

        // Assert
        Assert.Equal(shortDuration, shortOptions.MaxDuration);
        Assert.Equal(mediumDuration, mediumOptions.MaxDuration);
        Assert.Equal(longDuration, longOptions.MaxDuration);
    }
}
