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
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2.Commands;

/// <summary>
///     Get the YubiKey's FIDO2 credential metadata.
/// </summary>
/// <remarks>
///     The partner Response class is <see cref="CredentialManagementResponse" />.
///     <para>
///         This returns metadata on all the credentials. The return from this
///         command consists of the <c>existingResidentCredentialsCount</c> and
///         <c>maxPossibleRemainingResidentCredentialsCount</c> (section 6.8 of the
///         standard).
///     </para>
/// </remarks>
public class GetCredentialMetadataCommand : CredentialMgmtSubCommand, IYubiKeyCommand<GetCredentialMetadataResponse>
{
    private const int SubCmdGetMetadata = 0x01;

    // The default constructor explicitly defined. We don't want it to be
    // used.
    private GetCredentialMetadataCommand()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Constructs a new instance of <see cref="GetCredentialMetadataCommand" />.
    /// </summary>
    /// <param name="pinUvAuthToken">
    ///     The PIN/UV Auth Token built from the PIN. This is the encrypted token
    ///     key.
    /// </param>
    /// <param name="authProtocol">
    ///     The Auth Protocol used to build the Auth Token.
    /// </param>
    public GetCredentialMetadataCommand(
        ReadOnlyMemory<byte> pinUvAuthToken,
        PinUvAuthProtocolBase authProtocol)
        : base(
            new CredentialManagementCommand(
                SubCmdGetMetadata,
                null,
                pinUvAuthToken,
                authProtocol))
    {
    }

    /// <summary>
    ///     Constructs a new instance of <see cref="GetCredentialMetadataCommand" /> with a pre-computed PIN/UV auth param.
    /// </summary>
    /// <param name="pinUvAuthParam">
    ///     The pre-computed PIN/UV auth param for this command.
    /// </param>
    /// <param name="protocol">
    ///     The PIN/UV protocol version used to compute the auth param.
    /// </param>
    public GetCredentialMetadataCommand(
        ReadOnlyMemory<byte> pinUvAuthParam,
        PinUvAuthProtocol protocol)
        : base(new CredentialManagementCommand(SubCmdGetMetadata, null, pinUvAuthParam, protocol))
    {
    }

    #region IYubiKeyCommand<GetCredentialMetadataResponse> Members

    /// <inheritdoc />
    public GetCredentialMetadataResponse CreateResponseForApdu(ResponseApdu responseApdu) => new(responseApdu);

    #endregion

    /// <summary>
    ///     Creates the authentication message for this command, consisting of only the subcommand byte.
    /// </summary>
    /// <returns>
    ///     The message to be used for PIN/UV authentication.
    /// </returns>
    public static byte[] GetAuthenticationMessage() => new byte[] { SubCmdGetMetadata };
}
