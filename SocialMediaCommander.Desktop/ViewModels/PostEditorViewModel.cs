using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
        ThreadPosts = new ObservableCollection<ThreadPost>();
        PlatformPreviews = new ObservableCollection<PlatformPreview>();
        CharacterCounts = new ObservableCollection<PlatformCharacterCount>();
        
        // Set up collection change notifications for computed properties
        SelectedPlatforms.CollectionChanged += (s, e) => {
            OnPropertyChanged(nameof(CanPost));
            OnPropertyChanged(nameof(ThreadSupported));
            OnPropertyChanged(nameof(MediaSupported));
        };
        
        ThreadPosts.CollectionChanged += (s, e) => {
            OnPropertyChanged(nameof(HasContent));
            OnPropertyChanged(nameof(CanPost));
        };
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
    public ObservableCollection<ThreadPost> ThreadPosts { get; }
    public ObservableCollection<PlatformPreview> PlatformPreviews { get; }
    public ObservableCollection<PlatformCharacterCount> CharacterCounts { get; }
    
    #endregion
    
    #region Commands
    
    [RelayCommand(CanExecute = nameof(CanPost))]
    private async Task PublishPostAsync()
    {
        try
        {
            IsPublishing = true;
            
            var post = CreatePostFromViewModel();
            
            // Validate post before publishing
            var validationResults = await _postService.ValidatePostAsync(post);
            
            var failedValidations = validationResults.Where(vr => !vr.Value.IsValid).ToList();
            if (failedValidations.Any())
            {
                // Handle validation errors
                var errors = string.Join("\n", failedValidations.Select(fv => 
                    $"{fv.Key}: {string.Join(", ", fv.Value.Errors)}"));
                
                // In a real implementation, show validation errors to user
                throw new InvalidOperationException($"Validation failed:\n{errors}");
            }
            
            // Publish to selected platforms
            var publishResults = await _postService.PublishPostToPlatformsAsync(post, SelectedPlatforms);
            
            // Handle publish results
            var failedPublishes = publishResults.Where(pr => !pr.Value.Success).ToList();
            if (failedPublishes.Any())
            {
                // Handle partial failures
                var failures = string.Join("\n", failedPublishes.Select(fp => 
                    $"{fp.Key}: {fp.Value.ErrorMessage}"));
                
                // In a real implementation, show publish errors to user
                throw new InvalidOperationException($"Publishing failed for some platforms:\n{failures}");
            }
            
            // Clear form on successful publish
            ClearForm();
        }
        catch (Exception ex)
        {
            // Handle errors - in real implementation, show to user
            System.Diagnostics.Debug.WriteLine($"Post publishing failed: {ex.Message}");
        }
        finally
        {
            IsPublishing = false;
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
            
            // In real implementation, show success message to user
        }
        catch (Exception ex)
        {
            // Handle errors
            System.Diagnostics.Debug.WriteLine($"Save draft failed: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void AddNewThreadPost()
    {
        var newPost = new ThreadPost
        {
            Id = $"thread-{DateTime.Now.Ticks}-{Guid.NewGuid().ToString("N")[..8]}",
            Content = string.Empty,
            Media = new List<Media>()
        };
        
        ThreadPosts.Add(newPost);
    }
    
    [RelayCommand]
    private void CopyTextToClipboard(string text)
    {
        // In a real Avalonia implementation, use the clipboard service
        System.Diagnostics.Debug.WriteLine($"Copying to clipboard: {text}");
    }
    
    #endregion
    
    #region Helper Methods
    
    private Post CreatePostFromViewModel()
    {
        return new Post
        {
            Id = Guid.NewGuid().ToString(),
            Content = Content,
            Media = Media.ToList(),
            ThreadPosts = IsThread ? ThreadPosts.ToList() : new List<ThreadPost>(),
            Hashtags = Hashtags.ToList(),
            TargetPlatforms = SelectedPlatforms.ToList(),
            Status = PostStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            PromoMode = PromoMode,
            IsThread = IsThread,
            ThreadsOnlyMode = ThreadsOnlyMode
        };
    }
    
    private void ClearForm()
    {
        Content = string.Empty;
        Media.Clear();
        ThreadPosts.Clear();
        Hashtags.Clear();
        
        // Reset modes if not sticky
        if (!ThreadsOnlyMode)
        {
            IsThread = false;
        }
        
        ActiveTab = "composer";
    }
    
    private async Task UpdatePlatformPreviewsAsync()
    {
        if (!SelectedPlatforms.Any()) return;
        
        try
        {
            var post = CreatePostFromViewModel();
            var previews = await _postService.GetPostPreviewsAsync(post);
            
            // Update collection on UI thread
            PlatformPreviews.Clear();
            foreach (var preview in previews)
            {
                PlatformPreviews.Add(new PlatformPreview
                {
                    Platform = preview.Key,
                    FormattedContent = preview.Value
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Preview update failed: {ex.Message}");
        }
    }
    
    private async Task UpdateCharacterCountsAsync()
    {
        if (!SelectedPlatforms.Any()) return;
        
        try
        {
            var post = CreatePostFromViewModel();
            var counts = await _postService.GetCharacterCountsAsync(post);
            
            // Update collection on UI thread
            CharacterCounts.Clear();
            foreach (var count in counts)
            {
                var platformConfig = PlatformConfigurations.GetPlatformConfig(count.Key);
                CharacterCounts.Add(new PlatformCharacterCount
                {
                    Platform = count.Key,
                    Count = count.Value,
                    MaxCount = platformConfig.CharacterLimit ?? 280,
                    IsOverLimit = count.Value > (platformConfig.CharacterLimit ?? 280)
                });
            }
            
            // Update maximum character count for main display
            MaxCharacterCount = CharacterCounts.Any() 
                ? CharacterCounts.Max(c => c.MaxCount) 
                : 280;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Character count update failed: {ex.Message}");
        }
    }
    
    #endregion

    #region Property Change Handling
    
    partial void OnContentChanged(string value)
    {
        OnPropertyChanged(nameof(HasContent));
        OnPropertyChanged(nameof(CanPost));
        
        // Trigger async updates
        _ = Task.Run(async () => {
            await UpdatePlatformPreviewsAsync();
            await UpdateCharacterCountsAsync();
        });
    }
    
    partial void OnPromoModeChanged(bool value)
    {
        _ = Task.Run(() => UpdatePlatformPreviewsAsync());
    }
    
    partial void OnIsThreadChanged(bool value)
    {
        _ = Task.Run(() => UpdatePlatformPreviewsAsync());
    }
    
    partial void OnThreadsOnlyModeChanged(bool value)
    {
        if (value)
        {
            IsThread = true;
            ActiveTab = "thread";
        }
    }
    
    partial void OnIsPublishingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanPost));
        PublishPostCommand.NotifyCanExecuteChanged();
    }
    
    #endregion
}

/// <summary>
/// Supporting classes for platform-specific data display
/// </summary>
public class PlatformPreview
{
    public SocialPlatform Platform { get; set; }
    public string FormattedContent { get; set; } = string.Empty;
}

public class PlatformCharacterCount
{
    public SocialPlatform Platform { get; set; }
    public int Count { get; set; }
    public int MaxCount { get; set; }
    public bool IsOverLimit { get; set; }
} 