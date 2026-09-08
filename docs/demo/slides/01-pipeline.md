## Host App → SDK → Session → YubiKey

![h:530](assets/L2-layered-stack.svg)

---

## The pipeline in code

```csharp
// 1. HOST APP asks the SDK for devices
var keys = await YubiKeyManager.FindAllAsync();
IYubiKey key = keys[0];

// 2. SESSION — convenience: the session owns a hidden connection
await using var piv = await key.CreatePivSessionAsync();

// 3. YUBIKEY executes; the session speaks the applet protocol
var cert = await piv.GetCertificateAsync(PivSlot.Authentication);
```

Every layer boundary is `async`. There is no synchronous escape hatch on the golden path.

---

## Three ways to open a session

```csharp
// A. Convenience — SDK resolves the transport, owns and disposes the connection
await using var piv = await key.CreatePivSessionAsync();

// B. Convenience + options — force a transport, add a secure channel
await using var piv = await key.CreatePivSessionAsync(
    new SessionCreationOptions
    {
        PreferredConnectionType = ConnectionType.SmartCard,
        ScpKeyParameters = scpKeyParams,
    });

// C. Bring your own connection — you own it, you dispose both
await using var conn = await key.ConnectAsync<ISmartCardConnection>();
await using var piv  = await PivSession.CreateAsync(conn);
await using var mgmt = await ManagementSession.CreateAsync(conn);   // after piv is disposed
```

**A** for one-shot work. **B** when the default transport is wrong, or you need SCP.
**C** when several applets share one connection — sequentially; one connection admits
one live session.

<!-- Anchors: FindAllAsync src/Core/src/PublicAPI.Unshipped.txt:985;
     CreatePivSessionAsync src/Piv/src/IYubiKeyExtensions.cs:38;
     GetCertificateAsync src/Piv/src/PublicAPI.Unshipped.txt:191;
     PivSession.CreateAsync src/Piv/src/PivSession.cs:130;
     ConnectAsync<T> src/Core/src/Abstractions/IYubiKey.cs:164;
     SessionCreationOptions fields docs/architecture/applet-public-api.md:10-13;
     SCP-over-session real call site
     src/Management/tests/.../ManagementSessionSimpleTests.cs:212-217;
     one-session-per-connection src/Core/src/Sessions/ConnectionSessionGuard.cs:44-52 -->
