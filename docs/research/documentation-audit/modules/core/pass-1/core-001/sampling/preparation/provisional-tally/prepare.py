#!/usr/bin/env python3
"""Add PGP/PIV proposals and join the coordinator's exact 983 original verdict IDs."""
import ast
import collections
import hashlib
import json
import math
from pathlib import Path
import re
import subprocess

from pygments.lexers import CSharpLexer
from pygments.token import Comment

RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PREP=RUN/'sampling/preparation';OUT=PREP/'provisional-tally';PGP=PREP/'expansion-OpenPgp';PIV=PREP/'replacements-Piv'
PIN='fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef';SEED='core-001-clean-sampling-preparation-v1'
inputs,raw_originals,normalized_originals={},{},{}


def sha(b):return hashlib.sha256(b).hexdigest()


def read(p):
    b=p.read_bytes();inputs[str(p)]=sha(b);return b


def load(p):return json.loads(read(p))


def emit(directory,name,value):(directory/name).write_text(json.dumps(value,indent=2)+'\n')


read(Path(__file__));PGP.mkdir(parents=True,exist_ok=True);PIV.mkdir(parents=True,exist_ok=True)
population=load(PREP/'population.json')['units'];byid={r['id']:r for r in population}
selection=load(PREP/'selection.json');selected={r['id']:r for r in selection};original_ids=set(selected)
units={u['id']:u for u in load(RUN/'inventory/semantic-units.json')};files={f['path']:f for f in load(RUN/'inventory/files.json')};strata=load(PREP/'strata.json')
assert len(selected)==983
coordinator=RUN/'sampling/coordinator';tally={}
json_status={
    'SOURCE-CONTENT-SUPPORTED-PENDING-FINAL-ELIGIBILITY':'supported-pending-final-eligibility',
    'DOCUMENTATION-MISS':'documentation-miss', 'DEFERRED-SOURCE-OBSERVATION':'source-observation',
    'INELIGIBLE-DORMANT-ILLUSTRATION':'specific-ineligible', 'INELIGIBLE-ADMINISTRATIVE':'specific-ineligible',
    'KNOWN-DEFERRED-PROTECTED-OUTPUT':'protected-output-qualification',
    'SOURCE-QUALIFIED-NORMATIVE-GAP':'source-evidence-qualified', 'REFERENCE-DATA-QUALIFIED':'source-evidence-qualified'}


def add(key,category,payload,authority,reading_material):
    assert key in selected and key not in tally,('Extra/duplicate original verdict ID',key)
    if 'range_sha256' in payload:assert payload['range_sha256']==units[key]['range_sha256']
    tally[key]=dict(id=key,owner=selected[key]['owner'],stratum=selected[key]['stratum'],path=units[key]['path'],
        first_line=units[key]['first_line'],last_line=units[key]['last_line'],source_sha256=units[key]['source_sha256'],range_sha256=units[key]['range_sha256'],
        actually_read_by='Astra',raw_category=category,raw_verdict_payload=payload,authority=str(authority),authority_sha256=inputs[str(authority)],
        original_reading_material=str(reading_material),preparer_content_verdict=None)


# First batch: 17 exact table suffixes plus the three explicit material-miss IDs in its miss section.
first_path=coordinator/'read-001-verdicts.md';first=read(first_path).decode();first_material=coordinator/'read-001.md';read(first_material)
first_rows=[]
for line in first.splitlines():
    m=re.fullmatch(r'\| `([0-9a-f]{16})` \| (.*) \|',line)
    if m:first_rows.append(('core-001:unit:'+m[1],dict(suffix=m[1],verdict_text=m[2])))
assert len(first_rows)==17
for key,payload in first_rows:add(key,'supported-pending-final-eligibility',payload,first_path,first_material)
miss_section=first.split('## Material misses',1)[1].split('## Other 17 units',1)[0]
for suffix in ['711e47d18bab0742','c085f1cd3632e7c2','076b4d4328825902']:
    assert suffix in miss_section
    add('core-001:unit:'+suffix,'documentation-miss',dict(suffix=suffix,verdict_text=miss_section,scope='Coordinator exact first-batch material-miss section'),first_path,first_material)
assert len(tally)==20
for path in sorted(coordinator.glob('*-verdicts.json')):
    record=load(path)
    material=coordinator/(record.get('reading_file') or path.name.replace('-verdicts.json','.md'))
    assert sha(read(material))==record['reading_sha256']
    for row in record['units']:add(row['id'],json_status[row['verdict']],row,path,material)
md_special={
    'core-001:unit:e29522f04d40349a':'documentation-miss',
    'core-001:unit:7fe08c46f10afee5':'specific-ineligible','core-001:unit:c6719030f9881ef4':'specific-ineligible',
    'core-001:unit:c8377e0a58dac11c':'specific-ineligible',
    'core-001:unit:8b2a95f01db9bf2c':'documentation-miss','core-001:unit:710814862611f595':'documentation-miss',
    'core-001:unit:a579b6a5cf4242e4':'known-implementation-qualified','core-001:unit:8fc815177a95b970':'known-implementation-qualified',
    'core-001:unit:89c5d1ecb6fd0e8d':'edited-overlap'}
for owner,count in [('Oath',18),('WebAuthn',19),('OpenPgp',22),('Piv',40),('YubiOtp',28)]:
    path=coordinator/('read-boundary-'+owner+'-verdicts.md');data=read(path).decode();material=coordinator/('read-boundary-'+owner+'.md');read(material)
    rows=[dict(suffix=m[1],id='core-001:unit:'+m[1],verdict_text=m[2]) for line in data.splitlines() if (m:=re.fullmatch(r'\| `([0-9a-f]{16})` \| (.*) \|',line))]
    assert len(rows)==len({r['id'] for r in rows})==count
    assert {r['id'] for r in rows}=={r['id'] for r in selection if r['owner']==owner}
    for row in rows:
        category=md_special.get(row['id'],'supported-pending-final-eligibility')
        if row['id'] not in md_special:assert row['verdict_text'].startswith('Supported')
        add(row['id'],category,row,path,material)
