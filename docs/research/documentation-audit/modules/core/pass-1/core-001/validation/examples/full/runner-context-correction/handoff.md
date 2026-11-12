# FULL-061: genuine xUnit v3 context, unchanged body

**Ready for focused review; no new compilation claimed.** Management.IntegrationTests first failed
with exactly three CS0103 diagnostics, all on `TestContext` in FULL-061. The sequence stopped; the five
later integration contexts have not run. This worker ran only static Python preparation/verification.

## Raw authority, enclosing policy and classification

`actual-diagnostics.json` preserves the complete deduplicated diagnostics from retained 052456 build
log; `diagnostic-triage.json` classifies each as **generated runner-context mismatch**.

- Raw source: `CLAUDE.md:533–547`, fence 14. Raw SHA-256:
  `4b99ea8a21a5d33d427bd6a0b07568d057177206af24804ab62c50e6d99e123b`.
  The exact authority entry in `editing/SHARED/revision-2/example-bodies.json` retains the same body;
  later accepted prose transitions do not change its tokens. The manifest's document/authority/review
  fields and original raw coordinates remain unchanged.
- The body has `[Fact]` and three `TestContext.Current.CancellationToken` uses. Root policy at line 121
  explicitly names the xUnit v3 token contract. Current xUnit documentation and installed v3 compile
  assembly XML declare `Xunit.TestContext`, `Current` and `CancellationToken`.
- The enclosing heading describes an **integration task**, not a requirement to use this repository's
  v2 Management.IntegrationTests project. Lines 549–551 retain the authorized dedicated SmartCard key,
  configured SCP03 defaults and parameter/session lifetime prerequisites.
- **No true accepted-body or documentation-policy defect was identified in this scope.** The missing
  symbol is explained by wrong runner ownership. Hardware semantics are not converted to ordinary unit
  testing: the UnitTests-named project is only a real v3 **compile assembly** for this preparation.
  Never execute this injected test. Any later genuine body/policy error still returns to its owner.

Exact source ranges, authority entry, project/reference evidence and real v3 symbols are hash-bound in
`ownership-evidence.json`. No TestContext shim, nullable/token rewrite, NoWarn, copied SDK helper,
runner package modification, project edit or hardware execution was introduced.

## Precise reassignment and closure

**FULL-061: Management.IntegrationTests → Management.UnitTests.**

The existing `src/Management/tests/Yubico.YubiKit.Management.UnitTests/` project already references
`xunit.v3` 3.0.0, enables its actual MTP runner, and directly references the real Management project,
which references Core. Its restored assets select `xunit.v3.core.dll` with the required TestContext API.
Core.UnitTests was rejected: its real project closure has no Management reference. No new reference was added.

- **Contexts: 10 → 11** (consumer + ten existing test projects).
- **Projects in union closure: 25 → 26**; the only new project is existing Management.UnitTests.
  Its own closure is Management.UnitTests + Management + Core.
- Management integration injections **35 → 34**; new Management unit injections **1**.
- Selected sources remain **300 total / 56 test**; body occurrences and aliases are unchanged.
- New selected file: `injections/Management.UnitTests/FULL-061.cs`. Only the two wrapper-added
  `Tests.Shared` imports were removed: this body uses no shared fixture and the new project correctly
  has no Tests.Shared dependency. The whole accepted method, attribute, comments, tokens, directives
  and non-prefix remainder remain exact.
- Original `injections/Management.IntegrationTests/FULL-061.cs` is retained byte-exact as **nonselected
  predecessor evidence**. It is absent from the new injection plan and known compile-source membership;
  no wildcard adds it back. All 300 predecessor source files remain unchanged.

`compiled-contexts-runner-fixed.json` and `manifest-runner-fixed.json` are versioned successors, not
rewrites of previous proofs. `membership-delta.json` and `project-closure-delta.json` enumerate the exact
path/context/project/source differences. The new context reuses the already pinned xUnit v3 reporter,
entrypoint and self-registration producers under its exact owning obj directory. Only its explicit
name was added to the existing finite v3 capture guards. The seven Test.Sdk integration admissions
remain unchanged; the successor admission descriptor explicitly excludes the new v3 context.

## Whole test-population scan and preserved checkpoints

All **56 selected test preparations**, not just the failed file, were classified against their actual
restored runner libraries. Only FULL-011 and FULL-061 mention TestContext:

