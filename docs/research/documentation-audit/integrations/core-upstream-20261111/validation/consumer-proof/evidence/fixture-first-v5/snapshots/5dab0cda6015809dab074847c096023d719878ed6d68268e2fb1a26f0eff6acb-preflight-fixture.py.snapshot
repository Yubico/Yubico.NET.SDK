"""Full fixture predicates against existing compiler observations; read-only collector and write audit guard."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import sys
import traceback

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
PROOF = HERE.parent
sys.path.insert(0, str(PROOF))
import importlib.util


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True)
    args = parser.parse_args()
    output = HERE / args.output
    if output.parent != HERE or output.exists():
        raise ValueError('Use a new local report name')
    v5 = load('fixture_v5_predicates', PROOF / 'proof-v5.py')
    v5.accepted_v4()
    base = v5.module('readonly-fixture-not-capture', 'unsealed-readonly-preflight')
    readonly = load('accepted_v4_readonly_collector', PROOF / 'correction-v4/preflight.py')

    def audit(event, arguments):
        if event == 'open':
            path, mode, flags = arguments
            writing = ((isinstance(mode, str) and any(value in mode for value in ('w', 'a', 'x', '+'))) or
                       (isinstance(flags, int) and flags & (os.O_WRONLY | os.O_RDWR | os.O_CREAT | os.O_TRUNC | os.O_APPEND)))
            if writing and (not isinstance(path, (str, bytes, os.PathLike)) or Path(path).resolve() != output):
                raise PermissionError('Read-only preflight forbids write open: ' + str(path))
        if event in ('os.mkdir', 'os.remove', 'os.rename', 'os.rmdir', 'subprocess.Popen', 'os.system'):
            raise PermissionError('Read-only preflight forbids mutation/process event: ' + event)

    sys.addaudithook(audit)
    base['active_build_guard']()
    data = base['current_inputs']()
    receipt, host = base['build_receipt']('consumer-fixture-build-first', 'fixture', data)
    raw = base['RUNTIME'] / 'attempts/fixture-first/raw'
    folders = sorted(path for path in raw.iterdir() if path.is_dir())
    actual = [str(Path(base['raw_policy'](folder)['MSBuildProjectFullPath']).relative_to(base['ROOT'])) for folder in folders]
    expected = base['closure'](data['declared_graph'], data['contexts']['fixture']) | {str(host.relative_to(base['ROOT']))}
    base['require'](base['unique'](actual, 'fixture compiler project') == expected, 'Fixture actual compiler closure differs')
    collector = readonly.ReadOnlyCollector(base)
    projects, errors, graphs = [], [], []
    for folder in folders:
        try:
            result = base['compiler_project'](folder, data, collector)
            projects.append(result)
            policy = base['raw_policy'](folder)
            project = Path(policy['MSBuildProjectFullPath'])
            references = {str(base['resolve'](project, line.split('|')[0])) for line in base['lines'](folder / 'references.txt')}
            graph = base['graph_proof'](project, policy, references, folder, data, base['load'](Path(policy['ProjectAssetsFile'])), collector)
            graphs.append(dict(project=str(project), nodes=len(graph['nodes']), status='passed'))
        except Exception as error:
            errors.append(dict(project=base['raw_policy'](folder)['MSBuildProjectFullPath'],
                               error=str(error), traceback=traceback.format_exc()))
    source, transformations = v5.effective_collector()
    report = dict(status='all four fixture project predicates passed read-only' if not errors else 'fixture read-only preflight FAILED',
                  compiler_attempt='fixture-first', successful_build=v5.identity(receipt), projects=projects, errors=errors,
                  independent_graphs=graphs, read_only_records=collector.records,
                  tool_sources=[v5.identity(path) for path in (PROOF / 'proof-v5.py', HERE / 'compile-candidates.py', HERE / 'copy-owner.py', Path(__file__))],
                  effective_collector_sha256=hashlib.sha256(source.encode()).hexdigest(), transformations=transformations,
                  scope='No SDK/test/capture/native/device operation. Read-only identity/text collector creates no retained artifacts. '
                        'Audit hook denies mutation and subprocess execution; only this explicitly named report may be written.')
    with output.open('x') as stream:
        json.dump(report, stream, indent=2)
        stream.write('\n')
    print(f'{report["status"]}: {len(projects)} projects, {len(errors)} failures; {output}')
    if errors:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
