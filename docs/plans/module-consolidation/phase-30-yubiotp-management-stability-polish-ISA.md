# Phase 30 YubiOtp And Management Stability Polish ISA

## Problem

Phase 30 exists to keep two comparatively stable modules stable before the final quality gate. The Phase 20 program allows only source-backed polish here: YubiOtp protocol-codec noise if real, and Management backend payload/read-path clarity if real. A read-only audit found no YubiOtp source change that would improve clarity without churn, but Management `GetDeviceInfoAsync` still has inline page validation and multi-page TLV accumulation with no focused unit coverage.

Management docs also describe the backend interface as three operations even though the current interface includes `SetModeAsync`; that active documentation should stay source-backed when the read path is touched.

## Vision

Management device-info reads should be easier to reason about and unit-tested at the backend boundary without changing public APIs or transport backends. YubiOtp should remain unchanged unless a real defect or simplification is found. The phase should raise confidence through tests and a minimal readability extraction, not style churn.

## Out of Scope

- No YubiOtp production source changes unless a failing test reveals a concrete defect.
- No public YubiOtp or Management API changes.
- No Management backend interface changes.
- No Management configuration-change integration tests or destructive device-state tests.
- No Management integration test source changes; direct test-runtime package reference is allowed if build/smoke exposes a missing skip assembly.
- No broad Management or YubiOtp example CLI cleanup.
- No command/executor abstractions.
- No composite YubiKey design work.

## Principles

- Prefer tests and small extraction over architecture changes.
- Keep Management backend delegation visible.
- Use fake backend unit tests for device-info read behavior.
- Keep YubiOtp stable if the audit finds no real protocol-codec cleanup target.
- Keep active module documentation aligned with source when touched.

## Constraints

- Branch must be `yubikit-consolidation` before edits and verification.
- Required phase inputs: master ISA, Phase 20 program ISA, Phase 29 learning note, baseline/final reassessment, YubiOtp guidance, Management guidance.
- Use `dotnet toolchain.cs`; never raw `dotnet build` or `dotnet test`.
- Unit command: `dotnet toolchain.cs -- test --project Management --filter "FullyQualifiedName~ManagementSessionTests"`.
- Module command: `dotnet toolchain.cs -- test --project Management`.
- Build command: `dotnet toolchain.cs -- build --project Management`.
- Read-only integration smoke command: `dotnet toolchain.cs -- test --integration --project Management --filter "FullyQualifiedName~ManagementSessionSimpleTests.CreateManagementSession_WithSmartCardConnection_ReturnsValidSession" --smoke`.
- Docs command: `dotnet toolchain.cs -- docs-qa`.
- Stage only intended Phase 30 files.

## Goal

Add focused unit coverage for Management device-info read behavior, extract the page read/validation logic only if it makes `GetDeviceInfoAsync` clearer, update stale Management backend docs, and record the YubiOtp audit as no source change.

## Criteria

- [x] ISC-1: Phase 30 ISA exists before source edits.
- [x] ISC-2: Branch check confirms `yubikit-consolidation` before source edits.
- [x] ISC-3: Required phase inputs and module guidance were read.
- [x] ISC-4: YubiOtp audit records no source-backed production change target, or a concrete failing test if one is found.
- [x] ISC-5: Management unit coverage proves multi-page device-info reads request page 0 then page 1 when the `more device info` TLV is present.
- [x] ISC-6: Management unit coverage proves malformed page length throws `BadResponseException` before TLV parsing.
- [x] ISC-7: `GetDeviceInfoAsync` remains backend-delegated and public API signatures remain unchanged.
- [x] ISC-8: No Management config mutation or destructive integration test is added.
- [x] ISC-9: Active Management docs describe the current backend operation set, including `SetModeAsync`.
- [x] ISC-10: Management integration project has direct `Xunit.SkippableFact` only if required by build/smoke dependency evidence or parity with existing integration-project pattern.
- [x] ISC-11: Focused Management unit tests pass through toolchain.
- [x] ISC-12: Full Management unit tests pass through toolchain.
- [x] ISC-13: Management build passes through toolchain.
- [x] ISC-14: Read-only Management integration smoke passes or records a concrete skip/failure reason.
- [x] ISC-15: Docs QA passes.
- [x] ISC-16: DevTeam/cross-vendor review completes or an approved waiver is recorded.
- [x] ISC-17: Phase 30 learning note records findings, verification, YubiOtp no-change rationale, and Phase 31 input.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | file | Read ISA path | Exists | Read |
| ISC-2 | git | `git status --short --branch` | Shows `yubikit-consolidation` | Bash |
| ISC-3 | file | Required input reads | Context loaded | Read |
| ISC-4 | audit | YubiOtp source/docs audit | No-change rationale or failing test | Read/Grep |
| ISC-5 | unit | Multi-page Management device-info read test | Page sequence and parsed serial pass | Bash |
| ISC-6 | unit | Invalid length Management read test | Throws `BadResponseException` | Bash |
| ISC-7 | diff | Public signatures/backend interface unchanged | No signature diff | git diff |
| ISC-8 | diff | No destructive integration additions | No config-mutating test additions | git diff |
| ISC-9 | docs | Management backend docs updated | `SetModeAsync` present | Grep/Read |
| ISC-10 | integration/config | Skip dependency fixed if needed | Build/smoke can resolve skip attributes | Bash |
| ISC-11 | command | Focused Management unit tests | 0 failures | Bash |
| ISC-12 | command | Full Management unit tests | 0 failures | Bash |
| ISC-13 | command | Management build | 0 errors | Bash |
| ISC-14 | command | Read-only Management smoke | 0 failures or documented blocker | Bash |
| ISC-15 | command | Docs QA | 0 failures | Bash |
| ISC-16 | review | DevTeam/cross-vendor review | Pass or approved waiver | DevTeam |
| ISC-17 | file | Learning note exists | Contains evidence | Read |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Management Read Tests | Add fake-backend tests for multi-page device info and malformed page length. | ISC-5, ISC-6 | branch check | false |
| Management Read Extraction | Extract page read/validation only if tests prove and preserve behavior. | ISC-7 | Management Read Tests | false |
| Management Docs Correction | Align active backend docs with current `IManagementBackend`. | ISC-9 | source audit | false |
| Phase Evidence | Run focused verification, review, and learning capture. | ISC-10..ISC-16 | implementation | false |

