# Phase 25 Learnings: OpenPGP Session Wire Tests

Phase 25 added representative OpenPGP session APDU wire tests through the public session surface and fixed the OpenPGP integration project's missing runtime skip dependency exposed by the newly allowed full self-runnable integration run.

## Changed Files

- `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpSessionWireTests.cs`
  - Added public-session APDU capture tests using `RecordingSmartCardConnection`.
  - Covered initialization SELECT / GET VERSION / application-related GET DATA flow.
  - Covered `GetDataAsync(DataObject.PwStatusBytes)` GET DATA wire shape.
  - Covered `SetSignaturePinPolicyAsync(PinPolicy.Once)` PUT DATA wire shape.
  - Covered `VerifyPinAsync` VERIFY P2 and raw PIN payload when KDF data is absent.
- `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/Yubico.YubiKit.OpenPgp.UnitTests.csproj`
  - Added a private `Tests.Shared` project reference for the xUnit-free recorder helper.
- `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/Yubico.YubiKit.OpenPgp.IntegrationTests.csproj`
  - Added direct `Xunit.SkippableFact` package reference because `Tests.Shared` can throw `Xunit.SkipException` while keeping xUnit v2 packages private.
- `docs/plans/module-consolidation/phase-25-openpgp-session-wire-tests-ISA.md`
  - Records Phase 25 scope, constraints, criteria, verification, and review evidence.

## Key Findings

- The Phase 23/24 recorder pattern transfers cleanly to OpenPGP.
- `RecordingSmartCardConnection` is the correct shared test primitive for OpenPGP unit wire tests; no OpenPGP-local recorder or APDU DSL was needed.
- The OpenPGP initialization queue is stable for unit coverage: SELECT consumes `9000`, GET VERSION consumes BCD `05 08 00 9000`, and application-related GET DATA consumes a realistic `0x6E` TLV plus `9000`.
- Ordered-byte assertions matter here: tests assert APDU headers and short-APDU data fields, not mere tag presence.
- The VERIFY test intentionally queues `0x6A82` for KDF GET DATA to prove the current lazy KDF fallback path reaches `KdfNone` before sending VERIFY.
- No production OpenPGP source changed.
- No public OpenPGP API changed.
- No Core API change was needed; Phase 24 already fixed the relevant APDU payload ownership boundary.

## Integration Findings

- Full self-runnable OpenPGP integration was allowed under the updated master ISA and did not require UP, UV, touch, insert/remove, or human coordination.
- Marker scan found no `RequiresUserPresence`, `UserPresence`, insert/remove, manual, or skip markers in OpenPGP integration tests; it found only a firmware skip comment and UIF touch-policy section heading.
- Preflight evidence:
  - `ykman list --serials` returned `103`.
  - `ykman info` reported serial `103`, firmware `5.8.0.beta.0`, USB interfaces `OTP, FIDO, CCID`, and OpenPGP enabled.
  - Test infrastructure selected serial `103` over SmartCard with firmware `5.8.0`.
- First full integration attempt failed before hardware operations because `Xunit.SkippableFact` was not copied into the OpenPGP integration test runtime output.
- The fix mirrors Phase 23 PIV: direct package reference in the integration test project, with a comment explaining why `Tests.Shared` keeps its xUnit v2 packages private.
- Final OpenPGP integration passed: 92/92 unit tests and 48/48 integration tests in `3 m 4 s`.

## Verification Evidence

