# A follow-up — GetInfo absence semantics

**2026-11-04.** Editor A, GPT-6.1 Sol; source checkout `/tmp/opencode/ctap2-001-edit-a`,
HEAD/baseline `5b09a9b6942ded3913b5c9340c3e126617e61a82`.
Status: **documentation corrected / source and prior language evidence inspected / ready for
independent A follow-up review**. No implementation fix or independent re-review PASS claimed.
Read `review-a.md` and coordinator steering; loaded `review-receive` and followed verification
before implementation. Root/module/test instructions and previous evidence remain applicable.

## A-REV-001 — corrected introductory null overclaim

The original edited introduction's “absent scalar values remain null” was incorrect for required
`Aaguid`. Independently checked the full Parse initializer and property/local types in
`src/Ctap2/src/AuthenticatorInfo.cs`: baseline property is nonnullable `ReadOnlyMemory<byte>`
at line93, local is `byte[]?`, and line481 explicitly assigns `aaguid ?? []`.
Missing AAGUID therefore becomes empty even though CTAP requires 16 bytes. The suggested replacement
“optional nullable fields remain null” would still overclaim because of COORD-001 below.

Corrected only XML in `src/Ctap2/src/AuthenticatorInfo.cs`:

- Intro restricts preserved nulls to optional numeric, Boolean and text fields, and distinguishes
  empty lists/maps, missing-required-AAGUID fallback and encrypted metadata fallback.
- Aaguid remarks explicitly say the missing field becomes empty, retaining its required16-byte and
  unenforced-presence/length limitations.
- EncIdentifier and EncCredStoreState remarks locally describe present zero-length memory for
  absent fields; HasValue is not evidence of a returned field or feature support. RawData is the
  actionable encoded-presence escape hatch.
- Decode repeats the precise encrypted-field absence limitation while preserving borrowed input,
  complete RawData copy, separately materialized fields, unknown-field retention and existing
  required-field/integer/trailing-envelope qualifications.

No changes to parsing, API, literals, tests, assertions, example code or triage.

## COORD-001 — absent encrypted GetInfo fields become present empty memory

**Provenance:** coordinator Astra's expanded initializer inspection, independently verified by
editor A. **Source-only finding**, folded into **A-006 / CTAP-IMP-001 GetInfo decoder family**,
not a new implemented fix or SDK-executed reproduction. Coordinator owns ledger integration;
this editor did not edit `triage.md` or `triage-addendum.md`.

| Exact source path | Baseline evidence at 5b09a9b6 |
| --- | --- |
| `src/Ctap2/src/AuthenticatorInfo.cs` | Public `ReadOnlyMemory<byte>? EncIdentifier` line217 and `EncCredStoreState` line268; Parse initializes `byte[]? encIdentifier = null` line327 and `byte[]? encCredStoreState = null` line332; present keys read byte strings; result initializer assigns these arrays **directly** at lines507/512. Unknown/missing keys leave those locals null. |

Unlike a null literal assigned directly to nullable memory, the typed null array undergoes the
array-to-ReadOnlyMemory conversion, yielding nonnullable default memory, then becomes a present
nullable value. Therefore absent key0x19/0x1E yields **HasValue=true and Length=0**, not null.
That nullable presence/length does not establish wire presence; an encoded empty byte string also
has present/zero-length shape. No categorical claim is made that every aspect of the memory
representation is indistinguishable, nor is backing-object identity prescribed as a workaround.
RawData retains the complete original map and can distinguish encoded field absence.
Caller-constructed model defaults remain distinct from this Decode behavior.

### Reused pure-language evidence, with its limits

Read the complete archived experiment:
`docs/research/documentation-audit/records/runs/webauthn-001/experiment_nullable_blob.cs`,
lines5–14: a `byte[]? missing = null` is supplied to a `ReadOnlyMemory<byte>?` record constructor
parameter. Its guarded present/length-zero check corresponds to this same source-to-target
implicit conversion; the CTAP initializer has no explicit nullable-preserving conditional conversion.

The archived `records/runs/webauthn-001/validation.md:16` records coordinator execution on .NET10
with **HasValue=True, Length=0, PASS**. `review-b-blob-followup.md:15–60` independently inspects
that conversion and restricts the lost distinction to nullable presence/length, noting it did not
witness a retained execution log or rerun the experiment. This editor likewise **did not rerun it**;
the prior executed language result is reused with this provenance, not recounted as fresh CTAP evidence.

