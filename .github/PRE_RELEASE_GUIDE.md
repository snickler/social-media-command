# Pre-Release Process Documentation

## Overview

This project supports creating pre-release versions for testing and validation before stable releases. Pre-releases use semantic versioning with pre-release identifiers (alpha, beta, rc).

## Pre-Release Types

### Alpha (α) - Early Development
- **Purpose**: Internal testing, early feature previews
- **Stability**: Unstable, expect bugs and breaking changes
- **Version Format**: `1.2.3-alpha.1`, `1.2.3-alpha.2`, etc.
- **Recommended For**: Feature branches, experimental code

### Beta (β) - Feature Complete
- **Purpose**: External testing, feature freeze
- **Stability**: Mostly stable, feature-complete but may have bugs
- **Version Format**: `1.2.3-beta.1`, `1.2.3-beta.2`, etc.
- **Recommended For**: `develop` branch, pre-production testing

### Release Candidate (RC)
- **Purpose**: Final testing before stable release
- **Stability**: Stable, no new features, only critical bug fixes
- **Version Format**: `1.2.3-rc.1`, `1.2.3-rc.2`, etc.
- **Recommended For**: `rc/*` or `release/*` branches, production-ready testing

## Branch Strategy

```
main (stable)
  │
  ├─ develop (beta pre-releases)
  │   │
  │   └─ feature/* (alpha pre-releases)
  │
  └─ rc/* or release/* (RC pre-releases)
```

## Creating Pre-Releases

### Method 1: Automatic (Recommended)

Pre-releases are automatically created when you push to specific branches:

#### For Beta Releases (develop branch):
```bash
git checkout develop
git add .
git commit -m "feat: add new feature"
git push origin develop
```

**Result**: Creates `v1.2.3-beta.1` automatically

#### For Release Candidates (rc/* branch):
```bash
git checkout -b rc/1.2.3
git add .
git commit -m "fix: critical bug fix"
git push origin rc/1.2.3
```

**Result**: Creates `v1.2.3-rc.1` automatically

### Method 2: Manual Workflow Dispatch

For more control, manually trigger pre-releases:

1. Go to **Actions** → **Pre-Release** workflow
2. Click **Run workflow**
3. Select:
   - **Pre-release type**: alpha, beta, or rc
   - **Branch**: (optional) specify branch, defaults to current
4. Click **Run workflow**

**Result**: Creates a pre-release from the specified branch

### Method 3: Manual Tag Creation

For advanced users:

```bash
# Create a pre-release tag manually
git tag v1.2.3-beta.1
git push origin v1.2.3-beta.1
```

**Result**: Build workflow triggers and creates the pre-release

