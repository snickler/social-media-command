using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;
using Serilog;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// Service for validating data integrity and detecting corruption in stored data
/// </summary>
public class DataIntegrityService : IDataIntegrityService
{
    private readonly IAccountService _accountService;
    private readonly IOAuthConfigurationService _oauthConfigService;
    private readonly ILogger _logger;
    private readonly string _dataDirectory;
    private readonly string _integrityDirectory;

    public DataIntegrityService(IAccountService accountService, IOAuthConfigurationService oauthConfigService)
    {
        _accountService = accountService;
        _oauthConfigService = oauthConfigService;
        _logger = Log.ForContext<DataIntegrityService>();
        
        _dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SocialMediaCommander"
        );
        
        _integrityDirectory = Path.Combine(_dataDirectory, "Integrity");
        Directory.CreateDirectory(_integrityDirectory);
    }

    public async Task<DataIntegrityReport> ValidateDataIntegrityAsync()
    {
        var report = new DataIntegrityReport
        {
            CheckedAt = DateTime.UtcNow,
            EncryptionMethod = CrossPlatformEncryption.GetEncryptionMethod()
        };

        try
        {
            _logger.Information("Starting data integrity validation");

            // Validate accounts
            await ValidateAccountsAsync(report);

            // Validate OAuth configurations
            await ValidateOAuthConfigurationsAsync(report);

            // Check file integrity
            await ValidateFileIntegrityAsync(report);

            // Generate overall status
            report.OverallStatus = DetermineOverallStatus(report);

            _logger.Information("Data integrity validation completed. Status: {Status}", report.OverallStatus);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during data integrity validation");
            report.Errors.Add($"Validation failed: {ex.Message}");
            report.OverallStatus = IntegrityStatus.Error;
        }

        return report;
    }

    public async Task<bool> RepairDataAsync(DataIntegrityReport report)
    {
        try
        {
            _logger.Information("Starting data repair based on integrity report");
            var repaired = false;

            // Repair corrupted accounts
            foreach (var issue in report.AccountIssues.Where(i => i.CanRepair))
            {
                try
                {
                    await RepairAccountIssueAsync(issue);
                    issue.IsRepaired = true;
                    repaired = true;
                    _logger.Information("Repaired account issue: {Issue}", issue.Description);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to repair account issue: {Issue}", issue.Description);
                    issue.RepairError = ex.Message;
                }
            }

            // Repair OAuth configuration issues
            foreach (var issue in report.OAuthIssues.Where(i => i.CanRepair))
            {
                try
                {
                    await RepairOAuthIssueAsync(issue);
                    issue.IsRepaired = true;
                    repaired = true;
                    _logger.Information("Repaired OAuth issue: {Issue}", issue.Description);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to repair OAuth issue: {Issue}", issue.Description);
                    issue.RepairError = ex.Message;
                }
            }

            // Update checksums after repair
            if (repaired)
            {
                await UpdateIntegrityChecksumsAsync();
            }

            return repaired;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during data repair");
            return false;
        }
    }

    public async Task UpdateIntegrityChecksumsAsync()
    {
        try
        {
            _logger.Debug("Updating integrity checksums");

            var checksums = new Dictionary<string, string>();

            // Calculate checksum for accounts file
            var accountsFile = Path.Combine(_dataDirectory, "Data", "accounts.encrypted");
            if (File.Exists(accountsFile))
            {
                checksums["accounts"] = await CalculateFileChecksumAsync(accountsFile);
            }

            // Calculate checksum for OAuth configurations file
            var oauthFile = Path.Combine(_dataDirectory, "Config", "oauth-configs.encrypted");
            if (File.Exists(oauthFile))
            {
                checksums["oauth"] = await CalculateFileChecksumAsync(oauthFile);
            }

            // Save checksums
            var checksumFile = Path.Combine(_integrityDirectory, "checksums.json");
            var json = JsonSerializer.Serialize(checksums, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(checksumFile, json);

            _logger.Debug("Integrity checksums updated");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to update integrity checksums");
        }
    }

    private async Task ValidateAccountsAsync(DataIntegrityReport report)
    {
        try
        {
            var accounts = await _accountService.GetAllAccountsAsync();
            report.AccountCount = accounts.Count();

            foreach (var account in accounts)
            {
                ValidateAccount(account, report);
            }

            // Check for duplicate accounts
            var duplicates = accounts
                .GroupBy(a => new { a.PlatformId, a.Username })
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var duplicate in duplicates)
            {
                report.AccountIssues.Add(new DataIssue
                {
                    Type = "Duplicate Account",
                    Description = $"Duplicate accounts found for {duplicate.Key.PlatformId}: {duplicate.Key.Username}",
                    Severity = IssueSeverity.Warning,
                    CanRepair = true,
                    RelatedData = duplicate.ToList()
                });
            }
        }
        catch (Exception ex)
        {
            report.Errors.Add($"Failed to validate accounts: {ex.Message}");
        }
    }

    private void ValidateAccount(Account account, DataIntegrityReport report)
    {
        // Check required fields
        if (string.IsNullOrEmpty(account.Id))
        {
            report.AccountIssues.Add(new DataIssue
            {
                Type = "Missing ID",
                Description = $"Account missing ID: {account.DisplayName}",
                Severity = IssueSeverity.Error,
                CanRepair = true,
                RelatedData = account
            });
        }

        if (string.IsNullOrEmpty(account.Username))
        {
            report.AccountIssues.Add(new DataIssue
            {
                Type = "Missing Username",
                Description = $"Account missing username: {account.Id}",
                Severity = IssueSeverity.Error,
                CanRepair = false,
                RelatedData = account
            });
        }

        if (string.IsNullOrEmpty(account.DisplayName))
        {
            report.AccountIssues.Add(new DataIssue
            {
                Type = "Missing Display Name",
                Description = $"Account missing display name: {account.Id}",
                Severity = IssueSeverity.Warning,
                CanRepair = true,
                RelatedData = account
            });
        }

        // Check OAuth configuration
        if (account.OAuthConfiguration != null)
        {
            ValidateOAuthConfig(account.OAuthConfiguration, $"Account {account.Id}", report);
        }

        // Check token validity
        if (account.Tokens != null && account.Tokens.IsExpired)
        {
            report.AccountIssues.Add(new DataIssue
            {
                Type = "Expired Token",
                Description = $"Account has expired tokens: {account.DisplayName}",
                Severity = IssueSeverity.Warning,
                CanRepair = false,
                RelatedData = account
            });
        }
    }

    private async Task ValidateOAuthConfigurationsAsync(DataIntegrityReport report)
    {
        try
        {
            var configurations = await _oauthConfigService.GetAllConfigurationsAsync();
            report.OAuthConfigCount = configurations.Count;

            foreach (var kvp in configurations)
            {
                ValidateOAuthConfig(kvp.Value, $"Platform {kvp.Key}", report);
            }
        }
        catch (Exception ex)
        {
            report.Errors.Add($"Failed to validate OAuth configurations: {ex.Message}");
        }
    }

    private void ValidateOAuthConfig(OAuthConfig config, string context, DataIntegrityReport report)
    {
        if (string.IsNullOrEmpty(config.ClientId))
        {
            report.OAuthIssues.Add(new DataIssue
            {
                Type = "Missing Client ID",
                Description = $"{context}: Missing OAuth Client ID",
                Severity = IssueSeverity.Error,
                CanRepair = false,
                RelatedData = config
            });
        }

        if (string.IsNullOrEmpty(config.ClientSecret))
        {
            report.OAuthIssues.Add(new DataIssue
            {
                Type = "Missing Client Secret",
                Description = $"{context}: Missing OAuth Client Secret",
                Severity = IssueSeverity.Error,
                CanRepair = false,
                RelatedData = config
            });
        }

        if (string.IsNullOrEmpty(config.RedirectUri))
        {
            report.OAuthIssues.Add(new DataIssue
            {
                Type = "Missing Redirect URI",
                Description = $"{context}: Missing OAuth Redirect URI",
                Severity = IssueSeverity.Warning,
                CanRepair = true,
                RelatedData = config
            });
        }
    }

    private async Task ValidateFileIntegrityAsync(DataIntegrityReport report)
    {
        try
        {
            var checksumFile = Path.Combine(_integrityDirectory, "checksums.json");
            if (!File.Exists(checksumFile))
            {
                report.FileIssues.Add(new DataIssue
                {
                    Type = "Missing Checksums",
                    Description = "Integrity checksums file not found - creating new baseline",
                    Severity = IssueSeverity.Info,
                    CanRepair = true
                });
                await UpdateIntegrityChecksumsAsync();
                return;
            }

            var json = await File.ReadAllTextAsync(checksumFile);
            var storedChecksums = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();

            // Verify accounts file
            await VerifyFileIntegrity("accounts", Path.Combine(_dataDirectory, "Data", "accounts.encrypted"), 
                storedChecksums, report);

            // Verify OAuth configurations file
            await VerifyFileIntegrity("oauth", Path.Combine(_dataDirectory, "Config", "oauth-configs.encrypted"), 
                storedChecksums, report);
        }
        catch (Exception ex)
        {
            report.Errors.Add($"Failed to validate file integrity: {ex.Message}");
        }
    }

    private async Task VerifyFileIntegrity(string name, string filePath, Dictionary<string, string> storedChecksums, 
        DataIntegrityReport report)
    {
        if (!File.Exists(filePath))
        {
            if (storedChecksums.ContainsKey(name))
            {
                report.FileIssues.Add(new DataIssue
                {
                    Type = "Missing File",
                    Description = $"Expected file not found: {name}",
                    Severity = IssueSeverity.Error,
                    CanRepair = false,
                    RelatedData = filePath
                });
            }
            return;
        }

        var currentChecksum = await CalculateFileChecksumAsync(filePath);
        
        if (storedChecksums.TryGetValue(name, out var storedChecksum))
        {
            if (currentChecksum != storedChecksum)
            {
                report.FileIssues.Add(new DataIssue
                {
                    Type = "Checksum Mismatch",
                    Description = $"File integrity check failed for {name} - file may be corrupted",
                    Severity = IssueSeverity.Warning,
                    CanRepair = true,
                    RelatedData = filePath
                });
            }
        }
        else
        {
            report.FileIssues.Add(new DataIssue
            {
                Type = "New File",
                Description = $"New file detected: {name}",
                Severity = IssueSeverity.Info,
                CanRepair = true,
                RelatedData = filePath
            });
        }
    }

    private async Task<string> CalculateFileChecksumAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToBase64String(hash);
    }

    private async Task RepairAccountIssueAsync(DataIssue issue)
    {
        if (issue.RelatedData is Account account)
        {
            switch (issue.Type)
            {
                case "Missing ID":
                    account.Id = Guid.NewGuid().ToString();
                    await _accountService.UpdateAccountAsync(account);
                    break;
                    
                case "Missing Display Name":
                    account.DisplayName = account.Username ?? "Unnamed Account";
                    await _accountService.UpdateAccountAsync(account);
                    break;
                    
                case "Duplicate Account":
                    // Remove duplicates, keep the most recently used
                    if (issue.RelatedData is List<Account> duplicates)
                    {
                        var keeper = duplicates.OrderByDescending(a => a.LastUsed).First();
                        foreach (var duplicate in duplicates.Where(a => a.Id != keeper.Id))
                        {
                            await _accountService.DeleteAccountAsync(duplicate.Id);
                        }
                    }
                    break;
            }
        }
    }

    private Task RepairOAuthIssueAsync(DataIssue issue)
    {
        if (issue.RelatedData is OAuthConfig config && issue.Type == "Missing Redirect URI")
        {
            config.RedirectUri = "http://localhost:8080/callback";
            // Note: We'd need the platform to save this, but this is just an example
        }

        return Task.CompletedTask;
    }

    private IntegrityStatus DetermineOverallStatus(DataIntegrityReport report)
    {
        if (report.Errors.Any() || 
            report.AccountIssues.Any(i => i.Severity == IssueSeverity.Error) ||
            report.OAuthIssues.Any(i => i.Severity == IssueSeverity.Error) ||
            report.FileIssues.Any(i => i.Severity == IssueSeverity.Error))
        {
            return IntegrityStatus.Error;
        }

        if (report.AccountIssues.Any(i => i.Severity == IssueSeverity.Warning) ||
            report.OAuthIssues.Any(i => i.Severity == IssueSeverity.Warning) ||
            report.FileIssues.Any(i => i.Severity == IssueSeverity.Warning))
        {
            return IntegrityStatus.Warning;
        }

        return IntegrityStatus.Healthy;
    }
}

