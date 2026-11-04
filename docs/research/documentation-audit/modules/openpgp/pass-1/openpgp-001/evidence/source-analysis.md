# Original citations and firmware provenance

Access date: **2026-11-04**. Originals are retained beside this file; `source-manifest.json` identifies
their URL/revision/hash. Text line references below refer to the cached extraction, not PDF page numbers.
Read the passage and its conditions before copying a citation into active documentation.

## Card specification

**Functional Specification of the OpenPGP application on ISO Smart Card Operating Systems**, Achim
Pietig, **Version 3.4.1, March 18, 2020**, application functional specification. Original URL:
<https://gnupg.org/ftp/specs/OpenPGP-smart-card-application-3.4.pdf>. The URL says3.4; title page and
page footers identify3.4.1. No IETF standards-track designation applies to this document. The actual
paper's requirements use shall/should/may in context; keep their strength and actor intact.

| Section / PDF page | Exact consequential content and strength | Actor / applicability | Candidates |
|---|---|---|---|
|4.3 /17|Correctly verified PW access remains valid until reset, another DF selection, or specific internal command reset; signature behavior depends on C4.|Card access state; PW1/PW3, distinct from host object lifetime.|C001,C018,C019|
|4.3.1 /18|UTF-8 default format; assumed unpersonalized/default PIN values; recommends changing them.|Cardholder/terminal and card profile; a default example is not secure input acquisition.|C017,C018|
|4.3.2 /18–20|KDF is optional, announced in Extended Capabilities. With valid KDF content, passwords/resetting codes **shall** have derived content; terminal aligns all PWs/F9. KDF content is described as transparent to card commands.|Terminal software handles derivation/alignment; card exposes F9 when advertised. It does not specify the YubiKey provisioning transition as a universal card operation.|C001–C003|
|4.3.2 /19|Tags84/85/86 are User/Reset/Admin salts;87/88 initial User/Admin hashes; iteration count has four-byte field in example/table.|Terminal F9 encoding/interpretation. Distinguish credential identity from VERIFY P2.|C001,C002|
|4.4.1 /22–25 and4.4.2 /26–28|C4 lengths/format and error counters; C5/C6 fingerprint fields and CD dates. Authentication certificate occurrence and proprietary tags are distinguished.|Data objects; avoid assuming every enum tag is directly readable/writable or universally present.|C010,C018|
|4.4.3.4 /30|Language is1–4 byte pairs (2–8 bytes), lowercase ISO639-1; first highest priority; at least one entry **should** be present.|Terminal/cardholder data representation.|C018|
|4.4.3.6 /31|Optional UIF evaluated by card. Permanently-enabled flag may be supported; ordinary-profile text names factory reset. Abort/timeout **should** return6600.|Card optional feature;6600 is guidance, not proof this SDK recognizes real-device touch timeout. Vendor ATT extension needs separate recovery caveat.|C004,C010,C012|
|4.4.3.7 /32–33|Advertised optional GetChallenge/key import/KDF/changeable attrs; challenge/special-DO/certificate max lengths.|Card advertises; terminal consumes. SDK's limited enforcement is a separate implementation policy.|C010|
|4.4.3.8 /33–34|Key Information reports none/generated/imported; at least major three keys. Integrity warning requires protected command/response or alternate protection such as attestation.|Card status reporting; plaintext status alone proves no provenance/security property.|C012,C016,C018|
|4.4.3.9 /34–35|Terminal **shall** use attrs for import; card **should** reject unsupported values; mismatching changed attrs **should** delete key or prevent usage. RSA format02=CRT,03=CRT+modulus.|Terminal encoder/card policy; no rollback/atomicity guarantee.|C009,C014|
|4.4.3.11 /37|FA **shall** be present if attrs changeable; each key may have many entries; proprietary additional keys optional.|Card list encoding; preserves multiplicity, not dictionary-by-slot uniqueness.|Clean algorithm-list contract|
|4.4.3.12 /38–39|Import uses4D containing CRT,7F48 tag+length descriptors,5F48 concatenated values; order mandatory. EC private key big-endian and field-size padded. Signature-key import resets counter to zero.|Terminal encoding/card side effect. RFC7748 little-endian host scalar needs card-profile reversal.|C013,C014,C018|
|7.2.2 /52–53|P2=81 and82 both PW1;81 signature-only,82 other functions. P1=FF requires empty data and resets addressed access state.|Card VERIFY; credential remains User PIN for both references. Reset Code is a separate DO used in RESET RETRY COUNTER.|C001,C019|
|7.2.3 /55|Current PW + new PW; old length known by card; PW81/83. Access unrestricted but current PW verified.|Terminal concatenates credentials in correct format; card verifies.|C002,C019|
|7.2.4 /56|P1=02 after PW3 or P1=00 Reset Code+newPW; resets PW1 value/retry counter.|Card reset operation and terminal precondition.|C001,C017|
|7.2.5 /57–58|SELECT DATA selects current occurrence; subsequent GET/PUT accesses it; P1=0..2;60{5C taglist}.|Card selection/terminal command composition. Vendor pre5.4.4 length-prefix workaround is separate.|C010,C012|
|7.2.10 /63–66|Card consumes hash(ECDSA) or DigestInfo(RSA), needs PW1/81; card applies RSA00/01/FF/00 padding. Hash table listsSHA256/384/512; may not check DSI correctness.|Terminal prehash/DigestInfo versus card private-key operation. SDK SHA1 support is implementation support, not recommended algorithm selection.|C006|
|7.2.11 /67–69|RSA00 indicator plus encrypted PKCS#1 block; card checks padding and returns message. ECDH takesA6{7F49{86 external point}}, returns shared secret; other decipher calculations outside card.|Host packet/content processing versus card key operation. General EC point04||X||Y; X25519 vendor encoding separate.|C007|
|7.2.14 /74–75|Generate needsPW3; read public key always possible; response public-key template. Terminal calculates/stores fingerprint, device leaves it unset; signature generation resets counter.|Card and terminal responsibilities. Generate/import success does not construct an OpenPGP identity/file automatically.|C009,C017,C018|
|7.2.16–17 /77–78|Optional terminate/activate governed by life-cycle state; terminate access after admin or blocked admin conditions; activate restores profile defaults in appropriate state.|Card application lifecycle; vendor ATT persistence overrides assumptions about proprietary material.|C004,C005|
|7.9 /90|63Cx retry count;6982 includes PWwrong, PWunchecked, SMincorrect;6983 PWblocked counterzero;6A82 file/application missing;6A88 referenceddata/DO missing;6600 reservedUIF.|Card status meanings; contextual interpretation, not one universal wrong-PIN signal.|C005,C019; certificate absence current contract clean|

