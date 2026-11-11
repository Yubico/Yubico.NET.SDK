"""Read-only full representative predicates, with no filesystem retainer or compiler-proof capture."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import sys
import traceback

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
PROOF = HERE.parent
sys.path.insert(0, str(PROOF))


def load_v3():
    spec = importlib.util.spec_from_file_location('consumer_v3_readonly_preflight', PROOF / 'proof-v3.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True)
    args = parser.parse_args()
    output = HERE / args.output
    if output.parent != HERE or output.exists():
        raise ValueError('Use a new local preflight report name')
    v3 = load_v3()
    v2 = v3.accepted_v2()
    old = v2.module('readonly-source-check-not-capture')
    old['check_seal']()
    # This constructs Python predicates only. No capture(), verify(), Retainer or SDK action is called.
    base = v3.source_namespace('readonly-preflight-not-capture', 'unsealed-readonly-preflight')
    base['active_build_guard']()
    data = base['current_inputs']()
    build_receipt, host = base['build_receipt']('consumer-probe-build-first', 'probe', data)
    source, transformations = v3.effective_source()

    class ReadOnlyCollector:
        def __init__(self):
            self.records = []

        def keep(self, path, kind, binary=False):
            # Same identity/text predicates as the real Retainer.keep; no files/directories are created.
            row = dict(base['identity'](path), kind=kind)
            raw = path.read_bytes()
            if not binary:
                raw.decode('utf-8-sig')
                base['require'](b'\0' not in raw, 'Unexpected binary in text input: ' + str(path))
            row.update(binary=binary, retained=None, read_only=True)
            self.records.append(row)
            return row

    raw = base['RUNTIME'] / 'attempts/probe-first/raw'
    folders = sorted(path for path in raw.iterdir() if path.is_dir())
    actual = [str(Path(base['raw_policy'](folder)['MSBuildProjectFullPath']).relative_to(base['ROOT'])) for folder in folders]
    expected = base['closure'](data['declared_graph'], data['contexts']['probe']) | {str(host.relative_to(base['ROOT']))}
    base['require'](base['unique'](actual, 'read-only observed project') == expected, 'Original representative Csc project closure differs')
    collector = ReadOnlyCollector()
    projects, errors = [], []
    for folder in folders:
        try:
            result = base['compiler_project'](folder, data, collector)
            projects.append(dict(status='all compiler_project predicates passed read-only', result=result))
        except Exception as error:
            errors.append(dict(project=base['raw_policy'](folder)['MSBuildProjectFullPath'],
                               type=type(error).__name__, error=str(error), traceback=traceback.format_exc()))
    report = dict(status='read-only representative predicates passed' if not errors else 'read-only representative predicates FAILED',
                  scope='No SDK/build/test/native/device/formatter execution. No capture(), verify(), filesystem Retainer, '
                        'binary copies, snapshots or compiler manifests. Only this explicitly named preflight report is written.',
                  actual_compiler_attempt='probe-first', original_build_receipt=v3.identity(build_receipt),
                  tools=[v3.identity(path) for path in (PROOF / 'proof-v3.py', HERE / 'additional-files.py', Path(__file__))],
                  accepted_v2_review=v3.identity(PROOF / 'correction-v2/independent-review.md'),
                  effective_source_sha256=hashlib.sha256(source.encode()).hexdigest(), transformations=transformations,
                  projects=projects, errors=errors, read_only_collector_records=collector.records)
    with output.open('x') as stream:
        json.dump(report, stream, indent=2)
        stream.write('\n')
    print(f"{report['status']}: {len(projects)} complete project predicates, {len(errors)} failures; {output}")
    if errors:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
