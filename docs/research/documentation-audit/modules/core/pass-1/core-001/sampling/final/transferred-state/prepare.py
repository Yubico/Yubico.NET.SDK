#!/usr/bin/env python3
"""One stable final-transfer capture, exact range reconciliation, no Git/SDK/product writes."""
import ast
import bisect
import collections
import difflib
import hashlib
import json
from pathlib import Path
import re

RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PREP=RUN/'sampling/preparation';FINAL=RUN/'sampling/final';LOGICAL=FINAL/'compiler-WA-read-update';OUT=FINAL/'transferred-state'
RETRACTED='core-001:unit:4886f819fa316d28'
assert not (OUT/'identity-seal.json').exists(),'Sealed capture is immutable; use an additive successor.'
inputs={};products={};originals={};map_cache={}


def sha(b):return hashlib.sha256(b).hexdigest()


def read(p):
    b=Path(p).read_bytes();inputs[str(p)]=sha(b);return b


def load(p):return json.loads(read(p))


def emit(name,data):(OUT/name).write_text(json.dumps(data,indent=2)+'\n')


def current(p):
    if p not in products:products[p]=read(p)
    return products[p]


read(Path(__file__))
predecessors={}
for base in [FINAL,FINAL/'compiler-WA-addendum',LOGICAL]:
    for p in sorted(base.iterdir()):
        if p.is_file():predecessors[str(p)]=sha(read(p))
for r in load(LOGICAL/'structural-check.json')['sealed_sampling_records_only']:assert sha(read(r['path']))==r['sha256']
reads=load(LOGICAL/'actual-read-join.json');byread={r['id']:r for r in reads};assert len(byread)==len(reads)==1134
for r in reads:
    b=read(r['authority']);assert sha(b)==r['authority_sha256'];payload=r['raw_verdict_payload'];text=b.decode();suffix=r['id'].split(':')[-1]
    if r['authority'].endswith('.json'):assert next(v for v in json.loads(text)['units'] if v['id']==r['id'])==payload
    elif 'coordinator_list' in payload:assert payload['coordinator_list'] in text and '`'+suffix+'`' in payload['coordinator_list']
    elif 'scope' in payload:assert payload['verdict_text'] in text and '`'+suffix+'`' in payload['verdict_text']
    else:assert '| `'+suffix+'` | '+payload['verdict_text']+' |' in text or '- `'+suffix+'`: '+payload['verdict_text'] in text
retraction=load(FINAL/'compiler-WA-addendum/retraction-overlay.json');assert retraction['candidate_credit']==0
assert sha(read(retraction['authority']))==retraction['authority_sha256']
population=load(PREP/'population.json')['units'];pop={r['id']:r for r in population}
units={r['id']:r for r in load(RUN/'inventory/semantic-units.json')};files={r['path']:r for r in load(RUN/'inventory/files.json')}
broad={k for k,p in pop.items() if p['eligibility']=='candidate-eligible'};assert len(pop)==14044 and len(broad)==9352
selection=load(PREP/'selection.json');selected={r['id'] for r in selection};assert len(selected)==983
oldrec={r['id']:r for r in load(FINAL/'population-and-range-reconciliation.json')}
logical_quotas=load(LOGICAL/'owner-role-quotas.json')
tree=ast.parse(read(FINAL/'prepare.py'));ns={'re':re}
for name in ['parse_patch','apply','normalized']:
    node=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name==name)
    exec(compile(ast.Module(body=[node],type_ignores=[]),str(FINAL/'prepare.py'),'exec'),ns)
parse=ns['parse_patch'];apply=ns['apply'];normalized=ns['normalized']
accepted=parse(read(PREP/'provisional-tally/known-accepted-selected-role-scope.diff'))
old_packets=load(FINAL/'queued-patch-reconciliation.json');queued={};receipt_records=[]
expected={r['path']:r['sha256'] for r in load(FINAL/'current-products-at-capture.json')['product_files']}
for receipt_path in sorted((RUN/'editing/integration').glob('accepted-closeout-*.json')):
    receipt=load(receipt_path);packet=next(p for p in old_packets if p['patch_sha256']==receipt['patch_sha256'])
    patch=read(packet['patch']);assert sha(patch)==receipt['patch_sha256'];parsed=parse(patch)
    for r in receipt['product_files']:
        assert expected.get(r['path'],r['previous_sha256'])==r['previous_sha256'];expected[r['path']]=r['sha256']
    for r in receipt['retained_records']:assert sha(read(r['path']))==r['sha256']
    queued.update(parsed);receipt_records.append(dict(path=str(receipt_path),sha256=inputs[str(receipt_path)],receipt=receipt))
