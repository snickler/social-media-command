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

    public SocialFeedViewModel(IFeedService feedService, IAccountService accountService)
    {
        _feedService = feedService ?? throw new ArgumentNullException(nameof(feedService));
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        
        // Initialize collections
        FeedPosts = new ObservableCollection<SocialFeedPostViewModel>();
        PlatformFilters = new ObservableCollection<PlatformFilterViewModel>();
        
        // Initialize platform filters
        InitializePlatformFilters();
        
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
    
    private void InitializePlatformFilters()
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
        
        // Add platform-specific filters
        var platforms = new[]
        {
            new { Id = "bluesky", Name = "BlueSky", Color = "#0085FF" },
            new { Id = "x", Name = "X", Color = "#000000" },
            new { Id = "linkedin", Name = "LinkedIn", Color = "#0A66C2" },
            new { Id = "threads", Name = "Threads", Color = "#000000" },
            new { Id = "facebook", Name = "Facebook", Color = "#1877F2" }
        };
        
        foreach (var platform in platforms)
        {
            PlatformFilters.Add(new PlatformFilterViewModel
            {
                Id = platform.Id,
                Name = platform.Name,
                Color = platform.Color,
                IsSelected = false
            });
        }
    }
    
    private async Task LoadFeedAsync()
    {
        try
        {
            IsLoading = true;
            
            // Simulate loading feed data from multiple platforms
            var feedData = await GenerateMockFeedData();
            
            FeedPosts.Clear();
            foreach (var post in feedData)
            {
                FeedPosts.Add(post);
            }
            
            ApplyPlatformFilter();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"Failed to load feed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(StatusText));
        }
    }
    
    private async Task<List<SocialFeedPostViewModel>> GenerateMockFeedData()
    {
        // Simulate API delay
        await Task.Delay(1000);
        
        var posts = new List<SocialFeedPostViewModel>();
        var random = new Random();
        
        var mockUsers = new[]
        {
            new { Name = "BlueSky User 1", Handle = "@user1", Platform = "bluesky" },
            new { Name = "BlueSky User 2", Handle = "@user2", Platform = "bluesky" },
            new { Name = "BlueSky User 3", Handle = "@user3", Platform = "bluesky" },
            new { Name = "LinkedIn User", Handle = "@linkedinuser", Platform = "linkedin" },
            new { Name = "X User", Handle = "@xuser", Platform = "x" }
        };
        
        var mockContents = new[]
        {
            "Latest updates from the world of tech! #technology #innovation",
            "Just shared my thoughts on sustainable business practices. Check out my latest article!",
            "Amazing conference today! So many insights to share with the community.",
            "Working on some exciting new features. Can't wait to share them with you all!",
            "Great discussion about the future of remote work. What are your thoughts?",
            "Behind the scenes look at our latest project. The team has been incredible!",
            "Interesting article about AI and machine learning trends in 2024.",
            "Beautiful sunset from the office today. Sometimes you need to take a moment to appreciate the little things."
        };
        
        for (int i = 0; i < 15; i++)
        {
            var user = mockUsers[random.Next(mockUsers.Length)];
            var content = mockContents[random.Next(mockContents.Length)];
            var timeAgo = random.Next(1, 180); // 1 to 180 minutes ago
            
            posts.Add(new SocialFeedPostViewModel
            {
                Id = $"post-{i}",
                UserName = user.Name,
                UserHandle = user.Handle,
                Platform = user.Platform,
                Content = content,
                TimeAgo = $"{(timeAgo < 60 ? timeAgo + "m" : (timeAgo / 60) + "h")} ago",
                PostedAt = DateTime.Now.AddMinutes(-timeAgo),
                LikesCount = random.Next(0, 100),
                RetweetsCount = random.Next(0, 50),
                RepliesCount = random.Next(0, 25),
                IsLiked = random.Next(0, 10) < 2, // 20% chance of being liked
                IsRetweeted = random.Next(0, 10) < 1, // 10% chance of being retweeted
                HasMedia = random.Next(0, 10) < 3 // 30% chance of having media
            });
        }
        
        return posts.OrderByDescending(p => p.PostedAt).ToList();
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