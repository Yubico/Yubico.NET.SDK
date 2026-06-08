# Phase 29 SecurityDomain Test And Locality Follow-Up ISA

## Problem

`SecurityDomainSession` intentionally remains a single facade, but several SecurityDomain STORE DATA convenience methods still build meaningful GlobalPlatform TLV payloads without recorder-backed byte assertions. The module also has an integration-test runtime dependency risk already seen in WebAuthn and OATH: `Tests.Shared` uses `Xunit.SkippableFact` privately, while SecurityDomain integration tests do not reference it directly.

## Vision

SecurityDomain should keep GlobalPlatform APDU flow visible while gaining byte-level proof for the highest-value remaining STORE DATA payload paths. Phase 29 should avoid production churn unless tests reveal a concrete issue; the ideal outcome is stronger test evidence and any required test-project dependency fix, not a broad session split.

## Out of Scope

- No operation-specific `PutKeyCommand`, `GetDataCommand`, `StoreDataCommand`, `DeleteKeyCommand`, or `ResetCommand` types.
- No `SecurityDomainSession` partial-class split.
- No public SecurityDomain API changes.
- No Core, SCP, or TLV primitive changes.
- No broad SecurityDomain integration suite run.
- No unattended destructive SecurityDomain lifecycle tests beyond already self-contained reset tests explicitly selected by command.

## Principles

- Use Phase 22 `RecordingSmartCardConnection` for byte-level APDU proof.
- Keep `StoreDataAsync` command construction visible in production.
- Add source-backed tests before considering production locality changes.
- Treat direct `Xunit.SkippableFact` integration-project reference as a test-runtime dependency fix only if the read-only smoke path exposes it or project comparison proves it is missing.
- Preserve SecurityDomain's documented single-facade shape.

## Constraints

- Branch must be `yubikit-consolidation` before edits and verification.
- Required phase inputs: master ISA, SDK house style, module assessment, Phase 28 learning note, SecurityDomain module/test guidance.
- Unit command: `dotnet toolchain.cs -- test --project SecurityDomain --filter "FullyQualifiedName~SecurityDomainSessionTests"`.
- Build command: `dotnet toolchain.cs -- build --project SecurityDomain`.
- Read-only integration smoke command: `dotnet toolchain.cs -- test --integration --project SecurityDomain --filter "Method~GetData_Unauthenticated_Succeeds" --smoke`.
- Stage only intended Phase 29 files.

## Goal

Add recorder-backed SecurityDomain unit tests for STORE DATA payload helpers (`StoreAllowListAsync`, `ClearAllowListAsync`, and `StoreCaIssuerAsync`) and fix only the required integration-test skip dependency if the read-only smoke path cannot run because `Xunit.SkippableFact` is not available at runtime.

## Criteria

