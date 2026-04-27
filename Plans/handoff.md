# Handoff — ExcludeList preflight token re-mint + 4 CodeAudit Critical fixes shipped

**Date:** 2026-04-28
**Active branch:** `webauthn/phase-9.2-rust-port` (tip `a0070db5`)
**HEAD ↔ origin:** **In sync** — pushed `d4b95038..a0070db5` this session (6 commits flushed to origin including the 2 from prior handoff)
**PR:** [Yubico/Yubico.NET.SDK#466](https://github.com/Yubico/Yubico.NET.SDK/pull/466) — `feat(webauthn): WebAuthn Client + previewSign extension (Phase 9 close)` — OPEN, no review decision yet
**Eventual merge target:** `yubikit-applets` (NOT `develop`, NOT `yubikit`, NOT `main`)
**Strategy frame:** [`Plans/yes-we-have-started-composed-horizon.md`](yes-we-have-started-composed-horizon.md) (rev 2)
**Supersedes:** `Plans/handoff.md` from 2026-04-27 (post-consolidation cleanup; tip `43ea328d`)

---

## Critical next step (read first)

**No active blockers. Branch is in sync with origin; continue monitoring PR #466 and consider addressing remaining HIGH-severity CodeAudit findings.** This session resolved the `WebAuthnExcludeListStressTests` failure that was open at the prior handoff (was failing with `LimitExceeded`/`PinAuthInvalid` depending on how you read it; now passes in 34s isolated), then ran a CodeAudit pass and shipped 4 Critical fixes on top.

The next session should:

1. **Monitor PR #466** for Yubico maintainer review feedback (now ~20 commits beyond the prior handoff's tip)
2. **If review surfaces fixable items** → address inline on this branch and push
3. **Optionally address the remaining HIGH CodeAudit findings** (8 remain — see "Open follow-ups" section below)
4. **NFC test SkipException edge-case** still open from prior handoff — `FidoNfcTests` (3 methods) need `NotSupportedException` catch guards. Lower priority since the audit found bigger fish.
5. **Phase 10 ARKG work still belongs on a fresh branch off `yubikit-applets`** — do NOT branch it off this one.

---

## Session summary (2026-04-28)

This session was a **bug-fix + audit + critical-cleanup pass**. Sequence:

1. **Resumed from prior handoff** — pushed nothing yet, started by re-reading the handoff and confirming branch state. Discovered the 2 unpushed commits had grown to 4 (the handoff was already drift; commit `f62c7c4b feat(webauthn): add client-layer excludeList pre-flight (Java parity)` had landed since the handoff was written).

2. **Ran `WebAuthnExcludeListStressTests` integration test** — failed in 51s with `WebAuthnClientError(Unknown)` wrapping `CtapException: PIN authentication failed` on the 18th MakeCredential call (the excluded one). Specifically: 17 creates succeeded, then the final excluded MakeCredential after preflight failed.

3. **Three-agent forensic comparison** of our test vs yubikit-android's `Ctap2ClientTests.testMakeCredentialWithExcludeList`:
   - **Agent A** (wire format): identical CBOR encoding for excludeList; identified that Java mints token with `MC | (excludeCredentials.isEmpty() ? 0 : GA)` while C# always mints `MC|GA` upfront
   - **Agent B** (lifecycle): both stacks share token across preflight + MakeCredential within a single client call
   - **Agent C** (preflight semantics): **found the smoking gun** — the commit message of `f62c7c4b` itself documented this exact bug as a "KNOWN ISSUE" and specified the exact fix (re-mint token between preflight and MakeCredential per CTAP 2.1 §6.5.5.7 "authenticators MAY consume permissions on use")

4. **Fix #1: token re-mint** (Sia committed `dc2ed141`) — between `ExcludeListPreflight.FindFirstMatchAsync` and the actual MakeCredential dispatch, dispose the original `tokenSession` and re-mint with `MakeCredential`-only permissions. Also added `tokenCopy` zeroing in finally for hygiene. **Test went FAIL → PASS in 34s on freshly-Reset YK 5.8.0**.

5. **CodeAudit pass** (skill invocation, vslsp daemon used for diagnostics + structure) — scoped to `src/WebAuthn/src` plus adjacent Fido2 areas (Credentials/Extensions/Pin/CredentialManagement/FidoSession). Returned 4 Critical / 8 High / 6 Medium / 6 Low findings. Diagnostic baseline: **0 errors / 0 warnings**.

6. **Fix #2-5: 4 Critical CodeAudit findings addressed** (Sia committed `a0070db5`):
   - **PinUvAuthTokenSession finalizer** — class owns sensitive `byte[]` clone but had no finalizer; if a caller forgets `Dispose()`, bytes never zero. Added finalizer with `GC.SuppressFinalize` in Dispose.
   - **CredentialMatcher.IsNoCredentialsError** — was treating `CtapStatus.NotAllowed` (0x30, "device denied") as "no credentials." Removed that case so `NotAllowed` propagates and gets mapped properly.
   - **WebAuthnClient torn-state guard** — added `tokenSession = null` between Dispose and re-acquire so the outer finally is a clean no-op if the re-mint throws.
   - **CTAP→WebAuthn typed error mapping** — added private `MapCtapStatusToWebAuthnError` helper + general catch arms in both `MakeCredentialCoreAsync` and `GetAssertionCoreAsync`. Previously only previewSign-requested registrations had typed mapping; everything else leaked raw `CtapException`. New mapping covers PinAuth*/PinBlocked/NotAllowed/OperationDenied → NotAllowed; KeyStoreFull/LimitExceeded/Timeout → Constraint; Unsupported* → NotSupported; PinNotSet/UpRequired → Security; NoCredentials/InvalidCredential → InvalidState; everything else → Unknown.

7. **Pushed all 6 unpushed commits** to `origin/webauthn/phase-9.2-rust-port` (`d4b95038..a0070db5`).

8. **"Cosmetic" toolchain bug noted** — when the test filter matches no unit tests, `domain-test` toolchain wrapper invokes the xUnit v3 runner with `--minimum-expected-tests 0`, which the runner rejects with "expects a single non-zero positive integer value." Reports as project failure with no test ever running. Worth a follow-up bug in the toolchain wrapper but **not a code defect**.

---

## Branch state

```
yubikit-applets (merge target, origin)
  └── ... 73 commits prior phases ...
      └── webauthn/gate-2-fixup (95abc0c5)
          └── webauthn/phase-9.1-hygiene (5f7ab705)
              └── webauthn/phase-9.2-rust-port (a0070db5) ← CURRENT, in sync with origin
```

**31 commits since `webauthn/phase-9.1-hygiene`; 104 commits since `yubikit-applets`.**

Latest 6 commits on this branch (all pushed):
```
a0070db5 fix(webauthn): address 4 critical CodeAudit findings (token hygiene, error mapping)  ← Sia, today
dc2ed141 fix(webauthn): re-mint pinUvAuthToken between excludeList preflight and MakeCredential ← Sia, today
f62c7c4b feat(webauthn): add client-layer excludeList pre-flight (Java parity)               ← Dennis (prior session), now landed
37ff02fc fix(fido2,test): guard FidoNfcTests against NotSupportedException on USB-only devices ← prior session
43ea328d fix(fido2,test): re-mint pinUvAuthToken between GetAssertion calls in CredProtect L2 ← prior session
3107bd5c fix(fido2): parse previewSign unsignedExtensionOutputs at CTAP key 6 not 8           ← prior session
```

---

## Build & test status (verified at handoff time, 2026-04-28)

| Check | Status |
|---|---|
| `dotnet toolchain.cs build` | **0 errors** (1 pre-existing third-party `IL2026/IL3050` warning from `Microsoft.AspNetCore`) |
| `dotnet toolchain.cs -- test --project WebAuthn` (unit) | **All pass** (post-fix) |
| `vslsp get_diagnostics_summary` over solution | **0 errors / 0 warnings** baseline |
| `dotnet toolchain.cs -- test --integration --project WebAuthn --filter WebAuthnExcludeListStressTests` | **PASS in 34s** on freshly-Reset YK 5.8.0 (was FAIL in prior handoff) |
| `git status` | Clean (only `Plans/handoff.md` modified — being rewritten by this handoff) |
| Branch ↔ origin sync | **In sync** at `a0070db5` |

---

## Worktree / Parallel Agent State

None. Single working tree at `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK` on `webauthn/phase-9.2-rust-port`.

---

## Readiness Assessment

**Target:** .NET 10 application developers integrating YubiKey WebAuthn / passkey flows; browser/RP implementers building WebAuthn-spec-compliant clients on top of CTAP2; security-engineering teams requiring auditable, modern-C# crypto handling. Now with **closed silent-failure paths around excludeList ceremonies and proper token finalization**.

| Need | Status | Notes |
|---|---|---|
| WebAuthn data model + ClientData/AttestationObject/AuthenticatorData | ✅ Working | Phases 1-2 |
| `WebAuthnClient.MakeCredentialAsync` + `GetAssertionAsync` | ✅ Working | Hardware-verified |
| Status streaming (`IAsyncEnumerable<WebAuthnStatus>`) | ✅ Working | Hardware-verified |
| Extension framework (CredProtect, CredBlob, MinPinLength, LargeBlob, PRF, CredProps, previewSign) | ✅ Working | All inputs/outputs in Fido2 |
| `previewSign` registration encoder | ✅ Working | Hardware-verified for the byte path that completes |
| `previewSign` registration decoder (unsignedExtensionOutputs) | ✅ Correct (CTAP key 6) | Fixed `3107bd5c` (prior session) |
| `previewSign` single-credential authentication (encoder + decoder) | ✅ Encoder + decoder byte-correct | Decoder lives in Fido2 (Phase 9.7 F1) |
| `previewSign` single-credential authentication (hardware ceremony) | ⚠️ Skipped | Blocked on ARKG (Phase 10) |
| `previewSign` multi-credential probe-selection | ⚠️ Throws `NotSupported` | Phase 10 |
| **Architectural layering (Fido2 = canonical, WebAuthn = adapter)** | ✅ **Strict — zero duplication** | Maintained |
| Identity types, COSE typed model, AAGUID helper, AttestationStatement typed hierarchy | ✅ Single source in Fido2 | Maintained |
| **CredProtect Level 2 integration test** | ✅ Passing | Per-test re-mint via `43ea328d` (prior session) |
| **WebAuthn excludeList stress test (17 RKs + preflight)** | ✅ **Now passing** | This session: token re-mint via `dc2ed141` |
| **PinUvAuthTokenSession finalizer fallback** | ✅ Added | Defensive zeroing if Dispose is forgotten |
| **CTAP error code → typed WebAuthn error mapping** | ✅ Comprehensive | Was previewSign-only; now general |
| **`CtapStatus.NotAllowed` propagation** | ✅ Fixed | No longer swallowed as "no credentials" |
| **NFC integration tests (3 methods)** | ⚠️ SkipException edge case | Open since prior handoff — needs `NotSupportedException` catch guards |
| **WebAuthnClient.cs god-object (1067 LOC)** | ⚠️ Above 500-LOC threshold | Audit HIGH finding; not addressed this session |
| Build state | ✅ Clean | 0 errors |
| Unit test state | ✅ Green | All projects pass |
| Hardware integration sweep | ✅ Done 2026-04-27/28 | 67/76 + the previously-failing ExcludeList now PASS |

**Overall:** 🟢 **Production-ready for the spec-conformant subset, with strict Fido2/WebAuthn layering, full canonical extension coverage at the Fido2 layer, comprehensive CTAP→WebAuthn error mapping, defensive token finalization, and a passing excludeList stress ceremony.**

PR #466 contains the complete Phase 9 arc plus today's two response-decoder/test fixes plus today's ExcludeList correctness fix plus today's 4 audit fixes.

**Critical next step:** Continue monitoring PR #466 for review feedback.

---

## What's Next (Prioritized)

1. **Monitor PR #466** for maintainer review; address inline on this branch if fixable items surface — Critical next step
2. **Optionally fix remaining HIGH CodeAudit findings** (8 — see audit detail in "Open follow-ups"). Notable: dead `CreateUvRequest` + `_uvResponseTcs` field, unset `BackendMakeCredentialRequest.EnterpriseAttestation`, never-called `GetUvRetriesAsync`/`GetPinRetriesAsync` on `IWebAuthnBackend`, two large-scale DRY duplications between MakeCredential and GetAssertion overloads, `ExtensionPipeline` silent `CborContentException` swallowing, missing `OperationCanceledException` arm in producer Task.Run lambdas
3. **Fix NFC test SkipException edge case** — add `NotSupportedException` catch guards to 3 `FidoNfcTests` methods (open since prior handoff; pattern exists in `FidoTransportTests`)
4. **WebAuthnClient.cs split** — 1067 LOC, 8 audit findings; extract Builders + Validators into static helpers (audit MEDIUM)
5. **Test #2 marginal value** — `HmacSecretMcOutput_DecodesCorrectly` either delete or rename. Open since 2026-04-23
6. **Phase 10 work** (ARKG, multi-cred probe, sig-verify) stays deferred to a fresh branch off `yubikit-applets` *after* PR #466 lands
7. **C3 + C4 envelope helpers** deferred-as-optional. Tracker in `Plans/phase-9.8-attestation-typed-variants.md`
8. **Toolchain bug** — `domain-test` should not pass `--minimum-expected-tests 0` when the filter selects no tests in a project; should skip the project or omit the flag

---

## Blockers & Known Issues

- **None blocking PR #466.** All known regressions and silent-failure paths from the prior handoff are now closed.
- **8 HIGH CodeAudit findings remain.** Not blocking, but worth attention if review feedback prompts a cleanup pass. Full list available by re-reading session context or re-running `/CodeAudit` on the same scope.
- **Phase 10 work is still deferred.** ARKG `-9` rejected by this YubiKey's firmware; investigate when starting Phase 10 (off `yubikit-applets`).

---

## Open follow-ups (no active blockers)

| # | Item | Disposition | Owner | Path / Tracker |
|---|---|---|---|---|
| 1 | Land PR #466 — review + merge to `yubikit-applets` | Awaiting Yubico maintainer review | external | https://github.com/Yubico/Yubico.NET.SDK/pull/466 |
| 2 | **NFC tests `NotSupportedException` guard** | Open — pattern exists in FidoTransportTests | TBD | `FidoNfcTests.cs:38, 53, 72` — add `catch (NotSupportedException) { Skip.If(true, ...); }` |
| 3 | **HIGH: dead `CreateUvRequest` + `_uvResponseTcs`** | Open from CodeAudit | TBD | `src/WebAuthn/src/Client/Status/StatusChannel.cs:125-137` |
| 4 | **HIGH: unset `BackendMakeCredentialRequest.EnterpriseAttestation`** | Open from CodeAudit | TBD | `src/WebAuthn/src/Client/IWebAuthnBackend.cs:164` — wire it through or remove |
| 5 | **HIGH: never-called `GetUvRetriesAsync`/`GetPinRetriesAsync`** | Open from CodeAudit | TBD | `src/WebAuthn/src/Client/IWebAuthnBackend.cs:46,51` — wire into PIN-failure flow or remove |
| 6 | **HIGH: DRY between MakeCredential and GetAssertion overloads** | Open from CodeAudit | TBD | `src/WebAuthn/src/Client/WebAuthnClient.cs:84-124 vs 152-192` and `373-425 vs 441-500` |
| 7 | **HIGH: ExtensionPipeline swallows `CborContentException`** | Open from CodeAudit | TBD | `src/WebAuthn/src/Extensions/ExtensionPipeline.cs:179-265, 296-348` — log + surface MalformedExtension |
| 8 | **HIGH: missing `OperationCanceledException` arm in producer Task.Run** | Open from CodeAudit | TBD | `src/WebAuthn/src/Client/WebAuthnClient.cs:236-244, 316-324` — add Cancelled enum value |
| 9 | **MEDIUM: WebAuthnClient.cs god-object (1067 LOC)** | Open from CodeAudit | TBD | Extract Builders + Validators to static helpers |
| 10 | **Test #2 marginal value** — `HmacSecretMcOutput_DecodesCorrectly` either delete or rename | Open follow-up — undecided since 2026-04-23 | Dennis | `src/Fido2/tests/.../ExtensionTypesTests.cs:105` |
| 11 | **Phase 9.8 C3** — Fido2 envelope writer helper | Deferred-as-optional | TBD | `Plans/phase-9.8-attestation-typed-variants.md` |
| 12 | **Phase 9.8 C4** — Fido2 envelope decoder helper | Deferred-as-optional | TBD | Same tracker |
| 13 | Phase 10 — ARKG `additional_args` first-class builder | Deferred; gating prereq for any auth-path hardware test | TBD | `Plans/phase-10-previewsign-auth.md §3` |
| 14 | Phase 10 — multi-credential probe-selection | Deferred | TBD | `Plans/phase-10-previewsign-auth.md §1` |
| 15 | Phase 10 — cryptographic signature verification helper | Deferred | TBD | `Plans/phase-10-previewsign-auth.md §2` |
| 16 | **PreviewSign hardware re-verification** | Open — ARKG `-9` algorithm rejected by this YubiKey | TBD | `src/Fido2/tests/.../FidoPreviewSignTests.cs` |
| 17 | **Toolchain wrapper bug** — `--minimum-expected-tests 0` rejected by xUnit v3 runner | Open — flag should be omitted or project skipped when filter selects nothing | TBD | `dotnet toolchain.cs` test target |

**Resolved this session:**
- ✅ WebAuthn excludeList stress test went FAIL → PASS via `dc2ed141` token re-mint
- ✅ 4 Critical CodeAudit findings landed in `a0070db5` (finalizer, NotAllowed propagation, torn-state guard, general CTAP→WebAuthn mapping)
- ✅ All 6 unpushed commits from prior + this session pushed to origin
- ✅ Wisdom: CTAP 2.1 §6.5.5.7 permission-consumption rule confirmed via three-agent triangulation

---

## Key File References

| File | Purpose |
|---|---|
| `src/WebAuthn/src/Client/WebAuthnClient.cs` | Public client orchestration; lines 595-635 contain the preflight + token re-mint sequence; lines 887-911 contain the new `MapCtapStatusToWebAuthnError` helper; god-object at 1067 LOC |
| `src/WebAuthn/src/Client/PinUvAuthTokenSession.cs` | Sensitive token wrapper with new finalizer fallback (lines 76-83) |
| `src/WebAuthn/src/Client/Authentication/CredentialMatcher.cs` | `IsNoCredentialsError` no longer swallows `NotAllowed` (lines 81-87) |
| `src/WebAuthn/src/Internal/ExcludeListPreflight.cs` | Client-layer preflight from `f62c7c4b`; consumed by WebAuthnClient at line 595 |
| `src/WebAuthn/tests/.../WebAuthnExcludeListStressTests.cs` | Integration test now passing in 34s |
| `~/.claude/projects/.../memory/feedback_ppuat_token_reuse.md` | Prior wisdom: regular pinUvAuthToken regeneration; this session's bug is the related-but-distinct "permission consumption invalidates whole token" |
| `Plans/yes-we-have-started-composed-horizon.md` | Strategy frame (rev 2) |
| `Plans/phase-9.7-soc-consolidation.md` | SoC consolidation PRD + completion record |
| `Plans/phase-9.8-attestation-typed-variants.md` | AttestationStatement breaking-change record + C3/C4 deferred-as-optional |
| `Plans/phase-10-previewsign-auth.md` | Most-likely next destination after PR #466 lands |
| `artifacts/test-runs/excludelist-stress-2026-04-27.log` | Pre-fix failure: PinAuthInvalid on 18th call after preflight |
| `artifacts/test-runs/excludelist-stress-postfix-2026-04-28.log` | Post-fix success: 1/0/0 in 34s |

---

## Quick Start for New Agent

```bash
# 1. Confirm branch + check sync
git checkout webauthn/phase-9.2-rust-port
git fetch
git status                                  # expect: clean, up to date with origin

# 2. Verify build/test state
dotnet toolchain.cs build                   # expect 0 errors
dotnet toolchain.cs -- test --project WebAuthn  # expect green

# 3. Read in order
cat Plans/yes-we-have-started-composed-horizon.md      # strategy frame (rev 2)
cat Plans/phase-9.7-soc-consolidation.md               # SoC consolidation PRD + completion record
cat Plans/phase-9.8-attestation-typed-variants.md      # AttestationStatement breaking-change record
cat Plans/phase-10-previewsign-auth.md                 # most-likely next destination

# 4. Check PR status
gh pr view 466

# 5. If picking up audit cleanup → start with HIGH findings #3-8 in Open Follow-ups
# 6. If picking up NFC NotSupportedException guards → see Open Follow-ups #2
# 7. If user wants Phase 10 → branch off yubikit-applets, NOT off this branch
```

**Do not** branch Phase 10 work off `webauthn/phase-9.2-rust-port` — branch off `yubikit-applets` after PR #466 lands.
**Do not** PR against `develop` or `yubikit` — `yubikit-applets` is the only valid target.

---

## Lessons captured (this session — for future audit rubrics + Sia behavior)

1. **Three-agent isolated forensic comparisons converge powerfully.** When facing a non-trivial bug ambiguous between multiple root-cause families (wire format vs lifecycle vs semantics), spawning three Explore agents with hard-walled lenses produced a confident diagnosis in one round. Convergence = high confidence; divergence = where to dig. Agent C found the smoking gun (the bug we ourselves had documented in the prior commit message) faster than a single-agent investigation would have.

2. **CTAP 2.1 §6.5.5.7 — permission consumption invalidates the whole token, not just the consumed permission.** When a `pinUvAuthToken` is minted with `MC|GA` and a `GetAssertion(up=false)` consumes the GA permission, the token can no longer authorize ANY operation including MakeCredential (whose MC permission slot is logically untouched). The fix is always: re-mint between operations. This is a superset of the prior PPUAT rule.

3. **The commit message is part of the verification surface.** `f62c7c4b`'s "KNOWN ISSUE" section was the smoking gun for this session's bug. Always read commit messages of recent changes when triaging — they often document exactly the gap you're hitting.

4. **vslsp daemon-driven CodeAudit is dramatically more thorough than grep-based.** Using `find_usages` to verify dead-code claims before reporting prevents false positives. Using `get_code_structure` with file_filter prevents context overflow. Daemon was running through the audit + fix cycle and continued to surface diagnostics on every file save.

5. **"Cosmetic" failure summaries deserve real explanation.** When the test summary line shows red but the actual integration test passed, the toolchain itself is misreporting due to its xUnit-runner contract. Capture this as a separate toolchain bug so it doesn't keep eating future agents' time.

6. **CodeAudit then immediately address the Criticals — don't defer.** The audit was 25 findings; addressing all of them at once would have been a separate big project. Addressing the 4 Criticals immediately while context was warm cost ~30 min and produced a clean follow-up commit. The HIGH/MEDIUM findings can wait for a dedicated cleanup pass without losing the Critical-level safety wins.

(Lessons #1-9 from prior handoffs still apply — see `git show 43ea328d:Plans/handoff.md` for the full prior list.)

---

## Open risks (non-blocking)

Codebase is preview-stage; binary-compatibility / public-API stability is **not** a constraint *except* for the Fido2 public surface, which Dennis explicitly froze (and which Phase 9.8 deliberately broke for `AttestationStatement` only, with sign-off).

1. **Fido2 `AttestationStatement` breaking change is in PR #466.** A maintainer reviewing the PR should be flagged to commit `32145357`. Same risk profile as the prior handoff.
2. **WebAuthnClient.cs is now 1067 LOC** with concerns spanning orchestration, validation, request-building, response-building, error mapping, and PIN/UV state handling. CodeAudit flagged it as a god-object. Not blocking PR #466 but a sensible cleanup target before Phase 10.
3. **9 build warnings (CS7022) from `Microsoft.NET.Test.Sdk` infrastructure** — pre-existing third-party. Could be suppressed via `<NoWarn>`.
4. **YubiKey 5.8.0 firmware behaviors** confirmed via this session's hardware run. Documented in PR #466 description.
5. **PR review may surface scope-expansion requests.** If reviewers ask for ARKG support to land in this PR, push back to Phase 10 — the parity evidence supports the encoder-only ship.