- Branch check: `git status --short --branch` showed `## yubikit-consolidation...origin/yubikit-consolidation [ahead 4]` before Phase 25 edits.
- Focused wire tests: `dotnet toolchain.cs -- test --project OpenPgp --filter "ClassName~OpenPgpSessionWireTests"` passed 4/4.
- OpenPGP build before integration reference fix failed only while compiling the new test due missing `ApplicationIds` import and then `byte[].Span`; both were corrected in the test file.
- OpenPGP build after fixes: `dotnet toolchain.cs -- build --project OpenPgp` passed with 0 warnings/errors across source, unit tests, and integration tests.
- Full OpenPGP unit tests: `dotnet toolchain.cs -- test --project OpenPgp` passed 92/92.
- Targeted formatting initially failed on final newline in `OpenPgpSessionWireTests.cs`; after `dotnet format ... --include ...`, targeted `--verify-no-changes` passed.
- Docs QA: `dotnet toolchain.cs -- docs-qa` passed, 54 active documentation files validated.
- Whitespace: `git diff --check` emitted only line-ending normalization warnings for the two touched OpenPGP csproj files; no whitespace errors.
- Formatting limitation: broad `dotnet format` verification on the whole OpenPGP integration test project is blocked by pre-existing line-ending/final-newline issues in untouched integration `.cs` files. Phase 25 did not modify those files.
- Integration first attempt: `dotnet toolchain.cs -- test --integration --project OpenPgp` failed 48/48 with `System.IO.FileNotFoundException: Could not load file or assembly 'Xunit.SkippableFact, Version=1.5.0.0'`.
- Integration final: `dotnet toolchain.cs -- test --integration --project OpenPgp` passed 92/92 unit tests and 48/48 integration tests.

## Review Evidence

- DevTeam reviewer route was forced with primary model context: `google-vertex-anthropic/claude-opus-4-8@default`.
- Review prompt: `/tmp/opencode/phase25-devteam-review-prompt.md`.
- Review JSON output: `/tmp/opencode/phase25-devteam-review.json`.
- Review text output: `/tmp/opencode/phase25-devteam-review.md`.
- Verdict: no material correctness, security, API, or test-quality defects found.
- Low notes:
  - VERIFY test is intentionally coupled to lazy KDF fallback ordering; inline comments make this acceptable.
  - `Tests.Shared` private reference is appropriate but should keep recorder xUnit-free.
  - Hand-built application-related-data fixture is the main maintenance point if parser requirements tighten.

## Candidate Disposition

- Core: no change accepted. Existing SmartCard protocol and recorder seams are the right boundaries for this phase.
- Tests.Shared: accepted reuse of `RecordingSmartCardConnection`; no new shared helper needed.
- Public OpenPGP API: no change.
- OpenPGP integration project: accepted direct `Xunit.SkippableFact` reference, matching the PIV integration fix from Phase 23.

## Deferred Future Improvements

- Consider extracting a shared OpenPGP application-related-data fixture only if another OpenPGP test class needs the same realistic initialization data.
- Continue tracking integration projects that reference `Tests.Shared`; any project using runtime skip infrastructure may need a direct `Xunit.SkippableFact` reference after Phase 22's private dependency decision.
- Keys, crypto, certificates, and reset wire paths remain candidates for future OpenPGP byte-level expansion, but Phase 25 intentionally covered representative session flow only.

## Phase 26 Recommendation

- Proceed to FIDO2 remaining CTAP consistency.
- Carry forward the Phase 24 Core API lesson: if a FIDO2/Core API boundary is hiding ownership or sensitive-buffer semantics, change the Core API rather than preserving a code smell.
- Use full self-runnable integration where tests do not require UP, UV, touch, insert/remove, or human coordination.
- Keep FIDO2 CTAP request construction visible; avoid replacing manual CBOR with opaque command objects.

## Compact Summary

- Goal: add representative OpenPGP session wire tests.
- Files changed: Phase 25 ISA, OpenPGP unit test csproj, OpenPGP integration csproj, new wire tests, learning note.
- Final pattern: public session methods plus shared recorder plus ordered APDU byte assertions.
- Rejected approaches: production refactor, OpenPGP-local recorder, APDU DSL, Core API churn.
- Tests passed: focused wire 4/4, OpenPGP build, unit 92/92, integration 48/48 after integration package fix.
- Integration lifecycle: serial 103, firmware 5.8.0 beta, full self-runnable OpenPGP integration passed.
- Shared/Core candidates: recorder reuse accepted, no Core change, direct integration skip package accepted.
- Deferred future improvements: shared ARD fixture only after reuse, integration skip package audit for other modules.
- Next phase recommendation: Phase 26 FIDO2 remaining CTAP consistency.
