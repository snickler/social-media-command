using System;
using System.Collections.Generic;

namespace SocialMediaCommander.Core.Models;

/// <summary>
/// Data structure for backup files
/// </summary>
public class BackupData
{
    public DateTime CreatedAt { get; set; }
    public string Version { get; set; } = string.Empty;
    public string EncryptionMethod { get; set; } = string.Empty;
    public List<Account> Accounts { get; set; } = new();
    public Dictionary<SocialPlatform, OAuthConfig> OAuthConfigurations { get; set; } = new();
}

/// <summary>
/// Information about available backup files
/// </summary>
public class BackupInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long Size { get; set; }
    public string Version { get; set; } = string.Empty;
    public string EncryptionMethod { get; set; } = string.Empty;
    public int AccountCount { get; set; }
    public int OAuthConfigCount { get; set; }
    public bool IsCorrupted { get; set; }

    public string FormattedSize => FormatBytes(Size);

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }
}