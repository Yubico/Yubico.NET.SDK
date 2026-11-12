#!/usr/bin/env python3
"""B5 specific eligibility repairs and B6 same-role source-qualification replacements."""
import ast
import hashlib
import json
import math
from pathlib import Path
import re
import subprocess

RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PREP=RUN/'sampling/preparation';OUT=PREP/'family-replacements'
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
             'expansion-B3/additional-selection.json','expansion-B4/additional-selection.json','expansion-B4/replacement-selection.json']:
    rows=load(PREP/path);ids={r['id'] for r in rows};assert len(rows)==len(ids) and not ids&prior_ids;prior_ids.update(ids)
assert len(prior_ids)==1059
strata=load(PREP/'strata.json');units={u['id']:u for u in load(RUN/'inventory/semantic-units.json')};files={f['path']:f for f in load(RUN/'inventory/files.json')}
verdicts={};read_counts={}
for owner,expected in [('B5',73),('B6',29),('C',62)]:
    record=load(RUN/'sampling/coordinator'/('read-'+owner+'-verdicts.json'))
    assert sha(read(RUN/'sampling/coordinator'/('read-'+owner+'.md')))==record['reading_sha256']
    assert len(record['units'])==expected and {r['id'] for r in record['units']}=={r['id'] for r in selection if r['owner']==owner}
    for row in record['units']:assert row['range_sha256']==units[row['id']]['range_sha256'];verdicts[row['id']]=row
    read_counts[owner]=expected
assert all(verdicts[r['id']]['verdict']=='SOURCE-CONTENT-SUPPORTED-PENDING-FINAL-ELIGIBILITY' for r in selection if r['owner']=='C')
b5_keys=['core-001:unit:c7319c1c1182a4be','core-001:unit:5bcf834655fef594']
b6_xml=[k for k,row in verdicts.items() if byid[k]['owner']=='B6' and row['verdict']=='SOURCE-QUALIFIED-NORMATIVE-GAP']
b6_reference=[k for k,row in verdicts.items() if byid[k]['owner']=='B6' and row['verdict']=='REFERENCE-DATA-QUALIFIED']
assert all(verdicts[k]['verdict']=='INELIGIBLE-ADMINISTRATIVE' for k in b5_keys)
assert len(b6_xml)==5 and len(b6_reference)==1 and b6_reference[0]=='core-001:unit:59c33b8c7c8f294c'
assert all(byid[k]['stratum']=='XML-contracts' for k in b6_xml)
assert all(byid[k]['stratum']=='tests-and-helpers' for k in b5_keys+b6_reference)

original_helper=read(PREP/'expansion-B2/expand.py');parser_helper=read(PREP/'prepare.py')
namespace=dict(subprocess=subprocess,PIN=PIN,files=files,raw_originals=raw_originals,normalized_originals=normalized_originals,sha=sha,Path=Path,read=read,re=re)
for data,name,path in [(original_helper,'original',PREP/'expansion-B2/expand.py'),(parser_helper,'parse_patch',PREP/'prepare.py')]:
    node=next(n for n in ast.parse(data).body if isinstance(n,ast.FunctionDef) and n.name==name)
    exec(compile(ast.Module(body=[node],type_ignores=[]),str(path),'exec'),namespace)
original=namespace['original'];namespace['original']=original
roles={('B5','tests-and-helpers'),('B6','XML-contracts'),('B6','tests-and-helpers')}
candidate_paths={units[r['id']]['path'] for r in population if (r['owner'],r.get('stratum')) in roles and r['eligibility']=='candidate-eligible'}
manifest=load(RUN/'editing/integration/current-products.json');assert manifest['source_baseline']==PIN
accepted={r['path']:r['sha256'] for r in manifest['product_files']}
for path,digest in accepted.items():assert sha(Path(path).read_bytes())==digest,('Accepted current product changed',path)
for name in manifest['receipt_order']:load(RUN/'editing/integration'/name)
assert 'accepted-sampling-B4.json' in manifest['receipt_order']
relevant=sorted(candidate_paths&accepted.keys())
command=['git','--no-optional-locks','diff','--no-ext-diff','--no-textconv','--no-renames','--unified=3','--src-prefix=a/','--dst-prefix=b/',PIN,'--',*relevant]
patch=subprocess.check_output(command);patch_path=OUT/'accepted-current-family-role-scope.diff';patch_path.write_bytes(patch)
spans=namespace['parse_patch'](patch_path)


