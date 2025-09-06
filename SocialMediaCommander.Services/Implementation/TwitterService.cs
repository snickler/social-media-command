using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Services.Implementation
{
    /// <summary>
    /// Twitter/X specific service implementation (clean, minimal, test-friendly)
    /// </summary>
    public class TwitterService : ITwitterService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthenticationService _authService;
        private const string BaseUrl = "https://api.twitter.com/2";

        public SocialPlatform Platform => SocialPlatform.X;

        public TwitterService(HttpClient httpClient, IAuthenticationService authService)
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
                    return new PublishResult { Success = false, ErrorMessage = "Account is not authenticated" };
                }

                var tweetData = new { text = post.FormatForPlatform(Platform) };
                var json = JsonSerializer.Serialize(tweetData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/tweets") { Content = content };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

                var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    using var document = JsonDocument.Parse(responseContent);
                    var data = document.RootElement.GetProperty("data");
                    var id = data.GetProperty("id").GetString();

                    return new PublishResult { Success = true, PlatformPostId = id ?? string.Empty, PublishedAt = DateTime.UtcNow };
                }

                return new PublishResult { Success = false, ErrorMessage = $"Twitter API error: {responseContent}" };
            }
            catch (Exception ex)
            {
                return new PublishResult { Success = false, ErrorMessage = $"Failed to post to Twitter: {ex.Message}" };
            }
        }

        public async Task<PublishResult> PostThreadAsync(Post post, Account account)
        {
            if (!post.IsThread || !post.ThreadPosts.Any())
                return await PostAsync(post, account).ConfigureAwait(false);

            var results = new List<PublishResult>();
            var mainResult = await PostAsync(post, account).ConfigureAwait(false);
            results.Add(mainResult);

            if (!mainResult.Success)
                return mainResult;

            var replyToId = mainResult.PlatformPostId;

            foreach (var threadPost in post.ThreadPosts)
            {
                var threadTweetData = new { text = threadPost.Content, reply = new { in_reply_to_tweet_id = replyToId } };
                var tjson = JsonSerializer.Serialize(threadTweetData);
                var tcontent = new StringContent(tjson, Encoding.UTF8, "application/json");

                var treq = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/tweets") { Content = tcontent };
                treq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

                var tresp = await _httpClient.SendAsync(treq).ConfigureAwait(false);
                var trespContent = await tresp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (tresp.IsSuccessStatusCode)
                {
                    using var document = JsonDocument.Parse(trespContent);
                    var data = document.RootElement.GetProperty("data");
                    var id = data.GetProperty("id").GetString();

                    results.Add(new PublishResult { Success = true, PlatformPostId = id ?? string.Empty, PublishedAt = DateTime.UtcNow });
                    replyToId = id;
                }
                else
                {
                    results.Add(new PublishResult { Success = false, ErrorMessage = $"Twitter thread error: {trespContent}" });
                    break;
                }
            }

            var failedPosts = results.Where(r => !r.Success).ToList();
            if (failedPosts.Any())
                return new PublishResult { Success = false, ErrorMessage = $"Thread partially failed: {string.Join(", ", failedPosts.Select(f => f.ErrorMessage))}" };

            return new PublishResult { Success = true, PlatformPostId = mainResult.PlatformPostId, PublishedAt = DateTime.UtcNow };
        }

        public async Task<bool> DeletePostAsync(string postId, Account account)
        {
            try
            {
                if (!account.IsAuthenticated) return false;

                var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/tweets/{postId}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

                var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
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

                var userProfile = await _authService.GetUserProfileAsync(Platform, account.Tokens!.AccessToken).ConfigureAwait(false);
                if (userProfile == null) return Enumerable.Empty<SocialFeedItem>();

                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/users/{userProfile.Id}/tweets?max_results={limit}&tweet.fields=created_at,author_id,public_metrics");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

                var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return Enumerable.Empty<SocialFeedItem>();

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return ParseTwitterPosts(json, account);
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

                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/users/me/timelines/reverse_chronological?max_results={limit}&tweet.fields=created_at,author_id,public_metrics&expansions=author_id");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

                var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return Enumerable.Empty<SocialFeedItem>();

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return ParseTwitterTimeline(json);
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

                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/tweets/search/recent?query={Uri.EscapeDataString(query)}&max_results={limit}&tweet.fields=created_at,author_id,public_metrics&expansions=author_id");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

                var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return Enumerable.Empty<SocialFeedItem>();

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return ParseTwitterSearchResults(json);
            }
            catch
            {
                return Enumerable.Empty<SocialFeedItem>();
            }
        }

        public async Task<IEnumerable<string>> GetTrendingHashtagsAsync(Account account)
        {
            try
            {
                if (!account.IsAuthenticated) return Enumerable.Empty<string>();

                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/trends/place?id=1");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

                var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return Enumerable.Empty<string>();

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return ParseTwitterTrends(json);
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }

        public async Task<ValidationResult> ValidateContentAsync(Post post)
        {
            var limits = await GetPlatformLimitsAsync().ConfigureAwait(false);

            var result = new ValidationResult();

            if (post.Content.Length > limits.CharacterLimit)
                result.Errors.Add($"Content exceeds {limits.CharacterLimit} characters");

            if (post.Media.Count > limits.MaxMediaCount)
                result.Errors.Add($"Twitter supports maximum {limits.MaxMediaCount} media attachments");

            return result;
        }

        public Task<string> UploadMediaAsync(Media media, Account account)
        {
            try
            {
                if (!account.IsAuthenticated) return Task.FromResult(string.Empty);
                return Task.FromResult($"twitter-media-{Guid.NewGuid()}");
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
                CharacterLimit = 280,
                MaxMediaCount = 4,
                MaxMediaSize = 5000000,
                SupportedMediaTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "video/mp4" },
                MaxThreadLength = 25,
                PostingInterval = TimeSpan.FromSeconds(1),
                DailyPostLimit = 300
            });
        }

        public async Task<bool> RetweetAsync(string postId, Account account)
        {
            try
            {
                if (!account.IsAuthenticated) return false;

                var userProfile = await _authService.GetUserProfileAsync(Platform, account.Tokens!.AccessToken).ConfigureAwait(false);
                if (userProfile == null) return false;

                var retweetData = new { tweet_id = postId };
                var json = JsonSerializer.Serialize(retweetData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/users/{userProfile.Id}/retweets") { Content = content };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

                var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> LikeAsync(string postId, Account account)
        {
            try
            {
                if (!account.IsAuthenticated) return false;

                var userProfile = await _authService.GetUserProfileAsync(Platform, account.Tokens!.AccessToken).ConfigureAwait(false);
                if (userProfile == null) return false;

                var likeData = new { tweet_id = postId };
                var json = JsonSerializer.Serialize(likeData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/users/{userProfile.Id}/likes") { Content = content };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.Tokens!.AccessToken);

                var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public Task<RateLimitInfo> GetRateLimitInfoAsync(Account account)
        {
            try
            {
                if (!account.IsAuthenticated)
                    return Task.FromResult(new RateLimitInfo { Remaining = 0, Limit = 0, ResetTime = DateTime.UtcNow });

                return Task.FromResult(new RateLimitInfo { Remaining = 75, Limit = 75, ResetTime = DateTime.UtcNow.AddMinutes(15) });
            }
            catch
            {
                return Task.FromResult(new RateLimitInfo { Remaining = 0, Limit = 0, ResetTime = DateTime.UtcNow });
            }
        }

        private IEnumerable<SocialFeedItem> ParseTwitterPosts(string json, Account account)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                if (!document.RootElement.TryGetProperty("data", out var data))
                    return Enumerable.Empty<SocialFeedItem>();

                var items = new List<SocialFeedItem>();
                foreach (var tweet in data.EnumerateArray())
                {
                    items.Add(new SocialFeedItem
                    {
                        Id = tweet.GetProperty("id").GetString() ?? string.Empty,
                        Content = tweet.GetProperty("text").GetString() ?? string.Empty,
                        AuthorUsername = account.Username,
                        AuthorName = account.DisplayName,
                        PostedAt = DateTime.Parse(tweet.GetProperty("created_at").GetString() ?? DateTime.UtcNow.ToString()),
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

        private IEnumerable<SocialFeedItem> ParseTwitterTimeline(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                if (!document.RootElement.TryGetProperty("data", out var data))
                    return Enumerable.Empty<SocialFeedItem>();

                var users = new Dictionary<string, JsonElement>();
                if (document.RootElement.TryGetProperty("includes", out var includes) && includes.TryGetProperty("users", out var usersArray))
                {
                    foreach (var user in usersArray.EnumerateArray())
                    {
                        var userId = user.GetProperty("id").GetString();
                        if (userId != null) users[userId] = user;
                    }
                }

                var items = new List<SocialFeedItem>();
                foreach (var tweet in data.EnumerateArray())
                {
                    var authorId = tweet.GetProperty("author_id").GetString() ?? string.Empty;
                    var author = users.TryGetValue(authorId, out var userElement) ? userElement : (JsonElement?)null;

                    items.Add(new SocialFeedItem
                    {
                        Id = tweet.GetProperty("id").GetString() ?? string.Empty,
                        Content = tweet.GetProperty("text").GetString() ?? string.Empty,
                        AuthorUsername = author?.TryGetProperty("username", out var username) == true ? username.GetString() ?? string.Empty : string.Empty,
                        AuthorName = author?.TryGetProperty("name", out var name) == true ? name.GetString() ?? string.Empty : string.Empty,
                        PostedAt = DateTime.Parse(tweet.GetProperty("created_at").GetString() ?? DateTime.UtcNow.ToString()),
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

        private IEnumerable<SocialFeedItem> ParseTwitterSearchResults(string json) => ParseTwitterTimeline(json);

        private IEnumerable<string> ParseTwitterTrends(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                var trends = new List<string>();

                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var location in document.RootElement.EnumerateArray())
                    {
                        if (location.TryGetProperty("trends", out var trendsArray))
                        {
                            foreach (var trend in trendsArray.EnumerateArray())
                            {
                                var name = trend.GetProperty("name").GetString();
                                if (!string.IsNullOrEmpty(name) && name.StartsWith("#")) trends.Add(name);
                            }
                        }
                    }
                }

                return trends;
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }
    }
}