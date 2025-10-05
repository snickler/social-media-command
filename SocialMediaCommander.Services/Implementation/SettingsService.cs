using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SocialMediaCommander.Services.Interfaces;
using SocialMediaCommander.Services.Serialization;
using Serilog;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// Service for managing application settings and user preferences with secure storage
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ILogger _logger;
    private readonly string _settingsDirectory;
    private readonly string _settingsFile;
    private readonly object _lock = new();
    private AppSettings? _cachedSettings;

    public SettingsService()
    {
        _logger = Log.ForContext<SettingsService>();

        _settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SocialMediaCommander",
            "Settings"
        );

        _settingsFile = Path.Combine(_settingsDirectory, "app-settings.encrypted");
        Directory.CreateDirectory(_settingsDirectory);
    }

    /// <summary>
    /// Constructor for testing with custom directory path
    /// </summary>
    /// <param name="customDirectory">Custom directory path for testing</param>
    internal SettingsService(string customDirectory)
    {
        _logger = Log.ForContext<SettingsService>();

        _settingsDirectory = Path.Combine(customDirectory, "SocialMediaCommander", "Settings");
        _settingsFile = Path.Combine(_settingsDirectory, "app-settings.encrypted");
        Directory.CreateDirectory(_settingsDirectory);
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        return await LoadSettingsAsync();
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            lock (_lock)
            {
                _cachedSettings = settings;
            }

            var json = JsonSerializer.Serialize(settings, ServicesJsonContext.Default.AppSettings);

            var data = System.Text.Encoding.UTF8.GetBytes(json);
            var encryptedData = CrossPlatformEncryption.Protect(data, "SocialMediaCommander_Settings");

            await File.WriteAllBytesAsync(_settingsFile, encryptedData);
            _logger.Information("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save settings");
            throw;
        }
    }

    [RequiresUnreferencedCode("Generic JSON deserialization may require types that cannot be statically analyzed")]
    [RequiresDynamicCode("Generic JSON deserialization may require runtime code generation")]
    public async Task<T> GetSettingAsync<T>(string key, T defaultValue = default!)
    {
        var settings = await GetSettingsAsync();
        // Ensure the CustomSettings dictionary is initialized to avoid nullability warnings
        settings.CustomSettings ??= new Dictionary<string, object?>();

        if (settings.CustomSettings.TryGetValue(key, out var value))
        {
            try
            {
                if (value is JsonElement jsonElement)
                {
                    var deserialized = JsonSerializer.Deserialize<T>(jsonElement, ServicesJsonContext.Default.Options);
                    return deserialized != null ? deserialized : defaultValue;
                }

                // Convert.ChangeType may return null for incompatible conversions; handle defensively
                object? convertedObj = Convert.ChangeType(value, typeof(T));
                if (convertedObj is T convertedT)
                    return convertedT;
                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to convert setting {Key} to type {Type}", key, typeof(T).Name);
                return defaultValue;
            }
        }

        return defaultValue;
    }

    public async Task SetSettingAsync<T>(string key, T value)
    {
        var settings = await GetSettingsAsync();
        // Ensure the CustomSettings dictionary is initialized to avoid nullability warnings
        settings.CustomSettings ??= new Dictionary<string, object?>();
        settings.CustomSettings[key] = (object?)value;
        await SaveSettingsAsync(settings);
    }

    public async Task<bool> RemoveSettingAsync(string key)
    {
        var settings = await GetSettingsAsync();
        // Ensure the CustomSettings dictionary is initialized to avoid nullability warnings
        settings.CustomSettings ??= new Dictionary<string, object?>();
        var removed = settings.CustomSettings.Remove(key);

        if (removed)
        {
            await SaveSettingsAsync(settings);
        }

        return removed;
    }

    public async Task ResetToDefaultsAsync()
    {
        _logger.Information("Resetting settings to defaults");

        var defaultSettings = CreateDefaultSettings();
        await SaveSettingsAsync(defaultSettings);

        lock (_lock)
        {
            _cachedSettings = defaultSettings;
        }
    }

    public async Task<bool> ExportSettingsAsync(string filePath)
    {
        try
        {
            var settings = await GetSettingsAsync();
            var json = JsonSerializer.Serialize(settings, ServicesJsonContext.Default.AppSettings);

            await File.WriteAllTextAsync(filePath, json);
            _logger.Information("Settings exported to: {FilePath}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to export settings to: {FilePath}", filePath);
            return false;
        }
    }

    public async Task<bool> ImportSettingsAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.Warning("Settings file not found: {FilePath}", filePath);
                return false;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var importedSettings = JsonSerializer.Deserialize(json, ServicesJsonContext.Default.AppSettings);

            if (importedSettings != null)
            {
                await SaveSettingsAsync(importedSettings);
                _logger.Information("Settings imported from: {FilePath}", filePath);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to import settings from: {FilePath}", filePath);
            return false;
        }
    }

    private async Task<AppSettings> LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsFile))
            {
                _logger.Information("Settings file not found, creating defaults");
                var defaultSettings = CreateDefaultSettings();
                await SaveSettingsAsync(defaultSettings);
                return defaultSettings;
            }

            var encryptedData = await File.ReadAllBytesAsync(_settingsFile);
            var decryptedData = CrossPlatformEncryption.Unprotect(encryptedData, "SocialMediaCommander_Settings");
            var json = System.Text.Encoding.UTF8.GetString(decryptedData);

            var settings = JsonSerializer.Deserialize(json, ServicesJsonContext.Default.AppSettings);

            if (settings != null)
            {
                // Ensure CustomSettings dictionary is never null to satisfy callers and nullability checks
                settings.CustomSettings ??= new Dictionary<string, object?>();

                lock (_lock)
                {
                    _cachedSettings = settings!;
                }

                _logger.Debug("Settings loaded successfully");
                return settings;
            }

            _logger.Warning("Failed to deserialize settings, using defaults");
            return CreateDefaultSettings();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load settings, using defaults");
            return CreateDefaultSettings();
        }
    }

    private AppSettings CreateDefaultSettings()
    {
        return new AppSettings
        {
            Version = "1.0",
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,

            // UI Settings
            Theme = "Auto",
            Language = "en-US",
            StartupView = "Standard",
            AutoSaveInterval = TimeSpan.FromMinutes(5),
            ShowNotifications = true,
            MinimizeToTray = false,

            // Security Settings
            AutoLockTimeout = TimeSpan.FromMinutes(30),
            RequirePasswordOnStartup = false,
            EncryptBackups = true,
            AutoCreateBackups = true,
            BackupRetentionDays = 30,

            // Performance Settings
            EnableCaching = true,
            CacheTimeout = TimeSpan.FromHours(1),
            MaxConcurrentRequests = 5,
            RequestTimeout = TimeSpan.FromSeconds(30),

            // Privacy Settings
            AllowAnalytics = false,
            AllowCrashReporting = true,
            ClearLogsOnExit = false,

            // Advanced Settings
            EnableDebugLogging = false,
            LogLevel = "Information",
            MaxLogFileSize = 10 * 1024 * 1024, // 10MB

            CustomSettings = new Dictionary<string, object?>()
        };
    }
}

