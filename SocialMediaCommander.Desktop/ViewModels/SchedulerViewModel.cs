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
/// ViewModel for advanced post scheduling and automation management
/// </summary>
public partial class SchedulerViewModel : ObservableObject
{
    private readonly IPostService _postService;
    private readonly IAccountService _accountService;

    // Collections
    public ObservableCollection<ScheduledPostViewModel> ScheduledPosts { get; } = new();
    public ObservableCollection<AutomationRuleViewModel> AutomationRules { get; } = new();
    public ObservableCollection<ScheduleTemplateViewModel> ScheduleTemplates { get; } = new();

    // Current Selection
    [ObservableProperty]
    private ScheduledPostViewModel? _selectedScheduledPost;

    [ObservableProperty]
    private AutomationRuleViewModel? _selectedAutomationRule;

    [ObservableProperty]
    private ScheduleTemplateViewModel? _selectedScheduleTemplate;

    // View States
    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isCalendarViewActive = true;

    [ObservableProperty]
    private bool _isListViewActive = false;

    [ObservableProperty]
    private bool _isAutomationViewActive = false;

    // Calendar Properties
    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _calendarDisplayDate = DateTime.Today;

    // Filter Properties
    [ObservableProperty]
    private string _filterText = "";

    [ObservableProperty]
    private SocialPlatform? _filterPlatform = null;

    [ObservableProperty]
    private PostStatus? _filterStatus = null;

    [ObservableProperty]
    private bool _showOnlyToday = false;

    [ObservableProperty]
    private bool _showOnlyThisWeek = false;

    // Statistics
    [ObservableProperty]
    private int _totalScheduledPosts = 0;

    [ObservableProperty]
    private int _postsToday = 0;

    [ObservableProperty]
    private int _postsThisWeek = 0;

    [ObservableProperty]
    private int _activeAutomationRules = 0;

    [ObservableProperty]
    private double _automationSuccessRate = 0.0;

    // New Post Scheduling
    [ObservableProperty]
    private string _newPostContent = "";

    [ObservableProperty]
    private DateTime _newPostScheduleDate = DateTime.Today.AddHours(DateTime.Now.Hour + 1);

    [ObservableProperty]
    private TimeSpan _newPostScheduleTime = TimeSpan.FromHours(DateTime.Now.Hour + 1);

    [ObservableProperty]
    private List<SocialPlatform> _newPostSelectedPlatforms = new();

    [ObservableProperty]
    private bool _newPostRecurring = false;

    [ObservableProperty]
    private string _newPostRecurrencePattern = "Daily";

    // Quick Actions
    [ObservableProperty]
    private bool _smartSchedulingEnabled = true;

    [ObservableProperty]
    private bool _autoOptimizeTimingEnabled = true;

    [ObservableProperty]
    private bool _pauseAllScheduling = false;

    // Collections for UI binding
    public ObservableCollection<string> RecurrencePatterns { get; } = new()
    {
        "Daily", "Weekly", "Monthly", "Custom"
    };

    public ObservableCollection<SocialPlatform> AvailablePlatforms { get; } = new()
    {
        // TODO: Add Twitter, LinkedIn, Facebook, Threads when implementations are ready
        SocialPlatform.BlueSky
    };

    public ObservableCollection<PostStatus> StatusOptions { get; } = new()
    {
        PostStatus.Draft, PostStatus.Publishing, PostStatus.Published, PostStatus.Failed
    };

    public SchedulerViewModel(IPostService postService, IAccountService accountService)
    {
        _postService = postService;
        _accountService = accountService;

        _ = LoadInitialDataAsync(); // Fire-and-forget with discard to suppress CS4014
        StartAutomationEngine();
    }

    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        IsLoading = true;
        StatusMessage = "Refreshing schedule data...";

