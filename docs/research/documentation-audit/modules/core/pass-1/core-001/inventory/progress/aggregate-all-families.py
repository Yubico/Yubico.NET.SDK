#!/usr/bin/env python3
"""Mechanical joins for eight final families and all nine pinned static boundaries."""
import collections
import hashlib
import json
from pathlib import Path
import subprocess

RUN = Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
INV = RUN / 'inventory'
HISTORY = INV / 'progress/history/five-family'
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
inputs, sources, current_raw, metadata_context, source_representations = {}, {}, {}, {}, {}


def sha(data):
    return hashlib.sha256(data).hexdigest()


def read(path):
    data = path.read_bytes()
    inputs[str(path)] = sha(data)
    return data


def load(path):
    return json.loads(read(path))


def source(path):
    if path not in sources:
        if files[path]['git_blob'] is None:
            assert path == 'docs/research/documentation-audit/core.md'
            pinned = subprocess.check_output(['git', 'show', '01a114d4:' + path])
            metadata_context[path] = 'Post-source-pin active campaign handoff; inventory raw hash and published 01a114d4 bytes checked, no fe342 source-presence claim.'
        else:
            pinned = subprocess.check_output(['git', 'show', PIN + ':' + path])
        # Reconstruct only recorded checkout representation, selected by its exact original hash.
        current = Path(path).read_bytes()
        candidates = [pinned]
        if sha(current) == files[path]['sha256']:
            if current != pinned:
                normalize = lambda b: b.decode('utf-8-sig', errors='strict').replace('\r\n', '\n')
                assert normalize(current) == normalize(pinned), path
            candidates.append(current)
        if sha(pinned) != files[path]['sha256']:
            text = pinned.decode('utf-8-sig', errors='strict').replace('\r\n', '\n')
            candidates += [(bom + text.replace('\n', newline)).encode() for bom in ['', '\ufeff'] for newline in ['\n', '\r\n']]
        original = next((b for b in candidates if sha(b) == files[path]['sha256']), None)
        assert original is not None, ('Original inventory representation unavailable', path)
        sources[path] = original.decode('utf-8-sig', errors='replace')
        source_representations[path] = ('exact pinned bytes' if original == pinned else
            'unchanged original checkout bytes with exact inventory hash and normalized pinned equality' if original == current else
            'exact original hash recovered by explicit UTF8 BOM/newline representation')
        current_raw[path] = sha(current)
        if current_raw[path] != files[path]['sha256']:
            assert current_raw[path] == accepted_paths.get(path), ('Current path outside accepted A/C manifest', path)
    return sources[path]


def check_row(row, key):
    unit = units[key]
    for field in ['path', 'start_character', 'end_character_exclusive', 'first_line', 'last_line', 'source_sha256', 'range_sha256']:
        assert row[field] == unit[field], (key, field)
    assert unit['status'] == 'UNREAD'
    text = source(unit['path'])
    assert sha(text[unit['start_character']:unit['end_character_exclusive']].encode()) == unit['range_sha256'], key


def count(ids, states):
    result = dict(total=len(ids), scope={}, scope_by_assignment={}, primary_shared={}, dispositions={}, category={})
    for key in sorted(ids):
        u, state = units[key], states[key]
        scope = 'Core' if u['path'].startswith('src/Core/') else 'external'
        assignment = 'shared' if key in shared else 'primary'
        result['scope'][scope] = result['scope'].get(scope, 0) + 1
        result['scope_by_assignment'].setdefault(assignment, collections.Counter())[scope] += 1
        result['primary_shared'].setdefault(assignment, collections.Counter())[state] += 1
        result['dispositions'][state] = result['dispositions'].get(state, 0) + 1
        result['category'].setdefault(u['category'], collections.Counter())[state] += 1
    return result


read(Path(__file__))
receipt = load(HISTORY / 'receipt.json')
for entry in receipt['files']: assert sha(read(Path(entry['snapshot_path']))) == entry['sha256']
prior = load(HISTORY / 'coverage-progress.json')
previous_rollup_dir = INV / 'progress/history/eight-family-two-boundary'
previous_receipt = load(previous_rollup_dir / 'receipt.json')
for entry in previous_receipt['files']: assert sha(read(Path(entry['snapshot_path']))) == entry['sha256']
assert previous_receipt['completed_units'] == 12022 and previous_receipt['remaining_units'] == 2022
assert prior['source_baseline'] == PIN and prior['total_completed_return']['total'] == 7135 and prior['remaining']['total'] == 6909
for path, digest in prior['input_sha256'].items():
    target = HISTORY / 'aggregate.py.snapshot' if path == str(INV / 'progress/aggregate.py') or path == str((INV / 'progress/aggregate.py').resolve()) else Path(path)
    assert sha(read(target)) == digest, ('Prior input changed', path)
