using System.Collections.Generic;
using System.Threading.Tasks;
using SocialMediaCommander.Services.Implementation;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Service interface for creating and managing encrypted backups of account and OAuth configuration data
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates an encrypted backup of all account and OAuth configuration data
    /// </summary>
    /// <param name="customName">Optional custom name for the backup file</param>
    /// <returns>Path to the created backup file</returns>
    Task<string> CreateBackupAsync(string? customName = null);

    /// <summary>
    /// Restores account and OAuth configuration data from an encrypted backup
    /// </summary>
    /// <param name="backupPath">Path to the backup file</param>
    Task RestoreFromBackupAsync(string backupPath);

    /// <summary>
    /// Gets information about all available backup files
    /// </summary>
    /// <returns>Collection of backup information</returns>
    Task<IEnumerable<BackupInfo>> GetAvailableBackupsAsync();

    /// <summary>
    /// Deletes a backup file
    /// </summary>
    /// <param name="backupPath">Path to the backup file to delete</param>
    Task DeleteBackupAsync(string backupPath);

    /// <summary>
    /// Gets metadata from a backup file without fully restoring it
    /// </summary>
    /// <param name="backupPath">Path to the backup file</param>
    /// <returns>Backup metadata</returns>
    Task<BackupData> GetBackupMetadataAsync(string backupPath);
}