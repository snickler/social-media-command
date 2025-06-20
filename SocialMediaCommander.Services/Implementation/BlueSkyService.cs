using System.Text;
using System.Text.Json;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// BlueSky (AT Protocol) specific service implementation
/// </summary>
public class BlueSkyService : IBlueSkyService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationService _authService;
    private const string BaseUrl = "https://bsky.social/xrpc";

    public SocialPlatform Platform => SocialPlatform.BlueSky;

    public BlueSkyService(HttpClient httpClient, IAuthenticationService authService)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    public async Task<PublishResult> PostAsync(Post post, Account account)
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

            var session = await CreateSessionAsync(account);
            if (!session)
            {
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = "Failed to create BlueSky session"
                };
            }

            var postData = new
            {
                repo = account.Username,
                collection = "app.bsky.feed.post",
                record = new
                {
                    text = post.FormatForPlatform(Platform),
                    createdAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    facets = ExtractFacets(post.Content)
                }
            };

            var json = JsonSerializer.Serialize(postData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/com.atproto.repo.createRecord");
            request.Content = content;
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var document = JsonDocument.Parse(responseContent);
                var uri = document.RootElement.GetProperty("uri").GetString();

                return new PublishResult
                {
                    Success = true,
                    PlatformPostId = uri ?? "",
                    PublishedAt = DateTime.UtcNow
                };
            }
            else
            {
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = $"BlueSky API error: {responseContent}"
                };
            }
        }
        catch (Exception ex)
        {
            return new PublishResult
            {
                Success = false,
                ErrorMessage = $"Failed to post to BlueSky: {ex.Message}"
            };
        }
    }

    public async Task<PublishResult> PostThreadAsync(Post post, Account account)
    {
        try
        {
            if (!post.IsThread || !post.ThreadPosts.Any())
            {
                return await PostAsync(post, account);
            }

            var results = new List<PublishResult>();
            string? replyTo = null;

            // Post main post first
            var mainResult = await PostAsync(post, account);
            results.Add(mainResult);

            if (!mainResult.Success)
            {
                return mainResult;
            }

            replyTo = mainResult.PlatformPostId;

            // Post thread posts
            foreach (var threadPost in post.ThreadPosts)
            {
                var threadPostData = new Post
                {
                    Content = threadPost.Content,
                    TargetPlatforms = new List<SocialPlatform> { Platform }
                };

                var threadResult = await PostReplyAsync(threadPostData, account, replyTo ?? throw new InvalidOperationException("Reply URI cannot be null"));
                results.Add(threadResult);

                if (!threadResult.Success)
                {
                    break;
                }

                replyTo = threadResult.PlatformPostId;
            }

            var failedPosts = results.Where(r => !r.Success).ToList();
            if (failedPosts.Any())
            {
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = $"Thread partially failed: {string.Join(", ", failedPosts.Select(f => f.ErrorMessage))}"
                };
            }

            return new PublishResult
            {
                Success = true,
                PlatformPostId = mainResult.PlatformPostId,
                PublishedAt = DateTime.UtcNow
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

            var session = await CreateSessionAsync(account);
            if (!session) return false;

            var deleteData = new
            {
                repo = account.Username,
                collection = "app.bsky.feed.post",
                rkey = ExtractRkeyFromUri(postId)
            };

            var json = JsonSerializer.Serialize(deleteData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/com.atproto.repo.deleteRecord");
            request.Content = content;
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
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

            var session = await CreateSessionAsync(account);
            if (!session) return Enumerable.Empty<SocialFeedItem>();

            var request = new HttpRequestMessage(HttpMethod.Get, 
                $"{BaseUrl}/com.atproto.repo.listRecords?repo={account.Username}&collection=app.bsky.feed.post&limit={limit}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return Enumerable.Empty<SocialFeedItem>();

            var json = await response.Content.ReadAsStringAsync();
            return ParseBlueSkyPosts(json, account);
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

            var session = await CreateSessionAsync(account);
            if (!session) return Enumerable.Empty<SocialFeedItem>();

            var request = new HttpRequestMessage(HttpMethod.Get, 
                $"{BaseUrl}/app.bsky.feed.getTimeline?limit={limit}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return Enumerable.Empty<SocialFeedItem>();

            var json = await response.Content.ReadAsStringAsync();
            return ParseBlueSkyFeed(json);
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

            var session = await CreateSessionAsync(account);
            if (!session) return Enumerable.Empty<SocialFeedItem>();

            var request = new HttpRequestMessage(HttpMethod.Get, 
                $"{BaseUrl}/app.bsky.feed.searchPosts?q={Uri.EscapeDataString(query)}&limit={limit}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return Enumerable.Empty<SocialFeedItem>();

            var json = await response.Content.ReadAsStringAsync();
            return ParseBlueSkySearchResults(json);
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

    public async Task<ValidationResult> ValidateContentAsync(Post post)
    {
        var limits = await GetPlatformLimitsAsync();

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

            var session = await CreateSessionAsync(account);
            if (!session) return "";

            // BlueSky media upload implementation would go here
            // This is a placeholder - actual implementation would upload to BlueSky's blob storage
            return $"bsky-media-{Guid.NewGuid()}";
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
            if (account.Tokens == null || account.Tokens.IsExpired)
            {
                // Need to re-authenticate
                return false;
            }

            // For BlueSky, we need to create a session with the access token
            var sessionData = new
            {
                identifier = account.Username,
                password = account.Tokens.AccessToken // This would be the app password in real implementation
            };

            var json = JsonSerializer.Serialize(sessionData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BaseUrl}/com.atproto.server.createSession", content);
            return response.IsSuccessStatusCode;
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

            var session = await CreateSessionAsync(account);
            if (!session) return null;

            var request = new HttpRequestMessage(HttpMethod.Get, 
                $"{BaseUrl}/app.bsky.actor.getProfile?actor={account.Username}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            return new UserProfile
            {
                Id = root.GetProperty("did").GetString() ?? "",
                Username = root.GetProperty("handle").GetString() ?? "",
                DisplayName = root.TryGetProperty("displayName", out var name) ? name.GetString() ?? "" : "",
                Avatar = root.TryGetProperty("avatar", out var avatar) ? avatar.GetString() : null
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task<PublishResult> PostReplyAsync(Post post, Account account, string replyToUri)
    {
        try
        {
            var postData = new
            {
                repo = account.Username,
                collection = "app.bsky.feed.post",
                record = new
                {
                    text = post.FormatForPlatform(Platform),
                    createdAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    reply = new
                    {
                        root = new { uri = replyToUri, cid = "" },
                        parent = new { uri = replyToUri, cid = "" }
                    }
                }
            };

            var json = JsonSerializer.Serialize(postData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/com.atproto.repo.createRecord");
            request.Content = content;
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var document = JsonDocument.Parse(responseContent);
                var uri = document.RootElement.GetProperty("uri").GetString();

                return new PublishResult
                {
                    Success = true,
                    PlatformPostId = uri ?? "",
                    PublishedAt = DateTime.UtcNow
                };
            }
            else
            {
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = $"BlueSky reply error: {responseContent}"
                };
            }
        }
        catch (Exception ex)
        {
            return new PublishResult
            {
                Success = false,
                ErrorMessage = $"Failed to post reply to BlueSky: {ex.Message}"
            };
        }
    }

    private object[] ExtractFacets(string content)
    {
        // Extract mentions, hashtags, and links from content
        // This is a simplified implementation
        var facets = new List<object>();
        
        // Find hashtags
        var hashtagMatches = System.Text.RegularExpressions.Regex.Matches(content, @"#\w+");
        foreach (System.Text.RegularExpressions.Match match in hashtagMatches)
        {
            facets.Add(new
            {
                index = new { byteStart = match.Index, byteEnd = match.Index + match.Length },
                features = new[] { new { type = "app.bsky.richtext.facet#tag", tag = match.Value.Substring(1) } }
            });
        }

        return facets.ToArray();
    }

    private string ExtractRkeyFromUri(string uri)
    {
        // Extract the record key from a BlueSky URI
        var parts = uri.Split('/');
        return parts.LastOrDefault() ?? "";
    }

    private IEnumerable<SocialFeedItem> ParseBlueSkyPosts(string json, Account account)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var records = document.RootElement.GetProperty("records");

            var items = new List<SocialFeedItem>();
            foreach (var record in records.EnumerateArray())
            {
                var value = record.GetProperty("value");
                items.Add(new SocialFeedItem
                {
                    Id = record.GetProperty("uri").GetString() ?? "",
                    Content = value.GetProperty("text").GetString() ?? "",
                    AuthorUsername = account.Username,
                    AuthorName = account.DisplayName,
                    PostedAt = DateTime.Parse(value.GetProperty("createdAt").GetString() ?? DateTime.UtcNow.ToString()),
                    Platform = Platform
                });
            }

            return items;
        }
        catch
        {
            return Enumerable.Empty<SocialFeedItem>();
        }
    }

    private IEnumerable<SocialFeedItem> ParseBlueSkyFeed(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var feed = document.RootElement.GetProperty("feed");

            var items = new List<SocialFeedItem>();
            foreach (var item in feed.EnumerateArray())
            {
                var post = item.GetProperty("post");
                var record = post.GetProperty("record");
                var author = post.GetProperty("author");

                items.Add(new SocialFeedItem
                {
                    Id = post.GetProperty("uri").GetString() ?? "",
                    Content = record.GetProperty("text").GetString() ?? "",
                    AuthorUsername = author.GetProperty("handle").GetString() ?? "",
                    AuthorName = author.TryGetProperty("displayName", out var name) ? name.GetString() ?? "" : "",
                    PostedAt = DateTime.Parse(record.GetProperty("createdAt").GetString() ?? DateTime.UtcNow.ToString()),
                    Platform = Platform
                });
            }

            return items;
        }
        catch
        {
            return Enumerable.Empty<SocialFeedItem>();
        }
    }

    private IEnumerable<SocialFeedItem> ParseBlueSkySearchResults(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var posts = document.RootElement.GetProperty("posts");

            var items = new List<SocialFeedItem>();
            foreach (var post in posts.EnumerateArray())
            {
                var record = post.GetProperty("record");
                var author = post.GetProperty("author");

                items.Add(new SocialFeedItem
                {
                    Id = post.GetProperty("uri").GetString() ?? "",
                    Content = record.GetProperty("text").GetString() ?? "",
                    AuthorUsername = author.GetProperty("handle").GetString() ?? "",
                    AuthorName = author.TryGetProperty("displayName", out var name) ? name.GetString() ?? "" : "",
                    PostedAt = DateTime.Parse(record.GetProperty("createdAt").GetString() ?? DateTime.UtcNow.ToString()),
                    Platform = Platform
                });
            }

            return items;
        }
        catch
        {
            return Enumerable.Empty<SocialFeedItem>();
        }
    }
} 