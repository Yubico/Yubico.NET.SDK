#!/usr/bin/env python3
"""Records-only final reconciliation candidate. No Git/SDK/product writes or content judgments."""
import bisect
import collections
import difflib
import hashlib
import json
import math
from pathlib import Path
import re

RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PREP=RUN/'sampling/preparation';OLD=PREP/'provisional-tally';REV=OLD/'revision-2';OUT=RUN/'sampling/final'
PIN='fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
inputs={};current={};originals={};projected={};maps={}


def sha(b):return hashlib.sha256(b).hexdigest()


def read(p):
    b=Path(p).read_bytes();inputs[str(p)]=sha(b);return b


def load(p):return json.loads(read(p))


def emit(name,data):(OUT/name).write_text(json.dumps(data,indent=2)+'\n')


def normalized(b):return b.decode('utf-8-sig').replace('\r\n','\n')


def primary(p):
    if p not in current:current[p]=read(Path(p))
    return current[p]


def parse_patch(b):
    """Parse complete unified hunks, including exact no-newline data-line markers."""
    lines=b.decode('utf-8-sig').replace('\r\n','\n').splitlines(keepends=True);result={};i=0
    while i<len(lines):
        if not lines[i].startswith('--- '):i+=1;continue
        p=lines[i][4:].rstrip('\n').split('\t')[0];q=lines[i+1][4:].rstrip('\n').split('\t')[0]
        assert (p.startswith('a/') and q=='b/'+p[2:]) or (p.startswith('i/') and q=='w/'+p[2:])
        p=p[2:];assert p not in result
        result[p]=[];i+=2
        while i<len(lines) and not lines[i].startswith(('diff --git ','--- ')):
            if not lines[i].startswith('@@ '):i+=1;continue
            m=re.match(r'@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@',lines[i]);assert m
            oldcount=int(m[2]) if m[2] is not None else 1;newcount=int(m[4]) if m[4] is not None else 1
            h=dict(old_start=int(m[1])-1 if oldcount else int(m[1]),old_count=oldcount,
                new_start=int(m[3])-1 if newcount else int(m[3]),new_count=newcount,lines=[]);i+=1
            while i<len(lines) and not lines[i].startswith(('@@ ','diff --git ','--- ')):
                line=lines[i]
                if line.startswith('\\ No newline at end of file'):
                    op,value=h['lines'][-1];assert value.endswith('\n');h['lines'][-1]=(op,value[:-1]);i+=1;continue
                assert line and line[0] in ' +-';h['lines'].append((line[0],line[1:]));i+=1
            assert sum(op in ' -' for op,_ in h['lines'])==oldcount
            assert sum(op in ' +' for op,_ in h['lines'])==newcount
            result[p].append(h)
    return result


def apply(text,hunks,reverse=False):
    lines=text.splitlines(keepends=True);out=[];cursor=0
    for h in hunks:
        start=h['new_start' if reverse else 'old_start'];assert cursor<=start
        out+=lines[cursor:start];cursor=start
        for op,value in h['lines']:
            if op in (' +' if reverse else ' -'):
                assert cursor<len(lines) and lines[cursor]==value,('Patch context mismatch',cursor+1)
                cursor+=1
            if op in (' -' if reverse else ' +'):out.append(value)
    out+=lines[cursor:];return ''.join(out)


read(Path(__file__))
# Seal predecessors without rewriting them; source dispositions remain their original authorities.
for base in [PREP,RUN/'sampling/coordinator',RUN/'inventory']:
    for p in sorted(base.rglob('*')):
        if p.is_file():read(p)