assert set(tally)==original_ids and len(tally)==983
read(coordinator/'additional-observations.md')
history_overlay=load(PREP/'expansion-B3/history-credit-overlay.json')
for row in history_overlay:
    assert row['id'] in tally and row['final_clean_credit']==0
    tally[row['id']]['sampling_credit_adjudication']=row

# Fixed active proposals, explicitly excluding superseded SW choices and avoiding duplicate activation IDs.
active_specs=[
 ('expansion-A/additional-selection.json','expansion-A/original-units.md'),
 ('expansion-A-followup/additional-selection.json','expansion-A-followup/original-units.md'),
 ('expansion-B2/additional-selection.json','expansion-B2/original-units.md'),
 ('expansion-B2/replacement-selection.json','expansion-B2/original-units.md'),
 ('expansion-B3/activated-history-replacements.json','expansion-B3/original-units.md'),
 ('expansion-B3/additional-selection.json','expansion-B3/original-units.md'),
 ('expansion-B4/additional-selection.json','expansion-B4/original-units.md'),
 ('expansion-B4/replacement-selection.json','expansion-B4/original-units.md'),
 ('family-replacements/revision-2/replacement-selection.json','family-replacements/revision-2/original-units.md'),
 ('expansion-SecurityDomain/additional-selection.json','expansion-SecurityDomain/original-units.md'),
 ('expansion-WebAuthn/additional-selection.json','expansion-WebAuthn/original-units.md'),
 ('expansion-WebAuthn/replacement-selection.json','expansion-WebAuthn/original-units.md')]
active=[];active_ids=set()
for record,excerpt in active_specs:
    rows=load(PREP/record);excerpt_bytes=read(PREP/excerpt)
    for row in rows:
        key=row['id'];assert key not in active_ids and key not in original_ids and key.encode() in excerpt_bytes
        active_ids.add(key);active.append(dict(row,active_proposal_record=str(PREP/record),whole_unit_excerpt_file=str(PREP/excerpt),whole_unit_excerpt_sha256=sha(excerpt_bytes)))
assert len(active_ids)==100
predecessor=load(PREP/'family-replacements/revision-2/predecessor-history.json');retired_ids=set(predecessor['retired_provisional_ids'])
assert len(retired_ids)==4 and not retired_ids&active_ids
additional_reads={}


def add_extra(suffix,category,payload,authority):
    key='core-001:unit:'+suffix
    assert key in active_ids and key not in additional_reads,('Extra/duplicate additional verdict ID',key)
    additional_reads[key]=dict(id=key,owner=byid[key]['owner'],stratum=byid[key]['stratum'],path=units[key]['path'],
        first_line=units[key]['first_line'],last_line=units[key]['last_line'],source_sha256=units[key]['source_sha256'],range_sha256=units[key]['range_sha256'],
        actually_read_by='Astra',raw_category=category,raw_verdict_payload=payload,authority=str(authority),authority_sha256=inputs[str(authority)],preparer_content_verdict=None)


def table_rows(text):
    return [dict(suffix=m[1],verdict_text=m[2]) for line in text.splitlines() if (m:=re.fullmatch(r'\| `([0-9a-f]{16})` \| (.*) \|',line))]


def bullet_rows(text):
    return [dict(suffix=m[0],verdict_text=m[1]) for m in re.findall(r'(?ms)^- `([0-9a-f]{16})`:\s*(.*?)(?=\n\s*\n|\n- `|\Z)',text)]


a_path=coordinator/'expansion-A-verdicts.md';a_text=read(a_path).decode()
a_list=a_text.split('Content supported within the stated source scope for all listed IDs:',1)[1].split('\n\nThe feed',1)[0]
a_first=re.findall(r'`([0-9a-f]{16})`',a_list);assert len(a_first)==len(set(a_first))==23
for suffix in a_first:add_extra(suffix,'supported-pending-final-eligibility',dict(suffix=suffix,verdict_text='Content supported within stated source scope; explicit ID in first-expansion list',coordinator_list=a_list),a_path)
a_special={'f24674b7449e617e':'source-evidence-qualified','7b5daf3c140bfbc6':'source-evidence-qualified','13971cb61434ba4c':'source-evidence-qualified',
           '3ccf06e816f277d2':'source-observation','4089aedab94d4e23':'known-implementation-qualified',
           '297e68004991d4a8':'protected-output-qualification','cf3538431462d4ab':'known-implementation-qualified'}
a_follow=table_rows(a_text);assert len(a_follow)==19
for row in a_follow:
    category=a_special.get(row['suffix'],'supported-pending-final-eligibility')
    if row['suffix'] not in a_special:assert row['verdict_text'].startswith('Supported')
    add_extra(row['suffix'],category,row,a_path)
additional_reads['core-001:unit:3ccf06e816f277d2']['related_existing_finding']='SAMPLE-A004; additional caller facet, not permanent-leak/runtime proof or new unique bug'
additional_reads['core-001:unit:4089aedab94d4e23']['existing_class']='Pre-protected-scope cleanup qualification; no new experiment or category-wide expansion'
b_path=coordinator/'expansion-B2-B3-B4-verdicts.md';b_text=read(b_path).decode()
b_list=b_text.split('Content supported in the stated source scope for:',1)[1].split('\n\nThe complete',1)[0]
b_supported=re.findall(r'`([0-9a-f]{16})`',b_list);assert len(b_supported)==len(set(b_supported))==18
for suffix in b_supported:add_extra(suffix,'supported-pending-final-eligibility',dict(suffix=suffix,verdict_text='Content supported within stated source scope; explicit B2 expansion/replacement ID list',coordinator_list=b_list),b_path)
b2_section=b_text.split('## B2 XML expansion and three eligibility replacements',1)[1].split('## Seven activated',1)[0]
exceptions=bullet_rows(b2_section);assert {r['suffix'] for r in exceptions}=={'cf330b6a639970cd','82f001f6db22d507'}
for row in exceptions:add_extra(row['suffix'],'edited-overlap' if row['suffix']=='cf330b6a639970cd' else 'source-evidence-qualified',row,b_path)
additional_reads['core-001:unit:82f001f6db22d507']['editor_followup']='SAMPLE-B2-003 frequency wording correction; original XML stratum already expanded, no further full-category quota added.'
history_special={'4c955b22a2de68df','0b2135f2f301e2c1','4330e60d07cd5417'}
history_rows=table_rows(b_text);assert len(history_rows)==7
for row in history_rows:
    if row['suffix'] not in history_special:assert row['verdict_text'].startswith('Supported')
    add_extra(row['suffix'],'source-evidence-qualified' if row['suffix'] in history_special else 'supported-pending-final-eligibility',row,b_path)
