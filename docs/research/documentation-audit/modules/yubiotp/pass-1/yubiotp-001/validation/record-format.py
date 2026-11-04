"""Account only the named ordinary imports, import order and final-newline formatter changes."""
from pathlib import Path
import hashlib
import json
import re
import subprocess

HERE=Path(__file__).resolve().parent
ROOT=next(p for p in HERE.parents if (p/'toolchain.cs').exists())
BASELINE='95e5ccaeda33954c21f2a38058d4f66b5f208787'
capture=HERE/'pre-format-content.json'
before=json.loads(capture.read_text())
assert before['baseline']==BASELINE
assert (HERE/'format-apply.log').read_text().endswith('EXIT=0\n')
expected={'src/YubiOtp/src/IYubiKeyExtensions.cs':[
    'Yubico.YubiKit.Core.Protocols.SmartCard.Apdu','Yubico.YubiKit.Core.Protocols.SmartCard.Scp']}
pattern=r'^using ([A-Za-z0-9_.]+);\n'
def sha(data):return hashlib.sha256(data).hexdigest()
def relative(p):return str(p.relative_to(ROOT))
records=[]
for row in before['files']:
    old=row['content']; current=(ROOT/row['path']).read_bytes().decode()
    assert sha(old.encode())==row['sha256']
    a=re.findall(pattern,old,re.M); b=re.findall(pattern,current,re.M)
    removed=sorted(set(a)-set(b)); added=sorted(set(b)-set(a))
    assert removed==expected.get(row['path'],[]) and not added,row['path']
    comparison_old=old
    import_blank_removed=row['path']=='src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/YubiOtpSessionIntegrationTests.cs'
    if import_blank_removed:
        # The observed formatter moved this import into the group and removed its preceding blank.
        assert comparison_old.count('\n\nusing Yubico.YubiKit.Core.Sessions;\n')==1
        comparison_old=comparison_old.replace('\n\nusing Yubico.YubiKit.Core.Sessions;\n','\nusing Yubico.YubiKit.Core.Sessions;\n')
    assert re.sub(pattern,'',comparison_old,flags=re.M).rstrip('\n')==re.sub(pattern,'',current,flags=re.M).rstrip('\n'),row['path']
    records.append(dict(path=row['path'],before_sha256=row['sha256'],after_sha256=sha(current.encode()),
                        removed_namespaces=removed,added_namespaces=added,non_using_non_whitespace_identical=True,
                        using_order_changed=[n for n in a if n not in removed]!=b,import_group_blank_removed=import_blank_removed))
transform=HERE/'format-transform.json'
assert not transform.exists(),'Refuse to overwrite evidence'
transform.write_text(json.dumps(dict(baseline=BASELINE,formatter_exit=0,files=records),indent=2)+'\n')
manifest=dict(schema_version=1,baseline=BASELINE,formatter=dict(diagnostic='IDE0005',
    command_log=relative(HERE/'format-apply.log'),command_log_sha256=sha((HERE/'format-apply.log').read_bytes()),
    log=relative(HERE/'format-initial.log'),log_sha256=sha((HERE/'format-initial.log').read_bytes())),files=[])
for r in records:
    if not r['removed_namespaces']:continue
    original=subprocess.check_output(['git','show',BASELINE+':'+r['path']],cwd=ROOT)
    manifest['files'].append(dict(path=r['path'],baseline_sha256=sha(original),
        before=dict(path=relative(capture),bundle_sha256=sha(capture.read_bytes()),sha256=r['before_sha256']),
        after=dict(path=relative(transform),bundle_sha256=sha(transform.read_bytes()),sha256=r['after_sha256']),
        removals=[dict(namespace=n) for n in r['removed_namespaces']]))
(HERE/'import-removals.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(f'PASS:{len(records)} exact source transforms;2named ordinary import removals/1file; other text preserved except import order and final newlines. Roslyn proof is separate.')
