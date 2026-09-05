# Yubico.YubiKit.YubiHsm

> **Note:** This documentation is subject to change as the module evolves. Please check for updates regularly.

This module provides access to the **YubiHSM Auth** application on a YubiKey. The applet stores
credentials that are used to authenticate to a YubiHSM 2 hardware security module, so the
long-lived HSM authentication keys live on the YubiKey instead of on the host.

## Overview

The YubiHSM Auth applet manages:
- **Symmetric credentials** (AES-128): a K-ENC / K-MAC pair, stored directly or derived from a password via PBKDF2
- **Asymmetric credentials** (EC P-256): a private key, imported or generated on-device
- **Session key derivation**: the applet computes S-ENC, S-MAC, and S-RMAC for a YubiHSM 2 session
- **Management key lifecycle**: the 16-byte key that authorizes credential changes
- **Credential password changes**: user-authenticated and management-key (admin) variants

This module covers the *applet* only. It does not implement YubiHSM 2 connector or object
management — it produces the session keys you then use to talk to the HSM.

## Requirements

- **Minimum firmware**: YubiKey 5.4.3
- **Asymmetric credentials / `GetChallengeAsync`**: 5.6.0+
- **`GetChallengeAsync` with a credential password**: 5.7.1+
- **Credential password change**: 5.8.0+
- **Transport**: SmartCard only (USB CCID or NFC). There is no HID or OTP path.

Firmware gates are enforced with `EnsureSupports(...)`, so calling an unsupported operation
raises a clear error rather than an opaque APDU failure. Use `IsSupported(...)` to branch.

## Usage Example

```csharp
using System.Security.Cryptography;
using Yubico.YubiKit.YubiHsm;

static async Task UseSymmetricCredentialAsync(
    IHsmAuthSession session,
    string label,
    ReadOnlyMemory<byte> hostChallenge,
    ReadOnlyMemory<byte> hsmChallenge,
    ReadOnlyMemory<byte> credentialPasswordUtf8,
    ReadOnlyMemory<byte>? cardCryptogram,
    CancellationToken cancellationToken)
{
    if (hostChallenge.Length != 8 || hsmChallenge.Length != 8)
        throw new ArgumentException("The host and HSM challenges must each be 8 bytes.");

    var context = new byte[16];
    try
    {
        hostChallenge.CopyTo(context);
        hsmChallenge.CopyTo(context.AsMemory(8));

        using var sessionKeys = await session.CalculateSessionKeysSymmetricAsync(
            label,
            context,
            credentialPasswordUtf8,
            cardCryptogram,
            cancellationToken);

        // Use sessionKeys with the YubiHSM connector session.
    }
    finally
    {
        CryptographicOperations.ZeroMemory(context);
    }
}
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

Credential passwords, management keys, and session keys are never logged. Only lengths,
algorithm identifiers, status words, and other non-secret metadata are.

## Key Concepts

### Credentials

A credential is identified by a **label** (1–64 UTF-8 bytes). The label is not a secret — the
LIST command echoes it back verbatim — so it is a plain `string` throughout the API.

| Type | Algorithm | Firmware | Stored material |
|------|-----------|----------|-----------------|
| Symmetric | AES-128 | 5.4.3+ | K-ENC (16 bytes) + K-MAC (16 bytes) |
| Asymmetric | EC P-256 | 5.6.0+ | Private key (32 bytes), imported or generated on-device |

### Credential passwords

Every credential is protected by a credential password. Passwords cross the API as **UTF-8
`ReadOnlyMemory<byte>`**, never `string`:

- The wire format is a fixed 16 bytes. The SDK accepts **at most** 16 UTF-8 bytes and
  null-pads shorter values for you.
- **You own the buffer.** The SDK zeroes its own padded copy, but never the array you passed in.
  Zero it yourself in a `finally`, or hand in a buffer type that zeroes on disposal.

### Management key

A 16-byte key (default: all zeros) authorizing credential add/delete and admin password changes.
A wrong management key returns SW `0x63Cx` and raises `HsmAuthRetryException`, whose
`RetriesRemaining` property carries `x` — read the property, do not parse the message.

### Session keys

`SessionKeys` is `IDisposable` and zeroes S-ENC, S-MAC, and S-RMAC on disposal. Always
`using` it.

## Core API

### Creating a session

```csharp
// Via the IYubiKey extension (recommended)
await using var session = await yubiKey.CreateHsmAuthSessionAsync(
    cancellationToken: cancellationToken);

