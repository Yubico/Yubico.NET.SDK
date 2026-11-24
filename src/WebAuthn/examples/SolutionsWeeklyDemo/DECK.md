# Solutions Weekly — YubiKit .NET SDK v2: deck handoff

Owner: Dennis Dyall (TLA team). Purpose: hand this to a slide-building agent (Gemini) and a demo-prep agent.
Source of truth for SDK facts: branch `yubikit-pass2-base` — `docs/v2-highlights.md`,
`docs/v1-to-v2-comparison.md`, `docs/migration/v1-to-v2-gaps.md`, `src/WebAuthn/README.md`,
`src/Core/src/Credentials/`.

## Session facts

- Format: live, remote, recorded. 25 minutes total **including Q&A** (~15–17 min talk, ~8 min Q&A).
- No pre-recorded backup. Live hardware demo.
- Audience: Solutions Engineers. Technical, "happy scripters" (PowerShell/Bash), think in abstract/solution
  terms, prototype customer ideas against our SDKs to judge viability. They tailor solutions for high-tier
  customers and later close with Sales (Sales not in the room).
- Customer verticals: finance, government, healthcare. Heavy PIV, passkeys, OATH. Large enterprise / CMS vendors.
- First Solutions Weekly on the .NET SDK (as far as known).
- Session goal: (1) give SEs an accurate positioning of v2; (2) **collect demand signals** — what customers
  want in v2, v1 pain points.

## Style brief for Gemini

- 14 slides max. ≤ 4 bullets per slide, ≤ 12 words per bullet. Speaker notes separate from on-slide text.
- Tone: candid engineering talk; state trade-offs openly. No marketing superlatives.
- Use the Solutions Weekly template layouts (title, segment title, 2/3-column cards, metric list).
- Visual ideas: v1 two-box monolith vs v2 ten tiles; "1 key → 1 connection → 1 session" chain;
  timeline (Alpha now → Beta Dec 16 → GA with YubiKey 6/PQC).

## Recording guardrails (must follow)

- Do **not** name customers (Microsoft, Google, Amazon) or speculate about their deployments. Say
  "large enterprise and CMS vendors".
