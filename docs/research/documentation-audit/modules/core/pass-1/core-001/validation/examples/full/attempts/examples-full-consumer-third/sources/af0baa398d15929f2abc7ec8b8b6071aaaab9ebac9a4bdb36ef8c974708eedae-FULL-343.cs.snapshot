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
using Yubico.YubiKit.YubiHsm;

namespace Audit20261111.DocExamples.Full;
internal static class Body343
{
interface ISymmetricHsmExchange : IAsyncDisposable
{
    Task<(byte[] HsmChallenge, byte[] CardCryptogram)> BeginAsync(
        ushort authenticationKeyId, ReadOnlyMemory<byte> hostChallenge, CancellationToken cancellationToken);
    Task AuthenticateAndUseAsync(SessionKeys keys, CancellationToken cancellationToken);
}

static async Task UseSymmetricHsmAsync(
    IYubiKey device, string label, ushort hsmAuthenticationKeyId,
    ReadOnlyMemory<byte> credentialPassword,
    Func<CancellationToken, Task<ISymmetricHsmExchange>> openExchangeAsync,
    CancellationToken cancellationToken)
{
    byte[] hostChallenge = RandomNumberGenerator.GetBytes(8);
    byte[]? context = null;
    byte[]? hsmChallenge = null;
    byte[]? cardCryptogram = null;
    try
    {
        context = new byte[16];
        await using var exchange = await openExchangeAsync(cancellationToken);
        (hsmChallenge, cardCryptogram) = await exchange.BeginAsync(
            hsmAuthenticationKeyId, hostChallenge, cancellationToken);
        if (hsmChallenge.Length != 8 || cardCryptogram.Length != 8)
            throw new InvalidOperationException("Unexpected symmetric HSM handshake response.");

        hostChallenge.CopyTo(context, 0);
        hsmChallenge.CopyTo(context, 8);
        await using var session = await device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);
        using var keys = await session.CalculateSessionKeysSymmetricAsync(
            label, context, credentialPassword, cardCryptogram, cancellationToken);
        await exchange.AuthenticateAndUseAsync(keys, cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(hostChallenge);
        if (context is not null) CryptographicOperations.ZeroMemory(context);
        if (hsmChallenge is not null) CryptographicOperations.ZeroMemory(hsmChallenge);
        if (cardCryptogram is not null) CryptographicOperations.ZeroMemory(cardCryptogram);
    }
}
}
