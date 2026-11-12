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
using Yubico.YubiKit.Core.Credentials;

namespace Audit20261111.DocExamples.Full;
internal static class Body198
{
internal static void CompileOnly(UserPresenceContext context)
{
string message = context.Operation switch
{
    UserPresenceOperations.Ctap2.MakeCredential => "Touch your YubiKey to create a passkey.",
    UserPresenceOperations.Ctap2.GetAssertion => "Touch your YubiKey to sign in.",
    UserPresenceOperations.Piv.SignOrDecrypt => "Touch your YubiKey for the PIV key operation.",
    _ when context.Application == UserPresenceApplications.Oath => "Touch your YubiKey to generate a code.",
    _ => "Touch your YubiKey to continue."
};
}

}
