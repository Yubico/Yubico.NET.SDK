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

namespace Yubico.YubiKey.Fido2.Commands;

/// <summary>
///     Gets the number of PIN retries remaining for FIDO2.
/// </summary>
/// <remarks>
///     <para>
///         When verifying a FIDO2 PIN, it is possible that the user will incorrectly type it in and it will fail. Fail
///         enough times in a row, and the YubiKey may block further authentication attempts. Once this has happened, the
///         YubiKey FIDO application must be reset - resulting in the loss of all FIDO credentials.
///     </para>
///     <para>
///         While this may seem catastrophic, it is actually a valuable protection mechanism against attackers guessing at
///         the YubiKey's PIN. The range of possible PINs far exceeds the limited number of guesses available to the user.
///         By locking out the FIDO application, an attacker is denied the opportunity of unlimited guessing.
///     </para>
///     <para>
///         For non-malicious cases, where a user simply mistyped their PIN, the user will likely never exhaust the number
///         of allowed retries. This is because the retry counter is reset to the configured number of retries once a valid
///         PIN has been entered. For example: If the retry counter started with 8 retries and you enter in 4 false
///         guesses,
///         the retry counter will be reset to 8 if you enter the correct PIN on the 5th retry.
///     </para>
///     <para>
///         The number of allowable retries is configurable, and may differ between YubiKeys. By default, it is set to 8
///         retries. This command will return the current number of remaining retries for this particular YubiKey. Use the
///         value returned by this command's partner response class instead of making any assumptions as to the number of
///         retries remaining.
///     </para>
/// </remarks>
public class GetPinRetriesCommand : IYubiKeyCommand<GetPinRetriesResponse>
{
    private const int SubCmdGetPinRetries = 0x01;
    private readonly ClientPinCommand _command;

    /// <summary>
    ///     Constructs a new instance of <see cref="GetPinRetriesCommand" />.
    /// </summary>
    public GetPinRetriesCommand()
    {
        _command = new ClientPinCommand
        {
            SubCommand = SubCmdGetPinRetries
        };
    }

    #region IYubiKeyCommand<GetPinRetriesResponse> Members

    /// <inheritdoc />
    public YubiKeyApplication Application => _command.Application;

    /// <inheritdoc />
    public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

    /// <inheritdoc />
    public GetPinRetriesResponse CreateResponseForApdu(ResponseApdu responseApdu) => new(responseApdu);

    #endregion
}
