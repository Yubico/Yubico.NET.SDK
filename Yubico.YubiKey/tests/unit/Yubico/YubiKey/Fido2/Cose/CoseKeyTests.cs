// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
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

using System;
using System.Formats.Cbor;
using System.Linq;
using Xunit;

namespace Yubico.YubiKey.Fido2.Cose
{
    public class CoseKeyTests
    {
        [Fact]
        public void Create_EdDsaOkpKey_ReturnsEdDsaPublicKey()
        {
            byte[] encodedKey = BuildOkpKey(CoseAlgorithmIdentifier.EdDSA);

            CoseKey key = CoseKey.Create(encodedKey, out int bytesRead);

            var edDsaKey = Assert.IsType<CoseEdDsaPublicKey>(key);
            Assert.Equal(encodedKey.Length, bytesRead);
            Assert.Equal(CoseKeyType.Okp, edDsaKey.Type);
            Assert.Equal(CoseAlgorithmIdentifier.EdDSA, edDsaKey.Algorithm);
        }

        [Fact]
        public void Create_Es256WithOkpKeyType_ThrowsCtap2DataException()
        {
            byte[] encodedKey = BuildOkpKey(CoseAlgorithmIdentifier.ES256);

            _ = Assert.Throws<Ctap2DataException>(() => CoseKey.Create(encodedKey, out _));
        }

        [Fact]
        public void Create_EdDsaWithEc2KeyType_ThrowsCtap2DataException()
        {
            byte[] encodedKey = BuildEc2Key(CoseAlgorithmIdentifier.EdDSA);

            _ = Assert.Throws<Ctap2DataException>(() => CoseKey.Create(encodedKey, out _));
        }

        [Fact]
        public void Create_UnsupportedAlgorithm_ThrowsNotSupportedException()
        {
            byte[] encodedKey = BuildEc2Key((CoseAlgorithmIdentifier)(-70000));

            _ = Assert.Throws<NotSupportedException>(() => CoseKey.Create(encodedKey, out _));
        }

        [Fact]
        public void CreateOrUnsupported_Es256Ec2Key_ReturnsEcPublicKey()
        {
            byte[] encodedKey = BuildEc2Key(CoseAlgorithmIdentifier.ES256);

            CoseKey key = CoseKey.CreateOrUnsupported(encodedKey);

            var ecKey = Assert.IsType<CoseEcPublicKey>(key);
            Assert.Equal(CoseKeyType.Ec2, ecKey.Type);
            Assert.Equal(CoseAlgorithmIdentifier.ES256, ecKey.Algorithm);
        }

        [Fact]
        public void CreateOrUnsupported_EdDsaOkpKey_ReturnsEdDsaPublicKey()
        {
            byte[] encodedKey = BuildOkpKey(CoseAlgorithmIdentifier.EdDSA);

            CoseKey key = CoseKey.CreateOrUnsupported(encodedKey);

            _ = Assert.IsType<CoseEdDsaPublicKey>(key);
        }

        [Fact]
        public void CreateOrUnsupported_UnsupportedAlgorithm_ReturnsUnsupportedKey()
        {
            byte[] encodedKey = BuildEc2Key((CoseAlgorithmIdentifier)(-70000));

            CoseKey key = CoseKey.CreateOrUnsupported(encodedKey);

            var unsupported = Assert.IsType<CoseUnsupportedPublicKey>(key);
            Assert.Equal(CoseKeyType.Ec2, unsupported.Type);
            Assert.Equal((CoseAlgorithmIdentifier)(-70000), unsupported.Algorithm);
            Assert.Equal(encodedKey, unsupported.EncodedKey.ToArray());
        }

        [Fact]
        public void CreateOrUnsupported_UnmodeledStructuredKey_ReturnsUnsupportedKeyWithReportedTypeAndAlgorithm()
        {
            // A key whose type and algorithm are both outside the set this SDK
            // models, and whose value is itself structured rather than a flat
            // point. Mirrors the shape of a real extension-defined key.
            byte[] encodedKey = BuildUnmodeledStructuredKey();

            CoseKey key = CoseKey.CreateOrUnsupported(encodedKey);

            var unsupported = Assert.IsType<CoseUnsupportedPublicKey>(key);
            Assert.Equal(UnmodeledKeyType, (int)unsupported.Type);
            Assert.Equal(UnmodeledAlgorithm, (int)unsupported.Algorithm);
            Assert.Equal(encodedKey, unsupported.EncodedKey.ToArray());
        }

        [Fact]
        public void CreateOrUnsupported_UnsupportedAlgorithm_EncodeRoundTripsOriginalBytes()
        {
            byte[] encodedKey = BuildUnmodeledStructuredKey();

            CoseKey key = CoseKey.CreateOrUnsupported(encodedKey);

            Assert.Equal(encodedKey, key.Encode());
        }

        [Fact]
        public void CreateOrUnsupported_UnsupportedAlgorithm_EncodedKeyIsDefensiveCopy()
        {
            byte[] encodedKey = BuildEc2Key((CoseAlgorithmIdentifier)(-70000));
            byte[] original = encodedKey.ToArray();

            var key = Assert.IsType<CoseUnsupportedPublicKey>(CoseKey.CreateOrUnsupported(encodedKey));
            encodedKey[0] = 0xFF;

            Assert.Equal(original, key.EncodedKey.ToArray());
        }

        [Fact]
        public void CreateOrUnsupported_UnsupportedAlgorithmNoKeyType_ThrowsCtap2DataException()
        {
            // COSE requires the key type in every key, so its absence is
            // malformed data rather than an unrecognized algorithm. Create
            // rejects this for modeled algorithms; CreateOrUnsupported must
            // reject it for unmodeled ones too.
            byte[] encodedKey = BuildAlgorithmOnlyKey((CoseAlgorithmIdentifier)(-70000));

            _ = Assert.Throws<Ctap2DataException>(() => CoseKey.CreateOrUnsupported(encodedKey));
        }

