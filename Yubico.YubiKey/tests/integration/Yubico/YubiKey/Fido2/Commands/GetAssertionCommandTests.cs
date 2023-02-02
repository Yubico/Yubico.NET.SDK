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
using Xunit;
using Yubico.YubiKey.TestUtilities;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    public class GetAssertionCommandTests : SimpleIntegrationTestConnection
    {
        public GetAssertionCommandTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Bio)
        {
        }

        [Fact]
        public void GetAssertionCommand_Succeeds()
        {
            byte[] pin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

            var protocol = new PinUvAuthProtocolTwo();

            bool isValid = GetParams(protocol, pin, out GetAssertionParameters assertionParams);
            Assert.True(isValid);

            var cmd = new GetAssertionCommand(assertionParams);
            GetAssertionResponse rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
            GetAssertionData cData = rsp.GetData();
            if (!(cData.NumberOfCredentials is null) && (cData.NumberOfCredentials > 0))
            {
                int count = (int)cData.NumberOfCredentials;
                for (int index = 1; index < count; index++)
                {
                    var nextCmd = new GetNextAssertionCommand();
                    rsp = Connection.SendCommand(nextCmd);
                    Assert.Equal(ResponseStatus.Success, rsp.Status);
                    cData = rsp.GetData();
                }
            }
            Assert.Equal(48, cData.CredentialId.Id.Length);
        }

        private bool GetParams(
            PinUvAuthProtocolBase protocol,
            byte[] pin,
            out GetAssertionParameters assertionParams)
        {
            byte[] clientDataHash = new byte[] {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };

            var rp = new RelyingParty("SomeRpId")
            {
                Name = "SomeRpName",
            };

            assertionParams = new GetAssertionParameters(rp, clientDataHash);

            if (!GetPinToken(protocol, pin, out byte[] pinToken))
            {
                return false;
            }

            byte[] pinUvAuthParam = protocol.AuthenticateUsingPinToken(pinToken, clientDataHash);

            assertionParams.Protocol = protocol.Protocol;
            assertionParams.PinUvAuthParam = pinUvAuthParam;

            //assertionParams.AddOption("rk", true);
            assertionParams.AddOption("up", true);
            //assertionParams.AddOption("uv", false);

            return true;
        }

        // This will get a PIN token.
        // To do so, it will check the PinProtocol object. If it is not yet in a
        // post-Encapsulate state, it will get the YubiKey's public key, then
        // call Encapsulate (the input object will be updated).
        // Next, it will get a PIN token using the given PIN.
        // If that works, return the PIN token (the out arg).
        // If it doesn't work because there is no PIN set, set the PIN and then
        // get the PIN token.
        // If it doesn't work because the PIN was wron, return false.
        private bool GetPinToken(
            PinUvAuthProtocolBase protocol,
            byte[] pin,
            out byte[] pinToken)
        {
            pinToken = Array.Empty<byte>();
            if (protocol.AuthenticatorPublicKey is null)
            {
                var getKeyCmd = new GetKeyAgreementCommand(protocol.Protocol);
                GetKeyAgreementResponse getKeyRsp = Connection.SendCommand(getKeyCmd);
                if (getKeyRsp.Status != ResponseStatus.Success)
                {
                    return false;
                }

                protocol.Encapsulate(getKeyRsp.GetData());

                var getTokenCmd = new GetPinTokenCommand(protocol, pin);
                GetPinUvAuthTokenResponse getTokenRsp = Connection.SendCommand(getTokenCmd);
                if (getTokenRsp.Status == ResponseStatus.Success)
                {
                    pinToken = getTokenRsp.GetData().ToArray();
                    return true;
                }

                if (getTokenRsp.StatusWord == 0x6F31)
                {
                    return false;
                }

                var setPinCmd = new SetPinCommand(protocol, pin);
                SetPinResponse setPinRsp = Connection.SendCommand(setPinCmd);
                if (setPinRsp.Status != ResponseStatus.Success)
                {
                    return false;
                }
            }

            var cmd = new GetPinTokenCommand(protocol, pin);
            GetPinUvAuthTokenResponse rsp = Connection.SendCommand(cmd);
            if (rsp.Status == ResponseStatus.Success)
            {
                pinToken = rsp.GetData().ToArray();
                return true;
            }

            return false;
        }
    }
}
