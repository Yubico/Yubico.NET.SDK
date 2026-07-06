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

## 2026-07-06 - HID listener rescan hint migration note

- Added `hid-listener-rescan-hints` migration guidance for the change from v1 `Yubico.Core.Devices.Hid.HidDeviceListener.Arrived`/`Removed` events (`EventHandler<HidDeviceEventArgs>`) to the v2 low-level `HidDeviceListener.DeviceEvent` callback carrying `HidDeviceRescanHint` diagnostics.
- Clarified that v2 HID listener hints are rescan triggers only; public `YubiKeyManager.DeviceChanges` remains repository-diffed Added/Removed truth.
