"""Bounded original-source captures; no SDK imports or execution."""
import concurrent.futures
import datetime
import hashlib
import html.parser
import json
import pathlib
import urllib.request

HERE = pathlib.Path(__file__).parent
SOURCES = {
    'ctap20.html': 'https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html',
    'ctap21.html': 'https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html',
    'ctap23.html': 'https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html',
    'ykcore.c': 'https://raw.githubusercontent.com/Yubico/yubikey-personalization/db0c0d641d47ee52e43af94dcee603d76186b6d3/ykcore/ykcore.c',
    'ykdef.h': 'https://raw.githubusercontent.com/Yubico/yubikey-personalization/db0c0d641d47ee52e43af94dcee603d76186b6d3/ykcore/ykdef.h',
    'ValueTask-v10.0.0.cs': 'https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Threading/Tasks/ValueTask.cs',
    'SemaphoreSlim-v10.0.0.cs': 'https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Threading/SemaphoreSlim.cs',
    'ExecutionContext-v10.0.0.cs': 'https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Threading/ExecutionContext.cs',
}

class Text(html.parser.HTMLParser):
    def __init__(self):
        super().__init__(); self.parts = []
    def handle_data(self, data):
        if data.strip(): self.parts.append(data.strip())

def capture(item):
    name, url = item
    result = {'name': name, 'url': url, 'accessed_utc': datetime.datetime.now(datetime.timezone.utc).isoformat()}
    try:
        with urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': 'Core-documentation-audit'}), timeout=45) as response:
            data = response.read(); result.update(status=response.status, final_url=response.url)
        (HERE / name).write_bytes(data)
        result.update(bytes=len(data), sha256=hashlib.sha256(data).hexdigest())
        if name.endswith('.html'):
            parser = Text(); parser.feed(data.decode('utf-8-sig'))
            (HERE / name.replace('.html', '.txt')).write_text('\n'.join(parser.parts) + '\n')
    except Exception as error:
        result['error'] = str(error)
    return result

if __name__ == '__main__':
    import sys
    previous = HERE / 'captures.json'
    selected = {k: v for k, v in SOURCES.items() if not sys.argv[1:] or k in sys.argv[1:]}
    prior = json.loads(previous.read_text()) if previous.exists() else []
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as executor:
        results = list(executor.map(capture, selected.items()))
    output = 'captures-followup.json' if prior else 'captures.json'
    (HERE / output).write_text(json.dumps(results, indent=2) + '\n')
    print(json.dumps(results, indent=2))
