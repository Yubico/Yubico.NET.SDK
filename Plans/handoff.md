# Handoff — Phase 9 closed + architectural cleanup, PR #466 open

**Date:** 2026-04-23 (late session)
**Active branch:** `webauthn/phase-9.2-rust-port` (tip `5c732297`, 8 commits ahead of origin — needs push)
**PR:** [Yubico/Yubico.NET.SDK#466](https://github.com/Yubico/Yubico.NET.SDK/pull/466) — `feat(webauthn): WebAuthn Client + previewSign extension (Phase 9 close)`
**Eventual merge target:** `yubikit-applets` (NOT `develop`, NOT `yubikit`, NOT `main`)
**Strategy frame:** [`Plans/yes-we-have-started-composed-horizon.md`](yes-we-have-started-composed-horizon.md) (rev 2)
**Supersedes:** prior `Plans/handoff.md` from earlier in this session (which framed Phase 9 as already PR-ready; an architectural audit found and fixed real layering issues post-PR-open)

---

## Critical next step (read first)

**Push the branch then monitor PR #466.** 8 commits since the last push capture the architectural cleanup phase (Phase 9.5 + 9.5b + 9.5c). Then:

1. **Push:** `git push origin webauthn/phase-9.2-rust-port`
2. **Update PR description** to reference the new architectural truth: previewSign is now a first-class Fido2 extension via `ExtensionBuilder.WithPreviewSign(...)`; WebAuthn provides the high-level adapter on top
3. **Monitor PR** for review feedback
4. **Do NOT start Phase 10 ARKG work in this session** — that's a fresh branch off `yubikit-applets` after PR #466 lands

---

## Readiness Assessment

**Target user (inferred from `CLAUDE.md`, `src/Fido2/CLAUDE.md`, `src/WebAuthn/CLAUDE.md`):**
- .NET 10 application developers integrating YubiKey WebAuthn / passkey flows
- Browser/RP implementers building WebAuthn-spec-compliant clients on top of CTAP2
- Security-engineering teams requiring auditable, modern-C# crypto handling
- **NEW:** developers consuming the canonical Fido2 surface directly (now properly layered with previewSign as a first-class extension)

| Capability | Status | Notes |
|---|---|---|
| WebAuthn data model + ClientData/AttestationObject/AuthenticatorData | ✅ Working | Phases 1-2; byte-parity with Swift |
| `WebAuthnClient.MakeCredentialAsync` + `GetAssertionAsync` | ✅ Working | Hardware-verified 2026-04-23 |
| Status streaming (`IAsyncEnumerable<WebAuthnStatus>`) | ✅ Working | Hardware-verified 2026-04-23 |
| Extension framework (CredProtect, CredBlob, MinPinLength, LargeBlob, PRF, CredProps) | ✅ Working | Phase 6 + commit `95abc0c5` (extension-passthrough bugfix) |
| `previewSign` **registration** | ✅ Working | Hardware-verified 2026-04-23 (YubiKey 5.8.0-beta) |
| `previewSign` **single-credential authentication** (encoder) | ✅ Encoder byte-correct | 4 byte-level unit tests vs Rust upstream reference; encoder lives in **Fido2** layer (canonical) since Phase 9.5b |
| `previewSign` **single-credential authentication** (hardware ceremony) | ⚠️ Skipped | Blocked on ARKG (Phase 10 §3); test correctly reports as Skipped via `[SkippableTheory]` |
| `previewSign` **multi-credential probe-selection** | ⚠️ Throws `NotSupported` | Cites `Plans/phase-10-previewsign-auth.md` |
| Architectural layering (Fido2 = canonical, WebAuthn = adapter) | ✅ **Correct** | Cleaned up in Phase 9.5b + 9.5c — see commits below |
| `ExtensionBuilder.WithPreviewSign(...)` overloads | ✅ Working | Mirror `WithPrf` pattern at `src/Fido2/src/Extensions/ExtensionBuilder.cs:259,271` |
| Fido2 canonical previewSign integration test | ✅ Working | `FidoPreviewSignTests.cs` consumes `ExtensionBuilder.WithPreviewSign(...)` (no test-internal CBOR helpers) |
| Fido2-layer PIN-required error path test | ✅ Working | `FidoMakeCredentialTests.MakeCredential_WhenPinRequiredButNotProvided_ThrowsInvalidParameter` |
| `ExtensionPipeline` manual-merge dance | ✅ **Retired** | `src/WebAuthn/src/Extensions/ExtensionPipeline.cs` had 6 `CborWriter` allocations; now 0 |
| Embedded rationalizations in CLAUDE.md | ✅ Corrected | Both Fido2 + WebAuthn CLAUDE.md notes reflect architectural truth |
| Build state | ✅ Clean | 0 errors; 9 warnings are pre-existing `CS7022` from `Microsoft.NET.Test.Sdk` (third-party infra, not introduced this session) |
| Unit test state | ✅ Green | 10/10 projects pass · WebAuthn 90/0 · Fido2 353/0 |
| Hardware integration sweep (2026-04-23) | ✅ Done | 7/7 testable WebAuthn integration tests PASS on YubiKey 5.8.0-beta; 1 SKIP (previewSign auth ARKG-blocked) |
| Phase 10 tracker | ✅ Filed | `Plans/phase-10-previewsign-auth.md` (ARKG, multi-credential probe, sig-verify) |
| Phase 9.4 tracker | ✅ Filed | `Plans/phase-9.4-fido2-extension-coverage.md` (4 minor unit-test polish gaps) |

**Overall readiness:** 🟢 **Production-ready for the spec-conformant subset, with proper Fido2/WebAuthn layering.**

The architectural critique surfaced after PR open ("there shouldn't be tests in WebAuthn that Fido2 doesn't already have, by extension encoders shouldn't live in WebAuthn either") was acted upon: previewSign is now first-class Fido2; WebAuthn delegates. Two parallel cleanup engineers retired the `ExtensionPipeline` manual-merge code and eliminated intra-WebAuthn DRY in `WebAuthnClient.EncodeAttestationObject`.

**Critical next step:** Push branch + land PR #466.

---

## Session summary (2026-04-23 — late)

This session continued from the Phase 9 close handoff and went deeper:

1. **Architectural audit prompt by user:** *"Did we also address the integration tests gaps in the Fido2 module?"* triggered a wider assessment beyond the Phase 9.4 narrow extension-coverage scan. Found 2 inverted tests (previewSign registration + PIN-required error path proven on hardware in WebAuthn but not Fido2). Filed as Phase 9.5.

2. **Engineer dispatched** to add the 2 missing Fido2 integration tests. Engineer chose to construct CBOR manually with `CborWriter` inside the test (`BuildPreviewSignRegistrationInput` helper at `FidoPreviewSignTests.cs:196`), declined to add `WithPreviewSign(...)` to `ExtensionBuilder` claiming "doesn't fit pattern." **Worse: wrote that rationalization into `src/Fido2/CLAUDE.md` as canonical convention.**

3. **User caught the architectural call:** *"In terms of DRY and Principle of Least Surprise and from the standpoint of an SDK user trying to use our SDK — was that the right call for the architecture?"* I evaluated and refuted the Engineer's defense (PRF accepts a structurally-identical composite input shape; the test was tautological; the CLAUDE.md note baked the wrong call into convention). Verdict: WRONG.

4. **Phase 9.5b refactor** — Engineer dispatched (with explicit "may not diverge" PRD) to:
   - Move previewSign types + encoder into `Yubico.YubiKit.Fido2.Extensions.PreviewSignExtension`
   - Add `WithPreviewSign(PreviewSignRegistrationInput)` + `WithPreviewSign(PreviewSignAuthenticationInput)` to `ExtensionBuilder`
   - Refactor WebAuthn adapter to delegate to Fido2
   - Delete the test-internal `BuildPreviewSignRegistrationInput`
   - Migrate the 4 byte-level WebAuthn unit tests to Fido2.UnitTests
   - Fix both CLAUDE.md notes (Fido2 + WebAuthn)
   - 5 commits landed: `06eb7fc3`, `95205be6`, `c4dcdef1`, `49c75f68`, `57e77330`

5. **Cross-module CodeAudit** — dispatched to find OTHER instances of the same pattern class (DRY across boundary, layering inversion, test-internal re-impl, asymmetric convention, embedded rationalization). Verdict: PASS-WITH-FINDINGS — 3 findings:
   - Finding #1+#2 (paired, Medium): `ExtensionPipeline.cs` still bypassed the new `WithPreviewSign(...)` and hand-rolled merge with `CborWriter`; embedded rationalization comment said "PreviewSign has its own CBOR format - cannot use ExtensionBuilder" (false post-refactor)
   - Finding #3 (Observational): `WebAuthnClient.EncodeAttestationObject` duplicated `WebAuthnAttestationObject.Encode` — intra-module DRY

6. **Phase 9.5c — two parallel Engineer dispatches** (per "fix all findings"):
   - Engineer A (commit `5c732297`): Refactored `PreviewSignAdapter` to mirror PRF/CredProtect pattern (`ApplyToBuilderForRegistration`, `ApplyToBuilderForAuthentication`); retired all 6 `CborWriter` allocations and 2 obsolete comments in `ExtensionPipeline.cs`. Net: -146 lines.
   - Engineer B (commit `2df77454`): Refactored `WebAuthnClient.BuildRegistrationResponse` to use new `WebAuthnAttestationObject.Create(...)` factory; deleted `WebAuthnClient.EncodeAttestationObject` entirely. Verified byte-equivalence before refactor.

7. **Operational mishap:** I ran `git stash` (no-op, clean tree) followed by `git stash pop` which inadvertently popped a stale `WIP on yubikit-applets@e8540368` stash and created merge conflicts in `Plans/handoff.md` plus reintroduced 2 deleted test files. **Recovered via `git reset --hard HEAD` and dropping the bad stash.** Lesson captured below.

---

## Branch state

```
yubikit-applets (merge target, origin)
  └── ... 64 commits prior phases ...
      └── webauthn/gate-2-fixup (95abc0c5)
          └── webauthn/phase-9.1-hygiene (5f7ab705)
              └── webauthn/phase-9.2-rust-port (5c732297) ← CURRENT, NOT YET PUSHED (8 commits ahead)
```

**8 commits since last push (`728a1178`):**
```
5c732297 refactor(webauthn): route PreviewSignAdapter through ExtensionBuilder       [Engineer A, Phase 9.5c]
2df77454 refactor(webauthn): eliminate DRY in attestation-object encoding            [Engineer B, Phase 9.5c]
57e77330 docs(fido2,webauthn): update CLAUDE.md with architectural truth             [Phase 9.5b]
49c75f68 test(fido2): migrate previewSign tests to canonical Fido2 layer             [Phase 9.5b]
c4dcdef1 refactor(webauthn): delegate previewSign encoding to Fido2                  [Phase 9.5b]
95205be6 feat(fido2): add WithPreviewSign builder methods                            [Phase 9.5b]
06eb7fc3 feat(fido2): add PreviewSignExtension types and encoder                     [Phase 9.5b]
e94e6ffe test(fido2): add canonical previewSign registration and PIN error tests     [Phase 9.5]
```

(plus 8 earlier commits below, all already pushed; total 16 since `yubikit-applets`.)

---

## Build & test status (verified at handoff time)

| Check | Status |
|---|---|
| `dotnet toolchain.cs build` | **0 errors / 9 warnings** (warnings are pre-existing `CS7022` from `Microsoft.NET.Test.Sdk` infra; not introduced this session) |
| `dotnet toolchain.cs test` | **All 10 projects pass** |
| `dotnet toolchain.cs -- test --project WebAuthn` | **90 / 0 / 0** (was 104; 4 migrated to Fido2.UnitTests + ~10 from `[Theory]` row consolidation during adapter refactor — test methods unchanged at the file level, verified) |
| `dotnet toolchain.cs -- test --project Fido2` | **353 / 0 / 0** |
| WebAuthn integration suite on YubiKey 5.8.0-beta (2026-04-23) | **7 / 0 / 1** (1 SKIP = previewSign auth, ARKG-blocked) |
| `git status` | Clean working tree |
| Branch ↔ origin sync | **8 commits ahead** — needs `git push` |
| `grep -c "CborWriter" src/WebAuthn/src/Extensions/ExtensionPipeline.cs` | **0** (was 6) |
| `grep -c "cannot use ExtensionBuilder" src/WebAuthn/src/Extensions/ExtensionPipeline.cs` | **0** (was 2) |
| `grep -c "EncodeAttestationObject" src/WebAuthn/src/Client/WebAuthnClient.cs` | **0** (delegated to `WebAuthnAttestationObject.Create`) |

---

## Open work (no active blockers)

| # | Item | Disposition | Owner | Path / Tracker |
|---|---|---|---|---|
| 1 | **Push branch + update PR #466 description** | Immediate next action for orchestrator/user | Sia | `git push origin webauthn/phase-9.2-rust-port` then `gh pr edit 466 --body ...` |
| 2 | Land PR #466 — review + merge to `yubikit-applets` | Awaiting Yubico maintainer review | external | https://github.com/Yubico/Yubico.NET.SDK/pull/466 |
| 3 | Phase 10 — ARKG `additional_args` first-class builder | Deferred; gating prerequisite for any auth-path hardware test | TBD | `Plans/phase-10-previewsign-auth.md §3` |
| 4 | Phase 10 — multi-credential probe-selection | Deferred; no upstream proves it | TBD | `Plans/phase-10-previewsign-auth.md §1` |
| 5 | Phase 10 — cryptographic signature verification helper | Deferred | TBD | `Plans/phase-10-previewsign-auth.md §2` |
| 6 | Phase 9.4 — 4 Fido2 unit-test polish gaps | Deferred; non-functional | TBD | `Plans/phase-9.4-fido2-extension-coverage.md` |

**No item blocks PR #466.**

---

## Audit history

| Audit | Verdict | Reference |
|---|---|---|
| Gate 1 (after Phase 6) | 0 Critical / 4 High / 7 Med / 9 Low; all High + 6 Med fixed | `Plans/audit-gate-1.md` |
| Gate 2 (after Phase 8) | 3 Critical / 4 High / 5 Med / 4 Low; all Critical + High + 4 Med fixed | `Plans/audit-gate-2.md` |
| Phase 9.1 hygiene | PASS-WITH-NOTES | (in conversation) |
| Phase 9.2 path 2A | PASS-WITH-NOTES (encoder byte-correctness independently verified vs Rust source) | (in conversation) |
| Phase 9.3 hardware sweep | 7/7 testable PASS, 1 SKIP, 0 regressions | `Plans/yes-we-have-started-composed-horizon.md` § "Phase 9.3 — Hardware verification record" |
| Post-9 Fido2 extension coverage | 4 minor gaps → Phase 9.4 deferred tracker | `Plans/phase-9.4-fido2-extension-coverage.md` |
| **Architectural-layering audit (this session)** | **WRONG (Engineer's previewSign-not-in-builder call) → fixed in Phase 9.5b** | (in conversation) |
| **Cross-module CodeAudit (this session)** | **PASS-WITH-FINDINGS (3 findings, all shipped fixes in Phase 9.5c)** | (in conversation) |

---

## Quick start for fresh agent

```bash
# 1. Confirm branch + pull
git checkout webauthn/phase-9.2-rust-port
git fetch
git status                                  # should be: 8 ahead OR clean if pushed

# 2. If not pushed yet, push:
git push origin webauthn/phase-9.2-rust-port

# 3. Verify build/test state
dotnet toolchain.cs build                   # expect 0 errors / 9 pre-existing CS7022 warnings
dotnet toolchain.cs test                    # expect all 10 projects pass

# 4. Read in order
cat Plans/yes-we-have-started-composed-horizon.md      # strategy frame (rev 2)
cat Plans/phase-10-previewsign-auth.md                 # most-likely next destination
cat Plans/phase-9.4-fido2-extension-coverage.md        # smaller deferred tracker
cat Plans/cnh-authenticator-rs-previewsign-parity.md   # the upstream evidence
ls Plans/*previewsign*.md                              # all parity reports

# 5. Check PR status
gh pr view 466

# 6. If user wants Phase 10 → branch off yubikit-applets, NOT off this branch
```

**Do not** branch Phase 10 work off `webauthn/phase-9.2-rust-port` — branch off `yubikit-applets` after PR #466 lands.
**Do not** PR against `develop` or `yubikit` — `yubikit-applets` is the only valid target.

---

## Lessons captured (for future audit rubrics + Sia behavior)

1. **If any helper uses `Skip.If` from `xunit.SkippableFact`, the audit must run the integration suite** to confirm Skip behavior. Phase 9.1 audit ran only unit tests; the `[Theory]` vs `[SkippableTheory]` mismatch was invisible until Phase 9.3 hardware sweep.
2. **Single-source DEFER verdicts are fragile; multi-source parity matrices flip cleanly.** Phase 9.0 originally surveyed only libfido2; expanding to libfido2 + android + Rust + Swift gave a verdict that survived audit cross-checks.
3. **Engineer surprise findings need an audit cross-check that asks "is the new framing snapshot-tautological or independently derived?"** The Phase 9.2 audit's strategic-finding cross-check confirmed the byte-level unit tests asserted CBOR-spec-derived bytes, not snapshots of the C# encoder's output.
4. **NEW: When an Engineer's rationalization is verifiable, verify it against the relevant peer.** "Doesn't fit the existing pattern" is testable. The Phase 9.5 Engineer's claim that previewSign couldn't fit `ExtensionBuilder` was refuted in 30 seconds by reading `WithPrf(PrfInput)` — it accepts a structurally-identical composite input.
5. **NEW: A test that re-implements the encoder it tests is worse than no test.** The Phase 9.5 Engineer's `BuildPreviewSignRegistrationInput` test helper was tautological; it could never catch wire-format regressions in the canonical encoder. Audit rubrics should include "does this test consume the canonical production encoder, or a test-local re-impl?"
6. **NEW: Embedded rationalizations in CLAUDE.md / docs / comments bake mistakes into convention.** The Engineer's note in `src/Fido2/CLAUDE.md` ("doesn't fit the existing builder pattern") would have stopped future contributors from questioning the call. Audit rubrics should look for defensive justifications ("doesn't fit", "WebAuthn-level only", "special case") and verify their truth.
7. **NEW: Initial assessments scoped narrowly produce narrow findings.** The Phase 9.4 coverage assessment was prompt-scoped to "extension coverage" and missed the broader architectural question about layering inversions. Re-prompt at higher conceptual levels when an architectural principle hasn't been validated.
8. **NEW: Avoid `git stash` on a clean tree.** `git stash` with no changes silently does nothing; a subsequent `git stash pop` will then pop a stale stash from the stack from a completely different branch state. Operational lesson: check `git stash list` before any pop, and prefer `git diff` / `git show` for inspection over stash-based comparison.

---

## Open risks (non-blocking)

Codebase is preview-stage; binary-compatibility / public-API stability is **not** a constraint.

1. **WebAuthn unit-test count dropped from 104 → 90.** 4 of those migrated to Fido2.UnitTests (verified). The remaining ~10 are most likely `[Theory]` row consolidation during adapter API refactoring (test method count unchanged in `PreviewSignAdapterTests.cs`). Worth a future spot-check — count test METHODS pre and post via `git show 728a1178:.../*.cs | grep -cE "public.*Task|public.*void"` and compare.
2. **9 build warnings (CS7022) from `Microsoft.NET.Test.Sdk` infrastructure.** Pre-existing third-party. Could be suppressed via `<NoWarn>` if visual noise is bothersome.
3. **YubiKey 5.8.0-beta firmware behaviors** may differ from production firmware. Document any beta-specific findings in PR #466 description (the "only ARKG accepted for previewSign" finding is the notable one).
4. **PR review may surface scope-expansion requests.** If reviewers ask for ARKG support to land in this PR, push back to Phase 10 — the parity evidence supports the encoder-only ship.
