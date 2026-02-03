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

using System;

namespace Yubico.YubiKey.YubiHsmAuth
{
    /// <summary>
    /// These session keys are used to establish an encrypted and authenticated
    /// session with a YubiHSM 2 device using ECC P-256 authentication. The secure 
    /// session is based on the Global Platform Secure Channel Protocol '11' (SCP11).
    /// </summary>
    /// <remarks>
    /// <para>
    /// These session keys are calculated from an ECC P-256 credential in the 
    /// YubiHSM Auth application. See
    /// <see cref="Commands.GetEccP256SessionKeysCommand"/> and
    /// <see cref="Commands.GetEccP256SessionKeysResponse"/> for more information
    /// on retrieving these values.
    /// </para>
    /// </remarks>
    public class Ecp256SessionKeys
    {
        /// <summary>
        /// Secure Channel command and response encryption session key.
        /// </summary>
        /// <remarks>
        /// Used for data confidentiality.
        /// </remarks>
        public ReadOnlyMemory<byte> EncryptionKey { get; }

        /// <summary>
        /// Secure Channel Message Authentication Code session key for
        /// commands.
        /// </summary>
        /// <remarks>
        /// This session key is used for data and protocol integrity in
        /// commands.
        /// </remarks>
        public ReadOnlyMemory<byte> MacKey { get; }

        /// <summary>
        /// Secure Channel Message Authentication Code session key for
        /// responses.
        /// </summary>
        /// <remarks>
        /// This session key is used for data and protocol integrity in
        /// responses.
        /// </remarks>
        public ReadOnlyMemory<byte> RmacKey { get; }

        /// <summary>
        /// The host cryptogram derived from the ECC P-256 authentication.
        /// </summary>
        /// <remarks>
        /// This is an 8-byte value that represents the host's cryptographic
        /// commitment during the ECC P-256 session establishment.
        /// </remarks>
        public ReadOnlyMemory<byte> HostCryptogram { get; }

        /// <summary>
        /// Construct a set of ECC P-256 session keys with the given values.
        /// </summary>
        /// <param name="encryptionKey">
        /// Sets <see cref="EncryptionKey"/>.
        /// </param>
        /// <param name="macKey">
        /// Sets <see cref="MacKey"/>.
        /// </param>
        /// <param name="rmacKey">
        /// Sets <see cref="RmacKey"/>.
        /// </param>
        /// <param name="hostCryptogram">
        /// Sets <see cref="HostCryptogram"/>.
        /// </param>
        public Ecp256SessionKeys(ReadOnlyMemory<byte> encryptionKey, ReadOnlyMemory<byte> macKey, 
            ReadOnlyMemory<byte> rmacKey, ReadOnlyMemory<byte> hostCryptogram)
        {
            EncryptionKey = encryptionKey;
            MacKey = macKey;
            RmacKey = rmacKey;
            HostCryptogram = hostCryptogram;
        }
    }
}
