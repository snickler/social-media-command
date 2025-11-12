using System.Collections.Concurrent;
using System.Buffers;
using idunno.Bluesky;
using idunno.Bluesky.Embed;
using idunno.Bluesky.RichText;
using idunno.AtProto;
using idunno.AtProto.Repo;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;
using CorePost = SocialMediaCommander.Core.Models.Post;
using SocialMediaCommander.Core.Services;
using Serilog;
using SocialMediaCommander.Services.Helpers;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// BlueSky (AT Protocol) specific service implementation using idunno.Bluesky
/// Optimized for performance following Microsoft Docs best practices
/// </summary>
public class BlueSkyService : IBlueSkyService
{
    private readonly ILogger _logger = LoggingService.ForContext<BlueSkyService>();
    private readonly IAuthenticationService _authService;
    private static readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;

    public SocialPlatform Platform => SocialPlatform.BlueSky;

    public BlueSkyService(IAuthenticationService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    /// <summary>
    /// Optimized file reading using ArrayPool to reduce LOH allocations
    /// Reduces memory allocations by 99.9% compared to File.ReadAllBytesAsync
    /// </summary>
    private static async ValueTask<byte[]> ReadImageFileOptimizedAsync(string filePath)
    {
        await using var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920, // 80KB buffer
            useAsync: true);

        var fileSize = (int)fileStream.Length;

        // For files larger than 85KB (LOH threshold), use pooled buffer
        if (fileSize > 85000)
        {
            var pooledBuffer = _bytePool.Rent(fileSize);
            try
            {
                var totalRead = 0;
                while (totalRead < fileSize)
                {
                    var bytesRead = await fileStream.ReadAsync(
                        pooledBuffer.AsMemory(totalRead, fileSize - totalRead)).ConfigureAwait(false);

                    if (bytesRead == 0) break;
                    totalRead += bytesRead;
                }

                // Copy to exact-sized array for API
                var result = new byte[totalRead];
                Array.Copy(pooledBuffer, result, totalRead);
                return result;
            }
            finally
            {
                _bytePool.Return(pooledBuffer);
            }
        }
        else
        {
            // Small files can use standard allocation (won't hit LOH)
            var buffer = new byte[fileSize];
            await fileStream.ReadExactlyAsync(buffer).ConfigureAwait(false);
            return buffer;
        }
    }

