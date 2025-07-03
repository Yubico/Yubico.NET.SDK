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
using System.Globalization;

namespace Yubico.YubiKey.U2f
{
    /// <summary>
    /// This is a base class for those classes that need to collect and encode
    /// data into a single buffer, either as data in a command or data to verify.
    /// This will hold the buffer along with the Application ID Hash and Client
    /// Data Hash. Subclasses can add more data to be placed into the buffer.
    /// </summary>
    /// <remarks>
    /// Only the SDK will ever need to create subclasses, there is no reason for
    /// any other application to do so.
    /// </remarks>
    public abstract class U2fBuffer
    {
        protected const int AppIdHashLength = 32;
        protected const int ClientDataHashLength = 32;
        protected const int KeyHandleLength = 64;
        protected const int PublicKeyLength = 65;
        protected const byte PublicKeyTag = 0x04;
        protected const int CoordinateLength = 32;
        protected const int CounterLength = 4;

        private protected readonly byte[] _buffer;
        private protected readonly Memory<byte> _bufferMemory;
        private readonly int _clientDataOffset;
        private readonly int _appIdOffset;

        /// <summary>
        /// Set the <c>ApplicationIdHash</c>. It must be 32 bytes long.
        /// </summary>
        public ReadOnlyMemory<byte> ApplicationId
        {
            get => _bufferMemory.Slice(_appIdOffset, AppIdHashLength);
            set => SetBufferData(value, AppIdHashLength, _appIdOffset, nameof(ApplicationId));
        }

        /// <summary>
        /// Set and get the <c>ClientDataHash</c> or "challenge". It must be 32 bytes long.
        /// </summary>
        public ReadOnlyMemory<byte> ClientDataHash
        {
            get => _bufferMemory.Slice(_clientDataOffset, ClientDataHashLength);
            set => SetBufferData(value, ClientDataHashLength, _clientDataOffset, nameof(ClientDataHash));
        }

        /// <summary>
        /// Initialize the object to the given values.
        /// </summary>
        protected U2fBuffer(int bufferLength, int appIdOffset, int clientDataOffset)
        {
            _buffer = new byte[bufferLength];
            _bufferMemory = new Memory<byte>(_buffer);
            _appIdOffset = appIdOffset;
            _clientDataOffset = clientDataOffset;
        }

        /// <summary>
        /// Copy the buffer data into the _buffer beginning at the given offset.
        /// Throw an exception if the input length is not correct.
        /// </summary>
        protected void SetBufferData(ReadOnlyMemory<byte> bufferData, int expectedLength, int offset, string variableName)
        {
            int badLength = -1;
            if (bufferData.Length != expectedLength)
            {
                badLength = bufferData.Length;
            }
            else if (expectedLength + offset > _buffer.Length)
            {
                badLength = _buffer.Length > offset ? _buffer.Length - offset : 0;
            }
            if (badLength >= 0)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPropertyLength,
                        variableName,
                        expectedLength,
                        badLength));
            }

            bufferData.CopyTo(_bufferMemory[offset..]);
        }
    }
}
