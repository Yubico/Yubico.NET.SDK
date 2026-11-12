#!/usr/bin/env python3
"""Resolve four known-gap replacement choices and separately prepare two SD narrative reads."""
import ast
import hashlib
import json
from pathlib import Path
import re
import subprocess

RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PREP=RUN/'sampling/preparation';OLD=PREP/'family-replacements';OUT=OLD/'revision-2';SD=PREP/'expansion-SecurityDomain'
PIN='fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef';SEED='core-001-clean-sampling-preparation-v1'
SW='src/Core/src/Protocols/SmartCard/Apdu/SWConstants.cs'
inputs,raw_originals,normalized_originals={},{},{}


def sha(b):return hashlib.sha256(b).hexdigest()


def read(p):
    b=p.read_bytes();inputs[str(p)]=sha(b);return b


def load(p):return json.loads(read(p))


def emit(directory,name,value):(directory/name).write_text(json.dumps(value,indent=2)+'\n')


read(Path(__file__));SD.mkdir(parents=True,exist_ok=True)
population=load(PREP/'population.json')['units'];byid={r['id']:r for r in population}
selection=load(PREP/'selection.json');prior_ids={r['id'] for r in selection}
for path in ['expansion-A/additional-selection.json','expansion-A-followup/additional-selection.json',
             'expansion-B2/additional-selection.json','expansion-B2/replacement-selection.json','expansion-B2/conditional-history-replacements.json',
             'expansion-B3/additional-selection.json','expansion-B4/additional-selection.json','expansion-B4/replacement-selection.json']:
    prior_ids.update(r['id'] for r in load(PREP/path))
old_rows=load(OLD/'replacement-selection.json');old_quota=load(OLD/'quotas.json');old_overlay=load(OLD/'eligibility-and-qualification-overlay.json')
old_receipt=load(OLD/'verification.json')
for name in ['read-plan.md','original-units.md']:read(OLD/name)
assert len(prior_ids)==1059 and len(old_rows)==8
prior_ids.update(r['id'] for r in old_rows);assert len(prior_ids)==1067
retired=[r for r in old_rows if r['owner']=='B6' and r['path']==SW]
assert len(retired)==4
strata=load(PREP/'strata.json');units={u['id']:u for u in load(RUN/'inventory/semantic-units.json')};files={f['path']:f for f in load(RUN/'inventory/files.json')}
sd_verdict=load(RUN/'sampling/coordinator/read-boundary-SecurityDomain-verdicts.json')
assert sha(read(RUN/'sampling/coordinator/read-boundary-SecurityDomain.md'))==sd_verdict['reading_sha256']
assert {r['id'] for r in sd_verdict['units']}=={r['id'] for r in selection if r['owner']=='SecurityDomain'}
sd_miss='core-001:unit:ed3e115f81fd126c';sd_miss_row=next(r for r in sd_verdict['units'] if r['id']==sd_miss)
assert sd_miss_row['verdict']=='DOCUMENTATION-MISS' and sd_miss_row['range_sha256']==units[sd_miss]['range_sha256']

original_helper=read(PREP/'expansion-B2/expand.py');parser_helper=read(PREP/'prepare.py')
namespace=dict(subprocess=subprocess,PIN=PIN,files=files,raw_originals=raw_originals,normalized_originals=normalized_originals,sha=sha,Path=Path,read=read,re=re)
for data,name,path in [(original_helper,'original',PREP/'expansion-B2/expand.py'),(parser_helper,'parse_patch',PREP/'prepare.py')]:
    node=next(n for n in ast.parse(data).body if isinstance(n,ast.FunctionDef) and n.name==name)
    exec(compile(ast.Module(body=[node],type_ignores=[]),str(path),'exec'),namespace)
original=namespace['original'];namespace['original']=original
roles={('B6','XML-contracts'),('SecurityDomain','narrative-examples-and-maps')}
candidate_paths={units[r['id']]['path'] for r in population if (r['owner'],r.get('stratum')) in roles and r['eligibility']=='candidate-eligible'}
manifest=load(RUN/'editing/integration/current-products.json');assert manifest['source_baseline']==PIN
accepted={r['path']:r['sha256'] for r in manifest['product_files']}
for path,digest in accepted.items():assert sha(Path(path).read_bytes())==digest,('Accepted current product changed',path)
for name in manifest['receipt_order']:load(RUN/'editing/integration'/name)
relevant=sorted(candidate_paths&accepted.keys())
command=['git','--no-optional-locks','diff','--no-ext-diff','--no-textconv','--no-renames','--unified=3','--src-prefix=a/','--dst-prefix=b/',PIN,'--',*relevant]
patch=subprocess.check_output(command);patch_path=OUT/'accepted-current-B6-SD-scope.diff';patch_path.write_bytes(patch)
spans=namespace['parse_patch'](patch_path)