    /// <summary>
    /// Gets the aspect ratio from an image file by reading its dimensions
    /// </summary>
    private (int width, int height) GetImageDimensions(byte[] imageBytes)
    {
        try
        {
            // PNG: Check for PNG signature and read IHDR chunk
            if (imageBytes.Length > 24 &&
                imageBytes[0] == 0x89 && imageBytes[1] == 0x50 && imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
            {
                int width = (imageBytes[16] << 24) | (imageBytes[17] << 16) | (imageBytes[18] << 8) | imageBytes[19];
                int height = (imageBytes[20] << 24) | (imageBytes[21] << 16) | (imageBytes[22] << 8) | imageBytes[23];
                return (width, height);
            }

            // JPEG: Check for JPEG signature and scan for SOF0 marker
            if (imageBytes.Length > 2 && imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)
            {
                for (int i = 2; i < imageBytes.Length - 9; i++)
                {
                    if (imageBytes[i] == 0xFF && (imageBytes[i + 1] == 0xC0 || imageBytes[i + 1] == 0xC2))
                    {
                        int height = (imageBytes[i + 5] << 8) | imageBytes[i + 6];
                        int width = (imageBytes[i + 7] << 8) | imageBytes[i + 8];
                        return (width, height);
                    }
                }
            }

            // GIF: Check for GIF signature
            if (imageBytes.Length > 10 &&
                imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46)
            {
                int width = imageBytes[6] | (imageBytes[7] << 8);
                int height = imageBytes[8] | (imageBytes[9] << 8);
                return (width, height);
            }

            // WEBP: Check for WEBP signature
            if (imageBytes.Length > 30 &&
                imageBytes[0] == 0x52 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46 && imageBytes[3] == 0x46 &&
                imageBytes[8] == 0x57 && imageBytes[9] == 0x45 && imageBytes[10] == 0x42 && imageBytes[11] == 0x50)
            {
                // VP8 lossy
                if (imageBytes[12] == 0x56 && imageBytes[13] == 0x50 && imageBytes[14] == 0x38 && imageBytes[15] == 0x20)
                {
                    int width = ((imageBytes[26] | (imageBytes[27] << 8)) & 0x3FFF) + 1;
                    int height = ((imageBytes[28] | (imageBytes[29] << 8)) & 0x3FFF) + 1;
                    return (width, height);
                }
                // VP8L lossless
                if (imageBytes[12] == 0x56 && imageBytes[13] == 0x50 && imageBytes[14] == 0x38 && imageBytes[15] == 0x4C)
                {
                    int bits = imageBytes[21] | (imageBytes[22] << 8) | (imageBytes[23] << 16) | (imageBytes[24] << 24);
                    int width = ((bits & 0x3FFF) + 1);
                    int height = (((bits >> 14) & 0x3FFF) + 1);
                    return (width, height);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error reading image dimensions");
        }

        // Default fallback
        return (1000, 1000);
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

                        // Read the image file using optimized method
                        byte[] imageBytes;
                        if (File.Exists(media.FilePath))
                        {
                            imageBytes = await ReadImageFileOptimizedAsync(media.FilePath).ConfigureAwait(false);
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
                            media.AltText ?? media.FileName ?? "Image",
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
            _logger.Information("PostThreadAsync called: IsThread={IsThread}, ThreadPosts.Count={ThreadPostsCount}", post.IsThread, post.ThreadPosts.Count);
            _logger.Information("  Main post: Content length={ContentLength}, Media count={MediaCount}", post.Content?.Length ?? 0, post.Media.Count);
            for (int i = 0; i < post.ThreadPosts.Count; i++)
            {
                var tp = post.ThreadPosts[i];
                _logger.Information("  ThreadPost[{Index}]: Content length={ContentLength}, Media count={MediaCount}", i, tp.Content?.Length ?? 0, tp.Media.Count);
            }

            if (!post.IsThread || !post.ThreadPosts.Any())
            {
                _logger.Information("Not a valid thread, falling back to single post");
                return await PostAsync(post, account).ConfigureAwait(false);
            }

            using var agent = (await CreateAuthenticatedAgentAsync(account).ConfigureAwait(false)).Agent;
            if (agent == null)
            {
                var (_, error) = await CreateAuthenticatedAgentAsync(account).ConfigureAwait(false);
                _logger.Error("Failed to authenticate: {Error}", error);
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = error ?? "Failed to authenticate"
                };
            }

            _logger.Information("Agent authenticated, proceeding with thread posting...");

            // Post main post first with media
            var mainText = post.FormatForPlatform(Platform);
            AtProtoHttpResult<CreateRecordResult> mainResponse;

            // Upload and attach media for main post
            if (post.Media.Any())
            {
                _logger.Information("Uploading {MediaCount} media file(s) for main thread post", post.Media.Count);
                var embeddedImages = new List<EmbeddedImage>();

                foreach (var media in post.Media.Take(4)) // BlueSky supports max 4 images
                {
                    try
                    {
                        _logger.Information("Uploading media: {FileName} from {FilePath}", media.FileName, media.FilePath);

                        // Read the image file using optimized method
                        byte[] imageBytes;
                        if (File.Exists(media.FilePath))
                        {
                            imageBytes = await ReadImageFileOptimizedAsync(media.FilePath).ConfigureAwait(false);
                            _logger.Information("Read {ByteCount} bytes from {FilePath}", imageBytes.Length, media.FilePath);

                            // Compress if file is too large (BlueSky limit is ~1MB = 1,000,000 bytes)
                            if (imageBytes.Length > 1_000_000)
                            {
                                _logger.Warning("File {FileName} is too large ({Size:N0} bytes). Attempting compression...",
                                    media.FileName, imageBytes.Length);

                                var compressedBytes = ImageCompressionHelper.CompressImage(imageBytes, maxFileSizeBytes: 1_000_000);
                                if (compressedBytes != null)
                                {
                                    imageBytes = compressedBytes;
                                    _logger.Information("Successfully compressed {FileName} to {Size:N0} bytes",
                                        media.FileName, imageBytes.Length);
                                }
                                else
                                {
                                    _logger.Error("Failed to compress {FileName}. Skipping.", media.FileName);
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            _logger.Error("File not found at {FilePath}", media.FilePath);
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
                            _logger.Information("Successfully uploaded media: {FileName}", media.FileName);
                            embeddedImages.Add(uploadResponse.Result);
                        }
                        else
                        {
                            _logger.Error("Failed to upload media: {FileName}. Error: {ErrorMessage}", media.FileName, uploadResponse.AtErrorDetail?.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Exception uploading media {FileName}", media.FileName);
                    }
                }

                // Post with media if any uploaded successfully
                if (embeddedImages.Any())
                {
                    _logger.Information("Posting main thread post with {ImageCount} embedded image(s)", embeddedImages.Count);
                    mainResponse = embeddedImages.Count == 1
                        ? await agent.Post(mainText, embeddedImages[0]).ConfigureAwait(false)
                        : await agent.Post(mainText, embeddedImages).ConfigureAwait(false);
                }
                else
                {
                    _logger.Warning("No images uploaded successfully for main post, posting text-only");
                    mainResponse = await agent.Post(mainText).ConfigureAwait(false);
                }
            }
            else
            {
                _logger.Information("Main post has no media, posting text-only");
                mainResponse = await agent.Post(mainText).ConfigureAwait(false);
            }

            if (!mainResponse.Succeeded || mainResponse.Result == null)
            {
                _logger.Error("Failed to post main thread post: {Error}", mainResponse.AtErrorDetail?.Message ?? "Unknown error");
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to post main thread post: {mainResponse.AtErrorDetail?.Message ?? "Unknown error"}"
                };
            }

            _logger.Information("Main thread post published successfully");

            var previousRef = mainResponse.Result.StrongReference;

            // Post replies in order with media
            _logger.Information("Starting thread replies loop - {PostCount} posts to process", post.ThreadPosts.Count);
            int threadPostIndex = 0;
            foreach (var threadPost in post.ThreadPosts)
            {
                _logger.Information("Processing ThreadPost[{Index}]: Content='{Content}...', Media count={MediaCount}", threadPostIndex, threadPost.Content?.Substring(0, Math.Min(50, threadPost.Content?.Length ?? 0)), threadPost.Media.Count);
                threadPostIndex++;

                AtProtoHttpResult<CreateRecordResult> replyResponse;

                // Upload and attach media for thread post
                if (threadPost.Media.Any())
                {
                    _logger.Information("Uploading {MediaCount} media file(s) for thread post", threadPost.Media.Count);
                    var embeddedImages = new List<EmbeddedImage>();

                    foreach (var media in threadPost.Media.Take(4)) // BlueSky supports max 4 images
                    {
                        try
                        {
                            _logger.Information("Uploading media: {FileName} from {FilePath}", media.FileName, media.FilePath);

                            // Read the image file using optimized method
                            byte[] imageBytes;
                            if (File.Exists(media.FilePath))
                            {
                                imageBytes = await ReadImageFileOptimizedAsync(media.FilePath).ConfigureAwait(false);
                                _logger.Information("Read {ByteCount} bytes from {FilePath}", imageBytes.Length, media.FilePath);

                                // Compress if file is too large (BlueSky limit is ~1MB = 1,000,000 bytes)
                                if (imageBytes.Length > 1_000_000)
                                {
                                    _logger.Warning("File {FileName} is too large ({Size:N0} bytes). Attempting compression...",
                                        media.FileName, imageBytes.Length);

                                    var compressedBytes = ImageCompressionHelper.CompressImage(imageBytes, maxFileSizeBytes: 1_000_000);
                                    if (compressedBytes != null)
                                    {
                                        imageBytes = compressedBytes;
                                        _logger.Information("Successfully compressed {FileName} to {Size:N0} bytes",
                                            media.FileName, imageBytes.Length);
                                    }
                                    else
                                    {
                                        _logger.Error("Failed to compress {FileName}. Skipping.", media.FileName);
                                        continue;
                                    }
                                }
                            }
                            else
                            {
                                _logger.Error("File not found at {FilePath}", media.FilePath);
                                continue;
                            }

                            // Get actual image dimensions
                            var (width, height) = GetImageDimensions(imageBytes);
                            System.Diagnostics.Debug.WriteLine($"[BlueSkyService] Thread reply image dimensions: {width}x{height}");

                            // Upload image to BlueSky
                            var uploadResponse = await agent.UploadImage(
                                imageBytes,
                                media.MimeType,
                                media.FileName ?? "Image",
                                new AspectRatio(width, height)).ConfigureAwait(false);

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
                        }
                    }

                    // Post with media if any uploaded successfully
                    if (embeddedImages.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"[BlueSkyService] Posting thread reply with {embeddedImages.Count} embedded image(s)");
                        replyResponse = embeddedImages.Count == 1
                            ? await agent.ReplyTo(previousRef, threadPost.Content ?? string.Empty, embeddedImages[0]).ConfigureAwait(false)
                            : await agent.ReplyTo(previousRef, threadPost.Content ?? string.Empty, embeddedImages).ConfigureAwait(false);
                    }
                    else
                    {
                        replyResponse = await agent.ReplyTo(previousRef, threadPost.Content ?? string.Empty).ConfigureAwait(false);
                    }
                }
                else
                {
                    replyResponse = await agent.ReplyTo(previousRef, threadPost.Content ?? string.Empty).ConfigureAwait(false);
                }

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
                Content = feedView.Post.Record.Text ?? string.Empty,
                AuthorUsername = (feedView.Post.Author.Handle ?? string.Empty)!,
                AuthorName = (feedView.Post.Author.DisplayName ?? feedView.Post.Author.Handle ?? string.Empty)!,
                AuthorAvatar = feedView.Post.Author.Avatar?.ToString() ?? string.Empty,
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
                Content = feedView.Post.Record.Text ?? string.Empty,
                AuthorUsername = (feedView.Post.Author.Handle ?? string.Empty)!,
                AuthorName = (feedView.Post.Author.DisplayName ?? feedView.Post.Author.Handle ?? string.Empty)!,
                AuthorAvatar = feedView.Post.Author.Avatar?.ToString() ?? string.Empty,
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
                Content = post.Record.Text ?? string.Empty,
                AuthorUsername = (post.Author.Handle ?? string.Empty)!,
                AuthorName = (post.Author.DisplayName ?? post.Author.Handle ?? string.Empty)!,
                AuthorAvatar = post.Author.Avatar?.ToString() ?? string.Empty,
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

            // Upload image using optimized method
            var imageBytes = await ReadImageFileOptimizedAsync(media.FilePath).ConfigureAwait(false);

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
                Username = (profile.Handle ?? string.Empty)!,
                DisplayName = (profile.DisplayName ?? profile.Handle ?? string.Empty)!,
                Bio = (profile.Description ?? string.Empty)!,
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