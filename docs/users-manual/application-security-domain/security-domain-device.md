---
uid: SecurityDomainDevice
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

# Security Domain device information

The Security Domain provides access to various device information and configuration data. This document covers device information retrieval and generic data operations. For protocol details, see the [Secure Channel Protocol (SCP)](xref:UsersManualScp) documentation.

## Card recognition data

The card recognition data provides information about the YubiKey's capabilities and configuration according to GlobalPlatform Card Specification v2.3.1 §H.2.

### Retrieving card data

```csharp
using var session = new SecurityDomainSession(yubiKeyDevice);

// Get card recognition data (TLV encoded)
var cardData = session.GetCardRecognitionData();
```

### Card data structure

The card recognition data follows this TLV structure:

| Tag  | Description                 |
| ---- | --------------------------- |
| 0x66 | Card Data template          |
| 0x73 | Card Recognition Data       |
| ...  | Card-specific data elements |

## Generic data operations

The Security Domain supports general-purpose data retrieval and storage using TLV (Tag-Length-Value) formatting.

### Retrieving data

```csharp
// Get data for a specific tag
var data = session.GetData(tag);

// Get data with additional parameters
var data = session.GetData(tag, additionalData);
```

### Storing data

```csharp
// Store TLV-formatted data
byte[] tlvData = PrepareTlvData();
session.StoreData(tlvData);
```

### Common data tags

| Tag    | Description       | Access Level |
| ------ | ----------------- | ------------ |
| 0x66   | Card Data         | Read-only    |
| 0x73   | Card Recognition  | Read-only    |
| 0xE0   | Key Information   | Read-only    |
| 0xBF21 | Certificate Store | Read/Write   |
| 0xFF33 | KLOC Identifiers  | Read/Write   |
| 0xFF34 | KLCC Identifiers  | Read/Write   |

## Device configuration

### Checking capabilities

```csharp
// Check SCP11 support
if (yubiKeyDevice.HasFeature(YubiKeyFeature.Scp11))
{
    // Device supports SCP11
}

// Get installed key information
var keyInfo = session.GetKeyInformation();

// Get supported CA identifiers
var caIds = session.GetSupportedCaIdentifiers(
    kloc: true,
    klcc: true
);
```

### Device status information

1. **Key Status**

```csharp
var keyInfo = session.GetKeyInformation();
foreach (var entry in keyInfo)
{
    var keyRef = entry.Key;
    var components = entry.Value;

    // Check key type and components
    bool isScp03 = keyRef.Id == ScpKeyIds.Scp03;
    bool hasAllComponents = components.Count == 3; // For SCP03
}
```

2. **Certificate Status**

```csharp
// Check certificate configuration
foreach (var key in keyInfo.Keys)
{
    try
    {
        var certs = session.GetCertificates(key);
        // Analyze certificate chain
        AnalyzeCertificateChain(certs);
    }
    catch (Exception ex)
    {
        // Handle missing certificates
    }
}
```

## Data management

### TLV data handling

1. **Reading TLV Data**

```csharp
var tlvData = session.GetData(tag);
var tlvReader = new TlvReader(tlvData);

// Parse TLV structure
while (tlvReader.HasData)
{
    var tag = tlvReader.PeekTag();
    var value = tlvReader.ReadValue();
    ProcessTlvData(tag, value);
}
```

2. **Writing TLV Data**

```csharp
using var ms = new MemoryStream();
using var writer = new BinaryWriter(ms);

// Build TLV structure
writer.Write((byte)tag);
writer.Write((byte)length);
writer.Write(value);

session.StoreData(ms.ToArray());
```

### Data organization

The Security Domain organizes data hierarchically:

```
Root
├── Card Data (0x66)
│   └── Card Recognition (0x73)
├── Key Information (0xE0)
│   ├── Key References
│   └── Key Components
├── Certificate Store (0xBF21)
│   ├── Certificate Chains
│   └── CA Information
└── Device Configuration
    ├── KLOC Data (0xFF33)
    └── KLCC Data (0xFF34)
```

## Maintenance operations

### Data validation

```csharp
// Validate stored data
public void ValidateDeviceConfiguration()
{
    // Check card data
    var cardData = session.GetCardRecognitionData();
    ValidateCardData(cardData);

    // Check key information
    var keyInfo = session.GetKeyInformation();
    ValidateKeyConfiguration(keyInfo);

    // Check certificate store
    foreach (var key in keyInfo.Keys)
    {
        ValidateCertificateConfiguration(key);
    }
}
```

### Data cleanup

```csharp
// Remove unused data
public void CleanupDeviceData()
{
    // Clear unused certificates
    foreach (var key in keyInfo.Keys)
    {
        if (IsKeyExpired(key))
        {
            session.DeleteKey(key);
        }
    }

    // Clear obsolete allowlists
    foreach (var key in keyInfo.Keys)
    {
        if (IsAllowlistObsolete(key))
        {
            session.ClearAllowList(key);
        }
    }
}
```