coverage=load(RUN/'coverage-progress.json');read(RUN/'coverage.json')
popdoc=load(PREP/'population.json');population=popdoc['units'];pop={r['id']:r for r in population}
units={r['id']:r for r in load(RUN/'inventory/semantic-units.json')};files={r['path']:r for r in load(RUN/'inventory/files.json')}
for p in popdoc['owner_ledgers'].values():read(Path(p))
assert len(pop)==14044
broad={k for k,r in pop.items() if r['eligibility']=='candidate-eligible'};assert len(broad)==9352
selection=load(PREP/'selection.json');original_ids={r['id'] for r in selection};assert len(selection)==len(original_ids)==983
targets=load(REV/'role-quota-status.json');assert sum(q['conservative_target'] for q in targets)==1067
reads={r['id']:dict(r) for r in load(REV/'completed-read-tally.json')};assert len(reads)==1122
last_path=RUN/'sampling/coordinator/last-seven-verdicts.md';last_bytes=read(last_path)
last=[dict(suffix=m[1],verdict_text=m[2]) for line in last_bytes.decode().splitlines() if (m:=re.fullmatch(r'\| `([0-9a-f]{16})` \| (.*) \|',line))]
proposals=load(REV/'next-required-selection.json');assert len(last)==7 and {'core-001:unit:'+r['suffix'] for r in last}=={r['id'] for r in proposals}
for row in last:
    key='core-001:unit:'+row['suffix'];assert key not in reads;u=units[key];p=pop[key]
    reads[key]=dict(id=key,owner=p['owner'],stratum=p['stratum'],path=u['path'],first_line=u['first_line'],last_line=u['last_line'],
        source_sha256=u['source_sha256'],range_sha256=u['range_sha256'],actually_read_by='Astra',raw_category='supported-pending-final-eligibility',
        raw_verdict_payload=row,authority=str(last_path),authority_sha256=sha(last_bytes),preparer_content_verdict=None)
assert len(reads)==1129 and original_ids<=reads.keys() and len(reads.keys()-original_ids)==146
for key,r in reads.items():
    b=read(Path(r['authority']));assert sha(b)==r['authority_sha256'];payload=r['raw_verdict_payload'];text=b.decode();suffix=key.split(':')[-1]
    if r['authority'].endswith('.json'):
        match=[v for v in json.loads(text)['units'] if v['id']==key];assert len(match)==1 and match[0]==payload
    elif 'coordinator_list' in payload:assert payload['coordinator_list'] in text and '`'+suffix+'`' in payload['coordinator_list']
    elif 'scope' in payload:assert payload['verdict_text'] in text and '`'+suffix+'`' in payload['verdict_text']
    else:assert '| `'+suffix+'` | '+payload['verdict_text']+' |' in text or '- `'+suffix+'`: '+payload['verdict_text'] in text

manifest_bytes=read(RUN/'editing/integration/current-products.json');manifest=json.loads(manifest_bytes)
assert manifest['source_baseline']==PIN and len(manifest['product_files'])==361
for r in manifest['product_files']:assert sha(primary(r['path']))==r['sha256']
old_edits=load(OLD/'known-edit-overlap-inputs.json');assert sha(manifest_bytes)==old_edits['manifest_sha256']
accepted_diff=read(OLD/'known-accepted-selected-role-scope.diff');assert sha(accepted_diff)==old_edits['diff_sha256']
accepted_hunks=parse_patch(accepted_diff)


def original(path):
    if path not in originals:
        b=primary(path);text=normalized(b)
        if path in accepted_hunks:
            text=apply(text,accepted_hunks[path],reverse=True)
            assert apply(text,accepted_hunks[path])==normalized(b)
        # Recover exact original inventory BOM/newline representation without invoking Git.
        candidates=[b] if normalized(b)==text else []
        candidates += [(bom+text.replace('\n',newline)).encode() for bom in ['', '\ufeff'] for newline in ['\n','\r\n']]
        exact=next((x for x in candidates if sha(x)==files[path]['sha256']),None)
        if exact is None:
            originals[path]=dict(state='UNKNOWN-ORIGINAL-REPRESENTATION; DENOMINATOR RETAINED',text=text,raw_text=None,sha256=None)
        else:originals[path]=dict(state='EXACT-INVENTORY-REPRESENTATION',text=text,raw_text=exact.decode('utf-8-sig'),sha256=sha(exact))
    return originals[path]


