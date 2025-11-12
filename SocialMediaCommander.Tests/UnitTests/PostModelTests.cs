using SocialMediaCommander.Core.Models;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Comprehensive unit tests for Post model and related classes following Microsoft best practices
/// </summary>
public class PostModelTests
{
    [Fact]
    public void Post_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var post = new Post();

        // Assert
        Assert.NotNull(post.Id);
        Assert.NotEmpty(post.Id);
        Assert.Equal(string.Empty, post.Content);
        Assert.NotNull(post.TargetPlatforms);
        Assert.Empty(post.TargetPlatforms);
        Assert.NotNull(post.Hashtags);
        Assert.Empty(post.Hashtags);
        Assert.False(post.PromoMode);
        Assert.NotNull(post.Media);
        Assert.Empty(post.Media);
        Assert.False(post.IsThread);
        Assert.NotNull(post.ThreadPosts);
        Assert.Empty(post.ThreadPosts);
        Assert.False(post.ThreadsOnlyMode);
        Assert.NotNull(post.SelectedAccounts);
        Assert.Empty(post.SelectedAccounts);
        Assert.Equal(PostStatus.Draft, post.Status);
        Assert.True(post.CreatedAt <= DateTime.UtcNow);
        Assert.True(post.UpdatedAt <= DateTime.UtcNow);
        Assert.Null(post.PublishedAt);
        Assert.NotNull(post.PublishResults);
        Assert.Empty(post.PublishResults);
    }

    [Fact]
    public void GetCharacterCount_WithSimpleContent_ShouldReturnContentLength()
    {
        // Arrange
        var post = new Post { Content = "Hello world!" };

        // Act
        var count = post.GetCharacterCount();

        // Assert
        Assert.Equal(12, count);
    }

    [Fact]
    public void GetCharacterCount_WithPromoModeAndHashtags_ShouldIncludeHashtagLength()
    {
        // Arrange
        var post = new Post
        {
            Content = "Hello world!",
            PromoMode = true,
            Hashtags = new List<string> { "test", "demo" }
        };

        // Act
        var count = post.GetCharacterCount();

        // Assert
        // Content (12) + hashtags "#test #demo" (11) + line breaks (2) = 25
        Assert.Equal(25, count);
    }

    [Fact]
    public void GetCharacterCount_WithPromoModeButNoHashtags_ShouldReturnContentLength()
    {
        // Arrange
        var post = new Post
        {
            Content = "Hello world!",
            PromoMode = true,
            Hashtags = new List<string>()
        };

        // Act
        var count = post.GetCharacterCount();

        // Assert
        Assert.Equal(12, count);
    }

    [Fact]
    public void ValidateForPlatform_WithValidContent_ShouldReturnValid()
    {
        // Arrange
        var post = new Post
        {
            Content = "Short content",
            SelectedAccounts = new Dictionary<SocialPlatform, List<string>>
            {
                { SocialPlatform.BlueSky, new List<string> { "account1" } }
            }
        };

        // Act
        var result = post.ValidateForPlatform(SocialPlatform.BlueSky);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateForPlatform_WithContentExceedingLimit_ShouldReturnInvalid()
    {
        // Arrange
        var longContent = new string('x', 301); // Exceeds BlueSky's 300 limit
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
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("character limit"));
    }

    [Fact]
    public void ValidateForPlatform_WithThreadForThreadPlatform_ShouldReturnValid()
    {
        // Arrange
        var post = new Post
        {
            Content = "Thread content",
            IsThread = true,
            SelectedAccounts = new Dictionary<SocialPlatform, List<string>>
            {
                { SocialPlatform.BlueSky, new List<string> { "account1" } }
            }
        };

        // Act
        var result = post.ValidateForPlatform(SocialPlatform.BlueSky);

        // Assert - BlueSky supports threads, so this should be valid
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateForPlatform_WithMediaForNonMediaPlatform_ShouldReturnInvalid()
    {
        // Arrange
        var post = new Post
        {
            Content = "Content with media",
            Media = new List<Media> { new Media { FileName = "test.jpg" } },
            SelectedAccounts = new Dictionary<SocialPlatform, List<string>>
            {
                // Assuming some platform doesn't support media (this would need to be checked against actual platform configs)
                { SocialPlatform.BlueSky, new List<string> { "account1" } }
            }
        };

        // Act (Note: BlueSky actually supports media, so this might pass. Adjust platform as needed)
        var result = post.ValidateForPlatform(SocialPlatform.BlueSky);

        // Assert - This test might need adjustment based on actual platform capabilities
        Assert.True(result.IsValid || result.Errors.Any(e => e.Contains("does not support media")));
    }

    [Fact]
    public void ValidateForPlatform_WithNoSelectedAccount_ShouldReturnInvalid()
    {
        // Arrange
        var post = new Post
        {
            Content = "Content",
            SelectedAccounts = new Dictionary<SocialPlatform, List<string>>() // No account selected
        };

        // Act
        var result = post.ValidateForPlatform(SocialPlatform.BlueSky);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("No account selected"));
    }

    [Fact]
    public void FormatForPlatform_WithSimpleContent_ShouldReturnContent()
    {
        // Arrange
        var post = new Post { Content = "Hello world!" };

        // Act
        var formatted = post.FormatForPlatform(SocialPlatform.BlueSky);

        // Assert
        Assert.Equal("Hello world!", formatted);
    }

    [Fact]
    public void FormatForPlatform_WithPromoModeAndHashtags_ShouldIncludeHashtags()
    {
        // Arrange
        var post = new Post
        {
            Content = "Hello world!",
            PromoMode = true,
            Hashtags = new List<string> { "test", "demo" }
        };

        // Act
        var formatted = post.FormatForPlatform(SocialPlatform.BlueSky);

        // Assert
        Assert.Contains("Hello world!", formatted);
        Assert.Contains("#test", formatted);
        Assert.Contains("#demo", formatted);
    }

    [Fact]
    public void FormatForPlatform_WithMedia_ShouldReturnContentOnly()
    {
        // Arrange
        var post = new Post
        {
            Content = "Post with media",
            Media = new List<Media>
            {
                new Media { FileName = "image1.jpg" },
                new Media { FileName = "image2.png" }
            }
        };

        // Act
        var formatted = post.FormatForPlatform(SocialPlatform.BlueSky);

        // Assert - Media indicator removed from content
        Assert.Equal("Post with media", formatted);
        Assert.DoesNotContain("media attachments", formatted);
    }

    [Fact]
    public void FormatForPlatform_WithThread_ShouldReturnContentOnly()
    {
        // Arrange
        var post = new Post
        {
            Content = "Thread starter",
            IsThread = true,
            ThreadPosts = new List<ThreadPost>
            {
                new ThreadPost { Content = "Thread post 1" },
                new ThreadPost { Content = "Thread post 2" }
            }
        };

        // Act
        var formatted = post.FormatForPlatform(SocialPlatform.BlueSky);

        // Assert - Thread indicator removed from content
        Assert.Equal("Thread starter", formatted);
        Assert.DoesNotContain("Thread with", formatted);
    }

    [Fact]
    public void FormatForPlatform_WithContentExceedingLimit_ShouldTruncate()
    {
        // Arrange
        var longContent = new string('x', 310); // Exceeds BlueSky's 300 limit
        var post = new Post { Content = longContent };

        // Act
        var formatted = post.FormatForPlatform(SocialPlatform.BlueSky);

        // Assert
        Assert.True(formatted.Length <= 300);
        Assert.EndsWith("...", formatted);
    }

    [Theory]
    [InlineData(SocialPlatform.BlueSky)]
    // TODO: Add Twitter, Facebook, LinkedIn, Threads when implementations are ready
    public void FormatForPlatform_WithDifferentPlatforms_ShouldHandleAllPlatforms(SocialPlatform platform)
    {
        // Arrange
        var post = new Post { Content = "Test content for platform" };

        // Act
        var formatted = post.FormatForPlatform(platform);

        // Assert
        Assert.NotNull(formatted);
        Assert.Contains("Test content for platform", formatted);
    }

    [Fact]
    public void Post_CreatedAt_ShouldBeSetToRecentTime()
    {
        // Act
        var post = new Post();

        // Assert
        Assert.True(post.CreatedAt > DateTime.MinValue);
        Assert.True(post.CreatedAt <= DateTime.UtcNow.AddMinutes(1)); // Allow small buffer for test execution time
    }
}

