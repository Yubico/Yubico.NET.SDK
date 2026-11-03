# Management C — preliminary-compilation example corrections

## Status and scope

Editor: GPT-6.1 Sol (`github-copilot/gpt-6.1-sol`), 2026-11-03. Coordinator reported an actual
`shared-readme-5` CS0117 failure in its preliminary 63-group compilation, plus the async-without-await
callback in adjacent `shared-readme-6`. Verified those complete examples against current source and
preserved preliminary inputs, then corrected only `src/Tests.Shared/README.md` in
`/tmp/opencode/management-001-edit-c`. Updated PRIMARY `edit-c.md` mappings/snippet prerequisites and
wrote this report. No C# source, runtime behavior, API, test, project or compiler-reference configuration
was edited. Earlier module/shared review corrections and their original records remain preserved.

Loaded `workflow-debug` for source-first diagnosis; existing `review-receive`, `docs-module` and module/
shared instructions remain applicable. The coordinator's preserved reproduction is the evidence here;
this editor did **not** reproduce it by running compilation, build, tests or hardware, as prohibited.
No no-op await, fake helper, compile-source file, commit or delegation was introduced. Independent C2
follow-up and fresh compilation of corrected final snippets remain pending.

## Preserved failure and original example evidence

The original log and extracted inputs were read, not modified:

- `validation/snippets-preliminary.log:349–361`: group `shared-readme-5`, extracted source hash
  `bf5ad2d3a8525a1bb70f523d3b1388973bed8623aec43c5345db2edea86539b4`, records:
  `error CS0117: 'DeviceCapabilities' does not contain a definition for 'SecurityDomain'`
  at extracted `(27,73)`. That group also encounters the separately investigated reference warning;
  its compile-only FAIL is not a hardware or test execution result.
- `validation/preliminary-examples/shared-readme-5.cs:26–38` preserves the old row
  `[WithYubiKey(MinFirmware = "5.7.2", Capability = DeviceCapabilities.SecurityDomain)]`, old
  `Scp11_ModernDevice_ReturnsKeyInfo` method name, caller-owned default SCP03 local and actual awaited
  reset/helper/key-information/assertion task.
- `validation/preliminary-examples/shared-readme-6.cs:25–38` preserves `ProductionKeysOnly` and
  `RunsOnlyOnProductionKeyRange`; its async callback contains only
  `Assert.True(info.SerialNumber > 10_000_000);` and no await. Source inspection confirms the mismatch
  regardless of whether a compiler warning is enabled by that preliminary checker configuration.
- `validation/snippets-preliminary.log:362–371` identifies group `shared-readme-6` and its source hash
  `58e38f44f6310d992a808f135e2c4f0d93b879fee26c2e08f51dd209b13e77d6`. The recorded failure there is
  CS1701; this report does **not** relabel that warning as CS1998 or claim that callback changes fix it.

All paths above are relative to this PRIMARY run directory. The preliminary log and extracted source
hashes describe the failed original inputs, not the current corrected README or a newly passing run.
The separate assembly-reference identity investigation remains with the tooling agent/coordinator.

## Verified causes and corrections

### Filtered Test, fence 5 (`shared-readme-5`)

Actual source evidence:

- `src/Core/src/Devices/DeviceCapabilities.cs:17–61` defines None, Otp, U2f, OpenPgp, Piv, Oath, HsmAuth,
  Fido2 and their All combination. It has no SecurityDomain member; inventing that application bit was
  the source-level cause of CS0117, not a missing import or a need for a new runtime enum value.
- `src/Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs:78–83,98–106` parses a three-component
  minimum firmware criterion and tests exposed connection support independently of capability bits.
  `ConnectionType.SmartCard` is a real supported device-filter value.
- `src/SecurityDomain/src/SecurityDomainSession.cs:40–42` declares Security Domain and SCP03 at 5.3.0;
  SCP11's distinct minimum is 5.7.2. The example actually supplies SCP03 defaults, not SCP11 parameters.
  `ListKeyInformationAsync` at `:244–252` performs the real key-information GET DATA task.
- The actual project-specific helper at
  `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs:31–66`
  opens a typed SmartCard connection via `WithConnectionAsync`, optionally runs the reset session,
  constructs the body session with the supplied SCP parameters and then awaits the action. It disposes
  sessions/connection, not caller-owned input parameters. This narrow source dependency was already read
  for CSH-R2; no broader SecurityDomain audit or device-reset qualification is implied.

