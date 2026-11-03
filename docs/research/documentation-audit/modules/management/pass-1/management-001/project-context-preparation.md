# Project-context compilation preparation

## Status and scope

Prepared **one real SDK/MSBuild xUnit2 compilation host for 26 groups**, plus a **37-group strict-raw
manifest**. Together they partition all 63 selected examples without overlap or omissions. Raw CS1701
failures and the bounded comparer investigation remain intact as a separate outcome; no new comparer
experiments or warning suppressions were introduced.

**No build, restore, compiler invocation, MSBuild evaluation, test, snippet or hardware execution was
performed in this preparation.** The coordinator is running serialized final SDK gates/formatting.
Only Python preparation/hash checks and static Python/XML inspection are preparation evidence here.
No shipping solution, SDK/test/example implementation, or SDK documentation file was edited.

Prepared hashes are a snapshot. **After final SDK Release builds and formatting finish, rerun extraction
and context preparation before the host build**, so source documents, wrappers, policy and actual DLLs
all have final hashes. An initial preparation correctly refused a stale `src/Management/tests/CLAUDE.md`
document hash; re-extraction refreshed it. Staleness is not silently accepted.

## Ready paths

All paths below are beneath this run's `validation/`:

| Path | Purpose |
| --- | --- |
| `checker-source/prepare-project-context.py` | Prepare the explicit partition and package/source/reference items; `--check` validates snapshot hashes; `--collect` retains actual post-build Csc input evidence. Never invokes a build/test. |
| `checker-source/xunit2-context/src/Yubico.YubiKit.Management.DocExamples.Xunit2.csproj` | Compile-only host; real `Microsoft.NET.Sdk`, `net10.0`, test-context policy, not in shipping solution. |
| `checker-source/xunit2-context/Directory.Build.props` | Imports canonical **root** props, bypassing the standalone Roslyn utility's isolated props; sets test context early and output/intermediate paths under `/tmp/opencode`. |
| `checker-source/xunit2-context/Directory.Build.targets` | Imports canonical root analyzer targets, validates prepared hashes, captures actual compiler source/reference/analyzer lists, final policy and Csc arguments. |
| `checker-source/xunit2-context/Context.Packages.props` | Generated exact package items/metadata from the real integration project and its genuine project-reference package closure, plus its `Using Include="Xunit"`. |
| `checker-source/xunit2-context/Context.Items.props` | Explicit 26 wrapper-source items and six real Release DLL references; no SDK implementation source or project references. |
| `checker-source/xunit2-context/context-manifest.json` | Explicit 63-group routing/membership; source/document/configuration/reference hashes; package provenance and original project closure. |
| `checker-source/xunit2-context/strict-raw-manifest.json` | Exact remaining 37 original group records, rebased source paths only; root xUnit3 remains in this route. |

Default generated workspace:

`/tmp/opencode/management-001-xunit2-context/`

`BaseIntermediateOutputPath` and `MSBuildProjectExtensionsPath` are set early in the host's nearest
`Directory.Build.props`, before SDK extension/default paths. NuGet assets, generated metadata/global
usings, native/package copies and `bin` output therefore remain outside retained source records.
The host-specific `ManagementExamplesWorkspace` environment property can select a different absolute
`/tmp/opencode/...` workspace; pass the matching path to collection.

## Exact commands — coordinator, after serialized final gates

Run from final PRIMARY. Load `domain-build` before the build. **Do not run these build/compiler commands
concurrently with the coordinator's current SDK gates.** This report prepares them, not executes them.

```bash
RUN=docs/research/documentation-audit/modules/management/pass-1/management-001
CONTEXT="$RUN/validation/checker-source/xunit2-context"
CHECKER=/tmp/opencode/management-001-doccheck/bin/Release/net10.0/Yubico.YubiKit.Management.DocCheck.dll

# Final integrated docs and final Release DLL snapshot, after formatting/SDK builds:
python3 "$RUN/validation/compiled-examples/extract-examples.py"
python3 "$RUN/validation/checker-source/prepare-project-context.py"
python3 "$RUN/validation/checker-source/prepare-project-context.py" --check

# Exactly one isolated documentation host; compiler + real analyzers, no test execution.
# --clean forces fresh compilation instead of accepting stale/incremental capture files.
dotnet toolchain.cs -- build --project Management.DocExamples.Xunit2 --clean

# ONLY after the host RESULT is PASSED with Warnings: 0:
python3 "$RUN/validation/checker-source/prepare-project-context.py" --collect

# Separate strict raw route: all 37 remaining groups must have zero compiler warnings/errors.
python3 "$RUN/validation/checker-source/compile-examples.py" \
  --repo "$PWD" --checker "$CHECKER" \
  --manifest "$CONTEXT/strict-raw-manifest.json"
```

