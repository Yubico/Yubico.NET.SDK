# Primary-source and history analysis

Investigator: fresh GPT-6.1 Sol. Research started 2026-11-04T23:58Z and continued across
2026-11-05 UTC. The session's campaign date is November 5. Capture timestamps come from the host
clock. `source-manifest.json` and `followup-manifest.json` record exact URLs, hashes and failures.
The initial source batch and one focused follow-up are complete. Further unresolved points below
are bounded gaps rather than permission to widen this pass.

## Specification authority and applicability

The downloaded `nist-sp800-73-4-part1.pdf` is the **combined three-part SP 800-73-4** publication,
despite the local capture filename. Its first cover identifies May 2015, updated February 8, 2016;
its archive notice records withdrawal July 15, 2024 and supersession by the three revision-5 parts.
The guessed separate `...73-4pt2.pdf` URL returned 404; the combined original supplies Part 2 in
full. The two CSRC landing pages returned 403 in follow-up; the PDFs' own withdrawal notices
establish publication status. Preserve revision 4 when explaining revision-4 claims. Revision 5
is a different profile requiring a separate compatibility decision.

| Original passage | Actor, strength and local conditions | Consequence for this review |
|---|---|---|
| SP 800-73-4 Part 2 §2.4.2, combined text 3792–3819 | The card sets an authenticable entity's security indicator TRUE on success and FALSE on aborted/failed authentication; PIV PIN, PUK and administration-key indicators are application-local. | Supports state-transition rationale; a managed boolean is an SDK observation, while the card owns security state. Handle disposal alone is a separate OS/transport issue. |
| Part 2 §2.4.3, text 3830–3856 | PIV Card Application PIN **shall** be 6–8 bytes, ASCII digits 30–39, padded on the right with FF; the card enforces minimum/format. PUK **shall** be 8 bytes and **may** contain any binary octet. | SDK raw-byte acceptance and YubiKey UTF-8 practice are distinct from this federal profile. Describe actual SDK validation without attributing unrestricted UTF-8 to the standard. |
| Part 2 §3.2.1, text 4202–4245 | Card VERIFY P1=00 with absent Lc/data can return 63CX or 9000. P1=FF resets security status without decrementing retries. Correct PIN resets the retry counter to issuer-dependent reset value. | Explain count-unavailable 9000 and 4-bit status counts; three is a factory default, not the general limit. Empty data differs from an eight-FF credential submission used for reset preparation. |
| Part 2 §§3.2.1–3.2.3 | Command access/interface requirements belong to the card profile; CHANGE REFERENCE DATA changes PIN/PUK; RESET RETRY COUNTER uses PUK. | Avoid universal NFC or Bio assertions from ordinary USB evidence. Yubico reset/SET RETRIES/Bio temporary PIN APDUs remain vendor extensions. |
| Part 1 §3.3.3 and Table 19, text 1120–1200, 2209–2229 | Up to 20 retired management keys; on-card/off-card counts refer to **retired keys**. F3 URL is required with off-card count>0, absent if both counts zero, optional otherwise; URL format is `http://DNS-name/ASCII-HEX-SHA256`. | `KeyHistory` exposes a narrower fixed four-TLV reader and a more permissive count/URI constructor. Public docs need actual-shape qualification; compliance enforcement is PIVIMP-004. |
| Part 1 Tables 12/15–17/20+, text 2155–2170 and 2181–2197 | Certificate TLVs 70/71/FE; 1856 bytes is a **recommended** certificate value length and may be exceeded. | The SDK's automatic GZip threshold is local policy, not a normative device maximum or a promise compression will fit applet storage. |
| SP 800-78-4 (May 2015), §3.1/Table 3-1, text 414–479 | Federal PIV key profile: RSA exponent65537; RSA2048 and P256/P384 in slot-specific roles; PIV/Card Authentication EC constrained to P256. Applications **should** process a broader exponent range. | Generic YubiKey capabilities (RSA1024/3072/4096, Ed25519/X25519, arbitrary accepted slot combinations) exceed this profile. Import exponent65537 is an implemented request restriction; avoid implying every supported key/slot pairing is federal PIV compliant. |
| SP 800-78-4 §§3.2.1–3.2.2, Table3-2, text 481–598 | Issuer/CA/CMS signatures on PIV information; RSA-PSS and PKCS#1v1.5 both represented, curve/hash qualifications apply. | Distinguish issuer/CA requirements from a raw RSA API and general-purpose demo self-signed certificates. |
| SP 800-78-4 §5, text657+ | Card authenticates the management system via symmetric challenge/response; algorithm/size requirements are profile/date-conditioned. | Factory key bytes are a Yubico convention. The standard never mandates Yubico's repeated010203... factory pattern. |
| FIPS201-2 (August2013), §2.9.3, text1292–1350 | Issuer/department PIN-reset identity-verification procedures; SHALL controls depend on in-person/kiosk/remote route. | `ResetAsync` is a destructive Yubico applet reset, not implementation of federal issuer PIN-reset identity procedures. Archive cover records supersession by FIPS201-3 on January24,2022. |
| FIPS186-5 (February2023), §6.4.1 steps1–2, text1325–1338 | Signature generator hashes M then takes leftmost min(order-bit-length, hashlen) bits to integer e. | API accepts caller-prepared digest; P256/P384 are byte-aligned, so truncation/zero-left-padding preserves e. This citation explains arithmetic, not permission to use every hash under the PIV profile. |
| RFC8017 §§5.1/5.2, 7.1/7.2, 8.1/8.2, 9.1/9.2 | RSA primitives, encryption schemes, signature schemes, EMSA encoding/verification; caller/protocol selects the scheme. | Raw `SignOrDecryptAsync` expects a complete RSA representative; host constructs EMSA-PKCS1-v1_5/PSS or removes encryption padding. PSS verification is available with `.NET RSASignaturePadding.Pss`. |
| RFC8032 §§5.1.5–5.1.7, 8.2 | PureEd25519 signs the message; signing determinism supports differential tests, independent verification establishes key/message binding. | Core PIV implementation passes messages unchanged. PivTool's generic hash-first helper changes that message and needs a separate fix. |
| RFC7748 §§5–6.1 | X25519 uses32-byte little-endian u-coordinate encoding; applications derive keys from the shared result; all-zero shared-result handling is protocol-dependent. | Describe raw agreement bytes and caller KDF/authentication responsibilities. Present suite's named X25519 agreement test performs only generation. |
| RFC3279 §2.2.3 | ECDSA signature ASN.1 SEQUENCE of INTEGER r,s in certificate signature encoding. | Existing device tests explicitly verify DER with `Rfc3279DerSequence`; document this format instead of .NET's default P1363. |

