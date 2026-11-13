"""Read-only bounded example delta selection; no generation, compilation or execution."""
import hashlib
import json
from pathlib import Path
import re
import subprocess

RUN = Path(__file__).resolve().parents[1]
ROOT = RUN.parents[4]
BASE = "d5d01ef99f1478e4d49911194c17667b2450d2fd"
decision = json.loads((RUN / "merge-review/decision-map.json").read_text())
paths = {p for row in decision["overlaps"] for p in row["paths"]}
paths.add("docs/architecture/connection-ownership-and-contention.md")
# Also inspect fresh extraction recipients, not just predecessor conflict containers.
for row in decision["overlaps"]:
    paths.update(row.get("new_owners", []))
fence = re.compile(r"^```csharp[^\n]*\n(.*?)^```\s*$", re.M | re.S)
xml = re.compile(r"<code(?:\s[^>]*)?>(.*?)</code>", re.S)
markers = re.compile(r"^(?:<<<<<<< |=======\s*$|>>>>>>> |\|\|\|\|\|\|\| )", re.M)
result = []
for path in sorted(paths):
    before = subprocess.run(["git", "show", f"{BASE}:{path}"], cwd=ROOT,
                            capture_output=True, check=False)
    old = before.stdout.decode() if before.returncode == 0 else ""
    current = ROOT / path
    new = current.read_text() if current.is_file() else ""
    pattern = fence if path.endswith(".md") else xml
    old_bodies, new_bodies = pattern.findall(old), pattern.findall(new)
    result.append({"path": path, "exists": current.is_file(),
                   "unresolved_marker": bool(markers.search(new)),
                   "baseline_bodies": len(old_bodies), "current_bodies": len(new_bodies),
                   "body_sequences_equal": old_bodies == new_bodies,
                   "current_body_sha256": [hashlib.sha256(b.encode()).hexdigest() for b in new_bodies]})

# Original accepted wrappers are read, never imported/run/rebound. Old absolute paths
# identify the original checkout; map only their repository-relative suffix for inspection.
closeout = ROOT / "docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full/closeout/selected-bodies.json"
accepted = json.loads(closeout.read_text())
retired = re.compile(r"\b(?:UsbSmartCardConnection|PcscConnectionNativeState|IIOKitDeviceLifetime|IOKitDeviceLifetime|IHidInputBridge|HidInputBridge|IWindowsHidReportAccess|WindowsHidReportAccess)\b")
dependencies = []
for row in accepted["entries"]:
    original = row["source"]["path"]
    relative = original.split("/Yubico.Yubikit.NET/", 1)[1]
    generated = ROOT / relative
    names = sorted(set(retired.findall(generated.read_text())))
    if names:
        dependencies.append({"id": row["body_id"], "context": row["context"],
                             "generated_source": relative, "retired_name_matches": names})
print(json.dumps({"baseline": BASE, "scope": "19 overlap records, live recipients and ownership diagram; accepted Full wrappers read-only",
                  "paths": result,
                  "changed_example_paths": [r["path"] for r in result if not r["body_sequences_equal"]],
                  "accepted_wrapper_rename_candidates": dependencies,
                  "execution": "none; no compilation claim; unresolved source blocks release"}, indent=2))
