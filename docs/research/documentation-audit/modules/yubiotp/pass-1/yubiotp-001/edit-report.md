# YubiOTP documentation edit handoff

2026-11-04 · fresh GPT-6.1 Sol editor · baseline `95e5ccaeda33954c21f2a38058d4f66b5f208787`.

## Outcome and boundaries

Implemented accepted OTP-C001–018 documentation corrections/qualifications in 50 consumer/comment paths:
42 C# comment-only files, three module Markdown files, and the five assigned shared documents. This report
is an additional run-record path. No runtime, help-string, assertion, API snapshot, project/configuration,
formatter, build, test, hardware, commit, agent/delegation, or Core/other-module investigation was performed.
No imports were changed in C# source. The pre-existing campaign README change and untracked user files are
not editor-owned. Status/coverage phase transitions remain coordinator-owned.

Read root/module/test instructions, docs-module skill, campaign workflow/style, run plan/findings/
investigation/coverage/inventory/status and focused source evidence. Checked relevant module implementation
and actual assertions before changing their explanations. Used Context7 `/dotnet/docs` for immutable strings
and memory ownership; it does not establish SDK copying behavior. No new vendor research round was started;
the forthcoming independent original-citation review remains necessary.

The guide preserves the useful HMAC input / transmitted 64-byte payload / slot-hashed-message distinction,
including repeated/trailing-zero data and encoding collisions. It does not redefine padding or imply the
session knows the stored mode. Legacy rules are qualified to Manual 3.3/tagged vendor tools, not advertised
as universal 5.x contracts. Root FIPS relocation claims were not touched; YubiOTP migration relocation stays
explicitly unverified. The unrelated existing migration-map parse defect is untouched.

## Candidate-to-change mapping

All paths below are fully identified in the hash inventory. Overlapping candidates are not counted as
independent implementation fixes. Runtime findings remain deferred as specified in findings.md.

