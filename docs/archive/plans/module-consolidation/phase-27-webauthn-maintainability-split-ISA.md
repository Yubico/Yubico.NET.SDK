# Phase 27 WebAuthn Maintainability Split ISA

## Problem

`WebAuthnClient` is large and mixes public convenience overloads, status-stream producer mechanics, ceremony validation, PIN/UV token handling, FIDO2 request mapping, CTAP error mapping, and response building. The highest-confidence local duplication is not the FIDO2 delegation itself; it is repeated stream-draining and status-channel orchestration around both registration and authentication ceremonies.

## Vision

WebAuthn should keep FIDO2 delegation visible while removing duplicated ceremony shell code. A maintainer should be able to read registration and authentication paths as two ceremonies that share one status-stream runner and one convenience stream drain pattern, without introducing command objects, new public APIs, or hidden CTAP behavior.

## Out of Scope

- No public `WebAuthnClient` constructor or method signature changes.
- No WebAuthn public factory redesign.
- No FIDO2 protocol behavior changes.
- No previewSign hardware behavior changes.
- No User Presence, UV/PIN, reset, insert/remove, or destructive integration runs.
- No broad `WebAuthnClient` file split unless a focused compile/test failure proves it necessary.

## Principles

- Preserve WebAuthn-to-FIDO2 delegation at the backend boundary.
- Extract only ceremony shell duplication that is mechanically identical.
- Keep helper names about behavior, not architecture.
- Preserve typed WebAuthn error mapping and cancellation semantics exactly.
- Keep sensitive PIN buffer ownership and zeroing unchanged or clearer.

## Constraints

- Branch must be `yubikit-consolidation` before edits and verification.
- Use `dotnet toolchain.cs -- test --project WebAuthn` for tests.
- Use `dotnet toolchain.cs -- build --project WebAuthn` for build.
- Use `dotnet toolchain.cs -- test --integration --project WebAuthn --smoke --filter "FullyQualifiedName~WebAuthnClientFactoryTests"` for agent-runnable integration smoke.
- Stage only intended Phase 27 files.

## Goal

Refactor `WebAuthnClient` just enough to remove duplicated registration/authentication status-stream producer logic and duplicated automatic PIN/UV stream-draining logic, while preserving public API shape, FIDO2 delegation, status ordering, cancellation behavior, and PIN buffer cleanup.

## Criteria

- [x] ISC-1: Phase 27 ISA exists before source edits.
- [x] ISC-2: Branch check confirms `yubikit-consolidation` before edits.
- [x] ISC-3: WebAuthn module guidance was read before edits.
- [x] ISC-4: Prior Phase 26 learning note was read before edits.
- [x] ISC-5: Source audit identifies the exact duplicated ceremony shell logic.
- [x] ISC-6: Status-stream producer duplication is reduced without changing emitted status order.
- [x] ISC-7: Automatic PIN/UV stream-drain duplication is reduced without changing null-PIN, UV, failure, or terminal-state behavior.
- [x] ISC-8: Public WebAuthn APIs remain unchanged.
- [x] ISC-9: FIDO2 delegation remains explicit through `IWebAuthnBackend` and existing request builders.
- [x] ISC-10: No operation-specific command classes or ceremony executor objects are introduced.
- [x] ISC-11: PIN string and PIN bytes remain zeroed/owned as before.
- [x] ISC-12: Focused WebAuthn client tests pass through toolchain.
- [x] ISC-13: WebAuthn unit tests pass through toolchain.
- [x] ISC-14: WebAuthn build passes through toolchain.
- [x] ISC-15: Agent-runnable WebAuthn integration smoke either passes or has a recorded hardware/tooling skip reason.
- [x] ISC-16: DevTeam/cross-vendor review completes or a recorded waiver is approved.
- [x] ISC-17: Phase 27 learning note records findings, verification, and Phase 28 input.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | file | Read ISA path | Exists | Read |
| ISC-2 | git | `git status --short --branch` | Shows `yubikit-consolidation` | Bash |
| ISC-3 | file | Read WebAuthn `CLAUDE.md` | Relevant guidance loaded | Read |
| ISC-4 | file | Read Phase 26 learning note | Relevant handoff loaded | Read |
| ISC-5 | audit | Inspect `WebAuthnClient` stream/convenience paths | Duplication identified | Read |
| ISC-6 | diff/test | Stream producer helper preserves status/failure behavior | Tests pass | Diff/test |
| ISC-7 | diff/test | Convenience stream drain helper preserves PIN/UV behavior | Tests pass | Diff/test |
| ISC-8 | diff | Public signatures unchanged | No API delta | git diff |
| ISC-9 | diff | Backend/request mapping remains visible | No hidden FIDO2 delegation | git diff |
| ISC-10 | glob/grep | No new command/executor objects | No forbidden types | Glob/Grep |
| ISC-11 | diff/test | PIN owner cleanup remains in finally | Source/tests confirm | Read/test |
| ISC-12 | command | Focused client tests exit 0 | 0 failures | Bash |
| ISC-13 | command | WebAuthn unit tests exit 0 | 0 failures | Bash |
| ISC-14 | command | WebAuthn build exits 0 | 0 errors | Bash |
| ISC-15 | command | Factory smoke exits 0 or skip recorded | 0 failures or documented skip | Bash |
| ISC-16 | review | DevTeam/cross-vendor review | Pass or approved waiver | DevTeam |
| ISC-17 | file | Learning note exists | Contains evidence | Read |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Ceremony Shell Audit | Confirm duplicated WebAuthn stream and convenience wrapper mechanics. | ISC-5 | none | true |
| Stream Runner Refactor | Extract one internal status-stream runner for registration/authentication result types. | ISC-6, ISC-8, ISC-9, ISC-10 | Ceremony Shell Audit | false |
| Convenience Drain Refactor | Extract one internal automatic PIN/UV stream-drain helper for both ceremonies. | ISC-7, ISC-11 | Ceremony Shell Audit | false |
| Verification And Learnings | Run focused checks, review, and record handoff. | ISC-12, ISC-13, ISC-14, ISC-15, ISC-16, ISC-17 | Refactors | false |

