# Phase 38 Learnings: Extension Method Smart Defaults And Explicit Overrides

Handoff record for Phase 38. This phase formalized the applet session-entry extension methods so transport
selection on a physical (multi-connection) `IYubiKey` is an explicit, documented policy: an app-specific
smart default order plus an optional explicit caller override. It replaced the Phase 36 placeholders
(Fido2's dual-transport throw; the Management/YubiOtp gating comments).

## Phase Summary

- Branch: `yubikit-composite-device-new`.
- Scope: master ISC-21, ISC-21.1, ISC-22, ISC-23, ISC-24. Held-transport fallback is Phase 38.5 (ISC-23.1).
- Phase ISA: `docs/plans/composite-device/phase-38-extension-defaults-ISA.md` (interim Cato-gated, 10 rounds → PASS).

## Model (as built)

- New Core helper `YubiKeyConnectionExtensions.ResolveSessionTransports(yubiKey, preferredConnection, sessionName, params defaultOrder)` returns an **ordered, non-empty `IReadOnlyList<ConnectionType>`** of concrete transports to attempt:
  - **Override (`preferredConnection != null`)**: validated and returned as a single-element list (an override never falls back). Taxonomy: not-exactly-one-concrete (group flag / combined / `Unknown`) → `ArgumentException`; concrete-but-not-valid-for-applet (e.g. `HidOtp` for Fido2, `HidFido` for YubiOtp), even if device-supported → `ArgumentException`; concrete + applet-valid + device-unsupported → `NotSupportedException`. All validation happens before any connect.
  - **Default (`null`)**: the device-supported subset of the applet's ordered default list, preference order preserved; empty → `NotSupportedException`.
- Each multi-transport entry method gained an optional `ConnectionType? preferredConnection = null`, placed immediately before the trailing `CancellationToken` (CA1068 + warnings-as-errors). Default orders: Management `SmartCard → HidFido → HidOtp`; YubiOtp `SmartCard → HidOtp`; Fido2/WebAuthn `HidFido → SmartCard` (replacing the placeholder throw). WebAuthn forwards `preferredConnection` to the shared Fido2 path (no independent transport logic).
- Each connect site **iterates the ordered candidate list** and opens the first candidate today — this is the deliberate Phase 38.5 seam (see below).
- Single-transport applets (Piv, Oath, OpenPgp, SecurityDomain, YubiHsm) are unchanged (SmartCard-only, no override).

## Key Decisions

- **Override shape**: optional `ConnectionType? preferredConnection = null` (not `params ConnectionType[]`). Chosen for minimal surface and clear semantics.
- **Back-compatibility overloads**: because inserting a parameter before `CancellationToken` source-breaks callers that passed `CancellationToken` as the final positional argument, each method keeps a back-compat overload with the exact pre-Phase-38 positional shape (all-required params, ending in `CancellationToken`) that forwards with `preferredConnection: null`. Better-function-member resolution disambiguates (verified: no `CS0121`, including `cancellationToken:`-named callers). Added in response to the interim /DevTeam HIGH finding.
- **SCP semantics unchanged (and a doc bug fixed)**: SCP is only valid on SmartCard. Supplying `scpKeyParams` while a non-SmartCard transport is selected (including the default HID FIDO) throws `NotSupportedException` ("SCP is only supported on SmartCard protocols") from `ApplicationSession.InitializeCoreAsync` — a **pre-existing Core contract**, not introduced here (single-HID + SCP threw before Phase 38 too). The old Fido2 doc claim that SCP is "ignored over HID" was factually wrong (the code throws) and was corrected. "SCP implies/auto-switches to SmartCard" remains a deferred follow-up; silently dropping SCP on HID was explicitly rejected (it would hide a requested secure channel).
- **No held-transport fallback** in this phase (Phase 38.5).

## Review Evidence

- Interim Cato (ISA, pre-edit, API-boundary change): GPT-5.4 — converged over **10 rounds** to **PASS**. Notable: round 8 BLOCKER correctly flagged an earlier round-5 suggestion (make `scpKeyParams` imply SmartCard) as scope creep beyond ISC-21..24 and a contradiction of the fixed valid-transport model; resolved by deferring SCP-implied selection and keeping Phase 38 to defaults+overrides. Outputs: `/tmp/opencode/cato-review-phase38-isa-output{,2..10}.md`.
- Interim /DevTeam Reviewer (implementation): GPT-5.4 — **CHANGES REQUIRED → CHANGES REQUIRED → CHANGES REQUIRED → PASS WITH NOTES → (notes fixed)** over 5 rounds. Drove: back-compat overloads (positional source-compat); WebAuthn test coverage (device-unsupported, `Unknown`, SCP-default, SmartCard-only fallback); the ISC-12.1 seam fix (helper now returns the ordered list, connect sites iterate); a Core `ApplicationSession` SCP-guard unit test; the FIDO2 override-error taxonomy fix (only the default path remaps to the generic "no FIDO-capable connection" message; override failures propagate their accurate diagnostic); and correcting the stale SCP-over-HID README/XML docs. Outputs: `/tmp/opencode/devteam-review-phase38-output{,2..5}.md`.
- GPT-5.5 cross-vendor reviews (DevTeam + Cato) **QUEUED** for when quota returns.

## Verification Evidence

- Branch `## yubikit-composite-device-new`.
- Full solution build: succeeded, 0 warnings, 0 errors.
- Unit suites (all pass): Core, Management, YubiOtp, Fido2, WebAuthn. New `IYubiKeyExtensionsTransportTests` (fake-probe) in Management/YubiOtp/WebAuthn + extended Fido2 cover ISA cases (a)-(i): default top-choice, default fallback order (incl. Management "third choice only"), explicit non-default override on a 2+-transport probe, device-unsupported override → `NotSupportedException` (no connect), non-concrete override incl. combined flag → `ArgumentException` (no connect), applet-invalid override → `ArgumentException` even when device-supported (no connect), Fido2/WebAuthn no-FIDO-transport → `NotSupportedException`, and SCP-does-not-change-selection. New Core `ApplicationSessionScpTests` covers the SCP-on-non-SmartCard throw.
- **Hardware (serial 103, composite OTP+FIDO+CCID, CCID free via `gpgconf --kill scdaemon`):**
  - Management integration smoke 13/13 (default SmartCard path + HID transports).
  - Fido2 `FidoSessionSimpleTests` smoke 9/9 (default HID FIDO + SmartCard).
  - A temporary Management override smoke (removed before commit, not committed) proved on the merged composite key: default → SmartCard, and explicit overrides to `HidOtp`, `HidFido`, and `SmartCard` all opened a working session and read serial 103.
  - YubiOtp integration smoke could NOT run due to an unrelated harness issue (`FileNotFoundException: Xunit.SkippableFact` assembly not loaded in that project's output) — not a Phase 38 defect; YubiOtp unit tests pass and the YubiOtp extension shares the same Core helper.
- Changed-file `dotnet format --verify-no-changes` clean; `git diff --check` clean; docs-qa 54 validated; Core has no dependency on `Yubico.YubiKit.Management`.
- No allow-list edit was needed: `src/Tests.Shared/appsettings.json` already authorizes serial 103 (committed, `AllowUnknownSerials: true`); nothing to revert.

## What Did Not Work / Hazards

- **CA1068 forces parameter order.** With warnings-as-errors, `CancellationToken` must be the last parameter, so the override goes immediately before it — which is the source-break risk that required the back-compat overloads. Do not place new optional params after `CancellationToken`.
- **Adding an overload makes unqualified `<see cref="CreateFidoSessionAsync"/>` ambiguous (CS0419).** Qualify the cref with a parameter list or use `<c>...</c>`.
- **Reviewer oscillation on SCP.** The interim reviewer first asked for "SCP implies SmartCard" (round 5) then flagged it as scope creep (round 8). Judgment call: defer it, keep the throw, fix the docs. Recorded so Phase 38.5/owner can revisit deliberately.
- **`gpgconf --kill scdaemon` is the safe way to free the CCID. NEVER `gpg --card-status`** (it reset the beta key off the USB bus in a prior phase).

## Phase 38.5 Inputs (held-transport fallback)

- **Seam is ready.** `ResolveSessionTransports` returns the full ordered candidate list; each applet's connect site already iterates it (`foreach`), opening the first today. Phase 38.5 adds held-transport fallback by wrapping each iteration's connect in a `try/catch` for held-transport errors and continuing to the next candidate. Override returns a single-element list, so overrides never fall back automatically (correct). No public-signature or override-semantics change is needed.
- **Error taxonomy for "held".** A held CCID surfaces as `SCardException` with `ErrorCode.SCARD_E_SHARING_VIOLATION (0x8010000B)`; also consider `SCARD_E_SERVER_TOO_BUSY`. `DiscoveryIdentityReader` already retries transient sharing violations (a precedent for detection). Fallback should trigger ONLY for these "busy/held" codes (do not mask genuine connect failures), ONLY on the `null`-default path, and ONLY for multi-transport applets.
- **Library boundary.** Never kill `scdaemon`/`yubikey-agent` (no `kill_pcsc_blockers`); the Rust CLI does this but the SDK must not.

## Deferred Candidates

- Phase 38.5: held-transport fallback (above).
- SCP-implied/auto-switch-to-SmartCard transport selection for Fido2/WebAuthn (own phase or master-ISA amendment if wanted).
- YubiOtp integration test project `Xunit.SkippableFact` assembly-load fix (unrelated harness issue surfaced during smoke).
- Queued: GPT-5.5 DevTeam + Cato reviews of Phase 38.

## Next Phase Inputs (Phase 39)

- Phase 38 satisfies master ISC-21..24. Phase 39 (final integration/docs/Cato) should reconcile the master ISA checkboxes and run the final full verification + Cato.

## Compact Summary

- Goal: app-specific smart-default transport selection + explicit override on the composite `IYubiKey`; remove Phase 36 placeholders.
- Branch: `yubikit-composite-device-new`.
- Status: implemented, verified (build 0/0; Core/Management/YubiOtp/Fido2/WebAuthn units pass; hardware default + all overrides proven on serial 103 with CCID free). Interim Cato PASS (10 rounds), interim /DevTeam converged (5 rounds); GPT-5.5 queued.
