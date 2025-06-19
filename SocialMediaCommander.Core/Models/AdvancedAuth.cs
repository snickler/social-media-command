using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SocialMediaCommander.Core.Models;

/// <summary>
/// Advanced authentication and identity management
/// </summary>
public class AdvancedUserProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    public string? FirstName { get; set; }
    
    public string? LastName { get; set; }
    
    public string? Avatar { get; set; }
    
    public string? PhoneNumber { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    public bool EmailVerified { get; set; } = false;
    
    public bool PhoneVerified { get; set; } = false;
    
    public List<string> Roles { get; set; } = new();
    
    public List<Claim> Claims { get; set; } = new();
    
    public UserPreferences Preferences { get; set; } = new();
    
    public SecuritySettings Security { get; set; } = new();
    
    public List<UserSession> Sessions { get; set; } = new();
    
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class UserPreferences
{
    public string Theme { get; set; } = "Auto";
    
    public string Language { get; set; } = "en-US";
    
    public string TimeZone { get; set; } = "UTC";
    
    public NotificationSettings Notifications { get; set; } = new();
    
    public PrivacySettings Privacy { get; set; } = new();
    
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

public class NotificationSettings
{
    public bool EmailNotifications { get; set; } = true;
    
    public bool PushNotifications { get; set; } = true;
    
    public bool DesktopNotifications { get; set; } = true;
    
    public bool PostPublished { get; set; } = true;
    
    public bool ScheduledPosts { get; set; } = true;
    
    public bool AccountIssues { get; set; } = true;
    
    public bool SecurityAlerts { get; set; } = true;
    
    public bool WeeklyReports { get; set; } = false;
    
    public string NotificationFrequency { get; set; } = "Immediate";
}

public class PrivacySettings
{
    public bool ProfileVisible { get; set; } = false;
    
    public bool ShareAnalytics { get; set; } = false;
    
    public bool AllowDataCollection { get; set; } = false;
    
    public List<string> BlockedDomains { get; set; } = new();
    
    public string DataRetentionPeriod { get; set; } = "1 Year";
}

public class SecuritySettings
{
    public bool TwoFactorEnabled { get; set; } = false;
    
    public string? TwoFactorSecret { get; set; }
    
    public List<string> BackupCodes { get; set; } = new();
    
    public bool BiometricEnabled { get; set; } = false;
    
    public List<TrustedDevice> TrustedDevices { get; set; } = new();
    
    public List<SecurityEvent> SecurityEvents { get; set; } = new();
    
    public string PasswordPolicy { get; set; } = "Strong";
    
    public DateTime? LastPasswordChange { get; set; }
    
    public int FailedLoginAttempts { get; set; } = 0;
    
    public DateTime? LockoutUntil { get; set; }
}

public class TrustedDevice
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string DeviceName { get; set; } = string.Empty;
    
    public string DeviceType { get; set; } = string.Empty;
    
    public string UserAgent { get; set; } = string.Empty;
    
    public string IpAddress { get; set; } = string.Empty;
    
    public DateTime TrustedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
}

public class SecurityEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public SecurityEventType EventType { get; set; }
    
    public string Description { get; set; } = string.Empty;
    
    public string IpAddress { get; set; } = string.Empty;
    
    public string UserAgent { get; set; } = string.Empty;
    
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    
    public SecurityEventSeverity Severity { get; set; } = SecurityEventSeverity.Low;
    
    public Dictionary<string, object> Details { get; set; } = new();
}