units = {u['id']: u for u in load(INV / 'semantic-units.json')}
files = {f['path']: f for f in load(INV / 'files.json')}
shared = {s['unit_id']: s for s in load(INV / 'shared-sections.json')}
packets = {p.stem: load(p) for p in sorted((INV / 'packets').glob('*.json'))}
original_paths = subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', '01a114d4', '--', str(INV)], text=True).splitlines()
for path in original_paths + [str(RUN / 'coverage.json')]:
    assert Path(path).read_bytes() == subprocess.check_output(['git', 'show', '01a114d4:' + path]), path
accepted = load(RUN / 'editing/integration/accepted-A-C.json')
assert accepted['source_baseline'] == PIN
accepted_paths = {row['path']: row['sha256'] for row in accepted['product_files']}
assert len(accepted_paths) == len(accepted['product_files']) == 91
for path, digest in accepted_paths.items(): assert sha(Path(path).read_bytes()) == digest, ('Accepted A/C current identity', path)
for family, verdict_key in [('A', 'decision'), ('C', 'verdict')]:
    review = load(RUN / 'editing' / family / 'revision-2/review-identities.json')
    assert review[verdict_key] == 'PASS'
coordinator_checks = {}
for label, count_expected in [('source-tokens-after-a-c', 1320), ('api-after-a-c', 20)]:
    directory = RUN / 'validation/commands-v2' / label
    gate = load(directory / 'receipt.json')
    assert gate['status'] == 'passed' and gate['exit_code'] == 0 and gate['stream_complete'] and gate['retention_complete']
    for stream in gate['streams']: assert sha(read(Path(stream['path']))) == stream['sha256']
    stdout = read(directory / 'stdout.log').decode()
    assert ('files=' + str(count_expected) + ', failures=0') in stdout
    coordinator_checks[label] = dict(record=str(directory / 'receipt.json'), files=count_expected, failures=0,
        status='RETAINED-COORDINATOR-PASS; not rerun by aggregation')

states, raw_states, assignments = {}, {}, {}
families = dict(prior['completed_families'])
prior_packet_names = {'A': 'A', 'B1': 'B1-portable-lifetimes', 'B2': 'B2-linux-lifetimes', 'B3': 'B3-macos-lifetimes', 'C': 'C'}
for family, packet_name in prior_packet_names.items():
    p = packets[packet_name]
    expected = set(p['unit_ids'] + p['shared_read_unit_ids'])
    ledger = load(Path(families[family]['ledger']))
    rows = ledger['units'].items() if family == 'A' else ((x['unit_id'] if family == 'B1' else x['id'], x) for x in ledger)
    own = {}
    for key, row in rows:
        if family == 'A': status = row['status']; table = {'READ-CLEAN': 'read-clean', 'CANDIDATE': 'read-candidate'}
        elif family == 'B1': status = row['disposition']; table = {'READ-CLEAN': 'read-clean', 'READ-CANDIDATE': 'read-candidate', 'READ-EXCLUSION': 'read-exclusion'}
        else:
            status = row['status']
            table = {'READ-CLEAN': 'read-clean', 'READ-CANDIDATE': 'read-candidate', 'READ-EXCLUSION': 'read-exclusion',
                     'READ-CANDIDATE-CONTEXT': 'read-candidate-context', 'READ-HISTORICAL-EXCLUSION': 'read-historical-exclusion', 'READ-HISTORICAL-CONTEXT': 'read-historical-context'}
        assert key not in own and key not in states
        own[key] = table[status]; raw_states[key] = status; assignments[key] = family
    assert set(own) == expected and count(expected, own) == families[family]['counts']
    states.update(own)
assert len(states) == 7135

new_configs = {
    'B4': ('B4-windows-support', 'coverage-summary.json', 'source_pin', 'combined_status_counts', 'local_candidate_count'),
    'B5': ('B5-session-framing', 'coverage-summary.json', 'source_baseline', 'counts', 'local_candidates'),
    'B6': ('B6-wire-scp', 'final-summary.json', 'source_pin', 'unit_dispositions', 'local_concern_groups')}
