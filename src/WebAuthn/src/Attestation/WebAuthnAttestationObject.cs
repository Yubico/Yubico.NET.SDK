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

namespace Yubico.YubiKit.WebAuthn.Attestation;

/// <summary>
/// WebAuthn attestation object.
/// </summary>
/// <remarks>
/// <para>
/// The attestation object is a CBOR map with string keys:
/// - "fmt": attestation format identifier (text string)
/// - "attStmt": attestation statement (CBOR map)
/// - "authData": authenticator data (byte string)
/// </para>
/// <para>
/// See: https://www.w3.org/TR/webauthn-2/#sctn-attestation
/// </para>
/// </remarks>
public sealed class WebAuthnAttestationObject
{
    /// <summary>
    /// Gets the authenticator data.
    /// </summary>
    public WebAuthnAuthenticatorData AuthenticatorData { get; }

    /// <summary>
    /// Gets the attestation statement.
    /// </summary>
    public AttestationStatement Statement { get; }

    /// <summary>
    /// Gets the raw CBOR representation of the attestation object.
    /// </summary>
    public ReadOnlyMemory<byte> RawCbor { get; }

    private WebAuthnAttestationObject(
        WebAuthnAuthenticatorData authenticatorData,
        AttestationStatement statement,
        ReadOnlyMemory<byte> rawCbor)
    {
        AuthenticatorData = authenticatorData;
        Statement = statement;
        RawCbor = rawCbor;
    }

    /// <summary>
    /// Decodes an attestation object from CBOR bytes.
    /// </summary>
    /// <param name="cbor">The CBOR-encoded attestation object.</param>
    /// <returns>The decoded attestation object.</returns>
    public static WebAuthnAttestationObject Decode(ReadOnlyMemory<byte> cbor)
    {
        var reader = new CborReader(cbor, CborConformanceMode.Lax);
        var mapLength = reader.ReadStartMap();

        string? fmt = null;
        byte[]? authDataBytes = null;
        ReadOnlyMemory<byte>? attStmtRawCbor = null;

        // Track offset for capturing attStmt raw bytes
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadTextString();
            switch (key)
            {
                case "fmt":
                    fmt = reader.ReadTextString();
                    break;
                case "authData":
                    authDataBytes = reader.ReadByteString();
                    break;
                case "attStmt":
                    // Capture raw CBOR by tracking bytes remaining before/after
                    var bytesRemainingBefore = reader.BytesRemaining;
                    reader.SkipValue(); // Skip the attStmt to get past it
                    var bytesConsumed = bytesRemainingBefore - reader.BytesRemaining;
                    var offset = cbor.Length - bytesRemainingBefore;
                    attStmtRawCbor = cbor.Slice(offset, bytesConsumed);
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();

        if (fmt is null || authDataBytes is null || !attStmtRawCbor.HasValue)
        {
            throw new InvalidOperationException("Attestation object missing required fields (fmt, authData, attStmt).");
        }

        var authenticatorData = WebAuthnAuthenticatorData.Decode(authDataBytes);
        var format = new AttestationFormat(fmt);
        var statement = AttestationStatement.Decode(format, attStmtRawCbor.Value);

        return new WebAuthnAttestationObject(authenticatorData, statement, cbor);
    }

    /// <summary>
    /// Encodes the attestation object to CBOR bytes.
    /// </summary>
    /// <returns>The CBOR-encoded attestation object.</returns>
    public byte[] Encode()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);

        writer.WriteStartMap(3);

        // "authData" key (CBOR text string keys are sorted lexicographically in canonical mode)
        writer.WriteTextString("authData");
        writer.WriteByteString(AuthenticatorData.Raw.Span);

        // "attStmt" key
        writer.WriteTextString("attStmt");
        writer.WriteEncodedValue(Statement.RawCbor.Span);

        // "fmt" key
        writer.WriteTextString("fmt");
        writer.WriteTextString(Statement.Format.Value);

        writer.WriteEndMap();

        return writer.Encode();
    }
}
