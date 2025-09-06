using System.Text;
using System.Text.Json;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// LinkedIn specific service implementation
/// </summary>
public class LinkedInService : ILinkedInService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationService _authService;
    private const string BaseUrl = "https://api.linkedin.com/v2";

    public SocialPlatform Platform => SocialPlatform.LinkedIn;

    public LinkedInService(HttpClient httpClient, IAuthenticationService authService)
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
                author = $"urn:li:person:{userProfile.Id}",
                lifecycleState = "PUBLISHED",
                specificContent = new
                {
                    comLinkedinUgcShareContent = new
                    {
                        shareCommentary = new
                        {
                            text = post.FormatForPlatform(Platform)
                        },
                        shareMediaCategory = "NONE"
                    }
                },
                visibility = new
                {
                    comLinkedinUgcMemberNetworkVisibility = "PUBLIC"
                }
            };

            var json = JsonSerializer.Serialize(postData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/ugcPosts");
            request.Content = content;
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

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
                    ErrorMessage = $"LinkedIn API error: {responseContent}"
                };
            }
        }
        catch (Exception ex)
        {
            return new PublishResult
            {
                Success = false,
                ErrorMessage = $"Failed to post to LinkedIn: {ex.Message}"
            };
        }
    }

    public Task<PublishResult> PostThreadAsync(Post post, Account account)
    {
        // LinkedIn doesn't support native threads like Twitter
        // Post as a single long-form post
        return PostAsync(post, account);
    }

    public async Task<bool> DeletePostAsync(string postId, Account account)
    {
        try
        {
            if (!account.IsAuthenticated) return false;

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/ugcPosts/{postId}");
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

            var userProfile = await _authService.GetUserProfileAsync(Platform, account.Tokens!.AccessToken);
            if (userProfile == null) return Enumerable.Empty<SocialFeedItem>();

            var request = new HttpRequestMessage(HttpMethod.Get, 
                $"{BaseUrl}/ugcPosts?q=authors&authors=List(urn:li:person:{userProfile.Id})&count={limit}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return Enumerable.Empty<SocialFeedItem>();

            var json = await response.Content.ReadAsStringAsync();
            return ParseLinkedInPosts(json, account);
        }
        catch
        {
            return Enumerable.Empty<SocialFeedItem>();
        }
    }

    public Task<IEnumerable<SocialFeedItem>> GetTimelineAsync(Account account, int limit = 50)
    {
        // LinkedIn doesn't have a public timeline API for personal accounts
        // Return user's own posts instead
        return GetUserPostsAsync(account, limit);
    }

    public Task<IEnumerable<SocialFeedItem>> SearchPostsAsync(string query, Account account, int limit = 20)
    {
        // LinkedIn search API is limited - return empty for now
        return Task.FromResult(Enumerable.Empty<SocialFeedItem>());
    }

    public Task<IEnumerable<string>> GetTrendingHashtagsAsync(Account account)
    {
        // LinkedIn doesn't have a trending hashtags API
        return Task.FromResult(Enumerable.Empty<string>());
    }

    public async Task<ValidationResult> ValidateContentAsync(Post post)
    {
        var limits = await GetPlatformLimitsAsync();

        var result = new ValidationResult();

        // Check character limit (LinkedIn uses service-defined limit of 3000) on original content
        if (post.Content.Length > limits.CharacterLimit)
        {
            result.Errors.Add($"Content exceeds {limits.CharacterLimit} characters");
        }

        // LinkedIn supports up to 9 images or 1 video
        if (post.Media.Count > limits.MaxMediaCount)
        {
            result.Errors.Add($"LinkedIn supports maximum {limits.MaxMediaCount} media attachments");
        }

        return result;
    }

    public Task<string> UploadMediaAsync(Media media, Account account)
    {
        try
        {
            if (!account.IsAuthenticated) return Task.FromResult(string.Empty);

            // LinkedIn media upload implementation would go here
            // This is a placeholder - actual implementation would use LinkedIn's media upload API
            return Task.FromResult($"linkedin-media-{Guid.NewGuid()}");
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
            CharacterLimit = 3000,
            MaxMediaCount = 9,
            MaxMediaSize = 100000000, // 100MB
            SupportedMediaTypes = new[] { "image/jpeg", "image/png", "image/gif", "video/mp4" },
            MaxThreadLength = 1, // LinkedIn doesn't support threads
            PostingInterval = TimeSpan.FromMinutes(1),
            DailyPostLimit = 100
        });
    }

    public async Task<bool> SharePostAsync(string postId, Account account, string? commentary = null)
    {
        try
        {
            if (!account.IsAuthenticated) return false;

            var userProfile = await _authService.GetUserProfileAsync(Platform, account.Tokens!.AccessToken);
            if (userProfile == null) return false;

            var shareData = new
            {
                author = $"urn:li:person:{userProfile.Id}",
                lifecycleState = "PUBLISHED",
                specificContent = new
                {
                    comLinkedinUgcShareContent = new
                    {
                        shareCommentary = commentary != null ? new { text = commentary } : null,
                        shareMediaCategory = "NONE",
                        media = new[]
                        {
                            new
                            {
                                status = "READY",
                                originalUrl = $"https://linkedin.com/posts/{postId}"
                            }
                        }
                    }
                },
                visibility = new
                {
                    comLinkedinUgcMemberNetworkVisibility = "PUBLIC"
                }
            };

            var json = JsonSerializer.Serialize(shareData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/ugcPosts");
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

    public async Task<UserProfile?> GetProfileAsync(Account account)
    {
        try
        {
            if (!account.IsAuthenticated) return null;

            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/people/~");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return ParseLinkedInProfile(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<UserProfile>> GetConnectionsAsync(Account account)
    {
        return await GetConnectionsAsync(account, 50);
    }

    public async Task<IEnumerable<UserProfile>> GetConnectionsAsync(Account account, int limit = 50)
    {
        try
        {
            if (!account.IsAuthenticated) return Enumerable.Empty<UserProfile>();

            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/connections?count={limit}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return Enumerable.Empty<UserProfile>();

            var json = await response.Content.ReadAsStringAsync();
            return ParseLinkedInConnections(json);
        }
        catch
        {
            return Enumerable.Empty<UserProfile>();
        }
    }

    public async Task<IEnumerable<SocialFeedItem>> GetCompanyPostsAsync(string companyId, Account account, int limit = 20)
    {
        try
        {
            if (!account.IsAuthenticated) return Enumerable.Empty<SocialFeedItem>();

            var request = new HttpRequestMessage(HttpMethod.Get, 
                $"{BaseUrl}/ugcPosts?q=authors&authors=List(urn:li:organization:{companyId})&count={limit}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return Enumerable.Empty<SocialFeedItem>();

            var json = await response.Content.ReadAsStringAsync();
            return ParseLinkedInPosts(json, account);
        }
        catch
        {
            return Enumerable.Empty<SocialFeedItem>();
        }
    }

    public async Task<PublishResult> PostToCompanyPageAsync(Post post, Account account, string companyId)
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
                author = $"urn:li:organization:{companyId}",
                lifecycleState = "PUBLISHED",
                specificContent = new
                {
                    comLinkedinUgcShareContent = new
                    {
                        shareCommentary = new
                        {
                            text = post.FormatForPlatform(Platform)
                        },
                        shareMediaCategory = "NONE"
                    }
                },
                visibility = new
                {
                    comLinkedinUgcMemberNetworkVisibility = "PUBLIC"
                }
            };

            var json = JsonSerializer.Serialize(postData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/ugcPosts");
            request.Content = content;
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

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
                    ErrorMessage = $"LinkedIn API error: {responseContent}"
                };
            }
        }
        catch (Exception ex)
        {
            return new PublishResult
            {
                Success = false,
                ErrorMessage = $"Failed to post to LinkedIn company page: {ex.Message}"
            };
        }
    }

    public Task<RateLimitInfo> GetRateLimitInfoAsync(Account account)
    {
        try
        {
            if (!account.IsAuthenticated)
                return Task.FromResult(new RateLimitInfo { Remaining = 0, Limit = 0, ResetTime = DateTime.UtcNow });

            // LinkedIn rate limits are complex and vary by API
            // For now, return conservative default values
            return Task.FromResult(new RateLimitInfo
            {
                Remaining = 500,
                Limit = 500,
                ResetTime = DateTime.UtcNow.AddHours(1)
            });
        }
        catch
        {
            return Task.FromResult(new RateLimitInfo { Remaining = 0, Limit = 0, ResetTime = DateTime.UtcNow });
        }
    }

    private IEnumerable<SocialFeedItem> ParseLinkedInPosts(string json, Account account)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("elements", out var elements))
                return Enumerable.Empty<SocialFeedItem>();

            var items = new List<SocialFeedItem>();
            foreach (var post in elements.EnumerateArray())
            {
                var specificContent = post.GetProperty("specificContent")
                    .GetProperty("com.linkedin.ugc.ShareContent");
                
                var commentary = specificContent.TryGetProperty("shareCommentary", out var commentaryElement)
                    ? commentaryElement.GetProperty("text").GetString() ?? ""
                    : "";

                items.Add(new SocialFeedItem
                {
                    Id = post.GetProperty("id").GetString() ?? "",
                    Content = commentary,
                    AuthorUsername = account.Username,
                    AuthorName = account.DisplayName,
                    PostedAt = DateTime.Parse(post.GetProperty("created").GetProperty("time").GetString() ?? DateTime.UtcNow.ToString()),
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

    private UserProfile? ParseLinkedInProfile(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            return new UserProfile
            {
                Id = root.GetProperty("id").GetString() ?? "",
                Username = root.TryGetProperty("localizedFirstName", out var firstName) && 
                          root.TryGetProperty("localizedLastName", out var lastName)
                    ? $"{firstName.GetString()} {lastName.GetString()}"
                    : "",
                DisplayName = root.TryGetProperty("localizedFirstName", out var fName) && 
                             root.TryGetProperty("localizedLastName", out var lName)
                    ? $"{fName.GetString()} {lName.GetString()}"
                    : "",
                ProfileUrl = root.TryGetProperty("publicProfileUrl", out var url) ? url.GetString() : null,
                Platform = Platform
            };
        }
        catch
        {
            return null;
        }
    }

    private IEnumerable<UserProfile> ParseLinkedInConnections(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("values", out var values))
                return Enumerable.Empty<UserProfile>();

            var profiles = new List<UserProfile>();
            foreach (var connection in values.EnumerateArray())
            {
                profiles.Add(new UserProfile
                {
                    Id = connection.GetProperty("id").GetString() ?? "",
                    Username = connection.TryGetProperty("firstName", out var firstName) && 
                              connection.TryGetProperty("lastName", out var lastName)
                        ? $"{firstName.GetString()} {lastName.GetString()}"
                        : "",
                    DisplayName = connection.TryGetProperty("firstName", out var fName) && 
                                 connection.TryGetProperty("lastName", out var lName)
                        ? $"{fName.GetString()} {lName.GetString()}"
                        : "",
                    Platform = Platform
                });
            }

            return profiles;
        }
        catch
        {
            return Enumerable.Empty<UserProfile>();
        }
    }
} 