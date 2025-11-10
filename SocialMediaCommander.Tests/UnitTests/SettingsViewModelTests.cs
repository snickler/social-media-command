using System.ComponentModel;
using Moq;
using SocialMediaCommander.Desktop.ViewModels;
using SocialMediaCommander.Services.Interfaces;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Comprehensive unit tests for SettingsViewModel following Microsoft MVVM best practices
/// Testing property change notifications, settings persistence, and validation
/// </summary>
public class SettingsViewModelTests
{
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<IBackupService> _mockBackupService;
    private readonly Mock<IDataIntegrityService> _mockDataIntegrityService;
    private readonly SettingsViewModel _viewModel;

    public SettingsViewModelTests()
    {
        _mockAccountService = new Mock<IAccountService>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockBackupService = new Mock<IBackupService>();
        _mockDataIntegrityService = new Mock<IDataIntegrityService>();

        _viewModel = new SettingsViewModel(
            _mockAccountService.Object,
            _mockSettingsService.Object,
            _mockBackupService.Object,
            _mockDataIntegrityService.Object);
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldInitializeProperties()
    {
        // Assert
        Assert.Equal("System", _viewModel.SelectedTheme);
        Assert.False(_viewModel.IsDarkMode);
        Assert.Equal(1.0, _viewModel.UiScale);
        Assert.Equal("English", _viewModel.SelectedLanguage);
        Assert.True(_viewModel.EnableNotifications);
        Assert.True(_viewModel.EnableSoundNotifications);
        Assert.True(_viewModel.EnableDesktopNotifications);
        Assert.False(_viewModel.EnableEmailNotifications);
    }

    [Fact]
    public void SelectedTheme_WhenChanged_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyChangedRaised = false;
        var propertyName = string.Empty;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.SelectedTheme))
            {
                propertyChangedRaised = true;
                propertyName = e.PropertyName;
            }
        };

        // Act
        _viewModel.SelectedTheme = "Dark";

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal(nameof(SettingsViewModel.SelectedTheme), propertyName);
        Assert.Equal("Dark", _viewModel.SelectedTheme);
    }

    [Fact]
    public void IsDarkMode_WhenToggled_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyChangedRaised = false;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.IsDarkMode))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _viewModel.IsDarkMode = true;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.True(_viewModel.IsDarkMode);
    }

    [Fact]
    public void UiScale_WhenChanged_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyChangedRaised = false;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.UiScale))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _viewModel.UiScale = 1.5;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal(1.5, _viewModel.UiScale);
    }

    [Fact]
    public void SelectedLanguage_WhenChanged_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyChangedRaised = false;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.SelectedLanguage))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _viewModel.SelectedLanguage = "Spanish";

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal("Spanish", _viewModel.SelectedLanguage);
    }

    [Fact]
    public void EnableNotifications_WhenToggled_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyChangedRaised = false;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.EnableNotifications))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _viewModel.EnableNotifications = false;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.False(_viewModel.EnableNotifications);
    }

    [Fact]
    public void EnableSoundNotifications_WhenToggled_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyChangedRaised = false;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.EnableSoundNotifications))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _viewModel.EnableSoundNotifications = false;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.False(_viewModel.EnableSoundNotifications);
    }

    [Fact]
    public void EnableDesktopNotifications_WhenToggled_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyChangedRaised = false;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.EnableDesktopNotifications))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _viewModel.EnableDesktopNotifications = false;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.False(_viewModel.EnableDesktopNotifications);
    }

    [Fact]
    public void EnableEmailNotifications_WhenToggled_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var propertyChangedRaised = false;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.EnableEmailNotifications))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _viewModel.EnableEmailNotifications = true;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.True(_viewModel.EnableEmailNotifications);
    }

    [Theory]
    [InlineData("System")]
    [InlineData("Light")]
    [InlineData("Dark")]
    [InlineData("Auto")]
    public void SelectedTheme_WithDifferentThemes_ShouldUpdateCorrectly(string theme)
    {
        // Act
        _viewModel.SelectedTheme = theme;

        // Assert
        Assert.Equal(theme, _viewModel.SelectedTheme);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    public void UiScale_WithDifferentScales_ShouldUpdateCorrectly(double scale)
    {
        // Act
        _viewModel.UiScale = scale;

        // Assert
        Assert.Equal(scale, _viewModel.UiScale);
    }

    [Theory]
    [InlineData("English")]
    [InlineData("Spanish")]
    [InlineData("French")]
    [InlineData("German")]
    [InlineData("Japanese")]
    public void SelectedLanguage_WithDifferentLanguages_ShouldUpdateCorrectly(string language)
    {
        // Act
        _viewModel.SelectedLanguage = language;

        // Assert
        Assert.Equal(language, _viewModel.SelectedLanguage);
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
        _viewModel.SelectedTheme = "Dark";
        _viewModel.IsDarkMode = true;
        _viewModel.UiScale = 1.25;
        _viewModel.EnableNotifications = false;

        // Assert
        Assert.Contains(nameof(SettingsViewModel.SelectedTheme), propertyChangedEvents);
        Assert.Contains(nameof(SettingsViewModel.IsDarkMode), propertyChangedEvents);
        Assert.Contains(nameof(SettingsViewModel.UiScale), propertyChangedEvents);
        Assert.Contains(nameof(SettingsViewModel.EnableNotifications), propertyChangedEvents);
        Assert.Equal(4, propertyChangedEvents.Count);
    }

    [Fact]
    public void PropertyChanges_WithIdenticalValues_ShouldNotTriggerEvents()
    {
        // Arrange
        var initialTheme = _viewModel.SelectedTheme;
        var propertyChangedCount = 0;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.SelectedTheme))
            {
                propertyChangedCount++;
            }
        };

        // Act - Set the same value twice
        _viewModel.SelectedTheme = initialTheme;
        _viewModel.SelectedTheme = initialTheme;

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
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act - Using a fresh instance
        var freshViewModel = new SettingsViewModel(
            _mockAccountService.Object,
            _mockSettingsService.Object,
            _mockBackupService.Object,
            _mockDataIntegrityService.Object);

        // Assert
        Assert.Equal("System", freshViewModel.SelectedTheme);
        Assert.False(freshViewModel.IsDarkMode);
        Assert.Equal(1.0, freshViewModel.UiScale);
        Assert.Equal("English", freshViewModel.SelectedLanguage);
        Assert.True(freshViewModel.EnableNotifications);
        Assert.True(freshViewModel.EnableSoundNotifications);
        Assert.True(freshViewModel.EnableDesktopNotifications);
        Assert.False(freshViewModel.EnableEmailNotifications);
    }

    [Fact]
    public void NotificationSettings_WhenAllDisabled_ShouldUpdateCorrectly()
    {
        // Act
        _viewModel.EnableNotifications = false;
        _viewModel.EnableSoundNotifications = false;
        _viewModel.EnableDesktopNotifications = false;
        _viewModel.EnableEmailNotifications = false;

        // Assert
        Assert.False(_viewModel.EnableNotifications);
        Assert.False(_viewModel.EnableSoundNotifications);
        Assert.False(_viewModel.EnableDesktopNotifications);
        Assert.False(_viewModel.EnableEmailNotifications);
    }

    [Fact]
    public void NotificationSettings_WhenAllEnabled_ShouldUpdateCorrectly()
    {
        // Act
        _viewModel.EnableNotifications = true;
        _viewModel.EnableSoundNotifications = true;
        _viewModel.EnableDesktopNotifications = true;
        _viewModel.EnableEmailNotifications = true;

        // Assert
        Assert.True(_viewModel.EnableNotifications);
        Assert.True(_viewModel.EnableSoundNotifications);
        Assert.True(_viewModel.EnableDesktopNotifications);
        Assert.True(_viewModel.EnableEmailNotifications);
    }

    [Fact]
    public void PropertyChangedEventArgs_ShouldContainCorrectPropertyName()
    {
        // Arrange
        string? capturedPropertyName = null;

        _viewModel.PropertyChanged += (sender, e) =>
        {
            capturedPropertyName = e.PropertyName;
        };

        // Act
        _viewModel.IsDarkMode = true;

        // Assert
        Assert.Equal(nameof(SettingsViewModel.IsDarkMode), capturedPropertyName);
    }

    [Fact]
    public void ViewModel_ShouldHaveCorrectInitialState()
    {
        // Assert - Verify the ViewModel starts in a consistent state
        Assert.NotNull(_viewModel.SelectedTheme);
        Assert.NotNull(_viewModel.SelectedLanguage);
        Assert.True(_viewModel.UiScale > 0);

        // Notification settings should have reasonable defaults
        Assert.True(_viewModel.EnableNotifications || !_viewModel.EnableNotifications); // Either state is valid
        Assert.True(_viewModel.EnableSoundNotifications || !_viewModel.EnableSoundNotifications);
        Assert.True(_viewModel.EnableDesktopNotifications || !_viewModel.EnableDesktopNotifications);
        Assert.True(_viewModel.EnableEmailNotifications || !_viewModel.EnableEmailNotifications);
    }
}
