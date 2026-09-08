## SecurityDomain (SCP)

GlobalPlatform key management — SCP03 and SCP11, certificates, CA identifiers.

```csharp
await using var sd = await key.CreateSecurityDomainSessionAsync();
var keyInfo = await sd.GetKeyInfoAsync();
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

**Delta:** same operation, three spellings — .NET abbreviates to `GetKeyInfoAsync`
where Python and Swift both write *Information*. Bigger point: in .NET, SCP is *also*
a creation option on every other applet — pass `ScpKeyParameters` in
`SessionCreationOptions` and any session runs over a secure channel.

<!-- Anchors: .NET src/SecurityDomain/src/IYubiKeyExtensions.cs:45,
     GetKeyInfoAsync PublicAPI.Unshipped.txt:24;
     SCP-as-option src/Management/tests/.../ManagementSessionSimpleTests.cs:215-217,
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

```csharp
await using var otp = await key.CreateYubiOtpSessionAsync();
var response = await otp.CalculateHmacSha1Async(Slot.Two, challenge);
```

<div class="cols">

**ykman (Python)**
```python
otp = YubiOtpSession(conn)
r = otp.calculate_hmac_sha1(SLOT.TWO, challenge)
```

**yubikit-android**
```java
YubiOtpSession otp = new YubiOtpSession(otpConnection);
byte[] r = otp.calculateHmacSha1(Slot.TWO, challenge, null);
```

</div>

**Delta:** the one applet where all four SDKs agree almost exactly — same operation,
same slot enum, same argument order. The .NET difference is that its session is
**dual-transport and prefers SmartCard** (`SmartCard → HidOtp`), so on a CCID-enabled
key this runs over APDU, not HID. `yubikit-swift` has no YubiOTP session at all.

<!-- Anchors: .NET src/YubiOtp/src/IYubiKeyExtensions.cs:102,
     transport order :144-145, CalculateHmacSha1Async PublicAPI.Unshipped.txt:72;
     python yubikit/yubiotp.py:708, calculate_hmac_sha1 :901;
     android YubiOtpSession.java:253,447;
     rust crates/yubikit/src/yubiotp.rs:1165 (calculate_hmac_sha1);
     swift@1.4.0 absent Capability.swift:20 -->
