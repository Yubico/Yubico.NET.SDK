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
using System.Security.Cryptography;

namespace Yubico.YubiKey.Scp
{
    /// <summary>
    /// Represents a triple of SCP03 static keys shared with the device.
    /// </summary>
    /// <remarks>
    /// See also the <xref href="UsersManualScp">User's Manual entry</xref> on
    /// SCP03.
    /// <para>
    /// These are the three secret keys that only the device and remote user
    /// know. Clients must supply these to communicate securely with a remote
    /// device.
    /// </para>
    /// <para>
    /// Systems often derive and assign these keys using a diversification
    /// function keyed with a 'master key' and run on the 'DivData' of each
    /// device.
    /// </para>
    /// </remarks>
    public class StaticKeys : IDisposable
    {
        private const int KeySizeBytes = 16;

        private readonly byte[] _macKey = new byte[KeySizeBytes];
        private readonly byte[] _encKey = new byte[KeySizeBytes];
        private readonly byte[] _dekKey = new byte[KeySizeBytes];

        private bool _disposed;

        /// <summary>
        /// AES128 shared secret key used to calculate the Session-MAC key. Also
        /// called the 'DMK' or 'Key-MAC' in some specifications.
        /// </summary>
        public ReadOnlyMemory<byte> ChannelMacKey => _macKey;

        /// <summary>
        /// AES128 shared secret key used to calculate the Session-ENC key. Also
        /// called the 'DAK' or 'Key-ENC' in some specifications.
        /// </summary>
        public ReadOnlyMemory<byte> ChannelEncryptionKey => _encKey;

        /// <summary>
        /// AES128 shared secret key used to wrap secrets. Also called the 'DEK'
        /// in some specifications.
        /// </summary>
        public ReadOnlyMemory<byte> DataEncryptionKey => _dekKey;

        /// <summary>
        /// Constructs an instance given the supplied keys. This class will
        /// consider these keys to be the key set with the Key Version Number of
        /// <remarks>
        /// This class will copy the input key data, not just a reference. You
        /// can overwrite the input buffers as soon as the <c>StaticKeys</c>
        /// object is created.
        /// </remarks>
        /// <param name="channelMacKey">16-byte AES128 shared secret key</param>
        /// <param name="channelEncryptionKey">16-byte AES128 shared secret key</param>
        /// <param name="dataEncryptionKey">16-byte AES128 shared secret key</param>
        /// </summary>
        public StaticKeys(ReadOnlyMemory<byte> channelMacKey,
                          ReadOnlyMemory<byte> channelEncryptionKey,
                          ReadOnlyMemory<byte> dataEncryptionKey)
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

            SetKeys(channelMacKey, channelEncryptionKey, dataEncryptionKey);
        }

        /// <summary>
        /// Constructs an instance using the well-known default values; using
        /// these provides no security.  This class will consider these keys to
        /// be the key set with the Key Version Number of 255 (0xFF).
        /// </summary>
        public StaticKeys()
        {
            var defaultKey = new ReadOnlyMemory<byte>(
                new byte[]
                {
                    0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f
                });

            SetKeys(defaultKey, defaultKey, defaultKey);
        }

        private void SetKeys(ReadOnlyMemory<byte> channelMacKey,
                             ReadOnlyMemory<byte> channelEncryptionKey,
                             ReadOnlyMemory<byte> dataEncryptionKey)
        {
            channelMacKey.CopyTo(_macKey.AsMemory());
            channelEncryptionKey.CopyTo(_encKey.AsMemory());
            dataEncryptionKey.CopyTo(_dekKey.AsMemory());
        }

        // Get a copy (deep clone) of this object.
        internal StaticKeys GetCopy() =>
            new StaticKeys(ChannelMacKey, ChannelEncryptionKey, DataEncryptionKey)
            {
            };

        /// <summary>
        /// Determine if the contents of each key is the same for both objects.
        /// If so, this method will return <c>true</c>.
        /// </summary>
        public bool AreKeysSame(StaticKeys? compareKeys)
        {
            if (compareKeys is null)
            {
                return false;
            }

            return
                ChannelEncryptionKey.Span.SequenceEqual(compareKeys.ChannelEncryptionKey.Span) &&
                ChannelMacKey.Span.SequenceEqual(compareKeys.ChannelMacKey.Span) &&
                DataEncryptionKey.Span.SequenceEqual(compareKeys.DataEncryptionKey.Span);
        }
        /// <summary>
        /// This will clear all references and sensitive buffers  
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (!disposing)
            {
                return;
            }

            CryptographicOperations.ZeroMemory(_macKey.AsSpan());
            CryptographicOperations.ZeroMemory(_encKey.AsSpan());
            CryptographicOperations.ZeroMemory(_dekKey.AsSpan());

            _disposed = true;
        }
    }
}
