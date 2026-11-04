# Editor E follow-up — common extension and storage prose

**2026-11-04. Status: required corrections applied; ready for E-core independent follow-up review.**
Worktree `/tmp/opencode/ctap2-001-edit-e`, HEAD/baseline
`5b09a9b6942ded3913b5c9340c3e126617e61a82`. No independent re-review PASS is claimed here.

Read the complete `review-e-core.md`, `review-e-crypto.md`, `correction-a.md` and supplemental ledger;
loaded receive-review and applied the existing root/module/workflow/style and verification instructions.
Re-read all four corrected files in full, the complete common ExtensionOutput linkage, both metadata
crypto/test files as read-only dependencies, the GetInfo parser initializer and the cap fixture.
Independently fetched the original pinned framework implementations and CTAP storage condition before
editing. Only **four E-core source paths** changed relative to the reviewed snapshot, all XML/comments.
No crypto, test, string, API, trait, assertion, directive or executable-token change occurred. No builds,
tests, hardware, runtime probes, commits or delegation. This report and the edit-record update are in
the **primary packet**.

## Required corrections verified and applied

| Review ID | Exact affected path/member | Verification and correction | Existing disposition |
| --- | --- | --- | --- |
| RE-R1 | `src/Ctap2/src/Extensions/LargeBlob/LargeBlobOutput.cs`, Written remarks | Passive nullable init property and decoder simply accept a supplied boolean; the common getter also maps a legacy boolean to Written. Removed the remaining request/success promise: null means no supplied/decoded value; true/false reflect that value, not tracked execution or independently established storage success. Standard-route distinction and TryGetLargeBlob linkage preserved. | DOC-013/017, E-03, CTAP-IMP-032 |
| RE-R2 | `src/Ctap2/src/LargeBlobs/LargeBlobData.cs`, LargeBlobEntry preservation remarks | TryDecode calls ReadInt64 only for integer map keys and known origSize. Unknown values go through ReadEncodedValue → SkipValue, whose unsigned branch uses ReadUInt64. Narrowed signed-range limitation accordingly; retained unknown values can exceed Int64 subject to reader/writer conformance. ExtensionOutput's different decode/re-encode integer limitation remains unchanged and accurate. | DOC-017, E-11, CTAP-IMP-037 |
| RE-R3 | Same file, LargeBlobArray.Deserialize parameter/remarks | Known ciphertext/nonce ReadByteString calls allocate copies. Unknown encoded keys/values retain views of serialized input and escape through `_unknownElements` for later Encode. Added the source-storage lifetime/stability obligation and explicitly denied a fully detached snapshot. Existing no-full-array/no-universal-preservation qualifications remain. | DOC-017, E-11, CTAP-IMP-037; owner/view dependencies IMP-027/034, C/G |
| RE-R4 | Same file, Deserialize exceptions | Pinned ReadStartArray/typed reads reject the wrong major type with InvalidOperationException; ReadInt64 checked conversion can throw OverflowException; malformed/nonconforming encoding gives CborContentException. Kept ArgumentException for short/hash-corrupt data and documented the three separate CBOR/range families without normalizing them or adding guards. | DOC-017, E-11, CTAP-IMP-037/028 |

## Accepted small optional refinements

- **RE-O1:** `src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs`, introductory ownership paragraph.
  Build synchronously copies retained input into encoded output. Caller keeps borrowed source stable
  through the **last Encode/Build that reads it**, then separately retains the encoded map/request
  through the awaited exchange. Clearing input does not erase the encoded copy. No lifetime/API redesign.
- **RE-O2:** `src/Ctap2/src/LargeBlobs/LargeBlobStorage.cs`, introductory authentication/cache paragraphs
  and private fragment-limit summary. Device authorization checking is conditional on configured UV or
  alwaysUv:true; SDK writes still require its supplied protocol/token. Normally cached limits remain,
  but a write with no cached maximum array size reloads GetInfo and refreshes the fragment limit too.
  No automatic public refresh API or capability/token inspection is invented.
