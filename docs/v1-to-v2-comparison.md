# V1 to V2 Comparison

Last updated: 2026-08-31

This document exists to answer one question precisely: for each difference
between v1 and v2, is it a deliberate design decision, a deferred/undecided
question, something decided against, a genuine open gap, or something
already restored?

It is a manually curated synthesis, built for developers and stakeholders
who need the "why," not a replacement for the automated migration lane.

**For code-level before/after migration recipes**, use
[`docs/migration/v1-to-v2.md`](migration/v1-to-v2.md) — that document is
maintained by automation on every merge to `yubikit` and is the canonical
API mapping reference. This document does not duplicate its recipes.

**Source material**: this synthesis is built from
`docs/migration/v1-to-v2-gaps.md` (a point-in-time gap analysis dated
2026-07-21), `docs/migration/v1-to-v2-changelog.md` (automated updates
through 2026-08-24), and direct verification against current source. Per the
governance in `docs/live-documentation-governance.md`, `docs/migration/**` is
an automation-owned lane; this document lives outside it and should be
refreshed manually rather than assumed to track automatically.

## Design Decisions (Not Regressions)

These are intentional. Framing them as bugs in developer-facing
communication would be inaccurate and would undersell the reasoning behind
the breaking changes.

| V1 capability | V2 status | Why |
|---|---|---|
| `TlvReader`/`TlvWriter` typed, sequential TLV parsing | Replaced by a thinner `Tlv`/`TlvHelper`/`DisposableTlvList` surface — tag/value containers and static helpers only | V1's public typed TLV API was a broad extensibility surface; every change to it was a breaking change |
| `Base16`/`Base32`/`Bcd`/`ModHex` standalone codecs | No longer public | Same reasoning — general-purpose public utilities constrained internal iteration |
| Pluggable crypto primitives (`IAesGcmPrimitives`, `IEcdhPrimitives`, `ICmacPrimitives`) | No longer public extension points | Same reasoning; AES-CMAC is now hardcoded internal to SCP code |

Net effect: v1 shipped a very large public surface, and nearly everything in
it was a compatibility promise the team had to keep. V2 intentionally keeps
low-level primitives internal so the SDK can keep evolving without repeating
that pattern.

## Deferred / Under Evaluation

Not decided either way yet. Do not commit to a date or an outcome for these
in external communication.

- **Unified credential-collection pattern**: v1's `KeyCollector` delegate
  (one callback shape shared across PIV/FIDO2/OATH/U2F/YubiHSM Auth) has no
  v2 replacement. Each applet currently handles PIN/PUK/touch collection with
  its own bespoke shape. Whether v2 gets a unified pattern is under
  evaluation.

## Decided Against

- **Meta-package**: v1 was effectively one "install everything" package.
  V2 will not add an equivalent bundling package. Compartmentalization —
  installing only the applets an application uses — is the only supported
  install path, by design, to preserve the smaller-footprint benefit.

## Still Open

These have not been addressed and are not currently planned as design
decisions — they are real functionality differences a migrating developer
will hit.

| Gap | Impact |
|---|---|
| No .NET Framework / netstandard support — v2 targets `net10.0` only | Blocker for any consumer on .NET Framework, netstandard2.0 library authors, or older .NET (Core 3.1/5/6/8) until their host app is on .NET 10 |
| U2F/CTAP1 protocol removed entirely — no `U2fSession` equivalent | Blocker for U2F-only relying parties or non-CTAP2 browsers/servers |
| Logging silent by default | `YubiKitLogging.LoggerFactory` defaults to `NullLoggerFactory`; v1 auto-configured console logging at Error level. Apps that don't call `YubiKitLogging.Configure(...)` silently lose diagnostics they got for free in v1 |
| Exception hierarchy reduced (10 v1 types → 8 v2 types) | No v2 equivalent for `TlvException` or `KeyboardConnectionException`; consumers catching specific v1 exception types fall back to broader catches |
| Legacy pre-firmware-5 mode switching removed from the public Management surface | YubiKey NEO / YubiKey 4 (pre-5.0 firmware) users cannot reconfigure enabled USB interfaces through the public API |

Lower-severity items (MSROOTS support, NDEF read-back, `FromStaticKeys`
convenience factories, and similar cosmetic/niche API removals) are tracked
in the full point-in-time detail in `docs/migration/v1-to-v2-gaps.md` and are
not repeated here.

## Restored Since the 2026-07-21 Gap Analysis

The gap analysis above already reflects the current state — these items were
found missing on 2026-07-21 and have since been closed, per
`docs/migration/v1-to-v2-changelog.md` (2026-07-30 entry):

- PIV PIN-only (`PinProtected`) management-key mode, plus typed
  CHUID/CCC/AdminData/KeyHistory data objects.
- OATH `IsPasswordProtected` and `AuthenticateAndRetryAsync`, plus a
  dedicated `OathException`.
- YubiHSM Auth's `HsmAuthRetryException`, `OnTouchRequired` callback, and the
  hardware-verified `Counter` → `RetriesRemaining` rename.
- YubiOTP keyboard-layout-aware static passwords and Yubico-OTP-algorithm
  challenge-response.
- Dedicated exception types for SecurityDomain (`SecureChannelException`) and
  OpenPGP (`OpenPgpInvalidPinException`).

Note: v1's PIN-*derived* management-key mode (as opposed to PIN-*protected*)
remains deprecated and cannot be newly enabled in v2, though v2 can still
detect and recover an existing PIN-derived configuration.

## New in V2 (No V1 Comparison Applies)

These aren't v1-to-v2 differences at all — v1 never had them:

- **WebAuthn** (`Yubico.YubiKit.WebAuthn`) — a new client package for the
  FIDO ceremony.
- **OpenPGP** (`Yubico.YubiKit.OpenPgp`) — new applet support.

See [`docs/v2-highlights.md`](v2-highlights.md) for the developer-outreach
framing of both.

## Verified Strong/Full Parity (No Action Needed)

Per the 2026-07-21 gap analysis, these areas were verified as full parity or
improvements, not gaps: device discovery/hot-plug, HID/CCID/NFC transports,
Windows/macOS/Linux platform interop, PIV key management and algorithms
(including new Ed25519/X25519 support and touch-notification callbacks),
FIDO2 CTAP2 surface (GetInfo, PIN protocols, bio enrollment, credential
management, config, largeBlob, extensions), SCP03/SCP11 core protocol and key
management, Management device-info read/device-config write, OATH credential
types and PBKDF2 handling, and YubiHSM Auth's new capabilities (password
change, on-device EC key generation, derived credentials, zeroizing
`SessionKeys`).
