# Security Domain validation

## Current state

Final content gates passed: independent content review, examples, clean solution build/unit tests,
documentation/package QA, architecture, complexity, scoped formatting and content/API preservation.
Fresh independent final record review passed in review-closeout.md. Publication remains coordinator-owned
and is recorded outside the frozen run. See status.md for snapshot progression.

## Method

- Reuse the CTAP token/API checker with baseline/run/output identity substitutions recorded by
  `validation/prepare-checker.py` and `checker-provenance.json`; checking algorithms stay unchanged.
  Strict token mode is the default. CTAP's import exception is not approval for this cycle.
- The standalone checker is a validation utility, not a shipping or test project. Outputs live under
  `/tmp/opencode/security-domain-001-doccheck`; collision-free project name `AuditSd001.Checker`.
- Final example contexts will retain complete source blocks, typed prerequisites, genuine references and
  analyzer diagnostics. Compilation does not execute examples or establish hardware behavior.
- Run final full-solution warning-free build/unit tests and docs/package/architecture/complexity gates
  serially through the toolchain. Retain its logs before five-run rotation can remove them.
- Compare earlier finalized archive manifests and the four original untracked files against baseline;
  publication records live outside frozen run archives.

## Documentation consulted

2026-11-04 Context7 `/dotnet/docs`, query `dotnet format --include --verify-no-changes --exclude-diagnostics
format command options`, returned `https://github.com/dotnet/docs/blob/main/docs/core/tools/dotnet-format.md`:
`--include` scopes paths; `--verify-no-changes` exits nonzero if formatting would occur. Repository guidance
additionally requires actual solution membership and space-separated paths. This supports validation
command syntax, not SDK behavior.

## Preparation attempts

An initial read of CTAP `validation/checker-source/Program.cs` returned file-not-found; directory inspection
located the retained source under `checker-source/src/Program.cs`. No validation command failed or SDK
source changed in that navigation attempt.

## Formatter and content preservation

- Corrected pre-format strict Roslyn token/directive comparison: 18 C# paths, zero failures.
- Initial scoped formatter verification exited 2: expected trailing-newline normalization and IDE0005
  at the first unnecessary import. Apply exited 0; final scoped verification exited 0.
- Formatter removed **two** ordinary namespace imports from `IYubiKeyExtensions.cs`:
  `Yubico.YubiKit.Core.Protocols.SmartCard.Apdu` and `.Scp`. Other content is character-identical after
  removing ordinary import lines and normalizing final newlines (import ordering separately allowed).
- Initial transform recorder expected one removal from the single diagnostic location and failed closed.
  The exact diff established two; original recorder and failure are retained as
  `validation/record-format.initial.snapshot` and `record-format.log`. The corrected recorder permits
  exactly these two named removals in that one file, not arbitrary import loss.
- Run-local import manifest binds baseline, pre-format source bundle, final source, actual apply command,
  diagnostic log and transform hashes. Existing strict Roslyn algorithm independently checks pre-format
  syntax and final syntax after only those ordinary-import removals: 18 paths / one exception file / two
  explicit removals / zero failures. This is not a claim that every syntax token is identical.
- Final byte comparison: all 20 repository API snapshots unchanged, including both Security Domain
  snapshots. It is preservation checking, not other-module API investigation.
- Formatter workspace loading reports duplicate API source entries; diagnostic output is retained.
  These are distinct from compiler/analyzer warnings in build results. No analyzer was disabled.
- `reviewed-files.json` connects all 26 independently accepted pre-format hashes to final hashes;
  `coverage-final.json` reconciles all 38 paths (23 edited, 15 unchanged), eight public families / 96
  snapshot entries and the conservative final 3/9 clean sample set.

## Examples

See [snippet validation](snippet-validation.md): ten meaningful groups compiled in two real contexts,
zero warnings. Original blocks and wrappers remain unchanged after review corrections. Actual Csc
source/reference/analyzer membership and durable text/config evidence are retained. No host/test/device
execution follows from compilation.

## Final repository execution

Executed `dotnet toolchain.cs -- build test docs-qa docs-architecture complexity --clean` through
`validation/run-command.py final-repository`; full output and original toolchain run retained at
`validation/final-repository.log` and
`validation/final-repository-20261104-094646-468-build+test+docs-qa+docs-architecture+complexity/`.

- Clean full solution compilation, including integration-test projects: PASS, zero compiler/analyzer
  warnings and zero errors. Integration tests were compiled, not executed.
- Unit tests: **3998 executed, zero failures, five existing skips**, 13 passing projects. Security Domain:
  **50 executed, zero failures/skips**. The four CMAC vectors are among existing module unit cases.
- Docs QA: **73 active documents and ten package READMEs**, PASS. SD-QA-001 is resolved in this final run.
- Architecture: six evidence-map entries and seven fresh image pairs, PASS. No diagram source or image
  changed in this cycle; existing outputs retained.
- Complexity: changed-line scope inspected six files / three methods; all three within thresholds.
  This is not a claim that no complexity debt exists elsewhere.
- Native-AOT contract and hardware-test declaration static gates passed; no AOT publish or live hardware
  operation was executed.

Final example check proves all ten current fenced bodies, selected groups, inline constructor and
compiled wrappers remain unchanged after corrections. All **407 reference/analyzer identities** from
the two executed contexts match those present after the final clean solution build (including actual
Core/SD/integration reference assemblies). No recompilation is needed for prose-only citation/link
changes with exact unchanged blocks, wrappers and compiler dependencies. Final hashes are retained in
`validation/final-example-dependencies.json` and `validation/final-examples.log`.

## Remaining limits

No hardware integration, SCP handshake against a device, key operation, reset, PIN/touch action or
cross-platform run occurred. Final/profile GP evidence, SCP11c applicability and inherited Core questions
remain as recorded in findings.md. DOC-002 is the untouched pre-existing YAML parse error outside the
changed SD scalar; shared-document review does not claim whole-file YAML validity. The original
AI-DOCS-GUIDE memory-management anchor issue remains outside the edited SD excerpt.
