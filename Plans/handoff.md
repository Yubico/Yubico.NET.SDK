# Handoff — Phase 9 closed, PR #466 open against `yubikit-applets`

**Date:** 2026-04-23
**Active branch:** `webauthn/phase-9.2-rust-port` (tip `5b67f7d6`, pushed)
**PR:** [Yubico/Yubico.NET.SDK#466](https://github.com/Yubico/Yubico.NET.SDK/pull/466) — `feat(webauthn): WebAuthn Client + previewSign extension (Phase 9 close)`
**Eventual merge target:** `yubikit-applets` (NOT `develop`, NOT `yubikit`, NOT `main`)
**Strategy frame:** [`Plans/yes-we-have-started-composed-horizon.md`](yes-we-have-started-composed-horizon.md) (rev 2)
**Execution log for this session:** [`Plans/next-instruction-for-moving-unified-scott.md`](next-instruction-for-moving-unified-scott.md) — all 9 steps complete
**Supersedes:** prior `Plans/handoff.md` from 2026-04-22 evening (which had 3 open decisions; all resolved)

---

## Critical next step (read first)

**No active blockers; PR is open and ready for review.** The next session should:

1. **Monitor PR #466 for review feedback** from Yubico maintainers — the PR body has the full audit + hardware-verification record, so reviewers should have everything they need
2. **If review surfaces fixable items**, address inline on the same branch and push (no rebase)
3. **Do NOT start Phase 10 work in this session** — Phase 10 (ARKG `additional_args` builder + multi-credential probe + signature verify) should be a fresh branch off `yubikit-applets` *after* PR #466 lands. The orchestrator should propose it as a candidate "Path B" branch when the user is ready.

The user explicitly said "save B for later, we can try that in a separate branch later" — Phase 10 is that later.

---

## Readiness Assessment

**Target user (inferred from `CLAUDE.md`, `src/Fido2/CLAUDE.md`, `src/WebAuthn/CLAUDE.md`):**
- .NET 10 application developers integrating YubiKey WebAuthn / passkey flows
- Browser/RP implementers building WebAuthn-spec-compliant clients on top of CTAP2
- Security-engineering teams requiring auditable, modern-C# crypto handling

| Capability | Status | Notes |
|---|---|---|
| WebAuthn data model (RP, User, Descriptor, COSE, AAGUID, preferences) | ✅ Working | Phase 1; 110+ unit tests |
| `clientDataJSON` + `AttestationObject` + `AuthenticatorData` | ✅ Working | Phase 2; byte-parity with Swift via hand-rolled JSON |
| `WebAuthnClient.MakeCredentialAsync` (terminal) | ✅ Working | Hardware-verified 2026-04-23 (5 tests covering resident/non-resident/stream/no-PIN/full-ceremony) |
| `WebAuthnClient.GetAssertionAsync` + `MatchedCredential.SelectAsync` | ✅ Working | Hardware-verified 2026-04-23 (`GetAssertion_DiscoverableCredential_ReturnsUserInfo` + `FullCeremony_RegisterThenAuthenticate_Succeeds`) |
| Status streaming (`IAsyncEnumerable<WebAuthnStatus>`) + interactive PIN/UV | ✅ Working | Hardware-verified 2026-04-23 (`MakeCredentialStream_EmitsProcessingThenFinished`) |
| Extension framework (CredProtect, CredBlob, MinPinLength, LargeBlob, PRF, CredProps) | ✅ Working | Phase 6 + commit `95abc0c5` (CRITICAL bugfix; extensions were silently dropped before) |
| `previewSign` **registration** (key generation) | ✅ Working | Hardware-verified 2026-04-23 (`Registration_WithPreviewSign_ReturnsGeneratedSigningKey`) on YubiKey 5.8.0-beta |
| `previewSign` **single-credential authentication** (encoder) | ✅ Encoder byte-correct | 4 deterministic byte-level unit tests vs Rust upstream reference (`PreviewSignCborEncodingTests`); end-to-end hardware verification deferred to Phase 10 (ARKG block — see below) |
| `previewSign` **single-credential authentication** (hardware ceremony) | ⚠️ Skipped | Test re-skipped via `Skip.If(true, ...)` — YubiKey 5.8.0-beta firmware accepts only `Esp256` (ARKG) for previewSign; ARKG `additional_args` not yet built. See `Plans/phase-10-previewsign-auth.md §3` |
| `previewSign` **multi-credential probe-selection** | ⚠️ Throws `NotSupported` | Throw at `PreviewSignAdapter.cs:141-150` cites `Plans/phase-10-previewsign-auth.md` |
| Module documentation (`src/WebAuthn/CLAUDE.md`) | ✅ Working | 8/8 sections; created in Phase 9.1 |
| Logging factory guidance | ✅ Working | Root `CLAUDE.md` corrected `LoggingFactory`→`YubiKitLogging` in Phase 9.1 |
| WebAuthn integration test project | ✅ Working | 8 tests; 7/7 testable PASS on YubiKey 5.8.0-beta; 1 SKIP (previewSign auth, ARKG-blocked) |
| `[SkippableTheory]` attribute compliance | ✅ Working | All 8 `[Theory]` decorations migrated to `[SkippableTheory]` so `xunit.SkippableFact` Skip API reports correctly |
| Test cleanup helper (`DeleteAllCredentialsForRpAsync`) | ✅ Working | Added Phase 9.1; exercised during 9.3 hardware sweep |
| Build state | ✅ Clean | `dotnet toolchain.cs build` → 0 errors, 0 warnings |
| Unit test state | ✅ Clean | `dotnet toolchain.cs -- test --project WebAuthn` → 104/0 |

**Overall readiness:** 🟢 **Production-ready for the spec-conformant subset.**

Single-credential authentication for non-ARKG previewSign and the entire standard WebAuthn surface ship. Beta-firmware-specific limitation (only ARKG accepted for previewSign) is documented and tracked. Multi-credential probe is explicitly not supported with a clear `NotSupported` throw + Phase 10 reference.

**Critical next step:** Land PR #466 (review + merge to `yubikit-applets`).

---

## Session summary (2026-04-23)

This session executed Phase 9.2 → 9.3 → PR end-to-end. Three significant strategic discoveries shifted the path versus the prior session's plan:

1. **Rust upstream evidence flipped Phase 9.2 from path 2B (defer) to path 2A (port the wire format).** The user proposed scanning `~/Code/y/cnh-authenticator-rs-extension` at end-of-cycle; an early scan returned **HARDWARE-PROVEN previewSign authentication** with documented integer-keyed CBOR wire format. Path 2A was adopted.

2. **The Rust port turned out to be a non-event** — the byte-level unit tests proved the C# encoder was already byte-correct against the Rust reference. The `Invalid length (0x03)` integration test failure was caused by an ARKG-vs-non-ARKG algorithm mismatch in the test, not by the encoder.

3. **Hardware verification revealed a deeper constraint** — YubiKey 5.8.0-beta firmware **only accepts `Esp256` (ARKG) for previewSign** at registration. Non-ARKG algorithms (`Es256`, `EdDsa`) fail with `Unsupported algorithm`. This means single-credential previewSign authentication cannot be hardware-tested without ARKG `additional_args` support. **ARKG was promoted from Phase 10 nice-to-have to Phase 10 gating prerequisite for any auth-path hardware verification.** Path 2A reverted to a 2B-equivalent ship: the audit-proven encoder lands; the integration test re-skips with the refined diagnostic.

4. **Bonus discovery during the hardware sweep:** the `xunit.SkippableFact` API requires `[SkippableTheory]` attribute decoration for the runner to catch `Xunit.SkipException`. Without it (8 `[Theory]` decorations), Skip.If(true) reports as Failed instead of Skipped. Surfaced because Phase 9.1 audit only ran unit tests, never the integration suite. Fixed in `197e0dd7`.

5. **Post-9 Fido2 coverage assessment came back favorably** — 4 minor unit-test polish gaps, no functional defects. Filed as `Plans/phase-9.4-fido2-extension-coverage.md` (deferred tracker, non-blocking).

---

## Branch state

```
yubikit-applets (merge target)
  └── ... 64 commits prior phases ...
      └── webauthn/gate-2-fixup (95abc0c5)
          └── webauthn/phase-9.1-hygiene (5f7ab705)
              └── webauthn/phase-9.2-rust-port (5b67f7d6) ← CURRENT, pushed, PR #466 open
```

**13 commits this session-chain on `webauthn/phase-9.2-rust-port`:**
```
5b67f7d6 docs(fido2): file Phase 9.4 tracker for canonical extension coverage polish
bd4e0fd2 docs(webauthn): record Phase 9.3 hardware verification results
197e0dd7 test(webauthn): use [SkippableTheory] so xunit.SkippableFact catches Skip.If
b54bc0cc test(webauthn): re-skip previewSign auth integration test — ARKG is gating prerequisite
b26ef2aa test(webauthn): drop validation-only previewSign test per repo test-philosophy
d6691747 fix(webauthn): enable previewSign auth test with non-ARKG algorithms
3983cc99 refactor(webauthn): update multi-credential NotSupported message to reference Phase 10
36c98688 test(webauthn): add byte-level CBOR encoding tests for previewSign authentication
0b6fa310 docs(webauthn): pivot Phase 9.2 to path 2A — Rust upstream is hardware-proven
56dfbd53 docs(webauthn): land Phase 9 plan + libfido2/android parity reports + handoff
```

(plus 5 Phase 9.1 commits + 2 gate-2-fixup commits below.)

---

## Build & test status (verified at handoff time)

| Check | Status |
|---|---|
| `dotnet toolchain.cs build` | **0 errors / 0 warnings** |
| `dotnet toolchain.cs test` | **All 10 projects pass** |
| `dotnet toolchain.cs -- test --project WebAuthn` | **104 / 0 / 0** |
| WebAuthn integration suite on YubiKey 5.8.0-beta (2026-04-23) | **7 / 0 / 1** (1 SKIP = previewSign auth, ARKG-blocked) |
| `git status` | Clean working tree on `webauthn/phase-9.2-rust-port` |
| Branch ↔ origin sync | Up to date with `origin/webauthn/phase-9.2-rust-port` |

---

## Open work (no active blockers)

| # | Item | Disposition | Owner | Path / Tracker |
|---|---|---|---|---|
| 1 | Land PR #466 — review + merge to `yubikit-applets` | Awaiting Yubico maintainer review | external | https://github.com/Yubico/Yubico.NET.SDK/pull/466 |
| 2 | Phase 10 — ARKG `additional_args` first-class builder | Deferred; gating prerequisite for any auth-path hardware test | TBD | `Plans/phase-10-previewsign-auth.md §3` |
| 3 | Phase 10 — multi-credential probe-selection (CTAP §10.2.1 step 7) | Deferred; no upstream SDK has hardware-tested it | TBD | `Plans/phase-10-previewsign-auth.md §1` |
| 4 | Phase 10 — cryptographic signature verification helper | Deferred; post-9.3 polish | TBD | `Plans/phase-10-previewsign-auth.md §2` |
| 5 | Phase 9.4 — 4 Fido2 unit-test polish gaps | Deferred; non-functional | TBD | `Plans/phase-9.4-fido2-extension-coverage.md` |

**No item in the above list blocks PR #466.** All deferred work has clear unblock criteria in its tracker.

---

## Parity evidence base (frozen 2026-04-23)

| SDK | Version / commit | Reg code | Auth code | HW-proven reg | HW-proven single-cred auth | HW-proven multi-cred probe |
|---|---|---|---|---|---|---|
| yubikit-swift | release/1.3.0 | yes | yes | unverified | no | no |
| libfido2 | v1.17.0 | none | none | n/a | n/a | n/a |
| yubikit-android | v3.1.0 | yes | yes | ✅ | no | no |
| **cnh-authenticator-rs-extension** | commit `c83cbce` | yes | yes | (test runs reg first) | ✅ (`hid-test` binary) | no |
| **Yubico.NET.SDK (this PR)** | `5b67f7d6` | yes | encoder byte-correct, throws on Count!=1 | ✅ YubiKey 5.8.0-beta | encoder verified vs Rust; HW pending ARKG | n/a (Phase 10) |

Reports:
- `Plans/libfido2-previewsign-parity.md`
- `Plans/yubikit-android-previewsign-parity.md`
- `Plans/cnh-authenticator-rs-previewsign-parity.md`
- `Plans/swift-previewsign-parity.md` (retroactive)

---

## Audit history

| Audit | Verdict | File |
|---|---|---|
| Gate 1 (after Phase 6) | 0 Critical / 4 High / 7 Med / 9 Low; all High + 6 Med fixed | `Plans/audit-gate-1.md` |
| Gate 2 (after Phase 8) | 3 Critical / 4 High / 5 Med / 4 Low; all Critical + High + 4 Med fixed | `Plans/audit-gate-2.md` |
| Phase 9.1 hygiene audit | PASS-WITH-NOTES (observational only) | (in conversation; not filed) |
| Phase 9.2 path 2A audit | PASS-WITH-NOTES (encoder byte-correctness independently verified vs Rust source; 5 observational findings, 1 actioned — validation-only test removed) | (in conversation; not filed) |
| Phase 9.3 hardware sweep | 7/7 testable PASS, 1 SKIP, 0 regressions | Recorded in `Plans/yes-we-have-started-composed-horizon.md` § "Phase 9.3 — Hardware verification record (2026-04-23)" |
| Post-9 Fido2 coverage assessment | 4 minor gaps, no functional defects → Phase 9.4 deferred tracker | `Plans/phase-9.4-fido2-extension-coverage.md` |

---

## Worktree / parallel-agent state

No active worktrees:
```
/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK 5b67f7d6 [webauthn/phase-9.2-rust-port]
```

All session agents have terminated cleanly:
- Engineer (Phase 9.2 wire-format fix) — completed; 3 commits landed
- general-purpose audit agent (Phase 9.2 path 2A audit gate) — completed; verdict PASS-WITH-NOTES
- Explore (Fido2 canonical coverage assessment) — completed; matrix returned, 4 gaps filed as Phase 9.4 tracker

No `.claude/worktrees/` directory exists.

---

## Quick start for fresh agent

```bash
# 1. Confirm branch + state
git checkout webauthn/phase-9.2-rust-port      # or: git fetch && git checkout webauthn/phase-9.2-rust-port
git log --oneline -10                          # confirm tip is 5b67f7d6
git status                                     # expect: clean

# 2. Verify build/test state
dotnet toolchain.cs build                      # expect 0 errors / 0 warnings
dotnet toolchain.cs -- test --project WebAuthn # expect 104/0

# 3. Read the strategy frame + parity reports IN ORDER
cat Plans/yes-we-have-started-composed-horizon.md    # rev 2 strategy doc — start here
cat Plans/phase-10-previewsign-auth.md               # deferred work tracker — most likely next destination
cat Plans/phase-9.4-fido2-extension-coverage.md      # smaller deferred tracker
cat Plans/cnh-authenticator-rs-previewsign-parity.md # the hardware-proven Rust upstream evidence
cat Plans/libfido2-previewsign-parity.md             # historical
cat Plans/yubikit-android-previewsign-parity.md      # historical
cat Plans/swift-previewsign-parity.md                # retroactive
cat Plans/next-instruction-for-moving-unified-scott.md  # this session's execution log (all 9 steps done)

# 4. Check PR status
gh pr view 466                                 # state, review status, any comments

# 5. If review feedback arrives → address inline; if user wants Phase 10 → see Phase 10 §3 (ARKG is the unblock)
```

**Do not** branch Phase 10 work off `webauthn/phase-9.2-rust-port` — branch off `yubikit-applets` after PR #466 lands.
**Do not** PR against `develop` or `yubikit` — `yubikit-applets` is the only valid target for the WebAuthn module work.
**Do not** rebase or fast-forward across the phase branches individually — the squash-merge happens at PR #466 land time.

---

## Open risks (non-blocking, named for awareness)

Codebase is preview-stage; binary-compatibility / public-API stability is **not** a constraint. Breaking changes are acceptable.

1. **Rust `hid-test` ARKG key derivation may differ from a future C# port.** When Phase 10 ARKG support is built, signature returned ≠ signature verified. Cryptographic verification is itself a Phase 10 §2 item.
2. **YubiKey 5.8.0-beta firmware behavior may differ from production firmware.** The "only ARKG algorithm accepted for previewSign" finding is beta-specific; flag for Yubico reviewers in PR #466 (already noted in PR description).
3. **PR review may surface scope-expansion requests.** If reviewers ask for ARKG support to be in this PR, the orchestrator should push back to Phase 10 — the parity evidence supports the encoder-only ship and Phase 10 has a clear path.

---

## Lessons captured (for future audit rubrics)

1. **If any helper uses `Skip.If` from `xunit.SkippableFact`, the audit must run the integration suite (not just unit tests) to confirm Skip behavior.** Phase 9.1 audit ran only unit tests; the `[Theory]` vs `[SkippableTheory]` mismatch was invisible until Phase 9.3 hardware sweep.
2. **Single-source DEFER verdicts are fragile; multi-source parity matrices flip cleanly when new evidence arrives.** Phase 9.0 originally surveyed only libfido2; expanding to libfido2 + android + Rust + Swift gave a verdict that survived the strategic-finding cross-check during the audit gate.
3. **Engineer surprise findings need an audit cross-check that asks "is the new framing snapshot-tautological or independently derived?"** The Phase 9.2 audit's strategic-finding cross-check confirmed the byte-level unit tests asserted CBOR-spec-derived bytes (`0xA2`, `0x58`), not snapshots of the C# encoder's own output.
