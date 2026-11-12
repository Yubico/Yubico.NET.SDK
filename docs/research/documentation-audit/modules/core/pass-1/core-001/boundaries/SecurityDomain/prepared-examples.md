# Typed proposals for later example validation

These are complete composable application methods, **UNCOMPILED / VALID-NOT-CHECKED**. No SDK helper is invented. They need standard System, Collections.Generic, Threading and Tasks namespaces in addition to the imports shown. The supplied device must be the intended authorized fixture. The caller establishes the provenance of the exact known firmware and trusted public key before entering these methods; discovery metadata alone is last-known evidence, and certificate loading alone is not trust validation.

## SCP11b creation with its required effective version

```csharp
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.SecurityDomain;

static async Task<IReadOnlyList<Yubico.YubiKit.SecurityDomain.KeyInfo>> ReadKeyMetadataAsync(
    IYubiKey device,
    FirmwareVersion knownFirmware,
    KeyReference deviceKeyReference,
    ECPublicKey trustedDevicePublicKey,
    CancellationToken cancellationToken)
{
    using var parameters = new Scp11KeyParameters(deviceKeyReference, trustedDevicePublicKey);
    await using var session = await device.CreateSecurityDomainSessionAsync(
        new SessionCreationOptions
        {
            FirmwareVersionOverride = knownFirmware,
            ScpKeyParameters = parameters
        },
        cancellationToken);

    return await session.ListKeyInformationAsync(cancellationToken);
}
```

Use an SCP11b reference constructed with its correct KID and a known supported firmware (normally at least 5.7.2). The session is disposed before parameters. The parameters do not dispose the supplied SD public key. Returned records contain public key metadata, not secret keys. This verifies no host administrative authority and performs no key generation, reset or provisioning. The example's purpose is the missing version/lifetime creation task, not a recommended need for SCP to read public metadata.

## Borrow a connection with caller-owned SCP03 parameters

```csharp
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.SecurityDomain;

static async Task<IReadOnlyList<Yubico.YubiKit.SecurityDomain.KeyInfo>> ReadWithBorrowedKeysAsync(
    IYubiKey device,
    FirmwareVersion knownFirmware,
    Scp03KeyParameters callerOwnedParameters,
    CancellationToken cancellationToken)
{
    await using ISmartCardConnection connection =
        await device.ConnectAsync<ISmartCardConnection>(cancellationToken);
    await using SecurityDomainSession session = await SecurityDomainSession.CreateAsync(
        connection,
        new SessionCreationOptions
        {
            FirmwareVersionOverride = knownFirmware,
            ScpKeyParameters = callerOwnedParameters
        },
        cancellationToken);

    return await session.ListKeyInformationAsync(cancellationToken);
}
```

The caller keeps parameters and their contained StaticKeys alive until the returned task is terminal and disposes them in its enclosing scope. This method owns connection/session only; session disposal precedes connection disposal on success and failure. Outer WaitAsync cancellation does not shorten those lifetimes. No prompt is installed because SD does not use physical-presence notifications. For SCP11a/c, retain one parameter owner for the OCE private key and each contained certificate until every session finishes; a complete trust/provisioning tutorial awaits the selected final/vendor profile rather than a synthetic certificate helper here.

## Existing snippets retained

README getting-started/default SCP03/rotation/reset blocks and tests README/CLAUDE fragments were read as complete blocks with their prerequisites. The default SCP03 example owns its parameter wrapper outside the session; rotation disposes StaticKeys clone and clears caller source in finally; reset uses no SCP and explains destructive limits. The multi-session test fragment uses an independent cleanup token. These are useful retained examples; no fresh compilation is claimed. Reopen them if factory shape, key containment, reset policy or effective version changes. Do not copy the integration tests' undisposed inline Default pattern into consumer examples.
