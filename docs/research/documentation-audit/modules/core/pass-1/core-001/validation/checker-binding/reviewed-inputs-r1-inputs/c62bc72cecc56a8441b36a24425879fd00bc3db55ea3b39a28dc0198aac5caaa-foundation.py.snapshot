"""Explicit Core contexts and hash-bound reuse of frozen, read-only algorithms."""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
RUN = HERE.parent
ROOT = HERE.parents[7]
BASELINE = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
RUNTIME = Path('/home/rupicola/Code/y/.core-docs-audit-runtime/core-001')
INTEGRATION = ROOT / 'docs/research/documentation-audit/integrations/core-upstream-20261111'
PINS = {
    'run-command-v2.py': '48097a3593e3b71cac83bcc76c6e639608635ece05be2a579f7dc7b0dda3fd15',
    'run-command.py': '61bd7b3f218da8051c44ade033887ddbe598d9d9b2f95cc3fa7a8b8696838640',
    'qualify-logger-v2.py': '60040169cb713d1da60d9486ba7af820622af7980371455350807b3e932ab46a',
    'check-records.py': 'e287c3e35d6fec35d125c870125201816dc4d9822ae78ba94d7c0da167eeb3e4',
    'check-preserved.py': '0581b55098c6c9f3276c9e0766a0bef88a4a3a4380ee7282e2a136f4eac82dfc',
}


def require(value, message):
    if not value:
        raise ValueError(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def pinned(path, expected):
    require(path.is_file() and not path.is_symlink(), f'Require regular pinned input: {path}')
    raw = path.read_bytes()
    require(sha(raw) == expected, f'Pinned input changed: {path}')
    return raw


def baseline():
    data = json.loads(pinned(RUN / 'baseline.json', 'a901bda4ebcbb0c8e7298fb577d56c88a2f9da8ff8938e3d5e5dd7c30714a5fa'))
    require(data['baseline'] == BASELINE and Path(data['primary']) == ROOT and
            Path(data['scratch_root']) == RUNTIME, 'Reconcile Core run/root/source pin')
    return data


def git(*args, raw=None):
    return subprocess.check_output(['git', *args], cwd=ROOT, input=raw)


def frozen(name):
    path = INTEGRATION / 'validation' / name
    raw = pinned(path, PINS[name])
    spec = importlib.util.spec_from_file_location('core_bound_' + name.replace('-', '_'), path)
    module = importlib.util.module_from_spec(spec)
    exec(compile(raw, str(path), 'exec'), module.__dict__)
    return module


def environment():
    """Called only by an assigned execution; all temporary/job state stays home-backed."""
    for name in ('tmp', 'jobs', 'qualification'):
        path = RUNTIME / name
        path.mkdir(parents=True, exist_ok=True)
        require(not path.is_symlink() and path.resolve() == path, f'Runtime path redirects: {path}')
    values = {name: str(RUNTIME / 'tmp') for name in ('TMPDIR', 'TMP', 'TEMP')}
    values['PYTHONDONTWRITEBYTECODE'] = '1'
    os.environ.update(values)
    return values


def participating(names, local):
    """Bind fixed participating files only; later unrelated helpers leave old proofs valid."""
    rows = [dict(path=(INTEGRATION / 'validation' / name).relative_to(ROOT).as_posix(),
                 sha256=sha(pinned(INTEGRATION / 'validation' / name, PINS[name]))) for name in names]
    for name in ('foundation.py', *local):
        path = HERE / name
        rows.append(dict(path=path.relative_to(ROOT).as_posix(), sha256=sha(path.read_bytes())))
    return dict(source_baseline=BASELINE, metadata_head=git('rev-parse', 'HEAD').decode().strip(), files=rows)
