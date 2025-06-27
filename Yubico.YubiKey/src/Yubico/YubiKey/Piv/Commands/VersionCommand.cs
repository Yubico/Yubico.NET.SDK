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

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// Get the YubiKey's firmware version number.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="VersionResponse"/>.
    /// <para>
    /// Example:
    /// </para>
    /// <code language="csharp">
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);
    ///   VersionCommand versionCmd = new VersionCommand();
    ///   VersionResponse versionRsp = connection.SendCommand(versionCmd);<br/>
    ///   if (versionNum.Status == ResponseStatus.Success)
    ///   {
    ///       FirmwareVersion versionNum = versionRsp.GetData();
    ///   }
    /// </code>
    /// </remarks>
    public sealed class VersionCommand : IYubiKeyCommand<VersionResponse>
    {
        private const byte PivVersionInstruction = 0xFD;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs. For this
        /// command it's PIV.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Piv
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Piv;

        /// <summary>
        /// Initializes a new instance of the VersionCommand class. This command
        /// has no input.
        /// </summary>
        public VersionCommand()
        {
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu
        {
            Ins = PivVersionInstruction,
        };

        /// <inheritdoc />
        public VersionResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new VersionResponse(responseApdu);
    }
}
