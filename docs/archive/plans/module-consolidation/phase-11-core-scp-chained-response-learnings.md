# Phase 11 Learnings: Core SCP Chained Response

Use this note as the handoff record for Phase 11 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Scope: fix Core SCP response chaining so send-remaining APDUs are SCP-wrapped after secure messaging is established
- Baseline integration test before source edits: passed
- Baseline command: `dotnet toolchain.cs -- test --integration --project Oath.IntegrationTests --smoke --filter "FullyQualifiedName~OathSession_Create_ReadsSelectMetadataWithoutReset"`
- Baseline result: 1 passed, 0 failed; serial `103`; test time `0.7461` seconds
- Source files changed: Core SmartCard response chaining and SCP setup only
- Test files changed: Core SCP protocol tests and OATH integration SCP metadata coverage
- Integration tests requiring reset, touch, User Presence, UV, or persistent-state mutation: not run
- Result: implementation verified by Core/OATH/SecurityDomain unit tests plus read-only OATH and SecurityDomain SCP03 integration checks
- Commit: recorded by the Phase 11 commit containing this learning note
- `/Ping` status: pending

## What Changed

- `PcscProtocol` now exposes a raw command processor for SCP setup while keeping the existing response-chaining base processor for non-SCP initialization.
- `ScpInitializer` initializes SCP with the pre-secure response-chaining processor, then builds the established secure processor as `ChainedResponseReceiver(ScpProcessor(raw command processor))`.
- `ScpInitializer` now uses local `Feature` gates for SCP03/SCP11 thresholds and preserves the existing `Major == 0` alpha/beta firmware sentinel allowance for those checks. This keeps OATH on serial `103`, whose applet reports a placeholder major version, aligned with `ApplicationSession.IsSupported(...)` until Phase 13 centralizes firmware-gate semantics.
- `ChainedResponseReceiver` now disposes an inner disposable processor so `PcscProtocolScp.Dispose()` still releases SCP session state when the SCP processor is nested under response chaining.
- Added a Core unit test proving a chained `0xA5` send-remaining command is wrapped with secure messaging before reaching the raw processor.
- Added a read-only OATH integration test proving OATH session metadata can be read over SCP03 without resetting OATH state.

## Why This Shape

- The previous established SCP shape was effectively `ScpProcessor(ChainedResponseReceiver(raw processor))`.
- In that shape, the first SCP command was wrapped, but if the card returned `SW1=0x61`, the inner `ChainedResponseReceiver` consumed the response chain and sent the follow-up command to the raw processor.
- The new established SCP shape is `ChainedResponseReceiver(ScpProcessor(raw processor))`.
- That makes the response-chain follow-up command pass through `ScpProcessor`, so send-remaining is MACed/wrapped like the original command.
- SCP setup still uses the pre-secure response-chaining processor because secure messaging is not established until initialization completes.

## Verification Evidence

- RED test command: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~CreateSecureProcessor_ChainedResponse_WrapsSendRemainingCommand"`
- RED result: failed at compile because `ScpInitializer.CreateSecureProcessor` did not exist.
- Focused GREEN command: `dotnet run --project src/Core/tests/Yubico.YubiKit.Core.UnitTests/Yubico.YubiKit.Core.UnitTests.csproj -c Release -- --filter-method "*CreateSecureProcessor_ChainedResponse_WrapsSendRemainingCommand*"`
- Focused GREEN result: 1 passed, 0 failed.
- Core SCP class command: `dotnet run --project src/Core/tests/Yubico.YubiKit.Core.UnitTests/Yubico.YubiKit.Core.UnitTests.csproj -c Release -- --filter-class "*PcscProtocolScpTests*"`
- Core SCP class result: 8 passed, 0 failed.
- Plain response-chaining command: `dotnet run --project src/Core/tests/Yubico.YubiKit.Core.UnitTests/Yubico.YubiKit.Core.UnitTests.csproj -c Release -- --filter-method "*TransmitAndReceiveAsync_ChainedResponseWithConfiguredInsSendRemaining_UsesConfiguredCommand*"`
- Plain response-chaining result: 1 passed, 0 failed.
- OATH session unit command: `dotnet run --project src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/Yubico.YubiKit.Oath.UnitTests.csproj -c Release -- --filter-class "*OathSessionTests*"`
- OATH session unit result: 11 passed, 0 failed.
- Core full unit command: `dotnet toolchain.cs -- test --project Core`
- Core full unit result: 289 passed, 0 failed, 2 skipped; fresh post-Feature-gate result: 289 passed, 0 failed, 2 skipped.
- OATH full unit command: `dotnet toolchain.cs -- test --project Oath.UnitTests`
- OATH full unit result: 81 passed, 0 failed; fresh post-Feature-gate result: 81 passed, 0 failed.
- SecurityDomain unit command: `dotnet toolchain.cs -- test --project SecurityDomain.UnitTests`
- SecurityDomain unit result: 28 passed, 0 failed; fresh post-Feature-gate result: 28 passed, 0 failed.
- OATH SCP integration command: `dotnet toolchain.cs -- test --integration --project Oath.IntegrationTests --smoke --filter "FullyQualifiedName~OathSession_CreateWithScp03_ReadsSelectMetadataWithoutReset"`
- OATH SCP integration result: 1 passed, 0 failed; serial `103`; final post-Feature-gate test time `0.7750` seconds.
- SecurityDomain SCP integration command: `dotnet toolchain.cs -- test --integration --project SecurityDomain.IntegrationTests --smoke --filter "FullyQualifiedName~CreateAsync_WithScp03_Succeeds"`
- SecurityDomain SCP integration result: 1 passed, 0 failed; serial `103`; final post-Feature-gate test time `2.5369` seconds.
- After-fix integration command: `dotnet toolchain.cs -- test --integration --project Oath.IntegrationTests --smoke --filter "FullyQualifiedName~OathSession_Create_ReadsSelectMetadataWithoutReset"`
- After-fix integration result: 1 passed, 0 failed; serial `103`; final post-format test time `0.9283` seconds.
- Touched-file format command: `dotnet format --verify-no-changes --include src/Core/src/SmartCard/ChainedResponseReceiver.cs src/Core/src/SmartCard/PcscProtocol.cs src/Core/src/SmartCard/Scp/ScpExtensions.cs src/Core/src/SmartCard/Scp/ScpInitializer.cs src/Core/tests/Yubico.YubiKit.Core.UnitTests/SmartCard/Scp/PcscProtocolScpTests.cs src/Core/tests/Yubico.YubiKit.Core.UnitTests/SmartCard/Scp/ScpExtensionsTests.cs src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/OathSessionTests.cs`
- Touched-file format result: passed; fresh post-Feature-gate result: passed.

