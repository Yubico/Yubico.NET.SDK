# Independent review E-crypto — previewSign, ARKG and encrypted metadata

**Verdict: PASS for the 16-file documentation scope below. Required corrections: none.**
**Date:** 2026-11-04. **Reviewer:** GPT-6.1 Sol, fresh independent reviewer.
**Reviewed checkout:** `/tmp/opencode/ctap2-001-edit-e`.
**HEAD/baseline:** `5b09a9b6942ded3913b5c9340c3e126617e61a82`.

This is documentation acceptance at the exact working-file SHA-256 values below, not acceptance of
the implementation, test results, hardware qualification, production cryptography or complete draft
conformance. Reviewer E-core owns the other 30 changed E paths, including builder/identifiers/storage;
this report gives no overlapping verdict on those paths. Coordinator compilation, token/directive/API
preservation, documentation gates, clean-unit sampling and final cross-layer reconciliation remain pending.

Read root `CLAUDE.md`, Ctap2/module/test guidance, SDK house style, crypto/testing guidance, canonical
audit workflow and approved style; loaded security-guidelines and verify. Read primary `plan.md`,
`triage.md`, `triage-addendum.md`, `investigation-e.md`, `edit-e.md`, E primary excerpts and relevant C/G
investigations. The task packet's slash-compressed “plan/triage/addendum/…” names resolve to these sibling
files, not a nested directory. Independently re-fetched consequential original specifications and vendor
guidance rather than inheriting their verdicts. All 16 changed files were read in full: XML, ordinary
comments, implementations, fixtures and assertions, not just diff hunks or searches.

Only this primary report was written. No source edits, builds, tests, hardware, runtime crypto probes,
formatter, commits or delegation occurred.

## Required versus optional feedback

### Required

None. Existing behavioral deficiencies remain deferred under the primary ledger; truthful documentation
does not fix them or convert them into desirable SDK policy.

### Optional, non-blocking refinements

1. `src/Ctap2/src/Arkg/ArkgP256.cs:32–35`: “whose stated expiration is 31 August 2026” is accurate
   and does not claim current standardization. “Work in progress; revision -10 expired on 31 August
   2026” would spare readers the date calculation. Keep the pinned revision; do not upgrade to a moving
   latest draft. An active state for another revision is not renewed validity of -10.
2. `src/Ctap2/src/Extensions/PreviewSign/ArkgP256SignArgs.cs:23–41,90–109`: the internal comment correctly
   says length checks do not validate the SEC1 prefix/point. Exposing that qualification in public remarks
   would help callers distinguish an 81-byte carrier from a cryptographically valid ticket. The unchanged
   exception string attributing the 64-byte limit to prefix capacity is executable text, outside Pass 1;
   the corrected XML already identifies the real algorithm bound. Do not change that string in this pass.
3. `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2PreviewSignTests.cs:663`: the remaining
   “touch #1” comment could simply say “Register the credential with ordinary creation presence.” It
   describes an intended registration interaction, not a second-touch assertion; the class and full-flow
   remarks now explicitly deny exact gesture-count proof. No blocker or new engineering finding follows.

These are local clarity preferences, not requests for further speculative research or implementation.

## Independent original-source evidence

All URLs accessed **2026-11-04**. Quotes below are from re-fetched originals; whitespace normalized.

### P4 — previewSign version 4, not CTAP version 4

URL: <https://yubicolabs.github.io/webauthn-sign-extension/4/#sctn-sign-extension>.

- Exact title: **DRAFT: Web Authentication sign extension: Signing arbitrary data using the Web
  Authentication API. Version 4**. Header: **“Editor’s Draft, 26 August 2025”**.
- Abstract: **“This is a draft in a temporary location; the extension is intended to be upstreamed
  to WebAuthn after some more prototyping.”** This is not a finalized W3C/CTAP release or production approval.
- §10.2.1 `algorithm`: **“The algorithm identifier chosen from the algorithms input argument. This
  MAY be different from the alg (3) attribute of publicKey”**. Thus negotiated signing-op, public-seed
  marker and derived verification algorithm are not interchangeable.
- `keyHandle`: **“This value is REQUIRED but MAY be a zero-length byte array.”** The authenticator
  procedure separately says **“kh MAY be empty if the authenticator can store equivalent information
  internally.”** SDK constructor nonempty guards are correctly labeled restrictions, not universal rules.
- `tbs`: **“Depending on the signing algorithm, this may or may not need to be pre-hashed by the
  Relying Party.”** Generic encoding does not hash the supplied bytes.
