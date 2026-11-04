# Independent review A follow-up — GetInfo absence semantics

**2026-11-04. Reviewer: GPT-6.1 Sol. Documentation review PASS for the final 26-file A batch.**
Reviewed source checkout: `/tmp/opencode/ctap2-001-edit-a`.
HEAD/baseline: `5b09a9b6942ded3913b5c9340c3e126617e61a82`.
This report is authored in the **primary** checkout. Initial review and hashes remain preserved
in [review-a.md](review-a.md); this follow-up supersedes its pending verdict for the exact final
source hashes below, not its evidence history or validation limits.

## Required and optional dispositions

| Item | Final disposition | Basis |
| --- | --- | --- |
| **A-REV-001** | **Corrected; independently re-reviewed PASS** | Intro now bounds null preservation to optional numeric/Boolean/text fields. Missing required AAGUID is explicitly empty both there and in the property remarks. No blanket “nullable fields remain null” replacement. |
| **COORD-001** | **Documentation corrected; independently re-reviewed PASS; implementation remains deferred** | Coordinator Astra identified direct nullable-array assignments for the encrypted fields. Both local property remarks, intro and Decode now accurately describe absent fields becoming present empty memory and the resulting limits of HasValue as availability evidence. |
| Other required corrections in this A follow-up | **None** | No further factual or usefulness correction found in the corrected complete units and full initializer. |
| Optional wording preferences | **None requested** | Current prose is direct, locally useful and preserves product/token/representation qualifications. No preference-only rewrite needed. |

The original review's proposed “optional nullable fields remain null” alternative would have
remained too broad. COORD-001 was found by the coordinator's expanded initializer inspection,
not the initial review or an SDK test. This follow-up independently confirms it and corrects that
review limitation rather than treating the initial review as complete absence-behavior evidence.

## Inputs and complete source re-review

Previously read canonical root/module/test/Core guidance, approved style/workflow and
`workflow-verify` remain applicable. Read complete primary [correction-a.md](correction-a.md)
and updated [edit-a.md](edit-a.md), then **all 750 lines of final `AuthenticatorInfo.cs`**,
including every public property, local declaration, switch assignment, full result initializer,
Decode and helper implementation. In particular, re-reviewed these complete XML units with
their actual source rather than accepting the editor's summary:

- Intro: lines22–37.
- Aaguid: lines96–103.
- EncIdentifier: lines229–248.
- EncCredStoreState: lines292–309.
- Decode: lines325–343; full Parse: lines348–564.
- RawData: lines73–80, with actual clone and initializer assignment at340 and527.

Read the complete archived `records/runs/webauthn-001/experiment_nullable_blob.cs`, its
`validation.md` including row16, and `review-b-blob-followup.md`. Also read the complete current
`evidence/framework/src/experiment_framework.cs`: it covers writer sorting, reader trailers and
Guid byte conventions, **not this nullable-array conversion or this SDK decoder**. No probe,
experiment or SDK method was executed in this review.

## Independent type/assignment and language evidence

| Field family | Actual declarations/assignments in final source | Accurate public meaning |
| --- | --- | --- |
| Lists/maps | Nullable list/map locals; result uses `?? []` or explicit empty read-only dictionaries, lines528–562 | Encoded absence becomes empty at the typed surface. RawData is needed to inspect wire presence. This does not redefine missing required versions as valid. |
| Optional numeric/Boolean/text | Nullable numeric/Boolean locals and nullable text local start null, lines356–380; direct same-type assignments in result | Absent fields remain null for these families. Present false/zero/empty text is a different case; optionality does not waive required-if-supported or present-value protocol constraints. |
| FirmwareVersion | Nullable reference local365 and direct result assignment543 | Absence remains null here too; the corrected intro does not claim its three listed families are an exhaustive account of every null field. Existing YubiKey-layout/session-snapshot qualifications remain. |
| Required Aaguid | Property is nonnullable `ReadOnlyMemory<byte>` line103; local `byte[]? aaguid = null` line354; result explicitly assigns `aaguid ?? []` line530 | Missing required identifier becomes empty. Required16-byte status and unenforced presence/length are preserved in the public remarks. This is not a parser repair. |
| Optional EncIdentifier / EncCredStoreState | Properties are `ReadOnlyMemory<byte>?` lines248/309; locals are **`byte[]?`**, initialized null at376/381; present keys read byte strings at487–489/509–511; result assigns the arrays **directly** at556/561 | A typed null array converts to nonnullable default memory, then a present nullable value: HasValue=true, Length=0. HasValue alone does not establish that the field was encoded or supported. |

