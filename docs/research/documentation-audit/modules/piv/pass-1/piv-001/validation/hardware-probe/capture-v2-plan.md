# Versioned compiler capture correction

## Preserve the first failure as historical evidence

The coordinator's `p01-x25519-v2` clean build passed. Its original 44-line capture tool
then rejected `EnableNETAnalyzers=false` before any test, reset or device operation.
That flag selects the explicitly pinned NetAnalyzers 9.0.0 replacement, as proved by the
existing PIV example/integration capture tools; actual Csc contains both canonical analyzer
assemblies. Original failed artifacts remain under `evidence/p01-x25519-v2/`.

A separate actual policy mismatch also exists: the intact hardware test host records
`AnalysisMode=Recommended`, inherited from root test defaults. It was never an AllEnabled
build. The correction retains it truthfully instead of relabelling that mode.

The editor subsequently completed six required documentation/comment corrections,
including PivTool Attestation.cs/AttestationResult.cs and IPivSession. Their executable
tokens remain unchanged, while their source hashes differ from the first P01 compilation.
The old failed capture remains historical; current source hashes must not be presented
as that old compilation's inputs. The earlier proposed manual recapture is withdrawn.
Version 2 has no legacy-retention mode: it requires the richer fresh-build metadata and
AllEnabled policy before retaining complete current source/config/assets evidence.

## New build/execution entry point

The audit host's local props now explicitly select `AllEnabledByDefault` after the root
test defaults. The capture hook adds missing effective-policy fields, declared/resolved
project reference items and imported project inputs. Original props/targets are preserved
byte-for-byte in `capture-v1-preserved/`. Probe bodies, fixtures, method catalog, expected
failure assertions, physical deferral and device logic are unchanged.

After the coordinator freezes corrected source through the compile/capture window, use
the versioned wrapper for a fresh clean build:

```bash
/usr/bin/python3 docs/research/documentation-audit/modules/piv/pass-1/piv-001/validation/hardware-probe/run-probe-v3.py P01_X25519_Rfc7748AliceAndBobAgree --name p01-x25519-v3
```

Use the same six P01–P06 case names and unique evidence stems from `supplemental-plan.md`,
replacing only `run-probe.py` with `run-probe-v3.py`. P07 stays blocked. The new wrapper
differs from the preserved current wrapper by exactly two `capture-compiler.py` →
`capture-v2.py` path substitutions. It still clean-builds, captures before hardware,
retains raw exits and performs independent cleanup only under its existing boundaries.
Capture's default exit zero requires strict AllEnabled qualification; an unqualified or
failed capture stops before pre-cleanup/test execution.

For a coordinator-separated build/capture workflow, first run the repository build skill's
clean unique-project build. Immediately after that **fresh** build, the manual capture
command is:

```bash
/usr/bin/python3 docs/research/documentation-audit/modules/piv/pass-1/piv-001/validation/hardware-probe/capture-v2.py docs/research/documentation-audit/modules/piv/pass-1/piv-001/validation/hardware-probe/evidence/p01-x25519-v3-manual-compiler
```

Hold source stable until this capture completes and run it before formatting or another
build. The wrapper normally performs both stages itself; the manual command establishes
compiler evidence only and starts no test/device operation. Each later case's fresh build
and immediate capture bind that case to its own actual source time.

## Proof scope

`analyzer-policy-v2.py` is an exact byte copy of the current run's proven
`validation/examples/compiler_policy.py`. Its canonical proof requires the explicit root
PackageReference/pin, restored 9.0.0 package/assets/import files, known SDK import order,
both exact analyzer DLL paths/hashes, exact actual-Csc analyzer membership, warnings-as-errors,
warn level 9999 and active analyzer/compiler execution. It accepts the built-in-selection
flag false only with that complete explicit-package proof.

`capture-v2.py` adapts the existing PIV example and frozen HSM/OpenPGP reconciliation
algorithms. It requires exact normalized actual-Csc set equality for all source,
reference, analyzer and analyzer-config arguments. It checks every local probe source and
embedded-source membership, net10, C#14, nullable enable, WAE, warn9999, preview analysis
level, no compiler/analyzer skip and genuine xUnit analyzers. The four declared real project
references must exactly match Core, PIV, Tests.Shared and PivTool, and each must bind one
actual Csc ref DLL rooted in that project's own obj directory. New captures additionally
require exact actual ProjectReference item equality; resolved-project items remain retained
as evidence, accommodating their transitive shape without inventing a direct-only equality.

The archive retains complete host/generated source, analyzer configuration, restored
assets, root/local project and build configuration, package/import proof files and current
actual PivTool source text. Reference/analyzer/runtime output DLLs remain outside RUN;
their live paths, lengths and SHA-256 identities are recorded. The matching build log
establishes compiler success and diagnostics; capture establishes context/provenance.
The existing wrapper still checks loaded assembly events and unchanged pre/post-test DLLs.

`capture-v2-provenance.json` records original/new tool hashes, exact analyzer-proof copy,
the two wrapper substitutions, hook diffs and retained failed-artifact identities. This
worker inspected current Csc text and prepared sources only; it ran no capture tool,
compiler, test, hardware command, formatter, commit or subagent. Coordinator retention and
the future AllEnabled clean build remain pending.

The disjoint serial-125 helper/plan/provenance preparation is complete and unaffected by
this correction. Its control still precedes unattended two-key mutation after P01–P06;
the user deferred physical ceremonies only.