- `additionalArgs`: **“If present, this MUST contain a CBOR map encoding a COSE_Sign_Args object”**.
  Authenticator CDDL uses `args => bstr .cbor COSE_Sign_Args`; raw byte passthrough is not schema validation.
- `flags`: **“Present only in the attestation object embedded within the att-obj output during
  registration ceremonies.”** Unsigned output CDDL is `att-obj => bstr .cbor attObj`. The registration
  processing creates outer algorithm output and embeds policy in the generated-key attestation.
- Authentication: **“If allowCredentialDescriptorList is empty”** the CTAP implementation returns
  **`CTAP2_ERR_INVALID_OPTION`**; missing required algorithm arguments returns **`CTAP2_ERR_MISSING_PARAMETER`**.
  Registration invalid flags returns **`CTAP2_ERR_INVALID_OPTION`** and no supported algorithm returns
  **`CTAP2_ERR_UNSUPPORTED_ALGORITHM`**. Stored signFlags are checked against the response UP/UV bits.

The draft has a contradictory output-description sentence for require-up (it says UV without UP).
The input policy, CDDL names and authenticator UP/UV bit checks support 0 = unattended, 1 = UP,
5 = UP+UV. The edits use those consistent definitions, not that isolated typo. The original draft
does not make an ARKG argument ticket a generic replacement for CTAP authorization.

### A10 — pinned ARKG -10 is expired work in progress

Original: <https://www.ietf.org/archive/id/draft-bradleylundberg-cfrg-arkg-10.html>.
Revision record: <https://datatracker.ietf.org/doc/draft-bradleylundberg-cfrg-arkg/10/>.

- Header: **“Published: 27 February 2026”**, **“Intended Status: Informational”**, **“Expires: 31
  August 2026”**. Boilerplate: **“It is inappropriate to use Internet-Drafts as reference material
  or to cite them other than as ‘work in progress.’”** The expiration has passed as of this review.
  Datatracker calls the page an older version and displays a latest-revision state; that does not
  make this pinned -10 revision active. No claim about which revision is currently latest is needed.
- §2.3: ctx is **“An octet string of length at most 64”**, **“can be a zero-length string”**;
  procedure: `if LEN(ctx) > 64: Abort with an error`, then `ctx' = I2OSP(LEN(ctx), 1) || ctx`.
  §2.5 explains protocol-message size motivation, not inability of one octet to represent 65.
- §5.1: **“with kty value TBD (placeholder value -65537)”**; parameters pkbl/pkkem have value type
  **COSE_Key**, represented as direct maps in the example. Derived dkalg example is **-9**.
- §5.2 distinguishes ARKG-P256 instance placeholder **-65700**, ESP256-split-ARKG signing placeholder
  **-65539**, and corresponding verification algorithm **-9 (ESP256)**. Signing identifiers **“SHOULD
  NOT appear in COSE structures consumed by signature verifiers.”**

Dependency inspection confirms the checkout's seed carrier uses outer kty 2 and byte-string-wrapped
nested keys in Encode, not A10's outer -65537/direct-map example. Core also retains a -09 conformance
description. The edited E wrapper correctly calls this an **SDK experimental profile**, not full -10
conformance. C owns seed/COSE documentation and validation; existing CTAP-IMP-029/DEC-003 retain that
profile decision. This report does not clear C's unedited dependency prose or adjudicate full -09/-10 parity.

### S23 — encrypted device identifier and opaque credential-store state

URL: <https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html>.
Exact status: **“Proposed Standard, February 26, 2026”**, **“published by the FIDO Alliance as a
Proposed Standard Specification.”** No automatic replacement of earlier CTAP references is implied.

- §6.4 encIdentifier **0x19**, **Optional**, is **“iv || ct”**, with **“AES-128-CBC encryption of
  (128-bit device identifier) using HKDF-SHA-256(salt = 32 zero bytes, IKM = persistentPinUvAuthToken,
  L = 16, info = ‘encIdentifier’). The encryption iv must be regenerated for each output of getInfo.”**
- §6.4 encCredStoreState **0x1E**, **Optional**, has corresponding **“(128-bit credential store
  state)”**, same derivation except info **“encCredStoreState”**, and regenerated IV for every output.
  AES's 16-byte IV plus one unpadded 16-byte ciphertext block gives the standard 32-byte field.
