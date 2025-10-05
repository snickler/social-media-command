#!/bin/bash
#
# Install Git Hooks for Social Media Commander
# Run this script from the repository root to install/update Git hooks
#

set -e

FORCE=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -f|--force)
            FORCE=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [-f|--force]"
            exit 1
            ;;
    esac
done

echo ""
echo "🔧 Installing Git Hooks for Social Media Commander"
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m'

# Get the script directory and repo root
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
REPO_ROOT="$( cd "$SCRIPT_DIR/.." && pwd )"
HOOKS_DIR="$REPO_ROOT/.git/hooks"

# Check if .git directory exists
if [ ! -d "$REPO_ROOT/.git" ]; then
    echo -e "${RED}❌ ERROR: Not a Git repository. Run this from the repository root.${NC}"
    exit 1
fi

# Function to backup existing hooks
backup_hook() {
    local hook_path=$1
    if [ -f "$hook_path" ]; then
        local backup_path="${hook_path}.backup.$(date +%Y%m%d-%H%M%S)"
        cp "$hook_path" "$backup_path"
        echo -e "${YELLOW}📦 Backed up existing hook to: $backup_path${NC}"
    fi
}

PRE_COMMIT_PATH="$HOOKS_DIR/pre-commit"
POST_COMMIT_PATH="$HOOKS_DIR/post-commit"

# Check if hooks already exist
if [ -f "$PRE_COMMIT_PATH" ] || [ -f "$POST_COMMIT_PATH" ]; then
    if [ "$FORCE" = false ]; then
        echo -e "${YELLOW}⚠️  Git hooks already exist.${NC}"
        echo -e "${YELLOW}   Run with --force to overwrite existing hooks.${NC}"
        echo ""
        read -p "Do you want to overwrite existing hooks? (y/N) " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            echo -e "\n${RED}❌ Installation cancelled.${NC}\n"
            exit 0
        fi
    fi
fi

# Backup existing hooks
if [ -f "$PRE_COMMIT_PATH" ]; then
    backup_hook "$PRE_COMMIT_PATH"
fi
if [ -f "$POST_COMMIT_PATH" ]; then
    backup_hook "$POST_COMMIT_PATH"
fi

# Note: The actual hook files are already in .git/hooks/
# This script just makes them executable

# Make hooks executable
chmod +x "$PRE_COMMIT_PATH"
chmod +x "$POST_COMMIT_PATH"

echo ""
echo -e "${GREEN}✅ Git hooks are installed and executable${NC}"
echo -e "\n${CYAN}📚 Documentation available at: docs/development/git-hooks.md${NC}\n"

# Verify installation
echo -e "${CYAN}🔍 Verifying installation...${NC}"
if [ -x "$PRE_COMMIT_PATH" ] && [ -x "$POST_COMMIT_PATH" ]; then
    echo -e "${GREEN}✅ Pre-commit hook: Installed and executable${NC}"
    echo -e "${GREEN}✅ Post-commit hook: Installed and executable${NC}"
else
    echo -e "${RED}❌ Installation verification failed${NC}"
    exit 1
fi

# Tips
echo ""
echo -e "${CYAN}💡 Tips:${NC}"
echo -e "${GRAY}   • Hooks run automatically on commit${NC}"
echo -e "${GRAY}   • To bypass (not recommended): git commit --no-verify${NC}"
echo -e "${GRAY}   • View documentation: docs/development/git-hooks.md${NC}"
echo -e "${GRAY}   • Report issues or suggest improvements via GitHub Issues${NC}"
echo ""

echo -e "${GREEN}✅ Installation complete!${NC}\n"