# Only completed independently accepted queued packets are admitted from the central table.
queue_bytes=read(RUN/'editing/integration/queued-sampling-corrections.md')
queue_rows=[m.groups() for line in queue_bytes.decode().splitlines() if (m:=re.fullmatch(r'\| (.*?) \| `(.*?)` — `(.*?)` \| `([0-9a-f]{64})` \|',line))]
assert len(queue_rows)==5
patch_records=[];queue_changes={}
for finding,worktree,relative,digest in queue_rows:
    patch_path=Path('/home/rupicola/Code/y')/worktree/RUN/relative;packet=patch_path.parent
    b=read(patch_path);assert sha(b)==digest
    review_path=packet/'review-identities.json';review=load(review_path)
    assert review.get('verdict',review.get('outcome',review.get('decision')))=='PASS'
    assert review.get('accepted_patch_sha256',review.get('focused_patch_sha256'))==digest
    review_bindings=review.get('input_sha256',review.get('reviewed_record_sha256',review.get('accepted_input_records_relative_to_sampling_udev_receive',{})))
    for p,h in review_bindings.items():assert sha(read(packet/p))==h,('Review-bound record drift',packet,p)
    if 'accepted_paths' in review:identities={r['path']:(r['before_sha256'],r['after_sha256']) for r in review['accepted_paths']}
    elif 'accepted_before_sha256' in review:identities={p:(h,review['accepted_after_sha256'][p]) for p,h in review['accepted_before_sha256'].items()}
    elif 'product' in review:
        r=review['product'];identities={r['path']:(r['before_sha256'],r['accepted_after_sha256'])}
    else:identities={review['path']:(review['before_current_primary_raw_sha256'],review['accepted_after_raw_sha256'])}
    parsed=parse_patch(b);assert set(parsed)==set(identities)
    products=[]
    for path,hunks in parsed.items():
        before=primary(path);before_hash,after_hash=identities[path];assert sha(before)==before_hash and path not in projected
        text=apply(normalized(before),hunks)
        candidates=[(bom+text.replace('\n',newline)).encode() for bom in ['', '\ufeff'] for newline in ['\n','\r\n']]
        after=next((x for x in candidates if sha(x)==after_hash),None)
        if after is None:
            after=read(Path('/home/rupicola/Code/y')/worktree/path)
            assert normalized(after)==text and sha(after)==after_hash
        assert apply(text,hunks,reverse=True)==normalized(before)
        projected[path]=after
        products.append(dict(path=path,before_sha256=before_hash,projected_AFTER_sha256=after_hash,actual_transfer='PENDING'))
        queue_changes[path]=dict(finding=finding,patch_sha256=digest,review=str(review_path))
    patch_records.append(dict(finding=finding,patch=str(patch_path),patch_sha256=digest,review=str(review_path),review_sha256=inputs[str(review_path)],
        content_acceptance='PASS in named independent review; no sampler credit from acceptance',products=products))
assert len(projected)==9


def offsets(text):
    result=[0]
    for line in text.splitlines(keepends=True):result.append(result[-1]+len(line))
    return result


def spans(base,after,raw):
    o=offsets(raw if raw is not None else base);result=[]
    for tag,i,j,a,b in difflib.SequenceMatcher(a=base.splitlines(keepends=True),b=after.splitlines(keepends=True),autojunk=False).get_opcodes():
        if tag!='equal':result.append(dict(start_character=o[i],end_character_exclusive=o[j],old_first_line=i+1,old_line_count=j-i,added_lines=b-a,
            kind='insertion-point' if i==j else 'replacement-or-deletion',coordinate_representation='EXACT-ORIGINAL' if raw is not None else 'NORMALIZED-UNKNOWN-RAW'))
    return result


def hits(u,items):
    a,b=u['start_character'],u['end_character_exclusive']
    return [s for s in items if (a<s['end_character_exclusive'] and s['start_character']<b) if s['start_character']!=s['end_character_exclusive']] + [s for s in items if s['start_character']==s['end_character_exclusive'] and a<=s['start_character']<=b]


def file_mapping(path,state):
    cachekey=(path,state)
    if cachekey not in maps:
        o=original(path);b=projected.get(path,primary(path)) if state=='AFTER-QUEUED' else primary(path)
        text=normalized(b);equal={}
        for tag,i,j,a,z in difflib.SequenceMatcher(a=o['text'].splitlines(keepends=True),b=text.splitlines(keepends=True),autojunk=False).get_opcodes():
            if tag=='equal':equal.update({k:a+k-i for k in range(i,j)})
        maps[cachekey]=dict(text=text,sha256=sha(b),equal=equal,old_offsets=offsets(o['text']),new_offsets=offsets(text),
            spans=spans(o['text'],text,o['raw_text']))
    return maps[cachekey]


