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

## Final Status

- [x] All 7 applets ported and wired
- [x] All stubs removed (Stubs directory empty)
- [x] `yk --help` shows all 7 branches
- [x] Build succeeds: 0 warnings, 0 errors
