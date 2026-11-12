# CTAP2/Core source authority and bounded gaps

Originals govern protocol assertions; implementation and sibling SDKs explain current behavior and
policy. [source-identities.json](source-identities.json) hashes every reused source/report identity.
Access dates of retained captures remain their original dates; reading them in this packet adds no
new remote retrieval or runtime qualification. Full originals retained by B5 are primary authority;
earlier CTAP passage records identify exact version/actor/profile conditions for already investigated
applet issues. Independent reviewers must recheck consequential originals before content acceptance.

## Original passages inspected for this boundary

| Source / exact status | Section and actor / strength | Boundary conclusion and applicability |
| --- | --- | --- |
| CTAP 2.1, Proposed Standard 2021-06-15, errata edition 2022-06-21; B5 `sources/ctap21.html` and derived `.txt` | §6.5.1, platform MUST enforce four Unicode characters, NFC, UTF-8 <= 63 bytes and no final 0x00; authenticator MUST enforce baseline four code points / 63 bytes and may impose a larger minimum | Core char limits and ClientPin byte limits cover different domains. Allocation of platform enforcement between host input and SDK remains policy; no silent normalization fix authorized. Original text lines 6316–6349 inspected. |
| Same CTAP 2.1 errata | §6.5 introductory note: platform MAY manage token-copy lifetime and SHOULD erase as soon as unneeded | Supports host token owner/cleanup responsibility, with authenticator expiry a separate state. Local memory lifetime is not device authorization. Lines 6299–6315 inspected. |
| Same CTAP 2.1 errata | §6.5.5 input/output tables: platform-selected pinUvAuthProtocol MUST be supported; keyAgreement MUST include alg and MUST NOT include other optional parameters | Generic Core metadata and P-256 provider import supply no missing CTAP COSE-label validation. Lines 6947–7069 inspected. |
| Same CTAP 2.1 errata | §6.5.5.2/.3, platform retry queries and authenticator replies; optional powerCycleState gives no information when absent and is valid only for PIN retries | Current parsers default counts to 0 and booleans to false, and accept field 4 on UV. Keep snapshot/default and recovery responsibility distinct. Lines 7044–7115 inspected. |
| Same CTAP 2.1 errata | §6.5.6 ecdh/getPublicKey/encapsulate; procedure requires kty=2 (EC2), alg=-25, crv=1, 32-byte X/Y and an on-curve point; §6.5.7 inherits profile and overrides KDF/IV/tag/token details | Two independent HKDF invocations with distinct context strings; generic alg=-25 label is a profile marker, not actual generic ECDH-ES-HKDF operation selection. V1/V2 helper cleanup/profile requirements remain CTAP-owned. Lines 8200–8364 inspected. |
| CTAP 2.3 Proposed Standard, 2026-02-26; B5 retained `ctap23.html/.txt` | §11.2.9.1.5, authenticator MUST cancel cancelable active request and MUST NOT reply to CANCEL; client MAY send during CBOR; original request carries KEEPALIVE_CANCEL. §11.2.9.1.6/.7 specify control length 1 and keepalive SHOULD cadence | Core's keepalive-observed CANCEL/drain implementation is distinct from native abort/deadline. Returning raced real success is SDK policy, not an invented platform MUST. Lines 13960–14029 inspected. |
| Same CTAP 2.3; B5 register plus retained complete transport sections | §11.2.3/.4/.5/.9.1.3, host/authenticator channel and framing actors; SHALL accept INIT extension bytes beyond 17, reserved CIDs and nonce correlation | Reuse B5-C003's exact validation/recovery questions. Fixed 64-byte reports are the SDK contract; descriptor-defined standard sizes are broader. This boundary read current source, not a new whole transport-spec audit. |
| CTAP2.0 Proposed Standard 2019-01-30; CTAP2.1 original/errata; CTAP2.3 Proposed Standard as above | 2.0§5.2 permits descriptor omission for actual singleton allowList; later§6.2.2 require descriptor. Authenticator MUST NOT return identifying user names without UV; opaque ID and platform-to-WebAuthn account-picker processing have different actor conditions | Earlier accepted CTAP/User documentation remains resolved. Native selected-result serialization/privacy and WA descriptor recovery remain separately owned decisions. Exact original URLs/quotes retained in CTAP `evidence/g/boundary-originals.md:16–34`; no actor policy adopted here. |
| CTAP 2.3 Proposed Standard 2026-02-26, existing CTAP original record | §6.1/6.2.2 response key 6 / key 8; §6.4 optional encIdentifier/encCredStoreState; §6.10 shared storage; §12.3/.4 storage-key versus direct extension | Reuse exact original applet facts: unsigned slices versus signed authData, IV 16 bytes + ciphertext 16 bytes with matching HKDF context, conditional write authentication and fragment commit. Core framing never implements missing model support or a whole-array transaction. Existing original passage record `evidence/e/primary-excerpts.md:23–41`, independent boundary original check:54–85. |
| Yubico Labs DRAFT Web Authentication sign extension version 4, Editor's Draft 2025-08-26 | §10.2.1, algorithm-specific TBS, present additionalArgs MUST be an encoded COSE_Sign_Args map, required handle MAY be empty; attestation/key-binding actors | Preview policy/DTO constraints and Core mathematics remain distinct. Earlier exact URL/status/passages in `evidence/e/primary-excerpts.md:43–55` and `g/boundary-originals.md:105–113`. No adopted W3C/CTAP 4 or production claim. |
| ARKG draft-bradleylundberg-cfrg-arkg-10, 2026-02-27 Internet-Draft, intended Informational; expiry 2026-08-31 | §5.1/.2/8.3 representation; §2.3 ctx <= 64 algorithm bound | Preserve -65700 erratum, BL=-1/KEM=-2, example dkalg=-9, outer kty=-65537 / direct maps / optional int-or-text dkalg. Current SDK agrees partly; it emits kty=2 / wrapped maps. Earlier independent original check `g/boundary-originals.md:115–121` remains bounded. |
| ARKG -09, 2025-07-07 expired Internet-Draft; Core C's completed original work | §2.3/3.1/3.2/3.3/4.1 algorithm procedures; RFC9380 informational §5.2/5.3.1/8.2 | Core math reference target remains -09. C compared exact tags/KDF/reduction structure; CTAP representation -10 does not upgrade the Core mathematical claim. Full equivalence/side-channel/native producer evidence stays open. |

