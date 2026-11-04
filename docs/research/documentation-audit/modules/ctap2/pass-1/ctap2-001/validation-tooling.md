# CTAP2 validation tooling — preparation only

Baseline: `5b09a9b6942ded3913b5c9340c3e126617e61a82`. This task prepared new run-local
source and generic SDK-context templates. **No compilation, restore, test, hardware operation,
extractor execution, commit or delegation occurred. No validation gate or snippet count is claimed.**

## Retained paths and reuse

All paths below are relative to this run:

- `validation/checker-source/src/Program.cs`
- `validation/checker-source/src/Yubico.YubiKit.AuditCt001.Checker.csproj`
- `validation/checker-source/Directory.Build.props` and `Directory.Build.targets`
- `validation/checker-source/compile-examples.py`
- `validation/checker-source/prepare-project-context.py`
- `validation/checker-source/context/Directory.Build.{props,targets}.template`

These are new copies/adaptations of the immutable Management run's final `Program.cs`,
`compile-examples.py`, `prepare-project-context.py` and real-context host configuration. Read its
`validation.md`, `compiler-diagnostics.md` and `analyzer-context-correction.md` as mechanism provenance,
not CTAP results. No old manifests, extracted examples, generated assets, DLLs, logs, hardcoded
group membership, result counts or historical diagnostic drivers were copied. Management was not edited.

The helper project now has the unique discovery name **`AuditCt001.Checker`**, explicit `IsPackable=false`,
and no runner-shaped filename or `/tests/` path. Future hosts use
`Yubico.YubiKit.AuditCt001.Examples.<approved-id>.csproj` in `checker-source/context/<approved-id>/src/`.
Even when a host inherits genuine `IsTestProject=true` for xUnit policy/packages, its path/name does
not match repository test discovery. Never invoke a test target or host executable for this gate.
Generated outputs default to `/tmp/opencode/ctap2-001-doccheck` and
`/tmp/opencode/ctap2-001-<approved-id>-context`; retained source remains in this new run only.

The 2026-11-04 [discovery isolation follow-up](tooling-discovery.md) supersedes earlier filename-based
scope commands. Basenames no longer contain `Ctap2`; the checker keeps its existing DLL name through
explicit `AssemblyName`. Context IDs reject `ctap2`, `unittests` and `integrationtests` substrings.
This avoids this run's audit hosts in `--project Ctap2` without changing root discovery or old runs.

## Preservation checker

Both preservation commands use the full baseline hash, not the Management baseline:

- `tokens`: find changed and untracked C# under shipping/test/example/benchmark/verification roots;
  compare Roslyn C#14 token kinds and exact text, preserving literal text, directives, disabled branches
  and skipped syntax. Anchor significant trivia to token positions. Allow only directive-free using-list
  order normalization. Missing baseline/deleted/untracked executable source fails closed; an empty
  changed-source set refuses a vacuous pass. Research/checker/extracted-input paths are outside these roots.
- `api`: require the same nonempty set of all shipping `PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt`
  paths under `src` and byte-identical baseline/current content. This complements, not replaces, the
  eventual ordinary solution public-API analyzer gate.

No comments/XML semantics or protocol behavior are established by these comparisons. Generated C#
(`bin`, `obj`, `generated`, `.g.cs`, `.generated.cs`) is excluded explicitly. The token parser does not
semantically compile every preprocessor configuration; disabled branch text is nevertheless preserved.

## Two distinct compile-only mechanisms

### Real SDK/MSBuild hosts — primary for dependency-sensitive examples

After editors classify the complete inventory, the context adapter:

1. Requires an explicit, exhaustive, disjoint partition of selected groups into approved SDK contexts
   and `strict-raw`. No routing from failed compilation, guessed snippet groups or inherited old counts.
2. Starts from an explicitly named real source project; walks its actual `ProjectReference` package
   closure, preserves direct package metadata and source-project `Using` items, and imports canonical
   root props/targets and central package versions. No SDK implementation source or fake DLL is included.
3. Preserves supported compiler/framework/MTP properties. Sets an explicit compile-host output shape:
   `Library` for factored method/declaration wrappers, `Exe` for whole top-level programs or genuine
   xUnit3 generated entry points. It does not rewrite wrapper bodies. Split incompatible top-level
   programs into separate explicit hosts; do not combine them or drop entry points to hide failures.
