# OpenPGP Pass 1 editor handoff

**Editor:** fresh GPT-6.1 Sol session, 2026-11-04. **Baseline:**
`fe180282a2d77b15eaaa5868d6c8b00e251d1f05`. **Status:** documentation edits ready for independent
content/citation review and compilation; this report claims neither those gates nor implementation completion.

## Scope and preservation

Read root/module/test instructions, campaign workflow/style, SDK house style, RUN plan/findings/investigation,
coverage and original-source analysis. Checked complete affected operation/model bodies and actual relevant test
assertions. Used Context7 for current .NET memory/verification API orientation. Read cached original Card/RFC/vendor
passages before relying on consequential claims. Other-module targets received link-existence checks only;
Core source and other-module source/documentation were outside the investigation.

Changed 58 assigned product paths: seven shared documents, module README/CLAUDE, 48 C# comment/XML files and one
descriptive package-metadata field. The package README remains unchanged: its task description was accurate.
C017's encryption overclaim was in the project Description, which now describes raw decipher/agreement instead.
No executable bodies, help/output strings, imports, assertions, API snapshots or directives were changed.
Patch application added final newlines to some files that previously lacked one; this is whitespace only.
No builds, tests, formatter, hardware operations, agents or commits were run by this editor.

The coordinator's `docs/research/documentation-audit/README.md` edit and all untracked user/RUN work were left
alone. This editor owns only this RUN report plus the product paths below; all other RUN records remain with
their assigned owners. No prior finalized archive was edited.

## Finding-to-change map

Paths below are relative to `src/OpenPgp/` unless explicitly rooted elsewhere. Full scope and reopening IDs
remain in findings.md/investigation.md: **14 overlapping deferred engineering groups and four Core questions**,
not 21 independent implementation bugs.

| Accepted candidate | Edited semantic units |
|---|---|
| C001 | `src/Pw.cs`, `src/IOpenPgpSession.cs` Verify/crypto contracts, `src/OpenPgpSession.Pin.cs`, exception XML, README credentials/KDF, PIN wire-test explanations and migration exception notes. Explain PW1 modes versus Reset Code; fresh `AttemptsUser` inspection after extended failures; typed zero alone fails to establish User blocking. |
| C002 | Config/Pin/KDF/SetKdf XML, internal cache comment, README provisioning before keys and complete fresh-session ordinary User/Admin verification fragment; Advanced test comments identify both-derived equal inputs and cached readback. State-dependent F9 refusal remains bounded, with no universal no-key firmware rule. |
| C003 | Kdf/serialization/Get/Set/Reset ownership, session comments, module guidance and complete KDF resource scopes. Retain aliased objects until session teardown; explicitly cover dropped old references, separate raw cache and sensitive serialization. No future clone/transfer policy invented. |
| C004 | Reset/Uif/ATT contracts, module guide/guidance, reset/set-touch sample comments and user-interaction cross-link. Ordinary key replacement/deletion can clear fixed policy; ATT reset/fixed exceptions and irreversible factory-key trust loss remain local. |
| C005 | Reset XML/internal comments/helper/test explanations and guide readback task. Attempted sequence versus independently verified postconditions; ignored lifecycle statuses and partial state preserved as current limitations. |
| C006 | Sign/Authenticate XML and guide input/output table, P-256 decode/host-verification group; raw EdDSA message and unused hash parameter, RSA raw output and ECDSA DER. |
| C007 | Decrypt XML/guide raw operation and complete consume/clear fragment, plus the local crypto formatter comment. Remove `.gpg` file-processing implication; explain RSA PKCS#1 block versus NIST ECDH point/shared secret and current X25519 wrapper gap. |
| C008 | Result-transfer/clearing guidance in interface, README, crypto helper and module guidance. Clear actual owned ROM backing storage using MemoryMarshal.AsMemory; distinguish borrowed source and owned result. Preserve successful RSA/EdDSA transfer versus ECDSA conversion/clearing. |
| C009 | Generate/import/delete/attribute/raw/cache XML, key internal comments, sample provisioning comments and guide. Explain destructive attribute-first mutation, later refresh failure, fresh inspection and independent certificate/fingerprint/time management. |
| C010 | Feature-field XML, interface/config discovery, capability byte units, module requirements/guidance and guide. Current SDK thresholds versus advertised/family/slot limits; 1.0.0 compatibility fallback and historical commercial-version ambiguity preserved. |
| C011 | Guide ROCA rationale and four test early-return explanations. Exact vendor affected 4.2.6–4.3.4 versus Python conservative [4.2.0,4.3.5), corrected 4.3.5, imported-key mitigation, unchanged current <=4.3.5 tests and missing shipping guard. |
| C012 | Attest XML/guide/example and actual certificate-test commentary. Target overwrite, ordinary User route without exclusive-mode inference, ATT UIF, retrieval-after-write failure, caller disposal and host binding/CA trust decisions. |
| C013 | Crt.Att/KeyRef/key-operation XML, guide and module model guidance. Actual malformed B6 01 81 versus extension B6 03 84 01 81; keep ATT provisioning limitation separate from target-key attestation. |
| C014 | PrivateKeyImport/delete commentary, key-operation XML, guide/guidance. Current CRT02-with-modulus versus vendor03 and NEO delete/generate differences remain compatibility qualifications. |
| C015 | OpenPgpTool command/selector comments and guide direct-SDK alternatives. Unused --pin, fingerprint-length presence, immutable secret strings/process arguments, missing touch prompt, selection and certificate disposal limits; runtime/help strings preserved. |
| C016 | Integration/wire/model test explanations follow actual assertions. Nonempty output remains shape evidence; attestation bytes remain overwrite evidence; reset counters remain partial readback. Removed obsolete upstream/ticket narration from comments while preserving executable traits/names. |
| C017 | Complete guide task flow, all 40 interface operations, private-key/certificate resource scopes and project Description. Package README intentionally clean and unchanged. |
| C018 | Model/accessibility guidance; AlgorithmAttributes/ApplicationRelatedData/Discretionary/PwStatus/CurveOid/Aid/DataObject/SecuritySupportTemplate/template/credential-reader XML and relevant test commentary. Correct language byte pairs, OID value octets, serial fallback, signature-counter resets, parser scope and seed terminology. PinPolicy.Once describes card access state rather than only host lifetime. |
| C019 | Exception/PIN XML/internal commentary, guide and migration exception sections distinguish current broad VERIFY mapping, nullable/uncertain counters and general change/reset errors. |
| C020 | Root README row; highlights/comparison OpenPGP task links; migration guide/map/gaps current OpenPGP qualifications; user-interaction OpenPGP paragraph. Preserve historical chronology discrepancy, DOC-002 and other-module prose. |
| C021 | Qualified version provenance in factory/feature/module requirements guidance only. Preserve applet versus metadata/effective-policy distinction without diagnosing a Core defect or publishing a lab observation as generic behavior. |

