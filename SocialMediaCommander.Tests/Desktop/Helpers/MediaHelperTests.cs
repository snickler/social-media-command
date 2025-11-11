using System;
using System.Collections.ObjectModel;
using System.IO;
using FluentAssertions;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Desktop.Helpers;
using Xunit;

namespace SocialMediaCommander.Tests.Desktop.Helpers;

public class MediaHelperTests
{
    [Fact]
    public void AddMediaFile_ShouldAddMediaToCollection_WhenBelowMaxCount()
    {
        // Arrange
        var media = new ObservableCollection<Media>();
        var testFilePath = Path.Combine(Path.GetTempPath(), "test.jpg");

        // Create a temporary test file
        File.WriteAllText(testFilePath, "test content");

        try
        {
            // Act
            var result = MediaHelper.AddMediaFile(media, testFilePath);

            // Assert
            result.Should().BeTrue();
            media.Should().HaveCount(1);
            media[0].FileName.Should().Be("test.jpg");
            media[0].FilePath.Should().Be(testFilePath);
            media[0].Type.Should().Be(MediaType.Image);
            media[0].MimeType.Should().Be("image/jpeg");
            media[0].FileSize.Should().BeGreaterThan(0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }
        }
    }

    [Fact]
    public void AddMediaFile_ShouldReturnFalse_WhenMaxCountReached()
    {
        // Arrange
        var media = new ObservableCollection<Media>();
        for (int i = 0; i < 4; i++)
        {
            media.Add(new Media
            {
                Id = Guid.NewGuid().ToString(),
                FileName = $"test{i}.jpg",
                FilePath = $"/path/to/test{i}.jpg"
            });
        }

        // Act
        var result = MediaHelper.AddMediaFile(media, "/path/to/test5.jpg");

        // Assert
        result.Should().BeFalse();
        media.Should().HaveCount(4); // Should not add the 5th item
    }

    [Fact]
    public void AddMediaFile_ShouldRespectCustomMaxCount()
    {
        // Arrange
        var media = new ObservableCollection<Media>();
        var testFilePath1 = Path.Combine(Path.GetTempPath(), "test1.jpg");
        var testFilePath2 = Path.Combine(Path.GetTempPath(), "test2.jpg");
        var testFilePath3 = Path.Combine(Path.GetTempPath(), "test3.jpg");

        File.WriteAllText(testFilePath1, "test");
        File.WriteAllText(testFilePath2, "test");
        File.WriteAllText(testFilePath3, "test");

        try
        {
            // Act
            var result1 = MediaHelper.AddMediaFile(media, testFilePath1, maxCount: 2);
            var result2 = MediaHelper.AddMediaFile(media, testFilePath2, maxCount: 2);
            var result3 = MediaHelper.AddMediaFile(media, testFilePath3, maxCount: 2);

            // Assert
            result1.Should().BeTrue();
            result2.Should().BeTrue();
            result3.Should().BeFalse();
            media.Should().HaveCount(2);
        }
        finally
        {
            // Cleanup
            File.Delete(testFilePath1);
            File.Delete(testFilePath2);
            File.Delete(testFilePath3);
        }
    }

    [Theory]
    [InlineData(".jpg", MediaType.Image)]
    [InlineData(".jpeg", MediaType.Image)]
    [InlineData(".png", MediaType.Image)]
    [InlineData(".webp", MediaType.Image)]
    [InlineData(".bmp", MediaType.Image)]
    [InlineData(".gif", MediaType.Gif)]
    [InlineData(".mp4", MediaType.Video)]
    [InlineData(".mov", MediaType.Video)]
    [InlineData(".avi", MediaType.Video)]
    [InlineData(".webm", MediaType.Video)]
    [InlineData(".mkv", MediaType.Video)]
    [InlineData(".unknown", MediaType.Image)] // Default case
    public void GetMediaType_ShouldReturnCorrectType_ForFileExtension(string extension, MediaType expectedType)
    {
        // Arrange
        var filePath = $"/path/to/file{extension}";

        // Act
        var result = MediaHelper.GetMediaType(filePath);

        // Assert
        result.Should().Be(expectedType);
    }

    [Theory]
    [InlineData(".JPG", MediaType.Image)] // Test case-insensitivity
    [InlineData(".JPEG", MediaType.Image)]
    [InlineData(".PNG", MediaType.Image)]
    public void GetMediaType_ShouldBeCaseInsensitive(string extension, MediaType expectedType)
    {
        // Arrange
        var filePath = $"/path/to/file{extension}";

        // Act
        var result = MediaHelper.GetMediaType(filePath);

        // Assert
        result.Should().Be(expectedType);
    }

    [Theory]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".jpeg", "image/jpeg")]
    [InlineData(".png", "image/png")]
    [InlineData(".gif", "image/gif")]
    [InlineData(".webp", "image/webp")]
    [InlineData(".bmp", "image/bmp")]
    [InlineData(".mp4", "video/mp4")]
    [InlineData(".mov", "video/quicktime")]
    [InlineData(".avi", "video/x-msvideo")]
    [InlineData(".webm", "video/webm")]
    [InlineData(".mkv", "video/x-matroska")]
    [InlineData(".unknown", "application/octet-stream")] // Default case
    public void GetMimeType_ShouldReturnCorrectMimeType_ForFileExtension(string extension, string expectedMimeType)
    {
        // Arrange
        var filePath = $"/path/to/file{extension}";

        // Act
        var result = MediaHelper.GetMimeType(filePath);

        // Assert
        result.Should().Be(expectedMimeType);
    }

    [Theory]
    [InlineData(".JPG", "image/jpeg")] // Test case-insensitivity
    [InlineData(".PNG", "image/png")]
    [InlineData(".MP4", "video/mp4")]
    public void GetMimeType_ShouldBeCaseInsensitive(string extension, string expectedMimeType)
    {
        // Arrange
        var filePath = $"/path/to/file{extension}";

        // Act
        var result = MediaHelper.GetMimeType(filePath);

        // Assert
        result.Should().Be(expectedMimeType);
    }

    [Fact]
    public void AddMediaFile_ShouldHandleNonExistentFile()
    {
        // Arrange
        var media = new ObservableCollection<Media>();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_file_12345.jpg");

        // Act
        var result = MediaHelper.AddMediaFile(media, nonExistentPath);

        // Assert
        result.Should().BeTrue(); // Should still add the media
        media.Should().HaveCount(1);
        media[0].FileSize.Should().Be(0); // File doesn't exist, so size is 0
    }
}
