#!/usr/bin/env python3
"""Aggregate completed A/B1/B2/B3/C; reuse the exact four-family qualification snapshot."""
import collections
import hashlib
import json
from pathlib import Path
import subprocess

RUN = Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
INV = RUN / 'inventory'
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
PUBLISHED = '01a114d4e251e0f52b92f73393fbe1dca5d5c24e'
inputs = {}


def sha(data):
    return hashlib.sha256(data).hexdigest()


def read(path):
    data = path.read_bytes()
    inputs[str(path)] = sha(data)
    return data


def load(path):
    return json.loads(read(path))


read(Path(__file__))
HISTORY = INV / 'progress/history/four-family'
receipt = load(HISTORY / 'receipt.json')
for entry in receipt['files']:
    assert sha(read(Path(entry['snapshot_path']))) == entry['sha256']
prior = load(HISTORY / 'coverage-progress.json')
assert sha((HISTORY / 'coverage-progress.json').read_bytes()) == '0ba94491bf1791700e2136c4d0397d627a9572693049c39411aec567939ea974'
assert prior['source_baseline'] == PIN and prior['total_completed_return']['total'] == 5573
assert prior['remaining']['total'] == 8471 and set(prior['completed_families']) == {'A', 'B1', 'B2', 'B3'}
for path, digest in prior['input_sha256'].items():
    original = HISTORY / 'aggregate.py.snapshot' if path == str(Path(__file__)) else Path(path)
    assert sha(read(original)) == digest, ('Prior qualification input changed', path)
unit_rows = load(INV / 'semantic-units.json')
shared_rows = load(INV / 'shared-sections.json')
units = {u['id']: u for u in unit_rows}
shared = {s['unit_id']: s for s in shared_rows}
assert len(units) == len(unit_rows) and len(shared) == len(shared_rows)
files = {f['path']: f for f in load(INV / 'files.json')}
packets = {p.stem: load(p) for p in sorted((INV / 'packets').glob('*.json'))}
inventory_paths = subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', PUBLISHED, '--', str(INV)], text=True).splitlines()
for path in inventory_paths + [str(RUN / 'coverage.json')]:
    assert Path(path).read_bytes() == subprocess.check_output(['git', 'show', PUBLISHED + ':' + path]), path

source_cache = {}


def identity(row, unit, family):
    for key in ['path', 'start_character', 'end_character_exclusive', 'first_line', 'last_line']:
        assert row[key] == unit[key], (family, unit['id'], key)
    if family == 'A':
        assert row['inventory_source_sha256'] == unit['source_sha256']
        assert row['range_sha256'] == unit['range_sha256']
        assert row['read'] is True
    elif family == 'B1':
        assert row['read_status'] == 'READ-COMPLETE'
        assert row['original_source_sha256'] == row['current_source_sha256'] == unit['source_sha256']
        assert row['original_range_sha256'] == row['current_range_sha256'] == unit['range_sha256']
    elif family == 'C':
        assert row['read_complete'] is True
        assert row['source_sha256'] == unit['source_sha256']
        assert row['range_sha256'] == unit['range_sha256']
    else:
        assert row['source_sha256'] == row['current_source_sha256'] == unit['source_sha256']
        assert row['range_sha256'] == row['current_range_sha256'] == unit['range_sha256']
        if family == 'B2': assert row['source_baseline'] == PIN
    path = unit['path']
    assert unit['source_sha256'] == files[path]['sha256'], (family, unit['id'])
    # Old ledgers are byte-identical to the qualified snapshot; rejoin their hashes/ranges without
    # repeating its source qualification. New C ranges receive current raw/coordinate hashing.
    if family != 'C': return
    if path not in source_cache:
        raw = Path(path).read_bytes()
        assert sha(raw) == files[path]['sha256'], path
        pinned = subprocess.check_output(['git', 'show', PIN + ':' + path])
        assert raw == pinned or raw.replace(b'\r\n', b'\n') == pinned.replace(b'\r\n', b'\n'), path
        source_cache[path] = raw.decode('utf-8-sig', errors='replace')
    body = source_cache[path][unit['start_character']:unit['end_character_exclusive']]
    assert sha(body.encode()) == unit['range_sha256'], (family, unit['id'])


