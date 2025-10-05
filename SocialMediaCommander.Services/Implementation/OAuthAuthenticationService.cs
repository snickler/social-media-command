using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Web;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;
using SocialMediaCommander.Services.Serialization;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// OAuth authentication service that handles OAuth 2.0 flows for social media platforms
/// </summary>
public class OAuthAuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IOAuthConfigurationService _configService;
    private HttpListener? _httpListener;
    private const string RedirectUri = "http://localhost:8080/oauth/callback";

    public OAuthAuthenticationService(HttpClient httpClient, IOAuthConfigurationService configService)
    {
        _httpClient = httpClient;
        _configService = configService;
    }

    public async Task<AuthenticationResult> StartAuthenticationAsync(SocialPlatform platform, string? redirectUri = null)
    {
        try
        {
            var config = await _configService.GetConfigurationAsync(platform);
            if (config == null)
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"OAuth configuration not found for platform: {platform}. Please configure OAuth settings first."
                };
            }

            // Validate configuration
            var validation = await _configService.ValidateConfigurationAsync(platform, config);
            if (!validation.IsValid)
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Invalid OAuth configuration for {platform}: {string.Join(", ", validation.Errors)}"
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
            var config = await _configService.GetConfigurationAsync(platform);
            if (config == null)
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

        // Add additional parameters from configuration
        foreach (var param in config.AdditionalParameters)
        {
            queryParams[param.Key] = param.Value;
        }

        var queryString = string.Join("&", queryParams.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{config.AuthorizationEndpoint}?{queryString}";
    }

    private Task StartHttpListener()
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"{RedirectUri}/");
        _httpListener.Start();

        // Handle callback in background - schedule and return immediately
        _ = Task.Run(async () =>
        {
            try
            {
                var context = await _httpListener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                // Extract authorization code and state from query parameters
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
                        OnAuthenticationCallback?.Invoke("", "");
                        return;
                    }

                    if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(state))
                    {
                        await SendCallbackResponse(response, "Authentication successful! You can close this window.");
                        OnAuthenticationCallback?.Invoke(code, state);
                        return;
                    }
                }

                await SendCallbackResponse(response, "Authentication failed: Invalid callback parameters.");
                OnAuthenticationCallback?.Invoke("", "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OAuth callback error: {ex.Message}");
                OnAuthenticationCallback?.Invoke("", "");
            }
            finally
            {
                _httpListener?.Stop();
            }
        });

        return Task.CompletedTask;
    }

    private async Task SendCallbackResponse(HttpListenerResponse response, string message)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Social Media Commander - OAuth Callback</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }}
        .container {{
            background: white;
            padding: 2rem;
            border-radius: 10px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.2);
            text-align: center;
            max-width: 400px;
        }}
        .success {{ color: #28a745; }}
        .error {{ color: #dc3545; }}
        h1 {{ margin-bottom: 1rem; }}
        p {{ margin-bottom: 1rem; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>Social Media Commander</h1>
        <p class='{(message.Contains("successful") ? "success" : "error")}'>{message}</p>
        <script>
            setTimeout(() => {{
                window.close();
            }}, 3000);
        </script>
    </div>
</body>
</html>";

        var buffer = Encoding.UTF8.GetBytes(html);
        response.ContentLength64 = buffer.Length;
        response.ContentType = "text/html";
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
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

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Token exchange failed: {response.StatusCode} - {errorContent}");
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize(jsonResponse, ServicesJsonContext.Default.JsonElement);

            return new OAuthTokens
            {
                AccessToken = tokenData.GetProperty("access_token").GetString() ?? string.Empty,
                RefreshToken = tokenData.TryGetProperty("refresh_token", out var refreshToken)
                    ? refreshToken.GetString() : null,
                ExpiresAt = tokenData.TryGetProperty("expires_in", out var expiresIn)
                    ? DateTime.UtcNow.AddSeconds(expiresIn.GetInt32())
                    : DateTime.UtcNow.AddHours(1),
                TokenType = tokenData.TryGetProperty("token_type", out var tokenType)
                    ? tokenType.GetString() ?? "Bearer"
                    : "Bearer",
                Scopes = config.Scopes
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exchanging code for token: {ex.Message}");
            return null;
        }
    }

    public async Task<UserProfile?> GetUserProfileAsync(SocialPlatform platform, string accessToken)
    {
        try
        {
            var config = await _configService.GetConfigurationAsync(platform);
            if (config?.UserInfoEndpoint == null)
                return null;

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.GetAsync(config.UserInfoEndpoint);

            if (!response.IsSuccessStatusCode)
                return null;

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var userData = JsonSerializer.Deserialize(jsonResponse, ServicesJsonContext.Default.JsonElement);

            return new UserProfile
            {
                Id = ExtractUserProperty(userData, platform, "id"),
                Username = ExtractUserProperty(userData, platform, "username"),
                DisplayName = ExtractUserProperty(userData, platform, "name"),
                Bio = ExtractUserProperty(userData, platform, "bio"),
                Avatar = ExtractUserProperty(userData, platform, "avatar"),
                ProfileUrl = ExtractUserProperty(userData, platform, "url"),
                Platform = platform,
                IsVerified = ExtractUserProperty(userData, platform, "verified") == "true",
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user profile: {ex.Message}");
            return null;
        }
    }

    private string ExtractUserProperty(JsonElement userData, SocialPlatform platform, string property)
    {
        try
        {
            return platform switch
            {
                SocialPlatform.BlueSky => property switch
                {
                    "id" => userData.GetProperty("did").GetString() ?? string.Empty,
                    "username" => userData.GetProperty("handle").GetString() ?? string.Empty,
                    "name" => userData.TryGetProperty("displayName", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                    "avatar" => userData.TryGetProperty("avatar", out var avatar) ? avatar.GetString() ?? string.Empty : string.Empty,
                    _ => string.Empty
                },
                SocialPlatform.X => property switch
                {
                    "id" => userData.GetProperty("data").GetProperty("id").GetString() ?? string.Empty,
                    "username" => userData.GetProperty("data").GetProperty("username").GetString() ?? string.Empty,
                    "name" => userData.GetProperty("data").GetProperty("name").GetString() ?? string.Empty,
                    "bio" => userData.GetProperty("data").TryGetProperty("description", out var desc) ? desc.GetString() ?? string.Empty : string.Empty,
                    "avatar" => userData.GetProperty("data").TryGetProperty("profile_image_url", out var img) ? img.GetString() ?? string.Empty : string.Empty,
                    "verified" => userData.GetProperty("data").TryGetProperty("verified", out var ver) ? ver.GetBoolean().ToString() : "false",
                    _ => string.Empty
                },
                SocialPlatform.LinkedIn => property switch
                {
                    "id" => userData.GetProperty("id").GetString() ?? string.Empty,
                    "username" => userData.TryGetProperty("vanityName", out var vanity) ? vanity.GetString() ?? string.Empty : string.Empty,
                    "name" => $"{userData.GetProperty("firstName").GetProperty("localized").GetProperty("en_US").GetString()} {userData.GetProperty("lastName").GetProperty("localized").GetProperty("en_US").GetString()}",
                    "avatar" => userData.TryGetProperty("profilePicture", out var pic) ? pic.GetString() ?? string.Empty : string.Empty,
                    _ => string.Empty
                },
                _ => userData.TryGetProperty(property, out var prop) ? prop.GetString() ?? string.Empty : string.Empty
            };
        }
        catch
        {
            return string.Empty;
        }
    }

    public event Action<string, string>? OnAuthenticationCallback;

    // Placeholder implementations for interface completeness
    public Task<AuthenticationResult> RefreshTokenAsync(SocialPlatform platform, string refreshToken)
    {
        return Task.FromResult(new AuthenticationResult { IsSuccess = false, ErrorMessage = "Not implemented" });
    }

    public Task<bool> RevokeTokenAsync(SocialPlatform platform, string accessToken)
    {
        return Task.FromResult(false);
    }

    public Task<bool> ValidateTokenAsync(SocialPlatform platform, string accessToken)
    {
        return Task.FromResult(true);
    }

    public OAuthConfig GetOAuthConfig(SocialPlatform platform)
    {
        // This method should be synchronous according to the interface
        // Using GetAwaiter().GetResult() instead of .Wait()/.Result to preserve exception details
        // as recommended by Microsoft best practices
        var task = _configService.GetConfigurationAsync(platform);
        return task.GetAwaiter().GetResult() ?? _configService.GetDefaultConfiguration(platform);
    }

    public void Dispose()
    {
        _httpListener?.Stop();
        _httpListener?.Close();
        _httpClient?.Dispose();
    }
}