"""One-shot archive of owned failed MSBuild intermediate/output workspace; no build."""
from pathlib import Path
import hashlib
import json
import shutil

HERE = Path(__file__).resolve().parent
source = Path('/tmp/opencode/ctap2-001-consumer-lib-context')
destination = HERE/'failure-using-hoist/failed-msbuild-workspace'
assert source.is_dir() and not destination.exists()
mapping = [dict(source=str(p),archived=str(destination/p.relative_to(source)),sha256=hashlib.sha256(p.read_bytes()).hexdigest())
           for p in source.rglob('*') if p.is_file()]
assert not list(source.rglob('*.csproj'))
shutil.move(str(source),destination)
for record in mapping:
    assert hashlib.sha256(Path(record['archived']).read_bytes()).hexdigest() == record['sha256']
(HERE/'failure-using-hoist/workspace-manifest.json').write_text(json.dumps(dict(status='Preserved entire failed MSBuild workspace before replacement; includes assets/generated inputs/resolved references/analyzers', files=mapping),indent=2)+'\n')
print(f'Preserved complete failed workspace: {len(mapping)} files, exact hashes')
