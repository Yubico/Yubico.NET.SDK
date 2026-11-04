"""One focused follow-up: original encoders and NFC RTD editions, not further research."""
import hashlib
import json
from pathlib import Path
import urllib.request

root = Path(__file__).parent
sources = {
    'personalization-1.20.0-ykpers.c': 'https://raw.githubusercontent.com/Yubico/yubikey-personalization/v1.20.0/ykpers.c',
    'nfc-uri-rtd-official.html': 'https://nfc-forum.org/build/specifications/uri-rtd-technical-specification/',
    'nfc-text-rtd-official.html': 'https://nfc-forum.org/build/specifications/text-rtd-technical-specification/',
    'nfc-uri-rtd-1.0-mirror.pdf': 'https://berlin.ccc.de/~starbug/felica/NFCForum-TS-RTD_URI_1.0.pdf',
    'nfc-text-rtd-1.0-mirror.pdf': 'https://berlin.ccc.de/~starbug/felica/NFCForum-TS-RTD_Text_1.0.pdf',
}
records = []
for name, url in sources.items():
    try:
        with urllib.request.urlopen(url, timeout=40) as response:
            data = response.read()
            (root / name).write_bytes(data)
            record = dict(file=name, url=url, status=response.status, bytes=len(data), sha256=hashlib.sha256(data).hexdigest())
    except Exception as error:
        record = dict(file=None, attempted_file=name, url=url, error=str(error))
    records.append(record)
    print(json.dumps(record))
(root / 'source-followup.json').write_text(json.dumps(dict(access_date='2026-11-04', sources=records), indent=2) + '\n')