## Applicable RFCs — deliberately scoped

- **RFC9580, OpenPGP**, July2024, IETF **Standards Track** (obsoletes4880/5581/6637),
  <https://www.rfc-editor.org/rfc/rfc9580.txt>, §3.7.1.3. The S2K algorithm says octet count,
  repeated salt+passphrase, at least one complete copy regardless of count. Actor: implementer of
  Iterated-and-Salted S2K; this SDK's host PIN derivation uses that primitive. RFC packet S2K encodes
  count in **one coded octet**, while Card F9 stores the expanded count in four bytes. Do not copy RFC
  packet layout into F9, upgrade card key IDs/fingerprints/packet support, or introduceArgon2 merely
  because this newer RFC has it. Existing .NET minimum-input fix agrees with this passage; pinned
  Python `_do_process` still lacks that minimum, an intentional recorded divergence.
- **RFC8017, PKCS #1: RSA Cryptography Specifications Version2.2**, November2016, IETF
  **Informational**, <https://www.rfc-editor.org/rfc/rfc8017.txt>, §9.2 steps1–2 and notes give
  hash/DigestInfo DER; steps4–5 form fullEM. Actor: RSA signature encoder. Here the host suppliesT,
  card constructsEM. §7.2 specifiesRSAES-PKCS1-v1_5, relevant to raw encrypted-block decryption;
  its presence does not make arbitrary.gpg files orOAEP accepted by this card API.
