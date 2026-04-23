# Handoff — Phase 9.1 shipped + parity evidence in hand, awaiting Phase 9.2 direction

**Date:** 2026-04-22 (evening)
**Active branch:** `webauthn/phase-9.1-hygiene` (tip `5f7ab705`)
**Base for this branch:** `webauthn/gate-2-fixup` (tip `95abc0c5`)
**Eventual merge target:** `yubikit-applets` (NOT `develop`, NOT `yubikit`)
**Plan reference:** [`Plans/yes-we-have-started-composed-horizon.md`](yes-we-have-started-composed-horizon.md) — the approved Phase 9 plan; supersedes the prior session's "Phase 9 deferrals" list (which had 3 inaccurate items)

---

## Critical next step (read first)

**The user has 3 open decisions blocking Phase 9.2 startup.** They were posed at the end of the prior turn and not yet answered:

1. **Commit the 3 uncommitted plan/report files** (`Plans/handoff.md`, `Plans/yes-we-have-started-composed-horizon.md`, `Plans/libfido2-previewsign-parity.md`, `Plans/yubikit-android-previewsign-parity.md`) to the hygiene branch? Or branch 9.2 off `webauthn/gate-2-fixup` and let plans land separately?
2. **Dispatch the 9.2 Step 1 Engineer agent now**, or pause first?
3. **Short-circuit 9.2 Step 1 entirely** and go directly to path 2B (close the previewSign auth surface), trusting the parity evidence already in hand?

**Recommended (per the principle "only ship what an upstream reference has proven works"):** option 3 — go directly to path 2B for the **authentication** entry point only (registration is proven and ships unmarked). The combined libfido2 + yubikit-android evidence is sufficient — running a fourth Swift investigation would produce the same DEFER verdict.

---

## Readiness assessment

**Target user (inferred from `CLAUDE.md`, `src/Fido2/CLAUDE.md`, `src/WebAuthn/CLAUDE.md`):**
- .NET 10 application developers integrating YubiKey WebAuthn / passkey flows
- Browser/RP implementers building WebAuthn-spec-compliant clients on top of CTAP2
- Security-engineering teams requiring auditable, modern-C# crypto handling

| Need | Status | Notes |
|---|---|---|
| WebAuthn data model (RP, User, Descriptor, COSE, AAGUID, preferences) | ✅ Working | Phase 1; 110+ unit tests |
| `clientDataJSON` + `AttestationObject` + `AuthenticatorData` | ✅ Working | Phase 2; byte-parity with Swift via hand-rolled JSON |
| `WebAuthnClient.MakeCredentialAsync` (terminal) | ✅ Working | Phase 3 + Phase 5 stream-drain refactor |
| `WebAuthnClient.GetAssertionAsync` + `MatchedCredential.SelectAsync` | ✅ Working | Phase 4; idempotent via `Lazy<Task<>>` |
| Status streaming (`IAsyncEnumerable<WebAuthnStatus>`) + interactive PIN/UV | ✅ Working | Phase 5; deadlock-fixed |
| Extension framework (CredProtect, CredBlob, MinPinLength, LargeBlob, PRF, CredProps) | ✅ Working | Phase 6 + commit `95abc0c5` (CRITICAL bugfix — extensions were silently dropped before; now wired) |
| previewSign **registration** (key generation) | ✅ Working | Phase 7+8+Gate-2 fixes; hardware-validated on YubiKey 5.8.0 (`Registration_WithPreviewSign_ReturnsGeneratedSigningKey` passes) |
| previewSign **authentication** (sign-by-credential) | ⚠️ Partial | Code path exists; throws `CtapException: Invalid length` on hardware. **No upstream SDK has hardware-tested it** (libfido2: NONE; Swift: untested; yubikit-android: code exists, not hardware-tested) |
| previewSign multi-credential probe-selection | ❌ Missing | Deferred — `signByCredential.Count != 1` throws `NotSupported`. Blocked on auth path proving viable |
| Module documentation (`src/WebAuthn/CLAUDE.md`) | ✅ Working | NEW in 9.1 — 8/8 sections present |
| Logging factory guidance | ✅ Working | NEW in 9.1 — root `CLAUDE.md` corrected `LoggingFactory`→`YubiKitLogging` |
| Integration test project | ⚠️ Partial | 8 tests exist on hardware (registration paths pass; one previewSign auth test SKIPPED with diagnostic notes). No no-UP tests yet |
| Test cleanup helper (`DeleteAllCredentialsForRpAsync`) | ✅ Working | NEW in 9.1 — copied from Fido2 pattern |
| Unit-test warnings (xUnit1051, CS8625) | ✅ Working | NEW in 9.1 — 0 warnings on `dotnet toolchain.cs build` |

