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

namespace Yubico.YubiKit.Fido2.Credentials;

/// <summary>
/// Represents the response from authenticatorMakeCredential.
/// </summary>
/// <remarks>
/// <para>
/// Contains the attestation object with the newly created credential's
/// public key and attestation statement.
/// </para>
/// <para>
/// CTAP2 makeCredential response structure:
/// - 0x01: fmt (text string) - attestation statement format
/// - 0x02: authData (byte string) - authenticator data
/// - 0x03: attStmt (map) - attestation statement
/// - 0x04: epAtt (bool, optional) - enterprise attestation used
/// - 0x05: largeBlobKey (byte string, optional) - large blob key
/// </para>
/// </remarks>
public sealed class MakeCredentialResponse
{
    /// <summary>
    /// Gets the attestation statement format identifier.
    /// </summary>
    /// <remarks>
    /// Common formats: "packed", "tpm", "android-key", "android-safetynet", 
    /// "fido-u2f", "apple", "none"
    /// </remarks>
    public string Format { get; }
    
    /// <summary>
    /// Gets the authenticator data containing the new credential.
    /// </summary>
    public AuthenticatorData AuthenticatorData { get; }
    
    /// <summary>
    /// Gets the raw authenticator data bytes.
    /// </summary>
    public ReadOnlyMemory<byte> AuthenticatorDataRaw { get; }
    
    /// <summary>
    /// Gets the attestation statement.
    /// </summary>
    public AttestationStatement AttestationStatement { get; }
    
    /// <summary>
    /// Gets whether enterprise attestation was used.
    /// </summary>
    public bool? EnterpriseAttestation { get; }
    
    /// <summary>
    /// Gets the large blob key if the largeBlobKey extension was requested.
    /// </summary>
    public ReadOnlyMemory<byte>? LargeBlobKey { get; }
    
    /// <summary>
    /// Gets the CBOR-encoded extension outputs, if any.
    /// </summary>
    public ReadOnlyMemory<byte>? ExtensionOutputs { get; }
    
    private MakeCredentialResponse(
        string format,
        AuthenticatorData authenticatorData,
        ReadOnlyMemory<byte> authenticatorDataRaw,
        AttestationStatement attestationStatement,
        bool? enterpriseAttestation,
        ReadOnlyMemory<byte>? largeBlobKey,
        ReadOnlyMemory<byte>? extensionOutputs)
    {
        Format = format;
        AuthenticatorData = authenticatorData;
        AuthenticatorDataRaw = authenticatorDataRaw;
        AttestationStatement = attestationStatement;
        EnterpriseAttestation = enterpriseAttestation;
        LargeBlobKey = largeBlobKey;
        ExtensionOutputs = extensionOutputs;
    }
    
    /// <summary>
    /// Parses a makeCredential response from CBOR-encoded data.
    /// </summary>
    /// <param name="data">The CBOR-encoded response (excluding status byte).</param>
    /// <returns>The parsed response.</returns>
    public static MakeCredentialResponse Decode(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data, CborConformanceMode.Lax);
        return Decode(reader);
    }
    
    /// <summary>
    /// Parses a makeCredential response from a CBOR reader.
    /// </summary>
    /// <param name="reader">The CBOR reader.</param>
    /// <returns>The parsed response.</returns>
    public static MakeCredentialResponse Decode(CborReader reader)
    {
        var mapLength = reader.ReadStartMap();
        
        string? format = null;
        byte[]? authDataRaw = null;
        AuthenticatorData? authData = null;
        AttestationStatement? attStmt = null;
        bool? epAtt = null;
        byte[]? largeBlobKey = null;
        ReadOnlyMemory<byte>? extensionOutputs = null;
        
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();
            switch (key)
            {
                case 1: // fmt
                    format = reader.ReadTextString();
                    break;
                case 2: // authData
                    authDataRaw = reader.ReadByteString();
                    authData = AuthenticatorData.Parse(authDataRaw);
                    if (authData.HasExtensions && authData.Extensions.HasValue)
                    {
                        extensionOutputs = authData.Extensions;
                    }
                    break;
                case 3: // attStmt
                    attStmt = AttestationStatement.Decode(reader);
                    break;
                case 4: // epAtt
                    epAtt = reader.ReadBoolean();
                    break;
                case 5: // largeBlobKey
                    largeBlobKey = reader.ReadByteString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        
        reader.ReadEndMap();
        
        if (format is null || authData is null || authDataRaw is null || attStmt is null)
        {
            throw new InvalidOperationException("MakeCredential response missing required fields.");
        }
        
        return new MakeCredentialResponse(
            format,
            authData,
            authDataRaw,
            attStmt,
            epAtt,
            largeBlobKey,
            extensionOutputs);
    }
    
    /// <summary>
    /// Gets the credential ID from the attested credential data.
    /// </summary>
    /// <returns>The credential ID, or empty if not present.</returns>
    public ReadOnlyMemory<byte> GetCredentialId() =>
        AuthenticatorData.AttestedCredentialData?.CredentialId ?? ReadOnlyMemory<byte>.Empty;
    
    /// <summary>
    /// Gets the credential public key from the attested credential data.
    /// </summary>
    /// <returns>The COSE-encoded public key, or empty if not present.</returns>
    public ReadOnlyMemory<byte> GetCredentialPublicKey() =>
        AuthenticatorData.AttestedCredentialData?.CredentialPublicKey ?? ReadOnlyMemory<byte>.Empty;
    
    /// <summary>
    /// Gets the AAGUID from the attested credential data.
    /// </summary>
    /// <returns>The AAGUID, or <see cref="Guid.Empty"/> if not present.</returns>
    public Guid GetAaguid() =>
        AuthenticatorData.AttestedCredentialData?.Aaguid ?? Guid.Empty;
}

