"""Pure/read-only checks for the Core validation boundaries; no command capture."""
import copy
import ast
import importlib.util
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent


def load(name):
    spec = importlib.util.spec_from_file_location(name.replace('-', '_'), HERE / (name + '.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def rejected(action):
    try:
        action()
    except (ValueError, FileNotFoundError):
        return
    raise AssertionError('Boundary accepted a deliberately invalid input')


def main():
    python_files = ['foundation.py', 'run-command.py', 'qualify-logger.py', 'check-records.py',
                    'check-preserved.py', 'preflight.py', 'source-state.py', 'native-tokens.py', 'selfcheck.py']
    for name in python_files:
        ast.parse((HERE / name).read_bytes(), filename=name)
    foundation = load('foundation')
    logger = load('run-command').load()
    qualification_path = foundation.INTEGRATION / 'recovery/logger-v2-qualification-second/qualification.json'
    qualification = json.loads(qualification_path.read_bytes())
    assert qualification['logger_sha256'] == foundation.PINS['run-command-v2.py']
    assert qualification['qualifier_sha256'] == foundation.PINS['qualify-logger-v2.py']
    assert {row['case'] for row in qualification['cases']} == {'nonzero', 'orphan', 'retention', 'receipt'}
    assert all(row['quiescent'] and row['wrapper_exit'] == 1 for row in qualification['cases'])
    piv_source = foundation.ROOT / 'docs/research/documentation-audit/modules/piv/pass-1/piv-001/validation/checker/src/Program.cs'
    foundation.pinned(piv_source, '400b8242e144b4e152409f7b95d382cdbc9ab747ea90324639b7990f069ffd6c')
    assert logger.ROOT == foundation.ROOT
    assert logger.HERE == HERE and logger.RUNTIME == foundation.RUNTIME
    assert logger.original().ROOT == foundation.ROOT
    assert logger.original().HERE == HERE
    assert logger.__file__ == str(HERE / 'run-command.py')
    rejected(lambda: logger.atomic_json(foundation.INTEGRATION / 'validation/forbidden-new-receipt.json', {}))
    parser = logger.parser()
    assert parser.parse_args(['--label', 'later', '--', 'python3']).cwd == foundation.ROOT
    records = load('check-records')
    assert records.load()['ROOT'] == foundation.ROOT
    assert records.load()['PACKET'] == HERE.parent
    for name, raw in [('library.dll.sha256.snapshot', b'a' * 64),
                      ('restore/project.assets.json.snapshot', b'{"targets": {}}')]:
        assert records.classify(name, raw) in ('utf8-text', 'utf8-json')
    for name, raw in [('library.dll', b'text'), ('foo.log', b'MZpayload'),
                      ('foo.snapshot', b'\x7fELFpayload'), ('foo.log', b'abc\0'),
                      ('foo.log', b'\xff'), ('foo.zip.snapshot', b'PK\x03\x04'),
                      ('obj/cache.json', b'{}'), ('foo.json', b'not JSON')]:
        rejected(lambda name=name, raw=raw: records.classify(name, raw))
    preserved = load('check-preserved')
    rejected(lambda: preserved.safe(foundation.ROOT, '../escape'))
    rejected(lambda: preserved.safe(foundation.ROOT, '/absolute'))
    native = load('native-tokens')
    before = '#define N 1\nint f() { return N + 1; } // explanation\n'
    assert native.shape(before) == native.shape(before.replace('explanation', 'clearer explanation'))
    for changed in [before.replace('N 1', 'N 2'), before.replace('N + 1', 'N + 2'),
                    before.replace('N + 1', 'N ++ 1'), before.replace('int f', 'long f')]:
        assert native.shape(before) != native.shape(changed)
    assert native.shape('auto p = R"tag(/*text*/)tag";') != native.shape('auto p = R"tag(/*changed*/)tag";')
    rejected(lambda: native.shape('int x; /* unfinished'))
    rejected(lambda: native.shape('int x; \\\nint y;'))
    result_path = foundation.INTEGRATION / 'validation/commands-v2/main-gates-home-first'
    results = list(result_path.glob('toolchain/*/result.json'))
    assert len(results) == 1
    result = json.loads(results[0].read_bytes())
    expected = json.loads((HERE / 'expected-unit-projects.json').read_bytes()) + ['resilience:Core.UnitTests']
    targets = ['build', 'test', 'docs-qa', 'docs-architecture', 'complexity', 'resilience']
    counts = logger.original().check_result(result, 0, targets, expected)
    for kind in ('exit', 'zero', 'membership', 'warning', 'target'):
        changed = copy.deepcopy(result)
        if kind == 'zero':
            changed['projects'][0]['executed'] = 0
        if kind == 'membership':
            changed['projects'].pop()
        if kind == 'warning':
            changed['warningCount'] = 1
        if kind == 'target':
            changed['targets'] = []
        rejected(lambda: logger.original().check_result(changed, 1 if kind == 'exit' else 0, targets, expected))
    report = dict(status='static-read-only-ready', logger_context='Core run only',
                  historical_parser_counts=counts, negative_cases=23, parsed_python_files=python_files,
                  historical_process_qualification=dict(path=qualification_path.relative_to(foundation.ROOT).as_posix(),
                                                       sha256=foundation.sha(qualification_path.read_bytes()),
                                                       scope='Read retained qualification only; Core process cases remain unrun.'),
                  participating=foundation.participating(['run-command.py', 'run-command-v2.py', 'check-records.py'],
                                                         [name for name in python_files if name != 'foundation.py']),
                  limits='No process qualification, compilation, SDK test, native or device execution.')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
