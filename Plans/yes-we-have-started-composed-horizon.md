# Phase 9 Plan — WebAuthn Module Completion

**Date:** 2026-04-22
**Branch base:** `webauthn/gate-2-fixup` (tip `95abc0c5`) — branch new sub-phase work off this.
**Merge target (eventual):** `yubikit-applets` (NOT `develop`, NOT `yubikit`)
**Plan supersedes:** `Plans/handoff.md` "Phase 9 deferrals" list (3 items in that list were inaccurate — see Preflight Findings).

---

## Context

The WebAuthn Client port is ship-ready for the spec-conformant single-credential subset, with Critical/High audit findings cleared by the two prior gates. Between Gate-2 closure and now, the integration test project was scaffolded and hardware-driven testing began against a YubiKey 5.8.0-beta — and that exercise surfaced **one new Critical wire-format bug** (`previewSign` authentication) plus **one previously-latent Phase 6 bug** (extension passthrough was silently dropped before the most recent commit). The handoff's "8 deferred items" list was written from the audit-gate perspective and is now stale: one item is already done, one item the audits couldn't see is now blocking, and several need re-scoping.

This plan addresses all *actually*-remaining Phase 9 work using the same split-phase rigor as Phases 1–8: engineer agent ships against a tight PRD → `/CodeAudit` gate → `/Ping` checkpoint to user → next sub-phase. Hardware-verification with user-presence touches is consolidated into the final sub-phase (run when the user is physically present at the YubiKey).

---

## Preflight Findings

### 1. The two new commits beyond the handoff tip
| Commit | Effect |
|---|---|
| `95abc0c5` | **Bugfix:** wires `options.Extensions = request.Extensions` in both MakeCredential/GetAssertion paths of `FidoSessionWebAuthnBackend`. Phase 6 had built the extension framework but the backend was discarding the encoded CBOR — `// opaque for Phase 3/4` placeholders never got replaced. **Also** makes `flags` optional in `PreviewSignAdapter` registration output decode (matches Swift `PreviewSign.swift:132–176` and observed YubiKey 5.8.0 behavior — firmware returns only key 3 / algorithm). |
| `97a502d5` | Docs only — clarifies `--project + --filter` CLI usage. |

**Architectural verdict on `95abc0c5`:** 🟢 **KEEP**. Both changes are correct bug fixes against the spec and Swift reference; the build/tests are green; no public-API change.

### 2. Integration-test architecture alignment
| Dimension | SDK convention | New WebAuthn project | Verdict |
|---|---|---|---|
| Project file (net10.0, IsTestProject, xUnit v2, Tests.Shared ref, xunit.runner.json with parallel disabled) | Standard | Matches | ✅ Aligned |
| Device discovery (`[Theory] [WithYubiKey(...)]` → `YubiKeyTestState` → lazy `.Device`) | Standard | Matches | ✅ Aligned |
| Trait categorisation (`[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]`) | Canonical from `src/Tests.Shared/Infrastructure/TestCategories.cs` | Used correctly in all 8 tests | ✅ Aligned |
| Global `<Using Include="Xunit"/>` in csproj | Present in peer test csprojs | **Missing** | ⚠️ Minor — fix in 9.1 |
| PIN normalization helper | Fido2 has `SetOrVerifyPinAsync`; WebAuthn has `NormalizePinAsync` (more defensive — handles `ForcePinChange`, `Skip`s on PIN mismatch instead of hard-failing) | Custom but appropriate for WebAuthn semantics | ✅ Aligned (intentional drift) |
| Credential cleanup helper | Fido2 has `DeleteAllCredentialsForRpAsync` for inter-test isolation | **Missing** — multi-test runs may leak credentials on the YubiKey | ⚠️ Minor — fix in 9.1 |
| Reset-app-on-test-start | PIV does it; Fido2 does NOT (FIDO2 isolation is per-RP) | Does NOT reset | ✅ Matches Fido2 (correct choice) |

**Architectural verdict on integration tests:** 🟢 **MOSTLY ALIGNED** — two minor gaps fixable inside the 9.1 hygiene bundle.

