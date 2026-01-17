// Copyright 2025 Yubico AB
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

using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.Fido2;

/// <summary>
/// Interface for FIDO2/CTAP2 session operations on a YubiKey authenticator.
/// </summary>
/// <remarks>
/// <para>
/// Implements CTAP 2.1/2.3 specification.
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html
/// </para>
/// </remarks>
public interface IFidoSession : IApplicationSession
{
    /// <summary>
    /// Gets authenticator information. Always fetches fresh data from device.
    /// </summary>
    /// <remarks>
    /// Callers should cache the result if needed. The session does not cache InfoData
    /// to ensure fresh data is always returned.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticator information.</returns>
    Task<AuthenticatorInfo> GetInfoAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Requests the user to select this authenticator by touching the device.
    /// </summary>
    /// <remarks>
    /// This command is useful when multiple authenticators are present and the user
    /// needs to indicate which one they want to use.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SelectionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resets the FIDO application to factory defaults.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This operation requires user presence (touch) within 5 seconds of device insertion.
    /// All credentials and settings will be permanently deleted.
    /// </para>
    /// <para>
    /// WARNING: This is a destructive operation that cannot be undone.
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="CtapException">
    /// Thrown with <see cref="CtapStatus.NotAllowed"/> if reset is attempted too long after
    /// device insertion.
    /// </exception>
    Task ResetAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new credential on the authenticator.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method implements the CTAP2 authenticatorMakeCredential command.
    /// User presence is required (user must touch the authenticator).
    /// </para>
    /// <para>
    /// For discoverable credentials, set <see cref="MakeCredentialOptions.ResidentKey"/> to true.
    /// </para>
    /// </remarks>
    /// <param name="clientDataHash">SHA-256 hash of the client data (32 bytes).</param>
    /// <param name="rp">Relying party entity.</param>
    /// <param name="user">User entity.</param>
    /// <param name="pubKeyCredParams">Supported credential parameters in preference order.</param>
    /// <param name="options">Optional parameters including exclude list, extensions, etc.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created credential response with attestation.</returns>
    /// <exception cref="CtapException">
    /// Thrown with <see cref="CtapStatus.CredentialExcluded"/> if a credential in the exclude list exists.
    /// </exception>
    Task<MakeCredentialResponse> MakeCredentialAsync(
        ReadOnlyMemory<byte> clientDataHash,
        PublicKeyCredentialRpEntity rp,
        PublicKeyCredentialUserEntity user,
        IReadOnlyList<PublicKeyCredentialParameters> pubKeyCredParams,
        MakeCredentialOptions? options = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an assertion for authentication.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method implements the CTAP2 authenticatorGetAssertion command.
    /// User presence is required (user must touch the authenticator).
    /// </para>
    /// <para>
    /// If multiple credentials match, use <see cref="GetNextAssertionAsync"/> to retrieve
    /// additional assertions.
    /// </para>
    /// </remarks>
    /// <param name="rpId">Relying party identifier.</param>
    /// <param name="clientDataHash">SHA-256 hash of the client data (32 bytes).</param>
    /// <param name="options">Optional parameters including allow list, extensions, etc.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The first assertion response.</returns>
    /// <exception cref="CtapException">
    /// Thrown with <see cref="CtapStatus.NoCredentials"/> if no matching credential is found.
    /// </exception>
    Task<GetAssertionResponse> GetAssertionAsync(
        string rpId,
        ReadOnlyMemory<byte> clientDataHash,
        GetAssertionOptions? options = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the next assertion when multiple credentials match.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this method when <see cref="GetAssertionResponse.NumberOfCredentials"/> is greater than 1.
    /// This must be called immediately after <see cref="GetAssertionAsync"/> or a previous
    /// <see cref="GetNextAssertionAsync"/> call.
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next assertion response.</returns>
    Task<GetAssertionResponse> GetNextAssertionAsync(CancellationToken cancellationToken = default);
}
