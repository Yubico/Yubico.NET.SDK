# Independent bounded review — AdditionalFiles correction v3

## Decision and scope

**PASS. No required correction found in the reviewed v3 source/receipt/caller-command packet.**
V3 resolves the specific fifth-category blocker identified in the independent v2 review without
discarding the observed duplicate occurrences or relaxing the other compiler-input categories.
The coordinator may proceed to the versioned representative recapture commands below, reusing the
original successful compilation. **Actual capture/live acceptance remains pending; this review is not
a capture, a new compilation, or qualification of any remaining lane or hardware/runtime behavior.**

Reviewer: independent Sol consumer-proof review, 2026-11-11.
Only `correction-v3/independent-review.md` was written. No original/v2/v3 tooling, receipt, evidence,
compiled workspace, C# body, product input, hook, index/ref, or other file was modified. No SDK build,
restore/test, real capture, formatter, native/device execution, commit or delegation was performed.

## Exact reviewed identities

Paths are relative to this integration's `validation/consumer-proof/` directory.

| File | SHA-256 |
| --- | --- |
| `proof-v3.py` | `3be9675df87e5fd8d742948efa8faf632df59ee6df752541d2841a4c624ccccb` |
| `correction-v3/additional-files.py` | `8ae4b8b7b9475c68b5775f8d1f6550ccffded54ba10188d4feb0225216decc7f` |
| `correction-v3/receipt.json` | `dfebeb48ffd2cacc2c82d1ea7625f294bf84e8e08ae92961172b271adab2cab2` |
| `correction-v3/handoff.md` | `bb1af6666ade662c4db4af5fa11ed3d70a45c1f4e275bf320afcd59151ecbfdb` |
| `correction-v3/observed-additional-files.json` | `49001b16b13ac5cf0d6fa3112004fc3f0e0910cd7200e5b17dc17f036af03a8b` |
| `correction-v3/preflight-second.json` | `ed5ae8e08bb6c77f6c391821ea88a35c2d8969fcf5449f3ab86348c95cd6073a` |
| `run-lane-v3.sh` | `20ed91076c58aa57a0cd6280a714d48abbc71d06be8a89f6778651d70fd22f03` |

The effective v3 predicate source hashes to
`ecff32566b0d6335d886ee239a8e63cb510e1320f9115d82b5d1cf3de6085c56`.
Independently executed `check_receipt()` successfully: all protected originals and new-file identities,
exact transformation text/counts, effective source, accepted v2 adapter and v2 receipt bindings agree.
The original proof remains
`54cb56e12e3b683d2c662c133dc6e23eadef355d7e515f478a87b36a87f25281`;
the namespace helper remains
`2949c4359d893cb600042ba29eb48e7ccadf08e9af6e176c51fc9e1e9c17007f`.
The prior independent review remains
`7fbb6ab164b6d5112c0773d56b0598a306191297745a5a8f4dd822826c6c0d51`.

## Source-backed diagnosis and narrow allowance

Read the actual root targets, Core project, installed PublicApiAnalyzers 5.6.0 targets, selected SDK
import-order source, raw observations and recorded origin descriptors. They support the stated cause:

- Installed `Microsoft.CodeAnalysis.PublicApiAnalyzers.targets:96-100` adds the project-relative
  Shipped/Unshipped pair, each under its exact project-directory Exists guard.
- Root `Directory.Build.targets:35-42` adds the same pair with
  `$(MSBuildProjectDirectory)/...` Include expressions, scoped to `IsYubiKitSdkLibrary=true`.
  Core explicitly sets that property in its actual project.
- SDK 10.0.404 `Microsoft.Common.targets:42,60` imports generated extension targets before root
  Directory.Build.targets. The generated namespace-qualified package import and its Exists/restore
  guards match. Actual Csc wiring passes `@(AdditionalFiles)`.
