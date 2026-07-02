# Phase 38.5 ISA: Held-Transport Fallback

This phase adds **held-transport fallback** to the multi-transport applet session-entry extensions
(Management, YubiOtp, Fido2, WebAuthn). When no explicit transport override is given and the preferred
transport fails to connect *because another process is holding it* (the CCID held by GnuPG `scdaemon` is
the motivating real case), the session falls back to the next supported transport in the applet's default
order instead of surfacing the connect error. An explicit override never falls back.

Phase 38 deliberately built the seam for this: `ResolveSessionTransports` already returns the full
**ordered, non-empty** candidate list, and every applet connect site already iterates it (opening the
first today). This phase wraps that iteration with held-transport detection and continuation. No public
applet signature changes.

Read this together with:

- `docs/plans/composite-device/ISA.md` (master — ISC-23.1; Phase 38.5 row; Decisions 2026-06-11)
- `docs/plans/composite-device/phase-38-extension-defaults-learnings.md` (Phase 38.5 Inputs — seam, error taxonomy, library boundary)
- `docs/plans/composite-device/phase-37_5-pid-merge-learnings.md` (held-CCID gap origin)
- `src/Core/src/YubiKey/YubiKeyConnectionExtensions.cs` (`ResolveSessionTransports` — the seam)
- `src/Core/src/PlatformInterop/Desktop/SCard/SCardException.cs` and `SCardError.cs` (held error surface)
- `src/Core/src/YubiKey/DiscoveryIdentityReader.cs` (general transient connect-retry precedent — it retries *all* exceptions, not a held-code taxonomy; cited only as precedent that connect failures are tolerated, not as a model for *what* to catch)
- `src/Core/src/SmartCard/UsbSmartCardConnection.cs` (raw `SCardException` connect throw sites)

## Problem

Phase 37.5 (`phase-37_5-pid-merge-learnings.md`) recorded a connectivity gap: when GnuPG `scdaemon`
(or any other PC/SC client) holds the CCID exclusively, opening a SmartCard connection fails with
`SCARD_E_SHARING_VIOLATION`. On a composite key whose Management/YubiOtp default order prefers SmartCard,
this means the session-entry extension throws even though the very same operation would succeed over an
available HID transport (HID FIDO or HID OTP), which `scdaemon` does not hold.

Phase 38 formalized the per-applet default order and the explicit override but explicitly deferred
fallback (Phase 38 ISC-12 / ISC-22): a default that resolves to a held transport currently surfaces the
underlying `SCardException` unchanged. The Rust reference (`yubikey-manager`, `experiment/rust`) solves the
held case by killing the blocking process (`kill_pcsc_blockers`); a library must not do that.

## Vision

On a multi-transport applet, the documented default order is *resilient*: if the most-preferred transport
is held by another process, the session transparently uses the next supported transport. The behavior is
narrow and safe — it triggers only for the two PC/SC "held/busy" status codes, only on the default
(no-override) path, and only for applets that legitimately have more than one transport. An explicit
override is still honored exactly and never falls back. No process is ever killed. The fallback logic lives
in **one** Core helper that all four applet connect sites call, so detection is defined and tested once and
the duplicated per-applet transport switches are removed. The "default-path-only" and
"override-never-falls-back" guarantees are **applet-layer** properties: the public Core helper simply
follows the ordered candidate list it is given (it does not know whether that list came from a default or an
override); the applet entry points enforce the guarantee by passing `ResolveSessionTransports`' output
(a single-element list for an override).

## Out of Scope

- **HID-held fallback.** Only the SmartCard PC/SC codes named in master ISC-23.1
  (`SCARD_E_SHARING_VIOLATION`, `SCARD_E_SERVER_TOO_BUSY`) trigger fallback. A held/locked HID transport
  surfaces a different exception type and is *not* handled here; it is recorded as a deferred candidate.
- **Process killing / `kill_pcsc_blockers` / killing `scdaemon`.** The SDK never kills or signals another
  process to free a transport. (Operators may free the CCID out-of-band with `gpgconf --kill scdaemon`;
  that is not something the library does.)
- **Retry of the *same* transport.** This is fallback to the *next* transport, not a retry loop on the held
  one (`DiscoveryIdentityReader` already retries transient failures during discovery — and retries *all*
  exception types, which is why it is cited only as a general "connect failures are tolerated" precedent,
  not as a model for the narrow held-code taxonomy here; that concern is different and unchanged).
- **Held failures that surface *after* transport open.** The fallback is **connect-time only**: it wraps
  `IYubiKey.ConnectAsync<T>` (the transport-open). `UsbSmartCardConnection` can also throw `SCardException`
  later (e.g. from `SCardBeginTransaction` during use); such post-open held failures are not in scope and
  surface unchanged. The motivating held-CCID case fails at open, which is what this phase covers.
