# Mac routing caller v2 — nonsecret build-receipt linkage

## Narrow change

The routing review `c7b2e1e9ab925d2a13994d7379585a1c249590861f6baa829e65e56ba5fe20cf`
approved the three-property concept and required actual build-receipt binding. This additive caller
implements that linkage without modifying the original proposal, sealed v5 lane/verifier, logger,
SDK/product/configuration/authentication state or the 30 preserved failed-attempt records.

New files:

- `proposed-lane-v2.sh`: a caller derivative of the existing Mac lane. Canonical SDK selector/command,
  original capture hook and v5 capture/live calls are unchanged. Only the approved Boundary environment
  properties, descriptor retention and a pre-capture restore-profile gate are added.
- `routing-v2.py`: one fixed-profile descriptor creator and actual-profile checker. No environment
  enumeration, credential access, package resolution or generic routing manager.

At **future coordinator execution**, the shell defines three readonly, nonsecret values. The creator
validates those exact arguments, active checkout and reviewed original identities, hashes the current
regular Core DLL at admission, then publishes an attempt-specific descriptor. Only after successful
publication does the shell export those same immutable values:

```text
BoundaryCorePath=/home/rupicola/Code/y/yubikit-core-docs-integration/src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll
BoundaryNativeVersion=1.18.1
BoundaryComparisonVariant=audit-integration-20261111
```

The descriptor contains only those three environment properties, active root, actual attempt/build
label, current Core identity, and original/derivative/creator/review source identities. No credential,
token, full environment, configuration contents or unrelated process setting is recorded.

## Exact receipt path and failure behavior

For suffix `restore-bound-first`, the future descriptor is:

```text
docs/research/documentation-audit/integrations/core-upstream-20261111/validation/consumer-proof/mac-restore-blocker/routing/mac-comparison-restore-bound-first.json
```

The build logger receives **`--retain-text "$descriptor"`** with that exact root-relative path. Its
ordinary retained-record chain therefore copies/hash-binds the actual descriptor into the actual build
receipt. The SDK command remains exactly the sealed selector command; no accepted verifier needs changing.

The descriptor is created only when the launcher is executed. **No attempt descriptor or routing
directory was created during this source preparation.** Existing raw attempt, evidence or five logger
labels reject admission. Descriptor creation uses exclusive partial creation, file fsync, atomic
same-directory link publication that cannot overwrite a destination, and directory fsync. Any failure
stops before a logger/SDK command; available partial/published files remain. A new suffix is required
for a failed admission—there is no cleanup/retry/quiet overwrite.

After a real successful build, a separate logged profile check retains the actual child dgspec/assets.
It requires native override `[1.18.1, )`, the integration variant, actual resolved canonical package
`Yubico.NativeShims/1.18.1`, and the admission Core identity still unchanged. Failure stops before
capture; actual Csc/reference/copy consumption remains the unchanged v5 verifier's responsibility.
This is not a promise of restore success or feed authorization.

## Source-only checks and next command

Preparation checks parse the Python source, validate Bash syntax and source identity/command linkage.
They execute neither creator/checker, SDK, capture, test, native/device operation nor any authentication
action. The original source/receipt chain and all 30 failed-record snapshots remain unchanged.

After narrow review and coordinator checkout reservation:

```bash
cd /home/rupicola/Code/y/yubikit-core-docs-integration
P=docs/research/documentation-audit/integrations/core-upstream-20261111/validation/consumer-proof
bash "$P/mac-restore-blocker/proposed-lane-v2.sh" restore-bound-first
```

Use a different fresh suffix if that name becomes reserved. The command creates
`mac-comparison-restore-bound-first`, build label `consumer-mac-comparison-build-restore-bound-first`
and evidence `mac-comparison-restore-bound-first-v5`; it does not overwrite the old failed `first`.
All earlier successful contexts stay intact and are not rebuilt for provenance bookkeeping.
