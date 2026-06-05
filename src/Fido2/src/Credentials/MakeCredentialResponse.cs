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
/// - 0x06: unsignedExtensionOutputs (map, optional) - unsigned extension outputs (CTAP 2.2 / WebAuthn L3)
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

    /// <summary>
    /// Gets the unsigned extension outputs map (CTAP 2.2 / WebAuthn L3 key 6).
    /// </summary>
    /// <remarks>
    /// Per CTAP 2.2 / WebAuthn L3, unsigned extension outputs is a map keyed by extension
    /// identifier (text string) with values as raw CBOR bytes. Used by extensions
    /// like previewSign to deliver attestation objects via top-level response.
    /// Aligned with yubikit-swift, yubikit-android, yubikit-python.
    /// </remarks>
    public IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? UnsignedExtensionOutputs { get; }
    
    private MakeCredentialResponse(
        string format,
        AuthenticatorData authenticatorData,
        ReadOnlyMemory<byte> authenticatorDataRaw,
        AttestationStatement attestationStatement,
        bool? enterpriseAttestation,
        ReadOnlyMemory<byte>? largeBlobKey,
        ReadOnlyMemory<byte>? extensionOutputs,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? unsignedExtensionOutputs)
    {
        Format = format;
        AuthenticatorData = authenticatorData;
        AuthenticatorDataRaw = authenticatorDataRaw;
        AttestationStatement = attestationStatement;
        EnterpriseAttestation = enterpriseAttestation;
        LargeBlobKey = largeBlobKey;
        ExtensionOutputs = extensionOutputs;
        UnsignedExtensionOutputs = unsignedExtensionOutputs;
    }
    
    /// <summary>
    /// Parses a makeCredential response from CBOR-encoded data.
    /// </summary>
    /// <param name="data">The CBOR-encoded response (excluding status byte).</param>
    /// <returns>The parsed response.</returns>
    public static MakeCredentialResponse Decode(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data, CborConformanceMode.Lax);
        return DecodeInternal(reader, data);
    }
    
    /// <summary>
    /// Parses a makeCredential response from a CBOR reader.
    /// </summary>
    /// <param name="reader">The CBOR reader.</param>
    /// <returns>The parsed response.</returns>
    public static MakeCredentialResponse Decode(CborReader reader) =>
        DecodeInternal(reader, fullCbor: null);

    /// <summary>
    /// Internal decoder that optionally captures raw CBOR for attestation statement.
    /// </summary>
    private static MakeCredentialResponse DecodeInternal(
        CborReader reader,
        ReadOnlyMemory<byte>? fullCbor)
    {
        var mapLength = reader.ReadStartMap();

        string? format = null;
        byte[]? authDataRaw = null;
        AuthenticatorData? authData = null;
        AttestationStatement? attStmt = null;
        bool? epAtt = null;
        byte[]? largeBlobKey = null;
        ReadOnlyMemory<byte>? extensionOutputs = null;
        Dictionary<string, ReadOnlyMemory<byte>>? unsignedExtensionOutputs = null;

        // Track offset for raw CBOR capture
        int attStmtOffset = -1;
        int attStmtLength = -1;

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
                    // Calculate offset before reading
                    var attStmtBytesBefore = reader.BytesRemaining;

                    // Skip the CBOR value to calculate its length
                    reader.SkipValue();

                    // Calculate how many bytes were consumed
                    var attStmtBytesConsumed = attStmtBytesBefore - reader.BytesRemaining;
                    attStmtLength = attStmtBytesConsumed;
                    attStmtOffset = fullCbor.HasValue
                        ? fullCbor.Value.Length - attStmtBytesBefore
                        : -1;
                    break;
                case 4: // epAtt
                    epAtt = reader.ReadBoolean();
                    break;
                case 5: // largeBlobKey
                    largeBlobKey = reader.ReadByteString();
                    break;
                case 6: // unsignedExtensionOutputs (CTAP 2.2 / WebAuthn L3, aligned with sister SDKs)
                    unsignedExtensionOutputs = new Dictionary<string, ReadOnlyMemory<byte>>();
                    int? extMapSize = reader.ReadStartMap();
                    for (int j = 0; j < extMapSize; j++)
                    {
                        string extId = reader.ReadTextString();

                        // Capture the raw CBOR value bytes
                        int bytesRemainingBefore = reader.BytesRemaining;
                        reader.SkipValue();
                        int bytesConsumed = bytesRemainingBefore - reader.BytesRemaining;

                        // Extract the value from fullCbor if available
                        if (fullCbor.HasValue)
                        {
                            int valueOffset = fullCbor.Value.Length - bytesRemainingBefore;
                            unsignedExtensionOutputs[extId] = fullCbor.Value.Slice(valueOffset, bytesConsumed);
                        }
                    }
                    reader.ReadEndMap();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();

        if (format is null || authData is null || authDataRaw is null)
        {
            throw new InvalidOperationException("MakeCredential response missing required fields.");
        }

        // Decode attestation statement using the format-aware typed decoder
        if (fullCbor.HasValue && attStmtOffset >= 0 && attStmtLength > 0)
        {
            var rawAttStmt = fullCbor.Value.Slice(attStmtOffset, attStmtLength);
            var attestationFormat = ParseAttestationFormat(format);
            attStmt = AttestationStatement.Decode(attestationFormat, rawAttStmt);
        }
        else
        {
            throw new InvalidOperationException("AttestationStatement decoding requires fullCbor parameter.");
        }

        return new MakeCredentialResponse(
            format,
            authData,
            authDataRaw,
            attStmt,
            epAtt,
            largeBlobKey,
            extensionOutputs,
            unsignedExtensionOutputs);
    }

    /// <summary>
    /// Parses the attestation format string into the AttestationFormat type.
    /// </summary>
    private static AttestationFormat ParseAttestationFormat(string format) => format switch
    {
        "packed" => AttestationFormat.Packed,
        "fido-u2f" => AttestationFormat.FidoU2F,
        "apple" => AttestationFormat.Apple,
        "none" => AttestationFormat.None,
        "android-key" => AttestationFormat.AndroidKey,
        "android-safetynet" => AttestationFormat.AndroidSafetynet,
        "tpm" => AttestationFormat.Tpm,
        _ => AttestationFormat.Other(format)
    };

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
