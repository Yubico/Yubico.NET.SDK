#!/usr/bin/env python3
"""Bounded second A expansion; reuse original patch-span helper and exact rank."""
import ast
import hashlib
import json
from pathlib import Path
import re
import subprocess

RUN = Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PREP = RUN / 'sampling/preparation'
OUT = PREP / 'expansion-A-followup'
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
SEED = 'core-001-clean-sampling-preparation-v1'
inputs, raw_originals, normalized_originals = {}, {}, {}


def sha(b): return hashlib.sha256(b).hexdigest()


def read(p):
    b = p.read_bytes(); inputs[str(p)] = sha(b); return b


def load(p): return json.loads(read(p))


def emit(name, value): (OUT / name).write_text(json.dumps(value, indent=2) + '\n')


read(Path(__file__))
population = load(PREP / 'population.json')['units']; byid = {r['id']: r for r in population}
selection = load(PREP / 'selection.json'); original_ids = {r['id'] for r in selection}
earlier_extra = load(PREP / 'expansion-A/additional-selection.json'); previous_extra_ids = {r['id'] for r in earlier_extra}
earlier_overlay = load(PREP / 'expansion-A/miss-overlay.json')
strata = load(PREP / 'strata.json')
units = {u['id']: u for u in load(RUN / 'inventory/semantic-units.json')}
files = {f['path']: f for f in load(RUN / 'inventory/files.json')}
assert len(original_ids) == 983 and len(previous_extra_ids) == 23 and not original_ids & previous_extra_ids
coordinator = RUN / 'sampling/coordinator'
read(coordinator / 'admission.md'); read(coordinator / 'read-001-verdicts.md')
read_ids = set(re.findall(r'core-001:unit:[0-9a-f]{16}', read(coordinator / 'read-001.md').decode()))
verdict_rows = {}
for number in [2, 3, 4]:
    record = load(coordinator / f'read-{number:03}-verdicts.json')
    assert sha(read(coordinator / record['reading_file'])) == record['reading_sha256']
    batch = {r['id'] for r in record['units']}; assert not batch & read_ids
    read_ids.update(batch); verdict_rows.update({r['id']: r for r in record['units']})
assert read_ids == {r['id'] for r in selection if r['owner'] == 'A'} and len(read_ids) == 176
observations = read(coordinator / 'additional-observations.md')
new_misses = {'core-001:unit:cf1e2081af8b8ac4': ('SAMPLE-A001/SAMPLE-A003', 'narrative-examples-and-maps', 'DOCUMENTATION-MISS'),
              'core-001:unit:10653b759a2209f8': ('SAMPLE-A004', 'implementation-state-and-internal-contracts', 'DEFERRED-SOURCE-OBSERVATION')}
for key, (_, role, status) in new_misses.items():
    assert key in original_ids and verdict_rows[key]['verdict'] == status
    assert byid[key]['owner'] == 'A' and byid[key]['stratum'] == role
assert b'10653b759a2209f8' in observations


def original(path):
    if path not in raw_originals:
        raw = subprocess.check_output(['git', 'show', PIN + ':' + path]); candidates = [raw]
        if sha(raw) != files[path]['sha256']:
            s = raw.decode('utf-8-sig').replace('\r\n', '\n')
            candidates += [(bom + s.replace('\n', newline)).encode() for bom in ['', '\ufeff'] for newline in ['\n', '\r\n']]
            current = Path(path).read_bytes()
            if sha(current) == files[path]['sha256'] and current.decode('utf-8-sig').replace('\r\n', '\n') == s: candidates.append(current)
        exact = next((b for b in candidates if sha(b) == files[path]['sha256']), None)
        assert exact is not None, ('Missing original representation', path)
        raw_originals[path] = exact.decode('utf-8-sig', errors='replace')
        normalized_originals[path] = raw_originals[path].replace('\r\n', '\n')
    return raw_originals[path]


