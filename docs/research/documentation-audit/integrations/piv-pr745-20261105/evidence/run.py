"""Run one integration gate and retain rotating text logs immediately, including failures."""
from pathlib import Path
import json
import shlex
import subprocess
import sys

HERE = Path(__file__).resolve().parent
ROOT = Path('/home/rupicola/Code/y/Yubico.Yubikit.NET')
name, *command = sys.argv[1:]
assert name.replace('-', '').isalnum() and command
with (HERE / (name + '.log')).open('x') as out:
    out.write('$ ' + shlex.join(command) + '\n')
    out.flush()
    result = subprocess.run(command, cwd=ROOT, stdout=out, stderr=subprocess.STDOUT)
    out.write(f'\nEXIT={result.returncode}\n')
latest = ROOT / 'artifacts/toolchain/latest'
if command[:2] == ['dotnet', 'toolchain.cs'] and latest.exists():
    target = HERE / (name + '-toolchain')
    target.mkdir()
    for path in sorted(latest.rglob('*')):
        if path.is_file() and path.suffix in ('.log', '.json', '.txt', '.md'):
            data = path.read_bytes()
            data.decode('utf-8')
            dest = target / path.relative_to(latest)
            dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_bytes(data)
print((HERE / (name + '.log')).read_text())
raise SystemExit(result.returncode)
