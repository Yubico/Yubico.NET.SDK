"""Bounded external-editor receipt; reads source and writes only this owned record directory."""
import collections
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path('/home/rupicola/Code/y/yubikit-core-docs-edit-external')
PRIMARY = Path('/home/rupicola/Code/y/Yubico.Yubikit.NET')
RUN = Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
OUT = ROOT / RUN / 'editing/external'
BASE = 'da66f5d01ba594993974ddd26723e8c300c0a454'
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
os.environ['GIT_OPTIONAL_LOCKS'] = '0'

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)

def sha(data):
    return hashlib.sha256(data).hexdigest()

def optional_pin_blob(path):
    result = subprocess.run(['git', 'rev-parse', '--verify', PIN + ':' + path],
                            cwd=ROOT, capture_output=True, text=True)
    return result.stdout.strip() if result.returncode == 0 else None

def dump(name, data):
    (OUT / name).write_text(json.dumps(data, indent=2, ensure_ascii=False) + '\n')

def text(data):
    return data.decode('utf-8-sig').replace('\r\n', '\n')

shared = json.loads((ROOT / RUN / 'inventory/shared-sections.json').read_text())
mapping = json.loads((PRIMARY / RUN / 'triage/full-family/map.json').read_text())
modules = ['Core', 'Management', 'Ctap2', 'WebAuthn', 'SecurityDomain', 'Oath',
           'YubiOtp', 'YubiHsm', 'OpenPgp', 'Piv']

def external(path):
    return not (any(path.startswith('src/' + m + '/') for m in modules)
                or path in ['README.md', 'CLAUDE.md']
                or (path.startswith('docs/') and ('/' not in path[5:]
                    or path.startswith(('docs/usage/', 'docs/architecture/')))))

def historical(path):
    return (path.startswith(('docs/research/', 'docs/plans/'))
            or path in ['2026-09-21-yubikit-async-boundaries-ISA.md',
                        'docs/migration/v1-to-v2-gaps.md',
                        'docs/migration/v1-to-v2-changelog.md'])

external_paths = sorted({u['path'] for u in shared if external(u['path'])})
syntax_paths = [p for p in external_paths if Path(p).suffix in ['.cs', '.md']
                and not p.startswith('scripts/')]
allowlist = [p for p in syntax_paths if not historical(p)]
changed = git('diff', '--name-only', BASE).decode().splitlines()
product_changed = [p for p in changed if not p.startswith(str(RUN) + '/')]
assert set(product_changed) <= set(allowlist), set(product_changed) - set(allowlist)
assert git('rev-parse', 'HEAD').decode().strip() == BASE

