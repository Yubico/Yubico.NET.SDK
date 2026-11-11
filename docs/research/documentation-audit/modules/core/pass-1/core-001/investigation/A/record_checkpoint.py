"""Materialize A's explicit semantic checkpoint, without changing central coverage.

Reading decisions are supplied here by the investigator, not inferred from hashes or keywords.
Hashes merely bind those decisions to source. Unfinished shared reads stay UNREAD.
"""
import collections
import hashlib
import json
from pathlib import Path
import subprocess

HERE = Path(__file__).resolve().parent
RUN = HERE.parent.parent
BASELINE = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"
PACKET = RUN / "inventory/packets/A.json"
SOURCE = "src/Core/src/"
UNIT = "src/Core/tests/Yubico.YubiKit.Core.UnitTests/"
INTEGRATION = "src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/"

# These files were read in full, including implementation and assertion/helper context.
# This list does not expand disposition ownership beyond the packet's selected IDs.
SHARED_READ = {
    "src/Core/README.md", "src/Core/CLAUDE.md", "docs/TESTING.md",
    "docs/usage/device-discovery.md", "docs/architecture/device-identity.md",
    "docs/architecture/event-driven-device-discovery.md",
    "docs/architecture/physical-device-model.md", "docs/architecture/device-discovery-guarantees.md",
    "src/Tests.Shared/CLAUDE.md", "src/Tests.Shared/README.md",
    "src/Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs",
    "src/Tests.Shared/Infrastructure/FilterCriteria.cs",
    "src/Tests.Shared/Infrastructure/AuthorizedDevices.cs",
    "src/Tests.Shared/Infrastructure/AllowList.cs",
    "src/Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs",
    "src/Tests.Shared/Infrastructure/WithYubiKeyTraitDiscoverer.cs",
    "src/Tests.Shared/Infrastructure/TransientScanRetry.cs",
    "src/Tests.Shared/IYubiKeyFilter.cs", "src/Tests.Shared/ModernFirmwareFilter.cs",
    "src/Tests.Shared/YubiKeyTestState.cs", "src/Cli.Shared/src/Device/DeviceSelector.cs",
    "src/Cli.Shared/src/Device/DeviceSelection.cs",
    "src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Device/DeviceSelectorTests.cs",
    # Derived pair: full PNG visual inspection and SVG text/edge-label extraction, not path-data prose.
    "docs/architecture/images/L4-discovery.svg", "docs/architecture/images/png/L4-discovery.png",
}

# Complete assigned sections/methods were read in order with their enclosing ownership context.
# This is distinct from claiming the unrelated remainder of each external file was audited.
SHARED_RANGE_READ = set("""TOOLCHAIN.md
docs/architecture/connection-ownership-and-contention.md
docs/architecture/sdk-architecture-diagrams.md
docs/architecture/sdk-architecture-map.yml
docs/linux-setup.md
docs/migration/v1-to-v2-changelog.md
docs/migration/v1-to-v2-gaps.md
docs/migration/v1-to-v2-map.yml
docs/migration/v1-to-v2.md
docs/plans/yubikit-async-boundaries/02-architecture.md
docs/research/linux-hid-routing.md
docs/research/native-aot-readiness-data.md
docs/research/native-aot-readiness.md
docs/v1-to-v2-comparison.md
docs/v2-highlights.md
scripts/toolchain/BuildTargets.cs
scripts/toolchain/Context.cs
scripts/toolchain/Conventions.cs
scripts/toolchain/Discovery.cs
scripts/toolchain/Options.cs
scripts/toolchain/TestTargets.cs
src/Cli.Commands/src/Infrastructure/YkCommandBase.cs
src/Cli.Commands/src/Infrastructure/YkDeviceContext.cs
src/Cli.Commands/src/Management/ManagementConfigCommand.cs
src/Cli.Commands/src/Management/ManagementInfoCommand.cs
src/Cli.Commands/src/Management/ManagementResetCommand.cs
src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/Infrastructure/CommandMetadataTests.cs
src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/Infrastructure/YkDeviceContextTests.cs
src/Cli.Shared/src/Cli/SessionHelper.cs
src/Cli.Shared/src/Device/ConnectionTypeFormatter.cs
src/Cli.Shared/src/Device/FormFactorFormatter.cs
src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Device/ConnectionTypeFormatterTests.cs
src/Core/PACKAGE_README.md
src/Tests.Shared/Infrastructure/TestCategories.cs
src/Cli.Commands/src/Fido/FidoCommands.cs
src/Cli.Commands/src/HsmAuth/HsmAuthCommands.cs
src/Cli.Commands/src/Oath/OathCommands.cs
src/Cli.Commands/src/OpenPgp/OpenPgpAccessCommands.cs
src/Cli.Commands/src/OpenPgp/OpenPgpCertificatesCommands.cs
src/Cli.Commands/src/OpenPgp/OpenPgpInfoCommand.cs
src/Cli.Commands/src/OpenPgp/OpenPgpKeysCommands.cs
src/Cli.Commands/src/OpenPgp/OpenPgpResetCommand.cs
src/Cli.Commands/src/Otp/OtpCommands.cs
src/Cli.Commands/src/Piv/PivCommands.cs
src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/FactoryShapeTests.cs
src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/PublicReturnContractScanner.cs
src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/AppletSessionShapeTests.cs""".splitlines())


