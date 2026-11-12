# Core Packet C fresh documentation edit batch

**Ready for coordinator transfer review, not independent acceptance or publication:** 54 Packet C
primary C# paths have documentation-only edits; 50 primary paths are retained. The complete 104-path
disposition, raw base/current hashes, candidate/proposal mapping and exact example bodies are recorded
under [editing/C](editing/C/). Runtime code, declarations, imports, directives, defaults, attributes,
runtime strings, fixtures and assertions are preserved. No commit was made.

## Identity, authority and boundaries

- Isolated worktree: `/home/rupicola/Code/y/yubikit-core-docs-edit-crypto`.
- Branch: `yubikit-core-docs-edit-crypto`; editor base HEAD:
  `da66f5d01ba594993974ddd26723e8c300c0a454`.
- Product-source pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. All 104 base primary
  source texts match that pin after explicit BOM/CRLF normalization; raw identities are retained.
- Release: [triage/editing-release.md](triage/editing-release.md), completed C report/findings/final
  source register and [C clarifications](investigation/C/triage-clarifications.md), full-family map,
  inherited dispositions, B6 ownership and final overlap refinements. Root/Core/nested-test instructions,
  approved editorial voice and SDK house style were read. The original source-authority records remain
  unchanged; this is fresh editing rather than a rerun of investigation.
- Writes are confined to assigned Packet C primary `.cs` paths, `editing/C/*` and this report. Root,
  Core/test README/CLAUDE, shared guides, configuration, API snapshots, other modules, B6 sources,
  investigations, original reports, preserved archives and the primary checkout were not edited.
- Shared narrative recommendations and caller-specific dependencies are recorded for their assigned
  owners, rather than adopted as Core-wide or applet-specific guarantees.

## Transfer records

| Record | Contents |
| --- | --- |
| [files.json](editing/C/files.json) | All 104 primary paths: edit/retain reason, original product-pin/base/current raw SHA-256, local/proposal associations and exact changed documentation line ranges |
| [path-decisions.json](editing/C/path-decisions.json) | All 144 original candidate/path associations across 85 unique paths, with edit/retain/held-facet/owner-referral decisions and base/current hashes; retained facets remain distinct when a file changes for another candidate |
| [candidate-map.json](editing/C/candidate-map.json) | Twenty stable local IDs → FPT-083 through FPT-102, original facets/aliases, actual primary paths and per-path decision references; no accepted IDs allocated |
| [decisions.json](editing/C/decisions.json) | Authored local decisions, explicit retained primary/facet reasons and three additional primary documentation associations |
| [examples.json](editing/C/examples.json) | Exact base/current XML code-body lines, character ranges, hashes and classifications; all compiler/execution states NOT-RUN |
| [source-review.md](editing/C/source-review.md) | Consequential original-passage/editor checks, exact revision/status/actor/strength conditions and example type/lifetime review |
| [shared-followups.md](editing/C/shared-followups.md) | Shared-owner recommendations, B6 overlap refinements and bounded deferred engineering/provider/source/applet/physical qualifications |
| [static-verification.json](editing/C/static-verification.json) | Fresh scoped lexical/non-comment-line/XML/hash/allowlist/user-file checks and the exact archive-check qualification |
| [output-identities.json](editing/C/output-identities.json) | Editor record identities for coordinator transfer; not an acceptance/publication seal |

The small [capture-static.py](editing/C/capture-static.py) joins existing packet/candidate/map records
and captures static receipts in its own directory only. It reuses the inventory's Pygments lexer
approach and adds exact non-comment-line comparison for this standalone-comment edit shape. It does
not replace the repository's later compiler/API/example gates or create a new validation framework.
Product and authored record edits used patch; JSON receipts are generated from those exact inputs.

## Local and full-family proposal disposition

