# Automatic Git Hooks Installation Guide

## Overview

This repository now supports **automatic Git hooks installation** using Git's built-in `core.hooksPath` configuration. This means hooks are version-controlled and automatically updated!

## Quick Start

After cloning the repository, run **once**:

```bash
# Windows (PowerShell)
.\setup-hooks.ps1

# Linux/macOS
chmod +x setup-hooks.sh
./setup-hooks.sh
```

That's it! Hooks are now active and will auto-update with `git pull`.

---

## How It Works

### Traditional Approach (Old)
```
❌ Hooks in .git/hooks/ (not version-controlled)
❌ Manual copying required
❌ Updates need manual redistribution
❌ Easy to forget to install
```

### Modern Approach (New - Recommended)
```
✅ Hooks in .githooks/ (version-controlled)
✅ One-time setup command
✅ Automatic updates via git pull
✅ Team consistency guaranteed
```

### Technical Details

The setup script runs:
```bash
git config core.hooksPath .githooks
```

This tells Git to look for hooks in `.githooks/` instead of the default `.git/hooks/`. Since `.githooks/` is tracked in the repository, everyone gets the same hooks and updates automatically.

---

## Installation Methods Comparison

| Method | Pros | Cons | Recommended For |
|--------|------|------|-----------------|
| **Automatic (core.hooksPath)** | ✅ One-time setup<br>✅ Auto-updates<br>✅ Team consistency | Requires Git 2.9+ (2016) | **Everyone** (default choice) |
| **Manual Copy** | ✅ Works with very old Git | ❌ Manual updates needed<br>❌ Can get out of sync | Legacy systems only |
| **Symbolic Links** | ✅ Auto-updates | ❌ Windows compatibility issues | Unix-only teams |

---

## Step-by-Step Setup

### First-Time Setup (After Clone)

1. **Clone the repository** (if not already done):
   ```bash
   git clone https://github.com/snickler/social-media-command.git
   cd social-media-command
   ```

2. **Run the setup script**:
   
   **Windows (PowerShell):**
   ```powershell
   .\setup-hooks.ps1
   ```
   
   **Linux/macOS:**
   ```bash
   chmod +x setup-hooks.sh
   ./setup-hooks.sh
   ```

3. **Verify installation**:
   ```bash
   git config core.hooksPath
   # Should output: .githooks
   ```

4. **Test with a commit**:
   ```bash
   git add .
   git commit -m "Test commit"
   # You should see pre-commit checks running
   ```

### Expected Output

```
🚀 Social Media Commander - Git Hooks Setup

📍 Configuring Git to use hooks from: .githooks

✅ Git hooks path configured successfully!

Current hooks path: .githooks

✅ Made hooks executable

🎉 Setup complete! Git hooks are now active.

The hooks will automatically run on every commit.

📚 Documentation:
   - Quick Reference: docs/development/git-hooks-quick-reference.md
   - Full Guide: docs/development/git-hooks.md
   - Examples: docs/development/git-hooks-examples.md
```

---

## Automatic Updates

### How Updates Work

1. **Maintainer updates hooks**:
   - Edits `.githooks/pre-commit` or `.githooks/post-commit`
   - Commits and pushes changes

2. **Developers receive updates**:
   ```bash
   git pull
   # Hooks are automatically updated!
   ```

3. **No action needed** - Next commit uses new version

### Verifying Hook Version

Check when hooks were last updated:
```bash
git log -1 --oneline -- .githooks/
```

View current hook content:
```bash
cat .githooks/pre-commit    # Linux/macOS/Git Bash
type .githooks\pre-commit   # Windows CMD
```

---

## Configuration Scope

The `core.hooksPath` setting can be configured at different scopes:

### Repository-Level (Recommended)
```bash
# Default when running setup script
git config core.hooksPath .githooks
```
Only affects this repository.

### Global (All Repositories)
```bash
# Use same hooks for all your repos
git config --global core.hooksPath ~/.githooks
```
⚠️ Only do this if you want the same hooks everywhere!

### System-Level (All Users)
```bash
# Rarely used
git config --system core.hooksPath /etc/git/hooks
```
Requires admin privileges.

---

## Troubleshooting

### Problem: Hooks not running after setup

**Check configuration:**
```bash
git config core.hooksPath
```

**Expected output:** `.githooks`

**If empty or different:**
```bash
# Re-run setup
.\setup-hooks.ps1
```

### Problem: "Permission denied" (Linux/macOS)

**Cause:** Hooks are not executable

**Fix:**
```bash
chmod +x .githooks/pre-commit
chmod +x .githooks/post-commit

# Or re-run setup script
./setup-hooks.sh
```

### Problem: Hooks running from wrong location

**Check where hooks are located:**
```bash
# Show configured path
git config core.hooksPath

# Show what Git is actually using
GIT_TRACE=1 git commit --dry-run 2>&1 | grep hooks
```

### Problem: Want to temporarily disable hooks

**Method 1 - Bypass for one commit:**
```bash
git commit --no-verify -m "Your message"
```

**Method 2 - Disable globally:**
```bash
git config --unset core.hooksPath
# Re-enable with: .\setup-hooks.ps1
```

**Method 3 - Rename hooks directory:**
```bash
git config core.hooksPath .githooks-disabled
```

---

## Advanced Usage

### Using Different Hook Directories

If you want to use a different directory:

```bash
# Create your hooks directory
mkdir .my-custom-hooks
cp .githooks/* .my-custom-hooks/

# Configure Git to use it
git config core.hooksPath .my-custom-hooks
```

### Per-Repository Custom Hooks

Add repository-specific hooks alongside shared ones:

```bash
# .githooks/pre-commit calls:
if [ -f .githooks/pre-commit.local ]; then
    .githooks/pre-commit.local
fi
```

