// Copyright 2021 Yubico AB
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

using System;
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Yubico.Core.Buffers;

namespace Yubico.YubiKey.Piv
{
    public class PivPrivateKeyTests
    {
        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        public void Create_ReturnsPivPrivateKey(PivAlgorithm algorithm)
        {
            ReadOnlyMemory<byte> keyData = GetKeyData(algorithm);

            var keyObject = PivPrivateKey.Create(keyData);

            Assert.True(keyObject is PivPrivateKey);

            keyObject.Clear();
            keyObject.Clear();
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        public void Create_SetsAlgorithmCorrectly(PivAlgorithm algorithm)
        {
            ReadOnlyMemory<byte> keyData = GetKeyData(algorithm);

            var keyObject = PivPrivateKey.Create(keyData);

            Assert.Equal(algorithm, keyObject.Algorithm);

            keyObject.Clear();
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        public void Create_SetsEncodedCorrectly(PivAlgorithm algorithm)
        {
            ReadOnlyMemory<byte> keyData = GetKeyData(algorithm);
            ReadOnlyMemory<byte> encoding = GetCorrectEncoding(algorithm);

            var keyObject = PivPrivateKey.Create(keyData);

            ReadOnlyMemory<byte> getKeyData = keyObject.EncodedPrivateKey;

            bool compareResult = encoding.Span.SequenceEqual(getKeyData.Span);

            Assert.True(compareResult);

            keyObject.Clear();
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        public void Create_SetsPrimePCorrectly(PivAlgorithm algorithm)
        {
            ReadOnlyMemory<byte> keyData = GetKeyData(algorithm);
            ReadOnlyMemory<byte> primeP = GetRsaComponent(algorithm, 1);

            var keyObject = PivPrivateKey.Create(keyData);

            Assert.True(keyObject is PivRsaPrivateKey);

            var rsaObject = (PivRsaPrivateKey)keyObject;
            ReadOnlySpan<byte> getPrimeP = rsaObject.PrimeP;

            bool compareResult = primeP.Span.SequenceEqual(getPrimeP);

            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        public void Create_SetsPrimeQCorrectly(PivAlgorithm algorithm)
        {
            ReadOnlyMemory<byte> keyData = GetKeyData(algorithm);
            ReadOnlyMemory<byte> primeQ = GetRsaComponent(algorithm, 2);

            var keyObject = PivPrivateKey.Create(keyData);

            Assert.True(keyObject is PivRsaPrivateKey);

            var rsaObject = (PivRsaPrivateKey)keyObject;
            ReadOnlySpan<byte> getPrimeQ = rsaObject.PrimeQ;

            bool compareResult = primeQ.Span.SequenceEqual(getPrimeQ);

            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        public void Create_SetsExponentPCorrectly(PivAlgorithm algorithm)
        {
            ReadOnlyMemory<byte> keyData = GetKeyData(algorithm);
            ReadOnlyMemory<byte> exponentP = GetRsaComponent(algorithm, 3);

            var keyObject = PivPrivateKey.Create(keyData);

            Assert.True(keyObject is PivRsaPrivateKey);

            var rsaObject = (PivRsaPrivateKey)keyObject;
            ReadOnlySpan<byte> getExponentP = rsaObject.ExponentP;

            bool compareResult = exponentP.Span.SequenceEqual(getExponentP);

            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        public void Create_SetsExponentQCorrectly(PivAlgorithm algorithm)
        {
            ReadOnlyMemory<byte> keyData = GetKeyData(algorithm);
            ReadOnlyMemory<byte> exponentQ = GetRsaComponent(algorithm, 4);

            var keyObject = PivPrivateKey.Create(keyData);

            Assert.True(keyObject is PivRsaPrivateKey);

            var rsaObject = (PivRsaPrivateKey)keyObject;
            ReadOnlySpan<byte> getExponentQ = rsaObject.ExponentQ;

            bool compareResult = exponentQ.Span.SequenceEqual(getExponentQ);

            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        public void Create_SetsCoefficientCorrectly(PivAlgorithm algorithm)
        {
            ReadOnlyMemory<byte> keyData = GetKeyData(algorithm);
            ReadOnlyMemory<byte> coefficient = GetRsaComponent(algorithm, 5);

            var keyObject = PivPrivateKey.Create(keyData);

            Assert.True(keyObject is PivRsaPrivateKey);

            var rsaObject = (PivRsaPrivateKey)keyObject;
            ReadOnlySpan<byte> getCoefficient = rsaObject.Coefficient;

            bool compareResult = coefficient.Span.SequenceEqual(getCoefficient);

            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        public void CreateEcc_SetsPrivateValueCorrectly(PivAlgorithm algorithm)
        {
            ReadOnlyMemory<byte> keyData = GetKeyData(algorithm);
            ReadOnlyMemory<byte> privateValue = GetPrivateValue(algorithm);

            var keyObject = PivPrivateKey.Create(keyData);

            Assert.True(keyObject is PivEccPrivateKey);

            var eccObject = (PivEccPrivateKey)keyObject;

            ReadOnlySpan<byte> getPrivateValue = eccObject.PrivateValue;

            bool compareResult = privateValue.Span.SequenceEqual(getPrivateValue);

            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        public void RsaConstructor_Components_BuildsEncoding(PivAlgorithm algorithm)
        {
            ReadOnlyMemory<byte> primeP = GetRsaComponent(algorithm, 1);
            ReadOnlyMemory<byte> primeQ = GetRsaComponent(algorithm, 2);
            ReadOnlyMemory<byte> exponentP = GetRsaComponent(algorithm, 3);
            ReadOnlyMemory<byte> exponentQ = GetRsaComponent(algorithm, 4);
            ReadOnlyMemory<byte> coefficient = GetRsaComponent(algorithm, 5);
            ReadOnlyMemory<byte> encoding = GetCorrectEncoding(algorithm);

            var rsaPrivate = new PivRsaPrivateKey(
                primeP.Span,
                primeQ.Span,
                exponentP.Span,
                exponentQ.Span,
                coefficient.Span);

            ReadOnlyMemory<byte> getEncoding = rsaPrivate.EncodedPrivateKey;

            bool compareResult = encoding.Span.SequenceEqual(getEncoding.Span);

            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        public void EccConstructor_Components_BuildsEncoding(PivAlgorithm algorithm)
        {
            ReadOnlyMemory<byte> privateValue = GetPrivateValue(algorithm);
            ReadOnlyMemory<byte> encoding = GetCorrectEncoding(algorithm);

            var eccPrivate = new PivEccPrivateKey(privateValue.Span);

            ReadOnlyMemory<byte> getEncoding = eccPrivate.EncodedPrivateKey;

            bool compareResult = encoding.Span.SequenceEqual(getEncoding.Span);

            Assert.True(compareResult);
        }

        [Fact]
        public void Create_NullData_ThrowsExcpetion()
        {
            _ = Assert.Throws<ArgumentException>(() => PivPrivateKey.Create(null));
        }

        [Fact]
        public void Create_BadTag_ThrowsExcpetion()
        {
            Memory<byte> keyData = GetKeyData(PivAlgorithm.EccP256);
            keyData.Span[0] = 0x84;
            _ = Assert.Throws<ArgumentException>(() => PivPrivateKey.Create(keyData));
        }

        [Fact]
        public void RsaConstructor_NullData_ThrowsExcpetion()
        {
            _ = Assert.Throws<ArgumentException>(() => PivRsaPrivateKey.CreateRsaPrivateKey(null));
        }

        [Fact]
        public void RsaConstructor_NoPrimeP_ThrowsExcpetion()
        {
            Memory<byte> keyData = GetKeyData(PivAlgorithm.Rsa1024);
            Memory<byte> badData = keyData.Slice(66);
            _ = Assert.Throws<ArgumentException>(() => PivRsaPrivateKey.CreateRsaPrivateKey(badData));
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        public void RsaConstructor_BadPrimeQ_ThrowsExcpetion(PivAlgorithm algorithm)
        {
            ReadOnlyMemory<byte> primeP = GetRsaComponent(algorithm, 1);
            ReadOnlyMemory<byte> primeQ = GetRsaComponent(algorithm, 2);
            ReadOnlyMemory<byte> exponentP = GetRsaComponent(algorithm, 3);
            ReadOnlyMemory<byte> exponentQ = GetRsaComponent(algorithm, 4);
            ReadOnlyMemory<byte> coefficient = GetRsaComponent(algorithm, 5);

            primeQ = primeQ.Slice(1);

            _ = Assert.Throws<ArgumentException>(() => new PivRsaPrivateKey(
                primeP.Span, primeQ.Span, exponentP.Span, exponentQ.Span, coefficient.Span));
        }

        [Fact]
        public void EccConstructor_NullData_ThrowsExcpetion()
        {
            _ = Assert.Throws<ArgumentException>(() => new PivEccPrivateKey(null));
        }

        [Theory]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        public void EccConstructor_BadPoint_ThrowsExcpetion(PivAlgorithm algorithm)
        {
            Memory<byte> privateValue = GetCorrectEncoding(algorithm);
            privateValue = privateValue.Slice(3);
            _ = Assert.Throws<ArgumentException>(() => new PivEccPublicKey(privateValue.Span));
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        public void GetPivPrivateKey_FromPem(PivAlgorithm algorithm)
        {
            PivPrivateKey privateKey;

            bool isValid = GetPemKey(algorithm, out string pemKey);
            Assert.True(isValid);

            string b64EncodedKey = pemKey
                .Replace("-----BEGIN PRIVATE KEY-----\n", null)
                .Replace("\n-----END PRIVATE KEY-----", null);

            byte[] encodedKey = Convert.FromBase64String(b64EncodedKey);

            int offset = ReadTagLen(encodedKey, 0, false);
            offset = ReadTagLen(encodedKey, offset, true);
            offset = ReadTagLen(encodedKey, offset, false);
            offset = ReadTagLen(encodedKey, offset, false);

            // encodedKey[offset] is where the OID begins.
            //   RSA: 2A 86 48 86 F7 0D 01 01 01
            //   ECC: 2A 86 48 CE 3D 02 01
            // For this sample code, we'll look at oid[3], if it's 86, RSA,
            // otherwise ECC. If it's something else, we'll get an exception.
            if (encodedKey[offset + 3] == 0x86)
            {
                var rsaObject = RSA.Create();
                rsaObject.ImportPkcs8PrivateKey(encodedKey, out _);

                // We need to get the private key elements. Those can be
                // found in the RSAParameters class.
                RSAParameters rsaParams = rsaObject.ExportParameters(true);

                var rsaPriKey = new PivRsaPrivateKey(
                    rsaParams.P,
                    rsaParams.Q,
                    rsaParams.DP,
                    rsaParams.DQ,
                    rsaParams.InverseQ);
                privateKey = (PivPrivateKey)rsaPriKey;
            }
            else
            {
                var eccObject = ECDsa.Create();
                eccObject.ImportPkcs8PrivateKey(encodedKey, out _);
                // The KeySize gives the bit size, we want the byte size.
                int keySize = eccObject.KeySize / 8;

                // We need to build the private value and it must be exactly the
                // keySize.
                ECParameters eccParams = eccObject.ExportParameters(true);
                byte[] privateValue = new byte[keySize];
                offset = keySize - eccParams.D!.Length;
                Array.Copy(eccParams.D, 0, privateValue, offset, eccParams.D.Length);

                var eccPriKey = new PivEccPrivateKey(privateValue);
                privateKey = (PivPrivateKey)eccPriKey;
            }

            Assert.Equal(algorithm, privateKey.Algorithm);
        }

        // Read the tag in the buffer at the given offset. Then read the length
        // octet(s). If the readValue argument is false, return the offset into
        // the buffer where the value begins. If the readValue argument is true,
        // skip the value (that will be length octets) and return the offset into
        // the buffer where the next TLV begins.
        // If the length octets are invalid, return -1.
        private static int ReadTagLen(byte[] buffer, int offset, bool readValue)
        {
            // Make sure there are enough bytes to read.
            if (offset < 0 || buffer.Length < offset + 2)
            {
                return -1;
            }

            // Skip the tag, look at the first length octet.
            // If the length is 0x7F or less, the length is one octet.
            // If the length octet is 0x80, that's BER and we shouldn't see it.
            // Otherwise the length octet should be 81, 82, or 83 (technically it
            // could be 84 or higher, but this method does not support anything
            // beyond 83). This says the length is the next 1, 2, or 3 octets.
            int length = buffer[offset + 1];
            int increment = 2;
            if (length == 0x80 || length > 0x83)
            {
                return -1;
            }

            if (length > 0x80)
            {
                int count = length & 0xf;
                if (buffer.Length < offset + increment + count)
                {
                    return -1;
                }

                increment += count;
                length = 0;
                while (count > 0)
                {
                    length <<= 8;
                    length += (int)buffer[offset + increment - count] & 0xFF;
                    count--;
                }
            }

            if (readValue)
            {
                if (buffer.Length < offset + increment + length)
                {
                    return -1;
                }

                increment += length;
            }

            return offset + increment;
        }

        private static Memory<byte> GetPrivateValue(PivAlgorithm algorithm)
        {
            if (!algorithm.IsEcc())
            {
                return Memory<byte>.Empty;
            }

            Memory<byte> keyData = GetCorrectEncoding(algorithm);
            Memory<byte> privateValue = keyData.Slice(2);
            return privateValue;
        }

        // if tag is 01, get the primeP, if 02, get primeQ, and so on.
        private static Memory<byte> GetRsaComponent(PivAlgorithm algorithm, int tag) //TODO fix unit tests
        {
            int start = 2;
            int count = 64;
            if (algorithm != PivAlgorithm.Rsa1024)
            {
                if (algorithm != PivAlgorithm.Rsa2048)
                {
                    return Memory<byte>.Empty;
                }

                start = 3;
                count = 128;
            }

            if (tag <= 0 || tag > 5)
            {
                return Memory<byte>.Empty;
            }

            start += (start + count) * (tag - 1);

            Memory<byte> encoding = GetCorrectEncoding(algorithm);
            return encoding.Slice(start, count);
        }

        private static Memory<byte> GetCorrectEncoding(PivAlgorithm algorithm) => algorithm switch
        {
            PivAlgorithm.Rsa2048 => new Memory<byte>(new byte[]
            {
                0x01, 0x81, 0x80,
                0xBE, 0xA1, 0x5B, 0x22, 0xFD, 0xA3, 0x6A, 0x10, 0x02, 0x72, 0xA2, 0xF8, 0x24, 0xC7, 0xA1, 0x92,
                0x03, 0x97, 0x53, 0xFF, 0x75, 0x36, 0xC2, 0x0D, 0xF9, 0x48, 0xAD, 0x3E, 0xF0, 0xF0, 0x89, 0x5D,
                0xA9, 0xD7, 0x34, 0xE3, 0x9B, 0x70, 0x4C, 0x02, 0xA7, 0xAC, 0x72, 0x9E, 0x76, 0x16, 0x31, 0xAE,
                0xF7, 0xFC, 0xD8, 0x50, 0xDF, 0x9B, 0x4A, 0xCA, 0x9A, 0x82, 0xE7, 0xB8, 0x83, 0x0A, 0xA3, 0x59,
                0x80, 0xF4, 0x86, 0xBE, 0x3F, 0xA9, 0xED, 0xFA, 0xE1, 0x5A, 0xA8, 0xD4, 0x1A, 0x6A, 0x19, 0xFB,
                0x2F, 0x60, 0x45, 0x3B, 0x07, 0xEF, 0x7B, 0x32, 0x72, 0x16, 0xFC, 0xBA, 0xBA, 0x0E, 0x26, 0xB2,
                0xB6, 0x8A, 0x4E, 0x77, 0xF4, 0x94, 0x6E, 0x7D, 0xD4, 0x2D, 0x7C, 0x2B, 0x5C, 0xEC, 0xBA, 0xBC,
                0xD7, 0xF9, 0xE1, 0xFC, 0x80, 0x50, 0xB9, 0x44, 0xE7, 0xC8, 0xEC, 0x4C, 0xEB, 0xA5, 0x8D, 0x09,
                0x02, 0x81, 0x80,
                0xCB, 0x28, 0x4D, 0x6C, 0xFE, 0xDB, 0x36, 0xDF, 0x73, 0x00, 0xB1, 0xC7, 0x9D, 0x34, 0x89, 0x61,
                0x19, 0x45, 0x4C, 0x30, 0xD8, 0xE0, 0x30, 0xFA, 0x06, 0xF6, 0x89, 0x19, 0x42, 0xB8, 0x60, 0x71,
                0xA6, 0x36, 0x52, 0x99, 0x2D, 0x43, 0x57, 0x3D, 0xA0, 0x89, 0x13, 0x99, 0xE7, 0xFF, 0x4C, 0xD8,
                0xE5, 0x05, 0xA7, 0x5C, 0x67, 0xD1, 0x4A, 0x7E, 0xB3, 0x26, 0x61, 0xE7, 0x3C, 0xF5, 0x72, 0x22,
                0x1B, 0xC8, 0x0D, 0x9B, 0x9B, 0x19, 0xB5, 0x81, 0x5E, 0x9F, 0x01, 0x7F, 0xCF, 0x2A, 0x82, 0xE0,
                0x02, 0x31, 0xF1, 0x8C, 0x0E, 0x9F, 0xC8, 0x1A, 0x8F, 0x5B, 0x6F, 0x8C, 0x29, 0x42, 0xDD, 0x17,
                0xC3, 0xD7, 0x34, 0x61, 0x35, 0x8C, 0x96, 0xF4, 0x6F, 0x53, 0x00, 0x22, 0x3E, 0x1F, 0x49, 0x9C,
                0x11, 0x07, 0xF9, 0xD2, 0xB7, 0xDF, 0x1F, 0x3A, 0x54, 0xC6, 0xCD, 0xB0, 0x1C, 0x07, 0x3D, 0xA5,
                0x03, 0x81, 0x80,
                0xF2, 0xCC, 0xF0, 0x54, 0xD1, 0x9C, 0x65, 0x3B, 0x9D, 0x67, 0x72, 0x9D, 0x45, 0x68, 0xEE, 0x30,
                0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x0B, 0x05, 0x00, 0x30, 0x21,
                0x31, 0x1F, 0x30, 0x1D, 0x06, 0x03, 0x55, 0x04, 0x03, 0x0C, 0x16, 0x59, 0x75, 0x62, 0x69, 0x63,
                0x6F, 0x20, 0x50, 0x49, 0x56, 0x20, 0x41, 0x74, 0x74, 0x65, 0x73, 0x74, 0x61, 0x74, 0x69, 0x6F,
                0x6E, 0x30, 0x1E, 0x17, 0x0D, 0x31, 0x39, 0x30, 0x32, 0x31, 0x38, 0x31, 0x32, 0x33, 0x32, 0x32,
                0x32, 0x5A, 0x17, 0x0D, 0x32, 0x30, 0x30, 0x32, 0x31, 0x38, 0x31, 0x32, 0x33, 0x32, 0x32, 0x32,
                0x5A, 0x30, 0x25, 0x31, 0x23, 0x30, 0x21, 0x06, 0x03, 0x55, 0x04, 0x03, 0x0C, 0x1A, 0x59, 0x75,
                0x62, 0x69, 0x4B, 0x65, 0x79, 0x20, 0x50, 0x49, 0x56, 0x20, 0x41, 0x74, 0x74, 0x65, 0x73, 0x74,
                0x04, 0x81, 0x80,
                0x30, 0x82, 0x01, 0x0A, 0x02, 0x82, 0x01, 0x01, 0x00, 0xA5, 0x57, 0x2D, 0xFF, 0x51, 0x21, 0x1D,
                0x9D, 0xBC, 0x39, 0x58, 0x31, 0x1B, 0xCF, 0xDC, 0x9D, 0xD3, 0x84, 0x35, 0x39, 0x30, 0xC8, 0x50,
                0x0C, 0x5A, 0x21, 0xB8, 0x64, 0xE0, 0x92, 0x7F, 0xA3, 0xDB, 0xB3, 0x15, 0xEC, 0x8E, 0x54, 0xA4,
                0xA6, 0xE7, 0x79, 0x6B, 0x63, 0xAB, 0x70, 0xBB, 0xA7, 0x73, 0xCF, 0x50, 0xCE, 0x86, 0xCD, 0x49,
                0x36, 0x07, 0x75, 0x11, 0x2C, 0x39, 0x24, 0x6B, 0xF1, 0x8B, 0x4A, 0x60, 0x7A, 0x96, 0xB6, 0x6B,
                0x8E, 0xA3, 0x5A, 0x5B, 0x0B, 0xB5, 0xF3, 0x30, 0xF0, 0xFE, 0xBA, 0xB3, 0xBA, 0xD2, 0x31, 0x18,
                0x33, 0x7C, 0xB0, 0x46, 0xA7, 0x71, 0x37, 0x06, 0x7F, 0x15, 0x98, 0x6B, 0x3C, 0x2D, 0x13, 0x39,
                0xB9, 0x62, 0xFD, 0x03, 0xED, 0x67, 0x5E, 0x80, 0x6F, 0x4F, 0xAB, 0x18, 0xBE, 0x1F, 0xD9, 0x09,
                0x05, 0x81, 0x80,
                0x8D, 0x09, 0xCB, 0x28, 0x4D, 0x6C, 0xFE, 0xDB, 0x36, 0xDF, 0x73, 0x00, 0xB1, 0xC7, 0x9D, 0x34,
                0x89, 0x61, 0x19, 0x45, 0x4C, 0x30, 0xD8, 0xE0, 0x30, 0xFA, 0x06, 0xF6, 0x89, 0x19, 0x42, 0xB8,
                0x60, 0x71, 0xA6, 0x36, 0x52, 0x99, 0x2D, 0x43, 0x57, 0x3D, 0xA0, 0x89, 0x13, 0x99, 0xE7, 0xFF,
                0x4C, 0xD8, 0xE5, 0x05, 0xA7, 0x5C, 0x67, 0xD1, 0x4A, 0x7E, 0xB3, 0x26, 0x61, 0xE7, 0x3C, 0xF5,
                0x72, 0x22, 0x1B, 0xC8, 0x0D, 0x9B, 0x9B, 0x19, 0xB5, 0x81, 0x5E, 0x9F, 0x01, 0x7F, 0xCF, 0x2A,
                0x82, 0xE0, 0x02, 0x31, 0xF1, 0x8C, 0x0E, 0x9F, 0xC8, 0x1A, 0x8F, 0x5B, 0x6F, 0x8C, 0x29, 0x42,
                0xDD, 0x17, 0xC3, 0xD7, 0x34, 0x61, 0x35, 0x8C, 0x96, 0xF4, 0x6F, 0x53, 0x00, 0x22, 0x3E, 0x1F,
                0x49, 0x9C, 0x11, 0x07, 0xF9, 0xD2, 0xB7, 0xDF, 0x1F, 0x3A, 0x54, 0xC6, 0xCD, 0xB0, 0x1C, 0x07
            }),
            _ => GetKeyData(algorithm),
        };

        private static Memory<byte> GetKeyData(PivAlgorithm algorithm) => algorithm switch
        {
            PivAlgorithm.Rsa1024 => new Memory<byte>(new byte[]
            {
                0x01, 0x40, 0x9E, 0x30, 0x1E, 0x17, 0x0D, 0x31, 0x39, 0x30, 0x32, 0x31, 0x38, 0x31, 0x32, 0x33,
                0x32, 0x32, 0x32, 0x5A, 0x17, 0x0D, 0x32, 0x30, 0x30, 0x32, 0x31, 0x38, 0x31, 0x32, 0x33, 0x32,
                0x32, 0x32, 0x5A, 0x30, 0x25, 0x31, 0x23, 0x30, 0x21, 0x06, 0x03, 0x55, 0x04, 0x03, 0x0C, 0x1A,
                0x59, 0x75, 0x62, 0x69, 0x4B, 0x65, 0x79, 0x20, 0x50, 0x49, 0x56, 0x20, 0x41, 0x74, 0x74, 0x65,
                0x73, 0x75, 0x02, 0x40, 0xA1, 0x74, 0x69, 0x6F, 0x6E, 0x20, 0x39, 0x64, 0x30, 0x82, 0x01, 0x22,
                0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00, 0x03,
                0x82, 0x01, 0x0F, 0x00, 0x30, 0x82, 0x01, 0x0A, 0x02, 0x82, 0x01, 0x01, 0x00, 0xA5, 0x57, 0x2D,
                0xFF, 0x51, 0x21, 0x1D, 0x9D, 0xBC, 0x39, 0x58, 0x31, 0x1B, 0xCF, 0xDC, 0x9D, 0xD3, 0x84, 0x35,
                0x39, 0x30, 0xC8, 0x51, 0x03, 0x40, 0x0C, 0x5A, 0x21, 0xB8, 0x64, 0xE0, 0x92, 0x7F, 0xA3, 0xDB,
                0xB3, 0x15, 0xEC, 0x8E, 0x54, 0xA4, 0xA6, 0xE7, 0x79, 0x6B, 0x63, 0xAB, 0x70, 0xBB, 0xA7, 0x73,
                0xCF, 0x50, 0xCE, 0x86, 0xCD, 0x49, 0x36, 0x07, 0x75, 0x11, 0x2C, 0x39, 0x24, 0x6B, 0xF1, 0x8B,
                0x4A, 0x60, 0x7A, 0x96, 0xB6, 0x6B, 0x8E, 0xA3, 0x5A, 0x5B, 0x0B, 0xB5, 0xF3, 0x30, 0xF0, 0xFE,
                0xBA, 0xB3, 0xBA, 0xD2, 0x31, 0x18, 0x04, 0x40, 0x33, 0x7C, 0xB0, 0x46, 0xA7, 0x71, 0x37, 0x06,
                0x7F, 0x15, 0x98, 0x6B, 0x3C, 0x2D, 0x13, 0x39, 0xB9, 0x62, 0xFD, 0x03, 0xED, 0x67, 0x5E, 0x80,
                0x6F, 0x4F, 0xAB, 0x18, 0xBE, 0x1F, 0xD9, 0x09, 0x12, 0x7B, 0x6A, 0x59, 0x14, 0x94, 0x13, 0x9A,
                0xDB, 0x41, 0x87, 0x82, 0x3C, 0x42, 0x3F, 0x93, 0xF4, 0x91, 0x55, 0x74, 0x15, 0x7F, 0xF5, 0x30,
                0xED, 0xB8, 0x2E, 0xEE, 0x8F, 0x00, 0x5A, 0xCD, 0x05, 0x40, 0xC1, 0x04, 0x99, 0xBC, 0xB0, 0x52,
                0x59, 0xFD, 0xB3, 0xBF, 0xE7, 0x36, 0x4E, 0xC6, 0x8D, 0xE5, 0xEC, 0x17, 0xD3, 0x03, 0x25, 0xBA,
                0xD1, 0x22, 0x01, 0x02, 0x1F, 0x8E, 0xEE, 0x70, 0xF2, 0x22, 0x1D, 0x9A, 0x2D, 0xC8, 0x9D, 0x03,
                0x49, 0x9A, 0x79, 0x97, 0x56, 0x74, 0x5A, 0x00, 0xFF, 0xED, 0x46, 0x69, 0x4C, 0xF2, 0xF6, 0x3B,
                0xB3, 0x25, 0x53, 0x70, 0xE9, 0x04, 0x1D, 0xA9, 0x9D, 0xFC
            }),
            PivAlgorithm.Rsa2048 => new Memory<byte>(new byte[]
            {
                0x02, 0x81, 0x80,
                0xCB, 0x28, 0x4D, 0x6C, 0xFE, 0xDB, 0x36, 0xDF, 0x73, 0x00, 0xB1, 0xC7, 0x9D, 0x34, 0x89, 0x61,
                0x19, 0x45, 0x4C, 0x30, 0xD8, 0xE0, 0x30, 0xFA, 0x06, 0xF6, 0x89, 0x19, 0x42, 0xB8, 0x60, 0x71,
                0xA6, 0x36, 0x52, 0x99, 0x2D, 0x43, 0x57, 0x3D, 0xA0, 0x89, 0x13, 0x99, 0xE7, 0xFF, 0x4C, 0xD8,
                0xE5, 0x05, 0xA7, 0x5C, 0x67, 0xD1, 0x4A, 0x7E, 0xB3, 0x26, 0x61, 0xE7, 0x3C, 0xF5, 0x72, 0x22,
                0x1B, 0xC8, 0x0D, 0x9B, 0x9B, 0x19, 0xB5, 0x81, 0x5E, 0x9F, 0x01, 0x7F, 0xCF, 0x2A, 0x82, 0xE0,
                0x02, 0x31, 0xF1, 0x8C, 0x0E, 0x9F, 0xC8, 0x1A, 0x8F, 0x5B, 0x6F, 0x8C, 0x29, 0x42, 0xDD, 0x17,
                0xC3, 0xD7, 0x34, 0x61, 0x35, 0x8C, 0x96, 0xF4, 0x6F, 0x53, 0x00, 0x22, 0x3E, 0x1F, 0x49, 0x9C,
                0x11, 0x07, 0xF9, 0xD2, 0xB7, 0xDF, 0x1F, 0x3A, 0x54, 0xC6, 0xCD, 0xB0, 0x1C, 0x07, 0x3D, 0xA5,
                0x01, 0x81, 0x80,
                0xBE, 0xA1, 0x5B, 0x22, 0xFD, 0xA3, 0x6A, 0x10, 0x02, 0x72, 0xA2, 0xF8, 0x24, 0xC7, 0xA1, 0x92,
                0x03, 0x97, 0x53, 0xFF, 0x75, 0x36, 0xC2, 0x0D, 0xF9, 0x48, 0xAD, 0x3E, 0xF0, 0xF0, 0x89, 0x5D,
                0xA9, 0xD7, 0x34, 0xE3, 0x9B, 0x70, 0x4C, 0x02, 0xA7, 0xAC, 0x72, 0x9E, 0x76, 0x16, 0x31, 0xAE,
                0xF7, 0xFC, 0xD8, 0x50, 0xDF, 0x9B, 0x4A, 0xCA, 0x9A, 0x82, 0xE7, 0xB8, 0x83, 0x0A, 0xA3, 0x59,
                0x80, 0xF4, 0x86, 0xBE, 0x3F, 0xA9, 0xED, 0xFA, 0xE1, 0x5A, 0xA8, 0xD4, 0x1A, 0x6A, 0x19, 0xFB,
                0x2F, 0x60, 0x45, 0x3B, 0x07, 0xEF, 0x7B, 0x32, 0x72, 0x16, 0xFC, 0xBA, 0xBA, 0x0E, 0x26, 0xB2,
                0xB6, 0x8A, 0x4E, 0x77, 0xF4, 0x94, 0x6E, 0x7D, 0xD4, 0x2D, 0x7C, 0x2B, 0x5C, 0xEC, 0xBA, 0xBC,
                0xD7, 0xF9, 0xE1, 0xFC, 0x80, 0x50, 0xB9, 0x44, 0xE7, 0xC8, 0xEC, 0x4C, 0xEB, 0xA5, 0x8D, 0x09,
                0x04, 0x81, 0x80,
                0x30, 0x82, 0x01, 0x0A, 0x02, 0x82, 0x01, 0x01, 0x00, 0xA5, 0x57, 0x2D, 0xFF, 0x51, 0x21, 0x1D,
                0x9D, 0xBC, 0x39, 0x58, 0x31, 0x1B, 0xCF, 0xDC, 0x9D, 0xD3, 0x84, 0x35, 0x39, 0x30, 0xC8, 0x50,
                0x0C, 0x5A, 0x21, 0xB8, 0x64, 0xE0, 0x92, 0x7F, 0xA3, 0xDB, 0xB3, 0x15, 0xEC, 0x8E, 0x54, 0xA4,
                0xA6, 0xE7, 0x79, 0x6B, 0x63, 0xAB, 0x70, 0xBB, 0xA7, 0x73, 0xCF, 0x50, 0xCE, 0x86, 0xCD, 0x49,
                0x36, 0x07, 0x75, 0x11, 0x2C, 0x39, 0x24, 0x6B, 0xF1, 0x8B, 0x4A, 0x60, 0x7A, 0x96, 0xB6, 0x6B,
                0x8E, 0xA3, 0x5A, 0x5B, 0x0B, 0xB5, 0xF3, 0x30, 0xF0, 0xFE, 0xBA, 0xB3, 0xBA, 0xD2, 0x31, 0x18,
                0x33, 0x7C, 0xB0, 0x46, 0xA7, 0x71, 0x37, 0x06, 0x7F, 0x15, 0x98, 0x6B, 0x3C, 0x2D, 0x13, 0x39,
                0xB9, 0x62, 0xFD, 0x03, 0xED, 0x67, 0x5E, 0x80, 0x6F, 0x4F, 0xAB, 0x18, 0xBE, 0x1F, 0xD9, 0x09,
                0x05, 0x81, 0x80,
                0x8D, 0x09, 0xCB, 0x28, 0x4D, 0x6C, 0xFE, 0xDB, 0x36, 0xDF, 0x73, 0x00, 0xB1, 0xC7, 0x9D, 0x34,
                0x89, 0x61, 0x19, 0x45, 0x4C, 0x30, 0xD8, 0xE0, 0x30, 0xFA, 0x06, 0xF6, 0x89, 0x19, 0x42, 0xB8,
                0x60, 0x71, 0xA6, 0x36, 0x52, 0x99, 0x2D, 0x43, 0x57, 0x3D, 0xA0, 0x89, 0x13, 0x99, 0xE7, 0xFF,
                0x4C, 0xD8, 0xE5, 0x05, 0xA7, 0x5C, 0x67, 0xD1, 0x4A, 0x7E, 0xB3, 0x26, 0x61, 0xE7, 0x3C, 0xF5,
                0x72, 0x22, 0x1B, 0xC8, 0x0D, 0x9B, 0x9B, 0x19, 0xB5, 0x81, 0x5E, 0x9F, 0x01, 0x7F, 0xCF, 0x2A,
                0x82, 0xE0, 0x02, 0x31, 0xF1, 0x8C, 0x0E, 0x9F, 0xC8, 0x1A, 0x8F, 0x5B, 0x6F, 0x8C, 0x29, 0x42,
                0xDD, 0x17, 0xC3, 0xD7, 0x34, 0x61, 0x35, 0x8C, 0x96, 0xF4, 0x6F, 0x53, 0x00, 0x22, 0x3E, 0x1F,
                0x49, 0x9C, 0x11, 0x07, 0xF9, 0xD2, 0xB7, 0xDF, 0x1F, 0x3A, 0x54, 0xC6, 0xCD, 0xB0, 0x1C, 0x07,
                0x03, 0x81, 0x80,
                0xF2, 0xCC, 0xF0, 0x54, 0xD1, 0x9C, 0x65, 0x3B, 0x9D, 0x67, 0x72, 0x9D, 0x45, 0x68, 0xEE, 0x30,
                0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x0B, 0x05, 0x00, 0x30, 0x21,
                0x31, 0x1F, 0x30, 0x1D, 0x06, 0x03, 0x55, 0x04, 0x03, 0x0C, 0x16, 0x59, 0x75, 0x62, 0x69, 0x63,
                0x6F, 0x20, 0x50, 0x49, 0x56, 0x20, 0x41, 0x74, 0x74, 0x65, 0x73, 0x74, 0x61, 0x74, 0x69, 0x6F,
                0x6E, 0x30, 0x1E, 0x17, 0x0D, 0x31, 0x39, 0x30, 0x32, 0x31, 0x38, 0x31, 0x32, 0x33, 0x32, 0x32,
                0x32, 0x5A, 0x17, 0x0D, 0x32, 0x30, 0x30, 0x32, 0x31, 0x38, 0x31, 0x32, 0x33, 0x32, 0x32, 0x32,
                0x5A, 0x30, 0x25, 0x31, 0x23, 0x30, 0x21, 0x06, 0x03, 0x55, 0x04, 0x03, 0x0C, 0x1A, 0x59, 0x75,
                0x62, 0x69, 0x4B, 0x65, 0x79, 0x20, 0x50, 0x49, 0x56, 0x20, 0x41, 0x74, 0x74, 0x65, 0x73, 0x74,
            }),
            PivAlgorithm.Rsa3072 => Base16.DecodeText(
                "308206fc020100300d06092a864886f70d0101010500048206e6308206e2020100028201810099260fba1fb1a492505ba3ffdecf4a38f0803c3ecd71297be2a3b2b6eba974e685c5ac5e655d185f80100933622c023e3e5f5920779e481b51933ca45a7bae259f48950123183b02bc3f7281cde46459c9ce62d6546afbe8e1052dd40754c07046ebb2f6445173e3db80c77ae0ef1bb0961536973516b0614a8cd9f40c4499ee5d7b152936e1041531d484003156a1fe6ed9bb60e53bf80f25cee403e7c6ec571fbd7fa567597a88eab68d50016764372328d5a5e8cc1cb4e5901072c499f1f43eefd5b61d1c47c19843ba26b842203f3be861f50c7b96ecb9b2532187fa3d48996bd2ee3bc52509c654dd45049450ff339703dccf925461771b2a56cd4213a2f574b5455c564a50f986a662969aff3ec95f68dcdc3b4aa1acfd67ec67f704ad5cbf4f15a2f9be6979b24ec22a7ab6992de0de4a20ca831bd6915d628b46721563c4c4e274c92c466f877fb8c0dbb03c227a860028b3dfb3a9fcc5990c2283438cdb6b4cbcf9cb4265f39081ee7f309fb51de587bcbff94169b48c82fe5915210203010001028201802db509d9934ef9de7f243298917f8d57dc1371a78eba18d6f407c63548b54d01e5e7deaf579246cd6dd39b635e07e36d7f4106c1256234840ebf2248ad069fad73d1fe42961e4bb25fcb91d9c2c0c8e07155eaf2abc43845c32ec0043961e6833bef697c8d5c3ff9bfcfb9f966fb85e8988a613e14a69e629314e191b03da3315c6df91d51572bd845847716f5a2b4fb524b225ce35d9805b1538382d4e06e35fc6f9a929b7b3d927276a44b3df805155da578ca28e60e254124537c6547cac993cb7ac269a8f30e0dbb144c7438bed47612deaf5c1861159d4fd950f6491563dc2b35fe86f954c5948b128f5c9702d064a8bd35f011228e774b854dd59e6d74e1946c1bd5bdc92ad9697d85840c75eecb2f83fa879d2deee6f15c38f201a06582f81fc1be19e1596498fad5952d921f3e5e0742e4365a2bc96fffd7f37d4959165831b085861e0db62ac93be6d97fb6f486008522a58b2e9f32593679c71d59ed1e24775ba4b38fd7742d52afcecbbbb45cc1dd18f802edd38a21d0f4c6ed970281c100d08b4ba45f44c22de7bc7560952737a6246fdde9469ee88c2b8e4449cd3c33ee03965f1ae748d300892370cbcd6af4e01db615937c641a7dcb66c79ad3e4fdaad46f409e71b3295e3a4e09cb736a35137addc609f41843116f9c093ac24cd95376c68e3f6fbb02dcfb83e3a1d43b67289f0ba5c7365611c60be9c7e2ef0a100864a50e7e6336a43346a20c5e91b100484e132ad99c37dedfe1b81055b95cc9b3db1a44f4c9bc33e4eddc03ee08b53165a1083681d5c5adc61b008477aaccfeeb0281c100bbffb69074ea752bb4c110e94910a58ea4578e604cc0ceadd9702a225b8143f1b8a77a4ec0b88e55bc60565793a01408519aefbaf92893b173f63cee429d7faddd9cdd668e944d071acef371ad8c073e03c9b7af9b4445ebe2acdab53442ae81c456f0a660a3ddc71047e1f0d890b07220f0b2a8a149ca217341039aac27967f42dd758e2a0d5b7deca6a931e91999886941d684d09995bffe926f16030995f948cc7badd72a5ca68c18dfbc5f125c8a2864433fe608afce2d1cdd221945f1230281c058f6231582827d6741c4f63976471d89256007453d180ad1c8becec8c0e15eb1b91c0b841987ca631f1d5c3fc4684cffd20cdbd567a9f857134ecbf57350eb1955b803d3d362ff51b0039c500af312a335b5a7869577481d0704843769ad88c3ff162296531e6ed14005fa340daa2d8e7992696cbaf42a6ed6a42addd6e4ef03f59327c4a8a42595ae1af0b5e2e6a3dd34591edd67b3b9c2bdb25c5d854e5cc8f9bd920eee83f78b4020ac187de475a709f3cbf4c4f1a7f8ab8a23f83c8768730281c06fa488bf3a9f2d5bfa28992960998127b752c39b4e9945639a77f09d9ca7a438bd06c02c5a687f264d0b0cdb4f30c614b69982fa0f12d8ba8df9d1ef502205fbb35a7f64731180b8d263c9d05d5685ca7f27606ce990ded11938bb5cd69f2ed0a34f59f403f9ec2f55ecca3163fa70be25efaab957a6e16181f73ef3b07e85f2273c2a9e753c9f73a580c7837b41179b199ede8cdaf00a2d0d39dabc40ab85a39766cf9fc9e23f492c736d128986f6eb98d709d4bd7fb51f844cdac97026c7ef0281c0243675aae4cd112309f89c37c55d61a72859f6b64ac24ba3e9a4e8dc3f1363832c61bc9b7557122e30c530b1655d2dec7a1147836f07f0a8dda835771c47bbce9c566431b561afba34385dd7c10141e870812aef73ccc87bab34c651ca86bf079ac272b0d0566ecd5bea76dc64bd56856c7675aaca793f3951b7db349eddaaf35cde236b381c48186128bfed97d2c89c661736337633c89c4b09edbd6283d95df7ed47e93fcdbf82a6e3b3530339dd00b4c681c5d1cad53b9bdafdb0c398bcd3"),
            PivAlgorithm.EccP256 => new Memory<byte>(new byte[]
            {
                0x06, 0x20,
                0x0C, 0x3B, 0x19, 0x42, 0x63, 0x20, 0x8C, 0xA1, 0x2F, 0xEE, 0x1C, 0xB4, 0xD8, 0x81, 0x96, 0x9F,
                0xD8, 0xC8, 0xD0, 0x8D, 0xD1, 0xBB, 0x66, 0x58, 0x00, 0x26, 0x7D, 0x05, 0x34, 0xA8, 0xA3, 0x30
            }),
            _ => new Memory<byte>(new byte[]
            {
                0x06, 0x30,
                0xF2, 0xCC, 0xF0, 0x54, 0xD1, 0x9C, 0x65, 0x3B, 0x9D, 0x67, 0x72, 0x9D, 0x45, 0x68, 0xEE, 0x30,
                0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x0B, 0x05, 0x00, 0x30, 0x21,
                0x31, 0x1F, 0x30, 0x1D, 0x06, 0x03, 0x55, 0x04, 0x03, 0x0C, 0x16, 0x59, 0x75, 0x62, 0x69, 0x63
            }),
        };

        private static bool GetPemKey(PivAlgorithm algorithm, out string pemKey)
        {
            switch (algorithm)
            {
                default:
                    pemKey = "no key";
                    return false;

                case PivAlgorithm.Rsa1024:
                    pemKey =
                        "-----BEGIN PRIVATE KEY-----\n" +
                        "MIICdwIBADANBgkqhkiG9w0BAQEFAASCAmEwggJdAgEAAoGBAOx0RJUAUd0+rhhv\n" +
                        "l/8QA0ArDaENqDk3fqRYgCPWi3eFMv9Mn6yh3mf04aXPS8qNz1Ey2HoHrM/TH7g2\n" +
                        "hubxZ+cnymC6+syJXmTljk0zLDfVnElYr6l+tbtE8D9IFKDfwXPXaqAM+RUaMug/\n" +
                        "BfkAUkW5Ne+rb2PwUE0nEnoYjV7VAgMBAAECgYAPqX/lcrj5c65qdfHWdkQQ2wkz\n" +
                        "EsmCyLc9wZLzTMG+L/d5y6SD9dDah/DuX7XAe/YwhbKrGpkKxwxB0nLLF1Bvcaq5\n" +
                        "blcRfKmNHkZOkBWyFbKK0Nd7+DieNel4m6KxRJ6xb8lff/cs8gkqfxOIwoOtndTH\n" +
                        "0TqwXPR64X/XEm18IQJBAP1Y0ZVefpKAnW3CRXE/rF4XvIUTQdtF1IPRtUkZauwC\n" +
                        "ovt4IS5dNpe+r+3Gj+nXLD7fyc7cCpT3Q9Ec8AaXP90CQQDu7imXolR3meonc4Wk\n" +
                        "EZHQDsBnrGXEx+wH2KA47wJVK53wAleiARJGac82xGdBvav+pcgYOdpKug5Zg5IT\n" +
                        "G6dZAkARjtxHm9rt0FgYyUQCy0To6IA6QNFpnvdRg3Eq9cYBQVWGVBcInZExBxgu\n" +
                        "RHqo3C7G1L+pxHo/RLvAfF7uNgFJAkEAiDCg7JnO482LtqkWiAqrvphp+6485AnA\n" +
                        "9Ef6K/mwrrOJ9wCeyu0paZFuV51j7gkbPK9qesSfNPEQtN1WKiYdIQJBAJTFlvoO\n" +
                        "vmu8tW7cxuuigNu/o2baQOKRkKGxa67uRgWCWVvU+pgAu3rto7DWM3/hhlfGKq3G\n" +
                        "2omeVTglQg63GrY=\n" +
                        "-----END PRIVATE KEY-----";
                    break;

                case PivAlgorithm.Rsa2048:
                    pemKey =
                        "-----BEGIN PRIVATE KEY-----\n" +
                        "MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQCgsyBkABsCTCzz\n" +
                        "gvOT74zX6043eJgOVuQ8btNLjeAzYPj2X0X8hxOOgDvYY+tKQp8HppnLRzTKRcmY\n" +
                        "zzktrGaDzQPgh3FSjoUnXVf5xRzqtKlpADQSo+gAkfJexrKDD33EVO4uThRv2/BW\n" +
                        "TFCilNycxBsS4MEwLwRx/xDDL1+oXe7oVc88SidcICtbtccHka7jtPdWERQN8sop\n" +
                        "8SdUwYqTykv6IYA72pQWH9nojHSgFgnAVrzs8eOZimXPMENvMedepJLnCEGvTdgp\n" +
                        "2A0GEdUhzDm31+ZfY9HFlJMytMlZALTvdhl1ttqfqeMq/cbAi5OlRMc0jBC/q5Hi\n" +
                        "D1jttRu3AgMBAAECggEBAJqkBXV1zIfnehJTX8ZqbSSS4U/sEpcp8rRdCaPZQXjv\n" +
                        "xmR/xj9+VMl6iRxw+skZVyPrpG/Dc/96LMeKEkHrdzM6JJL6g4iocWYyIyjOEEej\n" +
                        "1qqecX3GkMmLqKqflsUcMTCvcgzJQk1qXtsM0UPC8JFC/bKq6f1OIX75rs3FVs4T\n" +
                        "H/6O60RO0Miyicsa/SRuBxk/Oeu4/2gS3Mufdd1KhpyhxSPFVUebm8RUz+3IZMUt\n" +
                        "SBx3LUxb701NPuLE4Oluvd6YxzmWECKUdSOvImAFxnXHGpiP+BrrEa9g42SUa1ez\n" +
                        "mKIBSYqZ/Gt0wM+GcyZO6kr3kCsWYU35WWh4Y/QuSPECgYEA0MUXodWI4B/Ew/62\n" +
                        "KCKsSE+xaiu0OFyHBo3t2sZOpOlse4TBD0B0mWPkTLRclKHwCY/Cg/4XwhYxqB3W\n" +
                        "rNE+nBCVBCqqp+sZEVQ7YTp8tldNKc6yo0DfDNNmUGsr1O0p145AbUlqaoKMEyq3\n" +
                        "Uy80iCP2TpRpuwS4WDmWunpMpk0CgYEAxQ4MyQUAA3C5VzeRtB5Bq0vCEjcjMjcn\n" +
                        "0zxLg9cZePRQ7mKUnVJDMbL8PAhrQmgeZYiknVP4iTaoQr8Ycmxt1lpRKeky4eFZ\n" +
                        "XimMP0VVsErNc+IOStQCtXZWzz1JQet1Py9F8Z50VgwtMikU6OSWLg7N2HQSqiUQ\n" +
                        "Z9IL9aIB1BMCgYBICMmHsJtC4hNNoVSO8q/JX54SyTOtAtggPdalVymJo3UoBX1r\n" +
                        "2sygpKQAh3cuXdXqJq1yR7lA4dGOdYU+KhDVXq9cObCasfb7ULoQaVLgw6y/US+4\n" +
                        "Psj3rvWtp9z+4jo+wzmdu+g5CgR1FJce37nbg7UYFgOJYS6OWoiUnWBXPQKBgBLt\n" +
                        "eY7pewnZjwPwo38wlNA2U6raPvg40gt5NCuywpCarxdmwq2l1Cx268F8cYkMZTcN\n" +
                        "e/pcsXfElz7qChgbkCVRwZAMBUYrFiF0TjNZnpRzau6hnQvU93mkp0v6sAmz6ywp\n" +
                        "h0dhF/2X59N0nLyOEFrWMzGCXLSZIM1IILv0VsafAoGBALj/gb2csVTl5tGvQ6y6\n" +
                        "43f1QYq11A5L8iiMSdKNam3O85CctBr77cjkEY5qeleTc+7d2izIbtgxx+ORk1m/\n" +
                        "vFii9r6KA5tHxgTRPYUPIe+xVoEi9lKEEPumsHOzjRncP0XP2PI9FKXaT9D1pqEe\n" +
                        "O6opEHSjoEWFd3QfkcKb/ikH\n" +
                        "-----END PRIVATE KEY-----";
                    break;

                case PivAlgorithm.EccP256:
                    pemKey =
                        "-----BEGIN PRIVATE KEY-----\n" +
                        "MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgFWkUnZ613jEzqIKn\n" +
                        "85v4aUe1ZJ+84tWs8r+A1VIiYcShRANCAAS2aFqjMssMgcMGQkik9EjNPkU8iqO8\n" +
                        "zRrbzaa3icyRegDJuCsktYxzoPK+ewkqnz05rRi8wvWBsKxRWmFagLj1\n" +
                        "-----END PRIVATE KEY-----";
                    break;

                case PivAlgorithm.EccP384:
                    pemKey =
                        "-----BEGIN PRIVATE KEY-----\n" +
                        "MIG2AgEAMBAGByqGSM49AgEGBSuBBAAiBIGeMIGbAgEBBDCy4BfV3HHsGbmN1sL7\n" +
                        "N/17mwTs8CPWH6c5RVa3Pcdb4JE202zOqO9HcwTY+d8567ihZANiAATyaAPnTNrb\n" +
                        "Ro5CcHW+bwWcM35S1+Mph5rLd26Hm7P0itkK/WJ+9SH5lT+at4YblnFrmPvtiZZu\n" +
                        "S6li2mZQTvldYUxMIaeptLi+DP6khhde0WCl1DCSq7Kz17ksxOY2hKY=\n" +
                        "-----END PRIVATE KEY-----";
                    break;
            }

            return true;
        }
    }
}
