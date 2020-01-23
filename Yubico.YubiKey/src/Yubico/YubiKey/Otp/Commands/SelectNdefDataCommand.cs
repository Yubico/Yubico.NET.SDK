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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Otp.Commands
{
    /// <summary>
    /// Selects the file containing the YubiKey's NDEF data. This must be sent prior to sending the
    /// <see cref="ReadNdefDataCommand"/> command. Note that this command only works over NFC.
    /// </summary>
    public class SelectNdefDataCommand : IYubiKeyCommand<OtpResponse>
    {
        private const byte SelectNdefDataInstruction = 0xA4;
        private const byte SelectNdefParameter2 = 0x0C;

        /// <summary>
        /// Indicates which file should be selected when this command is issued. Defaults to Ndef.
        /// </summary>
        public NdefFileId FileID { get; set; } = NdefFileId.Ndef;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.OtpNdef
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.OtpNdef;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectNdefDataCommand"/> class.
        /// </summary>
        public SelectNdefDataCommand()
        {

        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu()
        {
            Ins = SelectNdefDataInstruction,
            P2 = SelectNdefParameter2,
            Data = new byte[]
            {
                (byte)(((short)FileID >> 8) & 0xFF),
                (byte)((short)FileID & 0xFF)
            }
        };

        /// <inheritdoc />
        public OtpResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new OtpResponse(responseApdu);
    }
}
