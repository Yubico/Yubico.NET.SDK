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

using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.TestUtilities
{
    /// <summary>
    /// Provides convenient static methods to access test keys and certificates.
    /// </summary>
    public static class TestKeys
    {
        /// <summary>
        /// Gets a private key for the specified curve.
        /// </summary>
        /// <param name="keyType">The key type</param>
        /// <param name="index">If there are multiple stored keys, this is the index of the key</param>
        /// <returns>TestKey instance representing the private key</returns>
        public static TestKey GetTestPrivateKey(
            KeyType keyType,
            int? index = null) =>
            TestKey.LoadPrivateKey(keyType, index);

        public static (TestKey testPublicKey, TestKey testPrivateKey) GetKeyPair(KeyType keyType, int index1 = 0, int index2 = 0) 
            => (GetTestPublicKey(keyType, index1), GetTestPrivateKey(keyType, index2));

        /// <summary>
        /// Gets a public key for the specified curve.
        /// </summary>
        /// <param name="keyType">The key type</param>
        /// <param name="index">If there are multiple stored keys, this is the index of the key</param>
        /// <returns>TestKey instance representing the public key</returns>
        public static TestKey GetTestPublicKey(
            KeyType keyType,
            int? index = null) =>
            TestKey.LoadPublicKey(keyType, index);

        /// <summary>
        /// Gets a certificate for the specified curve.
        /// </summary>
        /// <param name="curve">The curve or key type</param>
        /// <param name="isAttestation">True to get an attestation certificate</param>
        /// <returns>TestCertificate instance</returns>s
        public static TestCertificate GetTestCertificate(
            KeyType curve,
            bool isAttestation = false) =>
            TestCertificate.Load(curve, isAttestation);
    }
}