| Local ID | Proposal | Applied documentation / retained qualification |
| --- | --- | --- |
| C001 | FPT-083 | Interactive pooled characters versus redirected managed-string storage; returned byte-owner disposal remains independent. Redirected-storage policy stays open. |
| C002 | FPT-084 | First confirmation entry retained through comparison, second cleared, first transferred only on success. Original resolved cleanup scope retained; pool sentinel identity is qualified. |
| C003 | FPT-085 | UTF-16 limits, code-point/encoded-byte prompt units, ignored versus rejected input, hex separators/decoded units, default UTF-8 fallback and empty allocation limit. No normalization, empty-input, hex or applet PIN policy change. |
| C004 | FPT-086 | Waiting-key cancellation polling versus queued keys/redirected reads. Existing waiting-branch test retained; responsiveness experiments unrun. |
| C005 | FPT-087 | TLV copy/view/disposal, list collection versus element ownership, pooled alias lifetime and clear-only handle versus pool return. Correct nested shared cleanup example remains with SHARED unchanged. |
| C006 | FPT-088 | Search/dictionary abandoned-copy qualifications and existing disposable-list alternative for sensitive sequences. No cleanup fix, new oracle or writable cast. |
| C007 | FPT-089 | Bounded tag/framing behavior and BerLength sizing/writing; regression comments describe current guards/exact sizing. X.690 original named clauses inspected; earlier pending confidence historical. Single-item/tag/negative-input engineering choices held. |
| C008 | FPT-090 | Factory ownership/configure-before-use/static instances/global temporary scopes and callback exception provenance. B6's ordinary header/length trace versus SELECT bytes distinction preserved in shared handoff, not edited into B6 files. |
| C009 | FPT-091 | Typed PKCS#8/SPKI subset, AlgorithmIdentifier versus outer/nested exhaustion and ASN.1 exception distinctions. No uniform-factory/full-DER acceptance claim; selected experiment remains unrun. |
| C010 | FPT-092 | Correct RSA public XML, mutable key-owned parameter aliases, cached EC point separation, actual shape/selected-relation versus full-validation boundary and DP-width admission. Provider acceptance and validation-depth/API choices remain separate. |
| C011 | FPT-093 | Remove no-added-exposure rationale, name independent secret-copy lifetimes, narrow macOS extra-D wording to inspected .NET v10.0.0 absent-Q route. Supplied Q skips that checked extraction; actual runtime/native erasure remains unqualified. |
| C012 | FPT-094 | Actual public creator reach versus direct runtime calls/internal seams, per-call ownership and complete provider disposal fragments. Fake helpers/stale xref removed; internal tests retain their actual scope. |
| C013 | FPT-095 | AES/KDF/alternate-CMAC/ASN writer and ECDH result cleanup boundaries. AsnWriter Encode/Reset source is settled; current omitted Reset, partial allocation and exceptional-copy cleanup remain unchanged engineering concerns. |
| C014 | FPT-096 | Current numeric/indefinite/size guards, early version/scalar checks, observer-alias limits, finite pool and published-vector context. No fixture, assertion or executable test changed. |
| C015 | FPT-097 | Exact COSE Key Types registry/local zero sentinel wording and structural versus complete profile pairing. Stable IDs and correct catalog/math/encoded-width context retained; no applet upgrade or NIST-final approval claim. |
| C016 | FPT-098 | Inner parameter omission versus RFC 5915 §3 requirement; explicit schema versus broad importer assertions. Target profile and runtime changes held; finite tests stay finite. |
| C017 | FPT-099 | Current RSAPrivateKey/VerifyPinAsync examples with borrowed inputs/session and actual owned scopes, ordinary short-Weierstrass terminology and private-owner rationale. Shared span/Base32/struct corrections handed off. |
| C018 | FPT-100 | Expired draft-09 target, domain separation without injectivity and exact A/B/C/handle assertions. Missing immutable Rust/generator provenance and stronger conformance claims remain held. |
| C019 | FPT-101 | **Held P/E/Q/B facets; no D facet was released.** Same paths receive independent C011/C012/C018 edits. No allocation/conversion/error/native-erasure fix or deployed producer/export/provider acceptance is claimed. |
| C020 | FPT-102 | **Retained/referral.** No C-owned primary SCP parameter path. B6/SHARED/applets own contained aliases, with-clone lifetimes, optional OCE-reference/profile/initializer and actual caller claims. |