def counts(ids, dispositions):
    result = dict(total=len(ids), scope={}, category={}, primary_shared={}, scope_by_assignment={},
                  dispositions=dict(collections.Counter(dispositions[k] for k in sorted(ids))))
    for k in sorted(ids):
        unit = units[k]
        scope = 'Core' if unit['path'].startswith('src/Core/') else 'external'
        result['scope'][scope] = result['scope'].get(scope, 0) + 1
        category = unit['category']
        result['category'].setdefault(category, collections.Counter())[dispositions[k]] += 1
        kind = 'shared' if k in shared else 'primary'
        result['primary_shared'].setdefault(kind, collections.Counter())[dispositions[k]] += 1
        result['scope_by_assignment'].setdefault(kind, collections.Counter())[scope] += 1
    return result


families, consumed = {}, {}
names = {'A': 'A', 'B1': 'B1-portable-lifetimes', 'B2': 'B2-linux-lifetimes', 'B3': 'B3-macos-lifetimes', 'C': 'C'}
local_groups = {'A': 21, 'B1': 12, 'B2': 6, 'B3': 9}
for family, packet_name in names.items():
    report = RUN / ('investigation-' + family + '.md')
    assert PIN.encode() in read(report)
    directory = RUN / 'investigation' / family
    ledger_path = directory / ('unit-results.json' if family == 'A' else 'unit-dispositions.json')
    ledger = load(ledger_path)
    if family == 'A':
        assert ledger['summary']['source_baseline'] == PIN
        rows = list(ledger['units'].items())
    else:
        summary = load(directory / ('final-summary.json' if family == 'C' else 'coverage-summary.json'))
        baseline_key = {'B1': 'baseline', 'B2': 'source_baseline', 'B3': 'source_pin', 'C': 'baseline'}[family]
        assert summary[baseline_key] == PIN
        rows = [(row['unit_id'] if family == 'B1' else row['id'], row) for row in ledger]
        if family == 'C':
            assert summary['packet_sha256'] == inputs[str(INV / 'packets/C.json')]
            assert summary['inventory_sha256'] == inputs[str(INV / 'semantic-units.json')]
            assert summary['primary_files_read_completely'] == summary['primary_file_count'] == 104
            assert len({row['path'] for _, row in rows if row['assignment'] == 'primary'}) == 104
            assert summary['accepted_ids_allocated'] == 0
            assert dict(collections.Counter(row['status'] for _, row in rows)) == summary['unit_status_counts']
            for assignment in ['primary', 'shared-read']:
                assert dict(collections.Counter(row['status'] for _, row in rows if row['assignment'] == assignment)) == summary['unit_status_counts_by_assignment'][assignment]
            local_groups['C'] = summary['local_candidate_groups']
    packet = packets[packet_name]
    expected_primary, expected_shared = set(packet['unit_ids']), set(packet['shared_read_unit_ids'])
    assert len(packet['unit_ids']) == len(expected_primary) and len(packet['shared_read_unit_ids']) == len(expected_shared)
    expected = expected_primary | expected_shared
    assert not expected_primary & expected_shared
    assert len(rows) == len(expected) and {k for k, _ in rows} == expected
    assert not expected & consumed.keys(), family
    own = {}
    for key, row in rows:
        unit = units[key]
        assert unit['status'] == 'UNREAD'
        if key in expected_shared:
            assert shared[key]['investigation_owner'] == (family if family in ['A', 'C'] else 'B')
            assert shared[key]['investigation_stage'] == packet_name
        else: assert unit['stage'] == packet_name
        identity(row, unit, family)
        if family == 'A':
            status = {'READ-CLEAN': 'read-clean', 'CANDIDATE': 'read-candidate'}[row['status']]
            assert row['ownership'] == ('assigned-shared-range' if key in expected_shared else 'primary')
        elif family == 'B1':
            status = {'READ-CLEAN': 'read-clean', 'READ-CANDIDATE': 'read-candidate', 'READ-EXCLUSION': 'read-exclusion'}[row['disposition']]
            assert row['ownership'] == ('B1-shared-reader; future editor SHARED' if key in expected_shared else 'B1-primary')
        elif family == 'C':
            status = {'READ-CLEAN': 'read-clean', 'READ-CANDIDATE': 'read-candidate',
                      'READ-CANDIDATE-CONTEXT': 'read-candidate-context',
                      'READ-HISTORICAL-EXCLUSION': 'read-historical-exclusion',
                      'READ-HISTORICAL-CONTEXT': 'read-historical-context'}[row['status']]
            assert row['assignment'] == ('shared-read' if key in expected_shared else 'primary')
        else:
            status = {'READ-CLEAN': 'read-clean', 'READ-CANDIDATE': 'read-candidate', 'READ-EXCLUSION': 'read-exclusion'}[row['status']]
            kind = row['assignment_kind'] if family == 'B2' else row['ownership']
            expected_kind = ('shared-read' if key in expected_shared else 'primary') if family == 'B2' else ('ASSIGNED-SHARED-READ' if key in expected_shared else 'PRIMARY')
            assert kind == expected_kind, (family, key, kind)
        own[key] = status
    consumed.update(own)
    families[family] = dict(packet=str(INV / 'packets' / (packet_name + '.json')), report=str(report), ledger=str(ledger_path),
        primary=len(expected_primary), shared=len(expected_shared), local_candidate_groups=local_groups[family],
        counts=counts(set(own), own), independent_review='PENDING', editing='PENDING', SDK_runtime_verification='PENDING')
    if family != 'C':
        old_counts = prior['completed_families'][family]['counts']
        assert {key: families[family]['counts'][key] for key in old_counts} == old_counts
    else:
        c_ids = set(own)
        c_exclusions = [row for _, row in rows if row['status'] == 'READ-HISTORICAL-EXCLUSION']
        nominees = load(directory / 'clean-nominees.json')
        assert len(nominees) == len({row['id'] for row in nominees}) == summary['clean_sample_nominees'] == 207
        for nominee in nominees:
            assert nominee['id'] in c_ids and own[nominee['id']] == 'read-clean'
            assert nominee['source_sha256'] == units[nominee['id']]['source_sha256'] and nominee['range_sha256'] == units[nominee['id']]['range_sha256']
        unchanged = load(directory / 'unchanged-paths.json')
        assert len({row['path'] for row in unchanged}) == 85
        families['C']['sampling_nominees'] = dict(count=207, status='NOMINATED-ONLY; independent sampling pending', record=str(directory / 'clean-nominees.json'))
        families['C']['candidate_associated_paths'] = dict(unique_paths=85, candidate_path_rows=len(unchanged), record=str(directory / 'unchanged-paths.json'), future_editor_reviewer='PENDING')

