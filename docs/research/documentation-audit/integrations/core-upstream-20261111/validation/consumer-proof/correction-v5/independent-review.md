# Independent bounded review — consolidated v5 corrections

## Decision and scope

**PASS. No required correction found in the reviewed consolidated v5 source/receipt/caller packet.**
The exact direct-Core copy classification addresses Mac R1 from the v4 review. The compile-candidate
correction removes a false consume-all-restored-candidates invariant without dropping strict checks
on any actually supplied compiler input. The fixture is ready for coordinator recapture using its
existing successful build, followed, only after acceptance, by the sole fresh Mac-comparison lane.

This is **source/read-only readiness**, not actual v5 capture/live acceptance or a successful Mac
compilation/runtime claim. Reviewer: independent Sol consumer-proof review, 2026-11-11.
Only `correction-v5/independent-review.md` was written. No SDK build/restore/test, real capture,
native/device operation, formatter, product edit, index/ref change, commit or delegation occurred.
No compiled workspace, original record, tool, receipt or earlier review was modified.

## Exact reviewed identities

Paths below are relative to this integration's `validation/consumer-proof/` directory.

| File | SHA-256 |
| --- | --- |
| `proof-v5.py` | `0c1bb1a6f71718706dc2a168ba66b9d5d83ed15c1addfaa6d2d53cac6eeb2a75` |
| `correction-v5/copy-owner.py` | `7f4374db50feafbf3c4c1fb2cc14b7b266a3c1b9010c1f7f0bf415b785dcdf07` |
| `correction-v5/compile-candidates.py` | `e82610fffbdb5011ada14dfa401529a887e7ecc9cbf2c4b1213ba43215b3786e` |
| `correction-v5/receipt.json` | `174bef5e532a95386a0e573bef9692b68e1e90824186b930779868f72eaabc39` |
| `correction-v5/handoff.md` | `f63080ead4c067c9472208b4670559994361564253f8e9dcf573ff839d5ed114` |
| `correction-v5/fixture-preflight-first.json` | `5cef54af907d875caff6634ff2dfa4488c17b2d9796415db9fbaf3c353df543f` |
| `correction-v5/mac-source-cases-first.json` | `c192618fb817d7204d79904a7b63f09d35219a711333590e2e0a8fd7df969ba4` |
| `correction-v5/lane.sh` | `9fe65a29122d95eabcfafcc60e00cda53e4ae8f58d53386a8582b7902f17a807` |

Effective collector SHA-256:
`8f48ef7a9ef476364c063d85b5a6da79289df4a56a97b0bbcb294d8cadc5ff4b`.
Independently executed `check_receipt()`: every protected-original/new-file identity, prior receipt
chain, exact counted collector transformation and effective collector hash agrees. Accepted v4
collector identity remains `f0432cbe6a8a88312c97b29c8f7982ebe62afc8ea45dbf476fbbddbfc3abd7ab`;
its independent review remains
`e2f0928dc50e756bf5b66f1c2052ba47b97c8f2a717668eb12ceeaf52afdf140`.

## Bounded changes and preserved strictness

Textual/AST comparison with accepted v4 changes only `restored_binary_bindings` in the collector:

1. Replace potential-package-compile-set equality with explicit actual-subset checking and a disposition
   for every potential candidate.
2. Add the precise declared-comparison copy branch between package ownership and the existing actual
   ProjectReference-output branch.
3. Add the candidate-disposition result field.

The adapter pins accepted v4 and its collector. It reconstructs only the already accepted capture
function with the single v4-to-v5 revision-marker substitution; it does not rewrite other capture
logic. All other compiler-project and base functions remain the accepted implementation. Receipt
checks are chained, and the new helper/source-case/preflight/lane/receipt identities join capture tools.

Unchanged guards include exact Csc/evaluated source/reference/analyzer/config membership and duplicate
rejection, complete prepared bodies/source inclusion, PublicAPI occurrence multisets, warning level
and warnings-as-errors, no compiler/analyzer/design-time skip, selected SDK/compiler provenance,
canonical NetAnalyzers DLL/import/Exists policy, actual TFM/RID selection, package/project/framework
ownership, graph closure/edges, source/output and emitted XML member/body identities, copy byte
equality, full output-tree retention, and retained/live-byte checks. No global unique-input guard or
namespace helper is weakened.

## Fixture — actual inputs versus potential restored candidates

Independently ran all four complete `compiler_project` paths and independent graph paths against the
actual `fixture-first` observations with an identity/text-only collector. Original/v2/v3/v4/v5 seals,
current prepared inputs, active-build exclusion, original successful fixture build receipt and exact
four-project raw closure passed. A Python audit hook forbade write opens, mutation and subprocesses
throughout the full reproduction.

