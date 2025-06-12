---
uid: UsersManualPrivateKeys
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

# Private key handling

The Yubico .NET SDK supports importing and exporting private keys in standard formats using type-safe factory methods. All private key classes implement `IPrivateKey` and provide secure memory handling.

## Supported key types

- **RSA keys**: 1024, 2048, 3072, 4096-bit
- **EC keys**: NIST P-256, P-384 curves
- **Curve25519 keys**: Ed25519 (signing), X25519 (key agreement)

## Factory methods

### RSA private keys

```csharp
// From PKCS#8 PrivateKeyInfo format
byte[] pkcs8Bytes = // your PKCS#8 encoded key
RSAPrivateKey rsaKey = RSAPrivateKey.CreateFromPkcs8(pkcs8Bytes);

// From RSA parameters
using var rsa = RSA.Create(2048);
RSAParameters parameters = rsa.ExportParameters(true);
RSAPrivateKey rsaKey = RSAPrivateKey.CreateFromParameters(parameters);

// From CRT parameters only
var crtParameters = new RSAParameters
{
    P = // prime P,
    Q = // prime Q,
    DP = // exponent P,
    DQ = // exponent Q,
    InverseQ = // coefficient
};
RSAPrivateKey rsaKey = RSAPrivateKey.CreateFromParameters(crtParameters);
```

### EC private keys

```csharp
// From PKCS#8 PrivateKeyInfo format
byte[] pkcs8Bytes = // your PKCS#8 encoded key
ECPrivateKey ecKey = ECPrivateKey.CreateFromPkcs8(pkcs8Bytes);

// From EC parameters
using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
ECParameters parameters = ecdsa.ExportParameters(true);
ECPrivateKey ecKey = ECPrivateKey.CreateFromParameters(parameters);

// From private scalar value
ReadOnlyMemory<byte> privateValue = // your private scalar
ECPrivateKey ecKey = ECPrivateKey.CreateFromValue(privateValue, KeyType.ECP256);
```

### Curve25519 private keys

```csharp
// From PKCS#8 PrivateKeyInfo format
byte[] pkcs8Bytes = // your PKCS#8 encoded key
Curve25519PrivateKey ed25519Key = Curve25519PrivateKey.CreateFromPkcs8(pkcs8Bytes);

// From private key bytes
ReadOnlyMemory<byte> privateKeyBytes = // your 32-byte private key
Curve25519PrivateKey ed25519Key = Curve25519PrivateKey.CreateFromValue(privateKeyBytes, KeyType.Ed25519);
Curve25519PrivateKey x25519Key = Curve25519PrivateKey.CreateFromValue(privateKeyBytes, KeyType.X25519);
```

## Importing to YubiKey

```csharp
using var pivSession = new PivSession(yubiKey);
pivSession.KeyCollector = yourKeyCollector;
pivSession.AuthenticateManagementKey();

// Import any IPrivateKey implementation
pivSession.ImportPrivateKey(
    PivSlot.Authentication,
    privateKey,
    PivPinPolicy.Once,
    PivTouchPolicy.Never);
```

## Exporting private keys

```csharp
// Export to PKCS#8 format
byte[] pkcs8Bytes = rsaKey.ExportPkcs8PrivateKey();
byte[] pkcs8Bytes = ecKey.ExportPkcs8PrivateKey();

// Access key-specific properties
RSAParameters rsaParams = rsaKey.Parameters;
ECParameters ecParams = ecKey.Parameters;
ReadOnlyMemory<byte> curve25519Bytes = curve25519Key.PrivateKey;
```

## PEM format conversion

```csharp
// Export to PEM format
byte[] pkcs8Bytes = privateKey.ExportPkcs8PrivateKey();
string pemKey = "-----BEGIN PRIVATE KEY-----\n" +
                Convert.ToBase64String(pkcs8Bytes, Base64FormattingOptions.InsertLineBreaks) +
                "\n-----END PRIVATE KEY-----";

// Import from PEM format
string pemData = // your PEM PRIVATE KEY
string base64Data = pemData
    .Replace("-----BEGIN PRIVATE KEY-----\n", "")
    .Replace("\n-----END PRIVATE KEY-----", "");
byte[] pkcs8Bytes = Convert.FromBase64String(base64Data);
IPrivateKey privateKey = RSAPrivateKey.CreateFromPkcs8(pkcs8Bytes); // or ECPrivateKey, etc.
```

## Secure memory handling

All private key classes implement secure cleanup and disposal patterns:

```csharp
// Using disposable pattern (recommended)
using (var privateKey = RSAPrivateKey.CreateFromPkcs8(pkcs8Bytes))
{
    // Use the private key
    pivSession.ImportPrivateKey(PivSlot.Authentication, privateKey, PivPinPolicy.Once, PivTouchPolicy.Never);
} // Sensitive data automatically cleared

// Explicit cleanup
ECPrivateKey privateKey = null;
try
{
    privateKey = ECPrivateKey.CreateFromPkcs8(pkcs8Bytes);
    // Use the private key
}
finally
{
    privateKey?.Clear(); // Securely zero sensitive data
}
```

## Error handling

Factory methods validate input and may throw exceptions:

```csharp
try
{
    var privateKey = Curve25519PrivateKey.CreateFromValue(keyBytes, KeyType.X25519);
}
catch (CryptographicException ex)
{
    // Handle invalid key format or bit clamping violations
}
catch (ArgumentException ex)
{
    // Handle invalid parameters or key type mismatches
}
```

## References

- [RFC 5208](https://tools.ietf.org/html/rfc5208) - PKCS#8 Private Key format
- [RFC 7748](https://tools.ietf.org/html/rfc7748) - Curve25519 and Curve448
- [RFC 7468](https://tools.ietf.org/html/rfc7468) - PEM encoding
- [SDK PIV integration tests](https://github.com/Yubico/Yubico.NET.SDK/tree/main/Yubico.YubiKey/tests/integration/Yubico/YubiKey/Piv)
- SDK unit tests for additional usage examples
  - [Curve25519PrivateKeyTests](https://github.com/Yubico/Yubico.NET.SDK/blob/main/Yubico.YubiKey/tests/unit/Yubico/YubiKey/Cryptography/Curve25519PrivateKeyTests.cs)
  - [ECPrivateKeyTests](https://github.com/Yubico/Yubico.NET.SDK/blob/main/Yubico.YubiKey/tests/unit/Yubico/YubiKey/Cryptography/ECPrivateKeyTests.cs)
  - [RSAPrivateKeyTests](https://github.com/Yubico/Yubico.NET.SDK/blob/main/Yubico.YubiKey/tests/unit/Yubico/YubiKey/Cryptography/RSAPrivateKeyTests.cs)
