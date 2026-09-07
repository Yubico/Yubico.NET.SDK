## Management

Read device info, toggle capabilities, write device config.

```csharp
var info = await key.GetDeviceInfoAsync();   // no session needed
```

<div class="cols">

**ykman (Python)**
```python
mgmt = ManagementSession(conn)
info = mgmt.read_device_info()
```

**yubikit-swift**
```swift
let s = try await Management.Session.makeSession(connection: connection)
let info = try await s.getDeviceInfo()
```

</div>

**Delta:** .NET gives Management a device-level shortcut — no session boilerplate for the
most common read. It is also the widest applet: three transports, `SmartCard → HidFido →
HidOtp`. Swift covers two, with `SmartCardConnection` and `FIDOConnection` overloads.

<!-- Anchors: .NET shortcut src/Management/src/PublicAPI.Unshipped.txt:33,
     transports src/Management/src/IYubiKeyExtensions.cs:143-144;
     python yubikit/support.py:83, yubikit/management.py:598;
     swift@1.4.0 ManagementSession.swift:142 (SmartCard), :159 (FIDO), :72 -->

---

## PIV

Certificates, key pairs, PIN/PUK, attestation.

```csharp
await using var piv = await key.CreatePivSessionAsync();
var cert = await piv.GetCertificateAsync(PivSlot.Authentication);
```

<div class="cols">

**ykman (Python)**
```python
piv = PivSession(conn)
cert = piv.get_certificate(SLOT.AUTHENTICATION)
```

**yubikit-android**
```java
PivSession piv = new PivSession(smartCardConnection);
X509Certificate cert = piv.getCertificate(Slot.AUTHENTICATION);
```

</div>

**Delta:** all three return a real certificate type — `X509Certificate2`,
`x509.Certificate`, `java.security.cert.X509Certificate`. The .NET difference is
**nullability**: an empty slot returns `null` rather than throwing.

<!-- Anchors: .NET src/Piv/src/IYubiKeyExtensions.cs:38,
     PublicAPI.Unshipped.txt:191 (X509Certificate2?);
     python yubikit/piv.py:1258; android PivSession.java:882 -->

---

## OATH

TOTP and HOTP credentials, calculate codes, applet password.

```csharp
await using var oath = await key.CreateOathSessionAsync();
var codes = await oath.CalculateAllAsync();
```

<div class="cols">

**ykman (Python)**
```python
oath = OathSession(conn)
creds = oath.list_credentials()
```

**yubikit-android**
```java
OathSession oath = new OathSession(conn);
Map<Credential, @Nullable Code> codes = oath.calculateCodes();
```

</div>

**Delta:** the closest convergence in the deck — .NET's
`IReadOnlyDictionary<Credential, Code?>` and Android's `Map<Credential, @Nullable Code>`
express the same touch-required semantics. .NET makes it a compiler-enforced `Code?`
rather than an annotation.

<!-- Anchors: .NET src/Oath/src/IYubiKeyExtensions.cs:39,
     PublicAPI.Unshipped.txt:50,87; python yubikit/oath.py:265,447;
     android OathSession.java:391 -->
