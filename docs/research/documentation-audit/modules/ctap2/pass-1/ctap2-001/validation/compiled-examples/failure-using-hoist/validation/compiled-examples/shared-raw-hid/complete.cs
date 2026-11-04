using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;

namespace AuditCt001.Prepared.shared_raw_hid;

public static class Example
{
public static async Task ExampleAsync(IYubiKey yubiKey, byte ctapHidCommand, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
{
await using RawCtapHidSession raw =
    await yubiKey.CreateRawCtapHidSessionAsync(cancellationToken);

ReadOnlyMemory<byte> response = await raw.SendAndReceiveAsync(
    ctapHidCommand,
    payload,
    cancellationToken);
}
}
