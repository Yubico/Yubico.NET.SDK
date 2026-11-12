# Independent Sol review: accepted A/C compile pilot

## Decision

**One bounded required correction, EX-R1, before the coordinator build.** Extraction, accepted-body
identity, genuine consumer context and the one-host compiler-capture design are otherwise ready.
The read-only verifier passed. This is not a request for a general capture framework, SDK tests,
rewriting accepted bodies or broadening this 17-body pilot.

Source baseline: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
Observed metadata HEAD: `da66f5d01ba594993974ddd26723e8c300c0a454`. Metadata may advance independently.
Prior reviews and qualified logger/checker sources are preserved.

## EX-R1 — account for referenced-project output ownership

**Paths:** `Directory.Build.props:2–6`, `run.py:68–76`,
`host/src/Yubico.YubiKit.Audit20261111.DocExamples.csproj:7–9`, and
`../../example-validation-preparation.md:24–29` (the last path is relative to this examples directory).

The preparation says that all intermediates/outputs for the five-project closure go to the home-backed
`core-001/jobs/<project-name>/` workspace. The current mechanism establishes that for the example host
only. Each referenced SDK project independently finds the repository-root `Directory.Build.props`;
Core, Piv, WebAuthn and transitive Ctap2 are not descendants of this examples directory. Their project
files and the root props do not contain this home routing. `run.py` forwards only
`DirectoryBuildTargetsPath` and `DocExamplesCapture`, not early output/restore-path routing. The capture
targets also add no such routing. Local host properties are not global properties inherited by a
ProjectReference build.

Consequently, the exact clean command can clean/rebuild those SDK projects in their ordinary `src/.../bin`
and `src/.../obj` locations, contrary to the stated output-ownership scope. Matching host Csc reference
hashes does not prevent that mutation. Source-before/after enumeration explicitly excludes `bin`/`obj`,
so its equality check cannot establish preservation of those original output bytes.

**Required bounded correction:** reconcile this before running `examples-ac-first`. Prefer a small
run-owned early props mechanism shared by restore, clean and build that imports unchanged root policy
and routes the exact five-project closure's intermediates, project-extension/restore state and outputs
to distinct home-backed directories. Check its propagation and effective paths for the referenced
projects, not merely the host. Do not edit shipping project files or qualified checker/logger tooling.
Alternatively, if ordinary SDK output paths are intentionally authorized, the coordinator must explicitly
accept that scope, correct the all-home claim and establish preservation of any required original SDK
outputs before clean. That is a scope decision, not an implicit consequence of this readiness review.

Preserve the current preparation descriptor/report as original evidence; bind any changed preparation
files in a fresh reviewed descriptor/checkpoint. The manifest/body bytes need no change for this issue.
An existing accepted command logger and one small props adjustment suffice; no new graph interpreter,
compiler-proof version stack or framework is requested. Re-review only that routing/scope correction.

## Accepted preparation findings

- **Exact accepted inputs.** Manifest SHA-256 is
  `f864583c4f36050eb1543a1f4cc1ca2a78c7001577ec8ea13c4715575b6b6f60`.
  The A revision-2 and C revision-2 review/inventory authorities match their recorded identities.
  All **eight** full source documents match their accepted/current hashes. The pilot does not borrow
  acceptance from mutable worktrees or claim compilation credit for the larger accepted source set.
- **Honest population.** All **41** example records have explicit dispositions: **17** prepared bodies
  (**four complete, 13 typed fragments**), **two** internal A illustrations and **22** historical C XML
  regions. The two internal expressions receive no consumer compilation credit, copied implementation,
  impersonated assembly name or new IVT. Historical diagrams/output/ASN.1 regions stay excluded.
- **Extraction and wrappers.** Exact raw XML region bytes, byte boundaries, line ranges and hashes are
  separate from the decoded derivative and wrapper. All bodies reconstruct exactly. The Manager import
  prefix remains verbatim outside its containing class. Wrappers add imports/namespace/containing class
  and real typed parameters, not fake SDK methods, substituted assertions or stubs. Complete prompt,
  borrowed PIN-session, RSA-owner, WebAuthn scope and provider-resource bodies stay intact. Provider
  factory assignments are retained but are not executed by this library host.
- **Real consumer host.** The non-packable library has actual Core/Piv/WebAuthn references and Ctap2
  transitively: one host plus four SDK projects. Its name avoids `Core` and `.UnitTests` selector
  collisions. There is no entry point or call to any example. Root compiler/analyzer policy is imported
  unchanged; there are no host warning suppressions or altered access grants.
- **Bounded compiler observation.** The hook applies only to DocExamples, imports actual root targets,
  waits for the selected SDK's Source Link/embedded-file producers, captures before inputs and reads
  real `CscCommandLineArgs` through the proven late-CoreCompile mechanism. Compiler membership must
  include the 17 exact host files and only genuine generated sources beneath this host's job workspace.
  References/analyzers/config argument membership is exact. AdditionalFiles are excluded by rejecting
  actual `/additionalfile:` arguments in this fixed host; no arbitrary additional-input exemption is
  introduced. Source Link and embedded inputs enter the before `other` population.
