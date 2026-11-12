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
using System.Formats.Cbor;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Cbor;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.CredentialManagement;
using Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.Ctap2.Pin;

namespace Audit20261111.DocExamples.Full;
internal static class Body3
{
internal static async Task CompileOnly(ICtap2Session session)
{
// Get authenticator capabilities
var info = await session.GetInfoAsync();

// Check versions
bool ctap2_1 = info.Versions.Contains("FIDO_2_1");

// Check supported extensions
bool hasHmacSecret = info.Extensions.Contains("hmac-secret");

// Check options
bool hasRk = info.Options.TryGetValue("rk", out var rk) && rk;
}

}
