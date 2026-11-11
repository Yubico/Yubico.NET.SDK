"""Isolated source-contract copy guards using retained consumer bytes, never mutable current Core bin files."""
import argparse
import ast
import copy
import hashlib
import json
from pathlib import Path
import sys
from types import SimpleNamespace
import xml.etree.ElementTree as ET

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
        raise ValueError('Use a new local case report name')
    v5 = load('copy_contract_v5', PROOF / 'proof-v5.py')
    namespace = v5.collector_namespace()
    owner_function = namespace['declared_comparison_copy_owner']
    helper = owner_function.__globals__
    project, core = helper['PROJECT'], helper['CORE']
    manifest_path = PROOF / 'evidence/consumers-first-v4/manifest.json'
    manifest = json.loads(manifest_path.read_bytes())
    oracles = {}
    for path in (core, core.with_suffix('.pdb'), core.with_suffix('.xml')):
        rows = [row for row in manifest['records'] if row['path'] == str(path) and row['binary']]
        if not rows or len({(row['bytes'], row['sha256']) for row in rows}) != 1:
            raise ValueError('Retained Core oracle is absent/ambiguous')
        row = rows[0]
        raw = Path(row['retained']).read_bytes()
        if len(raw) != row['bytes'] or hashlib.sha256(raw).hexdigest() != row['sha256']:
            raise ValueError('Retained oracle bytes changed')
        oracles[path] = (row, raw)
    target = project.parent / 'bin/audit-integration-20261111/build/Release/net10.0'
    policy = dict(MSBuildProjectFullPath=str(project), BoundaryCorePath=str(core), BoundaryNativeVersion='1.18.1',
                  BoundaryComparisonVariant='audit-integration-20261111', TargetDir=str(target))
    compiler_bindings = [dict(role='declared-comparison-Core-DLL', binary=dict(oracles[core][0], path=str(core)))]
    source, transformations = v5.effective_collector()
    function = next(node for node in ast.parse(source).body if isinstance(node, ast.FunctionDef) and node.name == 'restored_binary_bindings')
    loop = next(node for node in function.body if isinstance(node, ast.For) and
                isinstance(node.target, ast.Tuple) and [value.id for value in node.target.elts] == ['source', 'destination'])
    skeleton = ast.parse('def evaluate(project, policy, source, destination, compiler_bindings, retain):\n'
                         '    resolved = {str(source): destination}\n    copy_bindings = []\n    pass\n    return copy_bindings\n')
    skeleton.body[0].body[2] = copy.deepcopy(loop)
    ast.fix_missing_locations(skeleton)
    namespace.update(owner=lambda path: None, outputs={})  # Actual comparison declares no package/project owner for Core.
    exec(compile(skeleton, 'exact-v5-copy-loop-source-contract', 'exec'), namespace)
    cases = []

    class OracleReader:
        def __init__(self, source, wrong_copy=False, changed_core=False):
            self.source, self.wrong_copy, self.changed_core, self.records = source, wrong_copy, changed_core, []

        def keep(self, path, kind, binary=False):
            if binary:
                logical = self.source if path == target / self.source.name else path
                if logical not in oracles:
                    raise ValueError('Case attempted to read an unregistered binary oracle')
                raw = oracles[logical][1]
                if self.wrong_copy and path == target / self.source.name:
                    raw += b'\0'  # In-memory negative only; no actual file is written.
                if self.changed_core and path == core:
                    raw += b'\0'
            else:
                raw = path.read_bytes()  # Stable project/wrapper/props source only.
                raw.decode('utf-8-sig')
            row = dict(path=str(path), kind=kind, bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest(),
                       binary=binary, retained=None, source_contract_only=True)
            self.records.append(row)
            return row

    def run(name, expected, source_path=core, case_project=project, case_policy=None,
            bindings=None, wrong_copy=False, changed_core=False, missing_hint=False):
        reader = OracleReader(source_path, wrong_copy, changed_core)
        original_et = helper['ET']
        if missing_hint:
            tree = ET.parse(project)
            node = next(n for n in tree.iter('Reference') if n.attrib.get('Include') == 'Yubico.YubiKit.Core')
            node.remove(node.find('HintPath'))
            helper['ET'] = SimpleNamespace(parse=lambda path: tree if Path(path) == project else ET.parse(path))
        try:
            result = namespace['evaluate'](case_project, case_policy or policy, source_path, target / source_path.name,
                                           compiler_bindings if bindings is None else bindings, reader)
            passed, error = True, None
        except ValueError as failure:
            result, passed, error = None, False, str(failure)
        finally:
            helper['ET'] = original_et
        if passed != expected:
            raise ValueError('Source contract case did not match its required outcome: ' + name + ': ' + str(error))
        cases.append(dict(name=name, expected_admission=expected, observed_admission=passed, error=error,
                          result=result, read_only_oracle_records=reader.records))

    run('exact-declared-Core-DLL-copy', True)
    run('precisely-owned-Core-PDB-sidecar', True, source_path=core.with_suffix('.pdb'))
    run('precisely-owned-Core-XML-sidecar', True, source_path=core.with_suffix('.xml'))
    run('wrong-project-rejected', False, case_project=project.parent / 'Unrelated.csproj')
    run('missing-HintPath-rejected', False, missing_hint=True)
    run('wrong-prepared-Core-property-rejected', False, case_policy=dict(policy, BoundaryCorePath=str(core.parent / 'Other.dll')))
    run('missing-actual-Csc-Core-input-rejected', False, bindings=[])
    run('changed-current-DLL-vs-Csc-input-rejected', False, changed_core=True)
    run('wrong-copy-output-bytes-rejected', False, wrong_copy=True)
    run('unrelated-source-file-rejected', False, source_path=core.parent / 'Other.dll')
    report = dict(status='all ten isolated source-contract cases match required outcomes', cases=cases,
                  scope='Not a Mac compile/capture or simulated complete project proof. Exact copy-loop AST only; '
                        'package lookup has no Core owner and project-reference outputs are empty, matching this direct-reference source contract. '
                        'Binary reads use registered retained consumer oracles; never mutable current Core bin paths. '
                        'Negative data/XML mutations are in memory only. Only this report is written.',
                  retained_manifest=v5.identity(manifest_path), retained_oracles=[value[0] for value in oracles.values()],
                  actual_project_source=v5.identity(project), wrapper=v5.identity(helper['WRAPPER']),
                  tool_sources=[v5.identity(path) for path in (PROOF / 'proof-v5.py', HERE / 'copy-owner.py', Path(__file__))],
                  effective_collector_sha256=hashlib.sha256(source.encode()).hexdigest(),
                  exact_copy_loop_sha256=hashlib.sha256(ast.get_source_segment(source, loop).encode()).hexdigest(),
                  transformations=transformations)
    with output.open('x') as stream:
        json.dump(report, stream, indent=2)
        stream.write('\n')
    print(f'{report["status"]}; no actual Mac compiler/copy operation; {output}')


if __name__ == '__main__':
    main()
