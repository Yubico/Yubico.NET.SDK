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
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Piv;
namespace Audit20261111.DocExamples.External97;
internal static class Body
{
static async Task EnableProtectedAdministrationAsync(
    IYubiKey device,
    ReadOnlyMemory<byte> pin,
    ReadOnlyMemory<byte> activeManagementKey,
    CancellationToken cancellationToken)
{
    await using var piv = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);
    await piv.AuthenticateAsync(activeManagementKey, cancellationToken);
    await piv.SetPinOnlyModeAsync(
        PinOnlyMode.PinProtected,
        pin,
        activeManagementKey,
        cancellationToken);
}
}
