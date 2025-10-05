# Setup Git Hooks - Automatic Installation
# Run this once after cloning the repository to enable Git hooks

Write-Host ""
Write-Host "Setup Git Hooks" -ForegroundColor Cyan
Write-Host ""

git config core.hooksPath .githooks

if ($LASTEXITCODE -eq 0) {
    Write-Host "Success: Git hooks configured" -ForegroundColor Green
    Write-Host ""
    Write-Host "Current hooks path: " -NoNewline
    git config core.hooksPath
    Write-Host ""
    Write-Host "Setup complete! Hooks are now active." -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "Failed to configure Git hooks" -ForegroundColor Red
    exit 1
}
