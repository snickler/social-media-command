using FluentAssertions;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using SocialMediaCommander.Services.Interfaces;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Tests for MediaService
/// </summary>
public class MediaServiceTests
{
    private MediaService CreateService()
    {
        return new MediaService();
    }

    #region UploadMediaAsync (Stream) Tests

    [Fact]
    public async Task UploadMediaAsync_Stream_ShouldCreateMedia_WhenValidInput()
    {
        // Arrange
        var sut = CreateService();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var fileName = "test.jpg";
        var mimeType = "image/jpeg";

        // Act
        var result = await sut.UploadMediaAsync(stream, fileName, mimeType);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.FileName.Should().Be(fileName);
        result.MimeType.Should().Be(mimeType);
        result.FileSize.Should().Be(stream.Length);
        result.Type.Should().Be(MediaType.Image);
    }

    [Fact]
    public async Task UploadMediaAsync_Stream_ShouldSetCreatedAt()
    {
        // Arrange
        var sut = CreateService();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var beforeUpload = DateTime.UtcNow;

        // Act
        var result = await sut.UploadMediaAsync(stream, "test.jpg", "image/jpeg");

        // Assert
        result.CreatedAt.Should().BeOnOrAfter(beforeUpload);
        result.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task UploadMediaAsync_Stream_ShouldDetectImageType_FromMimeType()
    {
        // Arrange
        var sut = CreateService();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var result = await sut.UploadMediaAsync(stream, "test.jpg", "image/jpeg");

        // Assert
        result.Type.Should().Be(MediaType.Image);
    }

    [Fact]
    public async Task UploadMediaAsync_Stream_ShouldDetectVideoType_FromMimeType()
    {
        // Arrange
        var sut = CreateService();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var result = await sut.UploadMediaAsync(stream, "test.mp4", "video/mp4");

        // Assert
        result.Type.Should().Be(MediaType.Video);
    }

    [Fact]
    public async Task UploadMediaAsync_Stream_ShouldDetectAudioType_FromMimeType()
    {
        // Arrange
        var sut = CreateService();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var result = await sut.UploadMediaAsync(stream, "test.mp3", "audio/mpeg");

        // Assert
        result.Type.Should().Be(MediaType.Audio);
    }

    #endregion

    #region UploadMediaAsync (FilePath) Tests

    [Fact]
    public async Task UploadMediaAsync_FilePath_ShouldThrow_WhenFileNotExists()
    {
        // Arrange
        var sut = CreateService();
        var filePath = "/nonexistent/file.jpg";

        // Act
        var act = () => sut.UploadMediaAsync(filePath);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task UploadMediaAsync_FilePath_ShouldCreateMedia_WhenFileExists()
    {
        // Arrange
        var sut = CreateService();
        var tempFile = Path.GetTempFileName();
        File.WriteAllBytes(tempFile, new byte[] { 1, 2, 3, 4, 5 });

        try
        {
            // Act
            var result = await sut.UploadMediaAsync(tempFile);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.FilePath.Should().Be(tempFile);
            result.FileSize.Should().Be(5);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task UploadMediaAsync_FilePath_ShouldDetectImageType_FromExtension()
    {
        // Arrange
        var sut = CreateService();
        var tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".jpg");
        File.WriteAllBytes(tempFile, new byte[] { 1, 2, 3 });

        try
        {
            // Act
            var result = await sut.UploadMediaAsync(tempFile);

            // Assert
            result.Type.Should().Be(MediaType.Image);
            result.MimeType.Should().Be("image/jpeg");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task UploadMediaAsync_FilePath_ShouldDetectVideoType_FromExtension()
    {
        // Arrange
        var sut = CreateService();
        var tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".mp4");
        File.WriteAllBytes(tempFile, new byte[] { 1, 2, 3 });

        try
        {
            // Act
            var result = await sut.UploadMediaAsync(tempFile);

            // Assert
            result.Type.Should().Be(MediaType.Video);
            result.MimeType.Should().Be("video/mp4");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region GetMediaByIdAsync Tests

    [Fact]
    public async Task GetMediaByIdAsync_ShouldReturnMedia_WhenExists()
    {
        // Arrange
        var sut = CreateService();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var uploaded = await sut.UploadMediaAsync(stream, "test.jpg", "image/jpeg");

        // Act
        var result = await sut.GetMediaByIdAsync(uploaded.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(uploaded.Id);
    }

    [Fact]
    public async Task GetMediaByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.GetMediaByIdAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetMediaByIdsAsync Tests

    [Fact]
    public async Task GetMediaByIdsAsync_ShouldReturnMatchingMedia()
    {
        // Arrange
        var sut = CreateService();
        using var stream1 = new MemoryStream(new byte[] { 1 });
        using var stream2 = new MemoryStream(new byte[] { 2 });
        using var stream3 = new MemoryStream(new byte[] { 3 });

        var media1 = await sut.UploadMediaAsync(stream1, "test1.jpg", "image/jpeg");
        var media2 = await sut.UploadMediaAsync(stream2, "test2.jpg", "image/jpeg");
        var media3 = await sut.UploadMediaAsync(stream3, "test3.jpg", "image/jpeg");

        var ids = new[] { media1.Id, media3.Id };

        // Act
        var result = await sut.GetMediaByIdsAsync(ids);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(m => m.Id == media1.Id);
        result.Should().Contain(m => m.Id == media3.Id);
        result.Should().NotContain(m => m.Id == media2.Id);
    }

    [Fact]
    public async Task GetMediaByIdsAsync_ShouldReturnEmpty_WhenNoMatchingIds()
    {
        // Arrange
        var sut = CreateService();
        var ids = new[] { "nonexistent1", "nonexistent2" };

        // Act
        var result = await sut.GetMediaByIdsAsync(ids);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region DeleteMediaAsync Tests

    [Fact]
    public async Task DeleteMediaAsync_ShouldReturnTrue_WhenMediaExists()
    {
        // Arrange
        var sut = CreateService();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var media = await sut.UploadMediaAsync(stream, "test.jpg", "image/jpeg");

        // Act
        var result = await sut.DeleteMediaAsync(media.Id);

        // Assert
        result.Should().BeTrue();
        var deleted = await sut.GetMediaByIdAsync(media.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteMediaAsync_ShouldReturnFalse_WhenMediaNotExists()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.DeleteMediaAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GeneratePreviewAsync Tests

    [Fact]
    public async Task GeneratePreviewAsync_ShouldReturnFilePath_ForExistingImageFile()
    {
        // Arrange
        var sut = CreateService();
        var tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".jpg");
        File.WriteAllBytes(tempFile, new byte[] { 1, 2, 3 });

        try
        {
            var media = await sut.UploadMediaAsync(tempFile);

            // Act
            var result = await sut.GeneratePreviewAsync(media.Id);

            // Assert
            result.Should().Be(tempFile);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GeneratePreviewAsync_ShouldReturnNull_WhenMediaNotExists()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.GeneratePreviewAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GeneratePreviewAsync_ShouldReturnNull_ForNonImageMedia()
    {
        // Arrange
        var sut = CreateService();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var media = await sut.UploadMediaAsync(stream, "test.mp4", "video/mp4");

        // Act
        var result = await sut.GeneratePreviewAsync(media.Id);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetMediaStreamAsync Tests

    [Fact]
    public async Task GetMediaStreamAsync_ShouldReturnStream_WhenFileExists()
    {
        // Arrange
        var sut = CreateService();
        var tempFile = Path.GetTempFileName();
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        File.WriteAllBytes(tempFile, testData);

        try
        {
            var media = await sut.UploadMediaAsync(tempFile);

            // Act
            var result = await sut.GetMediaStreamAsync(media.Id);

            // Assert
            result.Should().NotBeNull();
            using var memStream = new MemoryStream();
            await result!.CopyToAsync(memStream);
            memStream.ToArray().Should().Equal(testData);
            result.Dispose();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetMediaStreamAsync_ShouldReturnNull_WhenMediaNotExists()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.GetMediaStreamAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ValidateMediaForPlatformAsync Tests

    [Fact]
    public async Task ValidateMediaForPlatformAsync_ShouldReturnError_WhenMediaNotFound()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.ValidateMediaForPlatformAsync("nonexistent", SocialPlatform.BlueSky);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Media not found");
    }

    [Fact]
    public async Task ValidateMediaForPlatformAsync_ShouldReturnError_WhenFileSizeExceedsLimit()
    {
        // Arrange
        var sut = CreateService();
        // Create a large stream (2MB) - exceeds BlueSky 1MB limit
        using var stream = new MemoryStream(new byte[2 * 1024 * 1024]);
        var media = await sut.UploadMediaAsync(stream, "large.jpg", "image/jpeg");

        // Act
        var result = await sut.ValidateMediaForPlatformAsync(media.Id, SocialPlatform.BlueSky);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("exceeds platform limit"));
    }

    [Fact]
    public async Task ValidateMediaForPlatformAsync_ShouldReturnValid_WhenFileSizeWithinLimit()
    {
        // Arrange
        var sut = CreateService();
        // Create a small stream (100KB) - within BlueSky 1MB limit
        using var stream = new MemoryStream(new byte[100 * 1024]);
        var media = await sut.UploadMediaAsync(stream, "small.jpg", "image/jpeg");

        // Act
        var result = await sut.ValidateMediaForPlatformAsync(media.Id, SocialPlatform.BlueSky);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region OptimizeMediaForPlatformAsync Tests

    [Fact]
    public async Task OptimizeMediaForPlatformAsync_ShouldReturnMedia_WhenExists()
    {
        // Arrange
        var sut = CreateService();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var media = await sut.UploadMediaAsync(stream, "test.jpg", "image/jpeg");

        // Act
        var result = await sut.OptimizeMediaForPlatformAsync(media.Id, SocialPlatform.BlueSky);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(media.Id);
    }

    [Fact]
    public async Task OptimizeMediaForPlatformAsync_ShouldThrow_WhenMediaNotExists()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var act = () => sut.OptimizeMediaForPlatformAsync("nonexistent", SocialPlatform.BlueSky);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region GetSupportedFormatsAsync Tests

    [Fact]
    public async Task GetSupportedFormatsAsync_ShouldReturnBlueSkyFormats()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.GetSupportedFormatsAsync(SocialPlatform.BlueSky);

        // Assert
        result.Should().NotBeNull();
        result.SupportedImageFormats.Should().Contain("jpg");
        result.SupportedImageFormats.Should().Contain("png");
        result.SupportedVideoFormats.Should().Contain("mp4");
        result.MaxFileSize.Should().Be(1 * 1024 * 1024); // 1MB
        result.SupportsGifs.Should().BeTrue();
    }

    #endregion

    #region CleanupUnusedMediaAsync Tests

    [Fact]
    public async Task CleanupUnusedMediaAsync_ShouldRemoveOldMedia()
    {
        // Arrange
        var sut = CreateService();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var media = await sut.UploadMediaAsync(stream, "test.jpg", "image/jpeg");

        // Simulate old media by setting CreatedAt in the past
        media.CreatedAt = DateTime.UtcNow.AddDays(-2);

        // Act
        var result = await sut.CleanupUnusedMediaAsync(TimeSpan.FromDays(1));

        // Assert
        result.Should().BeGreaterThan(0);
        var deletedMedia = await sut.GetMediaByIdAsync(media.Id);
        deletedMedia.Should().BeNull();
    }

    [Fact]
    public async Task CleanupUnusedMediaAsync_ShouldNotRemoveRecentMedia()
    {
        // Arrange
        var sut = CreateService();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var media = await sut.UploadMediaAsync(stream, "test.jpg", "image/jpeg");

        // Act - cleanup media older than 1 day, but our media is recent
        var result = await sut.CleanupUnusedMediaAsync(TimeSpan.FromDays(1));

        // Assert
        var stillExists = await sut.GetMediaByIdAsync(media.Id);
        stillExists.Should().NotBeNull();
    }

    #endregion

    #region GetStorageStatisticsAsync Tests

    [Fact]
    public async Task GetStorageStatisticsAsync_ShouldReturnCorrectStats()
    {
        // Arrange
        var sut = CreateService();
        using var stream1 = new MemoryStream(new byte[100]);
        using var stream2 = new MemoryStream(new byte[200]);
        using var stream3 = new MemoryStream(new byte[300]);

        await sut.UploadMediaAsync(stream1, "test1.jpg", "image/jpeg");
        await sut.UploadMediaAsync(stream2, "test2.jpg", "image/jpeg");
        await sut.UploadMediaAsync(stream3, "test3.mp4", "video/mp4");

        // Act
        var result = await sut.GetStorageStatisticsAsync();

        // Assert
        result.TotalFiles.Should().Be(3);
        result.TotalSize.Should().Be(600);
        result.FilesByType.Should().ContainKey(MediaType.Image);
        result.FilesByType[MediaType.Image].Should().Be(2);
        result.FilesByType.Should().ContainKey(MediaType.Video);
        result.FilesByType[MediaType.Video].Should().Be(1);
    }

    [Fact]
    public async Task GetStorageStatisticsAsync_ShouldReturnZero_WhenNoMedia()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.GetStorageStatisticsAsync();

        // Assert
        result.TotalFiles.Should().Be(0);
        result.TotalSize.Should().Be(0);
    }

    #endregion

    #region ProcessMediaAsync Tests

    [Fact]
    public async Task ProcessMediaAsync_ShouldReturnMedia_WhenExists()
    {
        // Arrange
        var sut = CreateService();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var media = await sut.UploadMediaAsync(stream, "test.jpg", "image/jpeg");
        var options = new MediaProcessingOptions();

        // Act
        var result = await sut.ProcessMediaAsync(media.Id, options);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(media.Id);
    }

    [Fact]
    public async Task ProcessMediaAsync_ShouldThrow_WhenMediaNotExists()
    {
        // Arrange
        var sut = CreateService();
        var options = new MediaProcessingOptions();

        // Act
        var act = () => sut.ProcessMediaAsync("nonexistent", options);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region ValidateFileAsync Tests

    [Fact]
    public async Task ValidateFileAsync_ShouldReturnError_WhenFileTooLarge()
    {
        // Arrange
        var sut = CreateService();
        // Create stream larger than 50MB
        using var stream = new MemoryStream(new byte[51 * 1024 * 1024]);

        // Act
        var result = await sut.ValidateFileAsync(stream, "large.jpg", "image/jpeg");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("50MB"));
    }

    [Fact]
    public async Task ValidateFileAsync_ShouldReturnError_WhenFileNameEmpty()
    {
        // Arrange
        var sut = CreateService();
        using var stream = new MemoryStream(new byte[100]);

        // Act
        var result = await sut.ValidateFileAsync(stream, "", "image/jpeg");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("name is required"));
    }

    [Fact]
    public async Task ValidateFileAsync_ShouldReturnValid_WhenFileValid()
    {
        // Arrange
        var sut = CreateService();
        using var stream = new MemoryStream(new byte[1024]); // 1KB

        // Act
        var result = await sut.ValidateFileAsync(stream, "test.jpg", "image/jpeg");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public async Task UploadAndRetrieve_ShouldWorkEndToEnd()
    {
        // Arrange
        var sut = CreateService();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        // Act
        var uploaded = await sut.UploadMediaAsync(stream, "test.jpg", "image/jpeg");
        var retrieved = await sut.GetMediaByIdAsync(uploaded.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(uploaded.Id);
        retrieved.FileName.Should().Be("test.jpg");
    }

    [Fact]
    public async Task MultipleUploads_ShouldGenerateUniqueIds()
    {
        // Arrange
        var sut = CreateService();
        using var stream1 = new MemoryStream(new byte[] { 1 });
        using var stream2 = new MemoryStream(new byte[] { 2 });

        // Act
        var media1 = await sut.UploadMediaAsync(stream1, "test1.jpg", "image/jpeg");
        var media2 = await sut.UploadMediaAsync(stream2, "test2.jpg", "image/jpeg");

        // Assert
        media1.Id.Should().NotBe(media2.Id);
    }

    [Theory]
    [InlineData(".jpg", "image/jpeg", MediaType.Image)]
    [InlineData(".png", "image/png", MediaType.Image)]
    [InlineData(".gif", "image/gif", MediaType.Image)]
    [InlineData(".webp", "image/webp", MediaType.Image)]
    [InlineData(".mp4", "video/mp4", MediaType.Video)]
    [InlineData(".mp3", "audio/mpeg", MediaType.Audio)]
    public async Task UploadMediaAsync_FilePath_ShouldDetectCorrectType(string extension, string expectedMimeType, MediaType expectedType)
    {
        // Arrange
        var sut = CreateService();
        var tempFile = Path.ChangeExtension(Path.GetTempFileName(), extension);
        File.WriteAllBytes(tempFile, new byte[] { 1, 2, 3 });

        try
        {
            // Act
            var result = await sut.UploadMediaAsync(tempFile);

            // Assert
            result.Type.Should().Be(expectedType);
            result.MimeType.Should().Be(expectedMimeType);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion
}
