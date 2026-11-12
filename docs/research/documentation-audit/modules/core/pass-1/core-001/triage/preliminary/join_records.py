"""One-run ledger join. Reads records and hashes; writes only this directory."""
import csv
import hashlib
import json
import re
from collections import defaultdict
from pathlib import Path

OUT = Path(__file__).resolve().parent
RUN = OUT.parents[1]
ROOT = RUN.parents[6]
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
inputs = {}


def read(path):
    raw = (RUN / path).read_bytes()
    inputs[path] = hashlib.sha256(raw).hexdigest()
    return raw.decode('utf-8-sig')


def load(path):
    return json.loads(read(path))


def save(name, value):
    (OUT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n')


files = {x['path']: x for x in load('inventory/files.json')}
units = {x['id']: x for x in load('inventory/semantic-units.json')}
shared = load('inventory/shared-sections.json')
registry = load('inherited-findings/registry.json')
routing = load('inventory/inherited-routing.json')
assert registry['source_baseline'] == PIN
assert routing['registry_sha256'] == inputs['inherited-findings/registry.json']
rows = list(csv.DictReader((OUT / 'proposals.tsv').open(), delimiter='\t'))
candidate_records = {}
family_units = {}
reports = {}
source_registers = {
    'A': 'investigation/A/sources/notes.md',
    'B1': 'investigation/B1/sources/register.md',
    'B2': 'investigation/B2/sources/register.md',
    'B3': 'investigation/B3/sources/register.md',
    'C': 'investigation/C/sources/final-source-register.md',
}
for family in ('A', 'B1', 'B2', 'B3', 'C'):
    report_path = f'investigation-{family}.md'
    report = read(report_path)
    reports[family] = report
    read(source_registers[family])
    if family == 'A':
        candidate_records.update({x['id']: x for x in load('investigation/A/findings.json')})
        result = load('investigation/A/unit-results.json')
        family_units[family] = [dict(x, id=k) for k, x in result['units'].items()]
    else:
        family_units[family] = load(f'investigation/{family}/unit-dispositions.json')
        if family == 'C':
            candidate_records.update({x['id']: x for x in load('investigation/C/findings.json')['candidates']})
        else:
            path = f'investigation/{family}/candidates.md'
            text = read(path)
            headings = list(re.finditer(r'^## (CORE-B\d-C\d{3}) — (.+)$', text, re.M))
            for n, match in enumerate(headings):
                end = headings[n + 1].start() if n + 1 < len(headings) else len(text)
                first = text[:match.start()].count('\n') + 1
                last = text[:end].count('\n')
                section = text[match.start():end]
                candidate_records[match[1]] = {
                    'id': match[1], 'title': match[2],
                    'report_evidence': {'path': path, 'first_line': first, 'last_line': last},
                    'inherited_aliases': re.findall(r'(?:audit-v2-20261022-20261031|piv-001):(?:#[0-9]+|PIVCORE-[0-9]+)', section),
                }

# Associated paths are explicit family returns, not keyword-derived defect identities.
associated = defaultdict(set)
for path, item in load('investigation/A/shared-dispositions.json').items():
    for cid in item['candidate_ids']:
        associated[cid].add(path)
for x in load('investigation/B2/candidate-index.json'):
    associated[x['id']].update(y['path'] for y in x['associated_unchanged_paths'])
for x in load('investigation/B3/candidate-associated-paths.json'):
    associated[x['candidate_id']].add(x['path'])
read('investigation/C/final-review.json')
read('investigation/C/inherited-dispositions.json')
for item in load('investigation/C/unchanged-paths.json'):
    associated[item['candidate_id']].add(item['path'])
# B1's Required unchanged paths paragraphs use short names. Resolve those exact
# names through the approved inventory, then retain explicit shared narrative paths.
b1_required = {
    1: 'SmartCardSharing.cs PcscNativeResources.cs SmartCardConnectionFactory.cs ISmartCardConnection.cs SmartCardSharingTests.cs src/Core/README.md src/Core/CLAUDE.md src/Piv/README.md src/Piv/CLAUDE.md',
    2: 'FindPcscDevices.cs ISCardApi.cs SCardContext.cs SCard.Interop.cs FindPcscDevicesTests.cs PcscReaderNameOwnershipTests.cs src/Core/CLAUDE.md docs/usage/device-discovery.md docs/architecture/connection-ownership-and-contention.md',
    3: 'DesktopSmartCardDeviceListener.cs SCardReaderStates.cs SCardState.cs SCard.Interop.cs DesktopSmartCardDeviceListenerSCardErrorTests.cs src/Core/CLAUDE.md docs/usage/device-discovery.md',
    4: 'DesktopSmartCardDeviceListener.cs PcscContextIsolationTests.cs SCardContext.cs SCardReaderStates.cs src/Core/CLAUDE.md',
    5: 'RegisteredConnections.cs DisposalGate.cs DeviceConnectionRegistryTests.cs HidConnectionSlot.cs YubiKeyDevice.cs docs/architecture/connection-ownership-and-contention.md docs/architecture/raw-access-tiers.md',
    6: 'ISmartCardConnection.cs PcscConnectionNativeState.cs UsbSmartCardConnection.cs PcscConnectionLifetimeTests.cs src/Core/README.md docs/architecture/connection-ownership-and-contention.md docs/architecture/raw-access-tiers.md',
    7: 'DeviceConnectionRegistry.cs RegisteredConnections.cs ApplicationSession.cs ApplicationSessionScpTests.cs ConnectionOwnershipContractTests.cs src/Core/README.md src/Core/CLAUDE.md docs/architecture/connection-ownership-and-contention.md docs/architecture/raw-access-tiers.md',
    8: 'YubiKeyConnectionExtensions.cs SessionTransportTests.cs YkCommandBase.cs src/Cli.Shared/src/Device/DeviceSelector.cs docs/architecture/connection-ownership-and-contention.md',
    9: 'PcscConnectionSlot.cs SmartCardConnectionFactory.cs UsbSmartCardConnection.cs YubiKeyDevice.cs YubiKitLogging.cs SmartCardConnectionFactoryTests.cs PcscConnectionLifetimeTests.cs SessionTransportTests.cs',
    10: 'PcscConnectionNativeState.cs UsbSmartCardConnection.cs PcscConnectionLifetimeTests.cs',
    11: 'src/Core/CLAUDE.md src/Core/tests/CLAUDE.md FakeSmartCardConnection.cs PcscProtocol.cs',
    12: 'DeviceConnectionRegistry.cs DeviceConnectionRegistryTests.cs src/Core/src/Abstractions/IYubiKey.cs docs/architecture/connection-ownership-and-contention.md',
}
basename_followup = []
for n, names in b1_required.items():
    cid = f'CORE-B1-C{n:03d}'
    for name in names.split():
        matches = [p for p in files if p == name] if '/' in name else [p for p in files if Path(p).name == name]
        if len(matches) == 1:
            associated[cid].add(matches[0])
        else:
            basename_followup.append({'candidate': cid, 'family_short_name': name, 'inventory_matches': matches})
read('investigation/B1/ownership-states.md')
read('investigation/B2/ownership-and-qualification.md')
read('investigation/B3/secondary-handoffs.json')
read('investigation/B3/source-checks.json')
read('investigation/B3/sources/package-static-identity.json')
for path in ('plan.md', 'status.md', 'inventory.md', 'dispatch-map.md', 'inherited-findings.md',
             'planning/2026-11-11-core-docs-pass-1.md', 'inherited-findings/sources.md',
             'inherited-findings/boundary-seeds.md'):
    read(path)

assert set(candidate_records) == {x['local_id'] for x in rows}
assert len(rows) == len({x['local_id'] for x in rows})
unit_by_candidate = defaultdict(list)
all_selected = []
for family, selected in family_units.items():
    for x in selected:
        uid = x.get('id', x.get('unit_id'))
        inv = units[uid]
        assert x['path'] == inv['path']
        source_hash = x.get('inventory_source_sha256', x.get('original_source_sha256', x.get('source_sha256')))
        range_hash = x.get('original_range_sha256', x.get('range_sha256'))
        assert source_hash == inv['source_sha256'], (family, uid, 'source hash')
        assert range_hash == inv['range_sha256'], (family, uid, 'range hash')
        if 'current_source_sha256' in x:
            assert x['current_source_sha256'] == source_hash
        if 'current_range_sha256' in x:
            assert x['current_range_sha256'] == range_hash
        all_selected.append(uid)
        # A/C associated context is retained separately from the directly flagged unit.
        for cid in set(x.get('candidate_ids', []) + x.get('associated_candidate_ids', [])):
            assert cid in candidate_records, (family, uid, cid)
            unit_by_candidate[cid].append({
                'id': uid, 'path': x['path'], 'first_line': x['first_line'], 'last_line': x['last_line'],
                'source_sha256': source_hash, 'range_sha256': range_hash,
                'family_status': x.get('status', x.get('disposition')),
                'association': 'direct' if cid in x.get('candidate_ids', []) else 'related-context',
            })

assert len(all_selected) == len(set(all_selected)), 'primary/shared investigation ownership overlap'
raw_hash_checks = {}
missing_paths = set()


def surface(path, cid):
    inv = files.get(path)
    if inv is None:
        missing_paths.add(path)
        return {'path': path, 'owner': 'OWNER-FOLLOWUP: absent from inventory', 'scope': 'family-reported dependency'}
    if path not in raw_hash_checks:
        actual = hashlib.sha256((ROOT / path).read_bytes()).hexdigest()
        raw_hash_checks[path] = {'inventory_sha256': inv['sha256'], 'current_sha256': actual, 'equal': actual == inv['sha256']}
        assert actual == inv['sha256'], ('source drift', path)
    ranges = [dict(x) for x in shared if x['path'] == path]
    flagged = [x['id'] for x in unit_by_candidate[cid] if x['path'] == path]
    return {
        'path': path, 'git_blob_at_source_pin': inv['git_blob'],
        'old_source_sha256': inv['sha256'], 'current_source_sha256': raw_hash_checks[path]['current_sha256'],
        'primary_inventory_owner': inv['assigned_family'], 'stage': inv['stage'],
        'suggested_editor': inv['future_editor_owner'],
        'associated_unit_ids': flagged,
        'shared_range_inventory': [{k: x[k] for k in ('unit_id', 'first_line', 'last_line', 'investigation_owner', 'investigation_stage', 'future_editor_owner')} for x in ranges],
        'scope': 'Exact candidate units above; other listed shared ranges are ownership coordinates only, not additional read/edit acceptance.',
        'future_disposition': 'Fresh editor/reviewer explicitly retain, qualify or edit after Astra adjudication.',
    }


proposals = []
local_to_pt = {}
for row in rows:
    cid = row['local_id']
    if cid == 'CORE-B3-C006':
        continue
    members = [cid, 'CORE-B3-C006'] if cid == 'CORE-B1-C010' else [cid]
    pt = f'PT-{len(proposals) + 1:03d}'
    proposal = {
        'proposal_id': pt, 'local_ids': members, 'accepted_id': None,
        'facets': row['facets'].split(','), 'topology': row['topology'],
        'proposal_and_rationale': row['proposal_and_rationale'],
        'implementation_reopen_trigger_or_needed_probe': row['reopen_trigger_or_needed_probe'],
        'dependencies': row['dependencies'], 'local_records': [],
        'state': 'PROVISIONAL; Astra adjudication after remaining families and boundaries',
    }
    if len(members) > 1:
        proposal['join_rationale'] = 'B3 candidates.md C006 explicitly extends B1 C010 nonadditively; one policy question, separate route observations retained.'
    for member in members:
        local_to_pt[member] = pt
        family = member.split('-')[1]
        original = candidate_records[member]
        evidence = unit_by_candidate[member]
        paths = {x['path'] for x in evidence} | associated[member]
        paths.update(original.get('reopening_paths', original.get('reopen', [])))
        for cite in original.get('citations', original.get('locations', [])):
            paths.add(cite['path'])
        assert evidence, ('candidate missing unit evidence', member)
        proposal['local_records'].append({
            'local_id': member, 'original_candidate': original,
            'unit_ledger': f'investigation/{family}/' + ('unit-results.json' if family == 'A' else 'unit-dispositions.json'),
            'source_report': f'investigation-{family}.md',
            'qualified_source_register': source_registers[family],
            'source_authority_status': 'Family-reported original-passage checks only; no independent normative verification in this triage.',
            'exact_evidence_units': evidence,
            'documentation_surfaces_and_unchanged_dependencies': [surface(p, member) for p in sorted(paths)],
            'documentation': 'Proposal only; product unchanged', 'implementation': 'Unchanged; concerns retain original observed/desired distinction',
            'review': 'Fresh full-unit/citation review pending', 'qualification': 'No new compiler, runtime, native or device evidence',
        })
    proposals.append(proposal)

# All inherited rows stay in their authoritative groups. A relation never converts an
# inherited question into a local defect, nor closes an unrelated facet of an old group.
explicit_relations = {
    'discovery-abandonment': ['CORE-A-C008', 'CORE-A-C012', 'CORE-B1-C007', 'CORE-B2-C002', 'CORE-B3-C008'],
    'native-stall-evidence': ['CORE-B1-C004', 'CORE-B2-C005', 'CORE-B3-C005'],
    'pcsc-reader-name-cleanup': ['CORE-B1-C002', 'CORE-B1-C004'],
    'metadata-field-length': ['CORE-A-C001', 'CORE-A-C002'],
    'metadata-pagination': ['CORE-A-C008'],
    'metadata-firmware-read': ['CORE-A-C015'],
    'scp-parameter-ownership': ['CORE-C-C020'],
    'apdu-tlv-ownership': ['CORE-C-C005', 'CORE-C-C006', 'CORE-C-C013'],
    'migration-yaml': ['CORE-C-C012'],
    'toolchain-host-selection': ['CORE-A-C018', 'CORE-A-C019'],
    'base32-failure-cleanup': ['CORE-C-C017'], 'base32-shape': ['CORE-C-C017'],
}
inherited = []
for old in registry['rows']:
    alias_members = [cid for cid, original in candidate_records.items() if old['key'] in original.get('inherited_aliases', [])]
    related = explicit_relations.get(old['dedup_group'], [])
    route = next(x for x in routing['groups'] if x['group'] == old['dedup_group'])
    inherited.append({
        'key': old['key'], 'original_identity_kind': old['original_identity_kind'],
        'group': old['dedup_group'], 'registry_row_index': registry['rows'].index(old),
        'authority': old['authority'], 'historical_definition': old['historical_definition'],
        'state_preserved': old['current_state'], 'conclusion_preserved': old['current_conclusion'],
        'qualification_preserved': old['qualification'], 'current_path_evidence': old['current_path_evidence'],
        'documentation_preserved': old['documentation'],
        'primary_packet': route['primary_packet'], 'mandatory_boundary_packets': route['mandatory_boundary_packets'],
        'local_alias_associations': alias_members,
        'related_proposal_ids': sorted({local_to_pt[cid] for cid in alias_members + related}),
        'relation_limit': 'Provenance/dependency association only; all original facets and status remain authoritative in registry.json. Unmatched rows remain queued to their routed owner.',
    })
for proposal in proposals:
    proposal['inherited_report_associations'] = [x['key'] for x in inherited if set(proposal['local_ids']) & set(x['local_alias_associations'])]
    proposal['inherited_related_keys'] = [x['key'] for x in inherited if proposal['proposal_id'] in x['related_proposal_ids'] and x['key'] not in proposal['inherited_report_associations']]

# Normalize repeated evidence and shared topology once. Each logical candidate still
# retains its exact association/status; reference tables avoid duplicating whole guides.
evidence_table = {}
surface_table = {}
for proposal in proposals:
    for local in proposal['local_records']:
        refs = []
        for item in local.pop('exact_evidence_units'):
            uid = item['id']
            evidence_table[uid] = {k: v for k, v in item.items() if k != 'association'}
            refs.append({'unit_id': uid, 'association': item['association']})
        local['exact_evidence_unit_refs'] = refs
        refs = []
        for item in local.pop('documentation_surfaces_and_unchanged_dependencies'):
            path = item['path']
            candidate_units = item.pop('associated_unit_ids', [])
            surface_table[path] = item
            refs.append({'path': path, 'associated_unit_ids': candidate_units})
        local['documentation_surface_refs'] = refs
save('map.json', {
    'source_baseline': PIN, 'metadata_checkpoint_supplied': 'bb94f088',
    'local_ids_changed': False, 'canonical_ids_allocated': 0,
    'facets': {'D': 'documentation-ready proposal, pending fresh editor/citation review',
               'E': 'bounded experiment before stronger wording', 'H': 'hold affected claims for remaining family/boundary',
               'P': 'Pass-2 engineering only; no runtime/assertion edit authorization',
               'M': 'maintainer decision', 'Q': 'qualification/source/provenance gap'},
    'facet_rule': 'Independent flags; D can coexist with held/experimental/decision/engineering facets in the same compound local group.',
    'proposals': proposals, 'evidence_units': evidence_table, 'document_surfaces': surface_table,
})
save('inherited-links.json', {'registry_sha256': inputs['inherited-findings/registry.json'],
    'source_baseline': PIN, 'rows': inherited, 'groups_preserved': registry['groups'],
    'separate_pgp_labels': routing['separately_preserved_pgp_documentation_labels'],
    'rule': '100 rows / 70 existing groups; separate from 68 local candidates / 67 PT groups. No additive Core bug total.'})

# Compact human view: full exact surfaces/hashes are in map.json, not repeated in each cell.
lines = ['# Provisional proposal rows', '',
         'PT IDs are internal ledger joins. D/E/H/P/M/Q are independent facets defined in `../../triage-preliminary.md`.',
         'Each local ID links its completed family definition. `map.json` retains exact source/range hashes, document paths, inventory editor ownership and shared-range coordinates.', '',
         'Inherited associations preserve exact old scope; an association alone is neither a defect alias nor a resolution. The complete 100-row backstop is `inherited-links.json`.', '',
         '| Proposal → local IDs | Inherited report associations; related dependencies | Facets / topology | Suggested correction or retained question | Reopen / decision / probe | Remaining dependencies |',
         '| --- | --- | --- | --- | --- | --- | --- |']
for proposal in proposals:
    ids = []
    for cid in proposal['local_ids']:
        family = cid.split('-')[1]
        original = candidate_records[cid]
        if family in ('A', 'C'):
            link = f'../../investigation-{family}.md'
        else:
            link = f'../../investigation/{family}/candidates.md'
        ids.append(f'[{cid}]({link})')
    old = ', '.join('`' + k + '`' for k in proposal['inherited_report_associations']) or '—'
    if proposal['inherited_related_keys']:
        old += '; related: ' + ', '.join('`' + k + '`' for k in proposal['inherited_related_keys'])
    cols = [proposal['proposal_id'] + ' → ' + ', '.join(ids), old,
            ','.join(proposal['facets']) + ' / ' + proposal['topology'],
            proposal['proposal_and_rationale'], proposal['implementation_reopen_trigger_or_needed_probe'], proposal['dependencies']]
    lines.append('| ' + ' | '.join(x.replace('|', '\\|') for x in cols) + ' |')
(OUT / 'rows.md').write_text('\n'.join(lines) + '\n')

counts = {family: len([x for x in rows if x['local_id'].split('-')[1] == family]) for family in family_units}
assert counts == {'A': 21, 'B1': 12, 'B2': 6, 'B3': 9, 'C': 20}
assert len(proposals) == 67 and len(local_to_pt) == 68
assert len(inherited) == len({x['key'] for x in inherited}) == 100
assert len(registry['groups']) == 70
assert all(cid in candidate_records for x in inherited for cid in x['local_alias_associations'])
registry_keys = {x['key'] for x in registry['rows']}
assert all(key in registry_keys for original in candidate_records.values() for key in original.get('inherited_aliases', []))
assert not missing_paths and not basename_followup
assert all(proposal['accepted_id'] is None for proposal in proposals)
assert set(evidence_table) == {x['id'] for selected in unit_by_candidate.values() for x in selected}
save('verification.json', {
    'status': 'PASS ledger joins and product-byte hash checks; semantic/content acceptance remains pending',
    'counts_derived': counts, 'local_candidates': len(rows), 'provisional_groups': len(proposals),
    'explicit_cross_family_joins': [['CORE-B1-C010', 'CORE-B3-C006']],
    'completed_family_unit_counts': {f: len(us) for f, us in family_units.items()},
    'disjoint_completed_unit_total': len(all_selected),
    'inherited_rows': len(inherited), 'inherited_groups': len(registry['groups']),
    'separate_pgp_labels': len(routing['separately_preserved_pgp_documentation_labels']),
    'missing_inventory_paths': sorted(missing_paths),
    'family_short_name_followup': basename_followup,
    'normalized_candidate_and_context_units': len(evidence_table),
    'logical_checks': ['Every original local candidate occurs in exactly one proposal.',
                       'Every reported inherited association resolves to its exact registry key.',
                       'Every inherited row retains its state, qualification and source-path evidence.',
                       'All 70 authoritative inherited groups and 21 separate PGP labels remain available.',
                       'Every proposed surface resolves uniquely to the approved inventory.',
                       'All accepted IDs remain null; local IDs remain unchanged.'],
    'preparation_observation': 'An initial read-only schema-orientation command treated A units as a list and exited with KeyError: 0. The dictionary schema was inspected and the corrected record join ran successfully; no source or output artifact was changed by that failed orientation.',
    'product_path_hash_checks': raw_hash_checks,
    'input_record_hashes': inputs,
    'source_hash_rule': 'Fresh raw-byte SHA-256 equality to inventory; Git pin equality is inherited from completed family identity checks, not newly inferred from metadata HEAD.',
    'limitations': ['No new source-body investigation or independent normative passage match.',
                    'No B4/B5/B6 return consumed, even if a concurrent worker finishes.',
                    'All nine boundary packets remain unexecuted in this triage scope.',
                    'No builds, tests, formatter, native/device, index/ref, commit/push or subagent action.'],
})
print(json.dumps({'local_candidates': len(rows), 'proposal_groups': len(proposals), 'family_counts': counts,
    'completed_units': len(all_selected), 'inherited_rows': len(inherited), 'inherited_groups': len(registry['groups']),
    'product_paths_hashed': len(raw_hash_checks), 'missing_inventory_paths': sorted(missing_paths)}))