**The four full project results, four independent graph results and all 2,751 collector records exactly
reproduce the sealed `fixture-preflight-first.json`, with zero errors.**

| Actual project | Sources | References | Analyzers | Configs | AdditionalFiles | Potential / actual package compile DLLs |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Core | 315 | 177 | 14 | 3 | 4 | 10 / 10 |
| Management | 12 | 178 | 14 | 3 | 4 | 10 / 10 |
| Management.IntegrationTests | 15 | 204 | 12 | 3 | 0 | 37 / 34 |
| Tests.Shared | 19 | 192 | 10 | 3 | 0 | 23 / 23 |

For IntegrationTests, the three unconsumed ObjectModel 18.0.1 `lib/net8.0` candidates are:

| Candidate | Bytes | SHA-256 |
| --- | ---: | --- |
| `Microsoft.TestPlatform.CoreUtilities.dll` | 99360 | `7837f9df4b11a9844143fc9419e5752bdd50305436adcbf84f82b8a52d5909d7` |
| `Microsoft.TestPlatform.PlatformAbstractions.dll` | 51784 | `668c7f0df2c30feac9cea9dce808ccabb72ff575c1e820404124212b4eae1773` |
| `Microsoft.VisualStudio.TestPlatform.ObjectModel.dll` | 335392 | `f6d6bc01a85fd68a0599533fcc0de7357922051bad90012b8ad15de31a4ef5e9` |

Each is byte-identical to the actual same-named TestHost 18.0.1 `lib/net8.0` reference. Recomputed
dispositions retain actual evaluated metadata: `ResolveAssemblyReference`, assembly version 15.0.0.0,
neutral culture and public-key token `b03f5f7f11d50a3a`. These are not assertions that package and
assembly version domains coincide. The exact resolver tie-break choosing TestHost is not recorded,
so neither the helper nor this review invents it.

Read selected SDK `Microsoft.NET.ConflictResolution.targets:40-64`: it filters Reference, Analyzer
and ReferenceCopyLocalPaths through ResolvePackageFileConflicts. Context7 `/dotnet/msbuild` was also
consulted for the distinction between reference candidates, RAR output and compiler paths. That
supports the domain distinction, not a reconstruction of this particular unlogged tie-break.

The corrected contract remains strict about **actual** inputs:

- Every actual package reference is first assigned selected package ownership, catalog file membership
  and selected compile-asset membership, with its actual binary content retained.
- The helper additionally requires `actual <= expected`; it does not accept an actual input outside
  selected compile candidates. Other actual references still require project/framework ownership.
- Base exact item/Csc equality and uniqueness remain. Actual reference multiplicity cannot disappear
  into the candidate sets: duplicate actual items/arguments are rejected before this helper.
- Every potential candidate receives a unique selected package/target owner and consumed/unconsumed
  status. Materialized unused candidates get their own identities and actual same-filename alternatives;
  absence of byte identity does not invent a resolver reason. An unmaterialized **unused declaration**
  is explicitly not represented as a missing consumed binary.

Additional independent process-local raw-view checks confirmed rejection of item/Csc reference
divergence, an actual reference missing on disk even when both streams name it, an existing package
file that is not a selected compile asset, and an actual set outside the candidate set. The exact
errors include `Csc/item set mismatch: /reference:`, `Require regular input`, and
`Actual Csc package reference is not a selected compile asset`. No actual observation was edited.
Thus the new unused-candidate treatment is not a missing-actual-input exemption.

## Mac R1 — precise direct Core copy/source contract

The new branch is limited to the exact active comparison project and exact active Core release DLL
or its precise sibling `.pdb`/`.xml` paths. It is not a basename wildcard, general HintPath evaluator,
package exemption or foreign-project exception. It requires:

- Actual comparison project policy and BoundaryCorePath matching the canonical current Core DLL.
- Native version 1.18.1 and the integration variant.
- Exactly one unchanged, unconditional direct Core reference, exact HintPath token and copy-local
  declaration, plus the sealed traversal wrapper's exact project target/properties and no ambiguous
  property keys.
- Actual output rooted under the comparison variant's build directory and exact destination filename.
- Exactly one prior collector binding for the **actual Csc Core DLL**, and a freshly retained current
  source DLL with identical size/hash to that binding.
- Unchanged v4 source/output copy-byte equality for the DLL and each actually observed precise sidecar.
  Sidecar provenance is tied to those exact project/output paths and the declared compiler DLL, without
  claiming PDB semantic/signature association. SDK related-file defaults admit `.pdb`/`.xml`.

The existing package branch still takes precedence, unknown sources still fail, and destination
containment/conflicting-copy checks remain before ownership. No product CopyLocal behavior or project
declaration was changed to satisfy the verifier. The comparison's local props retain variant-isolated
obj/bin paths; its Publish-only copy target is not misrepresented as ordinary-build evidence.