def map_unit(u,state):
    o=original(u['path']);m=file_mapping(u['path'],state)
    result=dict(file_sha256=m['sha256'],state='RANGE-CHANGED-OR-METADATA-UNKNOWN',exact_unit_match=False,
        original_span_overlap=hits(u,m['spans']) if o['raw_text'] is not None else [],metadata_unknown=o['raw_text'] is None)
    if o['raw_text'] is None:return result
    body=o['raw_text'][u['start_character']:u['end_character_exclusive']];assert sha(body.encode())==u['range_sha256']
    a=len(o['raw_text'][:u['start_character']].replace('\r\n','\n'));b=a+len(body.replace('\r\n','\n'))
    first=bisect.bisect_right(m['old_offsets'],a)-1;last=bisect.bisect_right(m['old_offsets'],max(a,b-1))-1
    if all(k in m['equal'] for k in range(first,last+1)):
        line_ids=[m['equal'][k] for k in range(first,last+1)]
        if line_ids==list(range(line_ids[0],line_ids[0]+len(line_ids))):
            x=m['new_offsets'][line_ids[0]]+a-m['old_offsets'][first];y=m['new_offsets'][line_ids[-1]]+b-m['old_offsets'][last]
            if m['text'][x:y]==body.replace('\r\n','\n'):
                result.update(state='EXACT-UNCHANGED-UNIT',exact_unit_match=True,start_character_normalized=x,end_character_exclusive_normalized=y,
                    first_line=m['text'].count('\n',0,x)+1,last_line=m['text'].count('\n',0,max(x,y-1))+1,match_rule='CONTIGUOUS-UNCHANGED-LINES')
    if not result['exact_unit_match']:
        needle=body.replace('\r\n','\n');x=m['text'].find(needle)
        if needle and x>=0 and m['text'].find(needle,x+1)<0:
            y=x+len(needle);result.update(state='EXACT-UNCHANGED-UNIT',exact_unit_match=True,start_character_normalized=x,end_character_exclusive_normalized=y,
                first_line=m['text'].count('\n',0,x)+1,last_line=m['text'].count('\n',0,max(x,y-1))+1,match_rule='UNIQUE-EXACT-WHOLE-UNIT-TEXT')
    return result


reconciliation=[];delta=[]
queued_original_ids={k for k,u in units.items() if k in pop and u['path'] in projected}
for key in sorted(broad|reads.keys()|queued_original_ids):
    u=units[key];p=pop[key];r=reads.get(key)
    before=map_unit(u,'CURRENT');after=map_unit(u,'AFTER-QUEUED');reasons=[]
    if key not in broad:reasons.append('outside-original-9352-admitted-population')
    if r and r['raw_category']!='supported-pending-final-eligibility':reasons.append('actual-Astra-'+r['raw_category'])
    if r and r.get('sampling_credit_adjudication'):reasons.append('actual-context-adjudication-retained')
    if before['original_span_overlap']:reasons.append('accepted-current-exact-original-span-overlap')
    if after['original_span_overlap']:reasons.append('combined-AFTER-queued-exact-original-span-overlap')
    if not before['exact_unit_match'] or not after['exact_unit_match']:reasons.append('changed-range-or-exact-metadata-unavailable')
    candidate=bool(r and not reasons)
    item=dict(id=key,owner=p['owner'],stratum=p.get('stratum'),path=u['path'],source_sha256=u['source_sha256'],range_sha256=u['range_sha256'],
        original_first_line=u['first_line'],original_last_line=u['last_line'],original_population_state=p['eligibility'],original_selected=key in original_ids,
        actual_verdict_authority=r['authority'] if r else None,CURRENT=before,AFTER_QUEUED=after,
        candidate_eligible_supported_unchanged=candidate,credit_withholding_reasons=reasons,
        unchanged_population_candidate=key in broad and not reasons,unsampled_population_row_is_not_source_content_approval=True,
        candidate_credit=1 if candidate else 0,final_clean_credit=None,
        retained_in_conservative_quota_denominator=key in broad,whole_file_exclusion=False)
    reconciliation.append(item)
    if key in broad and reasons:
        delta.append(dict(id=key,owner=p['owner'],stratum=p['stratum'],path=u['path'],reasons=reasons,actually_sampled=bool(r),original_selected=key in original_ids,
            candidate_unchanged_eligibility='WITHHELD',conservative_quota_population_delta=0,
            known_changed_or_adjudicated_delta_not_silent_history_rewrite=True))