reasons = {
    'TOOLCHAIN.md': 'Correct recursive path selection, same-kind translation limits, aggregate no-tests gate, full-argv logging, AOT compile/link scope and benchmark evidence boundaries. No runner or native result is claimed.',
    'docs/migration/v1-to-v2.md': 'Replace repository/extension-only implications with actual Find/monitor and public direct/owned factory roles. Add synchronous direct HID, firmware provenance, whole-workflow/input lifetime, exact SCP containment and plain-reselect limits. Replace absent credential loaders/providers with typed composable methods and full cleanup. Explain current public crypto factories and shared Base32 without rewriting historical gap/changelog evidence.',
    'src/Cli.Commands/src/Infrastructure/YkCommandBase.cs': 'AppletTransports orders device preference; only the explicit requested transport is forwarded. Correct XML without changing selection or options.',
    'src/Cli.Shared/src/Device/DeviceSelector.cs': 'Retain reference-keyed enrichment, cancellation and exception logging mechanisms. Clarify first-transport device preference, not later session transport pinning. No global redaction assurance is introduced.',
    'src/Cli.Shared/src/Device/FormFactorFormatter.cs': 'Exact enum matching can display Unknown for retained product flag bits. Document limitation without changing display strings or normalization policy.',
    'src/Cli.Commands/src/Otp/OtpCommands.cs': 'Correct ModHex 32-case alphabet and locale qualification in XML only. Runtime Description text still makes the broader any-host claim and remains deferred. Unified CLI translation is already functional, unlike standalone OtpTool.',
    'src/Cli.Shared/src/Output/SecureCredential.cs': 'Describe UTF-8 byte limit, interactive surrogate validation without normalization, raw redirected bytes, discarded CR and synchronous/noncancellable input. Keep clear-and-return ownership scope distinct from applet profile and runtime input interruption.',
    'src/Cli.Shared/src/Output/PinPrompt.cs': 'Document 128-byte prompted limit, raw-input versus interactive errors and caller profile responsibilities; retain string/argv visibility and fixed-time confirmation rationale.',
    'src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Output/SecureCredentialTests.cs': 'Scope exact-view assertion to length/backing size and declined vector to null. Separate tracking-pool clearing witness. No assertion/name/guard changes; no native/global erasure claim.',
    'src/Tests.Shared/CLAUDE.md': 'Explain actual lazy run-local fixture list, live reads before serial filtering, empty-result binding skips and unknown-serial exit exception. Remove invented async initializer body and unsupported fixed performance numbers; identify abbreviated structural sketches as sketches.',
    'src/Tests.Shared/README.md': 'Correct pre-I/O hard-fail promise, unknown-serial authorization and empty-result behavior. Repair recorder example with actual API and enclosing connection scope; distinguish synthetic recorder assertions from interoperability. Qualify null topology and no universal one-scan split guarantee.',
    'src/Tests.Shared/IYubiKeyFilter.cs': 'Replace absent YubiKeyTheory/YubiKeyTestDevice example with WithYubiKey, SkippableTheory and actual Management helper in complete test types. Serial-range filtering narrows authorized dedicated keys only.',
    'src/Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs': 'Correct InitializeDevicesAsync XML to actual read-before-filter order, empty-list/skipped binding and separate AllowList constructor exit. Exact form-factor comparison is retained. The private logger is separate from global SDK factory selection.',
    'src/Tests.Shared/YubiKeyTestState.cs': 'Correct placeholder re-enumeration claim to first-match lazy binding. Explain cached FIPS mask/any-bit tests without implying operating mode or repaired decoder. Wide-capability carrier policy remains held.',
    'src/Tests.Shared/Infrastructure/TestCategories.cs': 'Remove generic factory-reset/reinsertion equivalence. State application/firmware recovery conditions without changing trait values or reset behavior.',
    'src/Tests.Shared/Infrastructure/OperatorCue.cs': 'State Console/echo before best-effort speech, propagating sink exceptions and at-most-15-second child wait without termination/completion proof. No delivery or native-drain guarantee.',
    'src/Tests.Shared/RecordingSmartCardConnection.cs': 'Document copied commands, aliased queued responses and counter-only disposal. Clearing final owned fixture arrays does not erase unrelated TLV search intermediates or add protocol/native emulation.',
    'src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/AsyncSurfaceConventionTests.cs': 'Describe finite reflection grammar and explicit synchronous exceptions, not executed or responsive operations.',
    'src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/FactoryShapeTests.cs': 'Correct current application-owned RP validation names and one-shot test scope. Preserve separate client/session options rationale and all reflection assertions.',
    'src/Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs': 'RETAIN: current XML already states first-match binding, malformed firmware ignored, full form-factor comparison and FIPS mapping limits. Do not repair filters/decoder in documentation work; attribute sample names do not authorize production hardware.',
    'src/Cli.Commands/src/HsmAuth/HsmAuthCommands.cs': 'RETAIN: actual try-scope comments concern management-key clearing when password resolution aborts and match those scopes. There is no blanket provider/SCP-copy-erasure claim to correct. Source-supported pre-try allocation/other-owner gaps are not a repaired all-path cleanup contract.',
    'src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/AuditV2/CliStaticPasswordEncodingTests.cs': 'RETAIN: the unified CLI encoding regression is distinct from standalone OtpTool; keep executable assertions and existing local observer scope. No keyboard-output/locale/device qualification inferred.',
    'toolchain.cs': 'RETAIN: entry-point comments do not promise argv redaction. The actual RunLog.Start call forwards complete args at line 85; TOOLCHAIN now discloses capture. Changing logger plumbing, arguments, executable help or behavior is not authorized.',
}