assert len(receipt_records)==5
prior363={r['path']:r['sha256'] for r in load(FINAL/'projected-AFTER-products.json')['product_files']};assert expected==prior363

format_dir=RUN/'editing/format-final'
eof_receipt=load(RUN/'editing/integration/accepted-format-EOF.json')
eof_ids=load(format_dir/'eof-identities.json');snapshots=load(format_dir/'source-snapshots.json')
eof_review=load(format_dir/'review-identities.json');packaging=load(format_dir/'archive-packaging-correction.json')
assert eof_review['decision'].startswith('PASS') and eof_review['patch_sha256']==eof_receipt['patch_sha256']
assert sha(read(format_dir/'eof-identities.json'))==eof_review['eof_identities_sha256']
assert sha(read(format_dir/'source-snapshots.json'))==packaging['text_bundle']['sha256']
eof_patch=read(format_dir/'eof-only.patch');assert sha(eof_patch)==eof_receipt['patch_sha256'];eof_hunks=parse(eof_patch)
eof={};eof_proofs=[]
assert len(snapshots['files'])==len(eof_ids['files'])==len(eof_receipt['product_files'])==239
for r in eof_receipt['product_files']:
    p=r['path'];s=next(x for x in snapshots['files'] if x['path']==p);ident=next(x for x in eof_ids['files'] if x['path']==p)
    before=s['before']['text'].encode();after=s['after']['text'].encode()
    assert before.endswith(b'\n') and not before.endswith(b'\r\n') and after==before[:-1]
    assert sha(before)==r['previous_sha256']==ident['before_sha256']==expected[p]
    assert sha(after)==r['sha256']==ident['after_sha256'];assert current(p)==after
    assert apply(normalized(before),eof_hunks[p])==normalized(after)
    assert apply(normalized(after),eof_hunks[p],True)==normalized(before)
    eof[p]=dict(before=before,after=after);expected[p]=r['sha256']
    eof_proofs.append(dict(path=p,before_sha256=sha(before),after_sha256=sha(after),before_bytes=len(before),after_bytes=len(after),
        removed_hex='0a',removed_byte_interval=[len(after),len(before)],all_preceding_bytes_exact=True,semantic_edit_exclusion=False,whole_file_exclusion=False))
assert set(eof_hunks)==set(eof)
receipt_records.append(dict(path=str(RUN/'editing/integration/accepted-format-EOF.json'),sha256=inputs[str(RUN/'editing/integration/accepted-format-EOF.json')],receipt=eof_receipt))
latest={};latest_spans={};latest_reviews={}
for name in ['accepted-WA-logging.json','accepted-root-logging.json']:
    rp=RUN/'editing/integration'/name;receipt=load(rp)
    for r in receipt['retained_records']:assert sha(read(r['path']))==r['sha256']
    patch_entry=next(r for r in receipt['retained_records'] if r['sha256']==receipt['patch_sha256'])
    review_entry=next(r for r in receipt['retained_records'] if r['path'].endswith('review-identities.json'))
    review=load(review_entry['path']);assert review.get('outcome',review.get('verdict'))=='PASS'
    latest_reviews[name]=review
    parsed=parse(read(patch_entry['path']));assert set(parsed)=={r['path'] for r in receipt['product_files']}
    for r in receipt['product_files']:
        p=r['path'];after=current(p);assert sha(after)==r['sha256'] and expected[p]==r['previous_sha256']
        before_text=apply(normalized(after),parsed[p],True)
        variants=[(bom+before_text.replace('\n',newline)).encode() for bom in ['', '\ufeff'] for newline in ['\n','\r\n']]
        before=next(v for v in variants if sha(v)==r['previous_sha256'])
        assert apply(normalized(before),parsed[p])==normalized(after)
        latest[p]=dict(before=before,after=after,hunks=parsed[p],receipt=str(rp),patch_sha256=receipt['patch_sha256']);expected[p]=r['sha256']
    receipt_records.append(dict(path=str(rp),sha256=inputs[str(rp)],receipt=receipt))
