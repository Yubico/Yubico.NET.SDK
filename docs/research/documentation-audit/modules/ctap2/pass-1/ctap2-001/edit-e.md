# Editor E — extension, storage and crypto documentation batch

**Status:** crypto 16-file documentation review PASS at unchanged hashes; E-core RE-R1..R4 corrected,
with RE-O1/O2 refinements, and ready for independent follow-up. Coordinator integration/final gates pending.
**Editing dates:** 2026-11-03–04; final local checkpoint 2026-11-04. **Editor:** GPT-6.1 Sol.
**Worktree:** `/tmp/opencode/ctap2-001-edit-e`.
**Baseline:** `5b09a9b6942ded3913b5c9340c3e126617e61a82` (HEAD remains unchanged).

Read root/module/tests CLAUDE, available skills, canonical workflow/style, SDK house style, testing guidance,
the primary plan/inventory/triage, investigations E/B/C/G/A, E primary excerpts and framework adjudication.
Loaded security-guidelines for semantic ownership distinctions and verify for local checks. Read all 57
E-owned source/test paths as complete substantive units before editing, including implementations,
assertions and fixtures; license/import boilerplate was also inspected. This report is in the **primary
packet**, not only the isolated worktree.

**46 E-owned C# paths changed; 11 remain unchanged.** Changes are comments/XML only. No neighboring
credential, PIN, Core, WebAuthn, module Markdown, API snapshot or project file was edited. No executable
tokens, directives, APIs, assertions or strings changed. No build, test, hardware operation, commit,
delegation, formatter or runtime probe was performed. This is an editing checkpoint, not documentation
closeout, a fixed implementation, a passing SDK suite or hardware qualification.

## Accepted corrections and implementation boundaries

Primary groups are **CTAP-DOC-013 / CTAP-DOC-017**. Shared qualifications also support DOC-003/004/005/009/
012/015/016. Report-local E IDs below retain exact provenance until the coordinator assigns supplemental
implementation/decision IDs; none is invented here. All implementation and assertion work remains deferred.

| Provenance | Applied contract / deferred re-review trigger |
| --- | --- |
| E-01 | credProtect requests an integer; enforcePolicy remains inert. CTAP's conditional platform SHOULD NOT rule is not a wire flag. Preflight matters when creation must be prevented; inspection afterward cannot undo it. Reopen builder/policy and C/F/G creation claims after an enforcement-boundary decision/fix. |
| E-02 | Device capacity is maxCredBlobLength ≥32, not a fixed 32-byte maximum. DTO's 1–32 guard is SDK-only; builder has no size gate. Sensitive/PII content requires RP level-3 protection + enforcement. Reopen input/builder/outputs and F/G examples after size/empty/capability policy changes. |
| E-03 | Standard largeBlobKey is response envelope 5/7, not signed extension output. Direct largeBlob is a distinct unsigned compressed/originalSize route; legacy combined DTO/maps are not that route or WebAuthn's decompressed output. No nonexistent WithLargeBlob or all-device absence claim added. Reopen C envelope/G/F output guides after direct-route or convenience-model changes. |
| E-04 | minPinLength discloses the current code-point minimum to configured minPinLengthRPIDs at creation; no output is not zero. It does not set policy, check the current PIN or attest all complexity. Reopen with D configuration and G adapter changes. |
| E-05/06 | Random credential secrets have UV/non-UV variants. Exact salt/output ciphertext sizes are 32/64 (V1), 48/80 with IV (V2); tags 16/32. TryGetHmacSecret is assertion-byte-string only; registration boolean/MC need raw value decoding. Reopen on B negotiation/validation or ceremony-appropriate accessor changes. |
| E-07/08 | Extension convenience values are reconstructed, not byte-exact raw slices; unsupported CBOR/large uint/duplicates and identifiers-only reader limits are explicit. Parent raw guarantees are separate and not weakened. Borrowed inputs, copied outputs and absent cleanup owners are distinguished. Reopen jointly with C/G raw-envelope and owner/view redesign. |
| E-09/10 | Storage borrows session/protocol/token; limits are instance-cached; full reads/sequential chunks and full-array RMW have no sequence lock, conflict detection, transaction, rollback or resume. SDK write-token policy is stricter than conditional device UV. Re-read after errors/cancellation. Probe/request/compression copies lack end-to-end cleanup. Reopen after sequencing/recovery/limits/ownership changes. |
| E-11 | Preserve raw DEFLATE/GCM/AAD/hash rationale. 1 MiB is SDK read cap, not universal protocol maximum; write asymmetry remains. Known maps are reconstructed, nonconforming entries dropped, foreign unknown preservation bounded, UInt64/Int64 limitations explicit. Reopen after cap/full-uint/encoded-slice/foreign-entry policy changes. |
| E-12 | previewSign is Yubico Labs v4 Editor's Draft, not CTAP4. ARKG-10 is cited as work in progress with stated expiry 2026-08-31; SDK profile/algorithm roles are not full conformance or production guidance. Unsupported all-family/earliest-firmware claims removed, not replaced with guessed floors. Reopen with pinned vendor/profile/publication evidence. |
| E-13/14 | Signed chosen algorithm versus unsigned wrapped attestation and inner flags are distinct. Extraction is not trust verification. Accessors' derived-algorithm propagation disagreement remains candid; -65539/-65700/-9 roles distinct. Reopen after extraction, propagation, attestation or algorithm-dispatch changes. |
| E-15/16 | Generic Tbs/args passthrough is not schema/algorithm validation; constructor-only guards and mixed-builder precedence are SDK limitations. Handle/context borrowing versus encoded copies explicit. Fixed raw-message SHA256/P256 verification and incomplete DER validation remain experimental. Reopen after guard/encoding/verifier changes. |
| E-17/18 | Comments describe conditional/positive/count-only/synthetic assertions, not deterministic PRF, no-UV rejection, all-protocol crypto, full zeroing or live sibling interoperability. Existing status constants replace false TODO prerequisites; stronger assertions and setup-isolation remain TODOs. Secret export is exact opt-in file/environment behavior, not safe persistence. Reopen after test/harness/secret-channel changes and separately recorded hardware evidence. |
| E-19 | GetInfo encrypted device ID/opaque store state uses IV16+cipher16 and matching PPUAT. Generic helper acceptance is not strict field/token validation. CBC is unauthenticated, null is not wrong-token proof. Returned plaintext/DeriveKey secret arrays are caller-owned; cleanup is bounded. Reopen with A/B/D/F/G after field/token/freshness/ownership changes. |

