"""Separate records-only checker: identity/coverage/links, not independent technical review."""
import collections
import hashlib
import json
import pathlib
import re
import subprocess

HERE = pathlib.Path(__file__).resolve().parent
RUN = HERE.parents[1]
REPO = RUN.parents[6]
PIN = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"


def sha(data):
    return hashlib.sha256(data).hexdigest()


def read(name):
    return json.loads((HERE / name).read_text())


packet = json.loads((RUN / "inventory/packets/BOUNDARY-Ctap2.json").read_text())
units = read("unit-dispositions.json")
files = read("file-dispositions.json")
sources = read("source-identities.json")
inherited = read("inherited-dispositions.json")
summary = read("verification.json")
failures = []


def check(condition, message):
    if not condition:
        failures.append(message)


check(len(units) == len({u["id"] for u in units}) == 590, "unit count/duplicates")
check({u["id"] for u in units} == set(packet["unit_ids"]), "exact unit set")
check({f["path"] for f in files} == set(packet["disposition_paths"]), "candidate path set")
check(len(packet["paths"]) == 180 and len(files) == 181, "explicit packet list populations")
check(summary["disposition_counts"] == dict(collections.Counter(u["disposition"] for u in units)), "disposition summary")
for row in units:
    data = (REPO / row["path"]).read_bytes()
    text = data.decode("utf-8-sig")
    selected = text[row["start_character"]:row["end_character_exclusive"]]
    check(sha(data) == row["source_sha256"], f"source hash {row['id']}")
    check(sha(selected.encode()) == row["range_sha256"], f"range hash {row['id']}")
    check(row["read"] == "complete-assigned-unit-with-boundary-context", f"read status {row['id']}")
    check(row["independent_review"] == "PENDING", f"review status {row['id']}")
for row in files + read("dependency-identities.json"):
    data = (REPO / row["path"]).read_bytes()
    pinned = subprocess.check_output(["git", "show", f"{PIN}:{row['path']}"], cwd=REPO)
    check(sha(data) == row["source_sha256"], f"file hash {row['path']}")
    check(data.decode("utf-8-sig").replace("\r\n", "\n") == pinned.decode("utf-8-sig").replace("\r\n", "\n"), f"pin {row['path']}")
for row in sources:
    check(sha((REPO / row["path"]).read_bytes()) == row["sha256"], f"reused source {row['path']}")
inputs = json.loads((RUN / "inventory/inherited-packet-inputs.json").read_text())["BOUNDARY-Ctap2"]
check({r["key"] for r in inherited} == set(inputs["provenance_keys"]), "inherited keys")
check(len(inherited) == 26, "inherited count")
for row in read("candidate-index.json"):
    check(row["accepted_core_id"] is None, "accepted ID allocation")
    check(bool(row["assigned_unit_ids"]), f"unmapped candidate {row['id']}")
for row in read("clean-nominees.json"):
    check(row["disposition"] == "READ-CLEAN", f"nonclean nominee {row['id']}")
for path in [*HERE.glob("*.md"), RUN / "boundary-Ctap2.md"]:
    text = path.read_text()
    check(sum(line.startswith("```") for line in text.splitlines()) % 2 == 0, f"fences {path.name}")
    for link in re.findall(r"\]\(([^)]+)\)", text):
        if re.match(r"(?:https?://|#)", link):
            continue
        check((path.parent / link.split("#")[0]).exists(), f"link {path.name}: {link}")
owned = [*HERE.glob("*"), RUN / "boundary-Ctap2.md"]
receipt = {
    "kind": "separate-static-record-consistency-check",
    "source_baseline": PIN,
    "assigned_units": len(units),
    "selected_paths": len(packet["paths"]),
    "candidate_paths": len(files),
    "inherited_keys": len(inherited),
    "clean_nominees": len(read("clean-nominees.json")),
    "failures": failures,
    "sdk_native_crypto_device_execution": False,
    "independent_content_review": False,
    "owned_record_identities": [{"path": str(p.relative_to(REPO)), "sha256": sha(p.read_bytes()), "bytes": p.stat().st_size} for p in owned if p.is_file() and p.name != "verification-receipt.json"],
}
(HERE / "verification-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
print(json.dumps({key: value for key, value in receipt.items() if key != "owned_record_identities"}, indent=2))
raise SystemExit(bool(failures))
