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

namespace Yubico.YubiKit.Ctap2.Credentials;

/// <summary>
/// Represents the response from authenticatorMakeCredential.
/// </summary>
/// <remarks>
/// <para>
/// Exposes the received authenticator data and attestation statement for registration processing.
/// Decoding does not verify the attestation signature, certificate chain, challenge/origin, RP/account
/// binding, required UP/UV or RP attestation policy. Those checks belong to the RP registration workflow.
/// </para>
/// <para>
/// CTAP2 makeCredential response structure:
/// - 0x01: fmt (text string) - attestation statement format
/// - 0x02: authData (byte string) - authenticator data
/// - 0x03: attStmt (map) - attestation statement
/// - 0x04: epAtt (bool, optional) - enterprise attestation used
/// - 0x05: largeBlobKey (byte string, optional) - large blob key
/// - 0x06: unsignedExtensionOutputs (map, optional) - unsigned outputs (CTAP 2.3 §6.1)
/// </para>
/// <para>
/// Decode borrows attestation-statement and unsigned-output slices from the input buffer. Keep that
/// buffer unchanged while these views are used. Authenticator-data byte strings, parsed authenticator
/// fields and largeBlobKey are independent copies. This result has no disposal/zeroing API, and session
/// disposal does not clear it. Clearing the input cannot clear independent secret-bearing copies;
/// do not log large-blob keys or secret-derived extension data.
/// </para>
/// <para>
/// Unknown outer fields are skipped, not retained in a complete RawData envelope. Raw authenticator
/// data and attestation-statement bytes preserve only their respective nested structures.
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
    /// Gets the parsed authenticator data; the decoder does not require the AT bit or new credential data.
    /// </summary>
    public AuthenticatorData AuthenticatorData { get; }

    /// <summary>
    /// Gets the raw authenticator data bytes.
    /// </summary>
    public ReadOnlyMemory<byte> AuthenticatorDataRaw { get; }

    /// <summary>
    /// Gets the parsed or opaque attestation statement, not a verified attestation result.
    /// </summary>
    public AttestationStatement AttestationStatement { get; }

    /// <summary>
    /// Gets whether enterprise attestation was used.
    /// </summary>
    public bool? EnterpriseAttestation { get; }

    /// <summary>
    /// Gets the secret symmetric large-blob storage key from response key 0x05, if supplied.
    /// </summary>
    /// <remarks>The decoder does not validate key length or whether the extension was requested.</remarks>
    public ReadOnlyMemory<byte>? LargeBlobKey { get; }

    /// <summary>
    /// Gets the signed authenticator-data extension bytes, if the ED bit is set.
    /// </summary>
    public ReadOnlyMemory<byte>? ExtensionOutputs { get; }

    /// <summary>
    /// Gets the unsigned extension outputs from makeCredential response key 0x06.
    /// </summary>
    /// <remarks>
    /// CTAP 2.3 §6.1 defines text extension identifiers mapped to unsigned values. Each value here is
    /// its complete encoded CBOR slice, not necessarily a CBOR byte string, and borrows the input buffer.
    /// An empty map and an omitted field must be treated equivalently by clients, although this model
    /// preserves empty versus null. These values are separate from signed authData outputs; extensions
    /// such as experimental previewSign can carry proof objects that require their own verification.
    /// WebAuthn client-extension processing is a separate layer, not this integer-key envelope.
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
    /// <remarks>
    /// The input is borrowed as described on this type. Uses lax CBOR parsing and requires fmt,
    /// authData and attStmt, but does not enforce every attestation shape or complete envelope consumption.
    /// CBOR failures can raise <see cref="CborContentException"/>; missing modeled required fields raise
    /// <see cref="InvalidOperationException"/>. Authenticator-data parsing can also reject short input.
    /// </remarks>
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
    /// <remarks>
    /// Consumes one encoded value and applies the memory decoder's checks. Attestation and unsigned
    /// values can reference the reader's underlying buffer; keep it unchanged while those views are used.
    /// This overload is not a complete-input or cryptographic-verification boundary.
    /// </remarks>
    public static MakeCredentialResponse Decode(CborReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        return Decode(reader.ReadEncodedValue(disableConformanceModeChecks: true));
    }

    /// <summary>
    /// Decodes one response map while retaining slices of the borrowed response bytes.
    /// </summary>
    private static MakeCredentialResponse DecodeInternal(
        CborReader reader,
        ReadOnlyMemory<byte> fullCbor)
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
                    attStmtOffset = fullCbor.Length - attStmtBytesBefore;
                    break;
                case 4: // epAtt
                    epAtt = reader.ReadBoolean();
                    break;
                case 5: // largeBlobKey
                    largeBlobKey = reader.ReadByteString();
                    break;
                case 6: // makeCredential unsignedExtensionOutputs (CTAP 2.3 §6.1); values borrow fullCbor.
                    unsignedExtensionOutputs = new Dictionary<string, ReadOnlyMemory<byte>>();
                    int? extMapSize = reader.ReadStartMap();
                    for (int j = 0; j < extMapSize; j++)
                    {
                        string extId = reader.ReadTextString();

                        // Capture the raw CBOR value bytes
                        int bytesRemainingBefore = reader.BytesRemaining;
                        reader.SkipValue();
                        int bytesConsumed = bytesRemainingBefore - reader.BytesRemaining;

                        int valueOffset = fullCbor.Length - bytesRemainingBefore;
                        unsignedExtensionOutputs[extId] = fullCbor.Slice(valueOffset, bytesConsumed);
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
        if (attStmtOffset >= 0 && attStmtLength > 0)
        {
            var rawAttStmt = fullCbor.Slice(attStmtOffset, attStmtLength);
            var attestationFormat = ParseAttestationFormat(format);
            attStmt = AttestationStatement.Decode(attestationFormat, rawAttStmt);
        }
        else
        {
            throw new InvalidOperationException("MakeCredential response missing attestation statement.");
        }

        return new MakeCredentialResponse(
            format,
            authData,
            authDataRaw,
            attStmt,
            epAtt,
            // byte[]? → ReadOnlyMemory<byte>? converts a null array to an EMPTY memory; keep absent as null.
            largeBlobKey is null ? null : (ReadOnlyMemory<byte>?)largeBlobKey,
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
    /// <remarks>Empty is a helper fallback, not proof that a makeCredential response satisfies the protocol.</remarks>
    public ReadOnlyMemory<byte> GetCredentialId() =>
        AuthenticatorData.AttestedCredentialData?.CredentialId ?? ReadOnlyMemory<byte>.Empty;

    /// <summary>
    /// Gets the credential public key from the attested credential data.
    /// </summary>
    /// <returns>The COSE-encoded public key, or empty if not present.</returns>
    /// <remarks>The attested-data parser extracts the encoding without verifying key structure or mathematical validity.</remarks>
    public ReadOnlyMemory<byte> GetCredentialPublicKey() =>
        AuthenticatorData.AttestedCredentialData?.CredentialPublicKey ?? ReadOnlyMemory<byte>.Empty;

    /// <summary>
    /// Gets the AAGUID from the attested credential data.
    /// </summary>
    /// <returns>The AAGUID, or <see cref="Guid.Empty"/> if not present.</returns>
    /// <remarks>Guid.Empty can also be received on the wire; inspect AttestedCredentialData to distinguish absence.</remarks>
    public Guid GetAaguid() =>
        AuthenticatorData.AttestedCredentialData?.Aaguid ?? Guid.Empty;
}