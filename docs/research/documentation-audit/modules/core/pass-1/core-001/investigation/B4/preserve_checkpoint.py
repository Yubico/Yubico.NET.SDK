"""Preserve checkpoint 1 before successor records; never overwrites history."""
import hashlib
import json
from pathlib import Path

here = Path(__file__).resolve().parent
history = here / "history/checkpoint-1"
history.mkdir(parents=True, exist_ok=True)
paths = [here.parent.parent / "investigation-B4.md"] + [here / name for name in [
    "unit-dispositions.json", "coverage-summary.json", "continuation.json",
    "file-evidence.md", "file-identities.json", "candidate-index.json", "candidates.md",
    "continuation-and-verification.md", "preservation-verification.json",
    "checkpoint-verification.json", "record_checkpoint.py", "verify_checkpoint.py"]]
manifest = []
for path in paths:
    data = path.read_bytes()
    dest = history / path.name
    if dest.exists():
        assert dest.read_bytes() == data, f"History already exists with different bytes: {dest}"
    else:
        dest.write_bytes(data)
    manifest.append({"original": str(path.relative_to(here.parent.parent)), "history": dest.name,
                     "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()})
out = history / "manifest.json"
if not out.exists():
    out.write_text(json.dumps(manifest, indent=2) + "\n")
print(f"Preserved {len(manifest)} checkpoint records without changing their bytes.")
