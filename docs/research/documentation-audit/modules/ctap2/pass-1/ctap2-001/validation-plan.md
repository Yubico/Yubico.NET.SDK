# CTAP2 Pass-1 validation plan

Use the required build/test skills and canonical repository toolchain. Serialize toolchain use per checkout.
No hardware/integration execution is planned; snippet checks only compile, never load/run device operations.

## Final gates

- Full solution `dotnet toolchain.cs -- build test docs-qa docs-architecture complexity`, zero compiler/
  analyzer warnings, actual test counts/skips recorded. Latest previous baseline was 3,998 unit tests with
  492 CTAP2 cases; re-establish counts from the actual final run rather than assume they remain unchanged.
- Scope formatting to actual edited C# paths, and verify executable tokens/directives and public API
  snapshots against `5b09a9b6`. Preserve behavior/test assertions/executable sample strings. Avoid unrelated
  import cleanup; useful XML references should remain compiler-resolved. Record formatting-only deltas.

**2026-11-04 formatter adjudication:** the full scoped formatter also reports inherited unused namespace
imports. Applying this mandated style check may remove those non-executable imports. Retain the exact
pre-format source/hashes, formatter diagnostics and concrete removal manifest; require zero added imports
or other non-whitespace changes, and independently compare every declaration/body token and preprocessor
directive after accounting only for the listed ordinary namespace imports. Alias/static/global/unsafe or
conditional import changes are not covered. Preserve the original strict checker and report this explicit
syntax exception separately, with fresh build/API/test evidence; never call all source syntax identical.
This does not authorize SDK behavior, API, test-assertion or executable-string changes.
- Compile meaningful complete/factored Markdown/XML examples with exact typed prerequisites and real
  SDK/example/test dependencies. Preserve whole tasks, assertions, cleanup and scope. No fake/no-op API
  replacement, removed calls or suppressed diagnostics to claim success.
- Test/sample-library examples use actual SDK/MSBuild reference/analyzer policy. The Management cycle's
  retained helper/host designs can be adapted, with fresh baseline/module/source-group selection. Raw
  compiler context diagnostics stay separate from the real project gate; xUnit v2 and v3 remain separated.
- Verify local links mechanically across edited active documents and run records, including actual path
  resolution. Historical evidence remains immutable; generated/old/internal-sketch examples excluded explicitly.
- Reconcile all 183 module paths, effective public families/948 initial entries, current external references,
  complete examples, named clean-unit sampling, independent changed-file review and required re-reviews.
- Fresh cross-layer review must accept the final CTAP/WebAuthn boundary map and both modules' affected claims.
- Retain exact commands, outputs, source/reference hashes, failed attempts, final content patches, record
  hashes and independent closeout review. Stage explicit owned paths, commit/push and verify remote HEAD.

## Evidence and tools

The immutable Management run retains proven token/compile tools, real-project context construction and
diagnostic lessons. Read their final validation/collector correction before reuse; old pending descriptions
and baseline-specific inventories are historical. Reuse mechanisms, not old results or hardcoded group counts.

Known preparation traps: SDK-bundled analyzer flags can be false when canonical NuGet analyzers replace
them; verify actual analyzer inputs/execution. SDK/MSBuild handles redundant reference diagnostics after
real resolution; do not copy suppressions into a standalone compiler. Preserve original raw/compiler results.

The validation record must distinguish source inspection, compiled examples, executed unit tests and
not-run hardware/platform behavior. A green documentation pass is not protocol conformance or live
registration/authentication/UP/UV/PIN/biometric/reset qualification.
