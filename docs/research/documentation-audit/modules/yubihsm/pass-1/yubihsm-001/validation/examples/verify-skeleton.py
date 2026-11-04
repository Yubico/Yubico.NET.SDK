"""Read-only tooling/configuration preparation checks; final document extraction waits for release."""
from pathlib import Path
import ast
import hashlib
import json
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())


def require(condition, message):
    if not condition:
        raise SystemExit(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


scripts = sorted(HERE.glob('*.py'))
for path in scripts:
    ast.parse(path.read_text(), filename=str(path))
record = json.loads((HERE / 'tooling-provenance.json').read_text())
require(record['baseline'] == 'a74792facb657413e98223e059876d8b213ffff3' and record['status'] == 'skeleton-awaiting-editor-completion', 'Wrong preparation identity')
baseline = record['source_manifest']
require(sha(ROOT / baseline['manifest']) == baseline['manifest_sha256'], 'Frozen source manifest changed')
frozen_rows = json.loads((ROOT / baseline['manifest']).read_text())['files']
require(len(frozen_rows) == baseline['checked'] == 433, 'Frozen source count changed')
frozen = {row['path']: row for row in frozen_rows}
source_folder = (ROOT / baseline['manifest']).parent
for row in record['copies']:
    source, target = ROOT / row['source'], HERE / row['target']
    require(sha(source) == row['source_sha256'] and sha(target) == row['target_sha256'], f'Source/target hash mismatch: {target}')
    frozen_row = frozen[str(source.relative_to(source_folder))]
    require(sha(source) == frozen_row['sha256'] and source.stat().st_size == frozen_row['bytes'], 'Frozen source row mismatch')
    text = source.read_bytes().decode('utf-8')
    for before, after in row['substitutions'].items():
        require(text.count(before) == row['substitution_counts'][before], 'Substitution count mismatch')
        text = text.replace(before, after)
    require(target.read_bytes() == text.encode('utf-8'), f'Unrecorded template changes: {target}')
for row in record['adaptations']:
    require(sha(ROOT / row['source']) == row['source_sha256'] and sha(HERE / row['target']) == row['target_sha256'], 'Adaptation provenance mismatch')
    frozen_row = frozen[str((ROOT / row['source']).relative_to(source_folder))]
    require(row['source_sha256'] == frozen_row['sha256'], 'Adaptation source differs from frozen row')
for row in record['tools']:
    require(sha(HERE / row['path']) == row['sha256'] and (HERE / row['path']).stat().st_size == row['bytes'], 'Authored tool changed')
sample = record['sample_context']
require(sha(ROOT / sample['path']) == sample['sha256'], 'Observed sample project context changed; re-baseline after editor completion')
print(f'PASS: {len(scripts)} Python tools parse; eight transformed host/configuration copies and four adaptation-source bindings reconcile with frozen OTP hashes.')
xml = [p for p in HERE.rglob('*') if p.suffix in ('.props', '.targets', '.csproj')]
for path in xml:
    ET.parse(path)
names = ('AuditHsm001.Examples.v1.consumer', 'AuditHsm001.Examples.v1.annotations', 'AuditHsm001.Examples.Shipping.HsmAuthTool')
for name in names:
    matches = [p for p in ROOT.rglob('*.csproj') if name.lower() in p.stem.lower()]
    require(len(matches) == 1 and matches[0].is_relative_to(HERE), f'Compiler host/traversal selector collision: {name}')
hook = ET.parse(HERE / 'v1/hsmauthtool/HsmAuthTool.Capture.targets').getroot()
require({node.tag for group in hook.findall('PropertyGroup') for node in group} == {'DocumentationWorkspace', 'ProvideCommandLineArgs'}, 'Shipping hook must observe without changing output/intermediate/compiler policy')
require(not any(p.name.endswith('Output.props') for p in HERE.rglob('*')), 'Early output hook retained')
require(not (HERE / 'extraction-scope.json').exists() and not (HERE / 'v1/manifest.json').exists(), 'Skeleton check expects final extraction to remain pending')
require(not list(HERE.rglob('Examples.cs')) and not list(HERE.rglob('evidence')), 'Partial bodies or compiler results retained before editor release')
for path in HERE.rglob('*'):
    require(not path.is_symlink(), f'Symlink retained: {path}')
    require(not any(part.lower() in ('bin', 'obj', '__pycache__', '.cache') for part in path.relative_to(HERE).parts), f'Binary/cache directory retained: {path}')
    if path.is_file():
        require(path.suffix.lower() not in ('.dll', '.exe', '.pdb', '.binlog', '.nupkg', '.snupkg', '.pyc', '.cache'), f'Binary/cache retained: {path}')
        raw = path.read_bytes()
        raw.decode('utf-8')
        require(b'\0' not in raw, f'NUL-containing retained artifact: {path}')
print(f'PASS: {len(xml)} XML files parse; three unique selectors; actual shipping project/native-output late hook configured; final bodies/results await editor completion.')
print('PENDING: coordinator editor-complete notification, final fence/inline/XML inventory and dispositions, typed complete groups, extraction manifest, builds and actual Csc captures.')
