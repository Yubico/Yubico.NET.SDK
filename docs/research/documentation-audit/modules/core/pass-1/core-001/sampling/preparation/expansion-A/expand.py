#!/usr/bin/env python3
"""Expand two admitted A strata from immutable candidate population/ranks only."""
import hashlib
import json
from pathlib import Path
import subprocess

RUN = Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PREP = RUN / 'sampling/preparation'
OUT = PREP / 'expansion-A'
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
SEED = 'core-001-clean-sampling-preparation-v1'
inputs = {}


def sha(b): return hashlib.sha256(b).hexdigest()


def read(p):
    b = p.read_bytes(); inputs[str(p)] = sha(b); return b


def load(p): return json.loads(read(p))


def emit(name, value): (OUT / name).write_text(json.dumps(value, indent=2) + '\n')


read(Path(__file__))
admission = read(RUN / 'sampling/coordinator/admission.md')
verdict = read(RUN / 'sampling/coordinator/read-001-verdicts.md')
assert b'accepts the preparation' in admission
population = load(PREP / 'population.json')['units']
original_selection = load(PREP / 'selection.json')
strata = load(PREP / 'strata.json')
summary = load(PREP / 'summary.json')
unit_rows = load(RUN / 'inventory/semantic-units.json'); units = {u['id']: u for u in unit_rows}
files = {f['path']: f for f in load(RUN / 'inventory/files.json')}
byid = {r['id']: r for r in population}; selected_ids = {r['id'] for r in original_selection}
assert len(selected_ids) == len(original_selection) == 983 and summary['source_baseline'] == PIN
misses = {
    'core-001:unit:711e47d18bab0742': ('SAMPLE-A001', 'XML-contracts'),
    'core-001:unit:c085f1cd3632e7c2': ('SAMPLE-A002', 'enum-values'),
    'core-001:unit:076b4d4328825902': ('SAMPLE-A002', 'XML-contracts')}
for key, (finding, role) in misses.items():
    assert key in selected_ids and key.split(':')[-1].encode() in verdict
    assert byid[key]['owner'] == 'A' and byid[key]['stratum'] == role and byid[key]['original_status'] == 'READ-CLEAN'
miss_paths = {units[key]['path'] for key in misses}

additional, quota_records = [], []
for role in ['XML-contracts', 'enum-values']:
    stratum = next(s for s in strata if s['owner'] == 'A' and s['stratum'] == role)
    eligible = {r['id'] for r in population if r['owner'] == 'A' and r.get('stratum') == role and r['eligibility'] == 'candidate-eligible'}
    rank = sorted(eligible, key=lambda key: sha((SEED + '|A|' + role + '|' + key).encode()))
    quota = stratum['proposed_selected']
    assert len(eligible) == stratum['eligible'] and stratum['selected_ids'] == rank[:quota]
    available = [key for key in rank if key not in selected_ids and key not in misses and not byid[key].get('edited_overlap')]
    chosen = available[:quota]
    assert len(chosen) == quota
    quota_records.append(dict(owner='A', stratum=role, original_eligible_population=len(eligible), original_full_quota=quota,
        original_selected_ids=stratum['selected_ids'], additional_full_quota=len(chosen), additional_selected_ids=chosen,
        original_plus_additional_proposed=quota+len(chosen), known_missed_original_units=sum(misses[k][1] == role for k in misses),
        final_clean_credit='No additional content verdict; original material misses receive zero credit. Future edited overlaps must be reconciled.',
        rank_rule='Continue exact original fixed-seed SHA256 rank after excluding all original selected IDs and known edited overlaps.'))
    for key in chosen:
        u = units[key]
        additional.append(dict(id=key, owner='A', stratum=role, original_population_rank=rank.index(key)+1,
            path=u['path'], first_line=u['first_line'], last_line=u['last_line'], start_character=u['start_character'],
            end_character_exclusive=u['end_character_exclusive'], source_sha256=u['source_sha256'], range_sha256=u['range_sha256'],
            category=u['category'], label=u['label'], existing_known_edit_overlap=False,
            future_A_correction_path=u['path'] in miss_paths, read_status='ADDITIONAL-PROPOSED-UNREAD', verdict='UNASSIGNED',
            clean_credit=0,
            current_mapping='NOT-REFRESHED: await accepted combined diff; original pinned unit may be read now.',
            final_clean_credit='PENDING actual Astra reading and final edited-overlap reconciliation'))

assert [q['additional_full_quota'] for q in quota_records] == [19, 4]
assert len(additional) == len({r['id'] for r in additional}) == 23
assert not {r['id'] for r in additional} & selected_ids

original_cache = {}


def original(path):
    if path not in original_cache:
        pinned = subprocess.check_output(['git', 'show', PIN + ':' + path]); candidates = [pinned]
        if sha(pinned) != files[path]['sha256']:
            text = pinned.decode('utf-8-sig').replace('\r\n', '\n')
            candidates += [(bom + text.replace('\n', newline)).encode() for bom in ['', '\ufeff'] for newline in ['\n', '\r\n']]
        raw = next((b for b in candidates if sha(b) == files[path]['sha256']), None)
        assert raw is not None, ('Original pinned representation unavailable', path)
        original_cache[path] = raw.decode('utf-8-sig', errors='replace')
    return original_cache[path]


text_parts = ['# Additional A original whole units', '', '**No content verdicts.** Source `' + PIN + '`. Read enclosing owner/helper/assertion/cleanup context from each named pinned source path.', '']
for row in additional:
    text = original(row['path']); body = text[row['start_character']:row['end_character_exclusive']]
    assert sha(body.encode()) == row['range_sha256']
    text_parts += ['## `' + row['id'] + '`',
        f"`{row['path']}:{row['first_line']}–{row['last_line']}` — {row['stratum']}, original rank {row['original_population_rank']}; current mapping pending final diff.", '',
        '````csharp', body, '````', '']