## Decisions

- 2026-06-08: YubiOtp receives read-only audit in Phase 30; no production source change is justified by the observed code because NDEF and HMAC codec helpers are already isolated and tested.
- 2026-06-08: Management read-path tests are selected because `GetDeviceInfoAsync` has meaningful backend/TLV behavior but only sparse unit coverage today.
- 2026-06-08: Management configuration-change integration tests remain out of scope because they reboot devices and persist state.
- 2026-06-08: Direct `Xunit.SkippableFact` is in scope for the Management integration project after build/smoke exposed unresolved `[SkippableTheory]` and `[SkippableFact]` attributes.

## Verification

- Branch check: `git status --short --branch` showed `## yubikit-consolidation...origin/yubikit-consolidation [ahead 9]` before Phase 30 edits.
- Required inputs read: master consolidation ISA, Phase 20 quality convergence ISA, Phase 29 learning note, baseline/final reassessment, YubiOtp guidance, and Management guidance.
- YubiOtp audit: no production source change target found; NDEF and HMAC codec helpers are already isolated and covered enough for this phase.
- Red test: `dotnet toolchain.cs -- test --project Management --filter "FullyQualifiedName~ManagementSessionTests"` failed on `GetDeviceInfoAsync_InvalidPageLength_ThrowsPageAwareBadResponse` because the old message was `Invalid length` and lacked page/declared/actual context.
- Focused Management unit command: `dotnet toolchain.cs -- test --project Management --filter "FullyQualifiedName~ManagementSessionTests"`.
- Focused Management unit result: passed 5/5 after extracting `ReadDeviceInfoPageAsync` and adding page-aware length diagnostics.
- Full Management unit command: `dotnet toolchain.cs -- test --project Management`.
- Full Management unit result: passed 117/117.
- Scoped format commands: `dotnet format src/Management/tests/Yubico.YubiKit.Management.UnitTests/Yubico.YubiKit.Management.UnitTests.csproj --include src/Management/tests/Yubico.YubiKit.Management.UnitTests/ManagementSessionTests.cs --verify-no-changes` and `dotnet format src/Management/src/Yubico.YubiKit.Management.csproj --include src/Management/src/ManagementSession.cs --verify-no-changes`.
- Scoped format result: both passed. Project-wide Management format remains out of scope because existing line-ending diagnostics exist outside touched files.
- Diff whitespace command: `git diff --check`.
- Diff whitespace result: passed; only the expected CRLF conversion warning appeared for the Management integration csproj.
- Management build command: `dotnet toolchain.cs -- build --project Management`.
- Management build result: initially failed on unresolved `[SkippableTheory]`/`[SkippableFact]` attributes, then passed with 0 warnings and 0 errors after adding the direct `Xunit.SkippableFact` reference.
- Read-only Management integration smoke command: `dotnet toolchain.cs -- test --integration --project Management --filter "FullyQualifiedName~ManagementSessionSimpleTests.CreateManagementSession_WithSmartCardConnection_ReturnsValidSession" --smoke`.
- Read-only Management integration smoke result: passed 1/1 after the same direct skip package fix.
- Docs QA command: `dotnet toolchain.cs -- docs-qa`.
- Docs QA result: passed after adding the learning note; 54 active documentation files validated.
- DevTeam review route: OpenAI primary routed reviewer to `google-vertex-anthropic/claude-opus-4-8@default`.
- DevTeam review result: `PASS`; two low optional notes only, no required changes.
