---
uid: UsersManualScp
---

<!-- Copyright 2024 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->


# Secure Channel Protocol (SCP)

Commands sent to the YubiKey, or responses from the YubiKey, may contain sensitive data that should not leak to or be tampered with by other applications on the host machine. While the operating system provides some protection through memory space isolation and permissioned access, YubiKeys support additional layers of protection through Secure Channel Protocols (SCP).

These protocols, defined by <a href="https://globalplatform.org/">GlobalPlatform</a>, provide confidentiality and integrity of communication between the host and YubiKey. 
This standard prescribes methods to encrypt and authenticate smart card
(CCID) messages. That is, APDUs and responses are encrypted and contain checksums. If executed properly, the only entities that can see the contents of the messages (and
verify their correctness) will be the YubiKey itself and authorized applications.
The YubiKey supports two main variants of SCP:

- **SCP03** - A symmetric key protocol using AES-128 for encryption and authentication
- **SCP11** - An asymmetric protocol using elliptic curve cryptography (ECC) and X.509-certificates

## Protocol overview

### SCP03 (symmetric)
SCP03 provides a secure channel using shared secret keys. It is simpler to implement but requires secure key distribution. Think of SCP03 as wrapping commands and responses in an encrypted envelope that only trusted parties can open.

Key characteristics:
- Uses AES-128 symmetric keys
- Three keys per set: encryption, MAC, and data encryption
- Supported on YubiKey 5 Series with firmware 5.3+

### SCP11 (asymmetric)
SCP11 uses public key cryptography for authentication and key agreement. It provides stronger security guarantees and simpler key management, but with more complex implementation. SCP11 comes in three variants:

