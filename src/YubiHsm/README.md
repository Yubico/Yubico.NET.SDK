# Yubico.YubiKit.YubiHsm

The YubiHSM Auth applet stores the credentials used to authenticate to a YubiHSM 2 hardware security module, so
the long-lived HSM authentication keys live on a YubiKey instead of on the host. This package manages those
credentials and derives the S-ENC, S-MAC, and S-RMAC keys for an HSM session. Your application uses an HSM
library to provision the HSM, complete authentication, and exchange protected commands through its connector.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Requirements

- .NET 10 on Windows, macOS, or Linux; Linux also needs PC/SC and udev rules ([Linux setup](../../docs/linux-setup.md)).
- SmartCard transport, over USB CCID or NFC, with the YubiHSM Auth application enabled. See Yubico's
  [enablement guidance](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-yubihsm-auth.html).

| Feature | SDK version gate |
| --- | --- |
| YubiHSM Auth applet | 5.4.3 |
| Asymmetric credentials, `GetChallengeAsync` | 5.6.0 |
| `GetChallengeAsync` with a credential password | 5.7.1 |
| Credential password change | 5.8.0 |

A session normally resolves feature support from the applet's three-byte SELECT version, falling back to
5.4.3 when that value is absent or has another length. `FirmwareVersionOverride` supplies caller-selected
policy instead. Choose an override that the actual applet supports: device metadata, a development-build
label, and an override alone provide different evidence from successfully executing an operation.
`ResetAsync` reselects the applet and retains the session's resolved version policy.
An override does not skip SELECT, and malformed TLV framing can fail before fallback. Fixture/discovery
metadata is a separate version source; it is not substituted by the HSM session factory.

A credential is identified by a public label of 1 to 64 UTF-8 bytes, represented as a plain `string`. A
symmetric credential holds an AES-128 K-ENC and K-MAC pair, an asymmetric one an EC P-256 private key, and each is
protected by a 16-byte credential password. Keep these three secret roles separate:

| Secret | Purpose |
| --- | --- |
| 16-byte management key | Authorizes credential enrollment/deletion, management-key rotation and administrator password changes. |
| `CredentialPassword` | Authorizes use of a stored YubiKey credential; create it from UTF-8 text bytes or exactly 16 raw bytes. |
| Derivation password | Supplies PBKDF2 input when deriving the long-lived K-ENC/K-MAC pair for symmetric HSM authentication. |

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.YubiHsm --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Getting started

```csharp
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.YubiHsm;

IYubiKey device = await YubiKeyManager.FindFirstAsync();
await using var session = await device.CreateHsmAuthSessionAsync();
foreach (var credential in await session.ListCredentialsAsync())
{
    Console.WriteLine($"{credential.Label}: {credential.Algorithm}, {credential.RetriesRemaining} retries");
}
```

This complete listing example needs no password, management key, or touch.
`device.ListHsmAuthCredentialsAsync()` performs the same task with automatic session disposal. LIST returns
snapshots; unknown algorithm identifiers remain available as enum values and unknown touch values appear as
`null`. Check the algorithm before choosing a calculation method. Record equality compares all properties;
sorting compares only labels using ordinal, case-insensitive ordering.

Later C# blocks use the directives above. Methods take a selected `IYubiKey` explicitly, and own only the
resources they create. Obtain secret bytes through your application's secure input/storage layer and clear
them in its enclosing `finally` block. Literal passwords are suitable only for disposable test credentials.

## Common operations

### Enroll a symmetric credential on both devices

1. Select an authorized HSM authentication-key object ID and a YubiKey credential label. Record their
   mapping in your application; these identifiers serve different devices.
2. Provision identical K-ENC and K-MAC values in the HSM object and the YubiKey credential. Configure the
   HSM object's domains/capabilities through your HSM library and authorized provisioning workflow.
3. Keep any required recovery copy in protected storage before clearing the enrollment buffers. If the
   second device fails enrollment, reconcile the partial state before using or retrying the credential.

This **application-dependent sketch** accepts a typed provisioning callback. Implement it with your HSM
library: it must install the supplied keys in the selected object, persist the label/object mapping and
any required recovery material, and complete before returning. It borrows the key memories only for the
call; the caller owns the management-key and credential-password inputs for this method's full duration.

