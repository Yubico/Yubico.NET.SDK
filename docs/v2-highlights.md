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
deliver.

## Native AOT

V2 compiles to a single self-contained native binary with `PublishAot=true`,
and runs on machines with no .NET runtime installed. All ten libraries are
covered.

Verified against real YubiKey hardware on macOS arm64, Windows x64, and
Linux x64. Runtime coverage under AOT still varies by library — we're
deepening it before general availability.

## Why v2 breaks so much, on purpose

Worth being straight about this, because the migration cost is real.

V1 made almost everything public: typed TLV readers and writers,
general-purpose codecs, and pluggable low-level crypto primitive interfaces.
Once something is public it's a promise, and v1 had made so many promises
that nearly any internal improvement turned into a breaking change. The SDK
got slow to move.

V2 keeps that whole class of low-level primitives internal. This is API
discipline, not an oversight. A smaller public surface is what lets v2 keep
improving without putting you through v1's steady drip of breaking changes.
If your code leaned on those utilities directly, you'll need your own
replacement — [the v1 to v2 comparison](v1-to-v2-comparison.md) has the full
list and the reasoning behind each one.

## Still to come

**Post-quantum algorithms (ML-DSA, ML-KEM)** aren't in the .NET SDK yet.
We're coordinating parity timing across the SDKs before putting a date on it.

**A unified way to collect PINs and touch.** V1 had the `KeyCollector`
delegate — one callback shape shared across PIV, FIDO2, OATH, U2F, and
YubiHSM Auth. V2 has no callback equivalent: sessions take credentials as
direct method parameters, matching Yubico's other SDKs, so your application
owns the authentication flow. Whether v2 should additionally offer a
unified pattern for interactive flows is still an open design question.

[The v1 to v2 comparison](v1-to-v2-comparison.md) has the full inventory of
what's changed, restored, still open, or deliberately not coming.

## Where things stand today

V2 is available now as `2.0.0-alpha.*` from a public, anonymous, unsigned
NuGet feed. It hasn't completed Yubico's formal security audit yet, and it's
marked not for production use until it does.

Try it, build against it, and tell us what breaks — that feedback is exactly
what the alpha is for. Just don't ship it to production yet.
