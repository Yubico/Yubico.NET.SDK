#!/usr/bin/env python3
"""Add 53 actual verdicts, map completed reads, and prepare only remaining quota repairs."""
import ast
import bisect
import collections
import difflib
import hashlib
import json
from pathlib import Path
import re
import subprocess

from pygments.lexers import CSharpLexer
from pygments.token import Comment

RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PREP=RUN/'sampling/preparation';OLD=PREP/'provisional-tally';OUT=OLD/'revision-2'
PIN='fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef';SEED='core-001-clean-sampling-preparation-v1'
inputs,raw_originals,normalized_originals={},{},{}


def sha(b):return hashlib.sha256(b).hexdigest()


def read(p):
    b=p.read_bytes();inputs[str(p)]=sha(b);return b


def load(p):return json.loads(read(p))


def emit(name,value):(OUT/name).write_text(json.dumps(value,indent=2)+'\n')


read(Path(__file__))
previous_original=load(OLD/'original-read-tally.json');previous_extra=load(OLD/'additional-read-tally.json')
previous_summary=load(OLD/'summary.json');previous_check=load(OLD/'structural-check.json')
assert len(previous_original)==983 and len(previous_extra)==86 and previous_check['total_actual_unique_reads']==1069
previous_unread=load(OLD/'unread-required.json');assert len(previous_unread)==53
population=load(PREP/'population.json')['units'];byid={r['id']:r for r in population}
units={u['id']:u for u in load(RUN/'inventory/semantic-units.json')};files={f['path']:f for f in load(RUN/'inventory/files.json')}
strata=load(PREP/'strata.json');targets=load(OLD/'role-quota-status.json')
assert sum(q['conservative_required_target'] for q in targets)==1067
completed={r['id']:dict(r) for r in previous_original+previous_extra};assert len(completed)==1069

# Validate prior parsed payloads against the exact actual authority formats, never file presence.
authority_cache={}
for row in completed.values():
    path=Path(row['authority'])
    if str(path) not in authority_cache:
        b=read(path);authority_cache[str(path)]=b;assert sha(b)==row['authority_sha256']
    payload=row['raw_verdict_payload'];text=authority_cache[str(path)].decode()
    if path.suffix=='.json':
        raw=next(x for x in json.loads(text)['units'] if x['id']==row['id']);assert raw==payload
    elif 'coordinator_list' in payload:
        assert payload['coordinator_list'] in text and '`'+row['id'].split(':')[-1]+'`' in payload['coordinator_list']
    elif payload.get('scope')=='Coordinator exact first-batch material-miss section':
        assert payload['verdict_text'] in text and row['id'].split(':')[-1] in payload['verdict_text']
    else:
        suffix=row['id'].split(':')[-1]
        exact_table='| `'+suffix+'` | '+payload['verdict_text']+' |'
        exact_bullet='- `'+suffix+'`: '+payload['verdict_text']
        assert exact_table in text or exact_bullet in text,('No exact ID/verdict row',path,suffix)
    assert row['source_sha256']==units[row['id']]['source_sha256'] and row['range_sha256']==units[row['id']]['range_sha256']

path=RUN/'sampling/coordinator/remaining-53-verdicts.md';data=read(path).decode()
supported_block=data.split('The following 46 units are supported, subject to final current-span and overlap reconciliation:',1)[1].split('\n\nSpecific interpretation limits:',1)[0]
supported=re.findall(r'`([0-9a-f]{16})`',supported_block);assert len(supported)==len(set(supported))==46
table=[dict(suffix=m[1],verdict_text=m[2]) for line in data.splitlines() if (m:=re.fullmatch(r'\| `([0-9a-f]{16})` \| (.*) \|',line))]
categories={'d27f5051f8d21199':'known-implementation-qualified','0b5f8163f489c670':'documentation-miss','c2de2d6f26065229':'documentation-miss',
    '287e6410db16f0e7':'documentation-miss','606c7462df485c41':'known-implementation-qualified','be11505c581308a5':'known-implementation-qualified','f7fc274baa684518':'specific-ineligible'}
assert len(table)==7 and {r['suffix'] for r in table}==set(categories)
new_verdicts={}
for suffix in supported:
    key='core-001:unit:'+suffix
    new_verdicts[key]=dict(raw_category='supported-pending-final-eligibility',raw_verdict_payload=dict(suffix=suffix,verdict_text='Content supported within sampled source scope; explicit 46-ID list',coordinator_list=supported_block))