## Late evidence reconciliation

Coordinator packets report all 49 existing integration methods passed (48 non-KDF including RSA3072/4096,
then isolated KDF), and complete independent cleanup passed. The interrupted initial invocation remains separate.
These are coordinator observations, not editor executions. Public test comments still describe only reached
assertions, without upgrading length/nonempty checks into cryptographic/trust proof.

The coordinator's v1 retry probe observed User 3→2, Reset 0→0 and typed zero; its failure log did not record SW,
so this report infers no measured status for that attempt. The v2 KDF packet reports F9 before key generation,
fresh distinct-salt readback, direct default Admin/ordinary User verification without extra Change calls and a
host-verified ordinary signature, followed by extended verification of the correct User PIN returning SW6982,
typed zero and User 3→2. The earlier order generated a key before F9 and received 6985 during setup.
Guide/XML use that evidence for actionable extended-mode qualification and a pre-key provisioning task;
they make no universal all-firmware no-key precondition claim. `hardware/results.md` and the coordinator's raw
logs are the authoritative hardware record; its file may lag the newest packet during ongoing work.

The first human-ready sign attempt returned 6600 after about 14.24 seconds, with a noncancelled token and
PolicyRequires→Failed notification. A successful signature was unreached, and the packet establishes no causal
human-touch metadata. The public guide/guidance/shared interaction paragraph describe the actual SDK classification:
device APDU errors resolve as Failed, including 6600, rather than TimedOut. Card abort/timeout guidance alone
establishes no causal attribution for that attempt. A coordinated retry was still running at this checkpoint.

ATT CRT, X25519 and remaining presence/cancellation supplemental outcomes were still pending at this editor checkpoint.
Reconcile late outcomes against C004–C008/C012–C013 before final acceptance; this editor has not qualified those
hardware dimensions. The guide's current source-confirmed limitations remain in place.

## Public surface reconciliation

All 40 declared `IOpenPgpSession` operations retain their original executable/API tokens. Concrete partials inherit
the interface XML. Creation/extension factories are additional semantic units. The 39 effectively public types
remain unchanged; the reviewed groups are:

