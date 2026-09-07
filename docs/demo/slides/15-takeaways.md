<!-- _class: lead -->

## Takeaways

**One shape, eight applet sessions** — test-enforced, not conventional.
WebAuthn is the deliberate exception: it returns a client, not a session.

**Ownership is explicit.** Convenience owns the connection, the direct factory borrows it.
One connection, one live session, enforced at runtime.

**Device events are one API.** `StartMonitoring()` + `await foreach WatchAsync()`.
No Rx, no `IObservable`, BCL types only.

**Identity is honest about what it cannot promise.** `DeviceId` is diagnostic;
`SerialNumber` may be `null` forever and can arrive late without an event.

**AOT is real.** 11.7 MiB resident with the whole SDK linked, ~4.4× less than JIT.

<br>

`docs/architecture/` · `docs/usage/device-discovery.md` · `docs/NATIVE-AOT.md`
Status **2.0.0-alpha.2** — public API still in `PublicAPI.Unshipped.txt`, so breaking
changes are still cheap. Now is the time to complain.

<!-- Anchors: eight sessions AppletSessionShapeTests.cs:16-26;
     version Directory.Packages.props:6; all other claims anchored on their own slides -->
