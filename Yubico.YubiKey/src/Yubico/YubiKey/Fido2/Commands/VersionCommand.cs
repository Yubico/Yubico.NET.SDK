// Copyright 2021 Yubico AB
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

using System.Security.Cryptography;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Command to get the device firmware version.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="VersionResponse"/>.
    /// <p>
    /// This command does not work over NFC - it must be run over CTAPHID.
    /// </p>
    /// <p>
    /// Example:
    /// </p>
    /// <code language="csharp">
    /// IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Fido2);
    /// VersionCommand versionCmd = new VersionCommand();
    /// VersionResponse versionRsp = connection.SendCommand(versionCmd);
    /// if (versionNum.Status == ResponseStatus.Success)
    /// {
    ///     FirmwareVersion versionNum = versionRsp.GetData();
    /// }
    /// </code>
    /// </remarks>
    internal sealed class VersionCommand : IYubiKeyCommand<VersionResponse>
    {
        public YubiKeyApplication Application => YubiKeyApplication.Fido2;

        private readonly RandomNumberGenerator _rng;

        /// <summary>
        /// Initializes a new instance of the VersionCommand class.
        /// </summary>
        public VersionCommand() : this(RandomNumberGenerator.Create())
        {

        }

        /// <summary>
        /// Initializes a new instance of the VersionCommand class.
        /// </summary>
        public VersionCommand(RandomNumberGenerator rng)
        {
            _rng = rng;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            byte[] payload = new byte[8];

            _rng.GetBytes(payload, 0, 8);

            return new CommandApdu()
            {
                Ins = (byte)CtapHidCommand.InitializeChannel,
                Data = payload
            };
        }

        /// <inheritdoc />
        public VersionResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new VersionResponse(responseApdu);
    }
}
