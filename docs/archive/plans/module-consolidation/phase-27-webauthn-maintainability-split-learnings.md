# Phase 27 Learnings: WebAuthn Maintainability Split

Use this note as the handoff record for Phase 27 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`.
- Scope: reduce duplicated WebAuthn ceremony shell code without changing public APIs or FIDO2 delegation.
- Phase ISA: `docs/plans/module-consolidation/phase-27-webauthn-maintainability-split-ISA.md`.
- Source changed: `WebAuthnClient` now shares status-stream producer mechanics through `RunStatusStreamAsync<TResult>` and automatic PIN/UV stream draining through `DrainInteractiveStreamAsync<TResult>`.
- Tests/config changed: WebAuthn integration tests now reference `Xunit.SkippableFact` directly, matching the OpenPGP/FIDO2 package-private skip dependency fix.
- Integration requiring UP, UV/PIN, touch, reset, destructive cleanup, or insert/remove: not run.

## Source Audit

- `MakeCredentialStreamAsync` and `GetAssertionStreamAsync` had mechanically identical `StatusChannel`, `Task.Run`, `Processing`, `Finished<T>`, `Failed`, cancellation mapping, completion, and iterator-disposal flows.
- `MakeCredentialAsync` and `GetAssertionAsync` byte overloads had identical stream-draining logic with missing-PIN immediate `NotAllowed` behavior and automatic `useUv: false`.
- `MakeCredentialAsync` and `GetAssertionAsync` string overloads had identical PIN UTF-8 rental/zeroing and stream-draining logic, but intentionally drained the stream after missing-PIN cancellation so the producer emits its failed terminal status.
- Backend request mapping was already the correct seam; `IWebAuthnBackend`, `BuildMakeCredentialRequest`, and `BuildGetAssertionRequest` stayed in `WebAuthnClient`.

## What Changed

- Added `RunStatusStreamAsync<TResult>` to own the shared status producer flow for registration and authentication.
- Added `DrainInteractiveStreamAsync<TResult>` to own automatic PIN/UV status handling for both ceremony result types.
- Added `MissingPinBehavior` so byte overloads keep immediate `PIN required but not provided` behavior while string overloads keep drain-after-cancel behavior.
- Added `RentUtf8Pin` and `ZeroAndDispose` so string overloads share PIN buffer rental and zeroing.
- Added a status-stream unit test covering the byte-overload missing-PIN branch after DevTeam noted it was not directly pinned.
- Added direct `Xunit.SkippableFact` reference to `Yubico.YubiKit.WebAuthn.IntegrationTests.csproj`.

## Why This Shape

- The duplication was in ceremony shell mechanics, not CTAP request mapping, so extracting the shell improved readability without hiding FIDO2 behavior.
- A generic stream runner keeps status ordering and cancellation semantics in one place while leaving each ceremony core explicit.
- The missing-PIN enum prevents accidental behavior merge between byte overloads and string overloads.
- Keeping helpers inside `WebAuthnClient` avoids a broad file split and preserves locality for this phase.

## Verification Evidence

- Branch check: `git status --short --branch` showed `## yubikit-consolidation...origin/yubikit-consolidation [ahead 6]` before source edits.
- Initial WebAuthn build command: `dotnet toolchain.cs -- build --project WebAuthn`.
- Initial WebAuthn build result: source built cleanly, then integration test compile failed because `SkippableTheoryAttribute` was unavailable after `Tests.Shared` made skip package assets private.
- Focused WebAuthn client command: `dotnet toolchain.cs -- test --project WebAuthn.UnitTests --filter "FullyQualifiedName~WebAuthnClient"`.
- Focused WebAuthn client result: passed 31/31.
- Focused status-stream command: `dotnet toolchain.cs -- test --project WebAuthn.UnitTests --filter "FullyQualifiedName~WebAuthnStatusStreamTests"`.
- Focused status-stream result: passed 7/7 after adding byte-overload missing-PIN coverage.
- WebAuthn build command after package fix: `dotnet toolchain.cs -- build --project WebAuthn`.
- WebAuthn build result after package fix: passed source, integration tests, and unit tests with 0 warnings and 0 errors.
- WebAuthn unit command: `dotnet toolchain.cs -- test --project WebAuthn.UnitTests`.
- WebAuthn unit result: passed 107/107 after the added status-stream test.
- WebAuthn integration smoke command: `dotnet toolchain.cs -- test --integration --project WebAuthn --smoke --filter "FullyQualifiedName~WebAuthnClientFactoryTests"`.
- WebAuthn integration smoke result: passed 1/1 agent-runnable SmartCard factory test on serial `103`, firmware `5.8.0`; the UP ceremony test in the same class was excluded.
- Format command: `dotnet format --verify-no-changes --include src/WebAuthn/src/Client/WebAuthnClient.cs src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/Yubico.YubiKit.WebAuthn.IntegrationTests.csproj`.
- Format result: passed after normalizing mixed line endings and final-newline style with scoped `dotnet format`.
- DevTeam review: Vertex Opus 4.8 returned `verdict: pass`; low/info notes only. The byte-overload missing-PIN missed-test note was addressed in this phase; remaining empty-string PIN and broader GetAssertion missing-PIN parity tests are deferred.

## What Did Not Work

- Initial full WebAuthn build failed on missing `SkippableTheoryAttribute` in integration tests. The direct `Xunit.SkippableFact` package reference fixed it.
- `apply_patch` introduced mixed line endings into a CRLF-normalized file region; scoped `dotnet format` normalized it.
- Combined focused filter `FullyQualifiedName~WebAuthnStatusStreamTests|FullyQualifiedName~WebAuthnClient` did not match under this xUnit v3 toolchain path; separate `FullyQualifiedName~...` filters worked.

## Reusable Patterns

- When stream-based APIs duplicate status-channel producer mechanics, a generic status runner is acceptable if it does not hide protocol request mapping.
- If two overload families intentionally differ in missing-input behavior, encode that difference explicitly instead of relying on comments in duplicated loops.
- WebAuthn integration projects need direct skip package references when `Tests.Shared` keeps its packages private.

## Deferred Candidates

- Empty-string PIN policy: DevTeam noted `pin: ""` is still treated as a present empty PIN, matching previous behavior. Defer any behavior change because this phase was a maintainability split.
- Authentication missing-PIN parity: add direct `GetAssertionAsync` missing-PIN tests if WebAuthn gets another focused test pass.
- Terminal status ordering guard: add a direct Processing-first/terminal-last regression test if the status stream runner changes again.

## Next Phase Inputs

- Required reading before next phase: this learning note.
- Phase 28 should proceed to OATH locality polish.
- Preserve Core configured chained-response behavior for OATH `INS_SEND_REMAINING = 0xA5`.
- Use the WebAuthn package-fix pattern if another integration project fails on `SkippableTheory` after `Tests.Shared` private package assets.
- Do not expand WebAuthn public factory redesign during OATH work; WebAuthn construction-story work remains deferred unless separately approved.

## Compact Summary

- Goal: reduce WebAuthn ceremony shell duplication.
- Main fix: shared status runner and interactive drain helper.
- Public API: unchanged.
- FIDO2 delegation: still explicit through existing backend/request seams.
- Verification: focused client tests, full unit tests, build, integration smoke, and format passed.