manifest_bytes=read(RUN/'editing/integration/current-products.json');manifest=json.loads(manifest_bytes)
actual_map={r['path']:r['sha256'] for r in manifest['product_files']};assert len(actual_map)==363 and actual_map==expected
for p,h in actual_map.items():assert sha(current(p))==h


def original(path):
    if path not in originals:
        b=current(path);text=normalized(eof[path]['before'] if path in eof else b)
        if path in latest:text=apply(text,latest[path]['hunks'],True)
        if path in queued:text=apply(text,queued[path],True)
        if path in accepted:text=apply(text,accepted[path],True)
        variants=([b] if normalized(b)==text else [])+[(bom+text.replace('\n',newline)).encode() for bom in ['', '\ufeff'] for newline in ['\n','\r\n']]
        raw=next((v for v in variants if sha(v)==files[path]['sha256']),None);assert raw is not None,('Exact inventory reconstruction unavailable',path)
        originals[path]=dict(raw=raw.decode('utf-8-sig'),normalized=text,sha256=sha(raw))
    return originals[path]


def offsets(text):
    result=[0]
    for line in text.splitlines(keepends=True):result.append(result[-1]+len(line))
    return result


def mapping_file(path):
    if path not in map_cache:
        o=original(path);literal=current(path).decode('utf-8-sig');text=literal.replace('\r\n','\n');virtual=text
        # A virtual terminator is used only for the239 certified raw-LF removals and only to match
        # the original terminal-line representation. Actual interval hashes always use real bytes.
        virtual_LF=path in eof and o['normalized'].endswith('\n') and not text.endswith('\n')
        if virtual_LF:virtual+='\n'
        old_lines=o['normalized'].splitlines(keepends=True);new_lines=virtual.splitlines(keepends=True)
        opcodes=difflib.SequenceMatcher(a=old_lines,b=new_lines,autojunk=False).get_opcodes();equal={};spans=[];raw_offsets=offsets(o['raw'])
        for tag,i,j,a,b in opcodes:
            if tag=='equal':equal.update({k:a+k-i for k in range(i,j)})
            else:spans.append(dict(start_character=raw_offsets[i],end_character_exclusive=raw_offsets[j],old_first_line=i+1,old_line_count=j-i,added_lines=b-a))
        norm_to_raw=[0];i=0
        while i<len(literal):i+=2 if literal[i:i+2]=='\r\n' else 1;norm_to_raw.append(i)
        map_cache[path]=dict(text=text,virtual=virtual,virtual_LF=virtual_LF,literal=literal,norm_to_raw=norm_to_raw,
            equal=equal,old_offsets=offsets(o['normalized']),new_offsets=offsets(virtual),semantic_spans=spans)
    return map_cache[path]


def overlaps(u,spans):
    a,b=u['start_character'],u['end_character_exclusive']
    return [s for s in spans if (a<s['end_character_exclusive'] and s['start_character']<b) if s['start_character']!=s['end_character_exclusive']] + [s for s in spans if s['start_character']==s['end_character_exclusive'] and a<=s['start_character']<=b]


