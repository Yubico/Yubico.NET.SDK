"""Coordinator-only late capture: reconcile actual Csc sets and retain source/config text."""
from pathlib import Path
import hashlib
import json
import re
import sys

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
WORKSPACE = Path('/tmp/opencode/yubihsm-001-hardware')


def require(condition, message):
    if not condition:
        raise SystemExit(message)


require(len(sys.argv) == 3 and all(re.fullmatch(r'[a-z0-9-]+', name) for name in sys.argv[1:]),
        'Usage: retain-inputs.py <capture-name> <unique-final-input-name>')
capture = HERE / 'evidence' / sys.argv[1]
data = json.loads((capture / 'capture.json').read_text())
destination = HERE / 'evidence' / sys.argv[2]
require(not destination.exists() and not destination.is_symlink(), 'Final-input destination already exists')
projects = list((HERE / 'tests').rglob('*.csproj'))
require(len(projects) == 1, 'Expected exactly one probe project')
project = projects[0]
args = (capture / 'csc-command-line.txt').read_text().splitlines()


def resolve(value):
    return str((project.parent / value.strip('"')).resolve())


for kind, prefix in [('resolved-references.txt', '/reference:'), ('analyzers.txt', '/analyzer:'), ('analyzer-configs.txt', '/analyzerconfig:')]:
    actual = {resolve(arg[len(prefix):]) for arg in args if arg.startswith(prefix)}
    expected = {str(Path(row['path']).resolve()) for row in data['dependency_identities'] if row['kind'] == kind}
    require(bool(actual) and actual == expected, f'Actual Csc membership mismatch: {kind}')
source_args = {resolve(arg) for arg in args if arg.endswith('.cs') and not arg.startswith(('/reference:', '/analyzer:', '/embed:'))}
require(source_args == {str(Path(row['path']).resolve()) for row in data['dependency_identities'] if row['kind'] == 'compile-inputs.txt'}, 'Actual Csc source set mismatch')
require({str(path.resolve()) for path in project.parent.glob('*.cs')} <= source_args and bool(source_args), 'A complete probe source file is missing from actual Csc membership')
require({resolve(arg[len('/embed:'):]) for arg in args if arg.startswith('/embed:')} <= source_args, 'Unexpected embedded source')
require('/warnaserror+' in args, 'Csc warnings-as-errors missing')
require(any('Microsoft.CodeAnalysis.NetAnalyzers.dll' in arg for arg in args), 'Canonical .NET analyzers missing')
require(any('xunit.analyzers' in arg for arg in args), 'Genuine xUnit analyzers missing')
require(not any('/skipanalyzers' in arg.lower() for arg in args), 'Csc analyzers skipped')
require('SkipCompilerExecution=true' not in (capture / 'policy-final.txt').read_text(), 'Compiler execution skipped')
for row in data['files']:
    raw = (capture / row['path']).read_bytes()
    require(len(raw) == row['bytes'] and hashlib.sha256(raw).hexdigest() == row['sha256'], 'Compiler capture text changed')
rows = []
pending = {}
for row in data['dependency_identities']:
    path = Path(row['path'])
    raw = path.read_bytes()
    digest = hashlib.sha256(raw).hexdigest()
    require(digest == row['sha256'] and len(raw) == row['bytes'], f'Dependency changed since actual build: {path}')
    if row['kind'] in ('compile-inputs.txt', 'analyzer-configs.txt'):
        target = digest + '-' + path.name + '.snapshot'
        pending[target] = raw
        rows.append(dict(row, retained=target))
for name in ('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json', '.editorconfig'):
    raw = (ROOT / name).read_bytes()
    target = name + '.snapshot'
    pending[target] = raw
    rows.append(dict(path=str(ROOT / name), bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest(), retained=target))
for path in [HERE / 'Directory.Build.props', HERE / 'Directory.Build.targets', project,
             project.parent / 'xunit.runner.json', project.parent / 'probe.runsettings',
             ROOT / 'src/Tests.Shared/appsettings.json']:
    raw = path.read_bytes()
    digest = hashlib.sha256(raw).hexdigest()
    target = digest + '-' + path.name + '.snapshot'
    pending[target] = raw
    rows.append(dict(path=str(path), bytes=len(raw), sha256=digest, retained=target))
assets = WORKSPACE / 'obj/project.assets.json'
raw = assets.read_bytes()
pending['project.assets.json.snapshot'] = raw
rows.append(dict(path=str(assets), bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest(), retained='project.assets.json.snapshot'))
destination.mkdir(exist_ok=False)
for name, raw in pending.items():
    raw.decode('utf-8')
    require(b'\0' not in raw, 'Unexpected binary compiler input')
    with (destination / name).open('xb') as output:
        output.write(raw)
with (destination / 'manifest.json').open('x') as output:
    json.dump(dict(records=rows, checked_dependencies=len(data['dependency_identities']), capture=sys.argv[1],
                   evidence='Actual Csc source/reference/analyzer/config sets reconcile; compiler/analyzers enabled; full dependency identities match at late capture; source/config/assets text retained; no binary copies or test execution.'), output, indent=2)
    output.write('\n')
print(f"PASS: actual Csc membership and {len(data['dependency_identities'])} dependency identities reconciled; {len(pending)} source/config/assets text snapshots retained.")
