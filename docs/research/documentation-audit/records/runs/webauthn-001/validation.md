# WebAuthn cycle validation

Baseline `e353b49a`; all independent reviews and correction re-reviews passed. Commands ran on Linux. No live
WebAuthn/CTAP ceremony, PIN setup, credential mutation, reset or hardware qualification was performed.

## Executed checks

| Check | Result and evidence |
| --- | --- |
| `dotnet toolchain.cs build` | PASS, full solution including unit/integration projects; zero warnings/errors. Archived `validation/20261103-144954-345-build/` |
| `dotnet toolchain.cs test` | PASS, 3,998 executed, 0 failures, 5 pre-existing skips across 13 projects. WebAuthn: 371 executed, no skips/failures. Zero warnings. Archived `validation/20261103-145050-713-test/` |
| `dotnet toolchain.cs -- docs-qa docs-architecture complexity` | PASS, 73 active docs + 10 package READMEs, architecture map/image freshness, 0 new/worse complexity findings. Two existing unchanged methods above threshold. Zero compiler/analyzer warnings. Archived `validation/20261103-145050-731-docs-qa+docs-architecture+complexity/` |
| Scoped `dotnet format ... --verify-no-changes --no-restore` | Initial failure: EOF convention on all edited files, one import-order correction. Scoped formatter applied those changes; full scoped verification then exited 0. Logs `format-apply.log`, `format-verify.log` |
| Roslyn token/directive comparison | PASS for all 78 edited C# files against baseline, normalizing only top-level using-directive ordering. Runtime tokens/directives and API signatures preserved. One namespace using reordered by formatter in LargeBlobProcessor |
| `python3 .../compile-examples.py` | PASS, three source groups extracted from current docs and compiled with Roslyn against fresh SDK assemblies: complete README demo; prompt+touch configuration variant; allowlist/application-service/signing/Markdown+XML intent fragments with typed prerequisites. Zero compiler warnings; no execution of snippets |
| `dotnet run .../experiment_nullable_blob.cs` | PASS, isolated .NET10 language/BCL conversion: null byte[] argument to ReadOnlyMemory<byte>? constructor parameter yields HasValue=True, Length=0. No SDK/device call. This resolves WA-IMP-016's evidence uncertainty, not its implementation defect |
| `git diff --check` | PASS; Git separately notes csproj LF-to-CRLF normalization under repository attributes |

The documentation/complexity toolchain and helper build briefly overlapped the test run, emitting the
toolchain's parallel-run advisory. Helper built only its isolated, dependency-free checker project;
the docs/complexity commands did not build SDK assemblies. All result blocks completed successfully
with zero compiler/analyzer warnings. Subsequent final gates are serialized.

Formatter workspace-loading diagnostics concern duplicate PublicAPI.Shipped/Unshipped AdditionalFiles
in the existing project setup. Diagnostic log lists those duplicates; formatter did load all edited
source/test projects and perform checks. This is not an unresolved code formatting failure or a compiler
warning from the full build. No analyzer setting was disabled.

After the final blob and contributor/architecture wording corrections,
`dotnet toolchain.cs -- build docs-qa docs-architecture complexity` passed again, serialized and with zero
warnings/errors (`20261103-145550-000-build+docs-qa+docs-architecture+complexity`). Full scoped formatter
verification exited 0 (`format-verify-final.log`), and Roslyn again confirmed all 78 C# files' runtime
tokens/directives unchanged apart from namespace-import order. Unit tests did not need duplicate
execution for comments/Markdown-only changes. No test was added to mirror prose.

## Review evidence

- A: 36-file independent review, two required corrections, both passed `review-a-followup.md`.
- B: 40-file independent review, one required test-summary correction, passed `review-b-followup.md`.
- B blob precision follow-up passed `review-b-blob-final.md`; C narrative review and two corrections passed
  `review-c-followup.md`. The final claim is explicitly limited to nullable presence and length; no
  backing-identity workaround or live storage proof is implied.
- Eight clean-unit Astra sample triggered expanded external-example review and two corrected call sites.
- Five consequential B public contracts independently sampled by Astra; no further correction identified.
- Checkpoint independently reviewed; count/status clarifications applied.

## Limits

Compilation establishes example/API syntax, not RP integration, authenticator behavior or cross-platform
execution. Historical/mapping sketches and removed-API “Before” snippets are deliberately not compiled
as current runnable programs. The migration YAML parse issue DOC-002 remains unrelated and unchanged.
No claim of complete standards conformance or hardware certification follows from these gates.
