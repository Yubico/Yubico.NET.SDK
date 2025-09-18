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

namespace Yubico.YubiKey.Fido2.Commands;

/// <summary>
///     The <see cref="BioEnrollmentCommand" /> is the class for
///     <c>authenticatorBioEnrollment</c>. This command has a number of
///     subcommands, each of which is represented by its own class.
/// </summary>
/// <remarks>
///     The <c>authenticatorBioEnrollment (0x09)</c> FIDO2 command can be
///     thought of as a "meta" command. That is, it provides the structure and
///     mechanism for performing a number of subcommands. These subcommands are:
///     <code language="adoc">
/// - enrollBegin (0x01)
/// - enrollCaptureNextSample (0x02)
/// - cancelCurrentEnrollment (0x03)
/// - enumerateEnrollments (0x04)
/// - setFriendlyName (0x05)
/// - removeEnrollment (0x06)
/// - getFingerprintSensorInfo (0x07)
/// </code>
///     Since the SDK does not have the concept of a subcommand natively, these
///     are all exposed as their own separate commands.
///     <para>
///         See the user manual entry on
///         <xref href="Fido2BioEnrollment">Bio Enrollment</xref> for a more in depth
///         guide to enrolling fingerprints within FIDO2. For more information on a
///         particular subcommand, see the API reference documentation for that
///         command class.
///     </para>
///     <para>
///         Some of the subcommands return data (e.g. a template ID), others return
///         only a success or failure response code.
///     </para>
/// </remarks>
public class BioEnrollmentCommand : IYubiKeyCommand<BioEnrollmentResponse>
{
    // Command constants
    private const int ModalityValue = 1;
    private const byte FingerprintValue = 1;

    private const int TagModality = 1;
    private const int TagSubCommand = 2;
    private const int TagParams = 3;
    private const int TagProtocol = 4;
    private const int TagPinUvAuthParam = 5;
    private const int TagGetModality = 6;

    private readonly byte[]? _encodedParams;
    private readonly int? _protocol;

    /// <summary>
    ///     This constructor will throw <c>NotImplementedException</c>. It is the
    ///     default constructor explicitly defined. We don't want it to be used.
    ///     It is made <c>protected</c> rather than <c>private</c> because there
    ///     are subclasses.
    /// </summary>
    protected BioEnrollmentCommand()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Constructs a new instance of <see cref="BioEnrollmentCommand" />.
    /// </summary>
    /// <remarks>
    ///     Note that if the command does not need the <c>pinUvAuthToken</c> and
    ///     <c>authProtocol</c>, use the constructor that takes only the
    ///     <c>subCommand</c>.
    /// </remarks>
    /// <param name="subCommand">
    ///     The byte representing the subcommand to execute.
    /// </param>
    /// <param name="subCommandParams">
    ///     The parameters needed in order to execute the subcommand. Not all
    ///     subcommands have parameters, so this can be null.
    /// </param>
    /// <param name="pinUvAuthToken">
    ///     The PIN/UV Auth Token built from the PIN. This is the encrypted token
    ///     key.
    /// </param>
    /// <param name="authProtocol">
    ///     The Auth Protocol used to build the Auth Token.
    /// </param>
    public BioEnrollmentCommand(
        int subCommand,
        byte[]? subCommandParams,
        ReadOnlyMemory<byte> pinUvAuthToken,
        PinUvAuthProtocolBase authProtocol)
    {
        if (authProtocol is null)
        {
            throw new ArgumentNullException(nameof(authProtocol));
        }

        SubCommand = subCommand;
        _encodedParams = subCommandParams;
        _protocol = (int)authProtocol.Protocol;

        // If there are no params, the contents consists of the two bytes
        // Fingerprint (0x01) || subCommand.
        // If there are params, the contents consist of
        // Fingerprint (0x01) || subCommand || params.
        int length = subCommandParams?.Length ?? 0;
        byte[] message = new byte[length + 2];
        message[0] = FingerprintValue;
        message[1] = (byte)subCommand;
        subCommandParams?.CopyTo(message, 2);

        // The pinUvAuthToken is an encrypted value, so there's no need to
        // overwrite the array.
        byte[] authParam = authProtocol.AuthenticateUsingPinToken(pinUvAuthToken.ToArray(), message);
        PinUvAuthParam = new ReadOnlyMemory<byte>(authParam);
        PinUvAuthProtocol = authProtocol.Protocol;
    }

    /// <summary>
    ///     Constructs a new instance of <see cref="BioEnrollmentCommand" />.
    /// </summary>
    /// <param name="subCommand">
    ///     The byte representing the subcommand to execute.
    /// </param>
    public BioEnrollmentCommand(int subCommand)
    {
        SubCommand = subCommand;
        _encodedParams = null;
        PinUvAuthProtocol = null;
        PinUvAuthParam = null;
    }

