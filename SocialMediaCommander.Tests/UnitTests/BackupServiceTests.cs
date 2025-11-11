using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Unit tests for BackupService functionality
/// </summary>
public class BackupServiceTests : IDisposable
{
    private readonly BackupService _backupService;
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly Mock<IOAuthConfigurationService> _mockOAuthConfigService;
    private readonly string _tempDirectory;

    public BackupServiceTests()
    {
        _mockAccountService = new Mock<IAccountService>();
        _mockOAuthConfigService = new Mock<IOAuthConfigurationService>();

        // Create temp directory for testing
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"SMC_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);

        // Create backup service with mocked dependencies and custom directory
        _backupService = new BackupService(_mockAccountService.Object, _mockOAuthConfigService.Object, _tempDirectory);
    }

    [Fact]
    public async Task CreateBackupAsync_WithDefaultName_ShouldCreateBackupFile()
    {
        // Arrange
        var testAccounts = CreateTestAccounts();
        var testOAuthConfigs = CreateTestOAuthConfigs();

        _mockAccountService.Setup(x => x.GetAllAccountsAsync())
            .ReturnsAsync(testAccounts);
        _mockOAuthConfigService.Setup(x => x.GetAllConfigurationsAsync())
            .ReturnsAsync(testOAuthConfigs);

        // Act
        var backupPath = await _backupService.CreateBackupAsync();

        // Assert
        backupPath.Should().NotBeNullOrEmpty();
        File.Exists(backupPath).Should().BeTrue();
        Path.GetExtension(backupPath).Should().Be(".smcbackup");
        _mockAccountService.Verify(x => x.GetAllAccountsAsync(), Times.Once);
        _mockOAuthConfigService.Verify(x => x.GetAllConfigurationsAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateBackupAsync_WithCustomName_ShouldUseCustomName()
    {
        // Arrange
        var customName = "MyCustomBackup";
        var testAccounts = CreateTestAccounts();
        var testOAuthConfigs = CreateTestOAuthConfigs();

        _mockAccountService.Setup(x => x.GetAllAccountsAsync())
            .ReturnsAsync(testAccounts);
        _mockOAuthConfigService.Setup(x => x.GetAllConfigurationsAsync())
            .ReturnsAsync(testOAuthConfigs);

        // Act
        var backupPath = await _backupService.CreateBackupAsync(customName);

        // Assert
        backupPath.Should().Contain(customName);
        File.Exists(backupPath).Should().BeTrue();
    }

    [Fact]
    public async Task CreateBackupAsync_WhenAccountServiceThrows_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAccountService.Setup(x => x.GetAllAccountsAsync())
            .ThrowsAsync(new Exception("Account service error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _backupService.CreateBackupAsync());

        exception.Message.Should().Contain("Failed to create backup");
        exception.InnerException?.Message.Should().Be("Account service error");
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithValidBackup_ShouldRestoreData()
    {
        // Arrange
        var testAccounts = CreateTestAccounts();
        var testOAuthConfigs = CreateTestOAuthConfigs();

        _mockAccountService.Setup(x => x.GetAllAccountsAsync())
            .ReturnsAsync(testAccounts);
        _mockOAuthConfigService.Setup(x => x.GetAllConfigurationsAsync())
            .ReturnsAsync(testOAuthConfigs);
        _mockAccountService.Setup(x => x.GetAccountByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Account?)null);
        _mockAccountService.Setup(x => x.CreateAccountAsync(It.IsAny<Account>()))
            .ReturnsAsync((Account account) => account);
        _mockOAuthConfigService.Setup(x => x.SaveConfigurationAsync(It.IsAny<SocialPlatform>(), It.IsAny<OAuthConfig>()))
            .Returns(Task.CompletedTask);

        // Create a backup first
        var backupPath = await _backupService.CreateBackupAsync("TestRestore");

        // Act
        await _backupService.RestoreFromBackupAsync(backupPath);

        // Assert
        _mockAccountService.Verify(x => x.CreateAccountAsync(It.IsAny<Account>()),
            Times.Exactly(testAccounts.Count()));
        _mockOAuthConfigService.Verify(x => x.SaveConfigurationAsync(It.IsAny<SocialPlatform>(), It.IsAny<OAuthConfig>()),
            Times.Exactly(testOAuthConfigs.Count));
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.smcbackup");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _backupService.RestoreFromBackupAsync(nonExistentPath));

        exception.InnerException.Should().BeOfType<FileNotFoundException>();
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithExistingAccount_ShouldUpdateAccount()
    {
        // Arrange
        var testAccounts = CreateTestAccounts();
        var existingAccount = testAccounts.First();

        _mockAccountService.Setup(x => x.GetAllAccountsAsync())
            .ReturnsAsync(testAccounts);
        _mockOAuthConfigService.Setup(x => x.GetAllConfigurationsAsync())
            .ReturnsAsync(new Dictionary<SocialPlatform, OAuthConfig>());
        _mockAccountService.Setup(x => x.GetAccountByIdAsync(existingAccount.Id))
            .ReturnsAsync(existingAccount);
        _mockAccountService.Setup(x => x.UpdateAccountAsync(It.IsAny<Account>()))
            .ReturnsAsync((Account account) => account);

        var backupPath = await _backupService.CreateBackupAsync("TestUpdate");

        // Act
        await _backupService.RestoreFromBackupAsync(backupPath);

        // Assert
        _mockAccountService.Verify(x => x.UpdateAccountAsync(It.Is<Account>(a => a.Id == existingAccount.Id)),
            Times.Once);
    }

    [Fact]
    public async Task GetAvailableBackupsAsync_WithExistingBackups_ShouldReturnBackupInfo()
    {
        // Arrange
        var testAccounts = CreateTestAccounts();
        var testOAuthConfigs = CreateTestOAuthConfigs();

        _mockAccountService.Setup(x => x.GetAllAccountsAsync())
            .ReturnsAsync(testAccounts);
        _mockOAuthConfigService.Setup(x => x.GetAllConfigurationsAsync())
            .ReturnsAsync(testOAuthConfigs);

        await _backupService.CreateBackupAsync("Backup1");
        await _backupService.CreateBackupAsync("Backup2");

        // Act
        var backups = await _backupService.GetAvailableBackupsAsync();

        // Assert
        backups.Should().HaveCount(c => c >= 2);
        backups.All(b => !string.IsNullOrEmpty(b.FilePath)).Should().BeTrue();
        backups.All(b => !string.IsNullOrEmpty(b.FileName)).Should().BeTrue();
        backups.All(b => b.Size > 0).Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableBackupsAsync_WithNoBackups_ShouldReturnEmptyList()
    {
        // Act
        var backups = await _backupService.GetAvailableBackupsAsync();

        // Assert
        backups.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteBackupAsync_WithExistingFile_ShouldDeleteFile()
    {
        // Arrange
        var testAccounts = CreateTestAccounts();
        var testOAuthConfigs = CreateTestOAuthConfigs();

        _mockAccountService.Setup(x => x.GetAllAccountsAsync())
            .ReturnsAsync(testAccounts);
        _mockOAuthConfigService.Setup(x => x.GetAllConfigurationsAsync())
            .ReturnsAsync(testOAuthConfigs);

        var backupPath = await _backupService.CreateBackupAsync("ToDelete");
        File.Exists(backupPath).Should().BeTrue();

        // Act
        await _backupService.DeleteBackupAsync(backupPath);

        // Assert
        File.Exists(backupPath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBackupAsync_WithNonExistentFile_ShouldNotThrow()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.smcbackup");

        // Act & Assert
        await _backupService.DeleteBackupAsync(nonExistentPath); // Should not throw
    }

    [Fact]
    public async Task GetBackupMetadataAsync_WithValidBackup_ShouldReturnMetadata()
    {
        // Arrange
        var testAccounts = CreateTestAccounts();
        var testOAuthConfigs = CreateTestOAuthConfigs();

        _mockAccountService.Setup(x => x.GetAllAccountsAsync())
            .ReturnsAsync(testAccounts);
        _mockOAuthConfigService.Setup(x => x.GetAllConfigurationsAsync())
            .ReturnsAsync(testOAuthConfigs);

        var backupPath = await _backupService.CreateBackupAsync("MetadataTest");

        // Act
        var metadata = await _backupService.GetBackupMetadataAsync(backupPath);

        // Assert
        metadata.Should().NotBeNull();
        metadata.Version.Should().Be("1.0");
        metadata.Accounts.Should().HaveCount(testAccounts.Count());
        metadata.OAuthConfigurations.Should().HaveCount(testOAuthConfigs.Count);
        metadata.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void BackupInfo_FormattedSize_ShouldFormatCorrectly()
    {
        // Arrange & Act
        var backupInfo1 = new BackupInfo { Size = 1024 };
        var backupInfo2 = new BackupInfo { Size = 1048576 }; // 1 MB
        var backupInfo3 = new BackupInfo { Size = 512 };

        // Assert
        backupInfo1.FormattedSize.Should().Be("1.0 KB");
        backupInfo2.FormattedSize.Should().Be("1.0 MB");
        backupInfo3.FormattedSize.Should().Be("512.0 B");
    }

    private static List<Account> CreateTestAccounts()
    {
        return new List<Account>
        {
            new Account
            {
                Id = "test1",
                PlatformId = SocialPlatform.BlueSky,
                DisplayName = "Test Account 1",
                Username = "test1",
                AuthStatus = AuthenticationStatus.Authenticated,
                LastUsed = DateTime.UtcNow
            },
            new Account
            {
                Id = "test2",
                PlatformId = SocialPlatform.BlueSky,
                DisplayName = "Test Account 2",
                Username = "test2",
                AuthStatus = AuthenticationStatus.NotAuthenticated,
                LastUsed = DateTime.UtcNow
            }
        };
    }

    private static Dictionary<SocialPlatform, OAuthConfig> CreateTestOAuthConfigs()
    {
        return new Dictionary<SocialPlatform, OAuthConfig>
        {
            [SocialPlatform.BlueSky] = new OAuthConfig
            {
                ClientId = "YOUR_BLUESKY_CLIENT_ID_HERE",
                ClientSecret = "YOUR_BLUESKY_CLIENT_SECRET_HERE",
                AuthorizationEndpoint = "https://bsky.social/oauth/authorize",
                TokenEndpoint = "https://bsky.social/oauth/token",
                RedirectUri = "http://localhost:8080/callback",
                Scopes = ["atproto"]
            }
        };
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