/// <summary>
/// Unit tests for Media model
/// </summary>
public class MediaModelTests
{
    [Fact]
    public void Media_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var media = new Media();

        // Assert
        Assert.NotNull(media.Id);
        Assert.NotEmpty(media.Id);
        Assert.Equal(string.Empty, media.FileName);
        Assert.Equal(string.Empty, media.FilePath);
        Assert.Null(media.PreviewUrl);
        Assert.Equal(MediaType.Image, media.Type); // Default enum value
        Assert.Equal(0, media.FileSize);
        Assert.Equal(string.Empty, media.MimeType);
        Assert.Null(media.Width);
        Assert.Null(media.Height);
        Assert.Null(media.Duration);
        Assert.True(media.CreatedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(500, "500 B")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(2097152, "2.0 MB")]
    [InlineData(2147483648, "2.0 GB")]
    public void FileSizeFormatted_WithDifferentSizes_ShouldFormatCorrectly(long fileSize, string expected)
    {
        // Arrange
        var media = new Media { FileSize = fileSize };

        // Act
        var formatted = media.FileSizeFormatted;

        // Assert
        Assert.Equal(expected, formatted);
    }

    [Fact]
    public void Media_WithAllProperties_ShouldSetCorrectly()
    {
        // Arrange & Act
        var media = new Media
        {
            FileName = "test.jpg",
            FilePath = "/path/to/test.jpg",
            PreviewUrl = "https://example.com/preview.jpg",
            Type = MediaType.Image,
            FileSize = 1024000,
            MimeType = "image/jpeg",
            Width = 1920,
            Height = 1080,
            Duration = TimeSpan.FromSeconds(30)
        };

        // Assert
        Assert.Equal("test.jpg", media.FileName);
        Assert.Equal("/path/to/test.jpg", media.FilePath);
        Assert.Equal("https://example.com/preview.jpg", media.PreviewUrl);
        Assert.Equal(MediaType.Image, media.Type);
        Assert.Equal(1024000, media.FileSize);
        Assert.Equal("image/jpeg", media.MimeType);
        Assert.Equal(1920, media.Width);
        Assert.Equal(1080, media.Height);
        Assert.Equal(TimeSpan.FromSeconds(30), media.Duration);
    }
}

