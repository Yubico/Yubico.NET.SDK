"""Read exact assigned character ranges. Static text only; no SDK execution."""
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[9]
RUN = ROOT / 'docs/research/documentation-audit/modules/core/pass-1/core-001'
PACKET = json.loads((RUN / 'inventory/packets/BOUNDARY-WebAuthn.json').read_text())
IDS = set(PACKET['unit_ids'])
UNITS = [u for u in json.loads((RUN / 'inventory/semantic-units.json').read_text()) if u['id'] in IDS]

if __name__ == '__main__':
    start, stop = map(int, sys.argv[1:3])
    for i, unit in enumerate(UNITS[start:stop], start):
        text = (ROOT / unit['path']).read_bytes().decode('utf-8-sig')
        print(f"\nUNIT {i} {unit['id']} {unit['path']}:{unit['first_line']}-{unit['last_line']} {unit['label']}")
        print(text[unit['start_character']:unit['end_character_exclusive']])
