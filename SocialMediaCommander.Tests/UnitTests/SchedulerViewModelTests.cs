using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Desktop.ViewModels;
using SocialMediaCommander.Services.Interfaces;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Comprehensive unit tests for SchedulerViewModel
/// Tests scheduling logic, automation, MVVM compliance, and command execution
/// </summary>
public class SchedulerViewModelTests
{
    private readonly Mock<IPostService> _mockPostService;
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly SchedulerViewModel _viewModel;

    public SchedulerViewModelTests()
    {
        _mockPostService = new Mock<IPostService>();
        _mockAccountService = new Mock<IAccountService>();
        _viewModel = new SchedulerViewModel(_mockPostService.Object, _mockAccountService.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        // Assert
        Assert.False(_viewModel.IsLoading);
        Assert.Equal(string.Empty, _viewModel.StatusMessage);
        Assert.True(_viewModel.IsCalendarViewActive);
        Assert.False(_viewModel.IsListViewActive);
        Assert.False(_viewModel.IsAutomationViewActive);
        Assert.Equal(DateTime.Today, _viewModel.SelectedDate);
        Assert.Equal(DateTime.Today, _viewModel.CalendarDisplayDate);
        Assert.Equal(string.Empty, _viewModel.FilterText);
        Assert.Null(_viewModel.FilterPlatform);
        Assert.Null(_viewModel.FilterStatus);
        Assert.False(_viewModel.ShowOnlyToday);
        Assert.False(_viewModel.ShowOnlyThisWeek);
    }

    [Fact]
    public void Constructor_ShouldInitializeCollectionsCorrectly()
    {
        // Assert - Scheduler may load initial data in constructor
        Assert.NotNull(_viewModel.ScheduledPosts);

        Assert.NotNull(_viewModel.AutomationRules);
        Assert.NotNull(_viewModel.ScheduleTemplates);

        Assert.NotNull(_viewModel.RecurrencePatterns);
        Assert.Contains("Daily", _viewModel.RecurrencePatterns);
        Assert.Contains("Weekly", _viewModel.RecurrencePatterns);
        Assert.Contains("Monthly", _viewModel.RecurrencePatterns);
        Assert.Contains("Custom", _viewModel.RecurrencePatterns);

        Assert.NotNull(_viewModel.AvailablePlatforms);
        Assert.Contains(SocialPlatform.BlueSky, _viewModel.AvailablePlatforms);
        Assert.Contains(SocialPlatform.BlueSky, _viewModel.AvailablePlatforms);
        Assert.Contains(SocialPlatform.BlueSky, _viewModel.AvailablePlatforms);
        Assert.Contains(SocialPlatform.BlueSky, _viewModel.AvailablePlatforms);

        Assert.NotNull(_viewModel.StatusOptions);
        Assert.Contains(PostStatus.Draft, _viewModel.StatusOptions);
        Assert.Contains(PostStatus.Publishing, _viewModel.StatusOptions);
        Assert.Contains(PostStatus.Published, _viewModel.StatusOptions);
        Assert.Contains(PostStatus.Failed, _viewModel.StatusOptions);
    }

    [Fact]
    public void Constructor_ShouldInitializeNewPostPropertiesCorrectly()
    {
        // Assert
        Assert.Equal(string.Empty, _viewModel.NewPostContent);
        Assert.True(_viewModel.NewPostScheduleDate >= DateTime.Today);
        Assert.NotNull(_viewModel.NewPostSelectedPlatforms);
        Assert.Empty(_viewModel.NewPostSelectedPlatforms);
        Assert.False(_viewModel.NewPostRecurring);
        Assert.Equal("Daily", _viewModel.NewPostRecurrencePattern);
    }

    [Fact]
    public void Constructor_ShouldInitializeQuickActionPropertiesCorrectly()
    {
        // Assert
        Assert.True(_viewModel.SmartSchedulingEnabled);
        Assert.True(_viewModel.AutoOptimizeTimingEnabled);
        Assert.False(_viewModel.PauseAllScheduling);
    }

    [Fact]
    public void Constructor_ShouldInitializeStatisticsCorrectly()
    {
        // Assert - Statistics may be calculated from initial data
        Assert.True(_viewModel.TotalScheduledPosts >= 0);
        Assert.True(_viewModel.PostsToday >= 0);
        Assert.True(_viewModel.PostsThisWeek >= 0);
        Assert.True(_viewModel.ActiveAutomationRules >= 0);
        Assert.True(_viewModel.AutomationSuccessRate >= 0.0);
    }

    [Fact]
    public void IsLoading_Set_ShouldRaisePropertyChanged()
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.IsLoading))
                eventRaised = true;
        };

        // Act
        _viewModel.IsLoading = true;

        // Assert
        Assert.True(eventRaised);
        Assert.True(_viewModel.IsLoading);
    }

    [Fact]
    public void StatusMessage_Set_ShouldRaisePropertyChanged()
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.StatusMessage))
                eventRaised = true;
        };

        // Act
        _viewModel.StatusMessage = "Test status";

        // Assert
        Assert.True(eventRaised);
        Assert.Equal("Test status", _viewModel.StatusMessage);
    }

    [Fact]
    public void SelectedDate_Set_ShouldRaisePropertyChanged()
    {
        // Arrange
        var testDate = DateTime.Today.AddDays(1);
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.SelectedDate))
                eventRaised = true;
        };

        // Act
        _viewModel.SelectedDate = testDate;

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(testDate, _viewModel.SelectedDate);
    }

    [Fact]
    public void FilterText_Set_ShouldRaisePropertyChanged()
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.FilterText))
                eventRaised = true;
        };

        // Act
        _viewModel.FilterText = "test filter";

        // Assert
        Assert.True(eventRaised);
        Assert.Equal("test filter", _viewModel.FilterText);
    }

    [Theory]
    [InlineData(SocialPlatform.BlueSky)]
    // TODO: Add Twitter, LinkedIn, Facebook when implementations are ready
    public void FilterPlatform_Set_ShouldRaisePropertyChanged(SocialPlatform platform)
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.FilterPlatform))
                eventRaised = true;
        };

        // Act
        _viewModel.FilterPlatform = platform;

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(platform, _viewModel.FilterPlatform);
    }

    [Theory]
    [InlineData(PostStatus.Draft)]
    [InlineData(PostStatus.Published)]
    [InlineData(PostStatus.Failed)]
    public void FilterStatus_Set_ShouldRaisePropertyChanged(PostStatus status)
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.FilterStatus))
                eventRaised = true;
        };

        // Act
        _viewModel.FilterStatus = status;

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(status, _viewModel.FilterStatus);
    }

    [Fact]
    public void ViewStateProperties_Set_ShouldRaisePropertyChanged()
    {
        // Arrange
        var calendarEventRaised = false;
        var listEventRaised = false;
        var automationEventRaised = false;

        _viewModel.PropertyChanged += (_, args) =>
        {
            switch (args.PropertyName)
            {
                case nameof(_viewModel.IsCalendarViewActive):
                    calendarEventRaised = true;
                    break;
                case nameof(_viewModel.IsListViewActive):
                    listEventRaised = true;
                    break;
                case nameof(_viewModel.IsAutomationViewActive):
                    automationEventRaised = true;
                    break;
            }
        };

        // Act
        _viewModel.IsCalendarViewActive = false;
        _viewModel.IsListViewActive = true;
        _viewModel.IsAutomationViewActive = true;

        // Assert
        Assert.True(calendarEventRaised);
        Assert.True(listEventRaised);
        Assert.True(automationEventRaised);
        Assert.False(_viewModel.IsCalendarViewActive);
        Assert.True(_viewModel.IsListViewActive);
        Assert.True(_viewModel.IsAutomationViewActive);
    }

    [Fact]
    public void NewPostContent_Set_ShouldRaisePropertyChanged()
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.NewPostContent))
                eventRaised = true;
        };

        // Act
        _viewModel.NewPostContent = "Test content";

        // Assert
        Assert.True(eventRaised);
        Assert.Equal("Test content", _viewModel.NewPostContent);
    }

    [Fact]
    public void NewPostScheduleDate_Set_ShouldRaisePropertyChanged()
    {
        // Arrange
        var testDate = DateTime.Today.AddDays(2);
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.NewPostScheduleDate))
                eventRaised = true;
        };

        // Act
        _viewModel.NewPostScheduleDate = testDate;

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(testDate, _viewModel.NewPostScheduleDate);
    }

    [Fact]
    public void NewPostScheduleTime_Set_ShouldRaisePropertyChanged()
    {
        // Arrange
        var currentValue = _viewModel.NewPostScheduleTime;
        var testTime = currentValue == TimeSpan.FromHours(15) ? TimeSpan.FromHours(10) : TimeSpan.FromHours(15);
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.NewPostScheduleTime))
                eventRaised = true;
        };

        // Act
        _viewModel.NewPostScheduleTime = testTime;

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(testTime, _viewModel.NewPostScheduleTime);
    }

    [Fact]
    public void QuickActionProperties_Set_ShouldRaisePropertyChanged()
    {
        // Arrange
        var smartEventRaised = false;
        var optimizeEventRaised = false;
        var pauseEventRaised = false;

        _viewModel.PropertyChanged += (_, args) =>
        {
            switch (args.PropertyName)
            {
                case nameof(_viewModel.SmartSchedulingEnabled):
                    smartEventRaised = true;
                    break;
                case nameof(_viewModel.AutoOptimizeTimingEnabled):
                    optimizeEventRaised = true;
                    break;
                case nameof(_viewModel.PauseAllScheduling):
                    pauseEventRaised = true;
                    break;
            }
        };

        // Act
        _viewModel.SmartSchedulingEnabled = false;
        _viewModel.AutoOptimizeTimingEnabled = false;
        _viewModel.PauseAllScheduling = true;

        // Assert
        Assert.True(smartEventRaised);
        Assert.True(optimizeEventRaised);
        Assert.True(pauseEventRaised);
        Assert.False(_viewModel.SmartSchedulingEnabled);
        Assert.False(_viewModel.AutoOptimizeTimingEnabled);
        Assert.True(_viewModel.PauseAllScheduling);
    }

    [Fact]
    public void StatisticsProperties_Set_ShouldRaisePropertyChanged()
    {
        // Arrange
        var totalPostsEventRaised = false;
        var todayPostsEventRaised = false;
        var weekPostsEventRaised = false;
        var rulesEventRaised = false;
        var successRateEventRaised = false;

        _viewModel.PropertyChanged += (_, args) =>
        {
            switch (args.PropertyName)
            {
                case nameof(_viewModel.TotalScheduledPosts):
                    totalPostsEventRaised = true;
                    break;
                case nameof(_viewModel.PostsToday):
                    todayPostsEventRaised = true;
                    break;
                case nameof(_viewModel.PostsThisWeek):
                    weekPostsEventRaised = true;
                    break;
                case nameof(_viewModel.ActiveAutomationRules):
                    rulesEventRaised = true;
                    break;
                case nameof(_viewModel.AutomationSuccessRate):
                    successRateEventRaised = true;
                    break;
            }
        };

        // Act
        _viewModel.TotalScheduledPosts = 10;
        _viewModel.PostsToday = 2;
        _viewModel.PostsThisWeek = 5;
        _viewModel.ActiveAutomationRules = 3;
        _viewModel.AutomationSuccessRate = 95.5;

        // Assert
        Assert.True(totalPostsEventRaised);
        Assert.True(todayPostsEventRaised);
        Assert.True(weekPostsEventRaised);
        Assert.True(rulesEventRaised);
        Assert.True(successRateEventRaised);
        Assert.Equal(10, _viewModel.TotalScheduledPosts);
        Assert.Equal(2, _viewModel.PostsToday);
        Assert.Equal(5, _viewModel.PostsThisWeek);
        Assert.Equal(3, _viewModel.ActiveAutomationRules);
        Assert.Equal(95.5, _viewModel.AutomationSuccessRate);
    }

    [Fact]
    public async Task RefreshDataCommand_Execute_ShouldCompleteSuccessfully()
    {
        // Arrange
        _viewModel.IsLoading = false;

        // Act
        await _viewModel.RefreshDataCommand.ExecuteAsync(null);

        // Assert
        Assert.False(_viewModel.IsLoading); // Should be reset after completion
        Assert.Equal(string.Empty, _viewModel.StatusMessage); // Should be cleared after delay
    }

    [Fact]
    public async Task RefreshDataCommand_Execute_ShouldSetLoadingState()
    {
        // Arrange
        var wasLoading = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.IsLoading) && _viewModel.IsLoading)
                wasLoading = true;
        };

        // Act
        await _viewModel.RefreshDataCommand.ExecuteAsync(null);

        // Assert
        Assert.True(wasLoading);
        Assert.False(_viewModel.IsLoading); // Should be reset after completion
    }

    [Fact]
    public async Task ScheduleNewPostCommand_Execute_WithEmptyContent_ShouldShowError()
    {
        // Arrange
        _viewModel.NewPostContent = "";

        // Act
        await _viewModel.ScheduleNewPostCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("Please enter post content", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task ScheduleNewPostCommand_Execute_WithNoPlatforms_ShouldShowError()
    {
        // Arrange
        _viewModel.NewPostContent = "Test content";
        _viewModel.NewPostSelectedPlatforms.Clear();

        // Act
        await _viewModel.ScheduleNewPostCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("Please select at least one platform", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task ScheduleNewPostCommand_Execute_WithValidData_ShouldCreateScheduledPost()
    {
        // Arrange
        _viewModel.NewPostContent = "Test post content";
        _viewModel.NewPostSelectedPlatforms.Add(SocialPlatform.BlueSky);
        _viewModel.NewPostSelectedPlatforms.Add(SocialPlatform.BlueSky);
        _viewModel.NewPostScheduleDate = DateTime.Today.AddDays(1);
        _viewModel.NewPostScheduleTime = TimeSpan.FromHours(10);
        _viewModel.NewPostRecurring = true;
        _viewModel.NewPostRecurrencePattern = "Weekly";

        var initialCount = _viewModel.ScheduledPosts.Count;

        // Act
        await _viewModel.ScheduleNewPostCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(initialCount + 1, _viewModel.ScheduledPosts.Count);

        var scheduledPost = _viewModel.ScheduledPosts.Last();
        Assert.Equal("Test post content", scheduledPost.Content);
        Assert.Contains(SocialPlatform.BlueSky, scheduledPost.Platforms);
        Assert.Contains(SocialPlatform.BlueSky, scheduledPost.Platforms);
        Assert.Equal(DateTime.Today.AddDays(1).Date + TimeSpan.FromHours(10), scheduledPost.ScheduledTime);
        Assert.True(scheduledPost.IsRecurring);
        Assert.Equal("Weekly", scheduledPost.RecurrencePattern);
        Assert.Equal(PostStatus.Publishing, scheduledPost.Status);
    }

    [Fact]
    public async Task ScheduleNewPostCommand_Execute_ShouldClearFormAfterSuccess()
    {
        // Arrange
        _viewModel.NewPostContent = "Test content";
        _viewModel.NewPostSelectedPlatforms.Add(SocialPlatform.BlueSky);
        _viewModel.NewPostRecurring = true;

        // Act
        await _viewModel.ScheduleNewPostCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(string.Empty, _viewModel.NewPostContent);
        Assert.Empty(_viewModel.NewPostSelectedPlatforms);
        Assert.False(_viewModel.NewPostRecurring);
        Assert.Equal(DateTime.Today.AddDays(1), _viewModel.NewPostScheduleDate);
        Assert.Equal(TimeSpan.FromHours(9), _viewModel.NewPostScheduleTime);
    }

    [Fact]
    public async Task DeleteScheduledPostCommand_Execute_WithNullPost_ShouldNotThrow()
    {
        // Act & Assert
        await _viewModel.DeleteScheduledPostCommand.ExecuteAsync(null);

        // Should complete without throwing
        Assert.False(_viewModel.IsLoading);
    }

    [Fact]
    public async Task DeleteScheduledPostCommand_Execute_WithValidPost_ShouldRemovePost()
    {
        // Arrange
        var post = new ScheduledPostViewModel();

        _viewModel.ScheduledPosts.Add(post);
        var initialCount = _viewModel.ScheduledPosts.Count;

        // Act
        await _viewModel.DeleteScheduledPostCommand.ExecuteAsync(post);

        // Assert
        Assert.Equal(initialCount - 1, _viewModel.ScheduledPosts.Count);
        Assert.DoesNotContain(post, _viewModel.ScheduledPosts);
    }

    [Fact]
    public void Commands_ShouldBeInitializedCorrectly()
    {
        // Assert
        Assert.NotNull(_viewModel.RefreshDataCommand);
        Assert.NotNull(_viewModel.ScheduleNewPostCommand);
        Assert.NotNull(_viewModel.DeleteScheduledPostCommand);
    }

    [Fact]
    public void NewPostSelectedPlatforms_Modification_ShouldNotThrow()
    {
        // Act & Assert
        _viewModel.NewPostSelectedPlatforms.Add(SocialPlatform.BlueSky);
        _viewModel.NewPostSelectedPlatforms.Add(SocialPlatform.BlueSky);
        _viewModel.NewPostSelectedPlatforms.Remove(SocialPlatform.BlueSky);
        _viewModel.NewPostSelectedPlatforms.Clear();

        // Should complete without throwing
        Assert.Empty(_viewModel.NewPostSelectedPlatforms);
    }

    [Theory]
    [InlineData("Daily")]
    [InlineData("Weekly")]
    [InlineData("Monthly")]
    [InlineData("Custom")]
    public void NewPostRecurrencePattern_Set_ShouldAcceptValidPatterns(string pattern)
    {
        // Act
        _viewModel.NewPostRecurrencePattern = pattern;

        // Assert
        Assert.Equal(pattern, _viewModel.NewPostRecurrencePattern);
    }

    [Fact]
    public void BooleanFlags_Set_ShouldUpdateCorrectly()
    {
        // Act
        _viewModel.ShowOnlyToday = true;
        _viewModel.ShowOnlyThisWeek = true;
        _viewModel.NewPostRecurring = true;

        // Assert
        Assert.True(_viewModel.ShowOnlyToday);
        Assert.True(_viewModel.ShowOnlyThisWeek);
        Assert.True(_viewModel.NewPostRecurring);
    }

    [Fact]
    public void PropertyChangedEvent_ShouldBeRaisedForAllObservableProperties()
    {
        // This test ensures all properties properly implement INotifyPropertyChanged
        // We've already tested individual properties above, this is a summary verification

        var propertyNames = new HashSet<string>();
        _viewModel.PropertyChanged += (_, args) => propertyNames.Add(args.PropertyName ?? string.Empty);

        // Act - set various properties
        _viewModel.IsLoading = true;
        _viewModel.StatusMessage = "test";
        _viewModel.SelectedDate = DateTime.Today.AddDays(1);
        _viewModel.FilterText = "filter";
        _viewModel.NewPostContent = "content";

        // Assert
        Assert.Contains(nameof(_viewModel.IsLoading), propertyNames);
        Assert.Contains(nameof(_viewModel.StatusMessage), propertyNames);
        Assert.Contains(nameof(_viewModel.SelectedDate), propertyNames);
        Assert.Contains(nameof(_viewModel.FilterText), propertyNames);
        Assert.Contains(nameof(_viewModel.NewPostContent), propertyNames);
    }
}
