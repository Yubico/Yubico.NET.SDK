#!/usr/bin/env python3
"""Verify candidate records and exact byte/range identities, without Git or SDK execution."""
import ast
import collections
import hashlib
import json
import math
from pathlib import Path
import re

RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PREP=RUN/'sampling/preparation';OLD=PREP/'provisional-tally';REV=OLD/'revision-2';OUT=RUN/'sampling/final'


def sha(b):return hashlib.sha256(b).hexdigest()


def load(p):return json.loads(Path(p).read_bytes())


def seal_check(receipt):
    for path,digest in receipt['input_sha256'].items():assert sha(Path(path).read_bytes())==digest,('Bound input drift',path)
    for r in receipt['outputs']:assert sha(Path(r['path']).read_bytes())==r['sha256'],('Output drift',r['path'])


receipt=load(OUT/'verification.json');seal_check(receipt)
for path,digest in load(OUT/'historical-records-and-input-seal.json').items():assert sha(Path(path).read_bytes())==digest
for r in load(REV/'structural-check.json')['sealed_outputs']:assert sha(Path(r['path']).read_bytes())==r['sha256']
checks=['All predecessor/input/output hashes remain exact; no historical population, selection, verdict or disposition ledger was rewritten.']
summary=load(OUT/'summary.json');pop={r['id']:r for r in load(PREP/'population.json')['units']}
units={r['id']:r for r in load(RUN/'inventory/semantic-units.json')};files={r['path']:r for r in load(RUN/'inventory/files.json')}
broad={k for k,r in pop.items() if r['eligibility']=='candidate-eligible'};assert len(pop)==14044 and len(broad)==9352
selection=load(PREP/'selection.json');selected={r['id'] for r in selection};assert len(selection)==len(selected)==983
rows=load(OUT/'actual-read-verdicts.json');byread={r['id']:r for r in rows};assert len(rows)==len(byread)==1129 and selected<=byread.keys()
old_reads=load(REV/'completed-read-tally.json');assert len(old_reads)==1122
for old in old_reads:
    r=byread[old['id']]
    for field in ['raw_category','raw_verdict_payload','authority','authority_sha256','actually_read_by']:assert r[field]==old[field]
last_path=RUN/'sampling/coordinator/last-seven-verdicts.md'
last=[m.groups() for line in last_path.read_text().splitlines() if (m:=re.fullmatch(r'\| `([0-9a-f]{16})` \| (.*) \|',line))]
last_ids={'core-001:unit:'+k for k,_ in last};assert len(last_ids)==7 and last_ids=={r['id'] for r in load(REV/'next-required-selection.json')}
assert byread.keys()=={r['id'] for r in old_reads}|last_ids and len(byread.keys()-selected)==146
for r in rows:
    b=Path(r['authority']).read_bytes();assert sha(b)==r['authority_sha256'];text=b.decode();payload=r['raw_verdict_payload'];suffix=r['id'].split(':')[-1]
    if r['authority'].endswith('.json'):assert next(v for v in json.loads(text)['units'] if v['id']==r['id'])==payload
    elif 'coordinator_list' in payload:assert payload['coordinator_list'] in text and '`'+suffix+'`' in payload['coordinator_list']
    elif 'scope' in payload:assert payload['verdict_text'] in text and '`'+suffix+'`' in payload['verdict_text']
    else:assert '| `'+suffix+'` | '+payload['verdict_text']+' |' in text or '- `'+suffix+'`: '+payload['verdict_text'] in text
    assert r['all_proposal_occurrences'] and r['preparer_content_verdict'] is None
checks.append('Exact 983+146=1,129 actual-ID union, including seven explicit final table rows; all source verdict payloads/qualifications and proposal provenance preserved.')

# Load only pure parser/apply functions from the generator. Never import or execute its writer.
source=(OUT/'prepare.py').read_bytes();tree=ast.parse(source);ns={'re':re}
for name in ['parse_patch','apply','normalized']:
    node=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name==name)
    exec(compile(ast.Module(body=[node],type_ignores=[]),str(OUT/'prepare.py'),'exec'),ns)
