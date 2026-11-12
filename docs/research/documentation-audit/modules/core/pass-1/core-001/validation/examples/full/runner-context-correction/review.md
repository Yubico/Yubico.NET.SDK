# Independent Sol confirmation: FULL-061 runner ownership

## PASS — Management.UnitTests compile context ready

Accept `reviewed-inputs-runner-fixed.json` at SHA-256
`d75026297c5f13f7c611f2398b03da98755d8130265e344c2208d2dca8959f90` for serialized coordinator
compilation of the new real Management.UnitTests context, then the reduced Management integration
retry and five remaining integration contexts. No further correction or new framework is required by
this bounded review. Actual compiler/output acceptance remains pending each build.

**This is compilation only. Never execute the injected hardware example in the unit-named assembly.**
Its integration-task semantics, dedicated-key/SCP03 prerequisites and resource lifetimes are unchanged.
The final normal uninjected clean solution build must restore ordinary test outputs before any SDK
test lane, including after failed/interrupted injection attempts.

Source baseline remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; observed metadata HEAD was
`da66f5d01ba594993974ddd26723e8c300c0a454`. Keep the reserved primary stable and prior proofs exact.

## Source authority and genuine runner/reference context

- The raw root-document example and its exact SHARED authority entry have SHA-256
  `4b99ea8a21a5d33d427bd6a0b07568d057177206af24804ab62c50e6d99e123b`.
  Its `[Fact]` method uses `TestContext.Current.CancellationToken` three times. Root policy explicitly
  identifies that as the xUnit v3 token contract. Current xUnit documentation fetched through Context7
  supports the v3 API; the actual installed v3 compile assembly/XML and restored assets bind this
  repository's selected version, rather than relying on current online documentation alone.
- Independently reparsed the complete retained Management build log. All **three unique emitted
  diagnostics** match the retained path/location/code/message identities: CS0103 for TestContext in
  FULL-061. The original chosen integration project uses v2; this is a generated runner-context
  mismatch. No accepted-body or documentation-policy defect is identified in this bounded scope.
  Later compiler errors remain possible and still belong to the relevant editor/coordinator.
- The existing `Management.UnitTests.csproj` already references `xunit.v3`, enables its real MTP runner,
  and directly references Management, which references Core. Its actual restored assets select
  `xunit.v3.extensibility.core/3.0.0` and `xunit.v3.core.dll`; the pinned XML declares TestContext,
  Current and CancellationToken. **Its exact closure is Management.UnitTests + Management + Core.**
- The existing Core.UnitTests closure lacks Management. No Core.UnitTests reference was invented,
  and no project/runner-package/root-policy edit, TestContext shim, cancellation-token substitution,
  access grant, copied SDK helper or warning suppression was added.
- The enclosing root heading describes an integration task, not a required repository project name.
  Using a genuine v3 compile assembly does not reclassify this hardware body as an executable unit
  test or waive its authorized SmartCard/SCP prerequisites. No test execution credit is assigned.

## Exact source/membership change

- FULL-061 moves **Management.IntegrationTests → Management.UnitTests**. Independently reproduced
  the exact source diff: only the two wrapper-added `Tests.Shared` namespace imports disappear.
  The new real unit project has no shared-fixture dependency and this body uses none.
- The accepted method/attribute/comments/body tokens, original directives and every non-prefix byte
  remain exact. The v2 injection remains byte-exact at its old path as **nonselected evidence**. The
  explicit plan and known compile-source list exclude it; no wildcard reintroduces it.
- All **300 predecessor materialized sources** are preserved unchanged. The selected set still has
  **300 sources / 56 test preparations**: exactly one selected source path is replaced, not duplicated.
  Management integration has 34 injections instead of 35; the new unit context has one.
- Audit contexts increase **10 → 11** and union closure **25 → 26** only by adding the already existing
  Management.UnitTests project. This creates no new repository test project. The explicit compile-source
  set increases **1,331 → 1,337**: remove the old injection, add its new path and six existing real
  Management unit source files. Other compile paths and all 361 accepted product paths/map remain exact.
- All 56 selected test preparations were checked against actual restored runner libraries. FULL-011
  remains correctly in Ctap2.UnitTests/v3; FULL-061 now has real v3 ownership; the other 54 have no
  TestContext requirement. The recorded sole predecessor mismatch is FULL-061.
