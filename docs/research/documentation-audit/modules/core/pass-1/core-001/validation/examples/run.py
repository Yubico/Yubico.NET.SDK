"""One accepted A/C compile transaction through the unchanged qualified v2 logger."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import re
import signal
import sys
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
V = HERE.parent
sys.path.insert(0, str(V))
from foundation import ROOT, BASELINE, environment, git, require, sha


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


capture = load('example_capture', HERE / 'capture.py')


def closure(project, seen=None):
    seen = set() if seen is None else seen
    require(project.is_file() and not project.is_symlink(), 'Missing/redirected project reference')
    if project in seen:
        return seen
    seen.add(project)
    for node in ET.parse(project).getroot().findall('.//ProjectReference'):
        relative = node.attrib['Include'].replace('$(MSBuildThisFileDirectory)', '').replace('\\', '/')
        require('$(' not in relative, 'Review conditional/evaluated ProjectReference before extending this pilot')
        closure((project.parent / relative).resolve(), seen)
    return seen


def inputs():
    manifest = json.loads((HERE / 'manifest.json').read_bytes())
    descriptor = json.loads((HERE / 'preparation-identities.json').read_bytes())
    for row in descriptor['files']:
        require(capture.identity(ROOT / row['path'])['sha256'] == row['sha256'], 'Reviewed example preparation participant changed: ' + row['path'])
    for row in manifest['examples']:
        source = ROOT / row['path']
        original = source.read_bytes()
        require(sha(original) == row['source']['sha256'], 'Accepted source document changed; review/re-extract before compilation: ' + row['path'])
        require(sha(original[row['raw_start_byte']:row['raw_end_byte']]) == row['raw_sha256'], 'Original body boundary/hash changed')
        require(sha((ROOT / row['raw_file']).read_bytes()) == row['raw_sha256'], 'Retained original XML region changed')
        require(sha((ROOT / row['decoded_file']).read_bytes()) == row['decoded_sha256'], 'Decoded original body changed')
    projects = sorted(closure(capture.PROJECT))
    files = set(projects)
    for project in projects:
        # This fixed pilot uses ordinary SDK default Compile items; actual host compiler items are independently bound.
        files.update(path for path in project.parent.rglob('*') if path.is_file() and
                     not {'bin', 'obj'}.intersection(path.relative_to(project.parent).parts))
    files.update(ROOT / row['path'] for row in descriptor['files'])
    return dict(source_baseline=BASELINE, metadata_head=git('rev-parse', 'HEAD').decode().strip(),
                declared_project_closure=[str(path) for path in projects],
                files=[capture.identity(path) for path in sorted(files)])


def main(args):
    require(re.fullmatch(r'[a-z0-9][a-z0-9-]*', args.label), 'Use a fresh simple attempt label')
    before = inputs()
    environment()
    require(not os.environ.get('DirectoryBuildTargetsPath') and not os.environ.get('DocExamplesCapture'), 'Existing compiler capture override requires coordinator reconciliation')
    directory = HERE / 'attempts' / args.label
    directory.mkdir(parents=True, exist_ok=False)
    os.environ.update(DirectoryBuildTargetsPath=str(HERE / 'capture.targets'), DocExamplesCapture=str(directory))
    capture.write(directory / 'source-before.json', before)
    logger = load('accepted_examples_logger_adapter', V / 'run-command.py').load()
    command = ['--label', args.label, '--toolchain', '--expect-target', 'build', '--',
               'dotnet', 'toolchain.cs', '--', 'build', '--project', 'Audit20261111.DocExamples', '--clean']
    code = logger.capture(logger.parser().parse_args(command))
    after = inputs()
    capture.write(directory / 'source-after.json', after)
    require(before['files'] == after['files'] and before['declared_project_closure'] == after['declared_project_closure'], 'Source/config/context changed across build')
    receipt = V / 'commands-v2' / args.label / 'receipt.json'
    result = json.loads(receipt.read_bytes())
    require(code == 0 and result['status'] == 'passed' and result['exit_code'] == 0 and result['stream_complete'] and
            result['retention_complete'] and result['quiescent'], 'Preserved example build failed/incomplete; send the actual diagnostic to its editor')
    require(result['result_counts']['warnings'] == result['result_counts']['failed'] == 0, 'Actual RESULT has warning/failure')
    finish = json.loads((directory / 'finish.json').read_bytes())
    capture.verify(finish['inputs'])
    capture.verify(finish['outputs'])
    capture.verify(finish['compiler_outputs'])
    capture.write(directory / 'accepted-compilation.json', dict(source_baseline=BASELINE, command_receipt=capture.identity(receipt),
          captures=[capture.identity(directory / name) for name in ('source-before.json', 'before.json', 'compiler.json', 'finish.json', 'source-after.json')],
          body_manifest=capture.identity(HERE / 'manifest.json'), outputs=finish['outputs'],
          scope='17 accepted A/C bodies compiled in one genuine SDK-reference library context; no example execution.',
          execution_status='not-run', internal_examples='2 explicitly qualified internal illustrations, excluded from this consumer host'))
    print(json.dumps(capture.identity(directory / 'accepted-compilation.json'), indent=2))


if __name__ == '__main__':
    def interrupted(signum, frame):
        raise KeyboardInterrupt('Examples wrapper received SIGTERM')
    signal.signal(signal.SIGTERM, interrupted)
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--label', required=True)
    try:
        main(parser.parse_args())
    except (Exception, KeyboardInterrupt) as error:
        print(f'Example compilation unaccepted: {type(error).__name__}: {error}', file=sys.stderr)
        sys.exit(1)
