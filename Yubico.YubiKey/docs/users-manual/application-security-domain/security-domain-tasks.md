---
uid: SecurityDomainTasks
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

# Security Domain Common Tasks

This document covers common operational tasks and workflows for managing the Security Domain. For detailed information about secure channels, see the [Secure Channel Protocol (SCP)](xref:UsersManualScp) documentation.

## Setting Up a New YubiKey

### 1. Initial State Assessment

Check the current configuration of your YubiKey:

```csharp
using var session = new SecurityDomainSession(yubiKeyDevice);
var keyInfo = session.GetKeyInformation();
var hasDefaultKeys = keyInfo.Any(k => k.Key.VersionNumber == 0xFF);
```

### 2. Replacing Default SCP03 Keys

Always replace default keys in production environments:

```csharp
// Start with default keys
using var defaultSession = new SecurityDomainSession(
    yubiKeyDevice, 
    Scp03KeyParameters.DefaultKey);

// Generate or obtain your secure keys
var newKeys = new StaticKeys(newMacKey, newEncKey, newDekKey);
var keyRef = KeyReference.Create(ScpKeyIds.Scp03, 0x01);
defaultSession.PutKey(keyRef, newKeys, 0);
```

> [!WARNING]
> Default keys provide no security. Replace them before deploying to production.

## Setting Up SCP11

### 1. Generate Initial Keys

Start with an authenticated SCP03 session:

```csharp
// Use SCP03 session to set up SCP11
using var session = new SecurityDomainSession(yubiKeyDevice, scp03Params);

// Generate SCP11b key pair
var keyRef = KeyReference.Create(ScpKeyIds.Scp11B, 0x01);
var publicKey = session.GenerateEcKey(keyRef, 0);
```

### 2. Configure Certificate Chain

```csharp
// Store certificates
session.StoreCertificates(keyRef, certificateChain);

// Configure CA for SCP11a/c
var caRef = KeyReference.Create(OceKid, kvn);
session.StoreCaIssuer(caRef, skiBytes);
```

### 3. Set Up Access Control

```csharp
// Configure certificate allowlist
var allowedSerials = GetAllowedCertificateSerials();
session.StoreAllowlist(keyRef, allowedSerials);
```

## Key Management Tasks

### Rotating SCP03 Keys

```csharp
// Authenticate with current keys
using var session = new SecurityDomainSession(yubiKeyDevice, currentScp03Params);

// Replace with new keys
var newKeyRef = KeyReference.Create(ScpKeyIds.Scp03, 0x02);
session.PutKey(newKeyRef, newStaticKeys, currentKvn);
```

### Rotating SCP11 Keys

```csharp
using var session = new SecurityDomainSession(yubiKeyDevice, scpParams);

// Generate new key pair
var newKeyRef = KeyReference.Create(ScpKeyIds.Scp11B, 0x02);
var newPublicKey = session.GenerateEcKey(newKeyRef, oldKvn);
```

## Recovery Operations

### Status Check

```csharp
using var session = new SecurityDomainSession(yubiKeyDevice);

// Get key information
var keyInfo = session.GetKeyInformation();
var activeKeys = keyInfo.Select(k => k.Key).ToList();

// Check certificates
foreach (var key in activeKeys)
{
    try
    {
        var certs = session.GetCertificates(key);
        Console.WriteLine($"Key {key} has {certs.Count} certificates");
    }
    catch
    {
        Console.WriteLine($"No certificates for key {key}");
    }
}
```

### Factory Reset

```csharp
// Warning: This removes all custom keys
using var session = new SecurityDomainSession(yubiKeyDevice);
session.Reset();
```

> [!IMPORTANT]
> Resetting removes all custom keys and certificates. Have a recovery plan ready.

## Integration with Other Applications

### PIV with Secure Channel

```csharp
// Using SCP03
using var pivSession = new PivSession(yubiKeyDevice, scp03Params);
pivSession.GenerateKeyPair(...); // Protected by SCP03

// Using SCP11
using var pivSession = new PivSession(yubiKeyDevice, scp11Params);
pivSession.GenerateKeyPair(...); // Protected by SCP11
```

### OATH with Secure Channel

```csharp
// Using SCP03
using var oathSession = new OathSession(yubiKeyDevice, scp03Params);
oathSession.PutCredential(...); // Protected by SCP03

// Using SCP11
using var oathSession = new OathSession(yubiKeyDevice, scp11Params);
oathSession.PutCredential(...); // Protected by SCP11
```

## Production Deployment Tasks

### Initial Provisioning

1. **Prepare Keys and Certificates**
```csharp
var scp03Keys = GenerateSecureKeys();
var (privateKey, publicKey, certificates) = GenerateScp11Credentials();
```

2. **Configure YubiKey**
```csharp
using var session = new SecurityDomainSession(yubiKeyDevice, Scp03KeyParameters.DefaultKey);

// Replace SCP03 keys
var scp03Ref = KeyReference.Create(ScpKeyIds.Scp03, 0x01);
session.PutKey(scp03Ref, scp03Keys, 0);

// Set up SCP11
var scp11Ref = KeyReference.Create(ScpKeyIds.Scp11B, 0x01);
var scp11Public = session.GenerateEcKey(scp11Ref, 0);
session.StoreCertificates(scp11Ref, certificates);
```

3. **Validate Configuration**
```csharp
// Test new keys
using var verifySession = new SecurityDomainSession(
    yubiKeyDevice,
    new Scp03KeyParameters(scp03Ref, scp03Keys));

var keyInfo = verifySession.GetKeyInformation();
// Verify expected keys are present
```

### Regular Maintenance

1. **Monitor Key Status**
```csharp
// Check key information regularly
var keyInfo = session.GetKeyInformation();
foreach (var key in keyInfo)
{
    // Log key status and plan rotation if needed
    LogKeyStatus(key);
}
```

2. **Certificate Management**
```csharp
// Check certificate expiration
var certificates = session.GetCertificates(keyRef);
foreach (var cert in certificates)
{
    if (cert.NotAfter < DateTime.Now.AddMonths(3))
    {
        // Plan certificate renewal
        PlanCertificateRenewal(cert);
    }
}
```

## Troubleshooting

### Key Issues

1. **Unable to Authenticate**
   - Verify key version numbers
   - Check key reference values
   - Confirm key components
   - Try fallback to default keys

2. **Failed Key Import**
   - Validate key formats
   - Check authentication status
   - Verify available space
   - Confirm key compatibility

### Certificate Issues

1. **Certificate Chain Problems**
   - Verify chain order
   - Check CA configuration
   - Validate certificate formats
   - Confirm authentication level

2. **Access Control Issues**
   - Check allowlist configuration
   - Verify certificate serials
   - Validate certificate dates
   - Confirm SCP variant support

> [!NOTE]
> Always maintain detailed logs of key and certificate operations for troubleshooting.