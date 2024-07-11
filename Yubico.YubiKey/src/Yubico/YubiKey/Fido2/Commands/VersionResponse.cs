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

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Response to a command to get the firmware version.
    /// </summary>
    /// <remarks>
    /// <p>
    /// This is the partner Response class to <see cref="VersionCommand"/>.
    /// </p>
    /// <p>
    /// The data returned is <see cref="FirmwareVersion"/>.
    /// </p>
    /// </remarks>
    internal class VersionResponse : Fido2Response, IYubiKeyResponseWithData<FirmwareVersion>
    {
        private const int expectedResponseLength = 17;

        public VersionResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
        }

        /// <inheritdoc/>
        public FirmwareVersion GetData()
        {
            if (ResponseApdu.SW != SWConstants.Success)
            {
                throw new InvalidOperationException(ExceptionMessages.NoResponseDataApduFailed);
            }

            if (ResponseApdu.Data.Length != expectedResponseLength)
            {
                throw new MalformedYubiKeyResponseException(ExceptionMessages.UnknownFidoError);
            }

            ReadOnlySpan<byte> responseApduData = ResponseApdu.Data.Span;

            return new FirmwareVersion()
            {
                Major = responseApduData[13],
                Minor = responseApduData[14],
                Patch = responseApduData[15]
            };
        }
    }
}