def at(path, first, last):
    return {"path": path, "first_line": first, "last_line": last}


def finding(number, title, kind, citations, oracles, reopen, trigger, aliases=()):
    return {"id": f"CORE-A-C{number:03}", "status": "candidate",
            "accepted_finding_id": None, "title": title, "classification": kind,
            "citations": citations, "test_oracle_paths": oracles,
            "reopening_paths": reopen, "exact_reopening_trigger": trigger,
            "inherited_aliases": list(aliases), "counting": "nonadditive where inherited aliases identify the same question",
            "documentation_state": "proposed; no product edits; fresh editor/reviewer required",
            "implementation_state": "unchanged; no runtime closure implied",
            "execution_state": "source/assertion inspection only; no new SDK or physical run",
            "detail": "investigation-A.md#local-candidates-and-reopening-map"}


FINDINGS = [
    finding(1, "Qualifier XML uses signed rather than unsigned upper limit", ["documentation"],
            [at(SOURCE + "Devices/VersionQualifier.cs", 35, 60)],
            [UNIT + "Devices/DeviceInfoFieldLengthTests.cs"], [SOURCE + "Devices/VersionQualifier.cs"],
            "Iteration numeric range, public parameter representation, or wire iteration interpretation changes."),
    finding(2, "Public metadata/default/unit/flag contracts need reference coverage", ["documentation"],
            [at(SOURCE + "Devices/DeviceInfo.cs", 21, 69), at(SOURCE + "Devices/ConnectionType.cs", 17, 26),
             at(SOURCE + "Transport.cs", 17, 24), at(SOURCE + "Devices/Feature.cs", 17, 42),
             at(SOURCE + "Devices/DeviceCapabilities.cs", 57, 61)],
            [UNIT + "Devices/DeviceInfoFieldLengthTests.cs", UNIT + "Devices/PhysicalYubiKeyTests.cs"],
            [SOURCE + "Devices/DeviceInfo.cs", "src/Core/README.md", "src/Core/CLAUDE.md",
             "docs/usage/device-discovery.md", "src/Management/src/IManagementSession.cs"],
            "Any decoded field, absence default, units, flag interpretation, or capability carrier changes."),
    finding(3, "DeviceInfo timeout aliases disposable TLV memory", ["implementation", "ownership-decision", "documentation"],
            [at(SOURCE + "Devices/DeviceInfo.cs", 129, 199), at(SOURCE + "Devices/DeviceInfo.cs", 71, 126),
             at(SOURCE + "Abstractions/IYubiKey.cs", 121, 125)],
            [UNIT + "Devices/DeviceInfoTests.cs", UNIT + "Devices/YubiKeyDeviceTests.cs"],
            [SOURCE + "Devices/DeviceInfo.cs", SOURCE + "Devices/DeviceInfoReader.cs",
             SOURCE + "Abstractions/IYubiKey.cs", "src/Core/README.md", "docs/usage/device-discovery.md",
             "docs/architecture/physical-device-model.md"],
            "Decoded timeout becomes independently owned/scalar, input disposal behavior changes, or reader TLV cleanup is introduced."),
    finding(4, "FIPS tags use ordinary application mapping", ["implementation", "source-gap"],
            [at(SOURCE + "Devices/DeviceInfo.cs", 148, 154), at(SOURCE + "Devices/DeviceInfo.cs", 232, 240),
             at(SOURCE + "Devices/DeviceInfo.cs", 327, 360), at(UNIT + "Devices/DeviceInfoTests.cs", 113, 130)],
            [UNIT + "Devices/CapabilityMapperTests.cs", UNIT + "Devices/DeviceInfoTests.cs"],
            [SOURCE + "Devices/DeviceInfo.cs", "src/Tests.Shared/YubiKeyTestState.cs",
             "src/Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs", "src/Management/README.md"],
            "Authoritative FIPS tag profile or mapping/width fix plus integrated nonzero tag vectors becomes available.",
            ["management-001:MGMT-XREF-001", "audit-v2-20261022-20261031:deviceinfo-FIPS-residual"]),
    finding(5, "Flagged form factors differ from base-enum filters", ["implementation", "representation-decision"],
            [at(SOURCE + "Devices/DeviceInfo.cs", 142, 146), at(SOURCE + "Devices/FormFactor.cs", 17, 58)],
            [UNIT + "Devices/DeviceInfoTests.cs"],
            [SOURCE + "Devices/FormFactor.cs", SOURCE + "Devices/DeviceInfo.cs",
             "src/Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs",
             "src/Cli.Shared/src/Device/FormFactorFormatter.cs", "src/Management/README.md"],
            "Normalized/raw form-factor design is decided and flagged/unknown vectors verify it.",
            ["management-001:MGMT-XREF-002"]),
    finding(6, "Variable-width application bits truncate and signed-extend", ["implementation", "numeric-width-decision"],
            [at(SOURCE + "Devices/DeviceInfo.cs", 232, 240), at(SOURCE + "Devices/DeviceInfo.cs", 353, 360),
             at(UNIT + "Devices/DeviceInfoFieldLengthTests.cs", 254, 265)],
            [UNIT + "Devices/CapabilityMapperTests.cs", UNIT + "Devices/DeviceInfoFieldLengthTests.cs"],
            [SOURCE + "Devices/DeviceInfo.cs", SOURCE + "Devices/DeviceCapabilities.cs",
             "src/Tests.Shared/YubiKeyTestState.cs", "src/Management/README.md"],
            "Supported unsigned width/raw preservation is decided and nonzero trailing/high-bit tests cover it.",
            ["audit-v2-20261022-20261031:deviceinfo-wide-app-bits"]),
    finding(7, "IYubiKey XML retains old no-event/repository wording", ["documentation"],
            [at(SOURCE + "Abstractions/IYubiKey.cs", 49, 52), at(SOURCE + "Abstractions/IYubiKey.cs", 70, 74),
             at(SOURCE + "Abstractions/IYubiKey.cs", 90, 97), at(SOURCE + "Abstractions/IYubiKey.cs", 121, 125),
             at(SOURCE + "Abstractions/IYubiKey.cs", 155, 158), at("docs/architecture/physical-device-model.md", 37, 39)],
            [UNIT + "Devices/DeviceIdentityContractTests.cs", UNIT + "Devices/SnapshotDiffTests.cs"],
            [SOURCE + "Abstractions/IYubiKey.cs", "docs/architecture/physical-device-model.md"],
            "Monitor retention, serial latch, metadata update notification, or interface-group semantics change."),
    finding(8, "Internal read comments overstate budget/retry and old ingress behavior", ["documentation"],
            [at(SOURCE + "Devices/DiscoveryBudgets.cs", 29, 30), at(SOURCE + "Devices/DiscoveryIdentityReader.cs", 23, 32),
             at(SOURCE + "Devices/SharedDeviceInfoRead.cs", 145, 151), at(SOURCE + "Devices/DiscoveryReadScope.cs", 19, 22),
             at(SOURCE + "Devices/ProtocolDeviceInfo.cs", 35, 38),
             at(UNIT + "Devices/DiscoveryIdentityReaderTests.cs", 62, 67),
             at(UNIT + "Devices/FindYubiKeysFaultInjectionTests.cs", 252, 254)],
            [UNIT + "Devices/DiscoverySingleFlightTests.cs", UNIT + "Devices/DiscoveryReadSchedulingTests.cs",
             UNIT + "Devices/FindYubiKeysScopeTests.cs", UNIT + "Infrastructure/TransientScanRetryTests.cs"],
            [SOURCE + "Devices/DiscoveryBudgets.cs", SOURCE + "Devices/DiscoveryIdentityReader.cs",
             SOURCE + "Devices/SharedDeviceInfoRead.cs", SOURCE + "Devices/DiscoveryReadScope.cs",
             SOURCE + "Devices/ProtocolDeviceInfo.cs", "docs/architecture/event-driven-device-discovery.md"],
            "Admission, retry classification, invalidation ordering, native transfer, or wait budgets change."),
    finding(9, "Topology fallback is not a latency or universal OS-impossibility proof", ["documentation", "qualification"],
            [at(SOURCE + "Devices/FindYubiKeys.cs", 318, 321),
             at(SOURCE + "Devices/IDeviceTopologyResolver.cs", 24, 35),
             at(UNIT + "Devices/WindowsDeviceTopologyResolverTests.cs", 212, 221),
             at("docs/architecture/device-discovery-guarantees.md", 398, 413)],
            [UNIT + "Devices/WindowsDeviceTopologyResolverTests.cs"],
            [SOURCE + "Devices/IDeviceTopologyResolver.cs", SOURCE + "Devices/FindYubiKeys.cs",
             "docs/architecture/device-discovery-guarantees.md", "docs/usage/device-discovery.md"],
            "Topology implementation/API support, synchronous execution boundary, RDP behavior, or exact Windows native/hardware evidence changes."),
    finding(10, "Merge test prose retains completed TODO and overstates assertions", ["documentation"],
            [at(UNIT + "Devices/CompositeDeviceMergerTests.cs", 67, 67),
             at(UNIT + "Devices/CompositeDeviceMergerVectorTests.cs", 25, 41),
             at(UNIT + "Devices/CompositeDeviceMergerVectorTests.cs", 338, 347),
             at(UNIT + "Devices/CompositeDeviceMergerVectorTests.cs", 469, 474),
             at(UNIT + "Devices/CompositeDeviceMergerVectorTests.cs", 522, 527),
             at(UNIT + "Devices/CompositeDeviceMergerVectorTests.cs", 629, 632),
             at(UNIT + "Devices/CompositeDeviceMergerVectorTests.cs", 768, 783)],
            [UNIT + "Devices/CompositeDeviceMergerTests.cs", UNIT + "Devices/CompositeDeviceMergerVectorTests.cs"],
            [UNIT + "Devices/CompositeDeviceMergerTests.cs", UNIT + "Devices/CompositeDeviceMergerVectorTests.cs",
             "docs/architecture/device-discovery-guarantees.md"],
            "Merge tier rules, contestant heuristics, conservation/determinism oracle, or actual topology vector changes."),
    finding(11, "Global no-wrong-grouping assurance conflicts with accepted partial bound", ["documentation"],
            [at("docs/architecture/device-discovery-guarantees.md", 36, 37),
             at("docs/architecture/device-discovery-guarantees.md", 372, 375),
             at("docs/architecture/physical-device-model.md", 3, 8)],
            [UNIT + "Devices/CompositeDeviceMergerVectorTests.cs"],
            ["docs/architecture/device-discovery-guarantees.md", "docs/architecture/physical-device-model.md",
             "docs/usage/device-discovery.md"],
            "Grouping, slot-to-serial binding, or post-merge identity-verification policy changes."),
    finding(12, "Hardware descriptions exceed integration-test oracles", ["documentation", "test-adequacy", "qualification"],
            [at(INTEGRATION + "Devices/CompositeDiscoveryIntegrationTests.cs", 24, 37),
             at(INTEGRATION + "Devices/CompositeDiscoveryIntegrationTests.cs", 318, 320),
             at(INTEGRATION + "Devices/DiscoveryUnattendedTests.cs", 21, 28),
             at(INTEGRATION + "Devices/YubiKeyTests.cs", 19, 22),
             at(INTEGRATION + "Devices/YubiKeyTests.cs", 58, 60),
             at(UNIT + "Devices/DeviceInfoReaderTests.cs", 27, 52),
             at("docs/architecture/device-discovery-guarantees.md", 343, 346)],
            [INTEGRATION + "Devices/CompositeDiscoveryIntegrationTests.cs", INTEGRATION + "Devices/DiscoveryUnattendedTests.cs",
             INTEGRATION + "Devices/DeviceEventHotPlugTests.cs", INTEGRATION + "Devices/HotPlugIdentityContractTests.cs"],
            [INTEGRATION + "Devices/CompositeDiscoveryIntegrationTests.cs", INTEGRATION + "Devices/DiscoveryUnattendedTests.cs",
             INTEGRATION + "Devices/YubiKeyTests.cs", "docs/architecture/device-discovery-guarantees.md",
             "docs/architecture/device-identity.md", "docs/usage/device-discovery.md"],
            "Hardware harness/oracles change or a new exact source/OS/device/firmware/transport execution record is accepted."),
    finding(13, "Shared authorization and placeholder prose differ from source", ["documentation"],
            [at("src/Tests.Shared/CLAUDE.md", 73, 109), at("src/Tests.Shared/IYubiKeyFilter.cs", 17, 50),
             at("src/Tests.Shared/YubiKeyTestState.cs", 46, 54),
             at("src/Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs", 175, 308)],
            [], ["src/Tests.Shared/CLAUDE.md", "src/Tests.Shared/README.md", "src/Tests.Shared/IYubiKeyFilter.cs",
                 "src/Tests.Shared/YubiKeyTestState.cs"],
            "Fixture initialization, authorization order, live-versus-cached metadata, placeholder binding, or no-authorized-device policy changes.",
            ["management-001:MGMT-XREF-003"]),
    finding(14, "Malformed firmware filters remain ignored pending inherited decision", ["decision"],
            [at("src/Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs", 78, 84)],
            [], ["src/Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs", "src/Tests.Shared/README.md",
                 "src/Management/tests/CLAUDE.md"],
            "Strict malformed-filter policy is accepted or the Management caller's two-component criterion changes.",
            ["management-001:MGMT-IMP-009"]),
    finding(15, "Development-version compatibility policy is stated as hardware fact", ["documentation"],
            [at(SOURCE + "Devices/FirmwareVersion.cs", 46, 76)],
            [UNIT + "Devices/FeatureTests.cs", UNIT + "Devices/DeviceInfoReaderTests.cs"],
            [SOURCE + "Devices/FirmwareVersion.cs", SOURCE + "Devices/Feature.cs", SOURCE + "Devices/DeviceInfo.cs",
             "src/Core/README.md", "src/Core/CLAUDE.md"],
            "Major-zero optimism, default version semantics, feature checks, or effective qualifier precedence changes."),
    finding(16, "Raw/unknown metadata preservation and duplicate-tag error contract need a decision", ["representation-decision", "documentation"],
            [at(SOURCE + "Devices/DeviceInfo.cs", 129, 137), at(SOURCE + "Devices/DeviceInfo.cs", 274, 289)],
            [UNIT + "Devices/DeviceInfoFieldLengthTests.cs"],
            [SOURCE + "Devices/DeviceInfo.cs", "src/Management/src/IManagementSession.cs", "docs/usage/device-discovery.md"],
            "A raw-response escape hatch, unknown-field/qualifier policy, duplicate-tag handling, or metadata equality changes."),
    finding(17, "Metadata cache lacks identity cache's PID qualification", ["implementation", "documentation"],
            [at(SOURCE + "Devices/FindYubiKeys.cs", 423, 467), at(SOURCE + "Devices/FindYubiKeys.cs", 476, 485),
             at(SOURCE + "Devices/FindYubiKeys.cs", 538, 539),
             at("docs/architecture/physical-device-model.md", 110, 114)],
            [UNIT + "Devices/FindYubiKeysFaultInjectionTests.cs"],
            [SOURCE + "Devices/FindYubiKeys.cs", "docs/architecture/physical-device-model.md",
             "docs/usage/device-discovery.md"],
            "Metadata PID validity is decided or a stable-ID/PID-change metadata vector is added; retain hotplug/absence controls."),
    finding(18, "Toolchain filter comments promise an AND combination absent from the translator", ["documentation", "implementation-candidate", "version-source-gap"],
            [at("scripts/toolchain/TestTargets.cs", 156, 257)], [],
            ["scripts/toolchain/TestTargets.cs", "TOOLCHAIN.md", "docs/TESTING.md"],
            "Installed xUnit 3.0.0 filtering semantics are pinned and a pure translator/runner-list experiment verifies repeated same-kind AND inputs."),
    finding(19, "Shared project-discovery and category documentation retain stale scope claims", ["documentation", "boundary-qualification"],
            [at("TOOLCHAIN.md", 246, 254), at("src/Tests.Shared/Infrastructure/TestCategories.cs", 83, 90)], [],
            ["TOOLCHAIN.md", "src/Tests.Shared/Infrastructure/TestCategories.cs"],
            "Project discovery layout changes or the owning CTAP boundary establishes the precise applet-reset and power-cycle policy."),
    finding(20, "Canonical comparator and historical platform claims need exact provenance", ["qualification", "source-gap"],
            [at("docs/architecture/device-identity.md", 29, 58),
             at("docs/research/native-aot-readiness-data.md", 255, 297),
             at("docs/research/native-aot-readiness-data.md", 376, 443)], [],
            ["docs/architecture/device-identity.md", "docs/architecture/device-discovery-guarantees.md",
             "docs/research/native-aot-readiness-data.md", "docs/research/native-aot-readiness.md"],
            "Original Rust repository/7d7a7455 identity and passages or exact historical host/OS/device/run artifacts become available; current-source claims change."),
    finding(21, "Successful serial-less identities are cached despite retry prose", ["documentation", "cache-policy-decision"],
            [at(SOURCE + "Devices/FindYubiKeys.cs", 84, 86), at(SOURCE + "Devices/FindYubiKeys.cs", 364, 383),
             at("docs/architecture/device-discovery-guarantees.md", 39, 48)],
            [UNIT + "Devices/FindYubiKeysFaultInjectionTests.cs"],
            [SOURCE + "Devices/FindYubiKeys.cs", SOURCE + "Abstractions/IYubiKey.cs",
             "docs/architecture/device-discovery-guarantees.md", "docs/usage/device-discovery.md"],
            "Successful serial-less identity-cache expiry is decided and an omitted-serial success vector with invalidation controls verifies it."),
]


