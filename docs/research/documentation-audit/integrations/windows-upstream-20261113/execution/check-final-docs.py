#!/usr/bin/env python3
"""Run final documentation gates and retain the fresh logs with the executing actor."""
import datetime
import hashlib
import json
from pathlib import Path
import shutil
import subprocess

root = Path(__file__).resolve().parents[1]
destination = root / 'validation/final-documentation'
destination.mkdir(exist_ok=False)
assert not subprocess.check_output(['git', 'diff', '--name-only'])
command = ['dotnet', 'toolchain.cs', '--', 'docs-qa', 'docs-architecture']
tree = subprocess.check_output(['git', 'write-tree'], text=True).strip()
with (destination / 'console.txt').open('w') as output:
    result = subprocess.run(command, stdout=output, stderr=subprocess.STDOUT)
shutil.copytree('artifacts/toolchain/latest', destination / 'toolchain', symlinks=False)
receipt = {'actor': 'GPT-6.1 Sol', 'platform': 'Linux integration host', 'command': command,
           'candidate_tree': tree, 'exit_code': result.returncode,
           'finished_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
           'files': [{'path': p.relative_to(destination).as_posix(), 'bytes': p.stat().st_size,
                      'sha256': hashlib.sha256(p.read_bytes()).hexdigest()}
                     for p in sorted(destination.rglob('*')) if p.is_file()]}
(destination / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
print((destination / 'console.txt').read_text())
raise SystemExit(result.returncode)
