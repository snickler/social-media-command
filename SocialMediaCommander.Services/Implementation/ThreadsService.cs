using System.Text;
using System.Text.Json;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// Threads specific service implementation
/// </summary>
public class ThreadsService : IThreadsService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationService _authService;
    private const string BaseUrl = "https://graph.threads.net";

    public SocialPlatform Platform => SocialPlatform.Threads;

    public ThreadsService(HttpClient httpClient, IAuthenticationService authService)
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

            var userProfile = await _authService.GetUserProfileAsync(Platform, account.Tokens!.AccessToken);
            if (userProfile == null)
            {
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = "Could not retrieve user profile"
                };
            }

            var postData = new
            {
                media_type = "TEXT",
                text = post.FormatForPlatform(Platform),
                access_token = account.Tokens!.AccessToken
            };

            var json = JsonSerializer.Serialize(postData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1.0/{userProfile.Id}/threads");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var document = JsonDocument.Parse(responseContent);
                var id = document.RootElement.GetProperty("id").GetString();

                // Publish the created thread
                var publishData = new
                {
                    creation_id = id,
                    access_token = account.Tokens!.AccessToken
                };

                var publishJson = JsonSerializer.Serialize(publishData);
                var publishContent = new StringContent(publishJson, Encoding.UTF8, "application/json");

                var publishRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1.0/{userProfile.Id}/threads_publish");
                publishRequest.Content = publishContent;

                var publishResponse = await _httpClient.SendAsync(publishRequest);
                var publishResponseContent = await publishResponse.Content.ReadAsStringAsync();

                if (publishResponse.IsSuccessStatusCode)
                {
                    using var publishDocument = JsonDocument.Parse(publishResponseContent);
                    var publishedId = publishDocument.RootElement.GetProperty("id").GetString();

                    return new PublishResult
                    {
                        Success = true,
                        PlatformPostId = publishedId ?? "",
                        PublishedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    return new PublishResult
                    {
                        Success = false,
                        ErrorMessage = $"Threads publish error: {publishResponseContent}"
                    };
                }
            }
            else
            {
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = $"Threads API error: {responseContent}"
                };
            }
        }
        catch (Exception ex)
        {
            return new PublishResult
            {
                Success = false,
                ErrorMessage = $"Failed to post to Threads: {ex.Message}"
            };
        }
    }

    public async Task<PublishResult> PostThreadAsync(Post post, Account account)
    {
        // Implementation similar to main PostAsync since Threads handles threading natively
        return await PostAsync(post, account);
    }

    public async Task<bool> DeletePostAsync(string postId, Account account)
    {
        try
        {
            if (!account.IsAuthenticated) return false;

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/v1.0/{postId}?access_token={account.Tokens!.AccessToken}");
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

            var userProfile = await _authService.GetUserProfileAsync(Platform, account.Tokens!.AccessToken);
            if (userProfile == null) return Enumerable.Empty<SocialFeedItem>();

            var request = new HttpRequestMessage(HttpMethod.Get, 
                $"{BaseUrl}/v1.0/{userProfile.Id}/threads?fields=id,text,timestamp,permalink&limit={limit}&access_token={account.Tokens!.AccessToken}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return Enumerable.Empty<SocialFeedItem>();

            var json = await response.Content.ReadAsStringAsync();
            return ParseThreadsPosts(json, account);
        }
        catch
        {
            return Enumerable.Empty<SocialFeedItem>();
        }
    }

    public async Task<IEnumerable<SocialFeedItem>> GetTimelineAsync(Account account, int limit = 50)
    {
        // Threads API doesn't provide a timeline endpoint for third-party apps
        return await GetUserPostsAsync(account, limit);
    }

    public async Task<IEnumerable<SocialFeedItem>> SearchPostsAsync(string query, Account account, int limit = 20)
    {
        // Threads API doesn't provide a public search endpoint for third-party apps
        return Enumerable.Empty<SocialFeedItem>();
    }

    public async Task<IEnumerable<string>> GetTrendingHashtagsAsync(Account account)
    {
        // Threads API doesn't provide trending hashtags for third-party apps
        return Enumerable.Empty<string>();
    }

    public async Task<ValidationResult> ValidateContentAsync(Post post)
    {
        var config = PlatformConfigurations.GetPlatformConfig(Platform);
        var content = post.FormatForPlatform(Platform);

        var result = new ValidationResult();

        // Check character limit
        if (config.CharacterLimit.HasValue && content.Length > config.CharacterLimit.Value)
        {
            result.Errors.Add($"Content exceeds {config.CharacterLimit.Value} character limit");
        }

        // Check media count
        if (post.Media.Count > 10)
        {
            result.Errors.Add("Threads supports maximum 10 media attachments");
        }

        return result;
    }

    public async Task<string> UploadMediaAsync(Media media, Account account)
    {
        try
        {
            if (!account.IsAuthenticated) return "";

            // Threads media upload implementation placeholder
            return $"threads-media-{Guid.NewGuid()}";
        }
        catch
        {
            return "";
        }
    }

    public async Task<PlatformLimits> GetPlatformLimitsAsync()
    {
        return new PlatformLimits
        {
            CharacterLimit = 500,
            MaxMediaCount = 10,
            MaxMediaSize = 100000000, // 100MB
            SupportedMediaTypes = new[] { "image/jpeg", "image/png", "image/gif", "video/mp4" },
            MaxThreadLength = 500,
            PostingInterval = TimeSpan.FromSeconds(1),
            DailyPostLimit = 250
        };
    }

    public async Task<UserProfile?> GetProfileAsync(Account account)
    {
        try
        {
            if (!account.IsAuthenticated) return null;

            var userProfile = await _authService.GetUserProfileAsync(Platform, account.Tokens!.AccessToken);
            if (userProfile == null) return null;

            var request = new HttpRequestMessage(HttpMethod.Get, 
                $"{BaseUrl}/v1.0/{userProfile.Id}?fields=id,username,name,biography,followers_count&access_token={account.Tokens!.AccessToken}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return ParseThreadsProfile(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task<RateLimitInfo> GetRateLimitInfoAsync(Account account)
    {
        try
        {
            if (!account.IsAuthenticated) 
                return new RateLimitInfo { Remaining = 0, Limit = 0, ResetTime = DateTime.UtcNow };

            return new RateLimitInfo
            {
                Remaining = 200,
                Limit = 250,
                ResetTime = DateTime.UtcNow.AddHours(1)
            };
        }
        catch
        {
            return new RateLimitInfo { Remaining = 0, Limit = 0, ResetTime = DateTime.UtcNow };
        }
    }

    public async Task<bool> LikeAsync(string postId, Account account)
    {
        try
        {
            if (!account.IsAuthenticated) return false;

            var userProfile = await _authService.GetUserProfileAsync(Platform, account.Tokens!.AccessToken);
            if (userProfile == null) return false;

            var request = new HttpRequestMessage(HttpMethod.Post, 
                $"{BaseUrl}/v1.0/{userProfile.Id}/threads_likes?access_token={account.Tokens!.AccessToken}");
            
            var likeData = new { thread_id = postId };
            var json = JsonSerializer.Serialize(likeData);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RepostAsync(string postId, Account account)
    {
        try
        {
            if (!account.IsAuthenticated) return false;

            var userProfile = await _authService.GetUserProfileAsync(Platform, account.Tokens!.AccessToken);
            if (userProfile == null) return false;

            // Threads reposts are done by creating a new thread that quotes the original
            var repostData = new
            {
                media_type = "TEXT",
                text = "",
                quote_post_id = postId,
                access_token = account.Tokens!.AccessToken
            };

            var json = JsonSerializer.Serialize(repostData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1.0/{userProfile.Id}/threads");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private IEnumerable<SocialFeedItem> ParseThreadsPosts(string json, Account account)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("data", out var data))
                return Enumerable.Empty<SocialFeedItem>();

            var items = new List<SocialFeedItem>();
            foreach (var post in data.EnumerateArray())
            {
                var text = post.TryGetProperty("text", out var textElement) ? textElement.GetString() ?? "" : "";
                var timestamp = post.TryGetProperty("timestamp", out var timestampElement) ? timestampElement.GetString() : null;
                var permalink = post.TryGetProperty("permalink", out var permalinkElement) ? permalinkElement.GetString() : null;

                items.Add(new SocialFeedItem
                {
                    Id = post.GetProperty("id").GetString() ?? "",
                    Content = text,
                    AuthorUsername = account.Username,
                    AuthorName = account.DisplayName,
                    PostedAt = DateTime.TryParse(timestamp ?? "", out var parsedTime) ? parsedTime : DateTime.UtcNow,
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

    private UserProfile? ParseThreadsProfile(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            return new UserProfile
            {
                Id = root.GetProperty("id").GetString() ?? "",
                Username = root.TryGetProperty("username", out var username) ? username.GetString() ?? "" : "",
                DisplayName = root.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Bio = root.TryGetProperty("biography", out var bio) ? bio.GetString() ?? "" : "",
                FollowerCount = root.TryGetProperty("followers_count", out var followers) ? followers.GetInt32() : 0,
                Platform = Platform,
                CreatedAt = DateTime.UtcNow
            };
        }
        catch
        {
            return null;
        }
    }
} 