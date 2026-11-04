# CTAP2 compile-only validation — six contexts, 33 groups

> **Final retention update:** 101 generated binary/cache duplicate snapshots (40,827,466 bytes)
> are now outside Git at `/tmp/opencode/ctap2-001-omitted-build-artifacts`, resolved by the explicit
> `validation/compiled-examples/retention-overlay.json`. All original failure manifests remain
> byte-identical. The 138 archived text records and single pinned 645,594-byte xUnit nupkg proof
> remain durable. “Entire workspace” below describes the historical initial archive, not current
> in-repository binary storage. Final six PASS results/compiler snapshots are unchanged.
> See [validation-retention.md](validation-retention.md) for policy, counts and fresh verification.

2026-11-04. **All six clean SDK/MSBuild context host builds PASSED, Warnings0; all 33 selected
groups compiled and all six actual compiler-input/policy collections are retained and verified.**
No SDK test, snippet/host/helper assembly, sample CLI or hardware operation was executed. No source
SDK/content documentation, root compiler policy, immutable Management record, commit or delegation
was changed/performed. This is offline compilation, not device/RP/protocol/platform qualification.

PRIMARY is both extraction roots: `/home/rupicola/Code/y/Yubico.Yubikit.NET`.
Baseline: `5b09a9b6942ded3913b5c9340c3e126617e61a82`.
This follow-up supersedes preparation-only status, not its immutable failure evidence.

## Authority and prerequisites

The coordinator froze/formatted all final source and granted this exclusive helper/context build
lane. Read the preserved failure and actual extractor/host/scripts before fixing; loaded
workflow-debug and domain-build. Only canonical `dotnet toolchain.cs -- build ... --clean` was used
for .NET work. Its build-time declaration/AOT checks are not SDK test execution.

Fresh dependency provenance is the coordinator's retained
`validation/20261104-013307-468-build+test+docs-qa+docs-architecture+complexity/result.json`:
clean full gates passed, **3,998 unit cases, 492 CTAP cases, zero warnings**. Those tests were run by
the coordinator, **not this follow-up**. Formatter import changes remain separately accounted in
the coordinator's format/token records; this task changes no SDK runtime/API or documentation.
Final extractor materialization captured the current formatted document/API/project/reference
hashes before fresh host preparation. Actual DLL hashes are now retained by each context snapshot
and actual Csc input collector, not inferred from the earlier provisional DLL existence observations.

The coordinator's `consumer` → **`consumer-lib`** rename was preserved in selection/configuration.
The exact selector now matches one project, not other `consumer-*` contexts. All six toolchain logs
show exactly one matched host. No archival `*.csproj` duplicate remains under the repository.

## Root cause, preserved failure and minimal correction

Original `validation/context-runs/consumer-lib.log` and copied `20261104-014251-526-build` run
remain untouched. `guide-protocol/complete.cs` began with a resource declaration:

```csharp
using var protocol = new PinUvAuthProtocolV2();
```

`extract.py:99` incorrectly treated every initial line starting `using ` and ending `;` as an
import. It moved that resource declaration before file imports, leaving only the disposal comment
inside `Run`. The actual build failed CS1529 at generated lines2–5. This was an extractor/scope
defect, **not a printed example/API defect**. A static pre-fix reproduction confirmed that the entire
resource declaration was returned as imports and the method body became empty.

Before any correction/host deletion, `archive-using-hoist.py` preserved **91 exact files** covering
all failed extracted/raw/wrapped inputs, scripts/configuration/templates, generated host and original
logs. Every source/destination/hash mapping is in:

```
validation/compiled-examples/failure-using-hoist/archive-manifest.json
```

The archived host is named `Yubico.YubiKit.AuditCt001.Examples.consumer-lib.csproj.snapshot`.
`preserve-failed-workspace.py` also preserved the **entire 19-file failed MSBuild workspace**,
including assets/generated sources/reference/analyzer captures, with a separate exact path/hash map:

```
validation/compiled-examples/failure-using-hoist/workspace-manifest.json
```

Only the known generated consumer-lib host files were deleted via patch after preservation; only
its empty directories were removed. No SDK/user directory was removed. Other contexts did not yet
exist. All six were subsequently prepared with fresh snapshots against the corrected inputs.

The extractor now relocates only a bounded actual namespace/qualified-type alias/static using
directive prefix. Resource `using var`, typed resource declarations, `using (...)` and `await using`
remain body statements. Unsupported future complex directive syntax is not guessed/hoisted. No
resource declaration was converted into an await, removed or replaced by a helper. The original
protocol scope now encloses its original body in `Run` and disposes at the original method boundary.

