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

namespace Yubico.YubiKey.U2f.Commands
{
    /// <summary>
    /// Determines if the YubiKey is in a FIPS-approved operating mode.
    /// </summary>
    /// <remarks>
    /// For the YubiKey FIPS U2F sub-module to be in a FIPS approved mode of operation, an Admin PIN must be set.
    /// By default, no Admin PIN is set. Further, if the YubiKey FIPS U2F sub-module has been reset,
    /// it cannot be set into a FIPS approved mode of operation, even with the Admin PIN set.
    /// </remarks>
    public sealed class VerifyFipsModeCommand : IYubiKeyCommand<VerifyFipsModeResponse>
    {
        private const byte Ctap1MessageInstruction = 0x03;
        private const byte VerifyFipsModeInstruction = 0x46;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.FidoU2f
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.FidoU2f;

        /// <summary>
        /// Constructs an instance of the <see cref="VerifyFipsModeCommand" /> class.
        /// </summary>
        public VerifyFipsModeCommand()
        {
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            var innerCommand = new CommandApdu()
            {
                Ins = VerifyFipsModeInstruction,
            };

            return new CommandApdu()
            {
                Ins = Ctap1MessageInstruction,
                Data = innerCommand.AsByteArray(ApduEncoding.ExtendedLength),
            };
        }

        /// <inheritdoc />
        public VerifyFipsModeResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new VerifyFipsModeResponse(responseApdu);
    }
}