- **RE-O3:** no broader provenance or test-comment churn. The cap fixture assertion gap is retained below
  for coordinator ledger integration; no test source changed in this follow-up.

## Independent original pinned evidence

Access date **2026-11-04**; every URL below returned HTTP 200. These are source/standard inspections,
not executed SDK or platform fixtures. Context7 `/dotnet/docs` was queried for CBOR view/copy and signed
overflow/type errors and returned **no matching documentation**; that result is not evidence. Original
release-pinned framework source supplies the relevant mechanics instead of moving API preview banners.

All CBOR files use the prefix
`https://raw.githubusercontent.com/dotnet/runtime/v10.0.2/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor/Reader/`:

| Exact original file | Independently read relevant mechanism |
| --- | --- |
| [CborReader.String.cs](https://raw.githubusercontent.com/dotnet/runtime/v10.0.2/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor/Reader/CborReader.String.cs) | ReadByteString allocates `new byte[length]`, copies the source bytes, and returns the result. Its typed header read uses expected ByteString major type. |
| [CborReader.cs](https://raw.githubusercontent.com/dotnet/runtime/v10.0.2/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor/Reader/CborReader.cs) | ReadEncodedValue snapshots `_offset`, calls SkipValue, then returns `_data.Slice(initialOffset, _offset - initialOffset)`. PeekInitialByte(expectedType) throws InvalidOperationException for a major-type mismatch; unexpected buffer end can produce CborContentException. |
| [CborReader.SkipValue.cs](https://raw.githubusercontent.com/dotnet/runtime/v10.0.2/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor/Reader/CborReader.SkipValue.cs) | Unsigned branch calls ReadUInt64; negative branch uses ReadCborNegativeIntegerRepresentation; nested arrays/maps are traversed, not forced through signed Int64. Thus an unknown value UInt64.MaxValue is not rejected merely for exceeding Int64. No such fixture was executed. |
| [CborReader.Integer.cs](https://raw.githubusercontent.com/dotnet/runtime/v10.0.2/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor/Reader/CborReader.Integer.cs) | ReadInt64 delegates to PeekSignedInteger, whose unsigned conversion is `checked((long)DecodeUnsignedInteger(...))`; out-of-range throws OverflowException. Another major type throws InvalidOperationException. |
| [CborReader.Array.cs](https://raw.githubusercontent.com/dotnet/runtime/v10.0.2/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor/Reader/CborReader.Array.cs) | ReadStartArray calls PeekInitialByte(expectedType:Array); indefinite array encoding forbidden by its conformance mode throws CborContentException. This distinguishes a valid wrong root type from malformed/nonconforming encoding. |

Independently fetched [CTAP 2.3 Proposed Standard, 26 February 2026](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html).
§6.10.2 conditions write authentication on **“the authenticator is protected by some form of user
verification or the alwaysUv option ID is present and true”**, then verifies pinUvAuthParam and checks
lbw permission. That confirms RE-O2's conditional device check, not universal token enforcement.
The legacy Written carrier is distinguished from §12.4's unsigned direct-route result based on its
actual source, not by transferring that standard's success semantics to this passive DTO.

## COORD-001 metadata dependency — crypto preserved

Rechecked `AuthenticatorInfo.cs`: optional encrypted-field locals are `byte[]?`, initialized null;
result initializer assigns them directly to nullable ReadOnlyMemory properties. Independently fetched
[ReadOnlyMemory.cs at runtime v10.0.0](https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/ReadOnlyMemory.cs):
null-array constructor produces default memory; the implicit array operator calls that constructor;
Length returns `_length`. Together with A's recorded prior pure-language evidence, this supports
**absent encoded fields → present empty memory**, not nullable absence. No language/SDK probe was rerun.

The E decryptor XML does **not** promise that HasValue means field availability. It describes the
standard optional fields separately from generic borrowed spans, capability checks and empty-input
null returns. The integration XML also does not advertise its HasValue guards as feature proof.
Those executable guards/assertions remain incomplete: absent GetInfo fields can pass HasValue checks,
lead to token work, and then yield null decryption/early return or a later assertion failure. Preserve
this under **COORD-001 / A-006 / CTAP-IMP-001**, with E-19/IMP-043/010 and test-gap IMP-022 dependencies.
No count of a completed metadata ceremony follows from the fixture name or nullable presence alone.

**All 16 crypto-reviewed files are unchanged at review-e-crypto.md's PASS hashes.** No crypto re-review
is required because of edits in this correction batch. Its accepted -65539/-65700/-9 distinction is
preserved; draft-10 explicitly gives -65700 an ARKG instance placeholder role. No contrary unsupported-
identifier claim was added, and no accepted crypto prose was churned for that C-review clarification.

## Cap fixture assertion gap for the ledger

`src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/LargeBlobs/LargeBlobSpecFormatTests.cs`,
Entry_OrigSizeAboveTheCap_IsNotDecrypted: Encrypt creates ciphertext/AAD for BlobA.Length, then the
fixture replaces OriginalSize with MaxOriginalSize+1 without re-encrypting. Source TryDecrypt returns
at the cap check first, but the null assertion alone does not isolate that branch: removing the cap
would still leave mismatched origSize/AAD and authentication failure. Retain under **CTAP-IMP-022/037**;
a later authorized meaningful regression should distinguish cap rejection from AEAD rejection. No
assertion, fixture, test comment or ledger file was edited here; no cap experiment was run.

## Refreshed exact hashes and checks

| Follow-up changed path | Reviewed SHA-256 (preserved provenance) | New on-disk SHA-256 |
| --- | --- | --- |
| `src/Ctap2/src/Extensions/LargeBlob/LargeBlobOutput.cs` | `7d7a53faaa3664e2ed367bb6244a209ec98aa4b38a6c37c6175bf1da9a807daf` | `4ecd8e986d54394ecf3d735f464b732ffba985f0a44e5fa636f2ecc5da9d9f96` |
| `src/Ctap2/src/LargeBlobs/LargeBlobData.cs` | `399f4c57bc4e53be4685f49545a2588d45871b86df422cc37907db9dd6df53b2` | `1e8b6b3c5620246a89f36fd02d533cf7f77189a76dfd079470826ad3f43916f6` |
| `src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs` | `798af9e41ddb8e9c813e70343981b93fee14f42ce6be423bf04322ca21b15aac` | `b2867e349c5e6232461ac869052dc086947786f5f7baa6da87f5bff14255a345` |
| `src/Ctap2/src/LargeBlobs/LargeBlobStorage.cs` | `28d739cd8ebee33f55f6e5a2e49aeffc1c288f73b9802156bd0dbeaaaefabf36` | `b94c0c18a83700b75d2d122af35a12cc2d2759905c90734613396ee3c8597bfc` |

Fresh read-only checks from the E worktree: `git diff --check` exit 0/no output. Python/Pygments
comparison against `git show HEAD:<path>` verifies all **57 E non-comment token streams and directives**
equal baseline; **46 total changed paths** remain within E ownership. ElementTree parses **223 XML
groups** with zero failures. Parsed both review manifests (30 core +16 crypto); all **16 crypto** and
the other **26 core** hashes still match. Exactly the four rows above differ from reviewed snapshots.
Verification exit 0; this is not compiler cref/API/analyzer/snippet compilation or SDK execution.

New complete worktree `git diff --binary` SHA-256:
`cd4d40b22f2af01ef82e890932851e1c84353e969b4a1bda9c634e72922989b5`.
The two XML code blocks and their typed prerequisites/classifications are unchanged; only the builder's
adjacent borrowing lease was clarified. Independent E-core re-review must cover RE-R1..R4 and the two
accepted optional paragraphs at these hashes. Coordinator integration/final gates and implementation/
hardware statuses remain separate and pending.
