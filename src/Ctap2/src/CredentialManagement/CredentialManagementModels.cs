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
using Yubico.YubiKit.Ctap2.Credentials;

namespace Yubico.YubiKit.Ctap2.CredentialManagement;

/// <summary>
/// Represents the credential storage metadata returned by getCredsMetadata.
/// </summary>
public sealed class CredentialMetadata
{
    /// <summary>
    /// Gets the complete original CBOR-encoded authenticator response.
    /// </summary>
    public ReadOnlyMemory<byte> RawData { get; }

    /// <summary>
    /// Gets the number of existing discoverable credentials on the authenticator.
    /// </summary>
    public int ExistingResidentCredentialsCount { get; }

    /// <summary>
    /// Gets the authenticator's estimate of how many more discoverable credentials could be stored.
    /// </summary>
    /// <remarks>
    /// Actual storage depends on the algorithm and user information (CTAP 2.1,
    /// 2022-06-21 errata, Section 6.8.2). This is not a capacity reservation.
    /// </remarks>
    public int MaxPossibleRemainingResidentCredentialsCount { get; }

    private CredentialMetadata(ReadOnlyMemory<byte> rawData, int existingCount, int maxRemaining)
    {
        RawData = rawData;
        ExistingResidentCredentialsCount = existingCount;
        MaxPossibleRemainingResidentCredentialsCount = maxRemaining;
    }

    /// <summary>
    /// Decodes credential metadata from a CBOR response.
    /// </summary>
    /// <param name="data">The CBOR-encoded response data.</param>
    /// <returns>The decoded credential metadata.</returns>
    /// <remarks>
    /// Copies the complete response without modifying the borrowed input. Missing count
    /// fields currently default to zero; required-field and nonnegative-count validation
    /// is not performed, so defaults are not evidence of empty or full device storage.
    /// </remarks>
    public static CredentialMetadata Decode(ReadOnlyMemory<byte> data)
    {
        ReadOnlyMemory<byte> rawData = data.ToArray();
        var reader = new CborReader(rawData, CborConformanceMode.Ctap2Canonical);

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

        return new CredentialMetadata(rawData, existingCount, maxRemaining);
    }
}

/// <summary>
/// Represents a relying party with discoverable credentials.
/// </summary>
public sealed class RelyingPartyInfo
{
    /// <summary>
    /// Gets the complete original CBOR-encoded authenticator response.
    /// </summary>
    public ReadOnlyMemory<byte> RawData { get; }

    /// <summary>
    /// Gets the relying party entity information.
    /// </summary>
    /// <remarks>
    /// The returned Id can be truncated for display (CTAP 2.1, 2022-06-21 errata,
    /// Section 6.8.7). Use RpIdHash, not a new hash of this text, to enumerate credentials.
    /// </remarks>
    public PublicKeyCredentialRpEntity RelyingParty { get; }

    /// <summary>
    /// Gets the raw SHA-256 hash of the original, untruncated RP ID.
    /// </summary>
    /// <remarks>Pass it unchanged to credential enumeration. The decoder does not enforce its 32-byte length.</remarks>
    public ReadOnlyMemory<byte> RpIdHash { get; }

    /// <summary>
    /// Gets the total copied from this response, or null when omitted; the standard supplies it in the begin response.
    /// </summary>
    public int? TotalRpCount { get; }

    private RelyingPartyInfo(
        ReadOnlyMemory<byte> rawData,
        PublicKeyCredentialRpEntity rp,
        ReadOnlyMemory<byte> rpIdHash,
        int? totalRpCount)
    {
        RawData = rawData;
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
        ReadOnlyMemory<byte> rawData = data.ToArray();
        var reader = new CborReader(rawData, CborConformanceMode.Ctap2Canonical);

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

        return new RelyingPartyInfo(rawData, rp, rpIdHash, totalRpCount);
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