SP800-78-4 archive cover records withdrawal July15,2024 and supersession by revision5. This run
uses the original revision4 passages and makes no new revision5/FIPS201-3 compliance claim.
ISO7816 full edition text was not obtained. NIST's incorporated command tables and pinned vendor
implementations support the APDU explanations here; exact ISO edition/section conformance remains
unestablished and should stay out of public prose.

## Vendor evidence and bounded differences

`vendor-reset-command.txt:29–63` explicitly retains F9 key/certificate, requires blocked PIN+PUK,
and directs Bio MPE reset through the device-reset route. `vendor-technical-manual.txt:163–210`
retains F9 on reset; moving excludes F9 at both ends; deleting includes F9. These are vendor
capabilities, not authorization for this campaign to replace/delete existing F9 material.
`vendor-attestation-tool.txt` explains factory F9 material is replaceable and irrecoverable after
replacement, and distinguishes pre5.7.4 and newer certificate chains. Retrieval alone establishes
neither chain trust nor proof that the returned public key is the intended slot key.

Pinned Python CLI `python-ykman-_cli-piv.py:912–964` rejects identical source/destination selectors
and translates the device's incorrect-parameters status to an occupied destination, and missing
reference to an empty source. This supports the destination-empty prerequisite as implementation
evidence. The .NET helper rejects F9 as a source locally; destination F9 restrictions and occupancy
are left to the device. Certificate handling remains a separate operation.

`vendor-technical-manual.txt:463–469` dates AES management support to5.4.2. Python and .NET gate
at5.4.0; public docs should label the SDK gate and released-device evidence separately. The source
batch supports no newly invented5.4.0/5.4.1 interoperability claim. YSA2017-01 explicitly discusses
PIV on4.2.6–4.3.4 and recommends ECC generation or host-generated RSA import; Python's broader
4.2.0 gate is conservative implementation evidence. Keep the distinction and remove “safe” as an
unqualified result of `SupportsRsaGeneration`.

