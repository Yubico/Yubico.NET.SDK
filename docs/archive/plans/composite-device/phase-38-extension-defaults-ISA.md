# Phase 38 ISA: Extension Method Smart Defaults And Explicit Overrides

This phase formalizes the applet session-entry extension methods (`IYubiKeyExtensions`) so that, on the
physical (possibly multi-connection) `IYubiKey` introduced in Phase 36/37/37.5, each app keeps its
ergonomic one-call entry point while making transport selection an explicit, documented policy:
an app-specific smart **default** transport order plus an explicit caller **override**. It replaces the
two Phase 36 placeholders that currently throw or carry "Phase 38" gating comments (Fido2's
dual-transport throw; the Management/YubiOtp `ResolvePreferredConnection` gates).

Held-transport fallback (falling back off a transport another process holds) is **not** in this phase;
it is carved out to Phase 38.5 (master ISC-23.1). See the master ISA changelog entry dated 2026-06-11.

Read this together with:

- `docs/plans/composite-device/ISA.md` (master — ISC-21, ISC-21.1, ISC-22, ISC-23, ISC-24; Decisions 2026-06-11)
- `docs/plans/composite-device/phase-37_5-pid-merge-learnings.md` (Next Phase Inputs; Deferred Candidates)
- `src/Core/CLAUDE.md` (ConnectionType semantics; ApplicationSession)
- `src/Core/src/YubiKey/YubiKeyConnectionExtensions.cs` (`ResolvePreferredConnection`)
- `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/IYubiKeyExtensionsTransportTests.cs` (the fake-probe test pattern to mirror)

## Problem

Phase 36 removed the scalar `IYubiKey.ConnectionType` and made the parameterless `ConnectAsync()`
throw on a multi-connection device. The applet session-entry extensions were migrated only
mechanically and left placeholders explicitly deferred to Phase 38:

1. **Fido2** `ConnectForFidoAsync` (`src/Fido2/src/IYubiKeyExtensions.cs:137`) **throws** `NotSupportedException`
   when a device exposes both HID FIDO and SmartCard FIDO2, instead of choosing a sensible default.
2. **Management** (`src/Management/src/IYubiKeyExtensions.cs:130`) and **YubiOtp**
   (`src/YubiOtp/src/IYubiKeyExtensions.cs:107`) resolve a transport via `ResolvePreferredConnection`
   but with a "Full smart-default/override policy is Phase 38" gating comment and **no override path**.
3. **No multi-transport applet accepts a caller transport override.** A caller who knows it wants, e.g.,
   the OTP HID transport for YubiOtp, or SmartCard FIDO2 for Fido2, cannot express that.

Single-transport applets (Piv, Oath, OpenPgp, SecurityDomain, YubiHsm) hard-code
`ConnectAsync<ISmartCardConnection>()`; that is correct and stays.

## Vision

Every applet keeps its current ergonomic entry method signature; existing direct call sites remain source-compatible (the override is an additive optional parameter — see ISC-4/ISC-14 for the binary/method-group caveats).
Multi-transport applets (Management, YubiOtp, Fido2, WebAuthn) document an app-specific default
transport order and accept an optional explicit override; Fido2/WebAuthn default to HID FIDO then
SmartCard FIDO2 (matching v1 norms and master ISC-22). Single-transport applets remain SmartCard-only.
There is no remaining "Phase 38" placeholder throw or comment in extension transport selection, and no
extension reasons about a scalar connection type. The behavior is covered by unit tests proving both the
default selection and the explicit override (including an unsupported-override throw) over fakes.

## Out of Scope

- **Held-transport fallback** (catching `SCARD_E_SHARING_VIOLATION` / `SCARD_E_SERVER_TOO_BUSY` and
  falling back to the next transport). That is Phase 38.5 (master ISC-23.1). This phase's override path
  must be designed so 38.5 layers on top without re-shaping the API.
- No `kill_pcsc_blockers` / process killing (ever, for a library).
- No change to the discovery/merge mechanism (Phase 37.5), `YubiKeyManager.FindAllAsync`, `CompositeYubiKey`
  routing, repository event contract, or the `ResolvePreferredConnection` resolver semantics.
