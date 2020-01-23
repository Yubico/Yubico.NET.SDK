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

using System.Formats.Cbor;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Serialization;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Represents a response to the GetAssertion FIDO2 command. Contains a <see cref="GetAssertionOutput"/> as its data.
    /// </summary>
    internal class GetAssertionResponse : Fido2Response, IYubiKeyResponseWithData<GetAssertionOutput>
    {
        public GetAssertionResponse(ResponseApdu responseApdu) : base(responseApdu)
        {

        }

        public GetAssertionOutput GetData()
        {
            ThrowIfFailed();

            byte[] cborData = ResponseApdu.Data.Slice(1).ToArray();
            var reader = new CborReader(cborData, CborConformanceMode.Ctap2Canonical);

            GetAssertionOutput getAssertionOutput = Ctap2CborSerializer.Deserialize<GetAssertionOutput>(reader);

            return getAssertionOutput;
        }
    }
}
