// Copyright 2023 Yubico AB
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
using System.Formats.Cbor;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// The <see cref="CredentialManagementCommand"/> allows a client or platform
    /// to obtain credential information. This command has a number of
    /// subcommands, for which the SDK has separate command classes.
    /// </summary>
    /// <remarks>
    /// The <c>authenticatorCredentialManagement (0x0A)</c> FIDO2 command can be
    /// thought of as a "meta" command. That is, it provides the structure and
    /// mechanism for performing a number of sub-commands. These sub-commands are:
    /// <code language="adoc">
    /// - getCredsMetadata (0x01)
    /// - enumerateRPsBegin (0x02)
    /// - enumerateRPsGetNextRP (0x03)
    /// - enumerateCredentialsBegin (0x04)
    /// - enumerateCredentialsGetNextCredential (0x05)
    /// - deleteCredential (0x06)
    /// - updateUserInformation (0x07)
    /// </code>
    /// Since the SDK does not have the concept of a sub-command natively, these
    /// are all exposed as their own separate commands.
    /// <para>
    /// This command should seldom be used directly. It is exposed for
    /// completeness. The sub-commands exposed in this namespace use it as their
    /// implementation and expose a pared down version of the parameters.
    /// </para>
    /// <para>
    /// See the user manual entry on <xref
    /// href="Fido2CredentialManagement">Credential Management</xref> for a much
    /// more in depth guide to working with credentials within FIDO2. For more
    /// information on a particular sub-command, see the API reference
    /// documentation for that command class.
    /// </para>
    /// </remarks>
    public class CredentialManagementCommand : IYubiKeyCommand<CredentialManagementResponse>
    {
        // Command constants
        private const byte CmdAuthenticatorCredMgmt = 0x0A;

        private const int TagSubCommand = 1;
        private const int TagParams = 2;
        private const int TagProtocol = 3;
        private const int TagPinUvAuthParam = 4;

        /// <inheritdoc />
        public YubiKeyApplication Application => YubiKeyApplication.Fido2;

        /// <summary>
        /// The CredentialManagement sub-command to issue to the YubiKey.
        /// </summary>
        /// <remarks>
        /// This is a mandatory parameter, and must be one of the following values:
        /// <code language="adoc">
        /// - getCredsMetadata (0x01)
        /// - enumerateRPsBegin (0x02)
        /// - enumerateRPsGetNextRP (0x03)
        /// - enumerateCredentialsBegin (0x04)
        /// - enumerateCredentialsGetNextCredential (0x05)
        /// - deleteCredential (0x06)
        /// - updateUserInformation (0x07)
        /// </code>
        /// Alternatively - you can use one of the command classes exposed by the
        /// SDK that represents the sub-command itself. Such a method is
        /// recommended as these command classes will only expose the parameters
        /// that are relevant to that sub-command.
        /// </remarks>
        public int SubCommand { get; set; }

        /// <summary>
        /// The encoded params for the specified sub-command. If a sub-command
        /// has no parameters, this will be null.
        /// </summary>
        public ReadOnlyMemory<byte>? SubCommandParameters { get; set; }

        /// <summary>
        /// The PIN/UV protocol version chosen by the platform.
        /// </summary>
        /// <remarks>
        /// A PIN/UV protocol must be used when performing CredentialManagement
        /// operations. The specified protocol must be one of the protocols that
        /// are supported by the YubiKey. This can be determined by issuing the
        /// AuthenticatorGetInfo command.
        /// </remarks>
        public PinUvAuthProtocol PinUvAuthProtocol { get; set; }

        /// <summary>
        /// The output of calling authenticate on the PIN/UV protocol specific to
        /// a particular sub-command.
        /// </summary>
        /// <remarks>
        /// See the user manual entry on
        /// <xref href="Fido2PinProtocol">PIN protocols</xref> for a much more in
        /// depth guide to working with PINs within FIDO2.
        /// </remarks>
        public ReadOnlyMemory<byte> PinUvAuthParam { get; set; }

        /// <summary>
        /// Constructs a new instance of <see cref="CredentialManagementCommand"/>.
        /// </summary>
        /// <param name="subCommand">
        /// The byte representing the sub-command to execute.
        /// </param>
        /// <param name="subCommandParams">
        /// The parameters needed in order to execute the sub-command. Not all
        /// sub-commands have parameters, so this can be null.
        /// </param>
        /// <param name="pinUvAuthToken">
        /// The PIN/UV Auth Token built from the PIN. This is the encrypted token
        /// key.
        /// </param>
        /// <param name="authProtocol">
        /// The Auth Protocol used to build the Auth Token.
        /// </param>
        public CredentialManagementCommand(
            int subCommand, ReadOnlyMemory<byte>? subCommandParams,
            ReadOnlyMemory<byte> pinUvAuthToken, PinUvAuthProtocolBase authProtocol)
        {
            if (authProtocol is null)
            {
                throw new ArgumentNullException(nameof(authProtocol));
            }

            SubCommand = subCommand;
            SubCommandParameters = subCommandParams;

            // Compute pinUvAuthParam =
            //     authProtocol.AuthenticateUsingPinToken(authToken, contents);
            // If there are no params, the contents consists of the single byte
            // subCommand.
            // If there are params, the contents consist of
            // subCommand || params.
            int length = subCommandParams?.Length ?? 0;
            byte[] message = new byte[length + 1];
            message[0] = (byte)subCommand;
            subCommandParams?.CopyTo(message.AsMemory(1));

            // The pinUvAuthToken is an encrypted value, so there's no need to
            // overwrite the array.
            byte[] authParam = authProtocol.AuthenticateUsingPinToken(pinUvAuthToken.ToArray(), message);
            PinUvAuthParam = new ReadOnlyMemory<byte>(authParam, 0, 16);
            //PinUvAuthParam = new ReadOnlyMemory<byte>(authParam);
            PinUvAuthProtocol = authProtocol.Protocol;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);

            CborHelpers.BeginMap<int>(cbor)
                .Entry(TagSubCommand, (int)SubCommand)
                .OptionalEntry(TagParams, SubCommandParameters)
                .Entry(TagProtocol, (int)PinUvAuthProtocol)
                .Entry(TagPinUvAuthParam, PinUvAuthParam)
                .EndMap();

            byte[] data = new byte[1 + cbor.BytesWritten];
            int bytesWritten = cbor.Encode(data.AsSpan(1));

            if (bytesWritten != data.Length - 1)
            {
                throw new Ctap2DataException(ExceptionMessages.CborLengthMismatch);
            }

            data[0] = CmdAuthenticatorCredMgmt;

            return new CommandApdu()
            {
                Ins = CtapConstants.CtapHidCbor,
                Data = data
            };
        }

        /// <inheritdoc />
        public CredentialManagementResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new CredentialManagementResponse(responseApdu);
    }
}
