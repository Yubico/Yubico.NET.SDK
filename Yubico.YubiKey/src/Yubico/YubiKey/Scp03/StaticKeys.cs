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
using System.Security.Cryptography;
using Yubico.YubiKey.Scp;

namespace Yubico.YubiKey.Scp03
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
    [Obsolete("Use new Static Keys")]
    public class StaticKeys : IDisposable
    {
        private const int KeySizeBytes = 16;

        private const byte MinimumKvnValue = 1;
        private const byte MaximumKvnValue = 3;
        private const byte DefaultKvnValue = 0xff;

        private byte _keyVersionNumber;

        private readonly byte[] _macKey = new byte[KeySizeBytes];
        private readonly byte[] _encKey = new byte[KeySizeBytes];
        private readonly byte[] _dekKey = new byte[KeySizeBytes];

        private bool _disposed;

        /// <summary>
        /// The number that identifies the key set. Unless specified by the
        /// caller, this class will assume the Key Version Number is 1, or else
        /// 255 if the default keys are used.
        /// </summary>
        /// <remarks>
        /// When the SDK makes an SCP03 <c>Connection</c> with a YubiKey, it will
        /// specify the Key Version Number. In that way, the YubiKey will know
        /// which keys to use to complete the handshake.
        /// <para>
        /// A YubiKey can store up to three sets of SCP03 keys. You can think of
        /// it as if the YubiKey contains three slots (1, 2, and 3) for SCP03
        /// keys. Each set (slot) is specified by a number, which the standard
        /// calls the Key Version Number. On a YubiKey, the only numbers allowed
        /// to be a Key Version Number are 255, 1, 2, and 3.
        /// </para>
        /// <para>
        /// Most YubiKeys are manufactured with a default set of SCP03 keys in
        /// slot 1. Slots 2 and 3 are empty. The initial, default set of keys in
        /// slot 1 is given the Key Version Number 255. If you replace those
        /// keys, the replacement must be specified as number 1. If you want to
        /// add key sets to the other two slots, you must use the numbers 2 and
        /// 3. Note that you cannot set the two empty slots until the initial,
        /// default keys are replaced.
        /// </para>
        /// <para>
        /// If the Key Version Number to use is not the value this class uses by
        /// default, then set this value after constructing the object.
        /// </para>
        /// </remarks>
        public byte KeyVersionNumber
        {
            get => _keyVersionNumber;

            set
            {
                if (value != DefaultKvnValue && (value < MinimumKvnValue || value > MaximumKvnValue))
                {
                    throw new ArgumentException(ExceptionMessages.InvalidScp03Kvn);
                }

                _keyVersionNumber = value;
            }
        }

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
        /// 1. If the key version number should be something else, set the
        /// <see cref="KeyVersionNumber"/> property after calling the constructor.
        /// </summary>
        /// <remarks>
        /// This class will copy the input key data, not just a reference. You
        /// can overwrite the input buffers as soon as the <c>StaticKeys</c>
        /// object is created.
        /// </remarks>
        /// <param name="channelMacKey">16-byte AES128 shared secret key</param>
        /// <param name="channelEncryptionKey">16-byte AES128 shared secret key</param>
        /// <param name="dataEncryptionKey">16-byte AES128 shared secret key</param>
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
            KeyVersionNumber = 1;

            _disposed = false;
        }

        /// <summary>
        /// Constructs an instance using the well-known default values; using
        /// these provides no security.  This class will consider these keys to
        /// be the key set with the Key Version Number of 255 (0xFF).
        /// </summary>
        public StaticKeys()
        {
            var DefaultKey = new ReadOnlyMemory<byte>(
                new byte[]
                {
                    0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f
                });

            SetKeys(DefaultKey, DefaultKey, DefaultKey);
            KeyVersionNumber = 255;

            _disposed = false;
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
                KeyVersionNumber = KeyVersionNumber
            };
        
        internal Scp03KeyParameters ConvertToScp03KeyParameters() =>
                new Scp03KeyParameters(ScpKeyIds.Scp03, DefaultKvnValue, ConvertFromLegacy());

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

            return ChannelEncryptionKey.Span.SequenceEqual(compareKeys.ChannelEncryptionKey.Span) &&
                ChannelMacKey.Span.SequenceEqual(compareKeys.ChannelMacKey.Span) &&
                DataEncryptionKey.Span.SequenceEqual(compareKeys.DataEncryptionKey.Span);
        }
        
        private Scp.StaticKeys ConvertFromLegacy() =>
            new Scp.StaticKeys(ChannelMacKey, ChannelEncryptionKey, DataEncryptionKey)
            {
                // KeyVersionNumber = KeyVersionNumber
            };

        /// <summary>
        /// Releases any unmanaged resources and overwrites any sensitive data.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases any unmanaged resources and overwrites any sensitive data.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    CryptographicOperations.ZeroMemory(_macKey.AsSpan());
                    CryptographicOperations.ZeroMemory(_encKey.AsSpan());
                    CryptographicOperations.ZeroMemory(_dekKey.AsSpan());

                    _disposed = true;
                }
            }
        }
    }
}
