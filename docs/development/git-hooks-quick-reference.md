# Git Hooks Quick Reference

## 🎯 What They Do

### Pre-commit Hook (Runs BEFORE commit)
Prevents commits with:
- ❌ Hardcoded secrets/credentials
- ❌ Package versions in .csproj files (must be in Directory.Packages.props)
- ❌ Build failures
- ⚠️ Missing ConfigureAwait(false) in library code
- ⚠️ Code formatting issues
- ⚠️ console.log in production code

### Commit-msg Hook (Validates commit message)
Ensures commit messages:
- ✅ Follow Conventional Commits format (feat, fix, docs, etc.)
- ✅ Enable automated semantic versioning
- ✅ Trigger automated releases when merged to main
- 📝 Provide helpful examples when validation fails

### Post-commit Hook (Runs AFTER commit)
Provides reminders about:
- 🧪 Running tests if C# changed
- 🎨 Testing frontend if UI changed
- 📚 Updating documentation
- 🔧 Updating DI if ViewModels changed
- 📋 PR checklist requirements

## 🚀 Installation

```powershell
# Windows PowerShell
.\scripts\install-hooks.ps1

# Linux/macOS
chmod +x scripts/install-hooks.sh
./scripts/install-hooks.sh
```

## 💡 Common Commands

```bash
# Normal commit (hooks run automatically)
git commit -m "Your message"

# Bypass hooks (emergency only!)
git commit --no-verify -m "Your message"

# Fix formatting before commit
dotnet format

# Test before commit
dotnet build SocialMediaCommander.sln
dotnet test SocialMediaCommander.sln
```

## 🔍 What Gets Checked

| Check | Type | Blocks Commit? |
|-------|------|----------------|
| Secrets detection | ERROR | ✅ Yes |
| CPM compliance | ERROR | ✅ Yes |
| Build verification | ERROR | ✅ Yes |
| **Commit message format** | **ERROR** | **✅ Yes** |
| ConfigureAwait | WARNING | ❌ No |
| Code formatting | WARNING | ❌ No |
| Code markers (TODO/FIXME) | WARNING | ❌ No |
| console.log usage | WARNING | ❌ No |
| Large files (>1MB) | WARNING | ❌ No |

## 📝 Commit Message Format

**Required Format:** `type(optional-scope): description`

**Valid Types:**
- `feat` - New features (triggers minor version bump)
- `fix` - Bug fixes (triggers patch version bump)
- `docs` - Documentation only changes
- `style` - Code style changes (formatting, no functionality change)
- `refactor` - Code refactoring
- `test` - Adding or modifying tests
- `chore` - Maintenance tasks
- `perf` - Performance improvements
- `ci` - CI/CD changes
- `build` - Build system changes

**Examples:**
```bash
git commit -m "feat: add OAuth2 integration for Twitter"
git commit -m "fix: resolve null reference in authentication service"
git commit -m "docs: update API documentation"
git commit -m "chore(deps): update Avalonia to 11.0.0"
```

## 🛠️ Troubleshooting

### "Build failed" error
```bash
# See detailed build output
dotnet build SocialMediaCommander.sln

# Clean and rebuild
dotnet clean
dotnet build
```

### "Secrets detected" error
- Replace real credentials with `YOUR_CLIENT_ID_HERE` placeholders
- Move secrets to environment variables or secure storage
- Never commit real API keys, tokens, or passwords

### "CPM violation" error
```xml
<!-- ❌ WRONG: Version in .csproj -->
<PackageReference Include="Avalonia" Version="11.0.0" />

<!-- ✅ CORRECT: Only in Directory.Packages.props -->
<PackageReference Include="Avalonia" />
```

### "ConfigureAwait warning"
```csharp
// ❌ In library code (Services/, Core/)
var result = await SomeMethodAsync();

// ✅ Correct
var result = await SomeMethodAsync().ConfigureAwait(false);

// ℹ️ ViewModels don't need ConfigureAwait (UI context required)
```

### Hooks not running
```bash
# Check if executable (Linux/macOS)
ls -la .git/hooks/pre-commit

# Make executable
chmod +x .git/hooks/pre-commit
chmod +x .git/hooks/post-commit

# Reinstall
./scripts/install-hooks.sh --force
```

## 📋 Pre-commit Checklist (Manual)

Before committing, ensure:
1. ✅ `dotnet build` passes
2. ✅ `dotnet test` passes (or at least related tests)
3. ✅ No hardcoded secrets
4. ✅ All package versions in Directory.Packages.props only
5. ✅ `dotnet format` applied
6. ✅ ConfigureAwait(false) in library code

## 🎓 Learn More

- Full documentation: [docs/development/git-hooks.md](docs/development/git-hooks.md)
- Project guidelines: [.github/copilot-instructions.md](../.github/copilot-instructions.md)
- Performance patterns: [PERFORMANCE_OPTIMIZATIONS.md](../PERFORMANCE_OPTIMIZATIONS.md)
- Security: [secure-storage-implementation.md](../security/secure-storage-implementation.md)

## 🚨 When to Bypass (Use Sparingly!)

Only use `--no-verify` for:
- Emergency hotfixes (document in commit message)
- Known false positives (report them!)
- Work-in-progress commits on personal branches (but clean up before PR!)

**Never** bypass for main/production branches!
