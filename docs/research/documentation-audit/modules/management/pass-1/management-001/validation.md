# Management Pass-1 validation

Baseline: `da61d2e6d754b4c155cbb2579202e910986f4b3a`. Commands ran on Linux. No live Management
operation, configuration change, lock change, device reset, integration-test execution or cross-platform
verification occurred. Compile-only snippets are never loaded or executed.

## Repository gates — passed

`dotnet toolchain.cs -- build test docs-qa docs-architecture complexity` completed successfully in
`20261103-195646-771-build+test+docs-qa+docs-architecture+complexity`:

- Full Release solution build, including existing example and integration-test compilation.
- **3,998 unit tests**, zero failures, five existing skips across 13 projects. **Management: 96 passed**,
  zero failures/skips. All compiler/analyzer warning counts: **0**.
- Documentation QA: 73 active documents and ten package READMEs.
- Architecture evidence: six map entries and seven fresh images.
- Complexity: the one method in changed-line scope is within thresholds; no new/worsened violation.

The [complete run log](validation/20261103-195646-771-build+test+docs-qa+docs-architecture+complexity/run.log),
[result JSON](validation/20261103-195646-771-build+test+docs-qa+docs-architecture+complexity/result.json),
and all seventeen files from that run are retained. The source-token check below distinguishes comment
line changes counted by complexity from executable changes.

## Formatting and preservation — passed

- Scoped formatter verification passed on all **37 edited C# files**; exact file arguments and outputs
  are in `validation/format-verify-final.log`. No unrelated file was formatted.
- Initial verification found the repository's no-final-newline convention and unused imports after XML
  edits. Useful typed cross-references were added and independently queued for review; imports were
  preserved. Initial formatter removal of the SCP import was detected by the post-format check, then
  restored with its meaningful cref. Failed logs remain retained and are not passing evidence.
- `validation/format-final-changes.json` records final whitespace-only formatting deltas. Earlier
  changes and the deliberately rejected import-removal delta remain in `format-changes.json`.
- Roslyn token/directive comparison against the baseline passed for **37 files**, allowing only
  directive-free using-order normalization. It includes all edited shipping, test and executable-example
  C#; excludes historical/checker/extracted-input records. See `validation/final-tokens.log`.
- All **20 public API snapshot files** have the same baseline/current paths and byte-identical contents.
  See `validation/final-api.log`. This complements the compiler's normal API-analyzer build checks.

Diagnostic formatter verification identifies the pre-existing duplicate PublicAPI.Shipped/Unshipped
workspace entries for the ten SDK projects; `validation/format-workspace-diagnostics.log` retains the
exact messages. Compiler/analyzer warning counts in the actual solution build are zero. Both final
normal and diagnostic formatter verification exited zero.

## Example checks — 63 groups passed in their recorded contexts

The extractor inventories **146 blocks**: **63 selected complete typed compilation groups** and
**83 explicit exclusions** (commands/configuration, historical examples, internal sketches and unrelated
fragments). It retains original extracted text, wrapped source, provenance and source/reference hashes.
All meaningful selected tasks keep their real API calls, assertions, branches, cleanup and lifetimes.

Preliminary compilation caught a real nonexistent `DeviceCapabilities.SecurityDomain` in a shared
example; the source documentation was corrected and independently re-reviewed. The neighboring
custom-filter example now performs a meaningful fresh read. The initial hypothesis that it would emit
CS1998 was not borne out by this installed compiler; [diagnosis](compiler-diagnostics.md) preserves that
distinction and calibration evidence rather than claiming an observed warning that did not occur.

Raw Roslyn also reports CS1701 for existing SkippableFact/xUnit2 dependency identities (2.4 versus 2.9.3).
The checker remains strict and these failures remain visible. Real SDK/MSBuild reference resolution has
an existing standard policy for redundant CS1701/CS1702 diagnostics. The affected snippet groups were
compiled in that real project context, with normal analyzers and warnings-as-errors, rather than adding
suppression or fake dependencies to the standalone checker.

- **37 groups:** strict raw Roslyn compilation passed with zero warnings/errors; retained output
  `validation/snippets-final-strict-raw.log`. This includes the separated xUnit v3 root example and
  consumer/sample examples; actual operations were preserved and never executed.
- **26 xUnit2 groups:** the isolated real SDK/MSBuild host passed a fresh toolchain build with zero
  warnings/errors at `20261103-200621-338`. It uses actual repository package/reference resolution and
  NET/xUnit analyzers with warnings-as-errors; no project diagnostic suppression or fake dependency was added.
- The membership is explicit and disjoint: **26 + 37 = 63**. All wrappers, extracted original text,
  document hashes and referenced Release DLL hashes are retained. The final collector validated actual
  Csc source/reference/analyzer inputs and recorded them under
  `validation/checker-source/xunit2-context/evidence/`.
- A collector initially misunderstood `EnableNETAnalyzers=false`: the canonical NetAnalyzers NuGet
  package disables duplicate SDK-bundled analyzers while supplying the real analyzer DLLs. The actual
  Management integration project has that same policy. [Collector correction](analyzer-context-correction.md)
  retains the failure and verified package behavior; the corrected guard requires the actual package
  replacement and analyzer execution. The fresh host build and successful collection are both retained.

This does **not** mean all 63 groups are warning-free under the raw checker. Its xUnit2 CS1701 diagnostics
remain valid preserved observations; the project-context result uses the real SDK's existing redundant-
warning policy after reference resolution. No runtime assembly compatibility or hardware result is inferred.

See [snippet preparation](snippet-preparation.md), [compiler diagnosis](compiler-diagnostics.md), and
retained original inputs in `validation/preliminary-examples/`. Helper builds were serialized through
the repository toolchain; all passed with zero warnings. Their logs and experiment failures are retained.

## Limits

Source inspection, example compilation and existing unit tests do not establish device-family support,
firmware interoperability, full specification conformance, successful reset/configuration restoration,
or Windows/macOS behavior. Pass 2 retains implementation and hardware dimensions separately. Existing
historical migration YAML issue DOC-002 is unchanged and no parser repair is included in this run.
