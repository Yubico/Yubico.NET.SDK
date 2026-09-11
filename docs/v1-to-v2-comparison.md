# v1 to v2: what changed, and why

Last updated: 2026-09-11

V2 differs from v1 in a lot of places. This page sorts every one of those
differences into a straight answer: did we do this on purpose, are we still
thinking about it, did we decide against it, is it genuinely missing, or has
it already been fixed?

If you're weighing up a migration, this is the honest inventory.

**Looking for code?** [The migration guide](migration/v1-to-v2.md) has the
before/after recipes and is the canonical API mapping. Automation owns that
documentation lane; this page explains the reasoning without repeating the
code.

<sub>Built from the 2026-07-21 gap analysis and the automated migration
changelog and watermark through 2026-09-07 commit `277920d9`. Changes after
that through current HEAD `d04d59aa` are grounded separately in
[the applet public API decision record](architecture/applet-public-api.md) and
direct current-source verification. `docs/migration/**` is automation-owned per
[the documentation governance](live-documentation-governance.md); this page
sits outside that lane and is refreshed by hand.</sub>

## Deliberate design decisions

These aren't regressions, and it'd be misleading to describe them as such.
V1 made almost everything public, which meant almost every internal
improvement became a breaking change. V2 keeps low-level primitives internal
so the SDK can keep moving.

| V1 capability | V2 status | Why |
|---|---|---|
| `TlvReader`/`TlvWriter` typed, sequential TLV parsing | Replaced by a thinner `Tlv`/`TlvHelper`/`DisposableTlvList` surface — tag/value containers and static helpers only | V1's public typed TLV API was a broad extensibility surface; every change to it was a breaking change |
| `Base16`/`Base32`/`Bcd`/`ModHex` standalone codecs | No longer public | Same reasoning — general-purpose public utilities constrained internal iteration |
| Pluggable crypto primitives (`IAesGcmPrimitives`, `IEcdhPrimitives`, `ICmacPrimitives`) | No longer public extension points | Same reasoning; AES-CMAC is now hardcoded internal to SCP code |

If your code used any of these directly, you'll need your own replacement.
That's the cost, and we think the smaller surface is worth it.

### Device discovery, events, and ownership

- `YubiKeyManager.WatchAsync` is now the only public device-change stream,
  replacing the earlier v2 `DeviceChanges`/`System.Reactive` surface with
  `IAsyncEnumerable<DeviceEvent>`. Each watcher has an independent bounded
  buffer. Overflow faults that watcher rather than silently dropping a delta;
  resynchronise with `FindAllAsync` and start watching again.
- Discovery models a physical key as one `IYubiKey` when evidence supports
  grouping its interfaces. `SerialNumber` and tri-state `SameDeviceAs`
  (`Same`, `Different`, or `Unknown`) support correlation. Ambiguous evidence
  conservatively produces separate records rather than a guessed composite;
  see [the discovery guarantees](architecture/device-discovery-guarantees.md).
- Within a process, a grouped physical key permits one live connection across
  its known interfaces, and each connection permits one live session.
  Overlapping acquisition throws `ConnectionInUseException`; sequential reuse
  remains supported. Cross-process contention is out of scope and still
  surfaces a platform `SCardException`.

### Standard applet session creation

All eight applet sessions now use the same two factory shapes:
`IYubiKey.CreateXSessionAsync` for an owned connection and
`XSession.CreateAsync` for a borrowed connection. Both accept
`SessionCreationOptions`, which carries shared connection, secure-channel,
protocol-configuration, and firmware-version policy. Existing one-shot
conveniences also accept session options before their cancellation token, so
callers can apply the same creation policy without opening a session manually.

### Forward-compatible FIDO2 responses

Selected FIDO2 response models expose their original CBOR envelope through
`RawData`. Typed properties remain the normal path, while the raw bytes let
applications inspect valid fields introduced before the SDK models them.

## Still being decided

**How far to extend interactive prompting.** Direct credential parameters on
applet sessions are settled: calls such as `PivSession.VerifyPinAsync(pinUtf8)`
keep authentication flow under application control. One applet-local exception
exists — OATH's `AuthenticateAndRetryAsync` takes a password-provider callback
so it can re-authenticate a locked applet mid-operation. Core now provides
`ICredentialPrompt`, and `WebAuthnClient` adopts it for on-demand PIN entry
with a bounded retry loop that defaults to three attempts.

Cross-applet adoption remains open, and no unified touch-notification pattern
exists. Applet-specific touch callbacks do not amount to the single prompting
contract v1's `KeyCollector` provided.

## Decided against

