"""Inventory metadata only. Semantic verdicts come from the investigator's complete file reads."""
import hashlib
import json
import math
from pathlib import Path
import re

HERE = Path(__file__).resolve().parent
RUN = HERE.parent
ROOT = Path.cwd()
inventory = json.loads((RUN / 'inventory.json').read_text())
file_findings = {
    'CLAUDE.md': [4,10,14,18], 'README.md': [1,2,3,4,5,6,7,8,9,10,11,12,15,17,19],
    'PACKAGE_README.md': [], 'AlgorithmAttributes.cs': [9,10,18],
    'ApplicationRelatedData.cs': [18], 'DiscretionaryDataObjects.cs': [18],
    'OpenPgpSession.cs': [3,5,9,10,18], 'OpenPgpSession.Pin.cs': [1,2,10,12,19],
    'OpenPgpSession.Keys.cs': [9,10,11,12,13,14], 'OpenPgpSession.Crypto.cs': [6,7,8,9],
    'OpenPgpSession.Config.cs': [2,3,9,10], 'OpenPgpSession.Certificates.cs': [10,12],
    'OpenPgpSession.Reset.cs': [3,4,5], 'IOpenPgpSession.cs': list(range(1,20)),
    'Kdf.cs': [1,2,3,18], 'Pw.cs': [1], 'PwStatus.cs': [18],
    'Crt.cs': [13], 'DataObject.cs': [18], 'CurveOid.cs': [18],
    'OpenPgpAid.cs': [18], 'OpenPgpInvalidPinException.cs': [1,19],
    'SecuritySupportTemplate.cs': [18], 'Uif.cs': [4,10],
    'PrivateKeyImport.cs': [14], 'PrivateKeyTemplate.cs': [18],
    'OpenPgpCredentialOptions.cs': [18], 'OpenPgpAdvancedTests.cs': [2,3,16],
    'OpenPgpDecryptTests.cs': [11,16], 'OpenPgpKeyImportTests.cs': [11,16,18],
    'OpenPgpMultiCurveTests.cs': [10,16], 'OpenPgpPinManagementTests.cs': [16,18],
    'OpenPgpSessionTests.cs': [11,12,16], 'OpenPgpTestStateExtensions.cs': [5,16],
    'OpenPgpInvalidPinExceptionTests.cs': [1,19], 'OpenPgpSessionWireTests.cs': [1,8,18],
    'AlgorithmAttributesTests.cs': [18], 'PwStatusTests.cs': [18],
    'PrivateKeyTemplateTests.cs': [18], 'OpenPgpAuditReproTests.cs': [18],
    'KeysAttestCommand.cs': [12,15], 'InfoCommand.cs': [15],
    'CertificatesImportCommand.cs': [15], 'CertificatesExportCommand.cs': [15],
    'ResetCommand.cs': [4,5,15], 'KeysSetTouchCommand.cs': [4,10,15],
    'OpenPgpCommand.cs': [15,18], 'KeysImportCommand.cs': [15],
    'KeysGenerateCommand.cs': [9,10,13,15], 'DeviceSelector.cs': [15],
    'OpenPgpDeviceSelector.cs': [15], 'Program.cs': [4,15],
    'Yubico.YubiKit.OpenPgp.csproj': [17],
}
records = []
for entry in inventory['files']:
    path = Path(entry['path'])
    data = path.read_bytes()
    ids = file_findings.get(path.name, [])
    if '/tests/' in str(path) and path.name == 'CLAUDE.md':
        ids = []
    category = ('api-snapshot' if path.name.startswith('PublicAPI.') else
        'project-runner' if path.suffix in ('.csproj', '.json') else
        'narrative' if path.suffix == '.md' else
        'internal-test' if '/tests/' in str(path) else
        'example' if '/examples/' in str(path) else 'api-internal')
    records.append(dict(path=str(path), sha256=hashlib.sha256(data).hexdigest(),
        matches_inventory=hashlib.sha256(data).hexdigest() == entry['sha256'],
        lines=len(data.splitlines()), category=category, semantic_read='complete',
        reviewed_scope='entire file, including comments/XML, executable behavior and assertions where present',
        disposition='finding-bearing' if ids else 'investigator-clean',
        candidates=[f'PGP-C{i:03}' for i in ids], review_status='awaiting-independent-review'))

