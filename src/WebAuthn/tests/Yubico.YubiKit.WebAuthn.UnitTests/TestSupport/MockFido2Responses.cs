// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Formats.Cbor;
using System.Security.Cryptography;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Credentials;

namespace Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

/// <summary>
/// Shared test helpers for creating mock FIDO2 CBOR responses.
/// </summary>
internal static class MockFido2Responses
{
    public static MakeCredentialResponse CreateMockMakeCredentialResponse(Guid? aaguid = null)
    {
        var guid = aaguid ?? Guid.NewGuid();
        var authData = BuildAuthDataWithAttestedCredential(guid);
        var cborBytes = BuildMakeCredentialResponseCbor(authData, "none");
        return MakeCredentialResponse.Decode(cborBytes);
    }

    public static AuthenticatorInfo CreateMockAuthenticatorInfo(
        bool clientPinSupported = false,
        bool uvSupported = false)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);

        var optionsCount = 0;
        if (clientPinSupported) optionsCount++;
        if (uvSupported) optionsCount++;

        writer.WriteStartMap(optionsCount > 0 ? 4 : 3);

        // 0x01: versions
        writer.WriteInt32(1);
        writer.WriteStartArray(2);
        writer.WriteTextString("FIDO_2_0");
        writer.WriteTextString("FIDO_2_1");
        writer.WriteEndArray();

        // 0x02: extensions
        writer.WriteInt32(2);
        writer.WriteStartArray(1);
        writer.WriteTextString("hmac-secret");
        writer.WriteEndArray();

        // 0x03: aaguid
        writer.WriteInt32(3);
        writer.WriteByteString(Guid.NewGuid().ToByteArray());

        // 0x04: options (conditional)
        if (optionsCount > 0)
        {
            writer.WriteInt32(4);
            writer.WriteStartMap(optionsCount);

            if (clientPinSupported)
            {
                writer.WriteTextString("clientPin");
                writer.WriteBoolean(true);
            }

            if (uvSupported)
            {
                writer.WriteTextString("uv");
                writer.WriteBoolean(true);
            }

            writer.WriteEndMap();
        }

        writer.WriteEndMap();

        return AuthenticatorInfo.Decode(writer.Encode());
    }

    public static byte[] BuildAuthDataWithAttestedCredential(Guid aaguid)
    {
        var data = new List<byte>();

        // rpIdHash (32 bytes)
        var rpIdHash = new byte[32];
        SHA256.HashData("example.com"u8, rpIdHash);
        data.AddRange(rpIdHash);

        // flags = UP | UV | AT (0x45)
        data.Add(0x45);

        // signCount (4 bytes, big-endian)
        data.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x01 });

        // AAGUID (16 bytes, big-endian network byte order)
        data.AddRange(EncodeAaguidBigEndian(aaguid));

        // Credential ID length (2 bytes, big-endian) = 32
        data.AddRange(new byte[] { 0x00, 0x20 });

        // Credential ID (32 bytes)
        var credId = new byte[32];
        RandomNumberGenerator.Fill(credId);
        data.AddRange(credId);

        // COSE public key (minimal EC2 key in CBOR)
        var keyWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        keyWriter.WriteStartMap(5);

        // kty = 2 (EC2)
        keyWriter.WriteInt32(1);
        keyWriter.WriteInt32(2);

        // alg = -7 (ES256)
        keyWriter.WriteInt32(3);
        keyWriter.WriteInt32(-7);

        // crv = 1 (P-256)
        keyWriter.WriteInt32(-1);
        keyWriter.WriteInt32(1);

        // x coordinate (32 bytes)
        keyWriter.WriteInt32(-2);
        keyWriter.WriteByteString(new byte[32]);

        // y coordinate (32 bytes)
        keyWriter.WriteInt32(-3);
        keyWriter.WriteByteString(new byte[32]);

        keyWriter.WriteEndMap();
        data.AddRange(keyWriter.Encode());

        return [.. data];
    }

    public static byte[] BuildMakeCredentialResponseCbor(byte[] authData, string format)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);

        writer.WriteStartMap(3);

        // 0x01: fmt
        writer.WriteInt32(1);
        writer.WriteTextString(format);

        // 0x02: authData
        writer.WriteInt32(2);
        writer.WriteByteString(authData);

        // 0x03: attStmt (empty for "none" format)
        writer.WriteInt32(3);
        writer.WriteStartMap(0);
        writer.WriteEndMap();

        writer.WriteEndMap();

        return writer.Encode();
    }

    public static byte[] EncodeAaguidBigEndian(Guid guid)
    {
        // AAGUID must be in big-endian (network byte order)
        // .NET Guid.ToByteArray() gives little-endian on little-endian systems
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes);

        // Convert first 3 components from little-endian to big-endian
        if (BitConverter.IsLittleEndian)
        {
            // Reverse Data1 (4 bytes)
            (bytes[0], bytes[1], bytes[2], bytes[3]) =
                (bytes[3], bytes[2], bytes[1], bytes[0]);

            // Reverse Data2 (2 bytes)
            (bytes[4], bytes[5]) = (bytes[5], bytes[4]);

            // Reverse Data3 (2 bytes)
            (bytes[6], bytes[7]) = (bytes[7], bytes[6]);
        }

        return bytes.ToArray();
    }
}
