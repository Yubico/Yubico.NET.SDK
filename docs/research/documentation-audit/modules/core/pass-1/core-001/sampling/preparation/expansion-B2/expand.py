#!/usr/bin/env python3
"""One B2 material-miss expansion, separate eligibility replacements and history overlay."""
import ast
import hashlib
import json
import math
from pathlib import Path
import re
import subprocess

RUN = Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PREP = RUN / 'sampling/preparation'
OUT = PREP / 'expansion-B2'
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
original_selection = load(PREP / 'selection.json'); original_ids = {r['id'] for r in original_selection}
prior_additions = load(PREP / 'expansion-A/additional-selection.json') + load(PREP / 'expansion-A-followup/additional-selection.json')
prior_ids = original_ids | {r['id'] for r in prior_additions}
assert len(original_ids) == 983 and len(prior_ids) == 1025
strata = load(PREP / 'strata.json')
units = {u['id']: u for u in load(RUN / 'inventory/semantic-units.json')}
files = {f['path']: f for f in load(RUN / 'inventory/files.json')}
verdicts = {}
for owner, expected in [('B1', 111), ('B2', 120)]:
    record = load(RUN / 'sampling/coordinator' / ('read-' + owner + '-verdicts.json'))
    assert sha(read(RUN / 'sampling/coordinator' / ('read-' + owner + '.md'))) == record['reading_sha256']
    assert len(record['units']) == expected and {r['id'] for r in record['units']} == {r['id'] for r in original_selection if r['owner'] == owner}
    for row in record['units']:
        assert row['range_sha256'] == units[row['id']]['range_sha256']
        verdicts[row['id']] = row
history_clarification = read(RUN / 'investigation/B1/sampling-history-clarification.md')
miss_keys = ['core-001:unit:2ab3cc1db2ed4269', 'core-001:unit:7a90c15f7fb4a767', 'core-001:unit:5c0f333d06ef9126']
invalid_keys = ['core-001:unit:9715fd23161cbcd7', 'core-001:unit:e7b21994e1483471', 'core-001:unit:c7edeed9fe6d2e6c']
for key in miss_keys: assert verdicts[key]['verdict'] == 'DOCUMENTATION-MISS' and byid[key]['owner'] == 'B2' and byid[key]['stratum'] == 'XML-contracts'
for key in invalid_keys: assert verdicts[key]['verdict'] in ['INELIGIBLE-ADMINISTRATIVE', 'INELIGIBLE-DORMANT-ILLUSTRATION']
measurement_key = 'core-001:unit:f1d4372202bdaa9b'
assert measurement_key in verdicts and measurement_key.encode() in history_clarification
phase_keys = sorted(key for key, row in verdicts.items() if byid[key]['owner'] == 'B2' and row['path'].startswith('docs/research/'))
assert len(phase_keys) == 6
history_keys = {measurement_key, *phase_keys}


def original(path):
    if path not in raw_originals:
        pinned = subprocess.check_output(['git', 'show', PIN + ':' + path]); candidates = [pinned]
        if sha(pinned) != files[path]['sha256']:
            text = pinned.decode('utf-8-sig').replace('\r\n', '\n')
            candidates += [(bom + text.replace('\n', newline)).encode() for bom in ['', '\ufeff'] for newline in ['\n', '\r\n']]
            current = Path(path).read_bytes()
            if sha(current) == files[path]['sha256'] and current.decode('utf-8-sig').replace('\r\n', '\n') == text: candidates.append(current)
        exact = next((b for b in candidates if sha(b) == files[path]['sha256']), None)
        assert exact is not None, ('Missing original representation', path)
        raw_originals[path] = exact.decode('utf-8-sig', errors='replace')
        normalized_originals[path] = raw_originals[path].replace('\r\n', '\n')
    return raw_originals[path]


roles = {('B2', 'XML-contracts'), ('B1', 'lifetime-protocol-internal-contracts'), ('B2', 'native-interop-contracts'),
         ('B1', 'narrative-examples-and-maps'), ('B2', 'narrative-examples-and-maps')}
