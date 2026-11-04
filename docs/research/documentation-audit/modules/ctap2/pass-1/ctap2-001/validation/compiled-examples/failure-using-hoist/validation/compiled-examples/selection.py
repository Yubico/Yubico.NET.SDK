"""Explicit compile-input selection. Whole-body hashes pin anchors/ordinals fail closed."""
G = 'src/Ctap2/CLAUDE.md'
R = 'src/Ctap2/README.md'
T = 'src/Ctap2/tests/CLAUDE.md'
W = 'src/WebAuthn/README.md'
U = 'docs/usage/user-interaction.md'
A = 'docs/architecture/raw-access-tiers.md'
D = 'docs/architecture/sdk-architecture-diagrams.md'
S = 'docs/TESTING.md'
X = 'src/Ctap2/src/IYubiKeyExtensions.cs'
B = 'src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs'
O = 'src/Ctap2/src/Extensions/Shared/ExtensionOutput.cs'
P = 'src/Ctap2/src/Pin/PinUvAuthProtocolV2.cs'

EXPECTED = {
G: '''96efff99e3bc62c4c6a6af7ed204099a5c25e033fd89ec7c5390edfa8d2124f9
8c95463d5f36e750f67caf697013bcf6e68c4e0b1c5199822bbf7fe134b1f612
0d0459ed6006f488a0d499cefd113c23ca485a4c5bf14fb4725d7d54c9275ceb
fd7927c770fbcb26910b39d13c59536ff0e76a7931436b3a49e851eed6ee37f7
1e9b6ac91a7db4e9b4bdd6cd0b01550da1ddfe583020d7e683c0f118ccbd002a
09dc6b90a941d7c46a1da52a7367cbb2e82927d55bbaaf69d1cc6d5f45192c75
cd0e6a5a6bcc8496d56709ed71c3fd1d81b82128d44b549fbc27c8c16056f313
e761731dfe7dcb344ce4f1204f44bcb0d8f518a4e9bd6f56c9dbbef48219a9c8
a7a43ca63af2554326473e84a59693ebe84ea26528e64faedc49f1c1c9f43395
7143535e4b75851208195726f39c66dd348cfab783de617af76cce5e6d15832d
cb469fdca424212e35a6bd94c05516c42ffb6b55ff336899d8191ae36d56bb4a
458b518df8075453657d32e00e8efbd9f4047d61102cb25cd0b1704ee6fcc41d
2326e874ccd27227003b043692eeb1ba1c3a40f1a4317c9ddfb291ba1ef3c8ad
c79f2a9a89763087a5e839e064c98d21d9f23439627fe0c2bcefc643ef07ecec
5695fdaccb2a37f0e472100d3dc924c55d959b27a9256f1c6c08bfd87b3b601e''',
'src/Ctap2/PACKAGE_README.md': '302b61fc4c62d89ccf955a84b54a3955878b7f6b7039aee12fba97fb96bf5266',
R: '''302b61fc4c62d89ccf955a84b54a3955878b7f6b7039aee12fba97fb96bf5266
359878ebe2147dc0cd389baf7baef3a5f7030cdffc5a46a8096adddc590c3e97
5a9814c0f85109fbde055003efb50e4030dc2fb2c7ae59659fa2fd11f89ac49f
1b6a37d5e565bd29cac32b2e953bc9dc2baaa8c1e346db1800df1168f9f58983
2abef7efcd1d5b35ddd9fcbfede3b7a668f8a0fa5f583eede7077e3a381f9a4b
d462cf6bcbaa2c7451daabf34f9ea7e2754fad138c6017474c3e6a4958e2da8b
2a724dd1e8fa17d300b515f3424ce106b85920fff8ed446bbce7ec094f2c0245
ca8fe727d322c778aabf304fd3260fb21de2c9619067f783b11892536be2b6ae
4ca34b03d5ea432f87fc8f07a89bb177d33f90207df87a5ea65abce3a570ec05
cb12107b83a19e6275bbe2e6d21a9b0e3f8142e53e74c2c8567509ca15d5450d''',
'src/Ctap2/examples/FidoTool/README.md': '''31c0cb90e780de6ab229bebd1e195a8b953e20fac0e38e0f89241ea02b165fca
38a8284884beef9555823556677d340be89b0fb27feb78ce181a84864e5f55cb
4ae470dd57750d5e78616a818e487d40a4fd2e41d1aee2507b6ef3df1f9a01b7
4027a5f0146c4bac0df7440734be598584162083e2a32a548e3f6c9df79ad3e5
34a142d596daafafdae7375ea2ffa76f6681b189f152588af4fd70272d542991
30f74ab0552f217b2884d4545c8469bbd155513f037ef5939c511031ada4c932
3fe22cfa30d64802c5c733e619428e9202b11c95358aa5e28df45f31b3f0a33b
1930b55e2bd776df42cb02b93b36e3a6b6f95bd4e1f01b1b3552044ae2c3a83d''',
T: '''273b0a0eb8319f7f63466e0cb23a68bbc15f3f725d882a5898bc52e7d4cee378
4cea636cf382a900dd1df31a3ec8d8634e024080f81dfedaab03a95615a87593
3283086484817f07114dcf9c6ea1633d72c81bc7d964c0dc1b4bc369c94c3f47
2fe922258c20ca2a0e40b526556a790bf5a493494a93f94c08a2b516b9f1591b
42a2d199864bd193a31668a0d29c7f8d3f171c95f3273fd0765d1b4f38777d40''',
X: '''65acd99cf0434fef8b4577072275dea1985ddb6eafdcd69d8abbf38b19893640
54ac683714f62a3b3a43dcf8e65590f848d79bc44fcbc341f3a1d2b7ad98efa8''',
P: '84824bd98d1495d8ed9808fb8ed68c38e2ce2f3d545ce92a5de666f53bdb7245',
B: 'd674a4fb0f9d139f83a06d03240e302da1056e1fe70f1c42442e250f4fed6442',
O: 'eb06a60b2a0beb8648095b0568fce1c9f293903f5d2ba136c8ccfe7c05fb56e2',
U: '''a59f59abcbcf3f6a92fea0b7a1fd9de3755c1c256c92ac35bb0ea67ab389f951
aadb7f3aa7711e6babbfbb6f67170eae06bec69826decf184b495644af78529a
109cd113261d954faf24c614590f0ffd85ec337e78394d0ef3a1c046f050d5a3
cdb35996f0077d9974a37ad4ecf16805b34fd470e34c9cd7208f081d168728af''',
A: '''ddab09e5e37cfdc746f6d34bec1c0749339aa01a60dbe388d43915b77b2ed002
318e1d600007d9682a330cd9ee0ab8fb02fa9031bc36b3b37edbc4c4732ccc36
c89973710405dc9355bbeb345e86ef0d6fc52a79b76b33bfbb75cb50b5f84340
b2298d166d6a76d130750ebed9ab3775d271b2679426f79fe2252c924a8ca415
d3f9d4e02bf88ccf779784dcd47f1599056155e59afb8bb6529198cd729764e1
d1ebf13f8409ed7cf58cd2a24008902092e1d5d5403af8744b51332f33877f0f
3bd0b61590590b2f50200473ccaf72d9ea010b73bf81ac18d4e1577ca04fc4f2
048e4a43422e260a9df70e5e85b7e1576cb644c625850265b5811e1ca4e311d5''',
W: '''6a146994bf8613877c22721c76e81528341e33ee758602d1b82e5c0d4ac8f10c
8524d8c9737625f9fe458628726c4325e0598c5491125be8666bcf3fab7f22ab
50093a61d45d980d5394b59d0c15e9e4e56c8d3782b5c34253e9c7c24aa7c103
3af34f1932645ed27aad64cc992be6f48217cd323fa1058857391298f5e5d553
ca8fe727d322c778aabf304fd3260fb21de2c9619067f783b11892536be2b6ae
e8b1d9ed45468f9b6054fe6e0509b43130aeaefce6d0bbf787fc25ae0fa97370
f462600ff58560c91d5d2e8adb718d1fa8aa237294614d72b54ab32b8d60eec1
f553aa17d4f73575a3faf7646bc7f36b0af6d9cc253692effd0b5b6aabfb7a18
fe6f7ba78834d3d44845e2aed2794d5e5c3e4b72ac527d27f0ba301ff2443e09''',
D: '''db57c0dc9a0e2214e8b3ece5419847058baae2b8b8ace308b0a848ed77d2167b
5f1164ccfa0d2ac0a8a4ad19d054fee9869ce8d1fe6a1141e87969d9890271bb
27a07de60a0d7db46e5fada4aa78b5371b38207bbe330b221000bb59573b3c1a
1171447ba5041fa7d7202db784c83a37edf6b202448ed27510e60988d2767c67
341bebbd45c58bd897464ef5f6addc4718bad5785742f5f791dd3b7272b65932
fac3ec6398934352fba52945ac6a9b83e2e379c2b32f3fbf746ee9941a696108
9fc90dabfe4f5ff3135c69273d3acb226ddb075ba8358ff0069003c8bf7e5ac7
202d2a705cf7f4d69f471f06915458daa1600d6b071d423790e1bcb8aa79091b''',
S: '''5f64f3cab35661ee51e6ebe9c67823d6285f2c4a1069b62d23555a4bec1a135e
cf6a897b3024e06608921c48029f1182364b42ace12a894f9a6ebf28594e43f1
35f009fe409f9ff806cffd2f687bc004ced97a204c7b9f8fbb60a1c2fddf15d2
3b3d1fd8f5c71e90c3d6eba1d62c6752cd264ab5f6b9d969160fa4ad3f269acf
2808b744fe839cd88d2c3a3ec861f793b674dbfab80a5d9a0f464c90060adf9f
05456b5110e195031b7067900d5c5bc61b93e944849bdd95d427281964c6e1f1
0f609596277542601f36e2195b39cf5d0ed2f737fae56044bce9ae201ca31cee
e8d968676464a498df79f6611a219b4276663ce7375eebda41fdfe2b4c703ba3
c4977b1c498bafc3e8584567bad382c99be28f9de05c2f51ee601cb464d21105
67651df28721363c7ccef243e8a8a61f2d0d6b8f7328df6fff786d498968611c
9dedbdc9a06cb9503c1e8b1351b48e2103cef07895cd13e6989acc8131cfcd29
da244c11bfbf6860ff45ccf1a2d655fd2b570b0bf84584788a8be4b4eaf93de7
96c24635e172d8f3462d1a6d54cf878f7e3b1290ca30088504fba92182cac771
154754fe657ed96022e8fc963e638bbc1de503e71de93bac887e8ebd8e7b7d36
d80f6ef069d1b44d37d81ae005136622624bffd4cb13d1ce48377a59bb015bd6
f32cf0969185a64db816dda52d84934178f40f761d4cee34756336c9c07d76f9
59c87d7b0ac71c4c7f2d505f646f315614efaa3a8e2eec9c51443da2e0eb9fb5
f62032c84989e08c33c56ccada71d57711ef729eace28b1d861826e5f0d24935
0d5a9b0728f5d9935f33d06ca9380bb658730e4ba1e79d21034ec898f461fb11
ef31bc176844900aac8019071f41ded6d1aa1e0d2bdb67d69133d40708166f08
4a64be614107258f37ffe49e2d16320f89bb993123d90d474b9135b7fd21803e
f405281b2f89cc33ab43343184bf541d56ea51c31cd6273aec57877c63136c24''',
}

