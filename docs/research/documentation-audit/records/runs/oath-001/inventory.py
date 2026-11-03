"""Capture audit metadata without changing repository content."""
import hashlib
import json
import re
import subprocess
from datetime import datetime, timezone
from pathlib import Path

REPO = Path('/home/rupicola/Code/y/Yubico.Yubikit.NET')
OUT = Path(__file__).parent


def command(*args):
    result = subprocess.run(args, cwd=REPO, text=True, capture_output=True, check=True)
    return result.stdout


def save(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2) + '\n')


tracked = command('git', 'ls-files', '-z').split('\0')
tracked = [p for p in tracked if p]
revision = command('git', 'rev-parse', 'HEAD').strip()
active_output = command('dotnet', 'toolchain.cs', '--', 'docs-list-active')
(OUT / 'active-docs.txt').write_text(active_output)
active = active_output.splitlines()
assert active and all(p.endswith('.md') and (REPO / p).is_file() for p in active), 'Unexpected active-doc output'
oath_reference = re.compile(r'\bOATH\b|YubiKit\.Oath|\bOathSession\b|\bIOathSession\b|\bOathCredential\w*', re.I)
module = [p for p in tracked if p.startswith('src/Oath/')]
surfaces = [p for p in module if Path(p).suffix in ('.md', '.cs')]
references = []
for path in tracked:
    if path.startswith('src/Oath/') or not path.endswith('.md'):
        continue
    lane = ('active' if path in active else 'migration' if path.startswith('docs/migration/')
            else 'package' if Path(path).name == 'PACKAGE_README.md' else None)
    if lane is None:
        continue
    hits = [{'line': number, 'text': line.strip()} for number, line in enumerate((REPO / path).read_text().splitlines(), 1)
            if oath_reference.search(line)]
    if hits:
        references.append({'path': path, 'lane': lane, 'hits': hits, 'semantic_status': 'pending'})

coverage = []
signals = []
pattern = re.compile(r'\bTODO\b|\bFIXME\b|\bTBD\b|\bHACK\b|\bXXX\b|\btemporary\b|\bworkaround\b|\bbackward\w*\b|\bbreaking change\b|\bfor now\b|\beventually\b|\bRFC\s*\d+', re.I)
for path in surfaces:
    raw = (REPO / path).read_bytes()
    lines = raw.decode().splitlines()
    coverage.append({
        'path': path, 'baseline': revision, 'sha256': hashlib.sha256(raw).hexdigest(),
        'lines': len(lines), 'surface': 'markdown' if path.endswith('.md') else 'source-comments',
        'lane': 'active' if path in active else 'supplemental',
        'mechanical': {'pattern_scan': 'complete', 'links_xml_symbols': 'pending'},
        'semantic': 'pending', 'deep_verification': 'pending', 'independent_review': 'pending',
    })
    for number, line in enumerate(lines, 1):
        if pattern.search(line) and (path.endswith('.md') or line.lstrip().startswith(('//', '/*', '*'))):
            signals.append({'path': path, 'line': number, 'text': line.strip(), 'status': 'unverified-signal'})

save('baseline.json', {
    'captured_at': datetime.now(timezone.utc).isoformat(), 'repository': str(REPO),
    'branch': command('git', 'branch', '--show-current').strip(), 'commit': revision,
    'merge_base_origin_yubikit': command('git', 'merge-base', 'HEAD', 'origin/yubikit').strip(),
    'status_porcelain': command('git', 'status', '--porcelain=v1').splitlines(),
    'investigator_model': 'github-copilot/gpt-6.1-sol',
    'investigator_session': 'ses_e629b0717ffedUbklz42fAb2hw',
    'counts': {'active_docs_repository': len(active), 'tracked_oath_files': len(module),
               'oath_documentation_surfaces': len(surfaces),
               'oath_markdown': sum(p.endswith('.md') for p in surfaces),
               'oath_source_files': sum(p.endswith('.cs') for p in surfaces),
               'cross_module_reference_files': len(references), 'pattern_signals': len(signals)},
    'commands': ['git ls-files -z', 'git rev-parse HEAD', 'dotnet toolchain.cs -- docs-list-active',
                 'git branch --show-current', 'git merge-base HEAD origin/yubikit', 'git status --porcelain=v1'],
    'limitations': ['Pattern scans are lexical candidates, not semantic findings.',
                    'Cross-module references are lexical discovery, not exhaustive semantic coverage.',
                    'Build outputs and untracked research files are excluded.',
                    'No tests, hardware checks, docs-qa, or full module audit performed.'],
})
save('coverage.json', {'module_surfaces': coverage, 'external_reference_surfaces': references,
                       'supporting_files': [p for p in module if p not in surfaces]})
save('mechanical-signals.json', signals)
print(json.dumps(json.loads((OUT / 'baseline.json').read_text())['counts'], indent=2))
