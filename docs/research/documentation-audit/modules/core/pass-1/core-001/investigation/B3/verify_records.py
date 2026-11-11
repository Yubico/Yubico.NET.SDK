"""Final record/source reconciliation only; deliberately invokes no SDK or native code."""
import hashlib
import json
from pathlib import Path
import re
import subprocess

HERE = Path(__file__).parent
RUN = HERE.parent.parent

def sha(data):
    return hashlib.sha256(data).hexdigest()

packet = json.loads((RUN / "inventory/packets/B3-macos-lifetimes.json").read_text())
units = json.loads((HERE / "unit-dispositions.json").read_text())
files = json.loads((HERE / "file-identities.json").read_text())
expected = set(packet["unit_ids"]) | set(packet["shared_read_unit_ids"])
assert len(units) == len(expected) == 1144
assert {u["id"] for u in units} == expected
assert sum(u["ownership"] == "PRIMARY" for u in units) == 1139
assert sum(u["status"] == "READ-CLEAN" for u in units) == 1087
assert sum(u["status"] == "READ-CANDIDATE" for u in units) == 57
assert all(u["status"] in {"READ-CLEAN", "READ-CANDIDATE"} for u in units)
assert len(packet["paths"]) == 41
assert sum(f["lines"] for f in files if f["primary"]) == 7636
for f in files:
    assert sha(Path(f["path"]).read_bytes()) == f["source_sha256"], f["path"]
for u in units:
    text = Path(u["path"]).read_bytes().decode("utf-8-sig")
    assert sha(text[u["start_character"]:u["end_character_exclusive"]].encode()) == u["range_sha256"], u["id"]
for c in json.loads((HERE / "sources/captures.json").read_text()):
    if c.get("status") == 200:
        assert sha((HERE / "sources" / c["name"]).read_bytes()) == c["sha256"], c["name"]
for c in json.loads((HERE / "candidate-associated-paths.json").read_text()):
    assert Path(c["path"]).exists(), c["path"]
for path in [HERE / "verification.md", HERE / "candidates.md", RUN / "investigation-B3.md"]:
    for link in re.findall(r"\]\(([^)#]+)(?:#[^)]*)?\)", path.read_text()):
        if not link.startswith(("https:", "http:")):
            assert (path.parent / link).exists(), (str(path), link)
product_diff = subprocess.check_output(["git", "diff", "--name-only", "--", "src", "verification", "Directory.Packages.props"]).decode().strip()
assert product_diff == "", product_diff
receipt = {"outcome": "PASS-RECORD-ONLY", "assigned_units": 1144, "primary_units": 1139,
           "shared_units": 5, "primary_files": 41, "primary_lines": 7636,
           "read_clean": 1087, "read_candidate": 57, "unread": 0,
           "all_source_range_capture_hashes_equal": True, "all_mapped_paths_and_record_links_exist": True,
           "tracked_product_diff_empty": True, "new_sdk_execution_count": 0,
           "independent_review": "PENDING", "consumer_compilation": "VALID-NOT-CHECKED",
           "mac_runtime": "NO-NEW-EXECUTION"}
(HERE / "verification-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
print(json.dumps(receipt, indent=2))
