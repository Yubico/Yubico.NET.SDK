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
using Yubico.YubiKit.Fido2.Credentials;

namespace Yubico.YubiKit.Fido2.CredentialManagement;

/// <summary>
/// Represents the credential storage metadata returned by getCredsMetadata.
/// </summary>
public sealed class CredentialMetadata
{
    /// <summary>
    /// Gets the number of existing discoverable credentials on the authenticator.
    /// </summary>
    public int ExistingResidentCredentialsCount { get; }
    
    /// <summary>
    /// Gets the maximum number of remaining discoverable credentials the authenticator can store.
    /// </summary>
    public int MaxPossibleRemainingResidentCredentialsCount { get; }
    
    private CredentialMetadata(int existingCount, int maxRemaining)
    {
        ExistingResidentCredentialsCount = existingCount;
        MaxPossibleRemainingResidentCredentialsCount = maxRemaining;
    }
    
    /// <summary>
    /// Decodes credential metadata from a CBOR response.
    /// </summary>
    /// <param name="data">The CBOR-encoded response data.</param>
    /// <returns>The decoded credential metadata.</returns>
    public static CredentialMetadata Decode(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data, CborConformanceMode.Ctap2Canonical);
        
        var existingCount = 0;
        var maxRemaining = 0;
        
        var mapLength = reader.ReadStartMap();
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();
            switch (key)
            {
                case 1: // existingResidentCredentialsCount
                    existingCount = reader.ReadInt32();
                    break;
                case 2: // maxPossibleRemainingResidentCredentialsCount
                    maxRemaining = reader.ReadInt32();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        reader.ReadEndMap();
        
        return new CredentialMetadata(existingCount, maxRemaining);
    }
}

/// <summary>
/// Represents a relying party with discoverable credentials.
/// </summary>
public sealed class RelyingPartyInfo
{
    /// <summary>
    /// Gets the relying party entity information.
    /// </summary>
    public PublicKeyCredentialRpEntity RelyingParty { get; }
    
    /// <summary>
    /// Gets the hash of the RP ID.
    /// </summary>
    public ReadOnlyMemory<byte> RpIdHash { get; }
    
    /// <summary>
    /// Gets the total number of RPs (only present in first response).
    /// </summary>
    public int? TotalRpCount { get; }
    
    private RelyingPartyInfo(PublicKeyCredentialRpEntity rp, ReadOnlyMemory<byte> rpIdHash, int? totalRpCount)
    {
        RelyingParty = rp;
        RpIdHash = rpIdHash;
        TotalRpCount = totalRpCount;
    }
    
    /// <summary>
    /// Decodes relying party info from a CBOR response.
    /// </summary>
    /// <param name="data">The CBOR-encoded response data.</param>
    /// <returns>The decoded relying party info.</returns>
    public static RelyingPartyInfo Decode(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data, CborConformanceMode.Ctap2Canonical);
        
        PublicKeyCredentialRpEntity? rp = null;
        byte[]? rpIdHash = null;
        int? totalRpCount = null;
        
        var mapLength = reader.ReadStartMap();
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();
            switch (key)
            {
                case 3: // rp
                    rp = DecodeRpEntity(reader);
                    break;
                case 4: // rpIDHash
                    rpIdHash = reader.ReadByteString();
                    break;
                case 5: // totalRPs
                    totalRpCount = reader.ReadInt32();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        reader.ReadEndMap();
        
        if (rp is null || rpIdHash is null)
        {
            throw new InvalidOperationException("Invalid RP info response: missing required fields.");
        }
        
        return new RelyingPartyInfo(rp, rpIdHash, totalRpCount);
    }
    
