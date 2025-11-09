using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;
using System.Collections.Concurrent;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// Real implementation of IPostService that delegates to actual platform services
/// </summary>
public class PostService : IPostService
{
    private readonly ConcurrentDictionary<string, Post> _posts = new();
    private readonly IBlueSkyService _blueSkyService;
    private readonly ITwitterService _twitterService;
    private readonly ILinkedInService _linkedInService;
    private readonly IThreadsService _threadsService;
    private readonly IFacebookService _facebookService;
    private readonly IAccountService _accountService;

    public PostService(
        IBlueSkyService blueSkyService,
        ITwitterService twitterService,
        ILinkedInService linkedInService,
        IThreadsService threadsService,
        IFacebookService facebookService,
        IAccountService accountService)
    {
        _blueSkyService = blueSkyService ?? throw new ArgumentNullException(nameof(blueSkyService));
        _twitterService = twitterService ?? throw new ArgumentNullException(nameof(twitterService));
        _linkedInService = linkedInService ?? throw new ArgumentNullException(nameof(linkedInService));
        _threadsService = threadsService ?? throw new ArgumentNullException(nameof(threadsService));
        _facebookService = facebookService ?? throw new ArgumentNullException(nameof(facebookService));
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
    }

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

        System.Diagnostics.Debug.WriteLine($"[PostService] Publishing to {platforms.Count()} platforms");

        // Validate first
        var validationResults = await ValidatePostAsync(post).ConfigureAwait(false);
        var platformList = platforms.ToList();

        // Get accounts for selected platforms
        var accounts = await _accountService.GetAllAccountsAsync().ConfigureAwait(false);
        System.Diagnostics.Debug.WriteLine($"[PostService] Found {accounts.Count()} total accounts");

        var authenticatedAccounts = accounts.Where(a => a.IsAuthenticated).ToList();
        System.Diagnostics.Debug.WriteLine($"[PostService] Found {authenticatedAccounts.Count} authenticated accounts");

        foreach (var acc in authenticatedAccounts)
        {
            System.Diagnostics.Debug.WriteLine($"[PostService]   - {acc.PlatformId}: {acc.Username} (Authenticated: {acc.IsAuthenticated}, AuthStatus: {acc.AuthStatus})");
            if (acc.AuthMethod == AuthenticationMethod.AppPassword)
            {
                System.Diagnostics.Debug.WriteLine($"[PostService]     Has AppPassword: {!string.IsNullOrEmpty(acc.AppPassword)}");
            }
        }

        foreach (var platform in platformList)
        {
            System.Diagnostics.Debug.WriteLine($"[PostService] Processing platform: {platform}");

            try
            {
                // Validate
                var validationResult = validationResults.ContainsKey(platform)
                    ? validationResults[platform]
                    : post.ValidateForPlatform(platform);

                if (!validationResult.IsValid)
                {
                    System.Diagnostics.Debug.WriteLine($"[PostService] Validation failed for {platform}: {string.Join("; ", validationResult.Errors)}");
                    results[platform] = new PublishResult
                    {
                        Success = false,
                        ErrorMessage = string.Join("; ", validationResult.Errors),
                        PublishedAt = DateTime.UtcNow
                    };
                    continue;
                }

                // Get the selected account ID for this platform from the post
                Account? account = null;
                if (post.SelectedAccounts.TryGetValue(platform, out var selectedAccountIds) && selectedAccountIds.Any())
                {
                    var selectedAccountId = selectedAccountIds.First();
                    System.Diagnostics.Debug.WriteLine($"[PostService] Looking for selected account ID: {selectedAccountId}");

                    account = authenticatedAccounts.FirstOrDefault(a => a.Id == selectedAccountId && a.PlatformId == platform);

                    if (account != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PostService] Found selected account: {account.Username} (ID: {account.Id})");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[PostService] Selected account ID {selectedAccountId} not found or not authenticated, falling back to default");
                    }
                }

                // Fallback to first authenticated account for this platform if no account was selected or found
                if (account == null)
                {
                    account = authenticatedAccounts.FirstOrDefault(a => a.PlatformId == platform);
                    if (account != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PostService] Using fallback account: {account.Username} (ID: {account.Id})");
                    }
                }

                // Check if we have an authenticated account for this platform
                if (account == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[PostService] No authenticated account found for {platform}");
                    results[platform] = new PublishResult
                    {
                        Success = false,
                        ErrorMessage = $"No authenticated account found for {platform}",
                        PublishedAt = DateTime.UtcNow
                    };
                    continue;
                }

                System.Diagnostics.Debug.WriteLine($"[PostService] Using account: {account.Username} (ID: {account.Id}) for {platform}");

                // Call the actual platform service
                var platformService = GetPlatformService(platform);
                if (platformService == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[PostService] Platform service not available for {platform}");
                    results[platform] = new PublishResult
                    {
                        Success = false,
                        ErrorMessage = $"Platform service not available for {platform}",
                        PublishedAt = DateTime.UtcNow
                    };
                    continue;
                }

                System.Diagnostics.Debug.WriteLine($"[PostService] Calling platform service for {platform}...");

                // Post to the platform
                PublishResult publishResult;
                if (post.IsThread && post.ThreadPosts.Any())
                {
                    publishResult = await platformService.PostThreadAsync(post, account).ConfigureAwait(false);
                }
                else
                {
                    publishResult = await platformService.PostAsync(post, account).ConfigureAwait(false);
                }

                System.Diagnostics.Debug.WriteLine($"[PostService] Result for {platform}: Success={publishResult.Success}, Error={publishResult.ErrorMessage}");

                results[platform] = publishResult;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PostService] Exception for {platform}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[PostService] Stack trace: {ex.StackTrace}");
                results[platform] = new PublishResult
                {
                    Success = false,
                    ErrorMessage = $"Exception posting to {platform}: {ex.Message}",
                    PublishedAt = DateTime.UtcNow
                };
            }
        }

        // Update post status based on results
        var successCount = results.Values.Count(r => r.Success);
        System.Diagnostics.Debug.WriteLine($"[PostService] Publishing complete. Success count: {successCount}/{platformList.Count}");

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

    private IPlatformService? GetPlatformService(SocialPlatform platform)
    {
        return platform switch
        {
            SocialPlatform.BlueSky => _blueSkyService,
            SocialPlatform.X => _twitterService,
            SocialPlatform.LinkedIn => _linkedInService,
            SocialPlatform.Threads => _threadsService,
            SocialPlatform.Facebook => _facebookService,
            _ => null
        };
    }
}