- No override parameter on single-transport applets (Piv, Oath, OpenPgp, SecurityDomain, YubiHsm) —
  master ISC-23 scopes overrides to modules that can reasonably use more than one transport.
- No new public Core type unless a shared override→transport guard genuinely earns it; prefer reusing
  `ResolvePreferredConnection` plus a small private guard per module or one internal helper.
- **SCP-implied transport selection.** Making a supplied `scpKeyParams` imply the SmartCard transport for
  Fido2/WebAuthn (so a secure channel is never silently dropped onto HID FIDO) is a behavior change beyond
  the master-approved defaults+overrides scope (master ISC-21..24) and contradicts the fixed per-applet
  valid-transport model (ISC-6.1). It is deferred: the pre-existing documented behavior (SCP ignored over
  HID; select SmartCard explicitly to use SCP) is retained unchanged in Phase 38. Revisit as its own small
  phase or by amending the master ISA if a smart SCP default is wanted. (Surfaced and removed during the
  interim Cato review of this ISA — round 8 BLOCKER.)
- No final-verification/migration-doc work (Phase 39).

## Principles

- Preserve the ergonomic surface: existing direct `CreateXxxSessionAsync(...)` call sites stay
  source-compatible and behave the same on single-interface devices. The override parameter is optional
  and additive (`= null`); binary and method-group/delegate compatibility caveats are tracked in ISC-4/ISC-14.
- Smart defaults live in the app extension, never in raw Core connect APIs — the app knows its intent
  (master Decisions 2026-06-09; ISA Principles line 43).
- Default selection reuses `ResolvePreferredConnection(params ConnectionType[])`, which already ignores
  non-concrete flags and returns the first supported concrete transport. Do not reimplement it.
