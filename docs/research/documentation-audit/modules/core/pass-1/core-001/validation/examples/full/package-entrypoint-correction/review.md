# Independent Sol confirmation: exact Test.Sdk package source

## PASS — Ctap2 integration second attempt ready

Accept `reviewed-inputs-package-entrypoint.json` at SHA-256
`fa8db15afb99ca26f5e7912e2754981fb8a4cffba59e76df5a76136080652aed` for the coordinator's serialized
continuation. No further correction or new framework is required by this bounded review. Effective
producer/source participation and new compiler/output acceptance remain pending the actual retry.

Source baseline remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; observed metadata HEAD was
`da66f5d01ba594993974ddd26723e8c300c0a454`. Keep the reserved primary stable. The earlier three
accepted contexts retain their original profile and proofs; this profile change is not a reason to
recompile them for bookkeeping.

## Exact source and producer, not a generated-code wildcard

Read the actual package source/props/targets, descriptor, Full-local capture delta, assets/import
reconciliation and failure/preservation records. The admission is for exactly:

```text
/home/rupicola/.nuget/packages/microsoft.net.test.sdk/18.0.1/build/net8.0/Microsoft.NET.Test.Sdk.Program.cs
```

- Source: **218 bytes**, SHA-256
  `d66f12f9433816a8c7dc1b2acb9e1010f5418f5eeb51e80f1df57aa1dcab8d8a`.
- Producer: `build/net8.0/Microsoft.NET.Test.Sdk.targets`, SHA-256
  `93e2c4a4e08fa8db6af3b5907205741ad5184e642181bfdbe949c5109e19aa2f`.
  It declares `InitialTargets="GenerateProgramFile"`; its actual default property selects the shipped
  Program source and its conditioned target adds `$(GeneratedProgramFile)` to Compile for C#/VB.
  The tiny entry point is actual shipped package source, not a fabricated example helper or new obj file.
- The exact program, producer, props and nuspec bytes match their cached archive members. Archive
  SHA512 recomputation matches the archive sidecar. NuGet metadata/assets contentHash is retained and
  checked in its own role, not incorrectly equated with archive SHA512. No package-signature
  verification or assembly/package execution is claimed.
- All seven integration contexts' retained `net10.0` assets resolve the exact package/version, list
  that program and select its exact net8.0 props/targets. The NuGet-generated imports and their Exists/
  ExcludeRestorePackageImports conditions are checked, together with package folder, library path,
  content hash and owning project. This is actual dependency-source provenance, not permission for
  an arbitrary package to add source.

Admission uses seven explicit primary project paths: **Ctap2, WebAuthn, Management, Core,
SecurityDomain, Piv and Oath IntegrationTests**. **Consumer, Core.UnitTests and Ctap2.UnitTests are
excluded**. There is no filename glob, arbitrary external-source directory or generalized package
qualification exemption. Adding the one path to the global known-source list does not admit it to
excluded contexts: their expected population omits it and the unchanged unexpected-source guard
rejects an outside-obj package source.

## Actual retry proof remains mandatory

For each admitted integration project the Full before-capture now requires:

- Effective **GenerateProgramFile=true**, **GeneratedProgramFile=the exact pinned program**, the pinned
  **NuGetPackageRoot**, and **Language=C#**, plus the existing SDK/project/target/compiler/analyzer policy.
  The hook only records those effective values; it does not set them to manufacture producer proof.
- Exact current package-file identities, actual owning assets and matching NuGet import identities/
  conditions. The before record binds them, captures the assets identity and retains a source snapshot.
- The package program as a **mandatory** member of the before Compile population; exact before/post
  membership and actual executed Csc source arguments are still enforced by the unchanged qualified
  observer. Missing, duplicate, substituted or unapproved source cannot be accepted by a successful
  metadata check alone.

The positive effective-policy values used in the read-only oracle are explicitly **synthetic expected
values**, not observations from an unperformed retry. Current archive/assets/import inspection is not
a Csc completion claim. The unknown obj-source guard, qualified kernel, private selected-project
descriptor adapter, final output logic, parser, logger, canonical command and zero-warning policy remain
unchanged. No body, IVT, test/project policy or runtime execution path is added.

## Fresh read-only checks and preservation

After inspecting its entry points/imports and read-only package/metadata operations, ran:

```bash
python3 -B docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full/verify.py
```