depth_limits, exclusions = [], list(prior['exclusions'])
for family, (packet_name, summary_name, baseline_key, status_key, groups_key) in new_configs.items():
    d = RUN / 'investigation' / family
    report = RUN / ('investigation-' + family + '.md')
    assert PIN.encode() in read(report)
    summary, rows = load(d / summary_name), load(d / 'unit-dispositions.json')
    assert summary[baseline_key] == PIN
    p = packets[packet_name]; primary, shared_ids = set(p['unit_ids']), set(p['shared_read_unit_ids'])
    assert len(rows) == len(primary | shared_ids) and len({x['id'] for x in rows}) == len(rows)
    assert {x['id'] for x in rows} == primary | shared_ids and not (primary | shared_ids) & states.keys()
    own = {}
    for row in rows:
        key = row['id']; check_row(row, key)
        expected_assignment = 'primary' if key in primary else 'shared'
        if family == 'B4':
            assert row['ownership'] == ('primary' if key in primary else 'assigned-shared')
            assert row['assigned_scope_disposition_complete'] is True and row['pending_context'] is None
            assert row['actual_range_sha256'] == units[key]['range_sha256']
            raw_status = row['status']
            normalized = {'READ-CLEAN': 'read-clean', 'READ-CANDIDATE': 'read-candidate', 'READ-SOURCE-QUALIFIED': 'read-source-qualified',
                          'READ-RELATED-CONTEXT': 'read-related-context', 'READ-HISTORICAL-QUALIFIED': 'read-historical-qualified'}[raw_status]
            if row['complete_context_read'] is False:
                assert key == 'core-001:unit:8a14c1c56398fcde' and raw_status == 'READ-RELATED-CONTEXT'
                depth_limits.append(dict(id=key, path=row['path'], family='B4', original_status=raw_status,
                    full_context_read=False, scope=row['scope'], qualification=row['qualification'], clean_credit=0,
                    limitation='Whole Unshipped snapshot support/type reconciliation; no independent all-member semantic binding or compiler verification.'))
        elif family == 'B5':
            assert row['assignment'] == expected_assignment and row['read_status'] == 'FULL-ASSIGNED-UNIT-READ-WITH-CONTEXT'
            raw_status = row['status']
            normalized = {'READ-CLEAN': 'read-clean', 'READ-CANDIDATE': 'read-candidate', 'READ-EXCLUSION': 'read-exclusion'}[raw_status]
        else:
            assert row['assignment'] == expected_assignment and row['semantic_review'] == 'COMPLETE-FOR-ASSIGNED-SCOPE'
            raw_status = row['disposition']
            normalized = {'READ-CLEAN': 'read-clean', 'READ-CANDIDATE': 'read-candidate', 'READ-CONTEXT': 'read-context', 'READ-HISTORICAL-CONTEXT': 'read-historical-context'}[raw_status]
        if key in shared_ids: assert shared[key]['investigation_stage'] == packet_name
        else: assert units[key]['stage'] == packet_name
        own[key], raw_states[key], assignments[key] = normalized, raw_status, family
        if normalized == 'read-exclusion':
            exclusions.append(dict(id=key, path=row['path'], primary_owner=family, state=normalized, reason=row['semantic_context'], increment=0))
    assert dict(collections.Counter(raw_states[x] for x in own)) == summary[status_key]
    primary_key, shared_key = {'B4': ('primary_status_counts', 'shared_status_counts'), 'B5': ('primary_counts', 'shared_counts'), 'B6': ('primary_dispositions', 'shared_dispositions')}[family]
    assert dict(collections.Counter(raw_states[x] for x in primary)) == summary[primary_key]
    assert dict(collections.Counter(raw_states[x] for x in shared_ids)) == summary[shared_key]
    if family == 'B4': assert summary['complete_primary_file_reads'] == 105 and summary['scoped_related_context_snapshot_files'] == 1
    families[family] = dict(packet=str(INV / 'packets' / (packet_name + '.json')), report=str(report), ledger=str(d / 'unit-dispositions.json'),
        summary=str(d / summary_name), primary=len(primary), shared=len(shared_ids), local_candidate_groups=summary[groups_key],
        counts=count(set(own), own), original_status_counts=summary[status_key], independent_review='PENDING', editing='PENDING', SDK_runtime_verification='PENDING')
    states.update(own)

