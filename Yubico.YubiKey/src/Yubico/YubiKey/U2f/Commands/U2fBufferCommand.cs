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
    /// The base class for some U2F commands that use the U2fBuffer.
    /// </summary>
    /// <remarks>
    /// Only the SDK will ever need to create subclasses, there is no reason for
    /// any other application to do so.
    /// </remarks>
    public abstract class U2fBufferCommand : U2fBuffer
    {
        private const byte Ctap1MessageInstruction = 0x03;

        private readonly byte _instruction;

        /// <summary>
        /// The P1 value to use in the APDU.
        /// </summary>
        protected byte Parameter1 { get; set; }

        /// <summary>
        /// The YubiKeyApplication to which this command belongs.
        /// </summary>
        /// <value>
        /// <see cref="YubiKeyApplication.FidoU2f"/>
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.FidoU2f;

        /// <summary>
        /// Initialize the object to the given values.
        /// </summary>
        protected U2fBufferCommand(byte instruction, int bufferLength, int appIdOffset, int clientDataOffset)
            : base(bufferLength, appIdOffset, clientDataOffset)
        {
            _instruction = instruction;
        }

        /// <summary>
        /// Create a U2F Command APDU using the info provided during the life of
        /// this object.
        /// </summary>
        public CommandApdu CreateCommandApdu()
        {
            var innerCommand = new CommandApdu()
            {
                Ins = _instruction,
                P1 = Parameter1,
                Data = _buffer,
            };

            return new CommandApdu()
            {
                Ins = Ctap1MessageInstruction,
                Data = innerCommand.AsByteArray(ApduEncoding.ExtendedLength),
            };
        }
    }
}