byrec={r['id']:r for r in reconciliation}
quota_rows=[]
for q in targets:
    owner,role=q['owner'],q['stratum'];role_population={k for k in broad if (pop[k]['owner'],pop[k]['stratum'])==(owner,role)}
    assert len(role_population)==q['broad_original_population']
    minimum=math.ceil(len(role_population)/10);assert minimum==q['original_quota']
    counted=[k for k in sorted(role_population) if byrec[k]['candidate_eligible_supported_unchanged']]
    quota_rows.append(dict(owner=owner,stratum=role,conservative_original_population=len(role_population),conservative_10_percent_minimum=minimum,
        authorized_expansion=q['already_authorized_full_expansion'],required_target=q['conservative_target'],
        candidate_supported_unchanged_after_all_five_patches=len(counted),candidate_counted_ids=counted,
        candidate_shortfall=max(0,q['conservative_target']-len(counted)),candidate_target_met=len(counted)>=q['conservative_target'],
        known_withheld_population_ids=[k for k in sorted(role_population) if byrec[k]['credit_withholding_reasons']],
        unknown_metadata_population_retained=sum(byrec[k]['CURRENT']['metadata_unknown'] or byrec[k]['AFTER_QUEUED']['metadata_unknown'] for k in role_population),
        final_accepted_clean_count=None,post_transfer_hash_refresh='PENDING'))
assert sum(q['conservative_original_population'] for q in quota_rows)==9352
assert sum(q['required_target'] for q in quota_rows)==1067

# Proposal chronology is kept separately from actual readings. Repeated activated excerpts count once.
proposal_files=[PREP/'selection.json']+sorted(p for p in PREP.rglob('*.json') if p.name in ['additional-selection.json','replacement-selection.json','next-required-selection.json',
    'conditional-history-replacements.json','activated-history-replacements.json','new-non-SW-selection.json'] and 'history' not in p.parts)
proposal_history=[]
for p in proposal_files:
    obj=load(p)
    if not isinstance(obj,list):continue
    for index,row in enumerate(obj):
        if not isinstance(row,dict) or 'id' not in row:continue
        proposal_history.append(dict(id=row['id'],proposal_record=str(p),proposal_index=index,original_rank=row.get('original_rank'),
            original_seed='core-001-clean-sampling-preparation-v1',proposed_only_is_not_actual_read=True,
            ordering_scope='Exact within-record index/rank; file listing order is not a new global chronological assertion.'))
for key,r in reads.items():
    r['final_candidate_reconciliation']='population-and-range-reconciliation.json#'+key
    r['candidate_credit']=byrec[key]['candidate_credit'];r['final_clean_credit']=None
    r['original_selection']=key in original_ids
    r['all_proposal_occurrences']=[v for v in proposal_history if v['id']==key]
    assert r['all_proposal_occurrences'],('Actual read has no preserved proposal provenance',key)
aliases=[]
for r in reads.values():
    if not byrec[r['id']]['candidate_credit']:
        aliases.append(dict(id=r['id'],authority=r['authority'],raw_category=r['raw_category'],raw_verdict_payload=r['raw_verdict_payload'],
            preserved_finding_fields={k:v for k,v in r.items() if k in ['related_existing_finding','existing_class','editor_followup','sampling_credit_adjudication']},
            literal_finding_reference_tokens=sorted(set(re.findall(r'(?:SAMPLE-[A-Za-z0-9-]+|PIVIMP-\d+|CORE-[A-Z0-9-]+)',json.dumps(r['raw_verdict_payload'])+' '+json.dumps({k:v for k,v in r.items() if k in ['related_existing_finding','existing_class','editor_followup']})))),
            unique_new_engineering_bug_allocation=0,qualification='Exact Astra qualifications/aliases retained; corrected samples stay zero, acceptance of prose never creates bug or clean-unit credit.'))
