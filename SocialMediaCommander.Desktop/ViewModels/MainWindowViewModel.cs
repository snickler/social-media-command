using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Social Media Commander";
    
    public PostEditorViewModel PostEditor { get; }
    public SocialFeedViewModel SocialFeed { get; }
    public AccountManagerViewModel AccountManager { get; }
    public AnalyticsDashboardViewModel AnalyticsDashboard { get; }
    public SettingsViewModel Settings { get; }
    public SchedulerViewModel Scheduler { get; }
    public AIAssistantViewModel AIAssistant { get; }
    
    [ObservableProperty]
    private ViewMode currentViewMode = ViewMode.Standard;
    
    [ObservableProperty]
    private WorkspaceMode currentWorkspaceMode = WorkspaceMode.SingleThread;
    
    [ObservableProperty]
    private LayoutMode currentLayoutMode = LayoutMode.SplitView;
    
    [ObservableProperty]
    private bool isAccountManagerVisible = false;
    
    [ObservableProperty]
    private bool isAIAssistantVisible = false;

    public MainWindowViewModel(
        PostEditorViewModel postEditor,
        SocialFeedViewModel socialFeed,
        AccountManagerViewModel accountManager,
        AnalyticsDashboardViewModel analyticsDashboard,
        SettingsViewModel settings,
        SchedulerViewModel scheduler,
        AIAssistantViewModel aiAssistant)
    {
        Console.WriteLine("MainWindowViewModel constructor called with DI");
        
        PostEditor = postEditor;
        SocialFeed = socialFeed;
        AccountManager = accountManager;
        AnalyticsDashboard = analyticsDashboard;
        Settings = settings;
        Scheduler = scheduler;
        AIAssistant = aiAssistant;
        
        // Subscribe to error events for user notifications
        PostEditor.OnError += (message) => HandleError("Post Editor", message);
        PostEditor.OnPostPublished += () => HandlePostPublished();
        PostEditor.OnDraftSaved += () => HandleDraftSaved();
        
        Console.WriteLine("MainWindowViewModel initialization complete with DI");
        
        // Debug: Check if commands are available
        Console.WriteLine($"SetStandardViewCommand is null: {SetStandardViewCommand == null}");
        Console.WriteLine($"SetCompactViewCommand is null: {SetCompactViewCommand == null}");
        Console.WriteLine($"ManageAccountsCommand is null: {ManageAccountsCommand == null}");
        Console.WriteLine($"ToggleAIAssistantCommand is null: {ToggleAIAssistantCommand == null}");
        Console.WriteLine($"SetSplitViewCommand is null: {SetSplitViewCommand == null}");
        Console.WriteLine($"SetComposeOnlyCommand is null: {SetComposeOnlyCommand == null}");
        Console.WriteLine($"SetSingleThreadModeCommand is null: {SetSingleThreadModeCommand == null}");
        Console.WriteLine($"SetThreadsOnlyModeCommand is null: {SetThreadsOnlyModeCommand == null}");
    }
    
    #region View Mode Commands
    
    [RelayCommand]
    private void SetStandardView()
    {
        Console.WriteLine("SetStandardView command executed!");
        System.Diagnostics.Debug.WriteLine("Switching to Standard View");
        CurrentViewMode = ViewMode.Standard;
        UpdateViewLayout();
    }
    
    [RelayCommand]
    private void SetCompactView()
    {
        Console.WriteLine("SetCompactView command executed!");
        System.Diagnostics.Debug.WriteLine("Switching to Compact View");
        CurrentViewMode = ViewMode.Compact;
        UpdateViewLayout();
    }
    
    #endregion
    
    #region Account Management Commands
    
    [RelayCommand]
    private void ManageAccounts()
    {
        Console.WriteLine("ManageAccounts command executed!");
        System.Diagnostics.Debug.WriteLine("Opening Account Management");
        
        IsAccountManagerVisible = !IsAccountManagerVisible;
        
        if (IsAccountManagerVisible)
        {
            // Refresh account data when opening
            _ = AccountManager.RefreshAccountsCommand.ExecuteAsync(null);
        }
    }
    
    [RelayCommand]
    private void CloseAccountManager()
    {
        Console.WriteLine("CloseAccountManager command executed!");
        IsAccountManagerVisible = false;
    }
    
    #endregion
    
    #region AI Assistant Commands
    
    [RelayCommand]
    private void ToggleAIAssistant()
    {
        Console.WriteLine("ToggleAIAssistant command executed!");
        System.Diagnostics.Debug.WriteLine("Toggling AI Assistant");
        
        IsAIAssistantVisible = !IsAIAssistantVisible;
    }
    
    [RelayCommand]
    private void CloseAIAssistant()
    {
        Console.WriteLine("CloseAIAssistant command executed!");
        IsAIAssistantVisible = false;
    }
    
    #endregion
    
    #region Workspace Mode Commands
    
    [RelayCommand]
    private void SetSingleThreadMode()
    {
        Console.WriteLine("SetSingleThreadMode command executed!");
        System.Diagnostics.Debug.WriteLine("Switching to Single/Thread Posts mode");
        CurrentWorkspaceMode = WorkspaceMode.SingleThread;
        PostEditor.ThreadsOnlyMode = false;
    }
    
    [RelayCommand]
    private void SetThreadsOnlyMode()
    {
        Console.WriteLine("SetThreadsOnlyMode command executed!");
        System.Diagnostics.Debug.WriteLine("Switching to Threads Only mode");
        CurrentWorkspaceMode = WorkspaceMode.ThreadsOnly;
        PostEditor.ThreadsOnlyMode = true;
    }
    
    #endregion
    
    #region Layout Commands
    
    [RelayCommand]
    private void SetSplitView()
    {
        Console.WriteLine("SetSplitView command executed!");
        System.Diagnostics.Debug.WriteLine("Switching to Split View layout");
        CurrentLayoutMode = LayoutMode.SplitView;
        UpdateViewLayout();
    }
    
    [RelayCommand]
    private void SetComposeOnly()
    {
        Console.WriteLine("SetComposeOnly command executed!");
        System.Diagnostics.Debug.WriteLine("Switching to Compose Only layout");
        CurrentLayoutMode = LayoutMode.ComposeOnly;
        UpdateViewLayout();
    }
    
    #endregion
    
    #region Helper Methods
    
    private void UpdateViewLayout()
    {
        // Update UI based on current view mode and layout
        OnPropertyChanged(nameof(IsCompactViewActive));
        OnPropertyChanged(nameof(IsSplitViewActive));
        OnPropertyChanged(nameof(IsComposeOnlyActive));
        OnPropertyChanged(nameof(PostEditorColumnWidth));
        OnPropertyChanged(nameof(FeedColumnWidth));
    }
    
    #endregion
    
    #region View State Properties
    
    public bool IsCompactViewActive => CurrentViewMode == ViewMode.Compact;
    public bool IsSplitViewActive => CurrentLayoutMode == LayoutMode.SplitView;
    public bool IsComposeOnlyActive => CurrentLayoutMode == LayoutMode.ComposeOnly;
    
    public string PostEditorColumnWidth => CurrentLayoutMode switch
    {
        LayoutMode.ComposeOnly => "*",
        LayoutMode.SplitView when CurrentViewMode == ViewMode.Compact => "1.5*",
        LayoutMode.SplitView => "2*",
        _ => "*"
    };
    
    public string FeedColumnWidth => CurrentLayoutMode switch
    {
        LayoutMode.ComposeOnly => "0",
        LayoutMode.SplitView when CurrentViewMode == ViewMode.Compact => "*",
        LayoutMode.SplitView => "*",
        _ => "*"
    };
    
    #endregion
    
    #region Event Handlers
    
    private void HandleError(string source, string message)
    {
        Console.WriteLine($"Error from {source}: {message}");
        // TODO: Show error notification to user
    }
    
    private void HandlePostPublished()
    {
        Console.WriteLine("Post published successfully!");
        // TODO: Show success notification to user
        // Refresh feed data
        _ = SocialFeed.RefreshFeedCommand.ExecuteAsync(null);
    }
    
    private void HandleDraftSaved()
    {
        Console.WriteLine("Draft saved successfully!");
        // TODO: Show draft saved notification to user
    }
    
    #endregion
}

