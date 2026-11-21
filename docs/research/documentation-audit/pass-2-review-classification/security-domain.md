# Security Domain: Pass 2 classification
Classifier: one dedicated subagent, 2026-11-14. Source: master lines 3931–4743 at 52d34ca65. Policy: pass-2-review-policy.md (approved). Final per-card calls: `../pass-2-review-ledger.md`.
Sibling pins: yubikey-manager @ 4ca60f70; yubikit-android @ f4626856; yubikey-manager-rust-auto @ 90940e9b.

## Summary
| Disposition | Count | Cards |
| --- | --- | --- |
| A1 (no-brainer) | 0 | None. SD-IMP-002 is the only "No-brainer: yes" card, but G1 applies first. |
| A2 (sibling consensus) | 1 | SD-IMP-003 |
| A3 (Python precedent, partial confluence) | 0 | None |
| A4 (split siblings, stricter wins) | 0 | None |
| A5 (internal precedent) | 3 | SD-IMP-007, SD-IMP-008, CORE-SD-X003 |
| A6 (keep and document) | 1 | SD-DEP-002 |
| Interview (gated) | 7 | SD-IMP-002 (G1), SD-IMP-001 (G3, G5), SD-IMP-004 (G3), SD-IMP-005 (G5), SD-IMP-006 (G3), SD-DEP-005 (G3, G5), CORE-SD-X007 (G2) |
| T1 (qualification) | 2 | SD-DEP-001, Device and producer qualification |
| T2 tag | 8 | SD-IMP-001, SD-IMP-004, SD-IMP-008, SD-DEP-001, SD-DEP-002, SD-DEP-005, CORE-SD-X003, CORE-SD-X007 |

14 cards in total: 5 auto-approved, 7 interview, 2 T1. Each card has one primary disposition; the T2 tag is additional.

## Index
| Card | Severity | Disposition | One-line rationale |
| --- | --- | --- | --- |
| SD-IMP-001 | Medium | Interview (G3, G5) | Reset treats 65 non-blocking attempts as success; all three siblings do the same, so precedent cannot decide. |
| SD-IMP-002 | High (potential) | Interview (G1) | Empty SCP03 DELETE filter; all siblings reject it, but High severity goes to Dennis. |
| SD-IMP-003 | Medium | A2 (secondary A6) | Serials are not normalized to positive DER INTEGER, as all three siblings do; SCP11c counter stays documented as unsupported. |
| SD-IMP-004 | Medium | Interview (G3) | CA listings have no tag checks in any of the four implementations; odd key-information tails are silently dropped by C#, Python and Rust. |
| SD-IMP-005 | Medium | Interview (G5) | Reset reinitializes without original options; no sibling reinitializes, so the reuse choice is Dennis's. |
| SD-IMP-006 | Medium | Interview (G3) | GET DATA truncation, unchecked GENERATE KID and negative KVN wrap are each shared by at least two siblings. |
| SD-IMP-007 | Low test debt, Medium assurance | A5 | Unit-level oracles follow in-repo specific-exception, status and disposal-probe tests; device facets move to T1. |
| SD-IMP-008 | Medium (real secrets), Low (fixtures) | A5 (secondary A2) | Local KCV and AES temporaries zeroed as the repo already does; explicit P-256 curve byte matches all siblings. Fixture cleanup excluded. |
| SD-DEP-001 | Qualification, no defect severity | T1 | Setup and authority phase needs a dedicated SCP11 device and final spec text. |
| SD-DEP-002 | Medium conditional | A6 | Caller-owned SCP parameters are already the documented contract; keep, document and pin. |
| SD-DEP-005 | Medium conditional (privacy) | Interview (G3, G5) | Android and C# copy inner exception text into the wrapper message; diagnostics policy is shared with Core. |
| CORE-SD-X003 | Medium | A5 (secondary A6) | KCV-bearing PUT KEY response should be cleared internally, as PIV does; public GET DATA owner documented. |
| CORE-SD-X007 | Qualification, no applet severity | Interview (G2) | Python and Core pad and encrypt empty command data; GP Amendment D §6.2.6 says do not encrypt it. |
| Device and producer qualification | Qualification, no defect severity | T1 | Hardware, firmware, transport and native-provenance evidence only. |

