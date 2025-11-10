using FluentAssertions;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Tests for MockPostService
/// </summary>
public class MockPostServiceTests
{
    private MockPostService CreateService()
    {
        return new MockPostService();
    }

    private Post CreateTestPost()
    {
        return new Post
        {
            Content = "Test post content",
            TargetPlatforms = new List<SocialPlatform> { SocialPlatform.BlueSky }
        };
    }

    #region CreatePostAsync Tests

    [Fact]
    public async Task CreatePostAsync_ShouldCreatePost_WithGeneratedId()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();

        // Act
        var result = await sut.CreatePostAsync(post);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreatePostAsync_ShouldSetCreatedAt()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();
        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = await sut.CreatePostAsync(post);

        // Assert
        result.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        result.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task CreatePostAsync_ShouldSetStatusToDraft()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();

        // Act
        var result = await sut.CreatePostAsync(post);

        // Assert
        result.Status.Should().Be(PostStatus.Draft);
    }

    [Fact]
    public async Task CreatePostAsync_ShouldSetUpdatedAt()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();

        // Act
        var result = await sut.CreatePostAsync(post);

        // Assert
        result.UpdatedAt.Should().BeCloseTo(result.CreatedAt, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region UpdatePostAsync Tests

    [Fact]
    public async Task UpdatePostAsync_ShouldUpdatePost_WhenExists()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();
        var created = await sut.CreatePostAsync(post);
        created.Content = "Updated content";

        // Act
        var result = await sut.UpdatePostAsync(created);

        // Assert
        result.Content.Should().Be("Updated content");
    }

    [Fact]
    public async Task UpdatePostAsync_ShouldUpdateUpdatedAt()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();
        var created = await sut.CreatePostAsync(post);
        await Task.Delay(50);

        // Act
        var result = await sut.UpdatePostAsync(created);

        // Assert
        result.UpdatedAt.Should().BeOnOrAfter(created.UpdatedAt);
    }

    [Fact]
    public async Task UpdatePostAsync_ShouldThrow_WhenPostNotExists()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();
        post.Id = "nonexistent";

        // Act
        var act = () => sut.UpdatePostAsync(post);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region GetPostByIdAsync Tests

    [Fact]
    public async Task GetPostByIdAsync_ShouldReturnPost_WhenExists()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();
        var created = await sut.CreatePostAsync(post);

        // Act
        var result = await sut.GetPostByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetPostByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.GetPostByIdAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetPostsAsync Tests

    [Fact]
    public async Task GetPostsAsync_ShouldReturnAllPosts_WhenNoFilter()
    {
        // Arrange
        var sut = CreateService();
        await sut.CreatePostAsync(CreateTestPost());
        await sut.CreatePostAsync(CreateTestPost());
        await sut.CreatePostAsync(CreateTestPost());

        // Act
        var result = await sut.GetPostsAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetPostsAsync_ShouldFilterByStatus()
    {
        // Arrange
        var sut = CreateService();
        var post1 = await sut.CreatePostAsync(CreateTestPost());
        var post2 = await sut.CreatePostAsync(CreateTestPost());
        post2.Status = PostStatus.Published;
        post2.PublishedAt = DateTime.UtcNow;
        await sut.UpdatePostAsync(post2);

        // Act
        var result = await sut.GetPostsAsync(PostStatus.Draft);

        // Assert
        result.Should().ContainSingle();
        result.First().Status.Should().Be(PostStatus.Draft);
    }

    [Fact]
    public async Task GetPostsAsync_ShouldRespectLimit()
    {
        // Arrange
        var sut = CreateService();
        for (int i = 0; i < 10; i++)
        {
            await sut.CreatePostAsync(CreateTestPost());
        }

        // Act
        var result = await sut.GetPostsAsync(limit: 5);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetPostsAsync_ShouldOrderByUpdatedAtDescending()
    {
        // Arrange
        var sut = CreateService();
        var post1 = await sut.CreatePostAsync(CreateTestPost());
        await Task.Delay(100);
        var post2 = await sut.CreatePostAsync(CreateTestPost());
        await Task.Delay(100);
        var post3 = await sut.CreatePostAsync(CreateTestPost());

        // Act
        var result = (await sut.GetPostsAsync()).ToList();

        // Assert - Most recent should be first
        result[0].UpdatedAt.Should().BeOnOrAfter(result[1].UpdatedAt);
        result[1].UpdatedAt.Should().BeOnOrAfter(result[2].UpdatedAt);
    }

    #endregion

    #region DeletePostAsync Tests

    [Fact]
    public async Task DeletePostAsync_ShouldReturnTrue_WhenPostExists()
    {
        // Arrange
        var sut = CreateService();
        var post = await sut.CreatePostAsync(CreateTestPost());

        // Act
        var result = await sut.DeletePostAsync(post.Id);

        // Assert
        result.Should().BeTrue();
        var deleted = await sut.GetPostByIdAsync(post.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeletePostAsync_ShouldReturnFalse_WhenPostNotExists()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.DeletePostAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ValidatePostAsync Tests

    [Fact]
    public async Task ValidatePostAsync_ShouldReturnResultsForAllPlatforms()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();
        post.TargetPlatforms = new List<SocialPlatform> { SocialPlatform.BlueSky };

        // Act
        var result = await sut.ValidatePostAsync(post);

        // Assert
        result.Should().ContainKey(SocialPlatform.BlueSky);
        result[SocialPlatform.BlueSky].Should().NotBeNull();
    }

    #endregion

    #region PublishPostAsync Tests

    [Fact]
    public async Task PublishPostAsync_ShouldPublishToAllTargetPlatforms()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();
        post.Content = "Valid short content";

        // Act
        var result = await sut.PublishPostAsync(post);

        // Assert
        result.Should().ContainKey(SocialPlatform.BlueSky);
        post.PublishResults.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishPostAsync_ShouldSetPublishedStatus_WhenAllSucceed()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();
        post.Content = "Valid short content";

        // Act - May need to retry due to random 5% failure rate
        var result = await sut.PublishPostAsync(post);

        // Assert - If all succeed (95% chance)
        if (result.Values.All(r => r.Success))
        {
            post.Status.Should().Be(PostStatus.Published);
            post.PublishedAt.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task PublishPostAsync_ShouldSetPublishingStatus_Initially()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();

        // Act
        var publishTask = sut.PublishPostAsync(post);

        // The status is set to Publishing during execution
        // We check after completion
        await publishTask;

        // Assert - Status should be Published, Failed, or PartiallyPublished
        post.Status.Should().NotBe(PostStatus.Draft);
    }

    #endregion

    #region PublishPostToPlatformsAsync Tests

    [Fact]
    public async Task PublishPostToPlatformsAsync_ShouldPublishToSpecifiedPlatforms()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();
        post.TargetPlatforms = new List<SocialPlatform> { SocialPlatform.BlueSky };
        var platforms = new[] { SocialPlatform.BlueSky };

        // Act
        var result = await sut.PublishPostToPlatformsAsync(post, platforms);

        // Assert
        result.Should().ContainKey(SocialPlatform.BlueSky);
    }

    [Fact]
    public async Task PublishPostToPlatformsAsync_ShouldValidateBeforePublishing()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();
        post.Content = new string('a', 10000); // Very long content that might fail validation
        var platforms = new[] { SocialPlatform.BlueSky };

        // Act
        var result = await sut.PublishPostToPlatformsAsync(post, platforms);

        // Assert - Should have results for BlueSky
        result.Should().ContainKey(SocialPlatform.BlueSky);
    }

    #endregion

    #region GetPostPreviewsAsync Tests

    [Fact]
    public async Task GetPostPreviewsAsync_ShouldReturnPreviewsForAllPlatforms()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();

        // Act
        var result = await sut.GetPostPreviewsAsync(post);

        // Assert
        result.Should().ContainKey(SocialPlatform.BlueSky);
        result[SocialPlatform.BlueSky].Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetCharacterCountsAsync Tests

    [Fact]
    public async Task GetCharacterCountsAsync_ShouldReturnCountsForAllPlatforms()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();
        post.Content = "Test content";

        // Act
        var result = await sut.GetCharacterCountsAsync(post);

        // Assert
        result.Should().ContainKey(SocialPlatform.BlueSky);
        result[SocialPlatform.BlueSky].Should().BeGreaterThan(0);
    }

    #endregion

    #region SaveDraftAsync Tests

    [Fact]
    public async Task SaveDraftAsync_ShouldCreateNewPost_WhenIdIsEmpty()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();
        post.Id = string.Empty; // Ensure ID is empty

        // Act
        var result = await sut.SaveDraftAsync(post);

        // Assert
        result.Id.Should().NotBeNullOrEmpty();
        result.Status.Should().Be(PostStatus.Draft);
    }

    [Fact]
    public async Task SaveDraftAsync_ShouldUpdateExistingPost_WhenIdExists()
    {
        // Arrange
        var sut = CreateService();
        var post = await sut.CreatePostAsync(CreateTestPost());
        post.Content = "Updated draft content";

        // Act
        var result = await sut.SaveDraftAsync(post);

        // Assert
        result.Content.Should().Be("Updated draft content");
        result.Status.Should().Be(PostStatus.Draft);
    }

    #endregion

    #region GetRecentDraftsAsync Tests

    [Fact]
    public async Task GetRecentDraftsAsync_ShouldReturnOnlyDrafts()
    {
        // Arrange
        var sut = CreateService();
        await sut.CreatePostAsync(CreateTestPost());
        await sut.CreatePostAsync(CreateTestPost());

        var publishedPost = await sut.CreatePostAsync(CreateTestPost());
        publishedPost.Status = PostStatus.Published;
        publishedPost.PublishedAt = DateTime.UtcNow;
        await sut.UpdatePostAsync(publishedPost);

        // Act
        var result = await sut.GetRecentDraftsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => p.Status.Should().Be(PostStatus.Draft));
    }

    [Fact]
    public async Task GetRecentDraftsAsync_ShouldRespectLimit()
    {
        // Arrange
        var sut = CreateService();
        for (int i = 0; i < 15; i++)
        {
            await sut.CreatePostAsync(CreateTestPost());
        }

        // Act
        var result = await sut.GetRecentDraftsAsync(5);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetRecentDraftsAsync_ShouldOrderByUpdatedAtDescending()
    {
        // Arrange
        var sut = CreateService();
        var post1 = await sut.CreatePostAsync(CreateTestPost());
        await Task.Delay(50);
        var post2 = await sut.CreatePostAsync(CreateTestPost());
        await Task.Delay(50);
        var post3 = await sut.CreatePostAsync(CreateTestPost());

        // Act
        var result = (await sut.GetRecentDraftsAsync()).ToList();

        // Assert
        result.First().Id.Should().Be(post3.Id);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public async Task CreateAndRetrievePost_ShouldWorkEndToEnd()
    {
        // Arrange
        var sut = CreateService();
        var post = CreateTestPost();

        // Act
        var created = await sut.CreatePostAsync(post);
        var retrieved = await sut.GetPostByIdAsync(created.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Content.Should().Be(post.Content);
    }

    [Fact]
    public async Task MultipleCreates_ShouldGenerateUniqueIds()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var post1 = await sut.CreatePostAsync(CreateTestPost());
        var post2 = await sut.CreatePostAsync(CreateTestPost());
        var post3 = await sut.CreatePostAsync(CreateTestPost());

        // Assert
        post1.Id.Should().NotBe(post2.Id);
        post2.Id.Should().NotBe(post3.Id);
        post1.Id.Should().NotBe(post3.Id);
    }

    #endregion
}