    private static PublicKeyCredentialRpEntity DecodeRpEntity(CborReader reader)
    {
        string? id = null;
        string? name = null;
        
        var mapLen = reader.ReadStartMap();
        for (var i = 0; i < mapLen; i++)
        {
            var fieldKey = reader.ReadTextString();
            switch (fieldKey)
            {
                case "id":
                    id = reader.ReadTextString();
                    break;
                case "name":
                    name = reader.ReadTextString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        reader.ReadEndMap();
        
        return new PublicKeyCredentialRpEntity(id ?? string.Empty, name);
    }
}

/// <summary>
/// Represents a stored discoverable credential.
/// </summary>
public sealed class StoredCredentialInfo
{
    /// <summary>
    /// Gets the user entity associated with the credential.
    /// </summary>
    public PublicKeyCredentialUserEntity User { get; }
    
    /// <summary>
    /// Gets the credential ID (public key credential descriptor).
    /// </summary>
    public PublicKeyCredentialDescriptor CredentialId { get; }
    
    /// <summary>
    /// Gets the COSE public key.
    /// </summary>
    public ReadOnlyMemory<byte> PublicKey { get; }
    
    /// <summary>
    /// Gets the total number of credentials for this RP (only present in first response).
    /// </summary>
    public int? TotalCredentials { get; }
    
    /// <summary>
    /// Gets the credential protection policy.
    /// </summary>
    public int? CredProtectPolicy { get; }
    
    /// <summary>
    /// Gets the large blob key associated with this credential.
    /// </summary>
    public ReadOnlyMemory<byte>? LargeBlobKey { get; }
    
    /// <summary>
    /// Gets the third party payment flag.
    /// </summary>
    public bool? ThirdPartyPayment { get; }
    
    private StoredCredentialInfo(
        PublicKeyCredentialUserEntity user,
        PublicKeyCredentialDescriptor credentialId,
        ReadOnlyMemory<byte> publicKey,
        int? totalCredentials,
        int? credProtectPolicy,
        ReadOnlyMemory<byte>? largeBlobKey,
        bool? thirdPartyPayment)
    {
        User = user;
        CredentialId = credentialId;
        PublicKey = publicKey;
        TotalCredentials = totalCredentials;
        CredProtectPolicy = credProtectPolicy;
        LargeBlobKey = largeBlobKey;
        ThirdPartyPayment = thirdPartyPayment;
    }
    
    /// <summary>
    /// Decodes stored credential info from a CBOR response.
    /// </summary>
    /// <param name="data">The CBOR-encoded response data.</param>
    /// <returns>The decoded credential info.</returns>
    public static StoredCredentialInfo Decode(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data, CborConformanceMode.Ctap2Canonical);
        
        PublicKeyCredentialUserEntity? user = null;
        PublicKeyCredentialDescriptor? credentialId = null;
        byte[]? publicKey = null;
        int? totalCredentials = null;
        int? credProtectPolicy = null;
        byte[]? largeBlobKey = null;
        bool? thirdPartyPayment = null;
        
        var mapLength = reader.ReadStartMap();
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();
            switch (key)
            {
                case 6: // user
                    user = DecodePublicKeyCredentialUserEntity(reader);
                    break;
                case 7: // credentialID (PublicKeyCredentialDescriptor)
                    credentialId = DecodeCredentialDescriptor(reader);
                    break;
                case 8: // publicKey (COSE_Key)
                    publicKey = reader.ReadEncodedValue().ToArray();
                    break;
                case 9: // totalCredentials
                    totalCredentials = reader.ReadInt32();
                    break;
                case 10: // credProtect
                    credProtectPolicy = reader.ReadInt32();
                    break;
                case 11: // largeBlobKey
                    largeBlobKey = reader.ReadByteString();
                    break;
                case 12: // thirdPartyPayment (CTAP 2.2)
                    thirdPartyPayment = reader.ReadBoolean();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        reader.ReadEndMap();
        
        if (user is null || credentialId is null || publicKey is null)
        {
            throw new InvalidOperationException("Invalid credential info response: missing required fields.");
        }
        
        // Explicit cast for nullable ReadOnlyMemory<byte>
        ReadOnlyMemory<byte>? largeBlobKeyMemory = largeBlobKey is not null
            ? (ReadOnlyMemory<byte>?)new ReadOnlyMemory<byte>(largeBlobKey)
            : (ReadOnlyMemory<byte>?)null;
        
        return new StoredCredentialInfo(
            user,
            credentialId,
            publicKey,
            totalCredentials,
            credProtectPolicy,
            largeBlobKeyMemory,
            thirdPartyPayment);
    }
    
    private static PublicKeyCredentialUserEntity DecodePublicKeyCredentialUserEntity(CborReader reader)
    {
        byte[]? id = null;
        string? name = null;
        string? displayName = null;
        
        var mapLen = reader.ReadStartMap();
        for (var i = 0; i < mapLen; i++)
        {
            var fieldKey = reader.ReadTextString();
            switch (fieldKey)
            {
                case "id":
                    id = reader.ReadByteString();
                    break;
                case "name":
                    name = reader.ReadTextString();
                    break;
                case "displayName":
                    displayName = reader.ReadTextString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        reader.ReadEndMap();
        
        // Name and displayName might be empty in credential management responses
        return new PublicKeyCredentialUserEntity(id ?? [], name ?? string.Empty, displayName ?? string.Empty);
    }
    
    private static PublicKeyCredentialDescriptor DecodeCredentialDescriptor(CborReader reader)
    {
        string? type = null;
        byte[]? id = null;
        
        var mapLen = reader.ReadStartMap();
        for (var i = 0; i < mapLen; i++)
        {
            var fieldKey = reader.ReadTextString();
            switch (fieldKey)
            {
                case "type":
                    type = reader.ReadTextString();
                    break;
                case "id":
                    id = reader.ReadByteString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        reader.ReadEndMap();
        
        return new PublicKeyCredentialDescriptor(id ?? [], type ?? "public-key");
    }
}
