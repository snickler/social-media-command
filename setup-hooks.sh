#!/bin/bash
#
# Setup Git Hooks - Automatic Installation
# Run this once after cloning the repository to enable Git hooks
#

set -e

echo ""
echo "🚀 Social Media Commander - Git Hooks Setup"
echo ""

HOOKS_PATH=".githooks"

echo "📍 Configuring Git to use hooks from: $HOOKS_PATH"
echo ""

# Set Git to use the .githooks directory
git config core.hooksPath "$HOOKS_PATH"

echo "✅ Git hooks path configured successfully!"
echo ""

# Make hooks executable
chmod +x .githooks/pre-commit
chmod +x .githooks/post-commit

echo "✅ Made hooks executable"
echo ""

# Verify the configuration
CURRENT_HOOKS_PATH=$(git config core.hooksPath)
echo "Current hooks path: $CURRENT_HOOKS_PATH"
echo ""

echo "🎉 Setup complete! Git hooks are now active."
echo ""
echo "The hooks will automatically run on every commit."
echo ""

echo "📚 Documentation:"
echo "   - Quick Reference: docs/development/git-hooks-quick-reference.md"
echo "   - Full Guide: docs/development/git-hooks.md"
echo "   - Examples: docs/development/git-hooks-examples.md"
echo ""
