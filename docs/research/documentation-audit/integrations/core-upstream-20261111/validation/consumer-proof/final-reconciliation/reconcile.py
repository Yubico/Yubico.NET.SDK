"""Final read-only scope: current inputs/tools + exact retained tested outputs; live replacement is reported, not erased."""
import argparse
import contextlib
import io
import json
import os
from pathlib import Path
import sys

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import matrix


def require(value, message):
    if not value:
        raise ValueError(message)


def key(row):
    return (row['path'], row['kind'], row['bytes'], row['sha256'])


def evaluate(scope):
    cache, manifests, origins = {}, {}, {}
    for row in scope['source_and_record_bindings']:
        require(matrix.same(matrix.identity(Path(row['path']), cache), row), 'Final source/record binding changed: ' + row['path'])
    for expected in scope['contexts']:
        path = Path(expected['manifest']['path'])
        data = json.loads(path.read_bytes())
        require((data['context'], data['attempt'], data['capture_name'], data['proof_revision']) ==
                (expected['context'], expected['attempt'], expected['capture'], expected['version']), 'Captured context identity differs')
        manifests[expected['capture']] = data
        for record in data['records']:
            origins.setdefault((record['path'], record['bytes'], record['sha256']), set()).add(expected['capture'])
    results = []
    for expected in scope['contexts']:
        data = manifests[expected['capture']]
        base = matrix.base_for(expected['version'], expected['capture'], data['correction_receipt_sha256'])
        actual_inventory = base['tool_identities']()
        captured = {row['path']: row for row in data['tools']}
        actual = {row['path']: row for row in actual_inventory}
        approved = {row['path']: row for row in expected['additive_inventory']}
        require(len(captured) == len(data['tools']) and len(actual) == len(actual_inventory) and
                len(approved) == len(expected['additive_inventory']), 'Duplicate tool inventory identity')
        require(not (set(captured) & set(approved)) and set(actual) == set(captured) | set(approved),
                'Tool population differs from exact captured plus reviewed additive set')
        for path, row in (captured | approved).items():
            require(actual[path] == row and matrix.same(matrix.identity(Path(path), cache), row),
                    'Captured/additive tool missing or changed: ' + path)

        def exact_captured_inventory():
            # Only after all actual source hashes and the exact reviewed additive population pass.
            return data['tools']

        base['tool_identities'] = exact_captured_inventory
        buffer = io.StringIO()
        retained_error = None
        try:
            with contextlib.redirect_stdout(buffer):
                base['verify'](expected['attempt'], False)  # Original retained/graph/source/receipt predicates.
        except Exception as error:
            retained_error = str(error)
        changes, invalid = [], []
        allowed = {tuple(row['captured_record_key']): row for row in expected['reviewed_output_replacements']}
        require(len(allowed) == len(expected['reviewed_output_replacements']), 'Duplicate reviewed replacement key')
        build_roots = matrix.mutable_roots(data)
        for record in data['records']:
            current = matrix.identity(Path(record['path']), cache)
            if matrix.same(current, record):
                continue
            permit = allowed.get(key(record))
            matches = sorted(origins.get((record['path'], current.get('bytes'), current.get('sha256')), set()))
            valid = (record['binary'] and permit is not None and matrix.same(current, permit['current']) and
                     any(Path(record['path']).is_relative_to(root) for root in build_roots) and
                     matches == permit['matching_capture_states'] and bool(matches))
            change = dict(captured_record_key=list(key(record)), current=current, matching_capture_states=matches,
                          classification='reviewed shared-build output replacement; historical retained bytes remain mandatory' if valid else 'UNAPPROVED live drift')
            changes.append(change)
            if not valid:
                invalid.append(change)
        strict_error = None
        try:
            with contextlib.redirect_stdout(io.StringIO()):
                base['verify'](expected['attempt'], True)  # Keep original strict-live truth, including its failure.
        except Exception as error:
            strict_error = str(error)
        results.append(dict(context=expected['context'], capture=expected['capture'],
                            original_retained_predicates_pass=retained_error is None, retained_error=retained_error,
                            original_strict_live_pass=strict_error is None, original_strict_live_error=strict_error,
                            captured_tools=len(captured), reviewed_additive_tools=len(approved),
                            records=len(data['records']), current_path_drift=changes, unapproved_drift=invalid,
                            scoped_source_and_exact_retained_pass=retained_error is None and not invalid,
                            retained_verifier_stdout=buffer.getvalue()))
    return results


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--scope', type=Path, default=HERE / 'scope.json')
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    output = args.output.resolve()
    require(output.parent == HERE and not output.exists(), 'Write only a fresh local final-reconciliation report')
    scope = json.loads(args.scope.read_bytes())
    def audit(event, arguments):
        if event == 'open':
            path, mode, flags = arguments
            writing = ((isinstance(mode, str) and any(value in mode for value in ('w', 'a', 'x', '+'))) or
                       (isinstance(flags, int) and flags & (os.O_WRONLY | os.O_RDWR | os.O_CREAT | os.O_TRUNC | os.O_APPEND)))
            if writing and (not isinstance(path, (str, os.PathLike)) or Path(path).resolve() != output):
                raise PermissionError('Read-only reconciliation forbids write open: ' + str(path))
        if event in ('os.mkdir', 'os.remove', 'os.rename', 'os.rmdir', 'subprocess.Popen', 'os.system'):
            raise PermissionError('Read-only reconciliation forbids mutation/process event: ' + event)
    sys.addaudithook(audit)
    results = evaluate(scope)
    passed = all(row['scoped_source_and_exact_retained_pass'] for row in results)
    report = dict(status='current-source/exact-retained-output scope passed' if passed else 'final reconciliation FAILED',
                  scope_identity=matrix.identity(args.scope.resolve(), {}), contexts=results,
                  boundary='Read-only final verification, not SDK/capture execution. Original strict all-current-path status is reported separately. '
                           'No historical output is rewritten or claimed equal to a replacement. No new runtime/API/hardware qualification.')
    with output.open('x') as stream:
        json.dump(report, stream, indent=2)
        stream.write('\n')
    print(json.dumps([dict(context=row['context'], retained=row['original_retained_predicates_pass'],
                           scoped=row['scoped_source_and_exact_retained_pass'], strict_live=row['original_strict_live_pass'],
                           live_drift=len(row['current_path_drift']), unapproved=len(row['unapproved_drift'])) for row in results], indent=2))
    if not passed:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
