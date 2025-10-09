# Dependabot CI/CD Optimization - Quick Reference

## What Changed?

Dependabot PRs now run **faster, lighter CI/CD**:
- ✅ **Only linux-x64** (instead of 6 platforms)
- ✅ **Build + Test** (no publishing or artifacts)
- ⏱️ **~90% faster** (~3 min instead of ~30 min)

## When Does This Apply?

**Optimized (fast) mode:**
- ✅ PRs created by `dependabot[bot]`
- ✅ Dependency updates (npm, NuGet, GitHub Actions)

**Full (normal) mode:**
- ✅ Developer PRs
- ✅ Direct commits to main/develop
- ✅ Release tags
- ✅ Manual workflow runs

## What Gets Skipped for Dependabot?

### Platforms Skipped
- ⏭️ windows-x64
- ⏭️ windows-arm64
- ⏭️ linux-arm64
- ⏭️ macos-x64
- ⏭️ macos-arm64
- ✅ **linux-x64** (runs)

### Steps Skipped
- ⏭️ Publish Desktop App
- ⏭️ Create installers (Windows/macOS/Linux)
- ⏭️ Archive artifacts
- ⏭️ Upload artifacts

### Steps That Run
- ✅ Build solution
- ✅ Run tests
- ✅ Code coverage (linux-x64 only)
- ✅ UI tests (linux-x64 only)

## How to Verify

Check a Dependabot PR's workflow run:

1. Go to **Actions** tab
2. Find the Dependabot PR's workflow run
3. Confirm:
   - Only **1 job** runs (not 6)
   - Job name: `Build & Test linux-x64`
   - "Publish Desktop App" step shows as **skipped**
   - Logs show: `"Skipping <platform> for Dependabot PR"`

## When to Review Full CI

If a Dependabot PR updates critical dependencies, you may want full platform validation:

1. **Manual workflow run**: Go to Actions → Run workflow → Select the PR's branch
2. **Merge to develop**: Full CI runs automatically on develop branch
3. **Wait for release**: Full CI runs on release tags

## Troubleshooting

### "All platforms are skipped!"
- Check if PR author is `dependabot[bot]`
- Verify the workflow file has correct logic
- Check workflow run logs for detection output

### "Dependabot PR ran full CI"
- PR might have been created manually with Dependabot's changes
- Check `github.actor` and `github.event.pull_request.user.login` in logs
- May need to close and let Dependabot recreate the PR

## Related Documentation

- **Full details**: `.github/DEPENDABOT_OPTIMIZATION.md`
- **Workflow file**: `.github/workflows/ci-cd.yml`
- **Workflow overview**: `.github/WORKFLOWS_OVERVIEW.md`
