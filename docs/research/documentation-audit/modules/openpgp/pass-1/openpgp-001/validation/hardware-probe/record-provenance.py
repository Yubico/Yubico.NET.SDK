"""Coordinator-only static provenance: validate reused frozen HSM sources; hash the authored probe."""
from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
RUN = HERE.parent.parent
baseline = next(row for row in json.loads((RUN / 'prior-archive-baseline.json').read_text())
                if '/yubihsm/' in row['manifest'])
manifest = ROOT / baseline['manifest']
raw = manifest.read_bytes()
if hashlib.sha256(raw).hexdigest() != baseline['manifest_sha256']:
    raise SystemExit('Frozen HSM manifest identity changed')
frozen = {row['path']: row for row in json.loads(raw)['files']}
source_root = manifest.parent / 'validation/hardware-probe'
rows = []
for name in ('Directory.Build.props', 'Directory.Build.targets', 'capture-compiler.py', 'retain-inputs.py',
             'tests/Yubico.YubiKit.AuditHsm001.Hardware.IntegrationTests/src/Yubico.YubiKit.AuditHsm001.Hardware.IntegrationTests.csproj'):
    path = source_root / name
    original = path.read_bytes()
    row = frozen['validation/hardware-probe/' + name]
    if len(original) != row['bytes'] or hashlib.sha256(original).hexdigest() != row['sha256']:
        raise SystemExit(f'Frozen source changed: {path}')
    rows.append(dict(path=str(path.relative_to(ROOT)), bytes=len(original), sha256=row['sha256']))
identities = []
for path in sorted(HERE.rglob('*')):
    if path.is_file() and not any(part in ('evidence', 'bin', 'obj', '__pycache__') for part in path.relative_to(HERE).parts) and path.name != 'provenance.json':
        data = path.read_bytes()
        identities.append(dict(path=str(path.relative_to(HERE)), bytes=len(data), sha256=hashlib.sha256(data).hexdigest()))
with (HERE / 'provenance.json').open('x') as output:
    json.dump(dict(source_manifest=baseline, verified_hsm_sources=rows, authored_inputs=identities,
                   adaptations=['AuditHsm001 -> AuditPgp001; YubiHsm project reference -> OpenPgp; isolated /tmp/opencode/openpgp-001-hardware outputs',
                                'Capture hook retains pre-CoreCompile item membership and post-CoreCompile actual Csc args; rejects an empty Csc response',
                                'Frozen HSM retain-inputs.py executes with one verified workspace substitution and local __file__; all actual-Csc/dependency/analyzer checks retained',
                                'Probe tests, runner and vendor baseline/reset helper are newly authored; execution belongs to coordinator'],
                   status='Prepared source identities only; build/test/hardware evidence remains pending.'), output, indent=2)
    output.write('\n')
print('Recorded static provenance; no builds, tests, formatter, device access or commit.')
