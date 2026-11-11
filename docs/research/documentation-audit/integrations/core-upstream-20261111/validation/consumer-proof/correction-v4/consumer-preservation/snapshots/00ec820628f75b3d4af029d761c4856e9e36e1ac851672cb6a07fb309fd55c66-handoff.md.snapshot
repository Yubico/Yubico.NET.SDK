# Verifier v2 — preserve the successful probe build

## Root cause and unchanged compiler inputs

The original representative build passed with zero warnings. Its first compiler-proof capture failed
in `analyzer_proof`, not in Csc or the SDK. Actual SDK 10.0.404 / NuGet 7.0.0 generated both `.nuget.g.props`
and `.nuget.g.targets` with the default XML namespace
`http://schemas.microsoft.com/developer/msbuild/2003`. The v1 verifier queried unqualified `Import`
elements. ElementTree stores these as `{namespace}Import`, so that query returned **zero elements**.

There is **no observed slash, path-alias, property-expansion or Exists-expression discrepancy**.
Namespace-aware selection finds exactly one canonical import in each actual probe/Core document,
and its Project attribute and complete `Exists('$(NuGetPackageRoot)...')` guard already equal the
original expected strings. The original PIV `compiler_policy.py` had a namespace-aware node helper;
this bounded adaptation lost that detail. This is a verifier defect, not an accepted SDK exception.
Python's ElementTree documentation confirms expanded namespace tags and exact `iter(tag)` matching:
`https://github.com/python/cpython/blob/main/Doc/library/xml.etree.elementtree.rst` (Context7 consulted).

`generated-import-inspection-second.json` checks **22 already-restored project contexts / 44 generated
documents**. All have the same valid namespace-aware canonical lookup and exact guards. Seven contexts
are explicitly not restored at their intended output locations; no claim is made for those yet.
`recorded-probe-input-inspection.json` reads both projects' actual recorded Csc inputs:

| Actual project | Sources | References | Analyzers | Configs |
| --- | ---: | ---: | ---: | ---: |
| Probe | 4 | 178 | 10 | 3 |
| Core | 315 | 177 | 14 | 3 |

Each recorded set equals its actual argument set without duplicates. Both record SDK 10.0.404,
warnings-as-errors, warning level 9999, analyzers during build, no compiler/analyzer skip and the actual
canonical package analyzers with EnableNETAnalyzers=false. This source inspection is not a new complete
compiler-proof capture; the remaining proof stages still require coordinator execution.

Canonical DLL SHA-256 identities, preserved unchanged:

- NetAnalyzers: `572bf5c83b409a919d01da199c63cc58c7f7cec6cb8037419075471b47cb84c6`.
- CSharp.NetAnalyzers: `cd27a044799acfc9e04c451488b75dd20cd49ded5fd2dc7c6ff2dac0656cf1d5`.

## Additive correction and bindings

`proof.py`, its original static seal, inputs, all candidate bodies, host metadata, `Capture.targets`,
the old lane script and the original preparation handoff retain their exact identities. No source,
hook, build, restore, test, formatter, native or device operation was performed during this correction.

`proof-v2.py` loads the source-hash-pinned original proof with three explicit, counted transformations:

1. The single generated-import lookup calls `generated_msbuild_imports(path, package)` instead of
   unqualified `ElementTree.iter('Import')`. The helper admits only the observed standard MSBuild
   namespace and canonical NetAnalyzers 9.0.0 import. It verifies the exact installed package-file
   identity, the unchanged Exists guard, the enclosing ExcludeRestorePackageImports guard and the
   canonical NuGet root declaration. It does not interpret arbitrary MSBuild expressions or rewrite paths.
2. The two capture/verify evidence-directory selections use an explicit **capture name**, independent
   of the original **compiler attempt**. The actual raw compiler workspace and build receipt stay unchanged.
3. New manifests identify v2, both identities and the versioned correction-receipt hash.

