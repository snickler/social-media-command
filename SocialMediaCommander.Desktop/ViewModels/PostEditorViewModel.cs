using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;
using SocialMediaCommander.Core.Services;
using Serilog;

namespace SocialMediaCommander.Desktop.ViewModels;

/// <summary>
/// ViewModel for the post editor component with performance optimizations
/// following Microsoft's MVVM guidelines using CommunityToolkit.Mvvm
/// </summary>
public partial class PostEditorViewModel : ObservableObject
{
    private readonly ILogger _logger = LoggingService.ForContext<PostEditorViewModel>();
    private readonly IPostService _postService;
    private readonly IAccountService _accountService;
    private readonly IMediaService _mediaService;

    [ObservableProperty]
    private string _content = "Test post content for social media platforms!";

    [ObservableProperty]
    private bool _promoMode = false;

    [ObservableProperty]
    private bool _isThread = false;

    [ObservableProperty]
    private bool _threadsOnlyMode = false;

    [ObservableProperty]
    private bool _compact = false;

    [ObservableProperty]
    private bool _isPublishing = false;

    [ObservableProperty]
    private string _activeTab = "composer";

    [ObservableProperty]
    private int _maxCharacterCount = 280;

    [ObservableProperty]
    private bool _isPosting = false;

    [ObservableProperty]
    private string _newHashtag = string.Empty;

