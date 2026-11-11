using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management;
namespace AuditIntegration20261111.Example04;
public static class Example
{
public static async Task RunAsync(CancellationToken cancellationToken, int serial)
{
// Every YubiKey reachable as a smart card (CCID); HID is never enumerated.
var smartCardYubiKeys = await YubiKeyManager.FindAllAsync(interfaces: ConnectionType.SmartCard, cancellationToken: cancellationToken);

// Every YubiKey reachable over FIDO HID. HID descriptors are enumerated, but only FIDO interfaces are probed.
var fidoYubiKeys = await YubiKeyManager.FindAllAsync(interfaces: ConnectionType.HidFido, cancellationToken: cancellationToken);

// Every YubiKey on every interface, still without publishing into a monitor feed.
var allYubiKeys = await YubiKeyManager.FindAllAsync(cancellationToken: cancellationToken);

// Either family can discover the selected physical key. No OTP slots/protocol probes are admitted.
var selected = await YubiKeyManager.FindFirstAsync(
    predicate: key => key.SerialNumber == serial,
    interfaces: ConnectionType.SmartCard | ConnectionType.HidFido,
    cancellationToken: cancellationToken);

}
}
