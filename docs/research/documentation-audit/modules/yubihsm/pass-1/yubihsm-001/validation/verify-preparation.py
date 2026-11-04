"""Read-only syntax/XML/provenance inspection; execution gates remain separate."""
from pathlib import Path
import ast
import hashlib
import json
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
BASELINE = 'a74792facb657413e98223e059876d8b213ffff3'


def require(condition, message):
    if not condition:
        raise SystemExit(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


scripts = sorted(HERE.glob('*.py'))
for path in scripts:
    ast.parse(path.read_text(), filename=str(path))
print(f'PASS: Python syntax parsed for {len(scripts)} tools; inspection creates no bytecode caches.')
checker = json.loads((HERE / 'checker-provenance.json').read_text())
utility = json.loads((HERE / 'utility-provenance.json').read_text())
require(checker['baseline'] == BASELINE, 'Wrong checker baseline')
require(len(checker['files']) == 4 and len(utility['files']) == 4, 'Unexpected source-copy set')
for provenance in (checker, utility):
    archive = provenance['source_archive']
    require(sha(ROOT / archive['manifest']) == archive['manifest_sha256'], 'Frozen source manifest changed')
    frozen_rows = json.loads((ROOT / archive['manifest']).read_text())['files']
    require(len(frozen_rows) == archive['checked'] == 433, 'Frozen source archive count mismatch')
    folder = (ROOT / archive['manifest']).parent
    frozen = {row['path']: row for row in frozen_rows}
    for row in provenance['files']:
        source, target = ROOT / row['source'], HERE / row['target']
        require(sha(source) == row['source_sha256'] and sha(target) == row['target_sha256'], f'Source/target hash mismatch: {target}')
        original = source.read_bytes()
        frozen_row = frozen[str(source.relative_to(folder))]
        require(len(original) == frozen_row['bytes'] and sha(source) == frozen_row['sha256'], f'Frozen source row mismatch: {source}')
        text = original.decode('utf-8')
        for before, after in row['replacements'].items():
            require(text.count(before) == row['substitution_counts'][before], 'Substitution count mismatch')
            text = text.replace(before, after)
        require(target.read_bytes() == text.encode('utf-8'), f'Unrecorded source-copy changes: {target}')
print('PASS: four checker and four utility copies reconcile with frozen YubiOTP manifest rows, source/target hashes and exact substitutions.')
for row in utility['baselines']:
    require(sha(ROOT / row['path']) == row['sha256'], f"Preparation baseline changed: {row['path']}")
require(sha(HERE / utility['generator']['path']) == utility['generator']['sha256'], 'Generator changed')
print('PASS: two current-run baseline record hashes and preparation-generator hash reconcile.')
xml = sorted((HERE / 'checker').rglob('*.props')) + sorted((HERE / 'checker').rglob('*.targets')) + sorted((HERE / 'checker').rglob('*.csproj'))
for path in xml:
    ET.parse(path)
selector = 'AuditHsm001.Checker'
projects = sorted(path for path in ROOT.rglob('*.csproj') if selector.lower() in path.stem.lower())
require(projects == [HERE / 'checker/src/Yubico.YubiKit.AuditHsm001.Checker.csproj'], 'Checker selector collision or missing project')
print(f'PASS: {len(xml)} XML files parse; recursive project-filename discovery finds exactly one {selector} match.')
require(json.loads((HERE / 'primary-binary-sources.json').read_text()) == [], 'Preparation PDF allowlist must start empty')
require(not (HERE / 'import-removals.json').exists(), 'Unexpected import-removal approval')
require(not (HERE.parent / 'record-manifest.json').exists(), 'Preparation unexpectedly frozen')
for path in HERE.rglob('*'):
    require(not path.is_symlink(), f'Symlink retained: {path}')
    require(not any(p.lower() in ('bin', 'obj', '__pycache__', '.cache') for p in path.relative_to(HERE).parts), f'Binary/cache directory retained: {path}')
    if path.is_file():
        require(path.suffix.lower() not in ('.dll', '.exe', '.pdb', '.binlog', '.nupkg', '.snupkg', '.pyc', '.cache'), f'Binary/cache artifact retained: {path}')
        raw = path.read_bytes()
        raw.decode('utf-8')
        require(b'\0' not in raw, f'NUL-containing artifact: {path}')
print('PASS: empty primary-PDF allowlist, unfrozen preparation, source/configuration/text artifacts and zero import-removal approvals.')
print('PENDING: .NET build, token/API checks, archive/user-file preservation gate, final record review, examples, tests, formatter and hardware.')
