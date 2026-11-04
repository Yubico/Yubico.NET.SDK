"""Read-only overlay provenance and accepted-capture checks; no build or device access."""
from pathlib import Path
import json

from inventory import ROOT, HERE, sha

OVERLAY = Path(__file__).resolve().parent

def require(condition, message):
    if not condition:
        raise ValueError(message)

def check_file(row):
    path = Path(row['path'])
    if not path.is_absolute():
        path = ROOT / path
    raw = path.read_bytes()
    require(len(raw) == row['bytes'] and sha(raw) == row['sha256'], f'Overlay input/retained identity changed: {path}')

def validate_overlay():
    manifest = json.loads((OVERLAY / 'manifest.json').read_text())
    require(manifest['id'] == 'otptool-native-1' and manifest['base_revision'] == 'v2', 'Wrong overlay identity')
    for row in manifest['files'] + manifest['inputs'] + manifest['preserved_v1_v2']:
        check_file(row)
    actual = sorted(str(path.relative_to(ROOT)) for version in ('v1', 'v2')
                    for path in (HERE / version).rglob('*') if path.is_file())
    require(actual == sorted(row['path'] for row in manifest['preserved_v1_v2']), 'Historical v1/v2 path set changed; this overlay never rewrites those archives')
    for context, binding in manifest['reused_captures'].items():
        folder = ROOT / binding['directory']
        data = json.loads((folder / 'manifest.json').read_text())
        require(data['revision'] == 'v2' and data['context'] == context and data['attempt'] == 'first', 'Accepted first capture identity changed')
        require(data['counts'] == binding['counts'], 'Accepted first capture counts changed')
        for row in data['retained']:
            raw = (folder / row['path']).read_bytes()
            require(len(raw) == row['bytes'] and sha(raw) == row['sha256'], f'Accepted capture text changed: {context}/{row["path"]}')
        source = HERE / 'v2' / context / 'src/Examples.cs'
        recorded = next(row for row in data['records'] if row['kind'] == 'source' and row['path'] == str(source))
        require(sha(source.read_bytes()) == recorded['sha256'], f'Accepted {context} body changed')
    print('PASS: overlay provenance; unchanged v1/v2 archives; accepted consumer/first and annotations/first retained identities reused without copying or recompiling.')
    return manifest