def map_unit(key):
    u=units[key];o=original(u['path']);m=mapping_file(u['path']);body=o['raw'][u['start_character']:u['end_character_exclusive']]
    assert sha(body.encode())==u['range_sha256'];needle=body.replace('\r\n','\n')
    a=len(o['raw'][:u['start_character']].replace('\r\n','\n'));b=a+len(needle)
    first=bisect.bisect_right(m['old_offsets'],a)-1;last=bisect.bisect_right(m['old_offsets'],max(a,b-1))-1;location=None
    if all(k in m['equal'] for k in range(first,last+1)):
        lines=[m['equal'][k] for k in range(first,last+1)]
        if lines==list(range(lines[0],lines[0]+len(lines))):
            x=m['new_offsets'][lines[0]]+a-m['old_offsets'][first];y=m['new_offsets'][lines[-1]]+b-m['old_offsets'][last]
            if m['virtual'][x:y]==needle:location=(x,y)
    if location is None:
        x=m['virtual'].find(needle)
        if needle and x>=0 and m['virtual'].find(needle,x+1)<0:location=(x,x+len(needle))
    result=dict(id=key,path=u['path'],original_first_line=u['first_line'],original_last_line=u['last_line'],original_range_sha256=u['range_sha256'],
        current_file_sha256=sha(current(u['path'])),semantic_original_span_overlap=overlaps(u,m['semantic_spans']),
        reviewed_EOF_only_file=u['path'] in eof,state='SUBSTANTIVE-RANGE-CHANGED',semantic_interval_match=False)
    if location:
        x,y=location;z=min(y,len(m['text']));missing=y-z
        assert missing in [0,1]
        if missing:assert m['virtual_LF'] and y==len(m['virtual']) and needle.endswith('\n') and u['end_character_exclusive']==len(o['raw'])
        actual=m['text'][x:z];assert actual==needle[:-1] if missing else actual==needle
        raw_start=m['norm_to_raw'][x];raw_end=m['norm_to_raw'][z];literal_unit=m['literal'][raw_start:raw_end]
        result.update(state='SEMANTIC-INTERVAL-MATCH; LITERAL-TERMINAL-LF-DIFFERENCE' if missing else 'EXACT-NORMALIZED-INTERVAL-MATCH',semantic_interval_match=True,
            current_normalized_character_interval=[x,z],current_literal_character_interval=[raw_start,raw_end],
            current_first_line=m['text'].count('\n',0,x)+1,current_last_line=m['text'].count('\n',0,max(x,z-1))+1,
            original_normalized_range_sha256=sha(needle.encode()),current_normalized_range_sha256=sha(actual.encode()),current_literal_range_sha256=sha(literal_unit.encode()),
            literal_original_to_current_terminal_LF_difference='REMOVED 0a' if missing else None,
            EOF_only_change_is_not_semantic_exclusion=True,whole_file_exclusion=False)
    return result


def project_latest_spans(path):
    o=original(path);base=o['normalized'].splitlines(keepends=True)
    before=normalized(latest[path]['before']).splitlines(keepends=True);after=normalized(latest[path]['after']).splitlines(keepends=True)
    opcodes=difflib.SequenceMatcher(a=base,b=before,autojunk=False).get_opcodes();equal={};raw_offsets=offsets(o['raw']);result=[]
    for tag,i,j,a,b in opcodes:
        if tag=='equal':equal.update({k:i+k-a for k in range(a,b)})
    for tag,i,j,a,b in difflib.SequenceMatcher(a=before,b=after,autojunk=False).get_opcodes():
        if tag=='equal':continue
        mapped=[];basis='EXACT-PINNED-LINE-PROJECTION'
        if i<j and all(k in equal for k in range(i,j)):mapped=[equal[k] for k in range(i,j)];left=min(mapped);right=max(mapped)+1
        elif i==j and i in equal:left=right=equal[i]
        else:
            blocks=[(x,y) for _,x,y,s,t in opcodes if (i<t and s<j) or (i==j and s<=i<=t)]
            assert blocks,('Latest delta projection unavailable',path,i,j)
            left=min(x for x,_ in blocks);right=max(y for _,y in blocks);basis='CONSERVATIVE-WITHIN-PRIOR-ACCEPTED-BLOCK'
        result.append(dict(start_character=raw_offsets[left],end_character_exclusive=raw_offsets[right],projection=basis,
            before_first_line=i+1,before_line_count=j-i,after_first_line=a+1,after_line_count=b-a))
    return result


for path in latest:latest_spans[path]=project_latest_spans(path)


def fence_bodies(text):
    bodies=[];fence=None;body=[]
    for line in text.splitlines(keepends=True):
        match=re.match(r'^\s*(`{3,}|~{3,})',line)
        if match and fence is None:fence=match[1];body=[]
        elif match and match[1][0]==fence[0] and len(match[1])>=len(fence):bodies.append(''.join(body));fence=None
        elif fence is not None:body.append(line)
    assert fence is None;return bodies