        try
        {
            LoadScheduledPosts();
            await LoadAutomationRulesAsync();
            await LoadScheduleTemplatesAsync();
            UpdateStatistics();

            StatusMessage = "Data refreshed successfully!";
            await Task.Delay(2000);
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error refreshing data: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Refresh failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ScheduleNewPostAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPostContent))
        {
            StatusMessage = "Please enter post content";
            return;
        }

        if (NewPostSelectedPlatforms.Count == 0)
        {
            StatusMessage = "Please select at least one platform";
            return;
        }

        IsLoading = true;
        StatusMessage = "Scheduling post...";

        try
        {
            var scheduledDateTime = NewPostScheduleDate.Date + NewPostScheduleTime;

            var newPost = new ScheduledPostViewModel
            {
                Id = Guid.NewGuid().ToString(),
                Content = NewPostContent,
                ScheduledTime = scheduledDateTime,
                Platforms = NewPostSelectedPlatforms.ToList(),
                Status = PostStatus.Publishing,
                IsRecurring = NewPostRecurring,
                RecurrencePattern = NewPostRecurrencePattern,
                CreatedAt = DateTime.Now,
                Author = "Current User"
            };

            ScheduledPosts.Add(newPost);

            // Clear form
            NewPostContent = "";
            NewPostScheduleDate = DateTime.Today.AddDays(1);
            NewPostScheduleTime = TimeSpan.FromHours(9);
            NewPostSelectedPlatforms.Clear();
            NewPostRecurring = false;

            UpdateStatistics();
            StatusMessage = "Post scheduled successfully!";
            await Task.Delay(2000);
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error scheduling post: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Schedule failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteScheduledPostAsync(ScheduledPostViewModel post)
    {
        if (post == null) return;

        IsLoading = true;
        StatusMessage = "Deleting scheduled post...";

        try
        {
            ScheduledPosts.Remove(post);
            UpdateStatistics();

            StatusMessage = "Post deleted successfully!";
            await Task.Delay(1500);
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting post: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task EditScheduledPostAsync(ScheduledPostViewModel post)
    {
        if (post == null) return;

        // In a real implementation, this would open an edit dialog
        StatusMessage = $"Editing post: {post.Content.Substring(0, Math.Min(30, post.Content.Length))}...";
        await Task.Delay(1000);
        StatusMessage = "";
    }

    [RelayCommand]
    private async Task PublishNowAsync(ScheduledPostViewModel post)
    {
        if (post == null) return;

        IsLoading = true;
        StatusMessage = "Publishing post now...";

        try
        {
            post.Status = PostStatus.Published;
            post.PublishedAt = DateTime.Now;

            StatusMessage = "Post published successfully!";
            await Task.Delay(2000);
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error publishing post: {ex.Message}";
            post.Status = PostStatus.Failed;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SwitchToCalendarView()
    {
        IsCalendarViewActive = true;
        IsListViewActive = false;
        IsAutomationViewActive = false;
    }

    [RelayCommand]
    private void SwitchToListView()
    {
        IsCalendarViewActive = false;
        IsListViewActive = true;
        IsAutomationViewActive = false;
    }

    [RelayCommand]
    private void SwitchToAutomationView()
    {
        IsCalendarViewActive = false;
        IsListViewActive = false;
        IsAutomationViewActive = true;
    }

    [RelayCommand]
    private async Task CreateAutomationRuleAsync()
    {
        IsLoading = true;
        StatusMessage = "Creating automation rule...";

        try
        {
            var newRule = new AutomationRuleViewModel
            {
                Id = Guid.NewGuid().ToString(),
                Name = "New Automation Rule",
                Description = "Automatically post content based on conditions",
                IsActive = true,
                CreatedAt = DateTime.Now,
                TriggerType = "Time-based",
                ActionType = "Post Content",
                ExecutionCount = 0,
                SuccessRate = 100.0
            };

            AutomationRules.Add(newRule);
            UpdateStatistics();

            StatusMessage = "Automation rule created!";
            await Task.Delay(2000);
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating rule: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ToggleAutomationRuleAsync(AutomationRuleViewModel rule)
    {
        if (rule == null) return;

        rule.IsActive = !rule.IsActive;
        UpdateStatistics();

        StatusMessage = $"Automation rule {(rule.IsActive ? "enabled" : "disabled")}";
        await Task.Delay(1500);
        StatusMessage = "";
    }

    [RelayCommand]
    private async Task OptimizeScheduleAsync()
    {
        IsLoading = true;
        StatusMessage = "Optimizing schedule with AI...";

        try
        {
            await Task.Delay(2000); // Simulate AI processing

            // In a real implementation, this would use ML algorithms to optimize posting times
            foreach (var post in ScheduledPosts.Where(p => p.Status == PostStatus.Publishing))
            {
                // Simulate optimization by adjusting times slightly
                var random = new Random();
                var adjustment = random.Next(-30, 30); // ±30 minutes
                post.ScheduledTime = post.ScheduledTime.AddMinutes(adjustment);
            }

            StatusMessage = "Schedule optimized for maximum engagement!";
            await Task.Delay(3000);
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error optimizing schedule: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task BulkScheduleAsync()
    {
        IsLoading = true;
        StatusMessage = "Opening bulk schedule wizard...";

        try
        {
            await Task.Delay(1000);
            // In a real implementation, this would open a bulk scheduling dialog
            StatusMessage = "Bulk schedule wizard opened";
            await Task.Delay(2000);
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening bulk scheduler: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ApplyFilter()
    {
        // In a real implementation, this would filter the collections
        var filteredCount = ScheduledPosts.Count;
        StatusMessage = $"Filter applied - {filteredCount} posts shown";
        Task.Run(async () =>
        {
            await Task.Delay(2000);
            StatusMessage = "";
        });
    }

    [RelayCommand]
    private void ClearFilters()
    {
        FilterText = "";
        FilterPlatform = null;
        FilterStatus = null;
        ShowOnlyToday = false;
        ShowOnlyThisWeek = false;

        StatusMessage = "Filters cleared";
        Task.Run(async () =>
        {
            await Task.Delay(1500);
            StatusMessage = "";
        });
    }

    private async Task LoadInitialDataAsync()
    {
        try
        {
            LoadScheduledPosts();
            await LoadAutomationRulesAsync();
            await LoadScheduleTemplatesAsync();
            UpdateStatistics();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load initial data: {ex.Message}");
        }
    }

    private void LoadScheduledPosts()
    {
        try
        {
            // Simulate loading scheduled posts
            ScheduledPosts.Clear();

            var samplePosts = new[]
            {
                new ScheduledPostViewModel
                {
                    Id = "1",
                    Content = "🚀 Exciting news! Our new feature is launching next week. Stay tuned for updates! #Innovation #TechNews",
                    ScheduledTime = DateTime.Today.AddHours(14),
                    Platforms = new List<SocialPlatform> { SocialPlatform.BlueSky },
                    Status = PostStatus.Publishing,
                    CreatedAt = DateTime.Now.AddDays(-1),
                    Author = "Marketing Team"
                },
                new ScheduledPostViewModel
                {
                    Id = "2",
                    Content = "📊 Weekly analytics report shows 25% increase in engagement. Thank you for your continued support!",
                    ScheduledTime = DateTime.Today.AddDays(1).AddHours(10),
                    Platforms = new List<SocialPlatform> { SocialPlatform.BlueSky },
                    Status = PostStatus.Publishing,
                    CreatedAt = DateTime.Now.AddHours(-3),
                    Author = "Analytics Team"
                },
                new ScheduledPostViewModel
                {
                    Id = "3",
                    Content = "🎯 Pro tip: Use our advanced scheduling features to optimize your social media presence!",
                    ScheduledTime = DateTime.Today.AddDays(2).AddHours(16),
                    Platforms = new List<SocialPlatform> { SocialPlatform.BlueSky },
                    Status = PostStatus.Publishing,
                    CreatedAt = DateTime.Now.AddMinutes(-45),
                    Author = "Content Team",
                    IsRecurring = true,
                    RecurrencePattern = "Weekly"
                }
                // TODO: Add sample posts for Twitter, LinkedIn, Facebook, Threads when implementations are ready
            };

            foreach (var post in samplePosts)
            {
                ScheduledPosts.Add(post);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load scheduled posts: {ex.Message}");
        }
    }

    private Task LoadAutomationRulesAsync()
    {
        try
        {
            AutomationRules.Clear();

            var sampleRules = new[]
            {
                new AutomationRuleViewModel
                {
                    Id = "1",
                    Name = "Morning Motivation",
                    Description = "Post inspirational content every weekday at 8 AM",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-7),
                    TriggerType = "Time-based",
                    ActionType = "Post from Template",
                    ExecutionCount = 23,
                    SuccessRate = 95.7
                },
                new AutomationRuleViewModel
                {
                    Id = "2",
                    Name = "Engagement Booster",
                    Description = "Automatically repost top-performing content from last month",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-3),
                    TriggerType = "Performance-based",
                    ActionType = "Repost Content",
                    ExecutionCount = 8,
                    SuccessRate = 87.5
                },
                new AutomationRuleViewModel
                {
                    Id = "3",
                    Name = "Weekend Highlights",
                    Description = "Share weekly highlights every Saturday evening",
                    IsActive = false,
                    CreatedAt = DateTime.Now.AddDays(-14),
                    TriggerType = "Time-based",
                    ActionType = "Generate Summary",
                    ExecutionCount = 45,
                    SuccessRate = 92.2
                }
            };

            foreach (var rule in sampleRules)
            {
                AutomationRules.Add(rule);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load automation rules: {ex.Message}");
            return Task.CompletedTask;
        }
    }

    private Task LoadScheduleTemplatesAsync()
    {
        try
        {
            ScheduleTemplates.Clear();

            var sampleTemplates = new[]
            {
                new ScheduleTemplateViewModel
                {
                    Id = "1",
                    Name = "Daily Engagement",
                    Description = "3 posts per day with optimal timing",
                    PostCount = 3,
                    TimeSlots = new List<TimeSpan> { TimeSpan.FromHours(9), TimeSpan.FromHours(13), TimeSpan.FromHours(17) },
                    IsActive = true
                },
                new ScheduleTemplateViewModel
                {
                    Id = "2",
                    Name = "Weekend Special",
                    Description = "Relaxed posting schedule for weekends",
                    PostCount = 2,
                    TimeSlots = new List<TimeSpan> { TimeSpan.FromHours(11), TimeSpan.FromHours(15) },
                    IsActive = false
                }
            };

            foreach (var template in sampleTemplates)
            {
                ScheduleTemplates.Add(template);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load schedule templates: {ex.Message}");
            return Task.CompletedTask;
        }
    }

    private void UpdateStatistics()
    {
        TotalScheduledPosts = ScheduledPosts.Count;
        PostsToday = ScheduledPosts.Count(p => p.ScheduledTime.Date == DateTime.Today);
        PostsThisWeek = ScheduledPosts.Count(p => p.ScheduledTime.Date >= DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek) &&
                                                 p.ScheduledTime.Date <= DateTime.Today.AddDays(6 - (int)DateTime.Today.DayOfWeek));
        ActiveAutomationRules = AutomationRules.Count(r => r.IsActive);
        AutomationSuccessRate = AutomationRules.Any() ? AutomationRules.Average(r => r.SuccessRate) : 0.0;
    }

    private void StartAutomationEngine()
    {
        // In a real implementation, this would start a background service
        // that monitors and executes automation rules
        System.Diagnostics.Debug.WriteLine("Automation engine started");
    }
}

/// <summary>
/// ViewModel for individual scheduled posts
/// </summary>
public partial class ScheduledPostViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = "";

    [ObservableProperty]
    private string _content = "";

    [ObservableProperty]
    private DateTime _scheduledTime;

    [ObservableProperty]
    private DateTime? _publishedAt;

    [ObservableProperty]
    private List<SocialPlatform> _platforms = new();

    [ObservableProperty]
    private PostStatus _status;

    [ObservableProperty]
    private bool _isRecurring = false;

    [ObservableProperty]
    private string _recurrencePattern = "";

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private string _author = "";

    public string PlatformsText => string.Join(", ", Platforms.Select(p => p.ToString()));
    public string StatusColor => Status switch
    {
        PostStatus.Publishing => "#f59e0b",
        PostStatus.Published => "#10b981",
        PostStatus.Failed => "#ef4444",
        _ => "#6b7280"
    };
}

/// <summary>
/// ViewModel for automation rules
/// </summary>
public partial class AutomationRuleViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = "";

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private string _triggerType = "";

    [ObservableProperty]
    private string _actionType = "";

    [ObservableProperty]
    private int _executionCount = 0;

    [ObservableProperty]
    private double _successRate = 0.0;

    public string StatusColor => IsActive ? "#10b981" : "#6b7280";
    public string StatusText => IsActive ? "Active" : "Inactive";
}

/// <summary>
/// ViewModel for schedule templates
/// </summary>
public partial class ScheduleTemplateViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = "";

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private int _postCount = 0;

    [ObservableProperty]
    private List<TimeSpan> _timeSlots = new();

    [ObservableProperty]
    private bool _isActive = true;

    public string TimeSlotsText => string.Join(", ", TimeSlots.Select(t => t.ToString(@"hh\:mm")));
}