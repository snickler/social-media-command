# Git Hooks Bootstrap Template

This directory contains bootstrap hooks that auto-configure Git hooks when the repository is cloned.

## The Problem

Even with version-controlled hooks in `.githooks/`, there's a chicken-and-egg problem:
- Hooks won't run until `core.hooksPath` is configured
- But there's no automatic way to configure it on clone
- Developers can forget to run setup scripts

## The Solution: Global Template Directory

Git allows you to configure a **global template directory** that gets copied to every repository you clone. We use this to provide a bootstrap `post-checkout` hook that auto-configures hooks on first checkout.

## One-Time Global Setup (Recommended for All Developers)

### Option 1: Using This Repository's Template (Recommended)

```bash
# Configure Git to use this template directory globally
git config --global init.templateDir /absolute/path/to/social-media-command/.git-template

# Or use relative path from your common repos location
git config --global init.templateDir ~/.git-templates/social-media-command
```

**For this repository specifically (Windows):**
```powershell
# Get absolute path
$templatePath = (Get-Item ".git-template").FullName
git config --global init.templateDir $templatePath
```

**Linux/macOS:**
```bash
# Copy template to home directory
cp -r .git-template ~/.git-templates/social-media-command
git config --global init.templateDir ~/.git-templates/social-media-command
```

### Option 2: Copy Bootstrap Hook to Global Template

If you already have a global template directory:

```bash
# Find your current template directory
git config --global init.templateDir

# Copy our bootstrap hook there
cp .git-template/hooks/post-checkout /path/to/your/template/hooks/
chmod +x /path/to/your/template/hooks/post-checkout
```

### Option 3: Per-Repository Bootstrap (Manual)

If you don't want to configure globally, copy the bootstrap hook after cloning:

```bash
# After cloning, once per repository
cp .git-template/hooks/post-checkout .git/hooks/
chmod +x .git/hooks/post-checkout

# Then checkout any branch to trigger it
git checkout $(git branch --show-current)
```

## How It Works

1. **On clone/checkout**, the `post-checkout` hook runs automatically
2. The hook checks if `.githooks/` directory exists
3. If `core.hooksPath` isn't set to `.githooks`, it configures it
4. Hooks are now active for all future commits!

## Verification

After cloning a repository:

```bash
# Check if hooks are configured
git config core.hooksPath
# Should output: .githooks

# Test with a commit
git commit --allow-empty -m "Test"
# You should see pre-commit checks running
```

## Alternative: Manual Setup (Fallback)

If you prefer not to use global templates:

```bash
# Just run the setup script once after clone
.\setup-hooks.ps1  # Windows
./setup-hooks.sh   # Linux/macOS
```

## For Repository Maintainers

When distributing this repository, recommend developers:

1. **Best**: Set up global template directory (one-time setup, works for all future clones)
2. **Good**: Run setup script after cloning (per-repository, manual)
3. **Automatic**: MSBuild target runs on first build (requires building)

## Troubleshooting

### Bootstrap hook not running

**Check if you have a global template configured:**
```bash
git config --global init.templateDir
```

**If empty, configure it:**
```bash
git config --global init.templateDir ~/.git-templates/default
mkdir -p ~/.git-templates/default/hooks
cp .git-template/hooks/post-checkout ~/.git-templates/default/hooks/
```

### Hooks still not configured after clone

**Manually trigger the bootstrap hook:**
```bash
.git/hooks/post-checkout "" "" "1"
```

**Or just run the setup script:**
```bash
.\setup-hooks.ps1
```

## Template Structure

```
.git-template/
└── hooks/
    ├── post-checkout           # Bootstrap hook (bash)
    ├── post-checkout.ps1       # Bootstrap hook (PowerShell)
    └── README.md               # This file
```

This directory is **version-controlled** so all developers get the same bootstrap hooks.

## Security Note

Bootstrap hooks execute on clone/checkout. Only use templates from trusted sources. Review the `post-checkout` script before configuring it globally.

## References

- [Git init.templateDir Documentation](https://git-scm.com/docs/git-init#_template_directory)
- [Git Hooks Documentation](https://git-scm.com/docs/githooks)
- [Repository Hooks Documentation](../../docs/development/git-hooks.md)
