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
using System.Formats.Cbor;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// The <see cref="CredentialManagementCommand"/> is the class for
    /// <c>authenticatorCredentialManagement</c>. This command has a number of
    /// subcommands, each of which is represented by its own class. You will
    /// likely never use this class directly, but it does contain code shared by
    /// all the subcommands.
    /// </summary>
    /// <remarks>
    /// The <c>authenticatorCredentialManagement (0x0A)</c> FIDO2 command can be
    /// thought of as a "meta" command. That is, it provides the structure and
    /// mechanism for performing a number of subcommands. These subcommands are:
    /// <code language="adoc">
    /// - getCredsMetadata (0x01)
    /// - enumerateRPsBegin (0x02)
    /// - enumerateRPsGetNextRP (0x03)
    /// - enumerateCredentialsBegin (0x04)
    /// - enumerateCredentialsGetNextCredential (0x05)
    /// - deleteCredential (0x06)
    /// - updateUserInformation (0x07)
    /// </code>
    /// Since the SDK does not have the concept of a subcommand natively, these
    /// are all exposed as their own separate commands.
    /// <para>
    /// See the user manual entry on
    /// <xref href="Fido2CredentialManagement">Credential Management</xref> for a
    /// much more in depth guide to working with credentials within FIDO2. For
    /// more information on a particular subcommand, see the API reference
    /// documentation for that command class.
    /// </para>
    /// <para>
    /// Some of the subcommands return data (e.g. a credential), others return
    /// only a success or failure response code.
    /// </para>
    /// </remarks>
    public class CredentialManagementCommand : IYubiKeyCommand<CredentialManagementResponse>
    {
        // Command constants
        private const byte CmdAuthenticatorCredMgmt = 0x0A;
        private const byte CmdCredentialMgmtPreview = 0x41;

        private const int TagSubCommand = 1;
        private const int TagParams = 2;
        private const int TagProtocol = 3;
        private const int TagPinUvAuthParam = 4;

        private readonly byte[]? _encodedParams;
        private readonly int? _protocol;

        /// <inheritdoc />
        public YubiKeyApplication Application => YubiKeyApplication.Fido2;

        /// <summary>
        /// The CredentialManagement subcommand to issue to the YubiKey.
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
        /// SDK that represents the subcommand itself. Such a method is
        /// recommended as these command classes will only expose the parameters
        /// that are relevant to that subcommand.
        /// </remarks>
        public int SubCommand { get; set; }

        /// <summary>
        /// The encoded params for the specified subcommand. If a subcommand
        /// has no parameters, this will be null.
        /// </summary>
        public ReadOnlyMemory<byte>? SubCommandParameters => _encodedParams?.AsMemory();

        /// <summary>
        /// The PIN/UV protocol version chosen by the platform.
        /// </summary>
        /// <remarks>
        /// A PIN/UV protocol must be used when performing some of the
        /// CredentialManagement operations. The specified protocol must be one
        /// of the protocols that are supported by the YubiKey. This can be
        /// determined by issuing the AuthenticatorGetInfo command.
        /// </remarks>
        public PinUvAuthProtocol? PinUvAuthProtocol { get; set; }

        /// <summary>
        /// The output of calling authenticate on the PIN/UV protocol specific to
        /// a particular subcommand.
        /// </summary>
        /// <remarks>
        /// See the User's Manual entry on
        /// <xref href="Fido2PinProtocol">PIN protocols</xref> for a much more in
        /// depth guide to working with PINs within FIDO2.
        /// <para>
        /// See also the User's Manual entry on
        /// <xref href="Fido2CredentialManagement">FIDO2 Credential Management</xref>
        /// for more information on building the <c>PIN/UV Auth Param</c>
        /// specific to the CredentialManagement commands.
        /// </para>
        /// </remarks>
        public ReadOnlyMemory<byte>? PinUvAuthParam { get; set; }

        /// <summary>
        /// This constructor will throw <c>NotImplementedException</c>. It is the
        /// default constructor explicitly defined. We don't want it to be used.
        /// It is made <c>protected</c> rather than <c>private</c> because there
        /// are subclasses.
        /// </summary>
        protected CredentialManagementCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="CredentialManagementCommand"/>.
        /// </summary>
        /// <remarks>
        /// Note that if the command does not need the <c>pinUvAuthToken</c> and
        /// <c>authProtocol</c>, use the constructor that takes only the
        /// <c>subCommand</c>.
        /// </remarks>
        /// <param name="subCommand">
        /// The byte representing the subcommand to execute.
        /// </param>
        /// <param name="subCommandParams">
        /// The parameters needed in order to execute the subcommand. Not all
        /// subcommands have parameters, so this can be null.
        /// </param>
        /// <param name="pinUvAuthToken">
        /// The PIN/UV Auth Token built from the PIN. This is the encrypted token
        /// key.
        /// </param>
        /// <param name="authProtocol">
        /// The Auth Protocol used to build the Auth Token.
        /// </param>
        /// <param name="decryptAuthToken">TODO</param>
        public CredentialManagementCommand(
            int subCommand,
            byte[]? subCommandParams,
            ReadOnlyMemory<byte> pinUvAuthToken,
            PinUvAuthProtocolBase authProtocol,
            bool decryptAuthToken = true)
        {
            if (authProtocol is null)
            {
                throw new ArgumentNullException(nameof(authProtocol));
            }

            SubCommand = subCommand;
            _encodedParams = subCommandParams;
            _protocol = (int)authProtocol.Protocol;

            // If there are no params, the contents consists of the single byte
            // subCommand.
            // If there are params, the contents consist of
            // subCommand || params.
            int length = subCommandParams?.Length ?? 0;
            byte[] message = new byte[length + 1];
            message[0] = (byte)subCommand;
            subCommandParams?.CopyTo(message, 1);

            // The pinUvAuthToken is an encrypted value, so there's no need to
            // overwrite the array.
            byte[] authParam = decryptAuthToken
                ? authProtocol.AuthenticateUsingPinToken(pinUvAuthToken.ToArray(), message)
                : authProtocol.Authenticate(pinUvAuthToken.ToArray(), message);

            PinUvAuthParam = authParam;
            PinUvAuthProtocol = authProtocol.Protocol;
        }

        /// <summary>
        /// Constructs a new instance of <see cref="CredentialManagementCommand"/>.
        /// </summary>
        /// <param name="subCommand">
        /// The byte representing the subcommand to execute.
        /// </param>
        /// <param name="protocol"></param>
        public CredentialManagementCommand(
            int subCommand,
            PinUvAuthProtocol protocol = PinProtocols.PinUvAuthProtocol.ProtocolTwo)
        {
            SubCommand = subCommand;
            _encodedParams = null;
            _protocol = (int)protocol;
            PinUvAuthProtocol = null;
            PinUvAuthParam = null;
        }

        /// <summary>
        /// Creates a well-formed CommandApdu to send to the YubiKey.
        /// </summary>
        /// <remarks>
        /// This version of the CreateCommandApdu method will build the APDU for
        /// the authenticatorCredentialManagement command. That is, it is
        /// equivalent to calling <c>CreateCommandApdu(false)</c>.
        /// <para>
        /// This method will first perform validation on all of the parameters and data provided
        /// to it. The CommandAPDU it creates should contain all of the data payload for the
        /// command, even if it exceeds 65,535 bytes as specified by the ISO 7816-4 specification.
        /// The APDU will be properly chained by the device connection prior to being sent to the
        /// YubiKey, and the responses will collapsed into a single result.
        /// </para>
        /// </remarks>
        /// <returns>A valid CommandApdu that is ready to be sent to the YubiKey, or passed along
        /// to additional encoders for further processing.</returns>
        public CommandApdu CreateCommandApdu() => CreateCommandApdu(false);

        /// <summary>
        /// Creates a well-formed CommandApdu to send to the YubiKey.
        /// </summary>
        /// <remarks>
        /// This version of the CreateCommandApdu method contains an arg
        /// indicating whether it should build an APDU for the
        /// CredentialMgmtPreview command (<c>true</c>) or for the
        /// authenticatorCredentialManagement command (<c>false</c>).
        /// <para>
        /// The authenticatorCredentialManagement command was introduced in
        /// FIDO2.1. Hence, YubiKeys that do not support 2.1 will not have this
        /// feature. However, there was a version "2_1_PRE" which contained the
        /// "CredentialMgmtPreview" command. Each "credMgmt" command has a
        /// corresponding operation in this preview command.
        /// </para>
        /// <para>
        /// If the YubiKey does not support "credMgmt" but does support
        /// "CredentialMgmtPreview", then pass <c>true</c> as the isPreview arg.
        /// Otherwise, either pass <c>false</c> to this method or call the
        /// <c>CreateCommandApdu</c> method that take no argument.
        /// </para>
        /// </remarks>
        /// <param name="isPreview">
        /// Indicates whether this command should use the CredentialMgmtPreview
        /// command (<c>true</c>) or authenticatorCredentialManagement
        /// (<c>false</c>). This is an optional arg, and if not given the default
        /// will be <c>false</c>.
        /// </param>
        public CommandApdu CreateCommandApdu(bool isPreview)
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);

            CborHelpers.BeginMap<int>(cbor)
                .Entry(TagSubCommand, (int)SubCommand)
                .OptionalEntry<byte[]>(TagParams, WriteEncodedParams, _encodedParams)
                .OptionalEntry(TagProtocol, _protocol)
                .OptionalEntry(TagPinUvAuthParam, PinUvAuthParam)
                .EndMap();

            byte[] data = new byte[1 + cbor.BytesWritten];
            int bytesWritten = cbor.Encode(data.AsSpan(1));

            if (bytesWritten != data.Length - 1)
            {
                throw new Ctap2DataException(ExceptionMessages.CborLengthMismatch);
            }

            data[0] = isPreview
                ? CmdCredentialMgmtPreview
                : CmdAuthenticatorCredMgmt;

            return new CommandApdu()
            {
                Ins = CtapConstants.CtapHidCbor,
                Data = data
            };
        }

        /// <inheritdoc />
        public CredentialManagementResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new CredentialManagementResponse(responseApdu);

        // This implements CborHelpers.CborEncodeDelegate.
        private static byte[] WriteEncodedParams(byte[]? encodedParams) => encodedParams ?? Array.Empty<byte>();
    }
}
