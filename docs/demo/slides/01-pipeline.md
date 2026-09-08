## Host App → SDK → Session → YubiKey

![h:530](assets/L2-layered-stack.svg)

---

## The pipeline in code

```csharp
// 1. HOST APP asks the SDK for devices
var keys = await YubiKeyManager.FindAllAsync();

// 2. SDK returns IYubiKey handles
IYubiKey key = keys[0];

// 3. SESSION opened over a transport the SDK resolves
await using var piv = await key.CreatePivSessionAsync();
(REVIEW: Can we add the some more examples, e.g. using SessionCreationOptions, also the await using var conn = await key.ConnectAsync<ISmartCardConnection>(); variant, and PivSession)

// 4. YUBIKEY executes; the session speaks the applet protocol
var cert = await piv.GetCertificateAsync(PivSlot.Authentication);
```

Every layer boundary is `async`. There is no synchronous escape hatch on the golden path.

<!-- Anchors: FindAllAsync src/Core/src/PublicAPI.Unshipped.txt:985;
     CreatePivSessionAsync src/Piv/src/IYubiKeyExtensions.cs:38;
     GetCertificateAsync src/Piv/src/PublicAPI.Unshipped.txt:191 -->
