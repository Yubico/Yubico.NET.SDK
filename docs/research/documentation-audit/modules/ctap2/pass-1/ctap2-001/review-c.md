# C — fresh independent documentation review

**Verdict: REQUIRED CORRECTIONS; not PASS yet.** Two factual correction groups remain. The
credential/user/version/UV/ownership/parser/test-evidence changes otherwise pass the scoped semantic
review below. Optional wording suggestions are not acceptance blockers.

**Reviewer:** GPT-6.1 Sol, fresh independent session. **Date/access date:** 2026-11-04.
**Reviewed checkout:** `/tmp/opencode/ctap2-001-edit-c`.
**HEAD/baseline:** `5b09a9b6942ded3913b5c9340c3e126617e61a82`.
**Scope:** all 27 changed C-owned paths, plus the three unchanged C-owned paths; complete files,
XML/internal explanatory units, method bodies, fixture helpers and assertions. Reviewed the complete
source and test diffs against the baseline, not only added lines. Exact reviewed hashes are below.
This report is authored directly in the primary packet. No source, test, example, directive, runtime
string, assertion or API was edited by this reviewer. No build, SDK test, hardware/PIN operation,
commit, delegation, integration copy or framework experiment was run.

## Required corrections

### RC-R1 — ARKG-10 does specify the seed algorithm placeholder -65700

**Required factual correction; C-009 / CTAP-DOC-016 / CTAP-IMP-029 evidence reconciliation.**

Affected complete explanations:

