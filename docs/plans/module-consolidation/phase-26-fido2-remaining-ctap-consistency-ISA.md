---
task: Phase 26 FIDO2 Remaining CTAP Consistency
slug: phase-26-fido2-remaining-ctap-consistency
effort: E3
phase: verify
progress: 20/20
mode: algorithm
started: 2026-06-08
updated: 2026-06-08
---

# Phase 26 FIDO2 Remaining CTAP Consistency ISA

## Problem

Phase 6 stabilized MakeCredential/GetAssertion request encoding, but other FIDO2 CTAP command surfaces still mix direct CBOR construction, session-level send helpers, and mock-only tests. The consolidation risk is that privileged CTAP paths can drift in command byte, integer-key ordering, or payload shape without a focused byte-level unit test catching it.

## Vision

The FIDO2 module should feel mechanically auditable: when a maintainer opens a command surface such as Client PIN, Credential Management, Authenticator Config, Bio Enrollment, or Large Blobs, the corresponding test proves the exact CTAP request bytes that leave the API boundary.

## Out of Scope

- No public FIDO2 API redesign.
- No operation-specific CTAP command object framework.
- No WebAuthn behavior changes.
- No unattended User Presence, User Verification, touch, reset-window, or insert/remove integration runs.
- No broad transport/backend refactor unless a failing test proves it is necessary.

## Principles

- CTAP request consistency is proven at the byte boundary, not by model-only assertions.
- Public session methods keep protocol lifecycle visible while pure encoding helpers may own local CBOR mechanics.
- Sensitive data ownership and zeroing semantics outrank backward-compatible internal shape preservation.

## Constraints

- Use `dotnet toolchain.cs -- build --project Fido2` for builds.
- Use `dotnet toolchain.cs -- test --project Fido2` for tests.
- Preserve public module API shape unless explicitly approved.
- Exclude unattended FIDO2 tests that require UP, UV, touch, reset timing, or insert/remove coordination.
- Stage only intended Phase 26 files.

## Goal

Identify the remaining high-risk FIDO2 CTAP request surfaces after Phase 6 and add byte-level unit coverage or narrow production cleanup so the outgoing command byte and canonical CBOR payload are explicitly verified without changing public API behavior.

## Criteria

- [x] ISC-1: Phase 26 ISA exists at documented plan path.
- [x] ISC-2: FIDO2 module guidance was read before edits.
- [x] ISC-3: Prior Phase 6 FIDO2 encoding handoff was read.
- [x] ISC-4: Prior Phase 14 transport handoff was read.
- [x] ISC-5: Source audit identifies direct CTAP request construction sites.
- [x] ISC-6: Client PIN request surface is classified covered or tested.
- [x] ISC-7: Credential Management request surface is classified covered or tested.
- [x] ISC-8: Authenticator Config request surface is classified covered or tested.
- [x] ISC-9: Bio Enrollment request surface is classified covered or tested.
- [x] ISC-10: Large Blob request surface is classified covered or tested.
- [x] ISC-11: New tests assert command byte where applicable.
- [x] ISC-12: New tests assert canonical integer-key CBOR order.
- [x] ISC-13: New tests avoid hardware and human interaction.
- [x] ISC-14: Production changes, if any, stay minimal and internal.
- [x] ISC-15: Anti: No public FIDO2 API signature changes are introduced.
- [x] ISC-16: Anti: No command-object framework is introduced.
- [x] ISC-17: Focused Phase 26 tests pass through toolchain.
- [x] ISC-18: FIDO2 unit tests pass through toolchain.
- [x] ISC-19: FIDO2 build passes through toolchain.
- [x] ISC-20: Phase 26 learnings record findings and next phase input.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | file | Read ISA path | Exists with frontmatter | Read |
| ISC-2 | file | Read FIDO2 CLAUDE.md | Relevant guidance loaded | Read |
| ISC-3 | file | Read Phase 6 learning note | Relevant handoff loaded | Read |
| ISC-4 | file | Read Phase 14 learning note | Relevant handoff loaded | Read |
| ISC-5 | search | Search CTAP construction patterns | Sites enumerated | Grep |
| ISC-6 | test/audit | Client PIN path has byte proof | Covered or justified | Read/test |
| ISC-7 | test/audit | Credential Management path has byte proof | Covered or justified | Read/test |
| ISC-8 | test/audit | Config path has byte proof | Covered or justified | Read/test |
| ISC-9 | test/audit | Bio Enrollment path has byte proof | Covered or justified | Read/test |
| ISC-10 | test/audit | Large Blob path has byte proof | Covered or justified | Read/test |
| ISC-11 | test | Test assertions inspect request[0] | At least targeted tests | Read/test |
| ISC-12 | test | Test assertions inspect CBOR keys | At least targeted tests | Read/test |
| ISC-13 | test | Tests are unit-only | No hardware traits | Read/test |
| ISC-14 | diff | Source diff is narrow/internal | No broad refactor | git diff |
| ISC-15 | diff | Public signatures unchanged | No API delta | git diff/search |
| ISC-16 | diff | No command framework added | No new command classes | Glob/Grep |
| ISC-17 | command | Focused test command exits 0 | 0 failures | Bash |
| ISC-18 | command | FIDO2 unit command exits 0 | 0 failures | Bash |
| ISC-19 | command | FIDO2 build exits 0 | 0 errors | Bash |
| ISC-20 | file | Learning note exists | Contains evidence | Read |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| CTAP Surface Audit | Enumerate direct request construction and current test coverage. | ISC-5, ISC-6, ISC-7, ISC-8, ISC-9, ISC-10 | none | true |
| Wire Tests | Add focused byte-level unit tests for uncovered high-risk surfaces. | ISC-11, ISC-12, ISC-13, ISC-17 | CTAP Surface Audit | false |
| Minimal Cleanup | Apply only source changes forced by failing tests. | ISC-14, ISC-15, ISC-16 | Wire Tests | false |
| Verification And Learnings | Run toolchain checks and write handoff note. | ISC-18, ISC-19, ISC-20 | Wire Tests | false |

