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
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.WebAuthn.Attestation;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Attestation;

public class AttestationObjectTests
{
    [Fact]
    public void Decode_BorrowsOuterAndStatementCbor_ButMaterializesAuthenticatorData()
    {
        var source = BuildPackedAttestationObject(includeUnknownField: true);
        var decoded = WebAuthnAttestationObject.Decode(source);
        Assert.True(MemoryMarshal.TryGetArray(decoded.RawCbor, out var raw));
        Assert.Same(source, raw.Array);
        Assert.Equal(source.Length, raw.Count);
        Assert.True(MemoryMarshal.TryGetArray(decoded.Statement.RawCbor, out var statement));
        Assert.Same(source, statement.Array);

        byte[] previousEncoding = decoded.Encode();
        Assert.NotEqual(source.Length, previousEncoding.Length); // unknown outer field is only in RawCbor
        int signatureOffset = source.AsSpan().IndexOf(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
        int authDataOffset = source.AsSpan().IndexOf(decoded.AuthenticatorData.Raw.Span);
        Assert.True(signatureOffset >= statement.Offset && signatureOffset < statement.Offset + statement.Count);
        Assert.True(authDataOffset >= 0);

        // Deliberately violate the borrowing contract to demonstrate the retained views.
        source[signatureOffset] = 0xDA;
        source[authDataOffset + 32] = 0;
        Assert.Equal(0xDA, decoded.RawCbor.Span[signatureOffset]);
        Assert.Equal(0xDA, decoded.Statement.RawCbor.Span[signatureOffset - statement.Offset]);
        Assert.Equal(0x01, decoded.AuthenticatorData.Raw.Span[32]);
        Assert.True(decoded.AuthenticatorData.UserPresent);
        Assert.Equal(0xDA, Assert.IsType<PackedAttestationStatement>(
            WebAuthnAttestationObject.Decode(decoded.Encode()).Statement).Signature.Span[0]);
    }

    [Fact]
    public void Decode_PackedAttestation_RoundTripIdentical()
    {
        // Arrange - Build a packed attestation object
        var attestationObject = BuildPackedAttestationObject();

        // Act - Decode and re-encode
        var decoded = WebAuthnAttestationObject.Decode(attestationObject);
        var reEncoded = decoded.Encode();

        // Assert - Byte-identical round-trip
        Assert.True(attestationObject.AsSpan().SequenceEqual(reEncoded.AsSpan()));
    }

    [Fact]
    public void Decode_NoneAttestation_RoundTripIdentical()
    {
        // Arrange - Build a "none" attestation object
        var attestationObject = BuildNoneAttestationObject();

        // Act
        var decoded = WebAuthnAttestationObject.Decode(attestationObject);
        var reEncoded = decoded.Encode();

        // Assert
        Assert.True(attestationObject.AsSpan().SequenceEqual(reEncoded));
    }

    [Fact]
    public void Decode_FidoU2FAttestation_RoundTripIdentical()
    {
        // Arrange
        var attestationObject = BuildFidoU2FAttestationObject();

        // Act
        var decoded = WebAuthnAttestationObject.Decode(attestationObject);
        var reEncoded = decoded.Encode();

        // Assert
        Assert.True(attestationObject.AsSpan().SequenceEqual(reEncoded));
    }

    [Fact]
    public void Decode_PackedAttestation_PopulatesStatement()
    {
        // Arrange
        var attestationObject = BuildPackedAttestationObject();

        // Act
        var decoded = WebAuthnAttestationObject.Decode(attestationObject);

        // Assert
        Assert.NotNull(decoded.Statement);
        Assert.Equal(AttestationFormat.Packed, decoded.Statement.Format);

        var packed = Assert.IsType<PackedAttestationStatement>(decoded.Statement);
        Assert.Equal(-7, packed.Algorithm); // ES256
        Assert.False(packed.Signature.IsEmpty);
        Assert.True(packed.Signature.Length > 0);
    }

    [Fact]
    public void Decode_NoneAttestation_PopulatesNoneStatement()
    {
        // Arrange
        var attestationObject = BuildNoneAttestationObject();

        // Act
        var decoded = WebAuthnAttestationObject.Decode(attestationObject);

        // Assert
        Assert.NotNull(decoded.Statement);
        Assert.Equal(AttestationFormat.None, decoded.Statement.Format);
        Assert.IsType<NoneAttestationStatement>(decoded.Statement);
    }

    // Helper: Build a minimal packed attestation object
    private static byte[] BuildPackedAttestationObject(bool includeUnknownField = false)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);

        writer.WriteStartMap(includeUnknownField ? 4 : 3);

        // "authData" - minimal authenticator data (37 bytes: rpIdHash + flags + signCount)
        writer.WriteTextString("authData");
        var authData = new byte[37];
        SHA256.HashData("example.com"u8, authData.AsSpan(0, 32)); // rpIdHash
        authData[32] = 0x01; // flags: UP
        // signCount = 0 (4 bytes, already zero)
        writer.WriteByteString(authData);

        // "attStmt" - packed attestation statement
        writer.WriteTextString("attStmt");
        writer.WriteStartMap(2);
        writer.WriteTextString("alg");
        writer.WriteInt32(-7); // ES256
        writer.WriteTextString("sig");
        writer.WriteByteString([0xDE, 0xAD, 0xBE, 0xEF]);
        writer.WriteEndMap();

        // "fmt"
        writer.WriteTextString("fmt");
        writer.WriteTextString("packed");

        if (includeUnknownField)
        {
            writer.WriteTextString("future");
            writer.WriteInt32(42);
        }

        writer.WriteEndMap();

        return writer.Encode();
    }

    // Helper: Build a "none" attestation object
    private static byte[] BuildNoneAttestationObject()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);

        writer.WriteStartMap(3);

        // "authData"
        writer.WriteTextString("authData");
        var authData = new byte[37];
        SHA256.HashData("example.com"u8, authData.AsSpan(0, 32));
        authData[32] = 0x01; // UP
        writer.WriteByteString(authData);

        // "attStmt" - empty map
        writer.WriteTextString("attStmt");
        writer.WriteStartMap(0);
        writer.WriteEndMap();

        // "fmt"
        writer.WriteTextString("fmt");
        writer.WriteTextString("none");

        writer.WriteEndMap();

        return writer.Encode();
    }

    // Helper: Build a fido-u2f attestation object
    private static byte[] BuildFidoU2FAttestationObject()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);

        writer.WriteStartMap(3);

        // "authData"
        writer.WriteTextString("authData");
        var authData = new byte[37];
        SHA256.HashData("example.com"u8, authData.AsSpan(0, 32));
        authData[32] = 0x01;
        writer.WriteByteString(authData);

        // "attStmt" - fido-u2f requires sig + x5c
        writer.WriteTextString("attStmt");
        writer.WriteStartMap(2);
        writer.WriteTextString("sig");
        writer.WriteByteString([0xCA, 0xFE, 0xBA, 0xBE]);
        writer.WriteTextString("x5c");
        writer.WriteStartArray(1);
        writer.WriteByteString([0x30, 0x82, 0x01, 0x00]); // Dummy cert
        writer.WriteEndArray();
        writer.WriteEndMap();

        // "fmt"
        writer.WriteTextString("fmt");
        writer.WriteTextString("fido-u2f");

        writer.WriteEndMap();

        return writer.Encode();
    }
}