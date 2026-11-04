"""Read-only hardware-probe preparation checks; no compilation, discovery or test execution."""
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
print(f'PASS: syntax parsed for {len(scripts)} Python tools; zero imports of prepared tools or cache writes.')
record = json.loads((HERE / 'provenance.json').read_text())
require(record['baseline'] == 'a74792facb657413e98223e059876d8b213ffff3', 'Wrong baseline')
source_manifest = record['source_manifest']
require(sha(ROOT / source_manifest['manifest']) == source_manifest['manifest_sha256'], 'Frozen source manifest changed')
frozen_rows = json.loads((ROOT / source_manifest['manifest']).read_text())['files']
require(len(frozen_rows) == source_manifest['checked'] == 433, 'Frozen source count changed')
frozen = {row['path']: row for row in frozen_rows}
source_folder = (ROOT / source_manifest['manifest']).parent
for row in record['copies']:
    source, target = ROOT / row['source'], HERE / row['target']
    require(sha(source) == row['source_sha256'] and sha(target) == row['target_sha256'], f'Copy hash mismatch: {target}')
    original = source.read_bytes()
    frozen_row = frozen[str(source.relative_to(source_folder))]
    require(len(original) == frozen_row['bytes'] and sha(source) == frozen_row['sha256'], f'Frozen source row mismatch: {source}')
    text = original.decode('utf-8')
    for before, after in row['substitutions'].items():
        require(text.count(before) == row['substitution_counts'][before], 'Substitution count mismatch')
        text = text.replace(before, after)
    require(target.read_bytes() == text.encode('utf-8'), f'Unrecorded copy adaptation: {target}')
for row in record['adaptations']:
    require(sha(ROOT / row['source']) == row['source_sha256'] and sha(HERE / row['target']) == row['target_sha256'], 'Adaptation provenance mismatch')
for key, folder in [('preparation_inputs', HERE), ('canonical_preparation_identities', ROOT)]:
    for row in record[key]:
        path = folder / row['path']
        require(path.stat().st_size == row['bytes'] and sha(path) == row['sha256'], f'Preparation input changed: {path}')
print(f"PASS: {len(record['copies'])} exact frozen-source copies, retain-input adaptation and authored/canonical preparation hashes reconcile.")
sources = json.loads((HERE / 'sources/provenance.json').read_text())
for source in sources['sources']:
    for row in source['excerpts']:
        path = HERE / 'sources' / row['path']
        require(path.stat().st_size == row['bytes'] and sha(path) == row['sha256'], f'Original-source excerpt changed: {path}')
require(sources['sources'][0]['revision'] == '44a5ab52e0697c67b36c6dec953db08a6b7b183a', 'Wrong vendor revision')
print('PASS: pinned vendor derivation/key/cryptogram mapping and RFC 4493 algorithm/vector excerpt hashes reconcile.')
xml = [p for p in HERE.rglob('*') if p.suffix in ('.props', '.targets', '.csproj', '.runsettings')]
for path in xml:
    ET.parse(path)
selector = 'AuditHsm001.Hardware.IntegrationTests'
projects = sorted(p for p in ROOT.rglob('*.csproj') if selector.lower() in p.stem.lower())
require(projects == [HERE / 'tests/Yubico.YubiKit.AuditHsm001.Hardware.IntegrationTests/src/Yubico.YubiKit.AuditHsm001.Hardware.IntegrationTests.csproj'], 'Probe project selector collision')
project = ET.parse(projects[0]).getroot()
packages = {e.attrib['Include'] for e in project.findall('.//PackageReference')}
require({'xunit', 'Microsoft.NET.Test.Sdk', 'Xunit.SkippableFact', 'xunit.runner.visualstudio'} <= packages and 'xunit.v3' not in packages, 'Wrong runner packages')
references = {e.attrib['Include'] for e in project.findall('.//ProjectReference')}
require('$(RepositoryRoot)/src/Tests.Shared/Yubico.YubiKit.Tests.Shared.csproj' in references, 'Genuine Tests.Shared reference missing')
settings = next(HERE.rglob('probe.runsettings'))
require(ET.parse(settings).findtext('.//TestSessionTimeout') == '120000', 'VSTest bound changed')
runner = json.loads(next(HERE.rglob('xunit.runner.json')).read_text())
require(runner['parallelizeAssembly'] is False and runner['parallelizeTestCollections'] is False and runner['maxParallelThreads'] == 1, 'Parallel execution enabled')
print(f'PASS: {len(xml)} XML files parse; one unique probe project; canonical v2/TestShared references, serial runner and 120-second host bound configured.')
for path in HERE.rglob('*'):
    require(not path.is_symlink(), f'Symlink retained: {path}')
    require(not any(p.lower() in ('bin', 'obj', '__pycache__', '.cache') for p in path.relative_to(HERE).parts), f'Cache directory retained: {path}')
    if path.is_file():
        require(path.suffix.lower() not in ('.dll', '.exe', '.pdb', '.binlog', '.nupkg', '.snupkg', '.pyc', '.cache'), f'Binary/cache retained: {path}')
        raw = path.read_bytes()
        raw.decode('utf-8')
        require(b'\0' not in raw, f'NUL-containing artifact: {path}')
require(not (HERE / 'evidence').exists(), 'Preparation unexpectedly contains build/run evidence')
print('PASS: source/configuration/provenance-only preparation; prior hardware results and binaries/caches remain separate.')
print('PENDING: C# compilation/analyzers, actual Csc capture, RFC-vector execution, device binding, all four hardware lanes and cleanup execution.')
