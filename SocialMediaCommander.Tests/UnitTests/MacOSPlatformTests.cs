using System;
using System.Runtime.InteropServices;
using System.Text;
using SocialMediaCommander.Services.Implementation;
using Xunit;
using FluentAssertions;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Platform-specific tests for macOS functionality
/// These tests verify that the application works correctly on macOS
/// </summary>
public class MacOSPlatformTests
{
    [Fact]
    public void Platform_Detection_ShouldWorkOnAllPlatforms()
    {
        // Act
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        var isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        // Assert
        // Exactly one platform should be true
        var platformCount = (isWindows ? 1 : 0) + (isLinux ? 1 : 0) + (isMacOS ? 1 : 0);
        platformCount.Should().Be(1, "exactly one platform should be detected");
    }

    [Fact]
    public void Encryption_OnMacOS_ShouldUseAES()
    {
        // Arrange & Act
        var encryptionMethod = CrossPlatformEncryption.GetEncryptionMethod();

        // Assert
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            encryptionMethod.Should().Contain("AES", "macOS should use AES encryption");
            encryptionMethod.Should().NotContain("DPAPI", "macOS should not use Windows DPAPI");
        }
    }

    [Fact]
    public void Encryption_OnMacOS_ShouldEncryptAndDecrypt()
    {
        // Skip this test on non-macOS platforms
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        // Arrange
        var originalData = Encoding.UTF8.GetBytes("Test data for macOS encryption");

        // Act
        var encryptedData = CrossPlatformEncryption.Protect(originalData);
        var decryptedData = CrossPlatformEncryption.Unprotect(encryptedData);

        // Assert
        decryptedData.Should().Equal(originalData, "encryption and decryption should work on macOS");
        encryptedData.Should().NotEqual(originalData, "encrypted data should be different from original");
    }

    [Fact]
    public void Encryption_OnMacOS_WithEntropy_ShouldWork()
    {
        // Skip this test on non-macOS platforms
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        // Arrange
        var originalData = Encoding.UTF8.GetBytes("Sensitive macOS data");
        var entropy = "macOS_specific_entropy_string";

        // Act
        var encryptedData = CrossPlatformEncryption.Protect(originalData, entropy);
        var decryptedData = CrossPlatformEncryption.Unprotect(encryptedData, entropy);

        // Assert
        decryptedData.Should().Equal(originalData);

        // Verify wrong entropy fails
        var wrongEntropy = "wrong_entropy";
        var wrongDecryptAction = () => CrossPlatformEncryption.Unprotect(encryptedData, wrongEntropy);
        wrongDecryptAction.Should().Throw<Exception>("decryption with wrong entropy should fail");
    }

    [Fact]
    public void RuntimeInformation_Architecture_ShouldBeValid()
    {
        // Act
        var architecture = RuntimeInformation.ProcessArchitecture;

        // Assert
        architecture.Should().BeOneOf(
            Architecture.X64,
            Architecture.Arm64,
            Architecture.X86,
            Architecture.Arm
        );
    }

    [Fact]
    public void Environment_MachineName_ShouldNotBeEmpty()
    {
        // Act
        var machineName = Environment.MachineName;

        // Assert
        machineName.Should().NotBeNullOrEmpty("machine name should be available on all platforms");
    }

    [Fact]
    public void Environment_UserName_ShouldNotBeEmpty()
    {
        // Act
        var userName = Environment.UserName;

        // Assert
        userName.Should().NotBeNullOrEmpty("user name should be available on all platforms");
    }

    [Fact]
    public void Environment_OSVersion_ShouldBeValid()
    {
        // Act
        var osVersion = Environment.OSVersion;

        // Assert
        osVersion.Should().NotBeNull("OS version should be available");
        osVersion.Platform.Should().BeOneOf(
            PlatformID.Unix,        // macOS and Linux
            PlatformID.Win32NT,     // Windows
            PlatformID.MacOSX       // Legacy macOS
        );
    }

    [Fact]
    public void CurrentDirectory_ShouldBeAccessible()
    {
        // Act
        var currentDirectory = Environment.CurrentDirectory;

        // Assert
        currentDirectory.Should().NotBeNullOrEmpty("current directory should be accessible");
        System.IO.Directory.Exists(currentDirectory).Should().BeTrue("current directory should exist");
    }

    [Fact]
    public void SpecialFolder_ApplicationData_ShouldExist()
    {
        // Act
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // Assert
        appDataPath.Should().NotBeNullOrEmpty("ApplicationData folder path should be available on all platforms");
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            appDataPath.Should().Contain("/Library/Application Support", 
                "macOS ApplicationData should point to Library/Application Support or .config");
        }
    }

    [Fact]
    public void SpecialFolder_LocalApplicationData_ShouldExist()
    {
        // Act
        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Assert
        localAppDataPath.Should().NotBeNullOrEmpty("LocalApplicationData folder path should be available");
    }

    [Fact]
    public void PathSeparator_ShouldBeCorrectForPlatform()
    {
        // Act
        var separator = System.IO.Path.DirectorySeparatorChar;

        // Assert
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            separator.Should().Be('\\', "Windows should use backslash");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            separator.Should().Be('/', "macOS and Linux should use forward slash");
        }
    }

    [Fact]
    public void NewLine_ShouldBeCorrectForPlatform()
    {
        // Act
        var newLine = Environment.NewLine;

        // Assert
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            newLine.Should().Be("\r\n", "Windows should use CRLF");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            newLine.Should().Be("\n", "macOS and Linux should use LF");
        }
    }

    [Fact]
    public void ProcessorCount_ShouldBeGreaterThanZero()
    {
        // Act
        var processorCount = Environment.ProcessorCount;

        // Assert
        processorCount.Should().BeGreaterThan(0, "processor count should be at least 1");
    }

    [Fact]
    public void Is64BitOperatingSystem_ShouldBeTrue()
    {
        // Act
        var is64Bit = Environment.Is64BitOperatingSystem;

        // Assert
        is64Bit.Should().BeTrue("modern systems should be 64-bit");
    }

    [Fact]
    public void RuntimeIdentifier_ShouldBeValid()
    {
        // Act
        var runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;

        // Assert
        runtimeIdentifier.Should().NotBeNullOrEmpty("runtime identifier should be available");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            runtimeIdentifier.Should().Contain("osx", "macOS runtime identifier should contain 'osx'");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            runtimeIdentifier.Should().Contain("linux", "Linux runtime identifier should contain 'linux'");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            runtimeIdentifier.Should().Contain("win", "Windows runtime identifier should contain 'win'");
        }
    }
}
