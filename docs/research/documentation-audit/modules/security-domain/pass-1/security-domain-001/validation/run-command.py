"""Run a validation command once and retain output plus any toolchain run before rotation."""
from pathlib import Path
import re
import shutil
import subprocess
import sys

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
name, *command = sys.argv[1:]
if not re.fullmatch(r'[a-z0-9-]+', name) or not command:
    raise SystemExit('Usage: run-command.py <unique-lowercase-name> <command> [args...]')
log = HERE / f'{name}.log'
if log.exists():
    raise SystemExit(f'Refusing to overwrite prior evidence: {log}')
with log.open('x') as output:
    output.write('$ ' + ' '.join(command) + '\n')
    output.flush()
    result = subprocess.run(command, cwd=ROOT, stdout=output, stderr=subprocess.STDOUT)
    output.write(f'\nEXIT={result.returncode}\n')
text = log.read_text()
for match in set(re.findall(r'artifacts/toolchain/runs/([^/\s)]+)/run\.log', text)):
    source = ROOT / 'artifacts/toolchain/runs' / match
    shutil.copytree(source, HERE / f'{name}-{match}')
print(text)
raise SystemExit(result.returncode)