- Core's original Csc lines 224-227 contain the relative pair followed by the absolute pair. Original
  hook FullPath lines 1-4 resolve to the same four occurrences. This is additive input multiplicity,
  not a failed compilation or evidence of internal compiler/analyzer deduplication.

The exception is **not hard-coded to Core or granted to every project**. It derives both expected
PublicAPI paths from the actual compiler project's directory and requires exactly two occurrences
of each repeated file. `source_origin` then validates that project's explicit SDK-library opt-in,
its actual restored PublicApiAnalyzers 5.6.0 package identity, canonical root declarations, package
Include/Exists declarations, generated import/guards, SDK order and Csc AdditionalFiles wiring.
Those sources are retained as additional origin records. Another SDK project can qualify only with
its own local pair and its own actual source/restore context; a foreign pair or arbitrary repeated
input cannot borrow Core's allowance. A host with zero inputs correctly has zero multiplicities and
no origin exception. Unique additional inputs remain subject to exact item/argument/content matching.

## Occurrence fidelity — no set-based loss

`additional_file_multiset` compares canonical-path Counters and `(path, bytes, SHA-256)` Counters.
It separately records every item and Csc occurrence with raw line, spelling, ordinal and source line
number. The selected raw Csc argument sequence must also equal the parser's actual AdditionalFiles
argument sequence. Original whole raw texts are still retained by the base implementation.

The old `/additionalfile:` **set/uniqueness** comparison is removed only because this category now
has its own stricter multiplicity-aware comparison. It is not dropped without replacement. The helper
returns a **list containing every item occurrence**, not a set. That list continues through the base
content-retention loop, `counts.additional_files`, and sorted `csc_sets.additional` unchanged:

- Core: 4 items, 4 arguments, 2 unique paths, multiplicities `{Shipped: 2, Unshipped: 2}`.
- Core's normal manifest AdditionalFiles list still has length 4, including both copies of each path.
- Four additional-file content-retention calls occur; shared content-addressed snapshot storage does
  not erase occurrence records. The ordered descriptor and original text retain original ordering.
- Probe: 0 items, 0 arguments, 0 unique paths, empty occurrence lists, `origin: null`.

Independently recomputed the entire Core descriptor and found it exactly equal to both the sealed
preflight result and `observed-additional-files.json`. Its links to original preserved content records
are valid. Core baseline identities remain:

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `PublicAPI.Shipped.txt` | 17 | `3c3b422dddb1efeb0fc5b228608fd8682452b2a9507d4031894bce3c4dedadbd` |
| `PublicAPI.Unshipped.txt` | 159074 | `e993cf43c205ac7c8abc62afd1065e5acb16f7c1b7d6b1012f4954abe20dce76` |

Additional in-memory, read-only boundary checks confirmed rejection of:

1. Item/argument multiplicity mismatch, even with matching unique paths.
2. Both PublicAPI files repeated three times.
3. Only one repeated PublicAPI file.
4. An unrelated repeated additional file.
5. A raw Csc AdditionalFiles sequence differing from the supplied parsed argument sequence.

These checks changed only process-local text views; no actual observations were edited or generated.

## Unchanged strict guards, routing and tool bindings

Compared effective v2 and v3 sources textually and by top-level AST. Only `compiler_project` and
`capture` differ: the single category-specific helper call, removal of that category from the old
set loop, additive per-project descriptor, and v3 revision marker. The generic `unique` helper remains
unchanged. Source, reference, analyzer, config, project/node/edge and other original duplicate guards
remain strict. Warning-as-error/level, no warning exemption, analyzer/compiler participation, selected
SDK/compiler, full body/source membership, namespace import/Exists policy, graph closure, restored
assets, actual obj/bin identity, emitted XML member/body and live-byte predicates remain unchanged.

The main v3 entry checks its receipt before dispatch. Original, v2 and v3 seal/receipt checks are
chained. The added helper/preflight/wrapper/receipt identities join capture tools; original compilation
tools are not relabeled or rewritten. The original observation hook, wrappers/bodies and active-root
bindings retain their sealed identities. Fresh-lane environment/hook/build routing remains consistent
with the accepted wrapper; `bash -n run-lane-v3.sh` passed without executing the lane.

