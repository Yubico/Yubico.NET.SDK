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

using System.Formats.Cbor;
using System.Security.Cryptography;
using Yubico.YubiKit.Ctap2.Cbor;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Pin;

namespace Yubico.YubiKit.Ctap2.CredentialManagement;

/// <summary>
/// Provides operations for managing discoverable credentials on a CTAP2 authenticator.
/// </summary>
/// <remarks>
/// <para>
/// Credential management requires authenticator support (credMgmt option) and a
/// PIN/UV auth token with the <see cref="PinUvAuthTokenPermissions.CredentialManagement"/>
/// permission.
/// </para>
/// <para>
/// This helper sends the standard command (0x0A), without prototype-command fallback or
/// capability checks. Check that a fresh GetInfo response advertises credMgmt=true;
/// the SDK's firmware feature threshold is not a guarantee of standard-command support.
/// </para>
/// <para>
/// The session, protocol and token memory are borrowed. Keep the session and protocol alive
/// and the token bytes unchanged through each awaited operation, then clear the caller-owned
/// token with CryptographicOperations.ZeroMemory when no longer needed, including on failure.
/// Unlike ClientPin, this helper does not dispose the supplied protocol. It neither acquires
/// nor refreshes tokens and does not locally validate their permissions, RP scope or expiry.
/// The authenticator enforces authorization. The helper clears its owned temporary request
/// and authentication buffers. Disposal does not revoke a device token.
/// </para>
/// <para>
/// Await operations without interleaving other authenticator commands during enumeration.
/// The session guards individual exchanges, not the complete begin/next sequence. Under
/// CTAP 2.1 (2022-06-21 errata), Section 6, devices may require globally adjacent stateful
/// commands with no more than 30 seconds between them; token expiry discards sequence state.
/// Cancellation, transport failure or a decode error does not roll back device operations.
/// </para>
/// </remarks>
public sealed class CredentialManagement : IDisposable
{
    private bool _disposed;

    private readonly ICtap2Session _session;
    private readonly IPinUvAuthProtocol _protocol;
    private readonly ReadOnlyMemory<byte> _pinUvAuthToken;

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialManagement"/> class.
    /// </summary>
    /// <param name="session">The borrowed CTAP2 session to use for communication.</param>
    /// <param name="protocol">The borrowed, authenticator-supported PIN/UV auth protocol.</param>
    /// <param name="pinUvAuthToken">Borrowed token bytes with credential management permission;
    /// the caller owns their lifetime and clearing.</param>
    public CredentialManagement(
        Ctap2Session session,
        IPinUvAuthProtocol protocol,
        ReadOnlyMemory<byte> pinUvAuthToken)
        : this((ICtap2Session)session, protocol, pinUvAuthToken)
    {
    }

    internal CredentialManagement(
        ICtap2Session session,
        IPinUvAuthProtocol protocol,
        ReadOnlyMemory<byte> pinUvAuthToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(protocol);
        _session = session;
        _protocol = protocol;
        _pinUvAuthToken = pinUvAuthToken;
    }

    /// <summary>
    /// Gets metadata about stored credentials.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The credential metadata including counts.</returns>
    /// <remarks>
    /// Use a credential-management token with no associated RP ID, as required by
    /// CTAP 2.1 (2022-06-21 errata), Section 6.8.2. The remaining count is an estimate,
    /// not reserved storage or a guarantee that the next credential fits.
    /// </remarks>
    public async Task<CredentialMetadata> GetCredentialsMetadataAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var request = BuildCommandRequest(CredManagementSubCommand.GetCredsMetadata);
        var response = await SendCredentialManagementCommandAsync(request, cancellationToken)
            .ConfigureAwait(false);

