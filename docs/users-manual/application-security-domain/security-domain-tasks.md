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

# Security Domain common tasks

This document covers common operational tasks and workflows for managing the Security Domain. For detailed information about secure channels, see the [Secure Channel Protocol (SCP)](xref:UsersManualScp) documentation.

## Setting up a new YubiKey

### 1. Initial state assessment

Check the current configuration of your YubiKey:

```csharp
using var session = new SecurityDomainSession(yubiKeyDevice);
var keyInfo = session.GetKeyInformation();
var hasDefaultKeys = keyInfo.Any(k => k.Key.VersionNumber == 0xFF);
```

### 2. Replacing default SCP03 keys

Always replace default keys in production environments:

```csharp
// Start with default keys
using var defaultSession = new SecurityDomainSession(
    yubiKeyDevice, 
    Scp03KeyParameters.DefaultKey);

// Generate or obtain your secure keys
var newKeys = new StaticKeys(newMacKey, newEncKey, newDekKey);
var keyRef = KeyReference.Create(ScpKeyIds.Scp03, keyVersionNumber);
defaultSession.PutKey(keyRef, newKeys);
```

> [!WARNING]
> Default keys provide no security. Replace them before deploying to production.

## Setting up SCP11

### 1. Generate initial keys

Start with an authenticated SCP03 session:

```csharp
// Use SCP03 session to set up SCP11
using var session = new SecurityDomainSession(yubiKeyDevice, scp03Params);

// Generate SCP11b key pair
var keyRef = KeyReference.Create(ScpKeyIds.Scp11B, keyVersionNumber);
var publicKey = session.GenerateEcKey(keyRef);
```

### 2. Configure certificate chain

```csharp
// Store certificates
session.StoreCertificates(keyRef, certificateChain);

// Configure CA for SCP11a/c
var oceSubjectKeyIdentifier = GetSkiFromCertificate(oceCertCa);
var caRef = KeyReference.Create(OceKid, kvn);
session.StoreCaIssuer(caRef, oceSubjectKeyIdentifier);
```

### 3. Set up access control (Optional)

```csharp
// Configure certificate allowlist
var allowedSerials = GetAllowedCertificateSerials();
session.StoreAllowlist(keyRef, allowedSerials);
```

## Key management tasks

### Rotating SCP03 keys

```csharp
// Authenticate with current keys
using var session = new SecurityDomainSession(yubiKeyDevice, currentScp03Params);

// Replace with new keys
var newKeyRef = KeyReference.Create(ScpKeyIds.Scp03, newKvn);
session.PutKey(newKeyRef, newStaticKeys, kvnToReplace);
```

### Rotating SCP11 keys

```csharp
using var session = new SecurityDomainSession(yubiKeyDevice, scpParams);

// Generate new key pair
var newKeyRef = KeyReference.Create(ScpKeyIds.Scp11B, newKvn);
var newPublicKey = session.GenerateEcKey(newKeyRef, kvnToReplace); // Will be replaced
```

## Recovery operations

### Status check

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

### Factory reset

```csharp
// Warning: This removes all custom keys in the Security Domain
using var session = new SecurityDomainSession(yubiKeyDevice);
session.Reset();
```

> [!IMPORTANT]
> Resetting removes all custom keys and certificates. Have a recovery plan ready.

## Integration with other applications

### PIV with secure channel

```csharp
// Using SCP03
using var pivSession = new PivSession(yubiKeyDevice, scp03Params);
pivSession.GenerateKeyPair(...); // Protected by SCP03

// Using SCP11
using var pivSession = new PivSession(yubiKeyDevice, scp11Params);
pivSession.GenerateKeyPair(...); // Protected by SCP11
```

### OATH with secure channel

```csharp
// Using SCP03
using var oathSession = new OathSession(yubiKeyDevice, scp03Params);
oathSession.PutCredential(...); // Protected by SCP03

// Using SCP11
using var oathSession = new OathSession(yubiKeyDevice, scp11Params);
oathSession.PutCredential(...); // Protected by SCP11
```

## Production deployment tasks

### Initial provisioning

1. **Prepare Keys and Certificates**
```csharp
var scp03Keys = GenerateSecureKeys();
var (privateKey, publicKey, certificates) = GenerateScp11Credentials();
```

2. **Configure YubiKey for SCP11B**
```csharp
using var session = new SecurityDomainSession(yubiKeyDevice, Scp03KeyParameters.DefaultKey);

// Replace SCP03 keys
var scp03Ref = KeyReference.Create(ScpKeyIds.Scp03, keyVersionNumber);
session.PutKey(scp03Ref, scp03Keys);

// Set up SCP11
var scp11Ref = KeyReference.Create(ScpKeyIds.Scp11B, keyVersionNumber);
var scp11Public = session.GenerateEcKey(scp11Ref);
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

### Regular maintenance

1. **Monitor Key Status**
```csharp
// Check key information
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