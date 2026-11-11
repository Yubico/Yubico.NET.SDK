# Inherited source and authority register

Source baseline: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.

This is a bounded source rebaseline, not a complete Core semantic audit. Registry current-path hashes and history are taken from the pinned Git object, independently of metadata-only HEAD advancement.

## Authority order

Active external module handoff → latest final review/correction and final boundary record → authoritative triage/findings → original investigations and retained raw evidence. Original phase-pending language stays historical. Source existence, a rename, a PR title and a green suite each answer different questions.

The nine handoffs and their stable ledgers were read. Final evidence reviews were read for Management, CTAP2, Security Domain, YubiOTP, YubiHSM, OpenPGP and PIV; WebAuthn uses its correction chain plus CTAP's final independent boundary acceptance. OATH uses the final wave reset/provenance/documentation-impact records. Their source excerpts are incorporated by exact registry paths, without rewriting the archives.

## Historical source authority

GlobalPlatform final SCP11 source remained unavailable in the Security Domain cycle. The accessible source is Card Specification v2.3 Amendment F, Secure Channel Protocol 11, Version 1.3.0.13, Public Review, August 2023, GPC_SPE_093. It targets later final-1.4 work and is distinct from final 1.3 (November 2021) and final 1.4 (March 2024). The reviewed chain-serial passage is draft section 7.5.1, superseding the investigator's 7.5.4 pointer; SCP11c command restrictions are draft section 4.6. Final Card 2.3.1 section 11/11.11, Amendment F 1.3 sections 4.3/7.9 and Amendment D exact final KDF clauses remain unavailable/unverified in that bounded research. Preserve vendor actor/profile statements separately. Family reviewers reverify original passages before accepting normative conclusions.

OATH's Python revision 4ca60f706af930459138d8dc0f0f953480e1c7a4 corroborates policy/reset flow, not earliest physical support or fresh native proof. OpenPGP Card source at URL 3.4 is the retained 3.4.1 (2020-03-18). CTAP's seed-marker erratum and WebAuthn native-result actor decision remain authoritative. The source family owners receive the retained exact source/review paths rather than a new web-research corpus.

Historical original and alternate implementation hashes are resolved individually in registry.json. The alternate reader-cleanup 8b7427112, field-length 6bd7c3386 and strict-exclusive 26b77ece8 objects are not ancestors of this baseline; their corresponding original implementation hashes 013f3587, 331d3dc1 and eef399698 are ancestral. Current source/assertions, not those nonancestral variant titles, support the scoped rebaseline.

The old code-audit closeout/traceability/execution records are read-only context. Three are original untracked user files; their presence here authorizes no edit, staging or publication. Missing old tmpfs logs remain missing evidence rather than reconstructed successes.

## Execution qualification

No SDK compilation, unit/integration test, formatter, device operation, web retrieval, branch/worktree mutation, commit, push or subagent occurred in this preparation. The existing integration reports 4,442 unit and 197 resilience executions (overlapping populations), Linux JIT/AOT native checks and zero physical modes. That evidence does not close the old two-key 9681620 native-ownership failure, serial125 SCP initialization, or the user's PIV physical deferral. Windows CCID/HID concurrency handoff is pending user readiness; no unfinished checkout was inspected.

## Read-only tooling observations

PyYAML `6.0.3` safe_load of current `docs/migration/v1-to-v2-map.yml` failed with ScannerError at line 274 column 488. The unchanged unquoted `: ` in the PIV constructor mapping is the observed scope; no map edit was made.

Source-equivalent case-sensitive discovery plus case-insensitive filename `Core` selector yields these tracked test-host candidates (untracked generated hosts can add more):

