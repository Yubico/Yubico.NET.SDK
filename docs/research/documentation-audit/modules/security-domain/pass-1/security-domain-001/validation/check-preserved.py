"""Read-only comparison of prior campaign archives and initial user work."""
from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
BASE = ROOT / 'docs/research/documentation-audit'
archives = [
    (BASE / 'record-manifest.json', BASE / 'records'),
    (BASE / 'modules/management/pass-1/management-001/record-manifest.json', BASE / 'modules/management/pass-1/management-001'),
    (BASE / 'modules/ctap2/pass-1/ctap2-001/record-manifest.json', BASE / 'modules/ctap2/pass-1/ctap2-001'),
]
for manifest, folder in archives:
    rows = json.loads(manifest.read_text())['files']
    for row in rows:
        data = (folder / row['path']).read_bytes()
        assert len(data) == row['bytes'] and hashlib.sha256(data).hexdigest() == row['sha256'], row['path']
    print(f'PASS: {len(rows)} prior archive records: {manifest.relative_to(ROOT)}')
snapshot = HERE / 'user-work-baseline.json'
for row in json.loads(snapshot.read_text()):
    data = (ROOT / row['path']).read_bytes()
    assert len(data) == row['bytes'] and hashlib.sha256(data).hexdigest() == row['sha256'], row['path']
print('PASS: four original untracked files unchanged.')