- [x] ISC-1: Phase 29 ISA exists before SecurityDomain source/test edits.
- [x] ISC-2: Branch check confirms `yubikit-consolidation` before Phase 29 edits.
- [x] ISC-3: Master ISA, SDK house style, module assessment, Phase 28 learning, and SecurityDomain guidance were read.
- [x] ISC-4: `StoreAllowListAsync` unit test asserts exact STORE DATA APDU header and ordered TLV payload for multiple serials.
- [x] ISC-5: `ClearAllowListAsync` unit test asserts exact empty allow-list STORE DATA payload.
- [x] ISC-6: `StoreCaIssuerAsync` unit test asserts exact CA issuer STORE DATA payload for an SCP11 key reference.
- [x] ISC-7: Existing `StoreDataAsync` byte coverage still passes.
- [x] ISC-8: No production code changes are introduced unless a byte test reveals a concrete defect.
- [x] ISC-9: No forbidden command/executor abstractions or partial-class split are introduced.
- [x] ISC-10: Public SecurityDomain API signatures remain unchanged.
- [x] ISC-11: SecurityDomain integration project has direct `Xunit.SkippableFact` only if required by runtime dependency evidence or parity with existing integration-project pattern.
- [x] ISC-12: Focused SecurityDomain unit tests pass through toolchain.
- [x] ISC-13: SecurityDomain build passes through toolchain.
- [x] ISC-14: Read-only SecurityDomain integration smoke passes or records a concrete skip/failure reason.
- [x] ISC-15: Docs QA passes.
- [x] ISC-16: DevTeam/cross-vendor review completes or an approved waiver is recorded.
- [x] ISC-17: Phase 29 learning note records findings, verification, and Phase 30 input.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | file | Read ISA path | Exists | Read |
| ISC-2 | git | `git status --short --branch` | Shows `yubikit-consolidation` | Bash |
| ISC-3 | file | Required input reads | Context loaded | Read |
| ISC-4 | unit | `StoreAllowListAsync` APDU test | Byte-exact APDU | Bash |
| ISC-5 | unit | `ClearAllowListAsync` APDU test | Byte-exact APDU | Bash |
| ISC-6 | unit | `StoreCaIssuerAsync` APDU test | Byte-exact APDU | Bash |
| ISC-7 | unit | Existing `StoreDataAsync` test | Still passes | Bash |
| ISC-8 | diff | Production diff absent or justified | No unjustified production diff | git diff |
| ISC-9 | grep/diff | Forbidden abstraction search | No forbidden new types | Grep/git diff |
| ISC-10 | diff | Public API unchanged | No signature delta | git diff |
| ISC-11 | integration/config | Skip dependency fixed if needed | Smoke can resolve runtime skip assembly | Bash |
| ISC-12 | command | Focused unit tests exit 0 | 0 failures | Bash |
| ISC-13 | command | SecurityDomain build exits 0 | 0 errors | Bash |
| ISC-14 | command | Read-only smoke exits 0 or reason recorded | 0 failures or documented blocker | Bash |
| ISC-15 | command | Docs QA exits 0 | 0 failures | Bash |
| ISC-16 | review | DevTeam/cross-vendor review | Pass or approved waiver | DevTeam |
| ISC-17 | file | Learning note exists | Contains evidence | Read |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Store Data Wire Tests | Add recorder-backed APDU assertions for allow-list, clear allow-list, and CA issuer helpers. | ISC-4, ISC-5, ISC-6, ISC-7 | none | false |
| Integration Skip Dependency | Add direct `Xunit.SkippableFact` reference if required by SecurityDomain integration runtime behavior. | ISC-11, ISC-14 | baseline smoke | false |
| Verification And Learnings | Run focused checks, review, and record Phase 30 handoff. | ISC-12, ISC-13, ISC-14, ISC-15, ISC-16, ISC-17 | implementation | false |

## Decisions

- 2026-06-08: Phase 29 is test-first because source audit found no justified production split that would improve maintainability more than byte-level coverage.
- 2026-06-08: STORE DATA helpers are the target because they build meaningful TLV payloads over an already-tested generic `StoreDataAsync` command path.
- 2026-06-08: Whole SecurityDomain integration suite remains out of scope because most tests reset, import, rotate, delete, or generate persistent SecurityDomain state.
- 2026-06-08: Direct `Xunit.SkippableFact` is required for SecurityDomain integration tests by parity with OATH/WebAuthn/FIDO2/OpenPGP/PIV and because `Tests.Shared` keeps the package private.

## Verification

- Format command: `dotnet format src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/Yubico.YubiKit.SecurityDomain.UnitTests.csproj --verify-no-changes`.
- Format result: passed after scoped formatting of touched SecurityDomain unit-test files.
- Diff whitespace command: `git diff --check`.
- Diff whitespace result: passed; only a CRLF conversion warning was reported for the integration test project file.
- Focused unit command: `dotnet toolchain.cs -- test --project SecurityDomain --filter "FullyQualifiedName~SecurityDomainSessionTests"`.
- Focused unit result: passed 25/25.
- Full SecurityDomain unit command: `dotnet toolchain.cs -- test --project SecurityDomain`.
- Full SecurityDomain unit result: passed 31/31.
- Build command: `dotnet toolchain.cs -- build --project SecurityDomain`.
- Build result: SecurityDomain source, integration tests, and unit tests built with 0 warnings and 0 errors.
- Docs QA command: `dotnet toolchain.cs -- docs-qa`.
- Docs QA result: passed, 54 active documentation files validated.
- Read-only integration smoke command: `dotnet toolchain.cs -- test --integration --project SecurityDomain --filter "FullyQualifiedName~SecurityDomainSession_Scp03Tests.GetData_Unauthenticated_Succeeds" --smoke`.
- Read-only integration smoke result: passed 1/1 on authorized serial `103`, firmware `5.8.0`.
- DevTeam review route: `openai/gpt-5.5` primary routed reviewer to `google-vertex-anthropic/claude-opus-4-8@default`.
- DevTeam review result: `PASS`; low notes only, no required changes.