wa_before=fence_bodies(normalized(latest['src/WebAuthn/CLAUDE.md']['before']))
wa_after=fence_bodies(normalized(latest['src/WebAuthn/CLAUDE.md']['after']))
assert len(wa_before)==len(wa_after)
changed_fences=[dict(fence=i+1,before_body_sha256=sha(a.encode()),after_body_sha256=sha(b.encode())) for i,(a,b) in enumerate(zip(wa_before,wa_after)) if a!=b]
assert len(changed_fences)==1
accepted_fence=latest_reviews['accepted-WA-logging.json']['changed_example']
assert changed_fences[0]['fence']==accepted_fence['fence'] and changed_fences[0]['before_body_sha256']==accepted_fence['before_body_sha256']
assert changed_fences[0]['after_body_sha256']==accepted_fence['accepted_after_body_sha256']


reconciliation=[];actual_rows=[]
required_original_paths={units[k]['path'] for k in broad|byread.keys()}|set(latest)
included=broad|byread.keys()|{k for k,u in units.items() if k in pop and (u['path'] in latest or (u['path'] in eof and u['path'] in required_original_paths))}
for key in sorted(included):
    u=units[key];p=pop[key];m=map_unit(key);old=oldrec.get(key);logical=byread.get(key)
    reasons=list(old['credit_withholding_reasons']) if old else []
    if key==RETRACTED:reasons.append('Mandatory SAMPLE-WA-002/FULL-032 compiler retraction remains zero')
    if m['semantic_original_span_overlap']:reasons.append('Final substantive original-unit span overlap (EOF-only deltas excluded)')
    if not m['semantic_interval_match']:reasons.append('Final substantive interval no longer matches original')
    if key not in broad:reasons.append('Outside historical9352 admitted population; preserve original disposition')
    logical_credit=logical['logical_candidate_credit_before_final_range_refresh'] if logical else 0
    final_candidate_credit=int(bool(logical_credit and m['semantic_interval_match'] and not m['semantic_original_span_overlap'] and key!=RETRACTED))
    row=dict(id=key,owner=p['owner'],stratum=p.get('stratum'),path=u['path'],original_population_state=p['eligibility'],original_selected=key in selected,
        actual_verdict_authority=logical['authority'] if logical else None,current_mapping=m,
        latest_logging_patch_original_overlap=overlaps(u,latest_spans.get(u['path'],[])),
        historical_withholding_reasons_retained=reasons,logical_pretransfer_credit=logical_credit,final_candidate_credit=final_candidate_credit,
        conservative_denominator_member=key in broad,conservative_population_delta=0,whole_file_exclusion=False,accepted_clean_credit=None)
    reconciliation.append(row)
    if logical:actual_rows.append(dict(**logical,current_mapping=m,final_candidate_credit=final_candidate_credit,accepted_clean_credit=None))
byrec={r['id']:r for r in reconciliation};assert len(actual_rows)==1134
quotas=[]
for q in logical_quotas:
    counted=[k for k in q['logical_counted_ids'] if byrec[k]['final_candidate_credit']]
    quotas.append(dict(owner=q['owner'],stratum=q['stratum'],conservative_original_population=q['conservative_original_population'],
        original_minimum=q['original_minimum'],authorized_expansion=q['authorized_expansion'],required_target=q['required_target'],
        actual_current_supported_candidate_count=len(counted),counted_actual_ids=counted,shortfall=max(0,q['required_target']-len(counted)),
        current_range_refresh='COMPLETE AT SEALED FINAL TRANSFER STATE',Astra_adjudication='PENDING REVIEW',independent_adjudication='PENDING REVIEW',accepted_clean_count=None))
owners=[]
for owner in sorted({q['owner'] for q in quotas}):
    qs=[q for q in quotas if q['owner']==owner]
    owners.append(dict(owner=owner,actual_unique_reads=sum(r['owner']==owner for r in actual_rows),required_target=sum(q['required_target'] for q in qs),
        actual_current_supported_candidate_count=sum(q['actual_current_supported_candidate_count'] for q in qs),shortfall=sum(q['shortfall'] for q in qs)))
