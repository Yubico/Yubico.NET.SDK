// Copyright 2023 Yubico AB
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

namespace Yubico.YubiKey.Pipelines
{
    /// <summary>
    /// Used to parse out the CTAP status byte from a response APDU and reform the response APDU in the SW + Data
    /// format that the SDK's command layer can understand.
    /// </summary>
    internal class FidoErrorTransform : IApduTransform
    {
        private readonly IApduTransform _nextTransform;

        public FidoErrorTransform(IApduTransform nextTransform)
        {
            _nextTransform = nextTransform;
        }

        /// <inheritdoc />
        public ResponseApdu Invoke(CommandApdu command, Type commandType, Type responseType)
        {
            var fidoResponse = _nextTransform.Invoke(
                command,
                commandType,
                responseType);

            return CtapToApduResponse.ToCtap2ResponseApdu(fidoResponse.Data.ToArray());
        }

        /// <inheritdoc />
        public void Setup() => _nextTransform.Setup();

        /// <inheritdoc />
        public void Cleanup() => _nextTransform.Cleanup();
    }
}
