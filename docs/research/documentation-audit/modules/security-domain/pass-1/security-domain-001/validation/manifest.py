"""Freeze or verify the complete UTF-8 run archive, including tools and failed-attempt evidence."""
from pathlib import Path
import hashlib
import json
import sys

RUN=Path(__file__).resolve().parent.parent
manifest=RUN/'record-manifest.json'
def sha(data):return hashlib.sha256(data).hexdigest()
files=sorted(p for p in RUN.rglob('*') if p.is_file() and p!=manifest)
if sys.argv[1:] == ['write']:
    if manifest.exists():raise SystemExit('Manifest already exists; refusing to overwrite frozen evidence')
    rows=[]
    for p in files:
        assert not p.is_symlink(),p
        data=p.read_bytes(); data.decode('utf-8')
        rows.append(dict(path=str(p.relative_to(RUN)),bytes=len(data),sha256=sha(data),kind='text'))
    result=dict(run='security-domain-001',pass_number=1,baseline='b73efe3813b429b4b8eb2a7e819a6ad7be699d47',
        snapshot_date='2026-11-04',record_count=len(rows),total_bytes=sum(r['bytes'] for r in rows),
        scope='All run artifacts excluding this manifest; generated binaries/runtime caches were never copied; meaningful source/configuration/results and dependency identities retained.',files=rows)
    manifest.write_text(json.dumps(result,indent=2)+'\n')
elif sys.argv[1:] != ['verify']:
    raise SystemExit('Usage: manifest.py write|verify')
data=json.loads(manifest.read_text())
assert {str(p.relative_to(RUN)) for p in files}=={r['path'] for r in data['files']}
for row in data['files']:
    raw=(RUN/row['path']).read_bytes()
    assert len(raw)==row['bytes'] and sha(raw)==row['sha256'],row['path']
assert data['record_count']==len(data['files']) and data['total_bytes']==sum(r['bytes'] for r in data['files'])
print(f'PASS: {data["record_count"]} retained run artifacts, {data["total_bytes"]} bytes; every path/hash/size reconciled.')