- **Any change to override semantics, default orders, valid-transport sets, or public signatures** from
  Phase 38. This phase only inserts fallback into the existing ordered-candidate iteration.
- **Changes to discovery/merge (Phase 37.5), `YubiKeyManager.FindAllAsync`, or `CompositeYubiKey` routing.**
- **SCP-implied transport selection** (still deferred from Phase 38).
- **Final-verification / migration-doc work** (Phase 39).

## Principles

- Fallback is a property of the **default ordered candidate list**, which Phase 38 already produces. An
  override yields a single-element list, so it cannot fall back — no special-casing is needed for the
  override path beyond "rethrow when no further candidate exists".
- Detection is **narrow and explicit**: only `SCardException` carrying one of the two held/busy `HResult`
  codes counts as "held". Every other exception — including other `SCardException` codes, non-SCard
  exceptions, and `OperationCanceledException` — propagates unchanged. We never mask a genuine failure.
- The logic is **centralized in Core** (`YubiKeyConnectionExtensions`), beside `ResolveSessionTransports`,
  so the four applets share one implementation and one set of tests. Applets already depend on Core; this
  introduces no new dependency direction.
- The fallback **preserves order**: it attempts candidates in the order `ResolveSessionTransports` returns
  and stops at the first that connects, mirroring the documented per-app default order.
- Behavior is provable **without hardware** for the core logic: a fake `IYubiKey` whose
  `ConnectAsync<ISmartCardConnection>` throws a constructed held `SCardException` deterministically
  exercises every branch. Hardware verification then proves the real held-CCID case end-to-end.

## Constraints

- Execute on branch `yubikit-composite-device-new`.
- This phase changes connect-time *behavior* (a new public Core helper method, and a fallback that changes
  which transport a default call ends up on when one is held). Run the **interim Cato review of this ISA
  before source edits** via `scripts/interim-cross-vendor-review.sh` (GPT-5.4, high reasoning) and queue the
  GPT-5.5 Cato review.
- Implementation uses the **/DevTeam** workflow (interim GPT-5.4 reviewer; GPT-5.5 DevTeam review queued).
  Record review output before commit.
- Use `dotnet toolchain.cs`; never raw `dotnet build`/`dotnet test`. Focus with `--project` + `--filter`.
- Hardware verification uses the connected composite key serial 103 (OTP+FIDO+CCID). The held-CCID repro
  requires *holding* the CCID with an **exclusive** PC/SC handle, because `UsbSmartCardConnection` opens
  **shared by default** (`UsbSmartCardConnection.cs:127` — `SCARD_SHARE.SHARED` unless the compat switch is
  set). The reliable in-process holder procedure is therefore: (1)
  capture the prior switch value via `AppContext.TryGetSwitch(CoreCompatSwitches.OpenSmartCardHandlesExclusively, out var prior)`,
  then `AppContext.SetSwitch(CoreCompatSwitches.OpenSmartCardHandlesExclusively, true)`; (2) open and keep a
  holder `ISmartCardConnection` to serial 103 (now exclusive); (3) run the session-under-test
  `CreateManagementSessionAsync` (its SmartCard connect attempt, shared or exclusive, against an
  exclusively-held card returns a held/busy code — in practice `SCARD_E_SHARING_VIOLATION`, but
  `SCARD_E_SERVER_TOO_BUSY` is equally accepted; exact PC/SC codes are stack/platform dependent) and observe
  fallback to `HidFido`; (4) in a
  `finally` (executed even on assertion failure) dispose the holder and **restore the switch's prior
  behavioral state** with `AppContext.SetSwitch(CoreCompatSwitches.OpenSmartCardHandlesExclusively, prior)`
  (`AppContext` has no unset/clear API; if the switch was previously unset its effective value was `false`,
  the documented default, so restoring `false` is correct). To be robust against the process-global switch,
  run this verification in an **isolated, non-parallel** test collection (or a dedicated test host).
  The exclusive self-held repro is **deterministic** (the compat switch + an open holder guarantees the
  second connect sees a held/busy code) and is the **preferred** proof path; ISC-17 is not satisfiable by a
  skip. `scdaemon` (`gpgconf --launch scdaemon`) remains a **true fallback holder** for environments where
  the self-held exclusive repro is unavailable or unreliable — but because launching scdaemon does not
  guarantee it opens the card, the self-held exclusive path is preferred and used whenever available, and a
  scdaemon-hold attempt that fails to hold the CCID is recorded as such rather than treated as proof.
  Release scdaemon with `gpgconf --kill scdaemon`.
  **NEVER run `gpg --card-status`** (it reset the beta key off the USB bus in a prior phase). Any temporary
  hardware test is removed before commit. Because `CoreCompatSwitches.OpenSmartCardHandlesExclusively` is
  **process-global** `AppContext` state, the temporary held-CCID verification must run **isolated / in a
  non-parallel collection**, so no concurrent test inherits exclusive-open behavior and fails spuriously.
  `src/Tests.Shared/appsettings.json` already authorizes serial 103 (`AllowUnknownSerials: true`); no
  allow-list edit is needed.
