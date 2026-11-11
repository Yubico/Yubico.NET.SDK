"""Read-only mixed-version inventory/retained/current comparison; no SDK or filesystem capture."""
import hashlib
import importlib.util
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
PROOF = HERE.parent
sys.path.insert(0, str(PROOF))
CONTEXTS = [('probe', 'probe-first', 'probe-first-v3', 'v3'),
            ('examples', 'examples-first', 'examples-first-v3', 'v3'),
            ('annotations', 'annotations-first', 'annotations-first-v3', 'v3'),
            ('consumers', 'consumers-first', 'consumers-first-v4', 'v4'),
            ('fixture', 'fixture-first', 'fixture-first-v5', 'v5'),
            ('mac-comparison', 'mac-comparison-restore-isolated-first', 'mac-comparison-restore-isolated-first-v5', 'v5')]


def load_module(version):
    path = PROOF / ('proof-' + version + '.py')
    spec = importlib.util.spec_from_file_location('final_original_' + version, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def base_for(version, capture, receipt_sha):
    module = load_module(version)
    if module.check_receipt() != receipt_sha:
        raise ValueError('Original revision receipt differs from captured binding')
    return (module.module if version == 'v5' else module.source_namespace)(capture, receipt_sha)


def identity(path, cache):
    name = str(path)
    if name not in cache:
        if not path.is_file() or path.is_symlink():
            cache[name] = dict(path=name, state='missing-or-not-regular')
        else:
            raw = path.read_bytes()
            cache[name] = dict(path=name, bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())
    return cache[name]


def same(actual, expected):
    return actual.get('bytes') == expected['bytes'] and actual.get('sha256') == expected['sha256']


def mutable_roots(data):
    roots = set()
    for project in data['projects']:
        policy = project['policy']
        directory = Path(project['project']).parent
        for key in ('BaseIntermediateOutputPath', 'MSBuildProjectExtensionsPath', 'IntermediateOutputPath',
                    'BaseOutputPath', 'OutputPath', 'TargetDir'):
            value = policy.get(key)
            if value:
                roots.add((directory / value.replace('\\', '/')).resolve())
    return sorted(roots)


def main():
    if len(sys.argv) != 2 or Path(sys.argv[1]).name != sys.argv[1]:
        raise ValueError('Supply one fresh report filename')
    destination = HERE / sys.argv[1]
    if destination.exists():
        raise ValueError('Preserve the existing matrix report')
    cache, captured, origins = {}, [], {}
    for context, attempt, capture, version in CONTEXTS:
        folder = PROOF / 'evidence' / capture
        data = json.loads((folder / 'manifest.json').read_bytes())
        if (data['context'], data['attempt'], data['capture_name'], data['proof_revision']) != (context, attempt, capture, version):
            raise ValueError('Context/capture identity differs')
        captured.append((context, attempt, capture, version, folder, data))
        for row in data['records']:
            origins.setdefault((row['path'], row['bytes'], row['sha256']), set()).add(capture)
    rows = []
    for context, attempt, capture, version, folder, data in captured:
        base = base_for(version, capture, data['correction_receipt_sha256'])
        current_tools = base['tool_identities']()
        old_tools = {row['path']: row for row in data['tools']}
        now_tools = {row['path']: row for row in current_tools}
        if len(old_tools) != len(data['tools']) or len(now_tools) != len(current_tools):
            raise ValueError('Duplicate tool path')
        tool_changes = [dict(expected=row, current=identity(Path(path), cache)) for path, row in old_tools.items()
                        if path not in now_tools or not same(identity(Path(path), cache), row)]
        additions = [row for row in current_tools if row['path'] not in old_tools]
        retained_errors, drift = [], []
        roots = mutable_roots(data)
        for row in data['records']:
            retained = Path(row['retained']) if row['binary'] else folder / row['retained']
            kept = identity(retained, cache)
            if not same(kept, row):
                retained_errors.append(dict(expected=row, retained_actual=kept))
            actual = identity(Path(row['path']), cache)
            if not same(actual, row):
                matches = sorted(origins.get((row['path'], actual.get('bytes'), actual.get('sha256')), set()))
                drift.append(dict(path=row['path'], kind=row['kind'], binary=row['binary'],
                                  expected_bytes=row['bytes'], expected_sha256=row['sha256'], current=actual,
                                  in_declared_build_tree=any(Path(row['path']).is_relative_to(root) for root in roots),
                                  matching_capture_origins=matches))
        rows.append(dict(context=context, attempt=attempt, capture=capture, version=version,
                         manifest=identity(folder / 'manifest.json', cache), tools_captured=len(old_tools),
                         tools_now=len(now_tools), changed_or_missing_captured_tools=tool_changes,
                         added_tools=additions, records=len(data['records']), retained_errors=retained_errors,
                         live_drift=drift, mutable_build_roots=[str(root) for root in roots]))
    report = dict(scope='Read-only metadata preflight, not SDK/capture execution or replacement of original proof',
                  contexts=rows, unique_files_read=len(cache))
    with destination.open('x') as stream:
        json.dump(report, stream, indent=2)
        stream.write('\n')
    print(json.dumps([dict(context=row['context'], captured_tools=row['tools_captured'], current_tools=row['tools_now'],
                           tool_changes=len(row['changed_or_missing_captured_tools']), additions=len(row['added_tools']),
                           retained_errors=len(row['retained_errors']), live_drift=len(row['live_drift']),
                           unattributed=sum(not value['matching_capture_origins'] for value in row['live_drift']),
                           outside_build_tree=sum(not value['in_declared_build_tree'] for value in row['live_drift'])) for row in rows], indent=2))


if __name__ == '__main__':
    main()
