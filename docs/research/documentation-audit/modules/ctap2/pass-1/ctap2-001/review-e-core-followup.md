# Independent E-core follow-up review

**Verdict: PASS for the 30-path E-core documentation scope at the final hashes below. Remaining required corrections: none.**

**Date/access date:** 2026-11-04. **Reviewer:** GPT-6.1 Sol, same independent E-core reviewer, not the editor. **Worktree:** `/tmp/opencode/ctap2-001-edit-e`. **HEAD/baseline:** `5b09a9b6942ded3913b5c9340c3e126617e61a82`.

This follow-up resolves RE-R1–R4 from [review-e-core.md](review-e-core.md) and accepts the authorized concise RE-O1/O2 refinements recorded in [correction-e.md](correction-e.md) and [edit-e.md](edit-e.md). It supersedes the initial core review's pending verdict, not its evidence, full-unit coverage or deferred implementation findings. No new optional edit is requested. RE-O3's cap-fixture assertion gap is retained for the coordinator's ledger, not fixed or converted into a passed regression.

**No source edits, builds, tests, hardware operations, runtime probes, commits or delegation occurred. Only this primary-packet report was written.** PASS means documentation acceptance after static re-review, not implementation completeness, full protocol compliance, end-to-end secret erasure, compiled snippets or hardware qualification. Coordinator validation, clean-unit sampling and final integrated cross-layer reconciliation remain separate.

## Follow-up read scope

The root/module/test instructions, canonical workflow/style, SDK house style and applicable security/verification guidance loaded in the initial review remain applicable. Read the current correction record, edit-record follow-up and crypto review record. Re-read these **four corrected source files in full**, including unchanged bodies and adjacent properties/comments:

- `src/Ctap2/src/Extensions/LargeBlob/LargeBlobOutput.cs:1–107`.
- `src/Ctap2/src/LargeBlobs/LargeBlobData.cs:1–459`.
- `src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs:1–528`, including both previewSign overloads, encoding/copy helpers, HasExtensions and CountExtensions.
- `src/Ctap2/src/LargeBlobs/LargeBlobStorage.cs:1–521`, including constructors, cache loading, every operation, fragments/authentication and response parser.

Also re-read complete `src/Ctap2/src/Extensions/Shared/ExtensionOutput.cs:1–375` as the legacy Written/raw-decoder linkage, and `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/LargeBlobs/LargeBlobSpecFormatTests.cs:169–180` for the complete cap-fixture method/assertion. The other 26 core files retain their initial complete-file semantic review; this follow-up independently verifies unchanged hashes rather than claiming a second complete read of them. All 16 crypto source/test files were **hash-checked only**, not re-reviewed. Their independent PASS remains owned by [review-e-crypto.md](review-e-crypto.md).

## Required correction dispositions

| Review item | Full affected unit and actual implementation | Independent disposition |
| --- | --- | --- |
| **RE-R1 — Written** | `LargeBlobOutput.cs:56–63,70–105`: nullable init property defaults to null; decoder sets it only from a supplied `written` boolean. `ExtensionOutput.cs:168–199` still maps a synthetic boolean to Written or decodes the legacy map, without request tracking. | **PASS.** Remarks now say null means no supplied/decoded value and true/false reflect the supplied boolean, not tracked execution or independently established storage success. The summary/class route distinction remains consistent; no direct-route success guarantee leaks into this DTO. |
| **RE-R2 — range/preservation** | `LargeBlobData.cs:38–44,208–270,185–202`: integer map keys and known origSize pass through ReadInt64. Unknown values retain ReadEncodedValue slices and later WriteEncodedValue emits them subject to canonical conformance. | **PASS.** New remarks limit origSize/integer keys to signed Int64 and explicitly allow encoded unknown unsigned values beyond Int64. This is not universal byte-exact foreign-array preservation. It correctly differs from ExtensionOutput's reconstructive Int64 path. |
| **RE-R3 — Deserialize lifetime** | `LargeBlobData.cs:353–393` plus TryDecode: known ciphertext/nonce use ReadByteString copies; unknown keys/values use source-backed encoded slices stored in `_unknownElements` and subsequently serialized. Constructor/Entries init also borrow their supplied storage/list. | **PASS.** The data parameter now requires storage alive/unchanged while retained unknowns may be used/serialized; remarks distinguish copied known byte strings from borrowed unknown slices and deny a fully detached snapshot. Existing modeled-array and no-universal-preservation qualifications remain. |
| **RE-R4 — exceptions** | `LargeBlobData.cs:357–367` and Deserialize/hash/TryDecode bodies: short/hash-corrupt input throws ArgumentException; wrong major type can escape as InvalidOperationException; checked ReadInt64 range failure can escape as OverflowException; malformed/nonconforming CBOR can escape as CborContentException. | **PASS.** All four error families are locally described without normalizing them or adding guards. Required fields with recognized but wrong value shapes can still be skipped/rejected as nonconforming entries; the prose does not promise every such case throws. No complete-envelope validation claim was added. |

