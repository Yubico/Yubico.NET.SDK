#!/usr/bin/env python3
"""Retain only the exact receipt-listed logs omitted from the predecessor publication tree."""
import hashlib
import json
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[1].relative_to(Path.cwd())
def git(*args):
    return subprocess.check_output(['git', *args])
def sha(data):
    return hashlib.sha256(data).hexdigest()
review = root / 'review/final-publication-review.md'
assert sha(review.read_bytes()) == '0517e6c69c6401bdb2ccabcb8dc548886bfdfc76742e8aea471ad27223bc50d3'
manifest = root / 'execution/publication-manifest.json'
predecessor = root / 'execution/publication-manifest-before-log-retention.json'
assert sha(manifest.read_bytes()) == '2e0651bca9cb2696929ba239ed250ffc940c0b4d043fa132dc9d97bc2df0f5a1'
with predecessor.open('xb') as output:
    output.write(manifest.read_bytes())
entries = []
missing = []
for receipt in sorted((root / 'validation').glob('*/receipt.json')):
    for row in json.loads(receipt.read_text()).get('files', []):
        path = receipt.parent / row['path']
        data = path.read_bytes()
        assert len(data) == row['bytes'] and sha(data) == row['sha256'], path
        if subprocess.run(['git', 'cat-file', '-e', ':' + path.as_posix()], capture_output=True).returncode:
            missing.append(path.as_posix())
        entries.append({'receipt': receipt.as_posix(), 'path': path.as_posix(),
                        'bytes': row['bytes'], 'sha256': row['sha256']})
assert len(entries) == 67 and len(missing) == 40
assert all(p.endswith('.log') and '/toolchain/' in p for p in missing)
ignored_before = git('ls-files', '--others', '--ignored', '--exclude-standard', root.as_posix()).decode().splitlines()
other = [p for p in ignored_before if p not in missing and not {'bin', 'obj', '__pycache__'} & set(Path(p).parts)]
assert not other, other
subprocess.run(['git', 'add', '-f', '--', *missing], check=True)
for row in entries:
    data = git('show', ':' + row['path'])
    assert len(data) == row['bytes'] and sha(data) == row['sha256'], row['path']
record = {'actor': 'GPT-6.1 Sol', 'scope': 'evidence packaging only; no runtime, formatter or product edits',
          'predecessor_tree': '7e8b4b40111b65231470a06207cf9700f12f7015',
          'predecessor_manifest_sha256': sha(predecessor.read_bytes()),
          'review_sha256': sha(review.read_bytes()), 'force_added_exact_paths': missing,
          'all_receipt_entries_staged_verified': entries,
          'ignored_audit': {'before_count': len(ignored_before), 'other_evidence_omissions': other,
                            'excluded_generated_paths': [p for p in ignored_before if p not in missing]},
          'self_reference': 'Final tree and refreshed manifest identity belong to the external rereview checkpoint.'}
destination = root / 'execution/log-retention-correction.json'
with destination.open('x') as output:
    output.write(json.dumps(record, indent=2) + '\n')
print('Explicitly retained 40 logs; all 67 receipt entries match staged byte counts and SHA-256.')
print('No other ignored evidence omission; predecessor manifest and review preserved.')
