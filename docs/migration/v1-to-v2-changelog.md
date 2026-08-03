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
