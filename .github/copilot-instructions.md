## Copilot / AI agent instructions — Social Media Commander

Purpose: quick, actionable facts-only guidance so an AI coding agent can be productive immediately in this repository.

Overview (concise)
- Multi-project .NET (Avalonia) desktop app + small Vite frontend. Key folders:
  - `SocialMediaCommander.Core/` — domain models and helpers (logging, models).
  - `SocialMediaCommander.Services/` — interfaces and implementations (platform adapters, encryption, OAuth, backup, AI).
  - `SocialMediaCommander.Desktop/` — Avalonia UI, ViewModels, Views, DI wiring and app startup.
  - `SocialMediaCommander.Tests/` — unit & integration tests.
  - `src/` — small Vite/React UI used for docs/dev UI.

Startup & DI contract
- Startup flow: `Program.Main` -> `LoggingService.Initialize()` -> `App.axaml.cs` builds config and DI -> `ServiceCollectionExtensions.AddSocialMediaCommanderServices()` registers services -> `MainWindowViewModel` composed from child VMs and assigned to `MainWindow`.
- Important: logging is initialized before DI; many services call `LoggingService.ForContext<T>()` early — do not change initialization ordering.

Security & secure storage (summarized from `SECURE_STORAGE_IMPLEMENTATION.md` and `OAuthConfigurationService.cs`)
- All sensitive data is encrypted at rest. Files and locations:
  - Accounts: `%APPDATA%\SocialMediaCommander\Data\accounts.encrypted` (`SecureAccountService.cs`).
  - OAuth configs: `%APPDATA%\SocialMediaCommander\Config\oauth-configs.encrypted` (`OAuthConfigurationService.cs`).
  - Settings: `%APPDATA%\SocialMediaCommander\Settings\app-settings.encrypted` (`SettingsService.cs`).
- Cross-platform encryption: `CrossPlatformEncryption.cs` (DPAPI on Windows; AES-256 KDF fallback on Linux/macOS). Do not add plaintext secrets to repo; defaults show `YOUR_CLIENT_ID_HERE` placeholders.

Backups & integrity
- BackupService creates encrypted compressed backups under `%APPDATA%\SocialMediaCommander\Backups\` and supports selective restore and metadata. DataIntegrityService performs checksum/SHA-256 validations and auto-repair (see `BackupService.cs`, `DataIntegrityService.cs`).

Performance & implementation notes (from `PERFORMANCE_OPTIMIZATIONS.md` and `ENHANCED_FEATURES*.md`)
- Key optimizations to be aware of and preserve when changing code:
  - `ValueTask` returns for fast-path synchronous completions (`OptimizedAsyncService.cs`).
  - `ArrayPool<T>`, `Span<T>`, pooled buffers for zero-allocation paths (`PerformanceOptimizedService.cs`).
  - `LoggerMessage` delegates in `AdvancedLoggingService.cs` for low-allocation logging.
  - `ConfigureAwait(false)` in library code to avoid deadlocks.
  - Caching strategies (cache-first, TTL) with high hit ratios — changing caching must maintain thread-safety.

Docs & implementation map (files to read for details)
- Security & storage: `SocialMediaCommander.Services/Implementation/CrossPlatformEncryption.cs`, `SecureAccountService.cs`, `OAuthConfigurationService.cs`, `SettingsService.cs`.
- Performance: `PerformanceOptimizedService.cs`, `OptimizedAsyncService.cs`, `AdvancedLoggingService.cs`, `PERFORMANCE_OPTIMIZATIONS.md`.
- High-level summary and migration notes: `IMPLEMENTATION_SUMMARY.md`, `ENHANCED_FEATURES.md`, `ENHANCED_FEATURES_FINAL.md`.
- OAuth demo: `OAuthConfigDemo.cs` shows how to get defaults, save custom configs, validate, and use `OAuthAuthenticationService`.
- Logging: primary runtime logging initialized by `SocialMediaCommander.Core/Services/LoggingService.cs`; note `LOGGING_IMPLEMENTATION.md` exists but is empty.

Build, run, test commands (repo root, Windows `cmd.exe`)
- Build: `dotnet build SocialMediaCommander.sln`
- Run desktop app: `dotnet run --project SocialMediaCommander.Desktop\\SocialMediaCommander.Desktop.csproj`
- Run tests: `dotnet test SocialMediaCommander.Tests\\SocialMediaCommander.Tests.csproj` (or `dotnet test SocialMediaCommander.sln`)
- Frontend: `npm install` then `npm run dev` (root `package.json` drives `vite` for `src/`)

Project-specific conventions & gotchas (do not assume defaults)
- `appsettings.enhanced.json` is optional — enabling features often requires editing it (see `App.axaml.cs` BuildConfiguration).
- `MainWindowViewModel` is composed manually from child viewmodels in `App.axaml.cs` — if you change constructor signatures, update that site.
- Service lifetimes: many services are `Scoped` (UI lifetime) while performance helpers are `Singleton`. Match lifetimes to intended usage in `ServiceCollectionExtensions.cs`.

Debugging tips
- Logs: `%LOCALAPPDATA%\\SocialMediaCommander\\Logs` (Serilog file sink) — inspect `app-*.log` and `errors-*.log`.
- Reproduce DI issues: `App.axaml.cs` logs each ViewModel creation step (`Creating PostEditorViewModel...`) to help locate which registration hangs.
- OAuth troubleshooting: delete `%APPDATA%\\SocialMediaCommander\\Config\\oauth-configs.encrypted` to reset defaults if decryption fails (see `SECURE_STORAGE_IMPLEMENTATION.md` recovery procedures).

CI / release notes
- `.github/workflows/publish.yml` runs `build/build-release.sh` and expects `RELEASE_GITHUB_PAT` secret; do not rely on it for local testing.

Minimal PR checklist for AI agents (must run before proposing changes)
1. `dotnet build` and `dotnet test` — ensure no new compile errors or failing tests.
2. Search for plaintext secrets: `ClientSecret`, `YOUR_CLIENT`, `YOUR_CLIENT_ID_HERE`.
3. If changing ViewModel constructors, update `App.axaml.cs` where `MainWindowViewModel` is constructed from child VMs.
4. Preserve logging initialization ordering — never call `LoggingService.ForContext<T>()` before `LoggingService.Initialize()`.
5. Run the desktop app locally (if making UI changes) and check `%LOCALAPPDATA%\\SocialMediaCommander\\Logs` for errors.

If you want me to fold more documentation text verbatim, extract examples into a small reference file, or open a PR with this change, tell me which direction to take and I will proceed.
