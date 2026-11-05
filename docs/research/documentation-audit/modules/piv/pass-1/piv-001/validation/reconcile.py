"""Reconcile final accepted hashes and unchanged inventory bytes, including Git normalization.

Run after final review and formatting. The accepted manifest supplies baseline and
files[{path, final_sha256}]. This read-only tool prints its receipt for the logger.
"""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess

HERE = Path(__file__).resolve().parent
RUN = HERE.parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
BASELINE = '727fe43afc430c5d8364d868a6436de50626e476'


def require(condition, message):
    if not condition:
        raise SystemExit(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def safe(relative):
    require(not Path(relative).is_absolute() and '..' not in Path(relative).parts and '\\' not in relative, f'Unsafe path: {relative}')
    current = ROOT
    for part in Path(relative).parts:
        current /= part
        require(not current.is_symlink(), f'Symlinked input: {current}')
    return current


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--reviewed', required=True, help='Run-local final accepted manifest; exact final hashes')
args = parser.parse_args()
accepted_path = safe(args.reviewed)
require(accepted_path.is_relative_to(RUN), 'Accepted manifest belongs in this run')
accepted = json.loads(accepted_path.read_text())
require(accepted['baseline'] == BASELINE, 'Accepted baseline mismatch')
rows = accepted['files']
require(bool(rows) and len(rows) == len({row['path'] for row in rows}), 'Require distinct nonempty accepted paths')
reviewed = {row['path']: row for row in rows}
for row in rows:
    require(sha(safe(row['path']).read_bytes()) == row['final_sha256'], f'Final accepted hash changed: {row["path"]}')
inventory = json.loads((RUN / 'inventory.json').read_text())
require(inventory['baseline'] == BASELINE and len(inventory['files']) == 123, 'Inventory baseline mismatch')
normalized = []
unchanged = []
for row in inventory['files']:
    path = row['path']
    original = subprocess.check_output(['git', 'show', BASELINE + ':' + path], cwd=ROOT)
    require(sha(original) == row['git_sha256'], f'Baseline Git hash mismatch: {path}')
    current = safe(path).read_bytes()
    if path in reviewed:
        continue
    exact = sha(current) == row['sha256']
    git_equal = sha(current) == row['git_sha256']
    lf_equal = sha(current.replace(b'\r\n', b'\n')) == row['git_sha256']
    require(exact or git_equal or lf_equal, f'Unreviewed inventory change: {path}')
    unchanged.append(path)
    if not exact:
        normalized.append(dict(path=path, baseline_working_sha256=row['sha256'], baseline_git_sha256=row['git_sha256'], final_sha256=sha(current), reason='Git/LF normalization only'))
print(json.dumps(dict(baseline=BASELINE, accepted_manifest_sha256=sha(accepted_path.read_bytes()),
                     accepted_paths=len(rows), inventory_paths=123, unchanged_paths=len(unchanged),
                     normalization=normalized,
                     limits='Exact accepted final hashes and unchanged inventory reconciliation; semantic coverage/sampling, token/directive/API and compiler evidence retain separate gates.'), indent=2))
