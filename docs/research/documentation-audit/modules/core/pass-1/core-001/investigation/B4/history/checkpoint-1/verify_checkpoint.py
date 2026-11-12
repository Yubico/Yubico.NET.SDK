"""Read-only checkpoint validation; writes only its B4 verification result."""
import collections
import hashlib
import json
from pathlib import Path
import re

HERE = Path(__file__).resolve().parent
RUN = HERE.parent.parent
ROOT = next(parent for parent in HERE.parents if (parent / ".git").exists())


def sha(data):
    return hashlib.sha256(data).hexdigest()


def load(path):
    return json.loads(path.read_text())


packet = load(RUN / "inventory/packets/B4-windows-support.json")
units = load(HERE / "unit-dispositions.json")
summary = load(HERE / "coverage-summary.json")
remaining = load(HERE / "continuation.json")
failures = []


def check(condition, message):
    if not condition:
        failures.append(message)


expected = set(packet["unit_ids"] + packet["shared_read_unit_ids"])
check(len(units) == len({u["id"] for u in units}) == len(expected) == 2350, "ID count/uniqueness")
check({u["id"] for u in units} == expected, "exact assigned ID set")
outstanding = {u["id"] for u in units if u["status"] in {"UNREAD", "READ-PENDING-CONTEXT"}}
check(set(remaining["remaining_unit_ids"]) == outstanding and len(outstanding) == 1462, "continuation exact set")
check(summary["complete_primary_file_reads"] == 77 and summary["unread_primary_files"] == 29, "primary file checkpoint")
check(sum(f["files"] for f in summary["families"]) == 106, "family file reconciliation")
check(sum(f["lines"] for f in summary["families"]) == 14842, "family line reconciliation")
check(dict(collections.Counter(u["status"] for u in units)) == summary["combined_status_counts"], "status reconciliation")
inventory = {u["id"]: u for u in load(RUN / "inventory/semantic-units.json")}
cache = {}
for u in units:
    source = inventory[u["id"]]
    data = cache.setdefault(u["path"], (ROOT / u["path"]).read_bytes())
    check(sha(data) == u["source_sha256"] == source["source_sha256"], f"source hash {u['id']}")
    text = data.decode("utf-8-sig", errors="replace")
    actual = sha(text[u["start_character"]:u["end_character_exclusive"]].encode())
    check(actual == u["range_sha256"] == source["range_sha256"], f"range hash {u['id']}")
    check(u["sample_eligible"] == (u["status"] == "READ-CLEAN"), f"sample eligibility {u['id']}")
    check(u["status"] != "READ-CLEAN" or u["complete_context_read"], f"clean without read {u['id']}")
    check(u["status"] != "UNREAD" or not u["candidate_ids"], f"unread candidate {u['id']}")

capture_rows = load(HERE / "sources/captures.json")
for row in capture_rows:
    check(row.get("status") == 200, f"capture failure {row['name']}")
    path = HERE / "sources" / row["name"]
    check(path.is_file() and sha(path.read_bytes()) == row.get("sha256"), f"capture hash {row['name']}")

tree = load(RUN / "investigation/B3/sources/native-release-tree.json")
tree_blobs = {f["path"]: f["sha"] for f in tree["tree"] if f["type"] == "blob"}
producer_checks = []
for local, original in {
    "shim-pcsc.c": "pcsc.c", "shim-native_abi.h": "native_abi.h", "shim-CMakeLists.txt": "CMakeLists.txt",
    "shim-exports.msvc": "exports.msvc", "shim-nuspec.xml": "Yubico.NativeShims.nuspec",
    "shim-header.in": "Yubico.NativeShims.h.in", "shim-build-windows.ps1": "build-windows.ps1",
    "shim-pcscd.cmake": "cmake/pcscd.cmake",
}.items():
    data = (HERE / "sources" / local).read_bytes()
    blob = hashlib.sha1(f"blob {len(data)}\0".encode() + data).hexdigest()
    expected_blob = tree_blobs[f"Yubico.NativeShims/{original}"]
    check(blob == expected_blob, f"published source Git blob {local}")
    producer_checks.append({"local": local, "release_path": f"Yubico.NativeShims/{original}", "actual_git_blob": blob, "expected_git_blob": expected_blob, "match": blob == expected_blob})

for path in [RUN / "investigation-B4.md", HERE / "candidates.md", HERE / "continuation-and-verification.md", HERE / "sources/register.md"]:
    for link in re.findall(r"\]\(([^)]+)\)", path.read_text()):
        if "://" not in link and not link.startswith("#"):
            check((path.parent / link.split("#")[0]).exists(), f"local link {path.name}: {link}")

result = {"status": "PASSED-RECORD-IDENTITIES" if not failures else "FAILED", "packet_completion": "CONTINUATION-REQUIRED",
          "assigned_units": len(units), "outstanding_units": len(outstanding), "source_capture_count": len(capture_rows),
          "published_shim_source_blob_checks": producer_checks, "failures": failures,
          "qualification": "Static record/identity verification only; no semantic completeness, independent review, compiler/native/device result or installed native producer attestation."}
(HERE / "checkpoint-verification.json").write_text(json.dumps(result, indent=2) + "\n")
print(json.dumps({key: value for key, value in result.items() if key != "published_shim_source_blob_checks"}, indent=2))
if failures:
    raise SystemExit(1)
