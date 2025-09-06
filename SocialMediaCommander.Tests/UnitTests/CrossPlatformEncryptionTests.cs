using System;
using System.Runtime.InteropServices;
using System.Text;
using SocialMediaCommander.Services.Implementation;
using Xunit;
using FluentAssertions;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Unit tests for CrossPlatformEncryption to ensure secure data protection
/// </summary>
public class CrossPlatformEncryptionTests
{
    [Fact]
    public void Protect_WithValidData_ShouldReturnEncryptedBytes()
    {
        // Arrange
        var originalData = Encoding.UTF8.GetBytes("Test sensitive data");

        // Act
        var encryptedData = CrossPlatformEncryption.Protect(originalData);

        // Assert
        encryptedData.Should().NotBeNull();
        encryptedData.Should().NotBeEmpty();
        encryptedData.Should().NotEqual(originalData);
    }

    [Fact]
    public void Unprotect_WithEncryptedData_ShouldReturnOriginalData()
    {
        // Arrange
        var originalData = Encoding.UTF8.GetBytes("Test sensitive data");
        var encryptedData = CrossPlatformEncryption.Protect(originalData);

        // Act
        var decryptedData = CrossPlatformEncryption.Unprotect(encryptedData);

        // Assert
        decryptedData.Should().NotBeNull();
        decryptedData.Should().Equal(originalData);
    }

    [Fact]
    public void ProtectUnprotect_WithEntropy_ShouldWorkCorrectly()
    {
        // Arrange
        var originalData = Encoding.UTF8.GetBytes("Test sensitive data with entropy");
        var entropy = "MyCustomEntropy123!";

        // Act
        var encryptedData = CrossPlatformEncryption.Protect(originalData, entropy);
        var decryptedData = CrossPlatformEncryption.Unprotect(encryptedData, entropy);

        // Assert
        decryptedData.Should().Equal(originalData);
    }

    [Fact]
    public void Unprotect_WithWrongEntropy_ShouldThrow()
    {
        // Arrange
        var originalData = Encoding.UTF8.GetBytes("Test sensitive data");
        var correctEntropy = "CorrectEntropy123!";
        var wrongEntropy = "WrongEntropy456!";
        
        var encryptedData = CrossPlatformEncryption.Protect(originalData, correctEntropy);

        // Act & Assert
        var action = () => CrossPlatformEncryption.Unprotect(encryptedData, wrongEntropy);
        action.Should().Throw<Exception>();
    }

    [Fact]
    public void Protect_WithNullData_ShouldThrow()
    {
        // Act & Assert
        var action = () => CrossPlatformEncryption.Protect(null!);
        action.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void Unprotect_WithNullData_ShouldThrow()
    {
        // Act & Assert
        var action = () => CrossPlatformEncryption.Unprotect(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Protect_WithEmptyData_ShouldWork()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();

        // Act
        var encryptedData = CrossPlatformEncryption.Protect(emptyData);
        var decryptedData = CrossPlatformEncryption.Unprotect(encryptedData);

        // Assert
        decryptedData.Should().Equal(emptyData);
    }

    [Fact]
    public void ProtectUnprotect_WithLargeData_ShouldWork()
    {
        // Arrange
        var largeData = new byte[1024 * 1024]; // 1MB
        new Random().NextBytes(largeData);

        // Act
        var encryptedData = CrossPlatformEncryption.Protect(largeData);
        var decryptedData = CrossPlatformEncryption.Unprotect(encryptedData);

        // Assert
        decryptedData.Should().Equal(largeData);
    }

    [Fact]
    public void Protect_SameDataMultipleTimes_ShouldProduceDifferentResults()
    {
        // Arrange
        var originalData = Encoding.UTF8.GetBytes("Test data for randomness");

        // Act
        var encrypted1 = CrossPlatformEncryption.Protect(originalData);
        var encrypted2 = CrossPlatformEncryption.Protect(originalData);

        // Assert
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On non-Windows platforms using AES, different IVs should produce different results
            encrypted1.Should().NotEqual(encrypted2);
        }
        
        // Both should decrypt to the same original data
        var decrypted1 = CrossPlatformEncryption.Unprotect(encrypted1);
        var decrypted2 = CrossPlatformEncryption.Unprotect(encrypted2);
        
        decrypted1.Should().Equal(originalData);
        decrypted2.Should().Equal(originalData);
    }

    [Fact]
    public void GetEncryptionMethod_ShouldReturnValidDescription()
    {
        // Act
        var method = CrossPlatformEncryption.GetEncryptionMethod();

        // Assert
        method.Should().NotBeNullOrEmpty();
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            method.Should().Contain("DPAPI");
        }
        else
        {
            method.Should().Contain("AES");
        }
    }

