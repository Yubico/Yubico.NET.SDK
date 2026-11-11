# V4 decision: restore catalog is not the selected compiler/runtime target

## Decision and current status

Replace the single `restored_binary_bindings` collector, not the SDK, project source or existing input
guards. The bounded replacement uses actual emitted TFM/RID evidence to select one restored target,
binds every observed Csc reference/analyzer and resolved copy input to its proper owner, and separately
disposes unused catalog/build/download entries. It does not interpret arbitrary MSBuild or resolve
versions/RID fallbacks. This is a schema-model correction, not a fourth generic framework.

The final **read-only** matrix passes **22/22 complete compiler-project predicate paths**: 21 real
consumer/library/verification projects plus the traversal host. All 21 real-project graph predicates
also pass independently. **Actual v4 capture/live remains pending independent review and coordinator
execution.** Main gates and earlier successful examples/annotations are not rerun or relabelled.

## Original failure and root assumption

The successful `consumer-consumers-build-first` build compiled the actual seven tools, Mac HID route
host, all-module NativeAotVerification and their dependencies with zero warnings. Capture v3 failed
on `target[key]` for `runtime.linux-x64.Microsoft.DotNet.ILCompiler/10.0.15`.

The actual schema has distinct domains:

| Domain | Meaning / actual observation |
| --- | --- |
| `libraries` | Global restore catalog, with package content metadata and file lists; not one target's dependency membership |
| `targets[TFM]` / `targets[TFM/RID]` | A particular framework/runtime graph and its compile/runtime/native/build candidates |
| `project.frameworks[TFM].downloadDependencies` | Restore download declarations, including runtime/tool packs; not automatic Csc or application-runtime inputs |
| Actual Csc / copy items | The inputs actually supplied to this compilation/output transaction; these must be classified and content-bound |

Both AOT-capable verification hosts restore **`net10.0` and `net10.0/linux-x64`**, but their actual
ordinary builds are **RID-neutral net10.0**. The Csc-generated TargetFramework attribute, actual
intermediate/output paths and emitted `.deps.json` runtime target agree. The runtime ILCompiler catalog
row is present only in the RID target, has `hasTools: true`, and matches an exact download declaration.
It has no ordinary application's compile/runtime/native asset in the actual neutral target.

The old collector falsely assumed the global catalog and `targets['net10.0']` were bijective.
No product or SDK failure is established. This is not corrected with `except KeyError`, silent skipping,
or a union of all RID graphs. A used input missing from its actual selected target still fails.

NuGet's source model independently distinguishes `LockFileLibrary` from `LockFileTarget.Libraries` and
`LockFileTarget.RuntimeIdentifier`; Context7 `/nuget/nuget.client` was consulted. The selected SDK's
actual `Microsoft.PackageDependencyResolution.targets` supplies both TargetFramework and
RuntimeIdentifier to ResolvePackageAssets. That source, not a guessed host platform, backs selection.

## Preservation before correction

`consumer-preservation/manifest.json` binds **7,713 current original text/binary identities**, including
the successful build, exact raw observations, current compiled outputs and complete partial failed
capture. Binaries remain external under the home runtime's `preserved-consumers-first-v4/`.

The preservation tool also verified **6,939 prior retained record identities**, including the original
981 probe preservation records and successful Probe/Examples/Annotations evidence. It checks their
already retained bytes, not later obj/bin files that authorized compilations may replace. No old
capture, proof, input, receipt, hook, project, source body or review was rewritten. The failed-build-only
retention function was not called; the earlier invocation misuse remains separately classified.

`archived-success-shapes.json` reads the original retained restore snapshots for all nine project
observations in those three accepted captures. They have framework-only net10.0 target shapes without
catalog rows outside that framework graph. This is inspection of sealed evidence, not SDK replay or
re-capture of stronger proofs.

## Collector architecture and preserved constraints

`assets.py` replaces one asset-binding function. `proof-v4.py` pins accepted v3, preserves its namespace
and AdditionalFiles rules, and makes only two counted source transformations: call this collector for
the traversal host too, and identify new manifests as v4. The original source/reference/analyzer/config
uniqueness, exact PublicAPI multiset, policy, canonical NetAnalyzers, graph, source-membership, XML,
output-identity and live-byte guards remain unchanged.

The collector:

1. Selects the actual Csc TFM against restored framework/alias metadata. It retains all original restore
   frameworks and available targets. It corroborates RID with the emitted dependency runtime target,
   generated Csc TFM attribute and actual intermediate/output path suffixes; no missing RuntimeIdentifier
   policy field is invented. Ambiguous/missing evidence fails rather than choosing an arbitrary target.
2. Separates selected target nodes from the complete catalog. Every selected node must have consistent
   catalog metadata. Actual inputs owned by a package outside the selected target fail. Unused rows
   retain complete library/node membership metadata and explicit dispositions.
3. Binds actual compiler references to selected package compile assets, exact real-project reference
   outputs (also checked by the unchanged graph proof), declared Mac comparison Core DLL, or the real
   SDK reference-pack FrameworkList. Every actual analyzer binds a selected package analyzer file or
   an Analyzer entry in that same real framework-pack manifest. Unknown origins fail.