b3_section=b_text.split('## B3: two new XML units',1)[1].split('## B4:',1)[0]
b3_rows=bullet_rows(b3_section);assert len(b3_rows)==2
for row in b3_rows:assert row['verdict_text'].startswith('supported');add_extra(row['suffix'],'supported-pending-final-eligibility',row,b_path)
b4_section=b_text.split('## B4: four narrative additions and one protected-output replacement',1)[1]
b4_rows=bullet_rows(b4_section);assert len(b4_rows)==5
b4_categories={'f5a358ec89b4c4ce':'specific-ineligible','7070cc291aec3e7e':'specific-ineligible',
               '9a180bff18d16922':'supported-pending-final-eligibility','db781bee61b48612':'supported-pending-final-eligibility','7dc8f2fc14f0186c':'supported-pending-final-eligibility'}
assert {r['suffix'] for r in b4_rows}==set(b4_categories)
for row in b4_rows:add_extra(row['suffix'],b4_categories[row['suffix']],row,b_path)
family_path=coordinator/'family-and-SD-replacement-verdicts.md';family_rows=table_rows(read(family_path).decode());assert len(family_rows)==10
for row in family_rows:assert row['verdict_text'].startswith('Supported');add_extra(row['suffix'],'supported-pending-final-eligibility',row,family_path)
wa_prior_ids={r['id'] for r in active if r['owner']=='WebAuthn'}
assert len(additional_reads)==86 and set(additional_reads)==active_ids-wa_prior_ids and len(wa_prior_ids)==14
reserved=original_ids|active_ids|retired_ids
original_helper=read(PREP/'expansion-B2/expand.py');parser_helper=read(PREP/'prepare.py')
namespace=dict(subprocess=subprocess,PIN=PIN,files=files,raw_originals=raw_originals,normalized_originals=normalized_originals,sha=sha,Path=Path,read=read,re=re)
for data,name,path in [(original_helper,'original',PREP/'expansion-B2/expand.py'),(parser_helper,'parse_patch',PREP/'prepare.py')]:
    node=next(n for n in ast.parse(data).body if isinstance(n,ast.FunctionDef) and n.name==name)
    exec(compile(ast.Module(body=[node],type_ignores=[]),str(path),'exec'),namespace)
original=namespace['original'];namespace['original']=original
new_roles={('OpenPgp','implementation-state-and-internal-contracts'),('OpenPgp','narrative-examples-and-maps'),
           ('Piv','implementation-state-and-internal-contracts'),('Piv','narrative-examples-and-maps')}
target_paths={units[k]['path'] for k in original_ids|active_ids}
target_paths|={units[r['id']]['path'] for r in population if (r['owner'],r.get('stratum')) in new_roles and r['eligibility']=='candidate-eligible'}
target_paths|={units[r['id']]['path'] for r in population if r['eligibility']=='candidate-eligible'}
manifest=load(RUN/'editing/integration/current-products.json');assert manifest['source_baseline']==PIN
accepted={r['path']:r['sha256'] for r in manifest['product_files']}
for path,digest in accepted.items():assert sha(Path(path).read_bytes())==digest,('Accepted current product changed',path)
for name in manifest['receipt_order']:load(RUN/'editing/integration'/name)
relevant=sorted(target_paths&accepted.keys())
command=['git','--no-optional-locks','diff','--no-ext-diff','--no-textconv','--no-renames','--unified=3','--src-prefix=a/','--dst-prefix=b/',PIN,'--',*relevant]
patch=subprocess.check_output(command);patch_path=OUT/'known-accepted-selected-role-scope.diff';patch_path.write_bytes(patch)
accepted_spans=namespace['parse_patch'](patch_path);pending_spans={}
# Reuse the previously bound independently passed/queued WA focused correction as an exact pending span input.
wa_inputs=load(PREP/'expansion-WebAuthn/accepted-overlap-inputs.json')
wa_review_path=Path(wa_inputs['focused_review_record']);wa_review=load(wa_review_path);assert wa_review['outcome']=='PASS'
wa_patch=wa_review_path.parent/'focused.patch';assert sha(read(wa_patch))==wa_inputs['focused_patch_sha256']
wa_original_spans=namespace['parse_patch'](wa_patch)
wa_after=wa_review['product']['accepted_after_sha256']
wa_transfer_captured=accepted.get(wa_review['product']['path'])==wa_after
for path,rows in wa_original_spans.items():
    if not wa_transfer_captured:pending_spans[path]=rows


def hits(key,spans):
    u=units[key];a,b=u['start_character'],u['end_character_exclusive']
    return [s for s in spans.get(u['path'],[]) if (a<s['end_character_exclusive'] and s['start_character']<b
        if s['start_character']!=s['end_character_exclusive'] else a<=s['start_character']<=b)]


def rank_for(owner,role):
    keys={r['id'] for r in population if r['owner']==owner and r.get('stratum')==role and r['eligibility']=='candidate-eligible'}
    return sorted(keys,key=lambda key:sha((SEED+'|'+owner+'|'+role+'|'+key).encode()))


def new_row(key,purpose,rank):
    u=units[key];body=original(u['path'])[u['start_character']:u['end_character_exclusive']];assert sha(body.encode())==u['range_sha256']
    return dict(id=key,owner=byid[key]['owner'],stratum=byid[key]['stratum'],purpose=purpose,original_rank=rank.index(key)+1,
        **{f:u[f] for f in ['path','first_line','last_line','start_character','end_character_exclusive','source_sha256','range_sha256','category','label']},
        known_edited_overlap=False,read_status='PROPOSED-UNREAD',verdict='UNASSIGNED',clean_credit=0,
        current_mapping='DEFERRED to final canonical reconciliation; original pinned unit may be read now')