Independently reproduced the entire ten-case source report exactly: **three positive DLL/precise-
sidecar cases and seven negative cases** for wrong project, missing HintPath, wrong Core property,
missing Csc binding, changed source DLL, wrong output bytes and unrelated source. The exercised
copy-loop AST hashes to
`72ed667b9f845ee901de9a3c6c68ca0d527f0228d1fbcbbfeaebb3170f8fe9ad`.

For that reproduction, the only execution adaptation was returning the report in memory instead of
opening its output file. The exact copy-loop/owner algorithm, cases and all report fields remained
unchanged and equal to the sealed report. Binary oracles were the three registered **retained
consumers-v4 DLL/PDB/XML records**, never mutable current Core outputs rebound as historical evidence.
Negative XML/byte mutations stayed in memory. This discharges the source-known R1 gap; it is not an
actual or simulated complete Mac compiler/copy qualification. Actual future Mac policy, deps/target,
references and copy arrays must still pass the real capture.

## Preservation and failure classification

Independently verified all **2,079 fixture preserved records**, including **1,071 binary records**,
and all **23,256 prior retained records** across eight manifests, with no unexplained mismatch.
These are record counts, not counts of unique files or artifacts.

The one explicitly documented current-original difference is the unsealed copy-only `proof-v5.py`
draft: its earlier identity
`2c747ff245efca54a22e587d4b894752cb66d05928d394b8fa9cab7ffdf52d99`
remains retained; the current receipt pins the consolidated source reviewed above. That supersession
occurred before v5 receipt/capture and matches `conservation-final.json`. No earlier receipt, accepted
proof or original compilation input was overwritten.

Prior retention includes the actual 22-project consumers-v4 manifest (14,234 retained records), the
successful v3 probe/examples/annotations proofs and prior failed captures. Their retained identities
were checked without rebinding them to later mutable obj/bin files. Original probe preservation's
981 records remain included. Earlier actual acceptance remains historical v3/v4 acceptance; no v5
tool inventory or label was inserted retroactively and no successful build needs bookkeeping replay.

The fixture build is successful/zero-warning under the unchanged build-receipt validation. Its v4
capture failed the false candidate-consumption invariant, not compilation or SDK execution. The
routing-only fixture preservation adapter pins the accepted preserver and preserves that distinction;
the failed-build-only action was not invoked. Earlier namespace/additional-file failures, hash-domain
diagnosis, static launcher failure and retention-invocation misuse remain unmodified and distinct.

## Caller and lane readiness

The handoff fixture commands match the v5 parser, original actual compiler attempt/build receipt,
versioned evidence identity and logger interface:

```bash
cd /home/rupicola/Code/y/yubikit-core-docs-integration
V=docs/research/documentation-audit/integrations/core-upstream-20261111/validation
P="$V/consumer-proof"
python3 -B "$P/proof-v5.py" static
python3 -B "$V/run-command-v2.py" --label consumer-fixture-capture-v5-first -- \
  python3 -B "$P/proof-v5.py" capture --context fixture --attempt fixture-first \
    --capture-name fixture-first-v5 --build-label consumer-fixture-build-first
python3 -B "$V/run-command-v2.py" --label consumer-fixture-live-v5-first -- \
  python3 -B "$P/proof-v5.py" verify --attempt fixture-first --capture-name fixture-first-v5 --live
```

Reviewed, **not executed**. The alias affects evidence routing, not raw attempt selection or strict
context/build/closure acceptance. Verify checks context, attempt, capture name, v5 revision and receipt
hash before original retained/live identity checks. Reuse fixture-first; do not rebuild it.

After actual fixture capture/live acceptance, `bash "$P/correction-v5/lane.sh" first` consistently
selects only the uncompiled prepared MacComparison host, original hook/bodies, actual attempt
`mac-comparison-first` and new evidence `mac-comparison-first-v5`. `bash -n` passed. The wrapper
requires the active checkout, a fresh attempt/destination and no inherited competing hook, sets only
the established home-runtime temp routing, uses fresh logger labels and stops on a failed stage.

The unchanged hook is gated to an explicit AuditConsumerAttempt, active-checkout project paths and
non-`toolchain` project name. Its raw/generated destinations are per attempt **and project name**;
the BeforeCompiler guard rejects an existing policy record. No v5 global Csc trace file, outside-
project-context collector, shared diagnostic overwrite or new compiler hook was added. This is a
source-routing check, not execution of an external/toolchain compiler context during this review.

Static readiness and isolated checks do not replace actual Mac compilation or fixture/Mac capture/live
acceptance. No guarantee about future unobserved inputs, native behavior, devices, AOT linking, Windows
work or an unpublished whole-Core integration audit is inferred. No unrelated integration audit or
optional framework/cleanup work was started.
