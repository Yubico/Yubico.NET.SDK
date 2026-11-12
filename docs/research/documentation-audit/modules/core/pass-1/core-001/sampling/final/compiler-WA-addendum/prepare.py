#!/usr/bin/env python3
"""Additive compiler-verdict retraction, transfer refresh, and bounded unread preparation."""
import ast
import hashlib
import json
import math
from pathlib import Path
import re

RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PREP=RUN/'sampling/preparation';FINAL=RUN/'sampling/final';OUT=FINAL/'compiler-WA-addendum'
RETRACTED='core-001:unit:4886f819fa316d28';ROLE=('WebAuthn','narrative-examples-and-maps')
PIN='fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef';SEED='core-001-clean-sampling-preparation-v1'
inputs={}


def sha(b):return hashlib.sha256(b).hexdigest()


def read(p):
    b=Path(p).read_bytes();inputs[str(p)]=sha(b);return b


def load(p):return json.loads(read(p))


def emit(name,data):(OUT/name).write_text(json.dumps(data,indent=2)+'\n')


read(Path(__file__))
predecessor_files={str(p):sha(read(p)) for p in sorted(FINAL.iterdir()) if p.is_file()}
prior=load(FINAL/'summary.json');assert prior['candidate_supported_unchanged_after_all_five_patches']==1067
for r in load(FINAL/'structural-check.json')['sealed_outputs']:assert sha(read(r['path']))==r['sha256']
actual=load(FINAL/'actual-read-verdicts.json');byread={r['id']:r for r in actual};assert len(actual)==len(byread)==1129
previous=byread[RETRACTED];assert previous['raw_category']=='supported-pending-final-eligibility' and previous['candidate_credit']==1
old_authority=read(previous['authority']);assert sha(old_authority)==previous['authority_sha256']
assert previous['raw_verdict_payload']['verdict_text'] in old_authority.decode()
authority_path=RUN/'sampling/coordinator/compiler-WA-logging-retraction.md';authority=read(authority_path)
assert RETRACTED in authority.decode() and 'SAMPLE-WA-002 / FULL-032' in authority.decode() and 'zero' in authority.decode()
population=load(PREP/'population.json')['units'];pop={r['id']:r for r in population}
units={r['id']:r for r in load(RUN/'inventory/semantic-units.json')};files={r['path']:r for r in load(RUN/'inventory/files.json')}
reconciliation=load(FINAL/'population-and-range-reconciliation.json');byrec={r['id']:r for r in reconciliation}
old_quotas=load(FINAL/'owner-role-quotas.json');q=next(r for r in old_quotas if (r['owner'],r['stratum'])==ROLE)
role_ids={k for k,p in pop.items() if p['eligibility']=='candidate-eligible' and (p['owner'],p['stratum'])==ROLE}
original_minimum=math.ceil(len(role_ids)/10)
assert len(role_ids)==q['conservative_original_population']==33 and original_minimum==q['conservative_10_percent_minimum']==4
assert q['authorized_expansion']==0 and q['candidate_supported_unchanged_after_all_five_patches']==4

# The old projection is now an actual transferred current state; verify rather than rerun the
# predecessor writer against its intentionally superseded 361-path primary input identities.
projected={r['path']:r['sha256'] for r in load(FINAL/'projected-AFTER-products.json')['product_files']}
current_manifest=load(RUN/'editing/integration/current-products.json')
current_products={r['path']:r['sha256'] for r in current_manifest['product_files']}
assert len(current_products)==363 and current_products==projected
for p,digest in current_products.items():assert sha(read(p))==digest,('Transferred product hash mismatch',p)
packets=load(FINAL/'queued-patch-reconciliation.json');receipts=[]
for path in sorted((RUN/'editing/integration').glob('accepted-closeout-*.json')):
    receipt=load(path);match=next(p for p in packets if p['patch_sha256']==receipt['patch_sha256'])
    expect={r['path']:r for r in match['products']};assert set(expect)=={r['path'] for r in receipt['product_files']}
    for r in receipt['product_files']:
        assert r['previous_sha256']==expect[r['path']]['before_sha256']
        assert r['sha256']==expect[r['path']]['projected_AFTER_sha256']==current_products[r['path']]
    for r in receipt['retained_records']:assert sha(read(r['path']))==r['sha256']
    receipts.append(dict(receipt=str(path),sha256=inputs[str(path)],patch_sha256=receipt['patch_sha256'],products=receipt['product_files']))
