# CI/CD Documentation

This document describes the comprehensive CI/CD workflows implemented for the Social Media Commander project.

## Overview

The project uses GitHub Actions to provide a professional-grade CI/CD pipeline with the following capabilities:

- ✅ Cross-platform builds (Windows, Linux, macOS)
- ✅ Multi-architecture support (x64, arm64)
- ✅ Automated testing and code coverage
- ✅ Code quality checks and security scanning
- ✅ Semantic versioning and automated releases
- ✅ Dependency management and security updates

## Workflows

### 1. CI/CD Pipeline (`.github/workflows/ci-cd.yml`)

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop` branches
- Tag pushes (`v*`)
- Release events

**Jobs:**

#### Test Job
- Runs on Ubuntu (fastest for testing)
- Restores dependencies and builds solution
- Executes all 184+ unit tests
- Generates code coverage reports
- Posts coverage summary to PRs

#### Build Job (Matrix Strategy)
- **Platforms:** Windows, Linux, macOS
- **Architectures:** x64, arm64
- **Output formats:**
  - Windows: Self-contained executables + ZIP archives
  - Linux: AppImage packages + tar.gz archives
  - macOS: App bundles + tar.gz archives

#### Release Job
- Triggered only on release events
- Downloads all build artifacts
- Uploads to GitHub Releases with auto-generated release notes

### 2. Code Quality (`.github/workflows/code-quality.yml`)

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop` branches

**Jobs:**

#### Lint Job
- Code formatting verification with `dotnet format`
- Build analysis for code quality

#### Security Job
- CodeQL static analysis for security vulnerabilities
- Automated security scanning

#### Dependency Review Job
- Reviews dependency changes in PRs
- Fails on moderate or higher severity vulnerabilities

#### Static Analysis Job
- Runs .NET analyzers
- Treats warnings as errors in Release builds

### 3. Release Automation (`.github/workflows/release.yml`)

**Triggers:**
- Push to `main` branch
- Manual workflow dispatch

**Features:**
- Semantic versioning based on conventional commits
- Automated changelog generation
- Version bumping in `Directory.Build.props`
- Triggers build workflow for releases

## Release Process

### Automated Releases (Recommended)

1. **Commit Format:** Use conventional commits for automatic versioning:
   ```
   feat: add new OAuth provider support
   fix: resolve authentication timeout issue
   docs: update API documentation
   BREAKING CHANGE: remove deprecated methods
   ```

2. **Version Bumping:**
   - `fix:` → patch version (1.0.0 → 1.0.1)
   - `feat:` → minor version (1.0.0 → 1.1.0)
   - `BREAKING CHANGE:` → major version (1.0.0 → 2.0.0)

3. **Process:**
   - Push to `main` branch
   - Release workflow analyzes commits
   - Creates tag and GitHub release
   - Triggers build workflow
   - Publishes artifacts to release

### Manual Releases

1. Go to Actions → Release workflow
2. Click "Run workflow"
3. Select release type: patch/minor/major/prerelease
4. Confirm and run

## Dependency Management

### Dependabot Configuration

Automated dependency updates run on schedule:

- **NuGet packages:** Weekly on Mondays
- **NPM packages:** Weekly on Tuesdays  
- **GitHub Actions:** Weekly on Wednesdays
- **Dev containers:** Monthly

All updates include:
- Automatic reviewer assignment
- Proper labeling
- Conventional commit messages
- Limited concurrent PRs

### Security Updates

- Immediate security updates for critical vulnerabilities
- Dependency review on all PRs
- CodeQL security scanning
- Automated vulnerability alerts

## Artifacts and Distribution

### Build Artifacts

Each platform build produces:

**Windows:**
- `SocialMediaCommander.Desktop.exe` (self-contained)
- `start.bat` (launcher script)
- ZIP archive for distribution

**Linux:**
- Self-contained executable
- AppImage package (x64 only)
- tar.gz archive for distribution

**macOS:**
- `.app` bundle with proper Info.plist
- Self-contained executable
- tar.gz archive for distribution

### Artifact Retention

- Build artifacts: 90 days
- Release artifacts: Permanent (GitHub Releases)
- Coverage reports: Available in PR comments

## Monitoring and Maintenance

### Workflow Status

Add these badges to your README:

```markdown
[![CI/CD](https://github.com/snickler/social-media-command/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/snickler/social-media-command/actions/workflows/ci-cd.yml)
[![Code Quality](https://github.com/snickler/social-media-command/actions/workflows/code-quality.yml/badge.svg)](https://github.com/snickler/social-media-command/actions/workflows/code-quality.yml)
[![Release](https://github.com/snickler/social-media-command/actions/workflows/release.yml/badge.svg)](https://github.com/snickler/social-media-command/actions/workflows/release.yml)
```

### Troubleshooting

**Build failures:**
1. Check test results in CI logs
2. Verify .NET version compatibility
3. Review dependency conflicts

**Release issues:**
1. Ensure conventional commit format
2. Check semantic-release logs
3. Verify GitHub token permissions

**Security scan failures:**
1. Review CodeQL alerts
2. Update vulnerable dependencies
3. Fix identified security issues

## Environment Variables

Required secrets and variables:

- `GITHUB_TOKEN`: Automatically provided by GitHub
- No additional secrets required for basic functionality

## Performance Optimizations

- Parallel job execution where possible
- Dependency caching for faster builds
- Matrix strategy for efficient multi-platform builds
- Conditional job execution based on changes

## Compliance and Standards

This CI/CD setup follows industry best practices:

- ✅ Automated testing before deployment
- ✅ Security scanning and vulnerability management
- ✅ Code quality enforcement
- ✅ Semantic versioning
- ✅ Reproducible builds
- ✅ Artifact signing and verification
- ✅ Comprehensive logging and monitoring

The workflows are designed to scale with the project and can be easily extended for additional requirements such as deployment to specific environments, additional testing stages, or integration with external services.