CORE = ['System', 'System.Threading', 'System.Threading.Tasks']
DEVICE = ['Yubico.YubiKit.Core.Abstractions', 'Yubico.YubiKit.Core.Devices', 'Yubico.YubiKit.Core.Sessions']
CTAP = ['Yubico.YubiKit.Ctap2']
PIN = ['Yubico.YubiKit.Ctap2.Pin']
CRED = ['Yubico.YubiKit.Ctap2.Credentials']
EXT = ['Yubico.YubiKit.Ctap2.Extensions']
CRYPTO = ['System.Security.Cryptography']
CBOR = ['System.Formats.Cbor', 'Yubico.YubiKit.Ctap2.Cbor']
CM = ['Yubico.YubiKit.Ctap2.CredentialManagement']
IT = ['Xunit', 'Yubico.YubiKit.Tests.Shared', 'Yubico.YubiKit.Tests.Shared.Infrastructure', 'Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions']
IT_DLL = 'src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/bin/Release/net10.0/Yubico.YubiKit.Ctap2.IntegrationTests.dll'
SHARED_DLL = 'src/Tests.Shared/bin/Release/net10.0/Yubico.YubiKit.Tests.Shared.dll'
STATE_SOURCE = 'src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/TestExtensions/Ctap2TestStateExtensions.cs'


