"""Attach semantic setup/body/cleanup dispositions to every indexed integration method."""
import collections,json,pathlib,re
ROOT=pathlib.Path(__file__).parent
xs=json.loads((ROOT/'hardware-methods.json').read_text())
mutation={'ResetAsync','ChangePinAsync','ChangePukAsync','UnblockPinAsync','SetPinAttemptsAsync','SetManagementKeyAsync','SetPinOnlyModeAsync','GenerateKeyAsync','ImportKeyAsync','MoveKeyAsync','DeleteKeyAsync','StoreCertificateAsync','DeleteCertificateAsync','PutObjectAsync'}
helper={
 'ResetAndAuthenticate':'RESET applet then authenticate firmware-selected factory key.',
 'EncryptThenDecryptRoundTrip':'RESET/authenticate; generate RSA2048 in9D; verify default PIN; host encrypt; device decrypt/unpad; assert exact plaintext.',
 'ArmPinGatedSigningAsync':'RESET/authenticate; generate P2569A PinOnce; verify default PIN.',
 'BlockPinAsync':'Submit000000 three times, accepting InvalidPinException; reset setup establishes3 initial retries.',
 'AssertCrossKeyLivenessAsync':'Generate RSA4096 Retired1 on busy key; delay500ms; sign9A on other key under4s WaitAsync; drain keygen in finally; run both directions.',
 'ResetBothPivApplicationsAsync':'Attempt RESET on both existing sessions independently, collect both cleanup failures.',
 'GetSmartCardStatesOrSkip':'Select all AuthorizedDevices SmartCard states; skip unless >=2. Each chosen key may be reset; inspect each baseline before authorization.',
 'GetRsa4096SmartCardStatesOrSkip':'Select allowlisted SmartCard states firmware>=5.7; skip unless >=2.',
 'PollUntilRemovedAsync':'Poll serial for150s; each read has cooperative10s token; any exception counts as removalFailure. Native bound and physical removal require independent observation.',
 'AssertOpenSslVerifiesAsync':'Write SPKI/message/signature to temp files; openssl pkeyutl -verify -rawin; assert exit0; delete files in finally. No timeout on process.',
}
weak={
 'CalculateSecretAsync_X25519_ProducesSharedSecret':'Generates X25519 and checks returned public-point length; sends zero agreement commands.',
 'ImportKeyAsync_Ed25519_CanSignAndVerify':'Checks nonempty signature and metadata type/imported origin; performs zero independent signature verifications.',
 'ImportKeyAsync_Rsa3072_MetadataMatchesImportedKey':'Checks only metadata type; signing/key binding remains untested.',
 'ImportKeyAsync_X25519_MetadataMatchesImportedKey':'Checks only metadata type; agreement/key binding remains untested.',
 'GenerateKey_PinPolicyAlways_RequiresPinForEachSign':'Verifies one PIN and one signature; second-operation rejection remains untested.',
 'CompleteWorkflow_ECDHKeyAgreement':'Checks raw32-byte device result and nonempty host SHA256-derived result separately; compares zero shared secrets.',
 'CompleteWorkflow_MoveKeyBetweenSlots':'No source certificate existed; null certificate says little about source key removal. Only nonempty destination signature, without original-key verification.',
 'CompleteWorkflow_AttestGeneratedKey':'Checks certificate issuer/validity/serial; validates zero attestation chains or slot-SPKI bindings.',
 'AttestKeyAsync_GeneratedKey_ReturnsCertificate':'Checks issuer nonempty; validates zero chain/slot-key bindings.',
 'ConcurrentPivSessions_OnTwoKeys_OperateIndependently':'Counts nonempty signatures only; cross-key cryptographic binding remains untested.',
 'Rsa4096Keygen_OnOneKey_DoesNotDelayPivOperationsOnAnotherKey':'Times nonempty other-key signature; cryptographic key binding remains untested. WaitAsync timeout may leave sign task unobserved, even though keygen is drained.',
 'ResetAsync_ClearsAllSlots':'Provisions and checks9A only; F9 is retained by vendor reset.',
 'DecryptAsync_EccSlot_ThrowsArgumentException':'Metadata read precedes rejection; pre5.3 firmware instead fails metadata gate. Claim is before crypto APDU, not before any APDU.',
 'DecryptAsync_WrongCiphertextLength_ThrowsArgumentException':'Metadata read precedes length rejection; pre5.3 firmware fails metadata gate first.',
 'GetBioMetadataAsync_NonBioDevice_ThrowsOrReturnsError':'No non-Bio filter; a Bio success would fail the negative assertion. Serial103 unsupported response is a negative observation only.',
}
rows=[]
for x in xs:
 body=x['source_body'];calls=x['direct_session_calls'];attrs=' '.join(x['attributes'])
 expansions=[f'{name}: {detail}' for name,detail in helper.items() if name+'(' in body]
 extra_mutation=any(name in body for name in ('ResetAndAuthenticate(', 'EncryptThenDecryptRoundTrip(', 'ArmPinGatedSigningAsync(', 'AssertCrossKeyLivenessAsync(', 'ResetBothPivApplicationsAsync('))
 before,sep,after=body.partition('finally')
 x['reviewed_effects']={
  'setup':'RESET at body entry' if 'ResetAsync(' in before else ('Helper-expanded setup; see helper_expansions' if extra_mutation else 'Opens session/connection; no initial reset in this method'),
  'persistent_commands_in_full_body':list(dict.fromkeys(c for c in calls if c in mutation)),
  'helper_expansions':expansions,
  'credential_effects':'VERIFY failures can consume retry counts; RESET deliberately blocks PIN/PUK; successful PIN verification changes card security state. Inspect full command order in source_body.',
  'cleanup':'finally exists: '+(' '.join(re.findall(r'.{0,40}(?:ResetAsync|DeleteKeyAsync|PutObjectAsync|DisposeAsync|ZeroMemory).{0,100}',after))) if sep else 'Scopes dispose sessions/connections; persistent mutations remain until coordinator restoration unless a helper performs cleanup.',
  'physical_interaction':'human touch/hotplug coordination' if 'RequiresUserPresence' in attrs else 'routine dedicated-key configuration/authentication; no timed physical action in body',
  'multi_key': 'MultiKey' in x['path'],
  'bio_profile':'No positive Bio ceremony; non-Bio negative probe only' if 'Bio' in x['method'] else 'ordinary PIV fixture; Bio qualification separate',
  'attestation_material':'Existing F9 read/used only; zero methods request F9 private-key replacement/deletion or irreversible policy.',
  'oracle_limit':weak.get(x['method'],'Assertions are retained verbatim in this record; use actual equality/signature-verification/status checks rather than the method name as the oracle.'),
  'review_status':'full setup/body/helper/cleanup semantic read; execution pending coordinator',
 }
 x['persistent_mutation']=any(c in mutation for c in calls) or extra_mutation
 x['selected_by_smoke_traits']='Slow' not in attrs and 'RequiresUserPresence' not in attrs
 x['selected_by_unattended_traits']='RequiresUserPresence' not in attrs
 rows.append('| '+pathlib.Path(x['path']).stem+'.'+x['method']+' | '+('reset/write' if x['persistent_mutation'] else 'read/session state')+' | '+('Slow ' if 'Slow' in attrs else '')+('Presence' if 'RequiresUserPresence' in attrs else 'routine')+' | '+('finally' if sep else 'scope only')+' |')
(ROOT/'hardware-methods.json').write_text(json.dumps(xs,indent=2)+'\n')
(ROOT/'hardware-method-matrix.md').write_text('# Per-method source effect index\n\nAll rows await coordinator execution. Full attributes, source bodies, assertions, early returns, helper expansions and reviewed effects are in `hardware-methods.json`. `finally` means local cleanup exists, not complete restoration. RESET retains existing F9.\n\n| Method | Persistent effect | Traits/lane | Local cleanup |\n|---|---|---|---|\n'+'\n'.join(rows)+'\n')
print({'methods':len(xs),'smoke_trait_selected':sum(x['selected_by_smoke_traits'] for x in xs),'unattended_trait_selected':sum(x['selected_by_unattended_traits'] for x in xs),'multi_key':sum(x['reviewed_effects']['multi_key'] for x in xs)})