assert len(consumed.keys() - c_ids) == prior['total_completed_return']['total']
assert len(c_ids) == 1562 and len(consumed) == 7135
assert families['C']['counts']['dispositions'] == {'read-candidate': 305, 'read-candidate-context': 639, 'read-clean': 611, 'read-historical-exclusion': 3, 'read-historical-context': 4}

expected_totals = {'A': (2006, 1855, 151, 0), 'B1': (1204, 1104, 99, 1), 'B2': (1219, 1187, 31, 1), 'B3': (1144, 1087, 57, 0)}
for name, expected in expected_totals.items():
    c = families[name]['counts']
    assert (c['total'], c['dispositions'].get('read-clean', 0), c['dispositions'].get('read-candidate', 0), c['dispositions'].get('read-exclusion', 0)) == expected

secondary = load(RUN / 'investigation/B3/secondary-handoffs.json')
for row in secondary:
    identity({**row, 'current_source_sha256': row['source_sha256'], 'current_range_sha256': row['range_sha256']}, units[row['id']], 'B3')
    assert row['primary_count_increment'] == 0
platform = next(row for row in secondary if row['id'] == 'core-001:unit:9fe6da15d252bcd2')
assert platform['secondary_disposition'] == 'SECONDARY-READ-CLEAN-MACOS-CLAUSE-ONLY; Windows clauses remain B4'
assert consumed[platform['id']] == 'read-exclusion'
historical = 'core-001:unit:a9c5253178fb280d'
assert consumed[historical] == 'read-exclusion'

unchanged_maps = ['investigation/A/file-dispositions.json', 'investigation/A/findings.json', 'investigation/B1/candidates.md',
                  'investigation/B2/candidate-index.json', 'investigation/B3/candidate-associated-paths.json', 'investigation/C/unchanged-paths.json']
for relative in unchanged_maps: read(RUN / relative)
universe = {k for k, u in units.items() if u['status'] == 'UNREAD'}
remaining = universe - consumed.keys()
remaining_packets = {}
for name in ['B4-windows-support', 'B5-session-framing', 'B6-wire-scp'] + sorted(n for n in packets if n.startswith('BOUNDARY-')):
    assignment = set(packets[name]['unit_ids']) | set(packets[name]['shared_read_unit_ids'])
    assert assignment <= remaining
    remaining_packets[name] = dict(state='IN-PROGRESS' if name in ['B4-windows-support', 'B5-session-framing', 'B6-wire-scp'] else 'UNEXECUTED',
                                   primary=len(packets[name]['unit_ids']), shared=len(packets[name]['shared_read_unit_ids']),
                                   counts=counts(assignment, dict.fromkeys(assignment, 'pending-completed-return')))
