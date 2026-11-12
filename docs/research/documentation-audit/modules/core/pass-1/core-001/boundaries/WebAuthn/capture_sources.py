"""Capture selected original text. HTTP and static parsing only."""
import datetime
import hashlib
import json
import pathlib
import sys
import urllib.request

OUT = pathlib.Path(__file__).resolve().parent / 'sources'
SOURCES = {
    'webauthn3.html': 'https://www.w3.org/TR/2026/REC-webauthn-3-20260825/',
    'preview-sign-v4.html': 'https://yubicolabs.github.io/webauthn-sign-extension/4/',
    'Task-v10.0.0.cs': 'https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Threading/Tasks/Task.cs',
    'ReadOnlyMemory-v10.0.0.cs': 'https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/ReadOnlyMemory.cs',
    'ByteArrayOrderingRule-v7.0.0.cs': 'https://raw.githubusercontent.com/fluentassertions/fluentassertions/7.0.0/Src/FluentAssertions/Equivalency/Ordering/ByteArrayOrderingRule.cs',
    'FluentAssertions-objectgraphs-v7.0.0.md': 'https://raw.githubusercontent.com/fluentassertions/fluentassertions/7.0.0/docs/_pages/objectgraphs.md',
}

if __name__ == '__main__':
    OUT.mkdir(exist_ok=True)
    rows = json.loads((OUT / 'captures.json').read_text()) if (OUT / 'captures.json').exists() else []
    for name, url in SOURCES.items():
        if sys.argv[1:] and name not in sys.argv[1:]:
            continue
        row = {'url': url, 'access_utc': datetime.datetime.now(datetime.UTC).isoformat(), 'path': name}
        try:
            with urllib.request.urlopen(url, timeout=60) as response:
                data = response.read()
                row.update(status=response.status, sha256=hashlib.sha256(data).hexdigest(), bytes=len(data))
                (OUT / name).write_bytes(data)
        except Exception as error:
            row['error'] = str(error)
        rows = [old for old in rows if old['path'] != name] + [row]
    (OUT / 'captures.json').write_text(json.dumps(rows, indent=2) + '\n')
    print(json.dumps(rows, indent=2))
