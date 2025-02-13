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
    /// Get the version of the YubiHSM Auth application as a major, minor, and
    /// patch value.
    /// </summary>
    /// <remarks>
    /// The associated response class is <see cref="GetApplicationVersionResponse"/>.
    /// </remarks>
    public sealed class GetApplicationVersionCommand : IYubiKeyCommand<GetApplicationVersionResponse>
    {
        private const byte GetApplicationVersionInstruction = 0x07;

        /// <summary>
        /// Gets the <see cref="YubiKeyApplication"/> to which this command belongs.
        /// </summary>
        /// <value>
        /// <see cref="YubiKeyApplication.YubiHsmAuth"/>
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.YubiHsmAuth;

        /// <summary>
        /// Constructs an instance of the <see cref="GetApplicationVersionCommand"/> class.
        /// </summary>
        public GetApplicationVersionCommand()
        {
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu()
        {
            Ins = GetApplicationVersionInstruction
        };

        /// <inheritdoc />
        public GetApplicationVersionResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetApplicationVersionResponse(responseApdu);
    }
}
