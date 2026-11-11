# A original-source notes

Access date: 2026-11-11. Exact retained bytes and hashes: `captures.json`.

## Vendor response semantics

**YubiKey 5 Series Configuration Reference Guide**, unversioned manufacturer guide at
`https://developers.yubico.com/yubikey-manager/Config_Reference.html`.

- **Concepts → Form Factor:** a manufacturing-set one-byte value; USB-A keychain Standard 01,
  Security Key 41 and FIPS 81 for firmware 5.4+. This establishes raw codes, not a required C# enum design.
- **Concepts → Auto-Eject Timeout:** inactivity in seconds; meaningful with MODE_FLAG_EJECT.
- **Challenge-Response Timeout:** seconds, default 15. The read-side absence default in C# is empty
  memory, which is not an observation that the hardware timeout is zero.
- **Device Flags:** 80 pure-CCID touch eject/inject, 40 remote wakeup.
- **Reading Information → Device Information:** Management instruction 1d, OTP slot 13, FIDO command c2;
  one-byte total length, TLV triplets, serial four bytes big-endian, firmware three bytes,
  auto-eject two bytes big-endian, configuration locked 1 or 0.
- **List of Capabilities:** PIV 0010, FIDO2 0200 and other ordinary application bits. The page lists
  0100 unused, so it is not current authority for HSMAUTH, FIPS 14/15, qualifier 19, or component versions.

Actor/layer: device response encoding and configuration software. This is descriptive manufacturer
reference, with no identified RFC-style MUST/SHOULD profile. Its advertised family scope is YubiKey 5+
and Security Key; it separately describes compatibility with YubiKey 4 capabilities. Do not extrapolate
every modern tag or legacy-firmware support from it. Its NFC write section contains a known stale tag
entry; this packet makes no write-side finding or applet edit from that material.

**YubiKey Technical Manual → YubiKey 5 FIPS Series Features → FIPS Form Factors**, unversioned
manufacturer manual, `https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-fips-specifics.html`.
It says the form factor is set during manufacturing and returned in one byte, and lists 81..85 for
firmware 5.4+. This corroborates C005. Other certification/deployment sections are not used to infer
the metadata FIPS-mask wire layout or certify an SDK operation.

**Bounded research gap:** initial original-source searches and a focused follow-up did not locate a
versioned manufacturer definition of tags 14/15 with mapping/widths, or future-width capability rules.
Two guessed configuration-reference URLs returned 404 (technical-manual `yk5-config-reference.html`
and YESDK `application-management/configuration-reference.html`). The retained actual vendor guide
does not define those newer tags. Resolve the specific gap with firmware/vendor source or qualified
vectors; further broad standards collection is not requested.

## Python comparator reused from C

Shared authority: `../../../sources/python/manifest.json` from this directory's run context (use the
run-root `sources/python/manifest.json` path to resolve unambiguously). Repository source:
`/home/rupicola/Code/y/yubikey-manager`, commit
`4ca60f706af930459138d8dc0f0f953480e1c7a4`, package metadata 5.9.3-dev.0.
`yubikit/management.py` SHA-256:
`7cc29f5803f36c945ec613849c65ee9ea83f7d4e260880ebab7096a4dcbe3192`.

Read passages: CAPABILITY and `_from_fips` lines 65–146; FORM_FACTOR / `from_code` 149–185;
DeviceInfo class and complete `parse_tlvs` 295–418. `_from_fips` maps bits 0..4 to FIDO2/PIV/OPENPGP/OATH/
HSMAUTH; both FIPS fields invoke it after `bytes2int`. `from_code` masks `0xF`. Ordinary capability
integers consume all bytes. Reported version TLV takes precedence over default, then a non-final qualifier
changes behavioral version. C#'s supplied-default precedence differs and must be described as policy.
These are pinned sibling implementation facts; no import, tests, native calls, or duplicate repo fetch ran.
No earliest firmware support or conformance claim follows from comparator agreement.

The complete `bytes2int` helper in `yubikit/core/__init__.py:269–270` was also read:
`int.from_bytes(data, "big")`. Its shared-manifest SHA-256 is
`524305273499d3a0fc37ff58c207a180cf3d280490bb48c4596507ed8a56061c`.

## .NET Channels

Context7 resolved `/dotnet/docs` and queried bounded-channel Wait/TryWrite, completion drain, and
asynchronous continuations. Its returned original documentation is
`https://github.com/dotnet/docs/blob/main/docs/core/extensions/channels.md` (unversioned orientation).

Consequential implementation is retained from `dotnet/runtime` tag **v10.0.0**, file
`src/libraries/System.Threading.Channels/src/System/Threading/Channels/BoundedChannel.cs`.
Complete `TryComplete` 305–361 and relevant `TryWrite` branches 363–442 were read. Completed writing
rejects later writes; full Wait mode returns false immediately; full DropWrite discards the item and
returns true; completion waits for buffered items before the channel completion task finishes.
These are runtime implementation passages, not normative device requirements. Core tests remain the
oracle for its policy of terminating only the overflowing subscriber. No SDK runtime experiment ran.

## Windows native API contracts

Context7 resolved `/microsoftdocs/windows-driver-docs` and queried ContainerId grouping and GUID_NULL.
Retained Microsoft reference originals:

- **DEVPKEY_Device_ContainerId**, last-updated content 2023-02-28, Windows 7+ property, Remarks:
  PnP groups devnodes into a container representing one physical-device instance; matching property GUIDs
  identify that container. Actor: PnP and applications querying device properties. Descriptive API
  contract, not universal hardware attestation or this SDK's executed matching evidence.
- **SCardGetReaderDeviceInstanceIdW (winscard.h)**, last-updated content 2024-11-20,
  Parameters/Return value/Remarks/Requirements: null output buffer returns required character count
  including terminator with success; hContext comes from SCardEstablishContext; Windows 8 desktop /
  Server 2012 minimum; Remote Desktop redirection returns SCARD_E_READER_UNAVAILABLE. Actor: WinSCard
  caller; these exact clauses support the probe, platform scope, and actionable fallback explanation.

The API references give no completion-time guarantee for these synchronous calls. Scripted resolver
tests bypass the actual P/Invoke functions and therefore cannot qualify Windows marshalling or real
CCID/HID ContainerId equality. No Windows execution or other-agent branch source-content read occurred.

## Bounded comparator and test-runner gaps

The narrative's old Rust comparator names only `yubikey-manager-rust-auto` / `7d7a7455`; no verified
repository URL/full revision was supplied by that passage. Initial local-name discovery did not identify
it, and focused access to the named local `crates/yubikit/src/platform/device.rs` path failed because
that reference directory was absent. Preserve the exact source gap (C020); current public Python is a
different identified source, not a substitute verification of old Rust code.

Context7 resolved `/websites/xunit_net` and queried simple MTP filter combination, then made one focused
follow-up for same-kind OR versus cross-kind AND. Returned originals included the current
`https://xunit.net/docs/getting-started/v3/microsoft-testing-platform`, query-filter documentation,
and v3 release pages. They distinguish simple/query filters and accept multiple same-kind values,
but the returned passages did not settle repeated-switch semantics at the installed package version.
`Directory.Packages.props` pins `xunit.v3` and extensibility core to 3.0.0 and the VS adapter to 3.1.4.
C018's source-comment/translator mismatch is therefore recorded without claiming a pinned runner
experiment or universal selection outcome. The coordinator can run the proposed isolated list/translator
experiment; no runner was invoked here.
