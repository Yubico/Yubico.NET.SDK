#!/usr/bin/env bash
# Reviewed-caller derivative only; no configuration, credential or sealed verifier change.
set -euo pipefail
ROOT=/home/rupicola/Code/y/yubikit-core-docs-integration
V=docs/research/documentation-audit/integrations/core-upstream-20261111/validation
P="$V/consumer-proof"
test "$PWD" = "$ROOT"
test "$#" = 1
suffix=$1
[[ "$suffix" =~ ^[a-z0-9][a-z0-9-]*$ ]]
attempt="mac-comparison-$suffix"
capture_name="$attempt-v5"
descriptor="$P/mac-restore-blocker/routing/$attempt.json"
test ! -e "/home/rupicola/Code/y/.core-docs-audit-runtime/consumer-proof/attempts/$attempt"
test ! -e "$P/evidence/$capture_name"
test -z "${CustomAfterMicrosoftCommonTargets:-}"
for stage in static build restore-profile capture live; do
  test ! -e "$V/commands-v2/consumer-mac-comparison-$stage-$suffix"
done
readonly BoundaryCorePath="$ROOT/src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll"
readonly BoundaryNativeVersion=1.18.1
readonly BoundaryComparisonVariant=audit-integration-20261111
# These immutable shell values are exactly the arguments verified/published by the creator.
# A failed creation stops here, before any logger or SDK command; existing/partial files survive.
python3 -B "$P/mac-restore-blocker/routing-v2.py" create --suffix "$suffix" \
  --core "$BoundaryCorePath" --native "$BoundaryNativeVersion" --variant "$BoundaryComparisonVariant"
export BoundaryCorePath BoundaryNativeVersion BoundaryComparisonVariant
export TMPDIR=/home/rupicola/Code/y/.core-docs-audit-runtime/tmp
export TMP="$TMPDIR"
export TEMP="$TMPDIR"
python3 -B "$V/run-command-v2.py" --label "consumer-mac-comparison-static-$suffix" -- \
  python3 -B "$P/proof-v5.py" static
AuditConsumerAttempt="$attempt" CustomAfterMicrosoftCommonTargets="$PWD/$P/Capture.targets" \
  python3 -B "$V/run-command-v2.py" --label "consumer-mac-comparison-build-$suffix" --toolchain --expect-target build \
    --retain-text "$descriptor" -- \
    dotnet toolchain.cs -- build --project Yubico.YubiKit.AuditIntegration20261111.MacComparison --clean
python3 -B "$V/run-command-v2.py" --label "consumer-mac-comparison-restore-profile-$suffix" \
  --retain-text "verification/MacOSHidBoundaryComparison/obj/audit-integration-20261111/MacOSHidBoundaryComparison.csproj.nuget.dgspec.json" \
  --retain-text "verification/MacOSHidBoundaryComparison/obj/audit-integration-20261111/project.assets.json" -- \
  python3 -B "$P/mac-restore-blocker/routing-v2.py" check --suffix "$suffix"
python3 -B "$V/run-command-v2.py" --label "consumer-mac-comparison-capture-$suffix" -- \
  python3 -B "$P/proof-v5.py" capture --context mac-comparison --attempt "$attempt" \
    --capture-name "$capture_name" --build-label "consumer-mac-comparison-build-$suffix"
python3 -B "$V/run-command-v2.py" --label "consumer-mac-comparison-live-$suffix" -- \
  python3 -B "$P/proof-v5.py" verify --attempt "$attempt" --capture-name "$capture_name" --live
