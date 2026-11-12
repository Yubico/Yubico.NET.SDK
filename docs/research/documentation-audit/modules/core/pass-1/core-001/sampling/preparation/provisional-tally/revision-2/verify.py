#!/usr/bin/env python3
"""Independent records-only checks; not SDK tests, content approval, or a sampler pass."""
import collections
import hashlib
import json
from pathlib import Path
import re
import subprocess

RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PREP=RUN/'sampling/preparation';OLD=PREP/'provisional-tally';OUT=OLD/'revision-2'
PIN='fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
checks=[]


def sha(b):return hashlib.sha256(b).hexdigest()


def load(p):return json.loads(p.read_bytes())


def exact_ids(rows):
    keys=[r['id'] for r in rows];assert len(keys)==len(set(keys));return set(keys)


def verify_seal(receipt):
    for path,digest in receipt['input_sha256'].items():assert sha(Path(path).read_bytes())==digest,('Input seal',path)
    for row in receipt['outputs']:assert sha(Path(row['path']).read_bytes())==row['sha256'],('Output seal',row['path'])


receipt=load(OUT/'verification.json');verify_seal(receipt)
verify_seal(load(OLD/'verification.json'))
for row in load(OLD/'structural-check.json')['key_outputs']:
    assert sha(Path(row['path']).read_bytes())==row['sha256'],('Immutable predecessor receipt',row['path'])
checks.append('All current input/output seals and predecessor receipt seals match exact bytes.')
previous=load(OLD/'original-read-tally.json')+load(OLD/'additional-read-tally.json')
old_ids=exact_ids(previous);assert len(old_ids)==1069
old_queue=load(OLD/'unread-required.json');queue_ids=exact_ids(old_queue);assert len(queue_ids)==53 and not queue_ids&old_ids
rows=load(OUT/'completed-read-tally.json');read_ids=exact_ids(rows);assert len(read_ids)==1122 and read_ids==old_ids|queue_ids
latest=(RUN/'sampling/coordinator/remaining-53-verdicts.md').read_text()
supported_block=latest.split('The following 46 units are supported, subject to final current-span and overlap reconciliation:',1)[1].split('\n\nSpecific interpretation limits:',1)[0]
supported=re.findall(r'`([0-9a-f]{16})`',supported_block);assert len(supported)==len(set(supported))==46
zeros=[m.groups() for line in latest.splitlines() if (m:=re.fullmatch(r'\| `([0-9a-f]{16})` \| (.*) \|',line))]
assert len(zeros)==7 and {'core-001:unit:'+k for k in supported}|{'core-001:unit:'+k for k,_ in zeros}==queue_ids
byread={r['id']:r for r in rows};previous_byid={r['id']:r for r in previous}
for key,r in byread.items():
    if key in old_ids:
        for field in ['raw_category','raw_verdict_payload','authority','authority_sha256','actually_read_by']:
            assert r[field]==previous_byid[key][field],('Prior actual verdict changed',key,field)
    else:
        if key.split(':')[-1] in supported:assert r['raw_category']=='supported-pending-final-eligibility' and r['raw_verdict_payload']['coordinator_list']==supported_block
        else:assert r['raw_category']!='supported-pending-final-eligibility' and (key.split(':')[-1],r['raw_verdict_payload']['verdict_text']) in zeros
    b=Path(r['authority']).read_bytes();assert sha(b)==r['authority_sha256'];payload=r['raw_verdict_payload'];text=b.decode()
    if r['authority'].endswith('.json'):
        matches=[x for x in json.loads(text)['units'] if x['id']==key];assert len(matches)==1 and matches[0]==payload
    elif 'coordinator_list' in payload:
        assert payload['coordinator_list'] in text and '`'+key.split(':')[-1]+'`' in payload['coordinator_list']
    elif 'scope' in payload:
        assert payload['scope']=='Coordinator exact first-batch material-miss section' and payload['verdict_text'] in text
        assert '`'+key.split(':')[-1]+'`' in payload['verdict_text']
    else:
        suffix=key.split(':')[-1];verdict=payload['verdict_text']
        assert '| `'+suffix+'` | '+verdict+' |' in text or '- `'+suffix+'`: '+verdict in text
checks.append('Exact 983+86+53 ID union; 46 explicit supported IDs and seven zero rows; every prior actual payload preserved and matched to its authority format.')

units={r['id']:r for r in load(RUN/'inventory/semantic-units.json')}
files={r['path']:r for r in load(RUN/'inventory/files.json')}
population=load(PREP/'population.json')['units'];pop={r['id']:r for r in population}
source_cache={}


def original(path):
    if path not in source_cache:
        pinned=subprocess.check_output(['git','show',PIN+':'+path]);normalized=pinned.decode('utf-8-sig').replace('\r\n','\n')
        candidates=[pinned]+[(bom+normalized.replace('\n',newline)).encode() for bom in ['', '\ufeff'] for newline in ['\n','\r\n']]
        current=Path(path).read_bytes()
        if sha(current)==files[path]['sha256'] and current.decode('utf-8-sig').replace('\r\n','\n')==normalized:candidates.append(current)
        b=next((x for x in candidates if sha(x)==files[path]['sha256']),None)
        assert b is not None,('Exact inventory BOM/newline representation unavailable',path)
        source_cache[path]=(b,b.decode('utf-8-sig'))
    return source_cache[path]


