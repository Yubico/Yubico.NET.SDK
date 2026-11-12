#!/usr/bin/env python3
"""Join authored boundary observations to frozen IDs; no SDK or product writes."""
import collections
import hashlib
import json
from pathlib import Path
import subprocess

HERE = Path(__file__).resolve().parent
RUN = HERE.parent.parent
ROOT = next(parent for parent in HERE.parents if (parent / '.git').exists())
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'


def sha(data):
    return hashlib.sha256(data).hexdigest()


def load(path):
    return json.loads(path.read_text())


def save(name, value):
    (HERE / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')


def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)


O = 'src/Oath/'
U = O + 'tests/Yubico.YubiKit.Oath.UnitTests/'
I = O + 'tests/Yubico.YubiKit.Oath.IntegrationTests/'
E = O + 'examples/OathTool/'
# Exact concern anchors, chosen after complete semantic reads. Neighboring helpers are not defects.
anchors = [
    (O+'src/IOathSession.cs', 19, 60, 'X001'),
    (O+'src/OathSession.cs', 45, 55, 'X001'),
    (O+'README.md', 203, 246, 'X002'),
    (O+'CLAUDE.md', 164, 183, 'X002'),
    (O+'src/OathSession.cs', 397, 528, 'X002'),
    (O+'src/OathSession.cs', 577, 639, 'X002'),
    (O+'src/OathSession.cs', 844, 911, 'X002'),
    (U+'OathScpResetTests.cs', 76, 108, 'X003'),
    (U+'Yubico.YubiKit.Oath.UnitTests.csproj', 27, 28, 'X003'),
    (O+'src/Credentials/OathCredentialOptions.cs', 19, 33, 'X004'),
    (U+'OathSessionTests.cs', 115, 186, 'X005'),
    (U+'OathSessionTests.cs', 628, 646, 'X005'),
    (U+'CredentialDataTests.cs', 609, 623, 'X005'),
    (I+'AuditV2/OathAuditReproTests.cs', 16, 68, 'X005'),
    (I+'OathPasswordChangeTests.cs', 37, 65, 'X005'),
    (I+'OathPasswordChangeTests.cs', 122, 137, 'X005'),
    (I+'OathHashAlgorithmTests.cs', 155, 213, 'X005'),
    (I+'OathSessionTests.cs', 145, 193, 'X005'),
    (O+'src/IYubiKeyExtensions.cs', 41, 44, 'X006'),
    (O+'README.md', 39, 61, 'X006'),
    (O+'README.md', 228, 264, 'X006')
]

packet = load(RUN/'inventory/packets/BOUNDARY-Oath.json')
notes = load(HERE/'file-notes.json')
assert set(notes) == set(packet['disposition_paths'])
all_units = {unit['id']: unit for unit in load(RUN/'inventory/semantic-units.json')}
assert len(packet['unit_ids']) == len(set(packet['unit_ids']))
selected = [all_units[key] for key in packet['unit_ids']]
assert {unit['path'] for unit in selected} == set(packet['paths'])
identities = []
texts = {}
for path in packet['disposition_paths']:
    raw = (ROOT/path).read_bytes()
    text = raw.decode('utf-8-sig', errors='replace')
    texts[path] = text
    pinned = git('show', PIN+':'+path)
    normalized = raw.removeprefix(b'\xef\xbb\xbf').replace(b'\r\n', b'\n')
    pin_normalized = pinned.removeprefix(b'\xef\xbb\xbf').replace(b'\r\n', b'\n')
    assert normalized == pin_normalized, path
    identities.append(dict(path=path, source_pin=PIN,
        source_sha256=sha(raw), pinned_raw_sha256=sha(pinned), normalized_sha256=sha(normalized),
        pinned_git_blob=git('rev-parse', PIN+':'+path).decode().strip(),
        pin_equality='RAW-EQUAL' if raw == pinned else 'BOM-CRLF-NORMALIZED-EQUAL',
        lines=len(text.splitlines()), full_file_read=True,
        credit='assigned selected units only; other units are related context',
        candidates=sorted({'CORE-OATH-'+candidate for p, a, b, candidate in anchors if p == path}),
        observation=notes[path], product_disposition='UNCHANGED',
        future_editor_review='PENDING; retain or edit exact local passages after triage'))
save('file-dispositions.json', identities)

