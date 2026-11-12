"""Display exact assigned shared text in bounded pages. This assigns no verdict."""
import json
from pathlib import Path
import sys

here = Path(__file__).resolve().parent
run = here.parent.parent
root = next(p for p in here.parents if (p / ".git").exists())
packet = json.loads((run / "inventory/packets/B4-windows-support.json").read_text())
ids = set(packet["shared_read_unit_ids"])
units = [u for u in json.loads((run / "inventory/semantic-units.json").read_text()) if u["id"] in ids]
prefix, page = sys.argv[1], int(sys.argv[2])
chunks = []
for path in dict.fromkeys(u["path"] for u in units if u["path"].startswith(prefix)):
    text = (root / path).read_bytes().decode("utf-8-sig", errors="replace")
    for u in units:
        if u["path"] == path:
            body = text[u["start_character"]:u["end_character_exclusive"]]
            # Structural blank lines and repeated copyright carry no new contract.
            lines = [line.rstrip() for line in body.splitlines() if line.strip()]
            chunks.append(f"\n### {path}:{u['first_line']}-{u['last_line']} {u['id']}\n" + "\n".join(lines))
text = "\n".join(chunks)
size = int(sys.argv[3]) if len(sys.argv) > 3 else 28000
print(f"PAGE {page + 1}/{(len(text) + size - 1) // size}; exact selected text, character offsets {page * size}..{min((page + 1) * size, len(text))}")
print(text[page * size:(page + 1) * size])
