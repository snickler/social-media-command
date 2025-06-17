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
    private string _content = string.Empty;
    
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
        
        // Initialize available platforms
        AvailablePlatforms = new ObservableCollection<PlatformViewModel>
        {
            new PlatformViewModel { Platform = SocialPlatform.BlueSky, Name = "BlueSky", Color = "#0085FF", CharacterLimit = 300, IsSelected = true },
            new PlatformViewModel { Platform = SocialPlatform.X, Name = "X", Color = "#000000", CharacterLimit = 280, IsSelected = false },
            new PlatformViewModel { Platform = SocialPlatform.LinkedIn, Name = "LinkedIn", Color = "#0A66C2", CharacterLimit = null, IsSelected = true },
            new PlatformViewModel { Platform = SocialPlatform.Threads, Name = "Threads", Color = "#000000", CharacterLimit = 500, IsSelected = false },
            new PlatformViewModel { Platform = SocialPlatform.Facebook, Name = "Facebook", Color = "#1877F2", CharacterLimit = null, IsSelected = true }
        };
        
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
    
    public bool CanPost => HasContent && SelectedPlatforms.Any() && !IsPublishing;
    
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
        if (IsPosting) return;
        
        try
        {
            IsPosting = true;
            
            var post = CreatePostFromViewModel();
            var result = await _postService.PublishPostAsync(post);
            
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
    }
    
    [RelayCommand]
    private void RemoveThreadPost(ThreadPostViewModel threadPost)
    {
        if (threadPost != null)
        {
            ThreadPosts.Remove(threadPost);
            UpdateThreadPostIndices();
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
        if (!string.IsNullOrWhiteSpace(NewHashtag))
        {
            var tag = NewHashtag.Trim().TrimStart('#').ToLower();
            if (!Hashtags.Contains(tag))
            {
                Hashtags.Add(tag);
                NewHashtag = string.Empty;
            }
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
            UpdateCharacterCounts();
        }
    }
    
    [RelayCommand]
    private async Task UploadMediaAsync()
    {
        // This would typically open a file picker
        // For now, this is a placeholder
        OnMediaUploadRequested?.Invoke();
    }
    
    #endregion
    
    #region Helper Methods
    
    private Post CreatePostFromViewModel()
    {
        var post = new Post
        {
            Id = Guid.NewGuid().ToString(),
            Content = Content,
            TargetPlatforms = SelectedPlatforms.ToList(),
            Media = Media.ToList(),
            Hashtags = Hashtags.ToList(),
            CreatedAt = DateTime.UtcNow,
            Status = PostStatus.Draft
        };
        
        if (IsThread && ThreadPosts.Any())
        {
            post.ThreadPosts = ThreadPosts.Select(tp => new ThreadPost
            {
                Id = Guid.NewGuid().ToString(),
                Content = tp.Content,
                Order = tp.OrderIndex,
                Media = tp.Media.ToList()
            }).ToList();
        }
        
        return post;
    }
    
    private void UpdateSelectedPlatforms()
    {
        SelectedPlatforms.Clear();
        foreach (var platform in AvailablePlatforms.Where(p => p.IsSelected))
        {
            SelectedPlatforms.Add(platform.Platform);
        }
    }
    
    private void FilterPlatformsForThreadSupport()
    {
        foreach (var platform in AvailablePlatforms)
        {
            // Only keep platforms that support threads in threads-only mode
            if (ThreadsOnlyMode && !PlatformSupportsThreads(platform.Platform))
            {
                platform.IsSelected = false;
            }
        }
        UpdateSelectedPlatforms();
    }
    
    private bool PlatformSupportsThreads(SocialPlatform platform)
    {
        return platform switch
        {
            SocialPlatform.X => true,
            SocialPlatform.Threads => true,
            SocialPlatform.BlueSky => true,
            _ => false
        };
    }
    
    private void UpdateCharacterCounts()
    {
        CharacterCounts.Clear();
        
        foreach (var platform in AvailablePlatforms.Where(p => p.IsSelected))
        {
            var count = CalculateCharacterCount(Content, platform.Platform);
            CharacterCounts.Add(new PlatformCharacterCount
            {
                Platform = platform.Platform,
                PlatformName = platform.Name,
                Count = count,
                Limit = platform.CharacterLimit,
                IsValid = platform.CharacterLimit == null || count <= platform.CharacterLimit
            });
        }
    }
    
    private int CalculateCharacterCount(string content, SocialPlatform platform)
    {
        if (string.IsNullOrEmpty(content)) return 0;
        
        // Platform-specific character counting logic
        return platform switch
        {
            SocialPlatform.X => content.Length, // X has specific URL shortening rules
            SocialPlatform.BlueSky => content.Length,
            SocialPlatform.Threads => content.Length,
            _ => content.Length
        };
    }
    
    private async void UpdatePreviews()
    {
        if (SelectedPlatforms.Any())
        {
            try
            {
                var post = CreatePostFromViewModel();
                var previews = await _postService.GetPostPreviewsAsync(post);
                
                PlatformPreviews.Clear();
                foreach (var preview in previews)
                {
                    var platform = AvailablePlatforms.FirstOrDefault(p => p.Platform == preview.Key);
                    if (platform != null)
                    {
                        PlatformPreviews.Add(new PlatformPreview
                        {
                            Platform = preview.Key,
                            PlatformName = platform.Name,
                            FormattedContent = preview.Value
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Failed to update previews: {ex.Message}");
            }
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

    #region Property Change Handling
    
    partial void OnContentChanged(string value)
    {
        OnPropertyChanged(nameof(HasContent));
        OnPropertyChanged(nameof(CanPost));
        
        // Trigger async updates
        _ = Task.Run(() => {
            UpdatePreviews();
            UpdateCharacterCounts();
        });
    }
    
    partial void OnPromoModeChanged(bool value)
    {
        _ = Task.Run(() => UpdatePreviews());
    }
    
    partial void OnIsThreadChanged(bool value)
    {
        _ = Task.Run(() => UpdatePreviews());
    }
    
    partial void OnThreadsOnlyModeChanged(bool value)
    {
        if (value)
        {
            IsThread = true;
            FilterPlatformsForThreadSupport();
            ActiveTab = "thread";
        }
    }
    
    partial void OnIsPublishingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanPost));
    }
    
    #endregion

    #region Event Handling
    
    public event Action? OnPostPublished;
    public event Action? OnDraftSaved;
    public event Action<string>? OnError;
    public event Action? OnMediaUploadRequested;
    
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Content):
            case nameof(IsThread):
                UpdateCharacterCounts();
                UpdatePreviews();
                break;
            case nameof(ThreadsOnlyMode):
                if (ThreadsOnlyMode)
                {
                    IsThread = true;
                    FilterPlatformsForThreadSupport();
                }
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
    private SocialPlatform platform;
    
    [ObservableProperty]
    private string name = string.Empty;
    
    [ObservableProperty]
    private string color = string.Empty;
    
    [ObservableProperty]
    private int? characterLimit;
    
    [ObservableProperty]
    private bool isSelected;
}

public partial class ThreadPostViewModel : ObservableObject
{
    [ObservableProperty]
    private string content = string.Empty;
    
    [ObservableProperty]
    private int orderIndex;
    
    public ObservableCollection<Media> Media { get; set; } = new();
} 