pending_ids = [k for name in remaining_packets for k in packets[name]['unit_ids'] + packets[name]['shared_read_unit_ids']]
assert len(pending_ids) == len(set(pending_ids)) and set(pending_ids) == remaining
for path, digest in inputs.items(): assert sha(Path(path).read_bytes()) == digest, ('Input changed during capture', path)
for path in source_cache: assert sha(Path(path).read_bytes()) == files[path]['sha256'], ('Source changed during capture', path)
totals = counts(set(consumed), consumed)
remaining_counts = counts(remaining, dict.fromkeys(remaining, 'pending-completed-return'))
assert totals['total'] + remaining_counts['total'] == len(universe) == 14044
assert remaining_counts['total'] == 6909
result = dict(schema='core-001-coverage-progress-v2', source_baseline=PIN,
    observed_record_head=subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip(),
    measure_definitions=dict(total_completed_return='Distinct IDs with final investigator read dispositions, including explicit scoped/historical exclusions; not accepted clean prose or runtime verification',
        remaining='IDs awaiting a completed investigator return; the historical 8,471 was this remaining quantity'),
    completed_families=families, total_completed_return=totals,
    remaining=remaining_counts, remaining_packets=remaining_packets,
    prior_four_family_snapshot=dict(receipt=str(HISTORY / 'receipt.json'), receipt_sha256=inputs[str(HISTORY / 'receipt.json')],
        progress=str(HISTORY / 'coverage-progress.json'), progress_sha256=inputs[str(HISTORY / 'coverage-progress.json')],
        completed_units=5573, remaining_units=8471, original_qualification='Reused only after exact unchanged ledger/packet/inventory input hash checks'),
    original_partition=dict(reading_population=len(universe), Core=9020, external=5024,
        administrative_context=sum(u['status'] == 'ADMINISTRATIVE-CONTEXT' for u in units.values()),
        outside_scope=sum(u['status'] == 'OUT-OF-SCOPE' for u in units.values())),
    qualification=dict(semantic_state='Completed investigator read/dispositions only', independent_review='PENDING', editing='PENDING',
        SDK_runtime_verification='PENDING', accepted_bug_count=None, local_candidate_groups_nonadditive=sum(local_groups.values()),
        final_clean_sampling_denominator=None, sampling='PENDING dependency/edited-path reconciliation; type, native, rendering, historical and context strata stay explicit',
        C='COMPLETE final 1,562-ID return only; historical partial checkpoints supply no additional count',
        active_investigators=['B4-windows-support', 'B5-session-framing', 'B6-wire-scp'], partial_progress_credited=0, boundary_packets='All nine UNEXECUTED',
        Windows='Late user handoff pending; no unfinished worktree inspection', PIV='Physical/touch/hotplug deferral retained'),
    exclusions=[dict(id=platform['id'], primary_owner='B1', state='read-exclusion/partial-platform-scope',
        portable='B1 source disposition', macOS='B3 secondary source disposition supplied; independent review/native execution pending', Windows='B4 PENDING', increment=0),
        dict(id=historical, primary_owner='B2', state='read-exclusion/historical', reason='Complete inactive commented LinuxUdevScan read and preserved; current enumeration is LinuxHidInterface.GetList; no source removed')]
        + [dict(id=row['id'], primary_owner='C', state='read-historical-exclusion', path=row['path'],
                reason='Complete inactive historical source read and preserved; no active API/runtime claim or cleanup deletion') for row in c_exclusions],
    secondary_handoffs=dict(record='investigation/B3/secondary-handoffs.json', additional_primary_or_shared_count=0,
        B4_instruction_unit='core-001:unit:7a72c68ab4742b48; B3 callback-paragraph candidate supplied, B4 parent remains pending'),
    candidate_associated_unchanged_path_maps=unchanged_maps, input_sha256=inputs,
    structural_verification=dict(result='PASS', completed_IDs_consumed_exactly_once=len(consumed), missing_or_extra_assigned_IDs=0,
        original_inventory_files_unchanged=len(inventory_paths), source_and_range_hashes='All five ledgers joined exactly to original source/range hashes and coordinates; old four-family qualification reused by exact input hashes; C current raw/range hashing and pinned CRLF/LF identity checked',
        current_C_source_paths_checked=len(source_cache), prior_four_family_qualified_source_paths=prior['structural_verification']['current_source_paths_checked'],
        overlaps=0, no_completed_administrative_or_outside_scope_units=True))
