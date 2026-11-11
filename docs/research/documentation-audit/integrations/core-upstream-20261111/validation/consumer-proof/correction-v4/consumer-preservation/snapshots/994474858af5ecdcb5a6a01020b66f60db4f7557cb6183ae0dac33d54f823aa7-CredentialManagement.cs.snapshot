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
/// The authenticator enforces authorization. Disposal does not revoke a device token.
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
        var payload = BuildCommandPayload(CredManagementSubCommand.GetCredsMetadata);
        var response = await SendCredentialManagementCommandAsync(payload, cancellationToken)
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
    /// If the first total is missing, only the first entry is returned; this fallback does
    /// not establish a complete standard response. Totals are copied, not validated against
    /// the collection. Begin NoCredentials becomes an empty result with a null total;
    /// a continuation failure throws rather than returning a partial result. Keep the
    /// entire sequence free of intervening commands, including GetInfo and token requests.
    /// </remarks>
    public async Task<RelyingPartyEnumerationResult> EnumerateRelyingPartiesAsync(
        CancellationToken cancellationToken = default)
    {
        var results = new List<RelyingPartyInfo>();

        // Begin enumeration
        var payload = BuildCommandPayload(CredManagementSubCommand.EnumerateRPsBegin);
        ReadOnlyMemory<byte> response;

        try
        {
            response = await SendCredentialManagementCommandAsync(payload, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (CtapException ex) when (ex.Status == CtapStatus.NoCredentials)
        {
            // No credentials stored - return an empty result
            return new RelyingPartyEnumerationResult(results, null);
        }

        var firstRp = RelyingPartyInfo.Decode(response);
        results.Add(firstRp);

        // Get remaining RPs if any
        if (firstRp.TotalRpCount.HasValue && firstRp.TotalRpCount.Value > 1)
        {
            var nextPayload = BuildCommandPayload(CredManagementSubCommand.EnumerateRPsGetNextRP);

            for (var i = 1; i < firstRp.TotalRpCount.Value; i++)
            {
                response = await SendCredentialManagementCommandAsync(nextPayload, cancellationToken)
                    .ConfigureAwait(false);
                results.Add(RelyingPartyInfo.Decode(response));
            }
        }

        return new RelyingPartyEnumerationResult(results, firstRp.TotalRpCount);
    }

    /// <summary>
    /// Enumerates all credentials for a specific relying party.
    /// </summary>
    /// <param name="rpIdHash">The raw 32-byte SHA-256 hash of the original UTF-8 RP ID, preferably
    /// the unchanged RelyingPartyInfo.RpIdHash from RP enumeration. Do not hash a possibly
    /// truncated returned RP identifier. This helper does not validate the hash length.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The credentials and the total reported by the authenticator. The caller must dispose the
    /// result after using its credential entries.
    /// </returns>
    /// <remarks>
    /// The token must be unbound or bound to this RP (CTAP 2.1, 2022-06-21 errata, Section 6.8.4).
    /// This eagerly sends begin and, when the first total exceeds one, total-1 next requests.
    /// A missing total yields one entry and a null ReportedTotal, not proof of complete
    /// enumeration. The reported total is copied without count or range validation.
    /// Begin NoCredentials becomes an empty result; a continuation or decode failure
    /// disposes entries already decoded and throws, with no partial result. Do not interleave
    /// other authenticator commands before the awaited sequence completes.
    /// </remarks>
    public async Task<CredentialEnumerationResult> EnumerateCredentialsAsync(
        ReadOnlyMemory<byte> rpIdHash,
        CancellationToken cancellationToken = default)
    {
        var results = new List<StoredCredentialInfo>();

        // Begin enumeration
        var payload = BuildEnumerateCredentialsPayload(rpIdHash);
        ReadOnlyMemory<byte> response;

        try
        {
            response = await SendCredentialManagementCommandAsync(payload, cancellationToken)
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

            // Get remaining credentials if any
            if (firstCred.TotalCredentials.HasValue && firstCred.TotalCredentials.Value > 1)
            {
                var nextPayload = BuildCommandPayload(CredManagementSubCommand.EnumerateCredentialsGetNextCredential);

                for (var i = 1; i < firstCred.TotalCredentials.Value; i++)
                {
                    response = await SendCredentialManagementCommandAsync(nextPayload, cancellationToken)
                        .ConfigureAwait(false);
                    results.Add(StoredCredentialInfo.Decode(response));
                }
            }

            return new CredentialEnumerationResult(results, firstCred.TotalCredentials);
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
        var payload = BuildDeleteCredentialPayload(credentialId);
        await SendCredentialManagementCommandAsync(payload, cancellationToken)
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
        var payload = BuildUpdateUserPayload(credentialId, user);
        await SendCredentialManagementCommandAsync(payload, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Marks this helper disposed without releasing its borrowed dependencies.
    /// </summary>
    /// <remarks>
    /// Current operation methods do not check this marker and can still send requests.
    /// Do not use the helper after disposal or rely on disposal to prevent operations,
    /// clear token memory, dispose the session/protocol or revoke device authorization.
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
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        // Use standard CTAP 2.1 credential management command.
        // Caller should verify authenticator supports credMgmt option before creating this instance.
        var request = new byte[1 + payload.Length];
        request[0] = CtapCommand.CredentialManagement;
        payload.CopyTo(request.AsMemory(1));

        try
        {
            return await _session.SendCborRequestAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            // Clears this final request only, not payload/auth-param arrays or session response storage.
            CryptographicOperations.ZeroMemory(request);
        }
    }

    private ReadOnlyMemory<byte> BuildCommandPayload(byte subCommand)
    {
        // This encoder authenticates next requests too; Section 6.8 lists only subCommand for continuation.
        // Build PIN/UV auth param over just the subcommand.
        var subCommandBytes = new byte[] { subCommand };
        var pinUvAuthParam = _protocol.Authenticate(_pinUvAuthToken.Span, subCommandBytes);

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);

        // 0x01: subCommand
        writer.WriteInt32(1);
        writer.WriteInt32(subCommand);

        // 0x03: pinUvAuthProtocol
        writer.WriteInt32(3);
        writer.WriteInt32(_protocol.Version);

        // 0x04: pinUvAuthParam
        writer.WriteInt32(4);
        writer.WriteByteString(pinUvAuthParam.AsSpan());

        writer.WriteEndMap();

        return writer.Encode();
    }

    private ReadOnlyMemory<byte> BuildEnumerateCredentialsPayload(ReadOnlyMemory<byte> rpIdHash)
    {
        const byte subCommand = CredManagementSubCommand.EnumerateCredentialsBegin;

        // Build message to authenticate: subCommand || subCommandParams
        var subCommandParams = BuildRpIdHashParam(rpIdHash);
        var messageToAuth = new byte[1 + subCommandParams.Length];
        messageToAuth[0] = subCommand;
        subCommandParams.Span.CopyTo(messageToAuth.AsSpan(1));

        var pinUvAuthParam = _protocol.Authenticate(_pinUvAuthToken.Span, messageToAuth);

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(4);

        // 0x01: subCommand
        writer.WriteInt32(1);
        writer.WriteInt32(subCommand);

        // 0x02: subCommandParams (rpIDHash)
        writer.WriteInt32(2);
        writer.WriteStartMap(1);
        writer.WriteInt32(1); // rpIDHash key
        writer.WriteByteString(rpIdHash.Span);
        writer.WriteEndMap();

        // 0x03: pinUvAuthProtocol
        writer.WriteInt32(3);
        writer.WriteInt32(_protocol.Version);

        // 0x04: pinUvAuthParam
        writer.WriteInt32(4);
        writer.WriteByteString(pinUvAuthParam.AsSpan());

        writer.WriteEndMap();

        return writer.Encode();
    }

    private ReadOnlyMemory<byte> BuildDeleteCredentialPayload(PublicKeyCredentialDescriptor credentialId)
    {
        const byte subCommand = CredManagementSubCommand.DeleteCredential;

        // Build message to authenticate: subCommand || subCommandParams
        var subCommandParams = BuildCredentialIdParam(credentialId);
        var messageToAuth = new byte[1 + subCommandParams.Length];
        messageToAuth[0] = subCommand;
        subCommandParams.Span.CopyTo(messageToAuth.AsSpan(1));

        var pinUvAuthParam = _protocol.Authenticate(_pinUvAuthToken.Span, messageToAuth);

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(4);

        // 0x01: subCommand
        writer.WriteInt32(1);
        writer.WriteInt32(subCommand);

        // 0x02: subCommandParams (credentialId)
        writer.WriteInt32(2);
        writer.WriteStartMap(1);
        writer.WriteInt32(2); // credentialId key
        WriteCredentialDescriptor(writer, credentialId);
        writer.WriteEndMap();

        // 0x03: pinUvAuthProtocol
        writer.WriteInt32(3);
        writer.WriteInt32(_protocol.Version);

        // 0x04: pinUvAuthParam
        writer.WriteInt32(4);
        writer.WriteByteString(pinUvAuthParam.AsSpan());

        writer.WriteEndMap();

        return writer.Encode();
    }

    private ReadOnlyMemory<byte> BuildUpdateUserPayload(
        PublicKeyCredentialDescriptor credentialId,
        PublicKeyCredentialUserEntity user)
    {
        const byte subCommand = CredManagementSubCommand.UpdateUserInformation;

        // Build message to authenticate: subCommand || subCommandParams
        var subCommandParams = BuildUpdateUserParams(credentialId, user);
        var messageToAuth = new byte[1 + subCommandParams.Length];
        messageToAuth[0] = subCommand;
        subCommandParams.Span.CopyTo(messageToAuth.AsSpan(1));

        var pinUvAuthParam = _protocol.Authenticate(_pinUvAuthToken.Span, messageToAuth);

        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(4);

        // 0x01: subCommand
        writer.WriteInt32(1);
        writer.WriteInt32(subCommand);

        // 0x02: subCommandParams
        writer.WriteInt32(2);
        writer.WriteStartMap(2);

        // credentialId (key 2)
        writer.WriteInt32(2);
        WriteCredentialDescriptor(writer, credentialId);

        // user (key 3)
        writer.WriteInt32(3);
        WritePublicKeyCredentialUserEntity(writer, user);

        writer.WriteEndMap();

        // 0x03: pinUvAuthProtocol
        writer.WriteInt32(3);
        writer.WriteInt32(_protocol.Version);

        // 0x04: pinUvAuthParam
        writer.WriteInt32(4);
        writer.WriteByteString(pinUvAuthParam.AsSpan());

        writer.WriteEndMap();

        return writer.Encode();
    }

    private static ReadOnlyMemory<byte> BuildRpIdHashParam(ReadOnlyMemory<byte> rpIdHash)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteInt32(1); // rpIDHash
        writer.WriteByteString(rpIdHash.Span);
        writer.WriteEndMap();
        return writer.Encode();
    }

    private static ReadOnlyMemory<byte> BuildCredentialIdParam(PublicKeyCredentialDescriptor credentialId)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteInt32(2); // credentialId
        WriteCredentialDescriptor(writer, credentialId);
        writer.WriteEndMap();
        return writer.Encode();
    }

    private static ReadOnlyMemory<byte> BuildUpdateUserParams(
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