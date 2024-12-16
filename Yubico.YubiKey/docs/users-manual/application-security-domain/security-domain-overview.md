---
uid: SecurityDomainOverview
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

# Security Domain Overview

The Security Domain is a special application on the YubiKey responsible for managing secure communication channels and cryptographic keys. It implements protocols defined by [Global Platform Consortium](https://globalplatform.org/) that provide confidentiality and integrity for commands sent between host applications and the YubiKey.

For detailed information about the protocols, use cases, and transport options, see the [Secure Channel Protocol (SCP)](xref:UsersManualScp) documentation.

## Requirements

Hardware:
- YubiKey 5 Series or later
- For SCP03: Firmware 5.3 or later
- For SCP11: Firmware 5.7.2 or later

Transport Protocols:
- Smartcard over USB or NFC

## Core Features

The Security Domain provides:
- Management of secure communication channels (SCP03 and SCP11)
- Storage and management of cryptographic keys
- Certificate management for asymmetric protocols
- Access control through certificate allowlists

## Basic Usage

```csharp
// Create session without SCP protection
using var session = new SecurityDomainSession(yubiKeyDevice);
session.GetKeyInformation();

// Create SCP protected session
using var session = new SecurityDomainSession(yubiKeyDevice, scpKeyParameters);
session.GenerateEcKey(parameters...); // Protected by secure channel
```

## Documentation Structure

The Security Domain functionality is documented in the following sections:

- [Key Management](xref:SecurityDomainKeys) - Managing symmetric (SCP03) and asymmetric (SCP11) keys
- [Certificate Operations](xref:SecurityDomainCertificates) - Working with X.509 certificates and certificate chains
- [Common Tasks](xref:SecurityDomainTasks) - Setup, configuration, and maintenance operations
- [Device Information](xref:SecurityDomainDevice) - Device data and configuration management

## Basic Security Considerations

When working with the Security Domain:
- Most operations require an authenticated session
- Default SCP03 keys provide no security, replace them in production
- Some operations permanently modify the YubiKey
- Maintain proper key and certificate backups

> [!NOTE]
> For detailed implementation guidance and best practices, refer to the [Secure Channel Protocol (SCP)](xref:UsersManualScp) documentation.z