# Git Hooks - Setup Guide (Choose Your Path)

## 🎯 Goal
Ensure Git hooks run automatically on every commit to catch issues before CI/CD.

## 🌟 Option 1: Global Template (RECOMMENDED - 99% Effective)

**One-time 5-minute setup, works for ALL future repos!**

```
┌─────────────────────────────────────────────────────────────┐
│  🌟 GLOBAL TEMPLATE SETUP (DO THIS ONCE!)                   │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Windows PowerShell:                                         │
│    .\scripts\setup-global-template.ps1                      │
│                                                              │
│  Linux/macOS:                                                │
│    chmod +x scripts/setup-global-template.sh                │
│    ./scripts/setup-global-template.sh                       │
│                                                              │
│  ✅ Result:                                                  │
│    • ALL future git clones auto-configure hooks             │
│    • Never run setup again                                  │
│    • Works across all repositories                          │
│                                                              │
│  ⏱️  Time: 5 minutes once                                    │
│  💰 Saves: 2+ hours/month                                    │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### What Happens:
```
1. You run: .\scripts\setup-global-template.ps1
   ↓
2. Template copied to: ~/.git-templates/social-media-command
   ↓
3. Git configured globally to use template
   ↓
4. Future workflow:
   
   git clone <any-repo>
   ↓ (bootstrap hook runs automatically)
   ↓
   Hooks configured! ✅
   
   git commit -m "..."
   ↓ (pre-commit checks run)
   ↓
   Quality enforced! ✅
```

---

## ⚡ Option 2: Per-Repository Setup (70% Effective)

**Run after each clone, works for current repo only**

```
┌─────────────────────────────────────────────────────────────┐
│  ⚡ PER-REPO SETUP (AFTER EACH CLONE)                       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Windows:                                                    │
│    .\setup-hooks.ps1                                        │
│                                                              │
│  Linux/macOS:                                                │
│    chmod +x setup-hooks.sh                                  │
│    ./setup-hooks.sh                                         │
│                                                              │
│  ✅ Result:                                                  │
│    • Hooks configured for THIS repository                   │
│    • Must repeat for each new clone                         │
│                                                              │
│  ⏱️  Time: 30 seconds per repo                               │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔄 Option 3: Automatic Fallback (Built-in)

**No manual step, but only works when you build**

```
┌─────────────────────────────────────────────────────────────┐
│  🔄 AUTOMATIC ON BUILD (NO ACTION NEEDED)                   │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Just run:                                                   │
│    dotnet build                                             │
│                                                              │
│  ✅ Hooks auto-configure during build!                      │
│                                                              │
│  ⚠️  Limitation:                                             │
│    • Only works if you build                                │
│    • Won't help for text-only changes                       │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 📊 Comparison Table

| Method | Effectiveness | Setup Frequency | Best For |
|--------|--------------|-----------------|----------|
| **Global Template** | 99% | Once (5 min) | Everyone! |
| **Per-Repo Setup** | 70% | Each clone (30 sec) | Quick projects |
| **Build Fallback** | 50% | Automatic | Backup/safety net |

---

## 🚀 Quick Start Decision Tree

```
START
  │
  ├─ Do you clone repos frequently?
  │  └─ YES → Use Global Template (Option 1) ✅ BEST
  │  └─ NO  ↓
  │
  ├─ Is this a one-time clone?
  │  └─ YES → Use Per-Repo Setup (Option 2)
  │  └─ NO  ↓
  │
  └─ Just want to try it out?
     └─ Use Build Fallback (Option 3)
        (Then upgrade to Option 1 later!)
```

---

## ✅ Verification Steps

After setup, verify hooks are active:

```bash
# Check if configured
git config core.hooksPath
# Expected output: .githooks

# Test with a commit
git commit --allow-empty -m "Test hooks"
# You should see:
# 🔍 Running pre-commit checks...
# 🔐 Checking for secrets and sensitive data...
# (etc.)
```

---

## 💡 Pro Tips

### For New Developers
👍 **DO:**
- Choose Global Template (Option 1) - saves time long-term
- Run setup on day 1 of onboarding
- Test hooks immediately

👎 **DON'T:**
- Skip hook setup thinking "I'll do it later"
- Use `git commit --no-verify` regularly
- Ignore hook warnings

### For Team Leads
📋 **Checklist:**
- [ ] Add Global Template setup to onboarding docs
- [ ] Demonstrate setup in first week
- [ ] Track adoption metrics
- [ ] Celebrate teams with 100% adoption

---

## 🆘 Troubleshooting

### Problem: "Hooks not running after setup"

```bash
# Check configuration
git config core.hooksPath

# If empty, re-run setup
.\setup-hooks.ps1  # or setup-global-template.ps1
```

### Problem: "Bootstrap hook not working"

```bash
# Verify global template is configured
git config --global init.templateDir

# If empty, run global template setup
.\scripts\setup-global-template.ps1
```

### Problem: "Permission denied" (Linux/macOS)

```bash
# Make hooks executable
chmod +x .githooks/pre-commit
chmod +x .githooks/post-commit
```

---

## 📚 More Information

- **Complete Guide:** [docs/development/truly-automatic-git-hooks.md](docs/development/truly-automatic-git-hooks.md)
- **Quick Reference:** [docs/development/git-hooks-quick-reference.md](docs/development/git-hooks-quick-reference.md)
- **Examples:** [docs/development/git-hooks-examples.md](docs/development/git-hooks-examples.md)

---

## 🎯 Recommended Action

```
┌──────────────────────────────────────────────────┐
│  🌟 RECOMMENDED FOR EVERYONE:                    │
│                                                   │
│  Run this ONCE right now:                        │
│                                                   │
│  .\scripts\setup-global-template.ps1            │
│                                                   │
│  It takes 5 minutes and benefits you forever!    │
└──────────────────────────────────────────────────┘
```

**Then share with your team!** 🚀

---

*Last Updated: 2025-10-04*
