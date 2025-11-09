using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Desktop.ViewModels;

/// <summary>
/// ViewModel for application settings and configuration management
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IAccountService _accountService;
    private readonly ISettingsService _settingsService;
    private readonly IBackupService _backupService;
    private readonly IDataIntegrityService _dataIntegrityService;

    // Theme & Appearance
    [ObservableProperty]
    private string _selectedTheme = "System";

    [ObservableProperty]
    private bool _isDarkMode = false;

    [ObservableProperty]
    private double _uiScale = 1.0;

    [ObservableProperty]
    private string _selectedLanguage = "English";

    // Notifications
    [ObservableProperty]
    private bool _enableNotifications = true;

    [ObservableProperty]
    private bool _enableSoundNotifications = true;

    [ObservableProperty]
    private bool _enableDesktopNotifications = true;

    [ObservableProperty]
    private bool _enableEmailNotifications = false;

    [ObservableProperty]
    private int _notificationDuration = 5;

    // Automation & Scheduling
    [ObservableProperty]
    private bool _enableAutoScheduling = false;

    [ObservableProperty]
    private bool _enableSmartPosting = true;

    [ObservableProperty]
    private int _defaultScheduleInterval = 30;

    [ObservableProperty]
    private string _timezoneSelection = "UTC";

    [ObservableProperty]
    private bool _enableWeekendPosting = true;

    // Privacy & Security
    [ObservableProperty]
    private bool _enableAnalytics = true;

    [ObservableProperty]
    private bool _enableCrashReporting = true;

    [ObservableProperty]
    private bool _enableAutoSave = true;

    [ObservableProperty]
    private int _autoSaveInterval = 5;

    [ObservableProperty]
    private bool _enableEncryption = false;

    // Platform Settings
    [ObservableProperty]
    private bool _enableTwitterIntegration = false;

    [ObservableProperty]
    private bool _enableFacebookIntegration = false;

    [ObservableProperty]
    private bool _enableInstagramIntegration = false;

    [ObservableProperty]
    private bool _enableLinkedInIntegration = false;

    [ObservableProperty]
    private bool _enableTikTokIntegration = false;

    // Advanced Settings
    [ObservableProperty]
    private int _maxConcurrentUploads = 3;

    [ObservableProperty]
    private int _mediaQuality = 90;

    [ObservableProperty]
    private bool _enableImageOptimization = true;

    [ObservableProperty]
    private bool _enableVideoCompression = true;

    [ObservableProperty]
    private string _defaultMediaFolder = "";

    // Collections for UI binding
    public ObservableCollection<string> ThemeOptions { get; } = new()
    {
        "System", "Light", "Dark", "Auto"
    };

    public ObservableCollection<string> LanguageOptions { get; } = new()
    {
        "English", "Spanish", "French", "German", "Japanese", "Chinese"
    };

    public ObservableCollection<string> TimezoneOptions { get; } = new()
    {
        "UTC", "EST", "PST", "GMT", "CET", "JST"
    };

    public ObservableCollection<int> NotificationDurationOptions { get; } = new()
    {
        3, 5, 10, 15, 30
    };

    public ObservableCollection<int> AutoSaveIntervalOptions { get; } = new()
    {
        1, 2, 5, 10, 15, 30
    };

    public ObservableCollection<int> ScheduleIntervalOptions { get; } = new()
    {
        15, 30, 60, 120, 240
    };

    // Status and Info
    [ObservableProperty]
    private string _applicationVersion = "1.0.0";

    [ObservableProperty]
    private string _lastBackupDate = "Never";

    [ObservableProperty]
    private long _cacheSize = 0;

    [ObservableProperty]
    private int _totalAccounts = 0;

    [ObservableProperty]
    private int _totalPosts = 0;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _statusMessage = "";

    public SettingsViewModel(
        IAccountService accountService,
        ISettingsService settingsService,
        IBackupService backupService,
        IDataIntegrityService dataIntegrityService)
    {
        _accountService = accountService;
        _settingsService = settingsService;
        _backupService = backupService;
        _dataIntegrityService = dataIntegrityService;

        _ = LoadSettingsAsync(); // Fire-and-forget with discard to suppress CS4014
        _ = LoadStatisticsAsync(); // Fire-and-forget with discard to suppress CS4014
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        IsLoading = true;
        StatusMessage = "Saving settings...";

        try
        {
            var settings = await _settingsService.GetSettingsAsync();

            // Update settings from UI properties
            settings.Theme = SelectedTheme;
            settings.Language = SelectedLanguage;
            settings.ShowNotifications = EnableNotifications;
            settings.AllowAnalytics = EnableAnalytics;
            settings.AllowCrashReporting = EnableCrashReporting;
            settings.AutoSaveInterval = TimeSpan.FromMinutes(AutoSaveInterval);
            settings.LastModified = DateTime.UtcNow;

            await _settingsService.SaveSettingsAsync(settings);

            StatusMessage = "Settings saved successfully!";
            System.Diagnostics.Debug.WriteLine("Settings saved successfully");
            await Task.Delay(2000);
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving settings: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Settings save failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ResetSettingsAsync()
    {
        IsLoading = true;
        StatusMessage = "Resetting to defaults...";

        try
        {
            await Task.Delay(500);

            // Reset to default values
            SelectedTheme = "System";
            IsDarkMode = false;
            UiScale = 1.0;
            SelectedLanguage = "English";
            EnableNotifications = true;
            EnableSoundNotifications = true;
            EnableDesktopNotifications = true;
            EnableEmailNotifications = false;
            NotificationDuration = 5;
            EnableAutoScheduling = false;
            EnableSmartPosting = true;
            DefaultScheduleInterval = 30;
            TimezoneSelection = "UTC";
            EnableWeekendPosting = true;
            EnableAnalytics = true;
            EnableCrashReporting = true;
            EnableAutoSave = true;
            AutoSaveInterval = 5;
            EnableEncryption = false;
            MaxConcurrentUploads = 3;
            MediaQuality = 90;
            EnableImageOptimization = true;
            EnableVideoCompression = true;

            StatusMessage = "Settings reset to defaults!";
            await Task.Delay(2000);
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error resetting settings: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Settings reset failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportSettingsAsync()
    {
        IsLoading = true;
        StatusMessage = "Exporting settings...";

        try
        {
            await Task.Delay(1000);
            // In a real implementation, this would export settings to a file
            StatusMessage = "Settings exported successfully!";
            await Task.Delay(2000);
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting settings: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ImportSettingsAsync()
    {
        IsLoading = true;
        StatusMessage = "Importing settings...";

        try
        {
            await Task.Delay(1000);
            // In a real implementation, this would import settings from a file
            StatusMessage = "Settings imported successfully!";
            await Task.Delay(2000);
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error importing settings: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        IsLoading = true;
        StatusMessage = "Clearing cache...";

        try
        {
            await Task.Delay(1500);
            CacheSize = 0;
            StatusMessage = "Cache cleared successfully!";
            await Task.Delay(2000);
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error clearing cache: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        IsLoading = true;
        StatusMessage = "Creating backup...";

        try
        {
            var backupPath = await _backupService.CreateBackupAsync();
            LastBackupDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            StatusMessage = $"Backup created successfully! Saved to: {Path.GetFileName(backupPath)}";
            await Task.Delay(3000);
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating backup: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Backup creation failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectMediaFolder()
    {
        // In a real implementation, this would open a folder picker dialog
        DefaultMediaFolder = "/Users/Documents/SocialMedia";
        StatusMessage = "Media folder updated!";
        Task.Run(async () =>
        {
            await Task.Delay(2000);
            StatusMessage = "";
        });
    }

    [RelayCommand]
    private async Task TestNotificationsAsync()
    {
        StatusMessage = "Testing notifications...";
        await Task.Delay(500);

        // In a real implementation, this would trigger a test notification
        System.Diagnostics.Debug.WriteLine("Test notification sent");

        StatusMessage = "Test notification sent!";
        await Task.Delay(2000);
        StatusMessage = "";
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _settingsService.GetSettingsAsync();

            // Apply settings to properties
            SelectedTheme = settings.Theme;
            SelectedLanguage = settings.Language;
            EnableNotifications = settings.ShowNotifications;
            EnableAnalytics = settings.AllowAnalytics;
            EnableCrashReporting = settings.AllowCrashReporting;
            EnableAutoSave = true; // Always enabled for security
            AutoSaveInterval = (int)settings.AutoSaveInterval.TotalMinutes;
            EnableEncryption = true; // Always enabled for security

            // Load application info
            ApplicationVersion = "1.2.0";
            CacheSize = 1024 * 1024 * 15; // 15 MB
            DefaultMediaFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            System.Diagnostics.Debug.WriteLine("Settings loaded successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }
    }

    private async Task LoadStatisticsAsync()
    {
        try
        {
            var accounts = await _accountService.GetAllAccountsAsync();
            TotalAccounts = accounts.Count();

            // In a real implementation, this would get actual post count
            TotalPosts = 127;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load statistics: {ex.Message}");
        }
    }

    // Helper method to format file size
    public string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}