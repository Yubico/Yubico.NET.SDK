#!/usr/bin/env python3
"""Verify only the sixteen integration-edited C# documents without rewriting them."""
import datetime
import json
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[1]
paths = [row['path'] for row in json.loads((root / 'execution/terminal-newline-correction.json').read_text())['files']]
solution = [p for p in paths if p.startswith('src/Core/src/')]
verifier = [p for p in paths if p.startswith('verification/')]
assert len(solution) == 15 and len(verifier) == 1
rows = []
for label, project, includes in [
    ('format-core', 'Yubico.YubiKit.sln', solution),
    ('format-linux-verifier', 'verification/LinuxHidRouteVerification/LinuxHidRouteVerification.csproj', verifier),
]:
    destination = root / 'validation' / label
    destination.mkdir(exist_ok=False)
    command = ['dotnet', 'format', project, '--verify-no-changes', '--no-restore', '--include', *includes,
               '--report', str(destination / 'report.json'), '--verbosity', 'diagnostic']
    with (destination / 'console.txt').open('w') as output:
        result = subprocess.run(command, stdout=output, stderr=subprocess.STDOUT)
    row = {'actor': 'GPT-6.1 Sol', 'platform': 'Linux integration host', 'command': command,
           'exit_code': result.returncode, 'includes': includes,
           'candidate_tree': subprocess.check_output(['git', 'write-tree'], text=True).strip(),
           'finished_utc': datetime.datetime.now(datetime.timezone.utc).isoformat()}
    (destination / 'receipt.json').write_text(json.dumps(row, indent=2) + '\n')
    rows.append(row)
    print(label, result.returncode)
print(json.dumps(rows, indent=2))
