#!/usr/bin/env python3
"""One WA full test quota plus distinct specific-eligibility repairs; records only."""
import ast
import hashlib
import json
import math
from pathlib import Path
import re
import subprocess

RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PREP=RUN/'sampling/preparation';OUT=PREP/'expansion-WebAuthn'
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
             'expansion-B3/additional-selection.json','expansion-B4/additional-selection.json','expansion-B4/replacement-selection.json',
             'family-replacements/replacement-selection.json','family-replacements/revision-2/new-non-SW-selection.json',
             'expansion-SecurityDomain/additional-selection.json']:
    rows=load(PREP/path);ids={r['id'] for r in rows};assert len(rows)==len(ids) and not ids&prior_ids;prior_ids.update(ids)
strata=load(PREP/'strata.json');units={u['id']:u for u in load(RUN/'inventory/semantic-units.json')};files={f['path']:f for f in load(RUN/'inventory/files.json')}
parsed={}
for owner,expected in [('Oath',18),('WebAuthn',19)]:
    data=read(RUN/'sampling/coordinator'/('read-boundary-'+owner+'-verdicts.md')).decode()
    # Parse only exact table cells, not arbitrary IDs appearing in narrative/citation text.
    rows=[dict(suffix=m.group(1),id='core-001:unit:'+m.group(1),verdict_text=m.group(2)) for line in data.splitlines()
          if (m:=re.fullmatch(r'\| `([0-9a-f]{16})` \| (.*) \|',line))]
    assert len(rows)==len({r['id'] for r in rows})==expected
    assert {r['id'] for r in rows}=={r['id'] for r in selection if r['owner']==owner}
    parsed[owner]=rows
wa_verdicts={r['id']:r for r in parsed['WebAuthn']}
miss='core-001:unit:e29522f04d40349a';invalid_tests=['core-001:unit:7fe08c46f10afee5','core-001:unit:c6719030f9881ef4'];invalid_narrative='core-001:unit:c8377e0a58dac11c'
assert 'SAMPLE-WA-001 documentation miss' in wa_verdicts[miss]['verdict_text']
assert all(wa_verdicts[k]['verdict_text'].startswith('Ineligible preamble:') for k in invalid_tests)
assert wa_verdicts[invalid_narrative]['verdict_text'].startswith('Administrative maintenance policy:')
assert all(r['verdict_text'].startswith('Supported') for r in parsed['Oath'])

original_helper=read(PREP/'expansion-B2/expand.py');parser_helper=read(PREP/'prepare.py')
namespace=dict(subprocess=subprocess,PIN=PIN,files=files,raw_originals=raw_originals,normalized_originals=normalized_originals,sha=sha,Path=Path,read=read,re=re)
for data,name,path in [(original_helper,'original',PREP/'expansion-B2/expand.py'),(parser_helper,'parse_patch',PREP/'prepare.py')]:
    node=next(n for n in ast.parse(data).body if isinstance(n,ast.FunctionDef) and n.name==name)
    exec(compile(ast.Module(body=[node],type_ignores=[]),str(path),'exec'),namespace)
original=namespace['original'];namespace['original']=original
roles=['tests-and-helpers','narrative-examples-and-maps']
candidate_paths={units[r['id']]['path'] for r in population if r['owner']=='WebAuthn' and r.get('stratum') in roles and r['eligibility']=='candidate-eligible'}
manifest=load(RUN/'editing/integration/current-products.json');assert manifest['source_baseline']==PIN
accepted={r['path']:r['sha256'] for r in manifest['product_files']}
for path,digest in accepted.items():assert sha(Path(path).read_bytes())==digest,('Accepted current product changed',path)
for name in manifest['receipt_order']:load(RUN/'editing/integration'/name)
relevant=sorted(candidate_paths&accepted.keys())
command=['git','--no-optional-locks','diff','--no-ext-diff','--no-textconv','--no-renames','--unified=3','--src-prefix=a/','--dst-prefix=b/',PIN,'--',*relevant]
patch=subprocess.check_output(command);patch_path=OUT/'accepted-current-WebAuthn-role-scope.diff';patch_path.write_bytes(patch)
spans=namespace['parse_patch'](patch_path)
focused_dir=Path('/home/rupicola/Code/y/yubikit-core-docs-edit-fido-boundaries')/RUN/'editing/FIDO-boundaries/sampling-selection-oracle'
review=load(focused_dir/'review-identities.json');assert review['outcome']=='PASS' and review['sample']=='SAMPLE-WA-001'
focused=read(focused_dir/'focused.patch');assert sha(focused)==review['focused_patch_sha256']=='d70a50aaf297cc62fd6c7fa93f13e8cfb1acb44fe975279dc9fb60fc30e5b05e'
before=read(focused_dir/'before.snapshot');focused_path=review['product']['path']
assert sha(before)==review['product']['before_sha256']==files[focused_path]['sha256']
assert sha(Path(focused_path).read_bytes())==review['product']['before_sha256'], 'Primary is reserved: expected unchanged pre-transfer focused fixture.'
pending_spans=namespace['parse_patch'](focused_dir/'focused.patch')
assert focused_path not in spans, 'Focused fixture unexpectedly also has an accepted baseline delta; reconcile rather than merge coordinates blindly.'
spans.update(pending_spans)