// Directly from a SmartCard connection, optionally over SCP
await using var scpSession = await HsmAuthSession.CreateAsync(
    connection,
    new SessionCreationOptions { ScpKeyParameters = scpParams },
    cancellationToken: cancellationToken);
```

Applet dependency-injection registration is intentionally absent. Call `HsmAuthSession.CreateAsync` or
`CreateHsmAuthSessionAsync` directly.

The one-shot credential listing method also accepts `SessionCreationOptions`:

```csharp
var credentials = await yubiKey.ListHsmAuthCredentialsAsync(
    new SessionCreationOptions { ScpKeyParameters = scpParams },
    cancellationToken);
```

### Storing credentials

```csharp
// Symmetric, explicit keys
await session.PutCredentialSymmetricAsync(
    managementKey, label, keyEnc, keyMac, credentialPasswordUtf8, touchRequired: false);

// Symmetric, PBKDF2-derived keys
await session.PutCredentialDerivedAsync(
    managementKey, label, derivationPasswordUtf8, credentialPasswordUtf8);

// Asymmetric, explicit private key (fw 5.6.0+)
await session.PutCredentialAsymmetricAsync(
    managementKey, label, privateKey, credentialPasswordUtf8);

// Asymmetric, generated on-device — the private key never leaves the YubiKey (fw 5.6.0+)
await session.GenerateCredentialAsymmetricAsync(
    managementKey, label, credentialPasswordUtf8);
```

### Listing and deleting

```csharp
IReadOnlyList<HsmAuthCredential> credentials = await session.ListCredentialsAsync();
foreach (var credential in credentials)
{
    // credential.Label, .Algorithm, .RetriesRemaining, .TouchRequired
}

await session.DeleteCredentialAsync(managementKey, label);
```

### Calculating session keys

```csharp
using System.Security.Cryptography;