/// <summary>
/// Application settings and user preferences
/// </summary>
public class AppSettings
{
    public string Version { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }

    // UI Settings
    public string Theme { get; set; } = "Auto"; // Light, Dark, Auto
    public string Language { get; set; } = "en-US";
    public string StartupView { get; set; } = "Standard"; // Standard, Compact
    public TimeSpan AutoSaveInterval { get; set; } = TimeSpan.FromMinutes(5);
    public bool ShowNotifications { get; set; } = true;
    public bool MinimizeToTray { get; set; } = false;

    // Security Settings
    public TimeSpan AutoLockTimeout { get; set; } = TimeSpan.FromMinutes(30);
    public bool RequirePasswordOnStartup { get; set; } = false;
    public bool EncryptBackups { get; set; } = true;
    public bool AutoCreateBackups { get; set; } = true;
    public int BackupRetentionDays { get; set; } = 30;

    // Performance Settings
    public bool EnableCaching { get; set; } = true;
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromHours(1);
    public int MaxConcurrentRequests { get; set; } = 5;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    // Privacy Settings
    public bool AllowAnalytics { get; set; } = false;
    public bool AllowCrashReporting { get; set; } = true;
    public bool ClearLogsOnExit { get; set; } = false;

    // Advanced Settings
    public bool EnableDebugLogging { get; set; } = false;
    public string LogLevel { get; set; } = "Information";
    public long MaxLogFileSize { get; set; } = 10 * 1024 * 1024; // 10MB

    // Custom settings dictionary for extensibility
    public Dictionary<string, object?> CustomSettings { get; set; } = new();

    // Window Settings
    public WindowSettings? WindowSettings { get; set; }
}

/// <summary>
/// Window-specific settings
/// </summary>
public class WindowSettings
{
    public double Width { get; set; } = 1200;
    public double Height { get; set; } = 800;
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public bool IsMaximized { get; set; } = false;
    public bool RememberPosition { get; set; } = true;
}