Then create `.githooks/pre-commit.local` (gitignored) for custom checks.

### Testing Hooks Without Committing

```bash
# Run pre-commit manually
.githooks/pre-commit

# Or use Git's hook runner
git hook run pre-commit
```

---

## Migration Guide

### Migrating from Manual Installation

If you previously used `scripts/install-hooks.ps1`:

1. **Remove old hooks** (optional):
   ```bash
   rm .git/hooks/pre-commit
   rm .git/hooks/post-commit
   ```

2. **Run new setup**:
   ```bash
   .\setup-hooks.ps1
   ```

3. **Verify** it's using `.githooks/`:
   ```bash
   git config core.hooksPath
   # Should show: .githooks
   ```

### Team Migration Checklist

- [ ] Add setup instructions to onboarding docs
- [ ] Announce change to team
- [ ] Update CI/CD if it relies on hooks location
- [ ] Archive old `scripts/install-hooks.*` (keep for reference)
- [ ] Update README.md (already done!)

---

## CI/CD Integration

### GitHub Actions

```yaml
name: Verify Hooks

on: [push, pull_request]

jobs:
  verify-hooks:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup hooks
        run: |
          chmod +x setup-hooks.sh
          ./setup-hooks.sh
      
      - name: Verify hook configuration
        run: |
          if [ "$(git config core.hooksPath)" != ".githooks" ]; then
            echo "Hooks not configured correctly!"
            exit 1
          fi
      
      - name: Test pre-commit hook
        run: |
          chmod +x .githooks/pre-commit
          .githooks/pre-commit
```

### Azure DevOps

```yaml
steps:
- script: |
    chmod +x setup-hooks.sh
    ./setup-hooks.sh
    git config core.hooksPath
  displayName: 'Setup and verify Git hooks'
```

---

## Security Considerations

### Trusted Repository Required

⚠️ **Important:** Only run `setup-hooks.ps1` on repositories you trust!

Hooks can execute arbitrary code. The `.githooks/` directory is tracked in Git, so:

- ✅ Review hook changes in pull requests
- ✅ Audit hooks before first-time setup
- ✅ Use branch protection to prevent unauthorized hook changes
- ❌ Don't blindly run setup on unknown repositories

### Verification Before Setup

```bash
# Review hooks before enabling
cat .githooks/pre-commit
cat .githooks/post-commit

# Only then run setup
.\setup-hooks.ps1
```

---

## Best Practices

### For Developers

1. ✅ Run setup immediately after cloning
2. ✅ Pull regularly to get hook updates
3. ✅ Read hook output - it's helpful!
4. ✅ Report false positives
5. ❌ Don't use `--no-verify` without good reason

### For Maintainers

1. ✅ Test hook changes thoroughly before committing
2. ✅ Document hook changes in PR descriptions
3. ✅ Version hooks (add comments with version/date)
4. ✅ Keep hooks fast (<30 seconds)
5. ✅ Provide clear error messages

### Hook Development Guidelines

```bash
#!/bin/sh
#
# pre-commit hook v2.0.0 (2025-10-04)
# Changes: Added support for XYZ
#

# Always add version info
echo "Running pre-commit checks (v2.0.0)..."

# Provide actionable errors
error() {
    echo "❌ ERROR: $1"
    echo "   Fix: $2"  # Always include how to fix
}

# Time-bound operations
timeout 30s dotnet build || {
    error "Build timed out after 30s" "Check for infinite loops or hanging processes"
}
```

---

## Comparison: All Installation Methods

| Feature | Automatic Setup | Manual Copy | Symbolic Links |
|---------|----------------|-------------|----------------|
| **One-time setup** | ✅ Yes | ❌ No | ✅ Yes |
| **Auto-updates** | ✅ Yes | ❌ No | ✅ Yes |
| **Windows support** | ✅ Yes | ✅ Yes | ⚠️ Partial |
| **Git version required** | 2.9+ (2016) | Any | Any |
| **Team consistency** | ✅ Guaranteed | ⚠️ Manual | ✅ Yes |
| **Easy to disable** | ✅ Yes | ⚠️ Delete files | ⚠️ Delete link |
| **Complexity** | 🟢 Low | 🟡 Medium | 🔴 High |

---

## FAQ

### Q: Do I need to re-run setup after updating the repository?
**A:** No! Hooks update automatically with `git pull`.

### Q: Can I customize hooks for my workflow?
**A:** Yes! See "Advanced Usage" section above.

### Q: What if my Git is older than 2.9?
**A:** Use the manual installation method: `.\scripts\install-hooks.ps1`

### Q: Will this work in CI/CD?
**A:** Yes! Just run the setup script in your CI pipeline.

### Q: Can I use this approach in other repositories?
**A:** Absolutely! Copy `.githooks/`, setup scripts, and documentation.

### Q: How do I uninstall/disable?
**A:** Run `git config --unset core.hooksPath`

---

## References

- [Git Hooks Documentation](https://git-scm.com/docs/githooks)
- [Git core.hooksPath Config](https://git-scm.com/docs/git-config#Documentation/git-config.txt-corehooksPath)
- [Project Hooks Documentation](git-hooks.md)

---

## Summary

**Old Way (Manual):**
```bash
git clone repo
scripts/install-hooks.ps1  # Copy hooks to .git/hooks/
# When hooks update: manually run install script again
```

**New Way (Automatic - Recommended):**
```bash
git clone repo
setup-hooks.ps1            # Configure Git once
# When hooks update: automatic via git pull!
```

**Time saved:** ~5 minutes per developer per month

**Consistency improved:** 100% team alignment on hook versions

---

*Last updated: 2025-10-04*
