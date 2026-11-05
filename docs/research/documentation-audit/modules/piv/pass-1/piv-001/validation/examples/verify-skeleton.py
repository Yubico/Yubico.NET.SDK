"""Verify source-only tooling integrity and unique skeletons without reading current prose."""
from pathlib import Path
import ast
import hashlib
import json
import os
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


record = json.loads((HERE / 'tooling-provenance.json').read_text())
require(record['baseline'] == '727fe43afc430c5d8364d868a6436de50626e476', 'Baseline mismatch')
archive = record['source_archive']
raw = (ROOT / archive['manifest']).read_bytes()
require(sha(raw) == archive['manifest_sha256'], 'Frozen source manifest changed')
frozen = {row['path']: row for row in json.loads(raw)['files']}
source_run = (ROOT / archive['manifest']).parent
for row in record['copies']:
    source = ROOT / row['source']
    raw = source.read_bytes()
    source_row = frozen[str(source.relative_to(source_run))]
    require(len(raw) == source_row['bytes'] and sha(raw) == source_row['sha256'] == row['source_sha256'], 'Frozen source changed: ' + str(source))
    text = raw.decode()
    for operation in row['operations']:
        kind = operation['operation']
        if kind == 'literal-replace':
            require(text.count(operation['before']) == operation['count'], 'Transformation count changed')
            text = text.replace(operation['before'], operation['after'])
        elif kind == 'remove-between-markers':
            lo, hi = text.index(operation['start']), text.index(operation['end'])
            require(sha(text[lo:hi].encode()) == operation['removed_sha256'], 'Removed region changed')
            text = text[:lo] + text[hi:]
        elif kind == 'append-file':
            raw = (HERE / operation['path']).read_bytes()
            require(sha(raw) == operation['sha256'], 'Inventory extension changed')
            text += raw.decode()
        else:
            raise ValueError('Unknown transformation: ' + kind)
    target = HERE / row['target']
    require(target.read_bytes() == text.encode() and sha(target.read_bytes()) == row['target_sha256'], 'Prepared target changed: ' + str(target))
for row in record['tools']:
    path = HERE / row['path']
    require(len(path.read_bytes()) == row['bytes'] and sha(path.read_bytes()) == row['sha256'], 'Preparation helper changed: ' + str(path))
for path in HERE.glob('*.py'):
    ast.parse(path.read_text(), filename=str(path))
for path in HERE.rglob('*.json'):
    json.loads(path.read_text())
for pattern in ('*.csproj', '*.props', '*.targets'):
    for path in HERE.rglob(pattern):
        ET.parse(path)
projects = []
for folder, dirs, names in os.walk(ROOT):
    dirs[:] = [name for name in dirs if name not in ('.git', 'bin', 'obj', '__pycache__', 'node_modules')]
    projects.extend(Path(folder) / name for name in names if name.endswith('.csproj'))
selectors = {'AuditPiv001.Examples.v1.consumer': HERE / 'v1/consumer/src/Yubico.YubiKit.AuditPiv001.Examples.v1.consumer.csproj',
             'AuditPiv001.Examples.v1.annotations': HERE / 'v1/annotations/src/Yubico.YubiKit.AuditPiv001.Examples.v1.annotations.csproj',
             'AuditPiv001.Examples.Shipping.PivTool': HERE / 'dispatch/src/Yubico.YubiKit.AuditPiv001.Examples.Shipping.PivTool.csproj'}
for selector, expected in selectors.items():
    require([path for path in projects if selector.lower() in path.stem.lower()] == [expected], 'Project selector collision: ' + selector)
for context in ('consumer', 'annotations'):
    project = selectors['AuditPiv001.Examples.v1.' + context]
    require(not (project.parent / 'Examples.cs').exists(), 'Keep v1 source-free skeletons immutable')
    props = ET.parse(HERE / 'v1' / context / 'Directory.Build.props').getroot()
    require(props.find('Import').attrib['Project'] == '$(RepositoryRoot)/Directory.Build.props', 'Canonical props import changed')
    targets = ET.parse(HERE / 'v1' / context / 'Directory.Build.targets').getroot()
    require(targets.find('Import').attrib['Project'] == '$(RepositoryRoot)/Directory.Build.targets', 'Canonical targets import changed')
hook = HERE / 'v1/pivtool/PivTool.Capture.targets'
text = hook.read_text()
require('BeforeTargets="CoreCompile"' in text and 'AfterTargets="CoreCompile"' in text and
        'BaseOutputPath>' not in text and 'BaseIntermediateOutputPath>' not in text,
        'Late actual-Csc observation must preserve shipping output locations')
capture = (HERE / 'capture.py').read_text()
require('== 36' in capture and '_analyzer_policy.prove(' in capture and 'project_reference_proof(' in capture,
        'Require full shipping source membership and real analyzer/project-reference proofs')
print(f'PASS: {len(record["copies"])} final frozen source transformations replayed; Python/JSON/XML syntax; three unique recursive selectors; immutable empty hosts; canonical imports, late capture and ordinary shipping outputs.')
print('PENDING: reviewed/formatted release, final current-body inventory/task dispositions, typed extraction, coordinator compilation/actual-Csc capture and live verification. Current prose and snippet bodies remained unread.')
