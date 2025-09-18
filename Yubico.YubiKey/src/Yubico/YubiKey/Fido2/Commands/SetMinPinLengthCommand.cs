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
using System.Collections.Generic;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2.Commands;

/// <summary>
///     Set the minimum PIN length, and/or provide a list of relying party IDs
///     that specify which relying parties can see the minimum PIN length, and/or
///     specify whether the PIN must be changed before PIN verification can
///     succeed.
/// </summary>
/// <remarks>
///     The partner Response class is <see cref="Fido2Response" />. This command
///     does not return any data, it only returns "success" or "failure", and has
///     some FIDO2-specific error information.
///     <para>
///         This command is valid only if the "setMinPINLength" option is present and
///         set to <c>true</c>.
///     </para>
///     <para>
///         For any call to this command, it will perform any combination of one,
///         two, or three of the operations. Each operation is optional, including
///         setting a new minimum PIN length. For example, if you want to only set
///         the list of RP Ids, you can do so using this command with a null
///         <c>newMinPinLength</c> and a null <c>forceChangePin</c>.
///     </para>
///     <para>
///         The YubiKey's FIDO2 application is manufactured with a minimum PIN
///         length. Users that want a different length can call this command.
///         However, it is not possible to set the minimum PIN length to a value less
///         than the current minimum. The only way to possibly set a shorter minimum
///         PIN length is to reset the entire FIDO2 application on the given YubiKey.
///     </para>
///     <para>
///         The PIN length is measured in code points. See the User's Manual entry on
///         <xref href="TheFido2Pin">the FIDO2 PIN</xref> for more information on PIN
///         composition.
///     </para>
///     <para>
///         Note that the standard specifies that a PIN cannot be less than 4 Unicode
///         characters and no more than 63 bytes when encoded as UTF-8. Hence, there
///         are limits to the new minimum PIN length.
///     </para>
///     <para>
///         The list of RP IDs will specify that any RP on the list is allowed to see
///         the minimum PIN length of a YubiKey. That will be visible only during the
///         MakeCredential process. Generally, it is used so that an RP will refuse
///         to provide a credential to an authenticator if the minimum PIN length is
///         too low.
///     </para>
///     <para>
///         It is possible for a YubiKey to be manufactured with a pre-configured
///         list of RP IDs. That list will never change, even after reset. If RP IDs
///         are added using the SetMinPINLength command, they will be IDs in addition
///         to the pre-configured list.
///     </para>
///     <para>
///         If RP IDs are added using this command, they will replace any RP IDs that
///         had been added during a previous call to this command. Note that there is
///         no way to get the current list.
///     </para>
///     <para>
///         If the minimum PIN length is set, and if the current PIN is smaller than
///         this value, then the YubiKey will require the user to change the PIN. It
///         will not verify the current PIN and any operation that requires
///         PIN verification will fail until the PIN is changed to a value that meets
///         the new requirement. For example, suppose the current minimum PIN length
///         is 4 and you have a PIN of length 6. You set the minimum PIN length to 7,
///         but do not set <c>forceChangePin</c> (you pass in null for that arg). The
///         YubiKey will still require the user change the PIN.
///     </para>
///     <para>
///         If <c>forceChangePin</c> is true, then the caller is requiring the user
///         to change the PIN, no matter what.
///     </para>
///     <para>
///         You can know if a PIN must be changed (either because the min PIN length
///         is now longer than the existing PIN or the <c>forceChangePin</c> was
///         set), look at the <see cref="AuthenticatorInfo.ForcePinChange" /> property
///         in the <c>AuthenticatorInfo</c>.
///     </para>
/// </remarks>
public class SetMinPinLengthCommand : IYubiKeyCommand<Fido2Response>
{
    private const int SubCmdSetMinPinLength = 0x03;
    private const int KeyMinPinLen = 1;
    private const int KeyRpIds = 2;
    private const int KeyForceChangePin = 3;

    private readonly ConfigCommand _command;

