# Pre-Release Quick Reference

## Quick Start

### Create a Beta Pre-Release (Most Common)

```bash
# 1. Switch to develop branch
git checkout develop
git pull origin develop

# 2. Make your changes
git add .
git commit -m "feat: add new dashboard widget"

# 3. Push to trigger automatic beta release
git push origin develop

# Result: Creates v1.2.3-beta.1 automatically
```

### Create a Release Candidate

```bash
# 1. Create RC branch from develop
git checkout develop
git pull origin develop
git checkout -b rc/1.2.0

# 2. Push to create first RC
git push origin rc/1.2.0

# Result: Creates v1.2.0-rc.1 automatically

# 3. Fix any issues found during testing
git add .
git commit -m "fix: resolve critical bug"
git push origin rc/1.2.0

# Result: Creates v1.2.0-rc.2 automatically
```

### Manual Pre-Release (Any Branch)

1. Go to **Actions** → **Pre-Release**
2. Click **Run workflow**
3. Choose:
   - Type: `alpha`, `beta`, or `rc`
   - Branch: (optional, defaults to current)
4. Click **Run workflow**

## Commit Message Templates

### Feature (Minor Version Bump)
```bash
git commit -m "feat: add user export functionality"
# develop: 1.1.0-beta.1 → 1.2.0-beta.1
```

### Bug Fix (Patch Version Bump)
```bash
git commit -m "fix: resolve memory leak in dashboard"
# develop: 1.2.0-beta.1 → 1.2.1-beta.1
```

### Breaking Change (Major Version Bump)
```bash
git commit -m "feat!: redesign API endpoints

BREAKING CHANGE: All v1 API endpoints deprecated"
# develop: 1.2.0-beta.1 → 2.0.0-beta.1
```

### Pre-Release Increment Only
```bash
git commit -m "chore: update dependencies"
# develop: 1.2.0-beta.1 → 1.2.0-beta.2
```

## Version Flow

```
Feature Branch → Alpha → Develop → Beta → RC → Main → Stable
    ↓             ↓         ↓        ↓      ↓     ↓       ↓
v1.2.0-alpha.1  v1.2.0-beta.1  v1.2.0-rc.1  v1.2.0
```

## Branch → Pre-Release Type

| Branch Pattern | Pre-Release Type | Example Version |
|----------------|------------------|-----------------|
| `develop` | beta | `v1.2.3-beta.1` |
| `rc/*` | rc | `v1.2.3-rc.1` |
| `release/*` | rc | `v1.2.3-rc.2` |
| Feature branch (manual) | alpha | `v1.2.3-alpha.1` |

## Testing Pre-Releases

### Download
1. Go to **Releases** (enable "Include pre-releases")
2. Find your version (e.g., `v1.2.3-beta.1`)
3. Download platform-specific artifact
4. Extract and test

### Report Issues
```bash
# Create issue with pre-release tag
Title: [v1.2.3-beta.1] Feature X not working
```

## Promote to Stable

### From RC to Stable (Recommended)
```bash
git checkout main
git merge rc/1.2.0 --no-ff -m "Release v1.2.0"
git push origin main
# Result: Creates v1.2.0 (stable)
```

### From Develop to Stable (Minor releases only)
```bash
git checkout main
git merge develop --no-ff -m "Release v1.1.0"
git push origin main
# Result: Creates v1.1.0 (stable)
```

## Common Commands

### Check what version would be created
```bash
npx semantic-release --dry-run --branches develop
```

### List all pre-releases
```bash
git tag -l "*-beta.*"
git tag -l "*-rc.*"
git tag -l "*-alpha.*"
```

### Delete a pre-release tag (if needed)
```bash
git tag -d v1.2.3-beta.1
git push origin :refs/tags/v1.2.3-beta.1
```

## Troubleshooting

### No pre-release created
- ✅ Check commit uses conventional format (`feat:`, `fix:`, etc.)
- ✅ Verify you're on the correct branch
- ✅ Check Actions logs for errors

### Wrong version number
- ✅ Version is based on git history (all commits since last release)
- ✅ Use `--dry-run` to preview

### Artifacts missing
- ✅ Wait 10-20 minutes for build to complete
- ✅ Check "Build and Release" workflow status

## Quick Links

- 📖 [Full Pre-Release Guide](./PRE_RELEASE_GUIDE.md)
- 📖 [Release Workflow Documentation](./RELEASE_WORKFLOW.md)
- 🔄 [Pre-Release Workflow](../workflows/pre-release.yml)
- 🏗️ [Build Workflow](../workflows/ci-cd.yml)

---

**Need Help?** Create an issue with the `release-process` label
