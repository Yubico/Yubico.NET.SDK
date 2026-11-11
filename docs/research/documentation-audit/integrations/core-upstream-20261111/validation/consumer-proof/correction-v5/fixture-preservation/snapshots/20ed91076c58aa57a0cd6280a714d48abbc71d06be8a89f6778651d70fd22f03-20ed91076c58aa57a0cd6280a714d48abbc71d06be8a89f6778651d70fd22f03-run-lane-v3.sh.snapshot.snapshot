#!/usr/bin/env bash
# Coordinator-only fresh lane. The original C# bodies/hook remain unchanged.
set -euo pipefail
ROOT=/home/rupicola/Code/y/yubikit-core-docs-integration
V=docs/research/documentation-audit/integrations/core-upstream-20261111/validation
P="$V/consumer-proof"
RUNTIME=/home/rupicola/Code/y/.core-docs-audit-runtime/consumer-proof
test "$PWD" = "$ROOT"
test "$#" = 2
context=$1
suffix=$2
[[ "$suffix" =~ ^[a-z0-9][a-z0-9-]*$ ]]
case "$context" in
  probe) name=Probe ;;
  examples) name=Examples ;;
  annotations) name=Annotations ;;
  consumers) name=Consumers ;;
  mac-comparison) name=MacComparison ;;
  fixture) name=fixture ;;
  *) exit 2 ;;
esac
selector="Yubico.YubiKit.AuditIntegration20261111.$name"
if [ "$context" = fixture ]; then
  selector=Yubico.YubiKit.Management.IntegrationTests
fi
test -z "${CustomAfterMicrosoftCommonTargets:-}"
attempt="$context-$suffix"
capture_name="$attempt-v3"
test ! -e "$RUNTIME/attempts/$attempt"
test ! -e "$P/evidence/$capture_name"
test -f "$P/correction-v3/receipt.json"
export TMPDIR=/home/rupicola/Code/y/.core-docs-audit-runtime/tmp
export TMP="$TMPDIR"
export TEMP="$TMPDIR"
python3 -B "$V/run-command-v2.py" --label "consumer-$context-static-$suffix" -- \
  python3 -B "$P/proof-v3.py" static
AuditConsumerAttempt="$attempt" CustomAfterMicrosoftCommonTargets="$PWD/$P/Capture.targets" \
  python3 -B "$V/run-command-v2.py" --label "consumer-$context-build-$suffix" --toolchain \
    --expect-target build -- \
    dotnet toolchain.cs -- build --project "$selector" --clean
python3 -B "$V/run-command-v2.py" --label "consumer-$context-capture-$suffix" -- \
  python3 -B "$P/proof-v3.py" capture --context "$context" --attempt "$attempt" \
    --capture-name "$capture_name" --build-label "consumer-$context-build-$suffix"
python3 -B "$V/run-command-v2.py" --label "consumer-$context-live-$suffix" -- \
  python3 -B "$P/proof-v3.py" verify --attempt "$attempt" --capture-name "$capture_name" --live