- Session/factory: `IOpenPgpSession`, `OpenPgpSession`, `IYubiKeyExtensions`.
- Attributes/state: `AlgorithmAttributes`, `RsaAttributes`, `EcAttributes`, `ApplicationRelatedData`,
  `DiscretionaryDataObjects`, `ExtendedCapabilities`, `ExtendedLengthInfo`, `SupportedAlgorithm`.
- Domain/protocol carriers: `Crt`, `DataObject`, `CurveOid`, `CurveOidExtensions`, `KeyRef`, `KeyRefExtensions`,
  `Pw`, `PwStatus`, `Uif`, `UifExtensions`, `PinPolicy`, `KeyStatus`, `RsaSize`, `RsaImportFormat`,
  `EcImportFormat`, `ExtendedCapabilityFlags`, `GeneralFeatureManagement`.
- Parsed collections/identity: `GenerationTimes`, `KeyFingerprints`, `KeyInformation`, `OpenPgpAid`,
  `SecuritySupportTemplate`.
- KDF: `Kdf`, `KdfNone`, `KdfIterSaltedS2k`, `KdfHashAlgorithm`.
- Credentials/errors: `OpenPgpCredentialOptions`, `OpenPgpInvalidPinException`.

The private-key-template hierarchy, `PrivateKeyImport`, backend types, `BcdHelper`, `DigestInfo` and `Ins` remain
internal. Public members inside an internal type add no effectively public types. Snapshot/accessor/extension
representations are not separate prose tasks. The corrected CLAUDE model list reflects this distinction.

## Complete snippet inventory and compilation handoff

The complete current sources are the twelve fenced C# bodies in `src/OpenPgp/README.md` at raw SHA256
`a9d28e98804316fce007626cc7b579554023c964b882f13ccad44c28af5ed0b5`. This identifies **all 197 body lines**,
not selected success-path excerpts. Hash each body exactly between the opening fence/newline and closing fence,
including its final newline. The table records full-body hashes and grouping dependencies.

| Group / opening line | Classification and full scope | Full-body SHA256 |
|---|---|---|
| 1 / 50 | Complete read-only entry point; all common directives and owned session | 5542ca500da6f1a37c534e01082300f70d3b8f51744c3fe47e6af0f20007a9c3 |
| 2 / 100 | Composable ChangeCredentialsAsync; five borrowed credential parameters | 7ee760b780601580a24306e600e47871a9c24d3b92d318a38ddd95c68abf09aa |
| 3 / 120 | Two complete alternative recovery methods | 96e614e3c4c87c063a6d0a1474d866d3a8f806a7569a7348b5c2d6a96a7db884 |
| 4 / 169 | Composable ConfigureInitialKdfAsync; full salt/hash/KDF and both session scopes | 35c5b8c7782d30e9e026fce4f7b5921390b70dd56151c9d1175edaa25c4d56e7 |
| 5 / 229 | Composable P-256 generation/status method | a7931e0fab00a90a0fd257662b9382b4b5beb529f2db9b71402d082c80d9f121 |
| 6 / 250 | Composable RSA PKCS#8 import; borrowed byte[] source, owned private-key/session scopes | a3485ea5b6911f8e4f6602037a075a0a8e5fd8c50771f6a8e02bfc1602a17eec |
| 7 / 285 | Composable SignAndVerifyP256Async plus complete DecodeP256PublicKey helper in the same group | 2d963d0cdccd98cd64cb430c64518d2d897fe9b626408afb287a852b845fb8c8 |
| 8 / 342 | Composable decipher/consume method; typed application callback and owned result cleanup | 754cbe59c499c0360b6c4f6697e1b4c3a54043c5dc808dcd8bebe8af94be32d3 |
| 9 / 378 | Composable certificate store/read method; both complete certificate scopes | 7f232c1692697178f18b10a4113bc056dfb43b84bce378ac8ccf6f0e182f37d3 |
| 10 / 396 | Composable generated-key attestation/export; complete X509 scope | 13023e0a198b307520c8060c4e9b271467aeddadcadc47ad127893952a708335 |
| 11 / 416 | Composable signing policy write/read method | 0823dc3a40e008e70a56579670018d53f610299bde0fdf1efa5d2d871a937aee |
| 12 / 461 | Composable attempted reset/partial metadata readback; application owns authorization/full postcondition | 64afa4ae312015f8bf4c44aeb8cbab9998bdd6fc9b6b05b2e7b1cd1015136c18 |

