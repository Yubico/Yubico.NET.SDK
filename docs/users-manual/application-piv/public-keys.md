---
uid: UsersManualPublicKeys
---

<!-- Copyright 2021 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# Public key handling

The Yubico .NET SDK provides type-safe public key classes implementing `IPublicKey` for working with keys generated on or imported to YubiKeys.

## Supported key types

- **RSA keys**: 1024, 2048, 3072, 4096-bit
- **EC keys**: NIST P-256, P-384 curves
- **Curve25519 keys**: Ed25519 (signing), X25519 (key agreement)

## Factory methods

### RSA public keys

```csharp
// From SubjectPublicKeyInfo format
byte[] spkiBytes = // your SubjectPublicKeyInfo bytes
RSAPublicKey rsaKey = RSAPublicKey.CreateFromSubjectPublicKeyInfo(spkiBytes);

// From RSA parameters
using var rsa = RSA.Create(2048);
RSAParameters parameters = rsa.ExportParameters(false); // public only
RSAPublicKey rsaKey = RSAPublicKey.CreateFromParameters(parameters);
```

### EC public keys

```csharp
// From SubjectPublicKeyInfo format
byte[] spkiBytes = // your SubjectPublicKeyInfo bytes
ECPublicKey ecKey = ECPublicKey.CreateFromSubjectPublicKeyInfo(spkiBytes);

// From EC parameters
using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
ECParameters parameters = ecdsa.ExportParameters(false); // public only
ECPublicKey ecKey = ECPublicKey.CreateFromParameters(parameters);

// From public point (0x04 || X-coordinate || Y-coordinate)
ReadOnlyMemory<byte> publicPoint = [/* your uncompressed EC point */]
ECPublicKey ecKey = ECPublicKey.CreateFromValue(publicPoint, KeyType.ECP256);
```

### Curve25519 public keys

```csharp
// From SubjectPublicKeyInfo format
byte[] spkiBytes = // your SubjectPublicKeyInfo bytes
Curve25519PublicKey curve25519Key = Curve25519PublicKey.CreateFromSubjectPublicKeyInfo(spkiBytes);

// From public point bytes
ReadOnlyMemory<byte> publicPoint = [/* your uncompressed EC point */];
Curve25519PublicKey ed25519Key = Curve25519PublicKey.CreateFromValue(publicPoint, KeyType.Ed25519);
Curve25519PublicKey x25519Key = Curve25519PublicKey.CreateFromValue(publicPoint, KeyType.X25519);
```

## Generating key pairs

```csharp
using var pivSession = new PivSession(yubiKey);
pivSession.KeyCollector = yourKeyCollector;
pivSession.AuthenticateManagementKey();

// Generate returns the public key
IPublicKey publicKey = pivSession.GenerateKeyPair(
    PivSlot.Authentication,
    KeyType.Ed25519,
    PivPinPolicy.Once,
    PivTouchPolicy.Never);

// Type-check for specific key properties
if (publicKey is Curve25519PublicKey ed25519Key)
{
    Console.WriteLine("Generated public key: " + ed25519Key.PublicPoint);
}
```

## Exporting public keys

```csharp
// Export to SubjectPublicKeyInfo format (standard)
byte[] spkiBytes = publicKey.ExportSubjectPublicKeyInfo();

// Access key-specific properties
if (publicKey is RSAPublicKey rsaKey)
{
    RSAParameters rsaParams = rsaKey.Parameters;
    byte[] modulus = rsaParams.Modulus;
    byte[] exponent = rsaParams.Exponent;
}

if (publicKey is ECPublicKey ecKey)
{
    ECParameters ecParams = ecKey.Parameters;
    ReadOnlyMemory<byte> publicPoint = ecKey.PublicPoint; // 0x04 || X || Y format
    byte[] xCoord = ecParams.Q.X;
    byte[] yCoord = ecParams.Q.Y;
}

if (publicKey is Curve25519PublicKey curve25519Key)
{
    ReadOnlyMemory<byte> publicPoint = curve25519Key.PublicPoint;
}
```

## PEM format conversion

```csharp
// Export to PEM format
byte[] spkiBytes = publicKey.ExportSubjectPublicKeyInfo();
string pemKey = "-----BEGIN PUBLIC KEY-----\n" +
                Convert.ToBase64String(spkiBytes, Base64FormattingOptions.InsertLineBreaks) +
                "\n-----END PUBLIC KEY-----";

// Import from PEM format
string pemData = // your PEM PUBLIC KEY
string base64Data = pemData
    .Replace("-----BEGIN PUBLIC KEY-----\n", "")
    .Replace("\n-----END PUBLIC KEY-----", "");
byte[] spkiBytes = Convert.FromBase64String(base64Data);
IPublicKey publicKey = RSAPublicKey.CreateFromSubjectPublicKeyInfo(spkiBytes); // or ECPublicKey, etc.
```

## Error handling

Factory methods validate input and may throw exceptions:

```csharp
try
{
    var publicKey = ECPublicKey.CreateFromValue(publicPoint, KeyType.ECP256);
}
catch (ArgumentException ex)
{
    // Handle invalid parameters, key type mismatches, or malformed data
}
catch (CryptographicException ex)
{
    // Handle invalid key format or cryptographic validation failures
}
```

## References

- [RFC 5280](https://tools.ietf.org/html/rfc5280) - SubjectPublicKeyInfo format
- [RFC 7468](https://tools.ietf.org/html/rfc7468) - PEM encoding
- [RFC 7748](https://tools.ietf.org/html/rfc7748) - Curve25519 and Curve448
- [SDK PIV integration tests](https://github.com/Yubico/Yubico.NET.SDK/tree/main/Yubico.YubiKey/tests/integration/Yubico/YubiKey/Piv)
- SDK unit tests for additional usage examples:
  - [Curve25519PublicKeyTests](https://github.com/Yubico/Yubico.NET.SDK/blob/develop/Yubico.YubiKey/tests/unit/Yubico/YubiKey/Cryptography/Curve25519PublicKeyTests.cs)
  - [ECPublicKeyTests](https://github.com/Yubico/Yubico.NET.SDK/blob/develop/Yubico.YubiKey/tests/unit/Yubico/YubiKey/Cryptography/ECPublicKeyTests.cs)
  - [RSAPublicKeyTests](https://github.com/Yubico/Yubico.NET.SDK/blob/develop/Yubico.YubiKey/tests/unit/Yubico/YubiKey/Cryptography/RSAPublicKeyTests.cs)