The encrypted fields do **not** use a null-preserving conditional conversion such as the separate
`CtapResponseParser.ToNullableMemory` helper. They also do not assign a null literal directly to
the nullable property. The public auto-properties have no custom setter that changes this path.
Caller-constructed defaults may therefore be null even though Decode's absent-key path yields
present empty values. The new property remarks explicitly attribute the behavior to **Decode**.

### Original framework evidence and prior executed language evidence

Context7 was queried for the exact .NET conversion topic. It returned some ReadOnlyMemory
constructor and null-literal documentation, plus unrelated span/AsMemory material; none of those
alone proves this chained typed-array conversion. A bounded fresh original-source fetch succeeded
(HTTP200, 2026-11-04):

https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/ReadOnlyMemory.cs

Independently read the array constructor, implicit operator and Length implementation:

```csharp
public ReadOnlyMemory(T[]? array)
{
    if (array == null)
    {
        this = default;
        return;
    }
    _object = array;
    _index = 0;
    _length = array.Length;
}
public static implicit operator ReadOnlyMemory<T>(T[]? array) => new ReadOnlyMemory<T>(array);
public int Length => _length;
```

The archived pure-language experiment supplies `byte[]? missing = null` to an explicitly
`ReadOnlyMemory<byte>?` record parameter and guards a present Length0 result. Archived validation
row16 records its prior .NET10 execution as **HasValue=True, Length=0, PASS**. That source-to-target
conversion is the same relevant conversion as the CTAP auto-property assignments; the extra record
constructor does not introduce a null-preserving branch. Pinned framework source supports the
nonnullable default-memory intermediate, and the archived executed language result supports the
nullable wrapping outcome. This is **reused prior language evidence**, not a fresh SDK reproduction,
independently witnessed fresh execution log, all-runtime qualification or new test result.

The revised text stays within that evidence: absent fields produce a present zero-length value,
and HasValue does not prove wire presence/support. It does **not** assert that all aspects of absent
and encoded-empty memory are indistinguishable, prescribe a backing-identity workaround, or imply
that a valid IV||ciphertext was returned. Actual memory identity can differ; encoded presence belongs
to the preserved CBOR envelope, not an incidental backing object.

## Public usefulness and remaining bounds

**PASS:** Intro, both encrypted property remarks and Decode direct callers to RawData for encoded
presence. Decode clones the complete borrowed input at340, assigns that clone to RawData at527,
and preserves unknown fields while materializing known ones. The input is left unchanged. On
caller-constructed instances RawData remains empty, as its existing remarks state. This is an
actionable presence-inspection escape hatch, not an automatic support/decryptability validator.

The corrections preserve the required Versions/AAGUID, length/integer and unread-trailing-byte
limitations, current-state snapshot and continuation-adjacency guidance, optional protocol fields,
and product/firmware/persistent-token prerequisites. No field fix, request preflight, validation,
conversion, assertion or capability policy was added. Existing positive encrypted-byte fixtures
do not test encoded absence and are not newly claimed to do so.

COORD-001 remains under A-006/CTAP-IMP-001's GetInfo decoder family with E/F/G reconciliation
dependencies described in correction-a. In particular, metadata consumers and explanatory test
guards must not infer support solely from HasValue. A later absence-preserving implementation
change must reopen these XML units and absent/present-empty/valid-present evidence. This A verdict
does not independently close all E/F/G call-site dependencies or the final cross-layer review.

Other initial A dispositions remain valid because their exact source bytes are unchanged. Keep
the reset5.5.2/5.5.3 evidence gap, Guid hypothetical-big-endian/probe limits, canonical writer
AUTOSORT adjudication, successful-response cleanup limits, and PIN-normalizing-versus-read-only
test-lane distinctions from review-a. No module-wide conformance, hardware support or rollback
guarantee follows from documentation acceptance.

## Fresh read-only checks and final exact manifest

Executed read-only Git/inline Python comparisons only:

- Compared every actual final hash with the initial26-row manifest: **only AuthenticatorInfo changed;
  other25/25 hashes unchanged**.
- Removed full-line // comments and compared against baseline for every edited file:
  **26/26 noncomment line sequences identical**, including declarations, assignments, strings,
  directives, imports and assertions. This is bounded text preservation, not a Roslyn/compiler gate.
- Reversed only the five corrected XML units in memory; reconstructed file hashes to initial
  AuthenticatorInfo **`a09236d605a4d29b483086d9a1c6942e5a6289beeef2b33a71bfe7032620116a`**.
  This independently establishes the follow-up changed only those XML units.
- `git diff --check 5b09a9b6`: **exit0**, no whitespace diagnostics.
- HEAD still equals baseline. Final full `git diff --binary` SHA-256:
  **`b692d2fe56fc7101131f454098a084c9804491a0d9702ca45e75271867aefd42`**,
  matching correction-a/edit-a. Baseline hashes remain available in initial review-a.

