# Phase 7 Learnings: WebAuthn API Coherence

Use this note as the handoff record for Phase 7 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Base branch: `yubikit-applets`
- Base commit: `bfc6bdd5`, per consolidation ISA
- Branch check command/result: `git status --short --branch` showed `## yubikit-consolidation`
- Unrelated worktree changes present: yes, two untracked Core YubiKey note files remained unstaged
- Refactor work ran only on `yubikit-consolidation`: yes
- Scope: add coherent public WebAuthn production construction over `IFidoSession`, add YubiKey factory extension, preserve RP ID/public-suffix validation, and repair stale module docs
- Criteria satisfied: yes
- Criteria deferred: bundled Public Suffix List implementation; full WebAuthn relying-party/server API; broad ceremony refactor
- Promotion candidates declared up front: none for `Core`, `Tests.Shared`, or `Cli.Shared`
- Files changed: `src/WebAuthn/src/Client/PublicSuffixChecker.cs`, `src/WebAuthn/src/Client/WebAuthnClient.cs`, `src/WebAuthn/src/IYubiKeyExtensions.cs`, `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientConstructionTests.cs`, `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/WebAuthnClientFactoryTests.cs`, `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/WebAuthnTestHelpers.cs`, `src/WebAuthn/CLAUDE.md`, this learning note
- Tests run: scoped format verification, WebAuthn build, WebAuthn unit tests
- Integration tests run: WebAuthn SmartCard factory smoke; human-coordinated WebAuthn SmartCard MakeCredential user-presence check from this phase
- Result: passed; Cato cross-vendor audit unavailable due Vertex auth failure
- Commit: this Phase 7 commit
- `/Ping` sent after successful phase: no, pending commit and compact summary

## Cross-SDK Alignment Decision

- Python `python-fido2` validates RP IDs with `verify_rp_id(rp_id, origin)` backed by bundled Public Suffix List data.
- Swift `yubikit-swift` exposes a high-level `WebAuthn.Client` over `CTAP2.Session` and requires a caller-supplied `PublicSuffixChecker`.
- Android `yubikit-android` accepts caller-supplied `effectiveDomain` and performs only exact-or-dot-suffix validation.
- Decision: .NET aligns primarily with Swift's client construction shape and Python's security posture. It keeps explicit public-suffix validation instead of adopting Android's weaker `effectiveDomain` primary API.

## Hardware Target

- Device: YubiKey 5.8 beta
- Serial: `103`
- Firmware source of truth: Management `GetDeviceInfoAsync`
- Management firmware observed: `5.8.0`
- Applet firmware observed, if observable: SmartCard integration device reported firmware `5.8.0`
- Applet firmware caveat observed: no separate applet firmware string surfaced by the smoke test output

## Integration Lifecycle

- Management preflight command/result: `dotnet toolchain.cs -- test --integration --project Management.IntegrationTests --filter "FullyQualifiedName~CreateManagementSession_WithSmartCardConnection_ReturnsValidSession"`; passed on serial `103`, firmware `5.8.0`
- Management preflight evidence captured before applet tests: yes
- Management preflight exception path used: no
- Alternate identity proof, if preflight skipped: not applicable
- Agent-runnable integration test allowlist: `FullyQualifiedName~WebAuthnClientFactoryTests`
- Integration scope was read-only: yes; client creation/disposal only
- Tests run: `dotnet toolchain.cs -- test --integration --project WebAuthn.IntegrationTests --smoke --filter "FullyQualifiedName~WebAuthnClientFactoryTests"`; passed 1/1 read-only SmartCard factory smoke
- Tests skipped: existing WebAuthn ceremony integration tests requiring `RequiresUserPresence`; stress test requiring reset/persistent state
- Skip reason: consolidation ISA forbids unattended UP, reset, destructive, and persistent-state integration tests
- Skip approved by: consolidation ISA; human approved a later coordinated UP lane only
- Selected tests mutate persistent state: no
- User Presence / UV required: no for agent-runnable factory smoke; yes for later human-coordinated ceremony check
- Human-coordinated hardware needed: yes, for optional final UP ceremony check
- Human-coordinated hardware scheduled/deferred/replaced: scheduled after build/unit/read-only checks pass
- Persistent state changed: no known discoverable credential; human-coordinated MakeCredential used discouraged resident-key preference and passed
- Destructive tests skipped completely: yes
- Reset/cleanup performed: no
- Result: read-only smoke passed; human-coordinated user-presence ceremony check passed earlier in this phase