Applied documentation correction:

- Use `[WithYubiKey(MinFirmware = "5.3.0", ConnectionType = ConnectionType.SmartCard)]`; do not select a
  nonexistent capability or add a new enum merely to compile documentation.
- Rename the documentation-only test member to `Scp03_AfterReset_ReturnsKeyInfo`, accurately naming the
  supplied protocol and actual key-information task. No real test method or API was renamed.
- Keep `using var scpKeyParams = Scp03KeyParameters.Default`, the real awaited
  `WithSecurityDomainSessionAsync(resetBeforeUse: true, ...)`, real `ListKeyInformationAsync`, NotEmpty
  assertion and named SCP parameter. No operation was removed to evade compilation.
- State that execution requires a dedicated, allow-listed SmartCard key actually supporting Security
  Domain/SCP03, in state suitable for the real helper's reset and subsequent authentication with SCP03
  defaults. Reset destroys custom SecurityDomain key configuration. Minimum firmware/exposed SmartCard
  filters do not inspect provisioned SCP keys, prove application support or guarantee reset success.
  The helper explicitly opens SmartCard; a device filter is not itself a transport pin.

### Custom Filter, fence 6 (`shared-readme-6`)

The original body had a cached-only assertion inside an async callback. The actual Management helper
supports both synchronous and asynchronous callbacks, but this example now intentionally demonstrates
a fresh query rather than merely removing async from a cached check:

1. Await the actual `mgmt.GetDeviceInfoAsync()` into `freshInfo`.
2. Assert that the **fresh** serial satisfies the custom serial-range condition.
3. Compare cached callback `info.SerialNumber` with `freshInfo.SerialNumber`.

The custom `IYubiKeyFilter` declaration, actual helper, awaited task, ordinary disposal scope and
dedicated-device allow-list boundary remain intact. No completed-task/Task.Yield/delay await was added.
The serial-range filter name does not authorize running against production/personal devices.

## Current snippet extraction handoff

`edit-c.md` now locates fence 5 by `Scp03_AfterReset_ReturnsKeyInfo` and fence 6 by
`ProductionKeysOnly` / `RunsOnlyOnProductionKeyRange`.

- **Fence 5:** current composable test-member fragment. Keep its using directive at compilation-unit
  scope, wrap the whole actual member in a test class and use the real xUnit v2/SkippableFact dependencies.
  Reference the actual SecurityDomain integration helper project/assembly or link the exact helper source
  with real Core/SecurityDomain/Tests.Shared projects, as specified in `edit-c.md` and the preceding
  `correction-c-shared.md`. Retain the SmartCard/5.3.0 row, owned parameter lifetime, reset and read/assert.
- **Fence 6:** current complete class-member/filter example under profile T (Core/Management/Tests.Shared,
  xUnit v2/SkippableFact and their actual namespace imports). Keep the filter declaration plus real
  fresh read, serial-range assertion and cached/fresh comparison. No additional typed runtime provider
  or fake SDK member is needed.

Both are current meaningful compile targets; neither is reclassified as historical or excluded to hide
an error. No fence is added or removed. Their hardware prerequisites are not proven by a compile check,
and the coordinator must re-extract these final blocks rather than reuse the preserved failed inputs.

## Exact changed path, hash and limited verification

Only existing working file changed this round: `src/Tests.Shared/README.md`.

Final SHA-256: `061a24d373ea7e26445de3242836a85c5dcdd96631fccbf88b27ec2d993e821e`.

This supersedes that file's earlier shared-correction hash only; root/shared/module original review and
correction records remain accurate historical checkpoints. All other working files were left untouched
in this compilation-correction round.

`git diff --check da61d2e6` in the C worktree completed with exit 0 and no output after these edits.
This is whitespace checking, **not** recompilation or proof that CS0117/CS1701 or all warnings are gone.
Fresh final compilation remains coordinator/tooling work. C2 should re-review both complete examples,
prerequisites and this correction record; the existing CSH/module corrections remain separately tracked.
No MGMT-IMP/MGMT-XREF behavior/device-evidence item is implemented by these documentation corrections.
