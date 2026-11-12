using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Utilities;
namespace Audit20261111.DocExamples;
// The application supplies a nonblocking async callback that transfers an owned byte array.
internal sealed class DialogPrompt(
    Func<CredentialPromptContext, CancellationToken, ValueTask<byte[]?>> acquire) : ICredentialPrompt
{
    public async ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(
        CredentialPromptContext context, CancellationToken cancellationToken)
    {
        byte[]? entered = await acquire(context, cancellationToken);
        if (entered is null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            // This example requires a nonempty secret; the pooled owner rejects an empty source.
            return DisposableArrayPoolBuffer.CreateFromSpan(entered);
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(entered);
        }
    }
}
