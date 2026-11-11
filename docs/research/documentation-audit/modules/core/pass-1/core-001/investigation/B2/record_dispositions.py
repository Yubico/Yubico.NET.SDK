"""Serialize completed manual reads and verify identities; never execute product/tests."""
import collections
import hashlib
import json
from pathlib import Path
import subprocess

HERE = Path(__file__).parent
RUN = HERE.parents[1]
PIN = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"

def sha(data):
    return hashlib.sha256(data).hexdigest()

def save(name, value):
    (HERE / name).write_text(json.dumps(value, indent=2) + "\n")

packet = json.loads((RUN / "inventory/packets/B2-linux-lifetimes.json").read_text())
ledger = json.loads((RUN / "inventory/semantic-units.json").read_text())
notes = json.loads((HERE / "file-notes.json").read_text())
assert len(packet["paths"]) == len(notes) == 63
assert set(notes) == {Path(p).name for p in packet["paths"]}
primary = set(packet["unit_ids"])
shared = set(packet["shared_read_unit_ids"])
assert not primary & shared
assert len(primary) == 1148 and len(shared) == 71

# These intervals were selected from the complete manual source/assertion reads.
# Range intersection attaches all affected existing units; it is not a semantic audit algorithm.
candidate_intervals = {
    "docs/linux-setup.md": [(40, 103, "CORE-B2-C001"), (1, 39, "CORE-B2-C004"), (106, 117, "CORE-B2-C001"), (106, 113, "CORE-B2-C004")],
    "src/Core/src/Native/Linux/Libc/LinuxDescriptorLease.cs": [(19, 21, "CORE-B2-C002")],
    "src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs": [(192, 193, "CORE-B2-C002")],
    "docs/research/linux-hid-routing.md": [(37, 67, "CORE-B2-C002"), (95, 105, "CORE-B2-C002")],
    "src/Core/src/Native/Linux/Udev/Udev.Interop.cs": [(123, 129, "CORE-B2-C003"), (164, 171, "CORE-B2-C003"), (192, 199, "CORE-B2-C003"), (238, 246, "CORE-B2-C003")],
    "src/Core/src/Native/Linux/Linux.Libraries.cs": [(20, 20, "CORE-B2-C004")],
    "src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxFidoHidConnectionTests.cs": [(20, 70, "CORE-B2-C005"), (92, 117, "CORE-B2-C005")],
    "src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/LinuxHidRoutingTests.cs": [(149, 190, "CORE-B2-C005")],
    "src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxHidReadReactorTests.cs": [(37, 80, "CORE-B2-C005")],
    "src/Core/src/Transports/Hid/Linux/LinuxHidInterface.cs": [(126, 142, "CORE-B2-C006"), (325, 367, "CORE-B2-C006")],
}
shared_notes = {
    "linux-setup.md": "Read full configuration journey and nearby PC/SC, hidraw/group and troubleshooting conditions; permission and loader-prerequisite candidates remain separate from any executed deployment.",
    "NATIVE-AOT.md": "Read complete Linux native-contract section against actual host/publish/gate source: native-only real descriptors with synthetic FIDO64 differ from serial-selected physical and attended modes.",
    "00-status.md": "Read exact historical Linux stage checkpoint: L0-L6b statuses/counts belong to its own revision and remain distinct from current release qualification.",
    "linux-async-boundaries-baseline.md": "Read full L0 record, fake helper contract, negative controls and timings. Baseline compatibility-adapter behavior is historical; invocation return, actual operation drain and hardware measurements remain separate.",
    "linux-fido-async-lifetime.md": "Read full L3 record against current owner and complete tests. Historical stage exclusions remain historical; original-worker drain, finite attempts and pending native lifetime are accurately separated from elapsed-time guarantees.",
    "linux-otp-async-lifetime.md": "Read full L4 record against current separate OTP owner and complete tests. Stage routing exclusions are historical, while copied SET, transferred GET, dispatch cancellation and checked release remain current mechanisms.",
    "linux-hid-routing.md": "Read full L5 record against actual slot and current SharedDeviceInfoRead/borrowed ProtocolDeviceInfo split. Exact source owner attribution needs C002; recorded execution and fake scope remain historical.",
    "linux-managed-acceptance.md": "Read full L6a and witnessed synchronous-disposal follow-up. Runnable registry links, successful-path managed assertions, actual witness negative controls and pending native/device lanes are distinct. Historical 24-entry count is not current registry's 23.",
    "linux-native-acceptance-results.json": "Read complete nested/current/historical JSON, not only top-level blocked fields. Latest prPreparationCheckpoint supersedes historical physical/blocked checkpoints within its source/serial envelope; integration native-only capture is separate.",
    "linux-native-acceptance.md": "Read full chronological record and every assigned section. Latest serial 103 six read-only routes passed at recorded d5bb source; old blocked fields are historical. Operator actions, telemetry/budgets, other devices/OS and current merged source execution remain separate.",
    "linux-native-report-access.md": "Read full L1 original-source/report/count/ABI record against actual seam. Slice scope is historical; fd-int, fd0, borrowed spans, metadata versus actual counts and checked close remain current. Exact deployed kernel/libc attestation remains qualified.",
    "linux-read-reactor.md": "Read full L2 token/poller/retirement record against production reactor and complete real/fake tests. Historical integration exclusions remain stage scope; actual quiescence/reference release/device close stay distinct.",
    "CLAUDE.md": "Read full Core instructions with exact assigned Linux L1-L4/L6a sections against production implementation/assertions. Contract vocabulary is useful; section's 23 public-sync entries is current source, not historical L6a24. Cross-platform/native verification stays bounded.",
}

