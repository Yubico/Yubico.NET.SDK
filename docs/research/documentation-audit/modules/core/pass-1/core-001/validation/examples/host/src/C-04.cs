using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Utilities;
namespace Audit20261111.DocExamples;
internal static class ExampleC04
{
static void InspectRsaKey(ReadOnlyMemory<byte> encodedKey)
{
    // encodedKey is borrowed; its caller remains responsible for clearing it.
    using var owner = RSAPrivateKey.CreateFromPkcs8(encodedKey);
    IPrivateKey genericKey = owner;
    RSAPrivateKey rsaKey = genericKey.Cast<RSAPrivateKey>();
    // rsaKey and genericKey alias owner; only owner manages disposal.
}
}