## What Worked

- Pattern that improved readability: public `WebAuthnClient(IFidoSession, WebAuthnOrigin, PublicSuffixChecker, ...)` makes the production path visible without exposing backend internals as the primary story.
- Pattern that improved testability: existing `IWebAuthnBackend` constructor stayed intact for unit tests while the new constructor is tested through observable delegation to `IFidoSession`.
- Pattern that improved security/API clarity: `PublicSuffixChecker` names the policy requirement instead of exposing an anonymous `Func<string, bool>` in the new production constructor.

## What Did Not Work

- Rejected approach: Android-style `effectiveDomain` as the primary .NET API.
- Rejected approach rationale: it is only safe if callers compute eTLD+1 correctly; Swift and Python both keep explicit RP ID/public-suffix validation in SDK-owned WebAuthn flows.
- Helpers or abstractions that were too deep: none introduced.
- Changes that looked DRY but harmed readability: none introduced.

## House Style Updates

- Existing house-style rule confirmed: WebAuthn should delegate CTAP/FIDO behavior to Fido2 and avoid duplicating protocol logic.
- Existing house-style rule confirmed: public API construction paths should be visible and usable, not only test-seam-oriented.
- Rule that needs clarification: WebAuthn public-suffix checking is part of safe client-data/RP ID validation when the SDK builds `clientDataJSON`.
- Possible addition to `docs/SDK-HOUSE-STYLE.md`: WebAuthn client APIs should use named security policy delegates instead of raw `Func` parameters for public construction paths.

## Reusable Patterns

- Pattern: preserve the test backend seam, but add a production constructor that wraps the real lower-level session internally.
- Generalization class: candidate for one more module trial.
- Where it applies: modules where the only real implementation adapter is internal but the public constructor requires the adapter interface.
- Where it should not apply: modules whose public factory already creates a coherent application session directly.
- Example files: `src/WebAuthn/src/Client/WebAuthnClient.cs`, `src/WebAuthn/src/IYubiKeyExtensions.cs`

## Core / Shared Promotion Candidates

- Candidate: bundled Public Suffix List verifier
- Declared in phase ISA up front: no; explicitly deferred
- Should move to: stay module-local or dedicated future shared web utility after approval
- Evidence: Python has bundled PSL data; Swift requires caller-supplied checker; .NET currently requires caller policy
- Risk: shipping PSL data adds maintenance/update surface and broadens this phase beyond API coherence
- Decision: deferred
- Decision rationale: Phase 7 fixes construction coherence while preserving validation; bundled PSL can be a future API-hardening phase
- Revisit trigger: consumers ask for safe default WebAuthn construction without providing a checker
- Demotion/reversal needed for previous shared helper: no
- Demotion/reversal rationale: not applicable

## Cross-Module Implications

- Modules likely affected: WebAuthn only for source; Fido2 remains lower-level dependency
- Next module should copy: additive public construction over real lower-level session when test seams block production usability
- Next module should avoid: turning internal adapters into the primary public API solely to make construction possible
- Potential API compatibility concern: the existing backend constructor remains intact; new constructor is additive

## Verification Evidence

