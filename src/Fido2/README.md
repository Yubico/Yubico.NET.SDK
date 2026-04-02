# Yubico.YubiKit.Fido2

FIDO2/WebAuthn implementation for YubiKey authenticators, supporting passkey creation, authentication, and advanced CTAP 2.1/2.3 features.

## Overview

This module implements the CTAP 2.1/2.3 (Client to Authenticator Protocol) for YubiKey FIDO2 authenticators. It provides a complete implementation of FIDO2/WebAuthn functionality including:

- 🔐 **Passkey Management** - Create and use passkeys for passwordless authentication
- 🔑 **WebAuthn Operations** - MakeCredential and GetAssertion for web authentication
- 👤 **Resident Keys** - Discoverable credentials stored on the authenticator
- 🔒 **User Verification** - PIN and biometric authentication support
- 📱 **Credential Management** - Enumerate, update, and delete credentials
- 🧬 **Biometric Enrollment** - Fingerprint enrollment (YubiKey Bio series)
- 📦 **Large Blob Storage** - Per-credential blob storage
- ⚙️ **Authenticator Config** - Device configuration (firmware 5.4+)

## Requirements

- **YubiKey Models**: YubiKey 5 series, Security Key series, YubiKey Bio series
- **Firmware**: 5.0+ (some features require 5.2+, 5.4+, 5.7+)
- **Transports**:
  - USB: HID FIDO interface (primary)
  - NFC: SmartCard/CCID interface only

⚠️ **Important**: FIDO2 over USB uses HID FIDO, NOT CCID. USB CCID connections are NOT supported for FIDO2.

## Installation

```bash
dotnet add package Yubico.YubiKit.Fido2
```

## Quick Start

### Create a FIDO2 Session

```csharp
using Yubico.YubiKit.Fido2;

// Create session from IYubiKey (recommended)
await using var fidoSession = await yubiKey.CreateFidoSessionAsync();

// Get authenticator information
var info = await fidoSession.GetInfoAsync();
Console.WriteLine($"FIDO2 Version: {info.CtapVersion}");
Console.WriteLine($"Supports Resident Keys: {info.SupportsResidentKeys}");
```

### Make Credential (Registration)

```csharp
using Yubico.YubiKit.Fido2.Credentials;

// Define relying party and user
var rpEntity = new PublicKeyCredentialRpEntity
{
    Id = "example.com",
    Name = "Example Corporation"
};

var userEntity = new PublicKeyCredentialUserEntity
{
    Id = userId,
    Name = "user@example.com",
    DisplayName = "User Name"
};

// Create credential
var credOptions = new MakeCredentialOptions
{
    Rp = rpEntity,
    User = userEntity,
    PubKeyCredParams = new[]
    {
        new PubKeyCredParam { Type = "public-key", Alg = -7 }  // ES256
    },
    Options = new AuthenticatorOptions
    {
        Rk = true,  // Resident key (discoverable)
        Uv = true   // User verification required
    }
};

var response = await fidoSession.MakeCredentialAsync(credOptions);

Console.WriteLine($"Credential ID: {Convert.ToHexString(response.CredentialId)}");
Console.WriteLine($"Public Key (COSE): {Convert.ToHexString(response.AuthData.AttestedCredentialData.CredentialPublicKey)}");
```

### Get Assertion (Authentication)

```csharp
// Authenticate with specific credential
var assertionOptions = new GetAssertionOptions
{
    RpId = "example.com",
    AllowList = new[]
    {
        new PublicKeyCredentialDescriptor
        {
            Type = "public-key",
            Id = credentialId
        }
    },
    Options = new AuthenticatorOptions
    {
        Up = true,  // User presence (touch)
        Uv = true   // User verification (PIN)
    }
};

var assertion = await fidoSession.GetAssertionAsync(assertionOptions);

Console.WriteLine($"User Handle: {Convert.ToHexString(assertion.UserHandle)}");
Console.WriteLine($"Signature: {Convert.ToHexString(assertion.Signature)}");
```

### Discoverable Credentials (Resident Keys)

```csharp
// Authenticate without credential ID (using resident keys)
var options = new GetAssertionOptions
{
    RpId = "example.com",
    // No AllowList - authenticator returns available credentials
    Options = new AuthenticatorOptions { Uv = true }
};

var assertion = await fidoSession.GetAssertionAsync(options);

// If multiple credentials, get next one
if (assertion.NumberOfCredentials > 1)
{
    var nextAssertion = await fidoSession.GetNextAssertionAsync();
}
```

## Advanced Features

### Credential Management

```csharp
using Yubico.YubiKit.Fido2.CredentialManagement;

// Get all credentials
var creds = await fidoSession.EnumerateCredentialsAsync();
foreach (var cred in creds)
{
    Console.WriteLine($"RP: {cred.RpId}");
    Console.WriteLine($"User: {cred.User.Name}");
    Console.WriteLine($"Credential ID: {Convert.ToHexString(cred.CredentialId)}");
}

// Delete a credential
await fidoSession.DeleteCredentialAsync(credentialDescriptor);

// Update user information
await fidoSession.UpdateUserInformationAsync(
    credentialDescriptor,
    newUserEntity);
```

### Biometric Enrollment (YubiKey Bio)

```csharp
using Yubico.YubiKit.Fido2.BioEnrollment;

// Get fingerprint sensor info
var bioInfo = await fidoSession.GetBioModalityAsync();
Console.WriteLine($"Max samples: {bioInfo.MaxCaptureSamplesRequiredForEnroll}");

// Enroll a new fingerprint
var enrollment = await fidoSession.EnrollBiometricAsync(
    onCaptureCallback: (remaining, status) =>
    {
        Console.WriteLine($"Touch sensor ({remaining} samples remaining)");
    });

Console.WriteLine($"Fingerprint enrolled: {enrollment.TemplateId}");

// Enumerate enrolled fingerprints
var fingerprints = await fidoSession.EnumerateBiometricEnrollmentsAsync();
```