root_overlay=[r for r in reconciliation if r['path']=='CLAUDE.md' and r['latest_logging_patch_original_overlap']]
additional=[r for r in actual_rows if r['id'] not in selected]
changed_additional=[r['id'] for r in additional if r['logical_candidate_credit_before_final_range_refresh'] and not r['final_candidate_credit']]
eof_range_deltas=[r['current_mapping'] for r in reconciliation if r['current_mapping'].get('literal_original_to_current_terminal_LF_difference')]
summary=dict(state='FINAL TRANSFERRED-IDENTITY SAMPLING CANDIDATE; ASTRA/INDEPENDENT ADJUDICATION PENDING REVIEW',
    actual_unique_reads=1134,original_actual_reads=983,additional_actual_reads=len(additional),historical_broad_population=9352,
    original_minimum_sum=sum(q['original_minimum'] for q in quotas),authorized_expansion_sum=sum(q['authorized_expansion'] for q in quotas),
    required_target_sum=sum(q['required_target'] for q in quotas),actual_current_supported_candidate_count=sum(q['actual_current_supported_candidate_count'] for q in quotas),
    quota_shortfall=sum(q['shortfall'] for q in quotas),roles=len(quotas),owners=len(owners),current_manifest_paths=len(actual_map),
    verified_EOF_only_LF_removals=len(eof),literal_original_unit_EOF_range_differences=len(eof_range_deltas),
    new_supported_additional_reads_lost_to_final_substantive_edits=changed_additional,
    original_logging_credit=byrec[RETRACTED]['final_candidate_credit'],identity_and_unit_range_refresh='COMPLETE',
    root_bullet_new_delta_units=[dict(id=r['id'],original_population_state=r['original_population_state'],admitted_to9352=r['conservative_denominator_member'],actually_sampled=bool(r['actual_verdict_authority'])) for r in root_overlay],
    Astra_adjudication='PENDING REVIEW',independent_adjudication='PENDING REVIEW',accepted_clean_count=None,accepted_denominator=None,sampler_PASS=None,
    compiler_or_full_validation_completion='NOT ASSERTED; no SDK/compiler/formatter execution by this worker; existing evidence limits remain unchanged',preparer_content_verdicts=0)
assert len(quotas)==94 and len(owners)==17 and sum(q['conservative_original_population'] for q in quotas)==9352
assert summary['required_target_sum']==1071
for p,h in inputs.items():assert sha(Path(p).read_bytes())==h,('Stable capture drift',p)
for p,h in predecessors.items():assert sha(Path(p).read_bytes())==h
emit('actual-current-read-verdicts.json',actual_rows);emit('population-current-range-overlay.json',reconciliation)
emit('owner-role-quotas.json',quotas);emit('owner-totals.json',owners);emit('summary.json',summary)
emit('EOF-file-byte-proofs.json',eof_proofs);emit('EOF-original-unit-range-deltas.json',eof_range_deltas)
emit('EOF-scope.json',dict(verified_exact_file_byte_deltas=239,original_unit_scope='All9352 admitted units, all1134 actual samples, affected prose units and assigned EOF-neighbor units in those same reconstructed files.',
    EOF_paths_outside_admitted_or_sampled_original_files=[p for p in sorted(eof) if p not in required_original_paths],
    qualification='All239 raw before/after deltas verified. Already-excluded applet remainder is not promoted into the admitted population or claimed as new original-unit/content verification.'))
emit('root-bullet-population-overlay.json',root_overlay);emit('accepted-transfer-chain.json',receipt_records)
emit('latest-reviewed-prose-byte-proofs.json',dict(original_projected_spans=latest_spans,WA_file_fences=len(wa_before),changed_WA_fences=changed_fences,
    compiler_completion_claim=False,qualification='Exact bytes and accepted body hashes only; no new content or compiler verdict.'))
emit('additional-read-preservation.json',dict(actual_additional_units=len(additional),new_supported_units_lost=changed_additional,
    units=[dict(id=r['id'],raw_category=r['raw_category'],logical_pretransfer_credit=r['logical_candidate_credit_before_final_range_refresh'],final_candidate_credit=r['final_candidate_credit'],
        semantic_interval_match=r['current_mapping']['semantic_interval_match'],reviewed_EOF_only_file=r['current_mapping']['reviewed_EOF_only_file']) for r in additional]))
