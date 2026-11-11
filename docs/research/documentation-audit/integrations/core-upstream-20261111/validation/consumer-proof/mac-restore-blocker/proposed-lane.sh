#!/usr/bin/env bash
# PROPOSED coordinator-only derivative, not executed by the diagnosis owner.
# Environment properties reach every restore evaluation; no credential/config/source changes.
set -euo pipefail
ROOT=/home/rupicola/Code/y/yubikit-core-docs-integration
P=docs/research/documentation-audit/integrations/core-upstream-20261111/validation/consumer-proof
test "$PWD" = "$ROOT"
test "$#" = 1
[[ "$1" =~ ^[a-z0-9][a-z0-9-]*$ ]]
BoundaryCorePath="$ROOT/src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll" \
BoundaryNativeVersion=1.18.1 \
BoundaryComparisonVariant=audit-integration-20261111 \
  bash "$P/correction-v5/lane.sh" "$1"
