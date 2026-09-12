# What's new in YubiKit .NET v2

Last updated: 2026-09-12

V2 is a ground-up rewrite of the YubiKey .NET SDK. It speaks to YubiKey
applications v1 never supported, it's async from top to bottom, you install
only the pieces you actually use, and it supports Native AOT deployment with
no .NET runtime required on the machine.

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

Every device operation in v2 is `async`/`await`. There are no synchronous
wrappers anywhere, and that's deliberate rather than half-finished.

Talking to a YubiKey is mostly waiting on a YubiKey, and v2's API shape now
reflects that honestly. The tradeoff is real: if you're writing a small
console tool or a script, you'll be writing async code where v1 let you get
away without it.

## Device events without reactive dependencies

`YubiKeyManager.WatchAsync` exposes device arrivals and removals as an
`IAsyncEnumerable`, with no Reactive Extensions dependency and the same async
model as the rest of the SDK. Call `StartMonitoring` first, and start the
`await foreach` before the action you expect to produce an event — a watcher
subscribes on first enumeration, not when the method is called.

Each watcher has its own bounded buffer, so consumers do not block or
interfere with one another. If a consumer falls too far behind, its stream
faults instead of silently dropping device state; call `FindAllAsync` to
resynchronise.

## One physical key, safely owned

`IYubiKey` represents a physical key when discovery has enough evidence to
group its interfaces. `SerialNumber` and the tri-state `SameDeviceAs` help you
correlate references; when the evidence is ambiguous, discovery splits
interfaces rather than guessing.

Within your process, a grouped physical key admits one live connection, and
that connection one live session. Conflicting acquisition throws
`ConnectionInUseException` before another applet or transport can disrupt work
already in progress. Another process holding the same interface still surfaces
an ordinary platform `SCardException`.

## One session grammar across applets

All eight applet sessions share the same creation pattern:
`IYubiKey.CreateXSessionAsync` for a session that owns its connection, or
`XSession.CreateAsync` for one that borrows yours. `SessionCreationOptions`
carries cross-cutting creation policy — protocol configuration, secure-channel
parameters, connection preference, firmware-version override, and a caller-owned
`IUserPresencePrompt` — so those concerns work the same way across applets.

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

All ten published libraries carry Native AOT compatibility metadata and are
analyzer-checked and link-verified. Publish with `PublishAot=true` to produce
a platform-specific native executable that runs with no .NET runtime
installed.

Core discovery has physical-device evidence on macOS Apple Silicon, Windows
x64, and Linux x64. Deeper Management, PIV, and device-monitoring runtime
evidence is currently macOS-only; the remaining applets are link-verified but
not yet runtime-exercised under Native AOT.

Stable deployments are not a single standalone binary: ship the complete
publish output, which includes the platform-specific `Yubico.NativeShims`
native library.

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

**Credential collection is only partly unified.** Applet sessions deliberately
take most credentials as direct parameters, so your application owns those
flows. Core provides `ICredentialPrompt`, and `WebAuthnClient` uses it with a
bounded retry loop that defaults to three attempts, but other applets have not
adopted it generally. Touch notification is no longer part of this gap:
`IUserPresencePrompt` is shared by PIV, FIDO2/WebAuthn, OATH, OpenPGP, YubiOTP,
and YubiHSM Auth through `SessionCreationOptions.UserPresencePrompt`.

[The v1 to v2 comparison](v1-to-v2-comparison.md) has the full inventory of
what's changed, restored, still open, or deliberately not coming.

## Where things stand today

V2 is available now as `2.0.0-alpha.*` from a public, anonymous, unsigned
NuGet feed. It hasn't completed Yubico's formal security audit yet, and it's
marked not for production use until it does.

Try it, build against it, and tell us what breaks — that feedback is exactly
what the alpha is for. Just don't ship it to production yet.