def overlap(key):
    u=units[key];a,b=u['start_character'],u['end_character_exclusive']
    return [s for s in spans.get(u['path'],[]) if (a<s['end_character_exclusive'] and s['start_character']<b
        if s['start_character']!=s['end_character_exclusive'] else a<=s['start_character']<=b)]


def rank_for(owner,role):
    keys={r['id'] for r in population if r['owner']==owner and r.get('stratum')==role and r['eligibility']=='candidate-eligible'}
    return sorted(keys,key=lambda key:sha((SEED+'|'+owner+'|'+role+'|'+key).encode()))


def row_for(key,purpose,rank):
    u=units[key];body=original(u['path'])[u['start_character']:u['end_character_exclusive']];assert sha(body.encode())==u['range_sha256']
    return dict(id=key,owner=byid[key]['owner'],stratum=byid[key]['stratum'],purpose=purpose,original_rank=rank.index(key)+1,
        **{f:u[f] for f in ['path','first_line','last_line','start_character','end_character_exclusive','source_sha256','range_sha256','category','label']},
        known_edited_overlap=False,prior_selected=False,read_status='PROPOSED-UNREAD',verdict='UNASSIGNED',clean_credit=0,
        current_mapping='DEFERRED until final canonical reconciliation; original pinned unit may be read now')


b6_rank=rank_for('B6','XML-contracts');assert len(b6_rank)==104
pool_exclusions=[dict(id=k,path=units[k]['path'],original_rank=b6_rank.index(k)+1,
    scope='CLEAN-QUOTA-REPLACEMENT-POOL-ONLY',reason='Coordinator directs skipping remaining SWConstants constant claims because the same already-known exact original-ISO source qualification applies.',
    broad_population_membership='UNCHANGED; still in original 104-unit XML denominator',original_ledger_status='UNCHANGED',
    ISO_verified=False,clean_credit_by_preparer=0) for k in b6_rank if units[k]['path']==SW]
available=[k for k in b6_rank if k not in prior_ids and units[k]['path']!=SW and not byid[k].get('edited_overlap') and not overlap(k)]
needed=4;new_keys=available[:needed]
new_rows=[row_for(k,'NON-SW-QUALIFICATION-REPLACEMENT-POOL-SUCCESSOR; NOT DOC-DEFECT-EXPANSION',b6_rank) for k in new_keys]
replacement_map={old['id']:new for old,new in zip(retired,new_rows)}
successor=[replacement_map.get(row['id'],row) for row in old_rows if row['id'] not in {r['id'] for r in retired} or row['id'] in replacement_map]
for old,new in zip(retired,new_rows):new['supersedes_provisional_id']=old['id']
assert len(new_rows)==len({r['id'] for r in new_rows}) and not {r['id'] for r in new_rows}&prior_ids
assert all(r['path']!=SW for r in successor)
unchanged=[r for r in old_rows if r not in retired]
assert all(r in successor for r in unchanged) and len(unchanged)==4
used=prior_ids|set(new_keys)
sd_role='narrative-examples-and-maps';sd_quota=next(q for q in strata if q['owner']=='SecurityDomain' and q['stratum']==sd_role)
sd_rank=rank_for('SecurityDomain',sd_role);assert sd_quota['proposed_selected']==sd_quota['minimum_10_percent']==2 and sd_quota['selected_ids']==sd_rank[:2]
sd_available=[k for k in sd_rank if k not in used and not byid[k].get('edited_overlap') and not overlap(k)]
sd_keys=sd_available[:2];assert len(sd_keys)==2
sd_rows=[row_for(k,'SAMPLE-SD-001-FULL-ORIGINAL-NARRATIVE-QUOTA-EXPANSION',sd_rank) for k in sd_keys]
assert not set(sd_keys)&used
for row in sd_rows:row['future_SD_correction_path']=row['path']==units[sd_miss]['path']
miss_overlay=dict(id=sd_miss,owner='SecurityDomain',stratum=sd_role,coordinator_finding='SAMPLE-SD-001',
    original_ledger_status='READ-CLEAN-PRESERVED-AS-HISTORY',coordinator_verdict=sd_miss_row['verdict'],coordinator_note=sd_miss_row['note'],
    final_clean_credit=0,source_sha256=units[sd_miss]['source_sha256'],range_sha256=units[sd_miss]['range_sha256'],
    correction='Confirmed two-line documentation correction awaiting review; final patch/mapping remains a coordinator gate, no new runtime claim.')
