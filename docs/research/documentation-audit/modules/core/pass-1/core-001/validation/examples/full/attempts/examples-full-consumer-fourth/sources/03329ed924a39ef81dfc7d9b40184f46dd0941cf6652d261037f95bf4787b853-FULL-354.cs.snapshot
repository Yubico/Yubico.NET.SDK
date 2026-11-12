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
using Yubico.YubiKit.OpenPgp;

namespace Audit20261111.DocExamples.Full;
internal static class Body354
{
static async Task ConfigureInitialKdfAsync(
    IYubiKey device, ReadOnlyMemory<byte> currentAdminPin,
    ReadOnlyMemory<byte> defaultUserPin, ReadOnlyMemory<byte> defaultAdminPin,
    CancellationToken cancellationToken)
{
    byte[] saltUser = RandomNumberGenerator.GetBytes(8);
    byte[] saltReset = RandomNumberGenerator.GetBytes(8);
    byte[] saltAdmin = RandomNumberGenerator.GetBytes(8);
    byte[] hashUser = [];
    byte[] hashAdmin = [];
    try
    {
        using var derivation = new KdfIterSaltedS2k
        {
            HashAlgorithm = KdfHashAlgorithm.Sha256, IterationCount = 100000,
            SaltUser = saltUser, SaltReset = saltReset, SaltAdmin = saltAdmin
        };
        hashUser = derivation.Process(Pw.User, defaultUserPin.Span);
        hashAdmin = derivation.Process(Pw.Admin, defaultAdminPin.Span);
        using var configured = new KdfIterSaltedS2k
        {
            HashAlgorithm = KdfHashAlgorithm.Sha256, IterationCount = 100000,
            SaltUser = saltUser, SaltReset = saltReset, SaltAdmin = saltAdmin,
            InitialHashUser = hashUser, InitialHashAdmin = hashAdmin
        };
        await using (var provisioning = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken))
        {
            await provisioning.VerifyAdminAsync(currentAdminPin, cancellationToken);
            await provisioning.SetKdfAsync(configured, cancellationToken);
        }
        await using var verification = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
        await verification.VerifyAdminAsync(defaultAdminPin, cancellationToken);
        await verification.VerifyPinAsync(defaultUserPin, cancellationToken: cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(hashUser);
        CryptographicOperations.ZeroMemory(hashAdmin);
        CryptographicOperations.ZeroMemory(saltUser);
        CryptographicOperations.ZeroMemory(saltReset);
        CryptographicOperations.ZeroMemory(saltAdmin);
    }
}
}