boundaries=[];owners=[]
for owner in sorted({q['owner'] for q in quota_rows}):
    qs=[q for q in quota_rows if q['owner']==owner];rr=[r for r in reads.values() if r['owner']==owner]
    owners.append(dict(owner=owner,conservative_population=sum(q['conservative_original_population'] for q in qs),
        original_minimum=sum(q['conservative_10_percent_minimum'] for q in qs),authorized_expansion=sum(q['authorized_expansion'] for q in qs),
        required_target=sum(q['required_target'] for q in qs),actual_unique_reads=len(rr),candidate_counted=sum(q['candidate_supported_unchanged_after_all_five_patches'] for q in qs),
        candidate_shortfall=sum(q['candidate_shortfall'] for q in qs),all_role_targets_met=all(q['candidate_target_met'] for q in qs),final_acceptance='PENDING ASTRA + INDEPENDENT REVIEW + POSTTRANSFER REFRESH'))
for owner,record in coverage['completed_boundaries'].items():
    aggregate=next(o for o in owners if o['owner']==owner)
    boundaries.append(dict(owner=owner,original_coverage_record=record,source_disposition_ledger_sha256=inputs[record['ledger']],
        sampling_candidate=aggregate,source_boundary_completion=record.get('source_boundary_closure','Preserve original coverage record; no new completion assertion'),
        no_new_boundary_or_whole_module_content_verdict=True,runtime_native_physical_qualifications='UNCHANGED; no execution or conformance credit'))

shortfall=sum(q['candidate_shortfall'] for q in quota_rows)
next_proposals=[]
if shortfall:
    # Bounded deterministic fallback only, no whole-category expansion or new content judgment.
    pool_holds={r['id'] for p in PREP.rglob('replacement-pool-exclusions.json') if 'history' not in p.parts for r in load(p)}
    retired=set(load(OLD/'superseded-proposals.json')['retired_SW_ids'])
    for q in quota_rows:
        if not q['candidate_shortfall']:continue
        ranked=sorted([k for k in broad if (pop[k]['owner'],pop[k]['stratum'])==(q['owner'],q['stratum'])],
            key=lambda k:sha(('core-001-clean-sampling-preparation-v1|'+q['owner']+'|'+q['stratum']+'|'+k).encode()))
        available=[k for k in ranked if k not in reads and k not in pool_holds|retired and not byrec[k]['credit_withholding_reasons']]
        for k in available[:q['candidate_shortfall']]:next_proposals.append(dict(id=k,owner=q['owner'],stratum=q['stratum'],original_rank=ranked.index(k)+1,path=units[k]['path'],
            first_line=units[k]['first_line'],last_line=units[k]['last_line'],actual_read=False,credit=0))

pf02_paths={'docs/architecture/sdk-architecture-map.yml','docs/architecture/sdk-architecture-diagrams.md'}
pf02_rows=[r for r in reconciliation if r['path'] in pf02_paths]
new_pf=[r for r in delta if r['path'] in pf02_paths]
candidate_count=sum(r['candidate_credit'] for r in reconciliation)
summary=dict(state='FINAL SAMPLING RECONCILIATION CANDIDATE; NOT ASTRA/INDEPENDENT ACCEPTANCE',source_pin=PIN,
    assigned_original_inventory=14044,historical_admitted_population=9352,historical_original_selection=983,
    actual_unique_reads=1129,actual_additional_reads=146,conservative_quota_population=9352,
    original_role_minimum_sum=983,authorized_expansion_sum=84,required_target_sum=1067,
    all_five_actual_patches_considered=True,queued_packets=5,queued_paths=9,current_accepted_manifest_paths=361,
    candidate_supported_unchanged_after_all_five_patches=candidate_count,candidate_shortfall=shortfall,
    all_candidate_role_targets_met=all(q['candidate_target_met'] for q in quota_rows),
    actual_verdict_withheld_units=sum(not byrec[k]['candidate_credit'] for k in reads),known_population_eligibility_delta_units=len(delta),
    actual_CURRENT_mapping_states=dict(collections.Counter(byrec[k]['CURRENT']['state'] for k in reads)),
    actual_projected_AFTER_mapping_states=dict(collections.Counter(byrec[k]['AFTER_QUEUED']['state'] for k in reads)),
    unknown_original_metadata_files=[p for p,o in originals.items() if o['raw_text'] is None],
    PF02_changed_admitted_population_ids=[r['id'] for r in new_pf],
    PF02_changed_already_excluded_map_ids=[r['id'] for r in pf02_rows if r['path'].endswith('.yml') and r['original_population_state']!='candidate-eligible' and r['AFTER_QUEUED']['original_span_overlap']],
    final_accepted_denominator=None,final_accepted_clean_count=None,sampler_PASS=None,
    post_transfer_hash_refresh='PENDING ONCE AFTER ALL FIVE TRANSFERS; projected bytes are not primary bytes',
    review='PENDING ASTRA AND INDEPENDENT REVIEW',preparer_content_verdicts=0,
    inventory_clean_claim='NONE: original14,044 include candidate/context/history/exclusions; only original9,352 form the conservative sampling population',
    execution='Python records/patch/range/hash comparisons only; no Git, SDK/compiler/tests, native/device, formatter, cryptographic SDK operation or subagents')

