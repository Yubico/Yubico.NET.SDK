"""Freeze/verify text evidence and exactly registered primary-source PDFs; never overwrite."""
from pathlib import Path
import hashlib
import json
import sys

RUN = Path(__file__).resolve().parent.parent
MANIFEST = RUN / 'record-manifest.json'
BASELINE = 'a74792facb657413e98223e059876d8b213ffff3'

def require(condition, message):
    if not condition:
        raise SystemExit(message)

def inventory():
    rows = []
    primary = {r['path']: r for r in json.loads((RUN / 'validation/primary-binary-sources.json').read_text())}
    seen = set()
    for path in sorted(RUN.rglob('*')):
        require(not path.is_symlink(), f'Symlinked archive entry: {path}')
        if not path.is_file() or path == MANIFEST:
            continue
        relative = path.relative_to(RUN)
        require(not any(p.lower() in ('bin', 'obj', '__pycache__', '.cache') for p in relative.parts), f'Generated/cache path: {path}')
        require(path.suffix.lower() not in ('.dll', '.exe', '.pdb', '.binlog', '.nupkg', '.snupkg', '.pyc', '.cache'), f'Binary/cache artifact: {path}')
        raw = path.read_bytes()
        kind = 'text'
        if str(relative) in primary:
            expected = primary[str(relative)]
            require(raw.startswith(b'%PDF-') and len(raw) == expected['bytes'] and hashlib.sha256(raw).hexdigest() == expected['sha256'], f'Primary source changed: {path}')
            kind = 'primary-source-pdf'
            seen.add(str(relative))
        else:
            raw.decode('utf-8')
            require(b'\0' not in raw, f'NUL-containing archive entry: {path}')
        rows.append(dict(path=str(relative), bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest(), kind=kind))
    require(seen == set(primary), 'Registered primary sources missing')
    return rows

require(sys.argv[1:] in (['write'], ['verify']), 'Usage: manifest.py write|verify')
rows = inventory()
if sys.argv[1] == 'write':
    require(not MANIFEST.exists(), 'Manifest already exists; refusing to overwrite frozen evidence')
    record = dict(run='yubihsm-001', pass_number=1, baseline=BASELINE,
                  record_count=len(rows), total_bytes=sum(r['bytes'] for r in rows),
                  scope='All run artifacts excluding this manifest; text sources/configuration/provenance/results and explicitly registered primary-source PDFs retained (allowlist initially empty); build binaries/runtime caches excluded.', files=rows)
    with MANIFEST.open('x') as output:
        json.dump(record, output, indent=2)
        output.write('\n')
require(not MANIFEST.is_symlink(), 'Symlinked manifest')
record = json.loads(MANIFEST.read_text())
require(record['run'] == 'yubihsm-001' and record['pass_number'] == 1 and record['baseline'] == BASELINE, 'Manifest run identity mismatch')
require(record['files'] == rows, 'Manifest path/hash/size/kind set differs from current archive')
require(record['record_count'] == len(rows) and record['total_bytes'] == sum(r['bytes'] for r in rows), 'Manifest totals mismatch')
print(f"PASS: {len(rows)} retained run artifacts, {record['total_bytes']} bytes; exact path/hash/size/kind set reconciled.")