## Authorized optional refinements

- **RE-O1 — PASS:** `ExtensionBuilder.cs:24–32` now separates the borrowed source lease through the **last Encode/Build that reads it** from the encoded map/request lease through awaited transmission. Build is synchronous and returns an independent encoded array (`:328–338`); subsequent input mutation cannot erase that copy, but reuse of the builder still reads retained inputs. The paragraph preserves absent negotiation/ceremony checks and incomplete salt/authentication cleanup. The composable XML snippet is unchanged; no new ceremony or cleanup guarantee is implied.
- **RE-O2 — PASS:** `LargeBlobStorage.cs:33–55,114–133,173–202` now conditions device token-authorization checks on configured UV or alwaysUv:true while retaining the SDK's stricter supplied-protocol/token write route. Cache prose and private summary now match both branches: GetMaxFragmentLengthAsync normally uses its cached value; WriteLargeBlobArrayAsync reloads GetInfo when `_maxSerializedLargeBlobArray` is null, and LoadLimitsAsync refreshes **both** fields. No public refresh API, token-permission inspection or capability preflight was invented. Re-read the write size gate, fragment loop and authentication call; no transaction, conflict detection, rollback or resume claim was added.
- **RE-O3 — recorded, not implemented:** `LargeBlobSpecFormatTests.cs:172–177` encrypts using BlobA.Length, then reuses the ciphertext/nonce with a changed OriginalSize above MaxOriginalSize. TryDecrypt currently exits at its cap first, but removing the cap would still leave an AAD mismatch and authentication rejection. Its null assertion cannot isolate the cap branch. [correction-e.md](correction-e.md) retains this exact confounding gap under existing **CTAP-IMP-022/037** for coordinator ledger use. The test hash is unchanged; no assertion/comment/fixture edit or experiment occurred.

## Original evidence independently rechecked

Original-source fetches below returned **HTTP 200 on 2026-11-04**. No library/SDK code was executed. The initial independent review's CTAP2.1/2.2/2.3 publication/actor/strength evidence remains authoritative for unchanged contract groups; this follow-up re-fetches the consequential corrected mechanisms rather than inheriting the editor's verification verdict.

### Release-pinned .NET CBOR source

Context7 `/dotnet/docs` was queried again for ReadEncodedValue/ReadByteString ownership and signed-range/type errors. This time it returned generic .NET Core 2.1 Span/Memory view guidance, **not matching CBOR evidence**. It was not used to prove these APIs or .NET10 behavior. The original release-pinned **dotnet/runtime v10.0.2** files supply the relevant mechanics, avoiding moving Learn prerelease banners:

