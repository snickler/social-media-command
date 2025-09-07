using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using SocialMediaCommander.Desktop.ViewModels;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Tests;

/// <summary>
/// Comprehensive tests for SocialFeedViewModel with MVVM pattern compliance and modern C# features
/// </summary>
public class SocialFeedViewModelTests
{
    private readonly Mock<IFeedService> _mockFeedService;
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly SocialFeedViewModel _viewModel;

    public SocialFeedViewModelTests()
    {
        _mockFeedService = new Mock<IFeedService>();
        _mockAccountService = new Mock<IAccountService>();

        // Setup mock services with proper interface methods
        _mockFeedService.Setup(x => x.GetFeedItemsAsync(It.IsAny<IEnumerable<SocialPlatform>>(), It.IsAny<int>()))
            .ReturnsAsync(new List<SocialFeedItem>()); // Mock feed data

        _viewModel = new SocialFeedViewModel(_mockFeedService.Object, _mockAccountService.Object);
    }

    #region Constructor and Initialization Tests

    [Fact]
    public void Constructor_WithValidServices_ShouldInitializeCorrectly()
    {
        // Assert
        Assert.NotNull(_viewModel.FeedPosts);
        Assert.NotNull(_viewModel.PlatformFilters);
        Assert.False(_viewModel.IsLoading);
        Assert.False(_viewModel.IsRefreshing);
        Assert.Equal("all", _viewModel.SelectedPlatformFilter);
        Assert.False(_viewModel.HasError);
        Assert.Empty(_viewModel.ErrorMessage);
    }

