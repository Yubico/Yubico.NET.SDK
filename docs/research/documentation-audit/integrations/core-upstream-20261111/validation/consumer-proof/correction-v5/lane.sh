#!/usr/bin/env bash
# Coordinator-only fresh Mac comparison lane. Fixture reuses its already successful build separately.
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
test ! -e "/home/rupicola/Code/y/.core-docs-audit-runtime/consumer-proof/attempts/$attempt"
test ! -e "$P/evidence/$capture_name"
test -z "${CustomAfterMicrosoftCommonTargets:-}"
export TMPDIR=/home/rupicola/Code/y/.core-docs-audit-runtime/tmp
export TMP="$TMPDIR"
export TEMP="$TMPDIR"
python3 -B "$V/run-command-v2.py" --label "consumer-mac-comparison-static-$suffix" -- \
  python3 -B "$P/proof-v5.py" static
AuditConsumerAttempt="$attempt" CustomAfterMicrosoftCommonTargets="$PWD/$P/Capture.targets" \
  python3 -B "$V/run-command-v2.py" --label "consumer-mac-comparison-build-$suffix" --toolchain --expect-target build -- \
    dotnet toolchain.cs -- build --project Yubico.YubiKit.AuditIntegration20261111.MacComparison --clean
python3 -B "$V/run-command-v2.py" --label "consumer-mac-comparison-capture-$suffix" -- \
  python3 -B "$P/proof-v5.py" capture --context mac-comparison --attempt "$attempt" \
    --capture-name "$capture_name" --build-label "consumer-mac-comparison-build-$suffix"
python3 -B "$V/run-command-v2.py" --label "consumer-mac-comparison-live-$suffix" -- \
  python3 -B "$P/proof-v5.py" verify --attempt "$attempt" --capture-name "$capture_name" --live