- §6.8.2 note: **“Platforms can use the getInfo encIdentifier to identify a specific authenticator
  and getInfo encCredStoreState to tell if the state of the credential store … has changed since
  the last time they were cached.”** This is not a credential ID, count, enumerated status or
  per-credential metadata record. §6.6 regenerates device ID/store-state random values on reset;
  stability across ordinary sessions is not immutability across reset.
- §6.4 `perCredMgmtRO:true` advertises requesting pcmr support. §6.5.5.7.2 rejects pcmr when that
  capability is absent/false or another permission is requested; its pcmr branch returns **“the
  encrypted persistentPinUvAuthToken for the specified pinUvAuthProtocol”**. §6.5 says each protocol
  maintains its own persistent token. Matching token/protocol provenance matters; ordinary token
  acquisition or a version string alone does not prove it.

### Vendor and library qualifications

<https://developers.yubico.com/Passkeys/Passkey_concepts/Security_key_capabilities/Signing_Extension_Preview.html>
states **“Early Access Only”**, **“the algorithm ID (-65539) are not final and will change before
general availability. Do not use this in production.”** Its compatibility text requires 5.8+ and
advertised previewSign. This supports preserving the nonproduction warning, not a universal earliest
firmware claim for encrypted metadata or every experimental algorithm. Its illustrative touch flow
does not override explicit flags-0/up:false requests or establish these tests' gesture counts.

Context7 was used for CBOR library documentation; the resolved API-library query returned no matches.
Official Learn `ReadByteString` retrieval selected preview-moniker metadata despite `view=net-10.0`;
therefore ownership evidence is pinned instead to actual runtime **v10.0.2** source:
<https://raw.githubusercontent.com/dotnet/runtime/v10.0.2/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor/Reader/CborReader.String.cs>.
`ReadByteString` explicitly creates `new byte[length]` and copies the input span; it is not a borrowed
slice. The same source distinguishes `ReadDefiniteLengthByteString` returning a memory view. Official
<https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.ecdsa.verifydata?view=net-10.0>
defines VerifyData as checking supplied data with the specified hash algorithm. Fixed P-256/SHA-256
and omitted ASN.1 exhaustion checks are independently visible in the SDK source, not a runtime experiment.

Research/tooling limits retained: an initial guessed CTAP 2.3 2025 URL was not used as authority;
direct urllib retrieval of the correct S23 page returned HTTP 403, then Exa retrieved the correct original
and targeted clauses successfully. An initial guessed runtime `CborReader.ByteString.cs` path returned
404; the correct pinned `CborReader.String.cs` succeeded. No conclusion depends on either failed lookup.

## Contract and assertion assessment

- **Registration output/trust:** outer signed chosen algorithm versus unsigned wrapped generated-key
  attestation versus inner policy is correctly distinguished. Decoders/extractors do not verify
  certificates, attestation, device identity, signed algorithm agreement, key binding or requested policy.
  `TryExtractGeneratedKey` catches CBOR/InvalidOperation failures only; other malformed authData failures
  may propagate. `TryGetGeneratedKey` catches broadly and can return null for malformed input.
- **Algorithm wiring:** direct DTO accessor passes seed `Algorithm` (-65700); response extractor passes
  seed `DerivedKeyAlgorithm` (typically -9). Neither derivation dispatches on that property; verification
  remains fixed P-256/SHA-256. The mismatch is candidly documented, not fixed. Negotiated -65539 has a
  separate role. Generated-key tests supply -65539 but never assert accessor propagation.
- **Generic bytes/guards:** raw Tbs and present args are unchanged; null omits key 7, empty emits empty
  bstr without making a valid CBOR-map argument. Nonempty constructor handle/Tbs/list guards can be
  bypassed with init replacements; no algorithm, flag, ceremony or negotiation enforcement is promised.
  Registration always emits flags, default 1. ARKG context guard is 64, with ticket length 81 only.
- **Lifetime:** constructors retain supplied memory/list storage; caller must keep it stable. Encoding
  and byte-string decoding allocate independent arrays. Inner attStmt is a slice of copied inner CBOR;
  authData is another copy. Derivation returns new point/ticket/context arrays but borrows device handle.
  No DTO/encoded result owner guarantees disposal or end-to-end zeroing. Public points/signatures are
  not misclassified as secret private keys; sensitive application contexts/Tbs/handles need caller policy.
- **Experimental verifier:** imports P-256, checks SEC1 shape, invokes VerifyData with SHA-256 on raw
  message. Passing the digest hashes again. Converter reads two DER INTEGERs, pads r/s into P1363,
  lacks root/sequence exhaustion and explicit positivity/range validation; framework/provider checks
  still apply. Documentation does not establish actual acceptance of a malformed signature or exploit.
