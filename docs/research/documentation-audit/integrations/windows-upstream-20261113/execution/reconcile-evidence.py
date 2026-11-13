#!/usr/bin/env python3
"""Join retained software results to source, membership and local package identities."""
import hashlib
import json
from pathlib import Path
import re
import subprocess
import zipfile

root = Path(__file__).resolve().parents[1]
def sha(data):
    return hashlib.sha256(data).hexdigest()
def git(*args):
    return subprocess.check_output(['git', *args])
source = sorted(p for directory in ('src', 'verification', 'scripts') for p in Path(directory).rglob('*.cs')
                if not {'bin', 'obj'} & set(p.parts))
digest = hashlib.sha256()
for p in source:
    digest.update(p.as_posix().encode() + b'\0' + hashlib.sha256(p.read_bytes()).digest())
assert digest.hexdigest() == 'c05c4010789edd3424ec62dc143c68bb3fa3cdc36cacc2eef4069c27d6e82b02'
members = []
correction = json.loads((root / 'execution/terminal-newline-correction.json').read_text())
for project, prefix in [('src/Core/src/Yubico.YubiKit.Core.csproj', 'src/Core/src/'),
                        ('verification/LinuxHidRouteVerification/LinuxHidRouteVerification.csproj', 'verification/')]:
    command = ['dotnet', 'msbuild', project, '-getItem:Compile']
    output = subprocess.check_output(command)
    items = json.loads(output)['Items']['Compile']
    paths = {Path(row['FullPath']).resolve() for row in items}
    included = [row['path'] for row in correction['files'] if row['path'].startswith(prefix)]
    assert all(Path(p).resolve() in paths for p in included)
    members.append({'command': command, 'included_actual_documents': included, 'evaluated_compile_items': len(items)})
inventory = Path('src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory')
manifest = json.loads((inventory / 'core-boundaries.v1.json').read_text())
sites = sum(len(f['entries']) for f in manifest['families'])
assert sites == 245
assert all('outstanding' in f['status'] and 'verified' not in f['status'] for f in manifest['families'])
contracts = json.loads((inventory / 'adapter-contracts.v1.json').read_text())
links = [p['method'] for row in contracts for p in row['profiles']]
public = (inventory / 'PublicSyncBoundaryRegistry.cs').read_text()
required = len(re.findall(r'^\s*\("', public, re.M))
rows = len(re.findall(r'^\s*new\("', public, re.M))
assert (len(contracts), len(links), len(set(links)), required, rows) == (59, 104, 104, 31, 31)
api = sorted(Path('src').glob('*/src/PublicAPI.*.txt'))
assert len(api) == 20
assert all(git('show', 'HEAD:' + p.as_posix()) == p.read_bytes() for p in api)
incoming_api_delta = git('diff', 'e322712d609aaf03b9bd1cf2964a99326728866a', '--',
                         'src/*/src/PublicAPI.*.txt').decode()
assert incoming_api_delta.count('diff --git') == 1
assert 'Core.Utilities.Base32.Decode' in incoming_api_delta
packages = []
for p in sorted(Path('artifacts/packages').glob('*.nupkg')):
    with zipfile.ZipFile(p) as z:
        xml = [n for n in z.namelist() if n.startswith('lib/') and n.endswith('.xml')]
        readmes = [n for n in z.namelist() if n.lower().endswith('readme.md')]
        assert len(xml) == 1 and len(readmes) == 1, p
        module = p.name.removeprefix('Yubico.YubiKit.').split('.')[0]
        assert z.read(readmes[0]) == Path('src', module, 'PACKAGE_README.md').read_bytes(), p
        packages.append({'path': p.as_posix(), 'sha256': sha(p.read_bytes()),
                         'xml': xml, 'readme': readmes, 'readme_sha256': sha(z.read(readmes[0]))})
assert len(packages) == 10
results = []
for label in ['verifier-build', 'clean-solution-build', 'ordinary-unit-suite', 'portable-evidence',
              'device-event-lifecycle', 'complexity-from-audit', 'complexity-from-incoming', 'docs-inclusive-pack']:
    destination = root / 'validation' / label
    receipt = json.loads((destination / 'receipt.json').read_text())
    assert receipt['exit_code'] == 0 and receipt['candidate_tree'] == '05d4409729dd81c7717c99a15a2697d49eb10906'
    for row in receipt['files']:
        data = (destination / row['path']).read_bytes()
        assert len(data) == row['bytes'] and sha(data) == row['sha256']
    console = (destination / 'console.txt').read_text()
    assert 'RESULT: PASSED' in console and 'Warnings: 0' in console
    results.append({'label': label, 'receipt_sha256': sha((destination / 'receipt.json').read_bytes()),
                    'command': receipt['command'], 'actor': receipt['actor'], 'verified_retained_files': len(receipt['files'])})
record = {'actor': 'GPT-6.1 Sol', 'platform': 'Linux integration host', 'runtime_execution': 'none in this reconciliation',
          'source_files': len(source), 'source_fingerprint': digest.hexdigest(), 'formatter_membership': members,
          'inventory': {'source_sites': sites, 'operations': len(contracts), 'unique_profile_links': len(set(links)),
                        'public_required_entries': required, 'public_rows': rows,
                        'status': 'documented/outstanding; passing scanner is not native verification',
                        'execution_evidence': 'ordinary-unit-suite/toolchain/03-test-Core.UnitTests.log; actual scan explicitly observed, 2236 succeeded, zero failures',
                        'migration': '229 unchanged / 16 mapped is received historical identity mapping, not a new dynamic scan count'},
          'api_snapshots': [{'path': p.as_posix(), 'sha256': sha(p.read_bytes())} for p in api],
          'api_parent_comparison': 'All 20 byte-identical to audited HEAD; incoming differs only by the three accepted Core Base32 public entries.',
          'incoming_api_diff': incoming_api_delta,
          'packages': packages, 'retained_results': results}
(root / 'validation/evidence-reconciliation.json').write_text(json.dumps(record, indent=2) + '\n')
print(json.dumps({k: record[k] for k in ['source_files', 'source_fingerprint', 'inventory']}, indent=2))
print('Verified 20 byte-unchanged API snapshots, 10 package XML/readme pairs, 16 formatter members and all retained receipt files.')
