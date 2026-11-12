#!/usr/bin/env python3
"""Two next-ranked B3 XML units plus additive activation of seven history replacements."""
import ast
import hashlib
import json
from pathlib import Path
import re
import subprocess

RUN = Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PREP = RUN / 'sampling/preparation'
OUT = PREP / 'expansion-B3'
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
SEED = 'core-001-clean-sampling-preparation-v1'
inputs, raw_originals, normalized_originals = {}, {}, {}


def sha(b): return hashlib.sha256(b).hexdigest()


def read(p):
    b=p.read_bytes(); inputs[str(p)]=sha(b); return b


def load(p): return json.loads(read(p))


def emit(name,value): (OUT/name).write_text(json.dumps(value,indent=2)+'\n')


read(Path(__file__))
population=load(PREP/'population.json')['units']; byid={r['id']:r for r in population}
selection=load(PREP/'selection.json'); original_ids={r['id'] for r in selection}
prior_additions=[]
for path in ['expansion-A/additional-selection.json','expansion-A-followup/additional-selection.json',
             'expansion-B2/additional-selection.json','expansion-B2/replacement-selection.json','expansion-B2/conditional-history-replacements.json']:
    prior_additions += load(PREP/path)
prior_ids=original_ids|{r['id'] for r in prior_additions}
assert len(prior_ids)==len(original_ids)+len(prior_additions)==1052
strata=load(PREP/'strata.json'); q=next(x for x in strata if x['owner']=='B3' and x['stratum']=='XML-contracts')
assert q['proposed_selected']==q['minimum_10_percent']==2 and q['eligible']==14
units={u['id']:u for u in load(RUN/'inventory/semantic-units.json')}
files={f['path']:f for f in load(RUN/'inventory/files.json')}
record=load(RUN/'sampling/coordinator/read-B3-verdicts.json')
assert sha(read(RUN/'sampling/coordinator/read-B3.md'))==record['reading_sha256']
assert len(record['units'])==110 and {r['id'] for r in record['units']}=={r['id'] for r in selection if r['owner']=='B3'}
miss='core-001:unit:f8412d69f0903d78'; miss_row=next(r for r in record['units'] if r['id']==miss)
assert miss_row['verdict']=='DOCUMENTATION-MISS' and miss in q['selected_ids']
for row in record['units']: assert row['range_sha256']==units[row['id']]['range_sha256']

# Reuse the earlier original-byte reconstruction and patch-span helper definitions only; no prior
# preparation's top-level writer is imported or rerun.
original_helper=read(PREP/'expansion-B2/expand.py'); parser_helper=read(PREP/'prepare.py')
namespace=dict(subprocess=subprocess,PIN=PIN,files=files,raw_originals=raw_originals,normalized_originals=normalized_originals,
               sha=sha,Path=Path,read=read,re=re)
for data,name,path in [(original_helper,'original',PREP/'expansion-B2/expand.py'),(parser_helper,'parse_patch',PREP/'prepare.py')]:
    node=next(n for n in ast.parse(data).body if isinstance(n,ast.FunctionDef) and n.name==name)
    exec(compile(ast.Module(body=[node],type_ignores=[]),str(path),'exec'),namespace)
original=namespace['original']; namespace['original']=original
eligible={r['id'] for r in population if r['owner']=='B3' and r.get('stratum')=='XML-contracts' and r['eligibility']=='candidate-eligible'}
rank=sorted(eligible,key=lambda key:sha((SEED+'|B3|XML-contracts|'+key).encode()))
assert q['selected_ids']==rank[:2]
old_history=load(PREP/'expansion-B2/history-qualification-overlay.json')
old_replacements=load(PREP/'expansion-B2/conditional-history-replacements.json')
manifest=load(RUN/'editing/integration/current-products.json'); assert manifest['source_baseline']==PIN
accepted={r['path']:r['sha256'] for r in manifest['product_files']}
for path,digest in accepted.items(): assert sha(Path(path).read_bytes())==digest,('Accepted current product changed',path)
for name in manifest['receipt_order']: load(RUN/'editing/integration'/name)
target_paths=sorted(({units[k]['path'] for k in eligible}|{r['path'] for r in old_replacements})&accepted.keys())
command=['git','--no-optional-locks','diff','--no-ext-diff','--no-textconv','--no-renames','--unified=3','--src-prefix=a/','--dst-prefix=b/',PIN,'--',*target_paths]
patch=subprocess.check_output(command); patch_path=OUT/'accepted-current-B3-XML-scope.diff'; patch_path.write_bytes(patch)
spans=namespace['parse_patch'](patch_path)


def overlap(key):
    u=units[key];a,b=u['start_character'],u['end_character_exclusive']
    return [s for s in spans.get(u['path'],[]) if (a<s['end_character_exclusive'] and s['start_character']<b
        if s['start_character']!=s['end_character_exclusive'] else a<=s['start_character']<=b)]