4. Preserves equality between selected package compile assets and actual package Csc references. It
   checks each actual resolved runtime/native/resource/project copy against its selected target file
   metadata and actual destination bytes. It does not silently ignore unowned copy paths.
5. Keeps the complete assets snapshot. Selected build/metadata-only rows, unused candidates, other
   target rows and downloaded tools are not counted as actual compiler/runtime inputs. Actual NativeShims
   presence remains required for real consumers. The two known ReferenceOutputAssembly=false traversal
   hosts are explicitly distinguished from SDK consumers; their real referenced projects are checked.

`download-dispositions.json` separately records all **eight download declarations** (four in each
AOT-capable host). Runtime-pack declarations without corresponding application catalog inputs remain
declarations, not claimed executed/copied assets. The two runtime ILCompiler rows have explicit
restore-only downloaded-tool dispositions and retain their package metadata.

### Hash domains — preserved draft preflight failure

The first v4 draft correctly stopped both AOT hosts because it incorrectly equated a package archive
hash with NuGet's content hash. `hash-domain-diagnosis/` preserves that exact draft source and failure
identity. This was an offline collector check, not another actual pipeline/build failure.

NuGet PackageExtractor writes the signed archive hash to `.nupkg.sha512`, but writes the package's
unsigned content hash to `.nupkg.metadata.contentHash`; lock-file catalog sha512 uses the latter here.
Source consulted: `https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Packaging/PackageExtractor.cs`.
The actual package metadata confirms these distinct values. The corrected collector verifies both
domains independently: version-2 content metadata equals the catalog; streamed archive SHA-512 equals
the declared archive hash. Metadata/nuspec text is retained. The unused archive's identity is recorded,
not loaded, executed or counted as a Csc/native artifact. No signature-trust qualification is inferred.

## Full available-context diagnosis and bounds

- `diagnosis-v3-first.json`: all 22 contexts explored; exactly the two ILCompiler catalog lookups fail.
  All 21 real-project graphs were checked even where the earlier asset check failed.
- `preflight-v4-final.json`: **22 complete project predicates, zero failures, 21 independent graphs**,
  with exact tool-source identities. The read-only collector applies actual file identity/text checks
  and returns `retained: null`, never creating a snapshot, binary copy, evidence folder or compiler manifest.
- `asset-shapes-first.json`: complete current framework/RID/catalog schema observations.
- Pending Mac comparison: no restore exists at its assigned context. The source's historical native
  default remains unchanged; its prepared host explicitly overrides Core path/native version/variant.
  The new collector has the declared direct-Core-reference classification, but no actual Mac capture
  result is claimed.
- Pending fixture: its main-gate restore is available and has only net10.0, no download declarations
  or target/catalog mismatch. The report retains its test/build-package shapes. Xunit's runner props
  declare literal `None` build-content copies; these are not misrepresented as normal Csc/runtime
  assets. Whole output identities remain captured by the original compiler proof. Actual fixture Csc,
  copy items and test policy must still be checked in its assigned compile-only lane.

All 22 observed compilations select neutral net10.0. Multiple restored TFM metadata is distinguished
from an actual inner compilation; no multiple-TFM compilation occurred. The existing hook has one
project-name capture key and rejects overwriting it. Supporting a new multi-inner-build capture transaction
would require an explicit assignment, not a claim that this packet already qualified it.

Complete output-tree hashes are retained by the unchanged base proof. Generated apphosts, content
copies and downloaded tools are distinct from the **recorded resolved runtime/native asset** arrays;
the collector does not pretend every emitted file is a NuGet runtime asset. There is no general
build-target interpreter. Capture does not qualify AOT publication/linking, Mac runtime behavior on
Linux, native execution or hardware interoperability. The separately completed Linux native lane stays
separate. No whole-Core or late Windows CCID/HID audit is performed.

## Coordinator commands after fresh independent review

Reuse **consumers-first** and its successful original build. Do not rerun SDK compilation for bookkeeping:

```bash
cd /home/rupicola/Code/y/yubikit-core-docs-integration
V=docs/research/documentation-audit/integrations/core-upstream-20261111/validation
P="$V/consumer-proof"
python3 -B "$P/proof-v4.py" static
python3 -B "$V/run-command-v2.py" --label consumer-consumers-capture-v4-first -- \
  python3 -B "$P/proof-v4.py" capture --context consumers --attempt consumers-first \
    --capture-name consumers-first-v4 --build-label consumer-consumers-build-first
python3 -B "$V/run-command-v2.py" --label consumer-consumers-live-v4-first -- \
  python3 -B "$P/proof-v4.py" verify --attempt consumers-first --capture-name consumers-first-v4 --live
```

After actual capture/live acceptance, only the two unexecuted contexts need fresh compilation:

```bash
bash "$P/correction-v4/lane.sh" mac-comparison first
bash "$P/correction-v4/lane.sh" fixture first
```

The successful earlier v3 examples/annotations remain their actual immutable v3 proofs; no SDK rebuild
or version relabelling is requested. Do not mutate their captured tool lists to accommodate a newer
post-build verifier. This worker ran only preservation/read-only source checks, not any v4 actual capture,
SDK build/test, formatter, native/device operation, product edit, staging, commit or delegation.
