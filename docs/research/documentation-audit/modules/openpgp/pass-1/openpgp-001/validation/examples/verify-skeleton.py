"""Read-only syntax/XML/provenance checks; compiler and final document bodies stay gated."""
from pathlib import Path
import ast
import hashlib
import importlib.util
import json
import sys
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


scripts = sorted(HERE.glob('*.py'))
for path in scripts:
    ast.parse(path.read_text(), filename=str(path))
record = json.loads((HERE / 'tooling-provenance.json').read_text())
require(record['baseline'] == 'fe180282a2d77b15eaaa5868d6c8b00e251d1f05' and
        record['status'] == 'skeleton-awaiting-editor-completion', 'Wrong preparation identity')
archive = record['source_manifest']
require(sha(ROOT / archive['manifest']) == archive['manifest_sha256'], 'Frozen source manifest changed')
rows = json.loads((ROOT / archive['manifest']).read_text())['files']
require(len(rows) == archive['checked'] == 477, 'Frozen source record count changed')
frozen = {row['path']: row for row in rows}
source_folder = (ROOT / archive['manifest']).parent
for row in record['copies']:
    source, target = ROOT / row['source'], HERE / row['target']
    expected = frozen[str(source.relative_to(source_folder))]
    require(sha(source) == row['source_sha256'] == expected['sha256'] and
            source.stat().st_size == expected['bytes'], f'Frozen source mismatch: {source}')
    text = source.read_bytes().decode('utf-8')
    for before, after in row['substitutions'].items():
        require(text.count(before) == row['substitution_counts'][before], 'Substitution count mismatch')
        text = text.replace(before, after)
    require(target.read_bytes() == text.encode() and sha(target) == row['target_sha256'],
            f'Unrecorded source transformation: {target}')
for row in record['lesson_sources']:
    source = ROOT / row['source']
    expected = frozen[str(source.relative_to(source_folder))]
    require(sha(source) == row['sha256'] == expected['sha256'] and
            source.stat().st_size == row['bytes'] == expected['bytes'], 'Frozen lesson source changed')
for row in record['tools']:
    require(sha(HERE / row['path']) == row['sha256'] and (HERE / row['path']).stat().st_size == row['bytes'],
            f'Prepared tool changed: {row["path"]}')
sample = record['sample_context']
require(sha(ROOT / sample['path']) == sample['sha256'], 'Observed sample project changed; reconcile final inputs after editor completion')
xml = [p for p in HERE.rglob('*') if p.suffix in ('.props', '.targets', '.csproj')]
for path in xml:
    ET.parse(path)
selectors = ('AuditPgp001.Examples.v1.consumer', 'AuditPgp001.Examples.v1.annotations',
             'AuditPgp001.Examples.Shipping.OpenPgpTool')
for selector in selectors:
    matches = [p for p in ROOT.rglob('*.csproj') if selector.lower() in p.stem.lower()]
    require(len(matches) == 1 and matches[0].is_relative_to(HERE), f'Project-filename selector collision: {selector}')
hook = ET.parse(HERE / 'v1/openpgptool/OpenPgpTool.Capture.targets').getroot()
require({node.tag for group in hook.findall('PropertyGroup') for node in group} ==
        {'DocumentationWorkspace', 'ProvideCommandLineArgs'}, 'Late hook changes shipping compiler/output policy')
driver = ET.parse(HERE / 'dispatch/src/Yubico.YubiKit.AuditPgp001.Examples.Shipping.OpenPgpTool.csproj').getroot()
require(driver.find('.//ShippingOpenPgpToolProject').text.endswith('/src/OpenPgp/examples/OpenPgpTool/OpenPgpTool.csproj'),
        'Traversal targets another sample')
for target in driver.findall('Target'):
    task = target.find('MSBuild')
    require(task is not None and task.attrib['Projects'] == '$(ShippingOpenPgpToolProject)',
            'Traversal replaced real shipping project')
require(not (HERE / 'extraction-scope.json').exists() and not (HERE / 'v1/manifest.json').exists(),
        'Skeleton check expects final extraction pending')
require(not list(HERE.rglob('Examples.cs')) and not list(HERE.rglob('evidence')),
        'Final bodies/compiler results retained before release')
spec = importlib.util.spec_from_file_location('audit_pgp_sdk_policy', HERE / 'sdk-policy.py')
sdk = importlib.util.module_from_spec(spec)
spec.loader.exec_module(sdk)
evidence = sdk.inspect_default()
require(sdk.validate(dict(NETCoreSdkVersion=sdk.SDK_VERSION, CustomBeforeMicrosoftCommonProps=''))['observed_value'] == '',
        'Empty optional-hook value rejected')
require(sdk.validate(dict(NETCoreSdkVersion=sdk.SDK_VERSION, CustomBeforeMicrosoftCommonProps=str(sdk.DEFAULT)))['observed_value'] == str(sdk.DEFAULT),
        'Exact absent SDK default rejected')
for path in HERE.rglob('*'):
    require(not path.is_symlink(), f'Symlink retained: {path}')
    require(not any(part.lower() in ('bin', 'obj', '__pycache__', '.cache') for part in path.relative_to(HERE).parts),
            f'Binary/cache directory retained: {path}')
    if path.is_file():
        require(path.suffix.lower() not in ('.dll', '.exe', '.pdb', '.binlog', '.nupkg', '.snupkg', '.pyc', '.cache'),
                f'Binary/cache retained: {path}')
        raw = path.read_bytes()
        raw.decode('utf-8')
        require(b'\0' not in raw, f'NUL-containing retained artifact: {path}')
print(f'PASS: {len(scripts)} Python tools parse; {len(record["copies"])} exact transformations and two final-HSM lesson sources reconcile with frozen provenance.')
print(f'PASS: {len(xml)} XML files parse; three recursively unique project-filename selectors; actual OpenPgpTool traversal and observation-only late hook configured.')
print(f'PASS: SDK {evidence["sdk_version"]} default expression, exact path, conditional Exists import and absent target inspected; empty and exact dormant-default policies accepted.')
print('PASS: source/configuration/text artifacts only; final bodies/results await release. No compiler, tests or device operations invoked.')
print('PENDING: post-editor/formatter checkpoint, final inventory/dispositions/whole-body typed groups and dependency/resource ledger, extraction manifest, coordinator builds and actual Csc captures.')