Strengthened static checks cover resource declarations/statements after namespace, alias, static
and global directives and verify **every one of the 33 inputs**, not just the failed file.
Comparison with the preserved failure establishes:

- **All 95 original extracted/verbatim blocks unchanged.**
- **All 33 readable.raw files unchanged.**
- **Only one wrapped source changed: guide-protocol**, restoring its resource to its owning method.
- Every useful task, branch, assertion, callback, PIN/token/auth clearing and async disposal body
  remains exact after only legitimate initial import relocation.
- **39 module fences/five XML blocks**, 95 total inventoried blocks, **39 selected/56 excluded**, same
  **33 groups/six contexts**. No group was dropped/reclassified after compiler failure.

## Actual build and input-collection results

| Context | Groups | Clean build | Warnings | Csc sources / references / analyzers | Input collection |
|---|---:|---|---:|---|---|
| consumer-lib | 19 | PASSED | 0 | 22 / 180 / 10 | PASSED |
| consumer-demo | 1 | PASSED | 0 | 4 / 180 / 10 | PASSED |
| unit-v3 | 4 | PASSED | 0 | 10 / 199 / 12 | PASSED, focused package-source supplement |
| hardware-v2 | 4 | PASSED | 0 | 8 / 207 / 12 | PASSED |
| consumer-wa-demo | 1 | PASSED | 0 | 4 / 181 / 10 | PASSED |
| consumer-wa-lib | 4 | PASSED | 0 | 7 / 181 / 10 | PASSED |

Csc source counts include real SDK/package/generated items in addition to the selected wrappers.
Each host preserves canonical root compiler/analyzer policy and its genuine source-project package
closure. v3 MTP and v2/SkippableFact/Tests.Shared remain separated in both source package closure and
actual resolved references. The real public integration helper is referenced from its actual DLL,
not copied/reimplemented. Consumer output shapes remain Library/whole top-level Exe as classified.

All actual policies retain `TreatWarningsAsErrors=true`, `RunAnalyzersDuringBuild=true`, C#14,
net10.0/SDK10.0.403, warning9999, and the appropriate Recommended/AllEnabledByDefault analysis mode.
`EnableNETAnalyzers=false` occurs in all six **because the canonical NuGet NetAnalyzers9.0.0 DLL
replaces the SDK copy**; actual analyzer paths/execution policy were checked. This is not analyzer
disablement. No NoWarn, diagnostic suppression or source-project-local relaxation was added.
Actual SDK-default redundant-reference policy is captured, not copied into raw Roslyn.
**No raw compilation was run and no claim is made that raw CS1701 disappeared.**

Each context retains `context-manifest.json` and `evidence/` under
`validation/checker-source/context/<id>/`: complete Compile/resolved-reference/analyzer/editorconfig
items, Csc command line, final policy, assets hash, DLL/input hashes and original snapshots.
`verify-validation.py` freshly reconciles all these paths/hashes with successful retained builds.
Input evidence alone is not a build verdict; the clean toolchain verdict is also required and present.

### Preserved xUnit v3 collector failure and strict supplement

`compile-contexts.py --attempt after-using-hoist` built **all six successfully**, collected five,
then returned aggregate exit1 because the generic v3 collector rejected an external Compile source:

```
/home/rupicola/.nuget/packages/xunit.v3.core/3.0.0/_content/DefaultRunnerReporters.cs
```

The failure is retained in `context-runs/after-using-hoist/unit-v3.log` and unchanged
`after-using-hoist/results.json`. It is **not a compiler failure**. The generic collector and all
five successful evidence sets remain unchanged.

Inspected actual pinned package targets and complete reporter source. `xunit.v3.core.targets:77–81`
explicitly includes this source before CoreCompile. It is ten lines of real runner registration
metadata, not a fake test harness or an unexpected SDK implementation. Context7 was consulted, but
its current-template result did not establish this pinned file; actual 3.0.0 package/targets/asset
evidence is authoritative. No package was changed/restored for this correction.

`collect-unit-package-source.py` is a focused independent collector supplement in this new run,
**not a generic collector edit or broad external-source allowance**. It retains all existing
input/policy/reference/xUnit-generation/NET+xUnit analyzer checks and permits exactly that one item
only after proving the central v3 version, exact assets package path, target Compile declaration,
actual Csc membership and byte equality of both source and targets against the pinned nupkg.
Source/targets/nupkg and the complete prepared host/intermediate failure inputs were archived before
collection under `failure-xunit-package-content/`, with `.snapshot` project names and full path/hashes.
The successful supplement has its own fresh log, script hash and package-content proof in v3 evidence.
No recompilation was needed: the already fresh v3 build/input snapshots were unchanged, verified by
hash before reading, and its actual build was PASSED/Warnings0. No emitted assembly was loaded/run.