- **RFC8032, Edwards-Curve Digital Signature Algorithm (EdDSA)**, January2017, IRTF
  **Informational**, <https://www.rfc-editor.org/rfc/rfc8032.txt>, §5.1.6 signing procedure/64-byte
  signature; §7.1 test1 has the exact seed/public-key pair used by .NET import tests. Actor: Ed25519
  implementation; seed bytes are not the expanded/pruned secret scalar. Host APIs return/send
  raw message for ordinary Ed25519, rather than selectingEd25519ph through the unused hash parameter.
  Test public-key equality supports import encoding; it is separate from verifying a signature.
- **RFC7748, Elliptic Curves for Security**, January2016, IRTF **Informational**,
  <https://www.rfc-editor.org/rfc/rfc7748.txt>, §5 encoding (little-endian), §6.1 Alice/Bob vectors and
  raw shared secret. Actor: X25519 implementation; Card import's big-endian scalar requirement is a
  profile conversion, not a change to RFC7748. RFC encoding alone defines no APDU wrapper. Pinned
  Python encrypt/decrypt tests corroborate the A6 wrapper and agreement; .NET's vector import tests
  establish public-key equality only. Host applications also own shared-secret KDF/protocol validation.

## Original vendor extension and behavior

1. **OpenPGP Attestation**, <https://developers.yubico.com/PGP/Attestation.html>, sectionsImplementation,
   ProtocolSpecification/AttestationKey/GenerateAttestation. Undated live vendor extension, not an IETF
   standard. Defines ATT CRT **B6 03 84 01 81**, FB P1=1/2/3 and no response data; device writes7F21
   indexedSig2/Dec1/Aut0. Generated-only target, ATT key algorithm excludescurve25519. ATT key and
   certificate survive reset; replacing factory material loses manufacturer trust irreversibly.
   Verifier validates chain/public key and treats mutable name/fingerprint/date fields accordingly.
   Current CA transition wording (5.7.4/new2025 root) belongs to verifier trust-store choice, not a
   hard-codedSDK guarantee. .NET currently returns a decoded certificate without validating that chain.
2. **OpenPGP Commands**, <https://docs.yubico.com/software/yubikey/tools/ykman/OpenPGP_Commands.html>,
   reset/set-touch/import: ATT key/cert survive; fixedATT policy recovery requires key replacement;
   cached touch duration15s; ordinary fixed policy clears with associated key deletion/replacement.
   This is vendorCLI guidance, not proof that this .NET sample implements every option.
3. **Card edit**, <https://developers.yubico.com/PGP/Card_edit.html>, YubiKeytouch: ordinary fixedUIF
   can be cleared by generating/importing a new key; touch timeout15s; NEO app-version differs from
   physical firmware whereas4/5 app-version equals firmware. Historical setup recipes are not current
   SDK setup commands. This source also warns about old NEO security; basic fallback1.0.0 is not a
   recommendation to deploy that version.
4. **OpenPGP Specifics — YubiKey Technical Manual**,
   <https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-openpgp.html>: public commercial
   feature description names5.2.3 forCard3.4/ECC, listsX25519DEC andEd25519SIG/AUT. Historical Python
   SDK5.2.0/5.2.1 gates are precise implementation provenance but not sufficient to resolve commercial
   introduction boundaries. Publish SDK thresholds with that qualification; old-firmware qualification
   remains a bounded gap.
5. **Security advisoryYSA-2017-01 — Infineon weak RSA key generation**, published2017-10-16,
   <https://www.yubico.com/support/security-advisories/ysa-2017-01/>. Applies on-chipRSA generation;
   corrected4.3.5+ implementation; affected OpenPGP deployed range4.2.6–4.3.4; external import is a
   mitigation. Do not extend the advisory to deny importedRSA or label4.3.5 affected. Python's broader
   `[4.2.0,4.3.5)` guard is its conservative implementation range, not the advisory's exact wording.