## Decisions

- 2026-06-08: Phase 27 targets mechanically duplicated WebAuthn ceremony shell code, not backend/request mapping, because the backend is already the correct visible FIDO2 delegation seam.
- 2026-06-08: public factory redesign is deferred; current construction tests and `CreateWebAuthnClientAsync` already cover the immediate construction-story risk without requiring API churn.
- 2026-06-08: added direct `Xunit.SkippableFact` to WebAuthn integration tests because `Tests.Shared` private package assets made `SkippableTheory` unavailable during WebAuthn build.

## Verification

- ISC-1: Read — Phase 27 ISA was created before source edits.
- ISC-2: Bash — `git status --short --branch` showed `## yubikit-consolidation...origin/yubikit-consolidation [ahead 6]` before source edits.
- ISC-3: Read — `src/WebAuthn/CLAUDE.md` loaded before source edits.
- ISC-4: Read — Phase 26 learning note loaded before source edits.
- ISC-5: Read — `WebAuthnClient` duplicated status-channel producer and automatic PIN/UV stream-drain logic across registration and authentication.
- ISC-6: Diff/test — `RunStatusStreamAsync<TResult>` now owns the shared processing, success, failure, cancellation, completion, and iterator-disposal flow; focused client tests passed 31/31.
- ISC-7: Diff/test — `DrainInteractiveStreamAsync<TResult>` now owns automatic PIN/UV handling for byte and string overloads while preserving the different missing-PIN behaviors; focused client tests passed 31/31.
- ISC-8: Diff — no public `WebAuthnClient` signatures changed.
- ISC-9: Diff — `IWebAuthnBackend`, `BuildMakeCredentialRequest`, and `BuildGetAssertionRequest` remain the visible FIDO2 delegation/mapping seams.
- ISC-10: Grep — no `*Command`, executor, or ceremony command object added under `src/WebAuthn/src`.
- ISC-11: Diff/test — PIN string overloads still rent UTF-8 bytes and zero/dispose the owner in `finally`; PIN byte overload caller ownership remains unchanged.
- ISC-12: Bash — `dotnet toolchain.cs -- test --project WebAuthn.UnitTests --filter "FullyQualifiedName~WebAuthnClient"` passed 31/31.
- ISC-12 addendum: Bash — `dotnet toolchain.cs -- test --project WebAuthn.UnitTests --filter "FullyQualifiedName~WebAuthnStatusStreamTests"` passed 7/7 after adding byte-overload missing-PIN coverage. Combined `FullyQualifiedName~WebAuthnStatusStreamTests|FullyQualifiedName~WebAuthnClient` did not match under the xUnit v3 toolchain filter path.
- ISC-13: Bash — `dotnet toolchain.cs -- test --project WebAuthn.UnitTests` passed 107/107 after the added status-stream test.
- ISC-14: Bash — initial `dotnet toolchain.cs -- build --project WebAuthn` built source but failed integration compile on missing `SkippableTheory`; after adding direct `Xunit.SkippableFact`, the command passed source, integration tests, and unit tests with 0 warnings/errors.
- ISC-15: Bash — `dotnet toolchain.cs -- test --integration --project WebAuthn --smoke --filter "FullyQualifiedName~WebAuthnClientFactoryTests"` passed the agent-runnable SmartCard factory smoke 1/1 on serial `103`, firmware `5.8.0`; RequiresUserPresence test in the same class was excluded by `--smoke`.
- ISC-16: DevTeam — Vertex Opus 4.8 reviewer returned `verdict: pass`; low/info findings only. A focused byte-overload missing-PIN test was added in response to one missed-test note.
- ISC-17: Read — Phase 27 learning note created with findings, verification, and Phase 28 input.