parse=ns['parse_patch'];apply=ns['apply'];normalized=ns['normalized']
packets=load(OUT/'queued-patch-reconciliation.json');assert len(packets)==5
central=(RUN/'editing/integration/queued-sampling-corrections.md').read_text()
central_hashes=re.findall(r'\| `([0-9a-f]{64})` \|',central)
assert [p['patch_sha256'] for p in packets]==central_hashes
projected={};changed_paths=set()
for packet in packets:
    b=Path(packet['patch']).read_bytes();assert sha(b)==packet['patch_sha256']
    review=load(packet['review']);assert sha(Path(packet['review']).read_bytes())==packet['review_sha256']
    assert review.get('verdict',review.get('outcome',review.get('decision')))=='PASS'
    hunks=parse(b);assert set(hunks)=={r['path'] for r in packet['products']}
    for r in packet['products']:
        p=r['path'];before=Path(p).read_bytes();assert sha(before)==r['before_sha256'] and p not in changed_paths
        after_text=apply(normalized(before),hunks[p]);assert apply(after_text,hunks[p],True)==normalized(before)
        variants=[(bom+after_text.replace('\n',newline)).encode() for bom in ['', '\ufeff'] for newline in ['\n','\r\n']]
        after=next((x for x in variants if sha(x)==r['projected_AFTER_sha256']),None);assert after is not None
        projected[p]=after;changed_paths.add(p)
assert len(changed_paths)==9
manifest=load(OUT/'current-products-at-capture.json');assert manifest==load(RUN/'editing/integration/current-products.json') and len(manifest['product_files'])==361
for r in manifest['product_files']:assert sha(Path(r['path']).read_bytes())==r['sha256']
projected_manifest={r['path']:r['sha256'] for r in load(OUT/'projected-AFTER-products.json')['product_files']}
assert len(projected_manifest)==363
for r in manifest['product_files']:assert projected_manifest[r['path']]==sha(projected[r['path']]) if r['path'] in projected else projected_manifest[r['path']]==r['sha256']
for p,b in projected.items():assert projected_manifest[p]==sha(b)
checks.append('All five central patch hashes/reviewer PASS records and nine before/AFTER identities verified; exact in-memory application reverses to reserved primary. Primary361 and projected363 maps are distinct.')

accepted=parse((OLD/'known-accepted-selected-role-scope.diff').read_bytes());originals={};current={}


def original(path):
    if path not in originals:
        b=Path(path).read_bytes();current[path]=b;text=normalized(b)
        if path in accepted:text=apply(text,accepted[path],True)
        variants=([b] if normalized(b)==text else [])+[(bom+text.replace('\n',newline)).encode() for bom in ['', '\ufeff'] for newline in ['\n','\r\n']]
        raw=next((v for v in variants if sha(v)==files[path]['sha256']),None);assert raw is not None
        originals[path]=raw.decode('utf-8-sig')
    return originals[path]


rec=load(OUT/'population-and-range-reconciliation.json');byrec={r['id']:r for r in rec};assert len(rec)==len(byrec)
assert broad<=byrec.keys() and byread.keys()<=byrec.keys() and byrec.keys()<=pop.keys()
for r in rec:
    u=units[r['id']];raw=original(u['path']);body=raw[u['start_character']:u['end_character_exclusive']]
    assert sha(body.encode())==u['range_sha256']
    for state in ['CURRENT','AFTER_QUEUED']:
        m=r[state];b=projected.get(u['path'],current[u['path']]) if state=='AFTER_QUEUED' else current[u['path']]
        assert sha(b)==m['file_sha256']
        if m['exact_unit_match']:
            text=normalized(b);a=m['start_character_normalized'];z=m['end_character_exclusive_normalized']
            assert text[a:z]==body.replace('\r\n','\n')
            assert m['first_line']==text.count('\n',0,a)+1 and m['last_line']==text.count('\n',0,max(a,z-1))+1
        assert not m['metadata_unknown']
    if r['candidate_credit']:
        actual=byread[r['id']]
        assert actual['raw_category']=='supported-pending-final-eligibility' and not actual.get('sampling_credit_adjudication')
        assert r['id'] in broad and r['CURRENT']['exact_unit_match'] and r['AFTER_QUEUED']['exact_unit_match']
        assert not r['CURRENT']['original_span_overlap'] and not r['AFTER_QUEUED']['original_span_overlap'] and not r['credit_withholding_reasons']
    if r['CURRENT']['original_span_overlap'] or r['AFTER_QUEUED']['original_span_overlap']:assert r['candidate_credit']==0
    assert r['retained_in_conservative_quota_denominator']==(r['id'] in broad) and r['whole_file_exclusion'] is False
    assert r['final_clean_credit'] is None
assert sum(r['candidate_credit'] for r in rec)==1067
assert sum(not byrec[k]['candidate_credit'] for k in byread)==62
assert len(load(OUT/'eligibility-deltas.json'))==81
checks.append('Every admitted original unit and every actual read matched by original range hash against CURRENT and projected AFTER. All exact edited overlaps receive zero candidate credit; 62 actual reads withheld, 81 explicit population deltas, no metadata unknowns or whole-file exclusions.')

