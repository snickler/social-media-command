using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Advanced authentication service interface using Duende IdentityServer
/// </summary>
public interface IAdvancedAuthService
{
    /// <summary>
    /// Authenticate user with advanced security features
    /// </summary>
    Task<AdvancedAuthResponse> AuthenticateAsync(AdvancedAuthRequest request);

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    Task<AdvancedAuthResponse> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Logout user and revoke tokens
    /// </summary>
    Task<bool> LogoutAsync(string userId, string? sessionId = null);

    /// <summary>
    /// Enable two-factor authentication
    /// </summary>
    Task<string> EnableTwoFactorAsync(string userId);

    /// <summary>
    /// Verify two-factor authentication code
    /// </summary>
    Task<bool> VerifyTwoFactorAsync(string userId, string code);

    /// <summary>
    /// Disable two-factor authentication
    /// </summary>
    Task<bool> DisableTwoFactorAsync(string userId, string code);

    /// <summary>
    /// Generate backup codes for two-factor authentication
    /// </summary>
    Task<List<string>> GenerateBackupCodesAsync(string userId);

    /// <summary>
    /// Enable biometric authentication
    /// </summary>
    Task<bool> EnableBiometricAsync(string userId, string biometricData);

    /// <summary>
    /// Verify biometric authentication
    /// </summary>
    Task<bool> VerifyBiometricAsync(string userId, string biometricData);

    /// <summary>
    /// Add trusted device
    /// </summary>
    Task<TrustedDevice> AddTrustedDeviceAsync(string userId, string deviceName, string deviceType, string userAgent, string ipAddress);

    /// <summary>
    /// Remove trusted device
    /// </summary>
    Task<bool> RemoveTrustedDeviceAsync(string userId, string deviceId);

    /// <summary>
    /// Get user's trusted devices
    /// </summary>
    Task<List<TrustedDevice>> GetTrustedDevicesAsync(string userId);

    /// <summary>
    /// Create user session
    /// </summary>
    Task<UserSession> CreateSessionAsync(string userId, string ipAddress, string userAgent, string deviceId);

    /// <summary>
    /// Get active sessions for user
    /// </summary>
    Task<List<UserSession>> GetActiveSessionsAsync(string userId);

    /// <summary>
    /// Terminate session
    /// </summary>
    Task<bool> TerminateSessionAsync(string sessionId);

    /// <summary>
    /// Terminate all sessions for user
    /// </summary>
    Task<bool> TerminateAllSessionsAsync(string userId);

    /// <summary>
    /// Get user profile with advanced information
    /// </summary>
    Task<AdvancedUserProfile?> GetUserProfileAsync(string userId);

    /// <summary>
    /// Update user profile
    /// </summary>
    Task<AdvancedUserProfile> UpdateUserProfileAsync(AdvancedUserProfile profile);

    /// <summary>
    /// Change user password
    /// </summary>
    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);

    /// <summary>
    /// Reset user password
    /// </summary>
    Task<bool> ResetPasswordAsync(string email);

    /// <summary>
    /// Verify email address
    /// </summary>
    Task<bool> VerifyEmailAsync(string userId, string verificationCode);

    /// <summary>
    /// Send email verification
    /// </summary>
    Task<bool> SendEmailVerificationAsync(string userId);

    /// <summary>
    /// Get user roles
    /// </summary>
    Task<List<Role>> GetUserRolesAsync(string userId);

    /// <summary>
    /// Assign role to user
    /// </summary>
    Task<bool> AssignRoleAsync(string userId, string roleId);

    /// <summary>
    /// Remove role from user
    /// </summary>
    Task<bool> RemoveRoleAsync(string userId, string roleId);

    /// <summary>
    /// Check user permission
    /// </summary>
    Task<bool> HasPermissionAsync(string userId, string resource, string action);

    /// <summary>
    /// Get security events for user
    /// </summary>
    Task<List<SecurityEvent>> GetSecurityEventsAsync(string userId, int limit = 50);

    /// <summary>
    /// Log security event
    /// </summary>
    Task LogSecurityEventAsync(string userId, SecurityEventType eventType, string description, string ipAddress, string userAgent, Dictionary<string, object>? details = null);

    /// <summary>
    /// Lock user account
    /// </summary>
    Task<bool> LockAccountAsync(string userId, TimeSpan? lockDuration = null);

    /// <summary>
    /// Unlock user account
    /// </summary>
    Task<bool> UnlockAccountAsync(string userId);

    /// <summary>
    /// Validate token
    /// </summary>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// Get token claims
    /// </summary>
    Task<Dictionary<string, object>> GetTokenClaimsAsync(string token);
}