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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.Oath;

namespace Audit20261111.DocExamples.Full;
internal static class Body183
{
internal static async Task CompileOnly(int serial, CancellationToken cancellationToken)
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