// Enums for view modes
public enum ViewMode
{
    Standard,
    Compact
}

public enum WorkspaceMode
{
    SingleThread,
    ThreadsOnly
}

public enum LayoutMode
{
    SplitView,
    ComposeOnly
}

// Mock media service for now
public class MockMediaService : IMediaService
{
    public Task<Media> UploadMediaAsync(Stream fileStream, string fileName, string mimeType)
    {
        var media = new Media
        {
            Id = Guid.NewGuid().ToString(),
            FileName = fileName,
            FilePath = $"mock://uploads/{fileName}",
            MimeType = mimeType,
            Type = GetMediaTypeFromMimeType(mimeType),
            FileSize = fileStream.Length,
            PreviewUrl = $"mock://previews/{fileName}"
        };
        return Task.FromResult(media);
    }
    
    public Task<Media> UploadMediaAsync(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var media = new Media
        {
            Id = Guid.NewGuid().ToString(),
            FileName = fileName,
            FilePath = $"mock://uploads/{fileName}",
            MimeType = GetMimeTypeFromExtension(Path.GetExtension(filePath)),
            Type = GetMediaTypeFromExtension(Path.GetExtension(filePath)),
            FileSize = 1024 * 1024, // Mock 1MB
            PreviewUrl = $"mock://previews/{fileName}"
        };
        return Task.FromResult(media);
    }
    
