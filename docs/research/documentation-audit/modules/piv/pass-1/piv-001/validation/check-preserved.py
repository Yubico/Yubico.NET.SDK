"""Read-only verification against this run's archive and original-user-work baselines."""
from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
RUN = HERE.parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())

def require(condition, message):
    if not condition:
        raise SystemExit(message)

def read_safe(folder, relative):
    path = folder / relative
    require(not Path(relative).is_absolute() and '..' not in Path(relative).parts, f'Unsafe path: {relative}')
    current = folder
    for part in Path(relative).parts:
        current /= part
        require(not current.is_symlink(), f'Symlinked evidence: {current}')
    return path.read_bytes()

archives = json.loads((RUN / 'prior-archive-baseline.json').read_text())
require(len(archives) == 7 and sum(r['files'] for r in archives) == 3401, 'Unexpected prior archive baseline counts')
for archive in archives:
    raw = read_safe(ROOT, archive['manifest'])
    require(hashlib.sha256(raw).hexdigest() == archive['manifest_sha256'], f"Changed archive manifest: {archive['manifest']}")
    manifest = json.loads(raw)
    rows = manifest['files']
    require(len(rows) == archive['files'], f"Archive baseline mismatch: {archive['manifest']}")
    folder = (ROOT / archive['manifest']).parent
    if archive['manifest'] == 'docs/research/documentation-audit/record-manifest.json':
        folder /= 'records'
    require(len({row['path'] for row in rows}) == len(rows), 'Duplicate archive paths')
    for row in rows:
        data = read_safe(folder, row['path'])
        require(len(data) == row['bytes'] and hashlib.sha256(data).hexdigest() == row['sha256'], f"Changed prior archive record: {folder / row['path']}")
    print(f"PASS: {len(rows)} prior archive records; baseline manifest hash verified: {archive['manifest']}")
users = json.loads((RUN / 'user-work-baseline.json').read_text())
expected = {'docs/research/audit-v2-closeout-review.md', 'docs/research/audit-v2-execution.md',
            'docs/research/audit-v2-jira-traceability.md', 'mise.toml'}
require(len(users) == 4 and {row['path'] for row in users} == expected, 'Original user-work path set changed')
for row in users:
    data = read_safe(ROOT, row['path'])
    require(len(data) == row['bytes'] and hashlib.sha256(data).hexdigest() == row['sha256'], f"Changed original user file: {row['path']}")
integration = json.loads((RUN / 'integration-baseline.json').read_text())
require(integration['commit'] == '727fe43afc430c5d8364d868a6436de50626e476', 'Integration baseline changed')
require(len(integration['files']) == len({row['path'] for row in integration['files']}) == 114, 'Integration path set changed')
for row in integration['files']:
    data = read_safe(ROOT, row['path'])
    require(hashlib.sha256(data).hexdigest() == row['sha256'], f"Changed integration evidence: {row['path']}")
print('PASS: 3,401 prior artifacts, four original user files and 114 PR745 integration files preserved.')
