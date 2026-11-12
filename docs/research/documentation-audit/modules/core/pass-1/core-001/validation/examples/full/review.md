# Independent Sol review: full-example candidate readiness

## Decision

**Bounded corrections required before the full context release.** Source/authority/profile identity,
explicit qualifications, real-context selection and preservation checks passed. The current candidate
still has a compiler-file resolution mismatch and three wrapper-only defects below. Correct those in
new versioned preparation records, preserve current candidate/history and re-review the narrow delta.
Do not change accepted source bodies, qualified tooling or test/SDK policy to force a pass.

Current reviewed profile SHA-256:
`806ce07d050835dbcac7e4ce33ffb2b0006c5c8e9937e4321f3af6f569a4dc5c`.
Source baseline: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
Observed metadata HEAD: `da66f5d01ba594993974ddd26723e8c300c0a454`.
No Full context compilation or example/test execution was performed by this reviewer.

## FULL-R1 — resolve file descriptors in the selected actual project

**Paths:** `capture.py:17–25,102–103`; unchanged pilot `../capture.py:90–97`; qualified
`../../checker-binding/bind.py:154–172`.

`kernel(project)` correctly rebinds the pilot observer's `PROJECT` for source/reference membership.
Its reused `compiler()` then loads the qualified compiler-file parser afresh. That parser resolves
relative filenames using **its own DocCheck `PROJECT.parent`**, not the selected Full/test project.
The Full adapter does not supply that parser context. This was harmless for the original pilot's
absolute descriptors; ordinary primary test `obj`/Source Link/embedded input paths introduce a different
context. The selected SDK Source Link target derives its output from `IntermediateOutputPath` without
requiring an absolute filename. The Full contract must handle the actual project-relative case.

A pure metadata probe, without capture/build execution, demonstrates the mismatch:

```text
/sourcelink:obj/Release/net10.0/Yubico.YubiKit.Core.UnitTests.sourcelink.json
current resolver: validation/checker/src/obj/Release/net10.0/...
required base:    src/Core/tests/Yubico.YubiKit.Core.UnitTests/obj/Release/net10.0/...
```

The current membership guard fails closed; this is not silent admission of an unrelated file. It is
nevertheless a preparation defect before the ordinary-project contexts. Add a small **Full-owned**
explicit selected-project binding around the proven descriptor algorithm, preserving comma/quote
semantics and raw Csc text. Handle both relative and absolute descriptors. Do not rewrite captured
arguments to fabricate evidence, modify the qualified pilot/binder, or build a new general resolver.
Confirm with the relative Source Link case and a relative quoted comma-containing `/embed:` filename.
No actual Csc capture is authorized by this review.

## FULL-R2 — keep FULL-211's resource declaration inside its method

**Paths:** `test-contexts.py:64–68`,
`injections/Core.UnitTests/FULL-211.cs:18–25`, and alias `FULL-238`.

The broad `using [^\n]+;` import matcher treats
`using var fakeConnection = new FakeSmartCardConnection();` as a namespace import. The generated file
puts that resource-owning declaration before its file-scoped namespace, while the subsequent method
uses the local. It is not a valid import; the result also breaks the original ownership/local scope.
The existing byte reconstruction check accepts the erroneous split because its expected prefix was
created by the same matcher.

Only move genuine using directives into the import prefix. Keep the **entire original resource
declaration and following body** together inside `CompileOnly`. The existing narrower consumer import
splitter illustrates the required distinction; no SDK/body rewrite is needed. Refresh the generated
source/layout identities and FULL-238 same-context alias metadata while preserving original raw and
decoded body hashes. Add a pure static distinction for `using var` versus a namespace import.

## FULL-R3 — correct two generated method/import contexts

- `injections/Management.IntegrationTests/FULL-244.cs:22–37`: the wrapper adds
  `public async Task CompileOnly(...)` around a synchronous property/capability fragment containing no
  await. This produces CS1998 under the unchanged warnings-as-errors policy. Use a synchronous typed
  wrapper, not a dummy await, warning suppression or body change.
