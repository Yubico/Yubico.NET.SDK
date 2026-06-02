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
