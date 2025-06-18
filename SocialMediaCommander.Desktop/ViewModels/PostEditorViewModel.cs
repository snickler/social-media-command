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

namespace SocialMediaCommander.Desktop.ViewModels;

/// <summary>
/// ViewModel for the post editor component with performance optimizations
/// following Microsoft's MVVM guidelines using CommunityToolkit.Mvvm
/// </summary>
public partial class PostEditorViewModel : ObservableObject
{
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
                IsSelected = config.Id == SocialPlatform.BlueSky || config.Id == SocialPlatform.LinkedIn || config.Id == SocialPlatform.Facebook
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
        
        // Initialize default hashtags
        foreach (var tag in new[] { "socialmedia", "digitalmarketing", "marketing", "socialmediamarketing", "somehashtag" })
        {
            Hashtags.Add(tag);
        }
        
        // Update selected platforms collection
        UpdateSelectedPlatforms();
        
        // Initialize with main post if thread mode
        if (IsThread && ThreadPosts.Count == 0)
        {
            AddThreadPost();
        }
        
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
        System.Diagnostics.Debug.WriteLine("PostAsync command called!");
        Console.WriteLine("PostAsync command called!");
        
        if (IsPosting) return;
        
        try
        {
            IsPosting = true;
            System.Diagnostics.Debug.WriteLine("Starting post publishing...");
            
            var post = CreatePostFromViewModel();
            var result = await _postService.PublishPostAsync(post);
            
            System.Diagnostics.Debug.WriteLine($"Post published successfully! Results: {result.Count} platforms");
            
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
            System.Diagnostics.Debug.WriteLine($"Post publishing failed: {ex.Message}");
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
        if (!string.IsNullOrEmpty(hashtag))
        {
            Hashtags.Remove(hashtag);
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
    private void UploadMedia()
    {
        OnMediaUploadRequested?.Invoke();
    }
    
    #endregion
    
    #region Private Methods
    
    private Post CreatePostFromViewModel()
    {
        return new Post
        {
            Id = Guid.NewGuid().ToString(),
            Content = this.Content,
            CreatedAt = DateTime.UtcNow,
            Status = PostStatus.Draft,
            TargetPlatforms = new List<SocialPlatform>(this.SelectedPlatforms),
            Media = new List<Media>(this.Media),
            IsThread = this.IsThread,
            ThreadPosts = this.ThreadPosts.Select(tp => new ThreadPost
            {
                Content = tp.Content,
                Media = new List<Media>(tp.Media)
            }).ToList()
        };
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
        UpdatePreviews();
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
    
    private async void UpdatePreviews()
    {
        PlatformPreviews.Clear();
        
        var post = CreatePostFromViewModel();
        if (!post.TargetPlatforms.Any()) return;
        
        var previews = await _postService.GetPostPreviewsAsync(post);
        
        foreach (var preview in previews)
        {
            PlatformPreviews.Add(new PlatformPreview
            {
                Platform = preview.Key,
                PlatformName = preview.Key.ToString(),
                FormattedContent = preview.Value
            });
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
        UpdatePreviews();
    }
    
    partial void OnPromoModeChanged(bool value)
    {
        UpdatePreviews();
    }
    
    partial void OnIsThreadChanged(bool value)
    {
        UpdatePreviews();
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
                UpdatePreviews();
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
                UpdatePreviews();
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
    
    #endregion
}

/// <summary>
/// Supporting classes for platform-specific data display
/// </summary>
public class PlatformPreview
{
    public SocialPlatform Platform { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public string FormattedContent { get; set; } = string.Empty;
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
    
    public bool HasCharacterLimit => CharacterLimit.HasValue;
    
    partial void OnIsSelectedChanged(bool value)
    {
        // This will be called when IsSelected changes
        // The parent ViewModel should handle the platform selection updates
    }
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
    private void UploadMedia()
    {
        // This would typically open a file dialog and handle the upload
        // For this view model, we can simulate adding a media item
        // OnMediaUploadRequested?.Invoke(this);
    }
    
    partial void OnContentChanged(string value)
    {
        CharacterCount = value?.Length ?? 0;
    }
} 