#!/usr/bin/env python3
"""Four B4 narrative additions and a separate protected-output quota repair."""
import ast
import difflib
import hashlib
import json
import math
from pathlib import Path
import re
import subprocess

RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PREP=RUN/'sampling/preparation';OUT=PREP/'expansion-B4'
PIN='fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef';SEED='core-001-clean-sampling-preparation-v1'
inputs,raw_originals,normalized_originals={},{},{}


def sha(b):return hashlib.sha256(b).hexdigest()


def read(p):
    b=p.read_bytes();inputs[str(p)]=sha(b);return b


def load(p):return json.loads(read(p))


def emit(name,value):(OUT/name).write_text(json.dumps(value,indent=2)+'\n')


read(Path(__file__))
population=load(PREP/'population.json')['units'];byid={r['id']:r for r in population}
selection=load(PREP/'selection.json');original_ids={r['id'] for r in selection};prior_ids=set(original_ids)
for path in ['expansion-A/additional-selection.json','expansion-A-followup/additional-selection.json',
             'expansion-B2/additional-selection.json','expansion-B2/replacement-selection.json','expansion-B2/conditional-history-replacements.json',
             'expansion-B3/additional-selection.json']:
    rows=load(PREP/path);ids={r['id'] for r in rows};assert len(rows)==len(ids) and not ids&prior_ids;prior_ids.update(ids)
assert len(prior_ids)==1054
strata=load(PREP/'strata.json');units={u['id']:u for u in load(RUN/'inventory/semantic-units.json')};files={f['path']:f for f in load(RUN/'inventory/files.json')}
record=load(RUN/'sampling/coordinator/read-B4-verdicts.json')
assert sha(read(RUN/'sampling/coordinator/read-B4.md'))==record['reading_sha256']
assert len(record['units'])==121 and {r['id'] for r in record['units']}=={r['id'] for r in selection if r['owner']=='B4'}
verdicts={r['id']:r for r in record['units']}
for key,row in verdicts.items():assert row['range_sha256']==units[key]['range_sha256']
misses=['core-001:unit:001c58b1d1862d76','core-001:unit:a9e434874ba55d57'];protected='core-001:unit:9ea1794bce816ec8'
assert all(verdicts[k]['verdict']=='DOCUMENTATION-MISS' for k in misses)
assert verdicts[protected]['verdict']=='KNOWN-DEFERRED-PROTECTED-OUTPUT'

# Reuse only prior pure helper definitions, not any original preparation writer.
original_helper=read(PREP/'expansion-B2/expand.py');parser_helper=read(PREP/'prepare.py')
namespace=dict(subprocess=subprocess,PIN=PIN,files=files,raw_originals=raw_originals,normalized_originals=normalized_originals,sha=sha,Path=Path,read=read,re=re)
for data,name,path in [(original_helper,'original',PREP/'expansion-B2/expand.py'),(parser_helper,'parse_patch',PREP/'prepare.py')]:
    node=next(n for n in ast.parse(data).body if isinstance(n,ast.FunctionDef) and n.name==name)
    exec(compile(ast.Module(body=[node],type_ignores=[]),str(path),'exec'),namespace)
original=namespace['original'];namespace['original']=original
roles=['narrative-examples-and-maps','implementation-state-and-internal-contracts']
candidate_paths={units[r['id']]['path'] for r in population if r['owner']=='B4' and r.get('stratum') in roles and r['eligibility']=='candidate-eligible'}
manifest=load(RUN/'editing/integration/current-products.json');assert manifest['source_baseline']==PIN
accepted={r['path']:r['sha256'] for r in manifest['product_files']}
for path,digest in accepted.items():assert sha(Path(path).read_bytes())==digest,('Accepted current product changed',path)
for name in manifest['receipt_order']:load(RUN/'editing/integration'/name)
relevant=sorted(candidate_paths&accepted.keys())
command=['git','--no-optional-locks','diff','--no-ext-diff','--no-textconv','--no-renames','--unified=3','--src-prefix=a/','--dst-prefix=b/',PIN,'--',*relevant]
current_patch=subprocess.check_output(command);current_patch_path=OUT/'accepted-current-B4-role-scope.diff';current_patch_path.write_bytes(current_patch)
spans=namespace['parse_patch'](current_patch_path)

