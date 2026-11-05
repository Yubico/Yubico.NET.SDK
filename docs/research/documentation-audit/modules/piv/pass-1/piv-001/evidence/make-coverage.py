"""Index investigator-read source. This script checks records; it never compiles or accesses a device."""
import collections, hashlib, json, pathlib, re
ROOT=pathlib.Path(__file__).parent
RUN=ROOT.parent
inventory=json.loads((RUN/'inventory.json').read_text())
baseline=inventory['baseline']
# Whole-file candidate associations are conservative: the editor/reviewer refines affected spans.
findings={
 'IPivSession.cs':['PIV-C001','PIV-C002','PIV-C003','PIV-C004','PIV-C005','PIV-C006','PIV-C007','PIV-C011'],
 'PivSession.cs':['PIV-C001','PIV-C008','PIV-C011','PIVIMP-001','PIVIMP-002'],
 'README.md':['PIV-C001','PIV-C002','PIV-C003','PIV-C004','PIV-C005','PIV-C006','PIV-C007','PIV-C009'],
 'CLAUDE.md':['PIV-C008','PIV-C006'],
 'PinUtilities.cs':['PIV-C002'], 'AuthenticationProtocol.cs':['PIV-C002','PIV-C008'],
 'PrivateKeyOperations.cs':['PIV-C003','PIV-C008','PIVIMP-007'],
 'PinOnlyMode.cs':['PIV-C005'], 'PinPolicy.cs':['PIV-C006'],
 'Features.cs':['PIV-C007','PIVIMP-003'],
 'CardholderUniqueId.cs':['PIV-C004','PIVIMP-004'],
 'AdminData.cs':['PIV-C004','PIVIMP-009'],
 'DataObjectProtocol.cs':['PIV-C004','PIVIMP-010'],
 'CardCapabilityContainer.cs':['PIV-C004','PIVIMP-004'],
 'KeyHistory.cs':['PIV-C004','PIVIMP-004'],
 'Metadata.cs':['PIV-C004','PIV-C011'], 'SlotMetadataExtensions.cs':['PIV-C003','PIV-C009'],
 'Signing.cs':['PIV-C009','PIVIMP-005'], 'Decryption.cs':['PIV-C009','PIVIMP-006'],
 'PinManagement.cs':['PIV-C002','PIV-C009'], 'Certificates.cs':['PIV-C009','PIVIMP-006'],
 'Attestation.cs':['PIV-C001','PIV-C009'], 'Reset.cs':['PIV-C001','PIV-C009'],
 'PinPrompt.cs':['PIV-C008','PIVIMP-006'], 'Program.cs':['PIV-C008'],
 'CertificateResult.cs':['PIV-C009'], 'AttestationResult.cs':['PIV-C009'],
 'SlotInfoResult.cs':['PIV-C009'], 'DecryptionResult.cs':['PIV-C009','PIVIMP-006'],
 'KeyGenerationResult.cs':['PIV-C009'], 'DeviceInfo.cs':['PIV-C008','PIVIMP-006'],
 'BioProtocolTests.cs':['PIV-C008'], 'DataObjectProtocolTests.cs':['PIV-C008'],
 'PrivateKeyOperationsTests.cs':['PIV-C008'], 'PivSessionTests.cs':['PIV-C008'],
 'PivPolicyTests.cs':['PIV-C010','PIVIMP-008'], 'PivFullWorkflowTests.cs':['PIV-C010','PIVIMP-008'],
 'PivCryptoTests.cs':['PIV-C010','PIVIMP-008'], 'PivImportTests.cs':['PIV-C010','PIVIMP-008'],
 'PivSigningAlgorithmTests.cs':['PIV-C010'], 'PivDecryptTests.cs':['PIV-C010','PIVIMP-008'],
 'PivDiscoveryContentionTests.cs':['PIV-C010'], 'PivSessionContentionTests.cs':['PIV-C009','PIV-C010'],
 'PivMultiKeyContentionTests.cs':['PIV-C010','PIVIMP-008'],
 'JsonOutput.cs':['PIV-C008','PIVIMP-006'], 'CertificatesMenu.cs':['PIVIMP-006'],
}
files=[];units=[];hardware=[]
def unit(path,start,end,name,category,ids):
 lines=pathlib.Path(path).read_text().splitlines()
 text='\n'.join(lines[start-1:end])
 return {'id':f'{path}:{start}-{end}','path':path,'start':start,'end':end,'name':name,
  'category':category,'sha256':hashlib.sha256(text.encode()).hexdigest(),
  'read_status':'full-semantic-read','disposition':'candidate-associated' if ids else 'clean-nominee',
  'candidate_ids':ids,'independent_review':'pending','sample':'coordinator-selects',
  'sampling_eligible':not bool(ids)}
