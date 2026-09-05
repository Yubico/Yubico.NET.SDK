# V1 to V2 Migration Documentation Changelog

## 2026-06-30 - Initial baseline snapshot

- Created the initial migration guide, mapping seed, and automation state for branch `yubikit` at commit `e348013685d92a6a665cd0b8bd7e8b05850fddd5`.
- Recorded high-confidence package and namespace split guidance from `Yubico.YubiKey.*` and `Yubico.Core` to `Yubico.YubiKit.*` packages.
- Added manual-review guidance for device discovery, transport selection, applet session lifecycle, and raw APDU or low-level command migrations.
- Established automation expectations: PR preview comments for pull requests targeting `yubikit`, post-merge documentation PRs for pushes to `yubikit`, and weekly reconciliation.

## 2026-07-03 - Post-merge update through commit 5a82db9b

- Analyzed range `e348013685d92a6a665cd0b8bd7e8b05850fddd5..5a82db9bce05addc0385162e9f085adbc2366c5b` (479 commits, 1388 changed files).
- Added an assisted, high-confidence mapping (`session-creation-extension-methods`) documenting that every v2 applet package now exposes an `IYubiKey.Create{Applet}SessionAsync(...)` extension method as the standard v2 session-construction entry point, and referenced it from the Session Lifecycle section of the migration guide.
- The bulk of the analyzed range is v2-internal: device discovery/composite-device internals, APDU/CTAPHID protocol fixes, WebAuthn Swift/Rust exploration and documentation, previewSign/ARKG experimental API notes, and documentation/CI automation tooling. None of this required new v1-to-v2 mapping guidance beyond the session-construction update above; existing manual-review guidance for device discovery, transport selection, and security-sensitive flows still applies.
- Advanced `docs/migration/.state.yml` `last_analyzed_commit` to `5a82db9bce05addc0385162e9f085adbc2366c5b`.

## 2026-07-03 - Lightweight recipe examples

- Grounded recipe guidance in the existing analyzed migration state commit `5a82db9bce05addc0385162e9f085adbc2366c5b`. This was a manual documentation-authoring change and did not advance `docs/migration/.state.yml`.
- Added a `Common Migration Recipes` section to `v1-to-v2.md` with source-backed before/after examples for device discovery, device info, applet session creation, PIV key generation, FIDO2 authenticator info, OATH credential add/calculate, and YubiOTP HMAC-SHA1 challenge-response.
- Added recipe-backed mapping entries for the concrete v1 and v2 APIs used by those examples.
- Kept mutating and security-sensitive flows marked as manual review rather than automatic migrations.

## 2026-07-06 - HID listener rescan hint migration note

- Added `hid-listener-rescan-hints` migration guidance for the change from v1 `Yubico.Core.Devices.Hid.HidDeviceListener.Arrived`/`Removed` events (`EventHandler<HidDeviceEventArgs>`) to the v2 low-level `HidDeviceListener.DeviceEvent` callback carrying `HidDeviceRescanHint` diagnostics.
- Clarified that v2 HID listener hints are rescan triggers only; public `YubiKeyManager.DeviceChanges` remains repository-diffed Added/Removed truth.

## 2026-07-28 - SCP protocol construction closure

- Added `scp-protocol-construction` migration guidance: the v2 SCP wrapper `Yubico.YubiKit.Core.Protocols.SmartCard.Scp.PcscProtocolScp` keeps a public type surface, but its constructor is now internal, so `await protocol.WithScpAsync(scpKeyParameters, cancellationToken)` is the only supported construction path.
- Recorded the reason in the migration guide rather than as a bare API note: the wrapper must adopt the exchange gate of the concrete `PcscProtocol` whose connection its SCP processor drives, otherwise plain and encrypted exchanges could interleave on the wire.
- Noted that applet callers normally reach SCP through key parameters passed to `Create{Applet}SessionAsync(...)` instead of wrapping a protocol by hand.

## 2026-07-30 - Gap-remediation restorations (PIV, OATH, YubiOTP, YubiHSM, SecurityDomain, OpenPGP)