- **Metadata crypto:** HKDF contexts/salt/output match S23; internal stack AES key is cleared in finally
  after HKDF. DeriveKey returns a caller-owned secret array; decrypt returns caller-owned plaintext.
  Empty token/data or <16-byte input returns null, generic IV-only/block-multiple input is accepted,
  nonempty token length is not constrained, selected crypto failures return null. CBC/no padding
  provides neither ciphertext authentication nor a correct-token verdict. Failure/provider copies are
  explicitly outside the clearing guarantee. No capability or firmware gate is falsely attributed to it.
- **Meaningful tests, bounded claims:** encoder fixtures inspect keys, copied values and one explicit
  126-byte encoding, not live Rust/Python/firmware conformance; point fixtures are synthetic. Derivation
  tests check an expected public point, retained handle/context, and two changed-input comparisons,
  not universal uniqueness, private-key security, signature validation or zeroing. CBC tests have useful
  local round trips/context comparisons and wrong-token non-null bytes, but encryption fixture keys
  use the same DeriveKey helper and do not provide an independent HKDF known-answer oracle.
- **Integration scope:** positive preview registration checks algorithm and nonempty unsigned output,
  not verified inner attestation. Full flow sends SHA256(raw), verifies raw, requests flags0/up:false
  and no second PIN/UV parameter; there is no second-touch or generic ECDH/0x7F claim. Three negatives
  accept CtapException from setup and do not prove target phase/status; TODOs now name existing status
  constants (0x26,0x2C,0x14). Metadata fixtures can return before feature assertions, do not assert distinct
  persistent tokens, and compare lengths/equality rather than opaque-state semantics. Keys/plaintext/
  tokens are not comprehensively cleared; cleanup is best-effort. No PRF, conformance or private-key
  security result is inferred from names. No test was executed by this reviewer.

Existing reopening dependencies are retained: CTAP-IMP-027/035 result/failure ownership;
CTAP-IMP-029 profile; CTAP-IMP-038 extraction; CTAP-IMP-039 propagation;
CTAP-IMP-040 guards/ceremony; CTAP-IMP-041 verifier; CTAP-IMP-043 metadata wrapper;
CTAP-IMP-010 persistent-token provenance; CTAP-IMP-020/021/022 setup/cleanup/assertion gaps;
DEC-003 product evidence, plus existing C/G and WA trust/ownership boundaries. No new duplicate bug IDs.

## Exact changed-file hashes and full-unit coverage

Paths are repository-relative to the reviewed checkout. Line ranges cover every substantive unit;
license/import/namespace boilerplate was also read. SHA-256 values hash exact on-disk bytes, not Git
blob-normalized text. Each row is documentation PASS, with the overall limits above.

