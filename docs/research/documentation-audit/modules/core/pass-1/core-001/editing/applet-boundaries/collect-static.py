"""Capture this editor's exact path decisions, diff and static checks; no SDK execution."""
from pathlib import Path
import difflib
import hashlib
import html
import json
import re
import subprocess
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
RUN_REL = Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PRIMARY = Path('/home/rupicola/Code/y/Yubico.Yubikit.NET')
INPUT = PRIMARY / RUN_REL
MODULES = ['Management', 'SecurityDomain', 'Oath', 'YubiOtp', 'YubiHsm']

def sha(raw):
    return hashlib.sha256(raw).hexdigest()

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)

def save(name, value):
    (HERE / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')

inputs = []
files = []
indices = []
excluded = []
for module in MODULES:
    folder = INPUT / 'boundaries' / module
    index_name = 'candidates.json' if module == 'Management' else 'candidate-index.json'
    indices.extend(json.loads((folder / index_name).read_text()))
    for path in [INPUT / f'boundary-{module}.md', *sorted(folder.rglob('*'))]:
        if path.is_file():
            inputs.append({'primary_path': str(path.relative_to(PRIMARY)), 'sha256': sha(path.read_bytes())})
    for row in json.loads((folder / 'file-dispositions.json').read_text()):
        if row['path'].startswith(f'src/{module}/') and Path(row['path']).suffix in {'.cs', '.md'}:
            files.append((module, row))
        else:
            raw = (ROOT / row['path']).read_bytes()
            baseline = git('show', 'HEAD:' + row['path'])
            expected_hash = next(row[k] for k in ['source_sha256', 'raw_sha256', 'sha256'] if k in row)
            assert sha(raw) == expected_hash, (row['path'], 'outside-scope raw bytes differ from final ledger')
            assert raw.decode('utf-8-sig').replace('\r\n', '\n') == baseline.decode('utf-8-sig').replace('\r\n', '\n')
            excluded.append({'module': module, 'path': row['path'], 'decision': 'HOLD-OUTSIDE-EDIT-SCOPE',
                             'reason': 'Outside the five-module .cs-comment/.md ownership; retain exact bytes.',
                             'before_git_HEAD_sha256': sha(baseline), 'after_worktree_sha256': sha(raw),
                             'before_worktree_source_ledger_sha256': expected_hash,
                             'git_comparison': 'Explicit UTF-8 BOM/CRLF normalization; raw ledger equality checked separately',
                             'original_boundary_disposition': row})
for path in [INPUT / 'triage/editing-release.md', INPUT / 'triage-full-family.md']:
    inputs.append({'primary_path': str(path.relative_to(PRIMARY)), 'sha256': sha(path.read_bytes())})

changed = set(git('diff', '--name-only', 'HEAD', '--', 'src').decode().splitlines())
assert len({row['id'] for row in indices}) == len(indices) == 33
eligible = {row['path'] for _, row in files}
assert changed <= eligible, sorted(changed - eligible)
decisions = []
checks = []
examples = []
markdown = []
issues = []

def executable_lines(text):
    # Every C# edit here is a full // or /// line. Comparing all other lines is stronger than
    # whitespace-insensitive token comparison for this diff, and preserves directives/strings exactly.
    return '\n'.join(line for line in text.splitlines() if not line.lstrip().startswith('//'))

def xml_errors(text):
    errors = []
    for match in re.finditer(r'(?m)(?:^[ \t]*///[^\n]*(?:\n|$))+', text):
        body = re.sub(r'(?m)^[ \t]*/// ?', '', match[0])
        try:
            ET.fromstring('<doc>' + body + '</doc>')
        except ET.ParseError as error:
            errors.append(str(error))
    return errors

for module, original in files:
    path = original['path']
    before_raw = git('show', f'HEAD:{path}')
    after_raw = (ROOT / path).read_bytes()
    before = before_raw.decode('utf-8-sig')
    after = after_raw.decode('utf-8-sig')
    candidates = original.get('candidates', original.get('candidate_ids', []))
    note = next((original[k] for k in ['note', 'observation', 'semantic_observation', 'authored_context',
                                    'unchanged_reason', 'reason'] if k in original), None)
    ranges = [{'before': [i1 + 1, i2] if i2 > i1 else None,
               'after': [j1 + 1, j2] if j2 > j1 else None,
               'before_line_count': i2 - i1, 'after_line_count': j2 - j1,
               'insertion_or_deletion_anchor_before_line': i1 + 1, 'kind': kind}
              for kind, i1, i2, j1, j2 in difflib.SequenceMatcher(
                  a=before.splitlines(), b=after.splitlines(), autojunk=False).get_opcodes() if kind != 'equal']
    decisions.append({
        'module': module, 'path': path, 'candidate_ids_from_original_file_ledger': candidates,
        'decision': 'EDIT' if path in changed else 'RETAIN',
        'reason': ('Apply source-supported boundary prose; executable/engineering and qualification facets remain held.'
                   if path in changed else
                   'Retain existing local contract/rationale and assertion scope. No additional boundary prose correction '
                   'is needed here; any runtime, assertion, output, policy or evidence change remains separately held.'),
        'original_boundary_context': note,
        'original_boundary_disposition': original,
        'before_git_HEAD_sha256': sha(before_raw), 'after_worktree_sha256': sha(after_raw),
        'changed_ranges_1_based_inclusive': ranges,
        'terminal_newline_before': before_raw.endswith(b'\n'), 'terminal_newline_after': after_raw.endswith(b'\n')})
    original_hash = next(original[k] for k in ['source_sha256', 'raw_sha256', 'sha256'] if k in original)
    assert sha(before_raw) == original_hash, (path, 'baseline differs from final boundary file ledger')
    if path.endswith('.cs') and path in changed:
        same = executable_lines(before) == executable_lines(after)
        old_errors, new_errors = xml_errors(before), xml_errors(after)
        checks.append({'path': path, 'all_non_comment_lines_exact': same,
                       'before_xml_errors': old_errors, 'after_xml_errors': new_errors})
        if not same or new_errors:
            issues.append(path)
    if path.endswith('.md'):
        fences = re.findall(r'(?m)^```[^\n]*', after)
        local_missing = []
        for target in re.findall(r'\]\(([^\s)]+)(?:\s+[^)]*)?\)', after):
            clean = target.split('#')[0]
            if clean and not re.match(r'[A-Za-z][A-Za-z0-9+.-]*:', clean):
                if not ((ROOT / path).parent / clean).exists():
                    local_missing.append(target)
        markdown.append({'path': path, 'fence_marker_count': len(fences), 'balanced_fences': len(fences) % 2 == 0,
                         'missing_local_link_targets': local_missing})
        if len(fences) % 2:
            issues.append(path)
        pattern = r'(?m)^```([^\n]*)\n(.*?)\n```'
    else:
        pattern = r'<code>\s*\n(.*?)</code>'
    for ordinal, match in enumerate(re.finditer(pattern, after, re.S), 1):
        language = match[1].strip() if path.endswith('.md') else 'xml-code'
        raw_body = match[2] if path.endswith('.md') else match[1]
        body = html.unescape(re.sub(r'(?m)^[ \t]*/// ?', '', raw_body)).rstrip()
        classification = 'composable-fragment'
        if language not in {'cs', 'csharp', 'xml-code'}:
            classification = 'non-CSharp-command-or-informational-block'
        elif path == 'src/YubiOtp/src/SlotConfiguration.cs':
            classification = 'wire-layout-not-CSharp'
        elif path == 'src/YubiHsm/README.md' and ('static async Task ' in body or
                                                  'interface ISymmetricHsmExchange' in body):
            classification = 'application-dependent-typed-method-sketch'
        elif path == 'src/SecurityDomain/README.md' and body.startswith('static async Task'):
            classification = 'complete-composable-method-body'
        elif path == 'src/Management/CLAUDE.md' and (body.startswith('public static ') or
                                                    body.startswith('// ManagementSession delegates')):
            classification = 'implementation-excerpt-not-consumer-program'
        elif body.lstrip().startswith('[SkippableTheory]'):
            classification = 'test-class-member-fragment-xunit-v2-context'
        elif 'sealed class TouchPrompt' in body:
            classification = 'prompt-type-fragment'
        elif path.endswith('/README.md') and body.startswith('using System'):
            classification = 'complete-listing-body-with-stated-imports'
        prior_pattern = pattern
        before_matches = list(re.finditer(prior_pattern, before, re.S))
        # Body identity rather than ordinal handles the newly inserted SD method.
        existing = any(body == html.unescape(re.sub(r'(?m)^[ \t]*/// ?', '',
                           old[2] if path.endswith('.md') else old[1])).rstrip() for old in before_matches)
        examples.append({'path': path, 'ordinal_all_fences_or_code_blocks': ordinal,
                         'first_line': after[:match.start()].count('\n') + 1,
                         'last_line': after[:match.end()].count('\n') + 1,
                         'language': language, 'body_sha256_utf8': sha(body.encode()), 'body': body,
                         'classification': classification, 'body_change': 'RETAINED' if existing else 'NEW-OR-CHANGED',
                         'compilation': 'NOT-RUN / VALID-NOT-CHECKED' if classification not in
                             {'non-CSharp-command-or-informational-block', 'wire-layout-not-CSharp'} else 'NOT-APPLICABLE',
                         'context': 'Use full surrounding source prerequisites/imports/scopes; blocks are not concatenated. '
                                    'No compilation, SDK test or hardware execution is credited by this inventory.'})

save('input-identities.json', inputs)
save('path-decisions.json', decisions)
save('outside-scope-paths.json', excluded)
save('examples.json', examples)
git('diff', '--check')
save('static-checks.json', {'baseline_HEAD': git('rev-parse', 'HEAD').decode().strip(),
     'product_source_pin': 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef',
     'all_233_before_hashes_match_final_boundary_file_ledgers': True,
     'original_local_candidate_groups': len(indices),
     'git_diff_check': 'PASS',
     'eligible_paths': len(eligible), 'edited_paths': len(changed), 'retained_paths': len(eligible - changed),
     'csharp_preservation_and_XML': checks, 'Markdown': markdown,
     'issues': issues, 'SDK_build_tests_format_native_device_crypto_connector': 'NOT-RUN',
     'independent_content_citation_review': 'PENDING'})
(HERE / 'product.diff').write_bytes(git('diff', '--no-ext-diff', '--binary', 'HEAD', '--', *sorted(changed)))
owned = [HERE.parent.parent / 'edit-applet-boundaries.md',
         *sorted(p for p in HERE.rglob('*') if p.is_file() and p.name != 'owned-output-identities.json')]
save('owned-output-identities.json', [{'path': str(p.relative_to(ROOT)), 'before': None,
     'after_sha256': sha(p.read_bytes()), 'bytes': p.stat().st_size} for p in owned])
print(json.dumps({'eligible_paths': len(eligible), 'edited_paths': len(changed), 'examples_all_languages': len(examples),
                  'CSharp_checks': len(checks), 'static_issues': issues,
                  'missing_local_link_targets': [r for r in markdown if r['missing_local_link_targets']]}, indent=2))
assert not issues, issues
