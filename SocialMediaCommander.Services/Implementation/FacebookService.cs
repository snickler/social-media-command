using System.Text;
using System.Text.Json;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// Facebook specific service implementation
/// </summary>
public class FacebookService : IFacebookService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationService _authService;
    private const string BaseUrl = "https://graph.facebook.com/v18.0";

    public SocialPlatform Platform => SocialPlatform.Facebook;

    public FacebookService(HttpClient httpClient, IAuthenticationService authService)
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
                message = post.FormatForPlatform(Platform),
                access_token = account.Tokens!.AccessToken
            };

            var json = JsonSerializer.Serialize(postData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/{userProfile.Id}/feed");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var document = JsonDocument.Parse(responseContent);
                var id = document.RootElement.GetProperty("id").GetString();

                return new PublishResult
                {
                    Success = true,
                    PlatformPostId = id ?? "",
                    PublishedAt = DateTime.UtcNow
                };
            }
            else
            {
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = $"Facebook API error: {responseContent}"
                };
            }
        }
        catch (Exception ex)
        {
            return new PublishResult
            {
                Success = false,
                ErrorMessage = $"Failed to post to Facebook: {ex.Message}"
            };
        }
    }

    public Task<PublishResult> PostThreadAsync(Post post, Account account)
    {
        // Facebook doesn't support native threads, post as single message
        return PostAsync(post, account);
    }

    public async Task<bool> DeletePostAsync(string postId, Account account)
    {
        try
        {
            if (!account.IsAuthenticated) return false;

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{postId}?access_token={account.Tokens!.AccessToken}");
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
                $"{BaseUrl}/{userProfile.Id}/posts?fields=id,message,created_time,permalink_url&limit={limit}&access_token={account.Tokens!.AccessToken}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return Enumerable.Empty<SocialFeedItem>();

            var json = await response.Content.ReadAsStringAsync();
            return ParseFacebookPosts(json, account);
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

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{BaseUrl}/me/feed?fields=id,message,created_time,from&limit={limit}&access_token={account.Tokens!.AccessToken}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return Enumerable.Empty<SocialFeedItem>();

            var json = await response.Content.ReadAsStringAsync();
            return ParseFacebookFeed(json);
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

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{BaseUrl}/search?q={Uri.EscapeDataString(query)}&type=post&fields=id,message,created_time,from&limit={limit}&access_token={account.Tokens!.AccessToken}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return Enumerable.Empty<SocialFeedItem>();

            var json = await response.Content.ReadAsStringAsync();
            return ParseFacebookSearchResults(json);
        }
        catch
        {
            return Enumerable.Empty<SocialFeedItem>();
        }
    }

    public Task<IEnumerable<string>> GetTrendingHashtagsAsync(Account account)
    {
        // Facebook doesn't provide a trending hashtags API for third-party apps
        return Task.FromResult(Enumerable.Empty<string>());
    }

    public Task<ValidationResult> ValidateContentAsync(Post post)
    {
        var config = PlatformConfigurations.GetPlatformConfig(Platform);
        var content = post.FormatForPlatform(Platform);

        var result = new ValidationResult();

        // Facebook doesn't have a strict character limit, but very long posts may be truncated
        if (content.Length > 63206)
        {
            result.Errors.Add("Content exceeds Facebook's recommended character limit");
        }

        // Check media count
        if (post.Media.Count > 10)
        {
            result.Errors.Add("Facebook supports maximum 10 media attachments per post");
        }

        return Task.FromResult(result);
    }

    public Task<string> UploadMediaAsync(Media media, Account account)
    {
        try
        {
            if (!account.IsAuthenticated) return Task.FromResult(string.Empty);

            // Facebook media upload implementation placeholder
            return Task.FromResult($"facebook-media-{Guid.NewGuid()}");
        }
        catch
        {
            return Task.FromResult(string.Empty);
        }
    }

    public Task<PlatformLimits> GetPlatformLimitsAsync()
    {
        return Task.FromResult(new PlatformLimits
        {
            CharacterLimit = null, // Facebook doesn't have a strict character limit
            MaxMediaCount = 10,
            MaxMediaSize = 1000000000, // 1GB
            SupportedMediaTypes = new[] { "image/jpeg", "image/png", "image/gif", "video/mp4", "video/mov" },
            MaxThreadLength = 1, // Facebook doesn't support threads
            PostingInterval = TimeSpan.FromMinutes(1),
            DailyPostLimit = 200
        });
    }

    public async Task<UserProfile?> GetProfileAsync(Account account)
    {
        try
        {
            if (!account.IsAuthenticated) return null;

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{BaseUrl}/me?fields=id,name,email,picture&access_token={account.Tokens!.AccessToken}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return ParseFacebookProfile(json);
        }
        catch
        {
            return null;
        }
    }

    public Task<RateLimitInfo> GetRateLimitInfoAsync(Account account)
    {
        try
        {
            if (!account.IsAuthenticated)
                return Task.FromResult(new RateLimitInfo { Remaining = 0, Limit = 0, ResetTime = DateTime.UtcNow });

            // Facebook has rate limits but doesn't expose them in headers like Twitter
            return Task.FromResult(new RateLimitInfo
            {
                Remaining = 180,
                Limit = 200,
                ResetTime = DateTime.UtcNow.AddHours(1)
            });
        }
        catch
        {
            return Task.FromResult(new RateLimitInfo { Remaining = 0, Limit = 0, ResetTime = DateTime.UtcNow });
        }
    }

    public async Task<bool> LikeAsync(string postId, Account account)
    {
        try
        {
            if (!account.IsAuthenticated) return false;

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{BaseUrl}/{postId}/likes?access_token={account.Tokens!.AccessToken}");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ShareAsync(string postId, Account account)
    {
        try
        {
            if (!account.IsAuthenticated) return false;

            var userProfile = await _authService.GetUserProfileAsync(Platform, account.Tokens!.AccessToken);
            if (userProfile == null) return false;

            var shareData = new
            {
                link = $"https://facebook.com/{postId}",
                access_token = account.Tokens!.AccessToken
            };

            var json = JsonSerializer.Serialize(shareData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/{userProfile.Id}/feed");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<PublishResult> PostToPageAsync(Post post, Account account, string pageId)
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

            var postData = new
            {
                message = post.FormatForPlatform(Platform),
                access_token = account.Tokens!.AccessToken
            };

            var json = JsonSerializer.Serialize(postData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/{pageId}/feed");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var document = JsonDocument.Parse(responseContent);
                var id = document.RootElement.GetProperty("id").GetString();

                return new PublishResult
                {
                    Success = true,
                    PlatformPostId = id ?? "",
                    PublishedAt = DateTime.UtcNow
                };
            }
            else
            {
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = $"Facebook page post error: {responseContent}"
                };
            }
        }
        catch (Exception ex)
        {
            return new PublishResult
            {
                Success = false,
                ErrorMessage = $"Failed to post to Facebook page: {ex.Message}"
            };
        }
    }

    public async Task<IEnumerable<SocialFeedItem>> GetPagePostsAsync(string pageId, Account account, int limit = 20)
    {
        try
        {
            if (!account.IsAuthenticated) return Enumerable.Empty<SocialFeedItem>();

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{BaseUrl}/{pageId}/posts?fields=id,message,created_time,from&limit={limit}&access_token={account.Tokens!.AccessToken}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return Enumerable.Empty<SocialFeedItem>();

            var json = await response.Content.ReadAsStringAsync();
            return ParseFacebookPosts(json, account);
        }
        catch
        {
            return Enumerable.Empty<SocialFeedItem>();
        }
    }

    private IEnumerable<SocialFeedItem> ParseFacebookPosts(string json, Account account)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("data", out var data))
                return Enumerable.Empty<SocialFeedItem>();

            var items = new List<SocialFeedItem>();
            foreach (var post in data.EnumerateArray())
            {
                var message = post.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? "" : "";
                var createdTime = post.TryGetProperty("created_time", out var timeElement) ? timeElement.GetString() : null;

                items.Add(new SocialFeedItem
                {
                    Id = post.GetProperty("id").GetString() ?? "",
                    Content = message,
                    AuthorUsername = account.Username,
                    AuthorName = account.DisplayName,
                    PostedAt = DateTime.TryParse(createdTime ?? "", out var parsedTime) ? parsedTime : DateTime.UtcNow,
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

    private IEnumerable<SocialFeedItem> ParseFacebookFeed(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("data", out var data))
                return Enumerable.Empty<SocialFeedItem>();

            var items = new List<SocialFeedItem>();
            foreach (var post in data.EnumerateArray())
            {
                var message = post.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? "" : "";
                var createdTime = post.TryGetProperty("created_time", out var timeElement) ? timeElement.GetString() : null;
                var from = post.TryGetProperty("from", out var fromElement) ? fromElement : (JsonElement?)null;

                var authorName = from?.TryGetProperty("name", out var nameElement) == true ? nameElement.GetString() ?? "" : "";
                var authorId = from?.TryGetProperty("id", out var idElement) == true ? idElement.GetString() ?? "" : "";

                items.Add(new SocialFeedItem
                {
                    Id = post.GetProperty("id").GetString() ?? "",
                    Content = message,
                    AuthorUsername = authorId,
                    AuthorName = authorName,
                    PostedAt = DateTime.TryParse(createdTime ?? "", out var parsedTime) ? parsedTime : DateTime.UtcNow,
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

    private IEnumerable<SocialFeedItem> ParseFacebookSearchResults(string json)
    {
        return ParseFacebookFeed(json); // Same structure as feed
    }

    private UserProfile? ParseFacebookProfile(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var pictureUrl = "";
            if (root.TryGetProperty("picture", out var picture) &&
                picture.TryGetProperty("data", out var pictureData) &&
                pictureData.TryGetProperty("url", out var url))
            {
                pictureUrl = url.GetString() ?? "";
            }

            return new UserProfile
            {
                Id = root.GetProperty("id").GetString() ?? "",
                Username = root.GetProperty("id").GetString() ?? "", // Facebook uses ID as username
                DisplayName = root.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Avatar = pictureUrl,
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