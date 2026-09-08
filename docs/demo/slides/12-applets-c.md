## SecurityDomain (SCP)

GlobalPlatform key management — SCP03 and SCP11, certificates, CA identifiers.

```csharp
await using var sd = await key.CreateSecurityDomainSessionAsync();
var certs = await sd.GetCertificatesAsync(keyReference); (REVIEW: why are we not demonstrating getting key information, as we do in Python and Swift)
```

<div class="cols">

**ykman (Python)**
```python
sd = SecurityDomainSession(conn)
keys = sd.get_key_information()
```

**yubikit-swift**
```swift
let s = try await SecurityDomainSession.makeSession(connection: connection)
let keyInfo = try await s.getKeyInformation()
```

</div>

**Delta:** in .NET, SCP is *also* a creation option on every other applet — pass
`SessionCreationOptions { ScpKeyParameters = ... }` and any session runs over a secure
channel. Supplying it without a transport preference **forces SmartCard**.

<!-- Anchors: .NET src/SecurityDomain/src/IYubiKeyExtensions.cs:45,
     PublicAPI.Unshipped.txt:22; SCP-as-option
     src/Management/tests/.../ManagementSessionSimpleTests.cs:215-217,
     SCP-forces-SmartCard src/Management/src/IYubiKeyExtensions.cs:109;
     python yubikit/securitydomain.py:100,122;
     swift@1.4.0 SecurityDomainSession.swift:55,124 -->

---

## YubiHSM Auth

Store and use credentials that authenticate to a YubiHSM 2.

```csharp
await using var hsm = await key.CreateHsmAuthSessionAsync();
var creds = await hsm.ListCredentialsAsync();
```

<div class="cols">

**ykman (Python)**
```python
hsm = HsmAuthSession(conn)
creds = hsm.list_credentials()
```

**ykman (rust)**
```rust
let mut s = HsmAuthSession::new(conn).map_err(|(e, _)| e)?;
let creds = s.list_credentials()?;
```

</div>

**Delta:** only .NET, Python and rust implement this. `yubikit-android` has **no**
YubiHSM Auth module; `yubikit-swift` has only the `Capability.hsmAuth` bit.

<!-- Anchors: .NET src/YubiHsm/src/IYubiKeyExtensions.cs:41,
     PublicAPI.Unshipped.txt:36; python yubikit/hsmauth.py:223,250;
     rust crates/yubikit/src/hsmauth.rs:32,383;
     android absent settings.gradle.kts:35-39; swift@1.4.0 absent Capability.swift:30 -->

---

## YubiOTP

The two programmable slots — Yubico OTP, static password, HMAC-SHA1 challenge-response.

(REVIEW: why are we showcasing different aspects of the YubiOTP session across SDKs? We should aim for consistency in the examples shown for each SDK) 

```csharp
await using var otp = await key.CreateYubiOtpSessionAsync();
var serial = await otp.GetSerialNumberAsync();
```

<div class="cols">

**ykman (Python)**
```python
otp = YubiOtpSession(conn)
state = otp.get_config_state()
```

**yubikit-android**
```java
YubiOtpSession otp = new YubiOtpSession(otpConnection);
byte[] r = otp.calculateHmacSha1(Slot.TWO, challenge, null);
```

</div>

**Delta:** despite the name, .NET's YubiOTP session is **dual-transport and prefers
SmartCard** — `SmartCard → HidOtp`. On a CCID-enabled key the snippet above runs over
APDU, not HID. `yubikit-swift` has no YubiOTP slot session at all.

<!-- Anchors: .NET src/YubiOtp/src/IYubiKeyExtensions.cs:102,
     transport order :144-145, PublicAPI.Unshipped.txt:166;
     python yubikit/yubiotp.py:708,779; android YubiOtpSession.java:253,447;
     swift@1.4.0 absent Capability.swift:20 -->
