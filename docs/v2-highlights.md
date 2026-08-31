# V2 Highlights

Last updated: 2026-08-31

This is the source-of-truth feature summary for developer outreach, the Early
Access program, and launch communications. It is written to be handed to
developer relations, product, and documentation stakeholders without
additional interpretation.

Every claim below is grounded in current source, merged automation records,
or explicitly named pull requests. Where a capability is not yet true today,
it is labeled as such rather than implied.

## New Applet Support

V2 adds application support that v1 never had at all. These are not rewrites
of existing v1 capability — they are new YubiKey applications the SDK did not
previously speak to.

### WebAuthn (primary highlight)

`Yubico.YubiKit.WebAuthn` is a client package that encapsulates the FIDO
ceremony end to end: preparing payloads, building `clientDataJSON`, formatting
requests, making credentials, and getting assertions. This directly answers
the most common developer question about v1 — which data fields belong where
in a WebAuthn ceremony. The design is deliberately consistent with the
Android, Python, and Rust SDKs so behavior transfers across platforms.

There is no plan to ship a server-side validation component alongside this
client. Scope is the client ceremony only.

### OpenPGP

`Yubico.YubiKit.OpenPgp` is new applet support with no v1 equivalent.
Per-applet documentation covers supported operations and any firmware
constraints; this summary intentionally does not restate applet-specific
compatibility details.

## Async-First Architecture

Every v2 public API is `async`/`await`. There are no synchronous facades
anywhere in the SDK. This is a deliberate architectural choice, not a partial
port: it optimizes for how the SDK actually spends time waiting on the
YubiKey, at the cost of requiring async adoption even for simple
console-app or script use cases that v1 didn't require.

## Compartmentalized Packaging

V1 shipped as two packages (`Yubico.Core`, `Yubico.YubiKey`) that pulled in
every applet whether an application used it or not. V2 splits into 10 focused
packages:

`Yubico.YubiKit.Core`, `.Management`, `.Piv`, `.Fido2`, `.WebAuthn`, `.Oath`,
`.YubiOtp`, `.OpenPgp`, `.SecurityDomain`, `.YubiHsm`.

Applications install only the applets they use, which keeps deployed size
down — this matters for enterprise and CLI/agent tooling scenarios in
particular.

**There is no meta-package.** This is a deliberate decision, not an
oversight: bundling "install everything" back into one package would
undercut the footprint benefit compartmentalization is meant to deliver.
Consumers who genuinely need every applet add all 10 package references
explicitly.

## Native AOT Support

V2 SDK libraries compile to a single self-contained native binary with
`PublishAot=true` and run without a .NET runtime installed on the host
machine. All 10 SDK libraries are covered.

This work landed through a reviewed, evidence-backed PR stack
(`#592` → `#578` → `#587`, plus the native packaging prerequisite `#586`):

- Zero AOT/trimming analyzer warnings, no suppressions, across all 10
  libraries.
- Verified with real physical YubiKey hardware on macOS arm64, Windows x64,
  and Linux x64.
- A companion native-packaging effort is closing the last rough edge: today's
  public NativeShims package still ships a shared-library sidecar next to an
  AOT executable; a self-contained static NativeShims package removing that
  sidecar is in progress and expected shortly.

Deeper protocol-level runtime testing under AOT currently covers Core device
discovery, Management, and PIV most thoroughly. FIDO2/WebAuthn and YubiOTP
HID exchanges, and OATH/OpenPGP/SecurityDomain/YubiHSM session operations,
are confirmed to link and run under AOT but have less exhaustive
protocol-level runtime coverage than PIV. This is expected to deepen before
general availability, not a known defect.

## Why V2 Breaks So Much, On Purpose

V1 shipped a large public surface — typed TLV readers/writers, general-purpose
codecs (`Base16`, `Base32`, `Bcd`, `ModHex`), and pluggable low-level crypto
primitive interfaces. Because all of it was public, almost any internal
change became a breaking change, which made the SDK slow to evolve.

V2 deliberately keeps this class of low-level primitive internal:

- `TlvReader`/`TlvWriter` (typed, sequential parsing) are gone. They are
  replaced by a much thinner `Tlv`/`TlvHelper`/`DisposableTlvList` surface —
  simple tag/value containers and static encode/decode helpers, not a public
  extensibility point.
- `Base16`/`Base32`/`Bcd`/`ModHex` standalone codecs are no longer public.
- Pluggable crypto primitive interfaces (`IAesGcmPrimitives`,
  `IEcdhPrimitives`, `ICmacPrimitives`) are no longer public extension points.

This is intentional API discipline, not an oversight. A smaller, more
deliberate public surface means v2 can keep evolving without repeating v1's
pattern of constant breaking changes. Application code that depended on these
utilities directly needs its own replacement; see
[`docs/v1-to-v2-comparison.md`](v1-to-v2-comparison.md) for the breakdown.

## Restored From V1 (Since Initial Gap Analysis)

An initial v1/v2 gap analysis (2026-07-21) flagged several v1 capabilities
that were missing in early v2. Several of these have since been restored:

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

See [`docs/v1-to-v2-comparison.md`](v1-to-v2-comparison.md) for what remains
open, deferred, or intentionally decided against.

## Not Yet Supported

- **Post-quantum algorithms (ML-DSA, ML-KEM):** not implemented in the .NET
  SDK today. Feature-parity timing across SDKs is being coordinated
  separately; do not commit to a date for this in external communication
  until that's resolved.

## Near-Term Roadmap

- **Unified credential-collection pattern:** v1's `KeyCollector` delegate is
  not replaced by an equivalent in v2 — each applet currently handles
  PIN/PUK/touch collection independently. Whether to add a unified pattern is
  under evaluation, not committed.

## Current Release State

V2 ships today as `2.0.0-alpha.2` from a public, anonymous, unsigned NuGet
feed. It has **not yet completed Yubico's formal security audit** and is
explicitly marked not for production use. This is a real gap against the
"audited, fully functional release by end of year" commitment and needs an
explicit timeline decision, not just documentation work.