The canonical writer auto-sorts at map completion; comments now distinguish insertion from emitted order.
No runtime insertion-order bug is claimed. Framework adjudication and rejected IMP-017 provenance remain
authoritative. No unsupported firmware error phase, ECDH-reset/0x7F rule or exact touch count is asserted.

## Exact changed-file manifest

Each path below is repository-relative and refers to the isolated worktree diff. Mapping is to E provenance;
the accepted canonical documentation groups above apply throughout.

| Changed path | Mapping |
| --- | --- |
| `src/Ctap2/src/Arkg/ArkgP256.cs` | E-12; experimental profile/draft expiry, delegated provider |
| `src/Ctap2/src/Crypto/EncryptedMetadataDecryptor.cs` | E-19/E-08; semantics, generic lengths, CBC/authentication, key/plaintext ownership |
| `src/Ctap2/src/Extensions/CredBlob/CredBlobAssertionOutput.cs` | E-02/08; empty vs absent, init borrowing vs decoded copy |
| `src/Ctap2/src/Extensions/CredBlob/CredBlobInput.cs` | E-02/08; capacity/SDK guard, sensitive prerequisites, encoded ownership |
| `src/Ctap2/src/Extensions/CredProtect/CredProtectPolicy.cs` | E-01/12; requested/effective policy, normative strength, defaults |
| `src/Ctap2/src/Extensions/HmacSecret/HmacSecretInput.cs` | E-05/08; random/UV secrets, exact sizes, borrowing/validation |
| `src/Ctap2/src/Extensions/HmacSecret/HmacSecretOutput.cs` | E-05/06/08; shapes/sizes/copies |
| `src/Ctap2/src/Extensions/LargeBlob/LargeBlobOutput.cs` | E-03/08; legacy synthetic shape and output-layer/ownership truth |
| `src/Ctap2/src/Extensions/MinPinLength/MinPinLengthInput.cs` | E-04; authorized disclosure, requested boolean |
| `src/Ctap2/src/Extensions/MinPinLength/MinPinLengthOutput.cs` | E-04; code points/current policy/parser range |
| `src/Ctap2/src/Extensions/PreviewSign/ArkgP256SignArgs.cs` | E-08/12/15; borrow vs encode copy, context bound/point-validation limits |
| `src/Ctap2/src/Extensions/PreviewSign/CoseSignArgs.cs` | E-12/15; separate draft/experimental hierarchy |
| `src/Ctap2/src/Extensions/PreviewSign/PreviewSignAuthenticationOutput.cs` | E-08/12; passive borrowed/copied unverified bytes |
| `src/Ctap2/src/Extensions/PreviewSign/PreviewSignCbor.cs` | E-08/12/13/14/15; wrapped maps, inner policy, extraction/error/ownership |
| `src/Ctap2/src/Extensions/PreviewSign/PreviewSignDerivedKey.cs` | E-08/12/16; ownership, fixed raw-message verification/DER limits |
| `src/Ctap2/src/Extensions/PreviewSign/PreviewSignGeneratedKey.cs` | E-08/12/14; extraction trust/algorithm discrepancy, derivation/ownership bounds |
| `src/Ctap2/src/Extensions/PreviewSign/PreviewSignRegistrationInput.cs` | E-08/12/15; draft status/policy/borrowing/guards |
| `src/Ctap2/src/Extensions/PreviewSign/PreviewSignRegistrationOutput.cs` | E-08/13/14; passive output/trust/algorithm propagation |
| `src/Ctap2/src/Extensions/PreviewSign/PreviewSignSigningParams.cs` | E-08/15; raw/digest/args schema, constructor guards/borrowing |
| `src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs` | E-01..06/08/09/12/15; complete input/build contract, corrected XML example/order |
| `src/Ctap2/src/Extensions/Shared/ExtensionIdentifiers.cs` | E-03..05/12; output routes and publication scopes |
| `src/Ctap2/src/Extensions/Shared/ExtensionOutput.cs` | E-03/06/07/08; reconstruction/reader/Try/ownership/example |
| `src/Ctap2/src/LargeBlobs/ILargeBlobStorage.cs` | E-09..11; shared-array/lifetime/result contract |
| `src/Ctap2/src/LargeBlobs/LargeBlobData.cs` | E-09/11; format rationale, foreign/cap/ownership/error bounds |
| `src/Ctap2/src/LargeBlobs/LargeBlobStorage.cs` | E-03/09..11; limits/chunks/RMW/failure/borrowing/70-byte auth transcript |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/AuditV2/Ctap2LargeBlobAuditReproTests.cs` | E-17/18; one-fragment/manual interop scope, exact opt-in secret export |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2CredBlobTests.cs` | E-02/17/18; conditional creation vs unconditional retrieval, historical catch/setup |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2CredProtectTests.cs` | E-17/18 + B token lifetime; positive vs no-UV proof |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2EncryptedMetadataTests.cs` | E-17/18/19; field/token/distinctness/assertion/cleanup bounds |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2HmacSecretTests.cs` | E-05/17/18; positive/same-salt UV and cleanup limits |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2LargeBlobTests.cs` | E-03/09/17/18; route/test/cleanup scope |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2MinPinLengthTests.cs` | E-04/17/18; conditional disclosure, no configured RP proof |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2PreviewSignTests.cs` | E-12..18; draft/algorithm/authorization/policy/error/cleanup/fixture scope |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2PrfTests.cs` | E-17/18; hmac substrate only, one ordinary assertion, no PRF evaluation |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/AuditV2/Ctap2ExtensionAuditReproTests.cs` | E-12; exact draft label |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/AuditV2/Ctap2LargeBlobAuditReproTests.cs` | E-17/18; local format, bounded success-path seam |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Crypto/EncryptedMetadataDecryptorTests.cs` | E-18/19; local CBC/generic formats/token/cleanup scope |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/HmacSecret/HmacSecretInputTests.cs` | E-17; map count only |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/HmacSecret/HmacSecretOutputTests.cs` | E-06/17; direct DTO bytes, not MC routing |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/LargeBlob/LargeBlobOutputTests.cs` | E-03; synthetic legacy map |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/PreviewSign/PreviewSignCborTests.cs` | E-12/17; local exact-byte/shape scope, synthetic points/history |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/PreviewSign/PreviewSignGeneratedKeyTests.cs` | E-12/14/17; vector scope/algorithm unasserted |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/Shared/ExtensionBuilderTests.cs` | E-02/12/17; capacity/numbering/mock/canonical scope |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/Shared/ExtensionOutputTests.cs` | E-03/17; synthetic key map, empty not unknown fixture |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/LargeBlobs/LargeBlobSpecFormatTests.cs` | E-11/17/18; one foreign entry/success seam/local encoder scope |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/LargeBlobs/LargeBlobStorageTests.cs` | E-10/17; first map count and noncrypto fake scope |

## Read and unchanged (11 paths)

Narrow statements/assertions below need no editorial churn. This does not clear adjacent deferred behavior
or imply execution. The E investigation's clean-unit IDs remain available for fresh sampling.

- `src/Ctap2/src/Extensions/CredBlob/CredBlobMakeCredentialOutput.cs`: boolean stored result.
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/CredBlob/CredBlobAssertionOutputTests.cs`: decoded byte equality.
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/CredBlob/CredBlobInputTests.cs`: 8-byte encoding equality.
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/CredBlob/CredBlobMakeCredentialOutputTests.cs`: true decode.
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/CredProtect/CredProtectPolicyTests.cs`: enum numeric values, not enforcement.
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/MinPinLength/MinPinLengthInputTests.cs`: true encoding.
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/MinPinLength/MinPinLengthOutputTests.cs`: integer 8 decode, not authorized disclosure.
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/PreviewSign/ArkgP256SignArgsTests.cs`: lengths/identifier/context fixture, not point/crypto validity.
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/PreviewSign/CoseSignArgsTests.cs`: synthetic factory/constructor field equality.
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/Shared/ExtensionIdentifiersTests.cs`: exact strings, not standard/support evidence.
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/LargeBlobs/LargeBlobDataTests.cs`: modeled local roundtrips/hash/list probes; no broad cleanup claims added. Existing region directives preserved.

## Snippet classification and typed prerequisites

There are **two XML code blocks** in the changed E scope; neither is a complete WebAuthn ceremony.
Both are composable fragments, not application/RP integration examples. No new executable sample was added.

| Snippet | Classification / exact prerequisites | Check / limits |
| --- | --- | --- |
| `ExtensionBuilder` class XML `<code>` | Registration-extension encoding fragment. Existing `ReadOnlyMemory<byte> myBlobData`, within actual `maxCredBlobLength`; `Yubico.YubiKit.Ctap2.Extensions` import. For sensitive content, application preflight/enforcement of level-3 policy (builder's flag is inert). Caller keeps borrowed data stable and owns its cleanup. Returned `ReadOnlyMemory<byte>?` map is not a disposable owner. | Source/API names and typed inputs inspected. Not compiled here. No session/clientDataHash/RP/user/PIN authorization/RP verification is supplied; integration belongs to F/C/A/G. |
| `ExtensionOutput` class XML `<code>` | Parsed-policy fragment. Existing nonempty `ReadOnlyMemory<byte> extensionCbor` from authData's definite-length extension map; same Extensions import plus `System.Console` context. `out` infers `CredProtectPolicy`. Policy display is nonsecret metadata, not raw secret logging. | Replaced nonexistent `response.Extensions` assumption with explicit typed bytes. Not compiled here. Limited reconstruction and Try-shape/error behavior apply; no signature/trust/enforcement verification occurs. |

Other XML inline CBOR/byte-layout illustrations are format descriptions, not C# examples. Test bodies remain
executable fixtures, not snippets corrected/compiled by this editor; intentional synthetic/invalid inputs are
labeled where misleading. Framework-validation scripts are local preservation checks, not SDK test examples.

## Local checks and remaining gates

Executed from `/tmp/opencode/ctap2-001-edit-e`:

1. `git status --short` initially empty; `git rev-parse HEAD` returned the full baseline above.
2. `git diff --check`: exit 0, no output.
3. `git diff --stat`: 46 changed C# files; no neighboring paths.
4. Python 3 + Pygments `CSharpLexer`: for every `git diff --name-only` path, assert membership in the
   inventory's E set and compare `(token-type, value)` streams for baseline `git show HEAD:<path>` versus
   working text, excluding only Comment/Whitespace tokens and empty whitespace. Separately compare every
   `^\s*#.*$` line because lexers can classify directives as comments. **Exit 0; 46 paths equal, directives
   equal.** Strings, interpolated-string tokens, API declarations and assertions remain in the comparison.