results = []
for unit in selected:
    raw = (ROOT/unit['path']).read_bytes()
    body = texts[unit['path']][unit['start_character']:unit['end_character_exclusive']]
    assert sha(raw) == unit['source_sha256'], unit['id']
    assert sha(body.encode()) == unit['range_sha256'], unit['id']
    candidates = sorted({'CORE-OATH-'+c for p,a,b,c in anchors
        if p == unit['path'] and a <= unit['last_line'] and unit['first_line'] <= b})
    context = unit['last_line']-unit['first_line'] < 3 or unit['path'].endswith(('.csproj', '.txt'))
    status = 'READ-CANDIDATE' if candidates else 'READ-CONTEXT' if context else 'READ-CLEAN'
    results.append({**unit, 'status':status, 'read_context':'Complete containing file and relevant callers/helpers/assertions read.',
        'observation':notes[unit['path']], 'local_candidates':candidates,
        'unchanged_disposition':'Retain source bytes; candidate passages await fresh editor, related methods remain unchanged.',
        'review_status':'PENDING-INDEPENDENT-CONTENT-REVIEW',
        'validation_status':'VALID-NOT-CHECKED',
        'sample_status':'ELIGIBLE-UNSAMPLED' if status == 'READ-CLEAN' else 'INELIGIBLE',
        'source_pin':PIN, 'actual_range_sha256':sha(body.encode())})
save('unit-dispositions.json', results)

registry = load(RUN/'inherited-findings/registry.json')
inputs = load(RUN/'inventory/inherited-packet-inputs.json')['BOUNDARY-Oath']
outcomes = {
 'fixes-wave-001:CORE-001':'Changed architecture; original serial 9681620 two-key abandoned HID read remains unexplained. OATH factory uses SmartCard but default sample/fixture discovery uses All. A/B1/B2/B4 own cause/native-release qualification; no closure.',
 'audit-v2-20261022-20261031:multi-key-exclusions':'Retain separate historical provenance in same nonadditive discovery group. All/SmartCard-scope distinction changes which probes run, not diagnosis of old excluded125/9681620.',
 'oath-pilot-and-fixes-wave-002:IMP-004':'Resolved source exact parser/Core decode path: nonsecret checks precede decode, failure clears admitted disjoint destination, temporary finally clears after clone. Actual destination observers differ from source-only private abandoned-URI-array cleanup.',
 'oath-pilot-and-fixes-wave-002:IMP-005':'Resolved source exact shared Base32 length/padding/three-caller delegation; permissive pad bits preserved. OATH rejects decoded empty; generic decoder accepts empty. No broader canonical-decoder policy reopened.',
 'oath-pilot-and-fixes-wave-002:IMP-010':'Resolved source terminal success both plain/SCP: await disposal; owned closes/borrowed remains caller-owned; fresh close/reopen required. Actual fake state observers support cleanup. Serial1256A88 never reached EXAUTH/RESET; live SCP qualification remains open.',
 'oath-pilot-and-fixes-wave-002:DEC-001':'Host mismatch/missing response provenance corrected to BadResponse; device6984/6A80 retains neutral WrongPassword raw status. Broader post-verification-failure session-usability policy remains open; Core secure-wire recovery latch is separate.',
 'oath-pilot-and-fixes-wave-002:DEC-002':'Nonempty raw-key admission still differs from standard PBKDF2 16-byte workflow. No arbitrary-size device support claimed and no new restriction authorized. Preserve this source-run key, unrelated to CTAP native-selected-user decision.',
 'oath-pilot-and-fixes-wave-002:DEC-007':'Effective OATH5.6.3 any-SCP gate confirmed, Core5.3.0/5.7.2 separate; major-zero optimistic policy/override observed. Python4ca60f7 and floor-introduction1b87964 support source policy, not hardware rationale/earliest support.',
 'oath-pilot-and-fixes-wave-002:DOC-002':'Inherited/B4 read-only YAML ScannerError274:488 remains preexisting shared-map issue; no new parser execution or YAML edit. OATH migration fragments have separate resource/prerequisite handoff X006.',
 'openpgp-001:PGP-CORE-003':'OATH contributes exact selected/raw versus override/effective gate trace only. OpenPGP raw0.0.1/metadata/KDF/factory behavior stays its dedicated boundary; no secondary credit or closure.',
 'security-domain-001:SD-DEP-003':'OATH terminal reset retains no parameters/options and reopens explicitly; SD reinitialization-argument issue is a different local policy. Dedicated SD boundary retains complete disposition, no OATH-based closure.',
 'yubihsm-001:HSM-DEP-001':'OATH selected version requires three bytes even with override; HSM fallback/reset retention remain HSM boundary. Shared Feature compatibility policy applies, but no HSM caller review credit.',
 'yubiotp-001:OTP-DEP-004':'OATH SmartCard selected/raw/effective trace supplies comparison only. OTP HID status normalization/provenance remains YubiOTP/B5 ownership; no OATH HID branch or cross-applet policy acceptance.'
}
assert set(outcomes) == set(inputs['provenance_keys'])
save('inherited-dispositions.json', dict(source_pin=PIN, central_registry_changed=False,
    registry_sha256=sha((RUN/'inherited-findings/registry.json').read_bytes()),
    rows=[dict(key=row['key'], group=row['dedup_group'], original_state=row['current_state'],
        original_authority=row['authority'], original_definition=row['historical_definition'],
        boundary_outcome=outcomes[row['key']], implementation='scoped recommendation; authority unchanged',
        independent_review='PENDING', new_execution=False)
        for row in registry['rows'] if row['key'] in outcomes]))