    public Task<Media?> GetMediaByIdAsync(string id)
    {
        return Task.FromResult<Media?>(null);
    }
    
    public Task<IEnumerable<Media>> GetMediaByIdsAsync(IEnumerable<string> ids)
    {
        return Task.FromResult(Enumerable.Empty<Media>());
    }
    
    public Task<bool> DeleteMediaAsync(string id)
    {
        return Task.FromResult(true);
    }
    
    public Task<string?> GeneratePreviewAsync(string mediaId)
    {
        return Task.FromResult<string?>($"mock://previews/{mediaId}");
    }
    
    public Task<Stream?> GetMediaStreamAsync(string id)
    {
        return Task.FromResult<Stream?>(new MemoryStream());
    }
    
    public Task<ValidationResult> ValidateMediaForPlatformAsync(string mediaId, SocialPlatform platform)
    {
        return Task.FromResult(new ValidationResult());
    }
    
    public Task<Media> OptimizeMediaForPlatformAsync(string mediaId, SocialPlatform platform)
    {
        var media = new Media { Id = mediaId };
        return Task.FromResult(media);
    }
    
    public Task<MediaFormats> GetSupportedFormatsAsync(SocialPlatform platform)
    {
        return Task.FromResult(new MediaFormats());
    }
    
    public Task<int> CleanupUnusedMediaAsync(TimeSpan olderThan)
    {
        return Task.FromResult(0);
    }
    
    public Task<MediaStorageStats> GetStorageStatisticsAsync()
    {
        return Task.FromResult(new MediaStorageStats());
    }
    
    public Task<Media> ProcessMediaAsync(string mediaId, MediaProcessingOptions options)
    {
        var media = new Media { Id = mediaId };
        return Task.FromResult(media);
    }
    
    public Task<ValidationResult> ValidateFileAsync(Stream fileStream, string fileName, string mimeType)
    {
        return Task.FromResult(new ValidationResult());
    }
    
    private static MediaType GetMediaTypeFromMimeType(string mimeType)
    {
        return mimeType.StartsWith("image/") ? MediaType.Image :
               mimeType.StartsWith("video/") ? MediaType.Video :
               MediaType.Image;
    }
    
    private static MediaType GetMediaTypeFromExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" => MediaType.Image,
            ".mp4" or ".avi" or ".mov" or ".webm" => MediaType.Video,
            _ => MediaType.Image
        };
    }
    
    private static string GetMimeTypeFromExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".avi" => "video/avi",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            _ => "application/octet-stream"
        };
    }
}