    [Fact]
    public void Constructor_WithNullFeedService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SocialFeedViewModel(null!, _mockAccountService.Object));
    }

    [Fact]
    public void Constructor_WithNullAccountService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SocialFeedViewModel(_mockFeedService.Object, null!));
    }

    [Fact]
    public void Constructor_ShouldInitializePlatformFilters()
    {
        // Assert
        Assert.Equal(6, _viewModel.PlatformFilters.Count); // All + 5 platforms

        var allFilter = _viewModel.PlatformFilters.First();
        Assert.Equal("all", allFilter.Id);
        Assert.Equal("All", allFilter.Name);
        Assert.True(allFilter.IsSelected);

        var platformFilters = _viewModel.PlatformFilters.Skip(1).ToList();
        Assert.Contains(platformFilters, f => f.Id == "bluesky" && f.Name == "BlueSky");
        Assert.Contains(platformFilters, f => f.Id == "x" && f.Name == "X");
        Assert.Contains(platformFilters, f => f.Id == "linkedin" && f.Name == "LinkedIn");
        Assert.Contains(platformFilters, f => f.Id == "threads" && f.Name == "Threads");
        Assert.Contains(platformFilters, f => f.Id == "facebook" && f.Name == "Facebook");
    }

    #endregion

    #region Property Tests

    [Fact]
    public void HasFeedItems_WithEmptyFeed_ShouldReturnFalse()
    {
        // Arrange
        _viewModel.FeedPosts.Clear();

        // Assert
        Assert.False(_viewModel.HasFeedItems);
    }

    [Fact]
    public void HasFeedItems_WithFeedItems_ShouldReturnTrue()
    {
        // Arrange
        _viewModel.FeedPosts.Add(new SocialFeedPostViewModel { Id = "test-post" });

        // Assert
        Assert.True(_viewModel.HasFeedItems);
    }

    [Fact]
    public void TotalFeedItemCount_ShouldReturnCorrectCount()
    {
        // Arrange
        _viewModel.FeedPosts.Clear();
        _viewModel.FeedPosts.Add(new SocialFeedPostViewModel { Id = "post1" });
        _viewModel.FeedPosts.Add(new SocialFeedPostViewModel { Id = "post2" });
        _viewModel.FeedPosts.Add(new SocialFeedPostViewModel { Id = "post3" });

        // Assert
        Assert.Equal(3, _viewModel.TotalFeedItemCount);
    }

    [Theory]
    [InlineData(true, false, false, false, "Loading posts...")]
    [InlineData(false, true, false, false, "Refreshing feed...")]
    [InlineData(false, false, true, false, "Error: Test error")]
    [InlineData(false, false, false, true, "No posts available")]
    [InlineData(false, false, false, false, "Showing 0 posts")]
    public void StatusText_ShouldReturnCorrectStatus(bool isLoading, bool isRefreshing, bool hasError, bool isEmpty, string expectedStatus)
    {
        // Arrange
        _viewModel.IsLoading = isLoading;
        _viewModel.IsRefreshing = isRefreshing;
        _viewModel.HasError = hasError;
        _viewModel.ErrorMessage = hasError ? "Test error" : "";

        if (!isEmpty)
        {
            _viewModel.FeedPosts.Add(new SocialFeedPostViewModel { Id = "test" });
        }
        else
        {
            _viewModel.FeedPosts.Clear();
        }

        // Assert
        Assert.Contains(expectedStatus, _viewModel.StatusText);
    }

    #endregion

    #region Command Tests

    [Fact]
    public async Task RefreshFeedCommand_ShouldUpdateLastRefreshTime()
    {
        // Arrange
        var initialTime = _viewModel.LastRefreshTime;
        await Task.Delay(10); // Ensure time difference

        // Act
        await _viewModel.RefreshFeedCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_viewModel.LastRefreshTime > initialTime);
    }

    [Fact]
    public async Task RefreshFeedCommand_ShouldSetRefreshingStates()
    {
        // Arrange
        var refreshingStates = new List<bool>();
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.IsRefreshing))
                refreshingStates.Add(_viewModel.IsRefreshing);
        };

        // Act
        await _viewModel.RefreshFeedCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(true, refreshingStates); // Should have been refreshing
        Assert.False(_viewModel.IsRefreshing); // Should not be refreshing at the end
    }

    [Fact]
    public async Task RefreshFeedCommand_WhenAlreadyRefreshing_ShouldNotExecuteAgain()
    {
        // Arrange
        _viewModel.IsRefreshing = true;
        var initialTime = _viewModel.LastRefreshTime;

        // Act
        await _viewModel.RefreshFeedCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(initialTime, _viewModel.LastRefreshTime);
    }

    [Fact]
    public void FilterByPlatformCommand_ShouldUpdateSelectedFilter()
    {
        // Act
        _viewModel.FilterByPlatformCommand.Execute("bluesky");

        // Assert
        Assert.Equal("bluesky", _viewModel.SelectedPlatformFilter);

        // Check that only the selected filter is marked as selected
        var blueSkyFilter = _viewModel.PlatformFilters.First(f => f.Id == "bluesky");
        Assert.True(blueSkyFilter.IsSelected);

        var otherFilters = _viewModel.PlatformFilters.Where(f => f.Id != "bluesky");
        Assert.All(otherFilters, filter => Assert.False(filter.IsSelected));
    }

    [Fact]
    public async Task LikePostCommand_ShouldToggleLikeStatus()
    {
        // Arrange
        var post = new SocialFeedPostViewModel
        {
            Id = "test-post",
            IsLiked = false,
            LikesCount = 5
        };

        // Act - Like the post
        await _viewModel.LikePostCommand.ExecuteAsync(post);

        // Assert
        Assert.True(post.IsLiked);
        Assert.Equal(6, post.LikesCount);

        // Act - Unlike the post
        await _viewModel.LikePostCommand.ExecuteAsync(post);

        // Assert
        Assert.False(post.IsLiked);
        Assert.Equal(5, post.LikesCount);
    }

    [Fact]
    public async Task LikePostCommand_WithNullPost_ShouldNotThrow()
    {
        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await _viewModel.LikePostCommand.ExecuteAsync(null));
        Assert.Null(exception);
    }

    [Fact]
    public async Task RetweetPostCommand_ShouldToggleRetweetStatus()
    {
        // Arrange
        var post = new SocialFeedPostViewModel
        {
            Id = "test-post",
            IsRetweeted = false,
            RetweetsCount = 3
        };

        // Act - Retweet the post
        await _viewModel.RetweetPostCommand.ExecuteAsync(post);

        // Assert
        Assert.True(post.IsRetweeted);
        Assert.Equal(4, post.RetweetsCount);

        // Act - Unretweet the post
        await _viewModel.RetweetPostCommand.ExecuteAsync(post);

        // Assert
        Assert.False(post.IsRetweeted);
        Assert.Equal(3, post.RetweetsCount);
    }

    [Fact]
    public async Task RetweetPostCommand_WithNullPost_ShouldNotThrow()
    {
        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await _viewModel.RetweetPostCommand.ExecuteAsync(null));
        Assert.Null(exception);
    }

    [Fact]
    public void ViewPostDetailsCommand_ShouldRaiseEvent()
    {
        // Arrange
        var post = new SocialFeedPostViewModel { Id = "test-post" };
        SocialFeedPostViewModel? eventPost = null;
        _viewModel.OnPostDetailsRequested += (p) => eventPost = p;

        // Act
        _viewModel.ViewPostDetailsCommand.Execute(post);

        // Assert
        Assert.Equal(post, eventPost);
    }

    #endregion

    #region Property Change Notification Tests

    [Fact]
    public void Properties_ShouldRaisePropertyChangedEvents()
    {
        // Arrange
        var changedProperties = new List<string>();
        _viewModel.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName ?? "");

        // Act
        _viewModel.IsLoading = true;
        _viewModel.IsRefreshing = true;
        _viewModel.SelectedPlatformFilter = "x";
        _viewModel.ErrorMessage = "Test error";
        _viewModel.HasError = true;

        // Assert
        Assert.Contains(nameof(_viewModel.IsLoading), changedProperties);
        Assert.Contains(nameof(_viewModel.IsRefreshing), changedProperties);
        Assert.Contains(nameof(_viewModel.SelectedPlatformFilter), changedProperties);
        Assert.Contains(nameof(_viewModel.ErrorMessage), changedProperties);
        Assert.Contains(nameof(_viewModel.HasError), changedProperties);
    }

    [Fact]
    public void SelectedPlatformFilter_Changed_ShouldApplyFilter()
    {
        // Arrange
        var initialFilterState = _viewModel.PlatformFilters.ToDictionary(f => f.Id, f => f.IsSelected);

        // Act
        _viewModel.SelectedPlatformFilter = "linkedin";

        // Assert
        var linkedInFilter = _viewModel.PlatformFilters.First(f => f.Id == "linkedin");
        Assert.True(linkedInFilter.IsSelected);

        // Verify other filters are unselected
        var otherFilters = _viewModel.PlatformFilters.Where(f => f.Id != "linkedin");
        Assert.All(otherFilters, filter => Assert.False(filter.IsSelected));
    }

    #endregion

    #region SocialFeedPostViewModel Tests

    [Fact]
    public void SocialFeedPostViewModel_Properties_ShouldRaisePropertyChanged()
    {
        // Arrange
        var post = new SocialFeedPostViewModel();
        var changedProperties = new List<string>();
        post.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName ?? "");

        // Act
        post.UserName = "Test User";
        post.Content = "Test content";
        post.LikesCount = 10;
        post.IsLiked = true;

        // Assert
        Assert.Contains(nameof(post.UserName), changedProperties);
        Assert.Contains(nameof(post.Content), changedProperties);
        Assert.Contains(nameof(post.LikesCount), changedProperties);
        Assert.Contains(nameof(post.IsLiked), changedProperties);
    }

    [Theory]
    [InlineData("bluesky", "#0085FF", "BlueSky")]
    [InlineData("x", "#000000", "X")]
    [InlineData("linkedin", "#0A66C2", "LinkedIn")]
    [InlineData("threads", "#000000", "Threads")]
    [InlineData("facebook", "#1877F2", "Facebook")]
    [InlineData("unknown", "#6B7280", "Unknown")]
    public void SocialFeedPostViewModel_PlatformProperties_ShouldReturnCorrectValues(string platform, string expectedColor, string expectedDisplayName)
    {
        // Arrange
        var post = new SocialFeedPostViewModel { Platform = platform };

        // Assert
        Assert.Equal(expectedColor, post.PlatformColor);
        Assert.Equal(expectedDisplayName, post.PlatformDisplayName);
    }

    #endregion

    #region PlatformFilterViewModel Tests

    [Fact]
    public void PlatformFilterViewModel_Properties_ShouldRaisePropertyChanged()
    {
        // Arrange
        var filter = new PlatformFilterViewModel();
        var changedProperties = new List<string>();
        filter.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName ?? "");

        // Act
        filter.Id = "test-id";
        filter.Name = "Test Platform";
        filter.Color = "#FF0000";
        filter.IsSelected = true;

        // Assert
        Assert.Contains(nameof(filter.Id), changedProperties);
        Assert.Contains(nameof(filter.Name), changedProperties);
        Assert.Contains(nameof(filter.Color), changedProperties);
        Assert.Contains(nameof(filter.IsSelected), changedProperties);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task RefreshFeedCommand_WithServiceError_ShouldSetErrorState()
    {
        // Arrange
        var mockService = new Mock<IFeedService>();
        var mockAccountService = new Mock<IAccountService>();
        mockService.Setup(x => x.GetFeedItemsAsync(It.IsAny<IEnumerable<SocialPlatform>>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Service error"));

        var viewModel = new SocialFeedViewModel(mockService.Object, mockAccountService.Object);

        // Act
        await viewModel.RefreshFeedCommand.ExecuteAsync(null);

        // Assert
        Assert.True(viewModel.HasError);
        Assert.Contains("Service error", viewModel.ErrorMessage);
        Assert.False(viewModel.IsRefreshing);
    }

    #endregion

    #region Performance and Concurrency Tests

    [Fact]
    public async Task ConcurrentCommands_ShouldHandleGracefully()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act - Execute multiple commands concurrently
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_viewModel.RefreshFeedCommand.ExecuteAsync(null));
            tasks.Add(Task.Run(() => _viewModel.FilterByPlatformCommand.Execute("bluesky")));
        }

        // Assert
        var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(tasks));
        Assert.Null(exception);
    }

    [Fact]
    public void MultiplePostOperations_ShouldHandleEfficiently()
    {
        // Arrange
        var posts = new List<SocialFeedPostViewModel>();
        for (int i = 0; i < 100; i++)
        {
            posts.Add(new SocialFeedPostViewModel
            {
                Id = $"post-{i}",
                UserName = $"User {i}",
                Content = $"Content {i}",
                Platform = i % 2 == 0 ? "bluesky" : "x"
            });
        }

        // Act - Add posts to collection
        _viewModel.FeedPosts.Clear();
        foreach (var post in posts)
        {
            _viewModel.FeedPosts.Add(post);
        }

        // Assert
        Assert.Equal(100, _viewModel.TotalFeedItemCount);
        Assert.True(_viewModel.HasFeedItems);
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Fact]
    public void PlatformFilter_WithEmptyId_ShouldHandleGracefully()
    {
        // Act & Assert
        var exception = Record.Exception(() => _viewModel.FilterByPlatformCommand.Execute(""));
        Assert.Null(exception);
        Assert.Equal("", _viewModel.SelectedPlatformFilter);
    }

    [Fact]
    public async Task PostOperations_WithNegativeCounts_ShouldHandleCorrectly()
    {
        // Arrange
        var post = new SocialFeedPostViewModel
        {
            LikesCount = 0,
            RetweetsCount = 0,
            IsLiked = true,
            IsRetweeted = true
        };

        // Act
        await _viewModel.LikePostCommand.ExecuteAsync(post);
        await _viewModel.RetweetPostCommand.ExecuteAsync(post);

        // Assert
        Assert.Equal(-1, post.LikesCount); // Should allow negative values
        Assert.Equal(-1, post.RetweetsCount);
        Assert.False(post.IsLiked);
        Assert.False(post.IsRetweeted);
    }

    [Fact]
    public async Task LoadFeed_ShouldGenerateReasonableTestData()
    {
        // Act - The constructor automatically loads feed data
        await Task.Delay(1100); // Wait for mock feed generation

        // Assert
        Assert.True(_viewModel.FeedPosts.Count > 0);
        Assert.True(_viewModel.FeedPosts.Count <= 15); // Should generate up to 15 posts

        // Verify posts have reasonable data
        var firstPost = _viewModel.FeedPosts.FirstOrDefault();
        if (firstPost != null)
        {
            Assert.NotEmpty(firstPost.UserName);
            Assert.NotEmpty(firstPost.Content);
            Assert.NotEmpty(firstPost.Platform);
            Assert.True(firstPost.LikesCount >= 0);
            Assert.True(firstPost.RetweetsCount >= 0);
        }
    }

    #endregion
}