# Setup Global Git Template for Automatic Hooks
# This is a ONE-TIME setup that makes ALL future repository clones auto-configure hooks!

Write-Host ""
Write-Host "🌟 Git Hooks - Global Template Setup" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""

# Get paths
$repoRoot = Get-Location
$templateSource = Join-Path $repoRoot ".git-template"
$templateDest = Join-Path $env:USERPROFILE ".git-templates\social-media-command"

# Check if source template exists
if (-not (Test-Path $templateSource)) {
    Write-Host "❌ Error: .git-template directory not found" -ForegroundColor Red
    Write-Host "   Make sure you're running this from the repository root" -ForegroundColor Yellow
    exit 1
}

Write-Host "📁 Source template: $templateSource" -ForegroundColor Gray
Write-Host "📁 Destination: $templateDest" -ForegroundColor Gray
Write-Host ""

# Create destination directory
Write-Host "Creating template directory..." -ForegroundColor Yellow
$destParent = Split-Path $templateDest
if (-not (Test-Path $destParent)) {
    New-Item -ItemType Directory -Force -Path $destParent | Out-Null
}

# Copy template
Write-Host "Copying template files..." -ForegroundColor Yellow
if (Test-Path $templateDest) {
    Write-Host "   (Removing existing template)" -ForegroundColor Gray
    Remove-Item -Recurse -Force $templateDest
}
Copy-Item -Recurse -Force $templateSource $templateDest

# Make hooks executable (if using Git Bash/WSL)
if (Get-Command chmod -ErrorAction SilentlyContinue) {
    chmod +x "$templateDest/hooks/post-checkout" 2>$null
}

Write-Host "✅ Template copied successfully" -ForegroundColor Green
Write-Host ""

# Configure Git to use the template
Write-Host "Configuring Git to use global template..." -ForegroundColor Yellow
git config --global init.templateDir $templateDest

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Git global template configured!" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to configure global template" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "🎉 Setup Complete!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""

Write-Host "✅ What just happened:" -ForegroundColor Cyan
Write-Host "   • Git template installed to: $templateDest" -ForegroundColor White
Write-Host "   • Global Git config updated" -ForegroundColor White
Write-Host "   • ALL future repository clones will auto-configure hooks!" -ForegroundColor White
Write-Host ""

Write-Host "📋 Next time you clone ANY repository with .githooks/:" -ForegroundColor Cyan
Write-Host "   1. Clone: git clone <repo>" -ForegroundColor White
Write-Host "   2. That's it! Hooks automatically configured." -ForegroundColor White
Write-Host ""

Write-Host "🔍 Verify setup:" -ForegroundColor Cyan
Write-Host "   git config --global init.templateDir" -ForegroundColor Yellow
Write-Host "   # Should show: $templateDest" -ForegroundColor Gray
Write-Host ""

Write-Host "📚 More info:" -ForegroundColor Cyan
Write-Host "   docs/development/truly-automatic-git-hooks.md" -ForegroundColor White
Write-Host ""

Write-Host "💡 Pro tip:" -ForegroundColor Cyan
Write-Host "   Share this script with your team!" -ForegroundColor White
Write-Host "   Everyone who runs it gets automatic hooks forever." -ForegroundColor White
Write-Host ""