selected = [u for u in ledger if u["id"] in primary | shared]
assert {u["id"] for u in selected} == primary | shared
identities = []
texts = {}
for path in sorted({u["path"] for u in selected}):
    raw = Path(path).read_bytes()
    pinned = subprocess.check_output(["git", "show", f"{PIN}:{path}"])
    normalize = lambda b: b.replace(b"\r\n", b"\n")
    assert normalize(raw) == normalize(pinned), path
    texts[path] = raw.decode("utf-8-sig")
    identities.append({"path": path, "source_sha256": sha(raw), "pinned_git_blob": subprocess.check_output(["git", "rev-parse", f"{PIN}:{path}"], text=True).strip(),
                       "pinned_content_sha256": sha(pinned), "raw_equals_pin": raw == pinned, "lf_equals_pin": True,
                       "read_scope": "complete file" if path in packet["paths"] else "all assigned exact shared units plus enclosing context",
                       "unit_ids": [u["id"] for u in selected if u["path"] == path]})

results = []
for original in selected:
    unit = dict(original)
    text = texts[unit["path"]]
    raw = Path(unit["path"]).read_bytes()
    assert sha(raw) == unit["source_sha256"], unit["path"]
    excerpt = text[unit["start_character"]:unit["end_character_exclusive"]]
    assert sha(excerpt.encode()) == unit["range_sha256"], unit["id"]
    candidates = sorted({candidate for a, b, candidate in candidate_intervals.get(unit["path"], [])
                         if unit["first_line"] <= b and unit["last_line"] >= a})
    excluded = Path(unit["path"]).name == "LinuxUdevScan.cs"
    status = "READ-EXCLUSION" if excluded else "READ-CANDIDATE" if candidates else "READ-CLEAN"
    context = notes[Path(unit["path"]).name] if unit["id"] in primary else shared_notes[Path(unit["path"]).name]
    unit.update(status=status, assignment_kind="primary" if unit["id"] in primary else "shared-read",
                candidate_ids=candidates, read_scope="complete original semantic unit and implementation/assertion/lifetime context",
                disposition_reason=f"{unit['label']}: {context}", exclusion_reason="Complete inactive commented historical scan read and preserved; current enumeration is LinuxHidInterface.GetList." if excluded else None,
                future_editor_owner="SHARED" if unit["id"] in shared else "coordinator-assigned fresh editor",
                current_source_sha256=sha(raw), current_range_sha256=sha(excerpt.encode()), source_baseline=PIN,
                review_status="PENDING-INDEPENDENT-REVIEW", sample_status="ELIGIBLE-CLEAN-NOMINATION" if status == "READ-CLEAN" else "INELIGIBLE",
                documentation_state="candidate-triage-pending" if candidates else "read-disposition-only",
                implementation_state="UNCHANGED", compiler_state="VALID-NOT-CHECKED", execution_state="NO-NEW-EXECUTION")
    results.append(unit)