assert len(receipts)==5 and len({r['patch_sha256'] for r in receipts})==5 and sum(len(r['products']) for r in receipts)==9
refreshed_maps=[]
for r in actual:
    old=byrec[r['id']];m=old['AFTER_QUEUED'];b=read(old['path']);assert sha(b)==m['file_sha256']
    refreshed_maps.append(dict(id=r['id'],path=old['path'],CURRENT=m,authority='Five transferred receipts plus exact current primary hash equality to predecessor projected AFTER',
        prior_content_verdict_preserved=True,candidate_credit=0 if r['id']==RETRACTED else old['candidate_credit'],
        retraction_authority=str(authority_path) if r['id']==RETRACTED else None,
        pending_WA_body_fix_same_file=old['path']==units[RETRACTED]['path'],same_file_flag_is_not_whole_file_exclusion=True))

# Recover exact original source from sealed predecessor patches, with no Git invocation.
source=read(FINAL/'prepare.py');tree=ast.parse(source);ns={'re':re}
for name in ['parse_patch','apply','normalized']:
    node=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name==name)
    exec(compile(ast.Module(body=[node],type_ignores=[]),str(FINAL/'prepare.py'),'exec'),ns)
accepted=ns['parse_patch'](read(PREP/'provisional-tally/known-accepted-selected-role-scope.diff'))
queued={}
for packet in packets:
    b=read(packet['patch']);assert sha(b)==packet['patch_sha256'];queued.update(ns['parse_patch'](b))
originals={}


def original(path):
    if path not in originals:
        b=read(path);text=ns['normalized'](b)
        if path in queued:text=ns['apply'](text,queued[path],True)
        if path in accepted:text=ns['apply'](text,accepted[path],True)
        variants=([b] if ns['normalized'](b)==text else [])+[(bom+text.replace('\n',newline)).encode() for bom in ['', '\ufeff'] for newline in ['\n','\r\n']]
        raw=next((v for v in variants if sha(v)==files[path]['sha256']),None);assert raw is not None,('Exact original representation missing',path)
        originals[path]=raw.decode('utf-8-sig')
    return originals[path]


prior_proposals=load(FINAL/'proposal-replacement-order.json');proposed_ids={r['id'] for r in prior_proposals}
retired=set(load(FINAL/'replacement-history-policy.json')['retired_provisional_SW_ids'])
pool_holds={r['id']:r for p in PREP.rglob('replacement-pool-exclusions.json') if 'history' not in p.parts for r in load(p)}
rank=sorted(role_ids,key=lambda k:sha((SEED+'|'+ROLE[0]+'|'+ROLE[1]+'|'+k).encode()))
pool=[];available=[]
for key in rank:
    u=units[key];r=byrec[key];reason=None
    if key in byread:reason='Already actually read; retracted or supported readings are never counted as new reads.'
    elif key in proposed_ids or key in retired:reason='Previously proposed/retired; preserve original proposal history, do not recycle.'
    elif key in pool_holds:reason='Existing replacement-pool restriction: '+pool_holds[key]['reason']
    elif pop[key].get('edited_overlap') or not r['unchanged_population_candidate']:reason='Exact accepted/original/queued span or adjudication eligibility hold.'
    elif u['label'].lstrip('# ').startswith('CLAUDE.md') or u['label'].lstrip('# ')=='Documentation Maintenance':reason='Administrative routing/maintenance heading; replacement-pool-only avoidance.'
    elif u['path'].startswith('docs/research/'):reason='Historical/evidence or unresolved research scope avoided in replacement pool only.'
    if not reason:
        raw=original(u['path']);body=raw[u['start_character']:u['end_character_exclusive']];assert sha(body.encode())==u['range_sha256']
        m=r['AFTER_QUEUED'];text=ns['normalized'](read(u['path']))
        assert m['exact_unit_match'] and not m['original_span_overlap']
        assert text[m['start_character_normalized']:m['end_character_exclusive_normalized']]==body.replace('\r\n','\n')
        available.append(key)
    pool.append(dict(id=key,original_rank=rank.index(key)+1,path=u['path'],label=u['label'],excluded_reason=reason,
        eligible_for_new_unread_proposal=reason is None,pool_only_no_denominator_change=True,preparer_content_verdict=None))
new_target=q['required_target']+original_minimum
remaining_supported=q['candidate_supported_unchanged_after_all_five_patches']-1
need=max(0,new_target-remaining_supported);assert need==5
chosen=available[:need];assert len(chosen)==need
proposals=[]
for index,key in enumerate(chosen):
    u=units[key];proposals.append(dict(id=key,owner=ROLE[0],stratum=ROLE[1],original_rank=rank.index(key)+1,
        **{f:u[f] for f in ['path','first_line','last_line','start_character','end_character_exclusive','source_sha256','range_sha256','category','label']},
        slot_purpose='Lost original supported-slot replacement' if index==0 else 'New authorized full original WebAuthn narrative quota',
        slot_attribution='Administrative allocation of the same deterministic five-ID prefix, not distinct content judgments.',
        current_mapping=byrec[key]['AFTER_QUEUED'],pending_WA_body_fix_same_file=u['path']==units[RETRACTED]['path'],
        actual_read=False,content_verdict='UNASSIGNED',candidate_credit=0,preparer_content_verdict=None))