- Commit only intended files; never `git add .`/`-A`/`commit -a`. Do not introduce a `Core` -> `Management`
  dependency.

## Goal

Add a centralized Core helper
`ConnectSessionTransportAsync(this IYubiKey, IReadOnlyList<ConnectionType> candidates, string sessionName, CancellationToken)`
that opens the first candidate transport that connects, falling back to the next candidate only when a
connect fails with a held/busy PC/SC status (`SCARD_E_SHARING_VIOLATION` / `SCARD_E_SERVER_TOO_BUSY` via
`SCardException`), and rethrowing the original error when no further candidate remains. Refactor the four
multi-transport applet connect sites (`ConnectForManagementAsync`, `ConnectForYubiOtpAsync`,
`ConnectForFidoAsync`, and WebAuthn via the shared Fido2 path) to use it, removing their duplicated
transport switches. Prove every branch with fake-`IYubiKey` Core unit tests that emit a constructed held
`SCardException`, prove the real held-CCID case end-to-end on serial 103, verify the solution, run interim
Cato (ISA) and /DevTeam (impl) reviews with GPT-5.5 queued, write a learning note that feeds Phase 39, and
commit Phase 38.5 only.

## Criteria

### Governance

- [ ] ISC-1: Branch check shows `## yubikit-composite-device-new` before source edits, review delegation, build/test, or commit.
- [ ] ISC-2: This ISA records the fallback model, the exact held-error detection taxonomy, the override-never-falls-back rule, the multi-transport-only / default-path-only scope, and the Core-centralization decision before source edits.
- [ ] ISC-3: Interim Cato review of this ISA runs before source edits and returns pass or all concerns resolved; the GPT-5.5 Cato review is queued.

### Fallback Behavior

