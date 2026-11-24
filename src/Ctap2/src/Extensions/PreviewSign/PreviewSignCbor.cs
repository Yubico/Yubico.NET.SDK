// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// you may not use this file except in compliance with the License.
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
using Yubico.YubiKit.Ctap2.Cbor;

namespace Yubico.YubiKit.Ctap2.Extensions;

/// <summary>Wire parsing for the previewSign v4 draft registration outputs, shared with WebAuthn.</summary>
/// <remarks>Parsing and re-encoding do not verify attestation or the signed extension data.</remarks>
[System.Diagnostics.CodeAnalysis.Experimental("YKEXP001")]
internal static class PreviewSignCbor
{
    /// <summary>Decodes the signing algorithm from the signed extension value.</summary>
    /// <remarks>
    /// The parser tolerates flags at key 4 of the signed outer map, but the draft puts signing-policy
    /// flags in the inner attestation.
    /// </remarks>
    internal static (int Algorithm, int? Flags) DecodeRegistrationOutput(CborReader reader)
    {
        const int algorithmKey = 3;
        const int flagsKey = 4;

        int? algorithm = null;
        int? flags = null;

        CtapResponseParser.ReadIntKeyMap(reader, (key, valueReader) =>
        {
            if (key == algorithmKey)
            {
                algorithm = valueReader.ReadInt32();
            }
            else if (key == flagsKey)
            {
                flags = valueReader.ReadInt32();
            }
            else
            {
                valueReader.SkipValue();
            }
        });

        return (algorithm ?? throw new InvalidOperationException("previewSign registration output missing required algorithm (key 3)"), flags);
    }

    /// <summary>
    /// The decoded format, copied authenticator data, and encoded statement slice from the inner attestation.
    /// </summary>
    internal readonly record struct InnerAttestationObject(
        string Fmt,
        ReadOnlyMemory<byte> AuthData,
        ReadOnlyMemory<byte> AttStmtRawCbor);

    /// <summary>
    /// Reads the byte string at key 7 of the unsigned output and decodes the integer-keyed CTAP attestation it contains.
    /// </summary>
    internal static InnerAttestationObject DecodeUnsignedRegistrationOutput(ReadOnlyMemory<byte> cbor)
    {
        const int attestationObjectKey = 7;
        var reader = new CborReader(cbor, CborConformanceMode.Ctap2Canonical);
        byte[]? innerCbor = null;

        CtapResponseParser.ReadIntKeyMap(reader, (key, valueReader) =>
        {
            if (key == attestationObjectKey)
            {
                innerCbor = valueReader.ReadByteString();
            }
            else
            {
                valueReader.SkipValue();
            }
        });

        if (reader.BytesRemaining != 0)
        {
            throw new CborContentException("Trailing data after the previewSign unsigned registration map.");
        }

        return DecodeInnerAttestationObject(innerCbor ??
            throw new InvalidOperationException("previewSign unsigned output missing attestation object (key 7)"));
    }

    private static InnerAttestationObject DecodeInnerAttestationObject(ReadOnlyMemory<byte> innerCbor)
    {
        const int formatKey = 1;
        const int authDataKey = 2;
        const int attestationStatementKey = 3;

        var reader = new CborReader(innerCbor, CborConformanceMode.Ctap2Canonical);
        string? fmt = null;
        byte[]? authData = null;
        ReadOnlyMemory<byte>? attStmtRaw = null;

        CtapResponseParser.ReadIntKeyMap(reader, (key, valueReader) =>
        {
            if (key == formatKey)
            {
                fmt = valueReader.ReadTextString();
            }
            else if (key == authDataKey)
            {
                authData = valueReader.ReadByteString();
            }
            else if (key == attestationStatementKey)
            {
                if (valueReader.PeekState() != CborReaderState.StartMap)
                {
                    throw new CborContentException("previewSign inner attestation statement must be a map.");
                }

                attStmtRaw = valueReader.ReadEncodedValue();
            }
            else
            {
                valueReader.SkipValue();
            }
        });

        if (reader.BytesRemaining != 0)
        {
            throw new CborContentException("Trailing data after the previewSign inner attestation object.");
        }

        return new InnerAttestationObject(
            fmt ?? throw new InvalidOperationException("previewSign inner attestation object missing fmt (key 1)"),
            authData ?? throw new InvalidOperationException("previewSign inner attestation object missing authData (key 2)"),
            attStmtRaw ?? throw new InvalidOperationException("previewSign inner attestation object missing attStmt (key 3)"));
    }

    /// <summary>Repackages the attestation with text keys, preserving authenticator data and statement bytes.</summary>
    internal static byte[] EncodeWebAuthnAttestationObject(InnerAttestationObject inner)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);
        writer.WriteTextString("fmt");
        writer.WriteTextString(inner.Fmt);
        writer.WriteTextString("authData");
        writer.WriteByteString(inner.AuthData.Span);
        writer.WriteTextString("attStmt");
        // The statement is a map, not a byte string containing a map.
        writer.WriteEncodedValue(inner.AttStmtRawCbor.Span);
        writer.WriteEndMap();
        return writer.Encode();
    }
}