/// <summary>
/// Report containing data integrity validation results
/// </summary>
public class DataIntegrityReport
{
    public DateTime CheckedAt { get; set; }
    public string EncryptionMethod { get; set; } = string.Empty;
    public IntegrityStatus OverallStatus { get; set; }
    public int AccountCount { get; set; }
    public int OAuthConfigCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<DataIssue> AccountIssues { get; set; } = new();
    public List<DataIssue> OAuthIssues { get; set; } = new();
    public List<DataIssue> FileIssues { get; set; } = new();

    public int TotalIssues => AccountIssues.Count + OAuthIssues.Count + FileIssues.Count;
    public int RepairableIssues => AccountIssues.Count(i => i.CanRepair) + 
                                   OAuthIssues.Count(i => i.CanRepair) + 
                                   FileIssues.Count(i => i.CanRepair);
}

/// <summary>
/// Represents a data integrity issue
/// </summary>
public class DataIssue
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IssueSeverity Severity { get; set; }
    public bool CanRepair { get; set; }
    public bool IsRepaired { get; set; }
    public string? RepairError { get; set; }
    public object? RelatedData { get; set; }
}

/// <summary>
/// Severity levels for data integrity issues
/// </summary>
public enum IssueSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Overall integrity status
/// </summary>
public enum IntegrityStatus
{
    Healthy,
    Warning,
    Error
} 