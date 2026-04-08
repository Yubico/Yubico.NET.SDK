# YkTool Port Progress

> Updated at end of each DevTeam iteration. Unchecked boxes = work not done.

---

## Scaffold (Phase 1)

- [x] `Yubico.YubiKit.Cli.Commands` project created, added to solution
- [x] `Yubico.YubiKit.Cli.YkTool` project created, all 7 applet branches stubbed, compiles
- [x] `YkDeviceContext` + ManagementSession enrichment implemented in `YkCommandBase`
- [x] `GlobalSettings` with `--serial`, `--transport`, `-i`/`--interactive` flags defined
- [x] `YkCommandInterceptor` wired into `CommandApp`
- [x] Error taxonomy (`ExitCode`) defined and referenced in base command
- [x] `yk --help` renders all 7 applet branches with descriptions (verify runtime)
- [x] `dotnet build.cs build` — zero warnings, zero errors (Build succeeded)

---

## Management (Port 1 -- DevTeam Iteration 1)

- [x] CLI commands ported to YkTool: `info`, `config`, `reset`
- [x] Wired into Program.cs
- [x] Stub removed
- [x] Build verified

---

## OpenPGP (Port 2 -- DevTeam Iteration 2)

- [x] CLI commands ported to YkTool: `info`, `reset`, `access/*`, `keys/*`, `certificates/*`
- [x] Wired into Program.cs
- [x] Stub removed
- [x] Build verified

---

## OATH (Port 3 -- DevTeam Iteration 3)

- [x] CLI commands ported to YkTool: `info`, `reset`, `access/change-password`, `accounts/*`
- [x] Wired into Program.cs
- [x] Stub removed
- [x] Build verified

---

## HsmAuth (Port 4 -- DevTeam Iteration 4)

- [x] CLI commands ported to YkTool: `info`, `reset`, `access/*`, `credentials/*`
- [x] Wired into Program.cs
- [x] Stub removed
- [x] Build verified

---

## OTP (Port 5 -- DevTeam Iteration 5)

- [x] CLI commands ported to YkTool: `info`, `swap`, `delete`, `chalresp`, `hotp`, `static`, `yubiotp`, `calculate`, `ndef`, `settings`
- [x] Wired into Program.cs
- [x] Stub removed
- [x] Build verified

---

## PIV (Port 6 -- DevTeam Iteration 6)

- [x] CLI commands ported to YkTool: `info`, `reset`, `access/*`, `keys/*`, `certificates/*`
- [x] Wired into Program.cs
- [x] Stub removed
- [x] Build verified

---

## FIDO (Port 7 -- DevTeam Iteration 7)

> Tests require a human to physically touch the YubiKey gold contact. Run last.

- [x] CLI commands ported to YkTool: `info`, `reset`, `access/*`, `config/*`, `credentials/*`, `fingerprints/*`
- [x] Wired into Program.cs
- [x] Stub removed
- [x] Build verified

---

## E2E Test Results (YubiKey 5.8.0, session 2026-04-09)

| Command | Exit | Result |
|---------|------|--------|
| `yk management info` | 0 | ✅ Device info displayed |
| `yk openpgp info` | 0 | ✅ AID, key slots shown |
| `yk oath info` | 0 | ✅ Version, no password |
| `yk piv info` | 0 | ✅ FW, slot 9a RSA2048 |
| `yk hsm-auth info` | 0 | ✅ Version, 1 credential |
| `yk otp info` | 0 | ✅ Slots not configured |
| `yk fido info` | 0 | ✅ CTAP 2.0/2.1/2.2 |
| `yk fido access verify-pin --pin ***` | 0 | ✅ PIN correct |
| `yk fido credentials list --pin ***` | 0 | ✅ No credentials stored |
| `yk fido config toggle-always-uv --pin ***` | 0 | ✅ Toggled x2, state restored |
| `yk fido fingerprints list --pin ***` | 1 | ⚠️ Exit 1, expected 7 — CTAP 0x40 not mapped to ExitCode.FeatureUnsupported |

## Known Gaps

- [ ] **CTAP exception exit code mapping**: CTAP errors (e.g., 0x40 operation-denied, 0x31 pin-invalid) return `ExitCode.GenericError (1)` instead of structured codes (`ExitCode.AuthenticationFailed (4)`, `ExitCode.FeatureUnsupported (7)`). Fix in `YkCommandBase` by catching typed CTAP exceptions before the generic catch.

## Final Status

- [x] All 7 applets ported and wired
- [x] All stubs removed (Stubs directory empty)
- [x] `yk --help` shows all 7 branches
- [x] Build succeeds: 0 warnings, 0 errors