for path,digest in inputs.items():assert sha(Path(path).read_bytes())==digest,('Bound input changed',path)
for path,digest in accepted.items():assert sha(Path(path).read_bytes())==digest,('Accepted current product changed during capture',path)
emit(OUT,'predecessor-history.json',dict(status='Original eight-unit replacement proposal retained byte-exact as history; only four SW provisional choices superseded here.',
    original_count=8,source_baseline=PIN,original_selection=str(OLD/'replacement-selection.json'),original_selection_sha256=inputs[str(OLD/'replacement-selection.json')],
    original_quota_sha256=inputs[str(OLD/'quotas.json')],original_overlay_sha256=inputs[str(OLD/'eligibility-and-qualification-overlay.json')],
    original_receipt_sha256=inputs[str(OLD/'verification.json')],retired_provisional_ids=[r['id'] for r in retired],
    supersession=[dict(old_id=k,new_id=v['id']) for k,v in replacement_map.items()],unchanged_original_rows=4))
emit(OUT,'replacement-selection.json',successor);emit(OUT,'new-non-SW-selection.json',new_rows);emit(OUT,'replacement-pool-exclusions.json',pool_exclusions)
emit(OUT,'quotas.json',dict(owner='B6',stratum='XML-contracts',broad_original_population=104,original_minimum_quota=11,
    original_qualification_replacement_slots=5,retained_non_SW_slot=1,retired_SW_provisional_slots=4,requested_new_non_SW_slots=4,
    available_non_SW_unselected_unedited_pool=len(available),new_slots_filled=len(new_rows),shortfall=needed-len(new_rows),
    total_successor_family_replacements=len(successor),global_denominator_change=0,
    policy='SWConstants skipped only in replacement pool; original statuses/source gaps and 104 denominator unchanged. New units require Astra reading/source acceptance.'))
emit(OUT,'accepted-overlap-inputs.json',dict(manifest='editing/integration/current-products.json',manifest_sha256=inputs[str(RUN/'editing/integration/current-products.json')],
    accepted_paths=len(accepted),target_paths=relevant,command=command,diff_sha256=sha(patch),diff_bytes=len(patch),original_changed_spans=spans,
    parser_reuse_sha256=sha(parser_helper),scope='Exact known accepted B6 XML/SD narrative spans; no global population rewrite'))
emit(SD,'additional-selection.json',sd_rows);emit(SD,'miss-overlay.json',miss_overlay)
emit(SD,'quota.json',dict(owner='SecurityDomain',stratum=sd_role,original_population=len(sd_rank),original_full_quota=2,additional_full_quota=2,proposed_total=4,
    known_miss_clean_credit=0,source_baseline=PIN,other_owner_role_reductions=0))
for directory,heading,rows in [(OUT,'Successor non-SW family replacement whole units',successor),(SD,'Additional SD narrative whole units',sd_rows)]:
    text=['# '+heading,'','**No content verdicts.** Pinned source `'+PIN+'`; read complete units and relevant enclosing context.','']
    for row in rows:
        body=original(row['path'])[row['start_character']:row['end_character_exclusive']];assert sha(body.encode())==row['range_sha256']
        text+=['## `'+row['id']+'`',f"`{row['path']}:{row['first_line']}–{row['last_line']}`; {row['owner']} {row['stratum']}; **UNREAD / zero credit**.",'','````'+('csharp' if row['path'].endswith('.cs') else 'markdown'),body,'````','']
    (directory/'original-units.md').write_text('\n'.join(text)+'\n')
plan=['# Non-SW successor for B6 qualification replacements','','**Proposed reading only.** The original eight-unit proposal and original gap/status overlays remain unchanged at their original paths; `predecessor-history.json` binds those bytes as history.','',
    f"The clean-quota replacement pool skips {len(pool_exclusions)} SWConstants IDs from the original 104-unit XML population. This is a replacement-pool restriction only: broad denominator **104**, minimum **11**, all original statuses and exact ISO gaps remain unchanged.",
    f"Available next-ranked non-SW/unselected/unedited pool: **{len(available)}**. Requested **4**, filled **{len(new_rows)}**, shortfall **{needed-len(new_rows)}**. The previously selected ScpProcessor XML unit and the three B5/B6 test-role units are retained byte-for-byte; only the four provisional SW choices are superseded.",'','## Four new original units']