roles = ['narrative-examples-and-maps', 'implementation-state-and-internal-contracts']
candidate_paths = {units[r['id']]['path'] for r in population if r['owner'] == 'A' and r.get('stratum') in roles and r['eligibility'] == 'candidate-eligible'}
current_manifest = load(RUN / 'editing/integration/current-products.json')
assert current_manifest['source_baseline'] == PIN
accepted_paths = {r['path']: r['sha256'] for r in current_manifest['product_files']}
assert len(accepted_paths) == len(current_manifest['product_files'])
for path, digest in accepted_paths.items(): assert sha(Path(path).read_bytes()) == digest, ('Accepted current path changed', path)
accepted_receipts = []
for name in current_manifest['receipt_order']:
    receipt = load(RUN / 'editing/integration' / name)
    accepted_receipts.append(dict(path='editing/integration/' + name, product_paths=len(receipt['product_files']), scope=receipt['scope']))
relevant_paths = sorted(candidate_paths & accepted_paths.keys())
command = ['git', '--no-optional-locks', 'diff', '--no-ext-diff', '--no-textconv', '--no-renames', '--unified=3', '--src-prefix=a/', '--dst-prefix=b/', PIN, '--', *relevant_paths]
patch = subprocess.check_output(command)
patch_path = OUT / 'accepted-current-A-role-scope.diff'
patch_path.write_bytes(patch)
# Reuse only the existing pure parser definition. The preparation module's top-level writer is never imported/run.
helper_bytes = read(PREP / 'prepare.py'); tree = ast.parse(helper_bytes)
helper = next(n for n in tree.body if isinstance(n, ast.FunctionDef) and n.name == 'parse_patch')
namespace = dict(read=read, original=original, files=files, normalized_originals=normalized_originals, raw_originals=raw_originals, re=re)
exec(compile(ast.Module(body=[helper], type_ignores=[]), str(PREP / 'prepare.py'), 'exec'), namespace)
spans = namespace['parse_patch'](patch_path)


def known_overlap(key):
    u = units[key]; a, b = u['start_character'], u['end_character_exclusive']
    return [span for span in spans.get(u['path'], []) if (a < span['end_character_exclusive'] and span['start_character'] < b
        if span['start_character'] != span['end_character_exclusive'] else a <= span['start_character'] <= b)]


additional, quota_records = [], []
for role in roles:
    quota = next(q for q in strata if q['owner'] == 'A' and q['stratum'] == role)
    eligible = {r['id'] for r in population if r['owner'] == 'A' and r.get('stratum') == role and r['eligibility'] == 'candidate-eligible'}
    rank = sorted(eligible, key=lambda key: sha((SEED + '|A|' + role + '|' + key).encode()))
    assert quota['selected_ids'] == rank[:quota['proposed_selected']]
    candidates = [key for key in rank if key not in original_ids | previous_extra_ids and not byid[key].get('edited_overlap')]
    skipped = [dict(id=key, path=units[key]['path'], original_rank=rank.index(key)+1, spans=known_overlap(key)) for key in candidates if known_overlap(key)]
    available = [key for key in candidates if not known_overlap(key)]
    chosen = available[:quota['proposed_selected']]; assert len(chosen) == quota['proposed_selected']
    quota_records.append(dict(owner='A', stratum=role, original_population=len(eligible), original_full_quota=quota['proposed_selected'],
        original_selected_ids=quota['selected_ids'], additional_full_quota=len(chosen), proposed_stratum_total=quota['proposed_selected']+len(chosen),
        additional_selected_ids=chosen, accepted_overlap_skips=skipped, remaining_unchosen_eligible=len(available)-len(chosen)))
    for key in chosen:
        u = units[key]; body = original(u['path'])[u['start_character']:u['end_character_exclusive']]
        assert sha(body.encode()) == u['range_sha256']
        additional.append(dict(id=key, owner='A', stratum=role, original_rank=rank.index(key)+1,
            **{field:u[field] for field in ['path','first_line','last_line','start_character','end_character_exclusive','source_sha256','range_sha256','category','label']},
            known_accepted_edited_overlap=False, original_selected_or_prior_expansion=False, read_status='ADDITIONAL-PROPOSED-UNREAD', verdict='UNASSIGNED',
            clean_credit=0, current_mapping='DEFERRED to final canonical reconciliation; original pinned unit may be read now'))
