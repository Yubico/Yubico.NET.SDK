# YubiKit Pass 2: applet state and deliberate differences

All nine applet PRs are merged into `yubikit-pass2-base` at
[`4d217833`](https://github.com/Yubico/Yubico.NET.SDK/commit/4d217833964f35120ed54bb47e2d60fcf5328673),
with 13 successful GitHub checks on that commit. The aggregate [PR #788][base] is open as a draft on
hold for Core Pass 2. This is the applet delivery state, not a release or a claim of full SDK parity.

The comparisons below describe decisions that still matter on this base. “Python” means ykman and its
`yubikit` library at `4ca60f70`; “python-fido2” is the separate FIDO library at `5bc9d3a1c`. Rust means
the reviewed `yubikey-manager-rust-auto` at `90940e9bb`, and Android means `yubikit-android` at
`f46268563`, unless an evidence link specifies a different pin. These are source comparisons, not
claims about the latest sibling releases. Each applet's PR records its D1 comparisons and follow-ups;
the [final ledger][ledger] and [agreements][decisions] settle attribution and superseded decisions.

## Applet behavior

### OATH ([#789][oath])

- **Access keys and credential URIs.** `ValidateAsync` and `SetKeyAsync` require a 16-byte raw access
  key, matching Android's SET CODE check and Rust's access-key type rather than Python's permissive
  forwarding. This checks the YKOATH derived-key width before sending. For `otpauth` URIs, the query
  issuer wins as in Python and the Rust CLI, not the label issuer as in Android. A missing HOTP
  counter is zero; the SDK keeps its nonnegative `Int32` range (like Android's signed carrier, not
  the Python/Rust unsigned wire range) and 6–8 digits. The existing URI contract was retained; Android
  also uses 6–8 digits. OATH-DEC-002/003, AG-20;
  [`OathSession`](../../../src/Oath/src/OathSession.cs) and [`CredentialData`](../../../src/Oath/src/CredentialData.cs).
- **Reset and retry.** After confirmed RESET the successful reselect and SCP refresh follow Python;
  Android also reinitializes. If refresh fails, .NET ends the session and distinguishes confirmed
  reset from failed reinitialization. Python propagates the refresh error without ending its session.
  This prevents reuse of a session that did not refresh. A failed device HMAC during unlock, by
  contrast, leaves the .NET session locked and open, as in the reviewed siblings; a retry is not
  guaranteed to work. X2/AG-31 and OATH-DEC-001; [`OathSession`](../../../src/Oath/src/OathSession.cs).

### CTAP2 ([#790][ctap], UInt64 follow-up in [#798][wa])

- **Current PIN is not a new PIN.** A PIN being set or changed is NFC-normalized, with at least four
  Unicode code points, at most 63 UTF-8 bytes and no trailing NUL. Non-ASCII input requires .NET
  globalization support. The pinned siblings do not normalize;
  python-fido2 counts code points, Android UTF-16 units, and Rust bytes for the minimum. A *current*
  PIN is never normalized and is checked only for encodability, so callers can use existing PINs that
  would fail new-PIN policy. That facet deliberately departs from a literal CTAP 2.1 §6.5.1 reading.
  CTAP-IMP-006, AG-19/20; [`ClientPin`](../../../src/Ctap2/src/Pin/ClientPin.cs).
- **PIN/UV negotiation and replies.** The legacy PIN-token fallback is an alignment decision: like
  python-fido2, Rust and Android, .NET uses GetInfo's `pinUvAuthToken` option, not PIN protocol 1 vs 2.
  The legacy command does not request permissions or RP scope. .NET validates the peer key's required
  EC2/algorithm/P-256 labels as well as coordinate widths (peers consume X/Y without those label
  checks); unknown extra labels are ignored, not echoed. It rejects out-of-range retry counts rather
  than admitting signed values (python-fido2/Android) or narrowing (Rust), and checks decrypted token
  width like python-fido2/Rust, unlike Android. These checks prevent malformed replies becoming usable
  state. CTAP-IMP-007–010, AG-10/20/21; [`ClientPin`](../../../src/Ctap2/src/Pin/ClientPin.cs).
- **GetInfo and inventory.** .NET rejects missing or empty Versions and missing or wrong-width AAGUID
  instead of supplying identity defaults: python-fido2 requires Versions but defaults AAGUID, Rust
  defaults missing fields, and Android permits an absent AAGUID. Unknown fields stay available.
  Encrypted-field helpers reject IV-only values accepted by python-fido2/Rust and require the modeled
  token and ciphertext widths; decryption is not authentication. Vendor prototype command IDs are
  `ulong`, following CTAP and Python's admissible width rather than the pinned Rust `u32`. Credential
  inventory uses subcommand-only continuations like python-fido2, but requires a valid begin total
  (1–65,535) and a 32-byte RP hash instead of guessing from malformed data. The separate WebAuthn
  assertion cap does not narrow the CTAP DTO. CTAP-IMP-001/019/028/043, AG-19/31;
  [`AuthenticatorInfo`](../../../src/Ctap2/src/AuthenticatorInfo.cs),
  [`CredentialManagement`](../../../src/Ctap2/src/CredentialManagement/CredentialManagement.cs).
- **Bio and configuration.** Standard enrollment follows the standard wire format; the existing
  prototype route is explicit, not a fallback. Low-level capture returns non-Good feedback for the
  caller to interpret rather than applying Android's high-level capture-error policy. Inventory names
  are optional as in Android/Rust, not required as in python-fido2. Status `0x0C` is `DatabaseFull` as
  in python-fido2 rather than CTAP's unused assignment. `SetAlwaysUvAsync(bool)` reads before toggling
  so a retry expresses the desired state, and optional configuration fields distinguish omission from
  empty lists or explicit false. The explicit-false comparison to Rust is from the pinned review:
  Python/Android can send false; the reviewed Rust path omits it. CTAP-IMP-012–016, AG-12/16/21;
  [`BioEnrollment`](../../../src/Ctap2/src/BioEnrollment), [`AuthenticatorConfig`](../../../src/Ctap2/src/Config/AuthenticatorConfig.cs).
- **Large-blob storage.** Unlike the reviewed python-fido2, Android and Rust paths, .NET caps the
  serialized array at the advertised size (when present) and 16 MiB, caps reads at 32,768 fragments
  including the terminator, and caps each entry's plaintext at 1 MiB. The intent is bounded work and
  not accepting a local write larger than the reader supports, not imposing CTAP or device maxima.
  Short or hash-corrupt arrays act as empty per CTAP §6.10.2, unlike pinned Rust; valid-hash malformed
  CBOR is an error rather than an empty array. CTAP-IMP-036/037, AG-12/19;
  [`LargeBlobStorage`](../../../src/Ctap2/src/LargeBlobs/LargeBlobStorage.cs),
  [`LargeBlobData`](../../../src/Ctap2/src/LargeBlobs/LargeBlobData.cs).
- **Draft output.** previewSign CTAP Try extraction warns and omits malformed output like Rust, not
  the reviewed python-fido2/Android throwing paths. An entirely absent pair is not malformed.
  WebAuthn ignores unsigned-only output but rejects a decoded signed result without its unsigned
  counterpart. The .NET wrapped ARKG P-256 seed carrier is not a standard wire-shape claim;
  python-fido2 is the only compared sibling with ARKG. CTAP-IMP-029/038/039/040, AG-11/12/32;
  [`PreviewSign` extraction and codec](../../../src/Ctap2/src/Extensions/PreviewSign).

### Management ([#791][mgmt])

- **Preflights and metadata.** Enabling `NfcRestricted` requires 5.7 under the .NET gate. Python's
  library gates the generic write; its CLI additionally checks NFC availability. Device support
  remains authoritative; false/omitted recovery is not inferred. Public device-info reads use the
  sibling 4.1 floor, separate from the factory's best-effort probe. Configuration values outside the
  wire width are rejected, rather than expanded by Python or narrowed by Android; unknown in-width
  bits survive. FIPS decoding and the form-factor low nibble align, with an explicit one/two-byte FIPS
  bound and a raw form-factor byte for inspection. These fields do not prove approved operation or
  reset eligibility. MGMT-IMP-001/003/008, MGMT-XREF-001/002, AG-19/22;
  [`ManagementSession`](../../../src/Management/src/ManagementSession.cs), [`DeviceConfig`](../../../src/Management/src/DeviceConfig.cs).
- **Combined reset and legacy mode.** .NET uses a SmartCard/5.6 preflight (Python lacks the firmware
  check), lets the device decide family eligibility, and refreshes session/SCP after confirmed reset.
  This is not general erasure; the command targets FIDO/PIV/shared PIN and retains F9 attestation
  material. The dormant SmartCard SET_MODE encoder uses Python/Rust little-endian timeout bytes
  rather than Android's big-endian value. No public typed legacy mutation or runtime caller was
  added. Any future legacy routing must exclude prerelease/unknown firmware, rather than classifying
  numeric `0.x` as old like Python. MGMT-IMP-010/012, AG-14/15/22;
  [`ManagementSession`](../../../src/Management/src/ManagementSession.cs),
  [`SmartCardBackend`](../../../src/Management/src/Backend/SmartCardBackend.cs).

### Security Domain ([#792][sd])

- **Reset reports partial outcomes.** Python, Rust and Android can return normally when blocking
  attempts exhaust. .NET records outcomes by reference, still attempts remaining references and
  reports incomplete reset as failure. It does not refresh on uncertainty. On confirmed success it
  reselects and establishes default SCP03 on the same session regardless of the original channel;
  the peers do not reinitialize that way. The public defaults enable reprovisioning and confirm the
  defaults were accepted, not peer identity or complete factory contents. SD-IMP-001/005,
  AG-20/22/23; [`SecurityDomainSession`](../../../src/SecurityDomain/src/SecurityDomainSession.cs).
- **Strict modeled responses and GENERATE.** The SDK rejects malformed key and CA listing structure
  instead of dropping odd tails (Python/Rust) or parsing CA fields by position (including Android).
  Valid unknown descriptors remain visible. GENERATE rejects unsupported KIDs instead of forwarding
  arbitrary ones; explicit P-256 import, normalized DELETE and SCP03 KCV checks follow the reviewed
  peers. The secure-channel wrapper uses fixed outer text and preserves the original exception and
  card status for diagnosis, not full-log redaction. SD-IMP-002/004/006/008 and SD-DEP-005,
  AG-10/19/20; [`SecurityDomainSession`](../../../src/SecurityDomain/src/SecurityDomainSession.cs).

### YubiOTP ([#793][otp])

- **Programming and status.** Scan-map writes require exactly 45 bytes and firmware 3+ rather than
  the siblings' raw forwarding. A separate default-map reset uses the personalization tool's reset
  payload, not applet factory reset. The SmartCard sequence rule follows the Python/Rust precedent
  with a narrow `5.0.0 ≤ firmware < 5.4.3` window for reset to zero from a positive sequence; .NET
  uses effective session firmware, Python the returned status version. .NET admits byte wraparound,
  checks final status before publishing, and retains the calculated-data prefix rather than Python's
  exact SmartCard length. `ConfigState` exposes raw status firmware, not Python's session version;
  .NET's no-cache Management probe covers USB CCID and NFC, Python's NFC only. OTP-IMP-002/005/006,
  AG-14/20; [`YubiOtpSession`](../../../src/YubiOtp/src/YubiOtpSession.cs),
  [`SmartCardBackend`](../../../src/YubiOtp/src/Backend/SmartCardBackend.cs).
- **Legacy floors and protection.** HOTP, static and update floors are 2.2 with prerelease admission;
  the production floors follow Python/Rust, dropping .NET's older 2.1 HOTP and 2.0 static paths.
  On 4.3.2–4.3.5 .NET retains the restriction on changing protection but treats omitted and all-zero
  current/new codes as equivalent *for that comparison*. Pinned Python compares the arguments as
  supplied and appends six zero bytes on SmartCard when the current code is omitted. .NET sends the
  unextended payload; HID padding is separate. These are host rules, not a physical compatibility
  finding. OTP-IMP-006/007, AG-12/19/20 and [review follow-up][otp-codes];
  [`YubiOtpSession`](../../../src/YubiOtp/src/YubiOtpSession.cs),
  [`SmartCardBackend`](../../../src/YubiOtp/src/Backend/SmartCardBackend.cs).
- **NDEF.** Null content is rejected, not replaced with a default URI. `DisableNdefAsync` expresses
  disable separately; URI encoding uses the longest case-insensitive prefix rather than Python's
  first case-sensitive match. This preserves V1 null and case-handling intent, not a claim of
  identical encoding for every URI. OTP-IMP-008, AG-21/33; [`YubiOtpSession`](../../../src/YubiOtp/src/YubiOtpSession.cs).

### YubiHSM Auth ([#794][hsm])

- **LIST and key results.** .NET preserves unknown algorithm IDs (Python and Rust reject them) and a
  nullable touch policy rather than Python's boolean coercion. LIST rejects malformed records and
  requires 1–64 UTF-8 bytes for a label; Python uses that bound on input, not on decoded LIST labels.
  For CALCULATE, .NET follows Rust: require at least 48 bytes, take the first 48 and ignore the suffix.
  Python takes the same slices but can return short keys. .NET clears the entire owned reply; a
  standalone `SessionKeys.Parse` still requires exact width. This neither interprets the suffix nor
  establishes HSM authentication. HSM-IMP-003/008, AG-19/21; [`HsmAuthSession`](../../../src/YubiHsm/src/HsmAuthSession.cs),
  [`SessionKeys`](../../../src/YubiHsm/src/SessionKeys.cs).
- **Keys and reset.** GET PUBLIC KEY returns raw bytes instead of Python's typed key; point validation
  is not provenance. In asymmetric CALCULATE .NET retains the distinction between absent and empty
  optional cryptogram, like Rust's `None`/`Some(empty)` and unlike Python's falsey omission. The SDK
  reselects and reauthenticates after confirmed reset; Python just sends RESET. GET CHALLENGE still
  reports general APDU errors like Python/Rust; there is no inferred device retry policy. No Android
  HSM Auth session was identified in the reviewed comparison. HSM-IMP-001/007, CORE-HSM-X001,
  AG-20/22; [`HsmAuthSession`](../../../src/YubiHsm/src/HsmAuthSession.cs).

### PIV ([#795][piv])

- **Reset and response admission.** Bio preflight falls back only on `6A88` and `6D00`, matching
  Python rather than Rust's broad suppression; malformed or duplicate known metadata stops reset.
  Private-operation response shapes are checked before completion. Key History stays a fixed
  SP 800-73-4 model with raw access, not a general CHUID/CCC parser; raw object IDs must fit three
  bytes rather than aliasing by Android masking or using Python/Rust's wider natural encoding.
  These checks avoid treating malformed success or a different object as the requested result.
  PIVIMP-001/004/007a/010, AG-19/20;
  [`PivSession`](../../../src/Piv/src/PivSession.cs), [`DataObjects`](../../../src/Piv/src/DataObjects).
- **Existing data.** ADMIN timestamps keep V1's trimmed little-endian encoding rather than Python's
  fixed big-endian format; there is no byte-order auto-detection. Certificate reading admits Net iD
  zlib, rejects unmodeled CertInfo modes, and bounds decompression at 64 KiB, unlike the reviewed
  sibling readers' unbounded expansion. The ceiling is .NET policy, not a card limit. PIVIMP-007b/009,
  AG-19/20/21/33; [`AdminData`](../../../src/Piv/src/DataObjects/AdminData.cs),
  [`CertificateProtocol`](../../../src/Piv/src/Certificates/CertificateProtocol.cs).
- **PIN-only metadata is opt-in.** The raw methods and CLI remain raw. `*WithMetadataAsync` helpers
  preserve unrelated fields and coordinate ADMIN/PRINTED changes. Rotation clears legacy salt;
  PIN change in legacy derived-only mode re-derives with a fresh salt, following Python's `pivman`
  workflow. The pinned Rust companion coordinates rotation but does not clear the salt or implement
  legacy re-derivation. Unlike Python's boolean storage default or Rust's read-error fallback,
  .NET requires explicit storage intent and strict preflight. It refuses mixed stored/derived PIN
  changes and refuses retry-limit changes with legacy salt before mutation. The original failure and
  acknowledged/unconfirmed write phases are reported; there is no rollback or automatic retry.
  PIVIMP-002, AG-21/22/31; [`IPivSession`](../../../src/Piv/src/IPivSession.cs),
  [`PinOnlyProtocol`](../../../src/Piv/src/Authentication/PinOnlyProtocol.cs).

### OpenPGP ([#796][pgp])

- **Credentials, reset and old keys.** .NET keeps low-level F9 provisioning with caller readback
  rather than adding Python's `set_kdf` workflow. It separates C4 format bits and refuses PIN-block-2
  credential commands; both check processed PIN widths. For known pre-4.0 VERIFY, .NET uses an explicit
  `63Cx` nibble instead of Python's legacy C4 lookup, retaining the original status/header and not
  substituting a failed lookup. Reset stops on unrelated blocking errors, checks TERMINATE/ACTIVATE
  and refreshes state/SCP; Python ignores blocking errors and does not reinitialize after reset.
  The NEO reset gate still applies when applet version is known, and deletion is refused rather than
  using Python's RSA import-over workaround. PGP-IMP-002/003/005/014/019, PGP-CORE-001/003,
  AG-16/20/22; [`OpenPgpSession`](../../../src/OpenPgp/src/OpenPgpSession.cs) and [NEO review disposition][pgp-neo].
- **Forward-compatible reads.** Unknown policy, algorithm and curve values are preserved instead of
  defaulting to a known policy (Python falls back to Once) or failing applet initialization on
  unmodeled attributes. Malformed modeled fields still fail, and typed writes do not forward unknowns.
  Unsigned generation timestamps and the X25519 DECIPHER / ATT envelope shapes are aligned with
  Python, not remaining wire departures. PGP-IMP-007/013/018, AG-19;
  [`OpenPgpSession` and data models](../../../src/OpenPgp/src).

### WebAuthn ([#798][wa])

- **Verified PRF.** When evaluation applies, .NET requires the verified PRF variant under L3 §10.1.4
  and does not publish unverified PRF output. The pinned python-fido2, Android and Rust paths have
  no PRF-triggered UV override; that says nothing about UV they already select under ordinary policy.
  Resolve the selected credential's input (or fallback) before forcing UV or preparing agreement.
  After a complete silent miss, the optional supplied-PIN re-probe is allowed only with a configured
  PIN and applicable PRF inputs for **every** eligible candidate. A mixed list retains ordinary UV
  policy, so a hidden credential can still be missed. WA-IMP-011, AG-21;
  [`PrfProcessor`](../../../src/WebAuthn/src/Extensions/Processing/Processors/PrfProcessor.cs),
  [`Authentication`](../../../src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs).
- **Already-selected tokenless built-in UV.** Keep an associated positive silent probe, but do not
  treat an unverified miss as definitive when the ceremony already calls for built-in UV without a
  token. Unlike relying on the reviewed Python-fido2/Rust probe precedent alone, .NET can select
  through a real UV/UP assertion using the ceremony hash and eligible entries. It never publishes
  the silent response. It reuses the verified assertion unless a deferred input applies to the
  selected credential; only then does it send another assertion for that same credential (possible
  extra gesture and counter increment). Registration uses a fitting exclude list in the one creation
  command; for an oversized list it can reuse a positive probe or check exclusions with verified
  assertions first. Only statuses from the current device exchange can authorize advancing or
  shrinking a verified chunk, not an identically named status thrown by a callback or an ambiguous
  exception from a custom session. This preserves existing verification and exclusions, not broader
  UV escalation. WA-IMP-009/010/011, CTAP-IMP-003; [review disposition][wa-review],
  [`CredentialFilter`](../../../src/WebAuthn/src/Client/CredentialFilter.cs),
  [`Registration`](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs).
- **Assertion association and bounds.** Recover an omitted ID only from the final on-wire singleton,
  not the original unfiltered list or an arbitrary member. The reviewed Android path has singleton
  recovery; Python indexes the returned ID and Rust can substitute an empty ID. A singleton cannot
  yield multiple assertions, and a reported total above 1,024 raises `Constraint` before `GetNext`,
  not truncation. The CTAP count remains `uint`; 1,024 is a .NET ceremony resource limit, not a CTAP
  validity rule. WA-IMP-010, CTAP-IMP-028, AG-19/31;
  [`CredentialMatcher`](../../../src/WebAuthn/src/Client/Authentication/CredentialMatcher.cs).
- **Large-blob creation and writes.** Required storage with resident keys discouraged fails with
  `NotSupported` before ceremony device traffic. When discoverability depends on the device's
  capabilities, the SDK can read GetInfo but still rejects before `MakeCredential` if the effective
  credential would be nondiscoverable. For an explicit request, the pinned python-fido2, Android and
  Rust paths may send `largeBlobKey` without `rk`,
  which a conforming CTAP 2.3 authenticator rejects; actual YubiKey behavior here is unqualified.
  With preferred storage .NET can omit the key request and report unsupported without upgrading
  resident-key intent. For writes, .NET requires a **nonempty original allow list**, but agrees with
  python-fido2, Android and Rust in accepting multiple entries, deliberately departing from L3
  §10.1.5's exact-one rule. The write binds to the selected assertion's key, not the first candidate.
  Writes use token-authorized storage (like python-fido2's token requirement, unlike Android's
  tokenless attempt); the direct CTAP 2.2+ route and the pinless exception were not added. WA-IMP-014/015/017,
  AG-20/24/25; [`LargeBlobProcessor`](../../../src/WebAuthn/src/Extensions/Processing/Processors/LargeBlobProcessor.cs),
  [`Validation`](../../../src/WebAuthn/src/Client/WebAuthnClient.Validation.cs).
- **Extension outputs.** An unavailable large-blob read keeps the result present without a blob;
  that is distinct from an empty blob. Python can fail on storage or unsupported routing; Rust
  keeps the result on storage error but omits it if the key is missing; the reviewed Android path
  can omit output. An unsupported write route or missing/invalid returned key yields `Written = false`
  without a storage attempt; a present zero-byte write is valid. `false` is not general proof that
  storage was unchanged. More generally, optional registration output may be omitted rather than
  failing the ceremony after creation, unlike Python's whole-ceremony failure for the reviewed
  output error (Android/Rust have omission precedent). WA-IMP-002/015/016, AG-19/20;
  [`LargeBlobProcessor`](../../../src/WebAuthn/src/Extensions/Processing/Processors/LargeBlobProcessor.cs),
  [`RegistrationExtensionSession`](../../../src/WebAuthn/src/Extensions/Processing/RegistrationExtensionSession.cs).
- **Client data and timing.** The SDK uses L3 CCDToString escaping instead of python-fido2's JSON
  defaults or pinned Rust's unescaped construction; Android takes app-provided JSON. An explicit
  timeout above 600 seconds is shortened; omission starts no SDK timer. Cancellation is cooperative
  and a success may still win, as in Python's timer race, but Python does not apply that cap;
  Android and the reviewed Rust client do not apply a client deadline. WA-IMP-003/005, AG-19/20;
  [`WebAuthnClientData`](../../../src/WebAuthn/src/Client/WebAuthnClientData.cs),
  [`CeremonyDeadline`](../../../src/WebAuthn/src/Client/CeremonyDeadline.cs).

## Shared SDK policy and caller impact

- **Sessions (X1/X2, AG-22/23/29/31).** Serialize and fully await each operation or ceremony before
  starting another on the same session or disposing it. The existing logical-exchange guard is not
  an SDK-wide operation guard, a workflow lease, or device-wide exclusion. A rejected login can be
  retried on the same session; earlier success for that credential is cleared, not an independent
  credential's success. A failed SCP handshake is terminal. After confirmed reset, keep the session
  only if refresh succeeds; reuse the borrowed SCP inputs when applicable, except Security Domain
  uses restored defaults. See the [session contract](../../architecture/connection-ownership-and-contention.md#protocol-exchange-overlap).
- **Firmware (X8, AG-14 and Q6).** Prefer cached device firmware where wired, then applet metadata
  where available. Unknown, major-zero or identified non-final firmware can admit version gates
  optimistically; it does not prove feature support. A missing qualifier alone does not turn a known
  version into a prerelease. Required exchanges such as CTAP GetInfo or HSM SELECT still have to
  succeed. Numeric transport framing and legacy format selection are separate from feature admission;
  explicit override scope is applet-specific (on CTAP, framing and SCP eligibility, not the USB
  SmartCard gate). Python can treat `0.x` as old for SET_MODE, and Python/Rust map legacy OpenPGP
  `GET VERSION` `6985` to `1.0.0`; .NET's unavailable-advisory-version policy may instead let the
  command reach the card. CTAP-IMP-002, MGMT-IMP-008/012, OTP-IMP-006, PGP-CORE-003, CORE-HSM-X004;
  [`Feature`](../../../src/Core/src/Devices/Feature.cs), [#790][ctap], [#791][mgmt], [#793][otp], [#794][hsm], [#796][pgp].
- **ROCA (X13, AG-20/31).** PIV and OpenPGP both refuse on-card RSA generation in `[4.2.0, 4.3.5)`.
  This matches pinned Python in both and the pinned Rust OpenPGP guard; it starts earlier than the
  Android-aligned `[4.2.6, 4.3.5)` precedent. The extra versions are conservatively excluded, not newly
  known affected. Imports and existing key use are not blocked by this check. PIVIMP-003/PGP-IMP-011;
  [`PIV feature policy`](../../../src/Piv/src/Features.cs),
  [`OpenPGP generation`](../../../src/OpenPgp/src/OpenPgpSession.Keys.cs), [#795][piv], [#796][pgp].
- **Secret memory (R1, AG-24–27).** OATH Calculate, OTP HMAC and OpenPGP decipher return writable
  caller-owned memory rather than Python's immutable bytes. Sensitive CTAP/WebAuthn result buffers,
  including PRF and large-blob result fields, are writable too; result records are not disposable.
  Clear the buffers you receive when finished. The SDK clears sensitive storage it still owns; no new
  applet result `Clear()` was added, and existing `ApduResponse.Clear()` remains. Secret *holders*
  such as `SessionKeys` and `CredentialPassword` are disposable. The latter mirrors Python's text/raw
  split and Rust's factories: UTF-8 input up to 16 encoded bytes is zero-padded; raw input must be
  exactly 16 bytes. The clearable input and disposable owned copy are .NET API choices, not different
  credential bytes. CTAP-IMP-027, WA-IMP-013, X12/AG-23; [#789][oath], [#790][ctap], [#793][otp],
  [#794][hsm], [#796][pgp], [#798][wa]; [`CredentialPassword`](../../../src/YubiHsm/src/CredentialPassword.cs).
- **Explicit replacement (SD-IMP-006, AG-23/30).** All three Security Domain PUT KEY overloads and
  GENERATE require [`KeyReplacement`](../../../src/SecurityDomain/src/KeyReplacement.cs): `New`
  (also the default struct value), or `Of(0x01–0xFE)`. Unlike the peers' numeric APIs, the call must
  express intent and cannot select reserved existing versions. The existing version in P1 and the
  target KVN in command data can differ, as in Python's wire contract. This is not a new target-KVN
  restriction. OATH/PIV/OpenPGP/HSM import or PUT has no equivalent selector; document overwrite
  rather than imply an atomic host-side check. [#792][sd].
- **Experimental input (X11, AG-11/32).** previewSign and ARKG require compiler opt-in `YKEXP001`,
  not just the peers' draft warnings. A present empty previewSign handle is now accepted and sent as
  an empty byte string for the selected credential, aligning with python-fido2/Rust and the reviewed
  Android input. It is not an omitted argument or a default key. .NET still requires nonempty TBS and
  a complete map for present optional args: python-fido2 omits empty args, Rust forwards them, and
  .NET rejects them. CTAP-IMP-040/WA-IMP-019; [CTAP codec](../../../src/Ctap2/src/Extensions/PreviewSign/PreviewSignCbor.cs),
  [#790][ctap], [#798][wa].

## Tools and examples, not wire compatibility

S1 (AG-21/22) prioritizes readability in demo tools: reuse existing prompts and cleanup, label demos
as unsuitable for production, and do not add production-grade secret lifecycle machinery to samples.
It does not change production CLI or SDK responsibilities. HsmAuthTool masks passwords and keys but
retains demo-labelled key output without a diagnostics flag (HSM-IMP-005, [#794][hsm]); Python's CLI
does not offer the same CALCULATE/demo output. PivTool signs the message and offers RSA PKCS#1 v1.5,
not Python's additional padding options (PIVIMP-005, AG-10, [#795][piv];
[`Signing`](../../../src/Piv/examples/PivTool/PivExamples/Signing.cs)). ManagementTool selects SmartCard
only and uses the SDK's masked hex reader, unlike Python's transport selection and string prompts
(MGMT-IMP-013/015/016, [#791][mgmt]). OtpTool retains a supplied write code by default, prefers HID
for explicit hex C/R, and does not implement Python's full enrollment/TOTP workflow (OTP-IMP-007/010,
[#793][otp]). OpenPGP attest follows Python's ordinary User VERIFY, but .NET prints certificate output
and states the on-card replacement rather than applying Python's file-output checks (PGP-IMP-015, [#796][pgp]).

FidoTool requires an explicit serial for noninteractive targeting and scopes its Ctrl+C handler.
Keeping the chosen device on reset follows the sibling CLI precedent; .NET also verifies the live
serial on the connection used for reset. Only a conclusively missing original macOS HID entry can
trigger rediscovery, on the same transport with exactly one known-serial match. No serial-less
retargeting: GetInfo fingerprinting of a retained device is not unique identity. These are target and
lifetime safeguards, not a different reset command. CTAP-IMP-046–048, AG-31;
[`ResetAuthenticator`](../../../src/Ctap2/examples/FidoTool/FidoExamples/ResetAuthenticator.cs), [#790][ctap].

## Scope still open

[AG-33][ag33] sets sibling quality, correctness and UX as the floor: stay compatible by default and
give a concrete reason for a deliberate departure. NEO, other pre-5 keys and experimental paths can
need explicit human adjudication; this is not a blanket exemption or an automatic fourth review round.
An upgrade from .NET V1 should preserve existing data formats and provisioning where possible, not
force a reset solely for upgrading. This is not a V1 binary/source compatibility promise or global
certification. The relevant V1 precedents are the [ADMIN timestamp codec][v1-admin],
[NDEF null/case handling][v1-ndef] and the reviewed PIV certificate formats, not all devices or features.
The two linked V1 files are from the repository's `develop` at `731225e0f17`, not an assumption about `main`.

The ledger accounts for 178 original cards plus the `CORE-HSM-X005` fold into HSM-IMP-008.
`CTAP-IMP-030` (unsigned assertion output/raw envelope) and `PGP-IMP-009` (partial key mutation)
are explicit deferrals, not fixes delivered here. `HSM-IMP-002` was dropped under S1. Nineteen
qualification items remain open or partial, including Q10's three
compile-only PIV cases. The [hardware notes](README.md) distinguish the bounded metadata, Selection,
OTP HID HMAC and PIV signing witnesses from broader firmware, transport, built-in UV and provisioning
qualification. In particular, Selection did not exercise built-in UV. The SCP03 protected-empty path
retains Python's padding pending qualification against the relevant SCP03 edition; do not infer an
SCP11 rule from it (CORE-SD-X007, [#792][sd]). Core Pass 2 and the aggregate merge decision remain separate.

[base]: https://github.com/Yubico/Yubico.NET.SDK/pull/788
[oath]: https://github.com/Yubico/Yubico.NET.SDK/pull/789
[ctap]: https://github.com/Yubico/Yubico.NET.SDK/pull/790
[mgmt]: https://github.com/Yubico/Yubico.NET.SDK/pull/791
[sd]: https://github.com/Yubico/Yubico.NET.SDK/pull/792
[otp]: https://github.com/Yubico/Yubico.NET.SDK/pull/793
[hsm]: https://github.com/Yubico/Yubico.NET.SDK/pull/794
[piv]: https://github.com/Yubico/Yubico.NET.SDK/pull/795
[pgp]: https://github.com/Yubico/Yubico.NET.SDK/pull/796
[wa]: https://github.com/Yubico/Yubico.NET.SDK/pull/798
[otp-codes]: https://github.com/Yubico/Yubico.NET.SDK/pull/793#discussion_r4649563220
[pgp-neo]: https://github.com/Yubico/Yubico.NET.SDK/pull/796#discussion_r4652106464
[wa-review]: https://github.com/Yubico/Yubico.NET.SDK/pull/798#issuecomment-7066886361
[ledger]: pass-2-review-ledger.md
[decisions]: pass-2-review-decisions.md
[ag33]: pass-2-review-decisions.md#ag-33-review-adjudication-and-compatibility
[v1-admin]: https://github.com/Yubico/Yubico.NET.SDK/blob/731225e0f1747f9d54e621409e24017d14503ba5/Yubico.YubiKey/src/Yubico/YubiKey/Piv/Objects/AdminData.cs
[v1-ndef]: https://github.com/Yubico/Yubico.NET.SDK/blob/731225e0f1747f9d54e621409e24017d14503ba5/Yubico.YubiKey/src/Yubico/YubiKey/Otp/NdefConfig.cs
