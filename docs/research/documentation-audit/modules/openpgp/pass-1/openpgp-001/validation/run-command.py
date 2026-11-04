"""Run once; retain output and text toolchain evidence before five-run rotation.

Reused from final frozen YubiHSM validation/run-command.py. Binary/cache files
are indexed, not copied. This wrapper does not authorize the command it receives.
"""
from pathlib import Path
import hashlib
import json
import re
import shlex
import subprocess
import sys

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
if len(sys.argv) < 3:
    raise SystemExit('Usage: run-command.py <unique-lowercase-name> <command> [args...]')
name, *command = sys.argv[1:]
if not re.fullmatch(r'[a-z0-9-]+', name):
    raise SystemExit('Require a unique lowercase name containing letters/digits/hyphens')
log = HERE / f'{name}.log'
if log.exists():
    raise SystemExit(f'Refusing to overwrite prior evidence: {log}')
with log.open('x') as output:
    output.write('$ ' + shlex.join(command) + '\n')
    output.flush()
    try:
        result = subprocess.run(command, cwd=ROOT, stdout=output, stderr=subprocess.STDOUT)
        exit_code = result.returncode
    except OSError as error:
        output.write(f'LAUNCH ERROR: {error}\n')
        exit_code = 127
    output.write(f'\nEXIT={exit_code}\n')
text = log.read_text()
for run_id in sorted(set(re.findall(r'artifacts/toolchain/runs/([^/\s)]+)/run\.log', text))):
    if not re.fullmatch(r'[A-Za-z0-9+_.-]+', run_id) or run_id in ('.', '..'):
        raise SystemExit(f'Unsafe toolchain run identifier: {run_id}')
    source = ROOT / 'artifacts/toolchain/runs' / run_id
    destination = HERE / f'{name}-{run_id}'
    destination.mkdir(exist_ok=False)
    rows = []
    for path in sorted(source.rglob('*')):
        relative = path.relative_to(source)
        if path.is_symlink():
            raise SystemExit(f'Refusing symlinked retained evidence: {path}')
        if not path.is_file():
            continue
        data = path.read_bytes()
        reason = None
        if any(part.lower() in ('bin', 'obj', '__pycache__', '.cache') for part in relative.parts):
            reason = 'generated binary/cache directory'
        elif path.suffix.lower() in ('.dll', '.exe', '.pdb', '.binlog', '.nupkg', '.snupkg', '.pyc', '.cache'):
            reason = 'binary/cache artifact'
        else:
            try:
                data.decode('utf-8')
                if b'\0' in data:
                    reason = 'NUL-containing artifact'
            except UnicodeDecodeError:
                reason = 'non-UTF-8 artifact'
        rows.append(dict(path=str(relative), bytes=len(data), sha256=hashlib.sha256(data).hexdigest(),
                         retained=reason is None, exclusion=reason))
        if reason is None:
            target = destination / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            with target.open('xb') as output:
                output.write(data)
    index = destination / 'retention-index.json'
    with index.open('x') as output:
        json.dump(dict(source=str(source.relative_to(ROOT)), files=rows), output, indent=2)
        output.write('\n')
print(text, end='')
raise SystemExit(exit_code)