## Pre-Release Workflow

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Developer pushes to develop/rc/* branch                  │
│    OR manually triggers Pre-Release workflow                │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. Pre-Release Workflow (pre-release.yml)                   │
│    • Analyzes commits                                       │
│    • Determines next pre-release version                    │
│    • Creates tag (e.g., v1.2.3-beta.1)                      │
│    • Creates GitHub pre-release                             │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. Build and Release Workflow (ci-cd.yml)                   │
│    • Detects tag creation (v*)                              │
│    • Builds for all platforms                               │
│    • Runs tests                                             │
│    • Attaches artifacts to the pre-release                  │
└─────────────────────────────────────────────────────────────┘
```

## Version Progression

### Example: Developing version 1.2.0

1. **Feature Development (Alpha)**
   ```
   v1.2.0-alpha.1  ← Feature branch: Add user authentication
   v1.2.0-alpha.2  ← Feature branch: Fix login bug
   v1.2.0-alpha.3  ← Feature branch: Add password reset
   ```

2. **Integration Testing (Beta)**
   ```
   v1.2.0-beta.1   ← Develop branch: All features merged
   v1.2.0-beta.2   ← Develop branch: Fix integration issues
   v1.2.0-beta.3   ← Develop branch: UI improvements
   ```

3. **Pre-Production (Release Candidate)**
   ```
   v1.2.0-rc.1     ← RC branch: First release candidate
   v1.2.0-rc.2     ← RC branch: Critical bug fix
   ```

4. **Production (Stable)**
   ```
   v1.2.0          ← Main branch: Stable release
   ```

## Commit Message Format

Use [Conventional Commits](https://www.conventionalcommits.org/) for automatic versioning:

### For Pre-Releases:

```bash
# New feature (increments minor version for pre-release)
feat: add user profile page

# Bug fix (increments patch version for pre-release)
fix: resolve login timeout issue

# Breaking change (increments major version for pre-release)
feat!: redesign authentication system

BREAKING CHANGE: Old auth tokens no longer valid

# Pre-release only (increments pre-release number)
chore: update dependencies
docs: improve README
style: format code
```

### Examples:

- `feat: add dashboard` on `develop` → `v1.2.0-beta.1`
- `fix: login bug` on `develop` → `v1.2.0-beta.2`
- `feat: new API` on `rc/1.2.0` → `v1.2.0-rc.1`

## Testing Pre-Releases

### Installation

Download pre-release artifacts from the GitHub Releases page:

1. Go to **Releases** → **Pre-releases**
2. Find your version (e.g., `v1.2.3-beta.1`)
3. Download the artifact for your platform
4. Extract and test

### Feedback

Report issues using the pre-release tag:

```bash
# Create an issue referencing the pre-release
[v1.2.3-beta.1] Login fails on Firefox
```

## Promoting Pre-Release to Stable

### From Release Candidate to Stable:

1. **Verify RC is stable**:
   ```bash
   # Ensure v1.2.0-rc.2 is tested and approved
   ```

2. **Merge to main**:
   ```bash
   git checkout main
   git merge rc/1.2.0 --no-ff
   ```

3. **Push to main**:
   ```bash
   git push origin main
   ```

4. **Release workflow creates stable version**:
   ```
   Result: v1.2.0 (stable release)
   ```

### From Develop to Stable (Skip RC):

Only for minor releases without critical changes:

```bash
git checkout main
git merge develop --no-ff
git push origin main
```

## Best Practices

### DO ✅

- **Use develop branch** for beta testing before merging to main
- **Create RC branches** for final testing of major releases
- **Test pre-releases** on all target platforms before promoting to stable
- **Use conventional commits** for automatic versioning
- **Document breaking changes** in pre-release notes
- **Keep pre-releases short-lived** (weeks, not months)

### DON'T ❌

- **Don't skip testing phases** (don't go alpha → stable)
- **Don't merge unstable code** to develop
- **Don't create pre-releases from main** (use main for stable only)
- **Don't reuse pre-release tags** (always increment)
- **Don't promote alpha/beta directly to stable** without RC phase for major releases

## Troubleshooting

### Pre-release workflow doesn't create a release

**Check**:
1. Ensure you're using conventional commits (feat, fix, etc.)
2. Verify branch name matches expected patterns (develop, rc/*)
3. Check GitHub Actions logs for semantic-release output

### Pre-release version is wrong

**Cause**: Semantic versioning is based on commit history

**Solution**:
```bash
# View what semantic-release would do
npx semantic-release --dry-run --branches develop
```

### Can't find pre-release download

**Location**: GitHub Releases page, filter by "Pre-releases"

1. Go to **Code** → **Releases**
2. Check **Include Pre-releases** checkbox
3. Find your version (e.g., `v1.2.3-beta.1`)

### Pre-release artifacts missing

**Wait**: Build workflow takes 10-20 minutes to complete

**Verify**: Check Actions → Build and Release workflow status

## Configuration Files

### Branch Configuration

Pre-release branches are configured in:
- `.github/workflows/pre-release.yml` - Pre-release creation
- `.github/workflows/release.yml` - Supports beta/rc branches
- `.github/workflows/ci-cd.yml` - Builds for all tags

### Semantic Release Config

Generated dynamically in workflow, but follows:

```json
{
  "branches": [
    "main",                          // Stable releases (v1.2.3)
    {
      "name": "develop",
      "prerelease": "beta"           // Beta releases (v1.2.3-beta.1)
    },
    {
      "name": "rc/*",
      "prerelease": "rc"             // RC releases (v1.2.3-rc.1)
    }
  ]
}
```

## FAQs

### Q: Can I create multiple pre-releases from the same branch?

**A**: Yes! Each push with conventional commits increments the pre-release number automatically.

### Q: How do I create an alpha release?

**A**: Use workflow dispatch and select "alpha" type, or push to a feature branch with the pre-release workflow configured.

### Q: Can I delete a pre-release?

**A**: Yes, go to Releases → find the pre-release → click "Delete". Note: This doesn't delete the tag.

### Q: What happens if I push to main with pre-release commits?

**A**: semantic-release will create a stable version (v1.2.3), not a pre-release.

### Q: How do I skip a pre-release and go straight to stable?

**A**: Merge directly to main. However, this is NOT recommended for major releases.

## Version Examples

### Semantic Versioning with Pre-Releases:

```
v1.0.0          ← Stable release
v1.1.0-beta.1   ← First beta for 1.1.0
v1.1.0-beta.2   ← Second beta
v1.1.0-rc.1     ← First release candidate
v1.1.0          ← Stable 1.1.0 release
v1.1.1          ← Patch release
v1.2.0-alpha.1  ← Alpha for 1.2.0
v2.0.0-rc.1     ← RC for major version
v2.0.0          ← Major stable release
```

### Ordering:
```
1.0.0-alpha.1
  < 1.0.0-alpha.2
  < 1.0.0-beta.1
  < 1.0.0-beta.2
  < 1.0.0-rc.1
  < 1.0.0
  < 1.0.1
  < 1.1.0
```

## Support

For questions or issues with the pre-release process:

1. Check this documentation
2. Review GitHub Actions logs
3. Create an issue with the `release-process` label
4. Contact the release management team

---

**Last Updated**: October 6, 2025  
**Workflow Version**: 2.0