- `src/Ctap2/src/Cose/CoseArkgP256SeedKey.cs:33–51`, particularly **41–44**.
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Cose/CoseArkgP256SeedKeyTests.cs:70–114`,
  particularly **72–73**.
- Current handoff `edit-c.md:80–89,161–164` and the C-009 evidence/dependency interpretation.
  Preserve immutable investigation/evidence records; reconcile their unsupported subclaim explicitly
  rather than rewriting history.

Independent original retrieval of
[draft-bradleylundberg-cfrg-arkg-10](https://www.ietf.org/archive/id/draft-bradleylundberg-cfrg-arkg-10.html)
shows §5.1's public-seed example contains **`1: -65537` and `3: -65700`**, with direct maps at
`-1`/`-2` and `-3: -9`. §5.2 Table 2 and §8.3 also explicitly identify **ARKG-P256, placeholder
-65700**. §5.2 separately identifies **ESP256-split-ARKG, placeholder -65539**.

The new public passage says the draft uses different kty/direct maps, “not this SDK's
**kty=2/alg=-65700** wrapped-map profile.” As a statement that the complete profiles differ this can
be read correctly, but grouping the algorithm with the differences—and the test's categorical
“not these outer labels”—carries forward the investigator's false claim that -65700 is unsupported
by this draft. The actual outer *labels* 1/3 agree too; the kty *value* differs.

**Correct the distinction explicitly:** the seed algorithm placeholder **-65700**, BL=-1,
KEM=-2 and the example's derived ESP256=-9 agree; the SDK's emitted outer **kty=2** and
**byte-string-wrapped nested maps** differ from draft-10's **kty=-65537/direct maps**. The SDK also
requires an integer derived algorithm where the draft makes dkalg optional and permits int/tstr.
Do not promote that partial agreement into registration, vendor compatibility, whole-profile
interoperability, validated points or production cryptographic authority. Retain the experimental
warning, expiry qualification and deferred maintainer profile decision. Change comments only;
no wire constants, writes, assertions or constructors need changing in this pass.

### RC-R2 — the label-routing fixture description does not match all four coordinate arrays

**Required low-severity factual correction; C-011/014 / CTAP-DOC-012/016.**

`src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Cose/CoseArkgP256SeedKeyTests.cs:149–159` says
the two keys have “all-0xAA coordinates” and “all-0xBB” coordinates. The complete body at **168–173**
actually sets **BL X=AA, BL Y=AB, KEM X=BB, KEM Y=BC**, and **198–203** asserts both full X and
Y arrays as well as each first X byte. This inherited phrase remains inside the affected rewritten
summary and was not caught by the editor's full-assertion review.

Describe distinguishable synthetic coordinate patterns accurately—either list AA/AB and BB/BC or
say the **X coordinates** are AA versus BB. Preserve the useful BL/KEM routing rationale, the
non-crypto/non-device limitation and every executable assignment/assertion. This is not a request
to improve test coverage or run previewSign.

## Optional suggestions (not blockers)

- **RC-O1:** `CoseArkgP256SeedKey.cs:149` says “A new CBOR array”. Prefer “A new byte array
  containing the encoded COSE map” to avoid suggesting CBOR major type 4; Encode writes a map.
  The following remarks already expose the map profile, so this is a clarity suggestion.
- **RC-O2:** `GetAssertionResponse.cs:81–85` can say the decoder passes **modeled name/displayName**
  through. The normative prohibition correctly includes icon, but `PublicKeyCredentialUserEntity.Parse`
  skips icon with other unknown fields. The entity parser's own remarks already state this narrower
  preservation scope; avoid suggesting an icon-preserving privacy filter either way.

No broader preference-driven rewrite is requested. The added qualifications generally preserve the
approved direct, calm voice and supply useful actor/lifetime context.

## Independently checked original citations

All sources below were fetched anew from original URLs during this review. Exa was used for FIDO
originals after direct stdlib HTTP received 403; full extraction plus focused clause excerpts, not
publication previews alone, supplied the consequential clauses. Direct HTTP fetched W3C, IETF,
IANA and release-pinned runtime source. An initial local HTML extraction failed because BeautifulSoup
was unavailable; no dependency was installed. The stdlib extraction succeeded for non-FIDO sources.
These were read-only retrieval/inspection operations, not SDK execution.

| Original title/revision/publication status | Independently inspected clause and conclusion |
| --- | --- |
| [Client to Authenticator Protocol (CTAP), Proposed Standard, 30 January 2019](https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html) | §5.2 response table: descriptor “May be omitted if the allowList has exactly one Credential.” Exactly one **listed** descriptor, not one match or a non-discoverable blanket exception. §5.3: next-call count/timer semantics. |
| [CTAP 2.1 Proposed Standard with errata, heading 21 June 2022](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html) | §6.1 platform MUST NOT include uv with pinUvAuthParam, or uv without built-in UV; the prohibition concerns the **key**, including false. User name/displayName inputs optional for privacy; empty account ID valid but discouraged. §6.2.2 descriptor Required; discoverable user ID mandatory, server-side user entity optional, identifying fields MUST NOT be returned without authenticator UV. Defaults count=1/userSelected=false; next responses omit count. §6.3/§5 sequence obligations remain caller/device duties. Exa's generic published metadata says 2021-06-15, but the actual heading is **2022-06-21**; do not substitute URL-directory metadata. |
| [CTAP 2.3, Proposed Standard, 26 February 2026](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html) | §6.1/§6.2 same relevant uv and user boundaries; §6.2.2 required descriptor independently reread. Make unsigned outputs **0x06**, get unsigned outputs **0x08**; clients MUST treat empty map as omitted. No first-introduction history established. §6.4 ep=true enabled, ep=false capable but disabled, absent unsupported. §6.5.1 platform PIN NFC/UTF-8 maximum63 bytes/final0x00 exclusion and minimum/code-point distinction; GetInfo minimum is current code points. |
| [Web Authentication: An API for accessing Public Key Credentials — Level 2, W3C Recommendation, 8 April 2021](https://www.w3.org/TR/2021/REC-webauthn-2-20210408/) | §5.4.3 opaque handle max64, MUST NOT be empty/contain PII, RP authorization uses id not displayName/name. §5.8.5 credential-key profile: ES256→P256, ES384→P384, EdDSA→Ed25519, distinct from generic COSE algorithms. §6.1 base37 layout/counter; §6.1.1 mismatch is not proof which authenticator was cloned. §6.5.1 requires alg and relevant required COSE fields. §§7.1/7.2 separate RP verification. §8.2 packed fields; §8.7 none emptyMap={} / no attestation, not self. Signature verification during registration is policy-dependent, not an unconditional all-registration requirement. |
| [Web Authentication: An API for accessing Public Key Credentials — Level 3, W3C Recommendation, 25 August 2026](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#sctn-credential-backup) | **Publication status verified, not presumed from REC URL:** original title/header say W3C Recommendation, 25 August 2026; Status of this document says published as a Recommendation using the Recommendation track and endorsed by W3C/Members, successor to L2, no substantive changes since 26 May 2026 CR Snapshot. Actual **§6.1.3 Credential Backup State**: BE set during makeCredential and MUST NOT change; BS can change; BE0/BS1 prohibited; flags concern the credential. The new citation is valid and correctly scoped to BE/BS. Retaining L2 solely for base layout/counter/profile/user/none claims is appropriate. |
| [IANA CBOR Object Signing and Encryption registry](https://www.iana.org/assignments/cose/cose.xhtml), Last Updated **2026-08-25** | -9 ESP256=P256/SHA256; -47 ES256K=secp256k1/SHA256; -8 generic EdDSA, deprecated, with distinct Ed25519/Ed448 curve identifiers. Recognized SDK constants do not prove registry registration, provider/device support or trust. No identifier change warranted. |
| [The Asynchronous Remote Key Generation (ARKG) algorithm, draft-bradleylundberg-cfrg-arkg-10](https://www.ietf.org/archive/id/draft-bradleylundberg-cfrg-arkg-10.html), Internet-Draft published **27 February 2026**, intended Informational, expires **31 August 2026** | Original memo calls Internet-Drafts work in progress; stated expiry precedes review. §§5.1/5.2/8.3 independently establish **both** seed placeholder-65700 and signing placeholder-65539. kty=-65537/direct nested COSE maps differ from SDK emission. Required correction RC-R1; no new standardized/vendor/production authority follows. |
| [RFC8152 §7 COSE Key Objects](https://www.rfc-editor.org/rfc/rfc8152.html#section-7) | Original structural reference fetched. It is the older RFC cited by CoseKey, not an up-to-date whole-registry/support promise. The SDK's explicit int32-alg/required modeled-field contract is its actual narrower decoder behavior, not universal COSE optionality or mathematical validation. |

### Framework adjudication independently checked, without rerunning its experiment

Context7 resolved `/dotnet/docs` and was queried about Ctap2Canonical sorting and ReadEncodedValue
ownership. It returned array-range/AsMemory guidance, not the required CBOR implementation; that
result is **not** accepted as CBOR proof. Independently fetched official **dotnet/runtime v10.0.2**:

- [CborWriter.Map.cs](https://raw.githubusercontent.com/dotnet/runtime/v10.0.2/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor/Writer/CborWriter.Map.cs),
  **121–175**: tracks out-of-order key/value ranges and sorts/copies them at completed map write.
  Retrieved SHA256 `14549e61adba45feb51295c5313ca251365ceaad93d35c6da282a33d6141be57`.
- [CborReader.cs](https://raw.githubusercontent.com/dotnet/runtime/v10.0.2/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor/Reader/CborReader.cs),
  **46–81**: BytesRemaining is unread total; constructor retains supplied memory; ReadEncodedValue
  returns `_data.Slice(initialOffset, ...)`. **200–231**: PopDataItem restores enclosing state,
  not whole-input exhaustion. Retrieved SHA256
  `c7493df311a5f7229a7f59bba256b087b7637dc29a8682c06320447d7a34d0a5`.

Read primary `framework-adjudication.md`, including its binary/probe/package/source identity limits.
Its executed 10.0.2 evidence belongs to the coordinator's earlier isolated probe, not this reviewer.
The source independently corroborates automatic writer sorting and borrowed encoded-value slices.
No insertion-order wire bug is alleged. The trailing-root mechanism is scoped to decoders without an
exhaustion check; no SDK decoder or arbitrary malformed corpus was executed here, and nested parsers
must not be forced to consume their enclosing reader's entire buffer.

## Scoped PASS conclusions and remaining implementation limits

- **User / WA-XREF-001:** corrected `User`/handle/display distinction agrees with the original
  CTAP tables and real parser. ID can be present without UV; supplied modeled display fields are
  not filtered for UV. Public construction borrows nonempty<=64 ID and requires names; Parse copies
  ID, permits absent names and still rejects empty/oversized ID. These are SDK restrictions,
  not invented universal CTAP requirements. `GetUserHandle` empty fallback is not failed authentication.
- **Descriptor / WA-IMP-010:** exact 2.0 singleton request exception and later Required descriptor
  are now accurate. Decoder lacks request/version context and returns null/empty without recovery.
  Neither this review nor comment changes fix contextual validation/final-ID recovery.
- **UV/options/encoding:** independently read all options/setters and both builders. Nullable false
  is encoded, auth emitted independently, protocol2 default only with nonempty auth, protocol-only
  omitted, empty lists/zero-byte extension input omitted but encoded A0 present. up=false make
  rejection and duplicate parameter rejection/preference preservation are real. No token permission,
  tag length, capability negotiation or exclusive uv enforcement is invented.
- **Ownership:** make Decode borrows attestation/unsigned slices (including reader overload), while
  authData/raw/nested byte fields/largeBlobKey create independent arrays. Get byte fields/IDs are
  copied; its extension view is the parsed authData extension copy. Direct COSE/entity/attestation
  construction borrows; opaque COSE Decode copies full input. No result-disposal API clears the
  secret-bearing copies, and session disposal/input zeroing is not end-to-end cleanup. LargeBlobKey
  is sensitive symmetric key material; signatures/certificates/public points are not private keys.
- **Parser/flags/RP hash:** full Parse/Decode helpers match documented minimum/required-field checks,
  ED suffix extraction, ignored trailing bytes without ED, missing AT allowance, nullable defaults,
  unchecked count uint32→int and largeBlobKey lengths. Extraction of one CBOR value is not full
  COSE verification. Flags inspect received bits; VerifyRpIdHash only compares SHA256(UTF8 RP ID).
  No challenge/origin/signature/account/trust/counter-policy validation or complete malformed-input
  rejection is claimed. Known-format recognition is separate from typed statement support.
- **Unsigned fields:** make0x06 captures complete encoded values borrowing the envelope; get0x08
  is skipped. Unknown outer fields are lost, no complete RawData is invented, and empty unsigned
  map is not an extension-support distinction. Signed authData extension bytes are a different layer.
- **ARKG:** actual wrapped emission/direct-map acceptance, alg-before-kty dispatch (including99),
  EC2 carrier checks, <=32 coordinate slots/left-padding, no curve=1/point verification and borrowed
  construction were traced through full bodies. RC-R1 corrects original authority, not these source
  observations. Tests establish layout/routing/roundtrip only; no crypto/profile/device qualification.
- **Core preset:** narrow source reads of ConsoleCredentialReader **184–307**, reader options and
  DisposableArrayPoolBuffer establish **UTF-16 char/code-unit** limits before default UTF8 encoding,
  not code points or bytes; disposal zeros the returned owned secret. Non-ASCII input may exceed63
  bytes. Full Unicode/current-device PIN policy remains CTAP-IMP-006/XREF-003, not fixed by prose.
- **Tests:** all changed/unchanged fixture assertions were read. Algorithm tests never assert returned
  alg/curve and optional unsupported paths return normally. Enterprise ep=false skip is qualified;
  certificate/packed assertion absent and epAtt conditional. FIPS labels do not certify a device;
  firmware early return, optional field checks and clientPin key-presence test remain. Get compares
  exact handle/status where stated, but signature merely nonempty. Make asserts fields/ID, not trust
  or later discovery; Guid.Empty also encodes16 bytes; no-auth permits four statuses, not firmware
  qualification. Integration setup normalization precedes callbacks and can consume retries/mutate
  PIN; token finally-cleanup incomplete; enterprise deletion targets all RP credentials, with no
  configuration restoration. Existing runtime diagnostics/attributes/names were left unchanged.

### Narrow G boundary check, not acceptance of other editor worktrees

Read primary `boundary-map.md` and narrow actual baseline dependencies in this checkout:
`AuthenticationResponse.cs`, `MatchedCredential.cs`, `CredentialMatcher.cs`, `UvDecision.cs` and
CTAP session make/get/next bodies. WebAuthn User/match source forwards the entity, matcher preserves
empty final ID rather than recovering the narrowed request ID, and token UV decisions set UvOption
null. The corrected C contracts are consistent with these narrower client orchestration boundaries.
WA-XREF-001 is documentation-reviewed for C, **not** a privacy-filter implementation completion;
WA-IMP-010/006/013 and CTAP-IMP-011 remain deferred. Native selected-user versus browser/RP serializer
boundary DEC-002 stays a coordinator/maintainer decision. No automatic DTO stripping is proposed.

No A/E/F stale checkout prose is accepted by this report. Their resulting source/guide/example
integration and the final two-module boundary review remain independent coordinator gates. The L3
BE/BS citation is now independently confirmed for that narrow meaning, not a mechanical upgrade
of every L2 claim or proof of supported devices.

## Complete changed-unit coverage and exact reviewed SHA256

Paths are repository-relative; `1–N` means the **complete file**, including every method/helper/assertion
and explanatory unit, not a changed-line sample. SHA256 is over actual reviewed checkout bytes.
PASS entries are scoped semantic acceptance pending coordinator token/API/compiler gates and final
integration; RC-R1/R2 entries require fresh correction/re-review before overall PASS.

| Exact path | Full range / disposition | SHA256 |
| --- | --- | --- |
| `src/Ctap2/src/Cbor/CoseKeyWriter.cs` | 1–63 PASS: value dispatch, numeric insertion/auto-sorting | `8cc48d3f05ae9abede8e9f6ed7af26ebbdb9e339dcc6b08802316b6db0911a03` |
| `src/Ctap2/src/Cbor/Ctap2SessionRequestEncoding.cs` | 1–212 PASS: both requests/options/duplicates/array helpers | `7a13e008169055255249c08fea67fb1dd035b2996220a428e299f719dd880b8b` |
| `src/Ctap2/src/Cose/CoseAlgorithm.cs` | 1–145 PASS: all identifiers, aliases, recognition/string projection | `415e84ffd2374e3412d0c82c3b9f3439d311d95991d0650ecdd87431c48b0818` |
| `src/Ctap2/src/Cose/CoseArkgP256SeedKey.cs` | 1–200 RC-R1; source/ownership/layout otherwise PASS; RC-O1 | `eeb894c603627a7198a2796f035987469a390ff16409f4c6b7bc4e864bb4bf73` |
| `src/Ctap2/src/Cose/CoseKey.cs` | 1–373 PASS: parameter parsing/carrier dispatch/all Encode variants | `029ae79ac85c717e2ae7b183dbf1f409a400c6cb6e0a49436591c16f8890b2b8` |
| `src/Ctap2/src/Credentials/AttestationFormat.cs` | 1–67 PASS: all formats/none/recognition | `8616d8f5aca1019068baab89f2cb09ef89dcce087692faacc3a42b71763435ff` |
| `src/Ctap2/src/Credentials/AttestationStatement.cs` | 1–347 PASS: all typed/opaque constructors/decoders/fields | `3324cb868c766b1af7d07d3785aab7c46ba440baa3d6995ed484932dad680a71` |
| `src/Ctap2/src/Credentials/AttestedCredentialData.cs` | 1–136 PASS: offsets/copies/bytesRead/one-value helper/errors | `b90ebb9816c2566d96e566f7349879964332f6eb24b9ddeb58fbbb11c5036141` |
| `src/Ctap2/src/Credentials/AuthenticatorData.cs` | 1–274 PASS: full parsers/properties/hash/enum/L3 citation | `b95dbc4f5299aeb0ccaed297b4dd9e2961c50e0ced079eb5c92381bd7e2041d1` |
| `src/Ctap2/src/Credentials/Ctap2CredentialOptions.cs` | 1–41 PASS: exact preset/input-layer/owner limits | `79fe7cfe2ed2c19c121ec37d3279b58d15d379462ef3b48be811e5b4f9168899` |
| `src/Ctap2/src/Credentials/GetAssertionOptions.cs` | 1–153 PASS: all properties/overloaded fluent setters | `b2b52436bc4975c2e25e3b8eab12637a1816062a41dd74ee340fa954dcf4a30a` |
| `src/Ctap2/src/Credentials/GetAssertionResponse.cs` | 1–246 PASS: full field/parser/fallback contracts; RC-O2 | `e2dac366490a696ffe833038e1280bcb92fe78dcdcb37e08fc2082d3e0188a9d` |
| `src/Ctap2/src/Credentials/MakeCredentialOptions.cs` | 1–176 PASS: all properties/setters and authorization constraints | `e2230a517910d90f9b82c454f358c706669c146c5c3e11df13dfbb695c076479` |
| `src/Ctap2/src/Credentials/MakeCredentialResponse.cs` | 1–305 PASS: memory/reader/internal Decode/output/format/getters | `d3da37b6fd4ba90c003950c984b7ec697e44f4f49fb630bba7fb9c31b6d23bed` |
| `src/Ctap2/src/Credentials/PublicKeyCredentialTypes.cs` | 1–440 PASS: descriptor/RP/user constructors/Parse/Encode | `ac31b8bb4dbdd1e51723d44512cd1b0a78ff02d42965dffcccf7e0e747aa869a` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/AuditV2/Ctap2RequestAuditReproTests.cs` | 1–44 PASS: exception/ParamName versus earlier setup I/O | `3f46fb16918aa97d55a29e58c4dd8a484bd076d6355ab1cadb8d1e1c272f1eac` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2AlgorithmSupportTests.cs` | 1–155 PASS: three routes/helper/early return/assertions/cleanup | `50b985d2592082de010d39613ed70aa4cef8e742a7677db46f3f44ba5b8b0307` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2EnterpriseAttestationTests.cs` | 1–128 PASS: capability/config/token/conditional assertion/broad cleanup | `49c03604a55b43dcf2765ede7ded2d159881db89ab687a5c58afe326bfd5e588` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2FipsComplianceTests.cs` | 1–100 PASS: all four conditional metadata bodies/assertions | `c69eeb635d8bc3bea074ce20264926772d9c2d12c865d8c7cfe8c8def4224521` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2GetAssertionTests.cs` | 1–302 PASS: three flows/exact handle/status/signature/cleanup | `0c276eb580767643c1544086614e61693d942e9548e83ab7eb4922bb0f2ac431` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2MakeCredentialTests.cs` | 1–351 PASS: all four flows/exclusion/four-status/cleanup | `213b5d57a5382e7ec470e5e353df853d78ccd55c10f1855bd9217c533be3337e` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Cbor/Ctap2SessionRequestEncodingTests.cs` | 1–228 PASS: required/all-options fixtures and full read/write helpers | `2c595804a99b7eb5c26a69afdfd656bb4e6d1fe7813aa995add614adbbeb6bce` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Cose/CoseArkgP256SeedKeyTests.cs` | 1–251 RC-R1/R2; all routing/roundtrip/dispatch assertions read | `03f8ea9dfc5dc935c9dd3568d1d0b9de9f8bebe1f9b486ec89d6acfd22b11aef` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Credentials/AuthenticatorDataTests.cs` | 1–150 PASS: all flags/prefix/counter/hash/raw; AT/ED enum-only | `254f264f9db16fbddefb39702785dd909d23458c5a6ccc6809570791cbb64196` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Credentials/CredentialResponseTests.cs` | 1–396 PASS: complete fixture builders/all parsers/fallback assertions | `517f930220231eed69c8afbed1afbfece7da405daa7e224f9f6cc20ccc3b0021` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Credentials/PublicKeyCredentialTypesTests.cs` | 1–259 PASS: all model/roundtrip/setter/assertion bodies | `06634119b36416adfaad2e93ec3e1170107bd5f88656e787533bdd56de885b77` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/MakeCredentialResponseTests.cs` | 1–81 PASS: key6/key8 assertions and synthetic map-value helper | `405ecfb6ad36af2d32f7b6ce1312f998ebdbf9323aeb986f514a8d26920f3d78` |

### Three unchanged C paths: full-unit review, not exclusions

- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/AuditV2/Ctap2RequestAuditReproTests.cs`, **1–133**:
  PASS for scoped up-false/duplicate guards and exact preference-order/false-wire assertions;
  SHA256 `281a7428d37d36a7f6dc5a401a0e94e951aa9a73f188c4239b6b17d30315508d`.
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Cose/CoseKeyForwardCompatibilityTests.cs`, **1–212**:
  PASS for modeled/opaque unknown identifier and required-field fixtures, not math/complete strictness;
  SHA256 `bbddd607fc391c225ef2b855bce7d7f022e45d1406622ae11afb2852b18f7c6f`.
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Credentials/AttestationStatementTests.cs`, **1–111**:
  PASS for explicitly dummy proofs, typed fields and exact raw equality, not certificate/signature trust;
  SHA256 `476f57fbbd06e378af18b95636c0958141798af7a57bd6bb6fe1d9c5aaffd01e`.

