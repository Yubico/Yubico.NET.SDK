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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    /// <summary>
    /// Calls on the YubiKey to authenticate U2F data.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="AuthenticateResponse"/>.
    /// </remarks>
    public sealed class AuthenticateCommand : U2fBufferCommand, IYubiKeyCommand<AuthenticateResponse>
    {
        private const byte U2fAuthenticateCommandInstruction = 0x02;

        // For authentication, the payload is
        //  (clientDataHash || appIdHash || handleLength || keyHandle)
        // The handleLength 1 byte.
        private const int ClientDataOffset = 0;
        private const int AppIdOffset = ClientDataOffset + ClientDataHashLength;
        private const int KeyHandleLengthOffset = AppIdOffset + AppIdHashLength;
        private const int KeyHandleOffset = KeyHandleLengthOffset + 1;
        private const int PayloadLength = ClientDataHashLength + AppIdHashLength + KeyHandleLength + 1;

        private U2fAuthenticationType _controlByte;

        /// <summary>
        /// The authentication type that will be performed.
        /// </summary>
        public U2fAuthenticationType ControlByte
        {
            get => _controlByte;
            set
            {
                _controlByte = value;
                Parameter1 = (byte)value;
            }
        }

        /// <summary>
        /// The private key handle to be used to sign the challenge. This is the
        /// key handle returned by the YubiKey during registration.
        /// </summary>
        public ReadOnlyMemory<byte> KeyHandle
        {
            get => _bufferMemory.Slice(KeyHandleOffset, KeyHandleLength);
            set
            {
                SetBufferData(value, KeyHandleLength, KeyHandleOffset, nameof(KeyHandle));
                _buffer[KeyHandleLengthOffset] = KeyHandleLength;
            }
        }

        /// <summary>
        /// Creates an instance of the command.
        /// </summary>
        /// <remarks>
        /// This constructor is provided for those developers who want to use the
        /// object initializer pattern.
        /// <para>
        /// Set the <c>ClientDataHash</c>, <c>ApplicationId</c>, <c>KeyHandle</c>
        /// and <c>ControlByte</c> properties before sending the command to the
        /// YubiKey.
        /// </para>
        /// </remarks>
        public AuthenticateCommand()
            : base(U2fAuthenticateCommandInstruction, PayloadLength, AppIdOffset, ClientDataOffset)
        {
        }

        /// <summary>
        /// Creates an instance of the command with the given auth type,
        /// client data hash, app ID, and key handle.
        /// </summary>
        /// <remarks>
        /// The <c>controlByte</c> indicates what level of authentication to
        /// perform. It is called "control byte" because the standard specifies a
        /// control byte in the command's encoding.
        /// <para>
        /// The <c>applicationId</c> and <c>clientDataHash</c> are values
        /// provided by the client.
        /// </para>
        /// <para>
        /// The <c>keyHandle</c> is the value provided by the relying party, it
        /// was created by the YubiKey during registration and stored by the
        /// relying party.
        /// </para>
        /// </remarks>
        /// <param name="controlByte">
        /// The type of authentication to perform.
        /// </param>
        /// <param name="applicationId">
        /// The SHA256 hash of the Relying Party ID. It must be 32 bytes long.
        /// This is the hash of the origin data.
        /// </param>
        /// <param name="clientDataHash">
        /// The <c>clientDataHash</c> or "challenge" in the U2F (or CTAP2)
        /// specifications. It must be 32 bytes long.
        /// </param>
        /// <param name="keyHandle">
        /// The key handle provided by the Relying Party.
        /// </param>
        public AuthenticateCommand(
            U2fAuthenticationType controlByte,
            ReadOnlyMemory<byte> applicationId,
            ReadOnlyMemory<byte> clientDataHash,
            ReadOnlyMemory<byte> keyHandle)
            : this()
        {
            ControlByte = controlByte;
            ApplicationId = applicationId;
            ClientDataHash = clientDataHash;
            KeyHandle = keyHandle;
        }

        /// <inheritdoc />
        public AuthenticateResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new AuthenticateResponse(responseApdu);
    }
}