Capture naming changes the evidence destination only. Raw compiler context and binary directory
continue to use `attempt`. Context/build selector validation and actual Csc closure remain in force.
Verify additionally requires the exact attempt, capture name, v3 revision and v3 receipt hash before
the original retained/live identity checks. No evidence alias bypasses warning/skip/context policy.

## Fresh independent full read-only verification

Constructed v3 predicates directly, then independently checked active-build exclusion, original/v2/v3
seals, current prepared inputs, original successful build receipt and exact Probe-plus-Core raw closure.
Called **both complete `compiler_project` paths** with an identity/text-only collector implementing
the real retention predicates but no storage. A Python audit hook prohibited filesystem write opens,
directory/file mutation and external-process execution during this full verification.

| Actual project | Sources | References | Analyzers | Configs | AdditionalFiles |
| --- | ---: | ---: | ---: | ---: | ---: |
| Probe | 4 | 178 | 10 | 3 | 0 |
| Core | 315 | 177 | 14 | 3 | 4 |

**Both complete project results and all 820 identity-only collector records reproduced
`preflight-second.json` exactly, with zero project failures.** This includes full package/analyzer,
graph, restored copy/native-asset byte predicates (without loading anything), actual output binding,
current source membership and all three emitted Core XML examples. It is stronger than the v2
analyzer-only verification, but remains explicitly distinct from filesystem capture/live acceptance.

Independently re-read all **981 original/preserved record pairs**, including **411 binary records**;
all sizes and SHA-256 values match, with zero mismatches. The count is 981, not 986.
`preservation-readonly-first.json` is consistent with that independent check. The original successful
build and failed v1 capture remain intact; no v2/v3 actual capture was run by this reviewer.

The new initial preflight-entry failure is correctly separate: its retained stderr is
`ModuleNotFoundError: No module named 'prepare'`, raised before project predicates. The preserved
failed launcher source/exit/stdout/stderr are receipt-bound. Adding the proof directory to Python's
module search path is the launcher correction, not an SDK/product correction or failed compilation.
The prior failed-build-retention misuse remains separately classified and unaltered.

## Coordinator caller-command consistency

The handoff's representative commands match the v3 parser, exact actual compiler attempt/build label,
new evidence identity and v2 logger interface:

```bash
cd /home/rupicola/Code/y/yubikit-core-docs-integration
V=docs/research/documentation-audit/integrations/core-upstream-20261111/validation
P="$V/consumer-proof"
python3 -B "$P/proof-v3.py" static
python3 -B "$V/run-command-v2.py" --label consumer-probe-capture-v3-first -- \
  python3 -B "$P/proof-v3.py" capture --context probe --attempt probe-first \
    --capture-name probe-first-v3 --build-label consumer-probe-build-first
python3 -B "$V/run-command-v2.py" --label consumer-probe-live-v3-first -- \
  python3 -B "$P/proof-v3.py" verify --attempt probe-first --capture-name probe-first-v3 --live
```

These commands were reviewed, **not executed**. Do not rebuild the representative probe or rerun its
`first` lane. The original attempt/build receipt must remain `probe-first` /
`consumer-probe-build-first`; v3 evidence belongs under `probe-first-v3`. Any genuine capture failure
must remain retained and be reviewed, not be reclassified as a failed SDK compilation.

The fresh-lane wrapper's selectors and build/capture/live labels agree with the handoff for examples,
annotations, consumers, mac-comparison and fixture. Its new-attempt/new-destination checks remain.
Final live verification consistently uses `<context>-first` and `<context>-first-v3`. Remaining lanes
still require their own successful actual builds and accepted capture/live checks; the source-backed
per-project allowance does not predict those results. No whole-integration/Core audit was restarted,
and separate Windows work or hardware/native runtime acceptance is outside this review.
