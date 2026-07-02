# Phase 36 Learnings: Physical YubiKey Model

Use this note as the handoff record for Phase 36 of the composite-device program.

## Phase Summary

- Branch: `yubikit-composite-device-new`.
- Scope: reshape Core `IYubiKey` into a physical-device abstraction (available connections, support predicate, ambiguity-safe default connect, set-correct filter matching) and remove the scalar `IYubiKey.ConnectionType`.
- Phase ISA: `docs/plans/composite-device/phase-36-physical-yubikey-model-ISA.md`.
- Deferred to Phase 37: composite discovery merge, repository physical-identity keying, and device-info access on `IYubiKey`.
- Deferred to Phase 38: applet extension smart-default/override transport selection.

## Disposition Decision (recorded before edits)

- Scalar `IYubiKey.ConnectionType` is REMOVED (v2 clean break, no shim) and replaced by:
  - `ConnectionType AvailableConnections { get; }` — concrete connect bits only (`SmartCard`/`HidFido`/`HidOtp`), never the `Hid` group or `All`.
  - `bool SupportsConnection(ConnectionType)` — default-interface predicate: concrete types only; `Hid` = a HID interface present; `Unknown`/`All`/mixed multi-bit = `false`.
  - Ambiguity-safe default `ConnectAsync()` (default-interface method): resolves only when exactly one connection is available; `InvalidOperationException` for multiple; `NotSupportedException` for none.

## What Changed

- `src/Core/src/Interfaces/IYubiKey.cs`: removed scalar `ConnectionType`; added `AvailableConnections`, default `SupportsConnection`, and ambiguity-safe default `ConnectAsync()`.
- `src/Core/src/YubiKey/ConnectionTypeExtensions.cs`: added `SupportsConnection` (concrete/Hid semantics), `Matches` (set-correct filter; replaces the old `MatchesDevice` `HasFlag(set)` logic that was wrong for multi-bit sets), and `SingleConcreteConnectionOrUnknown`.
- `src/Core/src/YubiKey/PcscYubiKey.cs`, `src/Core/src/Hid/HidYubiKey.cs`: expose `AvailableConnections`.
- `src/Core/src/Hid/ConnectionTypeMapper.cs`: `ToConnectionType(Unknown)` now returns `Unknown` (not the `Hid` group) to honor the concrete-bits contract; `SupportsConnectionType` uses `Matches`.
- `src/Core/src/YubiKey/FindYubiKeys.cs`, `YubiKeyDeviceRepository.cs`: filter via `Matches(AvailableConnections)`.
- `src/Fido2/src/IYubiKeyExtensions.cs`: mechanical, preference-free FIDO transport selection in a renamed private `ConnectForFidoAsync` (called explicitly by `CreateFidoSessionAsync` to avoid binding ambiguity with the new default `ConnectAsync()`); both FIDO-capable transports present -> throw (Phase 38); neither -> throw.
- CLI + examples + `Tests.Shared`: migrated all scalar consumers (`DeviceSelectorBase`, `YkDeviceSelector`, every example tool `DeviceSelector`/`DeviceHelper`, `YubiKeyTestInfrastructure`) to `AvailableConnections`/`SupportsConnection`.
- Tests: added `PhysicalYubiKeyTests` (predicate, set matching, default-connect single/multi/none, typed connect) and `IYubiKeyExtensionsTransportTests` (FIDO2 single-routes/dual-throws/neither-throws); updated Core fakes and the mapper test.

## Review Evidence

- Interim cross-vendor reviews used the GPT-5.5 throttling workaround (`scripts/interim-cross-vendor-review.sh`, GPT-5.4 high reasoning); GPT-5.5 DevTeam/Cato reviews are QUEUED for when quota returns.
- Cato (interim, pre-edit, broad API-boundary gate): 5 rounds, converged to no blockers. It caught real plan defects that were fixed before coding: incomplete migration inventory (CLI selectors, example prompts, `Tests.Shared`); `MatchesDevice` being wrong for multi-bit sets; a FIDO2 preference that would leak Phase 38 policy; an `IYubiKey.GetDeviceInfoAsync` that would shadow the Management extension and create connection-ownership hazards (deferred to Phase 37); and a cross-phase sequencing gap for parameterless default-connect consumers. Outputs: `/tmp/opencode/cato-review-phase36-isa-output{,2,3,4,5}.md`.
- DevTeam (interim, post-implementation): PASS WITH NOTES. One LOW finding fixed: `ToConnectionType(Unknown)` returned the `Hid` group flag, violating the concrete-bits `AvailableConnections` contract; changed to `Unknown` and updated the mapper test. Output: `/tmp/opencode/devteam-review-phase36-output.md`.