- **SCP11a** - Mutual authentication between YubiKey and host
- **SCP11b** - YubiKey authenticates to host only
- **SCP11c** - Mutual authentication with additional features, such as offline scripting usage (See [GlobalPlatform SCP11 Specification Annex B](https://globalplatform.org/specs-library/secure-channel-protocol-11-amendment-f/))

Key characteristics:
- Uses NIST P-256 elliptic curve
- Certificate-based authentication
- Supported on YubiKey 5 Series with firmware 5.7.2+

## When to use secure channels

Secure channels are particularly valuable when:

- Communicating with YubiKeys over networks or untrusted channels (e.g. NFC)
- Managing YubiKeys remotely through card management systems
- Ensuring end-to-end security beyond transport encryption

For example, if you tunnel YubiKey commands over the Internet, you might use TLS for transport security and add SCP as an additional layer of defense.

## Security considerations

SCP03 relies entirely on symmetric cryptography, making key distribution a critical security concern. Most YubiKeys ship with default SCP03 keys that are publicly known - using these provides no additional security over cleartext communication.

SCP11, being asymmetric, simplifies key management but requires proper certificate handling and validation. Each variant (a/b/c) offers different security properties suitable for different use cases.

 It is possible to manufacture YubiKeys with custom non-default SCP key sets (this requires a custom order - contact your Yubico sales representative for details).

The following sections detail how to implement both protocols, manage keys and certificates, and integrate secure channels with various YubiKey applications.

## Using secure channels with YubiKey applications

The SDK provides a consistent way to use secure channels across different YubiKey applications. You can enable secure channel communication by providing SCP key parameters when creating application sessions.

### Common pattern

Each application session (PIV, OATH, OTP, YubiHSM Auth) accepts an optional `ScpKeyParameters` parameter. This can be either `Scp03KeyParameters` or `Scp11KeyParameters` depending on which protocol you want to use.

```csharp
// Using SCP03
using var scp03Params = Scp03KeyParameters.DefaultKey;  // For testing only
using var pivSession = new PivSession(yubiKeyDevice, scp03Params);

// Using SCP11b
using var sdSession = new SecurityDomainSession(yubiKeyDevice, scp03Params);

// Create SCP11b key parameters from public key on YubiKey
var keyVersionNumber = 0x1; // Example kvn
var keyId = ScpKeyIds.SCP11B;
var keyReference = KeyReference.Create(keyId, keyVersionNumber);

// Get certificate from YubiKey
var certificates = sdSession.GetCertificates(keyReference); 

// Verify the Yubikey's certificate chain against a trusted root using your implementation
CertificateChainVerifier.Verify(certificates)

// Use the verified leaf certificate to construct ECPublicKeyParameters
var publicKey = certificates.Last().GetECDsaPublicKey();
var scp11Params = new Scp11KeyParameters(keyReference, new ECPublicKeyParameters(publicKey));

// Use SCP11b parameters to open connection
using (var pivSession = new PivSession(yubiKeyDevice, scp11Params))
{
    // All PivSession-commands are now automatically protected by SCP11
    pivSession.GenerateKeyPair(PivSlot.Retired12, PivAlgorithm.EccP256, PivPinPolicy.Always); // Protected by SCP11
}
```

### Application examples

#### PIV with secure channel

```csharp
// Using SCP03
StaticKeys scp03Keys = RetrieveScp03KeySet();  // Your static keys
using Scp03KeyParameters scp03Params = Scp03KeyParameters.FromStaticKeys(scp03Keys);
using (var pivSession = new PivSession(yubiKeyDevice, scp03Params))
{
    // All PivSession-commands are now automatically protected by SCP03
}

// Using SCP11b
// Retrieve public key from YubiKey (see full example above for certificate verification)
var keyVersionNumber = 0x1; // Example kvn
var keyReference = KeyReference.Create(ScpKeyIds.Scp11B, keyVersionNumber);
var publicKey = GetScp11PublicKey(yubiKeyDevice, keyReference); // Your implementation
var scp11Params = new Scp11KeyParameters(keyReference, new ECPublicKeyParameters(publicKey));
using (var pivSession = new PivSession(yubiKeyDevice, scp11Params))
{
    // All PivSession-commands are now automatically protected by SCP11
}
```

#### OATH with secure channel
```csharp

// Using SCP03
StaticKeys scp03Keys = RetrieveScp03KeySet();  // Your static keys
using Scp03KeyParameters scp03Params = Scp03KeyParameters.FromStaticKeys(scp03Keys);
using (var oathSession = new OathSession(yubiKeyDevice, scp03Params))
{
    // All oathSession-commands are now automatically protected by SCP03
}

// Using SCP11b
// Retrieve public key from YubiKey (see full example above for certificate verification)
var keyVersionNumber = 0x1; // Example kvn
var keyReference = KeyReference.Create(ScpKeyIds.Scp11B, keyVersionNumber);
var publicKey = GetScp11PublicKey(yubiKeyDevice, keyReference); // Your implementation
var scp11Params = new Scp11KeyParameters(keyReference, new ECPublicKeyParameters(publicKey));
using (var oathSession = new OathSession(yubiKeyDevice, scp11Params))
{
    // All OathSession-commands are now automatically protected by SCP11
}
```

#### OTP with secure channel
```csharp

// Using SCP03
StaticKeys scp03Keys = RetrieveScp03KeySet();  // Your static keys
using Scp03KeyParameters scp03Params = Scp03KeyParameters.FromStaticKeys(scp03Keys); 
using (var otpSession = new OtpSession(yubiKeyDevice, scp03params))
{
    // All otpSession-commands are now automatically protected by SCP03
}

// Using SCP11b
var keyReference = KeyReference.Create(ScpKeyIds.Scp11B, kvn);
using (var otpSession = new OtpSession(yubiKeyDevice, scp11Params))
{
    // All OtpSession-commands are now automatically protected by SCP11
}
```

#### YubiHSM Auth with secure channel
```csharp
// Using SCP03
StaticKeys scp03Keys = RetrieveScp03KeySet();  // Your static keys
using Scp03KeyParameters scp03Params = Scp03KeyParameters.FromStaticKeys(scp03Keys); 
using (var yubiHsmSession = new YubiHsmAuthSession(yubiKeyDevice, scp03params))
{
    // All YubiHsmSession-commands are now automatically protected by SCP03
}

// Using SCP11b
var keyReference = KeyReference.Create(ScpKeyIds.Scp11B, kvn);
using (var yubiHsmSession = new YubiHsmSession(yubiKeyDevice, scp11Params))
{
    // All yubiHsmSession-commands are now automatically protected by SCP11
}

```

### Direct connection

If you need lower-level control, you can establish secure connections directly using [`Connect`](xref:Yubico.YubiKey.IYubiKeyDevice.Connect*):

```csharp
// Using application ID
using var connection = yubiKeyDevice.Connect(
    applicationId,  // byte array for ISO7816 applicationId
    Scp03KeyParameters.DefaultKey);

// Using YubiKeyApplication enum
using var connection = yubiKeyDevice.Connect(
    YubiKeyApplication.Piv,
    scp11Parameters);

// Try pattern
if (yubiKeyDevice.TryConnect(
    YubiKeyApplication.Oath,
    scpParameters,
    out var connection))
{
    using (connection)
    {
        // Use connection
    }
}
```

### Security Domain management

The [`SecurityDomainSession`](xref:Yubico.YubiKey.Scp.SecurityDomainSession) class provides methods to manage SCP configurations:

```csharp
using var session = new SecurityDomainSession(yubiKeyDevice, Scp03KeyParameters.DefaultKey);

// Get information about installed keys
var keyInfo = session.GetKeyInformation();

// Store certificates for SCP11
session.StoreCertificates(keyReference, certificates);

// Manage allowed certificate serials
session.StoreAllowlist(keyReference, allowedSerials);

// Import private key
session.PutKey(keyReference, privateKeyParameters)

// Import public key
session.PutKey(keyReference, publicKeyParameters)

// Reset to factory defaults
session.Reset();
```

> [!NOTE]
> Using `DefaultKey` in production code provides no security. Always use proper key management in production environments.

The next sections will detail specific key management and protocol details for both SCP03 and SCP11.

## SCP03 (symmetric key protocol)

### Static keys structure

SCP03 relies on a set of shared, secret, symmetric cryptographic keys. Each key set consists of three 16-byte AES-128 keys encapsulated in the [`StaticKeys`](xref:Yubico.YubiKey.Scp.StaticKeys) class:

- Channel encryption key (Key-ENC)
- Channel MAC key (Key-MAC) 
- Data encryption key (Key-DEK)

These keys are encapsulated in a `StaticKeys` class and provided to the SDK via `Scp03KeyParameters`:

```csharp
var staticKeys = new StaticKeys(keyDataMac, keyDataEnc, keyDataDek);
var scp03Params = new Scp03KeyParameters(ScpKeyIds.Scp03, 0x01, staticKeys);
```

### Key sets on the YubiKey

A YubiKey can contain up to three SCP03 key sets. Each set is identified by a Key Version Number (KVN):

```txt
   slot 1:   ENC   MAC   DEK    (KVN=1)
   slot 2:   ENC   MAC   DEK    (KVN=2) 
   slot 3:   ENC   MAC   DEK    (KVN=3)
```

Standard YubiKeys are manufactured with a default key set (KVN=0xFF):

```txt
   slot 1:   ENC(default)  MAC(default)  DEK(default)
   slot 2:   --empty--
   slot 3:   --empty--
```

> [!IMPORTANT]
> The default keys are publicly known (0x40 41 42 ... 4F) and provide no security. You should replace them in production environments.

### Managing key sets

Use `SecurityDomainSession` to manage SCP03 key sets:

```csharp
// Replace default keys
using var session = new SecurityDomainSession(yubiKeyDevice, Scp03KeyParameters.DefaultKey);
var newKeys = new StaticKeys(newKeyDataMac, newKeyDataEnc, newKeyDataDek);
var newKeyParams = new Scp03KeyParameters(ScpKeyIds.Scp03, 0x01, newKeys);
session.PutKey(newKeyParams.KeyReference, newKeyParams.StaticKeys);

// Add another key set
using var session = new SecurityDomainSession(yubiKeyDevice, existingKeyParams);
var keyRef = KeyReference.Create(ScpKeyIds.Scp03, 0x02);  // KVN=2
session.PutKey(keyRef, additionalKeys);

// Delete a key set
session.DeleteKey(keyRef, false);

// Reset to factory defaults (restore default keys)
session.Reset();
```

### Key set rules

1. **Key Version Numbers (KVN):**
   - Default key set: KVN=0xFF
   - Accepted values are between 1 and 0x7F

2. **Key Id's (KID)**
   - Default value: 1
   - Accepted values are between 1 and 3

3. **Default Key Replacement:**
   - When adding first custom key set, default keys are always removed
   - Cannot retain default keys alongside custom keys

4. **Multiple Key Sets:**
   - After default keys are replaced, can have 1-3 custom key sets
   - Each must have unique KVN
   - Can add/remove without affecting other sets

### Example: complete key management flow

```csharp
// Start with default keys
var defaultScp03Params = Scp03KeyParameters.DefaultKey;
var firstKvn = 0x1;
var keyRef1 = KeyReference.Create(ScpKeyIds.Scp03, firstKvn);
using (var session = new SecurityDomainSession(yubiKeyDevice, defaultScp03Params))
{
    // Add first custom key set (removes default)
    session.PutKey(keyRef1, newKeys);
}

// Now authenticate with new keys
var newScp03Params = new Scp03KeyParameters(keyRef1, newKeys);
using (var session = new SecurityDomainSession(yubiKeyDevice, newScp03Params))
{
    // Add second key set
    var secondKvn = 0x2;
    var keyRef2 = KeyReference.Create(ScpKeyIds.Scp03, secondKvn);
    session.PutKey(keyRef2, customKeys2);

    // Check current key information
    var keyInfo = session.GetKeyInformation();
}
```

### Key management responsibilities

You should:
- Track which keys are loaded on each YubiKey
- Track KVNs in use
- Know if a YubiKey has custom keys from manufacturing
- Handle key rotation

The YubiKey provides no metadata about installed keys beyond what's available through `GetKeyInformation()`.

> [!NOTE]
> Always use proper key management in production. Never store sensitive keys in source code or configuration files.

## SCP11 (asymmetric key protocol)

SCP11 uses asymmetric cryptography based on elliptic curves (NIST P-256) for authentication and key agreement. Compared to SCP03, it uses certificates instead of pre-shared keys, providing greater flexibility in cases where the two entities setting up the secure channel are not deployed in strict pairs. The secure channel can be embedded into complex use cases, such as:
- Installation of payment credentials on wearables
- Production systems
- Remote provisioning of cell phone subscriptions

Detailed information about SCP11 can be found in [GlobalPlatform Card Technology, Secure Channel Protocol '11' Card Specification v2.3 – Amendment F, Chapter 2](https://globalplatform.org/specs-library/secure-channel-protocol-11-amendment-f/)

It comes in three variants, each offering different security properties:

### SCP11 variants

- **SCP11a**: Full mutual authentication between host and YubiKey using certificates
  - Basic mutual authentication
  - Uses both static and ephemeral key pairs
  - Requires certificate chain and off-card entity (OCE) verification
  - Supports authorization rules in OCE certificates
  - Suitable for direct host-to-YubiKey communication

- **SCP11b**: YubiKey authenticates to host only
  - Simplest variant, no mutual authentication
  - Uses both static and ephemeral key pairs
  - Suitable when host authentication isn't required

- **SCP11c**: Enhanced mutual authentication with additional features
  - Uses both static and ephemeral key pairs
  - Supports offline scripting mode:
    - Can precompute personalization scripts for groups of cards
    - Scripts can be deployed via online services or companion apps
    - Cryptographic operations remain on secure OCE server
  - Supports authorization rules in OCE certificates

### Key benefits of SCP11 over SCP03

SCP11 provides several advantages over SCP03:
- Uses certificates instead of pre-shared keys for authentication
- More flexible deployment - doesn't require strict pairing of entities
- Supports ECC for key establishment with AES-128
- Better suited for complex deployment scenarios

### Key parameters

Unlike SCP03's static keys, SCP11 uses `Scp11KeyParameters` which can contain:
- Public/private key pairs
- Certificates
- Key references
- Off-card entity (OCE) information

```csharp
// SCP11b basic parameters
var keyReference = KeyReference.Create(ScpKeyIds.Scp11B, 0x1);
var scp11Params = new Scp11KeyParameters(
    keyReference,
    new ECPublicKeyParameters(publicKey));

// SCP11a/c with full certificate chain
var scp11Params = new Scp11KeyParameters(
    keyReference,           // Key reference for this connection
    pkSdEcka,              // Public key for key agreement
    oceKeyReference,        // Off-card entity reference
    skOceEcka,             // Private key for key agreement
    certificateChain);      // Certificate chain for authentication
```

### Key management

Use `SecurityDomainSession` to manage SCP11 keys and certificates:

```csharp
using var session = new SecurityDomainSession(yubiKeyDevice, Scp03KeyParameters.DefaultKey);

// Generate new EC key pair
var keyReference = KeyReference.Create(ScpKeyIds.Scp11B, 0x3);
var publicKey = session.GenerateEcKey(keyReference);

// Import existing key pair
var privateKey = new ECPrivateKeyParameters(ecdsa);
session.PutKey(keyReference, privateKey);

// Store certificates
session.StoreCertificates(keyReference, certificates);

// Manage certificate serial number allowlist
var serials = new List<string> { 
    "7F4971B0AD51F84C9DA9928B2D5FEF5E16B2920A", // Examples
    "6B90028800909F9FFCD641346933242748FBE9AD"
};
session.StoreAllowlist(oceKeyReference, serials);
```

### SCP11b example

Simplest variant, where YubiKey authenticates to host:

```csharp
// Get certificates stored on YubiKey
var keyReference = KeyReference.Create(ScpKeyIds.Scp11B, 0x1);
IReadOnlyCollection<X509Certificate2> certificateList;
using (var session = new SecurityDomainSession(yubiKeyDevice))
{
    certificateList = session.GetCertificates(keyReference);
}

// Verify the certificate chain against a trusted root using your implementation
CertificateChainVerifier.Verify(certificateList)

// Create parameters using leaf certificate which has now been verified
var leaf = certificateList.Last();
var ecDsaPublicKey = leaf.PublicKey.GetECDsaPublicKey()!.ExportParameters(false);
var keyParams = new Scp11KeyParameters(
    keyReference, 
    new ECPublicKeyParameters(ecDsaPublicKey));

// Use with any application
using var pivSession = new PivSession(yubiKeyDevice, keyParams);
```

### SCP11a example

Full mutual authentication requires more setup:

```csharp
// Start with default SCP03 connection
using var session = new SecurityDomainSession(yubiKeyDevice, Scp03KeyParameters.DefaultKey);

const byte kvn = 0x03;
var keyRef = KeyReference.Create(ScpKeyIds.Scp11A, kvn);

// Generate new key pair on YubiKey
var newPublicKey = session.GenerateEcKey(keyRef);

// Setup off-card entity (OCE)
var oceRef = KeyReference.Create(OceKid, kvn);
var ocePublicKey = new ECPublicKeyParameters(oceCerts.Ca.PublicKey.GetECDsaPublicKey());
session.PutKey(oceRef, ocePublicKey);

// Store CA identifier
var ski = GetSubjectKeyIdentifier(oceCerts.Ca);
session.StoreCaIssuer(oceRef, ski);

// Create SCP11a parameters
var scp11Params = new Scp11KeyParameters(
    keyRef,
    new ECPublicKeyParameters(newPublicKey.Parameters),
    oceRef,
    new ECPrivateKeyParameters(privateKey),
    certChain);

// Use the secure connection
using var session = new SecurityDomainSession(yubiKeyDevice, scp11Params);
```

### Security considerations

1. **Certificate Management:**
   - Proper certificate validation is crucial
   - Consider using certificate allowlists
   - Manage certificate chains carefully

2. **Key Generation:**
   - Can generate keys on YubiKey or import existing
   - YubiKey-generated keys never leave the device
   - Imported keys must be properly protected

3. **Protocol Selection:**
   - SCP11b: Simplest variant, no mutual authentication
   - SCP11a: Better security through mutual authentication
   - SCP11c: Additional features over SCP11a

4. **Certificate Allowlists:**
   - Restrict which certificates can authenticate
   - Update lists as certificates change
   - Can be used as a part of a certificate revocation strategy

### Checking SCP support

```csharp
// Check firmware version for SCP11 support
if (yubiKeyDevice.HasFeature(YubiKeyFeature.Scp11))
{
    // Device supports SCP11
}

// Get information about installed keys
using var session = new SecurityDomainSession(yubiKeyDevice);
var keyInfo = session.GetKeyInformation();

// Get supported CA identifiers
var caIds = session.GetSupportedCaIdentifiers(true, true);
```

> [!NOTE]
> SCP11 requires firmware version 5.7.2 or later. Earlier firmware versions only support SCP03.

# Additional documentation
- [Global Platform Consortium](https://globalplatform.org/)
- [GlobalPlatform SCP11 Specification](https://globalplatform.org/specs-library/secure-channel-protocol-11-amendment-f/)