quotas=load(OUT/'owner-role-quotas.json');prior=load(REV/'role-quota-status.json');assert len(quotas)==len(prior)==94
for q in quotas:
    old=next(x for x in prior if (x['owner'],x['stratum'])==(q['owner'],q['stratum']))
    role_ids={k for k in broad if (pop[k]['owner'],pop[k]['stratum'])==(q['owner'],q['stratum'])}
    ids={k for k in role_ids if byrec[k]['candidate_credit']}
    assert len(role_ids)==q['conservative_original_population']==old['broad_original_population']
    assert math.ceil(len(role_ids)/10)==q['conservative_10_percent_minimum']==old['original_quota']
    assert q['authorized_expansion']==old['already_authorized_full_expansion']
    assert q['required_target']==old['conservative_target'] and q['candidate_supported_unchanged_after_all_five_patches']==len(ids)
    assert set(q['candidate_counted_ids'])==ids and q['candidate_shortfall']==0 and q['candidate_target_met']
assert sum(q['conservative_original_population'] for q in quotas)==9352
assert sum(q['required_target'] for q in quotas)==1067
assert sum(q['conservative_10_percent_minimum'] for q in quotas)==983
assert sum(q['authorized_expansion'] for q in quotas)==84
assert load(OUT/'next-bounded-proposals.json')==[]
owners=load(OUT/'owner-totals.json');assert len(owners)==17 and sum(o['actual_unique_reads'] for o in owners)==1129
assert sum(o['candidate_counted'] for o in owners)==1067 and all(o['all_role_targets_met'] for o in owners)
boundaries=load(OUT/'boundary-completion.json');coverage=load(RUN/'coverage-progress.json')
assert {r['owner'] for r in boundaries}==set(coverage['completed_boundaries'])
for r in boundaries:assert r['original_coverage_record']==coverage['completed_boundaries'][r['owner']]
checks.append('All 94 roles across 17 owners meet unchanged conservative minima983 plus authorized expansions84; candidate target1067, shortfall0. Original qualified boundary returns preserved; no replacement queue needed.')

pf=load(OUT/'PF02-population-overlay.json');assert len(pf['added_evidence_symbols'])==5
assert summary['PF02_changed_admitted_population_ids']==['core-001:unit:f3504065f8d80ef8']
changed=byrec['core-001:unit:f3504065f8d80ef8'];assert changed['id'] not in byread and changed['AFTER_QUEUED']['original_span_overlap'] and changed['candidate_credit']==0
assert set(summary['PF02_changed_already_excluded_map_ids'])=={'core-001:unit:9081bf97fe2c9fb3','core-001:unit:a4dbc5b8c7e1ddcb'}
l4=byrec['core-001:unit:4353e889f8fe39ad'];assert l4['retained_in_conservative_quota_denominator'] and l4['unchanged_population_candidate']
assert not l4['AFTER_QUEUED']['original_span_overlap']
history=load(OUT/'replacement-history-policy.json');assert len(history['retired_provisional_SW_ids'])==4 and not set(history['retired_provisional_SW_ids'])&byread.keys()
assert history['repeated_activation_read_increment']==0
checks.append('PF02 unsampled admitted prose change withheld, two previously excluded map entries stay excluded, and unchanged admitted L4 map entry retained. Five inserted keys create no new original population/read credit; retired/activation history remains nonadditive.')
for field in ['final_accepted_denominator','final_accepted_clean_count','sampler_PASS']:assert summary[field] is None
assert summary['candidate_shortfall']==0 and summary['all_five_actual_patches_considered'] and summary['preparer_content_verdicts']==0
for p,b in current.items():assert Path(p).read_bytes()==b
seal_check(receipt)
checks.append('Reserved primary and every bound input remain unchanged through verification; Astra/independent acceptance and one post-transfer hash/map refresh explicitly pending.')
output=dict(result='STRUCTURAL CHECKS VERIFIED FOR REVIEW CANDIDATE; NOT SAMPLER PASS',actual_unique_reads=1129,
    conservative_population=9352,original_minimums=983,authorized_expansions=84,required_targets=1067,
    candidate_supported_unchanged_after_all_five_patches=1067,owner_roles=94,owners=17,shortfall=0,
    primary_manifest_paths=361,projected_AFTER_manifest_paths=363,queued_patches=5,queued_paths=9,
    final_accepted_denominator=None,final_accepted_clean_count=None,sampler_PASS=None,
    Astra_and_independent_review='PENDING',post_transfer_hash_map_refresh='PENDING ONCE AFTER ALL FIVE TRANSFERS',checks=checks,
    sealed_outputs=[dict(path=str(p),sha256=sha(p.read_bytes()),bytes=p.stat().st_size) for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='structural-check.json'])
(OUT/'structural-check.json').write_text(json.dumps(output,indent=2)+'\n')
print(json.dumps({k:v for k,v in output.items() if k!='sealed_outputs'},indent=2))