snapshot = (ROOT / 'src/OpenPgp/src/PublicAPI.Unshipped.txt').read_text().splitlines()
entries = [line for line in snapshot if line.strip() and not line.startswith('#')]
types = [line for line in entries if re.fullmatch(r'Yubico\.YubiKit\.OpenPgp\.[\w.]+', line)]
source = (ROOT / 'src/OpenPgp/src/IOpenPgpSession.cs').read_text()
operations = re.findall(r'\b(\w+Async)\s*\(', source)
operations = list(dict.fromkeys(operations))
assert len(entries) == 423 and len(types) == 39 and len(operations) == 40
api_records = []
for index, line in enumerate(snapshot, 1):
    if not line.strip() or line.startswith('#'):
        continue
    kind = ('type' if line in types else 'extension-container' if '.extension(' in line and ' -> ' not in line else
        'property-accessor' if re.search(r'\.(get|init|set) ->', line) else
        'enum-value' if ' = ' in line and ' -> Yubico.YubiKit.OpenPgp.' in line else
        'feature-field' if line.startswith('static readonly ') else 'callable-or-extension-representation')
    api_records.append(dict(line=index, entry=line, kind=kind,
        semantic_disposition='read; share owning type/property/member-family contract; preserve snapshot'))
(HERE / 'api-entry-reconciliation.json').write_text(json.dumps(api_records, indent=2) + '\n')

nominees = [
    ('api', 'src/OpenPgp/src/GenerationTimes.cs', '19-50', 'read-only dictionary contract and Unix-second values'),
    ('api', 'src/OpenPgp/src/KeyInformation.cs', '19-50', 'key status read-only dictionary contract'),
    ('api', 'src/OpenPgp/src/KeyFingerprints.cs', '19-50', '20-byte fingerprint read-only dictionary contract'),
    ('api', 'src/OpenPgp/src/SupportedAlgorithm.cs', '17-36', 'one supported algorithm entry; immutable properties'),
    ('api', 'src/OpenPgp/src/KeyRef.cs', '48-89', 'ordinary Sig/Dec/Aut data-object mappings; ATT CRT excluded by C013'),
    ('narrative', 'src/OpenPgp/PACKAGE_README.md', '1-24', 'package purpose, alpha qualification, installation and doc links'),
    ('narrative-example', 'src/OpenPgp/README.md', '66-80', 'metadata lookup fragment and named read-only collection explanation'),
    ('narrative', 'README.md', '64-64', 'OpenPGP package table row'),
    ('internal-test', 'src/OpenPgp/src/BcdHelper.cs', '22-55', 'decode and checked serial nibble interpretation; caller-size scope'),
    ('internal-test', 'src/OpenPgp/src/DigestInfo.cs', '29-67', 'four DER headers and header+digest builder'),
    ('internal-test', 'src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/DigestInfoTests.cs', '21-91', 'exact header and concatenation assertions'),
    ('internal-test', 'src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpCollectionContractTests.cs', '21-34', 'actual read-only-interface/no-public-constructor assertions'),
    ('internal-test', 'src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/ApplicationRelatedDataTests.cs', '153-204', 'lookup/unknown-status/independent-fingerprint-storage assertions'),
    ('api', 'src/OpenPgp/src/IOpenPgpSession.cs', '311-333', 'precise certificate absence/read/parse distinction and disposal'),
]
clean_units = [dict(id=f'PGP-CLEAN-{i:03}', category=cat, path=path, lines=lines, semantic_unit=unit,
    source_sha256=hashlib.sha256((ROOT/path).read_bytes()).hexdigest(),
    disposition='investigator-clean', sample_status='nominee-unsampled',
    final_eligibility='remove if editor changes this unit; coordinator samples exact final hash')
    for i, (cat,path,lines,unit) in enumerate(nominees, 1)]
eligible = [dict(id=f'PGP-FILE-CLEAN-{i:03}', path=r['path'], lines=f"1-{r['lines']}",
    category=r['category'], semantic_unit='complete clean file; generated boilerplate is not a separate unit',
    source_sha256=r['sha256'], disposition='investigator-clean', sample_status='unsampled')
    for i,r in enumerate(records,1)
    if r['disposition']=='investigator-clean' and r['category'] not in ('api-snapshot','project-runner')]
eligible_paths = {u['path'] for u in eligible}
eligible.extend(u for u in clean_units if u['path'] not in eligible_paths)

