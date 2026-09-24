# Build Script

This project uses a .NET 10 C# script for build automation with Bullseye task runner.

## Prerequisites

- .NET 10 SDK, 10.0.300 or newer (pinned by `global.json`; the metric scripts use the `#:include` directive)
- Bash for `docs-inventory` and `docs-architecture` (macOS/Linux, Git Bash, or WSL)

## Usage

Run targets with:
```bash
dotnet toolchain.cs [target]
dotnet toolchain.cs -- [target] [options]
```

### When to Use `--` Separator

The `--` separator tells the .NET file runner to pass arguments to the script instead of interpreting them. It is required before every script long option, including `--project`; otherwise the runner can reject the command because it sees both `--project` and the script file.

```bash
# These work WITHOUT -- (target names only)
dotnet toolchain.cs build

# These REQUIRE -- (every script long option)
dotnet toolchain.cs -- --help          # --help conflicts with dotnet's help
dotnet toolchain.cs -- -h              # Same issue
dotnet toolchain.cs -- test --project Piv
dotnet toolchain.cs -- build --clean

# Put -- before the target whenever the script receives a long option
dotnet toolchain.cs -- build --project Piv --clean
```

**Rule:** Use `dotnet toolchain.cs -- <target> <script-long-options>` for any script `--option`. Target-only commands may omit the separator.

### Available Targets

- **clean** - Remove artifacts directory (and optionally run `dotnet clean`); must be specified explicitly
- **restore** - Restore NuGet dependencies
- **build** - Build the solution (depends on: restore)
- **test** - Run unit tests with nice summary output (depends on: restore, build)
- **docs-qa** - Validate active documentation hygiene
- **docs-list-active** - Print the exact active documentation set used by `docs-qa`
- **docs-inventory** - Generate the report-only active documentation inventory
- **docs-architecture** - Validate architecture diagram evidence map and rendered-image freshness
- **coverage** - Run tests with code coverage collection (depends on: restore, build)
- **crap** - Compute CRAP scores from collected coverage (requires: coverage)
- **complexity** - Flag complex methods in your uncommitted changes; source only, no build or coverage needed
- **pack** - Create NuGet packages (depends on: restore, build)
- **setup-feed** - Configure local NuGet feed
- **publish** - Publish packages to local feed (depends on: pack, setup-feed)
- **default** - Run tests and publish (depends on: test, publish)

### Options

- `--package-version <version>` - Override package version (e.g., `1.2.3-preview.1`)
- `--nuget-feed-name <name>` - NuGet feed name (default: `Yubico.YubiKit-LocalNuGet`)
- `--nuget-feed-path <path>` - NuGet feed directory (default: `artifacts/nuget-feed`)
- `--include-docs` - Include XML documentation in packages
- `--dry-run` - Show what would be published without actually publishing
- `--clean` - Run `dotnet clean` before build
- `--filter <expression>` - Test filter expression (e.g., `"FullyQualifiedName~MyTest"`)
- `--project <name>` - Build/test specific project only (partial match, e.g., `Piv`; requires the preceding `--` separator)
- `--integration` - Include integration tests (requires `--project`)
- `--smoke` - Smoke test mode: skip `Slow` and `RequiresUserPresence` tests (fast integration runs)
- `--crap-args <args>` - Arguments passed to `crap.cs`
- `--complexity-args <args>` - Arguments passed to `complexity.cs`
- `-h, --help` - Show help message (use `dotnet toolchain.cs -- --help`)

### Examples

```bash
# Show help (requires -- to avoid dotnet intercepting --help)
dotnet toolchain.cs -- --help

# Clean artifacts
dotnet toolchain.cs clean

# Build the solution
dotnet toolchain.cs build

# Build specific project (partial match)
dotnet toolchain.cs -- build --project Piv

# Run tests
dotnet toolchain.cs test

# Validate active documentation hygiene
dotnet toolchain.cs docs-qa

# Generate report-only active documentation inventory
dotnet toolchain.cs -- docs-inventory

# Run tests for specific project with filter
dotnet toolchain.cs -- test --project Piv --filter "Method~Sign"

# Run tests with code coverage (all unit test projects)
dotnet toolchain.cs coverage

# Rank methods by CRAP score using the collected coverage
dotnet toolchain.cs crap
dotnet toolchain.cs -- crap --crap-args "--top 50 --json artifacts/crap/crap.json"

# Check the complexity of the methods you changed (pre-commit gate)
dotnet toolchain.cs complexity
dotnet toolchain.cs -- complexity --complexity-args "--module Piv"

# Run integration tests for a specific module
dotnet toolchain.cs -- test --integration --project Piv

# Quick smoke test (skips slow RSA keygen and user-presence tests)
dotnet toolchain.cs -- test --integration --project Piv --smoke

# Create and publish packages with custom version
dotnet toolchain.cs -- publish --package-version 1.0.0-preview.2

# Dry run to see what would be published
dotnet toolchain.cs -- publish --dry-run

# Full clean build (delete artifacts, then build)
dotnet toolchain.cs clean build
```

