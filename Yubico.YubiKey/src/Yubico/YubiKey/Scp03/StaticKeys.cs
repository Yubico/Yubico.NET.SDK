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

namespace Yubico.YubiKey.Scp03
{
    /// <summary>
    /// Represents a triple of SCP03 static keys shared with the device.
    /// </summary>
    /// <remarks>
    /// These are the three secret keys that only the device and remote user know. Clients must supply these to communicate securely with a remote device. Required by <see creh="RemoteYubikey"/>. 
    /// 
    /// Systems often derive and assign these keys using a diversification function keyed with a 'master key' and run on the 'DivData' of each device.
    /// </remarks>
    public class StaticKeys
    {
        /// <summary>
        /// AES128 shared secret key used to calculate the Session-MAC key. Also called the 'DMK' or 'Key-MAC' in some specifications.
        /// </summary>
        public ReadOnlyMemory<byte> ChannelMacKey { get; private set; }
        /// <summary>
        /// AES128 shared secret key used to calculate the Session-ENC key. Also called the 'DAK' or 'Key-ENC' in some specifications.
        /// </summary>
        public ReadOnlyMemory<byte> ChannelEncryptionKey { get; private set; }
        /// <summary>
        /// AES128 shared secret key used to wrap secrets. Also called the 'DEK' in some specifications.
        /// </summary>
        public ReadOnlyMemory<byte> DataEncryptionKey { get; private set; }

        internal static readonly byte[] DefaultKey = new byte[] { 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f };

        private const int KeySizeBytes = 16;

        /// <summary>
        /// Constructs an instance given the supplied keys.
        /// </summary>
        /// <param name="channelMacKey">16-byte AES128 shared secret key</param>
        /// <param name="channelEncryptionKey">16-byte AES128 shared secret key</param>
        /// <param name="dataEncryptionKey">16-byte AES128 shared secret key</param>
        public StaticKeys(ReadOnlyMemory<byte> channelMacKey, ReadOnlyMemory<byte> channelEncryptionKey, ReadOnlyMemory<byte> dataEncryptionKey)
        {
            if (channelMacKey.Length != KeySizeBytes)
            {
                throw new ArgumentException(ExceptionMessages.IncorrectStaticKeyLength, nameof(channelMacKey));
            }

            if (channelEncryptionKey.Length != KeySizeBytes)
            {
                throw new ArgumentException(ExceptionMessages.IncorrectStaticKeyLength, nameof(channelEncryptionKey));
            }

            if (dataEncryptionKey.Length != KeySizeBytes)
            {
                throw new ArgumentException(ExceptionMessages.IncorrectStaticKeyLength, nameof(dataEncryptionKey));
            }

            ChannelMacKey = channelMacKey;
            ChannelEncryptionKey = channelEncryptionKey;
            DataEncryptionKey = dataEncryptionKey;
        }

        /// <summary>
        /// Constructs an instance of the well-known default values; using these provides no security.
        /// </summary>
        public StaticKeys()
        {
            ChannelMacKey = DefaultKey;
            ChannelEncryptionKey = DefaultKey;
            DataEncryptionKey = DefaultKey;
        }
    }
}