- An explicit override is honored exactly: a supported concrete transport is used; an unsupported one
  throws `NotSupportedException`; a non-concrete value (`Hid`/`All`/`Unknown`) is a caller error
  (`ArgumentException`). An override never silently falls back (that is 38.5's job, and only for `null`).
- Fido2/WebAuthn default order is `HidFido -> SmartCard` (master ISC-22). SmartCard FIDO2 is reached only
  when HID FIDO is absent (NFC, or USB with FW 5.8+ exposing the FIDO2 AID) or when explicitly overridden.
- Single-transport applets stay SmartCard-only and gain no override; doc comments are cleaned of any
  "Phase 38" deferral language where present.
- Tests use the existing fake-probe pattern (`SelectionProbeYubiKey : IYubiKey`) that records the requested
  `ConnectAsync<TConnection>` type; no hardware required for ISC coverage of selection logic.

## Constraints

- Execute on branch `yubikit-composite-device-new`.
- This phase adds new **public** optional parameters to shipped extension methods (an API-boundary change),
  so a **Cato review of this ISA is required before source edits**. While GPT-5.5 is rate-limited, run the
  interim opposite-family Cato review via `scripts/interim-cross-vendor-review.sh` (GPT-5.4, high reasoning)
  and queue the GPT-5.5/Cato review.
- Implementation uses the **/DevTeam** workflow (Engineer implements; Reviewer reviews; fix loop). The
  cross-vendor reviewer is interim GPT-5.4 with the GPT-5.5 DevTeam review queued. Record review output
  before commit.
- Use `dotnet toolchain.cs`; never raw `dotnet build`/`dotnet test`. Focus tests with `--project` + `--filter`.
- Hardware smoke uses the connected composite key, serial 103 (OTP+FIDO+CCID), and requires the CCID to be
  **already free**. Freeing a CCID that GnuPG `scdaemon` happens to hold is an out-of-band operator action
  on the test environment, not a step this phase mandates or that the SDK performs (no process-killing —
  that boundary belongs to Phase 38.5 discussion, and even there the library never kills processes). If the
  CCID is held and cannot be freed out-of-band, the SmartCard-path smoke is skipped with a recorded
  rationale; the HID-path defaults/overrides smoke still runs. Add serial 103 to the relevant module
  integration allow-list locally only; revert before commit. NEVER run `gpg --card-status`.
- Commit only intended files; never `git add .`/`-A`/`commit -a`. Do not introduce a `Core` -> `Management`
  dependency.

## Goal

Add an optional explicit transport override (`ConnectionType? preferredConnection = null`) to the
multi-transport session-entry extensions (Management, YubiOtp, Fido2, WebAuthn), keep their documented
default orders (Management `SmartCard -> HidFido -> HidOtp`; YubiOtp `SmartCard -> HidOtp`; Fido2/WebAuthn
`HidFido -> SmartCard`), replace Fido2's placeholder dual-transport throw with the default-plus-override
policy, remove all "Phase 38" placeholder language from extension transport selection, leave
single-transport applets SmartCard-only, cover default selection + explicit override + unsupported-override
throw with unit tests over fakes for each multi-transport module, document the per-app default table,
verify the solution, run a safe hardware smoke (serial 103, CCID free), run interim Cato (ISA) and /DevTeam
(impl) reviews with GPT-5.5 queued, write a learning note that feeds Phase 38.5, and commit Phase 38 only.

## Criteria

### Governance

- [ ] ISC-1: Branch check shows `## yubikit-composite-device-new` before source edits, review delegation, build/test, or commit.
- [ ] ISC-2: This ISA records the override API shape, the per-app default orders, the override-honoring rules (supported/unsupported/non-concrete), and the explicit Phase 38.5 carve-out before source edits.
- [ ] ISC-3: Interim Cato review of this ISA runs before source edits and returns pass or all concerns resolved; the GPT-5.5 Cato review is queued.

### Override API Shape

- [ ] ISC-4: Each multi-transport session-entry method (Management `CreateManagementSessionAsync`, YubiOtp `CreateYubiOtpSessionAsync`, Fido2 `CreateFidoSessionAsync`, WebAuthn `CreateWebAuthnClientAsync`) gains an optional `ConnectionType? preferredConnection = null` parameter, placed immediately before the trailing `CancellationToken` (CA1068 requires `CancellationToken` last; warnings-as-errors). Because inserting a parameter before `CancellationToken` would source-break callers that previously passed `CancellationToken` as the final positional argument, each method also keeps a back-compatibility overload with the exact pre-Phase-38 positional shape (ending in `CancellationToken`, no `preferredConnection`) that forwards with `preferredConnection: null`. The overload's parameters are all required, so C# better-function-member resolution selects it for the old full-positional call and the new method for all default/partial/named calls — verified to compile with no `CS0121` ambiguity (including the common `cancellationToken:`-named callers).
- [ ] ISC-5: `preferredConnection == null` selects the app's **effective default candidate list** (the applet's documented order, ISC-8/ISC-9) via `ResolvePreferredConnection`; behavior on a single-interface device is unchanged (resolves to the only transport). There is exactly one reading of the null path: "resolve over the applet's ordered default candidate list."
- [ ] ISC-6: A non-null `preferredConnection` is validated and resolved in this exact order, **before any connect attempt**:
  - **Not exactly one concrete transport** → `ArgumentException`. A valid override is *exactly one* of `ConnectionType.SmartCard`, `ConnectionType.HidFido`, or `ConnectionType.HidOtp`. Anything else — `Hid`, `All`, `Unknown`, `0`, or a combined flag such as `SmartCard | HidFido` — is a caller error and throws `ArgumentException` (it is not a device-capability question).
  - **Concrete but invalid for this applet** → `ArgumentException`. Each applet has a fixed valid-transport set (ISC-6.1); a concrete transport outside that set (e.g. `HidOtp` for Fido2/WebAuthn, or `HidFido` for YubiOtp) is a programming error and throws `ArgumentException` **even if the device exposes that transport**.
  - **Concrete, applet-valid, but not supported by the device** → `NotSupportedException` naming `AvailableConnections`.
  - **Concrete, applet-valid, and device-supported** → used exactly.