## Pinned Python code, tests and salient ancestry

Reference revision `4ca60f706af930459138d8dc0f0f953480e1c7a4` is explicitly pinned in current
README. Freshly downloaded immutable files match that revision. `python-history.txt` is constrained
to that commit's ancestry; later Rust/Python reorganization commits observed under `--all` were
excluded. `collect-followup.py` first failed at an unavailable optional HTML parser after downloads;
its corrected standard-library parser reused captured bytes and completed local history capture.

* `python-yubikit-piv.py:600–638`: P3844.0, policies4.0, cached4.3, ROCA4.2.0–4.3.4, FIPS4.4.x
  plus explicit `fips_restrictions`, new key types5.7. .NET's firmware-only4.4.x restriction lacks
  the later product-profile argument; record a capability/qualification gap rather than assuming
  identical FIPS enforcement.
* `:736–785`: Python probes configured Bio before reset; .NET retains a TODO and starts blocking
  credentials immediately. `:893–994`: Python caches retry fallback estimates; .NET deliberately
  raises count-unavailable on9000 instead. Sibling code is comparison evidence, not the desired contract.
* `:912–974`, `1111–1133`, `1536–1537`: UV tag03/02/empty; temporary PIN tag01; metadata tagged
  retries06/configured07/temp08. MATCH capability probe succeeds independently of enrollment.
* `:1162–1183`: Python decrypt derives RSA type from ciphertext length; .NET queries slot metadata,
  so high-level decrypt requires metadata5.3 even where raw RSA works on older keys.
* `:1258–1309`: Python rejects unsupported CertInfo and handles GZip plus Net iD zlib; .NET reads
  GZip only and treats values other than1 as uncompressed. Keep interoperability/enforcement scope explicit.
* `python-ykman-piv.py:209–273`: Python timestamp is fixed4-byte **big-endian**; .NET
  AdminData date is trimmed little-endian Unix seconds. This is a concrete compatibility difference,
  PIVIMP-009; Yubico software format, not a NIST field.
* `:314–414`: Python's separate pivman helpers synchronize management-key rotation, derived-PIN
  rotation and PUK-blocked flags; the .NET raw operations leave related objects unchanged. Avoid
  promising atomicity or automatic coherence.
* `python-tests-test_piv.py`: source-backed capability tests, nonfederal sample CHUID shape and
  separate GZip/Net iD tests. `python-tests-device-test_piv.py:67–115` resets/selects profiles through
  fixture state, uses SCP for FIPS NFC, and handles reset-blocked devices through Management.
  `:558–575` actually rejects a second Always-policy sign; `:618–627` verifies with the slot public
  key; `:640–642` skips PUK tests on Bio. Those are stronger/profile-aware oracles than some .NET names.

Read immutable patches in this evidence directory:
`0717f648f7eb` adds ROCA generation refusal; `b8756f584446` introduces serial with5.0.3 gate;
`72de4264018b` adds configured-Bio reset preflight; `5a9d4960508d` establishes UV mode payloads;
`9909740c44c5` makes MATCH a capability probe; `377823882ec4` handles Bio metadata on older devices;
`d3ba7ebee917` introduces move/delete; `55470cbb7895` and `70e11d77942a` introduce25519/largeRSA;
`fe91d0ecc8d5` bounds malformed PRINTED handling; `d4982afa43a3` records Net iD decompression support.
Their existence explains origins; their code/tests and the current pinned implementation determine
the comparisons. Android remains pinned to`76b89d35142d9a4937b6b72e5c840e057a280620` as an
additional implementation comparison. No source was silently updated to latest.

## Merged local history rebaseline

Current PIV files match merged PR745. `74942e1dafc4` owns gates, retry fallback and PIN-only workflow
decisions; `1eb242e3ab86` switches response cleanup to explicit owner clearing and writable outputs;
`8a7d152dd4a5` strengthens unknown-algorithm extraction and second-stage recovery-witness tests;
`58d9f2b93428` plus merge`b1362127` map canonical key representations/subsets. These supersede
older candidates for those behaviors. Current source was read in full after this orientation.
Older`8a7860fa` already fixes Ed25519 message passthrough, short-ECDSA left-padding and oversize-RSA
rejection. This pass records their verified code shape instead of reopening them as current defects.
