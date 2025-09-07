using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SocialMediaCommander.Services.Implementation;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Unit tests for SettingsService functionality
/// </summary>
public class SettingsServiceTests : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly string _tempDirectory;
    private readonly string _originalAppData;

    public SettingsServiceTests()
    {
        // Create temp directory for testing
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"SMC_SettingsTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
        
        // Override AppData environment variable for testing
        _originalAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        Environment.SetEnvironmentVariable("APPDATA", _tempDirectory, EnvironmentVariableTarget.Process);
        
        _settingsService = new SettingsService();
    }

    [Fact]
    public async Task GetSettingsAsync_FirstTime_ShouldCreateDefaultSettings()
    {
        // Act
        var settings = await _settingsService.GetSettingsAsync();

        // Assert
        settings.Should().NotBeNull();
        settings.Version.Should().Be("1.0");
        settings.Theme.Should().Be("Auto");
        settings.Language.Should().Be("en-US");
        settings.EnableCaching.Should().BeTrue();
        settings.CustomSettings.Should().NotBeNull();
        settings.CustomSettings.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSettingsAsync_CalledTwice_ShouldReturnCachedSettings()
    {
        // Act
        var settings1 = await _settingsService.GetSettingsAsync();
        var settings2 = await _settingsService.GetSettingsAsync();

        // Assert
        settings1.Should().BeSameAs(settings2);
    }

    [Fact]
    public async Task SaveSettingsAsync_ValidSettings_ShouldPersistSettings()
    {
        // Arrange
        var settings = await _settingsService.GetSettingsAsync();
        settings.Theme = "Dark";
        settings.AutoSaveInterval = TimeSpan.FromMinutes(10);
        settings.EnableDebugLogging = true;

        // Act
        await _settingsService.SaveSettingsAsync(settings);

        // Create new service instance to test persistence
        var newService = new SettingsService();
        var loadedSettings = await newService.GetSettingsAsync();

        // Assert
        loadedSettings.Theme.Should().Be("Dark");
        loadedSettings.AutoSaveInterval.Should().Be(TimeSpan.FromMinutes(10));
        loadedSettings.EnableDebugLogging.Should().BeTrue();
    }

    [Fact]
    public async Task GetSettingAsync_ExistingSetting_ShouldReturnValue()
    {
        // Arrange
        await _settingsService.SetSettingAsync("TestKey", "TestValue");

        // Act
        var result = await _settingsService.GetSettingAsync<string>("TestKey");

        // Assert
        result.Should().Be("TestValue");
    }

    [Fact]
    public async Task GetSettingAsync_NonExistentSetting_ShouldReturnDefault()
    {
        // Act
        var result = await _settingsService.GetSettingAsync("NonExistent", "DefaultValue");

        // Assert
        result.Should().Be("DefaultValue");
    }

    [Fact]
    public async Task GetSettingAsync_DifferentTypes_ShouldHandleTypeConversion()
    {
        // Arrange
        await _settingsService.SetSettingAsync("IntSetting", 42);
        await _settingsService.SetSettingAsync("BoolSetting", true);
        await _settingsService.SetSettingAsync("DoubleSetting", 3.14);

        // Act
        var intResult = await _settingsService.GetSettingAsync<int>("IntSetting");
        var boolResult = await _settingsService.GetSettingAsync<bool>("BoolSetting");
        var doubleResult = await _settingsService.GetSettingAsync<double>("DoubleSetting");

        // Assert
        intResult.Should().Be(42);
        boolResult.Should().BeTrue();
        doubleResult.Should().Be(3.14);
    }

    [Fact]
    public async Task SetSettingAsync_NewSetting_ShouldAddToCustomSettings()
    {
        // Arrange
        var key = "NewCustomSetting";
        var value = "CustomValue";

        // Act
        await _settingsService.SetSettingAsync(key, value);

        // Assert
        var settings = await _settingsService.GetSettingsAsync();
        settings.CustomSettings.Should().ContainKey(key);
        settings.CustomSettings[key].Should().Be(value);
    }

    [Fact]
    public async Task SetSettingAsync_ComplexObject_ShouldSerializeCorrectly()
    {
        // Arrange
        var complexObject = new Dictionary<string, int>
        {
            ["Key1"] = 100,
            ["Key2"] = 200
        };

        // Act
        await _settingsService.SetSettingAsync("ComplexSetting", complexObject);
        var result = await _settingsService.GetSettingAsync<Dictionary<string, int>>("ComplexSetting");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result["Key1"].Should().Be(100);
        result["Key2"].Should().Be(200);
    }

    [Fact]
    public async Task RemoveSettingAsync_ExistingSetting_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        await _settingsService.SetSettingAsync("ToRemove", "Value");

        // Act
        var removed = await _settingsService.RemoveSettingAsync("ToRemove");

        // Assert
        removed.Should().BeTrue();
        var result = await _settingsService.GetSettingAsync<string>("ToRemove", "Default");
        result.Should().Be("Default");
    }

    [Fact]
    public async Task RemoveSettingAsync_NonExistentSetting_ShouldReturnFalse()
    {
        // Act
        var removed = await _settingsService.RemoveSettingAsync("NonExistent");

        // Assert
        removed.Should().BeFalse();
    }

    [Fact]
    public async Task ResetToDefaultsAsync_ShouldRestoreDefaultSettings()
    {
        // Arrange
        var settings = await _settingsService.GetSettingsAsync();
        settings.Theme = "Dark";
        settings.EnableDebugLogging = true;
        await _settingsService.SetSettingAsync("CustomSetting", "CustomValue");
        await _settingsService.SaveSettingsAsync(settings);

        // Act
        await _settingsService.ResetToDefaultsAsync();

        // Assert
        var resetSettings = await _settingsService.GetSettingsAsync();
        resetSettings.Theme.Should().Be("Auto");
        resetSettings.EnableDebugLogging.Should().BeFalse();
        resetSettings.CustomSettings.Should().BeEmpty();
    }

    [Fact]
    public async Task ExportSettingsAsync_ValidPath_ShouldExportToFile()
    {
        // Arrange
        var exportPath = Path.Combine(_tempDirectory, "exported_settings.json");
        await _settingsService.SetSettingAsync("ExportTest", "ExportValue");

        // Act
        var result = await _settingsService.ExportSettingsAsync(exportPath);

        // Assert
        result.Should().BeTrue();
        File.Exists(exportPath).Should().BeTrue();
        
        var exportedJson = await File.ReadAllTextAsync(exportPath);
        exportedJson.Should().Contain("exportTest");
        exportedJson.Should().Contain("ExportValue");
    }

    [Fact]
    public async Task ExportSettingsAsync_InvalidPath_ShouldReturnFalse()
    {
        // Arrange
        var invalidPath = Path.Combine("Z:\\NonExistent\\", "settings.json");

        // Act
        var result = await _settingsService.ExportSettingsAsync(invalidPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ImportSettingsAsync_ValidFile_ShouldImportSettings()
    {
        // Arrange
        var importPath = Path.Combine(_tempDirectory, "import_settings.json");
        var testSettings = new AppSettings
        {
            Version = "1.0",
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            Theme = "Dark",
            EnableDebugLogging = true,
            CustomSettings = new Dictionary<string, object?>
            {
                ["ImportedSetting"] = "ImportedValue"
            }
        };

        var json = JsonSerializer.Serialize(testSettings, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(importPath, json);

        // Act
        var result = await _settingsService.ImportSettingsAsync(importPath);

        // Assert
        result.Should().BeTrue();
        var settings = await _settingsService.GetSettingsAsync();
        settings.Theme.Should().Be("Dark");
        settings.EnableDebugLogging.Should().BeTrue();
        var importedValue = await _settingsService.GetSettingAsync<string>("ImportedSetting");
        importedValue.Should().Be("ImportedValue");
    }

    [Fact]
    public async Task ImportSettingsAsync_NonExistentFile_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.json");

        // Act
        var result = await _settingsService.ImportSettingsAsync(nonExistentPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ImportSettingsAsync_InvalidJson_ShouldReturnFalse()
    {
        // Arrange
        var invalidJsonPath = Path.Combine(_tempDirectory, "invalid.json");
        await File.WriteAllTextAsync(invalidJsonPath, "{ invalid json content");

        // Act
        var result = await _settingsService.ImportSettingsAsync(invalidJsonPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AppSettings_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var settings = new AppSettings();

        // Assert
        settings.Theme.Should().Be("Auto");
        settings.Language.Should().Be("en-US");
        settings.StartupView.Should().Be("Standard");
        settings.ShowNotifications.Should().BeTrue();
        settings.MinimizeToTray.Should().BeFalse();
        settings.EncryptBackups.Should().BeTrue();
        settings.EnableCaching.Should().BeTrue();
        settings.AllowAnalytics.Should().BeFalse();
        settings.AllowCrashReporting.Should().BeTrue();
        settings.EnableDebugLogging.Should().BeFalse();
        settings.LogLevel.Should().Be("Information");
        settings.CustomSettings.Should().NotBeNull();
        settings.CustomSettings.Should().BeEmpty();
    }

    [Fact]
    public void WindowSettings_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var windowSettings = new WindowSettings();

        // Assert
        windowSettings.Width.Should().Be(1200);
        windowSettings.Height.Should().Be(800);
        windowSettings.Left.Should().Be(100);
        windowSettings.Top.Should().Be(100);
        windowSettings.IsMaximized.Should().BeFalse();
        windowSettings.RememberPosition.Should().BeTrue();
    }

    [Fact]
    public async Task GetSettingAsync_WithInvalidTypeConversion_ShouldReturnDefault()
    {
        // Arrange
        await _settingsService.SetSettingAsync("StringValue", "NotANumber");

        // Act
        var result = await _settingsService.GetSettingAsync<int>("StringValue", 999);

        // Assert
        result.Should().Be(999); // Should return default value when conversion fails
    }

    public void Dispose()
    {
        // Restore original AppData environment variable
        Environment.SetEnvironmentVariable("APPDATA", _originalAppData, EnvironmentVariableTarget.Process);
        
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