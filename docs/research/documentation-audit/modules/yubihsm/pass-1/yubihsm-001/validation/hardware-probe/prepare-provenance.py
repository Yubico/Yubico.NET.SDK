"""Prepare uniquely named compiler host/configuration from frozen OTP sources; never compile."""
from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
SOURCE_RUN = ROOT / 'docs/research/documentation-audit/modules/yubiotp/pass-1/yubiotp-001'
SOURCE = SOURCE_RUN / 'validation/hardware-probe'
OLD_PROJECT = 'tests/Yubico.YubiKit.AuditOtp001.Hardware.IntegrationTests/src/'
NEW_PROJECT = 'tests/Yubico.YubiKit.AuditHsm001.Hardware.IntegrationTests/src/'
REPLACEMENTS = {
    'AuditOtp001': 'AuditHsm001',
    '/tmp/opencode/yubiotp-001-hardware-probe': '/tmp/opencode/yubihsm-001-hardware',
    'src/YubiOtp/src/Yubico.YubiKit.YubiOtp.csproj': 'src/YubiHsm/src/Yubico.YubiKit.YubiHsm.csproj',
    '<TestSessionTimeout>90000</TestSessionTimeout>': '<TestSessionTimeout>120000</TestSessionTimeout>',
}
FILES = {
    'Directory.Build.props': 'Directory.Build.props',
    'Directory.Build.targets': 'Directory.Build.targets',
    'capture-compiler.py': 'capture-compiler.py',
    OLD_PROJECT + 'Yubico.YubiKit.AuditOtp001.Hardware.IntegrationTests.csproj': NEW_PROJECT + 'Yubico.YubiKit.AuditHsm001.Hardware.IntegrationTests.csproj',
    OLD_PROJECT + 'xunit.runner.json': NEW_PROJECT + 'xunit.runner.json',
    OLD_PROJECT + 'probe.runsettings': NEW_PROJECT + 'probe.runsettings',
}


def require(condition, message):
    if not condition:
        raise SystemExit(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def retain(relative, raw):
    current = HERE
    for part in Path(relative).parts:
        current /= part
        require(not current.is_symlink(), f'Symlinked target: {current}')
    target = HERE / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists():
        require(target.read_bytes() == raw, f'Refusing changed existing target: {target}')
    else:
        with target.open('xb') as output:
            output.write(raw)


baseline = json.loads((HERE.parent.parent / 'prior-archive-baseline.json').read_text())[-1]
require(baseline['checked'] == 433, 'Wrong source archive')
manifest_bytes = (ROOT / baseline['manifest']).read_bytes()
require(sha(manifest_bytes) == baseline['manifest_sha256'], 'Frozen source manifest changed')
rows = json.loads(manifest_bytes)['files']
require(len(rows) == 433, 'Frozen source count changed')
frozen = {row['path']: row for row in rows}
copies = []
for old, new in FILES.items():
    source = SOURCE / old
    original = source.read_bytes()
    row = frozen[str(source.relative_to(SOURCE_RUN))]
    require(len(original) == row['bytes'] and sha(original) == row['sha256'], f'Frozen source changed: {source}')
    text = original.decode('utf-8')
    counts = {}
    for before, after in REPLACEMENTS.items():
        counts[before] = text.count(before)
        text = text.replace(before, after)
    raw = text.encode('utf-8')
    retain(new, raw)
    copies.append(dict(source=str(source.relative_to(ROOT)), source_sha256=sha(original),
                       target=new, target_sha256=sha(raw), substitutions=REPLACEMENTS,
                       substitution_counts=counts))
authored = []
for path in sorted(HERE.rglob('*')):
    if path.is_file() and path.name not in ('provenance.json', 'preparation-static.log') and not any(p in ('sources', 'evidence') for p in path.relative_to(HERE).parts):
        raw = path.read_bytes()
        authored.append(dict(path=str(path.relative_to(HERE)), bytes=len(raw), sha256=sha(raw)))
canonical = []
for name in ('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json', '.editorconfig',
             'src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.IntegrationTests/Yubico.YubiKit.YubiHsm.IntegrationTests.csproj'):
    raw = (ROOT / name).read_bytes()
    canonical.append(dict(path=name, bytes=len(raw), sha256=sha(raw)))
original = SOURCE / 'retain-inputs.py'
row = frozen[str(original.relative_to(SOURCE_RUN))]
require(sha(original.read_bytes()) == row['sha256'], 'Frozen retain-inputs source changed')
retain('provenance.json', (json.dumps(dict(
    baseline='a74792facb657413e98223e059876d8b213ffff3', source_manifest=baseline,
    copies=copies, preparation_inputs=authored, canonical_preparation_identities=canonical,
    adaptations=[dict(source=str(original.relative_to(ROOT)), source_sha256=row['sha256'],
                      target='retain-inputs.py', target_sha256=sha((HERE / 'retain-inputs.py').read_bytes()),
                      changes='Parameterize capture and final-input names; fail closed with explicit checks; reconcile actual Csc sets; retain full text input snapshots with exclusive creation and dynamic counts.')],
    policy='Host uses genuine central xUnit-v2/VSTest/TestShared references and canonical root analyzer policy. Probes/oracle are newly authored. Late Csc capture and final source/dependency reconciliation remain coordinator-owned.'
), indent=2) + '\n').encode('utf-8'))
print('Prepared AuditHsm001.Hardware.IntegrationTests host/configuration and source provenance; no build/test/device operation.')