Read each exit status and the toolchain RESULT block; do not mask failure with an unguarded `tee`.
Retain the host's toolchain run logs/result/step output before the five-run retention window expires.
If the host fails, retain diagnostics and correct/review actual documentation or proven scaffolding
defects; **do not** add suppressions, disable analyzers, drop statements or replace dependencies.

For a different temporary workspace, prefix only the host build with
`ManagementExamplesWorkspace=/tmp/opencode/<unique-name>` and use
`prepare-project-context.py --collect --workspace /tmp/opencode/<unique-name>`. No SDK build is triggered
through a project reference: the host has **none**, and uses explicit final Release metadata references.

Collection intentionally refuses to overwrite an existing `xunit2-context/evidence/resolved-inputs.json`.
Retain previous evidence separately before collecting another successful run. Collection does not itself
prove a warning-free build; its output explicitly requires the corresponding toolchain PASS/zero-warning
RESULT. Do not execute the produced assembly, `dotnet test`, or a runner target.

## Actual context alignment, with no new analysis exceptions

Source of truth:

`src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/Yubico.YubiKit.Management.IntegrationTests.csproj`

- Same `Microsoft.NET.Sdk`, `net10.0`, `IsTestProject=true`, `IsPackable=false`.
- `IsTestProject=true` is set **before** importing root props, so the existing test-only
  `AnalysisMode=Recommended` and existing root test naming/analysis conventions apply. This mirrors
  the real integration project; it is not an added suppression list.
- Root props retain C#14, nullable, implicit usings, `AnalysisLevel=preview` / warning level 9999,
  `TreatWarningsAsErrors=true`, `EnableNETAnalyzers=true`, and `RunAnalyzersDuringBuild=true`.
- Root targets import the actual centrally versioned NET analyzer package. The real xUnit2 package
  imports its real xUnit analyzers. Nothing sets `RunAnalyzers=false`, `SkipAnalyzers`,
  `SkipCompilerExecution`, or a custom `NoWarn`/diagnostic severity override.
- `Context.Packages.props` mirrors the integration project's seven direct package references, retaining
  metadata such as runner/collector PrivateAssets/IncludeAssets. Its genuine Core/Management/Tests.Shared
  project-reference closure contributes eight additional distinct package items, preserving the
  dependency context otherwise lost by replacing ProjectReferences with final-DLL references.
  No alternate version is supplied: versions come from actual root `Directory.Packages.props`.
  `package_origins` and all four original project paths/hashes are in the manifest. Unsupported future
  conditional project/package items fail preparation rather than being flattened incorrectly.
- Existing SDK redundant-reference-warning policy is applied **by actual SDK/MSBuild targets** after
  normal framework reference-pack and NuGet/reference resolution. The raw Roslyn checker is unchanged
  and still exposes CS1701. The host does not copy a `1701`/`1702` suppression into project XML.
- Only instrumentation is added: `ProvideCommandLineArgs=true` records actual Csc arguments while
  normal compiler/analyzer execution remains enabled. No root build policy is duplicated or weakened.

The package closure is flattened only as package references, not source. The host does not inherit
Core's unsafe-code settings or copy Tests.Shared implementation; it inherits the **test consumer's**
actual analysis policy and binds to final DLLs. It deliberately omits the original test runtime
`xunit.runner.json` content, because no runner is invoked. Standard Microsoft.NET.Test.Sdk may set
OutputType=Exe and include its genuine package-supplied entry-point source, as it does for the real
integration project; that boilerplate is captured/hashes recorded, never copied/replaced/executed.

There are no module-specific `.editorconfig` files below Management; the existing root `[*.cs]`
configuration applies to the retained linked sources, with existing severities unchanged.

## Explicit group reconciliation

The xUnit2 host membership is fixed and explicit, not inferred from whether a future compile fails:

```text
guide-01 guide-02 guide-03
guide-13 guide-14 guide-15 guide-16 guide-17 guide-18 guide-19
module-tests-1 module-tests-2
shared-helper-xml-1 shared-helper-xml-2 shared-helper-xml-3
shared-attribute-xml-1 shared-attribute-xml-2
shared-readme-1 shared-readme-4 shared-readme-5 shared-readme-6 shared-readme-7
testing-11 testing-12 testing-13 testing-15
```