4. References real Core/Ctap2 Release DLLs plus explicit additional module/DLL dependencies supplied
   by the classification. Retains exact wrapper/raw/document/configuration/project-closure/DLL hashes.
   Existing dependency DLLs must be freshly built by the coordinator before preparing the final snapshot.
5. Retains actual `Compile`, `ReferencePathWithRefAssemblies`, `Analyzer`, `EditorConfigFiles`, Csc
   command line, final executed compiler policy and assets hash when the coordinator later collects
   evidence after a fresh build. Collector success is input evidence, **not** a build-pass verdict.

Choose the real CTAP integration project for xUnit2/harness snippets and the real CTAP unit project
for xUnit3 snippets only when their actual dependencies match the classified example. Use the real
FidoTool or another explicitly reviewed consumer/sample source project for those dependencies. Naming
a project here is not assigning snippets. Additional modules/helper assemblies such as Management,
WebAuthn, Tests.Shared or a sibling helper must be explicitly inventoried, never silently included.

**xUnit2 and xUnit3 remain separate.** Both package closure and actual resolved reference inputs are
checked for mixing; xUnit3 cannot import Tests.Shared/SkippableFact/xUnit2. Normal NET/xUnit analyzers
and warnings-as-errors remain in force. The FINAL Management collector correction is incorporated:
`EnableNETAnalyzers=false` is accepted only with the centrally versioned canonical NuGet NetAnalyzers
DLL passed to Csc, while actual analyzer execution and `TreatWarningsAsErrors=true` are required.
The collector reads the current central package version, not an old hardcoded version/count.

No per-diagnostic suppression is added. Canonical root/package/SDK policy is imported unchanged;
source-project-local `NoWarn` is **not copied**. For example, FidoTool's local CA1852 relaxation is not
added to a host: this host is intentionally stricter at that boundary, not a claim of exact source-local
warning parity. Fix meaningful wrapper/doc diagnostics or report the bounded limitation. Normal SDK
redundant-reference warning handling is not copied into raw Roslyn or presented as an unsuppressed raw
result. Preserve failures and distinguish SDK-resolved success from raw compiler observations.

### Strict raw Roslyn — separate optional consumer check/diagnostic evidence

`compile` binds one complete retained source to real **Core + Ctap2** Release DLLs, explicitly supplied
additional SDK modules and `--reference <real.dll>` inputs. It excludes checker/Roslyn assemblies from
TPA binding, uses C#14/nullable/preview warning level 9999, collects full binding plus Emit diagnostics,
and fails on every warning or error. Emission is in memory: no emitted assembly is loaded or run.
Optional `--identity-comparer default|desktop` changes only identity comparison, never diagnostics.

`compile-examples.py` accepts only groups explicitly marked `context: "strict-raw"`, checks wrapper
and source-document hashes, prints commands/prerequisites/provenance, and fails the aggregate on any
failed group. Raw binding uses runtime TPA metadata, not SDK framework reference packs; it does not
run SDK/xUnit analyzers or MSBuild reference resolution and cannot certify project-context parity.
Test/sample-dependent groups therefore use their approved SDK contexts from the start.

## Required later extractor/configuration contract

F/G and A–E editing/classification still own the actual snippet inventory and extractor. No manifest
or guessed wrappers are supplied now. Retain complete fences/XML examples plus all explicit exclusions;
include current external references and document/block counts, locator anchors/ordinals/lines,
classification rationale, actual typed prerequisites, complete tasks/assertions/callbacks/cleanup and
ownership scopes. Do not remove calls, attributes, branches, tasks or disposal to obtain green results.

The extraction manifest uses the proven Management schema with fresh CTAP content:

- `primary_root` and `guide_root`: final integrated checkout absolute path.
- `groups`: nonempty unique `name`, retained `.cs` `source`/`source_sha256`, `provenance` list,
  `prerequisites` list, explicit `modules` and `references` lists, and **`context`** (`strict-raw` or
  an approved context ID). No `.UnitTests`/`.IntegrationTests` wrapper/project filenames.
- Each group retains `extraction.path`/`document_sha256` plus block/locator metadata;
  `raw_source`/`raw_sha256`, verbatim `extracted_text`/`extracted_sha256`, and explicit transformations.
- Retain complete exclusions and reference inventory hashes. Import relocation/whitespace changes
  require preservation review, especially multiline/verbatim literals; no guessed universal imports.