pgp_misses=['core-001:unit:8b2a95f01db9bf2c','core-001:unit:710814862611f595'];pgp_rows=[];pgp_quotas=[];used=set(reserved)
for role in ['implementation-state-and-internal-contracts','narrative-examples-and-maps']:
    q=next(q for q in strata if q['owner']=='OpenPgp' and q['stratum']==role);rank=rank_for('OpenPgp',role)
    assert q['selected_ids']==rank[:q['proposed_selected']]
    available=[k for k in rank if k not in used and not byid[k].get('edited_overlap') and not hits(k,accepted_spans) and not hits(k,pending_spans)]
    keys=available[:q['proposed_selected']];assert len(keys)==q['proposed_selected']
    rows=[new_row(k,'SAMPLE-PGP-001-FULL-ORIGINAL-QUOTA-EXPANSION',rank) for k in keys]
    for row in rows:row['pending_PGP_correction_path']=row['path'] in {units[k]['path'] for k in pgp_misses}
    pgp_rows+=rows;used.update(keys)
    pgp_quotas.append(dict(owner='OpenPgp',stratum=role,original_population=len(rank),original_full_quota=q['proposed_selected'],
        additional_full_quota=len(keys),proposed_total=q['proposed_selected']+len(keys),miss_final_clean_credit=0,additional_ids=keys))
assert [q['additional_full_quota'] for q in pgp_quotas]==[4,2]
piv_withheld={'implementation-state-and-internal-contracts':['core-001:unit:a579b6a5cf4242e4','core-001:unit:8fc815177a95b970'],
              'narrative-examples-and-maps':['core-001:unit:89c5d1ecb6fd0e8d']}
assert hits('core-001:unit:89c5d1ecb6fd0e8d',accepted_spans), 'Expected accepted PIV revision2 example overlap must be explicit.'
piv_rows=[];piv_quotas=[]
for role,withheld in piv_withheld.items():
    q=next(q for q in strata if q['owner']=='Piv' and q['stratum']==role);rank=rank_for('Piv',role)
    edited={k for k in q['selected_ids'] if hits(k,accepted_spans) or hits(k,pending_spans)}
    original_valid=set(q['selected_ids'])-set(withheld)-edited
    minimum=max(1,math.ceil(len(rank)/10));needed=max(0,minimum-len(original_valid))
    available=[k for k in rank if k not in used and k not in withheld and not byid[k].get('edited_overlap') and not hits(k,accepted_spans) and not hits(k,pending_spans)]
    keys=available[:needed];assert len(keys)==needed
    piv_rows += [new_row(k,'PIV-KNOWN-QUALIFICATION-OR-ACCEPTED-OVERLAP-REPLACEMENT; NOT MATERIAL-MISS EXPANSION',rank) for k in keys];used.update(keys)
    piv_quotas.append(dict(owner='Piv',stratum=role,broad_original_population=len(rank),original_quota=q['proposed_selected'],conservative_minimum=minimum,
        withheld_sample_ids=withheld,known_edited_original_selected_ids=sorted(edited),valid_original_selected=len(original_valid),required_replacements=needed,replacement_ids=keys,
        policy='Retain original broad role denominator/quota conservatively until stable final diff; specific sample credits withheld, no material-miss expansion.'))
assert [q['required_replacements'] for q in piv_quotas]==[2,1]
assert len({r['id'] for r in pgp_rows+piv_rows})==9 and not {r['id'] for r in pgp_rows+piv_rows}&reserved
pgp_overlay=[dict(id=k,coordinator_finding='SAMPLE-PGP-001',raw_verdict=tally[k]['raw_verdict_payload'],original_ledger_status='READ-CLEAN-PRESERVED-AS-HISTORY',
    final_clean_credit=0,scope='UIF advisory fallback logger-throw qualification in implementation/guide; isolated editor verifying comments. Actual correction patch not guessed or polled.') for k in pgp_misses]
piv_overlay=[dict(id=k,original_ledger_status='READ-CLEAN-PRESERVED-AS-HISTORY',raw_verdict=tally[k]['raw_verdict_payload'],final_clean_credit=0,
    reason='Known CORE-PIV-X006 / PIVIMP-006 pre-try discovery secret-cleanup qualification; not a new bug or runtime fix.' if role.startswith('implementation') else
        'Pinned README example already changed by accepted PIV revision2 protection-scope fix; exact edited overlap is excluded.') for role,keys in piv_withheld.items() for k in keys]
emit(PGP,'additional-selection.json',pgp_rows);emit(PGP,'quotas.json',pgp_quotas);emit(PGP,'miss-overlay.json',pgp_overlay)
emit(PIV,'replacement-selection.json',piv_rows);emit(PIV,'quotas.json',piv_quotas);emit(PIV,'credit-overlay.json',piv_overlay)
for directory,title,rows in [(PGP,'OpenPGP additional whole units',pgp_rows),(PIV,'PIV replacement whole units',piv_rows)]:
    text=['# '+title,'','**No content verdicts.** Pinned source `'+PIN+'`; read complete units with enclosing context as needed.','']
    for row in rows:
        body=original(row['path'])[row['start_character']:row['end_character_exclusive']]
        text+=['## `'+row['id']+'`',f"`{row['path']}:{row['first_line']}–{row['last_line']}`; {row['stratum']}; original rank {row['original_rank']}.",'','````'+('csharp' if row['path'].endswith('.cs') else 'markdown'),body,'````','']
    (directory/'original-units.md').write_text('\n'.join(text)+'\n')
for directory,heading,rows,quota in [(PGP,'OpenPGP material-miss expansion',pgp_rows,pgp_quotas),(PIV,'PIV qualification/edited-overlap repairs',piv_rows,piv_quotas)]:
    plan=['# '+heading,'','**Proposed reading only.** Original selections, broad population, prior proposals, source ledgers and coordinator verdicts remain unchanged.','']
    for q in quota:
        plan.append(f"- {q['stratum']}: original population {q.get('original_population',q.get('broad_original_population'))}, original quota {q['original_full_quota'] if 'original_full_quota' in q else q['original_quota']}; **{q.get('additional_full_quota',q.get('required_replacements'))} new units**.")
    plan+=['','[original-units.md](original-units.md) supplies every exact complete pinned unit.']
    for row in rows:plan.append(f"- `{row['id']}` — `{row['path']}:{row['first_line']}–{row['last_line']}`; original rank {row['original_rank']}; **UNREAD / zero credit**.")
    plan += ['', 'SAMPLE-PGP-001 has implementation/guide logging-success qualifications; both original miss IDs have zero clean credit. Pending corrections are not guessed and primary remains reserved.' if directory==PGP else
        'The two known implementation qualifications retain CORE-PIV-X006 / PIVIMP-006 provenance and no new-bug allocation. The accepted revision2 README overlap has zero clean credit. These are three conservative same-role replacements, not a PIV material-miss expansion.',
        'Known accepted/pending exact spans are excluded, never whole files. No other owner/role is reduced. Final canonical combined-diff eligibility, current mapping and actual Astra reads remain pending.',
        'Records-only work; no content verdict/source audit/research, product transfer/edit, SDK/build/test/native/device/formatter/crypto execution, Git mutation or subagent. Finish Core Pass 1 publication, then stop.']
    (directory/'read-plan.md').write_text('\n'.join(plan)+'\n')

