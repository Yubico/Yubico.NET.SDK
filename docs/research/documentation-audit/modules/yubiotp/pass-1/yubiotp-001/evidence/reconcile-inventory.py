"""Read tracked module metadata and snapshot; no SDK/test execution or other-module reads."""
import collections
import hashlib
import json
from pathlib import Path
import re
import subprocess

run = Path(__file__).parent.parent
inventory = json.loads((run / 'inventory.json').read_text())
tracked = subprocess.check_output(['git', 'ls-files', 'src/YubiOtp'], text=True).splitlines()
files = []
for entry in inventory['files']:
    path = Path(entry['path'])
    data = path.read_bytes()
    files.append(dict(path=str(path), lines=len(data.splitlines()), bytes=len(data),
                      sha256=hashlib.sha256(data).hexdigest(), baseline_matches=hashlib.sha256(data).hexdigest() == entry['sha256']))
types = []
for entry in files:
    path = entry['path']
    if path.startswith('src/YubiOtp/src/') and path.endswith('.cs'):
        types.extend(re.findall(r'^public (?:(?:abstract|static|sealed|readonly) )*(?:class|interface|enum|struct) (\w+)', Path(path).read_text(), re.M))
protected = {
    'KeyboardSlotConfiguration': ['InitializeKeys', 'KeyboardSlotConfiguration'],
    'SlotConfiguration': ['SetCfgFlag', 'SetExtFlag', 'SetFixed', 'SetHmacKey', 'SetKey', 'SetTktFlag', 'SetUid',
                          'SlotConfiguration', 'ThrowIfDisposed', '_cfgFlags', '_extFlags', '_tktFlags',
                          'Dispose(bool', 'GetEffectiveCfgFlags', 'GetEffectiveExtFlags', 'GetEffectiveTktFlags'],
}
api = []
prefix = 'Yubico.YubiKit.YubiOtp.'
for number, line in enumerate(Path('src/YubiOtp/src/PublicAPI.Unshipped.txt').read_text().splitlines(), 1):
    if not line or line.startswith('#'):
        continue
    stripped = re.sub(r'^(?:static|virtual|override|const) ', '', line)
    if stripped.removeprefix(prefix) in types:
        category = 'public-type'
    elif any(prefix + owner + '.' + member in stripped for owner, members in protected.items() for member in members):
        category = 'protected-subclass-surface'
    elif stripped == prefix + 'ConfigState.ConfigState() -> void':
        category = 'compiler-provided-struct-constructor'
    elif stripped == prefix + 'UpdateConfiguration.UpdateConfiguration() -> void':
        category = 'implicit-public-constructor'
    elif 'IYubiKeyExtensions.extension(' in stripped and ') ->' not in stripped and stripped.endswith('IYubiKey!)'):
        category = 'extension-block-grouping'
    elif line.startswith('static ' + prefix + 'IYubiKeyExtensions.'):
        category = 'duplicate-static-extension-spelling'
    else:
        category = 'other-public-member'
    api.append(dict(line=number, entry=line, category=category))
links = []
for file in ['src/YubiOtp/README.md', 'src/YubiOtp/CLAUDE.md', 'src/YubiOtp/tests/CLAUDE.md']:
    for target in re.findall(r'\]\(([^)]+)\)', Path(file).read_text()):
        if '://' not in target and not target.startswith('#'):
            resolved = Path(file).parent / target.split('#')[0]
            links.append(dict(source=file, target=target, exists=resolved.exists()))
result = dict(baseline=inventory['baseline'], date='2026-11-04', tracked_count=len(tracked), inventory_count=len(files),
              tracked_difference=sorted(set(tracked) ^ {e['path'] for e in files}), files=files,
              public_types=sorted(types), snapshot_entry_count=len(api),
              snapshot_categories=dict(collections.Counter(e['category'] for e in api)), snapshot_entries=api,
              local_links_existence_only=links,
              exclusions='Untracked/generated obj/bin trees are not module inventory; no target contents read outside module.')
(run / 'evidence' / 'local-reconciliation.json').write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps({key: result[key] for key in ['tracked_count', 'inventory_count', 'tracked_difference', 'snapshot_entry_count', 'snapshot_categories']}, indent=2))
print('Hash mismatches:', [e['path'] for e in files if not e['baseline_matches']])
print('Broken local links:', [e for e in links if not e['exists']])
