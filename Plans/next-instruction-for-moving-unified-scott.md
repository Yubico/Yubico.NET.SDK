# Next Instruction — Phase 9.2 Path 2A (Port Rust Wire Fix)

**Date:** 2026-04-23
**Active branch:** `webauthn/phase-9.1-hygiene` (tip `5f7ab705`)
**Eventual merge target:** `yubikit-applets`
**Supersedes:** the "go to 2B" recommendation in `Plans/handoff.md` and the gating Step-1-then-2B structure in `Plans/yes-we-have-started-composed-horizon.md`. New evidence has flipped the verdict.

---

## Context

**Why this change is being made:**

The prior session closed Phase 9.1 (hygiene bundle, audit PASS-WITH-NOTES) and entered Phase 9.2 with three open user decisions. The recommended path was **2B — close the previewSign authentication surface as `[Experimental]` + `NotSupported`** because no upstream SDK had a hardware-tested authentication path:

| SDK | Verdict (as known to handoff) |
|---|---|
| yubikit-swift | code present, untested |
| libfido2 | none |
| yubikit-android | code present, registration-tested only |
| Yubico.NET.SDK (this port) | throws `CtapException: Invalid length (0x03)` |

The user introduced a fourth reference — `~/Code/y/cnh-authenticator-rs-extension` — to be checked at end-of-cycle. Pre-planning scan promoted it because the evidence was strong enough to flip the strategic choice:

| SDK | Verdict (updated) |
|---|---|
| yubikit-swift | code present, untested |
| libfido2 | none |
| yubikit-android | code present, registration-tested only |
| **cnh-authenticator-rs-extension** | **HARDWARE-PROVEN** — registration + authentication, signature returned and printed |
| Yubico.NET.SDK (this port) | wire-format bug, fix candidate identified |

The Rust binary `hid-test` (`native/crates/hid-test/src/main.rs:257-379`) calls a real YubiKey, derives an ARKG key, signs `"Hello, previewSign v4!"`, and **prints the resulting signature** at line 373. The CBOR wire format is documented at `native/deps/authenticator/src/ctap2/commands/get_assertion.rs:290-323` — an integer-keyed map with keys `2` (`key_handle`), `6` (`tbs`), `7` (`additional_args`, optional). This is the upstream reference the user-stated principle ("only ship what an upstream reference has proven works on hardware") was waiting for.

**Intended outcome:** Port the Rust wire format to fix the C# `Invalid length (0x03)` error, validate on hardware, and ship single-credential previewSign authentication unmarked. Multi-credential probe-selection (CTAP §10.2.1 step 7) remains deferred to Phase 10 because the Rust `hid-test` does single-credential only — multi-credential probe is not in our parity evidence base yet.

---

## Critical Correction From Handoff

The handoff cited the throw site at `PreviewSignAuthenticationInput.cs:58`. **Wrong.** Verified locations:

- **`PreviewSignAuthenticationInput.cs:32-94`** — `sealed record class`; constructor at `:55-94` validates non-empty dictionary only
- **`PreviewSignAdapter.cs:141-149`** — `BuildAuthenticationCbor(PreviewSignAuthenticationInput?, IReadOnlyList<PublicKeyCredentialDescriptor>)` is the **actual** `Count != 1` throw site for multi-credential
- **`PreviewSignAdapter.cs` (full file)** — also contains the CBOR encoder responsible for the `Invalid length (0x03)` failure on single-credential auth

The wire-format fix and the multi-credential throw live in the same file but are **separate** problems. The fix in this plan addresses the wire-format only.

---

## Recommended Approach (Path 2A — ordered)

### Step 1 — Commit the 4 uncommitted Plans/ files (atomic)

Stage and commit these explicitly (no `git add .`):

```
Plans/handoff.md
Plans/yes-we-have-started-composed-horizon.md
Plans/libfido2-previewsign-parity.md
Plans/yubikit-android-previewsign-parity.md
```

