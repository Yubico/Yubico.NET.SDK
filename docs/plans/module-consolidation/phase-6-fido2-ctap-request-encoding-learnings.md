# Phase 6 Learnings: FIDO2 CTAP Request Encoding

Use this note as the handoff record for Phase 6 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Scope: move `FidoSession` MakeCredential/GetAssertion request construction into one shallow pure encoding helper while preserving visible CTAP key order and session-level transmit flow
- Criteria satisfied: no public API changes, no operation-specific command classes, no WebAuthn/PIN/config/credential-management refactor, canonical `CtapRequestBuilder` path used for MakeCredential and GetAssertion
- Files changed: FIDO2 session source, CTAP request builder, one module-local encoder helper, unit tests, FIDO2 module docs, this learning note
- Result: format, build, unit tests, read-only GetInfo integration, and staged whitespace verification passed
- Cato result: initial audit found incomplete intermediate-buffer zeroing; follow-up code fixed the finding, but Vertex Cato reauth blocked follow-up review
- User Presence result: full tagged run attempted with human approval but failed/inconclusive due repeated `OperationDenied` UP timeouts
- Commit: pending at note update time; next action is staged diff review and commit if accepted with the UP caveat recorded
- `/Ping` sent after successful phase: queued after commit and compact summary

## Hardware Target

- Device: YubiKey 5.8 beta
- Serial: `103`
- Firmware evidence: Management preflight and integration infrastructure reported firmware `5.8.0`
- Human approval: full FIDO2 `RequiresUserPresence` integration run approved for this phase

## Integration Lifecycle

- Branch check command/result: `git status --short --branch` showed `## yubikit-consolidation`
- Baseline build command/result: `dotnet toolchain.cs -- build --project Fido2` passed with 0 warnings and 0 errors
- Baseline unit command/result: `dotnet toolchain.cs -- test --project Fido2` passed, 378/378
- Management preflight command/result: `dotnet toolchain.cs -- test --integration --project Management.IntegrationTests --filter "FullyQualifiedName~CreateManagementSession_WithSmartCardConnection_ReturnsValidSession"` passed and reported serial `103`, firmware `5.8.0`, HID FIDO authorized
- Baseline read-only integration command/result: `dotnet toolchain.cs -- test --integration --project Fido2.IntegrationTests --smoke --filter "FullyQualifiedName~FidoGetInfoTests"` passed, 8/8 on HidFido serial `103`
- Red test command/result: focused FIDO2 unit run failed for missing `CtapRequestBuilder.WithValue` and missing `FidoSessionRequestEncoding` after test syntax corrections
- Focused green wrinkle: the xUnit v3/toolchain focused filter injected `--minimum-expected-tests 0`, so the full FIDO2 unit suite was used as the green check
- Final format command/result: `dotnet format --verify-no-changes --include ...` passed after scoped formatting
- Final build command/result: `dotnet toolchain.cs -- build --project Fido2` passed with 0 warnings and 0 errors
- Final unit command/result: `dotnet toolchain.cs -- test --project Fido2` passed, 383/383
- Final read-only integration command/result: `dotnet toolchain.cs -- test --integration --project Fido2.IntegrationTests --smoke --filter "FullyQualifiedName~FidoGetInfoTests"` passed, 8/8 on HidFido serial `103`
- Cato-fix verification command/result: format passed, FIDO2 build passed, FIDO2 unit passed 383/383, read-only GetInfo integration passed 8/8
- Human-coordinated User Presence command/result: `dotnet toolchain.cs -- test --integration --project Fido2.IntegrationTests --filter "Category=RequiresUserPresence"` discovered 50 tests, passed 17, failed 30, skipped 3; failures were predominantly `OperationDenied` after 28-31 seconds
- Focused User Presence rerun command/result: `dotnet toolchain.cs -- test --integration --project Fido2.IntegrationTests --filter "FullyQualifiedName~FidoMakeCredentialTests.MakeCredential_NonResidentKey_ReturnsValidAttestation"` failed with `OperationDenied` after 31 seconds
- Persistent state changed: possible, because full FIDO2 User Presence integration tests were attempted; most mutating operations denied before completion

## What Worked

- `FidoSession` became easier to read because MakeCredential/GetAssertion request construction moved out of the public session facade while transmit, logging, response decode, and API flow stayed visible.
- `FidoSessionRequestEncoding` stayed a pure module-local helper rather than a command object, service, executor, or operation class.
- `CtapRequestBuilder.WithValue` lets sensitive byte spans and complex CBOR values be written directly by the caller without the extra `WithBytes` copy.
- Unit tests now assert CTAP command byte and canonical request-map shape for minimal/full MakeCredential and GetAssertion requests.
- Final request buffers for MakeCredential/GetAssertion are zeroed after backend send.
- `CtapRequestBuilder.Build()` now zeroes the intermediate CBOR array returned from `CborWriter.Encode()` after copying into the command-prefixed request buffer.

## What Did Not Work

- Cato follow-up could not be completed after the fix because the required Vertex route returned `invalid_grant` / `invalid_rapt` twice.
- Full FIDO2 User Presence integration was not a useful unattended gate; most user-presence operations failed with `OperationDenied` around the 30-second timeout.
- Focused MakeCredential User Presence rerun also failed with `OperationDenied`, so this gate remains inconclusive rather than passed.
- The toolchain focused-filter path for xUnit v3 unit tests produced an invalid `--minimum-expected-tests 0`; full project unit tests are the safer focused-verification substitute until the runner is fixed.

## House Style Updates

