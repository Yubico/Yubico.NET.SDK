"""Prepare source-only PIV utilities from pinned final OpenPGP validation sources."""
from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
RUN = HERE.parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
SOURCE = ROOT / 'docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001'
BASELINE = '727fe43afc430c5d8364d868a6436de50626e476'


def require(condition, message):
    if not condition:
        raise SystemExit(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def retain(relative, raw):
    require(not (RUN / 'record-manifest.json').exists(), 'Run is frozen')
    target = HERE / relative
    current = HERE
    for part in Path(relative).parts:
        current /= part
        require(not current.is_symlink(), f'Symlinked target: {current}')
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists():
        require(target.read_bytes() == raw, f'Changed prepared target: {target}')
    else:
        with target.open('xb') as output:
            output.write(raw)


archives = json.loads((RUN / 'prior-archive-baseline.json').read_text())
require(sum(row['files'] for row in archives) == 3401 and len(archives) == 7,
        'Prior archive baseline changed')
archive = next(row for row in archives if row['manifest'] == str((SOURCE / 'record-manifest.json').relative_to(ROOT)))
manifest_raw = (ROOT / archive['manifest']).read_bytes()
require(sha(manifest_raw) == archive['manifest_sha256'], 'Frozen source manifest changed')
rows = json.loads(manifest_raw)['files']
require(len(rows) == archive['files'] == 607, 'Frozen source archive count changed')
frozen = {row['path']: row for row in rows}
provenance = []


def copy_source(old, new=None, replacements=None, cuts=None, truncate=None):
    original = (SOURCE / 'validation' / old).read_bytes()
    expected = frozen['validation/' + old]
    require(len(original) == expected['bytes'] and sha(original) == expected['sha256'], f'Frozen source changed: {old}')
    text = original.decode('utf-8')
    operations = []
    if truncate:
        require(text.count(truncate) == 1, f'Ambiguous truncation: {truncate}')
        text = text[:text.index(truncate)]
        operations.append(dict(operation='truncate-before-marker', marker=truncate))
    for start, end in cuts or []:
        require(text.count(start) == text.count(end) == 1, f'Ambiguous cut: {start}')
        lo, hi = text.index(start), text.index(end)
        removed = text[lo:hi]
        text = text[:lo] + text[hi:]
        operations.append(dict(operation='remove-between-markers', start=start, end=end,
                               removed_sha256=sha(removed.encode()), removed_bytes=len(removed.encode())))
    for before, after in (replacements or {}).items():
        count = text.count(before)
        require(count > 0, f'Substitution absent: {old}: {before}')
        text = text.replace(before, after)
        operations.append(dict(operation='literal-replace', before=before, after=after, count=count))
    raw = text.encode('utf-8')
    target = new or old
    retain(target, raw)
    provenance.append(dict(source=str((SOURCE / 'validation' / old).relative_to(ROOT)),
                           source_sha256=sha(original), target=target, target_sha256=sha(raw),
                           operations=operations))


# Retain combined output and rotated toolchain text before preparing the checker.
copy_source('run-command.py', replacements={
    'Reused from final frozen YubiHSM validation/run-command.py.':
        'Reused from final frozen OpenPGP validation/run-command.py.',
    "name, *command = sys.argv[1:]":
        "if (HERE.parent / 'record-manifest.json').exists():\n    raise SystemExit('Run is frozen')\nname, *command = sys.argv[1:]",
})
copy_source('check-records.py', replacements={
    "print('NOT RUN: final reviewed-files hash check (use --reviewed after review). No module content or external target contents opened.')":
        "print('PENDING: final reviewed-files hash check; use --reviewed after review. Scope: record text and local-target existence.')",
})
copy_source('manifest.py', replacements={
    'fe180282a2d77b15eaaa5868d6c8b00e251d1f05': BASELINE,
    "'openpgp-001'": "'piv-001'",
})
copy_source('check-preserved.py', replacements={
    "require([r['checked'] for r in archives] == [784, 402, 534, 164, 433, 477], 'Unexpected prior archive baseline counts')":
        "require(len(archives) == 7 and sum(r['files'] for r in archives) == 3401, 'Unexpected prior archive baseline counts')",
    "require(archive['result'] == 'pass' and len(rows) == archive['checked'],":
        "require(len(rows) == archive['files'],",
    "print('PASS: all four original user files unchanged; prior archives were read only.')":
        """integration = json.loads((RUN / 'integration-baseline.json').read_text())
require(integration['commit'] == '727fe43afc430c5d8364d868a6436de50626e476', 'Integration baseline changed')
require(len(integration['files']) == len({row['path'] for row in integration['files']}) == 114, 'Integration path set changed')
for row in integration['files']:
    data = read_safe(ROOT, row['path'])
    require(hashlib.sha256(data).hexdigest() == row['sha256'], f"Changed integration evidence: {row['path']}")
print('PASS: 3,401 prior artifacts, four original user files and 114 PR745 integration files preserved.')""",
})
copy_source('checker/src/Program.cs', replacements={
    'fe180282a2d77b15eaaa5868d6c8b00e251d1f05': BASELINE,
    '            ["tokens-import-removals", var root, var manifest] => await CheckImportRemovalsAsync(Path.GetFullPath(root), manifest),\n': '',
    '            ["compile", var source, var root, .. var modules] => Compile(source, Path.GetFullPath(root), modules),\n': '',
    'Usage: DocCheck tokens <repo> | tokens-import-removals <repo> <run-local import-removals.json> | api <repo> | compile <source.cs> <repo> [SDK module ...] [--reference <assembly.dll> ...] [--identity-comparer default|desktop]':
        'Usage: AuditPiv001.Checker tokens <repo> | api <repo>',
    'if (oldPaths.Length == 0 || !oldPaths.SequenceEqual(currentPaths))':
        'if (oldPaths.Length != 20 || !oldPaths.SequenceEqual(currentPaths))',
}, cuts=[
    ('static async Task<int> CheckImportRemovalsAsync(', 'static (string[] Tokens, string[] Trivia) Shape('),
    ('static int Compile(', 'static string Hash('),
])
for name in ('Directory.Build.props', 'Directory.Build.targets', 'src/Yubico.YubiKit.AuditPgp001.Checker.csproj'):
    replacements = {}
    if name.endswith('.props'):
        replacements['/tmp/opencode/openpgp-001-doccheck'] = '/tmp/opencode/piv-001-doccheck'
    if name.endswith('.csproj'):
        replacements['Yubico.YubiKit.AuditPgp001.Checker'] = 'Yubico.YubiKit.AuditPiv001.Checker'
    copy_source('checker/' + name, 'checker/' + name.replace('AuditPgp001', 'AuditPiv001'), replacements)

# Reuse the corrected package proof independently of historical extraction receipts.
copy_source('examples/analyzer-package-1/analyzer_policy.py', 'examples/analyzer_policy.py', truncate='def receipt():')

retain('primary-binary-sources.json', b'[]\n')
baselines = [dict(path=str((RUN / name).relative_to(ROOT)), sha256=sha((RUN / name).read_bytes()))
             for name in ('inventory.json', 'prior-archive-baseline.json', 'user-work-baseline.json', 'integration-baseline.json')]
record = dict(baseline=BASELINE, source_archive=archive, files=provenance, baselines=baselines,
              generator=dict(path='prepare-tools.py', sha256=sha(Path(__file__).read_bytes())),
              policy='Fresh prepared sources only; strict Roslyn tokens/directives and all 20 API snapshots. Import-removal and ad-hoc compilation routes removed. Historical result rows remain in frozen archives.')
retain('tool-provenance.json', (json.dumps(record, indent=2) + '\n').encode())
print('Prepared AuditPiv001.Checker, retention/preservation/record/freeze tools and canonical analyzer proof; execution gates remain coordinator work.')