## Tooling Notes

- Filtered xUnit v3 project runs through `toolchain.cs` hit the known `--minimum-expected-tests 0` runner bug.
- Direct xUnit v3 runner commands were used only for focused filtered unit tests after the project build path had already exposed that known toolchain issue.
- The explicit integration project match avoided the same wrapper issue and returned a clean integration baseline/comparison result.
- Repo-wide `dotnet format --verify-no-changes` remains blocked by unrelated existing end-of-line and import-order findings outside the Phase 11 touched files.
- A first SecurityDomain SCP integration rerun failed once during unauthenticated reset setup with `SW=0x6E00`; the immediate rerun passed. The final recorded result is the passing rerun.
- One OATH SCP03 integration rerun failed once at `EXTERNAL AUTHENTICATE` with `SW=0x6A80`; the immediate rerun passed. The final recorded result is the passing rerun.

## Review Evidence

- Cato route command: `bun ~/.claude/PAI/TOOLS/AgentHarnessRouter.ts --surface cato --role auditor --primary-model "openai/gpt-5.5" --cwd "$(pwd)" --dry-run --json`
- Cato route result: Vertex Opus 4.8 via `google-vertex-anthropic/claude-opus-4-8@default`.
- Cato audit command: `bun ~/.claude/PAI/TOOLS/AgentHarnessRouter.ts --surface cato --role auditor --primary-model "openai/gpt-5.5" --cwd "$(pwd)" --prompt-file /tmp/opencode/cato-phase11-prompt.txt --output /tmp/opencode/cato-phase11-audit.jsonl --execute --json --timeout-ms 150000`
- Cato verdict: `concerns`, low criticality.
- Cato blocking findings: none.
- Cato warning: two unrelated untracked Core YubiKey note files are present and must remain unstaged.
- Cato info finding: SCP03 constructs the final `ChainedResponseReceiver(ScpProcessor(...))` inline while SCP11 uses `CreateSecureProcessor(...)`; correct as-is, but a future micro-cleanup could unify the final wrapping shape if it stays small.
- Cato confirmed the Phase 11 core behavior: established SCP layering is `ChainedResponseReceiver(ScpProcessor(rawCommandProcessor))`, initialization remains pre-secure, SCP state disposal remains intact, and the local `Feature` gate plus `Major == 0` sentinel is scoped appropriately until Phase 13.

## Deferred Future Improvements

- Hardware OATH+SCP response-chaining coverage was not added because that would require a controlled SCP-enabled OATH scenario and potentially persistent device state.
- A broader SCP integration matrix should remain a separate approved hardware phase.
- No operation-specific command classes or broader APDU framework changes were introduced.
- A small SCP03/SCP11 helper-shape unification is available but intentionally not applied in Phase 11 because the current code is correct, tested, and smaller than adding a new overload only to remove one inline construction.

## Compact Summary

- Goal: wrap SCP response-chain send-remaining APDUs after secure messaging is established
- Baseline integration: passed before source edits on OATH metadata read
- Fix: established SCP processor is now response chaining over SCP over raw command processor
- Tests passed: focused Core SCP, plain chaining, OATH session, full Core, full OATH, full SecurityDomain
- Integration comparison: same OATH metadata read passed after fix
- Extra SCP coverage: OATH SCP03 metadata read and SecurityDomain SCP03 session creation passed
- Next phase recommendation: Phase 12 Core `ConnectionType` semantics
