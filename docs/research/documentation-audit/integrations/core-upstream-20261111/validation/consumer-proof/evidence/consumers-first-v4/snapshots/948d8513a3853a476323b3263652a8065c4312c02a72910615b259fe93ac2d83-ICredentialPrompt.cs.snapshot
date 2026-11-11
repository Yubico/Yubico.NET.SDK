// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Buffers;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.Credentials;

/// <summary>
/// Supplies secrets (PINs, passwords, keys) to SDK components on demand.
/// </summary>
/// <remarks>
/// <para>
/// Implementations decide how a secret is obtained: console input, a graphical
/// dialog, a secret vault, or a test fake. The SDK calls this interface only
/// when it needs a secret it was not given explicitly, so an application that
/// passes secrets directly never needs an implementation.
/// </para>
/// <para>
/// This is an SDK-to-application callback with protocol context and asynchronous cancellation.
/// It is distinct from <see cref="ISecureCredentialReader"/>, which is a synchronous,
/// application-initiated terminal input helper with display and encoding options. An implementation
/// may delegate to any credential acquisition mechanism, including a secure reader, provided it
/// returns promptly without blocking the calling thread and honors the cancellation token.
/// </para>
/// <para><b>Contract</b></para>
/// <list type="bullet">
/// <item>
/// Returning <c>null</c> means exactly one thing: the user declined. It never
/// signals cancellation, an input error, or a retry request.
/// </item>
/// <item>
/// The method must return a <see cref="ValueTask{TResult}"/> promptly, must not block the calling
/// thread while obtaining the secret, and must honor the supplied cancellation token.
/// Cancellation is reported by throwing <see cref="OperationCanceledException"/>.
/// </item>
/// <item>
/// The returned <see cref="IMemoryOwner{T}"/> must expose a
/// <see cref="IMemoryOwner{T}.Memory"/> sized <b>exactly</b> to the secret.
/// Pool-rented buffers are frequently larger than requested, and the SDK
/// transmits the whole of <c>Memory</c>; an over-sized buffer therefore sends
/// trailing padding as part of the secret. Use
/// <see cref="DisposableArrayPoolBuffer.CreateFromSpan"/>, which slices to the
/// exact length, rather than handing back a raw rented buffer.
/// </item>
/// <item>
/// The implementation owns the buffer until the returned <see cref="ValueTask{TResult}"/>
/// completes successfully. At that point ownership transfers to the SDK. The implementation must
/// not access or dispose the buffer after successful completion. A null, faulted, or cancelled
/// result transfers no buffer ownership.
/// </item>
/// <item>
/// Implementations must not retry credential verification internally. The SDK component using the
/// prompt owns any retry policy so each authenticator submission is represented by a separate
/// prompt invocation and context.
/// </item>
/// </list>
/// <para><b>Example</b></para>
/// <code>
/// internal sealed class DialogPrompt : ICredentialPrompt
/// {
///     public async ValueTask&lt;IMemoryOwner&lt;byte&gt;?&gt; RequestSecretAsync(
///         CredentialPromptContext context, CancellationToken cancellationToken)
///     {
///         var title = context.IsRetry
///             ? $"Incorrect PIN - {context.RetriesRemaining} attempts remaining"
///             : $"Enter PIN for {context.Scope}";
///
///         byte[]? entered = await ShowPinDialogAsync(title, cancellationToken);
///         if (entered is null)
///         {
///             return null;
///         }
///
///         try
///         {
///             return DisposableArrayPoolBuffer.CreateFromSpan(entered);
///         }
///         finally
///         {
///             System.Security.Cryptography.CryptographicOperations.ZeroMemory(entered);
///         }
///     }
/// }
/// </code>
/// </remarks>
public interface ICredentialPrompt
{
    /// <summary>
    /// Requests a secret from the user or another source.
    /// </summary>
    /// <param name="context">Describes what is being requested and why.</param>
    /// <param name="cancellationToken">Token that the implementation must monitor for cancellation requests.</param>
    /// <returns>
    /// The secret bytes in a buffer whose <see cref="IMemoryOwner{T}.Memory"/> is
    /// sized exactly to the secret, or <c>null</c> if the user declined to supply one. Ownership
    /// transfers to the caller only when the returned task completes successfully with a non-null
    /// owner.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when cancellation is requested through <paramref name="cancellationToken"/>.
    /// </exception>
    ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(
        CredentialPromptContext context,
        CancellationToken cancellationToken);
}