- Full-local source admission extends the existing finite v3 reporter/entrypoint/self-registration
  guards only by the explicit Management.UnitTests name and its actual owning obj directory. The
  seven exact Test.Sdk integration admissions are unchanged. The versioned admission descriptor adds
  Management.UnitTests to **exclusions**, matching its actual MTP assets without Microsoft.NET.Test.Sdk.
  No arbitrary generated/package-source allowance was added.

## Fresh read-only checks and immutable proof

Inspected the verifier's imports/entry points, then ran:

```bash
python3 -B docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full/verify.py
```

Exit **0**: exact body/authority/wrapper reconstruction, all 300 finite fault-family guards, all 56
runner classifications, explicit closure/membership, new v3 producer admission, unchanged seven
integration package admissions, original pilot proof and recorded retention identities pass.

Separate profile/JSON/diff checks confirmed every **1,762 fixed input row** matches its length/hash.
There are five changed preparation paths and nineteen physically new paths, with no physically removed
fixed path. The ledger's **1,735 unchanged active roles** exclude the three old manifest/plan/admission
selectors retained as history; raw path comparison has **1,738 identical rows**. This preserved-history
role distinction is not hidden input drift. All five preparation diffs were reproduced independently.

The **four accepted checkpoints** retain their actual original profiles and accepted-compilation
identities; all **78 retained files** match. Consumer-fourth and the two earlier unit contexts retain
`3e40cb44…`; accepted Ctap2 integration-second retains `fa8db15a…`. Do not rerun or relabel them merely
because this new profile adds a context. The failed Management attempt's **60 record identities**,
all predecessor source/preparation snapshots and profile `fa8db15a…` remain exact and failed; no
accepted compilation is manufactured for the former v2 placement. Qualified checker/pilot/parser/
logger and product/test project files remain unchanged.

## Exact coordinator continuation

With exclusive reserved-primary ownership and the current build skill loaded:

```bash
F=docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full
P=d75026297c5f13f7c611f2398b03da98755d8130265e344c2208d2dca8959f90
python3 -B "$F/run.py" --context Management.UnitTests --label examples-full-management-unit-first \
  --profile "$F/reviewed-inputs-runner-fixed.json" --profile-sha256 "$P"
# Only after accepting that actual complete zero-warning compilation/output/preservation proof:
python3 -B "$F/run.py" --context Management.IntegrationTests --label examples-full-management-second \
  --profile "$F/reviewed-inputs-runner-fixed.json" --profile-sha256 "$P"
```

Then serialize the five remaining SecurityDomain, Piv, Core, WebAuthn and Oath integration contexts
with this same profile and their fresh handoff labels. Require each actual successful original receipt,
complete streams/retention/quiescence, zero-warning Csc membership/output guards and immediate retained
output proof before continuing. Preserve any first failure and return genuine source errors literally.
No example or injected test assembly may execute. The uninjected final clean solution build remains
mandatory before tests; leaving the wrapper removes its environment, not the already built injection.

## Reviewed identities and limits

Paths below are relative to `validation/examples/full/`.

| Record | SHA-256 |
| --- | --- |
| `manifest-runner-fixed.json` | `a0ef9c3975b2ed3c89022185688f38a50508af7378c281eecbe70ddb58c5b7ec` |
| `compiled-contexts-runner-fixed.json` | `bde980ff7ae7d73f3407f769469c0306b12857a8a586c61b237af1dc0da06a67` |
| `package-entrypoint-admission-runner-fixed.json` | `6b87d219e42435fef25aca465c67b9b32c5a96ac0e2de398da3768fd95f37b08` |
| `runner-context-correction/ownership-evidence.json` | `15770c3c00e234b61370ecd0b376d57dd1259b0f67239d24982423dcb0ab81ad` |
| `runner-context-correction/membership-delta.json` | `612085f10287f259d7046f334cf296ae0a5115f265008142aa6065009b4adaba` |
| `runner-context-correction/preservation.json` | `1cc1664f8caa02c0ed12622d77c9a0b7059995a8df5208ac421cde69e7631b84` |
| `runner-context-correction/verification.json` | `57fd0f1dfec22a6f3b099495b12a9a693c42649ebc8ef2325796a9750cf4af34` |

Only this review is written. No SDK build/test, actual capture/replay, compiled checker/runtime,
native/device/provider operation, formatter, product edit, Git mutation or subagent ran. Prior reviews
and qualified proofs remain intact. Finish authorized Core Pass-1 gates/publication and deferred
handoff, then stop; platform/physical lanes retain their existing bounds.