**Overall readiness:** 🟢 **Production** for the spec-conformant subset (registration + single-credential authentication for *non-previewSign* extensions). previewSign **registration** ships clean; previewSign **authentication** + multi-credential probe should ship with a `[Experimental]` attribute + `NotSupported` throw per the Phase 9.2 path 2B plan, with a `Plans/phase-10-previewsign-auth.md` follow-up tracker.

---

## Session summary

This session did three things:

1. **Diagnosed the prior handoff's "Phase 9 deferrals" list as partially wrong.** Three of the eight items were inaccurate: M-5 (raw RP-ready `AttestationObject` bytes) is **already done** (`WebAuthnAttestationObject.RawCbor` exists at line 49); the integration test project was **partially built** with a previously-unknown CRITICAL wire-format bug discovered by hardware testing (`previewSign` authentication throws `CtapException: Invalid length` *before* user-presence prompt); and the most recent commit (`95abc0c5`) revealed a **latent Phase-6 bug** — the entire extension framework's CBOR was silently dropped because `options.Extensions = request.Extensions` was never assigned in the backend. All visible to the integration tests, none visible to Audit Gates 1 or 2.

2. **Wrote and got approval for the Phase 9 plan** at `Plans/yes-we-have-started-composed-horizon.md`. Three sub-phases:
   - **9.0** — Parallel libfido2 investigation (background)
   - **9.1** — Module hygiene bundle (CLAUDE.md, logging fix, helper, constants split, warning cleanup)
   - **9.2** — Swift+libfido2 parity check, then conditional auth/probe work (path 2A=port wire fix + probe; path 2B=close auth surface)
   - **9.3** — Hardware verification + integration test expansion (when user is back to touch the YubiKey 5.8.0-beta)

3. **Executed Phases 9.0 + 9.1 in parallel** using the proven DevTeam Ship → CodeAudit → Ping rhythm:
   - **9.0** → libfido2 v1.17.0 has **zero** previewSign code paths (verdict NONE)
   - **9.0 bonus** → user requested yubikit-android parity check too; found v3.1.0 has full code paths for **both** registration and authentication, with hardware-tested **registration only**, authentication code untested
   - **9.1** → 5 commits, audit verdict **PASS-WITH-NOTES** (better than engineer self-reported: 0 warnings vs claimed-residue-of-8; constants split finer than asked; all 8/8 CLAUDE.md sections; scope discipline excellent)

