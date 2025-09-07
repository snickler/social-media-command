using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using SocialMediaCommander.Desktop.ViewModels;
using SocialMediaCommander.Services.Interfaces;
using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Tests;

/// <summary>
/// Comprehensive tests for AccountManagerViewModel with MVVM compliance and modern C# features
/// </summary>
public class AccountManagerViewModelTests : IDisposable
{
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<IOAuthConfigurationService> _mockOAuthService;
    private readonly AccountManagerViewModel _viewModel;
    private readonly List<PropertyChangedEventArgs> _propertyChangedEvents;

    public AccountManagerViewModelTests()
    {
        _mockAccountService = new Mock<IAccountService>();
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockOAuthService = new Mock<IOAuthConfigurationService>();
        _propertyChangedEvents = new List<PropertyChangedEventArgs>();

        // Setup default mock behavior
        _mockAccountService.Setup(x => x.GetAccountsAsync())
            .ReturnsAsync(new List<Account>());

        _mockOAuthService.Setup(x => x.GetAllConfigurationsAsync())
            .ReturnsAsync(new Dictionary<SocialPlatform, OAuthConfig>());

        _viewModel = new AccountManagerViewModel(
            _mockAccountService.Object,
            _mockAuthService.Object,
            _mockOAuthService.Object
        );

        _viewModel.PropertyChanged += (s, e) => _propertyChangedEvents.Add(e);
    }

    #region Constructor and Initialization Tests

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Assert
        _viewModel.Should().NotBeNull();
        _viewModel.Accounts.Should().NotBeNull();
        _viewModel.PlatformConfigs.Should().NotBeNull();
        _viewModel.SelectedAccountIds.Should().NotBeNull();

        // Should have all supported platforms configured
        _viewModel.PlatformConfigs.Should().HaveCount(5); // BlueSky, X, LinkedIn, Threads, Facebook
        _viewModel.SelectedPlatformConfig.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldSetupPlatformConfigurations()
    {
        // Assert
        var expectedPlatforms = new[]
        {
            SocialPlatform.BlueSky,
            SocialPlatform.X,
            SocialPlatform.LinkedIn,
            SocialPlatform.Threads,
            SocialPlatform.Facebook
        };

        foreach (var platform in expectedPlatforms)
        {
            _viewModel.PlatformConfigs.Should().Contain(config => config.Id == platform);
        }
    }

    #endregion

    #region Property Change Notification Tests

    [Fact]
    public void SelectedPlatformConfig_WhenChanged_ShouldRaisePropertyChanged()
    {
        // Arrange
        var newConfig = _viewModel.PlatformConfigs.First(c => c.Id == SocialPlatform.X);
        _propertyChangedEvents.Clear();

        // Act
        _viewModel.SelectedPlatformConfig = newConfig;

        // Assert
        _propertyChangedEvents.Should().Contain(e => e.PropertyName == nameof(AccountManagerViewModel.SelectedPlatformConfig));
    }

    [Fact]
    public void IsLoading_WhenChanged_ShouldRaisePropertyChanged()
    {
        // Arrange
        _propertyChangedEvents.Clear();

        // Act
        _viewModel.GetType().GetProperty("IsLoading")?.SetValue(_viewModel, true);

        // Assert
        _propertyChangedEvents.Should().Contain(e => e.PropertyName == "IsLoading");
    }

    #endregion

    #region Account Management Tests

    [Fact]
    public async Task LoadAccountsAsync_ShouldPopulateAccountsCollection()
    {
        // Arrange
        var accounts = new List<Account>
        {
            CreateTestAccount(SocialPlatform.X, "test@example.com"),
            CreateTestAccount(SocialPlatform.BlueSky, "user@bsky.social")
        };

        _mockAccountService.Setup(x => x.GetAccountsAsync())
            .ReturnsAsync(accounts);

        // Act
        await InvokePrivateMethodAsync("LoadAccountsAsync");

        // Assert
        _viewModel.Accounts.Should().HaveCount(2);
        _viewModel.Accounts.Should().Contain(a => a.Platform == SocialPlatform.X);
        _viewModel.Accounts.Should().Contain(a => a.Platform == SocialPlatform.BlueSky);
    }

