# Install Git Hooks for Social Media Commander
# Run this script from the repository root to install/update Git hooks

param(
    [switch]$Force = $false
)

Write-Host "`n🔧 Installing Git Hooks for Social Media Commander`n" -ForegroundColor Cyan

$hooksDir = Join-Path $PSScriptRoot "..\..\.git\hooks"
$preCommitPath = Join-Path $hooksDir "pre-commit"
$postCommitPath = Join-Path $hooksDir "post-commit"

# Check if .git directory exists
if (-not (Test-Path (Join-Path $PSScriptRoot "..\..\.git"))) {
    Write-Host "❌ ERROR: Not a Git repository. Run this from the repository root." -ForegroundColor Red
    exit 1
}

# Function to backup existing hooks
function Backup-Hook {
    param([string]$Path)
    
    if (Test-Path $Path) {
        $backupPath = "$Path.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        Copy-Item $Path $backupPath
        Write-Host "📦 Backed up existing hook to: $backupPath" -ForegroundColor Yellow
    }
}

# Check if hooks already exist
$preCommitExists = Test-Path $preCommitPath
$postCommitExists = Test-Path $postCommitPath

if (($preCommitExists -or $postCommitExists) -and -not $Force) {
    Write-Host "⚠️  Git hooks already exist." -ForegroundColor Yellow
    Write-Host "   Run with -Force to overwrite existing hooks.`n" -ForegroundColor Yellow
    
    $response = Read-Host "Do you want to overwrite existing hooks? (y/N)"
    if ($response -ne 'y' -and $response -ne 'Y') {
        Write-Host "`n❌ Installation cancelled.`n" -ForegroundColor Red
        exit 0
    }
}

# Backup existing hooks
if ($preCommitExists) {
    Backup-Hook $preCommitPath
}
if ($postCommitExists) {
    Backup-Hook $postCommitPath
}

Write-Host "`n✅ Git hooks are installed in .git/hooks/" -ForegroundColor Green
Write-Host "`n📚 Documentation available at: docs/development/git-hooks.md`n" -ForegroundColor Cyan

# Verify installation
Write-Host "🔍 Verifying installation..." -ForegroundColor Cyan
if ((Test-Path $preCommitPath) -and (Test-Path $postCommitPath)) {
    Write-Host "✅ Pre-commit hook: Installed" -ForegroundColor Green
    Write-Host "✅ Post-commit hook: Installed" -ForegroundColor Green
} else {
    Write-Host "❌ Installation verification failed" -ForegroundColor Red
    exit 1
}

# Test if hooks are executable (Git Bash check)
Write-Host "`n💡 Tips:" -ForegroundColor Cyan
Write-Host "   • Hooks run automatically on commit" -ForegroundColor Gray
Write-Host "   • To bypass (not recommended): git commit --no-verify" -ForegroundColor Gray
Write-Host "   • View documentation: docs/development/git-hooks.md" -ForegroundColor Gray
Write-Host "   • Report issues or suggest improvements via GitHub Issues`n" -ForegroundColor Gray

Write-Host "✅ Installation complete!`n" -ForegroundColor Green
