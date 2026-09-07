## OpenPGP

Card data, key slots, sign / decrypt / authenticate, PIN management.

```csharp
await using var pgp = await key.CreateOpenPgpSessionAsync();
var data = await pgp.GetApplicationRelatedDataAsync();
```

<div class="cols">

**ykman (Python)**
```python
pgp = OpenPgpSession(conn)
data = pgp.get_application_related_data()
```

**yubikit-android**
```java
OpenPgpSession pgp = new OpenPgpSession(conn);
ApplicationRelatedData d = pgp.getApplicationRelatedData();
```

</div>

**Delta:** `yubikit-swift` has **no OpenPGP session at all** — only a `Capability.openPGP`
bit. If you work in Swift, this applet is entirely new to you.

<!-- Anchors: .NET src/OpenPgp/src/IYubiKeyExtensions.cs:37,
     PublicAPI.Unshipped.txt:181; python yubikit/openpgp.py:987,1088;
     android OpenPgpSession.java:154,266; swift@1.4.0 absent, Capability.swift:24 -->

---

## FIDO2 — the CTAP2 layer

Raw CTAP2: `GetInfo`, `MakeCredential`, `GetAssertion`, credential management, bio.

```csharp
await using var fido = await key.CreateFidoSessionAsync();
var info = await fido.GetInfoAsync();
```

<div class="cols">

**python-fido2**
```python
ctap2 = Ctap2(dev)
info = ctap2.info      # cached at construction
```

**yubikit-swift**
```swift
let s = try await CTAP2.Session.makeSession(connection: connection)
let info = try await s.getInfo()
```

</div>

**Delta:** `python-fido2` runs `GET_INFO` **inside the constructor** and caches it; .NET
keeps construction cheap and the round-trip explicit and cancellable. Note FIDO2 is
dual-transport: `HidFido` first, then SmartCard — NFC, or USB-CCID on **firmware 5.8.0+**.

<!-- Anchors: .NET src/Fido2/src/IYubiKeyExtensions.cs:124,
     PublicAPI.Unshipped.txt:609, transports :189-190, FW5.8 :95;
     python-fido2 fido2/ctap2/base.py:246-262 (get_info at :252), :304-309;
     swift@1.4.0 CTAPSession+Creation.swift:25, CTAPSession.swift:49 -->

---

## WebAuthn — the layer above CTAP2

Origin checks, client data, attestation, extensions, PIN/UV orchestration.

```csharp
_ = WebAuthnOrigin.TryParse("https://example.com", out var origin);
await using var client = await key.CreateWebAuthnClientAsync(
    origin!, isPublicSuffix: d => d is "com" or "org" or "net");
var reg = await client.MakeCredentialAsync(options, pinBytes);
```

<div class="cols">

**python-fido2**
```python
result = client.make_credential(create_options["publicKey"])
```

**yubikit-swift** *(release/1.4.0)*
```swift
let client = WebAuthn.Client(session: session, origin: try .init("https://example.com"),
                             isPublicSuffix: { publicSuffixList.contains($0) })
let r = try await client.makeCredential(options, authorization: .pin("1234")).value
```

</div>

**Delta:** both .NET and Swift demand an **origin and a public-suffix checker up front** —
you cannot accidentally skip origin validation. `python-fido2` also ships the
relying-party half (`Fido2Server`); v2 is client-side only.

<!-- Anchors: .NET src/WebAuthn/src/IYubiKeyExtensions.cs:57-62,
     PublicAPI.Unshipped.txt:111-112, real call site
     src/WebAuthn/tests/.../WebAuthnClientFactoryTests.cs:42-45;
     python-fido2 fido2/client/__init__.py:1066-1179, fido2/server.py:158;
     swift@1.4.0 FIDO/WebAuthn/Client/Client.swift:34-41 (doc), :95 (init) -->
