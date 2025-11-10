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
/// ViewModel for managing and displaying social media feeds
/// </summary>
public partial class SocialFeedViewModel : ObservableObject
{
    private readonly IFeedService _feedService;
    private readonly IAccountService _accountService;
    private readonly IBlueSkyService _blueSkyService;
    // TODO: Add Twitter, LinkedIn, Threads, Facebook services when implementations are ready

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private bool _isRefreshing = false;

    [ObservableProperty]
    private string _selectedPlatformFilter = "all";

    [ObservableProperty]
    private DateTime _lastRefreshTime = DateTime.Now;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError = false;

    public SocialFeedViewModel(
        IFeedService feedService,
        IAccountService accountService,
        IBlueSkyService blueSkyService)
    {
        _feedService = feedService ?? throw new ArgumentNullException(nameof(feedService));
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _blueSkyService = blueSkyService ?? throw new ArgumentNullException(nameof(blueSkyService));
        // TODO: Add Twitter, LinkedIn, Threads, Facebook services when implementations are ready

        // Initialize collections
        FeedPosts = new ObservableCollection<SocialFeedPostViewModel>();
        PlatformFilters = new ObservableCollection<PlatformFilterViewModel>();

        // Initialize platform filters based on connected accounts
        _ = Task.Run(InitializePlatformFiltersAsync);

        // Load initial feed data
        _ = Task.Run(LoadFeedAsync);

        PropertyChanged += OnPropertyChanged;
    }

    #region Properties

    public ObservableCollection<SocialFeedPostViewModel> FeedPosts { get; }
    public ObservableCollection<PlatformFilterViewModel> PlatformFilters { get; }

    // Computed properties
    public bool HasFeedItems => FeedPosts.Any();
    public int TotalFeedItemCount => FeedPosts.Count;

