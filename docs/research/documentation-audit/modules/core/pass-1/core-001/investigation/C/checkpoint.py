"""Build Packet C's local read ledger without changing central campaign records.

This is static inventory/annotation tooling. It executes no SDK or cryptographic code.
Read claims are explicit session observations below, never inferred from reading bytes here.
"""

from collections import Counter
import hashlib
import json
from pathlib import Path
import subprocess


ROOT = Path(__file__).resolve().parents[9]
RUN = ROOT / "docs/research/documentation-audit/modules/core/pass-1/core-001"
OWN = RUN / "investigation/C"
BASELINE = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"

# Every shipping primary file was actually presented in full in this session,
# including both reads of the 1,622-line historical RsaFormat file.
READ_TEST_NAMES = {
    "CoreCredentialReaderAuditReproTests.cs",
    "CoreNumericBoundaryAuditReproTests.cs",
    "ConsoleCredentialReaderTests.cs",
    "HexParsingTests.cs",
    "UserPresenceIntentTests.cs",
    "Base32Tests.cs",
    "DisposableArrayPoolBufferTests.cs",
    "DisposableTlvListTests.cs",
    "TlvHelperTests.cs",
    "TlvTests.cs",
    "CryptographicAlgorithmApplicabilityTests.cs",
    "CryptographicDefinitionsTests.cs",
    "NamedCurveCatalogVectorsTests.cs",
    "EcTestSupport.cs",
    "KnownPrivateKeyVectors.cs",
    "ArkgP256Tests.cs",
    "Curve25519PrivateKeyTests.cs",
    "PrivateKeyZeroingFollowupTests.cs",
}
READ_SHARED_PATHS = {
    "src/Core/README.md",
    "src/Core/CLAUDE.md",
    "docs/LOGGING.md",
    "docs/CRYPTO-APIS.md",
    "docs/MEMORY-MANAGEMENT.md",
    "docs/usage/tlv-processing.md",
    "src/Cli.Shared/src/Output/SecureCredential.cs",
    "src/Cli.Shared/src/Output/PinPrompt.cs",
    "src/Cli.Shared/src/Output/ConsoleUserPresencePrompt.cs",
}
HISTORICAL = {
    "src/Core/src/Cryptography/RsaFormat.cs",
    "src/Core/src/Cryptography/Cose/CoseKeyOperations.cs",
    "src/Core/src/Utilities/DisposableTlvDictionary.cs",
}
# These nominees have full method/assertion context inspected. They remain
# source-inspection-only conclusions and require independent review/sampling.
CLEAN_NOMINEES = {
    "src/Core/src/Utilities/Base32.cs":
        "Complete decode/admission/terminal-shape branches match local XML; full Base32Tests inspected. "
        "RFC 4648 decoder pad-bit discretion is distinct from this codec's explicit case/space/padding policy. "
        "Overlap is rejected before failure cleanup, success preserves the tail, admitted failure clears all output. "
        "No tests or giant-input overflow experiment ran.",
    "src/Core/tests/Yubico.YubiKit.Core.UnitTests/Utilities/Base32Tests.cs":
        "Complete literal byte vectors, malformed terminal/symbol fixtures, short-output cleanup, and byte-offset "
        "overlap assertions inspected. Tests demonstrate their intended checks in source, not a fresh execution.",
}


def load(relative):
    return json.loads((RUN / relative).read_text())


def digest(data):
    return hashlib.sha256(data).hexdigest()


def normalized(data):
    return data.decode("utf-8-sig").replace("\r\n", "\n")