| Candidate | Implemented correction / affected units |
|---|---|
| OTP-C001 | README replacement/update/protection task and retained-code fragment; session/base/update/extension XML; sample programming/settings/swap comments; migration recipe/map/gaps. Distinguishes current authentication from new code, full masks/defaults and protection removal. |
| OTP-C002 | Keyboard/update pacing 10/20/30 ms and fast-trigger correction; flag aliases/update-mask meanings; legacy serial/delay/keypad/firmware qualifications in XML and module guides; sample option explanation. |
| OTP-C003 | Raw binary public-ID inputs and emitted modhex characters in YubiOtpSlotConfiguration/base wire table, README mode table and integration comment. |
| OTP-C004 | Configuration copies/disposal, independent GetConfig ownership, borrowed async inputs, caller-owned calculation output and string tradeoff in XML/README/CLAUDE/migration. Full finally cleanup in task examples; explicit invalid-code preflight cleanup limitation. Sample output/parser limitations retained as defects, not erased by documentation. |
| OTP-C005 | Cached status refresh/raw OTP firmware versus effective firmware; keyboard trigger is not RequireTouch read-back. Existing SmartCard PolicyRequires heuristic/unknown silence explained in ConfigState/session/backend-adjacent comment, guides, shared interaction and migration. |
| OTP-C006 | Explicit OTP-HID USB challenge recipes without claiming SmartCard parity; ownership and complete session scopes; factory/extension docs and sample selection comments; unscoped CCID rejection folklore removed from integration comments. |
| OTP-C007 | Preserved HMAC three-layer logic; tagged manager 5.8.0 qualification, target-firmware boundary caveat, wire-test versus executed device-vector distinction; owned response cleanup and migration length-convention link. |
| OTP-C008 | AES ticket/configuration versus keyboard OTP, nondeterministic fields and verifier challenge/CRC/replay responsibilities; complete AES example; session/configuration XML and migration entries. |
| OTP-C009 | NDEF 53/51 UTF-8 byte limits, longest ordinal prefix, fixed en header, programming block versus on-tap output, existing slot/NFC prerequisites, null disable request versus empty content, no read-back or enum/URI validation; XML, guides, sample/test comments, migration gaps. |
| OTP-C010 | Enforced type/operation minimums are not complete capability checks; exact Management/OTP-major-3 version path and raw-state distinction; length/IMF/flag caveats; assigned architecture path corrected to SmartCardBackend. |
| OTP-C011 | Nonzero IMF is an incorrect implementation scale, not desired counter semantics. Recommend zero and verifier synchronization; constructor/protected helper XML, README/CLAUDE, sample IMF comment, migration gaps and current-encoding test comments. |
| OTP-C012 | 38-byte API versus vendor 45-entry map limitation, no truncation/full-map recipe and missing gate; session XML/constants comment, README/CLAUDE, migration gaps. |
| OTP-C013 | Precise SmartCard 5.0–<5.5 bypass, any-zero and short-fallback limitations; expected data prefix and HID CRC/write-status scope; malformed-status exceptions qualified; backend/test comments and module guides. |
| OTP-C014 | Test comments match actual assertions: IMF erroneous encoder, low-byte status vector, 3→5 sequence mismatch, fake firmware 5.2 scope, length/configured-only assertions, write-only NDEF, attempted swap restoration, disposal rejection versus actual reflected zeros. Removed development-history/CRC scratch narration without changing executable statements. |
| OTP-C015 | HidBackend diagram name, exact initialization, ownership and suite effects; module/test instructions distinguish future cleanup guidance from existing destructive tests. Existing valid test link preserved. |
| OTP-C016 | Prominent OtpTool UTF-8/raw-scancode/layout defect; public string/layout constructor is actionable route. Raw overload/string cleanup tradeoff preserved. Sample XML/hand-off comment, README and migration guidance corrected without behavior change. |
| OTP-C017 | Sample is demonstrative, not ykman parity. Required hex/no stdin, no TOTP generation, default transport/no prompt, full-mask settings, new-code omission/swap ignored code, JSON confirmation bypass and secret-output/parser/string limitations documented. Runtime help remains unchanged. |
| OTP-C018 | Only assigned YubiOTP migration sections reconciled: restored constructors/AES/URI compression/exact-size keys versus genuine generation/TOTP/language/UTF16/read-back gaps and unresolved relocation. V1 before block remains historical. |

## Complete example inventory and classification

Line ranges include fences in the exact hashed content below. These are the original final edited bodies,
not substitute summaries for compilation. No snippet was compiled or executed by this editor; coordinator
must compile complete wrappers with genuine project references/analyzers and retain provenance.

