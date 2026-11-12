"""Join authored boundary annotations to frozen IDs; static source/hash work only.

Run from the repository with python3 -B. Writes only this boundary directory.
It imports no SDK, provider or external package and performs no network/device work.
Semantic conclusions are the authored notes below and companion Markdown, not hashes.
"""
import collections
import hashlib
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[9]
RUN = ROOT / 'docs/research/documentation-audit/modules/core/pass-1/core-001'
OUT = Path(__file__).resolve().parent
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
SD = 'src/SecurityDomain/'
UNIT = SD + 'tests/Yubico.YubiKit.SecurityDomain.UnitTests/'
INTEG = SD + 'tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/'

def sha(b):
    return hashlib.sha256(b).hexdigest()

def load(p):
    return json.loads(p.read_bytes())

def save(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT,
                                   env={**os.environ, 'GIT_OPTIONAL_LOCKS': '0'})

# Each complete file's authored conclusion includes its meaningful cleanup/assertion context.
NOTES = {
    'CLAUDE.md': 'Read complete contributor flow and examples. Current shallow facade/backend, exact selected catch and reset omission are accurate. Retain plaintext-pool cleanup scope without promising all temporaries; expand concrete contained parameter/first-MAC/effective-version and failed-reset disposal context. Key-reference log example is public metadata.',
    'PACKAGE_README.md': 'Read complete package task/alpha/install/link text. Correctly calls reset destructive and says completion does not verify factory contents. No lifetime or universal conformance assurance added; retain unchanged.',
    'README.md': 'Read all complete tasks and nearby prerequisites. Default03 and rotation source/finally scopes are useful. SCP11 creation needs known-version condition beside the recipe; contained certificate/private-key identity and returned response-owner lifetime need actionable explanation. Keep draft and per-command authority qualifications, reset attempt limits and fresh-session workaround.',
    'src/Backend/ISecurityDomainBackend.cs': 'Read complete narrow interface. Select and Send do not own connection/protocol lifetime or add policy. Send carries throwOnError/token, response owner retained only until facade strips it. Keep forwarding contract; X003/X007 are caller consequences.',
    'src/Backend/SecurityDomainBackend.cs': 'Read complete implementation. Fixed SecurityDomain AID delegates selection to current protocol; Send forwards command/status/token. Rebound after wrapper changes. No applet authentication, independent lease, retry or cleanup policy; retain unchanged implementation and accurate forwarding explanation.',
    'src/CaIdentifier.cs': 'Read complete record/XML. Parsed reference and independent copied identifier value are public metadata, not cert/trust proof. Exposed ROM constructor can also accept caller memory; no immutability or secret-erasure promise present. Retain XML; malformed parser policy stays original SD-IMP-004.',
    'src/CaIdentifierType.cs': 'Read complete enum. Flags select KLOC=1/KLCC=2/None=0; None is not valid retrieval request. No extra Core transport/profile inference. Supplemental disposition path has no assigned unit; full file read retained.',
    'src/ISecurityDomainSession.cs': 'Read all XML/contracts including inventory-omitted GET DATA/recognition/CA/STORE/reset declarations. Correct draft permissions, trusted certificate/caller disposal and partial-reset limitations remain. Clarify hidden response ROM, zero Le/format bounds, logical versus physical STORE, a/c phase and reset recovery without endorsing defects.',
    'src/IYubiKeyExtensions.cs': 'Read both complete extensions. SmartCard-only resolution and copied settings, one open/no fallback, borrowed direct creation then OwnConnection transfer, await-using one-shot list. Caller parameters remain borrowed; prompt not forwarded. Existing selected-wrap/ownership XML is retained, refined version/failed-reset task condition referred.',
    'src/KeyInfo.cs': 'Read both records/XML. Component tag/value and reference/list are parsed public metadata, no key bytes/raw entry. List does not claim deep immutable ownership. Keep accurate definitions; original parser/raw-escape policy retained SD-IMP-004.',
    'src/PublicAPI.Shipped.txt': 'Read whole nullable-enable empty shipped API snapshot. Supplemental bookkeeping only, not a substantive clean nominee or evidence of compilation.',
    'src/PublicAPI.Unshipped.txt': 'Read complete snapshot, reconcile session/interface operations, factory options, extension/static representations, feature descriptors, data records and exception constructors/status against source. Compiler-generated clone/equality/accessor entries retained as API context, not invented authored prose or compiled binding proof.',
    'src/SecureChannelException.cs': 'Read complete type/constructors/XML/message helper. Immediate Apdu status only, null inner guarded; default message embeds inner.Message. Selected factory/reset wrapping is accurate. Keep XML unchanged; diagnostics/provider provenance and nested NotSupported status consequence recorded.',
    'src/SecurityDomainKeyMaterial.cs': 'Read complete checksum/component/KCV helpers and finally. Fixed-time compares; KCV validation minimum10, ignores version/trailing data. Pooled plaintext copy zeroed in finally; kcv/encrypted secret-derived temporaries separately unzeroed. AES/KCV source input initialized to01, IV cleared, provider disposed. Preserve SD-IMP-006/008; no execution or universal-zeroing claim.',
    'src/SecurityDomainSession.cs': 'Read all 955 lines. Prepare/select/configure/rebind precedes ready; retained exact SCP params, local assumed version and gate, ROM-only response helper, typed key imports, certificate partial cleanup, raw reset bypass and post-loop teardown/reinit. Existing parsed metadata/range limitations remain original SD findings, not new Core groups. Detailed tables and candidate anchors distinguish lifetimes and error phases.',
    'src/SecurityDomainTlvEncoding.cs': 'Read complete delete normalization/encoding. Both-zero input rejected before SCP03 component KID normalization; component+KVN0 can become empty filter. Nonzero fields encode D0/D2 through Core helper. Preserve SD-IMP-002; current public warning accurate. Retain source/no new boundary mechanism.',
    'src/Yubico.YubiKit.SecurityDomain.csproj': 'Read full project. Assembly/root namespace, module package description/tags, shared SDK/AOT opt-in and only Core project reference. No direct platform loader or test execution. Keep packaging unchanged.',
    'tests/CLAUDE.md': 'Read all examples/helper/setup/reset/firmware/oracle conditions. Owned default parameters enclose session helper; multi-session example has independent60-second cleanup and labels persistence not auth. Existing scopes/limits useful. Reopen version/failed reset and actual test adequacy explanations only as mapped; examples uncompiled.',
    'tests/README.md': 'Read complete setup/project/runner/examples. Distinguishes hardware integration from unit, selected firmware floors and destructive reset attempts. Owned parameter example correct; reset helper opens fresh session with config/version. Strengthen coverage/resource conditions without claiming all native or physical qualification.',
}
NOTES.update({
    'tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/Helpers/Scp11TestData.cs': 'Read complete public PEM/base64 PKCS12/password data. Source bytes kept unchanged; no container decrypted/imported. Reused B6 public DER serial observation is qualified separately. This supplemental file hands future fixture-oracle work to SD-IMP-007/008 owner, not to Core key mathematics.',
    'tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/Helpers/ScpCertificates.cs': 'Read full name/key-usage sorter, root/leaf extraction and leftover exception. Identity references passed through, no trust verification/disposal owner. Root matches issuer/subject only. Caller helpers own loaded objects and failure cleanup; SD-IMP-008, X008. Supplemental path read fully.',
    'tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_NegativeTests.cs': 'Read both full negative scenarios. Wrong b public key after generation and wrong03 after reset each accept any Exception; cannot identify authentication phase/cause. b Track base cleanup,03 no Track; static CTS and inline defaults/wrappers have separate lifetime gaps. Wrong StaticKeys scoped. No execution.',
    'tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp03KeyLifecycleTests.cs': 'Read complete seven-session import/delete and four-session rotation. Inner APDU wrapper assertions, retained/deleted version listing, fresh rotated nonnull factory checked. Scoped static clones; wrappers/inline defaults/source bytes not all disposed/cleared. Local finally same possibly canceled token plus Track independent base cleanup; no final factory-state assertion.',
    'tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp03Tests.cs': 'Read full factory/list/plain reset/import/recognition/GET/CA tests. Count3/4+FF is factory expectation, not universal; reset test plain only; fresh import factory nonnull and old reference APDU inner. CA wire regression constrains KID11/13/15 and nonempty identifier. Cleanup/inline defaults/tokens and Track vary per method; see test-oracles.',
    'tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp11Tests.cs': 'Read all six tests and four provisioning/parser helpers. Generated P-256 point/provider import plus later plain persistence; a admitted chain not negative allowlist proof; b import flag only; b authority broad APDU over Generate+Delete; certificate count/thumbprint not trust. PKCS12 D clone/omitted roots/PEM partial load/GetSki view have separate cleanup, several providers/certs/params undisposed.',
    'tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp11cTests.cs': 'Read complete c test and provisioning/PKCS12/PEM/SKI helpers. Admin setup over03, later c flag and protected public key listing. Listing no privilege proof. Name sorter/trust, exported D clone, selected/omitted certificate owners and undisposed provider/params/TLV same caller cleanup questions as a; no crypto/device execution.',
    'tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/SecurityDomainStateRestoringTests.cs': 'Read full Track and IAsyncLifetime. One tracked state, independent60-second cleanup source, no cleanup if untracked; reset remains attempt not verified contents. Supplemental full read, accurate remarks retained. Token does not bound admitted native work.',
    'tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs': 'Read complete helper and reset extension. Await shared WithConnection callback, separate lexical resetSession scope then session scope. Config/version passed each fresh factory; params borrowed and action token not injected. Same connection sequential reuse, caller-owned defaults must enclose call. Raw reset reinit loss not repaired by first session itself.',
    'tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/Yubico.YubiKit.SecurityDomain.IntegrationTests.csproj': 'Read full project: net10 xUnit2/TestSdk/skippable/traditional runner, shared/Core/SD refs, runner config copy. No new runtime/runner claim; retain configuration context.',
    'tests/Yubico.YubiKit.SecurityDomain.UnitTests/AuditV2/SecurityDomainAuditReproTests.cs': 'Read both full YESDK-1634 tests. Empty collection produces no transmit; explicit clear sends one E2 and exact A6/ref/empty70. Consequential restriction-removal wire oracle, no final/vendor counter or device claim. Await-using session, fake caller scope separate.',
    'tests/Yubico.YubiKit.SecurityDomain.UnitTests/Scp03HandshakeFakeConnection.cs': 'Read complete scripted seven-step fixture and independent CMAC implementation. Derives challenge-dependent keys/one9000 reply, accepts host cryptogram/MAC without validation, no generic simulator. Dispose clears static keys only; derived secret arrays/captures separate. Exact useful scenario and KAT limits already explained. Wrapper regression not final SCP conformance.',
    'tests/Yubico.YubiKit.SecurityDomain.UnitTests/Scp03HandshakeFakeConnectionTests.cs': 'Read complete four RFC4493 §4 length0/16/40/64 tuples and whole-tag equality. Public source KAT data, primitive only, not KDF/handshake or executed proof. Retain substantive clean nominee.',
    'tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecureChannelExceptionTests.cs': 'Read all six full tests. Raw ready3 transmissions,03 rejected status, local cryptogram mismatch, explicit too-old version, later malformed-list unwrapped, and scripted secure reset reinit rejection/false flags. Complete assertion scope documented accurately. Missing fake disposal and state owner gaps retained X008/B6, not implied proof from wrapper.',
    'tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecurityDomainCertificateBundleTests.cs': 'Read complete real-loader observer tests/helper. Corrupt later cert disposes first two actual handles, success preserves order/identity/DER through session disposal then caller finally clears handles; ECDsa/certificate helper scoped. Exact-scope historical consideration-5 resolution supported in source; not executed or trust validation.',
    'tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecurityDomainSessionTests.cs': 'Read all745 lines including parser-omitted wire/reset/guard tests. Factory nonnull versus actual wire/status/flags/detachment oracles distinguished; convenience disposal observes transmit only. Complete assertions/cleanup listed in test-oracles. Recording/no-hardware objects cannot prove native release, active cancellation or complete config propagation.',
    'tests/Yubico.YubiKit.SecurityDomain.UnitTests/Yubico.YubiKit.SecurityDomain.UnitTests.csproj': 'Read full net10 executable xUnit3/MTP project, NSubstitute/logging DI refs and shared SD/test refs. No compiler/analyzer execution inferred; retain configuration context.',
})

