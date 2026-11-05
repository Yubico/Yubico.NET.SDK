"""Bind application namespaces to generated hosts while preserving the original extraction checkpoint."""
from pathlib import Path
import json
import sys

sys.dont_write_bytecode = True
from inventory import HERE, ROOT, RUN, sha, file_record, require

folder = HERE / 'v2'
receipt_path = folder / 'host-scope-1/receipt.json'
require(sys.argv[1:] in (['prepare'], ['verify']), 'Usage: host-scope-v2.py prepare|verify')


def retain(path, raw):
    require(not (RUN / 'record-manifest.json').exists(), 'Run is frozen')
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('xb') as output:
        output.write(raw)


def write_json(path, value):
    path.write_text(json.dumps(value, indent=2) + '\n')


if sys.argv[1:] == ['prepare']:
    require(not receipt_path.exists(), 'Preserve the existing host-scope checkpoint')
    manifest_path, context_path = folder / 'manifest.json', folder / 'source-context.json'
    retained = []
    for path in (manifest_path, context_path):
        snapshot = folder / 'host-scope-1/before' / (path.name + '.snapshot')
        retain(snapshot, path.read_bytes())
        retained.append(file_record(snapshot))
    changes = []
    for context in ('consumer', 'annotations'):
        source = folder / context / 'src/Examples.cs'
        original = source.read_bytes()
        snapshot = folder / 'host-scope-1/before' / (context + '-Examples.cs.snapshot')
        retain(snapshot, original)
        retained.append(file_record(snapshot))
        text = original.decode()
        position = text.index('public ')
        declaration = 'namespace AuditPiv001.Examples.V2.' + context.title() + ';\n\n'
        changed = (text[:position] + declaration + text[position:]).encode()
        source.write_bytes(changed)
        changes.append(dict(path=str(source.relative_to(ROOT)), before_sha256=sha(original), after_sha256=sha(changed),
                            before_snapshot=str(snapshot.relative_to(ROOT)), namespace_declaration=declaration,
                            insertion_index=position, complete_body_bytes_unchanged=True))
    receipt = dict(reason='Place application-owned host types in explicit namespaces, matching the canonical final OpenPGP host structure. Complete original bodies/wrappers, references, compiler policy and the actual shipping project stay unchanged.',
                   generator=file_record(Path(__file__).resolve()), changes=changes, retained=retained)
    retain(receipt_path, (json.dumps(receipt, indent=2) + '\n').encode())
    manifest = json.loads(manifest_path.read_text())
    for row in manifest['prepared_files']:
        if row['path'] in {change['path'] for change in changes}:
            row.update(file_record(ROOT / row['path']))
    manifest['approach_provenance'].append(file_record(receipt_path))
    write_json(manifest_path, manifest)
    source_context = json.loads(context_path.read_text())
    source_context['extraction_manifest_sha256'] = sha(manifest_path.read_bytes())
    source_context['host_scope_receipt'] = file_record(receipt_path)
    source_context['preparation_tools'].append(file_record(Path(__file__).resolve()))
    write_json(context_path, source_context)
receipt = json.loads(receipt_path.read_text())
for row in receipt['retained'] + [receipt['generator']]:
    raw = (ROOT / row['path']).read_bytes()
    require(len(raw) == row['bytes'] and sha(raw) == row['sha256'], 'Host-scope source/snapshot identity changed')
for change in receipt['changes']:
    original = (ROOT / change['before_snapshot']).read_bytes()
    require(sha(original) == change['before_sha256'], 'Original extraction source changed')
    position = change['insertion_index']
    expected = original[:position] + change['namespace_declaration'].encode() + original[position:]
    require((ROOT / change['path']).read_bytes() == expected and sha(expected) == change['after_sha256'], 'Host change exceeds the explicit application namespace insertion')
print('PASS: two generated hosts have explicit application namespaces; original extraction manifest/source-context/host sources remain pinned snapshots; complete body/wrapper bytes and compiler policy are preserved.')
