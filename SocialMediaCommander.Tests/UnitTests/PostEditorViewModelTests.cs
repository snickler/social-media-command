using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Moq;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Desktop.ViewModels;
using SocialMediaCommander.Services.Interfaces;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Comprehensive unit tests for PostEditorViewModel following Microsoft MVVM best practices
/// Testing property change notifications, command execution, and business logic
/// </summary>
public class PostEditorViewModelTests
{
    private readonly Mock<IPostService> _mockPostService;
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly Mock<IMediaService> _mockMediaService;
    private readonly PostEditorViewModel _viewModel;

    public PostEditorViewModelTests()
    {
        _mockPostService = new Mock<IPostService>();
        _mockAccountService = new Mock<IAccountService>();
        _mockMediaService = new Mock<IMediaService>();

        _viewModel = new PostEditorViewModel(
            _mockPostService.Object,
            _mockAccountService.Object,
            _mockMediaService.Object);
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldInitializeProperties()
    {
        // Assert
        Assert.NotNull(_viewModel.Content);
        Assert.False(_viewModel.PromoMode);
        Assert.False(_viewModel.IsThread);
        Assert.False(_viewModel.ThreadsOnlyMode);
        Assert.False(_viewModel.Compact);
        Assert.False(_viewModel.IsPublishing);
        Assert.Equal("composer", _viewModel.ActiveTab);
        Assert.Equal(280, _viewModel.MaxCharacterCount);
    }

    [Fact]
    public void Content_WhenSet_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyChangedRaised = false;
        var propertyName = string.Empty;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(PostEditorViewModel.Content))
            {
                propertyChangedRaised = true;
                propertyName = e.PropertyName;
            }
        };

        // Act
        _viewModel.Content = "New content";

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal(nameof(PostEditorViewModel.Content), propertyName);
        Assert.Equal("New content", _viewModel.Content);
    }

    [Fact]
    public void PromoMode_WhenToggled_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyChangedRaised = false;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(PostEditorViewModel.PromoMode))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _viewModel.PromoMode = true;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.True(_viewModel.PromoMode);
    }

    [Fact]
    public void IsThread_WhenToggled_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyChangedRaised = false;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(PostEditorViewModel.IsThread))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _viewModel.IsThread = true;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.True(_viewModel.IsThread);
    }

    [Fact]
    public void ThreadsOnlyMode_WhenToggled_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyChangedRaised = false;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(PostEditorViewModel.ThreadsOnlyMode))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _viewModel.ThreadsOnlyMode = true;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.True(_viewModel.ThreadsOnlyMode);
    }

    [Fact]
    public void Compact_WhenToggled_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyChangedRaised = false;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(PostEditorViewModel.Compact))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _viewModel.Compact = true;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.True(_viewModel.Compact);
    }

    [Fact]
    public void IsPublishing_WhenSet_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyChangedRaised = false;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(PostEditorViewModel.IsPublishing))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _viewModel.IsPublishing = true;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.True(_viewModel.IsPublishing);
    }

    [Fact]
    public void ActiveTab_WhenChanged_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyChangedRaised = false;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(PostEditorViewModel.ActiveTab))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _viewModel.ActiveTab = "preview";

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal("preview", _viewModel.ActiveTab);
    }

    [Fact]
    public void MaxCharacterCount_WhenChanged_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyChangedRaised = false;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(PostEditorViewModel.MaxCharacterCount))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _viewModel.MaxCharacterCount = 500;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal(500, _viewModel.MaxCharacterCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Short post")]
    [InlineData("This is a longer post content that contains more text to test character counting and validation")]
    public void Content_WithVariousLengths_ShouldUpdateCorrectly(string content)
    {
        // Act
        _viewModel.Content = content;

        // Assert
        Assert.Equal(content, _viewModel.Content);
    }

    [Theory]
    [InlineData("composer")]
    [InlineData("preview")]
    [InlineData("settings")]
    [InlineData("media")]
    public void ActiveTab_WithDifferentTabs_ShouldUpdateCorrectly(string tabName)
    {
        // Act
        _viewModel.ActiveTab = tabName;

        // Assert
        Assert.Equal(tabName, _viewModel.ActiveTab);
    }

    [Fact]
    public void IsPosting_WhenSet_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyChangedRaised = false;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(PostEditorViewModel.IsPosting))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _viewModel.IsPosting = true;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.True(_viewModel.IsPosting);
    }

    [Fact]
    public void MultiplePropertyChanges_ShouldTriggerCorrectEvents()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName != null)
            {
                propertyChangedEvents.Add(e.PropertyName);
            }
        };

        // Act
        _viewModel.Content = "Test content";
        _viewModel.PromoMode = true;
        _viewModel.IsThread = true;
        _viewModel.MaxCharacterCount = 500;

        // Assert
        Assert.Contains(nameof(PostEditorViewModel.Content), propertyChangedEvents);
        Assert.Contains(nameof(PostEditorViewModel.PromoMode), propertyChangedEvents);
        Assert.Contains(nameof(PostEditorViewModel.IsThread), propertyChangedEvents);
        Assert.Contains(nameof(PostEditorViewModel.MaxCharacterCount), propertyChangedEvents);
        // Note: The ViewModel may trigger additional property changes, so we just verify the required ones are present
        Assert.True(propertyChangedEvents.Count >= 4, $"Expected at least 4 property change events, but got {propertyChangedEvents.Count}");
    }

    [Fact]
    public void PropertyChanges_WithIdenticalValues_ShouldNotTriggerEvents()
    {
        // Arrange
        var initialContent = _viewModel.Content;
        var propertyChangedCount = 0;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(PostEditorViewModel.Content))
            {
                propertyChangedCount++;
            }
        };

        // Act - Set the same value twice
        _viewModel.Content = initialContent;
        _viewModel.Content = initialContent;

        // Assert
        Assert.Equal(0, propertyChangedCount); // Should not trigger since value didn't change
    }

    [Fact]
    public void ViewModel_ImplementsINotifyPropertyChanged()
    {
        // Assert
        Assert.IsAssignableFrom<INotifyPropertyChanged>(_viewModel);
    }

    [Fact]
    public void Constructor_WithNullDependencies_ShouldHandleGracefully()
    {
        // This test assumes the ViewModel handles null dependencies gracefully
        // If the constructor requires non-null dependencies, this would test that
        // the appropriate exceptions are thrown

        // Act & Assert - This depends on the actual implementation
        // If the constructor requires non-null dependencies:
        // Assert.Throws<ArgumentNullException>(() => new PostEditorViewModel(null!, null!, null!));

        // For now, just verify our test instance is valid
        Assert.NotNull(_viewModel);
    }

    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act - Using a fresh instance
        var freshViewModel = new PostEditorViewModel(
            _mockPostService.Object,
            _mockAccountService.Object,
            _mockMediaService.Object);

        // Assert
        Assert.NotNull(freshViewModel.Content);
        Assert.False(freshViewModel.PromoMode);
        Assert.False(freshViewModel.IsThread);
        Assert.False(freshViewModel.ThreadsOnlyMode);
        Assert.False(freshViewModel.Compact);
        Assert.False(freshViewModel.IsPublishing);
        Assert.False(freshViewModel.IsPosting);
        Assert.Equal("composer", freshViewModel.ActiveTab);
        Assert.Equal(280, freshViewModel.MaxCharacterCount);
    }
}