def overlap(key):
    u=units[key];a,b=u['start_character'],u['end_character_exclusive']
    return [s for s in spans.get(u['path'],[]) if (a<s['end_character_exclusive'] and s['start_character']<b
        if s['start_character']!=s['end_character_exclusive'] else a<=s['start_character']<=b)]


def rank_for(role):
    keys={r['id'] for r in population if r['owner']=='WebAuthn' and r.get('stratum')==role and r['eligibility']=='candidate-eligible'}
    return sorted(keys,key=lambda key:sha((SEED+'|WebAuthn|'+role+'|'+key).encode()))


def row_for(key,purpose,rank):
    u=units[key];body=original(u['path'])[u['start_character']:u['end_character_exclusive']];assert sha(body.encode())==u['range_sha256']
    return dict(id=key,owner='WebAuthn',stratum=byid[key]['stratum'],purpose=purpose,original_rank=rank.index(key)+1,
        **{f:u[f] for f in ['path','first_line','last_line','start_character','end_character_exclusive','source_sha256','range_sha256','category','label']},
        known_edited_overlap=False,prior_selected=False,read_status='PROPOSED-UNREAD',verdict='UNASSIGNED',clean_credit=0,
        current_mapping='DEFERRED until final canonical reconciliation; primary remains untouched during compile cycle')


role='tests-and-helpers';test_quota=next(q for q in strata if q['owner']=='WebAuthn' and q['stratum']==role);rank=rank_for(role)
assert test_quota['proposed_selected']==test_quota['minimum_10_percent']==11 and test_quota['selected_ids']==rank[:11]
available=[k for k in rank if k not in prior_ids and not byid[k].get('edited_overlap') and not overlap(k)]
new_ids=available[:11];assert len(new_ids)==11
expansion=[row_for(k,'SAMPLE-WA-001-FULL-ORIGINAL-TEST-HELPER-QUOTA-EXPANSION',rank) for k in new_ids]
used=prior_ids|set(new_ids);replacement_rows=[];replacement_quotas=[]
for role,invalid in [('tests-and-helpers',invalid_tests),('narrative-examples-and-maps',[invalid_narrative])]:
    q=next(q for q in strata if q['owner']=='WebAuthn' and q['stratum']==role);rank=rank_for(role)
    denominator=set(rank)-set(invalid)
    # Repair the slots lost specifically to administrative eligibility. The distinct material miss
    # is never credited: its withheld slot is addressed by the full 11-unit expansion above.
    other_edited_selected={k for k in q['selected_ids'] if k!=miss and overlap(k)}
    retained_slots=set(q['selected_ids'])-set(invalid)-other_edited_selected
    minimum=max(1,math.ceil(len(denominator)/10));needed=max(0,minimum-len(retained_slots))
    available=[k for k in rank if k not in used and k not in invalid and not byid[k].get('edited_overlap') and not overlap(k)]
    keys=available[:needed];assert len(keys)==needed
    replacement_rows += [row_for(k,'SPECIFIC-ADMINISTRATIVE-ELIGIBILITY-REPLACEMENT; SEPARATE FROM MATERIAL-MISS EXPANSION',rank) for k in keys];used.update(keys)
    replacement_quotas.append(dict(owner='WebAuthn',stratum=role,original_population=len(rank),working_population=len(denominator),
        original_quota=q['proposed_selected'],working_minimum=minimum,specific_ineligible_ids=invalid,
        retained_original_slots_after_eligibility_only=len(retained_slots),material_miss_in_retained_slots=int(miss in retained_slots),
        actual_original_clean_credit_candidates_after_miss=len(retained_slots-{miss}),other_edited_selected=sorted(other_edited_selected),
        required_eligibility_replacements=needed,replacement_ids=keys,
        qualification='Slot repair is separate from the 11-unit miss expansion; SAMPLE-WA-001 receives zero credit throughout. Final canonical quotas and actual replacement reads remain pending.'))