- [ ] ISC-6.1: Per-applet valid-transport sets: Management `{ SmartCard, HidFido, HidOtp }`; YubiOtp `{ SmartCard, HidOtp }`; Fido2/WebAuthn `{ HidFido, SmartCard }`. These bound both default resolution and override validation.
- [ ] ISC-7: WebAuthn forwards `preferredConnection` to `CreateFidoSessionAsync`; it does not add independent transport logic. Override validation (ISC-6) therefore happens in the shared Fido2 path.

### Default Policy

- [ ] ISC-8: Management default order is `SmartCard -> HidFido -> HidOtp`; YubiOtp default order is `SmartCard -> HidOtp` (parity with the shipped pre-Phase-38 gating).
- [ ] ISC-9: Fido2/WebAuthn default order is `HidFido -> SmartCard` (master ISC-22). The Phase 36 placeholder dual-transport throw in `ConnectForFidoAsync` is removed; a device exposing both transports now defaults to HID FIDO. A device exposing neither FIDO-capable transport still throws `NotSupportedException`.
- [ ] ISC-9.1: `scpKeyParams` does NOT change Fido2/WebAuthn transport selection in this phase. The default (`HidFido -> SmartCard`) and the override semantics (ISC-6) apply regardless of whether `scpKeyParams` is supplied. SCP is only valid on the SmartCard transport: supplying `scpKeyParams` while a non-SmartCard transport is selected (including the default HID FIDO) results in `NotSupportedException` ("SCP is only supported on SmartCard protocols") thrown by `ApplicationSession.InitializeCoreAsync` during session initialization — this is a pre-existing Core contract, not introduced here, and is the same outcome a single-HID device with SCP produced before Phase 38. (The earlier Fido2 doc comment that SCP is "ignored over HID" was factually wrong — the code throws — and is corrected in this phase.) A caller who needs SCP must select SmartCard explicitly via `preferredConnection: ConnectionType.SmartCard`. Making `scpKeyParams` imply/auto-switch to SmartCard is a separate behavior change beyond the master-approved defaults+overrides scope and is recorded as a deferred follow-up (Out of Scope), not implemented here; silently dropping SCP on HID is explicitly rejected (it would hide a requested secure channel).
- [ ] ISC-10: No remaining "Phase 38" placeholder throw or deferral comment exists in any extension transport-selection code path. No extension reasons about a scalar connection type (only `AvailableConnections`/`SupportsConnection`/`ResolvePreferredConnection`).

### Scope Boundaries

