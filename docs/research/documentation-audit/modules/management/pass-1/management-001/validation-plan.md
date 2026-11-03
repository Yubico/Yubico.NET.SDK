# Validation plan

## Scope

Validate the integrated documentation changes on Linux after independent review. No hardware commands
are planned. Existing integration-test source is evidence for comment review, not live verification.

## Required checks

- `dotnet toolchain.cs -- build test docs-qa docs-architecture complexity` using the required build/test
  skills. Read the RESULT block, counts and warnings. Serialize toolchain runs within the checkout.
- Scope `dotnet format Yubico.YubiKit.sln --include <actual edited C# paths> --verify-no-changes --no-restore`
  to the final C# file list. Record any formatting application and harmless using-order changes.
- Compare Roslyn executable tokens/directives in edited shipping/test/example C# against `da61d2e6`;
  verify public API snapshots remain unchanged. Documentation-only changes must not alter code behavior.
- Compile meaningful current Markdown/XML examples against final Release SDK assemblies, using typed
  prerequisites for composable fragments. Retain the extracted inputs and exact commands. Never execute
  examples against devices as part of compilation.
- Check new run/handoff navigation and Markdown fences directly because research records are outside
  active-doc QA. Historical audit records remain immutable.
- Reconcile all 55 baseline module paths, public API families, external references, accepted findings,
  edited-file review ownership, clean-unit sampling and deferred implementation dependencies.
- `git diff --check`; review staged paths and final status; preserve four original untracked user files.

## Evidence retention

Copy toolchain logs/results before the five-run retention window can remove them. Keep validation
sources and outputs under `validation/`; build artifacts and temporary compiler workspace belong outside
the retained records. Record final reviewed-file SHA-256 values and the actual source revision/diff.

## Compiler-checker reference

The WebAuthn archive retains a Roslyn token comparator and in-memory example compiler under
`records/runs/webauthn-001/validation/checker-source/`. Adapt the SDK reference list for Management,
retaining provenance. Context7 checked `/dotnet/roslyn` on 2026-11-03: `CSharpCompilation.Create` accepts
syntax trees, metadata references and compilation options; diagnostics/Emit provide compile-only evidence.
Primary API: https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Compilation/CSharpCompilation.cs

Compilation proves syntax and API binding only. Unit tests prove their asserted paths, not all firmware,
transport, device state or OS combinations. Keep hardware gaps in the later Pass 2 handoff.