Groups 2–12 use group 1's directives only, with each method placed in a class. Group 7 includes its real TLV
helper; no stub cryptography or undefined PIN locals are required. The callback in group 8 is intentionally
application-dependent content/protocol processing, with a fully typed dependency and explicit borrow duration.
The guide contains no historical before/after C# groups. Preserve complete using/finally scopes during extraction.
Compile with actual OpenPgp/Core references, .NET10/C#14 and repository analyzers; this editor did not run compilation.
Separately compile all 19 OpenPgpTool sources with real Spectre/CLI references, preserving executable behavior.

## Static verification and exact identities

Read-only Python/Pygments checks compared all 48 changed C# files against HEAD: **48/48 identical non-comment,
non-whitespace token sequences**, retaining strings, imports, API declarations, preprocessor tokens and assertions.
Hash algorithm for body identity: Pygments `CSharpLexer`; remove only `Comment.Single`, `Comment.Multiline` and
whitespace `Text`; serialize `(str(token_type), value)` pairs with `json.dumps(..., separators=(',', ':'))`; SHA256
UTF-8 bytes. These are lexical preservation checks, not Roslyn compilation or runtime verification.
Every changed C# XML-comment group parsed as a well-formed XML fragment. All added/rewritten local Markdown link
paths exist; unrelated shared-document links were outside that scan. Existing test-instruction link was preserved.
Interface operation count is still 40. API snapshot bytes equal HEAD:

```text
3c3b422dddb1efeb0fc5b228608fd8682452b2a9507d4031894bce3c4dedadbd  src/OpenPgp/src/PublicAPI.Shipped.txt
51a2c4de21868278f00f7d012e7d1bb12907e70bd5e6a8dfad2ea5fbb85be9e9  src/OpenPgp/src/PublicAPI.Unshipped.txt
```

`git diff --check` exited zero; Git's only warning concerned the existing project-file CRLF normalization rule.

The ordered path/raw/body manifest digest is
`4b662c3884c9526c2cfce00c037b3e9b28588a17e7ae351e6e0d3343342850d0`:
ordered `git diff --name-only` product paths excluding the coordinator's campaign README; objects have `path`,
`raw_sha256`, then `body_sha256` for C# only; JSON serialization uses compact separators. Raw hashes use working
bytes, independently of Git line-ending normalization. No import-order exception exists.

### Changed-path raw SHA256 inventory

