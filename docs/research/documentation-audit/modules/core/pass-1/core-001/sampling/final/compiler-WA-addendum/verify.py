#!/usr/bin/env python3
"""Records-only checks for the additive retraction and five unread proposals."""
import ast
import hashlib
import json
import math
from pathlib import Path
import re

RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PREP=RUN/'sampling/preparation';FINAL=RUN/'sampling/final';OUT=FINAL/'compiler-WA-addendum'
KEY='core-001:unit:4886f819fa316d28';ROLE=('WebAuthn','narrative-examples-and-maps')


def sha(b):return hashlib.sha256(b).hexdigest()


def load(p):return json.loads(Path(p).read_bytes())


receipt=load(OUT/'verification.json')
for p,h in receipt['input_sha256'].items():assert sha(Path(p).read_bytes())==h,('Input drift',p)
for r in receipt['outputs']:assert sha(Path(r['path']).read_bytes())==r['sha256'],('Output drift',r['path'])
predecessor=load(OUT/'predecessor-file-seal.json')
for p,h in predecessor.items():assert sha(Path(p).read_bytes())==h
assert set(predecessor)=={str(p) for p in FINAL.iterdir() if p.is_file()}
prior=load(FINAL/'summary.json');assert prior['candidate_supported_unchanged_after_all_five_patches']==1067
oldactual=load(FINAL/'actual-read-verdicts.json');byread={r['id']:r for r in oldactual};assert len(byread)==len(oldactual)==1129
overlay=load(OUT/'retraction-overlay.json')
assert overlay['earlier_manual_verdict']==byread[KEY] and byread[KEY]['raw_category']=='supported-pending-final-eligibility'
assert overlay['earlier_manual_verdict']['candidate_credit']==1 and overlay['candidate_credit']==0
assert overlay['read_count_change']==0 and overlay['unique_Core_runtime_bug_allocation']==0
assert overlay['findings']==['SAMPLE-WA-002','FULL-032']
authority=Path(overlay['authority']).read_bytes();assert sha(authority)==overlay['authority_sha256'] and authority.decode()==overlay['authority_text']
assert KEY in authority.decode() and 'CS1929' in authority.decode()
checks=['All predecessor final files remain byte-exact. Earlier supported verdict preserved verbatim; exact coordinator compiler retraction overlays zero credit, no read-count or runtime-bug increment.']

refresh=load(OUT/'five-transfer-refresh.json');manifest=load(RUN/'editing/integration/current-products.json')
assert refresh['current_product_files']==manifest['product_files']
current={r['path']:r['sha256'] for r in manifest['product_files']}
projected={r['path']:r['sha256'] for r in load(FINAL/'projected-AFTER-products.json')['product_files']}
assert current==projected and len(current)==363
for p,h in current.items():assert sha(Path(p).read_bytes())==h
assert len(refresh['receipts'])==5 and len({r['patch_sha256'] for r in refresh['receipts']})==5
assert sum(len(r['products']) for r in refresh['receipts'])==9
for r in refresh['receipts']:
    actual=load(r['receipt']);assert sha(Path(r['receipt']).read_bytes())==r['sha256'] and actual['product_files']==r['products']
    assert actual['patch_sha256']==r['patch_sha256']
    for product in r['products']:assert product['sha256']==current[product['path']]
    for product in actual['retained_records']:assert sha(Path(product['path']).read_bytes())==product['sha256']
oldmaps={r['id']:r for r in load(FINAL/'population-and-range-reconciliation.json')}
maps=load(OUT/'refreshed-current-read-mapping.json');assert len(maps)==1129 and {r['id'] for r in maps}==byread.keys()
for m in maps:
    old=oldmaps[m['id']];assert m['CURRENT']==old['AFTER_QUEUED']
    assert sha(Path(m['path']).read_bytes())==m['CURRENT']['file_sha256']
    assert m['candidate_credit']==(0 if m['id']==KEY else old['candidate_credit'])
checks.append('Five transfer receipts/nine paths and current363 hashes exactly match predecessor AFTER projection. All1,129 actual read maps refreshed to matching primary bytes, with only the mandatory credit retraction.')

pop={r['id']:r for r in load(PREP/'population.json')['units']};units={r['id']:r for r in load(RUN/'inventory/semantic-units.json')}
files={r['path']:r for r in load(RUN/'inventory/files.json')}
role_ids={k for k,p in pop.items() if p['eligibility']=='candidate-eligible' and (p['owner'],p['stratum'])==ROLE}
assert len(role_ids)==33 and math.ceil(len(role_ids)/10)==4
prior_quotas=load(FINAL/'owner-role-quotas.json');quotas=load(OUT/'role-quota-status.json');assert len(quotas)==len(prior_quotas)==94
new=load(OUT/'new-proposals.json');new_ids=[r['id'] for r in new];assert len(new_ids)==len(set(new_ids))==5 and not set(new_ids)&byread.keys()
for q in quotas:
    old=next(r for r in prior_quotas if (r['owner'],r['stratum'])==(q['owner'],q['stratum']))
    is_WA=(q['owner'],q['stratum'])==ROLE
    assert q['conservative_original_population']==old['conservative_original_population'] and q['original_minimum']==old['conservative_10_percent_minimum']
    assert q['previous_authorized_expansion']==old['authorized_expansion']
    assert q['new_authorized_expansion']==(4 if is_WA else 0)
    assert q['required_target']==old['required_target']+(4 if is_WA else 0)
    assert q['candidate_counted_ids']==[k for k in old['candidate_counted_ids'] if k!=KEY]
    assert q['actually_read_supported_candidate_after_retraction']==len(q['candidate_counted_ids'])
    assert q['unread_reserved_credit']==0 and q['actual_shortfall']==(5 if is_WA else 0)
    assert q['unread_reserved_ids']==(new_ids if is_WA else []) and q['projected_shortfall_if_all_new_reads_supported_and_eligible']==0