### 3. Phase 9 list corrections vs. the handoff
| Handoff item | Actual status | Disposition |
|---|---|---|
| #1 Multi-credential probe-selection | DEFERRED — `signByCredential.Count != 1` throws `NotSupported` at `PreviewSignAuthenticationInput.cs:58`; test at `PreviewSignAdapterTests.cs:260-287` | → Phase 9.2 |
| #2 `src/WebAuthn/CLAUDE.md` | DOES-NOT-EXIST | → Phase 9.1 |
| #3 Integration test project | PARTIAL — exists, 8 tests, all UP-traited; `FullCeremony_RegisterWithPreviewSign_ThenSign` SKIPPED due to **NEW BUG** (#9) | → Phase 9.3 |
| #4 Logging guidance is stale | CONFIRMED — actual factory is `YubiKitLogging.CreateLogger<T>()` (`src/Core/src/YubiKitLogging.cs:20`, used 9+ places). WebAuthn has zero `ILogger` calls. CLAUDE.md says `LoggingFactory` (does not exist) | → Phase 9.1 (CLAUDE.md fix) + 9.2 (add logs to probe + auth paths) |
| #5 Re-encode `AttestationObject` raw bytes | **DONE** — `WebAuthnAttestationObject.RawCbor` exists at line 49 | → Closed; remove from list |
| #6 L-2 split CBOR key constants (`Signature=6` vs `ToBeSigned=6`) | DEFERRED — both at `PreviewSignCbor.cs:46,48` | → Phase 9.1 |
| #7 xUnit1051 warnings (~12) | DEFERRED — `WebAuthnClientGetAssertionTests.cs` lines 182, 183, 216 + `WebAuthnStatusStreamTests.cs` lines 72, 142, 178, 189, 260, 318, 382, 395 | → Phase 9.1 |
| #8 CS8625 at `WebAuthnClientGetAssertionTests.cs:404` | DEFERRED — `AuthenticatorData = null` and `User = null` on non-nullable properties | → Phase 9.1 |
| **#9 (NEW) previewSign authentication wire format bug** | **CRITICAL** — `GetAssertion` w/ previewSign extension throws `CtapException: Invalid length (0x03)` *before* user-presence prompt. Documented at `PreviewSignTests.cs:89-114`. Hidden from both audit gates because no integration tests existed | → Phase 9.2 (debug + fix together with multi-cred probe; hardware-validate in 9.3) |

---

## Sub-Phase Breakdown

Three sub-phases under a constraining principle: **only ship what an upstream reference implementation has proven works on hardware.** The primary reference is `yubikit-swift` (the source of this port); a secondary reference (libfido2) is investigated in parallel to broaden the evidence base. Crucially, `yubikit-swift PreviewSignTests.swift` has NO authentication test — only registration (verified in `PreviewSignTests.cs:107`). Swift therefore has not demonstrated end-to-end previewSign auth + probe on hardware. Phase 9.2's auth-wire-format bug fix and multi-credential probe are **gated on a parity check**: if neither Swift nor libfido2 can be shown to support them on hardware, they get explicitly deferred and the C# surface is closed off cleanly.

Each sub-phase follows the **DevTeam Ship → CodeAudit → Ping** rhythm proven through Phases 1–8. Engineer-agent prompt skeletons below are intentionally short — the orchestrator (you, in the next session) fills in surgical details from this plan when spawning. Authoritative references (CTAP spec sections, Swift file:line) are listed inline.

---

### Phase 9.0 — Parallel investigation (kicks off at start, non-blocking)

**Dispatched as a single Explore agent at the very start of the next session, runs in the background while 9.1 hygiene work proceeds.** Results inform 9.2's verdict step but do not block 9.1 in any way.

**Goal:** Determine whether libfido2 (the C reference implementation maintained by Yubico) has implemented or tested previewSign authentication and the multi-credential probe path on hardware. Findings broaden the evidence base for the 9.2 parity verdict (Swift alone may be insufficient).