- [ ] ISC-4: A new public Core helper `YubiKeyConnectionExtensions.ConnectSessionTransportAsync(this IYubiKey yubiKey, IReadOnlyList<ConnectionType> candidates, string sessionName, CancellationToken cancellationToken)` returns the opened `IConnection`. It iterates `candidates` in order, maps each `ConnectionType` to `ConnectAsync<ISmartCardConnection>` / `ConnectAsync<IFidoHidConnection>` / `ConnectAsync<IOtpHidConnection>`, and returns the first that connects. It is **public** for consistency with its companion seam `ResolveSessionTransports` (also public; the two form the resolve→connect pair the applets call). Because it is public it validates its input: it throws `ArgumentNullException` if `yubiKey` or `candidates` is null, `ArgumentException` if `candidates` is empty, `ArgumentException` if any element is not exactly one concrete transport (`SmartCard`/`HidFido`/`HidOtp`), and `ArgumentException` if `candidates` contains a duplicate transport (the helper attempts each transport at most once; a list that repeats a transport is a programming error and is rejected, so the helper cannot be used to retry the same transport — that scope boundary is enforced, not merely documented). Callers always pass the validated, deduplicated, ordered list from `ResolveSessionTransports`. Its held-error detection is defined against the **top-level** exception thrown by `IYubiKey.ConnectAsync<T>` (ISC-9); SDK call sites pass SDK connection types whose held failures surface unwrapped, and the helper does not unwrap. **Scope ownership:** the helper's contract is narrowly "connect over the given ordered candidate list, falling back past held SmartCard transports." The phase-level guarantees *default-path-only fallback* and *override-never-falls-back* are properties of the four applet entry points (which pass `ResolveSessionTransports` output — a single-element list for an override), NOT promises the helper makes to arbitrary callers; ISC-7/ISC-21 are enforced at the applet layer. **Capability mismatch:** the helper does not re-validate device support (that is `ResolveSessionTransports`' job); if a caller passes a transport the device does not expose, the helper simply attempts `ConnectAsync<T>` for it, and that transport's connect error surfaces unchanged — a non-held error, so it does not trigger fallback.
- [ ] ISC-5: Held-error detection is a single predicate `IsHeldTransportError(Exception)` that returns true **only** for a `SCardException` whose `(uint)HResult` is `ErrorCode.SCARD_E_SHARING_VIOLATION` (`0x8010000B`) or `ErrorCode.SCARD_E_SERVER_TOO_BUSY` (`0x80100031`). No other exception type, and no other `SCardException` code, is treated as held.
- [ ] ISC-6: Fallback is triggered **only when the failed candidate is `ConnectionType.SmartCard` AND the error is a held-transport error** (`candidate == ConnectionType.SmartCard && IsHeldTransportError(ex)`). This enforces the phase scope (SmartCard-only held fallback; HID-held deferred) even for the public helper or a custom `IYubiKey`: a held-coded `SCardException` surfacing from a non-SmartCard candidate does **not** trigger fallback and propagates. When fallback is triggered and at least one further candidate remains, the helper logs at debug and attempts the next candidate. When it is triggered on the **last** candidate (or when the failed candidate is SmartCard-held but it was the last), the helper rethrows that error unchanged (preserving stack via `throw;`). The helper calls `cancellationToken.ThrowIfCancellationRequested()` before each connect attempt (including before the first) and again after catching a fallback-triggering error before continuing to the next candidate, so a cancellation requested between attempts stops the loop instead of opening a fallback transport. On a **successful** connect the helper logs the selected transport at debug, so the chosen transport is observable in logs for both the held and free-CCID runs (ISC-17).
- [ ] ISC-7: Because an explicit override resolves to a **single-element** candidate list (Phase 38), a held error on an override surfaces unchanged with no fallback (the single element is the last element). The override path requires no special-casing; this is asserted by test, not by branching on "is this an override".
- [ ] ISC-8: A non-held error (any exception for which `IsHeldTransportError` is false, including other `SCardException` codes such as `SCARD_E_NO_SMARTCARD`, and any non-SCard exception) propagates immediately and is **not** retried against another candidate. `OperationCanceledException` always propagates immediately and is never treated as held — both when thrown by a connect attempt and when surfaced by the inter-attempt `ThrowIfCancellationRequested()` (ISC-6).
- [ ] ISC-9: Held `SCardException` detection works against the exception as it actually surfaces from `IYubiKey.ConnectAsync<T>` on a composite device. The current code path propagates the **raw** `SCardException` unwrapped — `UsbSmartCardConnection` (open) → `SmartCardConnectionFactory.CreateAsync` → `PcscYubiKey.ConnectAsync` → `CompositeYubiKey.ConnectAsync`, with no wrapping catch/rethrow — so `IsHeldTransportError` inspects the **top-level** exception. This invariant is **pinned by a focused Core unit test** (not just "verify and record"): a `CompositeYubiKey` whose selected member's `ConnectAsync` throws a held `SCardException` rethrows a **top-level `SCardException` with no outer wrapper and the held `HResult` preserved** (and likewise for `PcscYubiKey.ConnectAsync` over a low-level connection that throws). The contract is "unwrapped, held-code-preserving propagation", not object identity — a harmless refactor that rethrows an equivalent `SCardException` is acceptable so long as it is not wrapped and the code is preserved. Because the fake-`IYubiKey` helper tests cannot catch a future wrapper change in the real connect chain, this propagation test guards against the chain silently starting to wrap `SCardException` (which would break held fallback on real devices). The finding is recorded in the learning note. If (and only if) a defensive inner-exception unwrap is added for future-proofing, it must be accompanied by a unit case in which a held `SCardException` is wrapped in another exception and still detected; absent that, no unwrap logic is added (avoid dead/untested code).

### Wiring And Scope Preservation

- [ ] ISC-10: The four multi-transport connect sites are refactored to call `ConnectSessionTransportAsync`: Management `ConnectForManagementAsync`, YubiOtp `ConnectForYubiOtpAsync`, Fido2 `ConnectForFidoAsync`, and WebAuthn via the shared Fido2 path. The previously duplicated `foreach (var transport in candidates) { return transport switch { ... }; }` blocks and their trailing `throw new NotSupportedException(...)` are removed in favor of the helper. Fido2's `NotSupportedException` remap (the generic "no FIDO-capable connection" message) stays wrapped around the `ResolveSessionTransports` call **only** and must NOT widen to enclose the `ConnectSessionTransportAsync` call, so a held/fallback or non-held connect error is never masked as the generic no-FIDO message. This scoping is proven by a Fido2-level test (ISC-14).
- [ ] ISC-11: Phase 38 selection semantics are unchanged: the override taxonomy (`ArgumentException` for non-concrete / applet-invalid; `NotSupportedException` for device-unsupported) from `ResolveSessionTransports` still throws before any connect, default orders are unchanged, and single-transport applets (Piv, Oath, OpenPgp, SecurityDomain, YubiHsm) are untouched (they do not use the multi-transport helper and gain no fallback).
- [ ] ISC-12: No held-error connect succeeds by disposing a half-open connection improperly: a candidate that throws during connect leaves nothing to dispose (the connect threw before returning a connection); a candidate that connects successfully is returned to the caller, which owns disposal (existing per-applet `try/catch` around session creation is preserved). The "connect succeeded over fallback transport, then session init failed" path must dispose the opened fallback connection (the existing per-applet `catch { await connection.DisposeAsync(); throw; }` covers this) — this is asserted by test (ISC-14), because a leak here would itself create a held-transport condition.

