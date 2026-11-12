# NU1510: two redundant audit-host package references

**Bounded restore-configuration correction, ready for confirmation.** The original
`examples-full-consumer-first` attempt remains **failed before Csc**. It establishes no documentation-body,
compiler or runtime failure. No successful full-context compilation or example execution is claimed.

## Exact fix

The host retains its existing `FrameworkReference Include="Microsoft.AspNetCore.App"`. Remove only:

```xml
<PackageReference Include="Microsoft.Extensions.Hosting" />
<PackageReference Include="Microsoft.Extensions.Logging.Console" />
```

The three Serilog references/versions, all real project references, bodies, wrappers, observer hooks,
parser, SDK/root policy and qualified tooling remain exact. No NoWarn, pruning disablement, AOT flag
or substitute helper is added. `host-only.diff` is the complete two-line project delta.

The retained toolchain step reports both NU1510 errors and explicitly requests removing those direct
references because they are automatically available. Context7 `/dotnet/docs` .NET 10 SDK/package-pruning
documentation confirms framework-provided references are pruned by default. The installed
`Microsoft.AspNetCore.App.Ref/10.0.15/data/PackageOverrides.txt` lists Hosting and Logging.Console;
its exact identity is recorded in `preservation.json`. The actual retry must still prove successful
restore, real reference ownership and zero warnings through the existing canonical capture.

## Preserved failure and new profile

`preservation.json` hashes all **13** original attempt/toolchain/stream records, including
`source-before.json` and the complete failed command receipt. They remain at their original paths.
The old host and profile are retained byte-exact as snapshots; original profile
`8c385b8dae912d846dcd9463c08bf0bbb765ba6a00ffa9f657962a7971f0b81d` remains unchanged.
No Csc-before/compiler/finish/accepted-compilation record exists for that failed restore.

`reviewed-inputs-restore-fixed.json` changes **one known host-project input**; the other **1,714** fixed
rows, compile-source membership, 25-project closure and all manifest/raw body fields remain exact.
No capture/verifier selector change was required. Static body/wrapper/source/input guards pass for all
300 prepared sources and ten actual contexts. This is not a successful restore or compiler result.

**Fresh profile SHA-256:**
`54170deeb614b2683903b11ed2a52495a16335f0eb787c39a71f1a4b3f66f9ae`

## Coordinator retry after confirmation

```bash
F=docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full
P=54170deeb614b2683903b11ed2a52495a16335f0eb787c39a71f1a4b3f66f9ae
python3 -B "$F/verify.py"
python3 -B "$F/run.py" --context consumer --label examples-full-consumer-second \
  --profile "$F/reviewed-inputs-restore-fixed.json" --profile-sha256 "$P"
```

The wrapper uses the unchanged qualified logger/capture flow and canonical
`dotnet toolchain.cs -- build --project Yubico.YubiKit.Audit20261111.DocExamples.Full --clean`.
Accept its actual complete zero-warning restore/build/Csc/output/preservation proof before continuing
the nine unchanged test-context commands with this fresh profile. Stop and retain any first failure.
No test or example DLL is run; the final normal uninjected clean solution build still precedes SDK tests.
No build/test/capture/native/device/crypto-provider execution, Git mutation or subagent occurred here.