for row in table:new_verdicts['core-001:unit:'+row['suffix']]=dict(raw_category=categories[row['suffix']],raw_verdict_payload=row)
assert set(new_verdicts)=={r['id'] for r in previous_unread} and not set(new_verdicts)&completed.keys()
for key,row in new_verdicts.items():
    u=units[key];source=next(r for r in previous_unread if r['id']==key)
    completed[key]=dict(id=key,owner=source['owner'],stratum=source['stratum'],path=u['path'],first_line=u['first_line'],last_line=u['last_line'],
        source_sha256=u['source_sha256'],range_sha256=u['range_sha256'],actually_read_by='Astra',**row,
        authority=str(path),authority_sha256=inputs[str(path)],preparer_content_verdict=None)
assert len(completed)==1122

# Reuse original-byte/diff helpers and the earlier replacement-only scope screen, not its writer.
original_helper=read(PREP/'expansion-B2/expand.py');parser_helper=read(PREP/'prepare.py');prior_helper=read(OLD/'prepare.py')
namespace=dict(subprocess=subprocess,PIN=PIN,files=files,units=units,byid=byid,raw_originals=raw_originals,normalized_originals=normalized_originals,
    sha=sha,Path=Path,read=read,re=re,CSharpLexer=CSharpLexer,Comment=Comment)
for source,name,source_path in [(original_helper,'original',PREP/'expansion-B2/expand.py'),(parser_helper,'parse_patch',PREP/'prepare.py'),
    (prior_helper,'hits',OLD/'prepare.py'),(prior_helper,'pool_reason',OLD/'prepare.py')]:
    node=next(n for n in ast.parse(source).body if isinstance(n,ast.FunctionDef) and n.name==name)
    exec(compile(ast.Module(body=[node],type_ignores=[]),str(source_path),'exec'),namespace)
original=namespace['original'];namespace['original']=original
research_node=next(n for n in ast.parse(prior_helper).body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='current_research_headings' for t in n.targets))
namespace['current_research_headings']=ast.literal_eval(research_node.value)
manifest_bytes=read(RUN/'editing/integration/current-products.json');manifest=json.loads(manifest_bytes);assert manifest['source_baseline']==PIN
accepted={r['path']:r['sha256'] for r in manifest['product_files']}
for p,digest in accepted.items():assert sha(Path(p).read_bytes())==digest,('Accepted current product changed',p)
for name in manifest['receipt_order']:load(RUN/'editing/integration'/name)
(OUT/'accepted-products-at-capture.json').write_bytes(manifest_bytes)
old_edits=load(OLD/'known-edit-overlap-inputs.json')
if old_edits['manifest_sha256']==sha(manifest_bytes):
    accepted_spans=old_edits['accepted_original_changed_spans'];accepted_diff_scope='Exact unchanged accepted-manifest hash: earlier original-coordinate diff reused.'
else:
    paths=sorted({units[r['id']]['path'] for r in population if r['eligibility']=='candidate-eligible'}&accepted.keys())
    command=['git','--no-optional-locks','diff','--no-ext-diff','--no-textconv','--no-renames','--unified=3','--src-prefix=a/','--dst-prefix=b/',PIN,'--',*paths]
    patch=subprocess.check_output(command);patch_path=OUT/'accepted-current-role-scope.diff';patch_path.write_bytes(patch)
    accepted_spans=namespace['parse_patch'](patch_path);accepted_diff_scope=dict(command=command,sha256=sha(patch))
pending_spans=dict(old_edits['pending_original_changed_spans']);pending=[]
# Two independently passed, queued patches are fixed inputs. Unfinished guide work is not inspected.
wa_inputs=load(PREP/'expansion-WebAuthn/accepted-overlap-inputs.json');wa_review=load(Path(wa_inputs['focused_review_record']))
assert wa_review['outcome']=='PASS'
pending.append(dict(scope='SAMPLE-WA-001 fixture oracle comment',state='REVIEWED-PASS; TRANSFER-QUEUED AT HANDOFF',patch_sha256=wa_inputs['focused_patch_sha256'],
    path=wa_review['product']['path'],current_accepted_after_captured=accepted.get(wa_review['product']['path'])==wa_review['product']['accepted_after_sha256']))
