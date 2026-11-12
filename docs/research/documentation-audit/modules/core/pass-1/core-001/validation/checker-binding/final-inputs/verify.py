"""Bounded three-comment-input transition; no build or old-seal live replay."""
import hashlib
import importlib.util
import json
from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
BINDING = HERE.parent
V = BINDING.parent
ROOT = BINDING.parents[8]
sys.path.insert(0, str(V))
PROFILE_SHA = 'db8347bc9370f25bfc5c99edac854691d5baeca41ae4687fa794cb398a4a8ff1'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    spec = importlib.util.spec_from_file_location('unchanged_final_checker_binder', BINDING / 'bind.py')
    binder = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(binder)
    old_path, new_path = BINDING / 'reviewed-inputs-third.json', BINDING / 'reviewed-inputs-final-edits.json'
    assert sha(old_path) == 'ceea8be4c6681c15c51f4611de9546c35f1428f3a6d44de893a434fdab7cd2f1'
    new = binder.profile(new_path, PROFILE_SHA)
    old = json.loads(old_path.read_bytes())
    before, after = ({row['path']: row for row in profile['inputs']} for profile in (old, new))
    assert before.keys() == after.keys() and len(before) == 248
    changed = {path for path in before if before[path] != after[path]}
    expected = {str(ROOT / 'scripts/toolchain' / name) for name in ('Process.cs','Results.cs','TestTargets.cs')}
    assert changed == expected
    transition = json.loads((HERE / 'three-input-transition.json').read_bytes())
    for row in transition['deltas']:
        assert row['old'] == before[str(ROOT / row['path'])] and row['current'] == after[str(ROOT / row['path'])]
        for key in ('original_snapshot', 'diff', 'accepted_receipt'):
            binder.verify_rows([row[key]])
        original = Path(row['original_snapshot']['path']).read_text()
        current = (ROOT / row['path']).read_text()
        noncomments = lambda value: [line for line in value.splitlines() if not line.lstrip().startswith('//')]
        assert noncomments(original) == noncomments(current)
    preserved = json.loads((HERE / 'old-output-preservation.json').read_bytes())
    binder.verify_rows([preserved['old_profile'], preserved['old_seal']])
    for pair in preserved['files']:
        binder.verify_rows([pair['preserved']])
        assert pair['preserved']['bytes'] == pair['original']['bytes'] and pair['preserved']['sha256'] == pair['original']['sha256']
    assert sha(BINDING / 'bind.py') == 'a6c2c0954772242d99fed2460f7c473780835605752c59732d32b76002cb8cee'
    assert sha(V / 'foundation.py') == 'c62bc72cecc56a8441b36a24425879fd00bc3db55ea3b39a28dc0198aac5caaa'
    assert sha(V / 'run-command.py') == '648234e0eaa442249047a3c9c1337b9ec97d8e4431338d0048b9319b71f9b9bf'
    assert sha(V / 'checker/src/Program.cs') == '7c2d0df344f925ef1fccfe1333f9e40cc6bddc0a86dfa22cfa7e9f413128898b'
    return dict(scope='Read-only exact three-comment-input transition and retained old outputs; no compiler/checker run or whole-framework requalification.',
                final_profile=binder.identity(new_path), original_participants=248, changed_participants=sorted(changed),
                unchanged_participants=245, preserved_mutable_old_files=len(preserved['files']),
                old_profile_seal_captures_preserved=True, binder_logger_checker_sources_unchanged=True,
                final_build_status='pending bounded independent review and coordinator checker-bound-build-final-edits',
                old_live_seal='Intentionally not replayed against changed toolchain inputs.')


if __name__ == '__main__':
    print(json.dumps(main(), indent=2))
