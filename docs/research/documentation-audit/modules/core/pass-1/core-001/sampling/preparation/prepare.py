#!/usr/bin/env python3
"""One bounded records-only sampling join; no semantic verdicts or SDK execution."""
import bisect
import collections
import difflib
import hashlib
import json
import math
from pathlib import Path
import re
import subprocess

from pygments.lexers import CSharpLexer
from pygments.token import Comment

RUN = Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
OUT = RUN / 'sampling/preparation'
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
SEED = 'core-001-clean-sampling-preparation-v1'
inputs, raw_originals, normalized_originals = {}, {}, {}


def sha(b): return hashlib.sha256(b).hexdigest()


def read(p):
    b = p.read_bytes(); inputs[str(p)] = sha(b); return b


def load(p): return json.loads(read(p))


def emit(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')


def original(path):
    if path not in raw_originals:
        data = subprocess.check_output(['git', 'show', (PIN if files[path]['git_blob'] else '01a114d4') + ':' + path])
        candidates = [data]
        if sha(data) != files[path]['sha256']:
            s = data.decode('utf-8-sig').replace('\r\n', '\n')
            candidates += [(bom + s.replace('\n', newline)).encode() for bom in ['', '\ufeff'] for newline in ['\n', '\r\n']]
            current = Path(path).read_bytes()
            if sha(current) == files[path]['sha256']:
                assert current.decode('utf-8-sig').replace('\r\n', '\n') == s
                candidates.append(current)
        raw = next((b for b in candidates if sha(b) == files[path]['sha256']), None)
        assert raw is not None, ('Missing original representation', path)
        raw_originals[path] = raw.decode('utf-8-sig', errors='replace')
        normalized_originals[path] = raw_originals[path].replace('\r\n', '\n')
    return raw_originals[path]


read(Path(__file__))
progress = load(RUN / 'coverage-progress.json')
assert progress['source_baseline'] == PIN and progress['total_completed_return']['total'] == 14044 and progress['remaining']['total'] == 0
unit_rows = load(RUN / 'inventory/semantic-units.json'); units = {u['id']: u for u in unit_rows}
files = {f['path']: f for f in load(RUN / 'inventory/files.json')}
shared = {s['unit_id']: s for s in load(RUN / 'inventory/shared-sections.json')}
packets = {p.stem: load(p) for p in (RUN / 'inventory/packets').glob('*.json')}
ledgers, owners, statuses, nominee_ids = {}, {}, {}, set()
status_fields = {'A': 'status', 'B1': 'disposition', 'B2': 'status', 'B3': 'status', 'B4': 'status', 'B5': 'status', 'B6': 'disposition', 'C': 'status',
                 'Management': 'status', 'Ctap2': 'disposition', 'SecurityDomain': 'status', 'Oath': 'status', 'WebAuthn': 'disposition', 'YubiOtp': 'status', 'YubiHsm': 'status', 'OpenPgp': 'status', 'Piv': 'status'}
for group in [progress['completed_families'], progress['completed_boundaries']]:
    for owner, entry in group.items():
        ledger = load(Path(entry['ledger'])); rows = list(ledger['units'].items()) if owner == 'A' else [(r['unit_id'] if owner == 'B1' else r['id'], r) for r in ledger]
        packet_name = {'B1': 'B1-portable-lifetimes', 'B2': 'B2-linux-lifetimes', 'B3': 'B3-macos-lifetimes', 'B4': 'B4-windows-support', 'B5': 'B5-session-framing', 'B6': 'B6-wire-scp'}.get(owner, owner if owner in ['A', 'C'] else 'BOUNDARY-' + owner)
        expected = set(packets[packet_name]['unit_ids'] + packets[packet_name]['shared_read_unit_ids'])
        assert len(rows) == len(expected) and {k for k, _ in rows} == expected
        for key, row in rows:
            assert key not in statuses and units[key]['status'] == 'UNREAD'
            statuses[key], owners[key], ledgers[key] = row[status_fields[owner]], owner, entry['ledger']
            assert row['path'] == units[key]['path'] and row['start_character'] == units[key]['start_character'] and row['end_character_exclusive'] == units[key]['end_character_exclusive']
assert len(statuses) == 14044

# Only finalized nomination records are read. They annotate the complete population, never select it.
nominee_sources = {}
for owner, entry in {**progress['completed_families'], **progress['completed_boundaries']}.items():
    d = Path(entry['ledger']).parent
    name = 'sampling-nominees.json' if owner == 'A' else 'clean-nominees.json'
    p = d / name
    if p.exists():
        value = load(p)
        def ids(node):
            if isinstance(node, dict):
                for k, v in node.items():
                    if k in ['id', 'unit_id'] and isinstance(v, str) and v in units: yield v
                    else: yield from ids(v)
            elif isinstance(node, list):
                for v in node: yield from ids(v)
            elif isinstance(node, str) and node in units: yield node
        found = set(ids(value)); nominee_ids.update(found); nominee_sources[owner] = dict(record=str(p), matched_original_IDs=len(found))

batch_specs = []
for name, record, patches in [
    ('accepted-A-C', 'accepted-A-C.json', [('editing/A/revision-2/product.patch', 'A'), ('editing/C/revision-2/product.patch', 'C')]),
    ('accepted-protocols', 'accepted-protocols.json', [('editing/protocols/revision-2/full-transfer.diff', None)]),
    ('accepted-FIDO', 'accepted-FIDO.json', [('editing/FIDO-boundaries/revision-2/product.patch', None)])]:
    manifest = load(RUN / 'editing/integration' / record)
    wanted = {r['path']: r['sha256'] for r in manifest['product_files']}
    combined = []
    for patch, family in patches:
        path = RUN / patch; digest = sha(read(path))
        expected_digest = next(x['sha256'] for x in manifest['patches'] if x['family'] == family) if family else manifest['patch_sha256']
        assert digest == expected_digest
        combined.append(path)
    batch_specs.append(dict(name=name, state='ACCEPTED-INTEGRATED', patches=combined, expected_paths=set(wanted), manifest=str(RUN / 'editing/integration' / record)))

home = Path('/home/rupicola/Code/y')
pending = [
    ('transports', 'yubikit-core-docs-edit-transports', 'transports/revision-2', 'source.patch', 'static-preservation.json', 62, 'successor_full_patch_sha256'),
    ('SHARED', 'yubikit-core-docs-edit-guides', 'SHARED', 'product.patch', 'verification.json', 26, 'product_patch_sha256'),
    ('five-applets', 'yubikit-core-docs-edit-applet-boundaries', 'applet-boundaries', 'product.diff', 'static-checks.json', 37, None),
    ('OpenPgp', 'yubikit-core-docs-edit-pgp-piv', 'PGP-boundary', 'product.patch', 'static-verification.json', 17, None),
    ('Piv', 'yubikit-core-docs-edit-pgp-piv', 'PIV-boundary', 'product.patch', 'static-verification.json', 13, None),
    ('external', 'yubikit-core-docs-edit-external', 'external', 'source.patch', 'static-preservation.json', 19, 'source_patch_sha256')]
for name, wt, folder, patch, receipt, expected_paths, digest_key in pending:
    directory = home / wt / RUN / 'editing' / folder
    summary = load(directory / receipt); path = directory / patch; data = read(path)
    if digest_key: assert sha(data) == summary[digest_key]
    batch_specs.append(dict(name=name, state='PROVISIONAL-PENDING-REVIEW-OR-INTEGRATION', patches=[path], expected_count=expected_paths,
        manifest=str(directory / receipt), uncertainty='Completed editor return only; successor/final acceptance/combined diff and candidate retained-path dispositions await coordinator freeze.'))


def parse_patch(path):
    """Validate complete unified hunks against pinned lines, retain changed spans only."""
    lines = read(path).decode('utf-8-sig').splitlines(); result = {}; i = 0
    while i < len(lines):
        if not lines[i].startswith('--- '): i += 1; continue
        old_path = lines[i][4:].split('\t')[0]; new_path = lines[i + 1][4:].split('\t')[0]
        assert old_path != '/dev/null' and new_path != '/dev/null'
        target = old_path.split('/', 1)[1]; assert new_path.split('/', 1)[1] == target and target in files
        original(target); base = normalized_originals[target].splitlines()
        raw_lines = raw_originals[target].splitlines(keepends=True)
        offsets = [0]
        for line in raw_lines: offsets.append(offsets[-1] + len(line))
        spans = []; i += 2
        while i < len(lines) and not lines[i].startswith('diff --git '):
            if not lines[i].startswith('@@ '): i += 1; continue
            header = re.match(r'@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@', lines[i]); assert header, lines[i]
            start = int(header[1]); count = int(header[2]) if header[2] is not None else 1
            pos = start - 1 if count else start; old_consumed = 0; i += 1
            block_start = None; block_end = None; added = 0
            def flush():
                nonlocal block_start, block_end, added
                if block_start is not None:
                    spans.append(dict(start_character=offsets[min(block_start, len(offsets) - 1)],
                        end_character_exclusive=offsets[min(block_end, len(offsets) - 1)], old_first_line=block_start + 1,
                        old_line_count=block_end - block_start, added_lines=added,
                        kind='replacement-or-deletion' if block_end > block_start else 'insertion-point'))
                block_start = block_end = None; added = 0
            while i < len(lines) and not lines[i].startswith(('@@ ', 'diff --git ')):
                line = lines[i]
                if line.startswith('\\ No newline'): i += 1; continue
                assert line and line[0] in ' +- ', (path, line)
                kind, text = line[0], line[1:]
                if kind in ' -': assert pos < len(base) and base[pos] == text, (str(path), target, pos + 1, base[pos] if pos < len(base) else 'EOF', text)
                if kind == ' ': flush(); pos += 1; old_consumed += 1
                else:
                    if block_start is None: block_start = block_end = pos
                    if kind == '-': pos += 1; old_consumed += 1; block_end = pos
                    else: added += 1
                i += 1
            flush(); assert old_consumed == count, (path, target, old_consumed, count)
        result[target] = spans
    return result


edited = collections.defaultdict(list); batch_records = []
for batch in batch_specs:
    all_paths = set(); span_count = 0
    for patch in batch['patches']:
        parsed = parse_patch(patch); all_paths.update(parsed)
        for path, spans in parsed.items():
            for span in spans:
                edited[path].append(dict(batch=batch['name'], state=batch['state'], patch=str(patch), **span)); span_count += 1
    if 'expected_paths' in batch: assert all_paths == batch['expected_paths']
    else: assert len(all_paths) == batch['expected_count'], (batch['name'], len(all_paths))
    batch_records.append({k: v for k, v in batch.items() if k not in ['expected_paths', 'expected_count', 'patches']} | dict(
        paths=sorted(all_paths), path_count=len(all_paths), patches=[dict(path=str(p), sha256=inputs[str(p)]) for p in batch['patches']], changed_blocks=span_count))


def overlaps(unit):
    start, end = unit['start_character'], unit['end_character_exclusive']; found = []
    for span in edited.get(unit['path'], []):
        a, b = span['start_character'], span['end_character_exclusive']
        hit = start < b and a < end if a != b else start <= a <= end
        if hit: found.append(dict(batch=span['batch'], state=span['state'], kind=span['kind'], start_character=a, end_character_exclusive=b))
    return found


def classify(unit, body):
    path, category = unit['path'], unit['category']
    if path.endswith(('.svg', '.png')) or category == 'derived-diagram': return 'excluded-derived-rendering', 'Raw/diagram artifact, not eligible clean prose; source diagram/map claims retain separate units.'
    if 'historical' in files[path]['disposition'] or path.startswith('docs/plans/'):
        return 'excluded-historical', 'Inventory classifies historical/dormant material; preserve provenance without clean-current-contract credit.'
    if 'PublicAPI.' in path: return 'excluded-API-support', 'Analyzer/synthesized/declaration snapshot is support rather than prose-clean or compiler binding.'
    if category == 'source-context-and-explanations' and path.endswith('.cs'):
        tokens = list(CSharpLexer().get_tokens_unprocessed(body + '\n'))
        comments = ''.join(v for _, t, v in tokens if t in Comment)
        code = ''.join(v for _, t, v in tokens if t not in Comment)
        # Restrict exclusion to administrative preambles; declarations (including private fields) remain eligible.
        code = re.sub(r'(?m)^\s*(?:global\s+)?using\s+[^;]+;|(?m:^\s*namespace\s+[\w.]+\s*[;{])', '', code)
        administrative_comment = not re.search(r'///|<summary>|<remarks>|TODO|FIXME', comments) and (
            not comments.strip() or all(not s.strip(' /\t') or any(s.strip(' /\t').startswith(prefix) for prefix in [
                'Copyright ', 'Licensed under ', 'You may not ', 'You may obtain ', 'http://www.apache.org/',
                'Unless required ', 'distributed under ', 'WITHOUT WARRANTIES ', 'See the License ', 'limitations under ',
                'the License', 'Version 2.0', 'The above copyright', 'SPDX-']) for s in comments.splitlines()))
        if not re.sub(r'[\s{};]', '', code) and administrative_comment:
            return 'excluded-administrative-preamble', 'Exact unit contains only imports/namespace/license/structural context; no declaration, XML or substantive explanatory claim.'
    return 'candidate-eligible', 'Complete read-clean unit carries an explicit explanation or implicit declaration/state/assertion contract; private/internal/test and large/code-only units retain eligibility.'


def stratum(u, body):
    path, category = u['path'], u['category']
    if category == 'enum-value': return 'enum-values'
    if path.startswith('src/Core/src/Native/'): return 'native-interop-contracts'
    if '/tests/' in path or '.UnitTests/' in path or '.IntegrationTests/' in path: return 'tests-and-helpers'
    if category in ['narrative-section-with-complete-examples', 'structured-map-entry']: return 'narrative-examples-and-maps'
    if u['features']['xml']: return 'XML-contracts'
    if any(x in path for x in ['/Transports/', '/Protocols/', '/Sessions/', '/Devices/', 'ExchangeGuard']): return 'lifetime-protocol-internal-contracts'
    if category == 'type-contract': return 'type-declarations'
    return 'implementation-state-and-internal-contracts'


population, eligible, after_accepted_only = [], [], []
for key in sorted(statuses):
    u = units[key]; status = statuses[key]; owner = owners[key]
    row = dict(id=key, owner=owner, original_status=status, category=u['category'], ledger=ledgers[key], nominee=key in nominee_ids)
    if status != 'READ-CLEAN':
        row.update(eligibility='excluded-original-disposition', reason='Exact final ledger status is ' + status + '; no clean sampling credit.')
    else:
        text = original(u['path']); body = text[u['start_character']:u['end_character_exclusive']]
        assert sha(body.encode()) == u['range_sha256']
        state, reason = classify(u, body); row.update(eligibility=state, reason=reason, stratum=stratum(u, body), code_only=u['features']['code_only'])
        if state == 'candidate-eligible':
            hit = overlaps(u); row['edited_overlap'] = hit
            accepted_hits = [h for h in hit if h['state'] == 'ACCEPTED-INTEGRATED']
            pending_hits = [h for h in hit if h['state'] != 'ACCEPTED-INTEGRATED']
            if not accepted_hits: after_accepted_only.append(key)
            if accepted_hits: row.update(eligibility='excluded-accepted-edited-overlap', reason='Actual removed/replaced original line span or attached insertion boundary overlaps accepted integrated documentation patch.')
            elif pending_hits: row.update(eligibility='provisionally-excluded-pending-edit-overlap', reason='Completed pending-review editor patch overlaps this original unit; restore/reselect if final patch drops or moves the change.')
            else: eligible.append(key)
    population.append(row)

byid = {r['id']: r for r in population}; strata = collections.defaultdict(list)
for key in eligible: strata[(owners[key], byid[key]['stratum'])].append(key)
selected = set(); quotas = []
for (owner, role), ids in sorted(strata.items()):
    quota = min(len(ids), max(1, math.ceil(len(ids) / 10)))
    ordered = sorted(ids, key=lambda key: sha((SEED + '|' + owner + '|' + role + '|' + key).encode()))
    chosen = ordered[:quota]; selected.update(chosen)
    quotas.append(dict(owner=owner, stratum=role, eligible=len(ids), minimum_10_percent=math.ceil(len(ids)/10), proposed_selected=len(chosen), selected_ids=chosen))
for owner in sorted(set(owners.values())):
    ids = [k for k in eligible if owners[k] == owner]
    minimum = min(3, len(ids)); have = sum(owners[k] == owner for k in selected)
    for key in sorted(set(ids) - selected, key=lambda k: sha((SEED + '|owner-min|' + k).encode()))[:max(0, minimum - have)]: selected.add(key)
assert len(selected) >= math.ceil(len(eligible) / 10)

# Current primary mapping is separate from pending worktree patches; selected units remain full original units.
current_cache = {}


def map_current(u):
    path = u['path']; original(path)
    if path not in current_cache:
        raw = Path(path).read_bytes(); text = raw.decode('utf-8-sig', errors='replace').replace('\r\n','\n')
        a = normalized_originals[path].splitlines(keepends=True); b = text.splitlines(keepends=True)
        equal_lines = {}
        for tag, i, j, x, y in difflib.SequenceMatcher(a=a, b=b, autojunk=False).get_opcodes():
            if tag == 'equal': equal_lines.update({old: x + old - i for old in range(i, j)})
        current_cache[path] = dict(sha256=sha(raw), text=text, line_map=equal_lines)
    data = current_cache[path]; first, last = u['first_line'] - 1, u['last_line'] - 1
    if all(line in data['line_map'] for line in range(first, last + 1)):
        mapped = [data['line_map'][line] for line in range(first,last+1)]
        if mapped == list(range(mapped[0], mapped[0]+len(mapped))):
            return dict(state='UNCHANGED-LINES-MAPPED', first_line=mapped[0]+1, last_line=mapped[-1]+1, current_file_sha256=data['sha256'])
    return dict(state='READ-ORIGINAL-WHOLE-UNIT; boundary-line/current context needs coordinator check', current_file_sha256=data['sha256'])


selection = []
for key in sorted(selected, key=lambda k:(owners[k],byid[k]['stratum'],units[k]['path'],units[k]['start_character'])):
    u=units[key]
    selection.append(dict(id=key, owner=owners[key], stratum=byid[key]['stratum'], path=u['path'], first_line=u['first_line'],last_line=u['last_line'],
        start_character=u['start_character'],end_character_exclusive=u['end_character_exclusive'],source_sha256=u['source_sha256'],range_sha256=u['range_sha256'],
        category=u['category'],label=u['label'],current_mapping=map_current(u),nominee=key in nominee_ids,
        read_status='PROPOSED-NOT-READ-BY-SAMPLER', review_verdict='UNASSIGNED', context='Read complete unit plus enclosing owner/helper/assertions/cleanup and consequential original citation where applicable.'))


def breakdown(keys):
    result={}
    for key in keys:
        row=byid[key]; result.setdefault(row['owner'],collections.Counter())[row['category']]+=1
    return result


summary=dict(source_baseline=PIN,observed_head=subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip(),
    state='PROPOSED-POLICY-AND-SELECTION; COORDINATOR-ADJUDICATION/FINAL-DIFF-FREEZE-PENDING; NO CONTENT APPROVAL',
    original_assigned_units=len(statuses),original_READ_CLEAN=sum(v=='READ-CLEAN' for v in statuses.values()),
    eligibility_counts=dict(collections.Counter(r['eligibility'] for r in population)),
    candidate_population_after_accepted_edits_only=len(after_accepted_only),candidate_population_after_known_pending_edit_overlaps=len(eligible),
    candidate_global_minimum=math.ceil(len(eligible)/10),proposed_selected=len(selected),
    candidate_policy_options=dict(recommended='Broad meaningful declaration/state/assertion contracts including code-only, private/internal/test and large units; exclude exact administrative preambles only.',
        restore_all_160_preamble_exclusions=dict(population=len(eligible)+sum(r['eligibility']=='excluded-administrative-preamble' for r in population),
            minimum_10_percent=math.ceil((len(eligible)+sum(r['eligibility']=='excluded-administrative-preamble' for r in population))/10),
            note='Coordinator may restore borderline preambles; regenerate owner/role quotas and preserve previous selection before changes.'),
        exclude_code_only='Not recommended wholesale: 6,218 candidate units carry declaration/state/assertion contracts. Any narrower policy needs unit-specific claim-scope reasons and coordinator adjudication.'),
    eligible_by_owner_category=breakdown(eligible),selected_by_owner_category=breakdown(selected),
    owner_minimums={owner:dict(eligible=sum(owners[k]==owner for k in eligible),minimum=min(3,sum(owners[k]==owner for k in eligible)),
        ten_percent=math.ceil(sum(owners[k]==owner for k in eligible)/10),selected=sum(owners[k]==owner for k in selected)) for owner in sorted(set(owners.values()))},
    eligibility_by_owner={owner:dict(collections.Counter(r['eligibility'] for r in population if r['owner']==owner)) for owner in sorted(set(owners.values()))},
    included_code_only=sum(byid[k].get('code_only',False) for k in eligible),
    eligible_by_stratum=dict(collections.Counter(byid[k]['stratum'] for k in eligible)),
    selected_by_stratum=dict(collections.Counter(byid[k]['stratum'] for k in selected)),
    total_patch_paths=sum(b['path_count'] for b in batch_records),
    patch_path_conflicts={path:n for path,n in collections.Counter(path for batch in batch_records for path in batch['paths']).items() if n>1},
    mapped_selected_files=len(current_cache), selected_whole_unit_characters=sum(s['end_character_exclusive']-s['start_character'] for s in selection),
    nominees=dict(sources=nominee_sources,total_matched=len(nominee_ids),eligible=len(nominee_ids&set(eligible)),selected=len(nominee_ids&selected),
        unselected_eligible_ids=sorted((nominee_ids&set(eligible))-selected),meaning='Supplementary nominations only; deterministic population/stratum selection ignores nominee flags.'),
    known_pending_batches=[b['name'] for b in batch_records if b['state']!='ACCEPTED-INTEGRATED'],external_state='Completed 19-path commentary patch provisionally indexed; acceptance/integration and final candidate-retain dispositions pending.',
    original_administrative_and_outside_scope_counts=dict(collections.Counter(u['status'] for u in unit_rows if u['status']!='UNREAD')),
    final_denominator=None,finalization=['Astra adjudicates broad code/declaration contract eligibility and administrative preamble exclusions.',
        'Freeze final accepted combined product diff including all pending batches and external commentary; recompute exact original-range overlaps.',
        'Freeze every candidate-associated unchanged-path edit/retain/hold decision and independent correction/re-review; reopen eligible neighbors if dependencies change.',
        'Reconcile current source/comment mappings and rerun deterministic selection, preserving this preparation/history before any successor.',
        'Astra actually reads selected complete units/context and consequential originals; record verdicts, expand affected strata after material misses, then recompute final sampling gate.'],
    selection_policy='SHA256 fixed-seed rank independently in every owner x structural claim-role stratum; ceil(10%) each nonempty stratum, at least three per owner where available. Enum strata do not replace lifetime/native/tests/narrative/XML strata.',
    insertion_policy='Removed/replaced original lines intersect half-open source-character ranges. Pure insertions conservatively attach to both adjacent unit boundaries. Diff context lines never exclude units.',
    no_semantic_sampling_performed=True,no_execution='No SDK/test/formatter/native/device/crypto/Git mutation/subagent',input_sha256=inputs)
for path,digest in inputs.items():assert sha(Path(path).read_bytes())==digest,('Input changed during capture; stop and coordinator rebaseline',path)
for path,data in current_cache.items():assert sha(Path(path).read_bytes())==data['sha256'],('Current selected context changed',path)
if (OUT/'selection.json').exists():
    previous=json.loads((OUT/'selection.json').read_text())
    assert [s['id'] for s in previous]==[s['id'] for s in selection], 'Selection changed: preserve predecessor history and obtain coordinator reconciliation before replacing it.'
reason_catalog={row['eligibility']:row['reason'] for row in population if row['eligibility']!='excluded-original-disposition'}
reason_catalog['excluded-original-disposition']='Exact original_status supplies the exclusion: candidate, related context, historical, qualified or explicit exclusion. Original hash-bound ledger retains its detailed reason; none receives clean sampling credit.'
compact_rows=[{k:v for k,v in row.items() if k not in ['reason','ledger']} for row in population]
# One compact JSON object per line inside the array avoids repeating long reasons/ledger paths 14,044 times.
population_text=json.dumps(dict(schema='core-001-clean-population-candidate-v1',reason_catalog=reason_catalog,
    owner_ledgers={owner:entry['ledger'] for owner,entry in {**progress['completed_families'],**progress['completed_boundaries']}.items()}),indent=2)
population_text=population_text[:-2]+',\n  "units": [\n'+',\n'.join('    '+json.dumps(row,ensure_ascii=False) for row in compact_rows)+'\n  ]\n}\n'
(OUT/'population.json').write_text(population_text)
emit('selection.json',selection);emit('strata.json',quotas);emit('edit-overlaps.json',dict(batches=batch_records,changes=edited));emit('summary.json',summary)
emit('nominee-supplement.json',[dict(id=key,owner=owners[key],stratum=byid[key]['stratum'],path=units[key]['path'],
    first_line=units[key]['first_line'],last_line=units[key]['last_line'],source_sha256=units[key]['source_sha256'],range_sha256=units[key]['range_sha256'],
    state='OPTIONAL-ADDITIONAL-NOMINEE; does not replace deterministic selection or establish a verdict') for key in sorted((nominee_ids&set(eligible))-selected)])
plan=['# Proposed coordinator clean-sample reading plan','','**No sampling verdicts are recorded.** Source `'+PIN+'`; deterministic candidate selection pending final combined-diff and eligibility adjudication.','',
    'Use `python -B docs/research/documentation-audit/modules/core/pass-1/core-001/sampling/preparation/read-unit.py <selected-ID>` for an exact original whole unit. Read its enclosing context from the named source path; current primary line mapping is in selection.json.','',
    'Original source is read from pinned Git, with exact original newline/BOM hashes verified. Current accepted comments may differ; pending editor worktree patches are exclusions, not current-primary acceptance.','']
for owner in sorted(set(owners.values())):
    rows=[s for s in selection if s['owner']==owner]; stats=summary['owner_minimums'][owner]
    plan+=['## '+owner,f"Eligible candidate units {stats['eligible']}; owner >=10% threshold {stats['ten_percent']}, min {stats['minimum']}; proposed selected {len(rows)}.",'']
    for path in sorted({s['path'] for s in rows}):
        group=[s for s in rows if s['path']==path]; plan+=['### `'+path+'`']
        for s in group:
            cm=s['current_mapping'];current=f"current {cm['first_line']}–{cm['last_line']}" if cm['state']=='UNCHANGED-LINES-MAPPED' else cm['state']
            plan.append(f"- `{s['id']}` — original {s['first_line']}–{s['last_line']}; {current}; {s['stratum']}; full unit/context, verdict pending.")
        plan.append('')
(OUT/'read-plan.md').write_text('\n'.join(plan)+'\n')
print(json.dumps({k:summary[k] for k in ['original_assigned_units','original_READ_CLEAN','eligibility_counts','candidate_population_after_accepted_edits_only','candidate_population_after_known_pending_edit_overlaps','candidate_global_minimum','proposed_selected','included_code_only','owner_minimums']},indent=2))
