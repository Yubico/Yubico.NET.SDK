"""Bounded final ledger join. No SDK execution, source investigation or product edit.

Original ranges are verified against pinned Git blobs, with the recorded checkout
newline/BOM representation reconstructed by hash. Current documentation may differ.
Only this directory and the assigned draft handoff are writable outputs.
"""
import hashlib
import json
import re
import subprocess
from collections import Counter, defaultdict
from pathlib import Path

OUT = Path(__file__).resolve().parent
RUN = OUT.parents[1]
ROOT = RUN.parents[6]
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
MODULES = ('Management', 'Ctap2', 'SecurityDomain', 'Oath', 'WebAuthn', 'YubiOtp', 'YubiHsm', 'OpenPgp', 'Piv')
FAMILIES = ('A', 'B1', 'B2', 'B3', 'B4', 'B5', 'B6', 'C')
inputs = {}


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def read(path):
    p = Path(path)
    p = p if p.is_absolute() else RUN / p
    raw = p.read_bytes()
    key = str(p.relative_to(RUN)) if p.is_relative_to(RUN) else str(p)
    inputs[key] = sha(raw)
    return raw.decode('utf-8-sig')


def load(path):
    return json.loads(read(path))


def save(name, value):
    (OUT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n')


old_paths = [RUN / 'triage-preliminary.md', RUN / 'triage-full-family.md']
for folder in ('preliminary', 'full-family'):
    old_paths += sorted(p for p in (RUN / 'triage' / folder).iterdir() if p.is_file())
old_hashes = {str(p.relative_to(RUN)): sha(p.read_bytes()) for p in old_paths}
preservation_file = OUT / 'preserved-inputs.json'
if preservation_file.exists():
    assert json.loads(preservation_file.read_text()) == old_hashes, 'prior triage bytes changed'
else:
    save('preserved-inputs.json', old_hashes)
full = load('triage/full-family/map.json')
full_relations = load('triage/full-family/relationships.json')
inventory = {x['path']: x for x in load('inventory/files.json')}
units = {x['id']: x for x in load('inventory/semantic-units.json')}
shared = load('inventory/shared-sections.json')
registry = load('inherited-findings/registry.json')
assert registry['source_baseline'] == PIN
candidates = {}
candidate_refs = defaultdict(set)
candidate_paths = defaultdict(set)
all_units = {}
populations = {}


def section(text, marker):
    """Retain complete existing candidate section, with exact record coordinates."""
    matches = list(re.finditer(r'^#{2,3} (.+)$', text, re.M))
    found = next((i for i, x in enumerate(matches) if x[1].startswith(marker + ' ') or x[1] == marker), None)
    assert found is not None, marker
    start = matches[found].start()
    end = matches[found + 1].start() if found + 1 < len(matches) else len(text)
    return {'first_line': text[:start].count('\n') + 1, 'last_line': text[:end].count('\n'),
            'text': text[start:end], 'heading': matches[found][1]}


def add_units(owner, rows):
    counts = Counter()
    for row in rows:
        uid = row.get('id', row.get('unit_id'))
        assert uid not in all_units, ('overlap', owner, uid)
        inv = units[uid]
        assert row['path'] == inv['path']
        source = row.get('inventory_source_sha256', row.get('original_source_sha256', row.get('source_sha256')))
        ran = row.get('original_range_sha256', row.get('range_sha256'))
        assert source == inv['source_sha256'] and ran == inv['range_sha256'], (owner, uid)
        state = row.get('status', row.get('disposition'))
        assert state and 'UNREAD' not in state and 'PENDING-CONTEXT' not in state
        all_units[uid] = {'owner': owner, 'original_disposition': state, 'inventory': inv}
        counts[state] += 1
        refs = set()
        for key in ('candidate_ids', 'associated_candidate_ids', 'local_candidates', 'candidates', 'related_candidate_ids'):
            refs.update(x for x in row.get(key, []) if isinstance(x, str) and x.startswith('CORE-'))
        for cid in refs:
            candidate_refs[cid].add(uid)
            candidate_paths[cid].add(inv['path'])
    populations[owner] = {'unit_count': len(rows), 'dispositions': dict(counts)}


for family in FAMILIES:
    report = read(f'investigation-{family}.md')
    data = load(f'investigation/{family}/' + ('unit-results.json' if family == 'A' else 'unit-dispositions.json'))
    rows = [dict(x, id=k) for k, x in data['units'].items()] if family == 'A' else data
    add_units(family, rows)
for group in full['groups']:
    for old in group['local_records']:
        cid = old['local_id']
        family = cid.split('-')[1]
        path = f'investigation-{family}.md' if family == 'A' else f'investigation/{family}/' + ('findings.json' if family == 'C' else 'candidates.md')
        text = read(path)
        marker = cid if family not in ('B6',) else cid.rsplit('-', 1)[1]
        candidate_section = section(text, marker) if family != 'C' else None
        if family == 'C':
            original = old['original_definition']
            candidate_section = {'path': 'investigation/C/findings.json', 'json_id': cid,
                                 'text': json.dumps(original, ensure_ascii=False, indent=2), 'heading': original['title']}
        else:
            candidate_section['path'] = path
        candidates[cid] = {'original_id': cid, 'run_qualified_id': f'core-001:{cid}', 'owner': family,
            'source_definition': candidate_section, 'original_index': old['original_definition'],
            'full_family_reference': group['proposal_id'], 'reported_flags': group['facets'],
            'inherited_association_keys': group['inherited_association_keys'],
            'proposed_documentation': group['proposal_and_rationale'],
            'reopening_condition': group['implementation_reopen_trigger_or_needed_probe'],
            'latest_authority': old['latest_authority_addenda'], 'qualified_source_register': old['qualified_source_register'],
            'grouping_limit': 'Original compound source concern, not an accepted bug or a blanket edited/closed facet.'}
        candidate_paths[cid].update(old['documentation_surface_refs'])

boundary_inherited = {}
for module in MODULES:
    report = read(f'boundary-{module}.md')
    directory = f'boundaries/{module}'
    index_path = directory + ('/candidates.json' if module == 'Management' else '/candidate-index.json')
    index = load(index_path)
    assert isinstance(index, list)
    prose = read(directory + '/candidates.md') if module != 'Management' else None
    read(directory + ('/sources.md'))
    rows = load(directory + '/unit-dispositions.json')
    add_units('BOUNDARY-' + module, rows)
    boundary_inherited[module] = load(directory + '/inherited-dispositions.json')
    for item in index:
        cid = item['id']
        assert cid not in candidates
        marker = cid.rsplit('-', 1)[1] if module in ('Ctap2', 'WebAuthn') else cid
        if prose is None:
            definition = {'path': index_path, 'json_id': cid, 'text': json.dumps(item, ensure_ascii=False, indent=2), 'heading': item['title']}
        else:
            definition = dict(section(prose, marker), path=directory + '/candidates.md')
        paths = set(item.get('paths', []))
        for key in ('unchanged_paths', 'candidate_associated_unchanged_paths'):
            paths.update(x['path'] if isinstance(x, dict) else x for x in item.get(key, []))
        for key in ('anchors', 'exact_anchors'):
            paths.update(x['path'] for x in item.get(key, []))
        for key in ('unit_ids', 'assigned_unit_ids', 'exact_unit_ids', 'related_unit_ids'):
            for uid in item.get(key, []):
                assert uid in units, (cid, uid)
                candidate_refs[cid].add(uid)
                paths.add(units[uid]['path'])
        candidate_paths[cid].update(paths)
        candidates[cid] = {'original_id': cid, 'run_qualified_id': f'core-001:{cid}', 'owner': 'BOUNDARY-' + module,
            'source_definition': definition, 'original_index': item, 'full_family_reference': None,
            'reported_flags': item.get('facets', {'meaning': 'Use candidate section; local D/M/P/Q abbreviations differ from full-family flags.'}),
            'proposed_documentation': item.get('documentation', item.get('action', 'Use exact Documentation/D paragraph in source_definition.')),
            'reopening_condition': item.get('trigger', item.get('reopening_map', 'Use exact Reopen/Reopen when/Qualification-reopen paragraph in source_definition.')),
            'latest_authority': [], 'qualified_source_register': directory + '/sources.md',
            'grouping_limit': 'Boundary caller facets add applicability; shared Core mechanisms and old applet questions remain nonadditive.'}

registry_keys = {x['key'] for x in registry['rows']}
for cid, record in candidates.items():
    keys = set(record.get('inherited_association_keys', []))
    definition_text = record['source_definition']['text']
    keys.update(key for key in registry_keys if key in definition_text)
    for data in boundary_inherited.values():
        rows = data.get('rows', data.get('assigned', [])) if isinstance(data, dict) else data
        for row in rows:
            key = row.get('key', row.get('original', {}).get('key'))
            if key in registry_keys and cid in json.dumps(row):
                keys.add(key)
    assert keys <= registry_keys
    record['inherited_association_keys'] = sorted(keys)
    record['inherited_relation_limit'] = 'Exact retained provenance/referral association only; no automatic same-defect merge or old-scope closure.'

expected_boundary = {'Management': 331, 'Ctap2': 590, 'SecurityDomain': 193, 'Oath': 239, 'WebAuthn': 265,
                     'YubiOtp': 324, 'YubiHsm': 160, 'OpenPgp': 328, 'Piv': 513}
assert all(populations['BOUNDARY-' + m]['unit_count'] == n for m, n in expected_boundary.items())
core_count = sum(not x['owner'].startswith('BOUNDARY-') for x in candidates.values())
assert core_count == 103 and len(candidates) == 185
assert sum(expected_boundary.values()) == 2943 and len(all_units) == 14044
assert set(candidate_refs) <= set(candidates), set(candidate_refs) - set(candidates)

# Additive follow-ups replace only old source-return-pending wording.
followups = []
for module in ('Management', 'Ctap2'):
    paths = sorted((RUN / 'boundaries' / module).glob('followup-*.md'))
    paths.append(RUN / 'boundaries' / module / 'boundary-completion.md')
    for path in paths:
        text = read(path)
        followups.append({'path': str(path.relative_to(RUN)), 'text': text})
for path in ('boundaries/WebAuthn/ctap2-handoff.md', 'boundaries/YubiHsm/corrections.md',
             'boundaries/Piv/inherited-outcomes.md', 'boundaries/Oath/analysis.md',
             'investigation/B1/triage-clarifications.md', 'investigation/C/triage-clarifications.md'):
    followups.append({'path': path, 'text': read(path)})

# Read only completed editor returns and reviews; snapshot their returned records,
# never product bodies or an unfinished editor's files.
WORK = Path('/home/rupicola/Code/y')
batches = {
    'A': {'root': ROOT, 'report': 'edit-A.md', 'dir': 'editing/A/revision-2', 'decisions': 'path-dispositions.json',
          'review': 'editing/A/revision-2/review.md', 'state': 'CONTENT-PASS-REVISION-2; transferred to primary; final campaign gates pending'},
    'C': {'root': ROOT, 'report': 'edit-C.md', 'dir': 'editing/C', 'decisions': 'path-decisions.json',
          'review': 'editing/C/revision-2/review.md', 'state': 'CONTENT-PASS-REVISION-2; transferred to primary; C019 held and C020 referred'},
    'transports': {'root': WORK / 'yubikit-core-docs-edit-transports', 'report': 'edit-transports.md',
          'dir': 'editing/transports', 'decisions': 'file-dispositions.json', 'review': 'editing/transports/review.md',
          'state': 'REVIEW-REQUIRES-M1-M2-M3; successor acceptance not supplied'},
    'protocols': {'root': WORK / 'yubikit-core-docs-edit-protocols', 'report': 'edit-protocols.md',
          'dir': 'editing/protocols/revision-2', 'decisions': 'path-decisions.json', 'review': 'editing/protocols/review.md',
          'state': 'EDITOR-REVISION-2-RETURNED; R1-R4 corrections awaiting focused independent re-review'},
    'FIDO-boundaries': {'root': WORK / 'yubikit-core-docs-edit-fido-boundaries', 'report': 'edit-FIDO-boundaries.md',
          'dir': 'editing/FIDO-boundaries', 'decisions': 'path-decisions.json', 'review': 'editing/FIDO-boundaries/review.md',
          'state': 'REVIEW-REQUIRES-R1-R2; no batch PASS'},
    'SHARED': {'root': WORK / 'yubikit-core-docs-edit-guides', 'report': 'edit-SHARED.md',
          'dir': 'editing/SHARED', 'decisions': 'path-dispositions.json', 'review': None,
          'state': 'COMPLETED-EDITOR-RETURN; independent review pending'},
    'applet-boundaries': {'root': WORK / 'yubikit-core-docs-edit-applet-boundaries', 'report': 'edit-applet-boundaries.md',
          'dir': 'editing/applet-boundaries', 'decisions': 'path-decisions.json', 'review': None,
          'state': 'COMPLETED-EDITOR-RETURN; independent review pending'},
    'PGP-boundary': {'root': WORK / 'yubikit-core-docs-edit-pgp-piv', 'report': 'edit-PGP-boundary.md',
          'dir': 'editing/PGP-boundary', 'decisions': 'path-decisions.json', 'review': None,
          'state': 'COMPLETED-OPENPGP-EDITOR-RETURN; independent review pending; no PIV edit credit'},
}
editor_records = {}
editor_path_rows = {}
editor_candidates = defaultdict(list)
for batch, config in batches.items():
    run = config['root'] / RUN.relative_to(ROOT)
    report = read(run / config['report'])
    raw = load(run / config['dir'] / config['decisions'])
    rows = raw['files'] if isinstance(raw, dict) else raw
    review = read(run / config['review']) if config['review'] else None
    editor_records[batch] = {'worktree': str(config['root']), 'state': config['state'],
        'report_path': str(run / config['report']), 'report_text': report,
        'decision_record': str(run / config['dir'] / config['decisions']),
        'review_path': str(run / config['review']) if config['review'] else None, 'review_text': review,
        'returned_path_count': len(rows), 'scope': 'Completed return snapshot; later mutable bytes receive no implicit acceptance.'}
    for n, row in enumerate(rows):
        path = row['path']
        key = f'{batch}:{n}:{path}'
        editor_path_rows[key] = {'batch': batch, 'path': path, 'original_return_row': row,
                                'batch_review_state': config['state'], 'row_index': n}
        refs = set()
        for k in ('candidate_ids', 'local_candidate_ids', 'local_ids', 'candidates', 'candidate_ids_from_original_file_ledger'):
            refs.update(x for x in row.get(k, []) if isinstance(x, str))
        refs.update(x['local_id'] for x in row.get('candidate_associations', []))
        refs.update([row['candidate']] if 'candidate' in row else [])
        for cid in refs:
            if cid in candidates:
                editor_candidates[cid].append(key)
                candidate_paths[cid].add(path)
    # C's decision rows predate the sole AES correction. Keep their old hash verbatim;
    # accepted-A-C provides the independent successor hash separately below.

protocol_run = batches['protocols']['root'] / RUN.relative_to(ROOT)
protocol_revision = read(protocol_run / 'editing/protocols/revision-2/report.md')
protocol_citations = read(protocol_run / 'editing/protocols/revision-2/citations.md')
editor_records['protocols']['successor_report_text'] = protocol_revision
editor_records['protocols']['corrected_citation_record'] = protocol_citations
for cid in ('CORE-B6-C005', 'CORE-B6-C006', 'CORE-B6-C009', 'CORE-B6-C010', 'CORE-B6-C011', 'CORE-B6-C013'):
    candidates[cid]['latest_authority'].append(str(protocol_run / 'editing/protocols/review.md'))
    candidates[cid]['latest_authority'].append(str(protocol_run / 'editing/protocols/revision-2/citations.md'))
    candidates[cid]['review_qualification'] = 'Use R1-R4 corrected source locators, selected-security-level conditions, null-firmware admission and 64-byte agreements versus 80-byte KDF. Successor content acceptance pending.'
for path in ('editing/A/review.md', 'editing/C/review.md', 'editing/C/source-review.md',
             'editing/A/revision-2/candidate-dispositions.json', 'editing/C/candidate-map.json',
             'editing/C/revision-2/mapping.json', 'triage/editing-release.md'):
    read(path)

accepted = load('editing/integration/accepted-A-C.json')
accepted_hashes = {x['path']: x['sha256'] for x in accepted['product_files']}
assert len(accepted_hashes) == 91
accepted_primary = {p: {'accepted_sha256': digest, 'current_sha256': sha((ROOT / p).read_bytes())}
                    for p, digest in accepted_hashes.items()}
for row in accepted_primary.values():
    row['matches_accepted_transfer'] = row['accepted_sha256'] == row['current_sha256']
validation = {}
for lane in ('source-tokens-after-a-c', 'api-after-a-c', 'core-build-after-a-c'):
    path = 'validation/commands-v2/' + lane
    validation[lane] = {'receipt': load(path + '/receipt.json'), 'stdout': read(path + '/stdout.log'),
                        'scope': 'Existing coordinator run after A/C transfer, not a new execution or final all-batch gate.'}


def editor_for(path):
    """Actual release ownership, retaining unsupported/configuration handoffs."""
    if path.startswith('src/Core/') and path.endswith('.cs'):
        stage = inventory[path]['stage']
        family = inventory[path]['assigned_family']
        return family if family in ('A', 'C') else ('protocols' if stage.startswith(('B5', 'B6')) else 'transports')
    if path.startswith(('src/Ctap2/', 'src/WebAuthn/')):
        return 'FIDO-boundaries' if path.endswith(('.md', '.cs')) else 'coordinator/configuration-owner; no executable edit release'
    if any(path.startswith('src/' + m + '/') for m in ('Management', 'SecurityDomain', 'Oath', 'YubiOtp', 'YubiHsm')):
        return 'applet-boundaries' if path.endswith(('.md', '.cs')) else 'coordinator/configuration-owner; no executable edit release'
    if path.startswith('src/OpenPgp/'):
        return 'PGP-boundary' if path.endswith(('.md', '.cs')) else 'coordinator/configuration-owner'
    if path.startswith('src/Piv/'):
        return 'PIV/PGP-PIV editor active per coordinator steering; completed PIV edit return not consumed'
    if path.startswith('docs/research/') or path.startswith(('docs/archive/', 'docs/completed/')):
        return 'historical/current evidence owner; preserve archives, dated successor only if assigned'
    if path.endswith('.md') and (path.startswith('src/Core/') or '/' not in path or path.startswith(('docs/usage/', 'docs/architecture/')) or path.count('/') == 1 and path.startswith('docs/')):
        return 'SHARED'
    if path.endswith(('.md', '.cs')):
        return 'external; assigned, no completed return consumed'
    return 'coordinator/unreleased configuration or asset owner; explicit disposition needed'


# Verify exact original bytes using Git object reads. No current-product-diff invariant.
source_paths = sorted({x['inventory']['path'] for x in all_units.values()})
proc = subprocess.run(['git', 'cat-file', '--batch'], input=''.join(PIN + ':' + p + '\n' for p in source_paths).encode(),
                      stdout=subprocess.PIPE, stderr=subprocess.PIPE, cwd=ROOT, check=True)
offset = 0
pinned = {}
metadata_gaps = []
for path in source_paths:
    end = proc.stdout.index(b'\n', offset)
    header = proc.stdout[offset:end].decode()
    offset = end + 1
    if header.endswith(' missing'):
        assert inventory[path]['git_blob'] is None, ('missing pinned product', path)
        metadata_gaps.append({'path': path, 'status': 'INVENTORY-METADATA-OUTSIDE-SOURCE-PIN',
                              'original_sha256': inventory[path]['sha256'],
                              'limit': 'Current campaign handoff added after source pin; original inventory/range identities retained, no fictitious Git byte proof.'})
        continue
    blob, typ, size = header.split()
    assert typ == 'blob' and blob == inventory[path]['git_blob'], ('pin blob', path)
    raw = proc.stdout[offset:offset + int(size)]
    offset += int(size) + 1
    lf = raw.replace(b'\r\n', b'\n')
    body = lf.removeprefix(b'\xef\xbb\xbf')
    variants = [raw] + [bom + data for bom in (b'', b'\xef\xbb\xbf') for data in (body, body.replace(b'\n', b'\r\n'))]
    # A recorded checkout can mix CRLF body lines with one final LF. A matching
    # current byte representation is usable only with both the original raw hash
    # and normalized equality to the pinned blob; comments changed later fail it.
    if body.endswith(b'\n'):
        variants += [bom + body[:-1].replace(b'\n', b'\r\n') + b'\n' for bom in (b'', b'\xef\xbb\xbf')]
    current = (ROOT / path).read_bytes()
    if current.removeprefix(b'\xef\xbb\xbf').replace(b'\r\n', b'\n') == body:
        variants.append(current)
    original = next((x for x in variants if sha(x) == inventory[path]['sha256']), None)
    assert original is not None, ('inventory byte representation', path)
    pinned[path] = {'blob': blob, 'git_sha256': sha(raw), 'inventory_sha256': sha(original),
                    'reconstructed_checkout_representation': original != raw,
                    'text': original.decode('utf-8-sig', errors='replace')}
for uid, item in all_units.items():
    inv = item['inventory']
    if inv['path'] in pinned:
        text = pinned[inv['path']]['text'][inv['start_character']:inv['end_character_exclusive']]
        assert sha(text.encode()) == inv['range_sha256'], ('pinned range', uid)

path_records = {}
for path in sorted({p for values in candidate_paths.values() for p in values}):
    assert path in inventory, ('unknown candidate path', path)
    inv = inventory[path]
    path_records[path] = {'original_source_sha256': inv['sha256'], 'git_blob_at_pin': inv['git_blob'],
        'current_primary_raw_sha256': sha((ROOT / path).read_bytes()),
        'original_pin_checked': path in pinned,
        'editor_assignment': editor_for(path),
        'inventory_primary': inv['assigned_family'], 'inventory_stage': inv['stage'],
        'returned_path_decision_refs': [k for k, row in editor_path_rows.items() if row['path'] == path],
        'shared_range_ownership': [{k: x[k] for k in ('unit_id', 'first_line', 'last_line', 'future_editor_owner', 'investigation_owner', 'investigation_stage')}
                                   for x in shared if x['path'] == path],
        'accepted_transfer': accepted_primary.get(path),
        'limit': 'Current documentation hash is distinct from original evidence; same-path edits for other facets confer no closure.'}


def excerpts(text, kind):
    terms = {'documentation': ['Documentation', 'D'], 'engineering': ['Implementation', 'Runtime', 'M', 'M/P', 'M/P/Q'],
             'decision': ['Decision', 'Policy', 'P', 'M/P', 'M/P/Q'],
             'qualification': ['Qualification', 'Q', 'Q/oracle', 'Profile qualification', 'M/P/Q']}
    chunks = re.split(r'(?=\*\*[A-Za-z][^*\n]{0,80}\*\*)', text)
    matches = [x for x in chunks if any(re.match(r'\*\*' + re.escape(t) + r'(?::|\*\*|/)', x, re.I) for t in terms[kind])]
    return matches or ['See complete exact source_definition; this candidate does not separately spell every dimension. No intended behavior is invented.']


facet_count = 0
for cid, record in candidates.items():
    record['source_unit_refs'] = sorted(candidate_refs[cid])
    assert record['source_unit_refs'], ('no original evidence', cid)
    assert set(record['source_unit_refs']) <= set(all_units), ('unassigned source reference', cid)
    record['documentation_paths'] = sorted(candidate_paths[cid])
    record['completed_editor_candidate_path_refs'] = sorted(set(editor_candidates[cid]))
    record['path_assignment_refs'] = {p: path_records[p]['editor_assignment'] for p in record['documentation_paths']}
    record['independent_review_limit'] = 'Credit only exact path/facet/hash acceptance in completed review; source-packet completion or another facet edit is not acceptance.'
    definition = record['source_definition']['text']
    record['observed_behavior'] = record['original_index'].get('observed', record['original_index'].get('evidence',
        'Actual source observations and caller mechanisms are retained verbatim in source_definition and its original_index.'))
    record['desired_behavior'] = record['original_index'].get('desired', record['original_index'].get('action',
        'Documentation proposal and separately named policy choices only; no new runtime target selected by this reconciliation.'))
    record['facets'] = {}
    for kind in ('documentation', 'engineering', 'decision', 'qualification'):
        status = {'documentation': 'PENDING-EXACT-EDITOR-AND-INDEPENDENT-REVIEW-DISPOSITION',
                  'engineering': 'SOURCE-OR-INHERITED-CONCERN-ONLY; unchanged implementation; no plan or execution authorized',
                  'decision': 'Preserve only decisions explicitly raised in source definition; maintainer intent remains unselected',
                  'qualification': 'Scoped source/historical evidence; source/platform/provider/device gaps remain explicit'}[kind]
        if kind == 'documentation' and cid in ('CORE-C-C019', 'CORE-B6-C016'):
            status = 'HELD-NO-DOCUMENTATION-FACET-RELEASE; edited neighboring facets do not change this'
        record['facets'][kind] = {'reference': f'core-001:{cid}#{kind}', 'traceability_only': True,
            'status': status, 'source_passage_excerpts': excerpts(definition, kind),
            'evidence_unit_refs': record['source_unit_refs'], 'documentation_path_refs': record['documentation_paths'],
            'inherited_association_keys': record['inherited_association_keys'],
            'editor_decision_refs': record['completed_editor_candidate_path_refs'],
            'held_reason_and_reopening': record['reopening_condition'],
            'qualification_limit': 'No new source-body/spec review, runtime, native, device or cryptographic evidence in this ledger.'}
        facet_count += 1
    family = record['owner']
    if family in ('A', 'C'):
        record['latest_authority'] += [f'editing/{family}/review.md', f'editing/{family}/revision-2/review.md']
        own_decisions = [editor_path_rows[k]['original_return_row'] for k in editor_candidates[cid]
                         if editor_path_rows[k]['batch'] == family]
        has_scoped_return = any(x.get('primary', family == 'A') and
            x.get('action', x.get('decision', x.get('disposition', ''))).upper() in ('EDIT', 'RETAIN') for x in own_decisions)
        released_doc = isinstance(record['reported_flags'], list) and 'D' in record['reported_flags']
        if cid not in ('CORE-C-C019', 'CORE-C-C020') and has_scoped_return and released_doc:
            record['facets']['documentation']['status'] = 'SCOPED-CONTENT-PASS-REVISION-2-FOR-RETURNED-PRIMARY-FACETS; shared/caller/qualification and final closeout remain separate'
        elif cid not in ('CORE-C-C019', 'CORE-C-C020'):
            record['facets']['documentation']['status'] = 'NO-RELEASED-PRIMARY-D-EDIT-CREDIT-FOR-THIS-FACET; retain exact qualification/referral and outside-owner review'
        elif cid == 'CORE-C-C020':
            record['facets']['documentation']['status'] = 'RETAIN-REFER-B6-SHARED-APPLET-OWNERS; no C primary edit or C acceptance of those owners'
    record['completed_review_path_scope'] = []
    for key in record['completed_editor_candidate_path_refs']:
        decision = editor_path_rows[key]
        raw = decision['original_return_row']
        state = decision['batch_review_state']
        action = raw.get('action', raw.get('decision', raw.get('disposition', 'see exact row')))
        if decision['batch'] == 'C' and not raw.get('primary', False):
            state = 'OTHER-OWNER-REFERRAL-ONLY; C batch acceptance grants no review credit here'
        record['completed_review_path_scope'].append({'decision_ref': key, 'action': action,
            'review_state': state, 'original_path_review': raw.get('review_status'),
            'accepted_primary_transfer': accepted_primary.get(decision['path']),
            'limit': 'Exact returned facet/path only; held runtime or no-D facet remains held even when another facet edits this file.'})

save('editor-returns.json', {'as_of': 'Completed returns consumed at this reconciliation; exact input hashes in verification.json.',
    'batches': editor_records, 'path_decisions': editor_path_rows,
    'accepted_primary_A_C': accepted_primary, 'prior_coordinator_validation': validation,
    'pending_assignments': ['external: assigned with no completed return consumed', 'PIV: source complete; completed edit/review return not consumed'],
    'rule': 'No partial worktree credits. Review-corrected source authority is usable as qualification while successor batch acceptance remains pending.'})
save('source-followups.json', {'records': followups, 'rule': 'All nine source returns complete; old return-pending wording is historical. Review/engineering/policy/platform/device gates remain separate.'})

# Preserve every authoritative old row and all successor observations; local aliases
# never silently convert a resolved source scope into an open defect.
inherited_rows = []
for original in registry['rows']:
    observations = []
    for module, data in boundary_inherited.items():
        rows = data.get('rows', data.get('assigned', [])) if isinstance(data, dict) else data
        for n, row in enumerate(rows):
            key = row.get('key', row.get('original', {}).get('key'))
            if key == original['key']:
                observations.append({'boundary': module, 'row_index': n, 'row': row})
    inherited_rows.append({'key': original['key'], 'original_registry_row': original,
        'successor_boundary_observations': observations,
        'state_rule': 'Original scoped resolution/decision/uncertainty preserved. Source reconciliation completes the read dimension, not implementation/device or all-document acceptance.'})
assert len(inherited_rows) == len({x['key'] for x in inherited_rows}) == 100
save('inherited-reconciliation.json', {'registry_sha256': inputs['inherited-findings/registry.json'],
    'rows': inherited_rows, 'groups_preserved': registry['groups'],
    'pgp_documentation_dependencies_preserved': registry['openpgp_documentation_dependencies'],
    'boundary_inherited_returns': boundary_inherited,
    'rule': '100 provenance rows / 70 existing groups / 21 separate PGP labels; no additive accepted bug total.'})

oath_ledger_path = 'docs/research/documentation-audit/records/implementation-followups.md'
oath_text = read(ROOT / oath_ledger_path)
oath_resolved = [dict(section(oath_text, f'IMP-{n:03d}'), original_id=f'oath-pilot-and-fixes-wave-002:IMP-{n:03d}',
                      state='PRESERVE-EXACT-SCOPED-RESOLUTION; zero new execution', authority=oath_ledger_path) for n in range(1, 15)]
protected = {'oath_fourteen_resolutions': oath_resolved,
    'oath_earlier_decisions': [dict(section(oath_text, f'DEC-{n:03d}'), original_id=f'oath-pilot-and-fixes-wave-002:DEC-{n:03d}') for n in range(1, 8)],
    'resolved_user_descriptor_xrefs': ['ctap2-001:CTAP-XREF-001', 'webauthn-001:WA-XREF-001'],
    'xrefs_limit': 'Original User/descriptor prose resolution preserved; WA-IMP-010 recovery and CTAP-DEC-002 disclosure actor remain separate.',
    'hsm_retry_message': {'reference': 'core-001:CORE-HSM-X007#retry-message-observation', 'state': 'RETRACTED-NOT-AN-OPEN-BUG',
        'authority': 'boundaries/YubiHsm/corrections.md',
        'finding': 'Helper and asymmetric test both use Invalid credential password. Preserve failed checker/history, not the mistaken mismatch.',
        'remaining_X007': 'Actual exception XML and diagnostic/test-oracle aliases stay separate.'},
    'piv': {'authority': 'boundaries/Piv/inherited-outcomes.md', 'text': read('boundaries/Piv/inherited-outcomes.md'),
        'PIVIMP-005': 'Confirmed/unfixed for historical actual tool RSA original-message failure and Ed25519 over SHA256(message); direct SDK controls passed. No new cryptographic run.'},
    'openpgp_original_findings': load('boundaries/OpenPgp/applet-findings-dispositions.json'),
    'openpgp_documentation_dependencies': load('boundaries/OpenPgp/documentation-dependencies.json'),
    'otp_original_findings': load('boundaries/YubiOtp/applet-findings-dispositions.json'),
    'hsm_original_findings': boundary_inherited['YubiHsm']['module_engineering_aliases'],
    'sd_original_aliases': boundary_inherited['SecurityDomain'].get('local_original_aliases'),
    'rule': 'Historical applet groups are preserved in addition to the 100 Core dependency rows; they are not added as new Core local candidates.'}
save('protected-dispositions.json', protected)

relations = list(full_relations['links'])
relations += [
    {'locals': ['CORE-A-C003', 'CORE-MGMT-X002'], 'relation': 'same-exact-timeout-alias-facet',
     'scope': 'DeviceInfo timeout aliases TLV/caller storage; Management adds returned result/list applicability. Firmware fallback/other ownership facets remain separate.'},
    {'locals': ['CORE-B5-C006', 'CORE-CTAP-X002', 'CORE-WA-X002', 'CORE-OATH-X002', 'CORE-HSM-X003', 'CORE-PGP-X004', 'CORE-PIV-X002', 'CORE-OTP-X001', 'CORE-SD-X004'],
     'relation': 'same-Core-per-exchange-boundary-only',
     'scope': 'Core guards/drains one exchange without full public-workflow lease. Each caller-specific cache/reset/notification/adoption/teardown sequence retains separate evidence, responsibility and decisions.'},
    {'locals': ['CORE-C-C003', 'CORE-CTAP-X003', 'CORE-WA-X003', 'CORE-PGP-X002', 'CORE-OATH-X004'],
     'relation': 'same-Core-reader-UTF16-unit-facet-only',
     'scope': 'Same Core input unit carrier; CTAP Unicode/profile enforcement, OpenPGP credential identity and OATH raw-key/UI policy remain separate.'},
    {'locals': ['CORE-B6-C007', 'CORE-C-C020', 'CORE-SD-X002'], 'relation': 'same-contained-key-certificate-owner-facet-only',
     'scope': 'Core exact retained/disposed contained objects and shared record clones. SD parameter retention for reset, local temporary cleanup and profile choices remain separate.'},
    {'locals': ['CORE-B5-C005', 'CORE-CTAP-X019', 'CORE-OTP-X003'], 'relation': 'same-Core-incoming-report-copy-facet-only',
     'scope': 'Actual consumed report arrays differ from assembled output. Applet model-owned copies, Windows nested staging and APDU/decrypt producers retain their owners.'},
    {'locals': ['CORE-B6-C011', 'CORE-OATH-X003', 'CORE-SD-X007'], 'relation': 'same-empty-SCP-encryption-facet-only',
     'scope': 'Empty LIST/RESET/A5/GET DATA callers reach Core Encrypt. Preserve selected C-DECRYPTION plus C-MAC condition and separate real-device/profile and response-auth phases.'},
    {'locals': ['CORE-B6-C012', 'CORE-SD-X007', 'CORE-OATH-X003'], 'relation': 'same-Core-protected-response-graph-facet-only',
     'scope': 'Shared secure graph/synthetic transcript qualification; actual caller instructions and profile evidence retained.'},
    {'locals': ['CORE-WA-X007'], 'inherited': ['webauthn-001:WA-IMP-010'], 'relation': 'same-descriptor-recovery-question',
     'scope': 'Actual selected singleton request association is lost at final matcher/result construction; resolved descriptor prose and native disclosure decision stay separate.'},
    {'locals': ['CORE-WA-X008'], 'inherited': ['ctap2-001:CTAP-DEC-002'], 'relation': 'same-native-output-actor-decision',
     'scope': 'Actor trace complete; native DTO versus platform/browser/RP serializer intent needs decision; no stripping or closure.'},
    {'locals': ['CORE-HSM-X005'], 'inherited': ['yubihsm-001:HSM-IMP-008'], 'relation': 'same-56-versus-48-profile-uncertainty',
     'scope': 'No inferred tail identity or truncation remedy; missing raw region/profile/live HSM authentication remains qualified.'},
]
for relation in relations:
    assert all(cid in candidates for cid in relation['locals'])
    relation['evidence_refs'] = [candidates[cid]['source_definition']['path'] for cid in relation['locals']]
save('relationships.json', {'exact_facet_relationships': relations,
    'separations_preserved': full_relations['explicit_separations'],
    'rule': 'Deduplicate exact named facets only; preserve all 185 compound original candidates. Relationship count is not a defect or accepted-findings count.'})

save('candidate-reconciliation.json', {'state': 'DRAFT-PENDING-EDIT-REVIEW-CLOSEOUT', 'source_baseline': PIN,
    'stable_identity_policy': 'Use core-001:<original-local-ID>#dimension. No new CORE-DOC/CORE-IMP layer is necessary or allocated. These are traceability references, not accepted claims.',
    'candidates': candidates,
    'source_evidence_units': {uid: x for uid, x in all_units.items() if any(uid in refs for refs in candidate_refs.values())},
    'documentation_surfaces': path_records,
    'editor_reference_table': 'editor-returns.json', 'qualification': 'Original source pin distinct from legitimate current documentation edits; implementation-token proof is separate.'})
save('deferred-facets.json', {'state': 'DRAFT-PENDING-EDIT-REVIEW-CLOSEOUT',
    'purpose': 'Findings, decisions, status, evidence and exact documentation reopening only; no implementation/experiment plan or work authorization.',
    'facets': [{'candidate': cid, 'owner': item['owner'], 'original_source': item['source_definition']['path'],
               'observed': item['observed_behavior'], 'desired_or_unselected_intent': item['desired_behavior'],
               'engineering': item['facets']['engineering'], 'decision': item['facets']['decision'],
               'qualification': item['facets']['qualification'], 'latest_authority': item['latest_authority'],
               'editor_path_scope': item['completed_review_path_scope']}
              for cid, item in candidates.items()],
    'scope_rule': 'A dimension with no separately identified engineering defect is retained as a qualification/referral, not counted as an open bug. Read exact original passage; maintainer decisions remain unselected.',
    'protected_resolutions_and_retraction': 'protected-dispositions.json',
    'stop': 'Finish Core Pass-1 publication and this deferred handoff, then stop. No Pass-2 planning or implementation project.'})
save('coverage-reconciliation.json', {'source_baseline': PIN, 'populations': populations,
    'core_family_units': 11101, 'boundary_units': sum(expected_boundary.values()), 'total_distinct_assigned_units': len(all_units),
    'Core_local_candidates': core_count, 'boundary_local_candidates': len(candidates) - core_count, 'total_original_candidates': len(candidates),
    'facet_dimension_records': facet_count, 'read_scope': 'B4 105 full-file reads plus one qualified API snapshot reconciliation retained; boundary context/snapshot exclusions retained.',
    'acceptance': 'Source disposition coverage only; no new review/implementation/execution or Pass-1 closure.'})

lines = ['# Final source-candidate/editor reconciliation — draft', '',
    'All rows retain original source IDs. Facet references use `core-001:<ID>#documentation|engineering|decision|qualification` for traceability only.',
    'Exact observed/desired passages, source evidence/range hashes, editor/path decisions and holds are in `candidate-reconciliation.json`; current editor snapshots and review states are in `editor-returns.json`.', '',
    '| Original candidate | Source concern | Editor batches with returned candidate/path decisions | Remaining disposition |', '| --- | --- | --- | --- |']
for cid, item in candidates.items():
    owners = sorted({editor_path_rows[k]['batch'] for k in editor_candidates[cid]})
    title = item['source_definition']['heading']
    source = '../../' + item['source_definition']['path']
    state = '; '.join(batches[b]['state'] for b in owners) if owners else 'Source complete; per-path assignment/retained-facet decision remains explicit in map; no returned edit credit.'
    lines.append('| ' + ' | '.join(x.replace('|', '\\|').replace('\n', ' ') for x in
        [f'[{cid}]({source})', title, ', '.join(owners) or 'No completed candidate-associated return', state]) + ' |')
(OUT / 'rows.md').write_text('\n'.join(lines) + '\n')
link_count = 0
for file in (RUN / 'deferred-findings.md', OUT / 'rows.md'):
    for target in re.findall(r'\]\(([^)]+)\)', file.read_text()):
        if '://' not in target:
            assert (file.parent / target.split('#')[0]).exists(), ('local record link', file, target)
            link_count += 1
assert [x['original_registry_row'] for x in inherited_rows] == registry['rows']
assert len(registry['groups']) == 70 and len(registry['openpgp_documentation_dependencies']) == 21
assert len(re.findall(r'^\| \[CORE-', (OUT / 'rows.md').read_text(), re.M)) == len(candidates)
for path, digest in old_hashes.items():
    assert sha((RUN / path).read_bytes()) == digest
save('verification.json', {'status': 'PASS records/source-pin joins; edit/review/closeout pending', 'source_baseline': PIN,
    'core_local_ids': core_count, 'boundary_local_ids': len(candidates) - core_count, 'original_candidates': len(candidates),
    'family_unit_counts': {k: v['unit_count'] for k, v in populations.items()}, 'disjoint_units': len(all_units),
    'pinned_source_paths': len(pinned), 'original_ranges_verified_from_pin': sum(x['inventory']['path'] in pinned for x in all_units.values()),
    'non_pin_metadata_qualification': metadata_gaps, 'candidate_documentation_paths': len(path_records),
    'inherited_rows': len(inherited_rows), 'inherited_groups': len(registry['groups']), 'pgp_labels': len(registry['openpgp_documentation_dependencies']),
    'older_triage_files_preserved': old_hashes, 'accepted_A_C_transferred_paths': len(accepted_primary),
    'accepted_A_C_current_hash_matches': sum(x['matches_accepted_transfer'] for x in accepted_primary.values()),
    'local_record_links_checked': link_count, 'new_canonical_CORE_DOC_IMP_ids': 0,
    'input_hashes': inputs, 'new_build_test_native_device_crypto_probe_executions': 0,
    'limits': ['No product/coverage/registry/index/ref edits, commits, subagents or new investigations.',
               'Completed editor/review returns only; no partial or unfinished product inspection.',
               'No new schedules, refactors, regression implementations, experiment plans or Pass-2 project.',
               'Source reconciliation and documented gates do not imply Core Pass-1 publication.']})
save('output-identities.json', {'state': 'DRAFT-PENDING-EDIT-REVIEW-CLOSEOUT', 'source_baseline': PIN,
    'scope': 'Owned final reconciliation records and draft handoff; excludes this manifest itself. Not a publication/acceptance seal.',
    'files': {str(p.relative_to(RUN)): sha(p.read_bytes()) for p in
              [RUN / 'deferred-findings.md'] + sorted(x for x in OUT.iterdir() if x.is_file() and x.name != 'output-identities.json')}})
print(json.dumps({k: v for k, v in json.loads((OUT / 'verification.json').read_text()).items()
                  if k not in ('input_hashes', 'older_triage_files_preserved', 'limits')}))
