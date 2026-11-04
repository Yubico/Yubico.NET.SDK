"""Coordinator-only post-build capture: text compiler evidence and dependency hashes, never binaries."""
from pathlib import Path
import hashlib
import json
import re
import sys

HERE = Path(__file__).resolve().parent
SOURCE = Path('/tmp/opencode/yubihsm-001-hardware')
names = ('compile-inputs.txt', 'resolved-references.txt', 'analyzers.txt',
         'analyzer-configs.txt', 'csc-command-line.txt', 'policy-final.txt')
if len(sys.argv) != 2 or not re.fullmatch(r'[a-z0-9-]+', sys.argv[1]):
    raise SystemExit('Usage: capture-compiler.py <unique-lowercase-evidence-name>')
destination = HERE / 'evidence' / sys.argv[1]
destination.mkdir(parents=True, exist_ok=False)
rows = []
for name in names:
    data = (SOURCE / name).read_bytes()
    data.decode('utf-8')
    with (destination / name).open('xb') as output:
        output.write(data)
    rows.append(dict(path=name, bytes=len(data), sha256=hashlib.sha256(data).hexdigest()))
dependencies = []
for name in ('resolved-references.txt', 'analyzers.txt', 'analyzer-configs.txt', 'compile-inputs.txt'):
    for line in (SOURCE / name).read_text().splitlines():
        path = Path(line.split('|', 1)[0])
        data = path.read_bytes()
        dependencies.append(dict(kind=name, path=str(path), bytes=len(data), sha256=hashlib.sha256(data).hexdigest()))
with (destination / 'capture.json').open('x') as output:
    json.dump(dict(source=str(SOURCE), files=rows, dependency_identities=dependencies,
                   scope='Actual compiler membership/policy plus dependency hashes; no assembly/cache copies, no code executed.'), output, indent=2)
    output.write('\n')
print(f'Retained compiler evidence and {len(dependencies)} dependency identities at {destination}; no binaries copied.')
