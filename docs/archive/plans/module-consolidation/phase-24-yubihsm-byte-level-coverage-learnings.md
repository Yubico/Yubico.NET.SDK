# Phase 24 Learnings: YubiHsm Byte-Level Coverage

Phase 24 added narrow byte-level YubiHsm Auth coverage and fixed one Core APDU payload-lifecycle gap found while making the YubiHsm sensitive-buffer criteria observable.

## Changed Files

- `src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.UnitTests/HsmAuthSessionByteLevelTests.cs`
  - Added PUT symmetric credential APDU/TLV coverage.
  - Added CALCULATE symmetric session-key APDU/TLV coverage.
  - Added DELETE retry-counter failure coverage for SW `0x63C2`.
- `src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.UnitTests/Yubico.YubiKit.YubiHsm.UnitTests.csproj`
  - Added `Tests.Shared` project reference so YubiHsm unit tests can use `RecordingSmartCardConnection`.
- `src/Core/src/SmartCard/ApduTransmitter.cs`
  - Zeroes the mutable formatted APDU payload buffer in `finally` after transmit returns or throws.
- `src/Core/src/SmartCard/IApduFormatter.cs`
  - Changed formatter outputs from `ReadOnlyMemory<byte>` to `Memory<byte>` so the ownership and zeroing contract is explicit.
- `src/Core/src/SmartCard/ApduFormatterShort.cs`
  - Returns `Memory<byte>` for the newly allocated short APDU wire payload.
- `src/Core/src/SmartCard/ApduFormatterExtended.cs`
  - Returns `Memory<byte>` for the newly allocated extended APDU wire payload.
- `src/Core/src/SmartCard/Scp/ScpProcessor.cs`
  - Uses the mutable formatter payload directly and zeroes it with `CryptographicOperations.ZeroMemory(formattedApdu.Span)`.
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/SmartCard/PcscProtocolTests.cs`
  - Added RED/GREEN unit coverage proving formatted APDU payload buffers are zeroed after transmit.
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/SmartCard/Fakes/FakeSmartCardConnection.cs`
  - Copies transmitted commands for test assertions, since production now zeroes formatted payload buffers after transmit.
- `docs/plans/module-consolidation/phase-24-yubihsm-byte-level-coverage-ISA.md`
  - Records Phase 24 scope, constraints, criteria, and verification strategy.

## Security Findings

- `HsmAuthSession` already zeroed its internal encoded TLV `data` buffers and parsed credential passwords in `finally` blocks.
- Core `ApduTransmitter` formatted each APDU into a fresh array and passed that array to `ISmartCardConnection`, but did not zero the formatted array after transmit.
- The new Core RED test failed before the production fix because a retaining fake connection could still observe sensitive formatted APDU bytes after transmit completed.
- The first fix used `ReadOnlyMemory<byte>` plus `MemoryMarshal.TryGetArray`, but user review correctly challenged that as the wrong ownership model.
- The final fix changes `IApduFormatter.Format` to return `Memory<byte>`, because the formatter produces a freshly allocated owned buffer and the caller must be able to clear it directly.
- `ApduTransmitter` and `ScpProcessor` now zero formatter outputs through `.Span`, removing the array-backed-only `TryGetArray` assumption.

## Verification Evidence

- Branch check: `git status --short --branch` showed `## yubikit-consolidation...origin/yubikit-consolidation [ahead 2]` before implementation.
- RED: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~TransmitAndReceiveAsync_ZerosFormattedPayloadAfterTransmit"` failed before the Core fix with `Expected the formatted APDU payload buffer to be zeroed after transmit.`
- GREEN: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~TransmitAndReceiveAsync_ZerosFormattedPayloadAfterTransmit"` passed after the Core fix.
- Focused YubiHsm: `dotnet toolchain.cs -- test --project YubiHsm --filter "ClassName~HsmAuthSessionByteLevelTests"` passed, 3/3.
- Core build: `dotnet toolchain.cs -- build --project Core` passed, 0 warnings/errors.
- Core tests: `dotnet toolchain.cs -- test --project Core` passed, 343 total, 341 succeeded, 2 hardware-skipped SCP11 tests.
- Core recheck after `Memory<byte>` formatter refactor: `dotnet toolchain.cs -- build --project Core` passed, then `dotnet toolchain.cs -- test --project Core` passed, 343 total, 341 succeeded, 2 hardware-skipped SCP11 tests.
- YubiHsm build: `dotnet toolchain.cs -- build --project YubiHsm` passed, 0 warnings/errors across module, unit tests, and integration test project builds.
- YubiHsm tests: `dotnet toolchain.cs -- test --project YubiHsm` passed, 54/54.
- YubiHsm recheck after `Memory<byte>` formatter refactor: `dotnet toolchain.cs -- build --project YubiHsm` passed, then `dotnet toolchain.cs -- test --project YubiHsm` passed, 54/54.
- Docs QA: `dotnet toolchain.cs -- docs-qa` passed, 54 active documentation files validated.
- Whitespace: `git diff --check` emitted only line-ending warnings, no whitespace errors.

## Review Evidence

- DevTeam reviewer route was forced with primary model context: `google-vertex-anthropic/claude-opus-4-8@default`.
- Review prompt: `/tmp/opencode/phase24-devteam-review-prompt.md`.
- Review output: `/tmp/opencode/phase24-devteam-review.md`.
- Verdict: no material correctness, security, test, API, or compatibility defects found.
- Review low-risk finding: `IApduFormatter.Format` return ownership was an implicit security contract.
- Follow-up design correction: documentation alone was weaker than the contract required. `IApduFormatter.Format` now returns `Memory<byte>` so ownership and clearability are represented in the type system.

## Integration Decision

- No YubiHsm integration tests were run in Phase 24.
- Rationale: the new coverage targets byte-level APDU/TLV encoding and sensitive payload lifecycle through fake SmartCard connections; the relevant YubiHsm Auth operations mutate persistent applet state and remain deferred without revised hardware/reset approval.
- No FIDO, FIDO2, WebAuthn, User Presence, or User Verification behavior was claimed or tested.

## Phase 25 Recommendation

- Continue to the next module in the consolidation sequence with the Phase 23/24 pattern:
  - Use `RecordingSmartCardConnection` for byte-level SmartCard APDU tests.
  - Assert ordered protocol bytes rather than tag presence only.
  - Keep production refactors out unless a failing test proves a defect.
  - Treat formatter/transmitter buffer ownership as a cross-module security invariant.
