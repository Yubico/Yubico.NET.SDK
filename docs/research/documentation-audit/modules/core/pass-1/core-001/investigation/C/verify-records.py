"""Verify the completed record joins/identities, not SDK behavior or read quality."""

from collections import Counter
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[9]
RUN = ROOT / "docs/research/documentation-audit/modules/core/pass-1/core-001"
OWN = RUN / "investigation/C"


def load(path):
    return json.loads(path.read_text())


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    packet = load(RUN / "inventory/packets/C.json")
    summary = load(OWN / "final-summary.json")
    rows = load(OWN / "unit-dispositions.json")
    dossiers = load(OWN / "file-dispositions.json")
    findings = load(OWN / "findings.json")["candidates"]
    assert len(rows) == len({row["id"] for row in rows}) == 1562
    assert {row["id"] for row in rows if row["assignment"] == "primary"} == set(packet["unit_ids"])
    assert {row["id"] for row in rows if row["assignment"] == "shared-read"} == set(packet["shared_read_unit_ids"])
    assert all(row["read_complete"] and row["status"].startswith("READ-") for row in rows)
    assert not any("UNREAD" in row["status"] or "PENDING-FINAL-TRIAGE" in row["status"] for row in rows)
    assert set(summary["unit_status_counts"]) == set(Counter(row["status"] for row in rows))
    assert summary["unit_status_counts"] == dict(Counter(row["status"] for row in rows))
    assert {item["path"] for item in dossiers if item["complete_primary_file_read"]} == set(packet["paths"])
    assert sum(row["sample_eligible"] for row in rows) == summary["clean_sample_nominees"]
    assert {item["id"] for item in findings} == {f"CORE-C-C{i:03}" for i in range(1,21)}
    for item in findings:
        for loc in item["locations"]:
            assert 1 <= loc["first"] <= loc["last"] <= len((ROOT / loc["path"]).read_text(encoding="utf-8-sig").splitlines())

    # Every unchanged candidate path still matches the source pin after explicit
    # checkout newline normalization, including dependency-only paths.
    unchanged = load(OWN / "unchanged-paths.json")
    checks = {}
    for item in unchanged:
        path = item["path"]
        target = ROOT / path
        assert target.is_file() and digest(target) == item["source_sha256"], path
        if path not in checks:
            pinned = subprocess.check_output(["git","show",f"{summary['baseline']}:{path}"],cwd=ROOT)
            assert target.read_bytes().decode("utf-8-sig").replace("\r\n","\n") == pinned.decode("utf-8-sig").replace("\r\n","\n"),path
            checks[path] = True

    original_checks = 0
    for name in ["fetch-manifest.json","focused-fetch-manifest.json","completion-fetch-manifest.json",
                 "provider-source-manifest.json","oid-and-nist-followup-manifest.json","ber-source-attempts.json"]:
        for item in load(OWN / "sources" / name):
            if "sha256" in item:
                assert digest(OWN / "sources" / item["file"]) == item["sha256"],item
                original_checks += 1
    python_manifest = load(RUN / "sources/python/manifest.json")
    for item in python_manifest["files"]:
        path = RUN / "sources/python/snapshot" / item["path"]
        if path.exists():
            assert digest(path) == item["sha256"],item
    inherited = load(OWN / "inherited-dispositions.json")
    assert inherited["provenance_rows"] == 18 and inherited["nonadditive_groups"] == 13
    assert inherited["registry_sha256"] == digest(RUN / "inherited-findings/registry.json")
    assert summary["packet_sha256"] == digest(RUN / "inventory/packets/C.json")
    assert summary["inventory_sha256"] == digest(RUN / "inventory/semantic-units.json")
    assert "207 substantive clean sampling nominees" in (RUN / "investigation-C.md").read_text()
    for base in [OWN,RUN / "sources/python"]:
        for path in base.rglob("*.json"):
            load(path)

    # Avoid self-referential manifests. These two generated products are the
    # verification envelope; the content manifest covers the other owned bytes.
    members = [RUN / "investigation-C.md", *OWN.rglob("*"), *(RUN / "sources/python").rglob("*")]
    evidence = [{"path":str(path.relative_to(ROOT)),"sha256":digest(path),"bytes":path.stat().st_size}
                for path in members if path.is_file() and path.name not in {"verification.json","evidence-manifest.json"}]
    (OWN / "evidence-manifest.json").write_text(json.dumps(evidence,indent=2)+"\n")
    result = {
        "result":"PASS-STATIC-RECORD-IDENTITIES-AND-JOINS", "primary_files":104,"primary_units":1386,"shared_units":176,
        "unit_ids_reconciled":len(rows),"unread_or_pending_final_triage":0,
        "original_download_hashes_checked":original_checks,"unchanged_candidate_paths_compared_to_pin":len(checks),
        "local_candidates":20,"inherited_rows":18,"inherited_groups":13,"clean_nominees":summary["clean_sample_nominees"],
        "owned_evidence_files":len(evidence),
        "semantic_read_proof":"Authored complete-read observations in semantic-notes/final-review and exact unit dossiers; not inferred from these mechanical checks.",
        "sdk_tests_builds_crypto_experiments_native_device_execution":0,
        "limits":"This verifies records/source identity only; it is not independent review, compiled consumer/API binding or runtime/provider/device qualification."
    }
    (OWN / "verification.json").write_text(json.dumps(result,indent=2)+"\n")
    print(json.dumps(result,indent=2))


if __name__ == "__main__":
    main()