# Recheck every bound predecessor/product after calculation and before emitting the candidate.
for p,digest in inputs.items():assert sha(Path(p).read_bytes())==digest,('Bound input drift',p)
for p,b in current.items():assert Path(p).read_bytes()==b,('Reserved product drift',p)
emit('actual-read-verdicts.json',list(reads.values()));emit('population-and-range-reconciliation.json',reconciliation)
emit('eligibility-deltas.json',delta);emit('owner-role-quotas.json',quota_rows);emit('owner-totals.json',owners)
emit('boundary-completion.json',boundaries);emit('aliases-and-zero-credit.json',aliases);emit('proposal-replacement-order.json',proposal_history)
emit('queued-patch-reconciliation.json',patch_records);emit('summary.json',summary);emit('next-bounded-proposals.json',next_proposals)
emit('PF02-population-overlay.json',dict(scope='Both architecture prose/map paths, all assigned original units; exact patch-overlap only, not whole-file exclusion',
    original_units=pf02_rows,added_evidence_symbols=load(Path(patch_records[-1]['review']))['added_symbols'],
    added_fields='Five inserted symbol references are metadata inside existing original map units; no new original-population or actual-read credit.',
    conservative_population_delta=0,qualification='Changed admitted original units are explicitly withheld. Already excluded entries stay excluded. Unchanged admitted same-file units remain candidates.'))
emit('replacement-history-policy.json',dict(original_983_selection=str(PREP/'selection.json'),
    retired_provisional_SW_ids=load(OLD/'superseded-proposals.json')['retired_SW_ids'],
    canonical_family_successor=str(PREP/'family-replacements/revision-2/replacement-selection.json'),
    conditional_B2=str(PREP/'expansion-B2/conditional-history-replacements.json'),
    repeated_B3_activation=str(PREP/'expansion-B3/activated-history-replacements.json'),
    repeated_activation_read_increment=0,proposal_presence_never_grants_read_or_content_credit=True,
    order_rule='Within each immutable proposal record retain exact array index and original deterministic rank; conditional-to-activation and predecessor-to-successor relations remain explicit. Actual verdict union controls credit, never repeated proposal occurrences.'))
emit('historical-records-and-input-seal.json',inputs)
emit('current-products-at-capture.json',manifest)
projected_products={r['path']:r['sha256'] for r in manifest['product_files']}
projected_products.update({p:sha(b) for p,b in projected.items()})
emit('projected-AFTER-products.json',dict(state='IN-MEMORY PROJECTION ONLY; TRANSFER/HASH REFRESH PENDING',product_files=[dict(path=p,sha256=h) for p,h in sorted(projected_products.items())]))
emit('range-source-identities.json',[dict(path=p,original_inventory_sha256=files[p]['sha256'],reconstructed_original_sha256=o['sha256'],state=o['state'],
    current_sha256=sha(current[p]),projected_AFTER_sha256=sha(projected.get(p,current[p])),original_reconstruction='Reverse exact sealed accepted diff or exact unchanged bytes; no Git invocation') for p,o in sorted(originals.items())])