available=[k for k in rank if k not in prior_ids and not byid[k].get('edited_overlap') and not overlap(k)]
chosen=available[:2]; assert len(chosen)==2
additional=[]
for key in chosen:
    u=units[key];body=original(u['path'])[u['start_character']:u['end_character_exclusive']]
    assert sha(body.encode())==u['range_sha256']
    additional.append(dict(id=key,owner='B3',stratum='XML-contracts',original_rank=rank.index(key)+1,
        **{f:u[f] for f in ['path','first_line','last_line','start_character','end_character_exclusive','source_sha256','range_sha256','category','label']},
        known_edited_overlap=False,original_or_prior_selected=False,read_status='ADDITIONAL-PROPOSED-UNREAD',verdict='UNASSIGNED',clean_credit=0,
        future_B3_correction_path=u['path']==units[miss]['path'],current_mapping='DEFERRED until final canonical reconciliation; original pinned unit may be read now'))
assert not set(chosen)&prior_ids
assert len(old_history)==len(old_replacements)==7
assert len({r['id'] for r in old_replacements})==7 and not {r['id'] for r in old_replacements}&original_ids
history_overlay=[]
for row in old_history:
    key=row['id'];assert key in original_ids
    history_overlay.append(dict(id=key,owner=row['owner'],path=row['path'],original_qualification_record=row,
        coordinator_adjudication='SOURCE/EVIDENCE-QUALIFIED-CONTEXT; EXCLUDED-FROM-FINAL-CLEAN-QUOTA',
        actually_read_by='Astra; original B1/B2 selected reading retained',final_clean_credit=0,
        rationale='Exact report at 64efe844 was checked by Astra/owner; original raw capture/harness missing. Retain historical rig/repeats and no fresh physical attestation.' if row['owner']=='B1' else
                  'Phase-scope historical status/handoff evidence was actually read. It is not current product/platform acceptance and receives no final clean quota credit.',
        authority='Coordinator release of this task; original source-disposition ledger/rows unchanged'))
activated=[]
for row in old_replacements:
    u=units[row['id']];body=original(u['path'])[u['start_character']:u['end_character_exclusive']]
    assert sha(body.encode())==u['range_sha256']
    assert not overlap(row['id']), ('Activated replacement now overlaps accepted edits; preserve activation and seek canonical reconciliation', row['id'])
    activated.append(dict(**row,prior_conditional_purpose=row['purpose'],activation='ACTIVATED-BY-COORDINATOR; ACTUAL-READING-PENDING',
        activation_authority='Coordinator explicitly directs use of all seven replacement units',
        final_clean_credit=0,eligibility='Proposed replacement scope still needs actual Astra reading and final edited-overlap reconciliation; historical-looking new context is not silently clean.'))
miss_unit=units[miss];miss_body=original(miss_unit['path'])[miss_unit['start_character']:miss_unit['end_character_exclusive']]
assert sha(miss_body.encode())==miss_unit['range_sha256']
miss_overlay=dict(id=miss,owner='B3',stratum='XML-contracts',coordinator_finding='SAMPLE-B3-001',
    original_ledger_status='READ-CLEAN-PRESERVED-AS-HISTORY',coordinator_verdict=miss_row['verdict'],coordinator_note=miss_row['note'],
    source_sha256=miss_unit['source_sha256'],range_sha256=miss_unit['range_sha256'],final_clean_credit=0,
    correction='Transport editor correcting comments; final accepted patch/range mapping/re-review is a later gate. No runtime or native failure claim.')
for path,digest in inputs.items(): assert sha(Path(path).read_bytes())==digest,('Bound input changed',path)
for path,digest in accepted.items(): assert sha(Path(path).read_bytes())==digest,('Accepted current product changed during capture',path)
emit('additional-selection.json',additional);emit('miss-overlay.json',miss_overlay)
emit('history-credit-overlay.json',history_overlay);emit('activated-history-replacements.json',activated)
emit('quota.json',dict(owner='B3',stratum='XML-contracts',original_population=len(eligible),original_full_quota=2,
    original_selected_ids=q['selected_ids'],additional_full_quota=2,additional_selected_ids=chosen,proposed_total=4,
    original_misses_with_zero_final_credit=1,exact_accepted_overlap_skips=[dict(id=k,spans=overlap(k)) for k in rank if k not in prior_ids and overlap(k)]))
emit('accepted-overlap-inputs.json',dict(manifest='editing/integration/current-products.json',manifest_sha256=inputs[str(RUN/'editing/integration/current-products.json')],
    accepted_paths=len(accepted),target_sampling_paths=target_paths,scope='B3 candidate XML and existing seven activated history-replacement paths',command=command,diff_sha256=sha(patch),diff_bytes=len(patch),
    parser_reuse_sha256=sha(parser_helper),original_reconstruction_reuse_sha256=sha(original_helper),original_changed_spans=spans))