assert [q['required_eligibility_replacements'] for q in replacement_quotas]==[2,1]
assert len(expansion+replacement_rows)==len({r['id'] for r in expansion+replacement_rows})==14
assert not {r['id'] for r in expansion+replacement_rows}&prior_ids
miss_overlay=dict(id=miss,owner='WebAuthn',stratum='tests-and-helpers',coordinator_finding='SAMPLE-WA-001',
    original_ledger_status='READ-CLEAN-PRESERVED-AS-HISTORY',coordinator_verdict_text=wa_verdicts[miss]['verdict_text'],final_clean_credit=0,
    source_sha256=units[miss]['source_sha256'],range_sha256=units[miss]['range_sha256'],
    correction='Focused d70a50aa... independently PASS; queued until compile cycle releases primary. No source transfer or content re-review by preparer.')
invalid_overlay=[dict(id=k,owner='WebAuthn',stratum=byid[k]['stratum'],original_ledger_status='READ-CLEAN-PRESERVED-AS-HISTORY',
    coordinator_verdict_text=wa_verdicts[k]['verdict_text'],final_clean_credit=0,
    classification='Specific pure-preamble exclusion' if k in invalid_tests else 'Specific administrative maintenance policy; no substantive contract clean credit',
    scope='Only these exact IDs; no broader private/internal/test or narrative-policy narrowing') for k in invalid_tests+[invalid_narrative]]
for key in [miss,*invalid_tests,invalid_narrative]:
    u=units[key];body=original(u['path'])[u['start_character']:u['end_character_exclusive']];assert sha(body.encode())==u['range_sha256']
for path,digest in inputs.items():assert sha(Path(path).read_bytes())==digest,('Bound input changed',path)
for path,digest in accepted.items():assert sha(Path(path).read_bytes())==digest,('Accepted current product changed during capture',path)
assert sha(Path(focused_path).read_bytes())==review['product']['before_sha256'], 'Reserved primary focused fixture changed during capture.'
emit('additional-selection.json',expansion);emit('replacement-selection.json',replacement_rows);emit('miss-overlay.json',miss_overlay);emit('ineligibility-overlay.json',invalid_overlay)
emit('parsed-coordinator-tables.json',dict(schema='exact-Markdown-table-suffix-cells',prefix='core-001:unit:',Oath=parsed['Oath'],WebAuthn=parsed['WebAuthn'],
    scope='Exact original coordinator wording preserved; preparer allocates no new content verdicts.'))
emit('quotas.json',dict(material_test_expansion=dict(original_population=104,original_full_quota=11,additional_full_quota=11,proposed_total=22,
    known_miss_clean_credit=0),eligibility_slot_repairs=replacement_quotas,final_denominator=None))
emit('citation-qualification.json',dict(reader='Astra',authority='sampling/coordinator/read-boundary-WebAuthn-verdicts.md',
    dated_original_URL='https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#prf-extension',section='10.1.4',status='Dated W3C WebAuthn Level 3 Recommendation as recorded by coordinator',
    actor='Client PRF extension',condition='When implemented over hmac-secret',strength='MUST',
    requirement='Exposed single PRF is the one used when user verification is performed, overriding UserVerificationRequirement if necessary.',
    limits='Coordinator passage read retained; not a blanket authenticator claim, full conformance, erasure or fresh ceremony result. No new external research or passage verification by preparer.'))
emit('accepted-overlap-inputs.json',dict(manifest='editing/integration/current-products.json',manifest_sha256=inputs[str(RUN/'editing/integration/current-products.json')],
    accepted_paths=len(accepted),accepted_target_role_paths=relevant,command=command,diff_sha256=sha(patch),diff_bytes=len(patch),
    focused_review_record=str(focused_dir/'review-identities.json'),focused_patch_sha256=sha(focused),focused_primary_before_sha256=sha(before),
    focused_transfer='QUEUED; primary read-only until coordinator compilation cycle ends',original_changed_spans=spans,parser_reuse_sha256=sha(parser_helper)))
text=['# WebAuthn expansion and separate eligibility-replacement whole units','','**No content verdicts.** Pinned source `'+PIN+'`; read complete original units plus relevant enclosing context.','']
for row in expansion+replacement_rows:
    body=original(row['path'])[row['start_character']:row['end_character_exclusive']]
    text+=['## `'+row['id']+'`',f"{row['purpose']}; `{row['path']}:{row['first_line']}–{row['last_line']}`; original rank {row['original_rank']}.",'','````'+('csharp' if row['path'].endswith('.cs') else 'markdown'),body,'````','']
