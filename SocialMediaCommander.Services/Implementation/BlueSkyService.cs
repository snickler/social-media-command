using System.Collections.Concurrent;
using idunno.Bluesky;
using idunno.Bluesky.Embed;
using idunno.Bluesky.RichText;
using idunno.AtProto;
using idunno.AtProto.Repo;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;
using CorePost = SocialMediaCommander.Core.Models.Post;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// BlueSky (AT Protocol) specific service implementation using idunno.Bluesky
/// </summary>
public class BlueSkyService : IBlueSkyService
{
    private readonly IAuthenticationService _authService;

    public SocialPlatform Platform => SocialPlatform.BlueSky;

    public BlueSkyService(IAuthenticationService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    /// <summary>
    /// Creates an authenticated BlueSky agent for the account
    /// </summary>
    private async Task<(BlueskyAgent? Agent, string? Error)> CreateAuthenticatedAgentAsync(Account account)
    {
        var agent = new BlueskyAgent();

        try
        {
            if (account.AuthMethod == AuthenticationMethod.AppPassword)
            {
                // Login with App Password
                if (string.IsNullOrEmpty(account.AppPassword))
                {
                    return (null, "App password is not configured");
                }

                var loginResult = await agent.Login(account.Username, account.AppPassword).ConfigureAwait(false);

                if (!loginResult.Succeeded)
                {
                    var errorMsg = loginResult.AtErrorDetail?.Message ?? "Login failed";
                    return (null, errorMsg);
                }

                return (agent, null);
            }
            else if (account.AuthMethod == AuthenticationMethod.OAuth)
            {
                // For OAuth, we still use Login method with the access token
                // idunno.Bluesky manages sessions internally
                if (account.Tokens == null || account.Tokens.IsExpired)
                {
                    return (null, "OAuth tokens are missing or expired");
                }

                // Use the access token as the password for OAuth session
                var loginResult = await agent.Login(account.Username, account.Tokens.AccessToken).ConfigureAwait(false);

                if (!loginResult.Succeeded)
                {
                    var errorMsg = loginResult.AtErrorDetail?.Message ?? "OAuth session failed";
                    return (null, errorMsg);
                }

                return (agent, null);
            }

            return (null, $"Unsupported authentication method: {account.AuthMethod}");
        }
        catch (Exception ex)
        {
            return (null, $"Authentication error: {ex.Message}");
        }
    }

    public async Task<PublishResult> PostAsync(CorePost post, Account account)
    {
        try
        {
            if (!account.IsAuthenticated)
            {
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = "Account is not authenticated"
                };
            }

            using var agent = (await CreateAuthenticatedAgentAsync(account).ConfigureAwait(false)).Agent;
            if (agent == null)
            {
                var (_, error) = await CreateAuthenticatedAgentAsync(account).ConfigureAwait(false);
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = error ?? "Failed to authenticate"
                };
            }

            // Format post text for BlueSky
            var postText = post.FormatForPlatform(Platform);

            System.Diagnostics.Debug.WriteLine($"[BlueSkyService] Posting to BlueSky with {post.Media.Count} media attachments");

            // Check if we have media to upload
            if (post.Media.Any())
            {
                System.Diagnostics.Debug.WriteLine($"[BlueSkyService] Uploading {post.Media.Count} media file(s)");

                var embeddedImages = new List<EmbeddedImage>();

                foreach (var media in post.Media.Take(4)) // BlueSky supports max 4 images
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[BlueSkyService] Uploading media: {media.FileName} from {media.FilePath}");

                        // Read the image file
                        byte[] imageBytes;
                        if (File.Exists(media.FilePath))
                        {
                            imageBytes = await File.ReadAllBytesAsync(media.FilePath).ConfigureAwait(false);
                            System.Diagnostics.Debug.WriteLine($"[BlueSkyService] Read {imageBytes.Length} bytes from {media.FilePath}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[BlueSkyService] ERROR: File not found at {media.FilePath}");
                            continue;
                        }

                        // Upload image to BlueSky
                        var uploadResponse = await agent.UploadImage(
                            imageBytes,
                            media.MimeType,
                            media.FileName ?? "Image",
                            new AspectRatio(1000, 1000)).ConfigureAwait(false);

                        if (uploadResponse.Succeeded && uploadResponse.Result != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[BlueSkyService] Successfully uploaded media: {media.FileName}");
                            embeddedImages.Add(uploadResponse.Result);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[BlueSkyService] Failed to upload media: {media.FileName}. Error: {uploadResponse.AtErrorDetail?.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BlueSkyService] Exception uploading media {media.FileName}: {ex.Message}");
                        // Continue with other media even if one fails
                    }
                }

                // Post with media if any uploaded successfully
                if (embeddedImages.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[BlueSkyService] Posting with {embeddedImages.Count} embedded image(s)");

                    // Pass the collection of EmbeddedImage directly to Post method
                    // For single image: Post(text, EmbeddedImage, cancellationToken)
                    // For multiple images: Post(text, ICollection<EmbeddedImage>, cancellationToken)
                    AtProtoHttpResult<CreateRecordResult> response;
                    if (embeddedImages.Count == 1)
                    {
                        response = await agent.Post(postText, embeddedImages[0]).ConfigureAwait(false);
                    }
                    else
                    {
                        response = await agent.Post(postText, embeddedImages).ConfigureAwait(false);
                    }

                    if (response.Succeeded && response.Result != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BlueSkyService] Post with media succeeded! URI: {response.Result.Uri}");
                        return new PublishResult
                        {
                            Success = true,
                            PlatformPostId = response.Result.Uri.ToString(),
                            PublishedAt = DateTime.UtcNow,
                            Metadata = new Dictionary<string, string>
                            {
                                ["Cid"] = response.Result.Cid.ToString(),
                                ["Uri"] = response.Result.Uri.ToString(),
                                ["MediaCount"] = embeddedImages.Count.ToString()
                            }
                        };
                    }

                    System.Diagnostics.Debug.WriteLine($"[BlueSkyService] Post with media failed: {response.AtErrorDetail?.Message}");
                    return new PublishResult
                    {
                        Success = false,
                        ErrorMessage = response.AtErrorDetail?.Message ?? "Unknown error occurred"
                    };
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[BlueSkyService] No media uploaded successfully, posting text only");
                    // Fall through to text-only post
                }
            }

            // Post text-only (no media or media upload failed)
            System.Diagnostics.Debug.WriteLine("[BlueSkyService] Posting text-only");
            var textResponse = await agent.Post(postText).ConfigureAwait(false);

            if (textResponse.Succeeded && textResponse.Result != null)
            {
                System.Diagnostics.Debug.WriteLine($"[BlueSkyService] Text-only post succeeded! URI: {textResponse.Result.Uri}");
                return new PublishResult
                {
                    Success = true,
                    PlatformPostId = textResponse.Result.Uri.ToString(),
                    PublishedAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, string>
                    {
                        ["Cid"] = textResponse.Result.Cid.ToString(),
                        ["Uri"] = textResponse.Result.Uri.ToString()
                    }
                };
            }

            System.Diagnostics.Debug.WriteLine($"[BlueSkyService] Text-only post failed: {textResponse.AtErrorDetail?.Message}");
            return new PublishResult
            {
                Success = false,
                ErrorMessage = textResponse.AtErrorDetail?.Message ?? "Unknown error occurred"
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BlueSkyService] Exception in PostAsync: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[BlueSkyService] Stack trace: {ex.StackTrace}");
            return new PublishResult
            {
                Success = false,
                ErrorMessage = $"Failed to post to BlueSky: {ex.Message}"
            };
        }
    }

    public async Task<PublishResult> PostThreadAsync(CorePost post, Account account)
    {
        try
        {
            if (!post.IsThread || !post.ThreadPosts.Any())
            {
                return await PostAsync(post, account).ConfigureAwait(false);
            }

            using var agent = (await CreateAuthenticatedAgentAsync(account).ConfigureAwait(false)).Agent;
            if (agent == null)
            {
                var (_, error) = await CreateAuthenticatedAgentAsync(account).ConfigureAwait(false);
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = error ?? "Failed to authenticate"
                };
            }

            // Post main post first
            var mainText = post.FormatForPlatform(Platform);
            var mainResponse = await agent.Post(mainText).ConfigureAwait(false);

            if (!mainResponse.Succeeded || mainResponse.Result == null)
            {
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to post main thread post: {mainResponse.AtErrorDetail?.Message ?? "Unknown error"}"
                };
            }

            var previousRef = mainResponse.Result.StrongReference;

            // Post replies in order
            foreach (var threadPost in post.ThreadPosts)
            {
                var replyResponse = await agent.ReplyTo(previousRef, threadPost.Content).ConfigureAwait(false);

                if (!replyResponse.Succeeded || replyResponse.Result == null)
                {
                    return new PublishResult
                    {
                        Success = false,
                        ErrorMessage = $"Thread failed at reply: {replyResponse.AtErrorDetail?.Message ?? "Unknown error"}"
                    };
                }

                previousRef = replyResponse.Result.StrongReference;
            }

            return new PublishResult
            {
                Success = true,
                PlatformPostId = mainResponse.Result.Uri.ToString(),
                PublishedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, string>
                {
                    ["ThreadLength"] = (post.ThreadPosts.Count + 1).ToString(),
                    ["MainPostCid"] = mainResponse.Result.Cid.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            return new PublishResult
            {
                Success = false,
                ErrorMessage = $"Failed to post thread to BlueSky: {ex.Message}"
            };
        }
    }

    public async Task<bool> DeletePostAsync(string postId, Account account)
    {
        try
        {
            if (!account.IsAuthenticated) return false;

            using var agent = (await CreateAuthenticatedAgentAsync(account).ConfigureAwait(false)).Agent;
            if (agent == null) return false;

            // Parse AT URI from post ID
            if (!Uri.TryCreate(postId, UriKind.Absolute, out var uri))
            {
                return false;
            }

            var atUri = new AtUri(uri.ToString());
            var response = await agent.DeletePost(atUri).ConfigureAwait(false);

            return response.Succeeded;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<SocialFeedItem>> GetUserPostsAsync(Account account, int limit = 20)
    {
        try
        {
            if (!account.IsAuthenticated) return Enumerable.Empty<SocialFeedItem>();

            using var agent = (await CreateAuthenticatedAgentAsync(account).ConfigureAwait(false)).Agent;
            if (agent == null) return Enumerable.Empty<SocialFeedItem>();

            // Get user's posts using author feed
            var feedResponse = await agent.GetAuthorFeed(account.Username, limit: limit).ConfigureAwait(false);

            if (!feedResponse.Succeeded || feedResponse.Result == null)
            {
                return Enumerable.Empty<SocialFeedItem>();
            }

            // feedResponse.Result is directly enumerable
            return feedResponse.Result.Select(feedView => new SocialFeedItem
            {
                Id = feedView.Post.Uri.ToString(),
                Content = feedView.Post.Record.Text,
                AuthorUsername = feedView.Post.Author.Handle,
                AuthorName = feedView.Post.Author.DisplayName ?? feedView.Post.Author.Handle,
                AuthorAvatar = feedView.Post.Author.Avatar?.ToString(),
                PostedAt = feedView.Post.Record.CreatedAt.DateTime,
                Platform = Platform,
                LikeCount = feedView.Post.LikeCount,
                RepostCount = feedView.Post.RepostCount,
                ReplyCount = feedView.Post.ReplyCount
            }).ToList();
        }
        catch
        {
            return Enumerable.Empty<SocialFeedItem>();
        }
    }

    public async Task<IEnumerable<SocialFeedItem>> GetTimelineAsync(Account account, int limit = 50)
    {
        try
        {
            if (!account.IsAuthenticated) return Enumerable.Empty<SocialFeedItem>();

            using var agent = (await CreateAuthenticatedAgentAsync(account).ConfigureAwait(false)).Agent;
            if (agent == null) return Enumerable.Empty<SocialFeedItem>();

            var timelineResponse = await agent.GetTimeline(limit: limit).ConfigureAwait(false);

            if (!timelineResponse.Succeeded || timelineResponse.Result == null)
            {
                return Enumerable.Empty<SocialFeedItem>();
            }

            // timelineResponse.Result is directly enumerable
            return timelineResponse.Result.Select(feedView => new SocialFeedItem
            {
                Id = feedView.Post.Uri.ToString(),
                Content = feedView.Post.Record.Text,
                AuthorUsername = feedView.Post.Author.Handle,
                AuthorName = feedView.Post.Author.DisplayName ?? feedView.Post.Author.Handle,
                AuthorAvatar = feedView.Post.Author.Avatar?.ToString(),
                PostedAt = feedView.Post.Record.CreatedAt.DateTime,
                Platform = Platform,
                LikeCount = feedView.Post.LikeCount,
                RepostCount = feedView.Post.RepostCount,
                ReplyCount = feedView.Post.ReplyCount,
                IsRepost = feedView.Reason != null
            }).ToList();
        }
        catch
        {
            return Enumerable.Empty<SocialFeedItem>();
        }
    }

    public async Task<IEnumerable<SocialFeedItem>> SearchPostsAsync(string query, Account account, int limit = 20)
    {
        try
        {
            if (!account.IsAuthenticated) return Enumerable.Empty<SocialFeedItem>();

            using var agent = (await CreateAuthenticatedAgentAsync(account).ConfigureAwait(false)).Agent;
            if (agent == null) return Enumerable.Empty<SocialFeedItem>();

            var searchResponse = await agent.SearchPosts(query, limit: limit).ConfigureAwait(false);

            if (!searchResponse.Succeeded || searchResponse.Result == null)
            {
                return Enumerable.Empty<SocialFeedItem>();
            }

            // searchResponse.Result is directly enumerable
            return searchResponse.Result.Select(post => new SocialFeedItem
            {
                Id = post.Uri.ToString(),
                Content = post.Record.Text,
                AuthorUsername = post.Author.Handle,
                AuthorName = post.Author.DisplayName ?? post.Author.Handle,
                AuthorAvatar = post.Author.Avatar?.ToString(),
                PostedAt = post.Record.CreatedAt.DateTime,
                Platform = Platform,
                LikeCount = post.LikeCount,
                RepostCount = post.RepostCount,
                ReplyCount = post.ReplyCount
            }).ToList();
        }
        catch
        {
            return Enumerable.Empty<SocialFeedItem>();
        }
    }

    public Task<IEnumerable<string>> GetTrendingHashtagsAsync(Account account)
    {
        // BlueSky doesn't have a trending hashtags API yet
        // Return empty for now
        return Task.FromResult(Enumerable.Empty<string>());
    }

    public async Task<ValidationResult> ValidateContentAsync(CorePost post)
    {
        var limits = await GetPlatformLimitsAsync().ConfigureAwait(false);

        var result = new ValidationResult();

        // Check character limit on original content (not formatted)
        if (post.Content.Length > limits.CharacterLimit)
        {
            result.Errors.Add($"Content exceeds {limits.CharacterLimit} characters");
        }

        // Check media count
        if (post.Media.Count > limits.MaxMediaCount)
        {
            result.Errors.Add($"BlueSky supports maximum {limits.MaxMediaCount} media attachments");
        }

        return result;
    }

    public async Task<string> UploadMediaAsync(Media media, Account account)
    {
        try
        {
            if (!account.IsAuthenticated) return "";

            using var agent = (await CreateAuthenticatedAgentAsync(account).ConfigureAwait(false)).Agent;
            if (agent == null) return "";

            // Upload image using idunno.Bluesky
            var imageBytes = await File.ReadAllBytesAsync(media.FilePath).ConfigureAwait(false);

            var uploadResponse = await agent.UploadImage(
                imageBytes,
                media.MimeType,
                media.FileName ?? "Image",
                new AspectRatio(1000, 1000)).ConfigureAwait(false);

            if (uploadResponse.Succeeded && uploadResponse.Result != null)
            {
                // Return the blob ref link as the media ID
                return uploadResponse.Result.Image.ToString();
            }

            return "";
        }
        catch
        {
            return "";
        }
    }

    public Task<PlatformLimits> GetPlatformLimitsAsync()
    {
        return Task.FromResult(new PlatformLimits
        {
            CharacterLimit = 300,
            MaxMediaCount = 4,
            MaxMediaSize = 1000000, // 1MB
            SupportedMediaTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" },
            MaxThreadLength = 25, // BlueSky supports reasonable thread lengths
            PostingInterval = TimeSpan.FromSeconds(1),
            DailyPostLimit = 300
        });
    }

    public async Task<bool> CreateSessionAsync(Account account)
    {
        try
        {
            var (agent, _) = await CreateAuthenticatedAgentAsync(account).ConfigureAwait(false);
            agent?.Dispose();
            return agent != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserProfile?> GetProfileAsync(Account account)
    {
        try
        {
            if (!account.IsAuthenticated) return null;

            using var agent = (await CreateAuthenticatedAgentAsync(account).ConfigureAwait(false)).Agent;
            if (agent == null) return null;

            var profileResponse = await agent.GetProfile(account.Username).ConfigureAwait(false);

            if (!profileResponse.Succeeded || profileResponse.Result == null)
            {
                return null;
            }

            var profile = profileResponse.Result;

            return new UserProfile
            {
                Id = profile.Did.ToString(),
                Username = profile.Handle,
                DisplayName = profile.DisplayName ?? profile.Handle,
                Bio = profile.Description,
                Avatar = profile.Avatar?.ToString(),
                Banner = profile.Banner?.ToString(),
                FollowerCount = profile.FollowersCount,
                FollowingCount = profile.FollowsCount,
                PostCount = profile.PostsCount,
                CreatedAt = DateTime.UtcNow, // AT Protocol doesn't expose account creation date
                Platform = Platform
            };
        }
        catch
        {
            return null;
        }
    }
}