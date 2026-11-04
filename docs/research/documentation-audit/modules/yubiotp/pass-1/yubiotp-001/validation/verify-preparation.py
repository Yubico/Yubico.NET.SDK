"""Static preparation inspection only: no compilation, module reads or validation gate execution."""
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
print(f'PASS: Python syntax parsed for {len(scripts)} retained tools; no imports executed or bytecode caches generated.')
checker = json.loads((HERE / 'checker-provenance.json').read_text())
require(checker['baseline'] == '95e5ccaeda33954c21f2a38058d4f66b5f208787', 'Wrong baseline')
for row in checker['files']:
    source, target = ROOT / row['source'], HERE / row['target']
    require(sha(source) == row['source_sha256'] and sha(target) == row['target_sha256'], f'Provenance hash mismatch: {target}')
    expected = source.read_text()
    for before, after in checker['replacements'].items():
        require(expected.count(before) == row['substitution_counts'][before], 'Substitution count mismatch')
        expected = expected.replace(before, after)
    require(target.read_bytes() == expected.encode('utf-8'), f'Unexpected checker algorithm/configuration changes: {target}')
print('PASS: all four frozen checker source/config copies match exactly the recorded substitutions and hashes.')
utility = json.loads((HERE / 'utility-provenance.json').read_text())
for row in utility['files']:
    require(sha(ROOT / row['source']) == row['source_sha256'], f"Adaptation source changed: {row['source']}")
    require(sha(HERE / row['target']) == row['target_sha256'], f"Adaptation target changed: {row['target']}")
for row in utility['baselines']:
    require(sha(ROOT / row['path']) == row['sha256'], f"New run baseline changed: {row['path']}")
require(sha(HERE / utility['generator']['path']) == utility['generator']['sha256'], 'Preparation generator changed')
print('PASS: four adapted tool source/target hashes, two new baseline record hashes and generator hash reconciled.')
xml = sorted((HERE / 'checker').rglob('*.props')) + sorted((HERE / 'checker').rglob('*.targets')) + sorted((HERE / 'checker').rglob('*.csproj'))
for path in xml:
    ET.parse(path)
selector = 'AuditOtp001.Checker'
projects = sorted(path for path in ROOT.rglob('*.csproj') if selector.lower() in path.stem.lower())
require(projects == [HERE / 'checker/src/Yubico.YubiKit.AuditOtp001.Checker.csproj'], 'Checker project selector collision or missing project')
print(f'PASS: {len(xml)} XML configuration files parsed; recursive project path discovery finds exactly one {selector} match.')
require(not (HERE / 'import-removals.json').exists(), 'Unexpected preauthorized import-removal manifest')
require(not (HERE.parent / 'record-manifest.json').exists(), 'Preparation unexpectedly frozen')
for path in HERE.rglob('*'):
    require(not path.is_symlink(), f'Symlink retained: {path}')
    require(not any(p.lower() in ('bin', 'obj', '__pycache__', '.cache') for p in path.relative_to(HERE).parts), f'Binary/cache directory retained: {path}')
    if path.is_file():
        require(path.suffix.lower() not in ('.dll', '.exe', '.pdb', '.binlog', '.nupkg', '.snupkg', '.pyc', '.cache'), f'Binary/cache retained: {path}')
        raw = path.read_bytes()
        raw.decode('utf-8')
        require(b'\0' not in raw, f'NUL-containing retained artifact: {path}')
print('PASS: preparation remains unfrozen, without import-removal approvals, retained binaries or caches.')
print('NOT RUN: checker build, tokens/API, preservation, final record review, examples, repository tests/gates, formatter or hardware.')