(OUT/'original-units.md').write_text('\n'.join(text)+'\n')
plan=['# WebAuthn test/helper expansion and separate eligibility repairs','','**Proposed reading only.** Original selections/population, earlier expansion/history records and coordinator Markdown tables remain unchanged. Exact suffix IDs and original verdict wording are preserved in `parsed-coordinator-tables.json`.','',
    'SAMPLE-WA-001: original test/helper population **104**, full original quota **11**, additional next-ranked quota **11**, proposed test expansion total **22** before separate eligibility repairs. Miss e29522f04d40349a receives zero final clean credit.',
    'Focused patch d70a50aa... is independently PASS and queued until compilation releases primary. Its exact original-coordinate overlap is excluded; this preparer makes no transfer or primary product change.','',
    '## Eleven additional test/helper reads']
for row in expansion:plan.append(f"- `{row['id']}` — `{row['path']}:{row['first_line']}–{row['last_line']}`; rank {row['original_rank']}; **UNREAD / zero credit**.")
plan+=['','## Specific eligibility repairs — separate from material expansion',
    '7fe08c46f10afee5 and c6719030f9881ef4 are pure-preamble exclusions; c8377e0a58dac11c is administrative maintenance policy, not substantive contract content. Only these exact IDs lose clean credit; no broader code-only/private/test or narrative eligibility narrowing.']
for q in replacement_quotas:plan.append(f"- {q['stratum']}: working population **{q['working_population']}**, 10% minimum **{q['working_minimum']}**, retained original slots after these eligibility exclusions **{q['retained_original_slots_after_eligibility_only']}**; **{q['required_eligibility_replacements']} deterministic eligibility repairs**.")
plan+=['The test-role retained-slot count includes the separate material-miss slot for bookkeeping only, never clean credit: actual original non-miss candidates are eight. The full 11-unit expansion addresses that miss separately, while two eligibility repairs restore slots lost solely to the two preambles. No miss is credited or counted as an administrative exclusion.']
for row in replacement_rows:plan.append(f"- REPLACEMENT `{row['id']}` — `{row['path']}:{row['first_line']}–{row['last_line']}`; {row['stratum']}; rank {row['original_rank']}; **UNREAD / zero credit**.")
plan+=['','## Preserved scope and final gates',
    'OATH: all 18 table IDs reconcile to original selection; no OATH material miss or expansion is introduced. WebAuthn: all 19 exact table IDs reconcile; original PRF actor/condition/MUST qualification is retained in `citation-qualification.json`, not re-researched.',
    'Coordinator handoff reports remaining original PGP22/PIV40/OTP28 reads; all expansion/replacement actual reads remain separate and pending. No new sampling-content verdicts are supplied here.',
    '[original-units.md](original-units.md) supplies every exact whole pinned unit. Selections retain source/range hashes and pending verdicts; existing and new IDs have no duplicates or known changed-span overlaps.',
    'Final canonical combined-diff eligibility, current-comment mapping, sampler readings and compilation/publication gates remain separate. Keep primary stable during the coordinator compilation cycle.',
    'Records-only rank/hash/index work. No source audit/research, product transfer/edit, SDK/build/test/native/device/formatter/crypto execution, Git mutation or subagent. Finish Core Pass 1 publication, then stop.']
(OUT/'read-plan.md').write_text('\n'.join(plan)+'\n')
receipt=dict(result='STRUCTURAL-WA-EXPANSION-AND-SEPARATE-ELIGIBILITY-REPAIRS; NO CONTENT VERDICTS',source_baseline=PIN,
    Oath_exact_suffix_table_IDs=18,WebAuthn_exact_suffix_table_IDs=19,Oath_material_misses=0,
    original_test_quota=11,additional_test_units=11,material_miss_clean_credit=0,specific_ineligible_IDs=3,
    test_eligibility_replacements=2,narrative_eligibility_replacements=1,all_new_proposed_IDs=14,
    duplicate_prior_or_known_edited_new_IDs=0,other_owner_role_reductions=0,focused_patch_sha256=sha(focused),
    primary_focused_fixture_unchanged=True,focused_transfer='QUEUED; no preparation source mutation',
    PRF='Astra dated W3C10.1.4 actor/condition/MUST qualification preserved; not reverified here',
    final_denominator=None,current_mapping='DEFERRED until final canonical reconciliation',input_sha256=inputs,
    outputs=[dict(path=str(p),bytes=p.stat().st_size,sha256=sha(p.read_bytes())) for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='verification.json'])
emit('verification.json',receipt)
print(json.dumps({k:v for k,v in receipt.items() if k not in ['input_sha256','outputs']},indent=2))
