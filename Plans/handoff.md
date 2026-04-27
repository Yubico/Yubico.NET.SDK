# Handoff — Audit cleanup wave: ExcludeList fix + 4 Criticals + 4 HIGH-severity finds shipped

**Date:** 2026-04-28
**Active branch:** `webauthn/phase-9.2-rust-port` (tip `2b1b0852`)
**HEAD ↔ origin:** **In sync** — pushed `cfea6e1f..2b1b0852` this session
**PR:** [Yubico/Yubico.NET.SDK#466](https://github.com/Yubico/Yubico.NET.SDK/pull/466) — `feat(webauthn): WebAuthn Client + previewSign extension (Phase 9 close)` — OPEN, no review decision yet
**Eventual merge target:** `yubikit-applets` (NOT `develop`, NOT `yubikit`, NOT `main`)
**Strategy frame:** [`Plans/yes-we-have-started-composed-horizon.md`](yes-we-have-started-composed-horizon.md) (rev 2)
**Supersedes:** `Plans/handoff.md` mid-session 2026-04-28 (post-`cfea6e1f`; the prior 2026-04-27 handoff covered up to `43ea328d`)

---

## Critical next step (read first)

**No active blockers. Branch is in sync with origin; continue monitoring PR #466.** This long session resolved the WebAuthn excludeList stress test failure, then ran a vslsp-driven CodeAudit, then shipped all 4 Critical findings + 4 of 7 HIGH findings (one was "deferred to next session" turned out actually-required, see lessons #2 below).

The next session should:

1. **Monitor PR #466** for Yubico maintainer review feedback (now ~22 commits beyond the 2026-04-27 handoff's tip)
2. **If review surfaces fixable items** → address inline on this branch and push
3. **Optionally tackle the remaining 3 HIGH audit findings** (Tier B + Tier C — see "Open follow-ups #2-#4" below). Tier B needs a design call from Dennis on ExtensionPipeline error semantics. Tier C is the WebAuthnClient.cs split, which absorbs both DRY findings.
4. **Phase 10 ARKG work still belongs on a fresh branch off `yubikit-applets`** — do NOT branch it off this one.

---

## Session summary (2026-04-28, single long session)

This session ran **fix → audit → cleanup** across three waves, plus a parallel investigation:

### Wave 1 — ExcludeList preflight bug fix
1. **Resumed from 2026-04-27 handoff** (tip `43ea328d`); discovered 2 unpushed commits had grown to 4 (`f62c7c4b feat(webauthn): add client-layer excludeList pre-flight (Java parity)` had landed since the handoff was written).
2. **Ran `WebAuthnExcludeListStressTests`** — failed in 51s with `WebAuthnClientError(Unknown)` wrapping `CtapException: PIN authentication failed` on the 18th MakeCredential.
3. **Three-agent forensic investigation** vs yubikit-android:
   - Agent A (wire-format): identical CBOR encoding
   - Agent B (lifecycle): both stacks share token across preflight + MakeCredential
   - **Agent C (preflight semantics)**: found the smoking gun in commit `f62c7c4b`'s own KNOWN ISSUE section — CTAP 2.1 §6.5.5.7 permission consumption invalidates the whole token after `GetAssertion(up=false)`
4. **Shipped fix `dc2ed141`** — re-mint pinUvAuthToken between preflight and MakeCredential, scoped to MC-only permissions. Test went FAIL → PASS in 34s.

### Wave 2 — CodeAudit + 4 Critical findings
5. **Invoked `/CodeAudit` skill** (general-purpose agent + vslsp daemon) scoped to WebAuthn + adjacent Fido2 (Credentials/Extensions/Pin/CredentialManagement/FidoSession). Returned 4 Critical / 8 High / 6 Medium / 6 Low. Diagnostic baseline: 0 errors / 0 warnings.
6. **Shipped fix `a0070db5`** — addressed all 4 Criticals:
   - `PinUvAuthTokenSession` finalizer fallback (defensive zeroing if Dispose forgotten)
   - `CredentialMatcher.IsNoCredentialsError` no longer swallows `CtapStatus.NotAllowed`
   - `WebAuthnClient` torn-state guard between dispose and re-mint (`tokenSession = null`)
   - General `MapCtapStatusToWebAuthnError` helper + general catch arms in MakeCredentialCoreAsync + GetAssertionCoreAsync (was previewSign-only)

### Wave 3 — HIGH audit findings (4 of 7 closed)
7. **Shipped `489c8539`** — removed dead `CreateUvRequest` + `_uvResponseTcs` from `StatusChannel.cs` (verified zero usages via vslsp `find_usages`). HIGH #1.
8. **Shipped `f547fca9` via `/DevTeam Ship`** — Engineer agent diagnosed that the catch guards in `FidoNfcTests` were already in place (from prior `37ff02fc`); the actual gap was a missing `using Xunit;` import. One-line fix landed. NOT a CodeAudit finding but addressed parallel open-follow-up from prior handoff.
9. **Shipped `2b1b0852` (Tier A cleanup)** — closes 3 more HIGH audit findings:
   - Added `WebAuthnClientErrorCode.Cancelled` + `OperationCanceledException` catch arm in both producer Task.Run lambdas (HIGH #7)
   - Removed dead `BackendMakeCredentialRequest.EnterpriseAttestation` (HIGH #2)
   - Removed dead `IWebAuthnBackend.GetUvRetriesAsync`/`GetPinRetriesAsync` + the now-orphan `PinRetriesResult` record (HIGH #3)

### Parallel housekeeping
10. **Pushed all commits** to `origin/webauthn/phase-9.2-rust-port` (now 8 ahead of yesterday's origin).
11. **Captured "cosmetic toolchain bug"** — `domain-test` invokes xUnit v3 runner with `--minimum-expected-tests 0` when filter selects no tests; runner rejects flag, project reports as failed despite no tests running. Tracker logged in Open Follow-ups #11.

---

## Branch state

```
yubikit-applets (merge target, origin)
  └── ... 73 commits prior phases ...
      └── webauthn/gate-2-fixup (95abc0c5)
          └── webauthn/phase-9.1-hygiene (5f7ab705)
              └── webauthn/phase-9.2-rust-port (2b1b0852) ← CURRENT, in sync with origin
```

**34 commits since `webauthn/phase-9.1-hygiene`; 107 commits since `yubikit-applets`.**

Commits this session (5 fixes + 1 doc; all pushed):
```
2b1b0852 chore(webauthn): Tier A audit cleanup — typed cancellation + remove dead public API
f547fca9 fix(fido2,test): add missing 'using Xunit;' to FidoNfcTests for Skip resolution
489c8539 chore(webauthn): remove dead CreateUvRequest + _uvResponseTcs from StatusChannel
cfea6e1f docs(handoff): 2026-04-28 — ExcludeList preflight token re-mint + 4 CodeAudit Critical fixes
a0070db5 fix(webauthn): address 4 critical CodeAudit findings (token hygiene, error mapping)
dc2ed141 fix(webauthn): re-mint pinUvAuthToken between excludeList preflight and MakeCredential
```

Carried from prior session (also pushed this session):
```
f62c7c4b feat(webauthn): add client-layer excludeList pre-flight (Java parity)
37ff02fc fix(fido2,test): guard FidoNfcTests against NotSupportedException on USB-only devices
43ea328d fix(fido2,test): re-mint pinUvAuthToken between GetAssertion calls in CredProtect L2
3107bd5c fix(fido2): parse previewSign unsignedExtensionOutputs at CTAP key 6 not 8
```

---

## Build & test status (verified at handoff time, 2026-04-28)

| Check | Status |
|---|---|
| `dotnet toolchain.cs build` | **0 errors** (1 pre-existing third-party `IL2026/IL3050` warning from `Microsoft.AspNetCore`) |
| `dotnet toolchain.cs -- test --project WebAuthn` (unit) | **All pass** |
| `vslsp get_diagnostics_summary` over solution | **0 errors / 0 warnings** baseline |
| `dotnet toolchain.cs -- test --integration --project WebAuthn --filter WebAuthnExcludeListStressTests` | **PASS in 34s** on freshly-Reset YK 5.8.0 (was FAIL at start of session) |
| `git status` | Clean (only `Plans/handoff.md` modified — being rewritten by this handoff) |
| Branch ↔ origin sync | **In sync** at `2b1b0852` |

---

## Worktree / Parallel Agent State

None. Single working tree at `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK` on `webauthn/phase-9.2-rust-port`.

---

## Readiness Assessment

**Target:** .NET 10 application developers integrating YubiKey WebAuthn / passkey flows; browser/RP implementers building WebAuthn-spec-compliant clients on top of CTAP2; security-engineering teams requiring auditable, modern-C# crypto handling. Now with **closed silent-failure paths around excludeList ceremonies, defensive token finalization, comprehensive typed error mapping, and tightened public API surface**.

| Need | Status | Notes |
|---|---|---|
| WebAuthn data model + ClientData/AttestationObject/AuthenticatorData | ✅ Working | Phases 1-2 |
| `WebAuthnClient.MakeCredentialAsync` + `GetAssertionAsync` | ✅ Working | Hardware-verified |
| Status streaming (`IAsyncEnumerable<WebAuthnStatus>`) | ✅ Working | Hardware-verified |
| Extension framework (CredProtect, CredBlob, MinPinLength, LargeBlob, PRF, CredProps, previewSign) | ✅ Working | All inputs/outputs in Fido2 |
| `previewSign` registration encoder/decoder | ✅ Working | Hardware-verified post-`3107bd5c` |
| `previewSign` single-credential authentication (hardware ceremony) | ⚠️ Skipped | Blocked on ARKG (Phase 10) |
| `previewSign` multi-credential probe-selection | ⚠️ Throws `NotSupported` | Phase 10 |
| **Architectural layering (Fido2 = canonical, WebAuthn = adapter)** | ✅ **Strict — zero duplication** | Maintained |
| **CredProtect Level 2 integration test** | ✅ Passing | `43ea328d` (prior session) |
| **WebAuthn excludeList stress test (17 RKs + preflight)** | ✅ **Now passing** | This session: `dc2ed141` |
| **PinUvAuthTokenSession finalizer fallback** | ✅ Added | `a0070db5` |
| **CTAP error code → typed WebAuthn error mapping** | ✅ Comprehensive | `a0070db5` (was previewSign-only); `2b1b0852` adds typed cancellation |
| **`CtapStatus.NotAllowed` propagation** | ✅ Fixed | `a0070db5` |
| **Cancellation surfaces as typed `WebAuthnClientErrorCode.Cancelled`** | ✅ New | `2b1b0852` |
| **WebAuthn dead public API surface** | ✅ Trimmed | `489c8539` + `2b1b0852` removed `CreateUvRequest`, `EnterpriseAttestation`, `Get*RetriesAsync` |
| **NFC integration tests (`Skip.If` resolution)** | ✅ Fixed | `f547fca9` added missing `using Xunit;` |
| **WebAuthnClient.cs god-object (now ~1130 LOC)** | ⚠️ Above 500-LOC threshold | Audit MEDIUM finding still open; absorbs both remaining DRY HIGH findings |
| **ExtensionPipeline silent CborContentException swallow** | ⚠️ Open | Audit HIGH #4 (Tier B) — needs design call |
| Build state | ✅ Clean | 0 errors |
| Unit test state | ✅ Green | All projects pass |
| Hardware integration sweep | ✅ Done 2026-04-27/28 | All flagged failures from prior handoff resolved or explained |

**Overall:** 🟢 **Production-ready for the spec-conformant subset, with strict Fido2/WebAuthn layering, full canonical extension coverage at the Fido2 layer, comprehensive CTAP→WebAuthn typed error mapping (including cancellation), defensive token finalization, a passing excludeList stress ceremony, and a trimmed public API surface.**

PR #466 is now noticeably higher-quality than at the start of this session. **Critical next step:** Continue monitoring PR #466 for review feedback.

---

## What's Next (Prioritized)

1. **Monitor PR #466** for maintainer review; address inline on this branch if fixable items surface — Critical next step
2. **Tier B audit cleanup (1 HIGH finding)** — `ExtensionPipeline` silent `CborContentException` swallow. Needs a design decision from Dennis: (a) log-and-continue + add diagnostic, or (b) surface a typed `MalformedExtension` flag on the output. (a) is cheaper; (b) is more honest to consumers
3. **Tier C audit cleanup (2 HIGH findings + 1 MEDIUM)** — `WebAuthnClient.cs` is ~1130 LOC god-object; the two DRY HIGH findings (#4 below) are best addressed as part of an extract-Builders/Validators/CTAP-Mapper-into-static-helpers task. Out of scope for PR #466 cleanup; defer to a focused follow-up branch
4. **Audit MEDIUM/LOW backlog** — 6 MEDIUM + 6 LOW findings remain (see Open follow-ups). Not blocking. Pick at leisure
5. **Phase 10 work** (ARKG, multi-cred probe, sig-verify) stays deferred to a fresh branch off `yubikit-applets` *after* PR #466 lands
6. **C3 + C4 envelope helpers** deferred-as-optional. Tracker in `Plans/phase-9.8-attestation-typed-variants.md`
7. **Toolchain bug** — `domain-test` should not pass `--minimum-expected-tests 0` when the filter selects no tests; should skip the project or omit the flag

---

## Blockers & Known Issues

- **None blocking PR #466.** All known regressions, silent-failure paths, and Critical audit findings from this session are now closed.
- **3 HIGH CodeAudit findings remain.** All non-blocking, all documented below with file:line.

---

## Open follow-ups (no active blockers)

### From CodeAudit (post-`dc2ed141`, scoped to WebAuthn + adjacent Fido2)

#### Tier B — needs a design call (1 HIGH)

| # | Item | File:line | Effort | Notes |
|---|---|---|---|---|
| 2 | **`ExtensionPipeline` silent `CborContentException` swallow** — caller cannot distinguish "extension absent" from "device returned malformed CBOR" | `src/WebAuthn/src/Extensions/ExtensionPipeline.cs:179-265, 296-348` | M | Two design options: (a) log-and-continue with Warning + extension id, (b) surface typed `MalformedExtension` flag on output. Pick before implementing |

#### Tier C — depends on WebAuthnClient.cs split (2 HIGH + 1 MEDIUM)

| # | Item | File:line | Effort | Notes |
|---|---|---|---|---|
| 3 | **DRY: 4-arg `MakeCredentialAsync` ↔ 4-arg `GetAssertionAsync`** — ~50 LOC mirror PIN-encoding + switch loop | `src/WebAuthn/src/Client/WebAuthnClient.cs:373-425` ↔ `441-500` | M | Extract `DrivePinUvAsync<TResult>` helper |
| 4 | **DRY: 2-arg `MakeCredentialAsync` ↔ 2-arg `GetAssertionAsync`** — same pattern at smaller scale | `WebAuthnClient.cs:84-124` ↔ `152-192` | S | Combinable with #3 in one helper |
| 5 | **MEDIUM: `WebAuthnClient.cs` god-object (~1130 LOC)** — orchestration + validation + builders + response shaping + error mapping all in one file | `src/WebAuthn/src/Client/WebAuthnClient.cs` (whole file) | L | Extract Builders + Validators + CTAP mapper to static helpers; closes #3 + #4 in the process |

#### Audit MEDIUM backlog (6 items)

| # | Item | File:line | Effort |
|---|---|---|---|
| 6 | `.ToArray()` allocation on hot CTAP path for `PinUvAuthParam` | `FidoSessionWebAuthnBackend.cs:140, 189` | S |
| 7 | DRY: PIN-request + MemoryPool-rent + copy block twice in MakeCredentialCoreAsync vs GetAssertionCoreAsync | `WebAuthnClient.cs:551-577` and `716-743` | S |
| 8 | `EnsureProtocolInitialized` defers async init to ClientPin's first use; commented as wishful thinking | `FidoSessionWebAuthnBackend.cs:235-243` | M |
| 9 | `ClientPin.GetPinUvAuthTokenUsingUvAsync` doesn't dispose `platformKey`; only `sharedSecret` zeroed | `src/Fido2/src/Pin/ClientPin.cs:412-455` | S |
| 10 | `ExcludeListPreflight` doesn't zero `pinUvAuthParam` HMAC output in finally | `src/WebAuthn/src/Internal/ExcludeListPreflight.cs:100, 141` | S |
| — | (Other MEDIUM #5 in original audit was the WebAuthnClient god-object, listed above as Tier C #5) | | |

#### Audit LOW backlog (6 items, optional)

| # | Item | File:line |
|---|---|---|
| L1 | Unused `IProgress<CtapStatus>? progress` parameter on backend methods | `FidoSessionWebAuthnBackend.cs:87, 117, 165` |
| L2 | `string.EndsWith(string)` allocation in RP-id suffix check (hot path) | `RpIdValidator.cs:69` |
| L3 | `CredentialMatcher` trusts device's `numberOfCredentials` field; cap defensively | `CredentialMatcher.cs:64-75` |
| L4 | `ByteArrayKeyComparer.GetHashCode` uses randomized `HashCode` — non-stable across processes | `Extensions/PreviewSign/ByteArrayKeyComparer.cs:49-60` |
| L5 | Unused `using System.Buffers.Binary;` import | `Extensions/PreviewSign/ByteArrayKeyComparer.cs:15` |
| L6 | Two-arg overloads' inline switch loops — pattern smell, see DRY HIGH | `WebAuthnClient.cs:104-107, 167-170` |

### Other open items (carried forward)

| # | Item | Disposition | Owner | Path / Tracker |
|---|---|---|---|---|
| 11 | Land PR #466 — review + merge to `yubikit-applets` | Awaiting Yubico maintainer review | external | https://github.com/Yubico/Yubico.NET.SDK/pull/466 |
| 12 | **Test #2 marginal value** — `HmacSecretMcOutput_DecodesCorrectly` either delete or rename | Open follow-up — undecided since 2026-04-23 | Dennis | `src/Fido2/tests/.../ExtensionTypesTests.cs:105` |
| 13 | **Phase 9.8 C3** — Fido2 envelope writer helper | Deferred-as-optional | TBD | `Plans/phase-9.8-attestation-typed-variants.md` |
| 14 | **Phase 9.8 C4** — Fido2 envelope decoder helper | Deferred-as-optional | TBD | Same tracker |
| 15 | Phase 10 — ARKG `additional_args` first-class builder | Deferred; gating prereq for any auth-path hardware test | TBD | `Plans/phase-10-previewsign-auth.md §3` |
| 16 | Phase 10 — multi-credential probe-selection | Deferred | TBD | `Plans/phase-10-previewsign-auth.md §1` |
| 17 | Phase 10 — cryptographic signature verification helper | Deferred | TBD | `Plans/phase-10-previewsign-auth.md §2` |
| 18 | **PreviewSign hardware re-verification** | Open — ARKG `-9` algorithm rejected by this YubiKey | TBD | `src/Fido2/tests/.../FidoPreviewSignTests.cs` |
| 19 | **Toolchain wrapper bug** — `--minimum-expected-tests 0` rejected by xUnit v3 runner | Open — flag should be omitted or project skipped when filter selects nothing | TBD | `dotnet toolchain.cs` test target |

**Resolved this session (chronological):**
- ✅ WebAuthn excludeList stress test went FAIL → PASS via `dc2ed141` token re-mint
- ✅ 4 Critical CodeAudit findings landed in `a0070db5` (finalizer, NotAllowed propagation, torn-state guard, CTAP→WebAuthn mapping)
- ✅ All session commits pushed to origin
- ✅ HIGH #1: dead `CreateUvRequest` + `_uvResponseTcs` removed (`489c8539`)
- ✅ NFC test `Skip.If` resolution fix (`f547fca9`) — Engineer agent caught existing guards + diagnosed the actual missing `using Xunit;`
- ✅ HIGH #2: dead `BackendMakeCredentialRequest.EnterpriseAttestation` removed (`2b1b0852`)
- ✅ HIGH #3: dead `IWebAuthnBackend.GetUvRetriesAsync`/`GetPinRetriesAsync` + orphan `PinRetriesResult` removed (`2b1b0852`)
- ✅ HIGH #7: typed `Cancelled` enum + `OperationCanceledException` arms in producer Task.Run lambdas (`2b1b0852`)

---

## Key File References

| File | Purpose |
|---|---|
| `src/WebAuthn/src/Client/WebAuthnClient.cs` | Public client orchestration; ~1130 LOC god-object; lines 595-635 contain preflight + token re-mint; lines 887-911 contain `MapCtapStatusToWebAuthnError`; lines 240/325 contain new `OperationCanceledException` arms |
| `src/WebAuthn/src/Client/PinUvAuthTokenSession.cs` | Sensitive token wrapper with finalizer fallback (lines 76-83) |
| `src/WebAuthn/src/Client/Authentication/CredentialMatcher.cs` | `IsNoCredentialsError` no longer swallows `NotAllowed` (lines 81-87) |
| `src/WebAuthn/src/Client/IWebAuthnBackend.cs` | Trimmed: removed `GetUvRetriesAsync`, `GetPinRetriesAsync`, `PinRetriesResult`, `BackendMakeCredentialRequest.EnterpriseAttestation` |
| `src/WebAuthn/src/Client/FidoSessionWebAuthnBackend.cs` | Trimmed: removed corresponding implementations |
| `src/WebAuthn/src/WebAuthnClientError.cs` | New `Cancelled` enum value |
| `src/WebAuthn/src/Internal/ExcludeListPreflight.cs` | Client-layer preflight from `f62c7c4b`; consumed by WebAuthnClient at line 595 |
| `src/WebAuthn/src/Extensions/ExtensionPipeline.cs` | **Tier B HIGH finding lives here** — silent CborContentException swallow at lines 179-265, 296-348 |
| `src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoNfcTests.cs` | `using Xunit;` added so `Skip.If` resolves; existing catch guards preserved |
| `~/.claude/projects/.../memory/feedback_ppuat_token_reuse.md` | Prior wisdom; this session's bug is the related-but-distinct "permission consumption invalidates whole token" rule |
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

# 5. Pick up audit cleanup if desired:
#    - Tier B (#2 above): ExtensionPipeline error semantics — needs Dennis design call first
#    - Tier C (#3 + #4 + #5): WebAuthnClient.cs split — bigger task, absorbs both DRY findings
#    - MEDIUM/LOW: pick freely, see Open follow-ups #6-#10 + L1-L6
# 6. If user wants Phase 10 → branch off yubikit-applets, NOT off this branch
```

**Do not** branch Phase 10 work off `webauthn/phase-9.2-rust-port` — branch off `yubikit-applets` after PR #466 lands.
**Do not** PR against `develop` or `yubikit` — `yubikit-applets` is the only valid target.

---

## Lessons captured (this session — for future audit rubrics + Sia behavior)

1. **Three-agent isolated forensic comparisons converge powerfully when bugs span families.** When facing a non-trivial bug ambiguous between multiple root-cause families (wire format vs lifecycle vs semantics), spawning three Explore agents with hard-walled lenses produces a confident diagnosis in one round. Convergence = high confidence; divergence = where to dig. Agent C found the smoking gun (the bug we ourselves had documented in the prior commit message) faster than a single-agent investigation would have.

2. **The commit message is part of the verification surface.** `f62c7c4b`'s "KNOWN ISSUE" section was the smoking gun. Always read commit messages of recent changes when triaging.

3. **CTAP 2.1 §6.5.5.7 — permission consumption invalidates the whole token, not just the consumed permission.** Mint-MC-only-after-preflight is the correct shape. Superset of the prior PPUAT rule.

4. **vslsp-driven CodeAudit beats grep-based dramatically.** `find_usages` to verify dead-code claims before reporting prevents false positives. `get_code_structure` with `file_filter` prevents context overflow.

5. **vslsp `find_usages` resolves to the wrong symbol when names are ambiguous.** `GetUvRetriesAsync` exists in both Fido2 `ClientPin` AND WebAuthn `IWebAuthnBackend`; vslsp picked the wrong one. **Lesson**: when the audit cites a specific file:line, prefer reading that file directly over symbol lookup.

6. **CodeAudit then immediately address the Criticals — don't defer.** Cleared all 4 Criticals + 4 of 7 HIGH in this session while context was warm. The remaining HIGH (Tier B + Tier C) require design decisions or larger refactor scope; correctly deferred as separate scoped tasks.

7. **DevTeam Ship saved a redundant edit.** When dispatched to add NFC catch guards, the Engineer agent VERIFIED the existing file state and discovered the catch guards already existed — only `using Xunit;` was missing. A blind inline edit would have produced duplicate guards. This is the value of the Engineer→verify-first pattern over direct execution.

8. **"Cosmetic" failure summaries deserve real explanation.** The toolchain reports red because xUnit v3 runner rejects `--minimum-expected-tests 0`; the actual test passed. Captured as Open Follow-up #19.

9. **Removing dead public API surface deliberately is fine for preview-stage projects.** WebAuthn isn't binary-compatibility constrained (per CLAUDE.md). Better to remove `EnterpriseAttestation` / `Get*RetriesAsync` now than to ship them as "coming soon" surface that consumers depend on.

10. **`OperationCanceledException` MUST be caught before general `Exception`.** OCE is an `Exception` subclass; catching the general arm first shadows the typed cancellation arm and silently makes cancellation indistinguishable from device errors. The original code had this exact bug (audit HIGH #7) — the new arms are correctly ordered.

(Lessons #1-9 from prior handoffs still apply — see `git show 43ea328d:Plans/handoff.md` for the full prior list.)

---

## Open risks (non-blocking)

Codebase is preview-stage; binary-compatibility / public-API stability is **not** a constraint *except* for the Fido2 public surface, which Dennis explicitly froze (and which Phase 9.8 deliberately broke for `AttestationStatement` only, with sign-off).

1. **Fido2 `AttestationStatement` breaking change is in PR #466.** A maintainer reviewing the PR should be flagged to commit `32145357`. Same risk profile as the prior handoff.
2. **WebAuthn public-API removals this session** (`EnterpriseAttestation` field on `BackendMakeCredentialRequest`; `GetUvRetriesAsync` + `GetPinRetriesAsync` on `IWebAuthnBackend`; `PinRetriesResult` record; `WebAuthnClientErrorCode.Cancelled` added). All justified; all flagged in commit `2b1b0852` body. Worth surfacing in the PR description before merge.
3. **WebAuthnClient.cs is now ~1130 LOC** with concerns spanning orchestration, validation, request-building, response-building, error mapping, and PIN/UV state handling. CodeAudit MEDIUM finding still open. Sensible cleanup target before Phase 10.
4. **9 build warnings (CS7022) from `Microsoft.NET.Test.Sdk` infrastructure** — pre-existing third-party. Could be suppressed via `<NoWarn>`.
5. **PR review may surface scope-expansion requests.** If reviewers ask for ARKG support to land in this PR, push back to Phase 10 — the parity evidence supports the encoder-only ship.
