# OpenPGP: Pass 2 classification
Classifier: one dedicated subagent, 2026-11-14. Source: master lines 6260–7155 at 52d34ca65. Policy: pass-2-review-policy.md (approved). Final per-card calls: `../pass-2-review-ledger.md`.
Sibling pins: yubikey-manager @ 4ca60f706af930459138d8dc0f0f953480e1c7a4 (`yubikit/openpgp.py`, `ykman/_cli/openpgp.py`); yubikit-android @ f46268563437ac52910001222a77741d229b9b99 (`openpgp/src/main/java/com/yubico/yubikit/openpgp/…`, abbreviated `…/` below; `core/src/main/java/com/yubico/yubikit/core/keys/PublicKeyValues.java`); yubikey-manager-rust-auto @ 90940e9bb734e4daec04bdb17dae155ff3c85c57 (`crates/yubikit/src/openpgp.rs`). python-fido2 not used. All three siblings have only untracked files. .NET lines are at worktree HEAD 52d34ca65; BEFORE windows match except SetUifAsync, where current code also updates `_updatedUifs` (`Config.cs:61`). Governing spec: OpenPGP Smart Card Application 3.4.1, read through search excerpts (the PDF is not fetchable directly): §7.2.11 (DECIPHER, ECDH input), §4.4.3.8 (Key Information), §4.4.3.9 (Algorithm Attributes, import formats 00–03), and DO CD (generation times). The master's §4.3.2 and §7.9 cites were not re-verified here.

## Summary
| Disposition | Count | Cards |
| --- | --- | --- |
| A1 no-brainer | 2 | PGP-IMP-007, PGP-IMP-013 |
| A2 sibling consensus | 0 | none as a standalone card; A2 facets are noted inside PGP-IMP-014 and the interview queue |
| A3 Python precedent, partial confluence | 0 | none as a standalone card; the timestamp facet of PGP-IMP-018 is A3 |
| A4 split siblings, stricter fail-closed | 1 | PGP-IMP-014 (NEO deletion refused; CRT-with-modulus label is A2) |
| A5 internal precedent | 0 | none as a standalone card |
| A6 keep and document | 0 | none |
| Interview (gated) | 11 | G1 High: PGP-IMP-001, -002, -003, -005, -009, -011, -015. G5: PGP-IMP-010, PGP-CORE-004. G3 and G5: PGP-IMP-018. G3: PGP-IMP-019 |
| Interview (no gate hit, no clean auto rule) | 3 | PGP-IMP-016, PGP-CORE-001, PGP-CORE-003 |
| T1 hardware qualification | 1 | Physical/chronology qualification |
| T2 tag (overlaps the rows above) | 15 | PGP-IMP-001, -002, -003, -005, -009, -010, -011, -014, -015, -016, -018, -019, PGP-CORE-001, -003, -004 |

G2 hit: none. No sibling contradicts the spec on a cited path. The .NET source does contradict it for X25519 (PGP-IMP-007) and the NEO template label (PGP-IMP-014); those are defects, not sibling gates. G4 is suspended.

## Index
| Card | Severity | Disposition | One-line rationale |
| --- | --- | --- | --- |
| PGP-IMP-001 | High | Interview (G1) | Identity fix matches all three siblings and would be A1; High still goes to you. |
| PGP-IMP-002 | High | Interview (G1) | KDF transition contract. Siblings offer only low-level writes, not an atomic transition. |
| PGP-IMP-003 | High | Interview (G1) | KDF ownership and F9 fallback. All siblings re-read F9 per operation and propagate errors. |
| PGP-IMP-005 | High | Interview (G1, G3) | Reset lifecycle. Siblings share the blocking-loop error swallow; status checks are sibling-settled. |
| PGP-IMP-007 | Medium | A1 | Wrap the X25519 DECIPHER input as A6 {7F49 {86}}. Python, Android and Rust agree; §7.2.11. |
| PGP-IMP-009 | High | Interview (G1, G3) | Non-atomic attribute, generate and delete writes. Siblings share the sequence; no rollback exists. |
| PGP-IMP-010 | Medium | Interview (G5) | Synthesized pre-5.2.0 entries. Marking them diverges from all siblings. Challenge and UIF gates are A2. |
| PGP-IMP-011 | High | Interview (G1) | ROCA range: [4.2.0, 4.3.5) in Python and Rust, [4.2.6, 4.3.5) in Android. Four tests return without asserting. |
| PGP-IMP-013 | Medium | A1 | Nest the ATT CRT as B6 03 84 01 81. Python, Rust and Android agree. |
| PGP-IMP-014 | Medium | A4 | Siblings split on NEO deletion; the stricter refusal is chosen. CRT-with-modulus label is A2. |
| PGP-IMP-015 | High (part) | Interview (G1) | Attest PIN use, PW1 mode and prompt are decisions. Status display and certificate disposal are separable. |
| PGP-IMP-016 | Medium | Interview (no gate, no rule) | Oracle scope. Host-side oracles need no device; Unverify control needs a device lane. |
| PGP-IMP-018 | Medium (Low timestamp) | Interview (G3, G5) | Unknown policy: Python and Android use Once, Rust uses Always. Lossless carrier is Sia's lean. Timestamps are A3. |
| PGP-IMP-019 | Medium | Interview (G3) | The 6982 cause is ambiguous in all siblings. Python and Rust agree on CHANGE and Reset-Code mappings; Android maps less. |
| PGP-CORE-001 | Medium | Interview (no gate, no rule; T2) | Reader units versus card profile. Shared with CTAP, OATH and PIV reader policy. |
| PGP-CORE-003 | Medium | Interview (no gate, no clean rule; E1) | GET VERSION fallback. Siblings split three ways; the strictest choice breaks pre-1.0.2 cards. |
| PGP-CORE-004 | Medium (Low delay) | Interview (G5; T2) | Advisory UIF logging failure blocks crypto. Nonthrowing option diverges from .NET precedent. |
| Physical/chronology qualification | none assigned | T1 | Needs authorized device, firmware, transport and OS lanes. |