# Preserve all raw verdict formats; add only explicit coordinator adjudications and edit-credit overlays.
pending_pgp=set(pgp_misses)
for key,row in {**tally,**additional_reads}.items():
    row['known_accepted_edited_overlap']=hits(key,accepted_spans)
    row['known_reviewed_pending_overlap']=hits(key,pending_spans)
    row['pending_uncaptured_PGP_correction']=key in pending_pgp
    reasons=[]
    if row['raw_category']!='supported-pending-final-eligibility':reasons.append(row['raw_category'])
    if 'sampling_credit_adjudication' in row:reasons.append('source-evidence-qualified-context-overlay')
    if row['known_accepted_edited_overlap']:reasons.append('known-accepted-edited-overlap')
    if row['known_reviewed_pending_overlap']:reasons.append('known-reviewed-pending-edited-overlap')
    if row['pending_uncaptured_PGP_correction']:reasons.append('confirmed-PGP-miss-correction-pending')
    row['credit_withholding_reasons']=reasons
    row['provisional_credit_state']='WITHHELD-PER-RECORDED-REASON' if reasons else 'SOURCE-CONTENT-SUPPORTED; FINAL-ELIGIBILITY/MAPPING/QUOTA-PENDING'
    row['final_clean_credit']=0 if reasons else None
assert len(tally)==983
emit(OUT,'original-read-tally.json',list(tally.values()))
emit(OUT,'additional-read-tally.json',list(additional_reads.values()))
for directory,rows,record,excerpt in [(PGP,pgp_rows,'additional-selection.json','original-units.md'),(PIV,piv_rows,'replacement-selection.json','original-units.md')]:
    excerpt_path=directory/excerpt;data=read(excerpt_path)
    for row in rows:active.append(dict(row,active_proposal_record=str(directory/record),whole_unit_excerpt_file=str(excerpt_path),whole_unit_excerpt_sha256=sha(data)))
assert len(active)==len({r['id'] for r in active})==109 and not {r['id'] for r in active}&original_ids and not {r['id'] for r in active}&retired_ids
assert set(additional_reads)<={r['id'] for r in active} and len(additional_reads)==86
all_reads={**tally,**additional_reads};assert len(all_reads)==1069

# Replacement-pool screening is additive and does not rewrite broad population/denominators or
# supply content verdicts. Actual positive Astra verdicts are not reversed by lexical screening.
gap_pool=load(PREP/'family-replacements/revision-2/replacement-pool-exclusions.json')
SW_pool_ids={r['id'] for r in gap_pool};assert len(SW_pool_ids)==48
ctap_source=load(RUN/'boundaries/Ctap2/unit-dispositions.json')
localized_facets={r['id']:r['candidate_ids'] for r in ctap_source if r['disposition']=='READ-CLEAN' and r.get('candidate_ids')}
known_withheld={key for key,row in all_reads.items() if row['credit_withholding_reasons']}
current_research_headings={
    '## Native ABI and descriptor ownership','## Report contract','## Descriptor sizing limits','## Two-phase consumer contract',
    '## One poller, including notification-fd retirement','## Exact seam and ownership','## Report and cancellation behavior',
    '## Shutdown and release evidence','### Unexpected worker-loop failure','## Direct compatibility and deliberate differences',
    '## Reports and cancellation','## Shutdown, unexpected failure and finalization','## Routing and APIs'}
pool_exclusions={}


def pool_reason(key):
    u=units[key];path=u['path'];label=u['label'];reason=None
    if key in known_withheld:reason='Exact coordinator miss/ineligible/history/source-qualified/protected/edited-credit hold; never recycle as a clean replacement.'
    elif key in SW_pool_ids:reason='Existing original-ISO gap: SWConstants replacement-pool restriction only; broad 104 XML population is unchanged.'
    elif key in localized_facets:reason='Read-clean row still has exact localized candidate_ids links; avoid unresolved candidate facets in the new replacement pool pending explicit disposition. File-level association alone is not used.'
    elif hits(key,accepted_spans) or hits(key,pending_spans):reason='Exact known accepted or reviewed-pending changed-span overlap.'
    elif Path(path).name=='AGENTS.md' or label.lstrip('# ').startswith('CLAUDE.md') or label.lstrip('# ')=='Documentation Maintenance':reason='Administrative routing/maintenance heading scope, matching the specific coordinator adjudications; replacement-pool-only screen.'
    elif path=='docs/migration/v1-to-v2-changelog.md':reason='Dated migration change-record purpose; do not propose another historical changelog as current-contract replacement.'
    elif path.startswith('docs/research/') and label not in current_research_headings:reason='Research unit is evidence/phase/report/verification or mixed scope rather than an explicitly named current control-flow contract; withhold from replacement pool pending exact scope adjudication. Not a whole-file exclusion or a broad-denominator change.'
    elif u['category']=='narrative-section-with-complete-examples' and re.search(r'(?i)historical|firmware measurement|physical checkpoint|measurements, not budgets',label):reason='Explicit historical/measurement heading; not a current clean-contract replacement.'
    elif u['category']=='source-context-and-explanations' and path.endswith('.cs'):
        text=original(path);body=text[u['start_character']:u['end_character_exclusive']]
        tokens=list(CSharpLexer().get_tokens_unprocessed(body+'\n'))
        code=''.join(v for _,t,v in tokens if t not in Comment)
        code=re.sub(r'(?m)^\s*(?:global\s+)?using\s+[^;]+;|(?m:^\s*namespace\s+[\w.]+\s*[;{])','',code)
        comments=''.join(v for _,t,v in tokens if t in Comment)
        license_prefixes=['Copyright ','Licensed under ','You may not ','You may obtain ','http://www.apache.org/',
            'Unless required ','distributed under ','WITHOUT WARRANTIES ','See the License ','limitations under ','the License','Version 2.0','SPDX-']
        substantive=[]
        for line in comments.splitlines():
            s=line.strip(' /\t')
            if not s or s.startswith('#pragma') or any(s.startswith(prefix) for prefix in license_prefixes):continue
            words=re.sub(r'<[^>]+>','',s).strip()
            if re.search(r'[A-Za-z0-9]',words):substantive.append(words)
        if not re.sub(r'[\s{};]','',code) and not substantive:reason='Exact source-context unit has only legal/import/namespace/pragma/structural text, without a declaration or substantive XML/explanation; do not propose another pure preamble.'
    if reason:pool_exclusions[key]=dict(id=key,owner=byid[key]['owner'],stratum=byid[key].get('stratum'),path=path,label=label,
        reason=reason,scope='NEW-REPLACEMENT-POOL/RESERVATION-SCREEN-ONLY',global_denominator_change=0,preparer_content_verdict=None)
    return reason