pgp_dir=Path('/home/rupicola/Code/y/yubikit-core-docs-edit-pgp-piv')/RUN/'editing/PGP-boundary/sampling-UIF-fallback'
pgp_review=load(pgp_dir/'review-identities.json');assert pgp_review['verdict']=='PASS'
pgp_patch=read(pgp_dir/'focused.patch');assert sha(pgp_patch)==pgp_review['accepted_patch_sha256']
effective_parts=[];pgp_queued_spans={};pgp_eof_evidence=[]
for source_path,after_hash in pgp_review['accepted_after_sha256'].items():
    before=read(pgp_dir/'before'/source_path);after=read(pgp_dir/'after'/source_path)
    assert sha(before)==pgp_review['accepted_before_sha256'][source_path] and sha(after)==after_hash
    original(source_path);base=normalized_originals[source_path];successor=after.decode('utf-8-sig').replace('\r\n','\n')
    pgp_eof_evidence.append(dict(path=source_path,pinned_final_newline=base.endswith('\n'),before_final_newline=before.endswith(b'\n'),after_final_newline=after.endswith(b'\n')))
    # Attribute only the queued delta, not previously accepted PGP corrections, to pending spans.
    # Both reviewed edits replace unchanged pinned lines; fail closed if that premise changes.
    base_lines=base.splitlines(keepends=True);before_lines=before.decode('utf-8-sig').replace('\r\n','\n').splitlines(keepends=True)
    before_to_base={}
    for tag,i,j,a,b in difflib.SequenceMatcher(a=base_lines,b=before_lines,autojunk=False).get_opcodes():
        if tag=='equal':before_to_base.update({k:i+k-a for k in range(a,b)})
    offsets=[0]
    for line in raw_originals[source_path].splitlines(keepends=True):offsets.append(offsets[-1]+len(line))
    queued=[]
    for tag,i,j,a,b in difflib.SequenceMatcher(a=before_lines,b=successor.splitlines(keepends=True),autojunk=False).get_opcodes():
        if tag=='equal':continue
        assert i<j and all(k in before_to_base for k in range(i,j)),('Queued PGP needs explicit changed-line projection',source_path,i,j)
        mapped=[before_to_base[k] for k in range(i,j)];assert mapped==list(range(mapped[0],mapped[0]+len(mapped)))
        queued.append(dict(start_character=offsets[mapped[0]],end_character_exclusive=offsets[mapped[-1]+1],
            old_first_line=mapped[0]+1,old_line_count=j-i,added_lines=b-a,kind='replacement-or-deletion',
            projection='Exact reviewed before-to-after delta on unchanged contiguous pinned lines'))
    pgp_queued_spans[source_path]=queued
    diff_lines=difflib.unified_diff(base.splitlines(keepends=True),successor.splitlines(keepends=True),fromfile='a/'+source_path,tofile='b/'+source_path)
    # difflib preserves unterminated data lines but does not emit the unified-diff EOF marker.
    # Frame those lines explicitly; never add a newline to either source snapshot.
    framed=[line if line.endswith('\n') else line+'\n\\ No newline at end of file\n' for line in diff_lines]
    effective_parts.append(f'diff --git a/{source_path} b/{source_path}\n'+''.join(framed))
pgp_effective=''.join(effective_parts).encode();pgp_effective_path=OUT/'reviewed-queued-PGP-original-scope.diff';pgp_effective_path.write_bytes(pgp_effective)
pgp_combined_spans=namespace['parse_patch'](pgp_effective_path)
for p,rows in pgp_queued_spans.items():
    if accepted.get(p)!=pgp_review['accepted_after_sha256'][p]:pending_spans[p]=rows
pending.append(dict(scope='SAMPLE-PGP-001 UIF fallback implementation/guide comments',state='REVIEWED-PASS; TRANSFER-QUEUED AT HANDOFF',
    patch_sha256=sha(pgp_patch),paths=list(pgp_review['accepted_after_sha256']),review_record=str(pgp_dir/'review-identities.json')))
guide_pending={'core-001:unit:0b5f8163f489c670':'SAMPLE-A003 propagation guide',
    'core-001:unit:c2de2d6f26065229':'SAMPLE-A003 Core README propagation',
    'core-001:unit:287e6410db16f0e7':'SAMPLE-TESTING-001 runner table'}
