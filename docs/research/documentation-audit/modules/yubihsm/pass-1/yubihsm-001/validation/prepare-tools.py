"""Reproduce frozen YubiOTP tooling with explicit substitutions; prepare sources only."""
from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
RUN = HERE.parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
SOURCE_RUN = ROOT / 'docs/research/documentation-audit/modules/yubiotp/pass-1/yubiotp-001'
BASELINE = 'a74792facb657413e98223e059876d8b213ffff3'
REPLACEMENTS = {
    '95e5ccaeda33954c21f2a38058d4f66b5f208787': BASELINE,
    'modules/yubiotp/pass-1/yubiotp-001/': 'modules/yubihsm/pass-1/yubihsm-001/',
    '/tmp/opencode/yubiotp-001-doccheck': '/tmp/opencode/yubihsm-001-doccheck',
    'Yubico.YubiKit.AuditOtp001.Checker': 'Yubico.YubiKit.AuditHsm001.Checker',
}
CHECKER_FILES = {
    'checker/src/Program.cs': 'checker/src/Program.cs',
    'checker/Directory.Build.props': 'checker/Directory.Build.props',
    'checker/Directory.Build.targets': 'checker/Directory.Build.targets',
    'checker/src/Yubico.YubiKit.AuditOtp001.Checker.csproj': 'checker/src/Yubico.YubiKit.AuditHsm001.Checker.csproj',
}
UTILITY_REPLACEMENTS = {
    'run-command.py': {
        'Adapted from frozen Security Domain validation/run-command.py.':
            'Reused from frozen YubiOTP validation/run-command.py.',
    },
    'check-preserved.py': {
        '[784, 402, 534, 164]': '[784, 402, 534, 164, 433]',
    },
    'check-records.py': {},
    'manifest.py': {
        '95e5ccaeda33954c21f2a38058d4f66b5f208787': BASELINE,
        "'yubiotp-001'": "'yubihsm-001'",
        'text sources/configuration/provenance/results and three exact registered primary-source PDFs retained':
            'text sources/configuration/provenance/results and explicitly registered primary-source PDFs retained (allowlist initially empty)',
    },
}


def require(condition, message):
    if not condition:
        raise SystemExit(message)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def retain(relative, data):
    target = HERE / relative
    current = HERE
    for part in Path(relative).parts:
        current /= part
        require(not current.is_symlink(), f'Symlinked target: {current}')
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists():
        require(target.read_bytes() == data, f'Refusing to overwrite changed preparation: {target}')
    else:
        with target.open('xb') as output:
            output.write(data)


archives = json.loads((RUN / 'prior-archive-baseline.json').read_text())
require([r['checked'] for r in archives] == [784, 402, 534, 164, 433], 'Unexpected prior archive counts')
archive = archives[-1]
require(archive['manifest'] == str((SOURCE_RUN / 'record-manifest.json').relative_to(ROOT)), 'Unexpected tooling source archive')
manifest_bytes = (ROOT / archive['manifest']).read_bytes()
require(sha(manifest_bytes) == archive['manifest_sha256'], 'Frozen source manifest changed')
frozen = json.loads(manifest_bytes)['files']
require(len(frozen) == archive['checked'], 'Frozen source archive count changed')
frozen = {r['path']: r for r in frozen}


def copy_source(old, new, replacements):
    source = SOURCE_RUN / 'validation' / old
    original = source.read_bytes()
    expected = frozen['validation/' + old]
    require(len(original) == expected['bytes'] and sha(original) == expected['sha256'], f'Frozen source changed: {source}')
    text = original.decode('utf-8')
    counts = {}
    for before, after in replacements.items():
        counts[before] = text.count(before)
        text = text.replace(before, after)
    data = text.encode('utf-8')
    retain(new, data)
    return dict(source=str(source.relative_to(ROOT)), source_sha256=sha(original),
                target=new, target_sha256=sha(data), replacements=replacements,
                substitution_counts=counts)


checker = [copy_source(old, new, REPLACEMENTS) for old, new in CHECKER_FILES.items()]
utilities = [copy_source(name, name, replacements) for name, replacements in UTILITY_REPLACEMENTS.items()]
source_archive = dict(manifest=archive['manifest'], manifest_sha256=archive['manifest_sha256'], checked=archive['checked'])
retain('checker-provenance.json', (json.dumps(dict(
    baseline=BASELINE, source_archive=source_archive, files=checker,
    policy='Roslyn token/directive/API algorithms and compile subcommand unchanged; only baseline/run-path/output-path/checker-identity substitutions. No examples or import-removal approvals copied.'
), indent=2) + '\n').encode('utf-8'))
baselines = [dict(path=str((RUN / name).relative_to(ROOT)), sha256=sha((RUN / name).read_bytes()))
             for name in ('prior-archive-baseline.json', 'user-work-baseline.json')]
retain('utility-provenance.json', (json.dumps(dict(
    source_archive=source_archive, files=utilities, baselines=baselines,
    generator=dict(path='prepare-tools.py', sha256=sha(Path(__file__).read_bytes()))
), indent=2) + '\n').encode('utf-8'))
print('Prepared AuditHsm001 checker and four validation tools with exact frozen-source provenance; execution gates remain coordinator-owned.')
