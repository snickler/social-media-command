using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    private SocialPlatform? _selectedPlatform = null;
    
    [ObservableProperty]
    private string _searchQuery = string.Empty;
    
    [ObservableProperty]
    private DateTime? _selectedDate = null;
    
    [ObservableProperty]
    private string _errorMessage = string.Empty;
    
    [ObservableProperty]
    private bool _hasError = false;

    public SocialFeedViewModel(IFeedService feedService, IAccountService accountService)
    {
        _feedService = feedService ?? throw new ArgumentNullException(nameof(feedService));
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        
        // Initialize collections
        FeedItems = new ObservableCollection<SocialFeedItem>();
        FilteredFeedItems = new ObservableCollection<SocialFeedItem>();
        AvailablePlatforms = new ObservableCollection<SocialPlatform>();
        
        // Load initial data
        _ = Task.Run(InitializeAsync);
    }
    
    #region Properties
    
    public ObservableCollection<SocialFeedItem> FeedItems { get; }
    public ObservableCollection<SocialFeedItem> FilteredFeedItems { get; }
    public ObservableCollection<SocialPlatform> AvailablePlatforms { get; }
    
    // Computed properties
    public bool HasFeedItems => FeedItems.Any();
    public bool HasFilteredFeedItems => FilteredFeedItems.Any();
    public int TotalFeedItemCount => FeedItems.Count;
    public int FilteredFeedItemCount => FilteredFeedItems.Count;
    
    public string StatusText
    {
        get
        {
            if (IsLoading) return "Loading posts...";
            if (IsRefreshing) return "Refreshing feed...";
            if (HasError) return $"Error: {ErrorMessage}";
            if (!HasFeedItems) return "No posts available";
            if (!HasFilteredFeedItems && (!string.IsNullOrEmpty(SearchQuery) || SelectedPlatform.HasValue))
                return "No posts match your filters";
            return $"Showing {FilteredFeedItemCount} of {TotalFeedItemCount} posts";
        }
    }
    
    #endregion
    
    #region Commands
    
    [RelayCommand]
    private async Task RefreshFeedAsync()
    {
        try
        {
            IsRefreshing = true;
            HasError = false;
            ErrorMessage = string.Empty;
            
            await LoadFeedItemsAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"Refresh feed failed: {ex.Message}");
        }
        finally
        {
            IsRefreshing = false;
            OnPropertyChanged(nameof(StatusText));
        }
    }
    
    [RelayCommand]
    private void ClearFilters()
    {
        SelectedPlatform = null;
        SearchQuery = string.Empty;
        SelectedDate = null;
        ApplyFilters();
    }
    
    [RelayCommand]
    private void ApplyPlatformFilter(SocialPlatform platform)
    {
        SelectedPlatform = SelectedPlatform == platform ? null : platform;
        ApplyFilters();
    }
    
    [RelayCommand]
    private async Task LoadMoreFeedItemsAsync()
    {
        try
        {
            if (IsLoading) return;
            
            IsLoading = true;
            
            // Load more feed items (pagination)
            var platforms = SelectedPlatform.HasValue ? new[] { SelectedPlatform.Value } : AvailablePlatforms.ToArray();
            var moreFeedItems = await _feedService.GetFeedItemsAsync(platforms, 20);
            
            foreach (var item in moreFeedItems.Skip(FeedItems.Count))
            {
                FeedItems.Add(item);
            }
            
            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"Load more feed items failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(StatusText));
        }
    }
    
    [RelayCommand]
    private async Task LikeFeedItemAsync(SocialFeedItem feedItem)
    {
        try
        {
            // Toggle like status
            var currentLikes = feedItem.Engagement.Likes;
            var isLiked = feedItem.Engagement.CustomMetrics.ContainsKey("isLiked") && 
                Convert.ToBoolean(feedItem.Engagement.CustomMetrics["isLiked"]);
            
            feedItem.Engagement.Likes = isLiked ? currentLikes - 1 : currentLikes + 1;
            feedItem.Engagement.CustomMetrics["isLiked"] = isLiked ? 0 : 1;
            
            // Simulate async operation
            await Task.Delay(100);
            
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Like feed item failed: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task ShareFeedItemAsync(SocialFeedItem feedItem)
    {
        try
        {
            // Implement share functionality
            System.Diagnostics.Debug.WriteLine($"Sharing feed item: {feedItem.Id}");
            
            // Simulate async operation
            await Task.Delay(100);
            
            // Update share count
            feedItem.Engagement.Shares += 1;
            
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Share feed item failed: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task SearchFeedAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                ApplyFilters();
                return;
            }
            
            IsLoading = true;
            
            var platforms = SelectedPlatform.HasValue ? new[] { SelectedPlatform.Value } : null;
            var searchResults = await _feedService.SearchFeedItemsAsync(SearchQuery, platforms);
            
            FeedItems.Clear();
            foreach (var item in searchResults)
            {
                FeedItems.Add(item);
            }
            
            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"Search feed failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(StatusText));
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    private async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            HasError = false;
            
            // Load available platforms
            var accounts = await _accountService.GetAllAccountsAsync();
            var platforms = accounts.Select(a => a.PlatformId).Distinct().ToList();
            
            AvailablePlatforms.Clear();
            foreach (var platform in platforms)
            {
                AvailablePlatforms.Add(platform);
            }
            
            // Load initial feed items
            await LoadFeedItemsAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"Initialize failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(StatusText));
        }
    }
    
    private async Task LoadFeedItemsAsync()
    {
        try
        {
            var platforms = AvailablePlatforms.Any() ? AvailablePlatforms.ToArray() : Enum.GetValues<SocialPlatform>();
            var feedItems = await _feedService.GetFeedItemsAsync(platforms, 50);
            
            FeedItems.Clear();
            foreach (var item in feedItems)
            {
                FeedItems.Add(item);
            }
            
            ApplyFilters();
            
            OnPropertyChanged(nameof(HasFeedItems));
            OnPropertyChanged(nameof(TotalFeedItemCount));
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load feed items: {ex.Message}", ex);
        }
    }
    
    private void ApplyFilters()
    {
        var filtered = FeedItems.AsEnumerable();
        
        // Platform filter
        if (SelectedPlatform.HasValue)
        {
            filtered = filtered.Where(item => item.Platform == SelectedPlatform.Value);
        }
        
        // Search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var query = SearchQuery.ToLowerInvariant();
            filtered = filtered.Where(item => 
                item.Content.ToLowerInvariant().Contains(query) ||
                item.AuthorName.ToLowerInvariant().Contains(query) ||
                item.Hashtags.Any(h => h.ToLowerInvariant().Contains(query)));
        }
        
        // Date filter
        if (SelectedDate.HasValue)
        {
            var targetDate = SelectedDate.Value.Date;
            filtered = filtered.Where(item => item.PostedAt.Date == targetDate);
        }
        
        // Sort by creation date (newest first)
        filtered = filtered.OrderByDescending(item => item.PostedAt);
        
        FilteredFeedItems.Clear();
        foreach (var item in filtered)
        {
            FilteredFeedItems.Add(item);
        }
        
        OnPropertyChanged(nameof(HasFilteredFeedItems));
        OnPropertyChanged(nameof(FilteredFeedItemCount));
        OnPropertyChanged(nameof(StatusText));
    }
    
    #endregion
    
    #region Property Change Handling
    
    partial void OnSearchQueryChanged(string value)
    {
        // Debounce search to avoid too many filter operations
        _ = Task.Delay(300).ContinueWith(_ => ApplyFilters());
    }
    
    partial void OnSelectedPlatformChanged(SocialPlatform? value)
    {
        ApplyFilters();
    }
    
    partial void OnSelectedDateChanged(DateTime? value)
    {
        ApplyFilters();
    }
    
    #endregion
} 