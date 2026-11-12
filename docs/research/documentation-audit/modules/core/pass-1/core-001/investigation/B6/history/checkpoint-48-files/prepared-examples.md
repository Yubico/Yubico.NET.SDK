# Prepared secure-channel tasks — UNCOMPILED

These are complete source-shaped class examples for the coordinator's existing
consumer-compilation foundation. They are **UNCOMPILED / NOT EXECUTED**, not accepted
product-documentation edits. They require Core and SecurityDomain package references
from this source pin. No real key bytes are embedded. Callers await the complete task.

The first task uses public factory credentials only on an authorized dedicated device
whose factory key set is still installed. A factory-key channel provides transport
protection but gives no exclusive host identity because the keys are public.

The second task consumes an application-owned 48-byte ENC/MAC/DEK buffer, creates the
independent StaticKeys owner, and clears the transferred source even if setup or the
operation fails. The application must load/escrow that source through its own trusted
storage and avoid sharing the transferred buffer concurrently. The borrowed device
object and cancellation token have no disposal ownership transfer. Both methods return
only a count of public key references rather than an undisposed session or secret view.

```csharp
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.SecurityDomain;

public static class SecureChannelTasks
{
    public static async Task<int> CountKeysWithFactoryCredentialsAsync(
        IYubiKey device,
        FirmwareVersion firmwareVersion,
        CancellationToken cancellationToken)
    {
        using Scp03KeyParameters parameters = Scp03KeyParameters.Default;
        await using SecurityDomainSession session =
            await device.CreateSecurityDomainSessionAsync(
                new SessionCreationOptions
                {
                    ScpKeyParameters = parameters,
                    FirmwareVersionOverride = firmwareVersion
                },
                cancellationToken);

        var references = await session.ListKeyInformationAsync(cancellationToken);
        return references.Count;
    }

    public static async Task<int> CountKeysWithTransferredCredentialsAsync(
        IYubiKey device,
        FirmwareVersion firmwareVersion,
        KeyReference reference,
        byte[] ownedKeyMaterial,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ownedKeyMaterial);
        try
        {
            if (ownedKeyMaterial.Length != 48)
                throw new ArgumentException(
                    "Supply three 16-byte SCP03 keys.", nameof(ownedKeyMaterial));

            using var keys = new StaticKeys(
                ownedKeyMaterial.AsSpan(0, 16),
                ownedKeyMaterial.AsSpan(16, 16),
                ownedKeyMaterial.AsSpan(32, 16));
            using var parameters = new Scp03KeyParameters(reference, keys);
            await using SecurityDomainSession session =
                await device.CreateSecurityDomainSessionAsync(
                    new SessionCreationOptions
                    {
                        ScpKeyParameters = parameters,
                        FirmwareVersionOverride = firmwareVersion
                    },
                    cancellationToken);

            var references = await session.ListKeyInformationAsync(cancellationToken);
            return references.Count;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ownedKeyMaterial);
        }
    }
}
```

Session disposal runs before the parameter and StaticKeys scopes. Parameter disposal
also disposes its retained exact StaticKeys; the outer keys scope provides cleanup
if parameter construction throws, and repeated StaticKeys disposal is harmless.
This explicitly explains the shared owner instead of suggesting a hidden key clone.
The convenience session owns its hidden connection and disposes it; the methods own
no caller-created connection. Caller auth/key-slot/firmware/provisioning prerequisites
still determine whether the device accepts the channel.

These tasks avoid selecting a new applet after SCP establishment. The separate raw
SCP SELECT behavior remains B6-C015/B5-C001/C006 qualification, not an unexplained
recommended sequence. A sensitive raw-command example also needs response.Clear in
finally and caller source cleanup after its actual task; changing examples to such
a task requires selecting the actual applet/profile with its boundary owner.
