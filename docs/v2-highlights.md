# What's new in YubiKit .NET v2

Last updated: 2026-08-31

V2 is a ground-up rewrite of the YubiKey .NET SDK. It speaks to YubiKey
applications v1 never supported, it's async from top to bottom, you install
only the pieces you actually use, and it compiles to a native binary with no
.NET runtime required on the machine.

It also breaks a lot of things on purpose. This page covers both halves —
what you get, and what it costs you — so you can decide whether v2 is worth
the migration today.

## New applet support

Two YubiKey applications the SDK simply didn't speak to before.

### WebAuthn

If you've ever squinted at a WebAuthn spec trying to work out which field
goes where, that part is over. `Yubico.YubiKit.WebAuthn` runs the whole FIDO
ceremony for you — preparing payloads, building `clientDataJSON`, formatting
requests, making credentials, and getting assertions.

This was the single most common source of confusion in v1, so we designed the
package to match how the Android, Python, and Rust SDKs already do it. If you
know the ceremony on one platform, it transfers.

It's a client-side package. Server-side validation isn't in scope, and we're
not planning to add it.

### OpenPGP

`Yubico.YubiKit.OpenPgp` brings OpenPGP support to the .NET SDK for the first
time. Supported operations and firmware requirements live in the per-applet
documentation rather than here.

## Async all the way down

Every public API in v2 is `async`/`await`. There are no synchronous wrappers
anywhere, and that's deliberate rather than half-finished.

Talking to a YubiKey is mostly waiting on a YubiKey, and v2's API shape now
reflects that honestly. The tradeoff is real: if you're writing a small
console tool or a script, you'll be writing async code where v1 let you get
away without it.

## Install only what you use

V1 gave you two packages — `Yubico.Core` and `Yubico.YubiKey` — and every
applet came along whether you touched it or not. V2 splits into ten focused
packages:

`Yubico.YubiKit.Core`, `.Management`, `.Piv`, `.Fido2`, `.WebAuthn`, `.Oath`,
`.YubiOtp`, `.OpenPgp`, `.SecurityDomain`, `.YubiHsm`

Reference the applets your application actually uses and your deployed
footprint drops accordingly. That matters most for enterprise deployments and
CLI or agent tooling, where shipping unused protocol code is pure cost.

**There's no meta-package, and there won't be one.** An "install everything"
bundle would hand back exactly the footprint savings the split exists to
deliver. If you genuinely need all ten applets, reference all ten.

## Native AOT

V2 compiles to a single self-contained native binary with `PublishAot=true`,
and runs on machines with no .NET runtime installed. All ten libraries are
covered.

- Zero AOT and trimming analyzer warnings across every library — no
  suppressions hiding anything.
- Verified against real YubiKey hardware on macOS arm64, Windows x64, and
  Linux x64.
- One rough edge is still being smoothed: the current public NativeShims
  package ships a shared-library sidecar next to your AOT executable. A
  self-contained static build that removes the sidecar is in progress.

The work landed through the `#592` → `#578` → `#587` PR stack, with `#586` as
the native packaging prerequisite.

Runtime testing under AOT is deepest on Core device discovery, Management,
and PIV. FIDO2, WebAuthn, YubiOTP, OATH, OpenPGP, SecurityDomain, and YubiHSM
all link and run under AOT, but with lighter protocol-level coverage so far.
We're expanding that before general availability — nothing here is a known
defect.

## Why v2 breaks so much, on purpose

Worth being straight about this, because the migration cost is real.

V1 made almost everything public: typed TLV readers and writers,
general-purpose codecs (`Base16`, `Base32`, `Bcd`, `ModHex`), and pluggable
low-level crypto primitive interfaces. Once something is public it's a
promise, and v1 had made so many promises that nearly any internal
improvement turned into a breaking change. The SDK got slow to move.

V2 keeps that whole class of low-level primitive internal:

- `TlvReader`/`TlvWriter` are gone, replaced by a much thinner
  `Tlv`/`TlvHelper`/`DisposableTlvList` surface — tag/value containers and
  static encode/decode helpers, not an extensibility point.
- The `Base16`/`Base32`/`Bcd`/`ModHex` codecs are no longer public.
- Pluggable crypto primitives (`IAesGcmPrimitives`, `IEcdhPrimitives`,
  `ICmacPrimitives`) are no longer public extension points.

This is API discipline, not an oversight. A smaller public surface is what
lets v2 keep improving without putting you through v1's steady drip of
breaking changes. If your code leaned on those utilities directly, you'll
need your own replacement —
[the v1 to v2 comparison](v1-to-v2-comparison.md) has the details.

## Restored from v1

An early v1/v2 gap analysis in July flagged capabilities that hadn't made it
into v2 yet. These are back:

- PIV PIN-only (`PinProtected`) management-key mode, plus typed CHUID, CCC,
  AdminData, and KeyHistory data objects.
- OATH `IsPasswordProtected` and `AuthenticateAndRetryAsync`, plus a
  dedicated `OathException`.
- YubiHSM Auth's `HsmAuthRetryException`, the `OnTouchRequired` callback, and
  the hardware-verified `Counter` → `RetriesRemaining` rename.
- YubiOTP keyboard-layout-aware static passwords and Yubico-OTP-algorithm
  challenge-response.
- Dedicated exception types for SecurityDomain (`SecureChannelException`) and
  OpenPGP (`OpenPgpInvalidPinException`).

[The v1 to v2 comparison](v1-to-v2-comparison.md) covers what's still open,
still undecided, or deliberately not coming.

## Still to come

**Post-quantum algorithms (ML-DSA, ML-KEM)** aren't in the .NET SDK yet.
We're coordinating parity timing across the SDKs before putting a date on it.

**A unified way to collect PINs and touch.** V1 had the `KeyCollector`
delegate — one callback shape shared across PIV, FIDO2, OATH, U2F, and
YubiHSM Auth. V2 has no callback equivalent: sessions take credentials as
direct method parameters, matching Yubico's other SDKs, so your application
owns the authentication flow. Whether v2 should additionally offer a
unified pattern for interactive flows is still an open design question.

## Where things stand today

V2 is available now as `2.0.0-alpha.2` from a public, anonymous, unsigned
NuGet feed. It hasn't completed Yubico's formal security audit yet, and it's
marked not for production use until it does.

Try it, build against it, and tell us what breaks — that feedback is exactly
what the alpha is for. Just don't ship it to production yet.