## Interview queue (sorted High, then Medium, then Low)

### PGP-IMP-001: user identity in both verify modes
- Gate(s): G1 (High). A1 would otherwise apply: "No-brainer yes" for the identity separation.
- Severity / certainty: High. Current source confirmed. Identity fix matches three siblings.
- Decision: approve the identity-only VERIFY change (retry and derivation use PW1 in both modes; P2 = 81 or 82 by mode) and the wire-test correction?
- Options:
  - A. Approve as written. Sia's lean: A. Extended verification then reports User retries. Reset Code no longer derives a PW1 PIN. §7.2.11 requires PW1 with P2 82 for DECIPHER, and all siblings already use this identity.
  - B. Hold and land it with the T2 retry-identity decision (PGP-IMP-019). One more cycle, but one edit to the VERIFY path.
  - C. Keep Reset identity for extended mode and document it. Retry reports stay wrong (historical 3→2 with zero reported). Not recommended.
- T2 tag: theme 1.
- Evidence: .NET `src/OpenPgp/src/OpenPgpSession.Pin.cs:31-35`, `src/OpenPgp/src/Pw.cs:25-32`. Python `yubikit/openpgp.py:1205-1232` @4ca60f70. Android `…/OpenPgpSession.java:340-357, 371-374` @f4626856. Rust `crates/yubikit/src/openpgp.rs:1931-1956` @90940e9b.

### PGP-IMP-002: persistent KDF transitions
- Gate(s): G1 (High).
- Severity / certainty: High risk. Current flow confirmed; transition contract unsettled.
- Decision: ship low-level F9 provisioning with a documented caller procedure, or add a validated transition helper now?
- Options:
  - A. Low-level write, documented procedure, fresh-session readback. Sia's lean: A. No new public helper. Siblings do the same: Python `set_kdf` documents PIN reset and Reset Code invalidation; Rust and Android only write.
  - B. Vendor-profile transition helper that validates initial hashes. Needs a qualified profile first (T1 lane).
  - C. Refuse KDF enable in the SDK until B exists. Removes accidental credential changes but drops a capability all siblings keep.
- T2 tag: theme 2 (initial PIN hashes in F9 encodings).
- Evidence: .NET `OpenPgpSession.Config.cs:190-207`, `OpenPgpSession.Pin.cs:336-338`. Python `yubikit/openpgp.py:1165-1192`, `1253-1273` @4ca60f70. Rust `openpgp.rs:2120-2135` @90940e9b. Android `…/OpenPgpSession.java:329-337` @f4626856.

### PGP-IMP-003: KDF aliases and independent secret copies
- Gate(s): G1 (High).
- Severity / certainty: High. Source-confirmed; no exploit shown.
- Decision: should the session stop caching the KDF object and reread F9 per operation, as all three siblings do?
- Options:
  - A. Drop the cache. Reread F9 per PIN or Reset-Code operation. Propagate F9 errors; keep KdfNone only for known absence (capability flag clear). Clear each encoded copy in finally. Sia's lean: A. Costs one extra GET DATA per credential operation; removes the alias and stale-cache hazard. Precedent: Python `get_kdf` (openpgp.py:1165-1172), Android `getKdf` (OpenPgpSession.java:307-313), Rust `get_kdf` (openpgp.rs:2106-2118). None of them catch F9 errors.
  - B. Keep the cache with session-owned clones; dispose replaced, reset and disposed values. More ownership code; still stale after raw F9 writes unless PGP-IMP-009 lands too.
  - C. Keep borrow semantics and document caller disposal. The alias-clearing hazard stays.
- T2 tag: theme 2.
- Evidence: .NET `OpenPgpSession.Config.cs:199-206`, `OpenPgpSession.cs:383-401` (fallback 394-398), `Kdf.cs:324-326` (ToBytes copy), `Kdf.cs:350-360` (ParseData clones), `OpenPgpSession.Reset.cs:71-72`.

