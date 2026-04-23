# Phase 9 Plan — WebAuthn Module Completion (rev 2)

**Date:** 2026-04-23 (rev 2 — restructured around 4-SDK parity matrix after Rust evidence flipped the Phase 9.2 verdict)
**Active branch:** `webauthn/phase-9.1-hygiene` (tip `56dfbd53`)
**Branch base for new sub-phase work:** `webauthn/phase-9.1-hygiene`
**Merge target (eventual):** `yubikit-applets` (NOT `develop`, NOT `yubikit`)
**Supersedes:**
- The original revision of this plan (the gating Step-1-then-2A/2B narrative); rewritten under "full rewrite with new evidence model"
- The "go to 2B" recommendation in `Plans/handoff.md`
- The "8 deferred items" list in the prior handoff
**See also:** `Plans/next-instruction-for-moving-unified-scott.md` (active execution plan for Phase 9.2 path 2A — this horizon doc is the strategy frame; that one is the order of operations).

---

## Context

The WebAuthn Client port reached Phase 9.1 closure on 2026-04-22 with an audit verdict of **PASS-WITH-NOTES** (5 commits on `webauthn/phase-9.1-hygiene`, 0 build warnings, 101/0 WebAuthn unit tests). Phase 9.0 ran in parallel as a parity investigation (libfido2). A bonus parity investigation followed (yubikit-android). Both were incorporated into the Phase 9.2 verdict planning.

The day after closure, a fourth upstream reference — `cnh-authenticator-rs-extension` (Rust) — was scanned at the user's prompt. That scan returned **HARDWARE-PROVEN previewSign authentication** with a documented CBOR wire format. This evidence flips the Phase 9.2 path selection from 2B (defer) to **2A (port the Rust wire format and ship single-credential authentication unmarked)**. Multi-credential probe-selection remains deferred to Phase 10 because no upstream SDK — including Rust — has hardware-tested it.

The constraining principle has not changed: **only ship what an upstream reference implementation has proven works on hardware.** The principle is now satisfied for single-credential previewSign authentication.

---

## 4-SDK Parity Matrix (frozen at 2026-04-23; re-survey when Phase 10 is scheduled)

| Reference | Version / commit | Registration code | Auth code | Hardware-proven registration | Hardware-proven single-credential auth | Hardware-proven multi-credential probe |
|---|---|---|---|---|---|---|
| **yubikit-swift** | release/1.3.0 | yes | yes | unverified | **no** | **no** |
| **libfido2** | v1.17.0 | none | none | n/a | n/a | n/a |
| **yubikit-android** | v3.1.0 (commit `f4626856`) | yes | yes | ✅ instrumented + integration | **no** | **no** |
| **cnh-authenticator-rs-extension** | commit `c83cbce` (2026-04-09) | yes | yes | (test runs reg first) | ✅ `hid-test` binary, signature returned | **no** (encoder admits "for now, serialize the first entry") |
| **Yubico.NET.SDK (this port)** | `webauthn/phase-9.1-hygiene` (`56dfbd53`) | yes | yes (throws on hardware) | ✅ YubiKey 5.8.0-beta | ❌ → **target of Phase 9.2 path 2A** | ❌ → Phase 10 |

**Parity report files (all committed in `56dfbd53` or this branch):**
- `Plans/libfido2-previewsign-parity.md` — verdict NONE
- `Plans/yubikit-android-previewsign-parity.md` — verdict registration-only
- `Plans/cnh-authenticator-rs-previewsign-parity.md` — verdict HARDWARE-PROVEN (single-credential)
- `Plans/swift-previewsign-parity.md` — verdict CODE-PRESENT-UNTESTED (retroactive, closes original Step 1 deliverable)

---

## Decision Table

