# Complexity gate and code-metrics split plan

**Goal:** Give agents and humans a fast, source-only complexity check on the code they changed, used as a soft pre-commit gate, and split the 1,758-line `crap.cs` into focused files that every metric command can share.

**Why:** Simple, conventional code is easier for both humans and LLMs to read. Coverage is deliberately not part of the gate: raising coverage lowers a CRAP score without making code any simpler.

**Technology:** .NET 10 file-based apps with `#:include` (SDK 10.0.300+), Roslyn syntax trees, `git diff`, `dotnet toolchain.cs`.

## Announcement (written first; the implementation must match it)

> **New: `dotnet toolchain.cs complexity`, a fast complexity check for the code you changed**
>
> Before you commit, run:
>
> ```bash
> dotnet toolchain.cs complexity
> ```
>
> It looks only at the C# methods you touched (uncommitted changes compared with `HEAD`), measures them straight from source, and flags any method with **cyclomatic complexity above 10** or **cognitive complexity above 20**. It needs no tests and no coverage run, and finishes in seconds.
>
> Each flagged method is marked **new**, **worse**, **unchanged** or **improved** compared with `HEAD`. New and worse methods need action: simplify them, or explain why not with a `Complexity-Justification:` line in the commit message. Unchanged and improved methods are existing debt you touched; simplifying them is welcome but optional.
>
> The goal isn't a number. It's code that people and agents can read in one pass.
>
> Also in this change:
> - Every metric can be scoped: `--module Piv`, `--changed` and `--base <ref>` work for both `complexity` and `crap`.
> - `crap.cs` is split into focused files under `scripts/code-metrics/`. Its command line and output are unchanged.
> - The repo now requires .NET SDK 10.0.300 or newer via `global.json`. If you're on an older 10.0 SDK, install the latest 10.0 SDK.

## Usage model

### Commands

| Goal | Command |
|---|---|
| Pre-commit check of my uncommitted changes | `dotnet toolchain.cs complexity` |
| Everything changed on my branch | `dotnet toolchain.cs -- complexity --complexity-args "--base $(git merge-base HEAD origin/yubikit)"` |
| Survey one module | `dotnet toolchain.cs -- complexity --complexity-args "--module Piv"` |
| Changed lines within one module | `dotnet toolchain.cs -- complexity --complexity-args "--module Piv --changed"` |
| Survey the whole shipping SDK | `dotnet toolchain.cs -- complexity --complexity-args "--all"` |
| Machine-readable result | add `--json <path>` |
| Hook-style non-zero exit | add `--fail-on-findings` |
| CRAP for one module (after `coverage`) | `dotnet toolchain.cs -- crap --crap-args "--module Piv"` |
| CRAP for my changed methods (after `coverage`) | `dotnet toolchain.cs -- crap --crap-args "--changed"` |

The scripts can also be run directly: `dotnet complexity.cs [options]`, `dotnet crap.cs [options]`.

### Scope rules (shared by both commands)