nominee_ids = ['6556f321d499229a','08fa2a06930cf383','678b266776feac67','e19a0eeb78c8f878',
 '335bcece90474aec','d42708df6cbf5f75','566a4dca6e5e1756','15042402e458be8c','c709b96e9252e2c8',
 'dfa7422afebc263a','b9c2cf36e32a14d9','7d64d4398accfad4','024802ba2a87491d',
 '009154470ceb40e2','1a23ce6072e9aac9','8e2c83df186e5f3e']
by_id = {unit['id']:unit for unit in results}
nominees = []
for suffix in nominee_ids:
    unit = by_id['core-001:unit:'+suffix]
    assert unit['status'] == 'READ-CLEAN', unit['id']
    nominees.append(dict(unit_id=unit['id'], path=unit['path'], label=unit['label'],
        source_sha256=unit['source_sha256'], range_sha256=unit['range_sha256'],
        first_line=unit['first_line'],last_line=unit['last_line'],
        context=notes[unit['path']], sampled=False, executed=False,
        rule='Read complete helper/assertion scope; remove any later edited overlap before sample selection.'))
save('clean-nominees.json', nominees)

candidate_index = []
for suffix in ['X001','X002','X003','X004','X005','X006']:
    key = 'CORE-OATH-'+suffix
    candidate_index.append(dict(id=key, accepted_core_id=None,
        anchors=[dict(path=p,first_line=a,last_line=b) for p,a,b,c in anchors if c == suffix],
        assigned_unit_ids=[u['id'] for u in results if key in u['local_candidates']],
        all_facets_and_exact_secondary_reopening_map='candidates.md#core-oath-'+suffix.lower(),
        documentation='CANDIDATE', runtime='Separate facet/secondary ownership; no implementation authorized',
        decision='Maintainer/coordinator triage pending', qualification='No fresh SDK/native/device execution',
        independent_content_review='PENDING', product_files='UNCHANGED'))
save('candidate-index.json', candidate_index)

counts = dict(collections.Counter(u['status'] for u in results))
summary = dict(source_pin=PIN, observed_metadata_head=git('rev-parse','HEAD').decode().strip(),
    exact_packet_sha256=sha((RUN/'inventory/packets/BOUNDARY-Oath.json').read_bytes()),
    assigned_units=len(results), assigned_selected_paths=len(packet['paths']),
    disposition_paths=len(notes), full_files_read=len(notes), complete_context_lines=sum(x['lines'] for x in identities),
    counts=counts, unread=0, shared_read_unit_ids=packet['shared_read_unit_ids'],
    inherited_provenance_rows=len(outcomes), nonadditive_inherited_groups=len(inputs['group_ids']),
    local_candidate_groups=6, accepted_core_ids=0, clean_nominees=len(nominees),
    independent_content_review='PENDING', editing='NOT-ASSIGNED', sampling='PENDING',
    compilation='VALID-NOT-CHECKED', sdk_tests=0, crypto_runs=0, native_runs=0, device_operations=0,
    windows_late_delta='USER-READY-HANDOFF-PENDING', piv_physical_deferral='PRESERVED',
    historical_implementation_exception='Preserved; all 14 OATH IMP scopes retain source resolutions.',
    source_identity='Every selected raw/range hash checked; every disposition path pin-equal after explicit BOM/CRLF handling.',
    clean_meaning='Source/assertion documentation disposition, not independent review or runtime acceptance.')
save('coverage-summary.json', summary)
print(json.dumps(summary, indent=2))
