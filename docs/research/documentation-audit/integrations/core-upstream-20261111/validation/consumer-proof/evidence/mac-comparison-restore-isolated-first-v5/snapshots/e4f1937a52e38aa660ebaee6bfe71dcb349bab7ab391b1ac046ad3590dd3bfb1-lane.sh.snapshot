#!/usr/bin/env bash
# Coordinator-only remaining fresh contexts. Earlier accepted builds/captures are not repeated.
set -euo pipefail
ROOT=/home/rupicola/Code/y/yubikit-core-docs-integration
V=docs/research/documentation-audit/integrations/core-upstream-20261111/validation
P="$V/consumer-proof"
test "$PWD" = "$ROOT"
test "$#" = 2
context=$1
suffix=$2
[[ "$suffix" =~ ^[a-z0-9][a-z0-9-]*$ ]]
case "$context" in
  mac-comparison) selector=Yubico.YubiKit.AuditIntegration20261111.MacComparison ;;
  fixture) selector=Yubico.YubiKit.Management.IntegrationTests ;;
  *) exit 2 ;;
esac
attempt="$context-$suffix"
capture_name="$attempt-v4"
test ! -e "/home/rupicola/Code/y/.core-docs-audit-runtime/consumer-proof/attempts/$attempt"
test ! -e "$P/evidence/$capture_name"
test -z "${CustomAfterMicrosoftCommonTargets:-}"
export TMPDIR=/home/rupicola/Code/y/.core-docs-audit-runtime/tmp
export TMP="$TMPDIR"
export TEMP="$TMPDIR"
python3 -B "$V/run-command-v2.py" --label "consumer-$context-static-$suffix" -- \
  python3 -B "$P/proof-v4.py" static
AuditConsumerAttempt="$attempt" CustomAfterMicrosoftCommonTargets="$PWD/$P/Capture.targets" \
  python3 -B "$V/run-command-v2.py" --label "consumer-$context-build-$suffix" --toolchain --expect-target build -- \
    dotnet toolchain.cs -- build --project "$selector" --clean
python3 -B "$V/run-command-v2.py" --label "consumer-$context-capture-$suffix" -- \
  python3 -B "$P/proof-v4.py" capture --context "$context" --attempt "$attempt" \
    --capture-name "$capture_name" --build-label "consumer-$context-build-$suffix"
python3 -B "$V/run-command-v2.py" --label "consumer-$context-live-$suffix" -- \
  python3 -B "$P/proof-v4.py" verify --attempt "$attempt" --capture-name "$capture_name" --live
