"""Join authored complete-read notes to original IDs; verify records, never run SDK code."""
import collections
import hashlib
import json
import pathlib
import subprocess

HERE = pathlib.Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / '.git').exists())
RUN = HERE.parent.parent
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'

def load(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def sha(data):
    return hashlib.sha256(data).hexdigest()

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)

def save(name, value):
    (HERE / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')

NOTES = load(HERE / 'semantic-notes.json')
PACKET = load(RUN / 'inventory/packets/B5-session-framing.json')
UNITS = {u['id']: u for u in load(RUN / 'inventory/semantic-units.json')}
SHARED = {u['unit_id']: u for u in load(RUN / 'inventory/shared-sections.json')}

# Explicit investigator-selected concern ranges. Intersections identify review context, not a bug on every line.
RANGES = {
 'C001': [('IApplicationSession.cs', 21, 44), ('ApplicationSession.cs', 57, 86), ('ApplicationSession.cs', 307, 397), ('RawCtapHidSession.cs', 21, 76), ('RawOtpHidSession.cs', 21, 77), ('RawSmartCardSession.cs', 48, 122), ('SessionCreationOptions.cs', 22, 61)],
 'C002': [('FidoHidProtocol.cs', 268, 387), ('IFidoHidProtocol.cs', 27, 53), ('RawCtapHidSession.cs', 60, 76), ('FidoHidProtocolTests.cs', 558, 848)],
 'C003': [('FidoHidProtocol.cs', 133, 187), ('FidoHidProtocol.cs', 327, 433), ('FidoHidProtocolTests.cs', 24, 180), ('FidoHidProtocolTests.cs', 253, 399)],
 'C004': [('IFidoHidConnection.cs', 19, 70), ('RawCtapHidSession.cs', 60, 76), ('RawOtpHidSession.cs', 63, 77)],
 'C005': [('FidoHidProtocol.cs', 273, 325), ('FidoHidProtocol.cs', 414, 433), ('OtpHidProtocol.cs', 226, 252), ('OtpHidProtocol.cs', 327, 391), ('OtpHidProtocol.cs', 463, 489), ('FidoHidProtocolTests.cs', 402, 508), ('OtpHidProtocolTests.cs', 615, 778)],
 'C006': [('ApplicationSession.cs', 26, 44), ('ApplicationSession.cs', 123, 200), ('ApplicationSession.cs', 307, 397), ('ApplicationSession.cs', 434, 568), ('ExchangeGuard.cs', 19, 56), ('OtpHidProtocol.cs', 617, 632)],
 'C007': [('OtpHidProtocol.cs', 85, 129), ('OtpHidProtocol.cs', 183, 200), ('OtpHidProtocol.cs', 217, 321), ('OtpHidProtocol.cs', 393, 430), ('OtpConstants.cs', 71, 92)],
 'C008': [('FidoHidProtocolConcurrencyTests.cs', 21, 142), ('OtpHidProtocolConcurrencyTests.cs', 21, 110), ('RawCtapHidSessionTests.cs', 51, 73), ('RawOtpHidSessionTests.cs', 25, 86), ('RawSmartCardSessionTests.cs', 99, 140), ('RawSmartCardSessionTests.cs', 216, 240), ('RawSessionYubiKeyExtensionsTests.cs', 85, 190), ('ApplicationSessionDisposalTests.cs', 118, 149), ('UserPresenceNotificationTests.cs', 148, 167), ('SessionConstructionGuardTests.cs', 74, 106), ('ExchangeGuardTests.cs', 21, 169), ('FidoHidProtocolTests.cs', 558, 622), ('FidoHidProtocolTests.cs', 694, 748)],
 'C009': [('ConnectionSessionGuard.cs', 20, 42), ('ProtocolConnectionOwnershipTests.cs', 23, 40), ('SessionConstructionGuardTests.cs', 22, 32)]
}

SHARED_NOTES = {
 '691deab3141bcc0b': ('READ-EXCLUSION', [], 'Complete dated public-access decision and historical D16-D31/native evidence read. Preserve historical RawFidoHidSession vocabulary and source/package/device chronology; exclude from current contract/edit/clean denominator, not from reading.'),
 '3b82efe0a9a7aee3': ('READ-CANDIDATE', ['C001','C006'], 'Session Shape matches prepare/select/configure/readiness callback ordering; raw readiness, parameter retention exception and whole-workflow caveats require nearby qualifications. Not all options services are retained by every applet.'),
 'c337679fdbe3f69e': ('READ-CLEAN', [], 'Tier0 applet path and complete PIV GetPinMetadata snippet read; explicit typed prerequisites and no native execution. Firmware gates and applet classification remain local.'),
 '604b8d2421d76d79': ('READ-CANDIDATE', ['C002','C004','C006'], 'Tier1 framing inventory is accurate for ordinary exchanges; qualify request-response CTAPHID control commands, direct borrowed opens and whole-operation sequencing.'),
 '197ff074340e9a0e': ('READ-CANDIDATE', ['C002','C004'], 'Complete FIDO example/response-correlation section read. Supplied command/payload semantics and memory borrow need explicit ordinary request-response and actual-terminal-task limits.'),
 'e1b3c37a79542037': ('READ-CLEAN', [], 'Complete OTP example, caller-known prefix CRC check and one-abort/reset-latch/reopen section read against source. expectedLength is a valid command-defined nonnegative in-bounds prerequisite; no applet semantics or CRC authenticity implied.'),
 'e69894b46c744fcb': ('READ-CANDIDATE', ['C004','C006','C008'], 'Whole retained synchronous-path tables read with B1/B2/B3 owner results; compatibility method dispatch and latent OTP Configure qualifications accurate. Preserve native-platform/test-oracle evidence boundaries and reentry/lifetime limits; B4 Windows remains pending.'),
 '7e1d5b20f31edc04': ('READ-CLEAN', [], 'Before snippet is historical internal-protocol vocabulary; after uses real borrowing RawSmartCardSession plus exact AID. New consumer compilation is pending, not inferred from snippets.'),
 '4a26f5eb924b0b4e': ('READ-CANDIDATE', ['C009'], 'Dated migration note still calls current manager stream repository-diffed Added/Removed truth; add current monitor-owned DeviceUpdated context through SHARED/A without changing original event names.'),
 'f74211fa4f27f246': ('READ-CLEAN', [], 'Manual session-lifecycle migration row correctly requires construction/disposal/transport/authentication/reuse review instead of mechanical compatibility. Historical evidence paths are not current APIs.'),
 'dc1c19b8d927bb13': ('READ-CANDIDATE', ['C009'], 'Complete YAML factory row read. Preferred owned extension is not the only public construction entry; static borrowing CreateAsync remains public. Keep literal YAML grammar and historical DOC-002 evidence unchanged.'),
 'dea0a94378578a80': ('READ-EXCLUSION', [], 'Complete forward-compatibility migration row read. Detailed five CTAP response models and COSE decode assertions belong to Ctap2 boundary, not B5 framing; no claim to independent model clone/unknown-field semantics from transport copies. Retain row for boundary editor, not current clean sample.'),
 '8a007f7d036683e5': ('READ-CLEAN', [], 'Full event migration section describes payloadless hints, watch replay/snapshot/update and owned monitor. Native callbacks and historical listener types remain A/B4 responsibility.'),
 '7a7f459adca197e9': ('READ-CLEAN', [], 'Usage-page versus pair classification migration read. Current FIDO transport vocabulary is coherent; actual native descriptor classification retains platform owner.'),
 '8f68ef659f5a0e3d': ('READ-CANDIDATE', ['C001','C006'], 'Full lifecycle section and presence/intent conditions read. Clarify owned extension versus borrowed static factory and complete workflow sequencing without importing universal service retention.'),
 '7c4db6cca009ab6f': ('READ-CLEAN', [], 'Complete historical v1 ctor/current owned PIV extension alternatives and transport migration notes read; one scope per alternative, no actual hardware or compilation.'),
 '9353d45cd5f6a564': ('READ-CLEAN', [], 'OATH-only terminal reset migration matches successful RESET then DisposeAsync, caller closes borrowed connection and supplies fresh SCP parameters. No current secure RESET execution inferred from historical6A88 setup failure.'),
 'cb611449482d8a1a': ('READ-EXCLUSION', [], 'Historical earlier architecture internalization proposal read in context. Present IHid report APIs remain public; preserve proposal as historical rather than current compatibility claim or clean implementation evidence.'),
 '93efdcb3c94a43cf': ('READ-CLEAN', [], 'Full operation/native/cancel target sequence read. Explicit between-read keepalive CANCEL and no-further-keepalive pending state agree with current protocol; native callbacks target rule is labeled, not current listener acceptance.'),
 '6eebf07a5a9eadb7': ('READ-EXCLUSION', [], 'Dated August2026 evidence preamble read; historical clean-working-tree/revert claims cannot certify this source or current monitor architecture. Preserve without running historical commands.'),
 'fce2188000ccc79f': ('READ-CANDIDATE', ['C001'], 'Eight applet factory shapes supported by narrow actual factory reads; options carry common policy but version source/retained prompt/parameter exceptions need conditions.'),
 'de45cafad3d99a58': ('READ-CANDIDATE', ['C001'], 'Owned versus borrowed grammar supported; shared options do not guarantee identical per-applet firmware or retained-service semantics. Keep exception qualifications with claim.'),
 'd4192829bb584ac4': ('READ-CLEAN', [], 'FakeOathSession firmware5.8 getter read with entire fake, unlock/zero assertions and helpers. It is a fixed test response, not physical metadata or real protocol readiness.'),
 'ad6dd3e7ce26899c': ('READ-CLEAN', [], 'Fake IsSupported always true supports CLI helper isolation; no actual firmware-gate coverage asserted. Whole fake AuthenticateAndRetry and cleanup context read.'),
 '8c70f4cf5a322fe9': ('READ-CANDIDATE', ['C001','C006'], 'Current shared initializer/protocol owner callback summary read; normal full-callback readiness correct. Qualify retained parameters and multi-operation/disposal scope through related sections.'),
 '0d25ea17f40622e1': ('READ-CLEAN', [], 'Heading has no independent behavior claim; enclosing complete session section, raw readiness and version/prompt qualifications read.'),
 '6c9a63f623ae3d6a': ('READ-CLEAN', [], 'Raw-session fragment uses typed borrowed connection, selects exact AID, configures firmware; connectionFactory/reader/ct/firmwareVersion are typed external prerequisites, not invented production APIs.'),
 'e3761d8d59611060': ('READ-CLEAN', [], 'Public compatibility query/protected enforcement migration accurately matches current interface/base and reflection assertions, including no command-success implication.'),
 'ce4f782aab106080': ('READ-CANDIDATE', ['C001','C006'], 'Full applet opening example and readiness/SCP preparation explanation read; binding/failure cleanup correct. Retained service/parameter exceptions and whole-operation boundaries must remain explicit.'),
 '3267b91be99d2042': ('READ-CLEAN', [], 'Full AppletSessionShape imports and public reflection context read; actual eight modules resolve by current source, not a compiled check.'),
 '7449f0b2fc818733': ('READ-CLEAN', [], 'Complete sealed convention test class context read, including all helper methods.'),
 'a17418cf03f78763': ('READ-CLEAN', [], 'Public IsSupported/protected EnsureSupports reflection assertions agree with source. They verify visibility, not operation firmware enforcement.'),
 '01942f1401348dd9': ('READ-CLEAN', [], 'Exact eight applet session/contract/factory tuples read; WebAuthn is a client facade and deliberately separate.'),
 'd732bae8231ea804': ('READ-CLEAN', [], 'Full sealed/interface parity oracle read: declared public instance methods/properties checked against interface+inherited members with signature helper. No runtime or native behavior proof.'),
 '016797c23b38b006': ('READ-CLEAN', [], 'Full SameSignature compares names, generic arity, return shape, positional zipped parameter shape/names/optional/default, then parameter counts. No mismatched-length Zip false pass.'),
 '518a3341df9b9321': ('READ-CLEAN', [], 'Full recursive SameTypeShape read: generic parameter position and generic definition+arguments, else exact type identity. Helper context does not model every nullable annotation.'),
 '77ee50d694a6b6f0': ('READ-CANDIDATE', ['C009'], 'Complete allowlist and consumer ValidateOperation read; protected EnsureSupports entries are unreachable under public declared-only enumeration. This is stale context, not exposed enforcement API.'),
 '710d95782eb3fec4': ('READ-CLEAN', [], 'Full method and ValidateOperation context read. Checks Task, Async suffix, final defaulted token for selected declared applet ops/extensions; excludes disposal/special names. It is grammar, not invocation responsiveness.'),
 'b1c5a44497b2bdbb': ('READ-CANDIDATE', ['C009'], 'Full ValidateFactory and callers read: direct first connection parameter type not checked when receiverType null. Return/three-parameter/options/token assertions do not prove complete factory contract.'),
}

identities = []
rows = []
all_ids = PACKET['unit_ids'] + PACKET['shared_read_unit_ids']
assert len(all_ids) == len(set(all_ids))
assert set(NOTES) == {pathlib.Path(p).name for p in PACKET['paths']}
assert set(SHARED_NOTES) == {i.rsplit(':', 1)[1] for i in PACKET['shared_read_unit_ids']}
paths = sorted({UNITS[i]['path'] for i in all_ids})
for path in paths:
    raw = (ROOT / path).read_bytes()
    pinned = git('show', f'{PIN}:{path}')
    normalize = lambda b: b.decode('utf-8-sig').replace('\r\n', '\n').encode()
    assert normalize(raw) == normalize(pinned), path
    identities.append({'path': path, 'scope': 'COMPLETE-FILE' if path in PACKET['paths'] else 'ASSIGNED-SHARED-RANGES-WITH-CONTEXT',
      'current_raw_sha256': sha(raw), 'pinned_git_blob': git('rev-parse', f'{PIN}:{path}').decode().strip(),
      'pinned_git_content_sha256': sha(pinned), 'current_lf_sha256': sha(normalize(raw)),
      'pinned_lf_sha256': sha(normalize(pinned)), 'normalized_equal': True,
      'semantic_context': NOTES.get(pathlib.Path(path).name, 'See exact assigned shared-unit annotations and full-context report.')})
for uid in all_ids:
    unit = dict(UNITS[uid]); path = unit['path']; raw = (ROOT / path).read_bytes()
    text = raw.decode('utf-8-sig'); fragment = text[unit['start_character']:unit['end_character_exclusive']]
    assert sha(raw) == unit['source_sha256'], (uid, 'source')
    assert sha(fragment.encode()) == unit['range_sha256'], (uid, 'range')
    is_shared = uid in PACKET['shared_read_unit_ids']
    if is_shared:
        status, candidates, reason = SHARED_NOTES[uid.rsplit(':',1)[1]]
    else:
        candidates = [candidate for candidate, spans in RANGES.items() if any(
          pathlib.Path(path).name == name and unit['first_line'] <= last and unit['last_line'] >= first
          for name, first, last in spans)]
        status = 'READ-CANDIDATE' if candidates else 'READ-CLEAN'
        reason = NOTES[pathlib.Path(path).name]
    unit.update(assignment='shared' if is_shared else 'primary', read_status='FULL-ASSIGNED-UNIT-READ-WITH-CONTEXT',
      status=status, candidate_ids=['CORE-B5-'+c for c in candidates], semantic_context=reason,
      current_source_sha256=sha(raw), current_range_sha256=sha(fragment.encode()),
      source_preserved=True, review_status='PENDING-INDEPENDENT-REVIEW', validation_status='VALID-NOT-CHECKED',
      sample_status='ELIGIBLE-AFTER-TRIAGE-IF-UNCHANGED' if status == 'READ-CLEAN' else 'NOT-CLEAN-SAMPLE',
      product_edit_disposition='UNCHANGED; fresh editor must explicitly retain or correct related passage')
    rows.append(unit)

save('unit-dispositions.json', rows)
save('file-identities.json', identities)
save('file-dispositions.json', [dict(path=p, status='FULL-READ-CANDIDATE' if any(r['candidate_ids'] for r in rows if r['path']==p) else 'FULL-READ-CLEAN',
  semantic_context=NOTES[pathlib.Path(p).name], unit_ids=[r['id'] for r in rows if r['path']==p]) for p in PACKET['paths']])

extra = {
 'C001': ['src/Core/README.md','src/Core/CLAUDE.md','docs/SDK-HOUSE-STYLE.md','docs/architecture/raw-access-tiers.md','docs/migration/v1-to-v2.md','docs/v1-to-v2-comparison.md','docs/v2-highlights.md','src/SecurityDomain/src/SecurityDomainSession.cs','src/Management/src/ManagementSession.cs'],
 'C002': ['src/Core/README.md','src/Core/CLAUDE.md','docs/architecture/raw-access-tiers.md','src/Ctap2/src/Backend/HidBackend.cs','src/WebAuthn/src/Client/CeremonyDeadline.cs'],
 'C003': ['src/Core/README.md','docs/architecture/raw-access-tiers.md'],
 'C004': ['src/Core/README.md','src/Core/CLAUDE.md','docs/architecture/raw-access-tiers.md','src/Core/src/Transports/Hid/IOtpHidConnection.cs'],
 'C005': ['docs/MEMORY-MANAGEMENT.md','docs/usage/user-interaction.md','src/Ctap2/src/Backend/HidBackend.cs','src/YubiOtp/src/Backend/HidBackend.cs','src/Core/src/Transports/Hid/IOtpHidConnection.cs'],
 'C006': ['src/Core/README.md','src/Core/CLAUDE.md','docs/architecture/raw-access-tiers.md','docs/usage/user-interaction.md','src/Core/src/Abstractions/IProtocol.cs'],
 'C007': ['docs/architecture/raw-access-tiers.md','src/YubiOtp/src/Backend/HidBackend.cs','src/Core/src/Protocols/Otp/Hid/IOtpHidProtocol.cs'],
 'C008': ['src/Core/README.md','src/Core/CLAUDE.md','docs/architecture/raw-access-tiers.md'],
 'C009': ['docs/migration/v1-to-v2-map.yml','docs/migration/v1-to-v2-changelog.md','src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/FactoryShapeTests.cs','src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/AsyncSurfaceConventionTests.cs']
}
index=[]
for candidate in RANGES:
    cid='CORE-B5-'+candidate; affected=[r for r in rows if cid in r['candidate_ids']]
    unchanged=sorted({r['path'] for r in affected}|set(extra[candidate]))
    for path in unchanged: assert (ROOT/path).exists(), path
    index.append({'id':cid,'canonical_accepted_id':None,'unit_ids':[r['id'] for r in affected],
      'candidate_associated_unchanged_paths':[{'path':p,'decision':'PRESERVE-BYTES; fresh editor/reviewer must retain or correct exact claim under candidates.md trigger',
      'scope':'assigned B5 unit context' if p in paths else 'narrow dependency / future owner; no whole-file disposition'} for p in unchanged],
      'documentation':'CANDIDATE-TRIAGE','implementation':'NOT-FIXED; decisions in candidates.md','qualification':'NO-NEW-RUNTIME-PROOF','independent_review':'PENDING'})
save('candidate-index.json',index)

inputs=load(RUN/'inventory/inherited-packet-inputs.json')['B5-session-framing']
registry=load(RUN/'inherited-findings/registry.json')
inherited=[]
for value in registry.values():
    if not isinstance(value,list):continue
    for row in value:
        if not isinstance(row,dict) or row.get('key') not in inputs['provenance_keys']:continue
        group=row['dedup_group']
        observation={
          'fido-config-initialization':'Confirmed exact local Configure/awaited Prepare INIT separation; raw lazy initialization and latent OTP sync Configure remain distinct. No runtime execution.',
          'oath-terminal-reset':'Confirmed Core terminal disposal/borrowed connection mechanics and narrow OATH Reset success call site; historical serial1256A88 stopped before RESET. OATH boundary retains full reset qualification.',
          'otp-hid-framing':'Full framing/assertion review supports outbound64+slot+CRC+filler, status progseq, one abort and reset latch; C005/C007/C008 retain received-copy, malformed/custom/cadence and oracle qualifications.',
          'scp-effective-version':'Confirmed raw/effective/unknown/assumed distinctions and major-zero policy. SD reset omits config/version; raw HID inherited version remains0. C001 plus A-C003/C017/C021. B6 owns SCP floors/wire; applets own policy.',
          'shared-exchange-ownership':'Full Core bind/borrow/own/disposal/notification trace complete. Guard drains logical exchanges, not complete public workflows; C004/C005/C006/C008. Each applet retains its own response/profile/cleanup and physical qualification.'
        }[group]
        inherited.append({'key':row['key'],'group':group,'original_state_preserved':row['current_state'],
          'original_conclusion':row['current_conclusion'],'b5_observation':observation,
          'reopen_paths':row['documentation']['reopen_paths'],'xml_reopen_paths':row['documentation'].get('module_xml_reopening_paths',[]),
          'sample_test_reopen_paths':row['documentation'].get('sample_test_reopening_paths',[]),
          'trigger':row['documentation']['precise_trigger'],'status':'CORE-MECHANISM-READ; BOUNDARY/B6-DEPENDENCIES-REMAIN','fresh_runtime':False})
assert {r['key'] for r in inherited} == set(inputs['provenance_keys'])
save('inherited-dispositions.json',inherited)

nominee_labels = [
 ('ApplicationIdsTests.cs','Identifier_WritingThroughRecoveredArray'),
 ('ApplicationSessionDisposalTests.cs','CapabilityQueries_AfterDisposal'),
 ('RawSmartCardNativeBoundaryTests.cs','BorrowedRawSession_CancelDuringInitialNativeTransmit'),
 ('RawSessionConstructionFailureTests.cs','CreateRawCtapHidSession_WhenProtocolLoggerConstructionThrows'),
 ('ProtocolFactoryTests.cs','Create_MapsSupportedConnectionTypes'),
 ('OtpHidProtocolTests.cs','FailedAbandonmentReset_WhenWarningLoggerThrows'),
 ('OtpHidProtocolTests.cs','FailedResetAfterCompleteDataResponse'),
 ('SessionCreationOptionsTests.cs','WithPreferredConnectionType_CopiesEveryOption'),
 ('UserPresenceNotificationTests.cs','RequestFailure_PropagatesWithoutResolution'),
 ('RawSmartCardSessionTests.cs','CreateAsync_WithScp_ConfiguresBaseProcessor'),
 ('ChecksumUtils.cs','CalculateCrc'),
 ('ApplicationSessionDisposalTests.cs','DisposeAsync_ThrowingProtocol_ReleasesOwnedConnectionAsynchronously')
]
nominees=[]
for filename,needle in nominee_labels:
    selected=[r for r in rows if pathlib.Path(r['path']).name==filename and needle in r['label'] and r['status']=='READ-CLEAN']
    for r in selected:
        nominees.append({'id':r['id'],'path':r['path'],'label':r['label'],'source_sha256':r['source_sha256'],
         'range_sha256':r['range_sha256'],'reason':r['semantic_context'],'independent_sample':'NOT-PERFORMED; remove later edits/context overlaps'})
save('clean-nominees.json',nominees)
reuse=[]
for path in ['investigation/B1/sources/Task-v10.0.0.cs','sources/python/manifest.json','sources/python/context.md','inherited-findings/registry.json','sources/ownership.md']:
    actual=RUN/path
    reuse.append({'path':str(actual.relative_to(ROOT)),'raw_sha256':sha(actual.read_bytes()),'role':'existing immutable/reference source; read-only reuse, no duplicate capture'})
python_root=pathlib.Path('/home/rupicola/Code/y/yubikey-manager')
python_pin='4ca60f706af930459138d8dc0f0f953480e1c7a4'
python_path='yubikit/core/otp.py'
data=subprocess.check_output(['git','show',f'{python_pin}:{python_path}'],cwd=python_root)
reuse.append({'repository':str(python_root),'pin':python_pin,'path':python_path,'read_scope':'COMPLETE-FILE via git show; no package import',
 'raw_sha256':sha(data),'git_blob':subprocess.check_output(['git','rev-parse',f'{python_pin}:{python_path}'],cwd=python_root).decode().strip(),
 'immutable_url':f'https://raw.githubusercontent.com/Yubico/yubikey-manager/{python_pin}/{python_path}'})
(HERE/'sources/reuse-identities.json').write_text(json.dumps(reuse,indent=2)+'\n')
summary={'source_baseline':PIN,'observed_head':git('rev-parse','HEAD').decode().strip(),
 'packet_sha256':sha((RUN/'inventory/packets/B5-session-framing.json').read_bytes()),
 'primary_files':len(PACKET['paths']),'primary_lines':sum(len((ROOT/p).read_text().splitlines()) for p in PACKET['paths']),
 'primary_units':len(PACKET['unit_ids']),'shared_units':len(PACKET['shared_read_unit_ids']),
 'counts':dict(collections.Counter(r['status'] for r in rows)),
 'primary_counts':dict(collections.Counter(r['status'] for r in rows if r['assignment']=='primary')),
 'shared_counts':dict(collections.Counter(r['status'] for r in rows if r['assignment']=='shared')),
 'unread':0,'local_candidates':len(index),'accepted_allocations':0,'clean_nominees':len(nominees),
 'source_and_range_hashes_match':True,'pinned_normalized_paths_match':len(identities),
 'framework_research':'Context7 orientation plus .NETv10.0.0 originals; no runtime experiment',
 'sdk_builds':0,'sdk_tests':0,'native_probes':0,'device_operations':0,'formatter_runs':0,'subagents':0,
 'compiler_examples':'UNCOMPILED / VALID-NOT-CHECKED','independent_review':'PENDING',
 'completion':'PACKET-INVESTIGATION-HANDOFF-READY; not whole-Core or applet boundary closeout'}
save('coverage-summary.json',summary)
save('preservation-verification.json',{'passed':True,'checks':['exact43primarypaths','exact893primaryand39sharedIDs','allrawandrangesmatchinventory','allselectedpathsLFmatchsourcepin','all43authoredcontextnotes','all39sharedannotations','all15inheritedkeys','allcandidatepathsExist'],
  'note':'Static record reconciliation supplies no additional semantic coverage or SDK execution.'})
print(json.dumps(summary,indent=2))
