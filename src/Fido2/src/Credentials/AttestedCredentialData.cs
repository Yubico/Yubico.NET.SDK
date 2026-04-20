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

using System.Buffers.Binary;
using System.Formats.Cbor;

namespace Yubico.YubiKit.Fido2.Credentials;

/// <summary>
/// Represents attested credential data included in authenticator data during credential creation.
/// </summary>
/// <remarks>
/// <para>
/// Attested credential data contains:
/// - 16 bytes: AAGUID (Authenticator Attestation GUID)
/// - 2 bytes: Credential ID length (big-endian)
/// - L bytes: Credential ID
/// - Variable: COSE public key (CBOR-encoded)
/// </para>
/// <para>
/// See: https://www.w3.org/TR/webauthn-2/#sctn-attested-credential-data
/// </para>
/// </remarks>
public sealed class AttestedCredentialData
{
    /// <summary>
    /// Minimum size of attested credential data (AAGUID + length field).
    /// </summary>
    private const int MinimumLength = 18;
    
    /// <summary>
    /// Gets the Authenticator Attestation GUID identifying the authenticator model.
    /// </summary>
    public Guid Aaguid { get; }
    
    /// <summary>
    /// Gets the credential ID.
    /// </summary>
    public ReadOnlyMemory<byte> CredentialId { get; }
    
    /// <summary>
    /// Gets the COSE-encoded public key.
    /// </summary>
    public ReadOnlyMemory<byte> CredentialPublicKey { get; }
    
    private AttestedCredentialData(
        Guid aaguid,
        ReadOnlyMemory<byte> credentialId,
        ReadOnlyMemory<byte> credentialPublicKey)
    {
        Aaguid = aaguid;
        CredentialId = credentialId;
        CredentialPublicKey = credentialPublicKey;
    }
    
    /// <summary>
    /// Parses attested credential data from raw bytes.
    /// </summary>
    /// <param name="data">The raw data starting at attested credential data.</param>
    /// <param name="bytesRead">The number of bytes consumed from the input.</param>
    /// <returns>The parsed AttestedCredentialData.</returns>
    /// <exception cref="ArgumentException">If the data is malformed.</exception>
    public static AttestedCredentialData Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        if (data.Length < MinimumLength)
        {
            throw new ArgumentException(
                $"Attested credential data must be at least {MinimumLength} bytes, got {data.Length}.",
                nameof(data));
        }
        
        int offset = 0;
        
        // Parse AAGUID (16 bytes, big-endian UUID format)
        var aaguidBytes = data.Slice(offset, 16);
        var aaguid = ParseAaguid(aaguidBytes);
        offset += 16;
        
        // Parse credential ID length (2 bytes, big-endian)
        var credentialIdLength = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;
        
        if (data.Length < offset + credentialIdLength)
        {
            throw new ArgumentException(
                $"Attested credential data too short for credential ID length {credentialIdLength}.",
                nameof(data));
        }
        
        // Parse credential ID
        var credentialId = data.Slice(offset, credentialIdLength).ToArray();
        offset += credentialIdLength;
        
        // Parse COSE public key (CBOR-encoded, need to determine length)
        var coseKeyBytes = data[offset..];
        var coseKeyLength = GetCborLength(coseKeyBytes);
        var credentialPublicKey = coseKeyBytes[..coseKeyLength].ToArray();
        offset += coseKeyLength;
        
        bytesRead = offset;
        
        return new AttestedCredentialData(aaguid, credentialId, credentialPublicKey);
    }
    
    private static Guid ParseAaguid(ReadOnlySpan<byte> bytes)
    {
        // AAGUID is stored in big-endian (network byte order) format
        // .NET Guid constructor expects specific byte ordering
        Span<byte> guidBytes = stackalloc byte[16];
        bytes.CopyTo(guidBytes);
        
        // Convert from big-endian to little-endian for first 3 components
        if (BitConverter.IsLittleEndian)
        {
            // Reverse bytes for Data1 (4 bytes)
            (guidBytes[0], guidBytes[1], guidBytes[2], guidBytes[3]) = 
                (guidBytes[3], guidBytes[2], guidBytes[1], guidBytes[0]);
            
            // Reverse bytes for Data2 (2 bytes)
            (guidBytes[4], guidBytes[5]) = (guidBytes[5], guidBytes[4]);
            
            // Reverse bytes for Data3 (2 bytes)
            (guidBytes[6], guidBytes[7]) = (guidBytes[7], guidBytes[6]);
        }
        
        return new Guid(guidBytes);
    }
    
    private static int GetCborLength(ReadOnlySpan<byte> data)
    {
        // Use CborReader to determine the length of the CBOR value
        var reader = new CborReader(data.ToArray(), CborConformanceMode.Lax);
        reader.SkipValue();
        return data.Length - reader.BytesRemaining;
    }
}