def sha(data):
    return hashlib.sha256(data).hexdigest()


def git(*args):
    return subprocess.check_output(["git", *args])


def note(path):
    name = Path(path).stem
    if path.startswith(INTEGRATION):
        return (f"Full {name} assertions and helper scopes read. Hardware cases were not executed; "
                "readiness/count/retention/pump assertions have only their source-defined scope. See C012 and test-adequacy narrative.")
    if path.startswith(UNIT):
        return (f"Full {name} unit/test implementation and local helper context read against the production "
                "discovery/metadata/feed code. Unchanged assertions remain managed/fake evidence only; "
                "no execution, universal schedule, native release, or platform guarantee is inferred.")
    if name in {"DeviceInfo", "VersionQualifier", "FirmwareVersion", "Feature", "FormFactor", "DeviceFlags", "DeviceCapabilities"}:
        return (f"Full {name} declaration/implementation read: field values, defaults, widths, value comparison, "
                "effective-version/flag interpretation considered as applicable. Reported candidates are separate from fixed-width source resolution.")
    if name in {"YubiKeyMonitor", "DeviceChangeFeed", "DeviceChange", "SnapshotDiff", "HotplugSignals", "RescanScheduler", "IMonitoredDiscovery"}:
        return (f"Full {name} lifetime/state/publication code read against feed, monitor, scheduler, and watcher "
                "assertions: lazy subscription, replay, loaded-empty, scan revision, isolated overflow, retries, and teardown considered.")
    if "Discovery" in name or name in {"FindYubiKeys", "SharedDeviceInfoRead", "CompositeMetadataReader", "ProtocolDeviceInfo"}:
        return (f"Full {name} source read: immutable scope/result filtering, admission/worker/caller wait, "
                "retirement, borrowed protocol/connection cleanup, cache epoch/PID/absence, and late completion considered as applicable.")
    if "Topology" in name:
        return (f"Full {name} source read against scripted resolver assertions and retained Windows API "
                "passages. Optional native fallback is separate from native latency and Windows hardware qualification.")
    if name in {"CompositeDeviceMerger", "YubiKeyDevice", "IYubiKey", "DeviceCorrelation", "IYubiKeyConnectionSlot", "IDiscoveryConnectionProvider"}:
        return (f"Full {name} contracts and implementation read: member-set identity, conservative grouping, "
                "metadata/serial retention, typed routing, claim transfer, default implementations, and caller ownership considered as applicable.")
    return (f"Full {name} declaration/content read against current call sites and applicable mapping tests. "
            "No additional correction identified outside explicitly cited candidate ranges.")


