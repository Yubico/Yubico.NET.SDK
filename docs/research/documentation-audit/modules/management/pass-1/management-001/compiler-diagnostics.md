# Compiler diagnostics investigation

## Outcome / exact remaining blocker

The checker is usable and remains strict, but **the raw Roslyn xUnit v2 groups are not warning-free**.
The CS1701 diagnostic is a real pre-existing dependency-context warning, not a Management API failure:
`Xunit.SkippableFact` references strong-named `xunit.core` **2.4.0.4049**, while the real project resolves
**2.9.3.0**. Changing only to csc's desktop assembly-identity comparer does **not** remove it.

No warning suppression, diagnostic downgrade, analyzer disabling, dependency replacement, synthetic
assembly, forged binding policy, deleted attribute/callback/task, or copied SDK implementation was used
to obtain green results. The helper still exits **1** for CS1701. No supported non-suppression identity
configuration that removes this warning was identified in the bounded experiment; this is not an
exhaustive claim about all conceivable loader configurations.

The correct next gate for the affected groups is **real SDK/MSBuild project-context compilation**, with
normal reference resolution, inherited policies, and analyzers. Its result must be distinguished from
“raw Roslyn emitted no warnings.” Existing SDK policy already omits redundant CS1701/CS1702 compiler
messages after MSBuild reference resolution. Copying that omission into this raw checker would skip
the corresponding resolution context and manufacture an unjustified green result; it was rejected.
The final all-meaningful-example project-context gate remains pending, not completed by this work.

## Authorized activity and preserved inputs

Used `workflow-debug`: reproduced first, compared actual policy/dependencies, tested a single identity
variable, then strengthened warning coverage after direct evaluation found another real policy mismatch.
Loaded `domain-build`; every compilation of the utility itself went through the repository toolchain.
Only the isolated helper was built. No SDK/test project compilation, test execution, snippet execution,
hardware, source/docs edits, commits, dependency changes or delegation occurred. Toolchain's automatic
source-declaration/native-AOT contract QA targets ran as part of helper builds, not hardware tests.

- Coordinator's full initial attempt remains `validation/snippets-preliminary.log`.
- Original extracted inputs remain untouched in `validation/preliminary-examples/`.
- Regenerated `validation/compiled-examples/` against **final integrated PRIMARY**, using default roots:
  **63 groups / 83 exclusions**. Its manifest now identifies PRIMARY for both input roles.
- Failed/successful focused attempts have separate exclusive-created logs; none was overwritten.
- All three helper build run folders were copied to `validation/compiler-toolchain-evidence/` before
  normal five-run retention could remove them. Generated helper `obj`/`bin` remain outside the repo.

## Root-cause evidence

### Actual SDK and project policy, not assumptions

`validation/compiler-policy.log` and the expanded `compiler-policy-complete.log` retain exact read-only
commands, evaluated properties, installed target-file SHA-256 values/excerpts and package-assets evidence.
`dotnet msbuild -getProperty:...` was used **without any target**: evaluation only, no build or restore.

| Actual input | Observed policy |
| --- | --- |
| Selected SDK | `10.0.403`, selected under repository `global.json` roll-forward |
| Installed compiler | `5.9.0-1.26523.112 (5a2162bea6845eabc1fc721624a43568c65db3b1)` |
| Real Management integration project | `net10.0`, `LangVersion=14`, `TreatWarningsAsErrors=true`, `AnalysisLevel=preview`, `AnalysisMode=Recommended`, **`WarningLevel=9999`** |
| Evaluated repository `NoWarn` | Existing CS1591/CA/test conventions; no CS1701/CS1702 in the evaluation-only result |
| Installed `Roslyn/Microsoft.CSharp.Core.targets:70–75` | Inside `CoreCompile`, appends `1701;1702` to `NoWarn`; comment explains MSBuild handles reference resolution and duplicate compiler messages would be redundant |
| Same installed target, line 113 | Passes `DisabledWarnings="$(NoWarn)"` to Csc |
| `Microsoft.NET.Sdk.CSharp.props:15` | Also provides `1701;1702` if NoWarn is initially empty; repository props make it nonempty in this checkout |

The evaluation-only result therefore must **not** be misread as the final Csc warning policy: the
relevant append occurs when `CoreCompile` executes, not during property evaluation. Installed SDK
files were inspected and hashed, never edited.

Real `obj/project.assets.json` (path/hash retained in policy logs) resolves:

- `xunit/2.9.3` → `xunit.core/2.9.3` → `xunit.extensibility.core/2.9.3` (real `xunit.core.dll`).
- `Xunit.SkippableFact/1.5.23` → `xunit.extensibility.execution` dependency `2.4.0`.
- Actual DLL identities in CS1701 are 2.4.0.4049 versus 2.9.3.0, with the **same** public-key token.
- The extractor already supplied the actual project-selected Release DLLs, not an accidentally selected
  xUnit v3 core, arbitrary NuGet fallback, or fake helper. Reference paths/hashes are in every compile log.

