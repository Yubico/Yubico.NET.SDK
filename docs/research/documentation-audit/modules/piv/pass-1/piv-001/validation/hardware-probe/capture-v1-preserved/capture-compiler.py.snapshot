"""Retain immediate actual-Csc membership, policy and current dependency identities."""
from pathlib import Path
import argparse
import hashlib
import json

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
SOURCE = Path('/tmp/opencode/piv-001-hardware')
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('destination', type=Path)
args = parser.parse_args()
args.destination.mkdir(parents=True, exist_ok=False)
names = ('compile-inputs.txt', 'resolved-references.txt', 'analyzers.txt',
         'analyzer-configs.txt', 'csc-command-line.txt', 'policy-final.txt')
rows = []
for name in names:
    data = (SOURCE / name).read_bytes()
    with (args.destination / name).open('xb') as output:
        output.write(data)
    rows.append(dict(path=name, bytes=len(data), sha256=hashlib.sha256(data).hexdigest()))
command = (SOURCE / 'csc-command-line.txt').read_text(encoding='utf-8-sig')
policy = (SOURCE / 'policy-final.txt').read_text(encoding='utf-8-sig')
if not command.strip() or 'PivTool.dll' not in command:
    raise SystemExit('Actual Csc arguments must contain the real PivTool reference; retain failure and rebuild')
for expected in ['TreatWarningsAsErrors=true', 'EnableNETAnalyzers=true', 'RunAnalyzersDuringBuild=true']:
    if expected.lower() not in policy.lower():
        raise SystemExit('Compiler policy mismatch: ' + expected)
dependencies = []
for name in ('resolved-references.txt', 'analyzers.txt', 'analyzer-configs.txt', 'compile-inputs.txt'):
    for line in (SOURCE / name).read_text(encoding='utf-8-sig').splitlines():
        path = Path(line.split('|', 1)[0])
        data = path.read_bytes()
        dependencies.append(dict(kind=name, path=str(path), bytes=len(data), sha256=hashlib.sha256(data).hexdigest()))
sample = ROOT / 'src/Piv/examples/PivTool'
sample_sources = [dict(path=str(p.relative_to(ROOT)), sha256=hashlib.sha256(p.read_bytes()).hexdigest())
                  for p in sorted(sample.rglob('*')) if p.is_file() and p.suffix in ('.cs', '.csproj')
                  and not ({'bin', 'obj'} & set(p.relative_to(sample).parts))]
with (args.destination / 'capture.json').open('x') as output:
    json.dump(dict(files=rows, dependency_identities=dependencies, actual_sample_sources=sample_sources,
                   capture_tool_sha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
                   scope='Actual host Csc membership/policy plus immediate dependency hashes; current sample source identities. Runtime DLL identities appear in probe events.'), output, indent=2)
    output.write('\n')
print(f'Retained actual Csc context and {len(dependencies)} dependency identities at {args.destination}')