```text
67b8276779c972fa9b36f199272852867a02987c967dc2d714cb98a058b94db2 README.md
18b0785c357b54c44be9d9c05caaf1a9b8e8707189fc80d114250fa9d3aad6fe docs/migration/v1-to-v2-gaps.md
ec496ba7d7eb86c88b9c2ef176c11cc67771c6a49007b5342498eb1f074b5d73 docs/migration/v1-to-v2-map.yml
02d100831e0231835bee36e00c6fb844ba63c1a400078510978e8436d32b6130 docs/migration/v1-to-v2.md
5cc0a1d43d7c6373a1528912b5073ab8c5904a4fb2ac731fc5bf6fb32d5597a0 docs/usage/user-interaction.md
938e441358b1ae3475f1a68919421c13f50e0b8baab9c4f5f22a46af9b718057 docs/v1-to-v2-comparison.md
628c6911d7b2c62861db3c0a2227b734ba009b4312f9da053fb22ed5e12d155f docs/v2-highlights.md
c33139f4764f2405382831d874104cf1cb63a2992566f26771dc0340ae7d0b79 src/OpenPgp/CLAUDE.md
a9d28e98804316fce007626cc7b579554023c964b882f13ccad44c28af5ed0b5 src/OpenPgp/README.md
2fbad1fa86cf62a6e3720675baa2759a3650f85eaf680153bc60b738f6c8aac5 src/OpenPgp/examples/OpenPgpTool/Cli/Commands/CertificatesExportCommand.cs
a5a9b881d132d673b0ae04d6586a6931c8c45e6e246d3d4efbc17b22d8af6d70 src/OpenPgp/examples/OpenPgpTool/Cli/Commands/CertificatesImportCommand.cs
3fa7bdacdf96791a0074b0cdfd11d1a757e5f7006cfcb840681e1af9d78d78c2 src/OpenPgp/examples/OpenPgpTool/Cli/Commands/InfoCommand.cs
8cbe17965a59f94fc7141cba0786c3664136ad6503ca924bb400e59056044717 src/OpenPgp/examples/OpenPgpTool/Cli/Commands/KeysAttestCommand.cs
8966ef9ed58883bf2366e25d21d3fee6764d05aae44391d999f3b6bea91cfb3a src/OpenPgp/examples/OpenPgpTool/Cli/Commands/KeysGenerateCommand.cs
031279f1528a65506ad83bffc418cb5cc8c3b7dcb1b5d26591b909a0ec6353be src/OpenPgp/examples/OpenPgpTool/Cli/Commands/KeysImportCommand.cs
3584703895992acdd2de60492f7a11b623eee9e27891120a8132a94b7fd3e436 src/OpenPgp/examples/OpenPgpTool/Cli/Commands/KeysSetTouchCommand.cs
303de986b42d36fe4e699cee303e412005502b9e39b424ba3ac1a59a7bb14951 src/OpenPgp/examples/OpenPgpTool/Cli/Commands/OpenPgpCommand.cs
178a885130a56fc2c90aa2e4fda37e641671eb2f7aa61b9fd754cb4c4e91b0d8 src/OpenPgp/examples/OpenPgpTool/Cli/Commands/ResetCommand.cs
14ed72f594cbf7d504e91cbaf146cf6a75ef254e7ce9d716c1d7b4d7eeaed455 src/OpenPgp/examples/OpenPgpTool/Cli/Prompts/OpenPgpDeviceSelector.cs
e742cfd3fcfe46cf494e79497d665dd89d3dd3ade4dc089ad4337e473e7c22be src/OpenPgp/src/AlgorithmAttributes.cs
5ae325d247d60ed0bea8ee1b1d666f5e7ab7aeb2e60e35850732492346e1e6e5 src/OpenPgp/src/ApplicationRelatedData.cs
74ff3cd5b67c5adbd1b101d7550fb950d442944975c781f8db08bcfd3662ab9c src/OpenPgp/src/Credentials/OpenPgpCredentialOptions.cs
472043789195dd78289bd081ac36757b10841781b948f8892ae97cdc73f2b920 src/OpenPgp/src/Crt.cs
883f2378039fd639a0cb814a29903b0d0d061642e8f3483ffa7a7fe19ea14355 src/OpenPgp/src/CurveOid.cs
eaedb154c75ed52df38af40b18fdb9df9156c6ba13c4272d877c6c3a55e2ba24 src/OpenPgp/src/DataObject.cs
0bb9ea6c281bdedb86f25224c1f0e65397fff055bdfc35ebc905c529e2c5f104 src/OpenPgp/src/DiscretionaryDataObjects.cs
3591bc11683f40e0e3bff69b7fa9a93c729665ec251d8696a2309e1f1bde6f44 src/OpenPgp/src/ExtendedCapabilities.cs
b43c0bddd27120854db59e065746c743ea3529042c39a77d16f61ab2e8ba9d47 src/OpenPgp/src/IOpenPgpSession.cs
bf19616dd57e25528b1cf95d6a7906ac975729a629a73bf7da32af093113e837 src/OpenPgp/src/Kdf.cs
8659315711a981818be21b1f183a6bc1c83fad884c4b17906de60046d82c64f7 src/OpenPgp/src/KeyRef.cs
8ee3cfbce1845ce9193333ae1f6b0867a0d486256d2c0a5cfb5decb7e2b132a6 src/OpenPgp/src/OpenPgpAid.cs
32131072cb993ce4630c99ac7e1829f1506fc949a6d0d4747b3e1b807ba22220 src/OpenPgp/src/OpenPgpInvalidPinException.cs
7321f5d2c7a4da47d7b4aeffce568b73c651e42866899e2624bbf2796a9a7d2c src/OpenPgp/src/OpenPgpSession.Config.cs
c91410f4bd71eafb9597a4a4850238c6d627bc81a753a5e9d9193811d060778c src/OpenPgp/src/OpenPgpSession.Crypto.cs
02f054d3cecd25d87052f704ae86f297ad7b42c86d46148f1f95d4b770918cbc src/OpenPgp/src/OpenPgpSession.Keys.cs
0e37d06b8d231236ac0b61b0c480af1e115cf9dee8e71fa88ae28df0c273ec48 src/OpenPgp/src/OpenPgpSession.Pin.cs
f892a3f48338adeff8f2805440dc76eb4f327140b7d46e85ee2b3965bbc4dd6c src/OpenPgp/src/OpenPgpSession.Reset.cs
19aa4b550e4fd4e30cd8ce745f38ab0c8e391249784eda0ff2c31235f89bc2d5 src/OpenPgp/src/OpenPgpSession.cs
115d8df77dd27b548776df90c36d301e1300293697d9cf7ac24b9180d896f85d src/OpenPgp/src/PinPolicy.cs
b22541b9708eb8a9286bfa07312475a80119d334e973216ee5b3c0538e617434 src/OpenPgp/src/PrivateKeyImport.cs
4026623b84c0f54d144dab69851fb8b2d0b936291c2890fe55ae890d70c4ed91 src/OpenPgp/src/PrivateKeyTemplate.cs
e0e8fe18804a47f792d6aeb46662b14e16a89a09ad3094964d81ff32aa22a428 src/OpenPgp/src/Pw.cs
e8f13e6a7860ee814b8e632710df819d3bb57d9a79b0bedb8792cacbdd8b3c12 src/OpenPgp/src/PwStatus.cs
132fd5f1617873ff9342ce22521e21553e499b042aa75f9c65999d04241a2c0d src/OpenPgp/src/RsaSize.cs
5d476ef3485784f79e0b027c42c00ee26607301955d6e79125fb7112c0b028e4 src/OpenPgp/src/SecuritySupportTemplate.cs
465caf48023ddea18b5196a16c65a649109b1b435468e2a62f0fa4758eea98f4 src/OpenPgp/src/Uif.cs
d150bc39c7c4545e4cf9b87ed2ad25e967c6116211d66f6026638306a0ac68af src/OpenPgp/src/Yubico.YubiKit.OpenPgp.csproj
10fb63b144c1e22c28c524edfc3b4915a90df20ff1e8a2b7b932ddb7954e0027 src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpAdvancedTests.cs
65cd4304162f736d17938679b049d17b65b39c276743fa95ee6b472b79463236 src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpDecryptTests.cs
22875acf6dfdca9dfee221c801ab275553d20837e58a02148c074b6417ab92c8 src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpKeyImportTests.cs
10b2d3e9fc5b17a33e01d98863ada8872a9f627860abba5237b2971a3fde208d src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpPinManagementTests.cs
3af089b333d8a544437f967c885c085bf2934d324bd86eba55cef166def387da src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpSessionTests.cs
be2285192e822b3fc7fa5351a88bd52b11fb14a1a4a2af499a5d551947868713 src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/TestExtensions/OpenPgpTestStateExtensions.cs
a7419eb23dbfbb28a2aeb106a3f1fee3eccda593aeea4252d89f10f062794505 src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/AlgorithmAttributesTests.cs
8765e254667eab89c6c9de4beea68b033a3d349027317ffbbf41a789997997f9 src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/AuditV2/OpenPgpAuditReproTests.cs
f5a334155a99791c95f1c4ec33fabfbcf644fa40efc2680aad6de1a438593ce9 src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpInvalidPinExceptionTests.cs
43097e771b3d5e1d567d3a5b4be45b84868b7d70f3cc45572771334223796dcc src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpSessionWireTests.cs
6024d0f4fe09e66132dcb56866b64e951d94af170861e7c09aad328a6ab0fd8c src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/PrivateKeyTemplateTests.cs
```