- `injections/Ctap2.IntegrationTests/FULL-027.cs:34`: the original body uses `Encoding.UTF8`, but its
  wrapper imports omit `System.Text`. The actual existing project has no global System.Text using;
  its current generated global-usings file confirms that absence. Add that real framework import to
  this context. Do not replace the original call or invent a helper.

These are preflight wrapper defects, not independently established product API defects. Actual
compiler diagnostics may still expose other documentation issues; return those literal diagnostics
to the source owner rather than editing accepted bodies in the host.

## Accepted source, population and qualification findings

- All **1,713 fixed current profile rows** match their original bytes/size; its **1,330 known compile
  sources** and **25-project closure** are explicit. The accepted product map matches all **361 paths**.
  **No additional changed source-document paths or unexplained full-document drift were found after
  this candidate was prepared.** No blind repin is requested.
- Source transitions are explicit: the three comment-only toolchain inputs use the accepted external
  receipt/patch; the sampling discovery-guide transition preserves all 415 main bodies; the late B3
  IIOKit comment has recovered predecessor/map and accepted receipt; the late B4 root-CLAUDE correction
  preserves all 17 root fence bodies and updates coordinates. Current map SHA-256 is
  `d33ac76a90640420d868b77dcbfa285e9e1c84ac1c8ab10b644e314fcbb0d23f`.
  Those records explain known changes rather than granting automatic acceptance to future drift.
- **703 authority records** are accounted: 41 original pilot records, 415 additional main records and
  247 external records. **302 additional prepared occurrences** represent **300 distinct source/context
  compilations**, 244 consumer plus 56 real-test-context bodies. The two aliases retain same-body,
  same-real-context provenance; they do not merge different ownership contracts.
- Raw source/fence/XML regions, byte coordinates, full-document hashes and author/overlay/review
  identities are separate from decoded bodies and generated wrappers. The verifier reproduced the
  prepared source files exactly. Current After regions in FULL-047/FULL-165 are selected while historical
  Before subranges stay explicit. Secret-logging/allocation antipatterns are not assigned current
  recommended-body compiler credit. The obsolete external invalid filter remains historical, separate
  from its repaired current real type/test body.
- I read all **34** proposed qualification bodies and their individual reasons. Accept these as
  explicit **non-compiled** partial sketches, comparative type/member alternatives, rejected patterns,
  XML/generated excerpts, internal contributor or fixture-dependent scaffolds for this campaign scope.
  They remain visible and receive no compiler/runtime credit. In particular, omitted CMAC/parser/reset/
  reader behavior must not be manufactured; the RIGHT LibraryImport contributor declaration remains
  qualified rather than enabling unsafe code or adding IVT. This is not a claim that every standalone
  declaration within a comparative sketch was separately compiled. No qualification is closed by a
  fabricated SDK method or no-op implementation.
- The remaining external nonparticipants explicitly identify abbreviated implementation sketches,
  actual JSON and XML-embedded shell rather than disguising active SDK C# as historical material.
  Complete current consumer/test bodies are assigned genuine contexts; the defects above affect their
  preparation, not that accounting. Final compiler coverage cannot be marked complete before actual
  accepted context results and correction reconciliation.

## Genuine contexts, bounded capture and restoration

The Full consumer is a non-packable library with no entry point. Its real SDK/Cli.Shared/PivTool/
ManagementTool and framework/package references supply the actual public/application contexts. Serilog
audit-only package versions and original nuspec selection evidence are explicit; shipping package policy
is unchanged. Restore/assets and actual Csc references must still prove the build's selected context.
The application RP-validation adapter delegates to a real supplied callback rather than impersonating
an SDK API or providing dummy security behavior.

The nine test contexts are actual existing assemblies/projects with their own xUnit/fixtures/imports
and existing IVT. Compile injection is selected by exact project path; no assembly-name impersonation,
new grant or product project edit is used. Known xUnit v3 extra source/task inputs are explicit and
profile-bound, and unknown source membership fails. Existing test policy is recorded as such; analyzer
participation and zero warnings remain required. Mutable cleanup parameters preserve real writable
buffer ownership instead of fabricating read-only cleanup.