emit('current-products-at-final-capture.json',manifest);emit('predecessor-record-seal.json',predecessors)
emit('adjudication.json',dict(candidate_state='READY FOR ASTRA AND INDEPENDENT SAMPLING REVIEW',identity_refresh='COMPLETE',
    actual_verdict_join='COMPLETE',EOF_byte_and_range_reconciliation='COMPLETE',Astra=dict(state='PENDING REVIEW',review_record=None),
    independent=dict(state='PENDING REVIEW',review_record=None),acceptance_must_be_additive='Create hash-bound review/adjudication successor; do not mutate sealed files.',
    accepted_clean_count=None,accepted_denominator=None,sampler_PASS=None,compiler_completion_claim=False))
emit('input-identities.json',inputs)
report=['# Final transferred-state sampling candidate','','**Actual final bytes/ranges captured. Astra and independent sampling adjudication remain pending review.**','',
    f"**{summary['actual_unique_reads']} actual Astra reads = 983 original + {len(additional)} additional.** Current supported/unchanged candidate **{summary['actual_current_supported_candidate_count']} / target1071**; per-role shortfall **{summary['quota_shortfall']}**, across94 roles/17 owners. Conservative quota population remains9352; no borrowing or denominator shrink.",
    'All five earlier corrections/nine paths, the239 exact one-LF removals, WA one-fence logging correction and root bullet have verified transfer chains to the current363-path map. Actual raw patch applications and accepted before/after hashes reconcile. The WA4886 logging unit stays retracted/zero despite content review PASS.',
    f"The EOF changes remove exactly byte0a and preserve every preceding byte. **{len(eof_range_deltas)} original-unit terminal-range literal differences** are recorded in `EOF-original-unit-range-deltas.json`; normalized semantic interval matching distinguishes them from substantive edits. No new units, reads or whole-file exclusions arise from EOF policy.",
    'The root bullet is accounted at exact original assigned-unit scope in `root-bullet-population-overlay.json`, retaining its original eligibility history and conservative denominator. It is not a new whole-file exclusion or a new clean sample.',
    f"All **{len(additional)} additional actual units** have explicit preservation/credit rows. Newly lost supported additional readings after final transfers: **{len(changed_additional)}**. Raw verdicts and scoped CTAP2.3/retraction qualifications are retained; no new content judgments.",
    '## Records',
    '- `actual-current-read-verdicts.json`: all1134 actual verdicts, effective retraction, current file hashes, original/current ranges and literal range hashes.',
    '- `owner-role-quotas.json` / `owner-totals.json`: exact targets, counted IDs and current shortfalls.',
    '- `population-current-range-overlay.json`: admitted9352 plus affected already-excluded assigned units; no blanket14k-clean claim.',
    '- `EOF-file-byte-proofs.json`:239 exact before/after raw-byte proofs; one-LF **removal**, not the earlier hypothetical append direction.',
    '- `accepted-transfer-chain.json`, `input-identities.json`, `identity-seal.json`: immutable accepted-diff/input/output identities after structural verification.',
    '- `adjudication.json`: pending-review fields; reviewers append a hash-bound adjudication, preserving this candidate.',
    'No product/Git/SDK/native/device/subagent work. No compiler, full formatter, or full validation completion claim; retained formatter baseline/membership and review qualifications are not silently promoted to clean gates. Sampling accepted totals/denominator and sampler PASS remain null until adjudication. Finish Core Pass1 publication and precise deferred handoff, then stop.']
(OUT/'candidate.md').write_text('\n'.join(report)+'\n')
emit('verification.json',dict(state='FINAL CAPTURE PREPARED; STRUCTURAL VERIFY BEFORE IDENTITY SEAL',input_sha256=inputs,
    outputs=[dict(path=str(p),sha256=sha(p.read_bytes()),bytes=p.stat().st_size) for p in sorted(OUT.iterdir()) if p.is_file() and p.name not in ['verification.json','preparation.stdout.json','structural-check.json','identity-seal.json']]))
print(json.dumps(summary,indent=2))
