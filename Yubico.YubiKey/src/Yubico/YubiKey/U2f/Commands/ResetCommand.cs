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
    /// Resets the YubiKey's U2F application back to a factory default state.
    /// </summary>
    /// <remarks>
    /// Reset on FIPS devices will wipe the attestation certificate from the device
    /// preventing the device from being able to be in FIPS-mode again.
    /// This reset behavior is specific to U2F on FIPS.
    /// </remarks>
    public sealed class ResetCommand : IYubiKeyCommand<ResetResponse>
    {
        private const byte Ctap1MessageInstruction = 0x03;
        private const byte ResetInstruction = 0x45;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.FidoU2f
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.FidoU2f;

        /// <summary>
        /// Constructs an instance of the <see cref="ResetCommand" /> class.
        /// </summary>
        public ResetCommand()
        {
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            var innerCommand = new CommandApdu()
            {
                Ins = ResetInstruction,
            };

            return new CommandApdu()
            {
                Ins = Ctap1MessageInstruction,
                Data = innerCommand.AsByteArray(ApduEncoding.ExtendedLength),
            };
        }

        /// <inheritdoc />
        public ResetResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ResetResponse(responseApdu);
    }
}
