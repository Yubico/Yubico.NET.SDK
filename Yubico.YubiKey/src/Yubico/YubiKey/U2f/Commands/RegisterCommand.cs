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
    internal class RegisterCommand : IYubiKeyCommand<RegisterResponse>
    {
        private const byte Ctap1MessageInstruction = 0x03;
        private const byte U2fRegisterCommandInstruction = 0x01;
        private const int ExpectedClientDataHashLength = 32;
        private const int ExpectedAppIdHashLength = 32;

        private readonly byte[] dataPayload = new byte[ExpectedClientDataHashLength + ExpectedAppIdHashLength];

        public YubiKeyApplication Application => YubiKeyApplication.FidoU2f;

        // Explicitly hiding this
        private RegisterCommand()
        {

        }

        /// <summary>
        /// Initializes an instance of the command with the given client data hash and app ID.
        /// </summary>
        /// <remarks>
        /// The app ID is specifically the SHA256 hash of the site origin's 'effective domain'. For example, it is the SHA256 hash of "acme.org" if the origin is 'https://acme.org:8443'.
        /// </remarks>
        /// <param name="clientDataHash">The <c>clientDataHash</c> or "challenge" in the CTAP2 and U2F specifications. Must be 32 bytes long.</param>
        /// <param name="appId">The SHA256 hash of the Relying Party ID. Must be 32 bytes long.</param>
        public RegisterCommand(ReadOnlySpan<byte> clientDataHash, ReadOnlySpan<byte> appId)
        {
            SetClientDataHash(clientDataHash);
            SetAppId(appId);
        }

        /// <summary>
        /// Set the clientDataHash or 'challenge' for this registration command.
        /// </summary>
        /// <param name="clientDataHash">The "challenge" in the U2F specification, and 'clientDataHash' in CTAP2. Must be 32 bytes long.</param>
        public void SetClientDataHash(ReadOnlySpan<byte> clientDataHash)
        {
            if (clientDataHash.Length != ExpectedClientDataHashLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPropertyLength,
                        nameof(clientDataHash),
                        ExpectedClientDataHashLength,
                        clientDataHash.Length));
            }

            clientDataHash.CopyTo(dataPayload.AsSpan());
        }

        /// <summary>
        /// Set the app ID for this registration command.
        /// </summary>
        /// <remarks>
        /// The app ID is specifically the SHA256 hash of the site origin's 'effective domain'. For example, it is the SHA256 hash of "acme.org" if the origin is 'https://acme.org:8443'.
        /// </remarks>
        /// <param name="appId">The SHA256 hash of the Relying Party ID. Must be 32 bytes long.</param>
        public void SetAppId(ReadOnlySpan<byte> appId)
        {
            if (appId.Length != ExpectedAppIdHashLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPropertyLength,
                        nameof(appId),
                        ExpectedAppIdHashLength,
                        appId.Length));
            }

            appId.CopyTo(dataPayload.AsSpan(ExpectedClientDataHashLength));
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            var innerEchoCommand = new CommandApdu()
            {
                Ins = U2fRegisterCommandInstruction,
                Data = dataPayload,
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
