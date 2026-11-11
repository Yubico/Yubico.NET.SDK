"""Read-only bounded discovery-caller reconciliation with both pinned parent guides."""
import json
from pathlib import Path
import re
import subprocess
import prepare

ROOT, HERE = prepare.ROOT, prepare.HERE
PARENTS = dict(campaign='f0ba9687e9532e0a08b3ea17cacd9c52a6ba6ffa',
               incoming='5746e72ed9a848d08b5782ddde025aa4a4b522ef')
CALLERS = re.compile(r'\b(?:YubiKeyManager|IYubiKeyManager|CachedDeviceInfo|StartMonitoring|ShutdownAsync)\b|\.WatchAsync\(')


def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT, text=True)


def active(path):
    return (path in ('CLAUDE.md', 'README.md') or path.startswith(('src/', 'docs/'))) and not path.startswith(
        ('docs/research/', 'docs/archive/', 'docs/completed/', 'docs/plans/', 'docs/reviews/',
         'docs/specs/', 'docs/templates/'))


def main():
    data = json.loads((HERE / 'inputs.json').read_text())
    selected = {(row['source'], row.get('fence')): row['id'] for row in data['groups'] if 'fence' in row}
    rows = []
    for path in git('ls-files', '--', '*.md').splitlines():
        if not active(path) or not (ROOT / path).is_file():
            continue
        text = (ROOT / path).read_text()
        parent_text = {}
        for name, revision in PARENTS.items():
            exists = subprocess.run(['git', 'cat-file', '-e', revision + ':' + path], cwd=ROOT,
                                    stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).returncode == 0
            parent_text[name] = git('show', revision + ':' + path) if exists else ''
        for number, match in enumerate(prepare.FENCES.finditer(text), 1):
            if match[1] != 'csharp' or not CALLERS.search(match[2]):
                continue
            headings = re.findall(r'^#{1,6} .+$', text[:match.start()], re.M)
            raw = match[2]
            historical = (path == 'docs/usage/device-discovery.md' and number == 11 or
                          path == 'docs/TESTING.md' and '**Old (fragile):**' in text[max(0, match.start()-80):match.start()])
            illustrative = path == 'src/Tests.Shared/CLAUDE.md' or (path == 'CLAUDE.md' and number == 5)
            covered = selected.get((path, number))
            disposition = ('complete current body prepared as ' + covered if covered else
                           'historical before-example; deliberately not compiled' if historical else
                           'explicit illustrative/incomplete infrastructure shell; no invented completion' if illustrative else
                           'retained caller; reconcile signatures and parent body, not a new whole-module audit')
            rows.append(dict(source=path, fence=number, heading=headings[-1] if headings else '',
                             first_line=text[:match.start(2)].count('\n') + 1,
                             complete_body_sha256=prepare.sha(raw.encode()), body=raw,
                             source_sha256=prepare.sha((ROOT / path).read_bytes()), disposition=disposition,
                             parents={name: dict(full_document_sha256=prepare.sha(value.encode()) if value else None,
                                                exact_body_present=raw in value,
                                                code_bodies=[dict(fence=n, sha256=prepare.sha(m[2].encode()), body=m[2])
                                                             for n, m in enumerate(prepare.FENCES.finditer(value), 1)
                                                             if m[1] == 'csharp' and CALLERS.search(m[2])])
                                      for name, value in parent_text.items()}))
    print(json.dumps(dict(scope='Phase 0 affected discovery callers only; not a whole-Core audit',
                          parents=PARENTS, markdown_callers=rows,
                          xml_prepared=[row for row in data['groups'] if 'xml_code' in row]), indent=2))


if __name__ == '__main__':
    main()