static async Task<SessionKeys> CalculateSymmetricSessionKeysAsync(
    IHsmAuthSession session,
    string label,
    ReadOnlyMemory<byte> hostChallenge,
    ReadOnlyMemory<byte> hsmChallenge,
    ReadOnlyMemory<byte> credentialPasswordUtf8,
    ReadOnlyMemory<byte>? cardCryptogram,
    CancellationToken cancellationToken)
{
    if (hostChallenge.Length != 8 || hsmChallenge.Length != 8)
        throw new ArgumentException("The host and HSM challenges must each be 8 bytes.");

    var context = new byte[16];
    try
    {
        hostChallenge.CopyTo(context);
        hsmChallenge.CopyTo(context.AsMemory(8));
        return await session.CalculateSessionKeysSymmetricAsync(
            label, context, credentialPasswordUtf8, cardCryptogram, cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(context);
    }
}

static async Task<SessionKeys> CalculateAsymmetricSessionKeysAsync(
    IHsmAuthSession session,
    string label,
    ReadOnlyMemory<byte> epkOce,
    ReadOnlyMemory<byte> epkSd,
    ReadOnlyMemory<byte> hsmPublicKey,
    ReadOnlyMemory<byte> credentialPasswordUtf8,
    ReadOnlyMemory<byte> cardCryptogram,
    CancellationToken cancellationToken)
{
    if (epkOce.Length != 65 || epkSd.Length != 65 || hsmPublicKey.Length != 65)
        throw new ArgumentException("EPK-OCE, EPK-SD, and the HSM public key must each be 65-byte uncompressed points.");

    var context = new byte[130];
    try
    {
        epkOce.CopyTo(context);
        epkSd.CopyTo(context.AsMemory(65));
        return await session.CalculateSessionKeysAsymmetricAsync(
            label, context, hsmPublicKey, credentialPasswordUtf8, cardCryptogram, cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(context);
    }
}
```

Obtain `hostChallenge` or `epkOce` first, then send it to the connector. The connector returns
`hsmChallenge` or `epkSd` plus `cardCryptogram`; it also supplies `hsmPublicKey` for asymmetric
authentication. On firmware 5.6.0 and later, `GetChallengeAsync` obtains the YubiKey value. For a
symmetric credential on older supported firmware, generate a fresh 8-byte host challenge with
`RandomNumberGenerator.GetBytes(8)`. Pass the exact value sent to the connector into the helper;
do not call `GetChallengeAsync` again after the exchange. A symmetric `cardCryptogram` is optional,
but omitting it skips mutual authentication of the HSM. The SDK validates context lengths before
device I/O but does not implement the connector handshake. The returned `SessionKeys` must be
disposed by its caller.

### Touch notification

Credentials can require a physical touch. Register a callback to prompt the user before the
blocking CALCULATE exchange:

```csharp
session.OnTouchRequired = () => Console.WriteLine("Touch your YubiKey now...");
```

The callback fires only when the target credential requires touch, or when the touch policy
cannot be determined. It short-circuits with no device I/O if no callback is registered.

### Management key and reset

```csharp
int retries = await session.GetManagementKeyRetriesAsync();
await session.PutManagementKeyAsync(currentManagementKey, newManagementKey);

// Factory reset: deletes ALL credentials and restores the default management key
await session.ResetAsync();
```

### Changing a credential password (fw 5.8.0+)

```csharp
// Authenticated with the current credential password
await session.ChangeCredentialPasswordAsync(label, currentPasswordUtf8, newPasswordUtf8);

// Admin override, authorized by the management key
await session.ChangeCredentialPasswordAdminAsync(managementKey, label, newPasswordUtf8);
```

## PBKDF2 Key Derivation

`PutCredentialDerivedAsync` derives the symmetric key pair from a password:

- Algorithm: PBKDF2-HMAC-SHA256
- Salt: `"Yubico"` (UTF-8)
- Iterations: 10,000
- Output: 32 bytes → K-ENC = `[0..16]`, K-MAC = `[16..32]`

These constants are fixed by the YubiHSM Auth specification and are pinned by a known-answer
unit test. The derived buffer is zeroed after use.

## Error Handling

| Condition | Result |
|-----------|--------|
| Wrong management key | `HsmAuthRetryException` with `RetriesRemaining` |
| Wrong credential password | `HsmAuthRetryException` with `RetriesRemaining` |
| Unsupported firmware | Descriptive exception from `EnsureSupports(...)` |
| Other APDU failures | `ApduException` carrying `SW` and the command header |

`HsmAuthRetryException` derives from `ApduException`, so existing `catch (ApduException)` sites
keep working.

## Project Structure

```
Yubico.YubiKit.YubiHsm/
├── src/
│   ├── HsmAuthSession.cs          # Session implementation, all APDU flows
│   ├── IHsmAuthSession.cs         # Public contract
│   ├── Backend/HsmAuthBackend.cs  # SmartCard select/send transport wrapper
│   ├── SessionKeys.cs             # Disposable S-ENC / S-MAC / S-RMAC container
│   ├── HsmAuthAlgorithm.cs        # Algorithm enum + extension properties
│   ├── HsmAuthCredential.cs       # LIST response record
│   ├── HsmAuthRetryException.cs   # Typed retry-count exception
│   └── IYubiKeyExtensions.cs      # CreateHsmAuthSessionAsync()
├── examples/HsmAuthTool/          # Interactive CLI example
└── tests/
    ├── Yubico.YubiKit.YubiHsm.UnitTests/
    └── Yubico.YubiKit.YubiHsm.IntegrationTests/
```

## Testing Guidance

Run tests with `dotnet toolchain.cs test` — never `dotnet test` directly. Integration tests
require a physical YubiKey with firmware 5.4.3+ and an allow-listed serial number. See
[CLAUDE.md](CLAUDE.md) for detailed test infrastructure information.

## References

- **YubiHSM Auth documentation**: https://developers.yubico.com/YubiHSM2/Usage_Guides/YubiHSM_Auth.html
- **YubiKey documentation**: https://developers.yubico.com/
