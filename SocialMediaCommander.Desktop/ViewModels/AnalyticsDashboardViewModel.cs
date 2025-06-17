using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SocialMediaCommander.Desktop.ViewModels;

/// <summary>
/// ViewModel for analytics dashboard with social media metrics
/// </summary>
public partial class AnalyticsDashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private string _totalPosts = "247";
    
    [ObservableProperty]
    private string _totalEngagement = "12.4K";
    
    [ObservableProperty]
    private string _totalReach = "89.2K";
    
    [ObservableProperty]
    private string _totalFollowers = "5.8K";
    
    [ObservableProperty]
    private string _selectedTimeRange = "Last 7 days";
    
    [ObservableProperty]
    private bool _isLoading = false;
    
    public AnalyticsDashboardViewModel()
    {
        ActivityItems = new ObservableCollection<ActivityItem>();
        PlatformStats = new ObservableCollection<PlatformStat>();
        EngagementData = new ObservableCollection<EngagementDataPoint>();
        
        LoadMockData();
    }
    
    #region Properties
    
    public ObservableCollection<ActivityItem> ActivityItems { get; }
    public ObservableCollection<PlatformStat> PlatformStats { get; }
    public ObservableCollection<EngagementDataPoint> EngagementData { get; }
    
    #endregion
    
    #region Commands
    
    [RelayCommand]
    private async Task ExportReportAsync()
    {
        try
        {
            IsLoading = true;
            
            // Simulate report generation
            await Task.Delay(2000);
            
            // In a real implementation, this would generate and save a report
            System.Diagnostics.Debug.WriteLine("Analytics report exported successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Export report failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        try
        {
            IsLoading = true;
            
            // Simulate data refresh
            await Task.Delay(1500);
            
            // Update metrics with new data
            TotalPosts = $"{Random.Shared.Next(200, 300)}";
            TotalEngagement = $"{Random.Shared.Next(10, 20)}.{Random.Shared.Next(0, 9)}K";
            TotalReach = $"{Random.Shared.Next(80, 100)}.{Random.Shared.Next(0, 9)}K";
            TotalFollowers = $"{Random.Shared.Next(5, 8)}.{Random.Shared.Next(0, 9)}K";
            
            LoadMockData();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Refresh data failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private void ChangeTimeRange(string timeRange)
    {
        SelectedTimeRange = timeRange;
        _ = RefreshDataAsync();
    }
    
    #endregion
    
    #region Helper Methods
    
    private void LoadMockData()
    {
        // Load activity items
        ActivityItems.Clear();
        var activities = new[]
        {
            new ActivityItem
            {
                Icon = "🚀",
                Title = "Post published successfully",
                Description = "Your post 'New product launch' was published to 3 platforms",
                TimeAgo = "2 min ago",
                Type = ActivityType.Success
            },
            new ActivityItem
            {
                Icon = "❤️",
                Title = "High engagement detected",
                Description = "Your post is getting 2x more engagement than usual",
                TimeAgo = "15 min ago",
                Type = ActivityType.Engagement
            },
            new ActivityItem
            {
                Icon = "📊",
                Title = "Weekly report ready",
                Description = "Your analytics report for this week is now available",
                TimeAgo = "1 hour ago",
                Type = ActivityType.Report
            },
            new ActivityItem
            {
                Icon = "⚠️",
                Title = "Account connection issue",
                Description = "LinkedIn account needs to be reconnected",
                TimeAgo = "2 hours ago",
                Type = ActivityType.Warning
            },
            new ActivityItem
            {
                Icon = "🎯",
                Title = "Goal achieved",
                Description = "You've reached your monthly engagement goal!",
                TimeAgo = "1 day ago",
                Type = ActivityType.Achievement
            }
        };
        
        foreach (var activity in activities)
        {
            ActivityItems.Add(activity);
        }
        
        // Load platform stats
        PlatformStats.Clear();
        var platforms = new[]
        {
            new PlatformStat
            {
                Name = "BlueSky",
                Percentage = 45,
                Color = "#0085FF",
                Posts = 112,
                Engagement = "5.2K"
            },
            new PlatformStat
            {
                Name = "LinkedIn",
                Percentage = 30,
                Color = "#0A66C2",
                Posts = 74,
                Engagement = "3.8K"
            },
            new PlatformStat
            {
                Name = "Facebook",
                Percentage = 25,
                Color = "#1877F2",
                Posts = 61,
                Engagement = "3.4K"
            }
        };
        
        foreach (var platform in platforms)
        {
            PlatformStats.Add(platform);
        }
        
        // Load engagement data
        EngagementData.Clear();
        var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        for (int i = 0; i < days.Length; i++)
        {
            EngagementData.Add(new EngagementDataPoint
            {
                Day = days[i],
                Value = Random.Shared.Next(100, 300),
                Height = Random.Shared.Next(120, 220)
            });
        }
    }
    
    #endregion
}

/// <summary>
/// Represents an activity item in the dashboard
/// </summary>
public partial class ActivityItem : ObservableObject
{
    [ObservableProperty]
    private string _icon = string.Empty;
    
    [ObservableProperty]
    private string _title = string.Empty;
    
    [ObservableProperty]
    private string _description = string.Empty;
    
    [ObservableProperty]
    private string _timeAgo = string.Empty;
    
    [ObservableProperty]
    private ActivityType _type = ActivityType.Info;
    
    public string BackgroundColor => Type switch
    {
        ActivityType.Success => "#E0E7FF",
        ActivityType.Warning => "#FEF3C7",
        ActivityType.Error => "#FEE2E2",
        ActivityType.Engagement => "#FEF3C7",
        ActivityType.Report => "#D1FAE5",
        ActivityType.Achievement => "#E0E7FF",
        _ => "#F3F4F6"
    };
}

/// <summary>
/// Represents platform statistics
/// </summary>
public partial class PlatformStat : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private int _percentage = 0;
    
    [ObservableProperty]
    private string _color = "#4F46E5";
    
    [ObservableProperty]
    private int _posts = 0;
    
    [ObservableProperty]
    private string _engagement = string.Empty;
}

/// <summary>
/// Represents a data point for engagement charts
/// </summary>
public partial class EngagementDataPoint : ObservableObject
{
    [ObservableProperty]
    private string _day = string.Empty;
    
    [ObservableProperty]
    private int _value = 0;
    
    [ObservableProperty]
    private int _height = 120;
}

/// <summary>
/// Types of activities for styling
/// </summary>
public enum ActivityType
{
    Info,
    Success,
    Warning,
    Error,
    Engagement,
    Report,
    Achievement
} 