## Interview queue (sorted High, then Medium, then Low)

### SD-IMP-002: Reject the empty normalized DELETE filter
- Gate(s) and why: G1, High potential impact. A1 would otherwise apply (the only "No-brainer: yes" card), but G1 overrides it.
- Severity / certainty: High potential impact (master). Empty filter confirmed in source; the device outcome is unknown.
- Decision: Should an SCP03 KID 01, 02 or 03 with KVN 0, which normalizes to the empty filter, be rejected with ArgumentException before any DELETE is sent?
- Options:
  - A (Sia's lean): Yes. Move the zero check after normalization. Python, Android and Rust all reject this input, and the public remarks already say not to use it. No empty DELETE can reach the card.
  - B: No. Keep the current behaviour and remarks. An empty DELETE stays reachable through a documented-invalid input.
  - C: Probe on a device first. Not recommended: the probe is itself a key deletion, and host rejection needs no device evidence.
- Evidence: .NET `src/SecurityDomain/src/SecurityDomainTlvEncoding.cs:30-36` (check before normalization), `:41-42` (empty payload); `src/SecurityDomain/src/SecurityDomainSession.cs:508-522`; `src/SecurityDomain/src/ISecurityDomainSession.cs:241-243`. Python `py:yubikit/securitydomain.py:291-296 @4ca60f70`. Android `SecurityDomainSession.java:319-327 @f4626856`. Rust `crates/yubikit/src/securitydomain.rs:740-748 @90940e9b`.

### SD-IMP-001: Reset completion and partial raw mutation
- Gate(s) and why: G3. Exhaustion after 65 non-blocking attempts returns normally in C#, Python, Android and Rust, so precedent cannot decide. G5 applies to option A, which diverges from all three siblings with a concrete outcome benefit. Tagged T2 (failed-attempt reset and post-failure state).
- Severity / certainty: Medium. Ordering is source-confirmed; the intended outcome is unsettled.
- Decision: When a key is still unblocked after 65 attempts, or returns an unexpected status, should reset throw an incomplete-reset error instead of returning normally?
- Options:
  - A (Sia's lean): Throw after attempting every reference, carrying each reference's outcome (exhausted, stopped with SW, transport failure, skipped). Also propagate unexpected statuses and transport failures, as Python, Android and Rust do. C# alone continues on both. Breaking for callers who ignore exhaustion, which alpha allows. PIV's blocking helper throws when the status is not a retry-count status (`src/Piv/src/Authentication/AuthenticationProtocol.cs:786-797`).
  - B: Keep the silent return. It is already documented (`ISecurityDomainSession.cs:335-337`). Cost: a partially reset device still reads as complete to callers who skip the remarks.
  - C: Return a structured result (attempted, incomplete, verified) instead of throwing. Cost: callers can ignore it.
- Evidence: .NET `src/SecurityDomain/src/SecurityDomainSession.cs:768-804` (ResetAsync), `:861-903` (BlockKeyAsync; transmit errors continue at `:877-882`; other statuses continue at `:894-901`). Python `py:yubikit/securitydomain.py:209-220 @4ca60f70`. Android `SecurityDomainSession.java:547-562 @f4626856`. Rust `crates/yubikit/src/securitydomain.rs:619-638 @90940e9b`.

### SD-IMP-004: Malformed listings and future-valid values
- Gate(s) and why: G3. CA listings have no tag checks in C# (`src/SecurityDomain/src/SecurityDomainSession.cs:382-391`), Python (`securitydomain.py:162-165`), Android (`SecurityDomainSession.java:204-209`) or Rust (`securitydomain.rs:547-557`). Key information silently drops an odd tail in C# (`SecurityDomainSession.cs:275-279`), Python (`securitydomain.py:128`, zip) and Rust (`securitydomain.rs:499-502`, `as_chunks`). Android throws an incidental BufferUnderflowException there (`SecurityDomainSession.java:148-150`). Precedent does not settle it.
- Severity / certainty: Medium. Parser paths are source-confirmed; no device reproduction.
- Decision: Should key information and CA listings reject odd tails, missing or wrong tags, and short references with BadResponseException, rather than return a partial list?
- Options:
  - A (Sia's lean): Yes. Reject structural malformation; keep unknown but valid descriptors and values. This follows the forward-compatibility doctrine in root CLAUDE.md (lines 156-158): malformed and security-critical data stay strict. Cost: callers get an exception where they got a partial list.
  - B: No. Keep partial lists and document them. Cost: a provisioning tool can miss a key or CA without noticing.
  - C: Reject only CA listings, since they affect trust, and keep key information lenient. Cost: two parser policies in one applet.
- Evidence: .NET `SecurityDomainSession.cs:265-279` (key information), `:382-391` (CA). Python `py:yubikit/securitydomain.py:126-128`, `:162-165 @4ca60f70`. Android `SecurityDomainSession.java:141-154`, `:204-209 @f4626856`. Rust `crates/yubikit/src/securitydomain.rs:477-506`, `:547-557 @90940e9b`.

### SD-IMP-005: Reset options and retained reauthentication inputs
- Gate(s) and why: G5. C# reset reinitializes without the original configuration or firmware override (`SecurityDomainSession.cs:795-803`; default 5.3.0 at `:175-176`). No sibling reinitializes after reset, and PIV's reset does not reselect either (`src/Piv/src/PivSession.cs:207-208`). Reusing retained SCP parameters has no sibling or internal precedent, so the choice belongs to Dennis.
- Severity / certainty: Medium. The omission is source-confirmed and documented (`ISecurityDomainSession.cs:339-343`).
- Decision: After a reset, which reinitialization contract should apply?
- Options:
  - A: Reapply configuration and version, and reuse the retained SCP parameters. Cost: SCP sessions can fail with SecureChannelException after a reset that removed their keys, which is the documented failure.
  - B (Sia's lean): Reapply configuration and version, reinitialize unauthenticated, and never reuse retained SCP parameters. Cost: the caller re-authenticates explicitly. The session stays usable for discovery, and the SCP11 gate cannot fail after reset.
  - C: End the session after reset, so the caller opens a new one with explicit options. Cost: a session cannot continue after reset. This makes the documented workaround mandatory.
- Evidence: .NET `SecurityDomainSession.cs:148-150`, `:175-176`, `:795-803`; `ISecurityDomainSession.cs:339-343`. Python `py:yubikit/securitydomain.py:103-108`, `:185-221 @4ca60f70` (no reinitialization). Android `SecurityDomainSession.java:519-565 @f4626856`. Rust `crates/yubikit/src/securitydomain.rs:599-643 @90940e9b`. PIV `src/Piv/src/PivSession.cs:207-208`.

### SD-IMP-006: Wire ranges and import response identity

Historical range proposal below: [AG-30](../pass-2-review-decisions.md#ag-30-q11-keyreplacement-exact-shape)
supersedes the public integer replacement argument with required `KeyReplacement` on all four overloads.
- Gate(s) and why: G3, on three facets. (1) GET DATA identifier truncation: C# (`SecurityDomainSession.cs:227-228`) and Rust (`securitydomain.rs:468-471`) truncate silently; Python rejects through `struct.pack` (`py:yubikit/core/smartcard/__init__.py:158 @4ca60f70`). (2) GENERATE forwards the caller KID unchecked in all four implementations (`SecurityDomainSession.cs:551-558`; `py:yubikit/securitydomain.py:319-322 @4ca60f70`; `SecurityDomainSession.java:368 @f4626856`; `securitydomain.rs:783 @90940e9b`). (3) A negative replaceKvn wraps in C# (`(byte)` cast at `SecurityDomainSession.cs:477`) and in Android (`core/src/main/java/com/yubico/yubikit/core/smartcard/Apdu.java:101-106` accepts -128 to -1); Python and Rust reject it.
- Severity / certainty: Medium. Casts and admission paths are source-confirmed; no misprovisioned device has been demonstrated.
- Decision: Should GENERATE accept only the SCP11 key references 0x11, 0x13 and 0x15, rejected before transmission?
- Options:
  - A (Sia's lean): Yes. This matches the SCP11 scope of the method. Bundled with this decision, Sia proposes two range and response fixes that the siblings already back. Pass replaceKvn as `int` to `ApduCommand`, which already validates all four bytes (`src/Core/src/Protocols/SmartCard/Apdu/ApduCommand.cs:61-66`; `src/Core/src/Utilities/ByteUtils.cs:5-11`). Require an exact 10-byte SCP03 KCV response bound to the KVN: Python (`securitydomain.py:378-380`), Android (`SecurityDomainSession.java:417-418`) and Rust (`securitydomain.rs:831-835`) are exact, while C# is lenient (`src/SecurityDomain/src/SecurityDomainKeyMaterial.cs:65-78`). Cost: rejects references the card might accept (0x10, 0x20–0x2F), which would need a separate documented path.
  - B: Keep catalog membership, as all siblings do. Cost: an invalid reference reaches the card.
  - C: SCP11 family only, and only when the explicit version gate passes (`SecurityDomainSession.cs:533-534`). Cost: unversioned sessions get no admission check.
- Evidence: .NET as above. Python, Android and Rust as cited above.

### SD-DEP-005: Selected error provenance and diagnostics
- Gate(s) and why: G3. Android's constructor wraps the cause in `IllegalStateException(e)` (`SecurityDomainSession.java:106-107 @f4626856`), and Java copies the cause's text into that message. C# does the same in `src/SecurityDomain/src/SecureChannelException.cs:73`. G5 applies because nested status surfacing (`SecureChannelException.cs:33-34`, `:67`) is a concrete diagnostic gain that Python and Android do not offer. Tagged T2, since the diagnostics policy is shared with Core.
- Severity / certainty: Medium conditional, privacy. Catch and message text are confirmed. Provider text is not known to contain key material, and no real key was observed in a log.
- Decision: Should SecureChannelException stop copying the inner exception's text into its own message, and should it report a nested APDU status?
- Options:
  - A (Sia's lean): Yes to both. Use fixed message text and keep the original exception as InnerException, the Core pattern (`src/Core/src/Protocols/SmartCard/Scp/ScpInitializer.cs:91`). Take the status word from the nested ApduException. The wrap set stays as it is. Cost: consumers that display the message see less detail.
  - B: Add nested status only, and keep the current message. Cost: provider text can still reach logs through the message.
  - C: Keep everything and document it. Cost: the privacy risk stays open.
- Evidence: .NET `SecureChannelException.cs:53-56`, `:64-68`, `:70-74`; `SecurityDomainSession.cs:193-199`. Android `SecurityDomainSession.java:106-107 @f4626856`. Python `py:yubikit/securitydomain.py:110-116 @4ca60f70` (authenticate forwards setup errors, no wrapper).

### CORE-SD-X007: Empty query, Le and protected chaining
- Gate(s) and why: G2. Python's `ScpState.encrypt` pads and encrypts empty command data and advances the counter (`py:yubikit/core/smartcard/scp.py:204-216 @4ca60f70`). `ScpProcessor.send_apdu` encrypts by default (`py:yubikit/core/smartcard/__init__.py:292-297 @4ca60f70`). GP Card Specification Amendment D v1.1.2, §6.2.6, applies when C-DECRYPTION and C-MAC are both required. It states: "No encryption shall be applied to a command where there is no command data field. In this case, the encryption counter shall still be incremented." Core documents the same deviation (`src/Core/src/Protocols/SmartCard/Scp/ScpState.cs:73-77`). Precedent is evidence, not authority (policy G2). The §6.2.6 text was checked on 2026-11-14 from the public PDF (globalplatform.org, GPC_2.3_D_SCP03_v1.1.2_PublicRelease.pdf). Python's SCP11 key usage also lists C-DECRYPTION (`scp.py:313-317`); Amendment F was not re-read here.
- Severity / certainty: Qualification, no applet defect severity. Sorted last. Core owns the change; the SD applet has no workaround, and none is proposed.
- Decision: Should Core's SCP03 empty-data path follow Amendment D §6.2.6 now, or keep the padded block until a device check shows what the YubiKey accepts?
- Options:
  - A (Sia's lean): Keep the padded block as a documented deviation, and run the device check (T1) first. Cost: the spec gap stays open until hardware evidence exists.
  - B: Change Core now to the §6.2.6 form, with unit tests of counter and command bytes. Cost: if the firmware expects the padded form, empty GET DATA under SCP fails, and no hardware has confirmed the new form.
  - C: Keep and document only, with no device check. Cost: the question is never settled.
- Evidence: `src/Core/src/Protocols/SmartCard/Scp/ScpState.cs:71-79`; Python as above.

## Auto-approved

### SD-IMP-003: Allowlist entry and counter profile
- Rule: A2 (primary). Python, Android and Rust all encode serials as positive DER INTEGERs. They drop redundant leading zeros and prepend 00 when the high bit is set. C# does neither. Secondary A6: the SCP11c counter stays unsupported and documented (`src/SecurityDomain/src/ISecurityDomainSession.cs:268-269`). Empty entries are rejected before send. No sibling can express a zero-length serial: Python and Android take numbers, and Rust `int2asn1` indexes an empty slice at `securitydomain.rs:376`.
- Evidence pin (E2): `py:yubikit/securitydomain.py:84-88 @4ca60f70`; Android `SecurityDomainSession.java:261 @f4626856` (BigInteger.toByteArray); Rust `crates/yubikit/src/securitydomain.rs:370-380 @90940e9b`.
- Pseudocode for the fix:
```text
StoreAllowListAsync, each serial in caller order:
  bytes = Convert.FromHexString(serial)            // odd length or non-hex: FormatException, unchanged
  if bytes.Length == 0 -> throw ArgumentException  // before any transmit
  while bytes.Length > 1 and bytes[0] == 0x00 -> drop the leading byte
  if (bytes[0] & 0x80) != 0 -> prepend 0x00
  append TLV 0x93 with bytes                       // list order unchanged
ClearAllowListAsync and the SCP11c counter: unchanged
```
- Test to add: unit, using the recording-connection pattern at `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecurityDomainSessionTests.cs:616-640`. "A1B2" sends `93 03 00 A1 B2`; "0001" sends `93 01 01`; "00" sends `93 01 00`; "" throws ArgumentException with zero transmitted commands; two serials keep caller order. Review the existing test at `:616` first: if it asserts raw bytes for a high-bit serial, that assertion changes with this card.
- E1 escalation: stop if the change needs a different public parameter type, or if anyone asks for the SCP11c counter (tag 92). That needs the final or vendor profile, which is T1.

### SD-IMP-007: Tests that observe the intended security boundary
- Rule: A5. In-repo oracles exist for each unit-level facet: an inner `BadResponseException` checked by message (`src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecureChannelExceptionTests.cs:106-115`), an exact status word (`src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/PivSessionTests.cs:467`), and a managed-cleanup count (`src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/ApplicationSessionDisposalTests.cs:59-68`). The sibling wrong-key oracle is type-specific (`py:tests/device/test_securitydomain.py:127-132 @4ca60f70`), with a matching-key control at `:120-125`.
- Evidence pin (E2): the .NET precedents above, plus `py:tests/device/test_securitydomain.py:120-132 @4ca60f70`. The sibling's admin-denial checks use `ApduError` without a status check (`:124-125`, `:188-189`, `:208-209`), so an exact-status denial test is not sibling-backed.
- Pseudocode for the fix:
```text
Unit, SCP11b with a wrong device key (fake device reply whose receipt cannot match):
  ex = await Assert.ThrowsAsync<SecureChannelException>(create session with the wrong key)
  inner = Assert.IsType<BadResponseException>(ex.InnerException)
  Assert.Contains("Receipt does not match", inner.Message)
Unit, IYubiKeyExtensions.ListKeyInformationAsync: owned connection disposed exactly once (probe)
Unit, configuration: assert effective command bytes for the configured options, not only non-null
Integration negative tests (NegativeTests.cs:39-66, 71-102) and generated-key reauthentication: unchanged here (T1)
```
- Test to add: the unit tests above. The expected receipt failure path is `src/Core/src/Protocols/SmartCard/Scp/ScpState.Scp11.cs:149` into `Scp11X963Kdf.cs:62-63`, wrapped at `src/SecurityDomain/src/SecurityDomainSession.cs:193-199`. The engineer confirms this on the first run, since tests are not run in this pass.
- E1 escalation: stop if a matching-key SCP11b positive control needs device-side ECDH or KDF modelling beyond the existing fakes; move it to T1. Stop if an exact SCP11b denial status is needed without device evidence.

### SD-IMP-008: Local crypto, fixture and integration cleanup
- Scope of this approval: local production helpers and the curve byte only. Fixture and integration cleanup is excluded (E1).
- Rule: A5 for zeroing. Root CLAUDE.md requires zeroing secret-derived output, and the module already zeroes its plaintext copy in `EncodeKeyComponent` (`src/SecurityDomain/src/SecurityDomainKeyMaterial.cs:43-59`). PIV clears its secret outputs in `finally` (`src/Piv/src/Cryptography/PrivateKeyOperations.cs:145-153`). A2 for the curve byte: all three siblings write 0x00 for P-256 (`py:yubikit/securitydomain.py:62, 319, 376 @4ca60f70`; Android `SecurityDomainSession.java:459 @f4626856`; Rust `crates/yubikit/src/securitydomain.rs:166, 863 @90940e9b`). Rust does not zero its temporary AES output, so the zeroing facet rests on the repo rule, not on sibling behaviour.
- Evidence pin (E2): the .NET precedents above; the three sibling curve-byte pins above.
- Pseudocode for the fix:
```text
SecurityDomainKeyMaterial.CalculateKcv (:98-107):
  Span<byte> encrypted = stackalloc byte[16]
  try   { aes.EncryptCbc(kcvInput, iv, encrypted, None); encrypted[..3].CopyTo(output) }
  finally { CryptographicOperations.ZeroMemory(encrypted) }
ValidateKcv (:68-70): zero expectedKcvEnc, expectedKcvMac and expectedKcvDek in finally
SecurityDomainSession.PutEcKeyAsync (:653):
  using var paramsTlv = new Tlv(KeyTypeEccKeyParams, (ReadOnlySpan<byte>)[0x00]);
  trailing 0x00 at :655 unchanged
```
- Test to add: unit wire capture, following `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecurityDomainSessionTests.cs:564`. EC public and private import both send `F0 01 00` followed by a trailing `00`. Zeroing has no black-box test, because stack buffers are not observable; verify it by review and the pre-commit checklist.
- E1 escalation: stop if zeroing needs a changed ownership contract for `encryptedKey` (`SecurityDomainSession.cs:615-619`) or the Core ciphertext contract. Also stop if the change touches shared test bases (SecurityDomainStateRestoringTests, the Track base, Tests.Shared). The fixture facets (Low) stay out of this approval.

### SD-DEP-002: Parameter containment and one lifetime owner
- Rule: A6. The caller-owned contract is already the stated and implemented behaviour. `SecurityDomainSession.cs:126-127` and `:132-133` document it, `src/SecurityDomain/src/IYubiKeyExtensions.cs:38` repeats it, and `Dispose` (`SecurityDomainSession.cs:945-961`) releases only the session's protocol references. A5 supports the owner rule: `src/Core/src/Sessions/SessionCreationOptions.cs:30` ("The caller owns the prompt and ScpKeyParameters") and `src/Ctap2/src/IYubiKeyExtensions.cs:112`. The sibling has no disposal to copy: its parameters are frozen dataclasses.
- Evidence pin (E2): the .NET precedents above; `py:yubikit/core/smartcard/scp.py:174-187 @4ca60f70`.
- Pseudocode for the fix (documentation only, no code change):
```text
Keep these statements as the one ownership contract:
  SecurityDomainSession.CreateAsync remarks (SecurityDomainSession.cs:126-127, 132-133)
  IYubiKeyExtensions.CreateSecurityDomainSessionAsync remarks (IYubiKeyExtensions.cs:38)
Pin test: create a session with Scp03KeyParameters (fake handshake), dispose it,
  then create a second session with the same parameters and assert it succeeds.
```
- Test to add: the pin test above, using `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/Scp03HandshakeFakeConnection.cs`. It checks ownership, not validation. If the fake cannot serve two handshakes, keep the docs only and drop the test.
- E1 escalation: stop if Core's containment decision (CORE-B6-C007, CORE-C-C020) changes who disposes contained owners, or if reset needs a copy of the parameters.

### CORE-SD-X003: The hidden response owner
- Rule: A5 for the internal KCV-bearing PUT KEY (static) response. Root CLAUDE.md treats secret-derived output as requiring zeroing. PIV clears its response in `finally` (`src/Piv/src/Cryptography/PrivateKeyOperations.cs:152`), and Core provides `ApduResponse.Clear()` (`src/Core/src/Protocols/SmartCard/Apdu/ApduResponse.cs:104`). A6 for the public GET DATA return: no public SD result is secret-bearing. Key information, CA identifiers, certificates and card recognition data are metadata, so the hidden owner is documented, not changed.
- Evidence pin (E2): the .NET precedents above, and `src/SecurityDomain/src/Backend/SecurityDomainBackend.cs:32-36` (SendAsync returns ApduResponse). No sibling pin: Android and Rust return caller-owned arrays, which is a different API shape and is not proposed here.
- Pseudocode for the fix:
```text
PutKeyAsync(StaticKeys) (SecurityDomainSession.cs:477-483), replacing TransmitAndGetResponseDataAsync:
  response = await _backend.SendAsync(command, cancellationToken: cancellationToken)  // throwOnError defaults to true
  try    { ValidateKcv(staticKeys, response.Data.Span) }
  finally { response.Clear() }   // Clear also zeroes status bytes: read SW before this line
GetDataAsync: signature and behaviour unchanged; XML remarks say the caller cannot clear the returned memory
```
- Test to add: none that observes zeroing, since it is not visible through the public API. Existing PUT KEY KCV tests must still pass. If an observer is needed, follow the internal `CertificateLoadedObserver` seam (`SecurityDomainSession.cs:88-89`).
- E1 escalation: stop if clearing the response would affect a caller-visible GET DATA result, if a public signature changes, or if Core's ApduResponse ownership (CORE-B6-C002) must change.

## T1 hardware qualification
| Card | What needs a device | Device/firmware/transport/OS |
| --- | --- | --- |
| SD-DEP-001 | Setup and authority: b and c public metadata against OCE-authorized mutation, and the a/c first correct-MAC phase, with exact phase and status. Final GP SCP11 text is needed to judge the phase. | Dedicated authorized device with SCP11 keys provisioned; firmware 5.7.2 or later; USB CCID (NFC only for NFC-specific behaviour); native PC/SC provenance recorded. |
| Device and producer qualification | Physical identity reinventory before any command; authorized reset, rotation and profile cases with exact nonsecret phase and status; independent fresh-authentication and readback; separate cleanup; loaded native library identified. | Authorized dedicated device with escrow and restoration plan; USB CCID or NFC reader, firmware, OS and architecture recorded. NativeShims 1.18.1 is not yet tied to a loaded binary. Historical serial 125 (6A88) and serial 9681620 remain unexplained. |
| SD-IMP-007 (integration facets, split out) | Wrong-key SCP11b integration oracle with a positive control; generated-key SCP11b reauthentication; exact status for SCP11b admin denial. | Dedicated device; firmware 5.7.2 or later for SCP11 facets; USB CCID. |
| CORE-SD-X007 (device check, facet of an interview card) | Empty GET DATA under SCP03 and SCP11 with C-DECRYPTION and C-MAC: counter progression, Le 0 and 255, extended format, and large certificate chaining with 61xx. | Dedicated device; SCP03 test minimum 5.4.3 and SCP11 5.7.2 or later; USB CCID and NFC both recorded. |
| SD-IMP-001 (device facet, only if Dennis asks) | Real outcome after 65 non-blocking attempts; factory postcondition after reset. | Destructive-scope device with escrow and restoration plan; firmware and reader recorded. |

## T2 cross-cutting candidates
| Theme | Card(s) here | Why it is cross-cutting |
| --- | --- | --- |
| Secret zeroing and owned response or ciphertext buffers | SD-IMP-008, CORE-SD-X003 (and CORE-B6-C002) | Root CLAUDE.md zeroing rules bind every applet. `ApduResponse.Clear()` (ApduResponse.cs:104) and `ScpState.Encrypt` ciphertext ownership (ScpState.cs:79) are shared Core contracts. PIV clears responses in `finally` (PrivateKeyOperations.cs:152). |
| Caller-owned SCP parameters and contained-owner disposal | SD-DEP-002 (and CORE-B6-C007, CORE-C-C020) | Core's `Scp03KeyParameters` and `Scp11KeyParameters` dispose contained owners (Scp03KeyParameters.cs:54-61; Scp11KeyParameters.cs:110-119). `SessionCreationOptions` and the Ctap2 SCP docs say caller-owned. |
| Failed-attempt reset and post-failure session state | SD-IMP-001 | PIV's reset also blocks credentials by failed attempts before RESET (PivSession.cs:191-205; AuthenticationProtocol.cs:786-797). The policy's OATH, PIV and YubiHSM Auth session-state question has the same shape. |
| Parser tail policy for device listings | SD-IMP-004 | Root CLAUDE.md's forward-compatibility doctrine (lines 156-158) binds all applets. Other applets' listing parsers are not audited here, so tail loss there is unverified. |
| Diagnostics, exception text and log redaction | SD-DEP-005 (and CORE-SD-X005, CORE-B6-C014, CORE-C-C008) | Copying provider text into wrapper messages is a repo-wide question. Core uses fixed messages with InnerException (ScpInitializer.cs:91); Android copies the cause (SecurityDomainSession.java:106-107). |
| Secure-channel command wire behaviour for empty data, Le and chaining | CORE-SD-X007 (and CORE-B6-C011, C012) | `ScpState.Encrypt` serves every SCP session. Core's own remarks record the §6.2.6 deviation (ScpState.cs:73-77). |
| SCP11 administrative authority and profile | SD-DEP-001 (and CORE-SD-X006, CORE-B6-C009, C010) | Authority semantics for b, c and a are SCP11 behaviour used beyond SD (Python `tests/device/test_securitydomain.py:120-125, 199-209`; Android `SecurityDomainSession.java:124-127`). |

## Coverage check
- Review map rows (master lines 3950–3965): SD-IMP-001, SD-IMP-002, SD-IMP-003, SD-IMP-004, SD-IMP-005, SD-IMP-006, SD-IMP-007, SD-IMP-008, SD-DEP-001, SD-DEP-002, SD-DEP-005, CORE-SD-X003, CORE-SD-X007, and Device/producer qualification. Each appears once in the Index (14 of 14).
- Coverage-ledger aliases folded into the cards, not counted separately: SD-DEP-003 (into SD-IMP-005), SD-DEP-004 (into SD-IMP-001, SD-IMP-005, CORE-SD-X003, CORE-SD-X007 and the device card), CORE-SD-X001 (SD-IMP-005), CORE-SD-X002 (SD-IMP-008 curve facet), CORE-SD-X004 (SD-IMP-001 and SD-IMP-005), CORE-SD-X005 (SD-DEP-005 and SD-DEP-001), CORE-SD-X006 (SD-DEP-001), CORE-SD-X008 (SD-IMP-007 and SD-IMP-008).
- Ledger exclusions not treated as cards: CORE-B6-C005 and CORE-B6-C016 (nonlocal); audit-v2-20261022-20261031 consideration-5 (resolved for production certificate cleanup); the rejected four-level testing-link claim; the superseded §7.5.4 locator (the correct draft locator is §7.5.1).
- Pin check: `git diff --stat 6ce3554 HEAD -- src/SecurityDomain src/Core/src/Protocols/SmartCard` is empty, so the master's windows still match. The .NET line numbers above were read at 52d34ca65. Python paths are relative to yubikey-manager's root (`yubikit/`, `tests/`); Android paths are relative to `core/src/main/java/com/yubico/yubikit/core/`; Rust paths are relative to `crates/yubikit/src/`.
- Not verified in this pass: the Python SCP11 1.3 and 1.4 comment citations; the SCP11 Amendment F draft text; Android and Rust device test suites; any device behaviour. No build, test or hardware run was made.
