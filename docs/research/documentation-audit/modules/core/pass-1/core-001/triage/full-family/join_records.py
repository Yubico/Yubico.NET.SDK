"""Join completed family records; no source-body or standards investigation.

Writes only this directory. The preliminary join is read as evidence, never executed.
"""
import csv
import hashlib
import json
import re
from collections import Counter, defaultdict
from pathlib import Path

OUT = Path(__file__).resolve().parent
RUN = OUT.parents[1]
ROOT = RUN.parents[6]
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
FAMILIES = ('A', 'B1', 'B2', 'B3', 'B4', 'B5', 'B6', 'C')
inputs = {}


def read(path):
    raw = (RUN / path).read_bytes()
    inputs[path] = hashlib.sha256(raw).hexdigest()
    return raw.decode('utf-8-sig')


def load(path):
    return json.loads(read(path))


def save(name, value):
    (OUT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n')


preserved = load('triage/full-family/preliminary-preservation.json')
for path, digest in preserved.items():
    read(path)
    assert inputs[path] == digest, ('preliminary preservation', path)
prior = load('triage/preliminary/map.json')
prior_inherited = load('triage/preliminary/inherited-links.json')
assert len(prior['proposals']) == 67
old_locals = {x['local_id']: x for p in prior['proposals'] for x in p['local_records']}
assert len(old_locals) == 68
annotations = []
for path in ('triage/preliminary/proposals.tsv', 'triage/full-family/additional-proposals.tsv'):
    annotations.extend(csv.DictReader(read(path).splitlines(), delimiter='\t'))
assert len(annotations) == len({x['local_id'] for x in annotations})
annotation_by_id = {x['local_id']: x for x in annotations}
inventory = {x['path']: x for x in load('inventory/files.json')}
inv_units = {x['id']: x for x in load('inventory/semantic-units.json')}
shared = load('inventory/shared-sections.json')
registry = load('inherited-findings/registry.json')
routing = load('inventory/inherited-routing.json')
assert registry['source_baseline'] == PIN
assert routing['registry_sha256'] == inputs['inherited-findings/registry.json']
assert prior_inherited['registry_sha256'] == inputs['inherited-findings/registry.json']
assert prior_inherited['groups_preserved'] == registry['groups']
for earlier, original in zip(prior_inherited['rows'], registry['rows']):
    assert earlier['key'] == original['key']
    assert earlier['state_preserved'] == original['current_state']
    assert earlier['qualification_preserved'] == original['qualification']

for path in ('plan.md', 'status.md', 'inventory.md', 'dispatch-map.md', 'inherited-findings.md',
             'inherited-findings/boundary-seeds.md', 'investigation/B1/triage-clarifications.md',
             'investigation/C/triage-clarifications.md'):
    read(path)
registers = {'A': 'investigation/A/sources/notes.md', 'C': 'investigation/C/sources/final-source-register.md',
             'B6': 'investigation/B6/sources/final-source-register.md'}
for family in ('B1', 'B2', 'B3', 'B4', 'B5'):
    registers[family] = f'investigation/{family}/sources/register.md'
family_units = {}
stats = {}
candidate_index = {}
definitions = {cid: x['original_candidate'] for cid, x in old_locals.items()}
family_report_refs = {}

for family in FAMILIES:
    report_path = f'investigation-{family}.md'
    text = read(report_path)
    read(registers[family])
    if family == 'A':
        ledger = load('investigation/A/unit-results.json')
        family_units[family] = [dict(x, id=k) for k, x in ledger['units'].items()]
        stats[family] = ledger['summary']
        authoritative_ids = {x['id'] for x in load('investigation/A/findings.json')}
    else:
        family_units[family] = load(f'investigation/{family}/unit-dispositions.json')
        stats[family] = load(f'investigation/{family}/' + ('final-summary.json' if family in ('C', 'B6') else 'coverage-summary.json'))
        if family == 'C':
            authoritative_ids = {x['id'] for x in load('investigation/C/findings.json')['candidates']}
            read('investigation/C/final-review.json')
        elif family in ('B4', 'B5', 'B6'):
            index = load(f'investigation/{family}/candidate-index.json')
            candidate_index.update({x['id']: x for x in index})
            authoritative_ids = {x['id'] for x in index}
        else:
            authoritative_ids = {cid for cid in old_locals if cid.split('-')[1] == family}
    family_report_refs[family] = report_path
    assert authoritative_ids == {cid for cid in annotation_by_id if cid.split('-')[1] == family}, family
    if family in ('B4', 'B5', 'B6'):
        path = f'investigation/{family}/candidates.md'
        text = read(path)
        # B6 headings use Cnnn; normalize only the lookup key, retaining original heading.
        heads = list(re.finditer(r'^## ((?:CORE-B[456]-)?C\d{3}) — (.+)$', text, re.M))
        for n, match in enumerate(heads):
            cid = match[1] if match[1].startswith('CORE-') else f'CORE-{family}-{match[1]}'
            end = heads[n + 1].start() if n + 1 < len(heads) else len(text)
            definitions[cid] = {'id': cid, 'title': match[2], 'original_heading': match[0],
                'definition': {'path': path, 'first_line': text[:match.start()].count('\n') + 1,
                               'last_line': text[:end].count('\n')},
                'candidate_index': f'investigation/{family}/candidate-index.json'}
        assert len(heads) == len(authoritative_ids), family

assert set(definitions) == set(annotation_by_id)
paths_by_id = defaultdict(set)
for cid, local in old_locals.items():
    paths_by_id[cid].update(x['path'] for x in local['documentation_surface_refs'])
for cid, item in candidate_index.items():
    paths_by_id[cid].update(item.get('unchanged_primary_or_shared_paths', item.get('direct_paths', [])))
    paths_by_id[cid].update(x['path'] for x in item.get('candidate_associated_unchanged_paths', []))
for item in load('investigation/B6/unchanged-paths.json'):
    for cid in item['candidate_ids']:
        paths_by_id[cid].add(item['path'])

# These are documentation surfaces explicitly named in the completed reports.
# Narrow dependencies keep their approved primary editor; no full-file read is inferred.
extra_docs = {
    'CORE-B4-C001': ['docs/architecture/connection-ownership-and-contention.md', 'docs/architecture/raw-access-tiers.md'],
    'CORE-B4-C004': ['docs/architecture/connection-ownership-and-contention.md'],
    'CORE-B4-C007': ['docs/LOGGING.md', 'TOOLCHAIN.md'],
    'CORE-B4-C008': ['docs/NATIVE-AOT.md', 'docs/v2-highlights.md', 'TOOLCHAIN.md'],
    'CORE-B4-C009': ['docs/v2-highlights.md'],
    'CORE-B6-C002': ['docs/MEMORY-MANAGEMENT.md', 'CLAUDE.md'],
    'CORE-B6-C014': ['docs/LOGGING.md'],
    'CORE-B6-C015': ['CLAUDE.md', 'docs/architecture/raw-access-tiers.md'],
}
for cid, paths in extra_docs.items():
    paths_by_id[cid].update(paths)

evidence = {}
refs_by_id = defaultdict(list)
all_units = []
for family, selected in family_units.items():
    for x in selected:
        uid = x.get('id', x.get('unit_id'))
        inv = inv_units[uid]
        source_hash = x.get('inventory_source_sha256', x.get('original_source_sha256', x.get('source_sha256')))
        range_hash = x.get('original_range_sha256', x.get('range_sha256'))
        assert x['path'] == inv['path'] and source_hash == inv['source_sha256'] and range_hash == inv['range_sha256'], (family, uid)
        for k, expected in (('current_source_sha256', source_hash), ('current_range_sha256', range_hash), ('actual_range_sha256', range_hash)):
            if k in x:
                assert x[k] == expected, (family, uid, k)
        status = x.get('status', x.get('disposition'))
        assert status and 'UNREAD' not in status and 'PENDING-CONTEXT' not in status
        all_units.append(uid)
        cids = set(x.get('candidate_ids', []) + x.get('associated_candidate_ids', []))
        if cids:
            evidence[uid] = {'path': x['path'], 'first_line': x['first_line'], 'last_line': x['last_line'],
                'start_character': x['start_character'], 'end_character_exclusive': x['end_character_exclusive'],
                'old_source_sha256': source_hash, 'current_source_sha256': source_hash,
                'old_range_sha256': range_hash, 'current_range_sha256': range_hash,
                'family_disposition': status, 'primary_investigator': family,
                'ledger_path': f'investigation/{family}/' + ('unit-results.json' if family == 'A' else 'unit-dispositions.json')}
            for cid in cids:
                assert cid in definitions, (uid, cid)
                refs_by_id[cid].append({'unit_id': uid,
                    'relation': 'direct' if cid in x.get('candidate_ids', []) else 'related-context'})
                paths_by_id[cid].add(x['path'])
assert len(all_units) == len(set(all_units)), 'family assigned-unit ownership overlap'
assert all(refs_by_id[cid] for cid in definitions)
index_checks = {}
for cid, item in candidate_index.items():
    indexed = set(item.get('unit_ids', item.get('direct_unit_ids', [])))
    direct = {x['unit_id'] for x in refs_by_id[cid] if x['relation'] == 'direct'}
    assert indexed == direct, ('candidate index membership', cid)
    index_checks[cid] = len(direct)

surfaces = {}
for path in sorted({p for paths in paths_by_id.values() for p in paths}):
    inv = inventory[path]
    current = hashlib.sha256((ROOT / path).read_bytes()).hexdigest()
    assert current == inv['sha256'], ('product drift', path)
    surfaces[path] = {'old_source_sha256': inv['sha256'], 'current_source_sha256': current,
        'git_blob_at_pin': inv['git_blob'], 'inventory_primary_family': inv['assigned_family'],
        'inventory_stage': inv['stage'], 'suggested_editor': inv['future_editor_owner'],
        'shared_ranges': [{k: x[k] for k in ('unit_id', 'first_line', 'last_line', 'investigation_owner',
             'investigation_stage', 'future_editor_owner')} for x in shared if x['path'] == path],
        'scope_limit': 'Only candidate-anchored units are finding evidence; neighboring shared ranges identify ownership, not new acceptance.',
        'disposition': 'Fresh editor/reviewer must explicitly retain, qualify or edit after Astra release; product unchanged.'}

# Scoped duplicate facets stay nonadditive even when their compound parent groups
# also contain different questions. Only the already explicit ambient-policy group
# has identical whole-group responsibility; no keyword/symptom joins are inferred.
facet_links = [
    {'locals': ['CORE-B1-C010', 'CORE-B3-C006'], 'relation': 'whole-group-consolidation',
     'scope': 'Caller ExecutionContext retention policy on normally started rooted threads; per-route evidence retained.'},
    {'locals': ['CORE-B1-C005', 'CORE-B4-C001'], 'relation': 'nonadditive-family-extension-with-distinct-submechanisms',
     'scope': 'Registered HID failure/release policy. B1 includes arbitrary custom throwing teardown; B4 adds successful managed Dispose with deferred active native borrow. Compound groups retained separately.'},
    {'locals': ['CORE-C-C020', 'CORE-B6-C007'], 'relation': 'duplicate-contained-owner-facet',
     'scope': 'Exact Scp03/11 contained object retention/disposal and record-clone alias lifetime; B6 owns wire-side documentation. C020 also contains optional-reference and caller-option questions.'},
    {'locals': ['CORE-C-C020', 'CORE-B6-C009'], 'relation': 'duplicate-constructor-versus-initializer-facet',
     'scope': 'Optional OCE reference and wider constructor shapes are the same referred question; B6 adds actual profile/trust/key roles.'},
    {'locals': ['CORE-C-C008', 'CORE-B6-C014'], 'relation': 'same-global-logging-policy-claim-with-template-qualification',
     'scope': 'B6 header/length-only ordinary trace narrows C008 initial payload wording. C008 factory retention, temporary scopes and callback exceptions remain separate facets.'},
    {'locals': ['CORE-B1-C011', 'CORE-B6-C013'], 'relation': 'duplicate-shared-fake-example-correction',
     'scope': 'QueueResponse/SentCommands prose corrected to EnqueueResponse/TransmittedCommands once by SHARED; B6 C013 has other test/fixture concerns.'},
    {'locals': ['CORE-A-C020', 'CORE-B4-C008'], 'relation': 'shared-missing-historical-Windows-artifact-facet',
     'scope': 'Exact old Windows runtime identity gap; A Rust/watcher claims, B4 current gates and B3 Mac producer evidence remain separate.'},
]
separations = [
    {'locals': ['CORE-A-C003', 'CORE-C-C005', 'CORE-C-C006', 'CORE-B5-C005', 'CORE-B6-C002', 'CORE-B4-C007'],
     'reason': 'Metadata/TLV alias, helper abandoned copies, incoming reports, APDU copies and Windows report-ID staging are different storage owners. Coordinate one memory topology, preserve every copy/failure facet.'},
    {'locals': ['CORE-A-C017', 'CORE-B1-C012', 'CORE-B4-C001'],
     'reason': 'USB ProductId metadata-cache validity, member-acquisition precedence and native handle/registration release have different mechanisms; PID/lease vocabulary supplies no merge.'},
    {'locals': ['CORE-B1-C009', 'CORE-C-C008', 'CORE-B6-C014'],
     'reason': 'Post-open logger cleanup failure is distinct from payload redaction and logging configuration/template provenance.'},
    {'locals': ['CORE-B3-C001', 'CORE-B3-C009', 'CORE-B4-C003', 'CORE-B4-C006', 'CORE-B4-C009'],
     'reason': 'IOReturn constants, CFNumberType carrier, Windows indirection/Boolean, conditional Linux PCI and backend error mapping have distinct declaration/responsibility scopes.'},
    {'locals': ['CORE-A-C004', 'CORE-A-C006', 'CORE-C-C010', 'CORE-C-C015', 'CORE-B5-C003'],
     'reason': 'Application masks, mathematical key checks, crypto/COSE catalog applicability and CTAPHID INIT capabilities differ in actor and contract.'},
    {'locals': ['CORE-B6-C004', 'CORE-B6-C005', 'CORE-B6-C011', 'CORE-B6-C012'],
     'reason': 'Mutation/failure latch, instruction-only response exemption, empty-command encryption and protected-response transcript validity remain independent wire/phase requirements.'},
    {'locals': ['CORE-A-C020', 'CORE-C-C018', 'CORE-B5-C009'],
     'reason': 'Missing discovery Rust source, ARKG generator and session comparator need separate original provenance. Current Python is not a substitute identity.'},
]
save('relationships.json', {'whole_group_rule': 'Same precise mechanism/responsibility only; facet links never erase distinct questions.',
    'links': facet_links, 'explicit_separations': separations})

# Latest report-qualified corrections, not new source conclusions.
updates = {
    'CORE-B1-C001': ('D,Q,B', 'Describe strict sharing and requested UNPOWER with application-state conditions. Latest B1 clarification: Exa-attributed NIST passage text, direct PDF 403; independent original-PDF review remains required.',
                     'Original NIST PDF review before revised normative wording; no physical isolation/power result or permission for deferred PIV work.'),
    'CORE-A-C008': ('D,Q', None, 'Completed B1-B4 supply source handoffs; stronger logger resilience still requires A-E03 or selected implementation work.'),
    'CORE-A-C009': ('D,Q,B', None, 'B4 source is complete; current topology/ABI conditions can be documented. Native evidence and the future user-ready Windows delta are separate qualifications.'),
    'CORE-B1-C005': ('D,M,Q,B', None, 'Completed B4 C001 supplies the pinned Windows route, with custom-throw and built-in-deferred-close facets kept distinct; future checked-release policy/delta remains open.'),
    'CORE-B1-C007': ('D,Q,B', None, 'B2-B6 source routes are available. Reopen after mapping/wake/quarantine/workflow change; each applet still dispositions its exact caller.'),
    'CORE-B1-C011': ('D', None, 'B6 C013 confirms the same shared fake-name correction; compile actual internal test-context examples later, with async-dispose limitations preserved.'),
    'CORE-B2-C004': ('D,E,P,M,Q', None, 'Completed B4 supports loader/package boundary. Minimal deployment probe only before runtime assurance; stable soname/resolver remains selected Pass 2.'),
    'CORE-C-C007': (None, 'Describe bounded smart-card tag/length handling. Latest C clarification confirms inspected X.690 passages; old confidence wording is historical. Single-item remainder, tag round-trip and negative length remain separate policy/engineering facets.', None),
    'CORE-C-C008': (None, 'Explain factory/static-logger/temporary scope ownership and opaque exceptions. Completed B6 C014 records ordinary APDU header/length trace, not payload; SELECT caller bytes and exception provenance remain separate.',
                     'Reconcile the exact accepted template/payload claim with B6/Security Domain boundaries; global redaction assurance remains unsupported.'),
    'CORE-C-C011': (None, 'Remove no-added-exposure rationale; latest C clarification narrows .NET v10.0.0 macOS extra-D derivation to absent Q. Supplying Q skips that checked route; SDK intermediates and actual installed-provider cleanup remain separate.', None),
    'CORE-C-C013': (None, 'Separate provider disposal and every SDK copy owner. Latest C clarification settles pinned AsnWriter Encode retaining writer storage and Reset clearing its written prefix; installed-provider/exception cleanup qualifications remain.', None),
    'CORE-C-C020': (None, None, 'B6 C007/C009 now provide completed containment/initializer/profile evidence. Clone/OCE policy and actual applet retention/option propagation remain decisions/boundary work.'),
}
b4_continuations = {
    'CORE-B4-C004': [[523, 539]], 'CORE-B4-C005': [[417, 431]],
    'CORE-B4-C006': [[433, 457]], 'CORE-B4-C007': [[459, 470]],
    'CORE-B4-C008': [[472, 521]], 'CORE-B4-C009': [[523, 539]],
    'CORE-B4-C010': [[541, 597]],
}

groups = []
local_to_fpt = {}
ordered_ids = sorted(definitions, key=lambda x: (FAMILIES.index(x.split('-')[1]), x))
for cid in ordered_ids:
    if cid == 'CORE-B3-C006':
        continue
    members = [cid, 'CORE-B3-C006'] if cid == 'CORE-B1-C010' else [cid]
    annotation = dict(annotation_by_id[cid])
    # Earlier H flags waited for then-active families. All families are now complete;
    # B flags hold only dependent cross-layer acceptance, not pinned prose generally.
    annotation['facets'] = annotation['facets'].replace('H', 'B')
    if cid in updates:
        facets, proposal, trigger = updates[cid]
        if facets:
            annotation['facets'] = facets
        if proposal:
            annotation['proposal_and_rationale'] = proposal
        if trigger:
            annotation['reopen_trigger_or_needed_probe'] = trigger
    fpt = f'FPT-{len(groups) + 1:03d}'
    group = {'proposal_id': fpt, 'local_ids': members, 'accepted_id': None,
        'facets': annotation['facets'].split(','), 'topology': annotation['topology'],
        'proposal_and_rationale': annotation['proposal_and_rationale'],
        'implementation_reopen_trigger_or_needed_probe': annotation['reopen_trigger_or_needed_probe'],
        'dependencies_named_by_records': annotation['dependencies'],
        'dependency_status_rule': 'All named Core family returns are completed source inputs. B refers to dependent boundary acceptance; future Windows delta remains unready and does not block all pinned documentation.',
        'local_records': []}
    for member in members:
        local_to_fpt[member] = fpt
        family = member.split('-')[1]
        latest = []
        if family == 'C':
            latest = ['investigation/C/final-review.json', 'investigation/C/triage-clarifications.md']
        if member == 'CORE-B1-C001':
            latest = ['investigation/B1/triage-clarifications.md']
        group['local_records'].append({'local_id': member, 'original_definition': definitions[member],
            'completed_report': family_report_refs[family], 'qualified_source_register': registers[family],
            'latest_authority_addenda': latest,
            'continuation_scope': 'B4 candidates.md lines 408-597 add facets to existing IDs, not new groups.' if family == 'B4' else None,
            'continuation_anchors': [{'path': 'investigation/B4/candidates.md', 'first_line': a, 'last_line': b}
                                     for a, b in b4_continuations.get(member, [])],
            'evidence_unit_refs': refs_by_id[member], 'documentation_surface_refs': sorted(paths_by_id[member]),
            'disposition': {'documentation': 'Proposed; unchanged product, fresh editor/reviewer pending',
                'implementation': 'Unchanged; concerns and intended policy remain distinct',
                'independent_source_review': 'Future fresh full-unit and original-citation acceptance',
                'runtime_native_hardware': 'No new execution in triage or family investigation'},
            'original_index_fields': candidate_index.get(member)})
    groups.append(group)

# Copy every inherited original row; retain original authority/status/aliases exactly.
# New associations are explicit mechanism referrals, never a rewrite of old outcomes.
referrals = {
    'pcsc-pin-isolation': ['CORE-B4-C004'],
    'scp-parameter-ownership': ['CORE-B6-C007', 'CORE-B6-C009', 'CORE-B5-C001'],
    'scp-auth-readiness': ['CORE-B5-C001', 'CORE-B6-C009', 'CORE-B6-C010'],
    'scp-effective-version': ['CORE-B5-C001', 'CORE-B6-C003'],
    'shared-exchange-ownership': ['CORE-B5-C004', 'CORE-B5-C006', 'CORE-B6-C003'],
    'apdu-tlv-ownership': ['CORE-B5-C005', 'CORE-B6-C002'],
    'logging-payload-policy': ['CORE-B6-C014', 'CORE-B4-C007'],
    'otp-hid-framing': ['CORE-B5-C007', 'CORE-B5-C005'],
    'fido-config-initialization': ['CORE-B5-C001', 'CORE-B5-C008'],
    'scp-short-response': ['CORE-B6-C006', 'CORE-B6-C013'],
    'scp-response-authentication': ['CORE-B6-C005', 'CORE-B6-C011', 'CORE-B6-C012'],
    'migration-yaml': ['CORE-B4-C010', 'CORE-B5-C009'],
    'toolchain-host-selection': ['CORE-B4-C008'],
    'discovery-abandonment': ['CORE-B4-C008'],
}
old_backstop = {x['key']: x for x in prior_inherited['rows']}
inherited = []
for row in registry['rows']:
    earlier = old_backstop[row['key']]
    earlier_ids = list(earlier['local_alias_associations'])
    for pt in earlier['related_proposal_ids']:
        earlier_ids.extend(next(p['local_ids'] for p in prior['proposals'] if p['proposal_id'] == pt))
    associated_ids = sorted(set(earlier_ids + referrals.get(row['dedup_group'], [])))
    route = next(x for x in routing['groups'] if x['group'] == row['dedup_group'])
    inherited.append({'key': row['key'], 'original_registry_row': row,
        'existing_group': row['dedup_group'], 'routing': route,
        'related_local_ids': associated_ids,
        'related_proposal_ids': sorted({local_to_fpt[x] for x in associated_ids}),
        'relation_limit': 'Association preserves old exact scope; includes resolved-source records and distinct residual questions. No automatic alias, new defect count or boundary closure.'})
family_inherited = {}
for family in ('C', 'B5', 'B6'):
    data = load(f'investigation/{family}/inherited-dispositions.json')
    rows = data.get('rows', []) if isinstance(data, dict) else data
    family_inherited[family] = rows
    assert all(x['key'] in old_backstop for x in rows)
for group in groups:
    group['inherited_association_keys'] = [x['key'] for x in inherited if group['proposal_id'] in x['related_proposal_ids']]
save('inherited-links.json', {'source_baseline': PIN, 'registry_sha256': inputs['inherited-findings/registry.json'],
    'rows': inherited, 'groups_preserved': registry['groups'],
    'openpgp_documentation_dependencies_preserved': registry['openpgp_documentation_dependencies'],
    'separate_pgp_label_keys': routing['separately_preserved_pgp_documentation_labels'],
    'family_recommendations_preserved': family_inherited,
    'counting': '100 old provenance rows / 70 existing groups / 21 separate PGP labels; no additive Core bug total.'})

# Completed boundary follow-ups supply dependency flags only. No local boundary IDs
# enter this family's candidate set, evidence denominator or group membership.
boundary_inputs = ['boundaries/Management/followup-B4.md', 'boundaries/Management/followup-B6.md',
                   'boundaries/Ctap2/followup-B4-B6.md']
for path in boundary_inputs:
    read(path)
save('boundary-dependencies.json', {'mode': 'Dependency flags only; all nine returns feed later final triage.',
    'boundary_candidate_ids_imported': 0, 'boundary_units_counted': 0,
    'inputs': boundary_inputs,
    'observations': [
        {'boundary': 'Management', 'latest_input': boundary_inputs[0],
         'source_followup': 'B4 source reconciliation follows completed B6 reconciliation. Earlier B6 addendum OPEN-B4 wording is historical.',
         'impact': 'Empty SCP read/reset and Le/copy/auth-phase questions apply; normal session drain differs from direct concurrent Windows disposal; ordinary Management INS does not invoke later INS82 exemption or public reselect.',
         'remaining': 'Candidate acceptance, independent review, profile/native/device evidence and ready Windows delta; no boundary closure by this ledger.'},
        {'boundary': 'Ctap2', 'latest_input': boundary_inputs[2],
         'source_followup': 'Pinned B4/B6 mechanism follow-ups source-reconciled with qualifications.',
         'impact': 'Ordinary CTAP payload has command byte; empty SCP path occurs in remaining commands. Ordinary INS10/C0 avoids direct INS82 exemption. No post-setup factory command proves first protected-command auth phase or actual applet selection survival.',
         'remaining': 'WebAuthn actor/recovery/result dependency, independent acceptance, profile/device/native evidence and ready Windows delta.'}],
    'rule': 'Completed source follow-up is positive dependency evidence, not final adjudication of boundary-local groups or the nine-packet gate.'})

count_by_family = Counter(cid.split('-')[1] for cid in definitions)
assert len(definitions) == 103 and len(groups) == 102
assert count_by_family == {'A': 21, 'B1': 12, 'B2': 6, 'B3': 9, 'B4': 10, 'B5': 9, 'B6': 16, 'C': 20}
assert len(inherited) == len({x['key'] for x in inherited}) == 100
assert len(registry['groups']) == 70
assert len(local_to_fpt) == len(definitions)
assert all(g['accepted_id'] is None for g in groups)
save('map.json', {'source_baseline': PIN, 'provisional': True, 'local_ids_changed': False,
    'accepted_ids_allocated': 0, 'local_candidate_count': len(definitions), 'proposal_group_count': len(groups),
    'facets': {'D': 'Documentation-ready proposal subject to independent review',
        'E': 'Bounded experiment before stronger wording', 'M': 'Maintainer decision',
        'P': 'Deferred separately selected implementation/test-harness work',
        'Q': 'Source/producer/oracle/provider/deployment/native/device qualification gap',
        'B': 'Await boundary-specific acceptance for affected claims, not whole-group block'},
    'groups': groups, 'evidence_units': evidence, 'document_surfaces': surfaces,
    'source_authority': 'Completed reports and latest B1/C clarifications; no new source-body/spec reading or independent normative match.',
    'whole_group_join': ['CORE-B1-C010', 'CORE-B3-C006'],
    'facet_links': 'relationships.json', 'boundary_gate': 'All nine boundary returns feed later final triage; none imported here.'})

table = ['# Full-family provisional proposal rows', '',
    'FPT IDs are ledger proposals, separate from accepted Core namespaces and original IDs. Facets are independent; see `../../triage-full-family.md`.',
    'Exact path/line/source/range hashes and primary/shared editor ranges are normalized in `map.json`. Full inherited authority is preserved in `inherited-links.json`.', '',
    '| Proposal → original local IDs | Facets / topology | Proposed correction or retained question | Reopen / decision / probe | Inherited association keys |',
    '| --- | --- | --- | --- | --- |']
for group in groups:
    ids = ', '.join(f'[{cid}](../../investigation-{cid.split("-")[1]}.md)' for cid in group['local_ids'])
    cells = [f'{group["proposal_id"]} → {ids}', ','.join(group['facets']) + ' / ' + group['topology'],
             group['proposal_and_rationale'], group['implementation_reopen_trigger_or_needed_probe'],
             ', '.join('`' + x + '`' for x in group['inherited_association_keys']) or '—']
    table.append('| ' + ' | '.join(x.replace('|', '\\|') for x in cells) + ' |')
(OUT / 'rows.md').write_text('\n'.join(table) + '\n')

stats_by_family = {}
for family, selected in family_units.items():
    stats_by_family[family] = {'assigned_units': len(selected),
        'statuses': dict(Counter(x.get('status', x.get('disposition')) for x in selected)),
        'original_family_summary': stats[family]}
save('unit-statistics.json', {'source_baseline': PIN, 'families': stats_by_family,
    'disjoint_assigned_unit_total': len(all_units),
    'meaning': 'Investigator source dispositions only. B4 has 105 full-file reads plus one qualified Unshipped snapshot reconciliation; no independent source acceptance or execution.'})
for path, digest in preserved.items():
    assert hashlib.sha256((RUN / path).read_bytes()).hexdigest() == digest
link_checks = []
for path in ('triage-full-family.md', 'triage/full-family/rows.md', 'triage/full-family/probe-priorities.md'):
    file = RUN / path
    text = file.read_text()
    for target in re.findall(r'\]\(([^)]+)\)', text):
        if '://' not in target:
            resolved = (file.parent / target.split('#')[0]).resolve()
            assert resolved.exists() or resolved == OUT / 'output-identities.json', ('record link', path, target)
            link_checks.append({'path': path, 'target': target})
    for target in re.findall(r'`([^`]+)`', text):
        if target.startswith(('src/', 'docs/', 'scripts/')) and target.endswith(('.md', '.cs', '.json', '.yml')):
            assert target in inventory, ('document surface not in inventory', path, target)
assert len(re.findall(r'^\| FPT-', (OUT / 'rows.md').read_text(), re.M)) == len(groups)
save('verification.json', {'status': 'PASS records-only join and preservation checks', 'source_baseline': PIN,
    'family_local_counts': dict(count_by_family), 'local_ids': len(definitions), 'fpt_groups': len(groups),
    'disjoint_completed_unit_count': len(all_units), 'candidate_context_unit_count': len(evidence),
    'associated_product_paths_hashed': len(surfaces), 'inherited_rows': len(inherited), 'inherited_groups': len(registry['groups']),
    'separate_pgp_labels': len(routing['separately_preserved_pgp_documentation_labels']),
    'boundary_candidates_imported': 0, 'canonical_ids_allocated': 0,
    'new_family_direct_index_joins': index_checks, 'local_markdown_links_checked': len(link_checks),
    'exact_doc_surface_paths_resolve_to_inventory': True,
    'preliminary_bytes_preserved': preserved, 'input_record_hashes': inputs,
    'scope': 'Inventory/source/range ledger equality and fresh raw-byte SHA-256; pinned Git/newline qualification inherited from family identity records. No source-body/spec reread or runtime verification.',
    'limitations': ['No new SDK build/test/formatter/native/device/crypto/probe execution.',
        'No independent content/citation acceptance or hardware qualification.',
        'No central coverage/registry/product/index/ref/commit/push/subagent work.',
        'Boundary follow-ups are dependency context only; final nine-return triage remains future.']})
owned_files = [RUN / 'triage-full-family.md'] + sorted(p for p in OUT.iterdir()
    if p.is_file() and p.name != 'output-identities.json')
save('output-identities.json', {'source_baseline': PIN,
    'scope': 'Owned full-family report and records, excluding this manifest itself; no publication/acceptance seal.',
    'files': {str(p.relative_to(RUN)): hashlib.sha256(p.read_bytes()).hexdigest() for p in owned_files}})
for link in link_checks:
    assert ((RUN / link['path']).parent / link['target'].split('#')[0]).exists()
print(json.dumps({k: v for k, v in json.loads((OUT / 'verification.json').read_text()).items()
    if k not in ('preliminary_bytes_preserved', 'input_record_hashes', 'scope', 'limitations', 'new_family_direct_index_joins')}))
