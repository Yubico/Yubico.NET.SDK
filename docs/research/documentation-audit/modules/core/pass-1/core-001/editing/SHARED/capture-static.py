"""Bounded SHARED return capture. Static Markdown/hash/diff checks only; no SDK invocation."""
import collections
import hashlib
import json
import pathlib
import re
import subprocess
import urllib.parse

ROOT = pathlib.Path(__file__).resolve().parents[9]
RUN = ROOT / 'docs/research/documentation-audit/modules/core/pass-1/core-001'
OUT = RUN / 'editing/SHARED'
BASE = 'da66f5d01ba594993974ddd26723e8c300c0a454'
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)

def sha(b):
    return hashlib.sha256(b).hexdigest()

def put(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')

shared = json.loads((RUN / 'inventory/shared-sections.json').read_text())
mapping = json.loads((RUN / 'triage/full-family/map.json').read_text())

def permitted(p):
    bits = pathlib.PurePosixPath(p).parts
    return p.endswith('.md') and (p.startswith('src/Core/') or (
        p in {u['path'] for u in shared} and (
            p in ('README.md', 'CLAUDE.md') or
            (len(bits) == 2 and bits[0] == 'docs') or
            (len(bits) == 3 and bits[:2] in [('docs', 'usage'), ('docs', 'architecture')]))))

allowed = sorted({u['path'] for u in shared if permitted(u['path'])} |
                 {str(p.relative_to(ROOT)) for p in (ROOT / 'src/Core').rglob('*.md')})
changed = git('diff', '--name-only', 'HEAD').decode().splitlines()
product = [p for p in changed if permitted(p)]
assert set(changed) == set(product), 'Tracked change outside product allowlist'
assert git('rev-parse', 'HEAD').decode().strip() == BASE
assert not git('diff', '--cached', '--name-only').strip(), 'Index changed'

associations = collections.defaultdict(list)
for group in mapping['groups']:
    for local in group['local_records']:
        for path in local['documentation_surface_refs']:
            associations[path].append({'local_id': local['local_id'], 'proposal_id': group['proposal_id'],
                                       'facets': group['facets']})

reasons = {
    'src/Core/PACKAGE_README.md': 'Retain accurate short Find/watch ownership and live Management pointer.',
    'src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/README.md':
        'Retain existing source-site/link-registry/sentinel and native-execution limitations.',
    'docs/TESTING_PLATFORM_FINDINGS.md': 'Retain runner direction; Windows access candidate is dependency context only.',
}
files = []
examples = []
new_broken = []
existing_broken = []
unclosed = []

def links(text, path):
    result = []
    in_fence = False
    for n, line in enumerate(text.splitlines(), 1):
        if re.match(r'^\s*(```+|~~~+)', line):
            in_fence = not in_fence
        if in_fence:
            continue
        for href in re.findall(r'(?<!!)\[[^\]]+\]\(([^\s)]+)(?:\s+"[^"]*")?\)', line):
            url = urllib.parse.urlsplit(href)
            if url.scheme or not url.path:
                continue
            target = (ROOT / path).parent / urllib.parse.unquote(url.path)
            if not target.exists():
                result.append({'line': n, 'href': href})
    return result

for path in allowed:
    current = (ROOT / path).read_bytes()
    base = git('show', f'{BASE}:{path}')
    pin = git('show', f'{PIN}:{path}')
    edit = path in product
    reason = ('Edit source-supported task/contract facets; detailed rationale in decisions.md. '
              'Remaining E/M/P/Q/B facets and caller qualification remain held.' if edit else
              reasons.get(path, 'Retain existing text; no released candidate correction selected here. '
                                   'No new whole-file semantic acceptance claimed.'))
    files.append({'path': path, 'decision': 'edit' if edit else 'retain', 'reason': reason,
                  'candidate_associations': associations[path], 'base_sha256': sha(base),
                  'product_pin_sha256': sha(pin), 'current_sha256': sha(current),
                  'base_equals_product_pin': base == pin})
    text = current.decode('utf-8-sig')
    old = base.decode('utf-8-sig')
    old_bad = {x['href'] for x in links(old, path)}
    for problem in links(text, path):
        row = dict(path=path, **problem)
        (existing_broken if problem['href'] in old_bad else new_broken).append(row)
    lines = text.splitlines(keepends=True)
    opening = None
    language = ''
    mark = ''
    for i, line in enumerate(lines):
        match = re.match(r'^\s*(`{3,}|~{3,})([^\r\n]*)', line)
        if not match:
            continue
        if opening is None:
            opening, mark, language = i, match[1][0], match[2].strip()
        elif match[1][0] == mark:
            body = ''.join(lines[opening + 1:i])
            if language in ('csharp', 'cs'):
                historical = '// Before' in body or 'IYubiKeyManager manager' in body or 'ProtocolFactory.Create' in body
                complete = any(x in body for x in ('static async Task SendProtectedAsync',
                    'static async Task ReadAndVerifyPinAsync', 'static async Task DiscoverWithTemporaryLoggingAsync'))
                category = ('illustrative-historical-before-after' if historical else
                            'compilable-method-unchecked' if complete else 'illustrative-composable-fragment')
                why = ('Historical API/negative comparison, excluded from current compilation.' if historical else
                       'Complete typed method with scoped owned resources; compiler/analyzer binding NOT RUN.' if complete else
                       'API/pattern fragment requiring surrounding imports, typed caller inputs or app/test context; '
                       'some examples deliberately contrast rejected designs. Not a standalone consumer program.')
            else:
                category = 'illustrative-non-csharp'
                why = 'Command/configuration/diagram/tree/schema illustration, not a C# compilation body.'
            examples.append({'path': path, 'kind': 'fence', 'opening_line': opening + 1,
                'closing_line': i + 1, 'body_first_line': opening + 2, 'body_last_line': i,
                'language': language, 'body_sha256': sha(body.encode()), 'body': body,
                'classification': category, 'reason': why,
                'compiler_state': 'NOT RUN', 'execution_state': 'NOT RUN'})
            opening = None
    if opening is not None:
        unclosed.append({'path': path, 'opening_line': opening + 1})
    for match in re.finditer(r'<code(?:\s[^>]*)?>(.*?)</code>', text, re.S):
        body = match[1]
        examples.append({'path': path, 'kind': 'xml-code', 'body': body,
            'body_first_line': text.count('\n', 0, match.start(1)) + 1,
            'body_last_line': text.count('\n', 0, match.end(1)) + 1,
            'body_sha256': sha(body.encode()), 'classification': 'illustrative-embedded-xml',
            'reason': 'Embedded XML illustration in Markdown, not standalone SDK source.',
            'compiler_state': 'NOT RUN', 'execution_state': 'NOT RUN'})

candidate_rows = []
outside = []
for group in mapping['groups']:
    for local in group['local_records']:
        owned = [p for p in local['documentation_surface_refs'] if permitted(p)]
        external = [p for p in local['documentation_surface_refs'] if not permitted(p)]
        decisions = [dict(path=p, decision=next(x['decision'] for x in files if x['path'] == p),
                         reason=next(x['reason'] for x in files if x['path'] == p)) for p in owned]
        candidate_rows.append({'local_id': local['local_id'], 'proposal_id': group['proposal_id'],
            'original_facets': group['facets'], 'd_facet_released': 'D' in group['facets'],
            'shared_path_decisions': decisions, 'other_owner_paths': external,
            'proposal_rationale': group['proposal_and_rationale'],
            'remaining_trigger': group['implementation_reopen_trigger_or_needed_probe'],
            'evidence_unit_refs': local['evidence_unit_refs'],
            'inherited_association_keys': group['inherited_association_keys'],
            'independent_review': 'pending', 'implementation': 'unchanged', 'execution': 'not run'})
        for path in external:
            outside.append({'path': path, 'local_id': local['local_id'], 'proposal_id': group['proposal_id'],
                'decision': 'hold-for-exact-owner',
                'reason': 'Outside SHARED writable product scope; preserve bytes and route to C#/applet/'
                          'migration/config/diagram/historical owner as applicable.',
                'next_action': group['implementation_reopen_trigger_or_needed_probe']})

patch = git('diff', '--binary', '--full-index', '--src-prefix=a/', '--dst-prefix=b/', BASE, '--', *product)
(OUT / 'product.patch').write_bytes(patch)
put('path-dispositions.json', files)
put('changed-path-sha256.json', [x for x in files if x['decision'] == 'edit'])
put('candidate-map.json', candidate_rows)
put('outside-scope-followups.json', outside)
put('example-bodies.json', examples)
put('markdown-checks.json', {'new_missing_local_paths': new_broken,
    'preexisting_missing_local_paths': existing_broken, 'unclosed_fences': unclosed,
    'scope': 'Local file targets outside fences; anchors/external URLs and semantic binding not checked.'})
inputs = [RUN/'plan.md', RUN/'triage/editing-release.md', RUN/'triage/full-family/map.json',
          RUN/'inventory/shared-sections.json', ROOT/'docs/research/documentation-audit/workflow.md',
          ROOT/'docs/research/documentation-audit/records/style.md'] + list(RUN.glob('investigation-*.md'))
put('input-identities.json', [{'path': str(p.relative_to(ROOT)), 'sha256': sha(p.read_bytes())} for p in inputs])
external_inputs = []
run_suffix = pathlib.Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
for tree, names in {
    'yubikit-core-docs-edit-discovery': ['edit-A.md', 'editing/A/review.md',
        'editing/A/candidate-dispositions.json'],
    'yubikit-core-docs-edit-crypto': ['edit-C.md', 'editing/C/shared-followups.md',
        'editing/C/source-review.md', 'editing/C/candidate-map.json', 'editing/C/examples.json'],
    'yubikit-core-docs-edit-protocols': ['edit-protocols.md',
        'editing/protocols/outside-owner-handoffs.json', 'editing/protocols/candidate-decisions.json',
        'editing/protocols/citations.md', 'editing/protocols/examples.json'],
    'yubikit-core-docs-edit-transports': ['editing/transports/followups.json'],
    'yubikit-core-docs-edit-fido-boundaries': ['editing/FIDO-boundaries/citations-and-handoff.md',
        'editing/FIDO-boundaries/candidate-decisions.json'],
    'Yubico.Yubikit.NET': [f'boundary-{m}.md' for m in
        ['Management', 'Ctap2', 'WebAuthn', 'SecurityDomain', 'Oath', 'YubiOtp']] +
        ['boundaries/Ctap2/followup-B4-B6.md', 'boundaries/Ctap2/followup-WebAuthn.md'],
}.items():
    for name in names:
        p = ROOT.parent / tree / run_suffix / name
        external_inputs.append({'path': str(p), 'sha256': sha(p.read_bytes()),
            'authority': 'Read-only completed source/editor dependency; not blanket independent acceptance.'})
put('external-input-identities.json', external_inputs)
check = subprocess.run(['git', 'diff', '--check'], cwd=ROOT, capture_output=True, text=True)
patch_check = subprocess.run(['git', 'apply', '--reverse', '--check', str(OUT / 'product.patch')],
                             cwd=ROOT, capture_output=True, text=True)
receipt = {'base': BASE, 'product_pin': PIN, 'product_patch_sha256': sha(patch),
    'allowed_document_count': len(files), 'edited_document_count': len(product),
    'retained_document_count': len(files)-len(product), 'local_candidate_count': len(candidate_rows),
    'proposal_count': len(mapping['groups']), 'example_body_count': len(examples),
    'new_missing_local_paths': len(new_broken), 'unclosed_fences': len(unclosed),
    'git_diff_check_exit': check.returncode, 'git_diff_check_output': check.stdout + check.stderr,
    'product_patch_reverse_check_exit': patch_check.returncode,
    'product_patch_reverse_check_output': patch_check.stdout + patch_check.stderr,
    'no_csharp_config_asset_or_frozen_tracked_changes': set(changed) == set(product),
    'index_unchanged': True, 'build_tests_compiler_formatter_native_crypto_device': 'NOT RUN',
    'independent_review': 'pending'}
put('verification.json', receipt)
assert not new_broken and not unclosed and check.returncode == 0 and patch_check.returncode == 0, receipt
outputs = [p for p in OUT.iterdir() if p.is_file() and p.name != 'output-identities.json']
report = RUN / 'edit-SHARED.md'
if report.exists():
    outputs.append(report)
put('output-identities.json', [{'path': str(p.relative_to(RUN)), 'sha256': sha(p.read_bytes())}
                               for p in sorted(outputs)])
print(json.dumps(receipt, indent=2))