Single conventional-commit message:
```
docs(webauthn): land Phase 9 plan + libfido2/android parity reports + handoff
```

Done on `webauthn/phase-9.1-hygiene`. No new branch yet.

### Step 2 — Write `Plans/cnh-authenticator-rs-previewsign-parity.md`

Match the structure used by the existing two reports (header → Date/Investigated → Verdict → `## Findings` (Code paths, Hardware tests) → `## Citations`).

**Required content (all verbatim quotes with file:line):**
- Verdict: `HARDWARE-PROVEN (Registration + Authentication; hardware-tested registration + authentication via hid-test binary)`
- Wire format: integer-keyed CBOR map, keys 2/6/7, source `native/deps/authenticator/src/ctap2/commands/get_assertion.rs:290-323` (quote the encoder block)
- Hardware test evidence: `native/crates/hid-test/src/main.rs:257-294` (request build), `:330-331` (touch prompt), `:366-379` (signature receive + print)
- Crate metadata: `sign-extension-host` v0.1.0, last commit `c83cbce` 2026-04-09
- Constraints: `hid-test` exercises **single-credential** signByCredential only. Multi-credential probe NOT proven by Rust either.
- Cross-platform Python harness exists: `scripts/test_previewsign.py:131-138`

### Step 3 — Write `Plans/swift-previewsign-parity.md` (retroactive)

Closes the original Step 1 deliverable for the historical record. Same structure as the other parity reports. Verdict: `CODE-PRESENT-UNTESTED (release/1.3.0 has both registration + authentication code paths; PreviewSignTests.swift contains registration tests only)`. Cite the original diagnostic note at `PreviewSignTests.cs:107` as the source. Brief — one-page.

### Step 4 — Full rewrite of `Plans/yes-we-have-started-composed-horizon.md`

User chose "Full rewrite with new evidence model." Restructure around:

- **4-SDK parity matrix** (Swift / libfido2 / android / Rust / our port) replacing the current Step-1-gates-Step-2 narrative
- **Decision table** that explicitly shows: registration ships unmarked (3-of-5 hardware-proven), single-credential authentication ships unmarked once wire fix lands (1-of-5 hardware-proven, but the 1 has documented wire format), multi-credential probe defers to Phase 10 (0-of-5 hardware-proven)
- **Replace** old Step 1 (Swift investigation) with "Step 1 — closed, see four parity reports"
- **Replace** old Step 2A (port wire fix + probe) with "Step 2A — port wire fix only; probe stays in Phase 10"
- **Delete** old Step 2B (defer auth) — superseded by 2A
- **Keep** Step 9.3 (hardware verification) and Post-9 (Fido2 canonical extension assessment) substantively unchanged

### Step 5 — Branch and dispatch the wire-format fix

Create `webauthn/phase-9.2-rust-port` off the **post-Step-1 commit** (so the parity reports travel with the code work).

Engineer agent PRD (skeleton — orchestrator writes the full PRD at dispatch time):
- **Goal:** Port the Rust integer-keyed CBOR encoding for previewSign authentication into `PreviewSignAdapter.BuildAuthenticationCbor`. Eliminate `CtapException: Invalid length (0x03)` for single-credential signByCredential.
- **Scope IN:** wire-format fix at `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAdapter.cs`. Improve error message at `:141-149` to cite Phase 10 for multi-credential. Add deterministic unit test that asserts on the byte-level CBOR output matching the Rust reference.
- **Scope OUT:** multi-credential probe (Phase 10). Modifying registration code path. Touching the integration test runner config.
- **Inputs:**
  - Rust reference: `~/Code/y/cnh-authenticator-rs-extension/native/deps/authenticator/src/ctap2/commands/get_assertion.rs:290-323`
  - C# adapter to fix: `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAdapter.cs`
  - Existing PreviewSign constants split (Phase 9.1, 4 nested classes): `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignCbor.cs`
  - Failing integration test diagnostic: `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs:89-114`
  - Test cleanup helper to reuse: `WebAuthnTestHelpers.DeleteAllCredentialsForRpAsync`
