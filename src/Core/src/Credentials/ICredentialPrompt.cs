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

/// <summary>
/// The kind of secret being requested by an <see cref="ICredentialPrompt"/>.
/// </summary>
/// <remarks>
/// Implementations should treat an unrecognized value as a generic secret
/// request rather than failing, so that new kinds can be added without
/// breaking existing implementations.
/// </remarks>
public enum CredentialKind
{
    /// <summary>A PIN (personal identification number).</summary>
    Pin,

    /// <summary>A PUK (PIN unblocking key).</summary>
    Puk,

    /// <summary>A password or passphrase.</summary>
    Password,

    /// <summary>A management key, conventionally entered as hexadecimal.</summary>
    ManagementKey,

    /// <summary>A new PIN being established by a change or initialize flow.</summary>
    NewPin,

    /// <summary>A new PUK being established.</summary>
    NewPuk,

    /// <summary>A new password being established.</summary>
    NewPassword,

    /// <summary>A reset code, for example the OpenPGP resetting code.</summary>
    ResetCode
}

/// <summary>
/// Describes a single credential request made through <see cref="ICredentialPrompt"/>.
/// </summary>
/// <remarks>
/// This is a non-positional record so that optional properties can be added in
/// later releases without breaking implementations or callers. Implementations
/// should ignore properties they do not use.
/// </remarks>
public record CredentialPromptContext
{
    /// <summary>Gets the kind of secret being requested.</summary>
    public required CredentialKind Kind { get; init; }

    /// <summary>
    /// Gets a display-oriented description of what the secret unlocks, such as
    /// a relying-party identifier (<c>"example.com"</c>) for WebAuthn or an
    /// application name (<c>"PIV"</c>) elsewhere. May be <c>null</c> when no
    /// meaningful scope exists.
    /// </summary>
    public string? Scope { get; init; }

    /// <summary>
    /// Gets the number of attempts remaining before the credential is blocked,
    /// when the protocol reports it; otherwise <c>null</c>.
    /// </summary>
    public int? RetriesRemaining { get; init; }

    /// <summary>
    /// Gets a value indicating whether this request follows a rejected attempt.
    /// </summary>
    public bool IsRetry { get; init; }

    /// <summary>
    /// Gets the minimum acceptable secret length, measured in encoded bytes.
    /// </summary>
    /// <remarks>
    /// Lengths are expressed in bytes rather than characters because protocol
    /// limits are defined over the encoded form; a multi-byte character can
    /// consume more than one byte of the allowance.
    /// </remarks>
    public int MinLengthBytes { get; init; }

    /// <summary>
    /// Gets the maximum acceptable secret length, measured in encoded bytes.
    /// </summary>
    public int MaxLengthBytes { get; init; } = 255;

    /// <summary>
    /// Gets a value indicating whether the implementation should ask for the
    /// secret twice and confirm the entries match, which is conventional when
    /// establishing a new secret.
    /// </summary>
    public bool RequiresConfirmation { get; init; }
}