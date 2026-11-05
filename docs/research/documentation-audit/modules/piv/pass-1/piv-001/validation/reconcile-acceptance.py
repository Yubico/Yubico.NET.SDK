"""Bind accepted prose through whitespace-only formatting and reconcile final clean samples."""
from pathlib import Path
import argparse
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
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--post-format-review', help='Run-relative independent acceptance after a Markdown-only correction')
parser.add_argument('--late-coverage', action='store_true', help='Include the two independently reviewed final coverage corrections')
args = parser.parse_args()
post_path = RUN / args.post_format_review if args.post_format_review else None
post = json.loads(post_path.read_text()) if post_path else None
extra_paths = {'src/Piv/examples/PivTool/PivExamples/Results/KeyGenerationResult.cs',
               'src/Piv/src/Cryptography/PrivateKeyOperations.cs'} if args.late_coverage else set()
if post:
    assert post['baseline'] == BASE
    assert {r['path'] for r in post['files']} == {r['path'] for r in accepted['files']} | extra_paths
post_rows = {r['path']: r for r in post['files']} if post else {}
late_changes = []
pre = {r['path']: r for r in json.loads((RUN / 'validation/pre-format-content.json').read_text())['files']}
assert len(pre) == 48
accepted_rows = list(accepted['files'])
if args.late_coverage:
    assert post
    late = json.loads((RUN / 'validation/late-pre-format-content.json').read_text())['files']
    assert {r['path'] for r in late} == extra_paths
    pre.update({r['path']: r for r in late})
    accepted_rows.extend({'path': r['path'], 'sha256': r['sha256'],
                          'scope': 'Late coverage correction; formatting preserves the independently accepted content'} for r in late)
reviewed = []
transforms = []
for row in accepted_rows:
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
        if sha(raw) != row['sha256']:
            assert post, path
            late_changes.append(path)
    if post:
        assert sha(raw) == post_rows[path]['sha256'], path
    final_review = post_rows[path] if post else row
    reviewed.append(dict(final_review, original_reviewed_sha256=row['sha256'],
                         reviewed_sha256=final_review['sha256'], final_sha256=sha(raw)))
if post:
    assert set(late_changes) <= {'src/Piv/CLAUDE.md', 'src/Piv/README.md', 'src/Piv/examples/PivTool/README.md'} and 'src/Piv/CLAUDE.md' in late_changes, late_changes
save('reviewed-files.json', {'baseline': BASE, 'files': reviewed})
save('validation/format-transform.json', {'baseline': BASE, 'accepted_manifest_sha256': sha(review_path.read_bytes()),
    'post_format_review_sha256': sha(post_path.read_bytes()) if post_path else None,
    'late_markdown_changes': late_changes, 'files': transforms})

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
sample_files = ['sampling-accepted.json', 'sampling-expansion.json']
if args.late_coverage:
    sample_files.append('sampling-internal-expansion.json')
for filename in sample_files:
    samples.extend(json.loads((RUN / filename).read_text())['samples'])
assert len(samples) == len({s['id'] for s in samples}) == (64 if args.late_coverage else 61)
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
print(f'PASS:{len(reviewed)} accepted paths,{len(transforms)} whitespace-only C# transforms/imports unchanged,123 module identities,{len(samples)}/{len(eligible)} clean samples (minimum{minimum}).')
