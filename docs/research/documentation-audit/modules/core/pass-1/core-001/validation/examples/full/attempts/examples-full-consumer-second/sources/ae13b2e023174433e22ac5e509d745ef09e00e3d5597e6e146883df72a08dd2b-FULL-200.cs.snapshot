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
internal sealed class Body200 : IUserPresencePrompt
{
public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken)
{
    string operation = context.Operation switch
    {
        UserPresenceOperations.Ctap2.GetAssertion => "sign-in",
        UserPresenceOperations.Ctap2.MakeCredential => "passkey registration",
        _ => "key operation"
    };

    // "Touch your YubiKey to approve the transfer of 500 EUR (sign-in)."
    Console.WriteLine(context.Intent is { } intent
        ? $"Touch your YubiKey to {intent} ({operation})."
        : $"Touch your YubiKey to confirm the {operation}.");
    return default;
}
}