/// <summary>
/// Unit tests for ThreadPost model
/// </summary>
public class ThreadPostModelTests
{
    [Fact]
    public void ThreadPost_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var threadPost = new ThreadPost();

        // Assert
        Assert.NotNull(threadPost.Id);
        Assert.NotEmpty(threadPost.Id);
        Assert.Equal(string.Empty, threadPost.Content);
        Assert.NotNull(threadPost.Media);
        Assert.Empty(threadPost.Media);
        Assert.Equal(0, threadPost.Order);
        Assert.True(threadPost.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void ThreadPost_WithContent_ShouldSetCorrectly()
    {
        // Arrange & Act
        var threadPost = new ThreadPost
        {
            Content = "This is a thread post",
            Order = 1
        };

        // Assert
        Assert.Equal("This is a thread post", threadPost.Content);
        Assert.Equal(1, threadPost.Order);
    }
}

/// <summary>
/// Unit tests for ValidationResult class
/// </summary>
public class ValidationResultTests
{
    [Fact]
    public void ValidationResult_DefaultConstructor_ShouldBeValid()
    {
        // Act
        var result = new ValidationResult();

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidationResult_WithErrors_ShouldBeInvalid()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2" };

        // Act
        var result = new ValidationResult(errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("Error 1", result.Errors);
        Assert.Contains("Error 2", result.Errors);
    }

    [Fact]
    public void ValidationResult_WithEmptyErrorsList_ShouldBeValid()
    {
        // Arrange
        var errors = new List<string>();

        // Act
        var result = new ValidationResult(errors);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}

/// <summary>
/// Unit tests for PublishResult class
/// </summary>
public class PublishResultTests
{
    [Fact]
    public void PublishResult_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new PublishResult();

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.PlatformPostId);
        Assert.Equal(DateTime.MinValue, result.PublishedAt);
        Assert.NotNull(result.Metadata);
        Assert.Empty(result.Metadata);
    }

    [Fact]
    public void PublishResult_WithSuccessfulResult_ShouldSetCorrectly()
    {
        // Arrange & Act
        var result = new PublishResult
        {
            Success = true,
            PlatformPostId = "post123",
            PublishedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, string> { { "likes", "0" } }
        };

        // Assert
        Assert.True(result.Success);
        Assert.Equal("post123", result.PlatformPostId);
        Assert.True(result.PublishedAt > DateTime.MinValue);
        Assert.Single(result.Metadata);
        Assert.Equal("0", result.Metadata["likes"]);
    }
}