### Unchanged executable/API token SHA256, per edited C# file

Paths use `src/OpenPgp/` as their base.

```text
d8c325afc55fec7a826a021529ad57ce635b446ef6e191b7fb94b5d78f55c049 examples/OpenPgpTool/Cli/Commands/CertificatesExportCommand.cs
ef5f6d8873ec231800558497319ad9d3b78b2f31758d7d059cf29ff5203d3cbe examples/OpenPgpTool/Cli/Commands/CertificatesImportCommand.cs
77f3faadb7bc4654e6771d0ca9470646db276600ef86af019993c97f57b47efc examples/OpenPgpTool/Cli/Commands/InfoCommand.cs
dfd283b46343a6aef58f9e26e993c17d23dc5f6b1c1e192854fda374d9749359 examples/OpenPgpTool/Cli/Commands/KeysAttestCommand.cs
1b65be7e014e42297da8ac3c302fdf7deb642872ccc23b5b48eb85538b8bd0ef examples/OpenPgpTool/Cli/Commands/KeysGenerateCommand.cs
878fc254805f772034bf81f3ce8e78590e4a867f69223b652fa841f1aad9a0bc examples/OpenPgpTool/Cli/Commands/KeysImportCommand.cs
4fa36854b2f506d5a18de804351073e3524a20d2daa8b18ba571333509db0ced examples/OpenPgpTool/Cli/Commands/KeysSetTouchCommand.cs
e640bcb1d65dc2ad5edb318cbf023752ead1860195d08e96c8e5dded33201a82 examples/OpenPgpTool/Cli/Commands/OpenPgpCommand.cs
4b760cef7f855371e7c09252c74a8f521787932266fa2c710214446875e77cc2 examples/OpenPgpTool/Cli/Commands/ResetCommand.cs
7bc7515b48e069840eb795b7cd8befd98bcad3a07e7e64df263f4d62afa8ef58 examples/OpenPgpTool/Cli/Prompts/OpenPgpDeviceSelector.cs
6b2639dd2e50353049d1eb25752c3a5ac6ea684fc448f544750aaaa2f7f07e3e src/AlgorithmAttributes.cs
fcba597ea32f26dc539a51e70b3303e2887576ab1e4cfb7419c84df01c1e16fd src/ApplicationRelatedData.cs
9cc9e731a47b1c68f86b5b1b8768f3710205d8fad58780f79b1f2d5adb9de9cd src/Credentials/OpenPgpCredentialOptions.cs
6990c57e29f2c0e9ddf05fc2fca3dc5f693f38d619d1f6b5c139a963040b6c0a src/Crt.cs
b589dc266d2ee1cd4064d74dac856484e0aa1deac4a73cbe0a72e1bfeb761985 src/CurveOid.cs
43aa3039e6fcfe234375d54fb979b0cb7f16a0ee104f97bbb72edc2d0152edf8 src/DataObject.cs
d5173f29444800ae401fe04713a90f6436f9afd0f71806e28d4a76bda7883f22 src/DiscretionaryDataObjects.cs
95dd635ca7304c9c10481872b914952317df78f0681630ee64604ff2df350023 src/ExtendedCapabilities.cs
90cb3be471942777f866b2d141ba81bdced1f9e766b298dd5cee4edeb7e4ec7b src/IOpenPgpSession.cs
e4174d6f4c0efe807067b032076ae6542c0d17d422e78c136640c31c9fae6d7b src/Kdf.cs
e3ac6f3e4c443f8ba3c41a54745a0bc14505f7aa05c9253d68117c2437dca579 src/KeyRef.cs
5dd45dfcc5912056a0819316aadf33e33d63649bb7c3a77fe91fce6d9875e8c6 src/OpenPgpAid.cs
e93d6fdb45f2f5d8ac06066ef9a95daea502df3470b4103c16e8ad7a752be9e8 src/OpenPgpInvalidPinException.cs
1a924811cd6a45f7260def3ffb27b31ee8664fc7e380cdab17b17677e985c3b4 src/OpenPgpSession.Config.cs
ddc8a1def51d624693f81e3b6d223ab30a843b4656589febbe1e12b8c1cbcdd5 src/OpenPgpSession.Crypto.cs
f9100b7372b42bf79788b0223de8d2cac0adf7bb7a5cb3b8024f7b7868ec4f05 src/OpenPgpSession.Keys.cs
27f5ba9e9dfad1fc914bdd1d8ee81651b43f3f9f3bc3a30bb2ff57fcba04ca71 src/OpenPgpSession.Pin.cs
92ffe1173a66c9c2eca2a202d3e50bafa4d270d1ee623afd5d1238017f763b16 src/OpenPgpSession.Reset.cs
f118fd3507b6c29d0159ddb66a3ce942d9f4a9955adf9925d0592f4ea56e08f4 src/OpenPgpSession.cs
06da93e1ad9c1da9b53527427c9e3a195173da9854c821bdafd037f73a0ccd01 src/PinPolicy.cs
b3e2f3d1dbcd3e2a36acb762c00937b2c21f0d6d4d9bf662a223be4c23248b28 src/PrivateKeyImport.cs
7bcab7bdca80b5287fc8bb9f6c2a6dbf84d81f29efca3015b9b682d6f92a9ca3 src/PrivateKeyTemplate.cs
a746a285e9057aba8026724413c8f9fcac5b7ec77a496fb77773f3b252cf927f src/Pw.cs
2682a68ae0ab34b7e5424f709141eccbc3fa4704330d752424c000a7bfcedffe src/PwStatus.cs
ec79469bf9a60ff372a3a91479ff2be8db3f864d78fac704291e2c307593c2f8 src/RsaSize.cs
5629b32048073b195e6b803a41364a813898938fcefd73fd1b47e2b0fced3ba2 src/SecuritySupportTemplate.cs
6ca5373d27de367f456c3bcd75b58bb39b8080b15073f1982ac8e3a212bf2a23 src/Uif.cs
b6a10c07314a3becbe81a08c4bb43d8fca88bd63a6ab2e927ea61dd91f815fd0 tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpAdvancedTests.cs
7c20a0cb6d175d7bcad6a04e37c86c81971447f5f0cb0b11d696f6cc0bda23a2 tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpDecryptTests.cs
522c00587d2715f3e1515501d73133f6f221d322c3a3ab957267662147f9fdb4 tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpKeyImportTests.cs
16aed89890170b829dc687ee434edd2ff699812e8bde04997caead912aab8f30 tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpPinManagementTests.cs
18e5c3d19747d975daa37f7ed00e8526be90a1aa4235a738844715c69d353b07 tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpSessionTests.cs
f0c321f433b737d8971b8cf9de4ca8cfcb22c82970930a880d1f38654c1903d7 tests/Yubico.YubiKit.OpenPgp.IntegrationTests/TestExtensions/OpenPgpTestStateExtensions.cs
2a6e52a127bb1efeb47de73b471c73b570b7cd070d6561ab0dff6339813dbd5c tests/Yubico.YubiKit.OpenPgp.UnitTests/AlgorithmAttributesTests.cs
df0a95b8bd1e70d59b75746cfe6e2bf6d81afe0e4c2b509f3488fa83e76d1b46 tests/Yubico.YubiKit.OpenPgp.UnitTests/AuditV2/OpenPgpAuditReproTests.cs
e2300e556f1e8da26f2e5b454f8cf6d226689499ca813959bc9c624cf6069184 tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpInvalidPinExceptionTests.cs
eec2698b463d7d05812d1543d3cc429995af3dc066e780093491760eee2cc6b6 tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpSessionWireTests.cs
09fccadc7d2b73ad27921a9a4cb8826734a2c02bb05cd9b4695ce04bcca295d4 tests/Yubico.YubiKit.OpenPgp.UnitTests/PrivateKeyTemplateTests.cs
```

