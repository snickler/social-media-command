using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// OAuth authentication service for social media platforms
/// </summary>
public class OAuthAuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<SocialPlatform, OAuthConfig> _oauthConfigs;

    public OAuthAuthenticationService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _oauthConfigs = InitializeOAuthConfigs();
    }

    public async Task<AuthenticationResult> StartAuthenticationAsync(SocialPlatform platform, string? redirectUri = null)
    {
        try
        {
            var config = GetOAuthConfig(platform);
            if (string.IsNullOrEmpty(config.ClientId))
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"OAuth not configured for {platform}"
                };
            }

            var state = GenerateRandomString(32);
            var codeVerifier = GenerateRandomString(128);
            var codeChallenge = GenerateCodeChallenge(codeVerifier);

            var authUrl = BuildAuthorizationUrl(config, redirectUri ?? config.RedirectUri, state, codeChallenge);

            return new AuthenticationResult
            {
                IsSuccess = true,
                AuthorizationUrl = authUrl,
                State = state
            };
        }
        catch (Exception ex)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to start authentication: {ex.Message}"
            };
        }
    }

    public async Task<AuthenticationResult> CompleteAuthenticationAsync(SocialPlatform platform, string authorizationCode, string? state = null)
    {
        try
        {
            var config = GetOAuthConfig(platform);
            var tokenResponse = await ExchangeCodeForTokenAsync(config, authorizationCode);

            if (tokenResponse == null)
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to exchange authorization code for tokens"
                };
            }

            var userProfile = await GetUserProfileAsync(platform, tokenResponse.AccessToken);

            return new AuthenticationResult
            {
                IsSuccess = true,
                Tokens = tokenResponse,
                UserProfile = userProfile
            };
        }
        catch (Exception ex)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to complete authentication: {ex.Message}"
            };
        }
    }

    public async Task<AuthenticationResult> RefreshTokenAsync(SocialPlatform platform, string refreshToken)
    {
        try
        {
            var config = GetOAuthConfig(platform);
            var tokens = await RefreshAccessTokenAsync(config, refreshToken);

            if (tokens == null)
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to refresh access token"
                };
            }

            return new AuthenticationResult
            {
                IsSuccess = true,
                Tokens = tokens
            };
        }
        catch (Exception ex)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to refresh token: {ex.Message}"
            };
        }
    }

    public async Task<bool> RevokeTokenAsync(SocialPlatform platform, string accessToken)
    {
        try
        {
            var config = GetOAuthConfig(platform);
            if (string.IsNullOrEmpty(config.RevokeEndpoint))
                return true; // Platform doesn't support revocation

            var request = new HttpRequestMessage(HttpMethod.Post, config.RevokeEndpoint);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", accessToken),
                new KeyValuePair<string, string>("client_id", config.ClientId),
                new KeyValuePair<string, string>("client_secret", config.ClientSecret)
            });

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ValidateTokenAsync(SocialPlatform platform, string accessToken)
    {
        try
        {
            var userProfile = await GetUserProfileAsync(platform, accessToken);
            return userProfile != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserProfile?> GetUserProfileAsync(SocialPlatform platform, string accessToken)
    {
        try
        {
            var config = GetOAuthConfig(platform);
            if (string.IsNullOrEmpty(config.UserInfoEndpoint))
                return null;

            var request = new HttpRequestMessage(HttpMethod.Get, config.UserInfoEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return ParseUserProfile(platform, json);
        }
        catch
        {
            return null;
        }
    }

    public OAuthConfig GetOAuthConfig(SocialPlatform platform)
    {
        return _oauthConfigs.TryGetValue(platform, out var config) ? config : new OAuthConfig();
    }

    private Dictionary<SocialPlatform, OAuthConfig> InitializeOAuthConfigs()
    {
        return new Dictionary<SocialPlatform, OAuthConfig>
        {
            {
                SocialPlatform.BlueSky,
                new OAuthConfig
                {
                    ClientId = Environment.GetEnvironmentVariable("BLUESKY_CLIENT_ID") ?? "",
                    ClientSecret = Environment.GetEnvironmentVariable("BLUESKY_CLIENT_SECRET") ?? "",
                    AuthorizationEndpoint = "https://bsky.social/xrpc/com.atproto.server.createSession",
                    TokenEndpoint = "https://bsky.social/xrpc/com.atproto.server.createSession",
                    UserInfoEndpoint = "https://bsky.social/xrpc/com.atproto.server.getSession",
                    Scopes = new[] { "read", "write" },
                    RedirectUri = "http://localhost:8080/callback/bluesky"
                }
            },
            {
                SocialPlatform.X,
                new OAuthConfig
                {
                    ClientId = Environment.GetEnvironmentVariable("TWITTER_CLIENT_ID") ?? "",
                    ClientSecret = Environment.GetEnvironmentVariable("TWITTER_CLIENT_SECRET") ?? "",
                    AuthorizationEndpoint = "https://twitter.com/i/oauth2/authorize",
                    TokenEndpoint = "https://api.twitter.com/2/oauth2/token",
                    UserInfoEndpoint = "https://api.twitter.com/2/users/me",
                    RevokeEndpoint = "https://api.twitter.com/2/oauth2/revoke",
                    Scopes = new[] { "tweet.read", "tweet.write", "users.read", "offline.access" },
                    RedirectUri = "http://localhost:8080/callback/twitter"
                }
            },
            {
                SocialPlatform.LinkedIn,
                new OAuthConfig
                {
                    ClientId = Environment.GetEnvironmentVariable("LINKEDIN_CLIENT_ID") ?? "",
                    ClientSecret = Environment.GetEnvironmentVariable("LINKEDIN_CLIENT_SECRET") ?? "",
                    AuthorizationEndpoint = "https://www.linkedin.com/oauth/v2/authorization",
                    TokenEndpoint = "https://www.linkedin.com/oauth/v2/accessToken",
                    UserInfoEndpoint = "https://api.linkedin.com/v2/people/~",
                    Scopes = new[] { "r_liteprofile", "r_emailaddress", "w_member_social" },
                    RedirectUri = "http://localhost:8080/callback/linkedin"
                }
            },
            {
                SocialPlatform.Threads,
                new OAuthConfig
                {
                    ClientId = Environment.GetEnvironmentVariable("THREADS_CLIENT_ID") ?? "",
                    ClientSecret = Environment.GetEnvironmentVariable("THREADS_CLIENT_SECRET") ?? "",
                    AuthorizationEndpoint = "https://threads.net/oauth/authorize",
                    TokenEndpoint = "https://graph.threads.net/oauth/access_token",
                    UserInfoEndpoint = "https://graph.threads.net/v1.0/me",
                    Scopes = new[] { "threads_basic", "threads_content_publish" },
                    RedirectUri = "http://localhost:8080/callback/threads"
                }
            },
            {
                SocialPlatform.Facebook,
                new OAuthConfig
                {
                    ClientId = Environment.GetEnvironmentVariable("FACEBOOK_CLIENT_ID") ?? "",
                    ClientSecret = Environment.GetEnvironmentVariable("FACEBOOK_CLIENT_SECRET") ?? "",
                    AuthorizationEndpoint = "https://www.facebook.com/v18.0/dialog/oauth",
                    TokenEndpoint = "https://graph.facebook.com/v18.0/oauth/access_token",
                    UserInfoEndpoint = "https://graph.facebook.com/v18.0/me",
                    Scopes = new[] { "pages_manage_posts", "pages_read_engagement", "public_profile" },
                    RedirectUri = "http://localhost:8080/callback/facebook"
                }
            }
        };
    }

    private string BuildAuthorizationUrl(OAuthConfig config, string redirectUri, string state, string codeChallenge)
    {
        var parameters = new Dictionary<string, string>
        {
            { "client_id", config.ClientId },
            { "redirect_uri", redirectUri },
            { "scope", string.Join(" ", config.Scopes) },
            { "state", state },
            { "response_type", "code" }
        };

        // Add PKCE parameters for platforms that support it
        if (!string.IsNullOrEmpty(codeChallenge))
        {
            parameters.Add("code_challenge", codeChallenge);
            parameters.Add("code_challenge_method", "S256");
        }

        // Add additional platform-specific parameters
        foreach (var param in config.AdditionalParameters)
        {
            parameters[param.Key] = param.Value;
        }

        var queryString = string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        return $"{config.AuthorizationEndpoint}?{queryString}";
    }

    private async Task<OAuthTokens?> ExchangeCodeForTokenAsync(OAuthConfig config, string authorizationCode)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, config.TokenEndpoint);
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", config.ClientId),
            new KeyValuePair<string, string>("client_secret", config.ClientSecret),
            new KeyValuePair<string, string>("code", authorizationCode),
            new KeyValuePair<string, string>("redirect_uri", config.RedirectUri)
        });

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return ParseTokenResponse(json);
    }

    private async Task<OAuthTokens?> RefreshAccessTokenAsync(OAuthConfig config, string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, config.TokenEndpoint);
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", config.ClientId),
            new KeyValuePair<string, string>("client_secret", config.ClientSecret),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return ParseTokenResponse(json);
    }

    private OAuthTokens? ParseTokenResponse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var accessToken = root.GetProperty("access_token").GetString();
            if (string.IsNullOrEmpty(accessToken))
                return null;

            var expiresIn = root.TryGetProperty("expires_in", out var expiresInElement) ? expiresInElement.GetInt32() : 3600;
            var refreshToken = root.TryGetProperty("refresh_token", out var refreshTokenElement) ? refreshTokenElement.GetString() : null;
            var tokenType = root.TryGetProperty("token_type", out var tokenTypeElement) ? tokenTypeElement.GetString() : "Bearer";

            return new OAuthTokens
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn),
                TokenType = tokenType ?? "Bearer"
            };
        }
        catch
        {
            return null;
        }
    }

    private UserProfile? ParseUserProfile(SocialPlatform platform, string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            return platform switch
            {
                SocialPlatform.X => ParseTwitterProfile(root),
                SocialPlatform.LinkedIn => ParseLinkedInProfile(root),
                SocialPlatform.Facebook => ParseFacebookProfile(root),
                SocialPlatform.Threads => ParseThreadsProfile(root),
                SocialPlatform.BlueSky => ParseBlueSkyProfile(root),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private UserProfile ParseTwitterProfile(JsonElement root)
    {
        var data = root.GetProperty("data");
        return new UserProfile
        {
            Id = data.GetProperty("id").GetString() ?? "",
            Username = data.GetProperty("username").GetString() ?? "",
            DisplayName = data.GetProperty("name").GetString() ?? "",
            Avatar = data.TryGetProperty("profile_image_url", out var avatar) ? avatar.GetString() : null
        };
    }

    private UserProfile ParseLinkedInProfile(JsonElement root)
    {
        return new UserProfile
        {
            Id = root.GetProperty("id").GetString() ?? "",
            DisplayName = $"{root.GetProperty("localizedFirstName").GetString()} {root.GetProperty("localizedLastName").GetString()}",
            Username = root.TryGetProperty("vanityName", out var vanity) ? vanity.GetString() ?? "" : ""
        };
    }

    private UserProfile ParseFacebookProfile(JsonElement root)
    {
        return new UserProfile
        {
            Id = root.GetProperty("id").GetString() ?? "",
            DisplayName = root.GetProperty("name").GetString() ?? "",
            Username = root.TryGetProperty("username", out var username) ? username.GetString() ?? "" : ""
        };
    }

    private UserProfile ParseThreadsProfile(JsonElement root)
    {
        return new UserProfile
        {
            Id = root.GetProperty("id").GetString() ?? "",
            Username = root.GetProperty("username").GetString() ?? "",
            DisplayName = root.TryGetProperty("name", out var name) ? name.GetString() ?? "" : ""
        };
    }

    private UserProfile ParseBlueSkyProfile(JsonElement root)
    {
        return new UserProfile
        {
            Id = root.GetProperty("did").GetString() ?? "",
            Username = root.GetProperty("handle").GetString() ?? "",
            DisplayName = root.TryGetProperty("displayName", out var name) ? name.GetString() ?? "" : ""
        };
    }

    private string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
} 