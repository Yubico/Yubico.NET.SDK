"""Reproduce final frozen YubiHSM tools with exact substitutions; sources only."""
from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
RUN = HERE.parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
SOURCE_RUN = ROOT / 'docs/research/documentation-audit/modules/yubihsm/pass-1/yubihsm-001'
BASELINE = 'fe180282a2d77b15eaaa5868d6c8b00e251d1f05'
COUNTS = [784, 402, 534, 164, 433, 477]
CHECKER_REPLACEMENTS = {
    'a74792facb657413e98223e059876d8b213ffff3': BASELINE,
    'modules/yubihsm/pass-1/yubihsm-001/': 'modules/openpgp/pass-1/openpgp-001/',
    '/tmp/opencode/yubihsm-001-doccheck': '/tmp/opencode/openpgp-001-doccheck',
    'Yubico.YubiKit.AuditHsm001.Checker': 'Yubico.YubiKit.AuditPgp001.Checker',
}
CHECKER_FILES = {
    'checker/src/Program.cs': 'checker/src/Program.cs',
    'checker/Directory.Build.props': 'checker/Directory.Build.props',
    'checker/Directory.Build.targets': 'checker/Directory.Build.targets',
    'checker/src/Yubico.YubiKit.AuditHsm001.Checker.csproj': 'checker/src/Yubico.YubiKit.AuditPgp001.Checker.csproj',
}
UTILITY_REPLACEMENTS = {
    'run-command.py': {
        'Reused from frozen YubiOTP validation/run-command.py.':
            'Reused from final frozen YubiHSM validation/run-command.py.',
    },
    'check-preserved.py': {
        '[784, 402, 534, 164, 433]': repr(COUNTS),
    },
    'check-records.py': {},
    'manifest.py': {
        'a74792facb657413e98223e059876d8b213ffff3': BASELINE,
        "'yubihsm-001'": "'openpgp-001'",
    },
}
VERIFIER_REPLACEMENTS = {
    'a74792facb657413e98223e059876d8b213ffff3': BASELINE,
    "== 433": "== 477",
    'frozen YubiOTP manifest rows': 'final frozen YubiHSM manifest rows',
    'AuditHsm001.Checker': 'AuditPgp001.Checker',
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
require([r['checked'] for r in archives] == COUNTS and sum(COUNTS) == 2794,
        'Unexpected prior archive counts')
archive = archives[-1]
require(archive['manifest'] == str((SOURCE_RUN / 'record-manifest.json').relative_to(ROOT)),
        'Unexpected tooling source archive')
manifest_bytes = (ROOT / archive['manifest']).read_bytes()
require(sha(manifest_bytes) == archive['manifest_sha256'], 'Frozen source manifest changed')
frozen_rows = json.loads(manifest_bytes)['files']
require(len(frozen_rows) == archive['checked'], 'Frozen source archive count changed')
frozen = {r['path']: r for r in frozen_rows}


def copy_source(old, new, replacements):
    source = SOURCE_RUN / 'validation' / old
    original = source.read_bytes()
    expected = frozen['validation/' + old]
    require(len(original) == expected['bytes'] and sha(original) == expected['sha256'],
            f'Frozen source changed: {source}')
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


# Make the coordinator's output-retention wrapper available first.
utilities = [copy_source(name, name, replacements)
             for name, replacements in UTILITY_REPLACEMENTS.items()]
checker = [copy_source(old, new, CHECKER_REPLACEMENTS)
           for old, new in CHECKER_FILES.items()]
verifier = copy_source('verify-preparation.py', 'verify-preparation.py', VERIFIER_REPLACEMENTS)
source_archive = dict(manifest=archive['manifest'], manifest_sha256=archive['manifest_sha256'],
                      checked=archive['checked'])
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
retain('verifier-provenance.json', (json.dumps(dict(source_archive=source_archive, files=[verifier]),
                                               indent=2) + '\n').encode('utf-8'))
retain('primary-binary-sources.json', b'[]\n')
print('Prepared AuditPgp001 checker and validation utilities from final YubiHSM sources; coordinator execution gates remain separate.')
