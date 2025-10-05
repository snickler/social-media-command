# 🎯 Git Hooks - Quick Reference Card

**5-Second Decision**: Run this once → `.\scripts\setup-global-template.ps1` → Done forever! ✅

---

## 🚀 Installation (Pick ONE)

| Method | Command | When | Effectiveness |
|--------|---------|------|--------------|
| **Global Template** ⭐ | `.\scripts\setup-global-template.ps1` | Once, for all repos | 99% |
| **Per-Repo** | `.\setup-hooks.ps1` | Each clone | 70% |
| **Auto Build** | `dotnet build` | Automatic | 50% |

---

## ✅ Verification

```bash
git config core.hooksPath     # Should show: .githooks
git commit --allow-empty -m "Test"  # Should run checks
```

---

## 🛡️ What Gets Checked (Pre-Commit)

| Check | Blocks Commit? | Description |
|-------|---------------|-------------|
| 🔐 **Secrets** | ✅ Yes | API keys, passwords, tokens |
| 📦 **CPM** | ✅ Yes | Package versions in Directory.Packages.props |
| 🔨 **Build** | ✅ Yes | `dotnet build` must succeed |
| ⚡ **Async** | ✅ Yes | `ConfigureAwait(false)` in services |
| ✨ **Format** | ⚠️ Warn | Code formatting consistency |
| 🚫 **Markers** | ⚠️ Warn | TODO/FIXME/HACK comments |
| 🖥️ **Debug** | ✅ Yes | `console.log` in production |
| 📏 **Size** | ✅ Yes | Files > 1MB (use Git LFS) |
| 🔍 **Nulls** | ✅ Yes | Nullable type violations |
| ⚙️ **Config** | ⚠️ Warn | Git configuration issues |

---

## 💡 Post-Commit Reminders

Contextual reminders based on what you changed:
- **Tests changed** → Run `dotnet test`
- **Frontend** → `npm run build`
- **ViewModels** → Update `App.axaml.cs` if constructors changed
- **Security files** → Check for secrets
- **Always** → PR checklist

---

## 🆘 Troubleshooting

| Problem | Solution |
|---------|----------|
| Hooks not running | `.\setup-hooks.ps1` |
| Permission denied (Linux) | `chmod +x .githooks/*` |
| Build fails | `dotnet build` then fix errors |
| PowerShell blocked | `Set-ExecutionPolicy RemoteSigned -Scope CurrentUser` |
| Too slow | Set `SKIP_BUILD_CHECK=1` |

---

## 🚨 Emergency Bypass (Use Sparingly!)

```bash
git commit --no-verify -m "Emergency fix"
```

⚠️ **Warning**: Bypasses ALL quality checks. Use only in emergencies!

---

## 📚 Full Documentation

- **Setup Guide**: [HOOKS_SETUP_GUIDE.md](HOOKS_SETUP_GUIDE.md)
- **Troubleshooting**: [docs/development/git-hooks-troubleshooting.md](docs/development/git-hooks-troubleshooting.md)
- **Complete Guide**: [docs/development/git-hooks.md](docs/development/git-hooks.md)

---

## 🎓 Team Onboarding (30 seconds)

**New developer?**
1. Open PowerShell in repo root
2. Run: `.\scripts\setup-global-template.ps1`
3. That's it! Hooks work for ALL repos now

**Testing it:**
```bash
git commit --allow-empty -m "Test hooks"
# You should see: 🔍 Running pre-commit checks...
```

---

**Questions?** See [Troubleshooting Guide](docs/development/git-hooks-troubleshooting.md)

*Last Updated: 2025-01-04*
