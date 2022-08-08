// Copyright 2022 Yubico AB
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
using System.IO;
using System.Security.Cryptography;
using Yubico.Core;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class PinUvAuthProtocolOne : IPinUvAuthProtocol
    {
        private ECParameters _myKey;

        /// <summary>
        /// Always returns <see cref="PinUvAuthProtocol.ProtocolOne"/>.
        /// </summary>
        public PinUvAuthProtocol Protocol => PinUvAuthProtocol.ProtocolOne;

        /// <inheritdoc />
        public void Initialize()
        {
            IEcdhPrimitives ecdh = CryptographyProviders.EcdhPrimitivesCreator();
            _myKey = ecdh.GenerateKeyPair(ECCurve.NamedCurves.nistP256);
        }

        /// <inheritdoc />
        public (CoseKey coseKey, byte[] sharedSecret) Encapsulate(CosePublicEcKey peerCosePublicKey)
        {
            if (peerCosePublicKey is null)
            {
                throw new ArgumentNullException(nameof(peerCosePublicKey));
            }

            if (_myKey.D is null)
            {
                throw new InvalidOperationException("Missing private key.");
            }

            IEcdhPrimitives ecdh = CryptographyProviders.EcdhPrimitivesCreator();
            byte[] derivedValue = ecdh.ComputeSharedSecret(peerCosePublicKey.AsEcParameters(), _myKey.D);
            byte[] sharedSecret = Kdf(derivedValue);

            CryptographicOperations.ZeroMemory(derivedValue);

            return (new CosePublicEcKey(_myKey), sharedSecret);
        }

        /// <inheritdoc />
        public byte[] Encrypt(byte[] key, byte[] plaintext)
        {
            using Aes aes = CryptographyProviders.AesCreator();
            aes.Key = key;

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using var bwEncrypt = new BinaryWriter(csEncrypt);

            bwEncrypt.Write(plaintext);
            return msEncrypt.ToArray();
        }

        /// <inheritdoc />
        public byte[] Decrypt(byte[] key, byte[] ciphertext)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (ciphertext is null)
            {
                throw new ArgumentNullException(nameof(ciphertext));
            }

            using Aes aes = CryptographyProviders.AesCreator();
            aes.Key = key;

            if (ciphertext.Length % aes.BlockSize != 0)
            {
                throw new ArgumentException("The cipherText is not a multiple of the AES block size.");
            }

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(ciphertext);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var brDecrypt = new BinaryReader(csDecrypt);

            return brDecrypt.ReadBytes(ciphertext.Length);
        }

        /// <inheritdoc />
        public byte[] Authenticate(byte[] key, byte[] message)
        {
            using HMAC hmacSha256 = CryptographyProviders.HmacCreator("HMACSHA256");
            hmacSha256.Key = key;
            return hmacSha256.ComputeHash(message).AsMemory(0, 16).ToArray();
        }

        private static byte[] Kdf(byte[] buffer)
        {
            using SHA256 sha256 = CryptographyProviders.Sha256Creator();
            return sha256.ComputeHash(buffer);
        }
    }
}
