# ✨ Welcome to Your Spark Template!
You've just launched your brand-new Spark Template Codespace — everything's fired up and ready for you to explore, build, and create with Spark!

This template is your blank canvas. It comes with a minimal setup to help you get started quickly with Spark development.

🚀 What's Inside?
- A clean, minimal Spark environment
- Pre-configured for local development
- Ready to scale with your ideas
  
🧠 What Can You Do?

Right now, this is just a starting point — the perfect place to begin building and testing your Spark applications.

🧹 Just Exploring?
No problem! If you were just checking things out and don't need to keep this code:

- Simply delete your Spark.
- Everything will be cleaned up — no traces left behind.

📄 License For Spark Template Resources 

The Spark Template files and resources from GitHub are licensed under the terms of the MIT license, Copyright GitHub, Inc.

# Social Media Commander

## OAuth Configuration Setup Guide

The application logs show that OAuth configurations contain placeholder values. Here's how to set up each platform:

### 🔵 BlueSky Setup
1. Visit the BlueSky Developer Portal
2. Create a new application
3. Copy your Client ID and Client Secret
4. Open Social Media Commander → Settings → OAuth Configuration
5. Replace the placeholder values for BlueSky

### 🔵 Twitter/X Setup  
1. Go to [developer.twitter.com](https://developer.twitter.com)
2. Create a new Project and App
3. Generate OAuth 2.0 Client ID and Client Secret
4. Configure your redirect URI: `http://localhost:8080/oauth/callback`
5. Add the credentials to OAuth Configuration in the app

### 🔵 LinkedIn Setup
1. Visit [LinkedIn Developer Console](https://www.linkedin.com/developers/)
2. Create new Application
3. Get Client ID and Client Secret from Auth tab
4. Add `http://localhost:8080/oauth/callback` as redirect URI
5. Configure in Social Media Commander

### 🔵 Meta Platforms (Facebook/Threads)
1. Go to [developers.facebook.com](https://developers.facebook.com)
2. Create new App (select appropriate type)
3. Get App ID and App Secret
4. Configure OAuth redirect settings
5. Add credentials to the application

### ⚠️ Common Issues Found in Logs:
- **UI Binding Issues**: Fixed with better null checking
- **Placeholder OAuth Values**: Follow setup guides above
- **Account Management**: Enhanced error handling added

### 🛠️ Troubleshooting
If you see errors like "OAuth configuration has placeholder values":
1. Open the application
2. Go to Account Manager
3. The app will show specific setup instructions
4. Follow the platform-specific guide above

## Installation

1. Clone the repository
   ```bash
   git clone https://github.com/snickler/social-media-command.git
   cd social-media-command
   ```

2. **Setup Git hooks (ONE-TIME, 5 minutes) - HIGHLY RECOMMENDED**:
   
   This configures your Git globally so ALL future repository clones will automatically have hooks configured!
   
   ```bash
   # Windows (PowerShell)
   .\scripts\setup-global-template.ps1
   
   # Linux/macOS
   chmod +x scripts/setup-global-template.sh
   ./scripts/setup-global-template.sh
   ```
   
   **Alternative (per-repository only)**:
   ```bash
   .\setup-hooks.ps1  # Windows
   ./setup-hooks.sh   # Linux/macOS
   ```

3. Build the solution
   ```bash
   dotnet build SocialMediaCommander.sln
   ```

4. Configure OAuth settings (see guide above)

5. Run the application
   ```bash
   dotnet run --project SocialMediaCommander.Desktop
   ```

## Development

### Git Hooks (Truly Automatic Setup)

This project uses Git hooks to maintain code quality. We provide **multiple layers** to ensure hooks are always active:

#### 🌟 Best Option: Global Template (One-Time Setup for All Repos)

**Run once, benefits all future repository clones:**

```bash
# Windows
.\scripts\setup-global-template.ps1

# Linux/macOS
chmod +x scripts/setup-global-template.sh
./scripts/setup-global-template.sh
```

**What this does:**
- Configures your Git globally to auto-setup hooks on ANY repository clone
- Every future `git clone` of repos with `.githooks/` will work automatically
- Never manually setup hooks again!
- 5-minute investment, saves hours over time

#### Alternative: Per-Repository Setup

If you prefer not to configure globally:

```bash
.\setup-hooks.ps1       # Windows
./setup-hooks.sh        # Linux/macOS
```

#### Automatic Fallback: MSBuild Target

If you forget to run setup, hooks will auto-configure on first `dotnet build`!

**Hook Features:**
- **Pre-commit**: Blocks commits with secrets, build errors, or policy violations
- **Post-commit**: Provides helpful reminders and next-step suggestions

**📋 Quick Reference:** See [GIT_HOOKS_QUICK_REF.md](GIT_HOOKS_QUICK_REF.md) for a one-page cheat sheet

**📚 Complete Documentation:**
- [Setup Guide](HOOKS_SETUP_GUIDE.md) - Choose your installation method
- [Truly Automatic Implementation](docs/development/truly-automatic-git-hooks.md) - Complete technical guide
- [Troubleshooting](docs/development/git-hooks-troubleshooting.md) - Problem solving
- [All Git Hooks Docs](docs/development/) - Complete reference library

### Quality Standards

All commits must pass:
- ✅ No hardcoded secrets or credentials
- ✅ Central Package Management compliance (no versions in .csproj)
- ✅ Build verification (`dotnet build`)
- ✅ Async/await best practices (ConfigureAwait in library code)
- ✅ Code formatting (`dotnet format`)

See [.github/copilot-instructions.md](.github/copilot-instructions.md) for complete guidelines.
