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
using Yubico.YubiKey.Fido2.Serialization;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Command to create a FIDO2 credential on the YubiKey.
    /// </summary>
    /// <remarks>
    /// <p>
    /// This command takes as input a <see cref="MakeCredentialInput"/>, and produces a <see cref="MakeCredentialResponse"/>
    /// response containing <see cref="IMakeCredentialOutput"/> as its data.
    /// </p>
    /// <p>
    /// This command may require that the user tap their device to complete the assertion.
    /// </p>
    /// <p>
    /// On certain platforms, accessing a FIDO device over HID may require that the 
    /// application is running with elevated permissions.
    /// </p>
    /// </remarks>
    internal sealed class MakeCredentialCommand : IYubiKeyCommand<MakeCredentialResponse>
    {
        private const byte CtapMakeCredentialCmd = 0x01;

        public YubiKeyApplication Application => YubiKeyApplication.Fido2;

        private readonly MakeCredentialInput _makeCredentialInput;

        /// <summary>
        /// Initializes a new instance of the MakeCredentialCommand class.
        /// </summary>
        /// <remarks>
        /// Initialization with invalid input will trigger an <see cref="Ctap2DataException"/>.
        /// </remarks>
        public MakeCredentialCommand(MakeCredentialInput makeCredentialInput)
        {
            if (makeCredentialInput is null)
            {
                throw new ArgumentNullException(nameof(makeCredentialInput));
            }

            makeCredentialInput.Validate();

            _makeCredentialInput = makeCredentialInput;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            byte[] cborData = Ctap2CborSerializer.Serialize(_makeCredentialInput);

            byte[] payload = new byte[1 + cborData.Length];
            payload[0] = CtapMakeCredentialCmd;

            cborData.CopyTo(payload, 1);

            return new CommandApdu()
            {
                Ins = (byte)CtapHidCommand.Cbor,
                Data = payload
            };
        }
         
        /// <inheritdoc />
        public MakeCredentialResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new MakeCredentialResponse(responseApdu);
    }
}