/// <summary>
/// Represents an attestation statement from a makeCredential response.
/// </summary>
public sealed class AttestationStatement
{
    /// <summary>
    /// Gets the attestation signature.
    /// </summary>
    public ReadOnlyMemory<byte>? Signature { get; }
    
    /// <summary>
    /// Gets the attestation certificate chain.
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<byte>>? X5c { get; }
    
    /// <summary>
    /// Gets the ECDAA key ID (for ECDAA attestation).
    /// </summary>
    public ReadOnlyMemory<byte>? EcdaaKeyId { get; }
    
    /// <summary>
    /// Gets the algorithm used for the signature.
    /// </summary>
    public int? Algorithm { get; }
    
    /// <summary>
    /// Gets the raw CBOR representation of the attestation statement.
    /// </summary>
    public ReadOnlyMemory<byte> RawData { get; }
    
    /// <summary>
    /// Gets a value indicating whether this is a "none" attestation (self-attestation).
    /// </summary>
    public bool IsNone => Signature is null && X5c is null;
    
    private AttestationStatement(
        ReadOnlyMemory<byte>? signature,
        IReadOnlyList<ReadOnlyMemory<byte>>? x5c,
        ReadOnlyMemory<byte>? ecdaaKeyId,
        int? algorithm,
        ReadOnlyMemory<byte> rawData)
    {
        Signature = signature;
        X5c = x5c;
        EcdaaKeyId = ecdaaKeyId;
        Algorithm = algorithm;
        RawData = rawData;
    }
    
    /// <summary>
    /// Decodes an attestation statement from CBOR.
    /// </summary>
    /// <param name="reader">The CBOR reader.</param>
    /// <returns>The parsed attestation statement.</returns>
    public static AttestationStatement Decode(CborReader reader)
    {
        // Capture raw data by encoding what we read
        var startPosition = reader.BytesRemaining;
        
        var mapLength = reader.ReadStartMap();
        
        byte[]? sig = null;
        List<ReadOnlyMemory<byte>>? x5c = null;
        byte[]? ecdaaKeyId = null;
        int? alg = null;
        
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadTextString();
            switch (key)
            {
                case "sig":
                    sig = reader.ReadByteString();
                    break;
                case "x5c":
                    x5c = [];
                    var certCount = reader.ReadStartArray();
                    for (var j = 0; j < certCount; j++)
                    {
                        x5c.Add(reader.ReadByteString());
                    }
                    reader.ReadEndArray();
                    break;
                case "ecdaaKeyId":
                    ecdaaKeyId = reader.ReadByteString();
                    break;
                case "alg":
                    alg = reader.ReadInt32();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        
        reader.ReadEndMap();
        
        var bytesConsumed = startPosition - reader.BytesRemaining;
        // Note: We don't have easy access to the raw bytes from CborReader,
        // so RawData will be empty for now. In practice, callers should use
        // the full response raw data if needed.
        var rawData = ReadOnlyMemory<byte>.Empty;
        
        // Explicitly convert null byte arrays to null ReadOnlyMemory<byte>?
        ReadOnlyMemory<byte>? sigMemory = sig is not null ? (ReadOnlyMemory<byte>?)new ReadOnlyMemory<byte>(sig) : (ReadOnlyMemory<byte>?)null;
        ReadOnlyMemory<byte>? ecdaaKeyIdMemory = ecdaaKeyId is not null ? (ReadOnlyMemory<byte>?)new ReadOnlyMemory<byte>(ecdaaKeyId) : (ReadOnlyMemory<byte>?)null;
        
        return new AttestationStatement(sigMemory, x5c, ecdaaKeyIdMemory, alg, rawData);
    }
}
