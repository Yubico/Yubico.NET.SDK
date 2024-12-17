---
uid: SecurityDomainCertificates
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

# Security Domain certificate management

The Security Domain manages X.509 certificates primarily for SCP11 protocol operations. These certificates are used for authentication and establishing secure channels. For detailed information about certificate usage in secure channels, see the [Secure Channel Protocol (SCP)](xref:UsersManualScp) documentation.

## Certificate operations

### Storing certificates

Certificates are stored in chains associated with specific key references. A typical certificate chain includes:

- Root CA certificate
- Intermediate certificates (optional)
- Leaf (end-entity) certificate

```csharp
// Store certificate chain
var certificates = new List<X509Certificate2>
{
    rootCert,
    intermediateCert,
    leafCert
};
session.StoreCertificates(keyReference, certificates);
```

### CA configuration

For SCP11a and SCP11c, you need to configure the Certificate Authority (CA) information:

```csharp
// Store CA issuer information using Subject Key Identifier (SKI)
byte[] subjectKeyIdentifier = GetSkiFromCertificate(caCert);
session.StoreCaIssuer(keyReference, subjectKeyIdentifier);
```

### Retrieving certificates

```csharp
// Get all certificates for a key reference
var certificates = session.GetCertificates(keyReference);

// Get leaf certificate (last in chain)
var leafCert = certificates.Last();

// Get supported CA identifiers
var caIds = session.GetSupportedCaIdentifiers(
    kloc: true,   // Key Loading OCE Certificate
    klcc: true    // Key Loading Card Certificate
);
```

## Access control

### Certificate allowlists

Use of the allowlist can provide the following benefits: 
- A strong binding to one (or multiple) OCE(s) 
- Protection against compromised OCEs 
It is recommended to use the allowlist also as a revocation mechanism for OCE certificates. However, if the 
allowlist is used in this way, special care shall be taken never to empty/remove the allowlist (i.e. if created, the 
allowlist shall always contain at least one certificate) because no restrictions apply (i.e. all certificates are 
accepted) once an allowlist is removed. 

Control which certificates can be used for authentication by maintaining an allowlist of serial numbers:

```csharp
// Store allowlist of certificate serial numbers
var allowedSerials = new List<string>
{
    "7F4971B0AD51F84C9DA9928B2D5FEF5E16B2920A",
    "6B90028800909F9FFCD641346933242748FBE9AD"
};
session.StoreAllowlist(keyReference, allowedSerials);

// Clear allowlist (allows any valid certificate)
session.ClearAllowList(keyReference);
```

## Certificate management by SCP11 variant

Different SCP11 variants have different certificate requirements:

### SCP11a

- Full certificate chain required
- OCE (Off-Card Entity) certificates needed
- Supports authorization rules in certificates
- Used for mutual authentication

Example setup:

```csharp
// Setup with full chain for SCP11a
using var session = new SecurityDomainSession(yubiKeyDevice, scp03Params);
var keyRef = KeyReference.Create(ScpKeyIds.Scp11A, kvn);

// Store certificates
session.StoreCertificates(keyRef, certificates);

// Configure OCE
var oceRef = KeyReference.Create(OceKid, kvn);
session.StoreCaIssuer(oceRef, skiBytes);
```

### SCP11b

- Simplest variant, no mutual authentication
- Suitable when host authentication isn't required

Example setup:

```csharp
// Basic SCP11b setup
using var session = new SecurityDomainSession(yubiKeyDevice, scp03Params);
var keyRef = KeyReference.Create(ScpKeyIds.Scp11B, kvn);

// Only store device certificate
session.StoreCertificates(keyRef, new[] { deviceCert });
```

### SCP11c

- Mutual authentication
- Similar to SCP11a but with additional features
- such as offline scripting usage (See [GlobalPlatform SCP11 Specification Annex B](https://globalplatform.org/specs-library/secure-channel-protocol-11-amendment-f/))

## Security considerations

1. **Certificate Validation**

   - Verify certificate chains completely
   - Check certificate revocation status
   - Validate certificate purposes and extensions
   - Ensure proper key usage constraints

2. **Access Control**

   - Use allowlists in production environments
   - Regularly review and update allowlists
   - Monitor for unauthorized certificate usage
   - Document certificate authorization policies

3. **Certificate Lifecycle**

   - Plan for certificate renewal
   - Handle certificate revocation
   - Maintain certificate inventory
   - Test certificate rotation procedures

4. **Storage Limitations**
   - Be aware of YubiKey storage constraints
   - Optimize certificate chain length
   - Consider certificate compression if needed
   - Monitor available storage space

> [!IMPORTANT]
> Most certificate operations require an authenticated session. Operations are typically only available when using SCP11a or SCP11c variants.

## Common tasks

### Initial certificate setup

1. Generate or obtain required certificates
2. Store certificate chain on YubiKey
3. Configure CA information if needed
4. Set up allowlist for production use

```csharp
// Example of complete setup
using var session = new SecurityDomainSession(yubiKeyDevice, scp03Params);
var keyRef = KeyReference.Create(ScpKeyIds.Scp11A, kvn);

// Store full chain
session.StoreCertificates(keyRef, certificateChain);

// Configure CA
var oceRef = KeyReference.Create(OceKid, kvn);
session.StoreCaIssuer(oceRef, GetSkiFromCertificate(caCert));

// Set up allowlist
session.StoreAllowlist(keyRef, allowedSerials);
```

### Certificate rotation

```csharp
using var session = new SecurityDomainSession(yubiKeyDevice, scpParams);

// Store new certificates
session.StoreCertificates(keyRef, newCertificateChain);

// Update allowlist if needed
session.StoreAllowlist(keyRef, newAllowedSerials);
```

### Troubleshooting

1. **Certificate Loading Issues**

   - Verify certificate format (X.509 v3)
   - Check certificate chain order
   - Ensure sufficient storage space
   - Validate key references

2. **Authentication Problems**
   - Verify certificate trust chain
   - Check allowlist configuration
   - Confirm proper SCP variant usage
   - Validate certificate dates

> [!NOTE]
> For additional details on secure channel establishment and certificate usage, refer to the [Secure Channel Protocol (SCP)](xref:UsersManualScp) documentation.