def overlap(key):
    u=units[key];a,b=u['start_character'],u['end_character_exclusive']
    return [s for s in spans.get(u['path'],[]) if (a<s['end_character_exclusive'] and s['start_character']<b
        if s['start_character']!=s['end_character_exclusive'] else a<=s['start_character']<=b)]


all_replacements=[];quota_rows=[];used=set(prior_ids)
configs=[('B5','tests-and-helpers',b5_keys,'SPECIFIC-ADMINISTRATIVE-ELIGIBILITY-REPLACEMENT'),
         ('B6','XML-contracts',b6_xml,'SOURCE-QUALIFICATION-REPLACEMENT; NOT DOCUMENTATION-DEFECT-EXPANSION'),
         ('B6','tests-and-helpers',b6_reference,'REFERENCE-DATA-QUALIFICATION-REPLACEMENT; NOT DOCUMENTATION-DEFECT-EXPANSION')]
for owner,role,withheld,purpose in configs:
    q=next(q for q in strata if q['owner']==owner and q['stratum']==role)
    eligible={r['id'] for r in population if r['owner']==owner and r.get('stratum')==role and r['eligibility']=='candidate-eligible'}
    rank=sorted(eligible,key=lambda key:sha((SEED+'|'+owner+'|'+role+'|'+key).encode()))
    assert q['selected_ids']==rank[:q['proposed_selected']]
    # B6 keeps its broad original denominator: withholding source-qualified sample credit does
    # not shrink SWConstants or convert unavailable clauses/reference provenance into acceptance.
    denominator=eligible-set(withheld) if owner=='B5' else eligible
    minimum=max(1,math.ceil(len(denominator)/10));edited_selected={k for k in q['selected_ids'] if overlap(k)}
    valid=set(q['selected_ids'])-set(withheld)-edited_selected
    if owner=='B6':
        needed=len(withheld)
        assert len(valid)+needed>=minimum, 'Additional accepted overlap needs separate final canonical reconciliation'
    else:needed=max(0,minimum-len(valid))
    available=[k for k in rank if k not in used and k not in withheld and not byid[k].get('edited_overlap') and not overlap(k)]
    chosen=available[:needed];assert len(chosen)==needed
    quota_rows.append(dict(owner=owner,stratum=role,original_population=len(eligible),working_population=len(denominator),
        conservative_denominator_policy='Original broad role population retained; only six sampled units lose clean credit, no whole class narrowed' if owner=='B6' else
            'Only the two explicitly adjudicated preambles removed; no other code/private/test eligibility narrowed',
        original_quota=q['proposed_selected'],working_minimum=minimum,withheld_sample_ids=withheld,known_edited_original_selected_ids=sorted(edited_selected),
        valid_original_selected=len(valid),required_replacements=needed,replacement_ids=chosen,purpose=purpose,
        final_denominator=None,content_verdicts='PENDING actual Astra reading; these counts are not clean acceptance'))
    for key in chosen:
        u=units[key];body=original(u['path'])[u['start_character']:u['end_character_exclusive']];assert sha(body.encode())==u['range_sha256']
        all_replacements.append(dict(id=key,owner=owner,stratum=role,purpose=purpose,original_rank=rank.index(key)+1,
            **{f:u[f] for f in ['path','first_line','last_line','start_character','end_character_exclusive','source_sha256','range_sha256','category','label']},
            known_edited_overlap=False,prior_selected=False,read_status='REPLACEMENT-PROPOSED-UNREAD',verdict='UNASSIGNED',clean_credit=0,
            same_SWConstants_source_gap_path=u['path']=='src/Core/src/Protocols/SmartCard/Apdu/SWConstants.cs',
            qualification='Same role is not a promise of resolved normative or original-archive provenance. Astra must check exact scope; retain any source gaps, not automatically clean.',
            current_mapping='DEFERRED until final canonical reconciliation; original pinned unit may be read now'))
    used.update(chosen)