# The focused reviewed patch is pending transfer. Its immutable before/after captures let us
# map its effective original-coordinate overlap without editing or polling primary/worktrees.
focused_dir=Path('/home/rupicola/Code/y/yubikit-core-docs-edit-guides')/RUN/'editing/SHARED/sampling-type-guidance'
focused_review=load(focused_dir/'review-identities.json');assert focused_review['verdict']=='PASS'
focused_patch=read(focused_dir/'product.patch');assert sha(focused_patch)==focused_review['accepted_focused_patch_sha256']=='3c066d098c68a289da1ebc4dda2ded4fb2db66357f07907f5269a7061b334623'
effective_parts=[];focused_paths=[]
for row in focused_review['accepted_paths']:
    path=row['path'];name=Path(path).name
    before=read(focused_dir/'before'/name);after=read(focused_dir/'after'/name)
    assert sha(before)==row['before_sha256'] and sha(after)==row['after_sha256']
    assert accepted[path] in [row['before_sha256'],row['after_sha256']]
    original(path);base=normalized_originals[path];successor=after.decode('utf-8-sig').replace('\r\n','\n')
    assert base.endswith('\n') and successor.endswith('\n')
    effective_parts.append(f'diff --git a/{path} b/{path}\n'+''.join(difflib.unified_diff(base.splitlines(keepends=True),successor.splitlines(keepends=True),fromfile='a/'+path,tofile='b/'+path,n=3)))
    focused_paths.append(path)
effective=''.join(effective_parts).encode();effective_path=OUT/'reviewed-focused-effective-original-scope.diff';effective_path.write_bytes(effective)
focused_spans=namespace['parse_patch'](effective_path)
for path,rows in focused_spans.items():spans[path]=rows


def overlap(key):
    u=units[key];a,b=u['start_character'],u['end_character_exclusive']
    return [s for s in spans.get(u['path'],[]) if (a<s['end_character_exclusive'] and s['start_character']<b
        if s['start_character']!=s['end_character_exclusive'] else a<=s['start_character']<=b)]


def rank_for(role):
    keys={r['id'] for r in population if r['owner']=='B4' and r.get('stratum')==role and r['eligibility']=='candidate-eligible'}
    return sorted(keys,key=lambda key:sha((SEED+'|B4|'+role+'|'+key).encode()))


def row_for(key,purpose,rank):
    u=units[key];body=original(u['path'])[u['start_character']:u['end_character_exclusive']];assert sha(body.encode())==u['range_sha256']
    return dict(id=key,owner='B4',stratum=byid[key]['stratum'],purpose=purpose,original_rank=rank.index(key)+1,
        **{f:u[f] for f in ['path','first_line','last_line','start_character','end_character_exclusive','source_sha256','range_sha256','category','label']},
        known_edited_overlap=False,read_status='PROPOSED-UNREAD',verdict='UNASSIGNED',clean_credit=0,
        current_mapping='DEFERRED to final canonical reconciliation; original pinned unit may be read now')