public class UserSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string UserId { get; set; } = string.Empty;
    
    public string SessionToken { get; set; } = string.Empty;
    
    public string RefreshToken { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
    
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    
    public string IpAddress { get; set; } = string.Empty;
    
    public string UserAgent { get; set; } = string.Empty;
    
    public string DeviceId { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public Dictionary<string, object> SessionData { get; set; } = new();
}

/// <summary>
/// OAuth2/OpenID Connect client configuration
/// </summary>
public class OAuthClient
{
    public string ClientId { get; set; } = string.Empty;
    
    public string ClientName { get; set; } = string.Empty;
    
    public string ClientSecret { get; set; } = string.Empty;
    
    public List<string> AllowedGrantTypes { get; set; } = new();
    
    public List<string> AllowedScopes { get; set; } = new();
    
    public List<string> RedirectUris { get; set; } = new();
    
    public List<string> PostLogoutRedirectUris { get; set; } = new();
    
    public List<string> AllowedCorsOrigins { get; set; } = new();
    
    public bool RequireConsent { get; set; } = false;
    
    public bool AllowRememberConsent { get; set; } = true;
    
    public int AccessTokenLifetime { get; set; } = 3600;
    
    public int RefreshTokenLifetime { get; set; } = 86400;
    
    public bool RequirePkce { get; set; } = true;
    
    public bool AllowOfflineAccess { get; set; } = true;
}

/// <summary>
/// Advanced authentication request
/// </summary>
public class AdvancedAuthRequest
{
    public string Username { get; set; } = string.Empty;
    
    public string Password { get; set; } = string.Empty;
    
    public string? TwoFactorCode { get; set; }
    
    public string? BiometricData { get; set; }
    
    public bool RememberMe { get; set; } = false;
    
    public string? DeviceId { get; set; }
    
    public string? DeviceName { get; set; }
    
    public string ClientId { get; set; } = string.Empty;
    
    public List<string> RequestedScopes { get; set; } = new();
    
    public string? RedirectUri { get; set; }
    
    public string? State { get; set; }
    
    public string? CodeChallenge { get; set; }
    
    public string? CodeChallengeMethod { get; set; }
}

public class AdvancedAuthResponse
{
    public bool Success { get; set; }
    
    public string? ErrorCode { get; set; }
    
    public string? ErrorDescription { get; set; }
    
    public string? AccessToken { get; set; }
    
    public string? RefreshToken { get; set; }
    
    public string? IdToken { get; set; }
    
    public int ExpiresIn { get; set; }
    
    public string? TokenType { get; set; } = "Bearer";
    
    public List<string> Scopes { get; set; } = new();
    
    public AdvancedUserProfile? UserProfile { get; set; }
    
    public bool RequiresTwoFactor { get; set; }
    
    public bool RequiresBiometric { get; set; }
    
    public List<string> AvailableTwoFactorMethods { get; set; } = new();
}

/// <summary>
/// Role-based access control
/// </summary>
public class Role
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Name { get; set; } = string.Empty;
    
    public string DisplayName { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public List<Permission> Permissions { get; set; } = new();
    
    public bool IsSystemRole { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Permission
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Name { get; set; } = string.Empty;
    
    public string Resource { get; set; } = string.Empty;
    
    public string Action { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public PermissionType Type { get; set; } = PermissionType.Allow;
}

public enum SecurityEventType
{
    Login,
    Logout,
    LoginFailed,
    PasswordChanged,
    TwoFactorEnabled,
    TwoFactorDisabled,
    BiometricEnabled,
    BiometricDisabled,
    DeviceTrusted,
    DeviceUntrusted,
    SuspiciousActivity,
    AccountLocked,
    AccountUnlocked,
    DataExport,
    DataDeletion,
    PermissionChanged
}

public enum SecurityEventSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum PermissionType
{
    Allow,
    Deny
}

/// <summary>
/// Advanced authentication configuration
/// </summary>
public class AdvancedAuthConfig
{
    public string Authority { get; set; } = "https://localhost:5001";
    
    public string ClientId { get; set; } = string.Empty;
    
    public string ClientSecret { get; set; } = string.Empty;
    
    public List<string> Scopes { get; set; } = new() { "openid", "profile", "email" };
    
    public string RedirectUri { get; set; } = "https://localhost:3000/callback";
    
    public string PostLogoutRedirectUri { get; set; } = "https://localhost:3000";
    
    public bool RequireHttps { get; set; } = true;
    
    public bool ValidateIssuer { get; set; } = true;
    
    public bool ValidateAudience { get; set; } = true;
    
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
    
    public Dictionary<string, object> AdditionalParameters { get; set; } = new();
} 