associations = collections.defaultdict(list)
candidate_rows = []
for group in mapping['groups']:
    for local in group['local_records']:
        anchors = [dict(unit_id=ref['unit_id'], relation=ref['relation'],
                        **mapping['evidence_units'][ref['unit_id']])
                   for ref in local['evidence_unit_refs']]
        paths = set(local['documentation_surface_refs']) | {a['path'] for a in anchors}
        hits = sorted(paths & set(external_paths))
        if not hits:
            continue
        row = dict(proposal_id=group['proposal_id'], local_id=local['local_id'],
                   facets=group['facets'], original_definition=local['original_definition'],
                   proposal=group['proposal_and_rationale'],
                   reopening=group['implementation_reopen_trigger_or_needed_probe'],
                   qualified_source_register=local['qualified_source_register'],
                   latest_authority_addenda=local['latest_authority_addenda'],
                   inherited_association_keys=group['inherited_association_keys'],
                   exact_source_anchors=anchors, external_paths=hits,
                   documentation='Source-supported local edit/retain/hold only; independent review pending.',
                   remaining_facets='HOLD: no implementation/policy adoption, experiment, producer, platform or physical qualification.')
        candidate_rows.append(row)
        for path in hits:
            associations[path].append({'local_id': local['local_id'], 'proposal_id': group['proposal_id']})

supplemental = []
for local_id, path, reason in [
    ('CORE-A-C009', 'src/Tests.Shared/README.md', 'Adjacent multi-key guidance needs the current null-resolver versus operating-system API distinction.'),
    ('CORE-A-C010', 'src/Tests.Shared/README.md', 'Adjacent multi-key guidance must not imply one-scan convergence after a prior merge.'),
    ('CORE-A-C011', 'src/Tests.Shared/README.md', 'Keep complementary partial grouping limits next to multi-key test guidance.'),
    ('CORE-C-C017', 'docs/migration/v1-to-v2.md', 'Correct current guidance here while leaving the dated gap report unchanged: shared Base32, span suspension boundary and shared array-bearing struct ownership rationale.')]:
    group = next(g for g in mapping['groups'] if local_id in g['local_ids'])
    local = next(l for l in group['local_records'] if l['local_id'] == local_id)
    supplemental.append(dict(local_id=local_id, proposal_id=group['proposal_id'], path=path,
        reason=reason, authority='Editor-explicit adjacent dependency, not an original-map path or new finding.',
        exact_source_anchors=[dict(unit_id=ref['unit_id'], **mapping['evidence_units'][ref['unit_id']])
            for ref in local['evidence_unit_refs']]))
    associations[path].append({'local_id': local_id, 'proposal_id': group['proposal_id'], 'relation': 'editor-explicit adjacent dependency'})

boundary_rows = []
for module in modules[1:]:
    directory = PRIMARY / RUN / 'boundaries' / module
    index = directory / 'candidate-index.json'
    if not index.exists():
        index = directory / 'candidates.json'
    if not index.exists():
        continue
    for row in json.loads(index.read_text()):
        hits = [p for p in external_paths if p in json.dumps(row)]
        if hits:
            boundary_rows.append(dict(boundary=module, local_id=row['id'],
                definition_path=str(index.relative_to(PRIMARY)), original_index_record=row,
                paths=hits, acceptance='Source referral only; no blanket boundary/review acceptance.'))
            for path in hits:
                associations[path].append({'local_id': row['id'], 'boundary': module})

files = []
for path in external_paths:
    before = git('show', BASE + ':' + path)
    current = (ROOT / path).read_bytes()
    if path in product_changed:
        decision = 'EDIT'
    elif historical(path):
        decision = 'HOLD-HISTORICAL'
    elif path not in allowlist:
        decision = 'HOLD-READONLY-FORMAT-OR-SCRIPT'
    else:
        decision = 'RETAIN'
    reason = reasons.get(path)
    if reason is None:
        if historical(path):
            reason = 'Preserve dated experimental/planning/audit/migration capture bytes. Apply current guidance only in active owned surfaces; this record is not current execution or source authority.'
        elif path not in allowlist:
            reason = 'Configuration, script, YAML/JSON/project or other excluded format is read-only. Source-supported comment needs are explicit followups, not authorized changes.'
        else:
            reason = 'No candidate-associated correction selected for this path. Retain bytes without claiming a fresh whole-file clean audit or execution.'
    files.append(dict(path=path, decision=decision, reason=reason,
        candidates=associations[path], baseline_commit=BASE, source_pin=PIN,
        base_git_blob=git('rev-parse', BASE + ':' + path).decode().strip(),
        source_pin_git_blob=optional_pin_blob(path),
        base_git_sha256=sha(before), current_raw_sha256=sha(current),
        base_final_newline=before.endswith(b'\n'), current_final_newline=current.endswith(b'\n'),
        base_utf8_bom=before.startswith(b'\xef\xbb\xbf'), current_utf8_bom=current.startswith(b'\xef\xbb\xbf'),
        base_lf_sha256=sha(text(before).encode()), current_lf_sha256=sha(text(current).encode()),
        base_current_lf_equal=text(before) == text(current),
        shared_source_anchors=[u for u in shared if u['path'] == path]))

