"""One focused follow-up; preserve unsuccessful first-round URLs in source-manifest.json."""
import datetime, hashlib, json, pathlib, re, subprocess, urllib.request
ROOT=pathlib.Path(__file__).parent
urls={
 'vendor-technical-manual.html':'https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-piv.html',
 'vendor-reset-command.html':'https://docs.yubico.com/yesdk/yubikey-api/Yubico.YubiKey.Piv.Commands.ResetPivCommand.html',
 'vendor-attestation-tool.html':'https://docs.yubico.com/software/yubikey/tools/pivtool/piv-tool-attestation.html',
 'nist-sp800-73-4-status.html':'https://csrc.nist.gov/pubs/sp/800/73/4/final',
 'nist-sp800-78-4-status.html':'https://csrc.nist.gov/pubs/sp/800/78/4/final',
 'dotnet-rsa-verifydata.html':'https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsa.verifydata?view=net-10.0',
}
records=[]
for name,url in (urls.items() if not (ROOT/'followup-manifest.json').exists() else []):
 r={'file':name,'url':url,'accessed_utc':datetime.datetime.now(datetime.timezone.utc).isoformat()}
 try:
  b=urllib.request.urlopen(url,timeout=40).read(); (ROOT/name).write_bytes(b)
  r.update(status='captured',sha256=hashlib.sha256(b).hexdigest(),bytes=len(b))
 except Exception as ex:r.update(status='gap',error=str(ex))
 records.append(r)
if records:
 (ROOT/'followup-manifest.json').write_text(json.dumps(records,indent=2)+'\n')
from html.parser import HTMLParser
class Text(HTMLParser):
 def __init__(self):super().__init__();self.parts=[];self.hidden=0
 def handle_starttag(self,tag,attrs):
  if tag in ('script','style'):self.hidden+=1
 def handle_endtag(self,tag):
  if tag in ('script','style'):self.hidden=max(0,self.hidden-1)
 def handle_data(self,data):
  if not self.hidden and data.strip():self.parts.append(data.strip())
for p in ROOT.glob('*.html'):
 parser=Text();parser.feed(p.read_text())
 (ROOT/(p.stem+'.txt')).write_text('\n'.join(parser.parts))
repo='/tmp/opencode/openpgp-001-ykman'
pin='4ca60f706af930459138d8dc0f0f953480e1c7a4'
def git(*args):return subprocess.check_output(['git',*args],cwd=repo,text=True)
(ROOT/'python-history.txt').write_text(git('log',pin,'--format=%H %ad %s','--date=short','--','yubikit/piv.py','ykman/piv.py','tests/device/test_piv.py'))
commits=['0717f648f7ebe8184af73db3dfa704e5a7b3a04d','b8756f584446e48b3cd64d4e15288e45bcc5588e','72de4264018bfd5a1251e1571f01f5383062e2ef','5a9d4960508d566145f4796a46808da302d53ba5','9909740c44c5a16a74de3676b8b7146cac32d89b','d3ba7ebee9170e73fd52194196335644c7b6e220','55470cbb78951bd72eb1603a9a6d4511798cd34f','70e11d77942acd5c5f7c627e398cf5119c64fc55','377823882ec4c8dd8903159a254f3461303de694','fe91d0ecc8d568a3ccbce3d276f1871af25d0aa0','d4982afa43a3be3b21874bfcb2733afcf3f6375f']
for c in commits:
 try:
  subprocess.run(['git','merge-base','--is-ancestor',c,pin],cwd=repo,check=True,capture_output=True)
  (ROOT/('python-history-'+c[:12]+'.patch')).write_text(git('show','--format=fuller',c,'--','yubikit/piv.py','ykman/piv.py','tests/device/test_piv.py'))
 except Exception as ex:print(c,ex)
for c in ['74942e1dafc45b786c5151f293b972d1a85c4902','1eb242e3ab86ef73a5e1a1c44c442188c11979ab','8a7d152dd4a576e89c71b5fa1113197c04a11df7','58d9f2b9342888d16d1baa1c8e269abea30f33b1']:
 (ROOT/('merged-'+c[:12]+'.patch')).write_bytes(subprocess.check_output(['git','show','--format=fuller',c,'--','src/Piv']))
print(json.dumps(records,indent=2))