### PGP-IMP-005: reset status, postcondition and channel state
- Gate(s): G1 (High). G3 on the blocking loop: all three siblings also ignore per-attempt errors (Python `openpgp.py:1110-1133` `except ApduError: pass`; Android `…/OpenPgpSession.java:481-489` "Ignore"; Rust `openpgp.rs:2144-2151` result unused). Precedent does not settle when blocking is confirmed.
- Severity / certainty: High. Unchecked statuses confirmed; live failure not reproduced.
- Decision: after a successful reset, should the session be terminal (caller reopens a fresh session), or should the SDK support in-session reinitialization?
- Options:
  - A. Terminal. Check TERMINATE and ACTIVATE statuses; stop on unrelated blocking failure; report completion only after fresh-session readback. Sia's lean: A. Callers reopen after reset (alpha; breaking allowed). Removes the cached-state and SCP-graph ambiguity. The status checks are sibling-settled (Python `send_apdu` raises; Android `openpgp/…/OpenPgpSession.java:494-495`; Rust `openpgp.rs:2154-2155` uses `?`).
  - B. In-session reinitialization with defined cache, readiness and channel restoration. Larger state machine; T1 device lane.
  - C. Keep the current in-session refresh and document it. Completion can still be mistaken for erasure.
- T2 tags: themes 3 and 10.
- Evidence: .NET `OpenPgpSession.Reset.cs:44-55`, `:66`, `:71-72`, `:84-94`.

### PGP-IMP-009: partial key mutation and stale cache
- Gate(s): G1 (High). G3: all siblings share the non-atomic attribute, generate and delete sequence; none has rollback.
- Severity / certainty: High risk. Source-confirmed non-atomic flow.
- Decision: after a partial persistent write, should the session pre-validate and mark affected cached state invalid (reporting the failed phase), or repair the cache with immediate readback?
- Options:
  - A. Pre-validate before the first destructive write. On failure, mark affected cached attributes, UIF and app data invalid and report the phase. Never auto-retry a destructive step. Sia's lean: A. Callers refresh after failures; no false cache. §4.4.3.9 says a changed attribute "should delete this key internally", so destruction is expected card behaviour.
  - B. Readback after each write and repair the cache. More APDUs; still no rollback.
  - C. Keep the sequence and document it. Stale cache remains after partial failure.
- T2 tag: theme 3.
- Evidence: .NET `OpenPgpSession.Keys.cs:27-46` (42-45), `:63-71` (attribute gate precedent), `:97-100`, `:104-128` (115-122). Python `yubikit/openpgp.py:1408-1435`, `1525-1556`, `1581-1605`, `1608-1627` @4ca60f70. Android `…/OpenPgpSession.java:1107`, `1181-1206` @f4626856. Rust `openpgp.rs:2405-2420`, `2421-2432` @90940e9b.

### PGP-IMP-011: ROCA generation and test applicability
- Gate(s): G1 (High).
- Severity / certainty: High. Advisory and source confirmed; no key attack performed.
- Decision: which ROCA generation guard should ship: conservative [4.2.0, 4.3.5) as Python and Rust use, or advisory-exact [4.2.6, 4.3.5) as Android uses?
- Options:
  - A. Conservative [4.2.0, 4.3.5) for on-card RSA generation only, checked before any attribute write. Never exclude corrected 4.3.5. Imports unaffected. Sia's lean: A. Refuses generation on 4.2.0–4.2.5, which the advisory does not list; no vulnerable key is created.
  - B. Advisory-exact [4.2.6, 4.3.5). 4.2.0–4.2.5 generation allowed; relies on the advisory list being complete.
  - C. No guard, documented. Not recommended.