for key in misses:
    u = units[key]; text = original(u['path'])
    assert sha(text[u['start_character']:u['end_character_exclusive']].encode()) == u['range_sha256']

overlay = [dict(id=key, coordinator_finding=misses[key][0], owner='A', stratum=misses[key][1], path=units[key]['path'],
    original_ledger_status='READ-CLEAN-PRESERVED-AS-HISTORY', annotation='MATERIAL-DOCUMENTATION-MISS; CORRECTION-OVERLAP-EXCLUDED-FROM-FINAL-CLEAN-CREDIT',
    final_clean_credit=0, source_sha256=units[key]['source_sha256'], range_sha256=units[key]['range_sha256'],
    authority='sampling/coordinator/read-001-verdicts.md', correction='A editor verifying/correcting comments only; actual final patch overlap/re-review remains a separate coordinator gate.') for key in sorted(misses)]
for path,digest in inputs.items(): assert sha(Path(path).read_bytes()) == digest, ('Input changed during bounded join', path)
emit('additional-selection.json', additional)
emit('miss-overlay.json', overlay)
emit('quotas.json', quota_records)
(OUT / 'original-units.md').write_text('\n'.join(text_parts) + '\n')
plan = ['# A sampling expansion after material documentation misses', '',
    '**Prepared additional reads only; no new content verdict.** Astra accepted the broad policy and requested one additional full original quota in each affected stratum.', '',
    '| A stratum | Original eligible | Original quota | Additional next-ranked | Proposed total | Known misses with zero final credit |',
    '| --- | ---: | ---: | ---: | ---: | ---: |']
for q in quota_records:
    plan.append(f"| {q['stratum']} | {q['original_eligible_population']} | {q['original_full_quota']} | {q['additional_full_quota']} | {q['original_plus_additional_proposed']} | {q['known_missed_original_units']} |")
plan += ['', '**23 additional unique IDs**, disjoint from all original 983 selections and known edited-overlap exclusions. No other owner or role is reduced.',
    'Original plus expansion: 1,006 proposed IDs. Three original material-miss IDs receive zero final clean credit; the remaining 1,003 are only potential dispositions, not accepted clean samples.',
    'The original 983 selection, population, read plans and coordinator verdicts are unchanged. `miss-overlay.json` adds annotations rather than rewriting the historical read-clean ledgers.', '',
    '## Additional reading order', 'Use [original-units.md](original-units.md) for all 23 exact pinned whole units, verified by original source/range hashes. Read enclosing context from the named pinned path as needed.']
for row in additional:
    risk = ' **Future A correction path: zero clean credit pending final overlap check.**' if row['future_A_correction_path'] else ''
    plan.append(f"- `{row['id']}` — `{row['path']}:{row['first_line']}–{row['last_line']}`; {row['stratum']}; original rank {row['original_population_rank']}; **UNREAD / verdict unassigned**." + risk)
plan += ['', '## Final reconciliation remains required',
    'Source pin: `' + PIN + '`. Current primary mappings are deliberately not chased while integration/corrections are active. Original units may be read now; current comments need mapping and overlap checks after freeze.',
    'The pending external 35-file final patch, the new A comment corrections and three new toolchain-comment changes must enter the final accepted combined-diff reconciliation. Known earlier editor patch spans remain the population filter for this bounded expansion; unknown successor spans receive no automatic clean credit.',
    'A correction may change any selected neighbor on the same path; `future_A_correction_path` flags that risk without claiming an unreceived patch or silently excluding a whole file. Remove actual final edited overlaps and replace/read additional units as needed after freeze.',
    'Astra must actually read these units and record verdicts. Existing first-batch verdicts remain with the coordinator, and future material misses may require further category expansion. No denominator or sampling-pass claim is made here.',
    'Records-only rank/hash/index work. No product edits, source/spec investigation, SDK build/test/native/device/formatter/crypto execution, Git mutation or subagent. Finish Core Pass 1 publication, then stop; no new project or Pass 2 authorization.']
(OUT / 'read-plan.md').write_text('\n'.join(plan) + '\n')
receipt = dict(result='STRUCTURAL-EXPANSION-CHECKS-PASSED; NO CONTENT VERDICTS', source_baseline=PIN,
    additional_ids=23, XML_additional=19, enum_additional=4, original_selection=983, proposed_union=1006,
    overlaps_with_original_selection=0, known_material_miss_overlay_ids=3, miss_final_clean_credit=0,
    nominal_union_after_known_misses=1003, nominal_is_not_accepted_sampling_credit=True,
    original_population_and_selection_and_coordinator_records='UNCHANGED; exact input hashes below',
    exact_source_and_range_hashes_verified=26, original_source_paths=len(original_cache), other_owner_role_changes=0,
    final_denominator=None, current_mapping='DEFERRED until final accepted combined diff; no live source chasing',
    pending_dependencies=['External final 35-file patch', 'New A comment corrections', 'Three new toolchain comments', 'Final accepted combined diff, overlap exclusion/replacements and actual Astra verdicts'],
    input_sha256=inputs, outputs=[dict(path=str(p), bytes=p.stat().st_size, sha256=sha(p.read_bytes())) for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='verification.json'])
emit('verification.json', receipt)
print(json.dumps({k:v for k,v in receipt.items() if k not in ['input_sha256','outputs']}, indent=2))