| Original URL | Independently inspected mechanism |
| --- | --- |
| [CborReader.String.cs](https://raw.githubusercontent.com/dotnet/runtime/v10.0.2/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor/Reader/CborReader.String.cs) | ReadByteString allocates `new byte[length]`, copies the source payload and returns the new array. Known deserialized ciphertext/nonce are copied. |
| [CborReader.cs](https://raw.githubusercontent.com/dotnet/runtime/v10.0.2/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor/Reader/CborReader.cs) | ReadEncodedValue snapshots offset, calls SkipValue, returns `_data.Slice(initialOffset, _offset - initialOffset)`. PeekInitialByte(expectedType) throws InvalidOperationException for a major-type mismatch. Unknown stored encoded values borrow source memory. |
| [CborReader.SkipValue.cs](https://raw.githubusercontent.com/dotnet/runtime/v10.0.2/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor/Reader/CborReader.SkipValue.cs) | Unsigned branch calls **ReadUInt64**, negative branch uses ReadCborNegativeIntegerRepresentation. Unknown encoded values do not pass through signed Int64 reconstruction. |
| [CborReader.Integer.cs](https://raw.githubusercontent.com/dotnet/runtime/v10.0.2/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor/Reader/CborReader.Integer.cs) | PeekSignedInteger uses checked unsigned-to-long conversion; out of range produces OverflowException. Wrong major type produces InvalidOperationException. This applies to known origSize/integer-key branches. |
| [CborReader.Array.cs](https://raw.githubusercontent.com/dotnet/runtime/v10.0.2/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor/Reader/CborReader.Array.cs) | ReadStartArray uses expected Array major type, while prohibited indefinite-length encoding throws CborContentException. A valid wrong root type and malformed/nonconforming encoding are distinct error mechanisms. |

**Important distinction preserved:** `ExtensionOutput.cs:328–335` reads all reconstructed integers using ReadInt64/WriteInt64, including unmodeled extension values. LargeBlobEntry's unknown-value path instead borrows encoded slices through ReadEncodedValue. Its revised range qualification does not relax or contradict ExtensionOutput's existing reconstruction limitation. Neither is a universal unknown-data/conformance/immutable-storage guarantee.

### Original CTAP storage/direct-output clauses

Re-fetched [CTAP 2.3 Proposed Standard, 26 February 2026](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html):

- **§6.10.2, authenticator condition:** “If the authenticator is protected by some form of user verification or the alwaysUv option ID is present and true”, then require/verify pinUvAuthParam and check **lbw** permission. The condition belongs to the device algorithm; the helper does not inspect the opaque token. New RE-O2 wording preserves that distinction.
- **§6.10.2, platform MUST NOT:** do not attempt a serialized array write exceeding reported maxSerializedLargeBlobArray. Source checks a cached/reloaded nonnull report; missing-report robustness and sequence decisions remain deferred, not silently certified.
- **§6.10.3:** required map fields/types conform; unknown elements **MAY** exist. This permits modeling unknown values but does not promise this SDK handles every key/range/encoding or preserves a full foreign envelope.
- **§6.10.4 note, platform SHOULD:** limit origSize, maximum **SHOULD be at least 1 MiB**. The SDK's 1 MiB read cap and uncapped Encrypt asymmetry remain documented; the cap fixture is not evidence isolating that choice.
- **§12.4:** direct largeBlob is the mutually exclusive alternative; outputs are **unsigned**, with compressed blob/originalSize for reads and a written result for a write attempt. This does not give a synthetic passive Written carrier tracked-request/success semantics.

The existing canonical-writer sorting adjudication remains unchanged. No insertion-order defect, crypto/profile re-review or new device support claim is inferred from this follow-up.

## Exact final E-core SHA-256 manifest

Hashes are current **on-disk worktree bytes**. Refreshed rows were completely re-read in this follow-up; unchanged rows retain initial full-file review and passed an independent hash comparison. This table is the final 30-core-path identity checkpoint, not a claim all 30 were freshly re-read twice.

| # | Exact repository-relative path | Final SHA-256 | Since initial core review |
| --- | --- | --- | --- |
| 1 | `src/Ctap2/src/Extensions/CredBlob/CredBlobAssertionOutput.cs` | `ade9525c77e8a68f5b7e987fe195721f79fcd3968db11118301277851d914e0d` | Unchanged |
| 2 | `src/Ctap2/src/Extensions/CredBlob/CredBlobInput.cs` | `3478e31f215195a7bae5fc64bfa2e01b2dc3f753136f14c823bcb4b7d4dd14af` | Unchanged |
| 3 | `src/Ctap2/src/Extensions/CredProtect/CredProtectPolicy.cs` | `c8a6bd98a891da4c0cd1c8abc2722c8921f8164fa511a5097d0c689d05b83f36` | Unchanged |
| 4 | `src/Ctap2/src/Extensions/HmacSecret/HmacSecretInput.cs` | `97da81eeecafbf1ba3740d17eeec9024da93e2c4229feb9d355f16fa24294db1` | Unchanged |
| 5 | `src/Ctap2/src/Extensions/HmacSecret/HmacSecretOutput.cs` | `69f58af004730daed11c3c1230aca3d3fb1d869257dd0b15e65854aebee4afb2` | Unchanged |
| 6 | `src/Ctap2/src/Extensions/LargeBlob/LargeBlobOutput.cs` | `4ecd8e986d54394ecf3d735f464b732ffba985f0a44e5fa636f2ecc5da9d9f96` | Refreshed; RE-R1 PASS |
| 7 | `src/Ctap2/src/Extensions/MinPinLength/MinPinLengthInput.cs` | `18dab402040c214747974ecf765bf060232aae2a58ac98714eedebeffce84fa8` | Unchanged |
| 8 | `src/Ctap2/src/Extensions/MinPinLength/MinPinLengthOutput.cs` | `394855a32cc005dd4bcf8674983f111e3ccd43ce86411b6ede274ac93947f4ef` | Unchanged |
| 9 | `src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs` | `b2867e349c5e6232461ac869052dc086947786f5f7baa6da87f5bff14255a345` | Refreshed; RE-O1 PASS |
| 10 | `src/Ctap2/src/Extensions/Shared/ExtensionIdentifiers.cs` | `2a3cae362c92844616a2b9a81a0252e1aa93dbd12c71fa2e63431821bdbea766` | Unchanged |
| 11 | `src/Ctap2/src/Extensions/Shared/ExtensionOutput.cs` | `3d55d3de7f110976d4f34b9304e9279114bb326f9ea6f5ae5449ab0ede95f128` | Unchanged; full linkage re-read |
| 12 | `src/Ctap2/src/LargeBlobs/ILargeBlobStorage.cs` | `df57c7cce57b9c061b8e0188f251ee3974742924f9c565c0b8acebfe1cad8ad4` | Unchanged |
| 13 | `src/Ctap2/src/LargeBlobs/LargeBlobData.cs` | `1e8b6b3c5620246a89f36fd02d533cf7f77189a76dfd079470826ad3f43916f6` | Refreshed; RE-R2/R3/R4 PASS |
| 14 | `src/Ctap2/src/LargeBlobs/LargeBlobStorage.cs` | `b94c0c18a83700b75d2d122af35a12cc2d2759905c90734613396ee3c8597bfc` | Refreshed; RE-O2 PASS |
| 15 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/AuditV2/Ctap2LargeBlobAuditReproTests.cs` | `1a3fed0d41774a55f0ca2962baeb33332be05e937ba79a96d9dee59cfc2e9ef1` | Unchanged |
| 16 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2CredBlobTests.cs` | `95aa133270dce515f5f978dba281e2c76b41dedbf321a0fdc866a020d9d5884f` | Unchanged |
| 17 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2CredProtectTests.cs` | `f5851feeaddfb17d0c2e4abed52d02f5cef564a775a743a4b64dd274d093b716` | Unchanged |
| 18 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2HmacSecretTests.cs` | `a69a1e24a36ca82c648857d98393f80be21eb95cfee1327cfeb64d76b0408791` | Unchanged |
| 19 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2LargeBlobTests.cs` | `7b083402956725eeb2227708266a19d11258e4b5b7b301fc6173abe101fb87fa` | Unchanged |
| 20 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2MinPinLengthTests.cs` | `a71aeae5892645103619ee801c3652957256198a871b31c5699296f9ed084c8c` | Unchanged |
| 21 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2PrfTests.cs` | `ba873c8ed3b7bb3b4ddd795b6037b1274c69444a0ea5b431ce13dcaa729e9ba4` | Unchanged |
| 22 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/AuditV2/Ctap2ExtensionAuditReproTests.cs` | `3be14804d9f03d61b218f860f732f6ca27eaa2d8fd06e9f7cf879f87ff96b17a` | Unchanged |
| 23 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/AuditV2/Ctap2LargeBlobAuditReproTests.cs` | `6af977050992913b6117852f863670812a88482c8a9b7dc8a5ab04c40b0c9074` | Unchanged |
| 24 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/HmacSecret/HmacSecretInputTests.cs` | `4f8e980e0d292ce17d840da6ea098d98de884338b34fb645aa0159266bf193a3` | Unchanged |
| 25 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/HmacSecret/HmacSecretOutputTests.cs` | `621e9a94b32d421609a319ab55bf502723b344a95f088add1d9b80680b95a6e2` | Unchanged |
| 26 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/LargeBlob/LargeBlobOutputTests.cs` | `9841152f0eb7778c74c3c06011c07f3def0b48aeb1939439bc326c7cf8aafd0e` | Unchanged |
| 27 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/Shared/ExtensionBuilderTests.cs` | `35fd0b52d2868950e8f7f0bef1ede3edebc7eaf3be9ffd5811e99a04373738a3` | Unchanged |
| 28 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/Shared/ExtensionOutputTests.cs` | `d0c931ea75d90e8bc6ebae1348688047c000e3c2fa4329d9557af46d3ee95660` | Unchanged |
| 29 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/LargeBlobs/LargeBlobSpecFormatTests.cs` | `62db01f24e0bdb2d574cab2c74d5ab35ac174b97731c462e48ac5ae2007173f3` | Unchanged; cap method re-read |
| 30 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/LargeBlobs/LargeBlobStorageTests.cs` | `f257127c73d816fe385c1aec8faa6c9c2a47911292e634886c5e67602147812f` | Unchanged |

## Preservation checks and limits

Fresh read-only Python checks parsed the initial 30-core and 16-crypto review manifests, recomputed SHA-256 from every worktree file, and checked the complete baseline changed-path set. **Exit 0:** exactly the four corrected core hashes differ from the initial core checkpoint; the other **26 core hashes match**, and **all 16 crypto hashes match their existing independent review**. The 30+16 path union equals the entire 46-file E change set. HEAD independently remains the full baseline above. No crypto semantic re-review was triggered or performed.

The editor's exact `git diff --binary` hash was independently reproduced as:
`cd4d40b22f2af01ef82e890932851e1c84353e969b4a1bda9c634e72922989b5`.
For command provenance, `git diff --binary 5b09a9b6` produces a different stream hash (`5c04732723392e1f193e1eddb7ffdfb484346e83f9cea2964679a055e168cf51`): this checkout uses `i/` versus `c/` source prefixes for index-versus-commit comparison. It is not an unexplained source-hash change; both exact commands were checked.

Coordinator token/directive/API/compiler/analyzer/XML-cref/snippet/format/architecture/complexity checks are not replaced by this review or the editor's reported lexer/XML checks. No new source-preservation gate, build result or test result is claimed here. The initial 30-path full-unit review and final per-file identity checks jointly support this bounded documentation PASS.

Existing deferred mappings remain: DOC-013/017 and E-03/11; IMP-027/028/032/034/035/036/037; test setup/cleanup/assertion IMP-020/021/022; secret-export IMP-042; B/C/F/G and WA ownership/extension dependencies. The cap-fixture gap remains coordinator ledger work, and COORD-001 metadata/GetInfo absence findings are outside this core correction's semantic re-review. Correcting these comments does not fix any decoder, owner, token policy, shared-array sequence, crypto profile or test assertion.