All original strict Csc membership, warning/skip, package/DLL/import-order, SDK, project-graph,
output/source and live-byte checks remain in the immutable base implementation. The exact original
Exists comparison also remains. The v2 receipt pins original inputs, new tools and the effective
transformed Python source; new capture tools are explicitly post-build verifier tools, not relabelled
as tools that ran during the original compilation. No C# or compiled hook change requires a rebuild.

`run-lane-v2.sh` is additive. Future fresh lanes retain the original hook and C# bodies while using
v2 for static/capture/live steps and distinct `*-v2` evidence names. This handoff supersedes only the
old preparation handoff's verifier/lane commands; the original sealed handoff remains unchanged.

## Preserved failures and classification

Before corrective tooling changed any verifier behavior, `preserve-capture-failure-v2.py` retained
**981 original text/binary identities**, including the successful build, complete partial failed capture,
generated NuGet documents/assets, original sealed tools and inputs, raw Csc text/source/configs,
canonical analyzers, compiler and actual successful outputs. Text is under `failure-preservation/`;
binary copies are external under the home runtime's `preserved-capture-failure-probe-first-v2/`.
The preservation utility explicitly handles the existing **capture failure after a successful build**;
it never invokes proof capture or treats a failed capture as a failed compilation.

The coordinator subsequently invoked v1 `retain-failure` against the successful build. That entry
accepts failed BUILDS only and correctly rejected the invocation. Its independent failed logger label,
stderr and the empty `evidence/probe-first-failed-build/` directory remain intact. This was invocation
misuse, not another SDK/product failure. A v2 capture-stage failure automatically retains its own
partial data under its new capture name; do not invoke the failed-build-only action for that case.

The first new source-inventory helper also failed before any proof execution: it mixed ElementTree
nodes from two parses when finding a parent. `source-inspection-failure/` preserves its original source,
reproduced stderr and exit 1, plus the untouched zero-byte first inventory output. The corrected helper
uses one tree for that parent mapping. This is a separately recorded static-tool error, not a second
SDK or product failure. No failure was converted into successful compiler evidence.

## Coordinator commands after independent correction review

Do **not** rebuild the probe or rerun its lane script with `first`. Preserve and reuse the actual
`probe-first` compiler workspace and successful `consumer-probe-build-first` receipt:

```bash
cd /home/rupicola/Code/y/yubikit-core-docs-integration
V=docs/research/documentation-audit/integrations/core-upstream-20261111/validation
P="$V/consumer-proof"
python3 -B "$P/proof-v2.py" static
python3 -B "$V/run-command-v2.py" --label consumer-probe-capture-second -- \
  python3 -B "$P/proof-v2.py" capture --context probe --attempt probe-first \
    --capture-name probe-first-v2 --build-label consumer-probe-build-first
python3 -B "$V/run-command-v2.py" --label consumer-probe-live-second -- \
  python3 -B "$P/proof-v2.py" verify --attempt probe-first --capture-name probe-first-v2 --live
```

Only after that actual representative capture and live proof are accepted, scale with the new wrapper:

```bash
bash "$P/run-lane-v2.sh" examples first
bash "$P/run-lane-v2.sh" annotations first
bash "$P/run-lane-v2.sh" consumers first
bash "$P/run-lane-v2.sh" mac-comparison first
bash "$P/run-lane-v2.sh" fixture first
```

Final live checks use the same actual attempt names and distinct evidence names:

```bash
for context in probe examples annotations consumers mac-comparison fixture; do
  python3 -B "$V/run-command-v2.py" --label "consumer-$context-final-v2-first" -- \
    python3 -B "$P/proof-v2.py" verify --attempt "$context-first" \
      --capture-name "$context-first-v2" --live
done
```

No new actual capture has been run by the correction owner. Static source verification supplies no
claim that later graph/output/native-binding stages pass. Any further genuine failure remains a new,
retained failure for review—not permission to weaken guards or to start the whole-Core audit.