The hook retains the proven producer-before/late-CoreCompile pattern and original observer semantics,
subject to FULL-R1's explicit parser context fix. Actual sources/references/analyzers/config/file inputs,
nonempty Csc arguments, exact membership, unchanged before inputs and actual compiler/final outputs
must be established by the assigned build. The qualified logger's original stream/retention/quiescence
checks remain required; source/profile equality alone is not compiler success.

Only the declared ordinary primary tool/SDK/test output closure and dedicated Full host jobs are
approved. No other-worktree, frozen/archive or `/tmp` output is selected. The accepted pilot's **29
mutable files / 4,015,574 bytes** are retained externally, and all pilot record/copy identities checked
by the verifier still match. Each new context must immediately preserve its needed original mutable
outputs/references/generated inputs; later rebuilt paths are not equivalent historical evidence.

**No injected test or example DLL may run.** Injection environment is local to the wrapper process and
does not edit project files. After all compile evidence is preserved, the coordinator must perform
the final normal **uninjected clean solution build before any SDK test lane**. Exiting the wrapper
removes the injection environment but does not restore already built test assemblies. A failed/interrupted
context also requires that restoration before tests. This is a mandatory execution-order gate, not a
runtime cleanup result established here.

## Fresh verification and exact conditional commands

Inspected entry points/imports before running the read-only verifier:

```bash
python3 -B docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full/verify.py
```

Exit **0**: 703 authority accounting, 302 prepared occurrences, 300 exact generated files, real projects,
root imports, current source regions, wrapper reconstruction and retained pilot proofs passed. The
static verifier does not parse C# semantics or bind the loaded parser's project base, which explains
why the defects above remain despite that pass. Separate byte/JSON/source checks verified all fixed
profile rows, accepted map, transition receipts and the specific generated defects. No creator,
capture/compile operation or C# runtime was invoked.

**Hold Full context execution pending the narrow corrections, new profile identity and re-review.**
After that, exclusive release and current build skill, retain the intended first representative command:

```bash
F=docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full
P=<accepted-corrected-profile-sha256>
PROFILE=<accepted-corrected-profile-path>
python3 -B "$F/verify.py"
python3 -B "$F/run.py" --context consumer --label examples-full-consumer-first \
  --profile "$PROFILE" --profile-sha256 "$P"
```

The wrapper uses canonical `dotnet toolchain.cs -- build --project <exact context stem> --clean`, never
a test target. Inspect/accept consumer proof first, then serialize the nine existing test contexts from
`compiled-contexts.json` with fresh labels. Require each successful complete zero-warning original
receipt, actual capture, unchanged source/profile and immediate output preservation before acceptance.
Preserve first failures. Do not rerun the accepted pilot or rebuild old contexts for bookkeeping.
The separate final DocCheck transition is accepted in `checker-binding/final-inputs/review.md` and need
not wait for these wrapper corrections.

## Reviewed identities and limits

| File, relative to `validation/examples/full/` | SHA-256 |
| --- | --- |
| `reviewed-inputs-current.json` | `806ce07d050835dbcac7e4ce33ffb2b0006c5c8e9937e4321f3af6f569a4dc5c` |
| `manifest-current.json` | `bc34edaf8dcb153765aba87ebb786695f83010042d9df3d2bcb1af0923eef545` |
| `compiled-contexts.json` | `c1bbc9256087c9ed5a44c5a4d03d9966e3799fd7ab66800db5c8e40d52b6e05f` |
| `qualification-decisions.json` | `2dcc234df92710ddd9c5459e48f299cae3b45a28975315d2dc36c59653ae25f3` |
| `capture.py` | `acaf3f438cf103388da1ae600502a139a1ca6f729f054fa0df462d18ebf0d4bf` |
| `run.py` | `22f9649d358b418bbba8841cde5f4923cdf2a3d1f9b3ba1631c23b4527a9eca6` |
| `verify.py` | `4934e09597f442683e45daeec8e23bdb734c67f1b4854c68320d5744c74e6833` |

Only this review and the separately assigned final-input review are written. No build/test, actual
capture/replay, compiled checker, native/device/crypto operation, formatter, source/root product edit,
Git mutation or subagent ran. All prior reviews and original preparation remain intact. Complete the
remaining authorized Core Pass-1 gates/publication and deferred handoff, then stop; physical PIV and
later Windows qualification remain separately scoped.