Using the older xUnit core instead would no longer match the real Tests.Shared/current test project.
Including two conflicting identities or inventing a .NET Framework binding redirect would not establish
the real .NET 10 project/runtime context. Those dependency/policy substitutions were not attempted.

### Raw Roslyn versus csc comparer

The starting helper used `new CSharpCompilationOptions(...)`, which selects
`Microsoft.CodeAnalysis.AssemblyIdentityComparer`. Roslyn's csc source explicitly selects
`DesktopAssemblyIdentityComparer.Default` and passes it through `WithAssemblyIdentityComparer`.
The helper now exposes a diagnostic-only choice:

```text
--identity-comparer default
--identity-comparer desktop
```

Default behavior stays the plain comparer. Both modes print actual comparer type and diagnostic-policy
counts. **Specific diagnostic overrides remain zero in every experiment.** Only the comparer was varied
in the initial comparison; DLLs and preserved snippet sources were identical.

| Preserved preliminary input | Plain comparer | csc desktop comparer |
| --- | --- | --- |
| `guide-01` | exit 1, CS1701 | exit 1, same CS1701 |
| `shared-readme-6` | exit 1, CS1701 | exit 1, same CS1701 |
| `shared-readme-5` | exit 1, CS1701 + CS0117 | exit 1, same diagnostics |
| root `root-scp-v3` | exit 0, no warnings/errors | exit 0, no warnings/errors |

Evidence: `compiler-default-comparer.log` and `compiler-desktop-comparer.log`; earlier reproduction is
`compiler-baseline.log`. The complete preliminary run had **37 passed / 26 failed**, with **30 CS1701
messages across all 26 failed groups** and one additional CS0117 in Filtered Test. Multiple attributes
can repeat the same unlocated identity diagnostic. These counts describe the preserved initial attempt,
not an all-63 final compilation claim.

## Small retained helper improvements

1. Added comparer selection for diagnosis; did not silently switch compiler identity policy or hide warnings.
2. Collect both `compilation.GetDiagnostics()` and `Emit(...).Diagnostics`; union by exact diagnostic
   text, preserving every unique warning/error while avoiding duplicate phase reports. Log phase counts.
   On the focused inputs both phases agreed; this did **not** remove CS1701 or discover a missing CS1998.
3. Set **`warningLevel: 9999`** to match the actual evaluated project's preview warning-wave coverage.
   The former raw-constructor default level 4 was weaker than the real project. This strengthens the
   check; no `NoWarn` or `SpecificDiagnosticOptions` values were copied. Actual level is logged.

Helper builds `194257-618`, `194518-449`, and final **`194810-951`** each ended `RESULT: PASSED`,
**Warnings: 0**, exit 0. The final executable is still:

`/tmp/opencode/management-001-doccheck/bin/Release/net10.0/Yubico.YubiKit.Management.DocCheck.dll`

Final helper DLL SHA-256:
`a66a58f09f5696bf8e72095f71cee8493a0b23a8c72f3978ffd7ed7ea571c1e4`.

### No-await concern: distinguish inspection from actual diagnostics

The preserved custom-filter example does contain an async callback without await. However, **the
installed Roslyn 5.9 compiler did not emit CS1998**, either from full binding diagnostics or Emit.
A separate compiler-only calibration input has both an async method and an async lambda without await;
it also emits zero warnings at both the former default level and actual preview level 9999. The source
is retained under `checker-source/diagnostic-inputs/AsyncWarningCalibration.cs`; it is not substituted
for a documentation task and was never executed.

Evidence: `compiler-full-diagnostics.log`, `compiler-async-calibration.log`, and
`compiler-async-calibration-preview.log`. Current upstream Roslyn `ErrorCode.cs` comments out
`WRN_AsyncLacksAwaits = 1998`, consistent with the observation. That living source is corroboration,
not claimed to be the exact installed compiler revision: raw public source URLs at the compiler's
reported hash returned 404. The observed installed-compiler results are the primary evidence.

C's reviewed final custom-filter snippet now contains the actual awaited fresh read anyway. No helper
suppression or fake await was added to make it pass. Earlier “likely CS1998” language was a hypothesis,
not a confirmed compiler error; it is superseded by this investigation.

## Focused check against regenerated final PRIMARY

Final PRIMARY extraction includes the corrected SmartCard/5.3 Filtered Test with the real SecurityDomain
helper, and the fresh-read Custom Filter. The source API error is gone from the focused check; it was
not fixed or filtered inside the checker. Root xUnit v3 remains completely separated from xUnit v2.

| Final PRIMARY group, preview level 9999 | Result |
| --- | --- |
| `readme-02` | exit 0; binding/Emit both zero warnings/errors |
| `guide-01` | exit 1; CS1701 remains, with either comparer |
| `shared-readme-5` | exit 1; **CS1701 only**, no CS0117 |
| `shared-readme-6` | exit 1; **CS1701 only**, real fresh read retained |
| `root-scp-v3` | exit 0; binding/Emit both zero warnings/errors, with either comparer |

