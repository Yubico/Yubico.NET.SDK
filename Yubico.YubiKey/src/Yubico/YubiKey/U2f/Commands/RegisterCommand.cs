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
    /// <code>
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
    public class RegisterCommand : IYubiKeyCommand<RegisterResponse>
    {
        private const byte Ctap1MessageInstruction = 0x03;
        private const byte U2fRegisterCommandInstruction = 0x01;
        private const int ClientDataHashLength = 32;
        private const int ApplicationIdHashLength = 32;

        private readonly byte[] _dataPayload = new byte[ClientDataHashLength + ApplicationIdHashLength];

        public YubiKeyApplication Application => YubiKeyApplication.FidoU2f;

        /// <summary>
        /// A 32-byte challenge sent by the client application.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <remarks>
        /// <para>
        /// The FIDO Client is the application that facilitates the interaction between the authenticator (YubiKey) and
        /// the relying party (the website or service the user is authenticating against). The client is likely the
        /// software that is calling this API.
        /// </para>
        /// </remarks>
        public ReadOnlyMemory<byte> ClientDataHash
        {
            get => _dataPayload.AsMemory(0, ClientDataHashLength);
            set
            {
                if (value.Length != ClientDataHashLength)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidPropertyLength,
                            nameof(value),
                            ClientDataHashLength,
                            value.Length));
                }

                value.CopyTo(_dataPayload.AsMemory(0, ClientDataHashLength));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public ReadOnlyMemory<byte> ApplicationId
        {
            get => _dataPayload.AsMemory(ClientDataHashLength, ApplicationIdHashLength);
            set
            {
                if (value.Length != ApplicationIdHashLength)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidPropertyLength,
                            nameof(value),
                            ApplicationIdHashLength,
                            value.Length));
                }

                value.CopyTo(_dataPayload.AsMemory(ClientDataHashLength, ApplicationIdHashLength));
            }
        }

        /// <summary>
        /// Initializes an instance of the <see cref="RegisterCommand"/> class.
        /// </summary>
        /// <remarks>
        /// Use the <see cref="ClientDataHash"/> and <see cref="ApplicationId"/> properties to set the appropriate
        /// input data for this command. Failure to do so may register a default or empty credential, which would
        /// needlessly take up a credential slot from other real credentials. The only way to remove this credential would
        /// be to reset the entire FIDO application.
        /// </remarks>
        public RegisterCommand()
        {

        }

        /// <summary>
        /// Initializes an instance of the command with the given client data hash and app ID.
        /// </summary>
        /// <remarks>
        /// The app ID is specifically the SHA256 hash of the site origin's 'effective domain'. For example, it is the
        /// SHA256 hash of "acme.org" if the origin is 'https://acme.org:8443'.
        /// </remarks>
        /// <param name="clientDataHash">
        /// The <c>clientDataHash</c> or "challenge" in the CTAP2 and U2F specifications. Must be 32 bytes long.
        /// </param>
        /// <param name="applicationId">
        /// The SHA256 hash of the Relying Party ID. Must be 32 bytes long.
        /// </param>
        public RegisterCommand(ReadOnlyMemory<byte> clientDataHash, ReadOnlyMemory<byte> applicationId)
        {
            ClientDataHash = clientDataHash;
            ApplicationId = applicationId;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            var innerEchoCommand = new CommandApdu()
            {
                Ins = U2fRegisterCommandInstruction,
                Data = _dataPayload,
            };

            return new CommandApdu()
            {
                Ins = Ctap1MessageInstruction,
                Data = innerEchoCommand.AsByteArray(ApduEncoding.ExtendedLength),
            };
        }

        /// <inheritdoc />
        public RegisterResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new RegisterResponse(responseApdu);
    }
}