Exit **0**: seven source-set inclusions, three context exclusions, **12 negative cases**, exact archive/
package/assets/import checks, unchanged observer/obj guard and all original **300 body/wrapper**
reconstructions pass. Negative cases cover effective producer settings, wrong package hash/path/source,
missing build producer, foreign owning assets, wrong import version/conditions and excluded project.

Independent profile/JSON/diff checks confirmed:

- All **1,743 fixed rows** match current lengths/hashes: three changed Full-local preparation rows
  (`capture.py`, one observation-only targets line and verifier wiring/status), **1,729 identical old
  rows**, eleven explicit additions. No source/manifest/product map is repinned.
- Known compile sources grow **1,330 → 1,331**, solely by the exact pinned Program.cs. All original
  source paths, 25 projects, 361 accepted product paths/map and ten body contexts remain unchanged.
- Reproduced all three complete preparation diffs against preserved snapshots. The AST/static guard
  confirms `kernel`, `full_descriptors` and `finish` are unchanged; the compiler still calls the same
  qualified observer. The targets delta adds only four observed producer properties to the policy line.
- All 300 generated source identities and `manifest-contexts-fixed.json` remain byte-exact. No wrapper,
  accepted body, import, ownership scope or API call changed.
- The prior accepted consumer-fourth, Core.UnitTests and Ctap2.UnitTests records remain respectively
  `e044ba23cd4a0a55caa6cf0679716c6aba98c15f82adc292e05361bdf186c357`,
  `9ff6235c1c8c077d7767f14f2a53f125b9b591a074eca42ace60725652d77e8a` and
  `097d83207ee0bd098cd42fb862b2f91fa318f955ff762e24d1f8880c08d637c8`.
  Their immutable record identities and **59 retained files** (26 + 16 + 17) match, and each accepted
  record still binds the old `3e40cb44…` profile. Retained originals are not equated with subsequently
  overwritten live paths. Do not rerun these three contexts solely for this admission change.
- All **19 failed integration-attempt/command identities** match. The rejected package source appears
  exactly once in its observed pre-Csc source list. That failed attempt has no before/ compiler/finish/
  accepted-compilation proof and remains a guard admission failure, not a body/compiler/runtime result.

## Exact coordinator continuation

With exclusive reserved-primary ownership and the current build skill loaded:

```bash
F=docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full
P=fa8db15afb99ca26f5e7912e2754981fb8a4cffba59e76df5a76136080652aed
python3 -B "$F/run.py" --context Ctap2.IntegrationTests --label examples-full-ctap-integration-second \
  --profile "$F/reviewed-inputs-package-entrypoint.json" --profile-sha256 "$P"
```

Require the actual complete zero-warning original receipt, effective producer observation, Csc
membership/output proof and immediate retention before the six remaining integration contexts, one
at a time with this profile. Preserve any first failure under its own label; static admission promises
no later compiler success. Do not execute example or injected test assemblies. The final normal
uninjected clean solution build still restores test outputs before SDK tests. Do not rebuild prior
accepted contexts or checker/logger tooling merely to update profile bookkeeping.

## Reviewed identities and limits

Paths below are relative to `validation/examples/full/`.

| Record | SHA-256 |
| --- | --- |
| `package-entrypoint-admission.json` | `502d4f2dcf8cccd3c50105b601ff647a7600914dc1b8cfe314758f16c28880fd` |
| `capture.py` | `48d6c51c11a5ac79385453ccd068484fd17c59d40b601e05c1e5e35ed86a1424` |
| `capture.targets` | `ff4d4a5eff05e391e27418e1932b0b9b189663b9dbc6c4d6deb6cf6d7cc2b74c` |
| `package-entrypoint-correction/preservation.json` | `bf36dbec72c1d10dc86c25a934d18d7920287e29b80a1dd6016fe7e1ce2e2c13` |
| `package-entrypoint-correction/verification.json` | `91642e861499d7b90f5f3e2d0f49bec84759568c9eb3b83b13f5db3fc2a207d6` |
| `package-entrypoint-correction/profile-delta.json` | `f77a641a0910d90b7c54a45084bd2d2976bb8aae07317b9d1103a5137b2729f1` |

Only this review is written. No SDK build/test, actual capture/replay, compiled checker/runtime,
native/device/provider operation, formatter, product edit, Git mutation or subagent ran. Prior reviews,
profiles, seals and qualified tools remain intact. Finish authorized Core Pass-1 gates/publication and
deferred handoff, then stop; platform/physical lanes retain their existing bounds.
