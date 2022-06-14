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
using System.Globalization;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    /// <summary>
    /// Creates a U2F registration on the device.
    /// </summary>
    /// <remarks>
    /// <p>
    /// The partner Response class is <see cref="RegisterResponse"/>.
    /// </p>
    /// <p>
    /// This command will generally initially return a <see cref="ResponseStatus.ConditionsNotSatisfied"/>
    /// and will not return success until the user confirms presence by touching the device. Clients should
    /// repeatedly send the command as long as they receive this status.
    /// </p>
    /// <p>
    /// Example:
    /// </p>
    /// <code language="csharp">
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.FidoU2f);
    ///   byte[] challenge = new byte[32];
    ///   RandomNumberGenerator.Fill(challenge);
    ///   byte[] appId = new byte[32];
    ///   RandomNumberGenerator.Fill(appId);
    ///   RegisterCommand registerCommand = new RegisterCommand(challenge, appId);
    ///   <br/>
    ///   RegisterResponse registerResponse;
    ///   do
    ///   {
    ///       registerResponse = fidoConnection.SendCommand(registerCommand);
    ///   } while (registerResponse.Status == ResponseStatus.ConditionsNotSatisfied);
    ///   RegistrationData data = registerResponse.GetData();
    /// </code>
    /// </remarks>
    public sealed class RegisterCommand : U2fBufferCommand, IYubiKeyCommand<RegisterResponse>
    {
        private const byte U2fRegisterCommandInstruction = 0x01;

        // For registration, the payload is
        //  (clientDataHash || appIdHash)
        private const int ClientDataOffset = 0;
        private const int AppIdOffset = ClientDataOffset + ClientDataHashLength;
        private const int PayloadLength = ClientDataHashLength + AppIdHashLength;

        /// <summary>
        /// Creates an instance of the command.
        /// </summary>
        /// <remarks>
        /// This constructor is provided for those developers who want to use the
        /// object initializer pattern.
        /// <para>
        /// Set the <c>ApplicationId</c> and <c>ClientDataHash</c> properties
        /// before sending the command to the YubiKey.
        /// </para>
        /// </remarks>
        public RegisterCommand()
            : base(U2fRegisterCommandInstruction, PayloadLength, AppIdOffset, ClientDataOffset)
        {
        }

        /// <summary>
        /// Creates an instance of the command with the given client data hash and app ID.
        /// </summary>
        /// <remarks>
        /// The app ID is specifically the SHA256 hash of the site origin's 'effective domain'.
        /// For example, it is the SHA256 hash of "acme.org" if the origin is 'https://acme.org:8443'.
        /// </remarks>
        /// <param name="applicationId">The SHA256 hash of the Relying Party ID.
        /// Must be 32 bytes long. This is the hash of the origin data.</param>
        /// <param name="clientDataHash">The <c>clientDataHash</c> or "challenge" in the CTAP2 and U2F specifications. Must be 32 bytes long.</param>
        public RegisterCommand(ReadOnlyMemory<byte> applicationId, ReadOnlyMemory<byte> clientDataHash)
            : this()
        {
            ApplicationId = applicationId;
            ClientDataHash = clientDataHash;
        }

        /// <inheritdoc />
        public RegisterResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new RegisterResponse(responseApdu);
    }
}