### Tests And Verification

- [ ] ISC-13: New Core unit tests use a fake `IYubiKey` (`HeldTransportProbeYubiKey` or equivalent) that reports a chosen `AvailableConnections` and, per transport, either connects (returning a fake connection and recording the attempt) or throws a caller-specified exception. Cases, asserting both the returned/observed transport and the exact ordered sequence of attempts:
  - (a) **sharing-violation falls back**: SmartCard throws `SCARD_E_SHARING_VIOLATION`, next candidate connects → helper returns the next candidate; attempts recorded in order (SmartCard then next).
  - (b) **server-too-busy falls back**: SmartCard throws `SCARD_E_SERVER_TOO_BUSY`, next candidate connects → fallback occurs.
  - (c) **non-held SCard error does not fall back**: SmartCard throws `SCARD_E_NO_SMARTCARD` → that `SCardException` propagates, only one attempt made, no fallback.
  - (d) **non-SCard error does not fall back**: SmartCard throws e.g. `InvalidOperationException` → it propagates, one attempt, no fallback.
  - (e) **cancellation propagates**: SmartCard throws `OperationCanceledException` → it propagates, no fallback.
  - (f) **success on first**: SmartCard connects → exactly one attempt, no catch, SmartCard returned.
  - (g) **already-canceled token**: an already-canceled `CancellationToken` → the helper throws `OperationCanceledException` with **zero** connect attempts (proving the pre-first-attempt `ThrowIfCancellationRequested()` in ISC-6). (Under the SmartCard-only gate of ISC-6, a non-SmartCard held failure never continues, so there is no "all candidates held" exhaustion case; SmartCard-held-with-no-next-candidate is the single-element rethrow in (h)/(l).)
  - (h) **override single-element rethrows**: a single-element candidate list whose only transport throws a held error → that held error propagates (no fallback), proving ISC-7.
  - (i) **input validation**: `null` `candidates` throws `ArgumentNullException`; empty list throws `ArgumentException`; a list containing a non-concrete element (e.g. `Hid`/`All`/`Unknown`) throws `ArgumentException`; a list containing a **duplicate** transport (e.g. `[SmartCard, SmartCard]`) throws `ArgumentException` and makes **no** connect attempt (proving the no-same-transport-retry contract, ISC-4). (A `null` `yubiKey` guard is covered by the extension's `ArgumentNullException` check.)
  - (j) **cancellation between attempts**: candidate 1 throws a held error, the token is then canceled, and the helper throws `OperationCanceledException` **without** attempting candidate 2 (proving the inter-attempt `ThrowIfCancellationRequested()` in ISC-6).
  - (k) **held first, real failure second**: candidate 1 (SmartCard) throws a held error, candidate 2 throws a non-held error → the helper surfaces candidate 2's non-held exception immediately (it does not rethrow the earlier held error, and does not advance past candidate 2). Proves ISC-8/ISC-22 once the loop has already advanced.
  - (l) **held error on a non-SmartCard candidate does not fall back**: a candidate list whose first element is `HidFido` (or `HidOtp`) whose connect throws a held-coded `SCardException` → that exception propagates with no fallback, even though a further candidate exists (proving the `candidate == SmartCard` gate in ISC-6; SmartCard-only scope).
  - (m) **device-unsupported candidate (public-helper contract)**: an arbitrary caller passes a candidate the fake device does not expose; its `ConnectAsync` throws a non-held connect error → that error propagates unchanged with no fallback (the helper does not re-validate device capability or remap the error; ISC-4 capability-mismatch contract). If instead that unsupported candidate were SmartCard and threw a held code, normal SmartCard held-fallback applies.
- [ ] ISC-14: Applet-level fallback tests prove the wiring end-to-end through the public entry methods, not just the Core helper:
  - **Management**: `CreateManagementSessionAsync` (no override) is called on a fake `IYubiKey` whose SmartCard `ConnectAsync` throws `SCARD_E_SHARING_VIOLATION` and whose `HidFido` transport returns a **disposable probe connection** that implements the chosen HID interface (`IFidoHidConnection`), records disposal, and is valid enough to return from connect but **throws deterministically on every backend/protocol exchange** the HID `ManagementSession.CreateAsync` attempts (so initialization — `GetVersionAsync` and any header-select/backend call it makes — cannot complete by any path, guaranteeing the post-connect failure). The test asserts the ordered `ConnectAsync<T>` attempts (SmartCard first, then `HidFido`) and both (i) the surfaced failure is that post-connect session-init failure, not the `SCardException`, and (ii) the opened HID probe connection was **disposed** (ISC-12 — no leak on the fallback path).
  - **YubiOtp**: `CreateYubiOtpSessionAsync` (no override) on a fake whose SmartCard `ConnectAsync` throws `SCARD_E_SHARING_VIOLATION` and whose `HidOtp` transport returns a disposable probe (per the Management model) that throws at YubiOtp's session-init seam (`YubiOtpSession.CreateAsync`'s `ReadStatusAsync` on the HID path) asserts the ordered attempts (SmartCard first, then `HidOtp`), that the surfaced failure is the post-connect session-init failure (not the `SCardException`), and that the opened `HidOtp` probe was disposed — so the second non-FIDO multi-transport site is proven end-to-end, not inferred from Management.
  - **Fido2**: `CreateFidoSessionAsync` (no override) on a fake exposing `HidFido | SmartCard` whose HID FIDO `ConnectAsync` throws a **non-held** error surfaces that non-held error unchanged — NOT the generic "no FIDO-capable connection" `NotSupportedException` — proving the Fido2 remap stayed scoped to `ResolveSessionTransports` and did not widen around the helper (ISC-10).
  - **Override does not fall back (applet wiring)**: on **both** default-first-SmartCard applets, the override path is proven not to fall back, because each has a separate connect site that could independently regress. Management: `CreateManagementSessionAsync` with `preferredConnection: ConnectionType.SmartCard` on a fake exposing `SmartCard | HidFido` whose SmartCard `ConnectAsync` throws a held code surfaces that held `SCardException` and makes **no** HID attempt. YubiOtp: `CreateYubiOtpSessionAsync` with `preferredConnection: ConnectionType.SmartCard` on a fake exposing `SmartCard | HidOtp` whose SmartCard `ConnectAsync` throws a held code surfaces that held `SCardException` and makes **no** `HidOtp` attempt. Both prove the applet passes `ResolveSessionTransports`' single-element override list to the helper and does not substitute the default candidate list on the override path (guards the ISC-7 invariant at the applet layer, which ISC-13(h) only proves at the helper layer; the Phase 38 override tests only cover non-held selection).
  - **WebAuthn** needs no separate fallback test: `CreateWebAuthnClientAsync` adds no independent transport logic and forwards through the shared Fido2 path (Phase 38 ISC-7), so the Fido2 case above covers it.
