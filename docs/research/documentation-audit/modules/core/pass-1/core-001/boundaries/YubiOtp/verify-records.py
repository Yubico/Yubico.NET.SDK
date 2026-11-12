"""Read-only boundary record verification; no SDK, native, device or crypto execution."""
import collections
import hashlib
import json
from pathlib import Path
import subprocess

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[8]
RUN = HERE.parents[1]

def load(path):
    return json.loads(path.read_text())

def sha(data):
    return hashlib.sha256(data).hexdigest()

packet = load(RUN / 'inventory/packets/BOUNDARY-YubiOtp.json')
units = load(HERE / 'unit-dispositions.json')
files = load(HERE / 'file-dispositions.json')
index = load(HERE / 'candidate-index.json')
unchanged = load(HERE / 'unchanged-paths.json')
summary = load(HERE / 'coverage-summary.json')
assert len(units) == len({row['id'] for row in units}) == 324
assert {row['id'] for row in units} == set(packet['unit_ids'])
assert {row['path'] for row in files} == set(packet['disposition_paths'])
assert summary['counts'] == dict(collections.Counter(row['status'] for row in units))
assert all(row['status'] in {'READ-CLEAN', 'READ-CANDIDATE'} for row in units)
for row in units:
    raw = (ROOT / row['path']).read_bytes()
    text = raw.decode('utf-8-sig')
    assert sha(raw) == row['source_sha256'], row['path']
    assert sha(text[row['start_character']:row['end_character_exclusive']].encode()) == row['range_sha256'], row['id']
for row in load(HERE / 'file-identities.json'):
    raw = (ROOT / row['path']).read_bytes()
    pinned = subprocess.check_output(['git', 'show', f"{summary['source_baseline']}:{row['path']}"], cwd=ROOT)
    assert sha(raw) == row['raw_sha256']
    assert sha(pinned) == row['pinned_content_sha256']
    assert raw.decode('utf-8-sig').replace('\r\n','\n') == pinned.decode('utf-8-sig').replace('\r\n','\n')
for row in load(HERE / 'dependency-reads.json'):
    raw = (ROOT / row['path']).read_bytes()
    text = raw.decode('utf-8-sig')
    assert sha(raw) == row['raw_sha256']
    for selected in row['ranges']:
        assert sha(text[selected['start_character']:selected['end_character_exclusive']].encode()) == selected['range_sha256']
for row in index:
    assert row['accepted_id'] is None
    assert set(row['exact_unit_ids']) == {unit['id'] for unit in units if row['id'] in unit['candidates']}
    assert set(row['candidate_associated_unchanged_paths']) <= {record['path'] for record in unchanged}
for row in unchanged:
    assert row['reason'] and row['reopening_details']
    assert sha((ROOT / row['path']).read_bytes()) == row['sha256']
inherited = load(HERE / 'inherited-dispositions.json')
registry = load(RUN / 'inherited-findings/registry.json')
assert inherited['registry_sha256'] == sha((RUN / 'inherited-findings/registry.json').read_bytes())
assert len(inherited['rows']) == 14
for row in inherited['rows']:
    original = next(item for item in registry['rows'] if item['key'] == row['key'])
    assert row['authoritative_state_unchanged'] == original['current_state']
    assert row['authoritative_conclusion_unchanged'] == original['current_conclusion']
assert len(load(HERE / 'applet-findings-dispositions.json')) == 12
for row in load(HERE / 'source-identities.json'):
    if 'local_path' in row:
        path = ROOT / row['local_path']
    else:
        path = Path('/home/rupicola/Code/y/yubikey-manager') / row['path']
    assert sha(path.read_bytes()) == row['sha256']
capture = load(HERE / 'using-reference-capture.json')
assert sha((HERE / 'using-reference.original.md').read_bytes()) == capture['sha256']
for row in load(HERE / 'api-surface-search.json')['snapshots']:
    assert sha((ROOT / row['path']).read_bytes()) == row['sha256']
for row in load(HERE / 'clean-nominees.json'):
    unit = next(unit for unit in units if unit['id'] == row['id'])
    assert unit['status'] == 'READ-CLEAN'
    assert row['range_sha256'] == unit['range_sha256']
assert summary['build_test_formatter_native_device_crypto_executions'] == 0
print(json.dumps({'status':'PASS', 'exact_units':len(units), 'disposition_paths':len(files),
    'candidate_groups':len(index), 'inherited_keys':len(inherited['rows']),
    'clean_nominees':len(load(HERE / 'clean-nominees.json')),
    'qualification':'Static identities/accounting only; independent content review and all SDK/native/device/crypto executions remain unrun.'}, indent=2))
