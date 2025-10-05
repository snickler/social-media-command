# Git Hooks for Social Media Commander

This document describes the Git hooks configured for this repository to maintain code quality and enforce best practices.

## Overview

Git hooks are scripts that run automatically at specific points in the Git workflow. This repository includes pre-commit and post-commit hooks that enforce the quality standards outlined in `.github/copilot-instructions.md`.

## Installation

### Automatic Installation (Recommended)

Run the installation script from the repository root:

```bash
# Linux/macOS
chmod +x .git/hooks/pre-commit .git/hooks/post-commit

# Windows (PowerShell)
# Hooks are already installed in .git/hooks/
# The pre-commit hook will automatically detect Windows and use PowerShell logic
```

### Manual Installation

The hooks are already present in `.git/hooks/`. To enable them:

1. Ensure they are executable (Linux/macOS):
   ```bash
   chmod +x .git/hooks/pre-commit
   chmod +x .git/hooks/post-commit
   ```

2. On Windows, the hooks will run under Git Bash or you can use the PowerShell version:
   ```powershell
   # To use PowerShell version, create a wrapper in pre-commit:
   # #!/bin/sh
   # exec powershell.exe -ExecutionPolicy Bypass -File ".git/hooks/pre-commit.ps1"
   ```

## Pre-commit Hook

The pre-commit hook runs **before** a commit is created and prevents commits that don't meet quality standards.

### Checks Performed

1. **🔐 Secrets Detection**
   - Scans for hardcoded `ClientSecret` values
   - Detects API keys, passwords, tokens longer than 20 characters
   - Ensures only placeholder values (e.g., `YOUR_CLIENT_ID_HERE`) are committed
   - **Blocks commit** if real secrets are found

2. **📦 Central Package Management (CPM) Compliance**
   - Ensures no `<PackageReference>` elements have `Version` attributes in `.csproj` files
   - All package versions must be in `Directory.Packages.props`
   - **Blocks commit** if violations are found

