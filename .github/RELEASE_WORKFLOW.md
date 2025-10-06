# Release Workflow Documentation

## Overview

This project uses a two-stage release process to work around GitHub branch protection rules that prevent automated commits to the `main` branch.

## Workflow Stages

### 1. Release Workflow (`release.yml`)

**Trigger**: Runs on every push to `main` or manual dispatch

**Purpose**: Determines if a release is needed and creates the GitHub release

**Process**:
1. Analyzes commits using [Conventional Commits](https://www.conventionalcommits.org/) format
2. Determines the next version number (patch, minor, or major)
3. Generates release notes from commit messages
4. Creates a CHANGELOG.md file (stored as artifact, not committed)
5. **Creates a GitHub Release with a tag** (e.g., `v1.2.3`)
6. Triggers the Build and Release workflow

**Key Point**: This workflow does **NOT** commit back to `main` to avoid conflicts with branch protection rules.

### 2. Build and Release Workflow (`ci-cd.yml`)

**Trigger**: 
- All commits and PRs (for CI/CD testing)
- **Tags matching `v*`** (for release builds)

**Purpose**: Builds, tests, and publishes artifacts

**Process**:
1. Builds for all supported platforms (Windows, Linux, macOS × x64, ARM64)
2. Runs tests with code coverage
3. Creates platform-specific packages (installers, AppImages, etc.)
4. **When triggered by a tag**:
   - Downloads all build artifacts
   - Attaches them to the GitHub Release created by the Release workflow

## Release Process Flow

```
Push to main
    ↓
Release Workflow runs
    ↓
Analyzes commits → Determines version → Creates tag & GitHub Release
    ↓
Build and Release Workflow detects new tag
    ↓
Builds all platforms → Attaches artifacts to release
    ↓
Release is complete with all artifacts
```

## Manual Release

You can manually trigger a release:

1. Go to Actions → Release workflow
2. Click "Run workflow"
3. Select the release type (patch/minor/major/prerelease)
4. Click "Run workflow"

## Commit Message Format

To trigger releases, use [Conventional Commits](https://www.conventionalcommits.org/):

- `fix: ...` → Patch release (1.0.0 → 1.0.1)
- `feat: ...` → Minor release (1.0.0 → 1.1.0)
- `feat!: ...` or `BREAKING CHANGE:` → Major release (1.0.0 → 2.0.0)
- `chore:`, `docs:`, etc. → No release

## Version Management

The version number is:
- **Determined by**: semantic-release based on commit history
- **Stored in**: Git tags (e.g., `v1.2.3`)
- **Used for**: Build artifacts, GitHub releases, assembly versions

Directory.Build.props contains a base version, but the actual release version comes from git tags.

## Troubleshooting

### Release workflow fails with "protected branch" error

**Fixed**: The workflow no longer tries to commit to main. If you see this error, ensure you're using the updated workflow.

### Build and Release workflow doesn't create a release

**Fixed**: The workflow now triggers on tag creation. Ensure the Release workflow successfully created a tag.

### No artifacts attached to release

Check that:
1. The tag matches `v*` pattern
2. All build jobs completed successfully
3. Artifacts were uploaded in the build-and-test job

## Migration Notes

### What Changed (October 2025)

**Before**:
- semantic-release tried to commit CHANGELOG.md and Directory.Build.props to main
- Failed due to branch protection
- Release job only ran when release was already published (chicken-egg problem)

**After**:
- semantic-release only creates tags and GitHub releases (no commits)
- Build workflow detects tag and creates release with artifacts
- Works with branch protection enabled