| # | Path | SHA-256 | Full semantic coverage |
| --- | --- | --- | --- |
| 1 | `src/Ctap2/src/Arkg/ArkgP256.cs` | `48058d563e404bf5366808f823f5049b8d60b964679efc2213a641610f4c4e85` | 19–59: wrapper, provider delegation, expired-draft/profile warning, derivation params/result |
| 2 | `src/Ctap2/src/Crypto/EncryptedMetadataDecryptor.cs` | `0b59b00e4f2707db7bd097e767ecdbee0d6f0769f3c1bd3c1a28aa62785e7e31` | 19–171: both decrypt APIs, HKDF API, input/length/error behavior, CBC, ownership/finally |
| 3 | `src/Ctap2/src/Extensions/PreviewSign/ArkgP256SignArgs.cs` | `71ac98c97a25c9c00732e637cc30e54e003af41517c96c3ea56e8137c346072b` | 19–114: ticket/context shape, identifier/constants, borrowing, constructor checks |
| 4 | `src/Ctap2/src/Extensions/PreviewSign/CoseSignArgs.cs` | `6b21f2e468827e08b4d8628200768dc08f0bcfdc333209fa627afe2194896fa3` | 17–62: closed hierarchy, generic-vs-typed contract, algorithm/factory |
| 5 | `src/Ctap2/src/Extensions/PreviewSign/PreviewSignAuthenticationOutput.cs` | `2602394403964ce6c20b9ab5f6a02469532aa65011d2f26c42d6e3f020d06be9` | 17–46: passive signature, borrowed constructor/init, empty guard |
| 6 | `src/Ctap2/src/Extensions/PreviewSign/PreviewSignCbor.cs` | `30fd5d77ab3f9ed993503f70aa2fa0c1529b161b4e08009435dce0b2865ea78c` | 19–518: all key constants, typed bridge, three encoders, signed/unsigned/inner/auth decoders, copies/slices, extraction/error boundary |
| 7 | `src/Ctap2/src/Extensions/PreviewSign/PreviewSignDerivedKey.cs` | `34dbd93accc785097177e1a486e1f148e339b776b7fd2820c832bae1fb83b8a6` | 20–189: four properties/ctor, fixed verifier, SEC1/DER/P1363 conversion and padding |
| 8 | `src/Ctap2/src/Extensions/PreviewSign/PreviewSignGeneratedKey.cs` | `73aba3e35cf9b57f9a91cc27760da1cc6919d1374fd40d3183444817f4cc315c` | 20–144: seed/handle/algorithm, ctor, derivation/lifetimes/no uniqueness proof |
| 9 | `src/Ctap2/src/Extensions/PreviewSign/PreviewSignRegistrationInput.cs` | `1981983ac6fd3229637b48f227c2a65c47dcb55f5b4ee784265d5eafb845a3ee` | 17–70: draft status, preference/policy/list/default and ctor/init limitations |
| 10 | `src/Ctap2/src/Extensions/PreviewSign/PreviewSignRegistrationOutput.cs` | `5aad0e30eab64df67832d6f61b36923b1a0175d73c889bdff0dbd5fb06687caf` | 17–113: fields/borrowed ctor, trust separation, seed accessor/catch/propagation mismatch |
| 11 | `src/Ctap2/src/Extensions/PreviewSign/PreviewSignSigningParams.cs` | `3104b2202b862a8e17f392f0b4eea92bf9d0a599d38c4054e277853c9a182e04` | 17–91: generic Tbs/args, null/empty/schema/ownership, constructor/init guards |
| 12 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2EncryptedMetadataTests.cs` | `d6923ba8860966b3bd0c5e217046b7a4bfe94663bdc0853f91e21f9d0fc74ec6` | 25–258: all four fixtures, firmware/traits/early returns, two helpers and token selection/cleanup |
| 13 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2PreviewSignTests.cs` | `640d7e0b9608ad0528d381f7d4ed117a25caad1184060f0b984738de8f0d00d6` | 27–783: positive registration, algorithm reader, full ceremony/signature parsing, deletion helper, all three negatives/setup catches/TODOs |
| 14 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Crypto/EncryptedMetadataDecryptorTests.cs` | `0d705fac41ebfeaff52fb473b61a7148e626e15fde016a58d8039ffe2a424f4f` | 20–460: random fixture, all HKDF/empty/CBC/wrong-token/context/multiblock/64-token/short-input assertions |
| 15 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/PreviewSign/PreviewSignCborTests.cs` | `af081356266582edd129fb54ab98adbdb2d031509a1cf7a91c608e089c6bddbc` | 20–283: all eight encoder/bridge/structural/null fixtures and synthetic ticket construction |
| 16 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/PreviewSign/PreviewSignGeneratedKeyTests.cs` | `ce239eb7d6b68e6b68ef937b196644dfc7db9bfc5d579e88c39acf271ebc4288` | 21–121: fixed fixtures, all three derivation assertions, hex helper, unasserted algorithm limits |

Narrow dependencies read without their own verdict: seed COSE carrier full; Core
`ArkgPrimitivesOpenSsl.cs` full (particularly Derive/ticket construction, not a complete Core security audit);
builder preview methods/emission at 286–439; CtapStatus 80–169; integration `Ctap2TestHelpers.cs` 20–119.
No full WebAuthn source audit or E-core source verdict is represented by the C/G records read.

## Local record checks and remaining gates

Read-only `git rev-parse HEAD` returned the full baseline above. A Python enumeration of
`git diff --name-only 5b09a9b6`, filtered by `/PreviewSign/`, `/Arkg/`, `/Crypto/` and the two exact
integration filenames, returned **16 paths** and computed the hashes/line counts above. Scoped
`git diff --check 5b09a9b6 --` with those same pathspecs exited 0 with no output. These are manifest/
whitespace checks only, not token preservation, XML cref resolution or SDK compilation.

Coordinator must rerun hashes if the editor corrects any file, complete its independent executable-token/
directive/API gates and integrate E-core's separate review. Production warnings, unknown earliest
firmware/profile support and deferred behavioral/test findings must survive final reconciliation. No
successful tests, hardware, RP trust verification or private-key erasure guarantees are claimed here.
