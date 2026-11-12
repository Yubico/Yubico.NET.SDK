"""Serialize explicitly authored boundary observations; verify identity, never infer reading."""
import collections
import hashlib
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[9]
RUN = ROOT / 'docs/research/documentation-audit/modules/core/pass-1/core-001'
OUT = RUN / 'boundaries/YubiOtp'
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
RUNTIME = Path('/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/YubiOtp')
RUNTIME.mkdir(parents=True, exist_ok=True)
for variable in ('TMPDIR', 'TMP', 'TEMP'):
    os.environ[variable] = str(RUNTIME)

def sha(data):
    return hashlib.sha256(data).hexdigest()

def git(*args, cwd=ROOT):
    return subprocess.check_output(['git', *args], cwd=cwd)

def put(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')

def norm(data):
    return data.decode('utf-8-sig').replace('\r\n', '\n')

packet = json.loads((RUN / 'inventory/packets/BOUNDARY-YubiOtp.json').read_text())
inventory = json.loads((RUN / 'inventory/semantic-units.json').read_text())
selected = set(packet['unit_ids'])
units = [unit for unit in inventory if unit['id'] in selected]
assert len(units) == len(selected) == 324
notes = json.loads((OUT / 'semantic-notes.json').read_text())

# These ranges were selected manually after full file/helper/assertion reading.
anchors = {
 'CORE-OTP-X001': {
  'src/YubiOtp/README.md': [(322,329)],
  'src/YubiOtp/src/IYubiOtpSession.cs': [(20,32)],
  'src/YubiOtp/src/IYubiKeyExtensions.cs': [(96,150)],
  'src/YubiOtp/src/YubiOtpSession.cs': [(124,169),(248,258),(529,549)],
  'src/YubiOtp/src/Backend/SmartCardBackend.cs': [(91,122)],
 },
 'CORE-OTP-X002': {
  'src/YubiOtp/CLAUDE.md': [(36,55),(159,165)],
  'src/YubiOtp/src/Backend/HidBackend.cs': [(44,53)],
  'src/YubiOtp/src/YubiOtpSession.cs': [(136,169)],
  'src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/YubiOtpSessionLinuxCrossLayerTests.cs': [(19,54)],
 },
 'CORE-OTP-X003': {
  'src/YubiOtp/src/Backend/HidBackend.cs': [(74,141)],
  'src/YubiOtp/src/Backend/SmartCardBackend.cs': [(124,187)],
  'src/YubiOtp/README.md': [(348,361)],
  'src/YubiOtp/src/IYubiOtpSession.cs': [(20,32),(288,307),(309,339)],
  'src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/HidBackendTests.cs': [(230,251),(286,307)],
  'src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/SmartCardBackendTests.cs': [(226,251)],
 },
 'CORE-OTP-X004': {
  'src/YubiOtp/src/IYubiKeyExtensions.cs': [(69,94)],
  'src/YubiOtp/README.md': [(164,185)],
 },
 'CORE-OTP-X005': {
  'src/YubiOtp/src/SlotConfiguration.cs': [(140,184)],
  'src/YubiOtp/src/YubiOtpSession.cs': [(529,574)],
  'src/YubiOtp/examples/OtpTool/Commands/ChalRespCommand.cs': [(29,53)],
  'src/YubiOtp/examples/OtpTool/Commands/HotpCommand.cs': [(30,54)],
  'src/YubiOtp/examples/OtpTool/Commands/StaticCommand.cs': [(35,65)],
 },
 'CORE-OTP-X006': {
  'src/YubiOtp/src/YubiOtpSession.cs': [(136,169),(586,633)],
  'src/YubiOtp/src/Backend/SmartCardBackend.cs': [(53,89),(205,238)],
  'src/YubiOtp/src/ConfigState.cs': [(19,32),(81,125)],
  'src/YubiOtp/README.md': [(10,37),(79,82),(286,320)],
 },
 'CORE-OTP-X007': {
  'src/YubiOtp/src/StaticPasswordSlotConfiguration.cs': [(38,79),(145,178)],
  'src/YubiOtp/README.md': [(241,256),(365,379)],
  'src/YubiOtp/examples/OtpTool/Commands/StaticCommand.cs': [(20,32),(40,84)],
  'src/YubiOtp/examples/OtpTool/CliOptions.cs': [(86,102)],
 },
 'CORE-OTP-X008': {
  'src/YubiOtp/src/IYubiOtpSession.cs': [(20,32)],
  'src/YubiOtp/src/PublicAPI.Unshipped.txt': [(1,195)],
 },
 'CORE-OTP-X009': {
  'src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/YubiOtpSessionLinuxCrossLayerTests.cs': [(19,54),(141,152)],
  'src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/SlotConfigurationTests.cs': [(36,40),(1042,1050)],
  'src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/IYubiKeyExtensionsTransportTests.cs': [(121,151),(202,223)],
  'src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/AuditV2/YubiOtpProtectSlot2Tests.cs': [(116,129)],
 }
}

def note_for(path):
    key = 'tests/CLAUDE.md' if path == 'src/YubiOtp/tests/CLAUDE.md' else Path(path).name
    assert key in notes, path
    return notes[key]

identities = []
files = []
for path in packet['disposition_paths']:
    raw = (ROOT / path).read_bytes()
    pinned = git('show', f'{PIN}:{path}')
    assert norm(raw) == norm(pinned), path
    associated = [candidate for candidate, paths in anchors.items() if path in paths]
    ids = [unit['id'] for unit in units if unit['path'] == path]
    identity = {
        'path': path, 'source_baseline': PIN, 'raw_sha256': sha(raw),
        'pinned_git_blob': git('rev-parse', f'{PIN}:{path}').decode().strip(),
        'pinned_content_sha256': sha(pinned), 'normalized_sha256': sha(norm(raw).encode()),
        'normalized_pin_equal': True, 'raw_pin_equal': raw == pinned,
        'read_scope': 'complete file, including full callers/helpers/assertions and enclosing cleanup',
    }
    identities.append(identity)
    files.append({**identity, 'unit_ids': ids, 'candidates': associated,
        'outcome': 'READ-CANDIDATE-CONTEXT' if associated else 'READ-CLEAN',
        'semantic_observation': note_for(path),
        'unchanged_reason': 'Investigation-only assignment preserves product bytes; localized candidate facets await triage, a fresh editor and independent review.' if associated else 'Retain the complete scoped contract/assertion context for the authored reason; no boundary correction selected. Future changes reopen the mapped owners.',
        'review_status': 'PENDING-INDEPENDENT-REVIEW', 'runtime_status': 'NOT-EXECUTED'})

results = []
for unit in units:
    raw = (ROOT / unit['path']).read_bytes()
    text = raw.decode('utf-8-sig')
    assert sha(raw) == unit['source_sha256'], unit['path']
    fragment = text[unit['start_character']:unit['end_character_exclusive']]
    assert sha(fragment.encode()) == unit['range_sha256'], unit['id']
    candidates = [candidate for candidate, paths in anchors.items()
        if any(unit['first_line'] <= end and unit['last_line'] >= start
               for start, end in paths.get(unit['path'], []))]
    results.append({**unit, 'status': 'READ-CANDIDATE' if candidates else 'READ-CLEAN',
        'read_scope': 'complete original range with full containing file/caller/helper/assertion context',
        'candidates': candidates, 'observation': note_for(unit['path']),
        'reason': 'Localized concern anchors; associated mechanisms/facets are in candidates.md.' if candidates else 'Read retained scoped behavior/contract/assertion; see the distinct file observation. This is not implementation acceptance or executed verification.',
        'source_baseline': PIN, 'review_status': 'PENDING-INDEPENDENT-REVIEW',
        'sample_status': 'INELIGIBLE-CANDIDATE' if candidates else 'ELIGIBLE-FOR-COORDINATOR-SELECTION',
        'compiler_status': 'VALID-NOT-CHECKED', 'runtime_status': 'NOT-EXECUTED'})

put('file-identities.json', identities)
put('file-dispositions.json', files)
put('unit-dispositions.json', results)

dependencies = {
 'CORE-OTP-X001': ['src/Core/src/Sessions/ApplicationSession.cs','src/Core/src/Devices/YubiKeyDevice.cs','src/Core/src/Devices/HidConnectionSlot.cs','src/Core/src/Devices/YubiKeyConnectionExtensions.cs','docs/architecture/physical-device-model.md','docs/architecture/connection-ownership-and-contention.md','docs/architecture/raw-access-tiers.md'],
 'CORE-OTP-X002': ['src/Core/src/Protocols/Otp/Hid/OtpHidProtocol.cs','src/Core/src/Transports/Hid/IOtpHidConnection.cs','src/Core/README.md','src/Core/CLAUDE.md'],
 'CORE-OTP-X003': ['src/Core/src/Protocols/Otp/Hid/OtpHidProtocol.cs','src/Core/src/Protocols/SmartCard/Apdu/ApduResponse.cs','src/Core/src/Protocols/SmartCard/Apdu/ApduTransmitter.cs','src/Core/src/Protocols/SmartCard/Apdu/ChainedResponseReceiver.cs','src/Core/src/Credentials/UserPresenceNotification.cs','src/Core/src/Native/Windows/HidD/WindowsHidReportAccess.cs'],
 'CORE-OTP-X004': ['src/Core/src/Sessions/ApplicationSession.cs'],
 'CORE-OTP-X005': ['src/Cli.Commands/src/Otp/OtpCommands.cs','docs/migration/v1-to-v2.md'],
 'CORE-OTP-X006': ['src/Core/src/Devices/Feature.cs','src/Core/src/Devices/FirmwareVersion.cs','src/Core/src/Protocols/SmartCard/Scp/ScpInitializer.cs','docs/usage/user-interaction.md'],
 'CORE-OTP-X007': ['src/Core/src/Credentials/ConsoleCredentialReader.cs','src/Core/src/Transports/Hid/Keyboard/HidCodeTranslator.cs','src/Core/src/Transports/Hid/Keyboard/HidCodeTranslator.ModHex.cs','src/Cli.Commands/src/Otp/OtpCommands.cs','src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/AuditV2/CliStaticPasswordEncodingTests.cs'],
 'CORE-OTP-X008': ['src/Management/src/IManagementSession.cs','src/Management/src/ManagementSession.cs','src/Management/src/DeviceConfig.cs','src/Core/src/Devices/DeviceInfoReader.cs','docs/migration/v1-to-v2-gaps.md','docs/migration/v1-to-v2-map.yml','docs/migration/v1-to-v2.md'],
 'CORE-OTP-X009': ['src/YubiOtp/tests/CLAUDE.md','src/YubiOtp/CLAUDE.md','src/YubiOtp/README.md']
}
index = []
for candidate in anchors:
    primary = list(anchors[candidate])
    index.append({'id': candidate, 'accepted_id': None,
        'anchors': [{'path': path, 'line_ranges': ranges} for path, ranges in anchors[candidate].items()],
        'exact_unit_ids': [unit['id'] for unit in results if candidate in unit['candidates']],
        'candidate_associated_unchanged_paths': primary + dependencies[candidate],
        'facets': {'documentation': 'CANDIDATE-FRESH-EDIT-AND-REVIEW',
                   'runtime': 'DEFERRED-NO-AUTHORIZATION', 'decision': 'SEE-CANDIDATE-LOCAL-CONDITION',
                   'qualification': 'SOURCE-ONLY-PLUS-EXPLICIT-HISTORICAL-EVIDENCE'},
        'reopening_record': 'candidates.md', 'reopening_group': candidate})
put('candidate-index.json', index)

unchanged = []
triggers = {
 'CORE-OTP-X001': 'Change to known-member claims, hidden/borrowed connection ownership, session binding, failure cleanup or whole-operation admission/disposal policy.',
 'CORE-OTP-X002': 'Change to eight-byte report admission, first/second status provenance, NEO rejection classification, local polling or reset/recovery policy.',
 'CORE-OTP-X003': 'Change to sensitive native/report/raw/APDU/fragment/response storage ownership, result slicing, callback cleanup or logging-failure precedence.',
 'CORE-OTP-X004': 'Change to one-shot result retention/transfer/zeroing when hidden-session teardown fails after successful calculation.',
 'CORE-OTP-X005': 'Change to configuration assembly or validation order, source/config/result cleanup, or CLI code-parser exception scopes.',
 'CORE-OTP-X006': 'Change to raw/detected/effective firmware, overrides, slot/config/operation gates, sequence bypass, command capability or notification certainty.',
 'CORE-OTP-X007': 'Change to keyboard mapping/layout applicability, standalone versus unified CLI routing, generated character alphabet or credential-input policy.',
 'CORE-OTP-X008': 'A named device-info, FIPS-mode query or legacy configuration API is introduced/removed, or modern/legacy gates and device qualification change.',
 'CORE-OTP-X009': 'Change to named test setup/assertions/failure cleanup/independent oracles, or arrival of exact source-bound platform/device execution evidence.'
}
for path in sorted(set(packet['disposition_paths']) | {path for ps in dependencies.values() for path in ps}):
    relevant = [row['id'] for row in index if path in row['candidate_associated_unchanged_paths']]
    if path in packet['disposition_paths']:
        reason = note_for(path)
        owner = 'YubiOTP future fresh editor; this worker owns records only'
    else:
        reason = 'Retain current product bytes. This is a dependency/reopening path for the exact local candidate, not an accepted full-file clean outcome or edit. Reopen after the candidate-specific behavior/ownership/provenance decision in candidates.md; shared Core mechanisms retain A/B/C ownership and shared prose has one future SHARED editor.'
        owner = 'Owning Core family, CLI/Management boundary, or future SHARED editor'
    unchanged.append({'path': path, 'candidates': relevant, 'disposition': 'UNCHANGED',
        'reason': reason, 'owner': owner, 'reopening_details': 'candidates.md; flow-and-verification.md',
        'precise_reopening_triggers': [{'candidate': candidate, 'condition': triggers[candidate]} for candidate in relevant],
        'sha256': sha((ROOT / path).read_bytes())})
put('unchanged-paths.json', unchanged)

read_ranges = {
 'src/Core/src/Protocols/Otp/Hid/OtpHidProtocol.cs': [(1,632)],
 'src/Core/src/Sessions/ApplicationSession.cs': [(1,569)],
 'src/Core/src/Protocols/ProtocolFactory.cs': [(1,9999)],
 'src/Core/src/Protocols/SmartCard/Apdu/PcscProtocol.cs': [(1,207)],
 'src/Core/src/Protocols/SmartCard/Apdu/ApduResponse.cs': [(1,100)],
 'src/Core/src/Protocols/SmartCard/Apdu/ApduTransmitter.cs': [(1,49)],
 'src/Core/src/Protocols/SmartCard/Apdu/ChainedResponseReceiver.cs': [(1,77)],
 'src/Core/src/Protocols/SmartCard/Scp/ScpInitializer.cs': [(1,210)],
 'src/Core/src/Credentials/UserPresenceNotification.cs': [(1,205)],
 'src/Core/src/Credentials/ConsoleCredentialReader.cs': [(1,389)],
 'src/Core/src/Utilities/ExchangeGuard.cs': [(1,140)],
 'src/Core/src/Devices/DeviceInfoReader.cs': [(1,201)],
 'src/Core/src/Devices/YubiKeyDevice.cs': [(1,322)],
 'src/Core/src/Devices/HidConnectionSlot.cs': [(1,132)],
 'src/Core/src/Devices/YubiKeyConnectionExtensions.cs': [(175,305)],
 'src/Core/src/Devices/RegisteredConnections.cs': [(1,107)],
 'src/Core/src/Devices/FirmwareVersion.cs': [(1,277)],
 'src/Core/src/Devices/Feature.cs': [(1,42)],
 'src/Core/src/Protocols/Otp/Hid/OtpHidConnection.cs': [(1,78)],
 'src/Core/src/Transports/Hid/IOtpHidConnection.cs': [(1,62)],
 'src/Core/src/Transports/Hid/Linux/LinuxOtpHidConnection.cs': [(1,113)],
 'src/Core/src/Transports/Hid/Linux/LinuxOtpHidNativeOwner.cs': [(1,380)],
 'src/Core/src/Transports/Hid/Linux/LinuxHidFeatureReportConnection.cs': [(1,108)],
 'src/Core/src/Transports/Hid/MacOS/MacOSOtpHidConnection.cs': [(1,395)],
 'src/Core/src/Transports/Hid/MacOS/MacOSHidFeatureReportConnection.cs': [(1,49)],
 'src/Core/src/Transports/Hid/Windows/WindowsHidFeatureReportConnection.cs': [(1,84)],
 'src/Core/src/Native/Windows/HidD/WindowsHidReportAccess.cs': [(1,320)],
 'src/Core/src/Sessions/RawOtpHidSession.cs': [(1,77)],
 'src/Core/src/Transports/Hid/Keyboard/HidCodeTranslator.cs': [(1,214)],
 'src/Core/src/Transports/Hid/Keyboard/HidCodeTranslator.ModHex.cs': [(1,9999)],
 'src/Core/src/Transports/Hid/Keyboard/KeyboardLayout.cs': [(1,28)],
 'src/Management/src/IManagementSession.cs': [(1,130)],
 'src/Management/src/ManagementSession.cs': [(130,229)],
 'src/Management/src/DeviceConfig.cs': [(1,9999)],
 'src/Cli.Commands/src/Otp/OtpCommands.cs': [(190,264),(495,574)],
 'src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/AuditV2/CliStaticPasswordEncodingTests.cs': [(1,83)],
 'docs/migration/v1-to-v2-gaps.md': [(201,257)],
 'docs/usage/user-interaction.md': [(160,219)],
 'src/Core/README.md': [(1,661)],
 'src/Core/CLAUDE.md': [(1,907)]
}
dependency_reads = []
for path, ranges in read_ranges.items():
    raw = (ROOT / path).read_bytes()
    text = raw.decode('utf-8-sig')
    lines = text.splitlines(keepends=True)
    pinned = git('show', f'{PIN}:{path}')
    assert norm(raw) == norm(pinned), path
    selections = []
    for first, last in ranges:
        last = min(last, len(lines))
        start = sum(map(len, lines[:first - 1]))
        end = sum(map(len, lines[:last]))
        selections.append({'first_line': first, 'last_line': last,
            'start_character': start, 'end_character_exclusive': end,
            'range_sha256': sha(text[start:end].encode())})
    dependency_reads.append({'path': path, 'source_baseline': PIN,
        'raw_sha256': sha(raw), 'pinned_git_blob': git('rev-parse',f'{PIN}:{path}').decode().strip(),
        'normalized_pin_equal': True, 'ranges': selections,
        'role': 'Read-only caller/helper/owner context; primary family and future shared editor retain disposition/edit ownership. No new assigned units are created.'})
put('dependency-reads.json', dependency_reads)

import re
api_search = []
pattern = re.compile(r'QueryFips|FipsMode|SetLegacy|LegacyDeviceConfig|SetModeAsync')
for path in sorted((ROOT / 'src').glob('*/src/PublicAPI*.txt')):
    raw = path.read_bytes()
    hits = [{'line': number, 'text': line} for number, line in enumerate(raw.decode('utf-8-sig').splitlines(),1) if pattern.search(line)]
    api_search.append({'path': str(path.relative_to(ROOT)), 'sha256': sha(raw), 'matches': hits})
put('api-surface-search.json', {'pattern': pattern.pattern, 'snapshots': api_search,
    'scope': 'Complete current snapshot name search corroborates the full owning interfaces, not semantic equivalence of every v1 API or device capability.'})

registry = json.loads((RUN / 'inherited-findings/registry.json').read_text())
inputs = json.loads((RUN / 'inventory/inherited-packet-inputs.json').read_text())['BOUNDARY-YubiOtp']
recommendations = {
 'yubiotp-001:OTP-DEP-001': ('SOURCE-TRACE-COMPLETE-QUALIFIED', 'Hidden/direct connection ownership, no-fallback selection, binding and failure cleanup traced. Native release and whole-workflow limits remain B1/B2/B3/B4/B5; X001 supplies current applet applicability.', ['CORE-OTP-X001']),
 'yubiotp-001:OTP-DEP-002': ('SOURCE-TRACE-COMPLETE-RETAINED-CONCERNS', 'Built-in eight-byte invariant versus custom unchecked reports, outbound Core CRC versus inbound applet CRC, status/sequence, touch polling and awaited abort/reset latch traced. X002/X003 reuse B5 mechanisms; SmartCard permissiveness stays OTP-IMP-005.', ['CORE-OTP-X002','CORE-OTP-X003','CORE-OTP-X009']),
 'yubiotp-001:OTP-DEP-003': ('SOURCE-TRACE-COMPLETE-RETAINED-CONCERNS', 'Admitted SmartCard exchange uses None; backend resolves before sensitive result transfer. Source, serialized input, raw transport, ApduResponse Data/prefix and outer one-shot result are distinct owners. X004 adds an outer teardown-failure transfer concern; no shared mechanism is counted twice.', ['CORE-OTP-X001','CORE-OTP-X003','CORE-OTP-X004','CORE-OTP-X005']),
 'yubiotp-001:OTP-DEP-004': ('SOURCE-TRACE-COMPLETE-DECISION-QUALIFIED', 'First HID report/version, second cached status, SmartCard Management/OTP selection and effective override are separate. Major-zero gates remain optimistic policy; support, NEO rationale, raw/effective prompt and sequence policy retain qualifications.', ['CORE-OTP-X002','CORE-OTP-X006']),
 'yubiotp-001:OTP-DEP-005': ('RELOCATION-SOURCE-INVENTORY-COMPLETE', 'Device-info route exists through Management and Core OTP capabilities/page/CRC reader. No typed FIPS-mode query or legacy four-byte mode mutator was found in complete current public snapshots and owning interfaces. Modern Management TLV writes are separate; device/profile parity remains qualified.', ['CORE-OTP-X008'])
}
inherited = []
for key in inputs['provenance_keys']:
    original = next(row for row in registry['rows'] if row['key'] == key)
    outcome, observation, candidates = recommendations.get(key, (
        'RELATED-CORE-MECHANISM-APPLICABILITY-ONLY',
        'This YubiOTP trace confirms use of the shared effective-version or exchange/response ownership mechanism. Preserve the authoritative original state and applet-specific decision/physical gap. The named other applet boundary owns full source/profile closure; YubiOTP does not inherit or impose its firmware floor/reset/authorization policy.',
        ['CORE-OTP-X001','CORE-OTP-X003','CORE-OTP-X006']))
    inherited.append({'key': key, 'source_run': original['source_run'],
        'original_id': original['original_id'], 'group': original['dedup_group'],
        'authoritative_state_unchanged': original['current_state'],
        'authoritative_conclusion_unchanged': original['current_conclusion'],
        'boundary_recommendation': outcome, 'current_observation': observation,
        'candidates': candidates, 'documentation': 'REOPEN-ONLY-AS-MAPPED; fresh editor/reviewer pending',
        'runtime': 'UNCHANGED', 'qualification': 'NO-NEW-EXECUTION; original physical/profile qualifications retained'})
put('inherited-dispositions.json', {'registry_sha256': sha((RUN / 'inherited-findings/registry.json').read_bytes()),
    'counting': '14 original source-run keys in four nonadditive groups; no central state mutation', 'rows': inherited,
    'extra_provenance': ['audit-v2-20261022-20261031:N3', 'fixes-wave-001:CORE-001'],
    'extra_provenance_disposition': 'N3 unified CLI encoding/redirected confirmation fix retains exact scope; standalone OTP-IMP-009 persists. Historical serial 9681620 failure remains unexplained, with no physical closure.'})

imp = {
 '001': ('HOTP divisor remains applet-owned; no Core catalog or codec fix', ['CORE-OTP-X006','CORE-OTP-X009']),
 '002': ('38-byte scan-map API versus vendor 45 remains applet-owned', ['CORE-OTP-X008']),
 '003': ('Configuration/response/parser owners rechecked; shared copies remain family-owned and new outer one-shot transfer concern is X004', ['CORE-OTP-X003','CORE-OTP-X004','CORE-OTP-X005']),
 '004': ('Cached keyboard trigger still drives SmartCard heuristic; historical status matrix and current call sites retained', ['CORE-OTP-X006']),
 '005': ('SmartCard short fallback, bypass and zero acceptance remain applet-owned; HID polling/reset are separate Core mechanisms', ['CORE-OTP-X002','CORE-OTP-X006','CORE-OTP-X009']),
 '006': ('Type/operation gates remain incomplete per-flag/length support checks; raw/effective provenance traced', ['CORE-OTP-X006']),
 '007': ('Full masked update and omitted new-code semantics remain applet-owned; legacy code-change interval enforcement still absent', ['CORE-OTP-X005','CORE-OTP-X006']),
 '008': ('NDEF null/empty/enum/language/UTF8/read-back remain applet-owned; Core framing adds no NFC capability proof', ['CORE-OTP-X008']),
 '009': ('Standalone OtpTool still bypasses translator, while unified CLI uses working string route; historical N3 preserved separately', ['CORE-OTP-X007']),
 '010': ('CLI parser/help/JSON/secret-output/transport behavior remains sample-owned, with narrow unified CLI early config cleanup referral', ['CORE-OTP-X005','CORE-OTP-X007']),
 '011': ('Actual weak/strong oracles and failure cleanup scopes rechecked without changing assertions', ['CORE-OTP-X009']),
 '012': ('HMAC marker/AES/challenge transport qualification retains historical serial 103 successes and CCID rejection; no broader support inferred', ['CORE-OTP-X006','CORE-OTP-X009'])
}
put('applet-findings-dispositions.json', [{'key': 'yubiotp-001:OTP-IMP-' + number,
    'outcome': 'RETAIN-ORIGINAL-APPLET-SCOPE', 'observation': observation,
    'candidates': candidates, 'source_authority': 'docs/research/documentation-audit/modules/yubiotp/pass-1/yubiotp-001/findings.md:43–54',
    'implementation': 'DEFERRED-PASS-2', 'documentation': 'EXISTING-QUALIFICATIONS-RETAINED; mapped future reopening',
    'verification': 'NO-NEW-EXECUTION'} for number, (observation, candidates) in imp.items()])

nominee_labels = [
 ('StaticPasswordSlotConfiguration.cs', 'Initialize('),
 ('ConfigState.cs', 'public ConfigState(ReadOnlySpan'),
 ('SlotConfiguration.cs', 'SetHmacKey('),
 ('SlotConfigurationTests.cs', 'StaticPassword_FromString_CopiesOwnedScanCodesBeforeZeroing'),
 ('SlotConfigurationEncapsulationTests.cs', 'Dispose_ZeroesAllConfigurationBuffers'),
 ('YubiOtpSessionTests.cs', 'CalculateHmacSha1Async_ShortAndExplicitlyPaddedChallenges_SendIdenticalRequests'),
 ('YubiOtpSessionTests.cs', 'CalculateHmacSha1Async_AtShortAndFixedLengthBoundary_PreservesInputBytes'),
 ('IYubiKeyExtensionsTransportTests.cs', 'CreateYubiOtpSessionAsync_DefaultBothTransports_PicksSmartCard'),
 ('YubiOtpSessionLinuxCrossLayerTests.cs', 'PublicHmac_CancelAtTouchWaitResolvesOnlyAfterAbortThenReadOnlySerialRecovers'),
 ('YubiOtpSessionLinuxCrossLayerTests.cs', 'PublicHmac_TerminalPresenceOutcomeBelongsToAppletCrcValidation'),
 ('YubiOtpSessionTests.cs', 'BuildNdefPayload_HttpWwwUri_CompressesLongestPrefix'),
 ('SlotConfigurationTests.cs', 'YubiOtp_GetConfig_PlacesFieldsCorrectly')
]
nominees = []
for filename, symbol in nominee_labels:
    matches = [unit for unit in results if Path(unit['path']).name == filename
               and symbol in unit['label'] and unit['status'] == 'READ-CLEAN']
    # A localized candidate is deliberately excluded even if its neighboring helper is useful.
    if not matches:
        continue
    for unit in matches:
        nominees.append({'id': unit['id'], 'path': unit['path'], 'label': unit['label'],
            'first_line': unit['first_line'], 'last_line': unit['last_line'],
            'source_sha256': unit['source_sha256'], 'range_sha256': unit['range_sha256'],
            'reason': unit['observation'], 'sample_status': 'NOMINEE-NOT-INDEPENDENTLY-SAMPLED'})
put('clean-nominees.json', nominees)

source_identities = []
python_root = Path('/home/rupicola/Code/y/yubikey-manager')
python_pin = '4ca60f706af930459138d8dc0f0f953480e1c7a4'
for path in ['yubikit/core/otp.py','yubikit/yubiotp.py']:
    raw = (python_root / path).read_bytes()
    pinned = git('show', f'{python_pin}:{path}', cwd=python_root)
    assert raw == pinned
    source_identities.append({'repository': 'https://github.com/Yubico/yubikey-manager',
        'revision': python_pin, 'path': path, 'sha256': sha(raw), 'bytes': len(raw),
        'git_blob': git('rev-parse', f'{python_pin}:{path}', cwd=python_root).decode().strip(),
        'read_scope': 'complete source; no package import or execution'})
captures = json.loads((RUN / 'investigation/B5/sources/captures-followup.json').read_text())
for capture in captures:
    path = RUN / 'investigation/B5/sources' / capture['name']
    raw = path.read_bytes()
    assert sha(raw) == capture['sha256']
    source_identities.append({**capture, 'local_path': str(path.relative_to(ROOT)),
        'boundary_role': 'independently read existing immutable-source capture; original access metadata retained'})
put('source-identities.json', source_identities)

counts = dict(collections.Counter(unit['status'] for unit in results))
summary = {'source_baseline': PIN, 'observed_record_head': git('rev-parse','HEAD').decode().strip(),
    'packet': 'inventory/packets/BOUNDARY-YubiOtp.json', 'packet_sha256': sha((RUN / 'inventory/packets/BOUNDARY-YubiOtp.json').read_bytes()),
    'primary_files': len(packet['paths']), 'disposition_paths': len(packet['disposition_paths']),
    'packet_declared_file_count': packet['file_count'], 'disposition_lines': packet['lines'],
    'count_qualification': 'Literal paths has 61 entries; disposition_paths and declared file_count have 62. Shipped API snapshot is disposition-only and has no assigned unit. No central denominator was changed.',
    'assigned_units': len(results), 'shared_assigned_units': 0, 'counts': counts,
    'unread': 0, 'candidate_groups': len(anchors), 'accepted_core_ids': 0,
    'clean_nominees': len(nominees), 'source_investigation': 'COMPLETE-FOR-ASSIGNED-SCOPE-WITH-EXPLICIT-QUALIFICATIONS',
    'independent_review': 'PENDING', 'editing': 'NOT-STARTED', 'sampling': 'PENDING',
    'build_test_formatter_native_device_crypto_executions': 0,
    'compiler': 'VALID-NOT-CHECKED', 'windows_delta': 'AWAIT-USER-READY-SEPARATE-HANDOFF',
    'piv_physical': 'DEFERRED-UNCHANGED', 'temporary_environment': {v: os.environ[v] for v in ('TMPDIR','TMP','TEMP')}}
put('coverage-summary.json', summary)
put('preservation-verification.json', {'status': 'PASS', 'source_baseline': PIN,
    'checks': ['324 exact original IDs, no duplicate or omitted IDs',
               'every original raw source and character-range hash matches',
               'all 62 disposition paths match source pin after explicit BOM/newline normalization',
               'all candidate-associated paths have unchanged reasons',
               '14 inherited keys preserve original registry states/conclusions',
               '12 original OTP-IMP source-run scopes retained',
               'Python bytes match exact pin; vendor captures match immutable retrieval manifest',
               'clean nominees are selected clean substantive units, not completed sampling'],
    'limitations': 'Static record/identity verification is not independent content review, compilation, runtime behavior, crypto or hardware qualification.',
    'counts': summary})
print(json.dumps(summary, indent=2))
