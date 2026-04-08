# Session Handoff — Unified `yk` CLI

**Date:** 2026-04-09
**Branch:** `yubikit-applets`
**Resume with:** `/resume-handoff`

---

## Fitness-for-Purpose Assessment

**Target:** Developer and SDK users needing a single unified CLI for all 7 YubiKey applets.

| Area | Status | Notes |
|------|--------|-------|
| All 7 applets ported and building | ✅ | Management, FIDO, OATH, OpenPGP, PIV, HsmAuth, OTP |
| Non-interactive `--pin`/`--password` flags | ✅ | All commands accept credentials as CLI flags |
| E2E verified against real YubiKey 5.8.0 | ✅ | All 7 `info` + FIDO PIN/credentials/toggle verified |
| CTAP exception → exit code mapping | ⚠️ | All CTAP errors return exit 1; should map to 4/7 |
| DevTeam review + security audit | ✅ | 2 blocking issues found and fixed |
| `Cli.Commands` shared library populated | ⚠️ | Empty — commands live in `YkTool/Commands/` (accepted deviation) |
| PR to develop | ❌ | Branch has no common history with `develop` — cannot PR via API |

**Overall Readiness: 🟢 Functional — all applets work, one quality gap remains**

**Critical next step:** Fix CTAP exception exit code mapping in `YkCommandBase.cs` (task #39), then decide branch/PR strategy with Dennis.

---

## What Was Built This Session

### New Projects

| Project | Path | Purpose |
|---------|------|---------|
| `Yubico.YubiKit.Cli.Commands` | `src/Cli.Commands/src/` | Shared commands library (empty shell — placeholder for future refactor) |
| `Yubico.YubiKit.Cli.YkTool` | `src/Cli/YkTool/` | Unified `yk` binary — the main deliverable |

### YkTool Architecture

```
src/Cli/YkTool/
├── Program.cs                          CommandApp — 7 applet branches wired
├── Yubico.YubiKit.Cli.YkTool.csproj   Outputs binary named "yk"
├── Infrastructure/
│   ├── ExitCode.cs                     0=Success, 1=Error, 3=NoDevice, 4=AuthFailed, 5=Cancelled, 7=Unsupported
│   ├── GlobalSettings.cs               --serial, --transport, -i/--interactive
│   ├── YkCommandBase.cs                Abstract base: device selection + ManagementSession enrichment
│   ├── YkCommandInterceptor.cs         ICommandInterceptor (no-op, reserved)
│   ├── YkDeviceContext.cs              IYubiKey + DeviceSelection + DeviceInfo?
│   └── YkDeviceSelector.cs             Transport-configurable DeviceSelectorBase
└── Commands/
    ├── Management/                     info, config, reset
    ├── OpenPgp/                        info, reset, access/*, keys/*, certificates/*
    ├── Oath/                           info, reset, access/change-password, accounts/*
    ├── HsmAuth/                        info, reset, access/*, credentials/*
    ├── Otp/                            info, swap, delete, chalresp, hotp, static, yubiotp, calculate, ndef, settings
    ├── Piv/                            info, reset, access/*, keys/*, certificates/*
    └── Fido/                           info, reset, access/*, config/*, credentials/*, fingerprints/*
```

### Key Architectural Decisions

1. **`YkCommandBase<TSettings>`** — sealed `ExecuteAsync` handles device selection, ManagementSession enrichment, error handling. Commands only implement `ExecuteCommandAsync`.
2. **`deviceContext.Info`** — `DeviceInfo?` from Management fetched once per invocation. Commands never open their own ManagementSession.
3. **Commands in `YkTool/Commands/`** — deviated from plan (`Cli.Commands/src/`). `Cli.Commands` exists but is empty. Individual tools not refactored.
4. **Non-interactive by default** — all commands accept `--pin`, `--password`, `--management-key` flags. Interactive fallback via `SessionHelper.PromptOrUse()` when flag absent.
5. **Spectre.Console.Cli** throughout — auto-generates `--help` at every level.

---

## How to Run

```bash
# Build
dotnet build.cs build

# Run
dotnet run --project src/Cli/YkTool/Yubico.YubiKit.Cli.YkTool.csproj -- management info
dotnet run --project src/Cli/YkTool/Yubico.YubiKit.Cli.YkTool.csproj -- fido credentials list --pin 123456
dotnet run --project src/Cli/YkTool/Yubico.YubiKit.Cli.YkTool.csproj -- oath accounts list
dotnet run --project src/Cli/YkTool/Yubico.YubiKit.Cli.YkTool.csproj -- --help
```

---

## E2E Test Results (YubiKey 5.8.0, 2026-04-09)

| Command | Exit | Result |
|---------|------|--------|
| `yk management info` | 0 | ✅ 78CLUFX5000P S/N:125 FW:5.8.0 |
| `yk openpgp info` | 0 | ✅ AID 3.4, 4 key slots |
| `yk oath info` | 0 | ✅ v0.0.1, no password |
| `yk piv info` | 0 | ✅ FW:5.8.0, slot 9a RSA2048 |
| `yk hsm-auth info` | 0 | ✅ v0.0.1, 1 credential |
| `yk otp info` | 0 | ✅ Slots not configured |
| `yk fido info` | 0 | ✅ CTAP 2.0/2.1/2.2, AAGUID shown |
| `yk fido access verify-pin --pin ***` | 0 | ✅ PIN correct |
| `yk fido credentials list --pin ***` | 0 | ✅ No credentials stored |
| `yk fido config toggle-always-uv --pin ***` | 0 | ✅ Toggled x2, state restored |
| `yk fido fingerprints list --pin ***` | 1 | ⚠️ CTAP 0x40 — exits 1, expected 7 |

---

## Known Gap — Task #39 (One Fix Remaining)

**Problem:** CTAP exceptions caught by generic `catch (Exception)` in `YkCommandBase.ExecuteAsync`, all mapped to `ExitCode.GenericError (1)`.

**File:** `src/Cli/YkTool/Infrastructure/YkCommandBase.cs`

**Fix — add typed catches BEFORE the generic one:**

```csharp
// Add these before the existing catch (Exception ex) block:
catch (Ctap2Exception ex) when (ex.Status == CtapStatus.PinInvalid ||
                                 ex.Status == CtapStatus.PinAuthInvalid)
{
    OutputHelpers.WriteError($"Authentication failed: {ex.Message}");
    return ExitCode.AuthenticationFailed;  // 4
}
catch (Ctap2Exception ex) when (ex.Status == CtapStatus.OperationDenied ||
                                 ex.Status == CtapStatus.UnsupportedOption)
{
    OutputHelpers.WriteError($"Feature not supported on this device: {ex.Message}");
    return ExitCode.FeatureUnsupported;   // 7
}
catch (NotSupportedException ex)
{
    OutputHelpers.WriteError($"Feature not supported: {ex.Message}");
    return ExitCode.FeatureUnsupported;   // 7
}
```

**Note:** Verify actual CTAP exception class names in `Yubico.YubiKit.Fido2` before implementing — may be `Fido2Exception`, `CtapException`, or similar.

**Verification:** `yk fido fingerprints list --pin <pin>` on non-Bio device should exit `7` after fix.

---

## Git State

**Branch:** `yubikit-applets` (pushed to origin)

**Uncommitted:** `Plans/yk-cli-progress.md` (+20 lines — E2E results table)

**Recent commits:**
```
47b0136c  fix(cli): wire --force flag on management reset, stage all yk CLI files
64b0311d  feat(cli): add unified yk CLI with all 7 YubiKey applets
c74570e6  fix(cli): fix 4 bugs across PivTool, FidoTool, HsmAuthTool, ManagementTool
```

**Branch relationship:** `yubikit-applets` has NO common history with `develop`. Cannot create a PR to `develop` via GitHub API. Pre-existing PR #446 (targeting `yubikit` branch) is open.

---

## Relevant Plan Files

| File | Purpose |
|------|---------|
| `Plans/joyful-rolling-pnueli.md` | Authoritative command pattern guide — read this before touching commands |
| `Plans/yk-cli-progress.md` | Progress checklist with E2E results |
| `Plans/handoff.md` | This file |

---

## Next Steps (Priority Order)

1. **Commit** — `git add Plans/yk-cli-progress.md && git commit -m "docs: add E2E test results to progress file"`
2. **Fix #39** — CTAP exception exit code mapping in `YkCommandBase.cs` (`/DevTeam Ship`)
3. **Branch strategy** — Decide with Dennis how to get work into `develop` (cherry-pick / rebase / `--allow-unrelated-histories`)
4. **Cli.Commands population** — Optional: migrate commands from `YkTool/Commands/` to `Cli.Commands/src/` for Approach A shared-library goal
5. **Individual tool refactoring** — ManagementTool, OathTool etc. import from Cli.Commands once populated
