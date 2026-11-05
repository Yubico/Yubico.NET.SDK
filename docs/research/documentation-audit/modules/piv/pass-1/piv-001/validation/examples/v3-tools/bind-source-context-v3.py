"""Retain baseline body provenance and complete current shipping sources for final v3 verification."""
from pathlib import Path
import html
import json
import re
import subprocess
import sys

sys.dont_write_bytecode = True
from inventory import HERE, ROOT, RUN, sha, file_record, require, unit_id, source_paths

BASELINE = '727fe43afc430c5d8364d868a6436de50626e476'
folder = HERE / 'v3'
inventory = json.loads((folder / 'inventory.json').read_text())
manifest = json.loads((folder / 'manifest.json').read_text())
dispositions = {row['id']: row for row in manifest['dispositions']}


def retain(relative, raw):
    require(not (RUN / 'record-manifest.json').exists(), 'Run is frozen')
    target = folder / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists():
        require(target.read_bytes() == raw, 'Preserve changed context artifact: ' + str(target))
    else:
        with target.open('xb') as output:
            output.write(raw)
    return dict(path=relative, bytes=len(raw), sha256=sha(raw))


def bodies(path, raw):
    text = raw.decode().replace('\r\n', '\n')
    if path.endswith('.cs'):
        stripped = re.sub(r'^[ \t]*/// ?', '', text, flags=re.M)
        return [(html.unescape(match).strip('\n').rstrip() + '\n').encode() for match in re.findall(r'<code(?:\s[^>]*)?>(.*?)</code>', stripped, re.S)]
    opened = None
    result = []
    lines = text.splitlines(keepends=True)
    for number, line in enumerate(lines):
        match = re.match(r'^\s{0,3}(`{3,}|~{3,})([^\n]*)', line)
        if opened is not None:
            marker, start = opened
            if match and match[1][0] == marker[0] and len(match[1]) >= len(marker) and not match[2].strip():
                result.append(''.join(lines[start:number]).encode())
                opened = None
        elif match:
            opened = (match[1], number + 1)
    require(opened is None, 'Baseline fence closure changed')
    return result


report = (RUN / 'edit-report.md').read_text()
ledger = report.split('## Original complete-body hashes', 1)[1].split('```text', 1)[1].split('```', 1)[0]
originals = []
path = None
cache = {}
for line in ledger.splitlines():
    if line.startswith(('src/', 'docs/')):
        path = line
        cache[path] = bodies(path, subprocess.check_output(['git', 'show', BASELINE + ':' + path], cwd=ROOT))
    match = re.fullmatch(r'(\d+)@(\d+) ([a-f0-9]{64})', line)
    if match:
        ordinal = int(match[1])
        raw = cache[path][ordinal - 1]
        require(sha(raw) == match[3], f'Original editor body hash mismatch: {path}#{ordinal}')
        originals.append(dict(path=path, ordinal=ordinal, baseline_line=int(match[2]), sha256=sha(raw), body=raw.decode()))
require(len(originals) == 61, 'Reconcile all 61 original body identities')
current = []
for kind, plural in [('fence', 'fences'), ('xml', 'xml')]:
    ordinals = {}
    for item in inventory[plural]:
        path = item['path']
        if kind == 'xml' and item['tag'] != 'code':
            continue
        if path.startswith('src/Piv/'):
            chosen = True
        elif kind == 'fence':
            chosen = path in ('docs/architecture/raw-access-tiers.md', 'docs/usage/user-interaction.md') or (path == 'docs/migration/v1-to-v2.md' and item['index'] in (9, 10, 11, 12, 21, 22))
        else:
            chosen = False
        if not chosen:
            continue
        ordinals[path] = ordinals.get(path, 0) + 1
        identity = unit_id(kind, item)
        canonical_body = (item['body'].strip('\n') + '\n') if kind == 'xml' else item['body']
        current.append(dict(id=identity, path=path, ordinal=item['index'] if kind == 'fence' else ordinals[path],
                            exact_body_sha256=item['body_sha256'], editor_normalized_sha256=sha(canonical_body.encode()),
                            body=item['body'], disposition=dispositions[identity]))
require(len(current) == 60 and sum(row['path'].startswith('src/Piv/') for row in current) == 42, 'Reconcile 42 module plus 18 assigned shared bodies')
f9 = next(row for row in current if row['path'].endswith('/Attestation.cs') and row['ordinal'] == 2)
require(f9['editor_normalized_sha256'] == 'c26c99a88224f73c91b9d2821843aec7abd049d7cd4fb36c9618be9a574a0ccc' and 'result.AttestationCertificate' in f9['body'], 'Corrected F9 body changed')
native_sources = source_paths(ROOT / 'src/Piv/examples/PivTool')
require(len(native_sources) == 36 and native_sources == manifest['sample']['source_paths'], 'Require full actual shipping source path set')
shipping = []
for path in native_sources:
    source = ROOT / path
    raw = source.read_bytes()
    retained = retain('native-source/' + sha(raw) + '-' + source.name + '.snapshot', raw)
    shipping.append(dict(file_record(source), retained=retained))
native_project = ROOT / manifest['sample']['project']
project_snapshot = retain('native-project/PivTool.csproj.snapshot', native_project.read_bytes())
record = dict(baseline=BASELINE, revision='v3', extraction_manifest_sha256=sha((folder / 'manifest.json').read_bytes()),
              release_sha256=sha((HERE / 'v3/extraction-scope.json').read_bytes()),
              original_editor_report=file_record(RUN / 'edit-report.md'), authoritative_corrections=file_record(RUN / 'edit-corrections.md'),
              original_bodies=originals, current_bodies=current,
              module_sources=inventory['xml_documents'], documents=inventory['documents'],
              compiled_groups=len(manifest['groups']), compiled_bodies=sum(row['disposition']['status'] == 'compile' for row in current),
              active_contexts=manifest['active_contexts'], native_project=dict(file_record(native_project), retained=project_snapshot),
              native_sources=shipping,
              f9_qualification='Actual helper populates AttestationCertificate; JSON attestationCertificate carries F9 metadata and intermediateCertificate remains null. Certificate using scopes remain single-owner. Retrieval adds no chain-trust/key-binding claim.',
              preparation_tools=[file_record(path) for path in sorted(Path(__file__).resolve().parent.glob('*.py'))],
              limits='Prepared final inputs and immutable current/baseline body/source identities. Coordinator builds, actual Csc captures/analyzers and live dependency checks remain pending; hardware evidence has separate owners.')
retain('source-context.json', (json.dumps(record, indent=2) + '\n').encode())
print('Bound 61 original body hashes, 60 current bodies (42 module + 18 shared), all 111 module C# identities, ten document identities and complete original 36-source actual PivTool/project text; corrected F9 result/JSON qualification retained.')
