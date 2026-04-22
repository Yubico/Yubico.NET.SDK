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

namespace Yubico.YubiKit.WebAuthn.UnitTests.Cose;

/// <summary>
/// COSE key test fixtures.
/// </summary>
/// <remarks>
/// These fixtures were generated programmatically to ensure correct CBOR canonical encoding.
/// See the GenerateFixtures method for the source code used to create them.
/// </remarks>
internal static class Fixtures
{
    private static ReadOnlyMemory<byte>? _es256Key;
    private static ReadOnlyMemory<byte>? _edDsaKey;
    private static ReadOnlyMemory<byte>? _rsaKey;

    /// <summary>
    /// ES256 (ECDSA P-256) key with algorithm -7, curve 1 (P-256).
    /// </summary>
    public static ReadOnlyMemory<byte> Es256Key
    {
        get
        {
            if (_es256Key is null)
            {
                var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
                writer.WriteStartMap(5);
                writer.WriteInt32(-3); // y
                writer.WriteByteString(Convert.FromHexString("0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"));
                writer.WriteInt32(-2); // x
                writer.WriteByteString(Convert.FromHexString("FEDCBA9876543210FEDCBA9876543210FEDCBA9876543210FEDCBA9876543210"));
                writer.WriteInt32(-1); // crv
                writer.WriteInt32(1);
                writer.WriteInt32(1); // kty
                writer.WriteInt32(2);
                writer.WriteInt32(3); // alg
                writer.WriteInt32(-7);
                writer.WriteEndMap();
                _es256Key = writer.Encode();
            }
            return _es256Key.Value;
        }
    }

    /// <summary>
    /// EdDSA (Ed25519) key with algorithm -8, curve 6 (Ed25519).
    /// </summary>
    public static ReadOnlyMemory<byte> EdDsaKey
    {
        get
        {
            if (_edDsaKey is null)
            {
                var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
                writer.WriteStartMap(4);
                writer.WriteInt32(-2); // x
                writer.WriteByteString(Convert.FromHexString("ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789"));
                writer.WriteInt32(-1); // crv
                writer.WriteInt32(6);
                writer.WriteInt32(1); // kty
                writer.WriteInt32(1);
                writer.WriteInt32(3); // alg
                writer.WriteInt32(-8);
                writer.WriteEndMap();
                _edDsaKey = writer.Encode();
            }
            return _edDsaKey.Value;
        }
    }

    /// <summary>
    /// RS256 (RSA with SHA-256) key with algorithm -257, modulus 256 bytes, exponent 0x010001.
    /// </summary>
    public static ReadOnlyMemory<byte> RsaKey
    {
        get
        {
            if (_rsaKey is null)
            {
                var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
                writer.WriteStartMap(4);
                writer.WriteInt32(-2); // e
                writer.WriteByteString(Convert.FromHexString("010001"));
                writer.WriteInt32(-1); // n
                writer.WriteByteString(new byte[256]); // All zeros for test
                writer.WriteInt32(1); // kty
                writer.WriteInt32(3);
                writer.WriteInt32(3); // alg
                writer.WriteInt32(-257);
                writer.WriteEndMap();
                _rsaKey = writer.Encode();
            }
            return _rsaKey.Value;
        }
    }

    /// <summary>
    /// Helper method to generate the fixtures above.
    /// This is preserved for documentation purposes and to allow regeneration if needed.
    /// </summary>
    public static void GenerateFixtures()
    {
        // ES256 fixture
        {
            var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
            writer.WriteStartMap(5);
            writer.WriteInt32(-3); // y
            writer.WriteByteString(Convert.FromHexString("0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"));
            writer.WriteInt32(-2); // x
            writer.WriteByteString(Convert.FromHexString("FEDCBA9876543210FEDCBA9876543210FEDCBA9876543210FEDCBA9876543210"));
            writer.WriteInt32(-1); // crv
            writer.WriteInt32(1);
            writer.WriteInt32(1); // kty
            writer.WriteInt32(2);
            writer.WriteInt32(3); // alg
            writer.WriteInt32(-7);
            writer.WriteEndMap();
            Console.WriteLine("ES256: " + Convert.ToHexString(writer.Encode()));
        }

        // EdDSA fixture
        {
            var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
            writer.WriteStartMap(4);
            writer.WriteInt32(-2); // x
            writer.WriteByteString(Convert.FromHexString("ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789"));
            writer.WriteInt32(-1); // crv
            writer.WriteInt32(6);
            writer.WriteInt32(1); // kty
            writer.WriteInt32(1);
            writer.WriteInt32(3); // alg
            writer.WriteInt32(-8);
            writer.WriteEndMap();
            Console.WriteLine("EdDSA: " + Convert.ToHexString(writer.Encode()));
        }

        // RSA fixture
        {
            var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
            writer.WriteStartMap(4);
            writer.WriteInt32(-2); // e
            writer.WriteByteString(Convert.FromHexString("010001"));
            writer.WriteInt32(-1); // n
            writer.WriteByteString(new byte[256]); // All zeros for test
            writer.WriteInt32(1); // kty
            writer.WriteInt32(3);
            writer.WriteInt32(3); // alg
            writer.WriteInt32(-257);
            writer.WriteEndMap();
            Console.WriteLine("RSA: " + Convert.ToHexString(writer.Encode()));
        }
    }
}
