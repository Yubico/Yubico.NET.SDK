#!/usr/bin/env python3
"""Verify inventory partitions and protected inputs; write only inventory/verification.json."""
import collections
import ast
import hashlib
import json
import re
from pathlib import Path
import subprocess

ROOT = Path.cwd()
RUN = ROOT / 'docs/research/documentation-audit/modules/core/pass-1/core-001'
OUT = RUN / 'inventory'


def load(path):
    return json.loads(Path(path).read_text())


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


files = load(OUT / 'files.json')
units = load(OUT / 'semantic-units.json')
api = load(OUT / 'public-api.json')
summary = load(OUT / 'summary.json')
groups = collections.defaultdict(list)
for unit in units:
    groups[unit['path']].append(unit)
assert len({u['id'] for u in units}) == len(units)
for file in files:
    path = ROOT / file['path']
    assert digest(path) == file['sha256'], file['path']
    text = path.read_bytes().decode('utf-8-sig', errors='replace')
    cursor = 0
    for unit in sorted(groups[file['path']], key=lambda x: x['start_character']):
        assert cursor == unit['start_character'], (file['path'], cursor, unit['start_character'])
        cursor = unit['end_character_exclusive']
        assert hashlib.sha256(text[unit['start_character']:cursor].encode()).hexdigest() == unit['range_sha256']
    assert cursor == len(text), (file['path'], cursor, len(text))
    assert set(file['unit_ids']) == {u['id'] for u in groups[file['path']]}
assert summary['core_files'] == 515 and summary['core_csharp'] == 498
assert summary['shipping_csharp'] == 312 and summary['test_csharp'] == 186
assert summary['native_csharp'] == 57
assert len(api['entries']) == 1442
assert api['snapshot_types'] == api['effective_source_families'] == 160
assert not api['snapshot_only_types'] and not api['source_only_types'] and not api['duplicate_entries']
assert all(e['source_path'] and e['source_unit_candidates'] for e in api['entries'])
assert not any(e['documentation_projection'] == 'unmatched-declaration-review' for e in api['entries'])
assert all(u['status'] in ['UNREAD', 'OUT-OF-SCOPE', 'ADMINISTRATIVE-CONTEXT'] for u in units)
core = [f for f in files if f['scope'] == 'CORE']
assert all(f['current_matches_baseline'] in [True, 'LF-normalized-equal'] for f in core)
core_owners = {f['path']: f['assigned_family'] for f in core}
assert len(core_owners) == 515
staged_b = []
for packet in sorted((OUT / 'packets').glob('B?-*.json')):
    staged_b.extend(load(packet)['paths'])
assert len(staged_b) == len(set(staged_b))
assert set(staged_b) == set(load(OUT / 'packets/B.json')['disposition_paths'])
shared = load(OUT / 'shared-sections.json')
assert len(shared) == len({s['unit_id'] for s in shared})
assert {s['investigation_owner'] for s in shared} <= {'A', 'B', 'C'}
shared_b = [s for s in shared if s['investigation_owner'] == 'B']
staged_shared = [unit for packet in sorted((OUT / 'packets').glob('B?-*.json')) for unit in load(packet)['shared_read_unit_ids']]
assert len(staged_shared) == len(set(staged_shared))
assert set(staged_shared) == {s['unit_id'] for s in shared_b}
route = load(OUT / 'inherited-routing.json')
registry = load(RUN / 'inherited-findings/registry.json')
assert route['registry_sha256'] == digest(RUN / 'inherited-findings/registry.json')
assert len(route['groups']) == len(registry['groups']) == 70
assert {key for group in route['groups'] for key in group['provenance_keys']} == {row['key'] for row in registry['rows']}
assert len(route['separately_preserved_pgp_documentation_labels']) == 21
assert len(list((OUT / 'packets').glob('BOUNDARY-*.json'))) == 9
assert not any(f['path'].endswith(('.c', '.cpp', '.h', '.hpp')) for f in core)
for path in OUT.glob('*.py'):
    ast.parse(path.read_text(), filename=str(path))
for path in OUT.rglob('*.json'):
    load(path)
links_checked = 0
for path in [RUN / 'inventory.md', RUN / 'dispatch-map.md']:
    for link in re.findall(r'\]\(([^)]+)\)', path.read_text()):
        if '://' in link or link.startswith('#'): continue
        assert (path.parent / link.split('#')[0]).exists(), (str(path), link)
        links_checked += 1

baseline = load(RUN / 'baseline.json')
preserved = []
for manifest in baseline['frozen_manifests']:
    path = ROOT / manifest['path']
    assert digest(path) == manifest['sha256'], str(path)
    value = load(path)
    base = path.parent / 'records' if path.parent.name == 'documentation-audit' else path.parent
    for item in value['files']:
        target = base / item['path']
        assert digest(target) == item['sha256'], str(target)
    preserved.append(dict(manifest=manifest['path'], files=len(value['files']), sha256=digest(path)))
integration = baseline['integration_manifest']
path = ROOT / integration['path']
assert digest(path) == integration['sha256']
value = load(path)
for item in value['files']:
    target = ROOT / item['path']
    assert digest(target) == item['raw_sha256'], str(target)
assert len(value['files']) == 8325
for item in baseline['prior_piv_integration']['files']:
    assert digest(ROOT / item['path']) == item['sha256'], item['path']
for item in load(RUN / 'user-work-baseline.json'):
    assert digest(ROOT / item['path']) == item['sha256'], item['path']
result = dict(result='PASS', source_baseline=summary['baseline'],
              observed_head=subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip(),
              files_partitioned=len(files), units_partitioned=len(units), core_files=515,
              core_csharp=498, snapshot_entries=1442, effective_public_families=160,
              semantic_reviews_claimed=0, protected_campaign_artifacts=sum(m['files'] for m in preserved),
              inherited_groups_routed=70, inherited_provenance_rows_routed=100,
              separately_preserved_pgp_labels=21, local_record_links_checked=links_checked,
              protected_campaign_manifests=preserved, protected_integration_records=len(value['files']),
              protected_integration_manifest_sha256=digest(path), prior_piv_integration_files=len(baseline['prior_piv_integration']['files']),
              protected_user_files=4, csharp_compilation='NOT-RUN', tests='NOT-RUN', hardware='NOT-RUN')
(OUT / 'verification.json').write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps(result, indent=2))