expanded_roles={('A','XML-contracts'):19,('A','enum-values'):4,('A','narrative-examples-and-maps'):8,
    ('A','implementation-state-and-internal-contracts'):11,('B2','XML-contracts'):17,('B3','XML-contracts'):2,
    ('B4','narrative-examples-and-maps'):4,('SecurityDomain','narrative-examples-and-maps'):2,
    ('WebAuthn','tests-and-helpers'):11,('OpenPgp','implementation-state-and-internal-contracts'):4,
    ('OpenPgp','narrative-examples-and-maps'):2}
assert sum(q['proposed_selected'] for q in load(PREP/'strata.json'))==983 and sum(expanded_roles.values())==84
pending_existing=[r for r in active if r['id'] not in additional_reads]
assert len(pending_existing)==23
pending_pool_holds={r['id']:pool_reason(r['id']) for r in pending_existing if pool_reason(r['id'])}
next_rows=[];quota_status=[];already_proposed=original_ids|{r['id'] for r in active}|retired_ids
for q in sorted(strata,key=lambda q:(q['owner'],q['stratum'])):
    owner,role=q['owner'],q['stratum'];extra_target=expanded_roles.get((owner,role),0);target=q['proposed_selected']+extra_target
    read_supported={key for key,row in all_reads.items() if row['owner']==owner and row['stratum']==role and not row['credit_withholding_reasons']}
    pending={row['id'] for row in pending_existing if row['owner']==owner and row['stratum']==role and row['id'] not in pending_pool_holds}
    needed=max(0,target-len(read_supported)-len(pending))
    rank=rank_for(owner,role);available=[]
    if needed:
        for key in rank:
            if key in already_proposed or byid[key].get('edited_overlap'):continue
            if pool_reason(key):continue
            available.append(key)
    keys=available[:needed]
    for key in keys:
        row=new_row(key,'BOUNDED-QUOTA-SHORTFALL-REPLACEMENT; NO NEW FULL-CATEGORY EXPANSION',rank)
        row['pool_scope_basis']='Current substantive read-clean declaration/test/XML/active narrative or named current mechanism scope; not content approval.'
        row['history_sensitive_context']=row['path'].startswith('docs/research/')
        next_rows.append(row);already_proposed.add(key)
    quota_status.append(dict(owner=owner,stratum=role,broad_original_population=q['eligible'],original_quota=q['proposed_selected'],
        already_authorized_full_expansion=extra_target,conservative_required_target=target,
        actually_read_source_supported_without_known_hold=len(read_supported),pending_existing_reserved=len(pending),
        pending_reserved_is_not_actual_credit=True,next_required=needed,next_selected=len(keys),pool_available=len(available) if needed else None,
        shortfall=needed-len(keys),new_ids=keys,final_denominator=None,
        qualification='Candidate role-level requirement against conservative original quotas; final stable combined diff/mapping and actual sampler verdicts still required. No new whole original quota added.'))
assert sum(q['conservative_required_target'] for q in quota_status)==1067
assert len(next_rows)==len({r['id'] for r in next_rows}) and not {r['id'] for r in next_rows}&(original_ids|{r['id'] for r in active}|retired_ids)
if (OUT/'next-required-selection.json').exists():
    previous=json.loads((OUT/'next-required-selection.json').read_text())
    assert [r['id'] for r in previous]==[r['id'] for r in next_rows], 'Next selection changed: preserve predecessor history and reconcile before replacement.'
emit(OUT,'next-required-selection.json',next_rows)
emit(OUT,'role-quota-status.json',quota_status)
emit(OUT,'replacement-pool-exclusions.json',list(pool_exclusions.values()))
next_text=['# Bounded next-required replacement whole units','','**No content verdicts.** Pinned source `'+PIN+'`; these are only calculated shortfall repairs, not another full-category expansion. Read enclosing context as needed.','']
for row in next_rows:
    body=original(row['path'])[row['start_character']:row['end_character_exclusive']]
    next_text+=['## `'+row['id']+'`',f"`{row['path']}:{row['first_line']}–{row['last_line']}`; {row['owner']} {row['stratum']}; original rank {row['original_rank']}.",'','````'+('csharp' if row['path'].endswith('.cs') else 'markdown'),body,'````','']
