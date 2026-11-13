#!/usr/bin/env python3
"""Prepare an explicit reviewed staging manifest without committing or publishing."""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess

root = Path(__file__).resolve().parents[1].relative_to(Path.cwd())
def git(*args):
    return subprocess.check_output(['git', *args])
def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()
assert not git('ls-files', '--unmerged')
assert git('rev-parse', 'HEAD').decode().strip() == 'd5d01ef99f1478e4d49911194c17667b2450d2fd'
assert git('rev-parse', 'MERGE_HEAD').decode().strip() == 'e322712d609aaf03b9bd1cf2964a99326728866a'
assert not git('diff', '--name-only')
receipt_paths = set()
for receipt in sorted((root / 'validation').glob('*/receipt.json')):
    for row in json.loads(receipt.read_text()).get('files', []):
        path = receipt.parent / row['path']
        receipt_paths.add(path.as_posix())
        data = git('show', ':' + path.as_posix())
        assert len(data) == row['bytes'] and hashlib.sha256(data).hexdigest() == row['sha256'], path
ignored = git('ls-files', '--others', '--ignored', '--exclude-standard', root.as_posix()).decode().splitlines()
assert not [p for p in ignored if not {'bin', 'obj', '__pycache__'} & set(Path(p).parts)], ignored
pre = json.loads((root / 'execution/pre-merge.json').read_text())
unchanged = []
for row in pre['preparation_files']:
    if row['path'] == (root / 'status.md').as_posix():
        continue
    assert sha(Path(row['path'])) == row['sha256'], row['path']
    unchanged.append(row['path'])
shutil.copy2('docs/plans/2026-11-13-windows-refactor-integration.md', root / 'execution/final-plan-snapshot.md')
product_paths = [p for p in git('diff', '--cached', '--name-only', '--no-renames').decode().splitlines()
                 if not p.startswith(root.as_posix() + '/')]
packet_paths = sorted(set(git('ls-files', '--cached', '--others', '--exclude-standard', root.as_posix()).decode().splitlines())
                      - {(root / 'execution/publication-manifest.json').as_posix()})
assert all(p.startswith(root.as_posix() + '/') for p in packet_paths)
assert not any({'bin', 'obj', '__pycache__'} & set(Path(p).parts) for p in packet_paths)
assert len(product_paths) == 135, len(product_paths)
message = '''feat(core): integrate Windows native owners with the audited SDK

Merge the received Windows HID and adapter-consistency delivery with the
published audit history. Preserve audited runtime corrections and frozen
records; reconcile current ownership guidance and source-bound software evidence.

Complexity-Justification: HidConnectionSlot.OpenBuiltInConnectionAsync: Keep the six platform/transport routes and their claim transfer visible in one flat switch; cyclomatic 12, cognitive 1, with no additional lifecycle state machine.
Complexity-Justification: WindowsFidoHidNativeOwner.CompleteRequest: Keep cancellation/error reconciliation, exact retirement, owned-buffer clearing, and actual public task publication together so their ordering remains auditable.
Complexity-Justification: WindowsFidoHidNativeOwner.Advance: Keep the submit-return/completion join and once-only worker cancellation in one state transition method; these branches encode the frozen independent native observations.
'''
(root / 'execution/intended-commit-message.txt').write_text(message)
packet_paths = sorted(set(packet_paths + [(root / 'execution/intended-commit-message.txt').as_posix()]))
paths = sorted(set(product_paths + packet_paths))
rows = [{'path': p, 'sha256': sha(Path(p)) if Path(p).is_file() else None,
         'kind': 'integration-record' if p.startswith(root.as_posix() + '/') else 'product-or-incoming-tooling'} for p in paths]
for row in rows:
    if row['sha256'] is not None:
        row['git_blob_oid'] = git('hash-object', '--path=' + row['path'], row['path']).decode().strip()
manifest = root / 'execution/publication-manifest.json'
record = {'actor': 'GPT-6.1 Sol', 'status': 'prepared, awaiting independent review; no commit/push',
          'product_tree_after_final_docs': json.loads((root / 'validation/final-documentation/receipt.json').read_text())['candidate_tree'],
          'product_paths': len(product_paths), 'packet_paths_excluding_manifest': len(packet_paths),
          'receipt_entries_verified_against_staged_bytes': len(receipt_paths),
          'preserved_preparation_paths': unchanged, 'files': rows,
          'self_reference': 'This manifest excludes itself; its reviewed blob and final tree are pinned by the review checkpoint.'}
manifest.write_text(json.dumps(record, indent=2) + '\n')
subprocess.run(['git', 'add', '--', *packet_paths, manifest.as_posix()], check=True)
for row in rows:
    if row['sha256'] is not None:
        assert git('rev-parse', ':' + row['path']).decode().strip() == row['git_blob_oid']
assert not git('diff', '--name-only')
assert not git('ls-files', '--others', '--exclude-standard')
subprocess.run(['git', 'diff', '--cached', '--check'], check=True)
print(json.dumps({'review_tree': git('write-tree').decode().strip(), 'manifest_sha256': sha(manifest),
                  'product_paths': len(product_paths), 'record_paths_including_manifest': len(packet_paths) + 1,
                  'preserved_preparation_paths': len(unchanged)}, indent=2))
