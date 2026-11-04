"""Prepare fresh example host/capture skeletons from frozen final OTP tools; source work only."""
from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
RUN = HERE.parent.parent
SOURCE_RUN = ROOT / 'docs/research/documentation-audit/modules/yubiotp/pass-1/yubiotp-001'
SOURCE = SOURCE_RUN / 'validation/examples'
COMMON = {
    '/tmp/opencode/yubiotp-001-examples-v2-otptool-native-1': '/tmp/opencode/yubihsm-001-examples-v1-hsmauthtool',
    '/tmp/opencode/yubiotp-001-examples-v2-': '/tmp/opencode/yubihsm-001-examples-v1-',
    'AuditOtp001': 'AuditHsm001',
    'yubiotp-001': 'yubihsm-001',
    'YubiOtp': 'YubiHsm',
    'OtpTool': 'HsmAuthTool',
    'otptool': 'hsmauthtool',
}
FILES = {
    'v2/consumer/Directory.Build.props': 'v1/consumer/Directory.Build.props',
    'v2/consumer/Directory.Build.targets': 'v1/consumer/Directory.Build.targets',
    'v2/consumer/src/Yubico.YubiKit.AuditOtp001.Examples.v2.consumer.csproj': 'v1/consumer/src/Yubico.YubiKit.AuditHsm001.Examples.v1.consumer.csproj',
    'v2/annotations/Directory.Build.props': 'v1/annotations/Directory.Build.props',
    'v2/annotations/Directory.Build.targets': 'v1/annotations/Directory.Build.targets',
    'v2/annotations/src/Yubico.YubiKit.AuditOtp001.Examples.v2.annotations.csproj': 'v1/annotations/src/Yubico.YubiKit.AuditHsm001.Examples.v1.annotations.csproj',
    'overlays/otptool-native-1/OtpTool.Capture.targets': 'v1/hsmauthtool/HsmAuthTool.Capture.targets',
    'dispatch/src/Yubico.YubiKit.AuditOtp001.Examples.Shipping.OtpTool.csproj': 'dispatch/src/Yubico.YubiKit.AuditHsm001.Examples.Shipping.HsmAuthTool.csproj',
}


def require(condition, message):
    if not condition:
        raise SystemExit(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def retain(relative, raw):
    path = HERE / relative
    current = HERE
    for part in Path(relative).parts:
        current /= part
        require(not current.is_symlink(), f'Symlinked target: {current}')
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        require(path.read_bytes() == raw, f'Refusing changed existing preparation: {path}')
    else:
        with path.open('xb') as output:
            output.write(raw)


baseline = json.loads((RUN / 'prior-archive-baseline.json').read_text())[-1]
require(baseline['checked'] == 433, 'Wrong frozen source archive')
manifest_bytes = (ROOT / baseline['manifest']).read_bytes()
require(sha(manifest_bytes) == baseline['manifest_sha256'], 'Frozen source manifest changed')
frozen_rows = json.loads(manifest_bytes)['files']
require(len(frozen_rows) == 433, 'Frozen source record count changed')
frozen = {row['path']: row for row in frozen_rows}


def original(relative):
    path = SOURCE / relative
    raw = path.read_bytes()
    row = frozen[str(path.relative_to(SOURCE_RUN))]
    require(len(raw) == row['bytes'] and sha(raw) == row['sha256'], f'Frozen tooling source changed: {path}')
    return path, raw


copies = []
for before, after in FILES.items():
    path, raw = original(before)
    text = raw.decode('utf-8')
    counts = {}
    substitutions = dict(COMMON)
    if after.endswith('.csproj') and '/src/Yubico.YubiKit.AuditHsm001.Examples.v1.' in after:
        substitutions['Examples.v2.'] = 'Examples.v1.'
    for old, new in substitutions.items():
        counts[old] = text.count(old)
        text = text.replace(old, new)
    data = text.encode('utf-8')
    retain(after, data)
    copies.append(dict(source=str(path.relative_to(ROOT)), source_sha256=sha(raw), target=after,
                       target_sha256=sha(data), substitutions=substitutions, substitution_counts=counts))
adaptations = []
for name, changes in {
    'inventory.py': 'Keep bounded fence/inline/XML identity algorithms; use explicit post-editor scope marker/report hash, dynamic edited paths and HSM-only shared-section classifications. Guard final document reads until accepted scope exists.',
    'capture.py': 'Keep actual Csc/source/reference/analyzer/config/assets retention; require final extraction manifest; use HSM identities, all 22 shipping sources, and corrected native bin/obj/initial-extensions checks from the final OTP overlay. Observe shipping compilation through a late-only hook.',
    'verify.py': 'Keep current-body/source/config and optional live dependency checks; use HSM identities, explicit per-item dispositions and native-output sample capture; require all selected compiler contexts and final extraction-manifest identity.',
}.items():
    path, raw = original(name)
    adaptations.append(dict(source=str(path.relative_to(ROOT)), source_sha256=sha(raw), target=name,
                            target_sha256=sha((HERE / name).read_bytes()), adaptation=changes))
path, raw = original('overlays/otptool-native-1/capture.py')
adaptations.append(dict(source=str(path.relative_to(ROOT)), source_sha256=sha(raw), target='capture.py',
                        target_sha256=sha((HERE / 'capture.py').read_bytes()), adaptation='Carry the proven final native-output assertions into the initial HSM capture; omit historical overlay reuse and original OTP evidence bindings.'))
new_tools = [dict(path=p.name, bytes=p.stat().st_size, sha256=sha(p.read_bytes())) for p in sorted(HERE.glob('*.py'))]
sample = ROOT / 'src/YubiHsm/examples/HsmAuthTool/HsmAuthTool.csproj'
record = dict(baseline='a74792facb657413e98223e059876d8b213ffff3', status='skeleton-awaiting-editor-completion',
              source_manifest=baseline, copies=copies, adaptations=adaptations, tools=new_tools,
              sample_context=dict(path=str(sample.relative_to(ROOT)), bytes=sample.stat().st_size, sha256=sha(sample.read_bytes())),
              policy='Fresh tooling/configuration only. Final OTP results/bodies/old early-output hook are excluded. Consumer/annotation Examples.cs files and final extraction manifest await coordinator editor-complete notification.')
retain('tooling-provenance.json', (json.dumps(record, indent=2) + '\n').encode('utf-8'))
print('Prepared fresh HSM consumer/annotation host skeletons, native-output shipping traversal/late hook and provenance; final snippet extraction awaits editor completion.')