candidate_paths = {units[r['id']]['path'] for r in population if (r['owner'], r.get('stratum')) in roles and r['eligibility'] == 'candidate-eligible'}
manifest = load(RUN / 'editing/integration/current-products.json'); assert manifest['source_baseline'] == PIN
accepted = {r['path']: r['sha256'] for r in manifest['product_files']}
for path, digest in accepted.items(): assert sha(Path(path).read_bytes()) == digest, ('Accepted current product changed', path)
for name in manifest['receipt_order']: load(RUN / 'editing/integration' / name)
relevant = sorted(candidate_paths & accepted.keys())
command = ['git', '--no-optional-locks', 'diff', '--no-ext-diff', '--no-textconv', '--no-renames', '--unified=3', '--src-prefix=a/', '--dst-prefix=b/', PIN, '--', *relevant]
patch = subprocess.check_output(command); patch_path = OUT / 'accepted-current-B1-B2-role-scope.diff'; patch_path.write_bytes(patch)
helper_bytes = read(PREP / 'prepare.py'); tree = ast.parse(helper_bytes)
helper = next(n for n in tree.body if isinstance(n, ast.FunctionDef) and n.name == 'parse_patch')
namespace = dict(read=read, original=original, files=files, normalized_originals=normalized_originals, raw_originals=raw_originals, re=re)
exec(compile(ast.Module(body=[helper], type_ignores=[]), str(PREP / 'prepare.py'), 'exec'), namespace)
spans = namespace['parse_patch'](patch_path)


def overlap(key):
    u = units[key]; a, b = u['start_character'], u['end_character_exclusive']
    return [s for s in spans.get(u['path'], []) if (a < s['end_character_exclusive'] and s['start_character'] < b
        if s['start_character'] != s['end_character_exclusive'] else a <= s['start_character'] <= b)]


def rank_for(owner, role):
    eligible = {r['id'] for r in population if r['owner'] == owner and r.get('stratum') == role and r['eligibility'] == 'candidate-eligible'}
    return sorted(eligible, key=lambda key: sha((SEED + '|' + owner + '|' + role + '|' + key).encode()))


def row_for(key, purpose, rank):
    u = units[key]; body = original(u['path'])[u['start_character']:u['end_character_exclusive']]
    assert sha(body.encode()) == u['range_sha256']
    return dict(id=key, owner=byid[key]['owner'], stratum=byid[key]['stratum'], purpose=purpose, original_rank=rank.index(key)+1,
        **{field:u[field] for field in ['path','first_line','last_line','start_character','end_character_exclusive','source_sha256','range_sha256','category','label']},
        known_edited_overlap=False, read_status='PROPOSED-UNREAD', verdict='UNASSIGNED', clean_credit=0,
        future_B2_correction_path=u['path'] in {units[k]['path'] for k in miss_keys},
        history_sensitive_context=u['path'].startswith('docs/research/'),
        current_mapping='DEFERRED until final canonical reconciliation; original pinned unit may be read now')


used = set(prior_ids)
q = next(q for q in strata if q['owner'] == 'B2' and q['stratum'] == 'XML-contracts')
rank = rank_for('B2', 'XML-contracts'); assert q['selected_ids'] == rank[:q['proposed_selected']]
available = [k for k in rank if k not in used and k not in set(miss_keys+invalid_keys) and not byid[k].get('edited_overlap') and not overlap(k)]
chosen = available[:q['proposed_selected']]; assert len(chosen) == 17
expansion = [row_for(k, 'MATERIAL-MISS-FULL-ORIGINAL-XML-QUOTA', rank) for k in chosen]; used.update(chosen)
replacement_rows, replacement_quotas = [], []
for owner, role in [('B1', 'lifetime-protocol-internal-contracts'), ('B2', 'native-interop-contracts')]:
    quota = next(q for q in strata if q['owner'] == owner and q['stratum'] == role); rank = rank_for(owner, role)
    invalid = {k for k in invalid_keys if byid[k]['owner'] == owner and byid[k]['stratum'] == role}
    edited = {k for k in rank if overlap(k)}
    denominator = set(rank) - invalid - edited
    original_valid = set(quota['selected_ids']) - invalid - edited
    minimum = max(1, math.ceil(len(denominator)/10)); needed = max(0, minimum-len(original_valid))
    available = [k for k in rank if k not in used and k in denominator and not byid[k].get('edited_overlap')]
    keys = available[:needed]; assert len(keys) == needed
    replacement_rows += [row_for(k, 'INELIGIBILITY-REPLACEMENT-NOT-MATERIAL-EXPANSION', rank) for k in keys]; used.update(keys)
    replacement_quotas.append(dict(owner=owner,stratum=role,original_population=len(rank),original_sample_quota=quota['proposed_selected'],
        adjudicated_ineligible_ids=sorted(invalid),current_accepted_overlap_ids=sorted(edited),working_population_after_explicit_exclusions=len(denominator),
        working_10_percent_minimum=minimum,original_valid_selected=len(original_valid),required_replacements=needed,replacement_ids=keys,
        qualification='Candidate role-level calculation only; final canonical population/denominator and actual replacement reads remain pending.'))