role='narrative-examples-and-maps';q=next(q for q in strata if q['owner']=='B4' and q['stratum']==role);rank=rank_for(role)
assert q['proposed_selected']==q['minimum_10_percent']==4 and q['selected_ids']==rank[:4]
available=[k for k in rank if k not in prior_ids and not byid[k].get('edited_overlap') and not overlap(k)]
chosen=available[:4];assert len(chosen)==4
expansion=[row_for(k,'MATERIAL-MISS-FULL-ORIGINAL-NARRATIVE-QUOTA',rank) for k in chosen]
used=prior_ids|set(chosen)
role='implementation-state-and-internal-contracts';replacement_quota=next(q for q in strata if q['owner']=='B4' and q['stratum']==role);replacement_rank=rank_for(role)
assert protected in replacement_quota['selected_ids']
edited={k for k in replacement_rank if overlap(k)};denominator=set(replacement_rank)-{protected}-edited
valid=set(replacement_quota['selected_ids'])-{protected}-edited
minimum=max(1,math.ceil(len(denominator)/10));needed=max(0,minimum-len(valid))
available=[k for k in replacement_rank if k not in used and k in denominator and not byid[k].get('edited_overlap')]
replacements=available[:needed];assert len(replacements)==needed
replacement_rows=[row_for(k,'PROTECTED-OUTPUT-ELIGIBILITY-REPLACEMENT; NOT A NEW RUNTIME FINDING',replacement_rank) for k in replacements]
all_new=expansion+replacement_rows;assert len(all_new)==len({r['id'] for r in all_new}) and not {r['id'] for r in all_new}&prior_ids
miss_overlay=[]
for key in misses:
    u=units[key];body=original(u['path'])[u['start_character']:u['end_character_exclusive']];assert sha(body.encode())==u['range_sha256']
    miss_overlay.append(dict(id=key,owner='B4',stratum='narrative-examples-and-maps',coordinator_finding='SAMPLE-B4-001' if key==misses[0] else 'SAMPLE-B4-002',
        original_ledger_status='READ-CLEAN-PRESERVED-AS-HISTORY',coordinator_verdict=verdicts[key]['verdict'],coordinator_note=verdicts[key]['note'],
        final_clean_credit=0,source_sha256=u['source_sha256'],range_sha256=u['range_sha256'],
        correction='Focused two-document patch has independent PASS at 3c066d098c68a289da1ebc4dda2ded4fb2db66357f07907f5269a7061b334623; transfer/final canonical mapping remain separate.'))
output_overlay=dict(id=protected,owner='B4',stratum=byid[protected]['stratum'],original_ledger_status='READ-CLEAN-PRESERVED-AS-HISTORY',
    coordinator_verdict=verdicts[protected]['verdict'],reason=verdicts[protected]['note'],final_clean_credit=0,
    classification='Protected runtime help/non-D output with existing NoTests policy discrepancy; eligibility exclusion, not a new runtime finding.',
    runtime='UNCHANGED; this preparation edits no runtime help and allocates no new engineering ID')
for path,digest in inputs.items():assert sha(Path(path).read_bytes())==digest,('Bound input changed',path)
for path,digest in accepted.items():assert sha(Path(path).read_bytes())==digest,('Accepted current product changed during capture',path)
emit('additional-selection.json',expansion);emit('replacement-selection.json',replacement_rows);emit('miss-overlay.json',miss_overlay);emit('protected-output-overlay.json',output_overlay)
emit('quotas.json',dict(material_narrative=dict(original_population=len(rank),original_full_quota=4,additional_full_quota=4,proposed_total=8,known_miss_clean_credit=0),
    protected_output_replacement=dict(original_population=len(replacement_rank),original_selected_quota=replacement_quota['proposed_selected'],
        protected_exclusion=protected,exact_accepted_or_reviewed_overlap_ids=sorted(edited),working_population=len(denominator),
        working_minimum=minimum,valid_original_selected=len(valid),required_replacements=needed,replacement_ids=replacements),
    qualification='Candidate role-level calculation only; final canonical denominator and actual Astra replacement reading remain pending.'))
emit('accepted-overlap-inputs.json',dict(manifest='editing/integration/current-products.json',manifest_sha256=inputs[str(RUN/'editing/integration/current-products.json')],
    accepted_paths=len(accepted),accepted_target_role_paths=relevant,command=command,current_patch_sha256=sha(current_patch),
    focused_review_record=str(focused_dir/'review-identities.json'),focused_patch_sha256=sha(focused_patch),focused_paths=focused_paths,
    focused_effective_diff_sha256=sha(effective),scope='Accepted current target-role edits plus reviewed-pending two-document successor; exact original spans, not whole files.',
    original_changed_spans=spans,parser_reuse_sha256=sha(parser_helper)))