# Localized candidate anchors; an overlap associates a unit, not every line as defective.
ANCHORS = []
def anchor(candidate, path, first, last, reason):
    ANCHORS.append(dict(candidate=f'CORE-SD-X{candidate:03}', path=SD+path,
                        first_line=first, last_line=last, reason=reason))

for n, path, a, b, why in [
    (1,'README.md',126,152,'SCP11 task lacks immediate required effective-version creation example'),
    (1,'README.md',202,207,'Retain accurate assumed/explicit version conditions; add setup gate consequence'),
    (1,'src/SecurityDomainSession.cs',125,205,'Actual override/default -> Core gate; local config and version not retained'),
    (1,'src/SecurityDomainSession.cs',765,799,'Both reset callbacks omit config/version, retain exact SCP parameters'),
    (1,'CLAUDE.md',130,147,'Local setup/version provenance and selected catch must stay together'),
    (2,'README.md',215,225,'Contained private-key/certificate identity needs actionable ownership wording'),
    (2,'CLAUDE.md',123,128,'Replace unverified deep ownership caveat with source-qualified containment details'),
    (2,'src/SecurityDomainSession.cs',83,101,'Exact retained caller parameter object and borrowed connection'),
    (2,'src/SecurityDomainSession.cs',608,617,'Caller-owned private D alias, distinct encrypted copy'),
    (2,'src/SecurityDomainSession.cs',646,652,'Curve byte stackalloc uninitialized; source-quality qualification only'),
    (2,'src/SecurityDomainKeyMaterial.cs',32,108,'Pooled plaintext finally versus separate KCV/ciphertext/provider temporaries'),
    (3,'src/SecurityDomainSession.cs',816,823,'Facade strips response owner and exposes Data alias'),
    (3,'src/ISecurityDomainSession.cs',35,52,'Input borrow and result owner/clear policy absent; supplemental XML read'),
    (3,'README.md',215,225,'Public source/result cleanup responsibility needs hidden-owner qualification'),
    (4,'src/SecurityDomainSession.cs',765,936,'Raw attempts bypass guard; flags clear only after attempts/disposal, failure cleanup local'),
    (4,'src/ISecurityDomainSession.cs',312,332,'Add failed-reset disposal/reopen condition near existing destructive limits'),
    (4,'README.md',172,190,'Cancellation/failed-reset lifecycle and diagnostic completion boundaries'),
    (4,'CLAUDE.md',74,99,'Keep attempt count and fresh-session workaround, add stale graph/failure action'),
    (5,'src/SecureChannelException.cs',70,74,'Message embeds opaque immediate failure text; supplemental helper read'),
    (5,'CLAUDE.md',142,148,'Keep selected wrapper; useful nested status/local gate diagnosis'),
    (5,'CLAUDE.md',175,179,'Global logging and opaque exception provenance, no blanket privacy assurance'),
    (5,'src/SecurityDomainSession.cs',232,244,'GET DATA warning forwards exception object'),
    (5,'src/SecurityDomainSession.cs',485,489,'SCP03 import error forwards exception object'),
    (5,'src/SecurityDomainSession.cs',666,670,'EC import error forwards exception object'),
    (6,'README.md',30,37,'Add a/c first post-setup correct-MAC phase beside existing b/authority qualification'),
    (6,'README.md',137,163,'Retain draft permission/counter condition and trust role distinction'),
    (6,'src/ISecurityDomainSession.cs',25,31,'Setup flag versus device auth phase; draft final/profile scope'),
    (7,'src/ISecurityDomainSession.cs',35,52,'Zero Le and configured format limits; no absent-Le promise'),
    (7,'src/ISecurityDomainSession.cs',281,290,'One logical STORE DATA may physically chain through Core'),
    (7,'src/SecurityDomainSession.cs',208,228,'Common empty requestData/Le0 follows Core encryption/format policy'),
    (7,'src/Backend/SecurityDomainBackend.cs',32,36,'Forwarding dependency only, not a new local mechanism'),
    (8,'tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecureChannelExceptionTests.cs',167,192,'Unscoped borrowed fake and exact failed-reinit oracle, not device reset proof'),
    (8,'tests/Yubico.YubiKit.SecurityDomain.UnitTests/Scp03HandshakeFakeConnection.cs',68,75,'Fixture owns derived arrays separately from StaticKeys'),
    (8,'tests/Yubico.YubiKit.SecurityDomain.UnitTests/Scp03HandshakeFakeConnection.cs',104,109,'Dispose only static keys, not fixture derived buffers'),
    (8,'tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecurityDomainSessionTests.cs',24,50,'DisposeCount/no synchronous Dispose is narrower than complete disposal observer'),
    (8,'tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecurityDomainSessionTests.cs',217,435,'Nonnull/config inputs or transmit assertion do not verify all propagated settings/disposal'),
]:
    anchor(n,path,a,b,why)

