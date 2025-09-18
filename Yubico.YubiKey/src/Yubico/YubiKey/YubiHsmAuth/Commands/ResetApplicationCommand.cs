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

namespace Yubico.YubiKey.YubiHsmAuth.Commands;

/// <summary>
///     Reset the YubiHSM Auth application, which will delete all credentials,
///     reset the management key to its default value (all zeros), and reset
///     the management key retry counter to 8.
/// </summary>
/// <remarks>
///     The associated response class is <see cref="ResetApplicationResponse" />.
/// </remarks>
public sealed class ResetApplicationCommand : IYubiKeyCommand<ResetApplicationResponse>
{
    private const byte ResetAppInstruction = 0x06;
    private const byte ResetAppP1 = 0xde;
    private const byte ResetAppP2 = 0xad;

    /// <summary>
    ///     Constructs an instance of the <see cref="ResetApplicationCommand" /> class.
    /// </summary>
    public ResetApplicationCommand()
    {
    }

    #region IYubiKeyCommand<ResetApplicationResponse> Members

    /// <summary>
    ///     Gets the <see cref="YubiKeyApplication" /> to which this command belongs.
    /// </summary>
    /// <value>
    ///     <see cref="YubiKeyApplication.YubiHsmAuth" />
    /// </value>
    public YubiKeyApplication Application => YubiKeyApplication.YubiHsmAuth;

    /// <inheritdoc />
    public CommandApdu CreateCommandApdu() =>
        new()
        {
            Ins = ResetAppInstruction,
            P1 = ResetAppP1,
            P2 = ResetAppP2
        };

    /// <inheritdoc />
    public ResetApplicationResponse CreateResponseForApdu(ResponseApdu responseApdu) => new(responseApdu);

    #endregion
}