A fresh source-only fetch (HTTP200, 2026-11-04) rechecked the same pinned official runtime source
used by that archived review:
https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/ReadOnlyMemory.cs
The `ReadOnlyMemory(T[]? array)` constructor returns `default` for null; the implicit operator
returns `new ReadOnlyMemory<T>(array)`; Length is the resulting `_length`. This supports the
mechanism together with the prior language probe, not current SDK execution or all-framework-version
qualification. Context7's focused .NET API query returned no matching documentation and was not
treated as supporting evidence. No build, SDK test, hardware, probe execution or new language experiment.

### New documentation dependencies and deferred repair

| Owner / exact paths | Required reconciliation / future trigger |
| --- | --- |
| A: `src/Ctap2/src/AuthenticatorInfo.cs`; `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/AuthenticatorInfoTests.cs` | Intro, Aaguid, both encrypted properties and Decode corrected now. Existing encrypted-field unit fixtures check present arbitrary bytes, not absence; no assertions changed. Future absence-preserving decoder change must reopen these XML/fixture explanations, retaining required-field validation and complete RawData behavior. |
| E: `src/Ctap2/src/Crypto/EncryptedMetadataDecryptor.cs`; `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2EncryptedMetadataTests.cs` | Do not infer reported availability from HasValue. Inspected baseline integration lines95/96 assert nullable presence; lines130/174/209 use HasValue guards before token/decryption work, which do not detect this absent-key case. Decryptor's empty-input null result is separate from GetInfo field absence and successful decryption. Recheck explanatory test counts, guard/availability claims and decryptor call prerequisites; code/tests remain deferred and unexecuted. |
| F: `src/Ctap2/README.md`; `src/Ctap2/CLAUDE.md`; `src/Ctap2/tests/CLAUDE.md`; `src/Ctap2/examples/FidoTool/Cli/Menus/InfoMenu.cs` | Reconcile GetInfo absence/snapshot and metadata-example explanations; do not introduce blanket “all optional fields remain null” or encrypted availability checks based on HasValue alone. Numeric/Boolean display guards are a different typed case, not alleged faulty solely from this finding. Preserve product/token prerequisites and read-only GetInfo versus PIN-normalizing setup distinctions. |
| G: `docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001/boundary-map.md`; `src/WebAuthn/src/Client/WebAuthnBackend.cs`; `src/WebAuthn/README.md` | Reconcile CTAP metadata passthrough/cached-info/raw-preservation claims; do not generalize optional-null behavior across layers. A narrow WebAuthn source search found no direct EncIdentifier/EncCredStoreState consumer; no new WebAuthn implementation defect is alleged. Preserve archived WA-IMP-016 provenance as language-evidence history, not a duplicate CTAP storage bug or authorization for cross-layer runtime edits. |

**Deferred action:** coordinator retains this under the GetInfo decoder family. A later authorized
implementation pass can preserve encoded absence explicitly and add meaningful absent/present-empty/
valid-present coverage, with metadata-call-site and public XML re-review. No nullable conversion,
test assertion, capability policy or decoder validation was repaired in this documentation follow-up.

## Fresh mechanical verification and review handoff

`python3 /tmp/opencode/ctap2-001-edit-a-check.py`: **exit0**; all30 baseline inventory hashes
matched, only the same26 A-owned paths changed, lexical executable/string tokens and directives
matched in26/26 files, and218 XML blocks parsed. `git diff --check`: **exit0**.
The follow-up modifies only AuthenticatorInfo XML relative to the initial reviewed source snapshot;
the other25 reviewed files remain untouched by this correction. These are bounded text/XML checks,
not compiler/API/Roslyn gates or an independent re-review verdict.

New `src/Ctap2/src/AuthenticatorInfo.cs` SHA-256:
`922322ac1814df72ab9b03d03eb1ad74a65584c7cbf3a55abb90a79f8437f88a`.
New full worktree `git diff --binary` SHA-256:
`b692d2fe56fc7101131f454098a084c9804491a0d9702ca45e75271867aefd42`.
Diff scope:26 files,490 insertions/207 deletions. Initial independent review hashes/statistics remain
preserved in `review-a.md`; `edit-a.md` now maps this correction and records the updated snapshot.
Ready for A follow-up review of A-REV-001 **and** COORD-001, including all four corrected XML units.
No builds/tests/hardware/commits/delegation or triage edits.