## Clean-unit overlap reconciliation

Remove these whole-file clean nominees from the final clean denominator because edited units overlap:
`PGP-FILE-CLEAN-036` ExtendedCapabilities, `047` KeyRef, `058` PinPolicy and `066` RsaSize. Remove partial
`PGP-CLEAN-007` README metadata task, `008` root package row and `014` certificate contract. Of the original 40
deduplicated clean units, that leaves **33 before any independent-review/sampling corrections**. Conservatively
remove `PGP-CLEAN-005` from the optional nominee list as well: ordinary mappings are unchanged, but GetCrt's
new borrowed/ATT remark overlaps its broader bounded range. This adds no second denominator removal because
the whole-file KeyRef unit was already removed. Other clean subunit/file duplicates remain deduplicated.
`PACKAGE_README.md` remains a valid clean candidate; C017's project Description is a distinct edited unit.

PinPolicy.Once's old host-session-only wording was an additional clean-file miss discovered while editing.
Its correction is mapped to C018; the coordinator should expand API-category sampling as required by workflow.
The source threshold wording in RsaSize and missing byte units in ExtendedCapabilities were clarity refinements;
KeyRef's additions expose the known C013 qualification locally. No clean-sample acceptance is claimed here.

## Remaining acceptance/reopening triggers