3. **⚡ Async/Await Best Practices**
   - Checks for `await` calls without `ConfigureAwait(false)` in library code (`Core/` and `Services/`)
   - ViewModels are exempt from this check
   - **Warns** if violations are found (doesn't block commit)

4. **🔍 Nullable Reference Types**
   - Detects potential null assignments without proper nullable handling
   - **Warns** if violations are found

5. **🎨 Code Formatting**
   - Runs `dotnet format --verify-no-changes` on staged C# files
   - **Warns** if formatting issues are found (suggests running `dotnet format`)

6. **🏗️ Build Verification**
   - Runs `dotnet build SocialMediaCommander.sln` when C# files are staged
   - **Blocks commit** if build fails

7. **📝 Code Markers**
   - Detects `TODO`, `FIXME`, `HACK`, `XXX` comments in new code
   - **Warns** to ensure they're tracked

8. **🖥️ Frontend Logging**
   - Detects `console.log` and `console.debug` in new TypeScript/JavaScript code
   - **Warns** to use proper logging instead

9. **🧪 Test File Conventions**
   - Warns if committing `.disabled` test files

10. **📏 File Size Check**
    - Warns if files larger than 1MB are being committed

### Bypassing Pre-commit Checks

⚠️ **Not recommended**, but you can bypass the pre-commit hook:

```bash
git commit --no-verify -m "Your message"
```

Only use this for emergency fixes or when the hook gives false positives.

## Commit-msg Hook

The commit-msg hook runs **after** you write your commit message but **before** the commit is finalized. It validates that your commit message follows the [Conventional Commits](https://www.conventionalcommits.org/) format, which is required for automated semantic versioning and releases.

### Why Conventional Commits?

This project uses [semantic-release](https://github.com/semantic-release/semantic-release) to automatically:
- Determine the next version number based on commit messages
- Generate release notes
- Create GitHub releases
- Publish build artifacts

For this automation to work, commit messages **must** follow the Conventional Commits format.

### Validation Rules

**Valid Format:**
```
type(optional-scope): description

[optional body]

[optional footer]
```

**Required Elements:**
- **type**: One of the following
  - `feat` - New features (triggers **minor** version bump, e.g., 1.0.0 → 1.1.0)
  - `fix` - Bug fixes (triggers **patch** version bump, e.g., 1.0.0 → 1.0.1)
  - `docs` - Documentation only changes
  - `style` - Code style changes (formatting, no functionality change)
  - `refactor` - Code refactoring
  - `test` - Adding or modifying tests
  - `chore` - Maintenance tasks
  - `perf` - Performance improvements
  - `ci` - CI/CD pipeline changes
  - `build` - Build system changes
  - `revert` - Reverting changes
- **scope** (optional): Affected area (e.g., `auth`, `ui`, `deps`)
- **description**: Brief summary of the change (must not be empty)

**For Breaking Changes:**
Add `BREAKING CHANGE:` in the commit footer to trigger a **major** version bump (e.g., 1.0.0 → 2.0.0)

```bash
feat: redesign authentication API

BREAKING CHANGE: AuthService.Login() now returns Task<AuthResult> instead of bool
```

### Auto-Skipped Cases

The hook automatically skips validation for:
- Merge commits (e.g., "Merge branch 'main'")
- Revert commits (e.g., "Revert 'feat: add feature'")
- Automated release commits (e.g., "chore(release): 1.0.0")

### Examples

**✅ Valid commit messages:**
```bash
git commit -m "feat: add OAuth2 integration for Twitter"
git commit -m "fix: resolve null reference in authentication service"
git commit -m "docs: update API documentation"
git commit -m "chore(deps): update Avalonia to 11.0.0"
git commit -m "perf(query): optimize database queries"
```

**❌ Invalid commit messages (will be blocked):**
```bash
git commit -m "Add OAuth2 integration"          # Missing type
git commit -m "Added new feature"               # Wrong format
git commit -m "feat:"                           # Missing description
git commit -m "feature: add something"          # Wrong type name (use 'feat')
git commit -m "bug: fix issue"                  # Wrong type name (use 'fix')
```

### Bypassing Commit-msg Validation

⚠️ **Not recommended**, but you can bypass using:

```bash
git commit --no-verify -m "Your message"
```

**Important:** Commits without conventional format will **not** trigger automated releases when merged to main!

## Post-commit Hook

The post-commit hook runs **after** a commit is successfully created. It provides helpful reminders and suggestions based on what was changed.

### Actions Performed

1. **🧪 Test Reminders**
   - Suggests running `dotnet test` if C# files were modified

2. **🎨 Frontend Dev Server**
   - Reminds to test in dev server if frontend files changed

3. **📦 Dependency Updates**
   - Reminds to verify builds and check for vulnerabilities if `Directory.Packages.props` changed

4. **⚙️ Configuration Changes**
   - Warns to verify no secrets committed and documentation is updated

5. **🔧 ViewModel Changes**
   - Reminds to update `App.axaml.cs` if ViewModel constructors changed

6. **🔌 Service Interface Changes**
   - Reminds to update implementations and DI registrations

7. **📚 Documentation Reminders**
   - Suggests updating `CHANGELOG.md` for large commits (>10 files)

8. **🔐 Security File Changes**
   - Reminds to update security documentation if encryption/auth files changed

9. **⚡ Performance File Changes**
   - Suggests running performance tests if performance-critical files changed

10. **📋 PR Checklist**
    - Shows the complete PR checklist from `.github/copilot-instructions.md` when on a feature branch

11. **📊 Commit Stats**
    - Displays files changed, lines added/deleted

## Examples

### Successful Pre-commit (No Issues)

```
🔍 Running pre-commit checks...

🔐 Checking for secrets and sensitive data...
✅ No secrets detected

📦 Checking Central Package Management compliance...
✅ Central Package Management compliance verified

⚡ Checking async/await best practices...
✅ Async/await patterns verified

🎨 Running dotnet format check...
✅ Code formatting verified

🏗️  Running build verification...
✅ Build verification passed

📝 Checking for code markers...
✅ No unresolved code markers in new code

📏 Checking file sizes...
✅ No large files detected

================================
✅ All pre-commit checks PASSED!

Proceeding with commit...
```

### Failed Pre-commit (Build Error)

```
🔍 Running pre-commit checks...

🔐 Checking for secrets and sensitive data...
✅ No secrets detected

📦 Checking Central Package Management compliance...
✅ Central Package Management compliance verified

🏗️  Running build verification...
❌ ERROR: Build failed. Fix build errors before committing.
  Run 'dotnet build SocialMediaCommander.sln' to see details.

================================
❌ Pre-commit checks FAILED with 1 error(s) and 0 warning(s)

Fix the errors above before committing.
To bypass these checks (not recommended), use: git commit --no-verify
```

### Post-commit Output

```
🎉 Commit successful!

ℹ️  Post-commit tasks for commit: a1b2c3d

🧪 C# files modified. Consider running tests:
   dotnet test SocialMediaCommander.sln

⚠️  ViewModel(s) modified. If constructors changed:
   - Update manual composition in App.axaml.cs
   - Verify DI registration in ServiceCollectionExtensions.cs

📋 Before creating PR, verify the checklist in .github/copilot-instructions.md:
   ✓ dotnet build SocialMediaCommander.sln (no errors)
   ✓ dotnet test SocialMediaCommander.sln (all tests pass)
   ✓ No plaintext secrets
   ✓ ViewModel composition updated if needed
   ✓ Logging initialization order preserved
   ✓ Async patterns follow best practices
   ✓ Package versions only in Directory.Packages.props
   ✓ Desktop app runs without errors

📊 Commit Stats:
   Files changed: 3
   Lines added: +127
   Lines deleted: -15

✅ Post-commit tasks complete!
```

## Customization

### Disabling Specific Checks

Edit the hook files in `.git/hooks/` and comment out sections you want to disable:

```bash
# Comment out this section to disable secrets check
# echo ""
# echo "🔐 Checking for secrets and sensitive data..."
# ...
```

### Adjusting Thresholds

You can modify thresholds in the hooks:

```bash
# Change file size threshold (currently 1MB)
if [ $size -gt 1048576 ]; then  # Change 1048576 to your preferred size in bytes
```

### Adding Custom Checks

Add your own checks to the hook files. Example:

```bash
# 11. Custom check for copyright headers
echo ""
echo "©️  Checking copyright headers..."
for file in $(git diff --cached --name-only | grep "\.cs$"); do
    if [ -f "$file" ]; then
        if ! head -n 5 "$file" | grep -q "Copyright"; then
            warning "File $file is missing copyright header"
        fi
    fi
done
```

## Troubleshooting

### Hooks Not Running

1. Check if hooks are executable:
   ```bash
   ls -la .git/hooks/pre-commit
   ```

2. Ensure hooks don't have `.sample` extension

3. Check Git config:
   ```bash
   git config --get core.hooksPath
   ```
   If set, hooks may be in a different location.

### PowerShell Execution Policy (Windows)

If you get execution policy errors:

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### False Positives

If a check gives false positives:
1. Add an exception comment in your code (e.g., `// No ConfigureAwait needed: UI context required`)
2. Temporarily bypass with `--no-verify` (use sparingly)
3. Report the issue so the hook can be improved

## Best Practices

1. **Don't bypass hooks regularly** - They catch real issues
2. **Run manual checks before committing** - Especially `dotnet build` and `dotnet test`
3. **Keep hooks updated** - As project requirements evolve
4. **Share hook improvements** - Submit PRs for better checks
5. **Document exceptions** - If you bypass a hook, document why in commit message

## Integration with CI/CD

These hooks complement (not replace) CI/CD checks:

- **Pre-commit hooks**: Fast, local, immediate feedback
- **CI/CD pipeline**: Comprehensive, multi-environment, definitive

Always ensure CI/CD runs the same checks (or more comprehensive versions) to catch issues that slip through local hooks.

## References

- [Git Hooks Documentation](https://git-scm.com/docs/githooks)
- [Copilot Instructions](.github/copilot-instructions.md) - Project-specific quality standards
- [Performance Best Practices](PERFORMANCE_OPTIMIZATIONS.md)
- [Security Implementation](SECURE_STORAGE_IMPLEMENTATION.md)