guide_pending_paths={units[k]['path'] for k in guide_pending}
pending += [dict(scope='SAMPLE-A003 two-guide propagation',state='CORRECTION ASSIGNED; NO COMPLETED REVIEWED PATCH ADMITTED',confirmed_units=list(guide_pending)[:2],neighbors='Final exact patch may affect adjacent ranges; canonical reconciliation required.'),
            dict(scope='SAMPLE-TESTING-001 manual runner table',state='CORRECTION ASSIGNED; NO COMPLETED REVIEWED PATCH ADMITTED',confirmed_units=[list(guide_pending)[2]])]
namespace.update(accepted_spans=accepted_spans,pending_spans=pending_spans)
hits=namespace['hits']

# Mapping is informational, not a content verdict or read-credit source.
current_cache={};mapping=[]


def map_unit(key):
    u=units[key];p=u['path'];raw=original(p);old=normalized_originals[p]
    if p not in current_cache:
        b=Path(p).read_bytes();digest=sha(b);text=b.decode('utf-8-sig',errors='replace').replace('\r\n','\n')
        if p in accepted:assert digest==accepted[p],('Current product not bound to accepted capture',p)
        else:assert digest==files[p]['sha256'] or text==old,('Unexpected current nonaccepted product delta',p)
        a=old.splitlines(keepends=True);c=text.splitlines(keepends=True);line_map={}
        if old==text:line_map={i:i for i in range(len(a))}
        else:
            for tag,i,j,x,y in difflib.SequenceMatcher(a=a,b=c,autojunk=False).get_opcodes():
                if tag=='equal':line_map.update({k:x+k-i for k in range(i,j)})
        current_cache[p]=dict(sha256=digest,text=text,old_lines=a,new_lines=c,line_map=line_map)
    cur=current_cache[p];start=len(raw[:u['start_character']].replace('\r\n','\n'));end=len(raw[:u['end_character_exclusive']].replace('\r\n','\n'))
    body=raw[u['start_character']:u['end_character_exclusive']];assert sha(body.encode())==u['range_sha256'];body_norm=body.replace('\r\n','\n')
    old_offsets=[0];new_offsets=[0]
    for line in cur['old_lines']:old_offsets.append(old_offsets[-1]+len(line))
    for line in cur['new_lines']:new_offsets.append(new_offsets[-1]+len(line))
    first=bisect.bisect_right(old_offsets,start)-1;last=bisect.bisect_right(old_offsets,max(start,end-1))-1
    location=None
    if all(i in cur['line_map'] for i in range(first,last+1)):
        mapped=[cur['line_map'][i] for i in range(first,last+1)]
        if mapped==list(range(mapped[0],mapped[0]+len(mapped))):
            a=new_offsets[mapped[0]]+(start-old_offsets[first]);b=new_offsets[mapped[-1]]+(end-old_offsets[last])
            if cur['text'][a:b]==body_norm:location=(a,b,'CONTIGUOUS-UNCHANGED-LINE-MAPPING')
    if location is None and body_norm:
        a=cur['text'].find(body_norm)
        if a>=0 and cur['text'].find(body_norm,a+1)<0:location=(a,a+len(body_norm),'UNIQUE-EXACT-WHOLE-UNIT-TEXT-MAPPING')
    result=dict(id=key,path=p,original_first_line=u['first_line'],original_last_line=u['last_line'],original_range_sha256=u['range_sha256'],
        current_file_sha256=cur['sha256'],accepted_file_hash_bound=p in accepted,mapping_never_grants_read_or_clean_credit=True,
        known_accepted_edited_overlap=hits(key,accepted_spans),known_reviewed_queued_overlap=hits(key,pending_spans),
        assigned_unfinished_guide_correction=guide_pending.get(key),
        unfinished_guide_patch_in_same_file=p in guide_pending_paths,
        same_file_flag_is_not_exact_overlap_or_credit_withholding=True)
    if location:
        a,b,state=location;result.update(state=state,current_first_line=cur['text'].count('\n',0,a)+1,
            current_last_line=cur['text'].count('\n',0,max(a,b-1))+1,current_normalized_start_character=a,current_normalized_end_character_exclusive=b)
    else:result['state']='ORIGINAL-OVERLAP-CHANGED-OR-MAPPING-RECONCILIATION-REQUIRED'
    return result


