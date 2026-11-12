"""Records-only consistency check; executes no SDK or cryptography."""
import collections
import hashlib
import json
import pathlib
import re

HERE = pathlib.Path(__file__).resolve().parent
RUN = HERE.parent.parent
ROOT = next(parent for parent in HERE.parents if (parent / ".git").exists())

def load(path):
    return json.loads(path.read_text())

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

packet = load(RUN / "inventory/packets/B6-wire-scp.json")
inventory = {u["id"]: u for u in load(RUN / "inventory/semantic-units.json")}
units = load(HERE / "unit-dispositions.json")
files = load(HERE / "file-dispositions.json")
summary = load(HERE / "checkpoint-summary.json")
inherited = load(HERE / "inherited-dispositions.json")
keys = load(RUN / "inventory/inherited-packet-inputs.json")["B6-wire-scp"]["provenance_keys"]
assert len(units) == 684
assert len({u["id"] for u in units}) == len(units)
assert {u["id"] for u in units} == set(packet["unit_ids"] + packet["shared_read_unit_ids"])
assert {r["key"] for r in inherited} == set(keys)
for u in units:
    original = inventory[u["id"]]
    raw = (ROOT / u["path"]).read_bytes()
    text = raw.decode("utf-8-sig")
    assert hashlib.sha256(raw).hexdigest() == original["source_sha256"] == u["source_sha256"]
    assert hashlib.sha256(text[u["start_character"]:u["end_character_exclusive"]].encode()).hexdigest() == u["range_sha256"] == original["range_sha256"]
assert dict(collections.Counter(u["disposition"] for u in units)) == summary["unit_dispositions"]
assert sum(f["read_scope"] == "complete-file" for f in files) == 48
assert len([u for u in units if u["disposition"] == "UNREAD"]) == 2
nominees = load(HERE / "clean-nominees.json")
assert len(nominees) == 8
assert all(n["disposition"] == "READ-CLEAN" for n in nominees)
assert len(load(HERE / "unchanged-paths.json")) == len(files)
sources = []
for manifest in ["initial-download-manifest.json", "focused-followup-manifest.json"]:
    for r in load(HERE / "sources" / manifest):
        if "sha256" in r:
            path = HERE / "sources" / r["name"]
            assert sha(path) == r["sha256"]
            sources.append({"path": str(path.relative_to(ROOT)), "sha256": sha(path), "manifest": manifest})
for name in ["scp03-1.1.2.txt", "scp11-public-review-1.3.0.13.txt"]:
    path = HERE / "sources" / name
    sources.append({"path": str(path.relative_to(ROOT)), "sha256": sha(path), "role": "pdftotext -layout reading derivative"})

links_checked = []
authored = [RUN / "investigation-B6.md", *HERE.glob("*.md"), HERE / "sources/register.md"]
for path in authored:
    body = path.read_text()
    assert body.count("```") % 2 == 0, path
    for link in re.findall(r"\]\(([^)#]+)(?:#[^)]*)?\)", body):
        if "://" not in link:
            assert (path.parent / link).exists(), (path, link)
            links_checked.append({"path": str(path.relative_to(ROOT)), "target": link})

receipt = {"kind": "RECORDS-ONLY; not SDK tests, independent semantic review, or completion of B6", "ids_checked": len(units), "primary_files_completely_read": 48, "shared_ids_read": 16, "unread": [{"id": u["id"], "path": u["path"]} for u in units if u["disposition"] == "UNREAD"], "pending_related_context_triage": 662, "inherited_keys_checked": len(inherited), "clean_nominees_checked": len(nominees), "retained_sources_and_derivatives": sources, "authored_local_links_checked": len(links_checked), "source_and_assertion_edits": "none authorized or made", "new_sdk_execution_count": 0, "scope": "All684 original unit IDs/raw-range identities and packet partitions reconciled; original downloads match manifest; authored record links/fences checked."}
(HERE / "verification-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
print(json.dumps(receipt, indent=2))
