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

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    /// <summary>
    /// Get the number of retries remaining for the management key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There is a limit of 8 attempts to authenticate with the management key
    /// before the management key is blocked. Once the management key is
    /// blocked, the application must be reset before performing operations
    /// which require authentication with the management key (such as adding
    /// credentials, deleting credentials, and changing the management key).
    /// To reset the application, see <see cref="ResetApplicationCommand"/>.
    /// Supplying the correct management key before the management key is
    /// blocked will reset the retry counter to 8.
    /// </para>
    /// <para>
    /// The associated response class is <see cref="GetManagementKeyRetriesResponse"/>.
    /// </para>
    /// </remarks>
    public sealed class GetManagementKeyRetriesCommand : IYubiKeyCommand<GetManagementKeyRetriesResponse>
    {
        private const byte MgmtRetriesInstruction = 0x09;

        /// <summary>
        /// Gets the <see cref="YubiKeyApplication"/> to which this command belongs.
        /// </summary>
        /// <value>
        /// <see cref="YubiKeyApplication.YubiHsmAuth"/>
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.YubiHsmAuth;

        /// <summary>
        /// Constructs an instance of the <see cref="GetManagementKeyRetriesCommand"/> class.
        /// </summary>
        public GetManagementKeyRetriesCommand()
        {
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu()
        {
            Ins = MgmtRetriesInstruction
        };

        /// <inheritdoc />
        public GetManagementKeyRetriesResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetManagementKeyRetriesResponse(responseApdu);
    }
}
