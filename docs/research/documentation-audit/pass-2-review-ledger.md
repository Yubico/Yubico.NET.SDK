# Pass 2 review: per-card ledger

Final call for every card in `pass-2-applet-master.md` (at `52d34ca65`), as decided by Dennis between 2026-11-14 and
2026-11-20. Review closed 2026-11-20 (AG-25). This ledger supersedes the disposition columns and interview queues
in `pass-2-review-classification.md` and `pass-2-review-classification/<applet>.md`; those files keep the evidence
pins, options and pseudocode. Agreement numbers (AG-n) point into `pass-2-review-decisions.md`; rule IDs point into
`pass-2-review-policy.md`.

## How to read this

- **Auto:** cleared the policy (rules A1 to A6) without a gate. The fix is Sia's lean in the per-applet file. Veto
  pass AG-25: no vetoes.
- **Auto (AG-19):** moved to auto by rules M1 (repo strictness doctrine) or M2 (spec-mandated, quote re-verified), or
  reclassified as sibling consensus. No vetoes except WA-IMP-015 (vetoed in AG-20).
- **F1 confirmed:** High card settled by a one-line yes (AG-10).
- **Dennis:** decided by Dennis in interview or in the vault.
- **Split:** a rule-covered facet is auto (AG-19, split table); the rest of the card is Dennis's call shown here.
- **T1 hardware:** device qualification; no code decision in this pass.
- **Recorded in / amended in:** the agreement holding the call, and any later agreement that touches the card. When
  a later agreement changed the call, the final call column already reflects it.

## Cross-applet decisions that apply on top of every card

