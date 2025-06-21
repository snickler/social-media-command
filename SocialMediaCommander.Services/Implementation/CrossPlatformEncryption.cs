using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// Cross-platform encryption service that uses the best available encryption for each platform
/// </summary>
public static class CrossPlatformEncryption
{
    /// <summary>
    /// Encrypts data using platform-specific encryption
    /// </summary>
    public static byte[] Protect(byte[] data, string? optionalEntropy = null)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Use Windows DPAPI
            var entropy = optionalEntropy != null ? Encoding.UTF8.GetBytes(optionalEntropy) : null;
            return ProtectedData.Protect(data, entropy, DataProtectionScope.CurrentUser);
        }
        else
        {
            // Use AES encryption for non-Windows platforms
            return ProtectWithAes(data, optionalEntropy);
        }
    }

    /// <summary>
    /// Decrypts data using platform-specific decryption
    /// </summary>
    public static byte[] Unprotect(byte[] encryptedData, string? optionalEntropy = null)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Use Windows DPAPI
            var entropy = optionalEntropy != null ? Encoding.UTF8.GetBytes(optionalEntropy) : null;
            return ProtectedData.Unprotect(encryptedData, entropy, DataProtectionScope.CurrentUser);
        }
        else
        {
            // Use AES decryption for non-Windows platforms
            return UnprotectWithAes(encryptedData, optionalEntropy);
        }
    }

    private static byte[] ProtectWithAes(byte[] data, string? optionalEntropy)
    {
        // Generate a key based on machine and user characteristics
        var key = GenerateMachineUserKey(optionalEntropy);
        
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        var encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);
        
        // Prepend IV to encrypted data
        var result = new byte[aes.IV.Length + encryptedData.Length];
        Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
        Array.Copy(encryptedData, 0, result, aes.IV.Length, encryptedData.Length);
        
        return result;
    }

    private static byte[] UnprotectWithAes(byte[] encryptedData, string? optionalEntropy)
    {
        // Generate the same key used for encryption
        var key = GenerateMachineUserKey(optionalEntropy);
        
        using var aes = Aes.Create();
        aes.Key = key;
        
        // Extract IV from the beginning of encrypted data
        var iv = new byte[aes.IV.Length];
        Array.Copy(encryptedData, 0, iv, 0, iv.Length);
        aes.IV = iv;
        
        // Extract actual encrypted data
        var actualEncryptedData = new byte[encryptedData.Length - iv.Length];
        Array.Copy(encryptedData, iv.Length, actualEncryptedData, 0, actualEncryptedData.Length);
        
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(actualEncryptedData, 0, actualEncryptedData.Length);
    }

    private static byte[] GenerateMachineUserKey(string? optionalEntropy)
    {
        // Create a deterministic key based on machine and user characteristics
        var keyMaterial = new StringBuilder();
        
        // Add machine identifier
        keyMaterial.Append(Environment.MachineName);
        
        // Add user identifier
        keyMaterial.Append(Environment.UserName);
        
        // Add OS information
        keyMaterial.Append(Environment.OSVersion.ToString());
        
        // Add optional entropy if provided
        if (!string.IsNullOrEmpty(optionalEntropy))
        {
            keyMaterial.Append(optionalEntropy);
        }
        
        // Add a salt to make the key more secure
        keyMaterial.Append("SocialMediaCommander_SecureStorage_v1.0");
        
        // Generate SHA-256 hash as the key
        using var sha256 = SHA256.Create();
        var keyBytes = Encoding.UTF8.GetBytes(keyMaterial.ToString());
        return sha256.ComputeHash(keyBytes);
    }
    
    /// <summary>
    /// Gets a description of the encryption method being used
    /// </summary>
    public static string GetEncryptionMethod()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "Windows DPAPI (Data Protection API)";
        }
        else
        {
            return "AES-256 with machine/user-specific key derivation";
        }
    }
    
    /// <summary>
    /// Checks if the current platform supports secure encryption
    /// </summary>
    public static bool IsSecureEncryptionSupported()
    {
        return true; // Both Windows DPAPI and AES are considered secure
    }
} 