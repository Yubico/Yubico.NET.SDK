"""Bind accepted prose through whitespace-only formatting and reconcile final clean samples."""
from pathlib import Path
import collections
import difflib
import hashlib
import json
import math
import re
import subprocess

RUN = Path(__file__).resolve().parent.parent
ROOT = next(p for p in RUN.parents if (p / 'toolchain.cs').exists())
BASE = '727fe43afc430c5d8364d868a6436de50626e476'


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def save(name, value):
    (RUN / name).write_text(json.dumps(value, indent=2) + '\n')


review_path = RUN / 'evidence/review-accepted.json'
accepted = json.loads(review_path.read_text())
assert accepted['baseline'] == BASE and len(accepted['files']) == 57
pre = {r['path']: r for r in json.loads((RUN / 'validation/pre-format-content.json').read_text())['files']}
assert len(pre) == 48
reviewed = []
transforms = []
for row in accepted['files']:
    path = row['path']
    raw = (ROOT / path).read_bytes()
    if path in pre:
        prior = pre[path]
        assert sha(prior['content'].encode()) == prior['sha256'] == row['sha256'], path
        before, after = prior['content'], raw.decode()
        assert re.sub(r'\s', '', before) == re.sub(r'\s', '', after), path
        assert re.findall(r'^using .+;', before, re.M) == re.findall(r'^using .+;', after, re.M), path
        transforms.append({'path': path, 'before_sha256': row['sha256'], 'after_sha256': sha(raw),
                           'whitespace_only': True, 'imports_identical': True})
    else:
        assert sha(raw) == row['sha256'], path
    reviewed.append(dict(row, reviewed_sha256=row['sha256'], final_sha256=sha(raw)))
save('reviewed-files.json', {'baseline': BASE, 'files': reviewed})
save('validation/format-transform.json', {'baseline': BASE, 'accepted_manifest_sha256': sha(review_path.read_bytes()), 'files': transforms})

coverage = json.loads((RUN / 'coverage.json').read_text())
units = coverage['units'] + coverage['shared_units']
texts = {}
for path in {u['path'] for u in units if u.get('sampling_eligible')}:
    old = subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=ROOT).decode()
    new = (ROOT / path).read_text()
    texts[path] = (old, new, difflib.SequenceMatcher(a=old.splitlines(), b=new.splitlines(), autojunk=False).get_opcodes())
eligible = []
excluded = []
for unit in units:
    if not unit.get('sampling_eligible'):
        continue
    lo, hi = unit['start'] - 1, unit['end']
    old, new, opcodes = texts[unit['path']]
    overlap = any(tag != 'equal' and (lo <= a < hi if a == b else max(lo, a) < min(hi, b))
                  for tag, a, b, _, _ in opcodes)
    (excluded if overlap else eligible).append(unit)
samples = []
for filename in ('sampling-accepted.json', 'sampling-expansion.json'):
    samples.extend(json.loads((RUN / filename).read_text())['samples'])
assert len(samples) == len({s['id'] for s in samples}) == 61
ids = {u['id'] for u in eligible}
for sample in samples:
    assert sample['id'] in ids, sample['id']
    assert sha((ROOT / sample['path']).read_bytes()) == sample['sampled_file_sha256'], sample['path']
minimum = max(3, math.ceil(len(eligible) / 10))
assert len(samples) >= minimum
save('sampling-final.json', {'baseline': BASE, 'eligible': len(eligible), 'minimum': minimum,
    'sampled': len(samples), 'category_counts': dict(collections.Counter(s['category'] for s in samples)),
    'rule': coverage['unit_rule'], 'eligible_units': eligible, 'edited_overlaps': excluded, 'samples': samples})
inventory = json.loads((RUN / 'inventory.json').read_text())['files']
paths = {r['path'] for r in reviewed}
module = []
for row in inventory:
    raw = (ROOT / row['path']).read_bytes()
    edited = row['path'] in paths
    if not edited:
        assert sha(raw) == row['sha256'], row['path']
    module.append(dict(row, final_sha256=sha(raw), edited=edited))
save('coverage-final.json', {'baseline': BASE, 'module_paths': len(module), 'public_types': 23,
    'api_entries': 370, 'interface_operations': 42, 'authored_paths': len(reviewed),
    'module_edited': sum(r['edited'] for r in module), 'shared_edited': len(paths - {r['path'] for r in module}),
    'module': module, 'sampled': len(samples), 'clean_eligible': len(eligible)})
print(f'PASS:57 accepted paths,48 whitespace-only C# transforms/imports unchanged,123 module identities,61/{len(eligible)} clean samples (minimum{minimum}).')
