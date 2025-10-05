# Git Hooks Troubleshooting Guide

Complete guide to diagnosing and fixing Git hooks issues in the Social Media Commander project.

## 📋 Table of Contents
1. [Quick Diagnostics](#quick-diagnostics)
2. [Common Issues](#common-issues)
3. [Setup Problems](#setup-problems)
4. [Execution Problems](#execution-problems)
5. [Performance Issues](#performance-issues)
6. [Platform-Specific Issues](#platform-specific-issues)
7. [Advanced Troubleshooting](#advanced-troubleshooting)

---

## Quick Diagnostics

### 🔍 Diagnostic Checklist

Run these commands to get a complete picture of your hooks setup:

```bash
# 1. Check if hooks are configured
git config core.hooksPath

# 2. Check if global template is set
git config --global init.templateDir

# 3. Verify .githooks directory exists
ls -la .githooks/

# 4. Check hook file permissions (Linux/macOS)
ls -l .githooks/pre-commit
ls -l .githooks/post-commit

# 5. Test hook execution manually
.githooks/pre-commit

# 6. Check Git version
git --version

# 7. Verify you're in the repo root
git rev-parse --show-toplevel
```

### ✅ Expected Results

| Command | Expected Output | What It Means |
|---------|----------------|---------------|
| `git config core.hooksPath` | `.githooks` | Hooks properly configured |
| `git config --global init.templateDir` | Path to template | Global template active |
| `ls -la .githooks/` | `pre-commit`, `post-commit` files | Hook files exist |
| `ls -l .githooks/pre-commit` | `-rwxr-xr-x` (executable) | Hooks have execute permission |
| `.githooks/pre-commit` | No error, runs checks | Hook executes successfully |
| `git --version` | `≥ 2.9.0` | Git supports core.hooksPath |

---

## Common Issues

### Issue 1: "Hooks Not Running"

**Symptoms:**
- Commits succeed without any quality checks
- No output from pre-commit hook
- Changes committed without validation

**Diagnosis:**
```bash
# Check configuration
git config core.hooksPath
# If empty → hooks not configured
```

**Solutions:**

**Solution A: Run Per-Repo Setup**
```bash
# Windows
.\setup-hooks.ps1

# Linux/macOS
chmod +x setup-hooks.sh
./setup-hooks.sh
```

**Solution B: Setup Global Template (Recommended)**
```bash
# Windows
.\scripts\setup-global-template.ps1

# Linux/macOS
chmod +x scripts/setup-global-template.sh
./scripts/setup-global-template.sh
```

**Solution C: Manual Configuration**
```bash
git config core.hooksPath .githooks
```

**Verification:**
```bash
# Should show: .githooks
git config core.hooksPath

# Test with empty commit
git commit --allow-empty -m "Test hooks"
# Should see pre-commit checks run
```

---

### Issue 2: "Permission Denied" (Linux/macOS)

**Symptoms:**
```
bash: .githooks/pre-commit: Permission denied
```

**Diagnosis:**
```bash
ls -l .githooks/pre-commit
# If shows: -rw-r--r-- (not executable)
```

**Solution:**
```bash
# Make hooks executable
chmod +x .githooks/pre-commit
chmod +x .githooks/post-commit

# Verify
ls -l .githooks/
# Should show: -rwxr-xr-x
```

**Permanent Fix:**
```bash
# Add to setup script
chmod +x .githooks/*

# Or add Git config to preserve permissions
git update-index --chmod=+x .githooks/pre-commit
git update-index --chmod=+x .githooks/post-commit
```

---

### Issue 3: "Hook Configured But Not Running"

**Symptoms:**
- `git config core.hooksPath` shows `.githooks`
- But commits don't trigger hooks

**Diagnosis:**
```bash
# Run hook manually
.githooks/pre-commit
# Check for errors
```

**Common Causes:**

**A. Wrong Working Directory**
```bash
# Hook looks for files relative to repo root
git rev-parse --show-toplevel
# Make sure you're in: c:\repos\social-media-command
```

**B. Shell Interpreter Issues (Windows)**
```bash
# Git Bash not found
where bash
# If empty, install Git for Windows with Git Bash
```

**C. File Encoding Issues**
```bash
# Windows: Check for BOM or CRLF issues
file .githooks/pre-commit
# Should show: ASCII text executable

# Fix if needed:
dos2unix .githooks/pre-commit  # Linux/macOS
# Or re-create file with correct encoding
```

**Solution:**
```bash
# Ensure Git Bash is in PATH (Windows)
set PATH=%PATH%;C:\Program Files\Git\bin

# Or re-run setup script
.\setup-hooks.ps1
```

---

### Issue 4: "Global Template Not Working"

**Symptoms:**
- Hooks don't auto-configure on `git clone`
- Bootstrap hook doesn't run

**Diagnosis:**
```bash
# Check global template config
git config --global init.templateDir

# Check if template directory exists
ls -la ~/.git-templates/social-media-command/hooks/
```

**Solutions:**

**Solution A: Re-run Global Template Setup**
```bash
.\scripts\setup-global-template.ps1
```

**Solution B: Verify Template Path**
```bash
# Check template path
git config --global init.templateDir
# Should be: ~/.git-templates/social-media-command
# Or: C:\Users\<username>\.git-templates\social-media-command (Windows)

# If wrong, set manually:
git config --global init.templateDir "~/.git-templates/social-media-command"
```

**Solution C: Check Bootstrap Hook**
```bash
# Verify bootstrap hook exists
cat ~/.git-templates/social-media-command/hooks/post-checkout
# Should contain auto-configuration logic

# Test manually
bash ~/.git-templates/social-media-command/hooks/post-checkout
```

---

### Issue 5: "Build Verification Fails"

**Symptoms:**
```
❌ dotnet build failed. Commit blocked.
```

**Diagnosis:**
```bash
# Run build manually
dotnet build SocialMediaCommander.sln

# Check for errors
echo $?  # Linux/macOS
echo %ERRORLEVEL%  # Windows
# If non-zero → build has errors
```

**Solutions:**

**A. Fix Build Errors**
```bash
# View detailed errors
dotnet build SocialMediaCommander.sln --verbosity detailed

# Common issues:
# - Missing dependencies: dotnet restore
# - Compiler errors: Fix code issues
# - Wrong .NET version: Install .NET 9 SDK
```

**B. Temporarily Bypass (Emergency Only)**
```bash
# Skip pre-commit checks (NOT RECOMMENDED)
git commit --no-verify -m "Emergency fix"

# Better: Fix issues and commit normally
```

---

### Issue 6: "Secrets Detection False Positives"

**Symptoms:**
```
❌ Potential secrets found in staged files!
  • AWS_ACCESS_KEY_ID (placeholder in config)
```

**Diagnosis:**
```bash
# Check what triggered detection
git diff --staged | grep -i "key\|secret\|password\|token"
```

**Solutions:**

**A. Use Proper Placeholders**
```csharp
// ❌ Triggers false positive
string clientSecret = "sk-1234abcd";

// ✅ Recognized placeholder
string clientSecret = "YOUR_CLIENT_SECRET_HERE";
```

**B. Add to Allowlist (Edit Hook)**
```bash
# Edit .githooks/pre-commit
# Add to ALLOWED_PATTERNS:
ALLOWED_PATTERNS=(
    "YOUR_CLIENT_ID_HERE"
    "YOUR_CLIENT_SECRET_HERE"
    "example.com/api"
    # Add your pattern here
)
```

**C. Bypass for Specific Files**
```bash
# Edit .githooks/pre-commit
# Add to EXCLUDED_FILES:
EXCLUDED_FILES=(
    "appsettings.json"
    "appsettings.enhanced.json"
    # Add your file here
)
```

---

### Issue 7: "CPM Compliance Check Fails"

**Symptoms:**
```
❌ PackageReference with Version found in .csproj files!
  Move to Directory.Packages.props (Central Package Management)
```

**Diagnosis:**
```bash
# Find problematic references
grep -r 'PackageReference.*Version=' --include='*.csproj'
```

**Solution:**
```xml
<!-- ❌ WRONG: Version in .csproj -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />

<!-- ✅ CORRECT: No version in .csproj -->
<PackageReference Include="Newtonsoft.Json" />

<!-- Add to Directory.Packages.props -->
<ItemGroup>
  <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
</ItemGroup>
```

---

### Issue 8: "ConfigureAwait Warnings"

**Symptoms:**
```
⚠️ Missing ConfigureAwait(false) in library code
```

**Diagnosis:**
```bash
# Find async methods without ConfigureAwait
git diff --staged | grep -A5 "await " | grep -v "ConfigureAwait"
```

**Solution:**
```csharp
// ❌ WRONG: In library/service code
var result = await _service.GetDataAsync();

// ✅ CORRECT: Use ConfigureAwait(false)
var result = await _service.GetDataAsync().ConfigureAwait(false);

// ℹ️ EXCEPTION: UI code (ViewModels) can omit it
// Only in ViewModels where you need UI thread context
var result = await _service.GetDataAsync();  // OK in ViewModels
```

---

## Setup Problems

### Problem: "setup-hooks.ps1 Won't Run"

**Windows PowerShell Execution Policy**

**Error:**
```
.\setup-hooks.ps1 : File cannot be loaded because running scripts is disabled
```

**Solution:**
```powershell
# Check current policy
Get-ExecutionPolicy

# Set policy (as Administrator)
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser

# Or run with bypass
PowerShell -ExecutionPolicy Bypass -File setup-hooks.ps1
```

---

### Problem: "setup-hooks.sh: Command Not Found"

**Missing Execute Permission**

**Solution:**
```bash
# Make script executable
chmod +x setup-hooks.sh
./setup-hooks.sh

# Or run with bash explicitly
bash setup-hooks.sh
```

---

### Problem: "Template Already Exists"

**Error:**
```
Error: Template directory already exists at ~/.git-templates/social-media-command
```

**Solution:**
```bash
# Remove old template
rm -rf ~/.git-templates/social-media-command

# Re-run setup
.\scripts\setup-global-template.ps1
```

---

## Execution Problems

### Problem: "Hook Runs But Commands Fail"

**Missing Dependencies**

**Check for:**
```bash
# .NET SDK
dotnet --version
# Should be: 9.0.x or higher

# Git
git --version
# Should be: 2.9.0 or higher

# Grep (Windows Git Bash)
where grep
# Should be in Git installation

# PowerShell (if using .ps1 hooks)
pwsh --version
# Or: powershell -version
```

**Solution:**
Install missing dependencies:
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Git for Windows](https://git-scm.com/download/win) (includes Git Bash)
- [PowerShell 7](https://github.com/PowerShell/PowerShell) (optional)

---

### Problem: "Commit Takes Too Long"

**Performance Issues**

**Diagnosis:**
```bash
# Add timing to hook (edit .githooks/pre-commit)
time .githooks/pre-commit

# Check which step is slow
# (Hook should show progress for each check)
```

**Solutions:**

**A. Skip Expensive Checks Locally**
```bash
# Set environment variable to skip build
export SKIP_BUILD_CHECK=1  # Linux/macOS
set SKIP_BUILD_CHECK=1     # Windows

git commit -m "..."
```

**B. Optimize Build**
```bash
# Use incremental builds
dotnet build --no-restore

# Or cache build results (advanced)
```

**C. Use Partial Commits**
```bash
# Commit fewer files at once
git add specific-file.cs
git commit -m "..."
```

---

### Problem: "Hook Output Not Visible"

**Git Swallows Output**

**Solution:**
```bash
# Run hook manually to see full output
.githooks/pre-commit

# Or add verbose logging (edit hook)
set -x  # Bash: Enable debug mode
```

---

## Performance Issues

### Issue: Build Check Too Slow

**Optimization Strategies:**

**Strategy 1: Conditional Build**
```bash
# Only build if C# files changed
if git diff --cached --name-only | grep -q '\.cs$'; then
    dotnet build
fi
```

**Strategy 2: Parallel Checks**
```bash
# Run checks in parallel (advanced)
secrets_check &
cpm_check &
wait
```

**Strategy 3: Cache Results**
```bash
# Cache build results by commit hash
BUILD_HASH=$(git rev-parse HEAD)
if [ -f ".build-cache/$BUILD_HASH" ]; then
    echo "✅ Build cached"
else
    dotnet build && touch ".build-cache/$BUILD_HASH"
fi
```

---

## Platform-Specific Issues

### Windows-Specific

**Issue: Line Ending Conflicts**

**Solution:**
```bash
# Configure Git to not convert line endings for hooks
git config core.autocrlf false

# Or use .gitattributes
echo ".githooks/* text eol=lf" >> .gitattributes
```

**Issue: PowerShell vs Git Bash**

**Solution:**
```bash
# Use Git Bash for consistency
# Or create parallel .ps1 versions of hooks
# (Already provided in .githooks/)
```

---

### Linux/macOS-Specific

**Issue: Shebang Not Working**

**Solution:**
```bash
# Verify shebang line
head -n1 .githooks/pre-commit
# Should be: #!/bin/bash or #!/usr/bin/env bash

# If missing, add it:
sed -i '1i#!/bin/bash' .githooks/pre-commit
```

**Issue: Different Shell**

**Solution:**
```bash
# Force bash
bash .githooks/pre-commit

# Or change shebang to your shell
#!/bin/zsh
```

---

## Advanced Troubleshooting

### Debug Mode

**Enable Detailed Logging:**

```bash
# Edit .githooks/pre-commit
# Add at top:
set -x  # Bash: Show every command
$VerbosePreference = "Continue"  # PowerShell

# Or run manually with debug:
bash -x .githooks/pre-commit
```

---

### Tracing Hook Execution

**Git Hook Tracing:**

```bash
# Set Git trace
export GIT_TRACE=1
git commit -m "Test"

# Trace hook execution specifically
export GIT_TRACE_PERFORMANCE=1
git commit -m "Test"
```

---

### Complete Reset

**Nuclear Option (Last Resort):**

```bash
# 1. Remove all hook configuration
git config --unset core.hooksPath
git config --global --unset init.templateDir

# 2. Delete hook files
rm -rf .git/hooks/*

# 3. Delete templates
rm -rf ~/.git-templates/social-media-command

# 4. Re-run complete setup
.\scripts\setup-global-template.ps1
.\setup-hooks.ps1

# 5. Verify
git config core.hooksPath
git config --global init.templateDir
```

---

## Getting Help

### Self-Service Checklist

Before asking for help, verify:

- [ ] Git version ≥ 2.9.0
- [ ] .NET 9 SDK installed
- [ ] In repository root directory
- [ ] Hooks directory exists: `.githooks/`
- [ ] Hooks are executable (Linux/macOS)
- [ ] `core.hooksPath` configured
- [ ] Hook runs manually: `.githooks/pre-commit`
- [ ] No syntax errors in hook files

### Collect Diagnostic Info

```bash
# Run this and share output:
echo "=== Git Hooks Diagnostics ==="
echo "Git Version: $(git --version)"
echo ".NET Version: $(dotnet --version)"
echo "OS: $(uname -a)"
echo "Hooks Path: $(git config core.hooksPath)"
echo "Template Dir: $(git config --global init.templateDir)"
echo "Hook Files:"
ls -la .githooks/
echo "Manual Hook Run:"
.githooks/pre-commit
echo "=== End Diagnostics ==="
```

### Common Support Questions

**Q: Can I disable specific checks?**

A: Yes, edit `.githooks/pre-commit` and comment out the check:
```bash
# check_secrets  # Disabled
check_cpm
check_build
```

**Q: Can I have different rules for different branches?**

A: Yes, add branch detection:
```bash
BRANCH=$(git symbolic-ref --short HEAD)
if [ "$BRANCH" = "main" ]; then
    # Strict checks
else
    # Relaxed checks
fi
```

**Q: How do I update hooks after changes?**

A: Hooks are version-controlled in `.githooks/`. Just `git pull` and they update automatically.

**Q: Can I use hooks in CI/CD?**

A: Yes, but hooks run locally. For CI/CD, duplicate checks in pipeline configuration. See `docs/development/truly-automatic-git-hooks.md` Layer 5.

---

## Prevention Tips

### Best Practices

1. **Run Setup Immediately**: Don't delay hook setup after cloning
2. **Use Global Template**: One-time setup for all repos
3. **Keep Hooks Updated**: `git pull` regularly to get latest checks
4. **Test Before Committing**: Run `dotnet build` before `git commit`
5. **Don't Bypass Checks**: Avoid `--no-verify` unless emergency

### Team Guidelines

1. **Onboarding**: Add hook setup to day-1 checklist
2. **Documentation**: Keep this guide accessible
3. **Monitoring**: Use CI/CD to track hook adoption (Layer 5)
4. **Support**: Designate hooks champion for team support

---

## Related Documentation

- [Git Hooks Setup Guide](../../HOOKS_SETUP_GUIDE.md)
- [Truly Automatic Git Hooks](truly-automatic-git-hooks.md)
- [Git Hooks Execution Flow](git-hooks-execution-flow.md)
- [Git Hooks Quick Reference](git-hooks-quick-reference.md)

---

*Last Updated: 2025-01-04*
*For issues not covered here, open a GitHub issue or contact the development team.*