    // The default constructor explicitly defined. We don't want it to be
    // used.
    private SetMinPinLengthCommand()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Constructs a new instance of <see cref="SetMinPinLengthCommand" />.
    /// </summary>
    /// <remarks>
    ///     There are up to three elements to set with this command: a new
    ///     minimum PIN length, a new list of relying party IDs, and an
    ///     indication to require the user change the PIN. All three are
    ///     optional, although this command will do nothing if none are set (i.e.
    ///     the first three args are <c>null, null, null</c>).
    ///     <para>
    ///         If you want to set an element, provide a value for the corresponding
    ///         argument, otherwise, pass in null.
    ///     </para>
    ///     <para>
    ///         If you want to force a PIN change, pass in <c>true</c> for the
    ///         <c>forceChangePin</c> arg. If you pass in <c>false</c>, this class
    ///         will consider it the same as null. That is, the <c>forceChangePin</c>
    ///         element of this command is optional, meaning the command does not
    ///         need to include the element (i.e. leave it blank in the command sent
    ///         to the YubiKey). If you pass in <c>false</c>, this class will send
    ///         the command without that element (i.e., it will be left blank).
    ///     </para>
    /// </remarks>
    /// <param name="newMinPinLength">
    ///     The new PIN length, measured in code points. See the User's Manual
    ///     entry on <xref href="TheFido2Pin">the FIDO2 PIN</xref> for more
    ///     information on PIN composition. Pass in null to indicate the command
    ///     should not change the minimum PIN length.
    /// </param>
    /// <param name="relyingPartyIds">
    ///     A list of strings that are the relying party IDs for those RPs that
    ///     are allowed to see the minimum PIN length. Pass in null to indicate
    ///     the command should not add any RP IDs.
    /// </param>
    /// <param name="forceChangePin">
    ///     If you want to set the YubiKey to require the user change the PIN
    ///     before the verification event, pass in <c>true</c>. If you pass in
    ///     null or <c>false</c>, this command will consider the force PIN option
    ///     not taken.
    /// </param>
    /// <param name="pinUvAuthToken">
    ///     The PIN/UV Auth Token built from the PIN. This is the encrypted token
    ///     key.
    /// </param>
    /// <param name="authProtocol">
    ///     The Auth Protocol used to build the Auth Token.
    /// </param>
    public SetMinPinLengthCommand(
        int? newMinPinLength,
        IReadOnlyList<string>? relyingPartyIds,
        bool? forceChangePin,
        ReadOnlyMemory<byte> pinUvAuthToken,
        PinUvAuthProtocolBase authProtocol)
    {
        bool? forceChange = null;
        if (forceChangePin is not null && forceChangePin.Value)
        {
            forceChange = true;
        }

        _command = new ConfigCommand(
            SubCmdSetMinPinLength,
            EncodeParams(newMinPinLength, relyingPartyIds, forceChange),
            pinUvAuthToken,
            authProtocol);
    }

    #region IYubiKeyCommand<Fido2Response> Members

    /// <inheritdoc />
    public YubiKeyApplication Application => _command.Application;

    /// <inheritdoc />
    public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

    /// <inheritdoc />
    public Fido2Response CreateResponseForApdu(ResponseApdu responseApdu) => new ConfigResponse(responseApdu);

    #endregion

    // This method encodes the parameters. For
    // SetMinPINLength, the parameters consist of the minPinLength, list of
    // RPIDs, and the forceChangePin. If either or both of the first two are
    // null, they are not to be encoded. If the third is false, it is not to
    // be encoded.
    // It is encoded as
    //   map
    //     01 int
    //     02 array
    //     03 bool
    private static byte[]? EncodeParams(int? minPinLength, IReadOnlyList<string>? rpIds, bool? forceChangePin)
    {
        if (minPinLength is null && rpIds is null && forceChangePin is null)
        {
            return null;
        }

        return new CborMapWriter<int>()
            .OptionalEntry(KeyMinPinLen, minPinLength)
            .OptionalEntry<IReadOnlyList<string>>(KeyRpIds, CborHelpers.EncodeStringArray, rpIds)
            .OptionalEntry(KeyForceChangePin, forceChangePin)
            .Encode();
    }
}