- **Done means:**
  1. New unit test in `WebAuthn.UnitTests` asserts byte-for-byte equality between C# output and the Rust wire format (integer keys 2/6/7, correct CBOR length headers)
  2. `dotnet toolchain.cs -- test --project WebAuthn` passes 102+/0 (one new test)
  3. `dotnet toolchain.cs build` reports 0 warnings
  4. `Skip.If(true)` removed from `FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature`; replaced with `[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]` and a TODO comment pointing to Step 7 hardware verification
  5. Multi-credential throw at `PreviewSignAdapter.cs:141-149` rewritten to reference `Plans/phase-10-previewsign-auth.md` (file from Step 6)
- **Framework:** Apply the PAI Algorithm (`/Algorithm`) for structured execution with ISC.

### Step 6 — Create `Plans/phase-10-previewsign-auth.md` follow-up tracker

Captures the deferred multi-credential probe-selection work. Sections:
- **What ships in Phase 9.2:** single-credential auth (Rust-validated wire format)
- **What defers to Phase 10:** multi-credential probe per CTAP §10.2.1 step 7
- **Unblocking criteria:** hardware-proven multi-credential probe in any upstream SDK (Swift, libfido2, android, Rust); or Yubico statement; or RP-side use case demand
- **Suspected technical scope:** stub commented-out reference for the probe loop; cite the existing `signByCredential.Count != 1` throw site as the entry point
- **Owner:** TBD (Phase 10 lead)

### Step 7 — Audit gate (`/CodeAudit` then DevTeam)

Audit criteria for Step 5 deliverable:
- Wire-format unit test exists and passes; byte-level assertion matches Rust reference
- Integration test no longer carries `Skip.If(true)`
- Multi-credential throw cites Phase 10 tracker
- No log lines emit signature material, key handles in clear, or PIN bytes
- ZeroMemory called on any new temporary buffer holding `tbs` or signature output
- 0 build warnings
- All 4 parity reports + horizon rewrite committed

### Step 8 — Hardware verification (BLOCKED on user presence)

When you are physically present at the YubiKey 5.8.0-beta:
1. Plug in YubiKey
2. `dotnet toolchain.cs -- test --integration --project WebAuthn --filter "FullyQualifiedName~FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature"`
3. Touch when prompted (registration), touch again (authentication)
4. Assert: signature is returned (non-null, non-empty)
5. If pass: ship. If fail: `/Ping` me with diagnostic — likely indicates the Rust port missed an encoding nuance.

**Do not attempt this step without user presence.**

### Step 9 — PR prep

Branch: `webauthn/phase-9.2-rust-port` → PR target `yubikit-applets` (NOT `develop`, NOT `yubikit`). PR description must include:
- Link to all 4 parity reports
- Link to rewritten horizon doc
- Link to Phase 10 tracker
- Hardware verification screenshot/log
- Note that multi-credential probe is deferred (link tracker)

---

## Critical Files Annex

**Files to MODIFY:**
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAdapter.cs` — wire-format fix in `BuildAuthenticationCbor`; improve `:141-149` message
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs:87-114` — un-skip, add user-presence trait, TODO for Step 8
- `Plans/yes-we-have-started-composed-horizon.md` — full rewrite

**Files to CREATE:**
- `Plans/cnh-authenticator-rs-previewsign-parity.md`
- `Plans/swift-previewsign-parity.md`
- `Plans/phase-10-previewsign-auth.md`
- new unit test file in `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/PreviewSign/` asserting byte-level CBOR output

