# Independent Sol confirmation: owning-project SD helper imports

## PASS — Security Domain second attempt ready

Accept `reviewed-inputs-helper-imports-fixed.json` at SHA-256
`52234498f7d167899c296daadec96dd0a543d9dbbd3f72c72007d1983fc099e7` for the coordinator's serialized
SD retry. No further correction or new framework is required by this bounded review. Actual compiler/
output acceptance remains pending that build; static import checks do not promise later compiler success.

Source baseline remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; observed metadata HEAD was
`da66f5d01ba594993974ddd26723e8c300c0a454`. Keep the reserved primary stable and prior proofs exact.

## Five actual diagnostics, three exact generated directives

Read the retained diagnostics/triage, original namespace prerequisites and actual owning-project
`TestExtensions/TestStateExtensions.cs` declaration. Both public C#14 extension methods already exist
in `Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions`: WithSecurityDomainSessionAsync at
lines 31–36 and ResetSecurityDomainAsync at line 77. The helper is an existing fixed input and appears
in the actual failed before-Compile population. It is not a missing implementation or reference.

The original `src/SecurityDomain/tests/CLAUDE.md:80–84` and `tests/README.md:51–54` explicitly require
that exact namespace for these real xUnit-v2 integration fragments. The five retained CS1061 errors
are four unresolved WithSecurityDomainSessionAsync calls (FULL-302 once, FULL-303 twice, FULL-306 once)
and FULL-303's unresolved ResetSecurityDomainAsync. All match the omitted generated using. No true
body/API defect is identified in this emitted set; later diagnostics still belong to the relevant editor.

Independently reproduced all three exact generated source hashes/unified diffs. The only addition is:

```csharp
using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;
```

It occurs once in each FULL-302/FULL-303/FULL-306 prefix. Accepted directives/order and every non-prefix
byte are exact; **297 other selected sources are unchanged**. EXT-231's original accepted directive
already supplies this namespace and remains exact. The generator adds the import only for the named
SD integration context. No fake helper, body change, broad import allowance, NoWarn, runner/package/
project change or compiler observer alteration was introduced.

## Fresh read-only checks and whole-population bounds

After inspecting the pure verifier entry points/imports, ran:

```bash
python3 -B docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full/verify.py
```

Exit **0**. It reparses the complete retained log and compares all five diagnostic identities, exact
old generated lines and actual helper declarations; checks all **300** current sources and raw/body/
authority/wrapper evidence; and classifies all **56** test preparations against the six actual owning
helper/base providers and their real projects. No additional missing namespace was found in that finite
helper family. The checks reject removal, the ancestor SD namespace and the wrong Oath namespace as
substitutes. They do not claim universal C# binding.

The four unrun contexts are explicitly covered: Piv's body uses public session APIs; Core's real
IntegrationTestBase namespace is already imported; WebAuthn's real owning-project helper namespace is
already imported; Oath defines the original WithOathAsync extension and does not call the separate
WithOathSessionAsync helper. No unnecessary provider import or fake behavior was added to these bodies.
All 56 current runner classifications, package/descriptor/resource guards, duplicate/namespace checks
and original pilot retention checks also pass. Earlier correction oracles remain immutable historical
evidence rather than being rewritten to accept successor prefixes.

Independent profile/JSON/diff checks confirmed all **1,770 fixed input identities** match. Exactly six
existing paths change (three generated prefixes, generator and two verifier selections/wiring), with
current manifest selection changed and eight physically new evidence/verifier/manifest paths. The old
manifest remains retained: raw path comparison has **1,756 identical predecessor rows**, while the
ledger's **1,755 unchanged active roles** exclude that historical manifest. No hidden repin was found.
All three preparation diffs were reproduced. The **1,337 compile paths, 26 projects, 11 contexts,
361 accepted product paths/map**, aliases, accepted body authorities and qualified tools are unchanged.

All **six accepted checkpoints**, covering **291 body occurrences**, retain their original profiles,
records and **113 retained files**. No prior accepted context is rebound, relabelled or a bookkeeping
rebuild target. The **29 failed SD attempt/command identities**, predecessor profile `d7502629…`,
all predecessor snapshots and 300-source snapshot pass preservation checks. The failed SD attempt
remains failed. No later overwritten live binary is equated with an earlier retained checkpoint.

## Exact coordinator retry

With exclusive reserved-primary ownership and the current build skill loaded:

```bash
F=docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full
P=52234498f7d167899c296daadec96dd0a543d9dbbd3f72c72007d1983fc099e7
python3 -B "$F/run.py" --context SecurityDomain.IntegrationTests --label examples-full-security-domain-second \
  --profile "$F/reviewed-inputs-helper-imports-fixed.json" --profile-sha256 "$P"
```

Require its actual complete zero-warning original receipt, Csc membership/input/output guards and
immediate preservation proof before serial continuation to Piv, Core, WebAuthn and Oath integration
contexts with this profile and their fresh handoff labels. Preserve any first failure and return genuine
body errors with literal diagnostics. Never execute injected assemblies or destructive SD helpers.
The final normal uninjected clean solution build still restores ordinary outputs before SDK tests,
including after failed/interrupted injection attempts.

## Reviewed identities and limits

Paths below are relative to `validation/examples/full/`.

| Record | SHA-256 |
| --- | --- |
| `manifest-helper-imports-fixed.json` | `1d89859816e601d620eb6a9951b94241799c545648416e1e8a463774d34d94ff` |
| `test-contexts.py` | `64b94a9f5d290507e75a7e647733af3e0f7dffa08a8aec33767bf8ee535d6da8` |
| `helper-import-correction/helper-evidence.json` | `e9b5a614268ec5282470dd53c71114b858312905fc3f2e9c3e770366911d6e27` |
| `helper-import-correction/generated-imports.delta.json` | `818283f95bf630fd5a947a145561747867c671c3db2b9a0fd3952d0affe1e40f` |
| `helper-import-correction/preservation.json` | `aeeb15f98f343a7fb07b803abd37cfc504efb988d9d97b091b0f451e212b7254` |
| `helper-import-correction/verification.json` | `77ec41c794760ea27e3f924c6b9658fba53dc5b28cd0fbf1cca9b8d5c4b7e942` |

Only this review is written. No SDK build/test, actual capture/replay, compiled checker/runtime,
native/device/provider operation, formatter, product edit, Git mutation or subagent ran. Prior reviews
and qualified proofs remain intact. Finish authorized Core Pass-1 gates/publication and deferred
handoff, then stop; platform/physical lanes retain their existing bounds.
