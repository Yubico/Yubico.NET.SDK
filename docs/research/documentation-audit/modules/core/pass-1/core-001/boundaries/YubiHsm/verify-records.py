"""Independent static record joins, not independent content review or SDK tests."""
import collections
import hashlib
import json
from pathlib import Path
import re
import subprocess

out = Path(__file__).resolve().parent
root = out.parents[8]
run = out.parents[1]
pin = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"


def load(path):
    return json.loads(path.read_text())


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def norm(raw):
    return raw.decode("utf-8-sig").replace("\r\n", "\n")


packet = load(run / "inventory/packets/BOUNDARY-YubiHsm.json")
inventory = {u["id"]: u for u in load(run / "inventory/semantic-units.json")}
units = load(out / "unit-dispositions.json")
files = load(out / "file-dispositions.json")
assert len(units) == 160 and len(files) == 53
assert {u["id"] for u in units} == set(packet["unit_ids"])
assert {f["path"] for f in files} == set(packet["disposition_paths"])
for u in units:
    original = inventory[u["id"]]
    for key in ["path", "source_sha256", "range_sha256", "start_character", "end_character_exclusive", "first_line", "last_line", "label"]:
        assert u[key] == original[key], (u["id"], key)
    raw = (root / u["path"]).read_bytes()
    assert digest(raw) == u["source_sha256"]
    selected = raw.decode("utf-8")[u["start_character"]:u["end_character_exclusive"]]
    assert digest(selected.encode()) == u["range_sha256"]
    assert u["read_status"] == "READ-COMPLETE" and u["status"] != "UNREAD"

for f in files:
    raw = (root / f["path"]).read_bytes()
    pinned = subprocess.check_output(["git", "show", f"{pin}:{f['path']}"], cwd=root)
    assert digest(raw) == f["source_sha256"] and digest(pinned) == f["pinned_sha256"]
    assert norm(raw) == norm(pinned)
    assert f["outcome"].startswith("UNCHANGED-") and f["authored_context"]
    assert set(f["unit_ids"]) == {u["id"] for u in units if u["path"] == f["path"]}

candidates = load(out / "candidate-index.json")
candidate_ids = {c["id"] for c in candidates}
assert candidate_ids == {f"CORE-HSM-X{n:03}" for n in range(1, 8)}
for c in candidates:
    assert set(c["unchanged_paths"]) <= set(packet["disposition_paths"])
    assert set(c["unit_ids"]) == {u["id"] for u in units if c["id"] in u["candidate_ids"]}
    assert set(c["related_unit_ids"]) == {u["id"] for u in units if c["id"] in u["related_candidate_ids"]}
    assert c["accepted_id"] is None
for u in units:
    assert set(u["candidate_ids"] + u["related_candidate_ids"]) <= candidate_ids

inputs = load(run / "inventory/inherited-packet-inputs.json")["BOUNDARY-YubiHsm"]
inherited = load(out / "inherited-dispositions.json")
registry_path = run / "inherited-findings/registry.json"
registry = {r["key"]: r for r in load(registry_path)["rows"]}
assert inherited["registry_sha256"] == digest(registry_path.read_bytes())
assert {r["key"] for r in inherited["rows"]} == set(inputs["provenance_keys"])
for r in inherited["rows"]:
    assert r["registry_status_unchanged"] == registry[r["key"]]["current_state"]
    assert r["registry_conclusion_unchanged"] == registry[r["key"]]["current_conclusion"]
assert {r["key"] for r in inherited["module_engineering_aliases"]} == {f"yubihsm-001:HSM-IMP-{n:03}" for n in range(1, 9)}

nominees = load(out / "clean-nominees.json")
assert len(nominees) == 17
for n in nominees:
    u = next(u for u in units if u["id"] == n["id"])
    assert u["status"] == "READ-CLEAN" and u["range_sha256"] == n["range_sha256"]

dependencies = load(out / "dependency-dispositions.json")
for d in dependencies:
    raw = (root / d["path"]).read_bytes()
    assert digest(raw) == d["source_sha256"]
    selected = "".join(raw.decode("utf-8-sig").splitlines(keepends=True)[d["first_line"] - 1:d["last_line"]])
    assert digest(selected.encode()) == d["selected_line_text_sha256"]
    assert d["new_coverage_credit"] == 0
for s in load(out / "source-identities.json"):
    assert digest((root / s["path"]).read_bytes()) == s["sha256"]

summary = load(out / "coverage-summary.json")
counts = dict(collections.Counter(u["status"] for u in units))
assert counts == summary["dispositions"] == {"READ-CANDIDATE": 8, "READ-CONTEXT": 73, "READ-CLEAN": 79}
report = run / "boundary-YubiHsm.md"
for doc in list(out.glob("*.md")) + [report]:
    text = doc.read_text()
    assert sum(line.startswith("```") for line in text.splitlines()) % 2 == 0, doc
    for target in re.findall(r"\]\(([^)#]+)(?:#[^)]*)?\)", text):
        if not target.startswith(("http://", "https://", "mailto:")):
            assert (doc.parent / target).exists(), (doc, target)
assert "79 read-clean, eight read-candidate, 73 read-context" in report.read_text()

# Recheck actual matching message strings after the retained investigator correction.
test = root / "src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.UnitTests/HsmAuthRetryExceptionTests.cs"
implementation = root / "src/YubiHsm/src/HsmAuthSession.cs"
assert 'Assert.Contains("Invalid credential password", exception.Message);' in test.read_text()
assert 'ThrowOnRetryFailure(response, command, "Invalid credential password")' in implementation.read_text()
receipt = {
    "status": "STATIC-RECORD-CHECKS-PASSED", "source_baseline": pin,
    "assigned_units": len(units), "candidate_files": len(files), "candidate_groups": len(candidates),
    "dependent_source_ranges": len(dependencies), "inherited_keys": len(inherited["rows"]), "clean_nominees": len(nominees),
    "dispositions": counts, "links_fences_ids_hashes_and_pin": "CHECKED",
    "corrected_static_test_message_check": {"expected": "Invalid credential password", "actual_helper": "Invalid credential password", "result": "MATCH-NO-FINDING", "test_source_sha256": digest(test.read_bytes()), "implementation_source_sha256": digest(implementation.read_bytes()), "executed": False},
    "independent_content_review": "PENDING", "build_test_crypto_native_device_connector_executions": 0,
    "git_index_ref_or_product_writes": 0,
}
(out / "verification-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
owned = sorted(list(out.glob("*")) + [report])
manifest = [{"path": str(p.relative_to(root)), "bytes": p.stat().st_size, "sha256": digest(p.read_bytes())} for p in owned if p.is_file() and p.name != "record-manifest.json"]
(out / "record-manifest.json").write_text(json.dumps({"scope": "BOUNDARY-YubiHsm-owned-records-only", "excludes_self": True, "files": manifest}, indent=2) + "\n")
print(json.dumps(receipt, indent=2))
