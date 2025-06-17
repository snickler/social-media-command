using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SocialMediaCommander.Desktop.ViewModels;

/// <summary>
/// ViewModel for application settings and preferences
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _autoSaveDrafts = true;
    
    [ObservableProperty]
    private string _defaultPostMode = "Standard";
    
    [ObservableProperty]
    private bool _showCharacterCounter = true;
    
    [ObservableProperty]
    private bool _successNotifications = true;
    
    [ObservableProperty]
    private bool _errorNotifications = true;
    
    [ObservableProperty]
    private bool _clearDraftsOnExit = false;
    
    [ObservableProperty]
    private bool _analyticsEnabled = true;
    
    public SettingsViewModel()
    {
        // Load settings from configuration
        LoadSettings();
    }
    
    [RelayCommand]
    private void SaveSettings()
    {
        // Save settings to configuration
        // This would typically save to app settings or preferences file
    }
    
    [RelayCommand]
    private void ResetToDefaults()
    {
        AutoSaveDrafts = true;
        DefaultPostMode = "Standard";
        ShowCharacterCounter = true;
        SuccessNotifications = true;
        ErrorNotifications = true;
        ClearDraftsOnExit = false;
        AnalyticsEnabled = true;
    }
    
    [RelayCommand]
    private void OpenReleaseNotes()
    {
        // Open release notes URL
    }
    
    [RelayCommand]
    private void OpenWebsite()
    {
        // Open website URL
    }
    
    private void LoadSettings()
    {
        // Load settings from configuration
        // This would typically load from app settings or preferences file
    }
} 