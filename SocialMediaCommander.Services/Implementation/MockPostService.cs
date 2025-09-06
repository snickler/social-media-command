using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;
using System.Collections.Concurrent;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// Mock implementation of IPostService for development and demonstration
/// </summary>
public class MockPostService : IPostService
{
    private readonly ConcurrentDictionary<string, Post> _posts = new();
    private readonly Random _random = new();

    public Task<Post> CreatePostAsync(Post post)
    {
        if (string.IsNullOrEmpty(post.Id))
            post.Id = Guid.NewGuid().ToString();

        post.CreatedAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;
        post.Status = PostStatus.Draft;

        _posts.TryAdd(post.Id, post);
        return Task.FromResult(post);
    }

    public Task<Post> UpdatePostAsync(Post post)
    {
        if (!_posts.ContainsKey(post.Id))
            throw new ArgumentException($"Post with ID {post.Id} not found");

        post.UpdatedAt = DateTime.UtcNow;
        _posts.TryUpdate(post.Id, post, _posts[post.Id]);
        return Task.FromResult(post);
    }

    public Task<Post?> GetPostByIdAsync(string id)
    {
        _posts.TryGetValue(id, out var post);
        return Task.FromResult(post);
    }

    public Task<IEnumerable<Post>> GetPostsAsync(PostStatus? status = null, int? limit = null)
    {
        var posts = _posts.Values.AsEnumerable();

        if (status.HasValue)
            posts = posts.Where(p => p.Status == status.Value);

        posts = posts.OrderByDescending(p => p.UpdatedAt);

        if (limit.HasValue)
            posts = posts.Take(limit.Value);

        return Task.FromResult(posts);
    }

    public Task<bool> DeletePostAsync(string id)
    {
        return Task.FromResult(_posts.TryRemove(id, out _));
    }

    public Task<Dictionary<SocialPlatform, ValidationResult>> ValidatePostAsync(Post post)
    {
        var results = new Dictionary<SocialPlatform, ValidationResult>();

        foreach (var platform in post.TargetPlatforms)
        {
            results[platform] = post.ValidateForPlatform(platform);
        }

        return Task.FromResult(results);
    }

    public Task<Dictionary<SocialPlatform, PublishResult>> PublishPostAsync(Post post)
    {
        return PublishPostToPlatformsAsync(post, post.TargetPlatforms);
    }

    public async Task<Dictionary<SocialPlatform, PublishResult>> PublishPostToPlatformsAsync(Post post, IEnumerable<SocialPlatform> platforms)
    {
        var results = new Dictionary<SocialPlatform, PublishResult>();
        post.Status = PostStatus.Publishing;
        post.UpdatedAt = DateTime.UtcNow;

        // Validate first
        var validationResults = await ValidatePostAsync(post);
        var platformList = platforms.ToList();

        foreach (var platform in platformList)
        {
            var validationResult = validationResults.ContainsKey(platform)
                ? validationResults[platform]
                : post.ValidateForPlatform(platform);

            if (!validationResult.IsValid)
            {
                results[platform] = new PublishResult
                {
                    Success = false,
                    ErrorMessage = string.Join("; ", validationResult.Errors),
                    PublishedAt = DateTime.UtcNow
                };
                continue;
            }

            // Simulate publishing delay
            await Task.Delay(_random.Next(500, 2000));

            // Simulate 95% success rate
            var success = _random.NextDouble() > 0.05;

            if (success)
            {
                results[platform] = new PublishResult
                {
                    Success = true,
                    PlatformPostId = $"{platform.ToString().ToLower()}-{Guid.NewGuid().ToString()[..8]}",
                    PublishedAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, string>
                    {
                        ["platform"] = platform.ToString(),
                        ["url"] = $"https://{platform.ToString().ToLower()}.com/post/{Guid.NewGuid().ToString()[..8]}"
                    }
                };
            }
            else
            {
                results[platform] = new PublishResult
                {
                    Success = false,
                    ErrorMessage = GetRandomErrorMessage(platform),
                    PublishedAt = DateTime.UtcNow
                };
            }
        }

        // Update post status based on results
        var successCount = results.Values.Count(r => r.Success);
        post.Status = successCount switch
        {
            0 => PostStatus.Failed,
            var count when count == platformList.Count => PostStatus.Published,
            _ => PostStatus.PartiallyPublished
        };

        if (post.Status == PostStatus.Published || post.Status == PostStatus.PartiallyPublished)
        {
            post.PublishedAt = DateTime.UtcNow;
        }

        post.PublishResults = results;
        post.UpdatedAt = DateTime.UtcNow;

        return results;
    }

    public Task<Dictionary<SocialPlatform, string>> GetPostPreviewsAsync(Post post)
    {
        var previews = new Dictionary<SocialPlatform, string>();

        foreach (var platform in post.TargetPlatforms)
        {
            previews[platform] = post.FormatForPlatform(platform);
        }

        return Task.FromResult(previews);
    }

    public Task<Dictionary<SocialPlatform, int>> GetCharacterCountsAsync(Post post)
    {
        var counts = new Dictionary<SocialPlatform, int>();

        foreach (var platform in post.TargetPlatforms)
        {
            var formattedContent = post.FormatForPlatform(platform);
            counts[platform] = formattedContent.Length;
        }

        return Task.FromResult(counts);
    }

    public Task<Post> SaveDraftAsync(Post post)
    {
        post.Status = PostStatus.Draft;
        post.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrEmpty(post.Id))
        {
            return CreatePostAsync(post);
        }
        else
        {
            return UpdatePostAsync(post);
        }
    }

    public Task<IEnumerable<Post>> GetRecentDraftsAsync(int limit = 10)
    {
        var drafts = _posts.Values
            .Where(p => p.Status == PostStatus.Draft)
            .OrderByDescending(p => p.UpdatedAt)
            .Take(limit);

        return Task.FromResult(drafts);
    }

    private string GetRandomErrorMessage(SocialPlatform platform)
    {
        var errors = new[]
        {
            $"Failed to authenticate with {platform}",
            $"Rate limit exceeded for {platform}",
            $"Network timeout connecting to {platform}",
            $"Content rejected by {platform} moderation",
            $"Temporary service unavailable for {platform}",
            $"Account suspended or restricted on {platform}"
        };

        return errors[_random.Next(errors.Length)];
    }
}