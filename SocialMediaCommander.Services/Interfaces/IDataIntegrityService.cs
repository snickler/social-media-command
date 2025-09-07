using System.Threading.Tasks;
using SocialMediaCommander.Services.Implementation;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Service interface for validating data integrity and detecting corruption
/// </summary>
public interface IDataIntegrityService
{
    /// <summary>
    /// Validates the integrity of all stored data
    /// </summary>
    /// <returns>Detailed integrity report</returns>
    Task<DataIntegrityReport> ValidateDataIntegrityAsync();

    /// <summary>
    /// Attempts to repair data integrity issues
    /// </summary>
    /// <param name="report">Integrity report containing issues to repair</param>
    /// <returns>True if any repairs were made</returns>
    Task<bool> RepairDataAsync(DataIntegrityReport report);

    /// <summary>
    /// Updates integrity checksums for all data files
    /// </summary>
    Task UpdateIntegrityChecksumsAsync();
}