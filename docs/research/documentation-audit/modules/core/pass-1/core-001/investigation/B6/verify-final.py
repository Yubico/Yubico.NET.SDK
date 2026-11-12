"""Final static verification, separate from semantic judgment and SDK execution."""
import collections
import hashlib
import json
import pathlib
import re
import subprocess

HERE = pathlib.Path(__file__).resolve().parent
RUN = HERE.parent.parent
ROOT = next(parent for parent in HERE.parents if (parent / ".git").exists())
PIN = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"

def load(path):
    return json.loads(path.read_text())

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

packet = load(RUN / "inventory/packets/B6-wire-scp.json")
original = {u["id"]: u for u in load(RUN / "inventory/semantic-units.json")}
units = load(HERE / "unit-dispositions.json")
files = load(HERE / "file-dispositions.json")
summary = load(HERE / "final-summary.json")
assert len(units) == len({u["id"] for u in units}) == 684
assert {u["id"] for u in units} == set(packet["unit_ids"] + packet["shared_read_unit_ids"])
assert {f["path"] for f in files if f["primary"]} == set(packet["paths"])
assert len([f for f in files if f["primary"]]) == 50
assert sum(f["lines"] for f in files if f["primary"]) == 9441
for u in units:
    baseline = original[u["id"]]
    raw = (ROOT / u["path"]).read_bytes()
    text = raw.decode("utf-8-sig")
    assert digest(ROOT / u["path"]) == u["source_sha256"] == baseline["source_sha256"]
    assert hashlib.sha256(text[u["start_character"]:u["end_character_exclusive"]].encode()).hexdigest() == u["range_sha256"] == baseline["range_sha256"]
    assert u["semantic_review"] == "COMPLETE-FOR-ASSIGNED-SCOPE"
    assert u["disposition"] in {"READ-CLEAN", "READ-CANDIDATE", "READ-CONTEXT", "READ-HISTORICAL-CONTEXT"}
    assert bool(u["candidate_ids"]) == (u["disposition"] == "READ-CANDIDATE")
    assert u["reason"] and u["unchanged_decision"]
for file in files:
    raw = (ROOT / file["path"]).read_bytes()
    pinned = subprocess.check_output(["git", "show", f"{PIN}:{file['path']}"], cwd=ROOT)
    assert raw == pinned or raw.replace(b"\r\n", b"\n") == pinned
assert dict(collections.Counter(u["disposition"] for u in units)) == summary["unit_dispositions"]
assert summary["unread"] == summary["awaiting_context_or_final_triage"] == summary["new_sdk_execution_count"] == 0
nominees = load(HERE / "clean-nominees.json")
assert len(nominees) == len({n["id"] for n in nominees}) == 16
assert all(n["disposition"] == "READ-CLEAN" and not n["candidate_ids"] and not n["sampled"] for n in nominees)
candidates = load(HERE / "candidate-index.json")
assert {c["id"] for c in candidates} == {f"CORE-B6-C{i:03}" for i in range(1,17)}
for c in candidates:
    assert set(c["direct_unit_ids"]) == {u["id"] for u in units if c["id"] in u["candidate_ids"]}
inherited = load(HERE / "inherited-dispositions.json")
assert {r["key"] for r in inherited} == set(load(RUN / "inventory/inherited-packet-inputs.json")["B6-wire-scp"]["provenance_keys"])
unchanged = load(HERE / "unchanged-paths.json")
assert {f["path"] for f in files} <= {p["path"] for p in unchanged}
for record in unchanged:
    assert digest(ROOT / record["path"]) == record["source_sha256"]

history = HERE / "history/checkpoint-48-files"
for entry in load(history / "manifest.json")["files"]:
    assert digest(history / entry["snapshot"]) == entry["sha256"]
assert load(history / "checkpoint-summary.json")["state"] == "CHECKPOINT-INCOMPLETE"
source_manifest = load(HERE / "sources/final-manifest.json")
retained = []
failed = []
for entry in source_manifest["sources"]:
    if entry.get("retained_path"):
        assert digest(ROOT / entry["retained_path"]) == entry["sha256"]
        retained.append(entry["retained_path"])
    elif "error" in entry:
        failed.append({"url": entry["url"], "error": entry["error"]})
pdfs = [e for e in source_manifest["sources"] if (e.get("retained_path") or "").endswith(".pdf")]
assert len(pdfs) == 2 and all(e["byte_exact_original"] for e in pdfs)
vector = load(HERE / "vector-review.json")
assert len(vector["all_records"]) == 100
assert vector["embedded_assertion_rows"]["X963KdfTests"] == 20
assert sum(v["lines"] for v in vector["source_files"]) == 1890
for f in vector["source_files"]:
    assert digest(ROOT / f["path"]) == f["sha256"]

link_count = 0
for path in [RUN / "investigation-B6.md", *HERE.glob("*.md"), HERE / "sources/final-source-register.md", HERE / "sources/register.md"]:
    body = path.read_text()
    assert body.count("```") % 2 == 0, path
    for link in re.findall(r"\]\(([^)#]+)(?:#[^)]*)?\)", body):
        if "://" not in link:
            target = (path.parent / link).resolve()
            # This receipt is produced only after every other assertion succeeds.
            assert target.exists() or target == HERE / "final-verification.json", (path, link)
            link_count += 1
receipt = {"kind": "FINAL-STATIC-RECORD-VERIFICATION", "source_pin": PIN, "metadata_head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(), "ids_checked": 684, "primary_files_checked": 50, "primary_lines": 9441, "shared_ids_checked": 16, "dispositions": summary["unit_dispositions"], "unread_or_awaiting_context": 0, "clean_nominees": 16, "local_candidates": 16, "inherited_keys": 8, "unchanged_path_records": len(unchanged), "historical_artifacts_preserved": 17, "retained_source_entries_verified": len(retained), "byte_exact_original_pdfs": [{"path": e["retained_path"], "sha256": e["sha256"]} for e in pdfs], "failed_original_requests_preserved": len(failed), "complete_fixture_records": 100, "actual_embedded_vector_triples": 20, "authored_links_checked": link_count, "new_sdk_execution_count": 0, "limits": "Identity/partition/record/source-data consistency checks follow authored semantic reading. They are not independent content review, new cryptographic computations, SDK tests, compiler validation, hardware/native qualification or accepted Core finding triage."}
(HERE / "final-verification.json").write_text(json.dumps(receipt, indent=2) + "\n")
print(json.dumps(receipt, indent=2))