assert [q['additional_full_quota'] for q in quota_records] == [8, 11]
assert len(additional) == len({r['id'] for r in additional}) == 19
assert not {r['id'] for r in additional} & (original_ids | previous_extra_ids)

overlay = list(earlier_overlay)
for key, (finding, role, status) in new_misses.items():
    u = units[key]; body = original(u['path'])[u['start_character']:u['end_character_exclusive']]
    assert sha(body.encode()) == u['range_sha256']
    overlay.append(dict(id=key, coordinator_finding=finding, owner='A', stratum=role, path=u['path'],
        original_ledger_status='READ-CLEAN-PRESERVED-AS-HISTORY', coordinator_verdict=status, final_clean_credit=0,
        source_sha256=u['source_sha256'], range_sha256=u['range_sha256'],
        qualification='Accepted SHARED focused documentation correction; no clean credit for original miss.' if status=='DOCUMENTATION-MISS' else
            'Source-only missing deterministic X509Certificate2 disposal; runtime unchanged, no demonstrated permanent leak/native failure.',
        authority='sampling/coordinator/read-002-verdicts.json' if status=='DOCUMENTATION-MISS' else 'sampling/coordinator/additional-observations.md'))
assert len(overlay) == len({r['id'] for r in overlay}) == 5
supplementary_id = 'core-001:unit:3c7f0585447bd6d1'
assert supplementary_id not in original_ids and supplementary_id not in previous_extra_ids
supplement = dict(id=supplementary_id, path=units[supplementary_id]['path'], first_line=units[supplementary_id]['first_line'],last_line=units[supplementary_id]['last_line'],
    purpose='Supplementary DeviceChangeFeed.Complete source context only; not an original selected unit', primary_sample_increment=0, clean_credit=0,
    authority='Coordinator task handoff; accepted-sampling-A.json preserves the focused completion comment correction')
for path, digest in inputs.items(): assert sha(Path(path).read_bytes()) == digest, ('Bound input changed', path)
for path, digest in accepted_paths.items(): assert sha(Path(path).read_bytes()) == digest, ('Accepted source changed during capture', path)
emit('additional-selection.json', additional); emit('quotas.json', quota_records); emit('miss-overlay.json', overlay); emit('supplementary-context.json', supplement)
emit('accepted-overlap-inputs.json', dict(current_manifest='editing/integration/current-products.json', current_manifest_sha256=inputs[str(RUN/'editing/integration/current-products.json')],
    accepted_product_paths=len(accepted_paths), accepted_receipts=accepted_receipts, actual_diff_scope=relevant_paths,
    actual_diff_scope_meaning='All accepted current product paths capable of intersecting either A target-role candidate population; not a new final campaign-wide canonical reconciliation.',
    diff_sha256=sha(patch), diff_bytes=len(patch), command=command, pure_parser_reuse_sha256=sha(helper_bytes), original_coordinate_changed_spans=spans))
excerpts=['# Additional A narrative/internal whole units','','**No content verdicts.** Pinned source `'+PIN+'`; read complete units and relevant enclosing context.','']
for row in additional:
    body=original(row['path'])[row['start_character']:row['end_character_exclusive']]
    excerpts += ['## `'+row['id']+'`',f"`{row['path']}:{row['first_line']}–{row['last_line']}`; {row['stratum']}; original rank {row['original_rank']}.",'','````'+('csharp' if row['path'].endswith('.cs') else 'markdown'),body,'````','']
