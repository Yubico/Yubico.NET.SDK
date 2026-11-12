#!/usr/bin/env python3
"""Print one proposed whole original sample; performs no sampling judgment or writes."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys

RUN = Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
HERE = RUN / 'sampling/preparation'
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
sha = lambda b: hashlib.sha256(b).hexdigest()
if len(sys.argv) not in [2, 3] or len(sys.argv) == 3 and sys.argv[2] != '--current-lines':
    raise SystemExit('Usage: python -B .../read-unit.py core-001:unit:<ID> [--current-lines]')
rows = json.loads((HERE / 'selection.json').read_text())
row = next((r for r in rows if r['id'] == sys.argv[1]), None)
if row is None: raise SystemExit('ID is outside the proposed selected set; consult population.json/selection.json.')
path = row['path']
data = subprocess.check_output(['git', 'show', PIN + ':' + path])
candidates = [data]
if sha(data) != row['source_sha256']:
    text = data.decode('utf-8-sig').replace('\r\n', '\n')
    candidates += [(bom + text.replace('\n', newline)).encode() for bom in ['', '\ufeff'] for newline in ['\n', '\r\n']]
    current = Path(path).read_bytes()
    if sha(current) == row['source_sha256'] and current.decode('utf-8-sig').replace('\r\n', '\n') == text: candidates.append(current)
raw = next((b for b in candidates if sha(b) == row['source_sha256']), None)
if raw is None: raise SystemExit('Original representation unavailable: stop rather than substitute edited current text.')
original = raw.decode('utf-8-sig', errors='replace')
body = original[row['start_character']:row['end_character_exclusive']]
assert sha(body.encode()) == row['range_sha256']
print(f"PROPOSED SAMPLE — no verdict: {row['id']} / {row['owner']} / {row['stratum']}")
print(f"Original {PIN}:{path}:{row['first_line']}–{row['last_line']}")
print('Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:')
print(body)
mapping = row['current_mapping']
print('Current primary mapping:', json.dumps(mapping))
if len(sys.argv) == 3:
    if mapping['state'] != 'UNCHANGED-LINES-MAPPED': raise SystemExit('Current mapping requires explicit coordinator reconciliation; original unit printed above.')
    current = Path(path).read_bytes()
    assert sha(current) == mapping['current_file_sha256'], 'Current source changed since selection; refresh mapping before reading current context.'
    lines = current.decode('utf-8-sig').splitlines()
    print('Current complete intersecting lines (can include adjacent boundary-line context):')
    for number in range(mapping['first_line'], mapping['last_line'] + 1): print(f'{number}: {lines[number - 1]}')
