using System;
using System.Collections.Generic;
using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Desktop.Helpers;

/// <summary>
/// Validates account form data and provides error messages
/// Extracted from AccountManagerViewModel to reduce cyclic complexity
/// </summary>
public class AccountFormValidator
{
    /// <summary>
    /// Validates account form data and returns validation result
    /// </summary>
    public AccountValidationResult ValidateAccount(
        string username,
        string displayName,
        string? oauthClientId = null,
        string? oauthClientSecret = null,
        string? oauthRedirectUri = null)
    {
        var errors = new List<string>();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(username))
        {
            errors.Add("Username is required");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            errors.Add("Display name is required");
        }

        // Validate username format
        if (!string.IsNullOrWhiteSpace(username))
        {
            if (username.Length < 3)
            {
                errors.Add("Username must be at least 3 characters long");
            }

            if (username.Length > 50)
            {
                errors.Add("Username cannot exceed 50 characters");
            }

            if (!IsValidUsername(username))
            {
                errors.Add("Username contains invalid characters. Only letters, numbers, underscore, and hyphen are allowed");
            }
        }

        // Validate display name
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            if (displayName.Length > 100)
            {
                errors.Add("Display name cannot exceed 100 characters");
            }
        }

        // Validate OAuth configuration if provided
        var oauthValidation = ValidateOAuthConfiguration(oauthClientId, oauthClientSecret, oauthRedirectUri);
        if (!oauthValidation.IsValid)
        {
            errors.AddRange(oauthValidation.Errors);
        }

        return new AccountValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            FormattedErrorMessage = errors.Count > 0 ? $"⚠️ {string.Join(", ", errors)}" : string.Empty
        };
    }

    /// <summary>
    /// Validates OAuth configuration parameters
    /// </summary>
    public OAuthValidationResult ValidateOAuthConfiguration(
        string? clientId,
        string? clientSecret,
        string? redirectUri = null)
    {
        var errors = new List<string>();

        // If any OAuth field is provided, all required fields must be provided
        var hasAnyOAuthField = !string.IsNullOrWhiteSpace(clientId) ||
                              !string.IsNullOrWhiteSpace(clientSecret) ||
                              !string.IsNullOrWhiteSpace(redirectUri);

        if (hasAnyOAuthField)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                errors.Add("OAuth Client ID is required when providing OAuth configuration");
            }

            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                errors.Add("OAuth Client Secret is required when providing OAuth configuration");
            }

            // Validate Client ID format
            if (!string.IsNullOrWhiteSpace(clientId) && clientId.Length < 10)
            {
                errors.Add("OAuth Client ID appears to be too short");
            }

            // Validate Client Secret format
            if (!string.IsNullOrWhiteSpace(clientSecret) && clientSecret.Length < 20)
            {
                errors.Add("OAuth Client Secret appears to be too short");
            }

            // Validate Redirect URI format if provided
            if (!string.IsNullOrWhiteSpace(redirectUri))
            {
                if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
                {
                    errors.Add("OAuth Redirect URI must be a valid URL");
                }
                else if (uri.Scheme != "http" && uri.Scheme != "https")
                {
                    errors.Add("OAuth Redirect URI must use HTTP or HTTPS protocol");
                }
            }
        }

        return new OAuthValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            HasOAuthConfiguration = hasAnyOAuthField && errors.Count == 0
        };
    }

    private static bool IsValidUsername(string username)
    {
        // Allow letters, numbers, underscore, and hyphen
        foreach (char c in username)
        {
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
            {
                return false;
            }
        }
        return true;
    }
}

/// <summary>
/// Result of account validation
/// </summary>
public class AccountValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public string FormattedErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Result of OAuth configuration validation
/// </summary>
public class OAuthValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool HasOAuthConfiguration { get; set; }
}