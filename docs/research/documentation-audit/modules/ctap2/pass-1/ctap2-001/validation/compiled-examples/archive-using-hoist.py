"""One-shot preserve owned failed inputs/host before correction; no .NET invocation."""
from pathlib import Path
import hashlib
import json
import shutil

HERE = Path(__file__).resolve().parent
VALIDATION = HERE.parent
DESTINATION = HERE / 'failure-using-hoist'
assert not DESTINATION.exists(), 'Never overwrite historical failure evidence'
sources = list(HERE.rglob('*'))
sources = [p for p in sources if p.is_file() and p.name != Path(__file__).name]
sources += [VALIDATION/'run-contexts.py', VALIDATION.parent/'snippet-preparation.md',
            VALIDATION/'checker-source/prepare-project-context.py',
            VALIDATION/'checker-source/context/contexts-manifest.json',
            * (VALIDATION/'checker-source/context').glob('*.template')]
host = VALIDATION/'checker-source/context/consumer-lib'
assert host.is_dir()
sources += [p for p in host.rglob('*') if p.is_file()]
sources += [p for p in (VALIDATION/'context-runs').rglob('*') if p.is_file()]
mapping = []
for source in sources:
    relative = source.relative_to(VALIDATION.parent)
    destination = DESTINATION/relative
    if destination.suffix == '.csproj':
        destination = destination.with_name(destination.name + '.snapshot')
    destination.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(source,destination)
    sha = hashlib.sha256(source.read_bytes()).hexdigest()
    assert hashlib.sha256(destination.read_bytes()).hexdigest() == sha
    mapping.append(dict(source=str(source),archived=str(destination),sha256=sha))
assert not list(DESTINATION.rglob('*.csproj')), 'Archived host must not participate in discovery'
(DESTINATION/'archive-manifest.json').write_text(json.dumps(dict(status='Immutable complete failed-input/script/config/host/log snapshot before using-hoist fix', files=mapping),indent=2)+'\n')
print(f'Preserved {len(mapping)} exact files; all copies hash-match; archived projects use .csproj.snapshot')
