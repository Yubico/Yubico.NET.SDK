# Branch Restructuring Report
**Date:** 2026-01-27
**Repository:** Yubico.NET.SDK

## Summary

Successfully restructured the branch hierarchy to achieve clean separation of concerns (SOC) between shared infrastructure and feature-specific code. This enables cleaner PR reviews where each feature branch shows only relevant changes.

---

## Problem

Shared infrastructure changes (Core, docs, configs) were mixed into feature branches (yubikit-piv-example, yubikit-fido). When merging feature branches, reviewers would see unrelated changes, making PRs noisy and hard to review.

**Original structure:**
```
yubikit (d1358185) → yubikit-fido (+281 commits) → yubikit-piv (+48) → yubikit-piv-example (+38)
                                                                              └── contained Core, docs, configs mixed with Piv code
```

---

## Solution

Created a clean `yubikit` base with all shared infrastructure, then rebuilt feature branches to contain ONLY their respective feature code.

**New structure:**
```
yubikit (a98e8716) ─── shared infra, NO Fido2/Piv business logic
    │
    ├── yubikit-fido (98adf819) ─── +Fido2 only (1 commit, 87 files)
    │
    └── yubikit-piv (61139b99) ─── +Piv only (1 commit, 41 files)
            │
            └── yubikit-piv-example (0b740e1b) ─── +PivTool CLI (1 commit, 35 files)
```

---

## Classification Rules Applied

### Shared Infrastructure (→ yubikit)
- `.claude/`, `.copilot/`, `.github/`, `.junie/`, `.vscode/`
- `docs/` (all documentation)
- Root files: `CLAUDE.md`, `README.md`, `GEMINI.md`, `BUILD.md`, `.gitignore`, `Directory.*.props`
- `Yubico.YubiKit.Core/` (all src + tests)
- `Yubico.YubiKit.Tests.Shared/` (all)
- `Yubico.YubiKit.SecurityDomain/` (all)
- For other projects: metadata only (README, CLAUDE.md, .csproj, xunit.runner.json)
- `experiments/` (shared tooling)
- Build scripts: `toolchain.cs`, `sign.cs`

### Feature-Specific
- `Yubico.YubiKit.Fido2/` → yubikit-fido branch
- `Yubico.YubiKit.Piv/` → yubikit-piv branch
- `Yubico.YubiKit.Piv/examples/PivTool/` → yubikit-piv-example branch

---

## Steps Executed

### Phase 1: Preparation & Safety
1. ✅ Created backup branches before any destructive operations:
   - `backup/yubikit-20260127` → d1358185
   - `backup/yubikit-fido-20260127` → fbb7b3db
   - `backup/yubikit-piv-20260127` → 1714fb7b
   - `backup/yubikit-piv-example-20260127` → b750a221

2. ✅ Pushed original feature branches to GitHub for remote backup

### Phase 2: Build Clean yubikit Base
1. ✅ Merged `yubikit-piv-example` into `yubikit` (FF to b750a221)
2. ✅ Reset `Yubico.YubiKit.Fido2/` and `Yubico.YubiKit.Piv/` to skeleton state
3. ✅ Committed clean yubikit base (a98e8716)

### Phase 3: Rebuild Feature Branches
1. ✅ Reset `yubikit-fido` to `yubikit` base
2. ✅ Added Fido2 business logic from backup → committed as 98adf819
3. ✅ Reset `yubikit-piv` to `yubikit` base
4. ✅ Added Piv business logic from backup → committed as 61139b99
5. ✅ Reset `yubikit-piv-example` to `yubikit-piv`
6. ✅ Added PivTool example from backup → committed as 0b740e1b

### Phase 4: Push to GitHub
1. ✅ Pushed all updated feature branches
2. ✅ Pushed all backup branches
3. ✅ Pushed updated yubikit

---

## Final Branch State

| Branch | Commit | Description | Remote |
|--------|--------|-------------|--------|
| `yubikit` | a98e8716 | Clean base with shared infra | ✅ origin/yubikit |
| `yubikit-fido` | 98adf819 | +Fido2 implementation | ✅ origin/yubikit-fido |
| `yubikit-piv` | 61139b99 | +Piv implementation | ✅ origin/yubikit-piv |
| `yubikit-piv-example` | 0b740e1b | +PivTool CLI example | ✅ origin/yubikit-piv-example |
| `backup/yubikit-20260127` | d1358185 | Original yubikit state | ✅ |
| `backup/yubikit-fido-20260127` | fbb7b3db | Original fido state | ✅ |
| `backup/yubikit-piv-20260127` | 1714fb7b | Original piv state | ✅ |
| `backup/yubikit-piv-example-20260127` | b750a221 | Original piv-example state | ✅ |

---

## Additional Changes

1. **Removed PivTool from solution file** (`Yubico.YubiKit.sln`) - the project reference was pointing to a deleted file in the clean yubikit base

2. **PivTool reference copy** - Copied PivTool example to working directory (untracked) for reference while building ManagementExample CLI

---

## Benefits Achieved

1. **Clean PR reviews** - Each feature branch now shows only its relevant files
2. **Clear separation** - Shared infrastructure vs feature code is clearly delineated
3. **Safe rollback** - All original states preserved in backup branches on GitHub
4. **Consistent base** - All feature branches share the same updated infrastructure

---

## How to Resume Work

**For shared infrastructure work:**
```bash
git checkout yubikit
# Make changes to Core, docs, configs, etc.
```

**For FIDO2 work:**
```bash
git checkout yubikit-fido
# Make changes to Yubico.YubiKit.Fido2/
```

**For PIV work:**
```bash
git checkout yubikit-piv
# Make changes to Yubico.YubiKit.Piv/
```

**To restore original state (if needed):**
```bash
git checkout -b restored-branch backup/yubikit-piv-example-20260127
```