5. Python `xml.etree.ElementTree`: group contiguous `///` lines, remove the doc-comment prefix, wrap in
   `<doc>` and parse. **223 XML groups checked; zero parse failures.** This checks XML well-formedness,
   not compiler cref resolution, analyzer policy, generated API docs or snippet compilation.

Final combined rerun on 2026-11-04 checked **all 57 E paths**, not just changed paths: token/directive
streams match, 46 changed/11 unchanged, 223 XML groups parse, and this primary report names every E path.
Exit 0. `git diff --check` also reran with exit 0/no output. Initial reviewed C# `git diff --binary` SHA-256:
`2b4804abfe18c90aed83b08a3bc2981ad4f91c5e9c77cfb9de4c3f89f70723cc`.

Retained failed-attempt context: the first combined rerun exited 1 on a **report-metadata assertion**
expecting one literal `<code>` mention, although the manifest names two XML blocks. It failed after
source/XML checks and was not reported as a passing gate. Removed that unrelated, incorrect count
assertion and reran the full scope/token/directive/XML/report-path checks successfully. No source or
SDK behavior changed to obtain the result.

Context7 query used `/websites/learn_microsoft_en-us_dotnet_api` for .NET 10 ReadByteString/ReadEncodedValue
ownership; it returned **no matching documentation**. No inference relies on that failed lookup. The primary
E excerpts and framework adjudication retain original API/source evidence and its limits; no new experiment
or broad library research was run.

