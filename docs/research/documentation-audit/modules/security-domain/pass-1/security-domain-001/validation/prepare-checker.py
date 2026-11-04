"""Prepare the existing campaign checker with this run's baseline and isolated output paths."""
from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
SOURCE = ROOT / 'docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001/validation/checker-source'
DEST = HERE / 'checker'
replacements = {
    '5b09a9b6942ded3913b5c9340c3e126617e61a82': 'b73efe3813b429b4b8eb2a7e819a6ad7be699d47',
    'modules/ctap2/pass-1/ctap2-001/': 'modules/security-domain/pass-1/security-domain-001/',
    '/tmp/opencode/ctap2-001-doccheck': '/tmp/opencode/security-domain-001-doccheck',
    'Yubico.YubiKit.Ctap2.DocCheck': 'Yubico.YubiKit.AuditSd001.Checker',
}
files = {
    'src/Program.cs': 'src/Program.cs',
    'Directory.Build.props': 'Directory.Build.props',
    'Directory.Build.targets': 'Directory.Build.targets',
    'src/Yubico.YubiKit.AuditCt001.Checker.csproj': 'src/Yubico.YubiKit.AuditSd001.Checker.csproj',
}
records = []
for old, new in files.items():
    source = SOURCE / old
    target = DEST / new
    text = source.read_text()
    for before, after in replacements.items():
        text = text.replace(before, after)
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists() and target.read_text() != text:
        raise RuntimeError(f'Refusing to overwrite changed checker: {target}')
    target.write_text(text)
    records.append({'source': str(source.relative_to(ROOT)), 'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
                    'target': str(target.relative_to(HERE)), 'target_sha256': hashlib.sha256(target.read_bytes()).hexdigest()})
(HERE / 'checker-provenance.json').write_text(json.dumps({'replacements': replacements, 'files': records}, indent=2) + '\n')
print('Prepared existing token/API checker; algorithms unchanged, run identity/output paths adapted. No build performed.')
