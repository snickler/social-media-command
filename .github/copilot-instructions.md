## Copilot / AI Agent Instructions — Social Media Commander

**Purpose**: Actionable guidance for AI coding agents to be immediately productive in this repository.

---

## Project Structure & Architecture

Multi-project .NET 9 (Avalonia UI) desktop app + Vite/React documentation frontend:
- **`SocialMediaCommander.Core/`** — Domain models, core helpers, logging infrastructure
- **`SocialMediaCommander.Services/`** — Service interfaces & implementations (platform adapters, OAuth, encryption, AI, backups)
- **`SocialMediaCommander.Desktop/`** — Avalonia UI application (ViewModels, Views, DI wiring, app entry point)
- **`SocialMediaCommander.Tests/`** — xUnit unit & integration tests
- **`src/`** — Vite/React UI for documentation viewer

**MSBuild Standardization** (CRITICAL):
- `Directory.Build.props` — common properties (TargetFramework: net9.0, Version, Nullable: enable, TreatWarningsAsErrors in Release)
- `Directory.Packages.props` — Central Package Management (CPM) for all NuGet versions
- **Never** add `<PackageReference>` versions directly in `.csproj` files; all package versions centralized in `Directory.Packages.props`

---

## Startup Flow & DI Contract

**Boot sequence** (do NOT reorder):
1. `Program.Main` → `LoggingService.Initialize()` (before DI)
2. `App.axaml.cs` → `BuildConfiguration()` loads `appsettings.json`, `appsettings.enhanced.json` (optional), env vars
3. `ServiceCollectionExtensions.AddSocialMediaCommanderServices(services, configuration)` registers all services
4. `MainWindowViewModel` manually composed from child VMs and assigned in `App.OnFrameworkInitializationCompleted`

**Critical ordering constraint**: Many services call `LoggingService.ForContext<T>()` during construction. Logging MUST be initialized before DI container builds services.

**ViewModel composition** (manual, not auto-wired):
- `MainWindowViewModel` constructor signature changes require updating `App.axaml.cs` at the composition site
- Child VMs: `PostEditorViewModel`, `SocialFeedViewModel`, `AccountManagerViewModel`, etc. — all registered as `Transient`

**Service lifetimes** (defined in `ServiceCollectionExtensions.cs`):
- **Singleton**: `IOAuthConfigurationService`, `ISettingsService`, performance helpers (`PerformanceOptimizedService`, `AdvancedLoggingService`)
- **Scoped**: Platform services (BlueSky, Twitter, LinkedIn, etc.), `IAccountService`, `IBackupService`, `IDataIntegrityService`
- **Transient**: All ViewModels

---

## Security & Encrypted Storage

**All sensitive data encrypted at rest** using `CrossPlatformEncryption.cs`:
- Windows: DPAPI (`ProtectedData`)
- Linux/macOS: AES-256 with PBKDF2 key derivation