(OUT/'original-units.md').write_text('\n'.join(excerpts)+'\n')
plan=['# A follow-up expansion: narrative and internal contracts','','**Additional proposed reads only.** Original 983 selections, prior 23 XML/enum additions, all read plans and coordinator verdicts remain byte-preserved.','',
    '| Stratum | Original eligible | Original quota | Additional next-ranked | Proposed stratum total |','| --- | ---: | ---: | ---: | ---: |']
for q in quota_records: plan.append(f"| {q['stratum']} | {q['original_population']} | {q['original_full_quota']} | {q['additional_full_quota']} | {q['proposed_stratum_total']} |")
plan += ['','**19 new unique IDs**, disjoint from the original 983 and prior 23. Other owner/role counts are untouched. All new units remain unread, verdict unassigned and clean credit zero.',
    'Known accepted combined edits are filtered by exact changed original spans, not whole-file exclusions. The current-products manifest and ten receipt inputs bind the captured accepted state, including external revision 4 (37 files) and accepted A/SHARED sampling corrections.','',
    '## Reading order','[original-units.md](original-units.md) supplies every exact pinned whole unit; read enclosing source/helper/assertion/cleanup context as needed.']
for row in additional: plan.append(f"- `{row['id']}` — `{row['path']}:{row['first_line']}–{row['last_line']}`; {row['stratum']}; original rank {row['original_rank']}; **UNREAD**.")
plan += ['','## Misses and final reconciliation',
    '`miss-overlay.json` preserves the earlier three miss annotations and adds cf1e2081af8b8ac4 (SAMPLE-A001/A003) and 10653b759a2209f8 (SAMPLE-A004). All five original misses have zero final clean credit. Existing original read-clean ledger states remain history.',
    'SAMPLE-A004 is a source-only missing deterministic certificate-disposal observation. Runtime behavior is unchanged; neither a permanent leak nor a native/device failure is claimed.',
    '`3c7f0585447bd6d1` DeviceChangeFeed.Complete is supplementary context, outside the original selected set, with zero sample-count/clean increment.',
    'Original plus both expansions is 1,025 proposed distinct IDs. Removing five known misses leaves at most 1,020 potential dispositions before any other edited overlaps; this is not accepted clean credit or a final denominator.',
    'Final canonical reconciliation still binds all accepted patches, external/sampling/toolchain changes and current comment mappings. No live mapping chase or content verdict is performed here. Astra must actually read these new units and record outcomes.',
    'No product edits, source/spec investigation, SDK/build/test/native/device/formatter/crypto execution, Git mutation or subagent. Finish Core Pass 1 publication, then stop; no new implementation project.']
(OUT/'read-plan.md').write_text('\n'.join(plan)+'\n')
receipt=dict(result='STRUCTURAL-FOLLOWUP-EXPANSION-VERIFIED; NO CONTENT VERDICTS',source_baseline=PIN,original_A_selected_reads_reconciled=176,
    original_selection=983,prior_XML_enum_additions=23,new_narrative_additions=8,new_internal_additions=11,new_additions=19,
    proposed_distinct_union=1025,known_miss_overlay=5,known_miss_clean_credit=0,supplementary_sample_increment=0,other_owner_role_changes=0,
    duplicate_prior_or_known_edited_new_IDs=0,accepted_manifest_paths=len(accepted_paths),accepted_target_role_diff_paths=len(relevant_paths),
    accepted_overlap_skips={q['stratum']:len(q['accepted_overlap_skips']) for q in quota_records},final_denominator=None,
    original_source_paths_checked=len(raw_originals),current_mapping='DEFERRED to final canonical source/comment reconciliation',
    input_sha256=inputs,outputs=[dict(path=str(p),bytes=p.stat().st_size,sha256=sha(p.read_bytes())) for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='verification.json'])
emit('verification.json',receipt)
print(json.dumps({k:v for k,v in receipt.items() if k not in ['input_sha256','outputs']},indent=2))