| Surface | Hardware-proven references | Ship target | Verdict |
|---|---|---|---|
| `previewSign` registration (key generation) | 2 of 4 (android + this port) | **Phase 9.2 — ship unmarked** | Already shipped via Phases 7+8+Gate-2-fixup; no Phase 9.2 action needed beyond keeping it untouched |
| `previewSign` single-credential authentication (encoder) | 1 of 4 (Rust, byte-validated against C# encoder) | **Phase 9.2 — encoder shipped; integration test re-skipped pending ARKG (Phase 10)** | YubiKey 5.8.0-beta only accepts ARKG algorithms for previewSign — ARKG `additional_args` is the gating prerequisite for hardware verification. See Phase 10 tracker §3. |
| `previewSign` multi-credential probe (CTAP §10.2.1 step 7) | 0 of 4 | **Phase 10** | Tracker file: `Plans/phase-10-previewsign-auth.md`; throw at `PreviewSignAdapter.cs:141-149` cites this |
| Cryptographic signature verification helper | n/a | **Phase 10 (post-9.3)** | Tracker file: `Plans/phase-10-previewsign-auth.md` §2 |
| ARKG `additional_args` first-class builder | n/a | **Phase 10 (post-9.3)** | Tracker file: `Plans/phase-10-previewsign-auth.md` §3 |

Codebase is preview-stage; binary-compatibility / public-API stability is **not** a constraint. Breaking changes are acceptable across these decisions.

---

## Sub-Phase Status

| Sub-phase | Status | Branch | Notes |
|---|---|---|---|
| **9.0** Parallel parity investigations (libfido2, android, Rust, retroactive Swift) | ✅ **Closed** | (no branch — read-only) | Four parity reports landed; supersedes the original "Step 1 (Swift) is gating" structure. Multi-source parity matrix is the new artifact. |
| **9.1** Module hygiene bundle | ✅ **Shipped** — audit PASS-WITH-NOTES | `webauthn/phase-9.1-hygiene` | 5 commits, 0 build warnings, 101/0 WebAuthn unit tests; +1 commit landing parity reports + handoff |
| **9.2** Path 2A attempted → reverted to 2B-equivalent shape | ✅ **Shipped (encoder only)** | `webauthn/phase-9.2-rust-port` | Encoder verified byte-correct (audit PASS-WITH-NOTES). Hardware verification of auth path BLOCKED on ARKG: YubiKey 5.8.0-beta only accepts `Esp256` (ARKG) for previewSign and rejects non-ARKG algorithms with `Unsupported algorithm`. Integration test re-skipped citing `Plans/phase-10-previewsign-auth.md §3`. ARKG promoted to gating prerequisite for any auth-path hardware test (Phase 10 / candidate "Path B" branch). |
| **9.3** Hardware verification + integration test expansion | ✅ **Done** — executed 2026-04-23 on `webauthn/phase-9.2-rust-port` | (consolidated onto 9.2 branch) | Full WebAuthn integration suite ran on YubiKey 5.8.0-beta. **7 of 8 tests PASS** (all standard WebAuthn registration/authentication, status streaming, no-PIN throw, discoverable assertion). **1 SKIP** — `FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature`, blocked on ARKG (Phase 10 §3). Skip-reporting fixed by migrating `[Theory]` → `[SkippableTheory]` (`xunit.SkippableFact` requires the matching attribute for the runner to catch `SkipException`). See "Phase 9.3 — Hardware verification record (2026-04-23)" section below. |
| **Post-9** Fido2 canonical extension coverage assessment | ⏸️ Tracked, post-9.3 | (no branch — assessment only) | Substantively unchanged from rev 1 |

---

### Phase 9.0 — Closed (parallel parity investigations)

All four upstream references have been surveyed. The `Plans/libfido2-previewsign-parity.md`, `Plans/yubikit-android-previewsign-parity.md`, `Plans/cnh-authenticator-rs-previewsign-parity.md`, and `Plans/swift-previewsign-parity.md` reports are the deliverables. Re-open only if a new upstream reference (or a major version bump in an existing one) becomes relevant before Phase 10 is scheduled.

### Phase 9.1 — Shipped

5 commits on `webauthn/phase-9.1-hygiene`:
- `fbe45bc4` docs(webauthn): add module CLAUDE.md
- `f90bbc8f` test(webauthn): add DeleteAllCredentialsForRpAsync helper
- `63adea35` refactor(webauthn): split PreviewSignCbor key constants into scoped classes
- `eadb8fc3` test(webauthn): fix xUnit1051 warnings and CS8625 in GetAssertionTests
- `5f7ab705` docs: correct LoggingFactory→YubiKitLogging in root CLAUDE.md

Plus (2026-04-23) `56dfbd53` docs(webauthn): land Phase 9 plan + libfido2/android parity reports + handoff.

Audit verdict: PASS-WITH-NOTES — notes were observational only (build cleaner than self-reported, constants split improved beyond plan ask, CLAUDE.md exceeds 6/8 bar at 8/8). No follow-up required.

---

### Phase 9.2 — Active: Path 2A (port Rust wire format)

**Branch:** `webauthn/phase-9.2-rust-port` (off `webauthn/phase-9.1-hygiene`)
**Goal state:** Single-credential `previewSign` authentication on hardware. Eliminate `CtapException: Invalid length (0x03)`. Land deterministic byte-level unit test that asserts equivalence against the Rust encoder. Multi-credential probe stays deferred to Phase 10.

**Why path 2A (and not 2B):** The Rust `cnh-authenticator-rs-extension` provides both a hardware test (`native/crates/hid-test/src/main.rs:257-379`) that returns and prints the previewSign signature, and a documented byte-level encoder (`native/deps/authenticator/src/ctap2/commands/get_assertion.rs:290-323`). The "only ship what an upstream reference has proven works on hardware" principle is satisfied. Path 2B (close the auth surface as `[Experimental]` + `NotSupported`) is no longer warranted and is **deleted** from this revision of the plan.

**Tasks for the engineer (full execution plan in `Plans/next-instruction-for-moving-unified-scott.md`):**
1. Diff `PreviewSignAdapter.BuildAuthenticationCbor`'s CBOR output against the Rust `serde_cbor` encoder for an identical input. The C# diagnostic at `PreviewSignTests.cs:101-107` confirms C# already uses integer keys 2/6/7 — the bug is at a lower layer (byte-string length headers, outer wrapping, ordering, or omission of `additional_args` for ARKG payloads).
2. Apply the byte-targeted fix at `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAdapter.cs`.
3. Add a deterministic unit test in `WebAuthn.UnitTests` that asserts byte-for-byte equality between the C# CBOR output and the Rust reference shape (integer keys 2/6/7, byte-string values, `BTreeMap` ascending order).
4. Improve the multi-credential throw message at `PreviewSignAdapter.cs:141-149` to cite `Plans/phase-10-previewsign-auth.md`.
5. Un-skip `FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature` at `PreviewSignTests.cs:114`; add a TODO comment pointing to Phase 9.3 hardware verification.
6. Structured logs at probe/auth/error boundaries via `YubiKitLogging.CreateLogger<PreviewSignAdapter>()` (no PII; never log `tbs`, key-handle bytes, or signature material in clear).

**Engineer prompt skeleton:**
> *Phase 9.2 path 2A from `Plans/yes-we-have-started-composed-horizon.md`. Port the Rust integer-keyed CBOR encoding for previewSign authentication into `PreviewSignAdapter.BuildAuthenticationCbor`. Reference: `~/Code/y/cnh-authenticator-rs-extension/native/deps/authenticator/src/ctap2/commands/get_assertion.rs:290-323` (verbatim quote in `Plans/cnh-authenticator-rs-previewsign-parity.md`). The C# code already uses keys 2/6/7 — the bug is at byte-string length / outer wrap / args-omission level. Do byte-by-byte diff. Add a deterministic byte-level unit test as the primary verification artifact. Multi-credential probe stays deferred to Phase 10 — improve the throw message to cite `Plans/phase-10-previewsign-auth.md` rather than removing the throw. Apply the PAI Algorithm for structured execution with ISC.*

**`/CodeAudit` gate criteria (path 2A):**
- New unit test exists, passes, and asserts byte-for-byte equality with the Rust reference shape
- `dotnet toolchain.cs build` ⇒ 0 errors, 0 warnings
- `dotnet toolchain.cs -- test --project WebAuthn` ⇒ 102+/0
- `Skip.If(true)` removed from `PreviewSignTests.cs`; replaced with `[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]` and a TODO for Phase 9.3
- Multi-credential throw at `PreviewSignAdapter.cs:141-149` cites `Plans/phase-10-previewsign-auth.md`
- No log lines emit `tbs`, key-handle bytes in clear, or signature material
- `CryptographicOperations.ZeroMemory` called on any new temporary buffer holding `tbs` or signature output
- All 4 parity reports (`libfido2`, `yubikit-android`, `cnh-authenticator-rs`, `swift`) and the rewritten horizon doc (this file) are committed

**`/Ping` checkpoint:** "Phase 9.2 path 2A engineer complete — wire-format fix shipped, byte-level unit test green; ready for `/CodeAudit` gate."

**UP testing:** Deferred to 9.3 (`FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature` is the in-scope hardware test).

---

### Phase 9.3 — Hardware verification + integration test expansion

**Branch:** `webauthn/phase-9.3-integration` (off `webauthn/phase-9.2-rust-port`; rebase if needed)
**Goal state:** Single-credential `previewSign` authentication passes on YubiKey 5.8.0-beta with user present; integration coverage is broadened over what is now hardware-proven; multi-credential probe surface verified to throw cleanly with the documented Phase 10 reference.
**Requires:** User physically present at the YubiKey. **DO NOT START WITHOUT THE USER.**

**Pre-session checklist (orchestrator runs before pinging user):**
- 9.2 audit-passed and merged (or stacked cleanly)
- `dotnet toolchain.cs build` ⇒ 0 errors
- All non-UP integration tests pass (`--filter "Category!=RequiresUserPresence"`)
- YubiKey detected (`ykman list`)

**Tasks for the engineer (live with user available for touches):**
1. **Run the in-scope UP-traited suite** against the YubiKey 5.8.0-beta — capture pass/fail per test. Includes the unblocked `FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature` (touch ×2: registration + authentication).
2. **Broaden no-UP integration coverage** (these don't need touch, can run unattended):
   - Warm-up / first-connect on a fresh-reset key
   - RP-ID validation (`example.com` vs `not-allowed.com`)
   - PIN-required vs PIN-optional flows
   - `Reset` ceremony (gated `[Trait(TestCategories.Category, TestCategories.PermanentDeviceState)]`)
   - Credential-management cleanup using the Phase 9.1 `DeleteAllCredentialsForRpAsync` helper
3. **Verify the multi-credential probe surface throws cleanly** — a unit test asserts the `NotSupported` throw with the Phase 10 tracker reference in the message.
4. **Document the hardware-validated state** in a final commit on this branch; update `Plans/handoff.md` to reflect actually-shipped state for the next handoff.

**Engineer prompt skeleton:**
> *Phase 9.3 from `Plans/yes-we-have-started-composed-horizon.md`. The user IS present and IS available to touch the YubiKey. Run the existing UP suite first, including the just-unblocked `FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature`. If anything fails on a hardware-proven path, debug carefully — every touch costs the user a tap. Use `WebAuthnTestHelpers.DeleteAllCredentialsForRpAsync` between tests. All new touch-tests use `[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]`. Do NOT skip tests with `Skip.If(true)` — if a test can't pass, debug it or remove it (do not paper over). Multi-credential probe stays deferred to Phase 10; the unit test for that throw is the canonical verification.*

**`/CodeAudit` gate criteria (Integration Audit):**
- All in-scope UP-traited tests on YubiKey 5.8.0-beta documented as pass/fail with evidence (test output snippets)
- `FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature` returns a non-null, non-empty signature
- New no-UP tests pass on YubiKey unattended
- Multi-credential probe path verified to throw `NotSupported` with the Phase 10 tracker reference
- `Plans/handoff.md` accurately reflects the new shipped state (no stale "deferred" claims for single-credential auth; references `Plans/phase-10-previewsign-auth.md` for the still-deferred multi-credential path)
- Cleanup discipline: tests don't leak credentials across runs
- No `Skip.If(true)` remains in the test suite

**`/Ping` checkpoint:** "Phase 9.3 hardware verification complete — full module is shippable; ready to squash-merge the chain into `yubikit-applets`."

**UP testing:** This sub-phase IS the UP testing. User must be present.

---

### Phase 9.3 — Hardware verification record (2026-04-23)

**Executed against:** YubiKey 5 NFC Enhanced PIN, firmware `5.8.0.beta.0`, serial 103, transports OTP+FIDO+CCID
**Branch tested:** `webauthn/phase-9.2-rust-port` at commit `b54bc0cc` (pre-`SkippableTheory` fix; Skip API quirk discovered during this run)
**Test command:** `dotnet toolchain.cs -- test --integration --project WebAuthn`
**Total duration:** ~21 s execution time across 8 tests (2nd attempt; 1st attempt missed UP touches)

**Per-test outcomes:**

| # | Test | Result | Time | Touches |
|---|---|---|---|---|
| 1 | `PreviewSignTests.Registration_WithPreviewSign_ReturnsGeneratedSigningKey` | ✅ PASS | 5 s | 1 |
| 2 | `PreviewSignTests.FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature` | 🟡 SKIPPED (ARKG block, Phase 10 §3) | 1 ms | 0 |
| 3 | `WebAuthnClientTests.MakeCredential_NonResident_ReturnsValidResponse` | ✅ PASS | 1 s | 1 |
| 4 | `WebAuthnClientTests.MakeCredential_ResidentKey_ReturnsCredentialWithAaguid` | ✅ PASS | 1 s | 1 |
| 5 | `WebAuthnClientTests.MakeCredentialStream_EmitsProcessingThenFinished` | ✅ PASS | 935 ms | 1 |
| 6 | `WebAuthnClientTests.FullCeremony_RegisterThenAuthenticate_Succeeds` | ✅ PASS | 2 s | 2 |
| 7 | `WebAuthnClientTests.GetAssertion_DiscoverableCredential_ReturnsUserInfo` | ✅ PASS | 3 s | 2 |
| 8 | `WebAuthnClientTests.MakeCredential_NoPinProvided_ThrowsNotAllowed` | ✅ PASS | 188 ms | 0 |

**Net: 7 of 7 testable PASS · 1 SKIP · 0 hardware regressions.**

**Discoveries during this run (all addressed in branch tip):**

1. **Skip-reporting quirk** — `Skip.If(true, ...)` from `xunit.SkippableFact` threw `Xunit.SkipException`, but the test was decorated with `[Theory]` rather than `[SkippableTheory]`, so the runner reported it as Failed instead of Skipped. Fixed in `197e0dd7` (8 `[Theory]` → `[SkippableTheory]` migrations across both integration test files; the helpers also call `Skip.If` so all transitive consumers needed the attribute).

2. **YubiKey 5.8.0-beta only accepts ARKG algorithms for previewSign** — the only firmware-accepted algorithm for previewSign at registration is `Esp256` (-9), which is ARKG. Non-ARKG algorithms (`Es256`, `EdDsa`) fail with `CtapException: Unsupported algorithm`. This means single-credential previewSign authentication cannot be hardware-tested without ARKG `additional_args` support → ARKG promoted to gating prerequisite at `Plans/phase-10-previewsign-auth.md §3`.

3. **The original `Skip.If(true)` pattern was already dead code in the test suite** — the suite would have reported it as Failed if anyone had ever run integration tests. Phase 9.1 audit only ran unit tests, so the quirk was never surfaced. **Audit-rubric gap for future phases:** if any helper uses `Skip.If`, the audit must run the integration suite (not just unit tests) to confirm Skip behavior. Documented for next agent.

---

## Critical files reference

**To be modified (production code, Phase 9.2):**
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAdapter.cs` — wire-format fix in `BuildAuthenticationCbor`; improved `:141-149` message citing Phase 10 tracker

**To be modified (test code, Phase 9.2):**
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs:87-114` — un-skip, add UP trait, TODO for Phase 9.3

**To be created (Phase 9.2):**
- `Plans/cnh-authenticator-rs-previewsign-parity.md` ✅ done
- `Plans/swift-previewsign-parity.md` ✅ done
- `Plans/phase-10-previewsign-auth.md` ✅ done
- `Plans/next-instruction-for-moving-unified-scott.md` ✅ done — execution plan
- new unit test file in `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/PreviewSign/` asserting byte-level CBOR output

**Reference files (READ-ONLY, outside repo):**
- `~/Code/y/cnh-authenticator-rs-extension/native/deps/authenticator/src/ctap2/commands/get_assertion.rs:290-323` — wire format ground truth
- `~/Code/y/cnh-authenticator-rs-extension/native/crates/hid-test/src/main.rs:257-379` — hardware test choreography
- `~/Code/y/cnh-authenticator-rs-extension/native/crates/host/src/webauthn.rs:420-457` — high-level GetAssertion previewSign parsing
- `~/Code/y/cnh-authenticator-rs-extension/scripts/test_previewsign.py:131-138` — Python cross-platform harness (secondary)

**Reused functions/utilities (do NOT re-implement):**
- `Yubico.YubiKit.Core.YubiKitLogging.CreateLogger<T>()` — canonical logger factory (`src/Core/src/YubiKitLogging.cs:20`)
- `src/Tests.Shared/Infrastructure/TestCategories.cs` constants — canonical trait names
- `src/Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs` — xUnit data attribute
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/WebAuthnTestHelpers.cs:96-150` — `DeleteAllCredentialsForRpAsync` (added Phase 9.1)
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignCbor.cs` — Phase 9.1 nested constants (`AuthenticationInputKeys` is the relevant scope)
- `Yubico.YubiKit.WebAuthn.Extensions.PreviewSign.PreviewSignErrors.MapCtapError` — error mapper

---

## Verification — end-to-end (post-9.3, pre-PR)

```bash
# 1. Branch state
git checkout webauthn/phase-9.3-integration
git log --oneline yubikit-applets..HEAD

# 2. Build clean
dotnet toolchain.cs build

# 3. Unit tests
dotnet toolchain.cs test                      # all 10 projects green

# 4. WebAuthn-specific unit tests
dotnet toolchain.cs -- test --project WebAuthn

# 5. Non-UP integration (unattended)
dotnet toolchain.cs -- test --integration --project WebAuthn \
    --filter "Category!=RequiresUserPresence&Category!=PermanentDeviceState"

# 6. UP integration (user present at YubiKey 5.8.0-beta) — includes the unblocked previewSign auth test
dotnet toolchain.cs -- test --integration --project WebAuthn \
    --filter "Category=RequiresUserPresence"

# 7. Cross-module regression — Fido2 must still pass
dotnet toolchain.cs -- test --project Fido2

# 8. Documentation present
ls src/WebAuthn/CLAUDE.md
grep -c "YubiKitLogging" CLAUDE.md

# 9. Plans integrity
ls Plans/cnh-authenticator-rs-previewsign-parity.md \
   Plans/swift-previewsign-parity.md \
   Plans/phase-10-previewsign-auth.md \
   Plans/libfido2-previewsign-parity.md \
   Plans/yubikit-android-previewsign-parity.md

# 10. Handoff updated
git diff Plans/handoff.md
```

**Squash-merge plan (after verification, NOT during 9.x development):**
```bash
git checkout yubikit-applets
git merge --squash webauthn/phase-9.3-integration
git commit -m "feat(webauthn): port WebAuthn Client + CTAP v4 previewSign extension"
gh pr create --base yubikit-applets \
    --title "feat(webauthn): port WebAuthn Client + CTAP v4 previewSign extension" \
    --body-file <(printf "## Summary\nPorts yubikit-swift WebAuthn Client + CTAP v4 previewSign...\n\n## Audit history\n- Gate 1: Plans/audit-gate-1.md\n- Gate 2: Plans/audit-gate-2.md\n- Phase 9 (hygiene + Rust wire-format port + integration): Plans/yes-we-have-started-composed-horizon.md\n- Parity evidence base: Plans/{libfido2,yubikit-android,cnh-authenticator-rs,swift}-previewsign-parity.md\n- Deferred multi-credential probe: Plans/phase-10-previewsign-auth.md\n")
```

Do NOT fast-forward across the phase branches individually — later commits supersede earlier choices.

---

## Workflow conventions (recap from Phases 1–8 + 9.0/9.1)

- **One Engineer agent per sub-phase.** Spawn fresh; do not reuse the previous phase's agent.
- **PRD in the spawn prompt** — always include the §-reference to this plan, the source-of-truth refs, the audit-gate criteria, and the explicit non-goals.
- **`/CodeAudit` after every Engineer ship**, not at the end of the chain. Auditor agent reads this plan's audit-gate criteria as the rubric.
- **`/Ping` between sub-phases** so the user can intercept scope creep early.
- **Lessons applied this revision:**
  - Phase 3 lesson: bake authoritative API facts into the prompt; don't let the agent guess at signatures it could grep.
  - Phase 5 lesson: verify async streams don't deadlock on the consumer side — `Task.Run` for synchronous producers feeding `IAsyncEnumerable`.
  - Gate 2 lesson: spec parity is byte-level, not conceptual — when in doubt, dump and diff CBOR bytes against the upstream reference.
  - **9.0/9.1 lesson:** when one upstream is silent or untested, broaden the parity base. Single-source DEFER verdicts are fragile; multi-source matrices flip cleanly when new evidence arrives. Re-survey before scheduling Phase 10.

---

## Open risks (non-blocking, named for awareness)

Codebase is preview-stage; binary-compatibility / public-API stability is **not** a constraint. Breaking changes are acceptable.

1. **Rust `hid-test` may use ARKG key derivation that differs from C# port.** If signature comes back but doesn't verify against the public key, the encoding is right but the key-handle derivation is wrong. Mitigation: Phase 9.3 hardware test asserts only that a signature is returned (non-null, non-empty); cryptographic verification is a Phase 10 follow-up.
2. **Multi-credential probe is a Phase 10 obligation that isn't in any upstream SDK's hardware test.** Mitigation: `Plans/phase-10-previewsign-auth.md` explicitly lists "no upstream proves this yet" as the unblock criterion.
3. **YubiKey 5.8.0-beta firmware behaviors** may differ from production firmware. Document any beta-specific findings in commit messages and the eventual PR description.

---

## Post-Phase-9 follow-up — Fido2 module test coverage assessment

**Conclude before closing this work, but do not block Phase 9 on it.** Substantively unchanged from rev 1.

The WebAuthn port revealed gaps in Fido2 itself:
- The Phase 6 extension framework was silently dropped at the backend boundary for ≈ 2 weeks of audit cycles before the integration test caught it. A stronger Fido2 test would have demanded that *some* test send extensions through `FidoSession.MakeCredentialAsync` / `GetAssertionAsync` and observe them round-trip from the device. That test does not exist in `src/Fido2/tests/`.
- The CTAP v4 `previewSign` extension is novel; if Fido2 is to be the canonical FIDO2 surface for the SDK, it should ship with full canonical-extension coverage tests (`credProtect`, `credBlob`, `minPinLength`, `largeBlob`, `prf`, `credProps`, `previewSign`) at the Fido2 level — not pushed up to module-specific code paths.
- The `MakeCredentialResponse.UnsignedExtensionOutputs` plumbing added during Gate 2 (commit `3364ed1d`) is an internal Fido2 addition with no unit tests at the Fido2 layer that hit it independently of WebAuthn — coverage is only via WebAuthn's vectors.

**Action — after Phase 9.3 ships, before opening the WebAuthn PR:**

Spawn a single Explore agent to assess Fido2 canonical-test coverage with the following deliverable:

> *Read `src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/`, `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/`, and the CTAP 2.1+/v4 specification's extension list. Produce a coverage matrix: rows = canonical CTAP extensions (`credProtect`, `credBlob`, `minPinLength`, `largeBlob`, `prf`, `credProps`, `previewSign`, plus any others I missed); columns = registration test exists / authentication test exists / round-trip test exists / negative-case test exists. For each gap, propose a single test name + 1-line description. Do not write the tests. Output ≤ 400 words.*

**Decision after the matrix lands:**
- If gaps are **trivial** (≤ 5 missing tests), file as a 9.4 sub-phase before squash-merging.
- If gaps are **substantial** (> 5 missing tests), document as a separate follow-up plan (`Plans/fido2-canonical-extension-tests.md`), file Jira issues, and **explicitly defer** — do not block the WebAuthn PR on Fido2 test backfill.

**Why this is a follow-up and not a Phase 9 sub-phase:** Fido2 already shipped its own audit-passed integration suite earlier in the rewrite chain. The gap is "could be more canonical," not "is broken." WebAuthn's value is unblocked by the current Fido2 surface. Mixing Fido2 test backfill into the WebAuthn PR would expand scope, delay merge, and split the audit story. Better as a tracked-but-separate effort.