**Encrypted file locations** (relative to `%APPDATA%\SocialMediaCommander\`):
- `Data/accounts.encrypted` — user accounts (`SecureAccountService.cs`)
- `Config/oauth-configs.encrypted` — OAuth client IDs/secrets (`OAuthConfigurationService.cs`)
- `Settings/app-settings.encrypted` — app settings (`SettingsService.cs`)
- `Backups/*.smcbackup` — compressed encrypted backup archives (`BackupService.cs`)
- `Integrity/checksums.json` — SHA-256 checksums for validation (`DataIntegrityService.cs`)

**Security rules**:
- **Never** commit plaintext secrets; use placeholders like `YOUR_CLIENT_ID_HERE` in defaults
- OAuth configs show placeholders in UI until user provides real credentials
- Test constructors accept `customDirectory` parameter to isolate test data (see `SettingsService(string customDirectory)`)

**Recovery procedures** (see `SECURE_STORAGE_IMPLEMENTATION.md`):
- Decryption failure: delete encrypted file to reset to defaults
- Integrity issues: `DataIntegrityService` auto-repairs or flags for manual intervention

---

## Performance Patterns (CRITICAL to preserve)

This codebase follows **Microsoft's official async/performance best practices**. Reference implementations: `OptimizedAsyncService.cs`, `PerformanceOptimizedService.cs`.

### Async Patterns
1. **ValueTask for hot paths**: Use `ValueTask<T>` when operation may complete synchronously (cache hits, validation). See `GetAccountFastAsync`.
2. **ConfigureAwait(false) in library code**: All service/library methods MUST use `.ConfigureAwait(false)` to prevent deadlocks and improve performance. Example:
   ```csharp
   var account = await _accountService.GetAccountByIdAsync(id).ConfigureAwait(false);
   ```
3. **Task.WhenAll for parallelism**: Batch operations use `Task.WhenAll` for concurrent processing (see `GetAccountsBatchAsync`).
4. **IAsyncEnumerable for streaming**: Use `IAsyncEnumerable<T>` for streaming large result sets (see `ProcessAccountsStreamAsync`).
5. **Avoid async void**: Never use `async void` except for event handlers. Use `Task` or `ValueTask<T>`.
6. **Remove unused async** (CS1998): Methods marked `async` without `await` should return `Task.FromResult(...)`, `Task.CompletedTask`, or be made synchronous.

### Memory Optimization
- **ArrayPool<T>**: Rent/return buffers instead of allocating (`PerformanceOptimizedService.ProcessDataAsync`)
- **Span<T> / ReadOnlyMemory<T>**: Zero-allocation slicing for data processing
- **LoggerMessage delegates**: Pre-compiled logging for high-frequency events (`AdvancedLoggingService.cs`)

### Caching
- Cache-first with TTL expiration (see `OptimizedAsyncService._cache`)
- Thread-safe caching with lock or `ConcurrentDictionary`
- Call `ClearExpiredCache()` periodically to prevent memory leaks

---

## Build, Run, Test Commands

**Prerequisites**: .NET 9 SDK, Node.js 18+ (for frontend)

From repo root (Windows `cmd.exe` or PowerShell):
```cmd
REM Build solution
dotnet build SocialMediaCommander.sln

REM Run desktop app
dotnet run --project SocialMediaCommander.Desktop\SocialMediaCommander.Desktop.csproj

REM Run all tests
dotnet test SocialMediaCommander.sln

REM Run tests for specific project
dotnet test SocialMediaCommander.Tests\SocialMediaCommander.Tests.csproj

REM Frontend dev server (documentation viewer)
npm install
npm run dev
```

**Test framework**: xUnit with FluentAssertions and Moq. All tests are in `SocialMediaCommander.Tests/`.

**Test infrastructure**:
- **Headless UI Testing**: Avalonia.Headless with Skia rendering enabled for screenshot capture
- **Screenshot Testing**: `ScreenshotHelper.cs` — captures, saves, and compares UI screenshots using `Window.CaptureRenderedFrame()`
- **Visual Regression Testing**: `VisualRegressionTests.cs` — comprehensive screenshot tests for all views (see `.github/VISUAL_REGRESSION_TESTING.md`)
- **Test Recording**: `RecordedTestBase.cs` — records test interactions for debugging and documentation
- **Test App Configuration**: `TestAppBuilder.cs` — configures headless platform with Skia backend (`UseHeadlessDrawing = false`)

**Screenshot Testing Requirements**:
- Tests must host controls in a `Window` for proper rendering
- Use `ScreenshotHelper.Capture(control, width, height)` to capture `WriteableBitmap`
- Screenshots saved to `Screenshots/TestRun/` (test runs) and `Screenshots/Baselines/` (approved baselines)
- Force render with `AvaloniaHeadlessPlatform.ForceRenderTimerTick()` before capture
- Baseline screenshots committed to repo; test run screenshots gitignored
- **Visual regression workflow**: Run tests → Review screenshots → Approve baselines → Commit baselines

**Visual Regression Testing Workflow** (see `.github/VISUAL_REGRESSION_TESTING.md` for complete guide):
1. Run tests: `dotnet test --filter "FullyQualifiedName~VisualRegressionTests"`
2. Review screenshots in `Screenshots/TestRun/`
3. If approved, copy to `Screenshots/Baselines/`
4. Commit baselines with PR
5. CI automatically runs visual regression on PRs modifying views

**Known Test Limitations**:
- Some UI controls (ToggleSwitch) not fully supported in headless mode — tests skipped with documented reasons
- Screenshots produce actual PNG data with Skia enabled (not zero-byte files)
- Platform-specific font rendering may cause minor pixel differences between Windows/Linux/macOS

---

## Project-Specific Conventions

### Configuration
- `appsettings.json` — base config (checked into repo, no secrets)
- `appsettings.enhanced.json` — optional feature toggles (not required for basic functionality)
- Configuration manually bound in `ServiceCollectionExtensions` for AOT compatibility (avoid reflection-based binding)

### Nullability
- **Nullable reference types enabled** (`<Nullable>enable</Nullable>` in `Directory.Build.props`)
- Use `object?` for dictionary values that may be null (e.g., `CustomSettings` in `SettingsService`)
- Defensive null checks before assignment to avoid CS8601/CS8604

### Async/await
- See "Performance Patterns" above
- Integration tests: avoid unnecessary `async Task` if no `await` — convert to synchronous `void` test methods

---

## Debugging & Troubleshooting

**Logs location**: `%LOCALAPPDATA%\SocialMediaCommander\Logs\`
- `app-*.log` — general application logs (rolling daily)
- `errors-*.log` — error-level logs only

**Common issues**:
1. **DI registration hangs**: `App.axaml.cs` logs each ViewModel creation step. Check logs for which VM constructor is blocking.
2. **OAuth placeholder warnings**: UI shows "OAuth configuration has placeholder values" until user provides real credentials via Settings → OAuth Configuration.
3. **Decryption failures**: Delete `%APPDATA%\SocialMediaCommander\Config\oauth-configs.encrypted` to reset to defaults.
4. **Binding errors in Avalonia**: Check `DataAnnotationsValidationPlugin` is disabled in `App.DisableAvaloniaDataAnnotationValidation()`.

**Manual testing checklist**:
- Run desktop app and check for startup errors in logs
- Test OAuth flow end-to-end (requires valid client IDs)
- Verify encrypted files are created in `%APPDATA%\SocialMediaCommander\`

---

## CI/CD & Release

**Complete workflow documentation**: See `.github/WORKFLOWS_OVERVIEW.md` for visual diagrams and navigation.

### Workflows
- **`release.yml`** — Creates stable releases from `main` using semantic-release (no commits to main)
- **`pre-release.yml`** — Creates alpha/beta/rc pre-releases from `develop`, `rc/*`, or feature branches
- **`ci-cd.yml`** — Builds and tests all platforms; triggered by commits, PRs, and tags
- **`code-quality.yml`** — Runs linting, formatting checks on PRs
- **`visual-regression.yml`** — Runs visual regression tests on PRs; captures screenshots for manual review

### Release Process
- **Stable releases**: Push to `main` with conventional commits → auto-creates tag → builds artifacts
- **Pre-releases**: Push to `develop` (beta) or `rc/*` (rc) → auto-creates pre-release tag → builds artifacts
- **Manual releases**: Actions → Release/Pre-Release workflow → Run workflow

**Key Documentation**:
- `.github/RELEASE_WORKFLOW.md` — Stable release process
- `.github/PRE_RELEASE_GUIDE.md` — Complete pre-release documentation
- `.github/PRE_RELEASE_QUICK_REF.md` — Quick commands for developers
- `.github/RELEASE_CHECKLIST.md` — Step-by-step checklists for release managers
- `.github/VISUAL_REGRESSION_TESTING.md` — Visual regression testing guide and workflow

**Branch Protection**: Main branch is protected; workflows create tags/releases without committing to main.

**Do not rely on CI for local testing** — run `dotnet build` and `dotnet test` locally.

---

## Minimal PR Checklist for AI Agents

Before proposing changes, **MUST** complete:
1. ✅ `dotnet build SocialMediaCommander.sln` — no errors, warnings reviewed
2. ✅ `dotnet test SocialMediaCommander.sln` — all tests pass (970+ tests expected)
3. ✅ Search for plaintext secrets: `ClientSecret`, `YOUR_CLIENT_ID_HERE` — ensure none committed
4. ✅ If ViewModel constructors changed: update manual composition in `App.axaml.cs`
5. ✅ Preserve logging initialization order: never call `LoggingService.ForContext<T>()` before `LoggingService.Initialize()`
6. ✅ Async patterns follow best practices: `ConfigureAwait(false)` in library code, `ValueTask` where appropriate
7. ✅ Package versions added only to `Directory.Packages.props`, never in `.csproj` files
8. ✅ If UI changes: run desktop app locally and check logs for binding/runtime errors
9. ✅ **If view/style changes**: Run visual regression tests, review screenshots, update baselines if intentional changes (see `.github/VISUAL_REGRESSION_TESTING.md`)
10. ✅ **Commit messages follow Conventional Commits format** (enforced by commit-msg hook) — use `feat:`, `fix:`, `docs:`, `chore:`, etc.
11. ✅ **Pre-commit hooks**: Auto-formats code with `dotnet format` and re-stages changed files (configured in `.githooks/pre-commit`)
12. ✅ **After feature implementation**: Check if copilot instructions need updating; update `.github/copilot-instructions.md` if architecture, workflows, or critical patterns changed

---

## Key Implementation Files (for deep-dive context)

- **Startup & DI**: `SocialMediaCommander.Desktop/Program.cs`, `App.axaml.cs`, `ServiceCollectionExtensions.cs`
- **Security**: `CrossPlatformEncryption.cs`, `SecureAccountService.cs`, `OAuthConfigurationService.cs`
- **Performance**: `OptimizedAsyncService.cs`, `PerformanceOptimizedService.cs`, `AdvancedLoggingService.cs`
- **OAuth Demo**: `OAuthConfigDemo.cs` (shows how to get defaults, save custom configs, validate)
- **Testing Infrastructure**: `TestAppBuilder.cs`, `ScreenshotHelper.cs`, `RecordedTestBase.cs`, `VisualRegressionTests.cs`
- **Documentation**: 
  - Performance: `PERFORMANCE_OPTIMIZATIONS.md`
  - Security: `SECURE_STORAGE_IMPLEMENTATION.md`
  - Features: `ENHANCED_FEATURES_FINAL.md`
  - Workflows: `.github/WORKFLOWS_OVERVIEW.md`, `.github/RELEASE_WORKFLOW.md`, `.github/PRE_RELEASE_GUIDE.md`
  - Testing: `.github/VISUAL_REGRESSION_TESTING.md`

---

## Additional Resources

- **Copilot instructions docs**: https://aka.ms/vscode-instructions-docs
- **Performance best practices**: See `PERFORMANCE_OPTIMIZATIONS.md` for detailed implementation patterns
- **Security implementation**: See `SECURE_STORAGE_IMPLEMENTATION.md` for encryption details and recovery procedures