# Historical report scope is not silently promoted to current-platform clean credit. Offer separate
# conditional quota repairs if Astra withholds these historical units from current-contract sampling.
conditional_rows, history_quotas = [], []
for owner in ['B1', 'B2']:
    role='narrative-examples-and-maps'; quota=next(q for q in strata if q['owner']==owner and q['stratum']==role); rank=rank_for(owner,role)
    history={k for k in history_keys if byid[k]['owner']==owner}; edited={k for k in rank if overlap(k)}
    denominator=set(rank)-history-edited; valid=set(quota['selected_ids'])-history-edited
    minimum=max(1,math.ceil(len(denominator)/10)); needed=max(0,minimum-len(valid))
    available=[k for k in rank if k not in used and k in denominator and not byid[k].get('edited_overlap')]
    keys=available[:needed]; assert len(keys)==needed
    conditional_rows += [row_for(k,'CONDITIONAL-HISTORY-ELIGIBILITY-REPLACEMENT; COORDINATOR-ADJUDICATION-REQUIRED',rank) for k in keys]; used.update(keys)
    history_quotas.append(dict(owner=owner,stratum=role,qualified_history_ids=sorted(history),explicit_accepted_overlap_ids=sorted(edited),
        conditional_population=len(denominator),conditional_minimum=minimum,valid_original_if_history_withheld=len(valid),
        conditional_replacement_need=needed,proposed_conditional_ids=keys,
        decision='Astra decides whether exact historical-report claim review qualifies for this narrative denominator. These candidates have no read/clean credit; research-path context can itself need historical-scope adjudication.'))

miss_overlay=[]
for key in miss_keys:
    u=units[key]
    miss_overlay.append(dict(id=key,owner='B2',stratum='XML-contracts',coordinator_finding='SAMPLE-B2-002' if key.endswith('5c0f333d06ef9126') else 'SAMPLE-B2-001',
        original_ledger_status='READ-CLEAN-PRESERVED-AS-HISTORY',coordinator_verdict=verdicts[key]['verdict'],coordinator_note=verdicts[key]['note'],
        final_clean_credit=0,source_sha256=u['source_sha256'],range_sha256=u['range_sha256'],
        correction='Transport editor verifying/correcting comments only; final accepted correction overlap/review remains pending.'))
ineligible_overlay=[dict(id=k,owner=byid[k]['owner'],stratum=byid[k]['stratum'],coordinator_verdict=verdicts[k]['verdict'],reason=verdicts[k]['note'],
    original_ledger_status='READ-CLEAN-PRESERVED-AS-HISTORY',final_clean_credit=0,kind='ELIGIBILITY-ADJUDICATION; NOT A MATERIAL DOCUMENTATION MISS') for k in invalid_keys]