    public PostEditorViewModel(
        IPostService postService,
        IAccountService accountService,
        IMediaService mediaService)
    {
        _postService = postService ?? throw new ArgumentNullException(nameof(postService));
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));

        // Initialize collections with optimal performance settings
        SelectedPlatforms = new ObservableCollection<SocialPlatform>();
        Hashtags = new ObservableCollection<string>();
        Media = new ObservableCollection<Media>();
        ThreadPosts = new ObservableCollection<ThreadPostViewModel>();
        PlatformPreviews = new ObservableCollection<PlatformPreview>();
        CharacterCounts = new ObservableCollection<PlatformCharacterCount>();

        // Initialize available platforms from configuration
        AvailablePlatforms = new ObservableCollection<PlatformViewModel>();

        var allPlatforms = PlatformConfigurations.GetAllPlatforms().ToList();

        foreach (var config in allPlatforms)
        {
            var platformViewModel = new PlatformViewModel
            {
                Platform = config.Id,
                Name = config.Name,
                Color = config.Color,
                CharacterLimit = config.CharacterLimit,
                IsSelected = config.Id == SocialPlatform.BlueSky // TODO: Add default selections for Twitter, LinkedIn, Facebook, Threads when implementations are ready
            };

            // Subscribe to property changes to update selected platforms
            platformViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PlatformViewModel.IsSelected))
                {
                    UpdateSelectedPlatforms();
                }
            };

            AvailablePlatforms.Add(platformViewModel);
        }



        // Update selected platforms collection
        UpdateSelectedPlatforms();

        // Initialize with main post if thread mode
        if (IsThread && ThreadPosts.Count == 0)
        {
            AddThreadPost();
        }

        // Load accounts for all platforms
        _ = LoadAccountsForAllPlatformsAsync();

        PropertyChanged += OnPropertyChanged;
    }

    #region Computed Properties

    public bool HasContent => !string.IsNullOrWhiteSpace(Content) ||
                             ThreadPosts.Any(p => !string.IsNullOrWhiteSpace(p.Content));

    public bool ThreadSupported => SelectedPlatforms.Any(p =>
        PlatformConfigurations.GetPlatformConfig(p).ThreadSupport);

    public bool MediaSupported => SelectedPlatforms.Any(p =>
        PlatformConfigurations.GetPlatformConfig(p).MediaSupport);

    public bool CanPost
    {
        get
        {
            var canPost = HasContent && SelectedPlatforms.Any() && !IsPublishing;
            System.Diagnostics.Debug.WriteLine($"CanPost: {canPost} (HasContent: {HasContent}, SelectedPlatforms: {SelectedPlatforms.Count}, IsPublishing: {IsPublishing})");
            return canPost;
        }
    }

    // Tab management properties
    public bool IsComposerTabActive => ActiveTab == "composer";
    public bool IsPreviewTabActive => ActiveTab == "preview";

    // Platform selection properties
    public bool HasSelectedPlatforms => SelectedPlatforms.Any();

    // Status and UI properties
    public string StatusMessage => HasSelectedPlatforms
        ? $"Posting to {SelectedPlatforms.Count} platform{(SelectedPlatforms.Count > 1 ? "s" : "")}{(IsThread && ThreadPosts.Any() ? " as thread" : "")}"
        : "Select at least one platform to post";

    public string PostButtonText => IsThread && ThreadPosts.Any() ? "Post Thread" : "Post";

    // Thread posts properties
    public bool HasThreadPosts => ThreadPosts.Any();
    public bool HasNoThreadPosts => !ThreadPosts.Any();

    #endregion

    #region Collections

    public ObservableCollection<SocialPlatform> SelectedPlatforms { get; }
    public ObservableCollection<string> Hashtags { get; }
    public ObservableCollection<Media> Media { get; }
    public ObservableCollection<ThreadPostViewModel> ThreadPosts { get; }
    public ObservableCollection<PlatformPreview> PlatformPreviews { get; }
    public ObservableCollection<PlatformCharacterCount> CharacterCounts { get; }
    public ObservableCollection<PlatformViewModel> AvailablePlatforms { get; }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task PostAsync()
    {
        _logger.Information("PostAsync command called");

        if (IsPosting) return;

        try
        {
            IsPosting = true;
            _logger.Information("Starting post publishing...");

            var post = CreatePostFromViewModel();
            _logger.Information("Calling PublishPostAsync with {PlatformCount} platforms", post.TargetPlatforms.Count);
            var result = await _postService.PublishPostAsync(post);

            _logger.Information("Post published successfully! Results: {ResultCount} platforms", result.Count);

            // Reset form after successful post
            Content = string.Empty;
            Media.Clear();
            if (IsThread)
            {
                ThreadPosts.Clear();
                if (!ThreadsOnlyMode)
                {
                    IsThread = false;
                }
            }

            // Notify success
            OnPostPublished?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Post publishing failed: {Message}", ex.Message);
            OnError?.Invoke($"Failed to publish post: {ex.Message}");
        }
        finally
        {
            IsPosting = false;
        }
    }

    [RelayCommand]
    private async Task SaveDraftAsync()
    {
        try
        {
            var post = CreatePostFromViewModel();
            post.Status = PostStatus.Draft;
            await _postService.SaveDraftAsync(post);
            OnDraftSaved?.Invoke();
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Failed to save draft: {ex.Message}");
        }
    }

    [RelayCommand]
    private void AddThreadPost()
    {
        var newPost = new ThreadPostViewModel
        {
            Content = string.Empty,
            OrderIndex = ThreadPosts.Count,
            Media = new ObservableCollection<Media>()
        };
        ThreadPosts.Add(newPost);
        OnPropertyChanged(nameof(HasThreadPosts));
        OnPropertyChanged(nameof(HasNoThreadPosts));
        OnPropertyChanged(nameof(PostButtonText));
        OnPropertyChanged(nameof(StatusMessage));
    }

    [RelayCommand]
    private void RemoveThreadPost(ThreadPostViewModel threadPost)
    {
        if (threadPost != null)
        {
            ThreadPosts.Remove(threadPost);
            UpdateThreadPostIndices();
            OnPropertyChanged(nameof(HasThreadPosts));
            OnPropertyChanged(nameof(HasNoThreadPosts));
            OnPropertyChanged(nameof(PostButtonText));
            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    [RelayCommand]
    private void MoveThreadPostUp(ThreadPostViewModel threadPost)
    {
        var index = ThreadPosts.IndexOf(threadPost);
        if (index > 0)
        {
            ThreadPosts.RemoveAt(index);
            ThreadPosts.Insert(index - 1, threadPost);
            UpdateThreadPostIndices();
        }
    }

    [RelayCommand]
    private void MoveThreadPostDown(ThreadPostViewModel threadPost)
    {
        var index = ThreadPosts.IndexOf(threadPost);
        if (index < ThreadPosts.Count - 1)
        {
            ThreadPosts.RemoveAt(index);
            ThreadPosts.Insert(index + 1, threadPost);
            UpdateThreadPostIndices();
        }
    }

    [RelayCommand]
    private void AddHashtag()
    {
        if (!string.IsNullOrWhiteSpace(NewHashtag) && !Hashtags.Contains(NewHashtag))
        {
            Hashtags.Add(NewHashtag);
            NewHashtag = string.Empty;
        }
    }

    [RelayCommand]
    private void RemoveHashtag(string hashtag)
    {
        System.Diagnostics.Debug.WriteLine($"[PostEditorViewModel] RemoveHashtag called with: {hashtag}");

        if (!string.IsNullOrEmpty(hashtag))
        {
            var removed = Hashtags.Remove(hashtag);
            System.Diagnostics.Debug.WriteLine($"[PostEditorViewModel] Hashtag removed: {removed}. Remaining: {Hashtags.Count}");

            // Force UI update
            OnPropertyChanged(nameof(Hashtags));
            _ = UpdatePreviewsAsync();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[PostEditorViewModel] RemoveHashtag called with null or empty hashtag");
        }
    }

    [RelayCommand]
    private void TogglePlatform(PlatformViewModel platform)
    {
        if (platform != null)
        {
            platform.IsSelected = !platform.IsSelected;
            UpdateSelectedPlatforms();
        }
    }

    [RelayCommand]
    private Task UploadMediaAsync()
    {
        System.Diagnostics.Debug.WriteLine("[PostEditorViewModel] UploadMedia command called!");

        try
        {
            // Check if we can add more media
            if (Media.Count >= 4)
            {
                OnError?.Invoke("Maximum of 4 media files reached");
                System.Diagnostics.Debug.WriteLine("[PostEditorViewModel] Cannot upload - limit reached");
                return Task.CompletedTask;
            }

            // Trigger event for UI to handle file dialog
            // The view should subscribe to this and show Avalonia's OpenFileDialog
            OnMediaUploadRequested?.Invoke();

            System.Diagnostics.Debug.WriteLine("[PostEditorViewModel] Media upload requested event fired");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PostEditorViewModel] Upload media failed: {ex.Message}");
            OnError?.Invoke($"Failed to upload media: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void RemoveMedia(Media media)
    {
        System.Diagnostics.Debug.WriteLine($"[PostEditorViewModel] RemoveMedia called for: {media?.FileName}");

        if (media != null && Media.Contains(media))
        {
            Media.Remove(media);
            System.Diagnostics.Debug.WriteLine($"[PostEditorViewModel] Media removed. Remaining: {Media.Count}");

            // Force UI updates
            OnPropertyChanged(nameof(Media));
            OnPropertyChanged(nameof(MediaSupported));
            _ = UpdatePreviewsAsync();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[PostEditorViewModel] RemoveMedia called with null or non-existent media");
        }
    }

    /// <summary>
    /// Called by the view after user selects files
    /// </summary>
    public async Task ProcessSelectedFilesAsync(string[] filePaths)
    {
        System.Diagnostics.Debug.WriteLine($"[PostEditorViewModel] Processing {filePaths.Length} selected files");

        foreach (var filePath in filePaths)
        {
            try
            {
                if (Media.Count >= 4)
                {
                    OnError?.Invoke("Maximum of 4 media files reached");
                    break;
                }

                System.Diagnostics.Debug.WriteLine($"[PostEditorViewModel] Uploading file: {filePath}");

                var media = await _mediaService.UploadMediaAsync(filePath).ConfigureAwait(false);

                // Update UI on UI thread
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Media.Add(media);
                    OnPropertyChanged(nameof(Media));
                    OnPropertyChanged(nameof(MediaSupported));
                    _ = UpdatePreviewsAsync();

                    System.Diagnostics.Debug.WriteLine($"[PostEditorViewModel] Media added: {media.FileName}. Total: {Media.Count}");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PostEditorViewModel] Failed to upload {filePath}: {ex.Message}");
                OnError?.Invoke($"Failed to upload {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void SwitchToComposer()
    {
        ActiveTab = "composer";
    }

    [RelayCommand]
    private void SwitchToPreview()
    {
        ActiveTab = "preview";
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Loads accounts for all platforms and populates the account selection UI
    /// </summary>
    private async Task LoadAccountsForAllPlatformsAsync()
    {
        try
        {
            var allAccounts = await _accountService.GetAllAccountsAsync().ConfigureAwait(false);

            // Update UI on UI thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var platformVM in AvailablePlatforms)
                {
                    // Get accounts for this platform
                    var platformAccounts = allAccounts
                        .Where(a => a.PlatformId == platformVM.Platform)
                        .ToList();

                    platformVM.AvailableAccounts.Clear();

                    foreach (var account in platformAccounts)
                    {
                        var accountItem = new AccountSelectionItem
                        {
                            Id = account.Id,
                            Username = account.Username,
                            DisplayName = account.DisplayName ?? account.Username,
                            IsDefault = account.IsDefault,
                            IsAuthenticated = account.IsAuthenticated
                        };

                        platformVM.AvailableAccounts.Add(accountItem);
                    }

                    // Auto-select default account or first authenticated account
                    if (platformVM.AvailableAccounts.Any())
                    {
                        platformVM.SelectedAccount = platformVM.AvailableAccounts.FirstOrDefault(a => a.IsDefault)
                                                   ?? platformVM.AvailableAccounts.FirstOrDefault(a => a.IsAuthenticated)
                                                   ?? platformVM.AvailableAccounts.First();
                    }

                    System.Diagnostics.Debug.WriteLine($"[PostEditorViewModel] Loaded {platformVM.AvailableAccounts.Count} accounts for {platformVM.Name}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PostEditorViewModel] Failed to load accounts: {ex.Message}");
        }
    }

    private Post CreatePostFromViewModel()
    {
        var post = new Post
        {
            Id = Guid.NewGuid().ToString(),
            Content = this.Content,
            CreatedAt = DateTime.UtcNow,
            Status = PostStatus.Draft,
            TargetPlatforms = new List<SocialPlatform>(this.SelectedPlatforms),
            Media = new List<Media>(this.Media),
            IsThread = this.IsThread,
            PromoMode = this.PromoMode,
            Hashtags = new List<string>(this.Hashtags),
            ThreadPosts = this.ThreadPosts.Select(tp => new ThreadPost
            {
                Content = tp.Content,
                Media = new List<Media>(tp.Media)
            }).ToList()
        };

        // Debug logging
        _logger.Information("CreatePostFromViewModel: IsThread={IsThread}, ThreadPosts.Count={ThreadPostsCount}", post.IsThread, post.ThreadPosts.Count);
        _logger.Information("  Main post: Content length={ContentLength}, Media count={MediaCount}", post.Content?.Length ?? 0, post.Media.Count);
        for (int i = 0; i < post.ThreadPosts.Count; i++)
        {
            var tp = post.ThreadPosts[i];
            _logger.Information("  ThreadPost[{Index}]: Content length={ContentLength}, Media count={MediaCount}", i, tp.Content?.Length ?? 0, tp.Media.Count);
        }

        // Populate SelectedAccounts with the user's selected account for each platform
        foreach (var platform in this.SelectedPlatforms)
        {
            var platformVM = AvailablePlatforms.FirstOrDefault(p => p.Platform == platform);
            if (platformVM?.SelectedAccount != null)
            {
                post.SelectedAccounts[platform] = new List<string> { platformVM.SelectedAccount.Id };
                System.Diagnostics.Debug.WriteLine($"[PostEditorViewModel] Selected account for {platform}: {platformVM.SelectedAccount.Username}");
            }
            else
            {
                // Fallback to "default" if no account selected (shouldn't happen with auto-selection)
                post.SelectedAccounts[platform] = new List<string> { "default" };
                System.Diagnostics.Debug.WriteLine($"[PostEditorViewModel] WARNING: No account selected for {platform}, using default");
            }
        }

        System.Diagnostics.Debug.WriteLine($"[PostEditorViewModel] Created post with {post.TargetPlatforms.Count} platforms and {post.SelectedAccounts.Count} account selections");

        return post;
    }

    private void UpdateSelectedPlatforms()
    {
        SelectedPlatforms.Clear();
        foreach (var p in AvailablePlatforms.Where(p => p.IsSelected))
        {
            SelectedPlatforms.Add(p.Platform);
        }

        FilterPlatformsForThreadSupport();
        UpdateCharacterCounts();
        _ = UpdatePreviewsAsync(); // Fire-and-forget with discard to suppress CS4014
        OnPropertyChanged(nameof(HasSelectedPlatforms));
        OnPropertyChanged(nameof(CanPost));
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(ThreadSupported));
        OnPropertyChanged(nameof(MediaSupported));
    }

    private void FilterPlatformsForThreadSupport()
    {
        if (ThreadsOnlyMode)
        {
            foreach (var platform in AvailablePlatforms)
            {
                if (!PlatformSupportsThreads(platform.Platform))
                {
                    platform.IsSelected = false;
                }
            }
        }
    }

    private bool PlatformSupportsThreads(SocialPlatform platform)
    {
        return PlatformConfigurations.GetPlatformConfig(platform).ThreadSupport;
    }

    private void UpdateCharacterCounts()
    {
        CharacterCounts.Clear();
        foreach (var platform in AvailablePlatforms.Where(p => p.IsSelected))
        {
            CharacterCounts.Add(new PlatformCharacterCount
            {
                Platform = platform.Platform,
                PlatformName = platform.Name,
                Count = CalculateCharacterCount(Content, platform.Platform),
                Limit = platform.CharacterLimit,
                IsValid = (platform.CharacterLimit == null || CalculateCharacterCount(Content, platform.Platform) <= platform.CharacterLimit)
            });
        }
    }

    private int CalculateCharacterCount(string content, SocialPlatform platform)
    {
        // This is a simplified calculation. Real-world scenarios might be more complex.
        return content?.Length ?? 0;
    }

    private async Task UpdatePreviewsAsync()
    {
        try
        {
            PlatformPreviews.Clear();

            var post = CreatePostFromViewModel();
            if (!post.TargetPlatforms.Any()) return;

            var previews = await _postService.GetPostPreviewsAsync(post);

            foreach (var preview in previews)
            {
                var platformConfig = PlatformConfigurations.GetPlatformConfig(preview.Key);
                var characterCount = CalculateCharacterCount(preview.Value, preview.Key);

                PlatformPreviews.Add(new PlatformPreview
                {
                    Platform = preview.Key,
                    PlatformName = platformConfig.Name,
                    FormattedContent = preview.Value,
                    RawContent = post.Content,
                    Hashtags = post.Hashtags.ToList(),
                    HasHashtags = post.Hashtags.Any(),
                    IsThread = post.IsThread,
                    ThreadPostCount = post.ThreadPosts.Count,
                    HasMedia = post.Media.Any(),
                    MediaCount = post.Media.Count,
                    CharacterCount = characterCount,
                    CharacterLimit = platformConfig.CharacterLimit,
                    IsOverLimit = platformConfig.CharacterLimit.HasValue && characterCount > platformConfig.CharacterLimit.Value
                });
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw to prevent UI crashes
            System.Diagnostics.Debug.WriteLine($"Error updating previews: {ex.Message}");
        }
    }

    private void UpdateThreadPostIndices()
    {
        for (int i = 0; i < ThreadPosts.Count; i++)
        {
            ThreadPosts[i].OrderIndex = i;
        }
    }

    #endregion

    #region Property Changed Handlers

    partial void OnContentChanged(string value)
    {
        UpdateCharacterCounts();
        _ = UpdatePreviewsAsync(); // Fire-and-forget with discard to suppress CS4014
    }

    partial void OnPromoModeChanged(bool value)
    {
        _ = UpdatePreviewsAsync(); // Fire-and-forget with discard to suppress CS4014
    }

    partial void OnIsThreadChanged(bool value)
    {
        _ = UpdatePreviewsAsync(); // Fire-and-forget with discard to suppress CS4014
    }

    partial void OnThreadsOnlyModeChanged(bool value)
    {
        if (value)
        {
            IsThread = true;
        }
        FilterPlatformsForThreadSupport();
        UpdateSelectedPlatforms();
    }

    partial void OnIsPublishingChanged(bool value)
    {
        // This can be used to trigger UI updates, e.g., showing a loading indicator
    }

    #endregion

    #region Events

    public event Action? OnPostPublished;
    public event Action? OnDraftSaved;
    public event Action<string>? OnError;
    public event Action? OnMediaUploadRequested;

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Content):
                UpdateCharacterCounts();
                _ = UpdatePreviewsAsync(); // Fire-and-forget with discard to suppress CS4014
                OnPropertyChanged(nameof(HasContent));
                OnPropertyChanged(nameof(CanPost));
                OnPropertyChanged(nameof(StatusMessage));
                OnPropertyChanged(nameof(PostButtonText));
                break;
            case nameof(IsThread):
                if (IsThread && ThreadPosts.Count == 0)
                {
                    AddThreadPost();
                }
                FilterPlatformsForThreadSupport();
                UpdateCharacterCounts();
                _ = UpdatePreviewsAsync(); // Fire-and-forget with discard to suppress CS4014
                OnPropertyChanged(nameof(StatusMessage));
                OnPropertyChanged(nameof(PostButtonText));
                break;
            case nameof(ActiveTab):
                OnPropertyChanged(nameof(IsComposerTabActive));
                OnPropertyChanged(nameof(IsPreviewTabActive));
                break;
            case nameof(IsPublishing):
                OnPropertyChanged(nameof(CanPost));
                break;
        }
    }

    public void AddMediaFile(string filePath)
    {
        if (Media.Count >= 4)
        {
            // Max 4 media files per post
            return;
        }

        var media = Helpers.MediaHelper.CreateMediaFromFile(filePath);
        Media.Add(media);
    }

    #endregion
}

/// <summary>
/// Supporting classes for platform-specific data display
/// </summary>
public partial class PlatformPreview : ObservableObject
{
    [ObservableProperty]
    private SocialPlatform _platform;

    [ObservableProperty]
    private string _platformName = string.Empty;

    [ObservableProperty]
    private string _formattedContent = string.Empty;

    [ObservableProperty]
    private string _rawContent = string.Empty;

    [ObservableProperty]
    private List<string> _hashtags = new();

    [ObservableProperty]
    private bool _hasHashtags = false;

    [ObservableProperty]
    private bool _isThread = false;

    [ObservableProperty]
    private int _threadPostCount = 0;

    [ObservableProperty]
    private bool _hasMedia = false;

    [ObservableProperty]
    private int _mediaCount = 0;

    [ObservableProperty]
    private int _characterCount = 0;

    [ObservableProperty]
    private int? _characterLimit = null;

    [ObservableProperty]
    private bool _isOverLimit = false;

    public string CharacterCountText => CharacterLimit.HasValue
        ? $"{CharacterCount}/{CharacterLimit}"
        : CharacterCount.ToString();

    public string ThreadIndicatorText => IsThread
        ? $"Thread with {ThreadPostCount + 1} posts"
        : string.Empty;

    public string MediaIndicatorText => HasMedia
        ? $"{MediaCount} media attachment{(MediaCount > 1 ? "s" : "")}"
        : string.Empty;

    [RelayCommand]
    private async Task CopyToClipboard()
    {
        try
        {
            // Use Avalonia's clipboard functionality
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var clipboard = desktop.MainWindow?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(FormattedContent);
                    System.Diagnostics.Debug.WriteLine($"Successfully copied to clipboard: {FormattedContent}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to copy to clipboard: {ex.Message}");
        }
    }
}

public class PlatformCharacterCount
{
    public SocialPlatform Platform { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public int Count { get; set; }
    public int? Limit { get; set; }
    public bool IsValid { get; set; }
}

public partial class PlatformViewModel : ObservableObject
{
    [ObservableProperty]
    private SocialPlatform _platform;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _color = string.Empty;

    [ObservableProperty]
    private int? _characterLimit;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private ObservableCollection<AccountSelectionItem> _availableAccounts = new();

    [ObservableProperty]
    private AccountSelectionItem? _selectedAccount;

    public bool HasCharacterLimit => CharacterLimit.HasValue;
    public bool HasMultipleAccounts => AvailableAccounts.Count > 1;
    public bool HasAccounts => AvailableAccounts.Any();
    public string AccountDisplayText => SelectedAccount != null
        ? $"@{SelectedAccount.Username}"
        : "No account selected";

    partial void OnIsSelectedChanged(bool value)
    {
        // This will be called when IsSelected changes
        // The parent ViewModel should handle the platform selection updates
    }

    partial void OnSelectedAccountChanged(AccountSelectionItem? value)
    {
        OnPropertyChanged(nameof(AccountDisplayText));
    }
}

/// <summary>
/// Represents an account that can be selected for posting
/// </summary>
public partial class AccountSelectionItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private bool _isDefault = false;

    [ObservableProperty]
    private bool _isAuthenticated = false;

    public string DisplayText => !string.IsNullOrEmpty(DisplayName)
        ? $"{DisplayName} (@{Username}){(IsDefault ? " � Default" : "")}"
        : $"@{Username}{(IsDefault ? " � Default" : "")}";
}

public partial class ThreadPostViewModel : ObservableObject
{
    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private int _orderIndex;

    [ObservableProperty]
    private int _characterCount;

    public ObservableCollection<Media> Media { get; set; } = new();

    public string DisplayIndex => $"Post {OrderIndex}";

    public bool HasContent => !string.IsNullOrWhiteSpace(Content);

    public bool HasMedia => Media.Any();

    [RelayCommand]
    private void RemoveMedia(Media media)
    {
        if (media != null)
        {
            Media.Remove(media);
        }
    }

    [RelayCommand]
    private async Task UploadMedia()
    {
        // Trigger event for file picker (handled in code-behind)
        OnMediaUploadRequested?.Invoke();
    }

    public event Action? OnMediaUploadRequested;

    public void AddMediaFile(string filePath)
    {
        if (Media.Count >= 4)
        {
            // Max 4 media files per post
            return;
        }

        var media = Helpers.MediaHelper.CreateMediaFromFile(filePath);
        Media.Add(media);
    }

    partial void OnContentChanged(string value)
    {
        CharacterCount = value?.Length ?? 0;
    }
}