def examples(path, content):
    lines = content.splitlines()
    if path.endswith('.md'):
        active = None
        for i, line in enumerate(lines, 1):
            match = re.match(r'^\s*(`{3,}|~{3,})(.*)$', line)
            if not match:
                continue
            if active is None:
                active = (i, match[1], match[2].strip())
            elif match[1][0] == active[1][0] and len(match[1]) >= len(active[1]):
                yield active[0], i, active[2], '\n'.join(lines[active[0]:i-1])
                active = None
        assert active is None, ('unclosed fence', path, active)
    else:
        for match in re.finditer(r'<code(?:\s[^>]*)?>(.*?)</code>', content, re.S):
            body = re.sub(r'^\s*/// ?', '', match[1], flags=re.M)
            yield content.count('\n', 0, match.start()) + 1, content.count('\n', 0, match.end()) + 1, 'csharp/xml-code', body

example_rows = []
for f in files:
    path = f['path']
    if path not in syntax_paths:
        continue
    for version, data in [('base', git('show', BASE + ':' + path)), ('current', (ROOT / path).read_bytes())]:
        for first, last, language, body in examples(path, text(data)):
            if historical(path):
                classification = 'historical/proposal/experimental record; not current runnable guidance'
            elif path.endswith('CLAUDE.md') and ('/*' in body or '// ...' in body or 'class FakeAllowListProvider' in body):
                classification = 'abbreviated implementation sketch; read actual source; not executable replacement'
            elif re.search(r'using Yubico\.YubiKey[.;]', body) or '// alpha.2' in body:
                classification = 'historical v1 or alpha before/after migration material'
            elif language in ['bash', 'sh', 'shell', 'powershell']:
                classification = 'command fragment; documented prerequisites apply; not executed'
            elif language in ['json', 'text', '', 'yaml', 'yml']:
                classification = 'configuration/output/structural illustration; not an executed example'
            elif 'static async Task WithRawScpAsync' in body or 'static async Task ReadCodesAsync' in body:
                classification = 'complete composable method with typed borrowed/device or transferred-secret prerequisites and enclosing resource scopes'
            elif 'public sealed class MetadataTests' in body:
                classification = 'complete filter and test type example in the real integration-test project context; helper owns awaited session/connection scopes'
            else:
                classification = 'composable code fragment or method/type example; enclosing document supplies imports, caller inputs and project context'
            example_rows.append(dict(path=path, version=version, first_line=first, last_line=last,
                language=language, body_sha256=sha(body.encode()), body=body,
                classification=classification, compiler_status='NOT-RUN; no new compilation or execution credit',
                resources='Changed examples use actual APIs and explicit session/connection or secret-owner/finally scopes. Other retained bodies retain documented prerequisites; no all-platform/native/physical acceptance.'))

checks = []
for path in product_changed:
    before = text(git('show', BASE + ':' + path))
    after = text((ROOT / path).read_bytes())
    if path.endswith('.cs'):
        def noncomments(content):
            result = []
            for line in content.splitlines():
                if not line.strip() or line.lstrip().startswith('//'):
                    continue
                # The only edited inline comments are on existing return-[] statements.
                if re.match(r'^\s*return \[\];\s*//', line):
                    line = line.split('//', 1)[0].rstrip()
                result.append(line)
            return result
        assert noncomments(before) == noncomments(after), ('noncomment change', path)
        xml_count = 0
        for block in re.findall(r'(?:^[ \t]*///[^\n]*\n?)+', after, flags=re.M):
            xml = re.sub(r'^[ \t]*/// ?', '', block, flags=re.M)
            ET.fromstring('<root>' + xml + '</root>')
            xml_count += 1
        checks.append(dict(path=path, noncomment_lines='equal (full lines and unchanged inline code)',
                           xml_blocks_well_formed=xml_count, cref_resolution='NOT-CHECKED'))
    else:
        list(examples(path, after))
        checks.append(dict(path=path, markdown_fences='balanced'))