Every CTAP edition above retains its own section numbering and publication status. A Proposed Standard
claim about one transport/command never makes the entire SDK conformant to that edition. Optional
advertisement, current state, selected PIN protocol, token permission and firmware are separate conditions.

## Library/API orientation and pinned implementation evidence

Context7 resolved `/dotnet/docs` and was queried specifically about UTF-16 char counts, Unicode scalar
values and UTF-8 byte lengths. The result points to Microsoft's
`docs/standard/base-types/character-encoding-introduction.md`: char is one 16-bit UTF-16 code unit;
supplementary scalars require two char values; UTF-8 uses a different variable byte count. This current
orientation supports unit vocabulary, while Core's actual ReadCredentialCore/SatisfiesPolicy/ConvertToResult
source establishes which count the SDK uses. No runtime input experiment ran.

Existing .NET reference source retained in B1/B5/C and CTAP's framework evidence provides version-specific
context: Task.WaitAsync bounds another wait rather than source work; ReadOnlyMemory views have no owner;
default Guid byte conventions differ from native host order; CborWriter Encode creates another array and
Reset clears its written internal prefix. These source facts retain their exact pinned reference versions
(B5/C primarily runtime v10.0.0; earlier CTAP CBOR 10.0.2 and Guid v10.0.14). Current repository CBOR 10.0.15
is a different package; no old executed experiment is relabeled as this package's fresh validation.

## Pinned Python context and focused follow-up

The shared YubiKey Manager source pin is `4ca60f706af930459138d8dc0f0f953480e1c7a4`, project 5.9.3-dev.0,
separate from installed 5.9.2. C's `sources/python/context.md` and `manifest.json` retain general Core,
short-APDU/SCP and exact 0.0.1 development-override history. Those are implementation policy comparators,
with different immutable-byte/connection ownership.

This packet attempted read-only `git show <pin>:yubikit/fido.py`; Git reported that path absent. One
focused `git ls-tree <pin>:yubikit` confirmed Core/app applet modules without a FIDO module. The exact
gap is **no CTAP implementation oracle in that pinned manager tree**. Earlier python-fido2 comments
require their own exact repository/module pin and reviewed source; no guessed manager path, import,
new clone or package installation substitutes for them. The failed lookup is retained as an outcome,
not evidence of a protocol problem. No Python package/crypto/native/device code was executed.

## Research bounds and remaining questions

The initial pass reused the completed family and applet originals. The focused pass inspected exact
retained CTAP PIN/control passages and resolved the manager-module lookup above. Remaining issues have
precise owners rather than an indefinite search queue:

- B6 owns ISO7816 APDU/profile authority, CTAP-over-SmartCard command chaining/Le/status and final
  GlobalPlatform SCP sources. Preserve unavailable final GP/ISO passages and any draft/vendor distinctions.
- C retains NIST final PDF access 403, SEC 2 retrieved Draft headers, ARKG -09 math and unpinned Rust KAT
  generator provenance. This packet claims no final NIST/FIPS/production qualification.
- CTAP DEC-003 retains exact SKU/earliest hmac-secret-mc/PPUAT/vendor preview support and reset 5.5.2/5.5.3
  window gaps. Existing 5.8 vendor documentation bounds named families rather than earliest universal support.
- B3's published NativeShims 1.18.1 source establishes inspected exports, not the installed binary's
  producer/build. B4/validation must bind installed package/runtime/export/provider evidence separately.
- BOUNDARY-WebAuthn/maintainer owns selected-native User actor/serializer policy and WA-IMP-010 recovery.
  Authenticators' identifying-field restriction supplies no automatic field-stripping authorization.

Fresh independent citation review and selected runtime/platform/device qualification remain later gates.