```csharp
static async Task EnrollSymmetricAsync(
    IYubiKey device,
    ReadOnlyMemory<byte> managementKey,
    CredentialPassword credentialPassword,
    string label,
    ushort hsmAuthenticationKeyId,
    Func<ushort, string, ReadOnlyMemory<byte>, ReadOnlyMemory<byte>, CancellationToken, Task> provisionHsmAsync,
    CancellationToken cancellationToken)
{
    byte[] keyMaterial = RandomNumberGenerator.GetBytes(32);
    try
    {
        await provisionHsmAsync(hsmAuthenticationKeyId, label,
            keyMaterial.AsMemory(0, 16), keyMaterial.AsMemory(16, 16), cancellationToken);
        await using var session = await device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);
        await session.PutCredentialSymmetricAsync(managementKey, label,
            keyMaterial.AsMemory(0, 16), keyMaterial.AsMemory(16, 16), credentialPassword,
            touchRequired: true, cancellationToken: cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(keyMaterial);
    }
}
```

`PutCredentialDerivedAsync` derives the pair using PBKDF2-HMAC-SHA256, salt `"Yubico"`, 10,000 iterations
and a 32-byte result split into K-ENC then K-MAC. Enroll the same derived pair in the HSM using its supported
derivation/provisioning facility; the credential password remains a separate YubiKey access secret.

For asymmetric enrollment, call `GenerateCredentialAsymmetricAsync`, export the credential's long-lived
public key (PK-OCE) with `GetPublicKeyAsync`, and enroll that public key in the matching HSM authentication
object. The YubiKey keeps the generated private key on-device. Your provisioning process must also obtain
and authenticate the **HSM's static public key** for later calculations. `GetPublicKeyAsync` identifies the
YubiKey credential, so use your HSM library/trust process to obtain the HSM key.

### Calculate session keys

The host starts the HSM handshake using the enrolled HSM authentication-key ID. For symmetric credentials,
the context is `hostChallenge[8] || hsmChallenge[8]`, in that order. The HSM generates its challenge and card
cryptogram; the connector relays them. Supply the actual HSM response to CALCULATE. The optional card
cryptogram asks the applet to verify the HSM cryptogram; omitting it yields keys without that verification.
Your HSM library still completes host authentication and manages protected command/response state.

The session takes three 16-byte keys from the first 48 CALCULATE data bytes, ignores any suffix,
and clears the entire final response it owns. Fewer than 48 data bytes raise `BadResponseException`.
Standalone `SessionKeys.Parse` still accepts exactly the 48-byte key region. A 56-byte response was
previously observed on a development YubiKey; this software contract neither assigns meaning to the
extra bytes nor qualifies a firmware range. Verify the complete authenticated HSM flow on the target applet.

The following **application-dependent sketch** declares the application adapter contract explicitly.
Implement `ISymmetricHsmExchange` against your HSM library. `BeginAsync` opens the intended enrolled
HSM object and transfers ownership of its returned arrays to this method. `AuthenticateAndUseAsync` completes
host authentication and performs your HSM task while borrowing the keys; the adapter owns and clears any
copies it retains. Its asynchronous disposal closes the HSM session/connector even on failure. The factory
creates a fresh owned adapter and cleans up partial construction if it throws. The input password is borrowed.

```csharp
interface ISymmetricHsmExchange : IAsyncDisposable
{
    Task<(byte[] HsmChallenge, byte[] CardCryptogram)> BeginAsync(
        ushort authenticationKeyId, ReadOnlyMemory<byte> hostChallenge, CancellationToken cancellationToken);
    Task AuthenticateAndUseAsync(SessionKeys keys, CancellationToken cancellationToken);
}

static async Task UseSymmetricHsmAsync(
    IYubiKey device, string label, ushort hsmAuthenticationKeyId,
    CredentialPassword credentialPassword,
    Func<CancellationToken, Task<ISymmetricHsmExchange>> openExchangeAsync,
    CancellationToken cancellationToken)
{
    byte[] hostChallenge = RandomNumberGenerator.GetBytes(8);
    byte[]? context = null;
    byte[]? hsmChallenge = null;
    byte[]? cardCryptogram = null;
    try
    {
        context = new byte[16];
        await using var exchange = await openExchangeAsync(cancellationToken);
        (hsmChallenge, cardCryptogram) = await exchange.BeginAsync(
            hsmAuthenticationKeyId, hostChallenge, cancellationToken);
        if (hsmChallenge.Length != 8 || cardCryptogram.Length != 8)
            throw new InvalidOperationException("Unexpected symmetric HSM handshake response.");

        hostChallenge.CopyTo(context, 0);
        hsmChallenge.CopyTo(context, 8);
        await using var session = await device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);
        using var keys = await session.CalculateSessionKeysSymmetricAsync(
            label, context, credentialPassword, cardCryptogram, cancellationToken);
        await exchange.AuthenticateAndUseAsync(keys, cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(hostChallenge);
        if (context is not null) CryptographicOperations.ZeroMemory(context);
        if (hsmChallenge is not null) CryptographicOperations.ZeroMemory(hsmChallenge);
        if (cardCryptogram is not null) CryptographicOperations.ZeroMemory(cardCryptogram);
    }
}
```

