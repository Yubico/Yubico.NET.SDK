# B6 original-source checkpoint

The download manifests retain exact original URLs, UTC access timestamps, status,
byte counts and SHA-256. PDFs are original bytes; `.txt` files are `pdftotext -layout`
derivatives. Source retrieval happened November11 UTC; the session's local task date
is November12. Research is bounded: one initial search/catalogue inspection and one
focused follow-up. This checkpoint makes no final-specification or device certification claim.

| Source / exact status | Passage actually read | Actor, strength, applicability and result |
| --- | --- | --- |
| GlobalPlatform Card Specification v2.3 Amendment D, SCP03 v1.1.2, Public Release, March2019, GPC_SPE_014 | §4.1.5; §6.2.1–6.2.5; §6.2.6–6.2.8; §7.1.1 through §7.1.2.7 response/status introduction | §4.1.5 SHALL specifies counter-mode CMAC and exact label/separator/length/counter/context layout. §6.2.1 SHALL sets session-key context and constants. §6.2.4 off-card C-MAC excludes Le and modifies header. §6.2.5 card SHALL sends status only without protection for errors; errors exclude9000/62xx/63xx; external-authenticate response has noRMAC; off-card SHALL recomputes RMAC. §6.2.6 off-card SHALL advances counter but SHALL applies no encryption to absent command data. §6.2.7 card uses command counter with first byte80 and omits encryption for absent response data. §7.1.1.6 conditional sequence counter means minimum29 is different from exact29. §7.1.2 command table has absent Le. These specific final-edition claims are independently read, not inherited from Python. |
| GlobalPlatform SCP03 v1.1.2.6, Public Review, December2019, target1.2 | Search result original cover/revision text only | This is the edition cited by ScpState, but direct download returned403 during focused follow-up. Do not label it Public Release or transfer final1.1.2 section numbers automatically. Official catalogue says final1.2 published April2020 and adds S8/S16. Exact1.1.2.6 passages remain unverified. |
| GlobalPlatform Card Specification v2.3 Amendment F, SCP11 v1.3.0.13, Public Review, August2023, GPC_SPE_093 | §2.1/2.2, §3.1.1/3.1.2, §4.1 including complete a/b/c diagrams, §4.2 option tables, §7.5.1, §7.6.2.3 through Table7-18/response status introduction | Draft normative actors retained: a uses two static/two ephemeral pairs; b reuses OCE ephemeral; c reuses SD static (§3.1.1). §7.6.2.3 SD SHALL verifies fields, generates proper agreements and SHA256 KDF; SharedInfo ID fields are conditional on request option. Table7-18 defines c's returned5F49 as static, a's as ephemeral. §7.5.1 SD SHALL verifies OCE certificates, first CA-signed certificate allowlist, key usage/structure; other fields MAY be ignored in that command. This is SD processing, not a universal host-chain validation exemption. |
| GlobalPlatform SCP11 final1.4 catalogue, published March2024 | Catalogue description and prior-edition list; HTML retained | Establishes publication identity and stated changes only. Final wire/actor clauses remain uninspected. Initial/focused searches located public-review and older final1.2 links, not a retained final1.4 PDF. Catalogue exposes download_spec links; no registration, licence acceptance or gated action was submitted. |
| GlobalPlatform SCP11 v1.2 Public Release July2018, vendor-linked original | Search result cover only; direct download403 | An older final is a different target from current code's1.4. No normative clause was adopted from unseen final1.2. |
| Yubico technical manual, Secure Channel Protocol (SCP03 and SCP11) Specifics, live page snapshot | Firmware/subset/support, key/session tables, establishment/communication and diversification introduction through BMK generation, via Exa original-page extraction | Vendor describes AES128 SCP03 from5.3.0 and P256 SCP11 from5.7.2, strict security level, and says plain commands terminate channel. Broad narrative includes suspect external-authenticate encryption/RMAC descriptions and inconsistent transport-key language; use exact final GP wire tables for the claim at issue. Snapshot is vendor guidance, not fresh hardware result or complete device profile. Remaining page is not claimed read. |
| Context7 `/dotnet/docs`, stackalloc C# reference | Retrieved explicit undefined-initial-content guidance | Language documentation directs initialization before reading stackalloc memory. ScpState.DeriveIv leaves bytes1..11 unwritten; CbcEncrypt's “Zero IV” stack buffer has no explicit Clear; AesCmac subkey zero block and partial-final-block tail likewise rely on ambient local initialization. Record a portability/source-quality candidate under C004/C011/C013; actual compiled localsinit/JIT/AOT behavior remains untested. Do not claim observed garbage bytes or a current cryptographic failure. |
| .NET runtime v10.0.0 ArrayBufferWriter original | Two attempted file paths returned404 | Internal growth-buffer erasure remains unverified after bounded attempts. SDK-owned fragment, response, plaintext and failure paths supply C002 without relying on hidden BCL behavior. No runtime crypto/native experiments ran. |
| Packet C final source register | `../../C/sources/final-source-register.md`, §Actual framework/native call scope | Reuse exact retained .NET10/provider, AsnWriter, OpenSSL and package qualifications. C's checked source establishes AsnWriter.Encode leaves internal copy and Reset clears it; neither proves erasure of B6's other buffers. NativeShims1.18.1 source versus deployed binary binding remains separate B3/B4 qualification. |
| RFC 4493, Informational, June 2006, retained original `../../C/sources/rfc4493.txt` | §4 complete subkey and Examples 1–4 passage, derivative lines 570–608; §2.4 verification discussion read with preceding algorithm | Four AES-CMAC message/tag literals and K1/K2 values in AesCmacTests match the original text. This is published-vector identity and actual full-value assertions, not a new CMAC execution, constant-time proof or NIST certification. |
| Python YubiKey Manager `4ca60f706af930459138d8dc0f0f953480e1c7a4` | Complete retained `yubikit/core/smartcard/__init__.py`508 lines and `scp.py`375 lines | Implementation comparator only. `_derive` exact input bytes match C# AES128 scheme; Python empty Encrypt also pads; Python response chaining sits below SCP unlike C#; Python SELECT discards SCP processor; raw errors map differently. Python immutable bytes/key objects have different ownership. Shared manifest supplies hashes; tests/history remain pending B6 independent reading. No package import or Python crypto executed. |