- Branch check commands: `git status --short --branch`
- Branch check exit result: passed; branch was `yubikit-consolidation`
- Build commands: `dotnet toolchain.cs -- build --project WebAuthn`
- Build exit result: passed, 0 warnings, 0 errors
- Unit test commands: `dotnet toolchain.cs -- test --project WebAuthn`
- Unit test exit result: passed, 104/104
- Integration test commands: `dotnet toolchain.cs -- test --integration --project WebAuthn.IntegrationTests --smoke --filter "FullyQualifiedName~WebAuthnClientFactoryTests"`; earlier human-coordinated UP check used `dotnet toolchain.cs -- test --integration --project WebAuthn.IntegrationTests --filter "FullyQualifiedName~CreateWebAuthnClientAsync_WithSmartCard_MakeCredentialReturnsResponse"`
- Integration test exit result: read-only smoke passed 1/1; human-coordinated UP check passed
- Command filters/projects: `WebAuthn`, `WebAuthn.IntegrationTests`, `FullyQualifiedName~WebAuthnClientFactoryTests`, `FullyQualifiedName~CreateWebAuthnClientAsync_WithSmartCard_MakeCredentialReturnsResponse`
- Cross-module verification plan, if shared infrastructure changed: not applicable; no shared infrastructure changed
- Results: all final build/unit/read-only integration verification passed after scoped formatting; no whitespace errors from `dotnet format --verify-no-changes --include ...`
- Manual review notes: final diff limited to intended Phase 7 WebAuthn files and this learning note; unrelated Core YubiKey note files remained unstaged
- Reviewer concerns resolved: Cato could not execute because Vertex returned `invalid_grant` / `invalid_rapt`; fallback self-review found no blocking concern

## Review Summary

- DevTeam engineer result: not run; single-author implementation within approved narrow scope
- DevTeam reviewer result: not run; final self-review/diff inspection completed for narrow approved scope
- Cross-vendor review result: skipped due tooling/auth failure
- Cross-vendor review waiver, if any: documented skip for this Phase 7 commit only
- Waiver approved by: not separately approved; used because exact required Cato route was unavailable at execution time
- Waiver reason and scope: Vertex Opus Cato execution failed; scope limited to final cross-vendor audit, not build/test verification
- Waiver tooling failure/unavailability evidence: `/tmp/opencode/cato-phase7-webauthn-audit.jsonl` contains `invalid_grant` / `invalid_rapt`; router had selected `google-vertex-anthropic/claude-opus-4-8@default`
- Fallback review performed: yes, manual final diff review plus targeted build/unit/integration verification
- Findings fixed: formatting/EOL issues fixed by scoped `dotnet format`; no blocking code findings remained
- Findings deferred: bundled PSL verifier; full server/RP API
- Human decisions: approved Phase 7 implementation and offered final user-presence touch assistance

## Abort / Split Assessment

- Wrong branch detected: no
- Phase exceeded approved scope: no
- Public API change required: yes, additive public constructor/factory approved
- Helper depth concern found: no
- Protocol flow became harder to inspect: no CTAP/protocol flow changed
- Verification failed twice for different root causes: no
- Unapproved hardware coordination required: no
- Persistent-state or destructive integration required: no for agent-runnable lane
- Core/shared promotion became unavoidable: no
- Abort learning note required: no
- Abort learning note committed with human approval: not applicable
- Outcome: continue

## Next Phase Inputs

- Required reading before next phase: `docs/SDK-HOUSE-STYLE.md`
- Required reading before next phase: `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`
- Required reading before next phase: `docs/plans/module-consolidation/ISA.md`
- Required reading before next phase: this learning note
- Pattern to apply: prefer additive production constructors/factories over exposing test adapters as the public story.
- Risk to watch: public-suffix policy remains caller-supplied; future consumers may need a bundled PSL verifier.
- Open questions for human approval: whether to add a bundled Public Suffix List verifier in a future phase.

## Compact Summary

- Goal: make WebAuthn production client construction coherent and cross-SDK aligned
- Files changed: additive WebAuthn client constructor/factory, constructor/integration tests, helper migration, module guidance, learning note
- Final pattern: production constructor wraps `IFidoSession`; backend seam remains for tests; named `PublicSuffixChecker` preserves validation clarity
- Rejected approaches: Android-style `effectiveDomain` primary API; bundled PSL in this phase; full server API
- Tests passed: format verification, WebAuthn build, 104 WebAuthn unit tests, 1 read-only SmartCard smoke integration, earlier human-coordinated SmartCard UP ceremony
- Integration lifecycle: Management preflight passed; read-only smoke passed; human-coordinated UP test passed; destructive/reset tests skipped
- Shared/Core candidates: bundled PSL verifier deferred
- House-style update needed: possible note about named WebAuthn security policy delegates
- Next phase recommendation: CLI consolidation only after Phase 7 commit and optional UP check are complete
- Learning note path: `docs/plans/module-consolidation/phase-7-webauthn-api-coherence-learnings.md`
- Commit: this Phase 7 commit
- `/Ping` status: pending