**Files to COMMIT (Step 1, no edits):**
- `Plans/handoff.md`
- `Plans/yes-we-have-started-composed-horizon.md` (the existing version; rewrite happens in Step 4)
- `Plans/libfido2-previewsign-parity.md`
- `Plans/yubikit-android-previewsign-parity.md`

**Files to REUSE (no edits):**
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/WebAuthnTestHelpers.cs:96-150` (`DeleteAllCredentialsForRpAsync`)
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignCbor.cs` (Phase 9.1 nested constants — `AuthenticationInputKeys` is the relevant scope)

**Reference files (READ-ONLY, outside repo):**
- `~/Code/y/cnh-authenticator-rs-extension/native/deps/authenticator/src/ctap2/commands/get_assertion.rs:290-323` — wire format ground truth
- `~/Code/y/cnh-authenticator-rs-extension/native/crates/hid-test/src/main.rs:257-379` — hardware test choreography
- `~/Code/y/cnh-authenticator-rs-extension/native/crates/host/src/webauthn.rs:420-457` — high-level GetAssertion previewSign parsing
- `~/Code/y/cnh-authenticator-rs-extension/scripts/test_previewsign.py:131-138` — Python cross-platform harness (secondary reference)

---

## Verification

End-to-end verification at completion of Step 7 (pre-hardware):

```bash
# 1. Build clean
dotnet toolchain.cs build                                              # expect 0 / 0

# 2. Unit tests (new wire-format test must be present)
dotnet toolchain.cs -- test --project WebAuthn                         # expect 102+/0
dotnet toolchain.cs -- test --project WebAuthn --filter "FullyQualifiedName~PreviewSign"   # spot-check the new test runs

# 3. Cross-module regression
dotnet toolchain.cs test                                               # all 10 projects pass

# 4. Plans integrity
ls -la Plans/cnh-authenticator-rs-previewsign-parity.md Plans/swift-previewsign-parity.md Plans/phase-10-previewsign-auth.md
diff <(grep -c '## Findings' Plans/libfido2-previewsign-parity.md) <(grep -c '## Findings' Plans/cnh-authenticator-rs-previewsign-parity.md)   # both = 1

# 5. Skip.If removed
grep -n "Skip.If(true" src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs   # expect: no match

# 6. Multi-credential throw cites Phase 10
grep -n "phase-10-previewsign-auth" src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAdapter.cs    # expect: 1+ match

# 7. Horizon doc rewritten (4-SDK matrix should be detectable)
grep -c "cnh-authenticator-rs" Plans/yes-we-have-started-composed-horizon.md    # expect: 1+

# 8. Git state clean
git status                                                             # expect: clean working tree on webauthn/phase-9.2-rust-port
git log --oneline yubikit-applets..HEAD                                # expect: 1 commit (Step 1) + 1+ commits (Steps 2–7) on top of 9.1 hygiene
```

End-to-end verification at completion of Step 8 (post-hardware):

```bash
dotnet toolchain.cs -- test --integration --project WebAuthn \
  --filter "FullyQualifiedName~FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature"
# expect: PASS with non-null signature returned, two touch prompts honored
```

---

## Risks (named, non-blocking)

Codebase is preview-stage; binary-compatibility / public-API stability is **not** a constraint. Breaking changes are acceptable.

1. **Rust `hid-test` may use ARKG key derivation that differs from C# port.** If signature comes back but doesn't verify against the public key, the encoding is right but the key-handle derivation is wrong. Mitigation: Step 8 hardware test asserts on signature returned (non-null, non-empty); cryptographic verification of the signature is a Phase 9.3 follow-up.
2. **Multi-credential probe is a Phase 10 obligation that isn't in any upstream SDK's hardware test.** Mitigation: Phase 10 tracker explicitly lists "no upstream proves this yet" as the unblock criterion. Free to break the throw shape later when probe lands.
3. **Rewriting the horizon doc loses the original deferral plan from history.** Mitigation: git history retains it; the rewrite is a documentation update, not a destructive operation.