(OUT/'next-required-original-units.md').write_text('\n'.join(next_text)+'\n')
next_bytes=read(OUT/'next-required-original-units.md')
for row in next_rows:active.append(dict(row,active_proposal_record=str(OUT/'next-required-selection.json'),whole_unit_excerpt_file=str(OUT/'next-required-original-units.md'),whole_unit_excerpt_sha256=sha(next_bytes)))
unread=[]
for row in sorted((r for r in active if r['id'] not in additional_reads),key=lambda r:(r['owner'],r['stratum'],r['path'],r['start_character'])):
    key=row['id'];u=units[key];accepted_hits=hits(key,accepted_spans);pending_hits=hits(key,pending_spans)
    unread.append(dict(id=key,owner=row['owner'],stratum=row['stratum'],purpose=row.get('purpose',row.get('activation','additional-required-read')),
        path=u['path'],first_line=u['first_line'],last_line=u['last_line'],start_character=u['start_character'],end_character_exclusive=u['end_character_exclusive'],
        source_sha256=u['source_sha256'],range_sha256=u['range_sha256'],proposal_record=row['active_proposal_record'],
        whole_unit_excerpt_file=row['whole_unit_excerpt_file'],whole_unit_excerpt_sha256=row['whole_unit_excerpt_sha256'],excerpt_find_token='`'+key+'`',
        actual_sampler_read='PENDING',content_verdict='UNASSIGNED',clean_credit=0,
        known_accepted_edited_overlap=accepted_hits,known_reviewed_pending_overlap=pending_hits,
        replacement_pool_hold=pending_pool_holds.get(key),
        state='UNREAD-BLOCKED-FOR-CLEAN-CREDIT; CANONICAL-REPLACEMENT/OVERLAP-RECONCILIATION-REQUIRED' if accepted_hits or pending_hits or key in pending_pool_holds else 'UNREAD-REQUIRED-PROPOSED-UNIT',
        history_sensitive_context=row.get('history_sensitive_context',False),current_mapping='DEFERRED; exact original whole-unit excerpts may be read now'))
emit(OUT,'unread-required.json',unread)
emit(OUT,'superseded-proposals.json',dict(retired_SW_ids=sorted(retired_ids),active_unread_contains_retired_SW=False,
    authority='sampling/preparation/family-replacements/revision-2/predecessor-history.json',
    activation='Seven B1/B2 history replacements counted once through expansion-B3 activation; old conditional rows remain preserved, not duplicated.'))
emit(OUT,'known-edit-overlap-inputs.json',dict(manifest='editing/integration/current-products.json',manifest_sha256=inputs[str(RUN/'editing/integration/current-products.json')],
    accepted_paths=len(accepted),accepted_target_paths=relevant,command=command,diff_sha256=sha(patch),diff_bytes=len(patch),accepted_original_changed_spans=accepted_spans,
    reviewed_pending_WA_patch_sha256=sha(wa_patch.read_bytes()),pending_original_changed_spans=pending_spans,
    WA_transfer_state_at_manifest_capture='ACCEPTED-AFTER-HASH-CAPTURED' if wa_transfer_captured else 'REVIEWED-PATCH-STILL-QUEUED-AS-HANDOFF',
    uncaptured_pending_PGP_units=pgp_misses,scope='Provisional known accepted/reviewed-pending exact spans; no claim that final combined diff/remaining corrections are frozen'))
summary=dict(source_baseline=PIN,actual_original_read_IDs=983,original_IDs_mapped_once=True,missing_or_extra_or_duplicate_original_IDs=0,
    counting_policy='Actual-read and credit-state counts are unit counts, never additive bug counts. SAMPLE-A004 additional caller and existing cleanup/applicability findings retain their original aliases/classes.',
    raw_category_counts=dict(collections.Counter(r['raw_category'] for r in tally.values())),
    original_read_by_owner=dict(collections.Counter(r['owner'] for r in tally.values())),
    withheld_original_count=sum(bool(r['credit_withholding_reasons']) for r in tally.values()),
    supported_without_current_known_hold=sum(not r['credit_withholding_reasons'] for r in tally.values()),
    withholding_reasons_nonadditive=dict(collections.Counter(reason for r in tally.values() for reason in r['credit_withholding_reasons'])),
    historical_qualification_overlay_IDs=7,new_preparer_content_verdicts=0,final_clean_denominator=None,final_sample_pass=None,
    new_PGP_implementation_additions=4,new_PGP_narrative_additions=2,new_PIV_qualification_replacements=2,new_PIV_edited_example_replacement=1,
    no_PIV_material_miss_expansion=True,OTP_original_reads=28,OTP_new_misses=0,
    actual_additional_read_IDs=86,total_actual_unique_sample_reads=1069,repeated_activation_excerpt_read_increment=0,
    additional_raw_category_counts=dict(collections.Counter(r['raw_category'] for r in additional_reads.values())),
    additional_supported_without_current_known_hold=sum(not r['credit_withholding_reasons'] for r in additional_reads.values()),
    additional_withholding_reasons_nonadditive=dict(collections.Counter(reason for r in additional_reads.values() for reason in r['credit_withholding_reasons'])),
    previously_active_additional_proposals=109,previously_active_remaining_unread=23,
    active_unread_additional_IDs=len(unread),unread_by_owner=dict(collections.Counter(r['owner'] for r in unread)),
    unread_known_edit_blocked=sum(r['state'].startswith('UNREAD-BLOCKED') for r in unread),superseded_SW_proposals_excluded=4,
    conservative_original_population=9352,conservative_original_stratum_quota_sum=983,already_authorized_expansion_quota_sum=84,
    conservative_required_target_sum=1067,new_bounded_quota_replacement_IDs=len(next_rows),
    role_shortfall_total=sum(q['shortfall'] for q in quota_status),roles_needing_new_replacements=[q for q in quota_status if q['next_required']],
    replacement_pool_screen='Known explicit holds, pure administrative preambles/routing/maintenance, dated history/evidence and exact localized unresolved facets are not recycled. Named current research control-flow scopes remain candidates; no broad-denominator shrink or file-location credit.',
    all_new_content_verdicts='NONE by preparer; source support/withholding is parsed only from actual Astra records.',
    finalization=['Freeze accepted combined diff and pending WA/PGP/sampling patches.','Reconcile all current mappings and actual edited overlap; replace blocked or newly ineligible units without shrinking other quotas.',
        'Astra actually reads required additional/replacement whole units and records verdicts; preserve any source/actor/profile qualifications.','Recompute final admitted denominator, stratum quotas, expansions and accepted read credit; complete final validation/publication, then stop.'],
    input_sha256=inputs)
