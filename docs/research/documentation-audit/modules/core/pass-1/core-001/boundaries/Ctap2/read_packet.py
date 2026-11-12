"""Print pinned-inventory reading units; source identity is separate from semantic judgment."""
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
REPO = ROOT.parents[6]
packet = json.loads((ROOT / "inventory/packets/BOUNDARY-Ctap2.json").read_text())
ids = set(packet["unit_ids"])
units = [u for u in json.loads((ROOT / "inventory/semantic-units.json").read_text()) if u["id"] in ids]
patterns = sys.argv[1:]
for path in dict.fromkeys(u["path"] for u in units):
    if not any(pattern in path for pattern in patterns):
        continue
    text = (REPO / path).read_bytes().decode("utf-8-sig")
    for unit in (u for u in units if u["path"] == path):
        print(f"\n## {unit['id']} {path}:{unit['first_line']}-{unit['last_line']} {unit['label']}")
        print(text[unit["start_character"]:unit["end_character_exclusive"]])