report=['# Final sampling reconciliation candidate','','**For Astra and independent review. Not a sampler PASS or post-transfer acceptance.**','',
    f"Actual Astra readings: **1,129 distinct units = 983 original + 146 additions/replacements**. The last seven IDs join exactly to their prepared proposals and explicit verdict table.",
    f"All **five actual reviewed queued patches / nine paths** were applied in memory against their accepted before hashes and checked against accepted after hashes. Candidate supported/unchanged units after all patches: **{candidate_count}**. Existing target sum: **1,067**; per-role shortfall: **{shortfall}**.",
    'No clean count was derived from source/file presence, reviewer patch acceptance, or whole-file eligibility. Actual Astra source-content verdicts plus exact unchanged unit/range matching control candidate credit. Corrected/edited original units receive zero candidate credit.',
    '## Population and minimums',
    'The original **14,044** assigned units, **9,352** admitted population and **983** selections remain immutable history. `eligibility-deltas.json` explicitly identifies known changed/adjudicated units. Unknown metadata cannot silently shrink a denominator. For conservative quota calculations **9,352** is retained, original role minima total **983**, and previously authorized expansions total **84**. No new expansion is added.',
    'PF-02 changes an unsampled admitted diagram-prose unit; the two affected map entries were already excluded by their original dispositions. The admitted L4 map entry remains unchanged. `PF02-population-overlay.json` records both files at original unit/value scope. Five added symbols do not become new original units or sample reads; no whole-file exclusion is applied.',
    '## Owner candidate totals',
    '| Owner | Actual reads | Required | Candidate counted | Shortfall |','| --- | ---: | ---: | ---: | ---: |']
for o in owners:report.append(f"| {o['owner']} | {o['actual_unique_reads']} | {o['required_target']} | {o['candidate_counted']} | {o['candidate_shortfall']} |")
report+=['','## Review records',
    '- `owner-role-quotas.json`: exact conservative role population/minimums, expansion targets and counted IDs.',
    '- `population-and-range-reconciliation.json`: CURRENT and projected AFTER unit maps, exact hashes, overlap reasons, actual-verdict links and candidate-only credit.',
    '- `queued-patch-reconciliation.json`: all five patch/review identities and before/AFTER product hashes.',
    '- `actual-read-verdicts.json`: all raw actual verdict payloads and preserved proposal provenance.',
    '- `aliases-and-zero-credit.json`: raw findings/qualifications and aliases; no duplicate bug allocation.',
    '- `proposal-replacement-order.json`: proposal record/index/rank provenance; activation repeats and obsolete proposals do not add actual-read credit.',
    '- `boundary-completion.json`: original qualified boundary returns plus per-role sampling candidate state; no blanket module/platform/runtime acceptance.',
    '- `historical-records-and-input-seal.json`: exact predecessor/source/patch/current input hashes, unchanged through capture.',
    '- `next-bounded-proposals.json`: only actual shortfall slots, if any; empty when targets are met.',
    '## Explicit remaining gates',
    '1. Astra and independent review must accept the candidate exclusions, aliases, mapping and all owner/role quotas.',
    '2. Coordinator transfers the five exact accepted patches only after releasing primary reservation; preserve the 361-path predecessor product map.',
    '3. **Refresh hashes and all affected current unit maps once after all five transfers**, verify against `projected-AFTER-products.json`, and record explicit final acceptance. The projection is not a transferred primary state.',
    '4. Existing preservation/compiler/XML/consumer/capture-guard/native/physical qualification and publication gates stay with their owners. No recorded compile result is promoted into final capture success here.',
    'Final accepted denominator, accepted clean count and sampler PASS remain null. No further content reads or judgments by this worker; no product writes, Git, SDK, execution campaign or subagents. Complete Core Pass 1 publication/deferred handoff, then stop.']
(OUT/'candidate.md').write_text('\n'.join(report)+'\n')
emit('verification.json',dict(result='RECORDS-ONLY CANDIDATE GENERATED; INDEPENDENT STRUCTURAL CHECK PENDING',input_sha256=inputs,
    outputs=[dict(path=str(p),sha256=sha(p.read_bytes()),bytes=p.stat().st_size) for p in sorted(OUT.iterdir()) if p.is_file() and p.name not in ['verification.json','preparation.stdout.json','structural-check.json']],
    post_transfer_refresh='PENDING',Astra_and_independent_review='PENDING',sampler_PASS=None))
print(json.dumps(summary,indent=2))
