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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Scp.Commands;

/// <summary>
///     This command is used to reset the SCP keys on the YubiKey device back to its factory default state
///     In order to reset the YubiKey to its factory default state, one must issue the reset command to the Yubikey
///     with incorrect parameters 65 times. This will block the keys and reset the YubiKey to its factory default state.
/// </summary>
internal class ResetCommand : IYubiKeyCommand<YubiKeyResponse>
{
    private readonly byte[] _data;
    private readonly byte _ins;
    private readonly byte _keyVersionNumber;
    private readonly byte _kid;

    /// <summary>
    ///     Initialize a new instance of the <see cref="ResetCommand" />
    ///     Clients should not generally build this manually. Instead, use the
    ///     <see cref="SecurityDomainSession.Reset" /> to build commands.
    /// </summary>
    /// <param name="ins">
    ///     The instruction byte for the command
    ///     the instruction bytes that are valid are,
    ///     <see cref="InitializeUpdateCommand.GpInitializeUpdateIns" />,
    ///     <see cref="ExternalAuthenticateCommand.GpExternalAuthenticateIns" />,
    ///     <see cref="InternalAuthenticateCommand.GpInternalAuthenticateIns" />,
    ///     <see cref="PerformSecurityOperationCommand.GpPerformSecurityOperationIns" />
    /// </param>
    /// <param name="keyVersionNumber">The version number of the key</param>
    /// <param name="kid">The Key id</param>
    /// <param name="data">The data to be reset</param>
    public ResetCommand(byte ins, byte keyVersionNumber, byte kid, byte[] data)
    {
        _ins = ins;
        _data = data;
        _keyVersionNumber = keyVersionNumber;
        _kid = kid;
    }

    #region IYubiKeyCommand<YubiKeyResponse> Members

    public YubiKeyApplication Application => YubiKeyApplication.SecurityDomain;

    public CommandApdu CreateCommandApdu() =>
        new()
        {
            Cla = 0x80,
            Ins = _ins,
            P1 = _keyVersionNumber,
            P2 = _kid,
            Data = _data
        };

    public YubiKeyResponse CreateResponseForApdu(ResponseApdu responseApdu) => new(responseApdu);

    #endregion
}
