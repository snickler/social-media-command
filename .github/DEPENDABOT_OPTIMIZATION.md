# Dependabot CI/CD Optimization

## Overview

This document describes the optimizations made to GitHub Actions workflows to reduce CI time and resource usage for Dependabot-generated pull requests.

## Problem

Dependabot creates frequent pull requests for dependency updates. Running the full CI/CD pipeline (6 platforms × build/test/publish) for each dependency update is:
- **Time-consuming**: Full matrix takes 15-30 minutes per PR
- **Resource-intensive**: Uses significant GitHub Actions minutes
- **Unnecessary**: Dependency updates rarely require platform-specific validation

## Solution

### Optimized Workflow Behavior

The `ci-cd.yml` workflow now has two modes:

#### 1. **Dependabot PRs** (Optimized)
- **Platforms**: Only `linux-x64`
- **Actions**: Build + Test only
- **Skipped**: Publish, artifact packaging, and all other platforms
- **Time Savings**: ~75-80% reduction in CI time

#### 2. **Regular PRs/Commits** (Full)
- **Platforms**: All 6 platforms (win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, osx-arm64)
- **Actions**: Build + Test + Publish + Artifacts
- **Behavior**: No changes from original workflow

## Implementation Details

### Detection Logic

The workflow detects Dependabot PRs using:
```yaml
if [[ "${{ github.actor }}" == "dependabot[bot]" ]] || 
   [[ "${{ github.event.pull_request.user.login }}" == "dependabot[bot]" ]]; then
  IS_DEPENDABOT="true"
fi
```

### Skip Conditions

Each workflow step includes conditional logic:

1. **Platform Skip**: Non-linux-x64 platforms skip all steps for Dependabot
   ```yaml
   if: steps.should_skip.outputs.skip != 'true'
   ```

2. **Publish Skip**: All platforms skip publishing for Dependabot
   ```yaml
   if: steps.should_skip.outputs.skip != 'true' && 
       steps.should_skip.outputs.is_dependabot != 'true'
   ```

### Steps Affected

**Build & Test Steps** (Dependabot: linux-x64 only):
- ✅ Cache NuGet packages
- ✅ Setup .NET
- ✅ Setup Node.js
- ✅ Restore dependencies
- ✅ Build
- ✅ Test
- ✅ UI Tests
- ✅ Code Coverage (linux-x64 only)

**Publish Steps** (Dependabot: skipped entirely):
- ⏭️ Publish Desktop App
- ⏭️ Create Windows Installer
- ⏭️ Create macOS App Bundle
- ⏭️ Create Linux AppImage
- ⏭️ Archive artifacts
- ⏭️ Upload artifacts

## Benefits

### Time Savings
- **Before**: 6 platforms × ~5 min = ~30 minutes total
- **After (Dependabot)**: 1 platform × ~3 min = ~3 minutes total
- **Reduction**: ~90% faster for Dependabot PRs

### Resource Savings
- **GitHub Actions Minutes**: ~27 minutes saved per Dependabot PR
- **Storage**: No unnecessary artifacts for dependency updates
- **Compute**: Only essential validation runs

### Quality Assurance
- Full platform testing still runs on:
  - Developer PRs
  - Direct commits to main/develop
  - Release tags
- Dependabot changes are validated on linux-x64 (most common platform)

## Other Workflows

### Not Modified
The following workflows continue to run normally for Dependabot PRs:

- **`code-quality.yml`**: Linting and formatting checks (always valuable)
- **`visual-regression.yml`**: Only runs when views/styles change (path-filtered)

These workflows are lightweight and provide value for all PRs, including Dependabot.

## Verification

To verify the optimization is working:

1. Check the workflow run for a Dependabot PR
2. Confirm only 1 job runs (linux-x64) instead of 6
3. Confirm "Publish Desktop App" step is skipped
4. Check logs for: `"Skipping <platform> for Dependabot PR"`

## Future Improvements

Potential further optimizations:

1. **Skip visual regression** for Dependabot (dependency updates don't affect UI)
2. **Reduce test verbosity** for Dependabot (less logging output)
3. **Matrix exclude** strategy (cleaner than conditional skips)
4. **Separate workflow** for Dependabot (complete isolation)

## References

- Workflow file: `.github/workflows/ci-cd.yml`
- Copilot instructions: `.github/copilot-instructions.md`
- Workflow overview: `.github/WORKFLOWS_OVERVIEW.md`
