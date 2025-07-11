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

namespace Yubico.YubiKey.Otp.Commands
{
    /// <summary>
    /// Reads the NDEF data over an NFC connection to the YubiKey.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The NDEF data file must be selected first using the <see cref="SelectNdefDataCommand"/> class.
    /// </para>
    /// <para>
    /// This command will only succeed if the YubiKey is connected through an NFC reader.
    /// It will not work over CCID if plugged in by USB or EAP.
    /// </para>
    /// </remarks>
    public class ReadNdefDataCommand : IYubiKeyCommand<ReadNdefDataResponse>
    {
        private const byte ReadNdefDataInstruction = 0xB0; // Same as ISO READ_DATA command

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.OtpNdef
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.OtpNdef;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadNdefDataCommand"/> class.
        /// </summary>
        public ReadNdefDataCommand()
        {

        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu()
        {
            Ins = ReadNdefDataInstruction
        };

        /// <inheritdoc />
        public ReadNdefDataResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ReadNdefDataResponse(responseApdu);
    }
}