for row in new_rows:plan.append(f"- `{row['id']}` — `{row['path']}:{row['first_line']}–{row['last_line']}`; original rank {row['original_rank']}; replaces `{row['supersedes_provisional_id']}`; **UNREAD / zero credit**.")
plan+=['','[original-units.md](original-units.md) supplies the full successor proposal, including its four unchanged rows. `replacement-pool-exclusions.json` records every exact SW replacement-pool exclusion and its scope.',
    'No unknown ISO clause is declared verified; no original source gap or status is erased. Non-SW selection supplies checkable proposed units, not an automatic verdict on other normative/provider claims.',
    'Security Domain narrative expansion is separate in `sampling/preparation/expansion-SecurityDomain/`; it changes no B6 quota. Final accepted correction overlap, canonical mappings and actual Astra reads remain pending.',
    'No source research/audit, product edits, SDK/build/test/native/device/formatter/crypto execution, Git mutation or subagent. Finish Core Pass 1 publication, then stop.']
(OUT/'read-plan.md').write_text('\n'.join(plan)+'\n')
sd_plan=['# SD narrative expansion after SAMPLE-SD-001','','**Proposed reads only.** Original population 19, full original quota **2**, additional next-ranked quota **2**, proposed total **4**. Original ed3e115f81fd126c receives zero final clean credit.','',
    'The confirmed two-line namespace-bullet correction awaits review. Original source-disposition and coordinator rows remain unchanged; this expansion supplies no content verdict.','']
for row in sd_rows:sd_plan.append(f"- `{row['id']}` — `{row['path']}:{row['first_line']}–{row['last_line']}`; original rank {row['original_rank']}; **UNREAD / zero credit**"+(' — pending SD correction path, final overlap check required.' if row['future_SD_correction_path'] else '.'))
sd_plan+=['','[original-units.md](original-units.md) supplies exact whole pinned units. Existing selections, earlier additions and the separate B6 qualification-replacement proposal have no duplicate IDs or other-stratum reductions.',
    'Known accepted changed spans are excluded, not whole files. The still-pending SD correction and final canonical source/comment mapping must be reconciled before clean credit. Actual Astra reading remains pending.',
    'Source `'+PIN+'`. Records-only work: no source audit/research, product edits, SDK/build/test/native/device/formatter/crypto execution, Git mutation or subagent. Finish Core Pass 1 publication, then stop.']
(SD/'read-plan.md').write_text('\n'.join(sd_plan)+'\n')
receipt=dict(result='STRUCTURAL-REPLACEMENT-POOL-REVISION-AND-SEPARATE-SD-EXPANSION; NO CONTENT VERDICTS',source_baseline=PIN,
    original_eight_proposal='BYTE-PRESERVED AS HISTORY',broad_B6_XML_population_retained=104,SW_pool_excluded_ids=len(pool_exclusions),
    available_non_SW_pool=len(available),requested_new_non_SW=4,filled_new_non_SW=len(new_rows),shortfall=needed-len(new_rows),unchanged_predecessor_rows=4,
    successor_family_proposal_count=len(successor),original_global_denominator_change=0,SD_original_quota=2,SD_additional=2,SD_proposed_total=4,
    original_SD_miss_clean_credit=0,new_content_verdicts=0,duplicates_prior_or_known_edited_new_IDs=0,
    final_denominator=None,current_mapping='DEFERRED to final canonical reconciliation',input_sha256=inputs,
    outputs=[dict(path=str(p),bytes=p.stat().st_size,sha256=sha(p.read_bytes())) for directory in [OUT,SD] for p in sorted(directory.iterdir()) if p.is_file() and p.name!='verification.json'])
emit(OUT,'verification.json',receipt)
emit(SD,'verification.json',dict(result='STRUCTURAL-SD-EXPANSION-VERIFIED; NO CONTENT VERDICTS',source_baseline=PIN,original_quota=2,additional_units=2,proposed_total=4,
    miss_final_clean_credit=0,shared_preparation_receipt=str(OUT/'verification.json'),shared_preparation_receipt_sha256=sha((OUT/'verification.json').read_bytes()),
    selection_sha256=sha((SD/'additional-selection.json').read_bytes()),new_units_read_status='UNREAD; clean credit zero',final_denominator=None))
print(json.dumps({k:v for k,v in receipt.items() if k not in ['input_sha256','outputs']},indent=2))
