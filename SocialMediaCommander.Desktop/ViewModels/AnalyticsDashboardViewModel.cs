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
/// ViewModel for the analytics dashboard providing comprehensive social media insights
/// </summary>
public partial class AnalyticsDashboardViewModel : ObservableObject
{
    private readonly IPostService _postService;
    private readonly IAccountService _accountService;
    private readonly IFeedService _feedService;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private DateTime _selectedDateFrom = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime _selectedDateTo = DateTime.Today;

    [ObservableProperty]
    private TimeRangeOption? _selectedTimeRange;

    [ObservableProperty]
    private int _totalPosts = 0;

    [ObservableProperty]
    private int _totalEngagement = 0;

    [ObservableProperty]
    private double _engagementRate = 0.0;

    [ObservableProperty]
    private int _totalReach = 0;

    [ObservableProperty]
    private int _newFollowers = 0;

    [ObservableProperty]
    private double _growthRate = 0.0;

    [ObservableProperty]
    private string _bestPostingTime = "2:00 PM";

    [ObservableProperty]
    private string _topPerformingPlatform = "LinkedIn";

    public AnalyticsDashboardViewModel(
        IPostService postService,
        IAccountService accountService,
        IFeedService feedService)
    {
        _postService = postService ?? throw new ArgumentNullException(nameof(postService));
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _feedService = feedService ?? throw new ArgumentNullException(nameof(feedService));

        // Initialize collections
        PlatformMetrics = new ObservableCollection<PlatformMetricViewModel>();
        EngagementChartData = new ObservableCollection<ChartDataPoint>();
        ReachChartData = new ObservableCollection<ChartDataPoint>();
        TopPosts = new ObservableCollection<TopPostViewModel>();
        TimeRangeOptions = new ObservableCollection<TimeRangeOption>();

        InitializeTimeRangeOptions();
        SelectedTimeRange = TimeRangeOptions.FirstOrDefault(t => t.Id == "30d");
        LoadInitialData();

        PropertyChanged += OnPropertyChanged;
    }

    #region Properties

    public ObservableCollection<PlatformMetricViewModel> PlatformMetrics { get; }
    public ObservableCollection<ChartDataPoint> EngagementChartData { get; }
    public ObservableCollection<ChartDataPoint> ReachChartData { get; }
    public ObservableCollection<TopPostViewModel> TopPosts { get; }
    public ObservableCollection<TimeRangeOption> TimeRangeOptions { get; }

    public string EngagementRateFormatted => $"{EngagementRate:F1}%";
    public string GrowthRateFormatted => $"{GrowthRate:+F1;-F1;0}%";
    public string TotalEngagementFormatted => FormatNumber(TotalEngagement);
    public string TotalReachFormatted => FormatNumber(TotalReach);
    public string NewFollowersFormatted => FormatNumber(NewFollowers);

    public bool HasData => PlatformMetrics.Any() || TopPosts.Any();

    #endregion