mapping=load(OUT/'current-mapping.json');assert exact_ids(mapping)==read_ids
next_rows=load(OUT/'next-required-selection.json');next_ids=exact_ids(next_rows);assert len(next_ids)==7 and not next_ids&read_ids
next_maps=load(OUT/'next-required-current-mapping.json');assert exact_ids(next_maps)==next_ids
current_cache={}
for m in mapping+next_maps:
    u=units[m['id']];raw,text=original(u['path']);body=text[u['start_character']:u['end_character_exclusive']]
    assert sha(raw)==u['source_sha256'] and sha(body.encode())==u['range_sha256']
    if u['path'] not in current_cache:current_cache[u['path']]=Path(u['path']).read_bytes()
    current=current_cache[u['path']];assert sha(current)==m['current_file_sha256']
    assert m['mapping_never_grants_read_or_clean_credit']
    if m['state']=='ORIGINAL-OVERLAP-CHANGED-OR-MAPPING-RECONCILIATION-REQUIRED':
        assert m['id'] in read_ids and byread[m['id']]['credit_withholding_reasons']
    else:
        normalized=current.decode('utf-8-sig').replace('\r\n','\n');a=m['current_normalized_start_character'];b=m['current_normalized_end_character_exclusive']
        assert normalized[a:b]==body.replace('\r\n','\n')
        assert m['current_first_line']==normalized.count('\n',0,a)+1 and m['current_last_line']==normalized.count('\n',0,max(a,b-1))+1
    if m['id'] in next_ids:assert not m['known_accepted_edited_overlap'] and not m['known_reviewed_queued_overlap']
manifest=load(OUT/'accepted-products-at-capture.json');assert manifest==load(RUN/'editing/integration/current-products.json')
for r in manifest['product_files']:assert sha(Path(r['path']).read_bytes())==r['sha256']
checks.append('All 1,122 completed mappings and seven proposed mappings are bound to exact current hashes; 1,109 completed original units match exact normalized current slices; 13 changed-range holds remain explicit.')

targets=load(OLD/'role-quota-status.json');quotas=load(OUT/'role-quota-status.json')
assert len(quotas)==len(targets) and sum(q['conservative_target'] for q in quotas)==1067
assert sum(q['original_quota'] for q in quotas)==983 and sum(q['already_authorized_full_expansion'] for q in quotas)==84
assert sum(q['actually_read_source_supported_without_known_hold'] for q in quotas)==1060
assert sum(q['remaining_need'] for q in quotas)==7 and sum(q['shortfall'] for q in quotas)==0
assert sum(r['eligibility']=='candidate-eligible' for r in population)==9352
pool={r['id']:r for r in load(OUT/'replacement-pool-exclusions.json')}
retired=set(load(OLD/'superseded-proposals.json')['retired_SW_ids']);assert len(retired)==4 and not next_ids&retired
for q in quotas:
    old=next(x for x in targets if (x['owner'],x['stratum'])==(q['owner'],q['stratum']))
    for field in ['broad_original_population','original_quota','already_authorized_full_expansion']:assert q[field]==old[field]
    assert q['conservative_target']==old['conservative_required_target']
    eligible={r['id'] for r in rows if (r['owner'],r['stratum'])==(q['owner'],q['stratum']) and not r['credit_withholding_reasons']}
    assert len(eligible)==q['actually_read_source_supported_without_known_hold']
    assert q['remaining_need']==max(0,q['conservative_target']-len(eligible))
    ranked=sorted([k for k,r in pop.items() if (r['owner'],r.get('stratum'))==(q['owner'],q['stratum']) and r['eligibility']=='candidate-eligible'],
        key=lambda k:sha(('core-001-clean-sampling-preparation-v1|'+q['owner']+'|'+q['stratum']+'|'+k).encode()))
    if q['remaining_need']:
        available=[k for k in ranked if k not in read_ids|retired and not pop[k].get('edited_overlap') and k not in pool]
        assert q['new_ids']==available[:q['remaining_need']] and q['available_pool']==len(available)
    assert len(q['new_ids'])==q['selected']
    for key in q['new_ids']:assert next(r for r in next_rows if r['id']==key)['original_rank']==ranked.index(key)+1
for r in next_rows:
    assert r['read_status']=='PROPOSED-UNREAD' and r['verdict']=='UNASSIGNED' and r['clean_credit']==0
    u=units[r['id']];_,text=original(u['path']);body=text[u['start_character']:u['end_character_exclusive']]
    assert body in (OUT/'next-required-original-units.md').read_text() and r['id'] in (OUT/'next-required-original-units.md').read_text()
    assert 'CliRunner.cs' not in r['path'] and r['path']!='src/OpenPgp/src/PrivateKeyImport.cs'