**A meta-package.** V1 was effectively one "install everything" dependency.
V2 won't add an equivalent bundle. Installing only the applets you use is the
supported path, because a catch-all package would give back exactly the
footprint savings the split is there to deliver.

**Synchronous facades or wrappers.** V2 is async from top to bottom and won't
add sync-over-async entry points. Migrating callers must adopt async flows
rather than blocking on SDK operations.

## Genuinely missing

No decision behind these — they're real functionality differences you'll hit
if you migrate today.

| Gap | What it means for you |
|---|---|
| No .NET Framework / netstandard support — v2 targets `net10.0` only | A blocker if you're on .NET Framework, authoring netstandard2.0 libraries, or on older .NET (Core 3.1/5/6/8), until your host app moves to .NET 10 |
| U2F/CTAP1 protocol removed entirely — no `U2fSession` equivalent | A blocker for U2F-only relying parties, or non-CTAP2 browsers and servers |
| Logging is silent by default | `YubiKitLogging.LoggerFactory` defaults to `NullLoggerFactory`, where v1 auto-configured console logging at Error level. If you don't call `YubiKitLogging.Configure(...)`, you quietly lose diagnostics v1 gave you for free |
| Exception model changed | There is no direct `TlvException` or `KeyboardConnectionException`. V1's `Fido2Exception`/`Ctap2DataException` hierarchy collapses into the flat `CtapException`. Module-specific types now exist where appropriate, so audit each catch block instead of translating a hierarchy mechanically |
| Legacy pre-firmware-5 mode switching removed from the public Management surface | YubiKey NEO and YubiKey 4 (pre-5.0 firmware) users can't reconfigure enabled USB interfaces through the public API |

Smaller items — MSROOTS support, NDEF read-back, `FromStaticKeys` convenience
factories, and similar niche removals — are catalogued in full in
[the point-in-time gap analysis](migration/v1-to-v2-gaps.md).

## Already fixed

Flagged as missing on 2026-07-21 and shipped since — most in the 2026-07-30
changelog entry, the YubiHSM Auth password change later in the alpha series.
The gaps table above already accounts for these:

- PIV PIN-only (`PinProtected`) management-key mode, plus typed CHUID, CCC,
  AdminData, and KeyHistory data objects.
- OATH `IsPasswordProtected` and `AuthenticateAndRetryAsync`, plus a
  dedicated `OathException`.
- YubiHSM Auth's `HsmAuthRetryException`, the `OnTouchRequired` callback, and
  the hardware-verified `Counter` → `RetriesRemaining` rename.
- YubiHSM Auth password inputs are back to UTF-8 `ReadOnlyMemory<byte>`, as in
  v1, so callers can clear them after use. Its parameters are named plainly
  (`credentialPassword`, `derivationPassword`, `currentPassword`,
  `newPassword`, and `password`). FIDO2, OpenPGP, and OATH secret parameters
  were renamed the same way, which breaks named-argument call sites; PIV still
  uses the `...Utf8` suffix.
- YubiOTP keyboard-layout-aware static passwords and Yubico-OTP-algorithm
  challenge-response.
- Dedicated exception types for SecurityDomain (`SecureChannelException`) and
  OpenPGP (`OpenPgpInvalidPinException`).

One caveat: v1's PIN-*derived* management-key mode (as distinct from
PIN-*protected*) is still deprecated and can't be newly enabled in v2, though
v2 can detect and recover an existing PIN-derived configuration.

## New in v2

Not differences at all — v1 never had these:

- **WebAuthn** (`Yubico.YubiKit.WebAuthn`) — a client package that runs the
  full FIDO ceremony.
- **OpenPGP** (`Yubico.YubiKit.OpenPgp`) — OpenPGP applet support.

[What's new in v2](v2-highlights.md) covers both in more detail.

## Verified at full parity

The 2026-07-21 analysis classified these areas as parity or outright
improvements over v1. For discovery, that means the capability is present and
improved, not that every platform can always prove how interfaces group:
ambiguous evidence deliberately splits rather than guesses. The exact
platform guarantees and bounds are documented in
[device discovery guarantees](architecture/device-discovery-guarantees.md).

device discovery and hot-plug; HID, CCID, and NFC transports;
Windows/macOS/Linux platform interop; PIV key management and algorithms
(including new Ed25519 and X25519 support plus touch-notification callbacks);
the FIDO2 CTAP2 surface (GetInfo, PIN protocols, bio enrollment, credential
management, config, largeBlob, extensions); SCP03/SCP11 core protocol and key
management; Management device-info read and device-config write; OATH
credential types and PBKDF2 handling; and YubiHSM Auth's new capabilities
(password change, on-device EC key generation, derived credentials, and
zeroizing `SessionKeys`).
