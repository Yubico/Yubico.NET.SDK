# Phase 38.5 Learnings: Held-Transport Fallback

Handoff record for Phase 38.5. This phase added held-transport fallback to the multi-transport applet
session-entry extensions: when no explicit override is given and the SmartCard transport fails to connect
because another process holds the CCID, the session falls back to the next supported transport in the
applet's default order. An explicit override never falls back; no process is ever killed.

## Phase Summary

- Branch: `yubikit-composite-device-new`.
- Scope: master ISC-23.1. Depends on Phase 38; Phase 39 depends on this.
- Phase ISA: `docs/plans/composite-device/phase-38_5-held-transport-fallback-ISA.md` (interim Cato-gated,
  13 rounds → PASS-level convergence).

## Model (as built)

- New **public** Core helper
  `YubiKeyConnectionExtensions.ConnectSessionTransportAsync(this IYubiKey, IReadOnlyList<ConnectionType> candidates, string sessionName, CancellationToken)`
  opens the first candidate that connects. It is the connect half of the Phase 38 resolve→connect seam:
  callers pass the ordered, validated list from `ResolveSessionTransports`.
  - **Fallback gate**: a connect failure triggers fallback only when `candidate == ConnectionType.SmartCard`
    **and** the error is a held-transport error **and** a further candidate remains. On the last candidate
    (and for an override's single-element list) the error is rethrown unchanged. Non-held errors and
    `OperationCanceledException` propagate immediately. `ThrowIfCancellationRequested()` runs before each
    attempt and (via the loop) between attempts.
  - **Detection** (`IsHeldTransportError`): true only for an `SCardException` whose `(uint)HResult` is
    `SCARD_E_SHARING_VIOLATION` (0x8010000B) or `SCARD_E_SERVER_TOO_BUSY` (0x80100031). `SCardException`
    stores the PC/SC code as `HResult = (int)errorCode`, so the round-trip compares `(uint)HResult`.
  - **Input validation** (public surface): `ArgumentNullException` for null yubiKey/candidates;
    `ArgumentException` for empty, non-concrete element, or duplicate transport (the helper attempts each
    transport at most once — it cannot be used to retry the same transport).
  - On a successful connect it logs the selected transport at debug (observability for verification).
- The four multi-transport connect sites were refactored onto the helper, deleting the duplicated
  `foreach (var transport in candidates) { return transport switch { ... }; }` blocks:
  `ConnectForManagementAsync`, `ConnectForYubiOtpAsync`, `ConnectForFidoAsync`, and WebAuthn via the shared
  Fido2 path (no WebAuthn source change). The now-unused HID connection-interface usings were removed from
  the three applet files.
- The Fido2 `NotSupportedException` remap ("no FIDO-capable connection") stays wrapped around the
  `ResolveSessionTransports` call **only** — it does not enclose `ConnectSessionTransportAsync`, so a
  connect/held/fallback error is never masked as the generic message.

## Key Decisions

- **Centralized in one Core helper** (user-selected over per-applet inline try/catch): detection + fallback
  defined and tested once; the four applets collapse to one call. No new dependency direction (applets
  already depend on Core).
- **SmartCard-only fallback** (user-selected; master ISC-23.1): only the two SmartCard PC/SC held/busy codes
  trigger fallback. A held-coded error surfacing from a non-SmartCard candidate does not fall back. HID-held
  is a deferred candidate.
- **Override never falls back, structurally**: an override resolves to a single-element list (Phase 38), so
  the helper's "rethrow when no further candidate remains" rule yields correct override behavior with no
  "is this an override" branch. Proven at both the helper layer (ISC-13(h)) and the applet layer (ISC-14
  override-no-fallback tests for Management and YubiOtp).
- **Public helper kept public** (consistency with the already-public `ResolveSessionTransports`); the
  public-surface risk is mitigated by full input validation (incl. duplicate rejection) and by documenting
  that the phase guarantees are applet-layer properties, not promises of the helper to arbitrary callers.
- **No process killing**: the SDK never kills/launches another process to free a transport. The Rust
  reference's `kill_pcsc_blockers` is intentionally not ported.

## ISC-9 Finding: Raw `SCardException` Propagation (Wrapped vs Raw)

Confirmed by reading the connect chain and pinned by unit tests: the raw `SCardException` reaches the applet
connect site **unwrapped**. `SmartCardConnectionFactory.CreateAsync` does not wrap; `PcscYubiKey.ConnectAsync`
awaits without wrapping; `CompositeYubiKey.ConnectAsync` returns the member task directly. So
`IsHeldTransportError` inspects the **top-level** exception and adds **no** unwrap logic.
`HeldExceptionPropagationTests` pins this (same held code preserved through `CompositeYubiKey` and
`PcscYubiKey`) so a future wrapping regression is caught — the fake-`IYubiKey` helper tests alone could not
catch it.

## Review Evidence

- Interim Cato (ISA, behavior + new public Core helper): GPT-5.4 — converged over **13 rounds**. Notable:
  the SmartCard-only fallback gate (ISC-6) was added at round 7; a self-introduced "all candidates held"
  test case that contradicted that gate was a round-9 BLOCKER, fixed by replacing it with an
  already-canceled-token case; the exclusive-holder hardware repro (compat switch) replaced an unreliable
  shared self-holder at round 1. Outputs: `/tmp/opencode/cato-review-phase38_5-isa-output{,2..13}.md`.
- Interim /DevTeam (implementation): GPT-5.4 — **PASS first round** (no blockers, no concerns; all notes
  "No change requested"), because the ISA was already hardened through the 13 Cato rounds. Output:
  `/tmp/opencode/devteam-review-phase38_5-output.md`.
- GPT-5.5 cross-vendor reviews (Cato + DevTeam) **QUEUED** for when quota returns.

## Verification Evidence

- Branch `## yubikit-composite-device-new`.
- Full solution build: succeeded, 0 warnings, 0 errors (`dotnet toolchain.cs build`).
- Unit suites (all pass): Core, Management, YubiOtp, Fido2, WebAuthn
  (`dotnet toolchain.cs -- test --project <M> --filter "FullyQualifiedName~UnitTests"`).
  - New Core `ConnectSessionTransportTests` covers ISA cases (a)-(m): sharing-violation/server-too-busy
    fallback, non-held SCard + non-SCard no-fallback, connect-thrown + pre-canceled + between-attempts
    cancellation, success-first, single-element/override rethrow, empty/non-concrete/duplicate validation,
    held-then-real-failure, held-on-non-SmartCard no-fallback, and device-unsupported propagation.
  - New Core `HeldExceptionPropagationTests` pins ISC-9 (CompositeYubiKey + PcscYubiKey unwrapped).
  - Management/YubiOtp transport tests: held SmartCard → HID fallback through the public entry point with
    disposal of the opened fallback connection on session-init failure, plus override-no-fallback.
  - Fido2 transport test: a non-held HID connect error surfaces unchanged (remap stays scoped).
- Changed-file `dotnet format --verify-no-changes` clean (the two new Core test files needed a final-newline
  fix via `dotnet format` — the editor adds a trailing newline that `.editorconfig insert_final_newline=false`
  rejects); `git diff --check` clean; docs-qa 54 validated; no `Core` → `Management` ProjectReference (the
  `InternalsVisibleTo` in Core.csproj is the allowed IVT, not a dependency).
- **ISC-23 anti-criteria evidence**: `git diff` of SDK `src/*.cs` contains no `Process.Start`/`Process.Kill`/
  `pkill`/`gpgconf`/`scdaemon`/`kill_pcsc` — no process-management code was introduced.
- **Live held-CCID hardware verification (serial 103, composite OTP+FIDO+CCID, CCID freed via
  `gpgconf --kill scdaemon`):** a temporary `_TempHeldCcidFallbackTests` (removed before commit) proved on
  the merged composite key:
  - Control (CCID free): default `ConnectSessionTransportAsync` selected `SmartCard`; `CreateManagementSessionAsync`
    read serial **103**.
  - Held (exclusive holder via `AppContext.SetSwitch(CoreCompatSwitches.OpenSmartCardHandlesExclusively, true)`
    + a long-lived `ISmartCardConnection`): a second SmartCard connect produced a held code asserted to be
    `SCARD_E_SHARING_VIOLATION` (0x8010000B) / `SCARD_E_SERVER_TOO_BUSY`; default selection **fell back to
    `HidFido`**; `CreateManagementSessionAsync` read serial **103** over the fallback transport. The compat
    switch was restored to its prior value and the holder disposed in `finally`. `gpg --card-status` was
    never run.

## What Did Not Work / Hazards

- **`Yubico.YubiKit.Core.UnitTests.YubiKey` namespace collides with `Core.YubiKey`.** New Core test files
  under `tests/.../YubiKey/` must use namespace `Yubico.YubiKit.Core.UnitTests.CoreYubiKey` (not `.YubiKey`),
  or unrelated tests fail to compile (`YubiKey.FirmwareVersion` resolves to the test sub-namespace).
- **`(int)ErrorCode.SCARD_E_*` is a constant overflow (CS0221)** because the `ErrorCode` constants are
  `uint`. Use `unchecked((int)...)` in assertions.
- **`ErrorCode` is internal to Core.** Available in `Core.UnitTests` (IVT) but not in the applet test
  projects, which must construct held exceptions with the literal: `new SCardException("held", 0x8010000BL)`.
- **`UsbSmartCardConnection` opens shared by default.** A self-held repro only produces a sharing violation
  if the holder is opened **exclusively** via `CoreCompatSwitches.OpenSmartCardHandlesExclusively` (restore
  the prior switch value in `finally`; the switch is process-global).
- **`gpgconf --kill scdaemon` frees the CCID safely. NEVER `gpg --card-status`** (it reset the beta key off
  the USB bus in a prior phase).

## Deferred Candidates

- **HID-held fallback**: a held/locked HID transport surfaces a non-`SCardException` error and is out of
  scope here; revisit if a real need appears (own phase or master-ISA amendment).
- SCP-implied/auto-switch-to-SmartCard transport selection (carried over from Phase 38).
- YubiOtp integration test project `Xunit.SkippableFact` assembly-load issue (carried over from Phase 38).
- Queued: GPT-5.5 DevTeam + Cato reviews of Phase 38 and Phase 38.5.

## Next Phase Inputs (Phase 39)

- Phase 38.5 satisfies master ISC-23.1. Phase 39 (final integration/docs/Cato) should reconcile the master
  ISA, run the final full verification + Cato, and clear the queued GPT-5.5 reviews.

## Compact Summary

- Goal: held-CCID fallback for multi-transport applets — fall back off a SmartCard transport another process
  holds, to the next supported transport; override never falls back; no process killing.
- Branch: `yubikit-composite-device-new`.
- Status: implemented, verified (build 0/0; Core/Management/YubiOtp/Fido2/WebAuthn units pass; live held-CCID
  fallback proven on serial 103 — control=SmartCard, held→HidFido, serial read over fallback). Interim Cato
  PASS (13 rounds), interim /DevTeam PASS (1 round); GPT-5.5 queued.