for key,row in completed.items():
    m=map_unit(key);mapping.append(m)
    reasons=[]
    if row['raw_category']!='supported-pending-final-eligibility':reasons.append(row['raw_category'])
    if 'sampling_credit_adjudication' in row:reasons.append('source-evidence-qualified-context-overlay')
    if m['known_accepted_edited_overlap']:reasons.append('known-accepted-edited-overlap')
    if m['known_reviewed_queued_overlap']:reasons.append('known-reviewed-queued-overlap')
    if m['assigned_unfinished_guide_correction']:reasons.append('confirmed-guide-correction-pending')
    if m['state']=='ORIGINAL-OVERLAP-CHANGED-OR-MAPPING-RECONCILIATION-REQUIRED' and not reasons:reasons.append('current-location-reconciliation-required')
    row['credit_withholding_reasons']=reasons;row['current_mapping_record']='current-mapping.json';row['final_clean_credit']=0 if reasons else None
assert len(mapping)==1122

# Strengthen only the replacement-pool preamble screen: case-insensitive legal text and actual
# complete-unit XML text/reference content, never the old features.xml flag alone.
preamble_evidence={}


def preamble_screen(key):
    u=units[key];body=original(u['path'])[u['start_character']:u['end_character_exclusive']]
    assert sha(body.encode())==u['range_sha256']
    code=[];ordinary=[];xml=[]
    prefixes=[s.casefold() for s in ['Copyright ','Licensed under ','You may not ','You may obtain ','http://www.apache.org/',
        'Unless required ','distributed under ','WITHOUT WARRANTIES ','See the License ','limitations under ','the License','Version 2.0','SPDX-']]
    for _,token,value in CSharpLexer().get_tokens_unprocessed(body+'\n'):
        if token not in Comment:code.append(value);continue
        for line in value.splitlines():
            stripped=line.strip(' /*\t')
            if line.lstrip().startswith('///'):xml.append(line.lstrip()[3:]);continue
            if not stripped or stripped.casefold().startswith(('#pragma warning','#nullable')) or any(stripped.casefold().startswith(p) for p in prefixes):continue
            ordinary.append(stripped)
    code_text=re.sub(r'(?m)^\s*(?:global\s+)?using\s+[^;]+;|(?m:^\s*namespace\s+[\w.]+\s*[;{])','',''.join(code))
    meaningful_code=re.sub(r'[\s{};]','',code_text);xml_source='\n'.join(xml);xml_plain=re.sub(r'<[^>]+>','',xml_source).strip()
    summary_present=bool(re.search(r'[A-Za-z0-9]',xml_plain) or re.search(r'\bcref\s*=|<inheritdoc\b',xml_source))
    pure=not meaningful_code and not summary_present and not any(re.search(r'[A-Za-z0-9]',s) for s in ordinary)
    preamble_evidence[key]=dict(id=key,path=u['path'],source_sha256=u['source_sha256'],range_sha256=u['range_sha256'],
        first_line=u['first_line'],last_line=u['last_line'],nonadministrative_code_characters=len(meaningful_code),
        XML_plain_text_characters=len(xml_plain),substantive_XML_text_or_reference_present=summary_present,
        nonlegal_ordinary_comment_lines=len(ordinary),pure_administrative=pure,scope='Whole original unit lexical scope screen only, not a source-correctness verdict')
    return pure


assert preamble_screen('core-001:unit:f7fc274baa684518')
for key in ['core-001:unit:b4125585d8579c68','core-001:unit:7d926f1edba94468','core-001:unit:2ab1f3fe98ee5ca4']:
    assert not preamble_screen(key) and preamble_evidence[key]['substantive_XML_text_or_reference_present']
known_withheld={k for k,r in completed.items() if r['credit_withholding_reasons']}
SW_pool_ids={r['id'] for r in load(PREP/'family-replacements/revision-2/replacement-pool-exclusions.json')}
ctap=load(RUN/'boundaries/Ctap2/unit-dispositions.json');localized_facets={r['id']:r['candidate_ids'] for r in ctap if r['disposition']=='READ-CLEAN' and r.get('candidate_ids')}
old_tree=ast.parse(prior_helper)
research_node=next(n for n in old_tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='current_research_headings' for t in n.targets))
pool_exclusions={}
namespace.update(known_withheld=known_withheld,SW_pool_ids=SW_pool_ids,localized_facets=localized_facets,
    current_research_headings=ast.literal_eval(research_node.value),pool_exclusions=pool_exclusions)
credential_handlers={'HandlePinVerifyAsync','HandlePinChangeAsync','HandlePukChangeAsync','HandlePinUnblockAsync','HandleMgmtKeyAuthAsync','HandleMgmtKeyChangeAsync',
    'HandleKeyGenerateAsync','HandleCertSelfSignAsync','HandleCertCsrAsync','HandleCertDeleteAsync','HandleCertImportAsync',
    'HandleSignAsync','HandleDecryptAsync','HandleAttestAsync','HandleAttestIntermediateAsync','HandleResetAsync'}


