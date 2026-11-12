# SD: three genuine generated helper imports

**Ready for focused review, not a new compiler pass.** SecurityDomain.IntegrationTests first failed
with five CS1061 diagnostics. The sequence stopped; Piv/Core/WebAuthn/Oath integration contexts remain unrun.

## Exact root cause and fix

The full retained 053848 build log has four missing `WithSecurityDomainSessionAsync` diagnostics
(FULL-302 once, FULL-303 twice, FULL-306 once) and one missing `ResetSecurityDomainAsync` (FULL-303).
`actual-diagnostics.json` retains each diagnostic; `diagnostic-triage.json` maps it to the exact raw
document/body/hash/generated line and actual helper declaration.

Both methods already exist publicly in the owning project:

`src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs`

- Line **20**: `namespace Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;`
- Lines **31–36**: real `WithSecurityDomainSessionAsync` signature.
- Line **77**: real `ResetSecurityDomainAsync` signature.

The helper source is already fixed in predecessor profile `d750…` and listed in the actual failed
before-Compile items. `src/SecurityDomain/tests/CLAUDE.md:80–84` and `tests/README.md:51–54` explicitly
require that namespace. The bodies are correctly assigned to the real xUnit-v2 SD integration project.

Add exactly this one generated prefix directive to **FULL-302 / FULL-303 / FULL-306**:

```csharp
using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;
```

`generated-imports.delta.json` is the complete three-source diff. The generator adds the same exact
namespace only for its named SD integration context. Original accepted body bytes/tokens/directives,
directive order and all non-prefix bytes remain exact. **All five observed diagnostics are generated
context defects; no true body/API defect was identified.** Later compiler diagnostics remain possible
and must return to the proper editor rather than be hidden. No SDK/test-helper/body edit, fake helper,
NoWarn, runner/package/capture/driver change or broad namespace allowance was made.

## All 56 test preparations checked, including the four unrun contexts

`helper-evidence.json` binds six actual owning-project helper/base providers and their declarations;
the before/after scan records classify every selected test source. This is a finite import-family
check, not a general C# parser or binding framework.

| Context / body | Actual provider / result |
| --- | --- |
| SD FULL-302/303/306 | Required exact namespace was missing; corrected as above. |
| SD EXT-231 | Original accepted body already contains the correct using; unchanged. |
| Piv FULL-375 | Calls public Piv session APIs; no module-local test-extension call requiring an extra import. Existing Piv namespace retained. |
| Core FULL-212/239 | Real `IntegrationTestBase` is in `Core.IntegrationTests`, already explicitly imported. No extra local test-extension call. |
| WebAuthn FULL-037 | Real internal `WebAuthnTestHelpers` is in `WebAuthn.IntegrationTests`, already imported in its genuine owning project. FULL-032 needs no such helper import. |
| Oath EXT-204 / EXT-234 alias | Defines the original `WithOathAsync` extension; does not call module-local `WithOathSessionAsync`. No unused Oath test-extension import added. |
| Other selected preparations | Actual helper requirements/imports checked; no additional mismatch in this family. |

The oracle failed before correction for exactly these three IDs, then passed all 56. Negative cases
reject removal, the ancestor `SecurityDomain.IntegrationTests` namespace and the wrong Oath helper
namespace as substitutes. Current duplicate/nonexistent repository namespace guards, finite prior
fault-family checks, all-56 runner compatibility, all-300 source reconstruction, authority checks,
bounded descriptor/resource guards and preserved-pilot checks pass.

Earlier frozen correction oracles/records remain byte-exact historical evidence; they are not rewritten
or run against a successor they did not bind. The current verifier proves exactly the three-line prefix
delta and rechecks the current source/import/runner populations. No generic requalification mechanism
or new execution path was introduced.

## Preserved failure and six accepted checkpoints

`preservation.json` binds **29 failed SD attempt/command identities**, the original profile, five
preparation predecessors, prior correction records and one exact **300-source text snapshot**.
The failed attempt remains failed. Six accepted checkpoints retain original profiles/records and
**113 local retained files**, covering **291 body occurrences**:

| Checkpoint | Original profile | Accepted-compilation SHA-256 |
| --- | --- | --- |
| Consumer-fourth | `3e40…` | `e044ba23cd4a0a55caa6cf0679716c6aba98c15f82adc292e05361bdf186c357` |
| Core.UnitTests | `3e40…` | `9ff6235c1c8c077d7767f14f2a53f125b9b591a074eca42ace60725652d77e8a` |
| Ctap2.UnitTests | `3e40…` | `097d83207ee0bd098cd42fb862b2f91fa318f955ff762e24d1f8880c08d637c8` |
| Ctap2.IntegrationTests second | `fa8…` | `b71ca5bf04c566fc4d556b9ff935489bd194094557124777d7b4d207da774a58` |
| Management.UnitTests | `d750…` | `90e664c4a4f15dfdd732280616a341a405c9e6d59e558e87fbd12d610f15d17f` |
| Management integration second | `d750…` | `768ce039c766a566c306cb4b64139ab9a1efa4ee0187d34d8677d06373140355` |

No accepted checkpoint was replayed, rebound or relabelled. Retained checkpoint bytes are not called
equivalent to later overwritten primary output paths. Qualified DocCheck/pilot/logger/parser inputs
and primary root/product/helper/project sources remain unchanged.

## Fresh bounded profile

`../reviewed-inputs-helper-imports-fixed.json`

**SHA-256:** `52234498f7d167899c296daadec96dd0a543d9dbbd3f72c72007d1983fc099e7`

From unchanged predecessor
`d75026297c5f13f7c611f2398b03da98755d8130265e344c2208d2dca8959f90`:
**six changed rows** (three generated prefixes, generator and two verifier selectors/wiring),
**one manifest replacement**, **eight explicit evidence/verifier inputs**, **1,755 predecessor rows
unchanged**, **1,770 fixed rows**. `profile-delta.json` enumerates each identity. No old profile was edited.

The **1,337 known compile paths**, **26 projects**, **11 contexts**, **300 selected sources / 56 test
preparations**, **361 accepted product paths/map**, aliases and all source/body authorities remain
unchanged. No context membership or project closure adjustment is needed.

## Coordinator retry after focused acceptance

Load the build skill and serialize the unchanged qualified/canonical capture flow:

```bash
F=docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full
P=52234498f7d167899c296daadec96dd0a543d9dbbd3f72c72007d1983fc099e7
python3 -B "$F/verify.py"
python3 -B "$F/run.py" --context SecurityDomain.IntegrationTests --label examples-full-security-domain-second \
  --profile "$F/reviewed-inputs-helper-imports-fixed.json" --profile-sha256 "$P"
```

After accepting its actual complete zero-warning Csc/output/preservation proof, continue one at a time
with the same profile arguments:

| `--context` | Fresh `--label` |
| --- | --- |
| Piv.IntegrationTests | examples-full-piv-first |
| Core.IntegrationTests | examples-full-core-integration-first |
| WebAuthn.IntegrationTests | examples-full-webauthn-first |
| Oath.IntegrationTests | examples-full-oath-first |

Stop and retain any first failure. Never execute injected assemblies or the destructive SD helpers.
The final ordinary uninjected clean solution build still precedes SDK tests. No SDK build/test,
capture phase/replay, native/device/provider execution, formatter, Git mutation or subagent occurred
here. Primary remains reserved. Finish Core Pass 1 and the deferred-finding handoff, then stop.
