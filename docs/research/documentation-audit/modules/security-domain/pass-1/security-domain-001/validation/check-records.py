"""Read-only local record/link/current-content checks; no external target contents are opened."""
from pathlib import Path
import hashlib
import json
import re
import subprocess

HERE=Path(__file__).resolve().parent
RUN=HERE.parent
ROOT=next(p for p in HERE.parents if (p/'toolchain.cs').exists())
def sha(data):return hashlib.sha256(data).hexdigest()
for r in json.loads((RUN/'reviewed-files.json').read_text())['files']:
    assert sha((ROOT/r['path']).read_bytes())==r['final_sha256'],r['path']
files=list(RUN.glob('*.md'))+[ROOT/'docs/research/documentation-audit/security-domain.md',ROOT/'docs/research/documentation-audit/workflow.md',ROOT/'docs/research/documentation-audit/README.md']
links=0
for p in files:
    fence=False
    for line in p.read_text().splitlines():
        if re.match(r'^\s*(```|~~~)',line):fence=not fence;continue
        if fence:continue
        for target in re.findall(r'\]\(([^)]+)\)',line):
            if target.startswith(('https:','http:','#','mailto:')):continue
            target=target.split('#',1)[0]
            assert (p.parent/target).exists(),(str(p),target)
            links+=1
    assert not fence,p
for p in RUN.rglob('*'):
    if p.is_file():
        assert not p.is_symlink(),p
        p.read_bytes().decode('utf-8')
subprocess.run(['git','diff','--check'],cwd=ROOT,check=True)
print(f'PASS:26 current authored hashes; {len(files)} current record/handoff Markdown files / {links} local links; balanced fences, UTF-8-only retained artifacts, git diff whitespace clean.')