## Verification Evidence

- Branch: `git status --short --branch` -> `## yubikit-composite-device-new`.
- Rust ref: `git -C ../yubikey-manager status --short --branch` -> `## experiment/rust...origin/experiment/rust`.
- Full solution build: `dotnet toolchain.cs build` -> succeeded, 0 warnings, 0 errors.
- Core unit suite: 426 passed, 2 skipped, 0 failed; new `PhysicalYubiKeyTests` 22/22; `IYubiKeyExtensionsTransportTests` 4/4; `ConnectionTypeMapper` tests 13/13.
- ISC-13 proof: the solution compiles with no `IYubiKey.ConnectionType` member (compile-enforced), so no production code reads a scalar per-interface connection type; remaining `.ConnectionType` reads are `Tests.Shared` `YubiKeyTestState` wrapper state and `DeviceSelection.ConnectionType`/`IConnection.Type` (all exempt).
- Dependency direction: Core has no `ProjectReference` to Management and no Management namespace usage.
- Changed-file format clean; `dotnet toolchain.cs -- docs-qa` validated 54 files; `git diff --check` clean.

## What Did Not Work / Hazards Found

- `CreateFidoSessionAsync` previously called `yubiKey.ConnectAsync(ct)`, which is ambiguous between the new `IYubiKey` default `ConnectAsync()` and the FIDO2 private selector. Renaming the private method to `ConnectForFidoAsync` and calling it explicitly removed the ambiguity and preserved FIDO2 parity (OTP-only must throw, not resolve).
- The `dotnet format`/rg glob exclusion for `Tests.Shared` did not filter as expected; classification was done manually and is compile-enforced.
- The toolchain test `--filter` does not support `|` OR expressions; run one substring filter per invocation or the whole project suite.

## Reusable Patterns

- For a flags-typed capability set, provide a set-correct `Matches(filter, available)` helper; never reuse a scalar `HasFlag(set)` check for multi-bit capability sets.
- Put cross-cutting predicates (`SupportsConnection`) and safe defaults (`ConnectAsync()`) as default-interface methods defined in terms of one required property (`AvailableConnections`), so implementers stay minimal.
- When a default-interface method and an extension method share a name/signature, rename the extension and call it explicitly to avoid silent binding to the interface default.
- Removing an interface member makes the migration compile-enforced: a green full build is a stronger "no stale consumer" proof than grep.

## Deferred Candidates

- Phase 37: composite discovery merge, repository keying by physical identity, add/remove events, and device-info access on `IYubiKey` (read via the Phase 35 Core reader; decide eager vs lazy and connection ownership/disposal then).
- Phase 38: applet extension smart-default/override transport selection (FIDO2, Management, YubiOtp), replacing the Phase 36 mechanical FIDO2 stopgap.
- Queued: GPT-5.5 DevTeam review of the Phase 36 commit and a GPT-5.5 Cato review of the Phase 36 ISA, when quota returns.

## Next Phase Inputs (Phase 37)

- `AvailableConnections` is a set and `Matches` is set-correct, so Phase 37 can produce multi-connection physical devices and discovery filtering will behave correctly; multi-bit behavior is already unit-tested.
- SEQUENCING RULE (from master ISA): Phase 37 must not ship merged multi-connection physical devices until the parameterless default-connect consumers (Management/YubiOtp/FIDO2 applet extensions calling `yubiKey.ConnectAsync()`) are rewritten or gated by Phase 38, because a merged device makes the default connect ambiguous (throws). Land/gate Phase 38 before or with the Phase 37 cutover.
- When Phase 37 adds device-info on `IYubiKey`, use a distinct name from the Management `GetDeviceInfoAsync` extension (or supersede it deliberately) to avoid silent rebinding, and define connection ownership/disposal for the read.

## Compact Summary

- Goal: make `IYubiKey` a physical device; remove scalar `ConnectionType`.
- Branch: `yubikit-composite-device-new`.
- Status: implemented, verified (build 0/0, Core 426 pass + new tests); interim Cato (converged) and DevTeam (PASS WITH NOTES, fixed) done; GPT-5.5 reviews queued.