## Target Dependencies

```
default
├── test
│   └── build
│       └── restore
└── publish
    ├── pack
    │   └── build (shared)
    └── setup-feed

clean  (standalone — must be specified explicitly)
```

## Output

- **Packages**: `artifacts/packages/*.nupkg`
- **Coverage reports**: `artifacts/coverage/**/coverage.cobertura.xml`
- **CRAP report**: `artifacts/crap/crap.json` (when `--json` is passed)
- **Complexity report**: wherever `--complexity-args "--json <path>"` points
- **Local NuGet feed**: `artifacts/nuget-feed/`

## Analyzers and Formatting

- For routine development, run scoped `dotnet format` with `--include` so formatting fixes apply directly to staged files. For repository-wide rule changes and CI enforcement workflow, follow `docs/DEV-GUIDE.md`.
- Analyzer configuration details live in `docs/DEV-GUIDE.md`; review that guide before introducing new rules or suppressions.

## Documentation QA

Run `dotnet toolchain.cs docs-qa` to validate bounded active documentation hygiene.

The target scans:

- root `*.md` files
- top-level `docs/*.md`
- `docs/usage/**`, `docs/troubleshooting/**`, and `docs/architecture/**`
- module `src/**/README.md` and `src/**/CLAUDE.md`

It intentionally excludes archived or planning material under `docs/archive`, `docs/completed`, `docs/plans`, `docs/research`, `docs/reviews`, `docs/specs`, and `docs/templates`.

Current checks:

- fenced code blocks are balanced
- local markdown links outside fenced code examples resolve to existing files or directories
- stale FIDO2 user-presence trait examples that do not use `Category=RequiresUserPresence` are rejected

Snippet compilation is not part of this target. README examples are treated as documentation samples whose local links and fences must stay valid; compile-time snippet validation needs a separate approved phase.

Use `dotnet toolchain.cs -- docs-list-active` to print the exact active documentation set consumed by `docs-qa`. Use `dotnet toolchain.cs -- docs-inventory` to regenerate `docs/docs-inventory-report.md`; the inventory is report-only triage input and must not be treated as an auto-rewrite instruction.

## Project Discovery

The build script automatically discovers projects using glob patterns:

- **Packable projects**: All `Yubico.YubiKit.*/src/*.csproj` files
- **Test projects**: All `Yubico.YubiKit.*.UnitTests/*.csproj` files under `tests/` directories

This means you don't need to manually update the build script when adding new projects that follow the standard structure. Run `dotnet toolchain.cs -- --help` to see the current list of discovered projects.

## Native AOT Publish Verification

Native AOT publishing is **not** a `dotnet toolchain.cs` target — it is verified independently via
[`.github/workflows/native-aot.yml`](.github/workflows/native-aot.yml), which runs
`dotnet publish -p:PublishAot=true` directly against
[`verification/NativeAotVerification`](verification/NativeAotVerification), a minimal internal
console host that references every in-scope SDK library. See
[`docs/NATIVE-AOT.md`](docs/NATIVE-AOT.md) for the full support contract. To reproduce locally:

```bash
dotnet publish verification/NativeAotVerification/Yubico.YubiKit.NativeAotVerification.csproj \
  -c Release -r osx-arm64 --self-contained -p:PublishAot=true
```

The normal `build` target also validates that the exact ten supported SDK projects opt in and that
the verification host references and anchors each one.

## Code Coverage

The `coverage` target picks a collector per project, because the two test platforms need different mechanisms:

| Test platform | Mechanism | Why |
|---|---|---|
| xUnit v3 / Microsoft Testing Platform | `coverlet.console` against the test executable | `--collect:"XPlat Code Coverage"` is a VSTest data collector and requires `Microsoft.NET.Test.Sdk`, which MTP projects deliberately omit. Using it aborts the run with a missing `testhost.deps.json`. |
| xUnit v2 | `dotnet test --collect:"XPlat Code Coverage"` | The VSTest data collector path. |

All unit test projects currently use MTP, so the second row is not exercised today; it remains for xUnit v2 projects.

`coverlet.runsettings.xml` is the single definition of coverage policy. The VSTest collector reads it directly, and `toolchain.cs` projects the same settings onto `coverlet.console` flags, so both paths filter identically. Change filters there, not in `toolchain.cs`.

Use `--project` to run coverage for a specific module.

`Microsoft.Testing.Extensions.CodeCoverage` is not used: it currently throws `TypeLoadException` against `xunit.v3` 3.0.0, whose `Microsoft.Testing.Platform.MSBuild` 1.7.3 dependency is not binary compatible with `Microsoft.Testing.Platform` 2.3.3. It also does not emit the per-method complexity that CRAP analysis reads.

## CRAP Scores

The `crap` target ranks methods by the CRAP (Change Risk Anti-Patterns) metric:

```
CRAP(m) = cc(m)^2 * (1 - cov(m))^3 + cc(m)
```

It runs `crap.cs`, which computes **source-level cyclomatic complexity** with Roslyn and takes only coverage from the Cobertura reports.

This deliberately differs from the "crap score" ReportGenerator shows for the same reports. ReportGenerator reads coverlet's `complexity` attribute, which is `Math.Max(1, branches.Count)` over recorded IL branch outcomes — a single `if` contributes 2. Measured on this repo the ratio to source complexity ranges from 0.5x to 6x with no stable multiplier, and it follows Roslyn codegen, so an SDK upgrade can move the numbers with no source change.

Pass script options through `--crap-args`:

```bash
dotnet toolchain.cs -- crap --crap-args "--top 50 --min-crap 15"
dotnet toolchain.cs -- crap --crap-args "--json artifacts/crap/crap.json"
dotnet toolchain.cs -- crap --crap-args "--module Piv"
dotnet toolchain.cs -- crap --crap-args "--changed"
```