After classification, create `validation/checker-source/context/contexts-manifest.json` containing
`baseline` (full hash above), `extraction_manifest` (relative to that file), and `contexts`.
Each context explicitly provides `id` (lowercase letters/digits/hyphens, starting with a letter),
`kind` (`consumer`, `sample`, `xunit2`, `xunit3`), `source_project` (repository-relative actual project),
`output_type` (`Library` or `Exe`; xUnit3 requires `Exe`), nonempty `groups` (exact approved names),
and explicit extra `modules`/`references` lists. Build the full approved manifest before preparing hosts.
The adapter rejects incomplete, overlapping or inconsistent routing and stale original/raw/wrapped inputs.

For raw aggregation, provide a separately retained manifest containing only the explicitly routed
`strict-raw` group records, with source paths relative to that manifest. Reconcile its exact membership
with the origin manifest; there is no implicit raw subset selection. An empty raw partition needs an
explicit not-applicable record, not a vacuous passing invocation.

## Exact future commands — not run in preparation

Run from repository root, serialized with other toolchain work. `$CTX` is an ID from the later approved
manifest, not a guessed group. Use fresh log paths and retain complete toolchain run directories before
five-run retention expires. Require exit 0, `RESULT: PASSED`, **Warnings: 0** for each fresh host build.

```bash
RUN=docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001
TOOLS="$RUN/validation/checker-source"
CHECKER=/tmp/opencode/ctap2-001-doccheck/bin/Release/net10.0/Yubico.YubiKit.Ctap2.DocCheck.dll

dotnet toolchain.cs -- build --project AuditCt001.Checker --clean
dotnet "$CHECKER" tokens "$PWD"
dotnet "$CHECKER" api "$PWD"

# After final dependency builds and the later editor-owned extraction:
python3 "$TOOLS/prepare-project-context.py" --repo "$PWD" \
  --manifest "$TOOLS/context/contexts-manifest.json" --context "$CTX"
python3 "$TOOLS/prepare-project-context.py" --repo "$PWD" \
  --manifest "$TOOLS/context/contexts-manifest.json" --context "$CTX" --check
dotnet toolchain.cs -- build --project "AuditCt001.Examples.$CTX" --clean
python3 "$TOOLS/prepare-project-context.py" --repo "$PWD" \
  --manifest "$TOOLS/context/contexts-manifest.json" --context "$CTX" --collect

# Only if an approved nonempty strict-raw partition exists:
python3 "$TOOLS/compile-examples.py" --repo "$PWD" --checker "$CHECKER" \
  --manifest "$RUN/validation/compiled-examples/strict-raw-manifest.json"
```

The later extractor's own exact invocation cannot be specified until its owner supplies that script.
No build is hidden inside either Python preparer/checker/collector. A prepared context is one-shot;
repreparation/evidence refuses existing output. Retain earlier context/configuration/logs before selecting
a fresh approved ID after input changes. Ordinary dependency solution builds and all final repository
gates remain coordinator-owned, as described in `validation-plan.md`.

## Static checks performed and limits

Preparation verification used Python `ast.parse` (not Python bytecode compilation or tests) on both
scripts and ElementTree parsing on all project/props/targets/templates. Verified two exact full-baseline
constants, default `Core`/`Ctap2` binding, explicit project `IsPackable=false`, and no test-discovery-shaped
project path/name. These checks passed. They do **not** establish C# compilation or runtime validity.
All files in this task were written using the patch tool. Context7 `/dotnet/msbuild` was consulted on
2026-11-03 for target-order/input-capture mechanisms; the final Management working source and actual
future Csc inputs remain primary evidence for this adaptation.

Bounded adapter limits: only `Microsoft.NET.Sdk`/`net10.0`, unconditional package/project/using items,
root-only inherited configuration and the listed source compiler properties are modeled. Source
project imports, intermediate build configuration or conditional dependencies/properties fail for
explicit review rather than silently approximating context. Other source-local compiler properties,
generated sources, multi-targeting, unsafe examples, internal/friend API requirements or unusual package
resolution may require a focused adaptation after inventory. No assembly-name impersonation, analyzer
disablement, API replacement or new warning suppression is authorized. Captured item/policy evidence
must be considered together with a fresh successful build, not alone as proof of analyzer diagnostics.

Compilation/source inspection never establishes device support, interoperability, protocol conformance,
live registration/authentication, UP/UV/PIN/biometric/blob/reset behavior or Windows/macOS qualification.
No device operations are run for syntax proof.
