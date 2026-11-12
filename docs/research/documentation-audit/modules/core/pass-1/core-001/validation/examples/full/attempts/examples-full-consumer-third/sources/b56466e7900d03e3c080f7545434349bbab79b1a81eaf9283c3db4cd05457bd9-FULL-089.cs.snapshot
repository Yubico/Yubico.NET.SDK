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

namespace Audit20261111.DocExamples.Full;
internal sealed class Body89
{
public void SetPin(ReadOnlySpan<byte> pin)
{
    if (pin.Length is < 6 or > 8)
        throw new ArgumentException("PIN must be 6-8 bytes", nameof(pin));

    foreach (byte b in pin)
        if (b is < 0x30 or > 0x39)
            throw new ArgumentException("PIN must contain only digits", nameof(pin));
}
}
