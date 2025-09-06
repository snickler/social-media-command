# OAuth Configuration Demo

This document demonstrates how to configure OAuth settings for the Social Media Commander application.

## Overview

The OAuth configuration system allows you to set up authentication for various social media platforms including BlueSky, Twitter/X, LinkedIn, and Meta platforms (Facebook/Threads).

## Configuration Setup

### Getting OAuth Credentials

Each platform requires you to register your application and obtain OAuth credentials:

**BlueSky Setup:**
1. Visit the BlueSky Developer Portal
2. Create a new application
3. Copy your Client ID and Client Secret

**Twitter/X Setup:**
1. Go to [developer.twitter.com](https://developer.twitter.com)
2. Create a new Project and App
3. Generate OAuth 2.0 Client ID and Client Secret
4. Configure redirect URI: `http://localhost:8080/oauth/callback`

**LinkedIn Setup:**
1. Visit [LinkedIn Developer Console](https://www.linkedin.com/developers/)
2. Create new Application
3. Get Client ID and Client Secret from Auth tab
4. Add `http://localhost:8080/oauth/callback` as redirect URI

**Meta Platforms (Facebook/Threads):**
1. Go to [developers.facebook.com](https://developers.facebook.com)
2. Create new App
3. Get App ID and App Secret
4. Configure OAuth redirect settings

### Implementation

The OAuth configuration is managed through the `OAuthConfigurationService` class. Here's the demonstration code:

```csharp
// See OAuthConfigDemo.cs in the root directory for complete implementation
// This file shows how to:
// - Get default OAuth configurations
// - Save custom configurations
// - Validate OAuth settings
// - Use OAuthAuthenticationService
```

## Security Considerations

- OAuth credentials are encrypted at rest using the secure storage system
- Configurations are stored in: `%APPDATA%\SocialMediaCommander\Config\oauth-configs.encrypted`
- Default configurations use placeholder values that must be replaced
- Recovery procedures are available if decryption fails

## Troubleshooting

Common issues and solutions:

1. **Placeholder Values Error**: Replace `YOUR_CLIENT_ID_HERE` and similar placeholders with actual credentials
2. **Decryption Failures**: Delete the encrypted config file to reset to defaults
3. **Redirect URI Mismatches**: Ensure redirect URIs match exactly in platform settings

For complete implementation details, see the `OAuthConfigDemo.cs` file in the repository root.