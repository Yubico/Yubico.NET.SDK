"""Static documentation receipt. No SDK, compiler, native or device execution."""
import difflib
import hashlib
import json
import pathlib
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[9]
PRIMARY = pathlib.Path('/home/rupicola/Code/y/Yubico.Yubikit.NET')
REL = pathlib.Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
OUT = ROOT / REL / 'editing/FIDO-boundaries'
BASE = 'da66f5d01ba594993974ddd26723e8c300c0a454'
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'

def sha(b):
    return hashlib.sha256(b).hexdigest()

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)

def save(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')

assert git('rev-parse', 'HEAD').decode().strip() == BASE
packets = {}
inputs = []
for module in ['Ctap2', 'WebAuthn']:
    d = PRIMARY / REL / 'boundaries' / module
    packets[module] = {
        'files': json.loads((d / 'file-dispositions.json').read_text()),
        'candidates': json.loads((d / 'candidate-index.json').read_text()),
        'definitions': (d / 'candidates.md').read_text(),
    }
    for name in ['file-dispositions.json', 'candidate-index.json', 'candidates.md',
                 'sources.md', 'source-identities.json', 'inherited-dispositions.json',
                 'unit-dispositions.json', 'traces.md']:
        p = d / name
        inputs.append({'path': str(p), 'sha256': sha(p.read_bytes()), 'read_only': True})
for name in ['boundary-Ctap2.md', 'boundary-WebAuthn.md', 'triage-full-family.md',
             'triage/editing-release.md', 'boundaries/Ctap2/followup-B4-B6.md',
             'boundaries/Ctap2/followup-B4-B6.json', 'boundaries/Ctap2/followup-WebAuthn.md',
             'boundaries/Ctap2/followup-WebAuthn.json', 'boundaries/Ctap2/boundary-completion.md',
             'boundaries/WebAuthn/ctap2-handoff.md', 'boundaries/WebAuthn/test-oracles.md']:
    p = PRIMARY / REL / name
    inputs.append({'path': str(p), 'sha256': sha(p.read_bytes()), 'read_only': True})
save('input-identities.json', inputs)

files = {}
for module, packet in packets.items():
    for row in packet['files']:
        p = row['path']
        f = files.setdefault(p, {'path': p, 'packets': [], 'local_ids': set(), 'basis': []})
        f['packets'].append(module)
        f['local_ids'].update(row.get('candidate_ids', []))
        f['basis'].append({'packet': module, 'observation': row['observation'],
                           'original_disposition': row['candidate_file_disposition'],
                           'source_sha256': row['source_sha256']})
    for candidate in packet['candidates']:
        for p in candidate['candidate_associated_unchanged_paths']:
            if p in files:
                files[p]['local_ids'].add(candidate['id'])

# These reopening associations are explicit in the candidate prose, although the original
# file ledger leaves candidate_ids empty for the module instructions.
files['src/WebAuthn/CLAUDE.md']['local_ids'].update(
    ['CORE-WA-X001', 'CORE-WA-X002', 'CORE-WA-X003', 'CORE-WA-X004'])
changed = git('diff', '--name-only', BASE, '--', 'src/Ctap2', 'src/WebAuthn').decode().splitlines()
assert changed and all(p in files and pathlib.Path(p).suffix in ['.cs', '.md'] for p in changed)

def erase_comments(text):
    # Keep string/char literals before matching comments, including verbatim strings.
    pattern = r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*[\s\S]*?\*/'
    return re.sub(pattern, lambda m: '' if m[0].startswith(('//', '/*')) else m[0], text)

def xml_errors(text):
    errors = []
    lines = text.splitlines()
    i = 0
    while i < len(lines):
        if lines[i].lstrip().startswith('///'):
            start = i + 1
            body = []
            while i < len(lines) and lines[i].lstrip().startswith('///'):
                body.append(lines[i].lstrip()[3:])
                i += 1
            try:
                ET.fromstring('<root>' + '\n'.join(body) + '</root>')
            except ET.ParseError as ex:
                errors.append({'line': start, 'body': '\n'.join(body), 'error': str(ex)})
        else:
            i += 1
    return errors

checks = []
for p, f in sorted(files.items()):
    eligible = p.startswith(('src/Ctap2/', 'src/WebAuthn/')) and pathlib.Path(p).suffix in ['.cs', '.md']
    f['eligible'] = eligible
    f['local_ids'] = sorted(f['local_ids'])
    f['decision'] = 'edit' if p in changed else 'retain' if eligible else 'hold / refer'
    f['reason'] = ('Apply only the localized documentation facets in candidate-decisions.json; retain other accurate contracts.'
                   if p in changed else 'Retain the packet-qualified current contract and its limitations; no source-supported correction selected for this path.'
                   if eligible else 'Outside assigned .cs/.md module scope; refer to the owning Core/SHARED/applet or snapshot owner.')
    b = git('show', f'{BASE}:{p}')
    a = (ROOT / p).read_bytes()
    pin = git('show', f'{PIN}:{p}')
    f.update(before_sha256=sha(b), after_sha256=sha(a), pinned_product_sha256=sha(pin),
             base_equals_product_pin=b == pin, bytes_unchanged=b == a)
    if p not in changed:
        assert b.replace(b'\r\n', b'\n') == a.replace(b'\r\n', b'\n'), p
        f['checkout_eol_only_difference'] = b != a
        f['unchanged_worktree_before_sha256'] = sha(a)
    if p in changed:
        before, after = b.decode('utf-8-sig'), a.decode('utf-8-sig')
        f['changed_ranges'] = [{'before': [i+1, j], 'after': [k+1, l], 'operation': op}
            for op, i, j, k, l in difflib.SequenceMatcher(None, before.splitlines(), after.splitlines()).get_opcodes()
            if op != 'equal']
        if p.endswith('.cs'):
            # Compare the complete comment-erased code text, ignoring blank comment lines only.
            strip = lambda s: [v.rstrip() for v in erase_comments(s).splitlines() if v.strip()]
            assert strip(before) == strip(after), p
            old_xml, new_xml = xml_errors(before), xml_errors(after)
            assert all(x['body'] in [v['body'] for v in old_xml] for x in new_xml), (p, new_xml)
            checks.append({'path': p, 'comment_erased_code_equal': True,
                           'non_comment_line_sha256': sha('\n'.join(strip(after)).encode()),
                           'xml_new_errors': 0, 'preexisting_xml_errors': new_xml})
save('path-decisions.json', list(files.values()))

doc_actions = {
 'CORE-CTAP-X001': 'Edit README/CLAUDE membership and process scope; retain one-session and no-failover contracts.',
 'CORE-CTAP-X002': 'Edit session XML and guides with fully awaited serialized workflow-before-disposal and borrowed prompt/dependency lifetimes.',
 'CORE-CTAP-X003': 'Retain accurate CTAP preset, ClientPin and PIN guide units/profile gaps; clarify Core-reader adapter limits in WebAuthn README.',
 'CORE-CTAP-X004': 'Retain returned-copy/StoredCredentialInfo limitations; clarify actual backend HID/APDU response views and unreachable APDU-error storage.',
 'CORE-CTAP-X005': 'Retain scoped V1/V2/helper cleanup bounds; hold exceptional crypto repair and provider-erasure claims.',
 'CORE-CTAP-X006': 'Retain required strict PIN profile and disclosed missing peer labels; no catalog/provider validation substitution.',
 'CORE-CTAP-X007': 'Retain experimental status, draft-10 wire mismatch/-65700 erratum and separate draft-09 mathematics authority.',
 'CORE-CTAP-X008': 'Retain accessor algorithm-disagreement disclosure; hold propagation/dispatch repair.',
 'CORE-CTAP-X009': 'Retain raw-message P-256/SHA-256 and limited signature DER claims; no Core key-parser guarantee.',
 'CORE-CTAP-X010': 'Retain exact make key6/get key8 and optional direct-blob support limitations; no escape-hatch implementation.',
 'CORE-CTAP-X011': 'Retain structural/envelope/count limitations; no framing-to-schema conformance inference.',
 'CORE-CTAP-X012': 'Retain signed-range, cap and reconstruction qualifications; no atomic blob workflow claim.',
 'CORE-CTAP-X013': 'Retain separate plaintext-probe/compression/request cleanup scopes; hold cleanup repair.',
 'CORE-CTAP-X014': 'Retain salt builder and PRF intermediate/result ownership qualifications.',
 'CORE-CTAP-X015': 'Retain encrypted-wrapper acceptance/token/cleanup limitations; no persistent-token proof.',
 'CORE-CTAP-X016': 'Retain builder copies versus cleared transmission buffer and raw borrowed-input contracts.',
 'CORE-CTAP-X017': 'Edit guide sentinel policy, initialization/override/configuration and SCP provenance; retain accurate factory gate XML.',
 'CORE-CTAP-X018': 'Edit caller lifetime/cancellation recovery bounds and baseline synchronous Windows limit; retain framing/status and raced-success distinctions.',
 'CORE-CTAP-X019': 'Retain independent received-report ownership; clarify backend accumulated response is a different buffer.',
 'CORE-CTAP-X020': 'Qualify module and backend failure-cleanup claims with conditional throwing-logger path; no reproduced leak claimed.',
 'CORE-CTAP-X021': 'Correct selector target-continuity comment; menu reuses transport, command-line flow recomputes it. Retain reset implementation.',
 'CORE-CTAP-X022': 'Retain assertion-qualified CTAP tests/native/physical limits; clarify associated WebAuthn request-context and route/provenance comments.',
 'CORE-CTAP-X023': 'Retain CTAP endian versus WebAuthn default-value distinction; no Guid/provider probe.',
 'CORE-WA-X001': 'Edit README and module instructions with overlapping-known-member/process bound.',
 'CORE-WA-X002': 'Edit client XML/README/module instructions with serialized fully awaited whole-ceremony lifetime and borrowed connection ownership.',
 'CORE-WA-X003': 'Retain exact prompt transfer/context units; qualify README ConsoleCredentialReader adapter and application PIN-profile responsibility.',
 'CORE-WA-X004': 'Edit request-only fake-connection test summary and guide notification/prompt lifetime/outcome boundaries.',
 'CORE-WA-X005': 'Retain cooperative timer/raced-success/error distinctions; no universal reuse or native deadline claim.',
 'CORE-WA-X006': 'Retain borrowed attestation, authData/PRF/blob separate storage and lack of client-result erasure.',
 'CORE-WA-X007': 'Retain corrected edition attribution and safe original-singleton advice; WA-IMP-010 recovery gap remains.',
 'CORE-WA-X008': 'Retain actual User passthrough and application RP handoff. Hold native-output actor/serializer policy under CTAP-DEC-002; no stripping.',
 'CORE-WA-X009': 'Retain backend cache/factory firmware/transport provenance; no reset/cache invalidation or new gate.',
 'CORE-WA-X010': 'Retain independent profiles, key/algorithm roles, expired drafts, borrowed inputs and experimental restrictions.',
 'CORE-WA-X011': 'Retain all named applet conformance/extension limitations; Core work does not close them.',
 'CORE-WA-X012': 'Edit fake route description and re-mint test summary to actual assertions; retain 5.8 provenance only as unmeasured historical rationale.',
}
decisions = []
for module, packet in packets.items():
    for c in packet['candidates']:
        ident = c['id']
        number = ident.rsplit('X', 1)[1]
        match = re.search(r'^## X' + number + r'\b.*?(?=^## |\Z)', packet['definitions'], re.M | re.S)
        assert match, ident
        decisions.append({'local_id': ident, 'accepted_id': None,
            'documentation': doc_actions[ident],
            'path_decisions': [p for p, f in files.items() if ident in f['local_ids']],
            'implementation': 'HOLD: no executable change; retain exact original mechanism/aliases in the definition below.',
            'policy': 'HOLD: unresolved choices remain with maintainer/coordinator; documentation is not policy adoption.',
            'unrun_evidence': 'HOLD: no SDK/compiler/crypto/native/device verification; stronger claims await the original named evidence.',
            'definition_read_only_source': str(PRIMARY / REL / 'boundaries' / module / 'candidates.md'),
            'original_facets_and_reopening': match[0]})
assert len(decisions) == 35 and set(doc_actions) == {x['local_id'] for x in decisions}
save('candidate-decisions.json', decisions)

# Capture exact complete fenced bodies in every modified Markdown file, including unchanged context.
# No example body is edited in this batch; classification is explicit rather than compilation credit.
full_methods = {('src/Ctap2/CLAUDE.md', 10), ('src/Ctap2/CLAUDE.md', 11),
                ('src/Ctap2/CLAUDE.md', 12), ('src/Ctap2/tests/CLAUDE.md', 1),
                ('src/Ctap2/tests/CLAUDE.md', 5)}
full_types = {('src/Ctap2/README.md', 8), ('src/WebAuthn/README.md', 3), ('src/WebAuthn/README.md', 5)}
examples = []
for p in changed:
    if not p.endswith('.md'):
        continue
    b = git('show', f'{BASE}:{p}').decode()
    a = (ROOT / p).read_text()
    fence = re.compile(r'^```([^\n]*)\n(.*?)^```\s*$', re.M | re.S)
    old, new = list(fence.finditer(b)), list(fence.finditer(a))
    assert len(old) == len(new) and a.count('```') % 2 == 0
    for n, (before, after) in enumerate(zip(old, new), 1):
        assert before[0] == after[0], (p, n)
        lang = after[1]
        classification = ('directory diagram' if not lang else 'unexecuted shell command' if lang == 'bash'
                          else 'complete test method in named test-project/import context' if (p, n) in full_methods
                          else 'complete helper type; requires stated imports and application callback/context' if (p, n) in full_types
                          else 'historical before / current after API-shape fragments; not a resource-scoped consumer program'
                          if (p, n) == ('src/WebAuthn/README.md', 9)
                          else 'integration-test scaffold with omitted test body; not a complete executable example'
                          if (p, n) == ('src/WebAuthn/CLAUDE.md', 7)
                          else 'interface contract excerpt; not a consumer implementation'
                          if (p, n) == ('src/WebAuthn/CLAUDE.md', 5)
                          else 'schematic feature declarations, not a pasteable class'
                          if (p, n) == ('src/Ctap2/CLAUDE.md', 14)
                          else 'composable consumer demo with await-using resource scope, not RP verification'
                          if (p, n) in [('src/Ctap2/README.md', 2), ('src/WebAuthn/README.md', 2)]
                          else 'fragment / alternative: depends on surrounding supplied variables, imports and outer resource scopes')
        examples.append({'path': p, 'fence': n, 'language': lang, 'classification': classification,
                         'before_line': b[:before.start()].count('\n') + 1,
                         'after_line': a[:after.start()].count('\n') + 1,
                         'body': after[2], 'body_sha256': sha(after[2].encode()),
                         'body_unchanged': True, 'compiled': False, 'executed': False})
save('examples.json', examples)

links = []
link_re = re.compile(r'(?<!!)\[[^\]]+\]\(([^)]+)\)')
for p in changed:
    if not p.endswith('.md'):
        continue
    old = set(link_re.findall(git('show', f'{BASE}:{p}').decode()))
    for link in sorted(set(link_re.findall((ROOT / p).read_text()))):
        if re.match(r'[a-z]+:', link) or link.startswith('#'):
            continue
        target = link.split('#')[0]
        exists = (ROOT / p).parent.joinpath(target).exists()
        assert exists or link in old, (p, link)
        links.append({'path': p, 'link': link, 'target_exists': exists,
                      'new': link not in old, 'anchor_resolution': 'not inferred from target existence'})
save('link-check.json', links)
(OUT / 'product.patch').write_bytes(git('diff', '--binary', BASE, '--', 'src/Ctap2', 'src/WebAuthn'))
save('verification.json', {'base': BASE, 'product_source': PIN, 'changed_product_paths': changed,
    'candidate_groups': len(decisions), 'path_population': len(files),
    'eligible_path_population': sum(f['eligible'] for f in files.values()),
    'changed_file_checks': checks, 'example_bodies_unchanged': len(examples),
    'new_xml_errors': 0, 'new_missing_link_targets': 0,
    'scope': 'Static source/text preservation, XML fragment syntax, fence bodies and file-target checks only.',
    'not_run': ['build', 'test', 'formatter', 'compiler', 'crypto experiment', 'native', 'device'],
    'independent_review': 'pending', 'publication': 'pending coordinator; no index/ref/commit/push changes'})
print(json.dumps({'changed_paths': len(changed), 'path_decisions': len(files),
                  'candidate_decisions': len(decisions), 'exact_example_bodies': len(examples),
                  'static_checks': 'passed'}, indent=2))
