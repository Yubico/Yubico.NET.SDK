"""Check prepared utility syntax, provenance, selector uniqueness and evidence attributes."""
from pathlib import Path
import ast
import hashlib
import json
import os
import subprocess
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
RUN = HERE.parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())


def require(condition, message):
    if not condition:
        raise SystemExit(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


record = json.loads((HERE / 'tool-provenance.json').read_text())
require(record['baseline'] == '727fe43afc430c5d8364d868a6436de50626e476', 'Baseline mismatch')
archive = record['source_archive']
raw = (ROOT / archive['manifest']).read_bytes()
require(sha(raw) == archive['manifest_sha256'], 'Frozen source manifest changed')
frozen = {row['path']: row for row in json.loads(raw)['files']}
source_run = (ROOT / archive['manifest']).parent
for row in record['files']:
    source = ROOT / row['source']
    raw = source.read_bytes()
    original = frozen[str(source.relative_to(source_run))]
    require(len(raw) == original['bytes'] and sha(raw) == original['sha256'] == row['source_sha256'], f'Frozen source changed: {source}')
    text = raw.decode()
    for operation in row['operations']:
        kind = operation['operation']
        if kind == 'literal-replace':
            require(text.count(operation['before']) == operation['count'], 'Substitution count changed')
            text = text.replace(operation['before'], operation['after'])
        elif kind == 'remove-between-markers':
            lo, hi = text.index(operation['start']), text.index(operation['end'])
            removed = text[lo:hi].encode()
            require(len(removed) == operation['removed_bytes'] and sha(removed) == operation['removed_sha256'], 'Removed region changed')
            text = text[:lo] + text[hi:]
        elif kind == 'truncate-before-marker':
            text = text[:text.index(operation['marker'])]
        else:
            raise SystemExit(f'Unknown transformation: {kind}')
    target = HERE / row['target']
    require(target.read_bytes() == text.encode() and sha(target.read_bytes()) == row['target_sha256'], f'Prepared target changed: {target}')
for row in record['baselines']:
    require(sha((ROOT / row['path']).read_bytes()) == row['sha256'], f'Baseline record changed: {row["path"]}')
require(sha((HERE / record['generator']['path']).read_bytes()) == record['generator']['sha256'], 'Preparation generator changed')
for path in HERE.rglob('*.py'):
    ast.parse(path.read_text(), filename=str(path))
for pattern in ('*.csproj', '*.props', '*.targets'):
    for path in HERE.rglob(pattern):
        ET.parse(path)
matches = []
for folder, dirs, names in os.walk(ROOT):
    dirs[:] = [name for name in dirs if name not in ('.git', 'bin', 'obj', '__pycache__', 'node_modules')]
    matches.extend(Path(folder) / name for name in names if name.endswith('.csproj') and 'auditpiv001.checker' in name.lower())
expected = HERE / 'checker/src/Yubico.YubiKit.AuditPiv001.Checker.csproj'
require(matches == [expected], f'Checker project selector collision: {matches}')
text = (HERE / 'checker/src/Program.cs').read_text()
require('tokens-import-removals' not in text and 'CheckImportRemovalsAsync' not in text and
        'oldPaths.Length != 20' in text, 'Strict checker surface changed')
inventory = json.loads((RUN / 'inventory.json').read_text())
require(len(inventory['files']) == len({row['path'] for row in inventory['files']}) == 123, 'Inventory path count changed')
require(sum(row['path'].endswith('.cs') for row in inventory['files']) == 111, 'Inventory C# count changed')
require(all('sha256' in row and 'git_sha256' in row for row in inventory['files']), 'Require working and Git hashes')
scope = 'docs/research/documentation-audit/modules/piv/pass-1/piv-001/'
paths = [scope + category + '/attribute-probe.csproj' for category in ('validation', 'evidence', 'hardware')]
output = subprocess.check_output(['git', 'check-attr', '-z', 'text', 'whitespace', '--', *paths], cwd=ROOT)
fields = output.decode().split('\0')[:-1]
require(len(fields) == 18 and all(fields[index] == 'unset' for index in range(2, len(fields), 3)), 'Campaign evidence attributes must preserve bytes/whitespace')
print(f'PASS: {len(record["files"])} pinned/replayed source transformations; Python/XML syntax, unique AuditPiv001.Checker selector, 123 inventory paths/111 C# paths with both hashes, evidence attributes.')
print('PENDING: coordinator .NET compilation, actual token/API gates, preservation, final reviewed inputs/examples and repository/device gates.')
