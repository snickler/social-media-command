# Git Hooks - Tracked Templates

This directory contains the Git hook templates that are version-controlled with the repository.

## Why This Directory Exists

Instead of manually copying hooks to `.git/hooks/` (which is not version-controlled), we store hooks here and configure Git to use this directory via `core.hooksPath`.

## Automatic Setup (Recommended)

After cloning the repository, run the setup script **once**:

```powershell
# Windows (PowerShell)
.\setup-hooks.ps1

# Linux/macOS
chmod +x setup-hooks.sh
./setup-hooks.sh
```

This configures Git to use hooks from this directory automatically.

## What the Setup Does

The setup script runs:
```bash
git config core.hooksPath .githooks
```

This tells Git to look for hooks in `.githooks/` instead of `.git/hooks/`.

## Benefits

✅ **Automatic for all developers** - Hooks are ready after one setup command
✅ **Version-controlled** - Hook updates are tracked in Git
✅ **Team consistency** - Everyone uses the same hook versions
✅ **Easy updates** - Pull updates and they're immediately active
✅ **No manual copying** - Git uses this directory directly

## Verifying Setup

Check if hooks are configured:
```bash
git config core.hooksPath
# Should output: .githooks
```

Test with a commit:
```bash
git add .
git commit -m "Test commit"
# You should see hook output
```

## Hooks Included

- **pre-commit** - Quality checks before commit (blocks on errors)
- **commit-msg** - Validates commit messages follow Conventional Commits format (blocks on errors)
- **post-commit** - Helpful reminders after commit (informational)

## Documentation

- [Complete Guide](../docs/development/git-hooks.md)
- [Quick Reference](../docs/development/git-hooks-quick-reference.md)
- [Workflow Diagram](../docs/development/git-hooks-workflow.md)
- [Examples & Troubleshooting](../docs/development/git-hooks-examples.md)

## Manual Installation (Alternative)

If you prefer the old method of copying to `.git/hooks/`:

```powershell
# Windows
.\scripts\install-hooks.ps1

# Linux/macOS
./scripts/install-hooks.sh
```

## Disabling Hooks

To temporarily disable hooks:
```bash
# Remove the configuration
git config --unset core.hooksPath

# Or bypass for one commit
git commit --no-verify
```

To re-enable:
```bash
.\setup-hooks.ps1  # or ./setup-hooks.sh
```

## Updating Hooks

When hooks are updated in the repository:

1. Pull the latest changes: `git pull`
2. Hooks are automatically updated (no action needed!)
3. New version runs on next commit

## Platform Compatibility

- **Windows**: Works with Git for Windows (Git Bash)
- **Linux**: Native bash support
- **macOS**: Native bash support
- **WSL**: Works seamlessly

## Troubleshooting

### Hooks not running after setup

1. Verify configuration:
   ```bash
   git config core.hooksPath
   ```

2. Check if hooks are executable (Linux/macOS):
   ```bash
   ls -la .githooks/
   ```

3. Re-run setup:
   ```bash
   .\setup-hooks.ps1
   ```

### Permission denied (Linux/macOS)

```bash
chmod +x .githooks/pre-commit
chmod +x .githooks/post-commit
```

### Want to use different hooks location

```bash
# Use a different directory
git config core.hooksPath /path/to/your/hooks

# Reset to default (.git/hooks/)
git config --unset core.hooksPath
```

## For Repository Maintainers

When updating hooks:

1. Edit files in `.githooks/` directory
2. Test thoroughly
3. Commit and push changes
4. All developers get updates on next `git pull`
5. No manual distribution needed!

---

*This approach follows Git's built-in `core.hooksPath` feature, available since Git 2.9 (2016).*