| Body | Before | After |
| --- | --- | --- |
| FULL-011 | Ctap2.UnitTests, v3: correct | Unchanged, correct |
| FULL-061 | Management.IntegrationTests, v2: incompatible | Management.UnitTests, v3: correct |
| Other 54 preparations | No TestContext requirement | Unchanged |

`test-runner-scan-before.json` / `test-runner-scan-after.json` retain every classification. The finite
ownership oracle failed before the change and passes afterward; all 300 current selected bodies also
pass exact reconstruction and the prior fault-family checks. These are static checks, not a compiler pass.

`preservation.json` binds **60 failed Management attempt/command identities**, nine preparation
predecessors, profile `fa8…`, every original source and the four accepted contexts with **78 retained
files**. Their records/profiles/outputs are not rebound or rerun:

| Accepted context | Original profile | Accepted-compilation SHA-256 |
| --- | --- | --- |
| Consumer-fourth | `3e40…` | `e044ba23cd4a0a55caa6cf0679716c6aba98c15f82adc292e05361bdf186c357` |
| Core.UnitTests | `3e40…` | `9ff6235c1c8c077d7767f14f2a53f125b9b591a074eca42ace60725652d77e8a` |
| Ctap2.UnitTests | `3e40…` | `097d83207ee0bd098cd42fb862b2f91fa318f955ff762e24d1f8880c08d637c8` |
| Ctap2.IntegrationTests second | `fa8…` | `b71ca5bf04c566fc4d556b9ff935489bd194094557124777d7b4d207da774a58` |

The accepted CTAP integration proof retains 19 local files and actual GenerateProgramFile=true / pinned
path observations. The failed Management attempt remains failed. Qualified DocCheck/pilot/logger/parser,
root/product/test project files and package versions remain unchanged.

## Fresh candidate profile

`../reviewed-inputs-runner-fixed.json`

**SHA-256:** `d75026297c5f13f7c611f2398b03da98755d8130265e344c2208d2dca8959f90`

Derived from unchanged profile
`fa8db15afb99ca26f5e7912e2754981fb8a4cffba59e76df5a76136080652aed`:
**five changed preparation rows**, **three versioned replacements** (manifest, plan, admission descriptor),
**19 explicit additions**, **1,735 predecessor rows unchanged**, **1,762 fixed rows**.
The additions are eight unchanged existing Management unit project files, the new injection, three
historical direct proof inputs, five verifier/evidence inputs and actual v3 compile assembly/XML identities.
Known compile paths **1,331 → 1,337**: remove the old FULL-061 injection path; add its new path and the
six existing Management unit source files. The old source stays a fixed historical input, not a compile
participant. All 361 accepted product paths/map and all other original compile paths remain unchanged.

`profile-delta.json` lists every row. `preparation.delta.json` contains the exact five-file preparation
diff. `verification.json` records final profile checks across all eleven actual contexts.

## Coordinator commands after focused acceptance

Load the build skill. Compile, do not run tests, and accept each actual proof before the next command:

```bash
F=docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full
P=d75026297c5f13f7c611f2398b03da98755d8130265e344c2208d2dca8959f90
python3 -B "$F/verify.py"
python3 -B "$F/run.py" --context Management.UnitTests --label examples-full-management-unit-first \
  --profile "$F/reviewed-inputs-runner-fixed.json" --profile-sha256 "$P"
# After accepting that actual zero-warning compiler/output/preservation proof:
python3 -B "$F/run.py" --context Management.IntegrationTests --label examples-full-management-second \
  --profile "$F/reviewed-inputs-runner-fixed.json" --profile-sha256 "$P"
```

Then continue the five unrun contexts with the same profile, one at a time:

| `--context` | Fresh `--label` |
| --- | --- |
| SecurityDomain.IntegrationTests | examples-full-security-domain-first |
| Piv.IntegrationTests | examples-full-piv-first |
| Core.IntegrationTests | examples-full-core-integration-first |
| WebAuthn.IntegrationTests | examples-full-webauthn-first |
| Oath.IntegrationTests | examples-full-oath-first |

Stop and retain any first failure. Never execute injected assemblies, particularly this hardware body
in the unit-named project. The final ordinary uninjected clean solution build must restore normal test
outputs before any SDK test lane. No SDK build, capture phase/replay, SDK test, native/device/provider
execution, formatter, Git mutation or subagent ran here. Primary remains reserved. Finish Core Pass 1
and its deferred-finding handoff, then stop.
