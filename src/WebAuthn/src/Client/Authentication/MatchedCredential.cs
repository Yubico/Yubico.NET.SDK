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

using Yubico.YubiKit.Ctap2.Credentials;

namespace Yubico.YubiKit.WebAuthn.Client.Authentication;

/// <summary>
/// Represents a credential that matched an authentication request.
/// </summary>
/// <remarks>
/// <para>
/// The WebAuthn client builds the assertion response and processes extension outputs before returning
/// this match. <see cref="SelectAsync"/> retrieves that response without further authenticator work.
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
    /// <remarks>
    /// Currently empty if the authenticator omits the final credential descriptor. The client does not
    /// recover the selected request ID in that case. Only an original single-entry allow list identifies
    /// an unambiguous requested credential; do not guess from the first of multiple allowed credentials.
    /// </remarks>
    public ReadOnlyMemory<byte> Id { get; }

    /// <summary>
    /// Gets the user information, if available.
    /// </summary>
    /// <remarks>
    /// Returned when the authenticator supplies a user entity. The account handle can be present without
    /// user verification; identifying display fields may be absent. Display names are not authorization data.
    /// </remarks>
    public PublicKeyCredentialUserEntity? User { get; }

    private readonly Lazy<Task<AuthenticationResponse>> _responseFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MatchedCredential"/> class.
    /// </summary>
    /// <param name="id">The credential identifier.</param>
    /// <param name="user">The user information, if available.</param>
    /// <param name="responseFactory">Factory that produces the authentication response.</param>
    internal MatchedCredential(
        ReadOnlyMemory<byte> id,
        PublicKeyCredentialUserEntity? user,
        Func<CancellationToken, Task<AuthenticationResponse>> responseFactory)
    {
        Id = id;
        User = user;
        // Lazy ensures the factory runs at most once, even if SelectAsync is called multiple times
        _responseFactory = new Lazy<Task<AuthenticationResponse>>(
            () => responseFactory(CancellationToken.None));
    }

    /// <summary>
    /// Selects this credential and returns the authentication response.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for waiting for the response, not for the completed ceremony.</param>
    /// <returns>The authentication response for this credential.</returns>
    /// <remarks>
    /// <para>
    /// The response factory runs at most once. Repeated calls retrieve the same
    /// <see cref="AuthenticationResponse"/> instance; a factory failure is also cached.
    /// </para>
    /// <para>
    /// For matches returned by the client, the response is already built, including extension results.
    /// Selection does not authenticate again, verify the response for the RP, or defer extension writes.
    /// </para>
    /// <para>
    /// The cancellation token can interrupt waiting for the response factory if it is
    /// still computing when this method is called. The factory itself runs to completion;
    /// cancellation only affects the wait and cannot undo completed authenticator or extension work.
    /// </para>
    /// </remarks>
    public Task<AuthenticationResponse> SelectAsync(CancellationToken cancellationToken = default)
    {
        return _responseFactory.Value.WaitAsync(cancellationToken);
    }
}