The [scope options](#scope-options) narrow what is reported. Coverage is still correlated against every method, so the stale-coverage check keeps judging the whole report. `--changed` cannot be combined with `--baseline`; `--module` with `--baseline` filters both sides to the same modules.

### Two complexity axes

CRAP is driven by cyclomatic complexity, which answers *how many paths* — not *how risky is this to change*. Those diverge badly on lookup tables, so the report also computes **cognitive complexity** (SonarSource rule S3776).

| method | cyclomatic | cognitive | reading |
|---|---:|---:|---|
| `SCardException.GetErrorString` | 69 | 1 | a flat status-word table: large, obvious |
| `LinuxHidIOReportConnection.ParseReportSizes` | 13 | 24 | a nested parser: small, genuinely hard |

Cognitive complexity applies three rules that produce this split: a `switch` increments **once** regardless of arm count, nesting compounds, and readable shorthand (`??`, `??=`, `?.`) is ignored.

Filter with both to get the actionable set, which avoids maintaining a hand-written ignore list of "big but harmless" methods:

```bash
dotnet toolchain.cs -- crap --crap-args "--min-crap 8 --min-cognitive 15"
```

`--min-cognitive` defaults to 15, which is Sonar's own threshold for S3776.

### Cyclomatic complexity rules

The rules follow the [SonarQube C# specification](https://docs.sonarsource.com/sonarqube-server/user-guide/code-metrics/metrics-definition), so the numbers are comparable to any SonarQube report: base 1 per member, plus one for each conditional expression, conditional access (`?.`), switch case or switch-expression arm, `and`/`or` pattern, `do`/`for`/`foreach`/`if`/`while`, and `??`/`??=`/`||`/`&&`.

**One deliberate deviation:** `catch` clauses and `when` guards also increment. SonarQube's C# list omits them; textbook McCabe and the original crap4j both count them, and an exception handler is a real alternate path. This repo has 186 catch clauses, so the difference is material and is stated here rather than hidden.

`default:` and the `_` switch-expression arm do not increment, since neither adds an independent path.

Cognitive complexity does not implement the recursion increment, which needs a semantic model; directly recursive methods score one low.

Verify every rule against its golden fixtures with `dotnet crap.cs --self-check` (90 fixtures, several taken from the SonarSource white paper; they also cover the module report, the scope options, and the base comparison and markdown section used by `complexity`).

The CRAP report has no CI gate and no baseline ratchet. The soft gate for day-to-day work is the complexity check below, which leaves coverage out on purpose.

## Complexity check

The `complexity` target flags methods with **cyclomatic complexity above 10** or **cognitive complexity above 20**, the two axes described above. It measures source only: no build, no tests, and no coverage, so a warm run takes a few seconds. It exists to keep the codebase easy to read for people and agents, not to chase a coverage number.

```bash
dotnet toolchain.cs complexity                  # methods you changed vs HEAD (pre-commit)
dotnet toolchain.cs -- complexity --complexity-args "--base $(git merge-base HEAD origin/yubikit)"
dotnet toolchain.cs -- complexity --complexity-args "--module Piv"            # whole module
dotnet toolchain.cs -- complexity --complexity-args "--module Piv --changed"  # changed lines in Piv
dotnet toolchain.cs -- complexity --complexity-args "--all"                   # whole shipping SDK
```

The script runs directly too: `dotnet complexity.cs [options]`.

| Option | Default | Meaning |
|---|---|---|
| `--changed` | on unless `--all` or `--module` | methods overlapping changed lines |
| `--base <ref>` | `HEAD` | comparison ref; implies `--changed` |
| `--module <Name>` | all | repeatable module filter |
| `--all` | off | whole shipping SDK |
| `--max-cyclomatic <n>` | 10 | flag when cyclomatic > n |
| `--max-cognitive <n>` | 20 | flag when cognitive > n |
| `--top <n>` | 25 | rows in the console table |
| `--json <path>` | off | write every in-scope method as JSON |
| `--fail-on-findings` | off | exit 3 when a finding needs action |
| `--markdown` | off | print a GitHub markdown section instead of the console table |

In a changed scope, each flagged method is compared with the same member in the base version of its file:

| Status | Meaning | Action |
|---|---|---|
| new | the method or its file did not exist at the base | simplify it, or justify it |
| worse | cyclomatic or cognitive complexity went up | simplify it, or justify it |
| unchanged | both scores are the same | optional: existing debt you touched |
| improved | neither score went up, and at least one went down | optional |

To justify a method instead of simplifying it, add a line to the commit message; the report prints a template for each method that needs action:

```
Complexity-Justification: PivSession.ImportKeyAsync: <why this complexity is warranted>
```

Full scans (`--all`, or `--module` without `--changed`) have no base to compare with and list every finding.

Exit codes: `0` on success whether or not methods were flagged, `1` for a usage, IO, or git error, and `3` when `--fail-on-findings` is set and a finding needs action. Through `dotnet toolchain.cs`, any non-zero exit fails the target and the toolchain exits `1`; call `dotnet complexity.cs --fail-on-findings` directly (for example from a git hook) when you need the exact code.

### Pull request report

`.github/workflows/coverage-crap-report.yml` keeps one comment up to date on each pull request that touches C# or the metric tooling. It has two sections, each written by its own job:

- **Complexity** comes from a fast job that needs no build and no tests: `dotnet complexity.cs --markdown --base <first parent of the merge commit>`, so it lists exactly the methods the pull request changes. It usually appears within a couple of minutes.
- **Coverage and CRAP** comes from the slower job, which runs two full coverage passes (head and base, both measured with the pull request's tooling). It waits for the complexity job so the two never write the comment at the same time, and runs even if that job failed.

Neither job fails the pull request. Forks get both reports in the job summaries instead of the comment. To preview the complexity section locally:

```bash
dotnet complexity.cs --markdown --base origin/yubikit
```

The comment logic lives in `.github/scripts/report-comment.js`; `node .github/scripts/report-comment.test.js` tests it, and the complexity job runs that test first.

### Scope options

`crap` and `complexity` share these options. Both only ever measure shipping source: `src/<Module>/src/`, excluding tests, examples, `Tests.*`, `bin`, `obj`, and generated files.

- `--module <Name>` (repeatable) keeps only `src/<Name>/src/`. Unknown names fail and list the valid modules.
- `--changed` keeps methods whose span overlaps a line changed in the working tree (staged and unstaged) versus `HEAD`, plus every method in untracked `.cs` files. A pure deletion counts only when it falls strictly inside a method.
- `--base <ref>` compares with `<ref>` instead of `HEAD` and implies `--changed`.
- Options combine: `--module Piv --changed` means changed lines inside Piv.

`complexity` defaults to `--changed` because it is the pre-commit gate; `crap` defaults to everything.

The shared code lives in `scripts/code-metrics/` and is pulled into both entry points with `#:include`. `crap.cs` owns the coverage-based report and the self-check; `complexity.cs` owns the source-only report. A member is matched to its base version by type, name, and parameter list, so moving it or adding an overload beside it does not change its status.

## xUnit v2 vs v3 Test Runner Detection

**IMPORTANT: Always use `dotnet toolchain.cs test` instead of invoking `dotnet test` directly.**

This codebase uses a mix of xUnit v2 and xUnit v3 test projects, which require different command-line invocation:

| Runner | Detection | Command | Filter Syntax |
|--------|-----------|---------|---------------|
| **xUnit v3** (Microsoft.Testing.Platform) | `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>` in .csproj | `dotnet run --project <proj>` | `-- --filter "..."` |
| **xUnit v2** (traditional) | No such setting | `dotnet test <proj>` | `--filter "..."` |

### Why This Matters

If you invoke `dotnet test` on an xUnit v3 project, or use the wrong filter syntax, the tests will fail with confusing errors. The build script automatically detects which runner each project uses and invokes the correct command.

### Examples

```bash
# ✅ CORRECT - Let the build script handle runner detection
dotnet toolchain.cs test
dotnet toolchain.cs -- test --project Core
dotnet toolchain.cs -- test --filter "FullyQualifiedName~MyTest"

# ❌ WRONG - May fail if project uses xUnit v3
dotnet test Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Yubico.YubiKit.Fido2.UnitTests.csproj
```

### Test Filtering Tips

- **Always combine `--project` with `--filter`** to avoid building and running all test projects:
  ```bash
  # ✅ Fast — only builds and runs WebAuthn tests
  dotnet toolchain.cs -- test --project WebAuthn --filter "FullyQualifiedName~PreviewSign"

  # ⚠️ Slow — builds ALL test projects, runs filter against each (most find 0 matches)
  dotnet toolchain.cs -- test --filter "FullyQualifiedName~PreviewSign"
  ```
- Filter syntax: `FullyQualifiedName~Substring`, `Method~Name`, `Category!=Slow`
- The toolchain auto-translates VSTest filter expressions to xUnit v3 native options (`--filter-method`, `--filter-trait`, etc.)
- It also normalises `Method~` / `Name~` to `FullyQualifiedName~` for the xUnit v2 (VSTest) projects, which have no `Method` property. Without that, `Method~Sign` matches **zero** tests on every integration project while working fine on the unit projects. `FullyQualifiedName~` is the precise form if you want to be explicit
- **A filter matching no tests is an error, not a pass.** The toolchain preflights both runners and fails with `No tests matched the specified filter`. Left unguarded, VSTest prints `No test matches the given testcase filter` and still exits `0`, which previously surfaced as `✓ All tests passed` from a run that never happened

### For AI Agents / Automation

When writing scripts or automation that runs tests:

1. **Always use `dotnet toolchain.cs test`** - it handles the complexity for you
2. **Never assume** `dotnet test` will work for all projects
3. **Use `--project`** to filter to specific projects: `dotnet toolchain.cs -- test --project Fido2`
4. **Combine `--project` with `--filter`** for targeted test runs: `dotnet toolchain.cs -- test --project Fido2 --filter "Method~Sign"`
5. **Read the per-project `total:` line, not the final summary line.** The closing
   `Passed: 1 | Failed: 0 | Skipped: 1 | Total: 2` counts **projects**, not tests. Grepping for
   `Passed:` therefore reads green off a run that executed nothing. Assert on the per-project
   `total:` / `Total tests:` figure instead:

   ```bash
   dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~FidoHidProtocol" \
     | grep -E "total:|failed:"
   ```

   A zero-match filter now fails the target outright, but the project-vs-test counting still
   catches people out when a filter matches fewer tests than intended.
6. **Add `--smoke` to any unattended hardware run.** It skips `Slow` and `RequiresUserPresence`,
   so a missing human cannot leave a ceremony parked and wedge the key. Be aware it can thin
   coverage sharply — on WebAuthn it reduces the integration lane to a single SmartCard test, so
   check the `total:` figure before treating a smoke pass as meaningful validation.