- [ ] ISC-15: Focused unit suites for Core, Management, YubiOtp, Fido2, WebAuthn pass; full solution build passes with 0 warnings / 0 errors; Core has no dependency on `Yubico.YubiKit.Management`.
- [ ] ISC-16: Active documentation validates (`docs-qa`); changed-file formatting verifies clean (`dotnet format --verify-no-changes`); `git diff --check` is clean. Any module README/XML-doc note about default-order resilience is added where it aids callers (the helper's behavior is documented on the helper; per-app docs need only mention that the default order is tried in order and falls back when a transport is held).
- [ ] ISC-17: **Live held-CCID hardware verification on serial 103.** With the CCID actually held by another PC/SC client (preferred: a long-lived SDK exclusive SmartCard connection opened by the verification step; fallback: `scdaemon` launched via `gpgconf --launch scdaemon`), `CreateManagementSessionAsync` with no override selects SmartCard first, observes **one of the two approved held codes** (`SCARD_E_SHARING_VIOLATION` or `SCARD_E_SERVER_TOO_BUSY` — the exact code seen is recorded), falls back to the **next transport in Management's documented order — `HidFido`** (serial 103 exposes both `HidFido` and `HidOtp`; a fallback that skipped `HidFido` straight to `HidOtp` is a failure), and successfully reads serial 103. The repro records the **exact** fallback target, not merely "a HID transport". A control run with the CCID free confirms the default still selects SmartCard (no regression). The control run must **observe the selected transport authoritatively** — via the helper's success-path debug log of the selected transport (ISC-6) captured for the run, or a temporary instrumented connect that records the chosen `ConnectionType` — and assert it is `SmartCard`; merely asserting "a session was created" is insufficient (a regression that always used HID would pass). The holder is released afterward (`gpgconf --kill scdaemon` if scdaemon was used; dispose the held connection otherwise). `gpg --card-status` is never run. Any temporary test code is removed before commit. Evidence (commands + observed transport) is recorded in the learning note.
- [ ] ISC-18: /DevTeam review (interim GPT-5.4) returns pass or all findings fixed; GPT-5.5 review queued; review output recorded.
- [ ] ISC-19: Phase 38.5 learning note records the fallback model, the held-error detection taxonomy (and the wrapped-vs-raw `SCardException` finding from ISC-9), the source changes, review status, verification (incl. the **exact verification commands** and the live held-CCID hardware evidence — observed fallback target and free-CCID control transport), the grep/read evidence for ISC-23 (no process-management code), and the deferred HID-held candidate. It feeds Phase 39.
- [ ] ISC-20: Master ISA ISC-23.1 is checked and the Phase 38.5 row is reconciled (Phase 39 depends on Phase 38.5).

### Anti-Criteria

- [ ] ISC-21: Anti: an explicit override falls back to another transport.
- [ ] ISC-22: Anti: a non-held connect error, another `SCardException` code, or `OperationCanceledException` is swallowed or causes a fallback/retry.
- [ ] ISC-23: Anti: the SDK kills, signals, or launches-to-displace another process to free a transport (`kill_pcsc_blockers` / killing `scdaemon`), or `gpg --card-status` is run during verification. Enforced by evidence, not narrative: the learning note records the exact verification commands run, and a grep/read of the phase's `src/` diff confirms no process-management API (e.g. `Process.Start`, `Process.Kill`, `kill`, `pkill`, `gpgconf`, `scdaemon`) was introduced into SDK source.
- [ ] ISC-24: Anti: a `Core` -> `Management` dependency is introduced, HID-held fallback is implemented, or a single-transport applet gains fallback.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | branch | Active branch | `## yubikit-composite-device-new` | `git status --short --branch` |
| ISC-2 | design | Decisions recorded | present before edits | Read |
| ISC-3 | review | Interim Cato ISA review | pass/resolved | `scripts/interim-cross-vendor-review.sh` |
| ISC-4 to ISC-9 | source/unit | Helper shape, detection predicate, fallback/rethrow, non-held propagation, wrap finding | tests pass | `dotnet toolchain.cs -- test --project Core --filter ...` |
| ISC-10 to ISC-12 | source | Four sites refactored onto helper; Phase 38 semantics intact; disposal preserved | read/grep + tests | Read + grep + unit tests |
| ISC-13 | unit | Fake-probe held-fallback cases (a)-(m) | tests pass | Core unit tests |
| ISC-14 | unit | Applet-level end-to-end fallback (Management + YubiOtp disposal; Fido2 remap scope) | tests pass | unit tests |
| ISC-15 | build | Focused + full build; no Core->Mgmt | exit 0 / clean | `dotnet toolchain.cs build` + grep |
| ISC-16 | docs/format | docs-qa, changed-file format, whitespace | exit 0 / clean | `dotnet toolchain.cs -- docs-qa`; `dotnet format --verify-no-changes`; `git diff --check` |
| ISC-17 | integration | Live held-CCID repro (serial 103) + free-CCID control | fallback to HID succeeds; control selects SmartCard; holder released | `dotnet toolchain.cs -- test --project Management --integration --filter ...` (temporary) |
| ISC-18 | review | /DevTeam review | pass/fixed | review output |
| ISC-19 to ISC-20 | file | Learning note; master ISA reconciled | present/checked | Read |
| ISC-21 to ISC-24 | scope/dep | Anti-guards | no override fallback, no non-held swallow, no process kill, no Core->Mgmt / no HID-held / no single-transport fallback | tests / grep / git diff |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Phase 38.5 ISA + interim Cato | Write this ISA; record decisions; run interim Cato before edits. | ISC-1, ISC-2, ISC-3 | Phase 38 | false |
| Core fallback helper | Add `ConnectSessionTransportAsync` + `IsHeldTransportError`; verify raw-vs-wrapped `SCardException`. | ISC-4, ISC-5, ISC-6, ISC-7, ISC-8, ISC-9 | interim Cato pass | false |
| Refactor connect sites | Route Management/YubiOtp/Fido2/WebAuthn through the helper; remove duplicated switches; preserve Phase 38 semantics + disposal. | ISC-10, ISC-11, ISC-12 | Core fallback helper | false |
| Held-fallback tests | Core fake-probe cases (a)-(m) emitting held `SCardException`; Management + YubiOtp end-to-end fallback (disposal) + Fido2 remap-scope test. | ISC-13, ISC-14 | Core fallback helper | true |
| Verify, hardware, review, learn, commit | Build, docs/format/dep, live held-CCID repro on serial 103, /DevTeam review, learnings, master ISA reconcile, commit. | ISC-15, ISC-16, ISC-17, ISC-18, ISC-19, ISC-20, ISC-21..24 | implementation complete | false |

## Decisions

- 2026-06-11: **Fallback is centralized in one Core helper** `ConnectSessionTransportAsync`, not duplicated
  per applet. It owns both the transport-type switch and the held-error catch/continue, so detection is
  defined and tested once and the four applets collapse to a single call. Applets already depend on Core;
  no new dependency direction is created. (User-selected over inline per-applet try/catch.)
- 2026-06-11: **Detection is exactly two PC/SC codes.** `IsHeldTransportError` returns true only for a
  `SCardException` with `(uint)HResult` of `SCARD_E_SHARING_VIOLATION` (`0x8010000B`) or
  `SCARD_E_SERVER_TOO_BUSY` (`0x80100031`). `SCardException` stores its code in `HResult`
  (`SCardException(string, long)` sets `HResult = (int)errorCode`); there is no `ErrorCode` property, so
  detection compares `(uint)ex.HResult`. Every other exception propagates. (Matches master ISC-23.1.)
- 2026-06-11: **Override never falls back, structurally.** An override resolves to a single-element
  candidate list (Phase 38), so the helper's "rethrow when no further candidate remains" rule yields the
  correct override behavior without any "is this an override" branch. Proven by ISC-13(h).
- 2026-06-11: **HID-held is out of scope.** Only the SmartCard PC/SC held/busy codes trigger fallback,
  per master ISC-23.1. A held HID transport surfaces a different exception type and is deferred. (User-
  selected scope.)
- 2026-06-11: **No process killing.** The SDK never kills/launches another process to free a transport.
  The Rust reference's `kill_pcsc_blockers` is intentionally not ported. Freeing a held CCID is an operator
  action (`gpgconf --kill scdaemon`), outside the library.
- 2026-06-11: **Live held-CCID is verified on hardware (serial 103).** Per user direction, this phase does
  not rely solely on fakes for the held case: it reproduces a real held CCID and observes the live fallback
  to HID. `gpg --card-status` is forbidden; the holder is released afterward.
- 2026-06-11 (interim Cato, CHANGES REQUIRED): **The repro holder must be exclusive.**
  `UsbSmartCardConnection` opens shared by default (`SCARD_SHARE.SHARED`), so a self-held shared connection
  would not produce `SCARD_E_SHARING_VIOLATION`. The holder must set
  `AppContext.SetSwitch(CoreCompatSwitches.OpenSmartCardHandlesExclusively, true)` before opening, and reset
  it after. `scdaemon` is demoted to best-effort (launching it does not guarantee it opens the card); a
  failure to hold via scdaemon is a recorded skip, never the primary repro. (Constraints + ISC-17 updated.)
- 2026-06-11 (interim Cato): **Cancellation is checked between attempts.** The helper calls
  `ThrowIfCancellationRequested()` before each attempt and after catching a held error, so a token canceled
  after a held failure does not open a fallback transport. (ISC-6, ISC-8, ISC-13(j).)
- 2026-06-11 (interim Cato): **The helper stays public but validates its input.** Consistency with the
  already-public companion `ResolveSessionTransports` (the resolve→connect seam pair) outweighs minimizing
  surface; to mitigate the public-API risk the reviewer raised, the helper validates that `candidates` is
  non-empty and every element is a single concrete transport, throwing `ArgumentException` otherwise. (ISC-4.)
- 2026-06-11 (interim Cato): **ISC-14 applet wiring test tightened** to call `CreateManagementSessionAsync`
  and assert the ordered `ConnectAsync<T>` attempts, letting session creation fail after the successful HID
  connect, so the real applet refactor is proven (not only the Core helper).

## Changelog

- conjectured: Phase 38's deferral of fallback was a clean carve-out; held-CCID could wait indefinitely.
  refuted by: the held-CCID gap (`phase-37_5-pid-merge-learnings.md`) makes the SmartCard-first default
  throw on real systems running GnuPG, even though an available HID transport would succeed; master ISC-23.1
  requires the default order to fall back off a held transport.
  learned: add a centralized Core fallback helper over the Phase 38 ordered candidate list, detect exactly
  the two PC/SC held/busy codes, fall back only on the default path (override is single-element), and never
  kill a process.
  criterion now: ISC-4 to ISC-12 govern the helper, detection, and wiring; ISC-13/ISC-14 cover tests;
  ISC-17 covers the live hardware repro.
- conjectured: a "self-held SDK SmartCard connection" would reliably reproduce a held CCID, and `scdaemon`
  was an equivalent fallback holder; cancellation only needed handling when a connect attempt threw it.
  refuted by: interim Cato (GPT-5.4, CHANGES REQUIRED) — `UsbSmartCardConnection` opens shared by default, so
  a self-held connection only violates sharing if opened exclusively via
  `CoreCompatSwitches.OpenSmartCardHandlesExclusively`; `gpgconf --launch scdaemon` does not guarantee the
  card is opened; and a token canceled *between* attempts would otherwise still open a fallback transport.
  learned: pin the exclusive-handle holder procedure, demote scdaemon to best-effort/skip, check
  cancellation between attempts, keep the helper public but validate its input, and tighten the applet-level
  wiring test.
  criterion now: Constraints + ISC-17 (exclusive holder), ISC-6/ISC-8/ISC-13(j) (cancellation), ISC-4
  (input validation), ISC-14 (wiring) capture the fixes.

## Verification

Populated in the Phase 38.5 learning note before commit.
