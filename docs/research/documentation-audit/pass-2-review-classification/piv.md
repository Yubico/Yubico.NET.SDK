# PIV: Pass 2 classification
Classifier: one dedicated subagent, 2026-11-14. Source: master lines 7156–8034 at 52d34ca65. Policy: pass-2-review-policy.md (approved). Final per-card calls: `../pass-2-review-ledger.md`.
Sibling pins: yubikey-manager @ 4ca60f70 (`yubikit/`, `ykman/` paths); yubikit-android @ f4626856 (`piv/src/main/java/com/yubico/yubikit/piv/`, abbreviated `piv/…`); yubikey-manager-rust-auto @ 90940e9b (`crates/`). python-fido2 not used.

## Summary
| Disposition | Count | Cards |
| --- | ---: | --- |
| A1 No-brainer | 2 | SAMPLE-A004; PIVIMP-006c (scope-limited) |
| A2 Sibling consensus | 0 | none (consensus noted for PIVIMP-001 and -005; both gated by G1) |
| A3 Python precedent, partial confluence | 0 | none (also holds for the PIN step of PIVIMP-006c; A1 matches first) |
| A4 Split siblings | 1 | PIVIMP-010 |
| A5 Internal precedent | 0 | none (precedents cited for SAMPLE-A004 and PIVIMP-006c) |
| A6 Keep and document | 0 | none |
| Interview (gated, or no auto rule fits) | 12 | High: PIVIMP-001, -002, -005, -006a, -006b, PIVCORE-002. Medium: PIVIMP-004, -006d, -007a, -007b, -008, -009 |
| T1 Qualification | 2 | PIVIMP-003; PIV-QUAL (physical, profile and restoration lane) |
| T2 tag (count) | 7 | PIVIMP-001, -006a, -006b, -006d, -007a (candidate), -008, PIVCORE-002 |

Interpretation used: a card with no gate hit and no matching auto rule goes to the interview queue as decision-bearing. Where a card has a no-brainer half and a decision half, the card stays in the interview and names the auto-able half.

