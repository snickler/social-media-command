using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Web;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// OAuth authentication service that handles OAuth 2.0 flows for social media platforms
/// </summary>
public class OAuthAuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<SocialPlatform, OAuthConfig> _oauthConfigs;
    private HttpListener? _httpListener;
    private const string RedirectUri = "http://localhost:8080/oauth/callback";
    
    public OAuthAuthenticationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _oauthConfigs = InitializeOAuthConfigs();
    }

    private Dictionary<SocialPlatform, OAuthConfig> InitializeOAuthConfigs()
    {
        return new Dictionary<SocialPlatform, OAuthConfig>
        {
            [SocialPlatform.BlueSky] = new OAuthConfig
            {
                ClientId = "your-bluesky-client-id", // Replace with actual client ID
                ClientSecret = "your-bluesky-client-secret", // Replace with actual client secret
                AuthorizationEndpoint = "https://bsky.social/oauth/authorize",
                TokenEndpoint = "https://bsky.social/oauth/token",
                UserInfoEndpoint = "https://bsky.social/xrpc/com.atproto.server.getSession",
                RedirectUri = RedirectUri,
                Scopes = new[] { "read", "write" }
            },
            [SocialPlatform.X] = new OAuthConfig
            {
                ClientId = "your-twitter-client-id", // Replace with actual client ID
                ClientSecret = "your-twitter-client-secret", // Replace with actual client secret
                AuthorizationEndpoint = "https://twitter.com/i/oauth2/authorize",
                TokenEndpoint = "https://api.twitter.com/2/oauth2/token",
                UserInfoEndpoint = "https://api.twitter.com/2/users/me",
                RedirectUri = RedirectUri,
                Scopes = new[] { "tweet.read", "tweet.write", "users.read" }
            },
            [SocialPlatform.LinkedIn] = new OAuthConfig
            {
                ClientId = "your-linkedin-client-id", // Replace with actual client ID
                ClientSecret = "your-linkedin-client-secret", // Replace with actual client secret
                AuthorizationEndpoint = "https://www.linkedin.com/oauth/v2/authorization",
                TokenEndpoint = "https://www.linkedin.com/oauth/v2/accessToken",
                UserInfoEndpoint = "https://api.linkedin.com/v2/people/~",
                RedirectUri = RedirectUri,
                Scopes = new[] { "r_liteprofile", "r_emailaddress", "w_member_social" }
            },
            [SocialPlatform.Threads] = new OAuthConfig
            {
                ClientId = "your-threads-client-id", // Replace with actual client ID
                ClientSecret = "your-threads-client-secret", // Replace with actual client secret
                AuthorizationEndpoint = "https://threads.net/oauth/authorize",
                TokenEndpoint = "https://graph.threads.net/oauth/access_token",
                UserInfoEndpoint = "https://graph.threads.net/v1.0/me",
                RedirectUri = RedirectUri,
                Scopes = new[] { "threads_basic", "threads_content_publish" }
            },
            [SocialPlatform.Facebook] = new OAuthConfig
            {
                ClientId = "your-facebook-client-id", // Replace with actual client ID
                ClientSecret = "your-facebook-client-secret", // Replace with actual client secret
                AuthorizationEndpoint = "https://www.facebook.com/v18.0/dialog/oauth",
                TokenEndpoint = "https://graph.facebook.com/v18.0/oauth/access_token",
                UserInfoEndpoint = "https://graph.facebook.com/v18.0/me",
                RedirectUri = RedirectUri,
                Scopes = new[] { "pages_manage_posts", "pages_read_engagement" }
            }
        };
    }

    public async Task<AuthenticationResult> StartAuthenticationAsync(SocialPlatform platform, string? redirectUri = null)
    {
        try
        {
            if (!_oauthConfigs.TryGetValue(platform, out var config))
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"OAuth configuration not found for platform: {platform}"
                };
            }

            // Generate state parameter for security
            var state = Guid.NewGuid().ToString("N");
            
            // Build authorization URL
            var authUrl = BuildAuthorizationUrl(config, state);
            
            // Start HTTP listener for callback
            await StartHttpListener();
            
            // Open browser
            OpenBrowser(authUrl);
            
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
            if (!_oauthConfigs.TryGetValue(platform, out var config))
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"OAuth configuration not found for platform: {platform}"
                };
            }

            // Exchange authorization code for access token
            var tokenResponse = await ExchangeCodeForTokenAsync(config, authorizationCode);
            
            if (tokenResponse == null)
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to exchange authorization code for access token"
                };
            }

            // Get user profile
            var userProfile = await GetUserProfileAsync(platform, tokenResponse.AccessToken);
            
            return new AuthenticationResult
            {
                IsSuccess = true,
                Tokens = tokenResponse,
                UserProfile = userProfile,
                State = state
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

    private string BuildAuthorizationUrl(OAuthConfig config, string state)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = config.ClientId,
            ["redirect_uri"] = config.RedirectUri,
            ["response_type"] = "code",
            ["state"] = state,
            ["scope"] = string.Join(" ", config.Scopes)
        };

        var queryString = string.Join("&", queryParams.Select(kvp => 
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{config.AuthorizationEndpoint}?{queryString}";
    }

    private async Task StartHttpListener()
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"{RedirectUri}/");
        _httpListener.Start();
        
        // Handle callback in background
        _ = Task.Run(async () =>
        {
            try
            {
                var context = await _httpListener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;
                
                // Extract authorization code and state from callback
                var query = request.Url?.Query;
                if (!string.IsNullOrEmpty(query))
                {
                    var queryParams = HttpUtility.ParseQueryString(query);
                    var code = queryParams["code"];
                    var state = queryParams["state"];
                    var error = queryParams["error"];
                    
                    if (!string.IsNullOrEmpty(error))
                    {
                        await SendCallbackResponse(response, $"Authentication failed: {error}");
                        return;
                    }
                    
                    if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(state))
                    {
                        await SendCallbackResponse(response, "Authentication successful! You can close this window.");
                        
                        // Trigger callback completion event
                        OnAuthenticationCallback?.Invoke(code, state);
                        return;
                    }
                }
                
                await SendCallbackResponse(response, "Authentication failed: Missing authorization code.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HTTP listener error: {ex.Message}");
            }
            finally
            {
                _httpListener?.Stop();
                _httpListener?.Close();
            }
        });
    }

    private async Task SendCallbackResponse(HttpListenerResponse response, string message)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Social Media Commander - Authentication</title>
    <style>
        body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; }}
        .container {{ max-width: 500px; margin: 0 auto; }}
        .success {{ color: #28a745; }}
        .error {{ color: #dc3545; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>Social Media Commander</h1>
        <p class='{(message.Contains("successful") ? "success" : "error")}'>{message}</p>
    </div>
</body>
</html>";

        var buffer = Encoding.UTF8.GetBytes(html);
        response.ContentLength64 = buffer.Length;
        response.ContentType = "text/html";
        response.StatusCode = 200;
        
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private void OpenBrowser(string url)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open browser: {ex.Message}");
        }
    }

    private async Task<OAuthTokens?> ExchangeCodeForTokenAsync(OAuthConfig config, string authorizationCode)
    {
        try
        {
            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = config.ClientId,
                ["client_secret"] = config.ClientSecret,
                ["code"] = authorizationCode,
                ["redirect_uri"] = config.RedirectUri
            };

            var content = new FormUrlEncodedContent(tokenRequest);
            var response = await _httpClient.PostAsync(config.TokenEndpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<JsonElement>(json);
                
                return new OAuthTokens
                {
                    AccessToken = tokenData.GetProperty("access_token").GetString() ?? "",
                    RefreshToken = tokenData.TryGetProperty("refresh_token", out var refreshToken) ? refreshToken.GetString() : null,
                    ExpiresAt = tokenData.TryGetProperty("expires_in", out var expiresIn) 
                        ? DateTime.UtcNow.AddSeconds(expiresIn.GetInt32())
                        : DateTime.UtcNow.AddDays(1), // Default to 1 day if not specified
                    TokenType = tokenData.TryGetProperty("token_type", out var tokenType) ? tokenType.GetString() ?? "Bearer" : "Bearer",
                    Scopes = config.Scopes
                };
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token exchange error: {ex.Message}");
            return null;
        }
    }

    public async Task<UserProfile?> GetUserProfileAsync(SocialPlatform platform, string accessToken)
    {
        try
        {
            if (!_oauthConfigs.TryGetValue(platform, out var config))
                return null;

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            
            var response = await _httpClient.GetAsync(config.UserInfoEndpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var userData = JsonSerializer.Deserialize<JsonElement>(json);
                
                return new UserProfile
                {
                    Id = ExtractUserProperty(userData, platform, "id"),
                    Username = ExtractUserProperty(userData, platform, "username"),
                    DisplayName = ExtractUserProperty(userData, platform, "displayName"),
                    Bio = ExtractUserProperty(userData, platform, "bio"),
                    Avatar = ExtractUserProperty(userData, platform, "avatar"),
                    Platform = platform,
                    CreatedAt = DateTime.UtcNow
                };
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get user profile error: {ex.Message}");
            return null;
        }
    }

    private string ExtractUserProperty(JsonElement userData, SocialPlatform platform, string property)
    {
        try
        {
            return platform switch
            {
                SocialPlatform.X => property switch
                {
                    "id" => userData.GetProperty("data").GetProperty("id").GetString() ?? "",
                    "username" => userData.GetProperty("data").GetProperty("username").GetString() ?? "",
                    "displayName" => userData.GetProperty("data").GetProperty("name").GetString() ?? "",
                    "bio" => userData.GetProperty("data").TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                    "avatar" => userData.GetProperty("data").TryGetProperty("profile_image_url", out var avatar) ? avatar.GetString() ?? "" : "",
                    _ => ""
                },
                SocialPlatform.LinkedIn => property switch
                {
                    "id" => userData.GetProperty("id").GetString() ?? "",
                    "username" => userData.TryGetProperty("vanityName", out var vanity) ? vanity.GetString() ?? "" : "",
                    "displayName" => userData.TryGetProperty("localizedFirstName", out var firstName) && userData.TryGetProperty("localizedLastName", out var lastName) 
                        ? $"{firstName.GetString()} {lastName.GetString()}" : "",
                    "bio" => userData.TryGetProperty("headline", out var headline) ? headline.GetString() ?? "" : "",
                    "avatar" => userData.TryGetProperty("profilePicture", out var pic) ? pic.GetString() ?? "" : "",
                    _ => ""
                },
                SocialPlatform.BlueSky => property switch
                {
                    "id" => userData.GetProperty("did").GetString() ?? "",
                    "username" => userData.GetProperty("handle").GetString() ?? "",
                    "displayName" => userData.TryGetProperty("displayName", out var display) ? display.GetString() ?? "" : "",
                    "bio" => userData.TryGetProperty("description", out var bio) ? bio.GetString() ?? "" : "",
                    "avatar" => userData.TryGetProperty("avatar", out var avatar) ? avatar.GetString() ?? "" : "",
                    _ => ""
                },
                _ => ""
            };
        }
        catch
        {
            return "";
        }
    }

    // Event for callback completion
    public event Action<string, string>? OnAuthenticationCallback;

    // Implement other interface methods
    public Task<AuthenticationResult> RefreshTokenAsync(SocialPlatform platform, string refreshToken)
    {
        // Implementation for token refresh
        return Task.FromResult(new AuthenticationResult { IsSuccess = false, ErrorMessage = "Not implemented" });
    }

    public Task<bool> RevokeTokenAsync(SocialPlatform platform, string accessToken)
    {
        // Implementation for token revocation
        return Task.FromResult(false);
    }

    public Task<bool> ValidateTokenAsync(SocialPlatform platform, string accessToken)
    {
        // Implementation for token validation
        return Task.FromResult(false);
    }

    public OAuthConfig GetOAuthConfig(SocialPlatform platform)
    {
        return _oauthConfigs.TryGetValue(platform, out var config) ? config : new OAuthConfig();
    }

    public void Dispose()
    {
        _httpListener?.Stop();
        _httpListener?.Close();
        _httpClient?.Dispose();
    }
} 