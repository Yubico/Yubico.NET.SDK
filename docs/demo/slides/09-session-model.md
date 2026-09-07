## One session shape — for eight of the nine

```csharp
await using var s = await key.CreateXSessionAsync(options, cancellationToken);
```

- A sealed `XSession` plus a matching `IXSession` interface
- `IYubiKey.CreateXSessionAsync(...)` — session owns a hidden connection
- `XSession.CreateAsync(connection, ...)` — session borrows yours
- Both take `SessionCreationOptions?` then a defaulted `CancellationToken`
- Always `await using`

**Test-enforced, not conventional** — `AppletSessionShapeTests` enumerates the eight
sessions and asserts the shape holds for each.

> **WebAuthn is the deliberate exception.** It is not an applet session. It returns a
> `WebAuthnClient`, takes a required origin and public-suffix checker, and has its own
> factory test. The test file says so in as many words:
> *"WebAuthn is not an applet session and has its own factory test."*

<!-- Anchors: grammar docs/architecture/applet-public-api.md:3-9;
     eight sessions src/PublicApi/tests/.../AppletSessionShapeTests.cs:16-26;
     exclusion FactoryShapeTests.cs:56, WebAuthn factory shape :94-127 -->

---

## Ownership: who disposes the connection

```csharp
// Convenience — the session owns a hidden connection and disposes it
await using var piv = await key.CreatePivSessionAsync();
```

```csharp
// Direct factory — you own the connection, you dispose both
await using var conn = await key.ConnectAsync<ISmartCardConnection>();
await using var mgmt = await ManagementSession.CreateAsync(conn);
var info = await mgmt.GetDeviceInfoAsync();
```

**One key admits one live connection. One connection admits one live session** —
a second `Attach` throws `ConnectionInUseException`. Dispose session N before creating
N+1 over the same connection. Prefer `DisposeAsync`; sync `Dispose` blocks to drain.

| SDK | Who owns the connection |
|---|---|
| **.NET v2** | Both, named explicitly at the call site |
| ykman (Python) | Caller — `with dev.open_connection(...)` |
| ykman (rust) | Session **consumes** it; returns `(Error, Connection)` on failure |
| yubikit-android | Session borrows, but `close()` closes the connection too |
| yubikit-swift | Caller owns; session never closes it |

<!-- Anchors: real call site src/Management/tests/.../ManagementSessionSimpleTests.cs:40-43;
     OwnConnection src/Piv/src/IYubiKeyExtensions.cs:59;
     rules docs/architecture/raw-access-tiers.md:149-152,158-159;
     enforcement src/Core/src/Sessions/ConnectionSessionGuard.cs:44-52;
     peers: python yubikit/core/__init__.py:182; rust management.rs:34;
     android SmartCardProtocol.java:125-127; swift@1.4.0 PIVSession.swift:67 -->
