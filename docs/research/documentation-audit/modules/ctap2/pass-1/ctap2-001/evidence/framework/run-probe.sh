#!/usr/bin/env bash
# Revised 2026-11-04 discovery-isolated harness; NOT the script used for original evidence.
# Run only with the coordinator's reserved build lane; no SDK or test execution.
set -euo pipefail
set -C # Refuse to overwrite earlier reproduction logs.
repo=/home/rupicola/Code/y/Yubico.Yubikit.NET
workspace=/tmp/opencode/ctap2-001-framework-probe
cd "$repo"
dotnet --info
dotnet toolchain.cs -- build --project AuditCt001.FrameworkProbe > /tmp/opencode/ctap2-001-framework-isolated-build.log 2>&1
cat /tmp/opencode/ctap2-001-framework-isolated-build.log
dotnet "$workspace/bin/Release/net10.0/Yubico.YubiKit.Ctap2.FrameworkProbe.dll" > /tmp/opencode/ctap2-001-framework-isolated-probe.log 2>&1
cat /tmp/opencode/ctap2-001-framework-isolated-probe.log
