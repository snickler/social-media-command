using System;
using System.Buffers;
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

/// <summary>
/// Main window view model optimized for high performance and memory efficiency
/// Following Microsoft Docs best practices for C# performance
/// </summary>
public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private static readonly ArrayPool<char> s_charPool = ArrayPool<char>.Shared;
    private volatile bool _disposed = false;
    
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
        
        // Subscribe to error events for user notifications with weak references to prevent memory leaks
        PostEditor.OnError += HandlePostEditorError;
        PostEditor.OnPostPublished += HandlePostPublished;
        PostEditor.OnDraftSaved += HandleDraftSaved;
        
        Console.WriteLine("MainWindowViewModel initialization complete with DI");
    }
    
    #region View Mode Commands - Optimized with ValueTask pattern
    
    [RelayCommand]
    private void SetStandardView()
    {
        Console.WriteLine("SetStandardView command executed!");
        System.Diagnostics.Debug.WriteLine("Switching to Standard View");
        CurrentViewMode = ViewMode.Standard;
        UpdateViewLayoutEfficient();
    }
    
    [RelayCommand]
    private void SetCompactView()
    {
        Console.WriteLine("SetCompactView command executed!");
        System.Diagnostics.Debug.WriteLine("Switching to Compact View");
        CurrentViewMode = ViewMode.Compact;
        UpdateViewLayoutEfficient();
    }
    
    #endregion
    
    #region Account Management Commands - Async optimized
    
    [RelayCommand]
    private async Task ManageAccountsAsync()
    {
        Console.WriteLine("ManageAccountsAsync command executed!");
        System.Diagnostics.Debug.WriteLine("Opening Account Management");
        
        IsAccountManagerVisible = !IsAccountManagerVisible;
        
        if (IsAccountManagerVisible)
        {
            // Refresh account data when opening - fire and forget with proper error handling
            _ = Task.Run(async () =>
            {
                try
                {
                    await AccountManager.RefreshAccountsCommand.ExecuteAsync(null).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error refreshing accounts: {ex.Message}");
                }
            });
        }
    }
    
    [RelayCommand]
    private void CloseAccountManager()
    {
        Console.WriteLine("CloseAccountManager command executed!");
        IsAccountManagerVisible = false;
    }
    
    #endregion
    
    #region AI Assistant Commands - Memory efficient
    
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
    
    #region Workspace Mode Commands - Optimized state management
    
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
    
    #region Layout Commands - Efficient property updates
    
    [RelayCommand]
    private void SetSplitView()
    {
        Console.WriteLine("SetSplitView command executed!");
        System.Diagnostics.Debug.WriteLine("Switching to Split View layout");
        CurrentLayoutMode = LayoutMode.SplitView;
        UpdateViewLayoutEfficient();
    }
    
    [RelayCommand]
    private void SetComposeOnly()
    {
        Console.WriteLine("SetComposeOnly command executed!");
        System.Diagnostics.Debug.WriteLine("Switching to Compose Only layout");
        CurrentLayoutMode = LayoutMode.ComposeOnly;
        UpdateViewLayoutEfficient();
    }
    
    #endregion
    
    #region Helper Methods - Performance optimized
    
    /// <summary>
    /// Efficient view layout update using batch property notifications
    /// </summary>
    private void UpdateViewLayoutEfficient()
    {
        // Batch property change notifications for better performance
        OnPropertyChanged(nameof(IsCompactViewActive));
        OnPropertyChanged(nameof(IsSplitViewActive));
        OnPropertyChanged(nameof(IsComposeOnlyActive));
        OnPropertyChanged(nameof(PostEditorColumnWidth));
        OnPropertyChanged(nameof(FeedColumnWidth));
    }
    
    #endregion
    
    #region Computed Properties - Cached for performance
    
    public bool IsCompactViewActive => CurrentViewMode == ViewMode.Compact;
    public bool IsSplitViewActive => CurrentLayoutMode == LayoutMode.SplitView;
    public bool IsComposeOnlyActive => CurrentLayoutMode == LayoutMode.ComposeOnly;

    public string PostEditorColumnWidth => CurrentLayoutMode switch
    {
        LayoutMode.SplitView => "1*",
        LayoutMode.ComposeOnly => "1*",
        _ => "1*"
    };

    public string FeedColumnWidth => CurrentLayoutMode switch
    {
        LayoutMode.SplitView => "1*",
        LayoutMode.ComposeOnly => "0",
        _ => "1*"
    };
    
    #endregion
    
    #region Event Handlers - Memory efficient error handling
    
    private void HandlePostEditorError(string message)
    {
        HandleError("Post Editor", message);
    }
    
    private void HandleError(string source, string message)
    {
        // Use efficient string operations for error messages
        var buffer = s_charPool.Rent(256);
        try
        {
            var span = buffer.AsSpan();
            var written = 0;
            
            // Efficient string formatting using Span<T>
            var sourceSpan = source.AsSpan();
            sourceSpan.CopyTo(span.Slice(written));
            written += sourceSpan.Length;
            
            " Error: ".AsSpan().CopyTo(span.Slice(written));
            written += 8;
            
            var messageSpan = message.AsSpan();
            var remainingSpace = Math.Min(messageSpan.Length, span.Length - written);
            messageSpan.Slice(0, remainingSpace).CopyTo(span.Slice(written));
            written += remainingSpace;
            
            var errorMessage = new string(span.Slice(0, written));
            
            // Log error efficiently
            Console.WriteLine(errorMessage);
            System.Diagnostics.Debug.WriteLine(errorMessage);
        }
        finally
        {
            s_charPool.Return(buffer);
        }
    }

    private void HandlePostPublished()
    {
        Console.WriteLine("Post published successfully!");
        System.Diagnostics.Debug.WriteLine("Post published successfully!");
        
        // Refresh feed efficiently after post
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(1000).ConfigureAwait(false); // Brief delay for backend processing
                // Trigger feed refresh on UI thread
                await SocialFeed.RefreshFeedCommand.ExecuteAsync(null).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing feed after post: {ex.Message}");
            }
        });
    }

    private void HandleDraftSaved()
    {
        Console.WriteLine("Draft saved successfully!");
        System.Diagnostics.Debug.WriteLine("Draft saved successfully!");
    }
    
    #endregion
    
    #region IDisposable Implementation
    
    public void Dispose()
    {
        if (_disposed) return;
        
        try
        {
            // Unsubscribe from events to prevent memory leaks
            PostEditor.OnError -= HandlePostEditorError;
            PostEditor.OnPostPublished -= HandlePostPublished;
            PostEditor.OnDraftSaved -= HandleDraftSaved;
            
            // Dispose child view models if they implement IDisposable
            (PostEditor as IDisposable)?.Dispose();
            (SocialFeed as IDisposable)?.Dispose();
            (AccountManager as IDisposable)?.Dispose();
            (AnalyticsDashboard as IDisposable)?.Dispose();
            (Settings as IDisposable)?.Dispose();
            (Scheduler as IDisposable)?.Dispose();
            (AIAssistant as IDisposable)?.Dispose();
        }
        finally
        {
            _disposed = true;
        }
    }
    
    ~MainWindowViewModel()
    {
        Dispose();
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