## Decisions

- 2026-06-08: scoped Phase 26 to byte-level unit coverage and narrow cleanup because unattended hardware would not prove request encoding better than direct request capture.
- 2026-06-08: preserved the public `CredentialManagement(FidoSession, ...)` constructor while adding an internal `IFidoSession` seam so unit tests can capture command-prefixed requests without public API drift.
- 2026-06-08: added direct `Xunit.SkippableFact` to FIDO2 integration tests because `Tests.Shared` private package assets made `SkippableTheory` unavailable during FIDO2 build.

## Verification

- ISC-1: Read — Phase 26 ISA read back with frontmatter and criteria.
- ISC-2: Read — `src/Fido2/CLAUDE.md` loaded before edits.
- ISC-3: Read — Phase 6 FIDO2 encoding learning note loaded before edits.
- ISC-4: Read — Phase 14 FIDO2 transport learning note loaded before edits.
- ISC-5: Grep — CTAP construction audit identified ClientPin, CredentialManagement, Config, BioEnrollment, LargeBlobStorage, and Phase 6 session encoders.
- ISC-6: Test — `ClientPinTests` now asserts ClientPin command byte and canonical key order for retry requests.
- ISC-7: Test — `CredentialManagementWireTests` asserts CredentialManagement command byte and canonical key order for metadata and delete requests.
- ISC-8: Read — existing `AuthenticatorConfigTests` capture command byte, subcommand, protocol, auth param, and auth message.
- ISC-9: Read — existing `FingerprintBioEnrollmentTests` capture sensor-info and enroll request payload bytes; remaining operations classified lower risk for this phase.
- ISC-10: Read — existing `LargeBlobStorageTests` capture LargeBlobs command byte, read/write map shape, protocol, and auth param.
- ISC-11: Read/test — new Client PIN and Credential Management tests assert outgoing request command bytes.
- ISC-12: Read/test — new tests parse payloads with `CborConformanceMode.Ctap2Canonical` and assert ascending integer keys.
- ISC-13: Read/test — new tests are xUnit unit tests with mocked `IFidoSession`; no hardware traits or integration dependencies.
- ISC-14: Diff — production change limited to `CredentialManagement` internal session seam, request prefixing, and request zeroing.
- ISC-15: Diff — public constructor remains `CredentialManagement(FidoSession, IPinUvAuthProtocol, ReadOnlyMemory<byte>)`.
- ISC-16: Glob/Grep — no operation-specific command classes or command framework added.
- ISC-17: Bash — `CredentialManagementWireTests` passed 2/2 and `ClientPinTests` passed 27/27 through toolchain.
- ISC-18: Bash — `dotnet toolchain.cs -- test --project Fido2.UnitTests` passed 399/399.
- ISC-19: Bash — `dotnet toolchain.cs -- build --project Fido2` passed source, integration tests, and unit tests with 0 warnings/errors.
- ISC-20: Read — Phase 26 learning note created and updated with verification evidence.