def main():
    packet = load("inventory/packets/C.json")
    all_units = load("inventory/semantic-units.json")
    shared = load("inventory/shared-sections.json")
    findings = json.loads((OWN / "findings.json").read_text())["candidates"]
    primary_ids = set(packet["unit_ids"])
    shared_ids = set(packet["shared_read_unit_ids"])
    selected_ids = primary_ids | shared_ids
    unit_by_id = {unit["id"]: unit for unit in all_units}
    assert selected_ids <= unit_by_id.keys()
    assert {unit["unit_id"] for unit in shared if unit["unit_id"] in shared_ids} == shared_ids

    # Identity validation is mechanical and supplies no semantic read disposition.
    source_cache = {}
    for unit_id in selected_ids:
        unit = unit_by_id[unit_id]
        path = unit["path"]
        if path not in source_cache:
            raw = (ROOT / path).read_bytes()
            source_cache[path] = (digest(raw), raw.decode("utf-8-sig"))
        raw_hash, text = source_cache[path]
        assert raw_hash == unit["source_sha256"], (unit_id, path, "whole-file drift")
        selected = text[unit["start_character"]:unit["end_character_exclusive"]]
        assert digest(selected.encode("utf-8")) == unit["range_sha256"], (unit_id, path, "range drift")

    read_primary = {
        path for path in packet["paths"]
        if path.startswith("src/Core/src/") or Path(path).name in READ_TEST_NAMES
    }
    files = []
    for path in packet["paths"]:
        raw = (ROOT / path).read_bytes()
        pinned = subprocess.check_output(["git", "show", f"{BASELINE}:{path}"], cwd=ROOT)
        assert normalized(raw) == normalized(pinned), path
        files.append({
            "path": path,
            "source_sha256": digest(raw),
            "pinned_git_blob": subprocess.check_output(
                ["git", "rev-parse", f"{BASELINE}:{path}"], cwd=ROOT, text=True).strip(),
            "normalized_equal_to_baseline": True,
            "lines": len(raw.decode("utf-8-sig").splitlines()),
            "read_complete": path in read_primary,
            "status": "READ-HISTORICAL-EXCLUSION" if path in HISTORICAL else
                      "READ-CLEAN-NOMINEE" if path in CLEAN_NOMINEES else
                      "READ-SOURCE-PENDING-FINAL-TRIAGE" if path in read_primary else "UNREAD",
        })

    rows = []
    for unit_id in [*packet["unit_ids"], *packet["shared_read_unit_ids"]]:
        unit = unit_by_id[unit_id]
        path = unit["path"]
        read = path in read_primary if unit_id in primary_ids else path in READ_SHARED_PATHS
        candidate_ids = [
            finding["id"] for finding in findings
            if any(location["path"] == path
                   and location["first"] <= unit["last_line"]
                   and location["last"] >= unit["first_line"]
                   for location in finding["locations"])
        ]
        status = "UNREAD"
        reason = "Required complete semantic read and disposition remain pending; excluded from clean sampling."
        if read:
            status = "READ-SOURCE-PENDING-FINAL-TRIAGE"
            reason = "Complete file presented and inspected; source assertions/behavior considered. " \
                     "Outstanding citation, caller, independent review or final unit triage prevents clean closure."
            if candidate_ids:
                status = "READ-CANDIDATE"
                reason = "Complete source unit read; candidate location overlaps this unit. See exact finding scope."
            elif path in HISTORICAL:
                status = "READ-HISTORICAL-EXCLUSION"
                reason = "Entire file is commented-out source with no active API/runtime. Historical explanation " \
                         "read for applicability; preserve bytes and exclude from current contract claims."
            elif path in CLEAN_NOMINEES:
                status = "READ-CLEAN-NOMINEE"
                reason = CLEAN_NOMINEES[path]
        rows.append({
            "id": unit_id,
            "path": path,
            "first_line": unit["first_line"],
            "last_line": unit["last_line"],
            "label": unit["label"],
            "category": unit["category"],
            "source_sha256": unit["source_sha256"],
            "range_sha256": unit["range_sha256"],
            "assignment": "primary" if unit_id in primary_ids else "shared-read",
            "status": status,
            "candidate_ids": candidate_ids,
            "reason": reason,
            "independent_review": "PENDING",
            "execution": "NOT-RUN",
            "sample_eligible": status == "READ-CLEAN-NOMINEE",
        })

    dependencies = []
    for finding in findings:
        paths = {location["path"] for location in finding["locations"]} | set(finding["reopen"])
        for path in sorted(paths):
            dependencies.append({
                "candidate_id": finding["id"],
                "path": path,
                "exists": (ROOT / path).exists(),
                "source_sha256": digest((ROOT / path).read_bytes()) if (ROOT / path).is_file() else None,
                "disposition": "UNCHANGED-PENDING-EDITOR-AND-INDEPENDENT-REVIEW",
                "read_scope": "complete-primary-file" if path in read_primary else
                              "complete-shared-file-read; only assigned ranges dispositioned" if path in READ_SHARED_PATHS else
                              "not semantically read in this checkpoint; later owner must read complete affected units",
                "trigger": finding["title"],
            })

    summary = {
        "baseline": BASELINE,
        "observed_head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
        "packet_sha256": digest((RUN / "inventory/packets/C.json").read_bytes()),
        "inventory_sha256": digest((RUN / "inventory/semantic-units.json").read_bytes()),
        "primary_file_count": len(files),
        "primary_file_lines": sum(item["lines"] for item in files),
        "primary_files_read_completely": len(read_primary),
        "primary_unread_paths": [item["path"] for item in files if not item["read_complete"]],
        "primary_units": len(primary_ids),
        "shared_read_units": len(shared_ids),
        "dispatch_shared_count": 174,
        "shared_count_discrepancy": "Current JSON contains 176 IDs; dispatch prose says 174. Exact JSON retained, no denominator edited.",
        "unit_status_counts": dict(Counter(item["status"] for item in rows)),
        "unit_status_counts_by_assignment": {
            assignment: dict(Counter(item["status"] for item in rows if item["assignment"] == assignment))
            for assignment in ["primary", "shared-read"]
        },
        "candidate_count": len(findings),
        "selected_unit_source_and_range_hash_checks": len(selected_ids),
        "accepted_findings": 0,
        "completion": "PARTIAL-CHECKPOINT; whole-packet completion explicitly outstanding",
        "builds_tests_formatters_crypto_experiments_native_device_runs": 0,
        "central_records_changed": False,
        "commits_index_ref_writes_pushes_subagents": 0,
    }
    for name, value in [("files.json", files), ("unit-dispositions.json", rows),
                        ("unchanged-paths.json", dependencies), ("checkpoint-summary.json", summary)]:
        (OWN / name).write_text(json.dumps(value, indent=2) + "\n")
    print(json.dumps(summary, indent=2))


if __name__ == "__main__":
    main()