    #region Commands

    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        try
        {
            IsLoading = true;
            await LoadAnalyticsData();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to refresh analytics data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ChangeTimeRangeAsync(TimeRangeOption timeRange)
    {
        SelectedTimeRange = timeRange;
        UpdateDateRangeFromSelection(timeRange?.Id ?? "30d");
        await RefreshDataAsync();
    }

    [RelayCommand]
    private async Task ExportDataAsync()
    {
        try
        {
            // Simulate export functionality
            await Task.Delay(1000);
            System.Diagnostics.Debug.WriteLine("Analytics data exported successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to export data: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ViewPostDetails(TopPostViewModel post)
    {
        System.Diagnostics.Debug.WriteLine($"Viewing details for post: {post.Content}");
        // In a real implementation, this would open a detailed view
    }

    #endregion

    #region Private Methods

    private void InitializeTimeRangeOptions()
    {
        TimeRangeOptions.Clear();
        TimeRangeOptions.Add(new TimeRangeOption { Id = "7d", Name = "Last 7 days" });
        TimeRangeOptions.Add(new TimeRangeOption { Id = "30d", Name = "Last 30 days" });
        TimeRangeOptions.Add(new TimeRangeOption { Id = "90d", Name = "Last 3 months" });
        TimeRangeOptions.Add(new TimeRangeOption { Id = "1y", Name = "Last year" });
        TimeRangeOptions.Add(new TimeRangeOption { Id = "custom", Name = "Custom range" });
    }

    private void UpdateDateRangeFromSelection(string timeRange)
    {
        var endDate = DateTime.Today;
        var startDate = timeRange switch
        {
            "7d" => endDate.AddDays(-7),
            "30d" => endDate.AddDays(-30),
            "90d" => endDate.AddDays(-90),
            "1y" => endDate.AddYears(-1),
            _ => SelectedDateFrom
        };

        SelectedDateFrom = startDate;
        SelectedDateTo = endDate;
    }

    private async void LoadInitialData()
    {
        await LoadAnalyticsData();
    }

    private async Task LoadAnalyticsData()
    {
        try
        {
            IsLoading = true;

            // Generate mock analytics data
            await GenerateMockAnalyticsData();
            
            // Update computed properties
            OnPropertyChanged(nameof(HasData));
            OnPropertyChanged(nameof(EngagementRateFormatted));
            OnPropertyChanged(nameof(GrowthRateFormatted));
            OnPropertyChanged(nameof(TotalEngagementFormatted));
            OnPropertyChanged(nameof(TotalReachFormatted));
            OnPropertyChanged(nameof(NewFollowersFormatted));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load analytics data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task GenerateMockAnalyticsData()
    {
        await Task.Delay(500); // Simulate API call

        var random = new Random();
        var daysDiff = (SelectedDateTo - SelectedDateFrom).Days;

        // Generate platform metrics
        PlatformMetrics.Clear();
        var platforms = new[]
        {
            new { Name = "LinkedIn", Color = "#0A66C2", Icon = "💼" },
            new { Name = "X (Twitter)", Color = "#000000", Icon = "🐦" },
            new { Name = "BlueSky", Color = "#0085FF", Icon = "🦋" },
            new { Name = "Threads", Color = "#000000", Icon = "🧵" },
            new { Name = "Facebook", Color = "#1877F2", Icon = "📘" }
        };

        foreach (var platform in platforms)
        {
            var posts = random.Next(5, 25);
            var engagement = random.Next(100, 2000);
            var reach = random.Next(1000, 10000);
            var followers = random.Next(50, 500);

            PlatformMetrics.Add(new PlatformMetricViewModel
            {
                PlatformName = platform.Name,
                PlatformColor = platform.Color,
                PlatformIcon = platform.Icon,
                PostCount = posts,
                TotalEngagement = engagement,
                TotalReach = reach,
                NewFollowers = followers,
                EngagementRate = (double)engagement / reach * 100,
                FollowerGrowth = random.NextDouble() * 20 - 5 // -5% to +15%
            });
        }

        // Generate chart data
        EngagementChartData.Clear();
        ReachChartData.Clear();

        for (int i = 0; i <= daysDiff; i++)
        {
            var date = SelectedDateFrom.AddDays(i);
            var engagement = random.Next(50, 300);
            var reach = random.Next(500, 2000);

            EngagementChartData.Add(new ChartDataPoint
            {
                Date = date,
                Value = engagement,
                Label = date.ToString("MMM dd")
            });

            ReachChartData.Add(new ChartDataPoint
            {
                Date = date,
                Value = reach,
                Label = date.ToString("MMM dd")
            });
        }

        // Generate top posts
        TopPosts.Clear();
        var samplePosts = new[]
        {
            "🚀 Excited to share our latest product update! The response has been incredible.",
            "💡 Just published a new article about sustainable business practices. Check it out!",
            "🎉 Thank you to everyone who attended our webinar yesterday! Amazing turnout.",
            "📊 New research shows interesting trends in remote work adoption.",
            "🌟 Proud to announce our team's achievement this quarter!"
        };

        for (int i = 0; i < 5; i++)
        {
            TopPosts.Add(new TopPostViewModel
            {
                Id = $"post-{i}",
                Content = samplePosts[i],
                Platform = platforms[random.Next(platforms.Length)].Name,
                PostedDate = DateTime.Now.AddDays(-random.Next(1, daysDiff)),
                Likes = random.Next(50, 500),
                Comments = random.Next(5, 50),
                Shares = random.Next(2, 25),
                TotalEngagement = 0 // Will be calculated
            });

            TopPosts[i].TotalEngagement = TopPosts[i].Likes + TopPosts[i].Comments + TopPosts[i].Shares;
        }

        // Sort top posts by engagement
        var sortedPosts = TopPosts.OrderByDescending(p => p.TotalEngagement).ToList();
        TopPosts.Clear();
        foreach (var post in sortedPosts)
        {
            TopPosts.Add(post);
        }

        // Update summary metrics
        TotalPosts = PlatformMetrics.Sum(p => p.PostCount);
        TotalEngagement = PlatformMetrics.Sum(p => p.TotalEngagement);
        TotalReach = PlatformMetrics.Sum(p => p.TotalReach);
        NewFollowers = PlatformMetrics.Sum(p => p.NewFollowers);
        EngagementRate = TotalReach > 0 ? (double)TotalEngagement / TotalReach * 100 : 0;
        GrowthRate = PlatformMetrics.Average(p => p.FollowerGrowth);

        // Calculate best posting time (mock)
        var hours = new[] { "9:00 AM", "12:00 PM", "2:00 PM", "5:00 PM", "7:00 PM" };
        BestPostingTime = hours[random.Next(hours.Length)];

        // Determine top performing platform
        TopPerformingPlatform = PlatformMetrics
            .OrderByDescending(p => p.EngagementRate)
            .FirstOrDefault()?.PlatformName ?? "LinkedIn";
    }

    private static string FormatNumber(int number)
    {
        return number switch
        {
            >= 1000000 => $"{number / 1000000.0:F1}M",
            >= 1000 => $"{number / 1000.0:F1}K",
            _ => number.ToString()
        };
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Handle property change notifications if needed
    }

    #endregion
}

// Supporting ViewModels
public partial class PlatformMetricViewModel : ObservableObject
{
    [ObservableProperty]
    private string _platformName = string.Empty;

    [ObservableProperty]
    private string _platformColor = "#6B7280";

    [ObservableProperty]
    private string _platformIcon = "📱";

    [ObservableProperty]
    private int _postCount = 0;

    [ObservableProperty]
    private int _totalEngagement = 0;

    [ObservableProperty]
    private int _totalReach = 0;

    [ObservableProperty]
    private int _newFollowers = 0;

    [ObservableProperty]
    private double _engagementRate = 0.0;

    [ObservableProperty]
    private double _followerGrowth = 0.0;

    public string EngagementRateFormatted => $"{EngagementRate:F1}%";
    public string FollowerGrowthFormatted => $"{FollowerGrowth:+F1;-F1;0}%";
    public string TotalEngagementFormatted => FormatNumber(TotalEngagement);
    public string TotalReachFormatted => FormatNumber(TotalReach);

    private static string FormatNumber(int number)
    {
        return number switch
        {
            >= 1000000 => $"{number / 1000000.0:F1}M",
            >= 1000 => $"{number / 1000.0:F1}K",
            _ => number.ToString()
        };
    }
}

public partial class ChartDataPoint : ObservableObject
{
    [ObservableProperty]
    private DateTime _date;

    [ObservableProperty]
    private double _value;

    [ObservableProperty]
    private string _label = string.Empty;
}

public partial class TopPostViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _platform = string.Empty;

    [ObservableProperty]
    private DateTime _postedDate;

    [ObservableProperty]
    private int _likes = 0;

    [ObservableProperty]
    private int _comments = 0;

    [ObservableProperty]
    private int _shares = 0;

    [ObservableProperty]
    private int _totalEngagement = 0;

    public string PostedDateFormatted => PostedDate.ToString("MMM dd, yyyy");
    public string TotalEngagementFormatted => FormatNumber(TotalEngagement);

    private static string FormatNumber(int number)
    {
        return number switch
        {
            >= 1000000 => $"{number / 1000000.0:F1}M",
            >= 1000 => $"{number / 1000.0:F1}K",
            _ => number.ToString()
        };
    }
}

public partial class TimeRangeOption : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;
} 