**Still pending:** fresh independent original-source review, required corrections/re-review, coordinator
clean-unit sampling and supplemental engineering IDs; C/A/B/D/F/G resulting boundary reconciliation;
coordinator compilation/unit/docs/architecture/complexity/scoped-format and meaningful XML-fragment gates.
Hardware remains separately not run. No token-ownership, accessor, decoder, firmware-policy, crypto or
test-assertion defect is closed by this batch. Coordinator integrates only after fresh review.

## E-core follow-up checkpoint — 2026-11-04

See [correction-e.md](correction-e.md) for independent pinned-source evidence, required versus optional
dispositions, exact four-path/hash mapping and COORD-001/cap-fixture ledger dependencies. Relative to
the initial reviewed snapshot, only these E-core prose units changed:

- `src/Ctap2/src/Extensions/LargeBlob/LargeBlobOutput.cs`: RE-R1 passive nullable Written meaning.
- `src/Ctap2/src/LargeBlobs/LargeBlobData.cs`: RE-R2 origSize/integer-key range, RE-R3 borrowed unknown
  slices versus copied known byte strings, RE-R4 CBOR/wrong-type/overflow exception families.
- `src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs`: RE-O1 last Encode/Build input lease versus
  encoded-request exchange lease; snippet code and classification unchanged.
