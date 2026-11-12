"""Preserve the released incomplete checkpoint before final continuation edits."""
import hashlib
import json
import pathlib

here = pathlib.Path(__file__).resolve().parent
destination = here / "history/checkpoint-48-files"
destination.mkdir(parents=True, exist_ok=True)
manifest = []
paths = sorted(p for p in here.iterdir() if p.is_file() and p.name != __file__.split("/")[-1])
paths.append(here.parent.parent / "investigation-B6.md")
for path in paths:
    data = path.read_bytes()
    target = destination / path.name
    if target.exists():
        assert target.read_bytes() == data, "Checkpoint already preserved; do not overwrite it."
    else:
        target.write_bytes(data)
    manifest.append({"original_path": str(path.relative_to(here.parent.parent)), "snapshot": path.name,
                     "sha256": hashlib.sha256(data).hexdigest(), "bytes": len(data)})
(destination / "manifest.json").write_text(json.dumps({"status": "HISTORICAL-INCOMPLETE-CHECKPOINT", "files": manifest}, indent=2) + "\n")
print(f"Preserved {len(manifest)} incomplete-checkpoint artifacts.")
