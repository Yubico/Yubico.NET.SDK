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

using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2.Commands
{
    public interface IPinUvAuthProtocol
    {
        /// <summary>
        /// Gets the identifier of the PIN / UV authentication protocol that this instance implements.
        /// </summary>
        PinUvAuthProtocol Protocol { get; }

        /// <summary>
        /// This is run by the platform when starting a series of transactions with a specific authenticator.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Generates an encapsulation for the authenticator's public key and returns the message to transmit and the
        /// shared secret.
        /// </summary>
        (CosePublicEcKey coseKey, byte[] sharedSecret) Encapsulate(CosePublicEcKey peerCosePublicKey);

        /// <summary>
        /// Returns the AES-256-CBC encryption of plaintext using an all-zero IV.
        /// </summary>
        byte[] Encrypt(byte[] key, byte[] plaintext);

        /// <summary>
        /// Returns the AES-256-CBC decryption of the ciphertext using an all-zero IV.
        /// </summary>
        byte[] Decrypt(byte[] key, byte[] ciphertext);

        /// <summary>
        /// Returns the first 16 bytes of the result of computing HMAC-SHA-256 with the given key and message.
        /// </summary>
        byte[] Authenticate(byte[] key, byte[] message);
    }
}