patch = git('diff', '--binary', '--src-prefix=a/', '--dst-prefix=b/', BASE, '--', *product_changed)
(OUT / 'source.patch').write_bytes(patch)
diff_check = subprocess.run(['git', 'diff', '--check'], cwd=ROOT, capture_output=True, text=True)
assert diff_check.returncode == 0, diff_check.stdout + diff_check.stderr
inputs = [PRIMARY / RUN / f'investigation-{family}.md' for family in ['A','B1','B2','B3','B4','B5','B6','C']]
inputs += sorted((PRIMARY / RUN).glob('boundary-*.md'))
inputs += [PRIMARY / RUN / 'triage/full-family/map.json', PRIMARY / RUN / 'triage-full-family.md',
           ROOT / RUN / 'inventory/shared-sections.json', ROOT / RUN / 'triage/editing-release.md']
for worktree, relative in [
    ('discovery', 'edit-A.md'), ('crypto', 'editing/C/shared-followups.md'),
    ('protocols', 'editing/protocols/outside-owner-handoffs.json'),
    ('transports', 'editing/transports/followups.json'), ('transports', 'edit-transports.md'),
    ('fido-boundaries', 'editing/FIDO-boundaries/citations-and-handoff.md'),
    ('fido-boundaries', 'editing/FIDO-boundaries/candidate-decisions.json')]:
    inputs.append(ROOT.parent / ('yubikit-core-docs-edit-' + worktree) / RUN / relative)
dump('input-identities.json', [dict(path=str(p), bytes=p.stat().st_size, sha256=sha(p.read_bytes()),
    use='Read-only completed source/referral input, not independent review acceptance.') for p in inputs])
dump('allowlist.json', dict(base=BASE, source_pin=PIN,
    derivation='Exact shared-sections paths minus ten modules and existing guide owner. Only active .cs/.md; scripts/configuration/historical records read-only.',
    external_ledger_paths=external_paths, syntax_candidate_paths=syntax_paths,
    active_product_allowlist=allowlist, verification_benchmark_paths_in_shared_ledger=[],
    owned_records=[str(RUN / 'edit-external.md'), str(RUN / 'editing/external') + '/**']))
dump('path-decisions.json', files)
dump('candidate-decisions.json', candidate_rows)
dump('boundary-referrals.json', boundary_rows)
dump('supplemental-dependencies.json', supplemental)
followups = []
for row in candidate_rows:
    for path in row['external_paths']:
        if path not in allowlist:
            followups.append(dict(path=path, local_id=row['local_id'], proposal_id=row['proposal_id'],
                disposition='HOLD; excluded script/configuration or historical record remains byte-identical',
                source_supported_need=row['proposal'], reopening=row['reopening'],
                exact_source_anchors=row['exact_source_anchors']))
for group in mapping['groups']:
    for local in group['local_records']:
        for path in local['documentation_surface_refs']:
            if external(path) and path not in external_paths:
                followups.append(dict(path=path, local_id=local['local_id'], proposal_id=group['proposal_id'],
                    disposition='HOLD-OUTSIDE-LEDGER; not authorized by shared-sections.json',
                    source_supported_need=group['proposal_and_rationale'],
                    reopening=group['implementation_reopen_trigger_or_needed_probe'],
                    source_identity=mapping['document_surfaces'].get(path),
                    definition=local['original_definition']))
dump('outside-owner-followups.json', followups)
dump('example-bodies.json', example_rows)
dump('static-preservation.json', dict(base=BASE, head=git('rev-parse','HEAD').decode().strip(),
    source_pin=PIN, edited_product_files=len(product_changed), changed_paths=product_changed,
    dispositions=dict(collections.Counter(f['decision'] for f in files)),
    family_local_candidates=len(candidate_rows), boundary_referrals=len(boundary_rows),
    example_bodies_base_and_current=len(example_rows), checks=checks, git_diff_check='PASS',
    source_patch_sha256=sha(patch), index_refs_commits='No mutation requested or performed',
    limits='Text/XML/fence and raw/LF/hash preservation only; not a Roslyn token/API/cref check, build, formatter, test, example compilation, native/provider/device/crypto run or independent content/citation acceptance.'))
print(json.dumps(dict(edited=len(product_changed), dispositions=collections.Counter(f['decision'] for f in files),
    candidates=len(candidate_rows), examples=len(example_rows), allowlist=len(allowlist)), indent=2))