    public string StatusText
    {
        get
        {
            if (IsLoading) return "Loading posts...";
            if (IsRefreshing) return "Refreshing feed...";
            if (HasError) return $"Error: {ErrorMessage}";
            if (!HasFeedItems) return "No posts available";
            return $"Showing {TotalFeedItemCount} posts";
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task RefreshFeedAsync()
    {
        if (IsRefreshing) return;

        try
        {
            IsRefreshing = true;
            await LoadFeedAsync();
            LastRefreshTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"Failed to refresh feed: {ex.Message}");
        }
        finally
        {
            IsRefreshing = false;
            OnPropertyChanged(nameof(StatusText));
        }
    }

    [RelayCommand]
    private void FilterByPlatform(string platformId)
    {
        SelectedPlatformFilter = platformId;
        ApplyPlatformFilter();
    }

    [RelayCommand]
    private async Task LikePostAsync(SocialFeedPostViewModel post)
    {
        if (post != null)
        {
            post.IsLiked = !post.IsLiked;
            post.LikesCount += post.IsLiked ? 1 : -1;

            // In a real implementation, this would call the platform's API
            await Task.Delay(100); // Simulate API call
        }
    }

    [RelayCommand]
    private async Task RetweetPostAsync(SocialFeedPostViewModel post)
    {
        if (post != null)
        {
            post.IsRetweeted = !post.IsRetweeted;
            post.RetweetsCount += post.IsRetweeted ? 1 : -1;

            // In a real implementation, this would call the platform's API
            await Task.Delay(100); // Simulate API call
        }
    }

    [RelayCommand]
    private void ViewPostDetails(SocialFeedPostViewModel post)
    {
        // In a real implementation, this would navigate to post details
        OnPostDetailsRequested?.Invoke(post);
    }

    #endregion

    #region Helper Methods

    private async Task InitializePlatformFiltersAsync()
    {
        try
        {
            // Get all authenticated accounts
            var accounts = await _accountService.GetAllAccountsAsync().ConfigureAwait(false);
            var authenticatedAccounts = accounts.Where(a => a.IsAuthenticated).ToList();

            System.Diagnostics.Debug.WriteLine($"[SocialFeedViewModel] Found {authenticatedAccounts.Count} authenticated accounts");

            // Update UI on UI thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                PlatformFilters.Clear();

                // Add "All" filter
                PlatformFilters.Add(new PlatformFilterViewModel
                {
                    Id = "all",
                    Name = "All",
                    Color = "#6B7280",
                    IsSelected = true
                });

                // Get unique platforms from authenticated accounts
                var connectedPlatforms = authenticatedAccounts
                    .Select(a => a.PlatformId)
                    .Distinct()
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"[SocialFeedViewModel] Connected platforms: {string.Join(", ", connectedPlatforms)}");

                // Add platform-specific filters only for connected platforms
                foreach (var platform in connectedPlatforms)
                {
                    var config = PlatformConfigurations.GetPlatformConfig(platform);
                    PlatformFilters.Add(new PlatformFilterViewModel
                    {
                        Id = platform.ToString().ToLower(),
                        Name = config.Name,
                        Color = config.Color,
                        IsSelected = false
                    });
                }

                OnPropertyChanged(nameof(PlatformFilters));
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SocialFeedViewModel] Error initializing filters: {ex.Message}");
        }
    }

    private async Task LoadFeedAsync()
    {
        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;

            System.Diagnostics.Debug.WriteLine("[SocialFeedViewModel] Loading feeds from authenticated accounts...");

            // Get all authenticated accounts
            var accounts = await _accountService.GetAllAccountsAsync().ConfigureAwait(false);
            var authenticatedAccounts = accounts.Where(a => a.IsAuthenticated).ToList();

            System.Diagnostics.Debug.WriteLine($"[SocialFeedViewModel] Found {authenticatedAccounts.Count} authenticated accounts");

            var allFeedItems = new List<SocialFeedPostViewModel>();

            // Load feeds from each platform
            foreach (var account in authenticatedAccounts)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[SocialFeedViewModel] Loading feed for {account.PlatformId}: {account.Username}");

                    IEnumerable<SocialFeedItem>? feedItems = null;

                    switch (account.PlatformId)
                    {
                        case SocialPlatform.BlueSky:
                            feedItems = await _blueSkyService.GetTimelineAsync(account, 20).ConfigureAwait(false);
                            break;
                            // TODO: Add Twitter, LinkedIn, Threads, Facebook when implementations are ready
                    }

                    if (feedItems != null && feedItems.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"[SocialFeedViewModel] Loaded {feedItems.Count()} items from {account.PlatformId}");

                        foreach (var item in feedItems)
                        {
                            allFeedItems.Add(ConvertToViewModel(item, account.PlatformId));
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[SocialFeedViewModel] No items returned from {account.PlatformId}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SocialFeedViewModel] Error loading feed from {account.PlatformId}: {ex.Message}");
                    // Continue loading other platforms even if one fails
                }
            }

            // Update UI on UI thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                FeedPosts.Clear();

                if (allFeedItems.Any())
                {
                    // Sort by posted time, newest first
                    var sortedItems = allFeedItems.OrderByDescending(p => p.PostedAt).ToList();

                    foreach (var post in sortedItems)
                    {
                        FeedPosts.Add(post);
                    }

                    System.Diagnostics.Debug.WriteLine($"[SocialFeedViewModel] Displayed {FeedPosts.Count} total feed items");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[SocialFeedViewModel] No feed items to display");
                }

                OnPropertyChanged(nameof(HasFeedItems));
                OnPropertyChanged(nameof(TotalFeedItemCount));
                OnPropertyChanged(nameof(StatusText));
            });

            ApplyPlatformFilter();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"[SocialFeedViewModel] Failed to load feed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[SocialFeedViewModel] Stack trace: {ex.StackTrace}");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(StatusText));
        }
    }

    private SocialFeedPostViewModel ConvertToViewModel(SocialFeedItem item, SocialPlatform platform)
    {
        var platformName = platform.ToString().ToLower();

        return new SocialFeedPostViewModel
        {
            Id = item.PlatformPostId ?? item.Id,
            UserName = item.AuthorName,
            UserHandle = item.AuthorUsername.StartsWith("@") ? item.AuthorUsername : $"@{item.AuthorUsername}",
            Platform = platformName,
            Content = item.Content,
            TimeAgo = item.GetTimeAgo(), // Use the model's built-in method
            PostedAt = item.PostedAt,
            LikesCount = item.LikeCount,
            RetweetsCount = item.RepostCount,
            RepliesCount = item.ReplyCount,
            IsLiked = false, // Would need to check user's like status
            IsRetweeted = false, // Would need to check user's retweet status
            HasMedia = item.Media?.Any() ?? false
        };
    }

    private async Task<List<SocialFeedPostViewModel>> GenerateMockFeedData()
    {
        // Old mock method - no longer used
        // Kept for reference but replaced by LoadFeedAsync
        return new List<SocialFeedPostViewModel>();
    }

    private void ApplyPlatformFilter()
    {
        // Update filter selection
        foreach (var filter in PlatformFilters)
        {
            filter.IsSelected = filter.Id == SelectedPlatformFilter;
        }

        // Apply filter logic would go here
        // For now, we show all posts regardless of filter
        // In a real implementation, this would filter FeedPosts based on SelectedPlatformFilter
    }

    #endregion

    #region Property Change Handling

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SelectedPlatformFilter):
                ApplyPlatformFilter();
                break;
        }
    }

    #endregion

    // Events
    public event Action<SocialFeedPostViewModel>? OnPostDetailsRequested;
}

// Supporting ViewModels
public partial class SocialFeedPostViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _userHandle = string.Empty;

    [ObservableProperty]
    private string _platform = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _timeAgo = string.Empty;

    [ObservableProperty]
    private DateTime _postedAt;

    [ObservableProperty]
    private int _likesCount;

    [ObservableProperty]
    private int _retweetsCount;

    [ObservableProperty]
    private int _repliesCount;

    [ObservableProperty]
    private bool _isLiked;

    [ObservableProperty]
    private bool _isRetweeted;

    [ObservableProperty]
    private bool _hasMedia;

    public string PlatformColor => Platform switch
    {
        "bluesky" => "#0085FF",
        "x" => "#000000",
        "linkedin" => "#0A66C2",
        "threads" => "#000000",
        "facebook" => "#1877F2",
        _ => "#6B7280"
    };

    public string PlatformDisplayName => Platform switch
    {
        "bluesky" => "BlueSky",
        "x" => "X",
        "linkedin" => "LinkedIn",
        "threads" => "Threads",
        "facebook" => "Facebook",
        _ => "Unknown"
    };
}

public partial class PlatformFilterViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _color = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}