# Phase 26 Learnings: FIDO2 Remaining CTAP Consistency

Use this note as the handoff record for Phase 26 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Scope: audit remaining FIDO2 CTAP request construction after Phase 6 and add byte-level coverage for the weakest high-risk surfaces.
- Phase ISA: `docs/plans/module-consolidation/phase-26-fido2-remaining-ctap-consistency-ISA.md`
- Source changed: `CredentialManagement` now keeps its public `FidoSession` constructor but stores an `IFidoSession` internally and sends command-prefixed requests through `SendCborRequestAsync`.
- Tests changed: added Credential Management wire tests and Client PIN retry request wire assertions.
- Prerequisite fixed: FIDO2 integration tests now reference `Xunit.SkippableFact` directly, matching the Phase 25 OpenPGP fix after `Tests.Shared` made xUnit v2 skip packages private.
- Integration requiring UP, UV, touch, reset timing, insert/remove, or known PIN state: not run.

## CTAP Surface Audit

- `FidoSession` MakeCredential/GetAssertion: covered by Phase 6 `FidoSessionRequestEncodingTests`.
- `ClientPin`: used `CtapRequestBuilder`, but most tests only checked call count; Phase 26 added command byte and canonical key-order assertions for `GetRetries` and `GetUvRetries`.
- `CredentialManagement`: model-decode tests existed, but no outgoing request byte tests were possible because the class depended on concrete `FidoSession` for internal `SendCborAsync(byte, payload)`.
- `AuthenticatorConfig`: already captures outgoing request bytes for subcommands, command byte, protocol version, auth param, and auth message.
- `FingerprintBioEnrollment`: already captures request bytes for sensor info and enroll timeout, with call-count coverage for other operations.
- `LargeBlobStorage`: already captures command byte and request-map shape for read/write, protocol version, and auth param.

## What Changed

- Removed the concrete `FidoSession` field from `CredentialManagement` and replaced it with `IFidoSession`.
- Preserved the public constructor signature `CredentialManagement(FidoSession, IPinUvAuthProtocol, ReadOnlyMemory<byte>)`.
- Added an internal constructor `CredentialManagement(IFidoSession, IPinUvAuthProtocol, ReadOnlyMemory<byte>)` for unit-test request capture.
- Replaced `FidoSession.SendCborAsync(CtapCommand.CredentialManagement, payload, ...)` with explicit command-prefix construction plus `IFidoSession.SendCborRequestAsync(...)`.
- Zeroed the command-prefixed request buffer after send.
- Added `CredentialManagementWireTests` for metadata and delete-credential request bytes.
- Added Client PIN canonical request tests for PIN retry and UV retry subcommands.
- Added direct `Xunit.SkippableFact` reference to the FIDO2 integration test project.

## Why This Shape

- Promoting `SendCborAsync(byte, payload)` to `IFidoSession` would leak an internal protocol helper into the public interface.
- Keeping the public `FidoSession` constructor avoids API drift while the internal `IFidoSession` seam makes byte-boundary tests possible.
- Adding operation-specific command objects would violate the Phase 6 house style: encoding helpers are acceptable, command frameworks are not.
- Unit request capture proves CTAP bytes more directly than any unattended hardware integration run would.

## Verification Evidence

- Red diagnostic: `vslsp query --file ...CredentialManagementWireTests.cs --port 7850` returned `CS1503` for both new tests because `IFidoSession` could not be passed to the public `CredentialManagement(FidoSession, ...)` constructor.
- Focused Credential Management command: `dotnet toolchain.cs -- test --project Fido2.UnitTests --filter "FullyQualifiedName~CredentialManagementWireTests"`
- Focused Credential Management result: passed, 2/2.
- Focused Client PIN command: `dotnet toolchain.cs -- test --project Fido2.UnitTests --filter "FullyQualifiedName~ClientPinTests"`
- Focused Client PIN result: passed, 27/27.
- FIDO2 unit command: `dotnet toolchain.cs -- test --project Fido2.UnitTests`
- FIDO2 unit result: passed, 399/399.
- FIDO2 build command: `dotnet toolchain.cs -- build --project Fido2`
- FIDO2 build result: passed for source, integration tests, and unit tests with 0 warnings and 0 errors after adding the direct skip package reference.
- Scoped format command: `dotnet format --verify-no-changes --include <Phase 26 touched C#/csproj files>`
- Scoped format result: passed after applying scoped `dotnet format` to touched files.
- Docs QA command: `dotnet toolchain.cs -- docs-qa`
- Docs QA result: passed, 54 active documentation files validated.
- Read-only integration command: `dotnet toolchain.cs -- test --integration --project Fido2 --smoke --filter "FullyQualifiedName~FidoGetInfoTests"`
- Read-only integration result: passed 8/8 on HidFido serial `103`, firmware `5.8.0`.
- DevTeam review: passed with no material defects; review noted optional future wire tests for authenticated-message content and additional Credential Management encoders.

## What Did Not Work

- `ClassName~CredentialManagementWireTests|ClassName~ClientPinTests` did not match under the xUnit v3 toolchain path; `FullyQualifiedName~...` matched and was used instead.
- Initial FIDO2 build failed before source verification because the integration project lacked direct `Xunit.SkippableFact`, producing `SkippableTheoryAttribute` missing errors.

## Reusable Patterns

- If a command class depends on concrete `FidoSession` only to access internal command-prefix send helpers, prefer an internal `IFidoSession` seam plus explicit command-prefixed request construction.
- Preserve public constructors while adding internal test seams when public API stability matters.
- Use byte-copy capture in tests when production zeroes the transmitted request buffer after send.
- FIDO2 xUnit v2 integration projects need direct skip package references when `Tests.Shared` keeps its packages private.

## Next Phase Inputs

- Required reading before next phase: this learning note.
- Watch for the same concrete-session testability smell in any remaining FIDO2 advanced-command surfaces.
- Optional FIDO2 follow-up: add byte-level tests for `EnumerateCredentialsAsync`, `UpdateUserInformationAsync`, `EnumerateRelyingPartiesAsync`, and authenticated-message content if FIDO2 gets another focused pass.
- Do not promote internal send helpers into public interfaces without a broader API design phase.
- Continue using `FullyQualifiedName~...` filters for FIDO2 xUnit v3 focused runs.
- Next recommended phase: Phase 27 should continue consolidation outside FIDO2 unless a review finds another high-risk FIDO2 command gap.

## Compact Summary

- Goal: prove remaining FIDO2 CTAP command surfaces at byte boundary.
- Main fix: Credential Management now has an internal `IFidoSession` seam.
- Tests added: Credential Management wire tests and Client PIN retry wire tests.
- Build prerequisite: direct `Xunit.SkippableFact` reference for FIDO2 integration tests.
- Verification: focused wire tests, full FIDO2 unit tests, FIDO2 build, docs QA, format, and read-only integration passed.
