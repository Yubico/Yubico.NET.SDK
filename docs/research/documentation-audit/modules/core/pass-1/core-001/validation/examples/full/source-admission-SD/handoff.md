# Final SD source admission

**Ready for quick confirmation and coordinator compilation.** The four Full adapter/wrapper fixes are
already independently PASS at review SHA-256
`91109833a77574e5ed28e21e95dbe4c9d718769b8f27befd90d8efbd71769efb`.
This checkpoint admits only the later reviewed SD Related Modules namespace prose correction.

`transition.json` verifies exactly the two external input changes found by that review:

| Input | Previous SHA-256 | Admitted SHA-256 |
| --- | --- | --- |
| `src/SecurityDomain/CLAUDE.md` | `76408f4a4ce1995712731ca7201b390836b9d4ded236268c619424a460d3915a` | `dd24dbcc43606abd77888fecdf9368b0c103b74b860fba1c46d3c10ccdab9022` |
| Current-products map | `d33ac76a90640420d868b77dcbfa285e9e1c84ac1c8ab10b644e314fcbb0d23f` | `45ca220e401ba48082e14f138d703df0e29a969dd7ae7d5edc89d2ad805075c2` |

The exact authority is `editing/integration/accepted-sampling-SD.json`, with patch
`b1cb5c00f569c94499060cd1539edf2bafa67057763126e3786c3f0e83c8ab65`.
The prior coordinator map was reconstructed and checked against its exact old hash before retention.
The retained original SD document matches its old hash. All three SD example records retain their exact
raw/decoded bodies, classification, source review, wrapper and compilation-context fields; only current
document identity/coordinates are rebound. All other authority records remain unchanged.

The original `cc9da7…` profile, manifest and review remain exact. Snapshots preserve both verifier
predecessors and the preparation document before this checkpoint. The two verifiers change only their
literal manifest filename to select the admitted manifest consistently. No wrapper, hook, resolver,
parser, SDK project, qualified tool, root input or workspace changes. The accepted DocCheck `db8347…`
profile/seal/token/API lane is separate and needs no rebuild.

## Admitted profile

`../reviewed-inputs-admitted.json`

**SHA-256:** `8c385b8dae912d846dcd9463c08bf0bbb765ba6a00ffa9f657962a7971f0b81d`

`profile-delta.json` distinguishes the **two accepted source/map input changes** from the two local
verifier filename selectors and one manifest replacement. **1,710 original rows** remain exact;
the fixed count stays 1,715. Compile-source membership, all 25 declared projects and all 703 authority
records / 300 additional distinct compilation contexts remain unchanged. `verification.json` records
the fresh full static guard/profile pass. No managed build/capture/test/example/native/device execution,
Git mutation or subagent occurred.

## Exact caller sequence after quick confirmation

Load the current build skill and keep this compile cycle exclusive in the reserved primary checkout.
Verify first, run/accept the consumer proof, then serialize the nine actual test-project contexts:

```bash
F=docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full
PROFILE="$F/reviewed-inputs-admitted.json"
P=8c385b8dae912d846dcd9463c08bf0bbb765ba6a00ffa9f657962a7971f0b81d
python3 -B "$F/verify.py"
python3 -B "$F/run.py" --context consumer --label examples-full-consumer-first --profile "$PROFILE" --profile-sha256 "$P"
python3 -B "$F/run.py" --context Core.UnitTests --label examples-full-core-unit-first --profile "$PROFILE" --profile-sha256 "$P"
python3 -B "$F/run.py" --context Ctap2.UnitTests --label examples-full-ctap-unit-first --profile "$PROFILE" --profile-sha256 "$P"
python3 -B "$F/run.py" --context Ctap2.IntegrationTests --label examples-full-ctap-integration-first --profile "$PROFILE" --profile-sha256 "$P"
python3 -B "$F/run.py" --context Management.IntegrationTests --label examples-full-management-first --profile "$PROFILE" --profile-sha256 "$P"
python3 -B "$F/run.py" --context SecurityDomain.IntegrationTests --label examples-full-security-domain-first --profile "$PROFILE" --profile-sha256 "$P"
python3 -B "$F/run.py" --context Piv.IntegrationTests --label examples-full-piv-first --profile "$PROFILE" --profile-sha256 "$P"
python3 -B "$F/run.py" --context Core.IntegrationTests --label examples-full-core-integration-first --profile "$PROFILE" --profile-sha256 "$P"
python3 -B "$F/run.py" --context WebAuthn.IntegrationTests --label examples-full-webauthn-first --profile "$PROFILE" --profile-sha256 "$P"
python3 -B "$F/run.py" --context Oath.IntegrationTests --label examples-full-oath-first --profile "$PROFILE" --profile-sha256 "$P"
```

Execute each command only after the previous context passes its actual complete zero-warning receipt,
Csc/input/output guards and immediate preservation. Stop and retain any first failure. The wrapper uses
only canonical `dotnet toolchain.cs -- build --project <exact context stem> --clean`, never a test target.
No injected test or example DLL may execute. After all proofs are retained, the final ordinary
uninjected clean solution build restores test outputs before any SDK test lane. The original 17-body
pilot remains unchanged and is not rerun.