    [Fact]
    public void IsSecureEncryptionSupported_ShouldReturnTrue()
    {
        // Act
        var isSupported = CrossPlatformEncryption.IsSecureEncryptionSupported();

        // Assert
        isSupported.Should().BeTrue();
    }

    [Fact]
    public void ProtectUnprotect_WithSpecialCharacters_ShouldWork()
    {
        // Arrange
        var specialData = Encoding.UTF8.GetBytes("Special chars: émojis 🔒 and symbols ñáéíóú");

        // Act
        var encryptedData = CrossPlatformEncryption.Protect(specialData);
        var decryptedData = CrossPlatformEncryption.Unprotect(encryptedData);

        // Assert
        decryptedData.Should().Equal(specialData);
        Encoding.UTF8.GetString(decryptedData).Should().Be("Special chars: émojis 🔒 and symbols ñáéíóú");
    }

    [Fact]
    public void ProtectUnprotect_WithBinaryData_ShouldWork()
    {
        // Arrange
        var binaryData = new byte[] { 0x00, 0x01, 0xFF, 0x7F, 0x80, 0xAB, 0xCD, 0xEF };

        // Act
        var encryptedData = CrossPlatformEncryption.Protect(binaryData);
        var decryptedData = CrossPlatformEncryption.Unprotect(encryptedData);

        // Assert
        decryptedData.Should().Equal(binaryData);
    }

    [Fact]
    public void Unprotect_WithCorruptedData_ShouldThrowOrReturnGarbage()
    {
        // Arrange
        var originalData = Encoding.UTF8.GetBytes("Test data");
        var encryptedData = CrossPlatformEncryption.Protect(originalData);
        
        // Corrupt the encrypted data
        encryptedData[0] = (byte)(encryptedData[0] ^ 0xFF);

        // Act & Assert
        // The behavior may vary by platform - either throw or return corrupted data
        var action = () => CrossPlatformEncryption.Unprotect(encryptedData);
        
        try
        {
            var result = action();
            // If no exception, verify the result is different from original
            result.Should().NotEqual(originalData);
        }
        catch (Exception ex)
        {
            // Exception is also acceptable behavior for corrupted data
            ex.Should().NotBeNull();
        }
    }

    [Fact]
    public void ProtectUnprotect_Consistency_AcrossMultipleOperations()
    {
        // Arrange
        var testData = Encoding.UTF8.GetBytes("Consistency test data");
        const int iterations = 100;

        // Act & Assert
        for (int i = 0; i < iterations; i++)
        {
            var encrypted = CrossPlatformEncryption.Protect(testData);
            var decrypted = CrossPlatformEncryption.Unprotect(encrypted);
            
            decrypted.Should().Equal(testData, $"iteration {i} should produce consistent results");
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("simple")]
    [InlineData("Complex_Entropy_With_Numbers_123!@#")]
    [InlineData("🔐🗝️")]
    public void ProtectUnprotect_WithVariousEntropies_ShouldWork(string entropy)
    {
        // Arrange
        var originalData = Encoding.UTF8.GetBytes("Test data with various entropies");

        // Act
        var encryptedData = CrossPlatformEncryption.Protect(originalData, entropy);
        var decryptedData = CrossPlatformEncryption.Unprotect(encryptedData, entropy);

        // Assert
        decryptedData.Should().Equal(originalData);
    }
}