| # | Decision | Recorded |
| --- | --- | --- |
| X1 | A failed login never ends the session; it clears only that credential's earlier success; a failed SCP handshake ends it | AG-23 |
| X2 | Sessions continue after a confirmed reset in every applet: reselect, clear cached and authenticated state, re-read, re-run SCP; Security Domain re-authenticates with the default SCP03 keys (Q12 a). Flips PGP-IMP-005 and MGMT-IMP-010. OATH follow-up: correct the reset message and add focused software-only SCP11 coverage | AG-22, AG-23, [AG-31](pass-2-review-decisions.md#ag-31-november-21-follow-up-approvals) |
| X3 | Secret results are caller-owned writable `Memory<byte>` (single and composite, R1); `IDisposable` only for secret holders (`CredentialPassword`, `SessionKeys`). No new public `Clear()` convenience methods on applet result types. Existing public `ApduResponse.Clear()` remains supported; the SDK must still clear sensitive buffers it owns. Samples follow S1 | AG-21, AG-24, AG-25, [AG-27](pass-2-review-decisions.md#ag-27-r1-clear-scope) |
| X4 | Rule C1: callers serialize one complete operation at a time per session and await it before another operation or disposal. Concurrent use is unsupported; the existing logical protocol exchange guard is unchanged. No whole-operation admission guard or workflow lease. | [AG-29](pass-2-review-decisions.md#ag-29-c1-caller-serialization), supersedes AG-21 |
| X5, X6 | Cancellation and error mapping settled through their cards | AG-20, AG-21 |
| X7 | Rule M1: strict parsing per the repo doctrine (`CLAUDE.md`), unknown well-formed values preserved | AG-19 |
| X8 | Firmware: cached device info first, then the applet's own read; 0.x or a failed read is alpha/beta and admits every gate | AG-14, AG-21 (Q6) |
| X9 | Example tools read secrets through `ConsoleCredentialReader`; output follows S1 | AG-21 |
| X10 | Tests start from a known state; self-cleanup only as a justified exception | AG-13 |
| X11 | previewSign and ARKG in scope behind `[Experimental("YKEXP001")]`; no new user docs | AG-11 |
| X12 | `CredentialPassword` with `FromUtf8` (padded, at most 16 bytes) and `FromRaw` (exactly 16) | AG-23 |
| X13 | Conservative ROCA range [4.2.0, 4.3.5) for on-card RSA generation in PIV and OpenPGP; import, signing and decryption unchanged. PIV policy adoption does not close PIVIMP-003 qualification. | AG-20, [AG-31](pass-2-review-decisions.md#ag-31-november-21-follow-up-approvals) |
| S1 | Sample apps: readability first, no added secret clearing, demo-only warning | AG-21 |
| D1 | Every PR labels each divergence from the sibling SDKs with its reason | AG-21 |
| C1 | See X4 | [AG-29](pass-2-review-decisions.md#ag-29-c1-caller-serialization) |

Delivery (AG-5, AG-6, AG-7, [AG-28](pass-2-review-decisions.md#ag-28-delivery-commit-scope)): one commit per unsplit
card or per approved, independently scoped facet of a split card, with `Audit-Card` and `Triage-Rule` trailers.
One draft PR per applet against `--base yubikit-pass2-base`, starting with OATH, with the Copilot review-fix loop.
The withdrawn C1 guard is not a shared prerequisite.

Security Domain delivery: [draft PR #792](https://github.com/Yubico/Yubico.NET.SDK/pull/792). Hardware qualification remains pending.

Management software and documentation are in draft [PR #791](https://github.com/Yubico/Yubico.NET.SDK/pull/791),
including the two canonical Core metadata referrals. MGMT-IMP-002, MGMT-IMP-011 and the physical
Management qualification matrix remain pending; no device or integration execution was performed.

CTAP2 delivery: [draft PR #790](https://github.com/Yubico/Yubico.NET.SDK/pull/790) against `yubikit-pass2-base`.

## Cards

| Card | Applet | Severity | Classified as | Status | Final call | Recorded in |
| --- | --- | --- | --- | --- | --- | --- |
| OATH-DEC-001 | OATH | Medium (conditional) | A2, T2 candidate | Auto | Python, Android and Rust all keep the session locked and the challenge on device-HMAC mismatch; .NET matches. Docs and a retry test. Veto pass: no veto | AG-25; see also AG-23 |
| OATH-DEC-002 | OATH | Low | A4 | Auto | Android's `setAccessKey` and Rust's `OathAccessKey` admit only 16 bytes; Python forwards any length. Adopt the strict 16-byte check. Veto pass: no veto | AG-25 |
| OATH-DEC-003 | OATH | Low | Interview (G2; no auto rule) | Dennis | lean | AG-20 |
| OATH-DEC-004 | OATH | No assigned severity | T1 | T1 hardware | No code decision; device qualification | classification only |
| OATH-DEC-006 | OATH | No assigned severity | T1 | T1 hardware | No code decision; device qualification | classification only |
| OATH-DEC-007 | OATH | No assigned severity | T1, T2 candidate | T1 hardware | No code decision; device qualification | classification only |
| CORE-OATH-X005 | OATH | Medium (masking clause met) | A5, T2 candidate | Auto | Test CTS disposal, password-byte clearing, and cleanup that keeps the primary failure. Module precedents exist. Unverified sites excluded under E1. Veto pass: no veto | AG-25 |
| Secure-reset qualification (no ID) | OATH | No assigned severity | T1 | T1 hardware | No code decision; device qualification | classification only |
| WA-IMP-001 | WebAuthn | Medium | A2 | Auto | python-fido2, Android and Rust all forward enterprise attestation (python and Android only when GetInfo `ep` is set). Add vendor-facilitated mode 1; document Indirect as passthrough. Veto pass: no veto | AG-25 |
| WA-IMP-002 | WebAuthn | Medium | Interview (G5; T2 c) | Dennis | a | AG-20 |
| WA-IMP-003 | WebAuthn | Low | Interview (G2; T2 b) | Dennis | lean | AG-20 |
| WA-IMP-004 | WebAuthn | Low | A2 (T1 lane) | Auto | python, Android and Rust all make UV mandatory under Discouraged when `alwaysUv` is set. Test guard only. Veto pass: no veto | AG-25 |
| WA-IMP-005 | WebAuthn | Low | Interview (G2, G3) | Auto (AG-19) | M2 | AG-19 |
| WA-IMP-006 | WebAuthn | Medium | A5 | Auto | Ctap2's own decoders split copy and borrow, with the borrowed statement contract documented. Keep the borrowed contract and pin it with a test. Veto pass: no veto | AG-25 |
| WA-IMP-007 | WebAuthn | Low | A5 | Auto | CLAUDE.md ≤16-byte readonly-struct rule and Ctap2's `Guid` AAGUID give the carrier. Default is 16 zero bytes with reflexive equality. Veto pass: no veto | AG-25 |
| WA-IMP-008 | WebAuthn | Low | A1 (T2 d, test-only) | Auto | Mechanical test oracle fix: check retained owner storage after disposal. Veto pass: no veto | AG-25 |
| WA-IMP-009 | WebAuthn | Medium | A2 (T2 e) | Auto | python, Android and Rust negotiate the PIN/UV protocol from GetInfo. python and Rust gate built-in UV on `pinUvAuthToken`. Veto pass: no veto | AG-25 |
| WA-IMP-010 | WebAuthn | Medium | Interview (G2, G3) | Auto (AG-19) | §5.1.4.2 (issuing a credential request): "If allowCredentialDescriptorList has exactly one value, set savedCredentialIds[authenticator] to allowCredentialDescriptorList[0].id"; §5.1.4.1 `credentialIdResult` uses the saved ID / Verified, **lean corrected**: the spec saves the single entry of the list *filtered for that authenticator*, not of the caller's original list. M2 auto with the spec's rule; flagged at the top of the skim | AG-19 |
| WA-IMP-011 | WebAuthn | High | Interview (G1, G2, G3) | Dennis | A / Enforce verified PRF (L3 §10.1.4 MUST). Unit and integration tests where applicable, with L3 references. PR labels the sibling divergence (rule D1) | AG-21 |
| WA-IMP-012 | WebAuthn | High | Interview (G1) | F1 confirmed | Fail with not-supported before creating when required credProtect is unsupported / Yes: python-fido2, Android, Rust and spec agree / Approved 2026-11-15 | AG-10 |
| WA-IMP-013 | WebAuthn | High | Interview (G1; T2 d) | Dennis | B: mechanical clears now; the owner shape follows X3 R1 (plain records with writable caller-owned `Memory<byte>` fields) | AG-20; amended in AG-21 |
| WA-IMP-014 | WebAuthn | Medium | Interview (G3) | Dennis | A: refuse `largeBlob: required` without a discoverable credential before any CTAP traffic, clear NotSupported error; D1 label | AG-24 |
| WA-IMP-015 | WebAuthn | Medium | Interview (G2, G3) | Auto (AG-19) | Vetoed to B (Dennis: side with siblings): accept multi-entry largeBlob writes; deliberate, documented divergence from WebAuthn L3 §10.1.5 | AG-19; amended in AG-20, AG-25 |
| WA-IMP-016 | WebAuthn | Medium | Interview (G2; T2 c) | Auto (AG-19) | §10.1.5 note: "If the read is not successful, largeBlob will be present ... but the blob member will not be present" / Verified. M2 auto | AG-19 |
| WA-IMP-017 | WebAuthn | no severity | A6 | Auto | Storage-only, token-required writes are an acceptable fail-closed contract. Direct route and pinless exception are feature additions. Veto pass: no veto | AG-25 |
| WA-IMP-018 | WebAuthn | Low | A2 | Auto | python, Android and Rust request minPinLength only when `Requested` is true. .NET requests on presence. Veto pass: no veto | AG-25 |
| WA-IMP-019 | WebAuthn | Low | A2 | Auto | python, Android and Rust accept empty generated handles. .NET rejects them in the constructor. TBS rule unchanged. Veto pass: no veto | AG-25; see also AG-11 |
| WA-IMP-020 | WebAuthn | Low | A1 | Auto | Mechanical test fixture change from the card. Keep the extra-field case. Veto pass: no veto | AG-25; see also AG-11 |
| WA-IMP-021 | WebAuthn | Low | A1 | Auto | Mechanical test assertion: returned credential ID must belong to the allow list. Veto pass: no veto | AG-25 |
| WA-WHOLE-CEREMONY (Whole-ceremony lifetime) | WebAuthn | Medium (conditional) | Interview (unmatched; T2 a) | Dennis | A: ceremonies stay caller-serialized and fully awaited, documented; see X4/C1. Separate continuation follow-up: keep the `uint` DTO and fail with a typed error before `GetNext` if the count exceeds the SDK limit of 1024; no truncation. | AG-12; [AG-29](pass-2-review-decisions.md#ag-29-c1-caller-serialization); [AG-31](pass-2-review-decisions.md#ag-31-november-21-follow-up-approvals) |
| Ceremony/platform qualification | WebAuthn | no severity | T1 | T1 hardware | No code decision; device qualification | classification only |
| MGMT-IMP-001 | Management | Low | Interview (gated: G3; T2) | Dennis | (a): keep the NFC-restriction firmware preflight with the 5.7 boundary | AG-22 |
| MGMT-IMP-002 | Management | No assigned defect severity | T1 | T1 hardware | No code decision; device qualification | AG-19 |
| MGMT-IMP-003 | Management | Medium | Interview (gated: G3; T2) | Auto (AG-19) | M1 | AG-19 |
| MGMT-IMP-004 | Management | Medium | Interview (no gate or auto rule; T2) | Dennis | Lean accepted: (a) | AG-12 |
| MGMT-IMP-005 | Management | No assigned production-defect severity | A1 (T2 tag) | Auto | Test-only oracle for a held or failed backend write; production already zeroes after backend completion. Veto pass: no veto | AG-25 |
| MGMT-IMP-006 | Management | Low | A1 | Auto | Concrete-transport rows must pass the preferred connection and assert the session's `ConnectionType`. Veto pass: no veto | AG-25 |
| MGMT-IMP-007 | Management | Medium | Interview (no gate or auto rule; T2; T1 dependency) | Dennis | Lean accepted: (a) | AG-12 |
| MGMT-IMP-008 | Management | Low | A2 (probe kept under A6; T2 tag) | Auto | Python, Android and Rust all gate public device-info reads at 4.1; .NET has no gate. Factory probe stays best-effort. Veto pass: no veto | AG-25 |
| MGMT-IMP-009 | Management | Low | A1 (T2 tag) | Auto | Local `"5.0"` literal is ignored by the parser; the shared reject-or-ignore policy is T2. Veto pass: no veto | AG-25 |
| MGMT-IMP-010 | Management | High | Interview (gated: G1, G3; T2) | Dennis | (b) device-authoritative dispatch (5.6 and SmartCard), documented scope, uncertain outcomes reported without claiming completion; **flipped by X2: the session continues after a confirmed reset** instead of ending | AG-16; amended in AG-20, AG-22 |
| MGMT-IMP-011 | Management | No assigned defect severity | T1 | T1 hardware | No code decision; device qualification | classification only |
| MGMT-IMP-012 | Management | Low (if enabled unchanged) | Interview (no gate or auto rule; T2) | Dennis | (b) keep internal, match Python's bytes (P1 0x11, little-endian payload); AG-15 guard: firmware 5.0.0+ and alpha/beta route to DeviceConfig, never legacy SET_MODE; emitted-bytes unit test | AG-12; amended in AG-15 |
| MGMT-IMP-013 | Management | Low | A6 | Auto | SmartCard-only scope protects the reset menu and is already recorded in the module CLAUDE.md; keep and document. Veto pass: no veto | AG-25 |
| MGMT-IMP-014 | Management | Low | Interview (gated: G5) | Dennis | lean | AG-20 |
| MGMT-IMP-015 | Management | Medium | Interview (gated: G5; T2) | Split: facet auto (AG-19), rest Dennis | a / `ConsoleCredentialReader` with `CredentialReaderOptions.ForHexKey(16)` | AG-21 |
| MGMT-IMP-016 | Management | Low | A5 (T2 tag) | Auto | Unified CLI and OATH tool map cancellation to `ExitCode.UserCancelled`; token forwarding follows. Veto pass: no veto | AG-25 |
| MGMT-IMP-017 | Management | Medium (FIPS); Low (reboot, flags) | A1 + A6 | Auto | Label corrections are literal; the reboot flag is already documented as a request, so keep the member. Veto pass: no veto | AG-25 |
| MGMT-XREF-001 | Management | Medium | Interview (no gate or auto rule; Core-owned; T2) | Dennis | Lean accepted: (a) | AG-12 |
| MGMT-XREF-002 | Management | Low | Interview (gated: G5; Core-owned; T2) | Auto (AG-19) | A2 (reclassified; Python, Android and Rust all mask the base form factor) | AG-19 |
| CORE-MGMT-X003 | Management | Low | A1 | Auto | Bound oracle through public `CreateAsync` after failed init; reset-wire extension excluded (waits for MGMT-IMP-010). Veto pass: no veto | AG-25 |
| Management qualification | Management | No assigned defect severity | T1 | T1 hardware | No code decision; device qualification | classification only |
| CTAP-IMP-001 | CTAP2 | Medium | Interview (G3) | Auto (AG-19) | M1 | AG-19 |
| CTAP-IMP-002 | CTAP2 | Medium | Interview (no rule) | Dennis | (a) for the primary source with the optimistic fallback; Firmware source per AG-14: cached device info first, then the applet's own read; 0.x or a failed read (Q6) is alpha/beta and admits every gate | AG-12; amended in AG-14 |
| CTAP-IMP-003 | CTAP2 | Medium | Interview (no rule) | Dennis | Enforce size limit / Lean (a) | AG-12 |
| CTAP-IMP-004 | CTAP2 | Medium | Interview (no rule) | Dennis | Lean, follow SDK patterns / Lean (a): single-use builder, wipe after send or failure, reuse throws (OATH pattern) | AG-12 |
| CTAP-IMP-005 | CTAP2 | Medium | A1 | Auto | No-brainer yes. Clear owned V1 and V2 buffers on exceptional exits, as Android V2 and the PIV PIN protocol do. Veto pass: no veto | AG-25 |
| CTAP-IMP-006 | CTAP2 | Medium | Interview (G2) | Split: facet auto (AG-19), rest Dennis | lean / Current PIN encodability only; deliberate departure from the §6.5.1 "PINs used" reading | AG-20 |
| CTAP-IMP-007 | CTAP2 | Low | Interview (G2, G3) | Split: facet auto (AG-19), rest Dennis | lean | AG-20 |
| CTAP-IMP-008 | CTAP2 | Medium | Interview (G3) | Auto (AG-19) | M1 | AG-19 |
| CTAP-IMP-009 | CTAP2 | High | Interview (G1) | F1 confirmed | Validate decrypted PIN token length, zero rejected data, then throw / No: Android does not check / Approved 2026-11-15 (Android gap accepted; follow python-fido2 and Rust) | AG-10 |
| CTAP-IMP-010 | CTAP2 | Medium | Interview (G3) | Dennis | c / Keep the legacy fallback and document it. Clarification for the record: the choice is driven by GetInfo option `pinUvAuthToken` (`ClientPin.cs:409-416`), not by the PIN/UV auth protocol version (1 or 2) | AG-21 |
| CTAP-IMP-011 | CTAP2 | Medium | A2 | Auto | No-brainer yes. Spec §6.1.1 and §6.2.1 say platforms MUST NOT send uv with pinUvAuthParam; all clients omit it. Veto pass: no veto | AG-25 |
| CTAP-IMP-012 | CTAP2 | High | Interview (G1) | Dennis | Approved / (a) Standard bio subcommands 1 to 7, modality-prefixed authenticated transcript, timeout in parameter key 3; preview routing separate | AG-16 |
| CTAP-IMP-013 | CTAP2 | Medium | A3 | Auto | Python-fido2 precedent; Android and Rust agree. Decode templateInfos; InvalidOption means empty; other statuses propagate. T2. Veto pass: no veto | AG-25 |
| CTAP-IMP-014 | CTAP2 | Medium | A2 | Auto | Begin and next results split with the begin template ID carried; required fields per operation; completion at remaining 0. Veto pass: no veto | AG-25 |
| CTAP-IMP-015 | CTAP2 | Medium | Interview (no rule) | Dennis | Follow python-fido2 / Adopt python-fido2 names, including 0x0C "database full", with a code comment explaining the divergence from the spec's "unused" / Overrides Sia's lean (a) on 0x0C | AG-12 |
| CTAP-IMP-016 | CTAP2 | Low | Interview (G5) | Dennis | a / `SetAlwaysUvAsync(bool)` with read-before-toggle, plus optional-length `SetMinPinLength` | AG-21 |
| CTAP-IMP-018 | CTAP2 | Low | A5 (T2) | Auto | Reject use after Dispose with the ThrowIfDisposed pattern already used in Ctap2Session and Oath. Borrowed owners untouched. Veto pass: no veto | AG-25 |
| CTAP-IMP-019 | CTAP2 | Medium | Interview (G3) | Auto (AG-19) | M1 | AG-19 |
| CTAP-IMP-020 | CTAP2 | Medium | Interview (no rule) | Dennis | Normalize by default / `normalizePin` defaults to true; tests opt out explicitly; document it / Overrides Sia's lean (a); see AG-13 | AG-12 |
| CTAP-IMP-021 | CTAP2 | Medium | Interview (no rule) | Dennis | Start from known state / AG-13 rule: restore before the test, not teardown after; exceptions explicit / Overrides Sia's lean (a) | AG-12 |
| CTAP-IMP-022 | CTAP2 | none assigned | T1 | T1 hardware | No code decision; device qualification | classification only |
| CTAP-IMP-023 | CTAP2 | Medium | A4 (T2) | Auto | Siblings split. Stricter option: map only 6D00, 6A82 and 6E00 to not-supported (Android); reject empty responses (Rust). Veto pass: no veto | AG-25 |
| CTAP-IMP-024 | CTAP2 | Medium | A1 | Auto | No-brainer yes. Finally-clear named admin temporaries; Android changePin does this; borrowed token untouched. Veto pass: no veto | AG-25 |
| CTAP-IMP-025 | CTAP2 | Low | A1 | Auto | No-brainer yes. AAGUID conversion must be unconditional; siblings treat AAGUID as raw RFC-order bytes. Veto pass: no veto | AG-25 |
| CTAP-IMP-026 | CTAP2 | Low | Interview (no rule) | Dennis | Lean accepted: (a) | AG-12 |
| CTAP-IMP-027 | CTAP2 | Medium | A5 (T2; historical) | Dennis | Apply X3/R1: `MakeCredentialResponse` and `GetAssertionResponse` stay non-disposable. Return caller-owned writable `Memory<byte>` for sensitive output, retain SDK cleanup on decode failure, and eliminate inaccessible sensitive copies. Exact raw-envelope API remains open. | [AG-26](pass-2-review-decisions.md#ag-26-ctap-imp-027-ownership); supersedes the earlier A5 proposal |
| CTAP-IMP-028 | CTAP2 | Medium | Interview (no rule) | Dennis | Lean accepted: (a) | AG-12 |
| CTAP-IMP-029 | CTAP2 | High | Interview (G1) | Dennis | (a): validate curve, coordinate length and on-curve points before ARKG derivation; surface behind `[Experimental]` | AG-11 |
| CTAP-IMP-030 | CTAP2 | Medium | Interview (G3) | Dennis | Defer | AG-20 |
| CTAP-IMP-031 | CTAP2 | Low | A3 (A4 for empty and oversize) | Auto | Device-advertised credBlob capacity is shared by python-fido2 and Android; Rust errors on oversize; empty rejected. Veto pass: no veto | AG-25 |
| CTAP-IMP-032 | CTAP2 | Medium | Interview (no rule) | Dennis | Lean accepted: (a) | AG-12 |
| CTAP-IMP-033 | CTAP2 | Low | A3 | Auto | Ceremony-specific hmac-secret outputs in python-fido2 and Android: registration bool, assertion bytes. Veto pass: no veto | AG-25 |
| CTAP-IMP-034 | CTAP2 | Medium | Interview (no rule) | Dennis | Lean accepted: (a), confirm strict reader behaviour first | AG-12 |
| CTAP-IMP-035 | CTAP2 | Medium | A1 | Auto | No-brainer yes. Finally-clear untransferred temporaries in the extension builder and membership probe. Veto pass: no veto | AG-25 |
| CTAP-IMP-036 | CTAP2 | Medium | Interview (G2, G3) | Auto (AG-19) | §6.10.2: hash mismatch means "the configuration is corrupt and the platform MUST discard it and act as if the initial serialized large-blob array was received" / Verified. M2 for act-as-initial; fragment typing and the iteration cap are M1 | AG-19 |
| CTAP-IMP-037 | CTAP2 | Medium | Interview (no rule) | Dennis | Follow Python and Rust; no write-more-than-read / Lean (a): 64-bit original size, symmetric 1 MiB cap | AG-12 |
| CTAP-IMP-038 | CTAP2 | Medium | Interview (no rule) | Dennis | Most forward-compatible; Rust / (a): keep Try contract, return null with a distinct diagnostic (Rust warns and omits, `sign.rs:421–437`) / Same as lean | AG-12 |
| CTAP-IMP-039 | CTAP2 | Medium | A2 | Auto | Python-fido2, Rust and Android take the derived algorithm from device output. No sibling uses the seed marker. Veto pass: no veto | AG-25; see also AG-11 |
| CTAP-IMP-040 | CTAP2 | Medium | Interview (G3) | Dennis | (a): validate COSE_Sign_Args as one map, reject empty TBS and key handle; surface behind `[Experimental]` | AG-11 |
| CTAP-IMP-041 | CTAP2 | Medium | Interview (no rule) | Dennis | Keep if internal; if public it must be correct / (a): strict DER plus documented P-256/SHA-256 raw-message limit / Verified: `PreviewSignDerivedKey` and `VerifySignature` are **public** (`PreviewSignDerivedKey.cs:48, 121`), so Dennis's "must be correct" branch applies | AG-12 |
| CTAP-IMP-042 | CTAP2 | Medium | Interview (no rule) | Dennis | Low value; open to a same-or-better suggestion / Remove the env-var export (AG-15, Q4 yes) / Sia's suggestion: remove the export; the test already decrypts the blob in-process (`Ctap2LargeBlobAuditReproTests.cs:~90–106`), which is the guarantee | AG-12 |
| CTAP-IMP-043 | CTAP2 | Medium | Interview (G3) | Auto (AG-19) | M1 | AG-19 |
| CTAP-IMP-044 | CTAP2 | none assigned | A6 | Auto | The protocol-demo boundary is already documented in the FidoTool README and MakeCredential.cs. Keep and document. Veto pass: no veto | AG-25 |
| CTAP-IMP-045 | CTAP2 | Low | A1 | Auto | No-brainer yes. Missing flag values and unknown verbs fail; help runs before device selection; unknown sensor kind is not renamed. Veto pass: no veto | AG-25 |
| CTAP-IMP-046 | CTAP2 | Medium | Interview (no rule) | Dennis | Lean accepted: (a) | AG-12 |
| CTAP-IMP-047 | CTAP2 | Medium | Interview (G5, no rule) | Dennis | a | AG-20 |
| CTAP-IMP-048 | CTAP2 | High | Interview (G1) | Dennis | Retain the selected device through reinsertion; verify a retained serial-less device with a GetInfo fingerprint (not unique identity). If the old macOS entry is invalid, rediscover only with exactly one known serial match on the same transport; no automatic serial-less fallback. Verify and reset on the same open connection. | AG-16; [AG-31](pass-2-review-decisions.md#ag-31-november-21-follow-up-approvals) |
| CTAP-DEC-002 | CTAP2 | none assigned | Interview (no rule) | Dennis | Lean, follow siblings / (a) | AG-12 |
| CTAP-DEC-003 | CTAP2 | none assigned | T1 | T1 hardware | No code decision; device qualification | classification only |
| CORE-CTAP-X020 | CTAP2 | Medium | Interview (no rule) | Dennis | Lean accepted: (a) | AG-12 |
| SD-IMP-001 | Security Domain | Medium | Interview (G3, G5) | Dennis | A | AG-20 |
| SD-IMP-002 | Security Domain | High (potential) | Interview (G1) | F1 confirmed | Reject SCP03 DELETE with key version 0 before sending / Yes: all three siblings reject / Approved 2026-11-15 | AG-10 |
| SD-IMP-003 | Security Domain | Medium | A2 (secondary A6) | Auto | Serials are not normalized to positive DER INTEGER, as all three siblings do; SCP11c counter stays documented as unsupported. Veto pass: no veto | AG-25 |
| SD-IMP-004 | Security Domain | Medium | Interview (G3) | Auto (AG-19) | M1 | AG-19 |
| SD-IMP-005 | Security Domain | Medium | Interview (G5) | Dennis | Continue after reset (X2), re-authenticating with the restored default SCP03 keys; Q12 (a): always, whatever channel was used before | AG-22; AG-23 (Q12) |
| SD-IMP-006 | Security Domain | Medium | Interview (G3) | Split: facet auto (AG-19), rest Dennis | A: restrict GENERATE to 0x11, 0x13, 0x15 before sending and require exact SCP03 KCV response/KVN. Q11 (AG-23): Security Domain-only `KeyReplacement.New` or `Of(0x01–0xFE)` is required on four overloads; it supersedes the numeric argument. | AG-19–23 (AG-20 amended in AG-21); exact Q11 shape in [AG-30](pass-2-review-decisions.md#ag-30-q11-keyreplacement-exact-shape) |
| SD-IMP-007 | Security Domain | Low test debt, Medium assurance | A5 | Auto | Unit-level oracles follow in-repo specific-exception, status and disposal-probe tests; device facets move to T1. Veto pass: no veto | AG-25 |
| SD-IMP-008 | Security Domain | Medium (real secrets), Low (fixtures) | A5 (secondary A2) | Auto | Local KCV and AES temporaries zeroed as the repo already does; explicit P-256 curve byte matches all siblings. Fixture cleanup excluded. Veto pass: no veto | AG-25 |
| SD-DEP-001 | Security Domain | Qualification, no defect severity | T1 | T1 hardware | No code decision; device qualification | classification only |
| SD-DEP-002 | Security Domain | Medium conditional | A6 | Auto | Caller-owned SCP parameters are already the documented contract; keep, document and pin. Veto pass: no veto | AG-25 |
| SD-DEP-005 | Security Domain | Medium conditional (privacy) | Interview (G3, G5) | Dennis | A | AG-20 |
| CORE-SD-X003 | Security Domain | Medium | A5 (secondary A6) | Auto | KCV-bearing PUT KEY response should be cleared internally, as PIV does; public GET DATA owner documented. Veto pass: no veto | AG-25 |
| CORE-SD-X007 | Security Domain | Qualification, no applet severity | Interview (G2) | Dennis | Qualification / A | AG-20 |
| Device and producer qualification | Security Domain | Qualification, no defect severity | T1 | T1 hardware | No code decision; device qualification | classification only |
| OTP-IMP-001 | YubiOTP | Medium | A1 | Auto | HOTP divisor 16 and range; Python, Android and Rust all divide by 16. Firmware qualification is T1. Veto pass: no veto | AG-25 |
| OTP-IMP-002 | YubiOTP | Medium | Interview (G5) | Dennis | A | AG-20 |
| OTP-IMP-003 | YubiOTP | Medium | Interview (no rule) | Dennis | Lean accepted: B | AG-12 |
| CORE-OTP-X004 | YubiOTP | Medium | Interview (G5) | Dennis | A | AG-20 |
| OTP-IMP-004 | YubiOTP | Low | Interview (G5) | Auto (AG-19) | A2 (reclassified; the lean, stay silent, is what all three siblings do; Python's SmartCard `send_and_receive` has no prompt or keepalive, `yubikit/yubiotp.py` @4ca60f70 checked) | AG-19 |
| OTP-IMP-005 | YubiOTP | Medium | Interview (G3) | Split: facet auto (AG-19), rest Dennis | **A (against lean)** / Narrow now to the Python/Rust sequence rule; 5.4.3 boundary stays on T1 | AG-20 |
| OTP-IMP-006 | YubiOTP | Medium | Interview (no rule) | Dennis | Python and Rust firmware minimums, alpha/beta still admitted; drops 2.1 HOTP and 2.0 static password support; Firmware source per AG-14: cached device info first, then the applet's own read; 0.x or a failed read (Q6) is alpha/beta and admits every gate | AG-12; amended in AG-14 |
| OTP-IMP-007 | YubiOTP | Medium | Interview (G5) | Split: facet auto (AG-19), rest Dennis | A | AG-20 |
| OTP-IMP-008 | YubiOTP | Low | Interview (G5) | Split: facet auto (AG-19), rest Dennis | B variant / Reject null as V1 does, add explicit `DisableNdefAsync`, case-insensitive URI prefixes as V1 | AG-21 |
| OTP-IMP-009 | YubiOTP | Medium | A1 | Auto | Translate with the existing layout constructor; Python ykman and Rust CLI do the same. Input policy unchanged. Veto pass: no veto | AG-25 |
| OTP-IMP-010 | YubiOTP | Low | Interview (G5) | Dennis | lean | AG-20 |
| OTP-IMP-011 | YubiOTP | Medium | Interview (no rule) | Dennis | Reset before each test / AG-13 rule / Overrides Sia's lean (A/B) | AG-12 |
| OTP-IMP-012 | YubiOTP | none assigned | T1 | T1 hardware | No code decision; device qualification | classification only |
| OTP-DEP-005 | YubiOTP | Low | A6 | Auto | Inventory resolved (device info via Management; no typed OTP FIPS query or legacy mutator). Docs only. Veto pass: no veto | AG-25 |
| HSM-IMP-001 | YubiHSM Auth | Medium | Interview (G3, G5) | Dennis | A | AG-20 |
| HSM-IMP-002 | YubiHSM Auth | High | Interview (G1) | Dennis | Approved A on 2026-11-18, then **dropped** under sample rule S1 (Q9, AG-22) | AG-16; amended in AG-21, AG-22 |
| HSM-IMP-003 | YubiHSM Auth | Medium | Interview (G5) | Auto (AG-19) | M1 | AG-19 |
| HSM-IMP-004 | YubiHSM Auth | Medium | Auto (A1 + A5) | Auto | Symmetric-only chooser (A1) and console presence prompt as the PIV tool wires it (A5); asymmetric dispatch is out of scope. Veto pass: no veto | AG-25 |
| HSM-IMP-005 | YubiHSM Auth | High | Interview (G1) | Dennis | B, revised by Q9: hidden prompts through the existing `ConsoleCredentialReader`, no diagnostics flag, keys printed with the demo-only label (S1) | AG-16; amended in AG-21, AG-22 |
| HSM-IMP-006 | YubiHSM Auth | Medium | Interview (no gate, no single rule) | Dennis | Skip cleanup; fix circular crypto tests / B: independent PBKDF2 vectors computed outside .NET with Python `hashlib.pbkdf2_hmac`, command recorded next to each vector; no cleanup work (AG-15, Q5) / Overrides Sia's lean A | AG-12 |
| HSM-IMP-007 | YubiHSM Auth | Medium | Interview (no gate, no single rule) | Dennis | Lean accepted: A | AG-12 |
| HSM-IMP-008 | YubiHSM Auth | High | Interview (G1) | Dennis | Rust shape / >= 48 bytes, first 48 as keys, zero the whole buffer, log length only. Hardware capture optional | AG-21 |
| CORE-HSM-X001 | YubiHSM Auth | Medium (risk) | Interview (G5, T2) | Dennis | B: session continues after reset and refreshes SCP state through the shared helper (X2) | AG-22 |
| CORE-HSM-X002 | YubiHSM Auth | Low (risk) | Auto (A5) | Auto | Legacy zeroing becomes `ApduResponse.Clear()` as in PIV; partial SessionKeys cleanup added; lower copies stay with Core. Veto pass: no veto | AG-25 |
| CORE-HSM-X003 | YubiHSM Auth | Medium (risk) | Interview (no gate, T2) | Dennis | Lean accepted: B, decide properly under X4 | AG-12 |
| CORE-HSM-X004 | YubiHSM Auth | Low (risk) | Interview (no gate, T2) | Dennis | A, plus firmware from `CachedDeviceInfo` when present; Firmware source per AG-14: cached device info first, then the applet's own read; 0.x or a failed read (Q6) is alpha/beta and admits every gate | AG-12; amended in AG-14 |
| CORE-HSM-X005 | YubiHSM Auth | Folded | Folded into HSM-IMP-008 | Folded | Folded into HSM-IMP-008 (no separate body in the master) | see HSM-IMP-008 |
| QUAL-HSM (no ID) | YubiHSM Auth | None assigned | T1 | T1 hardware | No code decision; device qualification | classification only |
| PGP-IMP-001 | OpenPGP | High | Interview (G1) | F1 confirmed | Use the user PIN identity in both verify modes / Yes: Python, Android, Rust and spec agree / Approved 2026-11-15 | AG-10 |
| PGP-IMP-002 | OpenPGP | High | Interview (G1) | Dennis | Approved A / A. Low-level F9 write, documented caller procedure, fresh-session readback; no new public helper | AG-16 |
| PGP-IMP-003 | OpenPGP | High | Interview (G1) | Dennis | Approved / A. Drop the KDF cache; reread F9 per PIN or Reset-Code operation; propagate F9 errors; clear encoded copies in finally | AG-16 |
| PGP-IMP-005 | OpenPGP | High | Interview (G1, G3) | Dennis | **Flipped by X2: the session continues after reset** (reselect, clear cached and authenticated state, re-read, re-run SCP; failed re-handshake ends the session with a clear error). Checks on TERMINATE and ACTIVATE and fresh readback stay; the 'keep the session alive' follow-up becomes the fix | AG-16; amended in AG-20, AG-22 |
| PGP-IMP-007 | OpenPGP | Medium | A1 | Auto | Wrap the X25519 DECIPHER input as A6 {7F49 {86}}. Python, Android and Rust agree; §7.2.11. Veto pass: no veto | AG-25 |
| PGP-IMP-009 | OpenPGP | High | Interview (G1, G3) | Dennis | Defer / No change this pass | AG-20 |
| PGP-IMP-010 | OpenPGP | Medium | Interview (G5) | Split: facet auto (AG-19), rest Dennis | A / Built-in algorithm list on old keys, no inferred flag | AG-21 |
| PGP-IMP-011 | OpenPGP | High | Interview (G1) | Dennis | A / Conservative ROCA [4.2.0, 4.3.5); settles X13 | AG-20 |
| PGP-IMP-013 | OpenPGP | Medium | A1 | Auto | Nest the ATT CRT as B6 03 84 01 81. Python, Rust and Android agree. Veto pass: no veto | AG-25 |
| PGP-IMP-014 | OpenPGP | Medium | A4 | Auto | Siblings split on NEO deletion; the stricter refusal is chosen. CRT-with-modulus label is A2. Veto pass: no veto | AG-25 |
| PGP-IMP-015 | OpenPGP | High (part) | Interview (G1) | Dennis | A / Via existing `OpenPgpCommand.GetPin` (ConsoleCredentialReader); `KeysAttestCommand` never calls it today | AG-20 |
| PGP-IMP-016 | OpenPGP | Medium | Interview (no gate, no rule) | Dennis | Lean accepted: A | AG-12 |
| PGP-IMP-018 | OpenPGP | Medium (Low timestamp) | Interview (G3, G5) | Auto (AG-19) | M1 | AG-19 |
| PGP-IMP-019 | OpenPGP | Medium | Interview (G3) | Dennis | A | AG-20 |
| PGP-CORE-001 | OpenPGP | Medium | Interview (no gate, no rule; T2) | Dennis | Lean accepted: A | AG-12 |
| PGP-CORE-003 | OpenPGP | Medium | Interview (no gate, no clean rule; E1) | Dennis | Prefer `CachedDeviceInfo.FirmwareVersion`, GET VERSION as fallback; Firmware source per AG-14: cached device info first, then the applet's own read; 0.x or a failed read (Q6) is alpha/beta and admits every gate | AG-12; amended in AG-14 |
| PGP-CORE-004 | OpenPGP | Medium (Low delay) | Interview (G5; T2) | Dennis | A / Verified: the advisory UIF read builds a `UserPresenceContext` (Basis PolicyRequires / PolicyMayRequire) for Core `IUserPresencePrompt` (`OpenPgpSession.cs:327-378`), the same primitive PIV, OATH, OTP, HSM Auth and CTAP2 use. Engineer adds a test: failed UIF read plus throwing logger still notifies with PolicyMayRequire and signs | AG-21 |
| Physical/chronology qualification | OpenPGP | none assigned | T1 | T1 hardware | No code decision; device qualification | classification only |
| PIVIMP-001 | PIV | High | Interview (G1; T2) | Dennis | A | AG-20 |
| PIVIMP-002 | PIV | High | Interview (G1, G3; G5 on legacy derived) | Dennis | A, Python shape / Raw calls stay raw; opt-in coherent helpers modelled on ykman `pivman_set_mgm_key` / `pivman_change_pin` / `pivman_set_pin_attempts` (@4ca60f70 `ykman/piv.py:314-420`). Derived mode follows Python: management-key rotation clears the salt, PIN change re-derives with a fresh salt. Rust `crates/ykman/src/piv.rs:96-139` @90940e9b has no derived-mode handling, so Python is the only precedent there. Tests: Q10. The retry helper refuses legacy derived mode before mutation; PIN-only API design remains pending the Rust/Python comparison and Astra review. | AG-21; [AG-31](pass-2-review-decisions.md#ag-31-november-21-follow-up-approvals) |
| PIVIMP-003 | PIV | Qualification | T1 | T1 hardware | No code decision; device qualification | classification only |
| PIVIMP-004 | PIV | Medium | Interview | Dennis | Lean accepted: A | AG-12 |
| PIVIMP-005 | PIV | High | Interview (G1) | F1 confirmed | Sample tool signs the message, not the hash / Open choice: RSA padding, Sia leans PKCS#1 v1.5 only (Rust) vs also PSS (Python) / Approved 2026-11-15 with Sia's lean: PKCS#1 v1.5 only, no PSS (Rust precedent) | AG-10 |
| PIVIMP-006a | PIV | High | Interview (G1; T2) | Dennis | A, reduced by Q9: keep only the demo-only label (S1); no added cleanup scopes | AG-20; amended in AG-21, AG-22 |
| PIVIMP-006b | PIV | High | Interview (G1; T2) | Dennis | C + S1 / Keep the sample's result shape; no clearing. Add the demo-only warning (rule S1) | AG-21 |
| PIVIMP-006c | PIV | Medium | Auto (A1) | Auto | Export field, CSR PIN check and local disposal; PIN check matches Python and Rust CLIs. Broader result and callback scopes stay open. Veto pass: no veto | AG-25 |
| PIVIMP-006d | PIV | Medium | Interview (T2) | Dennis | Lean accepted: A | AG-12 |
| SAMPLE-A004 | PIV | Low | Auto (A1) | Auto | Attested certificate is never disposed; `using` matches the tool's own precedent. Veto pass: no veto | AG-25 |
| PIVIMP-007a | PIV | Medium | Interview (G3, G5; T2 candidate) | Auto (AG-19) | M1 | AG-19 |
| PIVIMP-007b | PIV | Medium | Interview (G3, G5) | Split: facet auto (AG-19), rest Dennis | 64 KiB / Documented SDK safety limit | AG-21 |
| PIVIMP-008 | PIV | Medium | Interview (T2; split) | Dennis | A (2026-11-17, chat) / A: fix the failure-path drain now; defer release expectations and fixture floors (AG-15, Q1) | AG-12 |
| PIVIMP-009 | PIV | Medium | Interview (G5) | Dennis | A by rule: stay V1-codec-compatible (little-endian trimmed timestamp, as V1 and V2 already do) | AG-20; amended in AG-21 |
| PIVIMP-010 | PIV | Medium | Auto (A4) | Auto | Android masks IDs to 24 bits; Python and Rust do not alias; reject out-of-range IDs before transmit. Veto pass: no veto | AG-25 |
| PIVCORE-002 | PIV | High | Interview (G1; T2) | Dennis | Documentation only: general Core caller-serialization contract and PIV-specific guidance in the PIV PR. No new runtime guard or bug fix is implied. | AG-21; superseded by [AG-29](pass-2-review-decisions.md#ag-29-c1-caller-serialization) |
| PIV-QUAL | PIV | Qualification | T1 | T1 hardware | No code decision; device qualification | classification only |