The sketch uses a fresh host-generated challenge, including on base-app supported firmware. From 5.6.0,
`GetChallengeAsync` can obtain a symmetric host challenge from the applet. For asymmetric authentication,
obtain EPK-OCE from `GetChallengeAsync` and pass the credential password when the session supports the 5.7.1
password gate. On 5.6.0–5.7.0 the SDK omits a supplied password. Check the returned shape before proceeding;
the SDK returns GET_CHALLENGE bytes without validating their length or point encoding.

The asymmetric CALCULATE context is `EPK-OCE[65] || EPK-SD[65]`, using uncompressed points. Obtain EPK-SD
and the required cryptogram from the HSM handshake, and supply the separately trusted HSM static public key.
The ephemeral EPK-OCE and the credential's long-lived PK-OCE serve different protocol steps. Locally generated
HSM inputs are useful for isolated applet tests; a live HSM exchange needs the HSM's own inputs.

### Rotate the management key

Persist a recoverable replacement in protected storage **before** installing it. Retain the current key
until the outcome is established; a communication failure can leave the result uncertain, so reconcile it
without repeated guesses. Losing the active management key requires a destructive applet reset to recover
management access. The factory key is 16 zero bytes; replace it for any deployment beyond evaluation.

This **application-dependent sketch** accepts a persistence callback that durably stores the candidate key
with the device identity/recovery information and returns only after success. The callback borrows the
memory for the call; its storage layer owns any retained copy. The method borrows the current key.

```csharp
static async Task RotateManagementKeyAsync(
    IYubiKey device, ReadOnlyMemory<byte> currentManagementKey,
    Func<ReadOnlyMemory<byte>, CancellationToken, Task> persistRecoverableKeyAsync,
    CancellationToken cancellationToken)
{
    byte[] replacement = RandomNumberGenerator.GetBytes(16);
    try
    {
        await persistRecoverableKeyAsync(replacement, cancellationToken);
        await using var session = await device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);
        await session.PutManagementKeyAsync(currentManagementKey, replacement, cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(replacement);
    }
}
```

## User interaction

A credential stored with `touchRequired: true` needs a physical touch during a session-key calculation; no
operation needs a PIN. Supply an `IUserPresencePrompt` to be notified around the blocking CALCULATE exchange.

```csharp
sealed class TouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) =>
        new(Console.Out.WriteLineAsync($"Touch your YubiKey for {context.Application} {context.Scope}."));
}
```

```csharp
var options = new SessionCreationOptions { UserPresencePrompt = new TouchPrompt() };
await using var session = await device.CreateHsmAuthSessionAsync(options);
```