- Also: the four integration sites must assert or report unsupported; none may return normally (`src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpSessionTests.cs:216-220, 271-272`; `OpenPgpDecryptTests.cs:27-28`; `OpenPgpKeyImportTests.cs:54-55`). That part is mechanical once the range is chosen.
- T2 tag: theme 7 (PIV has the same class of exclusion starting at 4.2.0, per the master's PIV section).
- Evidence: .NET `OpenPgpSession.Keys.cs:27-46` (no guard). Python `yubikit/openpgp.py:1525-1556` @4ca60f70. Rust `crates/yubikit/src/openpgp.rs:2321-2331` @90940e9b. Android `…/OpenPgpSession.java:111-117` @f4626856.

### PGP-IMP-015: tool input, state and resource contracts
- Gate(s): G1 (High) for secret exposure and wrong-device mutation. Low facets (status display, certificate disposal) are separable.
- Severity / certainty: High (part). Selected sites confirmed.
- Decision: how should `keys attest` acquire and use the User PIN?
- Options:
  - A. Require the User PIN (prompt or `--pin`). VERIFY in ordinary mode P2 = 81 before FB. Fail before any APDU if no PIN is supplied. Sia's lean: A. Matches the Python CLI (`ykman/_cli/openpgp.py:529-569`) and the historical successful ordinary-mode attestation. The `--pin` option stops being dead code.
  - B. Verify in extended mode P2 = 82 before FB. The historical extended-only FB attempt failed on serial 103 (scoped evidence). Needs T1 proof first.
  - C. Keep attest unauthenticated and document that the caller verifies. `--pin` stays dead. Not recommended.
- Separable now, approvable in the same reply (A-level facets): display status from key information instead of fingerprint length (`InfoCommand.cs:71-79`; Python `get_key_information` openpgp.py:1472-1475; Rust `openpgp.rs:1887-1893`). Dispose exclusively owned certificates (`src/Cli.Commands/src/OpenPgp/OpenPgpCertificatesCommands.cs:74, 123-150`; `OpenPgpKeysCommands.cs:304-336`, which has no PIN step either).
- Persisted `--pin` and PEM strings are T2 theme 2. No unsupported erasure claim.
- T2 tags: themes 2, 8, 9 and 11.
- Evidence: .NET `src/OpenPgp/examples/OpenPgpTool/Cli/Commands/KeysAttestCommand.cs:28-40`, `InfoCommand.cs:71-79`. Python `ykman/_cli/openpgp.py:529-569` @4ca60f70.

### PGP-IMP-010: capability and family admission
- Gate(s): G5. Marking synthesized entries is Sia's proposal and diverges from all three siblings. Python `openpgp.py:1349-1406`, Android `…/OpenPgpSession.java:739-800` and Rust `openpgp.rs:2177-2215` all synthesize pre-5.2.0 RSA lists without marking.
- Severity / certainty: Medium. Source gaps confirmed; old-family range unqualified.
- Decision: for pre-5.2.0 firmware, should algorithm discovery synthesize the old RSA entries as the siblings do, and should synthesized entries be marked?
- Options:
  - A. Synthesize as siblings do, unmarked. Matches siblings; callers cannot tell device-reported from synthesized entries.
  - B. Synthesize and mark each synthesized entry. Sia's lean: B. Callers avoid sizes the card never reported. Small public API addition (alpha; allowed).
  - C. Report only device-reported entries. Old families may show empty lists; no sibling does this.
- Separable A2 facets (approvable with this reply): challenge flag and maximum-length check (Python `openpgp.py:1048-1060`; Android `…/OpenPgpSession.java:437-447`; Rust `openpgp.rs:2589-2600`; .NET `OpenPgpSession.cs:216-237` has neither). Cached and ATT UIF gated at 5.2.1 (Python `openpgp.py:1443-1469`; Android `…/OpenPgpSession.java:711-722`; Rust `openpgp.rs:2276-2284`; .NET `Config.cs:48-62` checks only FeatureUif). NEO PIN-attempt gate at 1.0.7 versus 4.3.1 (Python `openpgp.py:1135-1162`; Rust `openpgp.rs:2080-2100`; .NET `Pin.cs:196-223` has no gate).
- T2 tags: themes 5 and 6.
- Evidence: .NET `src/OpenPgp/src/OpenPgpSession.Config.cs:211-217`, `:48-62`; `OpenPgpSession.Pin.cs:196-223`; `OpenPgpSession.cs:216-237`.

### PGP-IMP-016: meaningful oracles and failure cleanup
- Gate(s): none. No auto rule fits. Needs a decision on test scope and on a device lane (no-rule route).
- Severity / certainty: Medium. Representative oracles confirmed.
- Decision: upgrade only the host-verifiable oracles now (signature and attestation assertions, independent X25519 vectors), and leave the Unverify device control for T1?
- Options:
  - A. Host-side oracles now. Unverify control (Once/Always and both PW1 modes) goes to the T1 lane. Sia's lean: A. The length-only assertions get replaced without touching a card.
  - B. Everything in one change after a T1 session. Waits on hardware.
  - C. Correct comments only. The weak assertions stay.
- T2 tag: theme 1 (Unverify and verification state).
- Evidence: .NET `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpSessionTests.cs:255-260`, `OpenPgpPinManagementTests.cs:62-81` (comment at 80); `src/OpenPgp/tests/CLAUDE.md:34`.

### PGP-IMP-018: open values, credential format and generation time
- Gate(s): G5 (lossless carrier diverges from every sibling) and G3 (Python and Android share the Once fallback, Rust differs; Rust's Always is a sibling split, so precedent is not settled).
- Severity / certainty: Medium for credential and parser defaults; Low for timestamp display.
- Decision: should unknown signature-policy bytes be preserved in an open carrier (Sia's lean, per the forward-compatibility doctrine in CLAUDE.md), or mapped to a fixed value?
- Options:
  - A. Preserve raw unknown policy bytes. Keep malformed encodings and required fields strict. Sia's lean: A. Callers can see new values; typed enum changes (alpha).
  - B. Map unknown to Always, as Rust does. Fail-closed on PIN re-entry; loses the raw value.
  - C. Keep Once, as Python and Android do. The weakest of the three; not recommended.
- Separable A3 facet (approvable now): generation times as unsigned seconds, with zero meaning unspecified. Python `_parse_timestamps` (openpgp.py:588-593) and `set_generation_time` (1482-1492) are unsigned. Rust uses `u32` (openpgp.rs:1895-1910, 2301-2307). Android stores `int` with identical bits (`…/OpenPgpSession.java:857-860`). Card DO CD: 4 bytes big-endian, seconds since 1970, 00000000 not specified.
- T2 tag: theme 12.
- Evidence: .NET `src/OpenPgp/src/PwStatus.cs:109-112`, `src/OpenPgp/src/DiscretionaryDataObjects.cs:160-174, 176-188`. Python `yubikit/openpgp.py:344-357` @4ca60f70. Rust `openpgp.rs:209-222` @90940e9b. Android `…/PwStatus.java:88-91` @f4626856.

### PGP-IMP-019: PIN status provenance and retry uncertainty
- Gate(s): G3 (the 6982 cause is ambiguous in all three siblings; precedent does not settle it).
- Severity / certainty: Medium. Current mapping confirmed.
- Decision: should VERIFY, CHANGE and Reset-Code failures map only the statuses the siblings map (6982; 63Cx before 4.0 for VERIFY; 6983), keeping every other SW with provenance? Or keep the broad mapping with an explicit uncertainty field?
- Options:
  - A. Narrow typed mapping. Raw SW always retained. Unknown count reported as unknown. No automatic retry on ambiguous failure. Sia's lean: A. Unrelated and secure-messaging failures stop reporting "wrong PIN". Matches Python and Rust for VERIFY.
  - B. Broad mapping plus explicit uncertain-cause and retry-count fields. More API; callers still need to avoid retries.
  - C. Keep the current mapping. Unrelated failures can prompt retries that spend attempts.
- Note: Python and Rust agree on CHANGE and Reset-Code. CHANGE: 6982, or 6985 before 4.0, gives invalid PIN; 6983 gives blocked. Reset Code: 6982 gives invalid with the reset-code count; 6983 or 6A80 gives blocked (`yubikit/openpgp.py:1253-1273`, `1308-1337`; `crates/yubikit/src/openpgp.rs:1972-1994`, `2034-2066`). Android maps less: 6982 only (`…/OpenPgpSession.java:528-560`, `634-670`). Decide one mapping for all three operations.
- T2 tag: theme 1.
- Evidence: .NET `src/OpenPgp/src/OpenPgpSession.Pin.cs:241-280` (257-261), `:284-323`, `:144-193` (generic reset path), `:325-363`. Python `yubikit/openpgp.py:1205-1218` @4ca60f70. Rust `crates/yubikit/src/openpgp.rs:1931-1949` @90940e9b. Android `…/OpenPgpSession.java:340-357` @f4626856.

### PGP-CORE-001: reader units and card profile
- Gate(s): none. No auto rule. The reader-unit policy is shared across applets (T2), so it is decided once there.
- Severity / certainty: Medium. Source answer complete; policy open.
- Decision: should OpenPGP check the card profile itself after KDF derivation (applet-local, as Python and Rust do), keeping the presets as UTF-16 text limits? Or wait for a shared profile-aware acquisition in Core?
- Options:
  - A. Applet-local byte-length check after derivation, against the PW minimum and the card's status maximum, for supplied and interactive input alike. Presets documented as UTF-16 limits. Sia's lean: A now. Rejected input never reaches the card; no Core change.
  - B. Shared profile-aware acquisition (T2). Broader change across OATH, PIV, CTAP and OpenPGP.
  - C. Document only. Supplied bytes can still exceed the card limit.
- T2 tag: theme 4.
- Evidence: .NET `src/OpenPgp/src/Credentials/OpenPgpCredentialOptions.cs:22-39`, `src/OpenPgp/examples/OpenPgpTool/Cli/Commands/OpenPgpCommand.cs:110-118`, `OpenPgpSession.Pin.cs:241-251` (no derived-length check). Python `yubikit/openpgp.py:1194-1203` @4ca60f70. Rust `crates/yubikit/src/openpgp.rs:1912-1925` @90940e9b.

### PGP-CORE-003: version fallback and effective profile
- Gate(s): none. Siblings split three ways, so A3 does not apply (Android has no fallback). The strictest pick would drop pre-1.0.2 support, which is an E1 stop.
- Severity / certainty: Medium policy risk. Source-confirmed.
- Decision: should GET VERSION fall back to 1.0.0 only on SW 6985, as Python and Rust do?
- Options:
  - A. Narrow: 6985 only maps to 1.0.0. Other statuses propagate. Short or invalid BCD is an error. Sia's lean: A. Keeps pre-1.0.2 cards working (Python comment at `openpgp.py:1025`: pre-1.0.2 versions don't support reading the version) and stops masking unrelated failures.
  - B. No fallback, as Android does (`…/OpenPgpSession.java:191-199`). Pre-1.0.2 cards cannot open.
  - C. Keep the broad fallback. Unrelated failures become 1.0.0; this is the current behaviour.
- E1: no pre-1.0.2 device is in the qualification record. Option A's pre-1.0.2 claim stays unqualified until one exists.
- Short responses also differ: Rust pads missing BCD with zero (`openpgp.rs:1770-1776`); Android indexes three bytes and throws. Decide that too.
- T2 tag: theme 5.
- Evidence: .NET `src/OpenPgp/src/Backend/OpenPgpBackend.cs:67-93`. Python `yubikit/openpgp.py:1019-1028` @4ca60f70. Rust `crates/yubikit/src/openpgp.rs:1767-1790` @90940e9b. Android `…/OpenPgpSession.java:191-199` @f4626856.

### PGP-CORE-004: whole-workflow and notification result ownership
- Gate(s): G5. Sia's nonthrowing advisory read diverges from the current .NET precedent, which logs and propagates by design (`OpenPgpSession.cs:365-366`: "Logging failures propagate"). No sibling has an equivalent advisory read.
- Severity / certainty: Medium (lifetime and concurrency risk). Low for public-certificate delay.
- Decision: when the advisory UIF read cannot log, should the signing operation still proceed?
- Options:
  - A. Yes. Catch logging failures on the advisory path, keep any primary exception, and fall back to PolicyMayRequire. Sia's lean: A. A broken logger can no longer block signing.
  - B. No. Keep propagation and document it. A broken logger blocks crypto.
  - C. Defer with the T2 workflow-lease decision. The certificate and presence question waits.
- Also: attest result ownership (the certificate is created inside the notification callback, `Keys.cs:150-185`) is mechanical once the callback contract is chosen (theme 3). The workflow lease itself is T2.
- Evidence: .NET `src/OpenPgp/src/OpenPgpSession.cs:327-378` (363-369), `src/OpenPgp/src/OpenPgpSession.Keys.cs:150-185`, `src/Core/src/Utilities/ExchangeGuard.cs:49-63`. Python `yubikit/openpgp.py:1699-1713` @4ca60f70 (synchronous, no callback). SAMPLE-PGP-001 units from the master: `8b2a95f01db9bf2c`, `710814862611f595`.
- T2 tags: themes 2, 3, 8 and 9.

## Auto-approved

### PGP-IMP-007: X25519 decipher framing
- Rule: A1 (master: "Yes for wrapper"). Also A2 corroborated: Python, Android and Rust wrap X25519 identically.
- Evidence pin (E2):
  - .NET BEFORE: `src/OpenPgp/src/OpenPgpSession.Crypto.cs:251-264`, X25519 arm at 260 @52d34ca65. The in-source comment at 255-256 already cites §7.2.11.
  - Python: `yubikit/openpgp.py:1736-1768` @4ca60f70. EC branch wraps every EC key; X25519 uses `Encoding.Raw`.
  - Android: `…/OpenPgpSession.java:1282-1302` @f4626856 (wrap at 1298-1299). `core/…/PublicKeyValues.java:102-122` `Cv25519.getBytes()` returns the raw point.
  - Rust: `crates/yubikit/src/openpgp.rs:2466-2483` @90940e9b. EC arm wraps every EC key.
  - Spec: 3.4.1 §7.2.11. ECDH input is "A6 xx Cipher DO / 7F49 xx Public Key DO / 86 xx External Public Key".
- Pseudocode:
```text
FormatDecryptPayload(attrs, ciphertext):
  RSA      -> 0x00 || ciphertext                    (unchanged)
  EC, any  -> A6 { 7F49 { 86 ciphertext } }        (X25519 no longer bypasses)
  other    -> NotSupported                          (unchanged)
unchanged: X25519 import scalar reversal, key-consistency checks, NIST uncompressed input
```
- Test to add: exact vector. A 32-byte RFC 7748 point gives the data prefix `A6 25 7F 49 22 86 20` and a DECIPHER APDU `00 2A 80 86 ... 00`. Shared-secret equality against a fixed RFC 7748 Alice/Bob vector. NIST and RSA paths unchanged. The raw response is zeroed in finally (theme 2 clause only).
- E1 escalation risk: stop if a card rejects the raw 32-byte X25519 point under this wrapper. The historical serial-103 control accepted it. §7.2.11 gives the `04||x||y` form only for NIST curves, so the X25519 external-key form is not stated in that excerpt. Device confirmation is a T1 follow-up, not a gate.

### PGP-IMP-013: attestation CRT template
- Rule: A1 (master: "Yes for encoder"). Also corroborated by Python, Rust and Android.
- Evidence pin (E2):
  - .NET BEFORE: `src/OpenPgp/src/Crt.cs:55-60` @52d34ca65 (`inner.Value.Span` at 58). The remark at 42-46 already states the defect.
  - Python: `yubikit/openpgp.py:361-367` (`CRT.ATT = Tlv(0xB6, Tlv(0x84, b"\x81"))`) @4ca60f70.
  - Rust: `crates/yubikit/src/openpgp.rs:416-425` (ATT nests 0x84 in 0xB6). Test `:2700-2701` asserts `B6 03 84 01 81` @90940e9b.
  - Android: `…/Crt.java:25-26` (`ATT = B6 { 84 { 81 } }`) @f4626856.
  - Spec: key ref 0x81 is not in 3.4.1. The Yubico attestation extension governs, so no G2 check applies.
- Pseudocode:
```text
Crt.BuildAttCrt():
  inner = TLV(0x84, [0x81])
  outer = TLV(0xB6, inner.AsSpan())     // was inner.Value.Span
  return outer.AsSpan().ToArray()
```
- Test to add: `Crt.Att` equals `B6 03 84 01 81`. `Crt.Sig`, `Crt.Dec` and `Crt.Aut` are unchanged. A fake-connection GENERATE ASYMMETRIC (P1 = 81) request for ATT carries the nested CRT.
- E1 escalation risk: stop if a device accepts only the legacy `B6 01 81` for ATT read, generate or import. Optional T1 check: public read-existing comparison on an authorized device, with no private replacement.

### PGP-IMP-014: NEO import, generation and deletion
- Rule: A4 for the deletion facet. Siblings split: Python (`openpgp.py:1608-1627`) and Android (`…/OpenPgpSession.java:1181-1206`) overwrite with a generated RSA 2048 key; Rust (`openpgp.rs:2421-2430`) refuses NEO deletion. The stricter, fail-closed choice is the refusal. A2 for the format and generation-gate facets: Python, Android and Rust all use CRT-with-modulus (03) for major 1–3, and Python and Rust write attributes only when `ALGORITHM_ATTRIBUTES_CHANGEABLE` is set (`openpgp.py:1525-1556`; `openpgp.rs:2333-2346`).
- Evidence pin (E2):
  - .NET BEFORE: `src/OpenPgp/src/PrivateKeyImport.cs:103-105` @52d34ca65 (format choice); `PrivateKeyTemplate.cs:164-183` (`RsaCrtKeyTemplate` includes modulus); `OpenPgpSession.Keys.cs:27-46` (generation writes attributes unconditionally); `:104-128` (delete).
  - Internal precedent for the capability gate: `OpenPgpSession.Keys.cs:63-71` (PutKeyAsync).
  - Python: `yubikit/openpgp.py:883-897` (CRT_W_MOD for major 1–3) @4ca60f70.
  - Rust: `crates/yubikit/src/openpgp.rs:456-466` (CrtWMod = 3), `:2421-2430` (NEO refusal) @90940e9b.
  - Android: `…/OpenPgpSession.java:754` and `:980-988` (CRT_W_MOD below 4.0.0) @f4626856.
  - Spec: 3.4.1 §4.4.3.9. Import format 03 is "crt with modulus (n)"; 02 is "crt".
- Pseudocode:
```text
RSA import template, NEO (major 1..3):   label CRT-with-modulus (03) when the template carries n
GenerateKeyAsync:                        write attributes only when AlgorithmAttributesChangeable
                                         (mirror PutKeyAsync Keys.cs:63-71); otherwise require RSA 2048
DeleteKeyAsync, NEO (major 1..3):        throw NotSupported("Key deletion is not supported on YubiKey NEO")
                                         before any APDU
DeleteKeyAsync, other:                   unchanged RSA 4096 then RSA 2048 sequence (PGP-IMP-009 applies)
```
- Test to add: NEO template label is 03 and includes n (fake firmware 3.x). Modern template unchanged. NEO delete throws with zero APDUs recorded. Modern delete sequence unchanged. Generation on a non-changeable fake device writes no attributes.
- E1 escalation risk: stop if a NEO-class device (major 1–3) reports `ALGORITHM_ATTRIBUTES_CHANGEABLE`, or rejects the label-03 template. Dedicated old-family hardware readback is a T1 follow-up for any claimed support. The refusal's unit test needs no device.

## T1 hardware qualification
| Card | What needs a device | Device/firmware/transport/OS |
| --- | --- | --- |
| Physical/chronology qualification | Re-run the 49-method suite (initial 120 s run stopped after 24 passes, so it is interrupted, not passed). F9 refusal after key generation versus later success. Extended-only FB failure versus ordinary-mode success. 1500 ms cancellation (request 2074 ms, terminal 14811 ms, 6600/Failed). Native completion and release timing. ATT certificate restore to DER SHA256 `94afb703099edb4aaaee443c6e197c379f7f909d23f717f0868ef73057e4a16a`. Each mutation needs explicit authorization. | Historical: serial 103, Linux/Omarchy USB SmartCard, metadata 5.8.0.beta.0, SDK and Python 0.0.1, CLI application 5.8.0, card 3.4. Not yet covered: NFC, Windows and macOS, JIT versus Native AOT, pre-1.0.2 and NEO-class or 4.2.6–4.3.4 families. |
| Follow-up: PGP-IMP-001 | Re-run the intended extended-mode User PIN contract on an authorized dedicated key. Keep the original failed assertion as evidence. | Authorized dedicated OpenPGP key (serial 103 class), USB SmartCard. |
| Follow-up: PGP-IMP-007 | Confirm the raw 32-byte X25519 point is accepted under the A6 wrapper (historical control only). | Card 3.4 device (serial 103 class), USB SmartCard. |
| Follow-up: PGP-IMP-013 | Public-only read-existing ATT comparison. No private replacement. | Authorized device with an attestation key. |
| Follow-up: PGP-IMP-014 | Hardware readback of NEO-class import and deletion refusal, if support is claimed. | Authorized dedicated NEO-class device (major 1–3). |
| Follow-up: PGP-IMP-011 | Applicability on corrected 4.3.5 and on imported keys. Do not generate vulnerable keys for coverage. | Devices on 4.2.x and 4.3.x; 4.3.5. |
| Follow-up: PGP-CORE-003 | GET VERSION behaviour on a pre-1.0.2 card (SW 6985). | Pre-1.0.2 device. None on record. |
| Follow-up: PGP-IMP-005 | Reset postcondition and fresh-session readback. Destructive. | Authorized dedicated device. |
| Follow-up: PGP-IMP-016 | Unverify effect with Once/Always policy and PW1 modes 81/82. | Authorized device whose PIN policy can be set. |
| Follow-up: PGP-CORE-004 | Native cancellation and completion timing. | Installed PC/SC producer on Windows, macOS and Linux. |

## T2 cross-cutting candidates
| Theme | Card(s) here | Why it is cross-cutting |
| --- | --- | --- |
| 1. PIN verification state, retry identity and failure classification | PGP-IMP-001, -016, -019 | The policy's T2 example (session state after failed authentication in OATH, PIV and YubiHSM Auth) covers this. OATH-DEC-001 is the OATH card; the PIV path was not re-read here. |
| 2. Secret copy ownership, zeroing and disposal | PGP-IMP-002, -003, -007 (cleanup clause), -015, PGP-CORE-004 | Core C-C005/C006 and B6 copies recur in the HSM X002 and PIV owner tables and in the OTP one-shot boundary (master shared ledger). |
| 3. Whole-workflow admission, lease and callback reentrancy | PGP-IMP-005, -009, PGP-CORE-004 | CORE-B5-C006 also covers OATH workflows, the WebAuthn ceremony, PIVCORE-002, HSM X003 and CTAP sequences (master shared ledger). |
| 4. Reader units and credential profile | PGP-CORE-001 | C-C001–C004 are shared with CTAP-IMP-006, the OATH reader rows and PIV source acquisition (master shared ledger). |
| 5. Version and effective-profile fallback | PGP-CORE-003, PGP-IMP-010 | A-C015/B5-C001 recurs in OATH-DEC-007, CTAP-IMP-002, PIVIMP-003 and HSM X004 (master shared ledger). |
| 6. Firmware and family admission, NEO scope | PGP-IMP-010, -011, -014 | PIVIMP-003 asks for qualified firmware and product policy. Whether NEO scope recurs in other applets is not verified. |
| 7. ROCA generation guard | PGP-IMP-011 | PIV's Python ROCA exclusion starts at 4.2.0 (master PIV section); one range should serve both. |
| 8. Presence prompt and advisory notification or logging | PGP-IMP-015, PGP-CORE-004 | IUserPresencePrompt is a cross-applet contract. Core C-C008 logging owner is shared with CTAP X020 and WA X004. |
| 9. CLI certificate disposal and deterministic-certificate class | PGP-IMP-015, PGP-CORE-004 | SAMPLE-A004 is the same class as PIV certificate export and import (master lines 6743–6745, PIV section). |
| 10. Reset postcondition and retained channel state | PGP-IMP-005 | Core B6-C003/C015 and the HSM and Security Domain reset rows ask the same postcondition question. |
| 11. CLI credential acquisition before sensitive operations | PGP-IMP-015 | The PIV CLI verifies PIN before signing (master PIV section). One shared credential owner should serve both. |
| 12. Open-value carriers for protocol identifiers | PGP-IMP-018 | CLAUDE.md forward-compatibility doctrine; Core open-value owner (master ledger, lines 6839–6841). |

## Coverage check
Review map rows (18). Each appears once in the Index: PGP-IMP-001, -002, -003, -005, -007, -009, -010, -011, -013, -014, -015, -016, -018, -019; PGP-CORE-001, -003, -004; Physical/chronology qualification. Count matches.

Coverage-ledger exclusions deliberately not treated as cards:
- PGP-IMP-004/006/008/012/017/020/021: no engineering IDs were invented for documentation-only candidates (master line 7078).
- PGP-REV-001–005 and the C022 timestamp prose: resolved documentation. C022 is folded into PGP-IMP-018.
- PGP-CORE-002: answered in the ledger (factory borrow and async dispatch). Its open part sits in PGP-IMP-003 and PGP-CORE-004.
- PGP-DEP-001–021 (21 labels): documentation dependencies with zero additional Core bug count. Each reopens under its owning card.
- CORE-PGP-X001–X008: mapped to cards in the boundary ledger. Core C-C and B5/B6 items are dependencies, not new cards.
- SAMPLE-PGP-001 (inside PGP-CORE-004) and SAMPLE-A004 (inside PGP-IMP-015): kept under their owners.
- Windows successor CORE-B1/B4 items: PC/SC and HID, not this SmartCard applet. Excluded.
- OATH resolved IMPs, descriptor and User XREFs, the retracted HSM retry claim: other applets.
- PGP-DEP-017 (task and API documentation): documentation only.

Policy gap: PGP-IMP-016, PGP-CORE-001 and PGP-CORE-003 hit no gate and match no clean auto rule. The policy does not say where such cards go. I routed them to the interview queue so the decision is not made silently.
