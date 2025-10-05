# Bootstrap Post-Checkout Hook (PowerShell version for Windows)
# This hook automatically configures the repository's Git hooks on first checkout/clone
# It then disables itself so it only runs once

param($prevHead, $newHead, $isBranchCheckout)

# Only run on branch checkout (not file checkout)
if ($isBranchCheckout -ne "1") {
    exit 0
}

# Get the repository root
try {
    $repoRoot = git rev-parse --show-toplevel 2>$null
    if (-not $repoRoot) {
        exit 0
    }
} catch {
    exit 0
}

# Check if .githooks directory exists
$gitHooksDir = Join-Path $repoRoot ".githooks"
if (-not (Test-Path $gitHooksDir)) {
    exit 0
}

# Check if hooks are already configured
$currentHooksPath = git config --local core.hooksPath 2>$null

if ($currentHooksPath -ne ".githooks") {
    Write-Host ""
    Write-Host "🔧 Configuring Git hooks automatically..." -ForegroundColor Cyan
    
    # Configure Git to use .githooks directory
    git config --local core.hooksPath .githooks
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Git hooks configured: .githooks" -ForegroundColor Green
        Write-Host ""
        Write-Host "📚 Git hooks are now active!" -ForegroundColor Cyan
        Write-Host "   - Pre-commit: Quality checks before commits"
        Write-Host "   - Post-commit: Helpful reminders after commits"
        Write-Host ""
        Write-Host "Documentation: docs/development/git-hooks.md"
        Write-Host ""
    } else {
        Write-Host "⚠️  Failed to configure Git hooks. Run manually: .\setup-hooks.ps1" -ForegroundColor Yellow
        Write-Host ""
    }
}

exit 0