counts = collections.Counter(u["status"] for u in results)
summary = {"source_baseline": PIN, "observed_record_head": subprocess.check_output(["git", "rev-parse", "HEAD"], text=True).strip(),
           "primary_files": 63, "primary_lines": packet["lines"], "primary_units": len(primary), "shared_units": len(shared),
           "total_units": len(results), "statuses": dict(counts), "unread": 0,
           "primary_statuses": dict(collections.Counter(u["status"] for u in results if u["assignment_kind"] == "primary")),
           "shared_statuses": dict(collections.Counter(u["status"] for u in results if u["assignment_kind"] == "shared-read")),
           "candidate_groups": 6, "accepted_findings": 0, "sdk_builds_tests_native_device_runs": 0,
           "meaning": "Manual semantic reads preceded serialization. Identity checks establish preservation, not semantic audit or implementation acceptance."}
save("unit-dispositions.json", results)
save("file-identities.json", identities)
save("coverage-summary.json", summary)
titles = {
    "CORE-B2-C001": "Lead Linux setup with scoped device access",
    "CORE-B2-C002": "Distinguish stage history from current integration ownership",
    "CORE-B2-C003": "Describe libudev string and unref contracts accurately",
    "CORE-B2-C004": "Unversioned libudev name adds an undeclared runtime prerequisite",
    "CORE-B2-C005": "Assertion-failure test lifetimes need independent drain",
    "CORE-B2-C006": "Expose direct report ownership and parser scope at entry points",
}
associated = {
    "CORE-B2-C001": ["docs/linux-setup.md", "src/Core/README.md", "README.md", "docs/NATIVE-AOT.md", "src/Core/src/Transports/Hid/Linux/LinuxHidInterface.cs", "src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs", "src/Core/src/Transports/Hid/Linux/LinuxHidReportApi.cs", "src/Core/src/Transports/Hid/Linux/LinuxFidoHidNativeOwner.cs", "src/Core/src/Transports/Hid/Linux/LinuxOtpHidNativeOwner.cs"],
    "CORE-B2-C002": ["docs/research/linux-hid-routing.md", "src/Core/CLAUDE.md", "src/Core/README.md", "src/Core/src/Devices/SharedDeviceInfoRead.cs", "src/Core/src/Devices/ProtocolDeviceInfo.cs", "src/Core/src/Devices/HidConnectionSlot.cs", "docs/architecture/connection-ownership-and-contention.md", "docs/architecture/raw-access-tiers.md", "docs/migration/v1-to-v2.md", *[p for p in {u["path"] for u in selected} if p.startswith("docs/research/linux-")], *packet["paths"][49:]],
    "CORE-B2-C003": [*packet["paths"][9:13], "src/Core/src/Native/Linux/Udev/Udev.Interop.cs", "src/Core/src/Transports/Hid/Linux/LinuxHidInterface.cs", "src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs", "src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/LinuxUdevHidEventSourceTests.cs", "docs/linux-setup.md"],
    "CORE-B2-C004": ["src/Core/src/Native/Linux/Linux.Libraries.cs", "src/Core/src/Native/Linux/Udev/Udev.Interop.cs", "src/Core/src/Transports/Hid/FindHidInterfaces.cs", "docs/linux-setup.md", "src/Core/README.md", "README.md", "docs/NATIVE-AOT.md", "verification/LinuxHidRouteVerification/publish_contract.py", "verification/LinuxHidRouteVerification/LinuxHidRouteVerification.csproj"],
    "CORE-B2-C005": [packet["paths"][31], packet["paths"][35], packet["paths"][38], "src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/WithheldNativeProbe.cs", packet["paths"][45], packet["paths"][40], packet["paths"][62], "docs/research/linux-async-boundaries-baseline.md", "docs/research/linux-fido-async-lifetime.md", "docs/research/linux-hid-routing.md", "docs/research/linux-managed-acceptance.md", "src/Core/tests/CLAUDE.md"],
    "CORE-B2-C006": ["src/Core/src/Transports/Hid/Linux/LinuxHidInterface.cs", "src/Core/src/Transports/Hid/IHidInterface.cs", "src/Core/src/Transports/Hid/IHidConnection.cs", "src/Core/src/Transports/Hid/Linux/LinuxHidIOReportConnection.cs", "src/Core/src/Transports/Hid/Linux/LinuxHidFeatureReportConnection.cs", "src/Core/src/Transports/Hid/Linux/LinuxFidoHidConnection.cs", "src/Core/src/Transports/Hid/Linux/LinuxOtpHidConnection.cs", "docs/architecture/raw-access-tiers.md", "docs/migration/v1-to-v2.md", "src/Core/README.md", packet["paths"][46]],
}
candidate_index = []
for candidate, title in titles.items():
    affected = [u for u in results if candidate in u["candidate_ids"]]
    assert affected
    paths = sorted(set(associated[candidate]) | {u["path"] for u in affected})
    assert all(Path(p).is_file() for p in paths), paths
    candidate_index.append({"id": candidate, "title": title, "status": "candidate", "accepted_id": None,
                            "unit_ids": [u["id"] for u in affected], "documentation": "triage/fresh edit and independent review pending",
                            "implementation": "unchanged; no fix authorized", "decision": "see candidate-specific contract in candidates.md",
                            "qualification": "source/assertion evidence only; no new execution",
                            "associated_unchanged_paths": [{"path": p, "disposition": "PENDING-EXPLICIT-FUTURE-EDITOR-REVIEWER-DISPOSITION", "action": "retain or edit with reason after triage; preserve executable bodies in Pass 1"} for p in paths]})
