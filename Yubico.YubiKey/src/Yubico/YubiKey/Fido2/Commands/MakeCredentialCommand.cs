// Copyright 2022 Yubico AB
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
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Instruct the YubiKey to make a credential based on the input parameters.
    /// </summary>
    public class MakeCredentialCommand : IYubiKeyCommand<MakeCredentialResponse>
    {
        /// <inheritdoc />
        public YubiKeyApplication Application => YubiKeyApplication.Fido2;

        private readonly MakeCredentialParameters _params;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        // Note that there is no object-initializer constructor. All the
        // constructor inputs have no default or are secret byte arrays.
        private MakeCredentialCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs an instance of the <see cref="MakeCredentialCommand" />
        /// class using the given parameters.
        /// </summary>
        /// <remarks>
        /// This class will copy a reference to the input parameters object. It
        /// will no longer need it after the call to <c>SendCommand</c>.
        /// </remarks>
        /// <param name="makeCredentialParameters">
        /// An object containing all the parameters the YubiKey will use to make
        /// a new credential.
        /// </param>
        public MakeCredentialCommand(MakeCredentialParameters makeCredentialParameters)
        {
            _params = makeCredentialParameters;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            byte[] encodedParams = _params.CborEncode();
            byte[] payload = new byte[encodedParams.Length + 1];
            payload[0] = CtapConstants.CtapMakeCredentialCmd;
            Array.Copy(encodedParams, 0, payload, 1, encodedParams.Length);
            return new CommandApdu()
            {
                Ins = CtapConstants.CtapHidCbor,
                Data = payload
            };
        }

        /// <inheritdoc />
        public MakeCredentialResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new MakeCredentialResponse(responseApdu);
    }
}