clean_renderings = [k for k, v in consumed.items() if v == 'read-clean' and units[k]['path'].endswith(('.png', '.svg'))]
result['qualification']['read_clean_rendering_units_not_clean_prose'] = len(clean_renderings)
result['qualification']['read_native_CSharp_units_not_platform_acceptance'] = sum(units[k]['path'].startswith('src/Core/src/Native/') for k in consumed)
(RUN / 'coverage-progress.json').write_text(json.dumps(result, indent=2) + '\n')
lines = ['# Core structural coverage progress', '', f'Source: `{PIN}`. Investigator read dispositions only; independent review, editing and SDK/runtime verification remain **PENDING**.', '',
         '## Completed assigned read dispositions',
         '| Family | Primary | Shared | Read-clean | Direct candidate | Candidate context | Exclusions | Historical context |', '| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |']
for family in names:
    f = families[family]; c = f['counts']['dispositions']
    lines.append(f"| [{family}](../investigation-{family}.md) | {f['primary']} | {f['shared']} | {c.get('read-clean', 0)} | {c.get('read-candidate', 0)} | {c.get('read-candidate-context', 0)} | {c.get('read-exclusion', 0) + c.get('read-historical-exclusion', 0)} | {c.get('read-historical-context', 0)} |")
primary = sum(f['primary'] for f in families.values())
shared_count = sum(f['shared'] for f in families.values())
dc = totals['dispositions']
lines += ['', f"**Completed: {totals['total']:,} distinct IDs = {primary:,} primary + {shared_count:,} shared.** {sum(local_groups.values())} provisional local groups are nonadditive, not accepted bugs.",
          f"Read states: {dc['read-clean']:,} clean; {dc['read-candidate']:,} direct candidate; {dc['read-candidate-context']:,} related context; two scoped/historical B exclusions, three C historical exclusions and four C historical-context units.",
          f"Core completed: **{totals['scope']['Core']:,} / 9,020**; external completed: **{totals['scope']['external']:,} / 5,024**. C's 207 nominees are nominations only; 85 unique candidate-associated paths retain future review.", '',
          '## Remaining units awaiting completed returns',
          f"**{remaining_counts['total']:,} remaining**: {remaining_counts['scope']['Core']:,} Core + {remaining_counts['scope']['external']:,} external. B4 2,350; B5 932; B6 684 (three active). Nine unexecuted boundary packets: 2,943.",
          'No B4/B5/B6 partial records are read or credited. Exact ID unions have zero intersections, missing IDs or extra IDs.', '',
          'B1 platform notes `9fe6da15d252bcd2` retain a partial-scope exclusion: B3 supplies macOS source review; Windows remains B4 pending. No duplicate count or whole-platform closure.',
          'B2 `a9c5253178fb280d` retains its inactive historical-scan exclusion and original source. Both secondary B3 handoffs add zero primary/shared units.',
          'Read-clean counts include type/native/context/historical/rendering strata: **no final clean-prose or sampling denominator is claimed**. The 4,544 administrative and 7,140 outside-scope records remain separate.',
          'Candidate-associated unchanged-path maps still require later editor/reviewer disposition. Windows late work and PIV physical deferrals remain pending.', '',
          '[Prior four-family snapshot](progress/history/four-family/receipt.json) preserves exact bytes/hashes: **5,573 completed; 8,471 remaining**, not completed coverage.',
          '[Current keyed totals, categories, hashes and five final-ledger references](../coverage-progress.json) join all source/range identities; exact unchanged old qualification is reused, with current C hash checks.',
          'The original 38 inventory files, original coverage, packet IDs and investigator records remain unchanged. No source investigation, SDK build/test, formatter, device command, index/ref write, commit or subagent ran.']
(INV / 'coverage-progress.md').write_text('\n'.join(lines) + '\n')
print(json.dumps(dict(result='PASS', total=result['total_completed_return'], remaining=result['remaining']['scope'], current_C_source_paths_checked=len(source_cache)), indent=2))