- Existing house-style rule confirmed: keep public session methods as the visible protocol lifecycle owner; pure helpers can own local encoding mechanics.
- Existing sensitive-data rule confirmed: zero encoded payloads and intermediate buffers in `finally` where practical.
- FIDO2 module guidance now records that `.WithValue(key, writer => ...)` is preferred for complex values or sensitive byte spans that should not go through `WithBytes` copies.
- Residual limitation: `CborWriter` internal buffers are not directly exposed for zeroing; this phase zeroes the returned intermediate array and final request buffer.

## Reusable Patterns

- Pattern: pure session request encoder plus `CtapRequestBuilder.WithValue` for visible CTAP request construction.
- Generalization class: session methods with dense local CBOR request-building where moving pure encoding out improves readability without hiding send/decode behavior.
- Where it applies: FIDO2 MakeCredential/GetAssertion style methods with integer-key CTAP maps and operation-local option maps.
- Where it should not apply: transport backends, PIN/UV protocol state, credential-management subcommands, extension builders, or code where an operation-specific command object would obscure protocol flow.
- Example files: `src/Fido2/src/FidoSession.cs`, `src/Fido2/src/Cbor/FidoSessionRequestEncoding.cs`, `src/Fido2/src/Cbor/CtapRequestBuilder.cs`

## Core / Shared Promotion Candidates

- Candidate: runner support for xUnit v3 focused filters without injecting `--minimum-expected-tests 0`
- Evidence: focused FIDO2 unit filter failed on runner arguments even after implementation compiled and full unit suite passed
- Risk: changing test runner behavior could affect multiple modules
- Decision: defer to a dedicated toolchain/test-runner phase
- Revisit trigger: a second xUnit v3 module needs focused TDD verification and hits the same runner issue

## Verification Evidence

- Unrelated worktree changes present: yes, two untracked Core YubiKey note files remained unstaged
- Format verification: passed
- Build verification: passed, 0 warnings/errors
- Unit verification: passed, 383/383
- Read-only integration verification: passed, 8/8 on HidFido serial `103`
- Staged whitespace check: `git diff --cached --check` passed with no output
- Cato route: OpenAI primary used Vertex Opus 4.8 via `google-vertex-anthropic/claude-opus-4-8@default`
- Cato initial result: `verdict: concerns`, medium criticality; finding was incomplete zeroing of `CtapRequestBuilder.Build()` intermediate `cbor` array
- Cato fix: `CtapRequestBuilder.Build()` now zeroes `cbor` in `finally`
- Cato follow-up result: unavailable due Vertex auth `invalid_grant` / `invalid_rapt` on both `/tmp/opencode/cato-phase6-fido2-followup.jsonl` and `/tmp/opencode/cato-phase6-fido2-followup-retry.jsonl`
- User Presence gate: attempted, not passed; full tagged run failed 30/50 and focused MakeCredential rerun failed with `OperationDenied`

## Deferred Future Improvement Candidates

- Title: FIDO2 User Presence test coordination harness
- Source phase: Phase 6 full tagged `RequiresUserPresence` attempt
- Rationale: dozens of UP tests require reliable human timing or an explicit prompt/pace mechanism; a full unattended category run wastes time and produces timeout noise
- Why deferred: this phase targeted request encoding, not integration-runner UX
- Likely owning area: FIDO2 integration test infrastructure / `toolchain.cs`
- Needs human approval/hardware/Cato: yes, hardware coordination and likely review before changing test traits or runner pacing

- Title: xUnit v3 focused-filter runner fix
- Source phase: Phase 6 TDD red/green checks
- Rationale: focused filter injected `--minimum-expected-tests 0`, which xUnit v3 rejects
- Why deferred: runner change is cross-cutting and not required for FIDO2 request encoding
- Likely owning area: `toolchain.cs`
- Needs human approval/hardware/Cato: no hardware expected; review needed for runner behavior

## Abort / Split Assessment

- Wrong branch detected: no
- Phase exceeded approved scope: no
- Public API change required: no
- Helper depth concern found: no blocking concern
- Protocol flow became harder to inspect: no
- Verification failed twice for different root causes: partially; Cato follow-up failed due external Vertex auth, and User Presence failed due hardware UP denial rather than source/build/unit failure
- Unapproved hardware coordination required: no, full FIDO2 User Presence run was explicitly approved
- Persistent-state or destructive integration required: yes, potentially; full FIDO2 User Presence tests were explicitly approved
- Core/shared promotion became unavoidable: no
- Outcome: commit is acceptable only with the explicit caveat that Cato follow-up could not run due Vertex reauth and User Presence integration remains inconclusive/failed from UP denial

## Next Phase Inputs

- Required reading before next phase: `docs/SDK-HOUSE-STYLE.md`
- Required reading before next phase: `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`
- Required reading before next phase: `docs/plans/module-consolidation/ISA.md`
- Required reading before next phase: this learning note
- Pattern to apply: prefer pure encoding helpers where they reduce session noise without hiding protocol transmit/decode flow.
- Risk to watch: do not expand `CtapRequestBuilder.WithValue` into a generic command-object framework.
- Hardware risk to watch: do not interpret full `RequiresUserPresence` category failures as source regressions unless a coordinated focused test passes/fails under reliable touch timing.

## Compact Summary

- Goal: canonicalize FIDO2 MakeCredential/GetAssertion request encoding while preserving visible CTAP flow
- Files changed: FIDO2 session, CTAP builder, pure encoder helper, unit tests, module docs, learning note
- Final pattern: session owns send/decode; pure helper owns local CBOR request construction; builder supports direct value writes
- Tests passed: format, FIDO2 build, unit tests 383/383, read-only GetInfo integration 8/8
- Cato result: initial concern fixed locally; follow-up blocked by Vertex `invalid_rapt`
- User Presence result: full approved tagged run attempted, failed/inconclusive due repeated `OperationDenied` timeouts
- Shared/Core candidates: focused xUnit v3 runner behavior and UP coordination harness remain deferred