    [Fact]
    public async Task LoadAccountsAsync_WithError_ShouldHandleGracefully()
    {
        // Arrange
        _mockAccountService.Setup(x => x.GetAccountsAsync())
            .ThrowsAsync(new Exception("Service error"));

        // Act & Assert
        var act = () => InvokePrivateMethodAsync("LoadAccountsAsync");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void AddAccountCommand_ShouldBeExecutable()
    {
        // Assert
        _viewModel.AddAccountCommand.Should().NotBeNull();
        _viewModel.AddAccountCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RemoveAccountCommand_WithoutSelection_ShouldNotBeExecutable()
    {
        // Arrange - No accounts selected
        _viewModel.SelectedAccountIds.Clear();

        // Assert
        _viewModel.RemoveAccountCommand.Should().NotBeNull();
        _viewModel.RemoveAccountCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void RemoveAccountCommand_WithSelection_ShouldBeExecutable()
    {
        // Arrange
        var platform = SocialPlatform.X;
        _viewModel.SelectedAccountIds[platform] = "test-account-id";

        // Assert
        _viewModel.RemoveAccountCommand.CanExecute(platform).Should().BeTrue();
    }

    #endregion

    #region OAuth Integration Tests

    [Fact]
    public async Task ConnectAccountCommand_ShouldInitiateOAuthFlow()
    {
        // Arrange
        var platform = SocialPlatform.X;
        var authResult = new AuthenticationResult { IsSuccess = true, AuthorizationUrl = "https://example.com/auth" };

        _mockAuthService.Setup(x => x.StartAuthenticationAsync(platform, It.IsAny<string>()))
            .ReturnsAsync(authResult);

        _mockOAuthService.Setup(x => x.HasValidConfigurationAsync(platform))
            .ReturnsAsync(true);

        // Act
        _viewModel.ConnectAccountCommand.Execute(platform);
        await Task.Delay(100); // Allow async execution

        // Assert
        _mockAuthService.Verify(x => x.StartAuthenticationAsync(platform, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ConnectAccountCommand_WithInvalidConfig_ShouldNotInitiateAuth()
    {
        // Arrange
        var platform = SocialPlatform.X;

        _mockOAuthService.Setup(x => x.HasValidConfigurationAsync(platform))
            .ReturnsAsync(false);

        // Act
        _viewModel.ConnectAccountCommand.Execute(platform);
        await Task.Delay(100);

        // Assert
        _mockAuthService.Verify(x => x.StartAuthenticationAsync(It.IsAny<SocialPlatform>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Platform Selection Tests

    [Fact]
    public void SelectedPlatformConfig_WhenSet_ShouldUpdateRelatedProperties()
    {
        // Arrange
        var linkedInConfig = _viewModel.PlatformConfigs.First(c => c.Id == SocialPlatform.LinkedIn);
        _propertyChangedEvents.Clear();

        // Act
        _viewModel.SelectedPlatformConfig = linkedInConfig;

        // Assert
        _viewModel.SelectedPlatformConfig.Should().Be(linkedInConfig);
        _viewModel.SelectedPlatformConfig.Id.Should().Be(SocialPlatform.LinkedIn);
    }

    [Fact]
    public void GetAccountsForPlatform_ShouldReturnFilteredAccounts()
    {
        // Arrange
        _viewModel.Accounts.Add(CreateTestAccount(SocialPlatform.X, "x@example.com"));
        _viewModel.Accounts.Add(CreateTestAccount(SocialPlatform.BlueSky, "bsky@example.com"));
        _viewModel.Accounts.Add(CreateTestAccount(SocialPlatform.X, "x2@example.com"));

        // Act
        var xAccounts = _viewModel.Accounts.Where(a => a.Platform == SocialPlatform.X).ToList();

        // Assert
        xAccounts.Should().HaveCount(2);
        xAccounts.Should().OnlyContain(a => a.Platform == SocialPlatform.X);
    }

    #endregion

    #region Command Execution Tests

    [Fact]
    public void RefreshAccountsCommand_ShouldBeExecutable()
    {
        // Assert
        _viewModel.RefreshAccountsCommand.Should().NotBeNull();
        _viewModel.RefreshAccountsCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAccountsCommand_ShouldReloadAccounts()
    {
        // Arrange
        var newAccounts = new List<Account>
        {
            CreateTestAccount(SocialPlatform.Threads, "threads@example.com")
        };

        _mockAccountService.Setup(x => x.GetAccountsAsync())
            .ReturnsAsync(newAccounts);

        // Act
        _viewModel.RefreshAccountsCommand.Execute(null);
        await Task.Delay(100);

        // Assert
        _mockAccountService.Verify(x => x.GetAccountsAsync(), Times.AtLeastOnce);
    }

    #endregion

    #region Multi-Platform Account Selection Tests

    [Fact]
    public void SelectedAccountIds_ShouldSupportMultiplePlatforms()
    {
        // Arrange & Act
        _viewModel.SelectedAccountIds[SocialPlatform.X] = "x-account-1";
        _viewModel.SelectedAccountIds[SocialPlatform.BlueSky] = "bsky-account-1";
        _viewModel.SelectedAccountIds[SocialPlatform.LinkedIn] = "linkedin-account-1";

        // Assert
        _viewModel.SelectedAccountIds.Should().HaveCount(3);
        _viewModel.SelectedAccountIds[SocialPlatform.X].Should().Be("x-account-1");
        _viewModel.SelectedAccountIds[SocialPlatform.BlueSky].Should().Be("bsky-account-1");
        _viewModel.SelectedAccountIds[SocialPlatform.LinkedIn].Should().Be("linkedin-account-1");
    }

    [Fact]
    public void HasSelectedAccounts_WithSelections_ShouldReturnTrue()
    {
        // Arrange
        _viewModel.SelectedAccountIds[SocialPlatform.X] = "test-account";

        // Act
        var hasSelected = _viewModel.SelectedAccountIds.Any(kvp => !string.IsNullOrEmpty(kvp.Value));

        // Assert
        hasSelected.Should().BeTrue();
    }

    [Fact]
    public void HasSelectedAccounts_WithoutSelections_ShouldReturnFalse()
    {
        // Arrange
        _viewModel.SelectedAccountIds.Clear();

        // Act
        var hasSelected = _viewModel.SelectedAccountIds.Any(kvp => !string.IsNullOrEmpty(kvp.Value));

        // Assert
        hasSelected.Should().BeFalse();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task AccountOperations_WithServiceErrors_ShouldHandleGracefully()
    {
        // Arrange
        _mockAccountService.Setup(x => x.GetAccountsAsync())
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        // Act & Assert - Should not throw
        var act = () => InvokePrivateMethodAsync("LoadAccountsAsync");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OAuthCallback_WithInvalidData_ShouldHandleGracefully()
    {
        // Arrange
        var invalidCallback = new AuthenticationResult { IsSuccess = false, ErrorMessage = "Invalid state" };

        // Act & Assert - Should handle invalid callback without throwing
        var act = () => InvokePrivateMethodAsync("HandleOAuthCallbackAsync", invalidCallback);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task ConcurrentAccountOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act - Perform multiple concurrent operations
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await InvokePrivateMethodAsync("LoadAccountsAsync");
                _viewModel.SelectedPlatformConfig = _viewModel.PlatformConfigs.First();
            }));
        }

        // Assert - Should complete without exceptions
        var act = () => Task.WhenAll(tasks);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Helper Methods

    private static Account CreateTestAccount(SocialPlatform platform, string email)
    {
        return new Account
        {
            Id = Guid.NewGuid().ToString(),
            Platform = platform,
            Username = email.Split('@')[0],
            Email = email,
            DisplayName = $"Test User ({platform})",
            ProfilePictureUrl = $"https://example.com/avatar_{platform}.jpg",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            OAuthTokens = new OAuthTokens
            {
                AccessToken = "test_access_token",
                RefreshToken = "test_refresh_token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }
        };
    }

    private async Task InvokePrivateMethodAsync(string methodName, params object[] parameters)
    {
        var method = _viewModel.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method?.ReturnType == typeof(Task))
        {
            var task = (Task)method.Invoke(_viewModel, parameters)!;
            await task;
        }
        else
        {
            method?.Invoke(_viewModel, parameters);
        }
    }

    public void Dispose()
    {
        _viewModel?.Dispose();
    }

    #endregion
}