assert sum(q['conservative_original_population'] for q in quotas)==9352
assert sum(q['required_target'] for q in quotas)==1071 and sum(q['actually_read_supported_candidate_after_retraction'] for q in quotas)==1066
assert sum(q['new_authorized_expansion'] for q in quotas)==4 and sum(q['actual_shortfall'] for q in quotas)==5
rank=sorted(role_ids,key=lambda k:sha(('core-001-clean-sampling-preparation-v1|'+ROLE[0]+'|'+ROLE[1]+'|'+k).encode()))
pool=load(OUT/'replacement-pool-order.json');assert [r['id'] for r in pool]==rank
assert new_ids==[r['id'] for r in pool if r['eligible_for_new_unread_proposal']][:5]
assert [r['original_rank'] for r in new]==[rank.index(k)+1 for k in new_ids]
checks.append('Original narrative population33 gives quota4. Role supported3/target8 requires exactly5 unread units; the other93 role records stay unchanged, conservative population9352, global supported1066/target1071, no borrowing or shrink.')

tree=ast.parse((FINAL/'prepare.py').read_bytes());ns={'re':re}
for name in ['parse_patch','apply','normalized']:
    node=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name==name)
    exec(compile(ast.Module(body=[node],type_ignores=[]),str(FINAL/'prepare.py'),'exec'),ns)
accepted=ns['parse_patch']((PREP/'provisional-tally/known-accepted-selected-role-scope.diff').read_bytes());queued={}
for packet in load(FINAL/'queued-patch-reconciliation.json'):queued.update(ns['parse_patch'](Path(packet['patch']).read_bytes()))
cache={};excerpts=(OUT/'original-units.md').read_text()
for r in new:
    u=units[r['id']];p=u['path'];b=Path(p).read_bytes();text=ns['normalized'](b)
    if p not in cache:
        original=text
        if p in queued:original=ns['apply'](original,queued[p],True)
        if p in accepted:original=ns['apply'](original,accepted[p],True)
        options=([b] if text==original else [])+[(bom+original.replace('\n',newline)).encode() for bom in ['', '\ufeff'] for newline in ['\n','\r\n']]
        raw=next(v for v in options if sha(v)==files[p]['sha256']);cache[p]=raw.decode('utf-8-sig')
    body=cache[p][u['start_character']:u['end_character_exclusive']];assert sha(body.encode())==u['range_sha256']
    assert body in excerpts and r['id'] in excerpts
    m=r['current_mapping'];assert sha(b)==m['file_sha256'] and text[m['start_character_normalized']:m['end_character_exclusive_normalized']]==body.replace('\r\n','\n')
    assert not m['original_span_overlap'] and r['actual_read'] is False and r['candidate_credit']==0 and r['content_verdict']=='UNASSIGNED'
checks.append('Five new units are the exact next deterministic eligible prefix, with original hashes/ranges and exact current whole-unit matches; excerpts complete, all unread/unassigned/zero credit.')
summary=load(OUT/'summary.json');assert summary['actual_unique_reads']==1129 and summary['new_target_sum']==1071 and summary['supported_candidate_after_retraction']==1066
for k in ['final_accepted_denominator','final_accepted_clean_count','sampler_PASS']:assert summary[k] is None
assert summary['WA_body_fix_product_path_already_present'] and not summary['expected_new_product_path_for_WA_fix']
assert summary['WA_body_fix_before_sha256']==current[units[KEY]['path']]
for p,h in predecessor.items():assert sha(Path(p).read_bytes())==h
for p,h in receipt['input_sha256'].items():assert sha(Path(p).read_bytes())==h
checks.append('Prior transfer refresh does not accept pending WA body: genuine-context compilation, body review, exact later hash/span refresh and final sampler acceptance remain explicit gates.')
result=dict(result='RECORDS-ONLY ADDENDUM STRUCTURAL CHECKS VERIFIED; NO SAMPLER PASS',actual_unique_reads=1129,
    predecessor_candidate=1067,supported_after_retraction=1066,new_target=1071,WA_narrative_population=33,derived_original_quota=4,new_unread=5,
    prior_five_transfers_verified=True,current_manifest_paths=363,final_accepted_clean_count=None,sampler_PASS=None,checks=checks,
    sealed_outputs=[dict(path=str(p),sha256=sha(p.read_bytes()),bytes=p.stat().st_size) for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='structural-check.json'])
(OUT/'structural-check.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k!='sealed_outputs'},indent=2))