### WebAuthn Extensions

```csharp
using Yubico.YubiKit.Fido2.Extensions;

// Use credProtect extension
var credOptions = new MakeCredentialOptions
{
    // ... rp, user, etc.
    Extensions = new ExtensionBuilder()
        .AddCredProtect(CredProtectPolicy.UserVerificationRequired)
        .Build()
};

// Use hmac-secret extension
var assertionOptions = new GetAssertionOptions
{
    // ... rpId, etc.
    Extensions = new ExtensionBuilder()
        .AddHmacSecret(
            salt1: saltBytes,
            salt2: null,
            pinUvAuthProtocol: fidoSession.PinUvAuthProtocol)
        .Build()
};

var assertion = await fidoSession.GetAssertionAsync(assertionOptions);
var hmacOutput = assertion.Extensions.HmacSecret;
```

### Large Blob Storage

```csharp
using Yubico.YubiKit.Fido2.LargeBlobs;

// Store data associated with a credential
var blobData = Encoding.UTF8.GetBytes("Secret application data");
await fidoSession.WriteLargeBlobAsync(credentialId, blobData);

// Retrieve blob
var retrievedBlob = await fidoSession.ReadLargeBlobAsync(credentialId);
Console.WriteLine($"Retrieved: {Encoding.UTF8.GetString(retrievedBlob)}");
```

### Authenticator Configuration

```csharp
using Yubico.YubiKit.Fido2.Config;

// Enable Enterprise Attestation (firmware 5.4+)
await fidoSession.EnableEnterpriseAttestationAsync();

// Toggle Always-Require-UV
await fidoSession.SetAlwaysRequireUvAsync(enabled: true);

// Set minimum PIN length (firmware 5.4+)
await fidoSession.SetMinPinLengthAsync(minPinLength: 8);
```

## PIN Management

```csharp
// Set initial PIN
await fidoSession.SetPinAsync(newPin);

// Change PIN
await fidoSession.ChangePinAsync(currentPin, newPin);

// Get PIN retries
var retries = await fidoSession.GetPinRetriesAsync();
Console.WriteLine($"PIN attempts remaining: {retries.RetriesRemaining}");

// Get UV retries (biometric)
var uvRetries = await fidoSession.GetUvRetriesAsync();
```

## Key Classes

| Class | Purpose |
|-------|---------|
| `FidoSession` | Main session for FIDO2 operations |
| `AuthenticatorInfo` | Device capabilities and version information |
| `MakeCredentialOptions` / `MakeCredentialResponse` | Credential registration |
| `GetAssertionOptions` / `GetAssertionResponse` | Authentication |
| `CredentialManagement` | Manage discoverable credentials |
| `FingerprintBioEnrollment` | Biometric enrollment (YubiKey Bio) |
| `LargeBlobStorage` | Per-credential blob storage |
| `AuthenticatorConfig` | Device configuration |
| `ClientPin` | PIN/UV protocol operations |
| `ExtensionBuilder` | Build WebAuthn extension inputs |

## Firmware Version Features

Different features require specific firmware versions:

- **5.0+**: CTAP 2.0, MakeCredential, GetAssertion, PIN
- **5.2+**: Biometric enrollment (YubiKey Bio only), credBlob extension
- **5.3+**: Large blob storage
- **5.4+**: Authenticator config, Enterprise Attestation, minimum PIN length
- **5.7+**: Always-Require-UV
- **5.8+**: PRF extension (HMAC-secret v2)

## Security Considerations

- **PIN Security**: Always prompt users securely; never log PINs
- **User Verification**: Enforce UV for high-value operations
- **Attestation**: Validate attestation statements in production
- **Timeout Handling**: FIDO2 operations can timeout waiting for user interaction
- **Resident Key Limits**: YubiKeys have limited resident key storage (~25-32 credentials)

## Transport Differences

| Transport | Connection Type | Use Case |
|-----------|----------------|----------|
| USB HID | `IFidoConnection` | Primary FIDO2 interface |
| NFC SmartCard | `ISmartCardConnection` | Mobile NFC only |
| USB CCID | ❌ NOT SUPPORTED | Use HID instead |

## Common Patterns

### Check Feature Support

```csharp
var info = await fidoSession.GetInfoAsync();

if (info.SupportsResidentKeys)
{
    // Can use rk=true
}

if (info.Extensions.Contains("credProtect"))
{
    // Can use credProtect extension
}

if (info.CtapVersion >= new Version(2, 1))
{
    // Can use CTAP 2.1+ features
}
```

### Handle User Interaction

```csharp
try
{
    var response = await fidoSession.MakeCredentialAsync(options);
}
catch (CtapException ex) when (ex.StatusCode == CtapStatus.UserActionTimeout)
{
    Console.WriteLine("User didn't touch the YubiKey in time");
}
catch (CtapException ex) when (ex.StatusCode == CtapStatus.PinInvalid)
{
    Console.WriteLine("Incorrect PIN");
}
```

## Developer Documentation

For implementation details, CBOR encoding, and test patterns, see [CLAUDE.md](CLAUDE.md).

## References

- **WebAuthn Specification**: https://www.w3.org/TR/webauthn/
- **CTAP 2.1 Specification**: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-20210615.html
- **YubiKey FIDO2**: https://developers.yubico.com/FIDO2/
