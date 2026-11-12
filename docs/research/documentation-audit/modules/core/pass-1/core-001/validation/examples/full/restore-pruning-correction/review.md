# Independent Sol confirmation: redundant host PackageReferences

## PASS — consumer second attempt ready

Accept `reviewed-inputs-restore-fixed.json` at SHA-256
`54170deeb614b2683903b11ed2a52495a16335f0eb787c39a71f1a4b3f66f9ae` for the coordinator's serialized
consumer retry. No further correction or framework work is required by this bounded review. Successful
restore, actual Csc execution and accepted compilation/output proof remain pending that attempt.

Source baseline remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; observed metadata HEAD was
`da66f5d01ba594993974ddd26723e8c300c0a454`. Keep the reserved primary stable through this cycle;
the queued, untransferred sampling patch is outside this input admission.

## Exact correction and evidence

- Independently compared the preserved host to current bytes. The entire delta is removal of the
  two direct `Microsoft.Extensions.Hosting` and `Microsoft.Extensions.Logging.Console` PackageReferences.
  The `Microsoft.AspNetCore.App` FrameworkReference, all three Serilog references/versions and all
  ProjectReferences remain unchanged. No suppression, pruning disablement, AOT override or body/helper
  replacement was added.
- Read the actual retained NU1510 errors: both explicitly say the package is automatically available
  and request removal of the direct reference. The installed framework's pinned `PackageOverrides.txt`
  lists both packages. Current Context7 `/dotnet/docs` .NET 10 pruning documentation supports this
  framework-provided-reference correction. The actual retry must still prove its resolved references.
- Profile comparison confirms exactly **one changed host-project participant**, **1,714 identical rows**,
  the same 1,715-path population, unchanged compile-source membership and unchanged 25-project closure.
  Every new-profile input matches its current length/hash. No unrelated source or tool was repinned.
- The old profile remains exact at
  `8c385b8dae912d846dcd9463c08bf0bbb765ba6a00ffa9f657962a7971f0b81d`; its snapshot and the old host
  snapshot match their original identities. All **13 failed-attempt/command/stream records** match
  the preservation manifest. The original receipt remains failed/nonzero. No before-Csc/compiler/
  finish/accepted-compilation record exists for that restore failure; it is not a documentation-body,
  compiler or runtime verdict and receives no retrospective success.
- Bodies, wrappers, selectors, observer hooks, qualified parser/logger/checker and root/SDK policy
  remain unchanged. No broader context review is reopened.

Ran the already inspected read-only guard:

```bash
python3 -B docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full/verify.py
```

Exit **0**: all **300** prepared source reconstructions / **ten** contexts and existing source/body,
pilot-preservation and four-fix static guards pass. This does not claim a successful restore or build.

## Exact coordinator retry

With exclusive reserved-primary ownership and the current build skill loaded:

```bash
F=docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full
P=54170deeb614b2683903b11ed2a52495a16335f0eb787c39a71f1a4b3f66f9ae
python3 -B "$F/run.py" --context consumer --label examples-full-consumer-second \
  --profile "$F/reviewed-inputs-restore-fixed.json" --profile-sha256 "$P"
```

Require the actual complete zero-warning canonical restore/build RESULT, original receipt,
Csc/input/output guards and immediate preservation before accepting consumer compilation or continuing
serially to the existing nine test contexts with this profile. Preserve any first failure and use a
fresh label after reviewed corrections. No example or injected test DLL is run. The final normal
uninjected clean solution build still restores ordinary test outputs before SDK tests.

## Identities and limits

| Record, relative to `validation/examples/full/` | SHA-256 |
| --- | --- |
| `host/src/Yubico.YubiKit.Audit20261111.DocExamples.Full.csproj` | `b5f40bb307ca36e5d68c79d1d79dc1c7f79f7c247127761af271dcd4930fbc9a` |
| `restore-pruning-correction/host-only.diff` | `6f50621cacce42bd63e8f575cc4470317efd627d6906e39439a13767edd4731c` |
| `restore-pruning-correction/profile-delta.json` | `6852af5c8217f7aca10648c281797cc085f6e1273f003a3c85ea3e4ff5f135c0` |
| `restore-pruning-correction/preservation.json` | `f42712188a728239dba383e2ebf700a56339a16db0ebf86dbe86aca904581dea` |
| `restore-pruning-correction/verification.json` | `3389966640d4113fa562cbe50086137a128c5e6bfba2b9616be0b2da9ce56b14` |

Only this review is written. No SDK build/test, actual capture/replay, runtime/native/device operation,
formatter, product edit, Git mutation or subagent ran. Prior reviews and original failure/history remain
intact. Complete the authorized Core Pass-1 gates/publication and deferred handoff, then stop.