## Checks, snippet classification and next gate

Read root/module/test/Core/WebAuthn guidance, canonical campaign workflow/style, plan, triage/addendum,
investigation-C, evidence-C, edit-C, G boundary and framework adjudication; loaded execute-plan and
security-guidelines for the applicable read-only review/lifetime rules. Generic skill tables are not
substitutes for original protocol evidence.

Read-only shell checks in the reviewed checkout:

- `git rev-parse HEAD`: exact baseline above.
- `git diff --name-only <baseline>`: **27** paths, all within the inventory's **30 C paths**.
- Python read inventory and baseline `git show` bytes: **30/30 baseline SHA256 match**; actual
  checkout SHA256/ranges recorded for all 27 changed paths plus all three unchanged paths.
- `git diff --check <baseline>`: **exit0**, no output.
- Full30 source scan: **zero `<code>` / `<example>` / fenced snippet markers**. Existing tests
  are complete project-context fixtures, not consumer documentation snippets. Unit internal calls
  require their real friend/project context; xUnit2 hardware helpers are not xUnit3 consumer samples.
  No meaningful standalone documentation program exists in this owner set to compile.

Complete diffs show comment/XML and final-newline changes only; inspected tokens, runtime strings,
attributes, signatures, assertions and bodies remain untouched. **Formal Roslyn token/directive/API,
cref/param binding, compiler/analyzer, formatting, build/unit and final-integration checks remain the
coordinator's later gates.** No compiler/test success is asserted or borrowed from earlier reports.

Next: correct RC-R1 and RC-R2 in comments and reconcile the current ARKG handoff evidence; independently
re-review exact corrected units/hashes. Optional suggestions need no separate acceptance loop unless
their meaning changes. Reopen the relevant two source files plus evidence/dependency interpretation;
do not change ARKG behavior or silently close deferred profile/ownership/contextual decoder/PIN/test
work. Overall PASS requires those corrections and fresh re-review, followed by coordinator gates.
