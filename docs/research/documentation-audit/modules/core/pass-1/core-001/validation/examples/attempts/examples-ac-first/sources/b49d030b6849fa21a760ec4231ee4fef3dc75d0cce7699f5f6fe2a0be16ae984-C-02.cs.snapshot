using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Utilities;
namespace Audit20261111.DocExamples;
internal static class ExampleC02
{
static async Task ReadAndVerifyPinAsync(
    Yubico.YubiKit.Piv.IPivSession pivSession, CancellationToken cancellationToken)
{
    var reader = new ConsoleCredentialReader();
    using var credential = reader.ReadCredential(CredentialReaderOptions.ForPin(), cancellationToken);
    if (credential is not null)
    {
        await pivSession.VerifyPinAsync(credential.Memory, cancellationToken);
    }
}
}
