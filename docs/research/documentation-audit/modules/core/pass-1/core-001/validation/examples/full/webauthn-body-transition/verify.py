"""Verify archived predecessors/checkpoints only; never poll or admit live primary transfers."""
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

HERE=Path(__file__).resolve().parent
FULL=HERE.parent


def identity(path):
    raw=Path(path).read_bytes();return dict(path=str(path),bytes=len(raw),sha256=hashlib.sha256(raw).hexdigest())


def main():
    preserved=json.loads((HERE/'preservation.json').read_bytes())
    assert preserved['status']=='preserved-awaiting-explicit-completed-acceptance'
    participants={pair['original']['path']:pair for pair in preserved['participants']}
    for pair in preserved['participants']:
        assert identity(pair['snapshot']['path'])==pair['snapshot']
        assert pair['snapshot']['bytes']==pair['original']['bytes'] and pair['snapshot']['sha256']==pair['original']['sha256']
    for pair in preserved['source_and_authority_snapshots']:
        assert identity(pair['snapshot']['path'])==pair['snapshot']
        assert pair['snapshot']['bytes']==pair['original']['bytes'] and pair['snapshot']['sha256']==pair['original']['sha256']
        assert pair['matches_predecessor'] and pair['original']==pair['expected_predecessor']
    assert identity(preserved['predecessor_profile']['path'])==preserved['predecessor_profile']
    profile=json.loads((HERE/'before/reviewed-inputs-helper-imports-fixed.json.snapshot').read_bytes());fixed={row['path']:row for row in profile['files']}
    assert preserved['predecessor_profile']['sha256']=='52234498f7d167899c296daadec96dd0a543d9dbbd3f72c72007d1983fc099e7'
    manifest_path=FULL/'manifest-helper-imports-fixed.json'
    assert participants[str(manifest_path)]['original']==fixed[str(manifest_path)]
    manifest=json.loads((HERE/'before/manifest-helper-imports-fixed.json.snapshot').read_bytes());records={row['id']:row for row in manifest['records']}
    assert len(records)==662 and manifest['counts']['total_authority_records']==703
    assert identity(preserved['generated_sources_snapshot']['path'])==preserved['generated_sources_snapshot']
    snapshot=json.loads((HERE/'generated-sources.before.json.snapshot').read_bytes());assert snapshot['source_count']==len(snapshot['files'])==300
    sources={row['path']:row for row in snapshot['files']}
    for row in snapshot['files']:
        raw=row['content'].encode();assert len(raw)==row['bytes'] and hashlib.sha256(raw).hexdigest()==row['sha256']
        assert {k:row[k] for k in ('path','bytes','sha256')}==fixed[row['path']]
    outputs=0;occurrences=0;checkpoint_shas=[]
    for checkpoint in preserved['successful_contexts']:
        for row in checkpoint['records']+checkpoint['retained_outputs']:assert identity(row['path'])==row
        proof=json.loads(Path(checkpoint['accepted_compilation']['path']).read_bytes());assert proof['profile']==checkpoint['profile']
        assert identity(checkpoint['profile']['path'])==checkpoint['profile']
        receipt=json.loads(Path(proof['command_receipt']['path']).read_bytes())
        assert receipt['status']=='passed' and receipt['exit_code']==0 and receipt['result_counts']['warnings']==0
        assert proof['execution_status']=='not-run'
        outputs+=len(checkpoint['retained_outputs']);occurrences+=checkpoint['body_occurrences'];checkpoint_shas.append(checkpoint['accepted_compilation']['sha256'])
    assert len(preserved['successful_contexts'])==9 and occurrences==298 and outputs==169
    for row in preserved['failed_attempt_records']:assert identity(row['path'])==row
    receipt=json.loads((FULL.parents[1]/'commands-v2/examples-full-webauthn-first/receipt.json').read_bytes());assert receipt['status']=='failed' and receipt['exit_code']==1
    assert not (FULL/'attempts/examples-full-webauthn-first/accepted-compilation.json').exists()
    index=json.loads((HERE/'compilation-index.before.json').read_bytes());assert len(index['entries'])==300
    assert sum(row['accepted_checkpoint'] is not None for row in index['entries'])==297
    for row in index['entries']:
        source=sources[row['source_identity']['path']];assert row['source_identity']=={k:source[k] for k in ('path','bytes','sha256')}
        assert row['raw_body_sha256']==records[row['id']]['raw_sha256'] and row['decoded_body_sha256']==records[row['id']]['decoded_sha256']
        if row['accepted_checkpoint']:
            assert row['accepted_checkpoint']['sha256'] in checkpoint_shas and row['actual_compiler_input']==row['source_identity']
    aliases=json.loads((HERE/'authority-alias-map.before.json').read_bytes())
    assert [row['id'] for row in aliases['document_rows']]==['FULL-'+str(i).zfill(3) for i in range(31,39)]
    assert len(aliases['authority_guide_entries'])==8
    assert aliases['same_decoded_body_ids']==['FULL-032'] and not aliases['compilation_aliases_of_FULL032']
    assert len(aliases['shared_authority']['referencing_ids'])==len(aliases['shared_review']['referencing_ids'])==47
    assert len(aliases['generator_inventory_aliases']['current-inventory.json'])==len(aliases['generator_inventory_aliases']['final-current-inventory.json'])==8
    assert not aliases['generator_inventory_aliases']['external-current-inventory.json']
    diagnosis=json.loads((HERE/'diagnostic-to-authority.json').read_bytes());actual=json.loads((HERE/'actual-diagnostics.json').read_bytes())
    assert identity(actual['source_log']['path'])==actual['source_log'] and len(actual['unique_diagnostics'])==1
    assert actual['unique_diagnostics'][0]==diagnosis['diagnostic'] and diagnosis['diagnostic']['code']=='CS1929'
    target=records['FULL-032'];raw=(HERE/'before/src/WebAuthn/CLAUDE.md.snapshot').read_bytes()
    assert hashlib.sha256(raw).hexdigest()==target['document']['sha256']
    assert raw[target['raw_start_byte']:target['raw_end_byte']].decode()==target['raw_body']==diagnosis['original_raw_body']
    assert target['decoded_body'].splitlines()[0] in target['compiled']['prefix']
    assert sources[diagnosis['diagnostic']['path']]['content'].splitlines()[28]==diagnosis['actual_generated_line']
    api=(HERE/'before/src/Core/src/YubiKitLogging.cs.snapshot').read_text()
    assert 'internal static ILogger<T> CreateLogger<T>()' in api and 'public static ILoggerFactory LoggerFactory' in api
    friends=[node.attrib['Include'] for node in ET.fromstring((HERE/'before/src/Core/src/Yubico.YubiKit.Core.csproj.snapshot').read_bytes()).findall('.//InternalsVisibleTo')]
    assert friends==diagnosis['core_friend_assemblies'] and 'Yubico.YubiKit.WebAuthn' not in friends and 'Yubico.YubiKit.WebAuthn.IntegrationTests' not in friends
    ledger=json.loads((HERE/'pending-admission.json').read_bytes());assert ledger['status']=='BLOCKED_AWAITING_EXPLICIT_COMPLETED_KNOWN_TRANSFER_MESSAGE'
    assert ledger['planned_body_change_ids']==['FULL-032'] and not ledger['new_profile_created'] and not ledger['body_changed_by_worker'] and not ledger['polling_performed']
    assert all(not row['independently_admitted'] for row in ledger['awaited_transitions'])
    assert identity(ledger['pilot_manifest_snapshot']['path'])==ledger['pilot_manifest_snapshot']
    assert ledger['pilot_manifest_snapshot']['sha256']==manifest['pilot']['body_manifest']['sha256']
    for label in ledger['future_retry_labels']:assert not (FULL/'attempts'/label).exists() and not (FULL.parents[1]/'commands-v2'/label).exists()
    return dict(scope='Archival snapshots/immutable records/retained checkpoint files only; no live primary polling, transfer admission, compiler or test execution.',
                original_generated_sources=300,original_authority_records=703,accepted_contexts=9,accepted_body_occurrences=298,
                accepted_distinct_compilations=297,retained_local_files=169,failed_wa_record_identities=len(preserved['failed_attempt_records']),
                diagnostic='CS1929: TRUE FULL-032 documentation access defect, not wrapper context.',
                wa_document_alias_rows=8,compiled_source_document_alias_rows=2,shared_authority_and_review_identity_rows=47,
                same_body_or_compilation_aliases_of_FULL032=0,original_manifest_and_profiles_untouched=True,
                no_body_wrapper_profile_or_qualified_tool_change=True,
                final_admission='Blocked until explicit completed known-transfer acceptance covering queued patches, reviewed EOF patch, WA body/prose/root bullet and formatter qualifications.',
                qualification='EOF outside-body placement and the 299 unchanged-body matches remain future verification requirements, not established by this archival check.')


if __name__=='__main__':print(json.dumps(main(),indent=2))
