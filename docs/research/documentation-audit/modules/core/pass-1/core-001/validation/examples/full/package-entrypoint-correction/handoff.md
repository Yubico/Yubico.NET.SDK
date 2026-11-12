# Exact Microsoft.NET.Test.Sdk package-source admission

**Bounded Full-local guard correction, ready for focused review.** Ctap2.IntegrationTests failed in
`capture.py before`, before Csc. This is observer source-admission failure, not a documentation body,
compiler or test/runtime failure. No build or capture phase was run by this correction worker.

## Producer and admission proof

The retained `20261112-051141-913-build/02-build-Ctap2.IntegrationTests.log` identifies the rejected input:

`/home/rupicola/.nuget/packages/microsoft.net.test.sdk/18.0.1/build/net8.0/Microsoft.NET.Test.Sdk.Program.cs`

- **Source SHA-256:** `d66f12f9433816a8c7dc1b2acb9e1010f5418f5eeb51e80f1df57aa1dcab8d8a` (218 bytes).
- **Producer SHA-256:** `93e2c4a4e08fa8db6af3b5907205741ad5184e642181bfdbe949c5109e19aa2f` (3,211 bytes),
  `build/net8.0/Microsoft.NET.Test.Sdk.targets` in the same exact package.
- That target declares `InitialTargets="GenerateProgramFile"`; lines 25/34 select its package source
  by default and enable generation; lines 43–49 conditionally add `$(GeneratedProgramFile)` to Compile.
  This is shipped package source, not a new disk-generated obj file or an example implementation.
- All seven integration projects' restored `net10.0` assets resolve **Microsoft.NET.Test.Sdk/18.0.1**,
  list this source and select the net8.0 props/targets. Their NuGet-generated props/targets import those
  exact files. `resolved-contexts.before.json.snapshot` preserves all nine test-context assets/imports;
  `reconciliation.json` records seven inclusions and two xUnit v3 exclusions (plus the consumer exclusion).
- The package was in the **resolved assets**, but its files were **not** old fixed profile rows and the
  program was **not** in the old known compile-source list. This correction explicitly adds those identities.
- Exact source/props/targets/nuspec bytes match their cached package archive entries. The archive's
  recomputed SHA512 matches its `.nupkg.sha512` sidecar. NuGet metadata/assets **contentHash** is a
  different value and is retained/checked separately; archive/content hashes are not conflated.
  This does not claim digital-signature verification or package execution.

`package-entrypoint-admission.json` pins the exact source, producer, props, nuspec, archive, archive
sidecar and `.nupkg.metadata`, and seven explicit primary project paths. Capture checks their hashes,
the actual assets identity, owning project, build assets, package folder and NuGet import conditions.
The hook additionally **observes** effective `GenerateProgramFile`, `GeneratedProgramFile`,
`NuGetPackageRoot` and `Language`; admission requires true / exact pinned file / pinned root / C#.
These observations do not set or override producer properties.

The source becomes mandatory in the exact expected Compile population for those seven projects only.
The Full before-record captures package/producer/import identities and snapshots the source. The
**unchanged qualified observer** still requires equal before/post items and exact executed Csc source
arguments. Missing or substituted source fails; the existing unknown obj-source guard is unchanged.
No arbitrary source/package/generated-file allowance or global qualification mechanism was added.

## All remaining contexts and static checks

| Contexts | Entry point policy |
| --- | --- |
| Ctap2, WebAuthn, Management, Core, SecurityDomain, Piv, Oath **IntegrationTests** | This exact Test.Sdk 18.0.1 package source and producer. |
| Core.UnitTests / Ctap2.UnitTests | No Microsoft.NET.Test.Sdk in assets/imports; existing xUnit v3 admission unchanged. Already accepted, do not replay. |
| Consumer | No Test.Sdk admission. Already accepted, do not replay. |

The admission oracle failed for all seven integration projects before the correction, then passed.
Twelve negative cases reject altered effective policy, foreign package hash/path/source, missing build
producer, foreign project assets, wrong import version/conditions and an excluded project. Those are
read-only Python metadata/source-set checks, **not capture replay**. Positive effective-policy values
are explicitly synthetic expectations in the static oracle; actual values must be observed on retry.

