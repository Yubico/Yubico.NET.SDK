// Copyright 2024 Yubico AB
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

namespace Yubico.YubiKey.Scp
{
    /// <summary>
    /// Represents the parameters for a Secure Channel Protocol 03 (SCP03) key.
    /// </summary>
    public sealed class Scp03KeyParameters : ScpKeyParameters, IDisposable
    {
        private const int DefaultKeyKvn = 0xFF;
        private const int DefaultKvn = 0x01;

        /// <summary>
        /// The static keys shared with the device when initiating the connection.
        /// </summary>        
        public StaticKeys StaticKeys { get; }

        /// <summary>
        /// Creates a new instance of <see cref="Scp03KeyParameters"/>, representing the parameters for
        /// a Secure Channel Protocol 03 (SCP03) key.
        /// </summary>
        /// <param name="keyReference">A reference to the key.</param>
        /// <param name="staticKeys">The static keys shared with the device when initiating the connection.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="keyReference.Id"/> is not between 1 and 3, which are the only valid Key IDs
        /// for SCP03
        /// </exception>
        public Scp03KeyParameters(
            KeyReference keyReference,
            StaticKeys staticKeys) : base(keyReference)
        {
            if (keyReference.Id < 1 || keyReference.Id > 3)
            {
                throw new ArgumentException("Key ID (KID) must be between 1 and 3 for SCP03.", nameof(keyReference.Id));
            }

            StaticKeys = staticKeys.GetCopy();
        }

        /// <summary>
        /// Creates a new instance of <see cref="Scp03KeyParameters"/>, representing the parameters for
        /// a Secure Channel Protocol 03 (SCP03) key.
        /// </summary>
        /// <param name="keyId">The ID of the key. Must be between 1 and 3 for SCP03.</param>
        /// <param name="keyVersionNumber">The version number of the key.</param>
        /// <param name="staticKeys">The static keys shared with the device.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="keyId"/> is not between 1 and 3, which are the only valid Key IDs
        /// for SCP03
        /// </exception>
        public Scp03KeyParameters(
            byte keyId,
            byte keyVersionNumber,
            StaticKeys staticKeys) : this(new KeyReference(keyId, keyVersionNumber), staticKeys)
        {
        }
        
        /// <summary>
        /// Gets the default SCP03 key parameters using the default key identifier and static keys.
        /// </summary>
        /// <remarks>
        /// This property provides a convenient way to access default SCP03 key parameters,
        /// using the standard SCP03 key identifier (0x03) and default static keys with version number 0xFF.
        /// </remarks>
        public static Scp03KeyParameters DefaultKey => new Scp03KeyParameters(ScpKeyIds.Scp03, DefaultKeyKvn, new StaticKeys());

        /// <summary>
        /// Creates a new instance of <see cref="Scp03KeyParameters"/>, representing the parameters for
        /// a Secure Channel Protocol 03 (SCP03) key, using the standard SCP03 key identifier and
        /// the given static keys.
        /// </summary>
        /// <param name="staticKeys">The static keys shared with the device.</param>
        /// <returns>An instance of <see cref="Scp03KeyParameters"/> with key ID 0x03 and version number 0x01.</returns>
        public static Scp03KeyParameters FromStaticKeys(StaticKeys staticKeys) =>
            new Scp03KeyParameters(ScpKeyIds.Scp03, DefaultKvn, staticKeys);

        /// <summary>
        /// This will clear all references and sensitive buffers  
        /// </summary>
        public void Dispose() => StaticKeys.Dispose();
    }
}
