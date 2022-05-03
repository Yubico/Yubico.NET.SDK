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

using System;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    /// <summary>
    /// Sends data to the YubiKey which immediately echoes the same
    /// data back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This command is defined to be a uniform function for debugging,
    /// latency, and performance measurements.
    /// </para>
    /// <para>
    /// Behavior for <see cref="Data"/> larger than 1024 bytes is undefined.
    /// </para>
    /// </remarks>
    public sealed class EchoCommand : IYubiKeyCommand<EchoResponse>
    {
        private const byte Ctap1MessageInstruction = 0x03;
        private const byte EchoInstruction = 0x40;

        /// <summary>
        /// The data to send to the YubiKey.
        /// </summary>
        /// <remarks>
        /// Behavior for <see cref="Data"/> larger than 1024 bytes is undefined.
        /// </remarks>
        public ReadOnlyMemory<byte> Data { get; set; }

        /// <summary>
        /// The YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// <see cref="YubiKeyApplication.FidoU2f"/>
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.FidoU2f;

        /// <summary>
        /// Constructs an instance of the <see cref="EchoCommand"/> class.
        /// </summary>
        public EchoCommand()
        {
        }

        /// <summary>
        /// Constructs an instance of the <see cref="EchoCommand"/> class with
        /// the data to send to the YubiKey.
        /// </summary>
        /// <param name="data">
        /// The data to send to the YubiKey. See <see cref="Data"/>.
        /// </param>
        public EchoCommand(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }

        /// <inheritdoc/>
        public CommandApdu CreateCommandApdu()
        {
            var innerEchoCommand = new CommandApdu()
            {
                Ins = EchoInstruction,
                Data = Data.ToArray(),
            };

            return new CommandApdu()
            {
                Ins = Ctap1MessageInstruction,
                Data = innerEchoCommand.AsByteArray(ApduEncoding.ExtendedLength),
            };
        }

        /// <inheritdoc/>
        public EchoResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new EchoResponse(responseApdu);
    }
}