def pool_reason(key):
    u=units[key];p=u['path'];label=u['label'];reason=None
    if key in known_withheld:reason='Exact actual sampler zero-credit/source-qualified/miss/admin/edited status; no recycling.'
    elif p=='src/Piv/examples/PivTool/Cli/Commands/CliRunner.cs' and any(re.search(r'\b'+name+r'\(',label) for name in credential_handlers):
        reason='Known CliRunner credential/discovery/pre-protected-scope handler surface; avoid this cleanup-facet family in the replacement pool only. No new method defect verdict or denominator removal.'
    elif p=='src/OpenPgp/src/PrivateKeyImport.cs' and ('Curve25519Template(' in label or 'PrivateKeyImport Create(' in label):
        reason='Known unresolved PGP X25519 import/template dispatch surface; replacement-pool-only avoidance, not a class/algorithm denominator shrink or new source judgment.'
    elif key in SW_pool_ids:reason='Existing original-ISO SWConstants replacement-pool restriction; broad XML denominator104 remains.'
    elif key in localized_facets:reason='Exact localized unresolved candidate links; avoid in replacement pool, no file-wide exclusion.'
    elif hits(key,accepted_spans) or hits(key,pending_spans):reason='Exact known accepted or reviewed-queued changed-span overlap.'
    elif u['category']=='source-context-and-explanations' and p.endswith('.cs'):
        if preamble_screen(key):reason='Whole-unit screen confirms legal/import/namespace/pragma-only content and no substantive summary/reference or declaration.'
        # A real summary/reference survives; do not reapply the old legal-prefix heuristic to XML.
    else:return namespace['pool_reason'](key)
    if reason:pool_exclusions[key]=dict(id=key,owner=byid[key]['owner'],stratum=byid[key].get('stratum'),path=p,label=label,
        reason=reason,scope='REPLACEMENT-POOL-ONLY',global_denominator_change=0,preparer_content_verdict=None)
    return reason


# Record the explicitly requested avoidance surfaces even when a prior proposal or original edited
# overlap would already skip them before the rank walk reaches the lexical pool screen.
for key,u in units.items():
    if key not in byid or byid[key]['eligibility']!='candidate-eligible':continue
    if (u['path']=='src/Piv/examples/PivTool/Cli/Commands/CliRunner.cs' and any(re.search(r'\b'+name+r'\(',u['label']) for name in credential_handlers)) or (u['path']=='src/OpenPgp/src/PrivateKeyImport.cs' and ('Curve25519Template(' in u['label'] or 'PrivateKeyImport Create(' in u['label'])):
        assert pool_reason(key)
prior_proposed=set(completed)|{r['id'] for r in load(OLD/'unread-required.json')}|set(load(OLD/'superseded-proposals.json')['retired_SW_ids'])
assert {r['id'] for r in previous_unread}<set(completed)
next_rows=[];quota_rows=[]
for old in sorted(targets,key=lambda q:(q['owner'],q['stratum'])):
    owner,role=old['owner'],old['stratum'];target=old['conservative_required_target']
    supported={k for k,r in completed.items() if r['owner']==owner and r['stratum']==role and not r['credit_withholding_reasons']}
    needed=max(0,target-len(supported));eligible={r['id'] for r in population if r['owner']==owner and r.get('stratum')==role and r['eligibility']=='candidate-eligible'}
    rank=sorted(eligible,key=lambda k:sha((SEED+'|'+owner+'|'+role+'|'+k).encode()));available=[]
    if needed:
        for key in rank:
            if key in prior_proposed or byid[key].get('edited_overlap') or pool_reason(key):continue
            available.append(key)
    chosen=available[:needed]
    for key in chosen:
        u=units[key];body=original(u['path'])[u['start_character']:u['end_character_exclusive']];assert sha(body.encode())==u['range_sha256']
        next_rows.append(dict(id=key,owner=owner,stratum=role,original_rank=rank.index(key)+1,
            **{f:u[f] for f in ['path','first_line','last_line','start_character','end_character_exclusive','source_sha256','range_sha256','category','label']},
            purpose='ONLY REMAINING CONSERVATIVE ROLE-QUOTA SHORTFALL; NO NEW FULL CATEGORY',read_status='PROPOSED-UNREAD',verdict='UNASSIGNED',clean_credit=0,
            current_mapping='Final accepted/queued combined reconciliation pending; original whole unit supplied'))
        prior_proposed.add(key)
    quota_rows.append(dict(owner=owner,stratum=role,broad_original_population=old['broad_original_population'],original_quota=old['original_quota'],
        already_authorized_full_expansion=old['already_authorized_full_expansion'],conservative_target=target,
        actually_read_source_supported_without_known_hold=len(supported),remaining_need=needed,selected=len(chosen),available_pool=len(available) if needed else None,
        shortfall=needed-len(chosen),new_ids=chosen,final_denominator=None))
