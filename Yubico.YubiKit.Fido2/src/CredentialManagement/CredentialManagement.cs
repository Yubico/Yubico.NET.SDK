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
using Yubico.YubiKit.Fido2.Cbor;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.CredentialManagement;

/// <summary>
/// Provides operations for managing discoverable credentials on a FIDO2 authenticator.
/// </summary>
/// <remarks>
/// <para>
/// Credential management requires authenticator support (credMgmt option) and a
/// PIN/UV auth token with the <see cref="PinUvAuthTokenPermissions.CredentialManagement"/>
/// permission.
/// </para>
/// <para>
/// Requires YubiKey firmware 5.2 or later.
/// </para>
/// </remarks>
public sealed class CredentialManagement
{
    private readonly FidoSession _session;
    private readonly IPinUvAuthProtocol _protocol;
    private readonly ReadOnlyMemory<byte> _pinUvAuthToken;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialManagement"/> class.
    /// </summary>
    /// <param name="session">The FIDO session to use for communication.</param>
    /// <param name="protocol">The PIN/UV auth protocol to use.</param>
    /// <param name="pinUvAuthToken">The PIN/UV auth token with credential management permission.</param>
    public CredentialManagement(
        FidoSession session,
        IPinUvAuthProtocol protocol,
        ReadOnlyMemory<byte> pinUvAuthToken)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
        _pinUvAuthToken = pinUvAuthToken;
    }
    
    /// <summary>
    /// Gets metadata about stored credentials.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The credential metadata including counts.</returns>
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
    /// <returns>A list of all RPs with credentials.</returns>
    public async Task<IReadOnlyList<RelyingPartyInfo>> EnumerateRelyingPartiesAsync(
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
            // No credentials stored - return empty list
            return results;
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
        
        return results;
    }
    
    /// <summary>
    /// Enumerates all credentials for a specific relying party.
    /// </summary>
    /// <param name="rpIdHash">The SHA-256 hash of the RP ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of all credentials for the RP.</returns>
    public async Task<IReadOnlyList<StoredCredentialInfo>> EnumerateCredentialsAsync(
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
            return results;
        }
        
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
        
        return results;
    }
    
    /// <summary>
    /// Deletes a discoverable credential.
    /// </summary>
    /// <param name="credentialId">The credential descriptor to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
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
    /// <param name="user">The new user entity information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// Requires CTAP 2.1 support.
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
    
    private async Task<ReadOnlyMemory<byte>> SendCredentialManagementCommandAsync(
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        // Use standard CTAP 2.1 credential management command
        // Caller should verify authenticator supports credMgmt option before creating this instance
        return await _session.SendCborAsync(CtapCommand.CredentialManagement, payload, cancellationToken)
            .ConfigureAwait(false);
    }
    
    private ReadOnlyMemory<byte> BuildCommandPayload(byte subCommand)
    {
        // Build PIN/UV auth param over just the subcommand
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
        
        // "id" comes before "type" alphabetically
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
        
        // Fields in CTAP2 canonical order (alphabetical for text keys)
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