- Do **not** claim: production-ready; YubiKey 6 or PQC supported; firmware < 5 supported; previewSign stable;
  SDK verifies WebAuthn signatures (the relying party does); interactive PIN prompting shipped (PR #687 is a draft).
- No PowerShell/scripting content (deferred). If asked in Q&A: "Scripting support interests us — tell us in
  #ask-tla what you'd need."

## Slide outline

| # | Slide | ~min |
|---|---|---|
| 1 | Title | – |
| 2 | Agenda + what we want from you | 0.5 |
| 3 | Why v2: lessons from v1 | 1.5 |
| 4 | Built for today's stack | 1.5 |
| 5 | Install only what you use + Native AOT | 1 |
| 6 | Stable API, forward compatible | 1.5 |
| 7 | One key, one session, one grammar, async | 1.5 |
| 8 | New capabilities | 1 |
| 9 | Touch prompting + interactive auth roadmap | 1 |
| 10 | Remote & virtualized scenarios | 1.5 |
| 11 | Live demo | 4 |
| 12 | Migration cost + v1 continuity | 1 |
| 13 | Roadmap | 0.5 |
| 14 | Tell us + Q&A | 7–8 |

### 1. Title
- Solutions Weekly — YubiKit .NET SDK v2
- Subtitle: [Date] | Dennis Dyall, Senior Software Engineer, TLA Team
- Notes: 2 years maintaining v1; leading v2 design and implementation.

### 2. Agenda + what we want from you
- Why v2 · What changed · What's new · Live demo · Migration & roadmap · Q&A
- Ask: bring customer pain points and wishes — we want your signals.

### 3. Why v2: lessons from v1
- ~500k NuGet downloads over 5 years in production.
- Two packages (`Yubico.Core`, `Yubico.YubiKey`); every applet shipped regardless of use.
- Almost everything public (TLV, codecs, crypto hooks) → most internal fixes were breaking changes.
- Ambition to support any smart-card device/authenticator via a public core layer — dropped for now;
  v2 focuses on YubiKeys.
- Notes: "Once something is public it's a promise." v1 made too many promises and got slow to move.

### 4. Built for today's stack
- Teams adopting v2 run current .NET and current YubiKeys.
- .NET 10 LTS only (`net10.0`).
- YubiKey firmware 5.x is the tested floor; older firmware may work for some operations but is untested.
- Example: NEO / YubiKey 4 USB interface reconfiguration removed from the public Management API.
- YubiKey 6: partial today, expanding (Dennis has a YubiKey 6 for testing).
- Notes: "Designed for where customers are going, not where they've been."
- Notes (.NET Framework): v1 targets `netstandard2.0;netstandard2.1;net472`, so it runs on .NET Framework 4.7.2+,
  .NET Core and Unity-style hosts. v2 is `net10.0` only. No .NET Framework, netstandard or older .NET
  (Core 3.1/5/6/8). That's a deliberate drop, and the migration docs flag it as the biggest blocker for that group.
- Notes (what it cost in v1): keeping .NET Framework meant polyfill packages (`Microsoft.Bcl.AsyncInterfaces`,
  `Microsoft.Bcl.Cryptography`, `Microsoft.Bcl.HashCode`, `System.Memory`, PolySharp, `System.Formats.Cbor` as a
  package), framework-specific references (e.g. `System.Web` just to parse query strings on net472) and
  conditional code. One modern target lets v2 use current runtime APIs directly (Span/Memory, modern crypto,
  async streams) and keeps the code simpler to make trimming- and Native AOT-safe. .NET Framework apps can't
  use Native AOT at all. (Don't claim v1 is AOT-incompatible; that hasn't been checked.)
- Notes (what to tell customers): .NET Framework apps stay on v1, which keeps getting security fixes and is
  forward compatible. Moving to v2 means moving the host app to .NET 10. A netstandard library that wraps the
  SDK would need to target .NET 10 too.
- Sources: `docs/v1-to-v2-comparison.md`, `docs/migration/v1-to-v2-gaps.md` (gap #1), v1 `Yubico.YubiKey.csproj`.

### 5. Install only what you use + Native AOT
- Ten packages: `Core`, `Management`, `Piv`, `Ctap2`, `WebAuthn`, `Oath`, `YubiOtp`, `OpenPgp`,
  `SecurityDomain`, `YubiHsm`. No meta-package, deliberately.
- Native AOT: all ten marked AOT-compatible, analyzer-checked and link-verified → native executable,
  no .NET runtime required.
- Notes: runtime AOT evidence — core discovery on macOS, Windows, Linux; deeper testing mostly macOS;
  some applets link-verified only. Do not say "full support".
- Notes (AOT benefit): you ship one native executable. No .NET runtime to install, fast startup with no JIT
  warm-up, and a smaller footprint (trimmed to what you use, which pairs with the ten-package split).
- Notes (who benefits):
  - CLI and agent tooling, and SEs prototyping: a single binary that PowerShell or Bash can call.
  - Locked-down endpoints (government, finance, healthcare) where installing a runtime needs approval.
  - Containers and short-lived jobs, where startup time and image size matter.
  - Less so for long-running server or desktop apps that already ship with a runtime.
- Notes (the cost): one build per OS and CPU architecture, and the app's own code must be AOT-safe
  (no runtime reflection or dynamic code). The SDK being AOT-compatible doesn't make the whole app so.

### 6. Stable API, forward compatible
- High-level sessions public; commands, TLV parsing, codecs internal.
- Lenient by design: unknown CBOR fields, PIV data, algorithm/format IDs preserved and returned raw.
- New CTAP versions, YubiKey features or authenticators don't throw before the SDK catches up.
- Example: attestation format falls back to `Other(string)`.

### 7. One key, one session, one grammar, async
- Windows, macOS, Linux; HID and smart card.
- In-process: 1 physical key → 1 live connection → 1 live session; conflicts throw
  `ConnectionInUseException` before disturbing running work. Cross-process follows OS sharing rules.
- All 8 applet sessions created the same way: `IYubiKey.CreateXSessionAsync` or `XSession.CreateAsync`;
  shared `SessionCreationOptions`. Concepts align with YubiKit Android and Python.
- Applet operations are awaitable; device events via `YubiKeyManager.WatchAsync` as
  `IAsyncEnumerable<DeviceChange>`, no Rx dependency. (Not "async all the way down" — a few low-level paths stay sync.)
- Notes (why async, DX): v1 had no async at all. Every call blocked its thread, and `KeyCollector` was a
  synchronous callback invoked in the middle of a multi-step auth flow. In a desktop app that meant pushing work
  onto a background thread, then marshalling back to the UI thread from inside the callback to show a PIN or
  touch dialog. v2: just `await`. The app stays responsive while the key waits for USB and the user's touch,
  cancellation is a standard `CancellationToken` (designed for it, though not every native wait can be
  interrupted), and it fits ASP.NET Core, MAUI, WPF and `await foreach` device events.
- Notes (the cost): scripts and small CLIs become async (`async Task Main`); synchronous code paths migrating
  from v1 must become async, because blocking on async calls (`.Result`) can deadlock in UI contexts.
  Async doesn't make the hardware faster; it frees your threads.
- Slide line: "Async where the hardware waits."

### 8. New capabilities
- WebAuthn client: builds `clientDataJSON`, handles PIN/UV, processes extensions.
- OpenPGP: key provisioning, PIN recovery, sign/decrypt/agree, certificates, touch policy.
- Also in preview: raw signing with a key bound to a passkey (previewSign). One line only; details live in
  the optional demo and backup slide B1.

### 9. Touch prompting + interactive auth roadmap
- `IUserPresencePrompt` across 6 applets: PIV, FIDO2/WebAuthn, OATH, OpenPGP, YubiOTP, YubiHSM Auth.
- Operation (SDK): stable ID of what the key is doing. Intent (app): why the user touches,
  via `UserPresenceIntent.BeginScope("Unlock your notes")`.
- Requested → resolved callbacks; UI always cleared, even on cancellation.
- `KeyCollector` is gone. Uniform interactive auth in progress: PIV signing + key generation first
  (PR #687, draft), other applets next.
- Notes: "If customers depend on interactive PIN flows, tell us — it shapes the rollout."

### 10. Remote & virtualized scenarios
- Tested: Windows VMs on Linux and macOS hosts, USB passthrough / smart card redirection,
  RDP, Windows containers, OpenSSH.
- Not yet tested: Windows host → Windows VM.
- Ask: "Who has that setup and can help us test?"

### 11. Live demo
- Code: branch `yubikit-solweekly-demo`, folder `src/WebAuthn/examples/SolutionsWeeklyDemo/`.
  Packages: alpha feed, `2.0.0-alpha.115`.
- Main demo: `passkey-demo.cs` (run: `dotnet run passkey-demo.cs`). Detect key → device info →
  register passkey → sign in with app intent ("unlock your notes") → pretend server verifies. Two touches.
- On screen: the touch prompt shows the SDK operation and the app intent; the server verifies, not the SDK.
- Optional second demo, only if time allows or someone asks: `previewsign-demo.cs` (see backup slide B1).
- Fallback slide: screenshot of a successful passkey run.

### 12. Migration cost + v1 continuity
- v2 cost: .NET 10 + async; firmware 5.x floor; U2F/CTAP1 removed (no path for U2F-only);
  `KeyCollector` removed; code using v1's public low-level utilities needs its own replacement.
- Docs: `docs/migration/v1-to-v2.md`, changelog, gap list, v1→v2 API map.
- v1: maintenance mode — security fixes and patches, no new features; all new development in v2.
- v1 hardened to be forward compatible (lenient with unknown data) so it stays usable for years.
- Stay on v1 for: .NET Framework/older .NET, firmware < 5, U2F-only, current `KeyCollector` reliance.
- Notes: "No one is forced to migrate — unless they want proper YubiKey 6 and PQC support."

### 13. Roadmap
- Now: alpha, public; setup via repo README.
- Dec 16, 2026: beta on NuGet as pre-release.
- GA: aligned with YubiKey 6 + post-quantum (ML-DSA, ML-KEM) and the other Yubico SDKs.
- Next: interactive auth across applets; YubiKey 6 and PQC support.

### 14. Tell us + Q&A (keep on screen during Q&A)
- What did v1 make painful?
- What do your customers need from v2? (interactive PIN, remote/VDI, YubiKey 6/PQC, CMS integration)
- U2F-only customers or fleets below firmware 5?
- Who can help test Windows→Windows VMs?
- Feedback: **#ask-tla** Slack channel.

## Backup slides (not presented; for Q&A or the optional demo)

### B1. previewSign: hardware-bound signatures for any protocol (experimental)
- Signs the **raw input**. A normal WebAuthn signature covers authenticator data + a hash of client data,
  so verifiers must be adapted (as OpenSSH did). previewSign output drops into existing protocols
  (e.g. OpenPGP messages, digital identity credentials) with unchanged verifiers.
- **Separate key** from the passkey, so it can't be used to sign sign-in challenges for other sites.
- **No storage on the key**: signing keys are re-derived on each use from the credential's internal secret
  (credRandom, the same value PRF uses). The key handle works like an encrypted external key. No export.
- **ARKG**: the relying party can create fresh, unlinkable public keys without the YubiKey present
  (BIP32-inspired). Currently the only previewSign algorithm YubiKeys support.
- Primary use case: wallets (wwWallet, EU digital identity wallet) signing transactions and credential
  presentations without the protocols needing FIDO support.
- Notes: "Mainly valuable for security protocol designers. For customers: more uses for their YubiKeys,
  with interoperable signatures."
- Notes: **not transaction confirmation**. The key never displays the data; it is opaque bytes.
  The app can request a signature within an existing session without verifying the sign-in signature.
- Source: Emil Lundberg; explainer https://github.com/w3c/webauthn/blob/main/explainers/raw-signing-extension.md

## Demos (done)

- `passkey-demo.cs` (37 lines) and `previewsign-demo.cs` (53 lines), sharing `DemoSupport/` via `#:project`.
- Both verified on a YubiKey 5 NFC, firmware 5.8.0 (2026-11-24).
- previewSign demo: signing key at registration → ARKG public key derived without the YubiKey → raw payload
  signed → verified with plain ECDSA. No sign-in verification, no "approve transfer" wording.

## Demo prep checklist

- Demo verified on a YubiKey 5 NFC, firmware 5.8.0 (Linux workstation, 2026-11-24).
- On the Mac: `git fetch origin && git switch yubikit-solweekly-demo`, then run once before going live
  (the first `dotnet run` compiles; later runs reuse the build).
- Use the presenting key: it must support previewSign, and must not require UV ("Always Require UV" off).
  A FIDO PIN being set is fine. Check with `ykman fido info`.
- Don't leave the demo waiting: if nobody touches, the sign-in times out with a long stack trace.
  Start it only when ready to touch.
- Each run stores a test passkey for `example.com`; clean up with `ykman fido credentials list` / `delete`.
- Bring a second key. Rehearse on the presenting machine with screen sharing.
- Capture a success screenshot for the fallback slide.
- A missed or late touch fails with "Operation denied". Touch promptly.
- Optional polish: registration intent text (e.g. "create your passkey"); friendlier form-factor name.
- If the beta lands on nuget.org before the session: bump `#:package` versions and drop the alpha feed.

## Remaining TODOs

- Date on title slide.
- Confirm "~500k downloads" wording (approximate is fine).
- Confirm Dec 16 beta date is safe to state on a recording.