if (OUT/'new-proposals.json').exists():assert [r['id'] for r in json.loads((OUT/'new-proposals.json').read_text())]==chosen,'Preserve predecessor proposal history before changing IDs.'

overlay=dict(id=RETRACTED,findings=['SAMPLE-WA-002','FULL-032'],authority=str(authority_path),authority_sha256=sha(authority),authority_text=authority.decode(),
    earlier_manual_verdict=previous,original_source_identity=units[RETRACTED],
    effective_content_disposition='RETRACTED CLEAN-CONTENT CREDIT: compiler-confirmed assembly API-access documentation error',
    candidate_credit=0,read_count_change=0,unique_Core_runtime_bug_allocation=0,
    no_observed_runtime_logging_failure=True,pending_fix='FIDO guide body correction to public LoggerFactory plus real logging extension; no friend/API/runtime change authorized.',
    acceptance_gates=['Actual independent content/body review','Actual compilation in genuine WebAuthn assembly context','Exact corrected body/span/current hash refresh','Final sampler/coordinator reconciliation'],
    predecessor_status='1,067-unit candidate is immutable PREDECESSOR, not current final acceptance')
quotas=[]
for old in old_quotas:
    is_WA=(old['owner'],old['stratum'])==ROLE
    counted=[k for k in old['candidate_counted_ids'] if k!=RETRACTED]
    target=old['required_target']+(original_minimum if is_WA else 0)
    quotas.append(dict(owner=old['owner'],stratum=old['stratum'],conservative_original_population=old['conservative_original_population'],
        original_minimum=old['conservative_10_percent_minimum'],previous_authorized_expansion=old['authorized_expansion'],new_authorized_expansion=original_minimum if is_WA else 0,
        required_target=target,actually_read_supported_candidate_after_retraction=len(counted),candidate_counted_ids=counted,
        unread_reserved_ids=chosen if is_WA else [],unread_reserved_credit=0,actual_shortfall=max(0,target-len(counted)),
        projected_shortfall_if_all_new_reads_supported_and_eligible=max(0,target-len(counted)-(len(chosen) if is_WA else 0)),
        final_clean_count=None,no_cross_role_borrowing=True))
summary=dict(state='ADDITIVE RETRACTION/COMPILER EXPANSION CANDIDATE; READS AND WA BODY FIX PENDING',actual_unique_reads=1129,
    original_reads=983,actual_additional_reads=146,historical_broad_population=9352,original_WA_narrative_population=len(role_ids),
    derived_original_WA_narrative_quota=original_minimum,previous_candidate_count=1067,previous_target_sum=1067,
    supported_candidate_after_retraction=sum(q['actually_read_supported_candidate_after_retraction'] for q in quotas),
    new_authorized_full_category_quota=original_minimum,new_target_sum=sum(q['required_target'] for q in quotas),
    WA_narrative_remaining_supported=remaining_supported,WA_narrative_new_target=new_target,new_unread_proposals=len(proposals),
    actual_shortfall=sum(q['actual_shortfall'] for q in quotas),new_proposal_credit=0,
    five_prior_transfers='VERIFIED; receipts9 paths and current363 hash map exactly match predecessor projected AFTER',
    current_product_paths=363,WA_body_fix_product_path_already_present=units[RETRACTED]['path'] in current_products,
    WA_body_fix_before_sha256=current_products[units[RETRACTED]['path']],expected_new_product_path_for_WA_fix=False,
    final_accepted_denominator=None,final_accepted_clean_count=None,sampler_PASS=None,
    pending='Actual five readings; new WA corrected body/review/real compile; one subsequent exact body/hash/span refresh; final Astra/independent acceptance',
    preparer_content_verdicts=0)