**Key finding:** The combined parity evidence (Swift untested + libfido2 NONE + yubikit-android registration-tested-but-auth-untested) creates a clean **registration vs authentication asymmetry**. Registration is hardware-proven across two SDKs (Android + the C# port itself). Authentication is unimplemented (libfido2) or untested (Swift, Android). The Phase 9.2 plan's path 2B should be tightened to mark **only the authentication entry point** as `[Experimental]`, not the whole previewSign API.

---

## Branch state

```
develop  (mainline — DO NOT TARGET)
  └── yubikit  (SDK rewrite root)
      └── yubikit-applets  (per-applet branch — MERGE TARGET; tip b76b6144)
          └── webauthn/phase-1-data-model
              └── ... (8 phase + 2 audit-fixup branches; see prior handoff for tree) ...
                  └── webauthn/gate-2-fixup  (tip 95abc0c5; +2 commits beyond prior handoff:)
                      ├── 95abc0c5 fix(webauthn): wire extension passthrough and fix previewSign parser  ← CRITICAL bugfix
                      └── 97a502d5 docs: clarify --project + --filter usage for targeted test runs
                       │
                       └── webauthn/phase-9.1-hygiene  (tip 5f7ab705; ← CURRENT, 5 commits:)
                           ├── fbe45bc4 docs(webauthn): add module CLAUDE.md
                           ├── f90bbc8f test(webauthn): add DeleteAllCredentialsForRpAsync helper
                           ├── 63adea35 refactor(webauthn): split PreviewSignCbor key constants into scoped classes
                           ├── eadb8fc3 test(webauthn): fix xUnit1051 warnings and CS8625 in GetAssertionTests
                           └── 5f7ab705 docs: correct LoggingFactory→YubiKitLogging in root CLAUDE.md
```

**Total work since `yubikit-applets`:** 64 (prior phases) + 2 (post-handoff, on gate-2-fixup) + 5 (9.1 hygiene) = **71 commits across 11 branches**.

---

## Build & test status (verified at handoff time)

| Check | Status |
|---|---|
| `dotnet toolchain.cs build` | **0 errors / 0 warnings** (improved from prior session's ~12 xUnit1051) |
| `dotnet toolchain.cs test` | **All 10 projects pass** (~1246+ unit tests) |
| `dotnet toolchain.cs -- test --project WebAuthn` | **101 / 0** (WebAuthn unit tests) |
| Fido2 cross-module regression | ✅ Green (Phase 2's `RawData` + Gate-2's `UnsignedExtensionOutputs` Fido2-internal additions still cohere) |
| Hardware integration (registration) | ✅ Passes on YubiKey 5.8.0-beta (per `PreviewSignTests.cs`) |
| Hardware integration (previewSign auth) | ❌ `CtapException: Invalid length (0x03)` — test `Skip.If(true)`'d with diagnostic notes at `PreviewSignTests.cs:89-114` |

---

## Files added/modified this session

**Uncommitted on hygiene branch (need user direction whether to commit here, on 9.2 branch, or separately):**
- `Plans/handoff.md` — this file (overwrote prior session's handoff)
- `Plans/yes-we-have-started-composed-horizon.md` — approved Phase 9 plan
- `Plans/libfido2-previewsign-parity.md` — Phase 9.0 deliverable; verdict NONE
- `Plans/yubikit-android-previewsign-parity.md` — bonus parity report; verdict PROVEN-registration / untested-authentication

**Committed in 9.1 (5 commits, see branch state above):**
- `src/WebAuthn/CLAUDE.md` (new, 265 lines, 8 sections)
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/WebAuthnTestHelpers.cs` (added `DeleteAllCredentialsForRpAsync`)
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignCbor.cs` (split into 4 nested static classes — `RegistrationInputKeys`, `RegistrationOutputKeys`, `AuthenticationInputKeys`, `AuthenticationOutputKeys`)
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/.../WebAuthnClientGetAssertionTests.cs` (xUnit1051 + CS8625 fixes; CS8625 now uses `WebAuthnAuthenticatorData.Decode(rawAuthData)` — structural)
- Root `CLAUDE.md` (`LoggingFactory` → `YubiKitLogging` with citation to `src/Core/src/YubiKitLogging.cs:20`)

---

## Phase 9 sub-phase status

| Sub-phase | Status | Branch | Notes |
|---|---|---|---|
| **9.0** Parallel libfido2 + yubikit-android parity | ✅ Done | (no branch — read-only investigations) | Reports landed in `Plans/`; both feed 9.2 verdict |
| **9.1** Module hygiene bundle | ✅ Done — audit PASS-WITH-NOTES | `webauthn/phase-9.1-hygiene` | 5 commits, 0 build warnings, 101/0 tests |
| **9.2** Parity verdict + conditional auth/probe | 🔜 Next — awaiting user direction | TBD (`webauthn/phase-9.2-*`) | Three open decisions; recommend skipping Step 1 → straight to path 2B for auth entry point |
| **9.3** Hardware verification + integration expansion | ⏸️ Blocked on 9.2 + user presence | `webauthn/phase-9.3-integration` | Requires user physically present at YubiKey 5.8.0-beta |
| **Post-9** Fido2 canonical extension coverage assessment | ⏸️ Tracked, post-9.3 | (no branch — assessment only) | Spawn 1 Explore agent; trivial gaps → 9.4, substantial gaps → separate deferred plan |

---

## Audit cross-references

### Prior gates (still authoritative)
- **Gate 1** (after Phase 6) — `Plans/audit-gate-1.md` — 0 Critical / 4 High / 7 Med / 9 Low; all High + 6 Med fixed
- **Gate 2** (after Phase 8) — `Plans/audit-gate-2.md` — 3 Critical / 4 High / 5 Med / 4 Low; all Critical + High + 4 Med fixed

### This session
- **9.1 Hygiene Audit** (this session) — verdict PASS-WITH-NOTES; notes are observational only, none blocking. Highlights: build cleaner than self-reported (0 warnings), constants split improved beyond plan ask (4 nested classes vs 2), CLAUDE.md exceeds 6/8 bar (8/8). Full audit verdict captured in this session's transcript.

---

## Parity evidence base for Phase 9.2 verdict (combined)

| SDK | Version | previewSign code paths | Hardware-tested registration | Hardware-tested authentication | Verdict source |
|---|---|---|---|---|---|
| **yubikit-swift** | release/1.3.0 | both reg + auth | unverified | **none** (PreviewSignTests.swift has only registration) | per existing `PreviewSignTests.cs:107` diagnostic |
| **libfido2** | v1.17.0 | **none** | n/a | n/a | `Plans/libfido2-previewsign-parity.md` |
| **yubikit-android** | v3.1.0 | both reg + auth | ✅ instrumented + integration tests | **none** | `Plans/yubikit-android-previewsign-parity.md` |
| **Yubico.NET.SDK (this port)** | webauthn/phase-9.1-hygiene | both reg + auth (auth currently throws) | ✅ on YubiKey 5.8.0-beta | ❌ `CtapException: Invalid length` (wire-format bug) | this session's investigation |

**Synthesis:** Registration is hardware-proven in 2 of 4 implementations (Android + C#). Authentication is hardware-proven in **0 of 4**. Per the user-stated principle "only ship what an upstream reference has proven works on hardware," authentication should be deferred via path 2B with `[Experimental]` + `NotSupported` on the auth entry point only. Registration ships unmarked. Phase 10 follow-up tracker captures unblocking criteria.

---

## Open risks (non-blocking, named for awareness)

1. **`[Experimental]` is a public-API commitment.** Once shipped, removing it later (in Phase 10) is binary-compatible (fine); adding new throws is not (avoid). Mitigation: keep the throw site narrow (auth entry point only).
2. **YubiKey 5.8.0-beta firmware behaviors** may differ from production firmware. Document any beta-specific findings in PR description for Yubico reviewers.
3. **Three Plans/ files are uncommitted on the hygiene branch.** If the user picks "branch 9.2 off gate-2-fixup" route, those files won't travel automatically — the resumer must explicitly cherry-pick or re-create them.

---

## Worktree / parallel-agent state

No active worktrees:
```
/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK 5f7ab705 [webauthn/phase-9.1-hygiene]
```

All session agents have terminated cleanly:
- `phase9.1-hygiene-engineer` (Engineer agent for Phase 9.1) — completed; 5 commits landed
- `libfido2-parity-explorer` (Explore agent for Phase 9.0, ran in background) — completed; report at `Plans/libfido2-previewsign-parity.md`
- `yubikit-android-parity-explorer` (Explore agent, bonus investigation) — completed; report at `Plans/yubikit-android-previewsign-parity.md`
- `phase9.1-hygiene-auditor` (general-purpose audit gate agent) — completed; verdict PASS-WITH-NOTES

No `.claude/worktrees/` directory exists.

---

## Quick start for fresh agent

```bash
# 1. Confirm branch
git checkout webauthn/phase-9.1-hygiene
git log --oneline -10                              # confirm 5 hygiene commits + 95abc0c5 below them

# 2. Verify state
dotnet toolchain.cs build                          # expect 0 errors / 0 warnings
dotnet toolchain.cs -- test --project WebAuthn     # expect 101 / 0

# 3. Read the plan + parity reports IN ORDER
cat Plans/yes-we-have-started-composed-horizon.md  # the approved Phase 9 plan
cat Plans/libfido2-previewsign-parity.md           # 9.0 deliverable, verdict NONE
cat Plans/yubikit-android-previewsign-parity.md    # bonus, verdict PROVEN-reg-only
cat Plans/audit-gate-1.md                          # historical, still authoritative
cat Plans/audit-gate-2.md                          # historical, still authoritative

# 4. Resume — present the 3 open decisions to user (see "Critical next step" above) and wait for direction

# 5. If user picks "go straight to 2B" (recommended):
#    - Branch webauthn/phase-9.2-defer-auth off webauthn/phase-9.1-hygiene (or gate-2-fixup; user will say)
#    - Engineer prompt skeleton is in Plans/yes-we-have-started-composed-horizon.md §Phase 9.2 Step 2B
#    - Key tasks: improve NotSupported message at PreviewSignAuthenticationInput.cs:58, add [Experimental("YK-PreviewSignAuth")]
#      to the auth entry point, replace Skip.If(true) integration test with a unit test asserting the throw, create
#      Plans/phase-10-previewsign-auth.md follow-up tracker, write Plans/swift-previewsign-parity.md retroactively
#      synthesizing the libfido2 + android findings (so the Step 1 deliverable still lands)
#    - Then /CodeAudit gate per §9.2 path-2B audit criteria
#    - Then /Ping user with results
#    - Then queue up 9.3 hardware verification (DO NOT START 9.3 without user physically present at YubiKey)

# 6. If user picks "dispatch 9.2 Step 1 first":
#    - Engineer prompt skeleton in §Phase 9.2 Step 1
#    - Engineer must STOP and Ping user with verdict (PROVEN/PARTIAL/DEFER) before any code change
#    - Most likely verdict given existing libfido2+android evidence: DEFER → routes to 2B
```

**Do not** rebase or fast-forward across the 11 branches individually — later commits supersede earlier choices.
**Do not** PR against `develop` or `yubikit` directly — `yubikit-applets` is the only valid target.