assert len(depth_limits) == 1
family_ids = set(states)
assert len(family_ids) == 11101
boundaries = {}
for module, status_field, table in [
    ('Management', 'status', {'READ-CLEAN': 'read-clean', 'READ-CANDIDATE-CONTEXT': 'read-candidate-context'}),
    ('Ctap2', 'disposition', {'READ-CLEAN': 'read-clean', 'READ-CANDIDATE': 'read-candidate', 'READ-CANDIDATE-CONTEXT': 'read-candidate-context', 'READ-EXCLUSION': 'read-exclusion'})]:
    d = RUN / 'boundaries' / module; report = RUN / ('boundary-' + module + '.md')
    assert PIN.encode() in read(report)
    verification, rows, file_rows = load(d / 'verification.json'), load(d / 'unit-dispositions.json'), load(d / 'file-dispositions.json')
    assert verification['source_pin' if module == 'Management' else 'source_baseline'] == PIN
    packet_name = 'BOUNDARY-' + module; p = packets[packet_name]
    expected = set(p['unit_ids']); assert not p['shared_read_unit_ids']
    assert len(rows) == len({x['id'] for x in rows}) == len(expected) and {x['id'] for x in rows} == expected
    assert not expected & states.keys()
    assert len(file_rows) == len({x['path'] for x in file_rows}) == len(p['disposition_paths'])
    assert {x['path'] for x in file_rows} == set(p['disposition_paths'])
    for f in file_rows:
        assert f['source_sha256'] == files[f['path']]['sha256']; source(f['path'])
    own = {}
    for row in rows:
        key = row['id']; check_row(row, key); assert units[key]['assigned_family'] == packet_name
        if module == 'Ctap2': assert row['source_baseline'] == PIN and row['read'] == 'complete-assigned-unit-with-boundary-context'
        own[key] = table[row[status_field]]; raw_states[key] = row[status_field]; assignments[key] = packet_name
        if own[key] == 'read-exclusion': exclusions.append(dict(id=key, primary_owner=packet_name, path=row['path'], state='read-exclusion/API-support', reason=row['exclusion_reason'], increment=0))
    summary_key = 'unit_status_counts' if module == 'Management' else 'disposition_counts'
    assert dict(collections.Counter(raw_states[x] for x in own)) == verification[summary_key]
    boundaries[module] = dict(report=str(report), ledger=str(d / 'unit-dispositions.json'), file_dispositions=str(d / 'file-dispositions.json'),
        selected_paths=len(p['paths']), candidate_file_population=len(file_rows), primary=len(expected), shared=0, counts=count(expected, own),
        original_status_counts=verification[summary_key], independent_review='PENDING', editing='PENDING', SDK_runtime_verification='PENDING')
    states.update(own)

# Additive source follow-ups carry zero extra unit credit; bind their actual unchanged inputs.
def hash_map(mapping):
    for relative, digest in mapping.items(): assert sha(read(RUN / relative)) == digest, relative

mgmt6 = load(RUN / 'boundaries/Management/followup-B6.json')
mgmt4 = load(RUN / 'boundaries/Management/followup-B4.json')
ctap = load(RUN / 'boundaries/Ctap2/followup-B4-B6.json')
for record in [mgmt6, mgmt4, ctap]: assert record['source_pin'] == PIN
for record in [mgmt6, mgmt4]:
    for path, digest in record['current_source_sha256'].items():
        source(path)
        assert files[path]['sha256'] == digest, path
for path, digest, ranges in ctap['source_rows']:
    source(path)
    assert files[path]['sha256'] == digest and all(first <= last for first, last in ranges), path
for record, keys in [(mgmt6, ['preserved_original_sha256', 'completed_B6_inputs_sha256']),
                     (mgmt4, ['preserved_prior_sha256', 'B4_inputs_sha256']),
                     (ctap, ['input_hashes', 'retained_original_hashes'])]:
    for key in keys: hash_map(record[key])