history_overlay=[dict(id=measurement_key,owner='B1',path=units[measurement_key]['path'],status='HISTORICAL-REPORT-SUPPORT; FINAL-ELIGIBILITY-ADJUDICATION-PENDING',
    authority='investigation/B1/sampling-history-clarification.md',report_commit='64efe84487143235a42121db962c9328cfec0aa8',
    report_path='docs/plans/session-contention/phase1-findings.md',report_blob='738d5c433b5583aab2590b8b72391b3d2b01f796',
    report_sha256='fb602ed05f1fe4f9a6253d72195756879995df9b3e31e4a3ef36815fad78851b',
    scope='Astra/owner verified historical report scope; original raw captures/harness missing. No fresh physical verification, universal state claim or raw-capture attestation. Not independently re-investigated here.',
    clean_credit_by_this_preparer=0)]
history_overlay += [dict(id=k,owner='B2',path=units[k]['path'],first_line=units[k]['first_line'],last_line=units[k]['last_line'],
    status='READ-AS-HISTORICAL-RESEARCH-PHASE-EVIDENCE; NOT CURRENT-PLATFORM-QUALIFICATION',original_verdict=verdicts[k]['verdict'],
    original_ledger_status='READ-CLEAN-PRESERVED-AS-HISTORY',clean_credit_by_this_preparer=0,
    qualification='Preserve exact unit/phase/recorded evidence scope. No whole-file history renaming or current native/device acceptance; Astra adjudicates final narrative sampling eligibility.') for k in phase_keys]
all_new=expansion+replacement_rows+conditional_rows
assert len(all_new)==len({r['id'] for r in all_new}) and not {r['id'] for r in all_new}&prior_ids
for path,digest in inputs.items(): assert sha(Path(path).read_bytes())==digest,('Bound input changed',path)
for path,digest in accepted.items(): assert sha(Path(path).read_bytes())==digest,('Accepted current product changed during capture',path)
emit('additional-selection.json',expansion);emit('replacement-selection.json',replacement_rows);emit('conditional-history-replacements.json',conditional_rows)
emit('miss-overlay.json',miss_overlay);emit('ineligibility-overlay.json',ineligible_overlay);emit('history-qualification-overlay.json',history_overlay)
emit('quotas.json',dict(material_XML=dict(owner='B2',stratum='XML-contracts',original_population=q['eligible'],original_full_quota=17,additional_full_quota=17,proposed_total=34,
    original_misses_with_zero_final_credit=3),eligibility_replacements=replacement_quotas,conditional_history_replacements=history_quotas))
emit('accepted-overlap-inputs.json',dict(manifest='editing/integration/current-products.json',manifest_sha256=inputs[str(RUN/'editing/integration/current-products.json')],
    accepted_paths=len(accepted),target_role_paths=relevant,command=command,diff_sha256=sha(patch),diff_bytes=len(patch),pure_parser_sha256=sha(helper_bytes),original_changed_spans=spans))
excerpts=['# B2 expansion and separate replacement original units','','**No content verdicts.** Source `'+PIN+'`. Each excerpt is one exact original unit; read its named enclosing source context as needed.','']
for row in all_new:
    body=original(row['path'])[row['start_character']:row['end_character_exclusive']]
    excerpts += ['## `'+row['id']+'`',f"{row['purpose']}; `{row['path']}:{row['first_line']}–{row['last_line']}`; original rank {row['original_rank']}.",'','````'+('csharp' if row['path'].endswith('.cs') else 'markdown'),body,'````','']
(OUT/'original-units.md').write_text('\n'.join(excerpts)+'\n')
plan=['# B2 XML expansion and separate B1/B2 eligibility repairs','','**Additional proposed reading only. No sampler verdicts.** Original selections/earlier A expansions and all coordinator records remain byte-preserved.','',
    '## Material-miss expansion','B2 XML: original population 167, original quota 17; **17 additional next-ranked units**, for 34 proposed XML samples before excluding the three known misses. All three misses have zero final clean credit.']
for row in expansion: plan.append(f"- `{row['id']}` — `{row['path']}:{row['first_line']}–{row['last_line']}`; rank {row['original_rank']}; **UNREAD**"+(' — future B2 correction path; final overlap check required.' if row['future_B2_correction_path'] else '.'))
plan += ['','## Eligibility replacements — separate from the 17-unit expansion',
    'B1 9715fd23161cbcd7 and e7b21994e1483471, plus B2 c7edeed9fe6d2e6c, retain explicit coordinator administrative/dormant exclusions with zero clean credit. No material-miss count is assigned to these exclusions.']
