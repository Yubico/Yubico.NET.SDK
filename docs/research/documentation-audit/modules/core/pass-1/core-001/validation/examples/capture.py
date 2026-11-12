"""One-host observation hook using the qualified late-Csc/filename grammar; compile only."""
import importlib.util
import json
from pathlib import Path
import re
import shlex
import sys

HERE = Path(__file__).resolve().parent
V = HERE.parent
sys.path.insert(0, str(V))
from foundation import ROOT, RUNTIME, require, sha

NAME = 'Yubico.YubiKit.Audit20261111.DocExamples'
PROJECT = HERE / 'host/src' / (NAME + '.csproj')
GROUPS = ('sources', 'references', 'analyzers', 'configs', 'other')


def identity(path):
    path = Path(path).resolve()
    require(path.is_file() and not path.is_symlink(), 'Missing/redirected compiler input/output: ' + str(path))
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=sha(raw))


def verify(rows):
    require(rows and len({row['path'] for row in rows}) == len(rows), 'Empty/duplicate captured identity population')
    require(all(identity(row['path']) == row for row in rows), 'Captured compiler input/output bytes changed')


def write(path, data):
    with path.open('x') as stream:
        json.dump(data, stream, indent=2)
        stream.write('\n')


def items(directory, suffix=''):
    result = {}
    for group in GROUPS:
        values = [str((PROJECT.parent / value).resolve()) for value in (directory / (group + suffix + '.txt')).read_text().splitlines() if value]
        require(group == 'other' or len(values) == len(set(values)), 'Duplicate compiler item: ' + group)
        result[group] = sorted(set(values)) if group == 'other' else sorted(values)
    return result


def policy(directory):
    return dict(line.split('=', 1) for line in (directory / 'policy.txt').read_text().splitlines())


def before(directory):
    p = policy(directory)
    require(p['Project'] == str(PROJECT) and p['SDK'] == '10.0.404' and p['TFM'] == 'net10.0' and p['Configuration'] == 'Release', 'Unexpected actual example context/SDK')
    require(all(p[key] == 'true' for key in ('WarningsAsErrors', 'Analyzers', 'ProvideArgs')) and
            p['SkipCompiler'] == p['SharedCompiler'] == 'false', 'Compiler/analyzer execution policy differs')
    actual = items(directory)
    manifest = json.loads((HERE / 'manifest.json').read_bytes())
    expected = {str(ROOT / row['generated_host_source']['path']) for row in manifest['examples'] if row['compilation_status'] == 'prepared'}
    require(expected <= set(actual['sources']) and all(value in expected or Path(value).is_relative_to(RUNTIME / 'jobs' / NAME)
            for value in actual['sources']), 'Actual example Csc source membership differs')
    require(any(Path(value).name == 'Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll' for value in actual['analyzers']), 'Canonical analyzer missing')
    inputs = {Path(value) for values in actual.values() for value in values} | {Path(p['Assets']), Path(p['SDKPath']).parent / 'Roslyn/bincore/csc.dll'}
    snapshots = directory / 'sources'
    snapshots.mkdir()
    for source in actual['sources']:
        row = identity(source)
        with (snapshots / (row['sha256'] + '-' + Path(source).name + '.snapshot')).open('xb') as output:
            output.write(Path(source).read_bytes())
    with (directory / 'project.assets.json.snapshot').open('xb') as output:
        output.write(Path(p['Assets']).read_bytes())
    write(directory / 'before.json', dict(policy=p, items=actual, inputs=[identity(path) for path in sorted(inputs)]))


def compiler(directory):
    prior = json.loads((directory / 'before.json').read_bytes())
    verify(prior['inputs'])
    actual = items(directory, '-actual')
    require(actual == prior['items'], 'Actual Csc items changed after before capture')
    lines = (directory / 'csc.txt').read_text().splitlines()
    args = []
    for line in lines:
        value = shlex.split(line)
        require(len(value) == 1, 'Expected one literal Csc argument per item')
        args += value
    require(args and not any(arg.startswith('@') for arg in args), 'Actual executed Csc arguments missing/response-file profile unsupported')
    for group, prefix in [('references', '/reference:'), ('analyzers', '/analyzer:'), ('configs', '/analyzerconfig:')]:
        values = [str((PROJECT.parent / arg[len(prefix):]).resolve()) for arg in args if arg.startswith(prefix)]
        require(sorted(values) == actual[group], 'Csc/item exact membership differs: ' + group)
    sources = [str((PROJECT.parent / arg).resolve()) for arg in args if not re.match(r'^/[A-Za-z][A-Za-z0-9]*:', arg) and arg.endswith('.cs')]
    require(sorted(sources) == actual['sources'], 'Csc source arguments differ')
    # Reuse the qualified comma/quote-aware file descriptor algorithm without modifying its globals.
    bound_path = V / 'checker-binding/bind.py'
    require(sha(bound_path.read_bytes()) == 'a6c2c0954772242d99fed2460f7c473780835605752c59732d32b76002cb8cee', 'Qualified compiler-file parser changed')
    spec = importlib.util.spec_from_file_location('qualified_example_file_parser', bound_path)
    helper = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(helper)
    descriptors = helper.compiler_file_arguments(lines)
    require(all(row['path'] in set(actual['other'] + actual['sources']) for row in descriptors), 'Uncaptured Csc file input')
    require(not any(arg.startswith('/additionalfile:') for arg in args) and '/warnaserror+' in args and '/skipanalyzers+' not in args,
            'Unexpected AdditionalFiles or weakened analyzer/compiler policy')
    outputs = [identity(PROJECT.parent / arg[len(prefix):]) for prefix in ('/out:', '/refout:') for arg in args if arg.startswith(prefix)]
    require(len(outputs) == 2, 'Require actual implementation/reference output')
    outputs += [identity(Path(outputs[0]['path']).with_suffix('.pdb'))]
    write(directory / 'compiler.json', dict(arguments=args, file_arguments=descriptors, outputs=outputs,
          inputs=prior['inputs'], scope='Actual clean compile only; no example execution.'))


def finish(directory):
    prior = json.loads((directory / 'compiler.json').read_bytes())
    verify(prior['inputs'])
    verify(prior['outputs'])
    target = Path(policy(directory)['TargetPath'])
    require(target == RUNTIME / 'jobs' / NAME / 'bin/Release/net10.0' / (NAME + '.dll'), 'Unexpected host output')
    require(identity(target)['sha256'] == prior['outputs'][0]['sha256'], 'Final host DLL differs from actual Csc output')
    write(directory / 'finish.json', dict(outputs=[identity(path) for path in sorted(target.parent.rglob('*')) if path.is_file()],
          compiler_outputs=prior['outputs'], inputs=prior['inputs'], execution_status='not-run'))


if __name__ == '__main__':
    phase, folder = sys.argv[1:]
    directory = Path(folder).resolve()
    require(directory.is_relative_to(HERE / 'attempts') and directory.is_dir(), 'Use a fresh examples attempt')
    {'before': before, 'compiler': compiler, 'finish': finish}[phase](directory)