read(RUN / 'boundaries/Management/boundary-completion.md')
read(RUN / 'boundaries/Management/followup-B4.md'); read(RUN / 'boundaries/Management/followup-B6.md')
read(RUN / 'boundaries/Ctap2/followup-B4-B6.md')
assert mgmt4['source_investigation'] == 'COMPLETE-FOR-ORIGINAL-MANAGEMENT-PACKET-AND-MANDATORY-B4-B6-FOLLOWUPS'
assert ctap['overall_status'] == 'NAMED-PINNED-SOURCE-FOLLOWUPS-RECONCILED-WITH-QUALIFICATIONS'
assert all(x['source_status'] == 'RECONCILED' for x in ctap['followups'])
assert ctap['mandatory_open'][0]['owner'] == 'BOUNDARY-WebAuthn and maintainer/coordinator' and ctap['mandatory_open'][0]['status'] == 'OPEN-RETURN-NOT-RECEIVED'
# That original B4/B6 addendum remains historical; the later WebAuthn source handoff supersedes its pending return.
wa_followup = load(RUN / 'boundaries/Ctap2/followup-WebAuthn.json')
assert wa_followup['source_pin'] == PIN and wa_followup['source_followup_status'] == 'SOURCE-RECONCILED-WITH-QUALIFICATIONS'
assert wa_followup['final_pinned_boundary_source_status'] == 'COMPLETE-WITH-QUALIFICATIONS'
hash_map(wa_followup['input_hashes'])
assert sha(read(RUN / 'boundaries/Ctap2/followup-WebAuthn.md')) == wa_followup['report_sha256']
assert sha(read(RUN / 'boundaries/Ctap2/boundary-completion.md')) == wa_followup['completion_addendum_sha256']
assert sha((RUN / 'boundaries/Ctap2/unit-dispositions.json').read_bytes()) == wa_followup['preservation']['ctap_unit_ledger']['sha256']
assert sha((RUN / 'boundaries/WebAuthn/unit-dispositions.json').read_bytes()) == wa_followup['preservation']['webauthn_unit_ledger']['sha256']
boundaries['Management']['source_boundary_closure'] = 'COMPLETE-PINNED-SOURCE; mandatory B4/B6 follow-ups source-reconciled, qualifications retained'
boundaries['Ctap2']['source_boundary_closure'] = 'COMPLETE-PINNED-SOURCE; B4/B6/WebAuthn follow-ups source-reconciled; actor-policy/runtime qualifications remain OPEN'
for module in boundaries: boundaries[module]['followup_extra_unit_credit'] = 0

boundary_configs = {
    'SecurityDomain': ('summary.json', 'source_pin', 'dispositions', 'local_candidates', 'sha256', 'status',
        {'READ-CLEAN': 'read-clean', 'READ-CANDIDATE': 'read-candidate', 'READ-CONTEXT': 'read-context'}),
    'Oath': ('coverage-summary.json', 'source_pin', 'counts', 'local_candidate_groups', 'source_sha256', 'status',
        {'READ-CLEAN': 'read-clean', 'READ-CANDIDATE': 'read-candidate', 'READ-CONTEXT': 'read-context'}),
    'WebAuthn': ('verification.json', 'source_baseline', 'disposition_counts', 'local_concern_groups', 'source_sha256', 'disposition',
        {'READ-CLEAN': 'read-clean', 'READ-CANDIDATE': 'read-candidate', 'READ-RELATED-CANDIDATE-CONTEXT': 'read-related-candidate-context', 'READ-EXCLUSION': 'read-exclusion'}),
    'YubiOtp': ('coverage-summary.json', 'source_baseline', 'counts', 'candidate_groups', 'raw_sha256', 'status',
        {'READ-CLEAN': 'read-clean', 'READ-CANDIDATE': 'read-candidate'}),
    'YubiHsm': ('coverage-summary.json', 'source_baseline', 'dispositions', 'provisional_candidate_groups', 'source_sha256', 'status',
        {'READ-CLEAN': 'read-clean', 'READ-CANDIDATE': 'read-candidate', 'READ-CONTEXT': 'read-context'}),
    'OpenPgp': ('coverage-summary.json', 'source_baseline', 'states', 'provisional_local_groups', 'source_sha256', 'status',
        {'READ-CLEAN': 'read-clean', 'READ-CANDIDATE': 'read-candidate', 'READ-RELATED-CONTEXT': 'read-related-context'}),
    'Piv': ('coverage-summary.json', 'source_baseline', 'dispositions', 'local_proposal_groups', 'source_sha256', 'status',
        {'READ-CLEAN': 'read-clean', 'READ-CANDIDATE': 'read-candidate', 'READ-CONTEXT': 'read-context'})}