        [Fact]
        public void CreateOrUnsupported_IndefiniteLengthMap_ThrowsSameAsCreate()
        {
            // An indefinite-length map is malformed under Ctap2Canonical, not an
            // unrecognized algorithm, so decoding it must fail identically
            // whether the caller went through Create or CreateOrUnsupported.
            byte[] encodedKey = BuildIndefiniteLengthMap();

            _ = Assert.Throws<CborContentException>(() => CoseKey.Create(encodedKey, out _));
            _ = Assert.Throws<CborContentException>(() => CoseKey.CreateOrUnsupported(encodedKey));
        }

        [Fact]
        public void CreateOrUnsupported_NotAMap_ThrowsCtap2DataException()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteInt32(7);

            _ = Assert.Throws<Ctap2DataException>(() => CoseKey.CreateOrUnsupported(cbor.Encode()));
        }

        [Fact]
        public void CreateOrUnsupported_Es256WithOkpKeyType_ThrowsCtap2DataException()
        {
            // A modeled algorithm paired with the wrong key type is corrupt
            // data, not a future algorithm, so it must not be tolerated.
            byte[] encodedKey = BuildOkpKey(CoseAlgorithmIdentifier.ES256);

            _ = Assert.Throws<Ctap2DataException>(() => CoseKey.CreateOrUnsupported(encodedKey));
        }

        [Fact]
        public void CreateOrUnsupported_MissingAlgorithm_ThrowsCtap2DataException()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(1);
            cbor.WriteInt32(1);
            cbor.WriteInt32((int)CoseKeyType.Ec2);
            cbor.WriteEndMap();

            _ = Assert.Throws<Ctap2DataException>(() => CoseKey.CreateOrUnsupported(cbor.Encode()));
        }

        // Taken from a real extension-defined key type rather than invented, so
        // the fixtures exercise a shape a YubiKey can actually return. The
        // specific values do not matter; what matters is that they are outside
        // the set the SDK models.
        private const int UnmodeledKeyType = -65537;
        private const int UnmodeledAlgorithm = -65700;

        private static byte[] BuildIndefiniteLengthMap()
        {
            // System.Formats.Cbor will not emit an indefinite-length map in
            // Ctap2Canonical mode, so write the encoding directly:
            // 0xBF <start indefinite map> 01 02 <kty: Ec2> 03 39 01 0F <alg: -272> 0xFF <break>
            return new byte[] { 0xBF, 0x01, 0x02, 0x03, 0x39, 0x01, 0x0F, 0xFF };
        }

        private static byte[] BuildAlgorithmOnlyKey(CoseAlgorithmIdentifier algorithm)
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(1);
            cbor.WriteInt32(3);
            cbor.WriteInt32((int)algorithm);
            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static byte[] BuildUnmodeledStructuredKey()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(4);
            cbor.WriteInt32(1);
            cbor.WriteInt32(UnmodeledKeyType);
            cbor.WriteInt32(3);
            cbor.WriteInt32(UnmodeledAlgorithm);
            cbor.WriteInt32(-1);
            WriteEc2Point(cbor, 0x44);
            cbor.WriteInt32(-2);
            WriteEc2Point(cbor, 0x55);
            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static void WriteEc2Point(CborWriter cbor, byte fill)
        {
            cbor.WriteStartMap(5);
            cbor.WriteInt32(1);
            cbor.WriteInt32((int)CoseKeyType.Ec2);
            cbor.WriteInt32(3);
            cbor.WriteInt32((int)CoseAlgorithmIdentifier.ES256);
            cbor.WriteInt32(-1);
            cbor.WriteInt32((int)CoseEcCurve.P256);
            cbor.WriteInt32(-2);
            cbor.WriteByteString(Enumerable.Repeat(fill, 32).ToArray());
            cbor.WriteInt32(-3);
            cbor.WriteByteString(Enumerable.Repeat((byte)(fill + 1), 32).ToArray());
            cbor.WriteEndMap();
        }

        private static byte[] BuildOkpKey(CoseAlgorithmIdentifier algorithm)
        {
            byte[] publicKey = Enumerable.Repeat((byte)0x33, 32).ToArray();

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(4);
            cbor.WriteInt32(1);
            cbor.WriteInt32((int)CoseKeyType.Okp);
            cbor.WriteInt32(3);
            cbor.WriteInt32((int)algorithm);
            cbor.WriteInt32(-1);
            cbor.WriteInt32((int)CoseEcCurve.Ed25519);
            cbor.WriteInt32(-2);
            cbor.WriteByteString(publicKey);
            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static byte[] BuildEc2Key(CoseAlgorithmIdentifier algorithm)
        {
            byte[] x = Enumerable.Repeat((byte)0x11, 32).ToArray();
            byte[] y = Enumerable.Repeat((byte)0x22, 32).ToArray();

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(5);
            cbor.WriteInt32(1);
            cbor.WriteInt32((int)CoseKeyType.Ec2);
            cbor.WriteInt32(3);
            cbor.WriteInt32((int)algorithm);
            cbor.WriteInt32(-1);
            cbor.WriteInt32((int)CoseEcCurve.P256);
            cbor.WriteInt32(-2);
            cbor.WriteByteString(x);
            cbor.WriteInt32(-3);
            cbor.WriteByteString(y);
            cbor.WriteEndMap();
            return cbor.Encode();
        }
    }
}
