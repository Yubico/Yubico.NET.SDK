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
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Management;

namespace Audit20261111.DocExamples.Full;
internal static class Body264
{
internal static void CompileOnly()
{
// Flags enum - use bitwise operations
var capabilities = DeviceCapabilities.Piv | DeviceCapabilities.Oath;

// Check if specific capability is set
bool hasPiv = (capabilities & DeviceCapabilities.Piv) != 0;

// Add capability
capabilities |= DeviceCapabilities.Fido2;

// Remove capability  
capabilities &= ~DeviceCapabilities.Otp;

// Check multiple
bool hasPivAndOath = (capabilities & (DeviceCapabilities.Piv | DeviceCapabilities.Oath)) 
    == (DeviceCapabilities.Piv | DeviceCapabilities.Oath);
}

}