for module, (summary_name, baseline_key, status_key, groups_key, file_hash_key, row_status_key, table) in boundary_configs.items():
    d = RUN / 'boundaries' / module; report = RUN / ('boundary-' + module + '.md')
    assert PIN.encode() in read(report)
    summary, rows, file_rows = load(d / summary_name), load(d / 'unit-dispositions.json'), load(d / 'file-dispositions.json')
    assert summary[baseline_key] == PIN
    name = 'BOUNDARY-' + module; p = packets[name]; primary = set(p['unit_ids'])
    assert not p['shared_read_unit_ids'] and len(primary) == len(p['unit_ids'])
    assert len(rows) == len({row['id'] for row in rows}) == len(primary)
    assert {row['id'] for row in rows} == primary and not primary & states.keys()
    assert len(file_rows) == len({row['path'] for row in file_rows}) == len(p['disposition_paths'])
    assert {row['path'] for row in file_rows} == set(p['disposition_paths'])
    for row in file_rows:
        assert row[file_hash_key] == files[row['path']]['sha256'], (module, row['path'])
        source(row['path'])
    own = {}
    for row in rows:
        key = row['id']; check_row(row, key)
        assert units[key]['assigned_family'] == name
        assert row['source_pin' if module in ['SecurityDomain', 'Oath'] else 'source_baseline'] == PIN
        if module == 'SecurityDomain': assert row['current_source_sha256'] == units[key]['source_sha256'] and row['current_range_sha256'] == units[key]['range_sha256']
        elif module == 'Oath': assert row['actual_range_sha256'] == units[key]['range_sha256']
        elif module == 'WebAuthn':
            if row['id'] == 'core-001:unit:4f63111f73121848':
                assert row['disposition'] == 'READ-EXCLUSION' and row['read'] == 'declaration-inventory-reconciled-exclusion'
            else: assert row['read'] == 'complete-assigned-unit-with-boundary-context'
        elif module in ['YubiOtp', 'OpenPgp']: assert row['read_scope'] in ['complete original range with full containing file/caller/helper/assertion context', 'complete original assigned range with complete containing file, helper/caller/assertion context']
        elif module == 'YubiHsm': assert row['read_status'] == 'READ-COMPLETE'
        elif module == 'Piv': assert row['read_status'] == 'COMPLETE' and row['source_and_range_identity'] == 'MATCH'
        raw_status = row[row_status_key]
        own[key], raw_states[key], assignments[key] = table[raw_status], raw_status, name
        if own[key] == 'read-exclusion':
            exclusions.append(dict(id=key, primary_owner=name, path=row['path'], state='read-exclusion/API-support', reason=row['exclusion_reason'], increment=0))
    assert dict(collections.Counter(raw_states[key] for key in own)) == summary[status_key]
    boundaries[module] = dict(report=str(report), ledger=str(d / 'unit-dispositions.json'), summary=str(d / summary_name),
        file_dispositions=str(d / 'file-dispositions.json'), selected_paths=len(p['paths']), candidate_file_population=len(file_rows),
        primary=len(primary), shared=0, counts=count(primary, own), original_status_counts=summary[status_key],
        provisional_local_groups=summary[groups_key], source_boundary_closure='COMPLETE-PINNED-SOURCE-WITH-QUALIFICATIONS',
        independent_review='PENDING', editing='PENDING', compiler_runtime_qualification='PENDING', followup_extra_unit_credit=0)
    states.update(own)

otp_packet = packets['BOUNDARY-YubiOtp']
otp_extra = set(otp_packet['disposition_paths']) - set(otp_packet['paths'])
assert len(otp_packet['paths']) == 61 and len(otp_packet['disposition_paths']) == otp_packet['file_count'] == 62
assert otp_extra == {'src/YubiOtp/src/PublicAPI.Shipped.txt'}
assert not any(units[key]['path'] in otp_extra for key in otp_packet['unit_ids'])
boundaries['YubiOtp']['path_count_qualification'] = dict(selected_paths=61, disposition_paths=62, declared_file_count=62,
    disposition_only_paths=sorted(otp_extra), assigned_units=324, extra_assigned_unit_credit=0,
    explanation='Directive-only shipped API snapshot receives a file disposition, not an invented semantic unit. Literal original arrays and all 324 IDs preserved.')

read(RUN / 'investigation/B4/continuation-and-verification.md')
read(RUN / 'investigation/B4/candidates.md')
assert b'core-001:unit:9fe6da15d252bcd2' in (RUN / 'investigation/B4/continuation-and-verification.md').read_bytes()
platform = next(x for x in exclusions if x['id'] == 'core-001:unit:9fe6da15d252bcd2')
platform.update(state='read-exclusion/secondary-source-supplied', Windows='B4 source disposition supplied; access/elevation candidate and native/late-user-delta qualification OPEN',
    source_handoff='B3 macOS and B4 Windows supplied; parent remains original exclusion, zero duplicate credit')

universe = {k for k, u in units.items() if u['status'] == 'UNREAD'}
remaining = universe - states.keys(); remaining_packets = {}
assert len(boundaries) == 9 and not remaining and set(states) == universe
totals = count(set(states), states); family_counts = count(family_ids, states); pending_counts = count(remaining, dict.fromkeys(remaining, 'pending-completed-return'))
assert totals['total'] == 14044 and pending_counts['total'] == 0 and totals['scope'] == {'Core': 9020, 'external': 5024}
assert sum(row['primary'] for row in boundaries.values()) == 2943
# Final all-ID coordinate proof uses original Git/checkout bytes, including A/C ranges whose
# current comments have changed. This is mechanical hashing, not a second semantic source audit.
for key in sorted(states):
    unit = units[key]; original_text = source(unit['path'])
    assert sha(original_text[unit['start_character']:unit['end_character_exclusive']].encode()) == unit['range_sha256'], key