- Analyzed range `5a82db9bce05addc0385162e9f085adbc2366c5b..e042280ed7ec03a0250745ff3ff272680e5570b9` (80 commits, 482 changed files; PR #535 "yubikit-gaps" merge). The automated diff context for this range truncated before reaching any `src/` files, so this update was grounded directly in current source (`src/Piv`, `src/Oath`, `src/YubiOtp`, `src/YubiHsm`, `src/SecurityDomain`, `src/OpenPgp`) and in `docs/plans/yubikit-gaps-remediation/ISA.md`, the in-repo remediation record for this merge, rather than the truncated `diff.patch`.
- This range closes several confirmed v1-parity gaps recorded in `docs/migration/v1-to-v2-gaps.md`. Added map entries and migration-guide notes for: PIV PIN-only management-key mode and typed CHUID/CCC/AdminData/KeyHistory data objects; OATH `IsPasswordProtected`, `AuthenticateAndRetryAsync`, and the dedicated `OathException`; YubiOTP keyboard-aware static passwords and Yubico-OTP-algorithm challenge-response; YubiHSM Auth's `HsmAuthRetryException`, `OnTouchRequired` callback, and the hardware-verified `HsmAuthCredential.Counter` to `RetriesRemaining` rename; and new dedicated exception types for SecurityDomain (`SecureChannelException`) and OpenPGP (`OpenPgpInvalidPinException`).
- Added two new Common Migration Recipes: PIV PIN-only mode enablement and OATH password-protection-check-plus-retry, each backed by a corresponding `v1-to-v2-map.yml` entry.
- U2F/CTAP1 restoration, .NET Framework/netstandard targets, legacy pre-5.0 Management mode switching, synchronous facades, and a global cross-applet `KeyCollector` remain explicitly out of scope per `docs/plans/yubikit-gaps-remediation/ISA.md`; these are unchanged manual-review/non-goals, not new gaps.
- Advanced `docs/migration/.state.yml` `last_analyzed_commit` to `e042280ed7ec03a0250745ff3ff272680e5570b9`.

## 2026-08-06 - No-impact: alpha release packaging (no-op)

- Analyzed range `e042280ed7ec03a0250745ff3ff272680e5570b9..2bf3364889cd479aa9c7a7fe83bfc80a51fdb255` (12 commits, 19 changed files; PR #546 "yubikit-alpha-release" merge plus a GitHub Pages deploy fix).
- The range is entirely v2-internal release/packaging infrastructure: the version bump from `2.0.0-preview.1`/`2.0.0-preview` to `2.0.0-alpha.2` (`Directory.Packages.props`, `AGENTS.md`, `src/Cli/YkTool/Program.cs`), the new anonymous alpha NuGet feed and its bootstrap scripts (`scripts/alpha/*`, `nuget.config`, `.github/workflows/publish-alpha-feed.yml`), packaging metadata additions (`PACKAGE_README.md`, `Directory.Build.props` `PackageReadmeFile`/`PackageReleaseNotes`), alpha disclaimers in `README.md`/`PACKAGE_README.md`, and a GitHub Pages deploy workflow fix (`.github/workflows/build.yml`, `ecadb876`).
- No public API, package-identity, namespace, or behavior changes: `api-added.txt`, `api-removed.txt`, and `public-api-candidates.txt` were empty for this range, and `package-changes.txt` showed no package/namespace-shape changes (only a version-number bump, which does not change any v1-to-v2 mapping).
- No migration guide or map updates were needed; the existing `v1-to-v2.md` and `v1-to-v2-map.yml` guidance is unaffected.
- Advanced `docs/migration/.state.yml` `last_analyzed_commit` to `2bf3364889cd479aa9c7a7fe83bfc80a51fdb255`.

## 2026-08-20 - Raw access tier internalization closes the SCP construction gap

- Analyzed range `2bf3364889cd479aa9c7a7fe83bfc80a51fdb255..9347ee29bb628d0904a968d70c9e06572d34a0d2` (133 commits, 495 changed files). `diff.patch` truncated at 250000 of 4912687 bytes for this range, so `api-added.txt`/`api-removed.txt` missed most content; findings below were grounded directly in current `src/Core` source and the individual commits that made the change, not the truncated diff.
- The bulk of the range is v2-internal: connection-ownership/contention hardening, device-identity and composite-device fixes, macOS/Windows HID transport fixes, test-infrastructure and CI/documentation-automation work, and the yubikit-session-contention and raw-access-tiers effort logs. None of that required new migration guidance.
- Corrected the `scp-protocol-construction` map entry, which had gone stale: `refactor(core): internalize protocol machinery` (931e911d) made `PcscProtocolScp`, `ISmartCardProtocol`, and `ProtocolFactory` fully internal, so the previously documented `ISmartCardProtocol.WithScpAsync(...)` path is no longer public. The entry now points to the public `RawSmartCardSession`, constructed via `IYubiKey.CreateRawSmartCardSessionAsync(scpKeyParameters, firmwareVersion, protocolConfiguration, cancellationToken)`. `v1-to-v2.md`'s "Secure Channel (SCP) Session Construction" section already documented this raw-session path (added directly by the raw-access-tiers commits `fc95114b`/`291498c1` within this same range), so no guide prose changes were needed, only the backing map entry.
- Reinforced the `session-creation-extension-methods` map entry: `refactor(piv)!: make the PivSession constructor internal` (e03d01bb) closed the one remaining gap where an applet session (`PivSession`) still had a public constructor, so `Create{Applet}SessionAsync(...)` is now the only public construction entry point across every applet package. `v1-to-v2.md`'s existing PIV recipes already showed `CreatePivSessionAsync(...)` rather than `new PivSession(...)`, so no guide prose changes were needed.
- No new Common Migration Recipes were added; both updates reused existing, precise map entries per the anti-hallucination reuse rule rather than adding new ones for the same construction pattern.
- Advanced `docs/migration/.state.yml` `last_analyzed_commit` to `9347ee29bb628d0904a968d70c9e06572d34a0d2`.

## 2026-08-20 - No migration impact

- Analyzed range `9347ee29bb628d0904a968d70c9e06572d34a0d2..HEAD`; no migration-relevant source, package, namespace, or project-shape changes were found.
- Advanced `docs/migration/.state.yml` `last_analyzed_commit` to `5d22b610f09ae99f82c1071a2550f1a221ea46d0`.

## 2026-08-21 - No migration impact

- Analyzed range `5d22b610f09ae99f82c1071a2550f1a221ea46d0..HEAD`; no migration-relevant source, package, namespace, or project-shape changes were found.
- Advanced `docs/migration/.state.yml` `last_analyzed_commit` to `2bc278899bbab87cc12793a4e9727c6e0622ad67`.

## 2026-08-24 - No migration impact

- Analyzed range `2bc278899bbab87cc12793a4e9727c6e0622ad67..HEAD`; no migration-relevant source, package, namespace, or project-shape changes were found.
- Advanced `docs/migration/.state.yml` `last_analyzed_commit` to `6608d9bc51748d9cdb771dafbae445bc8a9d82cf`.

## 2026-08-25 - No migration impact

- Analyzed range `6608d9bc51748d9cdb771dafbae445bc8a9d82cf..HEAD`; no migration-relevant source, package, namespace, or project-shape changes were found.
- Advanced `docs/migration/.state.yml` `last_analyzed_commit` to `570db241ec23534ac0b45a658b4db7522ca642b6`.

## 2026-08-31 - No migration impact

- Analyzed range `570db241ec23534ac0b45a658b4db7522ca642b6..HEAD` (30 changed files). The range is entirely v2-internal: test-seam visibility changes from `private` to `internal` (`PivCertificateProtocol.GetCertificateObjectId`, `WebAuthnClient.MapCtapStatusToWebAuthnError`, `LinuxHidDevice.ParseHidDescriptorBytes`, `LinuxHidIOReportConnection.ParseReportSizes`, `LibcHelpers.GetErrnoString(int)`), a new internal `HidReportDescriptorReader` shared HID short-item walker replacing two duplicated hand-rolled parsers, an internal `ResolveRemainingRetriesAsync` extraction in OpenPGP PIN verification (behavior unchanged, confirmed by reading the diff), a `crap.cs`/`toolchain.cs` code-complexity dev-tool addition, and new unit tests for all of the above.
- One behavior fix: `FidoHidProtocol.ReceiveResponse` now sends `CTAPHID_CANCEL` when the caller's `cancellationToken` is signaled while a keep-alive is pending, so an abandoned ceremony does not strand the authenticator on a busy channel. `SendVendorCommandAsync`'s public signature (`byte command, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default`) is unchanged, so this is not a v1-to-v2 API or mapping change.
- `api-added.txt`, `api-removed.txt`, and `package-changes.txt` showed no public API or package/namespace-shape changes for this range.
- No migration guide or map updates were needed; the existing `v1-to-v2.md` and `v1-to-v2-map.yml` guidance is unaffected.
- Advanced `docs/migration/.state.yml` `last_analyzed_commit` to `6e8cf371214436beb36755abf0c82522d481f603`.

## 2026-09-03 - Curve25519 private-value validation

- Manually added for this focused correction: Core now accepts any exactly 32-byte X25519 private value, including RFC 7748 values whose stored bytes are not pre-clamped, and preserves those bytes across PKCS#8 import and export. Scalar masking remains the responsibility of the X25519 operation.
- `Curve25519PrivateKey.CreateFromValue` now rejects incompatible key types and Curve25519 values whose length is not exactly 32 bytes with `ArgumentException`.
- Accessing `Curve25519PrivateKey.PrivateKey` after disposal now throws `ObjectDisposedException`
  instead of exposing the zeroed owned buffer.

## 2026-08-31 - YubiHSM Auth credential passwords move from `string` to `ReadOnlyMemory<byte>`

- Manual documentation update accompanying a source change; it does not advance `docs/migration/.state.yml`.
- `Yubico.YubiKit.YubiHsm` was the last shipping module whose public API accepted a secret as a `string`. Nine members on `IHsmAuthSession`/`HsmAuthSession` (11 parameters in total) now take UTF-8 `ReadOnlyMemory<byte>` instead: `PutCredentialSymmetricAsync`, `PutCredentialDerivedAsync`, `CalculateSessionKeysSymmetricAsync`, `CalculateSessionKeysAsymmetricAsync`, `GetChallengeAsync`, `PutCredentialAsymmetricAsync`, `GenerateCredentialAsymmetricAsync`, `ChangeCredentialPasswordAsync`, and `ChangeCredentialPasswordAdminAsync`. Parameters were renamed with the `...Utf8` suffix already used by Fido2/OpenPgp/Oath.
- This finishes the sweep started by `75353fd1` ("security(fido2,openpgp,oath): replace string PIN/password APIs with `ReadOnlyMemory<byte>`", 2026-04-09), which did not reach `src/YubiHsm/` because that module had landed seven days earlier. The rationale is unchanged: .NET strings are immutable and cannot be securely wiped, so callers could not zero credential passwords after use.
- **This closes a v1 regression, it does not introduce one.** The legacy .NET SDK already exposed `ReadOnlyMemory<byte> credentialPassword` here (`YubiHsmAuthSession.Symmetric.cs`, `Aes128CredentialWithSecrets.cs`), so v2's `string` surface was strictly worse than v1 on this point. See the corresponding note in `v1-to-v2-gaps.md`.
- **Breaking, with no `[Obsolete]` overloads.** Overloads were rejected because `TreatWarningsAsErrors=true` means every in-repo `CS0618` fails the build anyway, and because `GetChallengeAsync`'s password parameter is optional — a byte overload with a default value would make existing `GetChallengeAsync(label)` calls ambiguous (`CS0121`). The package is `2.0.0-alpha.2` and documented as subject to breaking change.
- **Behavior is unchanged.** Credential passwords are still validated at *at most* 16 UTF-8 bytes and null-padded to exactly 16 before transmission, matching the retired `ParseCredentialPassword(string)` and the Python canonical SDK's `str` path. PBKDF2-HMAC-SHA256 / salt `"Yubico"` / 10,000 iterations / 32 bytes is untouched; the byte-span `Rfc2898DeriveBytes.Pbkdf2` overload was already what bound.
- **Migration**: pass `Encoding.UTF8.GetBytes(password)` and zero the resulting array in a `finally`, or use an owning buffer type that zeros on disposal.
- Session-key context lengths are now validated before device I/O. Symmetric callers pass the 8-byte host challenge followed by the actual 8-byte HSM challenge from the connector. Asymmetric callers pass the 65-byte EPK-OCE followed by the connector's 65-byte EPK-SD.
- Corrected the `GetChallengeAsync` firmware boundary: firmware 5.6.0 supports the command without a credential password, while firmware 5.7.1 adds the optional password field. `FeatureGetChallengeWithPassword` names that capability; the former `FeatureGetChallengeNoPassword` field remains as an obsolete compatibility alias.

## 2026-09-04 - Post-merge update through commit c3b7e206 (WebAuthn credential prompt, dependency-free device events, HID interface type, YubiHSM map backing)

- Analyzed range `6e8cf371214436beb36755abf0c82522d481f603..c3b7e206fd900413cf0a64b0efa75badbe71fd49` (~100 commits, 181 changed files). `diff.patch` truncated at 250000 of 1153663 bytes; `api-added.txt`/`public-api-candidates.txt` were dominated by the unrelated `crap.cs` coverage-report dev tool added in this range, so findings below were grounded directly in the individual commits and current source rather than the truncated automated diff.
- Several changes in this range were already documented directly in this changelog and in `v1-to-v2.md` by the commits that made them - YubiHSM Auth credential-password bytes, the `GetChallengeAsync` firmware boundary correction, session-key context validation (all three above), the Curve25519 private-value entry above, and the HID Listener Callbacks `IObservable`/Rx clarification (`9ba0f45e`) - without advancing `last_analyzed_commit`. This update adds the corresponding `v1-to-v2-map.yml` entries so that guidance has structured, source-backed evidence instead of prose only, and folds in the remaining migration-relevant changes found in the range.
- Added `webauthn-credential-prompt`: v2 introduces `Yubico.YubiKit.WebAuthn` (`feat(credentials): add ICredentialPrompt and adopt it in WebAuthn`, `a614d707`), a new package with no v1 equivalent, whose `WebAuthnClient` accepts an optional `ICredentialPrompt` (`Yubico.YubiKit.Core.Credentials`) that supplies a PIN on demand and owns a bounded (`MaxPromptAttempts = 3`) retry loop, replacing the global, unbounded `Fido2Session.KeyCollector` pattern v1 FIDO2 code used for interactive PIN retry. Added a `### WebAuthn` subsection to `v1-to-v2.md`'s Application Sections referencing this entry.
- Added `hid-usage-page-to-interface-type`: v1's `IHidDevice.UsagePage` (`HidUsagePage`: `Unknown`/`Fido`/`Keyboard`, with `Keyboard` actually meaning the Generic Desktop usage page) maps to v2's `IHidDevice.InterfaceType` (`HidInterfaceType`: `Unknown`/`Fido`/`Otp`), which classifies from the full UsagePage+Usage pair via `HidInterfaceClassifier`. V2 briefly carried an obsolete, same-shaped `HidUsagePage` as a near-direct v1 port; `refactor(core)!: remove the three obsolete public members` (`0e9eb18e`) deleted it in this range because it had zero callers, removing what would otherwise be a same-named decoy for a migrator searching v2 source for `HidUsagePage`. Added a `### HID Interface Type Classification` subsection to `v1-to-v2.md`.
- Added `yubihsm-credential-password-bytes`, backing the already-written 2026-08-31/2026-09-03/2026-09-04 changelog prose above about YubiHSM Auth credential passwords moving from `string` to UTF-8 `ReadOnlyMemory<byte>`, the `GetChallengeAsync` firmware boundary correction, and session-key context-length validation, so the guidance has a structured map entry rather than prose only. Appended a referencing sentence to the YubiHSM section of `v1-to-v2.md`.
- Strengthened `hid-listener-rescan-hints`: added evidence for the new internal `DeviceEventBroadcaster`/`DeviceEventStream` split and the `YubiKeyManager.WatchAsync` addition (`refactor(core): remove System.Reactive dependency`, `feat(core): add WatchAsync async-enumerable device event stream`) that back `v1-to-v2.md`'s existing Rx/`IObservable` clarification sentence, and noted the new `WatchAsync` entry point in the guide text. Bumped `last_reviewed_commit`.
- No-impact items confirmed in this range that need no guide or map changes: Native AOT compatibility enablement across SDK libraries (PR #578), a new v2-only capability with no v1 point of comparison; TLV output-ownership (`9a9d5ef6`), SCP11 TLV disposal (`97aa86a1`), and HID long-item parsing (`999517ab`) bugfixes with unchanged public signatures; and CLI/`Cli.Shared` prompt-and-credential-input hardening (`PinPrompt`, `SecureCredential`, `a68a3cb9` and related), which is example tooling, not an SDK package covered by this guide.
- `api-removed.txt`, `package-changes.txt`, and `namespace-changes.txt` showed no public SDK API removals or package/namespace-shape changes for this range.
- Advanced `docs/migration/.state.yml` `last_analyzed_commit` to `c3b7e206fd900413cf0a64b0efa75badbe71fd49`.

## 2026-09-04 - Device serial number/correlation and FIDO2 raw response envelopes

- Analyzed range `c3b7e206fd900413cf0a64b0efa75badbe71fd49..7b740667da8b2f8c0a85d5916e938da9c67afda4` (29 commits, 80 changed files). `diff.patch` truncated at 250000 of 698008 bytes on a large architecture-doc SVG partway through the diff, so `api-added.txt`/`api-removed.txt`/`public-api-candidates.txt` were empty; findings below were grounded directly in `git diff` against the current `src/Core` and `src/Fido2` source for the individual files, plus the commits that made each change.
- Added `device-serial-number-property`: `feat(core): expose SerialNumber and tri-state SameDeviceAs on IYubiKey` (`455b2b82`) added `IYubiKey.SerialNumber` (`int?`) and `IYubiKey.SameDeviceAs(IYubiKey)` (returning the new three-valued `DeviceCorrelation`). Unlike v1's synchronous, discovery-time-populated `IYubiKeyDeviceInfo.SerialNumber`, v2's property is populated by a background discovery read and can stay `null` well after `FindAllAsync` returns. Added a `### Device Identity: Serial Number and Correlation` subsection to `v1-to-v2.md`.
- Added `fido2-raw-response-envelopes`: `feat(fido2): expose safe raw responses` (`9f221bc4`) added a `RawData` property to `AuthenticatorInfo`, `FingerprintSensorInfo`, `EnrollmentSampleResult`, `CredentialMetadata`, and `RelyingPartyInfo`, preserving each response's complete original CBOR. This is a new v2-only forward-compatibility escape hatch (see the root `CLAUDE.md` "Forward compatibility doctrine" added in the same range by `fix(fido2): preserve forward compatibility`, `01eff955`) rather than a restored v1 feature, though v1's separate `CredentialManagementData.RawData` covered similar ground at coarser granularity. Appended a paragraph to the FIDO2 section of `v1-to-v2.md`.
- No-impact items confirmed in this range that need no guide or map changes: the flat device-model refactor replacing the internal `CompositeYubiKey`/`HidYubiKey`/`PcscYubiKey` types with an internal `YubiKeyDevice` plus `IYubiKeyConnectionSlot`/`PcscConnectionSlot`/`HidConnectionSlot` (`cdb1da3e`, `dbd02805`, `3f26baa7`, and related device-identity-contract commits) - all internal types, no public signature change beyond the `IYubiKey` members captured above; a serial-substitution refinement to `YubiKeyDeviceRepository`'s republish (Removed+Added) logic, already covered by the existing manual-review guidance for device discovery call sites; a `CoseKey.Decode` robustness fix (`fix(fido2): preserve forward compatibility` `01eff955`, `refactor(fido2): simplify COSE key decoding` `48d49523`) that stops throwing on unrecognized or non-`int32`-representable COSE key parameter labels/values instead of changing any public signature; and the public `IYubiKeyFactory`/`YubiKeyFactory` types being removed and `FindYubiKeys`'s constructor becoming `internal` (`cdb1da3e`) - `FindYubiKeys.Create()` (the only construction path the guide's `YubiKeyManager.FindAllAsync` recipe relies on transitively) is unaffected, and neither type was ever referenced by `v1-to-v2-map.yml`.
- `api-added.txt`, `api-removed.txt`, `package-changes.txt`, and `namespace-changes.txt` were empty due to the truncation noted above rather than confirming no changes; the manual review above substitutes for them.
- Advanced `docs/migration/.state.yml` `last_analyzed_commit` to `7b740667da8b2f8c0a85d5916e938da9c67afda4`.