The prompt type and session-options block above are **composable fragments**; the latter borrows the
previously selected `device`. `TouchRequired = true` maps to `UserPresenceBasis.PolicyRequires`; `false` and
a missing credential are silent. An unknown touch value or a LIST failure maps to `PolicyMayRequire`, while
LIST cancellation propagates. A configured prompt causes this policy lookup before CALCULATE; with no prompt
the session sends CALCULATE directly. Prompt callbacks should return promptly and avoid reentering the session.
The caller owns the prompt. Pass cancellation tokens to cancellable operations, and follow the shared
[cancellation guidance](../../docs/usage/user-interaction.md#request-and-resolution-contract). After requesting
cancellation, await the operation's terminal result and disposal/cleanup before opening another operation.
An in-flight SmartCard exchange may continue until the device responds; its terminal result can be an APDU
failure rather than `OperationCanceledException`. Use that result to decide recovery instead of treating
the cancellation request as a device-interruption guarantee.

## Constraints

- Requesting a non-SmartCard transport through `SessionCreationOptions { PreferredConnectionType = ... }` throws.
- Within this process, grouped views admit one live connection across known member IDs, and each connection
  admits one session. Disjoint scoped views remain uncoordinated. Whoever creates a connection disposes
  it with `await using`; a session from `CreateHsmAuthSessionAsync` owns the one it opened.
  See [connection ownership](../../docs/architecture/connection-ownership-and-contention.md).
- Concurrent use of one session is unsupported. Await the entire public workflow before another operation
  or disposal. Optional LIST policy lookup,
  CALCULATE, parsing and callbacks are separate phases; Core drains an admitted exchange, not all later
  application work. Keep borrowed inputs/prompt resources alive until the task is terminal.
- `ResetAsync` (no undo), `DeleteCredentialAsync`, and `PutManagementKeyAsync` change persistent applet state.
  A confirmed reset refreshes the same session: it reselects the applet on a fresh protocol and runs a new
  SCP handshake with the caller's original parameters if SCP was requested. Keep those parameters and key
  owners alive through session disposal. If refresh fails, the applet is already reset and the session ends;
  open a new session to recover rather than repeating the destructive command.
- `FirmwareVersion` describes effective feature policy, not a physical version measurement. An explicit override
  wins, followed by cached device metadata if available, then applet SELECT version data. Unknown or prerelease
  firmware permits applet operations optimistically; required SELECT errors and cancellation still propagate.
- Unsupported feature gates throw `NotSupportedException`; on versions 5.6.0–5.7.0 `GetChallengeAsync`
  omits a supplied password.
- Enrollment, deletion, management-key changes, both CALCULATE methods and both password-change methods
  translate retry-count status words into `HsmAuthRetryException` (an `ApduException`) with `RetriesRemaining`.
  `GetChallengeAsync` uses general `ApduException` errors, including with a password; inspect the status word
  before deciding on another attempt. The SDK performs one attempt per call.
- A context of the wrong length throws `ArgumentException` before calculation device I/O.
- Yubico's [YubiHSM Auth overview](https://docs.yubico.com/yesdk/users-manual/application-yubihsm-auth/yubihsm-auth-overview.html)
  describes eight initial retries and restoration to eight after successful authentication before exhaustion.
  Exhausting a credential's password retries permanently deletes that credential. Exhausting management-key
  retries blocks management operations until an applet reset, which erases all credentials and restores defaults.

Credential import and on-device generation send PUT directly, without a host pre-read or overwrite guard.
If the applet accepts PUT at an existing label, treat the existing credential as overwritten. A duplicate-label
rejection from the applet is not converted into replacement; the device error propagates.

## Security notes

- Migrate old credential-password byte arguments with `using var password = CredentialPassword.FromUtf8(input.Span);`
  for valid UTF-8 of at most 16 bytes (zero-padded), or `CredentialPassword.FromRaw(input.Span)` for exactly 16
  opaque bytes. Keep the holder alive through the awaited operation; disposal clears its copy, not the source.
  Acquire secrets in clearable buffers and clear those buffers separately. Derivation passwords, management
  keys and EC private keys remain borrowed `ReadOnlyMemory<byte>`.
  The SDK clears its owned request copies. Clear your input buffers with
  `CryptographicOperations.ZeroMemory` in a `finally`, including when acquisition or a later operation fails.
- `SessionKeys.Parse` copies the response into three owned arrays; disposal clears those arrays and later
  property access throws. Existing spans view the cleared storage. Clear the Parse source and any caller-made
  copies separately, and keep `SessionKeys` in a `using` scope through the HSM task.
  The calculation helper clears its final successful CALCULATE response even if strict parsing or the
  success callback fails. This does not clear independent transport, fragment or decrypted-response copies;
  retry/backend failures can occur before that final-response cleanup scope.
- `PutCredentialDerivedAsync` derives K-ENC and K-MAC with PBKDF2-HMAC-SHA256, salt `"Yubico"`, 10,000 iterations.
- Never log passwords, key material, challenges, or cryptograms. Log labels, algorithms, and status words only.

Inherited `IsAuthenticated` records Core SCP setup, independently of the applet's per-operation management
key/password checks and the application's external HSM authentication. It is not an ongoing channel-health check.

## Example

HsmAuthTool is **demo only, not for production**. It prompts for passwords and keys rather than taking
them as command-line options. Secret prompts require an interactive console for masked input.
The demo limits derivation passphrases to 128 UTF-16 code units; this is not an SDK or applet limit.
Generated symmetric keys and calculated session keys are explicitly printed, so use disposable test credentials.
The calculation menu lists only symmetric credentials and supplies a console touch prompt. It does not enroll
an HSM or complete the HSM handshake. From the repository root:

```bash
dotnet run --project src/YubiHsm/examples/HsmAuthTool/HsmAuthTool.csproj
```

## Related

- [Core](../Core/README.md) - device discovery, SmartCard protocol, and logging configuration.
- [Security Domain](../SecurityDomain/README.md) - SCP keys for running this session over a secure channel.
- [User interaction](../../docs/usage/user-interaction.md) and [device discovery](../../docs/usage/device-discovery.md) - cross-module guides.
- [YubiHSM Auth](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-yubihsm-auth.html) - the applet.
- [Developer guide](../../docs/DEV-GUIDE.md): building, testing, and contributing.