assert sum(q['conservative_target'] for q in quota_rows)==1067
assert len(next_rows)==len({r['id'] for r in next_rows}) and not {r['id'] for r in next_rows}&completed.keys()
if (OUT/'next-required-selection.json').exists():
    assert [r['id'] for r in json.loads((OUT/'next-required-selection.json').read_text())]==[r['id'] for r in next_rows], 'Preserve published proposal history before changing IDs.'
for p,digest in inputs.items():assert sha(Path(p).read_bytes())==digest,('Bound record changed',p)
for p,digest in accepted.items():assert sha(Path(p).read_bytes())==digest,('Accepted primary changed during capture',p)
for p,row in current_cache.items():assert sha(Path(p).read_bytes())==row['sha256'],('Current mapped file changed during capture',p)
emit('completed-read-tally.json',list(completed.values()));emit('current-mapping.json',mapping);emit('pending-corrections.json',pending)
emit('next-required-selection.json',next_rows);emit('role-quota-status.json',quota_rows);emit('replacement-pool-exclusions.json',list(pool_exclusions.values()))
emit('preamble-screen-evidence.json',list(preamble_evidence.values()))
emit('next-required-current-mapping.json',[map_unit(r['id']) for r in next_rows])
emit('known-edit-overlap-inputs.json',dict(accepted_manifest_sha256=sha(manifest_bytes),accepted_original_changed_spans=accepted_spans,
    reviewed_queued_original_changed_spans=pending_spans,PGP_queued_only_spans=pgp_queued_spans,
    PGP_accepted_plus_queued_original_scope_spans=pgp_combined_spans,PGP_EOF_evidence=pgp_eof_evidence,
    qualification='Combined PGP scope diff validates pinned coordinates and exact EOF. Only projected queued-only spans are attributed as pending. WA prior spans retained; unfinished guide deltas remain unknown.'))
text=['# Last bounded role-quota replacement whole units','','**No content verdicts.** Pinned source `'+PIN+'`; read complete units and relevant enclosing context.','']
for row in next_rows:
    body=original(row['path'])[row['start_character']:row['end_character_exclusive']]
    text+=['## `'+row['id']+'`',f"`{row['path']}:{row['first_line']}–{row['last_line']}`; {row['owner']} {row['stratum']}; original rank {row['original_rank']}.",'','````'+('csharp' if row['path'].endswith('.cs') else 'markdown'),body,'````','']
(OUT/'next-required-original-units.md').write_text('\n'.join(text)+'\n')
summary=dict(source_baseline=PIN,actual_unique_reads=1122,previous_actual_reads=1069,new_actual_reads=53,new_explicit_supported=46,new_explicit_zero=7,
    actual_IDs_mapped_once=True,raw_category_counts=dict(collections.Counter(r['raw_category'] for r in completed.values())),
    supported_without_current_known_hold=sum(not r['credit_withholding_reasons'] for r in completed.values()),
    current_mapping_states=dict(collections.Counter(r['state'] for r in mapping)),
    conservative_broad_population=9352,conservative_role_target_sum=1067,new_full_category_quota=0,
    next_required=sum(q['remaining_need'] for q in quota_rows),next_selected=len(next_rows),shortfall=sum(q['shortfall'] for q in quota_rows),
    final_denominator=None,final_clean_credit=None,sampler_PASS=None,preparer_content_verdicts=0,
    pending_correction_scopes=len(pending),current_mapping_is_not_read_credit=True,accepted_manifest_paths=len(accepted),accepted_diff_scope=accepted_diff_scope,
    primary_state='Reserved; no transfer/edit/SDK/Git mutation by preparer',input_sha256=inputs)
