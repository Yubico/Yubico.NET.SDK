// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.YubiHsm;

/// <summary>
///     Algorithms supported by the YubiHSM Auth applet for credential storage.
/// </summary>
public enum HsmAuthAlgorithm : byte
{
    /// <summary>
    ///     AES-128 symmetric authentication. Stores a pair of 16-byte keys (K-ENC, K-MAC)
    ///     used for SCP03 session establishment with a YubiHSM 2.
    /// </summary>
    Aes128YubicoAuthentication = 38,

    /// <summary>
    ///     EC P256 asymmetric authentication (firmware 5.6.0+). Stores an ECDH private key
    ///     used for key agreement during session establishment.
    /// </summary>
    EcP256YubicoAuthentication = 39
}

/// <summary>
///     Extension methods for <see cref="HsmAuthAlgorithm" />.
/// </summary>
public static class HsmAuthAlgorithmExtensions
{
    extension(HsmAuthAlgorithm algorithm)
    {
        /// <summary>
        ///     Gets the key length in bytes for this algorithm.
        /// </summary>
        public int KeyLength => algorithm switch
        {
            HsmAuthAlgorithm.Aes128YubicoAuthentication => 16,
            HsmAuthAlgorithm.EcP256YubicoAuthentication => 32,
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unknown algorithm.")
        };

        /// <summary>
        ///     Gets the public key length in bytes for this algorithm, or <c>null</c> for symmetric algorithms.
        /// </summary>
        public int? PublicKeyLength => algorithm switch
        {
            HsmAuthAlgorithm.Aes128YubicoAuthentication => null,
            HsmAuthAlgorithm.EcP256YubicoAuthentication => 64,
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unknown algorithm.")
        };
    }
}
