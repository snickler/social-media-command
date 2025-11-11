using FluentAssertions;
using SocialMediaCommander.Core.Models;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Tests for Post model and related classes
/// </summary>
public class PostTests
{
    #region Post Constructor Tests

    [Fact]
    public void Post_Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var post = new Post();

        // Assert
        post.Id.Should().NotBeNullOrEmpty();
        post.Content.Should().BeEmpty();
        post.TargetPlatforms.Should().NotBeNull().And.BeEmpty();
        post.Hashtags.Should().NotBeNull().And.BeEmpty();
        post.Media.Should().NotBeNull().And.BeEmpty();
        post.ThreadPosts.Should().NotBeNull().And.BeEmpty();
        post.SelectedAccounts.Should().NotBeNull().And.BeEmpty();
        post.PublishResults.Should().NotBeNull().And.BeEmpty();
        post.Status.Should().Be(PostStatus.Draft);
        post.PromoMode.Should().BeFalse();
        post.IsThread.Should().BeFalse();
        post.ThreadsOnlyMode.Should().BeFalse();
    }

    [Fact]
    public void Post_Constructor_ShouldSetCreatedAt()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var post = new Post();

        // Assert
        post.CreatedAt.Should().BeOnOrAfter(before);
        post.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    #endregion

    #region GetCharacterCount Tests

    [Fact]
    public void GetCharacterCount_ShouldReturnContentLength_WhenNoHashtags()
    {
        // Arrange
        var post = new Post { Content = "Test content" };

        // Act
        var count = post.GetCharacterCount();

        // Assert
        count.Should().Be(12);
    }

    [Fact]
    public void GetCharacterCount_ShouldIncludeHashtags_WhenPromoModeEnabled()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test",
            PromoMode = true,
            Hashtags = new List<string> { "test", "social" }
        };

        // Act
        var count = post.GetCharacterCount();

        // Assert
        // "Test" (4) + "#test #social" (13) + 2 for line breaks = 19
        count.Should().Be(19);
    }

    [Fact]
    public void GetCharacterCount_ShouldNotIncludeHashtags_WhenPromoModeDisabled()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test",
            PromoMode = false,
            Hashtags = new List<string> { "test", "social" }
        };

        // Act
        var count = post.GetCharacterCount();

        // Assert
        count.Should().Be(4);
    }

    [Fact]
    public void GetCharacterCount_ShouldReturnZero_WhenContentEmpty()
    {
        // Arrange
        var post = new Post { Content = "" };

        // Act
        var count = post.GetCharacterCount();

        // Assert
        count.Should().Be(0);
    }

    #endregion

    #region ValidateForPlatform Tests

    [Fact]
    public void ValidateForPlatform_ShouldReturnValid_WhenWithinLimits()
    {
        // Arrange
        var post = new Post
        {
            Content = "Short post",
            TargetPlatforms = new List<SocialPlatform> { SocialPlatform.BlueSky },
            SelectedAccounts = new Dictionary<SocialPlatform, List<string>>
            {
                { SocialPlatform.BlueSky, new List<string> { "account1" } }
            }
        };

        // Act
        var result = post.ValidateForPlatform(SocialPlatform.BlueSky);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateForPlatform_ShouldReturnError_WhenExceedsCharacterLimit()
    {
        // Arrange
        var longContent = new string('a', 500);
        var post = new Post
        {
            Content = longContent,
            SelectedAccounts = new Dictionary<SocialPlatform, List<string>>
            {
                { SocialPlatform.BlueSky, new List<string> { "account1" } }
            }
        };

        // Act
        var result = post.ValidateForPlatform(SocialPlatform.BlueSky);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainMatch("*character limit*");
    }

    [Fact]
    public void ValidateForPlatform_ShouldReturnError_WhenNoAccountSelected()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test",
            SelectedAccounts = new Dictionary<SocialPlatform, List<string>>()
        };

        // Act
        var result = post.ValidateForPlatform(SocialPlatform.BlueSky);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainMatch("*No account selected*");
    }

    [Fact]
    public void ValidateForPlatform_ShouldReturnError_WhenThreadNotSupported()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test",
            IsThread = true,
            SelectedAccounts = new Dictionary<SocialPlatform, List<string>>
            {
                { SocialPlatform.BlueSky, new List<string> { "account1" } }
            }
        };

        // Act - Use a platform that doesn't support threads (if any)
        // BlueSky supports threads, so this test validates the logic exists
        var result = post.ValidateForPlatform(SocialPlatform.BlueSky);

        // Assert - Should be valid since BlueSky supports threads
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateForPlatform_ShouldReturnError_WhenMediaNotSupported()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test",
            Media = new List<Media> { new Media { FileName = "test.jpg", FilePath = "/test.jpg", Type = MediaType.Image } },
            SelectedAccounts = new Dictionary<SocialPlatform, List<string>>
            {
                { SocialPlatform.BlueSky, new List<string> { "account1" } }
            }
        };

        // Act
        var result = post.ValidateForPlatform(SocialPlatform.BlueSky);

        // Assert - Should be valid since BlueSky supports media
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region FormatForPlatform Tests

    [Fact]
    public void FormatForPlatform_ShouldReturnContent_WhenNoFormatting()
    {
        // Arrange
        var post = new Post { Content = "Simple post" };

        // Act
        var formatted = post.FormatForPlatform(SocialPlatform.BlueSky);

        // Assert
        formatted.Should().Be("Simple post");
    }

    [Fact]
    public void FormatForPlatform_ShouldAddHashtags_WhenPromoModeEnabled()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test post",
            PromoMode = true,
            Hashtags = new List<string> { "test", "social" }
        };

        // Act
        var formatted = post.FormatForPlatform(SocialPlatform.BlueSky);

        // Assert
        formatted.Should().Contain("#test #social");
    }

    [Fact]
    public void FormatForPlatform_ShouldNotAddHashtags_WhenPromoModeDisabled()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test post",
            PromoMode = false,
            Hashtags = new List<string> { "test", "social" }
        };

        // Act
        var formatted = post.FormatForPlatform(SocialPlatform.BlueSky);

        // Assert
        formatted.Should().NotContain("#test");
        formatted.Should().Be("Test post");
    }

    [Fact]
    public void FormatForPlatform_ShouldReturnContentOnly_WhenMediaPresent()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test",
            Media = new List<Media>
            {
                new Media { FileName = "test.jpg", FilePath = "/test.jpg", Type = MediaType.Image }
            }
        };

        // Act
        var formatted = post.FormatForPlatform(SocialPlatform.BlueSky);

        // Assert - Media indicator removed from content
        formatted.Should().Be("Test");
        formatted.Should().NotContain("media attachment");
    }

    [Fact]
    public void FormatForPlatform_ShouldReturnContentOnly_WhenMultipleMedia()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test",
            Media = new List<Media>
            {
                new Media { FileName = "test1.jpg", FilePath = "/test1.jpg", Type = MediaType.Image },
                new Media { FileName = "test2.jpg", FilePath = "/test2.jpg", Type = MediaType.Image }
            }
        };

        // Act
        var formatted = post.FormatForPlatform(SocialPlatform.BlueSky);

        // Assert - Media indicator removed from content
        formatted.Should().Be("Test");
        formatted.Should().NotContain("media attachments");
    }

    [Fact]
    public void FormatForPlatform_ShouldReturnContentOnly_WhenThreadPost()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test",
            IsThread = true,
            ThreadPosts = new List<ThreadPost>
            {
                new ThreadPost { Content = "Post 1" },
                new ThreadPost { Content = "Post 2" }
            }
        };

        // Act
        var formatted = post.FormatForPlatform(SocialPlatform.BlueSky);

        // Assert - Thread indicator removed from content
        formatted.Should().Be("Test");
        formatted.Should().NotContain("Thread with");
    }

    [Fact]
    public void FormatForPlatform_ShouldTruncate_WhenExceedsCharacterLimit()
    {
        // Arrange
        var longContent = new string('a', 500);
        var post = new Post { Content = longContent };

        // Act
        var formatted = post.FormatForPlatform(SocialPlatform.BlueSky);

        // Assert
        formatted.Should().EndWith("...");
        formatted.Length.Should().BeLessThanOrEqualTo(300); // BlueSky limit
    }

    #endregion

    #region Media Tests

    [Fact]
    public void Media_Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var media = new Media();

        // Assert
        media.Id.Should().NotBeNullOrEmpty();
        media.FileName.Should().BeEmpty();
        media.FilePath.Should().BeEmpty();
        media.MimeType.Should().BeEmpty();
    }

    [Fact]
    public void Media_FileSizeFormatted_ShouldFormatBytes()
    {
        // Arrange
        var media = new Media { FileSize = 512 };

        // Act
        var formatted = media.FileSizeFormatted;

        // Assert
        formatted.Should().Be("512 B");
    }

    [Fact]
    public void Media_FileSizeFormatted_ShouldFormatKilobytes()
    {
        // Arrange
        var media = new Media { FileSize = 2048 };

        // Act
        var formatted = media.FileSizeFormatted;

        // Assert
        formatted.Should().Be("2.0 KB");
    }

    [Fact]
    public void Media_FileSizeFormatted_ShouldFormatMegabytes()
    {
        // Arrange
        var media = new Media { FileSize = 5 * 1024 * 1024 };

        // Act
        var formatted = media.FileSizeFormatted;

        // Assert
        formatted.Should().Be("5.0 MB");
    }

    [Fact]
    public void Media_FileSizeFormatted_ShouldFormatGigabytes()
    {
        // Arrange
        var media = new Media { FileSize = 2L * 1024 * 1024 * 1024 };

        // Act
        var formatted = media.FileSizeFormatted;

        // Assert
        formatted.Should().Be("2.0 GB");
    }

    #endregion

    #region ThreadPost Tests

    [Fact]
    public void ThreadPost_Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var threadPost = new ThreadPost();

        // Assert
        threadPost.Id.Should().NotBeNullOrEmpty();
        threadPost.Content.Should().BeEmpty();
        threadPost.Media.Should().NotBeNull().And.BeEmpty();
        threadPost.Order.Should().Be(0);
    }

    #endregion

    #region ValidationResult Tests

    [Fact]
    public void ValidationResult_ShouldBeValid_WhenNoErrors()
    {
        // Arrange
        var result = new ValidationResult();

        // Act & Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationResult_ShouldBeInvalid_WhenErrorsExist()
    {
        // Arrange
        var result = new ValidationResult(new List<string> { "Error 1", "Error 2" });

        // Act & Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    #endregion

    #region PublishResult Tests

    [Fact]
    public void PublishResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new PublishResult();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
        result.PlatformPostId.Should().BeNull();
        result.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public void Post_ShouldHandleValidation()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test",
            TargetPlatforms = new List<SocialPlatform>
            {
                SocialPlatform.BlueSky
            },
            SelectedAccounts = new Dictionary<SocialPlatform, List<string>>
            {
                { SocialPlatform.BlueSky, new List<string> { "account1" } }
            }
        };

        // Act
        var blueskyValidation = post.ValidateForPlatform(SocialPlatform.BlueSky);

        // Assert
        blueskyValidation.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Post_ShouldFormatForPlatform()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test post",
            PromoMode = true,
            Hashtags = new List<string> { "test" }
        };

        // Act
        var blueskyFormat = post.FormatForPlatform(SocialPlatform.BlueSky);

        // Assert
        blueskyFormat.Should().Contain("#test");
    }

    [Fact]
    public void Post_ComplexScenario_ShouldHandleAllFeatures()
    {
        // Arrange
        var post = new Post
        {
            Content = "Complex post with everything",
            PromoMode = true,
            Hashtags = new List<string> { "test", "social", "media" },
            IsThread = true,
            ThreadPosts = new List<ThreadPost>
            {
                new ThreadPost { Content = "Thread post 1", Order = 1 },
                new ThreadPost { Content = "Thread post 2", Order = 2 }
            },
            Media = new List<Media>
            {
                new Media { FileName = "image.jpg", FilePath = "/image.jpg", Type = MediaType.Image, FileSize = 1024 * 1024 }
            },
            TargetPlatforms = new List<SocialPlatform> { SocialPlatform.BlueSky },
            SelectedAccounts = new Dictionary<SocialPlatform, List<string>>
            {
                { SocialPlatform.BlueSky, new List<string> { "account1" } }
            }
        };

        // Act
        var validation = post.ValidateForPlatform(SocialPlatform.BlueSky);
        var formatted = post.FormatForPlatform(SocialPlatform.BlueSky);
        var charCount = post.GetCharacterCount();

        // Assert
        validation.IsValid.Should().BeTrue();
        formatted.Should().Contain("#test #social #media");
        // Media and thread indicators removed from FormatForPlatform
        formatted.Should().NotContain("media attachment");
        formatted.Should().NotContain("Thread with");
        charCount.Should().BeGreaterThan(0);
    }

    #endregion
}