- [ ] ISC-11: Single-transport applets (Piv, Oath, OpenPgp, SecurityDomain, YubiHsm) remain SmartCard-only and gain NO override parameter; only doc-comment cleanup (if any "Phase 38" language exists) is applied.
- [ ] ISC-12: No held-transport fallback is implemented (an override or default that resolves to a held transport surfaces the underlying connect error unchanged); the design leaves room for Phase 38.5 to add fallback on the `null`-override default path without changing the public signature.
- [ ] ISC-12.1: The `preferredConnection == null` default branch is candidate-order-aware: the shared Core helper `ResolveSessionTransports` returns an **ordered, non-empty `IReadOnlyList<ConnectionType>`** of concrete transports to attempt (the device-supported subset of the applet's default order per ISC-5/ISC-8/ISC-9, preference order preserved; an explicit override returns a single-element list because an override never falls back). Each applet's connect site iterates that ordered list and opens the first candidate today, so Phase 38.5 can add held-transport fallback by wrapping each attempt in the existing loop and continuing to the next candidate — without reshaping override semantics or the public signature. Anti: collapsing the default path to a single transport before the connect site (discarding the ordered list).

### Tests And Verification

- [ ] ISC-13: A `IYubiKeyExtensionsTransportTests` (fake-probe `SelectionProbeYubiKey`) exists for Management, YubiOtp, and WebAuthn UnitTests, and the existing Fido2 one is extended, each asserting:
  - (a) **default top-choice**: with all of the applet's valid transports present, default selection picks the documented first transport;
  - (b) **default fallback order**: at least one "first choice absent, second chosen" case per multi-transport module, plus for Management a "only the third choice (`HidOtp`) remains" case — proving the full ordered list, not just the top choice;
  - (c) **explicit non-default override honored on a multi-transport probe**: on a probe exposing 2+ applet-valid transports, an override to a transport that is NOT the default first choice is used exactly (proving the override beats default ordering, not merely equals it). Concretely: Fido2/WebAuthn on `HidFido | SmartCard` overridden to `SmartCard` records `SmartCard`; YubiOtp on `SmartCard | HidOtp` overridden to `HidOtp` records `HidOtp`; Management on a multi-transport probe overridden to a non-default valid transport (e.g. `HidOtp`) records that transport;
  - (d) **device-unsupported applet-valid override** throws `NotSupportedException` and makes no connect attempt;
  - (e) **non-concrete override** (`Hid`, `All`, `Unknown`, and a combined flag such as `SmartCard | HidFido`) throws `ArgumentException` and makes no connect attempt;
  - (f) **applet-invalid concrete override**: `Fido2`/`WebAuthn` + `HidOtp` and `YubiOtp` + `HidFido` throw `ArgumentException` **even when the device exposes that transport**, and make no connect attempt;
  - (g) **Fido2/WebAuthn both-transports** device now defaults to `HidFido` (no throw, probe records `HidFido`);
  - (h) **Fido2/WebAuthn no FIDO-capable transport** (`AvailableConnections = HidOtp` or `Unknown`) throws `NotSupportedException` and makes no connect attempt (preserves the pre-Phase-38 guarantee that the throw replacement does not regress this path);
  - (i) **Fido2/WebAuthn SCP does not change selection** (ISC-9.1): on a `HidFido | SmartCard` probe, `CreateFidoSessionAsync` / `CreateWebAuthnClientAsync` with `scpKeyParams` supplied and `preferredConnection == null` still records `HidFido` (the deferred "SCP implies SmartCard" behavior is NOT implemented); SmartCard is reached only via an explicit `preferredConnection: ConnectionType.SmartCard`. This guards against an implementer silently forcing SmartCard when SCP is supplied.
- [ ] ISC-13.1: Because the Fido2/WebAuthn default flips to `HidFido -> SmartCard`, existing transport-specific (SmartCard-tagged) Fido2/WebAuthn **integration** helpers/tests that previously relied on default routing to reach SmartCard must be updated to pin the SmartCard path explicitly — pass `preferredConnection: ConnectionType.SmartCard` (or use a direct typed `ConnectAsync<ISmartCardConnection>()`), or be renamed to generic default-route tests — so that on a merged composite key they cannot silently pass over HID FIDO and become false positives for the SmartCard path. The inventory is established by a **grep-driven sweep**, not a fixed list: enumerate every SmartCard-tagged Fido2/WebAuthn test or helper that reaches default FIDO/WebAuthn session creation — whether by calling `CreateFidoSessionAsync(...)` / `CreateWebAuthnClientAsync(...)` directly, via `state.WithFidoSessionAsync(...)`, or **indirectly through any wrapper helper that internally routes through default session creation** (e.g. `GetFidoInfoAsync`) — and pin each. Known hits to include at minimum: `src/Fido2/tests/.../TestExtensions/FidoTestStateExtensions.cs`, `src/Fido2/tests/.../FidoSmartCardTests.cs`, `src/Fido2/tests/.../FidoTransportTests.cs`, and `src/WebAuthn/tests/.../WebAuthnClientFactoryTests.cs`.
- [ ] ISC-13.2: SmartCard pinning under ISC-13.1 must be **opt-in, not applied to shared default paths**. The shared `WithFidoSessionAsync` helper's default path must stay unpinned (so HID-lane tests keep exercising the new `HidFido`-first default); add a dedicated SmartCard helper/overload OR an optional `preferredConnection` argument to the helper (default unpinned) and have only the SmartCard-tagged callers pass `ConnectionType.SmartCard`. Anti: changing the shared helper to always pin SmartCard (which would stop HID-lane tests from covering the default and create transport-mismatched false positives).
- [ ] ISC-14: Focused unit suites for Core, Management, YubiOtp, Fido2, WebAuthn pass; full solution build passes with 0 warnings/0 errors; Core has no dependency on `Yubico.YubiKit.Management`. A grep/review confirms no delegate or method-group binding of the four changed extension methods exists in-repo (so the optional-parameter addition does not source-break a consumer); any found binding is updated or noted.
- [ ] ISC-15: Active documentation validates (`docs-qa`); the per-app default-order table is documented in the relevant module docs/README; changed-file formatting verifies clean; `git diff --check` is clean.
- [ ] ISC-15.1: The XML doc comments on all four changed public methods (`CreateManagementSessionAsync`, `CreateYubiOtpSessionAsync`, `CreateFidoSessionAsync`, `CreateWebAuthnClientAsync`) are updated: a `<param name="preferredConnection">` describing null=default-order and explicit-override semantics; the per-app default order in `<remarks>`; and for Fido2/WebAuthn the removal of the stale dual-transport-throw wording plus a retained note that SCP is ignored over HID FIDO unless SmartCard is explicitly selected.
- [ ] ISC-16: Safe hardware smoke against serial 103 exercises the **changed module extension entry points directly** (not a Core-only run): Management `CreateManagementSessionAsync` and YubiOtp `CreateYubiOtpSessionAsync` default-select succeed; an explicit override to a supported HID transport on a multi-transport device succeeds; Fido2 `CreateFidoSessionAsync` default selects HID FIDO. The SmartCard-path checks (explicit `preferredConnection: ConnectionType.SmartCard` reaching SmartCard FIDO2, no-UP) require the CCID to be free; if it is held and cannot be freed out-of-band, those checks are skipped with a recorded rationale (no `scdaemon` kill is performed as part of verification). The smoke MUST exercise the new explicit-override path, not only defaults: each changed module's smoke (or a recorded ad hoc verification command/script) calls the entry method with a non-default `preferredConnection` on the composite key and records the resulting transport, so the override overloads are proven end-to-end on hardware (not just by unit fakes). Run via focused module integration smoke (`--project Management`, `--project YubiOtp`, `--project Fido2`), not `--project Core`. WebAuthn hardware coverage is satisfied by the Fido2 smoke plus the WebAuthn pass-through unit tests (ISC-7/ISC-13), because `CreateWebAuthnClientAsync` adds no independent transport logic and forwards `preferredConnection` to the shared Fido2 path; no separate WebAuthn hardware smoke is required. No UP/UV/touch. The local allow-list edit is reverted before commit.
- [ ] ISC-17: /DevTeam review (interim GPT-5.4) returns pass or all findings fixed; GPT-5.5 review queued; review output recorded.
- [ ] ISC-18: Phase 38 learning note records the override model, the per-app defaults, source changes, review status, verification (incl. hardware evidence), and feeds Phase 38.5 (held-transport fallback error taxonomy and the `null`-default fallback insertion point).
- [ ] ISC-19: Master ISA ISC-21, ISC-21.1, ISC-22, ISC-23, ISC-24 are checked, and the Phase 38 row is reconciled.

### Anti-Criteria

- [ ] ISC-20: Anti: an explicit override silently falls back to another transport (fallback is 38.5 and only for the `null` default path).
- [ ] ISC-21: Anti: an override parameter is added to a single-transport applet, or a single-transport applet's SmartCard-only behavior changes.
- [ ] ISC-22: Anti: held-transport fallback, `SCARD_E_SHARING_VIOLATION` handling, or process-killing is implemented in this phase.
- [ ] ISC-23: Anti: a Core reference to Management is introduced, or the local allow-list edit (serial 103) is committed.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | branch | Active branch | `## yubikit-composite-device-new` | `git status --short --branch` |
| ISC-2 | design | Decisions recorded | present before edits | Read |
| ISC-3 | review | Interim Cato ISA review | pass/resolved | `scripts/interim-cross-vendor-review.sh` |
| ISC-4 to ISC-7 | source/unit | Override param present, default/exact/throw semantics, WebAuthn pass-through | tests pass | `dotnet toolchain.cs -- test --project <M> --filter ...` |
| ISC-8 to ISC-10 | unit | Per-app default order; Fido2 throw replaced; no placeholder/scalar | tests pass | unit tests |
| ISC-11 to ISC-12 | source | Single-transport applets unchanged; no fallback | read/grep | Read + grep |
| ISC-13 | unit | Fake-probe transport tests per multi-transport module | tests pass | unit tests |
| ISC-14 | build | Focused + full build, no Core->Mgmt | exit 0 / clean | `dotnet toolchain.cs build` + grep |
| ISC-15 | docs/format | docs-qa, default table, changed-file format, whitespace | exit 0 / clean | `dotnet toolchain.cs -- docs-qa`; `dotnet format --verify-no-changes`; `git diff --check` |
| ISC-16 | integration | Hardware smoke (serial 103, CCID free) via module entry points | defaults + override succeed; allow-list reverted | `dotnet toolchain.cs -- test --project Management --integration --filter ...` (and `--project YubiOtp`, `--project Fido2`) |
| ISC-17 | review | /DevTeam review | pass/fixed | review output |
| ISC-18 to ISC-19 | file | Learning note; master ISA reconciled | present/checked | Read |
| ISC-20 to ISC-23 | scope/dep | Anti-guards | no override fallback, no single-transport override, no fallback impl, no Core->Mgmt, allow-list reverted | tests / grep / git diff |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Phase 38 ISA + interim Cato | Write this ISA; record decisions; run interim Cato before edits. | ISC-1, ISC-2, ISC-3 | Phase 37.5 | false |
| Override + defaults | Add `preferredConnection` to Management/YubiOtp/Fido2/WebAuthn; keep default orders; replace Fido2 throw with `HidFido -> SmartCard`; remove placeholders. | ISC-4, ISC-5, ISC-6, ISC-7, ISC-8, ISC-9, ISC-10, ISC-11, ISC-12 | interim Cato pass | false |
| Transport tests | Fake-probe `IYubiKeyExtensionsTransportTests` for Management/YubiOtp/WebAuthn; extend Fido2's. | ISC-13 | override + defaults | true |
| Verify, smoke, review, learn, commit | Build, docs/format/dep, default-table docs, hardware smoke, /DevTeam review, learnings, master ISA reconcile, commit. | ISC-14, ISC-15, ISC-16, ISC-17, ISC-18, ISC-19, ISC-20..23 | implementation complete | false |

## Decisions

- 2026-06-11: **Override shape is an optional `ConnectionType? preferredConnection = null`** placed
  immediately before the trailing `CancellationToken` (CA1068 + warnings-as-errors require `CancellationToken`
  last). Chosen over a `params ConnectionType[]` list (ambiguous empty-array semantics, larger surface).
  To keep full **source** compatibility for the old positional shape (which ended in `CancellationToken`),
  each method retains a back-compatibility overload with the exact pre-Phase-38 positional signature
  (all-required params, ending in `CancellationToken`) that forwards with `preferredConnection: null`.
  Better-function-member resolution disambiguates: the overload wins the old full-positional call; the new
  method wins all default/partial/named calls (verified — no `CS0121`). Adding an optional parameter
  remains a binary signature change for precompiled callers, which is acceptable at `2.0.0-preview`.
  (The back-compat overload was added in response to the interim /DevTeam review HIGH finding.)
- 2026-06-11: **`null` = documented default order via `ResolvePreferredConnection`; a concrete value is
  used exactly.** Supported concrete → used; unsupported concrete → `NotSupportedException`; non-concrete
  (`Hid`/`All`/`Unknown`) → `ArgumentException`. An override never falls back.
- 2026-06-11: **Default orders**: Management `SmartCard -> HidFido -> HidOtp`; YubiOtp `SmartCard -> HidOtp`
  (parity with shipped gating); Fido2/WebAuthn `HidFido -> SmartCard` (master ISC-22), replacing the
  Phase 36 placeholder dual-transport throw.
- 2026-06-11: **Single-transport applets get no override** (master ISC-23: overrides only where a module
  can reasonably use more than one transport). Piv/Oath/OpenPgp/SecurityDomain/YubiHsm stay SmartCard-only.
- 2026-06-11: **Held-transport fallback is Phase 38.5, not here.** The `null`-default path is the designated
  insertion point for 38.5 fallback so the public signature does not change again. Detection taxonomy
  (`SCARD_E_SHARING_VIOLATION` / `SCARD_E_SERVER_TOO_BUSY` via `SCardException`) is recorded for 38.5 in the
  learning note.
- 2026-06-11: **Guards added after interim Cato review (CONCERNS):**
  - Override validation distinguishes three failure modes (ISC-6): non-concrete / not-exactly-one-flag (incl. combined flags like `SmartCard | HidFido`) → `ArgumentException`; concrete-but-applet-invalid (e.g. `HidOtp` for Fido2, `HidFido` for YubiOtp), even if device-supported → `ArgumentException`; concrete + applet-valid + device-unsupported → `NotSupportedException`. All validation happens before any connect attempt.
  - Per-applet valid-transport sets are pinned (ISC-6.1) and bound both default resolution and override validation.
  - Test coverage (ISC-13) requires fallback-order cases (not just top-choice), applet-invalid override cases, combined-flag cases, and the Fido2/WebAuthn "no FIDO-capable transport" `NotSupportedException` case so the throw replacement does not regress.
  - The `null`-default branch is kept distinct and candidate-order-aware (ISC-12.1) so Phase 38.5 wraps only it.
- 2026-06-11: **SCP-implied transport selection considered, then deferred (interim Cato rounds 5 -> 8).** A round-5 finding suggested making `scpKeyParams` imply SmartCard so the HID-first default never silently drops a secure channel. On round 8 this was correctly identified as scope creep beyond master ISC-21..24 (defaults+overrides) and as contradicting the fixed valid-transport model (ISC-6.1). Resolution: keep Phase 38 to defaults+overrides only; retain the pre-existing documented behavior (SCP ignored over HID; caller must select SmartCard explicitly for SCP); record SCP-implied selection as a deferred follow-up (Out of Scope). ISC-9.1 now states the no-change rule.
- 2026-06-11: **Compatibility claim narrowed (interim Cato rounds 4-5).** ISC-4 preserves **source** compatibility for direct call sites only; adding an optional parameter is binary-breaking for precompiled callers and source-breaking for method-group/delegate consumers. Implementation must grep/review for delegate or method-group bindings of the four changed extension methods (ISC-14 note). Acceptable at `2.0.0-preview`; use additive overloads if binary compatibility ever becomes required.

## Changelog

- conjectured: Phase 36's mechanical migration (Fido2 dual-transport throw; Management/YubiOtp gating
  comments) was enough until a full smart-default/override policy was designed.
  refuted by: a multi-connection physical device makes the Fido2 throw user-hostile and leaves callers no
  way to force a transport; the master ISA (ISC-21..24) requires documented app-specific defaults plus
  explicit overrides.
  learned: add an optional `ConnectionType? preferredConnection` override, keep per-app default orders,
  default Fido2/WebAuthn to HID FIDO then SmartCard, and scope overrides to multi-transport applets only.
  criterion now: ISC-4 to ISC-12 govern the override API and default policy; ISC-13 covers selection tests.
- conjectured: a single "concrete and supported" vs "unsupported" override check plus top-choice default
  tests were enough.
  refuted by: interim Cato (GPT-5.4, CONCERNS) — combined concrete flags (`SmartCard | HidFido`) are also
  non-concrete; an override that is device-supported but invalid for the applet (e.g. `HidOtp` for Fido2)
  needs rejection before connect; top-choice-only tests do not prove Management's three-step fallback order;
  and ISC-13 dropped the existing Fido2 "no FIDO transport" throw guarantee.
  learned: specify a three-mode override taxonomy with per-applet valid sets, validate before connect, and
  require fallback-order, applet-invalid, combined-flag, and Fido2-no-transport test cases; keep the
  null-default branch factored for Phase 38.5.
  criterion now: ISC-6, ISC-6.1, ISC-12.1, ISC-13 capture the guards.

## Verification

Populated in the Phase 38 learning note before commit.