## Index
| Card | Severity | Disposition | One-line rationale |
| --- | --- | --- | --- |
| PIVIMP-001 | High | Interview (G1; T2) | No Bio preflight before blocking PIN and PUK; all three siblings preflight; product routing undecided. |
| PIVIMP-002 | High | Interview (G1, G3; G5 on legacy derived) | Partial writes are shared by Python and Rust; coherence model and legacy derived default open. |
| PIVIMP-003 | Qualification | T1 | Firmware, FIPS, serial and AES gates need device evidence; no defect severity. |
| PIVIMP-004 | Medium | Interview | Fixed versus general object model; KeyHistory F3 rule is a spec input. No sibling parser exists. |
| PIVIMP-005 | High | Interview (G1) | Framing fix matches all three siblings; RSA padding scope is the decision. |
| PIVIMP-006a | High | Interview (G1; T2) | Cleanup scopes have precedent; printed-key escrow policy is the decision. |
| PIVIMP-006b | High | Interview (G1; T2) | Plaintext carrier shape is the decision. |
| PIVIMP-006c | Medium | Auto (A1) | Export field, CSR PIN check and local disposal; PIN check matches Python and Rust CLIs. Broader result and callback scopes stay open. |
| PIVIMP-006d | Medium | Interview (T2) | Cancellation semantics are cross-cutting; the "touch timeout" label has no confirmed basis. |
| SAMPLE-A004 | Low | Auto (A1) | Attested certificate is never disposed; `using` matches the tool's own precedent. |
| PIVIMP-007a | Medium | Interview (G3, G5; T2 candidate) | All siblings accept unshaped responses; strict admission is a safety choice over every sibling. |
| PIVIMP-007b | Medium | Interview (G3, G5) | Decompression is unbounded in all siblings; ceiling value and Net iD zlib variant are open. |
| PIVIMP-008 | Medium | Interview (T2; split) | Failure-path drain is no-brainer; release expectations and fixture floors are open. |
| PIVIMP-009 | Medium | Interview (G5) | Only Python encodes the date (big-endian); no sibling confluence; migration risk. |
| PIVIMP-010 | Medium | Auto (A4) | Android masks IDs to 24 bits; Python and Rust do not alias; reject out-of-range IDs before transmit. |
| PIVCORE-002 | High | Interview (G1; T2; historical proposal) | Whole-workflow admission proposal superseded by [AG-29](../pass-2-review-decisions.md#ag-29-c1-caller-serialization): documentation only. |
| PIV-QUAL | Qualification | T1 | Physical, process and restoration lane; needs authorized devices. |

## Interview queue (sorted High, then Medium, then Low)

### PIVIMP-001: Preflight reset before consuming retries
- Gate(s) and why: G1 (High). Note: all three siblings check Bio before blocking, so the preflight itself has sibling consensus. G1 still applies.
- Severity / certainty: High / source-confirmed. TODO at `src/Piv/src/PivSession.cs:189`; PIN and PUK blocking starts at 191–196 with no preflight.
- The decision Dennis must make: Which preflight shape should PIV reset use before it blocks PIN and PUK?
- Options:
  - A. PIV-local Bio preflight only. Refuse with a precise error and send no retry APDU. Ignore only "unsupported" metadata; other errors fail. **Sia's lean.** Python and Android swallow only "unsupported" (`yubikit/piv.py:744-750`; `piv/…/PivSession.java:293-301`). Rust swallows every metadata error (`crates/yubikit/src/piv.rs:1265-1273`), which is the outlier. Product routing stays with MGMT-IMP-010.
  - B. Shared product router now, with Management reset. Larger change; waits on MGMT-IMP-010.
  - C. Keep the current order. Not recommended: the TODO names the gap and all three siblings preflight.
- Evidence: `src/Piv/src/PivSession.cs:189-205`, `:207-217` (conservative post-reset defaults already in place; not reopened). Python `yubikit/piv.py:744-750` @4ca60f70; `ykman/_cli/piv.py:255` (CLI `reset_blocked` check). Android `piv/…/PivSession.java:293-303` @f4626856. Rust `crates/yubikit/src/piv.rs:1265-1273` @90940e9b.

### PIVIMP-002: PIN-only coherence and recovery responsibility
- Gate(s) and why: G1 (High). G3: partial writes are shared. Python `ykman/piv.py:314-377` writes the key, then PIVMAN data, then the protected object, with no transaction. Rust `crates/ykman/src/piv.rs:96-139` does the same (`set_management_key` at 113, then `put_pivman_data` at 133). Precedent does not count. G5 on the legacy derived default: Python clears the derived salt on rotation (`ykman/piv.py:345-348`).
- Severity / certainty: High / sequential writes confirmed; the desired coherence model is unsettled.
- The decision Dennis must make: For PIN-only rotation, should raw PIN, PUK and management-key calls stay raw with caller coordination, or should the high-level rotation coordinate recovery objects?
- Options:
  - A. Raw calls stay raw. Add explicit opt-in coherent workflows that report what may have applied on each failure. **Sia's lean** for the coherence model: it matches both siblings' helper style, and no card transaction is atomic. For legacy derived users I lean to PRESERVE_DERIVED, which departs from Python's salt clearing; Dennis should weigh that divergence (G5).
  - B. High-level rotation refuses, or requires explicit coordination, when protected or derived recovery metadata exists. Stricter; raw-path callers must opt in.
  - C. Promise an atomic-looking workflow. Rejected: no atomic card transaction exists.
- Evidence: `src/Piv/src/PivSession.cs:1091-1099` (verified), `:1100-1102` (SetAdminData after `EnsureManagementKeyAuthenticated`). Python `ykman/piv.py:314-377`, `:378` (`pivman_change_pin`), `:401` @4ca60f70. Rust `crates/ykman/src/piv.rs:96-139` @90940e9b. PR745 resolutions (active-key authentication, default-before-delete, stale-derived witness) stay intact.

### PIVIMP-005: Sign the original message with algorithm-specific framing
- Gate(s) and why: G1 (High). Sibling framing already agrees (A2 consensus: Python, Android and Rust sign the original message). The gate still applies.
- Severity / certainty: High / source-confirmed in `Signing.cs`; historically device-confirmed by frozen probes (RSA original-message verification failed; Ed25519 verified the hash). Tool behaviour, not a raw SDK defect.
- The decision Dennis must make: Which RSA padding does the PivTool signing helper offer?
- Options:
  - A. PKCS#1 v1.5 only. **Sia's lean.** Matches Rust (`pkcs1v15_pad`, `crates/yubikit/src/piv.rs:2630-2633`) and the certificate helper already in this tool (`src/Piv/examples/PivTool/PivExamples/Certificates.cs:462-468`). Ed25519 signs the raw message and rejects any hash option; X25519 is refused before signing. PSS deferred.
  - B. PKCS#1 v1.5 plus an explicit PSS option. Matches Python's padding parameter (`yubikit/piv.py:560-567`). Adds a CLI choice and a PSS verification path.
  - C. PSS only. Not recommended: no sibling defaults to it for this helper.
- Evidence: `src/Piv/examples/PivTool/PivExamples/Signing.cs:97-103`. Python `yubikit/piv.py:546-567` (Ed25519 passthrough 547-548) @4ca60f70. Android `piv/…/jca/PivRsaSignatureSpi.java:115-127`, `piv/…/jca/PivEcSignatureSpi.java:136-137`, `piv/…/jca/PivProvider.java:97-103` (Ed25519 path buffers the raw message) @f4626856. Rust `crates/yubikit/src/piv.rs:2626-2650` @90940e9b.

### PIVIMP-006a: Protect acquisition and every independent source copy
- Gate(s) and why: G1 (High, confidentiality). The cleanup scopes themselves have .NET precedent; the gate still applies.
- Severity / certainty: High / source-confirmed unprotected scopes; no runtime leakage shown.
- The decision Dennis must make: For generated management keys that the tool prints in hex, should the tool keep printing with an evaluation-only label, or move to a secure route?
- Options:
  - A. Keep printing, labelled evaluation-only, and document that printing is not escrow. **Sia's lean.** Smallest change. The cleanup scopes are approved with it, using the existing `finally` and `DisposableArrayPoolBuffer` precedents: CLI pin and key paths, `GenerateRandomManagementKey`, passphrase derivation and `CreateFromDefault`.
  - B. Stop printing. Write to a file with restrictive permissions, or refuse display. Better secret handling; a UX change for the tool.
  - C. Defer both. Not recommended: the cleanup is local and cheap.
- Evidence: `src/Piv/examples/PivTool/Cli/Commands/CliRunner.cs:160-166` (verified), `:258-276` (`finally` clears at 275). `src/Piv/examples/PivTool/Cli/Prompts/PinPrompt.cs:249-256` (hex printed at 253), `:286-300`, `:305-308`, `:315-316`. Precedent `src/Core/src/Utilities/DisposableArrayPoolBuffer.cs:79-87` (zeroes and returns on Dispose). Context only: Android `piv/…/PivSession.java:1213-1216` zeroes encoded TLVs; Python bytes are immutable, so not a clearing precedent.
- T2 tag: secret-buffer clearing and disposal ownership.

### PIVIMP-006b: Give plaintext and sample results one cleanup owner
- Gate(s) and why: G1 (High, confidentiality for plaintext).
- Severity / certainty: High / source-confirmed API obstacle; the result shape is a choice.
- The decision Dennis must make: Should the decrypted-plaintext result carry a sealed disposable owner, or a mutable buffer that the caller clears?
- Options:
  - A. Mutable plaintext carrier with caller-owned clearing and no read-only view. Cheapest. Matches the owned byte outputs in Android (`piv/…/PivSession.java:1269-1274`) and Rust (`crates/yubikit/src/piv.rs:1821-1834`).
  - B. Sealed disposable result owning an `IMemoryOwner<byte>`. **Sia's lean.** An owned-buffer precedent exists in Core (`DisposableArrayPoolBuffer.cs:29-49, 79-87`). It also avoids a `sealed record` clone that aliases one owner.
  - C. Keep `ReadOnlyMemory<byte>` with a caveat. Not recommended: nothing can clear it.
- Evidence: `src/Piv/examples/PivTool/PivExamples/Results/DecryptionResult.cs:20-47` (verified), `src/Piv/examples/PivTool/PivExamples/Decryption.cs:77-81` (verified), consumers `src/Piv/examples/PivTool/Cli/Menus/CryptoMenu.cs:175-180` and `src/Piv/examples/PivTool/Cli/Commands/JsonOutput.cs:134-135`. Python `yubikit/piv.py:1162-1164` returns immutable bytes.
- T2 tag: result ownership and disposal (the master links this to OpenPGP PGP-CORE-002).

### PIVCORE-002: Whole-workflow lifetime and terminal recovery

**Historical proposal, superseded by [AG-29](../pass-2-review-decisions.md#ag-29-c1-caller-serialization).**
The proposed session-level refusal is withdrawn; use the documented caller-serialization contract.

- Gate(s) and why: G1 (High). Core-owned: admission and drain are CORE-B5-C006, shared with the WebAuthn whole-ceremony card.
- Severity / certainty: High / source boundary confirmed; product intent unselected.
- The decision Dennis must make: Should multi-step PIV workflows be serialized by the caller as whole tasks, or covered by a shared whole-operation admission lease in Core?
- Options:
  - A. Caller-serialized whole tasks, with the session refusing overlap and documented limits. **Sia's lean** for PIV now. Rust already does this structurally: each `PivSession` operation takes `&mut self` (`crates/yubikit/src/piv.rs:1263, 1329, 1809, 1821, 1863`). Python and Android workflows run synchronously.
  - B. Shared whole-operation lease covering authenticate, PIN-only, reset and metadata-plus-crypto, with nested-call rules. Larger change; waits on the Core B5-C006 decision.
  - C. Keep the per-exchange guard only. Not recommended: callbacks outside the guard can outlive the drain.
- Evidence: `src/Piv/src/PivSession.cs:207-217` (verified; no terminal disposal or reselection), `:1086-1103`. Python `yubikit/piv.py:736-777` (synchronous) @4ca60f70. Rust `crates/yubikit/src/piv.rs:1263, 1809, 1821` @90940e9b.
- T2 tag: whole-operation admission, reentrancy and callback drain; session state after failure or reset.

### PIVIMP-004: Separate fixed provisioning models from general PIV objects
- Gate(s) and why: none fired by siblings. No sibling implements KeyHistory or a CHUID/CCC parser: Android has constants only (`piv/…/ObjectId.java:22, 32`); Rust maps only the KeyHistory object ID (`crates/ykman-cli/src/piv.rs:831`); Python has general `Chuid` and `FascN` (`yubikit/piv.py:424, 487`) but no KeyHistory. The spec rule below is a decision input, not a sibling argument.
- Severity / certainty: Medium / parser scope confirmed; the representation is a decision.
- The decision Dennis must make: Should KeyHistory, CHUID and CCC stay as named fixed provisioning models, or move to a general object model that preserves unknown fields?
- Options:
  - A. Keep named fixed models. Add a KeyHistory profile validator that implements the SP 800-73-4 F3 rule and the count and URL rules; keep raw inspection. **Sia's lean.** The F3 rule is settled by the spec: F3 is required when the off-card count is above zero, absent when both counts are zero, and optional otherwise (frozen source-analysis line 25). The encoder always emits F3 (`src/Piv/src/DataObjects/KeyHistory.cs:103-109`), and the decoder requires exactly four TLVs (`:133`), so a spec-valid three-TLV object with zero counts is rejected.
  - B. General object model with preserved unknown TLVs; convenience factories stay separate. Larger API change.
  - C. Document only. Not recommended: a known encoding mismatch stays in place.
- Evidence: `src/Piv/src/DataObjects/KeyHistory.cs:71-84`, `:103-109`, `:121-167` (count check at 133; F3 check at 148-151). `src/Piv/src/DataObjects/CardholderUniqueId.cs:134-163` (fixed five-TLV shape). Spec input: `docs/research/documentation-audit/modules/piv/pass-1/piv-001/evidence/source-analysis.md:25`.

### PIVIMP-006d: Keep cancellation and synchronous signing callbacks truthful
- Gate(s) and why: T2 (cancellation semantics are cross-cutting; the policy names timeout and cancellation). G5 not raised: the lean follows an existing SDK precedent.
- Severity / certainty: Medium / source-confirmed: cancellation is reported as "touch timeout", and broad catches turn it into an ordinary failure.
- The decision Dennis must make: When a PIV operation is cancelled, should the error propagate to the tool's top level, or come back as a distinct canceled result from the helpers?
- Options:
  - A. Propagate `OperationCanceledException`. The top level prints "Operation cancelled" and exits with the cancel code. **Sia's lean.** In-SDK precedent at `src/Oath/examples/OathTool/OathToolApplication.cs:40-43`. It drops the "touch timeout" label, which PIV does not confirm.
  - B. Helpers return a distinct canceled result, consistently across helpers and the CLI. More result types for callers to carry.
  - C. Keep the touch-timeout label. Not recommended: no confirmed PIV touch-timeout status exists.
  - Bridge sub-question (lean, no separate vote): keep the blocking `X509SignatureGenerator` bridge with an explicit captured token and whole-task lifetime; move to async TBS signing only after provider review.
- Evidence: `src/Piv/examples/PivTool/PivExamples/Signing.cs:105-108`. `src/Piv/examples/PivTool/PivExamples/Certificates.cs:466-473` (blocking RSA bridge), `:551-566` (blocking ECDSA bridge), `:169-173` (export catches all exceptions). Precedent `src/Oath/examples/OathTool/OathToolApplication.cs:35-44`. Sibling: Python builders are synchronous (`ykman/piv.py:692`, `:729`) @4ca60f70; Rust `sign` takes no cancellation parameter (`crates/yubikit/src/piv.rs:1809-1814`) @90940e9b.
- T2 tag: cancellation and touch-timeout semantics.

### PIVIMP-007a: Admit algorithm-shaped output before reporting completion
- Gate(s) and why: G3 (all three siblings accept unshaped responses; precedent does not count). G5: strict admission is a safety improvement over every sibling, so it is raised here. T2 candidate (Core B6-C002 response owner).
- Severity / certainty: Medium / malformed-success acceptance confirmed in source.
- The decision Dennis must make: Should a signature or decryption response be admitted only when its shape matches the algorithm, before the operation reports Completed?
- Options:
  - A. Strict per-algorithm admission: RSA exact modulus width; Ed25519 64 bytes; P-256 and P-384 strict DER within range; agreement 32 or 48 bytes; anything else fails before transfer. **Sia's lean.** It matches the CLAUDE.md rule that security-critical protocol data stays strict. The RSA `BigInteger` residual is documented, not claimed erased.
  - B. Keep permissive and document that Completed means a 0x82 payload arrived. Matches all three siblings; leaves the malformed-success case open.
  - C. Length-only checks (RSA width, Ed25519 64 bytes), with no DER parsing for ECDSA. Middle option; ECDSA malformed success stays open.
- Evidence: `src/Piv/src/Cryptography/PrivateKeyOperations.cs:420-438` (verified; 429-437). Python `yubikit/piv.py:1476-1501` (returns the 0x82 payload unchecked) @4ca60f70. Rust `crates/yubikit/src/piv.rs:2166-2182` and `:2500-2506` (`PivSignature::try_from` accepts any bytes) @90940e9b. Android `piv/…/PivSession.java:454-466` (returns the unpacked 0x82 value) @f4626856.
- T2 candidate: response admission before user-presence Completed. Not checked against other applet sections in this pass.

### PIVIMP-007b: Bound certificate decompression and CertInfo admission
- Gate(s) and why: G3 on the decompression ceiling: all three siblings decompress without a bound. G5: a bounded copy is an improvement over every sibling. The CertInfo rejection is sibling consensus and is bundled here for one answer.
- Severity / certainty: Medium / source-confirmed unbounded copy; no exploit shown; certificates are public data.
- The decision Dennis must make: What decompressed-size ceiling should the SDK enforce, and should it add the Net iD zlib variant that all three siblings decode?
- Options:
  - A. Reject unknown nonzero CertInfo modes. Enforce a fixed ceiling while copying (my placeholder: 64 KiB, documented as an SDK safety limit, not a standard). Add the Net iD zlib variant. **Sia's lean.** Matches the siblings on format. The ceiling value is for Dennis to set; this pass did not measure the device object limit.
  - B. Same as A, gzip only. Smaller change; the interop gap stays.
  - C. Keep current code. Not recommended: unknown modes fall back to DER.
- Evidence: `src/Piv/src/Certificates/CertificateProtocol.cs:68-90` (verified; gzip only; unknown modes treated as DER). Python `yubikit/piv.py:660-694` (unbounded; Net iD branch 673-689) and `:1258-1281` (rejects unknown CertInfo at 1275-1276) @4ca60f70. Rust `crates/yubikit/src/piv.rs:1023-1052` (unbounded `read_to_end`; Net iD branch 1035-1049) and `:1906-1930` (rejects unknown at 1926-1928) @90940e9b. Android `piv/…/PivSession.java:882-903`, `piv/…/CompressionUtils.java:43-66` (unknown payload throws at 66), `piv/…/GzipUtils.java:59-73` (unbounded loop) @f4626856.
- Note: 1856 bytes is a recommended certificate length, not a maximum (frozen source-analysis line 26).

### PIVIMP-008: Make tests survive their own failing assertions
- Gate(s) and why: no sibling gate. Python device tests already use stronger oracles (frozen source-analysis lines 96-97), so the C# oracle gaps are not shared. Split: the failure-path drain is no-brainer per the author; release expectations and floors are decisions. T2: the drain is Core-owned (CORE-A-C013, CORE-B2-C005).
- Severity / certainty: Medium / source-confirmed pending-task gap at `PivConnectionConcurrencyTests.cs:74-80`.
- The decision Dennis must make: Approve the failure-path drain and owner cleanup now, and hold the release-failure expectation and fixture floors for a separate decision?
- Options:
  - A. Approve the drain and owner cleanup now; defer release expectations and floors. **Sia's lean.** It moves the no-brainer half without waiting on Core fixtures.
  - B. Hold the whole card until the Tests.Shared authorization decision (CORE-A-C013).
  - C. Upgrade weak oracles now, without deciding release-failure expectations. Not recommended: that decision defines what the upgraded assertions must check.
- Constraint: response-only PIN-only tests keep their accepted witnesses; no pool-view or pool-seam assertions (Windows successor rollback).
- Evidence: `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivConnectionConcurrencyTests.cs:66-82` (verified). Python tests: frozen source-analysis lines 96-97.

### PIVIMP-009: Decide the ADMIN timestamp format before migration
- Gate(s) and why: G5 (the lean keeps the C# codec, diverging from Python's big-endian format, to avoid misreading existing data). G2 does not apply: the frozen analysis calls this a Yubico software format, not a NIST field. No A3 confluence: Python is the only sibling that encodes the date. Rust's ykman crate has no ADMIN date encoder (grep on 2026-11-14), and Android has only the PIVMAN_DATA constant.
- Severity / certainty: Medium / source-confirmed format difference.
- The decision Dennis must make: Keep the SDK's little-endian trimmed encoding and document it, or adopt Python's fixed big-endian uint32 with an explicit legacy import?
- Options:
  - A. Keep the codec and document it. Add an explicit legacy-format choice for imports; never auto-detect four-byte dates. **Sia's lean.** Python's own decode is broken: `ykman/piv.py:214` yields a tuple that `:260` cannot re-pack, so Python is not yet a clean round-trip reference. A silent switch would misread existing little-endian data.
  - B. Adopt big-endian uint32 now, with a legacy import that requires the caller to state the source format.
  - C. Keep the codec with no documentation change. Not recommended: the interop difference stays invisible.
- Evidence: `src/Piv/src/DataObjects/AdminData.cs:245-262` (little-endian trimmed at 247-249 and 261). Python `ykman/piv.py:209-214` and `:253-261` @4ca60f70. Frozen source-analysis lines 87-89.

## Auto-approved

### SAMPLE-A004: Dispose the unified CLI attestation certificate
- Rule: A1 (no-brainer: yes; mechanical). No sibling equivalent exists. In-SDK precedent confirms the pattern: `src/Piv/examples/PivTool/PivExamples/Certificates.cs:54` documents `using var certificate = result.Certificate;`, and `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpSessionWireTests.cs:124` uses `using var certificate = await session.GetCertificateAsync(...)`.
- Evidence pin (E2): `src/Cli.Commands/src/Piv/PivCommands.cs:552-553` (verified; no `using` on `cert`).
- Pseudocode for the fix:
```text
await using var session = await deviceContext.Device.CreatePivSessionAsync();
using var cert = await session.AttestKeyAsync(slot);
// output block unchanged: subject, issuer, serial, validity, PEM
```
- Test to add: none. There is no observable disposal oracle for `X509Certificate2`, and CLAUDE.md forbids validation-only tests. Review gate: confirm the output block does not use `cert` after its scope.
- E1 escalation risk: stop if `AttestKeyAsync` returns a cached or shared instance, since disposing it would break other callers.

### PIVIMP-006c: Export, CSR authentication and resource tasks (scope-limited)
- Rule: A1 (no-brainer: yes for export field, PIN prerequisite and local disposal; first match). A3 also holds for the PIN step: Python and Rust CLIs both verify the PIN before CSR signing. A5 supports disposal. Excluded and still open: result carrier redesign (006b), callback and cancellation (006d), and the broader workflow (PIVCORE-002). This does not close those scopes.
- Evidence pin (E2): .NET `src/Piv/examples/PivTool/Cli/Menus/CertificatesMenu.cs:97-112` (verified; menu checks `result.CsrPem` at 103), `src/Piv/examples/PivTool/PivExamples/Certificates.cs:154-170` (export returns `Certificate` at 169), `CertificatesMenu.cs:203-208` (CSR menu has no PIN step), `src/Piv/examples/PivTool/Cli/Commands/CliRunner.cs:545-555` (CLI already verifies PIN before CSR: internal precedent). Siblings: Python `ykman/_cli/piv.py:1274-1275` @4ca60f70 (verified); Rust `crates/ykman-cli/src/piv.rs:1827-1828` @90940e9b (verified).
- Pseudocode for the fix:
```text
Export menu:
  result = ExportCertificatePemAsync(session, slot)
  if not result.Success or result.Certificate is null: report error; return
  using cert = result.Certificate
  filename = prompt; write cert.ExportCertificatePem() to filename
CSR menu:
  slot = select; subject = prompt
  using pin = PinPrompt.GetPinWithDefault(...)
  VerifyPinAsync(session, pin.Memory); on failure report and return   (same as CLI)
  result = GenerateCsrAsync(session, slot, subject); output as before
Self-sign and F9 read: each returned certificate has one owner; dispose it on any store or
  packaging failure; never replace existing F9 material
```
- Test to add: (1) the export helper's success result yields the PEM text through an extracted helper, not a console harness; (2) the CSR path calls PIN verification before the signing call (fake session records call order). No validation-only tests.
- E1 escalation risk: stop if the menu needs a new credential flow beyond the existing `PinPrompt`, or if the self-sign disposal needs `CertificateResult` to become disposable (that is 006b, interview).

### PIVIMP-010: Reject raw object IDs that would change on the wire
- Rule: A4 (split siblings; Medium, so pick the stricter fail-closed behaviour). Android masks any ID to 24 bits (silent aliasing). Python and Rust send the natural length and do not alias (Python raises for negatives; Rust's raw ID is `u32`). Stricter choice: reject outside 0..0xFFFFFF before any APDU. G5 not raised: this is the A4 default, not a new UX option. G3 not hit: only Android shares the aliasing.
- The 24-bit range rests on the 3-byte PIV object-ID encoding that every sibling uses for standard IDs. The NIST Part 2 text on the object-identifier length is not quoted in the frozen analysis and was not re-verified here.
- Evidence pin (E2): .NET `src/Piv/src/DataObjects/DataObjectProtocol.cs:119-131` (verified). Android `piv/…/ObjectId.java:61-69` @f4626856 (masks to 24 bits). Python `yubikit/core/__init__.py:264-266` and `yubikit/piv.py:1214-1256` @4ca60f70 (`int2bytes` natural length; `to_bytes` rejects negatives). Rust `crates/yubikit/src/piv.rs:952-954` and `:1873-1899` @90940e9b (`u32`, minimal encoding; constants at `:2853`).
- Pseudocode for the fix:
```text
EncodeObjectId(objectId):
  if objectId < 0 or objectId > 0xFFFFFF:
      throw ArgumentOutOfRange(objectId)      // before any APDU is built
  existing 1, 2 and 3 byte branches unchanged
  zero stays allowed (no sibling rejects it)
```
  Confirm at implementation that GET and PUT both route through `EncodeObjectId`.
- Test to add: emitted-bytes tests with a fake connection: 0x01 gives `5C 01 01`; 0x0100 gives `5C 02 01 00`; 0x10000 gives `5C 03 01 00 00`; 0xFFFFFF gives `5C 03 FF FF FF`. For -1, 0x01000000 and `int.MinValue`, assert `ArgumentOutOfRangeException` with zero APDUs sent, including the PUT delete path with null data.
- E1 escalation risk: stop if any shipped Yubico object or documented profile uses an ID above 0xFFFFFF or below zero, or if a device answers a 4-byte ID with success. Either would show the 24-bit assumption is wrong.

## T1 hardware qualification
| Card | What needs a device | Device / firmware / transport / OS |
| --- | --- | --- |
| PIVIMP-003 | ROCA interval (Python from 4.2.0; .NET from 4.2.6), serial gate (.NET 5.0.0; Python 5.0.3), AES gate (.NET 5.4.0; released 5.4.2), FIPS 4.4.x RSA1024 and PIN Never, 5.7 key types, NFC and Bio routes | YubiKeys from 4.2.x to 5.7.x (ROCA 4.2.0–4.3.4; FIPS 4.4.x; serial 5.0.0–5.0.2; AES 5.4.0–5.4.1; Ed25519 and RSA 3072/4096 at 5.7); USB and NFC; Windows, macOS, Linux |
| PIVIMP-001 (sub-scope) | Bio-configured reset refusal; post-RESET refresh failure after a successful RESET | Bio-configured key with an enrolled fingerprint; destructive reset, separately authorized; USB; any OS |
| PIVIMP-005 (sub-scope) | End-to-end PivTool RSA PKCS#1 v1.5 and Ed25519 signatures verified against the original message | RSA on any 5.x; Ed25519 needs 5.7+; USB; Windows and macOS or Linux |
| PIVIMP-006d (sub-scope) | Touch-dependent cancellation and timing | Touch-policy key (Always or Cached); USB; each supported OS |
| PIVIMP-008 (sub-scope) | Touch, hotplug, and release or quarantine expectations | Touch-capable key; reader hotplug; USB; Windows and Linux |
| PIVCORE-002 (physical sub-scope) | Native interruption and recovery after cancellation in an admitted task | Touch-capable key; USB; OS as authorized |
| PIV-QUAL (unnumbered in master) | Separate-process PIN isolation; UNPOWER and observed power; native interruption; touch-cache timing; post-removal recovery; historical serial 125 SCP failures; serial 9681620 discovery failure (unexplained) | Two processes or hosts; PC/SC readers; USB and NFC; Windows and Linux PC/SC; historical serials 103 and 125 are not a fresh baseline |

## T2 cross-cutting candidates
| Theme | Card(s) here | Why it is cross-cutting |
| --- | --- | --- |
| Whole-operation admission, reentrancy and callback drain | PIVCORE-002; PIVIMP-006d; PIVIMP-008 | Core CORE-B5-C006 owns admission. The master cross-references WebAuthn's whole-ceremony card, OpenPGP PGP-CORE-004, YubiOTP OTP-DEP-003 and YubiHSM HSM-DEP-002 as related owners. Decide once. |
| Cancellation and touch-timeout semantics | PIVIMP-006d | The policy names timeout and cancellation. OathTool maps cancellation to a distinct exit (`src/Oath/examples/OathTool/OathToolApplication.cs:40-43`); PivTool labels it "touch timeout" (`Signing.cs:105-108`). |
| Session state after failure or reset | PIVCORE-002 | OATH-DEC-001 and YubiHSM Auth HSM-DEP-002 ask whether a failed or uncertain authentication or mutation leaves the session usable. Use the same rule. |
| Reset product preflight and routing | PIVIMP-001 | Named owner is MGMT-IMP-010 (combined reset product and lifecycle contract). The route decision is the same one. |
| Secret buffer and result ownership | PIVIMP-006a; PIVIMP-006b | Shared borrowed and owned disposal with OpenPGP PGP-CORE-002 (per master). Core `DisposableArrayPoolBuffer` is the candidate owner. |
| Malformed-success response admission (candidate) | PIVIMP-007a | Core B6-C002 owns response copies. Other applet sections were not checked in this pass; confirm before deciding. |

## Coverage check
- Cards in the PIV review map (master lines 7195–7206 and card headings through 7886): PIVIMP-001, -002, -003, -004, -005, -006a, -006b, -006c, -006d, SAMPLE-A004, PIVIMP-007a, -007b, -008, -009, -010, PIVCORE-002, and the physical, profile and restoration qualification card. The master gives that card no ID; this file labels it PIV-QUAL. 17 cards; each appears once in the Index.
- The PIV review map table has no severity or no-brainer columns. Severity and no-brainer come from each card's bold lead line.
- Not treated as cards (referrals and ledger entries): PIVCORE-001 and PIVCORE-003 (routed to PIV-QUAL and to the B6-C002 owner row); CORE-PIV-X001 to X006 (boundary IDs, each mapped to a card above); the 11 routed provenance keys (existing owners, not new PIV findings); the narrow shared-dependency rows (B1, B4, B6, A-C013, B2-C005, OpenPGP, Security Domain, YubiHSM, YubiOTP, OATH DOC-002); PIV-C001 to C011 and PIV-REV-001 to 003 (completed documentation groups); the PR745 resolutions at master lines 8005–8014; the withdrawn pool-seam and pre-return observations; the Windows report-route supersession, limited to the successor's named mechanisms.
- Historical P05 and P06 signing failures and serial 125 and 9681620 failures are kept as records under PIVIMP-005, PIVIMP-008 and PIV-QUAL. They are not re-counted.
- Baseline: `src/Piv` and `src/Cli.Commands/src/Piv` are identical between `6ce355468` and worktree head `52d34ca65` (empty `git diff --stat`). The worktree's tracked PIV paths are clean. Sibling trees were clean for tracked files at the pins used.
- Not verified in this pass: the NIST SP 800-73-4 Part 2 object-identifier length text; Android's `calculateSecret` and other unlisted paths; the Python `pivman_change_pin` and `pivman_set_pin_attempts` bodies (grep line numbers only); other applet sections for the response-admission T2 candidate.
- No source edits, builds, tests, commits, pushes or hardware operations were performed.
