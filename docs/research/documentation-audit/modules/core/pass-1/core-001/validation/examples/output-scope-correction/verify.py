"""Read-only EX-R1 scope/descriptor delta and unchanged-pilot confirmation."""
import hashlib
import importlib.util
import json
from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
E = HERE.parent
RUN = E.parents[1]
ROOT = E.parents[8]
sys.path.insert(0, str(E.parent))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    preserved = json.loads((HERE / 'preserved-predecessors.json').read_bytes())
    assert sha(ROOT / preserved['review']['path']) == preserved['review']['sha256']
    for row in preserved['files']:
        path = ROOT / row['snapshot']
        assert path.stat().st_size == row['bytes'] and sha(path) == row['sha256']
    before_path = HERE / 'preparation-identities.json.snapshot'
    before = json.loads(before_path.read_bytes())
    current_path = E / 'preparation-identities.json'
    current = json.loads(current_path.read_bytes())
    old_rows = {row['path']: row for row in before['files']}
    new_rows = {row['path']: row for row in current['files']}
    assert len(old_rows) == 183 and len(new_rows) == 185
    assert all(new_rows.get(path) == row for path, row in old_rows.items())
    additions = set(new_rows) - set(old_rows)
    assert additions == {(RUN / 'example-validation-preparation.md').relative_to(ROOT).as_posix(),
                         (HERE / 'approved-scope.json').relative_to(ROOT).as_posix()}
    for row in new_rows.values():
        path = ROOT / row['path']
        assert path.stat().st_size == row['bytes'] and sha(path) == row['sha256'], row['path']
    assert not {'README.md', 'CLAUDE.md'}.intersection(new_rows)
    scope = json.loads((HERE / 'approved-scope.json').read_bytes())
    assert scope['primary'] == str(ROOT) and len(scope['projects']) == 5
    runner = load('example_scoped_runner', E / 'run.py')
    closure = {str(path) for path in runner.closure(runner.capture.PROJECT)}
    declared = set()
    for row in scope['projects']:
        project = (RUN / row['project']) if row['project'].startswith('validation/') else ROOT / row['project']
        declared.add(str(project))
        if row['assembly'].endswith('.DocExamples'):
            assert row['bin'].endswith('/core-001/jobs/' + row['assembly'] + '/bin')
            assert row['obj'].endswith('/core-001/jobs/' + row['assembly'] + '/obj')
        else:
            assert Path(row['bin']) == project.parent / 'bin' and Path(row['obj']) == project.parent / 'obj'
            ancestor = project.parent
            while not (ancestor / 'Directory.Build.props').is_file():
                ancestor = ancestor.parent
            assert ancestor == ROOT, 'Referenced SDK project acquired unexpected early routing'
        assert '/tmp/' not in row['bin'] and '/tmp/' not in row['obj']
        assert '.DocCheck/' not in row['bin'] and '.DocCheck/' not in row['obj']
    assert closure == declared
    original_static = json.loads((HERE / 'static-verification.json.snapshot').read_bytes())
    for row in original_static['checked_owned_utf8_records']:
        relative = row['path']
        if relative in additions or relative == current_path.relative_to(ROOT).as_posix():
            continue
        assert sha(ROOT / relative) == row['raw_sha256'], relative
    pilot = load('unchanged_example_verifier', E / 'verify.py').main()
    state = runner.inputs()
    return dict(scope='Static approved-output ownership and exact predecessor/current preparation delta only; no build or runtime execution.',
                original_fixed_rows_unchanged=183, added_fixed_inputs=sorted(additions),
                original_descriptor_sha256=sha(before_path), current_descriptor_sha256=sha(current_path),
                immutable_review_sha256=sha(E / 'review.md'), scope_approval_sha256=sha(HERE / 'approved-scope.json'),
                source_bodies_wrappers_hooks_and_qualified_tools_unchanged=True,
                root_readme_claude_not_descriptor_inputs=True, five_project_scope=scope['projects'],
                original_body_manifest_sha256=sha(E / 'manifest.json'), pilot=pilot,
                current_source_context_files=len(state['files']),
                pending='Bounded independent confirmation; then coordinator examples-ac-first canonical clean compile.')


if __name__ == '__main__':
    print(json.dumps(main(), indent=2))