All 26 are linked from the **exact retained** `compiled-examples/<group>.cs` files. Default Compile
globbing is disabled; `Context.Items.props` lists every wrapper explicitly. No original method body,
attribute, await, assertion, callback, configuration/reset task, finally or disposal was changed.
Each group's namespace/class isolation remains exactly as extracted.

The remaining **37 names are explicitly listed** in `context-manifest.json.strict_raw_groups` and
`strict-raw-manifest.json.groups`. This includes the root xUnit3 SCP example, all consumer/sample groups,
and the unchanged Oath helper declarations that already belong to the strict-raw route. Their source
hashes and original extra references remain unchanged; xUnit3 is not mixed into this host.

Six explicit real Release references are Core, Management, Tests.Shared, SecurityDomain SDK, the public
SecurityDomain integration-helper assembly, and Oath. The current 26 host members do not themselves call
the two Oath helper declarations; those stay in the 37-group raw route with the actual Oath reference.
The host nevertheless records/references the requested real Oath DLL, not a surrogate type.
Root xUnit2/core/assert/SkippableFact dependencies are resolved by the actual centrally-versioned NuGet
graph, not by arbitrary extra DLL paths or a fake assembly. All source/reference preparation hashes
and extraction-document provenance are retained in the context manifest.

## Final evidence capture and proof limits

Before compilation, the host runs `prepare-project-context.py --check` as an input guard. It rejects
changed wrapper/document/policy/project/package/reference hashes and instructs the coordinator to rerun
preparation after final integration/builds. A source change cannot silently reuse the prepared snapshot.

Actual Csc inputs are recorded under the temporary workspace:

- `compile-inputs.txt`: final Compile items, including standard generated metadata/program source.
- `resolved-references.txt`: **actual Csc ReferencePathWithRefAssemblies**, with identities when present.
- `analyzers.txt`: actual analyzer DLL inputs.
- `csc-command-line.txt`: real Csc arguments, not guessed switches.
- `policy-final.txt`: SDK/framework/language/warning/analyzer state and final NoWarn after SDK handling.

After a successful build, `--collect` copies these into retained `xunit2-context/evidence/` and produces
`resolved-inputs.json` with SHA-256 for every actual compiled source/reference/analyzer, captured file,
prepared manifest and restored `project.assets.json`. It verifies warnings-as-errors and analyzers
remain enabled, the actual 26 wrappers are included, required real Release references reached Csc,
and the genuine xUnit/NET analyzer metadata is present. Unexpected source outside the generated
workspace is rejected, except the specifically recognized genuine Microsoft.NET.Test.Sdk program file.

**Not yet proven:** restore/evaluation/compilation of this newly prepared host, analyzer acceptance of
the docs/scaffolding, actual SDK reference resolution, command-line capture timing, 37-group final raw
success, or the final all-63 gate. Static XML/Python checks cannot establish any of these. If metadata
references/packages produce additional genuine diagnostics, preserve them for evidence-based correction
without suppressing them. A warning-free host build under existing SDK policy does not erase raw CS1701,
prove runtime assembly loading, execute assertions, or establish hardware/firmware/restoration safety.

The host is **not in the shipping solution**. Its own `.csproj` explicitly says `IsPackable=false`.
The toolchain can discover it for the scoped build because its path contains `/src/` and its name has
the SDK prefix. It cannot match current unit/integration discovery: its own path contains **no `/tests/`**
segment and its own filename contains neither `.UnitTests` nor `.IntegrationTests`. Referenced DLL names
inside XML do not alter project discovery. No test target or execution command is proposed.

Context7 `/dotnet/msbuild` was consulted for Directory.Build import order, early intermediate/extension
paths and root import lookup. The original source project, canonical props/targets/packages and exact
prepared sources—not a hand-written weaker analyzer profile—remain the context provenance.

## Static preparation checks

- Python AST parsing and XML parsing passed for the script and all five host project/props/targets files.
- Source hashes reconciled **26 SDK + 37 raw = 63**, with disjoint explicit membership and root xUnit3 raw.
- Confirmed 26 exact Compile items, six real DLL references and no ProjectReference or Compile glob.
- Confirmed all seven original package items retain their exact attributes/metadata, plus the eight
  genuine dependency-closure package items. No new NoWarn, WarningsNotAsErrors or analysis disablement.
- Confirmed the project's current path/name cannot match unit/integration discovery and explicitly
  excludes packing; scoped whitespace/fences passed and no retained obj/bin/__pycache__ was created.
- Preparation and `--check` succeeded on the current snapshot. These are not MSBuild/compile passes.