for path in [p for p in NOTES if 'IntegrationTests/SecurityDomainSession_' in p]:
    anchor(8,path,1,len((ROOT/SD/path).read_bytes().decode('utf-8-sig').splitlines()),
           'Actual test/provider/parameter/token/cleanup scope; full oracle record supplied')
for path in ['tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/Helpers/ScpCertificates.cs',
             'tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/Helpers/Scp11TestData.cs']:
    anchor(8,path,1,len((ROOT/SD/path).read_bytes().decode('utf-8-sig').splitlines()),
           'Supplemental complete fixture read; exact caller trust/cleanup owner SD-IMP-007/008')
anchor(6,'tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp11cTests.cs',61,70,
       'Protected public listing after flag is not an administrative-authority oracle')

NOMINEES = {
    'core-001:unit:aa6d02c22a70d020': 'Real loaded certificate handles, corrupt later item stops iteration, all accumulated handles disposed, failure finally retained.',
    'core-001:unit:52c46bed2ec7d440': 'Exact owner identities/order/DER survive session disposal; caller finally disposes certificates and handles become zero.',
    'core-001:unit:de9d48abdf1759d5': 'Full published RFC4493 primitive vectors and exact complete tag equality, not length-only/self-consistency.',
    'core-001:unit:67a2df596ddf1fd8': 'Two GET DATA requests independently recorded in KLOC/KLCC order and exact reference/identifier association asserted.',
    'core-001:unit:58172ff4d770bcd1': 'Consequential explicit removal wire oracle: exactly one STORE DATA plus A6/ref and zero-length70.',
    'core-001:unit:f952a6db7aa67162': 'Complete typed owned-parameter/two-session persistence example with independent cleanup token and truthful reset limit.',
    'core-001:unit:fab93bc1d63af27a': 'Public certificate result contract names caller lifetime, independent session disposal and absent trust/order validation, matching partial failure implementation.',
}

