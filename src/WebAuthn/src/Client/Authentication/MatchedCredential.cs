// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Fido2.Credentials;

namespace Yubico.YubiKit.WebAuthn.Client.Authentication;

/// <summary>
/// Represents a credential that matched an authentication request.
/// </summary>
/// <remarks>
/// <para>
/// This class follows the deferred-selection pattern from yubikit-swift:
/// the authenticator has already computed the assertion during enumeration,
/// and <see cref="SelectAsync"/> packages that pre-computed response into
/// an <see cref="AuthenticationResponse"/> without re-calling the authenticator.
/// </para>
/// <para>
/// Construction is internal — only the WebAuthn client can create instances.
/// </para>
/// </remarks>
public sealed class MatchedCredential
{
    /// <summary>
    /// Gets the credential identifier.
    /// </summary>
    public ReadOnlyMemory<byte> Id { get; }

    /// <summary>
    /// Gets the user information, if available.
    /// </summary>
    /// <remarks>
    /// Only present for discoverable credentials when user verification is performed.
    /// </remarks>
    public PublicKeyCredentialUserEntity? User { get; }

    /// <summary>
    /// Gets whether this credential requires explicit user selection.
    /// </summary>
    /// <remarks>
    /// True when multiple credentials matched the authentication request.
    /// </remarks>
    public bool RequiresSelection { get; }

    private readonly Lazy<Task<AuthenticationResponse>> _responseFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MatchedCredential"/> class.
    /// </summary>
    /// <param name="id">The credential identifier.</param>
    /// <param name="user">The user information, if available.</param>
    /// <param name="requiresSelection">Whether this credential requires explicit selection.</param>
    /// <param name="responseFactory">Factory that produces the authentication response.</param>
    internal MatchedCredential(
        ReadOnlyMemory<byte> id,
        PublicKeyCredentialUserEntity? user,
        bool requiresSelection,
        Func<CancellationToken, Task<AuthenticationResponse>> responseFactory)
    {
        Id = id;
        User = user;
        RequiresSelection = requiresSelection;

        // Lazy ensures the factory runs at most once, even if SelectAsync is called multiple times
        _responseFactory = new Lazy<Task<AuthenticationResponse>>(
            () => responseFactory(CancellationToken.None));
    }

    /// <summary>
    /// Selects this credential and returns the authentication response.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The authentication response for this credential.</returns>
    /// <remarks>
    /// <para>
    /// This method is idempotent: calling it multiple times returns the same
    /// <see cref="AuthenticationResponse"/> instance (or value-equal copies).
    /// </para>
    /// <para>
    /// The underlying authenticator assertion was already computed during credential
    /// matching; this method packages that result without additional hardware interaction.
    /// </para>
    /// <para>
    /// The cancellation token can interrupt waiting for the response factory if it is
    /// still computing when this method is called. The factory itself runs to completion;
    /// cancellation only affects the wait.
    /// </para>
    /// </remarks>
    public Task<AuthenticationResponse> SelectAsync(CancellationToken cancellationToken = default)
    {
        return _responseFactory.Value.WaitAsync(cancellationToken);
    }
}