save("candidate-index.json", candidate_index)
save("preservation-verification.json", {
    "packet_primary_ids_exact": True, "packet_shared_ids_exact": True, "unique_unit_ids": len({u["id"] for u in results}),
    "all_source_and_range_hashes_match_inventory": True, "all_selected_paths_lf_equal_pin": True,
    "raw_pin_differences": [i["path"] for i in identities if not i["raw_equals_pin"]],
    "semantic_read_proof": "manual complete reads and file-evidence; identity checks are separate",
    "new_execution": {"build": 0, "test": 0, "formatter": 0, "native": 0, "device": 0},
    "tool_scope": "Python/Git static identity serialization and HTTP original-source fetches only",
})
with (HERE / "file-evidence.md").open("w") as output:
    output.write("# Complete file read evidence\n\nEvery primary file was read in full, including helper implementations, complete tests, barriers and cleanup. These are source/assertion observations, with zero new executions. Unit IDs/ranges/hashes and exact candidate intersections are in unit-dispositions.json.\n\n")
    for path in packet["paths"]:
        output.write(f"## `{path}`\n\n{notes[Path(path).name]}\n\n")
    output.write("## Shared exact-range context\n\n")
    for path in sorted({u["path"] for u in selected if u["id"] in shared}):
        output.write(f"- `{path}`: {shared_notes[Path(path).name]}\n")
print(json.dumps(summary, indent=2))