assert summary['supported_candidate_after_retraction']==1066 and summary['new_target_sum']==1071 and summary['actual_shortfall']==5
for p,h in inputs.items():assert sha(Path(p).read_bytes())==h,('Bound record or product changed during preparation',p)
for p,h in predecessor_files.items():assert sha(Path(p).read_bytes())==h
emit('retraction-overlay.json',overlay);emit('new-proposals.json',proposals);emit('role-quota-status.json',quotas)
emit('replacement-pool-order.json',pool);emit('refreshed-current-read-mapping.json',refreshed_maps)
emit('five-transfer-refresh.json',dict(state='PRIOR FIVE TRANSFERS VERIFIED; NOT NEW WA BODY ACCEPTANCE',current_product_files=current_manifest['product_files'],
    predecessor_projection=str(FINAL/'projected-AFTER-products.json'),exact_projection_equality=True,receipts=receipts,
    mapped_actual_reads=1129,remaining_pending_correction='SAMPLE-WA-002/FULL-032 new body, review and compilation'))
emit('summary.json',summary);emit('predecessor-file-seal.json',predecessor_files)
text=['# Compiler WA addendum — new unread whole-unit proposals','','**No content verdicts or sample credit.** Source pin `'+PIN+'`. Read each whole unit and relevant enclosing context.','']
for p in proposals:
    body=original(p['path'])[p['start_character']:p['end_character_exclusive']]
    text+=['## `'+p['id']+'`',f"`{p['path']}:{p['first_line']}–{p['last_line']}`; original rank {p['original_rank']}; {p['slot_purpose']}.",'','````markdown',body,'````','']
(OUT/'original-units.md').write_text('\n'.join(text)+'\n')
report=['# Compiler-confirmed WA logging retraction and sampling addendum','','**For Astra and independent review. The previous 1,067-unit candidate remains an immutable predecessor, not final acceptance.**','',
    '`4886f819fa316d28` loses clean-content credit under SAMPLE-WA-002 / FULL-032. The original manual supported row remains verbatim in `retraction-overlay.json` with the exact overriding coordinator authority. Assembly API accessibility, not a runtime logging failure or new unique Core bug, is the recorded issue.',
    '## Required quota repair',
    f"The original WebAuthn narrative population is **{len(role_ids)}**; its original 10% minimum is **ceil({len(role_ids)}/10) = {original_minimum}**. Add that one full quota (**4**) and replace one lost supported slot. This role has **3** supported candidate readings toward target **8**, requiring exactly **5 new actual reads**. No role surplus is borrowed and the broad denominator stays **9,352**.",
    'Global existing-supported candidate count falls **1,067 → 1,066**; authorized target rises **1,067 → 1,071**. The five proposals have zero credit until Astra actually reads and accepts them with final unchanged-range eligibility. All other role targets and counted IDs remain unchanged. Actual read count remains **1,129 = 983 + 146**.',
    '## Deterministic new proposals']
for p in proposals:report.append(f"- `{p['id']}` — `{p['path']}:{p['first_line']}–{p['last_line']}`; original rank **{p['original_rank']}**; **UNREAD / UNASSIGNED / zero credit**.")
report+=['','[original-units.md](original-units.md) contains exact whole pinned units. `replacement-pool-order.json` records every role rank and skip reason. Same-file future WA-body flags are informational, not whole-file exclusions.',
    '## Completed transfer refresh, separate from new pending fix',
    'All five prior queued patches are transferred: the five `accepted-closeout-*` receipts cover nine paths, retained packet hashes verify, and current primary’s **363-path map exactly equals the predecessor projected AFTER map**. `refreshed-current-read-mapping.json` promotes those exact AFTER hashes/ranges to current for all 1,129 actual readings, while applying the retraction overlay.',
    'The WA Logging body fix remains pending. Its file is already in the 363-path map; no new product path is expected. Current hashes are explicitly **before the new body change**. Do not treat this successful prior-transfer refresh as acceptance of that future body.',
    '## Remaining gates',
    'Astra must read the five proposals; the FIDO editor must complete the bounded public-factory body correction; independent review and actual genuine-WebAuthn-context compilation must accept it. Then refresh its exact body/hash/spans and reconcile all same-file sampled units before final acceptance. Retain the failed compiler evidence and earlier supported verdict as history.',
    'Final denominator, clean count and sampler PASS remain null. No content judgment, product/Git/SDK/native/device/formatter execution or subagent by this worker. Finish Core Pass 1 publication/deferred handoff, then stop.']
(OUT/'addendum.md').write_text('\n'.join(report)+'\n')
emit('verification.json',dict(result='RECORDS-ONLY ADDENDUM PREPARED; NO CONTENT/SAMPLER PASS',input_sha256=inputs,
    outputs=[dict(path=str(p),sha256=sha(p.read_bytes()),bytes=p.stat().st_size) for p in sorted(OUT.iterdir()) if p.is_file() and p.name not in ['verification.json','preparation.stdout.json','structural-check.json']],
    sampler_PASS=None,final_accepted_clean_count=None))
print(json.dumps(summary,indent=2))
