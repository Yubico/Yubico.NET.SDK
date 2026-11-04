# Security Domain example compilation

## Executed result

2026-11-04, Linux; .NET SDK 10.0.403, target net10.0, C# 14. **Ten meaningful example groups compiled
in two SDK contexts with zero compiler/analyzer warnings. No example host, test runner or device operation
was executed.** Final post-correction exact-block/retained-input verification passed, and all 407
captured reference/analyzer identities match the final clean solution's files.

| Context | Groups | Actual Csc sources / references / analyzers | Result |
| --- | --- | --- | --- |
| Consumer | ReadOnly, Authenticate, Import, Generate, Reset, Inspect, OpenScp11 | 4 / 179 / 10 | Build PASS, warnings 0 |
| xUnit-v2 hardware-test context | Basic, Factory, Successive | 5 / 206 / 12 | Build PASS, warnings 0; compilation only |

## Selection and source fidelity

All five module Markdown files were inventoried: **ten C# fences**, nine selected and one explicitly
excluded instruction/tag constant navigation sketch. The inline SCP11 constructor expression adds one
meaningful group. The authentication block is reused in two administrative task groups, not counted as
additional distinct fences. No C# XML examples were introduced; the three edited shared-document sections
contain no C# task block.

`validation/examples/v1/manifest.json` retains complete original blocks, line locations, initial document
hashes, raw combined task files, typed prerequisites and exact wrappers. Preparation separates only
ordinary namespace imports from the full entry task; resource using/await/finally, test attributes and
assertions remain intact. Each separate task is a method scope. No stub helpers, assertion removal,
`dynamic` parameters or newly suppressed diagnostics were used. The constructor fragment uses typed
caller-provided provisioned key material and an owned parameter/session scope; it is not evidence of
completed provisioning or trust validation.

## Real SDK/project context

Consumer host imports canonical repository props/targets and references the actual Security Domain
project. The xUnit-v2 host sets IsTestProject before the canonical imports, copies the real integration
project's direct PackageReference items, and references that actual project for its public helper APIs.
Project-reference builds resolve normal transitive dependencies; this is compilation, not a Core audit.
The test host is a library, with no v3 runner adaptation. IsPackable=false and collision-free
`AuditSd001.Examples.v1.consumer` / `.hardware` names keep hosts out of ordinary module/test selection.
Generated output remains outside the repository under `/tmp/opencode/security-domain-001-examples-v1-*`.

Compiler membership capture verifies actual Csc source/reference/analyzer sets, `/warnaserror+`, warning
wave 9999, enabled build analysis and absence of skip switches. Consumer policy is AllEnabledByDefault;
test policy is Recommended, exactly as canonical configuration selects. `EnableNETAnalyzers=false` in
the evaluated output is NuGet analyzer duplicate selection, not a skipped analyzer run: actual Csc
arguments include pinned NetAnalyzers 9.0.0 and, in the test context, xUnit analyzers. Canonical existing
NoWarn values are recorded unchanged; no per-example suppressions were added.

## Durable evidence and limits

The two toolchain runs, compiler arguments/policy, all actual source/config text snapshots, project assets,
canonical configuration snapshots, raw/wrapped examples and binary dependency identities are retained.
Generated binaries, caches and runtime duplicates are not copied into Git. The retained-text verifier
does not pretend those omitted binaries are a durable archive; reconstruction requires normal pinned
SDK/package restore. No original compiler failures occurred for these ten groups. The separate initial
docs-qa failure is retained in the corrections/validation records.

Commands and output:

- `validation/prepare-examples-v1.log`
- `validation/examples-v1-consumer.log` and its retained toolchain directory
- `validation/examples-v1-hardware.log` and its retained toolchain directory
- `validation/capture-examples-v1-consumer.log`, `validation/capture-examples-v1-hardware.log`
- `validation/verify-examples-v1.log`
- `validation/final-examples.log` and `validation/final-example-dependencies.json`

Full compilation supports API binding, resource-scope syntax and analyzer checks. It does not prove
hardware compatibility, correct trust provisioning, reset restoration, secret erasure or all-platform
behavior. Final prose-only corrections may change document hashes without changing the compiled blocks;
the final verifier must compare every original block and wrapper rather than silently re-baseline them.