- **Known filename/context handling.** The compiler-file parser is loaded read-only at its qualified
  hash. Literal comma/quote handling is reused unchanged. This pilot expects absolute descriptor paths;
  membership checks reject a descriptor resolving elsewhere. No checker globals are rebound, no
  checker capture/admission is called and no new generalized filename parser is needed.
- **Actual evidence remains necessary.** A new canonical clean toolchain build must emit complete
  source/input/argument/output records. Inputs are rechecked after Csc; final host DLL bytes must match
  the actual implementation output, with original reference/PDB identities retained externally. The
  wrapper checks full current source/configuration identities across the declared closure before/after,
  verifies actual output bytes and requires the successful complete zero-warning original v2 receipt
  before writing `accepted-compilation.json`. A present DLL, up-to-date build or successful generic
  logger result alone supplies no acceptance.
- **Qualification boundaries.** The accepted v2 process/stream/quiescence mechanism and SIGTERM
  exception path are reused; temp environment comes from the existing home-backed helper. There is no
  duplicate SDK-info or logger qualification. Compiler/runtime/provider/device execution, secret
  cleanup execution and lifetime behavior are not inferred from compilation. Examples are not run.

## Fresh read-only checks

After inspecting its entry point/imports, ran:

```bash
python3 -B docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/verify.py
```

Exit **0**: exact region extraction and wrapper reconstruction, 41/17 population, host source set,
root imports, non-packable library configuration, declared five-project closure, late hook and reused
SDK-info readiness checks passed. This command neither invokes SDK/compiler/capture code nor writes
the preparation files.

Separate byte/hash reads verified all **183** fixed preparation-descriptor rows and **145** supplied
owned-record hashes, the eight accepted full-document identities and both authority hashes, and all
declared qualified reuse hashes. Source reads independently confirmed the referenced projects' nearest
props and absence of routing propagation, the basis for EX-R1. Current Context7 `/dotnet/msbuild`
documentation/source context was consulted for upward props discovery and ProjectReference property
flow; the local files and exact command supply the concrete finding.

The known B4 source-authority schema rejection is a separate record-checker matter. It was not fixed,
broadened or treated as a compile blocker here. Final whole-run record/staging checks retain their own
coordinator checkpoint.

## Conditional coordinator commands

**Do not run the second command until EX-R1 is reconciled and the changed preparation is accepted.**
With exclusive checkout ownership and the current build skill loaded, the intended sequence remains:

```bash
E=docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples
python3 -B "$E/verify.py"
python3 -B "$E/run.py" --label examples-ac-first
```

Read the actual original command receipt, zero-warning toolchain RESULT/full step logs and
`attempts/examples-ac-first/{source-before,before,compiler,finish,source-after}.json`. Accept
`accepted-compilation.json` only with those successful actual records and the reconciled output paths.
Preserve the first failure if any. Return genuine source diagnostics to the example owner; a wrapper
context correction needs unchanged-body proof, review and a fresh attempt. No example DLL is run.

## Reviewed identities and write boundary

Paths below are relative to `validation/examples/`.

| File | SHA-256 |
| --- | --- |
| `manifest.json` | `f864583c4f36050eb1543a1f4cc1ca2a78c7001577ec8ea13c4715575b6b6f60` |
| `preparation-identities.json` | `77fdf952861491d35288531a6070d6ff069df155e18d1633c950bdd8f9d7657d` |
| `verify.py` | `79b07a13f035fc13676cba8ad92ccd9e61409c65b5261a1a58e1141b9c06f2f5` |
| `run.py` | `0cd2d66da89c850ac236d16853072649e78cf04f8036978c1a9cd21129036629` |
| `capture.py` | `482151879d4c9e8c911a8e6615352a1c05a6406d58383dbc96a8d36154112eed` |
| `capture.targets` | `8ca4e8a5fe6c2ca7e9a4c0b177dd2bb39904839f93ca214d43c37548404c65ec` |
| `Directory.Build.props` | `d5ada0486cdd5b993a1d1f2807ea0aefffb863302134f8579cd78a7db1b147a8` |
| `static-verification.json` | `20b42a609819e28fa1197e32bba13a4379825de4afceaa897c3a2e63037db789` |

The preparation document SHA-256 is
`e26d2deff2bf4e2cd5259e0aae1029ac84a5be15217543e870cc8979d63b74c4`.
Only this `review.md` is written. No SDK build/test, actual compiler capture, example/runtime execution,
native/device operation, formatter, product/source edit, Git mutation or subagent ran. Active Pass-1
work, final coverage, Windows handoff and PIV physical deferral retain their existing owners and bounds.