- `src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Yubico.YubiKit.Core.IntegrationTests.csproj`
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Yubico.YubiKit.Core.UnitTests.csproj`

Use `Yubico.YubiKit.Core.UnitTests` / `Yubico.YubiKit.Core.IntegrationTests` only after checking actual filename uniqueness, rather than a partial Core filter. New hosts should avoid embedding those names and all archived host selectors.

## Current source anchors

These focused anchors support initial mapping. Every full semantic unit and regression assertion still receives its assigned family/boundary read. A listed test path means source evidence to inspect; it is never a fresh test pass.

- `docs/migration/v1-to-v2-map.yml` — blob `5539ffd7c357aaea0bcc73a80d5a28341b71294d`; latest pinned-path commit `fe342fb0b chore(audit): integrate current Core upstream into documentation campaign`.
- `scripts/toolchain/Discovery.cs` — blob `2f51c2ced74f1d8e27bb2d03234129b845acf682`; latest pinned-path commit `9f75f2938 feat(toolchain): unambiguous status lines and failures for agents and CI logs`.
- `src/Cli.Commands/src/Oath/OathHelpers.cs` — blob `db7e653ab4f806438113c12b85bc843dabe1ff8b`; latest pinned-path commit `8503fe2dc fix(oath): share Base32 decoding and validate credential requests`.
- `src/Core/src/Abstractions/IYubiKey.cs` — blob `dca5f1de3b07140217ede8fd50ee6800e9208edd`; latest pinned-path commit `867005429 feat(core): expose last-known device metadata on IYubiKey`.
- `src/Core/src/Credentials/ConsoleCredentialReader.cs` — blob `0418d7391a09c38c3d86d844a09848b0207b9406`; latest pinned-path commit `7db073e0a fix(core): read credential confirmation by line when input is redirected (audit N3)`.
- `src/Core/src/Credentials/CredentialReaderOptions.cs` — blob `31a445a3c16e2eca0d8e5545abf3d9b4b02e06ce`; latest pinned-path commit `e2aa2192b style: apply dotnet format across solution`.
- `src/Core/src/Credentials/UserPresenceNotification.cs` — blob `a9704b1ed619b31ccda8ff49fbae6397e0521baf`; latest pinned-path commit `a58815339 feat(core): let apps attach intent to user-presence prompts (#682)`.
- `src/Core/src/Cryptography/ArkgPrimitivesOpenSsl.cs` — blob `3b19dc377dc0647dae2b8424be656fd2689ff8fb`; latest pinned-path commit `9ef6588d8 fix(core): replace hand-written HKDF with System.Security.Cryptography.HKDF (YESDK-1633, 25d)`.
- `src/Core/src/Cryptography/CryptographicDefinitions.cs` — blob `461957c1857a17f56054897223b92cb8c5745ce2`; latest pinned-path commit `56d721288 feat(core): add cryptographic catalog and OpenPGP key mappings`.
- `src/Core/src/Cryptography/ECPrivateKey.cs` — blob `b8caaefb00a693f30f8459be42d74af67adeff04`; latest pinned-path commit `56d721288 feat(core): add cryptographic catalog and OpenPGP key mappings`.
- `src/Core/src/Cryptography/PrivateKeyConsistency.cs` — blob `038948c96705c0907293cd80b0f68cd53a34e435`; latest pinned-path commit `56d721288 feat(core): add cryptographic catalog and OpenPGP key mappings`.
- `src/Core/src/Cryptography/RSAPrivateKey.cs` — blob `d529b4a0ecbc44fb7c29b823885adc6a7f100637`; latest pinned-path commit `56d721288 feat(core): add cryptographic catalog and OpenPGP key mappings`.
- `src/Core/src/Devices/DeviceConnectionRegistry.cs` — blob `0e88612391f4a1da1c90d661721cc91b9b248950`; latest pinned-path commit `257a88fff docs(core): document registry lease members and ConnectionType helpers`.
- `src/Core/src/Devices/DeviceInfo.cs` — blob `47c998d5b72b18e5d2e31dd261958098b3a8a2bb`; latest pinned-path commit `b590d44b8 fix(core): compare DeviceInfo by value and reject partial enumeration in monitored scans`.
- `src/Core/src/Devices/DeviceInfoReader.cs` — blob `c147cf1d5ac3bdb25b19b05256e072df91a724d7`; latest pinned-path commit `f2df4cc6b merge(yubikit): update CTAP2 rename against repaired audit history`.
- `src/Core/src/Devices/DiscoveryDeviceInfoReader.cs` — blob `5c36c52f971acb6c912f71a4013114f3fb8ca05f`; latest pinned-path commit `d4f61110a refactor(core): split ProtocolDeviceInfo by responsibility`.
- `src/Core/src/Devices/DiscoveryIdentityReader.cs` — blob `ce34656d46071159cc2f01d033f2c39bd48c9205`; latest pinned-path commit `d4f61110a refactor(core): split ProtocolDeviceInfo by responsibility`.
- `src/Core/src/Devices/Feature.cs` — blob `320c26f094782e4e706a74283326904ff17b7794`; latest pinned-path commit `752b34eff Reorganize Core source layout`.
- `src/Core/src/Devices/FindYubiKeys.cs` — blob `47252936c81ab20ea15d4b249ca28dde69e11811`; latest pinned-path commit `d4f61110a refactor(core): split ProtocolDeviceInfo by responsibility`.
- `src/Core/src/Devices/FirmwareVersion.cs` — blob `b8a2261a1302360dca391ae7fd29558befef0d83`; latest pinned-path commit `73c9069b7 fix(piv): treat an alpha or beta key as at least firmware 5.8.0`.
- `src/Core/src/Devices/FormFactor.cs` — blob `942584dd465edf85dc73f29cdb420f1b627e8c10`; latest pinned-path commit `752b34eff Reorganize Core source layout`.
- `src/Core/src/Devices/HidConnectionSlot.cs` — blob `febe52df52754dd3576854ec6cdcddeabc6556c6`; latest pinned-path commit `529e2c8ac docs(core): document Linux HID ownership and compatibility contracts`.
- `src/Core/src/Devices/ProtocolDeviceInfo.cs` — blob `316fe705b541feb056382809fda7359276c771d5`; latest pinned-path commit `820c323a8 chore(merge): integrate latest yubikit discovery changes`.
- `src/Core/src/Devices/SharedDeviceInfoRead.cs` — blob `17dd2b31dcde041ab7f750679e6a19bd8479d908`; latest pinned-path commit `820c323a8 chore(merge): integrate latest yubikit discovery changes`.
- `src/Core/src/Devices/YubiKeyDevice.cs` — blob `a910a7f6a34b080abf2297e5d10c95302773cf72`; latest pinned-path commit `820c323a8 chore(merge): integrate latest yubikit discovery changes`.
- `src/Core/src/Devices/YubiKeyManager.cs` — blob `8c1c947094cf9d9a7d024e91eaa58d8a578e392d`; latest pinned-path commit `fa0fe8e63 refactor(core): name the finder's interface set 'interfaces' and expose KnownPids`.
- `src/Core/src/Native/Desktop/SCard/SCardReaderStates.cs` — blob `ec325e46a9b0e5f64930ee84c7d472e3f7428d52`; latest pinned-path commit `013f3587d fix(core): release PCSC discovery reader names (YESDK-1626, audit 18)`.
- `src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs` — blob `45ffb348d555176602d99dc4bcaa2a6165728d67`; latest pinned-path commit `f2df4cc6b merge(yubikit): update CTAP2 rename against repaired audit history`.
- `src/Core/src/Protocols/Otp/Hid/OtpHidProtocol.cs` — blob `bb843d34eeabfe7969bdcebe17c6eb3387122501`; latest pinned-path commit `8d13fd1ed fix(core): keep opaque callback and recovery errors out of diagnostics`.
- `src/Core/src/Protocols/SmartCard/Apdu/ApduFormatterExtended.cs` — blob `77638a22740e9aadd938cf1bfb1a70c0e22cde20`; latest pinned-path commit `284557812 chore: clean usings`.
- `src/Core/src/Protocols/SmartCard/Apdu/ApduFormatterShort.cs` — blob `c9c77c94997c84995fb87af8594b4feda293acaf`; latest pinned-path commit `284557812 chore: clean usings`.
- `src/Core/src/Protocols/SmartCard/Apdu/ApduResponse.cs` — blob `346d21bb714448646ab3d9425f8860795bc702e3`; latest pinned-path commit `1eb242e3a fix(core,piv)!: clear response data through explicit buffer ownership`.
- `src/Core/src/Protocols/SmartCard/Apdu/PcscProtocol.cs` — blob `effdc2eaebcab2fd310bc68960e7f609e3999e94`; latest pinned-path commit `8d13fd1ed fix(core): keep opaque callback and recovery errors out of diagnostics`.
- `src/Core/src/Protocols/SmartCard/Scp/Scp03KeyParameters.cs` — blob `54cfb101b26e33c803c41081bfd74c3b1b9f8c6e`; latest pinned-path commit `284557812 chore: clean usings`.
- `src/Core/src/Protocols/SmartCard/Scp/Scp11KeyParameters.cs` — blob `e38476e52b50893e557d095e7fd6bb82aa014cd2`; latest pinned-path commit `284557812 chore: clean usings`.
- `src/Core/src/Protocols/SmartCard/Scp/ScpInitializer.cs` — blob `d7fe567c828ccbd1394b16e1e7525815ef0719c5`; latest pinned-path commit `cc13b5185 fix(core): refuse smart-card reuse after interrupted secure exchanges`.
- `src/Core/src/Protocols/SmartCard/Scp/ScpProcessor.cs` — blob `6e4b9b0588e851367bad3c6ab17f3a59fde6a051`; latest pinned-path commit `c20ef5a23 fix(core)!: under SCP, a success response without an R-MAC is refused (N2)`.
- `src/Core/src/Protocols/SmartCard/Scp/ScpState.Scp03.cs` — blob `de4ac094600cc2f0f4f192f5cf791a72d19adf7b`; latest pinned-path commit `598e93805 docs(core): annotate ScpState with GlobalPlatform references`.
- `src/Core/src/Protocols/SmartCard/Scp/ScpState.Scp11.cs` — blob `3eb54cb337d35ac6ebf5782a1974ed8254e59cf4`; latest pinned-path commit `598e93805 docs(core): annotate ScpState with GlobalPlatform references`.
- `src/Core/src/Protocols/SmartCard/Scp/ScpState.cs` — blob `c5109c82f53a9904fe4f2e0520cc28ece6b9c273`; latest pinned-path commit `c20ef5a23 fix(core)!: under SCP, a success response without an R-MAC is refused (N2)`.
- `src/Core/src/Protocols/SmartCard/Scp/StaticKeys.cs` — blob `bf09088014312610f0a317f8aa09232c3e4a8082`; latest pinned-path commit `865f33237 fix(core): add internal ECDH/CMAC seams and harden SCP03/SCP11 secret lifecycle`.
- `src/Core/src/Sessions/ApplicationSession.cs` — blob `675d5d69b4b4beaea0126981b2962a6bc02e9a47`; latest pinned-path commit `60574b706 refactor(core): make feature enforcement protected session machinery`.
- `src/Core/src/Transports/SmartCard/FindPcscDevices.cs` — blob `604531d4a57c6e77208baa31954135df19ac3a59`; latest pinned-path commit `7b9ca8ca7 fix(core): collapse duplicate PC/SC reader names during enumeration (defensive, unreproduced)`.
- `src/Core/src/Transports/SmartCard/PcscNativeResources.cs` — blob `e4773c7a9bd4707af1f4197e2998b4f86c0bb906`; latest pinned-path commit `eef399698 fix(core)!: default PCSC connections to strict exclusive access (YESDK-1630, audit 22)`.
- `src/Core/src/Transports/SmartCard/SmartCardConnectionFactory.cs` — blob `9d1ba3c10197ced851f09759ae0844599e5603ec`; latest pinned-path commit `54a4e08c4 docs(core): clarify PCSC contention recovery and ownership (YESDK-1630, audit 22)`.
- `src/Core/src/Transports/SmartCard/SmartCardSharing.cs` — blob `ba923598c70e70b884dee18fb9dc1264ea78460b`; latest pinned-path commit `eef399698 fix(core)!: default PCSC connections to strict exclusive access (YESDK-1630, audit 22)`.
- `src/Core/src/Utilities/Base32.cs` — blob `fc1cd20330d27f70345fe6bb2bf879a922601b93`; latest pinned-path commit `8503fe2dc fix(oath): share Base32 decoding and validate credential requests`.
- `src/Core/src/Utilities/ExchangeGuard.cs` — blob `b2980dd7dd5ce35c9ec0db1029e2380dae9edc60`; latest pinned-path commit `be3807563 fix(core): close raw protocol review gaps`.
- `src/Core/src/Utilities/Tlv.cs` — blob `829915a09524ed713edf97b5bea18f77582f2449`; latest pinned-path commit `ed9cd7356 fix(core): validate BER-TLV long-form lengths (YESDK-1633 25e, YESDK-1628 #20)`.
- `src/Core/src/Utilities/TlvHelper.cs` — blob `0d415f18d4fb5d5f345b0e08cd99ee5a76c7f314`; latest pinned-path commit `a8066061a fix(core): size TlvHelper.EncodeDictionary from the built TLVs (YESDK-1633, 25f)`.
- `src/Core/src/YubiKitLogging.cs` — blob `f6642bd38e1bcdcf9fd1da1151e8efba292a3817`; latest pinned-path commit `e2aa2192b style: apply dotnet format across solution`.
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreScpAuditReproTests.cs` — blob `87e5c1ec75fd94b0f9c9b44523a725cb20bb2c15`; latest pinned-path commit `d89b75349 docs: keep documentation about the code, not about how it was written`.
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceInfoFieldLengthTests.cs` — blob `b365b7a727d563ef982bc88e25407e0b961e9326`; latest pinned-path commit `331d3dc1c fix(core): validate device-info field lengths before decoding (YESDK-1629, audit 21)`.
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/PcscReaderNameOwnershipTests.cs` — blob `3e2bbc0408cfd0cf29b23e8452d2a394d9502f3b`; latest pinned-path commit `013f3587d fix(core): release PCSC discovery reader names (YESDK-1626, audit 18)`.
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/SmartCardSharingTests.cs` — blob `306a9af3f31b13353240323c01d94a0b8367c030`; latest pinned-path commit `eef399698 fix(core)!: default PCSC connections to strict exclusive access (YESDK-1630, audit 22)`.
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Utilities/Base32Tests.cs` — blob `72e02a63e50e7ce1c12ac9bea6c9f18350c38f8f`; latest pinned-path commit `8503fe2dc fix(oath): share Base32 decoding and validate credential requests`.
- `src/Ctap2/src/Cbor/AaguidConverter.cs` — blob `fe898959499ae3e29bdbfd8b8cd48b3ce87a94f3`; latest pinned-path commit `3bc3e4091 docs(ctap2): reconcile protocol contracts and WebAuthn boundaries`.
- `src/Ctap2/src/Cose/CoseArkgP256SeedKey.cs` — blob `739b54f84db0661e21167d0d844ba7d3068271b4`; latest pinned-path commit `3bc3e4091 docs(ctap2): reconcile protocol contracts and WebAuthn boundaries`.
- `src/Ctap2/src/Cose/CoseKey.cs` — blob `0c7bd496032a686cae30372be4f93388be04e551`; latest pinned-path commit `727fe43af chore(audit): integrate merged PIV APIs into documentation campaign`.
- `src/Ctap2/src/Credentials/AttestedCredentialData.cs` — blob `302a96a84ed23f8b995f1f15583d905b5b653b59`; latest pinned-path commit `3bc3e4091 docs(ctap2): reconcile protocol contracts and WebAuthn boundaries`.
- `src/Ctap2/src/Credentials/AuthenticatorData.cs` — blob `7ba5afe4a6765070a7692a4cb0bff2f0e02c6a6d`; latest pinned-path commit `3bc3e4091 docs(ctap2): reconcile protocol contracts and WebAuthn boundaries`.
- `src/Ctap2/src/Credentials/Ctap2CredentialOptions.cs` — blob `f3833504f051b5a3c41c5bf72b99a0f6ef112686`; latest pinned-path commit `3bc3e4091 docs(ctap2): reconcile protocol contracts and WebAuthn boundaries`.
- `src/Ctap2/src/Credentials/GetAssertionResponse.cs` — blob `c4169a5f9ff3a9489771830b923afc687d6a5d6a`; latest pinned-path commit `3bc3e4091 docs(ctap2): reconcile protocol contracts and WebAuthn boundaries`.
- `src/Ctap2/src/Credentials/MakeCredentialResponse.cs` — blob `d8e7194ff3f75a1e5957dca1a4bad5aadf1567e8`; latest pinned-path commit `3bc3e4091 docs(ctap2): reconcile protocol contracts and WebAuthn boundaries`.
- `src/Ctap2/src/Crypto/EncryptedMetadataDecryptor.cs` — blob `7068c9df6f495878d2593cb0721e8c6e589caaa4`; latest pinned-path commit `3bc3e4091 docs(ctap2): reconcile protocol contracts and WebAuthn boundaries`.
- `src/Ctap2/src/Ctap2Session.cs` — blob `611bcb0bc54ed620af59cc865f5b99261672e88b`; latest pinned-path commit `3bc3e4091 docs(ctap2): reconcile protocol contracts and WebAuthn boundaries`.
- `src/Ctap2/src/Extensions/PreviewSign/PreviewSignDerivedKey.cs` — blob `17e9df37649b2b2877d3e05eb3f015eed330c2eb`; latest pinned-path commit `3bc3e4091 docs(ctap2): reconcile protocol contracts and WebAuthn boundaries`.
- `src/Ctap2/src/Extensions/PreviewSign/PreviewSignRegistrationOutput.cs` — blob `0276084c107b4a775e9827cfde2038ae65a24a46`; latest pinned-path commit `3bc3e4091 docs(ctap2): reconcile protocol contracts and WebAuthn boundaries`.
- `src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs` — blob `4f3903023fd5493e70c224d321383c722547bdc6`; latest pinned-path commit `3bc3e4091 docs(ctap2): reconcile protocol contracts and WebAuthn boundaries`.
- `src/Ctap2/src/LargeBlobs/LargeBlobData.cs` — blob `f9cc6cc1c34af361cae213f8432a15159a2a3ba9`; latest pinned-path commit `3bc3e4091 docs(ctap2): reconcile protocol contracts and WebAuthn boundaries`.
- `src/Ctap2/src/LargeBlobs/LargeBlobStorage.cs` — blob `9e120dda261b018b87ada262182b19d1bbfeee36`; latest pinned-path commit `3bc3e4091 docs(ctap2): reconcile protocol contracts and WebAuthn boundaries`.
- `src/Ctap2/src/Pin/PinUvAuthProtocolV1.cs` — blob `1b542b2c5797256606df595abe644aa2873edd41`; latest pinned-path commit `3bc3e4091 docs(ctap2): reconcile protocol contracts and WebAuthn boundaries`.
- `src/Ctap2/src/Pin/PinUvAuthProtocolV2.cs` — blob `8aaeeef430d9600727bc112f9af9dcf430bca37b`; latest pinned-path commit `3bc3e4091 docs(ctap2): reconcile protocol contracts and WebAuthn boundaries`.
- `src/Management/src/IManagementSession.cs` — blob `7dcd36b4597191e92a01e50e7c6b080763538148`; latest pinned-path commit `bac07ee76 docs(management): reconcile contracts examples and audit evidence`.
- `src/Management/src/ManagementSession.cs` — blob `d57fb7e74a7072f3e25b6b1fac4eecf1ed7c3340`; latest pinned-path commit `bac07ee76 docs(management): reconcile contracts examples and audit evidence`.
- `src/Oath/src/CredentialData.cs` — blob `5e8ff7b80da7e661faf1c8a3420d10f42cf10dd4`; latest pinned-path commit `8503fe2dc fix(oath): share Base32 decoding and validate credential requests`.
- `src/Oath/src/OathSession.cs` — blob `d827d1acccee9239952a1061f1cfe08ae703ef7e`; latest pinned-path commit `57e0fa4d5 Merge reviewed terminal OATH reset and timestamp fixes`.
- `src/OpenPgp/src/OpenPgpSession.Config.cs` — blob `f7b742b9dfa8cf6e1f0125deea9ed235c93fa861`; latest pinned-path commit `727fe43af chore(audit): integrate merged PIV APIs into documentation campaign`.
- `src/OpenPgp/src/OpenPgpSession.Crypto.cs` — blob `01a8374188a42eb5e89d5faf7a7ed5fefe9197f7`; latest pinned-path commit `727fe43af chore(audit): integrate merged PIV APIs into documentation campaign`.
- `src/OpenPgp/src/OpenPgpSession.Pin.cs` — blob `8723f0c9418e2f16ca149a4124429193a8878048`; latest pinned-path commit `84715ee1c docs(openpgp): document PIN, key-operation and reset contracts`.
- `src/OpenPgp/src/OpenPgpSession.cs` — blob `28d8d539c1989b334b63448c808f5576999acfb1`; latest pinned-path commit `84715ee1c docs(openpgp): document PIN, key-operation and reset contracts`.
- `src/Piv/src/Cryptography/PrivateKeyOperations.cs` — blob `92b6cb47e3864e53b70c602dc9c5a2eab71d7d09`; latest pinned-path commit `fe342fb0b chore(audit): integrate current Core upstream into documentation campaign`.
- `src/Piv/src/PivSession.cs` — blob `64014877bb5fdb407c36e58d6b29672c90545866`; latest pinned-path commit `bf0c0207b docs(piv): document credential, key and certificate contracts`.
- `src/SecurityDomain/src/SecurityDomainSession.cs` — blob `bc30e5ae9948228ecd9396f524e799ed2ea50aaf`; latest pinned-path commit `727fe43af chore(audit): integrate merged PIV APIs into documentation campaign`.
- `src/Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs` — blob `91fbadb65e75fa67bf51b1e98cc261cf447b9401`; latest pinned-path commit `7c534e8b7 feat(core): make discovery one-shot and expose owned monitors`.
- `src/WebAuthn/src/Attestation/WebAuthnAttestationObject.cs` — blob `44110fe6d947e8d6223cb080fce8a82125e848db`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Client/Authentication/AuthenticationResponse.cs` — blob `102c10827ff1451c155ba01eccb2ec502f026e3b`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Client/Authentication/CredentialMatcher.cs` — blob `1cc293ba0a2b4542c09ccdee259e8d88f6a9839b`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Client/CeremonyDeadline.cs` — blob `1e9727d69bbe472f62cba241b10722cb8d770280`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Client/CredentialFilter.cs` — blob `1869537cf45c08aa32c2cfee83ec204a2da6e390`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Client/PinUvAuthTokenSession.cs` — blob `1c031d01a30385130918d0f96a45fd506d427653`; latest pinned-path commit `f7f05f846 refactor(ctap2)!: rename FIDO2 protocol module and session`.
- `src/WebAuthn/src/Client/PinUvSharedSecret.cs` — blob `b9dbbee7fa527a79f437926904944a92fa20f7c4`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Client/UserVerification/UvDecision.cs` — blob `f515efa639478bfb88a601391c640234d0e1cbdf`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Client/WebAuthnBackend.cs` — blob `b10cf771a163be153d8af97f16a14b3ebf8386d9`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs` — blob `316bdcc0e211f8b3899bfa371a353b099672e59f`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Client/WebAuthnClient.Ceremony.cs` — blob `a80d86e080fcb1e8fc18c0332ea5469e57d94274`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Client/WebAuthnClient.PinUvAuth.cs` — blob `4b1fe6a8399990aeae659793a5f3dbfb3cd0501a`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Client/WebAuthnClient.Registration.cs` — blob `cf2e08e1e3c113ef0bca323764c2e29daae6dff0`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Client/WebAuthnClient.Validation.cs` — blob `2bab4ba9b9ef061166d869fb97c5aa7398722c78`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Client/WebAuthnClientData.cs` — blob `499f366b17418d5fc8aa0b32e8f7ead29cd39aad`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Cose/Aaguid.cs` — blob `38d87c530b93d80577b8bd47a36fca3c247b1ed6`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Extensions/Codec/MinPinLengthCodec.cs` — blob `d58dc7ce9426f20f4ed45826db5241d84bef93d4`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Extensions/Codec/PreviewSignCodec.cs` — blob `df4bad978f5e23b2560b073b390c6d06b2cd8e31`; latest pinned-path commit `3bc3e4091 docs(ctap2): reconcile protocol contracts and WebAuthn boundaries`.
- `src/WebAuthn/src/Extensions/Codec/PrfCodec.cs` — blob `af51cada1c54f92b43f47aa6b8effda73e89f260`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Extensions/Inputs/CredProtectInput.cs` — blob `e3ca1d73b82df88311a85da148bd129078cda795`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Extensions/Inputs/LargeBlobInput.cs` — blob `18ca8c07b80a34b2cff5be668acb52bdb7750c43`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Extensions/Inputs/LargeBlobSupport.cs` — blob `a630f3a6e16df8cf6b9ddff886eaa8f4fd5474cc`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Extensions/Outputs/LargeBlobOutput.cs` — blob `0ddb7f58ef743c732c98049cc6106943835df8e4`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Extensions/Outputs/PrfOutput.cs` — blob `9bdbe50cbf5f0b0e8ad45e226d3fafd3984a0d19`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Extensions/PreviewSign/GeneratedSigningKey.cs` — blob `79063e8aa11ee90e16b6c1aa6031f73528971999`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignRegistrationOutput.cs` — blob `0c27c1cc294ca472f63e11e17697325b899c0ea8`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignSigningParams.cs` — blob `8a0db6787bdde961c4ad338368fadba27312fffa`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Extensions/Processing/AuthenticationExtensionSession.cs` — blob `07c062604de647ff9d5573ce284836c82fe1a064`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Extensions/Processing/Processors/CredProtectProcessor.cs` — blob `4d22e555794beb54fb3831ab05784da204b8a712`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Extensions/Processing/Processors/LargeBlobProcessor.cs` — blob `35960eaed8a59f24372f5e0bd3196bcf7f348edb`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Extensions/Processing/Processors/MinPinLengthProcessor.cs` — blob `80845d3d41902f1960a4456e2fc2a789a67524d7`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Extensions/Processing/Processors/PreviewSignProcessor.cs` — blob `d198739bacb24fc5ac6fbb6a30dde379b15d49cd`; latest pinned-path commit `f2df4cc6b merge(yubikit): update CTAP2 rename against repaired audit history`.
- `src/WebAuthn/src/Extensions/Processing/Processors/PrfProcessor.cs` — blob `8ee4732375b0c847d0d37373078b5eeecaed4367`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/WebAuthn/src/Preferences/AttestationPreference.cs` — blob `4914cd33504827ea4f8243b979b9310cdf5b5656`; latest pinned-path commit `9399b94bd docs(webauthn): reconcile public contracts, examples and spec references`.
- `src/YubiHsm/src/HsmAuthSession.cs` — blob `95e90197f92791edd57c09362c2fd43c46256907`; latest pinned-path commit `e053386a6 docs(yubihsm): clarify enrollment, retry and session-key contracts`.
- `src/YubiHsm/src/SessionKeys.cs` — blob `ff330c32262b6a8665343519c7f72f2114fcdd61`; latest pinned-path commit `e053386a6 docs(yubihsm): clarify enrollment, retry and session-key contracts`.
- `src/YubiOtp/src/Backend/HidBackend.cs` — blob `59f286ffc64be638e06801eae9154dc2644e6567`; latest pinned-path commit `f4419397d docs(yubiotp): clarify slot configuration and challenge contracts`.
- `src/YubiOtp/src/IYubiKeyExtensions.cs` — blob `1eb4669d595b81867a1017d8c91fab7615d598dd`; latest pinned-path commit `f4419397d docs(yubiotp): clarify slot configuration and challenge contracts`.
- `src/YubiOtp/src/IYubiOtpSession.cs` — blob `7e658cc4417848dd2b5827a05e6de0410f5ec168`; latest pinned-path commit `f4419397d docs(yubiotp): clarify slot configuration and challenge contracts`.
- `src/YubiOtp/src/YubiOtpSession.cs` — blob `c650c63c123348c808c213291fa3203113fb9d0e`; latest pinned-path commit `f4419397d docs(yubiotp): clarify slot configuration and challenge contracts`.

## Retired architecture mapping

| Earlier symbol/path | Current replacement | Rebaseline consequence |
| --- | --- | --- |
| `ProtocolDeviceInfo.DiscoveryReadLeaseTracker`; earlier identity-read plumbing | `DiscoveryIdentityReader` remains and delegates to `DiscoveryDeviceInfoReader`; `SharedDeviceInfoRead` owns the native/read lifetime; `ProtocolDeviceInfo` borrows an open connection | Bounded caller wait and actual native teardown remain separate. `d4f61110a` splits responsibilities; `f32848f62` owns Linux FIDO/OTP native lifetimes. |
| Device repository / event hub / global manager service | `DeviceChangeFeed`, `SnapshotDiff`, `YubiKeyMonitor`, `HotplugSignals`, `RescanScheduler` | `e8828151d`, `8754f075b`, `5ff8d25af`: recheck replay/publication/retention/disposal claims against single owned lifetime. Deleted generation/start-stop paths are history, not current defects. |
| Shared discovery/filter-only Find calls | fresh `FindYubiKeys.CreateForInterfaces`, manager `FindAllAsync(interfaces: ...)` | `6d1e8dc04`: scope is pre-enumeration selection rather than post-merge filtering. Disjoint view member sets still lack global serial aliasing. |
| `RawFidoHidSession`, `ApplicationIds.Fido2` | `RawCtapHidSession`, `ApplicationIds.Fido` plus same-AID `FidoU2f` | Keep CTAPHID framing separate from CTAP2 operation/UV/profile policy and FIDO2 capability identity. |
| Deleted per-tool `FidoDeviceSelector` / `OpenPgpDeviceSelector` | surviving shared CLI selector and its consumers | Carry selection/cancellation/result documentation triggers into active shared call sites; avoid resurrecting deleted files. |
