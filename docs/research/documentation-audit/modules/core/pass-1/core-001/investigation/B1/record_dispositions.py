"""Serialize B1's completed source review; writes only B1-owned investigation records."""
import hashlib
import json
import subprocess
from collections import Counter
from pathlib import Path

REPO = Path(__file__).resolve().parents[9]
RUN = Path("docs/research/documentation-audit/modules/core/pass-1/core-001")
OUT = RUN / "investigation/B1"
PIN = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"


def sha(data):
    return hashlib.sha256(data).hexdigest()


def git(*args):
    return subprocess.check_output(["git", *args], cwd=REPO)


def main():
    packet = json.loads((REPO / RUN / "inventory/packets/B1-portable-lifetimes.json").read_text())
    inventory = json.loads((REPO / RUN / "inventory/semantic-units.json").read_text())
    selected = set(packet["unit_ids"] + packet["shared_read_unit_ids"])
    primary = set(packet["unit_ids"])
    units = [u for u in inventory if u["id"] in selected]
    assert len(units) == len(selected) == 1204
    assert len(primary) == 1078 and len(packet["paths"]) == 49
    shipping = "src/Core/src/"
    tests = "src/Core/tests/Yubico.YubiKit.Core.UnitTests/"
    integrations = "src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/"
    spans = []

    def candidate(number, path, start, end):
        spans.append((f"CORE-B1-C{number:03}", path, start, end))

    for path, start, end in [
        (shipping + "Transports/SmartCard/SmartCardSharing.cs", 20, 32),
        (shipping + "Transports/SmartCard/PcscNativeResources.cs", 118, 119),
        (tests + "Transports/SmartCard/SmartCardSharingTests.cs", 23, 29),
        (tests + "Transports/SmartCard/SmartCardSharingTests.cs", 276, 289),
    ]:
        candidate(1, path, start, end)
    candidate(2, shipping + "Transports/SmartCard/FindPcscDevices.cs", 9, 14)
    candidate(2, shipping + "Transports/SmartCard/FindPcscDevices.cs", 45, 163)
    candidate(2, shipping + "Transports/SmartCard/PcscDevice.cs", 6, 33)
    listener = shipping + "Transports/SmartCard/DesktopSmartCardDeviceListener.cs"
    for start, end in [(21, 30), (215, 350)]:
        candidate(3, listener, start, end)
    for start, end in [(71, 111), (159, 213), (281, 350), (449, 479)]:
        candidate(4, listener, start, end)
    candidate(4, tests + "Transports/SmartCard/DesktopSmartCardDeviceListenerDisposalTests.cs", 19, 22)
    candidate(4, tests + "Transports/SmartCard/DesktopSmartCardDeviceListenerDisposalTests.cs", 231, 256)
    candidate(4, tests + "Transports/SmartCard/DesktopSmartCardDeviceListenerDisposalTests.cs", 312, 345)
    candidate(4, integrations + "Transports/SmartCard/SmartCardDeviceListenerIntegrationTests.cs", 20, 23)
    for start, end in [(21, 26), (60, 107)]:
        candidate(5, shipping + "Devices/RegisteredConnections.cs", start, end)
    candidate(5, tests + "Devices/DeviceConnectionRegistryTests.cs", 304, 313)
    candidate(5, "docs/architecture/connection-ownership-and-contention.md", 102, 114)
    candidate(6, shipping + "Transports/SmartCard/ISmartCardConnection.cs", 45, 68)
    candidate(6, shipping + "Transports/SmartCard/PcscConnectionNativeState.cs", 379, 414)
    candidate(6, shipping + "Transports/SmartCard/PcscConnectionNativeState.cs", 597, 619)
    candidate(6, shipping + "Transports/SmartCard/UsbSmartCardConnection.cs", 117, 131)
    candidate(6, tests + "Transports/SmartCard/PcscConnectionLifetimeTests.cs", 348, 393)
    candidate(6, tests + "Transports/SmartCard/PcscConnectionLifetimeTests.cs", 774, 792)
    guide = "docs/architecture/connection-ownership-and-contention.md"
    for start, end in [(1, 10), (154, 211), (238, 245)]:
        candidate(7, guide, start, end)
    candidate(7, shipping + "Devices/DeviceConnectionRegistry.cs", 24, 34)
    candidate(7, tests + "Devices/ApplicationSessionScpTests.cs", 26, 31)
    candidate(7, tests + "Devices/ApplicationSessionScpTests.cs", 301, 337)
    candidate(7, tests + "Devices/ConnectionOwnershipContractTests.cs", 237, 245)
    candidate(7, tests + "Devices/ConnectionOwnershipContractTests.cs", 294, 312)
    candidate(8, shipping + "Devices/YubiKeyConnectionExtensions.cs", 223, 289)
    candidate(8, tests + "Devices/SessionTransportTests.cs", 167, 181)
    candidate(8, "src/Cli.Commands/src/Infrastructure/YkCommandBase.cs", 21, 26)
    candidate(9, shipping + "Devices/PcscConnectionSlot.cs", 42, 97)
    candidate(10, shipping + "Transports/SmartCard/PcscConnectionNativeState.cs", 221, 244)
    candidate(11, "src/Core/CLAUDE.md", 746, 766)
    candidate(11, "src/Core/tests/CLAUDE.md", 37, 57)
    candidate(12, shipping + "Devices/DeviceConnectionRegistry.cs", 24, 34)
    candidate(12, shipping + "Devices/DeviceConnectionRegistry.cs", 87, 125)
    candidate(12, tests + "Devices/DeviceConnectionRegistryTests.cs", 30, 53)

    files = {}
    rows = []
    for unit in units:
        path = unit["path"]
        if path not in files:
            raw = (REPO / path).read_bytes()
            pinned = git("show", f"{PIN}:{path}")
            binary = path.endswith(".png")
            canonical_current = raw if binary else raw.replace(b"\r\n", b"\n")
            canonical_pin = pinned if binary else pinned.replace(b"\r\n", b"\n")
            assert canonical_current == canonical_pin, f"source drift: {path}"
            files[path] = {
                "path": path,
                "primary": path in packet["paths"],
                "inventory_original_sha256": unit["source_sha256"],
                "current_raw_sha256": sha(raw),
                "pinned_git_blob": git("rev-parse", f"{PIN}:{path}").decode().strip(),
                "pinned_git_bytes_sha256": sha(pinned),
                "current_lf_sha256": sha(canonical_current),
                "source_equal_to_pin_with_lf_normalization": True,
                "raw_inventory_match": sha(raw) == unit["source_sha256"],
                "read_scope": "complete file" if path in packet["paths"] else "assigned complete shared ranges plus necessary neighboring context",
                "evidence": "file-evidence.md",
            }
        raw = (REPO / path).read_bytes()
        # Inventory gives binary diagrams lossy text coordinates too. Reproduce that
        # administrative hash; the raw SHA and inspected image are the actual identity.
        current_range = raw.decode("utf-8-sig", errors="replace")[unit["start_character"]:unit["end_character_exclusive"]].encode()
        assert sha(raw) == unit["source_sha256"], f"inventory raw drift: {path}"
        assert sha(current_range) == unit["range_sha256"], f"range drift: {unit['id']}"
        associated = sorted({cid for cid, p, a, b in spans if p == path and unit["first_line"] <= b and unit["last_line"] >= a})
        exclusion = path == guide and unit["label"] == "## Platform notes"
        historical = path.startswith("docs/plans/") or path.startswith("2026-09-21-")
        status = "READ-EXCLUSION" if exclusion else "READ-CANDIDATE" if associated else "READ-CLEAN"
        row = {
            "unit_id": unit["id"],
            "path": path,
            "label": unit["label"],
            "first_line": unit["first_line"],
            "last_line": unit["last_line"],
            "start_character": unit["start_character"],
            "end_character_exclusive": unit["end_character_exclusive"],
            "ownership": "B1-primary" if unit["id"] in primary else "B1-shared-reader; future editor SHARED",
            "read_status": "READ-COMPLETE",
            "disposition": status,
            "candidate_ids": associated,
            "original_source_sha256": unit["source_sha256"],
            "current_source_sha256": sha(raw),
            "original_range_sha256": unit["range_sha256"],
            "current_range_sha256": sha(current_range),
            "range_identity_kind": "lossy inventory coordinates; raw SHA is authoritative" if path.endswith(".png") else "UTF-8 source character range",
            "evidence": "file-evidence.md" if unit["id"] in primary else "file-evidence.md#shared-units-and-narrow-dependencies",
            "evidence_scope": "historical proposal, not current contract" if historical else "source and exact assertion scope; no SDK execution",
            "documentation_state": "candidate-triage-pending" if associated else "read-clean-in-scoped-context",
            "runtime_state": "unchanged; no fix or acceptance implied",
            "validation_state": "VALID-NOT-CHECKED",
            "platform_hardware_state": "NOT-RUN",
            "independent_review": "PENDING",
            "sampling": "INELIGIBLE" if associated or exclusion or historical or "/images/" in path else "ELIGIBLE-CLEAN-SCOPED",
        }
        if exclusion:
            row["exclusion"] = "Read complete platform-notes unit. PC/SC no-fallback checked here; macOS seize and Windows access/elevation claims require B3/B4 native source qualification. No platform-wide clean disposition."
            row["documentation_state"] = "read; named platform claims deferred to B3/B4"
        rows.append(row)

    summary = {
        "baseline": PIN,
        "observed_head": git("rev-parse", "HEAD").decode().strip(),
        "primary_files": len(packet["paths"]),
        "primary_lines": packet["lines"],
        "primary_units": len(primary),
        "shared_units": len(selected) - len(primary),
        "total_units": len(rows),
        "statuses": dict(Counter(r["disposition"] for r in rows)),
        "candidate_groups": len({s[0] for s in spans}),
        "raw_inventory_matches": all(f["raw_inventory_match"] for f in files.values()),
        "all_source_equal_to_pin_with_lf_normalization": True,
        "new_build_test_formatter_native_device_runs": 0,
        "central_records_modified": False,
        "scope": "source/assertion investigation and B1-owned records only",
    }
    for name, value in [("unit-dispositions.json", rows), ("file-identities.json", list(files.values())), ("coverage-summary.json", summary)]:
        (REPO / OUT / name).write_text(json.dumps(value, indent=2) + "\n")
    print(json.dumps(summary, indent=2))


if __name__ == "__main__":
    main()