for entry in inventory['files']:
 p=pathlib.Path(entry['path']);text=p.read_text();lines=text.splitlines();ids=findings.get(p.name,[])
 if p.name=='README.md' and '/examples/' in str(p):ids=['PIV-C001','PIV-C009','PIVIMP-005','PIVIMP-006']
 category='api' if '/src/' in str(p) and p.suffix=='.cs' and re.search(r'^public\s',text,re.M) else 'internal-test'
 if '/examples/' in str(p):category='example'
 if p.suffix=='.md':category='narrative'
 if p.suffix in ('.json','.csproj','.txt'):category='configuration-api-snapshot'
 sha=hashlib.sha256(p.read_bytes()).hexdigest()
 files.append({**entry,'observed_sha256':sha,'matches_inventory':sha==entry['sha256'],
  'read_status':'full-semantic-read','candidate_ids':ids,'category':category})
 if p.suffix=='.md':
  starts=[i for i,l in enumerate(lines) if re.match(r'^#{1,6} ',l)] or [0]
  if starts[0]>0:starts.insert(0,0)
  for j,s in enumerate(starts):units.append(unit(str(p),s+1,starts[j+1] if j+1<len(starts) else len(lines),lines[s],category,ids))
 elif p.suffix=='.cs':
  # XML contract blocks form self-contained source regions. Remaining contiguous regions include
  # implementation/assertions and local comments; ranges partition the file without overlap.
  blocks=[];i=0
  while i<len(lines):
   if lines[i].lstrip().startswith('///'):
    s=i
    while i+1<len(lines) and lines[i+1].lstrip().startswith('///'):i+=1
    blocks.append((s,i,'xml:'+lines[s].strip()))
   i+=1
  starts={s:name for s,e,name in blocks}
  # A test without an XML summary remains an implicit explanatory unit: its actual assertions
  # and full helper context matter. Split before its attributes, retaining local comments.
  if '/tests/' in str(p):
   for i,line in enumerate(lines):
    if re.search(r'public\s+(?:async\s+)?(?:Task(?:<[^>]+>)?|void)\s+\w+\s*\(',line):
     s=i
     while s>0 and (lines[s-1].lstrip().startswith(('[','//')) or not lines[s-1].strip()):s-=1
     # An existing XML boundary already carries this test's summary and attributes.
     nearby=[a for a,b,n in blocks if a<=s<=b+1 or s<=a<i]
     if not nearby:starts[s]='test/assertions:'+line.strip()
  ordered=sorted(starts)
  if ordered and ordered[0]>0:ordered.insert(0,0);starts[0]='file/import/type-context'
  if not ordered:ordered=[0];starts[0]='implementation/assertions/local-context'
  for j,s in enumerate(ordered):
   end=ordered[j+1] if j+1<len(ordered) else len(lines)
   u=unit(str(p),s+1,end,starts[s],category,ids)
   if starts[s]=='file/import/type-context':u['sampling_eligible']=False
   units.append(u)
 else:units.append(unit(str(p),1,len(lines),'configuration/API reconciliation',category,ids))
 if '.IntegrationTests/' in str(p) and p.suffix=='.cs':
  for m in re.finditer(r'public\s+async\s+Task(?:<[^>]+>)?\s+(\w+)\s*\([^)]*\)\s*\{',text):
   opening=text.index('{',m.start());depth=1;end=opening+1
   while depth and end<len(text):
    if text[end]=='{':depth+=1
    elif text[end]=='}':depth-=1
    end+=1
   body=text[opening:end];startline=text[:m.start()].count('\n')+1
   attr_start=text.rfind('\n    [',0,m.start());previous=text.rfind('\n    }',0,m.start())
   attributes=text[max(previous+6,0):m.start()]
   hardware.append({'path':str(p),'method':m.group(1),'start':startline,'end':text[:end].count('\n')+1,
    'attributes':re.findall(r'^\s*\[.*\]',attributes,re.M),
    'direct_session_calls':re.findall(r'\b(\w+Async)\s*\(',body),
    'assertions':[l.strip() for l in body.splitlines() if 'Assert.' in l],
    'early_returns_skips':[l.strip() for l in body.splitlines() if re.search(r'\breturn\b|Skip\.',l)],
    'has_finally':'finally' in body,'source_body':body,
    'execution_status':'not-run-by-investigator',
    'helper_effects':'See hardware-test-plan.md; class setup/helpers are included in semantic review.'})