| Path / final lines | Classification | Compilation context, prerequisites and lifetime |
|---|---|---|
| README.md 41–44 | Shell setup recipe | .NET CLI and access to alpha feed; not executed. |
| README.md 50–71 | Complete discovery/status top-level example | .NET 10 console referencing YubiOtp; explicit System, threading/tasks, memory/crypto/text and real Core/module namespaces in block. Available device. Session ends inside braces before any later task opens one. |
| README.md 116–131 | Application-dependent update fragment | Reuse README imports; typed IYubiKey device and owned byte[] accessCode of exactly six bytes, authorized existing updatable keyboard slot 2. Intended complete masked settings explicitly chosen. New/current code both supplied; session/configuration scoped and source cleared in finally, including creation failure. |
| README.md 143–158 | Composable provisioning alternative | README imports and IYubiKey device; authorized slot 2 replacement, verifier enrollment responsibility. Generates exact 20-byte key; OTP HID explicit; using configuration, await-using session, key finally. RequireTouch makes later calculation a touch task. |
| README.md 166–181 | Application-dependent HMAC fragment | README imports/IYubiKey device; slot 2 already provisioned with agreed verifier key and short mode; touch when required. One-shot extension owns session. Owned challenge and documented owned response both cleared in finally; response consumed before erasure. |
| README.md 207–229 | Application-dependent AES provisioning/calculation alternative | README imports/IYubiKey device; authorized replacement, OTP HID availability, secure verifier enrollment supplied by application. Arrays initialized empty; both allocations inside try, session/configuration scoped, challenge/key/ticket cleared even on later allocation/session failure. No verifier implementation invented. |
| README.md 241–248 | Composable static-password alternative | README imports/IYubiKey device; authorized slot 2 replacement and host layout matching en_US. Complete resource scope. Literal demonstrates convenient immutable-string tradeoff explicitly, not deterministically erasable secret storage. |
| README.md 258–264 | Destructive alternative fragment | README imports/IYubiKey device; authorized unprotected slots and device swap permission. Complete session scope; affects both slots then deletes slot 2. Protected deletion needs current code; not an original-secret restoration recipe. |
| README.md 289–295 | Complete prompt type declaration | README System.Threading/Tasks, Core.Credentials imports; IUserPresencePrompt implementation. Type declaration must appear after top-level statements or in a separate file when assembled. |
| README.md 297–307 | Composable prompt-session fragment | README imports/IYubiKey device plus TouchPrompt type; explicit HID and complete session scope. Configuration-only sketch: add a calculation using preceding cleanup pattern; do not keep another session open. |
| README.md 377–379 | Shell sample launch | Repository root, .NET SDK and sample references; not executed. Sample limitations immediately precede it. |
| CLAUDE.md 22–28;77–79 | Non-executable diagrams | Backend hierarchy/wire layout, not C#. |
| CLAUDE.md 183–194 | Connection-dependent provisioning fragment | Typed caller-owned open IConnection connection, owned byte[] key exactly 20 bytes; System.Security.Cryptography, Core.Abstractions, YubiOtp imports. Slot authorized; session/config using scopes; source key cleared even if creation fails. Caller closes connection after session ends. |
| CLAUDE.md 202–214 | Existing-session challenge fragment | Open YubiOtpSession session with compatible provisioned slot, task-owned byte[] challenge; System/Runtime.InteropServices/Security.Cryptography/module imports. Response is documented caller-owned output, cleared with challenge in finally. Enclosing caller owns session/connection. |
| CLAUDE.md 220–223 | Existing-session cached-state fragment | Open typed YubiOtpSession session, configured-status-capable firmware and module import. Not a fresh read; enclosing caller owns session/connection. |
| migration/v1-to-v2.md 538–553 | Historical v1 before block | Not current v2 compilation or legacy-source verification target. |
| migration/v1-to-v2.md 561–598 | Application-dependent v2 recipe | Actual explicit System/Runtime.InteropServices/Security.Cryptography/Threading, Core.Abstractions/Devices/Sessions and YubiOtp imports. Typed IYubiKey device, owned byte[] hmacKey exactly 20 bytes, owned byte[] challenge 1–63 bytes, CancellationToken cancellationToken; enrolled verifier and authorized replacement. No existing connection. Session creation inside try, explicit HID, config/session scopes, all owned inputs/output cleared. |
| examples/OtpTool (14 existing C# files) | Complete executable sample | Compile with its actual project references/global imports/analyzers, not a reduced wrapper. Only 11 files' comments edited; executable/help tokens unchanged. CLI limitations remain unfixed. |

All `MemoryMarshal.AsMemory` cleanup examples are limited to documented caller-owned SDK calculation
outputs, never an arbitrary borrowed read-only buffer. Static-password strings remain an explicit convenience
tradeoff; no false promise of source-string zeroing is made.

## Verification performed / pending

- Read-only Python precheck compared comment-stripped lexical tokens against HEAD in all 42 edited C#
  files, including literal contents: equal for all 42. It also parsed every edited C# XML-comment block
  using ElementTree: zero errors. This is a preliminary check, not a substitute for coordinator Roslyn
  executable-token/directive/API-snapshot gates. The checker did not write files.
- `git diff --check -- src/YubiOtp docs/usage/user-interaction.md docs/migration/v1-to-v2.md
  docs/migration/v1-to-v2-gaps.md docs/migration/v1-to-v2-map.yml
  docs/architecture/device-discovery-guarantees.md`: exit 0, no output.
- No formatter/build/tests/hardware/commits were run. Independent content/original-citation review,
  compilation/analyzers, final Roslyn gates, local-link/package QA and coordinator sampling remain pending.
- Sampling must remove edited-overlap nominations at least OTP-CL003,005,008,014,023. Other nominations
  require range-aware reconciliation rather than excluding every otherwise-clean member of an edited
  file. In particular unchanged README ownership paragraph, test runner links, HMAC wire-capture family,
  reflected-zeroing tests, static-password intermediate tests and sample serial-ID helper were preserved.

## Hardware-sensitive wording to reopen

No device results are invented. The initial integration run was blocked before operations by external
exclusive-reader contention; it is not evidence that a YubiOTP command failed, passed or mutated state.
Coordinator owns subsequent probes and their actual device/firmware/OS/transport records.

1. SmartCard PolicyRequires remains explicitly the cached keyboard-trigger heuristic, not established
   RequireTouch certainty. Reopen ConfigState/session/backend comment, both module guides, interaction
   paragraph, migration/gaps if the status/notification matrix changes the supported interpretation.
2. Explicit OTP HID is presented as the supported USB challenge recipe pending hardware, not proof of
   all-device behavior or SmartCard command parity. Reopen both calculation contracts, recipe blocks and
   migration/selection comments after actual USB CCID/NFC/HID evidence.
3. HMAC minimum-one-byte behavior is preserved as the vector expectation, with target-firmware caveat.
   Reopen all empty/64-byte explanations if executed known-answer results disagree. Request tests alone
   do not settle device hashing.
4. AES description follows the legacy ticket protocol; no live AES KAT is claimed. Reopen ticket/verifier
   text after scoped decryption/CRC/repeated-challenge probes.
5. NDEF null is an all-zero disable request, distinct from empty content. NFC on-tap output/disable and
   old-firmware/flag applicability remain qualified; write-only/configured-bit tests are not proof.
6. Original Manual 3.3 and tagged personalization/manager references need fresh consequential-clause review.
   Modern 5.x exceptions are not inferred from legacy rules. FIPS/legacy-command relocation remains unverified.

## Exact edited-content inventory

SHA-256 and bytes at editor handoff. This report cannot contain its own final hash; coordinator should hash
it externally in the review/freeze manifest. The table excludes the coordinator-owned campaign README and
all unchanged paths. Future corrections must refresh affected hashes and example ranges.

| Path | Bytes | SHA-256 |
|---|---:|---|
| `docs/architecture/device-discovery-guarantees.md` | 25460 | `a96bc8ee80a2effd8d3c65b778e9683e5ef447ec13d2475ebc7bfe16b7e6c5f0` |
| `docs/migration/v1-to-v2-gaps.md` | 39254 | `7756ff4b87c1a44182b4e4524b00ecbf4fcb9fc3f013d23dfb6bdd455173e134` |
| `docs/migration/v1-to-v2-map.yml` | 64384 | `bb2c6af2cf957fb1f72fbe9d2c9fd8f6a740df1211f4487c4fea47392fc75055` |
| `docs/migration/v1-to-v2.md` | 55365 | `436d43e2c25c3a0238167f1f547a83adc930823aeda22c6324b42e7cd91a72b3` |
| `docs/usage/user-interaction.md` | 12958 | `6fc82b3d75b3fe35a33334b8d574986943fbab08597288afa7cc42f6533cf40b` |
| `src/YubiOtp/CLAUDE.md` | 12511 | `24c603f4339e7f410dbe9a04ae0ba817c6a8cfb289a5d445705eeda878dd2145` |
| `src/YubiOtp/README.md` | 22207 | `20f56a448dae2cbba9ef8deef5c732861c83a1711739d63238331c97744fb0ec` |
| `src/YubiOtp/examples/OtpTool/CliOptions.cs` | 19170 | `8c7a628e11fa1f2367dd9584270e8d198c9dcdbaf44f616a939cc976a37888b4` |
| `src/YubiOtp/examples/OtpTool/Commands/CalculateCommand.cs` | 2479 | `d83144628e283b34dda5db591fd742fccbc0138ba8676f133c90fff57ac52dcf` |
| `src/YubiOtp/examples/OtpTool/Commands/ChalRespCommand.cs` | 3723 | `e9eb737725715557b7012e0c9db1abcee0c167f22f60a37cf600b23b7751d4df` |
| `src/YubiOtp/examples/OtpTool/Commands/HotpCommand.cs` | 3405 | `2e8e0b9824eabf4502e1b6a93f3a747bfd395371f296648b593f0fa5dd2978b1` |
| `src/YubiOtp/examples/OtpTool/Commands/NdefCommand.cs` | 3248 | `6a3aa1848e5cbeb4af38ef07334c0437403524d4ccf167d174d6a0f00c472fc4` |
| `src/YubiOtp/examples/OtpTool/Commands/SettingsCommand.cs` | 3271 | `1c6a2ba587553406f8335c218c124f4d1c1d5db2f6403eb3fca02e81ca1cad5a` |
| `src/YubiOtp/examples/OtpTool/Commands/StaticCommand.cs` | 4560 | `03b101b5d199f8960c93947c2f14f1752cd098b9eef72b67475b9f34b1eb0afe` |
| `src/YubiOtp/examples/OtpTool/Commands/SwapCommand.cs` | 1794 | `655efc2440286e9c01355117bf77dff8a6aebcbc81447cf6e0fbb10a366646e2` |
| `src/YubiOtp/examples/OtpTool/Commands/YubiOtpCommand.cs` | 6081 | `2e91394f0ce0f6e53e394e4a32f2a1e30a93f00d1a917c824fb8eb2958115db5` |
| `src/YubiOtp/examples/OtpTool/DeviceHelper.cs` | 11667 | `431309511ddda3e3391c0eb962257db57ad50111c8dde09f3c5142aaf2c86661` |
| `src/YubiOtp/examples/OtpTool/OutputHelper.cs` | 4230 | `57e48f47712ab87f0b6dd53ac8cd9323a9ecce4ccb94c16b05f52e1acabb15d9` |
| `src/YubiOtp/src/Backend/HidBackend.cs` | 5364 | `71116ae755f93751bfb724debd4a7a9505f40a65f98615c46a864a92e44fe277` |
| `src/YubiOtp/src/Backend/SmartCardBackend.cs` | 8598 | `8ca26eba06f9edd66734f893ec7a9736eb72786aa8a20d32c24b5cc530fca109` |
| `src/YubiOtp/src/ConfigFlag.cs` | 2050 | `e130899ecaa76881dd59649b2d54e69b5ae205e4d85b12778d20872728be1b86` |
| `src/YubiOtp/src/ConfigState.cs` | 5506 | `8b267d2f7825d5e568a055c956a00a52dd2c8b9e48d5866e972d110c5a69c3a5` |
| `src/YubiOtp/src/ExtendedFlag.cs` | 1919 | `9c70b120a8c2ec18a5986db3be95a9a54553282e0dcf2d2289dc096a272907a9` |
| `src/YubiOtp/src/HmacSha1SlotConfiguration.cs` | 3459 | `85b95c757de9b5425ec5c0cc2d1f56c02aa00365788ead41273e5a1a423b02dd` |
| `src/YubiOtp/src/HotpSlotConfiguration.cs` | 4410 | `0cabb1f86817a968f4d9b7847e82f7e77f0f5e968ae38d4fad6333d09cfaf1ed` |
| `src/YubiOtp/src/IYubiKeyExtensions.cs` | 7848 | `9f6673bf6ba09f0f2c096805af9ba4b35f8b044fbe4eae0564749e860bdc0f1d` |
| `src/YubiOtp/src/IYubiOtpSession.cs` | 20930 | `2be16a3001b4956538c7d724ad87e95898e3ba6d1872ea3c8147b2a874d75905` |
| `src/YubiOtp/src/KeyboardSlotConfiguration.cs` | 5659 | `6e88fe341d4e4d445f66fc3090c211decbefaac18fcb45f96fc25ab1ddb5ebaa` |
| `src/YubiOtp/src/NdefType.cs` | 1072 | `5610839bc362f9b53c3ebdebdf5c16dba7339ed82e1a158cc03d184b071b8543` |
| `src/YubiOtp/src/SlotConfiguration.cs` | 13189 | `74b6975844a67c317eae16e06723635f79090168c909665b88262297de8b39b9` |
| `src/YubiOtp/src/StaticPasswordSlotConfiguration.cs` | 6839 | `81cd75a38707a1549c4b52352767555eb7a4ba3a208b98143d6a19bf4d32fd8b` |
| `src/YubiOtp/src/TicketFlag.cs` | 2087 | `1a763f1fcd5d36dc06627fe1e227e4c274674fa829470988d3ae69273c5f0a3c` |
| `src/YubiOtp/src/UpdateConfiguration.cs` | 5310 | `93805eb2c264ff12736502106960cd5bf536debaabaee7fc9e69657a5efbd8f0` |
| `src/YubiOtp/src/YubiOtpConstants.cs` | 1797 | `beaa9bd24ed5cdc25218162c0c7ef1182c402b866cadcc445d7e5d9c1a1c5068` |
| `src/YubiOtp/src/YubiOtpSession.cs` | 22997 | `c62b0f431411b34fe26bb28ed8725f178bd3c960b399878c8087b40f2e751468` |
| `src/YubiOtp/src/YubiOtpSlotConfiguration.cs` | 2155 | `774968a716dbdebec5f2e520f7d83585cb64203b490d7b62235dee893d4e0e07` |
| `src/YubiOtp/src/YubicoOtpChallengeResponseSlotConfiguration.cs` | 2943 | `760fa2607003834b6b7d4360a8e9bda9d1bae8896be48b1de6cb5191236b0ad0` |
| `src/YubiOtp/tests/CLAUDE.md` | 1509 | `7ff73c817886e7ada48b9b80048ee32bf3303971a5a0a413b7bdd04c7a509547` |
| `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/AuditV2/YubiOtpHmacChallengeKnownAnswerTests.cs` | 4552 | `f1b6d67378ebcf4e657d7b50f43fe65d44c5095cb53baf85971861d15b75b304` |
| `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/AuditV2/YubiOtpProtectSlot2Tests.cs` | 5823 | `ec7b36f767f026cd777e0823cba14a86449595d96fd2d3ce2926e6f4f2f67e86` |
| `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/YubiOtpSessionIntegrationTests.cs` | 5354 | `54214801a3eaeda52ee0e59fab4fa461357fd48c648afc834a864d30b11c4519` |
| `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/YubiOtpSlotConfigTests.cs` | 6952 | `24a79eac1aa7af865358611dc1134dcc651baa00ef622248d96fde12062157fc` |
| `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/AuditV2/YubiOtpAuditReproTests.cs` | 2259 | `a86a9bfd728338cd2d6f4dc33dea1ceb4286efb37eaeb430bf589c562dc13608` |
| `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/ConfigStateTests.cs` | 5216 | `1192d6d4ee69f679ceb8eb58d4932f58829f0874a8e695b1d0d10e76a80710e5` |
| `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/HidBackendTests.cs` | 20757 | `a415655ed5178f1498718504debc3559b7c4b120754cca5433a2ec0b8c7c5cdb` |
| `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/IYubiKeyExtensionsTransportTests.cs` | 9749 | `31933bac9fce6202a991cd672141a114527155400fa0420f9ddf36b7afc99347` |
| `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/SlotConfigurationTests.cs` | 36142 | `c00512b2cf27c41d9afc46b60b1f4874c3f5b2761a7eaeea58ef861a010ac3dd` |
| `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/SmartCardBackendTests.cs` | 18476 | `187c7fc18b5b276bf2888eb04a230b2481f511034ea4e6de08ab776cbe933f09` |
| `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/YubiOtpBackendLifecycleTests.cs` | 1677 | `ec58c9d652ae17981df955a5b3c3a1d7266d8972798441e468cff3394a4d4c5a` |
| `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/YubiOtpSessionTests.cs` | 28102 | `96994fe90887fb9a044f81c9667c04b01480444a3cd81220875e6c6d01c297a9` |