assert [q['required_replacements'] for q in quota_rows]==[2,5,1]
assert len(all_replacements)==len({r['id'] for r in all_replacements})==8 and not {r['id'] for r in all_replacements}&prior_ids
overlay=[]
for key in b5_keys+b6_xml+b6_reference:
    u=units[key];body=original(u['path'])[u['start_character']:u['end_character_exclusive']];assert sha(body.encode())==u['range_sha256']
    overlay.append(dict(id=key,owner=byid[key]['owner'],stratum=byid[key]['stratum'],path=u['path'],
        original_ledger_status='READ-CLEAN-PRESERVED-AS-HISTORY',coordinator_verdict=verdicts[key]['verdict'],coordinator_note=verdicts[key]['note'],
        actually_read_by='Astra; original read evidence retained',source_sha256=u['source_sha256'],range_sha256=u['range_sha256'],final_clean_credit=0,
        classification='Specific administrative-preamble eligibility adjudication' if key in b5_keys else 'Source/evidence-qualified context only; not a new documentation defect',
        runtime='UNCHANGED; no crypto outputs recomputed or original clauses/archive supplied'))
for path,digest in inputs.items():assert sha(Path(path).read_bytes())==digest,('Bound input changed',path)
for path,digest in accepted.items():assert sha(Path(path).read_bytes())==digest,('Accepted current product changed during capture',path)
emit('replacement-selection.json',all_replacements);emit('eligibility-and-qualification-overlay.json',overlay);emit('quotas.json',quota_rows)
emit('accepted-overlap-inputs.json',dict(manifest='editing/integration/current-products.json',manifest_sha256=inputs[str(RUN/'editing/integration/current-products.json')],
    accepted_paths=len(accepted),target_role_paths=relevant,command=command,diff_sha256=sha(patch),diff_bytes=len(patch),original_changed_spans=spans,
    parser_reuse_sha256=sha(parser_helper),scope='Exact known accepted target-role changed spans; no whole-file/class exclusion'))
text=['# Core family eligibility/qualification replacement whole units','','**No content verdicts.** Pinned source `'+PIN+'`; read exact whole units and enclosing context.','']
for row in all_replacements:
    body=original(row['path'])[row['start_character']:row['end_character_exclusive']]
    text+=['## `'+row['id']+'`',f"{row['owner']} {row['stratum']}; `{row['path']}:{row['first_line']}–{row['last_line']}`; original rank {row['original_rank']}.",'','````'+('csharp' if row['path'].endswith('.cs') else 'text'),body,'````','']
(OUT/'original-units.md').write_text('\n'.join(text)+'\n')
plan=['# Core family sampling replacements — no defect expansion','','**Proposed reading only.** All original 983 selections, prior additions/replacements, source-disposition ledgers and coordinator verdicts remain byte-preserved.','',
    'Astra has read all **802 original Core-family samples**. The remaining 181 original selected units belong to boundary owners and remain unread according to this handoff. Added/replacement reads are separate.',
    'C: all 62 source contents are supported pending final mapping, with original RFC/OID/vector checks retained in Astra’s verdict record. No new C exclusion or extra source/provider/runtime acceptance is inferred.','',
    '| Owner/role | Original population | Working population | Original quota / working minimum | Valid original after specific withholding | Replacements |',
    '| --- | ---: | ---: | ---: | ---: | ---: |']
