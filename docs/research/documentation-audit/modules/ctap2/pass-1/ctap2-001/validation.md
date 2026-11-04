# CTAP2 Pass-1 validation

Baseline `5b09a9b6942ded3913b5c9340c3e126617e61a82`; Linux. No CTAP/WebAuthn hardware ceremony,
PIN/UV interaction, credential/configuration/biometric mutation, device reset or integration-test
execution occurred. Pure framework probes and compile-only examples are separately identified.

## Repository gates — passed

The initial integrated gate and the final post-format clean gate both passed. The authoritative fresh
command is `dotnet toolchain.cs -- build test docs-qa docs-architecture complexity --clean`, run
`20261104-013307-468-build+test+docs-qa+docs-architecture+complexity`:

- Full solution clean Release build, including existing example/integration-project compilation.
- **3,998 unit tests**, including **492 CTAP2**; zero failures, five existing skips across13 projects.
- **Zero compiler/analyzer warnings**. No warning policy or SDK/test analyzer configuration weakened.
- Documentation QA:73 active documents and10 package READMEs.
- Architecture:6 evidence-map entries and7 fresh image pairs.
- Complexity:56 methods in changed-line scope;4 unchanged existing threshold violations, zero new/worse.

Complete logs/result/step files from this run and the earlier `20261104-010554-101` gate are retained
under validation/. Comment-line scope is not an executable-change count.

## Formatting and executable/API preservation — passed with explicit import accounting

- Scoped formatter verification passed for **152 edited C# paths** (`validation/format-final.log`).
  Its workspace diagnostics are the previously observed duplicate PublicAPI AdditionalFiles entries;
  actual solution compiler/analyzer warning count remains zero.
- Original strict Roslyn comparison passed for all152 files **before** formatting. Exact accepted
  source texts/hashes remain in `validation/pre-format-content.json` and pre-format-tokens.log.
- Formatter applied EOF/whitespace, import ordering and **31 unused ordinary namespace-import removals
  in30 files**:29 redundant CTAP2 imports and two unused Core imports. This is an explicit syntax
  exception, not a claim that every source token is identical or an SDK behavior fix.
- `validation/import-removals.json` records exact paths, directives, baseline/pre/post hashes and
  formatter provenance. A separate fail-closed checker mode validated only those removals and retained
  the original strict mode. All declaration/body tokens, other imports, literals and preprocessor
  directives match the baseline after this bounded accounting; **152 files, zero failures**.
- All **20 public API snapshot files** have unchanged path sets and byte-identical contents.
- `reviewed-files.json` reconciles165 authored content paths with independent review snapshots and
  bounded formatter/EOL transformations; two csproj files differ only by checkout CRLF/LF. Their actual
  semantic edits are Description/comment only, with project references/policies preserved.

Evidence: formatter-preservation.md; validation/format-initial.log, format-apply.log, format-transform.json,
pre-format-tokens.log, final-tokens.log and final-api.log. Final checker clean build at
`20261104-013718-241` passed with zero warnings.

## Architecture image regeneration — passed

Pinned Mermaid CLI11.16.0 was initially absent. Repository `npm ci` installed lockfile dependencies;
package.json/package-lock.json remained unchanged. The log retains npm's existing dependency/install-
script diagnostics; no dependency upgrades or npm audit fixes were performed. Existing Chromium
155.0.8059.84 was selected with the documented PUPPETEER_EXECUTABLE_PATH setting, using /tmp/opencode
for temporary/browser-cache paths. No desktop/system/browser configuration was changed.

The existing renderer processed all7 source blocks. Because output IDs are nondeterministic, the five
unchanged-source pairs retain their exact verified baseline images/manifest rows under the same tool
version/configuration. New L2/L3b SVG/PNG pairs and the manifest are retained: five generated changes.
Image/source hashes and architecture freshness checks passed. Astra inspected both new PNGs: revised
CTAP labels and request/backend/response flow are present and readable. This is diagram validation,
not verification of every unrelated Core/PIV path depicted.

Evidence: validation/diagram-preflight.log, diagram-npm-ci.log, diagram-render.log/.json and
diagram-freshness.log; generated file hashes in reviewed-files.json.

## Framework evidence

The bounded framework probe compiled and ran pure CBOR/Guid code only, with no SDK references/device
access. It rejected the inferred insertion-order wire bug, confirmed the narrow trailing-byte mechanism,
and separated actual little-endian results from code-inferred hypothetical big-endian behavior. Original
sources, exact versions, commands, outputs and harness snapshots are retained in evidence/framework/.
See framework-adjudication.md and tooling-discovery.md; renamed current harnesses do not rewrite the
original execution. Ordinary module build discovery excludes this run's AuditCt001 tool names.

## Example compilation — all33 groups passed

33 complete source groups select39 blocks from95 inventoried blocks, with56 explicit exclusions.
Six disjoint real SDK/MSBuild contexts separate consumers, top-level programs, xUnit3 and hardware-test
xUnit2 dependencies. No snippet/test/host assembly is executed. Genuine helpers and exact original
statements/cleanup remain; no replacement SDK implementation or assertion removal is authorized.

The first host attempt exposed an extractor error: a resource `using var` declaration was hoisted as
a namespace import. Failed inputs/scripts/host/logs were preserved, then the extractor was corrected
to distinguish imports from resource statements. All95 original blocks and33 raw files remain unchanged;
only guide-protocol's wrapper moved its resource back into its original method lifetime.

All six clean SDK/MSBuild hosts passed with zero compiler/analyzer warnings, accounting for all33 groups:
consumer-lib19, consumer-demo1, unit-v3 four, hardware-v2 four, consumer-wa-demo1, consumer-wa-lib four.
Actual sources, resolved references, analyzers, Csc arguments and executed policy are retained per context.
No group, operation, assertion, cleanup or real dependency was removed/replaced to obtain success.

The generic xUnit3 collector rejected a genuine package-supplied DefaultRunnerReporters.cs source after
the successful build. That failure is preserved. A narrowly scoped supplement verifies the exact source
against pinned package assets, targets, nupkg bytes and actual Csc membership while retaining all existing
policy/reference/analyzer checks. All six collections passed final reconciliation; no recompilation or
host execution was needed for that evidence-only correction.

See [snippet-validation.md](snippet-validation.md) for all commands/results, retained failures and proof
limits. The optional raw partition is explicitly not applicable; no disappearance of raw CS1701 or
runtime assembly/device interoperability is claimed. Generated runtime binaries from failure workspaces
are subject to explicit retention exclusions; complete text provenance and pinned package proof remain.

## Limits and unrelated existing work

Unit/source/compile evidence does not establish protocol conformance, product/firmware interoperability,
Windows/macOS behavior, real UP/UV/PIN/biometric/blob/reset behavior or restored device state. All selected
implementation and hardware work waits for the second sweep. The existing migration YAML parse issue
DOC-002 remains; the active mapping received only its narrow CTAP/PIN wording correction. Older retained
Management audit-host name collisions are a separately recorded tooling follow-up, not silently repaired.