- `src/Ctap2/src/LargeBlobs/LargeBlobStorage.cs`: RE-O2 conditional device authorization and missing
  maximum-array-size reload/fragment-refresh caveat, including its private cache summary.

All 16 [crypto-reviewed](review-e-crypto.md) hashes remain unchanged; no accepted crypto or test prose
was edited in this follow-up. The other 26 [E-core-reviewed](review-e-core.md) hashes also match their
original review manifest. Draft-10 -65700 placeholder clarification and GetInfo HasValue/absence
dependency were checked without changing crypto files. RE-O3 broad test churn was not accepted;
the cap fixture's AAD/range confounding is recorded under existing IMP-022/037 for coordinator use.

The [supplemental ledger](triage-addendum.md) now supplies existing engineering mappings; initial E-ID
provenance above remains historical and is not silently renumbered. No implementation is authorized or
closed. Fresh local checks: all57 E token/directive streams equal baseline, 46 total changed E paths,
223 well-formed XML groups, all42 non-follow-up review hashes preserved, `git diff --check` exit0.
Latest full worktree diff SHA-256 (supersedes the initial reviewed snapshot hash):
`cd4d40b22f2af01ef82e890932851e1c84353e969b4a1bda9c634e72922989b5`.

**Next:** independent E-core follow-up at the four refreshed hashes in correction-e.md; coordinator
integrates after that verdict. No crypto re-review triggered by this batch, no build/test/hardware/
commit/delegation and no compiler/API/cref/snippet gate claimed.
