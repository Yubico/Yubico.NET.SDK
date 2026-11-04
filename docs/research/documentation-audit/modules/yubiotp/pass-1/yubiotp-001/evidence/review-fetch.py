"""Independent bounded original retrieval; reuse pinned bytes only after fresh hash match."""
import concurrent.futures
import datetime
import hashlib
import json
from pathlib import Path
import urllib.request

ROOT = Path(__file__).parent
NAMES = {
    'vendor-technical-manual-v3.3.pdf', 'personalization-1.20.0-manual.txt',
    'personalization-1.20.0-ykdef.h', 'personalization-1.20.0-ykpers.c',
    'ykman-5.8.0-yubiotp.py', 'nfc-uri-rtd-official.html',
    'nfc-text-rtd-official.html', 'nfc-uri-rtd-1.0-mirror.pdf',
    'nfc-text-rtd-1.0-mirror.pdf', 'rfc4226.txt'
}
rows = [r for manifest in ('source-capture.json', 'source-followup.json')
        for r in json.loads((ROOT / manifest).read_text())['sources']
        if r.get('file') in NAMES or r.get('attempted_file') == 'vendor-manual-v3.4.pdf']

def retrieve(row):
    result = {'url': row['url'], 'pinned_file': row.get('file')}
    try:
        with urllib.request.urlopen(row['url'], timeout=35) as response:
            data = response.read(2_000_000)
            result.update(status=response.status, final_url=response.url,
                          bytes=len(data), sha256=hashlib.sha256(data).hexdigest())
        name = row.get('file')
        same = name and (ROOT / name).read_bytes() == data
        result['fresh_bytes_match_pinned'] = bool(same)
        if not same:
            target = ROOT / ('review-fresh-' + (name or 'unidentified'))
            target.write_bytes(data)
            result['retained'] = target.name
    except Exception as error:
        result['error'] = str(error)
    return result

with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
    results = list(pool.map(retrieve, rows))
(ROOT / 'review-retrieval.json').write_text(json.dumps({
    'date': datetime.datetime.now(datetime.timezone.utc).isoformat(),
    'reviewer': 'github-copilot/gpt-6.1-sol; fresh content/citation reviewer',
    'round': 'initial; no follow-up needed if pinned original clauses resolve claims',
    'sources': results
}, indent=2) + '\n')
print(json.dumps(results, indent=2))
