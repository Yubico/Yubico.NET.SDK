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

using Yubico.YubiKey.TestUtilities;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Cose;
using Xunit;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    public class GetKeyAgreeCommandTests : SimpleIntegrationTestConnection
    {
        public GetKeyAgreeCommandTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Bio)
        {
        }

        [Fact]
        public void GetKeyAgreeCommand_Succeeds()
        {
            var cmd = new GetKeyAgreementCommand() { PinUvAuthProtocol = PinUvAuthProtocol.ProtocolTwo, };
            GetKeyAgreementResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            CoseEcPublicKey pubKey = rsp.GetData();
            Assert.Equal(CoseEcCurve.P256, pubKey.Curve);
        }
    }
}
