using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Core.Services;
using SocialMediaCommander.Services.Interfaces;
using Serilog;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// Service for managing OAuth configurations per platform with secure storage
/// </summary>
public class OAuthConfigurationService : IOAuthConfigurationService
{
    private readonly string _configDirectory;
    private readonly string _configFileName = "oauth-configs.encrypted";
    private readonly Dictionary<SocialPlatform, OAuthConfig> _configurations;
    private readonly object _lock = new object();
    private readonly ILogger _logger;

    public OAuthConfigurationService()
    {
        _logger = LoggingService.ForContext<OAuthConfigurationService>();
        _configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SocialMediaCommander",
            "Config"
        );
        
        Directory.CreateDirectory(_configDirectory);
        _configurations = new Dictionary<SocialPlatform, OAuthConfig>();
        
        // Note: Removed synchronous async calls from constructor to prevent deadlocks
        // Configurations will be loaded lazily when first accessed via EnsureConfigurationsLoadedAsync()
        _logger.Information("OAuthConfigurationService initialized. Configuration loading will be done lazily.");
    }

    public async Task<OAuthConfig?> GetConfigurationAsync(SocialPlatform platform)
    {
        await EnsureConfigurationsLoadedAsync();
        
        lock (_lock)
        {
            return _configurations.TryGetValue(platform, out var config) ? config : null;
        }
    }

    public async Task SaveConfigurationAsync(SocialPlatform platform, OAuthConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        lock (_lock)
        {
            _configurations[platform] = config;
        }

        await SaveConfigurationsAsync();
    }

    public async Task<Dictionary<SocialPlatform, OAuthConfig>> GetAllConfigurationsAsync()
    {
        await EnsureConfigurationsLoadedAsync();
        
        lock (_lock)
        {
            return new Dictionary<SocialPlatform, OAuthConfig>(_configurations);
        }
    }

    public async Task DeleteConfigurationAsync(SocialPlatform platform)
    {
        lock (_lock)
        {
            _configurations.Remove(platform);
        }

        await SaveConfigurationsAsync();
    }

    public Task<ValidationResult> ValidateConfigurationAsync(SocialPlatform platform, OAuthConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.ClientId))
            errors.Add("Client ID is required");

        if (string.IsNullOrWhiteSpace(config.ClientSecret))
            errors.Add("Client Secret is required");

        if (string.IsNullOrWhiteSpace(config.AuthorizationEndpoint))
            errors.Add("Authorization Endpoint is required");
        else if (!Uri.TryCreate(config.AuthorizationEndpoint, UriKind.Absolute, out _))
            errors.Add("Authorization Endpoint must be a valid URL");

        if (string.IsNullOrWhiteSpace(config.TokenEndpoint))
            errors.Add("Token Endpoint is required");
        else if (!Uri.TryCreate(config.TokenEndpoint, UriKind.Absolute, out _))
            errors.Add("Token Endpoint must be a valid URL");

        if (string.IsNullOrWhiteSpace(config.RedirectUri))
            errors.Add("Redirect URI is required");
        else if (!Uri.TryCreate(config.RedirectUri, UriKind.Absolute, out _))
            errors.Add("Redirect URI must be a valid URL");

        if (!string.IsNullOrWhiteSpace(config.UserInfoEndpoint) && 
            !Uri.TryCreate(config.UserInfoEndpoint, UriKind.Absolute, out _))
            errors.Add("User Info Endpoint must be a valid URL");

        if (!string.IsNullOrWhiteSpace(config.RevokeEndpoint) && 
            !Uri.TryCreate(config.RevokeEndpoint, UriKind.Absolute, out _))
            errors.Add("Revoke Endpoint must be a valid URL");

        if (config.Scopes == null || config.Scopes.Length == 0)
            errors.Add("At least one scope is required");

        return Task.FromResult(new ValidationResult(errors));
    }

    public OAuthConfig GetDefaultConfiguration(SocialPlatform platform)
    {
        return platform switch
        {
            SocialPlatform.BlueSky => new OAuthConfig
            {
                AuthorizationEndpoint = "https://bsky.social/oauth/authorize",
                TokenEndpoint = "https://bsky.social/oauth/token",
                UserInfoEndpoint = "https://bsky.social/xrpc/com.atproto.server.getSession",
                RedirectUri = "http://localhost:8080/oauth/callback",
                Scopes = new[] { "read", "write" },
                AdditionalParameters = new Dictionary<string, string>
                {
                    ["response_type"] = "code",
                    ["code_challenge_method"] = "S256"
                }
            },
            SocialPlatform.X => new OAuthConfig
            {
                AuthorizationEndpoint = "https://twitter.com/i/oauth2/authorize",
                TokenEndpoint = "https://api.twitter.com/2/oauth2/token",
                UserInfoEndpoint = "https://api.twitter.com/2/users/me",
                RevokeEndpoint = "https://api.twitter.com/2/oauth2/revoke",
                RedirectUri = "http://localhost:8080/oauth/callback",
                Scopes = new[] { "tweet.read", "tweet.write", "users.read", "offline.access" },
                AdditionalParameters = new Dictionary<string, string>
                {
                    ["response_type"] = "code",
                    ["code_challenge_method"] = "S256"
                }
            },
            SocialPlatform.LinkedIn => new OAuthConfig
            {
                AuthorizationEndpoint = "https://www.linkedin.com/oauth/v2/authorization",
                TokenEndpoint = "https://www.linkedin.com/oauth/v2/accessToken",
                UserInfoEndpoint = "https://api.linkedin.com/v2/people/~",
                RedirectUri = "http://localhost:8080/oauth/callback",
                Scopes = new[] { "r_liteprofile", "r_emailaddress", "w_member_social" },
                AdditionalParameters = new Dictionary<string, string>
                {
                    ["response_type"] = "code"
                }
            },
            SocialPlatform.Threads => new OAuthConfig
            {
                AuthorizationEndpoint = "https://threads.net/oauth/authorize",
                TokenEndpoint = "https://graph.threads.net/oauth/access_token",
                UserInfoEndpoint = "https://graph.threads.net/v1.0/me",
                RedirectUri = "http://localhost:8080/oauth/callback",
                Scopes = new[] { "threads_basic", "threads_content_publish" },
                AdditionalParameters = new Dictionary<string, string>
                {
                    ["response_type"] = "code"
                }
            },
            SocialPlatform.Facebook => new OAuthConfig
            {
                AuthorizationEndpoint = "https://www.facebook.com/v18.0/dialog/oauth",
                TokenEndpoint = "https://graph.facebook.com/v18.0/oauth/access_token",
                UserInfoEndpoint = "https://graph.facebook.com/v18.0/me",
                RedirectUri = "http://localhost:8080/oauth/callback",
                Scopes = new[] { "pages_manage_posts", "pages_read_engagement", "public_profile" },
                AdditionalParameters = new Dictionary<string, string>
                {
                    ["response_type"] = "code"
                }
            },
            _ => throw new ArgumentException($"Unsupported platform: {platform}")
        };
    }

    public async Task<bool> HasValidConfigurationAsync(SocialPlatform platform)
    {
        var config = await GetConfigurationAsync(platform);
        if (config == null) return false;

        var validation = await ValidateConfigurationAsync(platform, config);
        return validation.IsValid && !HasPlaceholderValues(config);
    }

    public bool HasPlaceholderValues(OAuthConfig config)
    {
        return config.ClientId == "YOUR_CLIENT_ID_HERE" || 
               config.ClientSecret == "YOUR_CLIENT_SECRET_HERE" ||
               string.IsNullOrWhiteSpace(config.ClientId) || 
               string.IsNullOrWhiteSpace(config.ClientSecret);
    }
    
    public async Task<ConfigurationStatus> GetConfigurationStatusAsync(SocialPlatform platform)
    {
        var config = await GetConfigurationAsync(platform);
        
        if (config == null)
        {
            return new ConfigurationStatus
            {
                IsConfigured = false,
                HasPlaceholders = false,
                Status = "Not configured",
                Message = "OAuth configuration not found. Default configuration will be created."
            };
        }
        
        var hasPlaceholders = HasPlaceholderValues(config);
        var validation = await ValidateConfigurationAsync(platform, config);
        
        return new ConfigurationStatus
        {
            IsConfigured = validation.IsValid && !hasPlaceholders,
            HasPlaceholders = hasPlaceholders,
            Status = hasPlaceholders ? "Needs setup" : validation.IsValid ? "Ready" : "Invalid",
            Message = hasPlaceholders 
                ? "Please replace placeholder values with your actual OAuth credentials"
                : validation.IsValid 
                    ? "OAuth configuration is ready to use"
                    : $"Configuration errors: {string.Join(", ", validation.Errors)}"
        };
    }

    public async Task ImportConfigurationsAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Configuration file not found: {filePath}");

        var json = await File.ReadAllTextAsync(filePath);
        var importedConfigs = JsonSerializer.Deserialize<Dictionary<string, OAuthConfig>>(json);

        if (importedConfigs != null)
        {
            lock (_lock)
            {
                foreach (var kvp in importedConfigs)
                {
                    if (Enum.TryParse<SocialPlatform>(kvp.Key, out var platform))
                    {
                        _configurations[platform] = kvp.Value;
                    }
                }
            }

            await SaveConfigurationsAsync();
        }
    }

    public async Task ExportConfigurationsAsync(string filePath)
    {
        await EnsureConfigurationsLoadedAsync();
        
        Dictionary<string, OAuthConfig> exportConfigs;
        lock (_lock)
        {
            exportConfigs = _configurations.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value
            );
        }

        var json = JsonSerializer.Serialize(exportConfigs, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, json);
    }

    private async Task LoadConfigurationsAsync()
    {
        var configPath = Path.Combine(_configDirectory, _configFileName);
        
        if (!File.Exists(configPath))
        {
            // Initialize with default configurations
            _logger.Information("OAuth configuration file does not exist, creating defaults");
            await InitializeDefaultConfigurationsAsync();
            return;
        }

        try
        {
            // Read encrypted data
            var encryptedData = await File.ReadAllBytesAsync(configPath);
            
            // Decrypt using cross-platform encryption
            var decryptedData = CrossPlatformEncryption.Unprotect(encryptedData, "SocialMediaCommander_OAuth");
            var json = Encoding.UTF8.GetString(decryptedData);
            
            // Check if file is empty or just contains empty JSON
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
            {
                _logger.Warning("OAuth configuration file is empty, initializing defaults");
                await InitializeDefaultConfigurationsAsync();
                return;
            }
            
            var configs = JsonSerializer.Deserialize<Dictionary<string, OAuthConfig>>(json);

            if (configs != null && configs.Count > 0)
            {
                lock (_lock)
                {
                    _configurations.Clear();
                    foreach (var kvp in configs)
                    {
                        if (Enum.TryParse<SocialPlatform>(kvp.Key, out var platform))
                        {
                            _configurations[platform] = kvp.Value;
                        }
                    }
                }
                _logger.Information("Loaded {Count} encrypted OAuth configurations", configs.Count);
            }
            else
            {
                _logger.Warning("OAuth configuration file contains no valid configurations, initializing defaults");
                await InitializeDefaultConfigurationsAsync();
            }
        }
        catch (CryptographicException ex)
        {
            _logger.Error(ex, "Failed to decrypt OAuth configurations - may have been encrypted by different user or platform");
            await InitializeDefaultConfigurationsAsync();
        }
        catch (Exception ex)
        {
            // Log error and initialize defaults
            _logger.Error(ex, "Error loading OAuth configurations");
            await InitializeDefaultConfigurationsAsync();
        }
    }

    private async Task SaveConfigurationsAsync()
    {
        var configPath = Path.Combine(_configDirectory, _configFileName);
        
        try
        {
            Dictionary<string, OAuthConfig> saveConfigs;
            lock (_lock)
            {
                saveConfigs = _configurations.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => kvp.Value
                );
            }

            var json = JsonSerializer.Serialize(saveConfigs, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Encrypt the JSON data using cross-platform encryption
            var data = Encoding.UTF8.GetBytes(json);
            var encryptedData = CrossPlatformEncryption.Protect(data, "SocialMediaCommander_OAuth");

            await File.WriteAllBytesAsync(configPath, encryptedData);
            _logger.Debug("Saved {Count} OAuth configurations to encrypted storage", saveConfigs.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save OAuth configurations to encrypted storage");
            throw;
        }
    }

    private async Task InitializeDefaultConfigurationsAsync()
    {
        // Initialize with default configurations that have placeholder values
        // Users will need to provide their own client IDs and secrets
        lock (_lock)
        {
            _configurations.Clear();
            foreach (var platform in Enum.GetValues<SocialPlatform>())
            {
                var defaultConfig = GetDefaultConfiguration(platform);
                // Set placeholder values that users need to replace
                defaultConfig.ClientId = "YOUR_CLIENT_ID_HERE";
                defaultConfig.ClientSecret = "YOUR_CLIENT_SECRET_HERE";
                _configurations[platform] = defaultConfig;
            }
        }
        
        await SaveConfigurationsAsync();
    }

    private async Task EnsureConfigurationsLoadedAsync()
    {
        // Check if configurations are loaded
        lock (_lock)
        {
            if (_configurations.Count > 0)
            {
                return; // Already loaded
            }
        }
        
        // Load configurations if not already loaded
        _logger.Debug("Configurations not loaded, loading now...");
        await LoadConfigurationsAsync();
        
        // Verify configurations were loaded
        lock (_lock)
        {
            if (_configurations.Count == 0)
            {
                _logger.Warning("No configurations loaded, initializing defaults");
            }
            else
            {
                _logger.Debug("Configurations loaded successfully: {Count} platforms", _configurations.Count);
            }
        }
    }
} 