hw = []
for path in sorted((ROOT/'src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests').glob('OpenPgp*Tests.cs')):
    text = path.read_text()
    starts = list(re.finditer(r'public async Task (\w+)\(YubiKeyTestState state\)', text))
    for i, match in enumerate(starts):
        body = text[match.start(): starts[i+1].start() if i+1<len(starts) else len(text)]
        # Attributes before this method belong to it; slice back to preceding test marker.
        marker = text.rfind('[SkippableTheory]', 0, match.start())
        attrs = text[marker:match.start()].strip()
        body = body[:body.rfind('[SkippableTheory]')] if '[SkippableTheory]' in body else body
        name = match.group(1)
        fqn = f'Yubico.YubiKit.OpenPgp.IntegrationTests.{path.stem}.{name}'
        fw = re.search(r'MinFirmware = "([^"]+)"', attrs)
        hw.append(dict(path=str(path.relative_to(ROOT)), line=text[:match.start()].count('\n')+1,
            fully_qualified_name=fqn, filter=f'FullyQualifiedName={fqn}',
            command=f'dotnet toolchain.cs -- test --integration --project OpenPgp --filter "FullyQualifiedName={fqn}"',
            connection_filter='SmartCard', min_firmware=fw.group(1) if fw else None,
            reset_before_requested='resetBeforeUse: true' in body,
            setup='shared helper reset-before then fresh borrowed-connection session; skipped entirely on four early-return paths',
            cleanup='session/connection scopes only; no helper post-reset; body restores only what it explicitly restores',
            slow='TestCategories.Slow' in attrs, user_presence_trait=False,
            early_return_range='4.2.0 <= firmware <= 4.3.5; body/setup unexecuted' if 'state.FirmwareVersion >=' in body else None,
            source_attributes=attrs, method_source=body.rstrip(), execution_status='not-run-by-investigator'))
assert len(hw)==49 and all(h['reset_before_requested'] for h in hw)
assert sum(h['slow'] for h in hw)==2 and sum(bool(h['early_return_range']) for h in hw)==4
(HERE/'hardware-methods.json').write_text(json.dumps(hw, indent=2)+'\n')

