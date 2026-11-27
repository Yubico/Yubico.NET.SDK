# v2 security audit: close-out evidence review

Review date: 2026-10-30. Source snapshot: `yubikit` / `origin/yubikit` at `ddf3cdc2`.

**Status: in progress, batch 1 — scope reconciliation, five known exceptions, and CI/SCP/PIV residual triage. Not a release sign-off.**

**Implementation update (2026-10-31):** approved decisions are now being implemented in isolated branches, with red/green evidence, independent review and focused PRs. See [execution status](audit-v2-execution.md) for current commits, PRs, checks and residuals. The historical inspection statements below describe their review-time snapshots, not the later implementation branches.

## At a glance

| What needs attention | Why it matters | Current review position |
| --- | --- | --- |
| #18 / YESDK-1626 — PC/SC discovery leak | Scanning generic/NFC readers repeatedly loses native memory. | Reported allocation/cleanup gap remains in current code; keep open. |
| #21 / YESDK-1629 — short device-info fields | A valid TLV container can still contain an empty value that makes metadata decoding throw. | Unchecked field reads remain; keep open. |
| N4 — monthly migration workflow | The documentation generator still receives repository write authority alongside its Claude credential. | Architecture workflow hardened, migration workflow not split; incomplete. |
| #15 / YESDK-1623 — enterprise RP-ID override | An application can deliberately authorize an RP ID unrelated to its configured origin. | Intentional opt-in policy still needs an explicit acceptance/documentation decision. |
| #4 / YESDK-1612 — dropped unsigned assertion outputs | Results of an unsupported direct largeBlob mechanism are discarded by the response decoder. | Deferred spec gap, not a completed fix; tested YubiKeys did not produce it. |
| #17 / YESDK-1625 — private-key copies | SDK temporaries were fixed earlier; macOS runtime-created private-scalar copy remains outside SDK cleanup. | Separate completed SDK work from runtime residual/upstream ownership. |
| #22 / YESDK-1630 — shared PIV PIN state | Another handle can use card-wide PIN authorization when the SDK falls back to shared access. | Mitigated default, not unconditional isolation; compatibility risk needs acceptance. |
| #1 / YESDK-1609 — PRF UV policy | The same PRF input can derive different secrets depending on whether the user was verified. | Translation delivered; retained UV-policy gap needs a separate decision. |
| #19 / YESDK-1627 — CI binary trust | Locking the npm installer shim does not independently authenticate the vendor binary it downloads. | Least-privilege improvements delivered; residual vendor trust remains explicit. |

**Reading guide:** the detailed batch-1 sections explain the exceptions above. The **self-contained audit context** section later summarizes every original finding, grouped sub-item, follow-up and consideration, including items whose fix evidence has not yet been reviewed in this pass. “Recorded change” always means historical evidence, not fresh verification.

**Terms:** RP = relying party (the application/service using WebAuthn); RP ID = its credential scope/domain. CTAP = host-to-authenticator protocol. UV = user verification (PIN/biometric); UP = user presence (usually touch). PRF = authenticator-derived secret output. TLV = tag/length/value encoding. PC/SC = host smart-card API. PIV = smart-card identity/key applet. SCP = secure channel; R-MAC authenticates a response. largeBlob = encrypted per-credential blob storage. previewSign = extension providing a separate signing key. OATH = OTP credential applet; YubiOTP = the separate keyboard/challenge-response applet.

## How to read the references