api=[l for l in pathlib.Path('src/Piv/src/PublicAPI.Unshipped.txt').read_text().splitlines() if l.strip() and not l.startswith('#')]
types=[l for l in api if re.fullmatch(r'Yubico\.YubiKit\.Piv\.[\w.]+',l)]
ops=[l for l in api if l.startswith('Yubico.YubiKit.Piv.IPivSession.') and 'Async(' in l]
reconciliation={'baseline':baseline,'api_entries':len(api),'effective_public_types':types,'public_type_count':len(types),
 'interface_operations':len(ops),'interface_operation_entries':ops,'interface_properties':2,
 'extension_emission':'C#14 extension receiver declaration + extension member + static compatibility member represent one callable creation operation.',
 'accessibility':'PinUtilities public-looking members remain internal through containing type. Backends/helpers/KeyReference/KeyTypeEncoding/Initialization are internal; PivSession constructor is internal; EnsureSupports is inherited protected.',
 'generated':'Record Equals/GetHashCode/ToString/operators/Deconstruct/default constructors and positional accessors are snapshot entries, not extra workflow APIs. Positional metadata semantics belong on each record declaration.',
 'shipped':'Nullable header only; 0 entries. Snapshots preserved, no regeneration or compilation in this assignment.'}
(ROOT/'api-reconciliation.json').write_text(json.dumps(reconciliation,indent=2)+'\n')
(ROOT/'hardware-methods.json').write_text(json.dumps(hardware,indent=2)+'\n')
summary={'baseline':baseline,'investigator':'GPT-6.1 Sol; fresh investigator','read_scope':'All 123 inventory paths; complete source semantics; scoped shared units separately recorded in investigation.md',
 'counts':{'files':len(files),'semantic_regions':len(units),'clean_nominee_regions':sum(u['sampling_eligible'] for u in units),'hardware_methods':len(hardware)},
 'unit_rule':'Nonoverlapping source regions partition each file: narrative heading sections; XML contract plus following declaration/body/context; implicit test methods with attributes/assertions; configuration/API snapshots. Helpers remain attached to their calling file context. Candidate-associated files are conservatively excluded from clean sampling until the editor refines exact changed ranges. File/import/type prefixes and snapshot/configuration regions are excluded from sampling.',
 'files':files,'units':units}
for u in units:
 if u['category']=='configuration-api-snapshot' or (u['start']==1 and u['name']=='implementation/assertion/local-context' and u['end']<30):u['sampling_eligible']=False
eligible=[u for u in units if u['sampling_eligible']]
shared_specs=[
 ('README.md',59,59,[]),
 ('docs/v2-highlights.md',90,119,[]),
 ('docs/v2-highlights.md',143,149,[]),
 ('docs/NATIVE-AOT.md',1,55,[]),
 ('docs/architecture/applet-public-api.md',22,41,[]),
 ('docs/architecture/sdk-architecture-diagrams.md',89,101,['PIV-C011']),
 ('docs/architecture/raw-access-tiers.md',62,78,['PIV-C009']),
 ('docs/usage/user-interaction.md',15,28,[]),
 ('docs/usage/user-interaction.md',186,191,['PIV-C006']),
 ('docs/v1-to-v2-comparison.md',138,143,['PIV-C011']),
 ('docs/migration/v1-to-v2.md',64,95,['PIV-C011']),
 ('docs/migration/v1-to-v2.md',109,125,['PIV-C011']),
 ('docs/migration/v1-to-v2.md',313,389,[]),
 ('docs/migration/v1-to-v2.md',620,664,['PIV-C005']),
 ('docs/migration/v1-to-v2.md',709,716,['PIV-C011']),
 ('docs/migration/v1-to-v2-map.yml',370,398,['PIV-C011']),
 ('docs/migration/v1-to-v2-gaps.md',83,124,['PIV-C011']),
]
shared_units=[unit(p,s,e,'PIV-scoped shared reference','narrative' if p.endswith('.md') else 'example',ids) for p,s,e,ids in shared_specs]
summary['shared_units']=shared_units
eligible += [u for u in shared_units if u['sampling_eligible']]
summary['sampling']={'eligible_clean_regions':len(eligible),'required_minimum':max(3,(len(eligible)+9)//10),'by_category':dict(collections.Counter(u['category'] for u in eligible)),
 'selected':[],'rule':'Coordinator selects >=10%, minimum3; include api, narrative/example, internal-test; deduplicate exact ranges and remove edited overlaps. This is a nominee denominator, not completed independent sampling.'}
(RUN/'coverage.json').write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps({'counts':summary['counts'],'sampling':summary['sampling'],'api':{'entries':len(api),'types':len(types),'operations':len(ops)}},indent=2))
