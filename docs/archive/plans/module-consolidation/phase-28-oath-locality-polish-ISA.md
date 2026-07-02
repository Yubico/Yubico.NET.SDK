# Phase 28 OATH Locality Polish ISA

## Problem

`OathSession` is intentionally flat, but `PutCredentialAsync` currently combines credential ID derivation, secret processing, type-byte construction, optional touch property encoding, optional HOTP counter encoding, total TLV length calculation, APDU payload assembly, transmit, and sensitive cleanup in one method. This is the densest OATH inline APDU/TLV path and has little byte-level unit coverage beyond chained-response behavior.

## Vision

OATH should keep APDU flow visible while making the high-risk credential-put payload easier to inspect and test. The session method should show validation, secret processing, local payload encoding, APDU transmit, and cleanup without hiding the OATH wire format behind an operation-specific command object.

## Out of Scope

- No broad OATH rewrite.
- No operation-specific command classes or executors.
- No Core response-chaining changes.
- No public OATH API changes.
- No CLI/example changes.
- No destructive or persistent-state integration tests beyond existing self-contained smoke/lifecycle tests.

## Principles

- Preserve Core configured chained-response behavior for `INS_SEND_REMAINING = 0xA5`.
- Extract only pure local encode helpers where they make `OathSession` easier to read.
- Keep APDU instruction bytes and payload shape visible in source and tests.
- Preserve sensitive secret/key payload zeroing after transmit and on failures.
- Prefer `RecordingSmartCardConnection` byte assertions over hardware mutation for wire-shape proof.

## Constraints

- Branch must be `yubikit-consolidation` before edits and verification.
- Use `dotnet toolchain.cs -- test --project Oath.UnitTests` for unit tests.
- Use `dotnet toolchain.cs -- build --project Oath` for build.
- Use `dotnet toolchain.cs -- test --integration --project Oath.IntegrationTests --smoke --filter "FullyQualifiedName~OathSessionTests.OathSession_Create_ReadsSelectMetadataWithoutReset"` for read-only integration smoke.
- Stage only intended Phase 28 files.

## Goal

Refactor only the OATH credential-put payload assembly into a local pure helper and add focused byte-level unit tests that prove representative TOTP and HOTP/touch/counter APDU payloads, without changing public API behavior or Core chained-response configuration.

## Criteria

