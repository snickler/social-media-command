# Release Manager Checklist

## Pre-Release Checklist

### Creating a Beta Release

- [ ] Ensure `develop` branch is up to date
  ```bash
  git checkout develop
  git pull origin develop
  ```

- [ ] Verify all features are merged and tested
  ```bash
  git log --oneline origin/main..develop
  ```

- [ ] Check that CI is passing on develop
  - Go to Actions → Verify latest commit passed

- [ ] Push to create beta (automatic)
  ```bash
  git push origin develop
  ```

- [ ] Verify beta release created
  - Check Releases → Pre-releases
  - Verify artifacts are attached (wait 10-20 min)

- [ ] Announce beta to testing team
  - Include version number
  - Include download links
  - List major changes

### Creating a Release Candidate

- [ ] Create RC branch from develop
  ```bash
  git checkout develop
  git pull origin develop
  git checkout -b rc/X.Y.Z
  git push origin rc/X.Y.Z
  ```

- [ ] Verify RC release created
  - Check Releases → Pre-releases
  - Verify `vX.Y.Z-rc.1` tag exists

- [ ] Wait for build to complete
  - Monitor Actions → Build and Release
  - Verify all platforms built successfully

- [ ] Test RC on all platforms
  - [ ] Windows x64
  - [ ] Windows ARM64
  - [ ] Linux x64
  - [ ] Linux ARM64
  - [ ] macOS x64 (Intel)
  - [ ] macOS ARM64 (Apple Silicon)

- [ ] Document any issues found
  - Create issues with `[vX.Y.Z-rc.1]` prefix
  - Link to RC release

- [ ] If bugs found:
  ```bash
  git checkout rc/X.Y.Z
  # Fix bugs
  git commit -m "fix: describe the fix"
  git push origin rc/X.Y.Z
  # Result: Creates vX.Y.Z-rc.2
  ```
  - [ ] Re-test RC
  - [ ] Repeat until stable

## Stable Release Checklist

### Promoting RC to Stable

- [ ] Verify final RC is tested and approved
  - [ ] All platform tests passed
  - [ ] No critical issues reported
  - [ ] Security scan passed
  - [ ] Documentation updated

- [ ] Update version in documentation (if needed)
  - [ ] README.md
  - [ ] User guides
  - [ ] API docs

- [ ] Merge RC to main
  ```bash
  git checkout main
  git pull origin main
  git merge --no-ff rc/X.Y.Z -m "chore: release vX.Y.Z"
  ```

- [ ] Push to main to trigger stable release
  ```bash
  git push origin main
  ```

- [ ] Verify stable release created
  - Check Releases → Latest release
  - Verify `vX.Y.Z` tag (no pre-release suffix)

- [ ] Wait for artifacts to build
  - Monitor Actions → Build and Release
  - Typically takes 10-20 minutes

- [ ] Verify all artifacts attached
  - [ ] Windows x64 zip
  - [ ] Windows ARM64 zip
  - [ ] Linux x64 AppImage/tar.gz
  - [ ] Linux ARM64 AppImage/tar.gz
  - [ ] macOS x64 app bundle
  - [ ] macOS ARM64 app bundle

- [ ] Test download links
  - [ ] Verify files are downloadable
  - [ ] Spot-check one artifact (extract & verify)

- [ ] Update release notes (if needed)
  - Edit the release on GitHub
  - Add upgrade instructions
  - Add breaking changes notice
  - Add contributors

### Post-Release Tasks

- [ ] Merge main back to develop
  ```bash
  git checkout develop
  git merge main
  git push origin develop
  ```

- [ ] Delete RC branch (optional)
  ```bash
  git branch -d rc/X.Y.Z
  git push origin --delete rc/X.Y.Z
  ```

- [ ] Announce release
  - [ ] Post to project website
  - [ ] Update social media
  - [ ] Send announcement email
  - [ ] Post in community channels

- [ ] Close related issues and milestones
  - Link issues to release
  - Close milestone `vX.Y.Z`

- [ ] Update project roadmap
  - Mark version as released
  - Plan next version

- [ ] Monitor initial feedback
  - Watch for critical issues in first 24 hours
  - Be ready to create hotfix if needed

## Hotfix Release Checklist

### For Critical Issues in Stable Release

- [ ] Create hotfix branch from main
  ```bash
  git checkout main
  git pull origin main
  git checkout -b hotfix/X.Y.Z+1
  ```

- [ ] Apply the fix
  ```bash
  git commit -m "fix: critical issue description"
  ```

- [ ] Create RC for hotfix
  ```bash
  git checkout -b rc/X.Y.Z+1
  git push origin rc/X.Y.Z+1
  # Result: Creates vX.Y.Z+1-rc.1
  ```

- [ ] Test RC thoroughly
  - Focus on affected functionality
  - Verify no regressions

- [ ] Merge to main when approved
  ```bash
  git checkout main
  git merge --no-ff rc/X.Y.Z+1
  git push origin main
  # Result: Creates vX.Y.Z+1
  ```

- [ ] Announce hotfix immediately
  - Mark as critical security/bug fix
  - Encourage immediate upgrade

## Emergency Procedures

### If Release Build Fails

1. **Check Actions logs**
   - Identify which platform failed
   - Note the error message

2. **If code issue**:
   ```bash
   git checkout rc/X.Y.Z  # or appropriate branch
   # Fix the issue
   git commit -m "fix: build failure on platform"
   git push origin rc/X.Y.Z
   ```

3. **If infrastructure issue**:
   - Check GitHub Status
   - Wait and retry
   - Contact GitHub support if persistent

### If Wrong Version Released

1. **DO NOT delete the tag immediately**
   - Users may have already downloaded

2. **Create new corrected version**:
   ```bash
   # For pre-release: Just push another commit
   git commit -m "fix: correction"
   git push
   # Result: Increments pre-release number
   ```

3. **For stable release**:
   - Create hotfix with corrected version
   - Mark old release as deprecated in notes

### If Need to Rollback Release

1. **Mark release as "Pre-release" on GitHub**
   - Edit release → Check "Set as pre-release"
   - Add warning to release notes

2. **Create immediate hotfix**
   - Use previous stable version as base
   - Increment patch version

3. **Communicate clearly**
   - Notify all users
   - Provide rollback instructions
   - Explain the issue

## Monthly Tasks

- [ ] Review pre-release frequency
  - Are we creating too many/few?
  - Adjust process if needed

- [ ] Clean up old pre-releases
  - Keep last 3 beta/rc versions
  - Delete very old alphas

- [ ] Review release automation
  - Check for workflow failures
  - Update dependencies
  - Improve documentation

## Quarterly Tasks

- [ ] Audit release process
  - Gather feedback from team
  - Identify pain points
  - Propose improvements

- [ ] Update release documentation
  - Reflect any process changes
  - Add new best practices
  - Remove outdated info

- [ ] Security review
  - Audit release artifacts
  - Review signing process
  - Check for vulnerabilities

---

**Tip**: Copy this checklist for each release and track in GitHub Issues with the `release-management` label.

**Last Updated**: October 6, 2025