for q in quota_rows:plan.append(f"| {q['owner']} {q['stratum']} | {q['original_population']} | {q['working_population']} | {q['original_quota']} / {q['working_minimum']} | {q['valid_original_selected']} | {q['required_replacements']} |")
plan+=['','## Coordinator reading order','[original-units.md](original-units.md) supplies all eight complete pinned units; exact source/range hashes and pending verdicts are in `replacement-selection.json`.']
for row in all_replacements:plan.append(f"- `{row['id']}` — `{row['path']}:{row['first_line']}–{row['last_line']}`; {row['owner']} {row['stratum']}; rank {row['original_rank']}; **UNREAD / zero credit**"+(' — same SWConstants path, inherited source-gap qualification still requires exact review.' if row['same_SWConstants_source_gap_path'] else '.'))
plan+=['','## Separate exclusions and qualifications',
    'B5 c7319c1c1182a4be and 5bcf834655fef594 are specific pure-preamble exclusions, not documentation misses. Their role population is reduced only by those two adjudicated IDs (503 → 501); its 10% minimum remains 51, requiring two replacements.',
    'B6 five exact SWConstants sample IDs retain SOURCE-QUALIFIED-NORMATIVE-GAP. Their declaration meanings were read; exact original ISO clauses/revision remain unavailable. They have zero final clean credit, and no unknown clause is declared verified.',
    'B6 59c33b8c7c8f294c retains REFERENCE-DATA-QUALIFIED: the complete CAVS response file was read as reference data; original NIST archive provenance gap remains and no crypto output was recomputed. It has zero final clean-contract credit.',
    'B6 broad XML/test role populations stay **104 / 128**, with original quotas **11 / 13**. Five XML plus one same-role test unit are proposed replacements for six withheld sample slots, without shrinking the entire SWConstants class or broad denominator. These are qualification replacements, not new defect/category expansion; clean credit still requires actual reading and source acceptance.',
    'Replacement selection is deterministic in the original fixed-seed rank, skipping all prior selected IDs and exact known accepted changed spans. No unrelated code-only/private/internal/test exclusions or other-stratum reductions are introduced.',
    'A new same-class replacement may retain its own original-source gap. Same-role selection is not automatic content/source acceptance; Astra must read and adjudicate each exact claim. Current source mappings and the final canonical denominator remain separate pending gates.',
    'Known accepted current-product records include the integrated B4 two-guide patch. Final combined validation/sampling reconciliation remains pending; no source-content verdict is supplied by this preparer.',
    'Records-only rank/hash/index work. No source audit, product edits, SDK/build/test/native/device/formatter/crypto execution, Git mutation or subagent. Finish Core Pass 1 publication, then stop.']
(OUT/'read-plan.md').write_text('\n'.join(plan)+'\n')
receipt=dict(result='STRUCTURAL-FAMILY-REPLACEMENT-PREPARATION-VERIFIED; NO CONTENT VERDICTS',source_baseline=PIN,
    original_B5_reads_reconciled=73,original_B6_reads_reconciled=29,original_C_reads_reconciled=62,original_Core_family_reads=802,
    other_original_boundary_samples_unread=181,B5_specific_administrative_exclusions=2,B5_required_test_replacements=2,
    B6_normative_gap_XML_replacements=5,B6_reference_data_test_replacements=1,B6_qualification_replacements_total=6,
    broad_B6_XML_population_retained=104,broad_B6_test_population_retained=128,material_doc_defect_expansion=0,
    all_new_replacement_IDs=8,prior_or_known_edit_overlap_new_IDs=0,all_new_clean_credit=0,withheld_original_sample_credit=0,
    accepted_manifest_paths=len(accepted),accepted_target_role_paths=len(relevant),integrated_B4_patch_receipt_present=True,
    final_denominator=None,current_mapping='DEFERRED to final canonical reconciliation',input_sha256=inputs,
    outputs=[dict(path=str(p),bytes=p.stat().st_size,sha256=sha(p.read_bytes())) for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='verification.json'])
emit('verification.json',receipt)
print(json.dumps({k:v for k,v in receipt.items() if k not in ['input_sha256','outputs']},indent=2))
