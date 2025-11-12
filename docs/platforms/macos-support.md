# macOS Support - Social Media Commander

## Overview

Social Media Commander provides full native support for macOS on both Intel (x64) and Apple Silicon (ARM64) processors. The application leverages cross-platform technologies to deliver a consistent experience across all supported operating systems.

## Supported Versions

- **macOS Intel (x64)**: macOS 10.15 (Catalina) and later
- **macOS Apple Silicon (ARM64)**: macOS 11.0 (Big Sur) and later

## Installation on macOS

### From Release Binaries

1. Download the latest macOS release from the [GitHub Releases page](https://github.com/snickler/social-media-command/releases)
   - For Intel Macs: `SocialMediaCommander-*-macos-x64.tar.gz`
   - For Apple Silicon: `SocialMediaCommander-*-macos-arm64.tar.gz`

2. Extract the archive:
   ```bash
   tar -xzf SocialMediaCommander-*-macos-*.tar.gz
   cd Social\ Media\ Commander.app
   ```

3. Run the application:
   ```bash
   ./Contents/MacOS/SocialMediaCommander.Desktop
   ```

### Building from Source

1. **Prerequisites**:
   - .NET 10 SDK or later
   - Xcode Command Line Tools (optional, for code signing)

2. **Clone and build**:
   ```bash
   git clone https://github.com/snickler/social-media-command.git
   cd social-media-command
   dotnet build SocialMediaCommander.sln --configuration Release
   ```

3. **Run the application**:
   ```bash
   dotnet run --project SocialMediaCommander.Desktop
   ```

### Publishing for Distribution

#### Standard Build (Framework-Dependent)
```bash
dotnet publish SocialMediaCommander.Desktop \
  -r osx-x64 \
  -c Release \
  --self-contained false
```

#### Native AOT Build (Self-Contained, Optimized)
```bash
# For Intel Macs
dotnet publish SocialMediaCommander.Desktop \
  -r osx-x64 \
  -c Release \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishAot=true

# For Apple Silicon Macs
dotnet publish SocialMediaCommander.Desktop \
  -r osx-arm64 \
  -c Release \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishAot=true
```

## macOS-Specific Features

### Security & Encryption

On macOS, Social Media Commander uses **AES-256 encryption** for secure credential storage, combined with machine and user-specific key derivation:

- Keys are derived using machine name, username, and OS version
- Each user on each Mac has unique encryption keys
- Credentials cannot be decrypted on a different machine or user account

This provides equivalent security to Windows DPAPI while maintaining cross-platform compatibility.

### Data Storage Locations

macOS follows standard Apple directory conventions:

- **Application Data**: `~/Library/Application Support/SocialMediaCommander/`
- **Logs**: `~/Library/Logs/SocialMediaCommander/`
- **Cache**: `~/Library/Caches/SocialMediaCommander/`

Encrypted files are stored in the Application Support directory:
- `~/Library/Application Support/SocialMediaCommander/Data/accounts.encrypted`
- `~/Library/Application Support/SocialMediaCommander/Config/oauth-configs.encrypted`
- `~/Library/Application Support/SocialMediaCommander/Settings/app-settings.encrypted`

### App Bundle Structure

The macOS release includes a standard `.app` bundle:

```
Social Media Commander.app/
├── Contents/
│   ├── Info.plist           # Application metadata
│   ├── MacOS/               # Executable and libraries
│   │   └── SocialMediaCommander.Desktop
│   └── Resources/           # Application resources
```

## Platform-Specific Considerations

### Gatekeeper and Code Signing

If you download a pre-built binary, macOS Gatekeeper may block execution due to the app not being signed. To run unsigned apps:

```bash
# Remove the quarantine attribute
xattr -d com.apple.quarantine "Social Media Commander.app"

# Or right-click the app, hold Option, and select "Open"
```

For production distribution, we recommend code signing:
```bash
# Sign the app bundle (requires Apple Developer certificate)
codesign --deep --force --verify --verbose \
  --sign "Developer ID Application: Your Name" \
  "Social Media Commander.app"
```

### Rosetta 2 Compatibility

Intel (x64) builds run on Apple Silicon Macs through Rosetta 2. However, for best performance on Apple Silicon, use the native ARM64 build (`osx-arm64`).

### Native Libraries

The application uses Avalonia UI framework, which includes native libraries for macOS:
- Avalonia.Native (macOS windowing backend)
- SkiaSharp (graphics rendering)

These are automatically bundled during the publish process.

## Development on macOS

### Running Tests

All 962+ tests run successfully on macOS, including 16 macOS-specific platform tests:

```bash
# Run all tests
dotnet test SocialMediaCommander.sln --configuration Release

# Run only macOS platform tests
dotnet test SocialMediaCommander.Tests \
  --filter "FullyQualifiedName~MacOSPlatformTests" \
  --configuration Release
```

### Git Hooks Setup

Git hooks help maintain code quality. On macOS:

```bash
# One-time global setup (recommended)
chmod +x scripts/setup-global-template.sh
./scripts/setup-global-template.sh

# Or per-repository setup
chmod +x setup-hooks.sh
./setup-hooks.sh
```

### IDE Support

- **Visual Studio for Mac**: Full support (recommended)
- **VS Code**: Excellent support with C# extension
- **JetBrains Rider**: Full support

## CI/CD Pipeline

The GitHub Actions workflow automatically builds and tests macOS binaries:

- **Intel (x64)**: Built on `macos-latest` runners
- **Apple Silicon (ARM64)**: Built on `macos-latest` runners (Apple Silicon)

Each commit triggers:
1. ✅ Build verification
2. ✅ Test execution (x64 only)
3. ✅ Native AOT compilation
4. ✅ App bundle creation
5. ✅ Artifact upload

## Performance

Native AOT compilation provides excellent performance on macOS:

- **Startup time**: < 1 second (vs. 2-3 seconds for JIT)
- **Memory usage**: ~50% reduction vs. JIT
- **Binary size**: Self-contained, single-file executable
- **No .NET runtime required**: Fully self-contained

## Troubleshooting

### Application Won't Launch

**Issue**: "Cannot be opened because the developer cannot be verified"

**Solution**:
```bash
xattr -d com.apple.quarantine "Social Media Commander.app"
```

### Encryption Errors

**Issue**: "Decryption failed" or "Invalid key"

**Solution**: Remove encrypted files to reset to defaults:
```bash
rm -rf ~/Library/Application\ Support/SocialMediaCommander/Data/accounts.encrypted
rm -rf ~/Library/Application\ Support/SocialMediaCommander/Config/oauth-configs.encrypted
```

### Missing Libraries

**Issue**: "dylib not found" errors

**Solution**: Ensure you're using the self-contained publish (`--self-contained true`) or have .NET 9.0 runtime installed:
```bash
brew install dotnet-sdk
```

### Performance Issues

**Issue**: Slow performance on Apple Silicon

**Solution**: Use the ARM64 build, not the x64 build running through Rosetta:
```bash
# Verify you're running the correct architecture
file "Social Media Commander.app/Contents/MacOS/SocialMediaCommander.Desktop"
# Should show "arm64" for Apple Silicon
```

## Known Limitations

1. **UI Tests**: Some Avalonia headless UI tests are skipped on all platforms due to ToggleSwitch control template limitations
2. **OAuth Callback**: The OAuth callback server runs on `localhost:8080` - ensure this port is available

## Contributing

macOS-specific contributions are welcome! Areas of interest:

- macOS Keychain integration (alternative to current AES encryption)
- Touch Bar support
- macOS notification center integration
- App Store distribution preparation
- Code signing automation

See [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

## Support

- **GitHub Issues**: [Report macOS-specific issues](https://github.com/snickler/social-media-command/issues)
- **Discussions**: [Ask questions](https://github.com/snickler/social-media-command/discussions)
- **Documentation**: [Complete technical docs](../technical/)

## Version History

- **v1.0.0** (2024): Initial macOS support
  - Intel (x64) and Apple Silicon (ARM64) support
  - Native AOT compilation
  - AES-256 encryption
  - Full feature parity with Windows/Linux
  - 16 macOS-specific platform tests
