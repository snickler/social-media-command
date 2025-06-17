using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Service interface for managing posts and publishing operations
/// </summary>
public interface IPostService
{
    /// <summary>
    /// Creates a new post
    /// </summary>
    Task<Post> CreatePostAsync(Post post);
    
    /// <summary>
    /// Updates an existing post
    /// </summary>
    Task<Post> UpdatePostAsync(Post post);
    
    /// <summary>
    /// Gets a post by ID
    /// </summary>
    Task<Post?> GetPostByIdAsync(string id);
    
    /// <summary>
    /// Gets all posts with optional filtering
    /// </summary>
    Task<IEnumerable<Post>> GetPostsAsync(PostStatus? status = null, int? limit = null);
    
    /// <summary>
    /// Deletes a post
    /// </summary>
    Task<bool> DeletePostAsync(string id);
    
    /// <summary>
    /// Validates a post for all target platforms
    /// </summary>
    Task<Dictionary<SocialPlatform, ValidationResult>> ValidatePostAsync(Post post);
    
    /// <summary>
    /// Publishes a post to all target platforms
    /// </summary>
    Task<Dictionary<SocialPlatform, PublishResult>> PublishPostAsync(Post post);
    
    /// <summary>
    /// Publishes a post to specific platforms
    /// </summary>
    Task<Dictionary<SocialPlatform, PublishResult>> PublishPostToPlatformsAsync(Post post, IEnumerable<SocialPlatform> platforms);
    
    /// <summary>
    /// Formats post content for preview on specific platforms
    /// </summary>
    Task<Dictionary<SocialPlatform, string>> GetPostPreviewsAsync(Post post);
    
    /// <summary>
    /// Gets character count for post on each platform
    /// </summary>
    Task<Dictionary<SocialPlatform, int>> GetCharacterCountsAsync(Post post);
    
    /// <summary>
    /// Saves a post as draft
    /// </summary>
    Task<Post> SaveDraftAsync(Post post);
    
    /// <summary>
    /// Gets recent drafts
    /// </summary>
    Task<IEnumerable<Post>> GetRecentDraftsAsync(int limit = 10);
} 