emit('summary.json',summary)
plan=['# Updated provisional actual-read tally and last bounded queue','','**No final clean-quota or sampler pass.** All **1,122 unique units** have explicit actual Astra verdicts: 1,069 predecessors plus exactly 46 supported-list IDs and seven zero-credit table IDs from `remaining-53-verdicts.md`. No read credit comes from file presence or descriptions.','',
    f"Conservative role-target sum stays **1,067**; broad admitted population stays **9,352**. Actual source-supported reads without known holds: **{summary['supported_without_current_known_hold']}** (still pending final eligibility/mapping). Next bounded need **{summary['next_required']}**, selected **{len(next_rows)}**, pool shortfall **{summary['shortfall']}**. No new full-category quota.",
    'The seven latest zero dispositions remain exact: OTP static pre-try configuration cleanup; two existing A003 guide propagation misses; SAMPLE-TESTING-001 runner table; existing PGP X25519 import qualification; existing PIVIMP-006 CSR pre-try PIN/discovery qualification; pure WebAuthn preamble. No repeated caller wording becomes an independent bug.','',
    '## Next actual readings']
for row in next_rows:plan.append(f"- `{row['id']}` — `{row['path']}:{row['first_line']}–{row['last_line']}`; {row['owner']} {row['stratum']}; original rank {row['original_rank']}; **UNREAD / zero credit**.")
plan+=['','[next-required-original-units.md](next-required-original-units.md) supplies exact whole pinned units. `role-quota-status.json` shows every owner/role requirement and no other-stratum reduction.',
    '## Current mapping and staged corrections',
    '`current-mapping.json` maps all 1,122 completed reads to accepted-primary bytes or records original overlaps/mapping holds. Mapping never grants content or read credit. Current accepted source hashes and original normalized range equality are bound separately.',
    '`pending-corrections.json` flags four work scopes: reviewed queued WA oracle patch, reviewed queued PGP UIF fallback patch, assigned A003 two-guide propagation correction and assigned TESTING-001 runner table. Unfinished guide patch bytes are not inspected or guessed. Final transfer/canonical validation may affect adjacent ranges.',
    '## Pool rules and final gates',
    'Only next-slot replacements are selected. The pool avoids known CliRunner credential/discovery/pre-try handler surfaces and X25519-specific import/template dispatch paths, without shrinking broad populations or declaring unrelated private methods defective.',
    'Pure-preamble screening reads the entire original unit, case-folds legal headers and distinguishes actual substantive XML text/reference content from empty tags. f7fc... is verified as pure; independently supported summary-bearing preambles are preserved as substantive. `preamble-screen-evidence.json` records that lexical scope evidence, not a content verdict.',
    'Previous selections, actual verdicts and every stale/retired proposal remain preserved as history. `completed-read-tally.json` supersedes only the provisional accounting, not original source-disposition statuses. Keep primary stable while compilation/capture guards are being fixed by their owner.',
    'Astra must read the new units and record outcomes. Final accepted and queued-patch mapping, final denominator/quotas, consumer/capture/compiler validation and publication remain pending. No source audit/research, product edits, SDK/build/test/native/device/formatter/crypto execution, Git mutation or subagent. Finish Core Pass 1 publication, then stop.']
(OUT/'read-plan.md').write_text('\n'.join(plan)+'\n')
receipt=dict(result='STRUCTURAL-1122-READ-AND-LAST-BOUNDED-QUEUE-CHECKS; NO CONTENT/SAMPLER PASS',source_baseline=PIN,
    exact_actual_read_IDs=1122,new_record_IDs=53,new_supported_list_IDs=46,new_zero_table_IDs=7,duplicates=0,
    conservative_broad_population=9352,conservative_role_targets=1067,next_required=summary['next_required'],next_selected=len(next_rows),pool_shortfall=summary['shortfall'],new_full_category_quota=0,
    current_mappings=1122,staged_work_scopes=len(pending),pure_preamble_casefold_and_summary_scope_checks=True,
    final_denominator=None,final_clean_credit=None,sampler_PASS=None,preparer_content_verdicts=0,input_sha256=inputs,
    outputs=[dict(path=str(p),bytes=p.stat().st_size,sha256=sha(p.read_bytes())) for p in sorted(OUT.iterdir()) if p.is_file() and p.name not in ['verification.json','preparation.stdout.json','structural-check.json']])
emit('verification.json',receipt)
print(json.dumps({k:v for k,v in summary.items() if k not in ['input_sha256','accepted_diff_scope']},indent=2))