- Scope is always the shipping SDK: `src/<Module>/src/**/*.cs`, excluding tests, examples, `Tests.*`, `bin`, `obj`, and generated files. This matches today's `crap.cs`.
- `--module <Name>` (repeatable) restricts to `src/<Name>/src/`. Unknown names are a usage error that lists the valid modules.
- `--changed` restricts to methods whose source span overlaps a line changed in the working tree (staged and unstaged) compared with `HEAD`, plus every method in untracked `.cs` files. A pure deletion counts only when it falls strictly inside a method.
- `--base <ref>` compares with `<ref>` instead of `HEAD` and implies `--changed`.
- Filters combine: `--module Piv --changed` means changed lines inside Piv.
- **Defaults differ by purpose:** `complexity` defaults to `--changed` (it's the pre-commit gate); `--all` or `--module` switch it to a full scan. `crap` defaults to everything, as it does today.
- `crap` rejects `--changed` together with `--baseline`, because a module delta over a partial method set is meaningless. `--module` with `--baseline` filters both sides to the same modules.

### `complexity` options

| Option | Default | Meaning |
|---|---|---|
| `--changed` | on unless `--all`/`--module` | changed-line scope |
| `--base <ref>` | `HEAD` | comparison ref; implies `--changed` |
| `--module <Name>` | all | repeatable module filter |
| `--all` | off | whole shipping SDK |
| `--max-cyclomatic <n>` | 10 | flag when cyclomatic > n |
| `--max-cognitive <n>` | 20 | flag when cognitive > n |
| `--top <n>` | 25 | rows shown in the console table |
| `--json <path>` | off | write every in-scope method as JSON |
| `--fail-on-findings` | off | exit 3 when a finding needs action |
| `--markdown` | off | GitHub markdown section instead of the console table (follow-up) |

### Status (changed scope only)

Each flagged method is looked up by type, member name, and parameter list in the `--base` version of its file (a name that is unique on both sides still matches after a parameter change):

| Status | Rule | Action |
|---|---|---|
| new | method or file absent at base | simplify or justify |
| worse | cyclomatic or cognitive higher than at base | simplify or justify |
| unchanged | both scores equal | optional |
| improved | neither higher, at least one lower | optional |

Full scans (`--all`, `--module` without `--changed`) have no status; every finding is listed.

### Console output shape

```
Complexity check (source only, no coverage)
  scope        changed lines vs HEAD
  files        2
  methods      9
  thresholds   cyclomatic > 10, cognitive > 20

    cc   cog  status     method
    14    23  worse      Yubico.YubiKit.Piv.PivSession.ImportKeyAsync  (was 12/18)
                         src/Piv/src/PivSession.cs:120-188

1 of 9 methods exceeds a threshold: 1 needs action (new or worse), 0 are existing debt.
For each method that needs action, simplify it or add to the commit message:
  Complexity-Justification: PivSession.ImportKeyAsync: <why this complexity is warranted>
```

Clean: `All 9 methods are within thresholds.` Nothing in scope: `No shipping C# methods in scope.`

### Exit codes

| Command | 0 | 1 | 2 | 3 |
|---|---|---|---|---|
| `complexity` | success, findings or not | usage, IO or git error | — | findings need action and `--fail-on-findings` was given (through `toolchain.cs` the target fails with 1) |
| `crap` | unchanged | unchanged | unchanged | — |

## Design

### File layout

```
crap.cs                        entry: options, CRAP report, JSON writer (CLI unchanged)
complexity.cs                  entry: options, complexity report, JSON writer
scripts/code-metrics/
  Repo.cs                      repo-root discovery and git process helper
  SourceMethods.cs             SourceMethod, MethodExtractor, shipping-source discovery
  CyclomaticComplexity.cs
  CognitiveComplexity.cs
  Scope.cs                     --module/--changed/--base parsing, diff parser, span overlap
  BaseComparison.cs            match a member to its base version; new/worse/unchanged/improved
  ComplexityReport.cs          findings ordering and the markdown section (follow-up)
  Coverage.cs                  CrapRow, Cobertura ingest, span correlation
  ModuleReport.cs              module aggregation, baseline diff, markdown/console module report
  SelfCheck.cs                 golden fixtures (run via `dotnet crap.cs --self-check`)
```

Each include is a literal `#:include` line; globs would disable the file-based app build cache.

### SDK pin

`global.json`: `{ "sdk": { "version": "10.0.300", "rollForward": "latestFeature" } }`. CI already installs `10.0.x`, which resolves to the latest 10.0 feature band.

### CI

`coverage-crap-report.yml` measures the base commit with the pull request's tooling. It must copy `scripts/code-metrics/` alongside `crap.cs`, remove or restore it before returning to head (the base commit may not contain it, and leftover untracked files would block the checkout), and trigger on `scripts/code-metrics/**` and `global.json`.

### Soft gate in `CLAUDE.md`

- Code Quality quick reference: keep methods at cyclomatic ≤ 10 and cognitive ≤ 20.
- Pre-commit checklist: run `dotnet toolchain.cs complexity`; for each method that needs action, simplify it or add a `Complexity-Justification:` line to the commit message.

## Non-goals

- No hard CI gate and no per-module ratchet. (The pull request report gains a complexity section in the follow-up below; its CRAP table is unchanged.)
- No complexity checks on tests or examples.
- No recursion increment in cognitive complexity (unchanged limitation).

## Definition of done

1. `global.json` pins `10.0.300` with `latestFeature`; `dotnet --version` in the repo resolves to 10.0.4xx; `dotnet toolchain.cs build` passes on it.
2. `crap.cs` is an entry file only; shared code lives in `scripts/code-metrics/`, one responsibility per file.
3. Behaviour preserved: on the same coverage input, `crap.cs` JSON (ignoring `generatedOn`), console, markdown and `--baseline --markdown` output are byte-identical before and after the split; `--self-check` passes every pre-existing fixture.
4. New self-check fixtures cover the diff parser and span-overlap rules.
5. `complexity.cs` runs with no coverage present and behaves as in the usage model: default changed scope, `--base`, `--module`, `--all`, thresholds, `--top`, `--json`, `--fail-on-findings`, exit codes, and the console shape above.
6. Status classification (new, worse, unchanged, improved) is demonstrated on real diffs.
7. `crap.cs` honours `--module`, `--changed` and `--base`, and rejects `--changed` with `--baseline`.
8. `toolchain.cs` has a `complexity` target and `--complexity-args`; its header lists `crap` and `complexity` and both argument options.
9. The CI workflow copies and cleans up `scripts/code-metrics/` and triggers on its paths; the git overlay sequence is simulated locally and succeeds.
10. `CLAUDE.md` and `TOOLCHAIN.md` describe the gate, scopes, commands and fixture count; `dotnet toolchain.cs docs-qa` passes.
11. A warm `dotnet toolchain.cs complexity` run finishes in under 10 seconds.
12. Cross-vendor review of the diff is done and substantive findings are resolved.
13. The final output of every command is checked against this announcement and usage model.

## Verification log

Run on 2026-09-24 with SDK 10.0.401 (macOS arm64).

| DoD | Evidence |
|---|---|
| 1 | `dotnet --version` in the repo → `10.0.401`; `dotnet toolchain.cs build` succeeded. |
| 2 | `crap.cs` is options, report, and JSON only; shared code is in nine files under `scripts/code-metrics/`. |
| 3 | Original and split `crap.cs` run against the same Oath coverage in 12 modes (console, wide, no conditional access, JSON ×2, markdown, baseline markdown/console/fail, bad argument): stdout (normalised JSON without `generatedOn`), stderr, and exit codes byte-identical. The only difference is the self-check count, 60 → 82. |
| 4 | Scope fixtures: hunk ranges, single-line hunks, deletions inside/at/before a method, deleted files, `+++` content lines, tab-suffixed and C-quoted paths, module prefix, `--base` implying `--changed`. |
| 5 | Default scope, `--base HEAD~3`, `--module Oath`, `--module Oath --changed`, `--all` (2,789 methods, 69 findings), thresholds, `--top`, `--json`, `--fail-on-findings` (exit 3), and usage errors (exit 1) all behave as documented. |
| 6 | Scratch clone: new (untracked file), worse (`was 12/11`), unchanged (edit inside an existing debt method), improved (`was 12/9`); a pure deletion inside a method is detected; an overload inserted beside a method matches by parameter list; a C-quoted non-ASCII filename is found both untracked and modified. Base-comparison fixtures cover the same rules. |
| 7 | `crap.cs --module Oath` and `--base HEAD~3` report a `scope` line and scoped counts; `--changed --baseline` fails with exit 1; `--module Oath --baseline` filters both sides. |
| 8 | `dotnet toolchain.cs -- --help` lists `crap`, `complexity`, `--crap-args`, `--complexity-args`. |
| 9 | CI overlay sequence replayed in a scratch clone for a base without `scripts/code-metrics` and for a base that has it (modified plus new file): self-check passes on the base tree and the checkout back to head is clean both times. |
| 10 | `dotnet toolchain.cs docs-qa` succeeded. |
| 11 | Warm `dotnet toolchain.cs complexity`: 1.2 s with nothing changed; `dotnet complexity.cs --all`: about 4 s. |
| 12 | Cross-vendor review (GPT): no blockers. Fixed: a failed base lookup is now an error instead of "new"; git-quoted paths are decoded and `ls-files -z` is used; base matching moved to a shared file with fixtures; the toolchain exit behaviour for `--fail-on-findings` is documented. |
| 13 | Console output matches the usage model: header block, `cc`/`cog`/`status` columns, location line, summary wording, and one `Complexity-Justification:` template per method that needs action. |

## Follow-up: complexity in the pull request report

### Announcement

> **Pull requests now get complexity feedback within a couple of minutes**
>
> The sticky "Coverage and CRAP" comment on pull requests now opens with a **Complexity** section: the methods the pull request changes that exceed cyclomatic 10 or cognitive 20, marked new, worse, unchanged or improved, with a `Complexity-Justification:` template for each one that needs action. It comes from a separate, fast job that needs no build and no coverage, so it appears long before the coverage passes finish.
>
> The CRAP table below it is unchanged. It is background on coverage, not a target.
>
> Both are advisory: neither fails the pull request.
>
> Also fixed: the cached base CRAP measurement is now keyed on the measuring tooling as well as the base commit, so a pull request that changes the tooling is no longer compared against a base measured with the old tooling.

### Usage model

- `dotnet complexity.cs --markdown` prints the same findings as a GitHub markdown section instead of the console table. It combines with every scope option, `--top`, `--json` and `--fail-on-findings`. Locally: `dotnet complexity.cs --markdown --base origin/yubikit`.
- In CI the `complexity` job checks out the pull request's head and runs `dotnet complexity.cs --markdown --base <merge base> --link-base <head blob URL>`: the same diff as the "Files changed" tab, with every method linked to its lines. (First shipped as "first parent of the merge commit"; changed in the visual pass below so links and line numbers match the head.)
- The comment is one sticky comment with two sections, `complexity` and `crap`, each owned by one job and replaced in place. The `crap` job waits for the `complexity` job (and runs even if it failed), so the two never write the comment at the same time.
- Forks (read-only token) still get both sections in the job summaries.

Markdown shape:

```
### Complexity

Methods changed vs `abc1234` with cyclomatic complexity above 10 or cognitive complexity above 20. Source only; coverage plays no part.

| cc | cog | status | method | location |
|---:|---:|---|---|---|
| 14 | 12 | worse (was 12/11) | `OathSession.ValidateAsync` | `src/Oath/src/OathSession.cs:613-692` |

**1 method needs action** (new or worse). For each, simplify it or add to the commit message:

    Complexity-Justification: OathSession.ValidateAsync: <why this complexity is warranted>
```

Clean: `No changed method exceeds a threshold (N checked).` Nothing in scope: `No shipping C# methods changed.` Debt only: a note that simplifying is welcome but optional.

### Visual pass (after the first follow-up commit)

Requested: make both sections easier to read, without collapsing anything.

- Complexity: the verdict is the heading; columns are Status, Method, Cyclomatic, Cognitive; moved scores show `before → after`; bold marks a score over its limit; each method links to its lines with the path underneath; the threshold explanation moves to a `<sub>` footer.
- CRAP: headed "(background)"; a neutral summary line replaces the bold "CRAP increased by N" verdict and the note listing every module; only modules that moved are listed, plus the total; grouped numbers; `–` for no change; plain column names; the cognitive column is dropped so the comment shows one cognitive limit (the gate's 20). Console and JSON output are unchanged.

### Definition of done

14. `complexity.cs --markdown` renders the shape above for findings, debt only, clean and empty scopes, and for full scans (no status column).
15. A shared comment helper replaces one named section in the sticky comment, keeps the other, keeps a fixed section order, and replaces a comment in the old single-section format; tested locally with Node against a fake GitHub client.
16. The workflow has a fast `complexity` job (no build, no coverage) and the `crap` job `needs` it with `!cancelled()`; both write their job summary; forks skip only the comment.
17. The base cache key includes a hash of the measuring tooling; stale workflow comments are corrected; `actionlint` passes.
18. `TOOLCHAIN.md` documents `--markdown` and the pull request report.

### Verification log (follow-up)

| DoD | Evidence |
|---|---|
| 14 | Eight self-check fixtures render the empty, clean, mixed (action first, bold worse with previous scores, justification block), debt-only, full-scan and truncated shapes; `self-check: 90 passed`. Live runs: `--module Oath --markdown`, `--base 0a22a278 --markdown` (87 changed methods, clean). |
| 15 | `node .github/scripts/report-comment.test.js`: 7 scenarios (create, update keeping the other section, replace own section, fixed order, legacy comment, marker text inside a cell, unknown section). The marker scenario fails against the earlier `indexOf` parser and passes against the line-anchored one. |
| 16 | Depth-2 clone of a real merge commit: `--base $(git rev-parse --short HEAD^1)` reports exactly the pull request's worse method. The workflow has `needs: complexity` with `!cancelled()`, a per-pull-request concurrency group with `cancel-in-progress`, job summaries in both jobs, and comment steps guarded for forks. |
| 17 | Cache key: `crap-base-<base sha>-<hash of crap.cs, scripts/code-metrics/**, toolchain.cs, coverlet.runsettings.xml, .config/dotnet-tools.json>`. Stale comments rewritten. `actionlint` passes on the workflow. |
| 18 | `TOOLCHAIN.md` § Pull request report and the `--markdown` option row; `CLAUDE.md` checklist points at the branch-wide check. Cross-vendor review (GPT): no blockers; its two findings (renderer tests, marker text in cells) are fixed. `crap.cs` output is still byte-identical to the original script on the current source. |