excerpts=['# B3 additions and activated history-replacement whole units','','**No content verdicts.** Pinned source `'+PIN+'`; read enclosing owner/helper/assertion context as needed.','']
for label,rows in [('New B3 XML expansion',additional),('Existing seven replacements now activated',activated)]:
    excerpts += ['## '+label,'']
    for row in rows:
        body=original(row['path'])[row['start_character']:row['end_character_exclusive']]
        excerpts += ['### `'+row['id']+'`',f"`{row['path']}:{row['first_line']}–{row['last_line']}`; **UNREAD / zero clean credit**.",'','````'+('csharp' if row['path'].endswith('.cs') else 'markdown'),body,'````','']
(OUT/'original-units.md').write_text('\n'.join(excerpts)+'\n')
plan=['# B3 XML expansion and activation of history replacements','','**Proposed reading only.** Original 983 selections and all previous expansion/conditional records remain unchanged. No content verdict is supplied here.','',
    'B3 XML: original candidate population 14, original quota **2**, additional next-ranked quota **2**, proposed total **4**. Original SAMPLE-B3-001 has zero final clean credit.',
    'Astra has read all 110 original B3 units. Original A/B1/B2/B3 selected-read total is **517**; the other 466 original selections remain unread according to this coordinator handoff. New/replacement reads are separate and still pending.','',
    '## New B3 XML reads']
for row in additional:plan.append(f"- `{row['id']}` — `{row['path']}:{row['first_line']}–{row['last_line']}`; original rank {row['original_rank']}; **UNREAD**"+(' — future B3 correction path, final overlap check required.' if row['future_B3_correction_path'] else '.'))
plan += ['','## Seven existing history replacements: activated, not new IDs',
    'The coordinator directs use of all seven previously conditional units: one B1 narrative and six B2 narrative. Their original conditional rows remain byte-preserved; `activated-history-replacements.json` supplies additive activation. Actual Astra reading remains pending.']
for row in activated:plan.append(f"- `{row['id']}` — `{row['path']}:{row['first_line']}–{row['last_line']}`; {row['owner']}; **ACTIVATED / UNREAD / zero credit**.")
plan += ['','## Original historical units and source qualifications',
    '`history-credit-overlay.json` records seven original actually-read units as SOURCE/EVIDENCE-QUALIFIED context, excluded from final clean quota. It preserves their complete earlier qualification records and leaves all original source-disposition ledgers untouched.',
    'B1 f1d4372202bdaa9b: the exact report at 64efe844 was checked by Astra/owner; no original raw capture/harness or fresh physical attestation. B2 six phase-report/handoff excerpts preserve their historical scope and supply no current product/platform acceptance.',
    'Replacement activation is not a verdict that new research-path context is current clean prose. Astra must read each complete replacement in its exact scope and record any additional exclusion or qualification.',
    '[original-units.md](original-units.md) supplies the two new and seven activated complete pinned units. `additional-selection.json` and the activation record retain exact ranges and source/range hashes.',
    'Known accepted edits are checked by exact original changed spans for the B3 XML candidate set, never whole-file exclusion. Final accepted sampling corrections, final combined diff/current mappings and actual sampler verdicts remain pending canonical reconciliation.',
    'No other eligibility narrowing or private/internal/test exclusions are introduced. No other owner/role is reduced. All new units have zero current clean credit. No source audit, product edit, SDK/build/test/native/device/formatter/crypto execution, Git mutation or subagent. Finish Core Pass 1 publication, then stop.']
(OUT/'read-plan.md').write_text('\n'.join(plan)+'\n')
receipt=dict(result='STRUCTURAL-B3-EXPANSION-AND-HISTORY-ACTIVATION-VERIFIED; NO CONTENT VERDICTS',source_baseline=PIN,
    original_B3_reads_reconciled=110,original_A_B1_B2_B3_reads=517,other_original_selected_units_unread=466,
    original_XML_quota=2,additional_B3_XML=2,proposed_B3_XML_total=4,material_miss_zero_clean_credit=1,
    activated_existing_replacement_IDs=7,new_history_replacement_IDs_created=0,actually_read_history_units_excluded_from_clean_quota=7,
    history_final_clean_credit=0,replacement_actual_reading='PENDING',all_new_content_verdicts=0,
    duplicates_or_known_edited_new_B3_IDs=0,prior_selection_records='BYTE-PRESERVED; input hashes below',
    accepted_manifest_paths=len(accepted),accepted_target_sampling_paths=len(target_paths),known_edited_activated_replacement_IDs=0,final_denominator=None,current_mapping='DEFERRED until final canonical reconciliation',
    input_sha256=inputs,outputs=[dict(path=str(p),bytes=p.stat().st_size,sha256=sha(p.read_bytes())) for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='verification.json'])
emit('verification.json',receipt)
print(json.dumps({k:v for k,v in receipt.items() if k not in ['input_sha256','outputs']},indent=2))
