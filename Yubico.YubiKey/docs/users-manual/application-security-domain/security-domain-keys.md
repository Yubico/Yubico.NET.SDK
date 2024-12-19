---
uid: SecurityDomainKeys
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

# Security Domain key management

The Security Domain supports management of both symmetric (SCP03) and asymmetric (SCP11) keys. This document describes
the key types, their usage, and management operations.

For protocol details and secure channel implementation, see the [Secure Channel Protocol (SCP)](xref:UsersManualScp)
documentation.

## Key types

The Security Domain manages two main types of keys:

- **SCP03 Keys**: Symmetric AES-128 keys used for secure messaging
- **SCP11 Keys**: Asymmetric NIST P-256 keys and X.509-certificates used for authentication and key agreement

## SCP03 key management

Each SCP03 key set consists of three AES-128 keys that work together to secure communications:

| Key Type | Key ID (KID) | Purpose                                      |
|----------|--------------|----------------------------------------------|
| Key-ENC  | 0x1          | Channel encryption key for securing messages |
| Key-MAC  | 0x2          | Channel MAC key for message authentication   |
| Key-DEK  | 0x3          | Data encryption key for sensitive data       |

### Managing SCP03 keys

```csharp
// Put a new SCP03 key set
var kvn = 0x01;
var keyRef = KeyReference.Create(ScpKeyIds.Scp03, kvn);
var staticKeys = new StaticKeys(keyDataMac, keyDataEnc, keyDataDek);
session.PutKey(keyRef, staticKeys); 

// Replace existing keys
var newKeys = new StaticKeys(newMacKey, newEncKey, newDekKey);
session.PutKey(keyRef, newKeys, kvnToReplace);
```

### Key Version Numbers (KVN)

SCP03 key sets are identified by Key Version Numbers:

- Default key set: KVN=0xFF (publicly known, no security)
- Each YubiKey can store up to three custom SCP03 key sets

> [!NOTE]
> When adding the first custom key set, the default keys are automatically removed.

## SCP11 key management

SCP11 uses NIST P-256 elliptic curve cryptography. Keys can be:

- Generated on the YubiKey
- Imported from external sources

### Generating keys

```csharp
// Generate new EC key pair
var keyRef = KeyReference.Create(ScpKeyIds.Scp11B, keyVersionNumber);
var publicKey = session.GenerateEcKey(keyRef);
```

### Importing keys

```csharp
// Import existing private key
var privateKey = new ECPrivateKeyParameters(ecdsa);
session.PutKey(keyRef, privateKey);

// Import public key
var publicKey = new ECPublicKeyParameters(ecdsaPublic);
session.PutKey(keyRef, publicKey);
```

## Key management operations

### Querying key information

```csharp
// Get information about installed keys
var keyInfo = session.GetKeyInformation();
foreach (var entry in keyInfo)
{
    var keyRef = entry.Key;               // KeyReference containing ID and Version
    var components = entry.Value;         // Dictionary of key components
    Console.WriteLine($"Key {keyRef.Id:X2}:{keyRef.VersionNumber:X2}");
}
```

### Deleting keys

Keys can be deleted individually or reset to factory defaults:

```csharp
// Delete specific key
session.DeleteKey(keyRef);

// Reset all keys to factory defaults
session.Reset();
```

> [!WARNING]
> Resetting removes all custom keys and restores factory defaults (within the Security Domain). Ensure you have backups
> before resetting.

## Key rotation

Here are some simple key rotation procedures:

### SCP03 key rotation

```csharp
// Authenticate with current keys
using var session = new SecurityDomainSession(yubiKeyDevice, scpParams);

// Replace with new keys
var newKeyRef = KeyReference.Create(ScpKeyIds.Scp03, keyVersionNumber);
session.PutKey(newKeyRef, newStaticKeys, kvnToReplace);
```

### SCP11 key rotation

```csharp
using var session = new SecurityDomainSession(yubiKeyDevice, scpParams);

// Generate new key pair
var newKeyRef = KeyReference.Create(ScpKeyIds.Scp11B, keyVersionNumber);
var newPublicKey = session.GenerateEcKey(newKeyRef, kvnToReplace);
```

## Security considerations

1. **Key Protection**
    - Use unique keys per device when possible
    - Consider using SCP11 for mutual authentication

2. **Key Version Management**
    - Track which keys are loaded on each YubiKey
    - Track KVNs in use

3. **Default Keys**
    - Default SCP03 keys provide no security
    - Replace default keys in production environments
    - Cannot retain default keys alongside custom keys

> [!IMPORTANT]
> The YubiKey provides no metadata about installed keys beyond what's available through `GetKeyInformation()`. Your
> application must track additional key management details.