- **Jira epic:** [YESDK-1608 — .NET SDK v2.0 Pre-Release Findings](https://yubico.atlassian.net/browse/YESDK-1608). The linked cases below belong to this epic directly or through its numeric-boundary/API-footgun umbrella cases.
- **Original #number:** a row in the original audit's Findings table, not a PR number. **25a–25i / 26a–26q** are labels the subsequent verification assigned to the bullets in grouped audit rows #25/#26.
- **N1–N4:** additional findings from the verification, not rows in the original audit. No dedicated Jira cases were identified for them in the reviewed evidence; related tickets are not substitutes.
- **PR #number:** GitHub pull request. “Related PR” means implementation/history context, not proof that every part of the finding was fixed.
- Full Jira/subtask-to-commit mapping: [traceability report](audit-v2-jira-traceability.md).
- Live ticket descriptions and actual parent links were retrieved for every Jira key referenced here. See [Jira body snapshot](#jira-body-snapshot-tickets-and-their-parents); the audit explanations below are **not** copied Jira bodies, because the v2 descriptions are empty.

## Agreed review rules

- Account for all original findings, ordered by risk rather than Jira priority alone.
- Use the original audit as the scope authority; distinguish original findings from follow-up observations and ordinary improvement suggestions.
- Review existing code, tests, PRs and recorded results. Record missing evidence as blockers; do not run new tests, reproduce old failures or interact with devices in this pass.
- Fixes and explicit accepted-risk dispositions can support close-out. The maintainer must approve each risk acceptance; historical approvals are evidence, not fresh acceptance in this review.
- Present proposed Jira comments/status transitions as a batch for approval before applying them. No Jira mutations have been authorized or performed.
- Vault documents remain untouched. Repository changes in this pass are research reports only.

## Interview decisions and pending choices

Initially recorded 2026-10-30 and amended during implementation on 2026-10-31. These decisions supersede earlier recommendations where they differ. Individual delivery descriptions below preserve the agreed scope; actual commits, PRs and verification outcomes are in [execution status](audit-v2-execution.md). Jira dispositions have not been applied.

| Item | Interview outcome | Delivery / verification |
| --- | --- | --- |
| #21 / YESDK-1629 | Fix all known device-info field reads using small explicit checked-read helpers; no generic rules framework. Confirm protocol-defined lengths, required fields and defaults; preserve unknown tags. | One focused PR, failing regression tests first, then implementation and valid/optional-field controls. |
| #18 / YESDK-1626 | Focused reader-name cleanup with the existing representation. Allocation failure frees earlier allocations; successful allocation transfers cleanup responsibility to a caller finally block. | One focused PR, TDD for successful and failed discovery plus partial allocation. |
| N4 | Split monthly migration generation from PR writing using the existing architecture workflow pattern. Keep the change simple. | One focused PR. Explicit exception to new-test requirement: workflow validation and diff review, no new test framework or tests. |
| #4 / YESDK-1612 | Pending sibling-SDK team clarification of supported/planned YubiKey producers of unsigned getAssertion key 0x08. Ask internally before escalating to firmware. | Draft message prepared with audit/Jira/PR/spec evidence; not sent. No implementation PR selected. |
| #15 / YESDK-1623 | Replace PublicSuffixChecker and EnterpriseRpIds with one required application-supplied RP validator. **Implementation-time refinement approved: callback owns RP domain/URI validity AND caller/RP authorization.** SDK enforces nonempty, faithfully UTF-8-encodable CTAP text and fails closed before I/O; no hidden browser/domain/URI parser policy. .NET URI/IDN helpers do not implement the complete referenced standards, and a larger syntax subsystem was explicitly rejected. Breaking API changes accepted. | One focused TDD PR. Include XML docs, trusted exact-RP/origin-RP allowlists and application-service examples, explicit syntax/binding responsibility, and migration guidance. Native-origin representation remains unchanged and documented. |
| #22 / YESDK-1630 | **A approved: strict exclusive PC/SC access by default.** Fail when exclusive access cannot be acquired; retain shared/fallback policies only as explicit opt-ins. Preserve power-off on release. This supersedes historical acceptance of fallback as the default. | One focused TDD PR. Prove default mode selection, sharing-violation failure without shared retry, and explicit opt-in behavior. Document coexistence/migration implications. Unit tests do not independently prove physical card isolation; assess existing hardware evidence separately. |
| #17 / YESDK-1625 | **Report the macOS runtime scalar-copy residual upstream using collected evidence.** Search existing dotnet/runtime GitHub issues first and follow its current CONTRIBUTING.md and applicable reporting guidance before filing. | Upstream reporting task approved; no SDK workaround selected. Preserve accurate historical versions/reproduction limits and check the existing issue draft. Avoid duplicate filing if an existing issue covers it. Issue not yet searched/filed in this interview step; no risk acceptance or Jira closure implied. |
| #19 / YESDK-1627 | **A approved: retain and accept vendor-distribution trust.** Keep pinned versions, lockfiles, update review and reduced generator permissions; document that the shim lockfile does not independently authenticate the downloaded vendor binary. No installer redesign, repository-pinned binary digest or internal artifact pipeline selected. | Record this specific residual acceptance in the proposed Jira batch after evidence review. It does not accept N4's unsplit migration workflow (separately approved for fixing) or the separate patch symlink/mode-validation follow-up. No Jira transition performed. |
| Documentation patch validation / #19–N4 follow-up | **A approved: reject symlinks and unexpected file-mode changes in generated documentation patches.** Preserve ordinary documentation edits and existing legitimate modes; validate before committing/publishing in patch-consuming jobs. No dedicated Jira case identified. | One focused PR with small deterministic normal-patch and symlink/mode-change fixtures; no workflow-testing framework. Keep distinct from N4's simple job-split PR, whose no-new-test exception remains applicable. |
| 26p / YESDK-1659 | **A confirmed: retain sibling-compatible challenge padding and document device semantics.** Empty-input parity fix already merged; do not reopen the settled design or introduce a mode parameter. | **Review last**, using existing local hardware measurements/tests before proposing Jira closure. Check existing documentation/examples before proposing new work. No new measurement or fix claimed. |
| Original consideration #1 — Management timeout documentation/limits | **A approved: treat the original documentation mismatch as addressed.** Retain AutoEjectTimeout 0–3600 seconds and ChallengeResponseTimeout 0–60 seconds. Current DeviceConfig documentation matches validation; existing tests cover above-limit rejection. | No additional implementation PR selected. This records documentation/validation consistency, not independent proof of firmware maximums. |
| Original consideration #2 — OpenPGP certificate absence vs read failure | **A approved: return null only for genuine certificate absence.** Confirm supported absence responses against the protocol and sibling SDKs; narrow the current broad ApduException catch and propagate other failures. Preserve the nullable API. | One focused TDD PR covering absence, successful certificate decoding and non-absence APDU failure. Do not invent absence status codes before reference inspection. |
| Original consideration #3 — FIDO HID synchronous configuration | **A approved: treat the original concern as addressed.** Configure no longer starts the wire handshake; InitializeAsync is awaited during session preparation. Existing Configure_DoesNotStartWireInitialization test asserts no initialization exchange. | No new fix PR or broader synchronous-wait review selected. Code/test inspected; no fresh test execution claimed. |
| Original consideration #4 — registration transport metadata | **A approved:** forward existing GetInfo transports into WebAuthn registration results, following Android. Preserve unknown strings via the existing extensible WebAuthnTransport type; rename Unknown to FromValue. Return an ordinal-sorted, unique snapshot, empty rather than null when unavailable. No active-connection guessing or extra GetInfo request. | One focused TDD PR spanning CTAP preservation tests and WebAuthn response plumbing, plus docs/migration. No CTAP session redesign or dependency on WebAuthn. Record sibling capability omissions below for later handoff; no upstream issue filed. |
| Original consideration #5 — partial certificate-bundle cleanup | **A approved (recorded 2026-10-31):** build the certificate list explicitly; dispose already-loaded certificates if a later load fails, then rethrow. On success, caller owns disposal of returned certificates. | One focused TDD PR covering partial-load cleanup and successful ownership transfer. Do not silently skip invalid certificates or return an incomplete bundle. |
| Original consideration #6 — factory-reset transport documentation | **A approved:** document that whole-device factory reset requires SmartCard/CCID in the Management feature table and public API XML docs, including NotSupportedException and a session-selection example. Existing ResetDeviceAsync already rejects other transports. | Small documentation-only PR; no new unit tests or capability API. |
| Original consideration #7 — OATH credential comparison | **Implementation-time decision supersedes A's display-first ordering:** user approved **identity-based natural ordering** (DeviceId, raw Id), with display sorting explicitly chosen by UI callers. Equal identities can carry different display metadata; display-first ordering cannot satisfy both identity equality and transitivity for all constructible states. | One focused TDD PR covering SortedSet identity preservation and equality/ordering laws, plus explicit display-sorting documentation/example. Agent demonstrated the original conflict with six failing tests before the revised decision. |
| v1/v2 release boundary / YESDK-1588 | **A approved:** assess and close out v2 findings against v2 code independently. Keep v1 exposure, backport and support/deprecation decisions separately tracked under YESDK-1588. | No automatic v1 closure, backport commitment or migration/deprecation policy implied. No SDK implementation PR for this scope decision. |

Interview progress checkpoint: **20 of 20 initially identified topics discussed** (100% initial interview coverage). PRF follow-up **A approved: coordinate with siblings before changing behavior**. Pending external answers: #4 supported-firmware clarification and #1 PRF UV policy. The final 26p existing-evidence check remains saved for last. This counts interview coverage, not fixes completed or Jira cases closed. New evidence may reveal additional decisions.

### Sibling handoff: missing registration transport metadata

Recorded capability gap, not a demonstrated security vulnerability; no upstream issue/message has been submitted.

| Implementation / inspected revision | Available metadata | Missing capability |
| --- | --- | --- |
| YubiKey Manager Rust `experiment/rust`, `d520107a63458bc35e49c56cc455cd9a726e4c73` | CTAP Info parses transports. | WebAuthn AuthenticatorAttestationResponse/RegistrationResponse have no transport field; registration does not forward metadata. |
| python-fido2 main, `9d5991d6785e20bd487c8b6c996df6ce2226642a` | CTAP Info exposes transports; callers can obtain them separately through client info. | Registration response omits transports; its AuthenticatorAttestationResponse has only client data and attestation object. |
| YubiKey Manager Python main, `4ca60f706af930459138d8dc0f0f953480e1c7a4` | Depends on python-fido2. | No independent WebAuthn registration implementation found in inspected Python sources. Route this response-model gap to python-fido2, not a duplicate manager-only defect. |
| YubiKit Android, `76b89d35142d9a4937b6b72e5c840e057a280620` | Cached CTAP GetInfo transports. | Already forwards them into registration attestation response; CTAP1 supplies an empty list. Reference precedent. |

Evidence:

- Rust response construction: https://github.com/Yubico/yubikey-manager/blob/d520107a63458bc35e49c56cc455cd9a726e4c73/crates/yubikit/src/webauthn/client.rs#L425-L434
- Rust response type: https://github.com/Yubico/yubikey-manager/blob/d520107a63458bc35e49c56cc455cd9a726e4c73/crates/yubikit/src/webauthn/types.rs#L590-L600
- Python Info: https://github.com/Yubico/python-fido2/blob/9d5991d6785e20bd487c8b6c996df6ce2226642a/fido2/ctap2/base.py#L95
- Python response construction: https://github.com/Yubico/python-fido2/blob/9d5991d6785e20bd487c8b6c996df6ce2226642a/fido2/client/__init__.py#L922-L932
- Python response type: https://github.com/Yubico/python-fido2/blob/9d5991d6785e20bd487c8b6c996df6ce2226642a/fido2/webauthn.py#L570-L573
- Android forwarding: https://github.com/Yubico/yubikit-android/blob/76b89d35142d9a4937b6b72e5c840e057a280620/fido/src/main/java/com/yubico/yubikit/fido/client/Ctap2Client.java#L240-L244
- CTAP2 GetInfo transports (optional, unknown values must be tolerated): https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#getinfo-transports
- WebAuthn response transport contract (unique, lexicographically ordered; empty if unavailable): https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#dom-authenticatorattestationresponse-gettransports

### Secret-lifecycle and PRF follow-up decisions

| Item | Interview outcome | Delivery / verification |
| --- | --- | --- |
| 26o / YESDK-1658 | **Ownership fix approved.** CredentialData copies incoming secret bytes into private owned storage; Secret exposes a borrowed ReadOnlyMemory<byte> view without allocating per read. Dispose clears private storage directly. PUT processing always returns independently owned temporary storage, never an alias to the credential seed. This supersedes the historical not-a-defect disposition. | One focused TDD PR: prove PUT→Locked→VALIDATE→PUT preserves the intended seed, input isolation, read-only view/disposal behavior, and padding/hash boundary cases. Update ParseUri, CLI/examples and tests to clear their original decoded/input arrays on all relevant paths; document borrowed-view lifetime and caller-owned copies. No implementation or new test execution yet. |
| #1 / YESDK-1609 PRF UV residual | **A approved, 2026-10-31: ask sibling SDK maintainers before changing behavior.** Retain current behavior while awaiting a coordinated decision; preserve the evidence/spec mismatch explicitly. This is pending coordination, **not acceptance of the conformance risk**. | Prepare/share the evidence bundle through the agreed team channel; sending has not occurred here. No implementation PR selected yet. Mandatory end-of-patch reconciliation below prevents this residual from being lost when other fixes land. |

### PRF / UV decision evidence — separate sibling comparison

Scope: WebAuthn PRF evaluation built on CTAP hmac-secret, not raw CTAP hmac-secret use. PASS/FAIL assesses the stated WebAuthn L3 requirement only. The failures are consequences of **one missing PRF-specific UV override**, not separate bugs. “PASS — mapping” establishes the translation exists, not exhaustive conformance of the implementation.

Source snapshots: .NET `ddf3cdc2`; YubiKey Manager **experiment/rust** `d520107a63458bc35e49c56cc455cd9a726e4c73`; local YubiKit Android `76b89d35142d9a4937b6b72e5c840e057a280620`. This is source inspection, not a new cross-SDK hardware run. Do not generalize to all sibling versions.

| Question | .NET v2 | Rust experiment/rust | YubiKit Android | Spec mandate / guidance | Assessment (.NET / Rust / Android) |
| --- | --- | --- | --- | --- | --- |
| Translate WebAuthn PRF to CTAP hmac-secret? | Yes | Yes | Yes | WebAuthn §10.1.4 defines the hmac-secret mapping; alternative underlying PRF mechanisms are permitted. | **PASS / PASS / PASS — mapping present** |
| Force UV when necessary for PRF evaluation? | No PRF-specific override | No PRF-specific override | No PRF-specific override | WebAuthn **MUST** use the verified PRF variant, overriding UserVerificationRequirement if necessary. | **FAIL / FAIL / FAIL** |
| Apply caller preference with the required PRF exception? | Ordinary UV selection | Ordinary UV selection | Ordinary UV selection | Caller preference must not prevent use of the verified PRF variant. | **FAIL / FAIL / FAIL** |
| Prevent WebAuthn PRF from exposing the unverified variant? | Non-UV path possible | Non-UV path possible | Non-UV path possible | CTAP §12.7 permits verified and unverified variants; WebAuthn §10.1.4 exposes the verified variant only when using hmac-secret. | **FAIL / FAIL / FAIL** |
| Expose one PRF per credential across changes in actual UV state? | Not guaranteed | Not guaranteed | Not guaranteed | WebAuthn exposes one PRF per credential; selection of the verified CTAP variant avoids a UV-dependent split. | **FAIL / FAIL / FAIL** |
| Avoid making the RP explicitly request Required to select the verified variant reliably? | Docs tell callers to request Required | No PRF-specific enforcement; example uses Discouraged | No PRF-specific enforcement | The client owns the necessary override, not the RP. Explicit Required can make an individual ceremony correct, but is not general implementation conformance. | **FAIL / FAIL / FAIL** |
| Meet the specific L3 PRF UV-selection requirement overall? | No | No in inspected paths | No in inspected paths | Verified-variant selection is a **MUST**, not a SHOULD. | **FAIL / FAIL / FAIL — one shared gap** |

**WebAuthn L3 §10.1.4, exact relevant text:**

> This extension only exposes a single PRF per credential and, when implementing on top of hmac-secret, that PRF MUST be the one used for when user verification is performed. This overrides the UserVerificationRequirement if necessary.

https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#prf-extension

**CTAP 2.3 §12.7:** defines the two per-credential values `CredRandomWithUV` and `CredRandomWithoutUV` underlying the two modes. This lower-level capability does not remove the WebAuthn-layer requirement above.

https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#sctn-hmac-secret-extension

Both specification documents returned HTTP 200 and both anchor IDs were checked in their HTML during this update.

**Implementation evidence:**

- .NET public guidance explicitly says the client does not override UV for PRF; `PrfProcessor.Permissions` is None and both ceremonies use the options' UV preference:
  https://github.com/Yubico/Yubico.NET.SDK/blob/ddf3cdc2/src/WebAuthn/src/Extensions/Inputs/PrfInput.cs#L33-L37
- Rust caller preference enters token selection:
  https://github.com/Yubico/yubikey-manager/blob/d520107a63458bc35e49c56cc455cd9a726e4c73/crates/yubikit/src/webauthn/client.rs#L457-L483
- Rust UV selection has ordinary device/permission requirements but no PRF-specific override:
  https://github.com/Yubico/yubikey-manager/blob/d520107a63458bc35e49c56cc455cd9a726e4c73/crates/yubikit/src/webauthn/client.rs#L1100-L1147
- Rust PRF processor does not add a UV requirement:
  https://github.com/Yubico/yubikey-manager/blob/d520107a63458bc35e49c56cc455cd9a726e4c73/crates/yubikit/src/webauthn/extensions/prf.rs#L363-L451
- Rust PRF example explicitly asks for Discouraged:
  https://github.com/Yubico/yubikey-manager/blob/d520107a63458bc35e49c56cc455cd9a726e4c73/crates/yubikit/examples/webauthn_prf.rs#L84-L109
- Android uses the request's UV preference after collecting extension permissions:
  https://github.com/Yubico/yubikit-android/blob/76b89d35142d9a4937b6b72e5c840e057a280620/fido/src/main/java/com/yubico/yubikit/fido/client/Ctap2Client.java#L647-L667
- Android hmac-secret processor constructs encrypted inputs and decrypted outputs without a PRF-specific UV override:
  https://github.com/Yubico/yubikit-android/blob/76b89d35142d9a4937b6b72e5c840e057a280620/fido/src/main/java/com/yubico/yubikit/fido/client/extensions/HmacSecretExtension.java#L186-L302

**Audit link:** the original #1 translation defect is tracked by https://yubico.atlassian.net/browse/YESDK-1609 and was addressed by PR #698. Treat this remaining UV-policy question separately from the already delivered translation.

### PRF follow-up A — evidence bundle and potential change

**Status:** pending sibling-team discussion. Decision recorded 2026-10-31. Maintainer coordinates the handoff; named recipient/response owner and message URL remain to be recorded. No message has been sent by this session. This follow-up must be revisited after the approved patches, even if every implementation PR is green.

**Question for siblings:** is preserving the caller's UV preference for WebAuthn PRF intentional, and should the SDKs coordinate enforcement of WebAuthn L3 §10.1.4's verified-variant requirement? Both Rust and Android inspected paths omit the override. Do not attribute motive from source behavior alone.

**Bundle to include:**

- Original audit, finding #1: https://yubico.atlassian.net/wiki/spaces/YES/pages/7660142593/.NET+SDK+v2.0+Pre-Release
- Jira translation finding / residual context: https://yubico.atlassian.net/browse/YESDK-1609
- Merged implementation and documented UV exception: https://github.com/Yubico/Yubico.NET.SDK/pull/698
- Historical measurement commit: https://github.com/Yubico/Yubico.NET.SDK/commit/725a2e78f930059483f3be3c03ae9572f5873b3b
- Existing hardware test: `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/AuditV2/WebAuthnAuditHardwareTests.cs`, `Prf_WithAndWithoutUserVerification_YieldsDifferentOutputs`. The recorded measurement used the same credential/input on the 5.8.0 development key and obtained different outputs with and without actual UV. This session did not rerun it; it is not a claim about every firmware/platform.
- Vault sources: `Projects/Yubico-SDK-Audit-v2/findings/01-prf-not-translated-to-hmac-secret.md`, `decisions.md`, `python-divergences.md`, and the recorded PR/commit measurement above. Keep original evidence when updating the eventual disposition.
- Separate .NET/Rust/Android comparison and pinned source URLs immediately above.
- Exact normative WebAuthn clause: https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#prf-extension
- CTAP's two underlying secrets/variants: https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#sctn-hmac-secret-extension

**If coordinated L3 alignment is chosen, the expected focused change is:**

1. Determine an effective UV requirement for applicable WebAuthn PRF evaluation before token acquisition/ceremony dispatch. When hmac-secret evaluation requires the verified variant, caller Discouraged/Preferred must not select the unverified PRF. Inspect authentication and registration-time hmac-secret-mc evaluation; do not blindly force a prompt for every enable-only/unsupported extension request.
2. Keep the caller's options object unmodified. Apply the effective requirement inside WebAuthn ceremony/extension orchestration (`PrfProcessor`, `WebAuthnClient.Registration`, `.Authentication`, `.Ceremony` as needed). Do not globally change raw CTAP hmac-secret behavior: both variants are legitimate at that lower layer.
3. Refuse evaluation rather than silently downgrade when required verification cannot be satisfied; confirm error mapping against the selected contract and sibling agreement. Preserve cancellation and avoid replaying successful credential creation merely to process output.
4. TDD: prove PRF evaluation with Discouraged/Preferred takes the verified path, explicit Required still works, unavailable verification cannot yield an unverified PRF result, and non-PRF ceremonies retain their caller policy. Add enable-only/unsupported and registration-evaluation controls as appropriate. Unit tests establish request policy; existing hardware measurements motivate the issue but do not prove new cross-platform behavior.
5. Update XML/usage docs and examples, replacing the current requirement that callers remember Required with the implemented policy. Document compatibility: an application that previously derived from the non-UV variant will receive a different secret after enforcement. Coordinate migration guidance; do not promise old encrypted data will decrypt with the new value.

This is a change outline for the pending decision, not authorization to implement an override now. If the team instead retains current semantics, an explicit accepted-risk/conformance disposition and owner/revisit criteria are still required; parity alone does not satisfy the MUST.

### Mandatory end-of-patch reconciliation

Before presenting the final Jira-update batch or calling the audit closed:

- [ ] **PRF / YESDK-1609:** record sibling message/link and response (or explicitly pending answer); decide coordinated fix vs explicit acceptance, with owner/revisit condition. Preserve the distinction between completed translation and remaining UV-policy gap. An unanswered question is not closure evidence.
- [ ] **Unsigned outputs / YESDK-1612:** record supported-YubiKey clarification and the resulting implementation/deferral boundary.
- [ ] **Runtime scalar copy / YESDK-1625:** record existing upstream issue or the new report filed under contribution/reporting guidance, plus remaining disposition.
- [ ] **Sibling transport metadata:** retain the Rust/python-fido2 handoff record; do not claim upstream changes have been submitted merely because .NET now exposes transports.
- [ ] **26p / YESDK-1659, last:** review preserved local challenge-padding measurements/tests and the already merged fix; do not reopen the settled padding design without contradictory evidence.
- [ ] Reconcile every approved PR against its issue and evidence, retaining outstanding blockers separately. Request approval for the exact Jira update batch; do not transition tickets from merge status alone.

### Correction: 26o / YESDK-1658 — password evidence did not cover credential seed

The vault classified this as not reproduced/no defect on October 21. That disposition was recorded in #698, but the reviewed counterexample invokes LIST and asserts that the caller's **password** remains intact. It never submits or retries an OTP credential seed. Do not treat it as disproving the original seed-loss claim.

Current .NET source trace:

1. `CredentialData.Secret` is the stored array (`src/Oath/src/CredentialData.cs:43`).
2. `HmacShortenKey` returns that array unchanged when it fits the hash block size (lines 225–228). `PadSecret` also returns it unchanged when it is at least 14 bytes (lines 244–249).
3. Consequently `GetProcessedSecret` returns the same array for 14–64-byte SHA-1/SHA-256 seeds, and 14–128-byte SHA-512 seeds (lines 260–273).
4. `OathSession.PutCredentialAsync` clears that returned array in its finally block (`OathSession.cs:234–268`), including when PUT fails with Locked. This clears the credential object's seed, not merely a temporary buffer.
5. `AuthenticateAndRetryAsync` authenticates and calls the operation again (lines 797–809). If the operation reuses that CredentialData object, its second PUT is built from the cleared seed. Successful first PUT also leaves that object's seed cleared.

**Existing tests:** `OathAuthenticationTests.AuthenticateAndRetryAsync_OperationLocked_AuthenticatesThenRetriesOnce` already covers successful authentication/retry, but its operation is a synthetic callback returning 99, not credential PUT. The AuditV2 counterexample checks password preservation on failed LIST/VALIDATE. Neither establishes seed preservation. The earlier recommendation that success-path testing was generally absent was too broad; the missing behavior is specifically PUT seed preservation across retry.

**Sibling source comparison (not fresh execution):**

| Implementation | Credential PUT ownership | Relevant source |
| --- | --- | --- |
| .NET `ddf3cdc2` | Ordinary-sized processed seed aliases the credential array; finally clears it. | `CredentialData.cs:225–273`, `OathSession.cs:227–269` |
| Android `76b89d35` | `CredentialData.getSecret()` returns `Arrays.copyOf`; putCredential clears the prepared copy, not the stored source. | `oath/.../CredentialData.java:200–203`; `HashAlgorithm.java:40–51`; `OathSession.java:575–620` |
| Rust `experiment/rust`, `d520107a` | put_credential borrows CredentialData; hmac_shorten_key copies or hashes into a separate Zeroizing vector before payload construction. | `crates/yubikit/src/oath.rs:350–363,394–423,755–771` |

Source links:

- https://github.com/Yubico/Yubico.NET.SDK/blob/ddf3cdc2/src/Oath/src/CredentialData.cs#L225-L273
- https://github.com/Yubico/Yubico.NET.SDK/blob/ddf3cdc2/src/Oath/src/OathSession.cs#L227-L269
- https://github.com/Yubico/yubikit-android/blob/76b89d35142d9a4937b6b72e5c840e057a280620/oath/src/main/java/com/yubico/yubikit/oath/CredentialData.java#L200-L203
- https://github.com/Yubico/yubikit-android/blob/76b89d35142d9a4937b6b72e5c840e057a280620/oath/src/main/java/com/yubico/yubikit/oath/OathSession.java#L575-L620
- https://github.com/Yubico/yubikey-manager/blob/d520107a63458bc35e49c56cc455cd9a726e4c73/crates/yubikit/src/oath.rs#L350-L363
- https://github.com/Yubico/yubikey-manager/blob/d520107a63458bc35e49c56cc455cd9a726e4c73/crates/yubikit/src/oath.rs#L755-L771

**Revised recommendation:** keep YESDK-1658 open. Prove the actual PUT/retry path with a regression test before changing code. Do not remove authentication-key zeroing; fix the processed-seed ownership boundary instead. No runtime reproduction or fresh test result is claimed by this source review.

## Original scope authority and counting

Vault project: `/home/rupicola/Documents/Sunthings_AppStorage_EU_e2e/Projects/Yubico-SDK-Audit-v2/`.

Primary sources read: `original-audit.md`, `README.md`, `decisions.md`, `verification-report.md`, `close-out-summary.md`, the relevant finding pages, and the archived final-candidate verification manifest. The original audit names audited commits `55e02064` and `65e36386`.

| Scope | Treatment |
| --- | --- |
| Original findings #1–#28 | Mandatory accounting. #25 and #26 are umbrella rows containing nine and 17 sub-items respectively. |
| Jira records | 28 direct children of YESDK-1608 plus 26 subtasks = 54 records, **not 54 independent findings**. |
| Expanded original report | Replacing the two umbrella rows with their sub-items gives 52 occurrences before overlap reconciliation. Do not count umbrellas again. |
| Related/overlapping findings | #20 and 25e share the TLV length fix; #28 and 25i share OATH period handling; 25h and 26q have related KDF defects addressed together. Retain all Jira mappings without describing them as independent remediation projects. |
| N1–N4 | Four additional follow-up entries from the earlier verification, not original numbered findings. Account separately; N2/N3 themselves contain multiple observations. |
| Seven “Items to Consider” | Separate disposition/backlog review. The original explicitly says these are not necessarily security findings; none is automatically accepted or closed. |
| [YESDK-1588 — Tracking the triaged findings of the bug hunting initiative](https://yubico.atlassian.net/browse/YESDK-1588) / prior v1 findings | Original report explicitly flags a v1 supersession/deprecation dependency. Record as a release-scope decision; a v2 fix does not establish a v1 fix or deprecation policy. Live body is reproduced in the Jira snapshot below. |

The prior verification deliberately omitted original #15, #18, #21 and the seven considerations. #20 received incidental coverage with 25e. Those historical exclusions are **not approval to exclude them from this close-out**.

Merge status differs from the vault's October 22 snapshot: #698, #707, #708, #709, #710 and #711 are merged and their merge commits are ancestors of the inspected trunk. See [traceability report](audit-v2-jira-traceability.md). Archived pre-merge results do not prove the integrated trunk was tested.

## Batch 1 findings and recommendations

### Original #18 / YESDK-1626 — reader-name native allocations: keep open

| Context | Reference |
| --- | --- |
| Audit headline | **Unmanaged-memory leak** — original finding #18, LOW |
| Jira case | [YESDK-1626 — unmanaged-memory leak](https://yubico.atlassian.net/browse/YESDK-1626) |
| Jira body / actual parent | **No description.** Parent: [YESDK-1608](https://yubico.atlassian.net/browse/YESDK-1608), whose description contains only whitespace. |
| Codebase context | **Core → PC/SC discovery/native interop.** Repeated discovery allocates reader-name strings for generic/NFC readers without freeing them. |
| Fix/PR context | No specific remediation found in the merged audit PRs; prior verification explicitly excluded #18. |

**Vault context — what happens:** each scan asks PC/SC for reader names. Integrated USB YubiKey readers take a shortcut, but generic/NFC readers need a native reader-state array. Constructing that array allocates one unmanaged string per reader. The code releases the PC/SC context, not those strings, and the garbage collector cannot free raw native pointers. A small number of scans leaks little; a long-running process repeatedly scanning such readers accumulates the loss. This is a resource-lifetime issue, not demonstrated key disclosure. Source: original audit #18; no dedicated investigation note exists.

**Evidence supports an unresolved code defect, not just a missing Jira mention.**

- `src/Core/src/Native/Desktop/SCard/SCardReaderStates.cs:38-43` allocates native reader-name strings with `Marshal.StringToHGlobalAnsi`.
- `src/Core/src/Transports/SmartCard/FindPcscDevices.cs:105-117` creates these states for generic/NFC readers and passes them to PC/SC.
- Its cleanup at lines 135–138 disposes the context, but never frees the reader-name pointers on success or error.
- The separate listener demonstrates explicit matching cleanup: `DesktopSmartCardDeviceListener.cs:349` calls `Marshal.FreeHGlobal(state.ReaderName)`. A context's disposal is not the ownership release for these independent string allocations.

Impact matches the original LOW finding: repeated successful scans involving generic/NFC readers leak native reader-name allocations. No new runtime measurement or leak test was performed.

**Recommendation:** do not close as fixed. Record remediation/verification as a blocker unless the maintainer explicitly accepts the risk with an owner and revisit condition. No acceptance requested or granted yet.

### Original #21 / YESDK-1629 — device-info TLV lengths: keep open

| Context | Reference |
| --- | --- |
| Audit headline | **Device-info decoding indexes empty or undersized TLV values without validation** — original finding #21, LOW |
| Jira case | [YESDK-1629 — Device-info decoding indexes empty or undersized TLV values without validation](https://yubico.atlassian.net/browse/YESDK-1629) |
| Jira body / actual parent | **No description.** Parent: [YESDK-1608](https://yubico.atlassian.net/browse/YESDK-1608), whose description contains only whitespace. |
| Codebase context | **Core → device metadata decoding.** Structurally valid TLVs can contain values too short for the decoder's indexing and numeric reads. |
| Fix/PR context | No specific remediation found; prior verification excluded #21. The pagination fix (25b / [YESDK-1639](https://yubico.atlassian.net/browse/YESDK-1639)) addresses a different defect. |

**Vault context — concrete example:** a device-info page `02 0A 00` says “two TLV bytes follow”: tag `0A` (configuration locked), length zero. The page length and TLV structure are valid, but the metadata decoder then reads the first byte of the empty value and throws. Similar issues affect short firmware/version, flag and timeout values. The distinction is important: fixing malformed TLV *headers* (#20/25e) does not validate each field's expected *value length*. Impact is failed metadata decoding from malformed device input, not an established memory-corruption exploit. Source: original audit #21; no dedicated investigation note exists.

**Evidence supports an unresolved parser defect.**

- `src/Core/src/Devices/DeviceInfo.cs:79-80` indexes config-locked and form-factor spans without length checks.
- Other examples remain at lines 93–96, 102–104, 107–128 (optional flags, multi-byte timeouts, firmware fields).
- `src/Core/src/Utilities/MemoryExtensions.cs:24-25` directly returns the dictionary value's span; it does not validate its length.
- Thus a present but empty config-locked value reaches `[0]` without a guard, exactly the original report's claim. Structural TLV validation and the separately fixed pagination loop do not establish semantic field-length validation.

No new malformed-device test was run. Static review establishes that the reported unchecked operations remain; it does not establish the full public-call exception propagation on every transport.

**Recommendation:** do not close as fixed. Capture missing malformed-value regression evidence and remediation as blockers, or obtain explicit risk acceptance.

### Original #15 / YESDK-1623 — RP-ID policy override: decision needed

| Context | Reference |
| --- | --- |
| Audit headline | **Nonstandard RP-ID policy override** — original finding #15, LOW |
| Jira case | [YESDK-1623 — Nonstandard RP-ID policy override](https://yubico.atlassian.net/browse/YESDK-1623) |
| Jira body / actual parent | **No description.** Parent: [YESDK-1608](https://yubico.atlassian.net/browse/YESDK-1608), whose description contains only whitespace. |
| Codebase context | **WebAuthn → origin/RP-ID validation.** An application-supplied enterprise allowlist permits an RP ID that does not match the caller's origin/suffix. |
| Fix/PR context | No specific remediation found; prior verification excluded #15. Current code and tests retain the override as intentional policy. |

**Vault context — example and boundary:** an application configured for origin `https://login.example` can allow-list an unrelated RP ID such as `partner.test`. The normal origin equality/domain-suffix checks fail, but membership in `EnterpriseRpIds` makes validation succeed. This is a locally configured native-client override, not WebAuthn's `/.well-known/webauthn` related-origin validation. The configured origin still goes into signed client data, and the RP must validate that origin. Consequently the original audit rates this LOW and demonstrates no direct exploit; the decision is whether this policy is intentional, adequately documented and appropriate for the SDK's supported clients. Source: original audit #15; no dedicated investigation note exists.

**The deliberate override still exists; this is not a fixed-finding candidate.**

- `src/WebAuthn/src/Client/Validation/RpIdValidator.cs:82-85` returns success for an enterprise allow-listed RP ID after normal origin/suffix matching has failed.
- `src/WebAuthn/src/Client/WebAuthnClientOptions.cs:86-90` documents this bypass and defaults it to an empty set.
- Existing test `WebAuthnOriginTests.IsRpIdValid_EnterpriseAllowList_BypassesSuffixCheck` explicitly expects it. The test was read, not executed.
- The validator's remarks at lines 32–35 describe the enterprise alternative as “Per WebAuthn spec”; that wording needs review against the original finding, which distinguishes a native-client SDK policy from WebAuthn's related-origin validation.

The original LOW finding explicitly says no direct exploit was demonstrated and that impact depends on application configuration and RP-side origin checks. Preserve those limits; do not call it a generic origin bypass exploit.

**Recommendation:** propose acceptance as an intentional opt-in native-client policy only after confirming supported-client scope, documentation and ownership. Otherwise keep open for policy remediation. No acceptance granted.

### Original #4 / YESDK-1612 — unsigned assertion outputs: deferral, not fixed

| Context | Reference |
| --- | --- |
| Audit headline | **Incomplete and internally inconsistent extension implementation** — original finding #4, MED; specifically, unsigned getAssertion extension outputs are dropped |
| Jira case | [YESDK-1612 — Incomplete and internally inconsistent extension implementation](https://yubico.atlassian.net/browse/YESDK-1612) |
| Jira body / actual parent | **No description.** Parent: [YESDK-1608](https://yubico.atlassian.net/browse/YESDK-1608), whose description contains only whitespace. |
| Codebase context | **CTAP2 → getAssertion response decoding.** Top-level CBOR key 0x08 is skipped, so a direct largeBlob extension's result cannot be exposed. |
| Fix/PR context | [PR #698](https://github.com/Yubico/Yubico.NET.SDK/pull/698) records deferral. Related original #3 / [YESDK-1611 — Direct CTAP largeBlob write has an invalid shape](https://yubico.atlassian.net/browse/YESDK-1611) was addressed by removing the direct extension API, not by adding key-8 decoding. |

**Vault context — two different blob mechanisms:** YubiKeys use `largeBlobKey` plus the `authenticatorLargeBlobs` command. A different CTAP extension, direct `largeBlob`, returns results such as `written`, `blob` and `originalSize` in the top-level getAssertion unsigned-output map (key `0x08`). The old SDK could encode that direct request but discarded its result. Recorded measurements found no such output on the tested YubiKeys, and the direct API was removed. The general decoder gap remains relevant if a supported authenticator/extension later emits key 8. A future fix would preserve that map and treat an empty map as absent; deleting the unreachable repro did not implement either behavior. Source: `findings/04-getassertion-unsigned-extension-outputs.md`, especially its final deferral decision.

- `src/Ctap2/src/Credentials/GetAssertionResponse.cs:144-178` still handles response keys 1–7 and skips key 8; it exposes no unsigned-outputs property.
- The vault's finding page records a synthetic failing repro and a later firmware measurement. On the tested YubiKey, direct largeBlob input produced no key 8; the largeBlobKey positive control produced key 7.
- The direct extension was removed under original #3. That reduces current supported-path exposure; it does not implement key-8 decoding.
- The historical repro was deleted. Its absence is not passing coverage. The finding page still includes an obsolete command claiming the deleted repro will fail; do not use that command as current evidence.

**Recommendation:** explicit deferred/spec-gap disposition with a supported-authenticator boundary and a revisit trigger (new supported firmware/extension requiring key 8). Maintainer acceptance is required before audit close-out; do not resolve as fixed.

### Original #17 / YESDK-1625 — SDK fixes plus runtime residual

| Context | Reference |
| --- | --- |
| Audit headline | **Core private-key construction and normalization leave intermediate copies uncleared** — original finding #17, MED; prior verification rates the remaining runtime issue LOW |
| Jira case | [YESDK-1625 — Core Private-Key Construction And Normalization leave intermediate copies uncleared](https://yubico.atlassian.net/browse/YESDK-1625) |
| Jira body / actual parent | **No description.** Parent: [YESDK-1608](https://yubico.atlassian.net/browse/YESDK-1608), whose description contains only whitespace. |
| Codebase context | **Core → RSA/EC private-key decoding, normalization and construction.** The remaining concern is a scalar copy created inside the macOS .NET runtime, beyond SDK-owned buffers. |
| Fix/PR context | Original audit records SDK fixes in `65e36386`; [PR #698](https://github.com/Yubico/Yubico.NET.SDK/pull/698), commit `6ff76c34`, adds the residual warning. An upstream issue draft exists, but filing is not established. |

**Vault context — whose copy remains:** the original SDK problem involved temporary RSA private parameters and EC scalars copied during decoding, normalization and construction, then abandoned without clearing. The original audit already acknowledges those SDK fixes. The remaining path supplies a private EC scalar `D` without public point `Q` to .NET on macOS; the runtime derives `Q` by exporting/decoding the private key and allocates its own scalar array. SDK cleanup cannot reach that array. Recovering it requires local process-memory/heap-dump access after such an import; it is not a remote protocol vulnerability. The investigation rejects a simple “supply Q instead” workaround and records an upstream-report/documentation disposition. Source: `findings/17-private-key-intermediate-copies.md` and its October 21 decision.

**Correction to the earlier mention pass:** the original audit itself records SDK-owned temporary-copy fixes in `65e36386`, predating `audit/v2-fixes`. They are not missing merely because the later branch has a documentation commit.

- The finding page identifies the remaining runtime-created scalar copy in macOS .NET 10 ECC import and cites the runtime source versions examined then.
- Current `src/Core/src/Cryptography/ECPrivateKey.cs:247-265` disposes the temporary ECDsa and zeroes both SDK-observed temporary scalar arrays in nested finally blocks.
- Its public remarks at lines 205–208 explicitly disclose the runtime copy and explain why supplying Q is not a workaround through this SDK's consistency-checking path.
- The archived unit-test claims cover SDK-owned arrays, not the runtime's internal copy. A current runtime-source/upstream-issue check has not been performed in this pass.
- The vault contains an upstream issue draft, not evidence that it was filed or assigned.

**Recommendation:** separate completed SDK remediation from residual runtime risk. Require maintainer acceptance of the residual plus an upstream-tracking owner/issue or an explicit decision not to file. Do not describe the whole issue as either wholly unfixed or wholly fixed.

## Additional incomplete coverage: N4 monthly migration workflow

| Context | Reference |
| --- | --- |
| Follow-up headline | **CI lockless installs and credential co-location** — verification finding N4, LOW–MED; not an original numbered audit finding |
| Jira case | **No dedicated case identified.** Related [YESDK-1627 — Write-Capable Documentation Workflows Execute Unverified Remote Installers / CI supply-chain trust gap](https://yubico.atlassian.net/browse/YESDK-1627) tracks original #19; its scope is related but not identical. |
| Jira body / actual parent | No N4 ticket/parent to retrieve. Related YESDK-1627 has **no description**; its parent YESDK-1608 contains only whitespace. |
| Codebase context | **GitHub Actions → documentation generation and PR creation.** Focus here: `migration-docs-monthly-synthesis.yml` runs the generator and writer in one write-enabled job. |
| Fix/PR context | [PR #710](https://github.com/Yubico/Yubico.NET.SDK/pull/710) adds locked installs and splits the architecture monthly workflow; the migration monthly workflow remains unsplit. |

**Vault context — what the separation protects:** N4 covers both dependency resolution and credential exposure. Lockless npm installs could change transitive tool dependencies between runs; generator jobs also combined a Claude OAuth credential, a write-scoped GitHub token and a persisted checkout. A compromised tool/step or malicious instruction reaching the generator could therefore have more repository authority than needed. The approved direction was locked, updateable tools and **both** monthly generators separated from PR-writing jobs. The generators still legitimately need their service credential; the intended improvement is removing repository write authority from that environment. These are trusted-event workflows, not a demonstrated fork-PR exploit. Source: `findings/N4-ci-lockless-installs-and-credential-colocation.md`; its broad “done” result is qualified by the current-code mismatch below.

**The prior “done” narrative is broader than the implemented workflow split.**

- The N4 finding page explicitly identifies **both** monthly synthesis workflows and recommends splitting both into read-only generators and separate writers.
- Current `.github/workflows/architecture-docs-monthly-synthesis.yml` does have read-only workflow permissions, `persist-credentials: false`, and a separate write-enabled `create-pr` job.
- Current `.github/workflows/migration-docs-monthly-synthesis.yml:10-12` still grants `contents: write` and `pull-requests: write` at workflow scope.
- Its single `synthesize` job checks out without `persist-credentials: false` (lines 22–26), passes both the OAuth and GitHub tokens to the action (lines 43–47), and creates the PR in the same job (lines 56–71).
- Its allowed tools omit Bash and its triggers are trusted/manual or scheduled. These limit the exposure; they do not implement the requested token/job separation. No attack or contributor-controlled exploit was demonstrated here.
- PR #710's body specifically describes the architecture split, not a migration split. The vault N4 result says “monthly synthesis split” without preserving that distinction.

**Recommendation:** N4 is partially addressed, not complete. Keep the migration-workflow residual open or obtain a specifically scoped acceptance. Do not falsely reopen all of YESDK-1627: original #19's `docs-update.yml` remediation and N4's wider monthly-workflow scope need distinct dispositions. Decide whether N4 needs its own Jira record before drafting updates.

## Important residuals requiring explicit close-out decisions

These are identified for the next risk-ranked batch; they are not blanket approvals or full per-finding verification.

| Audit/follow-up headline and Jira context | Existing evidence / current source | Required decision or evidence |
| --- | --- | --- |
| **Original #22 — Cross-process PIV PIN-state confusion permits unauthorized use of non-exportable private keys.** [YESDK-1630](https://yubico.atlassian.net/browse/YESDK-1630). Core PC/SC sharing/release policy; [PR #698](https://github.com/Yubico/Yubico.NET.SDK/pull/698). | `SmartCardSharing.cs:22-45` explicitly documents shared-mode exposure. `PcscNativeResources.cs:59-86` tries exclusive then falls back to shared; line 119 requests UNPOWER_CARD on close. Historical Linux hardware evidence records concurrent exposure returns in fallback mode. | Accept the compatibility trade-off explicitly, or require strict exclusive policy for the security claim. Do not claim unconditional cross-process isolation. Historical Linux evidence is not Windows/macOS post-fix verification. |
| **Original #1 — WebAuthn PRF is not translated to CTAP hmac-secret.** [YESDK-1609](https://yubico.atlassian.net/browse/YESDK-1609). WebAuthn PRF/UV policy; [PR #698](https://github.com/Yubico/Yubico.NET.SDK/pull/698). This row concerns a residual policy gap, not failure to implement the translation. | `PrfInput.cs:33-36` documents different PRFs with/without UV and tells callers to request Required for stable output. Historical measurement supports the distinction; notes record deliberate sibling parity rather than full L3 UV-override conformance. | Confirm acceptance of the retained policy/conformance gap and its documentation; do not conflate functional PRF translation with complete conformance. |
| **Original #19 — Write-Capable Documentation Workflows Execute Unverified Remote Installers / CI supply-chain trust gap.** [YESDK-1627](https://yubico.atlassian.net/browse/YESDK-1627). GitHub Actions tool installation; [PR #710](https://github.com/Yubico/Yubico.NET.SDK/pull/710). | #710 and current docs-update/architecture workflows reduce generator permissions and use npm lockfiles. PR expressly states npm integrity covers the shim, whose postinstall fetches the vendor binary. | Accept the remaining vendor-origin trust rather than claim independently verified binary integrity. Preserve symlink/mode patch validation as an explicit follow-up. |
| **Follow-up N2 — SCP initialization slicing and bare 9000 response.** No dedicated Jira case identified. Core secure-channel response authentication; [PR #711](https://github.com/Yubico/Yubico.NET.SDK/pull/711) and initialization hardening in [#698](https://github.com/Yubico/Yubico.NET.SDK/pull/698). Related original #23 / [YESDK-1631 — Short SCP response crashes the host process](https://yubico.atlassian.net/browse/YESDK-1631) is a separate finding. | Current `ScpProcessor.cs:156-174` rejects unauthenticated success/warnings except EXTERNAL AUTHENTICATE and invokes secure-failure recovery after state advancement; merged #711 has specific tests/hardware claims. | Review existing test bodies and archived hardware evidence in the high-risk batch. No Jira key exists in the PR; decide tracking without inventing one. |

## Evidence quality and remaining review

The archived `evidence/final-candidate-68c25316-20261022/local-verification.json` records 3443 unit executions, no failures and nine skips at that candidate. Four skip entries are parked OATH/OpenPGP methods subsequently addressed by child PRs. These numbers must not be copied as a fresh integrated-trunk test result.

Remaining work, in order:

1. Review highest-risk changed paths and their existing tests/evidence, using the headline/key index below.
2. Review the remaining grouped applet/input findings and map their evidence to the already established Jira/subtask traceability.
3. Disposition the seven original non-finding considerations and v1 release dependency separately; none is silently excluded.
4. Compile per-finding recommendations and precise residual/verification blockers. Obtain individual risk-acceptance decisions and named owners/revisit conditions.
5. Prepare a Jira change batch for approval. Do not change issues merely because a PR is merged, a repro was deleted, or a finding was excluded from a historical verification scope.

## Self-contained audit context

**Provenance and status:** the explanations below summarize the vault's original audit, finding notes, decisions and recorded implementation/measurement results, reconciled with the already established merged-PR mapping. They describe the **original failure** and **recorded remediation**, not newly reproduced bugs or a fresh conclusion that the fixes work. Detailed batch-1 conclusions above take precedence for those reviewed items. Everything else remains pending evidence review in this pass.

The source notes often say “today” or describe an open PR; those statements refer to their historical baseline, **not current trunk**. Historical production/test paths called `Fido2` now live under `Ctap2`. Original audit severity and the investigators' revised severity also differ; neither Jira priority nor a historical checkmark is treated as an approval to close.

### Original standalone findings: #1–#24, #27–#28

Headlines below are shortened for scanning. Each row retains the original audit number and linked Jira case.

| Audit / Jira | Headline, original problem and consequence | Recorded change / what remains |
| --- | --- | --- |
| #1 / [YESDK-1609](https://yubico.atlassian.net/browse/YESDK-1609) | **PRF not translated to hmac-secret.** WebAuthn forwarded a literal `prf` extension instead of hashing the RP input, encrypting/authenticating salts and decrypting CTAP hmac-secret output. Apps received no usable derived secret. This was a functional failure, not a demonstrated key leak. | #698 adds the translation, credential-specific selection and creation-time evaluation where supported. Historical known-answer and hardware results exist. Remaining UV-policy decision is described above: Required UV is needed for stable output across ceremonies. |
| #2 / [YESDK-1610](https://yubico.atlassian.net/browse/YESDK-1610) | **Required UV sends mutually exclusive fields.** A PIN/UV token produced `pinUvAuthParam`, while request options also serialized the `uv` key. CTAP forbids both, even if `uv` is false. Required-PIN registration failed with INVALID_OPTION before touch. | #698 omits `uv` when using a token and preserves absent options through the backend. Vault records pre-fix rejection on both test keys; review the existing corrected-request tests before closure. |
| #3 / [YESDK-1611](https://yubico.atlassian.net/browse/YESDK-1611) | **Invalid direct largeBlob write shape.** The direct CTAP extension encoded `write` without its required original size/compression and could mix read/write modes. This is different from YubiKeys' largeBlobKey/storage-command mechanism. | #698 removes the unsupported direct-extension API rather than implementing it. Recorded test keys did not advertise it. Closure rationale must say intentional API removal, not wire-format implementation. |
| #4 / [YESDK-1612](https://yubico.atlassian.net/browse/YESDK-1612) | **Unsigned assertion extension outputs discarded.** getAssertion skips top-level key 8, hiding direct-extension read/write results. | Explicitly deferred; current decoder gap and supported-device boundary are detailed above. No passing-test claim may be inferred from deletion of the repro. |
| #5 / [YESDK-1613](https://yubico.atlassian.net/browse/YESDK-1613) | **WebAuthn largeBlob result path broken.** Registration decoded `largeBlobKey` but its output adapter searched the wrong map, reporting unsupported. Requesting the key without a discoverable credential could also fail registration; authentication read/write paths were incomplete. | #698 carries the response key into support reporting, gates registration requests appropriately, and connects read/write to blob storage. Historical registration→write→read hardware result exists. Review this end-to-end claim, not just a boolean-output fix. |
| #6 / [YESDK-1614](https://yubico.atlassian.net/browse/YESDK-1614) | **Attestation=None still forwards attestation.** The default preference did not strip an authenticator's non-self attestation, potentially relaying identifying information the caller declined to request. Actual privacy impact depends on the returned statement and RP. | #698 replaces the statement with `fmt="none"` and an empty attestation map. Review against the chosen WebAuthn L3 contract: unlike older L2 handling, L3 does not require zeroing AAGUID. |
| #7 / [YESDK-1615](https://yubico.atlassian.net/browse/YESDK-1615) | **Preferred resident key becomes rk=false.** A capable authenticator's `rk` capability was ignored; an omitted setting was coerced to false. Registration appeared successful, but later username-less discovery found no credential. | #698 resolves Preferred against GetInfo and rejects Required when unsupported. The vault records the original discovery failure; review the new request/discovery evidence. |
| #8 / [YESDK-1616](https://yubico.atlassian.net/browse/YESDK-1616) | **topOrigin without crossOrigin.** Caller options could claim a top-level embedding origin while signed client data simultaneously said the call was not cross-origin. This is inconsistent WebAuthn context; no RP exploit was demonstrated. | #698 rejects contradictory options before the ceremony. Intentional rejection, rather than silent omission, is the recorded policy. |
| #9 / [YESDK-1617](https://yubico.atlassian.net/browse/YESDK-1617) | **Ceremony timeout ignored.** Setting Timeout did not establish a deadline. A no-touch ceremony could wait until external cancellation. Related cancellation handling could discard a successful authenticator response and strand a newly created credential. | #698 enforces the ceremony timeout and retains CTAP success when it races cancellation. Historical no-touch timeout/session-reuse evidence exists. Review deadline behavior and success-race handling together; native SmartCard cancellation remains cooperative. |
| #10 / [YESDK-1618](https://yubico.atlassian.net/browse/YESDK-1618) | **makeCredential accepts up=false.** The low-level encoder serialized a forbidden user-presence option. YubiKeys rejected it before touch; this was **not** a presence bypass. getAssertion `up=false` is valid and needed for credential probing. | #698 rejects makeCredential false before transmission while retaining assertion probing. Review both rejection and the valid-control path. |
| #11 / [YESDK-1619](https://yubico.atlassian.net/browse/YESDK-1619) | **credBlob retrieval has the wrong CBOR type.** Assertion retrieval sent an empty byte string instead of boolean true; high-level WebAuthn lacked the corresponding input. One YubiKey tolerated it, which did not make the encoding conformant. | #698 adds correctly typed retrieval and exposes `getCredBlob`. Review creation byte-string and assertion-boolean cases separately; device tolerance alone is insufficient evidence. |
| #12 / [YESDK-1620](https://yubico.atlassian.net/browse/YESDK-1620) | **Incorrect largeBlob fragment length.** Storage used fixed 1024-byte fragments instead of GetInfo `maxMsgSize − 64`. When maxMsgSize is absent, the permitted fragment is 960. Strict/smaller authenticators can reject reads and writes. | #698 derives fragment sizing from GetInfo. The recorded YubiKeys advertised a larger limit, so their old-path success did not disprove the bug. Existing absent/default and advertised-limit tests are important. |
| #13 / [YESDK-1621](https://yubico.atlassian.net/browse/YESDK-1621) | **Private, incompatible large-blob format.** SDK entries were byte strings with private CBOR plaintext, rather than the specified map, DEFLATE content and authenticated original-size data. It could not process another client's entries, and other clients could not read its entries. | #698 rewrites the format and records two-way python-fido2 interop. Old pre-release private-format entries deliberately receive no migration shim; unrelated valid entries must survive rewrites. Compression bounds and malformed-entry behavior are part of this fix. |
| #14 / [YESDK-1622](https://yubico.atlassian.net/browse/YESDK-1622) | **Corrupt checksum prevents recovery.** A torn/corrupt array caused blob get/set/delete to throw indefinitely, whereas CTAP requires treating failed integrity as the initial empty array. | #698 applies recovery at the storage-read boundary, including short arrays. Next writes replace corrupt contents; that data-loss consequence is intentional recovery, not recovery of the old blob. Direct deserialization remains strict. |
| #15 / [YESDK-1623](https://yubico.atlassian.net/browse/YESDK-1623) | **Nonstandard RP-ID override.** Enterprise allowlist bypasses normal origin/domain binding by application configuration. | Intentional behavior remains; policy acceptance/documentation decision needed. Full example and limits above. |
| #16 / [YESDK-1624](https://yubico.atlassian.net/browse/YESDK-1624) | **Plaintext retained after blob operations.** Replacement/deletion decrypted existing blobs merely to identify the matching entry, then discarded plaintext copies without clearing; encryption also left temporary plaintext. Sensitive contents could remain in a local heap/crash dump. | #698 records cleanup of SDK-owned compression/decompression/GCM temporaries in finally blocks. Returned plaintext is caller-owned. Requires sensitive blob contents plus local memory access; not a remote decryption attack. |
| #17 / [YESDK-1625](https://yubico.atlassian.net/browse/YESDK-1625) | **Private-key intermediates uncleared.** Temporary RSA/EC copies could outlive the final wrapper's cleanup. | SDK copies fixed earlier in `65e36386`; #698 documents remaining macOS runtime copy. Acceptance/upstream ownership, not another blanket “fixed” claim, is needed. |
| #18 / [YESDK-1626](https://yubico.atlassian.net/browse/YESDK-1626) | **Unmanaged discovery memory leak.** Generic/NFC reader-name allocations are lost on repeated PC/SC scans. | Unresolved pattern confirmed by this pass's code inspection; detailed evidence above. |
| #19 / [YESDK-1627](https://yubico.atlassian.net/browse/YESDK-1627) | **Documentation workflow supply-chain trust.** A mutable installer/tool ran in a job with repository write authority, then received the Claude credential. Risk requires a compromised trusted distribution/step, not merely an ordinary PR author. | #710 locks tool versions/dependencies, adds update tracking and reduces generator permissions. Historical decision revised severity to LOW. Vendor binary trust remains; the npm shim hash is not independent binary verification. Wider monthly-migration gap is N4. |
| #20 / [YESDK-1628](https://yubico.atlassian.net/browse/YESDK-1628) | **Truncated TLV length headers throw the wrong exception.** `5A 81` declares a missing length byte; `5A 82 01` lacks the second byte. Parsing indexed beyond the remaining buffer instead of normally rejecting malformed encoding. | #698 addresses this with long-form-length fix 25e / YESDK-1644. Same remediation evidence can support both records; do not double-count the implementation. |
| #21 / [YESDK-1629](https://yubico.atlassian.net/browse/YESDK-1629) | **Empty/short device-info values indexed unchecked.** Valid TLV structure does not imply enough bytes for flags, timeouts or versions. | Unresolved pattern confirmed by this pass's code inspection. Distinct from TLV header fix #20 and pagination fix 25b. |
| #22 / [YESDK-1630](https://yubico.atlassian.net/browse/YESDK-1630) | **Cross-process reuse of PIV PIN state.** PIN=ONCE authorization belongs to the card, not an SDK object. A second handle could sign without knowing the PIN, during or after another application's verification. | #698 defaults to exclusive-first with shared fallback and power-off on release. Historical Linux measurement shows improvement and fallback exposure. Strict exclusive mode is available; default fallback intentionally does not guarantee concurrent isolation. |
| #23 / [YESDK-1631](https://yubico.atlassian.net/browse/YESDK-1631) | **Short SCP response aborts host.** Subtracting the MAC size from attacker-controlled short response data could yield a negative stack allocation and process abort, bypassing managed error handling/cleanup. A party controlling the reader/relay path can trigger it. | #698 rejects short R-MAC data and malformed handshake responses with BadResponseException. Historical child-process repro distinguishes process death from an ordinary thrown exception. N2's forged empty-success issue is separate. |
| #24 / [YESDK-1632](https://yubico.atlassian.net/browse/YESDK-1632) | **Ed25519 silently signs a changed message.** PIV formatting truncated messages longer than 32 bytes and zero-padded shorter ones. Signatures could fail standard verification; suffix bytes of long messages were not bound as the caller intended. | #698 passes Ed25519 input unchanged. Vault records independent verification of the original wrong-message behavior and subsequent hardware claims. Maximum supported message lengths across firmware remain a distinct boundary, not permission to truncate. |
| #27 / [YESDK-1635](https://yubico.atlassian.net/browse/YESDK-1635) | **Mismatched private/public key components accepted.** Malformed PKCS#8 could pair private A with public B. Applications could register/certify the wrong public key even though a card imports private A and derives public A. No private-key disclosure is demonstrated. | #698 adds RSA/EC consistency checks. Review rejection of inconsistent parameters and valid-key controls, including the runtime path's relationship to #17. |
| #28 / [YESDK-1636](https://yubico.atlassian.net/browse/YESDK-1636) | **One malformed OATH credential breaks all batch codes.** Importing `period=0` stores a name such as `0/demo`; later batch processing divides by zero and returns no results, although other credentials still work individually. Requires ability to add a credential or accidental bad import. | #698 rejects nonpositive TOTP periods and skips malformed IDs when listing/calculating all. Overflows/non-ASCII period prefixes are related cases. Same root cause as 25i / YESDK-1641. |

### Group #25 — numeric boundaries

Parent: [YESDK-1633 — Numeric-boundary issues](https://yubico.atlassian.net/browse/YESDK-1633), under epic YESDK-1608. Letters are the vault's labels, not Jira ordering. Historical fix claims below still need the agreed evidence review.

| Audit / subtask | Headline and original consequence | Recorded remediation |
| --- | --- | --- |
| 25a / [YESDK-1637](https://yubico.atlassian.net/browse/YESDK-1637) | **Unchecked CLI password length.** Negative static-password `--length` reaches stackalloc and can terminate the process instead of producing a normal argument error. | #698 validates before allocation; historical child-process test exists. |
| 25b / [YESDK-1639](https://yubico.atlassian.net/browse/YESDK-1639) | **Device-info pagination wraps.** Repeated “more pages” responses wrap the one-byte page index, permitting indefinite continuation. | #698 bounds pagination at the P1 byte limit. Different from short field values in #21. |
| 25c / [YESDK-1642](https://yubico.atlassian.net/browse/YESDK-1642) | **Wide RNG ranges return a constant.** Range arithmetic in a helper could produce deterministic output where callers expected randomness. This does not establish that every SDK crypto operation used that helper/range. | #698 removes the custom helper in favor of the runtime API. |
| 25d / [YESDK-1643](https://yubico.atlassian.net/browse/YESDK-1643) | **HKDF counter overflow.** Valid maximum-length SHA-256 output (8129–8160 bytes) wraps a one-byte loop counter and computes a negative offset. RFC maximum validation already existed; the loop was wrong inside that maximum. Prior notes found no caller reaching that range. | #698 replaces hand-written HKDF with runtime HKDF. |
| 25e / [YESDK-1644](https://yubico.atlassian.net/browse/YESDK-1644) | **BER-TLV length overflow/truncation.** Long-form lengths exceeding the integer width could wrap; truncated length headers could index past the buffer. The audit's four-byte example was already rejected, but five-or-more-byte cases remained. | #698 validates long-form lengths. Also covers original #20 / YESDK-1628. |
| 25f / [YESDK-1645](https://yubico.atlassian.net/browse/YESDK-1645) | **TLV encoder underallocates.** `EncodeDictionary` estimates too little space for encoded TLVs, then fails while copying rather than returning the encoding. | #698 sizes from the built TLVs instead of the faulty estimate. |
| 25g / [YESDK-1640](https://yubico.atlassian.net/browse/YESDK-1640) | **Nonpositive credential batches.** A zero/negative advertised list limit breaks exclude-list progress; probing can loop or miss an existing credential. A related tokenless path sent too many entries without preflight. | #698 guards the invalid limit and applies batched filtering without a PIN/UV token. Recorded red→green and 17-credential hardware evidence exists. |
| 25h / [YESDK-1638](https://yubico.atlassian.net/browse/YESDK-1638) | **OpenPGP KDF count sign-wrap.** An unsigned count at or above 2³¹ became negative, so the hashing loop could run zero times and derive SHA-256 of empty input. | #709 parses unsigned into a wider type; combined with 26q's minimum-full-input rule. No upper bound was chosen, matching the recorded GnuPG/ykman policy. |
| 25i / [YESDK-1641](https://yubico.atlassian.net/browse/YESDK-1641) | **Invalid OATH periods abort batch calculation.** A stored zero/custom malformed period breaks retrieval of otherwise valid credentials. | #698; same defect as original #28 / YESDK-1636, not another independent batch bug. |

### Group #26 — API footguns

Parent: [YESDK-1634 — API Footguns](https://yubico.atlassian.net/browse/YESDK-1634), under epic YESDK-1608. Some outcomes are intentionally documentation, device-semantics clarification or rejection of the finding—not code fixes.

| Audit / subtask | Headline and original consequence | Recorded remediation / qualification |
| --- | --- | --- |
| 26a / [YESDK-1654](https://yubico.atlassian.net/browse/YESDK-1654) | **Credential-specific PRF inputs ignored.** A per-credential override was dropped when global inputs existed, or encoding failed when only the map was given. Apps derived no result or one for the wrong input. | #698 selects the actual credential, uses its matching override, and validates invalid maps. Related to #1, but requires its own input-selection evidence. |
| 26b / [YESDK-1657](https://yubico.atlassian.net/browse/YESDK-1657) | **PRF enabled ignores returned value.** Merely seeing an extension entry could become Enabled=true, even for false/invalid content. | #698 derives enabled from hmac-secret's actual boolean; absent/nonboolean reports disabled. |
| 26c / [YESDK-1655](https://yubico.atlassian.net/browse/YESDK-1655) | **Missing previewSign attestation substitutes authentication key.** Registration could return an ordinary login credential as if it were the separate requested signing key. An RP could silently register the wrong key. | #698 fails when required inner signing-key attestation is absent instead of substituting. Investigators rated silent wrong-key binding HIGH. |
| 26d / [YESDK-1662](https://yubico.atlassian.net/browse/YESDK-1662) | **previewSign selects the first map entry.** Low-level encoding discarded credential IDs and used the first signing parameters, making message/key selection depend on dictionary order rather than the selected credential. | #698 low-level API accepts one credential's parameters; WebAuthn probes/selects the credential and resolves its matching map entry. Historical two-credential hardware evidence is recorded. |
| 26e / [YESDK-1649](https://yubico.atlassian.net/browse/YESDK-1649) | **PIV retry-limit changes reset PIN/PUK.** An authorized admin call intended to tighten retries also restores factory secrets (PIN `123456`, PUK `12345678`). Without warning, tooling could unintentionally weaken credentials. | #698 documents the reset and verified-PIN prerequisite. This is device behavior, not a promise to preserve the old PIN/PUK; maintainer chose documentation, not an acknowledgement parameter. |
| 26f / [YESDK-1660](https://yubico.atlassian.net/browse/YESDK-1660) | **PIV EC/RSA input altered.** Short EC digests were right-padded, changing their integer value; oversized RSA blocks lost leading bytes. Calls silently performed a different cryptographic operation. | #698 left-pads short EC digests and rejects oversized RSA blocks. Shares formatting code with #24, but Ed25519 requires unchanged messages, not digest padding. |
| 26g / [YESDK-1646](https://yubico.atlassian.net/browse/YESDK-1646) | **Redirected credential input bypasses policy.** Length/character rules applied interactively but were skipped for stdin automation, so the same options accepted different secrets depending on input mode. | #698 applies the credential policy to redirected input too. |
| 26h / [YESDK-1647](https://yubico.atlassian.net/browse/YESDK-1647) | **Confirmation exceptions leave secret buffers uncleared.** Cancellation or prompt failure after the first entry skipped its disposal, with no owner returned to clean it. | #698 clears both confirmation entries on all exit paths. Local memory-hygiene defect, not demonstrated remote leakage. |
| 26i / [YESDK-1648](https://yubico.atlassian.net/browse/YESDK-1648) | **OpenPGP import targets the template's slot.** Caller-specified slot and template-embedded slot could disagree: attributes changed one slot while import overwrote another, potentially affecting two keys. | #708 changes PutKeyAsync to take the slot once plus IPrivateKey; SDK derives attributes/templates. Historical wire tests and key-import hardware verification are recorded. |
| 26j / [YESDK-1650](https://yubico.atlassian.net/browse/YESDK-1650) | **OpenPGP attestation overwrites enrolled certificate.** Asking for attestation replaces the cardholder certificate, even if reading the result later fails; it does not delete the private key. | #698 adds a public warning. Investigation classifies this as specified device behavior with missing documentation, not a device defect to eliminate. |
| 26k / [YESDK-1651](https://yubico.atlassian.net/browse/YESDK-1651) | **OATH credential's device identity ignored.** A credential handle read from key A could delete/rename/calculate a same-named credential on key B instead of rejecting the mismatch. | #707 checks device identity before building/sending commands in all four Credential-taking operations. Same-device success and foreign-device refusal need evidence. |
| 26l / [YESDK-1652](https://yubico.atlassian.net/browse/YESDK-1652) | **OTP update flags silently dropped.** API setters accepted security settings that serialization masked out. Investigation also found the ProtectSlot2 bit was wrong and could affect a mode bit. | #698 fixes the bit, preserves AllowUpdate and rejects unsupported ProtectSlot2 updates rather than pretending to apply them. |
| 26m / [YESDK-1653](https://yubico.atlassian.net/browse/YESDK-1653) | **Empty certificate allowlist means allow any CA-signed host.** Code mapping “no permitted serials” to an empty list removes the restriction instead of deny-all. The host still needs a certificate from the trusted CA; this is not acceptance of arbitrary certificates. | #698 rejects an empty StoreAllowList call and keeps ClearAllowList explicit. Deny-all requires removing the trust anchor, not an empty list. Separate SCP11c counter observation remains outside this fix. |
| 26n / [YESDK-1656](https://yubico.atlassian.net/browse/YESDK-1656) | **Reused builder mutates already-built policy.** Built DeviceConfig shared mutable capability storage with its builder. Later builder edits could change an earlier validated snapshot, including disallowed capabilities. | #698 replaces the mutable builder with a flags record. |
| 26o / [YESDK-1658](https://yubico.atlassian.net/browse/YESDK-1658) | **OATH retry replaces seed with zeros.** Historical investigation checked password-derived authentication-key cleanup, not PUT's alias to the stored credential seed. | **Historical not-a-defect recommendation superseded:** current source trace shows PUT cleanup clears ordinary-sized source seeds before retry. Android/Rust use separate buffers. Keep open pending targeted failing regression; see interview correction above. |
| 26p / [YESDK-1659](https://yubico.atlassian.net/browse/YESDK-1659) | **YubiOTP HMAC challenge-padding ambiguity.** A short challenge and its padded 64-byte form can produce the same request/response. The device's short mode uses trailing bytes as an end marker, so arbitrary length separation cannot be restored just by host encoding. HMAC-SHA1 itself is not broken. | #698 follows ykman's padding, corrects empty-input padding to zero and documents mode/marker behavior. Hardware measurements distinguish short/fixed modes; empty input hashes a marker byte, not the mathematical empty message. No mode parameter was added. |
| 26q / [YESDK-1661](https://yubico.atlassian.net/browse/YESDK-1661) | **OpenPGP KDF count excludes the PIN.** With an eight-byte salt, a byte count of eight hashes only salt; nine includes only the first PIN byte. Different PINs become equivalent. This is distinct from 25h's unsigned sign-wrap. | #709 hashes at least one complete salt+PIN block, matching RFC 9580/GnuPG. Deliberate behavior change for unusual short-count objects; recorded libgcrypt comparison exists, but no KDF provisioning hardware run was made. |

### Follow-ups found during verification, not in the original audit

No dedicated Jira cases have been identified for N1–N4 in the reviewed evidence. Their original-finding relationships are given explicitly rather than inventing tracking IDs.

| ID / headline | Problem and consequence | Recorded work / remaining boundary |
| --- | --- | --- |
| **N1 — duplicate pubKeyCredParams** | Repeated algorithm entries were accepted at WebAuthn/CTAP boundaries although CTAP prohibits duplicates. A strict authenticator can reject the request; no security exploit is shown. | #698 rejects duplicates. Low-risk encoding/input-contract issue; related WebAuthn request-shaping work, no dedicated case. |
| **N2 — SCP setup slicing and forged bare success** | Malformed handshake data caused unhelpful range exceptions. Separately, an on-path party could strip an authenticated response or turn an error into bare `9000`, making provisioning appear successful when the card had not done it. This does not grant card-side operations or reveal keys. | #698 hardens setup; #711 rejects missing R-MAC on success/warnings except the initialization response. Historical SCP03/SCP11b tampering probes support it. Warning statuses were not measured on hardware. Related #23 / YESDK-1631 addresses process crashes, not this integrity failure. |
| **N3 — CLI password encoding and redirected confirmation** | Static-password CLI sent UTF-8 text as keyboard scan codes, so the key typed different characters despite successful programming. Confirmation used key polling that fails when stdin is redirected. | #698 uses layout-aware HID scan codes and line-based redirected confirmation. Vault records an exact keyboard-output check into an editor, not a shell; initial inconclusive attempt is not a newly diagnosed bug. |
| **N4 — lockless installs and credential co-location** | Tool dependency trees could change without a repository change; monthly generators combined service credentials and repository write authority. | #710 implements lockfiles/tool pinning and architecture job separation. This review found monthly migration still unsplit; detailed unresolved scope above. |

### Seven original “Items to Consider” — separate backlog, not silently closed

These are original improvement suggestions, **not automatically security findings**. The descriptions below state their original concerns only; this pass has not established their current fix status or identified dedicated Jira cases for them.

| Original consideration | Context / why a reader might care |
| --- | --- |
| 1. **Management timeout documentation vs validation** | Auto-eject docs advertised 0–65535 seconds but builder validation capped 3600; challenge-response docs advertised 0–255 but capped 60. Decide intended limits and align contract/tests, accounting for the later builder replacement. |
| 2. **OpenPGP absent certificate vs failed read** | Catching every ApduException and returning null can hide a real card/communication failure as “no certificate.” Narrow expected-absence handling if appropriate. |
| 3. **FIDO HID configuration vs async initialization** | Synchronous Configure waited on an async handshake, allowing configuration to block unexpectedly. Later session-init refactoring is relevant, but its existence alone does not close this suggestion. |
| 4. **WebAuthn registration transport metadata** | Registration always returned Transports=null despite potential backend information. Clarify reliable metadata and when unknown/null is intentional. |
| 5. **Security Domain certificate-bundle cleanup** | A collection built with Select(...).ToList() can leak already-created disposable certificate objects if a later parse fails. Review exception-safe ownership, not just successful caller disposal. |
| 6. **Management factory-reset transport docs** | Management generally supports SmartCard and HID, but whole-device factory reset requires SmartCard. Clarify the operation-specific restriction rather than suggesting all operations work on every backend. |
| 7. **OATH comparison can hide credentials in sorted sets** | CompareTo used issuer/account only, omitting full device/raw-ID identity. SortedSet can collapse distinct returned credential objects even though the underlying YubiKey entries and original list are intact. Separate display sorting from identity if needed. |

**v1 release dependency / YESDK-1588:** the original audit says older findings may be fixed in v2, but whether that closes the v1 obligation depends on supersession/deprecation timelines. This is a release-policy decision, not evidence that legacy branches received the changes.

## Jira body snapshot: tickets and their parents

Expanded/rechecked **2026-10-30 at 14:16:21 UTC**, read-only through Jira's API. “Body” here means the Jira **Description** field, not the title, comments, attachments, or original audit report. All **56** relevant records were returned: the 54 finding/umbrella/subtask records, epic YESDK-1608 and older tracker YESDK-1588. All parent links resolve within this snapshot. No parent was inferred from numbering. Only YESDK-1588 has substantive description text; the self-contained summaries above supply the missing audit context without presenting it as Jira text.

<details>
<summary>Expand ticket-by-ticket body/parent results and the older v1 description</summary>

### Parent descriptions

| Parent ticket | Actual parent | Jira body |
| --- | --- | --- |
| [YESDK-1608 — .NET SDK v2.0 Pre-Release Findings](https://yubico.atlassian.net/browse/YESDK-1608) | None returned | Whitespace only; **no substantive description**. |
| [YESDK-1633 — Numeric-boundary issues -Sub tasks](https://yubico.atlassian.net/browse/YESDK-1633) | YESDK-1608 | **No description** (`null`). |
| [YESDK-1634 — API- Footguns - Sub Tasks](https://yubico.atlassian.net/browse/YESDK-1634) | YESDK-1608 | **No description** (`null`). |

The numeric-boundary/API-footgun subtasks below therefore have no substantive body to inherit from either their immediate parent or the epic.

### Referenced finding tickets

Every “No description” below is the live API result (`description: null`), not a failed retrieval or a conclusion that the original audit lacked detail. Headlines are lightly whitespace-normalized for readability.

| Jira ticket and headline | Actual parent | Jira body |
| --- | --- | --- |
| [YESDK-1609 — WebAuthn PRF is not translated to CTAP hmac-secret](https://yubico.atlassian.net/browse/YESDK-1609) | YESDK-1608 | No description. |
| [YESDK-1611 — Direct CTAP largeBlob write has an invalid shape](https://yubico.atlassian.net/browse/YESDK-1611) | YESDK-1608 | No description. |
| [YESDK-1612 — Incomplete and internally inconsistent extension implementation](https://yubico.atlassian.net/browse/YESDK-1612) | YESDK-1608 | No description. |
| [YESDK-1613 — WebAuthn largeBlob is exposed but its result path is broken](https://yubico.atlassian.net/browse/YESDK-1613) | YESDK-1608 | No description. |
| [YESDK-1620 — Incorrect fragment length](https://yubico.atlassian.net/browse/YESDK-1620) | YESDK-1608 | No description. |
| [YESDK-1621 — Large-blob entries use a private, incompatible format](https://yubico.atlassian.net/browse/YESDK-1621) | YESDK-1608 | No description. |
| [YESDK-1622 — Corrupt checksum handling](https://yubico.atlassian.net/browse/YESDK-1622) | YESDK-1608 | No description. |
| [YESDK-1623 — Nonstandard RP-ID policy override](https://yubico.atlassian.net/browse/YESDK-1623) | YESDK-1608 | No description. |
| [YESDK-1624 — conditional plaintext-retention issue](https://yubico.atlassian.net/browse/YESDK-1624) | YESDK-1608 | No description. |
| [YESDK-1625 — Core Private-Key Construction And Normalization leave intermediate copies uncleared](https://yubico.atlassian.net/browse/YESDK-1625) | YESDK-1608 | No description. |
| [YESDK-1626 — unmanaged-memory leak](https://yubico.atlassian.net/browse/YESDK-1626) | YESDK-1608 | No description. |
| [YESDK-1627 — Write-Capable Documentation Workflows Execute Unverified Remote Installers / CI supply-chain trust gap](https://yubico.atlassian.net/browse/YESDK-1627) | YESDK-1608 | No description. |
| [YESDK-1629 — Device-info decoding indexes empty or undersized TLV values without validation](https://yubico.atlassian.net/browse/YESDK-1629) | YESDK-1608 | No description. |
| [YESDK-1630 — Cross-process PIV PIN-state confusion permits unauthorized use of non-exportable private keys](https://yubico.atlassian.net/browse/YESDK-1630) | YESDK-1608 | No description. |
| [YESDK-1631 — Short SCP response crashes the host process](https://yubico.atlassian.net/browse/YESDK-1631) | YESDK-1608 | No description. |
| [YESDK-1632 — PIV Ed25519 signing silently changes the caller’s message](https://yubico.atlassian.net/browse/YESDK-1632) | YESDK-1608 | No description. |
| [YESDK-1635 — SDK accepts mismatched public and private key components (ECDSA and RSA)](https://yubico.atlassian.net/browse/YESDK-1635) | YESDK-1608 | No description. |
| [YESDK-1639 — Device-info pagination wraps and permits indefinite continuation](https://yubico.atlassian.net/browse/YESDK-1639) | YESDK-1633 | No description. |
| [YESDK-1649 — PIV retry-limit changes also restore factory PIN and PUK](https://yubico.atlassian.net/browse/YESDK-1649) | YESDK-1634 | No description. |
| [YESDK-1653 — An empty certificate allowlist removes the restriction instead of denying all](https://yubico.atlassian.net/browse/YESDK-1653) | YESDK-1634 | No description. |
| [YESDK-1655 — Missing previewSign attestation substitutes the authentication key](https://yubico.atlassian.net/browse/YESDK-1655) | YESDK-1634 | No description. |
| [YESDK-1660 — PIV input formatting alters short ECDSA digests and truncates oversized RSA blocks](https://yubico.atlassian.net/browse/YESDK-1660) | YESDK-1634 | No description. |
| [YESDK-1662 — FIDO previewSign ignores credential IDs and selects the first entry’s signing parameters](https://yubico.atlassian.net/browse/YESDK-1662) | YESDK-1634 | No description. |

Additional tickets included by the complete self-contained context digest:

| Jira ticket and headline | Actual parent | Jira body |
| --- | --- | --- |
| [YESDK-1610 — Required UV composes an inconsistent CTAP request](https://yubico.atlassian.net/browse/YESDK-1610) | YESDK-1608 | No description. |
| [YESDK-1614 — Attestation=None still forwards the authenticator’s attestation statement](https://yubico.atlassian.net/browse/YESDK-1614) | YESDK-1608 | No description. |
| [YESDK-1615 — residentKey=preferred becomes CTAP rk=false on a capable authenticator](https://yubico.atlassian.net/browse/YESDK-1615) | YESDK-1608 | No description. |
| [YESDK-1616 — topOrigin can be serialized when crossOrigin is false](https://yubico.atlassian.net/browse/YESDK-1616) | YESDK-1608 | No description. |
| [YESDK-1617 — Ceremony timeout is ignored](https://yubico.atlassian.net/browse/YESDK-1617) | YESDK-1608 | No description. |
| [YESDK-1618 — Public low-level API Fido2 makeCredential serializes CTAP options.up=false, which CTAP 2.3 does not allow for every authenticator version](https://yubico.atlassian.net/browse/YESDK-1618) | YESDK-1608 | No description. |
| [YESDK-1619 — credBlob retrieval uses the wrong CBOR type](https://yubico.atlassian.net/browse/YESDK-1619) | YESDK-1608 | No description. |
| [YESDK-1628 — Truncated Long-Form TLV Length Headers Escape Normal Parser Rejection](https://yubico.atlassian.net/browse/YESDK-1628) | YESDK-1608 | No description. |
| [YESDK-1636 — One malformed credential breaks the entire OTP batch](https://yubico.atlassian.net/browse/YESDK-1636) | YESDK-1608 | No description. |
| [YESDK-1637 — Unchecked negative password length causes a fatal stack allocation](https://yubico.atlassian.net/browse/YESDK-1637) | YESDK-1633 | No description. |
| [YESDK-1638 — OpenPGP KDF count truncation removes PIN input from hashing](https://yubico.atlassian.net/browse/YESDK-1638) | YESDK-1633 | No description. |
| [YESDK-1640 — Nonpositive WebAuthn batch sizes break exclude-list progress](https://yubico.atlassian.net/browse/YESDK-1640) | YESDK-1633 | No description. |
| [YESDK-1641 — Invalid OATH periods abort batch code calculation](https://yubico.atlassian.net/browse/YESDK-1641) | YESDK-1633 | No description. |
| [YESDK-1642 — Large RNG intervals return deterministic values without randomness](https://yubico.atlassian.net/browse/YESDK-1642) | YESDK-1633 | No description. |
| [YESDK-1643 — HKDF block-counter overflow produces a negative output offset](https://yubico.atlassian.net/browse/YESDK-1643) | YESDK-1633 | No description. |
| [YESDK-1644 — BER-TLV length overflow accepts truncated lengths](https://yubico.atlassian.net/browse/YESDK-1644) | YESDK-1633 | No description. |
| [YESDK-1645 — TLV encoder underallocates its output buffer](https://yubico.atlassian.net/browse/YESDK-1645) | YESDK-1633 | No description. |
| [YESDK-1646 — Redirected credential input bypasses its policy options](https://yubico.atlassian.net/browse/YESDK-1646) | YESDK-1634 | No description. |
| [YESDK-1647 — Confirmation exceptions strand an uncleared credential buffer](https://yubico.atlassian.net/browse/YESDK-1647) | YESDK-1634 | No description. |
| [YESDK-1648 — OpenPGP key import obeys the template, not the requested target](https://yubico.atlassian.net/browse/YESDK-1648) | YESDK-1634 | No description. |
| [YESDK-1650 — OpenPGP attestation generation overwrites the enrolled certificate](https://yubico.atlassian.net/browse/YESDK-1650) | YESDK-1634 | No description. |
| [YESDK-1651 — OATH operations ignore the credential’s device identity](https://yubico.atlassian.net/browse/YESDK-1651) | YESDK-1634 | No description. |
| [YESDK-1652 — OTP update builders silently discard accepted protection setting](https://yubico.atlassian.net/browse/YESDK-1652) | YESDK-1634 | No description. |
| [YESDK-1654 — Credential-specific PRF inputs are accepted but ignored](https://yubico.atlassian.net/browse/YESDK-1654) | YESDK-1634 | No description. |
| [YESDK-1656 — Reusing a configuration builder changes an already-built policy](https://yubico.atlassian.net/browse/YESDK-1656) | YESDK-1634 | No description. |
| [YESDK-1657 — PRF enabled status ignores the returned value](https://yubico.atlassian.net/browse/YESDK-1657) | YESDK-1634 | No description. |
| [YESDK-1658 — OATH authentication retry silently replaces the intended credential seed with zeros](https://yubico.atlassian.net/browse/YESDK-1658) | YESDK-1634 | No description. |
| [YESDK-1659 — YubiOTP HMAC challenge padding maps distinct inputs to identical device requests](https://yubico.atlassian.net/browse/YESDK-1659) | YESDK-1634 | No description. |
| [YESDK-1661 — OpenPGP KDF accepts byte counts that exclude the PIN from derivation](https://yubico.atlassian.net/browse/YESDK-1661) | YESDK-1634 | No description. |

### Older v1 tracking ticket: actual body

[YESDK-1588 — Tracking the triaged findings of the bug hunting initiative](https://yubico.atlassian.net/browse/YESDK-1588). No parent returned. Status at retrieval: Todo. Description text below preserves the content, with paragraph/hard-break formatting restored:

> Tracking the triaged findings of the bug hunting initiative by the YES TEam
>
> **Parser Hygiene**
>
> **API Contract**
>
> **CI/CD Supply Chain**<br>
> issues
>
> The source lines might have drifted in between the report repo state and current repo state
>
> Repo git hash:`d2fc4560ed1d415137e1aa2f3615a2ee2985d02d`

No content from this v1 description has been substituted for any v2 finding's empty body. Detailed v2 claims continue to come from the original audit and evidence sources identified above.

</details>

No new builds, tests, workflow runs, hardware actions, upstream issue filings or Jira changes were performed in this batch. Jira descriptions and parent relationships were read only.