def main():
    packet = load(RUN/'inventory/packets/BOUNDARY-SecurityDomain.json')
    units = {u['id']:u for u in load(RUN/'inventory/semantic-units.json')}
    assert set(NOTES) == {p.removeprefix(SD) for p in packet['disposition_paths']}
    files = []
    for p in packet['disposition_paths']:
        raw=(ROOT/p).read_bytes()
        pinned=git('show',f'{PIN}:{p}')
        norm=lambda x: x.decode('utf-8-sig').replace('\r\n','\n')
        assert norm(raw)==norm(pinned), p
        files.append(dict(path=p,sha256=sha(raw),pinned_sha256=sha(pinned),
                          git_blob=git('rev-parse',f'{PIN}:{p}').decode().strip(),
                          normalized_pin_equal=True,raw_pin_equal=raw==pinned,
                          read_scope='COMPLETE-FILE, meaningful assertions/helpers/cleanup and full examples',
                          note=NOTES[p.removeprefix(SD)],
                          candidates=sorted({a['candidate'] for a in ANCHORS if a['path']==p}),
                          assigned_unit_ids=[i for i in packet['unit_ids'] if units[i]['path']==p],
                          candidate_associated_unchanged_reason='Records-only assignment. Preserve all product bytes; fresh editor rechecks exact mapped XML/comment/task, separately scoped implementation/test owner handles behavior.' if any(a['path']==p for a in ANCHORS) else 'Complete read found no additional boundary documentation correction in this file outside expressly retained dependency context; preserve current bytes.',
                          handoff_owner='Security Domain fresh editor/test owner after coordinator triage; Core mechanisms remain B5/B6/C',
                          supplemental= p not in packet['paths']))
    results=[]
    for i in packet['unit_ids']:
        u=units[i]
        raw=(ROOT/u['path']).read_bytes()
        text=raw.decode('utf-8-sig',errors='replace')
        body=text[u['start_character']:u['end_character_exclusive']]
        assert sha(raw)==u['source_sha256'],i
        assert sha(body.encode())==u['range_sha256'],i
        hit=[a for a in ANCHORS if a['path']==u['path'] and a['first_line']<=u['last_line'] and a['last_line']>=u['first_line']]
        context=u['category'] in ('type-contract','configuration-data-or-support-contract')
        results.append({**u,'status':'READ-CANDIDATE' if hit else 'READ-CONTEXT' if context else 'READ-CLEAN',
                        'review_status':'INDEPENDENT-REVIEW-PENDING','sample_status':'NOMINEE' if i in NOMINEES else 'NOT-SAMPLED',
                        'source_pin':PIN,'current_source_sha256':sha(raw),'current_range_sha256':sha(body.encode()),
                        'read_context':NOTES[u['path'].removeprefix(SD)],
                        'localized_observations':hit or [dict(reason='Complete unit retained in the specific file mechanism/assertion context above. No additional boundary correction identified; existing applet policy/implementation qualifications remain separate.',label=u['label'])],
                        'candidates':sorted({a['candidate'] for a in hit}),
                        'unchanged_decision':'Preserve product bytes. Candidate anchors require fresh accepted editor/reviewer disposition; a nearby clean member does not become defective by file association.' if hit else 'Retain current unit for its scoped documented behavior/assertion, not runtime acceptance.',
                        'validation':'NO SDK, compiler, tests, provider, native or device execution; source/record verification only'})
    save('unit-dispositions.json',results)
    save('file-dispositions.json',files)
    save('localized-anchors.json',ANCHORS)
    save('clean-nominees.json',[{k:u[k] for k in ['id','path','first_line','last_line','source_sha256','range_sha256']} | dict(reason=NOMINEES[u['id']],review='PENDING; remove edited overlap',execution='UNRUN') for u in results if u['id'] in NOMINEES])
    candidates=[]
    for n in range(1,9):
        cid=f'CORE-SD-X{n:03}'
        aas=[a for a in ANCHORS if a['candidate']==cid]
        candidates.append(dict(id=cid,status='LOCAL-CANDIDATE-NOT-ACCEPTED',definition='candidates.md',
                               anchors=aas,unit_ids=[u['id'] for u in results if cid in u['candidates']],
                               unchanged_paths=sorted({a['path'] for a in aas}),
                               documentation='CANDIDATE, fresh edit/review pending',implementation='UNCHANGED; separate authorization',
                               decision='Explicit per-group policy choice in candidates.md; not accepted as intended defect contract',
                               qualification='SOURCE-ONLY; original edition/profile and platform/device bounds retained',
                               counting='Caller-specific association; Core mechanism and inherited aliases nonadditive'))
    save('candidate-index.json',candidates)
    registry=load(RUN/'inherited-findings/registry.json')
    keys=set(load(RUN/'inventory/inherited-packet-inputs.json')['BOUNDARY-SecurityDomain']['provenance_keys'])
    recommendations={
      'SD-DEP-001': ('SOURCE-INVESTIGATION-COMPLETE; selected-wrap/phase qualification retained','X005/X006; ready after callback, setup bool before draft a/c first correct-MAC phase; b no host authority.'),
      'SD-DEP-002': ('SOURCE-INVESTIGATION-COMPLETE; aliases/cleanup decision retained','X002/X003/X008; caller wrapper, exact contained keys/certs, reset retains reference; no deep clone or session disposal of caller inputs.'),
      'SD-DEP-003': ('SOURCE-REPRODUCIBLE-LOCAL-OMISSION; original SD-IMP-005 retained','X001/X004; reset omits config/version, clears explicit flag; Core completion centralization leaves omission; SCP11 can fail pre-handshake local gate.'),
      'SD-DEP-004': ('SOURCE-INVESTIGATION-COMPLETE; workflow/ROM/Le qualifications retained','X003/X004/X007; direct borrow versus hidden own, per-exchange not whole reset guard, hidden response owner and always-appended zero Le.'),
      'SD-DEP-005': ('SOURCE-INVESTIGATION-COMPLETE; stale initial formatter claim superseded','X005; current formatters contain no logs; PCSC ordinary trace headers/length only; opaque exception objects/messages remain distinct provider provenance.'),
    }
    inherited=[]
    for row in registry['rows']:
        if row['key'] not in keys: continue
        own=row['original_id'] in recommendations and row['module']=='security-domain'
        status,note=recommendations[row['original_id']] if own else ('REFERRED-TO-EXACT-APPLET-BOUNDARY-OWNER','Shared mechanism evidence applied only to actual SD flow here. Complete other-applet call-site disposition remains with its named boundary; no automatic closure or extra bug.')
        owner={'management':'Management','oath':'Oath','openpgp':'OpenPgp','piv':'Piv','yubiotp':'YubiOtp','yubihsm':'YubiHsm'}.get(row['module'],row['module'])
        inherited.append(dict(key=row['key'],dedup_group=row['dedup_group'],original_state=row['current_state'],
                              original_authority=row['authority'],original_definition=row['historical_definition'],
                              original_reopening_map=row['documentation'],
                              original_aliases=row['aliases_or_related_ids'],boundary_status=status,recommendation=note,
                              owner='SecurityDomain/coordinator triage' if own else f'BOUNDARY-{owner}; Core mechanism owner B5/B6/C',
                              original_conclusion=row['current_conclusion'],qualification='No authoritative registry change, acceptance, implementation or device closure. DEC-007 rationale remains gap; PIV physical deferrals remain.'))
    save('inherited-dispositions.json',dict(assigned=inherited,
        local_original_aliases={
          'SD-IMP-001':'X004: incomplete/empty/exhausted reset and no post-state verification retained',
          'SD-IMP-002':'Original SD owner: component/zero normalized empty delete filter; public warning accurate, no duplicate Core candidate',
          'SD-IMP-003':'X006/X007: serial/counter draft/profile qualification; no new final conformity conclusion',
          'SD-IMP-004':'Original SD owner: malformed/trailing metadata parser/open raw result policy; XML already qualified',
          'SD-IMP-005':'X001/X004: omitted reset config/version persists',
          'SD-IMP-006':'Original SD owner with X007 wire dependency: ranges/truncation/KCV version/trailing data, public limitations accurate',
          'SD-IMP-007':'X006/X008: actual test oracles and phase/authority limits',
          'SD-IMP-008':'X002/X003/X008: concrete key/response/fixture/helper cleanup and alias scopes'},
        additional_exact_scope=[dict(key='audit-v2-20261022-20261031:consideration-5',
          outcome='RESOLVED-SOURCE-FOR-EXACT-CERTIFICATE-PARTIAL-CLEANUP-SCOPE',
          evidence='SD GetCertificates303–339 and both complete CertificateBundleTests: actual accumulated handles disposed on later corrupt item; success transfers identity/order and survives session disposal.',
          qualification='No fresh execution. Distinct from Scp11 parameter certificate alias disposal and integration helper leaks.')]))
    # Hash evidence actually read/reused; full-path identity is not whole dependency-file coverage.
    dependencies={
      'src/Core/src/Sessions/ApplicationSession.cs':'B5:1–135,203–569 binding/prepare/configure/ready/disposal; caller context only',
      'src/Core/src/Sessions/SessionCreationOptions.cs':'B5/C:complete options value/reference and shallow copy contracts',
      'src/Core/src/Devices/YubiKeyConnectionExtensions.cs':'B1/B5:170–305 actual resolver/single-open/failure cleanup',
      'src/Core/src/Protocols/ProtocolFactory.cs':'B5:complete SmartCard protocol route, no external injection',
      'src/Core/src/Devices/Feature.cs':'A/B5:complete major-zero compatibility policy',
      'src/Core/src/Protocols/SmartCard/Apdu/PcscProtocol.cs':'B6:complete selected plain graph/config/status/recovery route',
      'src/Core/src/Protocols/SmartCard/Apdu/PcscProtocol.Scp.cs':'B6:complete guarded SCP initialization capability',
      'src/Core/src/Protocols/SmartCard/Apdu/ProtocolConfiguration.cs':'B6:complete readonly struct flags/value-copy',
      'src/Core/src/Protocols/SmartCard/Apdu/ApduFormatterShort.cs':'B6:complete0..255/always Le',
      'src/Core/src/Protocols/SmartCard/Apdu/ApduFormatterExtended.cs':'B6:complete0..65536/always Le and max length',
      'src/Core/src/Protocols/SmartCard/Apdu/ApduResponse.cs':'B6:complete copied owner/status/Data/Clear',
      'src/Core/src/Protocols/SmartCard/Apdu/ChainedResponseReceiver.cs':'B6:complete fixed follow-up/accumulation/error path',
      'src/Core/src/Protocols/SmartCard/Scp/PcscProtocolScp.cs':'B6:complete effective graph/shared guard/disposal/status',
      'src/Core/src/Protocols/SmartCard/Scp/ScpInitializer.cs':'B6:complete feature/setup/outer cleanup and selected translation',
      'src/Core/src/Protocols/SmartCard/Scp/ScpState.Scp03.cs':'B6:complete partial-derivation owner transfer and failure scope',
      'src/Core/src/Protocols/SmartCard/Scp/ScpState.Scp11.cs':'B6:complete cert PSO/receipt/first-command gap/parameter use',
      'src/Core/src/Protocols/SmartCard/Scp/ScpProcessor.cs':'B6:complete encrypt/MAC/response cleanup/INS82 exception',
      'src/Core/src/Protocols/SmartCard/Scp/ScpState.cs':'B6:1–220 Encrypt counter mutation/DEK/copies; remaining mechanics B6 report reused',
      'src/Core/src/Protocols/SmartCard/Scp/Scp03KeyParameters.cs':'B6:complete exact contained StaticKeys owner',
      'src/Core/src/Protocols/SmartCard/Scp/Scp11KeyParameters.cs':'B6/C:complete reference/list/contained private and cert disposal',
      'src/Core/src/Protocols/SmartCard/Scp/StaticKeys.cs':'B6:complete clone/derive/DEK/session scopes',
      'src/Core/src/Utilities/ExchangeGuard.cs':'B5:complete entry cancellation, admitted None, close/drain',
      'src/Core/src/YubiKitLogging.cs':'C:complete static factory/temporary selection; no redaction',
      'docs/migration/v1-to-v2.md':'SHARED:119–120 and822–826 SD dependency only',
      'docs/migration/v1-to-v2-map.yml':'SHARED:securitydomain-exception unchanged dependency, original DOC-002 preserved',
      'docs/AI-DOCS-GUIDE.md':'SHARED:484–509 complete SD sketch, already correct destructive qualification',
    }
    evidence_paths=[RUN/'investigation/B6/sources'/f for f in ['final-source-register.md','final-manifest.json','scp03-1.1.2.pdf','scp03-1.1.2.txt','scp11-public-review-1.3.0.13.pdf','scp11-public-review-1.3.0.13.txt','scp03-catalogue.html','scp11-catalogue.html','yubico-profile.html']]
    evidence_paths += [RUN/'sources/python'/f for f in ['manifest.json','context.md','history/patch-manifest.json','history/focused-followup-manifest.json','snapshot/yubikit/core/smartcard/scp.py','snapshot/yubikit/core/smartcard/__init__.py']]
    evidence_paths += [RUN/f for f in ['investigation-A.md','investigation-B1.md','investigation-B2.md','investigation-B3.md','investigation-B4.md','investigation-B5.md','investigation-B6.md','investigation-C.md','plan.md','inherited-findings/registry.json','inventory/inherited-packet-inputs.json']]
    evidence_paths += [ROOT/f for f in ['AGENTS.md','CLAUDE.md','src/Core/CLAUDE.md','src/Core/tests/CLAUDE.md','docs/TESTING.md','docs/SDK-HOUSE-STYLE.md','docs/research/documentation-audit/workflow.md','docs/research/documentation-audit/records/style.md','docs/research/documentation-audit/modules/security-domain/pass-1/security-domain-001/findings.md']]
    dependency_records=[]
    for p,scope in dependencies.items():
        raw=(ROOT/p).read_bytes(); pinned=git('show',f'{PIN}:{p}')
        assert raw.decode('utf-8-sig').replace('\r\n','\n')==pinned.decode('utf-8-sig').replace('\r\n','\n'),p
        dependency_records.append(dict(path=p,sha256=sha(raw),pinned_sha256=sha(pinned),scope=scope,owner=scope.split(':')[0],unchanged_reason='Dependency-only referral; exact mechanism/shared-document owner retains full edit/review. No boundary write authorization.'))
    for p in evidence_paths:
        dependency_records.append(dict(path=str(p.relative_to(ROOT)),sha256=sha(p.read_bytes()),scope='REUSED-IMMUTABLE-EVIDENCE; exact independently read clauses/ranges versus contextual history identified in sources.md',owner='B6/C/shared source cache'))
    save('dependency-identities.json',dependency_records)
    # Exact shared-document referral scopes supplement direct source anchors.
    reopen=[]
    extras={
      1:[('src/SecurityDomain/src/IYubiKeyExtensions.cs','factory remarks35–41','Retain current warning; add immediate SCP11 effective-version requirement'),('src/SecurityDomain/src/ISecurityDomainSession.cs','Generate207–214 and Reset316–330','Retain accurate explicit-version/reset limitation; explain local pre-wire failure'),('docs/migration/v1-to-v2.md','factory row119–120 and SD822–826','Retain version source/wrapper mapping; reopen if reset retention or gate changes'),('src/Core/src/Sessions/SessionCreationOptions.cs','FirmwareVersionOverride XML46–52','SHARED/B5 review of SD only-source versus generation/setup gate condition')],
      2:[('src/SecurityDomain/src/IYubiKeyExtensions.cs','factory 35–41','Retain borrowed wrapper ownership; add concrete contained-owner cross-reference'),('src/Core/src/Protocols/SmartCard/Scp/Scp11KeyParameters.cs','constructors/properties/Dispose','B6/C primary owns containment/alias/clone XML correction'),('src/Core/src/Protocols/SmartCard/Scp/Scp03KeyParameters.cs','Keys/Default/Dispose','B6 primary owns exact StaticKeys reference disposal contract'),('src/SecurityDomain/tests/CLAUDE.md','owned parameter examples 78–147','Retain sound example scopes, do not imitate undisposed actual test inputs'),('src/Core/src/Sessions/SessionCreationOptions.cs','retained prompt XML 25–28,58–60','C/SHARED adds adoption condition: SD ignores supplied prompt and retains no service')],
      3:[('src/SecurityDomain/src/ISecurityDomainSession.cs','GET DATA35–52; certificates76–92','Add ROM lifetime/hidden clear-owner conditions; preserve accurate certificate transfer/trust text'),('src/Core/src/Protocols/SmartCard/Apdu/ApduResponse.cs','Data/Clear','B6 owns response copy/status owner; no unsafe writable cast recommendation'),('src/SecurityDomain/src/KeyInfo.cs','metadata record XML','Retain public metadata classification, not a secret owner'),('src/SecurityDomain/src/CaIdentifier.cs','identifier record XML','Retain value-only metadata/not trust wording')],
      4:[('src/SecurityDomain/tests/CLAUDE.md','reset details57–75 and examples','Retain two-session sequencing; add failed-reset recovery scope only if needed'),('src/SecurityDomain/tests/README.md','automatic reset90–104','Retain attempts/no-verification; qualify failed-session action'),('src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs','31–87','Preserve lexical reset-session disposal before next factory and parameter borrow; revisit comments if lifecycle changes'),('docs/AI-DOCS-GUIDE.md','SecurityDomain484–509','Already accurately destructive/partial/no-verified-state; retain unchanged unless policy changes'),('src/Core/src/Utilities/ExchangeGuard.cs','public-workflow versus exchange rationale','B5 primary retains mechanism; SD raw bypass is caller consequence')],
      5:[('src/SecurityDomain/src/SecureChannelException.cs','type/constructors/status19–74','Retain selected-wrap and immediate-status XML, review opaque Message policy separately'),('src/SecurityDomain/src/IYubiKeyExtensions.cs','factory exceptions43–46','Accurate selected catch; retain, reopen on changed set/phase'),('docs/migration/v1-to-v2-map.yml','securitydomain-exception scalar','Retain precise selected wrapping; no current edit warranted, preserve unrelated DOC-002'),('docs/migration/v1-to-v2.md','SecurityDomain822–826','Already accurate immediate-status/selection/cancellation boundary; retain'),('src/Core/src/YubiKitLogging.cs','factory selection/configuration','C/SHARED primary owns redaction/provider lifetime policy')],
      6:[('src/SecurityDomain/CLAUDE.md','SCP parameters123–128','Add a/c first-MAC phase beside existing b authority distinction'),('src/SecurityDomain/src/ISecurityDomainSession.cs','type25–31 and allowlist249–275','Retain draft restrictions; enrich a/c phase without final-profile upgrade'),('src/Core/src/Protocols/SmartCard/Scp/ScpState.Scp11.cs','handshake40–44/100–151','B6 owns setup-ready wording and final/draft citation correction'),('docs/migration/v1-to-v2.md','SecurityDomain822–826','Retain b actor distinction; reopen on changed auth contract')],
      7:[('src/SecurityDomain/src/SecurityDomainSession.cs','GetData208–245/StoreData727–735','Observed command fields unchanged; XML inherits exact method contract'),('src/Core/src/Protocols/SmartCard/Apdu/ApduFormatterShort.cs','Format22–56','B6 owns bounds/zero-Le semantics, no new SD mechanism'),('src/Core/src/Protocols/SmartCard/Apdu/ApduFormatterExtended.cs','Format25–68','B6 owns max/Le encoding; SD consumes reviewed outcome'),('src/Core/src/Protocols/SmartCard/Scp/ScpProcessor.cs','empty80/response133–166','B6 owns empty/profile/chaining fix decision; SD normal instructions exclude post-setup82')],
      8:[('src/SecurityDomain/tests/README.md','test project/coverage/setup and cleanup','Retain no-hardware versus hardware distinction; sharpen actual oracle/resource scope'),('src/SecurityDomain/tests/CLAUDE.md','factory oracle36–48 and reset57–75','Existing limits accurate; update only after real fixture/assertion/harness changes'),('src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/SecurityDomainStateRestoringTests.cs','19–58','Retain independent60-second Track-only cleanup and attempted restoration wording'),('src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/Scp03HandshakeFakeConnectionTests.cs','17–46','Retain published primitive-only scope and full tag equality; do not promote to handshake conformance')],
    }
    triggers={1:'Effective version/gate/reset option retention changes',2:'Key/certificate containment, aliases or temporary cleanup changes',3:'Facade response/result ownership or cleanup changes',4:'Reset raw admission/status/readiness/reinit/recovery policy changes',5:'Exception translation/nesting or logging/provider policy changes',6:'Auth completion/profile/command authority or trust provisioning changes',7:'B6 Le/empty/encryption/chaining/status behavior changes',8:'Actual tests, fixture, assertions, tokens or restore harness changes'}
    for n in range(1,9):
        cid=f'CORE-SD-X{n:03}'
        for a in [a for a in ANCHORS if a['candidate']==cid]:
            reopen.append(dict(candidate=cid,path=a['path'],section=f"{a['first_line']}–{a['last_line']}",
              outcome='UNCHANGED; records-only assignment',reason=a['reason'],trigger=triggers[n],owner='Fresh SD editor/reviewer or scoped test owner after triage'))
        for path,section,reason in extras[n]:
            reopen.append(dict(candidate=cid,path=path,section=section,outcome='UNCHANGED; explicit dependency retention/referral',reason=reason,trigger=triggers[n],owner='SD editor' if path.startswith(SD) else 'Named Core primary or SHARED editor; no boundary write authority'))
    save('reopening-map.json',reopen)
    summary=dict(source_pin=PIN,observed_metadata_head=git('rev-parse','HEAD').decode().strip(),
                 packet='inventory/packets/BOUNDARY-SecurityDomain.json',packet_sha256=sha((RUN/'inventory/packets/BOUNDARY-SecurityDomain.json').read_bytes()),
                 assigned_ids=len(results),disposition_paths=len(files),packet_primary_paths=len(packet['paths']),
                 supplemental_complete_paths=sum(f['supplemental'] for f in files),
                 actual_lines_read=sum(len((ROOT/f['path']).read_bytes().decode('utf-8-sig').splitlines()) for f in files),
                 dispositions=dict(collections.Counter(u['status'] for u in results)),
                 inherited_assigned=len(inherited),local_candidates=len(candidates),accepted_core_ids=0,
                 unread=0,awaiting_source_context=0,investigation='COMPLETE FOR PINNED ASSIGNMENT',
                 independent_review='PENDING',documentation_edits='NOT AUTHORIZED',implementation='UNCHANGED',
                 examples='UNCOMPILED / VALID-NOT-CHECKED',sdk_builds=0,sdk_tests=0,native_device_crypto_executions=0,
                 scratch='/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/BOUNDARY-SecurityDomain',
                 constraints='No central/product/assertion/index/ref/archive/user work edits, no subagents; metadata HEAD may advance; Windows ready handoff pending, PIV physical deferrals preserved.')
    save('summary.json',summary)
    print(json.dumps({k:summary[k] for k in ['assigned_ids','disposition_paths','actual_lines_read','dispositions','inherited_assigned','local_candidates']}))

if __name__=='__main__':
    main()