for q in replacement_quotas: plan.append(f"- {q['owner']} {q['stratum']}: working population {q['working_population_after_explicit_exclusions']}, 10% minimum {q['working_10_percent_minimum']}, valid original selected {q['original_valid_selected']}; **{q['required_replacements']} deterministic replacements required**.")
for row in replacement_rows: plan.append(f"- `{row['id']}` — `{row['path']}:{row['first_line']}–{row['last_line']}`; {row['owner']} {row['stratum']}; rank {row['original_rank']}; **UNREAD**.")
plan += ['','## Historical qualifications — coordinator decision stays separate',
    'B1 f1d4372202bdaa9b has traceable historical report support at 64efe844, scoped to its recorded rig/repeats. Raw captures/harness are missing; it grants no fresh physical or universal native-state qualification.',
    'The six selected B2 research-phase units retain exact historical evidence scope and no current-platform qualification. Original read-clean/source verdicts are not rewritten as silent history-clean acceptance.',
    'If these qualified history units are withheld from the current-contract narrative denominator, the following separate conditional replacements repair its minimum. Their exact claim scope still needs Astra reading; research-path candidates can themselves require historical qualification. They are not automatically activated or credited.']
for q in history_quotas: plan.append(f"- {q['owner']} narrative: conditional population {q['conditional_population']}, minimum {q['conditional_minimum']}, original valid if history withheld {q['valid_original_if_history_withheld']}; conditional replacement need **{q['conditional_replacement_need']}**.")
for row in conditional_rows: plan.append(f"- CONDITIONAL `{row['id']}` — `{row['path']}:{row['first_line']}–{row['last_line']}`; rank {row['original_rank']}; **UNREAD / zero credit**.")
plan += ['','## Reading and final reconciliation','[original-units.md](original-units.md) supplies all exact whole units, with purpose labels separating required expansion, eligibility repair and conditional history repair.',
    'Known accepted current-product changes are filtered by exact original changed spans, not whole files. The current manifest captures the accepted main/sampling/external state; later A/B2 corrections and final combined-diff/current-comment mapping still require canonical reconciliation.',
    'No other owner/role is reduced. New units have zero current clean credit and no content verdict from this preparer. Original units may be read now; current mappings are intentionally not chased while correction/integration work is active.',
    'Records-only rank/hash/index work. No source audit, product edits, SDK/build/test/native/device/formatter/crypto execution, Git mutation or subagent. Finish Core Pass 1 publication, then stop.']
(OUT/'read-plan.md').write_text('\n'.join(plan)+'\n')
receipt=dict(result='STRUCTURAL-EXPANSION-AND-SEPARATE-REPLACEMENT-PREPARATION; NO CONTENT VERDICTS',source_baseline=PIN,
    original_B1_reads_reconciled=111,original_B2_reads_reconciled=120,material_XML_additional=len(expansion),original_XML_quota=17,proposed_XML_total=34,
    administrative_dormant_exclusion_ids=3,mandatory_eligibility_replacements=len(replacement_rows),conditional_history_replacements=len(conditional_rows),
    selected_material_misses=3,miss_clean_credit=0,qualified_history_units=7,historical_clean_credit_by_preparer=0,
    overlaps_prior_selections_or_known_accepted_edits=0,original_983_and_prior_42_selections='BYTE-PRESERVED; input hashes bind exact records',
    accepted_manifest_paths=len(accepted),accepted_target_role_diff_paths=len(relevant),final_denominator=None,current_mapping='DEFERRED to final canonical reconciliation',
    input_sha256=inputs,outputs=[dict(path=str(p),bytes=p.stat().st_size,sha256=sha(p.read_bytes())) for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='verification.json'])
emit('verification.json',receipt)
print(json.dumps({k:v for k,v in receipt.items() if k not in ['input_sha256','outputs']},indent=2))
