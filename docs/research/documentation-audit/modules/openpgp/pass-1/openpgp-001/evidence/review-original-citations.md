# Independent original-citation checks

Fresh GPT-6.1 Sol reviewer, 2026-11-04. This record identifies passages actually read independently of
the investigator's conclusions. Live original retrievals matched the retained originals below. No
firmware/device operation or Core/other-module investigation was performed.

## Original identities

Downloaded the original Card PDF, all four RFC texts and pinned Python OpenPGP library/CLI files with
read-only HTTP requests. All seven live byte counts and SHA256 hashes match `source-manifest.json`
and retained files:

| Original | Bytes | SHA256 |
|---|---:|---|
| [Card PDF](https://gnupg.org/ftp/specs/OpenPGP-smart-card-application-3.4.pdf) | 1156819 | `2d2652a2bd690a6784c733ba6d88539e2af1f6e075f3a3d0af81eb5f68432782` |
| [RFC9580](https://www.rfc-editor.org/rfc/rfc9580.txt) | 404977 | `36540e57b63d24c0b17be0748b704bdeed3d643ce99a4fb1abb6469c1eb1ff18` |
| [RFC8017](https://www.rfc-editor.org/rfc/rfc8017.txt) | 154696 | `1e72dc473d18df3fc5598cdc12795a9f18f36f1aef15abc23a55eb0d58151d11` |
| [RFC8032](https://www.rfc-editor.org/rfc/rfc8032.txt) | 103210 | `ed63657ff389301282b169b0abde9b5dd2c7e4d524fdfa5da6ff3094fc93c4c3` |
| [RFC7748](https://www.rfc-editor.org/rfc/rfc7748.txt) | 39298 | `279ca0ecc5e92e2962e27b846986aeb74729d9dd34bd4a04a362f80dcb596ad3` |
| Pinned `yubikit/openpgp.py` | 60103 | `bcd266e53411a9611ee133ed5f33e2907294fbe374aa56a19672a151e86a2926` |
| Pinned `ykman/_cli/openpgp.py` | 20757 | `074cf73b1d91b2bbf3d8a495fc3ff947e100f88db5c9647dc938f0a608b5151b` |

Python revision is `4ca60f706af930459138d8dc0f0f953480e1c7a4` in
<https://github.com/Yubico/yubikey-manager>. It is a pinned reference, separate from installed vendor
source and this run's version policy. Re-read retained original commit diffs rather than relying on
the source-analysis history table. Live vendor pages listed below were also fetched and read.

## Card specification — actual revision, actor and strength

Title page: **Functional Specification of the OpenPGP application on ISO Smart Card Operating
Systems**, Achim Pietig, **Version 3.4.1**, ©2020 March 18th. The filename says 3.4. This is the Card
functional specification, not an IETF standards-track document. Printed page numbers below differ
from extracted-text lines; line ranges identify the retained original extraction.

| Clause / printed page / extraction | Actual passage and bounded interpretation |
|---|---|
| §4.3 /17 /647–672 | Correctly verified PW access remains valid until reset, another DF selection or specific internal command reset. C4 selects one-command versus retained signature state. Actor: card access state; not a host-object-lifetime guarantee. Supports REV-002. |
| §§4.3.1–2 /18–20 /678–770 | UTF-8 is default; default PW1/PW3 are assumed for unpersonalized cards, and changing them is highly recommended. KDF is optional/advertised; valid F9 means PWs/Reset Code **shall** be derived. Terminal software aligns credentials/F9; commands do not evaluate F9 in the generic profile. Tags 84/85/86 identify User/Reset/Admin salts, 87/88 initial hashes, 83 is four bytes. YubiKey transition behavior needs vendor/device evidence separately. |
| §4.4.1 /24 /927–930 | CD has four big-endian generation-time bytes per ordinary slot; values **shall** be seconds since Jan1,1970; default00000000 means **not specified**. Current signed-int representation is a C# implementation limitation, not permission to reinterpret high-bit card time as a negative epoch. Independently followed up after the coordinator's expanded sample; supports REV-005/C022. |
| §4.4.3.4 /30 /1201–1205 | One to four ISO639-1 lowercase byte pairs; at least one entry **should** be present, first has highest priority. Supports 2–8 byte description without upgrading should to shall. |
| §4.4.3.6 /31 /1224–1237 | Optional UIF/card evaluation; permanent enabling may be supported. Generic recovery names reset. Abort/timeout **should** return 6600. Neither an SDK TimedOut classification nor a causal explanation for a particular request follows from should. ATT is a separate vendor extension. |
| §§4.4.3.7–9 /32–35 /1243–1368 | Capability flags advertise optional support; limits include challenge/certificate/special-DO lengths. Terminal **shall** use attrs for import. Card **should** reject unsupported values and delete/prevent use when changed attrs mismatch a stored key. Formats 02/03 distinguish CRT from CRT+modulus. No transactional rollback guarantee. |
| §4.4.3.8 /33–34 /1296–1324 | Key information describes generated/imported/absent; card responses can be compromised on transport. Protected exchange or another mechanism such as attestation supplies integrity. Public key-status/fingerprint-length alone proves no provenance. |
| §4.4.3.11 /37 /1426–1450 | FA **shall** be present when attributes are changeable; each key can have many entries, additional proprietary keys optional. Ordered multiplicity is meaningful. |
| §4.4.3.12 /38–39 | 4D contains CRT, 7F48 descriptors, 5F48 concatenated values in required order. Card-profile conventional EC private material is big-endian/field-sized; signature-key import resets the counter. Ed25519 seed and X25519 byte conversion need algorithm/vendor context. |
| §7.2.2 /52–53 /2016–2059 | P2=81 and82 both mean PW1; signature mode versus other-function access state. P1=FF requires empty data and resets addressed access state. Supports logical User identity for both KDF/retry lookups, not Reset Code identity. |
| §§7.2.3–4 /55–56 /2093–2176 | Change checks current credential; reset uses prior PW3 with P1=02 or Reset Code with P1=00. User retry/value reset are card effects. Host constructs concatenated credentials with correct derivation/format. |
| §7.2.10 /63–66 /2433–2561 | Host gives ECDSA hash or RSA DigestInfo. Card constructs 00/01/FF/00 padded RSA DSI; PW1/81 authorization and C4 consumption. SHA256/384/512 are the listed recommended input hashes. SDK SHA1 acceptance is separate implementation support. |
| §7.2.11 /67–69 /2567–2662 | PW1/82; RSA indicator plus encrypted PKCS#1 block, card padding checks/removal. ECDH takes A6{7F49{86 external point}}, returns shared secret; other calculations are outside the card. Ordinary uncompressed point is 04||X||Y. This is no complete .gpg/OAEP contract and supplies no RFC7748 APDU format by itself. |
| §7.2.14 /74–75 /2837–2900 | Admin for generation, public read always possible. Host calculates/stores fingerprint. Signature generation resets counter; other related DOs **may** reset too. Separate certificate/metadata tasks do not promise their old bytes survive every mutation. |
| §§7.2.16–17 /77–78 /2959–3032 | Lifecycle commands optional and state-dependent. TERMINATE access depends on Admin/SM or blocked/unavailable Admin conditions. ACTIVATE in operational state can return9000 as a dummy command changing nothing. Supports attempted-reset wording and independent postcondition, with ATT exception separately sourced. |
| §7.9 /90 /3292–3331 | 63Cx retries;6982 includes PWwrong, PWunchecked and incorrect SM;6983 means blocked/zero;6A82=file/application missing;6A88=referenced data/DO missing;6600 reserved UIF. Supports broad-error qualification and REV-001/003. |

## RFCs — primitive/encoding scope only

- **RFC9580, OpenPGP**, July2024, IETF Standards Track, §3.7.1.3, lines713–749:
  packet S2K has a **coded one-octet count**, salt+passphrase is repeatedly fed into hash context(s),
  count means octets, at least one full input copy is hashed. Card PIN derivation's single selected
  hash digest uses that primitive; F9's expanded four-byte count is a different layout. This citation
  grants no new packet/fingerprint/Argon2/card-profile support.
- **RFC8017, PKCS #1: RSA Cryptography Specifications Version2.2**, November2016,
  Informational, §9.2, lines2490–2587: encoder hashes M, DER-encodes DigestInfo T, then builds
  full EM with FF padding. Card §7.2.10 divides this work: SDK supplies T, card constructs EM.
  It is incorrect to call the host-built DigestInfo a fully padded RSA block.
- **RFC8032, Edwards-Curve Digital Signature Algorithm (EdDSA)**, January2017, IRTF
  Informational, §§5.1.5–6/7.1, lines679–743 and1295–1320: 32 random private input bytes are hashed/
  pruned into an internal scalar; test1 matches the imported seed/public-key bytes. Ordinary Ed25519
  accepts M and emits32-byte R plus32-byte S. SDK's ignored hash argument selects no Ed25519ph mode.
  A public-key equality test remains import evidence, distinct from a host-verified signature.
- **RFC7748, Elliptic Curves for Security**, January2016, IRTF Informational, §§5/6.1,
  lines345–417/737–777: X25519 scalar/u-coordinate native encoding is little-endian; exact Alice
  private/public/shared-secret vectors agree with test bytes. Scalar reversal is a Card-profile import
  conversion; APDU wrapper comes from Card/vendor usage, not this RFC. Shared-secret KDF/protocol
  validation remains a host responsibility. Late scope-specific evidence records SDK6A80 versus
  a successful same-key installed-vendor shared-secret comparison; see review.md's hardware reconciliation.

## Live vendor originals

- [OpenPGP Attestation](https://developers.yubico.com/PGP/Attestation.html), Implementation,
  Protocol Specification / Attestation Key / Generate Attestation: generated-only Sig/Dec/Aut targets;
  overwrite of7F21; ATT CRT **B6 03 84 01 81**; FB has no response data and needs subsequent retrieval.
  ATT key/certificate survive reset; replacing factory material loses manufacturer provenance.
  Verifier checks chain/public key and distrusts mutable name/fingerprint/date fields. New root CA
  for5.7.4+ is verifier trust-store context, not SDK automatic trust. SDK source confirms no chain validation.
- [OpenPGP Commands](https://docs.yubico.com/software/yubikey/tools/ykman/OpenPGP_Commands.html),
  set-touch/import/reset: fixed ATT policy recovery requires ATT private-key replacement, ordinary
  fixed policy requires associated-key deletion, reset preserves ATT key/certificate. Cached touch is15s.
  VendorCLI help is not evidence that the C# sample honors its own unused option.
- [Card edit](https://developers.yubico.com/PGP/Card_edit.html), YubiKey touch: ordinary fixed recovery
  by generating/importing a new key; wait up to15s then operation fails. Distinguishes NEO applet from
  physical firmware history. Historical setup instructions are not current SDK commands.
- [OpenPGP Specifics, Technical Manual](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-openpgp.html):
  commercial5.2.3+ Card3.4/ECC language, X25519 decipher andEd25519 sign/auth. This differs in provenance
  from5.2.0/5.2.1 development-source gates. The active guide appropriately publishes SDK checks and
  capability qualification rather than deciding that unresolved historical introduction boundary.
- [YSA-2017-01](https://www.yubico.com/support/security-advisories/ysa-2017-01/), body publication
  **2017-10-16** (search metadata2020 is not the advisory date), Solution/Mitigation: corrected on-chip
  generation4.3.5+, affected deployed OpenPGP4.2.6–4.3.4, external generation/import is mitigation.
  Python conservative `[4.2.0,4.3.5)` and unchanged C# test `<=4.3.5` are different implementation ranges.

## Python and firmware history — bounded corroboration

Read full relevant pinned flows (`_read_version`, reset, retries, KDF set/verify, UIF, generation/import/
delete, certificate selection/CRUD, attestation, sign and decipher). `_verify(PW.USER,...,mode)` keeps
credential identity separate from P2. `set_kdf` states initial User/Admin transition and Reset Code
invalidation. All EC decipher inputs get A6 wrapper, including raw X25519. Ordinary CLI attestation
verification remains source evidence of that route, not a universal exclusivity proof.

Re-read the retained original diffs:0611c18f4012 (reset/UIF/retry checks),1e44cf75fc92/c883b926a9de
(cached/ATT5.2.1),6870378ce2c4 (5.2.0 checks),0ba0ad0e3fd2 (SELECT DATA prefix),69b535096f60/
4a2d6abba44f (algorithm workaround/bound),9654350803d6 (unverify),9c0a0999d615 (inline ROCA range,
**replacing an existing helper guard**, not proof the security guard first originated in2020),
ab83e4424963/11b6c36e85dc/4a2d6abba44f/fe09b795f1ad (NEO format/generation/deletion), and
31459ea393e1 (NEO error fallback). Full commit identities remain in source-analysis/patch headers.
No additional historical-origin claim is needed in product prose.

Initial research plus one focused follow-up already meets the campaign stopping bound. Retain exact
commercial5.2.x/NEO tolerance/development-version/low-count vector provenance gaps rather than launch
another open-ended search or imply original code is firmware-release authority. Four deferred Core
questions remain questions. No vendor tests were executed by this reviewer.

## Library API check

Context7 resolved .NET and queried current `MemoryMarshal.AsMemory(ReadOnlyMemory<T>)` plus memory
ownership guidance. It creates a mutable view; use only with the SDK outputs whose local implementation
hands their actual backing storage to the caller. This does not authorize mutation of borrowed/static/
model memory and does not establish snippet compilation. Complete snippet/compiler gates remain owned
by the coordinator/validation session.

## Bounded timestamp follow-up for expanded sampling

After the coordinator reported the GenerationTimes clean-unit miss, independently read Card §4.4.1
CD table and both full current parser/setter units, plus the collection/property/Get/Set/guide contracts.
Fetched original [RFC4880](https://www.rfc-editor.org/rfc/rfc4880.txt), **OpenPGP Message Format**,
November2007, IETF Standards Track; bytes203706, SHA256
`575dc55f0edea3cb490ad92941e83bd755f674c20619efee0a5a5b721b32bf02`.
Its §3.5 at lines520–523 states: “A time field is an unsigned four-octet number containing the number
of seconds elapsed since midnight, 1 January1970 UTC.” That original corroborates unsigned time
semantics within the Card's RFC4880-era profile. The Card's own CD clause supplies the special zero
sentinel; no packet-profile upgrade is inferred. This focused follow-up answers the exact reported
question and starts no broad new source/firmware research. Required documentation correction and
deferred representation policy are separated in REV-005/PGP-IMP-018.