## Pinned Python history — salient gate origins

Revision for present source/tests: **4ca60f706af930459138d8dc0f0f953480e1c7a4**. Commit URLs follow
`https://github.com/Yubico/yubikey-manager/commit/<full-hash>`; local `.patch` files retain exact
messages/diffs. These trace source history, rather than device firmware release authority.

| Gate/behavior | Exact historical evidence | Interpretation |
|---|---|---|
| Reset 1.0.6 / UIF 4.2 / NEO retries 1.0.7 / 4.x retries 4.3.1 | 0611c18f40122ad115829e1f59920bf69d1062db, 2016-12-06, “Better error handling.” | Moves/adds checks into controller; not a firmware changelog. 1.0.x is the NEO applet version. |
| Cached UIF 5.2.1 / ATT UIF 5.2.1 | 1e44cf75fc92368bfaadf9500aab046d0d972bfe, 2019-06-17; c883b926a9ded48cb2dc8d6116d388124b537c81, same date | Exact capability split; later Python source retains separate SetUif gates. |
| EC / certificates / attestation 5.2.0 | 6870378ce2c4138a79598425182dfc70a247299a, 2021-02-03, “Version checking for openpgp.” | Explicit checks added; retain the 5.2.3 manual qualification above. |
| SELECT DATA corrected after 5.4.3 | 0ba0ad0e3fd23059367415b7e7c116cb3c05a175, 2021-04-08, “Prefix length byte in APDU when needed (see #403).” | Exact legacy length-06 prefix; .NET threshold 5.4.4 is equivalent for stable versions. |
| Algorithm-info Curve25519 fix 5.6.1 | 69b535096f60ae34e9a0d828e0239c8afe332000 introduces the fix; 4a2d6abba44ffbb8d0a33639765beffc9506f475, authored 2023-01-08, limits it below 5.6.1 | Removes X25519/EdDSA, adds DEC X25519/ECDH if absent, removes Ed25519 DEC/ATT. .NET's replacement-only strategy can differ in multiplicity or missing-entry cases; retain a bounded unit-regression follow-up. |
| Unverify 5.6.0 | 9654350803d6fcce2e6a0c432a063989fbce0922, 2023-01-09 | Source check; generic P1=FF in Card §7.2.2 establishes no YubiKey minimum firmware. |
| ROCA generation guard | 9c0a0999d615859dfb195aac112775e266e48e10, 2020-11-11, “Various cleanups.” | Adds `[4.2.0,4.3.5)` check; original advisory establishes rationale and fixed boundary. |
| NEO CRT with modulus: import and generation | ab83e442496341e2634f87c88f1299e6705b96f5, 2019-08-13; 4a2d6abba44ffbb8d0a33639765beffc9506f475; fe09b795f1ad858dc5770591e75adfee0fb61345, 2024-03-24 | Format 03 with N; last commit explicitly fixes NEO generation format. |
| NEO delete fallback | 4a2d6abba44ffbb8d0a33639765beffc9506f475 | Overwrites with a random RSA2048 key rather than attribute-switch deletion; a distinct semantic compatibility concern. |
| NEO error-status fallback | 31459ea393e1e3d603dea99152fbfbd2917f5280, 2025-11-12 | Current source has additional pre-4.0 status handling. A 6982-only fallback establishes no all-firmware qualification. |

### Research stopping point

Initial source/spec/vendor acquisition plus one focused history/original-advisory follow-up is complete.
Unresolved: commercial 5.2.x boundaries versus development gates; historical NEO behavior; tolerance
of format 02 with N; behavior of hypothetical future unknown model fields; precise low-count GnuPG
provenance claimed by older audit tests (current test vectors retained, experiment not rerun); generic
Card KDF alignment versus YubiKey transition on each family. Record these gaps, ask for a product decision
where consequential, and qualify new hardware evidence to its actual key/firmware/transport.