- [x] ISC-1: Phase 28 ISA exists before source edits.
- [x] ISC-2: Branch check confirms `yubikit-consolidation` before edits.
- [x] ISC-3: OATH module and test guidance were read before edits.
- [x] ISC-4: Prior Phase 27 learning note was read before edits.
- [x] ISC-5: Prior OATH Phase 3 chained-response handoff was read before edits.
- [x] ISC-6: `PutCredentialAsync` payload assembly is separated into a small local helper.
- [x] ISC-7: APDU transmit remains visible in `PutCredentialAsync`.
- [x] ISC-8: Sensitive `secret`, `keyValue`, and encoded payload buffers are zeroed in all paths.
- [x] ISC-9: Unit tests assert TOTP PUT APDU header and ordered TLV bytes.
- [x] ISC-10: Unit tests assert HOTP counter and require-touch PUT payload bytes.
- [x] ISC-11: Existing OATH chained-response `0xA5` unit coverage still passes.
- [x] ISC-12: No public OATH API signature changes are introduced.
- [x] ISC-13: No operation-specific command classes or command-like executors are introduced.
- [x] ISC-14: Focused OATH unit tests pass through toolchain.
- [x] ISC-15: OATH build passes through toolchain.
- [x] ISC-16: Read-only OATH integration smoke passes or has a recorded skip reason.
- [x] ISC-17: DevTeam/cross-vendor review completes or an approved waiver is recorded.
- [x] ISC-18: Phase 28 learning note records findings, verification, and Phase 29 input.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | file | Read ISA path | Exists | Read |
| ISC-2 | git | `git status --short --branch` | Shows `yubikit-consolidation` | Bash |
| ISC-3 | file | Read OATH `CLAUDE.md` and test guidance | Relevant guidance loaded | Read |
| ISC-4 | file | Read Phase 27 learning note | Relevant handoff loaded | Read |
| ISC-5 | file | Read Phase 3 OATH handoff | Chained-response constraint loaded | Read |
| ISC-6 | diff | Helper extraction is local and small | Source confirms | git diff |
| ISC-7 | diff | `ApduCommand` and transmit remain in session method | Source confirms | git diff |
| ISC-8 | diff/test | Sensitive arrays zeroed in finally | Source confirms | Read |
| ISC-9 | unit | TOTP PUT wire test | Header/TLV bytes match | Bash |
| ISC-10 | unit | HOTP/touch/counter PUT wire test | Header/TLV bytes match | Bash |
| ISC-11 | unit | Existing chained-response tests | Still pass | Bash |
| ISC-12 | diff | Public API unchanged | No API delta | git diff |
| ISC-13 | grep | No forbidden command/executor objects | No matches | Grep |
| ISC-14 | command | Focused OATH unit tests exit 0 | 0 failures | Bash |
| ISC-15 | command | OATH build exits 0 | 0 errors | Bash |
| ISC-16 | command | Read-only OATH smoke exits 0 or skip recorded | 0 failures or documented skip | Bash |
| ISC-17 | review | DevTeam/cross-vendor review | Pass or approved waiver | DevTeam |
| ISC-18 | file | Learning note exists | Contains evidence | Read |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Put Payload Helper | Move credential PUT TLV payload assembly into one local pure helper. | ISC-6, ISC-7, ISC-8, ISC-12, ISC-13 | none | false |
| PUT Wire Tests | Add recorder-backed byte assertions for representative TOTP and HOTP/touch/counter payloads. | ISC-9, ISC-10, ISC-11 | Put Payload Helper | false |
| Verification And Learnings | Run focused checks, review, and record handoff. | ISC-14, ISC-15, ISC-16, ISC-17, ISC-18 | implementation | false |

## Decisions

- 2026-06-08: Phase 28 targets `PutCredentialAsync` only because it is the densest OATH APDU/TLV assembly path and can be improved without a broad rewrite.
- 2026-06-08: Core configured chained-response behavior is explicitly preserved; no Core changes are in scope.
- 2026-06-08: OATH integration tests now carry a direct `Xunit.SkippableFact` reference because `Tests.Shared` references it with private assets and runtime `Xunit.SkipException` usage does not flow transitively.

## Verification

- Branch check: work continued on `yubikit-consolidation`.
- Focused OATH session command: `dotnet toolchain.cs -- test --project Oath.UnitTests --filter "FullyQualifiedName~OathSessionTests"`.
- Focused OATH session result: passed 13/13 after preserving expected secret bytes before `PutCredentialAsync` zeroes the caller-owned secret buffer.
- Full OATH unit command: `dotnet toolchain.cs -- test --project Oath.UnitTests`.
- Full OATH unit result: passed 83/83.
- OATH build command: `dotnet toolchain.cs -- build --project Oath`.
- OATH build result: source, integration tests, and unit tests built with 0 warnings and 0 errors.
- Read-only OATH integration smoke command: `dotnet toolchain.cs -- test --integration --project Oath.IntegrationTests --smoke --filter "FullyQualifiedName~OathSessionTests.OathSession_Create_ReadsSelectMetadataWithoutReset"`.
- Read-only OATH integration smoke result: passed 1/1 on authorized serial `103`, firmware `5.8.0`.
- Docs QA command: `dotnet toolchain.cs -- docs-qa`.
- Docs QA result: passed, 54 active documentation files validated.
- Format evidence: `dotnet format src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/Yubico.YubiKit.Oath.UnitTests.csproj --verify-no-changes` passed after scoped formatting; full-repo `dotnet format --verify-no-changes` still fails on pre-existing non-OATH-wide files and warnings.
- Additional format evidence: scoped OATH source/integration format verification reports pre-existing final-newline/line-ending policy issues in untouched OATH files; Phase 28 kept the committed diff scoped to intended files.
- DevTeam review: Vertex Opus 4.8 returned `PASS`; low notes only, no correctness/security/blocking findings.