        return CredentialMetadata.Decode(response);
    }

    /// <summary>
    /// Enumerates all relying parties with stored discoverable credentials.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The relying parties and the total reported by the authenticator.</returns>
    /// <remarks>
    /// Use a token with no associated RP ID (CTAP 2.1, 2022-06-21 errata, Section 6.8.3).
    /// This eagerly sends begin, then total-1 next requests if the first total exceeds one.
    /// A successful begin must report a positive, bounded total. Begin NoCredentials becomes
    /// an empty result with a null total;
    /// a continuation failure throws rather than returning a partial result. Keep the
    /// entire sequence free of intervening commands, including GetInfo and token requests.
    /// </remarks>
    public async Task<RelyingPartyEnumerationResult> EnumerateRelyingPartiesAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var results = new List<RelyingPartyInfo>();

        // Begin enumeration
        var request = BuildCommandRequest(CredManagementSubCommand.EnumerateRPsBegin);
        ReadOnlyMemory<byte> response;

        try
        {
            response = await SendCredentialManagementCommandAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (CtapException ex) when (ex.Status == CtapStatus.NoCredentials)
        {
            // No credentials stored - return an empty result
            return new RelyingPartyEnumerationResult(results, null);
        }

        var firstRp = RelyingPartyInfo.Decode(response);
        int total = ValidateBeginTotal(firstRp.TotalRpCount);
        results.Add(firstRp);

        // Get remaining RPs if any
        if (total > 1)
        {
            var nextRequest = BuildCommandRequest(CredManagementSubCommand.EnumerateRPsGetNextRP);

            for (var i = 1; i < total; i++)
            {
                response = await SendCredentialManagementCommandAsync(nextRequest, cancellationToken)
                    .ConfigureAwait(false);
                results.Add(RelyingPartyInfo.Decode(response));
            }
        }

        return new RelyingPartyEnumerationResult(results, total);
    }

    /// <summary>
    /// Enumerates all credentials for a specific relying party.
    /// </summary>
    /// <param name="rpIdHash">The raw 32-byte SHA-256 hash of the original UTF-8 RP ID, preferably
    /// the unchanged RelyingPartyInfo.RpIdHash from RP enumeration. Do not hash a possibly
    /// truncated returned RP identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The credentials and the total reported by the authenticator. The caller must dispose the
    /// result after using its credential entries.
    /// </returns>
    /// <remarks>
    /// The token must be unbound or bound to this RP (CTAP 2.1, 2022-06-21 errata, Section 6.8.4).
    /// This eagerly sends begin and, when the first total exceeds one, total-1 next requests.
    /// A successful begin must report a positive, bounded total.
    /// Begin NoCredentials becomes an empty result; a continuation or decode failure
    /// disposes entries already decoded and throws, with no partial result. Do not interleave
    /// other authenticator commands before the awaited sequence completes.
    /// </remarks>
    public async Task<CredentialEnumerationResult> EnumerateCredentialsAsync(
        ReadOnlyMemory<byte> rpIdHash,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (rpIdHash.Length != SHA256.HashSizeInBytes)
            throw new ArgumentException("The RP ID hash must be exactly 32 bytes.", nameof(rpIdHash));
        var results = new List<StoredCredentialInfo>();

        // Begin enumeration
        var request = BuildEnumerateCredentialsRequest(rpIdHash);
        ReadOnlyMemory<byte> response;

        try
        {
            response = await SendCredentialManagementCommandAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (CtapException ex) when (ex.Status == CtapStatus.NoCredentials)
        {
            // No credentials for this RP
            return new CredentialEnumerationResult(results, null);
        }

        try
        {
            var firstCred = StoredCredentialInfo.Decode(response);
            results.Add(firstCred);
            int total = ValidateBeginTotal(firstCred.TotalCredentials);

            // Get remaining credentials if any
            if (total > 1)
            {
                var nextRequest = BuildCommandRequest(CredManagementSubCommand.EnumerateCredentialsGetNextCredential);

                for (var i = 1; i < total; i++)
                {
                    response = await SendCredentialManagementCommandAsync(nextRequest, cancellationToken)
                        .ConfigureAwait(false);
                    results.Add(StoredCredentialInfo.Decode(response));
                }
            }

            return new CredentialEnumerationResult(results, total);
        }
        catch
        {
            foreach (StoredCredentialInfo credential in results)
            {
                credential.Dispose();
            }

            throw;
        }
    }

    private static int ValidateBeginTotal(int? reportedTotal) =>
        reportedTotal is > 0 and <= ushort.MaxValue
            ? reportedTotal.Value
            : throw new InvalidOperationException("Invalid enumeration begin response: expected an entry count between 1 and 65535 (SDK limit).");

    /// <summary>
    /// Deletes a discoverable credential.
    /// </summary>
    /// <param name="credentialId">The credential descriptor to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// Permanently deletes the matching discoverable credential; no undo or response model
    /// is provided. The token must be unbound or scoped to the credential's RP. An unknown
    /// credential produces NoCredentials, which this method propagates. The helper does
    /// not delete associated large blobs or update the RP's account records. CTAP 2.1
    /// (2022-06-21 errata), Section 6.8.5, recommends platform blob cleanup in a Note.
    /// A failed or cancelled call is not proof that the credential remains on the device.
    /// </remarks>
    public async Task DeleteCredentialAsync(
        PublicKeyCredentialDescriptor credentialId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var request = BuildDeleteCredentialRequest(credentialId);
        await SendCredentialManagementCommandAsync(request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Updates the user information for a discoverable credential.
    /// </summary>
    /// <param name="credentialId">The credential descriptor to update.</param>
    /// <param name="user">The existing user handle (Id) and replacement Name and DisplayName.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// Requires support for the standard updateUserInformation subcommand. Keep the existing
    /// user Id: CTAP 2.1 (2022-06-21 errata), Section 6.8.6, requires the authenticator to
    /// reject a different handle. Only Name and DisplayName are replaced. Null or empty
    /// names are omitted by this encoder and request removal of those stored fields, not
    /// preservation. This does not create a user, rebind the credential or rename an RP-side
    /// account. The token must be unbound or scoped to the credential's RP; NoCredentials
    /// is propagated for an unknown credential. Re-enumerate after success to inspect device state.
    /// </remarks>
    public async Task UpdateUserInformationAsync(
        PublicKeyCredentialDescriptor credentialId,
        PublicKeyCredentialUserEntity user,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var request = BuildUpdateUserRequest(credentialId, user);
        await SendCredentialManagementCommandAsync(request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Marks this helper disposed without releasing its borrowed dependencies.
    /// </summary>
    /// <remarks>
    /// Subsequent operations throw <see cref="ObjectDisposedException"/>. Await the complete
    /// operation before disposing. Disposal does not clear token memory, dispose the
    /// session or protocol, or revoke device authorization.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        // _pinUvAuthToken is caller-provided — the caller retains zeroing responsibility.
        // Clearing borrowed memory here would invalidate other caller-held views.
        _disposed = true;
    }

    private async Task<ReadOnlyMemory<byte>> SendCredentialManagementCommandAsync(
        byte[] request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _session.SendCborRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(request);
        }
    }

    private byte[] BuildCommandRequest(byte subCommand, byte[]? parameters = null)
    {
        byte[]? tag = null;
        try
        {
            // Credential management authenticates subCommand || parameters, when present.
            // Preserve the existing authentication behavior for enumeration continuation.
            Span<byte> transcript = parameters is null ? stackalloc byte[1] : new byte[1 + parameters.Length];
            try
            {
                transcript[0] = subCommand;
                if (parameters is not null) parameters.AsSpan().CopyTo(transcript[1..]);
                tag = _protocol.Authenticate(_pinUvAuthToken.Span, transcript);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(transcript);
            }

            using var request = CtapRequestBuilder.Create(CtapCommand.CredentialManagement)
                .WithInt(1, subCommand)
                .WithInt(3, _protocol.Version)
                .WithBytes(4, tag.AsSpan());
            if (parameters is not null)
                request.WithEncodedValue(2, parameters);
            return request.Build();
        }
        finally
        {
            if (parameters is not null) CryptographicOperations.ZeroMemory(parameters);
            if (tag is not null) CryptographicOperations.ZeroMemory(tag);
        }
    }

    private byte[] BuildEnumerateCredentialsRequest(ReadOnlyMemory<byte> rpIdHash) =>
        BuildCommandRequest(CredManagementSubCommand.EnumerateCredentialsBegin, BuildRpIdHashParam(rpIdHash));

    private byte[] BuildDeleteCredentialRequest(PublicKeyCredentialDescriptor credentialId) =>
        BuildCommandRequest(CredManagementSubCommand.DeleteCredential, BuildCredentialIdParam(credentialId));

    private byte[] BuildUpdateUserRequest(PublicKeyCredentialDescriptor credentialId, PublicKeyCredentialUserEntity user) =>
        BuildCommandRequest(CredManagementSubCommand.UpdateUserInformation, BuildUpdateUserParams(credentialId, user));

    private static byte[] BuildRpIdHashParam(ReadOnlyMemory<byte> rpIdHash)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteInt32(1); // rpIDHash
        writer.WriteByteString(rpIdHash.Span);
        writer.WriteEndMap();
        return writer.Encode();
    }

    private static byte[] BuildCredentialIdParam(PublicKeyCredentialDescriptor credentialId)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteInt32(2); // credentialId
        WriteCredentialDescriptor(writer, credentialId);
        writer.WriteEndMap();
        return writer.Encode();
    }

    private static byte[] BuildUpdateUserParams(
        PublicKeyCredentialDescriptor credentialId,
        PublicKeyCredentialUserEntity user)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);

        // credentialId (key 2)
        writer.WriteInt32(2);
        WriteCredentialDescriptor(writer, credentialId);

        // user (key 3)
        writer.WriteInt32(3);
        WritePublicKeyCredentialUserEntity(writer, user);

        writer.WriteEndMap();
        return writer.Encode();
    }

    private static void WriteCredentialDescriptor(CborWriter writer, PublicKeyCredentialDescriptor descriptor)
    {
        writer.WriteStartMap(2);

        // Canonical text-key order puts the shorter "id" before "type".
        writer.WriteTextString("id");
        writer.WriteByteString(descriptor.Id.Span);

        writer.WriteTextString("type");
        writer.WriteTextString(descriptor.Type);

        writer.WriteEndMap();
    }

    private static void WritePublicKeyCredentialUserEntity(CborWriter writer, PublicKeyCredentialUserEntity user)
    {
        // Count non-null fields
        var fieldCount = 1; // id is always present
        if (!string.IsNullOrEmpty(user.DisplayName)) fieldCount++;
        if (!string.IsNullOrEmpty(user.Name)) fieldCount++;

        writer.WriteStartMap(fieldCount);

        // Ctap2Canonical sorts the completed map by encoded keys, not alphabetical insertion.
        // With all fields present, the emitted order is id, name, displayName (CTAP 2.1 Section 8).
        if (!string.IsNullOrEmpty(user.DisplayName))
        {
            writer.WriteTextString("displayName");
            writer.WriteTextString(user.DisplayName);
        }

        writer.WriteTextString("id");
        writer.WriteByteString(user.Id.Span);

        if (!string.IsNullOrEmpty(user.Name))
        {
            writer.WriteTextString("name");
            writer.WriteTextString(user.Name);
        }

        writer.WriteEndMap();
    }
}