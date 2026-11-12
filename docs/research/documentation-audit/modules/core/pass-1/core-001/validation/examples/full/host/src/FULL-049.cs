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
internal static class Body49
{
// ✅ BEST - Small, immutable, value semantics
public readonly struct StatusWord
{
    public StatusWord(byte sw1, byte sw2) => (SW1, SW2) = (sw1, sw2);
    
    public byte SW1 { get; }
    public byte SW2 { get; }
    public ushort Value => (ushort)((SW1 << 8) | SW2);
    
    public bool IsSuccess => Value == 0x9000;
}

// ✅ GOOD - Wraps small data
public readonly struct SlotNumber
{
    public SlotNumber(byte value) => Value = value;
    public byte Value { get; }
}

// ✅ GOOD - Protocol metadata
public readonly struct ApduHeader
{
    public ApduHeader(byte cla, byte ins, byte p1, byte p2)
    {
        CLA = cla;
        INS = ins;
        P1 = p1;
        P2 = p2;
    }
    
    public byte CLA { get; }
    public byte INS { get; }
    public byte P1 { get; }
    public byte P2 { get; }
}
}
