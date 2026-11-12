"""Apply accepted external revision-4 hash-keyed classifications, retaining historical versions."""
import hashlib
import json
from pathlib import Path
import re

HERE = Path(__file__).resolve().parent
RUN = HERE.parents[2]
ROOT = HERE.parents[9]
SOURCE = RUN / 'editing/external/revision-4'


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def identity(path):
    raw = path.read_bytes()
    return dict(path=path.relative_to(ROOT).as_posix(), bytes=len(raw), sha256=sha(raw))


def main():
    inventory = json.loads((SOURCE / 'example-inventory.json').read_bytes())
    overlay = json.loads((SOURCE / 'example-classification-overlay.json').read_bytes())
    rows = inventory['initial_bodies'] + inventory['retained_primary_bodies']
    assert len(rows) == inventory['total_body_records'] == 247
    for update in overlay['updates']:
        successor = update['successor_entry']
        assert any(row == successor for row in inventory['initial_bodies'])
        assert sha(update['immutable_original']['body'].encode()) == update['key']['body_sha256']
    counters, results = {}, []
    current_index = 0
    for index, row in enumerate(rows):
        result = dict(id=f'EXT-{index:03}', authority=identity(SOURCE / 'example-inventory.json'),
                      classification_overlay=identity(SOURCE / 'example-classification-overlay.json'),
                      review=identity(SOURCE / 'review.md'), version=row['version'], path=row['path'],
                      editor_body_sha256=row['body_sha256'], editor_classification=row['classification'],
                      language=row['language'], compilation_status='historical-excluded', execution_status='not-run')
        assert sha(row['body'].encode()) == row['body_sha256']
        if row['version'] != 'current':
            result['qualification'] = 'Exact retained base/historical body, including explicitly superseded invalid API shapes. No current compile context inferred.'
            results.append(result)
            continue
        data = (ROOT / row['path']).read_bytes()
        markdown = row['path'].endswith('.md')
        pattern = rb'^[ \t]*```([^\r\n]*)\r?\n(.*?)^[ \t]*```[^\r\n]*' if markdown else rb'<code(?:\s[^>]*)?>(.*?)</code>'
        candidates = []
        for ordinal, match in enumerate(re.finditer(pattern, data, re.M | re.S), 1):
            raw = match.group(2 if markdown else 1)
            normalized = raw.decode().replace('\r\n','\n')
            prefix_only = re.sub(r'^\s*/// ?', '', normalized, flags=re.M)
            forms = [('literal',normalized), ('literal-strip',normalized.strip()), ('literal-rstrip',normalized.rstrip()),
                     ('xml-prefix-only',prefix_only), ('xml-prefix-strip',prefix_only.strip()), ('xml-prefix-rstrip',prefix_only.rstrip())]
            if any(sha(body.encode()) == row['body_sha256'] for _, body in forms):
                candidates.append((ordinal,match,raw))
        key = (row['path'],row['body_sha256'])
        occurrence = counters.get(key,0)
        counters[key] = occurrence+1
        assert candidates and occurrence < len(candidates), ('Current external source mismatch',row['path'],row['body_sha256'])
        ordinal,match,raw = candidates[occurrence]
        result.update(current_source_index=current_index, ordinal=ordinal, first_line=data[:match.start()].count(b'\n')+1,
                      last_line=data[:match.end()].count(b'\n')+1, raw_start_byte=match.start(2 if markdown else 1),
                      raw_end_byte=match.end(2 if markdown else 1),raw_sha256=sha(raw),raw_body=raw.decode(),
                      decoded_body=row['body'],decoded_sha256=row['body_sha256'],document=identity(ROOT/row['path']))
        classification=row['classification'].lower()
        if 'historical' in classification:
            result['qualification']='Explicitly retained historical/proposal/v1/older-alpha material. This does not exclude the separate current SDK After examples.'
        elif row.get('csharp_compilation_participant') is False or 'sketch' in classification:
            result['compilation_status']='qualified-nonparticipant'
            result['qualification']=row['classification']
        elif row['language'] in ('csharp','csharp/xml-code'):
            result['compilation_status']='current-context-required'
        else:
            result['compilation_status']='non-csharp-excluded'
        current_index += 1
        results.append(result)
    report=dict(entries=results,authority_records=247,current_source_records=current_index,
                overlay_updates_verified=len(overlay['updates']),
                no_execution=True,qualification='The repaired current filter/test body is separate from the invalid historical base; JSON/shell remain outside C# compilation.')
    with (HERE/'external-current-inventory.json').open('x') as output:
        json.dump(report,output,indent=2);output.write('\n')
    from collections import Counter
    print(dict(Counter(row['compilation_status'] for row in results)))


if __name__=='__main__':
    main()