## Exact commands and retention

Run from PRIMARY, serially under the exclusive lane:

```bash
RUN=docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001
EXAMPLES="$RUN/validation/compiled-examples"
python3 "$EXAMPLES/archive-using-hoist.py"
python3 "$EXAMPLES/preserve-failed-workspace.py"
# Patch extractor/verification and remove only preserved generated host files; rmdir empty dirs.
python3 "$EXAMPLES/extract.py" --materialize
python3 "$EXAMPLES/verify-inputs.py"
python3 "$EXAMPLES/compile-contexts.py" --attempt after-using-hoist
python3 "$EXAMPLES/collect-unit-package-source.py"
python3 "$EXAMPLES/verify-validation.py"
```

The runner's exact per-context commands are retained in each context log:

```bash
python3 "$RUN/validation/checker-source/prepare-project-context.py" --repo "$PWD" --manifest "$RUN/validation/checker-source/context/contexts-manifest.json" --context "$CTX"
python3 "$RUN/validation/checker-source/prepare-project-context.py" --repo "$PWD" --manifest "$RUN/validation/checker-source/context/contexts-manifest.json" --context "$CTX" --check
dotnet toolchain.cs -- build --project "AuditCt001.Examples.$CTX" --clean
python3 "$RUN/validation/checker-source/prepare-project-context.py" --repo "$PWD" --manifest "$RUN/validation/checker-source/context/contexts-manifest.json" --context "$CTX" --collect
```

The generic unit-v3 collect line remains exit1 as explained above; its supplement command exits0.
The final reconciliation exits0 and separately reports **six clean build passes/six verified
collections**. Never rerun the one-shot archive/collector or overwrite existing evidence; select
a fresh reviewed attempt/context snapshot after any future input change.

Complete run directories were copied immediately, before five-run retention could delete earlier
runs, into `validation/compiled-examples/context-runs/after-using-hoist/`:

```
consumer-lib-20261104-014940-722-build
consumer-demo-20261104-014942-728-build
unit-v3-20261104-014944-861-build
hardware-v2-20261104-014947-005-build
consumer-wa-demo-20261104-014949-112-build
consumer-wa-lib-20261104-014951-198-build
```

The aggregate closeout record is `validation/compiled-examples/context-runs/final-result.json`.
Final extraction manifest SHA256: `6402578ca65eb04f2f24e3e35d692363c2f4e548458c9d9ceddaf784fa2f7376`.
Extractor SHA256: `0f149ce6c21ff6a8780c263c98fefaef4fdbb71e9723f0e22bc58e61c8abaa2e`.
Context configuration SHA256: `d97b6d3fe7463a942975082dda81eea299ff1f436a19024e31cf98d512620511`.

## Remaining limits / handoff

**No remaining printed-example/API/compiler blocker was observed.** No scaffolding beyond the
resource-hoist correction was needed; no fake awaits/APIs/helpers or removed groups were used.
The generic collector's v3 package-content limitation remains explicit; the current exact v3
evidence is collected by the strict retained supplement, not hidden by a broad validator relaxation.

Successful compilation does not establish runtime ownership cleanup, complete RP registration/sign-in,
protocol/crypto conformance, device capabilities, hardware interaction, transport/platform coverage or
final whole-documentation campaign acceptance. The 56 explicit shell/sketch/internal/historical
exclusions remain excluded, not passing no-op stubs. No tests, host/helper assemblies, snippets,
PIN/UP/UV/bio/blob/reset workflows, commits or delegation were executed by this follow-up.
All synchronous build/collection work is finished; the exclusive .NET lane is idle for coordinator handoff.

### Durable evidence retention policy

All meaningful original raw/wrapped/source/script/host metadata and actual Csc/reference/analyzer/
policy text remains in the run, including historical generated text at archived obj paths. These
paths are historical evidence, not active build outputs. No text was relocated. The single pinned
`failure-xunit-package-content/files/005-xunit.v3.core.3.0.0.nupkg.snapshot` is explicitly retained
as **PRIMARY-PACKAGE** proof, not generated runtime output; its source/targets byte equality is
verified offline. All other compiled/native/apphost/PDB/cache/marker duplicates are externalized
with original relative path, hash, byte size, reason and exact current location. Every original
failure record resolves through that overlay; missing files without a disposition fail closed.
No final compiler-input, body, host, configuration, analyzer policy or six-PASS result hash changed.
