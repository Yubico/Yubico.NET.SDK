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
using System.Security.Cryptography;
using Yubico.Core;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2.PinProtocols
{
    /// <summary>
    /// Base class for FIDO2 PIN/UV auth protocol implementations.
    /// </summary>
    /// <remarks>
    /// A PIN/UV auth protocol is a set of methods defined by the FIDO2 CTAP specification. The abstract interface is
    /// defined in section 6.5.4. As of FIDO 2.1, there are only two PIN protocols defined: protocol one and protocol
    /// two. These two implementations share some common code. The abstract interface as well as any shared code is
    /// defined by this class.
    /// </remarks>
    public abstract class PinUvAuthProtocolBase
    {
        /// <summary>
        /// The local key-pair for the protocol. Valid once <see cref="Initialize"/> has been called.
        /// </summary>
        private ECParameters _myKey;

        /// <summary>
        /// Gets the identifier of the PIN / UV authentication protocol that this instance implements.
        /// </summary>
        public PinUvAuthProtocol Protocol { get; protected set; }

        /// <summary>
        /// This is run by the platform when starting a series of transactions with a specific authenticator.
        /// </summary>
        public void Initialize()
        {
            IEcdhPrimitives ecdh = CryptographyProviders.EcdhPrimitivesCreator();
            _myKey = ecdh.GenerateKeyPair(ECCurve.NamedCurves.nistP256);
        }

        /// <summary>
        /// Generates an encapsulation for the authenticator's public key and returns the message to transmit and the
        /// shared secret.
        /// </summary>
        public (CosePublicEcKey coseKey, byte[] sharedSecret) Encapsulate(CosePublicEcKey peerCosePublicKey)
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
            byte[] sharedValue = ecdh.ComputeSharedSecret(peerCosePublicKey.AsEcParameters(), _myKey.D);
            byte[] sharedSecret = DeriveKey(sharedValue);

            CryptographicOperations.ZeroMemory(sharedValue);

            return (new CosePublicEcKey(_myKey), sharedSecret);
        }

        /// <summary>
        /// Returns the AES-256-CBC encryption of plaintext using an all-zero IV.
        /// </summary>
        public abstract byte[] Encrypt(byte[] key, byte[] plaintext);

        /// <summary>
        /// Returns the AES-256-CBC decryption of the ciphertext using an all-zero IV.
        /// </summary>
        public abstract byte[] Decrypt(byte[] key, byte[] ciphertext);

        /// <summary>
        /// Returns the first 16 bytes of the result of computing HMAC-SHA-256 with the given key and message.
        /// </summary>
        public abstract byte[] Authenticate(byte[] key, byte[] message);

        /// <summary>
        /// The key derivation function to run while performing ECDH.
        /// </summary>
        /// <param name="buffer">
        /// The shared value computed by ECDH.
        /// </param>
        /// <returns>
        /// The shared secret.
        /// </returns>
        protected abstract byte[] DeriveKey(byte[] buffer);
    }
}