These are nonadditive local/proposal mappings, not twenty accepted defects. M/P implementation/policy
choices remain deferred; E facets block only their stronger unrun-experiment statements. B facets
still require the actual applet boundary before caller-specific claims. Q gaps remain precise.

## Explicit unchanged candidate-associated primary paths

- `Credentials/ConsoleCredentialReaderTests.cs`: retain the actual pre-cancelled no-keys fixture;
  it covers the waiting branch, not queued/redirected cancellation.
- `Cryptography/CryptographyProviderExtensionTests.cs`: retain finite internal ECDH/CMAC factory and
  cleanup assertions, rather than treating them as public or universal substitution coverage.
- `Cryptography/ICmacPrimitives.cs`: narrow internal operation summaries make no all-copy erasure
  promise. Alternate helper cleanup is documented at its implementation.
- `Cryptography/CryptographicDefinitions.Algorithms.cs`: retain operation metadata/relationships;
  complete pairing qualification is in AlgorithmInfo and the affected test explanation.
- `Cryptography/KeyType.cs`: correct concrete identity/nominal-width descriptions and stable IDs stay.
- `Cryptography/IPublicKeyExtensions.cs`: ordinary checked-cast summary has no fake helper/type or
  special cryptographic safety promise. The stale private-key example is corrected separately.

All six paths above are beneath `src/Core/` in their assigned source or unit-test directories; exact
full paths/hashes are in the records. The other retained primary paths also have explicit reasons.
In particular `Utilities/Base32.cs` and its tests retain the already-correct source-actual empty,
space/case/padding/pad-bit, overlap/admission, failure clearing and output-tail ownership semantics.
Resolved OATH Base32 scopes are not reopened by unrelated Unicode or ownership questions.

Candidate-associated shared/B6/external/applet files are all retained with owner-referral reasons in
`path-decisions.json`. A file edited for another candidate does not silently close a retained facet:
the seven explicit retained-facet rows distinguish test/parser/provider overlaps.

## Example-body and citation limits

The current active XML population consists of the credential callback class, complete reader and
private-key methods, retained intent-scope fragment, and twelve provider lifetime/startup fragments.
The 22 RsaFormat XML bodies remain commented-out historical exclusions. `examples.json` retains base
and current body identities separately, including the removed internal-creator fake example.

No example was compiled. No algorithm/provider/device execution was used to support prose. The
editor checked actual types and declarations, kept complete owned using/finally scopes, and named
borrowed caller lifetimes. Independent reviewers must inspect the full affected units and consequential
original passages; the source-review note is editor work, not independent review.

## Static verification and preservation

The fresh static receipt checks all 104 primary files against the pinned/base text and preserves their
non-comment lines and non-comment lexical token streams. XML documentation blocks are well formed.
Tracked and untracked output paths stay inside the allowlist; staging is empty. `git diff --check`
passes. These checks do not claim semantic API binding, compilation or passing SDK tests.

The existing archive checker passed its six archive populations (784, 402, 534, 164, 433, 477 records),
then exited 1 because the original user files are absent in this isolated worktree. That exact failure
is retained rather than called a passing full command. Separate read-only checks at their original
primary-checkout locations match all four Core user-file baseline hashes. No file was copied into,
or changed in, the primary checkout. Other protected tracked files and original reports have no diff.

No SDK build, test, formatter, cryptographic experiment, native/device execution, index/ref update,
commit, push or subagent ran. The future Windows CCID/HID ready handoff remains unready and the PIV
physical touch/hotplug/touch-dependent cancellation deferral stands.

## Coordinator handoff and stopping point

Review/transfer this disjoint edit batch and route shared recommendations to the later SHARED owner.
Independent content/citation review and authorized static/compiler/example gates are still pending;
this report makes no Core Pass-1 publication claim. Preserve NIST-final table, original Rust generator,
installed provider/native producer and actual applet/profile qualifications in the deferred-finding
handoff. Finish Core Pass 1 publication plus that handoff, then stop. Do not start Pass-2 planning or
implementation from these records.