checks.append('All owner/role targets preserved: 983+84=1,067, 1,060 supported without known holds, exactly seven next-ranked shortfall replacements, no repeated/retired IDs or pool shortfall.')

preambles={r['id']:r for r in load(OUT/'preamble-screen-evidence.json')}
assert preambles['core-001:unit:f7fc274baa684518']['pure_administrative']
for suffix in ['b4125585d8579c68','7d926f1edba94468','2ab1f3fe98ee5ca4']:
    r=preambles['core-001:unit:'+suffix];assert not r['pure_administrative'] and r['substantive_XML_text_or_reference_present']
assert any('CliRunner credential/discovery' in r['reason'] for r in pool.values())
assert any('X25519' in r['reason'] for r in pool.values())
checks.append('Whole-unit pure preamble and three substantive XML-summary/reference probes preserved; known credential/X25519 paths excluded from the new pool only.')

# Reconstruct the synthetic original-scope PGP patch, including exact unterminated EOF semantics.
patch_lines=(OUT/'reviewed-queued-PGP-original-scope.diff').read_text().splitlines(keepends=True)
sections=[];i=0
while i<len(patch_lines):
    if patch_lines[i].startswith('diff --git '):
        end=next((k for k in range(i+1,len(patch_lines)) if patch_lines[k].startswith('diff --git ')),len(patch_lines))
        sections.append(patch_lines[i:end]);i=end
    else:i+=1
assert len(sections)==2
pgp_dir=Path('/home/rupicola/Code/y/yubikit-core-docs-edit-pgp-piv')/RUN/'editing/PGP-boundary/sampling-UIF-fallback'
for section in sections:
    p=section[1][4:].strip().split('/',1)[1];_,base=original(p);old=base.replace('\r\n','\n').splitlines(keepends=True)
    output=[];old_cursor=0;i=3
    while i<len(section):
        m=re.match(r'@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@',section[i]);assert m
        count=int(m[2]) if m[2] is not None else 1;start=int(m[1])-1 if count else int(m[1]);output+=old[old_cursor:start];old_cursor=start;i+=1
        consumed=0
        while i<len(section) and not section[i].startswith('@@ '):
            line=section[i];kind=line[0];value=line[1:];assert kind in ' +-'
            if i+1<len(section) and section[i+1].startswith('\\ No newline at end of file'):
                assert value.endswith('\n');value=value[:-1];i+=1
            if kind in ' -':assert old[old_cursor]==value;old_cursor+=1;consumed+=1
            if kind in ' +':output.append(value)
            i+=1
        assert consumed==count
    output+=old[old_cursor:]
    assert ''.join(output)==(pgp_dir/'after'/p).read_bytes().decode('utf-8-sig').replace('\r\n','\n')
edits=load(OUT/'known-edit-overlap-inputs.json')
assert sum(len(v) for v in edits['PGP_queued_only_spans'].values())==2
assert edits['PGP_EOF_evidence'][0]['pinned_final_newline'] is False
pending=load(OUT/'pending-corrections.json');assert len(pending)==4
assert pending[0]['patch_sha256']=='d70a50aaf297cc62fd6c7fa93f13e8cfb1acb44fe975279dc9fb60fc30e5b05e'
assert pending[1]['patch_sha256']=='9d5433109986b2037f47654d912970b7618cc84c336873950a5d1fecba33e2c5'
checks.append('Reviewed queued PGP original-scope diff reconstructs exact after snapshots, including pinned unterminated EOF; queued-only spans are distinct from accepted-plus-queued scope. Four pending work scopes remain flagged.')

summary=load(OUT/'summary.json')
for r in [receipt,summary]:
    assert r['final_denominator'] is None and r['final_clean_credit'] is None and r['sampler_PASS'] is None and r['preparer_content_verdicts']==0
assert summary['next_required']==7 and summary['supported_without_current_known_hold']==1060
for path in subprocess.check_output(['git','ls-tree','-r','--name-only','01a114d4','--',str(RUN/'inventory')],text=True).splitlines()+[str(RUN/'coverage.json')]:
    assert Path(path).read_bytes()==subprocess.check_output(['git','show','01a114d4:'+path])
for path,b in current_cache.items():assert Path(path).read_bytes()==b
verify_seal(receipt)
checks.append('Original inventory and coverage remain byte-unchanged; no final denominator, clean credit, or sampler PASS issued.')
out=dict(result='RECORDS-ONLY STRUCTURAL CHECKS; NOT CONTENT/SAMPLER PASS',actual_unique_reads=1122,new_actual_reads=53,supported_list=46,zero_table=7,
    current_mapping_records=1122,unchanged_exact_current_slices=1109,changed_range_holds=13,proposed_unread=7,
    conservative_broad_population=9352,conservative_target=1067,supported_without_known_hold=1060,new_full_category_quota=0,
    final_denominator=None,final_clean_credit=None,sampler_PASS=None,checks=checks,
    sealed_outputs=[dict(path=str(p),sha256=sha(p.read_bytes()),bytes=p.stat().st_size) for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='structural-check.json'])
(OUT/'structural-check.json').write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps({k:v for k,v in out.items() if k!='sealed_outputs'},indent=2))