Evidence: `compiler-final-preview-default.log` / `compiler-final-preview-desktop.log`.
Earlier final-source check before warning-wave alignment is `compiler-final-primary-focused.log`.
All reference and source hashes are printed. No snippet/test/hardware operation was executed.

## Commands, reruns, and proposed next gate

Every log starts with exact command arrays; `diagnose-compiler.py` records each inner compiler command,
raw diagnostic output and its real exit status. **The diagnostic driver itself returns zero after
recording intentional failures; its process exit is not a snippet-pass gate.** Use the normal
`compile-examples.py` runner for aggregate gating; it still fails on any compiler warning/error.

From PRIMARY, the current helper can be rebuilt and strict raw checks rerun with:

```bash
RUN=docs/research/documentation-audit/modules/management/pass-1/management-001
CHECKER=/tmp/opencode/management-001-doccheck/bin/Release/net10.0/Yubico.YubiKit.Management.DocCheck.dll

dotnet toolchain.cs -- build --project Management.DocCheck
python3 "$RUN/validation/compiled-examples/extract-examples.py"
python3 "$RUN/validation/checker-source/compile-examples.py" \
  --repo "$PWD" --checker "$CHECKER" \
  --manifest "$RUN/validation/compiled-examples/manifest.json"
```

The last command is currently expected to fail on dependency-context CS1701; do not relabel it green.
For new focused evidence, use a **new log path** (drivers refuse to overwrite existing evidence):

```bash
python3 "$RUN/validation/checker-source/diagnose-compiler.py" \
  --repo "$PWD" --checker "$CHECKER" \
  --manifest "$RUN/validation/compiled-examples/manifest.json" \
  --log "$RUN/validation/compiler-focused-NEXT.log" \
  --identity-comparer desktop --group guide-01 --group root-scp-v3

python3 "$RUN/validation/checker-source/inspect-compiler-policy.py" \
  --repo "$PWD" --log "$RUN/validation/compiler-policy-NEXT.log"
```

**Proposed project-context compilation, not performed here:**

1. After final SDK/sample/test dependency Release builds, create an isolated compile-only documentation
   project beneath retained `checker-source/<context>/src/`, with generated assets/output outside the
   repo. Derive its properties, packages, framework reference packs and analyzer configuration from the
   real Management integration project (v2) rather than mixing raw runtime assemblies with arbitrary
   compiler options. Reference the real final Tests.Shared, SkippableFact, xUnit2, optional SecurityDomain
   integration-helper and Oath binaries. Compile the exact retained wrapper sources; do not add SDK
   implementation source, strip attributes, change callback bodies, or replace dependencies.
2. Keep the root xUnit3 group in a **separate** context derived from Management.UnitTests, never importing
   Tests.Shared/xUnit2 into it. Consumer/sample groups can retain their strict raw checks or use separately
   matched consumer/sample contexts; final scope must reconcile all 63 selected groups.
3. Build only those hosts via `dotnet toolchain.cs -- build --project <unique-context-project-name>`.
   No runner/test target or executable invocation is involved. Keep analyzers and warnings-as-errors
   enabled; require zero reported warnings/errors. Preserve actual SDK/MSBuild/Csc command lines,
   resolved identities and policies so the normal SDK redundant-warning handling is explicit, not hidden.
4. Report **both** outcomes: successful SDK-project compilation under its existing policy, if achieved,
   and retained raw dependency diagnostics. This is not authorization to add a new per-diagnostic
   suppression, claim runtime compatibility from compilation, or declare the raw checker warning-free.

This proposal requires coordinator approval/implementation and is not an already-prepared runnable
host or completed gate. If the requirement instead means **zero unsuppressed CS1701 from raw Roslyn**,
the existing real dependency mismatch is the exact blocker; a separate dependency-policy change outside
this docs-only task would be needed. No such change was made.

## API provenance and limits

Context7 `/dotnet/roslyn` and `/dotnet/msbuild` were consulted on 2026-11-03. Relevant primary sources:

- [CSharpCompilation.Create](https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Compilation/CSharpCompilation.cs)
- [csc comparer selection](https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/CommandLine/CSharpCompiler.cs)
- [Roslyn diagnostic enum](https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Errors/ErrorCode.cs)
- [MSBuild evaluation-only query implementation](https://github.com/dotnet/msbuild/blob/main/src/MSBuild/XMake.cs)

Installed SDK targets, actual assets/DLLs, exact logs and observed compiler behavior take precedence
over living-source corroboration. The raw helper does not run SDK/xUnit analyzers or MSBuild reference
resolution, and uses runtime TPA metadata rather than the project's framework reference pack. It cannot
certify analyzer parity, runtime loading, device behavior, restored configuration or hardware safety.
All SDK executable source and public API preservation/final full gates remain coordinator-owned.