- Independent full-unit content/style review and original-citation re-review, including the complete KDF example,
  reset/ATT fixed policy, ROCA bounds, raw crypto formats, parse policies and actual test assertions.
- Compile all twelve complete documentation groups and all 19 sample sources with actual references/analyzers;
  retain actual compiler inputs/output. Lexical/XML checks above are insufficient to establish compilation.
- Coordinator-owned final formatter/token proof, API/solution/unit/docs-package/architecture/complexity gates.
  Refresh raw hashes/snippet identities after any reviewer/formatter correction; body identities should remain equal.
- Reconcile pending hardware supplements and final cleanup without generalizing this Linux USB beta-device scope
  to NFC, older firmware, NEO, other platforms or AOT runtime.
- Preserve PGP-IMP-001/002/003/005/007/009/010/011/013/014/015/016/018/019 as deferred Pass 2. Reopen each
  dependent guide/XML/comment group when its implementation contract/evidence changes.
- Preserve PGP-CORE-001–004 for final Core-cycle credential-reader, ownership/disposal, version/protocol and
  cancellation/presence review. No Core behavior was audited or changed here.
- Preserve historical v1 OpenPGP chronology discrepancy and DOC-002 rather than treating an active-doc edit
  as historical proof. Independent sampling/publication remain coordinator work.