    /// <summary>
    ///     The BioEnrollment subcommand to issue to the YubiKey.
    /// </summary>
    /// <remarks>
    ///     This is a mandatory parameter, and must be one of the following values:
    ///     <code language="adoc">
    /// - enrollBegin (0x01)
    /// - enrollCaptureNextSample (0x02)
    /// - cancelCurrentEnrollment (0x03)
    /// - enumerateEnrollments (0x04)
    /// - setFriendlyName (0x05)
    /// - removeEnrollment (0x06)
    /// - getFingerprintSensorInfo (0x07)
    /// </code>
    ///     There is one other value this property can possess, and that is zero
    ///     for <c>getModality</c>. The standard does not list <c>getModality</c>
    ///     as a subcommand, but specifies <c>getModality</c> as an operation of
    ///     <c>authenticatorBioEnrollment</c> executed as a subcommand. That is,
    ///     <c>getModality</c> is a subcommand, but not documented as such. If
    ///     the <c>SubCommand</c> property is set to zero, then this class will
    ///     build the BioEnrollment command to perform <c>getModality</c>.
    /// </remarks>
    public int SubCommand { get; }

    /// <summary>
    ///     The encoded params for the specified subcommand. If a subcommand
    ///     has no parameters, this will be null.
    /// </summary>
    public ReadOnlyMemory<byte>? SubCommandParameters => _encodedParams?.AsMemory();

    /// <summary>
    ///     The PIN/UV protocol version chosen by the platform.
    /// </summary>
    /// <remarks>
    ///     A PIN/UV protocol must be used when performing some of the
    ///     BioEnrollment operations. The specified protocol must be one
    ///     of the protocols that are supported by the YubiKey. This can be
    ///     determined by issuing the AuthenticatorGetInfo command.
    /// </remarks>
    public PinUvAuthProtocol? PinUvAuthProtocol { get; private set; }

    /// <summary>
    ///     The output of calling authenticate on the PIN/UV protocol specific to
    ///     a particular subcommand.
    /// </summary>
    /// <remarks>
    ///     See the User's Manual entry on
    ///     <xref href="Fido2PinProtocol">PIN protocols</xref> for a much more in
    ///     depth guide to working with PINs within FIDO2.
    ///     <para>
    ///         See also the User's Manual entry on
    ///         <xref href="Fido2BioEnrollment">FIDO2 Bio Enrollment</xref>
    ///         for more information on building the <c>PIN/UV Auth Param</c>
    ///         specific to the BioEnrollment commands.
    ///     </para>
    /// </remarks>
    public ReadOnlyMemory<byte>? PinUvAuthParam { get; }

    #region IYubiKeyCommand<BioEnrollmentResponse> Members

    /// <inheritdoc />
    public YubiKeyApplication Application => YubiKeyApplication.Fido2;

    /// <summary>
    ///     Creates a well-formed CommandApdu to send to the YubiKey.
    /// </summary>
    /// <remarks>
    ///     This method will first perform validation on all of the parameters and data provided
    ///     to it. The CommandApdu it creates should contain all of the data payload for the
    ///     command, even if it exceeds 65,535 bytes as specified by the ISO 7816-4 specification.
    ///     The APDU will be properly chained by the device connection prior to being sent to the
    ///     YubiKey, and the responses will be collapsed into a single result.
    /// </remarks>
    /// <returns>
    ///     A valid CommandApdu that is ready to be sent to the YubiKey, or passed along
    ///     to additional encoders for further processing.
    /// </returns>
    public CommandApdu CreateCommandApdu()
    {
        var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, true);
        int? subcommand = null;
        int? modality = null;
        bool? getModality = true;
        if (SubCommand != 0)
        {
            subcommand = SubCommand;
            modality = ModalityValue;
            getModality = null;
        }

        CborHelpers.BeginMap<int>(cbor)
            .OptionalEntry(TagModality, modality)
            .OptionalEntry(TagSubCommand, subcommand)
            .OptionalEntry(TagParams, WriteEncodedParams, _encodedParams)
            .OptionalEntry(TagProtocol, _protocol)
            .OptionalEntry(TagPinUvAuthParam, PinUvAuthParam)
            .OptionalEntry(TagGetModality, getModality)
            .EndMap();

        byte[] data = new byte[1 + cbor.BytesWritten];
        int bytesWritten = cbor.Encode(data.AsSpan(1));

        if (bytesWritten != data.Length - 1)
        {
            throw new Ctap2DataException(ExceptionMessages.CborLengthMismatch);
        }

        data[0] = CtapConstants.CtapBioEnrollCmd;

        return new CommandApdu
        {
            Ins = CtapConstants.CtapHidCbor,
            Data = data
        };
    }

    /// <inheritdoc />
    public BioEnrollmentResponse CreateResponseForApdu(ResponseApdu responseApdu) => new(responseApdu);

    #endregion

    // This implements CborHelpers.CborEncodeDelegate.
    private static byte[] WriteEncodedParams(byte[]? encodedParams) => encodedParams ?? Array.Empty<byte>();
}