emit(OUT,'summary.json',summary)
plan=['# Unified still-required unread sampling units','','**No final clean quota or sample pass.** All **983 original proposed IDs** have exactly one actual Astra verdict row across the first-batch Markdown, JSON and boundary Markdown formats. `original-read-tally.json` preserves every raw verdict and adds only explicit credit/edit overlays.','',
    f"Provisional original credit holds: {summary['withheld_original_count']}; source-supported with no currently known hold: {summary['supported_without_current_known_hold']}. Neither quantity is final clean credit. Exact known accepted and pending edited spans are flagged, and pending PGP corrections are explicit.",
    f"**86 additional units were actually read**, bringing the unique actual-read total to **1,069**. `additional-read-tally.json` parses the three new coordinator Markdown records explicitly. Repeated activation excerpts add zero read/sample count.",
    f"Previously active still-unread units: **23** (WA14 / PGP6 / PIV3). This bounded quota repair adds **{len(next_rows)}** next-ranked units, not another full original category quota. Total listed unread: **{len(unread)}**, of which **{summary['unread_known_edit_blocked']}** are blocked/reservation-held for canonical eligibility/overlap repair.",
    'Four stale SW proposals are retired, never counted. All excerpts below are exact whole original units at `'+PIN+'`; current mapping and final eligibility remain pending.','',
    '## Bounded quota calculation',
    'Conservative broad population remains 9,352, with original per-role quota sum 983 plus already authorized full-expansion sum 84. Each role target is its original quota plus its existing authorized expansion; no new whole-category expansion is invented.',
    'Per role, next need = target minus actually read source-supported units without known credit holds minus still-unread eligible reserved proposals. Reserved units are not actual clean credit. New pool screening is replacement-only, not a rewrite/shrink of the denominator.',
    '| Owner / role | Target | Read-supported, provisional | Unread reserved | Next need / selected |', '| --- | ---: | ---: | ---: | ---: |']
for q in quota_status:
    if q['next_required']:plan.append(f"| {q['owner']} {q['stratum']} | {q['conservative_required_target']} | {q['actually_read_source_supported_without_known_hold']} | {q['pending_existing_reserved']} | {q['next_required']} / {q['next_selected']} |")
plan+=['', 'Exact role calculations/shortfalls are in `role-quota-status.json`. Pool exclusions retain per-unit reasons in `replacement-pool-exclusions.json`: no repeated historical receipts, known pure administrative units, unavailable SW clauses or unresolved localized candidate facets are proposed for clean quota.',
    'Current control-flow explanations are distinguished from historical receipts by exact unit scope, not research-directory presence. Supported d8f176..., b45b579..., 457a487... retain their actual source verdicts; 4c955..., 0b213..., 4330... retain qualified-evidence zero credit. No material miss in an already expanded category triggers another full original quota here.','']
for owner in sorted({r['owner'] for r in unread}):
    rows=[r for r in unread if r['owner']==owner];plan+=['## '+owner+' — '+str(len(rows))+' unread','']
    for row in rows:
        excerpt=Path(row['whole_unit_excerpt_file']);rel=Path(__import__('os').path.relpath(excerpt,OUT))
        flag=' **KNOWN EDIT OVERLAP: no clean credit; canonical replacement/reconciliation needed.**' if row['state'].startswith('UNREAD-BLOCKED') else ''
        plan.append(f"- `{row['id']}` — `{row['path']}:{row['first_line']}–{row['last_line']}`; {row['stratum']}; [whole-unit excerpt file]({rel}) (find exact ID heading)."+flag)
    plan.append('')
plan+=['## Remaining gates','All additional units still need actual Astra reading. Current-context mappings, pending accepted corrections, final eligible denominator and category quotas remain separate. Known qualifications or edited overlaps receive no clean credit.',
    'This is an additive records-only tally, not source review or final acceptance. Original source-disposition ledgers, all old proposals/history and coordinator verdict files remain byte-preserved. No product edits or source transfer during compilation; no SDK/test/native/device/formatter/crypto execution, Git mutation or subagents. Finish Core Pass 1 publication, then stop.']
(OUT/'unread-read-plan.md').write_text('\n'.join(plan)+'\n')
for path,digest in inputs.items():assert sha(Path(path).read_bytes())==digest,('Bound input changed',path)
for path,digest in accepted.items():assert sha(Path(path).read_bytes())==digest,('Accepted current product changed during capture',path)
receipt=dict(result='STRUCTURAL-983-VERDICT-JOIN-AND-ADDITIVE-PROPOSALS; NO PREPARER CONTENT VERDICTS',source_baseline=PIN,
    exact_actual_original_verdict_IDs=983,missing_extra_duplicate_original_IDs=0,PGP_additions=6,PIV_replacements=3,other_owner_role_reductions=0,
    actual_additional_read_IDs=86,unique_actual_read_total=1069,activation_duplicates_counted=0,
    previously_active_unread=23,next_bounded_replacements=len(next_rows),active_unread_additional_IDs=len(unread),retired_SW_IDs_counted=0,
    conservative_required_target_sum=1067,broad_population_not_shrunk=9352,role_shortfall_total=sum(q['shortfall'] for q in quota_status),
    final_denominator=None,final_clean_credit=None,final_sampler_pass=None,input_sha256=inputs,
    stdout_capture='preparation.stdout.json is a live shell capture excluded here; final structural-check receipt binds its completed bytes.',
    outputs=[dict(path=str(p),bytes=p.stat().st_size,sha256=sha(p.read_bytes())) for directory in [OUT,PGP,PIV] for p in sorted(directory.iterdir()) if p.is_file() and p.name not in ['verification.json','preparation.stdout.json','structural-check.json']])
emit(OUT,'verification.json',receipt)
for directory,kind,count in [(PGP,'PGP separate implementation/narrative expansions',6),(PIV,'PIV known-qualification/edited-overlap replacements, not defect expansion',3)]:
    emit(directory,'verification.json',dict(result='STRUCTURAL-PROPOSAL-VERIFIED; NO CONTENT VERDICTS',source_baseline=PIN,scope=kind,additional_IDs=count,clean_credit=0,
        shared_preparation_receipt=str(OUT/'verification.json'),shared_preparation_receipt_sha256=sha((OUT/'verification.json').read_bytes()),final_denominator=None))
print(json.dumps({k:v for k,v in summary.items() if k not in ['input_sha256','finalization']},indent=2))