def block(path, number):
    return f'{path}#{"x" if path.endswith(".cs") else "f"}{number}'


def group(name, path, numbers, shape, parameters='', imports=(), context='consumer-lib', prerequisites=(), api=(), modules=(), references=(), class_blocks=(), implements=None):
    return dict(name=name, blocks=[block(path,n) for n in numbers], shape=shape, parameters=parameters,
                imports=list(dict.fromkeys(CORE+list(imports))), context=context, prerequisites=list(prerequisites),
                source_api_evidence=list(api), modules=list(modules), references=list(references), class_blocks=list(class_blocks), implements=implements)


def selection():
    session_api = ['src/Ctap2/src/ICtap2Session.cs','src/Ctap2/src/Ctap2Session.cs',X]
    protocol_api = ['src/Ctap2/src/Pin/ClientPin.cs','src/Ctap2/src/Pin/IPinUvAuthProtocol.cs',P]
    credential_api = ['src/Ctap2/src/AuthenticatorInfo.cs','src/Ctap2/src/Credentials/PublicKeyCredentialTypes.cs',
                      'src/Ctap2/src/Credentials/MakeCredentialOptions.cs','src/Ctap2/src/Credentials/GetAssertionOptions.cs',
                      'src/Ctap2/src/Credentials/AttestedCredentialData.cs']
    admin_api = ['src/Ctap2/src/CredentialManagement/CredentialManagement.cs','src/Ctap2/src/CredentialManagement/StoredCredentialInfo.cs']
    groups = [
        group('guide-session',G,[2],'async-method','IYubiKey yubiKey',DEVICE+CTAP,
              prerequisites=['Available caller-selected IYubiKey; session owns opened connection; no other live session.'],api=session_api),
        group('guide-info',G,[3],'async-method','ICtap2Session session',CTAP+['System.Linq'],
              prerequisites=['Live initialized session; advertisement/state only.'],api=session_api+['src/Ctap2/src/AuthenticatorInfo.cs']),
        group('guide-pin-create',G,[4],'async-method','ICtap2Session session, byte[] pin, string rpId, byte[] clientDataHash, MakeCredentialOptions options, PublicKeyCredentialRpEntity rp, PublicKeyCredentialUserEntity user, CancellationToken cancellationToken',CTAP+PIN+CRED+CRYPTO,
              prerequisites=['Configured PIN, supported V2, exact client-data hash32, matching RP scope, options.UserVerification null; caller clears borrowed PIN after final task. Whole creation await and both finally clears retained.'],api=session_api+protocol_api+credential_api),
        group('guide-extensions',G,[5],'sync-method','ReadOnlyMemory<byte> blobData, IPinUvAuthProtocol protocol, byte[] sharedSecret, IReadOnlyDictionary<int, object?> keyAgreement, byte[] salt1, byte[] salt2',PIN+CRED+EXT+['System.Collections.Generic'],
              prerequisites=['Nonsensitive blob within capacity; stable shared-secret/public agreement and salt32 arrays through encoding; caller owns clearing. No fake crypto helper/enforcement.'],api=[B]+protocol_api),
        group('guide-admin',G,[6],'async-method','Ctap2Session session, IPinUvAuthProtocol protocol, byte[] pinToken, ReadOnlyMemory<byte> rpIdHash, PublicKeyCredentialDescriptor credentialId',CTAP+PIN+CRED+CM,
              prerequisites=['Public constructor requires concrete Ctap2Session; internal ICtap2Session overload excluded. Live borrowed protocol/token, unbound cm token for metadata/RPs, returned hash32; explicit destructive authorization for delete. Helper/result disposal retained.'],api=admin_api),
        group('guide-raw',G,[7],'async-method','ICtap2Session session, IPinUvAuthProtocol protocol',CTAP+PIN+CBOR,
              prerequisites=['Initialized session/protocol; raw prefix/schema/status/size/sequence obligations are caller-owned. Whole build/send retained; guide lacks encoded-request finally cleanup.'],api=session_api+['src/Ctap2/src/Cbor/CtapRequestBuilder.cs']),
        group('guide-memory',G,[8],'sync-method','byte[] pinBytes',CRYPTO,
              prerequisites=['Writable caller PIN; synchronous stackalloc, real SHA256 computation, both finally clears retained.']),
        group('guide-protocol',G,[9],'sync-method',imports=PIN,
              prerequisites=['Actual V2 using/disposal example, not a fake helper or caller-output zeroing guarantee.'],api=[P]),
        group('guide-threshold',G,[15],'sync-method','FirmwareVersion firmwareVersion',DEVICE+CTAP,
              prerequisites=['Actual firmware metadata; complete negative branch/throw retained; sentinel passes, not feature qualification.'],api=['src/Core/src/Devices/Feature.cs','src/Core/src/Devices/FirmwareVersion.cs','src/Ctap2/src/Ctap2Session.cs']),
        group('unit-versions',G,[10],'test-method',imports=CTAP+CBOR+['Xunit'],context='unit-v3',
              prerequisites=['Real xUnit v3 context; partial versions-only fixture and actual Assert.Contains retained.'],api=['src/Ctap2/src/AuthenticatorInfo.cs']),
        group('unit-retries',G,[11],'test-method',imports=CTAP+PIN+CBOR+['Xunit','NSubstitute'],context='unit-v3',
              prerequisites=['Real v3/NSubstitute, actual awaited ClientPin call with TestContext runner token, capture and all wire/retry assertions. No xUnit2/harness DLL.'],api=protocol_api+session_api),
        group('hardware-info',G,[12],'test-method',imports=DEVICE+CTAP+IT,context='hardware-v2',modules=['Management'],references=[SHARED_DLL,IT_DLL],
              prerequisites=['Real v2/SkippableFact/Tests.Shared; HID-filtered authorized state, direct HID-preferred session, no PIN normalization; actual AAGUID assertion. Compile only.'],api=session_api+[STATE_SOURCE]),
        group('readme-paired',R,[2,3,4,7],'top-level',context='consumer-demo',
              prerequisites=['One session: nonresident creation and targeted assertion, then resident PIN creation and paired assertion. Dedicated key, known PIN123456, advertised V2, authorized UP/PIN/state; all token/auth/PIN cleanup. Prior rp/user/credentialId are actually created by selected earlier blocks, not undefined scaffolds. No RP verification/deletion.'],api=session_api+protocol_api+credential_api+[B]),
        group('readme-discoverable',R,[5],'async-method','ICtap2Session session, byte[] authenticationHash',CTAP+CRED+['System.Collections.Generic'],
              prerequisites=['Alternative to allow-list task: fresh hash32, existing discoverable example.com credential; preceding nonresident creation is ineligible. Whole first/count/N-1 consecutive awaited loop, no intervening command.'],api=session_api+['src/Ctap2/src/Credentials/GetAssertionResponse.cs']),
        group('readme-admin',R,[6],'async-method','Yubico.YubiKit.Ctap2.CredentialManagement.CredentialManagement credentialManagement, ReadOnlyMemory<byte> rpIdHash',CM,
              prerequisites=['Existing real authorized helper/session/protocol/token as typed input, returned RP hash; whole foreach inside owned-result disposal scope. No fake helper.'],api=admin_api),
        group('readme-prompt-factory',R,[9],'async-method','IYubiKey device',DEVICE+CTAP+['Yubico.YubiKit.Core.Credentials'],class_blocks=[block(R,8)],
              prerequisites=['Alternative owning factory on available physical key, not appended to original factory. Whole documented TouchPrompt class accompanies factory; callback is indication, not touch proof.'],api=session_api+['src/Core/src/Credentials/IUserPresencePrompt.cs']),
        group('hardware-create',T,[1],'test-method',imports=DEVICE+CTAP+PIN+CRED+CRYPTO+IT,context='hardware-v2',modules=['Management'],references=[SHARED_DLL,IT_DLL],
              prerequisites=['Real public Ctap2TestStateExtensions from actual integration DLL, not fabricated Ctap2IntegrationTestExtensions. Default known-PIN normalization may mutate state before callback. HID/V2/authorized UP/PIN/persistent creation; whole callback/create/assertions/finally retained.'],api=session_api+protocol_api+credential_api+[STATE_SOURCE]),
        group('hardware-set-pin',T,[2],'async-method','ICtap2Session session',CTAP+PIN+CRYPTO+['System.Text']+IT,context='hardware-v2',modules=['Management'],references=[SHARED_DLL,IT_DLL],
              prerequisites=['Real known-PIN constant from public integration helper; approved device with no PIN set and supported V2. Whole SetPin await and PIN finally. Compile only.'],api=protocol_api+[STATE_SOURCE]),
        group('unit-cbor-fixture',T,[4],'sync-method',imports=CBOR,context='unit-v3',
              prerequisites=['Real CBOR context; whole useful fixture encoding, not a conforming full GetInfo or fake no-op test. No assertion invented.']),
        group('unit-extension',T,[5],'test-method',imports=CBOR+EXT+['Xunit'],context='unit-v3',
              prerequisites=['Real v3; whole builder/null guard and every CBOR key/value assertion retained, not device enforcement.'],api=[B]),
        group('xml-info',X,[1],'async-method','IYubiKey yubiKey',DEVICE+CTAP+['System.Linq'],
              prerequisites=['One-shot temporary owning session only without other live connection. Both output calls retained.'],api=session_api),
        group('xml-session-alternatives',X,[2],'async-method','IYubiKey yubiKey',DEVICE+CTAP+['Yubico.YubiKit.Core.Protocols.SmartCard.Scp'],
              prerequisites=['Ordinary lexical block ends before SCP alternative opens; scopes kept intact. Authorized provisioned SmartCard/default keys are not production guidance; caller-owned scpKeys encloses secure-session disposal.'],api=session_api+['src/Core/src/Protocols/SmartCard/Scp/Scp03KeyParameters.cs']),
        group('xml-builder',B,[1],'sync-method','ReadOnlyMemory<byte> myBlobData',EXT,
              prerequisites=['Stable nonsensitive blob within device maxCredBlobLength; sensitive content needs independent prevention/enforcement. Whole builder fragment.'],api=[B]),
        group('xml-output',O,[1],'sync-method','ReadOnlyMemory<byte> extensionCbor',EXT,
              prerequisites=['Nonempty definite-length authData map; whole decode/conditional/output branch, not byte-exact raw preservation or trust verification.'],api=[O]),
        group('shared-operation-wording',U,[2],'sync-method','UserPresenceContext context',['Yubico.YubiKit.Core.Credentials'],
              prerequisites=['Real context; whole switch with CTAP/PIV/OATH/default branches retained.'],api=['src/Core/src/Credentials/UserPresenceContext.cs','src/Core/src/Credentials/UserPresenceOperations.cs']),
        group('shared-intent-callback',U,[4],'class-members',imports=['Yubico.YubiKit.Core.Credentials'],implements='IUserPresencePrompt',
              prerequisites=['Wrapper class implements real IUserPresencePrompt; documented callback/branches/output/return intact, no fake harness.'],api=['src/Core/src/Credentials/IUserPresencePrompt.cs','src/Core/src/Credentials/UserPresenceContext.cs']),
        group('shared-intent-task',U,[3],'async-method','WebAuthnClient client, PublicKeyCredentialRequestOptions requestOptions, CancellationToken cancellationToken',['Yubico.YubiKit.Core.Credentials','Yubico.YubiKit.WebAuthn.Client','Yubico.YubiKit.WebAuthn.Client.Authentication'],context='consumer-wa-lib',modules=['WebAuthn'],
              prerequisites=['Existing real WebAuthnClient/request options, configured underlying prompt; whole intent using scope/await/named cancellation; no upload helper.'],api=['src/WebAuthn/src/Client/WebAuthnClient.cs','src/Core/src/Credentials/UserPresenceIntent.cs']),
        group('shared-raw-hid',A,[4],'async-method','IYubiKey yubiKey, byte ctapHidCommand, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken',DEVICE,
              prerequisites=['Real raw factory/send/async disposal; caller selects CTAPHID command/payload/schema and response handling, not complete WebAuthn ceremony.'],api=['src/Core/src/Sessions/RawCtapHidSession.cs','src/Core/src/Devices/YubiKeyConnectionExtensions.cs']),
        group('shared-hid-management',S,[15],'test-method',imports=DEVICE+['Yubico.YubiKit.Management']+IT,context='hardware-v2',modules=['Management'],references=[SHARED_DLL,IT_DLL],
              prerequisites=['Real Tests.Shared WithManagementAsync; HID-filtered state/preferred connection and actual assertions. Whole shared implicit FIDO task; no Management historical edit. Compile only.'],api=['src/Tests.Shared/YubiKeyTestStateExtensions.cs']),
        group('webauthn-paired',W,[2],'top-level',context='consumer-wa-demo',modules=['WebAuthn'],
              prerequisites=['Whole existing WA demo: both ceremonies, cached registration metadata, zero-match branch, SelectAsync and PIN finally. Dedicated PIN11234567 key/V2; RP response verification absent.'],api=['src/WebAuthn/src/Client/WebAuthnClient.cs','src/WebAuthn/src/IYubiKeyExtensions.cs']),
        group('webauthn-pin-config',W,[4],'sync-method','WebAuthnClientOptions clientOptions, Func<CredentialPromptContext, CancellationToken, Task<byte[]>> ReadPinAsync',['System.Buffers','System.Security.Cryptography','Yubico.YubiKit.Core.Credentials','Yubico.YubiKit.Core.Utilities','Yubico.YubiKit.WebAuthn.Client'],context='consumer-wa-lib',modules=['WebAuthn'],class_blocks=[block(W,3)],
              prerequisites=['Application input delegate explicitly typed, not fake ReadPinAsync implementation; configuration before original factory, whole prompt transfer/finally retained.'],api=['src/WebAuthn/src/Client/WebAuthnClientOptions.cs','src/Core/src/Credentials/ICredentialPrompt.cs']),
        group('webauthn-prompt-factory',W,[6],'async-method','IYubiKey device, WebAuthnOrigin origin, WebAuthnClientOptions clientOptions',DEVICE+['Yubico.YubiKit.Core.Credentials','Yubico.YubiKit.WebAuthn','Yubico.YubiKit.WebAuthn.Client'],context='consumer-wa-lib',modules=['WebAuthn'],class_blocks=[block(W,5)],
              prerequisites=['Separate alternative to original owning factory, not second live client; documented TouchPrompt class intact.'],api=['src/WebAuthn/src/IYubiKeyExtensions.cs','src/Core/src/Credentials/IUserPresencePrompt.cs']),
        group('webauthn-pair-policy',W,[7],'sync-method',imports=['System.Collections.Generic','Yubico.YubiKit.WebAuthn.Client'],context='consumer-wa-lib',modules=['WebAuthn'],
              prerequisites=['Whole trusted exact-pair HashSet/callback configuration; not fake browser/RP validator implementation.'],api=['src/WebAuthn/src/Client/WebAuthnClientOptions.cs']),
    ]
    selected = {key for item in groups for key in item['blocks']+item['class_blocks']}
    exclusions = {
        block(G,1): 'Directory-tree architectural sketch, not C#; no replacement stub.',
        block(G,14): 'Schematic source-field declarations summarize actual Feature fields; not a consumer operation or class to add to SDK.',
        block(P,1): 'Protocol2 HMAC/AES 64-byte binary layout notation, not C# assignments; representation diagram explicitly excluded, no no-op stub.',
        block(A,7): 'Outside CTAP selection: undefined LoadScpParametersFromSecureStorage scaffold; no fake security helper.',
        block(A,8): 'Historical before/after internal ProtocolFactory/ISmartCardProtocol sketch; not current public consumer API; no internal/friend identity impersonation.',
        block(W,8): 'Undefined application rpValidationService scaffold, no actual SDK service type; exclude rather than fabricate helper.',
        block(W,9): 'Historical before/after migration sketch, not a current complete program; ownership prose is not compile proof.',
        block(U,1): 'Unchanged PIV/CLI.Shared setup outside CTAP task; separate modules not silently added.',
    }
    documents = []
    for path, hashes in EXPECTED.items():
        xml = path.endswith('.cs')
        items = []
        for n, sha in enumerate(hashes.split(),1):
            key = block(path,n)
            reason = 'Whole useful task/cleanup/assertions or typed fragment; see exact group prerequisites and edit-A–F provenance.' if key in selected else exclusions.get(key)
            if reason is None:
                reason = ('Mermaid architectural/internal sequence sketch, not executable public API; L2/L3b regeneration coordinator-owned.' if path == D and n < 8 else
                          'CLI/shell/config/runner command or unchanged non-CTAP shared illustration; retained verbatim, not C# compilation or executed-command evidence.')
            items.append(dict(sha256=sha,classification='compile' if key in selected else 'excluded',reason=reason))
        documents.append(dict(path=path,kind='xml' if xml else 'markdown',origin='primary' if xml or path in [U,A] else 'guide',
                              scope='module' if path.startswith('src/Ctap2/') else 'shared-current-dependency',blocks=items))
    paths = ['CLAUDE.md',S,U,A,D,'docs/migration/v1-to-v2-map.yml',W,'src/WebAuthn/CLAUDE.md',
             'src/WebAuthn/src/Yubico.YubiKit.WebAuthn.csproj','Directory.Build.props','Directory.Build.targets','Directory.Packages.props','.editorconfig','global.json',
             'src/Ctap2/src/Yubico.YubiKit.Ctap2.csproj','src/Ctap2/examples/FidoTool/FidoTool.csproj',
             'src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Yubico.YubiKit.Ctap2.UnitTests.csproj',
             'src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Yubico.YubiKit.Ctap2.IntegrationTests.csproj',
             'src/Tests.Shared/Yubico.YubiKit.Tests.Shared.csproj']
    references = [dict(path=p,origin='guide' if p in [W,D,S,'docs/migration/v1-to-v2-map.yml'] else 'primary',
                       disposition='Current dependency hash inventory, not a whole-Core/whole-WebAuthn audit or prior gate reuse.') for p in paths]
    references += [dict(path='docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001/'+p,origin='primary',
                        disposition='Current run provenance/prerequisite classification, not inherited validation results.')
                   for p in ['edit-a.md','edit-b.md','edit-c.md','edit-d.md','edit-e.md','edit-f.md','correction-f-review.md',
                             'validation-plan.md','validation-tooling.md','evidence/f/snippets.md','evidence/f/external-paths.md','evidence/f/references.md']]
    return dict(documents=documents,groups=groups,reference_inventory=references)