**Explore agent prompt skeleton:**
> *Read-only investigation. Look at the public libfido2 repository (https://github.com/Yubico/libfido2 — find latest release tag) for any code, tests, examples, regression scripts, or documentation that exercises the CTAP v4 `previewSign` extension on real YubiKey hardware. Specifically: (a) search for "previewSign" / "preview_sign" / "previewsign" string-insensitively; (b) search for the relevant CTAP CBOR keys / extension identifier; (c) read README, NEWS/CHANGELOG, regress/, examples/, fuzz/ directories; (d) check open issues + recent PRs for previewSign discussion. Output a 300-word report at `Plans/libfido2-previewsign-parity.md` with: which release(s) reference previewSign; what code paths exist (registration / authentication / both / neither); whether any test or example actually drives a hardware key through previewSign auth; and a 1-line verdict (PROVEN / PARTIAL / NONE / UNCLEAR). Do NOT clone the repo locally if avoidable — use GitHub web reads / search API. Do NOT block on this — return the report when ready.*

**Result handling:**
- The report lands at `Plans/libfido2-previewsign-parity.md`.
- 9.2's Step 1 (Swift parity investigation) explicitly reads this report as additional evidence.
- If libfido2 proves auth + probe on hardware AND Swift does not, that's still considered evidence of feasibility — the verdict becomes PROVEN, with libfido2 as the reference for the wire format instead of Swift.
- If both Swift and libfido2 are silent on auth, the DEFER path is reinforced.

**Workflow:** Pure background Explore agent. No `/CodeAudit`, no `/Ping` (the report itself is the deliverable; user reads it when 9.2 starts).

---

### Phase 9.1 — Module hygiene bundle

**Branch:** `webauthn/phase-9.1-hygiene` (off `webauthn/gate-2-fixup`)
**Goal state:** Module documentation, logging guidance, test-helper alignment, warning cleanup, and CBOR constants split — all in a single shippable commit set.
**Independence:** Fully independent of 9.2/9.3. Can ship in parallel.

**Tasks for the engineer:**
1. Create `src/WebAuthn/CLAUDE.md` — pattern after `src/Fido2/CLAUDE.md`. Include: module purpose, one-way Fido2 dep, test harness notes, why-no-LoggingFactory (link to `YubiKitLogging`), CTAP v4 previewSign extension reference.
2. Add `<Using Include="Xunit"/>` to `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/Yubico.YubiKit.WebAuthn.IntegrationTests.csproj`.
3. Add `DeleteAllCredentialsForRpAsync(FidoSession, string rpId)` to `WebAuthnTestHelpers.cs` — copy structure from `src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoTestHelpers.cs`, swap to WebAuthn types as needed; gracefully ignore "no credentials" error.
4. Fix `PreviewSignCbor.cs:46,48` — split into nested static classes `PreviewSignCbor.RegistrationKeys { ... }` and `PreviewSignCbor.AuthenticationKeys { ... }` so `Signature = 6` and `ToBeSigned = 6` are no longer in the same scope. Update all references inside `src/WebAuthn/src/Extensions/PreviewSign/` (do the work, don't add `using static` shortcuts).
5. xUnit1051 cleanup: replace `CancellationToken.None` / `default` with `TestContext.Current.CancellationToken` in: `WebAuthnClientGetAssertionTests.cs:182,183,216` and `WebAuthnStatusStreamTests.cs:72,142,178,189,260,318,382,395`.
6. CS8625 fix at `WebAuthnClientGetAssertionTests.cs:404` — restructure the test fixture so `AuthenticatorData` and `User` are not assigned to `null` against a non-nullable property. Prefer factory method or nullable backing field over `#pragma warning disable`.
7. Update root `CLAUDE.md` logging section: replace stale `LoggingFactory` references with `YubiKitLogging`, citing `src/Core/src/YubiKitLogging.cs:20` as canonical (or open a separate small commit if the change touches non-WebAuthn modules).

**Engineer prompt skeleton:**
> *Implement the 7 module-hygiene items in `Plans/yes-we-have-started-composed-horizon.md` §Phase 9.1. Each is independently scoped and trivial-to-small effort. Reference `src/Fido2/CLAUDE.md` for the module-doc template and `src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoTestHelpers.cs` for the cleanup-helper template. Do not change behavior of any production code beyond constants-split renames. Build must end with 0 errors and zero xUnit1051 warnings in the WebAuthn test projects. Ship as one commit per concern (7 commits) on branch `webauthn/phase-9.1-hygiene`.*

**`/CodeAudit` gate criteria (Hygiene Audit):**
- All 7 items addressed; no item silently dropped
- `dotnet toolchain.cs build` ⇒ 0 errors, no new warnings; xUnit1051 warnings in WebAuthn ⇒ 0 (or justified residue)
- `dotnet toolchain.cs test` ⇒ all WebAuthn unit tests still pass
- `src/WebAuthn/CLAUDE.md` covers ≥ 6 of: purpose, deps, test harness, logging factory, previewSign refs, CTAP version, security boundary, peer module pointer
- Constants split: zero usages remain of the unscoped `PreviewSignCbor.Signature`/`PreviewSignCbor.ToBeSigned` shorthand

**`/Ping` checkpoint:** "Phase 9.1 hygiene bundle ready for review — N small commits on `webauthn/phase-9.1-hygiene`."

**UP testing:** None required.

---

### Phase 9.2 — Swift-parity check + conditional auth/probe work

**Branch:** `webauthn/phase-9.2-swift-parity` (off `webauthn/gate-2-fixup`; rebase onto 9.1 if 9.1 lands first)
**Goal state:** Either (a) Swift is proven to support previewSign authentication + multi-credential probe on hardware, in which case we port the wire fix + probe; or (b) Swift cannot be shown to support those paths, in which case we explicitly close the C# auth surface with a clear `NotSupported` boundary and document the deferral.

**Step 1 — Parity investigation (gating step, do before any code change):**

The engineer agent's first task is a read-only investigation against upstream reference implementations to answer:
- **Does `yubikit-swift release/1.3.0` (or newer) include any test, example, or CI run that exercises previewSign **authentication** end-to-end on a real YubiKey?** Search `yubikit-swift/Tests/`, `Examples/`, release notes, and the `previewSign` PR/issue thread on GitHub.
- **Read the libfido2 parity report at `Plans/libfido2-previewsign-parity.md`** (produced by the parallel 9.0 Explore agent). Treat libfido2 as additional evidence of feasibility — if libfido2 has a working hardware-driven previewSign auth path, that counts toward PROVEN even if Swift doesn't.
- **Has Yubico published any blog post, release note, or known-good RP integration that demonstrates previewSign auth + signature verification on hardware?**
- **Is there a public Yubico statement about which firmware versions support previewSign auth, and whether 5.8.0-beta is in scope?**

Output: a short investigation report (engineer commits as `Plans/swift-previewsign-parity.md`) that synthesizes the Swift findings with the libfido2 report, with 1 of 3 verdicts:

| Verdict | Trigger | Path forward |
|---|---|---|
| **PROVEN** | Swift OR libfido2 has at least one passing hardware test for previewSign auth, or Yubico has demonstrated it elsewhere | Proceed to Step 2A — port the wire fix + probe (use whichever reference implementation is most complete as the byte-level source of truth) |
| **PARTIAL** | Swift and/or libfido2 have the code paths but no hardware test; Yubico has no public demo | Default: take the **DEFER** path. Optional override: user explicitly directs "ship registration-only with experimental auth flag" |
| **DEFER** | Neither Swift nor libfido2 has working code paths or only stubs; or the wire format is undocumented; or hardware support is firmware-version-gated outside our test key range | Proceed to Step 2B — close the auth surface |

**Step 2A — If PROVEN (and only if PROVEN):**
1. Wire-format investigation against Swift's known-good emitter — capture C# vs Swift CBOR bytes side-by-side; identify the divergence
2. Apply the byte-targeted fix in `PreviewSignCbor.EncodeAuthenticationInput` (or wherever the bytes diverge)
3. Implement multi-credential probe per CTAP v4 §10.2.1 step 7, mirroring Swift `Client+GetAssertion.swift:128-148` (allowCredentials iteration + dummy-credential fallback). Remove the `signByCredential.Count != 1` throw at `PreviewSignAuthenticationInput.cs:58`
4. Structured logs at probe/auth/error boundaries via `YubiKitLogging.CreateLogger<PreviewSignAdapter>()` (no PII)
5. Unit tests for the probe code path with mocked Fido2 layer (empty allowCredentials, single, multi, all-probes-fail, partial-success). Remove `Skip.If(true)` from `PreviewSignTests.cs` integration test so it runs in 9.3.

**Step 2B — If DEFER (or PARTIAL without override):**
1. **Keep the existing `signByCredential.Count != 1` throw** at `PreviewSignAuthenticationInput.cs:58` — but improve the message: explicitly cite "Swift parity not established — see Plans/swift-previewsign-parity.md"
2. **Mark the entire previewSign authentication entry point as `[Experimental("YK-PreviewSignAuth")]`** with a clear XML doc warning: "previewSign authentication on hardware is not yet validated in the upstream Swift SDK; this API path will throw `WebAuthnClientError(NotSupported)` until Yubico ships a reference implementation."
3. **Replace the SKIPPED integration test** `FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature` with a unit test that asserts the `NotSupported` throw — so the test suite stops carrying a `Skip.If(true)` (a code smell)
4. **Add `Plans/phase-10-previewsign-auth.md`** as a follow-up tracker — captures: what would unblock this work (Swift hardware test, Yubico statement, firmware bump), the suspected wire-format bug findings from the integration test diagnostic notes, and the multi-credential probe code as a stub commented-out reference
5. Do NOT remove the registration-side previewSign code — that path is fully ported, audited, and works on hardware (`Registration_WithPreviewSign_ReturnsGeneratedSigningKey` passes on YubiKey 5.8.0).

**Engineer prompt skeleton (covers both branches):**
> *Phase 9.2 from `Plans/yes-we-have-started-composed-horizon.md`. **Step 1 is a gating investigation — do that first, write the report at `Plans/swift-previewsign-parity.md`, then `/Ping` me with the verdict before writing any production code.** I will tell you to proceed with Step 2A or 2B based on your findings. Do not assume; the principle is "only ship what Swift has proven works." Reference materials: `yubikit-swift release/1.3.0` (search `Tests/`, `Examples/`, GitHub issues for "previewSign auth"), `PreviewSignTests.cs:89-114` (existing diagnostic notes), CTAP v4 §10.2.1.*

**`/CodeAudit` gate criteria — branches by verdict:**

*If 2A:*
- CTAP v4 §10.2.1 step 7 implemented end-to-end; no `Count != 1` throw remains
- Swift parity: side-by-side comparison documented; chosen fix has one-paragraph justification
- Logs at appropriate levels; zero sensitive-payload logs
- `CryptographicOperations.ZeroMemory` unchanged or expanded on PIN/UV/key buffers
- Unit tests cover: empty/single/multi allowCredentials, all-probes-fail, partial-success
- `dotnet toolchain.cs test --project WebAuthn` ⇒ all green
- `Plans/swift-previewsign-parity.md` exists with the PROVEN verdict and citations

*If 2B:*
- `Plans/swift-previewsign-parity.md` exists with the DEFER (or PARTIAL) verdict and reasoning
- The auth-path throw site has the improved message + plan reference
- `[Experimental(...)]` attribute present on the auth entry point with XML-doc warning
- `Skip.If(true)` removed from integration tests; replaced with a unit test asserting the `NotSupported` throw
- `Plans/phase-10-previewsign-auth.md` exists and captures the unblocking criteria
- Registration-side previewSign code is **untouched** (still works, still tested)

**`/Ping` checkpoint:** Two pings — one after Step 1 (verdict + recommendation), one after Step 2 (work complete).

**UP testing:** Step 2A only — deferred to 9.3. Step 2B requires no UP testing; the path is closed.

---

### Phase 9.3 — Hardware verification + integration test expansion

**Branch:** `webauthn/phase-9.3-integration` (off whichever of 9.1/9.2 lands last; rebase if needed)
**Goal state:** All Swift-supported integration tests pass on the YubiKey 5.8.0-beta with user present; integration coverage is broadened over what Swift demonstrably supports; deferred-auth boundary (if 9.2 took path 2B) is verified to throw cleanly.
**Requires:** User physically present at the YubiKey. **DO NOT START WITHOUT THE USER.**

**Scope branches based on 9.2 outcome:**

*If 9.2 took path 2A (PROVEN — auth + probe shipped):*
- Run all UP-traited tests including the unblocked `FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature`
- Add new UP-traited probe-path tests (multi-credential allowCredentials, discoverable, mixed valid/bogus)
- Iterate on any wire-format issues that surface — `/Ping` user before each touch retry

*If 9.2 took path 2B (DEFER — auth surface closed):*
- Run all UP-traited registration tests (the 6 existing `WebAuthnClientTests.cs` tests + `Registration_WithPreviewSign_ReturnsGeneratedSigningKey`)
- Verify the closed-auth path: a unit test asserts `WebAuthnClientError(NotSupported)` is thrown — no integration test attempts the deferred path
- Skip writing any new probe-path integration tests (probe is deferred too)

**Pre-session checklist (orchestrator runs before pinging user):**
- 9.1 + 9.2 both merged to a single integration branch (or stacked cleanly)
- `dotnet toolchain.cs build` ⇒ 0 errors
- All non-UP integration tests pass (`--filter "Category!=RequiresUserPresence"`)
- YubiKey detected (`ykman list`)

**Tasks for the engineer (live with user available for touches):**
1. **Run the in-scope UP-traited suite** (per 2A/2B branch above) against the YubiKey 5.8.0-beta — capture pass/fail per test
2. **Broaden no-UP integration coverage** (these don't need touch, can run unattended) — applies regardless of 2A/2B:
   - Warm-up / first-connect on a fresh-reset key
   - RP-ID validation (`example.com` vs `not-allowed.com`)
   - PIN-required vs PIN-optional flows
   - `Reset` ceremony (gated `[Trait(TestCategories.Category, TestCategories.PermanentDeviceState)]`)
   - Credential-management cleanup using the new `DeleteAllCredentialsForRpAsync` helper
3. **(2A only)** Add UP-traited multi-credential probe tests: both-valid-allowCredentials, discoverable-no-allowCredentials, valid-plus-bogus-fallthrough
4. **Document the hardware-validated state** in a final commit on this branch; update `Plans/handoff.md` to reflect actually-shipped state for the next handoff (and if 2B, link to `Plans/phase-10-previewsign-auth.md` as the follow-up tracker)

**Engineer prompt skeleton:**
> *Phase 9.3 from `Plans/yes-we-have-started-composed-horizon.md`. Read 9.2's outcome first (`Plans/swift-previewsign-parity.md`) — your scope branches on that. The user IS present and IS available to touch the YubiKey. Run the existing UP suite first; if anything fails on a Swift-supported path, debug carefully — every touch costs the user a tap. Use `WebAuthnTestHelpers.DeleteAllCredentialsForRpAsync` between tests. All new touch-tests use `[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]`. Do NOT skip tests with `Skip.If(true)` — if a test can't pass, debug it or remove it (do not paper over). The principle from 9.2 carries: only ship what Swift has proven works.*

**`/CodeAudit` gate criteria (Integration Audit):**
- All in-scope UP-traited tests on YubiKey 5.8.0-beta documented as pass/fail with evidence (test output snippets)
- New no-UP tests pass on YubiKey unattended
- *(2A only)* multi-credential probe tests pass on YubiKey 5.8.0-beta
- *(2B only)* deferred-auth path is verified to throw `NotSupported` cleanly with the documented message
- `Plans/handoff.md` accurately reflects the new shipped state (no stale "deferred" claims; if 2B, references `Plans/phase-10-previewsign-auth.md`)
- Cleanup discipline: tests don't leak credentials across runs
- No `Skip.If(true)` remains in the test suite

**`/Ping` checkpoint:** "Phase 9.3 hardware verification complete — full module is shippable [under path 2A/2B]; ready to squash-merge the 64+N commit chain into `yubikit-applets`."

**UP testing:** This sub-phase IS the UP testing. User must be present.

---

## Critical files reference

**To be created:**
- `src/WebAuthn/CLAUDE.md` (9.1)

**To be modified (production code):**
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignCbor.cs` (9.1 split, 9.2 probably wire-format fix)
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAuthenticationInput.cs:58` (9.2 remove `Count != 1` throw)
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAdapter.cs` (9.2 probe + logs)
- `src/WebAuthn/src/Client/FidoSessionWebAuthnBackend.cs` (9.2 may add probe-mode call site)
- Root `CLAUDE.md` (9.1 logging-factory correction)

**To be modified (test code):**
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/Yubico.YubiKit.WebAuthn.IntegrationTests.csproj` (9.1 add Xunit using)
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/WebAuthnTestHelpers.cs` (9.1 add `DeleteAllCredentialsForRpAsync`)
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs:89-114` (9.2 remove `Skip.If(true)`)
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/.../WebAuthnClientGetAssertionTests.cs:182,183,216,404` (9.1)
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/.../WebAuthnStatusStreamTests.cs:72,142,178,189,260,318,382,395` (9.1)

**Reused functions/utilities (do NOT re-implement):**
- `Yubico.YubiKit.Core.YubiKitLogging.CreateLogger<T>()` — canonical logger factory (`src/Core/src/YubiKitLogging.cs:20`)
- `src/Tests.Shared/Infrastructure/TestCategories.cs` constants — canonical trait names
- `src/Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs` — xUnit data attribute
- `src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoTestHelpers.cs` `DeleteAllCredentialsForRpAsync` — copy/adapt
- `Yubico.YubiKit.WebAuthn.Extensions.PreviewSign.PreviewSignErrors.MapCtapError` — error mapper (already wired in registration; reuse for probe)
- `Yubico.YubiKit.Fido2.Credentials.MakeCredentialResponse.UnsignedExtensionOutputs` (added Gate-2 C-2-prep) — for the auth-extensions read path
- `Yubico.YubiKit.Fido2.Attestation.AttestationStatement.RawData` (added Phase 2) — already exposed for Swift parity
- `Yubico.YubiKit.WebAuthn.Attestation.WebAuthnAttestationObject.RawCbor` — already done; do NOT re-implement (handoff M-5 was wrong)

---

## Verification — end-to-end

After all three sub-phases land, before opening the PR against `yubikit-applets`:

```bash
# 1. Branch state
git checkout webauthn/phase-9.3-integration  # or final integrated branch
git log --oneline yubikit-applets..HEAD       # should show original 64 + 9.1/9.2/9.3 commits

# 2. Build clean
dotnet toolchain.cs build                     # 0 errors, ideally 0 xUnit1051 in WebAuthn

# 3. Unit tests
dotnet toolchain.cs test                      # all 10 projects green, ~1246+ tests

# 4. WebAuthn-specific unit tests
dotnet toolchain.cs -- test --project WebAuthn

# 5. Non-UP integration (unattended)
dotnet toolchain.cs -- test --integration --project WebAuthn \
    --filter "Category!=RequiresUserPresence&Category!=PermanentDeviceState"

# 6. UP integration (user present at YubiKey 5.8.0-beta)
dotnet toolchain.cs -- test --integration --project WebAuthn \
    --filter "Category=RequiresUserPresence"

# 7. Cross-module regression — Fido2 must still pass (we touched MakeCredentialResponse internally back in Gate 2)
dotnet toolchain.cs -- test --project Fido2

# 8. Documentation present
ls src/WebAuthn/CLAUDE.md                     # exists
grep -c "YubiKitLogging" CLAUDE.md            # > 0; LoggingFactory references gone

# 9. Handoff updated
git diff Plans/handoff.md                     # reflects 9.1/9.2/9.3 shipped
```

**Squash-merge plan (after verification, NOT during 9.x development):**
```bash
git checkout yubikit-applets
git merge --squash webauthn/phase-9.3-integration
git commit -m "feat(webauthn): port WebAuthn Client + CTAP v4 previewSign extension"
gh pr create --base yubikit-applets \
    --title "feat(webauthn): port WebAuthn Client + CTAP v4 previewSign extension" \
    --body-file <(printf "## Summary\nPorts yubikit-swift WebAuthn Client + CTAP v4 previewSign...\n\n## Audit history\n- Gate 1: Plans/audit-gate-1.md\n- Gate 2: Plans/audit-gate-2.md\n- Phase 9 hygiene + probe + integration: Plans/yes-we-have-started-composed-horizon.md\n")
```

Do NOT fast-forward across the 10 phase branches individually — later commits supersede earlier choices.

---

## Workflow conventions (recap from Phases 1–8)

- **One Engineer agent per sub-phase.** Spawn fresh; do not reuse the previous phase's agent.
- **PRD in the spawn prompt** — always include the §-reference to this plan, the source-of-truth refs, the audit-gate criteria, and the explicit non-goals (e.g., "do not touch Fido2 unless 9.2 task 1 forces it").
- **`/CodeAudit` after every Engineer ship**, not at the end of the chain. Auditor agent reads this plan's audit-gate criteria as the rubric.
- **`/Ping` between sub-phases** so the user can intercept scope creep early.
- **Lessons from Phase 1–8 to apply here:**
  - Phase 3 lesson: bake authoritative API facts into the prompt; don't let the agent guess at signatures it could grep.
  - Phase 5 lesson: verify async streams don't deadlock on the consumer side — `Task.Run` for synchronous producers feeding `IAsyncEnumerable`.
  - Gate 2 lesson: spec parity is byte-level, not conceptual — when in doubt, dump and diff CBOR bytes against Swift.

---

## Open risks (non-blocking, named for awareness)

1. **9.2 verdict likely DEFER** — the existing diagnostic note (`PreviewSignTests.cs:107`) already states "yubikit-swift PreviewSignTests.swift has NO authentication test — only registration." If Step 1 of 9.2 confirms this, the principle "only ship what Swift has proven works" routes us to path 2B. Plan accordingly: probe + auth ship in a future Phase 10, not now.
2. **YubiKey 5.8.0-beta firmware behaviors** may differ from production firmware. Document any beta-specific findings in commit messages so the eventual PR description can flag them for Yubico reviewers.
3. **Closed-auth surface is a public-API commitment** (path 2B) — once `[Experimental]` ships and the `NotSupported` throw is documented, removing those in Phase 10 is a binary-compatible relaxation (good). Adding new throws later would not be (avoid). Mitigation: keep the throw site narrow (auth path only) and the message specific.

---

## Post-Phase-9 follow-up — Fido2 module test coverage assessment

**Conclude before closing this work, but do not block Phase 9 on it.**

The WebAuthn port revealed that **Fido2 itself has gaps** the WebAuthn integration tests partially expose:

- The Phase 6 extension framework was silently dropped at the backend boundary (extensions never made it onto the wire) for ≈ 2 weeks of audit cycles before the integration test caught it. The root cause was on the WebAuthn side, but a stronger Fido2 test would have demanded that *some* test send extensions through `FidoSession.MakeCredentialAsync` / `GetAssertionAsync` and observe them round-trip from the device. That test does not exist in `src/Fido2/tests/`.
- The CTAP v4 `previewSign` extension is novel; if Fido2 is to be the canonical FIDO2 surface for the SDK, it should ship with full canonical-extension coverage tests (`credProtect`, `credBlob`, `minPinLength`, `largeBlob`, `prf`, `credProps`, `previewSign`) at the Fido2 level — not pushed up to module-specific code paths like WebAuthn's adapter framework.
- The `MakeCredentialResponse.UnsignedExtensionOutputs` plumbing added during Gate 2 (commit `3364ed1d`) is an internal Fido2 addition with no unit tests at the Fido2 layer that hit it independently of WebAuthn — coverage is only via WebAuthn's vectors.

**Action — after Phase 9.3 ships, before opening the WebAuthn PR:**

Spawn a single Explore agent to assess Fido2 canonical-test coverage with the following deliverable:

> *Read `src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/`, `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/`, and the CTAP 2.1+/v4 specification's extension list. Produce a coverage matrix: rows = canonical CTAP extensions (`credProtect`, `credBlob`, `minPinLength`, `largeBlob`, `prf`, `credProps`, `previewSign`, plus any others I missed); columns = registration test exists / authentication test exists / round-trip test exists / negative-case test exists. For each gap, propose a single test name + 1-line description. Do not write the tests. Output ≤ 400 words.*

**Decision after the matrix lands:**
- If gaps are **trivial** (≤ 5 missing tests), file as a 9.4 sub-phase before squash-merging.
- If gaps are **substantial** (> 5 missing tests), document as a separate follow-up plan (`Plans/fido2-canonical-extension-tests.md`), file Jira issues, and **explicitly defer** — do not block the WebAuthn PR on Fido2 test backfill.

**Why this is a follow-up and not a Phase 9 sub-phase:** Fido2 already shipped its own audit-passed integration suite earlier in the rewrite chain. The gap is "could be more canonical," not "is broken." WebAuthn's value is unblocked by the current Fido2 surface. Mixing Fido2 test backfill into the WebAuthn PR would expand scope, delay merge, and split the audit story. Better as a tracked-but-separate effort.
