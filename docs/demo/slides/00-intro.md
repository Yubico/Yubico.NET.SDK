<!-- _class: lead -->

# YubiKit .NET **v2**

### A tour for people who live in the other SDKs

`yubikey-manager` · `yubikit-android` · `yubikit-swift` · `python-fido2`

<br>

- **Async on the golden path** — every applet operation is `async`; the few sync
  members that remain are deliberate lifecycle and transaction escapes
- **Install only what you use** — ten packages; all ten assemblies total 1.41 MiB
- **Native AOT** — 11.7 MiB resident, whole SDK linked
- **Event-driven discovery** — OS notifications, not a 500 ms polling timer
- **One session shape** — learn one applet, you know the rest

<br>

Branch `yubikit` @ `d04d59aa` · 2026-09-07