## Explicit continuation gaps

Supplemental complete passage reading: SCP03 §6.2.5 through its MAC-chaining figure
and following paragraph confirms response/current-command linkage and optional
R-MAC only in the general SCP03 security-level model. SCP11 Public Review §§4.2–4.6
and §7.1.1/Table 7-2 were then read with their full conditions. The SD's a/c host
authentication includes the first post-establishment command with a correct MAC;
the host verifies the receipt during establishment. The requested parameter byte's
bit 3 controls Host/Card/CardGroup inclusion; this differs from the catalogue's
implementation-option bitmap. The draft's SCP11 secure messaging forbids changing
R-MAC with BEGIN/END commands and distinguishes a/b forward secrecy from c session
replay and command restrictions. These remain draft-edition actor/profile statements,
not an inspected final v1.4 or current YubiKey permission verdict.

1. Read the two assigned vector files completely and compare embedded selected rows,
   headers/output lengths/SharedInfo against the actual consuming tests. Retrieve/check
   the original NIST/CAVS provenance only for claims those files make. C's NIST access
   failures remain qualified; they are not permission to use a draft as final.
2. Read the shared pinned Python device test and retained exact SCP03/SCP11/short-APDU
   history patches independently. Their source hashes alone add no reading coverage.
3. Verify claim-driven ISO7816 edition/section for general APDU cases/Le and logical
   channels. Keep observed C# bytes separate until that passage is read.
4. Final SCP11v1.4/vendor-specific exact-profile text and SCP03v1.1.2.6 raw original
   remain bounded gaps, not another open-ended latest-version search queue.
5. Narrow ApplicationSession/SD/key-implementation dependency reads and exact ownership
   call sites still need final trace completion; B5/C/boundary primary ownership remains.