text=['# B4 expansion and protected-output replacement whole units','','**No content verdicts.** Pinned source `'+PIN+'`; read complete units and relevant enclosing context.','']
for row in all_new:
    body=original(row['path'])[row['start_character']:row['end_character_exclusive']]
    text+=['## `'+row['id']+'`',f"{row['purpose']}; `{row['path']}:{row['first_line']}–{row['last_line']}`; original rank {row['original_rank']}.",'','````'+('csharp' if row['path'].endswith('.cs') else 'markdown'),body,'````','']
(OUT/'original-units.md').write_text('\n'.join(text)+'\n')
plan=['# B4 narrative expansion and separate protected-output quota repair','','**Additional proposed reading only.** All earlier selections, population, ledgers and coordinator verdicts remain unchanged.','',
    'B4 narrative: original population 38, full original quota **4**, additional next-ranked quota **4**, proposed total **8**. Both SAMPLE-B4-001/002 units have zero final clean credit.',
    'Original A/B1/B2/B3/B4 actual selected-read total is **638**. The other 345 original selected units remain unread according to the coordinator handoff; added/replacement units are separate and still unread.','',
    '## Four new narrative reads']
for row in expansion:plan.append(f"- `{row['id']}` — `{row['path']}:{row['first_line']}–{row['last_line']}`; original rank {row['original_rank']}; **UNREAD / zero credit**.")
plan+=['','## Separate protected-output exclusion/replacement',
    'Original 9ea1794bce816ec8 runtime-help unit receives zero final clean-quota credit. The known NoTests-policy discrepancy is retained as existing non-D/protected output; it is not a new runtime finding and runtime text remains unchanged.',
    f"Same-stratum working population {len(denominator)}, 10% minimum {minimum}, original valid selected {len(valid)}; **{needed} deterministic replacement(s) required**. This repair is separate from the four-unit material-miss expansion."]
for row in replacement_rows:plan.append(f"- `{row['id']}` — `{row['path']}:{row['first_line']}–{row['last_line']}`; original rank {row['original_rank']}; **UNREAD / zero credit**.")
plan+=['','## Exact inputs and remaining gates',
    'The accepted current-products manifest and reviewed focused two-document patch 3c066d... bind edit exclusions. The focused patch is independently PASS but transfer-pending at handoff; its immutable before/after snapshots are mapped back to original coordinates. No whole-file exclusions or live source chasing.',
    '[original-units.md](original-units.md) supplies every new complete pinned unit. JSON selections retain original character/line ranges, source/range hashes and pending verdicts.',
    'Final combined comment mapping, remaining validation/sampling patches, final eligibility and actual Astra reading/verdicts remain separate. Other strata are not reduced; the broad code-only/private/internal/test population is unchanged except already adjudicated specific exclusions.',
    'No content verdict, source audit, product/runtime change, SDK/build/test/native/device/formatter/crypto execution, Git mutation or subagent. Finish Core Pass 1 publication, then stop.']
(OUT/'read-plan.md').write_text('\n'.join(plan)+'\n')
receipt=dict(result='STRUCTURAL-B4-EXPANSION-AND-SEPARATE-REPLACEMENT-VERIFIED; NO CONTENT VERDICTS',source_baseline=PIN,
    original_B4_reads_reconciled=121,original_A_B1_B2_B3_B4_read_total=638,other_original_selected_units_unread=345,
    original_narrative_quota=4,additional_narrative=4,proposed_narrative_total=8,known_material_miss_clean_credit=0,
    protected_output_exclusions=1,protected_output_clean_credit=0,deterministic_same_stratum_replacements=needed,new_runtime_findings=0,
    overlaps_prior_selections_or_known_edited_new_IDs=0,other_owner_role_reductions=0,accepted_manifest_paths=len(accepted),
    focused_reviewed_patch_sha256=sha(focused_patch),focused_transfer='PENDING at coordinator handoff; effective overlap already excluded',
    final_denominator=None,current_mapping='DEFERRED to final canonical reconciliation',input_sha256=inputs,
    outputs=[dict(path=str(p),bytes=p.stat().st_size,sha256=sha(p.read_bytes())) for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='verification.json'])
emit('verification.json',receipt)
print(json.dumps({k:v for k,v in receipt.items() if k not in ['input_sha256','outputs']},indent=2))