for path, digest in inputs.items(): assert sha(Path(path).read_bytes()) == digest, ('Input changed during capture', path)
for path, digest in current_raw.items(): assert sha(Path(path).read_bytes()) == digest, ('Source changed during capture', path)
result = dict(schema='core-001-coverage-progress-v4', source_baseline=PIN,
    observed_record_head=subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip(),
    measure_definitions=dict(total_completed_return='Distinct IDs with final investigator scoped read dispositions, including context/qualified/excluded records; not all fully read prose, semantic API binding or runtime verification',
        remaining='IDs awaiting a released completed return; active/mutable boundaries receive zero partial credit'),
    completed_families=families, completed_boundaries=boundaries, family_only_totals=family_counts,
    total_completed_return=totals, remaining=pending_counts, remaining_packets=remaining_packets,
    original_partition=prior['original_partition'], source_depth_limitations=depth_limits, exclusions=exclusions,
    original_schema_fields=dict(A='unit-results.units[id].status; read bool; ownership', B1='unit_id; disposition; read_status; ownership',
        B2='id; status; assignment_kind', B3='id; status; ownership', C='id; status; assignment; read_complete',
        B4='id; status; ownership; complete_context_read and assigned_scope_disposition_complete kept distinct',
        B5='id; status; assignment; read_status', B6='id; disposition; assignment; semantic_review',
        Management='id; status; original boundary verification and additive source follow-ups', Ctap2='id; disposition; read; source_baseline; mandatory_open follow-up states'),
    original_field_preservation='No original ledger rewritten. Hash-bound ledger references retain every original field; normalization uses explicit per-schema exact-value maps, not status text matching.',
    prior_five_family_snapshot=dict(receipt=str(HISTORY / 'receipt.json'), sha256=inputs[str(HISTORY / 'receipt.json')], completed_units=7135, remaining_units=6909),
    prior_rollup_snapshot=dict(receipt=str(previous_rollup_dir / 'receipt.json'), sha256=inputs[str(previous_rollup_dir / 'receipt.json')], completed_units=12022, remaining_units=2022),
    current_documentation_state=dict(source_pin='Original investigation coordinates/hashes remain fe342; current documentation is a separate representation',
        A_C_accepted_transfer_record='editing/integration/accepted-A-C.json', accepted_documentation_paths=len(accepted_paths),
        accepted_path_hashes=accepted_paths, review='A/C revision-2 scoped content reviews PASS; broader independent review remains a separate gate',
        coordinator_token_API_checks=coordinator_checks, global_edit_review_compiler_runtime_completion='PENDING; bounded A/C and preservation gates are not campaign completion'),
    qualification=dict(independent_review='PENDING', editing='PENDING', SDK_runtime_verification='PENDING', accepted_bug_count=None,
        family_local_groups_nonadditive=sum(x['local_candidate_groups'] for x in families.values()),
        boundary_local_groups_nonadditive={module: entry.get('provisional_local_groups', {'Management': 3, 'Ctap2': 23}.get(module)) for module, entry in boundaries.items()},
        final_clean_sampling_denominator=None, clean='Type/native/context/historical/rendering strata explicit; source clean is not independent acceptance',
        API='1,442-entry orientation/projection is not compiler binding or all-member semantic verification. B4 whole Unshipped snapshot is qualified related context, zero clean credit.',
        active_investigators=[], source_boundary_packets='All nine completed static source dispositions; zero unfinished source packets', active_partial_records_read_or_credited=0,
        Windows='Pinned B4 secondary source supplied; new user Windows concurrency delta and native qualification remain OPEN', PIV='Physical/touch/hotplug deferral retained'),
    structural_verification=dict(result='PASS', completed_IDs_consumed_exactly_once=len(states), missing_or_extra_assigned_IDs=0, overlaps=0,
        original_inventory_files_unchanged=len(original_paths), original_coverage_unchanged=True, current_source_paths_checked=len(sources),
        prior_five_family_qualification='Exact ledger/packet/inventory input hashes preserved and ID unions rejoined; old source qualification reused',
        original_ranges='All ledger ranges checked against exact original inventory hashes. New checks use git show fe342 plus hash-selected original BOM/newline representation, not current edited comment coordinates.',
        current_source_state='Current raw hashes separately captured; differences from original inventory require exact accepted A/C transfer manifest identity. No runtime-change inference from documentation bytes.',
        source_representation_counts=dict(collections.Counter(source_representations.values())),
        current_edited_paths_with_original_ranges_checked=sorted(path for path in sources if current_raw[path] != files[path]['sha256']),
        boundary_candidate_file_populations={module: entry['candidate_file_population'] for module, entry in boundaries.items()}, secondary_followup_unit_increment=0,
        post_source_pin_metadata_context=metadata_context), input_sha256=inputs)
