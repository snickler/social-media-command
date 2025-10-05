#!/bin/bash
#
# Setup Global Git Template for Automatic Hooks
# This is a ONE-TIME setup that makes ALL future repository clones auto-configure hooks!
#

set -e

echo ""
echo "🌟 Git Hooks - Global Template Setup"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Get paths
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TEMPLATE_SOURCE="$REPO_ROOT/.git-template"
TEMPLATE_DEST="$HOME/.git-templates/social-media-command"

# Check if source template exists
if [ ! -d "$TEMPLATE_SOURCE" ]; then
    echo "❌ Error: .git-template directory not found"
    echo "   Make sure you're running this from the repository root"
    exit 1
fi

echo "📁 Source template: $TEMPLATE_SOURCE"
echo "📁 Destination: $TEMPLATE_DEST"
echo ""

# Create destination directory
echo "Creating template directory..."
mkdir -p "$(dirname "$TEMPLATE_DEST")"

# Copy template
echo "Copying template files..."
if [ -d "$TEMPLATE_DEST" ]; then
    echo "   (Removing existing template)"
    rm -rf "$TEMPLATE_DEST"
fi
cp -r "$TEMPLATE_SOURCE" "$TEMPLATE_DEST"

# Make hooks executable
chmod +x "$TEMPLATE_DEST/hooks/post-checkout" 2>/dev/null || true

echo "✅ Template copied successfully"
echo ""

# Configure Git to use the template
echo "Configuring Git to use global template..."
git config --global init.templateDir "$TEMPLATE_DEST"

if [ $? -eq 0 ]; then
    echo "✅ Git global template configured!"
else
    echo "❌ Failed to configure global template"
    exit 1
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🎉 Setup Complete!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

echo "✅ What just happened:"
echo "   • Git template installed to: $TEMPLATE_DEST"
echo "   • Global Git config updated"
echo "   • ALL future repository clones will auto-configure hooks!"
echo ""

echo "📋 Next time you clone ANY repository with .githooks/:"
echo "   1. Clone: git clone <repo>"
echo "   2. That's it! Hooks automatically configured."
echo ""

echo "🔍 Verify setup:"
echo "   git config --global init.templateDir"
echo "   # Should show: $TEMPLATE_DEST"
echo ""

echo "📚 More info:"
echo "   docs/development/truly-automatic-git-hooks.md"
echo ""

echo "💡 Pro tip:"
echo "   Share this script with your team!"
echo "   Everyone who runs it gets automatic hooks forever."
echo ""