| Path | Final reviewed SHA-256 |
| --- | --- |
| `src/Ctap2/src/AuthenticatorInfo.cs` | `922322ac1814df72ab9b03d03eb1ad74a65584c7cbf3a55abb90a79f8437f88a` |
| `src/Ctap2/src/Backend/HidBackend.cs` | `b8d43bc9167e4f6f2066cfeba1bdf44a9304ea334b82b4603a5fe3670c83c8ea` |
| `src/Ctap2/src/Backend/ICtap2Backend.cs` | `775983b21bc7f6ef1c8e42e640fa2957c12cd26f7aa515d835b635296d9b0d0f` |
| `src/Ctap2/src/Backend/SmartCardBackend.cs` | `63592ea8add83dc6ed9a6cfce0366272e958f4f2eb2c6d11cd15bbdefe4eb295` |
| `src/Ctap2/src/Cbor/AaguidConverter.cs` | `dfe700e949e12ddadb97bd80cda2a82a78d39256672df2b8474503375f93dd09` |
| `src/Ctap2/src/Cbor/CtapRequestBuilder.cs` | `e5b6de47d3a1e177ba12f8b5a6911d0037f53da1b223fe87d694e0104d40b7b7` |
| `src/Ctap2/src/Cbor/CtapResponseParser.cs` | `6de6a26c37a9f429977c886a5a48e1ecf39f9e9c661132296006f0330c84c33f` |
| `src/Ctap2/src/Ctap/CtapException.cs` | `d00285a610824aeae1407abe96ba1c1dd2592bee072a90b7bcd010429624b6e0` |
| `src/Ctap2/src/Ctap/CtapStatus.cs` | `ef56011b3bdca54eaee1149169901158ecc2c9e57bde07ba10ebc0ec01e024c9` |
| `src/Ctap2/src/Ctap2Session.cs` | `23fbfeec9b58730d49638c7557b160102dc01fa233f8e1fd1241778d41056eab` |
| `src/Ctap2/src/ICtap2Session.cs` | `ec5e8dec71d67e76899d655aa0421f94aeef0d9fbb1a8a0ed0b2c7e17870fcfa` |
| `src/Ctap2/src/IYubiKeyExtensions.cs` | `141e10279591de25200a5d1a41d9a493326eb12b0f14b3f503ec3b8e5dcb3644` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2GetInfoTests.cs` | `0189a5de09887165dc8f010f0f57f295f751bd0362a7d0655d02aa6ae38bd3b6` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2SessionSimpleTests.cs` | `8431df6913b1ce29a27524de19d5142591317cca2e5d82c5ad236bb1961aa74a` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2SmartCardTests.cs` | `31d06123090bb82be279aa1fb79788390eedc0abb26940e0cfdf786942212fa1` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2TestData.cs` | `4ee7a26504e3e8d268056646b7dd37717851958728dae8efd0ff13f8edbd8eb6` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2TestHelpers.cs` | `fce6094f3f89cc82158997db5cf3c49f6f90a32efb132c0f1f054d65478d1cda` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2TransportTests.cs` | `01875b75897b3f75898f45a1b26f2ae84fbe7ceb132b7ae17e7328f2cc7b15c0` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/IntegrationTestBase.cs` | `8534fe77acc8606e50d9d67a250ede3ef98d93afe7c5d68e510d6cb263594a1f` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/TestExtensions/Ctap2TestStateExtensions.cs` | `e9dcfe37200e017ddf6d157e56f1422af4d7a2068752c557715008390b886490` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/AuthenticatorInfoTests.cs` | `2f87c97f26887c16667165b8d7de65d62eb6fafacb6b507551260ed27da1f21a` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Ctap2SessionHidCrossLayerTests.cs` | `4b7e32722222c1d6cd2060bfacaaa7ca4a0ba2f63053e46e2d0310cab8b10dff` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Ctap2SessionTests.cs` | `25ce70917293498541396d32883c8a5edae683442c3f5f0403ed88fd8b1aa7b6` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/CtapExceptionTests.cs` | `88b9aaf518381cc2d383fc1858879b0612291aac24e2e145026e1d369e27b5cd` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/CtapRequestBuilderTests.cs` | `0c084830e84e043e623b6b0b78b9827d498ffa91461d32a650bf9f1054481ad2` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/IYubiKeyExtensionsTransportTests.cs` | `033d6c325a73962d7640e169dfbe7d3074b3dcb815f9faf107fbedeb2bfb3b28` |

**Final status: documentation independently re-reviewed PASS for this A batch; implementation
deferred; coordinator final compiler/token/example/integration gates pending.** No source edits,
builds, tests, fresh experiment execution, hardware, commits or delegation occurred. Only this
follow-up report was authored in this follow-up.
