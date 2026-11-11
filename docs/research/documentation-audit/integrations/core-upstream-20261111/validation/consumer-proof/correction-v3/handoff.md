# V3 AdditionalFiles correction — actual capture remains pending

## Source-backed diagnosis

The accepted namespace correction remains unchanged. Its independent review is SHA-256
`7fbb6ab164b6d5112c0773d56b0598a306191297745a5a8f4dd822826c6c0d51`.
That reviewer found a separate original-verifier policy mismatch before any v2 capture was run.
The four previously reported source/reference/analyzer/config sets were valid; those results did
**not** establish the fifth category, AdditionalFiles.

Core's existing compiler observations contain two occurrences per PublicAPI file, not one:

1. The restored PublicApiAnalyzers **5.6.0** target adds `PublicAPI.Shipped.txt` and
   `PublicAPI.Unshipped.txt` as relative AdditionalFiles when each exists. See its
   `buildTransitive/Microsoft.CodeAnalysis.PublicApiAnalyzers.targets`, lines 95–101.
2. Canonical root `Directory.Build.targets`, lines 35–42, adds those files with
   `$(MSBuildProjectDirectory)/...` absolute Include expressions for SDK libraries. Core explicitly
   opts into that policy in its actual project.
3. Selected SDK 10.0.404's `Microsoft.Common.targets` imports generated extension targets at line 42
   **before** root Directory.Build.targets at line 60. The generated Core NuGet target imports the
   exact installed PublicApiAnalyzers target under its unchanged Exists/restore guards.
4. The SDK's actual Csc invocation threads `AdditionalFiles="@(AdditionalFiles)"`. Recorded Csc lines
   224–227 contain the relative pair followed by their absolute equivalents. The hook's four recorded
   `%(FullPath)` items normalize to that same multiset.

This is existing additive compiler-input multiplicity, not a failed build or an SDK bug. No product
item is removed. No claim is made that Csc, Roslyn or the analyzer internally ignored/deduplicated
an occurrence: the proof counts **all four as-passed arguments**.

| Canonical file | MSBuild FullPath occurrences | Csc argument occurrences | Bytes | SHA-256 |
| --- | ---: | ---: | ---: | --- |
| Core `PublicAPI.Shipped.txt` | 2 | 2 | 17 | `3c3b422dddb1efeb0fc5b228608fd8682452b2a9507d4031894bce3c4dedadbd` |
| Core `PublicAPI.Unshipped.txt` | 2 | 2 | 159,074 | `e993cf43c205ac7c8abc62afd1065e5acb16f7c1b7d6b1012f4954abe20dce76` |

`observed-additional-files.json` retains the exact item and Csc spelling/order/line numbers, full
content identities, origin-source identities and matches to the original retained content records.
The item spellings are honestly described as the hook's captured FullPath metadata, not uncaptured
MSBuild Identity metadata. The complete original text files remain unchanged.

## Bounded transformation

`proof-v3.py` pins and reuses the immutable original proof through accepted v2's exact source transform.
Accepted v2's namespace helper/source/receipt remain unchanged. V3 adds only:

- An AdditionalFiles-specific canonical-path **multiset** and `(path, bytes, SHA-256)` multiset comparison.
  Every occurrence is preserved. The returned list and normal compiler counts retain multiplicity;
  Core's AdditionalFiles count is **4**, with **2** separately reported unique paths.
- A source-backed duplicate allowance restricted to **both project-local PublicAPI files exactly twice**,
  in an actual SDK-library project with the canonical root declarations, exact restored PublicApiAnalyzers
  5.6.0 declarations/import guards and selected-SDK import order. Other repeated AdditionalFiles fail.
  Unique additional inputs remain subject to exact multiset/content matching.
- An additive per-project occurrence/origin receipt and v3 manifest revision.

The generic `unique` helper and Compile/source/reference/analyzer/config/graph-node uniqueness checks
are untouched. Warning-as-error, compiler/analyzer participation, package identity, namespace guards,
source membership, XML, graph, output and live-byte predicates remain unchanged. There is no generic
MSBuild interpreter, blanket duplicate allowance, set-based AdditionalFiles deduplication, skipped
input, policy suppression or invented SDK helper.

This is a post-build verifier correction. The actual compiler attempt remains **probe-first**, and
its successful original build remains **consumer-probe-build-first**. C# bodies, source projects,
compiled capture hook, original static seal, original/v2 scripts, review and records are not rewritten.

## Full read-only representative preflight

`preflight-second.json` records **both complete compiler_project predicate paths passing**, with
**zero project failures** and 820 identity-only collector records. The entry point also checks the
original successful build receipt and exact observed two-project closure. It uses the same predicates,
not an approximation or an analyzer-only check:

| Project | Sources | References | Analyzers | Configs | AdditionalFiles occurrences |
| --- | ---: | ---: | ---: | ---: | ---: |
| Probe | 4 | 178 | 10 | 3 | 0 |
| Core | 315 | 177 | 14 | 3 | 4 |

The read-only collector applies the real identity/text predicates but returns `retained: null` and
`read_only: true`. It creates no snapshot, binary copy, evidence folder or compiler manifest. Full policy,
Csc memberships, canonical analyzer proof, restore/package/native predicates, actual obj/bin byte
bindings, graph closure/edges, current source inclusion and all three emitted Core XML examples were
checked against the existing compilation. No SDK code, example, test, native library or device ran.
The report is **not actual capture/live acceptance** and proves nothing about unbuilt future contexts.

The initial preflight launcher failed before project predicates because its new subdirectory entry
point lacked the original proof directory on Python's module search path. Its exact source and failed
stdout/stderr/exit are in `preflight-entry-failure/`. The one-line launcher-path correction preceded the
successful full preflight. This is a static-tool entry error, not another compiler or product failure.

All original failed-capture, superseded v2 and preservation records remain intact. The coordinator's
earlier failed-build-retention misuse remains classified as misuse of a failed-build-only action against
a successful build—not a failed SDK compilation. No v2 capture was executed. No new build, test,
filesystem compiler-proof capture, formatter, native/device operation, product edit, index change,
commit or agent delegation was performed for this correction. Windows CCID/HID work remains separate.

## Coordinator commands after correction review

Run from the active home checkout. Do **not** rebuild the probe or rerun its lane with `first`.
The following new labels identify v3's first actual capture, not an assertion that v2 capture ran:

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

Only after actual representative capture/live acceptance, use the additive wrapper for fresh contexts:

```bash
bash "$P/run-lane-v3.sh" examples first
bash "$P/run-lane-v3.sh" annotations first
bash "$P/run-lane-v3.sh" consumers first
bash "$P/run-lane-v3.sh" mac-comparison first
bash "$P/run-lane-v3.sh" fixture first
```

Final verification uses `proof-v3.py verify --attempt "$context-first" --capture-name
"$context-first-v3" --live`, each through a fresh v2 logger label, with no overlapping checkout job.
The original main gates remain passed and are not rerun by this correction. Any further real failure
must be retained and reviewed; read-only preflight is not permission to weaken later acceptance.