if __name__ == "__main__":
    packet = json.loads(PACKET.read_text())
    units = {u["id"]: u for u in json.loads((RUN / "inventory/semantic-units.json").read_text())}
    primary = set(packet["unit_ids"])
    shared = set(packet["shared_read_unit_ids"])
    assert not primary & shared
    assert len(primary) == 1630 and len(shared) == 376
    assert len(packet["paths"]) == 91
    assert sum(len(Path(path).read_bytes().decode("utf-8-sig").splitlines()) for path in packet["paths"]) == 16728
    selected = primary | shared
    assert selected <= units.keys()
    files = {}
    for path in sorted({units[key]["path"] for key in selected}):
        raw = Path(path).read_bytes()
        original = git("show", f"{BASELINE}:{path}")
        normalized = raw.replace(b"\r\n", b"\n")
        original_normalized = original.replace(b"\r\n", b"\n")
        assert normalized == original_normalized, f"Source drift: {path}"
        files[path] = {"original_path": path, "current_path": path,
                       "original_git_blob": git("rev-parse", f"{BASELINE}:{path}").decode().strip(),
                       "original_blob_sha256": sha(original), "current_raw_sha256": sha(raw),
                       "normalized_source_matches_baseline": True}
    results = {}
    for key in sorted(selected):
        u = units[key]
        path = u["path"]
        read = key in primary or path in SHARED_READ or path in SHARED_RANGE_READ
        affected = [f["id"] for f in FINDINGS if any(
            c["path"] == path and c["first_line"] <= u["last_line"] and c["last_line"] >= u["first_line"]
            for c in f["citations"])]
        results[key] = {"path": path, "first_line": u["first_line"], "last_line": u["last_line"],
                        "start_character": u["start_character"], "end_character_exclusive": u["end_character_exclusive"],
                        "label": u["label"], "range_sha256": u["range_sha256"],
                        "inventory_source_sha256": u["source_sha256"], "ownership": "primary" if key in primary else "assigned-shared-range",
                        "read": read, "status": "CANDIDATE" if read and affected else "READ-CLEAN" if read else "UNREAD",
                        "candidate_ids": affected, "exclusion_justification": None,
                        "rationale": note(path) if key in primary else
                            "Complete assigned range and enclosing ownership/helper context read; unrelated applet behavior remains with its boundary owner." if read else
                            "Shared remainder has not received a complete semantic read. Preserve UNREAD; hash/keyword inspection is not coverage.",
                        "review_status": "FRESH-INDEPENDENT-REVIEW-PENDING",
                        "sample_status": "ELIGIBLE-AFTER-DEPENDENCY-RECONCILIATION" if read and not affected else "INELIGIBLE",
                        "file_identity_key": path}
    dispositions = {}
    for path in packet["disposition_paths"]:
        related = [f["id"] for f in FINDINGS if path in f["test_oracle_paths"] or path in f["reopening_paths"] or
                   any(c["path"] == path for c in f["citations"])]
        dispositions[path] = {"read": True, "product_edit": False, "candidate_ids": related,
                              "disposition": "UNCHANGED-CANDIDATE-ASSOCIATED" if related else "READ-CLEAN-NO-EDIT-PROPOSED",
                              "reason": note(path) + (" Keep implementation/assertions unchanged in this investigation; "
                                                       "fresh editor must explicitly decide each associated unchanged path before final compilation."
                                                       if related else " Retain current implementation and explanation unless later boundary evidence reopens it.")}
    summary = {"schema": "core-001-A-investigation-checkpoint-v1", "source_baseline": BASELINE,
               "packet_sha256": sha(PACKET.read_bytes()), "observed_head": git("rev-parse", "HEAD").decode().strip(),
               "state": "INVESTIGATION-READ-COMPLETE; TRIAGE-AND-INDEPENDENT-REVIEW-PENDING",
               "primary_files": len(packet["paths"]), "primary_lines": packet["lines"],
               "primary_units": len(primary), "shared_units": len(shared),
               "primary_status_counts": dict(collections.Counter(results[k]["status"] for k in primary)),
               "shared_status_counts": dict(collections.Counter(results[k]["status"] for k in shared)),
               "local_candidates": len(FINDINGS), "accepted_findings": 0,
               "no_sdk_build_test_formatter_native_device_execution": True,
               "compiler_shape": "VALID-NOT-CHECKED", "independent_review": "PENDING",
               "central_records_modified": False, "new_subagents": 0}
    assert all(result["read"] for result in results.values()), "Complete the assigned read before emitting the final investigation state."
    shared_dispositions = {}
    for path in sorted({units[k]["path"] for k in shared}):
        owned = {k: results[k] for k in shared if units[k]["path"] == path}
        remaining = sorted(k for k, u in owned.items() if not u["read"])
        related = [f["id"] for f in FINDINGS if path in f["reopening_paths"] or path in f["test_oracle_paths"] or
                   any(c["path"] == path for c in f["citations"])]
        shared_dispositions[path] = {"scope": "Only A's exact shared IDs; no whole-file disposition outside them.",
                                     "unit_ids": sorted(owned), "unread_unit_ids": remaining,
                                     "status": "UNREAD" if remaining else "ASSIGNED-RANGES-READ",
                                     "candidate_ids": related, "product_edit": False,
                                     "future_editor_owner": "SHARED",
                                     "unchanged_disposition": "No edit in investigation; fresh shared editor must reconcile candidate-associated ranges before final compilation."}
    nominees = []
    for key in sorted(primary):
        result = results[key]
        if result["status"] == "READ-CLEAN" and any(text in result["label"] for text in [
                "ContinuousSignals_YieldsAtCoalesceCap", "Overflow_FaultsOnlySlowSubscriberAndAllowsReplayRecovery",
                "DifferentKnownSerials_RepublishesWithoutMutatingPredecessor",
                "ExitRetiresQueuedIdentityReadsBeforeLaterWorkerAdmission"]):
            nominees.append({"unit_id": key, "path": result["path"], "label": result["label"],
                             "source_sha256": result["inventory_source_sha256"],
                             "review_scope": "Read full test and complete local/fixture helper context; source-only evidence, no prior execution credit.",
                             "sample_state": "NOMINATED; COORDINATOR HAS NOT SAMPLED"})
    for filename, value in [("unit-results.json", {"summary": summary, "files": files, "units": results}),
                            ("findings.json", FINDINGS), ("file-dispositions.json", dispositions),
                            ("shared-dispositions.json", shared_dispositions), ("sampling-nominees.json", nominees),
                            ("remaining-shared.json", [results[k] | {"id": k} for k in sorted(shared) if not results[k]["read"]]),
                            ("checkpoint-verification.json", summary)]:
        (HERE / filename).write_text(json.dumps(value, indent=2) + "\n")
    print(json.dumps(summary, indent=2))