(RUN / 'coverage-progress.json').write_text(json.dumps(result, indent=2) + '\n')

lines = ['# Core structural source-disposition coverage', '', f'Original investigation source: `{PIN}`. **All eight families and nine static boundary packets have final scoped dispositions.**', '',
    '**14,044 distinct assigned IDs: 9,020 Core + 5,024 external; zero remaining, overlaps, missing or extra IDs.** Primary/shared totals and exact status/category strata remain in JSON.',
    'Eight families contribute 11,101 IDs (A 2,006; B1 1,204; B2 1,219; B3 1,144; B4 2,350; B5 932; B6 684; C 1,562). Boundaries add 2,943 disjoint IDs.', '',
    '## Completed static boundaries', '| Boundary | IDs | Clean | Candidate | Qualified/context/history | Excluded |', '| --- | ---: | ---: | ---: | ---: | ---: |']
for name in ['Management', 'Ctap2', 'SecurityDomain', 'Oath', 'WebAuthn', 'YubiOtp', 'YubiHsm', 'OpenPgp', 'Piv']:
    entry = boundaries[name]
    c = entry['counts']['dispositions']; clean = c.get('read-clean', 0); candidate = c.get('read-candidate', 0)
    excluded = c.get('read-exclusion', 0) + c.get('read-historical-exclusion', 0)
    context = entry['counts']['total'] - clean - candidate - excluded
    link = '../boundary-' + name + '.md'
    lines.append(f"| [{name}]({link}) | {entry['primary']} | {clean} | {candidate} | {context} | {excluded} |")
lines += ['', '## Depth and remaining campaign gates',
    'B4: 105 full primary-file reads plus one qualified whole Unshipped API snapshot support/type reconciliation; that unit is related context, **zero clean/all-member binding credit**.',
    'YubiOTP has **61 literal selected paths / 62 file dispositions / 324 IDs**. The extra directive-only shipped API snapshot has no assigned unit; original packet arrays are unchanged.',
    'Management B4/B6 and CTAP2 B4/B6/WebAuthn source follow-ups are reconciled with qualifications; former pending-return text remains historical, not current unfinished source work.',
    'B1 platform exclusion: B3 macOS/B4 Windows secondary source supplied, zero duplicate credit; late Windows delta/native qualification OPEN. B2 inactive scan and all other historical exclusions remain preserved.',
    'Clean/qualified/context/history/rendering/type/native records stay distinct in JSON; no final clean sampling denominator. Candidate groups are provisional/nonadditive. Candidate-associated unchanged paths require later editor/reviewer disposition.',
    'Current primary has **91 review-accepted A/C documentation paths**; their raw hashes are bound separately. Original investigation ranges are verified from pinned Git bytes with explicit original BOM/newline reconstruction, not current edited comments.',
    'Retained coordinator checks passed tokens/directives for **1,320 C# paths** and **20 API snapshots** after A/C. These are bounded preservation evidence, not all-member semantic binding, runtime qualification or final campaign validation.',
    '**Editing, broader independent review/sampling, final compiler/examples/runtime qualification and publication remain separate pending gates.** A/C scoped content acceptance is recorded; no all-module completion is inferred. PIV physical deferrals remain.',
    'Original 4,544 administrative / 7,140 outside-scope records, coverage.json, packet IDs/owners and investigator records remain unchanged.',
    '[Prior rollup/history](progress/history/eight-family-two-boundary/receipt.json): 12,022 completed / 2,022 remaining. [Current exact counts/categories/source and record hashes](../coverage-progress.json).',
    'Mechanical counts/hash joins only. No product edits, source/spec investigation, SDK build/test/native/device/formatter run, index/ref write, commit or subagent. User direction: finish Core Pass 1 publication, then stop; no Pass 2 authorization.']
(INV / 'coverage-progress.md').write_text('\n'.join(lines) + '\n')
print(json.dumps(dict(result='PASS', completed=totals, remaining=pending_counts, current_source_paths_checked=len(sources)), indent=2))
