# Validation tooling preparation

Prepared for `management-001`; **not built or run** during this preparation task. The coordinator
must serialize final toolchain runs and validate the utility itself before relying on its results.
No SDK source, SDK documentation, historical archive, tests, hardware, or commits were changed here.

## Retained files

- `validation/checker-source/src/Program.cs`: Roslyn executable-token/directive comparator,
  public API snapshot comparator, and in-memory compile-only checker.
- `validation/checker-source/src/Yubico.YubiKit.Management.DocCheck.csproj`: standalone .NET 10 /
  C# 14 executable; Roslyn references come from the selected SDK's `Roslyn/bincore`, not NuGet.
- `validation/checker-source/Directory.Build.props` and `Directory.Build.targets`: isolate this
  utility from shipping-library packaging and analyzer-package imports. Compiler warnings remain
  errors and the SDK's default analyzers remain enabled; this does not relax any SDK project's gates.
  Output and intermediate paths are set **early**, before SDK default paths are evaluated.
- `validation/checker-source/compile-examples.py`: manifest-driven runner, with no Management
  example guesses or extraction rules. The C editor supplies classified source groups later.

Provenance: adapted from the immutable WebAuthn archive's
`records/runs/webauthn-001/validation/checker-source/{Program.cs,Yubico.YubiKit.WebAuthn.DocCheck.csproj}`
and `records/runs/webauthn-001/compile-examples.py`. The archive was read, not modified. Roslyn
API provenance is already retained in [validation-plan.md](validation-plan.md#compiler-checker-reference):
Context7 `/dotnet/roslyn`, checked 2026-11-03, `CSharpCompilation.Create` / `Emit`.

## Build and run commands (coordinator only)

Run from the final integrated repository root. Load `domain-build` before compilation; load
`domain-test` before the separate final test gate. **No direct `dotnet build`, `dotnet restore`,
or `dotnet test` is needed.** The helper is deliberately placed beneath `checker-source/src/`:
`scripts/toolchain/Discovery.cs` discovers `Yubico.YubiKit.*.csproj` anywhere beneath a `src/`
directory, and `BuildTargets.cs` accepts a partial project-name match. `IsPackable=false` appears
in the project itself so discovery also excludes this helper from packing.

```bash
RUN=docs/research/documentation-audit/modules/management/pass-1/management-001
CHECKER=/tmp/opencode/management-001-doccheck/bin/Release/net10.0/Yubico.YubiKit.Management.DocCheck.dll

# Final SDK build/gates are coordinated separately; this is the validation plan's command.
dotnet toolchain.cs -- build test docs-qa docs-architecture complexity

# Build only the isolated utility through the repository toolchain.
dotnet toolchain.cs -- build --project Management.DocCheck

# Compare final integrated working-tree source and snapshots with the fixed audit baseline.
dotnet "$CHECKER" tokens "$PWD"
dotnet "$CHECKER" api "$PWD"

# One editor-supplied complete source group; Core + Management are always referenced.
dotnet "$CHECKER" compile "$RUN/validation/compiled-examples/SELECTED-GROUP.cs" "$PWD"

# Additional SDK references, only when the classified group needs them:
dotnet "$CHECKER" compile "$RUN/validation/compiled-examples/SELECTED-GROUP.cs" "$PWD" SecurityDomain

# Batch runner, after C has supplied the actual manifest and retained source groups:
python3 "$RUN/validation/checker-source/compile-examples.py" \
  --repo "$PWD" --checker "$CHECKER" \
  --manifest "$RUN/validation/compiled-examples/manifest.json"
```

`SELECTED-GROUP.cs` and `manifest.json` above are placeholders, **not prepared inputs or claimed
coverage**. Redirect output to retained logs as appropriate and inspect each exit status; do not
hide a failure behind an unguarded `tee` pipeline. Copy toolchain RESULT/log files before their
five-run retention expires. The helper build must show `RESULT: PASSED`, `Warnings: 0`.

By default all helper `obj`, `bin`, restore assets, generated assembly metadata and copied Roslyn
binaries go under `/tmp/opencode/management-001-doccheck/`, never the retained record. To isolate
another checkout, set an absolute `/tmp/opencode/...` workspace using the MSBuild environment
property, then update `CHECKER` accordingly:

```bash
ValidationWorkspace=/tmp/opencode/management-001-doccheck-final dotnet toolchain.cs -- build --project Management.DocCheck
```

This changes only the utility's output locations. SDK Release assembly paths remain the normal
`src/<Module>/src/bin/Release/net10.0/Yubico.YubiKit.<Module>.dll`. The helper has no SDK project
references, does not build SDK code, and cannot determine whether these existing DLLs are fresh.
Run against the final integrated checkout **after** its successful Release build. Each compile
invocation prints reference and source SHA-256 values to make the actual inputs auditable.

## Comparator contract

- Baseline is fixed to `da61d2e6`; no argument permits accidentally selecting another baseline.
- `tokens` uses `git diff` against the working tree (staged + unstaged relative to baseline),
  disables rename detection, and includes untracked source from `git ls-files --others`.
- Scope: C# beneath `src/`, `tests/`, `examples/`, `benchmarks/`, or `verification/`. Thus shipping,
  test and example edits are included while audit records/checker snapshots are excluded. Ignore
  path segments `bin`, `obj`, `generated`, `codebase_ast`, plus `*.g.cs` and `*.generated.cs`.
  Toolchain/root scripts are outside this source scope; review any such changes separately.
- Exact Roslyn token **kind and text**, not value text or normalized whitespace, are compared.
  This preserves literals, identifiers, modifiers, signatures, attributes and method bodies.
  Ordinary comments, XML documentation and whitespace are trivia and not compared.
- The sole structural normalization sorts unchanged tokenized using-directive lists at compilation
  unit, block namespace and file-scoped namespace level. Duplicate/add/remove/changed usings still
  fail. Lists containing directives, disabled text or skipped tokens are not reordered.
- Directives, disabled preprocessor branches and skipped malformed tokens are compared verbatim,
  anchored to their surrounding normalized token index and leading/trailing side. Moving a directive
  across code therefore fails even when directive sequence/text would otherwise match. Directive
  whitespace changes also fail conservatively. Parse configuration is C# 14 with no defined symbols;
  inactive source is retained verbatim rather than silently ignored.
- New/deleted/renamed source fails closed when no baseline/current counterpart exists. No changed
  C# files is an error, not a vacuous pass. Syntax parsing is not a substitute for the final SDK build.
- `api` requires the same baseline/current set of all `src/**/PublicAPI.Shipped.txt` and
  `PublicAPI.Unshipped.txt`, and byte-identical contents (including line endings). This is snapshot
  preservation, **not** a separately generated binary API comparison. The normal shipping-project
  PublicApiAnalyzers build gate plus executable-token preservation supplies the complementary check.
- Exit `0` means comparison/compilation passed; `1` means a comparison or snippet warning/error;
  `2` means usage, missing input/baseline/reference, or another handled tooling error.

## Compile groups and manifest

The runner accepts a JSON object with a nonempty `groups` array. Each group has:

| Field | Required content |
| --- | --- |
| `name` | Nonempty unique classification/group identifier |
| `source` | Complete `.cs` input path, relative to the manifest (or absolute) |
| `provenance` | Nonempty string array naming actual document/XML path and block identifiers |
| `prerequisites` | String array listing exact typed wrapper/stub assumptions; `[]` if none |
| `modules` | Optional additional module-name array; Core and Management are implicit |

There is intentionally no example manifest containing guessed Management snippets. Retain the
classified inputs, include every meaningful selected snippet in an explicit group, and separately
record exclusions with reasons. The runner reports exact command arrays and input hashes. It
does not check whether source matches its claimed provenance or whether the inventory is exhaustive;
the editor/reviewer must verify extraction and typed prerequisites. Combine related fragments into
one complete `.cs` input when necessary; no unrecorded implicit usings or application stubs are added
by the compiler.

`compile` always references final Release Core + Management binaries and optional explicitly named
SDK modules. It uses the running .NET 10 runtime's trusted-platform assemblies as framework metadata,
excluding checker/Roslyn assemblies. It selects console output for top-level statements and library
output otherwise, enables nullable analysis, rejects every emitted compiler warning/error, and emits
only to a disposable memory stream. No emitted assembly is loaded, saved or executed. This is compiler
binding evidence, not SDK analyzer parity, reference-pack portability, trimming/AOT verification,
runtime behavior, hardware behavior, or completeness of the documentation inventory. Missing dependent
SDK modules must be explicitly added to the invocation/manifest; nothing is auto-discovered or fetched.

## Preparation verification and outstanding work

Preparation checks are limited to source/script/project inspection, Python syntax parsing, XML parsing,
and scoped whitespace checks. No helper/SDK builds, tests, snippets, or hardware operations were run.
The first serialized coordinator build remains responsible for confirming C# compilation and actual
toolchain discovery. Final comparator execution, snippet extraction/classification, compilation results,
and retained logs remain pending; this file does not claim successful validation.
