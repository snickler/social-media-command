using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;
using Serilog;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// Service for creating and restoring encrypted backups of account and OAuth configuration data
/// </summary>
public class BackupService : IBackupService
{
    private readonly IAccountService _accountService;
    private readonly IOAuthConfigurationService _oauthConfigService;
    private readonly ILogger _logger;
    private readonly string _backupDirectory;

    public BackupService(IAccountService accountService, IOAuthConfigurationService oauthConfigService)
    {
        _accountService = accountService;
        _oauthConfigService = oauthConfigService;
        _logger = Log.ForContext<BackupService>();
        
        _backupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SocialMediaCommander",
            "Backups"
        );
        
        Directory.CreateDirectory(_backupDirectory);
    }

    public async Task<string> CreateBackupAsync(string? customName = null)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupName = customName ?? $"SocialMediaCommander_Backup_{timestamp}";
            var backupFileName = $"{backupName}.smcbackup";
            var backupPath = Path.Combine(_backupDirectory, backupFileName);

            _logger.Information("Creating backup: {BackupPath}", backupPath);

            // Gather all data to backup
            var backupData = new BackupData
            {
                CreatedAt = DateTime.UtcNow,
                Version = "1.0",
                EncryptionMethod = CrossPlatformEncryption.GetEncryptionMethod(),
                Accounts = (await _accountService.GetAllAccountsAsync()).ToList(),
                OAuthConfigurations = await _oauthConfigService.GetAllConfigurationsAsync()
            };

            // Serialize to JSON
            var json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Compress and encrypt
            var compressedData = await CompressDataAsync(json);
            var encryptedData = CrossPlatformEncryption.Protect(compressedData, "SocialMediaCommander_Backup");

            // Write to file
            await File.WriteAllBytesAsync(backupPath, encryptedData);

            _logger.Information("Backup created successfully: {BackupPath} ({Size} bytes)", 
                backupPath, encryptedData.Length);

            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create backup");
            throw new InvalidOperationException($"Failed to create backup: {ex.Message}", ex);
        }
    }

    public async Task RestoreFromBackupAsync(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath))
                throw new FileNotFoundException($"Backup file not found: {backupPath}");

            _logger.Information("Restoring from backup: {BackupPath}", backupPath);

            // Read and decrypt
            var encryptedData = await File.ReadAllBytesAsync(backupPath);
            var compressedData = CrossPlatformEncryption.Unprotect(encryptedData, "SocialMediaCommander_Backup");
            var json = await DecompressDataAsync(compressedData);

            // Deserialize
            var backupData = JsonSerializer.Deserialize<BackupData>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (backupData == null)
                throw new InvalidOperationException("Invalid backup data");

            _logger.Information("Backup contains {AccountCount} accounts and {OAuthCount} OAuth configurations",
                backupData.Accounts.Count, backupData.OAuthConfigurations.Count);

            // Restore accounts
            foreach (var account in backupData.Accounts)
            {
                try
                {
                    // Check if account already exists
                    var existingAccount = await _accountService.GetAccountByIdAsync(account.Id);
                    if (existingAccount != null)
                    {
                        await _accountService.UpdateAccountAsync(account);
                        _logger.Debug("Updated existing account: {AccountId}", account.Id);
                    }
                    else
                    {
                        await _accountService.CreateAccountAsync(account);
                        _logger.Debug("Created new account: {AccountId}", account.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to restore account {AccountId}: {Error}", account.Id, ex.Message);
                }
            }

            // Restore OAuth configurations
            foreach (var kvp in backupData.OAuthConfigurations)
            {
                try
                {
                    await _oauthConfigService.SaveConfigurationAsync(kvp.Key, kvp.Value);
                    _logger.Debug("Restored OAuth configuration for platform: {Platform}", kvp.Key);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to restore OAuth configuration for {Platform}: {Error}", 
                        kvp.Key, ex.Message);
                }
            }

            _logger.Information("Backup restoration completed successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to restore from backup: {BackupPath}", backupPath);
            throw new InvalidOperationException($"Failed to restore from backup: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<BackupInfo>> GetAvailableBackupsAsync()
    {
        try
        {
            var backups = new List<BackupInfo>();
            var backupFiles = Directory.GetFiles(_backupDirectory, "*.smcbackup");

            foreach (var file in backupFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    var backupInfo = new BackupInfo
                    {
                        FilePath = file,
                        FileName = Path.GetFileNameWithoutExtension(file),
                        CreatedAt = fileInfo.CreationTime,
                        Size = fileInfo.Length
                    };

                    // Try to read backup metadata without fully restoring
                    try
                    {
                        var metadata = await GetBackupMetadataAsync(file);
                        backupInfo.Version = metadata.Version;
                        backupInfo.EncryptionMethod = metadata.EncryptionMethod;
                        backupInfo.AccountCount = metadata.Accounts.Count;
                        backupInfo.OAuthConfigCount = metadata.OAuthConfigurations.Count;
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug(ex, "Could not read metadata for backup: {File}", file);
                        backupInfo.IsCorrupted = true;
                    }

                    backups.Add(backupInfo);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Error processing backup file: {File}", file);
                }
            }

            return backups.OrderByDescending(b => b.CreatedAt);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get available backups");
            return new List<BackupInfo>();
        }
    }

    public Task DeleteBackupAsync(string backupPath)
    {
        try
        {
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
                _logger.Information("Deleted backup: {BackupPath}", backupPath);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete backup: {BackupPath}", backupPath);
            throw;
        }
    }

    public async Task<BackupData> GetBackupMetadataAsync(string backupPath)
    {
        var encryptedData = await File.ReadAllBytesAsync(backupPath);
        var compressedData = CrossPlatformEncryption.Unprotect(encryptedData, "SocialMediaCommander_Backup");
        var json = await DecompressDataAsync(compressedData);
        
        var backupData = JsonSerializer.Deserialize<BackupData>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return backupData ?? throw new InvalidOperationException("Invalid backup data");
    }

    private async Task<byte[]> CompressDataAsync(string data)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(data);
        
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress))
        {
            await gzip.WriteAsync(bytes, 0, bytes.Length);
        }
        
        return output.ToArray();
    }

    private async Task<string> DecompressDataAsync(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        
        await gzip.CopyToAsync(output);
        return System.Text.Encoding.UTF8.GetString(output.ToArray());
    }
}

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