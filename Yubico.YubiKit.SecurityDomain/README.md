# Yubico.YubiKit.SecurityDomain

> **Note:** This documentation is subject to change as the module evolves. Please check for updates regularly.

This module provides access to the YubiKey Security Domain application, enabling secure channel establishment and key management using Secure Channel Protocol (SCP).

## Overview

The Security Domain is the root security application on YubiKey firmware 5.3.0 and newer. It manages:
- **SCP Key Lifecycle**: Generate, import, delete, and manage SCP keys
- **Secure Channel Establishment**: Create authenticated, encrypted channels using SCP03, SCP11a, SCP11b, and SCP11c
- **Certificate Management**: Store and retrieve certificates for SCP11 protocols
- **Allowlist Management**: Control which Off-Card Entities (OCEs) can authenticate

## Requirements

- **Minimum Firmware**: YubiKey 5.3.0
- **SCP03**: Available on YubiKey 5.3.0+
- **SCP11**: Available on YubiKey 5.7.2+

## Usage Example

```csharp
using Yubico.YubiKit.SecurityDomain;
using Yubico.YubiKit.Core.YubiKey;

IYubiKey yubiKey = ...;
using var sdSession = await yubiKey.CreateSecurityDomainSessionAsync();
// Use sdSession for SCP key management, etc.
```

## Logging

This SDK uses `Microsoft.Extensions.Logging`. To enable logs, set the global logger factory once at startup:

```csharp
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;

YubiKitLogging.LoggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
```


## Key Concepts

### SCP Protocols

- **SCP03**: Symmetric key-based secure channel using AES-128
  - Uses static keys (ENC, MAC, DEK)
  - Default key reference: KID=0x01, KVN=0xFF
  
- **SCP11a/c**: Asymmetric authentication using EC keys
  - YubiKey generates/imports EC key pair (P-256)
  - Requires OCE (Off-Card Entity) with certificate chain
  - Supports serial number allowlists for access control
  
- **SCP11b**: Simplified SCP11 without certificate chain requirement
  - Uses pre-shared public key knowledge
  - Faster authentication than SCP11a/c

### Key References

Keys are identified by:
- **KID** (Key ID): Identifies the key type/purpose (e.g., 0x01 for SCP03, 0x10 for SCP11a, 0x13 for SCP11b)
- **KVN** (Key Version Number): Allows multiple versions of the same key type

## Core API

### Creating a Session

```csharp
using Yubico.YubiKit.SecurityDomain;

// Without secure channel
using var session = await SecurityDomainSession.CreateAsync(
    connection,
    cancellationToken: cancellationToken);

// If you already know the device firmware version, you can optionally pass it via
// the `firmwareVersion` parameter to avoid hardcoding defaults.

// With SCP03 authentication
using var scpParams = Scp03KeyParameters.Default;
using var session = await SecurityDomainSession.CreateAsync(
    connection,
    scpKeyParams: scpParams,
    cancellationToken: cancellationToken);
```

### Key Operations

```csharp
// Get key information
var keyInfo = await session.GetKeyInformationAsync(cancellationToken);

// Generate EC key for SCP11b
var keyRef = new KeyReference(ScpKid.SCP11b, kvn: 0x01);
var publicKey = await session.GenerateKeyAsync(keyRef, 0, cancellationToken);

// Import SCP03 static keys
var staticKeys = new StaticKeys(encKey, macKey, dekKey);
var keyRef = new KeyReference(0x01, 0x02);
await session.PutKeyAsync(keyRef, staticKeys, replaceKvn: 0, cancellationToken);

// Delete a key
await session.DeleteKeyAsync(keyRef, deleteLast: false, cancellationToken);
```

### Certificate and Allowlist Management

```csharp
// Store CA issuer for SCP11
await session.StoreCaIssuerAsync(oceKeyRef, subjectKeyIdentifier, cancellationToken);

// Store serial number allowlist
string[] allowedSerials = ["7F4971B0AD51F84C9DA9928B2D5FEF5E16B2920A"];
await session.StoreAllowlistAsync(oceKeyRef, allowedSerials);
```

### Factory Reset

```csharp
// Block all registered keys and reinitialize
await session.ResetAsync(cancellationToken);
```

## Project Structure

```
Yubico.YubiKit.SecurityDomain/
├── src/
│   ├── SecurityDomainSession.cs     # Main session class
│   └── Yubico.YubiKit.SecurityDomain.csproj
└── tests/
    ├── Yubico.YubiKit.SecurityDomain.IntegrationTests/
    │   ├── SecurityDomainSessionTests.cs
    │   ├── Scp11TestData.cs
    │   ├── ScpCertificates.cs
    │   └── TestExtensions/
    │       └── SecurityDomainTestStateExtensions.cs
    └── Yubico.YubiKit.SecurityDomain.UnitTests/
```

## Common Use Cases

### 1. Rotating SCP03 Keys

```csharp
// Authenticate with current keys
using var currentParams = new Scp03KeyParameters(currentKeyRef, currentKeys);
using var session = await SecurityDomainSession.CreateAsync(
    connection,
    scpKeyParams: currentParams,
    cancellationToken: cancellationToken);

// Import new keys
var newKeyRef = new KeyReference(0x01, 0x02);
await session.PutKeyAsync(newKeyRef, newStaticKeys, replaceKvn: 0, cancellationToken);

// Future sessions use new keys
```

### 2. Setting Up SCP11b

```csharp
// Step 1: Generate key on YubiKey
var keyRef = new KeyReference(ScpKid.SCP11b, 0x01);
var publicKey = await session.GenerateKeyAsync(keyRef, 0, cancellationToken);

// Step 2: Store public key in your application
// (publicKey contains the P-256 public point)

// Step 3: Authenticate with SCP11b in future sessions
var scp11Params = new Scp11KeyParameters(keyRef, publicKey);
using var session = await SecurityDomainSession.CreateAsync(
    connection,
    scpKeyParams: scp11Params,
    cancellationToken: cancellationToken);
```

### 3. Factory Reset to Default State

```csharp
// Create session without authentication
using var session = await SecurityDomainSession.CreateAsync(
    connection,
    firmwareVersion: null,
    cancellationToken: cancellationToken);

// Block all keys and restore defaults
await session.ResetAsync(cancellationToken);
```

## Testing Guidance

See [CLAUDE.md](CLAUDE.md) for detailed test infrastructure information.

## References

- **GlobalPlatform Specification**: Card Specification v2.3.1
- **SCP Documentation**: GlobalPlatform Secure Channel Protocol specifications
- **YubiKey Documentation**: https://developers.yubico.com/
