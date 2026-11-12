using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

namespace Audit20261111.DocExamples.Full;
internal static class Body383
{
static async Task ProtectedAdministrationRoundTripAsync(
    IYubiKey device,
    ReadOnlyMemory<byte> pin,
    ReadOnlyMemory<byte> activeManagementKey,
    CancellationToken cancellationToken)
{
    await using (var setup = await device.CreatePivSessionAsync(cancellationToken: cancellationToken))
    {
        await setup.AuthenticateAsync(activeManagementKey, cancellationToken);
        await setup.SetPinOnlyModeAsync(PinOnlyMode.PinProtected, pin, activeManagementKey, cancellationToken);
    }

    await using (var reopened = await device.CreatePivSessionAsync(cancellationToken: cancellationToken))
    {
        PinOnlyMode detected = await reopened.GetPinOnlyModeAsync(cancellationToken);
        if (!detected.HasFlag(PinOnlyMode.PinProtected))
            throw new InvalidOperationException("Inspect ADMIN DATA before continuing.");
        PinOnlyMode recovered = await reopened.RecoverPinOnlyModeAsync(pin, cancellationToken);
        if (!recovered.HasFlag(PinOnlyMode.PinProtected) || !reopened.IsManagementKeyAuthenticated)
            throw new InvalidOperationException("Use the backed-up active management key to recover access.");

        // Perform authorized administration here, then restore the type-appropriate public default.
        await reopened.SetPinOnlyModeAsync(PinOnlyMode.None, pin, cancellationToken: cancellationToken);
    }
}
}
