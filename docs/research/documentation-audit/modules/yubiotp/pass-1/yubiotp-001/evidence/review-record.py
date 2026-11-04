"""Freeze this review's exact content and focused independently re-extracted source evidence."""
import hashlib
import json
from pathlib import Path
import re
import subprocess

E = Path(__file__).parent
R = E.parent
ROOT = Path.cwd()
BASE = '95e5ccaeda33954c21f2a38058d4f66b5f208787'
rows = re.findall(r'\| `([^`]+)` \| (\d+) \| `([0-9a-f]{64})` \|', (R / 'edit-report.md').read_text())
required = {
    'src/YubiOtp/src/ExtendedFlag.cs': ['OTP-REV-001'],
    'src/YubiOtp/examples/OtpTool/Commands/YubiOtpCommand.cs': ['OTP-REV-002'],
    'src/YubiOtp/README.md': ['OTP-REV-003'],
}
files = []
for path, size, sha in rows:
    data = (ROOT / path).read_bytes()
    actual = hashlib.sha256(data).hexdigest()
    baseline = subprocess.check_output(['git', 'show', BASE + ':' + path])
    files.append(dict(path=path, bytes=len(data), sha256=actual,
                      matches_editor_handoff=(len(data) == int(size) and actual == sha),
                      baseline_sha256=hashlib.sha256(baseline).hexdigest(),
                      verdict='required-correction' if path in required else 'accepted-content',
                      required=required.get(path, []),
                      scope='YubiOTP-only assigned sections' if path.startswith('docs/') else 'whole affected file'))
assert len(files) == 50 and sum(f['path'].endswith('.cs') for f in files) == 42
(E / 'review-content-manifest.json').write_text(json.dumps(dict(
    date='2026-11-04', baseline=BASE, reviewer='github-copilot/gpt-6.1-sol',
    verdict='47 accepted-content; 3 required-correction; hardware-sensitive acceptance conditional',
    files=files), indent=2) + '\n')

selections = {
    'vendor-technical-manual-v3.3.txt': [(1,18),(1173,1184),(1298,1311),(1348,1450),(1480,1494),(1557,1567),(1736,1752)],
    'personalization-1.20.0-manual.txt': [(59,65),(104,116),(277,302),(324,338)],
    'personalization-1.20.0-ykpers.c': [(730,760)],
    'personalization-1.20.0-ykdef.h': [(105,115),(125,139),(180,188),(246,252)],
    'ykman-5.8.0-yubiotp.py': [(370,394),(454,462),(609,615),(820,850),(901,929)],
}
pdf_logs = []
texts = {}
for name in ['vendor-technical-manual-v3.3', 'nfc-uri-rtd-1.0-mirror', 'nfc-text-rtd-1.0-mirror']:
    result = subprocess.run(['pdftotext', '-layout', str(E / (name + '.pdf')), '-'], capture_output=True)
    texts[name + '.txt'] = result.stdout.decode()
    pdf_logs.append(dict(source=name + '.pdf', command='pdftotext -layout <pinned-original> -',
                         exit=result.returncode, stderr=result.stderr.decode(),
                         extracted_sha256=hashlib.sha256(result.stdout).hexdigest(),
                         matches_investigator_text=(result.stdout == (E / (name + '.txt')).read_bytes())))

out = ['# Fresh reviewer original-source checks — 2026-11-04', '',
       'Fresh retrieval hashes are in review-retrieval.json. All ten fetched sources matched pinned bytes;',
       'no binary duplicate was saved. The Manual 3.4 404 is retained there. PDFs below were freshly',
       'extracted from those hash-matched originals, not accepted from the investigator excerpt.', '']
for name, ranges in selections.items():
    lines = texts.get(name, (E / name).read_text()).splitlines()
    out += ['## ' + name, '']
    for first, last in ranges:
        out += ['```text'] + [f'{i}: {lines[i-1]}' for i in range(first, min(last, len(lines)) + 1)] + ['```', '']
for name in ['nfc-uri-rtd-1.0-mirror.txt', 'nfc-text-rtd-1.0-mirror.txt']:
    lines = texts[name].splitlines()
    # Keep syntax/encoding sections and distinct revision-status rows, not the copyright boilerplate.
    start = next(i for i, line in enumerate(lines) if line.startswith('3.2 Records Mapping'))
    end = next(i for i in range(start + 1, len(lines)) if lines[i].startswith('4 Handling Guideline') or lines[i].startswith('A. Example UTF-8 Encoding'))
    revision = next(i for i in range(end, len(lines)) if lines[i].startswith('B. Revision History'))
    out += ['## ' + name, '', '```text']
    out += [f'{i+1}: {lines[i]}' for i in range(start, end)]
    out += [f'{i+1}: {lines[i]}' for i in range(revision, len(lines))]
    out += ['```', '']
out += ['## Extraction commands and original diagnostics', '', '```json', json.dumps(pdf_logs, indent=2), '```', '',
        '## Fresh Context7 check', '',
        'Resolved .NET to `/dotnet/docs` and queried ownership/lifetime and immutable strings.',
        'Returned https://github.com/dotnet/docs/blob/main/docs/csharp/fundamentals/strings/index.md',
        '(created string characters cannot change) and',
        'https://github.com/dotnet/docs/blob/main/docs/standard/memory-and-spans/memory-t-usage-guidelines.md',
        '(single ownership or transfer; async memory consumption ends at terminal Task state).',
        'These do not prove SDK output ownership; that conclusion uses module/backend return paths.', '']
(E / 'review-original-clauses.md').write_text('\n'.join(out))
print(json.dumps({'files':len(files), 'matches_editor':all(f['matches_editor_handoff'] for f in files),
                  'accepted':sum(not f['required'] for f in files), 'pdf_extraction':pdf_logs}, indent=2))
