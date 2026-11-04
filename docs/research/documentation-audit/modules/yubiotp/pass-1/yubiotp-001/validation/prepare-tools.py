"""Copy frozen validation sources with explicit substitutions and SHA-256 provenance; no build."""
from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
SOURCE = ROOT / 'docs/research/documentation-audit/modules/security-domain/pass-1/security-domain-001/validation'
REPLACEMENTS = {
    'b73efe3813b429b4b8eb2a7e819a6ad7be699d47': '95e5ccaeda33954c21f2a38058d4f66b5f208787',
    'modules/security-domain/pass-1/security-domain-001/': 'modules/yubiotp/pass-1/yubiotp-001/',
    '/tmp/opencode/security-domain-001-doccheck': '/tmp/opencode/yubiotp-001-doccheck',
    'Yubico.YubiKit.AuditSd001.Checker': 'Yubico.YubiKit.AuditOtp001.Checker',
}
FILES = {
    'checker/src/Program.cs': 'checker/src/Program.cs',
    'checker/Directory.Build.props': 'checker/Directory.Build.props',
    'checker/Directory.Build.targets': 'checker/Directory.Build.targets',
    'checker/src/Yubico.YubiKit.AuditSd001.Checker.csproj': 'checker/src/Yubico.YubiKit.AuditOtp001.Checker.csproj',
}

def sha(data):
    return hashlib.sha256(data).hexdigest()

records = []
for old, new in FILES.items():
    source = SOURCE / old
    target = HERE / new
    original = source.read_bytes()
    text = original.decode('utf-8')
    counts = {}
    for before, after in REPLACEMENTS.items():
        counts[before] = text.count(before)
        text = text.replace(before, after)
    data = text.encode('utf-8')
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists() and target.read_bytes() != data:
        raise SystemExit(f'Refusing to overwrite changed tool: {target}')
    if not target.exists():
        target.write_bytes(data)
    records.append(dict(source=str(source.relative_to(ROOT)), source_sha256=sha(original),
                        target=str(target.relative_to(HERE)), target_sha256=sha(data),
                        substitution_counts=counts))
provenance = dict(baseline='95e5ccaeda33954c21f2a38058d4f66b5f208787',
                  replacements=REPLACEMENTS, files=records,
                  policy='Checker algorithms unchanged; no examples or import-removal approvals copied. Compile subcommand retains historical CTAP defaults and is not the final example-validation host.')
data = (json.dumps(provenance, indent=2) + '\n').encode('utf-8')
target = HERE / 'checker-provenance.json'
if target.exists() and target.read_bytes() != data:
    raise SystemExit('Refusing to overwrite changed checker provenance')
if not target.exists():
    target.write_bytes(data)
adaptations = {
    'run-command.py': 'Retain unique command logs and rotated toolchain text evidence; add shell-safe command display, launch-error retention, binary/cache exclusion index and overwrite/symlink guards.',
    'check-preserved.py': 'Use this run\'s two baseline records; verify manifest hashes and expected 784/402/534/164 counts plus the four original user paths; use explicit fail-closed checks instead of assert.',
    'check-records.py': 'Use current run records only by default, optional explicit reviewed hash and campaign Markdown inputs; dynamic counts, bounded link/fence/text checks; no automatic git diff or external-content reads.',
    'manifest.py': 'Use YubiOTP identity/baseline, reject binary/cache/symlink entries, compare exact current inventory with frozen rows; exclusive manifest creation.',
}
records = []
for name, changes in adaptations.items():
    source = SOURCE / name
    target = HERE / name
    records.append(dict(source=str(source.relative_to(ROOT)), source_sha256=sha(source.read_bytes()),
                        target=name, target_sha256=sha(target.read_bytes()), adaptation=changes))
baselines = [dict(path=str((HERE.parent / name).relative_to(ROOT)),
                  sha256=sha((HERE.parent / name).read_bytes()))
             for name in ('prior-archive-baseline.json', 'user-work-baseline.json')]
data = (json.dumps(dict(files=records, baselines=baselines,
                        generator=dict(path='prepare-tools.py', sha256=sha(Path(__file__).read_bytes()))), indent=2) + '\n').encode('utf-8')
target = HERE / 'utility-provenance.json'
if target.exists() and target.read_bytes() != data:
    raise SystemExit('Refusing to overwrite changed utility provenance')
if not target.exists():
    target.write_bytes(data)
print('Prepared isolated AuditOtp001 checker sources/provenance only; no build or validation executed.')