Full static source/body/authority/wrapper, prior review and preserved-pilot guards pass. **All 300
generated sources and `manifest-contexts-fixed.json` remain byte-exact.** The qualified DocCheck/pilot
kernel and parser, product/root/project files, body contexts and canonical build path remain unchanged.
`capture.delta.json` is the complete three-file preparation delta: `capture.py`, one observation-only
`capture.targets` policy line and `verify.py` wiring/status.

## Preserved checkpoints: do not rerun for profile bookkeeping

All three accepted contexts retain profile
`3e40cb440ddd872de8bfba3ae3c0e45166ff5dbcb3a44aad890b968c97885228` and exact runner-retained outputs:

| Context | Bodies | Accepted-compilation SHA-256 |
| --- | ---: | --- |
| consumer-fourth | 244 | `e044ba23cd4a0a55caa6cf0679716c6aba98c15f82adc292e05361bdf186c357` |
| Core.UnitTests | 6 | `9ff6235c1c8c077d7767f14f2a53f125b9b591a074eca42ace60725652d77e8a` |
| Ctap2.UnitTests | 3 | `097d83207ee0bd098cd42fb862b2f91fa318f955ff762e24d1f8880c08d637c8` |

The `e044…` identity is the accepted-compilation record; its separate logger receipt is `f0e11d…`.
`preservation.json` binds their immutable records and **59 retained files** (26 + 16 + 17), the **19**
failed integration attempt/command identities, old capture/targets/verifier/profile snapshots, all 300
source identities and unchanged manifest. No old profile, success/failure status or output identity
is rewritten. No original mutable output path is called equivalent to its preserved checkpoint.

## Fresh profile and serialized coordinator continuation

`../reviewed-inputs-package-entrypoint.json`

**SHA-256:** `fa8db15afb99ca26f5e7912e2754981fb8a4cffba59e76df5a76136080652aed`

From old profile `3e40…`: **three changed fixed rows**, **11 explicit additions**, **1,729 old rows
unchanged**, **1,743 final fixed rows**. Known compile-source paths grow **1,330 → 1,331**, solely by
the exact pinned package program; all original paths remain. The additions are seven package-file
identities plus the admission descriptor, finite verifier, preservation record and assets/import snapshot.
No manifest replacement or source repin occurs. The 25 projects, 361 accepted product paths/map and all
ten body contexts remain unchanged. Exact changes are in `profile-delta.json`; final static checks in
`verification.json`.

After focused acceptance, load the build skill and run:

```bash
F=docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full
P=fa8db15afb99ca26f5e7912e2754981fb8a4cffba59e76df5a76136080652aed
python3 -B "$F/verify.py"
python3 -B "$F/run.py" --context Ctap2.IntegrationTests --label examples-full-ctap-integration-second \
  --profile "$F/reviewed-inputs-package-entrypoint.json" --profile-sha256 "$P"
```

Require its actual complete zero-warning receipt, effective producer observation, Csc membership,
output proof and immediate retention before the six remaining contexts, one at a time:

| `--context` | Fresh `--label` |
| --- | --- |
| Management.IntegrationTests | examples-full-management-first |
| SecurityDomain.IntegrationTests | examples-full-security-domain-first |
| Piv.IntegrationTests | examples-full-piv-first |
| Core.IntegrationTests | examples-full-core-integration-first |
| WebAuthn.IntegrationTests | examples-full-webauthn-first |
| Oath.IntegrationTests | examples-full-oath-first |

Use the same profile arguments and unchanged `run.py` qualified/canonical clean-build flow for each.
Stop and retain any first failure; these static checks promise no later compiler success. Do not rerun
the accepted three contexts or execute injected assemblies. The final ordinary uninjected clean solution
build still precedes SDK tests. No SDK build/test, capture phase/replay, native/device/provider execution,
formatter, Git mutation or subagent occurred here. Finish Core Pass 1 and the deferred handoff, then stop.