shared = [
    ('README.md','OpenPGP row64','clean'),
    ('docs/v2-highlights.md','OpenPGP32-36; package/prompt references','C020 chronology qualification'),
    ('docs/v1-to-v2-comparison.md','OpenPGP exception/secret/new module references125-161','C019,C020'),
    ('docs/migration/v1-to-v2.md','OpenPGP factory/PIN rename/return tables and752-759','C019,C020'),
    ('docs/migration/v1-to-v2-map.yml','OpenPGP package, exception, return/typed-result records','C019,C020'),
    ('docs/migration/v1-to-v2-gaps.md','OpenPGP occurrences29,34,372,381 and historical framing','C020 current/historical qualification'),
    ('docs/usage/user-interaction.md','OpenPGP operation row and policy/lifetime165-219','clean behavior; useful module link C020'),
    ('docs/architecture/physical-device-model.md','OpenPGP transport168','clean'),
    ('docs/architecture/sdk-architecture-diagrams.md','OpenPGP dependency/transport/session shape','clean'),
    ('docs/NATIVE-AOT.md','OpenPGP support scope/link-only evidence1-40, future-work mention','clean; runtime remains unqualified'),
]
result = dict(baseline=inventory['baseline'], investigation_date='2026-11-04',
    scope='whole module and assigned shared OpenPGP sections',
    file_read_count=len(records), files=records,
    reconciliation=dict(public_type_count=39, public_types=types, snapshot_entry_count=423,
        snapshot_reconciliation='evidence/api-entry-reconciliation.json', interface_operation_count=40,
        interface_operations=[dict(name=op, semantic_read='complete with implementation and callers',
            contract_location='src/OpenPgp/src/IOpenPgpSession.cs', review_status='awaiting-independent-review') for op in operations],
        partial_class='seven declarations; one shipping OpenPgpSession type',
        extension='extension container/member and lowered static method share one public creation contract',
        effective_accessibility='internal private-key templates/import/backend members excluded from public count; Crt/DataObject included',
        generated='implicit constructors and accessor entries share owning type/property contract'),
    clean_semantic_unit_definition='each complete clean prose/source/test file, plus specifically bounded clean contracts in finding-bearing/shared files; duplicate file/subunit overlaps counted once; snapshots/project-runner syntax excluded from prose denominator',
    eligible_clean_semantic_units=len(eligible), minimum_sample_required=max(3,math.ceil(len(eligible)/10)),
    clean_semantic_units=eligible, clean_unit_nominees=clean_units,
    sampling_status='coordinator pending; 14 nominees supplied; sample at least 10% of final eligible units with minimum3, across categories, removing edited overlaps and avoiding duplicate samples',
    shared_sections=[dict(path=p, section=s, disposition=d, semantic_read='complete assigned section') for p,s,d in shared],
    exclusions=['Core and other-module source/docs investigation deferred; only referenced-target existence checked',
        'historical changelogs/planning/research retain historical meaning; no active-code edits',
        'license boilerplate, compiler-generated representations, API snapshots and runner syntax accounted without invented prose',
        'validation outputs independently owned; no build/test/hardware execution by investigator'],
    hardware=dict(methods=49, slow_traits=2, user_presence_traits=0, reset_before_requests=49,
        firmware_early_return_methods=4, helper_post_reset=False, manifest='evidence/hardware-methods.json'),
    unfinished_markers=dict(pattern='TODO|FIXME|HACK|XXX', scope='src/OpenPgp C#/Markdown', hits=0,
        meaning='marker absence only; candidate ledger retains implicit unfinished behavior'),
    candidate_ids=[f'PGP-C{i:03}' for i in range(1,22)],
    execution_steer=dict(source='coordinator message; investigator performed no device I/O',
        direct_python_session_version='0.0.1', cli_application_version='5.8.0', card_version='3.4',
        device_metadata='5.8 beta', mutation_status='coordinator started 48 non-KDF methods including Slow; results pending',
        att_public_certificate_baseline='complete in hardware/vendor-baseline-v2.json; explicit APDU reads; all four actual UIF Off',
        att_certificate_der_sha256='94afb703099edb4aaaee443c6e197c379f7f909d23f717f0868ef73057e4a16a',
        attestation_mode='supplemental probe extended; existing tests ordinary; exclusivity unproven', dependency='PGP-DEP-021',
        interpretation='version provenance and effective feature override compatibility question; no SDK defect presumed'))
(RUN/'coverage.json').write_text(json.dumps(result, indent=2)+'\n')
link_results=[]
for path in [Path('src/OpenPgp/README.md'),Path('src/OpenPgp/CLAUDE.md'),Path('src/OpenPgp/tests/CLAUDE.md')]:
    for label,target in re.findall(r'\[([^\]]+)\]\(([^)]+)\)', path.read_text()):
        if target.startswith(('https:','http:','#')):
            continue
        resolved=(path.parent/target.split('#',1)[0]).resolve()
        link_results.append(dict(source=str(path), label=label, target=target, exists=resolved.exists(),
            scope='existence only; target source/docs not investigated'))
(HERE/'local-link-existence.json').write_text(json.dumps(link_results,indent=2)+'\n')
source_manifest=json.loads((HERE/'source-manifest.json').read_text())
if not any(r['path']=='vendor-ysa-2017-01.html' for r in source_manifest):
    data=(HERE/'vendor-ysa-2017-01.html').read_bytes()
    source_manifest.append(dict(path='vendor-ysa-2017-01.html',
        url='https://www.yubico.com/support/security-advisories/ysa-2017-01/',
        accessed='2026-11-04',bytes=len(data),sha256=hashlib.sha256(data).hexdigest()))
    (HERE/'source-manifest.json').write_text(json.dumps(source_manifest,indent=2)+'\n')
evidence_index=[dict(path=str(p.relative_to(RUN)),bytes=p.stat().st_size,
    sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in sorted(HERE.iterdir())
    if p.is_file() and p.name!='evidence-index.json']
(HERE/'evidence-index.json').write_text(json.dumps(evidence_index,indent=2)+'\n')
print(json.dumps(dict(files=len(records), mismatches=sum(not r['matches_inventory'] for r in records),
    public_types=len(types), snapshot_entries=len(entries), interface_operations=len(operations), hardware_methods=len(hw),
    clean_eligible=len(eligible), minimum_sample=max(3,math.ceil(len(eligible)/10)), clean_nominees=len(clean_units),
    broken_local_links=sum(not r['exists'] for r in link_results)),indent=2))
